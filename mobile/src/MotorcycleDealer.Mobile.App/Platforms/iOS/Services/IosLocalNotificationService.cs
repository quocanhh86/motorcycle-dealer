using Foundation;
using MotorcycleDealer.Mobile.Core.Realtime;
using UserNotifications;

namespace MotorcycleDealer.Mobile.App.Platforms.iOS.Services;

/// <summary>
/// Native Apple iOS UserNotifications (UNUserNotificationCenter) implementation
/// for all 10 real-time SignalR notification events with categorized foreground banners.
/// </summary>
public sealed class IosLocalNotificationService : ILocalNotificationService
{
    private static int _nextNotificationId = 3000;
    private readonly SemaphoreSlim _authorizationLock = new(1, 1);
    private bool _categoriesRegistered;
    private bool? _isAuthorized;

    public async Task ShowAsync(LocalNotificationRequest notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            UNUserNotificationCenter center = UNUserNotificationCenter.Current;
            EnsureCategoriesRegistered(center);

            if (!await EnsureAuthorizedAsync(center, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            string channelId = string.IsNullOrWhiteSpace(notification.ChannelId)
                ? NotificationChannels.BookingAlerts
                : notification.ChannelId;

            var userInfoKeys = new List<NSString> { new("event_type") };
            var userInfoValues = new List<NSObject> { new NSString(notification.EventType ?? string.Empty) };

            if (!string.IsNullOrWhiteSpace(notification.TargetRoute))
            {
                userInfoKeys.Add(new NSString("target_route"));
                userInfoValues.Add(new NSString(notification.TargetRoute));
            }

            if (!string.IsNullOrWhiteSpace(notification.PayloadId))
            {
                userInfoKeys.Add(new NSString("payload_id"));
                userInfoValues.Add(new NSString(notification.PayloadId));
            }

            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Body,
                Sound = UNNotificationSound.Default,
                CategoryIdentifier = channelId,
                ThreadIdentifier = channelId,
                UserInfo = NSDictionary.FromObjectsAndKeys(
                    userInfoValues.ToArray(),
                    userInfoKeys.Cast<NSObject>().ToArray())
            };

            UNTimeIntervalNotificationTrigger trigger =
                UNTimeIntervalNotificationTrigger.CreateTrigger(0.25, repeats: false);

            int notificationId = Interlocked.Increment(ref _nextNotificationId);
            string identifier = $"apex-moto-{channelId}-{notificationId}";

            UNNotificationRequest request = UNNotificationRequest.FromIdentifier(identifier, content, trigger);
            await center.AddNotificationRequestAsync(request).ConfigureAwait(false);
        }
        catch
        {
            // Guard against simulator/headless notification errors so SignalR loop never throws
        }
    }

    private void EnsureCategoriesRegistered(UNUserNotificationCenter center)
    {
        if (_categoriesRegistered)
        {
            return;
        }

        var bookingCategory = UNNotificationCategory.FromIdentifier(
            NotificationChannels.BookingAlerts,
            Array.Empty<UNNotificationAction>(),
            Array.Empty<string>(),
            UNNotificationCategoryOptions.CustomDismissAction);

        var loyaltyCategory = UNNotificationCategory.FromIdentifier(
            NotificationChannels.LoyaltyRewards,
            Array.Empty<UNNotificationAction>(),
            Array.Empty<string>(),
            UNNotificationCategoryOptions.CustomDismissAction);

        var warrantyCategory = UNNotificationCategory.FromIdentifier(
            NotificationChannels.WarrantyUpdates,
            Array.Empty<UNNotificationAction>(),
            Array.Empty<string>(),
            UNNotificationCategoryOptions.CustomDismissAction);

        center.SetNotificationCategories(new NSSet<UNNotificationCategory>(
            bookingCategory,
            loyaltyCategory,
            warrantyCategory));

        _categoriesRegistered = true;
    }

    private async Task<bool> EnsureAuthorizedAsync(UNUserNotificationCenter center, CancellationToken ct)
    {
        if (_isAuthorized.HasValue)
        {
            return _isAuthorized.Value;
        }

        await _authorizationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_isAuthorized.HasValue)
            {
                return _isAuthorized.Value;
            }

            (bool approved, NSError? _) = await center.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound).ConfigureAwait(false);

            _isAuthorized = approved;
            return approved;
        }
        finally
        {
            _authorizationLock.Release();
        }
    }
}
