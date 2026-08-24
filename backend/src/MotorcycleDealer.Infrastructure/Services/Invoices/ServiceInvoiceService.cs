using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.Services;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Services.Showroom;

namespace MotorcycleDealer.Infrastructure.Services.Invoices;

public sealed class ServiceInvoiceService : IServiceInvoiceService
{
    private const string BronzeTierCode = nameof(LoyaltyTierCode.Bronze);
    private const string ServiceInvoiceReferenceType = "ServiceInvoice";
    private const string SalesOrderEarnTransactionType = nameof(LedgerTransactionType.SalesOrderEarn);
    private const string ServiceInvoiceEarnTransactionType = nameof(LedgerTransactionType.ServiceInvoiceEarn);
    private const int DefaultFallbackIntervalKm = 4_000;
    private const int DefaultFallbackIntervalMonths = 6;

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<PaginationQuery> _paginationValidator;
    private readonly IValidator<CreateServiceInvoiceRequest> _createValidator;

    public ServiceInvoiceService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<PaginationQuery> paginationValidator,
        IValidator<CreateServiceInvoiceRequest> createValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _paginationValidator = paginationValidator ?? throw new ArgumentNullException(nameof(paginationValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    public async Task<ServiceInvoiceDto> CreateServiceInvoiceAsync(
        CreateServiceInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var staffUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Invoices.CreateDealer))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to issue workshop service invoices.",
                "FORBIDDEN_PERMISSION");
        }

        Guid? staffDealerId = null;
        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
        }

        var targetBookingId = request.ResolveEffectiveBookingId();
        ServiceInvoiceValidationRules.TryNormalizePaymentMethod(request.PaymentMethod, out var paymentMethod);
        var expectedBookingRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.ResolveEffectiveBookingRowVersion());
        var expectedVoucherRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.CustomerVoucherRowVersion);

        var normalizedWorkSummaryNotes = string.IsNullOrWhiteSpace(request.WorkSummaryNotes)
            ? null
            : request.WorkSummaryNotes.Trim();
        var normalizedLineItemsJson = string.IsNullOrWhiteSpace(request.LineItemsJson)
            ? null
            : request.LineItemsJson.Trim();

        BookingStatusChangedRealtimeEvent? postCommitBookingStatusEvent = null;
        LoyaltyPointsUpdatedRealtimeEvent? postCommitPointsEvent = null;
        TierUpgradedRealtimeEvent? postCommitTierEvent = null;
        var postCommitCustomerId = Guid.Empty;
        var postCommitDealerId = Guid.Empty;

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        ServiceInvoiceDto settledDto;
        try
        {
            settledDto = await strategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                postCommitBookingStatusEvent = null;
                postCommitPointsEvent = null;
                postCommitTierEvent = null;
                postCommitCustomerId = Guid.Empty;
                postCommitDealerId = Guid.Empty;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                var utcNow = _dateTimeProvider.UtcNow;

                // a) Load ServiceBooking and enforce DealerId scope + active Dealer invariant
                var booking = await _dbContext.ServiceBookings
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(b => b.Id == targetBookingId, ct)
                    ?? throw NotFoundException.ForEntity("ServiceBooking", targetBookingId);

                if (staffDealerId.HasValue && booking.DealerId != staffDealerId.Value)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff cannot issue workshop service invoices for bookings belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                var dealer = await _dbContext.Dealers
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == booking.DealerId, ct);

                if (dealer is null || dealer.IsDeleted || !dealer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "DEALER_INACTIVE",
                        "Cannot issue a workshop service invoice for an inactive dealership.");
                }

                // b) Verify 1:1 booking invoice uniqueness, settleable booking status, and optional BookingRowVersion
                var invoiceAlreadyExists = await _dbContext.ServiceInvoices
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(i => i.ServiceBookingId == booking.Id, ct);

                if (invoiceAlreadyExists)
                {
                    throw new ConflictException(
                        $"A workshop service invoice has already been issued for booking '{booking.BookingNumber}'.",
                        "INVOICE_ALREADY_ISSUED");
                }

                if (booking.Status is not ("Confirmed" or "CheckedIn" or "InProgress"))
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_BOOKING_STATUS",
                        $"Cannot issue a workshop service invoice while booking '{booking.BookingNumber}' is in '{booking.Status}' status. Allowed statuses: Confirmed, CheckedIn, InProgress.");
                }

                if (expectedBookingRowVersion is not null)
                {
                    if (!booking.RowVersion.AsSpan().SequenceEqual(expectedBookingRowVersion))
                    {
                        throw new ConflictException(
                            "The workshop booking was modified concurrently by another transaction. Please refresh and retry.",
                            "CONCURRENCY_CONFLICT");
                    }

                    _dbContext.Entry(booking).Property(b => b.RowVersion).OriginalValue = expectedBookingRowVersion;
                }

                // c) Load Customer, IssuedByStaff, and CustomerVehicle + enforce monotonic odometer rule
                var customer = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == booking.CustomerId, ct)
                    ?? throw NotFoundException.ForEntity("Customer", booking.CustomerId);

                if (!customer.IsActive || customer.IsDeleted)
                {
                    throw new BusinessRuleViolationException(
                        "CUSTOMER_INACTIVE",
                        "Cannot issue a workshop service invoice for an inactive customer account.");
                }

                var issuedByStaff = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == staffUserId, ct);

                var vehicle = await _dbContext.CustomerVehicles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.Id == booking.CustomerVehicleId && !v.IsDeleted, ct)
                    ?? throw NotFoundException.ForEntity("CustomerVehicle", booking.CustomerVehicleId);

                var motorcycleModel = await _dbContext.MotorcycleModels
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == vehicle.MotorcycleModelId, ct);

                var minAllowedOdometerKm = Math.Max(vehicle.CurrentMileageKm, booking.OdometerAtBookingKm);
                if (request.OdometerAtCompletionKm < minAllowedOdometerKm)
                {
                    throw new BusinessRuleViolationException(
                        "ODOMETER_REGRESSION",
                        $"Completion odometer ({request.OdometerAtCompletionKm:N0} km) cannot be lower than recorded vehicle or booking mileage ({minAllowedOdometerKm:N0} km).");
                }

                // d) Resolve itemized ServiceInvoiceLineItems (or synthesize summary lines) & compute LaborAmount + PartsAmount = GrossAmount
                var (laborAmount, partsAmount, grossAmount, draftLineItems) = await ResolveInvoiceLineItemsAndTotalsAsync(
                    request,
                    vehicle.MotorcycleModelId,
                    ct);

                // e) Resolve effective CustomerVoucher, release unused booking-reserved voucher, or consume applied voucher
                CustomerVoucher? existingBookingVoucher = null;
                if (booking.AppliedCustomerVoucherId is { } bookingVoucherId && bookingVoucherId != Guid.Empty)
                {
                    existingBookingVoucher = await _dbContext.CustomerVouchers
                        .IgnoreQueryFilters()
                        .Include(v => v.LoyaltyAccount)
                        .Include(v => v.VoucherCatalog)
                        .FirstOrDefaultAsync(v => v.Id == bookingVoucherId, ct);
                }

                var hasExplicitVoucherId = request.AppliedCustomerVoucherId is { } explicitVid && explicitVid != Guid.Empty;
                var hasExplicitVoucherCode = !string.IsNullOrWhiteSpace(request.VoucherCode);

                CustomerVoucher? appliedVoucher = null;
                if (hasExplicitVoucherId)
                {
                    var requestedVid = request.AppliedCustomerVoucherId!.Value;
                    var isRemovingSameReserved = request.RemoveReservedVoucher &&
                                                 booking.AppliedCustomerVoucherId == requestedVid;
                    if (!isRemovingSameReserved)
                    {
                        appliedVoucher = existingBookingVoucher?.Id == requestedVid
                            ? existingBookingVoucher
                            : await _dbContext.CustomerVouchers
                                .IgnoreQueryFilters()
                                .Include(v => v.LoyaltyAccount)
                                .Include(v => v.VoucherCatalog)
                                .FirstOrDefaultAsync(v => v.Id == requestedVid, ct)
                              ?? throw NotFoundException.ForEntity("CustomerVoucher", requestedVid);
                    }
                }
                else if (hasExplicitVoucherCode)
                {
                    var normalizedCode = request.VoucherCode!.Trim().ToUpperInvariant();
                    var isRemovingSameCode = request.RemoveReservedVoucher &&
                                             existingBookingVoucher is not null &&
                                             string.Equals(existingBookingVoucher.VoucherCode, normalizedCode, StringComparison.Ordinal);
                    if (!isRemovingSameCode)
                    {
                        appliedVoucher = existingBookingVoucher is not null &&
                                         string.Equals(existingBookingVoucher.VoucherCode, normalizedCode, StringComparison.Ordinal)
                            ? existingBookingVoucher
                            : await _dbContext.CustomerVouchers
                                .IgnoreQueryFilters()
                                .Include(v => v.LoyaltyAccount)
                                .Include(v => v.VoucherCatalog)
                                .FirstOrDefaultAsync(v => v.VoucherCode == normalizedCode, ct)
                              ?? throw NotFoundException.ForEntity("CustomerVoucher", normalizedCode);
                    }
                }
                else if (!request.RemoveReservedVoucher && request.UseBookingReservedVoucher != false)
                {
                    appliedVoucher = existingBookingVoucher;
                }

                // Release previously locked ReservedOnBooking voucher if removed or replaced
                if (existingBookingVoucher is not null &&
                    (appliedVoucher is null || appliedVoucher.Id != existingBookingVoucher.Id) &&
                    string.Equals(existingBookingVoucher.Status, "ReservedOnBooking", StringComparison.Ordinal))
                {
                    existingBookingVoucher.Status = existingBookingVoucher.ExpiresAtUtc > utcNow ? "Available" : "Expired";
                    existingBookingVoucher.UpdatedAtUtc = utcNow;
                }

                var voucherDiscountAmount = 0m;
                if (appliedVoucher is not null)
                {
                    if (appliedVoucher.LoyaltyAccount is null || appliedVoucher.LoyaltyAccount.CustomerId != booking.CustomerId)
                    {
                        throw new ForbiddenAccessException(
                            "The selected voucher belongs to another customer's loyalty account.",
                            "FORBIDDEN_RESOURCE_OWNER");
                    }

                    if (expectedVoucherRowVersion is not null)
                    {
                        if (!appliedVoucher.RowVersion.AsSpan().SequenceEqual(expectedVoucherRowVersion))
                        {
                            throw new ConflictException(
                                "The selected customer voucher was modified concurrently. Please refresh and retry.",
                                "CONCURRENCY_CONFLICT");
                        }

                        _dbContext.Entry(appliedVoucher).Property(v => v.RowVersion).OriginalValue = expectedVoucherRowVersion;
                    }

                    var isUsableStatus = string.Equals(appliedVoucher.Status, "Available", StringComparison.Ordinal) ||
                                         (string.Equals(appliedVoucher.Status, "ReservedOnBooking", StringComparison.Ordinal) &&
                                          booking.AppliedCustomerVoucherId == appliedVoucher.Id);

                    if (!isUsableStatus)
                    {
                        throw new ConflictException(
                            $"Voucher '{appliedVoucher.VoucherCode}' is currently '{appliedVoucher.Status}' and cannot be applied.",
                            "VOUCHER_NOT_AVAILABLE");
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

                    if (!string.Equals(catalog.ApplicableScope, "ServiceOnly", StringComparison.Ordinal) &&
                        !string.Equals(catalog.ApplicableScope, "All", StringComparison.Ordinal))
                    {
                        throw new BusinessRuleViolationException(
                            "VOUCHER_INELIGIBLE",
                            $"Voucher '{appliedVoucher.VoucherCode}' has scope '{catalog.ApplicableScope}' and cannot be applied to workshop service invoices.");
                    }

                    if (grossAmount < catalog.MinOrderAmount)
                    {
                        throw new BusinessRuleViolationException(
                            "VOUCHER_INELIGIBLE",
                            $"Invoice gross amount ({grossAmount:N0} VND) is below voucher minimum requirement ({catalog.MinOrderAmount:N0} VND).");
                    }

                    voucherDiscountAmount = ComputeVoucherDiscount(grossAmount, catalog);
                    appliedVoucher.Status = "Used";
                    appliedVoucher.UsedAtUtc = utcNow;
                    appliedVoucher.UpdatedAtUtc = utcNow;
                }

                // f) NetPaidAmount
                var netPaidAmount = grossAmount - voucherDiscountAmount;

                // g) Load LoyaltyTiers & Customer LoyaltyAccount (RowVersion-protected), compute two-step floor points & tier promotion
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
                    .FirstOrDefaultAsync(la => la.CustomerId == booking.CustomerId, ct);

                if (loyaltyAccount is null)
                {
                    loyaltyAccount = new LoyaltyAccount(Guid.CreateVersion7())
                    {
                        CustomerId = booking.CustomerId,
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

                var appliedMultiplier = previousTier.PointsMultiplier;
                var pointsEarned = LoyaltyPointsCalculator.CalculatePointsEarned(netPaidAmount, appliedMultiplier);

                loyaltyAccount.LifetimeSpentVnd += netPaidAmount;
                loyaltyAccount.UpdatedAtUtc = utcNow;

                var resultingTier = previousTier;
                var tierUpgraded = false;

                if (pointsEarned > 0)
                {
                    loyaltyAccount.AvailablePoints = checked(loyaltyAccount.AvailablePoints + pointsEarned);
                    loyaltyAccount.LifetimeQualifyingPoints = checked(loyaltyAccount.LifetimeQualifyingPoints + pointsEarned);

                    var evaluatedTierCode = LoyaltyPointsCalculator.EvaluateTier(loyaltyAccount.LifetimeQualifyingPoints);
                    var candidateTier = tiers
                        .Where(t => t.MinQualifyingPoints <= loyaltyAccount.LifetimeQualifyingPoints)
                        .OrderByDescending(t => t.MinQualifyingPoints)
                        .ThenByDescending(t => t.RankOrder)
                        .FirstOrDefault()
                        ?? tiers.FirstOrDefault(t =>
                            string.Equals(t.TierCode, evaluatedTierCode.ToString(), StringComparison.Ordinal));

                    if (candidateTier is not null && candidateTier.RankOrder > previousTier.RankOrder)
                    {
                        resultingTier = candidateTier;
                        loyaltyAccount.CurrentTierId = resultingTier.Id;
                        loyaltyAccount.CurrentTier = resultingTier;
                        loyaltyAccount.TierUpgradedAtUtc = utcNow;
                        tierUpgraded = true;
                    }
                }

                // h) Insert immutable ServiceInvoice + ServiceInvoiceLineItems
                var invoiceId = Guid.CreateVersion7();
                var invoiceNumber = GenerateInvoiceNumber(utcNow, invoiceId);
                foreach (var lineItem in draftLineItems)
                {
                    lineItem.ServiceInvoiceId = invoiceId;
                }

                var mappedLineItemDtos = draftLineItems
                    .OrderBy(li => li.LineNumber)
                    .Select(MapLineItemToDto)
                    .ToList();

                var effectiveLineItemsJson = normalizedLineItemsJson ?? JsonSerializer.Serialize(mappedLineItemDtos);

                var invoice = new ServiceInvoice(invoiceId)
                {
                    InvoiceNumber = invoiceNumber,
                    ServiceBookingId = booking.Id,
                    DealerId = booking.DealerId,
                    CustomerId = booking.CustomerId,
                    AppliedCustomerVoucherId = appliedVoucher?.Id,
                    IssuedByStaffId = staffUserId,
                    LaborAmount = laborAmount,
                    PartsAmount = partsAmount,
                    GrossAmount = grossAmount,
                    VoucherDiscountAmount = voucherDiscountAmount,
                    NetPaidAmount = netPaidAmount,
                    OdometerAtCompletionKm = request.OdometerAtCompletionKm,
                    PointsEarned = pointsEarned,
                    WorkSummaryNotes = normalizedWorkSummaryNotes,
                    LineItemsJson = effectiveLineItemsJson,
                    PaymentMethod = paymentMethod,
                    PaidAtUtc = utcNow,
                    CreatedAtUtc = utcNow
                };
                _dbContext.ServiceInvoices.Add(invoice);
                _dbContext.ServiceInvoiceLineItems.AddRange(draftLineItems);

                // i) Append immutable PointLedgerEntry when PointsEarned > 0
                if (pointsEarned > 0)
                {
                    var ledgerEntry = new PointLedgerEntry(Guid.CreateVersion7())
                    {
                        LoyaltyAccountId = loyaltyAccount.Id,
                        TransactionType = ServiceInvoiceEarnTransactionType,
                        PointsDelta = pointsEarned,
                        BalanceAfter = loyaltyAccount.AvailablePoints,
                        ReferenceType = ServiceInvoiceReferenceType,
                        ReferenceId = invoice.Id,
                        IdempotencyKey = $"SERVICE_INVOICE:{invoice.Id}:EARN",
                        Notes = $"Earned {pointsEarned} points on workshop invoice {invoice.InvoiceNumber} (booking {booking.BookingNumber}).",
                        CreatedByUserId = staffUserId,
                        CreatedAtUtc = utcNow
                    };
                    _dbContext.PointLedgerEntries.Add(ledgerEntry);
                }

                // j) Complete ServiceBooking & resolve open/linked MaintenanceReminders
                var previousBookingStatus = booking.Status;
                booking.Status = "Completed";
                booking.CompletedAtUtc = utcNow;
                booking.CheckedInAtUtc ??= utcNow;
                booking.AppliedCustomerVoucherId = appliedVoucher?.Id;
                booking.UpdatedAtUtc = utcNow;

                var completableReminders = await _dbContext.MaintenanceReminders
                    .IgnoreQueryFilters()
                    .Where(r =>
                        r.CustomerVehicleId == vehicle.Id &&
                        (r.Status == "Pending" || r.Status == "Sent" || r.Status == "Booked" || r.Status == "Scheduled") &&
                        (r.ResolvedBookingId == booking.Id || r.TargetMileageKm <= request.OdometerAtCompletionKm))
                    .ToListAsync(ct);

                foreach (var reminder in completableReminders)
                {
                    reminder.Status = "Completed";
                    reminder.ResolvedBookingId = booking.Id;
                    reminder.UpdatedAtUtc = utcNow;
                }

                // k) Update CustomerVehicle odometer & recompute NextDueMileageKm + NextDueDateUtc from active MaintenanceRules
                var modelRules = await _dbContext.MaintenanceRules
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.MotorcycleModelId == vehicle.MotorcycleModelId && r.IsActive && r.IntervalKm > 0 && r.IntervalMonths > 0)
                    .OrderBy(r => r.IntervalKm)
                    .ThenBy(r => r.IntervalMonths)
                    .ToListAsync(ct);

                var (nextDueMileageKm, nextDueDateUtc) = ComputeNextMaintenanceSchedule(
                    completionMileageKm: request.OdometerAtCompletionKm,
                    paidAtUtc: utcNow,
                    orderedRules: modelRules);

                vehicle.CurrentMileageKm = request.OdometerAtCompletionKm;
                vehicle.LastServiceMileageKm = request.OdometerAtCompletionKm;
                vehicle.LastServiceDateUtc = utcNow;
                vehicle.NextDueMileageKm = nextDueMileageKm;
                vehicle.NextDueDateUtc = nextDueDateUtc;
                vehicle.UpdatedAtUtc = utcNow;

                // l) Persist Notifications inside the SQL transaction, commit, then fan-out over SignalR
                postCommitCustomerId = booking.CustomerId;
                postCommitDealerId = booking.DealerId;

                var bookingNotifId = Guid.CreateVersion7();
                var bookingStatusPayload = new BookingStatusChangedRealtimeEvent(
                    NotificationId: bookingNotifId,
                    EventType: "BookingStatusChanged",
                    BookingId: booking.Id,
                    BookingNumber: booking.BookingNumber,
                    PreviousStatus: previousBookingStatus,
                    NewStatus: "Completed",
                    DealerName: dealer.Name,
                    OccurredAtUtc: utcNow);

                _dbContext.Notifications.Add(new Notification(bookingNotifId)
                {
                    RecipientUserId = booking.CustomerId,
                    EventType = "BookingStatusChanged",
                    Title = $"Workshop Service Completed — {invoice.InvoiceNumber}",
                    Message = $"Booking {booking.BookingNumber} at {dealer.Name} is completed (Net paid: {netPaidAmount:N0} VND, Odometer: {request.OdometerAtCompletionKm:N0} km).",
                    PayloadJson = JsonSerializer.Serialize(bookingStatusPayload),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });
                postCommitBookingStatusEvent = bookingStatusPayload;

                if (pointsEarned > 0)
                {
                    var pointsNotifId = Guid.CreateVersion7();
                    var pointsPayload = new LoyaltyPointsUpdatedRealtimeEvent(
                        NotificationId: pointsNotifId,
                        EventType: "LoyaltyPointsUpdated",
                        LoyaltyAccountId: loyaltyAccount.Id,
                        TransactionType: "ServiceInvoiceEarn",
                        PointsDelta: pointsEarned,
                        AvailablePointsAfter: loyaltyAccount.AvailablePoints,
                        LifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints,
                        ReferenceType: "ServiceInvoice",
                        ReferenceNumber: invoice.InvoiceNumber,
                        OccurredAtUtc: utcNow);

                    _dbContext.Notifications.Add(new Notification(pointsNotifId)
                    {
                        RecipientUserId = booking.CustomerId,
                        EventType = "LoyaltyPointsUpdated",
                        Title = $"+{pointsEarned:N0} Loyalty Points Accrued",
                        Message = $"Workshop invoice {invoice.InvoiceNumber} earned {pointsEarned:N0} loyalty points ({appliedMultiplier:0.00}x {previousTier.TierCode}).",
                        PayloadJson = JsonSerializer.Serialize(pointsPayload),
                        IsRead = false,
                        CreatedAtUtc = utcNow
                    });
                    postCommitPointsEvent = pointsPayload;

                    if (tierUpgraded)
                    {
                        var tierNotifId = Guid.CreateVersion7();
                        var tierPayload = new TierUpgradedRealtimeEvent(
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
                            RecipientUserId = booking.CustomerId,
                            EventType = "TierUpgraded",
                            Title = $"Congratulations! Upgraded to {resultingTier.DisplayName}",
                            Message = $"You unlocked {resultingTier.TierCode} tier ({resultingTier.PointsMultiplier:0.00}x points multiplier) with {loyaltyAccount.LifetimeQualifyingPoints:N0} lifetime qualifying points.",
                            PayloadJson = JsonSerializer.Serialize(tierPayload),
                            IsRead = false,
                            CreatedAtUtc = utcNow
                        });
                        postCommitTierEvent = tierPayload;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new ServiceInvoiceDto(
                    Id: invoice.Id,
                    InvoiceId: invoice.Id,
                    InvoiceNumber: invoice.InvoiceNumber,
                    ServiceBookingId: booking.Id,
                    BookingNumber: booking.BookingNumber,
                    ServiceType: booking.ServiceType,
                    DealerId: dealer.Id,
                    DealerCode: dealer.Code,
                    DealerName: dealer.Name,
                    CustomerId: customer.Id,
                    CustomerFullName: customer.FullName,
                    CustomerEmail: customer.Email,
                    CustomerVehicleId: vehicle.Id,
                    Vin: vehicle.VIN,
                    MotorcycleModelName: motorcycleModel?.ModelName ?? string.Empty,
                    LicensePlate: vehicle.LicensePlate,
                    IssuedByStaffId: staffUserId,
                    IssuedByStaffName: issuedByStaff?.FullName,
                    AppliedCustomerVoucherId: appliedVoucher?.Id,
                    AppliedVoucherCode: appliedVoucher?.VoucherCode,
                    LaborAmount: invoice.LaborAmount,
                    PartsAmount: invoice.PartsAmount,
                    GrossAmount: invoice.GrossAmount,
                    VoucherDiscountAmount: invoice.VoucherDiscountAmount,
                    NetPaidAmount: invoice.NetPaidAmount,
                    OdometerAtCompletionKm: invoice.OdometerAtCompletionKm,
                    NextDueMileageKm: vehicle.NextDueMileageKm,
                    NextDueDateUtc: vehicle.NextDueDateUtc,
                    PaymentMethod: invoice.PaymentMethod,
                    AppliedTierCode: previousTier.TierCode,
                    AppliedMultiplier: appliedMultiplier,
                    PointsEarned: invoice.PointsEarned,
                    AvailablePointsAfter: loyaltyAccount.AvailablePoints,
                    LifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints,
                    CurrentTierAfter: resultingTier.TierCode,
                    NewTierCode: resultingTier.TierCode,
                    TierUpgraded: tierUpgraded,
                    WorkSummaryNotes: invoice.WorkSummaryNotes,
                    LineItemsJson: invoice.LineItemsJson,
                    PaidAtUtc: invoice.PaidAtUtc,
                    CreatedAtUtc: invoice.CreatedAtUtc,
                    LineItems: mappedLineItemDtos);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Workshop booking, customer voucher, or loyalty balance was modified concurrently by another transaction. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_ServiceInvoices_BookingId", StringComparison.OrdinalIgnoreCase) ||
                sqlMsg.Contains("UQ_PointLedgerEntries_IdempotencyKey", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "A workshop service invoice has already been issued for this booking.",
                    "INVOICE_ALREADY_ISSUED");
            }

            throw new ConflictException(
                "A concurrent workshop invoice conflict occurred. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }

        // Post-commit SignalR broadcasts strictly after tx.CommitAsync succeeded
        if (postCommitBookingStatusEvent is not null)
        {
            await _notificationPublisher.PublishBookingStatusChangedAsync(
                postCommitBookingStatusEvent,
                postCommitCustomerId,
                postCommitDealerId,
                cancellationToken);
        }

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

        return settledDto;
    }

    public async Task<PagedResult<ServiceInvoiceDto>> ListServiceInvoicesAsync(
        ServiceInvoiceListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _paginationValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        var canViewDealer = !forceOwnCustomerOnly && _currentUserContext.HasPermission(Permissions.Invoices.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Invoices.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view workshop service invoices.",
                "FORBIDDEN_PERMISSION");
        }

        IQueryable<ServiceInvoice> invoicesQuery;

        if (!canViewDealer)
        {
            if (query.CustomerId is { } requestedCustId &&
                requestedCustId != Guid.Empty &&
                requestedCustId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view their own workshop service invoices.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }

            invoicesQuery = _dbContext.ServiceInvoices
                .AsNoTracking()
                .Where(i => i.CustomerId == callerUserId);

            if (query.DealerId is { } custDealerId && custDealerId != Guid.Empty)
            {
                invoicesQuery = invoicesQuery.Where(i => i.DealerId == custDealerId);
            }
        }
        else
        {
            // Global IDealerScoped filter is active on _dbContext.ServiceInvoices for DealerStaff.
            invoicesQuery = _dbContext.ServiceInvoices.AsNoTracking();

            if (_currentUserContext.IsDealerStaff)
            {
                var staffDealerId = _currentUserContext.RequireAssignedDealerId();
                if (query.DealerId is { } requestedDealerId &&
                    requestedDealerId != Guid.Empty &&
                    requestedDealerId != staffDealerId)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff is strictly prohibited from viewing service invoices belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                invoicesQuery = invoicesQuery.Where(i => i.DealerId == staffDealerId);
            }
            else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
            {
                invoicesQuery = invoicesQuery.Where(i => i.DealerId == adminDealerId);
            }

            if (query.CustomerId is { } filterCustomerId && filterCustomerId != Guid.Empty)
            {
                invoicesQuery = invoicesQuery.Where(i => i.CustomerId == filterCustomerId);
            }
        }

        if (query.ResolveEffectiveBookingId() is { } filterBookingId)
        {
            invoicesQuery = invoicesQuery.Where(i => i.ServiceBookingId == filterBookingId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            invoicesQuery = invoicesQuery.Where(i =>
                EF.Functions.Like(i.InvoiceNumber, pattern) ||
                (i.ServiceBooking != null && (
                    EF.Functions.Like(i.ServiceBooking.BookingNumber, pattern) ||
                    (i.ServiceBooking.CustomerVehicle != null && EF.Functions.Like(i.ServiceBooking.CustomerVehicle.VIN, pattern)))) ||
                (i.Customer != null && (
                    EF.Functions.Like(i.Customer.FullName, pattern) ||
                    EF.Functions.Like(i.Customer.Email, pattern))));
        }

        var totalCount = await invoicesQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<ServiceInvoiceDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var invoices = await ApplyInvoiceReadIncludes(invoicesQuery)
            .OrderByDescending(i => i.PaidAtUtc)
            .ThenByDescending(i => i.CreatedAtUtc)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var dtos = await HydrateExistingInvoiceDtosAsync(invoices, cancellationToken);

        return PagedResult<ServiceInvoiceDto>.Create(
            dtos,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<ServiceInvoiceDto> GetServiceInvoiceByIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Invoices.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Invoices.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view workshop service invoices.",
                "FORBIDDEN_PERMISSION");
        }

        var invoice = await ApplyInvoiceReadIncludes(
                _dbContext.ServiceInvoices
                    .IgnoreQueryFilters()
                    .AsNoTracking())
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw NotFoundException.ForEntity("ServiceInvoice", invoiceId);

        if (!canViewDealer)
        {
            if (invoice.CustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view their own workshop service invoices.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (invoice.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view workshop service invoices belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        var hydrated = await HydrateExistingInvoiceDtosAsync([invoice], cancellationToken);
        return hydrated[0];
    }

    public async Task<PrintableServiceReceiptDto> GetPrintableReceiptAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Invoices.ViewDealer);
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Invoices.ViewOwn);

        if (!canViewDealer && !canViewOwn)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view workshop service invoice receipts.",
                "FORBIDDEN_PERMISSION");
        }

        var invoice = await ApplyInvoiceReadIncludes(
                _dbContext.ServiceInvoices
                    .IgnoreQueryFilters()
                    .AsNoTracking())
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw NotFoundException.ForEntity("ServiceInvoice", invoiceId);

        if (!canViewDealer)
        {
            if (invoice.CustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view receipts for their own workshop service invoices.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (invoice.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view workshop service invoice receipts belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        var dto = (await HydrateExistingInvoiceDtosAsync([invoice], cancellationToken))[0];
        var dealer = invoice.Dealer;
        var customer = invoice.Customer;
        var booking = invoice.ServiceBooking;
        var vehicle = booking?.CustomerVehicle;
        var model = vehicle?.MotorcycleModel;

        var dealerPhone = dealer?.PhoneNumber ?? string.Empty;
        var customerPhone = customer?.PhoneNumber ?? string.Empty;

        return new PrintableServiceReceiptDto(
            ReceiptNumber: $"RCP-SVC-{invoice.InvoiceNumber}",
            ReceiptType: "ServiceInvoice",
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            BookingId: invoice.ServiceBookingId,
            BookingNumber: dto.BookingNumber,
            ServiceType: dto.ServiceType,
            PaymentMethod: invoice.PaymentMethod,
            IssuedAtUtc: invoice.PaidAtUtc,
            IssuedByStaffName: dto.IssuedByStaffName,
            VerificationCode: ReceiptVerificationHash.ComputeForServiceInvoice(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.NetPaidAmount),
            Dealer: new ReceiptDealerInfoDto(
                DealerId: invoice.DealerId,
                Code: dealer?.Code ?? string.Empty,
                Name: dealer?.Name ?? string.Empty,
                Address: dealer?.Address ?? string.Empty,
                City: dealer?.City ?? string.Empty,
                Phone: dealerPhone,
                PhoneNumber: dealerPhone),
            Customer: new ReceiptCustomerInfoDto(
                CustomerId: invoice.CustomerId,
                FullName: customer?.FullName ?? string.Empty,
                Email: customer?.Email ?? string.Empty,
                Phone: customerPhone,
                PhoneNumber: customerPhone),
            Vehicle: new ServiceReceiptVehicleInfoDto(
                CustomerVehicleId: dto.CustomerVehicleId,
                Vin: dto.Vin,
                LicensePlate: dto.LicensePlate,
                ModelCode: model?.ModelCode ?? string.Empty,
                ModelName: dto.MotorcycleModelName,
                OdometerAtBookingKm: booking?.OdometerAtBookingKm ?? 0,
                OdometerAtCompletionKm: invoice.OdometerAtCompletionKm,
                NextDueMileageKm: dto.NextDueMileageKm,
                NextDueDateUtc: dto.NextDueDateUtc),
            LineItems: dto.LineItems ?? [],
            LaborAmount: invoice.LaborAmount,
            PartsAmount: invoice.PartsAmount,
            GrossAmount: invoice.GrossAmount,
            AppliedVoucherCode: dto.AppliedVoucherCode,
            VoucherDiscountAmount: invoice.VoucherDiscountAmount,
            NetPaidAmount: invoice.NetPaidAmount,
            WorkSummaryNotes: invoice.WorkSummaryNotes,
            Loyalty: new ReceiptLoyaltySummaryDto(
                PointsEarned: invoice.PointsEarned,
                AppliedTierCode: dto.AppliedTierCode,
                TierCodeAfter: dto.CurrentTierAfter,
                NewTierCode: dto.NewTierCode,
                PointsMultiplierApplied: dto.AppliedMultiplier,
                TierMultiplier: dto.AppliedMultiplier,
                TierUpgraded: dto.TierUpgraded,
                AvailablePointsAfter: dto.AvailablePointsAfter,
                LifetimeQualifyingPointsAfter: dto.LifetimeQualifyingPointsAfter));
    }

    private async Task<(decimal LaborAmount, decimal PartsAmount, decimal GrossAmount, IReadOnlyList<ServiceInvoiceLineItem> DraftLineItems)> ResolveInvoiceLineItemsAndTotalsAsync(
        CreateServiceInvoiceRequest request,
        Guid vehicleModelId,
        CancellationToken cancellationToken)
    {
        var draftLines = new List<ServiceInvoiceLineItem>();
        decimal partsAmount;
        decimal laborAmount;

        if (request.LineItems is { Count: > 0 })
        {
            var referencedPartIds = request.LineItems
                .Select(x => x.ResolveEffectiveSparePartId())
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToArray();

            var partsById = referencedPartIds.Length == 0
                ? new Dictionary<Guid, SparePart>()
                : await _dbContext.SpareParts
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(p => referencedPartIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var reqItem in request.LineItems)
            {
                ServiceInvoiceValidationRules.TryNormalizeLineItemType(reqItem.ItemType, out var itemType);

                if (reqItem.ResolveEffectiveSparePartId() is { } partId)
                {
                    if (!partsById.TryGetValue(partId, out var part))
                    {
                        throw NotFoundException.ForEntity("SparePart", partId);
                    }

                    if (part.IsDeleted || !part.IsActive)
                    {
                        throw new BusinessRuleViolationException(
                            "SPARE_PART_INACTIVE",
                            $"Spare part '{part.PartCode}' ({part.Name}) is currently inactive and cannot be billed.");
                    }

                    if (part.CompatibleModelId.HasValue && part.CompatibleModelId.Value != vehicleModelId)
                    {
                        throw new BusinessRuleViolationException(
                            "SPARE_PART_INCOMPATIBLE",
                            $"Spare part '{part.PartCode}' ({part.Name}) is incompatible with the customer's motorcycle model.");
                    }

                    var unitPrice = Math.Round(reqItem.UnitPriceVnd ?? part.UnitPriceVnd, 2, MidpointRounding.AwayFromZero);
                    if (unitPrice <= 0m)
                    {
                        throw new BusinessRuleViolationException(
                            "INVALID_INVOICE_AMOUNT",
                            "Line item unit price must be strictly greater than zero.");
                    }

                    var description = !string.IsNullOrWhiteSpace(reqItem.Description)
                        ? reqItem.Description.Trim()
                        : part.Name;
                    var lineTotal = ServiceInvoiceValidationRules.ComputeLineTotalVnd(reqItem.Quantity, unitPrice);

                    draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                    {
                        SparePartId = part.Id,
                        PartCodeSnapshot = part.PartCode,
                        Description = description,
                        ItemType = "Part",
                        Quantity = reqItem.Quantity,
                        UnitPriceVnd = unitPrice,
                        LineTotalVnd = lineTotal
                    });
                }
                else
                {
                    var unitPrice = Math.Round(reqItem.UnitPriceVnd ?? 0m, 2, MidpointRounding.AwayFromZero);
                    if (unitPrice <= 0m)
                    {
                        throw new BusinessRuleViolationException(
                            "INVALID_INVOICE_AMOUNT",
                            "Line item unit price must be strictly greater than zero.");
                    }

                    var description = !string.IsNullOrWhiteSpace(reqItem.Description)
                        ? reqItem.Description.Trim()
                        : (string.Equals(itemType, "Labor", StringComparison.Ordinal)
                            ? "Workshop Service & Inspection Labor"
                            : "Workshop Genuine Parts & Consumables");
                    var lineTotal = ServiceInvoiceValidationRules.ComputeLineTotalVnd(reqItem.Quantity, unitPrice);

                    draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                    {
                        SparePartId = null,
                        PartCodeSnapshot = string.Equals(itemType, "Labor", StringComparison.Ordinal)
                            ? "LABOR-ITEM"
                            : "PART-CUSTOM",
                        Description = description,
                        ItemType = itemType,
                        Quantity = reqItem.Quantity,
                        UnitPriceVnd = unitPrice,
                        LineTotalVnd = lineTotal
                    });
                }
            }

            var partLines = draftLines.Where(l => string.Equals(l.ItemType, "Part", StringComparison.Ordinal)).ToList();
            var laborLines = draftLines.Where(l => string.Equals(l.ItemType, "Labor", StringComparison.Ordinal)).ToList();

            if (partLines.Count > 0)
            {
                partsAmount = partLines.Sum(l => l.LineTotalVnd);
            }
            else
            {
                partsAmount = Math.Round(request.PartsAmount, 2, MidpointRounding.AwayFromZero);
                if (partsAmount > 0m)
                {
                    draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                    {
                        SparePartId = null,
                        PartCodeSnapshot = "PARTS-SUMMARY",
                        Description = "Workshop Genuine Parts & Consumables",
                        ItemType = "Part",
                        Quantity = 1,
                        UnitPriceVnd = partsAmount,
                        LineTotalVnd = partsAmount
                    });
                }
            }

            if (laborLines.Count > 0)
            {
                laborAmount = laborLines.Sum(l => l.LineTotalVnd);
            }
            else
            {
                laborAmount = Math.Round(request.LaborAmount, 2, MidpointRounding.AwayFromZero);
                if (laborAmount > 0m)
                {
                    draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                    {
                        SparePartId = null,
                        PartCodeSnapshot = "LABOR-SUMMARY",
                        Description = "Workshop Service & Inspection Labor",
                        ItemType = "Labor",
                        Quantity = 1,
                        UnitPriceVnd = laborAmount,
                        LineTotalVnd = laborAmount
                    });
                }
            }
        }
        else
        {
            laborAmount = Math.Round(request.LaborAmount, 2, MidpointRounding.AwayFromZero);
            partsAmount = Math.Round(request.PartsAmount, 2, MidpointRounding.AwayFromZero);

            if (partsAmount > 0m)
            {
                draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                {
                    SparePartId = null,
                    PartCodeSnapshot = "PARTS-SUMMARY",
                    Description = "Workshop Genuine Parts & Consumables",
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = partsAmount,
                    LineTotalVnd = partsAmount
                });
            }

            if (laborAmount > 0m)
            {
                draftLines.Add(new ServiceInvoiceLineItem(Guid.CreateVersion7())
                {
                    SparePartId = null,
                    PartCodeSnapshot = "LABOR-SUMMARY",
                    Description = "Workshop Service & Inspection Labor",
                    ItemType = "Labor",
                    Quantity = 1,
                    UnitPriceVnd = laborAmount,
                    LineTotalVnd = laborAmount
                });
            }
        }

        var grossAmount = laborAmount + partsAmount;
        if (laborAmount < 0m || partsAmount < 0m || grossAmount <= 0m || draftLines.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "INVALID_INVOICE_AMOUNT",
                "LaborAmount and PartsAmount must be non-negative and GrossAmount must be greater than zero.");
        }

        for (var idx = 0; idx < draftLines.Count; idx++)
        {
            draftLines[idx].LineNumber = idx + 1;
        }

        return (laborAmount, partsAmount, grossAmount, draftLines);
    }

    private static ServiceInvoiceLineItemDto MapLineItemToDto(ServiceInvoiceLineItem item) =>
        new(
            Id: item.Id,
            LineNumber: item.LineNumber,
            SparePartId: item.SparePartId,
            PartCodeSnapshot: item.PartCodeSnapshot,
            Description: item.Description,
            ItemType: item.ItemType,
            Quantity: item.Quantity,
            UnitPriceVnd: item.UnitPriceVnd,
            LineTotalVnd: item.LineTotalVnd);

    private static IQueryable<ServiceInvoice> ApplyInvoiceReadIncludes(IQueryable<ServiceInvoice> query) =>
        query
            .Include(i => i.Dealer)
            .Include(i => i.Customer)
                .ThenInclude(c => c!.LoyaltyAccount)
                    .ThenInclude(la => la!.CurrentTier)
            .Include(i => i.IssuedByStaff)
            .Include(i => i.AppliedCustomerVoucher)
            .Include(i => i.LineItems)
            .Include(i => i.ServiceBooking)
                .ThenInclude(b => b!.CustomerVehicle)
                    .ThenInclude(v => v!.MotorcycleModel);

    private static decimal ComputeVoucherDiscount(decimal grossAmount, VoucherCatalog catalog) =>
        VoucherAndDocumentNumberHelper.ComputeVoucherDiscountAmount(grossAmount, catalog);

    public static (int NextDueMileageKm, DateTimeOffset NextDueDateUtc) ComputeNextMaintenanceSchedule(
        int completionMileageKm,
        DateTimeOffset paidAtUtc,
        IReadOnlyList<MaintenanceRule> orderedRules)
    {
        ArgumentNullException.ThrowIfNull(orderedRules);
        var baselineKm = Math.Max(0, completionMileageKm);

        if (orderedRules.Count == 0)
        {
            return (baselineKm + DefaultFallbackIntervalKm, paidAtUtc.AddMonths(DefaultFallbackIntervalMonths));
        }

        var candidates = new List<(MaintenanceRule Rule, int TargetMileageKm, DateTimeOffset DueDateUtc)>(orderedRules.Count);
        foreach (var rule in orderedRules)
        {
            if (!rule.IsRecurring)
            {
                // Non-recurring milestone (e.g., 1,000 km break-in) remains upcoming only if completion is well before its lead window.
                var leadKm = Math.Max(0, rule.LeadMileageKm);
                if (baselineKm >= rule.IntervalKm - leadKm)
                {
                    continue;
                }

                candidates.Add((rule, rule.IntervalKm, paidAtUtc.AddMonths(Math.Max(1, rule.IntervalMonths))));
            }
            else
            {
                var stepKm = Math.Max(500, rule.IntervalKm);
                var targetKm = ((baselineKm / stepKm) + 1) * stepKm;
                var leadKm = Math.Max(0, rule.LeadMileageKm);

                // If service was performed inside the upcoming milestone's lead window (e.g., 3,935 km for 4,000 km), roll to the next interval.
                if (targetKm - baselineKm <= leadKm)
                {
                    targetKm += stepKm;
                }

                candidates.Add((rule, targetKm, paidAtUtc.AddMonths(Math.Max(1, rule.IntervalMonths))));
            }
        }

        if (candidates.Count == 0)
        {
            var fallbackRule = orderedRules[^1];
            var fallbackStep = Math.Max(500, fallbackRule.IntervalKm);
            return (baselineKm + fallbackStep, paidAtUtc.AddMonths(Math.Max(1, fallbackRule.IntervalMonths)));
        }

        var nextMilestone = candidates
            .OrderBy(c => c.TargetMileageKm)
            .ThenByDescending(c => c.Rule.IntervalKm)
            .First();

        return (nextMilestone.TargetMileageKm, nextMilestone.DueDateUtc);
    }

    private static string GenerateInvoiceNumber(DateTimeOffset utcNow, Guid invoiceId) =>
        VoucherAndDocumentNumberHelper.FormatDocumentNumber("INV", utcNow, invoiceId);

    private readonly record struct InvoiceLedgerSnapshot(
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

    private async Task<IReadOnlyList<ServiceInvoiceDto>> HydrateExistingInvoiceDtosAsync(
        IReadOnlyList<ServiceInvoice> invoices,
        CancellationToken cancellationToken)
    {
        if (invoices.Count == 0)
        {
            return [];
        }

        var invoiceIds = invoices.Select(i => i.Id).ToArray();

        var tiers = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.RankOrder)
            .ToListAsync(cancellationToken);

        var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, BronzeTierCode, StringComparison.Ordinal))
                         ?? new LoyaltyTier
                         {
                             TierCode = BronzeTierCode,
                             RankOrder = 1,
                             MinQualifyingPoints = 0,
                             PointsMultiplier = 1.00m
                         };

        var invoiceLedgers = await _dbContext.PointLedgerEntries
            .AsNoTracking()
            .Where(p => p.ReferenceType == ServiceInvoiceReferenceType && invoiceIds.Contains(p.ReferenceId))
            .Select(p => new InvoiceLedgerSnapshot(
                p.Id,
                p.LoyaltyAccountId,
                p.ReferenceId,
                p.PointsDelta,
                p.BalanceAfter,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var ledgerByInvoiceId = invoiceLedgers
            .GroupBy(p => p.ReferenceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAtUtc).First());

        Dictionary<Guid, List<PositiveAccrualSnapshot>> positiveAccrualsByAccount;
        if (invoiceLedgers.Count == 0)
        {
            positiveAccrualsByAccount = [];
        }
        else
        {
            var matchedAccountIds = invoiceLedgers
                .Select(p => p.LoyaltyAccountId)
                .Distinct()
                .ToArray();

            var positiveRows = await _dbContext.PointLedgerEntries
                .AsNoTracking()
                .Where(p =>
                    matchedAccountIds.Contains(p.LoyaltyAccountId) &&
                    p.PointsDelta > 0 &&
                    (p.TransactionType == SalesOrderEarnTransactionType ||
                     p.TransactionType == ServiceInvoiceEarnTransactionType))
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

        return invoices
            .Select(invoice => MapExistingInvoiceToDto(
                invoice,
                tiers,
                bronzeTier,
                ledgerByInvoiceId.TryGetValue(invoice.Id, out var ledgerSnapshot)
                    ? ledgerSnapshot
                    : (InvoiceLedgerSnapshot?)null,
                invoice.Customer?.LoyaltyAccount is { } acc
                    ? positiveAccrualsByAccount.GetValueOrDefault(acc.Id)
                    : null))
            .ToList();
    }

    private static ServiceInvoiceDto MapExistingInvoiceToDto(
        ServiceInvoice invoice,
        IReadOnlyList<LoyaltyTier> activeTiers,
        LoyaltyTier bronzeTier,
        InvoiceLedgerSnapshot? invoiceLedger,
        IReadOnlyList<PositiveAccrualSnapshot>? accountPositiveAccruals)
    {
        var account = invoice.Customer?.LoyaltyAccount;
        var currentTier = account?.CurrentTier
                          ?? activeTiers.FirstOrDefault(t => account is not null && t.Id == account.CurrentTierId)
                          ?? bronzeTier;

        var availablePointsAfter = invoiceLedger?.BalanceAfter ?? (account?.AvailablePoints ?? 0);

        int lifetimeQualifyingAfter;
        if (invoiceLedger is { } ledger && accountPositiveAccruals is { Count: > 0 })
        {
            lifetimeQualifyingAfter = accountPositiveAccruals
                .Where(p =>
                    p.CreatedAtUtc < ledger.CreatedAtUtc ||
                    (p.CreatedAtUtc == ledger.CreatedAtUtc && p.Id == ledger.Id))
                .Sum(p => p.PointsDelta);

            if (lifetimeQualifyingAfter < invoice.PointsEarned)
            {
                lifetimeQualifyingAfter = invoice.PointsEarned;
            }
        }
        else
        {
            lifetimeQualifyingAfter = account?.LifetimeQualifyingPoints ?? invoice.PointsEarned;
        }

        LoyaltyTier appliedTier;
        if (invoice.NetPaidAmount >= LoyaltyPointsCalculator.BaseUnitVnd &&
            invoice.PointsEarned > 0 &&
            activeTiers.Count > 0)
        {
            var matchedFormulaTier = activeTiers.FirstOrDefault(t =>
                LoyaltyPointsCalculator.CalculatePointsEarned(invoice.NetPaidAmount, t.PointsMultiplier) == invoice.PointsEarned);

            if (matchedFormulaTier is not null)
            {
                appliedTier = matchedFormulaTier;
            }
            else
            {
                var qualifyingBefore = Math.Max(0, lifetimeQualifyingAfter - invoice.PointsEarned);
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
        if (invoiceLedger is not null && activeTiers.Count > 0)
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
        var booking = invoice.ServiceBooking;
        var vehicle = booking?.CustomerVehicle;
        var lineItemDtos = invoice.LineItems is { Count: > 0 }
            ? invoice.LineItems
                .OrderBy(li => li.LineNumber)
                .Select(MapLineItemToDto)
                .ToList()
            : BuildFallbackLineItemDtos(invoice);

        return new ServiceInvoiceDto(
            Id: invoice.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            ServiceBookingId: invoice.ServiceBookingId,
            BookingNumber: booking?.BookingNumber ?? string.Empty,
            ServiceType: booking?.ServiceType ?? string.Empty,
            DealerId: invoice.DealerId,
            DealerCode: invoice.Dealer?.Code ?? string.Empty,
            DealerName: invoice.Dealer?.Name ?? string.Empty,
            CustomerId: invoice.CustomerId,
            CustomerFullName: invoice.Customer?.FullName ?? string.Empty,
            CustomerEmail: invoice.Customer?.Email ?? string.Empty,
            CustomerVehicleId: booking?.CustomerVehicleId ?? Guid.Empty,
            Vin: vehicle?.VIN ?? string.Empty,
            MotorcycleModelName: vehicle?.MotorcycleModel?.ModelName ?? string.Empty,
            LicensePlate: vehicle?.LicensePlate,
            IssuedByStaffId: invoice.IssuedByStaffId,
            IssuedByStaffName: invoice.IssuedByStaff?.FullName,
            AppliedCustomerVoucherId: invoice.AppliedCustomerVoucherId,
            AppliedVoucherCode: invoice.AppliedCustomerVoucher?.VoucherCode,
            LaborAmount: invoice.LaborAmount,
            PartsAmount: invoice.PartsAmount,
            GrossAmount: invoice.GrossAmount,
            VoucherDiscountAmount: invoice.VoucherDiscountAmount,
            NetPaidAmount: invoice.NetPaidAmount,
            OdometerAtCompletionKm: invoice.OdometerAtCompletionKm,
            NextDueMileageKm: vehicle?.NextDueMileageKm,
            NextDueDateUtc: vehicle?.NextDueDateUtc,
            PaymentMethod: invoice.PaymentMethod,
            AppliedTierCode: appliedTier.TierCode,
            AppliedMultiplier: appliedTier.PointsMultiplier,
            PointsEarned: invoice.PointsEarned,
            AvailablePointsAfter: availablePointsAfter,
            LifetimeQualifyingPointsAfter: lifetimeQualifyingAfter,
            CurrentTierAfter: resultingTier.TierCode,
            NewTierCode: resultingTier.TierCode,
            TierUpgraded: tierUpgraded,
            WorkSummaryNotes: invoice.WorkSummaryNotes,
            LineItemsJson: invoice.LineItemsJson,
            PaidAtUtc: invoice.PaidAtUtc,
            CreatedAtUtc: invoice.CreatedAtUtc,
            LineItems: lineItemDtos);
    }

    private static IReadOnlyList<ServiceInvoiceLineItemDto> BuildFallbackLineItemDtos(ServiceInvoice invoice)
    {
        var fallback = new List<ServiceInvoiceLineItemDto>(2);
        var nextLine = 1;
        if (invoice.PartsAmount > 0m)
        {
            fallback.Add(new ServiceInvoiceLineItemDto(
                Id: Guid.Empty,
                LineNumber: nextLine++,
                SparePartId: null,
                PartCodeSnapshot: "PARTS-SUMMARY",
                Description: "Workshop Genuine Parts & Consumables",
                ItemType: "Part",
                Quantity: 1,
                UnitPriceVnd: invoice.PartsAmount,
                LineTotalVnd: invoice.PartsAmount));
        }

        if (invoice.LaborAmount > 0m || fallback.Count == 0)
        {
            fallback.Add(new ServiceInvoiceLineItemDto(
                Id: Guid.Empty,
                LineNumber: nextLine,
                SparePartId: null,
                PartCodeSnapshot: "LABOR-SUMMARY",
                Description: "Workshop Service & Inspection Labor",
                ItemType: "Labor",
                Quantity: 1,
                UnitPriceVnd: invoice.LaborAmount,
                LineTotalVnd: invoice.LaborAmount));
        }

        return fallback;
    }
}
