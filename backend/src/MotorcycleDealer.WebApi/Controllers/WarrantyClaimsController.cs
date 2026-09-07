using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/warranty-claims")]
public sealed class WarrantyClaimsController : ControllerBase
{
    private readonly IWarrantyClaimService _warrantyClaimService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WarrantyClaimsController(
        IWarrantyClaimService warrantyClaimService,
        IDateTimeProvider dateTimeProvider)
    {
        _warrantyClaimService = warrantyClaimService ?? throw new ArgumentNullException(nameof(warrantyClaimService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Warranty.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<WarrantyClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<WarrantyClaimDto>>> ListMyWarrantyClaimsAsync(
        [FromQuery] string? status = null,
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] Guid? customerVehicleId = null,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] string? defectCategory = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var effectiveVehicleId = vehicleId is { } vid && vid != Guid.Empty ? vid : customerVehicleId;

        var result = await _warrantyClaimService.ListWarrantyClaimsAsync(
            new WarrantyClaimListQuery(
                DealerId: dealerId,
                VehicleId: effectiveVehicleId,
                Status: status,
                DefectCategory: defectCategory,
                Search: search,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            forceOwnCustomerOnly: true,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet]
    [HasPermission(Permissions.Warranty.ViewDealer)]
    [ProducesResponseType(typeof(PagedApiEnvelope<WarrantyClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<WarrantyClaimDto>>> ListDealerWarrantyClaimsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? defectCategory = null,
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var result = await _warrantyClaimService.ListWarrantyClaimsAsync(
            new WarrantyClaimListQuery(
                DealerId: dealerId,
                VehicleId: vehicleId,
                Status: status,
                DefectCategory: defectCategory,
                Search: search,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{claimId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<WarrantyClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<WarrantyClaimDto>>> GetWarrantyClaimByIdAsync(
        [FromRoute] Guid claimId,
        CancellationToken cancellationToken = default)
    {
        var dto = await _warrantyClaimService.GetWarrantyClaimByIdAsync(claimId, cancellationToken);
        return Ok(WrapSuccess(dto));
    }

    [HttpPost]
    [HasPermission(Permissions.Warranty.CreateOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<WarrantyClaimDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<WarrantyClaimDto>>> CreateWarrantyClaimAsync(
        [FromBody] CreateWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _warrantyClaimService.CreateWarrantyClaimAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/warranty-claims/{created.ClaimId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPatch("{claimId:guid}/status")]
    [HasPermission(Permissions.Warranty.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<WarrantyClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<WarrantyClaimDto>>> UpdateWarrantyClaimStatusAsync(
        [FromRoute] Guid claimId,
        [FromBody] UpdateWarrantyClaimStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _warrantyClaimService.UpdateWarrantyClaimStatusAsync(claimId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
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
