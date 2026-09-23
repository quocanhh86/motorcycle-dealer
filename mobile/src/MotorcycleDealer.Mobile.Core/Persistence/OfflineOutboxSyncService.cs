using System.Text.Json;
using MotorcycleDealer.Mobile.Core.Api;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Realtime;

namespace MotorcycleDealer.Mobile.Core.Persistence;

public static class OfflineMutationOperations
{
    // Strictly allow-listed safe offline operations
    public const string NotificationRead = "NotificationRead";
    public const string NotificationReadAll = "NotificationReadAll";
    public const string VehicleMileageUpdate = "VehicleMileageUpdate";
    public const string ServiceReviewSubmit = "ServiceReviewSubmit";
    public const string WarrantyClaimSubmit = "WarrantyClaimSubmit";

    // Explicitly forbidden online-only immutable / RowVersion-protected financial & slot operations
    public const string SalesOrder = "SalesOrder";
    public const string ServiceInvoice = "ServiceInvoice";
    public const string PointLedger = "PointLedger";
    public const string ServiceBooking = "ServiceBooking";
    public const string TestDrive = "TestDrive";
    public const string VoucherRedeem = "VoucherRedeem";

    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NotificationRead,
        NotificationReadAll,
        VehicleMileageUpdate,
        ServiceReviewSubmit,
        WarrantyClaimSubmit
    };

    public static IReadOnlySet<string> ForbiddenOnlineOnly { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SalesOrder,
        ServiceInvoice,
        PointLedger,
        ServiceBooking,
        TestDrive,
        VoucherRedeem
    };
}

public sealed class OnlineOnlyViolationException : InvalidOperationException
{
    public OnlineOnlyViolationException(string operationType)
        : base($"Operation '{operationType}' is online-only and forbidden from the offline outbox queue (OnlineOnlyViolation: immutable financial/ledger or RowVersion-protected concurrency operation).")
    {
        OperationType = operationType;
    }

    public string OperationType { get; }
    public string ErrorCode => "OnlineOnlyViolation";
}

public sealed record OutboxFlushSummary(
    bool SkippedOffline,
    int ProcessedCount,
    int SucceededCount,
    int DroppedPermanentCount,
    int RetriedCount,
    int RemainingCount);

public interface IOfflineOutboxSyncService
{
    void EnsureOfflineAllowed(string operationType);

    Task<OfflineMutationQueueItem> EnqueueRawAsync(
        string ownerScopeKey,
        string operationType,
        string targetEntityId,
        string payloadJson,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellationToken = default);

