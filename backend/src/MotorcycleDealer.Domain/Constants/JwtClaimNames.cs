namespace MotorcycleDealer.Domain.Constants;

public static class JwtClaimNames
{
    public const string Sub = "sub";
    public const string Email = "email";
    public const string FullName = "full_name";
    public const string Role = "role";
    public const string DealerId = "dealer_id";
    public const string Permissions = "permissions";
    public const string PermissionSingle = "permission";
    public const string JwtId = "jti";
    public const string IssuedAt = "iat";
    public const string NotBefore = "nbf";
    public const string ExpiresAt = "exp";
    public const string Issuer = "iss";
    public const string Audience = "aud";
}
