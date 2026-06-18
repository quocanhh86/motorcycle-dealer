using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/motorcycles")]
public sealed class MotorcyclesController : ControllerBase
{
    private readonly IShowroomCatalogService _catalogService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MotorcyclesController(
        IShowroomCatalogService catalogService,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedApiEnvelope<MotorcycleModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedApiEnvelope<MotorcycleModelDto>>> ListMotorcyclesAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] string? category = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var canManageModels = _currentUserContext.IsAuthenticated &&
                              (_currentUserContext.IsAdmin ||
                               _currentUserContext.HasPermission(Permissions.Showroom.ManageModels));

        var query = new ShowroomCatalogQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            Category: category,
            Brand: brand,
            Search: search,
            DealerId: dealerId,
            IncludeInactive: includeInactive);

        var result = await _catalogService.ListModelsAsync(query, canManageModels, cancellationToken);
        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{modelId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<MotorcycleModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<MotorcycleModelDto>>> GetMotorcycleModelByIdAsync(
        [FromRoute] Guid modelId,
        CancellationToken cancellationToken = default)
    {
        var canManageModels = _currentUserContext.IsAuthenticated &&
                              (_currentUserContext.IsAdmin ||
                               _currentUserContext.HasPermission(Permissions.Showroom.ManageModels));

        var model = await _catalogService.GetModelByIdAsync(modelId, canManageModels, cancellationToken);
        return Ok(WrapSuccess(model));
    }

    [HttpPost]
    [HasPermission(Permissions.Showroom.ManageModels)]
    [ProducesResponseType(typeof(ApiEnvelope<MotorcycleModelDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MotorcycleModelDto>>> CreateMotorcycleModelAsync(
        [FromBody] UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _catalogService.CreateModelAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/motorcycles/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPut("{modelId:guid}")]
    [HasPermission(Permissions.Showroom.ManageModels)]
    [ProducesResponseType(typeof(ApiEnvelope<MotorcycleModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MotorcycleModelDto>>> UpdateMotorcycleModelAsync(
        [FromRoute] Guid modelId,
        [FromBody] UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _catalogService.UpdateModelAsync(modelId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    [HttpGet("{modelId:guid}/maintenance-rules")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<MaintenanceRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<MaintenanceRuleDto>>>> ListModelMaintenanceRulesAsync(
        [FromRoute] Guid modelId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var canManageRules = includeInactive &&
                             _currentUserContext.IsAuthenticated &&
                             (_currentUserContext.IsAdmin ||
                              _currentUserContext.HasPermission(Permissions.Vehicles.ManageRules));

        var rules = await _catalogService.ListMaintenanceRulesAsync(modelId, canManageRules, cancellationToken);
        return Ok(WrapSuccess(rules));
    }

    [HttpPost("{modelId:guid}/maintenance-rules")]
    [HasPermission(Permissions.Vehicles.ManageRules)]
    [ProducesResponseType(typeof(ApiEnvelope<MaintenanceRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MaintenanceRuleDto>>> CreateMaintenanceRuleAsync(
        [FromRoute] Guid modelId,
        [FromBody] UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _catalogService.CreateMaintenanceRuleAsync(modelId, request, cancellationToken);
        Response.Headers.Location = $"/api/v1/motorcycles/{modelId}/maintenance-rules/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPut("{modelId:guid}/maintenance-rules/{ruleId:guid}")]
    [HasPermission(Permissions.Vehicles.ManageRules)]
    [ProducesResponseType(typeof(ApiEnvelope<MaintenanceRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MaintenanceRuleDto>>> UpdateMaintenanceRuleAsync(
        [FromRoute] Guid modelId,
        [FromRoute] Guid ruleId,
        [FromBody] UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _catalogService.UpdateMaintenanceRuleAsync(modelId, ruleId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    [HttpPut("{modelId:guid}/maintenance-rules")]
    [HasPermission(Permissions.Vehicles.ManageRules)]
    [ProducesResponseType(typeof(ApiEnvelope<MaintenanceRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<MaintenanceRuleDto>>> UpdateMaintenanceRuleFromBodyAsync(
        [FromRoute] Guid modelId,
        [FromBody] UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var bodyRuleId = request.ResolveBodyRuleId();
        if (bodyRuleId is null || bodyRuleId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure("ruleId", "ruleId is required when updating a maintenance rule without a route parameter.")
            ]);
        }

        var updated = await _catalogService.UpdateMaintenanceRuleAsync(modelId, bodyRuleId.Value, request, cancellationToken);
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
