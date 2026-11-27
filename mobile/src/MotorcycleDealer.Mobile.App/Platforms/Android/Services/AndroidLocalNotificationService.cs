using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CommunityToolkit.Mvvm.Messaging;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.App.Platforms.Android.Services;

/// <summary>
/// Native Android NotificationChannel + NotificationCompat implementation (API 26-35+)
/// for all 10 real-time SignalR notification events with localized channel metadata and zero fragile plugin deps.
/// </summary>
public sealed class AndroidLocalNotificationService : ILocalNotificationService
{
    private static int _nextNotificationId = 2000;
    private string? _channelsCultureCode;

    public AndroidLocalNotificationService()
    {
        WeakReferenceMessenger.Default.Register<AndroidLocalNotificationService, CultureChangedMessage>(
            this,
            static (recipient, _) => recipient.RefreshChannelsForActiveCulture());
    }

    public Task ShowAsync(LocalNotificationRequest notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            Context context = global::Android.App.Application.Context;
            EnsureChannels(context);

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                Permission perm = ContextCompat.CheckSelfPermission(
                    context,
                    global::Android.Manifest.Permission.PostNotifications);
                if (perm != Permission.Granted)
                {
                    return Task.CompletedTask;
                }
            }

            string channelId = string.IsNullOrWhiteSpace(notification.ChannelId)
                ? NotificationChannels.BookingAlerts
                : notification.ChannelId;

            int notificationId = Interlocked.Increment(ref _nextNotificationId);
            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            if (!string.IsNullOrWhiteSpace(notification.TargetRoute))
            {
                launchIntent.PutExtra("target_route", notification.TargetRoute);
            }

            PendingIntent? pendingIntent = PendingIntent.GetActivity(
                context,
                notificationId,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            int smallIcon = context.ApplicationInfo?.Icon > 0
                ? context.ApplicationInfo.Icon
                : global::Android.Resource.Drawable.IcDialogInfo;

            var builder = new NotificationCompat.Builder(context, channelId);
            builder.SetSmallIcon(smallIcon);
            builder.SetContentTitle(notification.Title);
            builder.SetContentText(notification.Body);

            var bigTextStyle = new NotificationCompat.BigTextStyle();
            bigTextStyle.BigText(notification.Body);
            builder.SetStyle(bigTextStyle);
            builder.SetPriority(NotificationCompat.PriorityHigh);
            builder.SetAutoCancel(true);

            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            Notification? built = builder.Build();
            NotificationManagerCompat? manager = NotificationManagerCompat.From(context);
            if (built is not null && manager is not null)
            {
                manager.Notify(notificationId, built);
            }
        }
        catch
        {
            // Guard against headless/emulator notification manager errors so SignalR loop never throws
        }

        return Task.CompletedTask;
    }

    private void RefreshChannelsForActiveCulture()
    {
        try
        {
            Context context = global::Android.App.Application.Context;
            EnsureChannels(context);
        }
        catch
        {
            // Ignore when Android application context is not attached
        }
    }

    private void EnsureChannels(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        CultureInfo culture = LocalizationService.Current.CurrentCulture;
        if (string.Equals(_channelsCultureCode, culture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.GetSystemService(Context.NotificationService) is NotificationManager manager)
        {
            var bookingChannel = new NotificationChannel(
                NotificationChannels.BookingAlerts,
                ResolveChannelText("NotifChannel_BookingAlerts_Name", "BookingPageTitle", culture, "Booking & Bay Alerts"),
                NotificationImportance.High)
            {
                Description = ResolveChannelText(
                    "NotifChannel_BookingAlerts_Desc",
                    "BookingSubtitle",
                    culture,
                    "Real-time workshop booking, test-drive, and VIN maintenance alerts.")
            };

            var loyaltyChannel = new NotificationChannel(
                NotificationChannels.LoyaltyRewards,
                ResolveChannelText("NotifChannel_LoyaltyRewards_Name", "LoyaltyPageTitle", culture, "Loyalty Points & Rewards"),
                NotificationImportance.Default)
            {
                Description = ResolveChannelText(
                    "NotifChannel_LoyaltyRewards_Desc",
                    "LoyaltyFormulaCaption",
                    culture,
                    "Points accrual, review bonuses, and loyalty tier upgrades.")
            };

            var warrantyChannel = new NotificationChannel(
                NotificationChannels.WarrantyUpdates,
                ResolveChannelText("NotifChannel_WarrantyUpdates_Name", "WarrantyTitle", culture, "VIN Warranty Updates"),
                NotificationImportance.High)
            {
                Description = ResolveChannelText(
                    "NotifChannel_WarrantyUpdates_Desc",
                    "WarrantySubtitle",
                    culture,
                    "OEM VIN warranty claim submissions and status updates.")
            };

            manager.CreateNotificationChannel(bookingChannel);
            manager.CreateNotificationChannel(loyaltyChannel);
            manager.CreateNotificationChannel(warrantyChannel);
        }

        _channelsCultureCode = culture.Name;
    }

    private static string ResolveChannelText(
        string preferredKey,
        string fallbackResourceKey,
        CultureInfo culture,
        string hardFallback)
    {
        if (AppResources.TryGetString(preferredKey, out string preferred, culture)
            && !string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        if (AppResources.TryGetString(fallbackResourceKey, out string localized, culture)
            && !string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        return hardFallback;
    }
}
