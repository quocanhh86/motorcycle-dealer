using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Services.Showroom;

namespace MotorcycleDealer.Infrastructure.Services.Warranty;

public sealed class WarrantyClaimService : IWarrantyClaimService
{
    public const int MaxWarrantyMonths = WarrantyClaimConstants.MaxWarrantyMonths;
    public const int MaxWarrantyMileageKm = WarrantyClaimConstants.MaxWarrantyMileageKm;

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<WarrantyClaimListQuery> _listQueryValidator;
    private readonly IValidator<CreateWarrantyClaimRequest> _createValidator;
    private readonly IValidator<UpdateWarrantyClaimStatusRequest> _updateStatusValidator;

    public WarrantyClaimService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<WarrantyClaimListQuery> listQueryValidator,
        IValidator<CreateWarrantyClaimRequest> createValidator,
        IValidator<UpdateWarrantyClaimStatusRequest> updateStatusValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _listQueryValidator = listQueryValidator ?? throw new ArgumentNullException(nameof(listQueryValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateStatusValidator = updateStatusValidator ?? throw new ArgumentNullException(nameof(updateStatusValidator));
    }

    public async Task<WarrantyClaimDto> CreateWarrantyClaimAsync(
        CreateWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.IsCustomer || !_currentUserContext.HasPermission(Permissions.Warranty.CreateOwn))
        {
            throw new ForbiddenAccessException(
                "Only authenticated Customer accounts may submit VIN warranty claims.",
                "FORBIDDEN_PERMISSION");
        }

        var targetVehicleId = request.ResolveEffectiveVehicleId();
        WarrantyClaimValidationRules.TryNormalizeDefectCategory(request.DefectCategory, out var defectCategory);
        var normalizedSymptom = request.SymptomDescription.Trim();

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (createdDto, createdEvent, targetDealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

            if (_dbContext.Database.IsRelational())
            {
                // Serialize concurrent warranty claims on the same CustomerVehicleId to prevent duplicate active claims.
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT [Id] FROM [dbo].[CustomerVehicles] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {targetVehicleId};",
                    ct);
            }

            var utcNow = _dateTimeProvider.UtcNow;

            // a) Load CustomerVehicle (CustomerId == callerUserId && !IsDeleted -> 404) and Dealer (!IsDeleted && IsActive -> 422 DEALER_INACTIVE)
            var vehicle = await _dbContext.CustomerVehicles
                .IgnoreQueryFilters()
                .Include(v => v.MotorcycleModel)
                .Include(v => v.PurchasedSalesOrder)
                .FirstOrDefaultAsync(
                    v => v.Id == targetVehicleId &&
                         v.CustomerId == callerUserId &&
                         !v.IsDeleted &&
                         v.IsActive,
                    ct)
                ?? throw NotFoundException.ForEntity("CustomerVehicle", targetVehicleId);

            var dealer = await _dbContext.Dealers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DealerId, ct)
                ?? throw NotFoundException.ForEntity("Dealer", request.DealerId);

