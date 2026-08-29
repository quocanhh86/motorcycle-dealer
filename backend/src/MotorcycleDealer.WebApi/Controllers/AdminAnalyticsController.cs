using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/admin/analytics")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly IAdminAnalyticsService _analyticsService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AdminAnalyticsController(
        IAdminAnalyticsService analyticsService,
        IDateTimeProvider dateTimeProvider)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("overview")]
    [HttpGet("kpis")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<AnalyticsOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<AnalyticsOverviewDto>>> GetKpisOverviewAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var overview = await _analyticsService.GetOverviewAsync(query, cancellationToken);
        return Ok(WrapSuccess(overview));
    }

    [HttpGet("revenue-by-dealer")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<RevenueByDealerAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<RevenueByDealerAnalyticsDto>>> GetRevenueByDealerAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var revenue = await _analyticsService.GetRevenueByDealerAsync(query, cancellationToken);
        return Ok(WrapSuccess(revenue));
    }

    [HttpGet("bookings-by-dealer")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<BookingsByDealerAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<BookingsByDealerAnalyticsDto>>> GetBookingsByDealerAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var bookings = await _analyticsService.GetBookingsByDealerAsync(query, cancellationToken);
        return Ok(WrapSuccess(bookings));
    }

    [HttpGet("monthly-trends")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<MonthlyTrendsAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<MonthlyTrendsAnalyticsDto>>> GetMonthlyTrendsAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var trends = await _analyticsService.GetMonthlyTrendsAsync(query, cancellationToken);
        return Ok(WrapSuccess(trends));
    }

    [HttpGet("loyalty-distribution")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyDistributionAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<LoyaltyDistributionAnalyticsDto>>> GetLoyaltyDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var distribution = await _analyticsService.GetLoyaltyDistributionAsync(cancellationToken);
        return Ok(WrapSuccess(distribution));
    }

    [HttpGet("quality-warranty")]
    [HasPermission(Permissions.Analytics.ViewAllDealers)]
    [ProducesResponseType(typeof(ApiEnvelope<QualityWarrantyAnalyticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<QualityWarrantyAnalyticsDto>>> GetQualityWarrantyAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var qualityWarranty = await _analyticsService.GetQualityWarrantyAsync(query, cancellationToken);
        return Ok(WrapSuccess(qualityWarranty));
    }

    [HttpGet("export")]
    [HttpGet("export-csv")]
    [HasPermission(Permissions.Analytics.ExportReports)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCsvAsync(
        [FromQuery] AnalyticsFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var csvFile = await _analyticsService.ExportDealerAnalyticsCsvAsync(query, cancellationToken);
        return File(csvFile.ContentBytes, csvFile.ContentType, csvFile.FileName);
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
