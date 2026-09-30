using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.UnitTests.Fakes;

public sealed class InMemorySecureTokenStore : ISecureTokenStore
{
    private readonly object _sync = new();
    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset? _expiresAtUtc;
    private string? _sqlCipherKeyHex;
    private string? _apiBaseUrl;

    public InMemorySecureTokenStore(string? initialSqlCipherKeyHex = null)
    {
        _sqlCipherKeyHex = initialSqlCipherKeyHex;
    }

    public int ClearCount { get; private set; }
    public int SaveCount { get; private set; }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) { return Task.FromResult(_accessToken); }
    }

    public Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) { return Task.FromResult(_refreshToken); }
    }

    public Task<DateTimeOffset?> GetAccessTokenExpiresAtUtcAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) { return Task.FromResult(_expiresAtUtc); }
    }

    public Task SaveTokensAsync(AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _accessToken = tokens.AccessToken;
            _refreshToken = tokens.RefreshToken;
            _expiresAtUtc = tokens.ExpiresAtUtc;
            SaveCount++;
        }

        return Task.CompletedTask;
    }

    public Task ClearAuthTokensAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _accessToken = null;
            _refreshToken = null;
            _expiresAtUtc = null;
            ClearCount++;
        }

        return Task.CompletedTask;
    }

    public Task<string> GetOrCreateSqlCipherKeyHexAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(_sqlCipherKeyHex))
            {
                _sqlCipherKeyHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            }

            return Task.FromResult(_sqlCipherKeyHex);
        }
    }

    public Task<string?> GetApiBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) { return Task.FromResult(_apiBaseUrl); }
    }

    public Task SaveApiBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        lock (_sync) { _apiBaseUrl = baseUrl; }
        return Task.CompletedTask;
    }
}

public sealed class FakeMainThreadDispatcher : IMainThreadDispatcher
{
    public bool IsMainThread => true;
    public void BeginInvokeOnMainThread(Action action) => action();
    public Task InvokeOnMainThreadAsync(Func<Task> asyncAction) => asyncAction();
}

public sealed class FakeConnectivityMonitor : IConnectivityMonitor
{
    private bool _isConnected;

    public FakeConnectivityMonitor(bool isConnected = true)
    {
        _isConnected = isConnected;
    }

    public bool IsConnected => _isConnected;
    public event EventHandler<bool>? ConnectivityChanged;

    public void SetConnected(bool isConnected)
    {
        _isConnected = isConnected;
        ConnectivityChanged?.Invoke(this, isConnected);
    }
}

public sealed class FakeLocalNotificationService : ILocalNotificationService
{
    public List<LocalNotificationRequest> SentNotifications { get; } = [];

    public Task ShowAsync(LocalNotificationRequest notification, CancellationToken cancellationToken = default)
    {
        SentNotifications.Add(notification);
        return Task.CompletedTask;
    }
}

public sealed class FakeNavigationService : INavigationService
{
    public List<(string Route, IDictionary<string, object>? Parameters)> Navigations { get; } = [];
    public List<string> OpenedUrls { get; } = [];
    public List<(string? PromptTitle, bool RequireIso3779Vin)> BarcodeScanRequests { get; } = [];
    public int GoBackCount { get; private set; }
    public string? NextBarcodeScanResult { get; set; }

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        Navigations.Add((route, parameters));
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        GoBackCount++;
        return Task.CompletedTask;
    }

    public Task<string?> RequestBarcodeScanAsync(string? promptTitle = null, bool requireIso3779Vin = false)
    {
        BarcodeScanRequests.Add((promptTitle, requireIso3779Vin));
        return Task.FromResult(NextBarcodeScanResult);
    }

    public Task OpenExternalUrlAsync(string url)
    {
        OpenedUrls.Add(url);
        return Task.CompletedTask;
    }
}

public sealed class FakeHapticService : IHapticService
{
    public List<HapticKind> Performed { get; } = [];
    public void Perform(HapticKind kind) => Performed.Add(kind);
}

public sealed class FakeShareService : IShareService
{
    public List<(string Title, string Text)> SharedItems { get; } = [];

    public Task ShareTextAsync(string title, string text, CancellationToken cancellationToken = default)
    {
        SharedItems.Add((title, text));
        return Task.CompletedTask;
    }
}

