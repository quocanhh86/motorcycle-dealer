# Developer Setup, Operations & Live Interview Demo Runbook

| Metadata | Canonical Value |
| :--- | :--- |
| **Document ID** | `OPS-RUNBOOK-001-V1.3.3` |
| **Project** | Motorcycle Dealer & Loyalty Management Platform ([GitHub Repository](https://github.com/quocanhh86/motorcycle-dealer)) |
| **Role & Timeline** | Full-Stack Developer (`Solo Project`, `02/2026 - 08/2026`) |
| **Release & Verification Baseline** | **`v1.3.3` (`10` Phases, `88` WBS Tasks)** · **`27` SQL Server 3NF Tables + `9` Mobile `SQLCipher` Tables** · **`48` RBAC Permissions (`20 / 22 / 32`)** · **`10` SignalR Events** · **`30` Web SPA Routes + `16` Native Android & iOS MAUI Screens (`net10.0-android;net10.0-ios`)** |
| **Default Local Endpoints** | Web API & SignalR Hub: `http://localhost:5080` (`10.0.2.2:5080` Android Emulator / `localhost:5080` `adb reverse` & iOS Simulator) · AngularJS 1.8.3 SPA: `http://localhost:4200` · Mobile App: `vn.motorcycledealer.mobile` (`Pixel_8_API_35` & iOS 15+) · SQL Server: `(localdb)\MSSQLLocalDB` (`MotorcycleDealerDb`) |
| **Companion Specifications** | [README.md](../README.md) · [ARCHITECTURE.md](./ARCHITECTURE.md) · [DATABASE_DESIGN.md](./DATABASE_DESIGN.md) · [DESIGN.md](./DESIGN.md) · [MOBILE_MAUI_SCREENS.md](./screens/MOBILE_MAUI_SCREENS.md) · [openapi.v1.yaml](./openapi.v1.yaml) · [TEST_PLAN.md](./TEST_PLAN.md) · [RELEASE_NOTES_v1.1.0.md](./RELEASE_NOTES_v1.1.0.md) · [RELEASE_NOTES_v1.2.0.md](./RELEASE_NOTES_v1.2.0.md) · [RELEASE_NOTES_v1.3.0.md](./RELEASE_NOTES_v1.3.0.md) · [PROGRESS.md](../PROGRESS.md) |

---

## 1. Environment & Runtime Prerequisites Matrix

> [!NOTE]
> **Verified `v1.2.0` Full-Stack Baseline**: Phases 1–8 (`T-101`..`T-808`) are delivered and Phase 9 (`T-901`..`T-908`) expands the platform to **`27` 3NF tables** (`+ServiceReviews`, `+WarrantyClaims`), **`48` RBAC permissions**, **`10` SignalR events**, **`30` UI-Router routes**, `SMK-01..SMK-10`, and `100%` `183`-day Git schedule synchronization.

| Component | Required Version | Verification Command | Local Development Default | Docker / Portable Alternative |
| :--- | :--- | :--- | :--- | :--- |
| **.NET SDK** | `.NET 10 SDK` (`10.0.x`) | `dotnet --version` | Kestrel `MotorcycleDealer.WebApi` on `http://localhost:5080` | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| **EF Core CLI** | `dotnet-ef` (`10.0.x`) | `dotnet ef --version` | Code-First migrations (`27` 3NF tables) in `MotorcycleDealer.Infrastructure` | Applied via `ensure-sqlserver-db.sh -m` |
| **SQL Server** | SQL Server `2022` / LocalDB `15+` | `sqllocaldb info MSSQLLocalDB` | `(localdb)\MSSQLLocalDB` · DB `MotorcycleDealerDb` (Windows Integrated Auth) | `mcr.microsoft.com/mssql/server:2022-latest` (`localhost,1433`) |
| **Node.js Runtime** | `Node.js 22 LTS` (`>= 22.0`) | `node --version` | Powers zero-dep `.agents/scripts/*.mjs` MCP/sync gates & SPA dev server (`http://localhost:4200`) | `node:22-alpine` (`USER node`) serving `frontend/` |
| **SQL CLI Utility** | `sqlcmd` | `sqlcmd -?` | Used by `sqlserver-localdb-inspector` Stdio MCP & `ensure-sqlserver-db.ps1` | `/opt/mssql-tools18/bin/sqlcmd` inside container |

### 1.1 Secrets & Configuration Hygiene (`Zero Secrets in Git`)

Never commit real credentials or signing keys to `appsettings.json`. Configure local overrides via `dotnet user-secrets` or environment variables:

```powershell
# Configure local user-secrets for MotorcycleDealer.WebApi
dotnet user-secrets set "Jwt:SigningKey" "DevOnly-384Bit-Cryptographic-Signing-Secret-ReplaceInProd-2026!" --project backend/src/MotorcycleDealer.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=MotorcycleDealerDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True" --project backend/src/MotorcycleDealer.WebApi
```

| Environment Variable | Default Development Value | Purpose |
| :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | `Server=(localdb)\MSSQLLocalDB;Database=MotorcycleDealerDb;...` | SQL Server 3NF persistence (`25` tables, `RCSI` enabled) |
| `Jwt__SigningKey` | *(User-Secret / Env, `>= 32` bytes / `256-bit` required)* | Signs `15m` JWT access tokens for REST & SignalR handshake |
| `Cors__AllowedOrigins__0` | `http://localhost:4200` | Explicit SPA origin required for SignalR `AllowCredentials()` & `WithExposedHeaders` |
| `RateLimiting__Auth__PermitLimit` | `30` (`per 60s` fixed window per IP) | Protects `/api/v1/auth/{login,register,refresh}` (`429 RATE_LIMIT_EXCEEDED` + `Retry-After`) |
| `ReminderWorker__ScanIntervalSeconds` | `900` (`15m` default; `15` in demo mode) | Polling cadence for `VinMaintenanceReminderWorker` |

---

## 2. Quick-Start Boot Procedures

### Option A — Native Windows `(localdb)\MSSQLLocalDB` Workflow (Recommended for Fast Dev & Live Demo)

```powershell
# Step 1: Verify LocalDB instance is running and apply 25-table EF Core migrations
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1 -Migrate

# Step 2 (Terminal 1): Start ASP.NET Core 10 WebApi + SignalR NotificationHub (7 events) + Reminder Worker
dotnet run --project backend/src/MotorcycleDealer.WebApi --urls "http://localhost:5080"

# Step 3 (Terminal 2): Start decoupled AngularJS 1.8.3 SPA (27 routes) on http://localhost:4200
cd frontend && npm start

# Step 4 (Terminal 3): Run 9-probe Smoke Suite (SMK-01..SMK-09), 541 xUnit, 56 SPA & Post-Task Sync Gate
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1
dotnet test backend/MotorcycleDealer.sln --configuration Release
npm.cmd --prefix frontend test
node .agents/scripts/verify-post-task-sync.mjs
```

### Option B — Full Docker Compose Stack (`SQL Server 2022` + `.NET 10 API` + `SPA`)

```powershell
# Provide ephemeral SA password via session environment variable (never committed to Git)
$env:MSSQL_SA_PASSWORD = "LocalDocker!StrongPwd2026"
docker compose up --build -d

# Verify SQL Server 2022 container readiness & migrations
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1 -UseDocker -Migrate
```

### Option C — Native Android & iOS `.NET 10 MAUI` Field App (`net10.0-android;net10.0-ios`, `v1.3.3`)

Requires `.NET 10` `maui` (`maui-android` + `maui-ios`) workloads, Android SDK (`C:\Program Files (x86)\Android\android-sdk`), OpenJDK 21 (`C:\Program Files\Android\openjdk\jdk-21.0.8`), and Windows Hypervisor Platform (`WHPX`) for Android emulation. For running the compiled iOS Simulator bundle interactively, macOS with Xcode `16+` & iOS `15.0+` SDK is required (Windows verifies full `net10.0-ios` C# / XAML `SourceGen` compilation directly).

```powershell
# Step 1: Verify WHPX acceleration, Android SDK/JDK & provision Pixel_8_API_35 AVD (Android API 35 x86_64)
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1

# Step 2: Boot Pixel_8_API_35 emulator & bind `adb reverse tcp:5080 tcp:5080` to live WebApi
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-maui-android.ps1 -BootEmulator -SetupAdbReverse
# Manual ADB reverse equivalent:
# & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" reverse tcp:5080 tcp:5080

# Step 3: Run 8 static MAUI guardrails (incl. [MAUI-IOS-AND-TOOLKIT]) & pure net10.0 Mobile.UnitTests
node .agents/scripts/verify-mobile-maui.mjs
dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests/MotorcycleDealer.Mobile.UnitTests.csproj -c Release --warnaserror

# Step 4: Compile Debug & Release Android APKs (`net10.0-android`, 0 warnings) with explicit SDK/JDK paths
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Debug -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\openjdk\jdk-21.0.8" --warnaserror
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Release -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\openjdk\jdk-21.0.8" --warnaserror

# Step 5: Verify Apple iOS (`net10.0-ios`, iOS 15.0+) compilation directly on Windows (0 warnings)
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Debug -f net10.0-ios -t:Compile -warnaserror
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Release -f net10.0-ios -t:Compile -warnaserror
# Full Windows iOS assembly build (emits bin/<Config>/net10.0-ios/iossimulator-x64/MotorcycleDealer.Mobile.App.dll):
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj -c Debug -f net10.0-ios -warnaserror

# Step 6 (Android Live Smoke): Install APK, launch vn.motorcycledealer.mobile, verify pidof, dump uiautomator XML & PNG
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-mobile-android.ps1 -Configuration Debug
```

**macOS Xcode iOS Simulator Run (`net10.0-ios`, `IosSimulatorBaseUrl = http://localhost:5080`)**:

```bash
# On macOS (Xcode 16+ & iOS Simulator runtime installed, backend API listening on http://localhost:5080):
xcrun simctl list devices available
dotnet build mobile/src/MotorcycleDealer.Mobile.App/MotorcycleDealer.Mobile.App.csproj \
  -t:Run -c Debug -f net10.0-ios -p:_DeviceName=:v2:udid=<IOS_SIMULATOR_UDID> --warnaserror
```

---

## 3. Pre-Seeded Demo Credentials, VINs & Vouchers Reference

On startup in `Development` (or via `--seed`), `DatabaseSeeder` idempotently provisions **3 dealerships**, **8 role-scoped accounts** (`40` canonical permissions: `Customer = 16`, `DealerStaff = 18`, `Admin = 28`), **18 showroom VINs**, **8 genuine OEM `SpareParts` SKUs** (`PRT-OIL-10W30`..`PRT-CVT-BLT01`), **6 garage vehicles**, **14 rolling days of workshop time slots**, **5 voucher catalog items**, **sample `TestDriveBookings`**, and **6 months of historical sales/service transactions**.

### 3.1 Seeded Regional Dealerships (`Dealers`)

| Branch Code | Dealership Name | City | Service Bays (`DefaultSlotCapacity`) | Operating Hours (`Asia/Ho_Chi_Minh`) | Deterministic Seed GUID |
| :--- | :--- | :--- | :---: | :---: | :--- |
| **`DLR-HN-01`** | Apex Moto Hanoi Flagship | Hanoi | `4` bays / hr | `08:00 – 18:00` | `22222222-2222-2222-2222-222222222202` |
| **`DLR-DN-01`** | Apex Moto Da Nang Coastal Hub | Da Nang | `4` bays / hr | `08:00 – 18:00` | `22222222-2222-2222-2222-222222222203` |
| **`DLR-HCM-01`** | Apex Moto Saigon Center | Ho Chi Minh City | `5` bays / hr | `08:00 – 19:00` | `22222222-2222-2222-2222-222222222201` |

### 3.2 Pre-Seeded Demo User Credentials Table

| Persona / Role | Login Email | Dev Password | Assigned Scope / Tier State | Default Entry Screen |
| :--- | :--- | :--- | :--- | :--- |
| **Platform Admin** | `admin@motorcycledealer.vn` | `Admin@123!` | Global cross-dealer (`DealerId = NULL`, `28` admin/operational permissions) | `app.admin.analytics` (`/admin/analytics`) |
| **Hanoi Dealer Staff** | `staff.hn@motorcycledealer.vn` *(alias `lan.tran.hn@dealer.vn`)* | `Staff@123!` | Scoped strictly to `DLR-HN-01` (`EMP-HN-001`, `18` staff permissions) | `app.staff.workbench` (`/staff/workbench`) |
| **Da Nang Dealer Staff** | `staff.dn@motorcycledealer.vn` | `Staff@123!` | Scoped strictly to `DLR-DN-01` (`EMP-DN-001`, `18` staff permissions) | `app.staff.workbench` (`/staff/workbench`) |
| **Saigon Dealer Staff** | `staff.hcm@motorcycledealer.vn` | `Staff@123!` | Scoped strictly to `DLR-HCM-01` (`EMP-HCM-001`, `18` staff permissions) | `app.staff.workbench` (`/staff/workbench`) |
| **Bronze Customer** | `minh.nguyen@customer.vn` *(alias `customer.bronze@motorcycledealer.vn`)* | `Customer@123!` | `Bronze` (`1.00x`, `16` customer permissions) · `Avail: 120 pts` · `Lifetime: 120 pts` | `app.customer.garage` (`/customer/garage`) |
| **Silver Customer** | `customer.silver@motorcycledealer.vn` | `Customer@123!` | `Silver` (`1.20x`, `16` customer permissions) · `Avail: 649 pts` · `Lifetime: 1,960 pts` *(40 pts from Gold!)* | `app.customer.garage` (`/customer/garage`) |
| **Gold Customer** | `customer.gold@motorcycledealer.vn` | `Customer@123!` | `Gold` (`1.50x`, `16` customer permissions) · `Avail: 1,450 pts` · `Lifetime: 3,200 pts` | `app.customer.garage` (`/customer/garage`) |
| **Platinum Customer** | `customer.platinum@motorcycledealer.vn` | `Customer@123!` | `Platinum` (`2.00x`, `16` customer permissions) · `Avail: 4,800 pts` · `Lifetime: 10,100 pts` | `app.customer.garage` (`/customer/garage`) |

### 3.3 Sample ISO 3779 17-Character `VIN`s for Live Testing

All sample VINs strictly satisfy `^[A-HJ-NPR-Z0-9]{17}$` (zero `I`, `O`, `Q` characters):

| `VIN` (17 Chars) | Model | Initial Seed State | Best Live Demo Use Case |
| :--- | :--- | :--- | :--- |
| `RLHJF5108RY000101` | Honda Air Blade 160 SE (`45,000,000 VND`) | `Available` in `DLR-HN-01` (`DealerInventories`) | Complete live Showroom `SalesOrder` -> instant `CustomerVehicles` onboarding + `Bronze -> Gold` jump. |
| `RLHJC9204RY000309` | Honda SH160i ABS Sport (`102,500,000 VND`) | `Available` in `DLR-HCM-01` (`DealerInventories`) | Complete flagship Showroom sale -> `+10,100` pts & instant `Platinum` promotion. |
| `RLHJF5109RZ104829` | Honda SH160i ABS Sport (`Plate: 59-A3 889.21`) | Owned by `customer.silver@...` (`3,850 km`, next due `4,000 km`) | Pre-positioned inside `200 km` lead window — triggers `VinMaintenanceReminderWorker` & slot booking demo! |
| `RLHKF4102RY000205` | Honda Winner X ABS (`Plate: 43-D1 552.18`) | Owned by `minh.nguyen@customer.vn` (`900 km`, next due `1,000 km`) | Ready for Da Nang / Saigon dual-tab SignalR `BookingCreated` demonstration. |
| `RLCUG1010PY092341` | Yamaha Exciter 155 VVA *(Unregistered External Bike)* | **Not yet in DB** | Paste directly into `[+ Register External Bike VIN]` modal during live demo (`POST /api/v1/vehicles`). |

### 3.4 Seeded `VoucherCatalogs` & Ready-to-Apply `CustomerVouchers`

| Catalog Prefix / Sample Code | Reward Title | Cost (`Pts`) | Discount Rule | `MinOrderAmount` | `MinTier` | Demo Status |
| :--- | :--- | ---: | :--- | ---: | :---: | :--- |
| `SVC50K` | `50,000 VND` Off Workshop Oil & Filter | `100` | `FixedAmount: 50,000 VND` | `250,000 VND` | `Bronze` | Active in Reward Store |
| `OIL100K` / `SVC100K-7K9P2M4X` | `100,000 VND` Off Periodic Maintenance | `200` | `FixedAmount: 100,000 VND` | `500,000 VND` | `Bronze` | Pre-redeemed (`Available`) in `customer.silver` wallet |
| `PARTS10P` / `SVC15PCT-4R8N2W9Q` | `15%` Off Workshop Service (Cap `200,000 VND`) | `300` | `Percentage: 15%` (`Max 200k`) | `600,000 VND` | `Silver` | Active in Reward Store |
| `GOLD15P` | `15%` Off Genuine Parts & Tires (Cap `400,000 VND`) | `600` | `Percentage: 15%` (`Max 400k`) | `1,000,000 VND` | `Gold` | Tier-gated (`Gold+`) |
| `PLATVIP300K` | `300,000 VND` VIP Priority Bay & Detailing | `900` | `FixedAmount: 300,000 VND` | `800,000 VND` | `Platinum` | Tier-gated (`Platinum`) |

---

## 4. 5-Minute Technical Interview Live Walkthrough Script

**Pre-Demo Setup (30 seconds before call)**:
1. Ensure API (`http://localhost:5080`) and SPA (`http://localhost:4200`) are running cleanly.
2. Open **two side-by-side browser windows** (`50%` screen width each, using separate browser profiles or Normal + Incognito so JWT sessions stay independent):
   - **Left Window (`Customer`)**: Logged in as `customer.silver@motorcycledealer.vn` (`Customer@123!`, currently at `1,960` `LifetimeQualifyingPoints` — just `40` pts shy of `Gold`!).
   - **Right Window (`Saigon Staff`)**: Logged in as `staff.hcm@motorcycledealer.vn` (`Staff@123!`) parked on **Live Workbench** (`/#!/staff/workbench`) showing the green `[LIVE SignalR: Connected]` badge.

---

### Minute `0:00 – 0:45` · Act 1: Granular 3-Role RBAC & Multi-Dealer Isolation (`FR-01`, `ADR-002`, `ADR-006`)
1. **Action**: In the **Left Window (`Customer`)**, manually type `/#!/admin/analytics` in the address bar.
2. **Observable Result**: `@uirouter/angularjs` `$transitions.onBefore` immediately intercepts the transition and renders `/#!/403` (`app.public.forbidden`) without flashing sensitive layouts.
3. **Backend Defense-in-Depth Proof**: Show DevTools Network or run a quick curl/Postman call with Saigon Staff's JWT against a Hanoi booking ID — ASP.NET Core `PermissionAuthorizationHandler` + EF Core `IDealerScoped` Global Query Filter blocks cross-dealer access (`404`/`403` `application/problem+json` with W3C `traceId`).

### Minute `0:45 – 1:45` · Act 2: Customer Digital VIN Garage & Automated VIN Reminders (`FR-02`, `FR-03`, `FR-04`, `ADR-005`)
1. **Action**: Navigate **Left Window** back to **My VIN Garage** (`/#!/customer/garage`).
2. **Observable Result**:
   - Point out `Honda SH160i ABS Sport` (`VIN: RLHJF5109RZ104829`, onboarded atomically from a showroom `SalesOrder`) displaying the proactive **`MaintenanceReminderTriggered`** banner (`3,850 km / 4,000 km Periodic Service Due`).
   - Click **`[+ Register External Bike VIN]`**, enter `RLCUG1010PY092341` (`Yamaha Exciter 155`, `11,200 km`) — highlight instant ISO 3779 validation (try typing `'I'` or `'O'` first to show inline validation rejection, then fix to `'1'`/`'0'` and save).

### Minute `1:45 – 3:00` · Act 3: Concurrency-Safe Slot Booking & Zero-Refresh SignalR Push (`FR-04`, `FR-06`, `ADR-004`)
1. **Action**: Keep **both windows visible side-by-side**! On the **Left Window (`Customer`)**, click **`[Book Maintenance Now]`** on `RLHJF5109RZ104829`:
   - Select **Apex Moto Saigon Center**, tomorrow's `09:00 – 10:00` slot (showing live `Available Bays` & `RowVersion` protection), attach pre-redeemed voucher **`SVC100K-7K9P2M4X`** (`100,000 VND Off`), and click **`[Confirm Booking]`**.
2. **Observable Result (The Money Shot)**:
   - Within **`< 100 ms`** — **without touching or refreshing the Right Window** — `NotificationHub` routes `BookingCreated` strictly to group `dealer-{saigonDealerId}`.
   - Wrapped in `$rootScope.$applyAsync()`, the **Right Window (`Saigon Staff`)** flashes a live toast and prepends the highlighted booking card into the `PENDING` column! (Note: Hanoi & Da Nang staff workbenches receive nothing.)

### Minute `3:00 – 4:15` · Act 4: Workshop Invoice Settlement, Two-Step Loyalty Math & Live Tier Upgrade (`FR-05`, `ADR-003`)
1. **Action**: In the **Right Window (`Saigon Staff`)**, click **`[Confirm]`** -> **`[Check In]`** -> **`[Issue Invoice]`** on the newly arrived booking:
   - Enter `LaborAmount = 220,000 VND`, `PartsAmount = 480,000 VND` (`Gross = 700,000 VND`), verified exit odometer `3,935 km`.
   - Reserved voucher `SVC100K-7K9P2M4X` automatically deducts `100,000 VND` -> **`NetPaidAmount = 600,000 VND`**.
   - Point out the live preview calculator: `Floor(600,000 / 10,000) = 60 base pts * 1.20x (Silver) = +72 PointsEarned`. Click **`[Finalize & Settle Invoice]`**.
2. **Observable Result**:
   - Single SQL transaction marks voucher `'Used'`, updates `CustomerVehicles` (`LastServiceMileageKm = 3935`, advancing next due target to `8,000 km`), appends immutable `PointLedgerEntries` (`SERVICE_INVOICE:{Id}:EARN`, `+72`), and pushes `LifetimeQualifyingPoints` from `1,960 -> 2,032` (`>= 2,000`).
   - Instantly on the **Left Window (`Customer`)**, SignalR pushes `LoyaltyPointsUpdated` and **`TierUpgraded`**, popping the celebratory **Gold Member (`1.50x` Multiplier)** upgrade modal!
   - Switch to **Loyalty Wallet** (`/#!/customer/loyalty`) and redeem a `300`-point voucher — demonstrate that `AvailablePoints` drops by `300` while **`LifetimeQualifyingPoints` stays at `2,032` (`Gold` tier preserved)**.

### Minute `4:15 – 5:00` · Act 5: Executive Multi-Dealer `Chart.js v4` Analytics & CSV Export (`FR-07`)
1. **Action**: Log into `admin@motorcycledealer.vn` (`Admin@123!`) on `/#!/admin/analytics`.
2. **Observable Result**:
   - Showcase all **4 interactive `Chart.js v4` visualizations** backed by index-only SQL covering scans (`IX_SalesOrders_Analytics_Covering`, `IX_ServiceInvoices_Analytics_Covering`, `IX_ServiceBookings_Analytics_Dealer`, `IX_LoyaltyAccounts_Tier_Distribution`):
     1. **Stacked Bar**: Showroom Bike Sales vs. Workshop Service Revenue across Saigon, Hanoi, and Da Nang (already reflecting the `600,000 VND` invoice just settled!).
     2. **Toggleable Booking Funnel (`[By Dealer | Status Mix]`)**: Toggle live between Grouped Bar per branch and 7-status network Doughnut without canvas leaks (`RISK-04`).
     3. **Dual-Axis Multi-Line**: Contiguous 12-month (`YYYY-MM` UTC zero-filled) combined revenue & booking trend.
     4. **Loyalty Tier Doughnut**: Customer distribution across `Bronze`, `Silver`, `Gold`, and `Platinum` + `PointsLiabilityVnd` (`AvailablePoints * 500 VND`).
   - Click **`[Export CSV Report]`** (`Permissions.Analytics.ExportReports`) to download the UTF-8 BOM + OWASP formula-sanitized `dealer-analytics-YYYYMMDD.csv` spreadsheet over authenticated `$http` (`Access-Control-Expose-Headers: Content-Disposition`).

### Bonus `v1.1.0` Act (`+60s`) · Act 6: Showroom Test-Drive -> Staff Live Confirm -> Itemized OEM Spare-Parts Invoice -> `@media print` Receipt (`FR-08`)
1. **Showroom Test-Drive & Zero-Refresh Staff Alert**:
   - In the **Left Window (`Customer`)**, visit `/showroom`, click **`[Book Test Drive]`** on `Honda SH160i ABS Sport` (deep-linking to `/customer/test-drives?modelId=...`), pick **Apex Moto Saigon Center** + an open 45-minute window, enter license number, and submit.
   - In the **Right Window (`Saigon Staff`)** parked on `/staff/test-drives` (`SCR-STF-06`), watch SignalR `TestDriveCreated` prepend the `TD-*` card **with zero page refresh**! Click **`[Confirm]`**, assign an `Available` branch demo `VIN`, and confirm — the **Left Window** updates live (`TestDriveStatusChanged`) with the assigned demo `VIN` badge, and Staff can click **`[Convert to Bike Sale ->]`** upon completion.
2. **Itemized OEM Spare-Parts Invoice & `@media print` Official Receipt**:
   - Inside Workshop Invoice Builder (`SCR-STF-02`), pick model-compatible **OEM Spare Parts** (e.g., `PRT-OIL-10W30`) from the line-item picker — `PartsAmount` and `GrossAmount` reconcile automatically into immutable `ServiceInvoiceLineItems`.
   - Click **`[Print Official Receipt]`** (available on `SCR-STF-02`, `SCR-STF-03`, `SCR-CUS-04`, and `SCR-CUS-05`) to open `<printable-receipt-modal>` displaying `RCP-SVC-*` / `RCP-SLS-*`, itemized parts/labor, loyalty summary, and the 16-char uppercase SHA-256 `VerificationCode`, formatted cleanly via `@media print` (`$window.print()`).

### Bonus `v1.2.0` Act (`+60s`) · Act 7: Post-Service 5-Star CSAT `ServiceReview` (`+25` Bonus Points) & VIN `WarrantyClaim` Live Adjudication (`FR-09`)
1. **5-Star CSAT Review & `+25` Spendable Bonus Credit**:
   - In the **Left Window (`Customer`)** on `/customer/bookings` (`SCR-CUS-04`) or `/customer/warranty` (`SCR-CUS-07`), click **`[Rate Service (+25 Pts)]`** on a settled `ServiceInvoice` (`INV-202608-0001`), select `5` stars + comment, and submit (`POST /api/v1/service-reviews`).
   - Confirm **`AvailablePoints` increases by `+25`** (`SERVICE_REVIEW:{InvoiceId}:BONUS`) while `LifetimeQualifyingPoints` remains unchanged, and watch `ServiceReviewSubmitted` prepend the 5-star card live onto **Right Window (`Saigon Staff`)** `/staff/warranty-reviews` (`SCR-STF-07`), where Staff can post an official branch response (`PATCH /response`).
2. **36-Month / 30,000-km VIN `WarrantyClaim` & Quality Analytics (`SCR-ADM-08`)**:
   - On `/customer/warranty` (`SCR-CUS-07`), file a `WarrantyClaim` (`Electrical` / `Engine`) on an in-warranty `VIN` -> watch `WarrantyClaimCreated` arrive instantly on `/staff/warranty-reviews` (`SCR-STF-07`) with zero page reloads -> Staff approves with a compatible OEM `SparePart` (`WarrantyClaimStatusChanged` updates Customer live) -> inspect network-wide `AverageCsatRating` & `ApprovedWarrantyCoveredVnd` `Chart.js v4` charts on `/admin/quality-warranty` (`SCR-ADM-08`).

---

## 5. Operational Runbooks & Troubleshooting Playbooks

### 5.1 Runbook 1 — Clean Database Reset & Idempotent Re-Seed
When resetting local state before a clean QA run or live demo *(run manually via EF CLI outside protected production databases)*:

```powershell
# Drop local dev database, recreate all 25 3NF tables + RCSI + triggers, and re-seed on boot
dotnet ef database drop --force --project backend/src/MotorcycleDealer.Infrastructure --startup-project backend/src/MotorcycleDealer.WebApi
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1 -Migrate
dotnet run --project backend/src/MotorcycleDealer.WebApi -- --seed-only
```

### 5.2 Runbook 2 — SignalR `/hubs/notifications` WebSocket Diagnostics
If `DealerStaff` workbenches show `[Reconnecting]` or do not update live:
1. **Check Negotiate Handshake**: Run `powershell -File .agents/scripts/smoke-fullstack.ps1` (`SMK-05` probes `POST /hubs/notifications/negotiate?negotiateVersion=1`).
2. **Verify CORS Origin**: SignalR WebSockets with credentials reject wildcard `*` origins; confirm `Cors:AllowedOrigins` contains the exact SPA origin (`http://localhost:4200`).
3. **Verify Query-String Token Extraction**: Browser `WebSocket` upgrades pass `?access_token={jwt}`; confirm `JwtBearerEvents.OnMessageReceived` extracts `access_token` when `Path.StartsWithSegments("/hubs/notifications")`.
4. **Verify Durable Fallback**: Query `Notifications` via `sqlserver-localdb-inspector` MCP (`SELECT TOP 20 * FROM dbo.Notifications ORDER BY CreatedAtUtc DESC`) to confirm whether the domain event committed to SQL Server.

### 5.3 Runbook 3 — Triaging `409 Conflict` Responses & Ledger Health
Use workspace Stdio MCP server **`sqlserver-localdb-inspector`** (`query_readonly_sql` — never global `dbhub`) to inspect live invariants:

```sql
-- 1. Check ServiceTimeSlots utilization & verify zero overbooked slots
SELECT TOP 50 Id, DealerId, SlotDate, StartTime, MaxCapacity, BookedCount, IsBlocked
FROM dbo.ServiceTimeSlots
WHERE SlotDate >= CAST(SYSDATETIMEOFFSET() AS DATE)
ORDER BY SlotDate ASC, StartTime ASC;

-- 2. Verify 100% mathematical reconciliation between PointLedgerEntries and LoyaltyAccounts
SELECT la.Id AS LoyaltyAccountId, la.AvailablePoints, ISNULL(SUM(ple.PointsDelta), 0) AS LedgerSum
FROM dbo.LoyaltyAccounts la
LEFT JOIN dbo.PointLedgerEntries ple ON ple.LoyaltyAccountId = la.Id
GROUP BY la.Id, la.AvailablePoints
HAVING ISNULL(SUM(ple.PointsDelta), 0) <> la.AvailablePoints;
-- 3. Verify 0 drift between itemized ServiceInvoiceLineItems and parent ServiceInvoices.GrossAmount
SELECT si.Id AS InvoiceId, si.InvoiceNumber, si.GrossAmount, SUM(li.LineTotalVnd) AS LineItemsSum
FROM dbo.ServiceInvoices si
INNER JOIN dbo.ServiceInvoiceLineItems li ON li.ServiceInvoiceId = si.Id
GROUP BY si.Id, si.InvoiceNumber, si.GrossAmount
HAVING si.GrossAmount <> SUM(li.LineTotalVnd);

-- 4. Verify every ServiceReview with BonusPointsAwarded > 0 has a matching +25 ServiceReviewBonus ledger row
SELECT sr.Id, sr.ReviewNumber, sr.BonusPointsAwarded
FROM dbo.ServiceReviews sr
WHERE sr.BonusPointsAwarded > 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PointLedgerEntries ple
      WHERE ple.ReferenceType = 'ServiceReview'
        AND ple.ReferenceId = sr.Id
        AND ple.PointsDelta = sr.BonusPointsAwarded
  );
```

### 5.4 Runbook 4 — `.agents/` Automation Suite, Post-Task Sync & Daily GitHub Shipper Reference

| Script Path | Purpose & Common Flags |
| :--- | :--- |
| `.agents/scripts/verify-post-task-sync.mjs` | **Mandatory Post-Task Synchronization Gate**: verifies `100%` workspace -> `git-6month-schedule-plan.json` file coverage (`0` unmapped/missing/empty days), `25` `DbSet<T>` tables, `40` `Permissions.*` constants, `72/72` WBS sync, and `docs/` + `.docx` parity |
| `.agents/scripts/daily-git-shipper.mjs` | Lock-safe `183`-day daily GitHub commit & push engine (`--status` for read-only progress & git tree audit; `--dry-run` for preview) |
| `.agents/scripts/register-daily-git-schedule.ps1` | Registers `MotorcycleDealer-DailyGitShipper` in Windows Task Scheduler with `-DontStopOnIdleEnd` (`StopOnIdleEnd = False`) |
| `.agents/scripts/verify-dealer-platform.ps1` | Master verification gate across JSON, skills, guardrails, MCP, post-task sync, `541` xUnit & `56` SPA checks (`-Phase 8`) |
| `.agents/scripts/audit-dealer-guardrails.ps1` | Enforces `AGENTS.md` byte caps, Clean Arch `.csproj` inward dependencies, `4`-table financial/ledger immutability & secret hygiene (`-Phase 8`) |
| `.agents/scripts/ensure-sqlserver-db.ps1` | Starts `(localdb)\MSSQLLocalDB` or Docker SQL Server 2022 (`-UseDocker`, `-Migrate`, `-DryRun`) |
| `.agents/scripts/seed-demo-scenarios.mjs` | Validates canonical 3-Dealer / 4-Tier / ISO 3779 VIN / `v1.1.0` closed-loop scenarios (`--dry-run`, `--api-url`) |
| `.agents/scripts/smoke-fullstack.ps1` | 9-probe (`SMK-01`..`SMK-09`) HTTP, OWASP header, `RFC 7807`, `IDealerScoped`, SignalR, Analytics/CSV, Test-Drives, Spare-Parts & `/receipt` smoke runner |
| `.agents/scripts/run-phase-quality-loop.ps1` | 3-battery (`T*.1`..`T*.3`) Release `--warnaserror` `541` xUnit + `56` SPA/SignalR/CDP E2E + MCP LocalDB reconciliation gate (`-Phase 8 -Round 3`) |
| `.agents/scripts/run-and-capture-dealer-platform.ps1` | Boots API + SPA, runs scenarios, and captures Playwright screenshots into `docs/evidence/` (`-DryRun`) |
| `.agents/scripts/sync-progress.mjs` | Verifies (`--check`) or synchronizes (`--write`) `72 / 72` task counts between `TASKS.md` and `PROGRESS.md` |
| `.agents/scripts/verify-mobile-maui.mjs` | **Phase 10 Static MAUI Guardrails (`8 / 8`)**: verifies `mobile/AGENTS.md` (`< 8 KB`), pure `net10.0` `Mobile.Core`, `100%` `x:DataType` + `SourceGen`, `0` legacy `_field` `[ObservableProperty]`, `allowBackup="false"`, `network_security_config.xml`, `48/48` 3-way RBAC parity, and `[MAUI-IOS-AND-TOOLKIT]` (`net10.0-android;net10.0-ios`, strict ATS `Info.plist`, iOS host/notification files, `CommunityToolkit.Maui`, `FluentValidation`, `Humanizer` & `Http.Resilience`) |
| `.agents/scripts/ensure-maui-android.ps1` | Validates Android SDK/JDK/WHPX, provisions `Pixel_8_API_35` AVD (`android-35;google_apis_playstore;x86_64`), boots emulator (`-BootEmulator`), and runs `adb reverse tcp:5080 tcp:5080` (`-SetupAdbReverse`) |
| `.agents/scripts/smoke-mobile-android.ps1` | End-to-end Android smoke runner: builds `-f net10.0-android`, installs APK via `adb install -r`, launches `vn.motorcycledealer.mobile`, verifies `pidof`, dumps `uiautomator` XML, and captures PNG screenshot |
| `.agents/scripts/build-spec-docx.mjs` | Regenerates [Motorcycle_Dealer_Loyalty_Platform_Specification.docx](./Motorcycle_Dealer_Loyalty_Platform_Specification.docx) cleanly with zero root `node_modules` |

```powershell
# Verify Post-Task Docs/Snapshot/Schedule Sync & Inspect 6-Month Daily Git Shipper Status (Read-Only)
node .agents/scripts/verify-post-task-sync.mjs
node .agents/scripts/daily-git-shipper.mjs --status

# Rebuild & OpenXML-validate the Master Word Specification (.docx)
$env:NODE_PATH = "C:\Users\HELLO\.gemini\antigravity\brain\dfa8ffa1-46c0-461f-8f59-3ffe9853361f\scratch\docx_builder\node_modules"
node .agents/scripts/build-spec-docx.mjs
python "C:\Users\HELLO\.gemini\config\plugins\developer-power-skills\skills\docx\scripts\office\validate.py" "c:\Users\HELLO\Downloads\bike_car_management\docs\Motorcycle_Dealer_Loyalty_Platform_Specification.docx"
```
