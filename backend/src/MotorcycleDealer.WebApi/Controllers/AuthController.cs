using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthController(
        IAuthService authService,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiEnvelope<AuthTokenData>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiEnvelope<AuthTokenData>>> RegisterAsync(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenData = await _authService.RegisterCustomerAsync(
            request,
            ResolveClientIp(),
            cancellationToken);

        var envelope = WrapSuccess(tokenData);
        Response.Headers.Location = "/api/v1/auth/me";
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiEnvelope<AuthTokenData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiEnvelope<AuthTokenData>>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenData = await _authService.LoginAsync(
                request,
                ResolveClientIp(),
                cancellationToken);

            return Ok(WrapSuccess(tokenData));
        }
        catch (AuthAuthenticationException ex)
        {
            return CreateUnauthorizedProblemResult(ex);
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ApiEnvelope<AuthTokenData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiEnvelope<AuthTokenData>>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenData = await _authService.RefreshTokenAsync(
                request,
                ResolveClientIp(),
                cancellationToken);

            return Ok(WrapSuccess(tokenData));
        }
        catch (AuthAuthenticationException ex)
        {
            return CreateUnauthorizedProblemResult(ex);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<LogoutResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiEnvelope<LogoutResultDto>>> LogoutAsync(
        [FromBody] LogoutRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserContext.RequireUserId();
        var result = await _authService.LogoutAsync(userId, request, cancellationToken);
        return Ok(WrapSuccess(result));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<CurrentUserDto>>> GetMeAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserContext.RequireUserId();
        var profile = await _authService.GetCurrentUserProfileAsync(userId, cancellationToken);
        return Ok(WrapSuccess(profile));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));

    private ObjectResult CreateUnauthorizedProblemResult(AuthAuthenticationException exception)
    {
        var problem = GlobalExceptionHandler.CreateCustomProblemDetails(
            HttpContext,
            StatusCodes.Status401Unauthorized,
            title: exception.Title,
            detail: exception.Message,
            errorCode: exception.ErrorCode);

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status401Unauthorized,
            ContentTypes = { "application/problem+json" }
        };
    }

    private string? ResolveClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
