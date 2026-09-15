using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Logging;

namespace MotorcycleDealer.Mobile.Core.Api;

public interface IDealerMobileApiClient
{
    // Health
    Task<ApiResult<SystemHealthDto>> GetHealthAsync(bool probeDb = false, CancellationToken cancellationToken = default);

    // Auth
    Task<ApiResult<AuthTokenData>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<AuthTokenData>> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<AuthTokenData>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<LogoutResultDto>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<CurrentUserDto>> GetMeAsync(CancellationToken cancellationToken = default);

    // Dealers
    Task<ApiResult<IReadOnlyList<DealerDto>>> ListDealersAsync(string? city = null, CancellationToken cancellationToken = default);
    Task<ApiResult<DealerDto>> GetDealerByIdAsync(Guid dealerId, CancellationToken cancellationToken = default);

    // Showroom Catalog & Maintenance Rules
    Task<ApiResult<IReadOnlyList<MotorcycleModelDto>>> ListMotorcyclesAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? category = null,
        string? brand = null,
        string? search = null,
        Guid? dealerId = null,
        CancellationToken cancellationToken = default);
    Task<ApiResult<MotorcycleModelDto>> GetMotorcycleByIdAsync(Guid modelId, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<MaintenanceRuleDto>>> ListMaintenanceRulesAsync(Guid modelId, CancellationToken cancellationToken = default);

    // Inventories (VIN stock)
    Task<ApiResult<IReadOnlyList<DealerInventoryDto>>> ListInventoriesAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? dealerId = null,
        Guid? modelId = null,
        string? stockStatus = null,
        string? vin = null,
        string? search = null,
        bool publicOnly = false,
        CancellationToken cancellationToken = default);
    Task<ApiResult<DealerInventoryDto>> GetInventoryByIdAsync(Guid inventoryId, CancellationToken cancellationToken = default);
    Task<ApiResult<DealerInventoryDto>> CreateInventoryAsync(CreateDealerInventoryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<DealerInventoryDto>> UpdateInventoryAsync(Guid inventoryId, UpdateDealerInventoryRequest request, CancellationToken cancellationToken = default);

    // Sales Orders & Receipts
    Task<ApiResult<IReadOnlyList<SalesOrderDto>>> ListMySalesOrdersAsync(int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<SalesOrderDto>>> ListDealerSalesOrdersAsync(int pageNumber = 1, int pageSize = 20, Guid? dealerId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResult<SalesOrderDto>> GetSalesOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ApiResult<PrintableSalesOrderReceiptDto>> GetSalesOrderReceiptAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ApiResult<SalesOrderDto>> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);

    // Test Drives
    Task<ApiResult<IReadOnlyList<TestDriveBookingDto>>> ListMyTestDrivesAsync(string? status = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<TestDriveBookingDto>>> ListDealerTestDrivesAsync(Guid? dealerId = null, DateOnly? preferredDate = null, string? status = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<TestDriveBookingDto>> GetTestDriveByIdAsync(Guid testDriveId, CancellationToken cancellationToken = default);
    Task<ApiResult<TestDriveBookingDto>> CreateTestDriveAsync(CreateTestDriveRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<TestDriveBookingDto>> CancelTestDriveAsync(Guid testDriveId, CancelTestDriveRequest? request = null, CancellationToken cancellationToken = default);
    Task<ApiResult<TestDriveBookingDto>> UpdateTestDriveStatusAsync(Guid testDriveId, UpdateTestDriveStatusRequest request, CancellationToken cancellationToken = default);

    // Customer Vehicles (VIN Garage)
    Task<ApiResult<IReadOnlyList<CustomerVehicleDto>>> ListMyVehiclesAsync(string? vin = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<CustomerVehicleDto>>> LookupVehiclesAsync(string? vin = null, Guid? customerId = null, string? licensePlate = null, string? email = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResult<CustomerVehicleDto>> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<ApiResult<VehicleMaintenanceScheduleDto>> GetVehicleMaintenanceScheduleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<ApiResult<CustomerVehicleDto>> RegisterExternalVehicleAsync(RegisterExternalVehicleRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<CustomerVehicleDto>> UpdateVehicleMileageAsync(Guid vehicleId, UpdateVehicleMileageRequest request, CancellationToken cancellationToken = default);

    // Service Time Slots
    Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> ListTimeSlotsAsync(Guid? dealerId = null, DateOnly? fromDate = null, DateOnly? toDate = null, DateOnly? slotDate = null, bool? availableOnly = null, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> ListDealerTimeSlotsAsync(Guid? dealerId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceTimeSlotDto>> CreateTimeSlotAsync(CreateTimeSlotRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> GenerateTimeSlotsAsync(GenerateTimeSlotsRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceTimeSlotDto>> UpdateTimeSlotAsync(Guid slotId, UpdateTimeSlotRequest request, CancellationToken cancellationToken = default);

    // Service Bookings
    Task<ApiResult<IReadOnlyList<BookingDto>>> ListMyBookingsAsync(string? status = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<BookingDto>>> ListDealerBookingsAsync(Guid? dealerId = null, string? status = null, DateOnly? slotDate = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<BookingDto>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<ApiResult<BookingDto>> CreateBookingAsync(CreateServiceBookingRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<BookingDto>> CancelBookingAsync(Guid bookingId, CancelBookingRequest? request = null, CancellationToken cancellationToken = default);
    Task<ApiResult<BookingDto>> UpdateBookingStatusAsync(Guid bookingId, UpdateBookingStatusRequest request, CancellationToken cancellationToken = default);

    // Spare Parts
    Task<ApiResult<IReadOnlyList<SparePartDto>>> ListSparePartsAsync(string? category = null, Guid? compatibleModelId = null, bool includeUniversal = true, string? search = null, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ApiResult<SparePartDto>> GetSparePartByIdAsync(Guid partId, CancellationToken cancellationToken = default);

    // Service Invoices & Receipts
    Task<ApiResult<IReadOnlyList<ServiceInvoiceDto>>> ListMyServiceInvoicesAsync(Guid? bookingId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<ServiceInvoiceDto>>> ListDealerServiceInvoicesAsync(Guid? dealerId = null, Guid? customerId = null, Guid? bookingId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceInvoiceDto>> GetServiceInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResult<PrintableServiceReceiptDto>> GetServiceInvoiceReceiptAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceInvoiceDto>> CreateServiceInvoiceAsync(CreateServiceInvoiceRequest request, CancellationToken cancellationToken = default);

    // Service Reviews (+25 CSAT)
    Task<ApiResult<IReadOnlyList<ServiceReviewDto>>> ListMyServiceReviewsAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceReviewPageDto>> ListDealerServiceReviewsAsync(Guid? dealerId = null, int? rating = null, bool? unansweredOnly = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceReviewDto?>> GetServiceReviewByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceReviewDto>> CreateServiceReviewAsync(CreateServiceReviewRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<ServiceReviewDto>> RespondToServiceReviewAsync(Guid reviewId, RespondToServiceReviewRequest request, CancellationToken cancellationToken = default);

    // VIN Warranty Claims
    Task<ApiResult<IReadOnlyList<WarrantyClaimDto>>> ListMyWarrantyClaimsAsync(string? status = null, Guid? vehicleId = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<WarrantyClaimDto>>> ListDealerWarrantyClaimsAsync(Guid? dealerId = null, string? status = null, string? defectCategory = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<WarrantyClaimDto>> GetWarrantyClaimByIdAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<ApiResult<WarrantyClaimDto>> CreateWarrantyClaimAsync(CreateWarrantyClaimRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<WarrantyClaimDto>> UpdateWarrantyClaimStatusAsync(Guid claimId, UpdateWarrantyClaimStatusRequest request, CancellationToken cancellationToken = default);

    // Loyalty & Vouchers
    Task<ApiResult<IReadOnlyList<LoyaltyTierDto>>> ListLoyaltyTiersAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<LoyaltyAccountSummaryDto>> GetMyLoyaltyWalletAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<PointLedgerEntryDto>>> ListMyPointLedgerAsync(string? transactionType = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResult<CustomerLoyaltyInspectionDto>> InspectCustomerLoyaltyAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<VoucherCatalogDto>>> ListVoucherCatalogAsync(string? applicableScope = null, CancellationToken cancellationToken = default);
    Task<ApiResult<IReadOnlyList<CustomerVoucherDto>>> ListMyVouchersAsync(string? status = null, string? applicableScope = null, CancellationToken cancellationToken = default);
    Task<ApiResult<CustomerVoucherDto>> RedeemVoucherAsync(RedeemVoucherRequest request, CancellationToken cancellationToken = default);

    // Notifications
    Task<ApiResult<IReadOnlyList<NotificationDto>>> ListNotificationsAsync(int pageNumber = 1, int pageSize = 30, bool unreadOnly = false, DateTimeOffset? sinceUtc = null, Guid? dealerId = null, bool dealerFeedOnly = false, CancellationToken cancellationToken = default);
    Task<ApiResult<MarkNotificationReadResultDto>> MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<ApiResult<MarkAllNotificationsReadResultDto>> MarkAllNotificationsReadAsync(Guid? dealerId = null, CancellationToken cancellationToken = default);

    // Correlated User Activity Logs
    Task<ApiResult<UserActionLogQueryResultDto>> GetMyActivityLogsAsync(string? source = null, string? search = null, int take = 80, CancellationToken cancellationToken = default);
}
