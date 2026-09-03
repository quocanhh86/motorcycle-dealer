using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

public sealed class TestDriveService : ITestDriveService
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IValidator<TestDriveListQuery> _listQueryValidator;
    private readonly IValidator<CreateTestDriveRequest> _createValidator;
    private readonly IValidator<CancelTestDriveRequest> _cancelValidator;
    private readonly IValidator<UpdateTestDriveStatusRequest> _updateStatusValidator;

    public TestDriveService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IValidator<TestDriveListQuery> listQueryValidator,
        IValidator<CreateTestDriveRequest> createValidator,
        IValidator<CancelTestDriveRequest> cancelValidator,
        IValidator<UpdateTestDriveStatusRequest> updateStatusValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _listQueryValidator = listQueryValidator ?? throw new ArgumentNullException(nameof(listQueryValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _cancelValidator = cancelValidator ?? throw new ArgumentNullException(nameof(cancelValidator));
        _updateStatusValidator = updateStatusValidator ?? throw new ArgumentNullException(nameof(updateStatusValidator));
    }

    public async Task<TestDriveBookingDto> CreateTestDriveAsync(
        CreateTestDriveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customerId = _currentUserContext.RequireUserId();
        if (!_currentUserContext.IsCustomer || !_currentUserContext.HasPermission(Permissions.TestDrives.Create))
        {
            throw new ForbiddenAccessException(
                "Only authenticated Customer accounts may schedule showroom test-drives.",
                "FORBIDDEN_PERMISSION");
        }

        var targetModelId = request.ResolveEffectiveModelId();
        DealerValidationPatterns.TryParseWorkshopTime(request.StartTime, out var startTime);
        DealerValidationPatterns.TryParseWorkshopTime(request.EndTime, out var endTime);

        var normalizedLicense = request.DriverLicenseNumber.Trim().ToUpperInvariant();
        var normalizedPhone = request.ContactPhone.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.CustomerNotes)
            ? null
            : request.CustomerNotes.Trim();

        var vietnamToday = TestDriveValidationRules.GetVietnamToday(_dateTimeProvider.UtcNow);
        if (request.PreferredDate < vietnamToday)
        {
            throw new BusinessRuleViolationException(
                "PAST_TEST_DRIVE_DATE",
                $"Preferred test-drive date ({request.PreferredDate:yyyy-MM-dd}) cannot be earlier than today in Asia/Ho_Chi_Minh ({vietnamToday:yyyy-MM-dd}).");
        }

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (createdDto, createdEvent, targetDealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            if (_dbContext.Database.IsRelational())
            {
                // Acquire deterministic row-level update locks (Users -> Dealers) to serialize concurrent
                // customer overlap checks and (Dealer, Model, PreferredDate, StartTime) capacity checks with zero deadlocks.
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT [Id] FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {customerId};",
                    ct);
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT [Id] FROM [dbo].[Dealers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {request.DealerId};",
                    ct);
            }

            var utcNow = _dateTimeProvider.UtcNow;

            var customer = await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == customerId && !u.IsDeleted && u.IsActive, ct)
                ?? throw new UnauthorizedAccessException("Authenticated customer profile is missing or inactive.");

            var dealer = await _dbContext.Dealers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DealerId, ct)
                ?? throw NotFoundException.ForEntity("Dealer", request.DealerId);

            if (dealer.IsDeleted || !dealer.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "DEALER_INACTIVE",
                    $"Dealership '{dealer.Name}' ({dealer.Code}) is currently inactive and cannot accept test-drive bookings.");
            }

            var model = await _dbContext.MotorcycleModels
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == targetModelId, ct)
                ?? throw NotFoundException.ForEntity("MotorcycleModel", targetModelId);

            if (model.IsDeleted || !model.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "MODEL_INACTIVE",
                    $"Motorcycle model '{model.ModelName}' ({model.ModelCode}) is currently inactive and unavailable for test-drives.");
            }

            var hasCustomerOverlap = await _dbContext.TestDriveBookings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    t => t.CustomerId == customerId &&
                         t.PreferredDate == request.PreferredDate &&
                         (t.Status == TestDriveStatus.Requested || t.Status == TestDriveStatus.Confirmed) &&
                         t.StartTime < endTime &&
                         startTime < t.EndTime,
                    ct);

            if (hasCustomerOverlap)
            {
                throw new ConflictException(
                    $"You already have an active test-drive scheduled on {request.PreferredDate:yyyy-MM-dd} overlapping {startTime:HH\\:mm}–{endTime:HH\\:mm}.",
                    "TEST_DRIVE_OVERLAP");
            }

            var activeSlotBookingsCount = await _dbContext.TestDriveBookings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(
                    t => t.DealerId == dealer.Id &&
                         t.MotorcycleModelId == model.Id &&
                         t.PreferredDate == request.PreferredDate &&
                         (t.Status == TestDriveStatus.Requested || t.Status == TestDriveStatus.Confirmed) &&
                         t.StartTime < endTime &&
                         startTime < t.EndTime,
                    ct);

            if (activeSlotBookingsCount >= TestDriveValidationRules.MaxActiveBookingsPerModelSlot)
            {
                throw new ConflictException(
                    $"Selected test-drive slot ({request.PreferredDate:yyyy-MM-dd} {startTime:HH\\:mm}–{endTime:HH\\:mm}) for '{model.ModelName}' at '{dealer.Name}' is full (max {TestDriveValidationRules.MaxActiveBookingsPerModelSlot} active riders).",
                    "TEST_DRIVE_SLOT_FULL");
            }

            var bookingId = Guid.CreateVersion7();
            var bookingNumber = GenerateTestDriveBookingNumber(utcNow, bookingId);
            var testDrive = new TestDriveBooking(bookingId)
            {
                BookingNumber = bookingNumber,
                CustomerId = customer.Id,
                DealerId = dealer.Id,
                MotorcycleModelId = model.Id,
                AssignedInventoryId = null,
                PreferredDate = request.PreferredDate,
                StartTime = startTime,
                EndTime = endTime,
                DriverLicenseNumber = normalizedLicense,
                ContactPhone = normalizedPhone,
                Status = TestDriveStatus.Requested,
                CustomerNotes = normalizedNotes,
                CreatedAtUtc = utcNow
            };

            var dealerNotifId = Guid.CreateVersion7();
            var realtimeEvent = new TestDriveCreatedRealtimeEvent(
                NotificationId: dealerNotifId,
                EventType: "TestDriveCreated",
                TestDriveId: testDrive.Id,
                BookingNumber: testDrive.BookingNumber,
                DealerId: dealer.Id,
                DealerName: dealer.Name,
                CustomerFullName: customer.FullName,
                MotorcycleModelName: model.ModelName,
                PreferredDate: testDrive.PreferredDate,
                StartTime: testDrive.StartTime,
                EndTime: testDrive.EndTime,
                Status: testDrive.Status.ToString(),
                OccurredAtUtc: utcNow);

            var payloadJson = JsonSerializer.Serialize(realtimeEvent);

            _dbContext.TestDriveBookings.Add(testDrive);

            _dbContext.Notifications.Add(new Notification(dealerNotifId)
            {
                TargetDealerId = dealer.Id,
                EventType = "TestDriveCreated",
                Title = $"New Test-Drive Request {testDrive.BookingNumber}",
                Message = $"{customer.FullName} requested a test-drive of {model.ModelName} on {testDrive.PreferredDate:yyyy-MM-dd} ({startTime:HH\\:mm}–{endTime:HH\\:mm}).",
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAtUtc = utcNow
            });

            _dbContext.Notifications.Add(new Notification(Guid.CreateVersion7())
            {
                RecipientUserId = customer.Id,
                EventType = "TestDriveCreated",
                Title = $"Test-Drive Requested — {model.ModelName}",
                Message = $"Your test-drive request {testDrive.BookingNumber} at {dealer.Name} on {testDrive.PreferredDate:yyyy-MM-dd} ({startTime:HH\\:mm}–{endTime:HH\\:mm}) has been submitted.",
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAtUtc = utcNow
            });

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (SqlExceptionGuards.TryGetUniqueViolationMessage(ex, out var sqlMsg))
            {
                if (sqlMsg.Contains("UQ_TestDriveBookings_BookingNumber", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException(
                        "A concurrent test-drive booking number collision occurred. Please retry.",
                        "CONCURRENCY_CONFLICT");
                }

                throw new ConflictException(
                    "A concurrent test-drive booking conflict occurred. Please retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await transaction.CommitAsync(ct);

            var dto = MapToDto(testDrive, customer, dealer, model, assignedInventory: null);
            return (dto, realtimeEvent, dealer.Id);
        }, cancellationToken);

        await _notificationPublisher.PublishTestDriveCreatedAsync(
            createdEvent,
            targetDealerId,
            cancellationToken);

        return createdDto;
    }

    public async Task<PagedResult<TestDriveBookingDto>> ListTestDrivesAsync(
        TestDriveListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();

        TestDriveStatus? statusFilter =
            !string.IsNullOrWhiteSpace(query.Status) &&
            TestDriveValidationRules.TryNormalizeStatus(query.Status, out var parsedStatus)
                ? parsedStatus
                : null;

        IQueryable<TestDriveBooking> bookingsQuery;

        if (forceOwnCustomerOnly)
        {
            if (!_currentUserContext.HasPermission(Permissions.TestDrives.ViewOwn))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view customer test-drive bookings.",
                    "FORBIDDEN_PERMISSION");
            }

            bookingsQuery = _dbContext.TestDriveBookings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.CustomerId == callerUserId);

            if (query.DealerId is { } custDealerId && custDealerId != Guid.Empty)
            {
                bookingsQuery = bookingsQuery.Where(t => t.DealerId == custDealerId);
            }
        }
        else
        {
            if (!_currentUserContext.HasPermission(Permissions.TestDrives.ViewDealer))
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to view dealership test-drive queues.",
                    "FORBIDDEN_PERMISSION");
            }

            // Global IDealerScoped query filter is automatically active for DealerStaff.
            bookingsQuery = _dbContext.TestDriveBookings.AsNoTracking();

            if (_currentUserContext.IsDealerStaff)
            {
                var staffDealerId = _currentUserContext.RequireAssignedDealerId();
                if (query.DealerId is { } requestedDealerId &&
                    requestedDealerId != Guid.Empty &&
                    requestedDealerId != staffDealerId)
                {
                    throw new ForbiddenAccessException(
                        "DealerStaff is strictly prohibited from viewing test-drive bookings belonging to another dealership.",
                        "FORBIDDEN_DEALER_SCOPE");
                }

                bookingsQuery = bookingsQuery.Where(t => t.DealerId == staffDealerId);
            }
            else if (query.DealerId is { } adminDealerId && adminDealerId != Guid.Empty)
            {
                bookingsQuery = bookingsQuery.Where(t => t.DealerId == adminDealerId);
            }
        }

        if (query.PreferredDate is { } preferredDate && preferredDate != default)
        {
            bookingsQuery = bookingsQuery.Where(t => t.PreferredDate == preferredDate);
        }

        if (statusFilter.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(t => t.Status == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{ShowroomRowVersionHelper.EscapeLikePattern(query.Search.Trim())}%";
            bookingsQuery = bookingsQuery.Where(t =>
                EF.Functions.Like(t.BookingNumber, pattern) ||
                EF.Functions.Like(t.DriverLicenseNumber, pattern) ||
                EF.Functions.Like(t.ContactPhone, pattern) ||
                (t.Customer != null && (
                    EF.Functions.Like(t.Customer.FullName, pattern) ||
                    EF.Functions.Like(t.Customer.Email, pattern))) ||
                (t.MotorcycleModel != null && (
                    EF.Functions.Like(t.MotorcycleModel.ModelName, pattern) ||
                    EF.Functions.Like(t.MotorcycleModel.ModelCode, pattern))) ||
                (t.AssignedInventory != null && EF.Functions.Like(t.AssignedInventory.VIN, pattern)));
        }

        var totalCount = await bookingsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return PagedResult<TestDriveBookingDto>.Create(
                [],
                query.NormalizedPageNumber,
                query.NormalizedPageSize,
                totalCount: 0);
        }

        var rows = await bookingsQuery
            .Include(t => t.Customer)
            .Include(t => t.Dealer)
            .Include(t => t.MotorcycleModel)
            .Include(t => t.AssignedInventory)
            .OrderByDescending(t => t.PreferredDate)
            .ThenBy(t => t.StartTime)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip(query.SkipCount)
            .Take(query.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(t => MapToDto(t, t.Customer, t.Dealer, t.MotorcycleModel, t.AssignedInventory))
            .ToList();

        return PagedResult<TestDriveBookingDto>.Create(
            items,
            query.NormalizedPageNumber,
            query.NormalizedPageSize,
            totalCount);
    }

    public async Task<TestDriveBookingDto> GetTestDriveByIdAsync(
        Guid testDriveId,
        CancellationToken cancellationToken = default)
    {
        var callerUserId = _currentUserContext.RequireUserId();
        var canViewOwn = _currentUserContext.HasPermission(Permissions.TestDrives.ViewOwn);
        var canViewDealer = _currentUserContext.HasPermission(Permissions.TestDrives.ViewDealer);

        if (!canViewOwn && !canViewDealer)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to view test-drive booking details.",
                "FORBIDDEN_PERMISSION");
        }

        var booking = await _dbContext.TestDriveBookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Dealer)
            .Include(t => t.MotorcycleModel)
            .Include(t => t.AssignedInventory)
            .FirstOrDefaultAsync(t => t.Id == testDriveId, cancellationToken)
            ?? throw NotFoundException.ForEntity("TestDriveBooking", testDriveId);

        if (!canViewDealer)
        {
            if (booking.CustomerId != callerUserId)
            {
                throw new ForbiddenAccessException(
                    "Customers may only view their own test-drive bookings.",
                    "FORBIDDEN_RESOURCE_OWNER");
            }
        }
        else if (_currentUserContext.IsDealerStaff)
        {
            var staffDealerId = _currentUserContext.RequireAssignedDealerId();
            if (booking.DealerId != staffDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff may only view test-drive bookings belonging to their assigned dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }

        return MapToDto(
            booking,
            booking.Customer,
            booking.Dealer,
            booking.MotorcycleModel,
            booking.AssignedInventory);
    }

    public async Task<TestDriveBookingDto> CancelTestDriveAsync(
        Guid testDriveId,
        CancelTestDriveRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveRequest = request ?? new CancelTestDriveRequest();
        await _cancelValidator.ValidateAndThrowAsync(effectiveRequest, cancellationToken);

        var callerUserId = _currentUserContext.RequireUserId();
        var canCancelOwn = _currentUserContext.HasPermission(Permissions.TestDrives.CancelOwn);
        var canUpdateStatus = _currentUserContext.HasPermission(Permissions.TestDrives.UpdateStatus);

        if (!canCancelOwn && !canUpdateStatus)
        {
            throw new ForbiddenAccessException(
                "You do not have permission to cancel test-drive bookings.",
                "FORBIDDEN_PERMISSION");
        }

        Guid? staffDealerId = null;
        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
        }

        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(effectiveRequest.RowVersion);
        var reason = effectiveRequest.ResolveEffectiveReason();
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (cancelledDto, statusEvent, customerId, dealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var utcNow = _dateTimeProvider.UtcNow;

            var ctx = await LoadTestDriveMutationContextAsync(testDriveId, ct)
                ?? throw NotFoundException.ForEntity("TestDriveBooking", testDriveId);
            var booking = ctx.Booking;

            if (!canUpdateStatus)
            {
                if (booking.CustomerId != callerUserId)
                {
                    throw new ForbiddenAccessException(
                        "Customers cannot cancel test-drive bookings belonging to another user.",
                        "FORBIDDEN_RESOURCE_OWNER");
                }
            }
            else if (staffDealerId.HasValue && booking.DealerId != staffDealerId.Value)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot cancel test-drive bookings belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            if (expectedRowVersion is not null)
            {
                if (!booking.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                {
                    throw new ConflictException(
                        "The test-drive booking was modified concurrently by another transaction. Please refresh and retry.",
                        "CONCURRENCY_CONFLICT");
                }

                _dbContext.Entry(booking).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
            }

            if (booking.Status is not (TestDriveStatus.Requested or TestDriveStatus.Confirmed))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_TEST_DRIVE_STATUS",
                    $"Cannot cancel test-drive '{booking.BookingNumber}' while in '{booking.Status}' status. Only Requested or Confirmed bookings can be cancelled.");
            }

            var previousStatus = booking.Status.ToString();
            booking.Status = TestDriveStatus.Cancelled;
            booking.CancelledAtUtc = utcNow;
            booking.CancellationReason = reason;
            booking.UpdatedAtUtc = utcNow;

            var dealerName = ctx.Dealer?.Name ?? "Authorized Dealership";
            var modelName = ctx.Model?.ModelName ?? "Motorcycle";
            var notifId = Guid.CreateVersion7();

            var realtimePayload = new TestDriveStatusChangedRealtimeEvent(
                NotificationId: notifId,
                EventType: "TestDriveStatusChanged",
                TestDriveId: booking.Id,
                BookingNumber: booking.BookingNumber,
                PreviousStatus: previousStatus,
                NewStatus: nameof(TestDriveStatus.Cancelled),
                DealerName: dealerName,
                MotorcycleModelName: modelName,
                AssignedVin: ctx.AssignedInventory?.VIN,
                OccurredAtUtc: utcNow);

            _dbContext.Notifications.Add(new Notification(notifId)
            {
                RecipientUserId = booking.CustomerId,
                TargetDealerId = booking.DealerId,
                EventType = "TestDriveStatusChanged",
                Title = $"Test-Drive {booking.BookingNumber} — Cancelled",
                Message = $"Test-drive {booking.BookingNumber} ({modelName}) at {dealerName} was cancelled: {reason}",
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
                    "Concurrent update detected while cancelling test-drive booking. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await transaction.CommitAsync(ct);

            var dto = MapToDto(booking, ctx.Customer, ctx.Dealer, ctx.Model, ctx.AssignedInventory);
            return (dto, realtimePayload, booking.CustomerId, booking.DealerId);
        }, cancellationToken);

        await _notificationPublisher.PublishTestDriveStatusChangedAsync(
            statusEvent,
            customerId,
            dealerId,
            cancellationToken);

        return cancelledDto;
    }

    public async Task<TestDriveBookingDto> UpdateTestDriveStatusAsync(
        Guid testDriveId,
        UpdateTestDriveStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = _currentUserContext.RequireUserId();
        if (!_currentUserContext.HasPermission(Permissions.TestDrives.UpdateStatus))
        {
            throw new ForbiddenAccessException(
                "You do not have permission to update test-drive booking status.",
                "FORBIDDEN_PERMISSION");
        }

        Guid? staffDealerId = null;
        if (_currentUserContext.IsDealerStaff)
        {
            staffDealerId = _currentUserContext.RequireAssignedDealerId();
        }

        TestDriveValidationRules.TryNormalizeStatus(request.ResolveEffectiveStatus(), out var targetStatus);
        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.RowVersion);
        var staffNotes = request.ResolveEffectiveStaffNotes();
        var cancellationReason = request.ResolveEffectiveCancellationReason();
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var (updatedDto, statusEvent, customerId, dealerId) = await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var utcNow = _dateTimeProvider.UtcNow;

            var ctx = await LoadTestDriveMutationContextAsync(testDriveId, ct)
                ?? throw NotFoundException.ForEntity("TestDriveBooking", testDriveId);
            var booking = ctx.Booking;

            if (staffDealerId.HasValue && booking.DealerId != staffDealerId.Value)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff cannot update test-drive bookings belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            if (ctx.Dealer is null || ctx.Dealer.IsDeleted || !ctx.Dealer.IsActive)
            {
                throw new BusinessRuleViolationException(
                    "DEALER_INACTIVE",
                    "Cannot update test-drive bookings for an inactive dealership.");
            }

            if (expectedRowVersion is not null)
            {
                if (!booking.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                {
                    throw new ConflictException(
                        "The test-drive booking was modified concurrently by another transaction. Please refresh and retry.",
                        "CONCURRENCY_CONFLICT");
                }

                _dbContext.Entry(booking).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
            }

            if (!TestDriveValidationRules.IsAllowedStaffTransition(booking.Status, targetStatus))
            {
                throw new BusinessRuleViolationException(
                    "INVALID_TEST_DRIVE_STATUS",
                    $"Illegal test-drive status transition from '{booking.Status}' to '{targetStatus}'.");
            }

            var resolvedAssignedInventory = ctx.AssignedInventory;
            if (request.AssignedInventoryId is { } requestedInvId && requestedInvId != Guid.Empty)
            {
                if (targetStatus is not (TestDriveStatus.Confirmed or TestDriveStatus.Completed))
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_DEMO_VEHICLE",
                        "A demo vehicle VIN can only be assigned when confirming or completing a test-drive.");
                }

                if (_dbContext.Database.IsRelational())
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT [Id] FROM [dbo].[DealerInventories] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {requestedInvId};",
                        ct);
                }

                var candidateInventory = await _dbContext.DealerInventories
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == requestedInvId, ct);

                var isPreservingAlreadyAssignedOnCompletion =
                    targetStatus == TestDriveStatus.Completed &&
                    booking.AssignedInventoryId == requestedInvId;

                if (candidateInventory is null ||
                    candidateInventory.DealerId != booking.DealerId ||
                    candidateInventory.MotorcycleModelId != booking.MotorcycleModelId ||
                    (!isPreservingAlreadyAssignedOnCompletion &&
                     !string.Equals(candidateInventory.StockStatus, nameof(StockStatus.Available), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new BusinessRuleViolationException(
                        "INVALID_DEMO_VEHICLE",
                        "Assigned demo vehicle must belong to the same dealership and motorcycle model and must be in Available stock status.");
                }

                booking.AssignedInventoryId = candidateInventory.Id;
                resolvedAssignedInventory = candidateInventory;
            }

            if (targetStatus == TestDriveStatus.Confirmed && resolvedAssignedInventory is not null)
            {
                var hasDemoVinTimeConflict = await _dbContext.TestDriveBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        t => t.Id != booking.Id &&
                             t.AssignedInventoryId == resolvedAssignedInventory.Id &&
                             t.PreferredDate == booking.PreferredDate &&
                             t.Status == TestDriveStatus.Confirmed &&
                             t.StartTime < booking.EndTime &&
                             booking.StartTime < t.EndTime,
                        ct);

                if (hasDemoVinTimeConflict)
                {
                    throw new ConflictException(
                        $"Demo vehicle VIN '{resolvedAssignedInventory.VIN}' is already assigned to another confirmed test-drive on {booking.PreferredDate:yyyy-MM-dd} overlapping {booking.StartTime:HH\\:mm}–{booking.EndTime:HH\\:mm}.",
                        "DEMO_VIN_TIME_CONFLICT");
                }
            }

            var previousStatus = booking.Status.ToString();
            booking.Status = targetStatus;
            booking.UpdatedAtUtc = utcNow;

            if (staffNotes is not null)
            {
                booking.StaffNotes = staffNotes;
            }

            switch (targetStatus)
            {
                case TestDriveStatus.Confirmed:
                    booking.ConfirmedAtUtc ??= utcNow;
                    break;

                case TestDriveStatus.Completed:
                    booking.ConfirmedAtUtc ??= utcNow;
                    booking.CompletedAtUtc = utcNow;
                    break;

                case TestDriveStatus.Cancelled:
                    booking.CancelledAtUtc = utcNow;
                    booking.CancellationReason = cancellationReason ?? "Cancelled by dealership staff.";
                    break;

                case TestDriveStatus.NoShow:
                    booking.CancellationReason = cancellationReason ?? "Marked as NoShow by dealership staff.";
                    break;
            }

            var dealerName = ctx.Dealer.Name;
            var modelName = ctx.Model?.ModelName ?? "Motorcycle";
            var notifId = Guid.CreateVersion7();

            var realtimePayload = new TestDriveStatusChangedRealtimeEvent(
                NotificationId: notifId,
                EventType: "TestDriveStatusChanged",
                TestDriveId: booking.Id,
                BookingNumber: booking.BookingNumber,
                PreviousStatus: previousStatus,
                NewStatus: targetStatus.ToString(),
                DealerName: dealerName,
                MotorcycleModelName: modelName,
                AssignedVin: resolvedAssignedInventory?.VIN,
                OccurredAtUtc: utcNow);

            _dbContext.Notifications.Add(new Notification(notifId)
            {
                RecipientUserId = booking.CustomerId,
                TargetDealerId = booking.DealerId,
                EventType = "TestDriveStatusChanged",
                Title = $"Test-Drive {booking.BookingNumber} — {targetStatus}",
                Message = resolvedAssignedInventory is not null
                    ? $"Your {modelName} test-drive ({booking.BookingNumber}) at {dealerName} is now {targetStatus} (Demo VIN: {resolvedAssignedInventory.VIN})."
                    : $"Your {modelName} test-drive ({booking.BookingNumber}) at {dealerName} changed from {previousStatus} to {targetStatus}.",
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
                    "Concurrent update detected while updating test-drive status. Please refresh and retry.",
                    "CONCURRENCY_CONFLICT");
            }

            await transaction.CommitAsync(ct);

            var dto = MapToDto(booking, ctx.Customer, ctx.Dealer, ctx.Model, resolvedAssignedInventory);
            return (dto, realtimePayload, booking.CustomerId, booking.DealerId);
        }, cancellationToken);

        await _notificationPublisher.PublishTestDriveStatusChangedAsync(
            statusEvent,
            customerId,
            dealerId,
            cancellationToken);

        return updatedDto;
    }

    private sealed record LoadedTestDriveMutationContext(
        TestDriveBooking Booking,
        User? Customer,
        Dealer? Dealer,
        MotorcycleModel? Model,
        DealerInventory? AssignedInventory);

    private async Task<LoadedTestDriveMutationContext?> LoadTestDriveMutationContextAsync(
        Guid testDriveId,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.TestDriveBookings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == testDriveId, cancellationToken);

        if (booking is null)
        {
            return null;
        }

        var customer = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == booking.CustomerId, cancellationToken);

        var dealer = await _dbContext.Dealers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == booking.DealerId, cancellationToken);

        var model = await _dbContext.MotorcycleModels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == booking.MotorcycleModelId, cancellationToken);

        DealerInventory? assignedInventory = null;
        if (booking.AssignedInventoryId.HasValue)
        {
            assignedInventory = await _dbContext.DealerInventories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == booking.AssignedInventoryId.Value, cancellationToken);
        }

        return new LoadedTestDriveMutationContext(booking, customer, dealer, model, assignedInventory);
    }

    private static string GenerateTestDriveBookingNumber(DateTimeOffset utcNow, Guid bookingId) =>
        VoucherAndDocumentNumberHelper.FormatDocumentNumber("TD", utcNow, bookingId);

    private static TestDriveBookingDto MapToDto(
        TestDriveBooking booking,
        User? customer,
        Dealer? dealer,
        MotorcycleModel? model,
        DealerInventory? assignedInventory) =>
        new(
            Id: booking.Id,
            TestDriveId: booking.Id,
            BookingNumber: booking.BookingNumber,
            Status: booking.Status.ToString(),
            CustomerId: booking.CustomerId,
            CustomerFullName: customer?.FullName ?? string.Empty,
            CustomerEmail: customer?.Email ?? string.Empty,
            DealerId: booking.DealerId,
            DealerCode: dealer?.Code ?? string.Empty,
            DealerName: dealer?.Name ?? string.Empty,
            DealerCity: dealer?.City ?? string.Empty,
            MotorcycleModelId: booking.MotorcycleModelId,
            MotorcycleModelCode: model?.ModelCode ?? string.Empty,
            MotorcycleModelName: model?.ModelName ?? string.Empty,
            AssignedInventoryId: booking.AssignedInventoryId,
            AssignedVin: assignedInventory?.VIN,
            AssignedColorName: assignedInventory?.ColorName,
            PreferredDate: booking.PreferredDate,
            StartTime: booking.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            EndTime: booking.EndTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            DriverLicenseNumber: booking.DriverLicenseNumber,
            ContactPhone: booking.ContactPhone,
            CustomerNotes: booking.CustomerNotes,
            StaffNotes: booking.StaffNotes,
            CancellationReason: booking.CancellationReason,
            ConfirmedAtUtc: booking.ConfirmedAtUtc,
            CompletedAtUtc: booking.CompletedAtUtc,
            CancelledAtUtc: booking.CancelledAtUtc,
            RowVersion: RowVersionCodec.Encode(booking.RowVersion),
            CreatedAtUtc: booking.CreatedAtUtc,
            UpdatedAtUtc: booking.UpdatedAtUtc);
}
