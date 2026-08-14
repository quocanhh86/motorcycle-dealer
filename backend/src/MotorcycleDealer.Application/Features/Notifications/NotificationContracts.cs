using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    Guid NotificationId,
    Guid? RecipientUserId,
    Guid? TargetDealerId,
    string? TargetRole,
    string EventType,
    string Title,
    string Message,
    string? PayloadJson,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset OccurredAtUtc);

public sealed record NotificationListQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    bool UnreadOnly = false,
    DateTimeOffset? SinceUtc = null,
    Guid? DealerId = null) : PaginationQuery(PageNumber, PageSize);

public sealed record NotificationInboxPage(
    PagedResult<NotificationDto> Page,
    int UnreadCount);

public sealed record NotificationFeedMeta(
    DateTimeOffset TimestampUtc,
    string TraceId,
    int UnreadCount);

public sealed record NotificationFeedEnvelope(
    bool Success,
    IReadOnlyList<NotificationDto> Data,
    int UnreadCount,
    PaginationMetadata Pagination,
    NotificationFeedMeta Meta)
{
    public static NotificationFeedEnvelope Create(
        NotificationInboxPage inboxPage,
        DateTimeOffset timestampUtc,
        string traceId)
    {
        ArgumentNullException.ThrowIfNull(inboxPage);

        return new NotificationFeedEnvelope(
            Success: true,
            Data: inboxPage.Page.Items,
            UnreadCount: inboxPage.UnreadCount,
            Pagination: inboxPage.Page.Pagination,
            Meta: new NotificationFeedMeta(
                TimestampUtc: timestampUtc,
                TraceId: traceId,
                UnreadCount: inboxPage.UnreadCount));
    }
}

public sealed record MarkNotificationReadResultDto(
    Guid Id,
    Guid NotificationId,
    bool IsRead,
    DateTimeOffset ReadAtUtc,
    int UnreadCount);

public sealed record MarkAllNotificationsReadResultDto(
    int MarkedReadCount,
    int UnreadCount,
    DateTimeOffset ReadAtUtc);

public interface INotificationInboxService
{
    Task<NotificationInboxPage> ListNotificationsAsync(
        NotificationListQuery query,
        bool forceDealerFeedOnly = false,
        CancellationToken cancellationToken = default);

    Task<MarkNotificationReadResultDto> MarkNotificationReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<MarkAllNotificationsReadResultDto> MarkAllNotificationsReadAsync(
        Guid? dealerId = null,
        CancellationToken cancellationToken = default);
}
