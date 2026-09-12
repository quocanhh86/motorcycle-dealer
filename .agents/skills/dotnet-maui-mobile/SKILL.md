---
name: dotnet-maui-mobile
description: >-
  Use this skill when building, refactoring, debugging, or verifying the native C# .NET 10 MAUI
  Android Field App (`mobile/`, `net10.0-android`, API 26-35, `vn.motorcycledealer.mobile`),
  pure `net10.0` `MotorcycleDealer.Mobile.Core` ViewModels (`CommunityToolkit.Mvvm` C# 13
  `partial` `[ObservableProperty]`), `.NET 10` XAML `SourceGen` with mandatory `x:DataType`,
  `SingleFlightTokenRefreshHandler`, encrypted 9-table `SQLCipher` WAL offline cache,
  `ZXing.Net.Maui` Camera2 VIN/QR scanning, 10-event SignalR `/hubs/notifications` + Android
  `NotificationChannel` / `Plugin.LocalNotification` alerts, or WHPX `Pixel_8_API_35` emulator
  and ADB smoke automation ("Tạo ứng dụng Android .NET 10 MAUI", "Quét mã VIN / QR bằng camera",
  "Cache offline SQLCipher", "Thông báo đẩy SignalR Android", "Chạy giả lập Pixel_8_API_35 ADB").
---

# `.NET 10 MAUI` Android Field App Playbook (`v1.3.0` · `2`-Role Super-App · `16` Views · `9` SQLCipher Tables)

Authoritative mobile engineering skill for the **Motorcycle Dealer & Loyalty Management Platform** (`Phase 10`, `T-1001`..`T-1008`, `ADR-001`..`ADR-006`). Complements [AGENTS.md](../../../AGENTS.md) and [mobile/AGENTS.md](../../../mobile/AGENTS.md).

---

## 1. Phase-Gate & Solution Topology

Before modifying files under `mobile/`, confirm Phase 10 compliance via `motorcycle-dealer-guardrails` (`check_phase_gate`). Keep `mobile/` strictly decoupled from `backend/` and `frontend/`:

```text
mobile/
├── AGENTS.md                                      # Scoped mobile governance (< 8,000 B, zero YAML frontmatter)
├── Directory.Build.props                          # Nullable=enable, ImplicitUsings=enable, TreatWarningsAsErrors=true, LangVersion=13.0
├── MotorcycleDealer.Mobile.sln                    # References Mobile.Core, Mobile.App, Mobile.UnitTests
├── src/
│   ├── MotorcycleDealer.Mobile.Core/              # Pure net10.0 (ZERO <UseMaui>, ZERO Microsoft.Maui.*)
│   │   ├── Contracts/                             # Permissions.cs (48), ApiEnvelopes.cs, MobileDtos.cs, MobileJsonSerializerContext.cs
│   │   ├── Security/                              # SingleFlightTokenRefreshHandler, LogoutCoordinator, MobileRoleGuard, MobileSessionState
│   │   ├── Domain/                                # Iso3779VinValidator, TwoStepLoyaltyPreviewCalculator, DealerQrPayloadParser, VietnamFormatting
│   │   ├── Persistence/                           # OfflineEntities.cs (9 tables), MobileOfflineDb (SQLCipher WAL), OfflineOutboxSyncService
│   │   ├── Api/                                   # IDealerMobileApiClient & DealerMobileApiClient (/api/v1/*)
│   │   ├── Realtime/                              # SignalRMobileService (10 events) + platform port interfaces
│   │   └── ViewModels/                            # 16 Screen ViewModels + AppShellViewModel (CommunityToolkit.Mvvm C# 13 partial)
│   └── MotorcycleDealer.Mobile.App/               # net10.0-android (API 26..35, <UseMaui>true</UseMaui>, SourceGen)
│       ├── Platforms/Android/                     # AndroidManifest.xml (allowBackup=false), network_security_config.xml, Services/
│       ├── Resources/                             # Adaptive AppIcon, Splash, MotorsportColors.xaml, MotorsportStyles.xaml
│       ├── Controls/                              # StatusBadgeView, ShimmerCardView, LoyaltyPassbookCardView, QrCodeDisplayView, CameraBarcodeScannerPage
│       └── Views/                                 # Public/ (4), Customer/ (5), Staff/ (6), Shared/ (1) = 16 compiled x:DataType pages
└── tests/
    └── MotorcycleDealer.Mobile.UnitTests/         # Pure net10.0 xUnit suite (>= 65 unit & boundary [Theory] tests)
```

---

## 2. 2-Role Field Super-App Scope & 16 Mobile Views

