using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/dealers")]
public sealed class DealersController : ControllerBase
{
    private readonly IDealerAdministrationService _dealerService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DealersController(
        IDealerAdministrationService dealerService,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dealerService = dealerService ?? throw new ArgumentNullException(nameof(dealerService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DealerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<DealerDto>>>> ListDealersAsync(
        [FromQuery] string? city = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var canIncludeInactive = includeInactive &&
                                 _currentUserContext.IsAuthenticated &&
                                 (_currentUserContext.IsAdmin ||
                                  _currentUserContext.HasPermission(Permissions.Dealers.ManageAll));

        var dealers = await _dealerService.ListDealersAsync(city, canIncludeInactive, cancellationToken);
        return Ok(WrapSuccess(dealers));
    }

    [HttpGet("{dealerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<DealerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<DealerDto>>> GetDealerByIdAsync(
        [FromRoute] Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        var allowInactive = _currentUserContext.IsAuthenticated &&
                            (_currentUserContext.IsAdmin ||
                             _currentUserContext.HasPermission(Permissions.Dealers.ManageAll));

        var dealer = await _dealerService.GetDealerByIdAsync(dealerId, allowInactive, cancellationToken);
        return Ok(WrapSuccess(dealer));
    }

    [HttpPost]
    [HasPermission(Permissions.Dealers.ManageAll)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<DealerDto>>> CreateDealerAsync(
        [FromBody] UpsertDealerRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _dealerService.CreateDealerAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/dealers/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPut("{dealerId:guid}")]
    [HasPermission(Permissions.Dealers.ManageAll)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<DealerDto>>> UpdateDealerAsync(
        [FromRoute] Guid dealerId,
        [FromBody] UpsertDealerRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _dealerService.UpdateDealerAsync(dealerId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    [HttpGet("{dealerId:guid}/staff")]
    [HasPermission(Permissions.Users.ManageStaff)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DealerStaffDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<DealerStaffDto>>>> ListDealerStaffAsync(
        [FromRoute] Guid dealerId,
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var staff = await _dealerService.ListDealerStaffAsync(dealerId, includeInactive, cancellationToken);
        return Ok(WrapSuccess(staff));
    }

    [HttpPost("{dealerId:guid}/staff")]
    [HasPermission(Permissions.Users.ManageStaff)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerStaffDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<DealerStaffDto>>> ProvisionDealerStaffAsync(
        [FromRoute] Guid dealerId,
        [FromBody] CreateDealerStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminUserId = _currentUserContext.RequireUserId();
        var created = await _dealerService.ProvisionDealerStaffAsync(
            dealerId,
            request,
            adminUserId,
            cancellationToken);

        Response.Headers.Location = $"/api/v1/dealers/{dealerId}/staff";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPatch("{dealerId:guid}/staff/{userId:guid}/status")]
    [HasPermission(Permissions.Users.ManageStaff)]
    [ProducesResponseType(typeof(ApiEnvelope<DealerStaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<DealerStaffDto>>> UpdateDealerStaffStatusAsync(
        [FromRoute] Guid dealerId,
        [FromRoute] Guid userId,
        [FromBody] UpdateStaffStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _dealerService.UpdateDealerStaffStatusAsync(
            dealerId,
            userId,
            request,
            cancellationToken);

        return Ok(WrapSuccess(updated));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