            if (dealer.IsDeleted || !dealer.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "DEALER_INACTIVE",
                    $"Dealership '{dealer.Name}' ({dealer.Code}) is currently inactive and cannot accept warranty claims.");
            }

            var customer = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == callerUserId && !u.IsDeleted && u.IsActive, ct)
                ?? throw new UnauthorizedAccessException("Authenticated customer profile is missing or inactive.");

            // b) Verify monotonic odometer rule (OdometerAtClaimKm >= vehicle.CurrentMileageKm -> 422 ODOMETER_REGRESSION)
            if (request.OdometerAtClaimKm < vehicle.CurrentMileageKm)
            {
                throw new BusinessRuleViolationException(
                    "ODOMETER_REGRESSION",
                    $"Claim odometer ({request.OdometerAtClaimKm:N0} km) cannot be lower than recorded vehicle mileage ({vehicle.CurrentMileageKm:N0} km).");
            }

            // c) OEM Warranty Coverage Check: <= 36 months from purchase/onboarding AND <= 30,000 km
            var warrantyStartUtc = WarrantyClaimValidationRules.ResolveWarrantyStartUtc(
                vehicle.OwnershipStartDateUtc,
                vehicle.CreatedAtUtc,
                vehicle.PurchasedSalesOrder?.CompletedAtUtc);
            var warrantyExpiresAtUtc = WarrantyClaimValidationRules.ComputeWarrantyExpiresAtUtc(warrantyStartUtc);

            if (!WarrantyClaimValidationRules.IsWithinWarrantyCoverage(warrantyStartUtc, utcNow, request.OdometerAtClaimKm))
            {
                throw new BusinessRuleViolationException(
                    "WARRANTY_COVERAGE_EXPIRED",
                    $"OEM factory warranty coverage ({MaxWarrantyMonths} months / {MaxWarrantyMileageKm:N0} km) has expired for VIN '{vehicle.VIN}' (odometer: {request.OdometerAtClaimKm:N0} km, warranty window ended: {warrantyExpiresAtUtc:yyyy-MM-dd}).");
            }

            // d) Duplicate Active Claim Guard on (CustomerVehicleId, DefectCategory)
            var activeClaimExists = await _dbContext.WarrantyClaims
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    c => c.CustomerVehicleId == vehicle.Id &&
                         c.DefectCategory == defectCategory &&
                         (c.Status == WarrantyClaimStatus.Submitted ||
                          c.Status == WarrantyClaimStatus.UnderReview ||
                          c.Status == WarrantyClaimStatus.Approved),
                    ct);

            if (activeClaimExists)
            {
                throw new ConflictException(
                    $"An active warranty claim for '{defectCategory}' is already open on VIN '{vehicle.VIN}'.",
                    "ACTIVE_WARRANTY_CLAIM_EXISTS");
            }

            if (request.OdometerAtClaimKm > vehicle.CurrentMileageKm)
            {
                vehicle.CurrentMileageKm = request.OdometerAtClaimKm;
                vehicle.UpdatedAtUtc = utcNow;
            }

            // e) Persist WarrantyClaim + Notifications (Customer + Dealer)
            var claimId = Guid.CreateVersion7();
            var claimNumber = GenerateClaimNumber(utcNow, claimId);
            var model = vehicle.MotorcycleModel;
            var modelName = model?.ModelName ?? "Motorcycle";

            var claim = new WarrantyClaim(claimId)
            {
                ClaimNumber = claimNumber,
                CustomerId = callerUserId,
                CustomerVehicleId = vehicle.Id,
                DealerId = dealer.Id,
                VinSnapshot = vehicle.VIN,
                OdometerAtClaimKm = request.OdometerAtClaimKm,
                DefectCategory = defectCategory,
                SymptomDescription = normalizedSymptom,
                Status = WarrantyClaimStatus.Submitted,
                CoveredSparePartId = null,
                CoveredAmountVnd = 0.00m,
                ResolutionNotes = null,
                RejectionReason = null,
                ReviewedByUserId = null,
                ReviewedAtUtc = null,
                CompletedAtUtc = null,
                CreatedAtUtc = utcNow
            };
            _dbContext.WarrantyClaims.Add(claim);

            var dealerNotifId = Guid.CreateVersion7();
            var realtimeEvent = new WarrantyClaimCreatedRealtimeEvent(
                NotificationId: dealerNotifId,
                EventType: nameof(NotificationEventType.WarrantyClaimCreated),
                ClaimId: claim.Id,
                ClaimNumber: claim.ClaimNumber,
                DealerId: dealer.Id,
                DealerName: dealer.Name,
                CustomerFullName: customer.FullName,
                Vin: claim.VinSnapshot,
                MotorcycleModelName: modelName,
                DefectCategory: claim.DefectCategory.ToString(),
                OdometerAtClaimKm: claim.OdometerAtClaimKm,
                Status: claim.Status.ToString(),
                OccurredAtUtc: utcNow);

            var payloadJson = JsonSerializer.Serialize(realtimeEvent);

            _dbContext.Notifications.Add(new Notification(dealerNotifId)
            {
                TargetDealerId = dealer.Id,
                EventType = nameof(NotificationEventType.WarrantyClaimCreated),
                Title = $"New VIN Warranty Claim — {claim.ClaimNumber}",
                Message = $"{customer.FullName} filed a {claim.DefectCategory} warranty claim on {modelName} (VIN {claim.VinSnapshot}, {claim.OdometerAtClaimKm:N0} km).",
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAtUtc = utcNow
            });

            _dbContext.Notifications.Add(new Notification(Guid.CreateVersion7())
            {
                RecipientUserId = callerUserId,
                EventType = nameof(NotificationEventType.WarrantyClaimCreated),
                Title = $"Warranty Claim Submitted — {claim.ClaimNumber}",
                Message = $"Your {claim.DefectCategory} warranty claim {claim.ClaimNumber} for VIN {claim.VinSnapshot} has been submitted to {dealer.Name}.",
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAtUtc = utcNow
            });

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out _))
            {
                throw new ConflictException(
                    "A concurrent warranty claim persistence conflict occurred. Please retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await tx.CommitAsync(ct);

            var dto = MapToDto(
                claim: claim,
                customer: customer,
                vehicle: vehicle,
                model: model,
                dealer: dealer,
                coveredPart: null,
                reviewedByUser: null);

            return (dto, realtimeEvent, dealer.Id);
        }, cancellationToken);

        await _notificationPublisher.PublishWarrantyClaimCreatedAsync(
            createdEvent,
            targetDealerId,
            cancellationToken);

        return createdDto;
    }

    public async Task<PagedResult<WarrantyClaimDto>> ListWarrantyClaimsAsync(
        WarrantyClaimListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();

        WarrantyClaimStatus? statusFilter =
            !string.IsNullOrWhiteSpace(query.Status) &&
            WarrantyClaimValidationRules.TryNormalizeStatus(query.Status, out var parsedStatus)
                ? parsedStatus
                : null;

        WarrantyDefectCategory? categoryFilter =
            !string.IsNullOrWhiteSpace(query.DefectCategory) &&
            WarrantyClaimValidationRules.TryNormalizeDefectCategory(query.DefectCategory, out var parsedCategory)
                ? parsedCategory
                : null;

        IQueryable<WarrantyClaim> claimsQuery;

        if (forceOwnCustomerOnly)
        {
            if (!_currentUserContext.HasPermission(Permissions.Warranty.ViewOwn))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view personal VIN warranty claims.",
                    "FORBIDDEN_PERMISSION");
            }

            claimsQuery = _dbContext.WarrantyClaims
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.CustomerId == callerUserId);

            if (query.DealerId is { } custDealerId && custDealerId != Guid.Empty)
            {
                claimsQuery = claimsQuery.Where(c => c.DealerId == custDealerId);
            }
        }
        else
        {
            if (!_currentUserContext.HasPermission(Permissions.Warranty.ViewDealer))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view dealership VIN warranty claims.",
                    "FORBIDDEN_PERMISSION");
            }

            // Global IDealerScoped query filter is automatically active for DealerStaff.
            claimsQuery = _dbContext.WarrantyClaims.AsNoTracking();

            if (_currentUserContext.IsDealerStaff)
            {
                var staffDealerId = _currentUserContext.RequireAssignedDealerId();
                if (query.DealerId is { } requestedDealerId &&
                    requestedDealerId != Guid.Empty &&
                    requestedDealerId != staffDealerId)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff is strictly prohibited from viewing warranty claims belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                claimsQuery = claimsQuery.Where(c => c.DealerId == staffDealerId);
            }
            else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
            {
                claimsQuery = claimsQuery.Where(c => c.DealerId == adminDealerId);
            }
        }

        if (query.VehicleId is { } vehicleId && vehicleId != Guid.Empty)
        {
            claimsQuery = claimsQuery.Where(c => c.CustomerVehicleId == vehicleId);
        }

        if (statusFilter.HasValue)
        {
            claimsQuery = claimsQuery.Where(c => c.Status == statusFilter.Value);
        }

        if (categoryFilter.HasValue)
        {
            claimsQuery = claimsQuery.Where(c => c.DefectCategory == categoryFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            claimsQuery = claimsQuery.Where(c =>
                EF.Functions.Like(c.ClaimNumber, pattern) ||
                EF.Functions.Like(c.VinSnapshot, pattern) ||
                EF.Functions.Like(c.SymptomDescription, pattern) ||
                (c.Customer != null && (
                    EF.Functions.Like(c.Customer.FullName, pattern) ||
                    EF.Functions.Like(c.Customer.Email, pattern))) ||
                (c.CustomerVehicle != null &&
                 c.CustomerVehicle.MotorcycleModel != null &&
                 EF.Functions.Like(c.CustomerVehicle.MotorcycleModel.ModelName, pattern)));
        }

        var totalCount = await claimsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<WarrantyClaimDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var rows = await ApplyClaimReadIncludes(claimsQuery)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapLoadedClaimToDto).ToList();

        return PagedResult<WarrantyClaimDto>.Create(
            items,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<WarrantyClaimDto> GetWarrantyClaimByIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Warranty.ViewOwn);
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Warranty.ViewDealer);

        if (!canViewOwn && !canViewDealer)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view warranty claim details.",
                "FORBIDDEN_PERMISSION");
        }

        var claim = await ApplyClaimReadIncludes(
                _dbContext.WarrantyClaims
                    .IgnoreQueryFilters()
                    .AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken)
            ?? throw NotFoundException.ForEntity("WarrantyClaim", claimId);

        if (!canViewDealer)
        {
            if (claim.CustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view their own VIN warranty claims.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (claim.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view VIN warranty claims belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        return MapLoadedClaimToDto(claim);
    }

    public async Task<WarrantyClaimDto> UpdateWarrantyClaimStatusAsync(
        Guid claimId,
        UpdateWarrantyClaimStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var staffUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Warranty.ManageDealer))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to manage dealership VIN warranty claims.",
                "FORBIDDEN_PERMISSION");
        }

        Guid? staffDealerId = null;
        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
        }

        WarrantyClaimValidationRules.TryNormalizeStatus(request.ResolveEffectiveStatus(), out var targetStatus);
        var expectedRowVersion = RowVersionCodec.DecodeRequiredOrThrow(request.RowVersion);
        var requestedPartId = request.ResolveEffectiveSparePartId();
        var resolutionNotes = request.ResolveEffectiveResolutionNotes();
        var rejectionReason = request.ResolveEffectiveRejectionReason();

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (updatedDto, statusEvent, customerId, dealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

            var utcNow = _dateTimeProvider.UtcNow;

            var claim = await _dbContext.WarrantyClaims
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == claimId, ct)
                ?? throw NotFoundException.ForEntity("WarrantyClaim", claimId);

            if (staffDealerId.HasValue && claim.DealerId != staffDealerId.Value)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot update VIN warranty claims belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            var dealer = await _dbContext.Dealers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == claim.DealerId, ct);

            if (dealer is null || dealer.IsDeleted || !dealer.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "DEALER_INACTIVE",
                    "Cannot update warranty claims for an inactive dealership.");
            }

            if (!claim.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
            {
                throw new ConflictException(
                    "The warranty claim was modified concurrently by another user. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            _dbContext.Entry(claim).Property(c => c.RowVersion).OriginalValue = expectedRowVersion;

            if (!WarrantyClaimValidationRules.IsAllowedStatusTransition(claim.Status, targetStatus))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_WARRANTY_STATUS_TRANSITION",
                    $"Illegal warranty claim status transition from '{claim.Status}' to '{targetStatus}'.");
            }

            var vehicle = await _dbContext.CustomerVehicles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(v => v.MotorcycleModel)
                .Include(v => v.PurchasedSalesOrder)
                .FirstOrDefaultAsync(v => v.Id == claim.CustomerVehicleId, ct)
                ?? throw NotFoundException.ForEntity("CustomerVehicle", claim.CustomerVehicleId);

            var customer = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == claim.CustomerId, ct);

            var reviewedByUser = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == staffUserId, ct);

            SparePart? resolvedSparePart = null;
            var previousStatus = claim.Status.ToString();
            var previousCoveredPartId = claim.CoveredSparePartId;

            switch (targetStatus)
            {
                case WarrantyClaimStatus.UnderReview:
                    if (resolutionNotes is not null)
                    {
                        claim.ResolutionNotes = resolutionNotes;
                    }

                    if (claim.CoveredSparePartId.HasValue)
                    {
                        resolvedSparePart = await _dbContext.SpareParts
                            .IgnoreQueryFilters()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == claim.CoveredSparePartId.Value, ct);
                    }
                    break;

                case WarrantyClaimStatus.Approved:
                case WarrantyClaimStatus.Completed:
                    if (requestedPartId.HasValue)
                    {
                        var candidatePart = await _dbContext.SpareParts
                            .IgnoreQueryFilters()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == requestedPartId.Value, ct)
                            ?? throw NotFoundException.ForEntity("SparePart", requestedPartId.Value);

                        if (candidatePart.IsDeleted || !candidatePart.IsActive)
                        {
                            throw new BusinessRuleViolationException(
                                "SPARE_PART_INACTIVE",
                                $"Spare part '{candidatePart.PartCode}' ({candidatePart.Name}) is currently inactive and cannot be assigned to warranty repairs.");
                        }

                        if (candidatePart.CompatibleModelId.HasValue &&
                            candidatePart.CompatibleModelId.Value != vehicle.MotorcycleModelId)
                        {
                            throw new BusinessRuleViolationException(
                                "SPARE_PART_INCOMPATIBLE",
                                $"Spare part '{candidatePart.PartCode}' ({candidatePart.Name}) is incompatible with the vehicle's motorcycle model.");
                        }

                        claim.CoveredSparePartId = candidatePart.Id;
                        resolvedSparePart = candidatePart;
                    }
                    else if (claim.CoveredSparePartId.HasValue)
                    {
                        resolvedSparePart = await _dbContext.SpareParts
                            .IgnoreQueryFilters()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == claim.CoveredSparePartId.Value, ct);
                    }

                    if (request.CoveredAmountVnd.HasValue)
                    {
                        claim.CoveredAmountVnd = Math.Round(
                            request.CoveredAmountVnd.Value,
                            2,
                            MidpointRounding.AwayFromZero);
                    }
                    else if (resolvedSparePart is not null &&
                             (claim.CoveredAmountVnd == 0.00m ||
                              (requestedPartId.HasValue && previousCoveredPartId != resolvedSparePart.Id)))
                    {
                        claim.CoveredAmountVnd = resolvedSparePart.UnitPriceVnd;
                    }

                    claim.RejectionReason = null;

                    if (targetStatus == WarrantyClaimStatus.Completed)
                    {
                        claim.ResolutionNotes = resolutionNotes!;
                        claim.CompletedAtUtc = utcNow;
                    }
                    else if (resolutionNotes is not null)
                    {
                        claim.ResolutionNotes = resolutionNotes;
                    }
                    break;

                case WarrantyClaimStatus.Rejected:
                    claim.RejectionReason = rejectionReason!;
                    claim.CoveredSparePartId = null;
                    claim.CoveredAmountVnd = 0.00m;
                    resolvedSparePart = null;
                    break;
            }

            claim.Status = targetStatus;
            claim.ReviewedByUserId = staffUserId;
            claim.ReviewedAtUtc ??= utcNow;
            claim.UpdatedAtUtc = utcNow;

            var notifId = Guid.CreateVersion7();
            var realtimePayload = new WarrantyClaimStatusChangedRealtimeEvent(
                NotificationId: notifId,
                EventType: nameof(NotificationEventType.WarrantyClaimStatusChanged),
                ClaimId: claim.Id,
                ClaimNumber: claim.ClaimNumber,
                PreviousStatus: previousStatus,
                NewStatus: targetStatus.ToString(),
                DealerName: dealer.Name,
                Vin: claim.VinSnapshot,
                CoveredAmountVnd: claim.CoveredAmountVnd,
                OccurredAtUtc: utcNow);

            _dbContext.Notifications.Add(new Notification(notifId)
            {
                RecipientUserId = claim.CustomerId,
                TargetDealerId = claim.DealerId,
                EventType = nameof(NotificationEventType.WarrantyClaimStatusChanged),
                Title = $"Warranty Claim {claim.ClaimNumber} — {targetStatus}",
                Message = targetStatus == WarrantyClaimStatus.Rejected
                    ? $"Warranty claim {claim.ClaimNumber} (VIN {claim.VinSnapshot}) at {dealer.Name} was rejected: {claim.RejectionReason}"
                    : $"Warranty claim {claim.ClaimNumber} (VIN {claim.VinSnapshot}) at {dealer.Name} is now {targetStatus} (Covered: {claim.CoveredAmountVnd:N0} VND).",
                PayloadJson = JsonSerializer.Serialize(realtimePayload),
                IsRead = false,
                CreatedAtUtc = utcNow
            });

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Concurrent update detected while updating warranty claim status. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await tx.CommitAsync(ct);

            var dto = MapToDto(
                claim: claim,
                customer: customer,
                vehicle: vehicle,
                model: vehicle.MotorcycleModel,
                dealer: dealer,
                coveredPart: resolvedSparePart,
                reviewedByUser: reviewedByUser);

            return (dto, realtimePayload, claim.CustomerId, claim.DealerId);
        }, cancellationToken);

        await _notificationPublisher.PublishWarrantyClaimStatusChangedAsync(
            statusEvent,
            customerId,
            dealerId,
            cancellationToken);

        return updatedDto;
    }

    private static IQueryable<WarrantyClaim> ApplyClaimReadIncludes(IQueryable<WarrantyClaim> query) =>
        query
            .Include(c => c.Customer)
            .Include(c => c.Dealer)
            .Include(c => c.CoveredSparePart)
            .Include(c => c.ReviewedByUser)
            .Include(c => c.CustomerVehicle)
                .ThenInclude(v => v!.MotorcycleModel)
            .Include(c => c.CustomerVehicle)
                .ThenInclude(v => v!.PurchasedSalesOrder);

    private static string GenerateClaimNumber(DateTimeOffset utcNow, Guid claimId) =>
        VoucherAndDocumentNumberHelper.FormatDocumentNumber("WC", utcNow, claimId);

    private static WarrantyClaimDto MapLoadedClaimToDto(WarrantyClaim claim) =>
        MapToDto(
            claim,
            claim.Customer,
            claim.CustomerVehicle,
            claim.CustomerVehicle?.MotorcycleModel,
            claim.Dealer,
            claim.CoveredSparePart,
            claim.ReviewedByUser);

    private static WarrantyClaimDto MapToDto(
        WarrantyClaim claim,
        User? customer,
        CustomerVehicle? vehicle,
        MotorcycleModel? model,
        Dealer? dealer,
        SparePart? coveredPart,
        User? reviewedByUser)
    {
        var warrantyStartUtc = vehicle is not null
            ? WarrantyClaimValidationRules.ResolveWarrantyStartUtc(
                vehicle.OwnershipStartDateUtc,
                vehicle.CreatedAtUtc,
                vehicle.PurchasedSalesOrder?.CompletedAtUtc)
            : claim.CreatedAtUtc;
        var warrantyExpiresAtUtc = WarrantyClaimValidationRules.ComputeWarrantyExpiresAtUtc(warrantyStartUtc);

        return new WarrantyClaimDto(
            Id: claim.Id,
            ClaimId: claim.Id,
            ClaimNumber: claim.ClaimNumber,
            Status: claim.Status.ToString(),
            CustomerId: claim.CustomerId,
            CustomerFullName: customer?.FullName ?? string.Empty,
            CustomerEmail: customer?.Email ?? string.Empty,
            CustomerPhoneNumber: customer?.PhoneNumber ?? string.Empty,
            CustomerVehicleId: claim.CustomerVehicleId,
            Vin: vehicle?.VIN ?? claim.VinSnapshot,
            VinSnapshot: claim.VinSnapshot,
            LicensePlate: vehicle?.LicensePlate,
            MotorcycleModelId: vehicle?.MotorcycleModelId ?? Guid.Empty,
            MotorcycleModelCode: model?.ModelCode ?? string.Empty,
            MotorcycleModelName: model?.ModelName ?? string.Empty,
            WarrantyStartUtc: warrantyStartUtc,
            WarrantyExpiresAtUtc: warrantyExpiresAtUtc,
            DealerId: claim.DealerId,
            DealerCode: dealer?.Code ?? string.Empty,
            DealerName: dealer?.Name ?? string.Empty,
            DealerCity: dealer?.City ?? string.Empty,
            OdometerAtClaimKm: claim.OdometerAtClaimKm,
            DefectCategory: claim.DefectCategory.ToString(),
            SymptomDescription: claim.SymptomDescription,
            CoveredSparePartId: claim.CoveredSparePartId,
            CoveredSparePartCode: coveredPart?.PartCode,
            CoveredSparePartName: coveredPart?.Name,
            CoveredAmountVnd: claim.CoveredAmountVnd,
            ResolutionNotes: claim.ResolutionNotes,
            RejectionReason: claim.RejectionReason,
            ReviewedByUserId: claim.ReviewedByUserId,
            ReviewedByStaffName: reviewedByUser?.FullName,
            ReviewedAtUtc: claim.ReviewedAtUtc,
            CompletedAtUtc: claim.CompletedAtUtc,
            RowVersion: RowVersionCodec.Encode(claim.RowVersion),
            CreatedAtUtc: claim.CreatedAtUtc,
            UpdatedAtUtc: claim.UpdatedAtUtc);
    }
}
