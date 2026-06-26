namespace MotorcycleDealer.Application.Features.TimeSlots;

public sealed record ServiceTimeSlotDto(
    Guid Id,
    Guid SlotId,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    DateOnly SlotDate,
    string StartTime,
    string EndTime,
    int MaxCapacity,
    int BookedCount,
    int RemainingCapacity,
    int AvailableBays,
    bool IsBlocked,
    bool IsAvailable,
    string RowVersion);

public sealed record TimeSlotQuery(
    Guid? DealerId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    DateOnly? SlotDate = null,
    bool? AvailableOnly = null);

public sealed record CreateTimeSlotRequest
{
    public Guid? DealerId { get; init; }
    public DateOnly SlotDate { get; init; }
    public string StartTime { get; init; } = "08:00:00";
    public string EndTime { get; init; } = "09:00:00";
    public int MaxCapacity { get; init; } = 4;
    public bool IsBlocked { get; init; }
}

public sealed record GenerateTimeSlotsRequest
{
    public Guid? DealerId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public int SlotDurationMinutes { get; init; } = 60;
    public int? MaxCapacityPerSlot { get; init; }
    public int? MaxCapacity { get; init; }
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }

    public DateOnly ResolveEffectiveToDate() => ToDate ?? FromDate;

    public int? ResolveRequestedCapacity() => MaxCapacityPerSlot ?? MaxCapacity;
}

public sealed record UpdateTimeSlotRequest
{
    public int? MaxCapacity { get; init; }
    public bool? IsBlocked { get; init; }
    public string? RowVersion { get; init; }
}

public interface ITimeSlotService
{
    Task<IReadOnlyList<ServiceTimeSlotDto>> ListTimeSlotsAsync(
        TimeSlotQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceTimeSlotDto>> ListDealerTimeSlotsAsync(
        Guid? dealerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<ServiceTimeSlotDto> CreateTimeSlotAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceTimeSlotDto>> GenerateTimeSlotsAsync(
        GenerateTimeSlotsRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceTimeSlotDto> UpdateTimeSlotAsync(
        Guid slotId,
        UpdateTimeSlotRequest request,
        CancellationToken cancellationToken = default);
}
