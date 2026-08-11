using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Services;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Loyalty;

public sealed class VoucherService : IVoucherService
{
    private const int VoucherSuffixLength = 8;

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<VoucherCatalogQuery> _catalogQueryValidator;
    private readonly IValidator<UpsertVoucherCatalogRequest> _upsertCatalogValidator;
    private readonly IValidator<MyCustomerVouchersQuery> _myVouchersQueryValidator;
    private readonly IValidator<RedeemVoucherRequest> _redeemValidator;

    public VoucherService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<VoucherCatalogQuery> catalogQueryValidator,
        IValidator<UpsertVoucherCatalogRequest> upsertCatalogValidator,
        IValidator<MyCustomerVouchersQuery> myVouchersQueryValidator,
        IValidator<RedeemVoucherRequest> redeemValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _catalogQueryValidator = catalogQueryValidator ?? throw new ArgumentNullException(nameof(catalogQueryValidator));
        _upsertCatalogValidator = upsertCatalogValidator ?? throw new ArgumentNullException(nameof(upsertCatalogValidator));
        _myVouchersQueryValidator = myVouchersQueryValidator ?? throw new ArgumentNullException(nameof(myVouchersQueryValidator));
        _redeemValidator = redeemValidator ?? throw new ArgumentNullException(nameof(redeemValidator));
    }

    public async Task<IReadOnlyList<VoucherCatalogDto>> ListCatalogAsync(
        VoucherCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _catalogQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var canIncludeInactive = query.IncludeInactive &&
                                 _currentUserContext.IsAuthenticated &&
                                 (_currentUserContext.IsAdmin ||
                                  _currentUserContext.HasPermission(Permissions.Loyalty.ManageVoucherCatalog));

        var catalogQuery = _dbContext.VoucherCatalogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.MinTier)
            .Where(c => !c.IsDeleted && (canIncludeInactive || c.IsActive));

        if (!string.IsNullOrWhiteSpace(query.ApplicableScope) &&
            VoucherValidationRules.TryNormalizeApplicableScope(query.ApplicableScope, out var normalizedScope))
        {
            catalogQuery = catalogQuery.Where(c => c.ApplicableScope == normalizedScope);
        }

        var items = await catalogQuery
            .OrderBy(c => c.PointsCost)
            .ThenBy(c => c.Title)
            .ToListAsync(cancellationToken);

        int? callerAvailablePoints = null;
        int? callerTierMinPoints = null;
        int? callerTierRankOrder = null;

        if (_currentUserContext.IsAuthenticated &&
            _currentUserContext.IsCustomer &&
            _currentUserContext.UserId is { } callerCustomerId &&
            callerCustomerId != Guid.Empty)
        {
            var callerSnapshot = await _dbContext.LoyaltyAccounts
                .AsNoTracking()
                .Where(a => a.CustomerId == callerCustomerId)
                .Select(a => new
                {
                    a.AvailablePoints,
                    MinQualifyingPoints = a.CurrentTier != null ? a.CurrentTier.MinQualifyingPoints : 0,
                    RankOrder = a.CurrentTier != null ? (int)a.CurrentTier.RankOrder : 1
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (callerSnapshot is not null)
            {
                callerAvailablePoints = callerSnapshot.AvailablePoints;
                callerTierMinPoints = callerSnapshot.MinQualifyingPoints;
                callerTierRankOrder = callerSnapshot.RankOrder;
            }
            else
            {
                callerAvailablePoints = 0;
                callerTierMinPoints = 0;
                callerTierRankOrder = 1;
            }
        }

        return items
            .Select(c => MapCatalogToDto(c, callerAvailablePoints, callerTierMinPoints, callerTierRankOrder))
            .ToList();
    }

    public async Task<VoucherCatalogDto> CreateCatalogItemAsync(
        UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertCatalogValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ManageVoucherCatalog))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage the reward voucher catalog.",
                "FORBIDDEN_PERMISSION");
        }

        var normalizedPrefix = request.CodePrefix.Trim().ToUpperInvariant();
        VoucherValidationRules.TryNormalizeApplicableScope(request.ApplicableScope, out var normalizedScope);
        VoucherValidationRules.TryNormalizeDiscountType(request.DiscountType, out var normalizedDiscountType);

        var normalizedTitle = request.Title.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(request.Description)
            ? normalizedTitle
            : request.Description.Trim();

        var effectiveMaxDiscount = string.Equals(normalizedDiscountType, "Percentage", StringComparison.Ordinal)
            ? request.MaxDiscountAmount
            : (request.MaxDiscountAmount is > 0m ? request.MaxDiscountAmount : null);

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var utcNow = _dateTimeProvider.UtcNow;
                var minTier = await ResolveMinTierAsync(request, ct);

                var duplicateExists = await _dbContext.VoucherCatalogs
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.CodePrefix == normalizedPrefix, ct);

                if (duplicateExists)
                {
                    throw new ConflictException(
                        $"A voucher catalog item with CodePrefix '{normalizedPrefix}' already exists.",
                        "DUPLICATE_VOUCHER_PREFIX");
                }

                var catalog = new VoucherCatalog(Guid.CreateVersion7())
                {
                    CodePrefix = normalizedPrefix,
                    Title = normalizedTitle,
                    Description = normalizedDescription,
                    ApplicableScope = normalizedScope,
                    PointsCost = request.PointsCost,
                    DiscountType = normalizedDiscountType,
                    DiscountValue = Math.Round(request.DiscountValue, 2, MidpointRounding.AwayFromZero),
                    MaxDiscountAmount = effectiveMaxDiscount.HasValue
                        ? Math.Round(effectiveMaxDiscount.Value, 2, MidpointRounding.AwayFromZero)
                        : null,
                    MinOrderAmount = Math.Round(request.MinOrderAmount, 2, MidpointRounding.AwayFromZero),
                    ValidDays = request.ValidDays,
                    QuotaTotal = request.QuotaTotal,
                    QuotaRedeemed = 0,
                    MinTierId = minTier.Id,
                    MinTier = minTier,
                    IsActive = request.IsActive,
                    IsDeleted = false,
                    CreatedAtUtc = utcNow
                };

                _dbContext.VoucherCatalogs.Add(catalog);
                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapCatalogToDto(catalog, null, null, null);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg) &&
                                           sqlMsg.Contains("UQ_VoucherCatalogs_CodePrefix", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                $"A voucher catalog item with CodePrefix '{normalizedPrefix}' already exists.",
                "DUPLICATE_VOUCHER_PREFIX");
        }
    }

    public async Task<VoucherCatalogDto> UpdateCatalogItemAsync(
        Guid catalogId,
        UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertCatalogValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ManageVoucherCatalog))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage the reward voucher catalog.",
                "FORBIDDEN_PERMISSION");
        }

        var normalizedPrefix = request.CodePrefix.Trim().ToUpperInvariant();
        VoucherValidationRules.TryNormalizeApplicableScope(request.ApplicableScope, out var normalizedScope);
        VoucherValidationRules.TryNormalizeDiscountType(request.DiscountType, out var normalizedDiscountType);

        var normalizedTitle = request.Title.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(request.Description)
            ? normalizedTitle
            : request.Description.Trim();

        var effectiveMaxDiscount = string.Equals(normalizedDiscountType, "Percentage", StringComparison.Ordinal)
            ? request.MaxDiscountAmount
            : (request.MaxDiscountAmount is > 0m ? request.MaxDiscountAmount : null);

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var utcNow = _dateTimeProvider.UtcNow;

                var existing = await _dbContext.VoucherCatalogs
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == catalogId && !c.IsDeleted, ct)
                    ?? throw NotFoundException.ForEntity("VoucherCatalog", catalogId);

                if (request.QuotaTotal.HasValue && request.QuotaTotal.Value < existing.QuotaRedeemed)
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_VOUCHER_QUOTA",
                        $"QuotaTotal ({request.QuotaTotal.Value}) cannot be lower than already redeemed count ({existing.QuotaRedeemed}).");
                }

                var minTier = await ResolveMinTierAsync(request, ct);

                var duplicateExists = await _dbContext.VoucherCatalogs
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Id != catalogId && c.CodePrefix == normalizedPrefix, ct);

                if (duplicateExists)
                {
                    throw new ConflictException(
                        $"Another voucher catalog item already uses CodePrefix '{normalizedPrefix}'.",
                        "DUPLICATE_VOUCHER_PREFIX");
                }

                existing.CodePrefix = normalizedPrefix;
                existing.Title = normalizedTitle;
                existing.Description = normalizedDescription;
                existing.ApplicableScope = normalizedScope;
                existing.PointsCost = request.PointsCost;
                existing.DiscountType = normalizedDiscountType;
                existing.DiscountValue = Math.Round(request.DiscountValue, 2, MidpointRounding.AwayFromZero);
                existing.MaxDiscountAmount = effectiveMaxDiscount.HasValue
                    ? Math.Round(effectiveMaxDiscount.Value, 2, MidpointRounding.AwayFromZero)
                    : null;
                existing.MinOrderAmount = Math.Round(request.MinOrderAmount, 2, MidpointRounding.AwayFromZero);
                existing.ValidDays = request.ValidDays;
                existing.QuotaTotal = request.QuotaTotal;
                existing.MinTierId = minTier.Id;
                existing.MinTier = minTier;
                existing.IsActive = request.IsActive;
                existing.UpdatedAtUtc = utcNow;

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapCatalogToDto(existing, null, null, null);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg) &&
                                           sqlMsg.Contains("UQ_VoucherCatalogs_CodePrefix", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                $"Another voucher catalog item already uses CodePrefix '{normalizedPrefix}'.",
                "DUPLICATE_VOUCHER_PREFIX");
        }
    }

    public async Task<IReadOnlyList<CustomerVoucherDto>> ListMyVouchersAsync(
        MyCustomerVouchersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _myVouchersQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Loyalty.ViewOwn))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view personal customer vouchers.",
                "FORBIDDEN_PERMISSION");
        }

        var accountSnapshot = await _dbContext.LoyaltyAccounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => new
            {
                a.Id,
                a.AvailablePoints,
                a.LifetimeQualifyingPoints
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (accountSnapshot is null)
        {
            return [];
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var vouchersQuery = _dbContext.CustomerVouchers
            .AsNoTracking()
            .Include(v => v.VoucherCatalog)
            .Where(v => v.LoyaltyAccountId == accountSnapshot.Id);

        if (!string.IsNullOrWhiteSpace(query.ApplicableScope) &&
            VoucherValidationRules.TryNormalizeApplicableScope(query.ApplicableScope, out var normalizedScope))
        {
            vouchersQuery = vouchersQuery.Where(v =>
                v.VoucherCatalog != null && v.VoucherCatalog.ApplicableScope == normalizedScope);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            VoucherValidationRules.TryNormalizeVoucherStatus(query.Status, out var normalizedStatus))
        {
            vouchersQuery = normalizedStatus switch
            {
                "Available" => vouchersQuery.Where(v => v.Status == "Available" && v.ExpiresAtUtc > utcNow),
                "Expired" => vouchersQuery.Where(v =>
                    v.Status == "Expired" ||
                    (v.Status == "Available" && v.ExpiresAtUtc <= utcNow)),
                _ => vouchersQuery.Where(v => v.Status == normalizedStatus)
            };
        }

        var vouchers = await vouchersQuery
            .OrderByDescending(v => v.RedeemedAtUtc)
            .ThenByDescending(v => v.Id)
            .ToListAsync(cancellationToken);

        return vouchers
            .Select(v => MapCustomerVoucherToDto(
                v,
                utcNow,
                accountSnapshot.AvailablePoints,
                accountSnapshot.LifetimeQualifyingPoints))
            .ToList();
    }

    public async Task<CustomerVoucherDto> RedeemVoucherAsync(
        RedeemVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _redeemValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.IsCustomer || !_currentUserContext.HasPermission(Permissions.Loyalty.RedeemVoucher))
        {
            throw new ForbiddenAccessException(
                "Only authenticated Customer accounts may redeem reward vouchers.",
                "FORBIDDEN_PERMISSION");
        }

        var targetCatalogId = request.ResolveEffectiveCatalogId();
        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.ResolveEffectiveRowVersion());
        var explicitIdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        LoyaltyPointsUpdatedRealtimeEvent? postCommitPointsEvent = null;
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        CustomerVoucherDto redeemedDto;

        try
        {
            redeemedDto = await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                postCommitPointsEvent = null;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                var utcNow = _dateTimeProvider.UtcNow;

                var customerActive = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id == customerId && u.IsActive && !u.IsDeleted, ct);

                if (!customerActive)
                {
                    throw new UnauthorizedAccessException("Authenticated customer profile is missing or inactive.");
                }

                if (explicitIdempotencyKey is not null)
                {
                    var duplicateKey = await _dbContext.PointLedgerEntries
                        .IgnoreQueryFilters()
                        .AnyAsync(p => p.IdempotencyKey == explicitIdempotencyKey, ct);

                    if (duplicateKey)
                    {
                        throw new ConflictException(
                            $"Voucher redemption with IdempotencyKey '{explicitIdempotencyKey}' has already been processed.",
                            "DUPLICATE_IDEMPOTENCY_KEY");
                    }
                }

                var catalog = await _dbContext.VoucherCatalogs
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(c => c.MinTier)
                    .FirstOrDefaultAsync(c => c.Id == targetCatalogId, ct)
                    ?? throw NotFoundException.ForEntity("VoucherCatalog", targetCatalogId);

                if (catalog.IsDeleted || !catalog.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "VOUCHER_INACTIVE",
                        $"Reward voucher catalog '{catalog.CodePrefix}' is currently inactive.");
                }

                if (catalog.QuotaTotal.HasValue && catalog.QuotaRedeemed >= catalog.QuotaTotal.Value)
                {
                    throw new ConflictException(
                        $"Reward voucher catalog '{catalog.CodePrefix}' has reached its maximum quota ({catalog.QuotaTotal.Value}).",
                        "VOUCHER_QUOTA_EXHAUSTED");
                }

                var tiers = await _dbContext.LoyaltyTiers
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.RankOrder)
                    .ToListAsync(ct);

                var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                                 ?? tiers.FirstOrDefault()
                                 ?? throw new BusinessRuleViolationException(
                                     "SYSTEM_SEED_MISSING",
                                     "Canonical LoyaltyTiers seed data is missing.");

                var account = await _dbContext.LoyaltyAccounts
                    .Include(a => a.CurrentTier)
                    .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

                if (account is null)
                {
                    account = new LoyaltyAccount(Guid.CreateVersion7())
                    {
                        CustomerId = customerId,
                        CurrentTierId = bronzeTier.Id,
                        AvailablePoints = 0,
                        LifetimeQualifyingPoints = 0,
                        LifetimeSpentVnd = 0m,
                        CreatedAtUtc = utcNow
                    };
                    _dbContext.LoyaltyAccounts.Add(account);
                }

                if (expectedRowVersion is not null)
                {
                    if (!account.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                    {
                        throw new ConflictException(
                            "Your loyalty wallet balance changed concurrently. Please refresh and retry.",
                            "CONCURRENCY_CONFLICT");
                    }

                    _dbContext.Entry(account).Property(a => a.RowVersion).OriginalValue = expectedRowVersion;
                }

                var currentTier = account.CurrentTier
                                  ?? tiers.FirstOrDefault(t => t.Id == account.CurrentTierId)
                                  ?? bronzeTier;

                var minTier = catalog.MinTier
                              ?? tiers.FirstOrDefault(t => t.Id == catalog.MinTierId)
                              ?? bronzeTier;

                if (currentTier.MinQualifyingPoints < minTier.MinQualifyingPoints ||
                    currentTier.RankOrder < minTier.RankOrder)
                {
                    throw new BusinessRuleViolationException(
                        "TIER_REQUIREMENT_NOT_MET",
                        $"Reward '{catalog.Title}' requires {minTier.TierCode} tier ({minTier.MinQualifyingPoints:N0} qualifying points); your current tier is {currentTier.TierCode}.");
                }

                if (account.AvailablePoints < catalog.PointsCost)
                {
                    throw new BusinessRuleViolationException(
                        "INSUFFICIENT_POINTS",
                        $"Insufficient spendable loyalty points: required {catalog.PointsCost:N0}, available {account.AvailablePoints:N0}.");
                }

                // ADR-003 non-negotiable invariant: deduct AvailablePoints ONLY; LifetimeQualifyingPoints never decreases!
                account.AvailablePoints -= catalog.PointsCost;
                account.UpdatedAtUtc = utcNow;

                var voucherCode = await GenerateUniqueVoucherCodeAsync(catalog.CodePrefix, ct);
                var voucherId = Guid.CreateVersion7();
                var expiresAtUtc = utcNow.AddDays(catalog.ValidDays);

                var customerVoucher = new CustomerVoucher(voucherId)
                {
                    VoucherCode = voucherCode,
                    LoyaltyAccountId = account.Id,
                    VoucherCatalogId = catalog.Id,
                    PointsSpent = catalog.PointsCost,
                    Status = "Available",
                    RedeemedAtUtc = utcNow,
                    ExpiresAtUtc = expiresAtUtc,
                    CreatedAtUtc = utcNow
                };
                _dbContext.CustomerVouchers.Add(customerVoucher);

                var ledgerEntry = new PointLedgerEntry(Guid.CreateVersion7())
                {
                    LoyaltyAccountId = account.Id,
                    TransactionType = "VoucherRedeem",
                    PointsDelta = -catalog.PointsCost,
                    BalanceAfter = account.AvailablePoints,
                    ReferenceType = "CustomerVoucher",
                    ReferenceId = customerVoucher.Id,
                    IdempotencyKey = explicitIdempotencyKey ?? $"VOUCHER_REDEEM:{customerVoucher.Id}:SPEND",
                    Notes = $"Redeemed reward voucher {customerVoucher.VoucherCode} ({catalog.Title}).",
                    CreatedByUserId = customerId,
                    CreatedAtUtc = utcNow
                };
                _dbContext.PointLedgerEntries.Add(ledgerEntry);

                var notifId = Guid.CreateVersion7();
                var pointsEvent = new LoyaltyPointsUpdatedRealtimeEvent(
                    NotificationId: notifId,
                    EventType: "LoyaltyPointsUpdated",
                    LoyaltyAccountId: account.Id,
                    TransactionType: "VoucherRedeem",
                    PointsDelta: -catalog.PointsCost,
                    AvailablePointsAfter: account.AvailablePoints,
                    LifetimeQualifyingPointsAfter: account.LifetimeQualifyingPoints,
                    ReferenceType: "CustomerVoucher",
                    ReferenceNumber: customerVoucher.VoucherCode,
                    OccurredAtUtc: utcNow);

                _dbContext.Notifications.Add(new Notification(notifId)
                {
                    RecipientUserId = customerId,
                    EventType = "LoyaltyPointsUpdated",
                    Title = $"Voucher Redeemed — {customerVoucher.VoucherCode}",
                    Message = $"Spent {catalog.PointsCost:N0} points for '{catalog.Title}'. Remaining spendable balance: {account.AvailablePoints:N0} points.",
                    PayloadJson = JsonSerializer.Serialize(pointsEvent),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                postCommitPointsEvent = pointsEvent;

                await _dbContext.SaveChangesAsync(ct);

                var quotaUpdated = await _dbContext.VoucherCatalogs
                    .IgnoreQueryFilters()
                    .Where(c =>
                        c.Id == catalog.Id &&
                        !c.IsDeleted &&
                        c.IsActive &&
                        (!c.QuotaTotal.HasValue || c.QuotaRedeemed < c.QuotaTotal.Value))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(c => c.QuotaRedeemed, c => c.QuotaRedeemed + 1)
                            .SetProperty(c => c.UpdatedAtUtc, utcNow),
                        ct);

                if (quotaUpdated == 0)
                {
                    throw new ConflictException(
                        $"Reward voucher catalog '{catalog.CodePrefix}' quota was exhausted by another transaction.",
                        "VOUCHER_QUOTA_EXHAUSTED");
                }

                await tx.CommitAsync(ct);

                customerVoucher.VoucherCatalog = catalog;
                return MapCustomerVoucherToDto(
                    customerVoucher,
                    utcNow,
                    account.AvailablePoints,
                    account.LifetimeQualifyingPoints);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Your loyalty wallet was modified concurrently by another redemption or transaction. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_PointLedgerEntries_IdempotencyKey", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "Duplicate IdempotencyKey detected for voucher redemption.",
                    "DUPLICATE_IDEMPOTENCY_KEY");
            }

            throw new ConflictException(
                "A concurrent voucher redemption conflict occurred. Please retry.",
                "CONCURRENCY_CONFLICT");
        }

        if (postCommitPointsEvent is not null)
        {
            await _notificationPublisher.PublishLoyaltyPointsUpdatedAsync(
                postCommitPointsEvent,
                customerId,
                cancellationToken);
        }

        return redeemedDto;
    }

    private async Task<LoyaltyTier> ResolveMinTierAsync(
        UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MinTierId is { } tierId && tierId != Guid.Empty)
        {
            return await _dbContext.LoyaltyTiers
                       .FirstOrDefaultAsync(t => t.Id == tierId && t.IsActive, cancellationToken)
                   ?? throw NotFoundException.ForEntity("LoyaltyTier", tierId);
        }

        VoucherValidationRules.TryNormalizeTierCode(request.MinTierCode, out var normalizedCode);
        return await _dbContext.LoyaltyTiers
                   .FirstOrDefaultAsync(t => t.TierCode == normalizedCode && t.IsActive, cancellationToken)
               ?? throw NotFoundException.ForEntity("LoyaltyTier", normalizedCode);
    }

    private async Task<string> GenerateUniqueVoucherCodeAsync(
        string codePrefix,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = CreateRandomVoucherCode(codePrefix);
            var exists = await _dbContext.CustomerVouchers
                .IgnoreQueryFilters()
                .AnyAsync(v => v.VoucherCode == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        var fallbackHex = Guid.CreateVersion7().ToString("N")[^8..]
            .ToUpperInvariant()
            .Replace('I', 'X')
            .Replace('O', 'Y')
            .Replace('Q', 'Z');
        return $"{codePrefix}-{fallbackHex}";
    }

    private static string CreateRandomVoucherCode(string codePrefix)
    {
        ReadOnlySpan<char> alphabet = LoyaltyPointsCalculator.VoucherCodeSuffixAlphabet;
        Span<char> suffixChars = stackalloc char[VoucherSuffixLength];
        for (var i = 0; i < VoucherSuffixLength; i++)
        {
            suffixChars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return $"{codePrefix}-{new string(suffixChars)}";
    }

    private static VoucherCatalogDto MapCatalogToDto(
        VoucherCatalog catalog,
        int? callerAvailablePoints,
        int? callerTierMinPoints,
        int? callerTierRankOrder)
    {
        var minTierCode = catalog.MinTier?.TierCode ?? "Bronze";
        var minTierMinPoints = catalog.MinTier?.MinQualifyingPoints ?? 0;
        var minTierRank = catalog.MinTier?.RankOrder ?? (byte)1;

        int? remainingQuota = catalog.QuotaTotal.HasValue
            ? Math.Max(0, catalog.QuotaTotal.Value - catalog.QuotaRedeemed)
            : null;

        var hasQuota = !remainingQuota.HasValue || remainingQuota.Value > 0;
        bool isEligible;
        if (callerAvailablePoints.HasValue && callerTierMinPoints.HasValue && callerTierRankOrder.HasValue)
        {
            isEligible = catalog.IsActive &&
                         hasQuota &&
                         callerTierMinPoints.Value >= minTierMinPoints &&
                         callerTierRankOrder.Value >= minTierRank &&
                         callerAvailablePoints.Value >= catalog.PointsCost;
        }
        else
        {
            isEligible = catalog.IsActive && hasQuota;
        }

        return new VoucherCatalogDto(
            Id: catalog.Id,
            CatalogId: catalog.Id,
            CodePrefix: catalog.CodePrefix,
            Code: catalog.CodePrefix,
            Title: catalog.Title,
            Description: catalog.Description,
            ApplicableScope: catalog.ApplicableScope,
            PointsCost: catalog.PointsCost,
            DiscountType: catalog.DiscountType,
            DiscountValue: catalog.DiscountValue,
            MaxDiscountAmount: catalog.MaxDiscountAmount,
            MinOrderAmount: catalog.MinOrderAmount,
            ValidDays: catalog.ValidDays,
            QuotaTotal: catalog.QuotaTotal,
            QuotaRedeemed: catalog.QuotaRedeemed,
            RemainingQuota: remainingQuota,
            MinTierId: catalog.MinTierId,
            MinTierCode: minTierCode,
            MinTierRankOrder: minTierRank,
            IsEligibleForCaller: isEligible,
            IsActive: catalog.IsActive,
            CreatedAtUtc: catalog.CreatedAtUtc,
            UpdatedAtUtc: catalog.UpdatedAtUtc);
    }

    internal static CustomerVoucherDto MapCustomerVoucherToDto(
        CustomerVoucher voucher,
        DateTimeOffset utcNow,
        int? availablePointsAfter,
        int? lifetimeQualifyingPoints)
    {
        var catalog = voucher.VoucherCatalog;
        var isExpired = string.Equals(voucher.Status, "Expired", StringComparison.Ordinal) ||
                        (string.Equals(voucher.Status, "Available", StringComparison.Ordinal) &&
                         voucher.ExpiresAtUtc <= utcNow);

        var effectiveStatus = isExpired && string.Equals(voucher.Status, "Available", StringComparison.Ordinal)
            ? "Expired"
            : voucher.Status;

        return new CustomerVoucherDto(
            Id: voucher.Id,
            CustomerVoucherId: voucher.Id,
            VoucherCode: voucher.VoucherCode,
            LoyaltyAccountId: voucher.LoyaltyAccountId,
            VoucherCatalogId: voucher.VoucherCatalogId,
            CodePrefix: catalog?.CodePrefix ?? string.Empty,
            Title: catalog?.Title ?? voucher.VoucherCode,
            Description: catalog?.Description ?? string.Empty,
            ApplicableScope: catalog?.ApplicableScope ?? "ServiceOnly",
            DiscountType: catalog?.DiscountType ?? "FixedAmount",
            DiscountValue: catalog?.DiscountValue ?? 0m,
            MaxDiscountAmount: catalog?.MaxDiscountAmount,
            MinOrderAmount: catalog?.MinOrderAmount ?? 0m,
            PointsSpent: voucher.PointsSpent,
            AvailablePointsAfter: availablePointsAfter,
            LifetimeQualifyingPoints: lifetimeQualifyingPoints,
            Status: voucher.Status,
            EffectiveStatus: effectiveStatus,
            IsExpired: isExpired,
            RedeemedAtUtc: voucher.RedeemedAtUtc,
            ExpiresAtUtc: voucher.ExpiresAtUtc,
            UsedAtUtc: voucher.UsedAtUtc,
            RowVersion: RowVersionCodec.Encode(voucher.RowVersion));
    }
}
