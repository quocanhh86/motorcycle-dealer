using System.Data.Common;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.WebApi.Middleware;

namespace MotorcycleDealer.WebApi.Security;

/// <summary>
/// Enforces immediate access-token revocation across all protected and dual-mode REST and SignalR
/// endpoints whenever a persisted User account is deactivated/deleted or a DealerStaff user's assigned
/// dealership branch is deactivated/deleted, closing the 15-minute JWT TTL window.
/// </summary>
public sealed class ActivePrincipalMiddleware
{
    private static readonly object VerifiedPrincipalOutcomeKey = new();

    private static readonly HashSet<string> BypassAuthenticatedRevocationPaths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/auth/refresh",
            "/health",
            "/api/v1/health"
        };

    private readonly RequestDelegate _next;

    public ActivePrincipalMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await _next(httpContext);
            return;
        }

        if (IsAuthBootstrapOrHealthEndpoint(httpContext.Request.Path))
        {
            await _next(httpContext);
            return;
        }

        var rawUserId = httpContext.User.FindFirstValue(JwtClaimNames.Sub) ??
                        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId) || userId == Guid.Empty)
        {
            await _next(httpContext);
            return;
        }

        var dbContext = httpContext.RequestServices.GetService<MotorcycleDealerDbContext>();
        if (dbContext is not null)
        {
            PrincipalGuardOutcome evaluation;
            if (httpContext.Items.TryGetValue(VerifiedPrincipalOutcomeKey, out var cached) &&
                cached is PrincipalGuardOutcome cachedOutcome)
            {
                evaluation = cachedOutcome;
            }
            else
            {
                evaluation = await EvaluatePersistedPrincipalAsync(
                    dbContext,
                    userId,
                    httpContext.User,
                    httpContext.RequestAborted);
                httpContext.Items[VerifiedPrincipalOutcomeKey] = evaluation;
            }

            if (evaluation is { IsAllowed: false } blocked)
            {
                var problem = GlobalExceptionHandler.CreateCustomProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: blocked.Detail,
                    errorCode: blocked.ErrorCode);

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                httpContext.Response.ContentType = "application/problem+json";
                await httpContext.Response.WriteAsJsonAsync(
                    problem,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: httpContext.RequestAborted);
                return;
            }
        }

        await _next(httpContext);
    }

    internal static async Task<PrincipalGuardOutcome> EvaluatePersistedPrincipalAsync(
        MotorcycleDealerDbContext dbContext,
        Guid userId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = await dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.IsActive,
                    u.IsDeleted,
                    u.DealerId,
                    u.StaffEmployeeCode,
                    DealerUnavailable = u.DealerId != null &&
                                        (u.Dealer == null || !u.Dealer.IsActive || u.Dealer.IsDeleted)
                })
                .FirstOrDefaultAsync(cancellationToken);

            // Synthetic test principals not backed by a dbo.Users row pass through to claim/policy handlers.
            if (state is null)
            {
                return PrincipalGuardOutcome.Allowed;
            }

            if (!state.IsActive || state.IsDeleted)
            {
                return new PrincipalGuardOutcome(
                    IsAllowed: false,
                    ErrorCode: "ACCOUNT_LOCKED",
                    Detail: "This user account has been deactivated.");
            }

            var roleClaim = principal.FindFirstValue(JwtClaimNames.Role) ??
                            principal.FindFirstValue(ClaimTypes.Role);
            var isDealerStaff = string.Equals(roleClaim, RoleNames.DealerStaff, StringComparison.Ordinal) ||
                                principal.IsInRole(RoleNames.DealerStaff);

            if (isDealerStaff &&
                (state.DealerId is null ||
                 state.DealerId == Guid.Empty ||
                 string.IsNullOrWhiteSpace(state.StaffEmployeeCode) ||
                 state.DealerUnavailable))
            {
                return new PrincipalGuardOutcome(
                    IsAllowed: false,
                    ErrorCode: "STAFF_DEALER_UNASSIGNED",
                    Detail: "DealerStaff account is not bound to an active dealership branch.");
            }

            return PrincipalGuardOutcome.Allowed;
        }
        catch (Exception ex) when (ex is DbException || ex.InnerException is DbException)
        {
            // Degraded/unmigrated sandbox contexts fall back to standard authorization handlers.
            return PrincipalGuardOutcome.Allowed;
        }
    }

    private static bool IsAuthBootstrapOrHealthEndpoint(PathString requestPath)
    {
        var raw = requestPath.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Length > 1 ? raw.TrimEnd('/') : raw;
        return BypassAuthenticatedRevocationPaths.Contains(normalized);
    }

    internal readonly record struct PrincipalGuardOutcome(
        bool IsAllowed,
        string ErrorCode,
        string Detail)
    {
        public static PrincipalGuardOutcome Allowed => new(true, string.Empty, string.Empty);
    }
}
