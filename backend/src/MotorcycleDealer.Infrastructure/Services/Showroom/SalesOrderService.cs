using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.Services;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

public sealed class SalesOrderService : ISalesOrderService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<PaginationQuery> _paginationValidator;
    private readonly IValidator<CreateSalesOrderRequest> _createValidator;

    public SalesOrderService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<PaginationQuery> paginationValidator,
        IValidator<CreateSalesOrderRequest> createValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _paginationValidator = paginationValidator ?? throw new ArgumentNullException(nameof(paginationValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var actorUserId = _currentUserContext.RequireUserId();
        Guid? staffDealerId = null;

        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (request.DealerId is { } reqDealerId &&
                reqDealerId != Guid.Empty &&
                reqDealerId != staffDealerId.Value)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot complete sales orders for another dealership branch.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        SalesOrderValidationRules.TryNormalizePaymentMethod(request.PaymentMethod, out var paymentMethod);
        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.ResolveEffectiveRowVersion());
        var expectedVoucherRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.CustomerVoucherRowVersion);

        var normalizedLicensePlate = string.IsNullOrWhiteSpace(request.LicensePlate)
            ? null
            : request.LicensePlate.Trim().ToUpperInvariant();

        LoyaltyPointsUpdatedRealtimeEvent? postCommitPointsEvent = null;
        TierUpgradedRealtimeEvent? postCommitTierEvent = null;
        var postCommitCustomerId = Guid.Empty;

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        SalesOrderDto completedDto;
        try
        {
            completedDto = await strategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                postCommitPointsEvent = null;
                postCommitTierEvent = null;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                var utcNow = _dateTimeProvider.UtcNow;

                // a) Load DealerInventory and enforce DealerId scope + StockStatus + optional RowVersion
                var inventory = await _dbContext.DealerInventories
                    .IgnoreQueryFilters()
                    .Include(i => i.Dealer)
                    .Include(i => i.MotorcycleModel)
                    .FirstOrDefaultAsync(i => i.Id == request.DealerInventoryId, ct)
                    ?? throw NotFoundException.ForEntity("DealerInventory", request.DealerInventoryId);

                if (staffDealerId.HasValue && inventory.DealerId != staffDealerId.Value)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff cannot sell inventory belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                if (request.DealerId is { } explicitDealerId &&
                    explicitDealerId != Guid.Empty &&
                    inventory.DealerId != explicitDealerId)
                {
                    throw new BusinessRuleViolationException(
                        "DEALER_INVENTORY_MISMATCH",
                        "Selected inventory unit does not belong to the specified dealership.");
                }

                if (inventory.Dealer is null || inventory.Dealer.IsDeleted || !inventory.Dealer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "DEALER_INACTIVE",
                        "Cannot complete a showroom sale for an inactive dealership.");
                }

                if (inventory.MotorcycleModel is null || !inventory.MotorcycleModel.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "MODEL_INACTIVE",
                        "Cannot complete a showroom sale for an inactive motorcycle model.");
                }

                if (expectedRowVersion is not null)
                {
                    if (!inventory.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                    {
                        throw new ConflictException(
                            "The selected VIN inventory unit was modified concurrently. Please refresh stock and retry.",
                            "CONCURRENCY_CONFLICT");
                    }

                    _dbContext.Entry(inventory).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
                }

                if (!string.Equals(inventory.StockStatus, "Available", StringComparison.Ordinal))
                {
                    throw new ConflictException(
                        $"Inventory unit '{inventory.VIN}' is currently '{inventory.StockStatus}' and is not available for sale.",
                        "INVENTORY_NOT_AVAILABLE");
                }

                var vinExistsInGarage = await _dbContext.CustomerVehicles
                    .IgnoreQueryFilters()
                    .AnyAsync(v => v.VIN == inventory.VIN, ct);

                if (vinExistsInGarage)
                {
                    throw new ConflictException(
                        $"VIN '{inventory.VIN}' is already registered in a customer garage.",
                        "DUPLICATE_VIN");
                }

                if (normalizedLicensePlate is not null)
                {
                    var plateExists = await _dbContext.CustomerVehicles
                        .IgnoreQueryFilters()
                        .AnyAsync(v => !v.IsDeleted && v.LicensePlate == normalizedLicensePlate, ct);

                    if (plateExists)
                    {
                        throw new ConflictException(
                            $"License plate '{normalizedLicensePlate}' is already registered to another active vehicle.",
                            "DUPLICATE_LICENSE_PLATE");
                    }
                }

                // b) Verify target Customer exists, is active, and holds Customer role
                User? customer;
                if (request.CustomerId is { } reqCustId && reqCustId != Guid.Empty)
                {
                    customer = await _dbContext.Users
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Id == reqCustId, ct);
                }
                else
                {
                    var normalizedEmail = User.NormalizeEmailValue(request.CustomerEmail!);
                    customer = await _dbContext.Users
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);
                }

                if (customer is null)
                {
                    throw NotFoundException.ForEntity(
                        "Customer",
                        request.CustomerId is { } cid && cid != Guid.Empty ? cid : request.CustomerEmail ?? "unknown");
                }

                if (!customer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "CUSTOMER_INACTIVE",
                        "Cannot complete a showroom sale for an inactive customer account.");
                }

                var holdsCustomerRole = customer.UserRoles.Any(ur =>
                    ur.Role != null && string.Equals(ur.Role.Name, RoleNames.Customer, StringComparison.Ordinal));

                if (!holdsCustomerRole)
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_CUSTOMER_ROLE",
                        "SalesOrders can only be issued to principals holding the Customer role.");
                }

                var vietnamToday = TestDriveValidationRules.GetVietnamToday(utcNow);
                var assignedToOtherCustomerTestDrive = await _dbContext.TestDriveBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        t => t.AssignedInventoryId == inventory.Id &&
                             t.PreferredDate >= vietnamToday &&
                             t.Status == TestDriveStatus.Confirmed &&
                             t.CustomerId != customer.Id,
                        ct);

                if (assignedToOtherCustomerTestDrive)
                {
                    throw new ConflictException(
                        $"Inventory unit '{inventory.VIN}' is currently assigned to an upcoming confirmed showroom test-drive for another customer and cannot be sold until that test-drive is completed or reassigned.",
                        "INVENTORY_ASSIGNED_TO_TEST_DRIVE");
                }

                postCommitCustomerId = customer.Id;

                // c & d) Load LoyaltyTiers + Customer LoyaltyAccount (creating Bronze if missing) & optional CustomerVoucher
                var tiers = await _dbContext.LoyaltyTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.RankOrder)
                    .ToListAsync(ct);

                var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                                 ?? tiers.FirstOrDefault()
                                 ?? throw new BusinessRuleViolationException(
                                     "SYSTEM_SEED_MISSING",
                                     "Canonical LoyaltyTiers seed data is missing.");

                var loyaltyAccount = await _dbContext.LoyaltyAccounts
                    .Include(la => la.CurrentTier)
                    .FirstOrDefaultAsync(la => la.CustomerId == customer.Id, ct);

                if (loyaltyAccount is null)
                {
                    loyaltyAccount = new LoyaltyAccount(Guid.CreateVersion7())
                    {
                        CustomerId = customer.Id,
                        CurrentTierId = bronzeTier.Id,
                        CurrentTier = bronzeTier,
                        AvailablePoints = 0,
                        LifetimeQualifyingPoints = 0,
                        LifetimeSpentVnd = 0m,
                        CreatedAtUtc = utcNow
                    };
                    _dbContext.LoyaltyAccounts.Add(loyaltyAccount);
                }

                var previousTier = loyaltyAccount.CurrentTier
                                   ?? tiers.FirstOrDefault(t => t.Id == loyaltyAccount.CurrentTierId)
                                   ?? bronzeTier;

                var totalAmount = inventory.ListedPrice;
                var discountAmount = request.DiscountAmount ?? 0m;
                CustomerVoucher? appliedVoucher = null;

                if ((request.AppliedCustomerVoucherId is { } voucherId && voucherId != Guid.Empty) ||
                    !string.IsNullOrWhiteSpace(request.VoucherCode))
                {
                    if (request.AppliedCustomerVoucherId is { } vid && vid != Guid.Empty)
                    {
                        appliedVoucher = await _dbContext.CustomerVouchers
                            .IgnoreQueryFilters()
                            .Include(cv => cv.VoucherCatalog)
                            .FirstOrDefaultAsync(cv => cv.Id == vid, ct);
                    }
                    else
                    {
                        var trimmedCode = request.VoucherCode!.Trim().ToUpperInvariant();
                        appliedVoucher = await _dbContext.CustomerVouchers
                            .IgnoreQueryFilters()
                            .Include(cv => cv.VoucherCatalog)
                            .FirstOrDefaultAsync(cv => cv.VoucherCode == trimmedCode, ct);
                    }

                    if (appliedVoucher is null)
                    {
                        throw NotFoundException.ForEntity(
                            "CustomerVoucher",
                            request.AppliedCustomerVoucherId is { } missingVid && missingVid != Guid.Empty
                                ? missingVid
                                : request.VoucherCode ?? "unknown");
                    }

                    if (appliedVoucher.LoyaltyAccountId != loyaltyAccount.Id)
                    {
                        throw new ForbiddenAccessException(
                            "The selected voucher belongs to another customer's loyalty account.",
                            "FORBIDDEN_RESOURCE_OWNER");
                    }

                    if (!string.Equals(appliedVoucher.Status, "Available", StringComparison.Ordinal))
                    {
                        throw new ConflictException(
                            $"Voucher '{appliedVoucher.VoucherCode}' is currently '{appliedVoucher.Status}' and cannot be applied.",
                            "VOUCHER_NOT_AVAILABLE");
                    }

                    if (expectedVoucherRowVersion is not null)
                    {
                        if (!appliedVoucher.RowVersion.AsSpan().SequenceEqual(expectedVoucherRowVersion))
                        {
                            throw new ConflictException(
                                "The selected customer voucher was modified concurrently. Please refresh and retry.",
                                "CONCURRENCY_CONFLICT");
                        }

                        _dbContext.Entry(appliedVoucher).Property(x => x.RowVersion).OriginalValue = expectedVoucherRowVersion;
                    }

                    if (appliedVoucher.ExpiresAtUtc <= utcNow)
                    {
                        throw new BusinessRuleViolationException(
                            "VOUCHER_INELIGIBLE",
                            $"Voucher '{appliedVoucher.VoucherCode}' expired at {appliedVoucher.ExpiresAtUtc:O}.");
                    }

                    var catalog = appliedVoucher.VoucherCatalog
                                  ?? throw new BusinessRuleViolationException(
                                      "VOUCHER_INELIGIBLE",
                                      "Voucher catalog metadata could not be loaded.");

                    if (!string.Equals(catalog.ApplicableScope, "ShowroomOnly", StringComparison.Ordinal) &&
                        !string.Equals(catalog.ApplicableScope, "All", StringComparison.Ordinal))
                    {
                        throw new BusinessRuleViolationException(
                            "VOUCHER_INELIGIBLE",
                            $"Voucher '{appliedVoucher.VoucherCode}' has scope '{catalog.ApplicableScope}' and cannot be applied to showroom purchases.");
                    }

                    if (totalAmount < catalog.MinOrderAmount)
                    {
                        throw new BusinessRuleViolationException(
                            "VOUCHER_INELIGIBLE",
                            $"Order total ({totalAmount:N0} VND) is below voucher minimum requirement ({catalog.MinOrderAmount:N0} VND).");
                    }

                    var voucherDiscount = ComputeVoucherDiscount(totalAmount, catalog);
                    discountAmount += voucherDiscount;

                    appliedVoucher.Status = "Used";
                    appliedVoucher.UsedAtUtc = utcNow;
                    appliedVoucher.UpdatedAtUtc = utcNow;
                }

                if (discountAmount > totalAmount)
                {
                    throw new BusinessRuleViolationException(
                        "DISCOUNT_EXCEEDS_TOTAL",
                        $"Total discount ({discountAmount:N0} VND) cannot exceed listed price ({totalAmount:N0} VND).");
                }

                var finalAmount = totalAmount - discountAmount;
                var appliedMultiplier = previousTier.PointsMultiplier;
                var pointsEarned = LoyaltyPointsCalculator.CalculatePointsEarned(finalAmount, appliedMultiplier);

                // e) Transition DealerInventory.StockStatus = "Sold"
                inventory.MarkSold(utcNow);

                // f) Insert immutable SalesOrder
                var salesOrderId = Guid.CreateVersion7();
                var orderNumber = GenerateOrderNumber(utcNow, salesOrderId);
                var salesOrder = new SalesOrder(salesOrderId)
                {
                    OrderNumber = orderNumber,
                    DealerId = inventory.DealerId,
                    CustomerId = customer.Id,
                    DealerInventoryId = inventory.Id,
                    AppliedCustomerVoucherId = appliedVoucher?.Id,
                    SoldByStaffId = actorUserId,
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    PointsEarned = pointsEarned,
                    PaymentMethod = paymentMethod,
                    Status = "Completed",
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CompletedAtUtc = utcNow,
                    CreatedAtUtc = utcNow
                };
                _dbContext.SalesOrders.Add(salesOrder);

                // g) Insert CustomerVehicle bound to model's first MaintenanceRule (fallback 1000 km / 1 month)
                var firstRule = await _dbContext.MaintenanceRules
                    .AsNoTracking()
                    .Where(r => r.MotorcycleModelId == inventory.MotorcycleModelId && r.IsActive)
                    .OrderBy(r => r.IntervalKm)
                    .ThenBy(r => r.IntervalMonths)
                    .FirstOrDefaultAsync(ct);

                var initialIntervalKm = firstRule?.IntervalKm ?? 1_000;
                var initialIntervalMonths = firstRule?.IntervalMonths ?? 1;

                var customerVehicle = new CustomerVehicle(Guid.CreateVersion7())
                {
                    VIN = inventory.VIN,
                    CustomerId = customer.Id,
                    MotorcycleModelId = inventory.MotorcycleModelId,
                    PurchasedSalesOrderId = salesOrder.Id,
                    LicensePlate = normalizedLicensePlate,
                    ColorName = inventory.ColorName,
                    RegistrationSource = "ShowroomSale",
                    OwnershipStartDateUtc = utcNow,
                    CurrentMileageKm = 0,
                    LastServiceMileageKm = 0,
                    LastServiceDateUtc = null,
                    NextDueMileageKm = initialIntervalKm,
                    NextDueDateUtc = utcNow.AddMonths(initialIntervalMonths),
                    IsActive = true,
                    CreatedAtUtc = utcNow
                };
                _dbContext.CustomerVehicles.Add(customerVehicle);

                // h) Update LoyaltyAccount + evaluate Tier upgrade + append PointLedgerEntry + Notifications
                loyaltyAccount.LifetimeSpentVnd += finalAmount;
                loyaltyAccount.UpdatedAtUtc = utcNow;

                var resultingTier = previousTier;
                var tierUpgraded = false;

                if (pointsEarned > 0)
                {
                    loyaltyAccount.AvailablePoints = checked(loyaltyAccount.AvailablePoints + pointsEarned);
                    loyaltyAccount.LifetimeQualifyingPoints = checked(loyaltyAccount.LifetimeQualifyingPoints + pointsEarned);

                    var evaluatedTierCode = LoyaltyPointsCalculator.EvaluateTier(loyaltyAccount.LifetimeQualifyingPoints);
                    var candidateTier = tiers.FirstOrDefault(t =>
                        string.Equals(t.TierCode, evaluatedTierCode.ToString(), StringComparison.Ordinal))
                        ?? tiers
                            .Where(t => t.MinQualifyingPoints <= loyaltyAccount.LifetimeQualifyingPoints)
                            .OrderByDescending(t => t.MinQualifyingPoints)
                            .FirstOrDefault();

                    if (candidateTier is not null && candidateTier.RankOrder > previousTier.RankOrder)
                    {
                        resultingTier = candidateTier;
                        loyaltyAccount.CurrentTierId = resultingTier.Id;
                        loyaltyAccount.TierUpgradedAtUtc = utcNow;
                        tierUpgraded = true;
                    }

                    var ledgerEntry = new PointLedgerEntry(Guid.CreateVersion7())
                    {
                        LoyaltyAccountId = loyaltyAccount.Id,
                        TransactionType = "SalesOrderEarn",
                        PointsDelta = pointsEarned,
                        BalanceAfter = loyaltyAccount.AvailablePoints,
                        ReferenceType = "SalesOrder",
                        ReferenceId = salesOrder.Id,
                        IdempotencyKey = $"SALES_ORDER:{salesOrder.Id}:EARN",
                        Notes = $"Earned {pointsEarned} points on showroom order {salesOrder.OrderNumber} ({inventory.VIN}).",
                        CreatedByUserId = actorUserId,
                        CreatedAtUtc = utcNow
                    };
                    _dbContext.PointLedgerEntries.Add(ledgerEntry);

                    var pointsNotifId = Guid.CreateVersion7();
                    var pointsEventPayload = new LoyaltyPointsUpdatedRealtimeEvent(
                        NotificationId: pointsNotifId,
                        EventType: "LoyaltyPointsUpdated",
                        LoyaltyAccountId: loyaltyAccount.Id,
                        TransactionType: "SalesOrderEarn",
                        PointsDelta: pointsEarned,
                        AvailablePointsAfter: loyaltyAccount.AvailablePoints,
                        LifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints,
                        ReferenceType: "SalesOrder",
                        ReferenceNumber: salesOrder.OrderNumber,
                        OccurredAtUtc: utcNow);

                    _dbContext.Notifications.Add(new Notification(pointsNotifId)
                    {
                        RecipientUserId = customer.Id,
                        EventType = "LoyaltyPointsUpdated",
                        Title = $"+{pointsEarned:N0} Loyalty Points Accrued",
                        Message = $"Your showroom purchase {salesOrder.OrderNumber} ({inventory.MotorcycleModel.ModelName}) earned {pointsEarned:N0} loyalty points.",
                        PayloadJson = JsonSerializer.Serialize(pointsEventPayload),
                        IsRead = false,
                        CreatedAtUtc = utcNow
                    });

                    postCommitPointsEvent = pointsEventPayload;

                    if (tierUpgraded)
                    {
                        var tierNotifId = Guid.CreateVersion7();
                        var tierEventPayload = new TierUpgradedRealtimeEvent(
                            NotificationId: tierNotifId,
                            EventType: "TierUpgraded",
                            LoyaltyAccountId: loyaltyAccount.Id,
                            PreviousTierCode: previousTier.TierCode,
                            NewTierCode: resultingTier.TierCode,
                            NewPointsMultiplier: resultingTier.PointsMultiplier,
                            LifetimeQualifyingPoints: loyaltyAccount.LifetimeQualifyingPoints,
                            OccurredAtUtc: utcNow);

                        _dbContext.Notifications.Add(new Notification(tierNotifId)
                        {
                            RecipientUserId = customer.Id,
                            EventType = "TierUpgraded",
                            Title = $"Congratulations! Upgraded to {resultingTier.DisplayName}",
                            Message = $"You unlocked {resultingTier.TierCode} tier ({resultingTier.PointsMultiplier:0.00}x points multiplier) with {loyaltyAccount.LifetimeQualifyingPoints:N0} lifetime qualifying points.",
                            PayloadJson = JsonSerializer.Serialize(tierEventPayload),
                            IsRead = false,
                            CreatedAtUtc = utcNow
                        });

                        postCommitTierEvent = tierEventPayload;
                    }
                }

                _dbContext.Notifications.Add(new Notification(Guid.CreateVersion7())
                {
                    RecipientUserId = customer.Id,
                    EventType = "SalesOrderCompleted",
                    Title = $"Showroom Purchase Completed — {inventory.MotorcycleModel.ModelName}",
                    Message = $"VIN {inventory.VIN} has been added to your digital garage under order {salesOrder.OrderNumber}.",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        salesOrderId = salesOrder.Id,
                        orderNumber = salesOrder.OrderNumber,
                        vin = inventory.VIN,
                        customerVehicleId = customerVehicle.Id,
                        finalAmount,
                        pointsEarned
                    }),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new SalesOrderDto(
                    Id: salesOrder.Id,
                    SalesOrderId: salesOrder.Id,
                    OrderNumber: salesOrder.OrderNumber,
                    DealerId: inventory.DealerId,
                    DealerCode: inventory.Dealer.Code,
                    DealerName: inventory.Dealer.Name,
                    CustomerId: customer.Id,
                    CustomerFullName: customer.FullName,
                    CustomerEmail: customer.Email,
                    DealerInventoryId: inventory.Id,
                    Vin: inventory.VIN,
                    MotorcycleModelId: inventory.MotorcycleModelId,
                    MotorcycleModelName: inventory.MotorcycleModel.ModelName,
                    ColorName: inventory.ColorName,
                    AppliedCustomerVoucherId: salesOrder.AppliedCustomerVoucherId,
                    TotalAmount: salesOrder.TotalAmount,
                    DiscountAmount: salesOrder.DiscountAmount,
                    FinalAmount: salesOrder.FinalAmount,
                    PaymentMethod: salesOrder.PaymentMethod,
                    AppliedTierCode: previousTier.TierCode,
                    AppliedMultiplier: appliedMultiplier,
                    PointsEarned: salesOrder.PointsEarned,
                    NewTierCode: resultingTier.TierCode,
                    TierUpgraded: tierUpgraded,
                    AvailablePointsAfter: loyaltyAccount.AvailablePoints,
                    LifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints,
                    OnboardedCustomerVehicleId: customerVehicle.Id,
                    LicensePlate: customerVehicle.LicensePlate,
                    Status: salesOrder.Status,
                    Notes: salesOrder.Notes,
                    CompletedAtUtc: salesOrder.CompletedAtUtc,
                    CreatedAtUtc: salesOrder.CreatedAtUtc);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Showroom inventory or customer loyalty balance was modified concurrently by another transaction. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_SalesOrders_ActiveInventory", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "The selected inventory unit was just sold in another concurrent transaction.",
                    "INVENTORY_NOT_AVAILABLE");
            }

            if (sqlMsg.Contains("UQ_CustomerVehicles_LicensePlate", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "The supplied license plate is already registered to another vehicle.",
                    "DUPLICATE_LICENSE_PLATE");
            }

            if (sqlMsg.Contains("UQ_CustomerVehicles_VIN", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "This VIN is already registered in a customer garage.",
                    "DUPLICATE_VIN");
            }

            throw new ConflictException(
                "A concurrent showroom order conflict occurred. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }

        // i) Post-commit SignalR dispatch ONLY after tx.CommitAsync succeeded
        if (postCommitPointsEvent is not null)
        {
            await _notificationPublisher.PublishLoyaltyPointsUpdatedAsync(
                postCommitPointsEvent,
                postCommitCustomerId,
                cancellationToken);
        }

        if (postCommitTierEvent is not null)
        {
            await _notificationPublisher.PublishTierUpgradedAsync(
                postCommitTierEvent,
                postCommitCustomerId,
                cancellationToken);
        }

        return completedDto;
    }

    public async Task<PagedResult<SalesOrderDto>> ListSalesOrdersAsync(
        SalesOrderListQuery query,
        bool forceOwnCustomerOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _paginationValidator.ValidateAndThrowAsync(query, cancellationToken);

        var canViewDealer = !forceOwnCustomerOnly && _currentUserContext.HasPermission(Permissions.SalesOrders.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.SalesOrders.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view showroom sales orders.",
                "FORBIDDEN_PERMISSION");
        }

        IQueryable<SalesOrder> ordersQuery;

        if (!canViewDealer)
        {
            var customerId = _currentUserContext.RequireUserId();
            ordersQuery = _dbContext.SalesOrders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId);
        }
        else
        {
            // Global IDealerScoped filter is active on _dbContext.SalesOrders for DealerStaff.
            ordersQuery = _dbContext.SalesOrders.AsNoTracking();

            if (_currentUserContext.IsDealerStaff)
            {
                var staffDealerId = _currentUserContext.RequireAssignedDealerId();
                ordersQuery = ordersQuery.Where(o => o.DealerId == staffDealerId);
            }
            else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
            {
                ordersQuery = ordersQuery.Where(o => o.DealerId == adminDealerId);
            }

            if (query.CustomerId is { } filterCustomerId && filterCustomerId != Guid.Empty)
            {
                ordersQuery = ordersQuery.Where(o => o.CustomerId == filterCustomerId);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            ordersQuery = ordersQuery.Where(o =>
                EF.Functions.Like(o.OrderNumber, pattern) ||
                (o.DealerInventory != null && EF.Functions.Like(o.DealerInventory.VIN, pattern)) ||
                (o.Customer != null && (
                    EF.Functions.Like(o.Customer.FullName, pattern) ||
                    EF.Functions.Like(o.Customer.Email, pattern))));
        }

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<SalesOrderDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var orders = await ordersQuery
            .Include(o => o.Dealer)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.LoyaltyAccount)
                    .ThenInclude(la => la!.CurrentTier)
            .Include(o => o.DealerInventory)
                .ThenInclude(inv => inv!.MotorcycleModel)
            .Include(o => o.OnboardedCustomerVehicle)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var dtos = await HydrateExistingOrderDtosAsync(orders, cancellationToken);

        return PagedResult<SalesOrderDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<SalesOrderDto> GetSalesOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var canViewDealer = _currentUserContext.HasPermission(Permissions.SalesOrders.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.SalesOrders.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view showroom sales orders.",
                "FORBIDDEN_PERMISSION");
        }

        var order = await _dbContext.SalesOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(o => o.Dealer)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.LoyaltyAccount)
                    .ThenInclude(la => la!.CurrentTier)
            .Include(o => o.DealerInventory)
                .ThenInclude(inv => inv!.MotorcycleModel)
            .Include(o => o.OnboardedCustomerVehicle)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw NotFoundException.ForEntity("SalesOrder", orderId);

        if (!canViewDealer)
        {
            var customerId = _currentUserContext.RequireUserId();
            if (order.CustomerId != customerId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view their own showroom sales orders.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (order.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view sales orders belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        var hydrated = await HydrateExistingOrderDtosAsync([order], cancellationToken);
        return hydrated[0];
    }

    public async Task<PrintableSalesOrderReceiptDto> GetPrintableReceiptAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var canViewDealer = _currentUserContext.HasPermission(Permissions.SalesOrders.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.SalesOrders.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view showroom sales order receipts.",
                "FORBIDDEN_PERMISSION");
        }

        var order = await _dbContext.SalesOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(o => o.Dealer)
            .Include(o => o.SoldByStaff)
            .Include(o => o.AppliedCustomerVoucher)
            .Include(o => o.Customer)
                .ThenInclude(c => c!.LoyaltyAccount)
                    .ThenInclude(la => la!.CurrentTier)
            .Include(o => o.DealerInventory)
                .ThenInclude(inv => inv!.MotorcycleModel)
            .Include(o => o.OnboardedCustomerVehicle)
                .ThenInclude(v => v!.MotorcycleModel)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw NotFoundException.ForEntity("SalesOrder", orderId);

        if (!canViewDealer)
        {
            var customerId = _currentUserContext.RequireUserId();
            if (order.CustomerId != customerId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view receipts for their own showroom sales orders.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (order.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view showroom sales order receipts belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        var dto = (await HydrateExistingOrderDtosAsync([order], cancellationToken))[0];
        var dealer = order.Dealer;
        var customer = order.Customer;
        var inventory = order.DealerInventory;
        var onboardedVehicle = order.OnboardedCustomerVehicle;
        var model = inventory?.MotorcycleModel ?? onboardedVehicle?.MotorcycleModel;

        var dealerPhone = dealer?.PhoneNumber ?? string.Empty;
        var customerPhone = customer?.PhoneNumber ?? string.Empty;

        return new PrintableSalesOrderReceiptDto(
            ReceiptNumber: $"RCP-SLS-{order.OrderNumber}",
            ReceiptType: "SalesOrder",
            SalesOrderId: order.Id,
            OrderNumber: order.OrderNumber,
            PaymentMethod: order.PaymentMethod,
            Status: order.Status,
            IssuedAtUtc: order.CompletedAtUtc ?? order.CreatedAtUtc,
            SoldByStaffName: order.SoldByStaff?.FullName,
            VerificationCode: ReceiptVerificationHash.ComputeForSalesOrder(
                order.Id,
                order.OrderNumber,
                order.FinalAmount),
            Dealer: new ReceiptDealerInfoDto(
                DealerId: order.DealerId,
                Code: dealer?.Code ?? string.Empty,
                Name: dealer?.Name ?? string.Empty,
                Address: dealer?.Address ?? string.Empty,
                City: dealer?.City ?? string.Empty,
                Phone: dealerPhone,
                PhoneNumber: dealerPhone),
            Customer: new ReceiptCustomerInfoDto(
                CustomerId: order.CustomerId,
                FullName: customer?.FullName ?? string.Empty,
                Email: customer?.Email ?? string.Empty,
                Phone: customerPhone,
                PhoneNumber: customerPhone),
            Vehicle: new SalesOrderReceiptVehicleInfoDto(
                CustomerVehicleId: onboardedVehicle?.Id,
                ModelCode: model?.ModelCode ?? string.Empty,
                ModelName: model?.ModelName ?? dto.MotorcycleModelName,
                ColorName: dto.ColorName,
                Vin: dto.Vin,
                EngineNumber: inventory?.EngineNumber ?? string.Empty,
                LicensePlate: dto.LicensePlate,
                InitialNextDueMileageKm: onboardedVehicle?.NextDueMileageKm,
                InitialNextDueDateUtc: onboardedVehicle?.NextDueDateUtc),
            UnitPrice: order.TotalAmount,
            TotalAmount: order.TotalAmount,
            AppliedVoucherCode: order.AppliedCustomerVoucher?.VoucherCode,
            DiscountAmount: order.DiscountAmount,
            FinalAmount: order.FinalAmount,
            Notes: order.Notes,
            Loyalty: new ReceiptLoyaltySummaryDto(
                PointsEarned: order.PointsEarned,
                AppliedTierCode: dto.AppliedTierCode,
                TierCodeAfter: dto.NewTierCode,
                NewTierCode: dto.NewTierCode,
                PointsMultiplierApplied: dto.AppliedMultiplier,
                TierMultiplier: dto.AppliedMultiplier,
                TierUpgraded: dto.TierUpgraded,
                AvailablePointsAfter: dto.AvailablePointsAfter,
                LifetimeQualifyingPointsAfter: dto.LifetimeQualifyingPointsAfter));
    }

    private static decimal ComputeVoucherDiscount(decimal totalAmount, VoucherCatalog catalog) =>
        VoucherAndDocumentNumberHelper.ComputeVoucherDiscountAmount(totalAmount, catalog);

    private static string GenerateOrderNumber(DateTimeOffset utcNow, Guid orderId) =>
        VoucherAndDocumentNumberHelper.FormatDocumentNumber("SO", utcNow, orderId);

    private readonly record struct OrderLedgerSnapshot(
        Guid Id,
        Guid LoyaltyAccountId,
        Guid ReferenceId,
        int PointsDelta,
        int BalanceAfter,
        DateTimeOffset CreatedAtUtc);

    private readonly record struct PositiveAccrualSnapshot(
        Guid Id,
        Guid LoyaltyAccountId,
        int PointsDelta,
        DateTimeOffset CreatedAtUtc);

    private async Task<IReadOnlyList<SalesOrderDto>> HydrateExistingOrderDtosAsync(
        IReadOnlyList<SalesOrder> orders,
        CancellationToken cancellationToken)
    {
        if (orders.Count == 0)
        {
            return [];
        }

        var orderIds = orders.Select(o => o.Id).ToArray();

        var tiers = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.RankOrder)
            .ToListAsync(cancellationToken);

        var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                         ?? new LoyaltyTier
                         {
                             TierCode = "Bronze",
                             RankOrder = 1,
                             MinQualifyingPoints = 0,
                             PointsMultiplier = 1.00m
                         };

        var orderLedgers = await _dbContext.PointLedgerEntries
            .AsNoTracking()
            .Where(p => p.ReferenceType == "SalesOrder" && orderIds.Contains(p.ReferenceId))
            .Select(p => new OrderLedgerSnapshot(
                p.Id,
                p.LoyaltyAccountId,
                p.ReferenceId,
                p.PointsDelta,
                p.BalanceAfter,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var ledgerByOrderId = orderLedgers
            .GroupBy(p => p.ReferenceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAtUtc).First());

        Dictionary<Guid, List<PositiveAccrualSnapshot>> positiveAccrualsByAccount;
        if (orderLedgers.Count == 0)
        {
            positiveAccrualsByAccount = [];
        }
        else
        {
            var matchedAccountIds = orderLedgers
                .Select(p => p.LoyaltyAccountId)
                .Distinct()
                .ToArray();

            var positiveRows = await _dbContext.PointLedgerEntries
                .AsNoTracking()
                .Where(p => matchedAccountIds.Contains(p.LoyaltyAccountId) && p.PointsDelta > 0)
                .OrderBy(p => p.CreatedAtUtc)
                .ThenBy(p => p.Id)
                .Select(p => new PositiveAccrualSnapshot(
                    p.Id,
                    p.LoyaltyAccountId,
                    p.PointsDelta,
                    p.CreatedAtUtc))
                .ToListAsync(cancellationToken);

            positiveAccrualsByAccount = positiveRows
                .GroupBy(p => p.LoyaltyAccountId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        return orders
            .Select(order => MapExistingOrderToDto(
                order,
                tiers,
                bronzeTier,
                ledgerByOrderId.TryGetValue(order.Id, out var ledgerSnapshot)
                    ? ledgerSnapshot
                    : (OrderLedgerSnapshot?)null,
                order.Customer?.LoyaltyAccount is { } acc
                    ? positiveAccrualsByAccount.GetValueOrDefault(acc.Id)
                    : null))
            .ToList();
    }

    private static SalesOrderDto MapExistingOrderToDto(
        SalesOrder order,
        IReadOnlyList<LoyaltyTier> activeTiers,
        LoyaltyTier bronzeTier,
        OrderLedgerSnapshot? orderLedger,
        IReadOnlyList<PositiveAccrualSnapshot>? accountPositiveAccruals)
    {
        var account = order.Customer?.LoyaltyAccount;
        var currentTier = account?.CurrentTier
                          ?? activeTiers.FirstOrDefault(t => account is not null && t.Id == account.CurrentTierId)
                          ?? bronzeTier;

        var availablePointsAfter = orderLedger?.BalanceAfter ?? (account?.AvailablePoints ?? 0);

        int lifetimeQualifyingAfter;
        if (orderLedger is { } ledger && accountPositiveAccruals is { Count: > 0 })
        {
            lifetimeQualifyingAfter = accountPositiveAccruals
                .Where(p =>
                    p.CreatedAtUtc < ledger.CreatedAtUtc ||
                    (p.CreatedAtUtc == ledger.CreatedAtUtc && p.Id == ledger.Id))
                .Sum(p => p.PointsDelta);

            if (lifetimeQualifyingAfter < order.PointsEarned)
            {
                lifetimeQualifyingAfter = order.PointsEarned;
            }
        }
        else
        {
            lifetimeQualifyingAfter = account?.LifetimeQualifyingPoints ?? order.PointsEarned;
        }

        LoyaltyTier appliedTier;
        if (order.FinalAmount >= LoyaltyPointsCalculator.BaseUnitVnd && activeTiers.Count > 0)
        {
            var matchedFormulaTier = activeTiers.FirstOrDefault(t =>
                LoyaltyPointsCalculator.CalculatePointsEarned(order.FinalAmount, t.PointsMultiplier) == order.PointsEarned);

            if (matchedFormulaTier is not null)
            {
                appliedTier = matchedFormulaTier;
            }
            else
            {
                var qualifyingBefore = Math.Max(0, lifetimeQualifyingAfter - order.PointsEarned);
                var inferredBeforeCode = LoyaltyPointsCalculator.EvaluateTier(qualifyingBefore).ToString();
                appliedTier = activeTiers.FirstOrDefault(t =>
                    string.Equals(t.TierCode, inferredBeforeCode, StringComparison.Ordinal)) ?? currentTier;
            }
        }
        else
        {
            appliedTier = currentTier;
        }

        LoyaltyTier resultingTier;
        if (orderLedger is not null && activeTiers.Count > 0)
        {
            var evaluatedAfterCode = LoyaltyPointsCalculator.EvaluateTier(lifetimeQualifyingAfter).ToString();
            var evaluatedAfterTier = activeTiers.FirstOrDefault(t =>
                string.Equals(t.TierCode, evaluatedAfterCode, StringComparison.Ordinal)) ?? currentTier;

            resultingTier = evaluatedAfterTier.RankOrder >= appliedTier.RankOrder
                ? evaluatedAfterTier
                : appliedTier;
        }
        else
        {
            resultingTier = currentTier.RankOrder >= appliedTier.RankOrder
                ? currentTier
                : appliedTier;
        }

        var tierUpgraded = resultingTier.RankOrder > appliedTier.RankOrder;

        return new SalesOrderDto(
            Id: order.Id,
            SalesOrderId: order.Id,
            OrderNumber: order.OrderNumber,
            DealerId: order.DealerId,
            DealerCode: order.Dealer?.Code ?? string.Empty,
            DealerName: order.Dealer?.Name ?? string.Empty,
            CustomerId: order.CustomerId,
            CustomerFullName: order.Customer?.FullName ?? string.Empty,
            CustomerEmail: order.Customer?.Email ?? string.Empty,
            DealerInventoryId: order.DealerInventoryId,
            Vin: order.DealerInventory?.VIN ?? order.OnboardedCustomerVehicle?.VIN ?? string.Empty,
            MotorcycleModelId: order.DealerInventory?.MotorcycleModelId ?? order.OnboardedCustomerVehicle?.MotorcycleModelId ?? Guid.Empty,
            MotorcycleModelName: order.DealerInventory?.MotorcycleModel?.ModelName ?? string.Empty,
            ColorName: order.DealerInventory?.ColorName ?? order.OnboardedCustomerVehicle?.ColorName ?? string.Empty,
            AppliedCustomerVoucherId: order.AppliedCustomerVoucherId,
            TotalAmount: order.TotalAmount,
            DiscountAmount: order.DiscountAmount,
            FinalAmount: order.FinalAmount,
            PaymentMethod: order.PaymentMethod,
            AppliedTierCode: appliedTier.TierCode,
            AppliedMultiplier: appliedTier.PointsMultiplier,
            PointsEarned: order.PointsEarned,
            NewTierCode: resultingTier.TierCode,
            TierUpgraded: tierUpgraded,
            AvailablePointsAfter: availablePointsAfter,
            LifetimeQualifyingPointsAfter: lifetimeQualifyingAfter,
            OnboardedCustomerVehicleId: order.OnboardedCustomerVehicle?.Id,
            LicensePlate: order.OnboardedCustomerVehicle?.LicensePlate,
            Status: order.Status,
            Notes: order.Notes,
            CompletedAtUtc: order.CompletedAtUtc,
            CreatedAtUtc: order.CreatedAtUtc);
    }
}

public static class VoucherAndDocumentNumberHelper
{
    public static decimal ComputeVoucherDiscountAmount(decimal eligibleAmount, VoucherCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (string.Equals(catalog.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            var rawDiscount = Math.Round(eligibleAmount * (catalog.DiscountValue / 100m), 2, MidpointRounding.AwayFromZero);
            var capped = catalog.MaxDiscountAmount.HasValue
                ? Math.Min(rawDiscount, catalog.MaxDiscountAmount.Value)
                : rawDiscount;
            return Math.Min(capped, eligibleAmount);
        }

        return Math.Min(catalog.DiscountValue, eligibleAmount);
    }

    public static string FormatDocumentNumber(string prefix, DateTimeOffset utcNow, Guid entityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        var shortHex = entityId.ToString("N")[^6..].ToUpperInvariant();
        return $"{prefix.Trim().ToUpperInvariant()}-{utcNow:yyyyMMdd}-{shortHex}";
    }
}
