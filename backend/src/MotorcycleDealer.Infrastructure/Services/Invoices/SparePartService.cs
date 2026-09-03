using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Services.Showroom;

namespace MotorcycleDealer.Infrastructure.Services.Invoices;

public sealed class SparePartService : ISparePartService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<PaginationQuery> _paginationValidator;
    private readonly IValidator<UpsertSparePartRequest> _upsertValidator;

    public SparePartService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<PaginationQuery> paginationValidator,
        IValidator<UpsertSparePartRequest> upsertValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _paginationValidator = paginationValidator ?? throw new ArgumentNullException(nameof(paginationValidator));
        _upsertValidator = upsertValidator ?? throw new ArgumentNullException(nameof(upsertValidator));
    }

    public async Task<PagedResult<SparePartDto>> ListSparePartsAsync(
        SparePartCatalogQuery query,
        bool canViewInactive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _paginationValidator.ValidateAndThrowAsync(query, cancellationToken);

        var includeInactive = canViewInactive && query.IncludeInactive;
        var partsQuery = _dbContext.SpareParts
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!includeInactive)
        {
            partsQuery = partsQuery.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!SparePartValidationRules.TryNormalizeCategory(query.Category, out var parsedCategory))
            {
                return PagedResult<SparePartDto>.Create(
                    [],
                    query.NormalizedPageNumber,
                    query.NormalizedPageSize,
                    totalCount: 0);
            }

            partsQuery = partsQuery.Where(p => p.Category == parsedCategory);
        }

        if (query.CompatibleModelId is { } modelId && modelId != Guid.Empty)
        {
            partsQuery = query.IncludeUniversal
                ? partsQuery.Where(p => p.CompatibleModelId == modelId || p.CompatibleModelId == null)
                : partsQuery.Where(p => p.CompatibleModelId == modelId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            partsQuery = partsQuery.Where(p =>
                EF.Functions.Like(p.PartCode, pattern) ||
                EF.Functions.Like(p.Name, pattern) ||
                EF.Functions.Like(p.UnitLabel, pattern) ||
                (p.CompatibleModel != null && (
                    EF.Functions.Like(p.CompatibleModel.ModelName, pattern) ||
                    EF.Functions.Like(p.CompatibleModel.ModelCode, pattern))));
        }

        var totalCount = await partsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<SparePartDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var parts = await partsQuery
            .Include(p => p.CompatibleModel)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.PartCode)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var dtos = parts.Select(MapToDto).ToList();

        return PagedResult<SparePartDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<SparePartDto> GetSparePartByIdAsync(
        Guid partId,
        bool canViewInactive,
        CancellationToken cancellationToken = default)
    {
        var part = await _dbContext.SpareParts
            .AsNoTracking()
            .Include(p => p.CompatibleModel)
            .FirstOrDefaultAsync(p => p.Id == partId && !p.IsDeleted, cancellationToken);

        if (part is null || (!canViewInactive && !part.IsActive))
        {
            throw NotFoundException.ForEntity("SparePart", partId);
        }

        return MapToDto(part);
    }

    public async Task<SparePartDto> CreateSparePartAsync(
        UpsertSparePartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.SpareParts.ManageCatalog))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage the OEM spare-parts catalog.",
                "FORBIDDEN_PERMISSION");
        }

        var normalizedCode = request.PartCode.Trim().ToUpperInvariant();
        SparePartValidationRules.TryNormalizeCategory(request.Category, out var parsedCategory);
        var compatibleModelId = request.ResolveNormalizedCompatibleModelId();
        var roundedUnitPrice = Math.Round(request.UnitPriceVnd, 2, MidpointRounding.AwayFromZero);

        var codeExists = await _dbContext.SpareParts
            .IgnoreQueryFilters()
            .AnyAsync(p => p.PartCode == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"Spare part code '{normalizedCode}' already exists.",
                "DUPLICATE_PART_CODE");
        }

        if (compatibleModelId.HasValue)
        {
            var modelExists = await _dbContext.MotorcycleModels
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(m => m.Id == compatibleModelId.Value && !m.IsDeleted, cancellationToken);

            if (!modelExists)
            {
                throw NotFoundException.ForEntity("MotorcycleModel", compatibleModelId.Value);
            }
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var part = new SparePart(Guid.CreateVersion7())
        {
            PartCode = normalizedCode,
            Name = request.Name.Trim(),
            Category = parsedCategory,
            CompatibleModelId = compatibleModelId,
            UnitLabel = request.UnitLabel.Trim(),
            UnitPriceVnd = roundedUnitPrice,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAtUtc = utcNow
        };

        _dbContext.SpareParts.Add(part);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Spare part code '{normalizedCode}' already exists.",
                "DUPLICATE_PART_CODE");
        }

        return await GetSparePartByIdAsync(part.Id, canViewInactive: true, cancellationToken);
    }

    public async Task<SparePartDto> UpdateSparePartAsync(
        Guid partId,
        UpsertSparePartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _upsertValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.SpareParts.ManageCatalog))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage the OEM spare-parts catalog.",
                "FORBIDDEN_PERMISSION");
        }

        var part = await _dbContext.SpareParts
            .FirstOrDefaultAsync(p => p.Id == partId && !p.IsDeleted, cancellationToken)
            ?? throw NotFoundException.ForEntity("SparePart", partId);

        var normalizedCode = request.PartCode.Trim().ToUpperInvariant();
        SparePartValidationRules.TryNormalizeCategory(request.Category, out var parsedCategory);
        var compatibleModelId = request.ResolveNormalizedCompatibleModelId();
        var roundedUnitPrice = Math.Round(request.UnitPriceVnd, 2, MidpointRounding.AwayFromZero);

        if (!string.Equals(part.PartCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            var duplicateExists = await _dbContext.SpareParts
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id != partId && p.PartCode == normalizedCode, cancellationToken);

            if (duplicateExists)
            {
                throw new ConflictException(
                    $"Spare part code '{normalizedCode}' already exists.",
                    "DUPLICATE_PART_CODE");
            }
        }

        if (compatibleModelId.HasValue)
        {
            var modelExists = await _dbContext.MotorcycleModels
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(m => m.Id == compatibleModelId.Value && !m.IsDeleted, cancellationToken);

            if (!modelExists)
            {
                throw NotFoundException.ForEntity("MotorcycleModel", compatibleModelId.Value);
            }
        }

        part.PartCode = normalizedCode;
        part.Name = request.Name.Trim();
        part.Category = parsedCategory;
        part.CompatibleModelId = compatibleModelId;
        part.UnitLabel = request.UnitLabel.Trim();
        part.UnitPriceVnd = roundedUnitPrice;
        part.IsActive = request.IsActive;
        part.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
        {
            throw new ConflictException(
                $"Spare part code '{normalizedCode}' already exists.",
                "DUPLICATE_PART_CODE");
        }

        return await GetSparePartByIdAsync(part.Id, canViewInactive: true, cancellationToken);
    }

    private static SparePartDto MapToDto(SparePart part) =>
        new(
            Id: part.Id,
            PartId: part.Id,
            PartCode: part.PartCode,
            Name: part.Name,
            Category: part.Category.ToString(),
            CompatibleModelId: part.CompatibleModelId,
            CompatibleModelCode: part.CompatibleModel?.ModelCode,
            CompatibleModelName: part.CompatibleModel?.ModelName,
            IsUniversalFit: part.CompatibleModelId is null,
            UnitLabel: part.UnitLabel,
            UnitPriceVnd: part.UnitPriceVnd,
            IsActive: part.IsActive,
            CreatedAtUtc: part.CreatedAtUtc,
            UpdatedAtUtc: part.UpdatedAtUtc);
}
