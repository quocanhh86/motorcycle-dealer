using System.Text.Json.Serialization;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;
using MotorcycleDealer.Mobile.Core.Media;

namespace MotorcycleDealer.Mobile.Core.Contracts;

// =========================================================================
// Health
// =========================================================================

public sealed record SystemHealthDto(
    string Status,
    string Service,
    string Version,
    string Environment,
    string Database,
    DateTimeOffset TimestampUtc,
    string TraceId);

// =========================================================================
// Auth & Identity
// =========================================================================

public sealed record RegisterCustomerRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record LogoutRequest
{
    public string? RefreshToken { get; init; }
    public bool RevokeAllDevices { get; init; }
}

public sealed record LogoutResultDto(
    bool Revoked,
    int RevokedTokenCount);

public sealed record CurrentUserDto(
    Guid UserId,
    string Email,
    string FullName,
    string PhoneNumber,
    string Role,
    Guid? DealerId,
    string? DealerCode,
    string? DealerName,
    string? StaffEmployeeCode,
    string? TierCode,
    decimal? TierMultiplier,
    int? AvailablePoints,
    int? LifetimeQualifyingPoints,
    decimal? LifetimeSpentVnd,
    IReadOnlyList<string> Permissions,
    decimal? PointsMultiplier = null);

public sealed record AuthTokenData(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    int ExpiresInSeconds,
    CurrentUserDto User);

// =========================================================================
// Dealers
// =========================================================================

public sealed record DealerDto(
    Guid Id,
    string Code,
    string Name,
    string City,
    string Address,
    string PhoneNumber,
    string ContactEmail,
    decimal? Latitude,
    decimal? Longitude,
    string OpeningTime,
    string ClosingTime,
    int DefaultSlotCapacity,
    int AvailableVinCount,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    [JsonIgnore]
    public string OperatingHoursDisplay => VietnamFormatting.FormatTimeRange(OpeningTime, ClosingTime);
}

// =========================================================================
// Showroom Catalog & Maintenance Rules
// =========================================================================

public sealed record DealerModelStockSummaryDto(
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string City,
    int AvailableCount)
{
    [JsonIgnore]
    public string UnitsLabel => LocalizationService.Current.FormatQuantity("unit", AvailableCount);
}

