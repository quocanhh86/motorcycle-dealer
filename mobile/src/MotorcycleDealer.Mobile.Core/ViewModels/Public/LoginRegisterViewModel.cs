using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentValidation;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Messaging;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;
using MotorcycleDealer.Mobile.Core.Validation;

namespace MotorcycleDealer.Mobile.Core.ViewModels.Public;

public sealed partial class LoginRegisterViewModel : MobileViewModelBase
{
    private readonly IDealerMobileApiClient _apiClient;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IApiEndpointProvider _endpointProvider;
    private readonly MobileSessionState _sessionState;
    private readonly ISignalRMobileService _signalRService;
    private readonly ILogoutCoordinator _logoutCoordinator;
    private readonly INavigationService _navigationService;
    private readonly IHapticService _hapticService;
    private readonly IValidator<LoginInput> _loginValidator;
    private readonly IValidator<RegisterInput> _registerValidator;
    private readonly IMessenger _messenger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly IMobileOfflineDb? _offlineDb;

    public LoginRegisterViewModel(
        IDealerMobileApiClient apiClient,
        ISecureTokenStore tokenStore,
        IApiEndpointProvider endpointProvider,
        MobileSessionState sessionState,
        ISignalRMobileService signalRService,
        ILogoutCoordinator logoutCoordinator,
        INavigationService navigationService,
        IHapticService hapticService,
        IValidator<LoginInput> loginValidator,
        IValidator<RegisterInput> registerValidator,
        IMessenger messenger,
        IUserFeedbackService feedbackService,
        IMobileOfflineDb? offlineDb = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _logoutCoordinator = logoutCoordinator ?? throw new ArgumentNullException(nameof(logoutCoordinator));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _hapticService = hapticService ?? throw new ArgumentNullException(nameof(hapticService));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _offlineDb = offlineDb;

        Email = string.Empty;
        Password = string.Empty;
        FullName = string.Empty;
        PhoneNumber = string.Empty;
        ApiBaseUrl = _endpointProvider.CurrentBaseUrl;
        HealthStatusMessage = string.Empty;
    }

