namespace MotorcycleDealer.Application.Common.Models;

public sealed record ResponseMeta(
    DateTimeOffset TimestampUtc,
    string TraceId);

public sealed record ApiResponse<T>(
    bool Success,
    T Data,
    ResponseMeta Meta);

public sealed record PaginationMetadata(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    PaginationMetadata Pagination)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Max(1, pageSize);
        var safeTotalCount = Math.Max(0, totalCount);
        var totalPages = safeTotalCount == 0 ? 0 : (int)Math.Ceiling(safeTotalCount / (double)safePageSize);

        var metadata = new PaginationMetadata(
            PageNumber: safePageNumber,
            PageSize: safePageSize,
            TotalCount: safeTotalCount,
            TotalPages: totalPages,
            HasPreviousPage: safePageNumber > 1 && totalPages > 0,
            HasNextPage: safePageNumber < totalPages);

        return new PagedResult<T>(items, metadata);
    }
}
