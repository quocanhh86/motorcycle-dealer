using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.Infrastructure.Services.Showroom;

namespace MotorcycleDealer.Infrastructure.Services.Reviews;

public sealed class ServiceReviewService : IServiceReviewService
{
    public const int StandardReviewBonusPoints = ServiceReviewConstants.StandardReviewBonusPoints;
    private const string ServiceReviewReferenceType = "ServiceReview";
    private const string ServiceReviewBonusTxType = nameof(LedgerTransactionType.ServiceReviewBonus);

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<ServiceReviewListQuery> _listQueryValidator;
    private readonly IValidator<CreateServiceReviewRequest> _createValidator;
    private readonly IValidator<RespondToServiceReviewRequest> _respondValidator;

    public ServiceReviewService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<ServiceReviewListQuery> listQueryValidator,
        IValidator<CreateServiceReviewRequest> createValidator,
        IValidator<RespondToServiceReviewRequest> respondValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _listQueryValidator = listQueryValidator ?? throw new ArgumentNullException(nameof(listQueryValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _respondValidator = respondValidator ?? throw new ArgumentNullException(nameof(respondValidator));
    }

    public async Task<ServiceReviewDto> CreateServiceReviewAsync(
        CreateServiceReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.IsCustomer || !_currentUserContext.HasPermission(Permissions.Reviews.CreateOwn))
        {
            throw new ForbiddenAccessException(
                "Only authenticated Customer accounts may submit workshop CSAT service reviews.",
                "FORBIDDEN_PERMISSION");
        }

        var targetInvoiceId = request.ResolveEffectiveInvoiceId();
        ServiceReviewValidationRules.TryNormalizeQualityTags(
            request.ResolveRawQualityTags(),
            out var normalizedQualityTags,
            out _,
            out _);

        var normalizedComment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        ServiceReviewSubmittedRealtimeEvent? postCommitReviewEvent = null;
        LoyaltyPointsUpdatedRealtimeEvent? postCommitPointsEvent = null;
        var postCommitDealerId = Guid.Empty;

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        ServiceReviewDto createdDto;

        try
        {
            createdDto = await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                postCommitReviewEvent = null;
                postCommitPointsEvent = null;
                postCommitDealerId = Guid.Empty;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                if (_dbContext.Database.IsRelational())
                {
                    // Serialize concurrent review attempts targeting the same ServiceInvoiceId with zero deadlocks.
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT [Id] FROM [dbo].[ServiceInvoices] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {targetInvoiceId};",
                        ct);
                }

                var utcNow = _dateTimeProvider.UtcNow;

                // a) Load ServiceInvoice (.IgnoreQueryFilters() with ServiceBooking, Dealer, CustomerVehicle)
                var invoice = await _dbContext.ServiceInvoices
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(i => i.Dealer)
                    .Include(i => i.Customer)
                    .Include(i => i.ServiceBooking)
                        .ThenInclude(b => b!.CustomerVehicle)
                            .ThenInclude(v => v!.MotorcycleModel)
                    .FirstOrDefaultAsync(i => i.Id == targetInvoiceId, ct)
                    ?? throw NotFoundException.ForEntity("ServiceInvoice", targetInvoiceId);

                var booking = invoice.ServiceBooking
                              ?? throw NotFoundException.ForEntity("ServiceBooking", invoice.ServiceBookingId);

                var ownerCustomerId = booking.CustomerId != Guid.Empty ? booking.CustomerId : invoice.CustomerId;
                if (ownerCustomerId != callerUserId)
                {
                    throw new ForbiddenAccessException(
                        "Customers may only submit CSAT reviews for their own settled workshop service invoices.",
                        "FORBIDDEN_RESOURCE_OWNER");
                }

                if (!string.Equals(booking.Status, nameof(BookingStatus.Completed), StringComparison.Ordinal))
                {
                    throw new BusinessRuleViolationException(
                        "BOOKING_NOT_COMPLETED",
                        $"Cannot submit a CSAT service review while booking '{booking.BookingNumber}' is in '{booking.Status}' status. Only Completed bookings can be reviewed.");
                }

                var dealer = invoice.Dealer
                             ?? await _dbContext.Dealers
                                 .IgnoreQueryFilters()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(d => d.Id == invoice.DealerId, ct);

                if (dealer is null || dealer.IsDeleted || !dealer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "DEALER_INACTIVE",
                        "Cannot submit a workshop CSAT review for an inactive dealership.");
                }

