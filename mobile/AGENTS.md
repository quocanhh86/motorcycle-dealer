# Mobile Engineering Rules — MotorcycleDealer (.NET 10 MAUI Android & iOS)

Scoped rulebook for the native **C# `.NET 10 MAUI` Android & iOS Field App** (`mobile/`, `v1.4.0`, `net10.0-android;net10.0-ios`, Android API `26`–`35`, iOS `15.0+`, `ApplicationId: vn.motorcycledealer.mobile`). Inherits [AGENTS.md](../AGENTS.md).

## 1. 2-Role Super-App Scope, Zero-Mock & 48-RBAC Parity

1. **Views (`16` Pages / `26` Compiled `x:DataType` Views)**:
   - **Public (`3` tabs)**: `ShowroomCatalogPage` (studio cards, `toolkit:Expander` OEM/stock, `Humanizer.Core.vi`, `VI | EN`/theme toggles), `DealerLocatorPage`, `LoginRegisterPage` (zero demo presets, collapsed endpoint `Expander`, `EmailValidationBehavior`, `FluentValidation`).
   - **Customer (`20` perms, `5` tabs)**: `CustomerHomePage`, `VinGaragePage`, `BookingWizardPage` (`409` reload, Phosphor 5-star `+25 pts` CSAT, receipts), `LoyaltyPassbookPage` (4-tier metallic foil + Lottie sparkle, `/loyalty/tiers`, offline QR vouchers, ledger), `CustomerWarrantyPage`.
   - **DealerStaff (`22` perms, `DealerId`-scoped, `5` tabs)**: `StaffWorkbenchPage` (workshop banner, bay swipe, VIN scan), `MobileInvoiceBuilderPage` (OEM parts, voucher QR, two-step floor preview), `ShowroomVinSalesPage`, `StaffTestDrivePage`, `StaffWarrantyCsatPage`, `SlotCapacityPage`.
   - **Shared**: `NotificationsFeedPage` (SignalR bell + correlated `Activity Logs`).
2. **Admin Gate & RBAC**: `Admin` (`32` perms) redirects via `MobileRoleGuard` to `AdminWebConsoleNoticePage` (Web HQ only). `Mobile.Core/Contracts/Permissions.cs` MUST match all `48` strings in `backend/.../Constants/Permissions.cs` 1-for-1; server `[HasPermission]` & `8`-table `IDealerScoped` are authoritative.

## 2. Architecture & Build Props (`ADR-001`, `ADR-007`)

`mobile/Directory.Build.props` sets `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=13.0` (NEVER set singular `<TargetFramework>`).

| Project | TFM | Allowed | Prohibited |
| :--- | :--- | :--- | :--- |
| `Mobile.Core` | `net10.0` | `CommunityToolkit.Mvvm` (`WeakReferenceMessenger`), `FluentValidation 11.11`, `Microsoft.Extensions.Http.Resilience` (Polly v8), `Humanizer.Core` + `Humanizer.Core.vi 2.14.1`, `SignalR.Client`, `sqlite-net-sqlcipher` | `<UseMaui>true</UseMaui>`, `Microsoft.Maui.*`, `SkiaSharp.*`, platform UI |
| `Mobile.App` | `net10.0-android;net10.0-ios` | `Mobile.Core`, `Microsoft.Maui.Controls`, `CommunityToolkit.Maui 13.0`, `SkiaSharp.Views.Maui.Controls 3.119.4`, `SkiaSharp.Extended.UI.Maui 3.0.0`, `ZXing.Net.Maui.Controls`, Android/iOS APIs | EF Core/SQL Server, untyped bindings, demo presets, raw UI emojis |
| `Mobile.UnitTests` | `net10.0` | `Mobile.Core`, `xUnit`, fakes/mocks | `<UseMaui>true</UseMaui>`, emulator dependency |

## 3. `.NET 10` XAML `SourceGen`, C# 13 MVVM, Toolkit `13.0` & `v1.4.0` UI

