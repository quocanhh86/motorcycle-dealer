using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Infrastructure.Services.Analytics;
using MotorcycleDealer.WebApi.Security;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests.Analytics;

public sealed class AdminAnalyticsBoundaryTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 8, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void BuildDealerAnalyticsCsv_ShouldEmitUtf8Bom_Rfc4180Crlf_Exact17ColumnHeader_AndInvariantDecimals()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");

            var rows = new[]
            {
                new DealerAnalyticsRowDto(
                    DealerId: Guid.Parse("22222222-2222-2222-2222-222222222201"),
                    DealerCode: "DLR-HCM-01",
                    DealerName: "Apex Moto Saigon Center",
                    City: "District 1, Ho Chi Minh City",
                    IsActive: true,
                    UnitsSoldCount: 3,
                    ShowroomRevenueVnd: 285000000.50m,
                    ShowroomDiscountVnd: 1500000.00m,
                    InvoicesIssuedCount: 5,
                    ServiceRevenueVnd: 4250000.25m,
                    ServiceVoucherDiscountVnd: 300000.00m,
                    TotalVoucherDiscountVnd: 1800000.00m,
                    TotalRevenueVnd: 289250000.75m,
                    RevenueSharePct: 100.00m,
                    Pending: 1,
                    Confirmed: 1,
                    CheckedIn: 0,
                    InProgress: 0,
                    Completed: 5,
                    Cancelled: 1,
                    NoShow: 1,
                    ActiveBookings: 2,
                    CancelledOrNoShowBookings: 2,
                    TotalBookings: 9,
                    CompletionRatePct: 55.56m,
                    CancellationRatePct: 22.22m,
                    AverageCsatRating: 4.80m,
                    TotalServiceReviewsCount: 5,
                    FiveStarReviewsCount: 4,
                    UnansweredReviewsCount: 1,
                    OpenWarrantyClaimsCount: 1,
                    CompletedWarrantyClaimsCount: 2,
                    ApprovedWarrantyCoveredVnd: 850000.00m)
            };

            var file = AnalyticsCsvExporter.BuildDealerAnalyticsCsv(rows, FixedUtcNow);

            Assert.Equal("text/csv; charset=utf-8", file.ContentType);
            Assert.Equal("dealer-analytics-20260815.csv", file.FileName);
            Assert.True(file.ContentBytes.Length > 3);
            Assert.Equal(0xEF, file.ContentBytes[0]);
            Assert.Equal(0xBB, file.ContentBytes[1]);
            Assert.Equal(0xBF, file.ContentBytes[2]);

            var csvBody = Encoding.UTF8.GetString(file.ContentBytes, 3, file.ContentBytes.Length - 3);
            Assert.Contains("\r\n", csvBody, StringComparison.Ordinal);

            var lines = csvBody.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.Equal(17, AnalyticsCsvExporter.HeaderRow.Split(',').Length);
            Assert.Equal(AnalyticsCsvExporter.HeaderRow, lines[0]);
            Assert.Equal(
                "DLR-HCM-01,Apex Moto Saigon Center,\"District 1, Ho Chi Minh City\",3,285000000.50,5,4250000.25,289250000.75,9,5,2,55.56,22.22,4.80,5,1,850000.00",
                lines[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("=CMD()",'0', "0.00", 0, 0, "0.00", "\"'=CMD()\"")]
    [InlineData("+SUM(A1:A9)", '5', "5.00", 12, 3, "1250000.50", "\"'+SUM(A1:A9)\"")]
    [InlineData("-100+200", '4', "4.25", 4, 1, "350000.00", "\"'-100+200\"")]
    [InlineData("@HYPERLINK(\"x\")", '3', "3.67", 3, 2, "480000.75", "\"'@HYPERLINK(\"\"x\"\")\"")]
    [InlineData("  =CMD()", '5', "4.90", 10, 0, "0.00", "\"'  =CMD()\"")]
    [InlineData("\t+EXEC()", '4', "4.00", 1, 1, "99000.99", "\"'\t+EXEC()\"")]
    [InlineData("\r-EXEC()", '2', "2.50", 2, 0, "0.00", "\"'\r-EXEC()\"")]
    [InlineData("Flagship \"Saigon\", Hub", '5', "4.75", 8, 2, "1650000.00", "\"Flagship \"\"Saigon\"\", Hub\"")]
    public void BuildDealerAnalyticsCsv_17ColumnMatrix_ShouldNeutralizeFormulaInjection_QuoteCommasAndQuotes_AndFormatInvariantCsatWarrantyDecimals(
        string rawDealerName,
        char _,
        string csatRaw,
        int reviewsCount,
        int openClaimsCount,
        string coveredVndRaw,
        string expectedEscapedNameCell)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var csat = decimal.Parse(csatRaw, CultureInfo.InvariantCulture);
            var coveredVnd = decimal.Parse(coveredVndRaw, CultureInfo.InvariantCulture);

            var row = new DealerAnalyticsRowDto(
                DealerId: Guid.Parse("22222222-2222-2222-2222-222222222299"),
                DealerCode: "DLR-MTX-01",
                DealerName: rawDealerName,
                City: "Ho Chi Minh City",
                IsActive: true,
                UnitsSoldCount: 1,
                ShowroomRevenueVnd: 100000000.00m,
                ShowroomDiscountVnd: 0.00m,
                InvoicesIssuedCount: 2,
                ServiceRevenueVnd: 1500000.00m,
                ServiceVoucherDiscountVnd: 0.00m,
                TotalVoucherDiscountVnd: 0.00m,
                TotalRevenueVnd: 101500000.00m,
                RevenueSharePct: 100.00m,
                Pending: 0,
                Confirmed: 0,
                CheckedIn: 0,
                InProgress: 0,
                Completed: 2,
                Cancelled: 0,
                NoShow: 0,
                ActiveBookings: 0,
                CancelledOrNoShowBookings: 0,
                TotalBookings: 2,
                CompletionRatePct: 100.00m,
                CancellationRatePct: 0.00m,
                AverageCsatRating: csat,
                TotalServiceReviewsCount: reviewsCount,
                OpenWarrantyClaimsCount: openClaimsCount,
                ApprovedWarrantyCoveredVnd: coveredVnd);

            var csvFile = AnalyticsCsvExporter.BuildDealerAnalyticsCsv([row], FixedUtcNow);
            var body = Encoding.UTF8.GetString(csvFile.ContentBytes, 3, csvFile.ContentBytes.Length - 3);
            var lines = body.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, lines.Length);
            Assert.Equal(17, lines[0].Split(',').Length);
            Assert.StartsWith($"DLR-MTX-01,{expectedEscapedNameCell},Ho Chi Minh City,", lines[1], StringComparison.Ordinal);
            Assert.EndsWith($",{csatRaw},{reviewsCount},{openClaimsCount},{coveredVndRaw}", lines[1], StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("=CMD|' /C calc'!A0", "\"'=CMD|' /C calc'!A0\"")]
    [InlineData("=1+1", "\"'=1+1\"")]
    [InlineData("+SUM(A1:A10)", "\"'+SUM(A1:A10)\"")]
    [InlineData("-1+1", "\"'-1+1\"")]
    [InlineData("@SUM(1,1)", "\"'@SUM(1,1)\"")]
    [InlineData("\t=1+1", "\"'\t=1+1\"")]
    [InlineData("\r=1+1", "\"'\r=1+1\"")]
    [InlineData("   =HYPERLINK(\"http://evil.example\")", "\"'   =HYPERLINK(\"\"http://evil.example\"\")\"")]
    [InlineData(" +cmd", "\"' +cmd\"")]
    [InlineData(" -cmd", "\"' -cmd\"")]
    [InlineData(" @cmd", "\"' @cmd\"")]
    public void FormatTextCell_ShouldNeutralizeOwaspFormulaInjectionVectors_AndQuoteSingleQuotePrefix(
        string maliciousInput,
        string expectedFormattedCell)
    {
        Assert.True(AnalyticsCsvExporter.IsFormulaInjectionRisk(maliciousInput));
        var actual = AnalyticsCsvExporter.FormatTextCell(maliciousInput);
        Assert.Equal(expectedFormattedCell, actual);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Hanoi", "Hanoi")]
    [InlineData("Đà Nẵng Coastal Hub", "Đà Nẵng Coastal Hub")]
    [InlineData("District 1, Ho Chi Minh City", "\"District 1, Ho Chi Minh City\"")]
    [InlineData("Apex \"Flagship\" Store", "\"Apex \"\"Flagship\"\" Store\"")]
    [InlineData("O'Connor Motors", "\"O'Connor Motors\"")]
    [InlineData("Line1\nLine2", "\"Line1\nLine2\"")]
    [InlineData(" LeadingSpace", "\" LeadingSpace\"")]
    [InlineData("TrailingSpace ", "\"TrailingSpace \"")]
    public void FormatTextCell_ShouldFollowRfc4180QuotingAndEscapingRules_ForNonFormulaStrings(
        string? rawInput,
        string expectedFormattedCell)
    {
        Assert.False(AnalyticsCsvExporter.IsFormulaInjectionRisk(rawInput));
        var actual = AnalyticsCsvExporter.FormatTextCell(rawInput);
        Assert.Equal(expectedFormattedCell, actual);
    }

    [Fact]
    public void ValidateAndResolveStandardWindow_WhenOmitted_ShouldDefaultToLast180Days()
    {
        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(
            new AnalyticsFilterQuery(),
            FixedUtcNow);

        Assert.Equal(FixedUtcNow.AddDays(-180), fromUtc);
        Assert.Equal(FixedUtcNow, toUtc);
        Assert.Null(dealerId);
    }

    [Fact]
    public void ValidateAndResolveMonthlyTrendsWindow_WhenOmitted_ShouldDefaultToExact12CalendarMonths()
    {
        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveMonthlyTrendsWindow(
            new AnalyticsFilterQuery(),
            FixedUtcNow);

        Assert.Equal(new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero), fromUtc);
        Assert.Equal(FixedUtcNow, toUtc);
        Assert.Null(dealerId);

        var spine = AnalyticsMath.BuildContiguousMonthSpine(fromUtc, toUtc);
        Assert.Equal(12, spine.Count);
        Assert.Equal("2025-09", spine[0].Label);
        Assert.Equal("2026-08", spine[^1].Label);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(180.0)]
    [InlineData(365.0)]
    [InlineData(730.99)]
    [InlineData(731.0)]
    public void ValidateAndResolveStandardWindow_ShouldAcceptValidBoundarySpans(double spanDays)
    {
        var query = new AnalyticsFilterQuery
        {
            FromUtc = FixedUtcNow - TimeSpan.FromDays(spanDays),
            ToUtc = FixedUtcNow,
            DealerId = Guid.Parse("22222222-2222-2222-2222-222222222202")
        };

        var (fromUtc, toUtc, dealerId) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(query, FixedUtcNow);
        Assert.Equal(FixedUtcNow - TimeSpan.FromDays(spanDays), fromUtc);
        Assert.Equal(FixedUtcNow, toUtc);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222202"), dealerId);
    }

    [Theory]
    [InlineData("2024-02-29T12:00:00Z", "2026-03-01T12:00:00Z", true)]
    [InlineData("2024-02-29T12:00:00Z", "2026-03-01T12:14:24Z", false)]
    [InlineData("2024-08-14T10:30:00Z", "2026-08-15T10:30:00Z", true)]
    [InlineData("2024-08-14T10:15:36Z", "2026-08-15T10:30:00Z", false)]
    public void ValidateAndResolveStandardWindow_ShouldEnforceLeapYearAndFractional731DayCap(
        string rawFromUtc,
        string rawToUtc,
        bool shouldSucceed)
    {
        var query = new AnalyticsFilterQuery
        {
            FromUtc = DateTimeOffset.Parse(rawFromUtc, CultureInfo.InvariantCulture),
            ToUtc = DateTimeOffset.Parse(rawToUtc, CultureInfo.InvariantCulture)
        };

        if (shouldSucceed)
        {
            var (fromUtc, toUtc, _) = AdminAnalyticsValidators.ValidateAndResolveStandardWindow(query, FixedUtcNow);
            Assert.True((toUtc - fromUtc).TotalDays <= AnalyticsConstants.MaxWindowDays);
        }
        else
        {
            var ex = Assert.Throws<ValidationException>(
                () => AdminAnalyticsValidators.ValidateAndResolveStandardWindow(query, FixedUtcNow));
            Assert.Contains(ex.Errors, e => e.PropertyName.Equals("ToUtc", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AnalyticsFilterQueryValidator_ShouldRejectInvertedWindow_ExceedingMaxDays_AndEmptyDealerId()
    {
        var invertedQuery = new AnalyticsFilterQuery
        {
            FromUtc = FixedUtcNow.AddSeconds(1),
            ToUtc = FixedUtcNow
        };
        var invertedEx = Assert.Throws<ValidationException>(
            () => AdminAnalyticsValidators.ValidateAndResolveStandardWindow(invertedQuery, FixedUtcNow));
        Assert.Contains(invertedEx.Errors, e => e.PropertyName.Equals("FromUtc", StringComparison.OrdinalIgnoreCase));

        var oversizedQuery = new AnalyticsFilterQuery
        {
            FromUtc = FixedUtcNow - TimeSpan.FromDays(731.01),
            ToUtc = FixedUtcNow
        };
        var oversizedEx = Assert.Throws<ValidationException>(
            () => AdminAnalyticsValidators.ValidateAndResolveStandardWindow(oversizedQuery, FixedUtcNow));
        Assert.Contains(oversizedEx.Errors, e => e.PropertyName.Equals("ToUtc", StringComparison.OrdinalIgnoreCase));

        var partialFutureFromQuery = new AnalyticsFilterQuery
        {
            FromUtc = FixedUtcNow.AddDays(2)
        };
        Assert.Throws<ValidationException>(
            () => AdminAnalyticsValidators.ValidateAndResolveMonthlyTrendsWindow(partialFutureFromQuery, FixedUtcNow));

        var emptyDealerQuery = new AnalyticsFilterQuery
        {
            DealerId = Guid.Empty
        };
        var emptyDealerEx = Assert.Throws<ValidationException>(
            () => AdminAnalyticsValidators.ValidateAndResolveStandardWindow(emptyDealerQuery, FixedUtcNow));
        Assert.Contains(emptyDealerEx.Errors, e => e.PropertyName.Equals("DealerId", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-5, 10, "0.00")]
    [InlineData(5, -10, "0.00")]
    [InlineData(-5, -10, "0.00")]
    [InlineData(0, 0, "0.00")]
    [InlineData(0, 10, "0.00")]
    [InlineData(5, 0, "0.00")]
    [InlineData(1, 800, "0.13")]
    [InlineData(3, 800, "0.38")]
    [InlineData(5, 800, "0.63")]
    [InlineData(1, 3, "33.33")]
    [InlineData(2, 3, "66.67")]
    [InlineData(3, 3, "100.00")]
    [InlineData(1, 8, "12.50")]
    public void ComputeRatePct_ShouldBeDivideByZeroSafe_GuardNegatives_AndRoundHalfCentsAwayFromZero(
        int numerator,
        int denominator,
        string expectedDecimal)
    {
        var expected = decimal.Parse(expectedDecimal, CultureInfo.InvariantCulture);
        Assert.Equal(expected, AnalyticsMath.ComputeRatePct(numerator, denominator));
    }

    [Theory]
    [InlineData("-100.00", "500.00", "0.00")]
    [InlineData("100.00", "-500.00", "0.00")]
    [InlineData("0.00", "0.00", "0.00")]
    [InlineData("1.00", "800.00", "0.13")]
    [InlineData("3.00", "800.00", "0.38")]
    [InlineData("5.00", "800.00", "0.63")]
    [InlineData("25000000.00", "100000000.00", "25.00")]
    [InlineData("10000000.00", "30000000.00", "33.33")]
    public void ComputeSharePct_ShouldRoundHalfCentsAwayFromZero_AndGuardZeroOrNegativeInputs(
        string rawPart,
        string rawTotal,
        string expectedDecimal)
    {
        var part = decimal.Parse(rawPart, CultureInfo.InvariantCulture);
        var total = decimal.Parse(rawTotal, CultureInfo.InvariantCulture);
        var expected = decimal.Parse(expectedDecimal, CultureInfo.InvariantCulture);
        Assert.Equal(expected, AnalyticsMath.ComputeSharePct(part, total));
    }

    [Theory]
    [InlineData(-10, 0.00)]
    [InlineData(0, 0.00)]
    [InlineData(1, 500.00)]
    [InlineData(649, 324500.00)]
    [InlineData(5000, 2500000.00)]
    public void ComputePointsLiabilityVnd_ShouldApplyCanonical500VndPerPointParity(
        long availablePoints,
        decimal expectedVnd)
    {
        Assert.Equal(expectedVnd, AnalyticsMath.ComputePointsLiabilityVnd(availablePoints));
    }

    [Fact]
    public void BuildContiguousMonthSpine_ShouldCrossYearBoundaryWithoutGaps_AndHandleTimezoneOffsets()
    {
        var fromPlusSeven = new DateTimeOffset(2025, 11, 15, 8, 0, 0, TimeSpan.FromHours(7));
        var toPlusSeven = new DateTimeOffset(2026, 3, 2, 19, 0, 0, TimeSpan.FromHours(7));

        var spine = AnalyticsMath.BuildContiguousMonthSpine(fromPlusSeven, toPlusSeven);
        Assert.Equal(
            ["2025-11", "2025-12", "2026-01", "2026-02", "2026-03"],
            spine.Select(s => s.Label).ToArray());

        Assert.Empty(AnalyticsMath.BuildContiguousMonthSpine(toPlusSeven, fromPlusSeven));
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(60, 60)]
    [InlineData(3600, 3600)]
    [InlineData(86400, 3600)]
    public void ResolveEffectiveWindowSeconds_ShouldClampStrictlyToOneThrough3600Seconds(
        int configuredSeconds,
        int expectedSeconds)
    {
        var options = new AuthRateLimitOptions { WindowSeconds = configuredSeconds };
        Assert.Equal(expectedSeconds, options.ResolveEffectiveWindowSeconds());
    }

    [Theory]
    [InlineData(-10, true, false, 1)]
    [InlineData(0, false, false, 1)]
    [InlineData(3, true, true, 3)]
    [InlineData(30, false, false, 30)]
    [InlineData(30, false, true, 300)]
    public void ResolveEffectivePermitLimit_ShouldEnforcePositiveFloorAndInMemoryFallback(
        int configuredPermitLimit,
        bool hasExplicitConfig,
        bool isInMemoryPipe,
        int expectedPermitLimit)
    {
        var options = new AuthRateLimitOptions { PermitLimit = configuredPermitLimit };
        Assert.Equal(
            expectedPermitLimit,
            options.ResolveEffectivePermitLimit(hasExplicitConfig, isInMemoryPipe));
    }

    [Fact]
    public void AuthRateLimitOptions_ShouldEnforcePartitionBounds_WhitespaceFallback_AndSecurityHeaderConstants()
    {
        Assert.Equal(["Content-Disposition", "Retry-After", "Location"], AuthRateLimitOptions.ExposedCorsHeaders);

        var context = new DefaultHttpContext();
        AuthRateLimitOptions.ApplySecurityResponseHeaders(context.Response.Headers);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());

        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.42");
        context.Request.Headers[AuthRateLimitOptions.TestPartitionHeaderName] = "suite-alpha";

        var devEnv = new StubHostEnvironment(Environments.Development);
        var prodEnv = new StubHostEnvironment(Environments.Production);

        Assert.Equal("test:suite-alpha", AuthRateLimitOptions.ResolvePartitionKey(context, devEnv));
        Assert.Equal("203.0.113.42", AuthRateLimitOptions.ResolvePartitionKey(context, prodEnv));

        // Whitespace-only header falls back to client IP
        context.Request.Headers[AuthRateLimitOptions.TestPartitionHeaderName] = "   \t  ";
        Assert.Equal("203.0.113.42", AuthRateLimitOptions.ResolvePartitionKey(context, devEnv));

        // Exact 80-char boundary accepted; 81-char boundary falls back to client IP / "local"
        var exactMaxPartition = new string('p', AuthRateLimitOptions.MaxTestPartitionKeyLength);
        context.Request.Headers[AuthRateLimitOptions.TestPartitionHeaderName] = exactMaxPartition;
        Assert.Equal($"test:{exactMaxPartition}", AuthRateLimitOptions.ResolvePartitionKey(context, devEnv));

        var oversizedPartition = new string('x', AuthRateLimitOptions.MaxTestPartitionKeyLength + 1);
        context.Request.Headers[AuthRateLimitOptions.TestPartitionHeaderName] = oversizedPartition;
        Assert.Equal("203.0.113.42", AuthRateLimitOptions.ResolvePartitionKey(context, devEnv));

        context.Connection.RemoteIpAddress = null;
        Assert.Equal("local", AuthRateLimitOptions.ResolvePartitionKey(context, devEnv));
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "MotorcycleDealer.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
