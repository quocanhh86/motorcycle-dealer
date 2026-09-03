using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.SpareParts;

public sealed record SparePartDto(
    Guid Id,
    Guid PartId,
    string PartCode,
    string Name,
    string Category,
    Guid? CompatibleModelId,
    string? CompatibleModelCode,
    string? CompatibleModelName,
    bool IsUniversalFit,
    string UnitLabel,
    decimal UnitPriceVnd,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record SparePartCatalogQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    string? Category = null,
    Guid? CompatibleModelId = null,
    bool IncludeUniversal = true,
    string? Search = null,
    bool IncludeInactive = false) : PaginationQuery(PageNumber, PageSize);

public sealed record UpsertSparePartRequest
{
    public string PartCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Guid? CompatibleModelId { get; init; }
    public string UnitLabel { get; init; } = "Unit";
    public decimal UnitPriceVnd { get; init; }
    public bool IsActive { get; init; } = true;

    public Guid? ResolveNormalizedCompatibleModelId() =>
        CompatibleModelId is { } id && id != Guid.Empty ? id : null;
}

public interface ISparePartService
{
    Task<PagedResult<SparePartDto>> ListSparePartsAsync(
        SparePartCatalogQuery query,
        bool canViewInactive,
        CancellationToken cancellationToken = default);

    Task<SparePartDto> GetSparePartByIdAsync(
        Guid partId,
        bool canViewInactive,
        CancellationToken cancellationToken = default);

    Task<SparePartDto> CreateSparePartAsync(
        UpsertSparePartRequest request,
        CancellationToken cancellationToken = default);

    Task<SparePartDto> UpdateSparePartAsync(
        Guid partId,
        UpsertSparePartRequest request,
        CancellationToken cancellationToken = default);
}
