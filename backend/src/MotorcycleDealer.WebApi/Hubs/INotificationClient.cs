using MotorcycleDealer.Application.Common.Interfaces;

namespace MotorcycleDealer.WebApi.Hubs;

public interface INotificationClient
{
    Task BookingCreated(BookingCreatedRealtimeEvent payload);
    Task BookingStatusChanged(BookingStatusChangedRealtimeEvent payload);
    Task MaintenanceReminderTriggered(MaintenanceReminderRealtimeEvent payload);
    Task LoyaltyPointsUpdated(LoyaltyPointsUpdatedRealtimeEvent payload);
    Task TierUpgraded(TierUpgradedRealtimeEvent payload);
    Task TestDriveCreated(TestDriveCreatedRealtimeEvent payload);
    Task TestDriveStatusChanged(TestDriveStatusChangedRealtimeEvent payload);
    Task ServiceReviewSubmitted(ServiceReviewSubmittedRealtimeEvent payload);
    Task WarrantyClaimCreated(WarrantyClaimCreatedRealtimeEvent payload);
    Task WarrantyClaimStatusChanged(WarrantyClaimStatusChangedRealtimeEvent payload);
}
