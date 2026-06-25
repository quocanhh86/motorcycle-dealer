using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.ValueObjects;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

public sealed class DealerInventoryService : IDealerInventoryService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<PaginationQuery> _paginationValidator;
    private readonly IValidator<CreateDealerInventoryRequest> _createValidator;
    private readonly IValidator<UpdateDealerInventoryRequest> _updateValidator;

    public DealerInventoryService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<PaginationQuery> paginationValidator,
        IValidator<CreateDealerInventoryRequest> createValidator,
        IValidator<UpdateDealerInventoryRequest> updateValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _paginationValidator = paginationValidator ?? throw new ArgumentNullException(nameof(paginationValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    public async Task<PagedResult<DealerInventoryDto>> ListInventoriesAsync(
        DealerInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _paginationValidator.ValidateAndThrowAsync(query, cancellationToken);

        var hasDealerInventoryPermission =
            _currentUserContext.IsAuthenticated &&
            _currentUserContext.HasPermission(Permissions.Inventories.ViewDealer);

        if (!query.PublicOnly)
        {
            if (!_currentUserContext.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("Authentication is required to view dealer inventory records.");
            }

            if (!hasDealerInventoryPermission)
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view dealer showroom inventory.",
                    "FORBIDDEN_PERMISSION");
            }
        }

        IQueryable<DealerInventory> inventoriesQuery;

        if (query.PublicOnly)
        {
            inventoriesQuery = _dbContext.DealerInventories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(inv =>
                    inv.StockStatus == "Available" &&
                    inv.Dealer != null &&
                    inv.Dealer.IsActive &&
                    !inv.Dealer.IsDeleted &&
                    inv.MotorcycleModel != null &&
                    inv.MotorcycleModel.IsActive &&
                    !inv.MotorcycleModel.IsDeleted);

            if (query.DealerId is { } publicDealerId && publicDealerId != Guid.Empty)
            {
                inventoriesQuery = inventoriesQuery.Where(inv => inv.DealerId == publicDealerId);
            }
        }
        else
        {
            // Global IDealerScoped query filter is active on _dbContext.DealerInventories for DealerStaff.
            inventoriesQuery = _dbContext.DealerInventories.AsNoTracking();

            if (_currentUserContext.IsDealerStaff)
            {
                var staffDealerId = _currentUserContext.RequireAssignedDealerId();
                inventoriesQuery = inventoriesQuery.Where(inv => inv.DealerId == staffDealerId);
            }
            else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
            {
                inventoriesQuery = inventoriesQuery.Where(inv => inv.DealerId == adminDealerId);
            }

            if (!string.IsNullOrWhiteSpace(query.StockStatus))
            {
                if (!InventoryValidationRules.TryNormalizeStockStatus(query.StockStatus, out var normalizedStatus))
                {
                    return PagedResult<DealerInventoryDto>.Create(
                        [],
                        query.NormalizedPageNumber,
                        query.NormalizedPageSize,
                        totalCount: 0);
                }

                inventoriesQuery = inventoriesQuery.Where(inv => inv.StockStatus == normalizedStatus);
            }
        }

        if (query.ModelId is { } modelId && modelId != Guid.Empty)
        {
            inventoriesQuery = inventoriesQuery.Where(inv => inv.MotorcycleModelId == modelId);
        }

        if (!string.IsNullOrWhiteSpace(query.Vin))
        {
            var normalizedVin = query.Vin.Trim().ToUpperInvariant();
            if (normalizedVin.Length == Vin.CanonicalLength)
            {
                inventoriesQuery = inventoriesQuery.Where(inv => inv.VIN == normalizedVin);
            }
            else
            {
                var vinPattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(normalizedVin)}%";
                inventoriesQuery = inventoriesQuery.Where(inv => EF.Functions.Like(inv.VIN, vinPattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            inventoriesQuery = inventoriesQuery.Where(inv =>
                EF.Functions.Like(inv.VIN, pattern) ||
                EF.Functions.Like(inv.EngineNumber, pattern) ||
                EF.Functions.Like(inv.ColorName, pattern) ||
                (inv.MotorcycleModel != null && (
                    EF.Functions.Like(inv.MotorcycleModel.ModelCode, pattern) ||
                    EF.Functions.Like(inv.MotorcycleModel.ModelName, pattern))));
        }

        var totalCount = await inventoriesQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<DealerInventoryDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var rows = await inventoriesQuery
            .Include(inv => inv.Dealer)
            .Include(inv => inv.MotorcycleModel)
            .OrderByDescending(inv => inv.ReceivedAtUtc)
            .ThenBy(inv => inv.VIN)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var maskEngineNumber = query.PublicOnly && !hasDealerInventoryPermission;
        var dtos = rows.Select(inv => MapToDto(inv, maskEngineNumber)).ToList();

        return PagedResult<DealerInventoryDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<DealerInventoryDto> GetInventoryByIdAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(inv => inv.Dealer)
            .Include(inv => inv.MotorcycleModel)
            .FirstOrDefaultAsync(inv => inv.Id == inventoryId, cancellationToken)
            ?? throw NotFoundException.ForEntity("DealerInventory", inventoryId);

        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (inventory.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot view or manage inventory belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        return MapToDto(inventory);
    }

    public async Task<DealerInventoryDto> CreateInventoryAsync(
        CreateDealerInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid effectiveDealerId;
        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (request.DealerId is { } clientDealerId &&
                clientDealerId != Guid.Empty &&
                clientDealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot intake inventory for another dealership branch.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            effectiveDealerId = staffDealerId;
        }
        else
        {
            if (request.DealerId is null || request.DealerId == Guid.Empty)
            {
                throw new ValidationException(
                [
                    new ValidationFailure("dealerId", "dealerId is required when creating inventory as Admin.")
                ]);
            }

            effectiveDealerId = request.DealerId.Value;
        }

        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == effectiveDealerId, cancellationToken)
            ?? throw NotFoundException.ForEntity("Dealer", effectiveDealerId);

        if (!dealer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "DEALER_INACTIVE",
                $"Cannot intake showroom inventory for inactive dealership '{dealer.Code}'.");
        }

        var model = await _dbContext.MotorcycleModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MotorcycleModelId, cancellationToken)
            ?? throw NotFoundException.ForEntity("MotorcycleModel", request.MotorcycleModelId);

        if (!model.IsActive)
        {
            throw new BusinessRuleViolationException(
                "MODEL_INACTIVE",
                $"Cannot intake showroom inventory for inactive motorcycle model '{model.ModelCode}'.");
        }

        var normalizedVin = Vin.Create(request.Vin.Trim().ToUpperInvariant()).Value;
        var normalizedEngine = request.EngineNumber.Trim().ToUpperInvariant();

        var existsInInventory = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AnyAsync(inv => inv.VIN == normalizedVin, cancellationToken);

        var existsInCustomerGarage = !existsInInventory && await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.VIN == normalizedVin, cancellationToken);

        if (existsInInventory || existsInCustomerGarage)
        {
            throw new ConflictException(
                $"VIN '{normalizedVin}' already exists in platform showroom inventory or customer garage records.",
                "DUPLICATE_VIN");
        }

        var engineExists = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .AnyAsync(inv => inv.EngineNumber == normalizedEngine, cancellationToken);

        if (engineExists)
        {
            throw new ConflictException(
                $"Engine number '{normalizedEngine}' already exists in showroom inventory.",
                "DUPLICATE_ENGINE_NUMBER");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var inventory = new DealerInventory(Guid.CreateVersion7())
        {
            DealerId = effectiveDealerId,
            MotorcycleModelId = model.Id,
            VIN = normalizedVin,
            EngineNumber = normalizedEngine,
            ColorName = request.ColorName.Trim(),
            ManufactureYear = checked((short)request.ManufactureYear),
            ListedPrice = request.ListedPrice,
            StockStatus = "Available",
            ReceivedAtUtc = utcNow,
            SoldAtUtc = null,
            CreatedAtUtc = utcNow,
            Dealer = null,
            MotorcycleModel = null
        };

        _dbContext.DealerInventories.Add(inventory);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_DealerInventories_EngineNumber", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    $"Engine number '{normalizedEngine}' already exists in showroom inventory.",
                    "DUPLICATE_ENGINE_NUMBER");
            }

            throw new ConflictException(
                $"VIN '{normalizedVin}' already exists in platform showroom inventory or customer garage records.",
                "DUPLICATE_VIN");
        }

        inventory.Dealer = dealer;
        inventory.MotorcycleModel = model;
        return MapToDto(inventory);
    }

    public async Task<DealerInventoryDto> UpdateInventoryAsync(
        Guid inventoryId,
        UpdateDealerInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var inventory = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .Include(inv => inv.Dealer)
            .Include(inv => inv.MotorcycleModel)
            .FirstOrDefaultAsync(inv => inv.Id == inventoryId, cancellationToken)
            ?? throw NotFoundException.ForEntity("DealerInventory", inventoryId);

        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (inventory.DealerId != staffDealerId ||
                (request.DealerId is { } clientDealerId &&
                 clientDealerId != Guid.Empty &&
                 clientDealerId != staffDealerId))
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot modify inventory belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        if (inventory.Dealer is null || inventory.Dealer.IsDeleted || !inventory.Dealer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "DEALER_INACTIVE",
                $"Cannot modify showroom inventory for inactive dealership '{inventory.Dealer?.Code ?? inventory.DealerId.ToString()}'.");
        }

        if (string.Equals(inventory.StockStatus, "Sold", StringComparison.Ordinal))
        {
            throw new ConflictException(
                $"Inventory unit '{inventory.VIN}' has already been sold and cannot be modified.",
                "INVENTORY_ALREADY_SOLD");
        }

        var expectedRowVersion = RowVersionCodec.DecodeRequiredOrThrow(request.RowVersion);
        if (!inventory.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            throw new ConflictException(
                "The inventory record was modified by another transaction. Please refresh and retry with the latest RowVersion.",
                "CONCURRENCY_CONFLICT");
        }

        _dbContext.Entry(inventory).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

        var utcNow = _dateTimeProvider.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.StockStatus))
        {
            InventoryValidationRules.TryNormalizeStockStatus(request.StockStatus, out var normalizedStatus);
            if (string.Equals(normalizedStatus, nameof(StockStatus.Sold), StringComparison.Ordinal))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_STATUS_TRANSITION",
                    "Inventory units can only be transitioned to Sold via an atomic SalesOrder.");
            }

            if (string.Equals(normalizedStatus, nameof(StockStatus.Reserved), StringComparison.Ordinal) &&
                !string.Equals(inventory.StockStatus, nameof(StockStatus.Reserved), StringComparison.Ordinal))
            {
                var vietnamToday = TestDriveValidationRules.GetVietnamToday(utcNow);
                var assignedToUpcomingTestDrive = await _dbContext.TestDriveBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        t => t.AssignedInventoryId == inventory.Id &&
                             t.PreferredDate >= vietnamToday &&
                             t.Status == TestDriveStatus.Confirmed,
                        cancellationToken);

                if (assignedToUpcomingTestDrive)
                {
                    throw new ConflictException(
                        $"Inventory unit '{inventory.VIN}' is currently assigned to an upcoming confirmed showroom test-drive and cannot be marked Reserved.",
                        "INVENTORY_ASSIGNED_TO_TEST_DRIVE");
                }
            }

            inventory.StockStatus = normalizedStatus;
        }

        if (request.ListedPrice.HasValue)
        {
            inventory.ListedPrice = request.ListedPrice.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.ColorName))
        {
            inventory.ColorName = request.ColorName.Trim();
        }

        inventory.UpdatedAtUtc = utcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The inventory record was modified by another transaction. Please refresh and retry with the latest RowVersion.",
                "CONCURRENCY_CONFLICT");
        }

        return MapToDto(inventory);
    }

    internal static DealerInventoryDto MapToDto(DealerInventory inv, bool maskEngineNumber = false) =>
        new(
            Id: inv.Id,
            DealerId: inv.DealerId,
            DealerCode: inv.Dealer?.Code ?? string.Empty,
            DealerName: inv.Dealer?.Name ?? string.Empty,
            DealerCity: inv.Dealer?.City ?? string.Empty,
            MotorcycleModelId: inv.MotorcycleModelId,
            ModelCode: inv.MotorcycleModel?.ModelCode ?? string.Empty,
            Brand: inv.MotorcycleModel?.Brand ?? string.Empty,
            MotorcycleModelName: inv.MotorcycleModel?.ModelName ?? string.Empty,
            Category: inv.MotorcycleModel?.Category ?? string.Empty,
            Vin: inv.VIN,
            EngineNumber: maskEngineNumber ? string.Empty : inv.EngineNumber,
            ColorName: inv.ColorName,
            ManufactureYear: inv.ManufactureYear,
            ListedPrice: inv.ListedPrice,
            StockStatus: inv.StockStatus,
            ReceivedAtUtc: inv.ReceivedAtUtc,
            SoldAtUtc: inv.SoldAtUtc,
            RowVersion: RowVersionCodec.Encode(inv.RowVersion));
}
