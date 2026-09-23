using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Realtime;
using MauiFont = Microsoft.Maui.Font;

namespace MotorcycleDealer.Mobile.App.Services;

/// <summary>
/// Cross-platform CommunityToolkit.Maui 13.0 Toast and Snackbar service for Android &amp; iOS.
/// Styled in Apex Moto Carbon (#020617), Slate50 (#F8FAFC), and Motorsport Crimson (#DC2626).
/// </summary>
public sealed class MauiUserFeedbackService : IUserFeedbackService
{
    private static readonly SnackbarOptions MotorsportSnackbarOptions = new()
    {
        BackgroundColor = Color.FromArgb("#020617"),
        TextColor = Color.FromArgb("#F8FAFC"),
        ActionButtonTextColor = Color.FromArgb("#DC2626"),
        CornerRadius = new CornerRadius(8),
        Font = MauiFont.SystemFontOfSize(14),
        ActionButtonFont = MauiFont.SystemFontOfSize(14, FontWeight.Bold)
    };

    public async Task ShowToastAsync(string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        MobileUserActionLogHub.Current?.Track("Feedback.Toast", "UserFeedback", message.Trim(), syncToServer: false);

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IToast toast = Toast.Make(message.Trim(), ToastDuration.Short, textSize: 14);
                await toast.Show(ct);
            }).ConfigureAwait(false);
        }
        catch
        {
            // Keep ViewModel workflows resilient when app window is transitioning or backgrounded
        }
    }

    public async Task ShowSnackbarAsync(
        string message,
        string? actionLabel = null,
        Func<Task>? onAction = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        MobileUserActionLogHub.Current?.Track("Feedback.Snackbar", "UserFeedback", message.Trim(), syncToServer: false);

        try
        {
            string buttonText = string.IsNullOrWhiteSpace(actionLabel) ? "OK" : actionLabel.Trim();
            Action? syncAction = onAction is null ? null : () => _ = InvokeActionSafelyAsync(onAction);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ISnackbar snackbar = Snackbar.Make(
                    message.Trim(),
                    action: syncAction,
                    actionButtonText: buttonText,
                    duration: TimeSpan.FromSeconds(4),
                    visualOptions: MotorsportSnackbarOptions);
                await snackbar.Show(ct);
            }).ConfigureAwait(false);
        }
        catch
        {
            // Keep ViewModel workflows resilient when app window is transitioning or backgrounded
        }
    }

    private static async Task InvokeActionSafelyAsync(Func<Task> onAction)
    {
        try
        {
            await onAction().ConfigureAwait(false);
        }
        catch
        {
            // Prevent unhandled fire-and-forget exceptions from native Snackbar action callbacks
        }
    }
}
