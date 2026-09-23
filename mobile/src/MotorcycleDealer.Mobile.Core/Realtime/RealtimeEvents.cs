namespace MotorcycleDealer.Mobile.Core.Realtime;

public static class SignalRHubEventNames
{
    public const string HubPath = "/hubs/notifications";

    public const string BookingCreated = "BookingCreated";
    public const string BookingStatusChanged = "BookingStatusChanged";
    public const string MaintenanceReminderTriggered = "MaintenanceReminderTriggered";
    public const string LoyaltyPointsUpdated = "LoyaltyPointsUpdated";
    public const string TierUpgraded = "TierUpgraded";
    public const string TestDriveCreated = "TestDriveCreated";
    public const string TestDriveStatusChanged = "TestDriveStatusChanged";
    public const string ServiceReviewSubmitted = "ServiceReviewSubmitted";
    public const string WarrantyClaimCreated = "WarrantyClaimCreated";
    public const string WarrantyClaimStatusChanged = "WarrantyClaimStatusChanged";

    public static IReadOnlyList<string> All { get; } =
    [
        BookingCreated,
        BookingStatusChanged,
        MaintenanceReminderTriggered,
        LoyaltyPointsUpdated,
        TierUpgraded,
        TestDriveCreated,
        TestDriveStatusChanged,
        ServiceReviewSubmitted,
        WarrantyClaimCreated,
        WarrantyClaimStatusChanged
    ];
}

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
