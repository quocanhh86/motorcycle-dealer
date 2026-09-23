using MotorcycleDealer.Mobile.App.Controls;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.App.Services;

public sealed class MauiMainThreadDispatcher : IMainThreadDispatcher
{
    public bool IsMainThread => MainThread.IsMainThread;

    public void BeginInvokeOnMainThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

    public Task InvokeOnMainThreadAsync(Func<Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);
        return MainThread.IsMainThread
            ? asyncAction()
            : MainThread.InvokeOnMainThreadAsync(asyncAction);
    }
}

public sealed class MauiConnectivityMonitor : IConnectivityMonitor, IDisposable
{
    public MauiConnectivityMonitor()
    {
        Connectivity.Current.ConnectivityChanged += OnNativeConnectivityChanged;
    }

    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    private void OnNativeConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnNativeConnectivityChanged;
    }
}

public sealed class MauiHapticService : IHapticService
{
    public void Perform(HapticKind kind)
    {
        try
        {
            if (!HapticFeedback.Default.IsSupported)
            {
                return;
            }

            HapticFeedbackType nativeType = kind switch
            {
                HapticKind.LongPress or HapticKind.Warning => HapticFeedbackType.LongPress,
                _ => HapticFeedbackType.Click
            };

            MainThread.BeginInvokeOnMainThread(() => HapticFeedback.Default.Perform(nativeType));
        }
        catch
        {
            // Ignore on emulators/simulators without vibrator hardware
        }
    }
}

public sealed class MauiShareService : IShareService
{
    public Task ShareTextAsync(string title, string text, CancellationToken cancellationToken = default)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            MobileUserActionLogHub.Current?.Track("Share.TextShared", "UserInteraction", title);
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = title,
                Text = text
            });
        });
}

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabasePath()
        => Path.Combine(FileSystem.AppDataDirectory, "dealer_mobile_cache_v1.db3");
}

public sealed class MauiNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null || string.IsNullOrWhiteSpace(route))
            {
                return;
            }

            MobileUserActionLogHub.Current?.Track("Navigation.GoTo", "Navigation", route);

            if (Shell.Current is AppShell appShell)
            {
                appShell.EnsureTabVisibilityForRoute(route);
            }

            if (parameters is { Count: > 0 })
            {
                await Shell.Current.GoToAsync(route, new ShellNavigationQueryParameters(parameters));
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }
        });

    public Task GoBackAsync()
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is not null)
            {
                MobileUserActionLogHub.Current?.Track("Navigation.GoBack", "Navigation", "..");
                await Shell.Current.GoToAsync("..");
            }
        });

    public Task<string?> RequestBarcodeScanAsync(string? promptTitle = null)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null)
            {
                return null;
            }

            string heading = string.IsNullOrWhiteSpace(promptTitle) ? "Scan VIN or QR Code" : promptTitle;
            bool strictVin = heading.Contains("17-Character", StringComparison.OrdinalIgnoreCase)
                || heading.Contains("Stock Intake", StringComparison.OrdinalIgnoreCase);

            MobileUserActionLogHub.Current?.Track("Scanner.Opened", "Scanner", heading);

            var scannerPage = new CameraBarcodeScannerPage(
                heading: heading,
                subtitle: strictVin
                    ? "ISO 3779 17-char chassis VIN (excludes I, O, Q)"
                    : "ISO 3779 VIN barcode · Voucher QR · Booking Check-In QR",
                requireIso3779Vin: strictVin);

            await Shell.Current.Navigation.PushModalAsync(scannerPage, animated: true);
            string? scanned = await scannerPage.WaitForResultAsync();
            MobileUserActionLogHub.Current?.Track(
                action: string.IsNullOrWhiteSpace(scanned) ? "Scanner.Cancelled" : "Scanner.Completed",
                category: "Scanner",
                target: string.IsNullOrWhiteSpace(scanned) ? heading : scanned,
                outcome: string.IsNullOrWhiteSpace(scanned) ? "Cancelled" : "Success");
            return scanned;
        });

    public Task OpenExternalUrlAsync(string url)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            MobileUserActionLogHub.Current?.Track("ExternalLink.Opened", "UserInteraction", url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ? "PhoneDialer" : "ExternalBrowser");

            if (url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            {
                string number = url["tel:".Length..].Trim();
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrWhiteSpace(number))
                {
                    PhoneDialer.Default.Open(number);
                    return;
                }
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                try
                {
                    await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
                }
                catch
                {
                    await Launcher.Default.OpenAsync(uri);
                }
            }
        });
}
