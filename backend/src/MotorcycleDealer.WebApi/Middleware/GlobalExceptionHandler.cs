using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.WebApi.Middleware;

/// <summary>
/// Translates domain, validation, concurrency, and unhandled exceptions into deterministic
/// RFC 7807 ProblemDetails (application/problem+json) envelopes without leaking internal details
/// outside the Development environment.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var traceId = ResolveTraceId(httpContext);
        var (statusCode, title, detail, errorCode, errors) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception processed by GlobalExceptionHandler. TraceId={TraceId}, Path={Path}",
                traceId,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Handled domain/request exception ({StatusCode} {ErrorCode}): {Message}. TraceId={TraceId}, Path={Path}",
                statusCode,
                errorCode,
                exception.Message,
                traceId,
                httpContext.Request.Path);
        }

        var problemDetails = CreateCustomProblemDetails(
            httpContext,
            statusCode,
            title,
            detail,
            errorCode,
            errors);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    public static ProblemDetails CreateCustomProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string errorCode,
        IDictionary<string, string[]>? errors = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path.Value
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = ResolveTraceId(httpContext);
        problemDetails.Extensions["errors"] = errors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
        return problemDetails;
    }

    public static ProblemDetails CreateStatusCodeProblemDetails(HttpContext httpContext, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var (title, detail, errorCode) = statusCode switch
        {
            StatusCodes.Status400BadRequest => (
                "Bad Request",
                "The request payload or parameters could not be understood by the server.",
                "BAD_REQUEST"),
            StatusCodes.Status401Unauthorized => (
                "Unauthorized",
                "Authentication is required or the supplied access token is invalid or expired.",
                "UNAUTHORIZED"),
            StatusCodes.Status403Forbidden => (
                "Forbidden",
                "You do not have sufficient permissions or dealer scope to perform this operation.",
                "FORBIDDEN_PERMISSION"),
            StatusCodes.Status404NotFound => (
                "Resource Not Found",
                $"The requested endpoint or resource '{httpContext.Request.Path}' was not found.",
                "RESOURCE_NOT_FOUND"),
            StatusCodes.Status405MethodNotAllowed => (
                "Method Not Allowed",
                $"HTTP method '{httpContext.Request.Method}' is not supported for '{httpContext.Request.Path}'.",
                "METHOD_NOT_ALLOWED"),
            StatusCodes.Status429TooManyRequests => (
                "Too Many Requests",
                "Rate limit exceeded. Please retry after a brief pause.",
                "RATE_LIMIT_EXCEEDED"),
            _ => (
                "Request Failed",
                $"The request failed with HTTP status code {statusCode}.",
                $"HTTP_{statusCode}")
        };

        return CreateCustomProblemDetails(httpContext, statusCode, title, detail, errorCode);
    }

    public static void EnrichProblemDetails(ProblemDetails problemDetails, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(httpContext);

        var statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;
        problemDetails.Status = statusCode;
        problemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
        problemDetails.Instance ??= httpContext.Request.Path.Value;
        problemDetails.Extensions["traceId"] = ResolveTraceId(httpContext);

        if (!problemDetails.Extensions.ContainsKey("errorCode"))
        {
            problemDetails.Extensions["errorCode"] = statusCode switch
            {
                StatusCodes.Status400BadRequest => "VALIDATION_FAILED",
                StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
                StatusCodes.Status403Forbidden => "FORBIDDEN_PERMISSION",
                StatusCodes.Status404NotFound => "RESOURCE_NOT_FOUND",
                StatusCodes.Status409Conflict => "CONCURRENCY_CONFLICT",
                StatusCodes.Status422UnprocessableEntity => "BUSINESS_RULE_VIOLATION",
                StatusCodes.Status429TooManyRequests => "RATE_LIMIT_EXCEEDED",
                StatusCodes.Status503ServiceUnavailable => "DATABASE_UNAVAILABLE",
                _ => "INTERNAL_SERVER_ERROR"
            };
        }

        if (problemDetails is not ValidationProblemDetails && !problemDetails.Extensions.ContainsKey("errors"))
        {
            problemDetails.Extensions["errors"] = new Dictionary<string, string[]>(StringComparer.Ordinal);
        }
    }

    public static string ResolveTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    private (int StatusCode, string Title, string Detail, string ErrorCode, IDictionary<string, string[]> Errors) MapException(
        Exception exception)
    {
        var emptyErrors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more request validation rules failed.",
                "VALIDATION_FAILED",
                validationException.Errors
                    .GroupBy(e => ToCamelCase(e.PropertyName), StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                        StringComparer.Ordinal)),

            UnauthorizedAccessException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                string.IsNullOrWhiteSpace(unauthorizedException.Message)
                    ? "Authentication is required to access this resource."
                    : unauthorizedException.Message,
                "UNAUTHORIZED",
                emptyErrors),

            ForbiddenAccessException forbiddenException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenException.Message,
                forbiddenException.ErrorCode,
                emptyErrors),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFoundException.Message,
                notFoundException.ErrorCode,
                emptyErrors),

            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Message,
                conflictException.ErrorCode,
                emptyErrors),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "The target record was modified by another transaction. Please refresh and retry with the latest RowVersion.",
                "CONCURRENCY_CONFLICT",
                emptyErrors),

            BusinessRuleViolationException businessRuleException => (
                StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation",
                businessRuleException.Message,
                businessRuleException.ErrorCode,
                emptyErrors),

            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                "Domain Validation Error",
                domainException.Message,
                domainException.ErrorCode,
                emptyErrors),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected server error occurred. Provide the traceId when contacting support.",
                "INTERNAL_SERVER_ERROR",
                emptyErrors)
        };
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "request";
        }

        if (propertyName.Length == 1)
        {
            return propertyName.ToLowerInvariant();
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
