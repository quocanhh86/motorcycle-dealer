using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Vouchers;

namespace MotorcycleDealer.Application.Features.Loyalty;

public sealed record LoyaltyTierDto(
    Guid Id,
    Guid TierId,
    string TierCode,
    string DisplayName,
    int RankOrder,
    int MinQualifyingPoints,
    decimal PointsMultiplier,
    string BenefitsJson,
    bool IsActive,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record UpdateLoyaltyTierRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public decimal? PointsMultiplier { get; init; }
    public int? MinQualifyingPoints { get; init; }
    public string? BenefitsJson { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record LoyaltyAccountSummaryDto(
    Guid Id,
    Guid AccountId,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid TierId,
    string TierCode,
    string CurrentTierCode,
    string TierDisplayName,
    int RankOrder,
    decimal PointsMultiplier,
    decimal TierMultiplier,
    int AvailablePoints,
    int LifetimeQualifyingPoints,
    decimal LifetimeSpentVnd,
    string? NextTierCode,
    string? NextTierDisplayName,
    int? NextTierMinPoints,
    decimal? NextTierMultiplier,
    int PointsToNextTier,
    decimal ProgressPercent,
    int AvailableVouchers,
    int ReservedVouchers,
    int UsedVouchers,
    int ExpiredVouchers,
    DateTimeOffset? TierUpgradedAtUtc,
    string RowVersion);

public sealed record PointLedgerQuery(
    string? TransactionType = null,
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize) : PaginationQuery(PageNumber, PageSize);

public sealed record PointLedgerEntryDto(
    Guid Id,
    Guid LedgerEntryId,
    Guid LoyaltyAccountId,
    string TransactionType,
    int PointsDelta,
    int BalanceAfter,
    string ReferenceType,
    Guid ReferenceId,
    string IdempotencyKey,
    string? Notes,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerLoyaltyInspectionDto(
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid AccountId,
    string TierCode,
    string CurrentTierCode,
    decimal PointsMultiplier,
    decimal TierMultiplier,
    int AvailablePoints,
    int LifetimeQualifyingPoints,
    decimal LifetimeSpentVnd,
    string? NextTierCode,
    int PointsToNextTier,
    decimal ProgressPercent,
    string RowVersion,
    LoyaltyAccountSummaryDto Account,
    IReadOnlyList<CustomerVoucherDto> EligibleVouchers,
    IReadOnlyList<CustomerVoucherDto> AvailableServiceVouchers,
    IReadOnlyList<PointLedgerEntryDto> RecentLedgerEntries);

public sealed record AdminPointAdjustmentRequest
{
    public Guid? LoyaltyAccountId { get; init; }
    public Guid? AccountId { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerEmail { get; init; }
    public int PointsDelta { get; init; }
    public bool? QualifiesForTier { get; init; }
    public bool? AffectQualifyingPoints { get; init; }
    public string? Notes { get; init; }
    public string? ReasonNotes { get; init; }
    public string? Reason { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? AccountRowVersion { get; init; }
    public string? RowVersion { get; init; }

    public Guid? ResolveTargetAccountId() =>
        LoyaltyAccountId is { } laId && laId != Guid.Empty
            ? laId
            : AccountId is { } accId && accId != Guid.Empty
                ? accId
                : null;

    public string ResolveEffectiveNotes() =>
        !string.IsNullOrWhiteSpace(Notes)
            ? Notes.Trim()
            : !string.IsNullOrWhiteSpace(ReasonNotes)
                ? ReasonNotes.Trim()
                : !string.IsNullOrWhiteSpace(Reason)
                    ? Reason.Trim()
                    : string.Empty;

    public bool ResolveQualifiesForTier() =>
        QualifiesForTier ?? AffectQualifyingPoints ?? true;

    public string? ResolveEffectiveRowVersion() =>
        !string.IsNullOrWhiteSpace(AccountRowVersion) ? AccountRowVersion : RowVersion;
}

public sealed record LoyaltyAdjustmentResultDto(
    Guid Id,
    Guid LedgerEntryId,
    Guid LoyaltyAccountId,
    Guid CustomerId,
    string TransactionType,
    int PointsDelta,
    int BalanceAfter,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    string PreviousTierCode,
    string CurrentTierCode,
    string NewTierCode,
    decimal PointsMultiplier,
    bool TierUpgraded,
    string ReferenceType,
    Guid ReferenceId,
    string IdempotencyKey,
    string Notes,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);

public interface ILoyaltyService
{
    Task<IReadOnlyList<LoyaltyTierDto>> ListTiersAsync(
        CancellationToken cancellationToken = default);

    Task<LoyaltyTierDto> UpdateTierAsync(
        Guid tierId,
        UpdateLoyaltyTierRequest request,
        CancellationToken cancellationToken = default);

    Task<LoyaltyAccountSummaryDto> GetMyLoyaltySummaryAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<PointLedgerEntryDto>> ListMyPointLedgerAsync(
        PointLedgerQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerLoyaltyInspectionDto> GetCustomerLoyaltyForInspectionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<LoyaltyAdjustmentResultDto> AdjustCustomerPointsAsync(
        AdminPointAdjustmentRequest request,
        CancellationToken cancellationToken = default);
}
