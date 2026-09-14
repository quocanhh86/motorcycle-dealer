using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Persistence;

namespace MotorcycleDealer.Mobile.Core.Security;

public sealed class SessionResetEventArgs(string reasonCode, string? message, bool wasForced) : EventArgs
{
    public string ReasonCode { get; } = reasonCode;
    public string? Message { get; } = message;
    public bool WasForced { get; } = wasForced;
}

public interface ISessionResetCoordinator
{
    event EventHandler<SessionResetEventArgs>? SessionReset;
    Task ForceSignOutAsync(string reasonCode, string? message = null, CancellationToken cancellationToken = default);
}

public interface ILogoutCoordinator : ISessionResetCoordinator
{
    Task<LogoutResultDto> LogoutAsync(bool revokeAllDevices = false, CancellationToken cancellationToken = default);
    void RegisterRealtimeStopHook(Func<CancellationToken, Task> stopHook);
}

/// <summary>
/// Coordinates single-device or all-device logout and forced security sign-outs:
/// posts POST /api/v1/auth/logout with RevokeAllDevices=false by default (preserving Web SPA sessions),
/// stops SignalR, clears SecureTokenStore, purges user/dealer SQLCipher rows (preserving public showroom cache),
/// and resets MobileSessionState.
/// </summary>
public sealed class LogoutCoordinator : ILogoutCoordinator
{
    private readonly ISecureTokenStore _tokenStore;
    private readonly IAuthRefreshClient _authRefreshClient;
    private readonly IMobileOfflineDb _offlineDb;
    private readonly MobileSessionState _sessionState;
    private readonly IUserActionLogger? _userActionLogger;
    private readonly SemaphoreSlim _resetGate = new(1, 1);
    private Func<CancellationToken, Task>? _realtimeStopHook;

    public LogoutCoordinator(
        ISecureTokenStore tokenStore,
        IAuthRefreshClient authRefreshClient,
        IMobileOfflineDb offlineDb,
        MobileSessionState sessionState,
        IUserActionLogger? userActionLogger = null)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _authRefreshClient = authRefreshClient ?? throw new ArgumentNullException(nameof(authRefreshClient));
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _userActionLogger = userActionLogger;
    }

    public event EventHandler<SessionResetEventArgs>? SessionReset;

    public void RegisterRealtimeStopHook(Func<CancellationToken, Task> stopHook)
    {
        _realtimeStopHook = stopHook;
    }

    public async Task<LogoutResultDto> LogoutAsync(
        bool revokeAllDevices = false,
        CancellationToken cancellationToken = default)
    {
        await _resetGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accessToken = await _tokenStore.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var refreshToken = await _tokenStore.GetRefreshTokenAsync(cancellationToken).ConfigureAwait(false);

            LogoutResultDto serverResult = new(Revoked: false, RevokedTokenCount: 0);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var request = new LogoutRequest
                {
                    RefreshToken = refreshToken,
                    RevokeAllDevices = revokeAllDevices
                };

                var response = await _authRefreshClient.LogoutAsync(accessToken, request, cancellationToken)
                    .ConfigureAwait(false);
                if (response.IsSuccess && response.Data is not null)
                {
                    serverResult = response.Data;
                }
            }

            var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
            activeLogger?.Track(
                action: "Auth.Logout",
                category: "Auth",
                target: revokeAllDevices ? "AllDevices" : "CurrentDevice",
                outcome: "Success");
            if (activeLogger is not null)
            {
                await activeLogger.FlushPendingAsync(cancellationToken).ConfigureAwait(false);
            }

            await WipeLocalSessionStateAsync(cancellationToken).ConfigureAwait(false);
            SessionReset?.Invoke(this, new SessionResetEventArgs("USER_LOGOUT", "Signed out cleanly.", wasForced: false));
            return serverResult;
        }
        finally
        {
            _resetGate.Release();
        }
    }

    public async Task ForceSignOutAsync(
        string reasonCode,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        await _resetGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var activeLogger = _userActionLogger ?? MobileUserActionLogHub.Current;
            activeLogger?.Track(
                action: "Auth.ForceSignOut",
                category: "Auth",
                target: string.IsNullOrWhiteSpace(reasonCode) ? "FORCED_SIGNOUT" : reasonCode,
                outcome: "Denied");
            await WipeLocalSessionStateAsync(cancellationToken).ConfigureAwait(false);
            SessionReset?.Invoke(
                this,
                new SessionResetEventArgs(
                    string.IsNullOrWhiteSpace(reasonCode) ? "FORCED_SIGNOUT" : reasonCode,
                    message ?? "Your session was terminated. Please sign in again.",
                    wasForced: true));
        }
        finally
        {
            _resetGate.Release();
        }
    }

    private async Task WipeLocalSessionStateAsync(CancellationToken cancellationToken)
    {
        if (_realtimeStopHook is not null)
        {
            try
            {
                await _realtimeStopHook(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort realtime shutdown during sign-out.
            }
        }

        await _tokenStore.ClearAuthTokensAsync(cancellationToken).ConfigureAwait(false);
        await _offlineDb.ClearLastAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);
        await _offlineDb.PurgeUserScopedDataAsync(cancellationToken).ConfigureAwait(false);
        _sessionState.Clear();
    }
}