public sealed record MaintenanceRuleDto(
    Guid Id,
    Guid MotorcycleModelId,
    string MilestoneCode,
    string MilestoneName,
    int IntervalKm,
    int IntervalMonths,
    bool IsRecurring,
    int LeadTimeDays,
    int LeadMileageKm,
    int EstimatedDurationMinutes,
    string? ChecklistSummary,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record MotorcycleModelDto(
    Guid Id,
    string ModelCode,
    string Brand,
    string ModelName,
    string Category,
    int EngineDisplacementCc,
    decimal BaseRetailPrice,
    int WarrantyMonths,
    int WarrantyMileageKm,
    string? ImageUrl,
    string? SpecificationsJson,
    int AvailableStockCount,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<MaintenanceRuleDto> MaintenanceRules,
    IReadOnlyList<DealerModelStockSummaryDto> DealerStockBreakdown)
{
    [JsonIgnore]
    public string ResolvedImageSource => MotorcycleImageResolver.Current.Resolve(ModelCode, ImageUrl);

    [JsonIgnore]
    public string LocalizedCategory => LocalizationService.Current.GetEnumLabel("MotorcycleCategory", Category);
}

// =========================================================================
// Dealer Inventories (VIN Stock)
// =========================================================================

public sealed record DealerInventoryDto(
    Guid Id,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid MotorcycleModelId,
    string ModelCode,
    string Brand,
    string MotorcycleModelName,
    string Category,
    string Vin,
    string EngineNumber,
    string ColorName,
    short ManufactureYear,
    decimal ListedPrice,
    string StockStatus,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? SoldAtUtc,
    string RowVersion)
{
    [JsonIgnore]
    public string ResolvedImageSource => MotorcycleImageResolver.Current.Resolve(ModelCode, null);

    [JsonIgnore]
    public string LocalizedStockStatus => LocalizationService.Current.GetEnumLabel("StockStatus", StockStatus);
}

public sealed record CreateDealerInventoryRequest
{
    public Guid? DealerId { get; init; }
    public Guid MotorcycleModelId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string EngineNumber { get; init; } = string.Empty;
    public string ColorName { get; init; } = string.Empty;
    public int ManufactureYear { get; init; } = 2026;
    public decimal ListedPrice { get; init; }
}

public sealed record UpdateDealerInventoryRequest
{
    public Guid? DealerId { get; init; }
    public string? StockStatus { get; init; }
    public decimal? ListedPrice { get; init; }
    public string? ColorName { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

// =========================================================================
// Sales Orders & Printable Sales Receipt
// =========================================================================

public sealed record SalesOrderDto(
    Guid Id,
    Guid SalesOrderId,
    string OrderNumber,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid DealerInventoryId,
    string Vin,
    Guid MotorcycleModelId,
    string MotorcycleModelName,
    string ColorName,
    Guid? AppliedCustomerVoucherId,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string PaymentMethod,
    string AppliedTierCode,
    decimal AppliedMultiplier,
    int PointsEarned,
    string NewTierCode,
    bool TierUpgraded,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    Guid? OnboardedCustomerVehicleId,
    string? LicensePlate,
    string Status,
    string? Notes,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateSalesOrderRequest
{
    public Guid? CustomerId { get; init; }
    public string? CustomerEmail { get; init; }
    public Guid DealerInventoryId { get; init; }
    public Guid? DealerId { get; init; }
    public string? InventoryRowVersion { get; init; }
    public string? RowVersion { get; init; }
    public Guid? AppliedCustomerVoucherId { get; init; }
    public string? VoucherCode { get; init; }
    public string? CustomerVoucherRowVersion { get; init; }
    public string? LicensePlate { get; init; }
    public decimal? DiscountAmount { get; init; }
    public string? PaymentMethod { get; init; } = "BankTransfer";
    public string? Notes { get; init; }
}

public sealed record SalesOrderReceiptVehicleInfoDto(
    Guid? CustomerVehicleId,
    string ModelCode,
    string ModelName,
    string ColorName,
    string Vin,
    string EngineNumber,
    string? LicensePlate,
    int? InitialNextDueMileageKm,
    DateTimeOffset? InitialNextDueDateUtc);

public sealed record PrintableSalesOrderReceiptDto(
    string ReceiptNumber,
    string ReceiptType,
    Guid SalesOrderId,
    string OrderNumber,
    string PaymentMethod,
    string Status,
    DateTimeOffset IssuedAtUtc,
    string? SoldByStaffName,
    string VerificationCode,
    ReceiptDealerInfoDto Dealer,
    ReceiptCustomerInfoDto Customer,
    SalesOrderReceiptVehicleInfoDto Vehicle,
    decimal UnitPrice,
    decimal TotalAmount,
    string? AppliedVoucherCode,
    decimal DiscountAmount,
    decimal FinalAmount,
    string? Notes,
    ReceiptLoyaltySummaryDto Loyalty);

// =========================================================================
// Test Drives
// =========================================================================

public sealed record TestDriveBookingDto(
    Guid Id,
    Guid TestDriveId,
    string BookingNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid MotorcycleModelId,
    string MotorcycleModelCode,
    string MotorcycleModelName,
    Guid? AssignedInventoryId,
    string? AssignedVin,
    string? AssignedColorName,
    DateOnly PreferredDate,
    string StartTime,
    string EndTime,
    string DriverLicenseNumber,
    string ContactPhone,
    string? CustomerNotes,
    string? StaffNotes,
    string? CancellationReason,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    [JsonIgnore]
    public string FormattedStatus => LocalizationService.Current.GetEnumLabel("TestDriveStatus", Status);

    [JsonIgnore]
    public string ResolvedImageSource => MotorcycleImageResolver.Current.Resolve(MotorcycleModelCode, null);

    [JsonIgnore]
    public string ShortStartTime =>
        string.IsNullOrEmpty(StartTime) ? string.Empty : (StartTime.Length >= 5 ? StartTime[..5] : StartTime);

    [JsonIgnore]
    public bool CanStaffConfirm => Status == "Requested";

    [JsonIgnore]
    public bool CanStaffComplete => Status == "Confirmed";

    [JsonIgnore]
    public bool CanStaffCancel => Status is "Requested" or "Confirmed";

    [JsonIgnore]
    public bool HasStaffActions => CanStaffConfirm || CanStaffComplete || CanStaffCancel;
}

public sealed record CreateTestDriveRequest
{
    public Guid DealerId { get; init; }
    public Guid MotorcycleModelId { get; init; }
    public DateOnly PreferredDate { get; init; }
    public string StartTime { get; init; } = "09:00:00";
    public string EndTime { get; init; } = "10:00:00";
    public string DriverLicenseNumber { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string? CustomerNotes { get; init; }
}

public sealed record CancelTestDriveRequest
{
    public string? Reason { get; init; }
    public string? CancellationReason { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record UpdateTestDriveStatusRequest
{
    public string? Status { get; init; }
    public string? NewStatus { get; init; }
    public Guid? AssignedInventoryId { get; init; }
    public string? StaffNotes { get; init; }
    public string? CancellationReason { get; init; }
    public string? RowVersion { get; init; }
}

// =========================================================================
// Customer Vehicles (VIN Garage) & Maintenance Reminders
// =========================================================================

public sealed record VehicleRecentBookingSummaryDto(
    Guid BookingId,
    string BookingNumber,
    Guid DealerId,
    string DealerName,
    DateOnly SlotDate,
    string ServiceType,
    string Status,
    int OdometerAtBookingKm,
    DateTimeOffset CreatedAtUtc);

public sealed record VehicleRecentInvoiceSummaryDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid ServiceBookingId,
    Guid DealerId,
    string DealerName,
    decimal GrossAmount,
    decimal VoucherDiscountAmount,
    decimal NetPaidAmount,
    int OdometerAtCompletionKm,
    int PointsEarned,
    DateTimeOffset PaidAtUtc);

public sealed record CustomerVehicleDto(
    Guid Id,
    string Vin,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid MotorcycleModelId,
    string ModelCode,
    string Brand,
    string MotorcycleModelName,
    string Category,
    int EngineDisplacementCc,
    string? ImageUrl,
    string? SpecificationsJson,
    Guid? PurchasedSalesOrderId,
    string? LicensePlate,
    string? ColorName,
    string RegistrationSource,
    DateTimeOffset OwnershipStartDateUtc,
    int CurrentMileageKm,
    int LastServiceMileageKm,
    DateTimeOffset? LastServiceDateUtc,
    int NextDueMileageKm,
    DateTimeOffset NextDueDateUtc,
    string? NextMilestoneName,
    string MaintenanceStatus,
    bool IsDueSoonOrOverdue,
    int ActiveReminderCount,
    IReadOnlyList<VehicleRecentBookingSummaryDto> RecentBookings,
    IReadOnlyList<VehicleRecentInvoiceSummaryDto> RecentInvoices)
{
    [JsonIgnore]
    public string ResolvedImageSource => MotorcycleImageResolver.Current.Resolve(ModelCode, ImageUrl);

    [JsonIgnore]
    public string LocalizedMaintenanceStatus => LocalizationService.Current.GetEnumLabel("MaintenanceStatus", MaintenanceStatus);

    [JsonIgnore]
    public string LocalizedCategory => LocalizationService.Current.GetEnumLabel("MotorcycleCategory", Category);
}

public sealed record MaintenanceReminderDto(
    Guid Id,
    Guid CustomerVehicleId,
    Guid MaintenanceRuleId,
    string MilestoneCode,
    string MilestoneName,
    int TargetMileageKm,
    DateTimeOffset DueDateUtc,
    string TriggerReason,
    string DeduplicationKey,
    string Status,
    Guid? ResolvedBookingId,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record VehicleMaintenanceScheduleDto(
    CustomerVehicleDto Vehicle,
    IReadOnlyList<MaintenanceReminderDto> Reminders,
    IReadOnlyList<MaintenanceRuleDto> MaintenanceRules,
    IReadOnlyList<VehicleRecentInvoiceSummaryDto> ServiceInvoices);

public sealed record RegisterExternalVehicleRequest
{
    public string Vin { get; init; } = string.Empty;
    public Guid MotorcycleModelId { get; init; }
    public int CurrentMileageKm { get; init; }
    public int LastServiceMileageKm { get; init; }
    public DateTimeOffset? LastServiceDateUtc { get; init; }
    public string? LicensePlate { get; init; }
    public string? ColorName { get; init; }
}

public sealed record UpdateVehicleMileageRequest
{
    public int? CurrentMileageKm { get; init; }
    public int? NewMileageKm { get; init; }
}

// =========================================================================
// Service Time Slots
// =========================================================================

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
    string RowVersion)
{
    [JsonIgnore]
    public string TimeRangeDisplay => VietnamFormatting.FormatTimeRange(StartTime, EndTime);

    [JsonIgnore]
    public string OpenBaysLabel => LocalizationService.Current.Format("BookingSlotOpenBaysFormat", LocalizationService.Current.FormatQuantity("bay", RemainingCapacity));

    [JsonIgnore]
    public string BookedRatioLabel => LocalizationService.Current.Format("BookingSlotBookedRatioFormat", BookedCount, MaxCapacity);

    [JsonIgnore]
    public string FreeBadgeLabel => LocalizationService.Current.Format("BookingSlotFreeBadgeFormat", RemainingCapacity);
}

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
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }
}

public sealed record UpdateTimeSlotRequest
{
    public int? MaxCapacity { get; init; }
    public bool? IsBlocked { get; init; }
    public string? RowVersion { get; init; }
}

// =========================================================================
// Service Bookings
// =========================================================================

public sealed record BookingDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    Guid CustomerVehicleId,
    string Vin,
    string MotorcycleModelName,
    string? LicensePlate,
    Guid ServiceTimeSlotId,
    DateOnly SlotDate,
    string StartTime,
    string EndTime,
    string ServiceType,
    int OdometerAtBookingKm,
    Guid? AppliedCustomerVoucherId,
    string? ReservedVoucherCode,
    string? ReportedNotes,
    string? CancellationReason,
    DateTimeOffset? CheckedInAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    Guid MotorcycleModelId = default)
{
    [JsonIgnore]
    public string FormattedStatus => LocalizationService.Current.GetEnumLabel("BookingStatus", Status);

    [JsonIgnore]
    public string ShortStartTime =>
        string.IsNullOrEmpty(StartTime) ? string.Empty : (StartTime.Length >= 5 ? StartTime[..5] : StartTime);

    [JsonIgnore]
    public string FormattedServiceType => LocalizationService.Current.GetEnumLabel("ServiceType", ServiceType);

    [JsonIgnore]
    public bool CanStaffCheckIn => Status is "Pending" or "Confirmed";

    [JsonIgnore]
    public bool CanStaffStartBay => Status == "CheckedIn";

    [JsonIgnore]
    public bool CanStaffInvoice => Status is "CheckedIn" or "InProgress";

    [JsonIgnore]
    public bool HasStaffActions => CanStaffCheckIn || CanStaffStartBay || CanStaffInvoice;

    [JsonIgnore]
    public bool CanCustomerCancel => Status is "Pending" or "Confirmed";
}

public sealed record CreateServiceBookingRequest
{
    public Guid DealerId { get; init; }
    public Guid ServiceTimeSlotId { get; init; }
    public string? SlotRowVersion { get; init; }
    public Guid CustomerVehicleId { get; init; }
    public Guid? AppliedCustomerVoucherId { get; init; }
    public string? VoucherCode { get; init; }
    public string? VoucherRowVersion { get; init; }
    public string ServiceType { get; init; } = "PeriodicMaintenance";
    public int OdometerAtBookingKm { get; init; }
    public string? ReportedNotes { get; init; }
}

public sealed record UpdateBookingStatusRequest
{
    public string? NewStatus { get; init; }
    public string? Status { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record CancelBookingRequest
{
    public string? CancellationReason { get; init; }
    public string? Reason { get; init; }
    public string? RowVersion { get; init; }
}

// =========================================================================
// OEM Spare Parts
// =========================================================================

public sealed record SparePartDto(
    Guid Id,
    Guid PartId,
    string PartCode,
    string Name,
    string Category,
    Guid? CompatibleModelId,
    string? CompatibleModelCode,
    string? CompatibleModelName,
    bool IsUniversalFit,
    string UnitLabel,
    decimal UnitPriceVnd,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    [JsonIgnore]
    public string LocalizedCategory => LocalizationService.Current.GetEnumLabel("SparePartCategory", Category);
}

// =========================================================================
// Service Invoices & Printable Receipts
// =========================================================================

public sealed record ServiceInvoiceLineItemDto(
    Guid Id,
    int LineNumber,
    Guid? SparePartId,
    string? PartCodeSnapshot,
    string Description,
    string ItemType,
    int Quantity,
    decimal UnitPriceVnd,
    decimal LineTotalVnd);

public sealed record ServiceInvoiceDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid ServiceBookingId,
    string BookingNumber,
    string ServiceType,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid CustomerVehicleId,
    string Vin,
    string MotorcycleModelName,
    string? LicensePlate,
    Guid IssuedByStaffId,
    string? IssuedByStaffName,
    Guid? AppliedCustomerVoucherId,
    string? AppliedVoucherCode,
    decimal LaborAmount,
    decimal PartsAmount,
    decimal GrossAmount,
    decimal VoucherDiscountAmount,
    decimal NetPaidAmount,
    int OdometerAtCompletionKm,
    int? NextDueMileageKm,
    DateTimeOffset? NextDueDateUtc,
    string PaymentMethod,
    string AppliedTierCode,
    decimal AppliedMultiplier,
    int PointsEarned,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    string CurrentTierAfter,
    string NewTierCode,
    bool TierUpgraded,
    string? WorkSummaryNotes,
    string? LineItemsJson,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ServiceInvoiceLineItemDto>? LineItems = null);

public sealed record ReceiptDealerInfoDto(
    Guid DealerId,
    string Code,
    string Name,
    string Address,
    string City,
    string Phone,
    string PhoneNumber);

public sealed record ReceiptCustomerInfoDto(
    Guid CustomerId,
    string FullName,
    string Email,
    string Phone,
    string PhoneNumber);

public sealed record ServiceReceiptVehicleInfoDto(
    Guid CustomerVehicleId,
    string Vin,
    string? LicensePlate,
    string ModelCode,
    string ModelName,
    int OdometerAtBookingKm,
    int OdometerAtCompletionKm,
    int? NextDueMileageKm,
    DateTimeOffset? NextDueDateUtc);

public sealed record ReceiptLoyaltySummaryDto(
    int PointsEarned,
    string AppliedTierCode,
    string TierCodeAfter,
    string NewTierCode,
    decimal PointsMultiplierApplied,
    decimal TierMultiplier,
    bool TierUpgraded,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter);

public sealed record PrintableServiceReceiptDto(
    string ReceiptNumber,
    string ReceiptType,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid BookingId,
    string BookingNumber,
    string ServiceType,
    string PaymentMethod,
    DateTimeOffset IssuedAtUtc,
    string? IssuedByStaffName,
    string VerificationCode,
    ReceiptDealerInfoDto Dealer,
    ReceiptCustomerInfoDto Customer,
    ServiceReceiptVehicleInfoDto Vehicle,
    IReadOnlyList<ServiceInvoiceLineItemDto> LineItems,
    decimal LaborAmount,
    decimal PartsAmount,
    decimal GrossAmount,
    string? AppliedVoucherCode,
    decimal VoucherDiscountAmount,
    decimal NetPaidAmount,
    string? WorkSummaryNotes,
    ReceiptLoyaltySummaryDto Loyalty);

public sealed record CreateServiceInvoiceLineItemRequest
{
    public Guid? SparePartId { get; init; }
    public string ItemType { get; init; } = "Part";
    public string? Description { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal? UnitPriceVnd { get; init; }
}

public sealed record CreateServiceInvoiceRequest
{
    public Guid ServiceBookingId { get; init; }
    public string? BookingRowVersion { get; init; }
    public decimal LaborAmount { get; init; }
    public decimal PartsAmount { get; init; }
    public int OdometerAtCompletionKm { get; init; }
    public Guid? AppliedCustomerVoucherId { get; init; }
    public string? VoucherCode { get; init; }
    public string? CustomerVoucherRowVersion { get; init; }
    public bool? UseBookingReservedVoucher { get; init; }
    public bool RemoveReservedVoucher { get; init; }
    public string? PaymentMethod { get; init; } = "BankTransfer";
    public string? WorkSummaryNotes { get; init; }
    public IReadOnlyList<CreateServiceInvoiceLineItemRequest>? LineItems { get; init; }
}

// =========================================================================
// Service Reviews (+25 CSAT Bonus Points)
// =========================================================================

public sealed record ServiceReviewDto(
    Guid Id,
    Guid ReviewId,
    string ReviewNumber,
    Guid ServiceInvoiceId,
    string InvoiceNumber,
    Guid ServiceBookingId,
    string BookingNumber,
    string ServiceType,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid CustomerVehicleId,
    Guid MotorcycleModelId,
    string Vin,
    string MotorcycleModelName,
    string? LicensePlate,
    int Rating,
    string? QualityTags,
    IReadOnlyList<string> QualityTagList,
    string? Comment,
    int BonusPointsAwarded,
    int? AvailablePointsAfter,
    int? LifetimeQualifyingPointsAfter,
    string? StaffResponse,
    Guid? RespondedByUserId,
    string? RespondedByStaffName,
    DateTimeOffset? RespondedAtUtc,
    bool IsAnswered,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    [JsonIgnore]
    public string RatingDisplay => LocalizationService.Current.Format("StaffQaCsatRatingBadge", Rating);
}

public sealed record ServiceReviewPageDto(
    IReadOnlyList<ServiceReviewDto> Items,
    PaginationMeta Pagination,
    decimal AverageRating,
    int TotalReviewsCount,
    int FiveStarCount,
    int UnansweredCount,
    int TotalBonusPointsAwarded);

public sealed record CreateServiceReviewRequest
{
    public Guid ServiceInvoiceId { get; init; }
    public int Rating { get; init; } = 5;
    public string? QualityTags { get; init; }
    public IReadOnlyList<string>? QualityTagList { get; init; }
    public string? Comment { get; init; }
}

public sealed record RespondToServiceReviewRequest
{
    public string StaffResponse { get; init; } = string.Empty;
    public string? RowVersion { get; init; }
}

// =========================================================================
// VIN Warranty Claims (OEM Coverage)
// =========================================================================

public sealed record WarrantyClaimDto(
    Guid Id,
    Guid ClaimId,
    string ClaimNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid CustomerVehicleId,
    string Vin,
    string VinSnapshot,
    string? LicensePlate,
    Guid MotorcycleModelId,
    string MotorcycleModelCode,
    string MotorcycleModelName,
    DateTimeOffset WarrantyStartUtc,
    DateTimeOffset WarrantyExpiresAtUtc,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    int OdometerAtClaimKm,
    string DefectCategory,
    string SymptomDescription,
    Guid? CoveredSparePartId,
    string? CoveredSparePartCode,
    string? CoveredSparePartName,
    decimal CoveredAmountVnd,
    string? ResolutionNotes,
    string? RejectionReason,
    Guid? ReviewedByUserId,
    string? ReviewedByStaffName,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    [JsonIgnore]
    public string FormattedStatus => LocalizationService.Current.GetEnumLabel("WarrantyStatus", Status);

    [JsonIgnore]
    public string LocalizedDefectCategory => LocalizationService.Current.GetEnumLabel("WarrantyDefect", DefectCategory);

    [JsonIgnore]
    public string ResolvedImageSource => MotorcycleImageResolver.Current.Resolve(MotorcycleModelCode, null);

    [JsonIgnore]
    public bool CanStaffStartReview => Status == "Submitted";

    [JsonIgnore]
    public bool CanStaffApprove => Status is "Submitted" or "UnderReview";

    [JsonIgnore]
    public bool CanStaffComplete => Status == "Approved";

    [JsonIgnore]
    public bool CanStaffReject => Status is "Submitted" or "UnderReview";

    [JsonIgnore]
    public bool HasStaffActions => CanStaffStartReview || CanStaffApprove || CanStaffComplete || CanStaffReject;
}

public sealed record CreateWarrantyClaimRequest
{
    public Guid CustomerVehicleId { get; init; }
    public Guid DealerId { get; init; }
    public int OdometerAtClaimKm { get; init; }
    public string DefectCategory { get; init; } = string.Empty;
    public string SymptomDescription { get; init; } = string.Empty;
}

public sealed record UpdateWarrantyClaimStatusRequest
{
    public string NewStatus { get; init; } = string.Empty;
    public Guid? CoveredSparePartId { get; init; }
    public decimal? CoveredAmountVnd { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? RejectionReason { get; init; }
    public string? RowVersion { get; init; }
}

// =========================================================================
// Loyalty & Vouchers
// =========================================================================

public sealed record LoyaltyTierDto(
    Guid Id,
    Guid TierId,
    string TierCode,
    string DisplayName,
    int RankOrder,
    int MinQualifyingPoints,
    decimal PointsMultiplier,
    string BenefitsJson,
    bool IsActive,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record LoyaltyAccountSummaryDto(
    Guid Id,
    Guid AccountId,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid TierId,
    string TierCode,
    string CurrentTierCode,
    string TierDisplayName,
    int RankOrder,
    decimal PointsMultiplier,
    decimal TierMultiplier,
    int AvailablePoints,
    int LifetimeQualifyingPoints,
    decimal LifetimeSpentVnd,
    string? NextTierCode,
    string? NextTierDisplayName,
    int? NextTierMinPoints,
    decimal? NextTierMultiplier,
    int PointsToNextTier,
    decimal ProgressPercent,
    int AvailableVouchers,
    int ReservedVouchers,
    int UsedVouchers,
    int ExpiredVouchers,
    DateTimeOffset? TierUpgradedAtUtc,
    string RowVersion);

public sealed record PointLedgerEntryDto(
    Guid Id,
    Guid LedgerEntryId,
    Guid LoyaltyAccountId,
    string TransactionType,
    int PointsDelta,
    int BalanceAfter,
    string ReferenceType,
    Guid ReferenceId,
    string IdempotencyKey,
    string? Notes,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAtUtc)
{
    [JsonIgnore]
    public string LocalizedTransactionType => LocalizationService.Current.GetEnumLabel("LedgerType", TransactionType);
}

public sealed record VoucherCatalogDto(
    Guid Id,
    Guid CatalogId,
    string CodePrefix,
    string Code,
    string Title,
    string Description,
    string ApplicableScope,
    int PointsCost,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int ValidDays,
    int? QuotaTotal,
    int QuotaRedeemed,
    int? RemainingQuota,
    Guid MinTierId,
    string MinTierCode,
    int MinTierRankOrder,
    bool IsEligibleForCaller,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    [JsonIgnore]
    public string LocalizedMinTier => LocalizationService.Current.GetEnumLabel("LoyaltyTier", MinTierCode);

    [JsonIgnore]
    public string LocalizedScope => LocalizationService.Current.GetEnumLabel("ApplicableScope", ApplicableScope);
}

public sealed record CustomerVoucherDto(
    Guid Id,
    Guid CustomerVoucherId,
    string VoucherCode,
    Guid LoyaltyAccountId,
    Guid VoucherCatalogId,
    string CodePrefix,
    string Title,
    string Description,
    string ApplicableScope,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal MinOrderAmount,
    int PointsSpent,
    int? AvailablePointsAfter,
    int? LifetimeQualifyingPoints,
    string Status,
    string EffectiveStatus,
    bool IsExpired,
    DateTimeOffset RedeemedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    string RowVersion)
{
    [JsonIgnore]
    public string LocalizedEffectiveStatus => LocalizationService.Current.GetEnumLabel("VoucherStatus", EffectiveStatus);

    [JsonIgnore]
    public string LocalizedScope => LocalizationService.Current.GetEnumLabel("ApplicableScope", ApplicableScope);
}

public sealed record CustomerLoyaltyInspectionDto(
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid AccountId,
    string TierCode,
    string CurrentTierCode,
    decimal PointsMultiplier,
    decimal TierMultiplier,
    int AvailablePoints,
    int LifetimeQualifyingPoints,
    decimal LifetimeSpentVnd,
    string? NextTierCode,
    int PointsToNextTier,
    decimal ProgressPercent,
    string RowVersion,
    LoyaltyAccountSummaryDto Account,
    IReadOnlyList<CustomerVoucherDto> EligibleVouchers,
    IReadOnlyList<CustomerVoucherDto> AvailableServiceVouchers,
    IReadOnlyList<PointLedgerEntryDto> RecentLedgerEntries);

public sealed record RedeemVoucherRequest
{
    public Guid VoucherCatalogId { get; init; }
    public string? AccountRowVersion { get; init; }
    public string? IdempotencyKey { get; init; }
}

// =========================================================================
// Notifications
// =========================================================================

public sealed record NotificationDto(
    Guid Id,
    Guid NotificationId,
    Guid? RecipientUserId,
    Guid? TargetDealerId,
    string? TargetRole,
    string EventType,
    string Title,
    string Message,
    string? PayloadJson,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset OccurredAtUtc)
{
    [JsonIgnore]
    public string LocalizedEventType => LocalizationService.Current.GetEnumLabel("NotificationEvent", EventType);
}

public sealed record MarkNotificationReadResultDto(
    Guid Id,
    Guid NotificationId,
    bool IsRead,
    DateTimeOffset ReadAtUtc,
    int UnreadCount);

public sealed record MarkAllNotificationsReadResultDto(
    int MarkedReadCount,
    int UnreadCount,
    DateTimeOffset ReadAtUtc);
