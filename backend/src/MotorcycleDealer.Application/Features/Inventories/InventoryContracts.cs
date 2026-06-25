using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Inventories;

public sealed record DealerInventoryDto(
    Guid Id,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid MotorcycleModelId,
    string ModelCode,
    string Brand,
    string MotorcycleModelName,
    string Category,
    string Vin,
    string EngineNumber,
    string ColorName,
    short ManufactureYear,
    decimal ListedPrice,
    string StockStatus,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? SoldAtUtc,
    string RowVersion);

public sealed record DealerInventoryQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    Guid? DealerId = null,
    Guid? ModelId = null,
    string? StockStatus = null,
    string? Vin = null,
    string? Search = null,
    bool PublicOnly = false) : PaginationQuery(PageNumber, PageSize);

public sealed record CreateDealerInventoryRequest
{
    public Guid? DealerId { get; init; }
    public Guid MotorcycleModelId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string EngineNumber { get; init; } = string.Empty;
    public string ColorName { get; init; } = string.Empty;
    public int ManufactureYear { get; init; } = 2026;
    public decimal ListedPrice { get; init; }
}

public sealed record UpdateDealerInventoryRequest
{
    public Guid? DealerId { get; init; }
    public string? StockStatus { get; init; }
    public decimal? ListedPrice { get; init; }
    public string? ColorName { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public interface IDealerInventoryService
{
    Task<PagedResult<DealerInventoryDto>> ListInventoriesAsync(
        DealerInventoryQuery query,
        CancellationToken cancellationToken = default);

    Task<DealerInventoryDto> GetInventoryByIdAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    Task<DealerInventoryDto> CreateInventoryAsync(
        CreateDealerInventoryRequest request,
        CancellationToken cancellationToken = default);

    Task<DealerInventoryDto> UpdateInventoryAsync(
        Guid inventoryId,
        UpdateDealerInventoryRequest request,
        CancellationToken cancellationToken = default);
}
