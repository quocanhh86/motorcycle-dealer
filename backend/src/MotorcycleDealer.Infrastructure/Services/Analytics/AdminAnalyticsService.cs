using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Analytics;

public sealed class AdminAnalyticsService : IAdminAnalyticsService
{
    private const string CompletedSalesStatus = nameof(SalesOrderStatus.Completed);
    private const string PendingBookingStatus = nameof(BookingStatus.Pending);
    private const string ConfirmedBookingStatus = nameof(BookingStatus.Confirmed);
    private const string CheckedInBookingStatus = nameof(BookingStatus.CheckedIn);
    private const string InProgressBookingStatus = nameof(BookingStatus.InProgress);
    private const string CompletedBookingStatus = nameof(BookingStatus.Completed);
    private const string CancelledBookingStatus = nameof(BookingStatus.Cancelled);
    private const string NoShowBookingStatus = nameof(BookingStatus.NoShow);
    private const string UtcSqlTimeZoneId = "UTC";

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AdminAnalyticsService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(
            query,
            _dateTimeProvider.UtcNow);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var dealerRows = await AggregateDealerRowsAsync(dealers, fromUtc, toUtc, dealerId, cancellationToken);

        var loyaltySummary = await _dbContext.LoyaltyAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(la => !la.Customer!.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalMembersCount = g.Count(),
                UnredeemedPointsPool = g.Sum(x => x.AvailablePoints)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalMembersCount = loyaltySummary?.TotalMembersCount ?? 0;
        var unredeemedPointsPool = loyaltySummary?.UnredeemedPointsPool ?? 0;
        var pointsLiabilityVnd = AnalyticsMath.ComputePointsLiabilityVnd(unredeemedPointsPool);

        var showroomRevenueVnd = dealerRows.Sum(r => r.ShowroomRevenueVnd);
        var unitsSoldCount = dealerRows.Sum(r => r.UnitsSoldCount);
        var serviceRevenueVnd = dealerRows.Sum(r => r.ServiceRevenueVnd);
        var invoicesIssuedCount = dealerRows.Sum(r => r.InvoicesIssuedCount);
        var totalVoucherDiscountVnd = dealerRows.Sum(r => r.TotalVoucherDiscountVnd);
        var totalRevenueVnd = showroomRevenueVnd + serviceRevenueVnd;

        var totalBookings = dealerRows.Sum(r => r.TotalBookings);
        var completedBookings = dealerRows.Sum(r => r.CompletedBookings);
        var activeBookings = dealerRows.Sum(r => r.ActiveBookings);
        var cancelledBookings = dealerRows.Sum(r => r.CancelledBookings);
        var noShowBookings = dealerRows.Sum(r => r.NoShowBookings);
        var cancelledOrNoShowBookings = cancelledBookings + noShowBookings;

        var nonDeletedDealerIds = dealers.Select(d => d.Id).ToArray();
        var totalServiceReviewsCount = dealerRows.Sum(r => r.TotalServiceReviewsCount);
        var fiveStarReviewsCount = dealerRows.Sum(r => r.FiveStarReviewsCount);
        var unansweredReviewsCount = dealerRows.Sum(r => r.UnansweredReviewsCount);
        var openWarrantyClaimsCount = dealerRows.Sum(r => r.OpenWarrantyClaimsCount);
        var completedWarrantyClaimsCount = dealerRows.Sum(r => r.CompletedWarrantyClaimsCount);
        var approvedWarrantyCoveredVnd = dealerRows.Sum(r => r.ApprovedWarrantyCoveredVnd);

        decimal averageCsatRating = 0.00m;
        if (totalServiceReviewsCount > 0 && nonDeletedDealerIds.Length > 0)
        {
            var ratingSum = await BuildScopedReviewsQuery(fromUtc, toUtc, dealerId, nonDeletedDealerIds)
                .SumAsync(r => r.Rating, cancellationToken);

            averageCsatRating = Math.Round(
                (decimal)ratingSum / totalServiceReviewsCount,
                2,
                MidpointRounding.AwayFromZero);
        }

        return new AnalyticsOverviewDto(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            DealerId: dealerId,
            TotalRevenueVnd: totalRevenueVnd,
            ShowroomRevenueVnd: showroomRevenueVnd,
            UnitsSoldCount: unitsSoldCount,
            ServiceRevenueVnd: serviceRevenueVnd,
            InvoicesIssuedCount: invoicesIssuedCount,
            TotalVoucherDiscountVnd: totalVoucherDiscountVnd,
            TotalBookings: totalBookings,
            CompletedBookings: completedBookings,
            ActiveBookings: activeBookings,
            CancelledBookings: cancelledBookings,
            NoShowBookings: noShowBookings,
            CancelledOrNoShowBookings: cancelledOrNoShowBookings,
            CompletionRatePct: AnalyticsMath.ComputeRatePct(completedBookings, totalBookings),
            CancellationRatePct: AnalyticsMath.ComputeRatePct(cancelledOrNoShowBookings, totalBookings),
            ActiveDealersCount: dealers.Count(d => d.IsActive),
            TotalMembersCount: totalMembersCount,
            UnredeemedPointsPool: unredeemedPointsPool,
            PointsLiabilityVnd: pointsLiabilityVnd,
            Dealers: dealerRows,
            AverageCsatRating: averageCsatRating,
            TotalServiceReviewsCount: totalServiceReviewsCount,
            OpenWarrantyClaimsCount: openWarrantyClaimsCount,
            ApprovedWarrantyCoveredVnd: approvedWarrantyCoveredVnd,
            FiveStarReviewsCount: fiveStarReviewsCount,
            UnansweredReviewsCount: unansweredReviewsCount,
            CompletedWarrantyClaimsCount: completedWarrantyClaimsCount);
    }

