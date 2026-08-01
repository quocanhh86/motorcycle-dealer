using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Booking;

public sealed class TimeSlotService : ITimeSlotService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateTimeSlotRequest> _createValidator;
    private readonly IValidator<GenerateTimeSlotsRequest> _generateValidator;
    private readonly IValidator<UpdateTimeSlotRequest> _updateValidator;

    public TimeSlotService(
        MotorcycleDealerDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateTimeSlotRequest> createValidator,
        IValidator<GenerateTimeSlotsRequest> generateValidator,
        IValidator<UpdateTimeSlotRequest> updateValidator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _generateValidator = generateValidator ?? throw new ArgumentNullException(nameof(generateValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    public async Task<IReadOnlyList<ServiceTimeSlotDto>> ListTimeSlotsAsync(
        TimeSlotQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Guid? effectiveDealerId;
        if (_currentUserContext.IsDealerStaff)
        {
            var assignedDealerId = _currentUserContext.RequireAssignedDealerId();
            if (query.DealerId is { } requestedDealerId &&
                requestedDealerId != Guid.Empty &&
                requestedDealerId != assignedDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff is strictly prohibited from viewing workshop time slots belonging to another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            effectiveDealerId = assignedDealerId;
        }
        else if (_currentUserContext.IsAdmin)
        {
            effectiveDealerId = query.DealerId is { } id && id != Guid.Empty ? id : null;
        }
        else
        {
            if (query.DealerId is null || query.DealerId == Guid.Empty)
            {
                throw new ValidationException([
                    new ValidationFailure("dealerId", "Query parameter 'dealerId' is required.")
                ]);
            }

            effectiveDealerId = query.DealerId.Value;
        }

        if (effectiveDealerId.HasValue)
        {
            var dealer = await _dbContext.Dealers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == effectiveDealerId.Value, cancellationToken);

            if (dealer is null || (!_currentUserContext.IsAdmin && !_currentUserContext.IsDealerStaff && !dealer.IsActive))
            {
                throw NotFoundException.ForEntity("Dealer", effectiveDealerId.Value);
            }
        }

        var (fromDate, toDate) = ResolveDateWindow(query.FromDate, query.ToDate, query.SlotDate);

        var slotsQuery = _dbContext.ServiceTimeSlots
            .AsNoTracking()
            .Include(s => s.Dealer)
            .Where(s => s.SlotDate >= fromDate && s.SlotDate <= toDate);

        if (effectiveDealerId.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.DealerId == effectiveDealerId.Value);
        }

        if (query.AvailableOnly == true)
        {
            slotsQuery = slotsQuery.Where(s => !s.IsBlocked && s.BookedCount < s.MaxCapacity);
        }

        var slots = await slotsQuery
            .OrderBy(s => s.SlotDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return slots.Select(s => MapToDto(s, s.Dealer)).ToList();
    }

    public Task<IReadOnlyList<ServiceTimeSlotDto>> ListDealerTimeSlotsAsync(
        Guid? dealerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsDealerStaff && !_currentUserContext.IsAdmin)
        {
            throw new ForbiddenAccessException(
                "Only DealerStaff or Admin may access the dealership time-slot management schedule.",
                "FORBIDDEN_PERMISSION");
        }

        return ListTimeSlotsAsync(
            new TimeSlotQuery(
                DealerId: dealerId,
                FromDate: fromDate,
                ToDate: toDate,
                SlotDate: null,
                AvailableOnly: false),
            cancellationToken);
    }

    public async Task<ServiceTimeSlotDto> CreateTimeSlotAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var targetDealerId = ResolveManagedDealerId(request.DealerId);
        var dealer = await RequireActiveDealerAsync(targetDealerId, cancellationToken);

        DealerValidationPatterns.TryParseWorkshopTime(request.StartTime, out var startTime);
        DealerValidationPatterns.TryParseWorkshopTime(request.EndTime, out var endTime);

        var existingSlot = await _dbContext.ServiceTimeSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.DealerId == dealer.Id &&
                     s.SlotDate == request.SlotDate &&
                     s.StartTime == startTime,
                cancellationToken);

        if (existingSlot is not null)
        {
            return MapToDto(existingSlot, dealer);
        }

        var slot = new ServiceTimeSlot
        {
            DealerId = dealer.Id,
            SlotDate = request.SlotDate,
            StartTime = startTime,
            EndTime = endTime,
            MaxCapacity = request.MaxCapacity,
            BookedCount = 0,
            IsBlocked = request.IsBlocked,
            CreatedAtUtc = _dateTimeProvider.UtcNow
        };

        _dbContext.ServiceTimeSlots.Add(slot);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            _dbContext.ChangeTracker.Clear();
            var concurrentExisting = await _dbContext.ServiceTimeSlots
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.DealerId == dealer.Id &&
                         s.SlotDate == request.SlotDate &&
                         s.StartTime == startTime,
                    cancellationToken);

            if (concurrentExisting is not null)
            {
                return MapToDto(concurrentExisting, dealer);
            }

            throw new ConflictException(
                "A workshop time slot already exists for the specified dealership, date, and start time.",
                "DUPLICATE_TIME_SLOT");
        }

        return MapToDto(slot, dealer);
    }

    public async Task<IReadOnlyList<ServiceTimeSlotDto>> GenerateTimeSlotsAsync(
        GenerateTimeSlotsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _generateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var targetDealerId = ResolveManagedDealerId(request.DealerId);
        var dealer = await RequireActiveDealerAsync(targetDealerId, cancellationToken);

        var openTime = !string.IsNullOrWhiteSpace(request.OpeningTime) &&
                       DealerValidationPatterns.TryParseWorkshopTime(request.OpeningTime, out var parsedOpen)
            ? parsedOpen
            : dealer.OpeningTime;

        var closeTime = !string.IsNullOrWhiteSpace(request.ClosingTime) &&
                        DealerValidationPatterns.TryParseWorkshopTime(request.ClosingTime, out var parsedClose)
            ? parsedClose
            : dealer.ClosingTime;

        if (closeTime <= openTime)
        {
            throw new ValidationException([
                new ValidationFailure("closingTime", "Effective closing time must be strictly later than opening time.")
            ]);
        }

        var windows = BuildDailyWindows(openTime, closeTime, request.SlotDurationMinutes);
        if (windows.Count == 0)
        {
            throw new ValidationException([
                new ValidationFailure(
                    "slotDurationMinutes",
                    "No complete bay windows fit within the dealership's opening and closing hours for the requested duration.")
            ]);
        }

        var fromDate = request.FromDate;
        var toDate = request.ResolveEffectiveToDate();
        var capacity = request.ResolveRequestedCapacity() ?? dealer.DefaultSlotCapacity;

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async ct =>
        {
            _dbContext.ChangeTracker.Clear();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var existingKeys = await _dbContext.ServiceTimeSlots
                .IgnoreQueryFilters()
                .Where(s => s.DealerId == dealer.Id && s.SlotDate >= fromDate && s.SlotDate <= toDate)
                .Select(s => new { s.SlotDate, s.StartTime })
                .ToListAsync(ct);

            var occupiedSet = existingKeys
                .Select(k => (k.SlotDate, k.StartTime))
                .ToHashSet();

            var utcNow = _dateTimeProvider.UtcNow;
            var anyAdded = false;

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                foreach (var (start, end) in windows)
                {
                    if (occupiedSet.Add((date, start)))
                    {
                        _dbContext.ServiceTimeSlots.Add(new ServiceTimeSlot
                        {
                            DealerId = dealer.Id,
                            SlotDate = date,
                            StartTime = start,
                            EndTime = end,
                            MaxCapacity = capacity,
                            BookedCount = 0,
                            IsBlocked = false,
                            CreatedAtUtc = utcNow
                        });
                        anyAdded = true;
                    }
                }
            }

            if (anyAdded)
            {
                await _dbContext.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }, cancellationToken);

        var resultingSlots = await _dbContext.ServiceTimeSlots
            .AsNoTracking()
            .Where(s => s.DealerId == dealer.Id && s.SlotDate >= fromDate && s.SlotDate <= toDate)
            .OrderBy(s => s.SlotDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return resultingSlots.Select(s => MapToDto(s, dealer)).ToList();
    }

    public async Task<ServiceTimeSlotDto> UpdateTimeSlotAsync(
        Guid slotId,
        UpdateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (_currentUserContext.IsDealerStaff)
        {
            _ = _currentUserContext.RequireAssignedDealerId();
        }

        var slot = await _dbContext.ServiceTimeSlots
            .Include(s => s.Dealer)
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken)
            ?? throw NotFoundException.ForEntity("ServiceTimeSlot", slotId);

        var owningDealer = slot.Dealer ?? await _dbContext.Dealers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == slot.DealerId, cancellationToken);

        if (owningDealer is null || owningDealer.IsDeleted || !owningDealer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "DEALER_INACTIVE",
                $"Cannot modify workshop time slots for inactive dealership '{owningDealer?.Code ?? slot.DealerId.ToString()}'.");
        }

        var expectedRowVersion = RowVersionCodec.DecodeOptionalOrThrow(request.RowVersion);
        if (expectedRowVersion is not null)
        {
            if (!slot.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
            {
                throw new ConflictException(
                    "The workshop time slot was modified by another transaction. Please refresh and retry with the latest RowVersion.",
                    "CONCURRENCY_CONFLICT");
            }

            _dbContext.Entry(slot).Property(s => s.RowVersion).OriginalValue = expectedRowVersion;
        }

        if (request.MaxCapacity.HasValue)
        {
            if (request.MaxCapacity.Value < slot.BookedCount)
            {
                throw new ConflictException(
                    $"Cannot reduce MaxCapacity ({request.MaxCapacity.Value}) below current BookedCount ({slot.BookedCount}).",
                    "CAPACITY_BELOW_BOOKED");
            }

            slot.MaxCapacity = request.MaxCapacity.Value;
        }

        if (request.IsBlocked.HasValue)
        {
            slot.IsBlocked = request.IsBlocked.Value;
        }

        slot.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The workshop time slot was modified by another transaction. Please refresh and retry with the latest RowVersion.",
                "CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new ConflictException(
                "Cannot reduce MaxCapacity below current BookedCount.",
                "CAPACITY_BELOW_BOOKED");
        }

        return MapToDto(slot, slot.Dealer);
    }

    private Guid ResolveManagedDealerId(Guid? requestedDealerId)
    {
        if (_currentUserContext.IsDealerStaff)
        {
            var assignedDealerId = _currentUserContext.RequireAssignedDealerId();
            if (requestedDealerId is { } suppliedId &&
                suppliedId != Guid.Empty &&
                suppliedId != assignedDealerId)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff is strictly prohibited from managing workshop time slots for another dealership.",
                    "FORBIDDEN_DEALER_SCOPE");
            }

            return assignedDealerId;
        }

        if (_currentUserContext.IsAdmin)
        {
            if (requestedDealerId is null || requestedDealerId == Guid.Empty)
            {
                throw new ValidationException([
                    new ValidationFailure("dealerId", "DealerId is required when managing time slots as Admin.")
                ]);
            }

            return requestedDealerId.Value;
        }

        throw new ForbiddenAccessException(
            "Caller does not have permission to manage dealership time slots.",
            "FORBIDDEN_PERMISSION");
    }

    private async Task<Dealer> RequireActiveDealerAsync(Guid dealerId, CancellationToken cancellationToken)
    {
        var dealer = await _dbContext.Dealers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken);

        if (dealer is null || dealer.IsDeleted)
        {
            throw NotFoundException.ForEntity("Dealer", dealerId);
        }

        if (!dealer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "DEALER_INACTIVE",
                $"Cannot generate or modify time slots for inactive dealership '{dealer.Code}'.");
        }

        return dealer;
    }

    private (DateOnly FromDate, DateOnly ToDate) ResolveDateWindow(
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly? slotDate)
    {
        var vietnamToday = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.ToOffset(VietnamOffset).DateTime);

        DateOnly effectiveFrom;
        DateOnly effectiveTo;

        if (slotDate is { } singleDate && singleDate != default)
        {
            effectiveFrom = singleDate;
            effectiveTo = singleDate;
        }
        else if (fromDate is { } start && start != default)
        {
            effectiveFrom = start;
            effectiveTo = toDate is { } end && end != default ? end : start.AddDays(6);
        }
        else
        {
            effectiveFrom = vietnamToday;
            effectiveTo = toDate is { } end && end != default ? end : vietnamToday.AddDays(13);
        }

        if (effectiveTo < effectiveFrom)
        {
            throw new ValidationException([
                new ValidationFailure("toDate", "Query parameter 'toDate' must be on or after 'fromDate'.")
            ]);
        }

        return (effectiveFrom, effectiveTo);
    }

    private static List<(TimeOnly Start, TimeOnly End)> BuildDailyWindows(
        TimeOnly openTime,
        TimeOnly closeTime,
        int durationMinutes)
    {
        var startTotalMinutes = openTime.Hour * 60 + openTime.Minute;
        var closeTotalMinutes = closeTime.Hour * 60 + closeTime.Minute;

        var windows = new List<(TimeOnly Start, TimeOnly End)>();
        for (var cursor = startTotalMinutes; cursor + durationMinutes <= closeTotalMinutes; cursor += durationMinutes)
        {
            var slotStart = new TimeOnly(cursor / 60, cursor % 60);
            var slotEndMinutes = cursor + durationMinutes;
            var slotEnd = new TimeOnly(slotEndMinutes / 60, slotEndMinutes % 60);
            windows.Add((slotStart, slotEnd));
        }

        return windows;
    }

    internal static ServiceTimeSlotDto MapToDto(ServiceTimeSlot slot, Dealer? dealer)
    {
        var remaining = Math.Max(0, slot.MaxCapacity - slot.BookedCount);
        return new ServiceTimeSlotDto(
            Id: slot.Id,
            SlotId: slot.Id,
            DealerId: slot.DealerId,
            DealerCode: dealer?.Code ?? string.Empty,
            DealerName: dealer?.Name ?? string.Empty,
            SlotDate: slot.SlotDate,
            StartTime: slot.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            EndTime: slot.EndTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            MaxCapacity: slot.MaxCapacity,
            BookedCount: slot.BookedCount,
            RemainingCapacity: remaining,
            AvailableBays: remaining,
            IsBlocked: slot.IsBlocked,
            IsAvailable: !slot.IsBlocked && slot.BookedCount < slot.MaxCapacity,
            RowVersion: RowVersionCodec.Encode(slot.RowVersion));
    }
}