                var customer = invoice.Customer
                               ?? await _dbContext.Users
                                   .IgnoreQueryFilters()
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(u => u.Id == callerUserId, ct)
                               ?? throw new UnauthorizedAccessException("Authenticated customer profile is missing.");

                if (customer.IsDeleted || !customer.IsActive)
                {
                    throw new UnauthorizedAccessException("Authenticated customer profile is inactive.");
                }

                // b) Verify 1:1 ServiceReview uniqueness on ServiceInvoiceId
                var bonusIdempotencyKey = $"SERVICE_REVIEW:{invoice.Id}:BONUS";
                var reviewAlreadyExists = await _dbContext.ServiceReviews
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(r => r.ServiceInvoiceId == invoice.Id, ct);

                if (reviewAlreadyExists)
                {
                    throw new ConflictException(
                        $"A CSAT review has already been submitted for workshop invoice '{invoice.InvoiceNumber}'.",
                        "REVIEW_ALREADY_SUBMITTED");
                }

                var bonusAlreadyRecorded = await _dbContext.PointLedgerEntries
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(p => p.IdempotencyKey == bonusIdempotencyKey, ct);

                if (bonusAlreadyRecorded)
                {
                    throw new ConflictException(
                        $"A CSAT review bonus has already been awarded for workshop invoice '{invoice.InvoiceNumber}'.",
                        "REVIEW_ALREADY_SUBMITTED");
                }

