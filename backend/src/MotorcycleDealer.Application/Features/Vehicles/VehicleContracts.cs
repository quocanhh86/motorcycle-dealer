using MotorcycleDealer.Application.Features.Showroom;

namespace MotorcycleDealer.Application.Features.Vehicles;

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
    IReadOnlyList<VehicleRecentInvoiceSummaryDto> RecentInvoices);

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

public sealed record VehicleLookupQuery(
    string? Vin = null,
    Guid? CustomerId = null,
    string? LicensePlate = null,
    string? Email = null,
    string? Search = null);

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

    public int? ResolveMileageKm() => CurrentMileageKm ?? NewMileageKm;
}

public interface ICustomerVehicleService
{
    Task<IReadOnlyList<CustomerVehicleDto>> ListVehiclesAsync(
        VehicleLookupQuery query,
        bool forceOwnCustomerOnly,
        CancellationToken cancellationToken = default);

    Task<CustomerVehicleDto> GetVehicleByIdAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceScheduleDto> GetVehicleMaintenanceScheduleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<CustomerVehicleDto> RegisterExternalVehicleAsync(
        RegisterExternalVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerVehicleDto> UpdateVehicleMileageAsync(
        Guid vehicleId,
        UpdateVehicleMileageRequest request,
        CancellationToken cancellationToken = default);
}
