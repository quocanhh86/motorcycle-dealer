using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Invoices;

namespace MotorcycleDealer.Application.Features.SalesOrders;

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

public sealed record SalesOrderListQuery(
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize,
    Guid? DealerId = null,
    Guid? CustomerId = null,
    string? Search = null) : PaginationQuery(PageNumber, PageSize);

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

    public string? ResolveEffectiveRowVersion() =>
        !string.IsNullOrWhiteSpace(InventoryRowVersion)
            ? InventoryRowVersion.Trim()
            : !string.IsNullOrWhiteSpace(RowVersion)
                ? RowVersion.Trim()
                : null;
}

public interface ISalesOrderService
{
    Task<SalesOrderDto> CreateSalesOrderAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SalesOrderDto>> ListSalesOrdersAsync(
        SalesOrderListQuery query,
        bool forceOwnCustomerOnly,
        CancellationToken cancellationToken = default);

    Task<SalesOrderDto> GetSalesOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PrintableSalesOrderReceiptDto> GetPrintableReceiptAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
