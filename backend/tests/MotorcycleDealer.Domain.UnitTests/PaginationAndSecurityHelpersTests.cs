using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Security;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class PaginationAndSecurityHelpersTests
{
    [Theory]
    [InlineData(-10, 0, 1, 1, 0)]
    [InlineData(0, -5, 1, 1, 0)]
    [InlineData(1, 20, 1, 20, 0)]
    [InlineData(3, 25, 3, 25, 50)]
    [InlineData(2, 500, 2, 100, 100)]
    public void PaginationQuery_ShouldNormalizeOutOfRangeBounds_AndComputeExactSkipCount(
        int rawPageNumber,
        int rawPageSize,
        int expectedPageNumber,
        int expectedPageSize,
        int expectedSkipCount)
    {
        var query = new PaginationQuery(rawPageNumber, rawPageSize);

        Assert.Equal(expectedPageNumber, query.NormalizedPageNumber);
        Assert.Equal(expectedPageSize, query.NormalizedPageSize);
        Assert.Equal(expectedSkipCount, query.SkipCount);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 20, true)]
    [InlineData(5, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(-1, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -10, false)]
    [InlineData(1, 101, false)]
    public void PaginationQueryValidator_ShouldValidateBoundaryInputs(
        int pageNumber,
        int pageSize,
        bool expectedIsValid)
    {
        var validator = new PaginationQueryValidator();
        var result = validator.Validate(new PaginationQuery(pageNumber, pageSize));

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData(1, 20, 0, 0, false, false)]
    [InlineData(1, 20, 20, 1, false, false)]
    [InlineData(1, 20, 40, 2, false, true)]
    [InlineData(2, 20, 40, 2, true, false)]
    [InlineData(2, 20, 41, 3, true, true)]
    [InlineData(3, 20, 41, 3, true, false)]
    public void PagedResultCreate_ShouldComputeExactTotalPagesAndNavigationFlags(
        int pageNumber,
        int pageSize,
        int totalCount,
        int expectedTotalPages,
        bool expectedHasPrevious,
        bool expectedHasNext)
    {
        var paged = PagedResult<string>.Create(["sample"], pageNumber, pageSize, totalCount);

        Assert.Equal(pageNumber, paged.Pagination.PageNumber);
        Assert.Equal(pageSize, paged.Pagination.PageSize);
        Assert.Equal(totalCount, paged.Pagination.TotalCount);
        Assert.Equal(expectedTotalPages, paged.Pagination.TotalPages);
        Assert.Equal(expectedHasPrevious, paged.Pagination.HasPreviousPage);
        Assert.Equal(expectedHasNext, paged.Pagination.HasNextPage);
    }

    [Fact]
    public void HttpCurrentUserContext_ShouldCachePermissionSetPerPrincipal_AndReturnEmptyFrozenSetWhenAnonymous()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var anonymousContext = new HttpCurrentUserContext(accessor);

        Assert.False(anonymousContext.IsAuthenticated);
        Assert.Empty(anonymousContext.Permissions);
        Assert.False(anonymousContext.HasPermission(Permissions.Showroom.View));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtClaimNames.Sub, Guid.CreateVersion7().ToString("D")),
            new Claim(JwtClaimNames.Role, RoleNames.Customer),
            new Claim(JwtClaimNames.Permissions, Permissions.Bookings.Create),
            new Claim(JwtClaimNames.Permissions, Permissions.Vehicles.ViewOwn)
        ], "Bearer"));

        accessor.HttpContext.User = principal;

        var firstRead = anonymousContext.Permissions;
        var secondRead = anonymousContext.Permissions;

        Assert.Same(firstRead, secondRead);
        Assert.True(anonymousContext.HasPermission(Permissions.Bookings.Create));
        Assert.False(anonymousContext.HasPermission(Permissions.Analytics.ViewAllDealers));
    }

    [Fact]
    public void CorsOptions_ShouldFilterWildcardAndEmptyEntries_AndFallbackToDefaultSpaOrigins()
    {
        var wildcardOptions = new CorsOptions { AllowedOrigins = ["*", "  ", ""] };
        Assert.Equal(CorsOptions.DefaultAllowedOrigins, wildcardOptions.ResolveEffectiveOrigins());

        var customOptions = new CorsOptions { AllowedOrigins = ["http://localhost:4200/", "https://spa.dealer.vn/"] };
        Assert.Equal(["http://localhost:4200", "https://spa.dealer.vn"], customOptions.ResolveEffectiveOrigins());
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_AndHttpCurrentUserContext_ShouldShareRequestPermissionCache()
    {
        var httpContext = new DefaultHttpContext();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtClaimNames.Sub, Guid.CreateVersion7().ToString("D")),
            new Claim(JwtClaimNames.Role, RoleNames.DealerStaff),
            new Claim(JwtClaimNames.DealerId, Guid.CreateVersion7().ToString("D")),
            new Claim(JwtClaimNames.Permissions, $"[\"{Permissions.Bookings.ViewDealer}\",\"{Permissions.Invoices.CreateDealer}\"]")
        ], "Bearer"));
        httpContext.User = principal;

        var handler = new PermissionAuthorizationHandler();
        var reqAllowed = new PermissionRequirement(Permissions.Invoices.CreateDealer);
        var reqDenied = new PermissionRequirement(Permissions.Analytics.ViewAllDealers);

        var authContextAllowed = new AuthorizationHandlerContext([reqAllowed], principal, httpContext);
        await handler.HandleAsync(authContextAllowed);
        Assert.True(authContextAllowed.HasSucceeded);

        var authContextDenied = new AuthorizationHandlerContext([reqDenied], principal, httpContext);
        await handler.HandleAsync(authContextDenied);
        Assert.False(authContextDenied.HasSucceeded);

        var userContext = new HttpCurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });
        var handlerCachedSet = HttpCurrentUserContext.GetOrExtractPermissions(principal, httpContext);

        Assert.Same(handlerCachedSet, userContext.Permissions);
        Assert.True(userContext.HasPermission(Permissions.Bookings.ViewDealer));
    }
}