    public async Task<RevenueByDealerAnalyticsDto> GetRevenueByDealerAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(
            query,
            _dateTimeProvider.UtcNow);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var dealerRows = await AggregateDealerRowsAsync(dealers, fromUtc, toUtc, dealerId, cancellationToken);

        var revenueItems = dealerRows
            .Select(r => new DealerRevenueItemDto(
                DealerId: r.DealerId,
                DealerCode: r.DealerCode,
                DealerName: r.DealerName,
                City: r.City,
                IsActive: r.IsActive,
                UnitsSoldCount: r.UnitsSoldCount,
                ShowroomRevenueVnd: r.ShowroomRevenueVnd,
                ShowroomDiscountVnd: r.ShowroomDiscountVnd,
                InvoicesIssuedCount: r.InvoicesIssuedCount,
                ServiceRevenueVnd: r.ServiceRevenueVnd,
                ServiceVoucherDiscountVnd: r.ServiceVoucherDiscountVnd,
                TotalRevenueVnd: r.TotalRevenueVnd,
                RevenueSharePct: r.RevenueSharePct))
            .ToArray();

        var showroomRevenueVnd = revenueItems.Sum(r => r.ShowroomRevenueVnd);
        var serviceRevenueVnd = revenueItems.Sum(r => r.ServiceRevenueVnd);
        var totalVoucherDiscountVnd = revenueItems.Sum(r => r.ShowroomDiscountVnd + r.ServiceVoucherDiscountVnd);

