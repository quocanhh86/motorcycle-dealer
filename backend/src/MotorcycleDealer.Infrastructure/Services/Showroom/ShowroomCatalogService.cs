using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

public sealed class ShowroomCatalogService : IShowroomCatalogService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly IValidator<PaginationQuery> _paginationValidator;
    private readonly IValidator<UpsertMotorcycleModelRequest> _upsertModelValidator;
    private readonly IValidator<UpsertMaintenanceRuleRequest> _upsertRuleValidator;

    public ShowroomCatalogService(
        MotorcycleDealerDbContext dbContext,
        IValidator<PaginationQuery> paginationValidator,
        IValidator<UpsertMotorcycleModelRequest> upsertModelValidator,
        IValidator<UpsertMaintenanceRuleRequest> upsertRuleValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _paginationValidator = paginationValidator ?? throw new ArgumentNullException(nameof(paginationValidator));
        _upsertModelValidator = upsertModelValidator ?? throw new ArgumentNullException(nameof(upsertModelValidator));
        _upsertRuleValidator = upsertRuleValidator ?? throw new ArgumentNullException(nameof(upsertRuleValidator));
    }

    public async Task<PagedResult<MotorcycleModelDto>> ListModelsAsync(
        ShowroomCatalogQuery query,
        bool canViewInactive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _paginationValidator.ValidateAndThrowAsync(query, cancellationToken);

        var includeInactive = canViewInactive && query.IncludeInactive;
        var modelsQuery = _dbContext.MotorcycleModels.AsNoTracking();

        if (!includeInactive)
        {
            modelsQuery = modelsQuery.Where(m => m.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!ShowroomValidationRules.TryNormalizeCategory(query.Category, out var normalizedCategory))
            {
                return PagedResult<MotorcycleModelDto>.Create(
                    [],
                    query.NormalizedPageNumber,
                    query.NormalizedPageSize,
                    totalCount: 0);
            }

            modelsQuery = modelsQuery.Where(m => m.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            var trimmedBrand = query.Brand.Trim();
            modelsQuery = modelsQuery.Where(m => m.Brand == trimmedBrand);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            modelsQuery = modelsQuery.Where(m =>
                EF.Functions.Like(m.ModelCode, pattern) ||
                EF.Functions.Like(m.Brand, pattern) ||
                EF.Functions.Like(m.ModelName, pattern) ||
                EF.Functions.Like(m.Category, pattern));
        }

        var totalCount = await modelsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<MotorcycleModelDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var models = await modelsQuery
            .OrderBy(m => m.Brand)
            .ThenBy(m => m.BaseRetailPrice)
            .ThenBy(m => m.ModelCode)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var dtos = await HydrateModelDtosAsync(
            models,
            filterStockDealerId: query.DealerId,
            includeInactiveRules: includeInactive,
            cancellationToken);

        return PagedResult<MotorcycleModelDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<MotorcycleModelDto> GetModelByIdAsync(
        Guid modelId,
        bool canViewInactive,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.MotorcycleModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

        if (model is null || (!canViewInactive && !model.IsActive))
        {
            throw NotFoundException.ForEntity("MotorcycleModel", modelId);
        }

        var hydrated = await HydrateModelDtosAsync(
            [model],
            filterStockDealerId: null,
            includeInactiveRules: canViewInactive,
            cancellationToken);

        return hydrated[0];
    }

    public async Task<MotorcycleModelDto> CreateModelAsync(
        UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertModelValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedCode = request.ModelCode.Trim().ToUpperInvariant();
        ShowroomValidationRules.TryNormalizeCategory(request.Category, out var normalizedCategory);

        var codeExists = await _dbContext.MotorcycleModels
            .IgnoreQueryFilters()
            .AnyAsync(m => m.ModelCode == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"Motorcycle model code '{normalizedCode}' already exists.",
                "DUPLICATE_MODEL_CODE");
        }

        var model = new MotorcycleModel(Guid.CreateVersion7())
        {
            ModelCode = normalizedCode,
            Brand = request.Brand.Trim(),
            ModelName = request.ModelName.Trim(),
            Category = normalizedCategory,
            EngineDisplacementCc = request.EngineDisplacementCc,
            BaseRetailPrice = request.BaseRetailPrice,
            WarrantyMonths = request.WarrantyMonths,
            WarrantyMileageKm = request.WarrantyMileageKm,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            SpecificationsJson = string.IsNullOrWhiteSpace(request.SpecificationsJson) ? null : request.SpecificationsJson.Trim(),
            IsActive = request.IsActive
        };

        _dbContext.MotorcycleModels.Add(model);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Motorcycle model code '{normalizedCode}' already exists.",
                "DUPLICATE_MODEL_CODE");
        }

        return new MotorcycleModelDto(
            Id: model.Id,
            ModelCode: model.ModelCode,
            Brand: model.Brand,
            ModelName: model.ModelName,
            Category: model.Category,
            EngineDisplacementCc: model.EngineDisplacementCc,
            BaseRetailPrice: model.BaseRetailPrice,
            WarrantyMonths: model.WarrantyMonths,
            WarrantyMileageKm: model.WarrantyMileageKm,
            ImageUrl: model.ImageUrl,
            SpecificationsJson: model.SpecificationsJson,
            AvailableStockCount: 0,
            IsActive: model.IsActive,
            CreatedAtUtc: model.CreatedAtUtc,
            MaintenanceRules: [],
            DealerStockBreakdown: []);
    }

    public async Task<MotorcycleModelDto> UpdateModelAsync(
        Guid modelId,
        UpsertMotorcycleModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertModelValidator.ValidateAndThrowAsync(request, cancellationToken);

        var model = await _dbContext.MotorcycleModels
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken)
            ?? throw NotFoundException.ForEntity("MotorcycleModel", modelId);

        var normalizedCode = request.ModelCode.Trim().ToUpperInvariant();
        ShowroomValidationRules.TryNormalizeCategory(request.Category, out var normalizedCategory);

        if (!string.Equals(model.ModelCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            var duplicateExists = await _dbContext.MotorcycleModels
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id != modelId && m.ModelCode == normalizedCode, cancellationToken);

            if (duplicateExists)
            {
                throw new ConflictException(
                    $"Motorcycle model code '{normalizedCode}' already exists.",
                    "DUPLICATE_MODEL_CODE");
            }
        }

        model.ModelCode = normalizedCode;
        model.Brand = request.Brand.Trim();
        model.ModelName = request.ModelName.Trim();
        model.Category = normalizedCategory;
        model.EngineDisplacementCc = request.EngineDisplacementCc;
        model.BaseRetailPrice = request.BaseRetailPrice;
        model.WarrantyMonths = request.WarrantyMonths;
        model.WarrantyMileageKm = request.WarrantyMileageKm;
        model.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        model.SpecificationsJson = string.IsNullOrWhiteSpace(request.SpecificationsJson) ? null : request.SpecificationsJson.Trim();
        model.IsActive = request.IsActive;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Motorcycle model code '{normalizedCode}' already exists.",
                "DUPLICATE_MODEL_CODE");
        }

        return await GetModelByIdAsync(model.Id, canViewInactive: true, cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRuleDto>> ListMaintenanceRulesAsync(
        Guid modelId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.MotorcycleModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

        if (model is null || (!includeInactive && !model.IsActive))
        {
            throw NotFoundException.ForEntity("MotorcycleModel", modelId);
        }

        var rulesQuery = _dbContext.MaintenanceRules
            .AsNoTracking()
            .Where(r => r.MotorcycleModelId == modelId);

        if (!includeInactive)
        {
            rulesQuery = rulesQuery.Where(r => r.IsActive);
        }

        var rules = await rulesQuery
            .OrderBy(r => r.IntervalKm)
            .ThenBy(r => r.IntervalMonths)
            .ToListAsync(cancellationToken);

        return rules.Select(MapRuleToDto).ToList();
    }

    public async Task<MaintenanceRuleDto> CreateMaintenanceRuleAsync(
        Guid modelId,
        UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertRuleValidator.ValidateAndThrowAsync(request, cancellationToken);

        var modelExists = await _dbContext.MotorcycleModels
            .AsNoTracking()
            .AnyAsync(m => m.Id == modelId, cancellationToken);

        if (!modelExists)
        {
            throw NotFoundException.ForEntity("MotorcycleModel", modelId);
        }

        var normalizedCode = request.MilestoneCode.Trim().ToUpperInvariant();
        var duplicateExists = await _dbContext.MaintenanceRules
            .AsNoTracking()
            .AnyAsync(
                r => r.MotorcycleModelId == modelId && r.MilestoneCode == normalizedCode,
                cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                $"Maintenance milestone code '{normalizedCode}' already exists for this motorcycle model.",
                "DUPLICATE_MILESTONE_CODE");
        }

        var rule = new MaintenanceRule(Guid.CreateVersion7())
        {
            MotorcycleModelId = modelId,
            MilestoneCode = normalizedCode,
            MilestoneName = request.MilestoneName.Trim(),
            IntervalKm = request.IntervalKm,
            IntervalMonths = request.IntervalMonths,
            IsRecurring = request.IsRecurring,
            LeadTimeDays = request.LeadTimeDays,
            LeadMileageKm = request.LeadMileageKm,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            ChecklistSummary = string.IsNullOrWhiteSpace(request.ChecklistSummary)
                ? request.MilestoneName.Trim()
                : request.ChecklistSummary.Trim(),
            IsActive = request.IsActive
        };

        _dbContext.MaintenanceRules.Add(rule);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Maintenance milestone code '{normalizedCode}' already exists for this motorcycle model.",
                "DUPLICATE_MILESTONE_CODE");
        }

        return MapRuleToDto(rule);
    }

    public async Task<MaintenanceRuleDto> UpdateMaintenanceRuleAsync(
        Guid modelId,
        Guid ruleId,
        UpsertMaintenanceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertRuleValidator.ValidateAndThrowAsync(request, cancellationToken);

        var rule = await _dbContext.MaintenanceRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.MotorcycleModelId == modelId, cancellationToken)
            ?? throw NotFoundException.ForEntity("MaintenanceRule", ruleId);

        var normalizedCode = request.MilestoneCode.Trim().ToUpperInvariant();
        if (!string.Equals(rule.MilestoneCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            var duplicateExists = await _dbContext.MaintenanceRules
                .AsNoTracking()
                .AnyAsync(
                    r => r.MotorcycleModelId == modelId &&
                         r.Id != ruleId &&
                         r.MilestoneCode == normalizedCode,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new ConflictException(
                    $"Maintenance milestone code '{normalizedCode}' already exists for this motorcycle model.",
                    "DUPLICATE_MILESTONE_CODE");
            }
        }

        rule.MilestoneCode = normalizedCode;
        rule.MilestoneName = request.MilestoneName.Trim();
        rule.IntervalKm = request.IntervalKm;
        rule.IntervalMonths = request.IntervalMonths;
        rule.IsRecurring = request.IsRecurring;
        rule.LeadTimeDays = request.LeadTimeDays;
        rule.LeadMileageKm = request.LeadMileageKm;
        rule.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        rule.ChecklistSummary = string.IsNullOrWhiteSpace(request.ChecklistSummary)
            ? request.MilestoneName.Trim()
            : request.ChecklistSummary.Trim();
        rule.IsActive = request.IsActive;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Maintenance milestone code '{normalizedCode}' already exists for this motorcycle model.",
                "DUPLICATE_MILESTONE_CODE");
        }

        return MapRuleToDto(rule);
    }

    internal static MaintenanceRuleDto MapRuleToDto(MaintenanceRule rule) =>
        new(
            Id: rule.Id,
            MotorcycleModelId: rule.MotorcycleModelId,
            MilestoneCode: rule.MilestoneCode,
            MilestoneName: rule.MilestoneName,
            IntervalKm: rule.IntervalKm,
            IntervalMonths: rule.IntervalMonths,
            IsRecurring: rule.IsRecurring,
            LeadTimeDays: rule.LeadTimeDays,
            LeadMileageKm: rule.LeadMileageKm,
            EstimatedDurationMinutes: rule.EstimatedDurationMinutes,
            ChecklistSummary: rule.ChecklistSummary,
            IsActive: rule.IsActive,
            CreatedAtUtc: rule.CreatedAtUtc);

    private async Task<IReadOnlyList<MotorcycleModelDto>> HydrateModelDtosAsync(
        IReadOnlyList<MotorcycleModel> models,
        Guid? filterStockDealerId,
        bool includeInactiveRules,
        CancellationToken cancellationToken)
    {
        if (models.Count == 0)
        {
            return [];
        }

        var modelIds = models.Select(m => m.Id).ToArray();

        var rulesQuery = _dbContext.MaintenanceRules
            .AsNoTracking()
            .Where(r => modelIds.Contains(r.MotorcycleModelId));

        if (!includeInactiveRules)
        {
            rulesQuery = rulesQuery.Where(r => r.IsActive);
        }

        var rules = await rulesQuery
            .OrderBy(r => r.IntervalKm)
            .ThenBy(r => r.IntervalMonths)
            .ToListAsync(cancellationToken);

        var rulesByModel = rules
            .GroupBy(r => r.MotorcycleModelId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MaintenanceRuleDto>)g.Select(MapRuleToDto).ToList());

        var stockRows = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(inv =>
                modelIds.Contains(inv.MotorcycleModelId) &&
                inv.StockStatus == "Available" &&
                inv.Dealer != null &&
                inv.Dealer.IsActive &&
                !inv.Dealer.IsDeleted)
            .GroupBy(inv => new
            {
                inv.MotorcycleModelId,
                inv.DealerId,
                DealerCode = inv.Dealer!.Code,
                DealerName = inv.Dealer.Name,
                DealerCity = inv.Dealer.City
            })
            .Select(g => new
            {
                g.Key.MotorcycleModelId,
                g.Key.DealerId,
                g.Key.DealerCode,
                g.Key.DealerName,
                g.Key.DealerCity,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var breakdownByModel = stockRows
            .GroupBy(x => x.MotorcycleModelId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DealerModelStockSummaryDto>)g
                    .OrderBy(x => x.DealerCode, StringComparer.Ordinal)
                    .Select(x => new DealerModelStockSummaryDto(
                        DealerId: x.DealerId,
                        DealerCode: x.DealerCode,
                        DealerName: x.DealerName,
                        City: x.DealerCity,
                        AvailableCount: x.Count))
                    .ToList());

        return models
            .Select(m =>
            {
                var breakdown = breakdownByModel.GetValueOrDefault(m.Id, []);
                var availableCount = filterStockDealerId is { } targetDealerId && targetDealerId != Guid.Empty
                    ? breakdown.Where(b => b.DealerId == targetDealerId).Sum(b => b.AvailableCount)
                    : breakdown.Sum(b => b.AvailableCount);

                return new MotorcycleModelDto(
                    Id: m.Id,
                    ModelCode: m.ModelCode,
                    Brand: m.Brand,
                    ModelName: m.ModelName,
                    Category: m.Category,
                    EngineDisplacementCc: m.EngineDisplacementCc,
                    BaseRetailPrice: m.BaseRetailPrice,
                    WarrantyMonths: m.WarrantyMonths,
                    WarrantyMileageKm: m.WarrantyMileageKm,
                    ImageUrl: m.ImageUrl,
                    SpecificationsJson: m.SpecificationsJson,
                    AvailableStockCount: availableCount,
                    IsActive: m.IsActive,
                    CreatedAtUtc: m.CreatedAtUtc,
                    MaintenanceRules: rulesByModel.GetValueOrDefault(m.Id, []),
                    DealerStockBreakdown: breakdown);
            })
            .ToList();
    }
}
