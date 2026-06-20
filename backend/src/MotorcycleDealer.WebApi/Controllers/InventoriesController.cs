using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/inventories")]
public sealed class InventoriesController : ControllerBase
{
    private readonly IDealerInventoryService _inventoryService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InventoriesController(
        IDealerInventoryService inventoryService,
        IDateTimeProvider dateTimeProvider)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedApiEnvelope<DealerInventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<DealerInventoryDto>>> ListInventoriesAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] Guid? modelId = null,
        [FromQuery] Guid? motorcycleModelId = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] string? vin = null,
        [FromQuery] string? search = null,
        [FromQuery] bool publicOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new DealerInventoryQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            DealerId: dealerId,
            ModelId: modelId ?? motorcycleModelId,
            StockStatus: stockStatus,
            Vin: vin,
            Search: search,
            PublicOnly: publicOnly);

        var result = await _inventoryService.ListInventoriesAsync(query, cancellationToken);
        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{inventoryId:guid}")]
    [HasPermission(Permissions.Inventories.ViewDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerInventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<DealerInventoryDto>>> GetInventoryByIdAsync(
        [FromRoute] Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetInventoryByIdAsync(inventoryId, cancellationToken);
        return Ok(WrapSuccess(item));
    }

    [HttpPost]
    [HasPermission(Permissions.Inventories.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerInventoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<DealerInventoryDto>>> CreateInventoryAsync(
        [FromBody] CreateDealerInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _inventoryService.CreateInventoryAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/inventories/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPatch("{inventoryId:guid}")]
    [HasPermission(Permissions.Inventories.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerInventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<DealerInventoryDto>>> UpdateInventoryAsync(
        [FromRoute] Guid inventoryId,
        [FromBody] UpdateDealerInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _inventoryService.UpdateInventoryAsync(inventoryId, request, cancellationToken);
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
