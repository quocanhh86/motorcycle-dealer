namespace MotorcycleDealer.Application.Features.Vouchers;

public sealed record VoucherCatalogQuery(
    bool IncludeInactive = false,
    string? ApplicableScope = null);

public sealed record VoucherCatalogDto(
    Guid Id,
    Guid CatalogId,
    string CodePrefix,
    string Code,
    string Title,
    string Description,
    string ApplicableScope,
    int PointsCost,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int ValidDays,
    int? QuotaTotal,
    int QuotaRedeemed,
    int? RemainingQuota,
    Guid MinTierId,
    string MinTierCode,
    int MinTierRankOrder,
    bool IsEligibleForCaller,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record UpsertVoucherCatalogRequest
{
    public string CodePrefix { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ApplicableScope { get; init; } = "ServiceOnly";
    public int PointsCost { get; init; }
    public string DiscountType { get; init; } = "FixedAmount";
    public decimal DiscountValue { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal MinOrderAmount { get; init; }
    public int ValidDays { get; init; } = 90;
    public int? QuotaTotal { get; init; }
    public Guid? MinTierId { get; init; }
    public string? MinTierCode { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record MyCustomerVouchersQuery(
    string? Status = null,
    string? ApplicableScope = null);

public sealed record CustomerVoucherDto(
    Guid Id,
    Guid CustomerVoucherId,
    string VoucherCode,
    Guid LoyaltyAccountId,
    Guid VoucherCatalogId,
    string CodePrefix,
    string Title,
    string Description,
    string ApplicableScope,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int PointsSpent,
    int? AvailablePointsAfter,
    int? LifetimeQualifyingPoints,
    string Status,
    string EffectiveStatus,
    bool IsExpired,
    DateTimeOffset RedeemedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    string RowVersion);

public sealed record RedeemVoucherRequest
{
    public Guid VoucherCatalogId { get; init; }
    public Guid? CatalogId { get; init; }
    public string? AccountRowVersion { get; init; }
    public string? RowVersion { get; init; }
    public string? IdempotencyKey { get; init; }

    public Guid ResolveEffectiveCatalogId() =>
        VoucherCatalogId != Guid.Empty
            ? VoucherCatalogId
            : CatalogId ?? Guid.Empty;

    public string? ResolveEffectiveRowVersion() =>
        !string.IsNullOrWhiteSpace(AccountRowVersion) ? AccountRowVersion : RowVersion;
}

public interface IVoucherService
{
    Task<IReadOnlyList<VoucherCatalogDto>> ListCatalogAsync(
        VoucherCatalogQuery query,
        CancellationToken cancellationToken = default);

    Task<VoucherCatalogDto> CreateCatalogItemAsync(
        UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task<VoucherCatalogDto> UpdateCatalogItemAsync(
        Guid catalogId,
        UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerVoucherDto>> ListMyVouchersAsync(
        MyCustomerVouchersQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerVoucherDto> RedeemVoucherAsync(
        RedeemVoucherRequest request,
        CancellationToken cancellationToken = default);
}
