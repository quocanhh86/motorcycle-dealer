using System.Buffers.Binary;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Loyalty;

public sealed class LoyaltyService : ILoyaltyService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<UpdateLoyaltyTierRequest> _updateTierValidator;
    private readonly IValidator<PointLedgerQuery> _ledgerQueryValidator;
    private readonly IValidator<AdminPointAdjustmentRequest> _adjustmentValidator;

    public LoyaltyService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<UpdateLoyaltyTierRequest> updateTierValidator,
        IValidator<PointLedgerQuery> ledgerQueryValidator,
        IValidator<AdminPointAdjustmentRequest> adjustmentValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _updateTierValidator = updateTierValidator ?? throw new ArgumentNullException(nameof(updateTierValidator));
        _ledgerQueryValidator = ledgerQueryValidator ?? throw new ArgumentNullException(nameof(ledgerQueryValidator));
        _adjustmentValidator = adjustmentValidator ?? throw new ArgumentNullException(nameof(adjustmentValidator));
    }

    public async Task<IReadOnlyList<LoyaltyTierDto>> ListTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var tiers = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.RankOrder)
            .ToListAsync(cancellationToken);

        return tiers.Select(MapTierToDto).ToList();
    }

    public async Task<LoyaltyTierDto> UpdateTierAsync(
        Guid tierId,
        UpdateLoyaltyTierRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateTierValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ManageTiers))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage loyalty tier configurations.",
                "FORBIDDEN_PERMISSION");
        }

        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.RowVersion);
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var utcNow = _dateTimeProvider.UtcNow;
                var allTiers = await _dbContext.LoyaltyTiers
                    .OrderBy(t => t.RankOrder)
                    .ToListAsync(ct);

                var targetTier = allTiers.FirstOrDefault(t => t.Id == tierId)
                                 ?? throw NotFoundException.ForEntity("LoyaltyTier", tierId);

                if (expectedRowVersion is not null)
                {
                    var currentTokenBytes = ComputeTierConcurrencyBytes(targetTier);
                    if (!currentTokenBytes.AsSpan().SequenceEqual(expectedRowVersion))
                    {
                        throw new ConflictException(
                            "The loyalty tier configuration was modified concurrently. Please refresh and retry.",
                            "CONCURRENCY_CONFLICT");
                    }
                }

                var effectiveMinPoints = request.MinQualifyingPoints ?? targetTier.MinQualifyingPoints;
                if (effectiveMinPoints < 0)
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_TIER_THRESHOLD",
                        "Tier minimum qualifying points cannot be negative.");
                }

                if ((string.Equals(targetTier.TierCode, "Bronze", StringComparison.Ordinal) || targetTier.RankOrder == 1) &&
                    effectiveMinPoints != 0)
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_TIER_THRESHOLD",
                        "Bronze tier minimum qualifying points must remain 0.");
                }

                for (var i = 0; i < allTiers.Count - 1; i++)
                {
                    var leftTier = allTiers[i];
                    var rightTier = allTiers[i + 1];
                    var leftThreshold = leftTier.Id == tierId ? effectiveMinPoints : leftTier.MinQualifyingPoints;
                    var rightThreshold = rightTier.Id == tierId ? effectiveMinPoints : rightTier.MinQualifyingPoints;

                    if (leftThreshold >= rightThreshold)
                    {
                        throw new BusinessRuleViolationException(
                            "INVALID_TIER_THRESHOLD",
                            $"Loyalty tier thresholds must maintain strict monotonic order ({leftTier.TierCode}: {leftThreshold} must be strictly less than {rightTier.TierCode}: {rightThreshold}).");
                    }
                }

                targetTier.DisplayName = request.DisplayName.Trim();
                targetTier.MinQualifyingPoints = effectiveMinPoints;

                if (request.PointsMultiplier.HasValue)
                {
                    targetTier.PointsMultiplier = Math.Round(
                        request.PointsMultiplier.Value,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                if (request.BenefitsJson is not null)
                {
                    targetTier.BenefitsJson = request.BenefitsJson.Trim();
                }

                targetTier.UpdatedAtUtc = utcNow;

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapTierToDto(targetTier);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg) &&
                                           sqlMsg.Contains("UQ_LoyaltyTiers_MinPoints", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleViolationException(
                "INVALID_TIER_THRESHOLD",
                "Another loyalty tier already uses the specified minimum qualifying points threshold.");
        }
    }

    public async Task<LoyaltyAccountSummaryDto> GetMyLoyaltySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ViewOwn))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view a personal loyalty wallet.",
                "FORBIDDEN_PERMISSION");
        }

        var customer = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == callerUserId && u.IsActive && !u.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated customer profile is missing or inactive.");

        var tiers = await LoadOrderedActiveTiersAsync(cancellationToken);
        var account = await EnsureLoyaltyAccountExistsAsync(customer.Id, tiers, cancellationToken);
        var utcNow = _dateTimeProvider.UtcNow;

        var voucherSnapshots = await _dbContext.CustomerVouchers
            .AsNoTracking()
            .Where(v => v.LoyaltyAccountId == account.Id)
            .Select(v => new VoucherStatusSnapshot(v.Status, v.ExpiresAtUtc))
            .ToListAsync(cancellationToken);

        return BuildAccountSummaryDto(account, customer, tiers, voucherSnapshots, utcNow);
    }

    public async Task<PagedResult<PointLedgerEntryDto>> ListMyPointLedgerAsync(
        PointLedgerQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _ledgerQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ViewOwn))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view loyalty ledger entries.",
                "FORBIDDEN_PERMISSION");
        }

        var accountId = await _dbContext.LoyaltyAccounts
            .AsNoTracking()
            .Where(a => a.CustomerId == callerUserId)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (accountId is null)
        {
            return PagedResult<PointLedgerEntryDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var ledgerQuery = _dbContext.PointLedgerEntries
            .AsNoTracking()
            .Where(e => e.LoyaltyAccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(query.TransactionType) &&
            LoyaltyValidationRules.TryNormalizeTransactionType(query.TransactionType, out var normalizedTxType))
        {
            ledgerQuery = ledgerQuery.Where(e => e.TransactionType == normalizedTxType);
        }

        var totalCount = await ledgerQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<PointLedgerEntryDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var dtos = await ledgerQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .Select(e => new PointLedgerEntryDto(
                e.Id,
                e.Id,
                e.LoyaltyAccountId,
                e.TransactionType,
                e.PointsDelta,
                e.BalanceAfter,
                e.ReferenceType,
                e.ReferenceId,
                e.IdempotencyKey,
                e.Notes,
                e.CreatedByUserId,
                e.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return PagedResult<PointLedgerEntryDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<CustomerLoyaltyInspectionDto> GetCustomerLoyaltyForInspectionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ViewDealerCustomer))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to inspect customer loyalty profiles.",
                "FORBIDDEN_PERMISSION");
        }

        if (_currentUserContext.IsDealerStaff)
        {
            _ = _currentUserContext.RequireAssignedDealerId();
        }

        var customer = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == customerId && !u.IsDeleted, cancellationToken)
            ?? throw NotFoundException.ForEntity("Customer", customerId);

        var holdsCustomerRole = customer.UserRoles.Any(ur =>
            ur.Role != null && string.Equals(ur.Role.Name, RoleNames.Customer, StringComparison.Ordinal));

        if (!holdsCustomerRole)
        {
            throw NotFoundException.ForEntity("Customer", customerId);
        }

        var tiers = await LoadOrderedActiveTiersAsync(cancellationToken);
        var account = await EnsureLoyaltyAccountExistsAsync(customer.Id, tiers, cancellationToken);
        var utcNow = _dateTimeProvider.UtcNow;

        var allCustomerVouchers = await _dbContext.CustomerVouchers
            .AsNoTracking()
            .Include(v => v.VoucherCatalog)
            .Where(v => v.LoyaltyAccountId == account.Id)
            .OrderBy(v => v.ExpiresAtUtc)
            .ThenByDescending(v => v.RedeemedAtUtc)
            .ToListAsync(cancellationToken);

        var voucherSnapshots = allCustomerVouchers
            .Select(v => new VoucherStatusSnapshot(v.Status, v.ExpiresAtUtc))
            .ToList();

        var summaryDto = BuildAccountSummaryDto(account, customer, tiers, voucherSnapshots, utcNow);

        var eligibleVoucherDtos = allCustomerVouchers
            .Where(v =>
                v.ExpiresAtUtc > utcNow &&
                (string.Equals(v.Status, "Available", StringComparison.Ordinal) ||
                 string.Equals(v.Status, "ReservedOnBooking", StringComparison.Ordinal)))
            .Select(v => VoucherService.MapCustomerVoucherToDto(
                v,
                utcNow,
                account.AvailablePoints,
                account.LifetimeQualifyingPoints))
            .ToList();

        var serviceVoucherDtos = eligibleVoucherDtos
            .Where(v =>
                string.Equals(v.ApplicableScope, "ServiceOnly", StringComparison.Ordinal) ||
                string.Equals(v.ApplicableScope, "All", StringComparison.Ordinal))
            .ToList();

        var recentLedgerRows = await _dbContext.PointLedgerEntries
            .AsNoTracking()
            .Where(e => e.LoyaltyAccountId == account.Id)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ThenByDescending(e => e.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentLedgerDtos = recentLedgerRows.Select(MapLedgerEntryToDto).ToList();

        return new CustomerLoyaltyInspectionDto(
            CustomerId: customer.Id,
            CustomerFullName: customer.FullName,
            CustomerEmail: customer.Email,
            CustomerPhoneNumber: customer.PhoneNumber,
            AccountId: summaryDto.AccountId,
            TierCode: summaryDto.TierCode,
            CurrentTierCode: summaryDto.CurrentTierCode,
            PointsMultiplier: summaryDto.PointsMultiplier,
            TierMultiplier: summaryDto.TierMultiplier,
            AvailablePoints: summaryDto.AvailablePoints,
            LifetimeQualifyingPoints: summaryDto.LifetimeQualifyingPoints,
            LifetimeSpentVnd: summaryDto.LifetimeSpentVnd,
            NextTierCode: summaryDto.NextTierCode,
            PointsToNextTier: summaryDto.PointsToNextTier,
            ProgressPercent: summaryDto.ProgressPercent,
            RowVersion: summaryDto.RowVersion,
            Account: summaryDto,
            EligibleVouchers: eligibleVoucherDtos,
            AvailableServiceVouchers: serviceVoucherDtos,
            RecentLedgerEntries: recentLedgerDtos);
    }

    public async Task<LoyaltyAdjustmentResultDto> AdjustCustomerPointsAsync(
        AdminPointAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _adjustmentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var adminUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.AdjustPoints))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to execute manual loyalty point adjustments.",
                "FORBIDDEN_PERMISSION");
        }

        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.ResolveEffectiveRowVersion());
        var effectiveNotes = request.ResolveEffectiveNotes();
        var qualifiesForTier = request.ResolveQualifiesForTier();
        var explicitIdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        LoyaltyPointsUpdatedRealtimeEvent? postCommitPointsEvent = null;
        TierUpgradedRealtimeEvent? postCommitTierEvent = null;
        var postCommitCustomerId = Guid.Empty;

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        LoyaltyAdjustmentResultDto resultDto;

        try
        {
            resultDto = await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                postCommitPointsEvent = null;
                postCommitTierEvent = null;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                var utcNow = _dateTimeProvider.UtcNow;

                if (explicitIdempotencyKey is not null)
                {
                    var keyAlreadyUsed = await _dbContext.PointLedgerEntries
                        .IgnoreQueryFilters()
                        .AnyAsync(p => p.IdempotencyKey == explicitIdempotencyKey, ct);

                    if (keyAlreadyUsed)
                    {
                        throw new ConflictException(
                            $"Point adjustment with IdempotencyKey '{explicitIdempotencyKey}' has already been recorded.",
                            "DUPLICATE_IDEMPOTENCY_KEY");
                    }
                }

                var tiers = await _dbContext.LoyaltyTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.RankOrder)
                    .ToListAsync(ct);

                var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                                 ?? tiers.FirstOrDefault()
                                 ?? throw new BusinessRuleViolationException(
                                     "SYSTEM_SEED_MISSING",
                                     "Canonical LoyaltyTiers seed data is missing.");

                LoyaltyAccount? account = null;
                User? customer = null;

                if (request.ResolveTargetAccountId() is { } targetAccountId)
                {
                    account = await _dbContext.LoyaltyAccounts
                        .Include(a => a.CurrentTier)
                        .Include(a => a.Customer)
                            .ThenInclude(u => u!.UserRoles)
                                .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(a => a.Id == targetAccountId, ct)
                        ?? throw NotFoundException.ForEntity("LoyaltyAccount", targetAccountId);

                    customer = account.Customer
                               ?? throw NotFoundException.ForEntity("Customer", account.CustomerId);

                    if (customer.IsDeleted)
                    {
                        throw NotFoundException.ForEntity("Customer", account.CustomerId);
                    }
                }
                else
                {
                    if (request.CustomerId is { } reqCustId && reqCustId != Guid.Empty)
                    {
                        customer = await _dbContext.Users
                            .Include(u => u.UserRoles)
                                .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.Id == reqCustId && !u.IsDeleted, ct);
                    }
                    else if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
                    {
                        var normalizedEmail = User.NormalizeEmailValue(request.CustomerEmail);
                        customer = await _dbContext.Users
                            .Include(u => u.UserRoles)
                                .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);
                    }

                    if (customer is null)
                    {
                        throw NotFoundException.ForEntity(
                            "Customer",
                            request.CustomerId is { } cid && cid != Guid.Empty
                                ? cid
                                : request.CustomerEmail ?? "unknown");
                    }

                    account = await _dbContext.LoyaltyAccounts
                        .Include(a => a.CurrentTier)
                        .FirstOrDefaultAsync(a => a.CustomerId == customer.Id, ct);

                    if (account is null)
                    {
                        account = new LoyaltyAccount(Guid.CreateVersion7())
                        {
                            CustomerId = customer.Id,
                            CurrentTierId = bronzeTier.Id,
                            CurrentTier = bronzeTier,
                            AvailablePoints = 0,
                            LifetimeQualifyingPoints = 0,
                            LifetimeSpentVnd = 0m,
                            CreatedAtUtc = utcNow
                        };
                        _dbContext.LoyaltyAccounts.Add(account);
                    }
                }

                if (!customer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "CUSTOMER_INACTIVE",
                        "Cannot adjust loyalty points for a deactivated customer account.");
                }

                var holdsCustomerRole = customer.UserRoles.Any(ur =>
                    ur.Role != null && string.Equals(ur.Role.Name, RoleNames.Customer, StringComparison.Ordinal));

                if (!holdsCustomerRole)
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_CUSTOMER_ROLE",
                        "Manual loyalty point adjustments apply exclusively to Customer accounts.");
                }

                if (expectedRowVersion is not null)
                {
                    if (!account.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                    {
                        throw new ConflictException(
                            "The customer's loyalty wallet was modified concurrently. Please refresh and retry.",
                            "CONCURRENCY_CONFLICT");
                    }

                    _dbContext.Entry(account).Property(a => a.RowVersion).OriginalValue = expectedRowVersion;
                }

                var updatedAvailablePoints = checked(account.AvailablePoints + request.PointsDelta);
                if (updatedAvailablePoints < 0)
                {
                    throw new BusinessRuleViolationException(
                        "INSUFFICIENT_POINTS",
                        $"Insufficient spendable points ({account.AvailablePoints}) to deduct {Math.Abs(request.PointsDelta)} points.");
                }

                var previousTier = account.CurrentTier
                                   ?? tiers.FirstOrDefault(t => t.Id == account.CurrentTierId)
                                   ?? bronzeTier;

                account.AvailablePoints = updatedAvailablePoints;
                account.UpdatedAtUtc = utcNow;

                var resultingTier = previousTier;
                var tierUpgraded = false;

                if (request.PointsDelta > 0 && qualifiesForTier)
                {
                    account.LifetimeQualifyingPoints = checked(account.LifetimeQualifyingPoints + request.PointsDelta);

                    var candidateTier = tiers
                        .Where(t => t.MinQualifyingPoints <= account.LifetimeQualifyingPoints)
                        .OrderByDescending(t => t.RankOrder)
                        .FirstOrDefault();

                    if (candidateTier is not null && candidateTier.RankOrder > previousTier.RankOrder)
                    {
                        resultingTier = candidateTier;
                        account.CurrentTierId = resultingTier.Id;
                        account.CurrentTier = resultingTier;
                        account.TierUpgradedAtUtc = utcNow;
                        tierUpgraded = true;
                    }
                }

                var ledgerEntryId = Guid.CreateVersion7();
                var effectiveIdempotencyKey = explicitIdempotencyKey ?? $"ADMIN_ADJUST:{ledgerEntryId:D}";
                var referenceNumber = $"ADJ-{ledgerEntryId.ToString("N")[^6..].ToUpperInvariant()}";

                var ledgerEntry = new PointLedgerEntry(ledgerEntryId)
                {
                    LoyaltyAccountId = account.Id,
                    TransactionType = "AdminAdjustment",
                    PointsDelta = request.PointsDelta,
                    BalanceAfter = account.AvailablePoints,
                    ReferenceType = "AdminManual",
                    ReferenceId = ledgerEntryId,
                    IdempotencyKey = effectiveIdempotencyKey,
                    Notes = effectiveNotes,
                    CreatedByUserId = adminUserId,
                    CreatedAtUtc = utcNow
                };
                _dbContext.PointLedgerEntries.Add(ledgerEntry);

                var pointsNotifId = Guid.CreateVersion7();
                var signedLabel = request.PointsDelta > 0
                    ? $"+{request.PointsDelta:N0}"
                    : $"{request.PointsDelta:N0}";

                var pointsPayload = new LoyaltyPointsUpdatedRealtimeEvent(
                    NotificationId: pointsNotifId,
                    EventType: "LoyaltyPointsUpdated",
                    LoyaltyAccountId: account.Id,
                    TransactionType: "AdminAdjustment",
                    PointsDelta: request.PointsDelta,
                    AvailablePointsAfter: account.AvailablePoints,
                    LifetimeQualifyingPointsAfter: account.LifetimeQualifyingPoints,
                    ReferenceType: "AdminManual",
                    ReferenceNumber: referenceNumber,
                    OccurredAtUtc: utcNow);

                _dbContext.Notifications.Add(new Notification(pointsNotifId)
                {
                    RecipientUserId = customer.Id,
                    EventType = "LoyaltyPointsUpdated",
                    Title = $"Loyalty Points Adjusted ({signedLabel} pts)",
                    Message = $"Administrator recorded a {signedLabel} point adjustment ({referenceNumber}): {effectiveNotes}",
                    PayloadJson = JsonSerializer.Serialize(pointsPayload),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                postCommitCustomerId = customer.Id;
                postCommitPointsEvent = pointsPayload;

                if (tierUpgraded)
                {
                    var tierNotifId = Guid.CreateVersion7();
                    var tierPayload = new TierUpgradedRealtimeEvent(
                        NotificationId: tierNotifId,
                        EventType: "TierUpgraded",
                        LoyaltyAccountId: account.Id,
                        PreviousTierCode: previousTier.TierCode,
                        NewTierCode: resultingTier.TierCode,
                        NewPointsMultiplier: resultingTier.PointsMultiplier,
                        LifetimeQualifyingPoints: account.LifetimeQualifyingPoints,
                        OccurredAtUtc: utcNow);

                    _dbContext.Notifications.Add(new Notification(tierNotifId)
                    {
                        RecipientUserId = customer.Id,
                        EventType = "TierUpgraded",
                        Title = $"Congratulations! Upgraded to {resultingTier.DisplayName}",
                        Message = $"You unlocked {resultingTier.TierCode} tier ({resultingTier.PointsMultiplier:0.00}x points multiplier) with {account.LifetimeQualifyingPoints:N0} lifetime qualifying points.",
                        PayloadJson = JsonSerializer.Serialize(tierPayload),
                        IsRead = false,
                        CreatedAtUtc = utcNow
                    });

                    postCommitTierEvent = tierPayload;
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new LoyaltyAdjustmentResultDto(
                    Id: ledgerEntry.Id,
                    LedgerEntryId: ledgerEntry.Id,
                    LoyaltyAccountId: account.Id,
                    CustomerId: customer.Id,
                    TransactionType: ledgerEntry.TransactionType,
                    PointsDelta: ledgerEntry.PointsDelta,
                    BalanceAfter: ledgerEntry.BalanceAfter,
                    AvailablePointsAfter: account.AvailablePoints,
                    LifetimeQualifyingPointsAfter: account.LifetimeQualifyingPoints,
                    PreviousTierCode: previousTier.TierCode,
                    CurrentTierCode: resultingTier.TierCode,
                    NewTierCode: resultingTier.TierCode,
                    PointsMultiplier: resultingTier.PointsMultiplier,
                    TierUpgraded: tierUpgraded,
                    ReferenceType: ledgerEntry.ReferenceType,
                    ReferenceId: ledgerEntry.ReferenceId,
                    IdempotencyKey: ledgerEntry.IdempotencyKey,
                    Notes: effectiveNotes,
                    CreatedByUserId: adminUserId,
                    CreatedAtUtc: ledgerEntry.CreatedAtUtc,
                    RowVersion: RowVersionCodec.Encode(account.RowVersion));
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The customer's loyalty wallet was modified concurrently by another transaction. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg) &&
                                           sqlMsg.Contains("UQ_PointLedgerEntries_IdempotencyKey", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "Duplicate IdempotencyKey detected for manual point adjustment.",
                "DUPLICATE_IDEMPOTENCY_KEY");
        }

        if (postCommitPointsEvent is not null)
        {
            await _notificationPublisher.PublishLoyaltyPointsUpdatedAsync(
                postCommitPointsEvent,
                postCommitCustomerId,
                cancellationToken);
        }

        if (postCommitTierEvent is not null)
        {
            await _notificationPublisher.PublishTierUpgradedAsync(
                postCommitTierEvent,
                postCommitCustomerId,
                cancellationToken);
        }

        return resultDto;
    }

    private async Task<List<LoyaltyTier>> LoadOrderedActiveTiersAsync(CancellationToken cancellationToken)
    {
        var tiers = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.RankOrder)
            .ToListAsync(cancellationToken);

        if (tiers.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "SYSTEM_SEED_MISSING",
                "Canonical LoyaltyTiers seed data is missing.");
        }

        return tiers;
    }

    private async Task<LoyaltyAccount> EnsureLoyaltyAccountExistsAsync(
        Guid customerId,
        IReadOnlyList<LoyaltyTier> orderedTiers,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.LoyaltyAccounts
            .AsNoTracking()
            .Include(a => a.CurrentTier)
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var bronzeTier = orderedTiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                         ?? orderedTiers[0];

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                var recheck = await _dbContext.LoyaltyAccounts
                    .AsNoTracking()
                    .Include(a => a.CurrentTier)
                    .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

                if (recheck is not null)
                {
                    return recheck;
                }

                var utcNow = _dateTimeProvider.UtcNow;
                var created = new LoyaltyAccount(Guid.CreateVersion7())
                {
                    CustomerId = customerId,
                    CurrentTierId = bronzeTier.Id,
                    AvailablePoints = 0,
                    LifetimeQualifyingPoints = 0,
                    LifetimeSpentVnd = 0m,
                    CreatedAtUtc = utcNow
                };

                _dbContext.LoyaltyAccounts.Add(created);
                await _dbContext.SaveChangesAsync(ct);

                created.CurrentTier = bronzeTier;
                return created;
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            return await _dbContext.LoyaltyAccounts
                .AsNoTracking()
                .Include(a => a.CurrentTier)
                .FirstAsync(a => a.CustomerId == customerId, cancellationToken);
        }
    }

    private static LoyaltyAccountSummaryDto BuildAccountSummaryDto(
        LoyaltyAccount account,
        User customer,
        IReadOnlyList<LoyaltyTier> orderedTiers,
        IReadOnlyList<VoucherStatusSnapshot> vouchers,
        DateTimeOffset utcNow)
    {
        var currentTier = orderedTiers.FirstOrDefault(t => t.Id == account.CurrentTierId)
                          ?? account.CurrentTier
                          ?? orderedTiers[0];

        var nextTier = orderedTiers.FirstOrDefault(t => t.RankOrder > currentTier.RankOrder);

        int pointsToNextTier;
        decimal progressPercent;

        if (nextTier is null)
        {
            pointsToNextTier = 0;
            progressPercent = 100.00m;
        }
        else
        {
            pointsToNextTier = Math.Max(0, nextTier.MinQualifyingPoints - account.LifetimeQualifyingPoints);
            var bandSpan = Math.Max(1, nextTier.MinQualifyingPoints - currentTier.MinQualifyingPoints);
            var bandProgress = Math.Clamp(
                account.LifetimeQualifyingPoints - currentTier.MinQualifyingPoints,
                0,
                bandSpan);
            progressPercent = Math.Round(
                bandProgress * 100m / bandSpan,
                2,
                MidpointRounding.AwayFromZero);
        }

        var availableVouchers = 0;
        var reservedVouchers = 0;
        var usedVouchers = 0;
        var expiredVouchers = 0;

        foreach (var v in vouchers)
        {
            if (string.Equals(v.Status, "Used", StringComparison.Ordinal))
            {
                usedVouchers++;
            }
            else if (string.Equals(v.Status, "ReservedOnBooking", StringComparison.Ordinal))
            {
                reservedVouchers++;
            }
            else if (string.Equals(v.Status, "Expired", StringComparison.Ordinal) ||
                     (string.Equals(v.Status, "Available", StringComparison.Ordinal) && v.ExpiresAtUtc <= utcNow))
            {
                expiredVouchers++;
            }
            else if (string.Equals(v.Status, "Available", StringComparison.Ordinal))
            {
                availableVouchers++;
            }
        }

        return new LoyaltyAccountSummaryDto(
            Id: account.Id,
            AccountId: account.Id,
            CustomerId: customer.Id,
            CustomerFullName: customer.FullName,
            CustomerEmail: customer.Email,
            TierId: currentTier.Id,
            TierCode: currentTier.TierCode,
            CurrentTierCode: currentTier.TierCode,
            TierDisplayName: currentTier.DisplayName,
            RankOrder: currentTier.RankOrder,
            PointsMultiplier: currentTier.PointsMultiplier,
            TierMultiplier: currentTier.PointsMultiplier,
            AvailablePoints: account.AvailablePoints,
            LifetimeQualifyingPoints: account.LifetimeQualifyingPoints,
            LifetimeSpentVnd: account.LifetimeSpentVnd,
            NextTierCode: nextTier?.TierCode,
            NextTierDisplayName: nextTier?.DisplayName,
            NextTierMinPoints: nextTier?.MinQualifyingPoints,
            NextTierMultiplier: nextTier?.PointsMultiplier,
            PointsToNextTier: pointsToNextTier,
            ProgressPercent: progressPercent,
            AvailableVouchers: availableVouchers,
            ReservedVouchers: reservedVouchers,
            UsedVouchers: usedVouchers,
            ExpiredVouchers: expiredVouchers,
            TierUpgradedAtUtc: account.TierUpgradedAtUtc,
            RowVersion: RowVersionCodec.Encode(account.RowVersion));
    }

    private static LoyaltyTierDto MapTierToDto(LoyaltyTier tier) =>
        new(
            Id: tier.Id,
            TierId: tier.Id,
            TierCode: tier.TierCode,
            DisplayName: tier.DisplayName,
            RankOrder: tier.RankOrder,
            MinQualifyingPoints: tier.MinQualifyingPoints,
            PointsMultiplier: tier.PointsMultiplier,
            BenefitsJson: tier.BenefitsJson,
            IsActive: tier.IsActive,
            UpdatedAtUtc: tier.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(ComputeTierConcurrencyBytes(tier)));

    private static byte[] ComputeTierConcurrencyBytes(LoyaltyTier tier)
    {
        var buffer = new byte[RowVersionCodec.ByteLength];
        var stampTicks = (tier.UpdatedAtUtc ?? tier.CreatedAtUtc).UtcTicks;
        BinaryPrimitives.WriteInt64BigEndian(buffer, stampTicks);
        return buffer;
    }

    internal static PointLedgerEntryDto MapLedgerEntryToDto(PointLedgerEntry entry) =>
        new(
            Id: entry.Id,
            LedgerEntryId: entry.Id,
            LoyaltyAccountId: entry.LoyaltyAccountId,
            TransactionType: entry.TransactionType,
            PointsDelta: entry.PointsDelta,
            BalanceAfter: entry.BalanceAfter,
            ReferenceType: entry.ReferenceType,
            ReferenceId: entry.ReferenceId,
            IdempotencyKey: entry.IdempotencyKey,
            Notes: entry.Notes,
            CreatedByUserId: entry.CreatedByUserId,
            CreatedAtUtc: entry.CreatedAtUtc);

    private readonly record struct VoucherStatusSnapshot(
        string Status,
        DateTimeOffset ExpiresAtUtc);
}
