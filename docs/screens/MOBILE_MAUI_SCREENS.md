# Native Android & iOS `.NET 10 MAUI` Field App Screen Specifications (`v1.3.3`)

| Metadata | Canonical Value |
| :--- | :--- |
| **Document ID** | `UX-MOB-001-V1.3.3` |
| **Platform & Targets** | Native C# `.NET 10 MAUI` (**`net10.0-android;net10.0-ios`**, Android API `26`–`35`, Apple iOS `15.0+`, `ApplicationId: vn.motorcycledealer.mobile`, Display Title: `Apex Moto`) |
| **XAML, Toolkit & MVVM Engine** | `.NET 10` XAML `SourceGen` (`<MauiXamlInflator>SourceGen</MauiXamlInflator>`) · `100%` Compiled Bindings (`x:DataType`) · **`CommunityToolkit.Maui 13.0`** (`StatusBarBehavior`, `Expander`, `TouchBehavior`, `EmailValidationBehavior`, `Toast`/`Snackbar`, official converters) · `CommunityToolkit.Mvvm` C# 13 `partial` `[ObservableProperty]` & `WeakReferenceMessenger` · `FluentValidation 11.11` · `Humanizer.Core 2.14.1` · `Microsoft.Extensions.Http.Resilience` (Polly v8) |
| **Screen Inventory** | **`16` Native Compiled Views** (`4` Public/Gate + `5` Customer Tabs + `6` DealerStaff Field Views + `1` Shared Real-Time Notifications Feed) + **`1` Modal Camera2 / AVFoundation VIN/QR Scanner** |
| **Role Scope & Data Integrity** | **2-Role Field Super-App (100% Zero-Mock Real-Data)**: Anonymous `Public Showroom` (`3` tabs), `Customer` (`20` perms, `5` tabs), `DealerStaff` (`22` perms `DealerId`-scoped, `5` tabs + pushed Invoice Builder); `Admin` (`32` perms) redirects to Web HQ (`AdminWebConsoleNoticePage`) |
| **Companion Specifications** | [DESIGN_SYSTEM_AND_NAVIGATION.md](./DESIGN_SYSTEM_AND_NAVIGATION.md) · [PRD.md](../PRD.md) (`FR-10`) · [ARCHITECTURE.md](../ARCHITECTURE.md) (`ADR-007`) · [DESIGN.md](../DESIGN.md) · [DD_04_SCREEN_SPECIFICATIONS.md](../detail-design/DD_04_SCREEN_SPECIFICATIONS.md) · [mobile/AGENTS.md](../../mobile/AGENTS.md) |

---

## 1. Role-Adaptive `AppShell` Navigation Architecture (`AppShell.xaml`)

`MotorcycleDealer.Mobile.App` uses a single declarative **`AppShell`** (`FlyoutBehavior="Disabled"`, bound to `AppShellViewModel`) that dynamically switches active `TabBar` visibility based on `MobileSessionState` and enforces route guards inside `AppShell.OnNavigating` via `MobileRoleGuard.CanAccessRoute(route, session)`:

```mermaid
flowchart TD
    Boot["App Launch -> App.CreateWindow -> AppShellViewModel.BootstrapSessionCommand"]
    Boot -->|"Anonymous / Logged Out"| PubBar["PublicTabBar (3 Tabs)"]
    Boot -->|"Role == Customer (20 perms)"| CusBar["CustomerTabBar (5 Tabs + Bell)"]
    Boot -->|"Role == DealerStaff (22 perms, DealerId != Empty)"| StfBar["StaffTabBar (5 Tabs + Bell)"]
    Boot -->|"Role == Admin (32 perms)"| AdmBar["AdminTabBar (Web HQ Gate)"]

    PubBar --> P1["SCR-MOB-PUB-01: Showroom (//public/showroom)"]
    PubBar --> P2["SCR-MOB-PUB-02: Dealers (//public/dealers)"]
    PubBar --> P3["SCR-MOB-PUB-03: Sign In (//public/auth)"]
    AdmBar --> P4["SCR-MOB-PUB-04: Admin HQ Notice (//admin/web-notice)"]

    CusBar --> C1["SCR-MOB-CUS-01: Home (//customer/home)"]
    CusBar --> C2["SCR-MOB-CUS-02: Garage (//customer/garage)"]
    CusBar --> C3["SCR-MOB-CUS-03: Book (//customer/bookings)"]
    CusBar --> C4["SCR-MOB-CUS-04: Loyalty (//customer/loyalty)"]
    CusBar --> C5["SCR-MOB-CUS-05: Warranty (//customer/warranty)"]

    StfBar --> S1["SCR-MOB-STF-01: Bay Ops (//staff/workbench)"]
    StfBar --> S3["SCR-MOB-STF-03: VIN Sales (//staff/sales)"]
    StfBar --> S4["SCR-MOB-STF-04: Rides (//staff/test-drives)"]
    StfBar --> S5["SCR-MOB-STF-05: QA (//staff/warranty-csat)"]
    StfBar --> S6["SCR-MOB-STF-06: Slots (//staff/slots)"]
    S1 -->|"Push Route (bookingId)"| S2["SCR-MOB-STF-02: Invoice Builder (staff-invoice-builder)"]

    CusBar & StfBar -->|"Top Bell Toolbar"| Bell["SCR-MOB-CUS-06 / Shared: NotificationsFeedPage (notifications)"]
    C2 & S1 & S2 & S3 -->|"Modal Push"| Scan["CameraBarcodeScannerPage (barcode-scanner)"]
```

