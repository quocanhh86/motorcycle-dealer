# Mobile Engineering Rules — MotorcycleDealer (.NET 10 MAUI Android & iOS)

Scoped rulebook for the native **C# `.NET 10 MAUI` Android & iOS Field App** (`mobile/`, `v1.3.3`, `net10.0-android;net10.0-ios`, Android API `26`–`35`, iOS `15.0+`, `ApplicationId: vn.motorcycledealer.mobile`) of the **Motorcycle Dealer & Loyalty Management Platform**. Inherits root [AGENTS.md](../AGENTS.md).

## 1. 2-Role Field Super-App Scope, Zero-Mock & 48-Permission Parity

1. **Roles & Views (`16` Screens)**:
   - **Public Showroom (`3` tabs)**: `ShowroomCatalogPage` (`toolkit:Expander` OEM/stock panel + `Humanizer` dynamic counts), `DealerLocatorPage`, `LoginRegisterPage` (100% real-data sign-in with zero demo preset buttons, collapsed `toolkit:Expander` endpoint health panel, `EmailValidationBehavior` & `FluentValidation`).
   - **Customer (`20` perms, `5` tabs)**: `CustomerHomePage`, `VinGaragePage`, `BookingWizardPage` (`409` reload + `+25 pts` CSAT + receipt share), `LoyaltyPassbookPage` (live `/loyalty/tiers` progress card, offline QR vouchers, ledger, receipts), `CustomerWarrantyPage`.
   - **DealerStaff (`22` perms, `DealerId`-scoped, `5` tabs)**: `StaffWorkbenchPage` (bay swipe check-in + VIN scanner), `MobileInvoiceBuilderPage` (OEM parts + voucher QR + live two-step floor preview), `ShowroomVinSalesPage`, `StaffTestDrivePage`, `StaffWarrantyCsatPage`, `SlotCapacityPage`.
   - **Shared**: `NotificationsFeedPage` (top bell feed + live `Activity Logs` on-device & server-correlated user action viewer for `Customer` & `DealerStaff`).
2. **Admin Web-Only Redirect**: `Admin` (`32` perms) is redirected on login by `MobileRoleGuard` to `AdminWebConsoleNoticePage` (Web HQ AngularJS console only — never build Admin analytics/CRUD tables on mobile).
3. **RBAC Parity**: `mobile/src/MotorcycleDealer.Mobile.Core/Contracts/Permissions.cs` MUST match all `48` strings in `backend/src/MotorcycleDealer.Domain/Constants/Permissions.cs` 1-for-1. Server `[HasPermission]` and `8`-table `IDealerScoped` checks remain authoritative.

## 2. Inward Architecture & Build Props (`ADR-001`, `ADR-007`)

Shared `mobile/Directory.Build.props` sets `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=13.0` (NEVER hardcode singular `<TargetFramework>`).

| Project | TFM | Allowed Dependencies | Prohibited |
| :--- | :--- | :--- | :--- |
| `src/MotorcycleDealer.Mobile.Core` | `net10.0` | `CommunityToolkit.Mvvm` (`WeakReferenceMessenger`), `FluentValidation 11.11`, `Microsoft.Extensions.Http.Resilience` (Polly v8), `Humanizer.Core 2.14`, `SignalR.Client`, `sqlite-net-sqlcipher` | `<UseMaui>true</UseMaui>`, `Microsoft.Maui.*`, Android/iOS UI |
| `src/MotorcycleDealer.Mobile.App` | `net10.0-android;net10.0-ios` | `Mobile.Core`, `Microsoft.Maui.Controls`, `CommunityToolkit.Maui 13.0` (`StatusBarBehavior`, `Expander`, `TouchBehavior`, `EmailValidationBehavior`, `Toast`/`Snackbar`), `ZXing.Net.Maui.Controls`, Android/iOS platform APIs | EF Core / SQL Server, untyped XAML bindings, hardcoded demo presets |
| `tests/MotorcycleDealer.Mobile.UnitTests` | `net10.0` | `Mobile.Core`, `xUnit`, fakes/mocks | `<UseMaui>true</UseMaui>`, emulator dependency |

## 3. `.NET 10` XAML `SourceGen`, C# 13 MVVM & Toolkit Hygiene

- **XAML SourceGen & `x:DataType`**: Enable `<MauiXamlInflator>SourceGen</MauiXamlInflator>` in `Mobile.App.csproj`. Every `ContentPage`, `ContentView`, and `DataTemplate` MUST declare `x:DataType="..."` (zero reflection bindings).
- **C# 13 `partial` MVVM & Messaging**: Use `[ObservableProperty] public partial T Prop { get; set; }`, `[RelayCommand]` on `async Task`, `FluentValidation` (`AbstractValidator<T>`), and `WeakReferenceMessenger` (`IMessenger`) for cross-VM invalidation. Legacy `_field` `[ObservableProperty]` declarations are **prohibited**.
- **Official Toolkit Components & AOT**: Prefer `CommunityToolkit.Maui 13.0` (`StatusBarBehavior`, `Expander`, `TouchBehavior`, `EmailValidationBehavior`, `Toast`/`Snackbar`, `InvertedBoolConverter`, `IsStringNotNullOrEmptyConverter`, `IsNotNullConverter`). Register all DTOs in `MobileJsonSerializerContext`; Debug/Release builds MUST pass with `TreatWarningsAsErrors=true`.

