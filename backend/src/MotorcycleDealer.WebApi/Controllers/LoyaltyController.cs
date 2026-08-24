using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/loyalty")]
public sealed class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoyaltyController(
        ILoyaltyService loyaltyService,
        IDateTimeProvider dateTimeProvider)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("tiers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<LoyaltyTierDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<LoyaltyTierDto>>>> ListTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var tiers = await _loyaltyService.ListTiersAsync(cancellationToken);
        return Ok(WrapSuccess(tiers));
    }

    [HttpPut("tiers/{tierId:guid}")]
    [HasPermission(Permissions.Loyalty.ManageTiers)]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyTierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<LoyaltyTierDto>>> UpdateTierAsync(
        [FromRoute] Guid tierId,
        [FromBody] UpdateLoyaltyTierRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _loyaltyService.UpdateTierAsync(tierId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    [HttpGet("me")]
    [HttpGet("account")]
    [HasPermission(Permissions.Loyalty.ViewOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyAccountSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<LoyaltyAccountSummaryDto>>> GetMyLoyaltySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await _loyaltyService.GetMyLoyaltySummaryAsync(cancellationToken);
        return Ok(WrapSuccess(summary));
    }

    [HttpGet("ledger")]
    [HasPermission(Permissions.Loyalty.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<PointLedgerEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<PointLedgerEntryDto>>> ListMyPointLedgerAsync(
        [FromQuery] string? transactionType = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new PointLedgerQuery(
            TransactionType: transactionType,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var paged = await _loyaltyService.ListMyPointLedgerAsync(query, cancellationToken);
        return Ok(WrapPagedSuccess(paged));
    }

    [HttpGet("customers/{customerId:guid}")]
    [HasPermission(Permissions.Loyalty.ViewDealerCustomer)]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerLoyaltyInspectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<CustomerLoyaltyInspectionDto>>> InspectCustomerLoyaltyAsync(
        [FromRoute] Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var inspection = await _loyaltyService.GetCustomerLoyaltyForInspectionAsync(customerId, cancellationToken);
        return Ok(WrapSuccess(inspection));
    }

    [HttpPost("adjustments")]
    [HasPermission(Permissions.Loyalty.AdjustPoints)]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyAdjustmentResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<LoyaltyAdjustmentResultDto>>> AdjustPointsAsync(
        [FromBody] AdminPointAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var effectiveRequest = ApplyHeaderIdempotencyKey(request);
        var result = await _loyaltyService.AdjustCustomerPointsAsync(effectiveRequest, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(result));
    }

    [HttpPost("accounts/{accountId:guid}/adjustments")]
    [HasPermission(Permissions.Loyalty.AdjustPoints)]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyAdjustmentResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<LoyaltyAdjustmentResultDto>>> AdjustAccountPointsAsync(
        [FromRoute] Guid accountId,
        [FromBody] AdminPointAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var effectiveRequest = ApplyHeaderIdempotencyKey(request with { LoyaltyAccountId = accountId });
        var result = await _loyaltyService.AdjustCustomerPointsAsync(effectiveRequest, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(result));
    }

    private AdminPointAdjustmentRequest ApplyHeaderIdempotencyKey(AdminPointAdjustmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IdempotencyKey is null &&
            Request.Headers.TryGetValue("Idempotency-Key", out var headerValues) &&
            !string.IsNullOrWhiteSpace(headerValues.ToString()))
        {
            return request with { IdempotencyKey = headerValues.ToString().Trim() };
        }

        return request;
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));

    private PagedApiEnvelope<T> WrapPagedSuccess<T>(PagedResult<T> result) =>
        PagedApiEnvelope.Success(
            result,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
