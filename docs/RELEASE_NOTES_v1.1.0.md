# Release Notes — `v1.1.0` (`Motorcycle Dealer & Loyalty Management Platform`)

| Release Attribute | Canonical Value |
| :--- | :--- |
| **Version** | **`v1.1.0`** (Phase 8 Feature Expansion — Production & Portfolio Release) |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **GitHub Repository** | [https://github.com/quocanhh86/motorcycle-dealer](https://github.com/quocanhh86/motorcycle-dealer) |
| **Engineering Timeline & Team** | `02/2026 - 08/2026` · Solo Full-Stack Engineering Release (`Team Size: 1`) |
| **Back-End Runtime** | `C#`, `ASP.NET Core Web API` (`.NET 10` / `net10.0`, `16` Controllers), `Entity Framework Core 10`, `SignalR`, `FluentValidation` |
| **Front-End SPA** | Decoupled Modular `AngularJS 1.8.3` (`ng-strict-di`, `27` Navigable Routes), `@uirouter/angularjs`, `Chart.js v4`, `@microsoft/signalr` |
| **Database Engine** | Microsoft SQL Server (`(localdb)\MSSQLLocalDB` & SQL Server 2022 Docker) · `MotorcycleDealerDb` (`25` 3NF tables, `RCSI`) |
| **Roadmap & Gate Status** | **`8 / 8` Phases (`72 / 72` WBS Tasks = `100.0%`)** · Passed Mandatory Per-Phase **7-Stage Quality Loop (`15R + 6T`)** |

---

## 1. Executive Summary

**`v1.1.0`** expands the **`v1.0.0`** platform (`MOD-01`..`MOD-06`) with **Phase 8 (`T-801`..`T-808`)**, delivering end-to-end **Showroom Test-Drive Scheduling with Zero-Refresh SignalR Alerts**, an **OEM Spare-Parts Catalog with Itemized Workshop `ServiceInvoices`**, and **Printable Tax & Loyalty Verification Receipts** across both Showroom `SalesOrders` and Workshop `ServiceInvoices`. All enhancements preserve strict 4-Layer Clean Architecture (`ADR-001`), `IDealerScoped` multi-dealer isolation (`ADR-002`), append-only financial immutability (`ADR-003`), SignalR group security (`ADR-004`), and `RowVersion` optimistic concurrency (`ADR-005`).

---

## 2. What's New in `v1.1.0` (`T-801` .. `T-808`)

### 2.1 Showroom Test-Drive Scheduling & Zero-Refresh SignalR Dispatch (`T-801`, `T-802`, `T-804`)
- **Normalized `TestDriveBookings` Aggregate (`Table 23`, `IDealerScoped`, `RowVersion`)**:
  - Tracks customer showroom test-drive appointments (`TD-YYYYMMDD-XXXXXX` via `VoucherAndDocumentNumberHelper`), linking `CustomerId`, `DealerId`, `MotorcycleModelId`, optional demo unit `AssignedInventoryId` (`VIN`), `PreferredDate`, `StartTime`, `EndTime`, `DriverLicenseNumber`, `ContactPhone`, and lifecycle timestamps (`ConfirmedAtUtc`, `CompletedAtUtc`, `CancelledAtUtc`).
  - Enforces a concurrency-safe dealership/model window cap (`<= 2` active `Requested`/`Confirmed` test drives per `(DealerId, MotorcycleModelId, PreferredDate, StartTime)` inside `CreateExecutionStrategy()` + `BeginTransactionAsync()`, returning `409 TEST_DRIVE_SLOT_FULL` under parallel contention) and customer time-overlap rejection (`409 TEST_DRIVE_OVERLAP`).
  - Supports seamless test-drive-to-purchase lifecycle progression: if a demo `DealerInventory` unit assigned during `Confirmed` is subsequently sold via `SalesOrders`, `PATCH /api/v1/test-drives/{testDriveId}/status` cleanly transitions `Confirmed -> Completed` while preserving the historical `AssignedInventoryId` audit link.
- **REST `/api/v1/test-drives` & SignalR Real-Time Push (`/hubs/notifications`)**:
  - `TestDrivesController` exposes `GET /api/v1/test-drives/my` (`Permissions.TestDrives.ViewOwn`), `GET /api/v1/test-drives` (`Permissions.TestDrives.ViewDealer`), `GET /api/v1/test-drives/{testDriveId}`, `POST /api/v1/test-drives` (`Permissions.TestDrives.Create`), `POST /api/v1/test-drives/{testDriveId}/cancel` (`Permissions.TestDrives.CancelOwn` / `UpdateStatus`), and `PATCH /api/v1/test-drives/{testDriveId}/status` (`Permissions.TestDrives.UpdateStatus`).
  - Publishes post-commit `TestDriveCreated` to `dealer-{dealerId}` + `role-Admin` and `TestDriveStatusChanged` to `user-{customerId}` + `dealer-{dealerId}`.
- **Live AngularJS Portals (`SCR-PUB-01`, `SCR-CUS-06`, `SCR-STF-06`)**:
  - Public Showroom (`SCR-PUB-01`) adds a 1-click **`[Book Test Drive]`** CTA preserving `modelId` and `dealerId` across `/login?returnUrl=...` via sanitized `AuthService.resolvePostLoginTarget`.
  - Customer Test-Drives (`app.customer.testDrives`, `SCR-CUS-06`) provides interactive booking, live SignalR status updates (`AssignedVin` badge), and cancellation modal.
  - Dealer Staff Live Test-Drive Workbench (`app.staff.testDrives`, `SCR-STF-06`) receives `TestDriveCreated` cards with **zero page reloads**, supports demo `VIN` assignment (`Available` stock filtered by model), status transitions, and 1-click **`[Convert to Bike Sale]`** handoff into `app.staff.salesDesk`.

### 2.2 OEM Spare-Parts Catalog & Itemized `ServiceInvoiceLineItems` (`T-801`, `T-803`, `T-805`)
- **`SpareParts` Master Catalog (`Table 24`, `ISoftDeletable`) & `ServiceInvoiceLineItems` (`Table 25`, Immutable)**:
  - `SpareParts` models genuine OEM replacement parts (`PartCode`, `Name`, `Category` across `EngineOil`, `Filter`, `Brakes`, `Transmission`, `Tires`, `Electrical`, `Accessory`, optional `CompatibleModelId` or universal fit `NULL`, `UnitLabel`, `UnitPriceVnd`, `IsActive`), seeded with `8` realistic Honda/Yamaha/universal SKUs (`PRT-OIL-10W30`..`PRT-CVT-BLT01`).
  - `ServiceInvoiceLineItems` persists append-only itemized `Part` and `Labor` rows (`LineNumber`, `SparePartId`, `PartCodeSnapshot`, `Description`, `ItemType`, `Quantity`, `UnitPriceVnd`, `LineTotalVnd = Quantity * UnitPriceVnd`), protected from mutation by `IImmutableFinancialEntity` in `AppendOnlyLedgerInterceptor` and `audit-dealer-guardrails`.
- **Dual-Mode `/api/v1/spare-parts` & Backward-Compatible Itemized Settlement**:
  - `SparePartsController` exposes `[AllowAnonymous]` `GET /api/v1/spare-parts` & `GET /api/v1/spare-parts/{partId}` (restricting `?includeInactive=true` strictly to `Admin` / `Permissions.SpareParts.ManageCatalog`) plus Admin `POST` & `PUT` (`409 DUPLICATE_PART_CODE`).
  - `POST /api/v1/service-invoices` accepts optional `LineItems`, validating model compatibility against the booked vehicle (`422 SPARE_PART_INCOMPATIBLE`) and active SKU state (`422 SPARE_PART_INACTIVE`), snapshotting `PartCode` and `UnitPriceVnd`, and reconciling `PartsAmount` + `LaborAmount == GrossAmount` (`InvoiceLineItemsDriftCount == 0`) while keeping legacy summary-only payloads 100% compatible.
- **Workshop Spare-Parts Picker (`SCR-STF-02`) & Admin Parts Studio (`SCR-ADM-07`)**:
  - `staffInvoiceBuilder` (`SCR-STF-02`) embeds a model-filtered OEM Spare-Parts line-item picker that auto-computes `PartsAmount`, `GrossAmount`, voucher discounts, and two-step floor loyalty points while preserving in-progress drafts across SignalR events.
  - `adminSpareParts` (`app.admin.spareParts`, `SCR-ADM-07`) enables Admin SKU creation, pricing updates, category/model filtering, and activation toggles.

### 2.3 Printable Showroom & Workshop Receipts with SHA-256 Verification (`T-803`, `T-805`)
- **Receipt Endpoints (`/receipt`)**:
  - `GET /api/v1/service-invoices/{invoiceId}/receipt` issues `PrintableServiceReceiptDto` (`RCP-SVC-{InvoiceNumber}`) and `GET /api/v1/sales-orders/{orderId}/receipt` issues `PrintableSalesOrderReceiptDto` (`RCP-SLS-{OrderNumber}`), enforcing strict Customer ownership (`403 FORBIDDEN_RESOURCE_OWNER`) and `DealerStaff` branch isolation (`IDealerScoped` -> `404`).
  - Every receipt carries a deterministic 16-character uppercase hex SHA-256 `VerificationCode` computed by `ReceiptVerificationHash` (`SVC|{InvoiceId}|{InvoiceNumber}|{NetPaidAmount:0.00}` or `SLS|{SalesOrderId}|{OrderNumber}|{FinalAmount:0.00}`).
- **Reusable `<printable-receipt-modal>` (`@media print`)**:
  - Integrated across `staffInvoiceBuilder`, `staffSalesDesk`, `customerBookings`, and `customerLoyalty`, formatting dealership header, customer & ISO 3779 `VIN` details, itemized parts/labor table, voucher discount, net VND paid, loyalty points earned/tier summary, and SHA-256 seal for clean A4/thermal `$window.print()` output while hiding navigation chrome under `@media print`.

### 2.4 Granular RBAC (`40` Permissions) & Scheduled Shipper Reliability (`T-801`, `T-808`)
- **Expanded `40`-Permission RBAC Matrix**: Adds `Permissions.TestDrives.{Create,CancelOwn,ViewOwn,ViewDealer,UpdateStatus}` (`5`) and `Permissions.SpareParts.ManageCatalog` (`1`) -> `40` total policies (`Customer = 16`, `DealerStaff = 18`, `Admin = 28`, `RolePermissions = 62`).
- **Windows Modern Standby Task Scheduler Fix**: Hardened `.agents/scripts/register-daily-git-schedule.ps1` (`MotorcycleDealer-DailyGitShipper`) with `-DontStopOnIdleEnd` (`StopOnIdleEnd = False`), preventing Windows Modern Standby wake transitions from terminating catch-up runs with `0x00041306` (`267014`), and mapped all `347` workspace files across `Days 001..183` in `git-6month-schedule-plan.json`.

---

## 3. Final `v1.1.0` Empirical Verification & Quality-Loop Metrics

| Verification Suite | Verified Result | Coverage Highlights |
| :--- | :--- | :--- |
| **Phase 8 7-Stage Quality Loop** | **`15 / 15` Reviews + `6 / 6` Test Batteries PASS** | `5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T` (`run-phase-quality-loop.ps1 -Phase 8 -Round 3` in `210,766 ms`, `0` warnings) |
| **`.NET 10` xUnit Automated Suite** | **`541 / 541` PASS (`0` warnings, `0` flaky)** | `461` `Domain.UnitTests` + `80` SQL Server LocalDB `Api.IntegrationTests` (`1,623 / 1,623` Round 3 runs green, `StrayTestDbCount = 0`) |
| **AngularJS `1.8.3` SPA Verification** | **`56 / 56` Automated Checks PASS** | `42` `.js` + `3` `.mjs` files, `44` `ng-strict-di` registrations, `resolvePostLoginTarget` & `normalizePrintableReceipt` suites |
| **Full-Stack HTTP & SignalR Smoke** | **`9 / 9` Probes PASS (`SMK-01`..`SMK-09`)** | Health, OWASP headers, `401`/`403` `ProblemDetails`, `IDealerScoped`, SignalR, Analytics/CSV, plus `SMK-09` Spare-Parts/Test-Drives/Receipts |
| **Headless Edge CDP Closed-Loop E2E** | **`27 / 27` Navigable Routes (`0` Console Errors)** | Showroom CTA -> Login deep-link -> `TD-20261123-C239AB` zero-reload SignalR `TestDriveCreated`/`Confirmed` + itemized `1,000,000 VND` `RCP-SVC-*` modal |
| **SQL Server LocalDB Reconciliation** | **`25 / 25` 3NF Tables (`0` Drift)** | `LedgerDriftCount = 0`, `SlotDriftCount = 0/291`, `VoucherQuotaDriftCount = 0/5`, `InvoiceLineItemsDriftCount = 0`, `.docx` `validate.py` PASS |
