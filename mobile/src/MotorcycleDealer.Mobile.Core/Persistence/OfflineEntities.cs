using SQLite;

namespace MotorcycleDealer.Mobile.Core.Persistence;

/// <summary>
/// Table 1/9: Watermark and sync metadata per owner &amp; resource key.
/// </summary>
[Table("CacheSyncState")]
public sealed class CacheSyncState
{
    [PrimaryKey]
    public string ScopeKey { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string ResourceKey { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedUtc { get; set; }
    public string? CursorToken { get; set; }
}

/// <summary>
/// Table 2/9: Public dealership catalog cache (preserved across user logout).
/// </summary>
[Table("CachedDealer")]
public sealed class CachedDealer
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public int DefaultSlotCapacity { get; set; }
    public int AvailableVinCount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 3/9: Public showroom motorcycle models cache (preserved across user logout).
/// </summary>
[Table("CachedMotorcycleModel")]
public sealed class CachedMotorcycleModel
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string ModelCode { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int EngineDisplacementCc { get; set; }
    public double BaseRetailPrice { get; set; }
    public int WarrantyMonths { get; set; }
    public int WarrantyMileageKm { get; set; }
    public string? ImageUrl { get; set; }
    public string? SpecificationsJson { get; set; }
    public int AvailableStockCount { get; set; }
    public bool IsActive { get; set; }
    public string MaintenanceRulesJson { get; set; } = "[]";
    public string DealerStockJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 4/9: User-scoped VIN garage vehicles cache.
/// </summary>
[Table("CachedCustomerVehicle")]
public sealed class CachedCustomerVehicle
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string VehicleId { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string MotorcycleModelName { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public int CurrentMileageKm { get; set; }
    public int NextDueMileageKm { get; set; }
    public string MaintenanceStatus { get; set; } = string.Empty;
    public bool IsDueSoonOrOverdue { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 5/9: User-scoped Loyalty Passbook wallet snapshot.
/// </summary>
[Table("CachedLoyaltyWallet")]
public sealed class CachedLoyaltyWallet
{
    [PrimaryKey]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;
    public string TierCode { get; set; } = string.Empty;
    public double PointsMultiplier { get; set; }
    public int AvailablePoints { get; set; }
    public int LifetimeQualifyingPoints { get; set; }
    public double LifetimeSpentVnd { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 6/9: User-scoped redeemed customer vouchers (enables offline QR passbook display).
/// </summary>
[Table("CachedCustomerVoucher")]
public sealed class CachedCustomerVoucher
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string VoucherId { get; set; } = string.Empty;
    public string VoucherCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApplicableScope { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 7/9: User/dealer-scoped active service bookings and test-drive appointments.
/// </summary>
[Table("CachedActiveAppointment")]
public sealed class CachedActiveAppointment
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string AppointmentKind { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string BookingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SlotDateIso { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 8/9: Infinite-TTL offline snapshot of immutable Service &amp; Sales receipts (wiped only on logout).
/// </summary>
[Table("CachedReceiptSnapshot")]
public sealed class CachedReceiptSnapshot
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string ReceiptKind { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset CachedAtUtc { get; set; }
}

/// <summary>
/// Table 9/9: Strictly allow-listed offline mutation outbox (FIFO). Never accepts immutable financial/RowVersion ops.
/// </summary>
[Table("OfflineMutationQueueItem")]
public sealed class OfflineMutationQueueItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string OwnerScopeKey { get; set; } = string.Empty;

    public string OperationType { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? NextRetryAfterUtc { get; set; }
    public string? LastError { get; set; }
}