        return new RevenueByDealerAnalyticsDto(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            DealerId: dealerId,
            Labels: revenueItems.Select(r => r.DealerName).ToArray(),
            DealerCodes: revenueItems.Select(r => r.DealerCode).ToArray(),
            ShowroomSalesVnd: revenueItems.Select(r => r.ShowroomRevenueVnd).ToArray(),
            WorkshopServiceVnd: revenueItems.Select(r => r.ServiceRevenueVnd).ToArray(),
            CombinedRevenueByDealerVnd: revenueItems.Select(r => r.TotalRevenueVnd).ToArray(),
            UnitsSoldCount: revenueItems.Sum(r => r.UnitsSoldCount),
            ShowroomRevenueVnd: showroomRevenueVnd,
            InvoicesIssuedCount: revenueItems.Sum(r => r.InvoicesIssuedCount),
            ServiceRevenueVnd: serviceRevenueVnd,
            TotalVoucherDiscountVnd: totalVoucherDiscountVnd,
            TotalRevenueVnd: showroomRevenueVnd + serviceRevenueVnd,
            Dealers: revenueItems);
    }

    public async Task<BookingsByDealerAnalyticsDto> GetBookingsByDealerAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(
            query,
            _dateTimeProvider.UtcNow);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var dealerRows = await AggregateDealerRowsAsync(dealers, fromUtc, toUtc, dealerId, cancellationToken);

        var totalBookings = dealerRows.Sum(r => r.TotalBookings);
        var completedBookings = dealerRows.Sum(r => r.CompletedBookings);
        var activeBookings = dealerRows.Sum(r => r.ActiveBookings);
        var cancelledBookings = dealerRows.Sum(r => r.CancelledBookings);
        var noShowBookings = dealerRows.Sum(r => r.NoShowBookings);
        var cancelledOrNoShowBookings = cancelledBookings + noShowBookings;

        var networkCountsByStatus = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CompletedBookingStatus] = completedBookings,
            [ConfirmedBookingStatus] = dealerRows.Sum(r => r.Confirmed),
            [CheckedInBookingStatus] = dealerRows.Sum(r => r.CheckedIn),
            [InProgressBookingStatus] = dealerRows.Sum(r => r.InProgress),
            [PendingBookingStatus] = dealerRows.Sum(r => r.Pending),
            [CancelledBookingStatus] = cancelledBookings,
            [NoShowBookingStatus] = noShowBookings
        };

        var statusLabels = AnalyticsConstants.CanonicalBookingStatusOrder;
        var statusCounts = statusLabels
            .Select(label => networkCountsByStatus.GetValueOrDefault(label, 0))
            .ToArray();

        var statusBreakdown = statusLabels
            .Select(label =>
            {
                var count = networkCountsByStatus.GetValueOrDefault(label, 0);
                return new BookingStatusBreakdownItemDto(
                    Status: label,
                    Count: count,
                    SharePct: AnalyticsMath.ComputeRatePct(count, totalBookings));
            })
            .ToArray();

        return new BookingsByDealerAnalyticsDto(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            DealerId: dealerId,
            StatusLabels: statusLabels,
            StatusCounts: statusCounts,
            StatusBreakdown: statusBreakdown,
            DealerLabels: dealerRows.Select(r => r.DealerName).ToArray(),
            DealerCodes: dealerRows.Select(r => r.DealerCode).ToArray(),
            CompletedByDealer: dealerRows.Select(r => r.CompletedBookings).ToArray(),
            ActiveByDealer: dealerRows.Select(r => r.ActiveBookings).ToArray(),
            CancelledOrNoShowByDealer: dealerRows.Select(r => r.CancelledOrNoShowBookings).ToArray(),
            TotalBookings: totalBookings,
            CompletedBookings: completedBookings,
            ActiveBookings: activeBookings,
            CancelledBookings: cancelledBookings,
            NoShowBookings: noShowBookings,
            CancelledOrNoShowBookings: cancelledOrNoShowBookings,
            CompletionRatePct: AnalyticsMath.ComputeRatePct(completedBookings, totalBookings),
            CancellationRatePct: AnalyticsMath.ComputeRatePct(cancelledOrNoShowBookings, totalBookings),
            Dealers: dealerRows);
    }

    public async Task<MonthlyTrendsAnalyticsDto> GetMonthlyTrendsAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveMonthlyTrendsWindow(
            query,
            _dateTimeProvider.UtcNow);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var monthSpine = AnalyticsMath.BuildContiguousMonthSpine(fromUtc, toUtc);

        if (dealers.Count == 0 || monthSpine.Count == 0)
        {
            var emptyBuckets = monthSpine
                .Select(m => new MonthlyTrendBucketDto(
                    Month: m.Label,
                    Year: m.Year,
                    MonthNumber: m.Month,
                    ShowroomRevenueVnd: 0.00m,
                    UnitsSoldCount: 0,
                    ServiceRevenueVnd: 0.00m,
                    InvoicesIssuedCount: 0,
                    TotalRevenueVnd: 0.00m,
                    TotalBookings: 0,
                    CompletedBookings: 0,
                    CancelledOrNoShowBookings: 0,
                    CompletionRatePct: 0.00m))
                .ToArray();

            return BuildMonthlyTrendsDto(fromUtc, toUtc, dealerId, emptyBuckets);
        }

        var nonDeletedDealerIds = dealers.Select(d => d.Id).ToArray();

        var monthlySales = await BuildCompletedSalesQuery(fromUtc, toUtc, dealerId, nonDeletedDealerIds)
            .GroupBy(so => new
            {
                Year = EF.Functions.AtTimeZone(so.CompletedAtUtc!.Value, UtcSqlTimeZoneId).Year,
                Month = EF.Functions.AtTimeZone(so.CompletedAtUtc!.Value, UtcSqlTimeZoneId).Month
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                ShowroomRevenueVnd = g.Sum(x => x.FinalAmount),
                UnitsSoldCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var monthlyInvoices = await BuildPaidInvoicesQuery(fromUtc, toUtc, dealerId, nonDeletedDealerIds)
            .GroupBy(si => new
            {
                Year = EF.Functions.AtTimeZone(si.PaidAtUtc, UtcSqlTimeZoneId).Year,
                Month = EF.Functions.AtTimeZone(si.PaidAtUtc, UtcSqlTimeZoneId).Month
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                ServiceRevenueVnd = g.Sum(x => x.NetPaidAmount),
                InvoicesIssuedCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var monthlyBookings = await BuildScopedBookingsQuery(fromUtc, toUtc, dealerId, nonDeletedDealerIds)
            .GroupBy(sb => new
            {
                Year = EF.Functions.AtTimeZone(sb.CreatedAtUtc, UtcSqlTimeZoneId).Year,
                Month = EF.Functions.AtTimeZone(sb.CreatedAtUtc, UtcSqlTimeZoneId).Month,
                sb.Status
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Status,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var salesLookup = monthlySales.ToDictionary(x => (x.Year, x.Month));
        var invoiceLookup = monthlyInvoices.ToDictionary(x => (x.Year, x.Month));

        var bookingLookup = monthlyBookings
            .GroupBy(x => (x.Year, x.Month))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var total = g.Sum(x => x.Count);
                    var completed = g
                        .Where(x => string.Equals(x.Status, CompletedBookingStatus, StringComparison.Ordinal))
                        .Sum(x => x.Count);
                    var cancelledOrNoShow = g
                        .Where(x => string.Equals(x.Status, CancelledBookingStatus, StringComparison.Ordinal) ||
                                    string.Equals(x.Status, NoShowBookingStatus, StringComparison.Ordinal))
                        .Sum(x => x.Count);
                    return (Total: total, Completed: completed, CancelledOrNoShow: cancelledOrNoShow);
                });

        var buckets = new MonthlyTrendBucketDto[monthSpine.Count];
        for (var i = 0; i < monthSpine.Count; i++)
        {
            var (year, month, label) = monthSpine[i];
            var hasSales = salesLookup.TryGetValue((year, month), out var saleGroup);
            var hasInvoices = invoiceLookup.TryGetValue((year, month), out var invoiceGroup);
            var hasBookings = bookingLookup.TryGetValue((year, month), out var bookingGroup);

            var showroomVnd = hasSales ? saleGroup!.ShowroomRevenueVnd : 0.00m;
            var unitsSold = hasSales ? saleGroup!.UnitsSoldCount : 0;
            var serviceVnd = hasInvoices ? invoiceGroup!.ServiceRevenueVnd : 0.00m;
            var invoicesIssued = hasInvoices ? invoiceGroup!.InvoicesIssuedCount : 0;
            var totalBookings = hasBookings ? bookingGroup.Total : 0;
            var completedBookings = hasBookings ? bookingGroup.Completed : 0;
            var cancelledOrNoShow = hasBookings ? bookingGroup.CancelledOrNoShow : 0;

            buckets[i] = new MonthlyTrendBucketDto(
                Month: label,
                Year: year,
                MonthNumber: month,
                ShowroomRevenueVnd: showroomVnd,
                UnitsSoldCount: unitsSold,
                ServiceRevenueVnd: serviceVnd,
                InvoicesIssuedCount: invoicesIssued,
                TotalRevenueVnd: showroomVnd + serviceVnd,
                TotalBookings: totalBookings,
                CompletedBookings: completedBookings,
                CancelledOrNoShowBookings: cancelledOrNoShow,
                CompletionRatePct: AnalyticsMath.ComputeRatePct(completedBookings, totalBookings));
        }

        return BuildMonthlyTrendsDto(fromUtc, toUtc, dealerId, buckets);
    }

    public async Task<LoyaltyDistributionAnalyticsDto> GetLoyaltyDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var tiers = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .OrderBy(t => t.MinQualifyingPoints)
            .ThenBy(t => t.RankOrder)
            .ToListAsync(cancellationToken);

        var accountsByTier = await _dbContext.LoyaltyAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(la => !la.Customer!.IsDeleted)
            .GroupBy(la => la.CurrentTierId)
            .Select(g => new
            {
                TierId = g.Key,
                MemberCount = g.Count(),
                AvailablePointsSum = g.Sum(x => x.AvailablePoints),
                LifetimeQualifyingPointsSum = g.Sum(x => (long)x.LifetimeQualifyingPoints),
                LifetimeSpentVndSum = g.Sum(x => x.LifetimeSpentVnd)
            })
            .ToDictionaryAsync(x => x.TierId, cancellationToken);

        var totalMembers = accountsByTier.Values.Sum(x => x.MemberCount);
        var unredeemedPointsPool = accountsByTier.Values.Sum(x => x.AvailablePointsSum);

        var slices = tiers
            .Select(tier =>
            {
                var hasGroup = accountsByTier.TryGetValue(tier.Id, out var group);
                var memberCount = hasGroup ? group!.MemberCount : 0;
                var availablePoints = hasGroup ? group!.AvailablePointsSum : 0;
                var lifetimePoints = hasGroup ? group!.LifetimeQualifyingPointsSum : 0L;
                var lifetimeSpentVnd = hasGroup ? group!.LifetimeSpentVndSum : 0.00m;

                return new LoyaltyTierDistributionItemDto(
                    TierId: tier.Id,
                    TierCode: tier.TierCode,
                    TierName: tier.DisplayName,
                    RankOrder: tier.RankOrder,
                    PointsMultiplier: tier.PointsMultiplier,
                    MinQualifyingPoints: tier.MinQualifyingPoints,
                    MemberCount: memberCount,
                    MemberSharePct: AnalyticsMath.ComputeRatePct(memberCount, totalMembers),
                    AvailablePointsSum: availablePoints,
                    LifetimeQualifyingPointsSum: lifetimePoints,
                    LifetimeSpentVndSum: lifetimeSpentVnd,
                    PointsLiabilityVnd: AnalyticsMath.ComputePointsLiabilityVnd(availablePoints));
            })
            .ToArray();

        return new LoyaltyDistributionAnalyticsDto(
            TierLabels: slices.Select(s => s.TierCode).ToArray(),
            DisplayLabels: slices
                .Select(s => $"{s.TierCode} ({s.PointsMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}x)")
                .ToArray(),
            CustomerCounts: slices.Select(s => s.MemberCount).ToArray(),
            AvailablePointsByTier: slices.Select(s => s.AvailablePointsSum).ToArray(),
            LifetimeSpentVndByTier: slices.Select(s => s.LifetimeSpentVndSum).ToArray(),
            TotalMembers: totalMembers,
            UnredeemedPointsPool: unredeemedPointsPool,
            PointsLiabilityVnd: AnalyticsMath.ComputePointsLiabilityVnd(unredeemedPointsPool),
            Tiers: slices);
    }

    public async Task<QualityWarrantyAnalyticsDto> GetQualityWarrantyAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ViewAllDealers);

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(
            query,
            _dateTimeProvider.UtcNow);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var canonicalCategories = Enum.GetValues<WarrantyDefectCategory>();

        if (dealers.Count == 0)
        {
            var emptyDefects = canonicalCategories
                .Select(cat => new WarrantyDefectBreakdownItemDto(
                    DefectCategory: cat.ToString(),
                    ClaimsCount: 0,
                    OpenClaimsCount: 0,
                    CompletedClaimsCount: 0,
                    RejectedClaimsCount: 0,
                    SharePercent: 0.00m,
                    ApprovedCoveredAmountVnd: 0.00m))
                .ToArray();

            return new QualityWarrantyAnalyticsDto(
                FromUtc: fromUtc,
                ToUtc: toUtc,
                DealerId: dealerId,
                AverageCsatRating: 0.00m,
                TotalReviewsCount: 0,
                FiveStarReviewsCount: 0,
                UnansweredReviewsCount: 0,
                WouldRecommendRatePercent: 0.00m,
                TotalWarrantyClaimsCount: 0,
                OpenWarrantyClaimsCount: 0,
                CompletedWarrantyClaimsCount: 0,
                RejectedWarrantyClaimsCount: 0,
                ApprovedWarrantyCoveredVnd: 0.00m,
                Dealers: [],
                DefectBreakdown: emptyDefects);
        }

        var nonDeletedDealerIds = dealers.Select(d => d.Id).ToArray();

        var (reviewsByDealer, warrantyByDealer, warrantyGroups) = await QueryReviewsAndWarrantyByDealerAsync(
            fromUtc,
            toUtc,
            dealerId,
            nonDeletedDealerIds,
            cancellationToken);

        var dealerQualityRows = dealers
            .Select(dealer =>
            {
                var hasReviews = reviewsByDealer.TryGetValue(dealer.Id, out var rev);
                var hasWarranty = warrantyByDealer.TryGetValue(dealer.Id, out var war);

                return new DealerQualityWarrantyRowDto(
                    DealerId: dealer.Id,
                    DealerCode: dealer.Code,
                    DealerName: dealer.Name,
                    City: dealer.City,
                    IsActive: dealer.IsActive,
                    AverageCsatRating: hasReviews ? rev!.AverageCsatRating : 0.00m,
                    TotalReviewsCount: hasReviews ? rev!.TotalReviewsCount : 0,
                    FiveStarReviewsCount: hasReviews ? rev!.FiveStarCount : 0,
                    UnansweredReviewsCount: hasReviews ? rev!.UnansweredCount : 0,
                    WouldRecommendRatePercent: hasReviews ? rev!.WouldRecommendRatePercent : 0.00m,
                    TotalWarrantyClaimsCount: hasWarranty ? war!.TotalClaims : 0,
                    OpenWarrantyClaimsCount: hasWarranty ? war!.OpenClaims : 0,
                    CompletedWarrantyClaimsCount: hasWarranty ? war!.CompletedClaims : 0,
                    RejectedWarrantyClaimsCount: hasWarranty ? war!.RejectedClaims : 0,
                    ApprovedWarrantyCoveredVnd: hasWarranty ? war!.ApprovedCoveredVnd : 0.00m);
            })
            .OrderBy(r => r.DealerCode, StringComparer.Ordinal)
            .ToArray();

        var totalReviewsCount = reviewsByDealer.Values.Sum(x => x.TotalReviewsCount);
        var totalRatingSum = reviewsByDealer.Values.Sum(x => x.RatingSum);
        var fiveStarReviewsCount = reviewsByDealer.Values.Sum(x => x.FiveStarCount);
        var unansweredReviewsCount = reviewsByDealer.Values.Sum(x => x.UnansweredCount);
        var recommendedReviewsCount = reviewsByDealer.Values.Sum(x => x.RecommendedCount);

        var averageCsatRating = ComputeAverageCsatRating(totalRatingSum, totalReviewsCount);

        var totalWarrantyClaimsCount = dealerQualityRows.Sum(r => r.TotalWarrantyClaimsCount);
        var openWarrantyClaimsCount = dealerQualityRows.Sum(r => r.OpenWarrantyClaimsCount);
        var completedWarrantyClaimsCount = dealerQualityRows.Sum(r => r.CompletedWarrantyClaimsCount);
        var rejectedWarrantyClaimsCount = dealerQualityRows.Sum(r => r.RejectedWarrantyClaimsCount);
        var approvedWarrantyCoveredVnd = dealerQualityRows.Sum(r => r.ApprovedWarrantyCoveredVnd);

        var warrantyByDefect = warrantyGroups
            .GroupBy(x => x.DefectCategory)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var defectBreakdown = canonicalCategories
            .Select((category, orderIdx) =>
            {
                var hasBucket = warrantyByDefect.TryGetValue(category, out var bucket);
                var claimsCount = hasBucket ? bucket!.Sum(x => x.Count) : 0;
                var openCount = hasBucket
                    ? bucket!.Where(x => IsOpenWarrantyStatus(x.Status)).Sum(x => x.Count)
                    : 0;
                var completedCount = hasBucket
                    ? bucket!.Where(x => x.Status == WarrantyClaimStatus.Completed).Sum(x => x.Count)
                    : 0;
                var rejectedCount = hasBucket
                    ? bucket!.Where(x => x.Status == WarrantyClaimStatus.Rejected).Sum(x => x.Count)
                    : 0;
                var coveredVnd = hasBucket
                    ? bucket!.Where(x => IsCoveredWarrantyStatus(x.Status)).Sum(x => x.CoveredAmountVnd)
                    : 0.00m;

                return (
                    OrderIdx: orderIdx,
                    Item: new WarrantyDefectBreakdownItemDto(
                        DefectCategory: category.ToString(),
                        ClaimsCount: claimsCount,
                        OpenClaimsCount: openCount,
                        CompletedClaimsCount: completedCount,
                        RejectedClaimsCount: rejectedCount,
                        SharePercent: AnalyticsMath.ComputeRatePct(claimsCount, totalWarrantyClaimsCount),
                        ApprovedCoveredAmountVnd: coveredVnd));
            })
            .OrderByDescending(x => x.Item.ClaimsCount)
            .ThenBy(x => x.OrderIdx)
            .Select(x => x.Item)
            .ToArray();

        return new QualityWarrantyAnalyticsDto(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            DealerId: dealerId,
            AverageCsatRating: averageCsatRating,
            TotalReviewsCount: totalReviewsCount,
            FiveStarReviewsCount: fiveStarReviewsCount,
            UnansweredReviewsCount: unansweredReviewsCount,
            WouldRecommendRatePercent: AnalyticsMath.ComputeRatePct(recommendedReviewsCount, totalReviewsCount),
            TotalWarrantyClaimsCount: totalWarrantyClaimsCount,
            OpenWarrantyClaimsCount: openWarrantyClaimsCount,
            CompletedWarrantyClaimsCount: completedWarrantyClaimsCount,
            RejectedWarrantyClaimsCount: rejectedWarrantyClaimsCount,
            ApprovedWarrantyCoveredVnd: approvedWarrantyCoveredVnd,
            Dealers: dealerQualityRows,
            DefectBreakdown: defectBreakdown);
    }

    public async Task<AnalyticsCsvExportFile> ExportDealerAnalyticsCsvAsync(
        AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminPermission(Permissions.Analytics.ExportReports);

        var now = _dateTimeProvider.UtcNow;
        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(query, now);

        var dealers = await LoadTargetDealersAsync(dealerId, cancellationToken);
        var dealerRows = await AggregateDealerRowsAsync(dealers, fromUtc, toUtc, dealerId, cancellationToken);

        return AnalyticsCsvExporter.BuildDealerAnalyticsCsv(dealerRows, now);
    }

    private void EnsureAdminPermission(string requiredPermission)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication is required to access executive analytics.");
        }

        if (!_currentUserContext.IsAdmin || !_currentUserContext.HasPermission(requiredPermission))
        {
            throw new ForbiddenAccessException(
                $"Executive analytics endpoint requires the Admin role and permission '{requiredPermission}'.",
                "FORBIDDEN_PERMISSION");
        }
    }

    private async Task<IReadOnlyList<Dealer>> LoadTargetDealersAsync(
        Guid? dealerId,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.Dealers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => !d.IsDeleted);

        if (dealerId.HasValue)
        {
            var singleDealer = await baseQuery
                .FirstOrDefaultAsync(d => d.Id == dealerId.Value, cancellationToken);

            if (singleDealer is null)
            {
                throw NotFoundException.ForEntity("Dealer", dealerId.Value);
            }

            return [singleDealer];
        }

        return await baseQuery
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<SalesOrder> BuildCompletedSalesQuery(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds)
    {
        var query = _dbContext.SalesOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(so => so.Status == CompletedSalesStatus
                         && so.CompletedAtUtc != null
                         && so.CompletedAtUtc >= fromUtc
                         && so.CompletedAtUtc <= toUtc);

        return scopedDealerId.HasValue
            ? query.Where(so => so.DealerId == scopedDealerId.Value)
            : query.Where(so => nonDeletedDealerIds.Contains(so.DealerId));
    }

    private IQueryable<ServiceInvoice> BuildPaidInvoicesQuery(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds)
    {
        var query = _dbContext.ServiceInvoices
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(si => si.PaidAtUtc >= fromUtc && si.PaidAtUtc <= toUtc);

        return scopedDealerId.HasValue
            ? query.Where(si => si.DealerId == scopedDealerId.Value)
            : query.Where(si => nonDeletedDealerIds.Contains(si.DealerId));
    }

    private IQueryable<ServiceBooking> BuildScopedBookingsQuery(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds)
    {
        var query = _dbContext.ServiceBookings
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(sb => sb.CreatedAtUtc >= fromUtc && sb.CreatedAtUtc <= toUtc);

        return scopedDealerId.HasValue
            ? query.Where(sb => sb.DealerId == scopedDealerId.Value)
            : query.Where(sb => nonDeletedDealerIds.Contains(sb.DealerId));
    }

    private IQueryable<ServiceReview> BuildScopedReviewsQuery(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds)
    {
        var query = _dbContext.ServiceReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(sr => sr.CreatedAtUtc >= fromUtc && sr.CreatedAtUtc <= toUtc);

        return scopedDealerId.HasValue
            ? query.Where(sr => sr.DealerId == scopedDealerId.Value)
            : query.Where(sr => nonDeletedDealerIds.Contains(sr.DealerId));
    }

    private IQueryable<WarrantyClaim> BuildScopedWarrantyClaimsQuery(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds)
    {
        var query = _dbContext.WarrantyClaims
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(wc => wc.CreatedAtUtc >= fromUtc && wc.CreatedAtUtc <= toUtc);

        return scopedDealerId.HasValue
            ? query.Where(wc => wc.DealerId == scopedDealerId.Value)
            : query.Where(wc => nonDeletedDealerIds.Contains(wc.DealerId));
    }

    private async Task<IReadOnlyList<DealerAnalyticsRowDto>> AggregateDealerRowsAsync(
        IReadOnlyList<Dealer> dealers,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        CancellationToken cancellationToken)
    {
        if (dealers.Count == 0)
        {
            return [];
        }

        var nonDeletedDealerIds = dealers.Select(d => d.Id).ToArray();

        var salesByDealer = await BuildCompletedSalesQuery(fromUtc, toUtc, scopedDealerId, nonDeletedDealerIds)
            .GroupBy(so => so.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                UnitsSoldCount = g.Count(),
                ShowroomRevenueVnd = g.Sum(x => x.FinalAmount),
                ShowroomDiscountVnd = g.Sum(x => x.DiscountAmount)
            })
            .ToDictionaryAsync(x => x.DealerId, cancellationToken);

        var invoicesByDealer = await BuildPaidInvoicesQuery(fromUtc, toUtc, scopedDealerId, nonDeletedDealerIds)
            .GroupBy(si => si.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                InvoicesIssuedCount = g.Count(),
                ServiceRevenueVnd = g.Sum(x => x.NetPaidAmount),
                ServiceVoucherDiscountVnd = g.Sum(x => x.VoucherDiscountAmount)
            })
            .ToDictionaryAsync(x => x.DealerId, cancellationToken);

        var bookingGroups = await BuildScopedBookingsQuery(fromUtc, toUtc, scopedDealerId, nonDeletedDealerIds)
            .GroupBy(sb => new { sb.DealerId, sb.Status })
            .Select(g => new
            {
                g.Key.DealerId,
                g.Key.Status,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var bookingsByDealer = bookingGroups
            .GroupBy(x => x.DealerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.Status, x => x.Count, StringComparer.Ordinal));

        var (reviewsByDealer, warrantyByDealer, _) = await QueryReviewsAndWarrantyByDealerAsync(
            fromUtc,
            toUtc,
            scopedDealerId,
            nonDeletedDealerIds,
            cancellationToken);

        var networkTotalRevenueVnd = dealers.Sum(dealer =>
        {
            var showroom = salesByDealer.TryGetValue(dealer.Id, out var s) ? s.ShowroomRevenueVnd : 0.00m;
            var service = invoicesByDealer.TryGetValue(dealer.Id, out var inv) ? inv.ServiceRevenueVnd : 0.00m;
            return showroom + service;
        });

        var rows = dealers
            .Select(dealer =>
            {
                var hasSales = salesByDealer.TryGetValue(dealer.Id, out var sales);
                var hasInvoices = invoicesByDealer.TryGetValue(dealer.Id, out var invoices);
                bookingsByDealer.TryGetValue(dealer.Id, out var statusMap);
                var hasReviews = reviewsByDealer.TryGetValue(dealer.Id, out var reviewSummary);
                var hasWarranty = warrantyByDealer.TryGetValue(dealer.Id, out var warrantySummary);

                var unitsSoldCount = hasSales ? sales!.UnitsSoldCount : 0;
                var showroomRevenueVnd = hasSales ? sales!.ShowroomRevenueVnd : 0.00m;
                var showroomDiscountVnd = hasSales ? sales!.ShowroomDiscountVnd : 0.00m;

                var invoicesIssuedCount = hasInvoices ? invoices!.InvoicesIssuedCount : 0;
                var serviceRevenueVnd = hasInvoices ? invoices!.ServiceRevenueVnd : 0.00m;
                var serviceVoucherDiscountVnd = hasInvoices ? invoices!.ServiceVoucherDiscountVnd : 0.00m;

                var totalVoucherDiscountVnd = showroomDiscountVnd + serviceVoucherDiscountVnd;
                var totalRevenueVnd = showroomRevenueVnd + serviceRevenueVnd;

                var pending = statusMap?.GetValueOrDefault(PendingBookingStatus, 0) ?? 0;
                var confirmed = statusMap?.GetValueOrDefault(ConfirmedBookingStatus, 0) ?? 0;
                var checkedIn = statusMap?.GetValueOrDefault(CheckedInBookingStatus, 0) ?? 0;
                var inProgress = statusMap?.GetValueOrDefault(InProgressBookingStatus, 0) ?? 0;
                var completed = statusMap?.GetValueOrDefault(CompletedBookingStatus, 0) ?? 0;
                var cancelled = statusMap?.GetValueOrDefault(CancelledBookingStatus, 0) ?? 0;
                var noShow = statusMap?.GetValueOrDefault(NoShowBookingStatus, 0) ?? 0;

                var activeBookings = pending + confirmed + checkedIn + inProgress;
                var cancelledOrNoShowBookings = cancelled + noShow;
                var totalBookings = activeBookings + completed + cancelledOrNoShowBookings;

                var totalReviewsCount = hasReviews ? reviewSummary!.TotalReviewsCount : 0;
                var averageCsatRating = hasReviews ? reviewSummary!.AverageCsatRating : 0.00m;
                var fiveStarReviewsCount = hasReviews ? reviewSummary!.FiveStarCount : 0;
                var unansweredReviewsCount = hasReviews ? reviewSummary!.UnansweredCount : 0;

                var openWarrantyClaimsCount = hasWarranty ? warrantySummary!.OpenClaims : 0;
                var completedWarrantyClaimsCount = hasWarranty ? warrantySummary!.CompletedClaims : 0;
                var approvedWarrantyCoveredVnd = hasWarranty ? warrantySummary!.ApprovedCoveredVnd : 0.00m;

                return new DealerAnalyticsRowDto(
                    DealerId: dealer.Id,
                    DealerCode: dealer.Code,
                    DealerName: dealer.Name,
                    City: dealer.City,
                    IsActive: dealer.IsActive,
                    UnitsSoldCount: unitsSoldCount,
                    ShowroomRevenueVnd: showroomRevenueVnd,
                    ShowroomDiscountVnd: showroomDiscountVnd,
                    InvoicesIssuedCount: invoicesIssuedCount,
                    ServiceRevenueVnd: serviceRevenueVnd,
                    ServiceVoucherDiscountVnd: serviceVoucherDiscountVnd,
                    TotalVoucherDiscountVnd: totalVoucherDiscountVnd,
                    TotalRevenueVnd: totalRevenueVnd,
                    RevenueSharePct: AnalyticsMath.ComputeSharePct(totalRevenueVnd, networkTotalRevenueVnd),
                    Pending: pending,
                    Confirmed: confirmed,
                    CheckedIn: checkedIn,
                    InProgress: inProgress,
                    Completed: completed,
                    Cancelled: cancelled,
                    NoShow: noShow,
                    ActiveBookings: activeBookings,
                    CancelledOrNoShowBookings: cancelledOrNoShowBookings,
                    TotalBookings: totalBookings,
                    CompletionRatePct: AnalyticsMath.ComputeRatePct(completed, totalBookings),
                    CancellationRatePct: AnalyticsMath.ComputeRatePct(cancelledOrNoShowBookings, totalBookings),
                    AverageCsatRating: averageCsatRating,
                    TotalServiceReviewsCount: totalReviewsCount,
                    FiveStarReviewsCount: fiveStarReviewsCount,
                    UnansweredReviewsCount: unansweredReviewsCount,
                    OpenWarrantyClaimsCount: openWarrantyClaimsCount,
                    CompletedWarrantyClaimsCount: completedWarrantyClaimsCount,
                    ApprovedWarrantyCoveredVnd: approvedWarrantyCoveredVnd);
            })
            .OrderByDescending(r => r.TotalRevenueVnd)
            .ThenBy(r => r.DealerCode, StringComparer.Ordinal)
            .ToArray();

        return rows;
    }

    private sealed record DealerReviewRollup(
        int TotalReviewsCount,
        int RatingSum,
        int FiveStarCount,
        int RecommendedCount,
        int UnansweredCount)
    {
        public decimal AverageCsatRating => ComputeAverageCsatRating(RatingSum, TotalReviewsCount);

        public decimal WouldRecommendRatePercent =>
            AnalyticsMath.ComputeRatePct(RecommendedCount, TotalReviewsCount);
    }

    private sealed record WarrantyGroupSlice(
        Guid DealerId,
        WarrantyClaimStatus Status,
        WarrantyDefectCategory DefectCategory,
        int Count,
        decimal CoveredAmountVnd);

    private sealed record DealerWarrantyRollup(
        int TotalClaims,
        int OpenClaims,
        int CompletedClaims,
        int RejectedClaims,
        decimal ApprovedCoveredVnd);

    private async Task<(
        Dictionary<Guid, DealerReviewRollup> ReviewsByDealer,
        Dictionary<Guid, DealerWarrantyRollup> WarrantyByDealer,
        IReadOnlyList<WarrantyGroupSlice> WarrantyGroups)> QueryReviewsAndWarrantyByDealerAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? scopedDealerId,
        Guid[] nonDeletedDealerIds,
        CancellationToken cancellationToken)
    {
        var reviewRows = await BuildScopedReviewsQuery(fromUtc, toUtc, scopedDealerId, nonDeletedDealerIds)
            .GroupBy(sr => sr.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                TotalReviewsCount = g.Count(),
                RatingSum = g.Sum(x => x.Rating),
                FiveStarCount = g.Count(x => x.Rating == 5),
                RecommendedCount = g.Count(x => x.Rating >= 4),
                UnansweredCount = g.Count(x => x.StaffResponse == null || x.StaffResponse == "")
            })
            .ToListAsync(cancellationToken);

        var reviewsByDealer = reviewRows.ToDictionary(
            x => x.DealerId,
            x => new DealerReviewRollup(
                TotalReviewsCount: x.TotalReviewsCount,
                RatingSum: x.RatingSum,
                FiveStarCount: x.FiveStarCount,
                RecommendedCount: x.RecommendedCount,
                UnansweredCount: x.UnansweredCount));

        var warrantyGroups = await BuildScopedWarrantyClaimsQuery(fromUtc, toUtc, scopedDealerId, nonDeletedDealerIds)
            .GroupBy(wc => new { wc.DealerId, wc.Status, wc.DefectCategory })
            .Select(g => new WarrantyGroupSlice(
                g.Key.DealerId,
                g.Key.Status,
                g.Key.DefectCategory,
                g.Count(),
                g.Sum(x => x.CoveredAmountVnd)))
            .ToListAsync(cancellationToken);

        var warrantyByDealer = warrantyGroups
            .GroupBy(x => x.DealerId)
            .ToDictionary(
                g => g.Key,
                g => new DealerWarrantyRollup(
                    TotalClaims: g.Sum(x => x.Count),
                    OpenClaims: g.Where(x => IsOpenWarrantyStatus(x.Status)).Sum(x => x.Count),
                    CompletedClaims: g.Where(x => x.Status == WarrantyClaimStatus.Completed).Sum(x => x.Count),
                    RejectedClaims: g.Where(x => x.Status == WarrantyClaimStatus.Rejected).Sum(x => x.Count),
                    ApprovedCoveredVnd: g.Where(x => IsCoveredWarrantyStatus(x.Status)).Sum(x => x.CoveredAmountVnd)));

        return (reviewsByDealer, warrantyByDealer, warrantyGroups);
    }

    private static bool IsOpenWarrantyStatus(WarrantyClaimStatus status) =>
        WarrantyClaimValidationRules.IsActiveStatus(status);

    private static bool IsCoveredWarrantyStatus(WarrantyClaimStatus status) =>
        status is WarrantyClaimStatus.Approved or WarrantyClaimStatus.Completed;

    private static decimal ComputeAverageCsatRating(int ratingSum, int totalReviewsCount) =>
        totalReviewsCount > 0
            ? Math.Round((decimal)ratingSum / totalReviewsCount, 2, MidpointRounding.AwayFromZero)
            : 0.00m;

    private static MonthlyTrendsAnalyticsDto BuildMonthlyTrendsDto(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? dealerId,
        IReadOnlyList<MonthlyTrendBucketDto> buckets) =>
        new(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            DealerId: dealerId,
            MonthLabels: buckets.Select(b => b.Month).ToArray(),
            ShowroomRevenueVnd: buckets.Select(b => b.ShowroomRevenueVnd).ToArray(),
            UnitsSoldCounts: buckets.Select(b => b.UnitsSoldCount).ToArray(),
            ServiceRevenueVnd: buckets.Select(b => b.ServiceRevenueVnd).ToArray(),
            InvoicesIssuedCounts: buckets.Select(b => b.InvoicesIssuedCount).ToArray(),
            CombinedRevenueVnd: buckets.Select(b => b.TotalRevenueVnd).ToArray(),
            BookingCounts: buckets.Select(b => b.TotalBookings).ToArray(),
            CompletedBookingCounts: buckets.Select(b => b.CompletedBookings).ToArray(),
            CancelledOrNoShowBookingCounts: buckets.Select(b => b.CancelledOrNoShowBookings).ToArray(),
            Months: buckets);
}