- **App Lifecycle, Messaging & SignalR Coordination (`App.xaml.cs` & `MauiProgram.cs`)**:
  - `Window.Created` triggers `AppShellViewModel.BootstrapSessionCommand` to restore platform secure tokens (`MauiSecureTokenStore` over Android Keystore / Apple iOS Keychain), hydrate `MobileSessionState`, verify `/api/v1/auth/me` over Polly v8 idempotent resilience (`DealerMobileResilience`), or fall back gracefully to encrypted `SQLCipher` offline snapshots, and connect `SignalRMobileService`.
  - Cross-ViewModel mutations broadcast typed `WeakReferenceMessenger` (`IMessenger`) events (`BookingMutatedMessage`, `WalletMutatedMessage`, `VehicleMutatedMessage`, `WarrantyMutatedMessage`, `TestDriveMutatedMessage`) and surface native `CommunityToolkit.Maui 13.0` **`Toast` / `Snackbar`** feedback (`MauiUserFeedbackService`: `#020617` Carbon / `#F8FAFC` White / `#DC2626` Crimson).
  - `Window.Stopped` invokes `ISignalRMobileService.OnAppBackgroundedAsync()` to pause WebSockets cleanly without draining battery.
  - `Window.Resumed` invokes `ISignalRMobileService.OnAppResumedAsync()` to reconnect `/hubs/notifications`, fetch delta unread events via `GET /api/v1/notifications?sinceUtc=...`, and flush allow-listed offline outbox items.

---

## 2. Motorsport Adaptive Design System (Light Showroom `<->` Dark Pit-Lane)

Defined in `Resources/Styles/MotorsportColors.xaml` and `Resources/Styles/MotorsportStyles.xaml` using `AppThemeBinding` for WCAG 2.1 Level AA (`>= 4.5:1`) parity with `frontend/src/styles/design-tokens.css`:

