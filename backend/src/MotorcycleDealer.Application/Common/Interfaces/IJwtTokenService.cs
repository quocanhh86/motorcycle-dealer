namespace MotorcycleDealer.Application.Common.Interfaces;

public sealed record JwtTokenSubject(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? DealerId,
    IReadOnlyCollection<string> Permissions);

public sealed record IssuedAccessToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string JwtId);

public interface IJwtTokenService
{
    IssuedAccessToken GenerateAccessToken(JwtTokenSubject subject);
    string GenerateRefreshToken();
    string ComputeRefreshTokenHash(string rawRefreshToken);
}
