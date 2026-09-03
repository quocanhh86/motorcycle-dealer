using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/spare-parts")]
public sealed class SparePartsController : ControllerBase
{
    private readonly ISparePartService _sparePartService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SparePartsController(
        ISparePartService sparePartService,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sparePartService = sparePartService ?? throw new ArgumentNullException(nameof(sparePartService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedApiEnvelope<SparePartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedApiEnvelope<SparePartDto>>> ListSparePartsAsync(
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] string? category = null,
        [FromQuery] Guid? compatibleModelId = null,
        [FromQuery] bool includeUniversal = true,
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var canManageCatalog = _currentUserContext.IsAuthenticated &&
                               (_currentUserContext.IsAdmin ||
                                _currentUserContext.HasPermission(Permissions.SpareParts.ManageCatalog));

        var effectivePageNumber = page ?? pageNumber;
        var query = new SparePartCatalogQuery(
            PageNumber: effectivePageNumber,
            PageSize: pageSize,
            Category: category,
            CompatibleModelId: compatibleModelId,
            IncludeUniversal: includeUniversal,
            Search: search,
            IncludeInactive: includeInactive);

        var result = await _sparePartService.ListSparePartsAsync(query, canManageCatalog, cancellationToken);
        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{partId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<SparePartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<SparePartDto>>> GetSparePartByIdAsync(
        [FromRoute] Guid partId,
        CancellationToken cancellationToken = default)
    {
        var canManageCatalog = _currentUserContext.IsAuthenticated &&
                               (_currentUserContext.IsAdmin ||
                                _currentUserContext.HasPermission(Permissions.SpareParts.ManageCatalog));

        var part = await _sparePartService.GetSparePartByIdAsync(partId, canManageCatalog, cancellationToken);
        return Ok(WrapSuccess(part));
    }

    [HttpPost]
    [HasPermission(Permissions.SpareParts.ManageCatalog)]
    [ProducesResponseType(typeof(ApiEnvelope<SparePartDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<SparePartDto>>> CreateSparePartAsync(
        [FromBody] UpsertSparePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _sparePartService.CreateSparePartAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/spare-parts/{created.PartId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPut("{partId:guid}")]
    [HasPermission(Permissions.SpareParts.ManageCatalog)]
    [ProducesResponseType(typeof(ApiEnvelope<SparePartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<SparePartDto>>> UpdateSparePartAsync(
        [FromRoute] Guid partId,
        [FromBody] UpsertSparePartRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _sparePartService.UpdateSparePartAsync(partId, request, cancellationToken);
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