| Token Category | Light Showroom Mode | Dark Pit-Lane Mode | Usage & Ergonomic Standard |
| :--- | :--- | :--- | :--- |
| **Brand Motorsport Crimson** | `#DC2626` (`#B91C1C` pressed, `#FEF2F2` soft) | `#DC2626` (`#B91C1C` pressed, `#450A0A` soft) | Primary CTAs, active tab indicator, racing apex chevron (`appiconfg.svg`) |
| **App Canvas / Card / Sunken** | `#F8FAFC` / `#FFFFFF` / `#F1F5F9` | `#020617` / `#0F172A` / `#1E293B` | High-legibility outdoor showroom & dark workshop pit-lane surfaces |
| **Borders & Radii** | `#E2E8F0` subtle, `#94A3B8` strong | `#334155` subtle, `#475569` strong | Crisp `1dp` hairline borders; `6dp` badges, `8dp` buttons/inputs, `12dp` cards (`RoundRectangle`) |
| **Typography & Slashed-Zero Mono** | `#0F172A` primary, `#334155` secondary, `#64748B` muted | `#F8FAFC` primary, `#CBD5E1` secondary, `#94A3B8` muted | `JetBrains Mono` / tabular `Monospace` for 17-char `VIN`, VND prices, point balances, and `RCP-*` hashes |
| **4 Loyalty Tier Metallics** | `Bronze` (`#B45309`), `Silver` (`#64748B`), `Gold` (`#CA8A04`), `Platinum` (`#0284C7`) | Same metallic accents on dark brushed card gradients | `LoyaltyPassbookCardView` & tier pills |
| **Semantic Status Triplets** | `Success` (`#15803D`/`#DCFCE7`), `Warning` (`#92400E`/`#FEF3C7`), `Danger` (`#991B1B`/`#FEE2E2`), `Info` (`#1E40AF`/`#DBEAFE`), `CheckedIn` (`#155E75`/`#CFFAFE`), `InProgress` (`#1D4ED8`/`#EFF6FF`) | High-contrast dark-surface triplets (`#86EFAC`/`#052E16`, `#FDE68A`/`#451A03`, `#FCA5A5`/`#450A0A`, `#93C5FD`/`#172554`, `#67E8F9`/`#083344`, `#93C5FD`/`#1E3A8A`) | `StatusBadgeView` across bookings, test-drives, warranty claims, and online/offline indicators |
| **Touch Target & Field Ergonomics** | **`>= 48dp` minimum interactive height** (`MinimumHeightRequest="48"` on all buttons, inputs, pills, and swipe actions) | Same `>= 48dp` floor | One-handed glove-friendly workshop bay operation; zero AI-purple/beige styling |

---

## 3. Reusable Compiled (`x:DataType`) Controls & Hardware Modules (`Controls/`)

| Control / Module | Source File(s) | Capabilities & Lifecycle Hygiene |
| :--- | :--- | :--- |
| **`StatusBadgeView`** | `Controls/StatusBadgeView.xaml(.cs)` | Maps canonical backend enum strings (`Pending`, `Confirmed`, `CheckedIn`, `InProgress`, `Completed`, `Cancelled`, `NoShow`, `Available`, `ReservedOnBooking`, `Used`, `Expired`, `Submitted`, `UnderReview`, `Approved`, `Rejected`, `Bronze`..`Platinum`) to adaptive WCAG AA badge pills. |
| **`ShimmerCardView`** | `Controls/ShimmerCardView.xaml(.cs)` | Skeleton loading placeholder with opacity animation that **explicitly calls `this.AbortAnimation(...)` on `Unloaded`** to prevent retained native animation leaks (`RISK-04` mobile parity). |
| **`LoyaltyPassbookCardView`** | `Controls/LoyaltyPassbookCardView.xaml(.cs)` | Brushed metallic 4-tier digital membership card with **`toolkit:TouchBehavior`** (`PressedScale="0.97"`), strictly separating spendable **`AvailablePoints`** from **`LifetimeQualifyingPoints`**, computing next-tier progress dynamically from `GET /api/v1/loyalty/tiers` (zero hardcoded thresholds), and showing active `PointsMultiplier`. |
| **`QrCodeDisplayView` + `QrCodeMatrixEncoder`** | `Controls/QrCodeDisplayView.xaml(.cs)`, `Controls/QrCodeMatrixEncoder.cs` | **100% offline, AOT-safe pure C# ISO/IEC 18004 QR matrix encoder** (`Microsoft.Maui.Graphics.IDrawable`) rendering crisp scannable QR codes inside basement service bays with zero signal for `dealer://voucher/v1`, `dealer://booking/v1`, and `dealer://receipt/v1` payloads. |
| **`CameraBarcodeScannerPage`** | `Controls/CameraBarcodeScannerPage.xaml(.cs)` | Dual-platform 1D/2D scanner (`ZXing.Net.Maui.Controls`: Android **Camera2** + Apple iOS **AVFoundation `AVCaptureDevice`** check) with torch toggle, laser reticle, and haptic confirmation decoding ISO 3779 17-char `VIN` labels (`^[A-HJ-NPR-Z0-9]{17}$`) and `dealer://` deep-link QRs, paired with a neutral **manual VIN/QR input fallback** (zero sample VIN presets) for Android Emulators & iOS Simulators without a physical camera. |
| **Cross-Platform `CommunityToolkit.Maui 13.0` Behaviors & Converters** | `App.xaml`, `Services/MauiUserFeedbackService.cs`, `Views/**` | Official `toolkit:InvertedBoolConverter`, `toolkit:IsStringNotNullOrEmptyConverter`, `toolkit:IsNotNullConverter`; **`toolkit:StatusBarBehavior`** (`#020617` / `LightContent`) on 5 primary pages; **`toolkit:Expander`** on `LoginRegisterPage` & `ShowroomCatalogPage`; **`toolkit:TouchBehavior`** (`0.97` scale / `0.88` opacity) on CTA & quick-action cards; **`toolkit:EmailValidationBehavior`** on Sign-In; native **`Toast` & `Snackbar`** alerts. |

