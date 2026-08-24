using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Vouchers;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/vouchers")]
public sealed class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VouchersController(
        IVoucherService voucherService,
        IDateTimeProvider dateTimeProvider)
    {
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("catalog")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<VoucherCatalogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<VoucherCatalogDto>>>> ListCatalogAsync(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? applicableScope = null,
        CancellationToken cancellationToken = default)
    {
        var query = new VoucherCatalogQuery(
            IncludeInactive: includeInactive,
            ApplicableScope: applicableScope);

        var items = await _voucherService.ListCatalogAsync(query, cancellationToken);
        return Ok(WrapSuccess(items));
    }

    [HttpPost("catalog")]
    [HasPermission(Permissions.Loyalty.ManageVoucherCatalog)]
    [ProducesResponseType(typeof(ApiEnvelope<VoucherCatalogDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<VoucherCatalogDto>>> CreateCatalogItemAsync(
        [FromBody] UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _voucherService.CreateCatalogItemAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/vouchers/catalog/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPut("catalog/{catalogId:guid}")]
    [HasPermission(Permissions.Loyalty.ManageVoucherCatalog)]
    [ProducesResponseType(typeof(ApiEnvelope<VoucherCatalogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<VoucherCatalogDto>>> UpdateCatalogItemAsync(
        [FromRoute] Guid catalogId,
        [FromBody] UpsertVoucherCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _voucherService.UpdateCatalogItemAsync(catalogId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Loyalty.ViewOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerVoucherDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CustomerVoucherDto>>>> ListMyVouchersAsync(
        [FromQuery] string? status = null,
        [FromQuery] string? applicableScope = null,
        CancellationToken cancellationToken = default)
    {
        var query = new MyCustomerVouchersQuery(
            Status: status,
            ApplicableScope: applicableScope);

        var items = await _voucherService.ListMyVouchersAsync(query, cancellationToken);
        return Ok(WrapSuccess(items));
    }

    [HttpPost("redeem")]
    [HasPermission(Permissions.Loyalty.RedeemVoucher)]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerVoucherDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<CustomerVoucherDto>>> RedeemVoucherAsync(
        [FromBody] RedeemVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        var effectiveRequest = ApplyHeaderIdempotencyKey(request);
        var redeemed = await _voucherService.RedeemVoucherAsync(effectiveRequest, cancellationToken);
        Response.Headers.Location = $"/api/v1/vouchers/my?status=Available";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(redeemed));
    }

    private RedeemVoucherRequest ApplyHeaderIdempotencyKey(RedeemVoucherRequest request)
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
}
