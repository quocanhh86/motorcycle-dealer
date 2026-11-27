using System.Net;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Contracts;

public sealed record ResponseMeta(
    DateTimeOffset TimestampUtc,
    string TraceId);

public sealed record PaginationMeta(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record ApiEnvelope<T>(
    bool Success,
    T Data,
    ResponseMeta Meta);

public sealed record PagedApiEnvelope<T>(
    bool Success,
    IReadOnlyList<T> Data,
    PaginationMeta Pagination,
    ResponseMeta Meta);

public sealed record NotificationFeedMeta(
    DateTimeOffset TimestampUtc,
    string TraceId,
    int UnreadCount);

public sealed record NotificationFeedEnvelope(
    bool Success,
    IReadOnlyList<NotificationDto> Data,
    int UnreadCount,
    PaginationMeta Pagination,
    NotificationFeedMeta Meta);

public sealed record ProblemDetailsPayload
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public string? ErrorCode { get; init; }
    public string? TraceId { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }

    public string GetFriendlyMessage() => GetLocalizedMessage(LocalizationService.Current);

    public string GetLocalizedMessage(LocalizationService? localizationService = null)
    {
        var loc = localizationService ?? LocalizationService.Current;

        if (!string.IsNullOrWhiteSpace(ErrorCode))
        {
            var normalizedCode = ErrorCode.Trim().ToUpperInvariant().Replace('-', '_');
            if (AppResources.TryGetString($"Err_{normalizedCode}", out var localizedByCode, loc.CurrentCulture))
            {
                return localizedByCode;
            }
        }

        if (Errors is { Count: > 0 })
        {
            var firstError = Errors.Values
                .SelectMany(v => v ?? [])
                .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg));
            if (!string.IsNullOrWhiteSpace(firstError))
            {
                return firstError;
            }
        }

        if (!string.IsNullOrWhiteSpace(Detail))
        {
            return Detail;
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            return Title;
        }

        return loc.Err_REQUEST_FAILED;
    }
}

public sealed record ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public PaginationMeta? Pagination { get; init; }
    public int? UnreadCount { get; init; }
    public ProblemDetailsPayload? Problem { get; init; }
    public int StatusCode { get; init; }
    public bool IsOfflineFallback { get; init; }
    public bool IsNetworkError { get; init; }

    public bool IsConflict409 => StatusCode == (int)HttpStatusCode.Conflict;
    public bool IsUnauthorized401 => StatusCode == (int)HttpStatusCode.Unauthorized;
    public bool IsForbidden403 => StatusCode == (int)HttpStatusCode.Forbidden;
    public bool IsUnprocessable422 => StatusCode == (int)HttpStatusCode.UnprocessableEntity;
    public bool IsServerError5xx => StatusCode >= 500 && StatusCode < 600;

    public string ErrorMessage => GetLocalizedErrorMessage(LocalizationService.Current);

    public string GetLocalizedErrorMessage(LocalizationService? localizationService = null)
    {
        var loc = localizationService ?? LocalizationService.Current;
        return Problem?.GetLocalizedMessage(loc) ??
               (IsNetworkError ? loc.Err_NETWORK_OFFLINE : loc.Err_REQUEST_FAILED);
    }

    public static ApiResult<T> Ok(
        T data,
        int statusCode = 200,
        PaginationMeta? pagination = null,
        int? unreadCount = null,
        bool isOfflineFallback = false) =>
        new()
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode,
            Pagination = pagination,
            UnreadCount = unreadCount,
            IsOfflineFallback = isOfflineFallback
        };

    public static ApiResult<T> Fail(
        int statusCode,
        ProblemDetailsPayload? problem = null,
        bool isNetworkError = false) =>
        new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Problem = problem,
            IsNetworkError = isNetworkError
        };
}
