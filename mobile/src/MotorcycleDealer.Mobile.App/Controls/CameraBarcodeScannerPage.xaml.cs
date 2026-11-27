using MotorcycleDealer.Mobile.Core.Localization;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MotorcycleDealer.Mobile.App.Controls;

public partial class CameraBarcodeScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _resultTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _lifecycleCts = new();
    private readonly bool _requireIso3779Vin;
    private CameraBarcodeReaderView? _cameraReader;
    private Task? _cameraActivationTask;
    private DateTimeOffset? _cameraMountedAtUtc;
    private int _completed;
    private bool _isClosing;
    private string _manualInput = string.Empty;

    public LocalizationService Loc => LocalizationService.Current;

    public string ScannerHeading { get; }
    public string ScannerSubtitle { get; }

    public bool IsCameraActive { get; private set; }
    public bool ShowCameraFallback => !IsCameraActive;
    public string CameraStatusMessage { get; private set; }
    public string TorchButtonText => _cameraReader?.IsTorchOn == true ? Loc.ScannerTorchOn : Loc.ScannerTorchOff;

    public string ManualInput
    {
        get => _manualInput;
        set
        {
            string normalized = NormalizeInput(value);
            if (_manualInput == normalized)
            {
                return;
            }

            _manualInput = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ManualCharCountText));
            OnPropertyChanged(nameof(ManualValidationHint));
            OnPropertyChanged(nameof(HasValidationHint));
        }
    }

    public string ManualCharCountText => _requireIso3779Vin
        ? $"{ManualInput.Length}/17 VIN"
        : $"{ManualInput.Length}";

    public string ManualValidationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ManualInput))
            {
                return string.Empty;
            }

            if (_requireIso3779Vin)
            {
                if (ManualInput.Length != 17)
                {
                    return Loc.ScannerManualHintVin;
                }

                foreach (char c in ManualInput)
                {
                    if (!char.IsAsciiLetterOrDigit(c) || c is 'I' or 'O' or 'Q')
                    {
                        return Loc.Val_VinForbiddenChar;
                    }
                }
            }

            return string.Empty;
        }
    }

    public bool HasValidationHint => !string.IsNullOrWhiteSpace(ManualValidationHint);

    public CameraBarcodeScannerPage(
        string? heading = null,
        string? subtitle = null,
        bool requireIso3779Vin = false)
    {
        LocalizationService loc = LocalizationService.Current;
        ScannerHeading = string.IsNullOrWhiteSpace(heading) ? loc.ScannerDefaultHeading : heading;
        ScannerSubtitle = string.IsNullOrWhiteSpace(subtitle)
            ? (requireIso3779Vin ? loc.ScannerStrictVinSubtitle : loc.ScannerMultiCodeSubtitle)
            : subtitle;
        CameraStatusMessage = loc.ScannerManualHintGeneral;
        _requireIso3779Vin = requireIso3779Vin;

        InitializeComponent();
        Loaded += (_, _) => ApplyModalCarbonStatusBar();
    }

    public Task<string?> WaitForResultAsync() => _resultTcs.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyModalCarbonStatusBar();
        Dispatcher.Dispatch(ApplyModalCarbonStatusBar);
        _ = ReapplyStatusBarAfterTransitionAsync(_lifecycleCts.Token);

        if (_isClosing)
        {
            return;
        }

        _cameraActivationTask = TryActivateCameraAsync(_lifecycleCts.Token);
        try
        {
            await _cameraActivationTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when user cancels or presses Back while camera is warming up
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
        {
            _ = CompleteAndCloseAsync(null);
        }

        // Intercept hardware Back so Android never tears down the modal DialogFragment mid-CameraManager.Connect()
        return true;
    }

    protected override void OnDisappearing()
    {
        _isClosing = true;
        try
        {
            _lifecycleCts.Cancel();
        }
        catch
        {
            // Ignore cancellation race
        }

        if (_cameraReader is not null)
        {
            try
            {
                _cameraReader.IsDetecting = false;
                _cameraReader.IsTorchOn = false;
            }
            catch
            {
                // Defensive during native CameraX teardown
            }
        }

        base.OnDisappearing();
        _resultTcs.TrySetResult(null);
    }

    private async Task ReapplyStatusBarAfterTransitionAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(150, ct);
            if (!_isClosing)
            {
                ApplyModalCarbonStatusBar();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on fast dismiss
        }
    }

    private async Task TryActivateCameraAsync(CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested || _isClosing)
            {
                return;
            }

            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (ct.IsCancellationRequested || _isClosing)
            {
                return;
            }

            if (status != PermissionStatus.Granted)
            {
                SetFallbackState(Loc.ScannerCameraPermissionDenied);
                return;
            }