                // c) Load customer's LoyaltyAccount (RowVersion concurrency protected) and credit +25 AvailablePoints ONLY
                var tiers = await _dbContext.LoyaltyTiers
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.RankOrder)
                    .ToListAsync(ct);

                var bronzeTier = tiers.FirstOrDefault(t => string.Equals(t.TierCode, "Bronze", StringComparison.Ordinal))
                                 ?? tiers.FirstOrDefault()
                                 ?? throw new BusinessRuleViolationException(
                                     "SYSTEM_SEED_MISSING",
                                     "Canonical LoyaltyTiers seed data is missing.");

                var loyaltyAccount = await _dbContext.LoyaltyAccounts
                    .FirstOrDefaultAsync(a => a.CustomerId == callerUserId, ct);

                if (loyaltyAccount is null)
                {
                    loyaltyAccount = new LoyaltyAccount(Guid.CreateVersion7())
                    {
                        CustomerId = callerUserId,
                        CurrentTierId = bronzeTier.Id,
                        AvailablePoints = 0,
                        LifetimeQualifyingPoints = 0,
                        LifetimeSpentVnd = 0m,
                        CreatedAtUtc = utcNow
                    };
                    _dbContext.LoyaltyAccounts.Add(loyaltyAccount);
                }
                else if (loyaltyAccount.RowVersion is { Length: > 0 })
                {
                    _dbContext.Entry(loyaltyAccount).Property(a => a.RowVersion).OriginalValue =
                        loyaltyAccount.RowVersion.ToArray();
                }

                // ADR-003 non-negotiable goodwill/bonus invariant:
                // Add +25 to AvailablePoints ONLY; LifetimeQualifyingPoints MUST remain strictly unchanged!
                loyaltyAccount.AvailablePoints = checked(loyaltyAccount.AvailablePoints + StandardReviewBonusPoints);
                loyaltyAccount.UpdatedAtUtc = utcNow;

                // d) Persist ServiceReview, immutable PointLedgerEntry, and persistent Notifications
                var reviewId = Guid.CreateVersion7();
                var reviewNumber = GenerateReviewNumber(utcNow, reviewId);
                var vehicle = booking.CustomerVehicle;
                var modelName = vehicle?.MotorcycleModel?.ModelName ?? "Motorcycle";

                var review = new ServiceReview(reviewId)
                {
                    ReviewNumber = reviewNumber,
                    ServiceInvoiceId = invoice.Id,
                    ServiceBookingId = booking.Id,
                    CustomerId = callerUserId,
                    DealerId = dealer.Id,
                    CustomerVehicleId = booking.CustomerVehicleId,
                    Rating = request.Rating,
                    QualityTags = normalizedQualityTags,
                    Comment = normalizedComment,
                    BonusPointsAwarded = StandardReviewBonusPoints,
                    StaffResponse = null,
                    RespondedByUserId = null,
                    RespondedAtUtc = null,
                    CreatedAtUtc = utcNow
                };
                _dbContext.ServiceReviews.Add(review);

                var ledgerEntry = new PointLedgerEntry(Guid.CreateVersion7())
                {
                    LoyaltyAccountId = loyaltyAccount.Id,
                    TransactionType = ServiceReviewBonusTxType,
                    PointsDelta = StandardReviewBonusPoints,
                    BalanceAfter = loyaltyAccount.AvailablePoints,
                    ReferenceType = ServiceReviewReferenceType,
                    ReferenceId = review.Id,
                    IdempotencyKey = bonusIdempotencyKey,
                    Notes = $"Earned +{StandardReviewBonusPoints} CSAT bonus points for {request.Rating}-star workshop review {reviewNumber} on invoice {invoice.InvoiceNumber}.",
                    CreatedByUserId = callerUserId,
                    CreatedAtUtc = utcNow
                };
                _dbContext.PointLedgerEntries.Add(ledgerEntry);

                var dealerNotifId = Guid.CreateVersion7();
                var reviewEvent = new ServiceReviewSubmittedRealtimeEvent(
                    NotificationId: dealerNotifId,
                    EventType: nameof(NotificationEventType.ServiceReviewSubmitted),
                    ReviewId: review.Id,
                    ReviewNumber: review.ReviewNumber,
                    ServiceInvoiceId: invoice.Id,
                    InvoiceNumber: invoice.InvoiceNumber,
                    DealerId: dealer.Id,
                    DealerName: dealer.Name,
                    CustomerFullName: customer.FullName,
                    MotorcycleModelName: modelName,
                    Rating: review.Rating,
                    BonusPointsAwarded: review.BonusPointsAwarded,
                    OccurredAtUtc: utcNow);

                _dbContext.Notifications.Add(new Notification(dealerNotifId)
                {
                    TargetDealerId = dealer.Id,
                    EventType = nameof(NotificationEventType.ServiceReviewSubmitted),
                    Title = $"New {review.Rating}★ CSAT Review — {review.ReviewNumber}",
                    Message = $"{customer.FullName} rated invoice {invoice.InvoiceNumber} ({modelName}) {review.Rating}/5 stars.",
                    PayloadJson = JsonSerializer.Serialize(reviewEvent),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                var pointsNotifId = Guid.CreateVersion7();
                var pointsEvent = new LoyaltyPointsUpdatedRealtimeEvent(
                    NotificationId: pointsNotifId,
                    EventType: nameof(NotificationEventType.LoyaltyPointsUpdated),
                    LoyaltyAccountId: loyaltyAccount.Id,
                    TransactionType: ServiceReviewBonusTxType,
                    PointsDelta: StandardReviewBonusPoints,
                    AvailablePointsAfter: loyaltyAccount.AvailablePoints,
                    LifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints,
                    ReferenceType: ServiceReviewReferenceType,
                    ReferenceNumber: review.ReviewNumber,
                    OccurredAtUtc: utcNow);

                _dbContext.Notifications.Add(new Notification(pointsNotifId)
                {
                    RecipientUserId = callerUserId,
                    EventType = nameof(NotificationEventType.LoyaltyPointsUpdated),
                    Title = $"+{StandardReviewBonusPoints} CSAT Bonus Points Earned",
                    Message = $"Thank you for rating workshop invoice {invoice.InvoiceNumber} ({review.Rating}/5 stars)! +{StandardReviewBonusPoints} spendable loyalty points credited.",
                    PayloadJson = JsonSerializer.Serialize(pointsEvent),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                postCommitDealerId = dealer.Id;
                postCommitReviewEvent = reviewEvent;
                postCommitPointsEvent = pointsEvent;

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapToDto(
                    review: review,
                    invoice: invoice,
                    booking: booking,
                    customer: customer,
                    dealer: dealer,
                    vehicle: vehicle,
                    respondedByUser: null,
                    availablePointsAfter: loyaltyAccount.AvailablePoints,
                    lifetimeQualifyingPointsAfter: loyaltyAccount.LifetimeQualifyingPoints);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
        {
            if (sqlMsg.Contains("UQ_ServiceReviews_ServiceInvoiceId", StringComparison.OrdinalIgnoreCase) ||
                sqlMsg.Contains("UQ_PointLedgerEntries_IdempotencyKey", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "A CSAT review has already been submitted for this workshop service invoice.",
                    "REVIEW_ALREADY_SUBMITTED");
            }

            throw new ConflictException(
                "A concurrent CSAT review persistence conflict occurred. Please retry.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateConcurrencyException)
        {
            var nowReviewed = await _dbContext.ServiceReviews
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(r => r.ServiceInvoiceId == targetInvoiceId, cancellationToken);

            if (nowReviewed)
            {
                throw new ConflictException(
                    "A CSAT review has already been submitted for this workshop service invoice.",
                    "REVIEW_ALREADY_SUBMITTED");
            }

            throw new ConflictException(
                "Your loyalty wallet balance was modified concurrently by another transaction. Please retry.",
                "CONCURRENCY_CONFLICT");
        }

        if (postCommitReviewEvent is not null)
        {
            await _notificationPublisher.PublishServiceReviewSubmittedAsync(
                postCommitReviewEvent,
                postCommitDealerId,
                cancellationToken);
        }

        if (postCommitPointsEvent is not null)
        {
            await _notificationPublisher.PublishLoyaltyPointsUpdatedAsync(
                postCommitPointsEvent,
                callerUserId,
                cancellationToken);
        }

        return createdDto;
    }

    public async Task<PagedResult<ServiceReviewDto>> ListMyServiceReviewsAsync(
        ServiceReviewListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Reviews.ViewOwn))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view personal CSAT service reviews.",
                "FORBIDDEN_PERMISSION");
        }

        var reviewsQuery = _dbContext.ServiceReviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.CustomerId == callerUserId);

        if (query.DealerId is { } dealerId && dealerId != Guid.Empty)
        {
            reviewsQuery = reviewsQuery.Where(r => r.DealerId == dealerId);
        }

        if (query.Rating.HasValue)
        {
            reviewsQuery = reviewsQuery.Where(r => r.Rating == query.Rating.Value);
        }

        if (query.MinRating.HasValue)
        {
            reviewsQuery = reviewsQuery.Where(r => r.Rating >= query.MinRating.Value);
        }

        if (query.UnansweredOnly == true)
        {
            reviewsQuery = reviewsQuery.Where(r => r.StaffResponse == null || r.StaffResponse == "");
        }

        return await ExecutePagedReviewProjectionAsync(reviewsQuery, query, cancellationToken);
    }

    public async Task<ServiceReviewPageDto> ListDealerServiceReviewsAsync(
        ServiceReviewListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Reviews.ViewDealer))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view dealership CSAT service reviews.",
                "FORBIDDEN_PERMISSION");
        }

        // Global IDealerScoped query filter is automatically active for DealerStaff.
        IQueryable<ServiceReview> dealerScopedQuery = _dbContext.ServiceReviews.AsNoTracking();

        if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (query.DealerId is { } requestedDealerId &&
                requestedDealerId != Guid.Empty &&
                requestedDealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff is strictly prohibited from viewing CSAT reviews belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            dealerScopedQuery = dealerScopedQuery.Where(r => r.DealerId == staffDealerId);
        }
        else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
        {
            dealerScopedQuery = dealerScopedQuery.Where(r => r.DealerId == adminDealerId);
        }

        var summarySnapshot = await dealerScopedQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalReviewsCount = g.Count(),
                RatingSum = g.Sum(x => x.Rating),
                FiveStarCount = g.Count(x => x.Rating == 5),
                UnansweredCount = g.Count(x => x.StaffResponse == null || x.StaffResponse == ""),
                TotalBonusPointsAwarded = g.Sum(x => x.BonusPointsAwarded)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalBranchReviews = summarySnapshot?.TotalReviewsCount ?? 0;
        var averageRating = totalBranchReviews > 0
            ? Math.Round((decimal)summarySnapshot!.RatingSum / totalBranchReviews, 2, MidpointRounding.AwayFromZero)
            : 0.00m;

        var filteredQuery = dealerScopedQuery;
        if (query.Rating.HasValue)
        {
            filteredQuery = filteredQuery.Where(r => r.Rating == query.Rating.Value);
        }

        if (query.MinRating.HasValue)
        {
            filteredQuery = filteredQuery.Where(r => r.Rating >= query.MinRating.Value);
        }

        if (query.UnansweredOnly == true)
        {
            filteredQuery = filteredQuery.Where(r => r.StaffResponse == null || r.StaffResponse == "");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            filteredQuery = filteredQuery.Where(r =>
                EF.Functions.Like(r.ReviewNumber, pattern) ||
                (r.Comment != null && EF.Functions.Like(r.Comment, pattern)) ||
                (r.ServiceInvoice != null && EF.Functions.Like(r.ServiceInvoice.InvoiceNumber, pattern)) ||
                (r.CustomerVehicle != null && EF.Functions.Like(r.CustomerVehicle.VIN, pattern)) ||
                (r.Customer != null && (
                    EF.Functions.Like(r.Customer.FullName, pattern) ||
                    EF.Functions.Like(r.Customer.Email, pattern))));
        }

        var paged = await ExecutePagedReviewProjectionAsync(filteredQuery, query, cancellationToken);

        return new ServiceReviewPageDto(
            Items: paged.Items,
            Pagination: paged.Pagination,
            AverageRating: averageRating,
            TotalReviewsCount: totalBranchReviews,
            FiveStarCount: summarySnapshot?.FiveStarCount ?? 0,
            UnansweredCount: summarySnapshot?.UnansweredCount ?? 0,
            TotalBonusPointsAwarded: summarySnapshot?.TotalBonusPointsAwarded ?? 0);
    }

    public async Task<ServiceReviewDto?> GetServiceReviewByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewOwn = _currentUserContext.HasPermission(Permissions.Reviews.ViewOwn);
        var canViewDealer = _currentUserContext.HasPermission(Permissions.Reviews.ViewDealer);

        if (!canViewOwn && !canViewDealer)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view workshop CSAT service reviews.",
                "FORBIDDEN_PERMISSION");
        }

        var invoice = await _dbContext.ServiceInvoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.ServiceBooking)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw NotFoundException.ForEntity("ServiceInvoice", invoiceId);

        var ownerCustomerId = invoice.ServiceBooking?.CustomerId ?? invoice.CustomerId;
        if (!canViewDealer)
        {
            if (ownerCustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only inspect CSAT reviews for their own workshop service invoices.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (invoice.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only inspect CSAT reviews belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        var review = await ApplyReviewReadIncludes(
                _dbContext.ServiceReviews
                    .IgnoreQueryFilters()
                    .AsNoTracking())
            .FirstOrDefaultAsync(r => r.ServiceInvoiceId == invoiceId, cancellationToken);

        if (review is null)
        {
            return null;
        }

        return MapLoadedReviewToDto(review);
    }

    public async Task<ServiceReviewDto> RespondToServiceReviewAsync(
        Guid reviewId,
        RespondToServiceReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _respondValidator.ValidateAndThrowAsync(request, cancellationToken);

        var staffUserId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.Reviews.RespondDealer))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to post official dealership responses to CSAT reviews.",
                "FORBIDDEN_PERMISSION");
        }

        Guid? staffDealerId = null;
        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
        }

        var expectedRowVersion = RowVersionCodec.DecodeRequiredOrThrow(request.RowVersion);
        var effectiveStaffResponse = request.ResolveEffectiveStaffResponse();
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

                var utcNow = _dateTimeProvider.UtcNow;

                var review = await _dbContext.ServiceReviews
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.Id == reviewId, ct)
                    ?? throw NotFoundException.ForEntity("ServiceReview", reviewId);

                if (staffDealerId.HasValue && review.DealerId != staffDealerId.Value)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff cannot respond to CSAT reviews belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                var dealer = await _dbContext.Dealers
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == review.DealerId, ct);

                if (dealer is null || dealer.IsDeleted || !dealer.IsActive)
                {
                    throw new BusinessRuleViolationException(
                        "DEALER_INACTIVE",
                        "Cannot respond to CSAT reviews for an inactive dealership.");
                }

                if (!review.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                {
                    throw new ConflictException(
                        "The CSAT service review was modified concurrently by another user. Please refresh and retry.",
                        "CONCURRENCY_CONFLICT");
                }

                _dbContext.Entry(review).Property(r => r.RowVersion).OriginalValue = expectedRowVersion;

                review.StaffResponse = effectiveStaffResponse;
                review.RespondedByUserId = staffUserId;
                review.RespondedAtUtc = utcNow;
                review.UpdatedAtUtc = utcNow;

                var invoice = await _dbContext.ServiceInvoices
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == review.ServiceInvoiceId, ct);

                var booking = await _dbContext.ServiceBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == review.ServiceBookingId, ct);

                var customer = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == review.CustomerId, ct);

                var vehicle = await _dbContext.CustomerVehicles
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(v => v.MotorcycleModel)
                    .FirstOrDefaultAsync(v => v.Id == review.CustomerVehicleId, ct);

                var respondedByUser = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == staffUserId, ct);

                var notifId = Guid.CreateVersion7();
                var payloadJson = JsonSerializer.Serialize(new
                {
                    notificationId = notifId,
                    eventType = nameof(NotificationEventType.ServiceReviewSubmitted),
                    reviewId = review.Id,
                    reviewNumber = review.ReviewNumber,
                    invoiceNumber = invoice?.InvoiceNumber ?? string.Empty,
                    dealerId = dealer.Id,
                    dealerName = dealer.Name,
                    staffResponse = effectiveStaffResponse,
                    respondedAtUtc = utcNow
                });

                _dbContext.Notifications.Add(new Notification(notifId)
                {
                    RecipientUserId = review.CustomerId,
                    EventType = nameof(NotificationEventType.ServiceReviewSubmitted),
                    Title = $"Dealership Replied to Review {review.ReviewNumber}",
                    Message = $"{dealer.Name} responded to your {review.Rating}-star CSAT review on invoice {invoice?.InvoiceNumber ?? review.ReviewNumber}.",
                    PayloadJson = payloadJson,
                    IsRead = false,
                    CreatedAtUtc = utcNow
                });

                await _dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapToDto(
                    review,
                    invoice,
                    booking,
                    customer,
                    dealer,
                    vehicle,
                    respondedByUser,
                    availablePointsAfter: null,
                    lifetimeQualifyingPointsAfter: null);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Concurrent update detected while saving dealership response to CSAT review. Please refresh and retry.",
                "CONCURRENCY_CONFLICT");
        }
    }

    private static async Task<PagedResult<ServiceReviewDto>> ExecutePagedReviewProjectionAsync(
        IQueryable<ServiceReview> filteredQuery,
        PaginationQuery query,
        CancellationToken cancellationToken)
    {
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<ServiceReviewDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var rows = await ApplyReviewReadIncludes(filteredQuery)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapLoadedReviewToDto).ToList();

        return PagedResult<ServiceReviewDto>.Create(
            items,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    private static ServiceReviewDto MapLoadedReviewToDto(ServiceReview review) =>
        MapToDto(
            review,
            review.ServiceInvoice,
            review.ServiceBooking,
            review.Customer,
            review.Dealer,
            review.CustomerVehicle,
            review.RespondedByUser,
            availablePointsAfter: null,
            lifetimeQualifyingPointsAfter: null);

    private static IQueryable<ServiceReview> ApplyReviewReadIncludes(IQueryable<ServiceReview> query) =>
        query
            .Include(r => r.ServiceInvoice)
            .Include(r => r.ServiceBooking)
            .Include(r => r.Customer)
            .Include(r => r.Dealer)
            .Include(r => r.CustomerVehicle)
                .ThenInclude(v => v!.MotorcycleModel)
            .Include(r => r.RespondedByUser);

    private static string GenerateReviewNumber(DateTimeOffset utcNow, Guid reviewId) =>
        VoucherAndDocumentNumberHelper.FormatDocumentNumber("REV", utcNow, reviewId);

    private static ServiceReviewDto MapToDto(
        ServiceReview review,
        ServiceInvoice? invoice,
        ServiceBooking? booking,
        User? customer,
        Dealer? dealer,
        CustomerVehicle? vehicle,
        User? respondedByUser,
        int? availablePointsAfter,
        int? lifetimeQualifyingPointsAfter) =>
        new(
            Id: review.Id,
            ReviewId: review.Id,
            ReviewNumber: review.ReviewNumber,
            ServiceInvoiceId: review.ServiceInvoiceId,
            InvoiceNumber: invoice?.InvoiceNumber ?? string.Empty,
            ServiceBookingId: review.ServiceBookingId,
            BookingNumber: booking?.BookingNumber ?? string.Empty,
            ServiceType: booking?.ServiceType ?? string.Empty,
            CustomerId: review.CustomerId,
            CustomerFullName: customer?.FullName ?? string.Empty,
            CustomerEmail: customer?.Email ?? string.Empty,
            DealerId: review.DealerId,
            DealerCode: dealer?.Code ?? string.Empty,
            DealerName: dealer?.Name ?? string.Empty,
            DealerCity: dealer?.City ?? string.Empty,
            CustomerVehicleId: review.CustomerVehicleId,
            MotorcycleModelId: vehicle?.MotorcycleModelId ?? Guid.Empty,
            Vin: vehicle?.VIN ?? string.Empty,
            MotorcycleModelName: vehicle?.MotorcycleModel?.ModelName ?? string.Empty,
            LicensePlate: vehicle?.LicensePlate,
            Rating: review.Rating,
            QualityTags: review.QualityTags,
            QualityTagList: ServiceReviewValidationRules.SplitStoredQualityTags(review.QualityTags),
            Comment: review.Comment,
            BonusPointsAwarded: review.BonusPointsAwarded,
            AvailablePointsAfter: availablePointsAfter,
            LifetimeQualifyingPointsAfter: lifetimeQualifyingPointsAfter,
            StaffResponse: review.StaffResponse,
            RespondedByUserId: review.RespondedByUserId,
            RespondedByStaffName: respondedByUser?.FullName,
            RespondedAtUtc: review.RespondedAtUtc,
            IsAnswered: !string.IsNullOrWhiteSpace(review.StaffResponse),
            RowVersion: RowVersionCodec.Encode(review.RowVersion),
            CreatedAtUtc: review.CreatedAtUtc,
            UpdatedAtUtc: review.UpdatedAtUtc);
}