| Role / Shell Mode | Tabs / Entry | Permissions | Screens |
| :--- | :--- | :--- | :--- |
| **Public Showroom** | `3` Tabs | Anonymous (`Showroom.View`, `Dealers.ViewPublic`) | `ShowroomCatalogPage`, `DealerLocatorPage`, `LoginRegisterPage` (with Dev quick-fill + `10.0.2.2:5080`/`localhost:5080` preset) |
| **Customer** | `5` Tabs + Bell | `20` Canonical Permissions (`CustomerDefaults`) | `CustomerHomePage`, `VinGaragePage`, `BookingWizardPage` (`409` slot reload + `+25 pts` CSAT + receipt share), `LoyaltyPassbookPage`, `CustomerWarrantyPage`, `NotificationsFeedPage` |
| **DealerStaff** | `5` Tabs + Bell | `22` Canonical Permissions (`DealerId`-Scoped) | `StaffWorkbenchPage` (bay swipe check-in + VIN scanner), `MobileInvoiceBuilderPage` (OEM parts + QR voucher + two-step floor preview), `ShowroomVinSalesPage`, `StaffTestDrivePage`, `StaffWarrantyCsatPage`, `SlotCapacityPage`, `NotificationsFeedPage` |
| **Admin** | Web HQ Redirect | `32` Canonical Permissions | `AdminWebConsoleNoticePage` — explains mobile is field-focused and directs `Admin` to the AngularJS Web HQ console |

---

## 3. `.NET 10` XAML `SourceGen`, C# 13 `CommunityToolkit.Mvvm` & AOT Rules

1. **XAML SourceGen**: Set `<MauiXamlInflator>SourceGen</MauiXamlInflator>` in `MotorcycleDealer.Mobile.App.csproj`.
2. **Mandatory `x:DataType`**: Declare `x:DataType="vm:..."` (or model type) on every `ContentPage`, `ContentView`, and `DataTemplate`.
3. **C# 13 `partial` Properties Only**:
   ```csharp
   // CORRECT (C# 13 + CommunityToolkit.Mvvm 8.4+)
   [ObservableProperty]
   public partial bool IsBusy { get; set; }

   // FORBIDDEN (legacy backing field causes MVVMTK warnings / guardrail failure)
   // [ObservableProperty] private bool _isBusy;
   ```
4. **Source-Generated JSON (`System.Text.Json`)**: Register all request/response DTOs and `ApiEnvelope<T>` wrappers in `MobileJsonSerializerContext` so Release trimming and Android AOT never break serialization.
5. **Strict Warning Hygiene**: Compile both `Debug` and `Release` with `--warnaserror`.

---

## 4. Security, Single-Flight Refresh & Android Hardening

1. **`SingleFlightTokenRefreshHandler`**:
   - Uses a singleton `SemaphoreSlim(1, 1)` around `POST /api/v1/auth/refresh`.
   - Double-checks `currentAccessToken != failedBearerToken` inside the lock so 10 simultaneous `401` responses trigger **exactly 1** refresh token rotation (preventing `ReuseDetected` revocation).
   - Calls a dedicated handler-free `HttpClient` (`"AuthRefreshClient"`) and clones buffered `HttpRequestMessage` headers/content before retrying.
   - Immediately invokes `ISessionResetCoordinator.ForceSignOutAsync()` on `403 ACCOUNT_LOCKED`, `403 STAFF_DEALER_UNASSIGNED`, or failed refresh.
2. **Device-Safe Logout**: `LogoutCoordinator` posts `LogoutRequest { RefreshToken, RevokeAllDevices = false }` by default so the user's AngularJS Web SPA session remains active, then purges `SecureStorage` and user-scoped `SQLCipher` tables.
3. **Android Keystore & Transport Security**:
   - Store access/refresh tokens and the generated 256-bit `SQLCipher` database passphrase in `SecureStorage.Default` (Android Keystore).
   - `Platforms/Android/AndroidManifest.xml` MUST declare `android:allowBackup="false"` and NEVER set `android:usesCleartextTraffic="true"`.
   - `Platforms/Android/Resources/xml/network_security_config.xml` MUST set `<base-config cleartextTrafficPermitted="false" />` and restrict cleartext exclusively to loopback/emulator hosts (`10.0.2.2`, `localhost`, `127.0.0.1`).

---

## 5. Encrypted 9-Table `SQLCipher` WAL Cache & `ADR-003`/`ADR-005` Online-Only Law

1. **9 Local Tables (`MobileOfflineDb`)**:
   - Public (preserved across logout): `CacheSyncState`, `CachedDealer`, `CachedMotorcycleModel`.
   - Tenant/User-Scoped (keyed by `OwnerScopeKey = "{UserId}:{Role}:{DealerId ?? 'global'}"`, wiped on logout/`403`): `CachedCustomerVehicle`, `CachedLoyaltyWallet`, `CachedCustomerVoucher`, `CachedActiveAppointment`, `CachedReceiptSnapshot` (infinite TTL offline receipts), `OfflineMutationQueueItem`.
   - Always open with `SQLiteConnectionString(dbPath, storeDateTimeAsTicks: true, key: hexKey)` via `SQLitePCLRaw.bundle_e_sqlcipher` and execute `PRAGMA journal_mode=WAL;`.