---

## 4. Complete 16-Screen Native Android & iOS Catalog (`SCR-MOB-*`)

### 4.1 Public Showroom & Role Gate Screens (`SCR-MOB-PUB-01` .. `SCR-MOB-PUB-04`)

| Screen ID | MAUI Page & Shell Route | ViewModel (`Mobile.Core`) | Role / Permission Scope | Key UI Capabilities, Offline Cache & API Bindings |
| :--- | :--- | :--- | :--- | :--- |
| **`SCR-MOB-PUB-01`** | `ShowroomCatalogPage`<br>`//public/showroom` | `ShowroomCatalogViewModel` | Anonymous / `Permissions.Showroom.View` | Searchable motorcycle catalog (`GET /api/v1/motorcycles`) with `Humanizer` dynamic model/branch/unit counts (`"6 models across 3 dealership branches"`), **`toolkit:StatusBarBehavior`**, **`toolkit:Expander` (`IsExpanded="True"`, OEM Coverage & Branch Stock Breakdown)**, **`toolkit:TouchBehavior`** primary CTA, VND MSRP formatting, pull-to-refresh, and offline `CachedMotorcycleModel` fallback (`SQLCipher`). |
| **`SCR-MOB-PUB-02`** | `DealerLocatorPage`<br>`//public/dealers` | `DealerLocatorViewModel` | Anonymous / `Permissions.Dealers.ViewPublic` | Regional dealership directory (`GET /api/v1/dealers`) hydrated dynamically from live SQL Server branches with city filter, opening hours (`Asia/Ho_Chi_Minh`), `Humanizer` bay/unit counts, one-tap native Android/iOS phone dialer & maps launcher (`tel:`/`maps:`), and offline `CachedDealer` fallback. |
| **`SCR-MOB-PUB-03`** | `LoginRegisterPage`<br>`//public/auth` | `LoginRegisterViewModel` | Anonymous | 100% real-data Sign In / Customer Registration form (`POST /api/v1/auth/login`, `POST /api/v1/auth/register`; **zero hardcoded demo presets**) with **`toolkit:StatusBarBehavior`**, **`toolkit:EmailValidationBehavior`** real-time visual cue, **`FluentValidation`** (`LoginInputValidator` / `RegisterInputValidator`), RFC 7807 inline errors, native **`Toast`** feedback, and a collapsed **`toolkit:Expander` (`IsExpanded="False"`, `"Backend API Endpoint & Health Check ▾"`)** exposing `10.0.2.2:5080` (Android Emulator) / `localhost:5080` (ADB reverse & iOS Simulator `IosSimulatorBaseUrl`) + live `/api/v1/health` probe. |
| **`SCR-MOB-PUB-04`** | `AdminWebConsoleNoticePage`<br>`//admin/web-notice` | `AdminWebConsoleNoticeViewModel` | `Admin` (`32` perms) | Intercepted automatically by `MobileRoleGuard` when an `Admin` signs in on mobile; explains that network-wide `Chart.js` analytics and catalog administration live on the AngularJS Web HQ console (`http://localhost:4200`), with `[Open Web Console]` browser launcher and device-safe `[Sign Out]` CTA. |

### 4.2 Customer Field Portal Screens (`SCR-MOB-CUS-01` .. `SCR-MOB-CUS-06`)

