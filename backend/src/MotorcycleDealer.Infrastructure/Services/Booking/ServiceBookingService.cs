using System.Globalization;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Booking;

public sealed class ServiceBookingService : IServiceBookingService
{
    private const int MaxSlotConcurrencyRetries = 2;
    private const string SlotExhaustedMessage =
        "Selected time slot capacity was just claimed by another customer. Please choose another available slot.";

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<CreateServiceBookingRequest> _createValidator;
    private readonly IValidator<UpdateBookingStatusRequest> _updateStatusValidator;
    private readonly IValidator<CancelBookingRequest> _cancelValidator;

    public ServiceBookingService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<CreateServiceBookingRequest> createValidator,
        IValidator<UpdateBookingStatusRequest> updateStatusValidator,
        IValidator<CancelBookingRequest> cancelValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateStatusValidator = updateStatusValidator ?? throw new ArgumentNullException(nameof(updateStatusValidator));
        _cancelValidator = cancelValidator ?? throw new ArgumentNullException(nameof(cancelValidator));
    }

    public async Task<BookingDto> CreateBookingAsync(
        CreateServiceBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.IsCustomer || !_currentUserContext.HasPermission(Permissions.Bookings.Create))
        {
            throw new ForbiddenAccessException(
                "Only authenticated Customer accounts may create workshop service bookings.",
                "FORBIDDEN_PERMISSION");
        }

        var targetSlotId = request.ResolveEffectiveSlotId();
        BookingValidationRules.TryNormalizeServiceType(request.ServiceType, out var normalizedServiceType);
        var decodedSlotRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.SlotRowVersion);
        var decodedVoucherRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.VoucherRowVersion);

        for (var attempt = 0; attempt <= MaxSlotConcurrencyRetries; attempt++)
        {
            var currentAttempt = attempt;
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                var (createdDto, realtimePayload, targetDealerId) = await executionStrategy.ExecuteAsync(async ct =>
                {
                    _dbContext.ChangeTracker.Clear();
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                    var utcNow = _dateTimeProvider.UtcNow;

                    var customer = await _dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == customerId && u.IsActive, ct)
                        ?? throw new UnauthorizedAccessException("Authenticated customer profile is missing or inactive.");

                    var vehicle = await _dbContext.CustomerVehicles
                        .IgnoreQueryFilters()
                        .Include(v => v.MotorcycleModel)
                        .FirstOrDefaultAsync(v => v.Id == request.CustomerVehicleId && !v.IsDeleted, ct)
                        ?? throw NotFoundException.ForEntity("CustomerVehicle", request.CustomerVehicleId);

                    if (vehicle.CustomerId != customerId)
                    {
                        throw new ForbiddenAccessException(
                            "Cannot book workshop service for a motorcycle owned by another customer.",
                            "FORBIDDEN_RESOURCE_OWNER");
                    }

                    if (!vehicle.IsActive)
                    {
                        throw new BusinessRuleViolationException(
                            "VEHICLE_INACTIVE",
                            "Cannot book workshop service for an inactive vehicle.");
                    }

                    if (request.OdometerAtBookingKm < vehicle.CurrentMileageKm)
                    {
                        throw new BusinessRuleViolationException(
                            "ODOMETER_REGRESSION",
                            $"Declared odometer ({request.OdometerAtBookingKm} km) cannot be lower than recorded garage mileage ({vehicle.CurrentMileageKm} km).");
                    }

                    if (request.OdometerAtBookingKm > vehicle.CurrentMileageKm)
                    {
                        vehicle.CurrentMileageKm = request.OdometerAtBookingKm;
                        vehicle.UpdatedAtUtc = utcNow;
                    }

                    var dealer = await _dbContext.Dealers
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == request.DealerId, ct);

                    if (dealer is null || dealer.IsDeleted)
                    {
                        throw NotFoundException.ForEntity("Dealer", request.DealerId);
                    }

                    if (!dealer.IsActive)
                    {
                        throw new BusinessRuleViolationException(
                            "DEALER_INACTIVE",
                            $"Dealership '{dealer.Name}' ({dealer.Code}) is currently inactive and cannot accept workshop bookings.");
                    }

                    var slot = await _dbContext.ServiceTimeSlots
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(s => s.Id == targetSlotId && s.DealerId == dealer.Id, ct)
                        ?? throw NotFoundException.ForEntity("ServiceTimeSlot", targetSlotId);

                    if (slot.IsBlocked || slot.BookedCount >= slot.MaxCapacity)
                    {
                        throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
                    }

                    if (currentAttempt == 0 && decodedSlotRowVersion is not null)
                    {
                        _dbContext.Entry(slot).Property(s => s.RowVersion).OriginalValue = decodedSlotRowVersion;
                    }

                    var hasDuplicateActiveBooking = await _dbContext.ServiceBookings
                        .IgnoreQueryFilters()
                        .AnyAsync(
                            b => b.CustomerVehicleId == vehicle.Id &&
                                 b.ServiceTimeSlotId == slot.Id &&
                                 b.Status != "Cancelled" &&
                                 b.Status != "NoShow",
                            ct);

                    if (hasDuplicateActiveBooking)
                    {
                        throw new ConflictException(
                            "This motorcycle already holds an active booking in the selected time slot.",
                            "DUPLICATE_VEHICLE_BOOKING");
                    }

                    var reservedVoucher = await LockOptionalCustomerVoucherAsync(
                        request,
                        customerId,
                        currentAttempt == 0 ? decodedVoucherRowVersion : null,
                        utcNow,
                        ct);

                    slot.BookedCount += 1;
                    slot.UpdatedAtUtc = utcNow;

                    var bookingNumber = GenerateBookingNumber(utcNow);
                    var trimmedNotes = string.IsNullOrWhiteSpace(request.ReportedNotes)
                        ? null
                        : request.ReportedNotes.Trim();

                    var booking = new ServiceBooking
                    {
                        BookingNumber = bookingNumber,
                        CustomerId = customerId,
                        DealerId = slot.DealerId,
                        CustomerVehicleId = vehicle.Id,
                        ServiceTimeSlotId = slot.Id,
                        AppliedCustomerVoucherId = reservedVoucher?.Id,
                        ServiceType = normalizedServiceType,
                        ReportedNotes = trimmedNotes,
                        OdometerAtBookingKm = request.OdometerAtBookingKm,
                        Status = "Pending",
                        CreatedAtUtc = utcNow
                    };

                    var openReminders = await _dbContext.MaintenanceReminders
                        .Where(r =>
                            r.CustomerVehicleId == vehicle.Id &&
                            r.ResolvedBookingId == null &&
                            (r.Status == "Pending" || r.Status == "Sent"))
                        .ToListAsync(ct);

                    foreach (var reminder in openReminders)
                    {
                        reminder.ResolvedBookingId = booking.Id;
                        reminder.Status = "Booked";
                        reminder.UpdatedAtUtc = utcNow;
                    }

                    var modelName = vehicle.MotorcycleModel?.ModelName ?? "Motorcycle";
                    var notification = new Notification
                    {
                        TargetDealerId = slot.DealerId,
                        EventType = "BookingCreated",
                        Title = $"New Workshop Booking {booking.BookingNumber}",
                        Message = $"{customer.FullName} booked {normalizedServiceType} for {modelName} ({vehicle.VIN}) on {slot.SlotDate:yyyy-MM-dd} at {slot.StartTime:HH\\:mm}.",
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            bookingId = booking.Id,
                            bookingNumber = booking.BookingNumber,
                            dealerId = slot.DealerId,
                            customerId,
                            customerFullName = customer.FullName,
                            vin = vehicle.VIN,
                            motorcycleModelName = modelName,
                            slotDate = slot.SlotDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            startTime = slot.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                            endTime = slot.EndTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                            serviceType = normalizedServiceType,
                            hasReservedVoucher = reservedVoucher is not null,
                            status = booking.Status
                        }),
                        IsRead = false,
                        CreatedAtUtc = utcNow
                    };

                    _dbContext.ServiceBookings.Add(booking);
                    _dbContext.Notifications.Add(notification);

                    await _dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    var dto = MapToDto(booking, customer, dealer, vehicle, slot, reservedVoucher);
                    var realtimeEvent = new BookingCreatedRealtimeEvent(
                        NotificationId: notification.Id,
                        EventType: "BookingCreated",
                        BookingId: booking.Id,
                        BookingNumber: booking.BookingNumber,
                        DealerId: slot.DealerId,
                        CustomerFullName: customer.FullName,
                        Vin: vehicle.VIN,
                        MotorcycleModelName: modelName,
                        SlotDate: slot.SlotDate,
                        StartTime: slot.StartTime,
                        EndTime: slot.EndTime,
                        ServiceType: booking.ServiceType,
                        HasReservedVoucher: reservedVoucher is not null,
                        Status: booking.Status,
                        OccurredAtUtc: utcNow);

                    return (dto, realtimeEvent, slot.DealerId);
                }, cancellationToken);

                await _notificationPublisher.PublishBookingCreatedAsync(
                    realtimePayload,
                    targetDealerId,
                    cancellationToken);

                return createdDto;
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.ChangeTracker.Clear();

                var freshSlot = await _dbContext.ServiceTimeSlots
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == targetSlotId && s.DealerId == request.DealerId, cancellationToken);

                if (freshSlot is null || freshSlot.IsBlocked || freshSlot.BookedCount >= freshSlot.MaxCapacity)
                {
                    throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
                }

                if (attempt < MaxSlotConcurrencyRetries)
                {
                    await Task.Delay(25 * (attempt + 1) + Random.Shared.Next(5, 20), cancellationToken);
                    continue;
                }

                throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                _dbContext.ChangeTracker.Clear();

                if (sqlEx.Number == 547)
                {
                    throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
                }

                if (sqlEx.Number is 2601 or 2627)
                {
                    if (sqlEx.Message.Contains("UQ_ServiceBookings_ActiveVehicleSlot", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ConflictException(
                            "This motorcycle already holds an active booking in the selected time slot.",
                            "DUPLICATE_VEHICLE_BOOKING");
                    }

                    if (sqlEx.Message.Contains("UQ_ServiceBookings_ActiveVoucher", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ConflictException(
                            "The selected voucher is already reserved on another active booking.",
                            "VOUCHER_NOT_AVAILABLE");
                    }

                    if (attempt < MaxSlotConcurrencyRetries)
                    {
                        continue;
                    }

                    throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
                }

                throw;
            }
        }

        throw new ConflictException(SlotExhaustedMessage, "SLOT_CAPACITY_EXHAUSTED");
    }

    public async Task<PagedResult<BookingDto>> ListBookingsAsync(
        BookingQuery query,
        bool forceCustomerOwnScope = false,
        bool forceDealerQueueScope = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var callerUserId = _currentUserContext.RequireUserId();

        if (query.PageNumber < 1 || query.PageSize is < 1 or > PaginationQuery.MaxPageSize)
        {
            throw new ValidationException([
                new ValidationFailure(
                    "pagination",
                    $"PageNumber must be >= 1 and PageSize must be between 1 and {PaginationQuery.MaxPageSize}.")
            ]);
        }

        string? normalizedStatusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!BookingValidationRules.TryNormalizeBookingStatus(query.Status, out var parsedStatus))
            {
                throw new ValidationException([
                    new ValidationFailure("status", $"Unsupported booking status filter '{query.Status}'.")
                ]);
            }

            normalizedStatusFilter = parsedStatus;
        }

        IQueryable<ServiceBooking> bookingsQuery;

        if (forceCustomerOwnScope || (_currentUserContext.IsCustomer && !forceDealerQueueScope))
        {
            if (!_currentUserContext.HasPermission(Permissions.Bookings.ViewOwn))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view customer bookings.",
                    "FORBIDDEN_PERMISSION");
            }

            bookingsQuery = _dbContext.ServiceBookings
                .AsNoTracking()
                .Where(b => b.CustomerId == callerUserId);

            if (query.DealerId is { } filterDealerId && filterDealerId != Guid.Empty)
            {
                bookingsQuery = bookingsQuery.Where(b => b.DealerId == filterDealerId);
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            if (!_currentUserContext.HasPermission(Permissions.Bookings.ViewDealer))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view dealership workshop bookings.",
                    "FORBIDDEN_PERMISSION");
            }

            var assignedDealerId = _currentUserContext.RequireAssignedDealerId();
            if (query.DealerId is { } requestedDealerId &&
                requestedDealerId != Guid.Empty &&
                requestedDealerId != assignedDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff is strictly prohibited from viewing workshop bookings belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            bookingsQuery = _dbContext.ServiceBookings
                .AsNoTracking()
                .Where(b => b.DealerId == assignedDealerId);
        }
        else if (_currentUserContext.IsAdmin)
        {
            if (!_currentUserContext.HasPermission(Permissions.Bookings.ViewDealer))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view workshop bookings.",
                    "FORBIDDEN_PERMISSION");
            }

            bookingsQuery = _dbContext.ServiceBookings.AsNoTracking();
            if (query.DealerId is { } filterDealerId && filterDealerId != Guid.Empty)
            {
                bookingsQuery = bookingsQuery.Where(b => b.DealerId == filterDealerId);
            }
        }
        else
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view workshop bookings.",
                "FORBIDDEN_PERMISSION");
        }

        if (normalizedStatusFilter is not null)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Status == normalizedStatusFilter);
        }

        if (query.SlotDate is { } slotDate && slotDate != default)
        {
            bookingsQuery = bookingsQuery.Where(b =>
                b.ServiceTimeSlot != null && b.ServiceTimeSlot.SlotDate == slotDate);
        }

        var totalCount = await bookingsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<BookingDto>.Create([], query.NormalizedPageNumber, query.NormalizedPageSize, 0);
        }

        var rows = await bookingsQuery
            .Include(b => b.Customer)
            .Include(b => b.Dealer)
            .Include(b => b.CustomerVehicle)
                .ThenInclude(v => v!.MotorcycleModel)
            .Include(b => b.ServiceTimeSlot)
            .Include(b => b.AppliedCustomerVoucher)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(b => MapToDto(
                b,
                b.Customer,
                b.Dealer,
                b.CustomerVehicle,
                b.ServiceTimeSlot,
                b.AppliedCustomerVoucher))
            .ToList();

        return PagedResult<BookingDto>.Create(
            items,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<BookingDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Bookings.ViewOwn);
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Bookings.ViewDealer);

        if (!canViewOwn && !canViewDealer)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view booking details.",
                "FORBIDDEN_PERMISSION");
        }

        if (_currentUserContext.IsDealerStaff)
        {
            _ = _currentUserContext.RequireAssignedDealerId();
        }

        var booking = await _dbContext.ServiceBookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Dealer)
            .Include(b => b.CustomerVehicle)
                .ThenInclude(v => v!.MotorcycleModel)
            .Include(b => b.ServiceTimeSlot)
            .Include(b => b.AppliedCustomerVoucher)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw NotFoundException.ForEntity("ServiceBooking", bookingId);

        if (_currentUserContext.IsCustomer && booking.CustomerId != callerUserId)
        {
            throw new ForbiddenAccessException(
                "Customers cannot view workshop bookings belonging to another user.",
                "FORBIDDEN_RESOURCE_OWNER");
        }

        return MapToDto(
            booking,
            booking.Customer,
            booking.Dealer,
            booking.CustomerVehicle,
            booking.ServiceTimeSlot,
            booking.AppliedCustomerVoucher);
    }

    public async Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        CancelBookingRequest? request,
        CancellationToken cancellationToken = default)
    {
        var effectiveRequest = request ?? new CancelBookingRequest();
        await _cancelValidator.ValidateAndThrowAsync(effectiveRequest, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        var canCancelOwn = _currentUserContext.HasPermission(Permissions.Bookings.CancelOwn);
        var canUpdateStatus = _currentUserContext.HasPermission(Permissions.Bookings.UpdateStatus);

        if (!canCancelOwn && !canUpdateStatus)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to cancel workshop bookings.",
                "FORBIDDEN_PERMISSION");
        }

        if (_currentUserContext.IsDealerStaff)
        {
            _ = _currentUserContext.RequireAssignedDealerId();
        }

        var decodedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(effectiveRequest.RowVersion);
        var cancellationReason = effectiveRequest.ResolveEffectiveCancellationReason();
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (cancelledDto, statusEvent, customerId, dealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var utcNow = _dateTimeProvider.UtcNow;

            var ctx = await LoadBookingAggregateForMutationAsync(bookingId, ct)
                ?? throw NotFoundException.ForEntity("ServiceBooking", bookingId);
            var booking = ctx.Booking;

            if (_currentUserContext.IsCustomer && booking.CustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Cannot cancel a workshop booking belonging to another customer.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }

            if (decodedRowVersion is not null)
            {
                if (!booking.RowVersion.AsSpan().SequenceEqual(decodedRowVersion))
                {
                    throw new ConflictException(
                        "The booking was modified by another transaction. Please refresh and retry.",
                        "CONCURRENCY_CONFLICT");
                }

                _dbContext.Entry(booking).Property(b => b.RowVersion).OriginalValue = decodedRowVersion;
            }

            if (booking.Status is not ("Pending" or "Confirmed"))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_STATUS_TRANSITION",
                    $"Cannot cancel booking '{booking.BookingNumber}' while in '{booking.Status}' status. Only Pending or Confirmed bookings can be cancelled.");
            }

            var previousStatus = booking.Status;
            booking.Status = "Cancelled";
            booking.CancelledAtUtc = utcNow;
            booking.CancellationReason = cancellationReason;
            booking.UpdatedAtUtc = utcNow;

            await ReleaseSlotVoucherAndRemindersAsync(booking.Id, ctx.Slot, ctx.Voucher, utcNow, ct);

            var dealerName = ctx.Dealer?.Name ?? "Authorized Dealership";
            var notification = CreateStatusChangedNotification(
                booking,
                previousStatus,
                "Cancelled",
                dealerName,
                utcNow);

            _dbContext.Notifications.Add(notification);

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Concurrent update detected while cancelling booking. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await transaction.CommitAsync(ct);

            var dto = MapToDto(
                booking,
                ctx.Customer,
                ctx.Dealer,
                ctx.Vehicle,
                ctx.Slot,
                ctx.Voucher);

            var realtimePayload = new BookingStatusChangedRealtimeEvent(
                NotificationId: notification.Id,
                EventType: "BookingStatusChanged",
                BookingId: booking.Id,
                BookingNumber: booking.BookingNumber,
                PreviousStatus: previousStatus,
                NewStatus: "Cancelled",
                DealerName: dealerName,
                OccurredAtUtc: utcNow);

            return (dto, realtimePayload, booking.CustomerId, booking.DealerId);
        }, cancellationToken);

        await _notificationPublisher.PublishBookingStatusChangedAsync(
            statusEvent,
            customerId,
            dealerId,
            cancellationToken);

        return cancelledDto;
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(
        Guid bookingId,
        UpdateBookingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Bookings.UpdateStatus))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to update workshop booking status.",
                "FORBIDDEN_PERMISSION");
        }

        if (_currentUserContext.IsDealerStaff)
        {
            _ = _currentUserContext.RequireAssignedDealerId();
        }

        BookingValidationRules.TryNormalizeBookingStatus(request.ResolveEffectiveStatus(), out var targetStatus);
        var decodedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.RowVersion);
        var reasonOrNotes = request.ResolveEffectiveReasonOrNotes();
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (updatedDto, statusEvent, customerId, dealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var utcNow = _dateTimeProvider.UtcNow;

            var ctx = await LoadBookingAggregateForMutationAsync(bookingId, ct)
                ?? throw NotFoundException.ForEntity("ServiceBooking", bookingId);
            var booking = ctx.Booking;

            if (decodedRowVersion is not null)
            {
                if (!booking.RowVersion.AsSpan().SequenceEqual(decodedRowVersion))
                {
                    throw new ConflictException(
                        "The booking was modified by another transaction. Please refresh and retry.",
                        "CONCURRENCY_CONFLICT");
                }

                _dbContext.Entry(booking).Property(b => b.RowVersion).OriginalValue = decodedRowVersion;
            }

            var previousStatus = booking.Status;
            if (!IsAllowedStatusTransition(previousStatus, targetStatus))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_STATUS_TRANSITION",
                    $"Illegal workshop booking status transition from '{previousStatus}' to '{targetStatus}'.");
            }

            booking.Status = targetStatus;
            booking.UpdatedAtUtc = utcNow;

            switch (targetStatus)
            {
                case "CheckedIn":
                    booking.CheckedInAtUtc ??= utcNow;
                    break;

                case "InProgress":
                    booking.CheckedInAtUtc ??= utcNow;
                    break;

                case "Completed":
                    booking.CheckedInAtUtc ??= utcNow;
                    booking.CompletedAtUtc = utcNow;
                    var linkedReminders = await _dbContext.MaintenanceReminders
                        .Where(r => r.ResolvedBookingId == booking.Id && r.Status == "Booked")
                        .ToListAsync(ct);
                    foreach (var reminder in linkedReminders)
                    {
                        reminder.Status = "Completed";
                        reminder.UpdatedAtUtc = utcNow;
                    }
                    break;

                case "Cancelled":
                    booking.CancelledAtUtc = utcNow;
                    booking.CancellationReason = reasonOrNotes ?? "Cancelled by dealership workshop staff.";
                    await ReleaseSlotVoucherAndRemindersAsync(booking.Id, ctx.Slot, ctx.Voucher, utcNow, ct);
                    break;

                case "NoShow":
                    booking.CancellationReason = reasonOrNotes ?? "Marked as NoShow by dealership workshop staff.";
                    await ReleaseSlotVoucherAndRemindersAsync(booking.Id, ctx.Slot, ctx.Voucher, utcNow, ct);
                    break;
            }

            var dealerName = ctx.Dealer?.Name ?? "Authorized Dealership";
            var notification = CreateStatusChangedNotification(
                booking,
                previousStatus,
                targetStatus,
                dealerName,
                utcNow);

            _dbContext.Notifications.Add(notification);

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Concurrent update detected while updating booking status. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await transaction.CommitAsync(ct);

            var dto = MapToDto(
                booking,
                ctx.Customer,
                ctx.Dealer,
                ctx.Vehicle,
                ctx.Slot,
                ctx.Voucher);

            var realtimePayload = new BookingStatusChangedRealtimeEvent(
                NotificationId: notification.Id,
                EventType: "BookingStatusChanged",
                BookingId: booking.Id,
                BookingNumber: booking.BookingNumber,
                PreviousStatus: previousStatus,
                NewStatus: targetStatus,
                DealerName: dealerName,
                OccurredAtUtc: utcNow);

            return (dto, realtimePayload, booking.CustomerId, booking.DealerId);
        }, cancellationToken);

        await _notificationPublisher.PublishBookingStatusChangedAsync(
            statusEvent,
            customerId,
            dealerId,
            cancellationToken);

        return updatedDto;
    }

    private async Task<CustomerVoucher?> LockOptionalCustomerVoucherAsync(
        CreateServiceBookingRequest request,
        Guid customerId,
        byte[]? expectedVoucherRowVersion,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        CustomerVoucher? voucher = null;

        if (request.AppliedCustomerVoucherId is { } voucherId && voucherId != Guid.Empty)
        {
            voucher = await _dbContext.CustomerVouchers
                .IgnoreQueryFilters()
                .Include(v => v.LoyaltyAccount)
                .Include(v => v.VoucherCatalog)
                .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken)
                ?? throw new ConflictException(
                    "The selected voucher was not found or is not available.",
                    "VOUCHER_NOT_AVAILABLE");
        }
        else if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var normalizedCode = request.VoucherCode.Trim().ToUpperInvariant();
            voucher = await _dbContext.CustomerVouchers
                .IgnoreQueryFilters()
                .Include(v => v.LoyaltyAccount)
                .Include(v => v.VoucherCatalog)
                .FirstOrDefaultAsync(v => v.VoucherCode == normalizedCode, cancellationToken)
                ?? throw new ConflictException(
                    $"Voucher '{normalizedCode}' was not found or is not available.",
                    "VOUCHER_NOT_AVAILABLE");
        }

        if (voucher is null)
        {
            return null;
        }

        if (voucher.LoyaltyAccount is null || voucher.LoyaltyAccount.CustomerId != customerId)
        {
            throw new ForbiddenAccessException(
                "Cannot reserve a voucher belonging to another customer.",
                "FORBIDDEN_RESOURCE_OWNER");
        }

        if (voucher.VoucherCatalog is not null &&
            string.Equals(voucher.VoucherCatalog.ApplicableScope, "ShowroomOnly", StringComparison.Ordinal))
        {
            throw new BusinessRuleViolationException(
                "VOUCHER_INELIGIBLE",
                "Showroom-only vouchers cannot be reserved on workshop service bookings.");
        }

        if (!string.Equals(voucher.Status, "Available", StringComparison.Ordinal) ||
            voucher.ExpiresAtUtc <= utcNow)
        {
            throw new ConflictException(
                "The selected customer voucher is not available or has expired.",
                "VOUCHER_NOT_AVAILABLE");
        }

        if (expectedVoucherRowVersion is not null)
        {
            if (!voucher.RowVersion.AsSpan().SequenceEqual(expectedVoucherRowVersion))
            {
                throw new ConflictException(
                    "The selected voucher was modified concurrently. Please refresh and retry.",
                    "VOUCHER_NOT_AVAILABLE");
            }

            _dbContext.Entry(voucher).Property(v => v.RowVersion).OriginalValue = expectedVoucherRowVersion;
        }

        voucher.Status = "ReservedOnBooking";
        voucher.UpdatedAtUtc = utcNow;
        return voucher;
    }

    private sealed record LoadedBookingMutationContext(
        ServiceBooking Booking,
        User? Customer,
        Dealer? Dealer,
        CustomerVehicle? Vehicle,
        ServiceTimeSlot? Slot,
        CustomerVoucher? Voucher);

    private async Task<LoadedBookingMutationContext?> LoadBookingAggregateForMutationAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        // Note: Do NOT call IgnoreQueryFilters() on ServiceBookings here so IDealerScoped
        // automatically isolates DealerStaff callers to their own assigned DealerId!
        var booking = await _dbContext.ServiceBookings
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return null;
        }

        // Keep read-only reference aggregates detached from tracked navigation properties
        // so EF Core ChangeTracker never attempts to re-insert AsNoTracking lookup rows.
        var customer = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == booking.CustomerId, cancellationToken);

        var dealer = await _dbContext.Dealers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == booking.DealerId, cancellationToken);

        var vehicle = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .Include(v => v.MotorcycleModel)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == booking.CustomerVehicleId, cancellationToken);

        var slot = await _dbContext.ServiceTimeSlots
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == booking.ServiceTimeSlotId, cancellationToken);

        CustomerVoucher? voucher = null;
        if (booking.AppliedCustomerVoucherId.HasValue)
        {
            voucher = await _dbContext.CustomerVouchers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == booking.AppliedCustomerVoucherId.Value, cancellationToken);
        }

        return new LoadedBookingMutationContext(booking, customer, dealer, vehicle, slot, voucher);
    }

    private async Task ReleaseSlotVoucherAndRemindersAsync(
        Guid bookingId,
        ServiceTimeSlot? slot,
        CustomerVoucher? voucher,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (slot is not null)
        {
            slot.BookedCount = Math.Max(0, slot.BookedCount - 1);
            slot.UpdatedAtUtc = utcNow;
        }

        if (voucher is not null &&
            string.Equals(voucher.Status, "ReservedOnBooking", StringComparison.Ordinal))
        {
            voucher.Status = voucher.ExpiresAtUtc > utcNow ? "Available" : "Expired";
            voucher.UpdatedAtUtc = utcNow;
        }

        var linkedReminders = await _dbContext.MaintenanceReminders
            .Where(r => r.ResolvedBookingId == bookingId && r.Status == "Booked")
            .ToListAsync(cancellationToken);

        foreach (var reminder in linkedReminders)
        {
            reminder.ResolvedBookingId = null;
            reminder.Status = reminder.SentAtUtc.HasValue ? "Sent" : "Pending";
            reminder.UpdatedAtUtc = utcNow;
        }
    }

    private static bool IsAllowedStatusTransition(string currentStatus, string nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            ("Pending", "Confirmed" or "CheckedIn" or "Cancelled") => true,
            ("Confirmed", "CheckedIn" or "InProgress" or "Cancelled" or "NoShow") => true,
            ("CheckedIn", "InProgress" or "Completed") => true,
            ("InProgress", "Completed") => true,
            _ => false
        };

    private static Notification CreateStatusChangedNotification(
        ServiceBooking booking,
        string previousStatus,
        string newStatus,
        string dealerName,
        DateTimeOffset utcNow) =>
        new()
        {
            RecipientUserId = booking.CustomerId,
            TargetDealerId = booking.DealerId,
            EventType = "BookingStatusChanged",
            Title = $"Booking {booking.BookingNumber} — {newStatus}",
            Message = $"Your workshop appointment {booking.BookingNumber} at {dealerName} changed from {previousStatus} to {newStatus}.",
            PayloadJson = JsonSerializer.Serialize(new
            {
                bookingId = booking.Id,
                bookingNumber = booking.BookingNumber,
                previousStatus,
                newStatus,
                dealerId = booking.DealerId,
                dealerName
            }),
            IsRead = false,
            CreatedAtUtc = utcNow
        };

    private static string GenerateBookingNumber(DateTimeOffset utcNow) =>
        $"BK-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static BookingDto MapToDto(
        ServiceBooking booking,
        User? customer,
        Dealer? dealer,
        CustomerVehicle? vehicle,
        ServiceTimeSlot? slot,
        CustomerVoucher? voucher) =>
        new(
            Id: booking.Id,
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            Status: booking.Status,
            CustomerId: booking.CustomerId,
            CustomerFullName: customer?.FullName ?? string.Empty,
            CustomerEmail: customer?.Email ?? string.Empty,
            CustomerPhoneNumber: customer?.PhoneNumber ?? string.Empty,
            DealerId: booking.DealerId,
            DealerCode: dealer?.Code ?? string.Empty,
            DealerName: dealer?.Name ?? string.Empty,
            CustomerVehicleId: booking.CustomerVehicleId,
            Vin: vehicle?.VIN ?? string.Empty,
            MotorcycleModelName: vehicle?.MotorcycleModel?.ModelName ?? string.Empty,
            LicensePlate: vehicle?.LicensePlate,
            ServiceTimeSlotId: booking.ServiceTimeSlotId,
            SlotDate: slot?.SlotDate ?? default,
            StartTime: slot?.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "00:00:00",
            EndTime: slot?.EndTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "00:00:00",
            ServiceType: booking.ServiceType,
            OdometerAtBookingKm: booking.OdometerAtBookingKm,
            AppliedCustomerVoucherId: booking.AppliedCustomerVoucherId,
            ReservedVoucherCode: voucher?.VoucherCode,
            ReportedNotes: booking.ReportedNotes,
            CancellationReason: booking.CancellationReason,
            CheckedInAtUtc: booking.CheckedInAtUtc,
            CompletedAtUtc: booking.CompletedAtUtc,
            CancelledAtUtc: booking.CancelledAtUtc,
            RowVersion: RowVersionCodec.Encode(booking.RowVersion),
            CreatedAtUtc: booking.CreatedAtUtc,
            UpdatedAtUtc: booking.UpdatedAtUtc,
            MotorcycleModelId: vehicle?.MotorcycleModelId ?? Guid.Empty);
}