public sealed class FakeUserFeedbackService : IUserFeedbackService
{
    public List<string> Toasts { get; } = [];
    public List<(string Message, string? ActionLabel)> Snackbars { get; } = [];

    public Task ShowToastAsync(string message, CancellationToken ct = default)
    {
        Toasts.Add(message);
        return Task.CompletedTask;
    }

    public Task ShowSnackbarAsync(
        string message,
        string? actionLabel = null,
        Func<Task>? onAction = null,
        CancellationToken ct = default)
    {
        Snackbars.Add((message, actionLabel));
        return Task.CompletedTask;
    }
}

public sealed class TempDatabasePathProvider : IDatabasePathProvider, IDisposable
{
    private readonly string _directory;
    private readonly string _dbPath;

    public TempDatabasePathProvider()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"md_mobile_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _dbPath = Path.Combine(_directory, "test_mobile.db3");
    }

    public string GetDatabasePath() => _dbPath;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch
        {
            // Ignore temp cleanup race on Windows.
        }
    }
}

public sealed class DelegateHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        handler(request, cancellationToken);
}

public sealed class StubHttpClientFactory(Func<string, HttpClient> factory) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => factory(name);
}

public static class TestJsonResponses
{
    public static HttpResponseMessage JsonOk<T>(T value, JsonTypeInfo<T> typeInfo, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage Problem(HttpStatusCode status, string errorCode, string detail, string? title = null)
    {
        var payload = new ProblemDetailsPayload
        {
            Status = (int)status,
            Title = title ?? status.ToString(),
            Detail = detail,
            ErrorCode = errorCode,
            TraceId = "test-trace-001"
        };
        var json = JsonSerializer.Serialize(payload, MobileJsonSerializerContext.Default.ProblemDetailsPayload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/problem+json")
        };
    }
}

public static class TestDataFactory
{
    public static ResponseMeta Meta() => new(DateTimeOffset.UtcNow, "trace-123");

    public static CurrentUserDto CustomerUser(Guid? userId = null) =>
        new(
            UserId: userId ?? Guid.Parse("44444444-4444-4444-4444-444444444409"),
            Email: "customer.gold@motorcycledealer.vn",
            FullName: "Le Hoang Nam (Gold)",
            PhoneNumber: "0913000301",
            Role: RoleNames.Customer,
            DealerId: null,
            DealerCode: null,
            DealerName: null,
            StaffEmployeeCode: null,
            TierCode: "Gold",
            TierMultiplier: 1.50m,
            AvailablePoints: 1850,
            LifetimeQualifyingPoints: 2450,
            LifetimeSpentVnd: 165_000_000m,
            Permissions: Permissions.CustomerDefaults,
            PointsMultiplier: 1.50m);

    public static CurrentUserDto StaffUser(Guid? dealerId = null, Guid? userId = null) =>
        new(
            UserId: userId ?? Guid.Parse("44444444-4444-4444-4444-444444444402"),
            Email: "staff.hn@motorcycledealer.vn",
            FullName: "Tran Thi Lan (Hanoi Staff)",
            PhoneNumber: "0902000101",
            Role: RoleNames.DealerStaff,
            DealerId: dealerId ?? Guid.Parse("22222222-2222-2222-2222-222222222202"),
            DealerCode: "DLR-HN-01",
            DealerName: "Apex Moto Hanoi Flagship",
            StaffEmployeeCode: "EMP-HN-001",
            TierCode: null,
            TierMultiplier: null,
            AvailablePoints: null,
            LifetimeQualifyingPoints: null,
            LifetimeSpentVnd: null,
            Permissions: Permissions.DealerStaffDefaults);

    public static CurrentUserDto AdminUser() =>
        new(
            UserId: Guid.Parse("44444444-4444-4444-4444-444444444401"),
            Email: "admin@motorcycledealer.vn",
            FullName: "Quoc Anh (Platform Admin)",
            PhoneNumber: "0901000001",
            Role: RoleNames.Admin,
            DealerId: null,
            DealerCode: null,
            DealerName: null,
            StaffEmployeeCode: null,
            TierCode: null,
            TierMultiplier: null,
            AvailablePoints: null,
            LifetimeQualifyingPoints: null,
            LifetimeSpentVnd: null,
            Permissions: Permissions.AdminDefaults);