| Screen ID | MAUI Page & Shell Route | ViewModel (`Mobile.Core`) | Required Permissions (`Customer = 20`) | Key UI Capabilities, Offline Cache, SignalR & Hardware Integration |
| :--- | :--- | :--- | :--- | :--- |
| **`SCR-MOB-CUS-01`** | `CustomerHomePage`<br>`//customer/home` | `CustomerHomeViewModel` | `Vehicles.ViewOwn`, `Bookings.ViewOwn`, `Loyalty.ViewOwn` | Compact `LoyaltyPassbookCardView`, proactive due/overdue VIN maintenance alert strip, active booking/test-drive cards (`CachedActiveAppointment`), SignalR live connection badge, unread notification bell button, and quick-action shortcuts. |
| **`SCR-MOB-CUS-02`** | `VinGaragePage`<br>`//customer/garage` | `VinGarageViewModel` | `Vehicles.ViewOwn`, `Vehicles.RegisterOwn` | Owned `CustomerVehicles` fleet (`GET /api/v1/vehicles/my` + `CachedCustomerVehicle`), **Camera2 17-char ISO 3779 VIN barcode scan** into external bike registration (`POST /api/v1/vehicles`), monotonic odometer update (`PATCH /mileage` — queues into `OfflineMutationQueueItem` when offline, surfaces `422 ODOMETER_REGRESSION`), and OEM `MaintenanceRules` / `MaintenanceReminders` drawer. |
| **`SCR-MOB-CUS-03`** | `BookingWizardPage`<br>`//customer/bookings` | `BookingWizardViewModel` | `Bookings.Create`, `Bookings.ViewOwn`, `Bookings.CancelOwn`, `TimeSlots.ViewAvailable`, `Invoices.ViewOwn`, `Reviews.CreateOwn` | Unified **3-Step Workshop Booking Wizard** (online-only `ADR-005` with `Idempotency-Key` & `RowVersion`; preserves draft & auto-reloads live slots on **`409 SLOT_CAPACITY_EXHAUSTED`**), active/historical bookings list with **offline Check-In QR (`dealer://booking/v1`)**, **5-Star CSAT Review sheet (`POST /api/v1/service-reviews` awarding `+25` `AvailablePoints` ONLY)**, and **printable/shareable SHA-256 receipt (`RCP-SVC-*`)** cached with infinite TTL in `CachedReceiptSnapshot`. |
| **`SCR-MOB-CUS-04`** | `LoyaltyPassbookPage`<br>`//customer/loyalty` | `LoyaltyPassbookViewModel` | `Loyalty.ViewOwn`, `Loyalty.RedeemVoucher`, `SalesOrders.ViewOwn` | Full-bleed metallic `LoyaltyPassbookCardView` (`CachedLoyaltyWallet`), **My Vouchers tab with offline `QrCodeDisplayView` (`dealer://voucher/v1?code=...&id=...&rv=...` from `CachedCustomerVoucher`)**, online-only Reward Catalog redemption (`POST /api/v1/vouchers/redeem` with `Idempotency-Key`), immutable `PointLedgerEntries` feed (including `+25` `ServiceReviewBonus`), and Showroom `SalesOrders` receipts (`RCP-SLS-*`). |
| **`SCR-MOB-CUS-05`** | `CustomerWarrantyPage`<br>`//customer/warranty` | `CustomerWarrantyViewModel` | `Warranty.ViewOwn`, `Warranty.CreateOwn`, `Reviews.ViewOwn` | `36`-month / `30,000`-km OEM warranty eligibility meter per VIN, claim submission form (`POST /api/v1/warranty-claims`, offline outbox eligible with `409`/`422` drop guard), live adjudication status timeline reacting to SignalR `WarrantyClaimStatusChanged`, and covered OEM `SparePart` / VND details. |
| **`SCR-MOB-CUS-06`** *(Shared Bell)* | `NotificationsFeedPage`<br>`notifications` | `NotificationsFeedViewModel` | `Notifications.ViewOwn` *(+ `ViewDealer` for `DealerStaff`)* | Dual-section **Alerts Feed + Live Activity Logs** view: persistent SignalR `10`-event inbox (`GET /api/v1/notifications` / `/dealer`, `Unread Only`, offline-queued read actions) + live **User Action Audit Trail** switching between On-Device logs (`user_actions.ndjson`, Android `logcat` / iOS Console) and Server-correlated multi-platform trail (`GET /api/v1/activity-logs/my`) with `[Sync Logs]`. |

### 4.3 Dealer Staff Field Workbench Screens (`SCR-MOB-STF-01` .. `SCR-MOB-STF-06`)

All six `DealerStaff` screens are strictly scoped to the signed-in advisor's `DealerId` (`IDealerScoped` server enforcement + `OwnerScopeKey` local `SQLCipher` partitioning) and display live SignalR status:

| Screen ID | MAUI Page & Shell Route | ViewModel (`Mobile.Core`) | Required Permissions (`DealerStaff = 22`) | Key Field Capabilities, Camera2 Scan & SignalR Behavior |
| :--- | :--- | :--- | :--- | :--- |
| **`SCR-MOB-STF-01`** | `StaffWorkbenchPage`<br>`//staff/workbench` | `StaffWorkbenchViewModel` | `Bookings.ViewDealer`, `Bookings.UpdateStatus`, `Vehicles.ViewDealer` | Segmented live workshop lanes (`Pending`, `Confirmed`, `CheckedIn`, `InProgress`, `Completed`) prepending incoming **`BookingCreated`** alerts via SignalR with zero manual refresh; Android **`SwipeView` bay check-in & status transitions** (`RowVersion`-guarded); **Camera2 scan of customer Booking QR (`dealer://booking/v1`) or 17-char chassis VIN** for instant lookup; 1-tap `[Settle Invoice]` navigation to `SCR-MOB-STF-02`. |
| **`SCR-MOB-STF-02`** | `MobileInvoiceBuilderPage`<br>`staff-invoice-builder?bookingId=` | `MobileInvoiceBuilderViewModel` | `Invoices.CreateDealer`, `Invoices.ViewDealer`, `Loyalty.ViewDealerCustomer` | Pushed via `IQueryAttributable` (`bookingId`) from `SCR-MOB-STF-01`. Interactive model-compatible **OEM `SpareParts` line-item picker** + labor charges, **Camera2 Voucher QR scan (`dealer://voucher/v1`)**, sticky bottom bar computing live **`TwoStepLoyaltyPreviewCalculator`** (`Floor(Floor(NetPaidAmount / 10000m) * TierMultiplier)`) and tier-upgrade preview, online-only `POST /api/v1/service-invoices` settlement (`ADR-003`), and immediate offline-cached `RCP-SVC-*` QR & native Android Share Sheet export. |
| **`SCR-MOB-STF-03`** | `ShowroomVinSalesPage`<br>`//staff/sales` | `ShowroomVinSalesViewModel` | `Inventories.ViewDealer`, `Inventories.ManageDealer`, `SalesOrders.Create`, `SalesOrders.ViewDealer` | Branch `DealerInventories` (`VIN`) browser + **Camera2 17-char ISO 3779 VIN barcode intake** (`POST /api/v1/inventories`), atomic Showroom **`SalesOrders` checkout** (`POST /api/v1/sales-orders`, online-only, `RowVersion` protected, auto-onboarding `CustomerVehicles` & accruing points), and instant `RCP-SLS-*` receipt QR/share. |
| **`SCR-MOB-STF-04`** | `StaffTestDrivePage`<br>`//staff/test-drives` | `StaffTestDriveViewModel` | `TestDrives.ViewDealer`, `TestDrives.UpdateStatus` | Live showroom test-drive queue prepending incoming SignalR **`TestDriveCreated`** cards without refresh, branch `Available` demo VIN picker (`RowVersion`-guarded `PATCH /api/v1/test-drives/{id}/status`), `Confirmed -> Completed` post-sale VIN continuity, and 1-tap handoff to `SCR-MOB-STF-03`. |
| **`SCR-MOB-STF-05`** | `StaffWarrantyCsatPage`<br>`//staff/warranty-csat` | `StaffWarrantyCsatViewModel` | `Warranty.ViewDealer`, `Warranty.ManageDealer`, `Reviews.ViewDealer`, `Reviews.RespondDealer` | Dual-tab live quality desk receiving SignalR **`WarrantyClaimCreated`** and **`ServiceReviewSubmitted`** alerts; `RowVersion`-guarded warranty claim adjudication (`UnderReview`, `Approved`, `Completed`, `Rejected` with compatible OEM `CoveredSparePartId` & VND coverage) and official dealership CSAT review responses (`PATCH /api/v1/service-reviews/{id}/response`). |
| **`SCR-MOB-STF-06`** | `SlotCapacityPage`<br>`//staff/slots` | `SlotCapacityViewModel` | `TimeSlots.ManageDealer`, `TimeSlots.ViewAvailable` | Branch daily workshop bay capacity grid (`GET /api/v1/time-slots/dealer`), batch slot generator (`POST /api/v1/time-slots/dealer/generate`), and `RowVersion`-guarded capacity/block stepper (`PATCH /api/v1/time-slots/{slotId}`, handling `409 CAPACITY_BELOW_BOOKED` / `CONCURRENCY_CONFLICT`). |
