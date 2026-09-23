using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Logging;
using MotorcycleDealer.Mobile.Core.Security;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MotorcycleDealer.Mobile.Core.Api;

/// <summary>
/// Typed, AOT/trimmer-safe REST client targeting all /api/v1/* endpoints of the .NET 10 MotorcycleDealer.WebApi.
/// Uses the named "DealerApiClient" HttpClient pipeline backed by SingleFlightTokenRefreshHandler.
/// </summary>
public sealed class DealerMobileApiClient : IDealerMobileApiClient
{
    public const string HttpClientName = "DealerApiClient";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiEndpointProvider _endpointProvider;

    public DealerMobileApiClient(IHttpClientFactory httpClientFactory, IApiEndpointProvider endpointProvider)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
    }

    // =========================================================================
    // Health
    // =========================================================================

    public Task<ApiResult<SystemHealthDto>> GetHealthAsync(bool probeDb = false, CancellationToken cancellationToken = default)
    {
        var path = BuildPath("/api/v1/health", ("probeDb", probeDb ? "true" : null));
        return SendRawAsync(HttpMethod.Get, path, MobileJsonSerializerContext.Default.SystemHealthDto, cancellationToken);
    }

    // =========================================================================
    // Auth
    // =========================================================================

    public Task<ApiResult<AuthTokenData>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/auth/login",
            request,
            MobileJsonSerializerContext.Default.LoginRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData,
            cancellationToken);

    public Task<ApiResult<AuthTokenData>> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/auth/register",
            request,
            MobileJsonSerializerContext.Default.RegisterCustomerRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData,
            cancellationToken);

    public Task<ApiResult<AuthTokenData>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            request,
            MobileJsonSerializerContext.Default.RefreshTokenRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeAuthTokenData,
            cancellationToken);

    public Task<ApiResult<LogoutResultDto>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            request,
            MobileJsonSerializerContext.Default.LogoutRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeLogoutResultDto,
            cancellationToken);

    public Task<ApiResult<CurrentUserDto>> GetMeAsync(CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            "/api/v1/auth/me",
            MobileJsonSerializerContext.Default.ApiEnvelopeCurrentUserDto,
            cancellationToken);

    // =========================================================================
    // Dealers
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<DealerDto>>> ListDealersAsync(string? city = null, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath("/api/v1/dealers", ("city", city)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListDealerDto,
            cancellationToken);

    public Task<ApiResult<DealerDto>> GetDealerByIdAsync(Guid dealerId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/dealers/{dealerId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeDealerDto,
            cancellationToken);

    // =========================================================================
    // Showroom Catalog & Maintenance Rules
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<MotorcycleModelDto>>> ListMotorcyclesAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? category = null,
        string? brand = null,
        string? search = null,
        Guid? dealerId = null,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/motorcycles",
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
                ("category", category),
                ("brand", brand),
                ("search", search),
                ("dealerId", dealerId?.ToString("D"))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeMotorcycleModelDto,
            cancellationToken);

    public Task<ApiResult<MotorcycleModelDto>> GetMotorcycleByIdAsync(Guid modelId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/motorcycles/{modelId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeMotorcycleModelDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<MaintenanceRuleDto>>> ListMaintenanceRulesAsync(Guid modelId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/motorcycles/{modelId:D}/maintenance-rules",
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListMaintenanceRuleDto,
            cancellationToken);

    // =========================================================================
    // Inventories
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<DealerInventoryDto>>> ListInventoriesAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? dealerId = null,
        Guid? modelId = null,
        string? stockStatus = null,
        string? vin = null,
        string? search = null,
        bool publicOnly = false,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/inventories",
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
                ("dealerId", dealerId?.ToString("D")),
                ("modelId", modelId?.ToString("D")),
                ("stockStatus", stockStatus),
                ("vin", vin),
                ("search", search),
                ("publicOnly", publicOnly ? "true" : null)),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeDealerInventoryDto,
            cancellationToken);

    public Task<ApiResult<DealerInventoryDto>> GetInventoryByIdAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/inventories/{inventoryId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeDealerInventoryDto,
            cancellationToken);

    public Task<ApiResult<DealerInventoryDto>> CreateInventoryAsync(CreateDealerInventoryRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/inventories",
            request,
            MobileJsonSerializerContext.Default.CreateDealerInventoryRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeDealerInventoryDto,
            cancellationToken);

    public Task<ApiResult<DealerInventoryDto>> UpdateInventoryAsync(Guid inventoryId, UpdateDealerInventoryRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/inventories/{inventoryId:D}",
            request,
            MobileJsonSerializerContext.Default.UpdateDealerInventoryRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeDealerInventoryDto,
            cancellationToken);

    // =========================================================================
    // Sales Orders & Receipts
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<SalesOrderDto>>> ListMySalesOrdersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/sales-orders/my",
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
                ("search", search)),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeSalesOrderDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<SalesOrderDto>>> ListDealerSalesOrdersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? dealerId = null,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/sales-orders",
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
                ("dealerId", dealerId?.ToString("D")),
                ("search", search)),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeSalesOrderDto,
            cancellationToken);

    public Task<ApiResult<SalesOrderDto>> GetSalesOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/sales-orders/{orderId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeSalesOrderDto,
            cancellationToken);

    public Task<ApiResult<PrintableSalesOrderReceiptDto>> GetSalesOrderReceiptAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/sales-orders/{orderId:D}/receipt",
            MobileJsonSerializerContext.Default.ApiEnvelopePrintableSalesOrderReceiptDto,
            cancellationToken);

    public Task<ApiResult<SalesOrderDto>> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/sales-orders",
            request,
            MobileJsonSerializerContext.Default.CreateSalesOrderRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeSalesOrderDto,
            cancellationToken);

    // =========================================================================
    // Test Drives
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<TestDriveBookingDto>>> ListMyTestDrivesAsync(
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/test-drives/my",
                ("status", status),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<TestDriveBookingDto>>> ListDealerTestDrivesAsync(
        Guid? dealerId = null,
        DateOnly? preferredDate = null,
        string? status = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/test-drives",
                ("dealerId", dealerId?.ToString("D")),
                ("preferredDate", preferredDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("status", status),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    public Task<ApiResult<TestDriveBookingDto>> GetTestDriveByIdAsync(Guid testDriveId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/test-drives/{testDriveId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    public Task<ApiResult<TestDriveBookingDto>> CreateTestDriveAsync(CreateTestDriveRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/test-drives",
            request,
            MobileJsonSerializerContext.Default.CreateTestDriveRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    public Task<ApiResult<TestDriveBookingDto>> CancelTestDriveAsync(
        Guid testDriveId,
        CancelTestDriveRequest? request = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            $"/api/v1/test-drives/{testDriveId:D}/cancel",
            request ?? new CancelTestDriveRequest(),
            MobileJsonSerializerContext.Default.CancelTestDriveRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    public Task<ApiResult<TestDriveBookingDto>> UpdateTestDriveStatusAsync(
        Guid testDriveId,
        UpdateTestDriveStatusRequest request,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/test-drives/{testDriveId:D}/status",
            request,
            MobileJsonSerializerContext.Default.UpdateTestDriveStatusRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeTestDriveBookingDto,
            cancellationToken);

    // =========================================================================
    // Customer Vehicles (VIN Garage)
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<CustomerVehicleDto>>> ListMyVehiclesAsync(
        string? vin = null,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath("/api/v1/vehicles/my", ("vin", vin), ("search", search)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListCustomerVehicleDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<CustomerVehicleDto>>> LookupVehiclesAsync(
        string? vin = null,
        Guid? customerId = null,
        string? licensePlate = null,
        string? email = null,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath(
                "/api/v1/vehicles/lookup",
                ("vin", vin),
                ("customerId", customerId?.ToString("D")),
                ("licensePlate", licensePlate),
                ("email", email),
                ("search", search)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListCustomerVehicleDto,
            cancellationToken);

    public Task<ApiResult<CustomerVehicleDto>> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/vehicles/{vehicleId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVehicleDto,
            cancellationToken);

    public Task<ApiResult<VehicleMaintenanceScheduleDto>> GetVehicleMaintenanceScheduleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/vehicles/{vehicleId:D}/maintenance-history",
            MobileJsonSerializerContext.Default.ApiEnvelopeVehicleMaintenanceScheduleDto,
            cancellationToken);

    public Task<ApiResult<CustomerVehicleDto>> RegisterExternalVehicleAsync(RegisterExternalVehicleRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/vehicles",
            request,
            MobileJsonSerializerContext.Default.RegisterExternalVehicleRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVehicleDto,
            cancellationToken);

    public Task<ApiResult<CustomerVehicleDto>> UpdateVehicleMileageAsync(
        Guid vehicleId,
        UpdateVehicleMileageRequest request,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/vehicles/{vehicleId:D}/mileage",
            request,
            MobileJsonSerializerContext.Default.UpdateVehicleMileageRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVehicleDto,
            cancellationToken);

    // =========================================================================
    // Service Time Slots
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> ListTimeSlotsAsync(
        Guid? dealerId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        DateOnly? slotDate = null,
        bool? availableOnly = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath(
                "/api/v1/time-slots",
                ("dealerId", dealerId?.ToString("D")),
                ("fromDate", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("toDate", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("slotDate", slotDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("availableOnly", availableOnly.HasValue ? (availableOnly.Value ? "true" : "false") : null)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListServiceTimeSlotDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> ListDealerTimeSlotsAsync(
        Guid? dealerId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath(
                "/api/v1/time-slots/dealer",
                ("dealerId", dealerId?.ToString("D")),
                ("fromDate", fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("toDate", toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListServiceTimeSlotDto,
            cancellationToken);

    public Task<ApiResult<ServiceTimeSlotDto>> CreateTimeSlotAsync(CreateTimeSlotRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/time-slots",
            request,
            MobileJsonSerializerContext.Default.CreateTimeSlotRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceTimeSlotDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<ServiceTimeSlotDto>>> GenerateTimeSlotsAsync(GenerateTimeSlotsRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/time-slots/generate",
            request,
            MobileJsonSerializerContext.Default.GenerateTimeSlotsRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListServiceTimeSlotDto,
            cancellationToken);

    public Task<ApiResult<ServiceTimeSlotDto>> UpdateTimeSlotAsync(Guid slotId, UpdateTimeSlotRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/time-slots/{slotId:D}",
            request,
            MobileJsonSerializerContext.Default.UpdateTimeSlotRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceTimeSlotDto,
            cancellationToken);

    // =========================================================================
    // Service Bookings
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<BookingDto>>> ListMyBookingsAsync(
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/bookings/my",
                ("status", status),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeBookingDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<BookingDto>>> ListDealerBookingsAsync(
        Guid? dealerId = null,
        string? status = null,
        DateOnly? slotDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/bookings/dealer",
                ("dealerId", dealerId?.ToString("D")),
                ("status", status),
                ("slotDate", slotDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeBookingDto,
            cancellationToken);

    public Task<ApiResult<BookingDto>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/bookings/{bookingId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeBookingDto,
            cancellationToken);

    public Task<ApiResult<BookingDto>> CreateBookingAsync(CreateServiceBookingRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/bookings",
            request,
            MobileJsonSerializerContext.Default.CreateServiceBookingRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeBookingDto,
            cancellationToken);

    public Task<ApiResult<BookingDto>> CancelBookingAsync(
        Guid bookingId,
        CancelBookingRequest? request = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            $"/api/v1/bookings/{bookingId:D}/cancel",
            request ?? new CancelBookingRequest(),
            MobileJsonSerializerContext.Default.CancelBookingRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeBookingDto,
            cancellationToken);

    public Task<ApiResult<BookingDto>> UpdateBookingStatusAsync(
        Guid bookingId,
        UpdateBookingStatusRequest request,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/bookings/{bookingId:D}/status",
            request,
            MobileJsonSerializerContext.Default.UpdateBookingStatusRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeBookingDto,
            cancellationToken);

    // =========================================================================
    // Spare Parts
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<SparePartDto>>> ListSparePartsAsync(
        string? category = null,
        Guid? compatibleModelId = null,
        bool includeUniversal = true,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/spare-parts",
                ("category", category),
                ("compatibleModelId", compatibleModelId?.ToString("D")),
                ("includeUniversal", includeUniversal ? "true" : "false"),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeSparePartDto,
            cancellationToken);

    public Task<ApiResult<SparePartDto>> GetSparePartByIdAsync(Guid partId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/spare-parts/{partId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeSparePartDto,
            cancellationToken);

    // =========================================================================
    // Service Invoices & Receipts
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<ServiceInvoiceDto>>> ListMyServiceInvoicesAsync(
        Guid? bookingId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/service-invoices/my",
                ("bookingId", bookingId?.ToString("D")),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeServiceInvoiceDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<ServiceInvoiceDto>>> ListDealerServiceInvoicesAsync(
        Guid? dealerId = null,
        Guid? customerId = null,
        Guid? bookingId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/service-invoices",
                ("dealerId", dealerId?.ToString("D")),
                ("customerId", customerId?.ToString("D")),
                ("bookingId", bookingId?.ToString("D")),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeServiceInvoiceDto,
            cancellationToken);

    public Task<ApiResult<ServiceInvoiceDto>> GetServiceInvoiceByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/service-invoices/{invoiceId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceInvoiceDto,
            cancellationToken);

    public Task<ApiResult<PrintableServiceReceiptDto>> GetServiceInvoiceReceiptAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/service-invoices/{invoiceId:D}/receipt",
            MobileJsonSerializerContext.Default.ApiEnvelopePrintableServiceReceiptDto,
            cancellationToken);

    public Task<ApiResult<ServiceInvoiceDto>> CreateServiceInvoiceAsync(CreateServiceInvoiceRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/service-invoices",
            request,
            MobileJsonSerializerContext.Default.CreateServiceInvoiceRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceInvoiceDto,
            cancellationToken);

    // =========================================================================
    // Service Reviews (+25 CSAT)
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<ServiceReviewDto>>> ListMyServiceReviewsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/service-reviews/my",
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeServiceReviewDto,
            cancellationToken);

    public Task<ApiResult<ServiceReviewPageDto>> ListDealerServiceReviewsAsync(
        Guid? dealerId = null,
        int? rating = null,
        bool? unansweredOnly = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath(
                "/api/v1/service-reviews",
                ("dealerId", dealerId?.ToString("D")),
                ("rating", rating?.ToString(CultureInfo.InvariantCulture)),
                ("unansweredOnly", unansweredOnly.HasValue ? (unansweredOnly.Value ? "true" : "false") : null),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceReviewPageDto,
            cancellationToken);

    public Task<ApiResult<ServiceReviewDto?>> GetServiceReviewByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/service-reviews/by-invoice/{invoiceId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceReviewDto,
            cancellationToken)!;

    public Task<ApiResult<ServiceReviewDto>> CreateServiceReviewAsync(CreateServiceReviewRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/service-reviews",
            request,
            MobileJsonSerializerContext.Default.CreateServiceReviewRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceReviewDto,
            cancellationToken);

    public Task<ApiResult<ServiceReviewDto>> RespondToServiceReviewAsync(
        Guid reviewId,
        RespondToServiceReviewRequest request,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/service-reviews/{reviewId:D}/response",
            request,
            MobileJsonSerializerContext.Default.RespondToServiceReviewRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeServiceReviewDto,
            cancellationToken);

    // =========================================================================
    // Warranty Claims
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<WarrantyClaimDto>>> ListMyWarrantyClaimsAsync(
        string? status = null,
        Guid? vehicleId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/warranty-claims/my",
                ("status", status),
                ("vehicleId", vehicleId?.ToString("D")),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeWarrantyClaimDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<WarrantyClaimDto>>> ListDealerWarrantyClaimsAsync(
        Guid? dealerId = null,
        string? status = null,
        string? defectCategory = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/warranty-claims",
                ("dealerId", dealerId?.ToString("D")),
                ("status", status),
                ("defectCategory", defectCategory),
                ("search", search),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopeWarrantyClaimDto,
            cancellationToken);

    public Task<ApiResult<WarrantyClaimDto>> GetWarrantyClaimByIdAsync(Guid claimId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/warranty-claims/{claimId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeWarrantyClaimDto,
            cancellationToken);

    public Task<ApiResult<WarrantyClaimDto>> CreateWarrantyClaimAsync(CreateWarrantyClaimRequest request, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/warranty-claims",
            request,
            MobileJsonSerializerContext.Default.CreateWarrantyClaimRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeWarrantyClaimDto,
            cancellationToken);

    public Task<ApiResult<WarrantyClaimDto>> UpdateWarrantyClaimStatusAsync(
        Guid claimId,
        UpdateWarrantyClaimStatusRequest request,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/warranty-claims/{claimId:D}/status",
            request,
            MobileJsonSerializerContext.Default.UpdateWarrantyClaimStatusRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeWarrantyClaimDto,
            cancellationToken);

    // =========================================================================
    // Loyalty & Vouchers
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<LoyaltyTierDto>>> ListLoyaltyTiersAsync(CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            "/api/v1/loyalty/tiers",
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListLoyaltyTierDto,
            cancellationToken);

    public Task<ApiResult<LoyaltyAccountSummaryDto>> GetMyLoyaltyWalletAsync(CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            "/api/v1/loyalty/me",
            MobileJsonSerializerContext.Default.ApiEnvelopeLoyaltyAccountSummaryDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<PointLedgerEntryDto>>> ListMyPointLedgerAsync(
        string? transactionType = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        SendPagedEnvelopeAsync(
            BuildPath(
                "/api/v1/loyalty/ledger",
                ("transactionType", transactionType),
                ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
                ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.PagedApiEnvelopePointLedgerEntryDto,
            cancellationToken);

    public Task<ApiResult<CustomerLoyaltyInspectionDto>> InspectCustomerLoyaltyAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            $"/api/v1/loyalty/customers/{customerId:D}",
            MobileJsonSerializerContext.Default.ApiEnvelopeCustomerLoyaltyInspectionDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<VoucherCatalogDto>>> ListVoucherCatalogAsync(
        string? applicableScope = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath("/api/v1/vouchers/catalog", ("applicableScope", applicableScope)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListVoucherCatalogDto,
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<CustomerVoucherDto>>> ListMyVouchersAsync(
        string? status = null,
        string? applicableScope = null,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath("/api/v1/vouchers/my", ("status", status), ("applicableScope", applicableScope)),
            MobileJsonSerializerContext.Default.ApiEnvelopeIReadOnlyListCustomerVoucherDto,
            cancellationToken);

    public Task<ApiResult<CustomerVoucherDto>> RedeemVoucherAsync(RedeemVoucherRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, string>? headers = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Idempotency-Key"] = request.IdempotencyKey.Trim()
            }
            : null;

        return SendEnvelopeAsync(
            HttpMethod.Post,
            "/api/v1/vouchers/redeem",
            request,
            MobileJsonSerializerContext.Default.RedeemVoucherRequest,
            MobileJsonSerializerContext.Default.ApiEnvelopeCustomerVoucherDto,
            cancellationToken,
            headers);
    }

    // =========================================================================
    // Notifications
    // =========================================================================

    public Task<ApiResult<IReadOnlyList<NotificationDto>>> ListNotificationsAsync(
        int pageNumber = 1,
        int pageSize = 30,
        bool unreadOnly = false,
        DateTimeOffset? sinceUtc = null,
        Guid? dealerId = null,
        bool dealerFeedOnly = false,
        CancellationToken cancellationToken = default)
    {
        var baseRoute = dealerFeedOnly ? "/api/v1/notifications/dealer" : "/api/v1/notifications";
        var path = BuildPath(
            baseRoute,
            ("pageNumber", pageNumber.ToString(CultureInfo.InvariantCulture)),
            ("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
            ("unreadOnly", unreadOnly ? "true" : null),
            ("sinceUtc", sinceUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            ("dealerId", dealerId?.ToString("D")));

        return ExecuteRequestAsync(
            HttpMethod.Get,
            path,
            contentFactory: null,
            additionalHeaders: null,
            parseSuccessAsync: async (response, ct) =>
            {
                var feed = await response.Content.ReadFromJsonAsync(
                    MobileJsonSerializerContext.Default.NotificationFeedEnvelope,
                    ct).ConfigureAwait(false);

                return feed is not null
                    ? ApiResult<IReadOnlyList<NotificationDto>>.Ok(
                        feed.Data,
                        (int)response.StatusCode,
                        feed.Pagination,
                        feed.UnreadCount)
                    : InvalidJsonPayload<IReadOnlyList<NotificationDto>>((int)response.StatusCode, "Empty notification feed payload.");
            },
            cancellationToken);
    }

    public Task<ApiResult<MarkNotificationReadResultDto>> MarkNotificationReadAsync(Guid notificationId, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId:D}/read",
            MobileJsonSerializerContext.Default.ApiEnvelopeMarkNotificationReadResultDto,
            cancellationToken);

    public Task<ApiResult<MarkAllNotificationsReadResultDto>> MarkAllNotificationsReadAsync(Guid? dealerId = null, CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Post,
            BuildPath("/api/v1/notifications/read-all", ("dealerId", dealerId?.ToString("D"))),
            MobileJsonSerializerContext.Default.ApiEnvelopeMarkAllNotificationsReadResultDto,
            cancellationToken);

    // =========================================================================
    // Correlated User Activity Logs
    // =========================================================================

    public Task<ApiResult<UserActionLogQueryResultDto>> GetMyActivityLogsAsync(
        string? source = null,
        string? search = null,
        int take = 80,
        CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(
            HttpMethod.Get,
            BuildPath(
                "/api/v1/activity-logs/my",
                ("source", source),
                ("search", search),
                ("take", Math.Clamp(take, 1, 200).ToString(CultureInfo.InvariantCulture))),
            MobileJsonSerializerContext.Default.ApiEnvelopeUserActionLogQueryResultDto,
            cancellationToken);

    // =========================================================================
    // Core Transport Helpers (Shared, Deduplicated, AOT & JsonException-Safe)
    // =========================================================================

    private Task<ApiResult<T>> SendRawAsync<T>(
        HttpMethod method,
        string relativePath,
        JsonTypeInfo<T> responseTypeInfo,
        CancellationToken cancellationToken) =>
        ExecuteRequestAsync(
            method,
            relativePath,
            contentFactory: null,
            additionalHeaders: null,
            parseSuccessAsync: async (response, ct) =>
            {
                var payload = await response.Content.ReadFromJsonAsync(responseTypeInfo, ct).ConfigureAwait(false);
                return payload is not null
                    ? ApiResult<T>.Ok(payload, (int)response.StatusCode)
                    : InvalidJsonPayload<T>((int)response.StatusCode, "Empty JSON response body.");
            },
            cancellationToken);

    private Task<ApiResult<T>> SendEnvelopeAsync<T>(
        HttpMethod method,
        string relativePath,
        JsonTypeInfo<ApiEnvelope<T>> envelopeTypeInfo,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? additionalHeaders = null) =>
        ExecuteRequestAsync(
            method,
            relativePath,
            contentFactory: null,
            additionalHeaders,
            parseSuccessAsync: async (response, ct) =>
            {
                var envelope = await response.Content.ReadFromJsonAsync(envelopeTypeInfo, ct).ConfigureAwait(false);
                return envelope is not null
                    ? ApiResult<T>.Ok(envelope.Data, (int)response.StatusCode)
                    : InvalidJsonPayload<T>((int)response.StatusCode, "Empty API envelope payload.");
            },
            cancellationToken);

    private Task<ApiResult<TResponse>> SendEnvelopeAsync<TRequest, TResponse>(
        HttpMethod method,
        string relativePath,
        TRequest body,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<ApiEnvelope<TResponse>> envelopeTypeInfo,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? additionalHeaders = null) =>
        ExecuteRequestAsync(
            method,
            relativePath,
            contentFactory: () => JsonContent.Create(body, requestTypeInfo),
            additionalHeaders,
            parseSuccessAsync: async (response, ct) =>
            {
                var envelope = await response.Content.ReadFromJsonAsync(envelopeTypeInfo, ct).ConfigureAwait(false);
                return envelope is not null
                    ? ApiResult<TResponse>.Ok(envelope.Data, (int)response.StatusCode)
                    : InvalidJsonPayload<TResponse>((int)response.StatusCode, "Empty API envelope payload.");
            },
            cancellationToken);

    private Task<ApiResult<IReadOnlyList<T>>> SendPagedEnvelopeAsync<T>(
        string relativePath,
        JsonTypeInfo<PagedApiEnvelope<T>> envelopeTypeInfo,
        CancellationToken cancellationToken) =>
        ExecuteRequestAsync(
            HttpMethod.Get,
            relativePath,
            contentFactory: null,
            additionalHeaders: null,
            parseSuccessAsync: async (response, ct) =>
            {
                var envelope = await response.Content.ReadFromJsonAsync(envelopeTypeInfo, ct).ConfigureAwait(false);
                return envelope is not null
                    ? ApiResult<IReadOnlyList<T>>.Ok(envelope.Data, (int)response.StatusCode, envelope.Pagination)
                    : InvalidJsonPayload<IReadOnlyList<T>>((int)response.StatusCode, "Empty paged envelope payload.");
            },
            cancellationToken);

    private async Task<ApiResult<T>> ExecuteRequestAsync<T>(
        HttpMethod method,
        string relativePath,
        Func<HttpContent>? contentFactory,
        IReadOnlyDictionary<string, string>? additionalHeaders,
        Func<HttpResponseMessage, CancellationToken, Task<ApiResult<T>>> parseSuccessAsync,
        CancellationToken cancellationToken)
    {
        var lastStatusCode = 0;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(method, _endpointProvider.BuildApiUri(relativePath));

            if (contentFactory is not null)
            {
                request.Content = contentFactory();
            }

            if (additionalHeaders is not null)
            {
                foreach (var header in additionalHeaders)
                {
                    if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            lastStatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return await parseSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var problem = await TryReadProblemAsync(response, cancellationToken).ConfigureAwait(false);
            return ApiResult<T>.Fail(lastStatusCode, problem);
        }
        catch (JsonException ex)
        {
            return InvalidJsonPayload<T>(lastStatusCode, ex.Message);
        }
        catch (BrokenCircuitException)
        {
            return NetworkError<T>("The dealership API is temporarily recovering from errors. Please retry shortly.");
        }
        catch (TimeoutRejectedException)
        {
            return TimeoutError<T>();
        }
        catch (HttpRequestException ex)
        {
            return NetworkError<T>(ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutError<T>();
        }
    }

    private static async Task<ProblemDetailsPayload?> TryReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return JsonSerializer.Deserialize(text, MobileJsonSerializerContext.Default.ProblemDetailsPayload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildPath(string basePath, params (string Key, string? Value)[] queryParams)
    {
        var sb = new StringBuilder(basePath);
        var hasQuery = false;
        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            sb.Append(hasQuery ? '&' : '?');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
            hasQuery = true;
        }

        return sb.ToString();
    }

    private static ApiResult<T> InvalidJsonPayload<T>(int statusCode, string detail) =>
        ApiResult<T>.Fail(
            statusCode: statusCode,
            problem: new ProblemDetailsPayload
            {
                Status = statusCode,
                Title = "Invalid JSON Payload",
                Detail = detail,
                ErrorCode = "INVALID_JSON_PAYLOAD"
            });

    private static ApiResult<T> NetworkError<T>(string message) =>
        ApiResult<T>.Fail(
            statusCode: 0,
            problem: new ProblemDetailsPayload
            {
                Title = "Network Unavailable",
                Detail = message,
                ErrorCode = "NETWORK_UNREACHABLE"
            },
            isNetworkError: true);

    private static ApiResult<T> TimeoutError<T>() =>
        ApiResult<T>.Fail(
            statusCode: 0,
            problem: new ProblemDetailsPayload
            {
                Title = "Request Timed Out",
                Detail = "The server did not respond in time. Please check your connection.",
                ErrorCode = "REQUEST_TIMEOUT"
            },
            isNetworkError: true);
}
