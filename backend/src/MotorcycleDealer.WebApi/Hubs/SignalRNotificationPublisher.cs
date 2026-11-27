using Microsoft.AspNetCore.SignalR;
using MotorcycleDealer.Application.Common.Interfaces;

namespace MotorcycleDealer.WebApi.Hubs;

public sealed class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishBookingCreatedAsync(
        BookingCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildDealerGroup(targetDealerId), NotificationHub.AdminGroup];
            await _hubContext.Clients.Groups(groups).BookingCreated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for BookingCreated (BookingId={BookingId}, DealerId={DealerId}).",
                payload.BookingId,
                targetDealerId);
        }
    }

    public async Task PublishBookingStatusChangedAsync(
        BookingStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildUserGroup(customerId), NotificationHub.BuildDealerGroup(dealerId)];
            await _hubContext.Clients.Groups(groups).BookingStatusChanged(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for BookingStatusChanged (BookingId={BookingId}).",
                payload.BookingId);
        }
    }

    public async Task PublishMaintenanceReminderAsync(
        MaintenanceReminderRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _hubContext.Clients.Group(NotificationHub.BuildUserGroup(customerId))
                .MaintenanceReminderTriggered(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for MaintenanceReminderTriggered (ReminderId={ReminderId}).",
                payload.ReminderId);
        }
    }

    public async Task PublishLoyaltyPointsUpdatedAsync(
        LoyaltyPointsUpdatedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _hubContext.Clients.Group(NotificationHub.BuildUserGroup(customerId))
                .LoyaltyPointsUpdated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for LoyaltyPointsUpdated (LoyaltyAccountId={LoyaltyAccountId}).",
                payload.LoyaltyAccountId);
        }
    }

    public async Task PublishTierUpgradedAsync(
        TierUpgradedRealtimeEvent payload,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _hubContext.Clients.Group(NotificationHub.BuildUserGroup(customerId))
                .TierUpgraded(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for TierUpgraded (LoyaltyAccountId={LoyaltyAccountId}).",
                payload.LoyaltyAccountId);
        }
    }

    public async Task PublishTestDriveCreatedAsync(
        TestDriveCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildDealerGroup(targetDealerId), NotificationHub.AdminGroup];
            await _hubContext.Clients.Groups(groups).TestDriveCreated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for TestDriveCreated (TestDriveId={TestDriveId}, DealerId={DealerId}).",
                payload.TestDriveId,
                targetDealerId);
        }
    }

    public async Task PublishTestDriveStatusChangedAsync(
        TestDriveStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildUserGroup(customerId), NotificationHub.BuildDealerGroup(dealerId)];
            await _hubContext.Clients.Groups(groups).TestDriveStatusChanged(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for TestDriveStatusChanged (TestDriveId={TestDriveId}).",
                payload.TestDriveId);
        }
    }

    public async Task PublishServiceReviewSubmittedAsync(
        ServiceReviewSubmittedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildDealerGroup(targetDealerId), NotificationHub.AdminGroup];
            await _hubContext.Clients.Groups(groups).ServiceReviewSubmitted(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for ServiceReviewSubmitted (ReviewId={ReviewId}, DealerId={DealerId}).",
                payload.ReviewId,
                targetDealerId);
        }
    }

    public async Task PublishWarrantyClaimCreatedAsync(
        WarrantyClaimCreatedRealtimeEvent payload,
        Guid targetDealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups = [NotificationHub.BuildDealerGroup(targetDealerId), NotificationHub.AdminGroup];
            await _hubContext.Clients.Groups(groups).WarrantyClaimCreated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for WarrantyClaimCreated (ClaimId={ClaimId}, DealerId={DealerId}).",
                payload.ClaimId,
                targetDealerId);
        }
    }

    public async Task PublishWarrantyClaimStatusChangedAsync(
        WarrantyClaimStatusChangedRealtimeEvent payload,
        Guid customerId,
        Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] groups =
            [
                NotificationHub.BuildUserGroup(customerId),
                NotificationHub.BuildDealerGroup(dealerId),
                NotificationHub.AdminGroup
            ];
            await _hubContext.Clients.Groups(groups).WarrantyClaimStatusChanged(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Transient SignalR broadcast warning for WarrantyClaimStatusChanged (ClaimId={ClaimId}, DealerId={DealerId}).",
                payload.ClaimId,
                dealerId);
        }
    }
}