    public static DealerDto SampleDealer(Guid? id = null, string code = "DLR-HN-01") =>
        new(
            Id: id ?? Guid.Parse("22222222-2222-2222-2222-222222222202"),
            Code: code,
            Name: "Apex Moto Hanoi Flagship",
            City: "Hanoi",
            Address: "54 Trang Tien Street, Hoan Kiem District, Hanoi",
            PhoneNumber: "02439368802",
            ContactEmail: "hanoi@motorcycledealer.vn",
            Latitude: 21.024915m,
            Longitude: 105.854821m,
            OpeningTime: "08:00:00",
            ClosingTime: "18:00:00",
            DefaultSlotCapacity: 4,
            AvailableVinCount: 12,
            IsActive: true,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddMonths(-6));

    public static MotorcycleModelDto SampleModel(Guid? id = null) =>
        new(
            Id: id ?? Guid.Parse("55555555-5555-5555-5555-555555555501"),
            ModelCode: "HND-SH160I-2026",
            Brand: "Honda",
            ModelName: "Honda SH160i ABS Sport",
            Category: "Scooter",
            EngineDisplacementCc: 157,
            BaseRetailPrice: 102_500_000m,
            WarrantyMonths: 36,
            WarrantyMileageKm: 30_000,
            ImageUrl: "/assets/models/sh160i.jpg",
            SpecificationsJson: "{\"abs\":\"Dual-Channel\"}",
            AvailableStockCount: 8,
            IsActive: true,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddMonths(-6),
            MaintenanceRules: [],
            DealerStockBreakdown: []);

    public static CustomerVehicleDto SampleVehicle(Guid? id = null, string vin = "RLHJC7920NY123456") =>
        new(
            Id: id ?? Guid.NewGuid(),
            Vin: vin,
            CustomerId: Guid.Parse("44444444-4444-4444-4444-444444444409"),
            CustomerFullName: "Le Hoang Nam (Gold)",
            CustomerEmail: "customer.gold@motorcycledealer.vn",
            CustomerPhoneNumber: "0913000301",
            MotorcycleModelId: Guid.Parse("55555555-5555-5555-5555-555555555501"),
            ModelCode: "HND-SH160I-2026",
            Brand: "Honda",
            MotorcycleModelName: "Honda SH160i ABS Sport",
            Category: "Scooter",
            EngineDisplacementCc: 157,
            ImageUrl: null,
            SpecificationsJson: null,
            PurchasedSalesOrderId: null,
            LicensePlate: "29B1-99888",
            ColorName: "Pearl Matte Black",
            RegistrationSource: "DealerPurchase",
            OwnershipStartDateUtc: DateTimeOffset.UtcNow.AddMonths(-6),
            CurrentMileageKm: 4200,
            LastServiceMileageKm: 1000,
            LastServiceDateUtc: DateTimeOffset.UtcNow.AddMonths(-3),
            NextDueMileageKm: 6000,
            NextDueDateUtc: DateTimeOffset.UtcNow.AddDays(10),
            NextMilestoneName: "6,000 km Periodic Service",
            MaintenanceStatus: "DueSoon",
            IsDueSoonOrOverdue: true,
            ActiveReminderCount: 1,
            RecentBookings: [],
            RecentInvoices: []);

    public static LoyaltyAccountSummaryDto SampleWallet(string tierCode = "Gold", int availablePoints = 1850, int lifetimePoints = 2450) =>
        new(
            Id: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            CustomerId: Guid.Parse("44444444-4444-4444-4444-444444444409"),
            CustomerFullName: "Le Hoang Nam (Gold)",
            CustomerEmail: "customer.gold@motorcycledealer.vn",
            TierId: Guid.Parse("33333333-3333-3333-3333-333333333303"),
            TierCode: tierCode,
            CurrentTierCode: tierCode,
            TierDisplayName: $"{tierCode} Member",
            RankOrder: 3,
            PointsMultiplier: 1.50m,
            TierMultiplier: 1.50m,
            AvailablePoints: availablePoints,
            LifetimeQualifyingPoints: lifetimePoints,
            LifetimeSpentVnd: 165_000_000m,
            NextTierCode: "Platinum",
            NextTierDisplayName: "Platinum Member",
            NextTierMinPoints: 5000,
            NextTierMultiplier: 2.00m,
            PointsToNextTier: Math.Max(0, 5000 - lifetimePoints),
            ProgressPercent: 49.0m,
            AvailableVouchers: 2,
            ReservedVouchers: 0,
            UsedVouchers: 1,
            ExpiredVouchers: 0,
            TierUpgradedAtUtc: DateTimeOffset.UtcNow.AddMonths(-2),
            RowVersion: "AAAAAAAAB9E=");

