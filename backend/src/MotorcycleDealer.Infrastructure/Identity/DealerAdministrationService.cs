using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Identity;

public sealed class DealerAdministrationService : IDealerAdministrationService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<UpsertDealerRequest> _upsertDealerValidator;
    private readonly IValidator<CreateDealerStaffRequest> _createStaffValidator;
    private readonly IValidator<UpdateStaffStatusRequest> _updateStaffStatusValidator;

    public DealerAdministrationService(
        MotorcycleDealerDbContext dbContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IValidator<UpsertDealerRequest> upsertDealerValidator,
        IValidator<CreateDealerStaffRequest> createStaffValidator,
        IValidator<UpdateStaffStatusRequest> updateStaffStatusValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _upsertDealerValidator = upsertDealerValidator ?? throw new ArgumentNullException(nameof(upsertDealerValidator));
        _createStaffValidator = createStaffValidator ?? throw new ArgumentNullException(nameof(createStaffValidator));
        _updateStaffStatusValidator = updateStaffStatusValidator ?? throw new ArgumentNullException(nameof(updateStaffStatusValidator));
    }

    public async Task<IReadOnlyList<DealerDto>> ListDealersAsync(
        string? city,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Dealers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim();
            query = query.Where(d => d.City == trimmedCity);
        }

        var dealers = await query
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        if (dealers.Count == 0)
        {
            return [];
        }

        var dealerIds = dealers.Select(d => d.Id).ToArray();
        var availableCounts = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(inv => dealerIds.Contains(inv.DealerId) && inv.StockStatus == "Available")
            .GroupBy(inv => inv.DealerId)
            .Select(g => new { DealerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DealerId, x => x.Count, cancellationToken);

        return dealers
            .Select(d => MapToDealerDto(d, availableCounts.GetValueOrDefault(d.Id, 0)))
            .ToList();
    }

    public async Task<DealerDto> GetDealerByIdAsync(
        Guid dealerId,
        bool allowInactive,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken);

        if (dealer is null || (!allowInactive && !dealer.IsActive))
        {
            throw NotFoundException.ForEntity("Dealer", dealerId);
        }

        var availableCount = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(inv => inv.DealerId == dealerId && inv.StockStatus == "Available", cancellationToken);

        return MapToDealerDto(dealer, availableCount);
    }

    public async Task<DealerDto> CreateDealerAsync(
        UpsertDealerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertDealerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeExists = await _dbContext.Dealers
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Code == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"Dealership branch code '{normalizedCode}' already exists.",
                "DUPLICATE_DEALER_CODE");
        }

        DealerValidationPatterns.TryParseWorkshopTime(request.OpeningTime, out var openingTime);
        DealerValidationPatterns.TryParseWorkshopTime(request.ClosingTime, out var closingTime);

        var dealer = new Dealer
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OpeningTime = openingTime,
            ClosingTime = closingTime,
            DefaultSlotCapacity = request.DefaultSlotCapacity,
            IsActive = request.IsActive
        };

        _dbContext.Dealers.Add(dealer);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException(
                $"Dealership branch code '{normalizedCode}' already exists.",
                "DUPLICATE_DEALER_CODE");
        }

        return MapToDealerDto(dealer, availableVinCount: 0);
    }

    public async Task<DealerDto> UpdateDealerAsync(
        Guid dealerId,
        UpsertDealerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertDealerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var dealer = await _dbContext.Dealers
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Dealer", dealerId);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (!string.Equals(dealer.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            var duplicateCode = await _dbContext.Dealers
                .IgnoreQueryFilters()
                .AnyAsync(d => d.Id != dealerId && d.Code == normalizedCode, cancellationToken);

            if (duplicateCode)
            {
                throw new ConflictException(
                    $"Dealership branch code '{normalizedCode}' already exists.",
                    "DUPLICATE_DEALER_CODE");
            }
        }

        var wasActive = dealer.IsActive;
        DealerValidationPatterns.TryParseWorkshopTime(request.OpeningTime, out var openingTime);
        DealerValidationPatterns.TryParseWorkshopTime(request.ClosingTime, out var closingTime);

        dealer.Code = normalizedCode;
        dealer.Name = request.Name.Trim();
        dealer.City = request.City.Trim();
        dealer.Address = request.Address.Trim();
        dealer.PhoneNumber = request.PhoneNumber.Trim();
        dealer.ContactEmail = request.ContactEmail.Trim();
        dealer.Latitude = request.Latitude;
        dealer.Longitude = request.Longitude;
        dealer.OpeningTime = openingTime;
        dealer.ClosingTime = closingTime;
        dealer.DefaultSlotCapacity = request.DefaultSlotCapacity;
        dealer.IsActive = request.IsActive;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException(
                $"Dealership branch code '{normalizedCode}' already exists.",
                "DUPLICATE_DEALER_CODE");
        }

        if (wasActive && !dealer.IsActive)
        {
            var utcNow = _dateTimeProvider.UtcNow;
            await _dbContext.RefreshTokens
                .Where(rt => rt.RevokedAtUtc == null &&
                             rt.User != null &&
                             rt.User.DealerId == dealerId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                        .SetProperty(rt => rt.RevocationReason, "DealerDeactivated"),
                    cancellationToken);
        }

        var availableCount = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(inv => inv.DealerId == dealerId && inv.StockStatus == "Available", cancellationToken);

        return MapToDealerDto(dealer, availableCount);
    }

    public async Task<IReadOnlyList<DealerStaffDto>> ListDealerStaffAsync(
        Guid dealerId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Dealer", dealerId);

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.DealerId == dealerId &&
                        u.StaffEmployeeCode != null &&
                        u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == RoleNames.DealerStaff));

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var staffUsers = await query
            .OrderBy(u => u.StaffEmployeeCode)
            .ToListAsync(cancellationToken);

        return staffUsers
            .Select(u => MapToDealerStaffDto(u, dealer))
            .ToList();
    }

    public async Task<DealerStaffDto> ProvisionDealerStaffAsync(
        Guid dealerId,
        CreateDealerStaffRequest request,
        Guid assignedByAdminUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createStaffValidator.ValidateAndThrowAsync(request, cancellationToken);

        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Dealer", dealerId);

        if (!dealer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "DEALER_INACTIVE",
                $"Cannot provision DealerStaff for inactive branch '{dealer.Code}'.");
        }

        var trimmedEmail = request.Email.Trim();
        var normalizedEmail = User.NormalizeEmailValue(trimmedEmail);
        var trimmedPhone = request.PhoneNumber.Trim();
        var normalizedEmployeeCode = request.StaffEmployeeCode.Trim().ToUpperInvariant();

        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail || u.Email == trimmedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "An account with this email address already exists.",
                "DUPLICATE_EMAIL");
        }

        var phoneExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.PhoneNumber == trimmedPhone, cancellationToken);

        if (phoneExists)
        {
            throw new ConflictException(
                "An account with this phone number already exists.",
                "DUPLICATE_PHONE");
        }

        var codeExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.StaffEmployeeCode == normalizedEmployeeCode, cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"Staff employee badge '{normalizedEmployeeCode}' is already assigned.",
                "DUPLICATE_EMPLOYEE_CODE");
        }

        var staffRole = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == RoleNames.DealerStaff, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                "SYSTEM_SEED_MISSING",
                "Canonical 'DealerStaff' role is missing from database seed.");

        var passwordHash = _passwordHasher.HashPassword(request.ResolveEffectivePassword());
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var utcNow = _dateTimeProvider.UtcNow;

                var staffUser = User.CreateDealerStaff(
                    email: trimmedEmail,
                    passwordHash: passwordHash,
                    fullName: request.FullName.Trim(),
                    phoneNumber: trimmedPhone,
                    dealerId: dealer.Id,
                    staffEmployeeCode: normalizedEmployeeCode);

                var userRole = new UserRole
                {
                    UserId = staffUser.Id,
                    RoleId = staffRole.Id,
                    AssignedAtUtc = utcNow,
                    AssignedByUserId = assignedByAdminUserId != Guid.Empty ? assignedByAdminUserId : null
                };

                _dbContext.Users.Add(staffUser);
                _dbContext.UserRoles.Add(userRole);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return MapToDealerStaffDto(staffUser, dealer);
            });
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException(
                "A user with the supplied email, phone number, or employee code already exists.",
                "DUPLICATE_ACCOUNT");
        }
    }

    public async Task<DealerStaffDto> UpdateDealerStaffStatusAsync(
        Guid dealerId,
        Guid staffUserId,
        UpdateStaffStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateStaffStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Dealer", dealerId);

        var staffUser = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.Id == staffUserId && u.DealerId == dealerId,
                cancellationToken);

        if (staffUser is null ||
            string.IsNullOrWhiteSpace(staffUser.StaffEmployeeCode) ||
            !staffUser.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == RoleNames.DealerStaff))
        {
            throw NotFoundException.ForEntity("DealerStaff", staffUserId);
        }

        var utcNow = _dateTimeProvider.UtcNow;

        if (request.IsActive == true)
        {
            if (!dealer.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "DEALER_INACTIVE",
                    $"Cannot activate staff member while branch '{dealer.Code}' is inactive.");
            }

            staffUser.Activate(utcNow);
        }
        else
        {
            staffUser.Deactivate(utcNow);

            await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == staffUser.Id && rt.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                        .SetProperty(rt => rt.RevocationReason, "AccountDisabled"),
                    cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDealerStaffDto(staffUser, dealer);
    }

    private static DealerDto MapToDealerDto(Dealer dealer, int availableVinCount) =>
        new(
            Id: dealer.Id,
            Code: dealer.Code,
            Name: dealer.Name,
            City: dealer.City,
            Address: dealer.Address,
            PhoneNumber: dealer.PhoneNumber,
            ContactEmail: dealer.ContactEmail,
            Latitude: dealer.Latitude,
            Longitude: dealer.Longitude,
            OpeningTime: dealer.OpeningTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            ClosingTime: dealer.ClosingTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            DefaultSlotCapacity: dealer.DefaultSlotCapacity,
            AvailableVinCount: availableVinCount,
            IsActive: dealer.IsActive,
            CreatedAtUtc: dealer.CreatedAtUtc);

    private static DealerStaffDto MapToDealerStaffDto(User user, Dealer dealer) =>
        new(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            DealerId: dealer.Id,
            DealerCode: dealer.Code,
            DealerName: dealer.Name,
            StaffEmployeeCode: user.StaffEmployeeCode ?? string.Empty,
            IsActive: user.IsActive,
            CreatedAtUtc: user.CreatedAtUtc,
            LastLoginAtUtc: user.LastLoginAtUtc);
}