#if ANDROID
            if (!HasAndroidCameraHardware())
            {
                SetFallbackState(Loc.ScannerCameraPermissionDenied);
                return;
            }

            bool providerReady = await WaitForAndroidCameraProviderReadyAsync(ct);
            if (ct.IsCancellationRequested || _isClosing)
            {
                return;
            }

            if (!providerReady)
            {
                SetFallbackState(Loc.ScannerCameraPermissionDenied);
                return;
            }
#elif IOS
            if (!HasIosCameraHardware())
            {
                SetFallbackState(Loc.ScannerCameraPermissionDenied);
                return;
            }
#endif

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (ct.IsCancellationRequested || _isClosing)
                {
                    return;
                }

                if (_cameraReader is null)
                {
                    var reader = new CameraBarcodeReaderView
                    {
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        IsDetecting = true,
                        Options = new BarcodeReaderOptions
                        {
                            Formats = BarcodeFormats.All,
                            AutoRotate = true,
                            Multiple = false
                        }
                    };
                    reader.BarcodesDetected += OnBarcodesDetected;
                    _cameraReader = reader;
                    _cameraMountedAtUtc = DateTimeOffset.UtcNow;
                    CameraHost.Content = reader;
                }
                else
                {
                    _cameraReader.IsDetecting = true;
                }

                IsCameraActive = true;
                CameraStatusMessage = _requireIso3779Vin ? Loc.ScannerStrictVinSubtitle : Loc.ScannerMultiCodeSubtitle;
                NotifyCameraStateChanged();
                ApplyModalCarbonStatusBar();
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when dismissed while initializing
        }
        catch
        {
            SetFallbackState(Loc.ScannerCameraPermissionDenied);
        }
    }

    private void SetFallbackState(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsCameraActive = false;
            if (_cameraReader is not null)
            {
                _cameraReader.IsDetecting = false;
            }

            CameraStatusMessage = message;
            NotifyCameraStateChanged();
        });
    }

    private void NotifyCameraStateChanged()
    {
        OnPropertyChanged(nameof(IsCameraActive));
        OnPropertyChanged(nameof(ShowCameraFallback));
        OnPropertyChanged(nameof(CameraStatusMessage));
        OnPropertyChanged(nameof(TorchButtonText));
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        string? raw = e.Results?.FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string candidate = _requireIso3779Vin ? NormalizeInput(raw) : raw;
        if (_requireIso3779Vin && candidate.Length != 17)
        {
            return;
        }

        if (Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            TryHapticConfirm();
            await CompleteAndCloseAsync(candidate);
        });
    }

    private void OnToggleTorchClicked(object? sender, EventArgs e)
    {
        if (!IsCameraActive || _cameraReader is null)
        {
            return;
        }

        _cameraReader.IsTorchOn = !_cameraReader.IsTorchOn;
        OnPropertyChanged(nameof(TorchButtonText));
    }

    private async void OnEnableCameraClicked(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _cameraActivationTask = TryActivateCameraAsync(_lifecycleCts.Token);
        try
        {
            await _cameraActivationTask;
        }
        catch (OperationCanceledException)
        {
            // Expected on cancel
        }
    }

    private async void OnManualSubmitClicked(object? sender, EventArgs e)
    {
        string value = ManualInput.Trim();
        if (string.IsNullOrWhiteSpace(value) || HasValidationHint)
        {
            OnPropertyChanged(nameof(ManualValidationHint));
            OnPropertyChanged(nameof(HasValidationHint));
            return;
        }

        if (Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return;
        }

        TryHapticConfirm();
        await CompleteAndCloseAsync(value);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
        {
            return;
        }

        await CompleteAndCloseAsync(null);
    }

    private async Task CompleteAndCloseAsync(string? result)
    {
        _isClosing = true;
        try
        {
            _lifecycleCts.Cancel();
        }
        catch
        {
            // Ignore cancellation race
        }

        Task? activation = _cameraActivationTask;
        if (activation is not null && !activation.IsCompleted)
        {
            try
            {
                await Task.WhenAny(activation, Task.Delay(600));
            }
            catch
            {
                // Ignore activation faults during teardown
            }
        }

        // Allow ZXing.Net.Maui's CameraManager.Connect() ListenableFuture callback on Android Looper
        // to complete before detaching CameraBarcodeReaderView and dismissing the modal DialogFragment.
        if (_cameraMountedAtUtc.HasValue)
        {
            TimeSpan elapsed = DateTimeOffset.UtcNow - _cameraMountedAtUtc.Value;
            TimeSpan settleWindow = TimeSpan.FromMilliseconds(650);
            if (elapsed < settleWindow)
            {
                try
                {
                    await Task.Delay(settleWindow - elapsed);
                }
                catch
                {
                    // Ignore delay errors
                }
            }
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            DetachCameraReaderSafely();
            await Task.Delay(60);

            _resultTcs.TrySetResult(result);

            if (Navigation.ModalStack.Count > 0)
            {
                try
                {
                    await Navigation.PopModalAsync(animated: true);
                }
                catch
                {
                    // Ignore if already dismissed
                }
            }
        });
    }

    private void DetachCameraReaderSafely()
    {
        CameraBarcodeReaderView? reader = _cameraReader;
        if (reader is null)
        {
            return;
        }

        _cameraReader = null;
        try
        {
            reader.BarcodesDetected -= OnBarcodesDetected;
            reader.IsDetecting = false;
            reader.IsTorchOn = false;
        }
        catch
        {
            // Defensive during native CameraX teardown
        }

        IsCameraActive = false;
        CameraHost.Content = null;
        NotifyCameraStateChanged();
    }

    private void ApplyModalCarbonStatusBar()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Platform.CurrentActivity is MainActivity mainActivity)
            {
                mainActivity.ApplyCarbonStatusBar();
            }

            if (Handler?.PlatformView is Android.Views.View androidView)
            {
                ApplyWhiteStatusBarIconsOnView(androidView);
                androidView.Post(() => ApplyWhiteStatusBarIconsOnView(androidView));
            }
        });