    public static CustomerVoucherDto SampleVoucher(Guid? id = null, string code = "SVC150-AB12CD") =>
        new(
            Id: id ?? Guid.NewGuid(),
            CustomerVoucherId: id ?? Guid.NewGuid(),
            VoucherCode: code,
            LoyaltyAccountId: Guid.NewGuid(),
            VoucherCatalogId: Guid.NewGuid(),
            CodePrefix: "SVC150",
            Title: "150,000 VND Workshop Discount",
            Description: "Valid on periodic service invoices over 500,000 VND.",
            ApplicableScope: "ServiceOnly",
            DiscountType: "FixedAmount",
            DiscountValue: 150_000m,
            MaxDiscountAmount: 150_000m,
            MinOrderAmount: 500_000m,
            PointsSpent: 150,
            AvailablePointsAfter: 1700,
            LifetimeQualifyingPoints: 2450,
            Status: "Available",
            EffectiveStatus: "Available",
            IsExpired: false,
            RedeemedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(85),
            UsedAtUtc: null,
            RowVersion: "AAAAAAAAB9F=");

    public static ServiceTimeSlotDto SampleSlot(Guid? id = null, int remaining = 2, string rv = "AAAAAAAAC01=")
    {
        var slotId = id ?? Guid.NewGuid();
        return new ServiceTimeSlotDto(
            Id: slotId,
            SlotId: slotId,
            DealerId: Guid.Parse("22222222-2222-2222-2222-222222222202"),
            DealerCode: "DLR-HN-01",
            DealerName: "Apex Moto Hanoi Flagship",
            SlotDate: new DateOnly(2026, 11, 25),
            StartTime: "09:00:00",
            EndTime: "10:00:00",
            MaxCapacity: 4,
            BookedCount: 4 - remaining,
            RemainingCapacity: remaining,
            AvailableBays: remaining,
            IsBlocked: false,
            IsAvailable: remaining > 0,
            RowVersion: rv);
    }

    public static BookingDto SampleBooking(Guid? id = null, string status = "Confirmed", string vin = "RLHJC7920NY123456")
    {
        var bookingId = id ?? Guid.NewGuid();
        return new BookingDto(
            Id: bookingId,
            BookingId: bookingId,
            BookingNumber: "BK-2026-0042",
            Status: status,
            CustomerId: Guid.Parse("44444444-4444-4444-4444-444444444409"),
            CustomerFullName: "Le Hoang Nam (Gold)",
            CustomerEmail: "customer.gold@motorcycledealer.vn",
            CustomerPhoneNumber: "0913000301",
            DealerId: Guid.Parse("22222222-2222-2222-2222-222222222202"),
            DealerCode: "DLR-HN-01",
            DealerName: "Apex Moto Hanoi Flagship",
            CustomerVehicleId: Guid.NewGuid(),
            Vin: vin,
            MotorcycleModelName: "Honda SH160i ABS Sport",
            LicensePlate: "29B1-99888",
            ServiceTimeSlotId: Guid.NewGuid(),
            SlotDate: new DateOnly(2026, 11, 25),
            StartTime: "09:00:00",
            EndTime: "10:00:00",
            ServiceType: "PeriodicMaintenance",
            OdometerAtBookingKm: 4200,
            AppliedCustomerVoucherId: null,
            ReservedVoucherCode: null,
            ReportedNotes: "Check front brake pad",
            CancellationReason: null,
            CheckedInAtUtc: null,
            CompletedAtUtc: null,
            CancelledAtUtc: null,
            RowVersion: "AAAAAAAAD01=",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddHours(-2),
            UpdatedAtUtc: null,
            MotorcycleModelId: Guid.Parse("55555555-5555-5555-5555-555555555501"));
    }

