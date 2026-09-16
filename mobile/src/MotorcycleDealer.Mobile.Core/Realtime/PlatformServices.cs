namespace MotorcycleDealer.Mobile.Core.Realtime;

public sealed record LocalNotificationRequest(
    string ChannelId,
    string Title,
    string Body,
    string EventType,
    string? TargetRoute = null,
    string? PayloadId = null);

public static class NotificationChannels
{
    public const string BookingAlerts = "BookingAlerts";
    public const string LoyaltyRewards = "LoyaltyRewards";
    public const string WarrantyUpdates = "WarrantyUpdates";
}

public interface IMainThreadDispatcher
{
    bool IsMainThread { get; }
    void BeginInvokeOnMainThread(Action action);
    Task InvokeOnMainThreadAsync(Func<Task> asyncAction);
}

public interface ILocalNotificationService
{
    Task ShowAsync(LocalNotificationRequest notification, CancellationToken cancellationToken = default);
}

public interface IConnectivityMonitor
{
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
    Task<string?> RequestBarcodeScanAsync(string? promptTitle = null);
    Task OpenExternalUrlAsync(string url);
}

public enum HapticKind
{
    Click = 0,
    LongPress = 1,
    Success = 2,
    Warning = 3
}

public interface IHapticService
{
    void Perform(HapticKind kind);
}

public interface IShareService
{
    Task ShareTextAsync(string title, string text, CancellationToken cancellationToken = default);
}

public interface IUserFeedbackService
{
    Task ShowToastAsync(string message, CancellationToken ct = default);
    Task ShowSnackbarAsync(string message, string? actionLabel = null, Func<Task>? onAction = null, CancellationToken ct = default);
}

/// <summary>
/// Safe fallback registered via TryAddSingleton when no native platform UI feedback host is wired (e.g. unit tests).
/// Mobile.App registers the native CommunityToolkit.Maui Toast + Snackbar implementation.
/// </summary>
public sealed class NoOpUserFeedbackService : IUserFeedbackService
{
    public static NoOpUserFeedbackService Instance { get; } = new();

    public Task ShowToastAsync(string message, CancellationToken ct = default) => Task.CompletedTask;

    public Task ShowSnackbarAsync(
        string message,
        string? actionLabel = null,
        Func<Task>? onAction = null,
        CancellationToken ct = default) => Task.CompletedTask;
}
