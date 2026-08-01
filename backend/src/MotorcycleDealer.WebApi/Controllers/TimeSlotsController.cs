using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/time-slots")]
public sealed class TimeSlotsController : ControllerBase
{
    private readonly ITimeSlotService _timeSlotService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TimeSlotsController(
        ITimeSlotService timeSlotService,
        IDateTimeProvider dateTimeProvider)
    {
        _timeSlotService = timeSlotService ?? throw new ArgumentNullException(nameof(timeSlotService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>>> ListTimeSlotsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] DateOnly? slotDate = null,
        [FromQuery] DateOnly? date = null,
        [FromQuery] bool? availableOnly = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveSlotDate = slotDate ?? date;
        var slots = await _timeSlotService.ListTimeSlotsAsync(
            new TimeSlotQuery(
                DealerId: dealerId,
                FromDate: fromDate,
                ToDate: toDate,
                SlotDate: effectiveSlotDate,
                AvailableOnly: availableOnly),
            cancellationToken);

        return Ok(WrapSuccess(slots));
    }

    [HttpGet("dealer")]
    [HasPermission(Permissions.TimeSlots.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>>> ListDealerTimeSlotsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var slots = await _timeSlotService.ListDealerTimeSlotsAsync(
            dealerId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(WrapSuccess(slots));
    }

    [HttpPost]
    [HasPermission(Permissions.TimeSlots.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceTimeSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<ServiceTimeSlotDto>>> CreateTimeSlotAsync(
        [FromBody] CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _timeSlotService.CreateTimeSlotAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/time-slots/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPost("generate")]
    [HttpPost("dealer/generate")]
    [HasPermission(Permissions.TimeSlots.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<ServiceTimeSlotDto>>>> GenerateTimeSlotsAsync(
        [FromBody] GenerateTimeSlotsRequest request,
        CancellationToken cancellationToken = default)
    {
        var generated = await _timeSlotService.GenerateTimeSlotsAsync(request, cancellationToken);
        Response.Headers.Location = "/api/v1/time-slots/dealer";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(generated));
    }

    [HttpPatch("{slotId:guid}")]
    [HasPermission(Permissions.TimeSlots.ManageDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceTimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiEnvelope<ServiceTimeSlotDto>>> UpdateTimeSlotAsync(
        [FromRoute] Guid slotId,
        [FromBody] UpdateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _timeSlotService.UpdateTimeSlotAsync(slotId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