    [ObservableProperty]
    public partial bool IsRegisterMode { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string PhoneNumber { get; set; }

    [ObservableProperty]
    public partial string ApiBaseUrl { get; set; }

    [ObservableProperty]
    public partial string HealthStatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsApiHealthy { get; set; }

    public string SubmitButtonText => IsRegisterMode ? "Create Rider Account" : "Sign In";
    public string ModeSwitchText => IsRegisterMode ? "Already have an account? Sign In" : "New rider? Create Account";

    partial void OnIsRegisterModeChanged(bool value)
    {
        OnPropertyChanged(nameof(SubmitButtonText));
        OnPropertyChanged(nameof(ModeSwitchText));
        ClearMessages();
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        TrackUserAction("Auth.ToggleMode", "UserInteraction", IsRegisterMode ? "Register" : "SignIn");
    }

    [RelayCommand]
    private async Task UseEmulatorEndpointAsync()
    {
        ApiBaseUrl = MobileEndpointDefaults.EmulatorBaseUrl;
        await SaveApiBaseUrlAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task UseAdbLocalhostEndpointAsync()
    {
        ApiBaseUrl = MobileEndpointDefaults.AdbReverseLocalhostBaseUrl;
        await SaveApiBaseUrlAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveApiBaseUrlAsync()
    {
        ClearMessages();
        if (!DefaultApiEndpointProvider.TryNormalize(ApiBaseUrl, out _))
        {
            ErrorMessage = "Enter a valid http:// or https:// API base URL (e.g. http://10.0.2.2:5080).";
            TrackUserAction("Settings.UpdateApiEndpoint", "Settings", ApiBaseUrl, "Failed");
            return;
        }

        await _endpointProvider.SetBaseUrlAsync(ApiBaseUrl).ConfigureAwait(false);
        ApiBaseUrl = _endpointProvider.CurrentBaseUrl;
        SuccessMessage = $"API endpoint updated to {ApiBaseUrl}";
        TrackUserAction("Settings.UpdateApiEndpoint", "Settings", ApiBaseUrl, "Success");
        await _feedbackService.ShowToastAsync(SuccessMessage).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task TestApiHealthAsync()
    {
        ClearMessages();
        if (DefaultApiEndpointProvider.TryNormalize(ApiBaseUrl, out _) &&
            !string.Equals(ApiBaseUrl.TrimEnd('/'), _endpointProvider.CurrentBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _endpointProvider.SetBaseUrlAsync(ApiBaseUrl).ConfigureAwait(false);
        }

        var health = await _apiClient.GetHealthAsync(probeDb: true).ConfigureAwait(false);
        if (health.IsSuccess && health.Data is not null)
        {
            IsApiHealthy = true;
            HealthStatusMessage = $"{health.Data.Status} · {health.Data.Database}";
            TrackUserAction("Settings.TestApiHealth", "Settings", _endpointProvider.CurrentBaseUrl, "Success");
        }
        else
        {
            IsApiHealthy = false;
            HealthStatusMessage = health.ErrorMessage;
            TrackUserAction("Settings.TestApiHealth", "Settings", _endpointProvider.CurrentBaseUrl, "Failed");
        }
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ClearMessages();
        var actionName = IsRegisterMode ? "Auth.Register" : "Auth.Login";
        var validationResult = IsRegisterMode
            ? _registerValidator.Validate(new RegisterInput(Email, Password, FullName, PhoneNumber))
            : _loginValidator.Validate(new LoginInput(Email, Password));

        if (!validationResult.IsValid)
        {
            ErrorMessage = validationResult.Errors[0].ErrorMessage;
            TrackUserAction(actionName, "Auth", "ValidationFailed", "Failed");
            _hapticService.Perform(HapticKind.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            if (DefaultApiEndpointProvider.TryNormalize(ApiBaseUrl, out _) &&
                !string.Equals(ApiBaseUrl.TrimEnd('/'), _endpointProvider.CurrentBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                await _endpointProvider.SetBaseUrlAsync(ApiBaseUrl).ConfigureAwait(false);
            }

            var authResult = IsRegisterMode
                ? await _apiClient.RegisterCustomerAsync(new RegisterCustomerRequest
                {
                    Email = Email.Trim(),
                    Password = Password,
                    FullName = FullName.Trim(),
                    PhoneNumber = PhoneNumber.Trim()
                }).ConfigureAwait(false)
                : await _apiClient.LoginAsync(new LoginRequest
                {
                    Email = Email.Trim(),
                    Password = Password
                }).ConfigureAwait(false);

            if (!authResult.IsSuccess || authResult.Data is null)
            {
                ErrorMessage = authResult.ErrorMessage;
                TrackUserAction(actionName, "Auth", Email.Trim(), "Failed");
                _hapticService.Perform(HapticKind.Warning);
                return;
            }

            var tokenData = authResult.Data;
            await _tokenStore.SaveTokensAsync(
                new AuthTokens(tokenData.AccessToken, tokenData.RefreshToken, tokenData.ExpiresAtUtc))
                .ConfigureAwait(false);

            _sessionState.ApplyAuthenticatedUser(tokenData.User);
            TrackUserAction(actionName, "Auth", $"{tokenData.User.Role}", "Success");
            var decision = MobileRoleGuard.EvaluateSession(_sessionState);

            if (decision.Mode == MobileAccessMode.StaffDealerUnassigned)
            {
                await _logoutCoordinator.ForceSignOutAsync(
                    decision.BlockingReasonCode ?? "STAFF_DEALER_UNASSIGNED",
                    decision.BlockingMessage).ConfigureAwait(false);
                ErrorMessage = decision.BlockingMessage ?? "DealerStaff branch is unassigned.";
                await _feedbackService.ShowSnackbarAsync(ErrorMessage).ConfigureAwait(false);
                return;
            }

            if (_offlineDb is not null)
            {
                await _offlineDb.SaveLastAuthenticatedUserAsync(tokenData.User).ConfigureAwait(false);
            }

            _hapticService.Perform(HapticKind.Success);
            var signInToast = IsRegisterMode
                ? $"Account created — welcome, {tokenData.User.FullName}!"
                : $"Signed in as {tokenData.User.FullName}";
            await _feedbackService.ShowToastAsync(signInToast).ConfigureAwait(false);

            if (decision.Mode == MobileAccessMode.AdminWebOnly)
            {
                // Platform Admin is redirected strictly to the Web Console notice screen (no operational mobile tabs).
                await _navigationService.GoToAsync(MobileRoutes.AdminWebConsoleNotice).ConfigureAwait(false);
                return;
            }

            if (decision.Mode == MobileAccessMode.Customer)
            {
                _messenger.Send(new WalletMutatedMessage("SessionSignedIn"));
            }

            await _signalRService.StartAsync().ConfigureAwait(false);
            Password = string.Empty;
            await _navigationService.GoToAsync(decision.DefaultRoute).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