    Task<OfflineMutationQueueItem> EnqueueNotificationReadAsync(
        string ownerScopeKey,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<OfflineMutationQueueItem> EnqueueNotificationReadAllAsync(
        string ownerScopeKey,
        Guid? dealerId = null,
        CancellationToken cancellationToken = default);

    Task<OfflineMutationQueueItem> EnqueueVehicleMileageUpdateAsync(
        string ownerScopeKey,
        Guid vehicleId,
        int newMileageKm,
        CancellationToken cancellationToken = default);

    Task<OfflineMutationQueueItem> EnqueueServiceReviewSubmitAsync(
        string ownerScopeKey,
        CreateServiceReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<OfflineMutationQueueItem> EnqueueWarrantyClaimSubmitAsync(
        string ownerScopeKey,
        CreateWarrantyClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<OutboxFlushSummary> FlushAsync(
        string ownerScopeKey,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default);

    Task<int> GetPendingCountAsync(string ownerScopeKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strictly allow-listed FIFO offline outbox queue. Explicitly rejects online-only immutable/RowVersion
/// mutations (SalesOrder, ServiceInvoice, PointLedger, ServiceBooking, TestDrive, VoucherRedeem),
/// drops permanently rejected 409/422 items cleanly, and retries transient network/5xx errors with exponential cap.
/// </summary>
public sealed class OfflineOutboxSyncService : IOfflineOutboxSyncService
{
    public const int MaxRetryCapSeconds = 300;
    public const int BaseRetrySeconds = 5;

    private readonly IMobileOfflineDb _offlineDb;
    private readonly IDealerMobileApiClient _apiClient;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    public OfflineOutboxSyncService(
        IMobileOfflineDb offlineDb,
        IDealerMobileApiClient apiClient,
        IConnectivityMonitor connectivityMonitor)
    {
        _offlineDb = offlineDb ?? throw new ArgumentNullException(nameof(offlineDb));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
    }

    public void EnsureOfflineAllowed(string operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType) ||
            OfflineMutationOperations.ForbiddenOnlineOnly.Contains(operationType) ||
            !OfflineMutationOperations.Allowed.Contains(operationType))
        {
            throw new OnlineOnlyViolationException(operationType ?? string.Empty);
        }
    }

    public async Task<OfflineMutationQueueItem> EnqueueRawAsync(
        string ownerScopeKey,
        string operationType,
        string targetEntityId,
        string payloadJson,
        DateTimeOffset? createdAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        EnsureOfflineAllowed(operationType);

        var item = new OfflineMutationQueueItem
        {
            Id = Guid.CreateVersion7().ToString("D"),
            OwnerScopeKey = ownerScopeKey,
            OperationType = operationType,
            TargetEntityId = targetEntityId ?? string.Empty,
            PayloadJson = payloadJson ?? "{}",
            RetryCount = 0,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            NextRetryAfterUtc = null,
            LastError = null
        };

        await _offlineDb.EnqueueOutboxItemAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    public Task<OfflineMutationQueueItem> EnqueueNotificationReadAsync(
        string ownerScopeKey,
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        EnqueueRawAsync(
            ownerScopeKey,
            OfflineMutationOperations.NotificationRead,
            notificationId.ToString("D"),
            "{}",
            cancellationToken: cancellationToken);

    public Task<OfflineMutationQueueItem> EnqueueNotificationReadAllAsync(
        string ownerScopeKey,
        Guid? dealerId = null,
        CancellationToken cancellationToken = default) =>
        EnqueueRawAsync(
            ownerScopeKey,
            OfflineMutationOperations.NotificationReadAll,
            dealerId?.ToString("D") ?? string.Empty,
            "{}",
            cancellationToken: cancellationToken);

    public Task<OfflineMutationQueueItem> EnqueueVehicleMileageUpdateAsync(
        string ownerScopeKey,
        Guid vehicleId,
        int newMileageKm,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(
            new UpdateVehicleMileageRequest { CurrentMileageKm = newMileageKm, NewMileageKm = newMileageKm },
            MobileJsonSerializerContext.Default.UpdateVehicleMileageRequest);

        return EnqueueRawAsync(
            ownerScopeKey,
            OfflineMutationOperations.VehicleMileageUpdate,
            vehicleId.ToString("D"),
            payload,
            cancellationToken: cancellationToken);
    }

    public Task<OfflineMutationQueueItem> EnqueueServiceReviewSubmitAsync(
        string ownerScopeKey,
        CreateServiceReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payload = JsonSerializer.Serialize(
            request,
            MobileJsonSerializerContext.Default.CreateServiceReviewRequest);

        return EnqueueRawAsync(
            ownerScopeKey,
            OfflineMutationOperations.ServiceReviewSubmit,
            request.ServiceInvoiceId.ToString("D"),
            payload,
            cancellationToken: cancellationToken);
    }

    public Task<OfflineMutationQueueItem> EnqueueWarrantyClaimSubmitAsync(
        string ownerScopeKey,
        CreateWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payload = JsonSerializer.Serialize(
            request,
            MobileJsonSerializerContext.Default.CreateWarrantyClaimRequest);

        return EnqueueRawAsync(
            ownerScopeKey,
            OfflineMutationOperations.WarrantyClaimSubmit,
            request.CustomerVehicleId.ToString("D"),
            payload,
            cancellationToken: cancellationToken);
    }

    public async Task<OutboxFlushSummary> FlushAsync(
        string ownerScopeKey,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);

        if (!_connectivityMonitor.IsConnected)
        {
            var pending = await _offlineDb.GetOutboxCountAsync(ownerScopeKey, cancellationToken).ConfigureAwait(false);
            return new OutboxFlushSummary(
                SkippedOffline: true,
                ProcessedCount: 0,
                SucceededCount: 0,
                DroppedPermanentCount: 0,
                RetriedCount: 0,
                RemainingCount: pending);
        }

        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var referenceNow = nowUtc ?? DateTimeOffset.UtcNow;
            var items = await _offlineDb.GetPendingOutboxItemsAsync(ownerScopeKey, referenceNow, cancellationToken)
                .ConfigureAwait(false);

            var processed = 0;
            var succeeded = 0;
            var dropped = 0;
            var retried = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                var outcome = await DispatchItemAsync(item, cancellationToken).ConfigureAwait(false);
                if (outcome.IsSuccess)
                {
                    await _offlineDb.DeleteOutboxItemAsync(item.Id, cancellationToken).ConfigureAwait(false);
                    succeeded++;
                }
                else if (outcome.ShouldDropPermanently)
                {
                    await _offlineDb.DeleteOutboxItemAsync(item.Id, cancellationToken).ConfigureAwait(false);
                    dropped++;
                }
                else
                {
                    item.RetryCount++;
                    item.LastError = outcome.ErrorMessage;
                    var delaySeconds = ComputeExponentialBackoffSeconds(item.RetryCount);
                    item.NextRetryAfterUtc = referenceNow.AddSeconds(delaySeconds);
                    await _offlineDb.UpdateOutboxItemAsync(item, cancellationToken).ConfigureAwait(false);
                    retried++;
                }
            }

            var remaining = await _offlineDb.GetOutboxCountAsync(ownerScopeKey, cancellationToken).ConfigureAwait(false);
            return new OutboxFlushSummary(
                SkippedOffline: false,
                ProcessedCount: processed,
                SucceededCount: succeeded,
                DroppedPermanentCount: dropped,
                RetriedCount: retried,
                RemainingCount: remaining);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public Task<int> GetPendingCountAsync(string ownerScopeKey, CancellationToken cancellationToken = default) =>
        _offlineDb.GetOutboxCountAsync(ownerScopeKey, cancellationToken);

    public static int ComputeExponentialBackoffSeconds(int retryCount)
    {
        var clampedAttempt = Math.Clamp(retryCount, 1, 10);
        var raw = BaseRetrySeconds * (1 << (clampedAttempt - 1));
        return Math.Min(MaxRetryCapSeconds, raw);
    }

    private async Task<DispatchOutcome> DispatchItemAsync(
        OfflineMutationQueueItem item,
        CancellationToken cancellationToken)
    {
        switch (item.OperationType)
        {
            case OfflineMutationOperations.NotificationRead:
            {
                if (!Guid.TryParse(item.TargetEntityId, out var notificationId))
                {
                    return DispatchOutcome.Drop("Invalid notification ID in offline outbox.");
                }

                var result = await _apiClient.MarkNotificationReadAsync(notificationId, cancellationToken).ConfigureAwait(false);
                return ClassifyResult(result);
            }

            case OfflineMutationOperations.NotificationReadAll:
            {
                Guid? dealerId = Guid.TryParse(item.TargetEntityId, out var parsed) && parsed != Guid.Empty ? parsed : null;
                var result = await _apiClient.MarkAllNotificationsReadAsync(dealerId, cancellationToken).ConfigureAwait(false);
                return ClassifyResult(result);
            }

            case OfflineMutationOperations.VehicleMileageUpdate:
            {
                if (!Guid.TryParse(item.TargetEntityId, out var vehicleId))
                {
                    return DispatchOutcome.Drop("Invalid vehicle ID in offline outbox.");
                }

                var payload = JsonSerializer.Deserialize(
                    item.PayloadJson,
                    MobileJsonSerializerContext.Default.UpdateVehicleMileageRequest);
                if (payload is null)
                {
                    return DispatchOutcome.Drop("Corrupt mileage update payload.");
                }

                var result = await _apiClient.UpdateVehicleMileageAsync(vehicleId, payload, cancellationToken).ConfigureAwait(false);
                return ClassifyResult(result);
            }

            case OfflineMutationOperations.ServiceReviewSubmit:
            {
                var payload = JsonSerializer.Deserialize(
                    item.PayloadJson,
                    MobileJsonSerializerContext.Default.CreateServiceReviewRequest);
                if (payload is null)
                {
                    return DispatchOutcome.Drop("Corrupt service review payload.");
                }

                var result = await _apiClient.CreateServiceReviewAsync(payload, cancellationToken).ConfigureAwait(false);
                return ClassifyResult(result);
            }

            case OfflineMutationOperations.WarrantyClaimSubmit:
            {
                var payload = JsonSerializer.Deserialize(
                    item.PayloadJson,
                    MobileJsonSerializerContext.Default.CreateWarrantyClaimRequest);
                if (payload is null)
                {
                    return DispatchOutcome.Drop("Corrupt warranty claim payload.");
                }

                var result = await _apiClient.CreateWarrantyClaimAsync(payload, cancellationToken).ConfigureAwait(false);
                return ClassifyResult(result);
            }

            default:
                return DispatchOutcome.Drop($"Unsupported outbox operation '{item.OperationType}'.");
        }
    }

    private static DispatchOutcome ClassifyResult<T>(ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return DispatchOutcome.Success();
        }

        // Permanent domain/conflict/validation errors (400, 404, 409, 422) drop cleanly rather than looping forever.
        if (result.StatusCode is 400 or 404 or 409 or 422)
        {
            return DispatchOutcome.Drop(result.ErrorMessage);
        }

        // Network errors, timeouts, 401/403 transient states, 429, and 5xx retry with exponential cap.
        return DispatchOutcome.Retry(result.ErrorMessage);
    }

    private readonly record struct DispatchOutcome(bool IsSuccess, bool ShouldDropPermanently, string? ErrorMessage)
    {
        public static DispatchOutcome Success() => new(true, false, null);
        public static DispatchOutcome Drop(string message) => new(false, true, message);
        public static DispatchOutcome Retry(string message) => new(false, false, message);
    }
}
