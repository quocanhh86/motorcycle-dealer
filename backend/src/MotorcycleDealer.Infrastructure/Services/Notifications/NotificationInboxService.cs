using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Notifications;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Notifications;

public sealed class NotificationInboxService : INotificationInboxService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<NotificationListQuery> _queryValidator;

    public NotificationInboxService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<NotificationListQuery> queryValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _queryValidator = queryValidator ?? throw new ArgumentNullException(nameof(queryValidator));
    }

    public async Task<NotificationInboxPage> ListNotificationsAsync(
        NotificationListQuery query,
        bool forceDealerFeedOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = EnsureCallerCanAccessNotifications(forceDealerFeedOnly);
        var scopedBaseQuery = BuildCallerScopedQuery(
            _dbContext.Notifications.AsNoTracking(),
            callerUserId,
            query.DealerId,
            forceDealerFeedOnly);

        var unreadCount = await scopedBaseQuery.CountAsync(n => !n.IsRead, cancellationToken);

        var filteredQuery = scopedBaseQuery;
        if (query.UnreadOnly)
        {
            filteredQuery = filteredQuery.Where(n => !n.IsRead);
        }

        if (query.SinceUtc is { } sinceUtc)
        {
            filteredQuery = filteredQuery.Where(n => n.CreatedAtUtc >= sinceUtc);
        }

        var totalCount = query.UnreadOnly && query.SinceUtc is null
            ? unreadCount
            : await filteredQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyPage = PagedResult<NotificationDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);

            return new NotificationInboxPage(emptyPage, unreadCount);
        }

        var items = await filteredQuery
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Id,
                n.RecipientUserId,
                n.TargetDealerId,
                n.TargetRole,
                n.EventType,
                n.Title,
                n.Message,
                n.PayloadJson,
                n.IsRead,
                n.ReadAtUtc,
                n.CreatedAtUtc,
                n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var page = PagedResult<NotificationDto>.Create(
            items,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);

        return new NotificationInboxPage(page, unreadCount);
    }

    public async Task<MarkNotificationReadResultDto> MarkNotificationReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = EnsureCallerCanAccessNotifications(forceDealerFeedOnly: false);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            var utcNow = _dateTimeProvider.UtcNow;

            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
                ?? throw NotFoundException.ForEntity("Notification", notificationId);

            EnsureCallerCanMutateNotification(notification, callerUserId);

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = utcNow;
                await _dbContext.SaveChangesAsync(ct);
            }

            var readTimestamp = notification.ReadAtUtc ?? utcNow;
            var unreadCount = await BuildCallerScopedQuery(
                    _dbContext.Notifications.AsNoTracking(),
                    callerUserId,
                    dealerFilter: null,
                    forceDealerFeedOnly: false)
                .CountAsync(n => !n.IsRead, ct);

            return new MarkNotificationReadResultDto(
                Id: notification.Id,
                NotificationId: notification.Id,
                IsRead: true,
                ReadAtUtc: readTimestamp,
                UnreadCount: unreadCount);
        }, cancellationToken);
    }

    public async Task<MarkAllNotificationsReadResultDto> MarkAllNotificationsReadAsync(
        Guid? dealerId = null,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = EnsureCallerCanAccessNotifications(forceDealerFeedOnly: false);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            var utcNow = _dateTimeProvider.UtcNow;

            var unreadTargetQuery = BuildCallerScopedQuery(
                    _dbContext.Notifications,
                    callerUserId,
                    dealerId,
                    forceDealerFeedOnly: false)
                .Where(n => !n.IsRead);

            var markedReadCount = await unreadTargetQuery.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, utcNow),
                ct);

            var remainingUnreadCount = await BuildCallerScopedQuery(
                    _dbContext.Notifications.AsNoTracking(),
                    callerUserId,
                    dealerFilter: dealerId,
                    forceDealerFeedOnly: false)
                .CountAsync(n => !n.IsRead, ct);

            return new MarkAllNotificationsReadResultDto(
                MarkedReadCount: markedReadCount,
                UnreadCount: remainingUnreadCount,
                ReadAtUtc: utcNow);
        }, cancellationToken);
    }

    private Guid EnsureCallerCanAccessNotifications(bool forceDealerFeedOnly)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Notifications.ViewOwn);
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Notifications.ViewDealer);

        if (forceDealerFeedOnly)
        {
            if (!canViewDealer && !_currentUserContext.IsAdmin)
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view dealership notifications.",
                    "FORBIDDEN_PERMISSION");
            }

            return callerUserId;
        }

        if (!canViewOwn && !canViewDealer && !_currentUserContext.IsAdmin)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view notifications.",
                "FORBIDDEN_PERMISSION");
        }

        return callerUserId;
    }

    private static Guid? NormalizeDealerFilter(Guid? dealerFilter) =>
        dealerFilter is { } did && did != Guid.Empty ? did : null;

    private IQueryable<Notification> BuildCallerScopedQuery(
        IQueryable<Notification> source,
        Guid callerUserId,
        Guid? dealerFilter,
        bool forceDealerFeedOnly)
    {
        var effectiveDealerFilter = NormalizeDealerFilter(dealerFilter);

        if (_currentUserContext.IsCustomer)
        {
            if (effectiveDealerFilter.HasValue)
            {
                throw new ForbiddenAccessException(
                    "Customers cannot query dealership-targeted notification feeds.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            return source.Where(n => n.RecipientUserId == callerUserId);
        }

        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (effectiveDealerFilter.HasValue && effectiveDealerFilter.Value != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot view notifications belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            if (forceDealerFeedOnly)
            {
                return source.Where(n => n.TargetDealerId == staffDealerId && n.RecipientUserId == null);
            }

            return source.Where(n =>
                n.RecipientUserId == callerUserId ||
                (n.TargetDealerId == staffDealerId && n.RecipientUserId == null));
        }

        if (_currentUserContext.IsAdmin)
        {
            if (forceDealerFeedOnly)
            {
                return effectiveDealerFilter.HasValue
                    ? source.Where(n => n.TargetDealerId == effectiveDealerFilter.Value && n.RecipientUserId == null)
                    : source.Where(n => n.TargetDealerId != null && n.RecipientUserId == null);
            }

            if (effectiveDealerFilter.HasValue)
            {
                var targetDealerId = effectiveDealerFilter.Value;
                return source.Where(n => n.TargetDealerId == targetDealerId && n.RecipientUserId == null);
            }

            return source.Where(n =>
                n.RecipientUserId == callerUserId ||
                n.TargetRole == RoleNames.Admin);
        }

        return source.Where(n => n.RecipientUserId == callerUserId);
    }

    private void EnsureCallerCanMutateNotification(Notification notification, Guid callerUserId)
    {
        if (_currentUserContext.IsCustomer)
        {
            if (notification.RecipientUserId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only mark their own personal notifications as read.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }

            return;
        }

        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            var isOwnPersonal = notification.RecipientUserId == callerUserId;
            var isAssignedDealerFeed = notification.TargetDealerId == staffDealerId &&
                                       notification.RecipientUserId == null;

            if (isOwnPersonal || isAssignedDealerFeed)
            {
                return;
            }

            if (notification.TargetDealerId.HasValue && notification.TargetDealerId.Value != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot modify notifications belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            throw new ForbiddenAccessException(
                "You do not have permission to modify this notification.",
                "FORBIDDEN_RESOURCE_OWNER");
        }

        if (_currentUserContext.IsAdmin)
        {
            var isOwnPersonal = notification.RecipientUserId == callerUserId;
            var isAdminRoleFeed = string.Equals(notification.TargetRole, RoleNames.Admin, StringComparison.Ordinal);
            var isDealerBroadcast = notification.TargetDealerId.HasValue && notification.RecipientUserId is null;

            if (!isOwnPersonal && !isAdminRoleFeed && !isDealerBroadcast)
            {
                throw new ForbiddenAccessException(
                    "Cannot mark another user's private notification as read.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }

            return;
        }

        if (notification.RecipientUserId != callerUserId)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to modify this notification.",
                "FORBIDDEN_RESOURCE_OWNER");
        }
    }
}
