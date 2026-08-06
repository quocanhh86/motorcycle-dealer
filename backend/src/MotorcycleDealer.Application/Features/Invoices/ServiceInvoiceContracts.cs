using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Invoices;

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

public static class ReceiptVerificationHash
{
    public static string ComputeForServiceInvoice(Guid invoiceId, string invoiceNumber, decimal netPaidAmount)
    {
        var canonicalPayload = string.Create(
            CultureInfo.InvariantCulture,
            $"SVC|{invoiceId:D}|{invoiceNumber.Trim().ToUpperInvariant()}|{netPaidAmount:0.00}");

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(digest)[..16];
    }

    public static string ComputeForSalesOrder(Guid salesOrderId, string orderNumber, decimal finalAmount)
    {
        var canonicalPayload = string.Create(
            CultureInfo.InvariantCulture,
            $"SLS|{salesOrderId:D}|{orderNumber.Trim().ToUpperInvariant()}|{finalAmount:0.00}");

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(digest)[..16];
    }
}

public sealed record ServiceInvoiceListQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    Guid? DealerId = null,
    Guid? CustomerId = null,
    Guid? BookingId = null,
    Guid? ServiceBookingId = null,
    string? Search = null) : PaginationQuery(PageNumber, PageSize)
{
    public Guid? ResolveEffectiveBookingId() =>
        BookingId is { } bid && bid != Guid.Empty
            ? bid
            : ServiceBookingId is { } sbid && sbid != Guid.Empty
                ? sbid
                : null;
}

public sealed record CreateServiceInvoiceLineItemRequest
{
    public Guid? SparePartId { get; init; }
    public string ItemType { get; init; } = "Part";
    public string? Description { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal? UnitPriceVnd { get; init; }

    public Guid? ResolveEffectiveSparePartId() =>
        SparePartId is { } id && id != Guid.Empty ? id : null;
}

public sealed record CreateServiceInvoiceRequest
{
    public Guid ServiceBookingId { get; init; }
    public Guid? BookingId { get; init; }
    public string? BookingRowVersion { get; init; }
    public string? RowVersion { get; init; }
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
    public string? LineItemsJson { get; init; }
    public IReadOnlyList<CreateServiceInvoiceLineItemRequest>? LineItems { get; init; }

    public Guid ResolveEffectiveBookingId() =>
        ServiceBookingId != Guid.Empty
            ? ServiceBookingId
            : BookingId is { } bid && bid != Guid.Empty
                ? bid
                : Guid.Empty;

    public string? ResolveEffectiveBookingRowVersion() =>
        !string.IsNullOrWhiteSpace(BookingRowVersion)
            ? BookingRowVersion.Trim()
            : !string.IsNullOrWhiteSpace(RowVersion)
                ? RowVersion.Trim()
                : null;
}

public interface IServiceInvoiceService
{
    Task<ServiceInvoiceDto> CreateServiceInvoiceAsync(
        CreateServiceInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ServiceInvoiceDto>> ListServiceInvoicesAsync(
        ServiceInvoiceListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default);

    Task<ServiceInvoiceDto> GetServiceInvoiceByIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PrintableServiceReceiptDto> GetPrintableReceiptAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