    public static PrintableServiceReceiptDto SampleServiceReceipt(Guid? invoiceId = null)
    {
        var id = invoiceId ?? Guid.NewGuid();
        return new PrintableServiceReceiptDto(
            ReceiptNumber: "RCP-SVC-2026-0009",
            ReceiptType: "Service",
            InvoiceId: id,
            InvoiceNumber: "INV-2026-0009",
            BookingId: Guid.NewGuid(),
            BookingNumber: "BK-2026-0042",
            ServiceType: "PeriodicMaintenance",
            PaymentMethod: "BankTransfer",
            IssuedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
            IssuedByStaffName: "Tran Thi Lan (Hanoi Staff)",
            VerificationCode: "A1B2C3D4E5F60718",
            Dealer: new ReceiptDealerInfoDto(Guid.NewGuid(), "DLR-HN-01", "Apex Moto Hanoi Flagship", "54 Trang Tien", "Hanoi", "02439368802", "02439368802"),
            Customer: new ReceiptCustomerInfoDto(Guid.NewGuid(), "Le Hoang Nam", "customer.gold@motorcycledealer.vn", "0913000301", "0913000301"),
            Vehicle: new ServiceReceiptVehicleInfoDto(Guid.NewGuid(), "RLHJC7920NY123456", "29B1-99888", "HND-SH160I-2026", "Honda SH160i ABS Sport", 4200, 4210, 8210, DateTimeOffset.UtcNow.AddMonths(4)),
            LineItems: [new ServiceInvoiceLineItemDto(Guid.NewGuid(), 1, Guid.NewGuid(), "OEM-OIL-10W30", "Honda Synthetic Oil 1L", "Part", 1, 250_000m, 250_000m)],
            LaborAmount: 350_000m,
            PartsAmount: 250_000m,
            GrossAmount: 600_000m,
            AppliedVoucherCode: null,
            VoucherDiscountAmount: 0m,
            NetPaidAmount: 600_000m,
            WorkSummaryNotes: "Oil change & brake inspection.",
            Loyalty: new ReceiptLoyaltySummaryDto(90, "Gold", "Gold", "Gold", 1.50m, 1.50m, false, 1940, 2540));
    }

    public static PrintableSalesOrderReceiptDto SampleSalesReceipt(Guid? orderId = null)
    {
        var id = orderId ?? Guid.NewGuid();
        return new PrintableSalesOrderReceiptDto(
            ReceiptNumber: "RCP-SLS-2026-0005",
            ReceiptType: "Sales",
            SalesOrderId: id,
            OrderNumber: "SO-2026-0005",
            PaymentMethod: "BankTransfer",
            Status: "Completed",
            IssuedAtUtc: DateTimeOffset.UtcNow.AddDays(-10),
            SoldByStaffName: "Tran Thi Lan (Hanoi Staff)",
            VerificationCode: "F0E1D2C3B4A59687",
            Dealer: new ReceiptDealerInfoDto(Guid.NewGuid(), "DLR-HN-01", "Apex Moto Hanoi Flagship", "54 Trang Tien", "Hanoi", "02439368802", "02439368802"),
            Customer: new ReceiptCustomerInfoDto(Guid.NewGuid(), "Le Hoang Nam", "customer.gold@motorcycledealer.vn", "0913000301", "0913000301"),
            Vehicle: new SalesOrderReceiptVehicleInfoDto(Guid.NewGuid(), "HND-SH160I-2026", "Honda SH160i ABS Sport", "Pearl Matte Black", "RLHJC7920NY123456", "ENG-160-991", "29B1-99888", 1000, DateTimeOffset.UtcNow.AddMonths(1)),
            UnitPrice: 102_500_000m,
            TotalAmount: 102_500_000m,
            AppliedVoucherCode: null,
            DiscountAmount: 0m,
            FinalAmount: 102_500_000m,
            Notes: "Showroom delivery completed.",
            Loyalty: new ReceiptLoyaltySummaryDto(15375, "Gold", "Platinum", "Platinum", 1.50m, 1.50m, true, 17225, 17825));
    }
}
