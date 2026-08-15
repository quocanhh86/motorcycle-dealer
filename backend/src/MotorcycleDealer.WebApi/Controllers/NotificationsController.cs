using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Notifications;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationInboxService _notificationInboxService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationsController(
        INotificationInboxService notificationInboxService,
        IDateTimeProvider dateTimeProvider)
    {
        _notificationInboxService = notificationInboxService ?? throw new ArgumentNullException(nameof(notificationInboxService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(NotificationFeedEnvelope), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationFeedEnvelope>> ListNotificationsAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] DateTimeOffset? sinceUtc = null,
        [FromQuery] Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new NotificationListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            UnreadOnly: unreadOnly,
            SinceUtc: sinceUtc,
            DealerId: dealerId);

        var inboxPage = await _notificationInboxService.ListNotificationsAsync(
            query,
            forceDealerFeedOnly: false,
            cancellationToken);

        return Ok(WrapFeedSuccess(inboxPage));
    }

    [HttpGet("dealer")]
    [HasPermission(Permissions.Notifications.ViewDealer)]
    [ProducesResponseType(typeof(NotificationFeedEnvelope), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationFeedEnvelope>> ListDealerNotificationsAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] DateTimeOffset? sinceUtc = null,
        [FromQuery] Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new NotificationListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            UnreadOnly: unreadOnly,
            SinceUtc: sinceUtc,
            DealerId: dealerId);

        var inboxPage = await _notificationInboxService.ListNotificationsAsync(
            query,
            forceDealerFeedOnly: true,
            cancellationToken);

        return Ok(WrapFeedSuccess(inboxPage));
    }

    [HttpPatch("{notificationId:guid}/read")]
    [HttpPost("{notificationId:guid}/read")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<MarkNotificationReadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<MarkNotificationReadResultDto>>> MarkNotificationReadAsync(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationInboxService.MarkNotificationReadAsync(notificationId, cancellationToken);
        return Ok(WrapSuccess(result));
    }

    [HttpPost("read-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<MarkAllNotificationsReadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<MarkAllNotificationsReadResultDto>>> MarkAllNotificationsReadAsync(
        [FromQuery] Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationInboxService.MarkAllNotificationsReadAsync(dealerId, cancellationToken);
        return Ok(WrapSuccess(result));
    }

    private NotificationFeedEnvelope WrapFeedSuccess(NotificationInboxPage inboxPage) =>
        NotificationFeedEnvelope.Create(
            inboxPage,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
