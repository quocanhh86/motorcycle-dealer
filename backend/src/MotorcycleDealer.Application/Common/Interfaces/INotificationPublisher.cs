namespace MotorcycleDealer.Application.Common.Interfaces;

public sealed record BookingCreatedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid BookingId,
    string BookingNumber,
    Guid DealerId,
    string CustomerFullName,
    string Vin,
    string MotorcycleModelName,
    DateOnly SlotDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ServiceType,
    bool HasReservedVoucher,
    string Status,
    DateTimeOffset OccurredAtUtc);

public sealed record BookingStatusChangedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid BookingId,
    string BookingNumber,
    string PreviousStatus,
    string NewStatus,
    string DealerName,
    DateTimeOffset OccurredAtUtc);

public sealed record MaintenanceReminderRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid ReminderId,
    Guid CustomerVehicleId,
    string Vin,
    string MotorcycleModelName,
    string MilestoneName,
    int CurrentMileageKm,
    int TargetMileageKm,
    DateTimeOffset DueDateUtc,
    string DeduplicationKey,
    DateTimeOffset OccurredAtUtc);

public sealed record LoyaltyPointsUpdatedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid LoyaltyAccountId,
    string TransactionType,
    int PointsDelta,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    string ReferenceType,
    string ReferenceNumber,
    DateTimeOffset OccurredAtUtc);

public sealed record TierUpgradedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid LoyaltyAccountId,
    string PreviousTierCode,
    string NewTierCode,
    decimal NewPointsMultiplier,
    int LifetimeQualifyingPoints,
    DateTimeOffset OccurredAtUtc);

public sealed record TestDriveCreatedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid TestDriveId,
    string BookingNumber,
    Guid DealerId,
    string DealerName,
    string CustomerFullName,
    string MotorcycleModelName,
    DateOnly PreferredDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    DateTimeOffset OccurredAtUtc);

public sealed record TestDriveStatusChangedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid TestDriveId,
    string BookingNumber,
    string PreviousStatus,
    string NewStatus,
    string DealerName,
    string MotorcycleModelName,
    string? AssignedVin,
    DateTimeOffset OccurredAtUtc);

public sealed record ServiceReviewSubmittedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid ReviewId,
    string ReviewNumber,
    Guid ServiceInvoiceId,
    string InvoiceNumber,
    Guid DealerId,
    string DealerName,
    string CustomerFullName,
    string MotorcycleModelName,
    int Rating,
    int BonusPointsAwarded,
    DateTimeOffset OccurredAtUtc);

public sealed record WarrantyClaimCreatedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid ClaimId,
    string ClaimNumber,
    Guid DealerId,
    string DealerName,
    string CustomerFullName,
    string Vin,
    string MotorcycleModelName,
    string DefectCategory,
    int OdometerAtClaimKm,
    string Status,
    DateTimeOffset OccurredAtUtc);

public sealed record WarrantyClaimStatusChangedRealtimeEvent(
    Guid NotificationId,
    string EventType,
    Guid ClaimId,
    string ClaimNumber,
    string PreviousStatus,
    string NewStatus,
    string DealerName,
    string Vin,
    decimal CoveredAmountVnd,
    DateTimeOffset OccurredAtUtc);

public interface INotificationPublisher
{
    Task PublishBookingCreatedAsync(
        BookingCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default);

    Task PublishBookingStatusChangedAsync(
        BookingStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default);

    Task PublishMaintenanceReminderAsync(
        MaintenanceReminderRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task PublishLoyaltyPointsUpdatedAsync(
        LoyaltyPointsUpdatedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task PublishTierUpgradedAsync(
        TierUpgradedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task PublishTestDriveCreatedAsync(
        TestDriveCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default);

    Task PublishTestDriveStatusChangedAsync(
        TestDriveStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default);

    Task PublishServiceReviewSubmittedAsync(
        ServiceReviewSubmittedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    Task PublishWarrantyClaimCreatedAsync(
        WarrantyClaimCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    Task PublishWarrantyClaimStatusChangedAsync(
        WarrantyClaimStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
