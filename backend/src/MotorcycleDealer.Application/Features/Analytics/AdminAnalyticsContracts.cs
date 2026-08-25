using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Application.Features.Analytics;

public static class AnalyticsConstants
{
    public const int DefaultOverviewWindowDays = 180;
    public const int DefaultMonthlyTrendMonths = 12;
    public const int MaxWindowDays = 731;
    public const decimal VndPerLoyaltyPoint = 500.00m;

    public static IReadOnlyList<string> CanonicalBookingStatusOrder { get; } =
    [
        nameof(BookingStatus.Completed),
        nameof(BookingStatus.Confirmed),
        nameof(BookingStatus.CheckedIn),
        nameof(BookingStatus.InProgress),
        nameof(BookingStatus.Pending),
        nameof(BookingStatus.Cancelled),
        nameof(BookingStatus.NoShow)
    ];
}

public static class AnalyticsMath
{
    public static decimal ComputeRatePct(int numerator, int denominator)
    {
        if (denominator <= 0 || numerator <= 0)
        {
            return 0.00m;
        }

        return Math.Round((decimal)numerator * 100.00m / denominator, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ComputeSharePct(decimal part, decimal total)
    {
        if (total <= 0.00m || part <= 0.00m)
        {
            return 0.00m;
        }

        return Math.Round(part * 100.00m / total, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ComputePointsLiabilityVnd(long unredeemedPoints) =>
        unredeemedPoints <= 0 ? 0.00m : unredeemedPoints * AnalyticsConstants.VndPerLoyaltyPoint;

    public static IReadOnlyList<(int Year, int Month, string Label)> BuildContiguousMonthSpine(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc)
    {
        var normalizedFrom = fromUtc.ToUniversalTime();
        var normalizedTo = toUtc.ToUniversalTime();

        var startMonth = new DateOnly(normalizedFrom.Year, normalizedFrom.Month, 1);
        var endMonth = new DateOnly(normalizedTo.Year, normalizedTo.Month, 1);

        if (startMonth > endMonth)
        {
            return [];
        }

        var buckets = new List<(int Year, int Month, string Label)>();
        for (var cursor = startMonth; cursor <= endMonth; cursor = cursor.AddMonths(1))
        {
            buckets.Add((cursor.Year, cursor.Month, $"{cursor.Year:D4}-{cursor.Month:D2}"));
        }

        return buckets;
    }
}

public sealed record AnalyticsFilterQuery
{
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
    public Guid? DealerId { get; init; }
}

public sealed record DealerAnalyticsRowDto(
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string City,
    bool IsActive,
    int UnitsSoldCount,
    decimal ShowroomRevenueVnd,
    decimal ShowroomDiscountVnd,
    int InvoicesIssuedCount,
    decimal ServiceRevenueVnd,
    decimal ServiceVoucherDiscountVnd,
    decimal TotalVoucherDiscountVnd,
    decimal TotalRevenueVnd,
    decimal RevenueSharePct,
    int Pending,
    int Confirmed,
    int CheckedIn,
    int InProgress,
    int Completed,
    int Cancelled,
    int NoShow,
    int ActiveBookings,
    int CancelledOrNoShowBookings,
    int TotalBookings,
    decimal CompletionRatePct,
    decimal CancellationRatePct,
    decimal AverageCsatRating = 0.00m,
    int TotalServiceReviewsCount = 0,
    int FiveStarReviewsCount = 0,
    int UnansweredReviewsCount = 0,
    int OpenWarrantyClaimsCount = 0,
    int CompletedWarrantyClaimsCount = 0,
    decimal ApprovedWarrantyCoveredVnd = 0.00m)
{
    public int UnitsSold => UnitsSoldCount;
    public int InvoicesIssued => InvoicesIssuedCount;
    public int PendingBookings => Pending;
    public int ConfirmedBookings => Confirmed;
    public int CheckedInBookings => CheckedIn;
    public int InProgressBookings => InProgress;
    public int CompletedBookings => Completed;
    public int CancelledBookings => Cancelled;
    public int NoShowBookings => NoShow;
}

public sealed record AnalyticsOverviewDto(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? DealerId,
    decimal TotalRevenueVnd,
    decimal ShowroomRevenueVnd,
    int UnitsSoldCount,
    decimal ServiceRevenueVnd,
    int InvoicesIssuedCount,
    decimal TotalVoucherDiscountVnd,
    int TotalBookings,
    int CompletedBookings,
    int ActiveBookings,
    int CancelledBookings,
    int NoShowBookings,
    int CancelledOrNoShowBookings,
    decimal CompletionRatePct,
    decimal CancellationRatePct,
    int ActiveDealersCount,
    int TotalMembersCount,
    int UnredeemedPointsPool,
    decimal PointsLiabilityVnd,
    IReadOnlyList<DealerAnalyticsRowDto> Dealers,
    decimal AverageCsatRating = 0.00m,
    int TotalServiceReviewsCount = 0,
    int OpenWarrantyClaimsCount = 0,
    decimal ApprovedWarrantyCoveredVnd = 0.00m,
    int FiveStarReviewsCount = 0,
    int UnansweredReviewsCount = 0,
    int CompletedWarrantyClaimsCount = 0)
{
    public int ActiveLoyaltyMembers => TotalMembersCount;
    public int ActivePointsLiability => UnredeemedPointsPool;
}

public sealed record DealerRevenueItemDto(
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string City,
    bool IsActive,
    int UnitsSoldCount,
    decimal ShowroomRevenueVnd,
    decimal ShowroomDiscountVnd,
    int InvoicesIssuedCount,
    decimal ServiceRevenueVnd,
    decimal ServiceVoucherDiscountVnd,
    decimal TotalRevenueVnd,
    decimal RevenueSharePct)
{
    public int UnitsSold => UnitsSoldCount;
    public int InvoicesIssued => InvoicesIssuedCount;
}

public sealed record RevenueByDealerAnalyticsDto(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? DealerId,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> DealerCodes,
    IReadOnlyList<decimal> ShowroomSalesVnd,
    IReadOnlyList<decimal> WorkshopServiceVnd,
    IReadOnlyList<decimal> CombinedRevenueByDealerVnd,
    int UnitsSoldCount,
    decimal ShowroomRevenueVnd,
    int InvoicesIssuedCount,
    decimal ServiceRevenueVnd,
    decimal TotalVoucherDiscountVnd,
    decimal TotalRevenueVnd,
    IReadOnlyList<DealerRevenueItemDto> Dealers);

public sealed record BookingStatusBreakdownItemDto(
    string Status,
    int Count,
    decimal SharePct);

public sealed record BookingsByDealerAnalyticsDto(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? DealerId,
    IReadOnlyList<string> StatusLabels,
    IReadOnlyList<int> StatusCounts,
    IReadOnlyList<BookingStatusBreakdownItemDto> StatusBreakdown,
    IReadOnlyList<string> DealerLabels,
    IReadOnlyList<string> DealerCodes,
    IReadOnlyList<int> CompletedByDealer,
    IReadOnlyList<int> ActiveByDealer,
    IReadOnlyList<int> CancelledOrNoShowByDealer,
    int TotalBookings,
    int CompletedBookings,
    int ActiveBookings,
    int CancelledBookings,
    int NoShowBookings,
    int CancelledOrNoShowBookings,
    decimal CompletionRatePct,
    decimal CancellationRatePct,
    IReadOnlyList<DealerAnalyticsRowDto> Dealers)
{
    public IReadOnlyList<DealerAnalyticsRowDto> DealerBreakdown => Dealers;
}

public sealed record MonthlyTrendBucketDto(
    string Month,
    int Year,
    int MonthNumber,
    decimal ShowroomRevenueVnd,
    int UnitsSoldCount,
    decimal ServiceRevenueVnd,
    int InvoicesIssuedCount,
    decimal TotalRevenueVnd,
    int TotalBookings,
    int CompletedBookings,
    int CancelledOrNoShowBookings,
    decimal CompletionRatePct);

public sealed record MonthlyTrendsAnalyticsDto(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? DealerId,
    IReadOnlyList<string> MonthLabels,
    IReadOnlyList<decimal> ShowroomRevenueVnd,
    IReadOnlyList<int> UnitsSoldCounts,
    IReadOnlyList<decimal> ServiceRevenueVnd,
    IReadOnlyList<int> InvoicesIssuedCounts,
    IReadOnlyList<decimal> CombinedRevenueVnd,
    IReadOnlyList<int> BookingCounts,
    IReadOnlyList<int> CompletedBookingCounts,
    IReadOnlyList<int> CancelledOrNoShowBookingCounts,
    IReadOnlyList<MonthlyTrendBucketDto> Months);

public sealed record LoyaltyTierDistributionItemDto(
    Guid TierId,
    string TierCode,
    string TierName,
    byte RankOrder,
    decimal PointsMultiplier,
    int MinQualifyingPoints,
    int MemberCount,
    decimal MemberSharePct,
    int AvailablePointsSum,
    long LifetimeQualifyingPointsSum,
    decimal LifetimeSpentVndSum,
    decimal PointsLiabilityVnd);

public sealed record LoyaltyDistributionAnalyticsDto(
    IReadOnlyList<string> TierLabels,
    IReadOnlyList<string> DisplayLabels,
    IReadOnlyList<int> CustomerCounts,
    IReadOnlyList<int> AvailablePointsByTier,
    IReadOnlyList<decimal> LifetimeSpentVndByTier,
    int TotalMembers,
    int UnredeemedPointsPool,
    decimal PointsLiabilityVnd,
    IReadOnlyList<LoyaltyTierDistributionItemDto> Tiers);

public sealed record AnalyticsCsvExportFile(
    byte[] ContentBytes,
    string ContentType,
    string FileName);

public sealed record DealerQualityWarrantyRowDto(
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string City,
    bool IsActive,
    decimal AverageCsatRating,
    int TotalReviewsCount,
    int FiveStarReviewsCount,
    int UnansweredReviewsCount,
    decimal WouldRecommendRatePercent,
    int TotalWarrantyClaimsCount,
    int OpenWarrantyClaimsCount,
    int CompletedWarrantyClaimsCount,
    int RejectedWarrantyClaimsCount,
    decimal ApprovedWarrantyCoveredVnd)
{
    public int TotalServiceReviewsCount => TotalReviewsCount;
}

public sealed record WarrantyDefectBreakdownItemDto(
    string DefectCategory,
    int ClaimsCount,
    int OpenClaimsCount,
    int CompletedClaimsCount,
    int RejectedClaimsCount,
    decimal SharePercent,
    decimal ApprovedCoveredAmountVnd)
{
    public int Count => ClaimsCount;
    public decimal SharePct => SharePercent;
    public decimal CoveredAmountVnd => ApprovedCoveredAmountVnd;
}

public sealed record QualityWarrantyAnalyticsDto(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    Guid? DealerId,
    decimal AverageCsatRating,
    int TotalReviewsCount,
    int FiveStarReviewsCount,
    int UnansweredReviewsCount,
    decimal WouldRecommendRatePercent,
    int TotalWarrantyClaimsCount,
    int OpenWarrantyClaimsCount,
    int CompletedWarrantyClaimsCount,
    int RejectedWarrantyClaimsCount,
    decimal ApprovedWarrantyCoveredVnd,
    IReadOnlyList<DealerQualityWarrantyRowDto> Dealers,
    IReadOnlyList<WarrantyDefectBreakdownItemDto> DefectBreakdown)
{
    public int TotalServiceReviewsCount => TotalReviewsCount;
}

public interface IAdminAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<RevenueByDealerAnalyticsDto> GetRevenueByDealerAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<BookingsByDealerAnalyticsDto> GetBookingsByDealerAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<MonthlyTrendsAnalyticsDto> GetMonthlyTrendsAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<LoyaltyDistributionAnalyticsDto> GetLoyaltyDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<QualityWarrantyAnalyticsDto> GetQualityWarrantyAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<AnalyticsCsvExportFile> ExportDealerAnalyticsCsvAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default);
}
