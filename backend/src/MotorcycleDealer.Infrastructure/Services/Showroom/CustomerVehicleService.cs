using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.ValueObjects;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

public sealed class CustomerVehicleService : ICustomerVehicleService
{
    private const int DefaultDueSoonMileageLeadKm = 300;
    private const int DefaultDueSoonLeadDays = 14;

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<RegisterExternalVehicleRequest> _registerValidator;
    private readonly IValidator<UpdateVehicleMileageRequest> _mileageValidator;

    public CustomerVehicleService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<RegisterExternalVehicleRequest> registerValidator,
        IValidator<UpdateVehicleMileageRequest> mileageValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _mileageValidator = mileageValidator ?? throw new ArgumentNullException(nameof(mileageValidator));
    }

    public async Task<IReadOnlyList<CustomerVehicleDto>> ListVehiclesAsync(
        VehicleLookupQuery query,
        bool forceOwnCustomerOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var canViewDealer = !forceOwnCustomerOnly && _currentUserContext.HasPermission(Permissions.Vehicles.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Vehicles.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view customer garage vehicles.",
                "FORBIDDEN_PERMISSION");
        }

        var vehiclesQuery = _dbContext.CustomerVehicles
            .AsNoTracking()
            .Where(v => v.IsActive);

        if (!canViewDealer)
        {
            var customerId = _currentUserContext.RequireUserId();
            if (query.CustomerId is { } reqCustId && reqCustId != Guid.Empty && reqCustId != customerId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only query vehicles belonging to their own garage.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }

            vehiclesQuery = vehiclesQuery.Where(v => v.CustomerId == customerId);
        }
        else if (query.CustomerId is { } staffCustId && staffCustId != Guid.Empty)
        {
            vehiclesQuery = vehiclesQuery.Where(v => v.CustomerId == staffCustId);
        }

        if (!string.IsNullOrWhiteSpace(query.Vin))
        {
            var normalizedVin = query.Vin.Trim().ToUpperInvariant();
            if (normalizedVin.Length == Vin.CanonicalLength)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.VIN == normalizedVin);
            }
            else
            {
                var vinPattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(normalizedVin)}%";
                vehiclesQuery = vehiclesQuery.Where(v => EF.Functions.Like(v.VIN, vinPattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.LicensePlate))
        {
            var platePattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.LicensePlate.Trim().ToUpperInvariant())}%";
            vehiclesQuery = vehiclesQuery.Where(v => v.LicensePlate != null && EF.Functions.Like(v.LicensePlate, platePattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var emailPattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Email.Trim())}%";
            vehiclesQuery = vehiclesQuery.Where(v => v.Customer != null && EF.Functions.Like(v.Customer.Email, emailPattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            vehiclesQuery = vehiclesQuery.Where(v =>
                EF.Functions.Like(v.VIN, searchPattern) ||
                (v.LicensePlate != null && EF.Functions.Like(v.LicensePlate, searchPattern)) ||
                (v.Customer != null && (
                    EF.Functions.Like(v.Customer.FullName, searchPattern) ||
                    EF.Functions.Like(v.Customer.Email, searchPattern) ||
                    EF.Functions.Like(v.Customer.PhoneNumber, searchPattern))) ||
                (v.MotorcycleModel != null && EF.Functions.Like(v.MotorcycleModel.ModelName, searchPattern)));
        }

        var vehicles = await vehiclesQuery
            .Include(v => v.Customer)
            .Include(v => v.MotorcycleModel)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return await HydrateVehicleDtosAsync(vehicles, cancellationToken);
    }

    public async Task<CustomerVehicleDto> GetVehicleByIdAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAuthorizedVehicleAsync(vehicleId, cancellationToken);
        var hydrated = await HydrateVehicleDtosAsync([vehicle], cancellationToken);
        return hydrated[0];
    }

    public async Task<VehicleMaintenanceScheduleDto> GetVehicleMaintenanceScheduleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAuthorizedVehicleAsync(vehicleId, cancellationToken);
        var hydrated = await HydrateVehicleDtosAsync([vehicle], cancellationToken);
        var vehicleDto = hydrated[0];

        var rules = await _dbContext.MaintenanceRules
            .AsNoTracking()
            .Where(r => r.MotorcycleModelId == vehicle.MotorcycleModelId && r.IsActive)
            .OrderBy(r => r.IntervalKm)
            .ThenBy(r => r.IntervalMonths)
            .ToListAsync(cancellationToken);

        var reminders = await _dbContext.MaintenanceReminders
            .AsNoTracking()
            .Include(r => r.MaintenanceRule)
            .Where(r => r.CustomerVehicleId == vehicle.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        // Global IDealerScoped filter automatically scopes ServiceInvoices when called by DealerStaff.
        var invoices = await _dbContext.ServiceInvoices
            .AsNoTracking()
            .Include(i => i.Dealer)
            .Include(i => i.ServiceBooking)
            .Where(i => i.ServiceBooking != null && i.ServiceBooking.CustomerVehicleId == vehicle.Id)
            .OrderByDescending(i => i.PaidAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        var reminderDtos = reminders
            .Select(r => new MaintenanceReminderDto(
                Id: r.Id,
                CustomerVehicleId: r.CustomerVehicleId,
                MaintenanceRuleId: r.MaintenanceRuleId,
                MilestoneCode: r.MaintenanceRule?.MilestoneCode ?? string.Empty,
                MilestoneName: r.MaintenanceRule?.MilestoneName ?? $"{r.TargetMileageKm:N0} km Periodic Service",
                TargetMileageKm: r.TargetMileageKm,
                DueDateUtc: r.DueDateUtc,
                TriggerReason: r.TriggerReason,
                DeduplicationKey: r.DeduplicationKey,
                Status: r.Status,
                ResolvedBookingId: r.ResolvedBookingId,
                SentAtUtc: r.SentAtUtc,
                CreatedAtUtc: r.CreatedAtUtc))
            .ToList();

        var invoiceDtos = invoices
            .Select(i => new VehicleRecentInvoiceSummaryDto(
                InvoiceId: i.Id,
                InvoiceNumber: i.InvoiceNumber,
                ServiceBookingId: i.ServiceBookingId,
                DealerId: i.DealerId,
                DealerName: i.Dealer?.Name ?? string.Empty,
                GrossAmount: i.GrossAmount,
                VoucherDiscountAmount: i.VoucherDiscountAmount,
                NetPaidAmount: i.NetPaidAmount,
                OdometerAtCompletionKm: i.OdometerAtCompletionKm,
                PointsEarned: i.PointsEarned,
                PaidAtUtc: i.PaidAtUtc))
            .ToList();

        return new VehicleMaintenanceScheduleDto(
            Vehicle: vehicleDto,
            Reminders: reminderDtos,
            MaintenanceRules: rules.Select(ShowroomCatalogService.MapRuleToDto).ToList(),
            ServiceInvoices: invoiceDtos);
    }

    public async Task<CustomerVehicleDto> RegisterExternalVehicleAsync(
        RegisterExternalVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        var utcNow = _dateTimeProvider.UtcNow;

        if (request.LastServiceDateUtc is { } lastSvcDate && lastSvcDate > utcNow.AddMinutes(5))
        {
            throw new ValidationException(
            [
                new ValidationFailure("lastServiceDateUtc", "LastServiceDateUtc cannot be in the future.")
            ]);
        }

        var customer = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Customer", customerId);

        var model = await _dbContext.MotorcycleModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MotorcycleModelId, cancellationToken)
            ?? throw NotFoundException.ForEntity("MotorcycleModel", request.MotorcycleModelId);

        if (!model.IsActive)
        {
            throw new BusinessRuleViolationException(
                "MODEL_INACTIVE",
                $"Motorcycle model '{model.ModelCode}' is inactive and unavailable for registration.");
        }

        var normalizedVin = Vin.Create(request.Vin.Trim().ToUpperInvariant()).Value;
        var normalizedPlate = string.IsNullOrWhiteSpace(request.LicensePlate)
            ? null
            : request.LicensePlate.Trim().ToUpperInvariant();

        var existsInGarage = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.VIN == normalizedVin, cancellationToken);

        if (existsInGarage)
        {
            throw new ConflictException(
                $"VIN '{normalizedVin}' is already registered in a customer garage.",
                "DUPLICATE_VIN");
        }

        var existsInUnsoldShowroomStock = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AnyAsync(
                inv => inv.VIN == normalizedVin &&
                       (inv.StockStatus == "Available" || inv.StockStatus == "Reserved"),
                cancellationToken);

        if (existsInUnsoldShowroomStock)
        {
            throw new ConflictException(
                $"VIN '{normalizedVin}' belongs to unsold showroom inventory and must be onboarded via a dealer SalesOrder.",
                "DUPLICATE_VIN");
        }

        if (normalizedPlate is not null)
        {
            var plateExists = await _dbContext.CustomerVehicles
                .IgnoreQueryFilters()
                .AnyAsync(v => !v.IsDeleted && v.LicensePlate == normalizedPlate, cancellationToken);

            if (plateExists)
            {
                throw new ConflictException(
                    $"License plate '{normalizedPlate}' is already registered to another active vehicle.",
                    "DUPLICATE_LICENSE_PLATE");
            }
        }

        var rules = await _dbContext.MaintenanceRules
            .AsNoTracking()
            .Where(r => r.MotorcycleModelId == model.Id && r.IsActive)
            .OrderBy(r => r.IntervalKm)
            .ThenBy(r => r.IntervalMonths)
            .ToListAsync(cancellationToken);

        var (nextDueMileageKm, nextDueDateUtc) = ComputeNextDueSchedule(
            currentMileageKm: request.CurrentMileageKm,
            lastServiceMileageKm: request.LastServiceMileageKm,
            referenceDateUtc: request.LastServiceDateUtc ?? utcNow,
            orderedRules: rules);

        var vehicle = new CustomerVehicle(Guid.CreateVersion7())
        {
            VIN = normalizedVin,
            CustomerId = customer.Id,
            MotorcycleModelId = model.Id,
            PurchasedSalesOrderId = null,
            LicensePlate = normalizedPlate,
            ColorName = string.IsNullOrWhiteSpace(request.ColorName) ? null : request.ColorName.Trim(),
            RegistrationSource = "ExternalManual",
            OwnershipStartDateUtc = utcNow,
            CurrentMileageKm = request.CurrentMileageKm,
            LastServiceMileageKm = request.LastServiceMileageKm,
            LastServiceDateUtc = request.LastServiceDateUtc,
            NextDueMileageKm = nextDueMileageKm,
            NextDueDateUtc = nextDueDateUtc,
            IsActive = true,
            CreatedAtUtc = utcNow
        };

        _dbContext.CustomerVehicles.Add(vehicle);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_CustomerVehicles_LicensePlate", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    $"License plate '{normalizedPlate}' is already registered to another active vehicle.",
                    "DUPLICATE_LICENSE_PLATE");
            }

            throw new ConflictException(
                $"VIN '{normalizedVin}' is already registered in a customer garage.",
                "DUPLICATE_VIN");
        }

        vehicle.Customer = customer;
        vehicle.MotorcycleModel = model;

        var hydrated = await HydrateVehicleDtosAsync([vehicle], cancellationToken);
        return hydrated[0];
    }

    public async Task<CustomerVehicleDto> UpdateVehicleMileageAsync(
        Guid vehicleId,
        UpdateVehicleMileageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _mileageValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        var requestedMileageKm = request.ResolveMileageKm()!.Value;

        var vehicle = await _dbContext.CustomerVehicles
            .Include(v => v.Customer)
            .Include(v => v.MotorcycleModel)
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.IsActive, cancellationToken)
            ?? throw NotFoundException.ForEntity("CustomerVehicle", vehicleId);

        if (vehicle.CustomerId != customerId)
        {
            throw new ForbiddenAccessException(
                "Customers may only update odometer readings for vehicles in their own garage.",
                "FORBIDDEN_RESOURCE_OWNER");
        }

        if (requestedMileageKm < vehicle.CurrentMileageKm)
        {
            throw new BusinessRuleViolationException(
                "ODOMETER_REGRESSION",
                $"New odometer reading ({requestedMileageKm:N0} km) cannot be less than current recorded mileage ({vehicle.CurrentMileageKm:N0} km).");
        }

        vehicle.CurrentMileageKm = requestedMileageKm;
        vehicle.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var hydrated = await HydrateVehicleDtosAsync([vehicle], cancellationToken);
        return hydrated[0];
    }

    private async Task<CustomerVehicle> LoadAuthorizedVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Vehicles.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Vehicles.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view customer garage vehicles.",
                "FORBIDDEN_PERMISSION");
        }

        var vehicle = await _dbContext.CustomerVehicles
            .AsNoTracking()
            .Include(v => v.Customer)
            .Include(v => v.MotorcycleModel)
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.IsActive, cancellationToken)
            ?? throw NotFoundException.ForEntity("CustomerVehicle", vehicleId);

        if (!canViewDealer)
        {
            var customerId = _currentUserContext.RequireUserId();
            if (vehicle.CustomerId != customerId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only access vehicles belonging to their own garage.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }

        return vehicle;
    }

    private async Task<IReadOnlyList<CustomerVehicleDto>> HydrateVehicleDtosAsync(
        IReadOnlyList<CustomerVehicle> vehicles,
        CancellationToken cancellationToken)
    {
        if (vehicles.Count == 0)
        {
            return [];
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var vehicleIds = vehicles.Select(v => v.Id).ToArray();
        var modelIds = vehicles.Select(v => v.MotorcycleModelId).Distinct().ToArray();

        var rules = await _dbContext.MaintenanceRules
            .AsNoTracking()
            .Where(r => modelIds.Contains(r.MotorcycleModelId) && r.IsActive)
            .OrderBy(r => r.IntervalKm)
            .ThenBy(r => r.IntervalMonths)
            .ToListAsync(cancellationToken);

        var rulesByModel = rules
            .GroupBy(r => r.MotorcycleModelId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var activeReminderCounts = await _dbContext.MaintenanceReminders
            .AsNoTracking()
            .Where(r =>
                vehicleIds.Contains(r.CustomerVehicleId) &&
                (r.Status == "Pending" || r.Status == "Sent"))
            .GroupBy(r => r.CustomerVehicleId)
            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VehicleId, x => x.Count, cancellationToken);

        var bookings = await _dbContext.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Dealer)
            .Include(b => b.ServiceTimeSlot)
            .Where(b => vehicleIds.Contains(b.CustomerVehicleId))
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var recentBookingsByVehicle = bookings
            .GroupBy(b => b.CustomerVehicleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<VehicleRecentBookingSummaryDto>)g
                    .Take(5)
                    .Select(b => new VehicleRecentBookingSummaryDto(
                        BookingId: b.Id,
                        BookingNumber: b.BookingNumber,
                        DealerId: b.DealerId,
                        DealerName: b.Dealer?.Name ?? string.Empty,
                        SlotDate: b.ServiceTimeSlot?.SlotDate ?? DateOnly.FromDateTime(b.CreatedAtUtc.DateTime),
                        ServiceType: b.ServiceType,
                        Status: b.Status,
                        OdometerAtBookingKm: b.OdometerAtBookingKm,
                        CreatedAtUtc: b.CreatedAtUtc))
                    .ToList());

        var invoices = await _dbContext.ServiceInvoices
            .AsNoTracking()
            .Include(i => i.Dealer)
            .Include(i => i.ServiceBooking)
            .Where(i => i.ServiceBooking != null && vehicleIds.Contains(i.ServiceBooking.CustomerVehicleId))
            .OrderByDescending(i => i.PaidAtUtc)
            .ToListAsync(cancellationToken);

        var recentInvoicesByVehicle = invoices
            .GroupBy(i => i.ServiceBooking!.CustomerVehicleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<VehicleRecentInvoiceSummaryDto>)g
                    .Take(5)
                    .Select(i => new VehicleRecentInvoiceSummaryDto(
                        InvoiceId: i.Id,
                        InvoiceNumber: i.InvoiceNumber,
                        ServiceBookingId: i.ServiceBookingId,
                        DealerId: i.DealerId,
                        DealerName: i.Dealer?.Name ?? string.Empty,
                        GrossAmount: i.GrossAmount,
                        VoucherDiscountAmount: i.VoucherDiscountAmount,
                        NetPaidAmount: i.NetPaidAmount,
                        OdometerAtCompletionKm: i.OdometerAtCompletionKm,
                        PointsEarned: i.PointsEarned,
                        PaidAtUtc: i.PaidAtUtc))
                    .ToList());

        return vehicles
            .Select(v =>
            {
                var modelRules = rulesByModel.GetValueOrDefault(v.MotorcycleModelId, []);
                var matchedRule = modelRules.FirstOrDefault(r => r.IntervalKm >= v.NextDueMileageKm)
                                  ?? modelRules.Where(r => r.IsRecurring).OrderBy(r => r.IntervalKm).FirstOrDefault()
                                  ?? modelRules.LastOrDefault();

                var leadKm = Math.Max(DefaultDueSoonMileageLeadKm, matchedRule?.LeadMileageKm ?? DefaultDueSoonMileageLeadKm);
                var leadDays = Math.Max(DefaultDueSoonLeadDays, matchedRule?.LeadTimeDays ?? DefaultDueSoonLeadDays);

                var status = EvaluateMaintenanceStatus(
                    currentMileageKm: v.CurrentMileageKm,
                    nextDueMileageKm: v.NextDueMileageKm,
                    nextDueDateUtc: v.NextDueDateUtc,
                    leadMileageKm: leadKm,
                    leadTimeDays: leadDays,
                    utcNow: utcNow);

                return new CustomerVehicleDto(
                    Id: v.Id,
                    Vin: v.VIN,
                    CustomerId: v.CustomerId,
                    CustomerFullName: v.Customer?.FullName ?? string.Empty,
                    CustomerEmail: v.Customer?.Email ?? string.Empty,
                    CustomerPhoneNumber: v.Customer?.PhoneNumber ?? string.Empty,
                    MotorcycleModelId: v.MotorcycleModelId,
                    ModelCode: v.MotorcycleModel?.ModelCode ?? string.Empty,
                    Brand: v.MotorcycleModel?.Brand ?? string.Empty,
                    MotorcycleModelName: v.MotorcycleModel?.ModelName ?? string.Empty,
                    Category: v.MotorcycleModel?.Category ?? string.Empty,
                    EngineDisplacementCc: v.MotorcycleModel?.EngineDisplacementCc ?? 0,
                    ImageUrl: v.MotorcycleModel?.ImageUrl,
                    SpecificationsJson: v.MotorcycleModel?.SpecificationsJson,
                    PurchasedSalesOrderId: v.PurchasedSalesOrderId,
                    LicensePlate: v.LicensePlate,
                    ColorName: v.ColorName,
                    RegistrationSource: v.RegistrationSource,
                    OwnershipStartDateUtc: v.OwnershipStartDateUtc,
                    CurrentMileageKm: v.CurrentMileageKm,
                    LastServiceMileageKm: v.LastServiceMileageKm,
                    LastServiceDateUtc: v.LastServiceDateUtc,
                    NextDueMileageKm: v.NextDueMileageKm,
                    NextDueDateUtc: v.NextDueDateUtc,
                    NextMilestoneName: matchedRule?.MilestoneName ?? $"{v.NextDueMileageKm:N0} km Periodic Service",
                    MaintenanceStatus: status,
                    IsDueSoonOrOverdue: !string.Equals(status, "Upcoming", StringComparison.Ordinal),
                    ActiveReminderCount: activeReminderCounts.GetValueOrDefault(v.Id, 0),
                    RecentBookings: recentBookingsByVehicle.GetValueOrDefault(v.Id, []),
                    RecentInvoices: recentInvoicesByVehicle.GetValueOrDefault(v.Id, []));
            })
            .ToList();
    }

    private static (int NextDueMileageKm, DateTimeOffset NextDueDateUtc) ComputeNextDueSchedule(
        int currentMileageKm,
        int lastServiceMileageKm,
        DateTimeOffset referenceDateUtc,
        IReadOnlyList<MaintenanceRule> orderedRules)
    {
        var baselineKm = Math.Max(currentMileageKm, lastServiceMileageKm);

        if (orderedRules.Count == 0)
        {
            return (baselineKm + 1_000, referenceDateUtc.AddMonths(1));
        }

        var nextFixedRule = orderedRules.FirstOrDefault(r => r.IntervalKm > baselineKm);
        if (nextFixedRule is not null)
        {
            return (nextFixedRule.IntervalKm, referenceDateUtc.AddMonths(nextFixedRule.IntervalMonths));
        }

        var recurringRule = orderedRules
            .Where(r => r.IsRecurring && r.IntervalKm > 0)
            .OrderBy(r => r.IntervalKm)
            .FirstOrDefault()
            ?? orderedRules[^1];

        var stepKm = Math.Max(500, recurringRule.IntervalKm);
        var nextRecurringTargetKm = ((baselineKm / stepKm) + 1) * stepKm;
        return (nextRecurringTargetKm, referenceDateUtc.AddMonths(Math.Max(1, recurringRule.IntervalMonths)));
    }

    private static string EvaluateMaintenanceStatus(
        int currentMileageKm,
        int nextDueMileageKm,
        DateTimeOffset nextDueDateUtc,
        int leadMileageKm,
        int leadTimeDays,
        DateTimeOffset utcNow)
    {
        if (currentMileageKm >= nextDueMileageKm || utcNow >= nextDueDateUtc)
        {
            return "Overdue";
        }

        if (currentMileageKm >= nextDueMileageKm - leadMileageKm ||
            utcNow >= nextDueDateUtc.AddDays(-leadTimeDays))
        {
            return "DueSoon";
        }

        return "Upcoming";
    }
}