#endif
    }

#if ANDROID
    private static void ApplyWhiteStatusBarIconsOnView(Android.Views.View androidView)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            int mask = (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars;
            androidView.WindowInsetsController?.SetSystemBarsAppearance(0, mask);
            androidView.RootView?.WindowInsetsController?.SetSystemBarsAppearance(0, mask);
        }

        Android.Graphics.Color carbonColor = Android.Graphics.Color.ParseColor("#020617");
        androidView.RootView?.SetBackgroundColor(carbonColor);
    }

    private static bool HasAndroidCameraHardware()
    {
        try
        {
            if (Android.App.Application.Context.GetSystemService(Android.Content.Context.CameraService)
                is Android.Hardware.Camera2.CameraManager cameraManager)
            {
                string[] ids = cameraManager.GetCameraIdList();
                return ids is { Length: > 0 };
            }
        }
        catch
        {
            // Fall through to unavailable
        }

        return false;
    }

    private static async Task<bool> WaitForAndroidCameraProviderReadyAsync(CancellationToken ct)
    {
        try
        {
            Android.Content.Context appContext = Android.App.Application.Context;
            Google.Common.Util.Concurrent.IListenableFuture future =
                AndroidX.Camera.Lifecycle.ProcessCameraProvider.GetInstance(appContext);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            future.AddListener(
                new Java.Lang.Runnable(() =>
                {
                    try
                    {
                        Java.Lang.Object? provider = future.Get();
                        tcs.TrySetResult(provider is not null);
                    }
                    catch
                    {
                        tcs.TrySetResult(false);
                    }
                }),
                AndroidX.Core.Content.ContextCompat.GetMainExecutor(appContext));

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(2500));

            using CancellationTokenRegistration reg = timeoutCts.Token.Register(() => tcs.TrySetResult(false));
            return await tcs.Task;
        }
        catch
        {
            return false;
        }
    }
#elif IOS
    private static bool HasIosCameraHardware()
    {
        try
        {
            return AVFoundation.AVCaptureDevice.GetDefaultDevice(AVFoundation.AVMediaTypes.Video) is not null;
        }
        catch
        {
            return false;
        }
    }
#endif

    private string NormalizeInput(string? raw)
    {
        string trimmed = (raw ?? string.Empty).Trim();
        if (!_requireIso3779Vin)
        {
            return trimmed;
        }

        Span<char> buffer = stackalloc char[Math.Min(trimmed.Length, 17)];
        int idx = 0;
        foreach (char ch in trimmed.ToUpperInvariant())
        {
            if (idx >= 17)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(ch) && ch is not ('I' or 'O' or 'Q'))
            {
                buffer[idx++] = ch;
            }
        }

        return new string(buffer[..idx]);
    }

    private static void TryHapticConfirm()
    {
        try
        {
            if (HapticFeedback.Default.IsSupported)
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
        }
        catch
        {
            // Ignore on emulators without vibrator hardware
        }
    }
}
