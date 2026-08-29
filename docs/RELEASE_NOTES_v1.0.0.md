# Release Notes — `v1.0.0` (`Motorcycle Dealer & Loyalty Management Platform`)

| Release Attribute | Canonical Value |
| :--- | :--- |
| **Version** | **`v1.0.0`** (Production & Portfolio General Availability) |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **GitHub Repository** | [https://github.com/quocanhh86/motorcycle-dealer](https://github.com/quocanhh86/motorcycle-dealer) |
| **Engineering Timeline & Team** | `02/2026 - 08/2026` · Solo Full-Stack Engineering Release (`Team Size: 1`) |
| **Back-End Runtime** | `C#`, `ASP.NET Core Web API` (`.NET 10` / `net10.0`), `Entity Framework Core 10`, `SignalR`, `FluentValidation` |
| **Front-End SPA** | Decoupled Modular `AngularJS 1.8.3` (`ng-strict-di`), `@uirouter/angularjs`, `Chart.js v4`, `@microsoft/signalr` |
| **Database Engine** | Microsoft SQL Server (`(localdb)\MSSQLLocalDB` & SQL Server 2022 Docker) · `MotorcycleDealerDb` (`22` 3NF tables, `RCSI`) |
| **Roadmap & Gate Status** | **`7 / 7` Phases (`64 / 64` WBS Tasks)** · Passed Mandatory Per-Phase **7-Stage Quality Loop (`15R + 6T`)** |

---

## 1. Executive Summary

**`v1.0.0`** delivers the complete, closed-loop **Motorcycle Dealer & Loyalty Management Platform** spanning nationwide showroom inventory, atomic 17-character ISO 3779 `VIN` vehicle sales and digital garage onboarding, `RowVersion` concurrency-safe workshop appointment scheduling, deduplicated background `VIN` maintenance reminders, workshop invoice settlement with immutable append-only loyalty point accounting and 4-tier membership progression, zero-refresh **SignalR** WebSocket notifications, and **Chart.js v4** multi-dealer executive analytics with OWASP formula-hardened CSV reporting.

---

## 2. Delivered Vertical Modules (`MOD-01` .. `MOD-06`)

### `MOD-01` — Identity, Rotating JWT Auth & Multi-Dealer Granular RBAC (`FR-01`, `ADR-002`, `ADR-006`)
- **3 Governed Roles & 34 Granular Permissions**: Enforces `[HasPermission(Permissions.<Module>.<Action>)]` across `Customer`, branch-isolated `DealerStaff`, and platform-wide `Admin` roles via an `O(1)` `FrozenDictionary` `PermissionPolicyProvider` and per-request `FrozenSet<string>` claim cache.
- **Cryptographic Token Lifecycle & Rate Limiting**: `stackalloc` `PBKDF2-SHA512` (`210,000` iterations + `CryptographicOperations.ZeroMemory`), `15m` JWT access tokens, atomic RCSI `ExecuteUpdateAsync` refresh-token rotation with `TokenFamilyId` `ReuseDetected` breach revocation, immediate mid-session `ActivePrincipalMiddleware` + `NotificationHub` lockout enforcement (`403 ACCOUNT_LOCKED` / `STAFF_DEALER_UNASSIGNED`), and IP-partitioned `AuthRateLimitOptions` (`429 RATE_LIMIT_EXCEEDED` `RFC 7807 ProblemDetails` + `Retry-After`).
- **Defense-in-Depth Multi-Dealer Isolation (`IDealerScoped`)**: `MotorcycleDealerDbContext` applies automatic EF Core Global Query Filters (`e.DealerId == currentUser.DealerId`) and `SaveChangesAsync` write guards (`403 FORBIDDEN_DEALER_SCOPE`) across all 5 dealer-owned tables, mirrored in AngularJS via `@uirouter/angularjs` `$transitions.onBefore` guards, open-redirect sanitization, single-flight `401` refresh queuing, and `has-permission` directives.

### `MOD-02` — Dealer Network, Motorcycle Showroom, ISO 3779 VIN Stock & Customer Garage (`FR-02`, `FR-03`)
- **Showroom & ISO 3779 `VIN` Inventory**: Manages regional dealerships (`DLR-HN-01`, `DLR-DN-01`, `DLR-HCM-01`), `MotorcycleModels`, OEM `MaintenanceRules`, and `DealerInventories` validated against 17-character uppercase ISO 3779 `Vin` rules (`^[A-HJ-NPR-Z0-9]{17}$`, excluding `I`, `O`, `Q`) with public `engineNumber` masking on `?publicOnly=true`.
- **Atomic Showroom Sale & Auto-Garage Onboarding**: Completing `POST /api/v1/sales-orders` inside `CreateExecutionStrategy()` + `BeginTransactionAsync()` atomically transitions `DealerInventories` to `Sold` (`RowVersion`-guarded), redeems eligible showroom vouchers, records an immutable `SalesOrder`, auto-onboards the motorcycle into `CustomerVehicles` with initial maintenance targets, posts `SALES_ORDER:{Id}:EARN` `PointLedgerEntries`, evaluates 4-tier upgrades, and pushes post-commit SignalR alerts.
- **Digital Customer `VIN` Garage**: Enables riders to self-register external `VIN` motorcycles (`.IgnoreQueryFilters()` global VIN & license plate uniqueness) and log monotonic odometer readings (`422 ODOMETER_REGRESSION` guard).

### `MOD-03` — Concurrency-Safe Workshop Slot Booking & Automated VIN Reminders (`FR-04`, `ADR-005`)
- **Anti-Double-Booking `ServiceTimeSlots` (`RISK-02`)**: Workshop bay capacity (`BookedCount < MaxCapacity`) is protected by SQL Server `ROWVERSION` optimistic concurrency tokens and relational `CHECK` constraints, returning `409 Conflict` (`SLOT_CAPACITY_EXHAUSTED`) under parallel contention with 1-click slot refresh in the AngularJS 4-step booking wizard.
- **Hosted `VinMaintenanceReminderWorker` (`RISK-06`)**: Background scanner aligned to covering index `IX_CustomerVehicles_ReminderScan` evaluates odometer (`NextDueMileageKm`) and calendar (`NextDueDateUtc`) lead windows against `MaintenanceRules` (`MAJOR_12000` tie-break), enforcing `UQ_MaintenanceReminders_DeduplicationKey` (`{VIN}:{RuleId}:{TargetKm}`) so restarts never emit duplicate reminders.

### `MOD-04` — Workshop Invoicing, Immutable Loyalty Ledger, 4-Tier Engine & Vouchers (`FR-05`, `ADR-003`)
- **Atomic `ServiceInvoices` Settlement**: Transitions `ServiceBookings` to `Completed`, consumes `ReservedOnBooking` `CustomerVouchers` (`-> Used`) or releases unused reservations (`-> Available`), rolls `CustomerVehicles` maintenance schedules forward via `ComputeNextMaintenanceSchedule`, resolves open reminders, and credits loyalty points in a single ACID transaction.
- **Two-Step Floor Math & Append-Only `PointLedgerEntries` (`RISK-03`)**: Computes `PointsEarned = (int)Math.Floor(Math.Floor(NetPaidAmount / 10_000m) * TierMultiplier)` (`10,000 VND = 1 base point`). Enforces strict immutability via both EF Core `AppendOnlyLedgerInterceptor` and SQL Server trigger `TR_PointLedgerEntries_BlockMutation` (`THROW 50001`), backed by unique `IdempotencyKey` constraints.
- **4-Tier Progression & Concurrency-Safe Voucher Store**: Automates promotions across **`Bronze`** (`0+` / `1.00x`), **`Silver`** (`500+` / `1.20x`), **`Gold`** (`2,000+` / `1.50x`), and **`Platinum`** (`5,000+` / `2.00x`) based on `LifetimeQualifyingPoints`. Voucher redemption (`POST /api/v1/vouchers/redeem`) deducts `AvailablePoints` ONLY (never demoting tier), enforces atomic SQL `QuotaRedeemed < QuotaTotal`, and issues 33-character `VoucherCodeSuffixAlphabet` codes (excluding `I`, `O`, `Q`).

### `MOD-05` — SignalR Real-Time Notification Hub & Persistent Inbox (`FR-06`, `ADR-004`)
- **Zero-Refresh Staff & Customer Push (`/hubs/notifications`)**: Strongly-typed `NotificationHub : Hub<INotificationClient>` authenticates WebSocket `?access_token=` handshakes and enrolls connections strictly from server-verified JWT claims into `dealer-{dealerId}`, `user-{userId}`, and `role-Admin` groups (`0` client-callable group methods).
- **Outbox-First Persistence & Digest-Safe SPA Integration**: Commits `Notifications` rows inside SQL transactions before non-throwing post-commit WebSocket fan-out (`BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`). AngularJS `SignalRClientService` configures `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])`, `$rootScope.$applyAsync()` marshaling, reconnect unread delta catch-up (`?sinceUtc=`), and `$onDestroy` listener disposal.

### `MOD-06` — Executive Multi-Dealer `Chart.js v4` Analytics & CSV Reporting (`FR-07`, `ADR-006`)
- **Index-Only SQL Aggregations & Zero-Fill Buckets**: `AdminAnalyticsController` (`/api/v1/admin/analytics/{overview,kpis,revenue-by-dealer,bookings-by-dealer,monthly-trends,loyalty-distribution,export,export-csv}`) executes `AsNoTracking().IgnoreQueryFilters()` `GROUP BY` projections over 4 dedicated SQL covering indexes (`IX_SalesOrders_Analytics_Covering`, `IX_ServiceInvoices_Analytics_Covering`, `IX_ServiceBookings_Analytics_Dealer`, `IX_LoyaltyAccounts_Tier_Distribution`), zero-filling quiet dealerships, contiguous `YYYY-MM` calendar months, and 4 loyalty tiers.
- **Leak-Free `Chart.js v4` Executive Dashboard (`SCR-ADM-01`, `RISK-04`)**: Renders 4 interactive visualizations via `<dealer-chart>` (Stacked Bar Showroom vs. Service Revenue, Toggleable Grouped Bar / 7-Status Funnel Doughnut, Dual-Axis Monthly Revenue & Booking Trends, and 4-Tier Loyalty Distribution Doughnut), updating datasets in-place via `chartInstance.update('none')`, cleanly re-instantiating on chart-type switches, and calling `chartInstance.destroy()` inside `$onDestroy`.
- **RFC 4180 + OWASP Formula-Sanitized CSV Export**: Streams UTF-8 BOM (`0xEF, 0xBB, 0xBF`) CSV spreadsheets while neutralizing spreadsheet formula-injection vectors (`=`, `+`, `-`, `@`, `\t`, `\r`) with leading single-quote escaping.

---

## 3. Database, Architecture & Security Highlights

- **4-Layer Clean Architecture (`ADR-001`)**: `MotorcycleDealer.Domain` (`0` external NuGet/project refs) $\leftarrow$ `MotorcycleDealer.Application` (`Domain` only) $\leftarrow$ `MotorcycleDealer.Infrastructure` & `MotorcycleDealer.WebApi` (`TreatWarningsAsErrors=true`, `Nullable=enable`).
- **22 Normalized 3NF SQL Server Tables (`MotorcycleDealerDb`)**: `Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `RefreshTokens`, `Dealers`, `MotorcycleModels`, `DealerInventories`, `SalesOrders`, `CustomerVehicles`, `MaintenanceRules`, `MaintenanceReminders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `LoyaltyTiers`, `LoyaltyAccounts`, `PointLedgerEntries`, `VoucherCatalogs`, `CustomerVouchers`, and `Notifications` (`NEWSEQUENTIALID()` + `Guid.CreateVersion7()`, `DECIMAL(18,2)`, `DATETIMEOFFSET(7)` UTC, `READ_COMMITTED_SNAPSHOT ON`, `23` workload B-tree indexes, `5` `ROWVERSION` tables).
- **OWASP API Security Top 10 (2023) Hardening**:
  - `API1 / API5` (BOLA / BFLA): 34 permission policies + `IDealerScoped` query/write guards + Customer ownership checks + runtime `Admin` analytics verification.
  - `API2` (Broken Authentication): `PBKDF2-SHA512` (`210k` iterations), `>= 256-bit` JWT signing key fail-fast, RCSI refresh-token family rotation, and `ActivePrincipalMiddleware` instant revocation.
  - `API3 / API6` (Property Authorization & Business Flows): Immutable DTO contracts (never returning EF entities), `RowVersion` slot/inventory/voucher race locks, and append-only ledger triggers.
  - `API4` (Unrestricted Resource Consumption): `AuthRateLimitOptions` fixed-window IP limiter (`429 RATE_LIMIT_EXCEEDED` + `Retry-After`), `PaginationQueryValidator` (`pageSize <= 100`), and `731`-day analytics range cap.
  - `API7 / API8 / API10` (CORS, Security Headers & Injection): Explicit `SpaCorsPolicy` origin allowlist + `WithExposedHeaders("Content-Disposition", "Retry-After", "Location")`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `RFC 7807 ProblemDetails` with non-Development stack trace suppression, `0` unparameterized SQL queries, and OWASP CSV cell sanitization.

### 3.1 Final `v1.0.0` Empirical Verification & Quality-Loop Metrics

| Verification Suite | Verified Result | Coverage Highlights |
| :--- | :--- | :--- |
| **Per-Phase 7-Stage Quality Loop** | **`15 / 15` Reviews + `6 / 6` Test Batteries PASS** | Enforced across Phases 3, 4, 5, 6, and 7 (`5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T`; Phase 7 Round 3 in `207,068 ms`) |
| **`.NET 10` xUnit Automated Suite** | **`417 / 417` PASS (`0` warnings, `0` flaky)** | `340` `Domain.UnitTests` + `77` real SQL Server LocalDB `Api.IntegrationTests` (`1,251 / 1,251` Round 3 runs green, `StrayTestDbCount = 0`) |
| **AngularJS `1.8.3` SPA Verification** | **`48 / 48` Automated Checks PASS** | `38` `.js` + `3` `.mjs` files, `40` explicit `ng-strict-di` `$inject` units, `track by` / `ng-options` freeze guards, `Chart.js v4` & CSV suites |
| **Full-Stack HTTP & SignalR Smoke** | **`8 / 8` Probes PASS (`SMK-01`..`SMK-08`)** | `/api/v1/health?probeDb=true`, OWASP headers, `401`/`403` `ProblemDetails`, `IDealerScoped`, SignalR negotiate, all `8` analytics/CSV bindings, SPA `/` |
| **Headless Edge CDP Closed-Loop E2E** | **`24 / 24` Routes (`0` Console Errors)** | Live `BookingCreated` push + Hanoi `SalesOrder` (`+86,000,000` VND) + HCMC `ServiceInvoice` (`+700,000` VND) exact `Chart.js` delta + `4->0->4->0` canvas teardown |
| **SQL Server LocalDB Reconciliation** | **`22 / 22` 3NF Tables (`0` Drift)** | `LedgerDriftCount = 0`, `OverbookedSlotCount = 0`, `SlotDriftCount = 0/291`, `VoucherQuotaDriftCount = 0/5`, OpenXML `.docx` `validate.py` PASS |

---

## 4. Deployment & Quick-Start Guide

### Option A — Native LocalDB Development & Smoke Verification
```powershell
# 1. Ensure SQL Server LocalDB & apply 22-table 3NF migrations
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1 -Migrate

# 2. Start ASP.NET Core 10 WebApi + SignalR Hub (http://localhost:5080)
dotnet run --project backend/src/MotorcycleDealer.WebApi --urls "http://localhost:5080"

# 3. Start AngularJS 1.8.3 SPA (http://localhost:4200)
npm --prefix frontend start

# 4. Run 8-stage Full-Stack HTTP, RBAC, IDealerScoped, SignalR, Analytics & CSV Smoke Suite
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1
```

### Option B — Non-Root Docker Compose Stack (`SQL Server 2022` + `WebApi` + `SPA`)
```powershell
$env:MSSQL_SA_PASSWORD = "LocalDocker!StrongPwd2026"
$env:JWT_SIGNING_KEY = "ReplaceWithStrong256BitProductionSigningSecretKey2026!"
docker compose config -q
docker compose up --build -d
```

---

## 5. Pre-Seeded Demo Personas (`DatabaseSeeder`)

| Role | Demo Email | Password | Scope / Tier | Default Portal Route |
| :--- | :--- | :--- | :--- | :--- |
| **`Admin`** | `admin@motorcycledealer.vn` | `Admin@123!` | Platform-wide (`All 3 Dealers`) | `/#!/admin/analytics` |
| **`DealerStaff` (Hanoi)** | `staff.hn@motorcycledealer.vn` *(alias `lan.tran.hn@dealer.vn`)* | `Staff@123!` | `DLR-HN-01` (`Apex Moto Hanoi Flagship`) | `/#!/staff/workbench` |
| **`DealerStaff` (Da Nang)** | `staff.dn@motorcycledealer.vn` | `Staff@123!` | `DLR-DN-01` (`Apex Moto Da Nang Coastal Hub`) | `/#!/staff/workbench` |
| **`DealerStaff` (Saigon)** | `staff.hcm@motorcycledealer.vn` | `Staff@123!` | `DLR-HCM-01` (`Apex Moto Saigon Center`) | `/#!/staff/workbench` |
| **`Customer` (Bronze)** | `customer.bronze@motorcycledealer.vn` *(alias `minh.nguyen@customer.vn`)* | `Customer@123!` | `Bronze` (`1.00x` · `120` pts) | `/#!/customer/garage` |
| **`Customer` (Silver)** | `customer.silver@motorcycledealer.vn` | `Customer@123!` | `Silver` (`1.20x` · `1,960` Lifetime pts) | `/#!/customer/garage` |
| **`Customer` (Gold)** | `customer.gold@motorcycledealer.vn` | `Customer@123!` | `Gold` (`1.50x` · `3,200` Lifetime pts) | `/#!/customer/garage` |
| **`Customer` (Platinum)** | `customer.platinum@motorcycledealer.vn` | `Customer@123!` | `Platinum` (`2.00x` · `10,100` Lifetime pts) | `/#!/customer/garage` |
