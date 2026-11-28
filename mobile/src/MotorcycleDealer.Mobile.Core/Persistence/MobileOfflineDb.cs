using System.Globalization;
using System.Text.Json;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Security;
using SQLite;

namespace MotorcycleDealer.Mobile.Core.Persistence;

public interface IDatabasePathProvider
{
    string GetDatabasePath();
}

public sealed class DefaultDatabasePathProvider : IDatabasePathProvider
{
    private readonly string _path;

    public DefaultDatabasePathProvider(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            _path = explicitPath;
        }
        else
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.GetTempPath();
            }

            _path = Path.Combine(baseDir, "MotorcycleDealerMobile", "motorcycle_dealer_mobile_v1.db3");
        }
    }

    public string GetDatabasePath() => _path;
}

public interface IMobileOfflineDb : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListTableNamesAsync(CancellationToken cancellationToken = default);
    Task<string> GetJournalModeAsync(CancellationToken cancellationToken = default);

    // Sync state watermarks
    Task UpsertSyncStateAsync(string ownerScopeKey, string resourceKey, DateTimeOffset syncedUtc, string? cursorToken = null, CancellationToken cancellationToken = default);
    Task<CacheSyncState?> GetSyncStateAsync(string ownerScopeKey, string resourceKey, CancellationToken cancellationToken = default);

    // Encrypted session profile snapshot (offline cold-launch resume; wiped on logout / force sign-out)
    Task SaveLastAuthenticatedUserAsync(CurrentUserDto user, CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetLastAuthenticatedUserAsync(CancellationToken cancellationToken = default);
    Task ClearLastAuthenticatedUserAsync(CancellationToken cancellationToken = default);

    // Public caches (preserved across logout)
    Task SaveDealersAsync(IReadOnlyList<DealerDto> dealers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DealerDto>> GetCachedDealersAsync(CancellationToken cancellationToken = default);

    Task SaveMotorcycleModelsAsync(IReadOnlyList<MotorcycleModelDto> models, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MotorcycleModelDto>> GetCachedMotorcycleModelsAsync(CancellationToken cancellationToken = default);

    Task SaveLoyaltyTiersAsync(IReadOnlyList<LoyaltyTierDto> tiers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoyaltyTierDto>> GetLoyaltyTiersAsync(CancellationToken cancellationToken = default);

    // User/Dealer-scoped caches (partitioned by OwnerScopeKey, wiped on logout)
    Task SaveCustomerVehiclesAsync(string ownerScopeKey, IReadOnlyList<CustomerVehicleDto> vehicles, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerVehicleDto>> GetCachedCustomerVehiclesAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    Task SaveLoyaltyWalletAsync(string ownerScopeKey, LoyaltyAccountSummaryDto wallet, CancellationToken cancellationToken = default);
    Task<LoyaltyAccountSummaryDto?> GetCachedLoyaltyWalletAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    Task SaveCustomerVouchersAsync(string ownerScopeKey, IReadOnlyList<CustomerVoucherDto> vouchers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerVoucherDto>> GetCachedCustomerVouchersAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    Task SaveActiveBookingsAsync(string ownerScopeKey, IReadOnlyList<BookingDto> bookings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingDto>> GetCachedActiveBookingsAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    Task SaveActiveTestDrivesAsync(string ownerScopeKey, IReadOnlyList<TestDriveBookingDto> testDrives, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestDriveBookingDto>> GetCachedActiveTestDrivesAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    // Infinite-TTL receipt snapshots (partitioned by OwnerScopeKey)
    Task SaveServiceReceiptAsync(string ownerScopeKey, PrintableServiceReceiptDto receipt, CancellationToken cancellationToken = default);
    Task<PrintableServiceReceiptDto?> GetCachedServiceReceiptAsync(string ownerScopeKey, Guid invoiceId, CancellationToken cancellationToken = default);

    Task SaveSalesReceiptAsync(string ownerScopeKey, PrintableSalesOrderReceiptDto receipt, CancellationToken cancellationToken = default);
    Task<PrintableSalesOrderReceiptDto?> GetCachedSalesReceiptAsync(string ownerScopeKey, Guid salesOrderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedReceiptSnapshot>> ListCachedReceiptSnapshotsAsync(string ownerScopeKey, CancellationToken cancellationToken = default);

    // Offline mutation outbox
    Task EnqueueOutboxItemAsync(OfflineMutationQueueItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OfflineMutationQueueItem>> GetPendingOutboxItemsAsync(string ownerScopeKey, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    Task<int> GetOutboxCountAsync(string ownerScopeKey, CancellationToken cancellationToken = default);
    Task UpdateOutboxItemAsync(OfflineMutationQueueItem item, CancellationToken cancellationToken = default);
    Task DeleteOutboxItemAsync(string id, CancellationToken cancellationToken = default);

    // Security wipe
    Task PurgeUserScopedDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Encrypted SQLCipher 9-table offline cache &amp; outbox store with WAL journaling and strict OwnerScopeKey isolation.
/// </summary>
public sealed class MobileOfflineDb : IMobileOfflineDb
{
    public const string PublicOwnerScope = "public:anonymous:global";
    public const string LoyaltyTiersOwnerScope = "public:loyalty_tiers";
    public const string LoyaltyTiersResourceKey = "loyalty_tiers";
    public const string SessionProfileOwnerScope = "session:last_user";
    public const string SessionProfileResourceKey = "current_user_profile";
    private const string ServiceReceiptKind = "Service";
    private const string SalesReceiptKind = "Sales";
    private const string BookingAppointmentKind = "Booking";
    private const string TestDriveAppointmentKind = "TestDrive";

    private static int _batteriesInitialized;

    private readonly IDatabasePathProvider _pathProvider;
    private readonly ISecureTokenStore _tokenStore;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile SQLiteAsyncConnection? _connection;
    private int _disposed;

    public MobileOfflineDb(IDatabasePathProvider pathProvider, ISecureTokenStore tokenStore)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public static void EnsureSqlCipherInitialized()
    {
        if (Interlocked.Exchange(ref _batteriesInitialized, 1) == 0)
        {
            SQLitePCL.Batteries_V2.Init();
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        if (_connection is not null)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
            if (_connection is not null)
            {
                return;
            }

            EnsureSqlCipherInitialized();

            var dbPath = _pathProvider.GetDatabasePath();
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var hexKey = await _tokenStore.GetOrCreateSqlCipherKeyHexAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(hexKey))
            {
                throw new InvalidOperationException("SQLCipher encryption key cannot be empty.");
            }

            var connString = new SQLiteConnectionString(
                dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
                storeDateTimeAsTicks: true,
                key: hexKey);

            var conn = new SQLiteAsyncConnection(connString);
            _ = await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL;").ConfigureAwait(false);
            _ = await conn.ExecuteScalarAsync<string>("PRAGMA busy_timeout=5000;").ConfigureAwait(false);
            await conn.ExecuteAsync("PRAGMA foreign_keys=ON;").ConfigureAwait(false);

            await conn.CreateTableAsync<CacheSyncState>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedDealer>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedMotorcycleModel>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedCustomerVehicle>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedLoyaltyWallet>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedCustomerVoucher>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedActiveAppointment>().ConfigureAwait(false);
            await conn.CreateTableAsync<CachedReceiptSnapshot>().ConfigureAwait(false);
            await conn.CreateTableAsync<OfflineMutationQueueItem>().ConfigureAwait(false);

            _connection = conn;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> ListTableNamesAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.QueryAsync<TableNameRow>(
            "SELECT name AS Name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")
            .ConfigureAwait(false);
        return rows.Select(r => r.Name).ToArray();
    }

    public async Task<string> GetJournalModeAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode;").ConfigureAwait(false);
    }

    public async Task UpsertSyncStateAsync(
        string ownerScopeKey,
        string resourceKey,
        DateTimeOffset syncedUtc,
        string? cursorToken = null,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var entity = new CacheSyncState
        {
            ScopeKey = $"{ownerScopeKey}|{resourceKey}",
            OwnerScopeKey = ownerScopeKey,
            ResourceKey = resourceKey,
            LastSyncedUtc = syncedUtc,
            CursorToken = cursorToken
        };

        await conn.InsertOrReplaceAsync(entity).ConfigureAwait(false);
    }

    public async Task<CacheSyncState?> GetSyncStateAsync(
        string ownerScopeKey,
        string resourceKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var key = $"{ownerScopeKey}|{resourceKey}";
        return await conn.FindAsync<CacheSyncState>(key).ConfigureAwait(false);
    }

    public async Task SaveLastAuthenticatedUserAsync(
        CurrentUserDto user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var json = JsonSerializer.Serialize(user, MobileJsonSerializerContext.Default.CurrentUserDto);
        await UpsertSyncStateAsync(
            SessionProfileOwnerScope,
            SessionProfileResourceKey,
            DateTimeOffset.UtcNow,
            cursorToken: json,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<CurrentUserDto?> GetLastAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetSyncStateAsync(
            SessionProfileOwnerScope,
            SessionProfileResourceKey,
            cancellationToken).ConfigureAwait(false);

        if (state is null || string.IsNullOrWhiteSpace(state.CursorToken))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(state.CursorToken, MobileJsonSerializerContext.Default.CurrentUserDto);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task ClearLastAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var key = $"{SessionProfileOwnerScope}|{SessionProfileResourceKey}";
        await conn.DeleteAsync<CacheSyncState>(key).ConfigureAwait(false);
    }

    public async Task SaveDealersAsync(IReadOnlyList<DealerDto> dealers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dealers);
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.DeleteAll<CachedDealer>();
            foreach (var d in dealers)
            {
                syncConn.InsertOrReplace(new CachedDealer
                {
                    Id = d.Id.ToString("D"),
                    Code = d.Code,
                    Name = d.Name,
                    City = d.City,
                    Address = d.Address,
                    PhoneNumber = d.PhoneNumber,
                    ContactEmail = d.ContactEmail,
                    Latitude = d.Latitude.HasValue ? (double)d.Latitude.Value : null,
                    Longitude = d.Longitude.HasValue ? (double)d.Longitude.Value : null,
                    OpeningTime = d.OpeningTime,
                    ClosingTime = d.ClosingTime,
                    DefaultSlotCapacity = d.DefaultSlotCapacity,
                    AvailableVinCount = d.AvailableVinCount,
                    IsActive = d.IsActive,
                    CreatedAtUtc = d.CreatedAtUtc,
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(PublicOwnerScope, "dealers", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DealerDto>> GetCachedDealersAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedDealer>().ToListAsync().ConfigureAwait(false);
        return rows.Select(r => new DealerDto(
            Id: Guid.Parse(r.Id),
            Code: r.Code,
            Name: r.Name,
            City: r.City,
            Address: r.Address,
            PhoneNumber: r.PhoneNumber,
            ContactEmail: r.ContactEmail,
            Latitude: r.Latitude.HasValue ? (decimal)r.Latitude.Value : null,
            Longitude: r.Longitude.HasValue ? (decimal)r.Longitude.Value : null,
            OpeningTime: r.OpeningTime,
            ClosingTime: r.ClosingTime,
            DefaultSlotCapacity: r.DefaultSlotCapacity,
            AvailableVinCount: r.AvailableVinCount,
            IsActive: r.IsActive,
            CreatedAtUtc: r.CreatedAtUtc)).ToArray();
    }

    public async Task SaveMotorcycleModelsAsync(IReadOnlyList<MotorcycleModelDto> models, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(models);
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.DeleteAll<CachedMotorcycleModel>();
            foreach (var m in models)
            {
                syncConn.InsertOrReplace(new CachedMotorcycleModel
                {
                    Id = m.Id.ToString("D"),
                    ModelCode = m.ModelCode,
                    Brand = m.Brand,
                    ModelName = m.ModelName,
                    Category = m.Category,
                    EngineDisplacementCc = m.EngineDisplacementCc,
                    BaseRetailPrice = (double)m.BaseRetailPrice,
                    WarrantyMonths = m.WarrantyMonths,
                    WarrantyMileageKm = m.WarrantyMileageKm,
                    ImageUrl = m.ImageUrl,
                    SpecificationsJson = m.SpecificationsJson,
                    AvailableStockCount = m.AvailableStockCount,
                    IsActive = m.IsActive,
                    MaintenanceRulesJson = JsonSerializer.Serialize(
                        m.MaintenanceRules,
                        MobileJsonSerializerContext.Default.IReadOnlyListMaintenanceRuleDto),
                    DealerStockJson = JsonSerializer.Serialize(
                        m.DealerStockBreakdown,
                        MobileJsonSerializerContext.Default.IReadOnlyListDealerModelStockSummaryDto),
                    CreatedAtUtc = m.CreatedAtUtc,
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(PublicOwnerScope, "motorcycles", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MotorcycleModelDto>> GetCachedMotorcycleModelsAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedMotorcycleModel>().ToListAsync().ConfigureAwait(false);
        return rows.Select(r =>
        {
            var rules = SafeDeserialize(r.MaintenanceRulesJson, MobileJsonSerializerContext.Default.IReadOnlyListMaintenanceRuleDto) ?? [];
            var stock = SafeDeserialize(r.DealerStockJson, MobileJsonSerializerContext.Default.IReadOnlyListDealerModelStockSummaryDto) ?? [];

            return new MotorcycleModelDto(
                Id: Guid.Parse(r.Id),
                ModelCode: r.ModelCode,
                Brand: r.Brand,
                ModelName: r.ModelName,
                Category: r.Category,
                EngineDisplacementCc: r.EngineDisplacementCc,
                BaseRetailPrice: (decimal)r.BaseRetailPrice,
                WarrantyMonths: r.WarrantyMonths,
                WarrantyMileageKm: r.WarrantyMileageKm,
                ImageUrl: r.ImageUrl,
                SpecificationsJson: r.SpecificationsJson,
                AvailableStockCount: r.AvailableStockCount,
                IsActive: r.IsActive,
                CreatedAtUtc: r.CreatedAtUtc,
                MaintenanceRules: rules,
                DealerStockBreakdown: stock);
        }).ToArray();
    }

    public async Task SaveLoyaltyTiersAsync(
        IReadOnlyList<LoyaltyTierDto> tiers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        var json = JsonSerializer.Serialize(
            tiers,
            MobileJsonSerializerContext.Default.IReadOnlyListLoyaltyTierDto);
        await UpsertSyncStateAsync(
            LoyaltyTiersOwnerScope,
            LoyaltyTiersResourceKey,
            DateTimeOffset.UtcNow,
            cursorToken: json,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LoyaltyTierDto>> GetLoyaltyTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var state = await GetSyncStateAsync(
            LoyaltyTiersOwnerScope,
            LoyaltyTiersResourceKey,
            cancellationToken).ConfigureAwait(false);

        if (state is null || string.IsNullOrWhiteSpace(state.CursorToken))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(
                state.CursorToken,
                MobileJsonSerializerContext.Default.IReadOnlyListLoyaltyTierDto) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SaveCustomerVehiclesAsync(
        string ownerScopeKey,
        IReadOnlyList<CustomerVehicleDto> vehicles,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(vehicles);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.Execute("DELETE FROM CachedCustomerVehicle WHERE OwnerScopeKey = ?;", ownerScopeKey);
            foreach (var v in vehicles)
            {
                syncConn.InsertOrReplace(new CachedCustomerVehicle
                {
                    Key = $"{ownerScopeKey}|{v.Id:D}",
                    OwnerScopeKey = ownerScopeKey,
                    VehicleId = v.Id.ToString("D"),
                    Vin = v.Vin,
                    MotorcycleModelName = v.MotorcycleModelName,
                    LicensePlate = v.LicensePlate,
                    CurrentMileageKm = v.CurrentMileageKm,
                    NextDueMileageKm = v.NextDueMileageKm,
                    MaintenanceStatus = v.MaintenanceStatus,
                    IsDueSoonOrOverdue = v.IsDueSoonOrOverdue,
                    PayloadJson = JsonSerializer.Serialize(v, MobileJsonSerializerContext.Default.CustomerVehicleDto),
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(ownerScopeKey, "vehicles", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerVehicleDto>> GetCachedCustomerVehiclesAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedCustomerVehicle>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey)
            .ToListAsync()
            .ConfigureAwait(false);

        var list = new List<CustomerVehicleDto>(rows.Count);
        foreach (var row in rows)
        {
            var dto = SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.CustomerVehicleDto);
            if (dto is not null)
            {
                list.Add(dto);
            }
        }

        return list;
    }

    public async Task SaveLoyaltyWalletAsync(
        string ownerScopeKey,
        LoyaltyAccountSummaryDto wallet,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(wallet);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var entity = new CachedLoyaltyWallet
        {
            OwnerScopeKey = ownerScopeKey,
            AccountId = wallet.AccountId.ToString("D"),
            TierCode = wallet.TierCode,
            PointsMultiplier = (double)wallet.PointsMultiplier,
            AvailablePoints = wallet.AvailablePoints,
            LifetimeQualifyingPoints = wallet.LifetimeQualifyingPoints,
            LifetimeSpentVnd = (double)wallet.LifetimeSpentVnd,
            PayloadJson = JsonSerializer.Serialize(wallet, MobileJsonSerializerContext.Default.LoyaltyAccountSummaryDto),
            CachedAtUtc = now
        };

        await conn.InsertOrReplaceAsync(entity).ConfigureAwait(false);
        await UpsertSyncStateAsync(ownerScopeKey, "loyalty_wallet", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<LoyaltyAccountSummaryDto?> GetCachedLoyaltyWalletAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await conn.FindAsync<CachedLoyaltyWallet>(ownerScopeKey).ConfigureAwait(false);
        if (row is null || string.IsNullOrWhiteSpace(row.PayloadJson))
        {
            return null;
        }

        return SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.LoyaltyAccountSummaryDto);
    }

    public async Task SaveCustomerVouchersAsync(
        string ownerScopeKey,
        IReadOnlyList<CustomerVoucherDto> vouchers,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(vouchers);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.Execute("DELETE FROM CachedCustomerVoucher WHERE OwnerScopeKey = ?;", ownerScopeKey);
            foreach (var v in vouchers)
            {
                syncConn.InsertOrReplace(new CachedCustomerVoucher
                {
                    Key = $"{ownerScopeKey}|{v.Id:D}",
                    OwnerScopeKey = ownerScopeKey,
                    VoucherId = v.Id.ToString("D"),
                    VoucherCode = v.VoucherCode,
                    Status = v.EffectiveStatus,
                    ApplicableScope = v.ApplicableScope,
                    ExpiresAtUtc = v.ExpiresAtUtc,
                    PayloadJson = JsonSerializer.Serialize(v, MobileJsonSerializerContext.Default.CustomerVoucherDto),
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(ownerScopeKey, "vouchers", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomerVoucherDto>> GetCachedCustomerVouchersAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedCustomerVoucher>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey)
            .ToListAsync()
            .ConfigureAwait(false);

        var list = new List<CustomerVoucherDto>(rows.Count);
        foreach (var row in rows)
        {
            var dto = SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.CustomerVoucherDto);
            if (dto is not null)
            {
                list.Add(dto);
            }
        }

        return list;
    }

    public async Task SaveActiveBookingsAsync(
        string ownerScopeKey,
        IReadOnlyList<BookingDto> bookings,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(bookings);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.Execute(
                "DELETE FROM CachedActiveAppointment WHERE OwnerScopeKey = ? AND AppointmentKind = ?;",
                ownerScopeKey,
                BookingAppointmentKind);

            foreach (var b in bookings)
            {
                syncConn.InsertOrReplace(new CachedActiveAppointment
                {
                    Key = $"{ownerScopeKey}|{BookingAppointmentKind}|{b.BookingId:D}",
                    OwnerScopeKey = ownerScopeKey,
                    AppointmentKind = BookingAppointmentKind,
                    AppointmentId = b.BookingId.ToString("D"),
                    BookingNumber = b.BookingNumber,
                    Status = b.Status,
                    SlotDateIso = b.SlotDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PayloadJson = JsonSerializer.Serialize(b, MobileJsonSerializerContext.Default.BookingDto),
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(ownerScopeKey, "bookings", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BookingDto>> GetCachedActiveBookingsAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedActiveAppointment>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey && x.AppointmentKind == BookingAppointmentKind)
            .ToListAsync()
            .ConfigureAwait(false);

        var list = new List<BookingDto>(rows.Count);
        foreach (var row in rows)
        {
            var dto = SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.BookingDto);
            if (dto is not null)
            {
                list.Add(dto);
            }
        }

        return list;
    }

    public async Task SaveActiveTestDrivesAsync(
        string ownerScopeKey,
        IReadOnlyList<TestDriveBookingDto> testDrives,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(testDrives);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        await conn.RunInTransactionAsync(syncConn =>
        {
            syncConn.Execute(
                "DELETE FROM CachedActiveAppointment WHERE OwnerScopeKey = ? AND AppointmentKind = ?;",
                ownerScopeKey,
                TestDriveAppointmentKind);

            foreach (var td in testDrives)
            {
                syncConn.InsertOrReplace(new CachedActiveAppointment
                {
                    Key = $"{ownerScopeKey}|{TestDriveAppointmentKind}|{td.TestDriveId:D}",
                    OwnerScopeKey = ownerScopeKey,
                    AppointmentKind = TestDriveAppointmentKind,
                    AppointmentId = td.TestDriveId.ToString("D"),
                    BookingNumber = td.BookingNumber,
                    Status = td.Status,
                    SlotDateIso = td.PreferredDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PayloadJson = JsonSerializer.Serialize(td, MobileJsonSerializerContext.Default.TestDriveBookingDto),
                    CachedAtUtc = now
                });
            }
        }).ConfigureAwait(false);

        await UpsertSyncStateAsync(ownerScopeKey, "test_drives", now, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TestDriveBookingDto>> GetCachedActiveTestDrivesAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await conn.Table<CachedActiveAppointment>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey && x.AppointmentKind == TestDriveAppointmentKind)
            .ToListAsync()
            .ConfigureAwait(false);

        var list = new List<TestDriveBookingDto>(rows.Count);
        foreach (var row in rows)
        {
            var dto = SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.TestDriveBookingDto);
            if (dto is not null)
            {
                list.Add(dto);
            }
        }

        return list;
    }

    public async Task SaveServiceReceiptAsync(
        string ownerScopeKey,
        PrintableServiceReceiptDto receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(receipt);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var entity = new CachedReceiptSnapshot
        {
            Key = $"{ownerScopeKey}|{ServiceReceiptKind}|{receipt.InvoiceId:D}",
            OwnerScopeKey = ownerScopeKey,
            ReceiptKind = ServiceReceiptKind,
            DocumentId = receipt.InvoiceId.ToString("D"),
            ReceiptNumber = receipt.ReceiptNumber,
            VerificationCode = receipt.VerificationCode,
            PayloadJson = JsonSerializer.Serialize(receipt, MobileJsonSerializerContext.Default.PrintableServiceReceiptDto),
            IssuedAtUtc = receipt.IssuedAtUtc,
            CachedAtUtc = DateTimeOffset.UtcNow
        };

        await conn.InsertOrReplaceAsync(entity).ConfigureAwait(false);
    }

    public async Task<PrintableServiceReceiptDto?> GetCachedServiceReceiptAsync(
        string ownerScopeKey,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var key = $"{ownerScopeKey}|{ServiceReceiptKind}|{invoiceId:D}";
        var row = await conn.FindAsync<CachedReceiptSnapshot>(key).ConfigureAwait(false);
        return row is null
            ? null
            : SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.PrintableServiceReceiptDto);
    }

    public async Task SaveSalesReceiptAsync(
        string ownerScopeKey,
        PrintableSalesOrderReceiptDto receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerScopeKey);
        ArgumentNullException.ThrowIfNull(receipt);

        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var entity = new CachedReceiptSnapshot
        {
            Key = $"{ownerScopeKey}|{SalesReceiptKind}|{receipt.SalesOrderId:D}",
            OwnerScopeKey = ownerScopeKey,
            ReceiptKind = SalesReceiptKind,
            DocumentId = receipt.SalesOrderId.ToString("D"),
            ReceiptNumber = receipt.ReceiptNumber,
            VerificationCode = receipt.VerificationCode,
            PayloadJson = JsonSerializer.Serialize(receipt, MobileJsonSerializerContext.Default.PrintableSalesOrderReceiptDto),
            IssuedAtUtc = receipt.IssuedAtUtc,
            CachedAtUtc = DateTimeOffset.UtcNow
        };

        await conn.InsertOrReplaceAsync(entity).ConfigureAwait(false);
    }

    public async Task<PrintableSalesOrderReceiptDto?> GetCachedSalesReceiptAsync(
        string ownerScopeKey,
        Guid salesOrderId,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var key = $"{ownerScopeKey}|{SalesReceiptKind}|{salesOrderId:D}";
        var row = await conn.FindAsync<CachedReceiptSnapshot>(key).ConfigureAwait(false);
        return row is null
            ? null
            : SafeDeserialize(row.PayloadJson, MobileJsonSerializerContext.Default.PrintableSalesOrderReceiptDto);
    }

    public async Task<IReadOnlyList<CachedReceiptSnapshot>> ListCachedReceiptSnapshotsAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.Table<CachedReceiptSnapshot>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey)
            .OrderByDescending(x => x.IssuedAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task EnqueueOutboxItemAsync(
        OfflineMutationQueueItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.InsertOrReplaceAsync(item).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OfflineMutationQueueItem>> GetPendingOutboxItemsAsync(
        string ownerScopeKey,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var items = await conn.Table<OfflineMutationQueueItem>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        return items
            .Where(x => x.NextRetryAfterUtc is null || x.NextRetryAfterUtc.Value <= nowUtc)
            .ToArray();
    }

    public async Task<int> GetOutboxCountAsync(
        string ownerScopeKey,
        CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await conn.Table<OfflineMutationQueueItem>()
            .Where(x => x.OwnerScopeKey == ownerScopeKey)
            .CountAsync()
            .ConfigureAwait(false);
    }

    public async Task UpdateOutboxItemAsync(
        OfflineMutationQueueItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.UpdateAsync(item).ConfigureAwait(false);
    }

    public async Task DeleteOutboxItemAsync(string id, CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.DeleteAsync<OfflineMutationQueueItem>(id).ConfigureAwait(false);
    }

    public async Task PurgeUserScopedDataAsync(CancellationToken cancellationToken = default)
    {
        var conn = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await conn.RunInTransactionAsync(syncConn =>
        {
            // Preserve public showroom tables: CachedDealer, CachedMotorcycleModel, and public CacheSyncState rows (including loyalty_tiers)!
            syncConn.Execute(
                "DELETE FROM CacheSyncState WHERE OwnerScopeKey NOT IN (?, ?);",
                PublicOwnerScope,
                LoyaltyTiersOwnerScope);
            syncConn.DeleteAll<CachedCustomerVehicle>();
            syncConn.DeleteAll<CachedLoyaltyWallet>();
            syncConn.DeleteAll<CachedCustomerVoucher>();
            syncConn.DeleteAll<CachedActiveAppointment>();
            syncConn.DeleteAll<CachedReceiptSnapshot>();
            syncConn.DeleteAll<OfflineMutationQueueItem>();
        }).ConfigureAwait(false);
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        if (_connection is null)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        return _connection!;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await _initLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            var conn = _connection;
            _connection = null;
            if (conn is not null)
            {
                await conn.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _initLock.Release();
            _initLock.Dispose();
        }
    }

    private static T? SafeDeserialize<T>(string? json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private sealed class TableNameRow
    {
        public string Name { get; set; } = string.Empty;
    }
}