- **SourceGen, `x:DataType` & `.NET 10` Safe Areas**: Keep `<MauiXamlInflator>SourceGen</MauiXamlInflator>`. Every `ContentPage`, `ContentView`, and `DataTemplate` MUST set `x:DataType`. Use **`.NET 10` `SafeAreaEdges="All"`** across pages for zero-warning Android edge-to-edge & iOS notch/Home-Indicator insets.
- **C# 13 `partial` MVVM & AOT**: Use `[ObservableProperty] public partial T Prop { get; set; }` (legacy `_field` forbidden), `[RelayCommand]`, deferred `FluentValidation` `.WithMessage(_ => AppResources.*)`, `WeakReferenceMessenger`, and `MobileJsonSerializerContext`; Debug/Release MUST pass `--warnaserror`.
- **`CommunityToolkit.Maui 13.0` `TouchBehavior` `x:Reference` Rule**: `TouchBehavior` does **not** inherit `BindingContext` in Toolkit `13.0`. Always bind page commands via `Source={x:Reference PageRoot}, Path=ViewModel.<Cmd>` and `DataTemplate` params via `Source={x:Reference <CardRoot>}, Path=BindingContext, x:DataType=Border`. Also use `StatusBarBehavior`, `Expander`, `EmailValidationBehavior`, `Toast`/`Snackbar`, and official converters.
- **Fonts, Phosphor Icons, Studio Media & Lottie (`v1.4.0`)**:
  - Register **`9` bundled TTFs** in `MauiProgram.ConfigureFonts` (`SpaceGrotesk-{SemiBold,Bold}`, `BeVietnamPro-{Regular,Medium,SemiBold}`, `JetBrainsMono-{Medium,SemiBold}`, `Phosphor.ttf`, `Phosphor-Fill.ttf`; zero system `"Monospace"`).
  - Use **`Icons/PhosphorIcons.cs`** (`FontFamily="Phosphor"`/`"PhosphorFill"`) across `14/14` Shell tabs, toolbars, stars, carets & badges (**zero** raw `★`/`▾`/`→`/emoji hacks).
  - Resolve **`8` offline studio images** (`Resources/Images/{model_*,hero_*}.jpg`) at `0ms` via `MotorcycleImageResolver`.
  - Chain `.UseSkiaSharp()` and render **`4` bundled `Resources/Raw/lottie_*.json`** via `LottieOrIconView` (`SkiaSharp.Extended.UI.Maui` `SKLottieView`) with `Unloaded` stop & reduced-motion (`IMotionPreferencesService`) Phosphor fallback.
- **Bilingual `LocalizationService` + `Humanizer.Core.vi`**: Default `vi-VN` `<->` `en-US` live hot-switch via `803`-key `AppResources.{,vi.}resx`, `CultureChangedMessage`, `Accept-Language` header, `HumanizeStatus` Picker display bindings, and `IAppPreferencesStore` (MAUI `Preferences`, preserving exact `9` `SQLCipher` tables).

## 4. Security, Resilience & Multi-Platform Hardening (`Android + iOS`)

1. **`401` Refresh + Polly v8**: `SingleFlightTokenRefreshHandler` (`SemaphoreSlim(1,1)` over `"AuthRefreshClient"`) wraps `DealerMobileResilience` (`2x` exp+jitter retry strictly on `GET`/`HEAD`/`OPTIONS` or explicit `Idempotency-Key`, circuit breaker `30s/0.5/6/15s`, `15s` timeout); invokes `ForceSignOutAsync()` on `403 ACCOUNT_LOCKED`/`STAFF_DEALER_UNASSIGNED`.
2. **Device Logout & OS Hardening**: `LogoutCoordinator` defaults `RevokeAllDevices = false` and wipes `SecureStorage` + user/dealer `SQLCipher` rows. Android enforces Keystore, `allowBackup="false"`, zero global `usesCleartextTraffic="true"`, HTTPS `network_security_config.xml` (`10.0.2.2`/`localhost`/`127.0.0.1` only). iOS `15.0+` enforces Keychain `Entitlements.plist`, strict ATS (`NSAllowsLocalNetworking=true` only, NEVER `NSAllowsArbitraryLoads=true`), `NSCameraUsageDescription`, `UINavigationBarAppearance`/`UITabBarAppearance`, `IosLocalNotificationService`, `AVCaptureDevice` fallback.
3. **`9`-Table `SQLCipher` WAL & Online-Only Law**: Partition by `OwnerScopeKey`; NEVER queue `SalesOrders`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `PointLedgerEntries`, `RowVersion` slot/test-drive reservations, or voucher redemptions offline. Outbox allows ONLY `NotificationRead`, `NotificationReadAll`, `VehicleMileageUpdate`, `ServiceReviewSubmit`, and `WarrantyClaimSubmit`.
4. **10-Event SignalR & Correlated Logs**: Subscribe to all `10` `/hubs/notifications` events, pause in `Window.Stopped`, catch up via `sinceUtc` in `Window.Resumed`. `UserActionLogger` + `UserActionHttpLoggingHandler` emit `X-Correlation-Id`/`X-Client-*`/`Accept-Language`, rolling `user_actions.ndjson`, and `POST /api/v1/activity-logs` (zero secrets).

## 5. Motorsport Adaptive Theme (WCAG 2.1 AA)

`#DC2626` Crimson, Light Showroom (`#F8FAFC`/`#FFFFFF`) `<->` Dark Pit-Lane (`#020617`/`#0F172A`, `BorderStrongDark #64748B` `3.75:1` AA), Titanium Ice-Cyan Platinum (`#0C4A6E`/`#E0F2FE`/`#38BDF8`), InProgress Violet (`#5B21B6`/`#C4B5FD`), `DoubleBezelOuter`/`Inner`, `>= 48dp` touch targets.

## 6. Verification Commands

```powershell
node .agents/scripts/verify-mobile-maui.mjs
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Release -f net10.0-ios -t:Compile -warnaserror
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1 -BootEmulator -SetupAdbReverse
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-mobile-android.ps1 -Configuration Debug
node .agents/scripts/verify-post-task-sync.mjs
```
