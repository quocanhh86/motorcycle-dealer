namespace MotorcycleDealer.Application.Features.Auth;

public sealed record ResponseMeta(
    DateTimeOffset TimestampUtc,
    string TraceId);

public sealed record ApiEnvelope<T>(
    bool Success,
    T Data,
    ResponseMeta Meta);

public static class ApiEnvelope
{
    public static ApiEnvelope<T> Success<T>(T data, DateTimeOffset timestampUtc, string traceId) =>
        new(
            Success: true,
            Data: data,
            Meta: new ResponseMeta(TimestampUtc: timestampUtc, TraceId: traceId));
}

public sealed record RegisterCustomerRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record LogoutRequest
{
    public string? RefreshToken { get; init; }
    public bool RevokeAllDevices { get; init; }
}

public sealed record LogoutResultDto(
    bool Revoked,
    int RevokedTokenCount);

public sealed record CurrentUserDto(
    Guid UserId,
    string Email,
    string FullName,
    string PhoneNumber,
    string Role,
    Guid? DealerId,
    string? DealerCode,
    string? DealerName,
    string? StaffEmployeeCode,
    string? TierCode,
    decimal? TierMultiplier,
    int? AvailablePoints,
    int? LifetimeQualifyingPoints,
    decimal? LifetimeSpentVnd,
    IReadOnlyList<string> Permissions,
    decimal? PointsMultiplier = null);

public sealed record AuthTokenData(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    int ExpiresInSeconds,
    CurrentUserDto User);

public sealed class AuthAuthenticationException : UnauthorizedAccessException
{
    public AuthAuthenticationException(string errorCode, string title, string message)
        : base(message)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "UNAUTHORIZED" : errorCode;
        Title = string.IsNullOrWhiteSpace(title) ? "Unauthorized" : title;
    }

    public string ErrorCode { get; }
    public string Title { get; }
}

public interface IAuthService
{
    Task<AuthTokenData> RegisterCustomerAsync(
        RegisterCustomerRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    Task<AuthTokenData> LoginAsync(
        LoginRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    Task<AuthTokenData> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    Task<LogoutResultDto> LogoutAsync(
        Guid currentUserId,
        LogoutRequest? request,
        CancellationToken cancellationToken = default);

    Task<CurrentUserDto> GetCurrentUserProfileAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
