using System.Collections.Frozen;
using System.Security.Claims;
using System.Text.Json;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Constants;

namespace MotorcycleDealer.WebApi.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private static readonly object CachedPermissionsItemKey = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal? _cachedPrincipal;
    private IReadOnlySet<string>? _cachedPermissions;

    private sealed record PrincipalPermissionCacheEntry(
        ClaimsPrincipal Principal,
        IReadOnlySet<string> Permissions);

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(JwtClaimNames.Sub) ??
                      Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var parsed) ? parsed : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(JwtClaimNames.Email) ??
        Principal?.FindFirstValue(ClaimTypes.Email);

    public string? Role =>
        Principal?.FindFirstValue(JwtClaimNames.Role) ??
        Principal?.FindFirstValue(ClaimTypes.Role);

    public Guid? DealerId
    {
        get
        {
            var raw = Principal?.FindFirstValue(JwtClaimNames.DealerId);
            return Guid.TryParse(raw, out var parsed) ? parsed : null;
        }
    }

    public IReadOnlySet<string> Permissions
    {
        get
        {
            var principal = Principal;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return FrozenSet<string>.Empty;
            }

            if (ReferenceEquals(principal, _cachedPrincipal) && _cachedPermissions is not null)
            {
                return _cachedPermissions;
            }

            var extracted = GetOrExtractPermissions(principal, _httpContextAccessor.HttpContext);
            _cachedPrincipal = principal;
            _cachedPermissions = extracted;
            return extracted;
        }
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission) || !IsAuthenticated)
        {
            return false;
        }

        return Permissions.Contains(permission);
    }

    public static IReadOnlySet<string> GetOrExtractPermissions(ClaimsPrincipal principal, HttpContext? httpContext)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return FrozenSet<string>.Empty;
        }

        if (httpContext is not null &&
            httpContext.Items.TryGetValue(CachedPermissionsItemKey, out var rawEntry) &&
            rawEntry is PrincipalPermissionCacheEntry cached &&
            ReferenceEquals(cached.Principal, principal))
        {
            return cached.Permissions;
        }

        var extracted = ExtractPermissionClaims(principal);
        if (httpContext is not null)
        {
            httpContext.Items[CachedPermissionsItemKey] = new PrincipalPermissionCacheEntry(principal, extracted);
        }

        return extracted;
    }

    internal static IReadOnlySet<string> ExtractPermissionClaims(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return FrozenSet<string>.Empty;
        }

        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!string.Equals(claim.Type, JwtClaimNames.Permissions, StringComparison.Ordinal) &&
                !string.Equals(claim.Type, JwtClaimNames.PermissionSingle, StringComparison.Ordinal))
            {
                continue;
            }

            var value = claim.Value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (value.StartsWith('[') && value.EndsWith(']'))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<string[]>(value);
                    if (items is not null)
                    {
                        foreach (var item in items)
                        {
                            if (!string.IsNullOrWhiteSpace(item))
                            {
                                set.Add(item.Trim());
                            }
                        }
                    }

                    continue;
                }
                catch (JsonException)
                {
                    // Fall through to literal value handling if malformed JSON array.
                }
            }

            set.Add(value);
        }

        return set.Count == 0
            ? FrozenSet<string>.Empty
            : set.ToFrozenSet(StringComparer.Ordinal);
    }
}