2. **Strict Online-Only Boundary**:
   - **NEVER** queue `SalesOrders`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `PointLedgerEntries`, `RowVersion`-protected `ServiceBookings`/`TestDriveBookings` slot reservations, or `CustomerVouchers` redemptions offline — `OfflineOutboxSyncService` MUST reject them (`InvalidOperationException` / `OnlineOnlyViolation`).
   - Permit offline FIFO queueing ONLY for `NotificationRead`, `NotificationReadAll`, `VehicleMileageUpdate`, `ServiceReviewSubmit`, and `WarrantyClaimSubmit`, dropping cleanly on `409`/`422` and retrying with backoff on transient network/`5xx` failures.

---

## 6. `ZXing.Net.Maui` Camera2 VIN/QR Scanner & Offline QR Rendering

- **Scanner (`CameraBarcodeScannerPage`)**: Scans 1D/2D ISO 3779 17-character VIN chassis labels (`^[A-HJ-NPR-Z0-9]{17}$`, stripping/forbidding `I`, `O`, `Q`) and signed deep-link QR payloads:
  - Voucher QR: `dealer://voucher/v1?code=<CODE>&id=<GUID>&rv=<BASE64>`
  - Booking Check-In QR: `dealer://booking/v1?id=<GUID>&num=<BOOKING_NUM>&rv=<BASE64>`
  - Receipt Hash: 16-char SHA-256 `RCP-*` code
- **Emulator Fallback**: Always expose an instant manual 17-char VIN / QR payload input sheet inside `CameraBarcodeScannerPage` for Android emulators without a live webcam.
- **Offline QR Display (`QrCodeDisplayView`)**: Render crisp vector/graphics QR codes locally so customers can present `CustomerVouchers`, `ServiceBookings`, and `CachedReceiptSnapshot` codes inside basement service bays with zero signal.

---

## 7. `10`-Event SignalR Hub & Native Android Notifications

- Connect `SignalRMobileService` to `/hubs/notifications` using `AccessTokenProvider` with reconnect delays `[0, 2, 5, 10, 30]` seconds.
- Subscribe to all **`10` canonical events**: `BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`, `TestDriveCreated`, `TestDriveStatusChanged`, `ServiceReviewSubmitted`, `WarrantyClaimCreated`, `WarrantyClaimStatusChanged`.
- **Android Window Lifecycle (`App.xaml.cs`)**: Pause/disconnect gracefully in `Window.Stopped`; reconnect and reconcile missed events via `GET /api/v1/notifications?sinceUtc=...` in `Window.Resumed` and `HubConnection.Reconnected`.
- **Native Alerts**: Dispatch Android `NotificationChannel` notifications (`BookingAlerts`, `LoyaltyRewards`, `WarrantyUpdates`) via `AndroidX.Core.App.NotificationCompat` (or `Plugin.LocalNotification` when configured), requesting `POST_NOTIFICATIONS` on Android 13+ (API 33+).

---

## 8. Motorsport Adaptive Design System

- Exact WCAG 2.1 AA parity with `frontend/src/styles/design-tokens.css` plus `AppThemeBinding`:
  - Brand Crimson: `#DC2626` (`#B91C1C` pressed, `#FEF2F2` soft)
  - Light Showroom: `#F8FAFC` app / `#FFFFFF` card / `#0F172A` text
  - Dark Pit-Lane: `#020617` app / `#0F172A` card / `#F8FAFC` text
  - Tier Metallics: `Bronze`, `Silver`, `Gold`, `Platinum`; Semantic triplets (`Success`, `Warning`, `Danger`, `Info`, `CheckedIn`, `InProgress`).
  - Typography & Touch: `JetBrains Mono` slashed-zero for VIN/VND/Points; minimum `48dp` interactive height; `6dp`/`8dp`/`12dp` radii; crisp `1dp` borders; zero AI-purple/beige slop.

---

## 9. Windows WHPX `Pixel_8_API_35` Emulator & ADB Workflow

On Windows workstations, always pass explicit SDK/JDK paths to `dotnet build` for `-f net10.0-android`:

```powershell
# 1. Verify WHPX accel & provision Pixel_8_API_35 AVD (Android API 35 x86_64)
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1

# 2. Boot emulator & bind adb reverse tcp:5080 -> host WebApi
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1 -BootEmulator -SetupAdbReverse

# 3. Run static MAUI guardrails + pure net10.0 xUnit tests
node .agents/scripts/verify-mobile-maui.mjs
dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests/MotorcycleDealer.Mobile.UnitTests.csproj -c Release --warnaserror

# 4. Build Debug & Release Android APKs (0 warnings)
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Debug -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\openjdk\jdk-21.0.8" --warnaserror
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Release -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\openjdk\jdk-21.0.8" --warnaserror

# 5. Install APK, launch activity, verify pidof, dump uiautomator XML & capture PNG screenshot
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-mobile-android.ps1 -Configuration Debug

# 6. Run Phase 10 Quality-Loop & Post-Task Sync Gate
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase 10 -Round 1
node .agents/scripts/verify-post-task-sync.mjs
```
