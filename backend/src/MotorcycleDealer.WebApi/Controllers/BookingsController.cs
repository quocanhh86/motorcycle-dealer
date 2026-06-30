using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IServiceBookingService _bookingService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookingsController(
        IServiceBookingService bookingService,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpPost]
    [HasPermission(Permissions.Bookings.Create)]
    [ProducesResponseType(typeof(ApiEnvelope<BookingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<BookingDto>>> CreateBookingAsync(
        [FromBody] CreateServiceBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _bookingService.CreateBookingAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/bookings/{created.BookingId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<BookingDto>>> ListBookingsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? slotDate = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.ListBookingsAsync(
            new BookingQuery(dealerId, status, slotDate, pageNumber, pageSize),
            forceCustomerOwnScope: false,
            forceDealerQueueScope: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Bookings.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<BookingDto>>> ListMyBookingsAsync(
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.ListBookingsAsync(
            new BookingQuery(DealerId: null, Status: status, SlotDate: null, PageNumber: pageNumber, PageSize: pageSize),
            forceCustomerOwnScope: true,
            forceDealerQueueScope: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("dealer")]
    [HasPermission(Permissions.Bookings.ViewDealer)]
    [ProducesResponseType(typeof(PagedApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<BookingDto>>> ListDealerBookingsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? slotDate = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.ListBookingsAsync(
            new BookingQuery(dealerId, status, slotDate, pageNumber, pageSize),
            forceCustomerOwnScope: false,
            forceDealerQueueScope: true,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{bookingId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<BookingDto>>> GetBookingByIdAsync(
        [FromRoute] Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId, cancellationToken);
        return Ok(WrapSuccess(booking));
    }

    [HttpPost("{bookingId:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<BookingDto>>> CancelBookingAsync(
        [FromRoute] Guid bookingId,
        [FromBody] CancelBookingRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var cancelled = await _bookingService.CancelBookingAsync(bookingId, request, cancellationToken);
        return Ok(WrapSuccess(cancelled));
    }

    [HttpPatch("{bookingId:guid}/status")]
    [HasPermission(Permissions.Bookings.UpdateStatus)]
    [ProducesResponseType(typeof(ApiEnvelope<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<BookingDto>>> UpdateBookingStatusAsync(
        [FromRoute] Guid bookingId,
        [FromBody] UpdateBookingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _bookingService.UpdateBookingStatusAsync(bookingId, request, cancellationToken);
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
