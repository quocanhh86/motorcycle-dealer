using MotorcycleDealer.Application.Common.Models;
using ResponseMeta = MotorcycleDealer.Application.Features.Auth.ResponseMeta;

namespace MotorcycleDealer.Application.Features.Showroom;

public sealed record PagedApiEnvelope<T>(
    bool Success,
    IReadOnlyList<T> Data,
    PaginationMetadata Pagination,
    ResponseMeta Meta);

public static class PagedApiEnvelope
{
    public static PagedApiEnvelope<T> Success<T>(
        PagedResult<T> pagedResult,
        DateTimeOffset timestampUtc,
        string traceId)
    {
        ArgumentNullException.ThrowIfNull(pagedResult);
        return new PagedApiEnvelope<T>(
            Success: true,
            Data: pagedResult.Items,
            Pagination: pagedResult.Pagination,
            Meta: new ResponseMeta(TimestampUtc: timestampUtc, TraceId: traceId));
    }
}

public sealed record DealerModelStockSummaryDto(
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string City,
    int AvailableCount);

public sealed record MaintenanceRuleDto(
    Guid Id,
    Guid MotorcycleModelId,
    string MilestoneCode,
    string MilestoneName,
    int IntervalKm,
    int IntervalMonths,
    bool IsRecurring,
    int LeadTimeDays,
    int LeadMileageKm,
    int EstimatedDurationMinutes,
    string? ChecklistSummary,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record MotorcycleModelDto(
    Guid Id,
    string ModelCode,
    string Brand,
    string ModelName,
    string Category,
    int EngineDisplacementCc,
    decimal BaseRetailPrice,
    int WarrantyMonths,
    int WarrantyMileageKm,
    string? ImageUrl,
    string? SpecificationsJson,
    int AvailableStockCount,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<MaintenanceRuleDto> MaintenanceRules,
    IReadOnlyList<DealerModelStockSummaryDto> DealerStockBreakdown);

public sealed record ShowroomCatalogQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    string? Category = null,
    string? Brand = null,
    string? Search = null,
    Guid? DealerId = null,
    bool IncludeInactive = false) : PaginationQuery(PageNumber, PageSize);

public sealed record UpsertMotorcycleModelRequest
{
    public string ModelCode { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int EngineDisplacementCc { get; init; }
    public decimal BaseRetailPrice { get; init; }
    public int WarrantyMonths { get; init; } = 36;
    public int WarrantyMileageKm { get; init; } = 30_000;
    public string? ImageUrl { get; init; }
    public string? SpecificationsJson { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record UpsertMaintenanceRuleRequest
{
    public Guid? Id { get; init; }
    public Guid? RuleId { get; init; }
    public string MilestoneCode { get; init; } = string.Empty;
    public string MilestoneName { get; init; } = string.Empty;
    public int IntervalKm { get; init; }
    public int IntervalMonths { get; init; }
    public bool IsRecurring { get; init; } = true;
    public int LeadTimeDays { get; init; } = 7;
    public int LeadMileageKm { get; init; } = 250;
    public int EstimatedDurationMinutes { get; init; } = 60;
    public string? ChecklistSummary { get; init; }
    public bool IsActive { get; init; } = true;

    public Guid? ResolveBodyRuleId() => RuleId ?? Id;
}

public interface IShowroomCatalogService
{
    Task<PagedResult<MotorcycleModelDto>> ListModelsAsync(
        ShowroomCatalogQuery query,
        bool canViewInactive,
        CancellationToken cancellationToken = default);

    Task<MotorcycleModelDto> GetModelByIdAsync(
        Guid modelId,
        bool canViewInactive,
        CancellationToken cancellationToken = default);

    Task<MotorcycleModelDto> CreateModelAsync(
        UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default);

    Task<MotorcycleModelDto> UpdateModelAsync(
        Guid modelId,
        UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceRuleDto>> ListMaintenanceRulesAsync(
        Guid modelId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRuleDto> CreateMaintenanceRuleAsync(
        Guid modelId,
        UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRuleDto> UpdateMaintenanceRuleAsync(
        Guid modelId,
        Guid ruleId,
        UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default);
}