## 4. Security, Resilience & Multi-Platform Hardening (`Android + iOS`)

1. **Single-Flight `401` Refresh + Polly v8 Resilience (`ADR-006`/`ADR-007`)**: `SingleFlightTokenRefreshHandler` (`SemaphoreSlim(1,1)` over handler-free `"AuthRefreshClient"`) wraps inner Polly v8 `DealerMobileResilience` (`Microsoft.Extensions.Http.Resilience`: retry `2x` exp+jitter **strictly on idempotent `GET`/`HEAD`/`OPTIONS` or explicit `Idempotency-Key`**, circuit breaker `30s/0.5/6/15s`, `15s` attempt timeout). Immediately triggers `ISessionResetCoordinator.ForceSignOutAsync()` on `403 ACCOUNT_LOCKED`/`STAFF_DEALER_UNASSIGNED`.
2. **Device-Scoped Logout**: `LogoutCoordinator` posts `LogoutRequest { RefreshToken, RevokeAllDevices = false }` by default (keeping Web SPA active) and wipes `SecureStorage` (`MauiSecureTokenStore`) + user/dealer `SQLCipher` rows.
3. **Android Hardening**: Android Keystore (`SecureStorage`), `android:allowBackup="false"`, zero global `usesCleartextTraffic="true"`, HTTPS-only `<base-config cleartextTrafficPermitted="false" />` (`network_security_config.xml` cleartext limited to `10.0.2.2`, `localhost`, `127.0.0.1`).
4. **Apple iOS Hardening (`Platforms/iOS/`)**: iOS `15.0+`, Keychain `Entitlements.plist` (`keychain-access-groups`), strict ATS in `Info.plist` (`NSAllowsLocalNetworking=true` ONLY, NEVER `NSAllowsArbitraryLoads=true`), `NSCameraUsageDescription`, `UIStatusBarStyleLightContent`, `AppDelegate` foreground banner delegate, `IosLocalNotificationService` (`UNUserNotificationCenter`), and `AVCaptureDevice` simulator camera fallback (`IosSimulatorBaseUrl = http://localhost:5080`).
5. **Encrypted `9`-Table `SQLCipher` WAL Cache & Online-Only Law**: Partition user tables by `OwnerScopeKey`; NEVER queue `SalesOrders`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `PointLedgerEntries`, `RowVersion` slot/test-drive reservations, or voucher redemptions offline. Outbox permits ONLY `NotificationRead`, `NotificationReadAll`, `VehicleMileageUpdate`, `ServiceReviewSubmit`, and `WarrantyClaimSubmit`.
6. **10-Event SignalR (`ADR-004`)**: Subscribe to all `10` `/hubs/notifications` events, pause in `Window.Stopped`, catch up via `sinceUtc` in `Window.Resumed`, and surface native Android `NotificationChannel` / iOS `UNUserNotificationCenter` alerts.
7. **End-to-End Correlated User Action Logging (`v1.3.4`)**: `UserActionLogger` + `UserActionHttpLoggingHandler` (`Mobile.Core/Logging/`) attach `X-Correlation-Id`, `X-Client-Platform` (`MobileAndroid`/`MobileIos`), `X-Client-Session-Id`, and `X-Client-Screen` on every HTTP/SignalR call, emit structured logs to Android `logcat` (`MotorcycleDealer.UserAction`) / iOS Console + rolling `user_actions.ndjson` (preserving exact `9` `SQLCipher` tables), batch-sync to `POST /api/v1/activity-logs`, and never log passwords or tokens.

## 5. Motorsport Adaptive Theme (WCAG 2.1 AA)

- `#DC2626` Crimson, Light Showroom (`#F8FAFC`/`#FFFFFF`) `<->` Dark Pit-Lane (`#020617`/`#0F172A`) via `AppThemeBinding`, `StatusBarBehavior` (`#020617`/`LightContent`), `JetBrains Mono` for VIN/VND/Points, `>= 48dp` touch targets, `6/8/12dp` radii, `1dp` borders.

## 6. Verification Commands

```powershell
node .agents/scripts/verify-mobile-maui.mjs                                   # 8/8 guardrails (incl. iOS + Toolkit)
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Release -f net10.0-ios -t:Compile -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1 -BootEmulator -SetupAdbReverse
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-mobile-android.ps1 -Configuration Debug
node .agents/scripts/verify-post-task-sync.mjs                                # 11/11 Post-Task Sync Gate
```
