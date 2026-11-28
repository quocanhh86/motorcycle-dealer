---
name: qa-verification-suite
description: >-
  Use this skill when writing or running Clean Architecture dependency tests, xUnit domain unit tests,
  SQL Server 27-table integration tests, parallel RowVersion double-booking, test-drive, and duplicate
  CSAT review race tests, 48-permission cross-dealer IDOR/RBAC security audits, full-stack verification
  scripts, or post-task documentation and git-schedule sync checks ("Viết unit test / integration test",
  "Kiểm thử đặt trùng khung giờ", "Kiểm tra bảo mật phân quyền đại lý", "Chạy kiểm thử QA toàn hệ thống").
---

# QA Verification, 7-Stage Quality Loop & Post-Task Sync Suite (`qa-verification-suite`)

Authoritative testing and quality-gate playbook for the **Motorcycle Dealer & Loyalty Management Platform** (`v1.4.0`, `FR-01`..`FR-09`, `ADR-001`..`ADR-007`). Every phase exit gate (`T-208`..`T-1008` across `88` WBS tasks) must neutralize all **6 Pre-Mortem Risks (`RISK-01`..`RISK-06`)** tracked in [PROGRESS.md](../../../PROGRESS.md) and pass the **Mandatory Post-Task Synchronization Gate**.

---

## 1. Pre-Mortem Risk-to-Test Coverage Matrix (`RISK-01`..`RISK-06`)

| Risk ID | Failure Mode | Mandatory Automated Verification Gate | Target Test Suite / Script |
| :--- | :--- | :--- | :--- |
| **`RISK-01`** | Phase-gate violation, Clean Architecture inward breach, or stale docs/git plan | File-boundary check + reflection test (`Domain` 0 external deps; `Application` refs `Domain` only) + `100%` post-task docs & git-plan sync | `.agents/scripts/audit-dealer-guardrails.ps1` + `verify-post-task-sync.mjs` + `ArchitectureDependencyTests.cs` |
| **`RISK-02`** | `ServiceTimeSlots` or `TestDriveBookings` double-booking race condition | `Task.WhenAll` parallel booking/test-drive tests asserting exact capacity wins (`201 Created`) and overflow contenders receive `409 Conflict` `ProblemDetails` | `SlotBookingConcurrencyTests.cs` + `TestDriveConcurrencyTests.cs` (`T-510`, `T-807`) |
| **`RISK-03`** | Loyalty rounding drift, itemized invoice mismatch, or tier demotion on voucher spend | Two-step floor unit tests + 4-tier boundary tests + `UQ_PointLedgerEntries_IdempotencyKey` replay + `SUM(PointsDelta) == AvailablePoints` & `GrossAmount == SUM(LineTotalVnd)` SQL checks | `LoyaltyCalculatorTests.cs` + `LoyaltyLedgerIdempotencyTests.cs` (`T-609`, `T-807`) |
| **`RISK-04`** | AngularJS `$digest` errors, detached SignalR handlers, or `Chart.js` canvas leaks | `30`-route transition churn check + `Chart.js` remount assertion + zero browser console errors across all `10` SignalR events | Headless Chromium CDP / Playwright / Chrome DevTools E2E (`T-609`, `T-705`, `T-807`, `T-907`) |
| **`RISK-05`** | Cross-dealer `DealerStaff` IDOR / BOLA leakage across API or SignalR | `48`-permission matrix (`Customer=20`, `DealerStaff=22`, `Admin=32`) + `8`-table `IDealerScoped` isolation (`DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, `WarrantyClaims`) | `RbacAndTenancyTests.cs` (`T-409`, `T-706`, `T-807`, `T-907`) |
| **`RISK-06`** | `VinMaintenanceReminderWorker` emitting duplicate alerts across polling ticks | Execute worker sweep 3x back-to-back for due `VIN`; assert **exactly 1** `MaintenanceReminders` row (`{VIN}:{MaintenanceRuleId}:{TargetMileageKm}`) | `VinMaintenanceReminderIdempotencyTests.cs` (`T-510`) |

---

## 2. Clean Architecture & Domain Unit Tests (`MotorcycleDealer.Domain.UnitTests`)

1. **Inward Dependency Guard (`RISK-01`, `ADR-001`)**:
   - `MotorcycleDealer.Domain` references **none** of `Application`, `Infrastructure`, `WebApi`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`.
   - `MotorcycleDealer.Application` references `Domain` ONLY; zero controllers inject `MotorcycleDealerDbContext` or return `BaseEntity` types.
2. **Canonical Loyalty Math, 4-Tier, Itemized Line-Item & ISO 3779 VIN Unit Tests (`RISK-03`)**:
   - Two-step floor accrual: `PointsEarned = Floor(Floor(NetPaidAmount / 10,000) * TierMultiplier)` (`455,000 VND` at `Gold 1.50x` -> `67` pts; `830,000 VND` at `Silver 1.20x` -> `99` pts).
   - 4-tier non-downgrade boundaries (`Bronze 0`, `Silver 500`, `Gold 2,000`, `Platinum 5,000`) on voucher spend.
   - `ServiceInvoiceLineItems` arithmetic (`LineTotalVnd == Quantity * UnitPriceVnd`, `GrossAmount == LaborAmount + PartsAmount`) and ISO 3779 17-char `VIN` (`^[A-HJ-NPR-Z0-9]{17}$`, excluding `I`, `O`, `Q`).

---

## 3. SQL Server 27-Table Integration, Security & Concurrency Tests (`MotorcycleDealer.Api.IntegrationTests`)

Run against real SQL Server (`(localdb)\MSSQLLocalDB` or SQL Server 2022 container — **never** EF Core InMemory):

1. **Cross-Dealer Isolation (`8` `IDealerScoped` Tables) & `48`-Permission Matrix (`RISK-05`)**:
   - Verify `401` anonymous `ProblemDetails`, `403 FORBIDDEN_PERMISSION` across Separation of Duties (`Customer` vs `DealerStaff` vs `Admin`), `403`/`404` cross-dealer `DealerStaff` isolation across `DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, and `WarrantyClaims`, and customer ownership guards on `/vehicles`, `/bookings`, `/test-drives`, `/warranty-claims`, `/service-invoices/{id}/receipt`, and `/sales-orders/{id}/receipt`.
2. **Parallel `Task.WhenAll` Concurrency Tests (`RISK-02`, `8` `RowVersion` Tables)**:
   - Verify exact capacity enforcement (`201 Created` vs `409 Conflict`) on `ServiceTimeSlots`, `TestDriveBookings`, `DealerInventories`, `LoyaltyAccounts`, `CustomerVouchers`, `ServiceBookings`, `ServiceReviews`, and `WarrantyClaims`.
3. **4-Table Financial Immutability & VIN Reminder Deduplication (`RISK-03`, `RISK-06`)**:
   - Confirm `AppendOnlyLedgerInterceptor` and SQL triggers block any `UPDATE`/`DELETE` on `PointLedgerEntries` and `ServiceInvoiceLineItems` (alongside immutable `SalesOrders` & `ServiceInvoices`), and confirm `UQ_MaintenanceReminders_DedupKey` emits zero duplicate reminders.

---

## 4. Mandatory Per-Phase 7-Stage Quality Loop (`Phases 3–10`) & Post-Task Sync Gate

Every coding phase (**Phases 3–10**) enforces **`5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test`** (`15` Adversarial Reviews + `6` Test Batteries) plus the **Mandatory Post-Task Synchronization Gate**:

| Stage | Gate Code | Execution Requirement |
| :--- | :--- | :--- |
| **Stage 1** | **`Review 5x` (`R1.1`..`R1.5`)** | Audit across 5 lenses: (`1`) `ADR-001` Clean Arch purity; (`2`) SSOT, OpenAPI 3.1, `48`-permission, `30`-route, `16`-mobile-view & loyalty math parity; (`3`) `8`-table `IDealerScoped` tenancy, `10`-event SignalR JWT group isolation, CORS & `RFC 7807`; (`4`) `8`-table `RowVersion`, `4`-table immutability & SPA `$onDestroy` memory safety; (`5`) `--warnaserror`, non-root Dockerfiles & `AGENTS.md` byte caps. |
| **Stage 2** | **`Test 3x` (`T1.1`..`T1.3`)** | Run `run-phase-quality-loop.ps1 -Phase <3..10> -Round 1`: (`T1.1`) Release `dotnet build --warnaserror` + xUnit; (`T1.2`) `frontend` `npm test` + `smoke-fullstack.ps1` + `30`-route Headless Chromium CDP E2E; (`T1.3`) `verify-dealer-platform.ps1` + `verify-post-task-sync.mjs` + MCP `check_phase_gate` + LocalDB `27`-table SQL reconciliation + `docker compose config -q` + determinism re-test. |
| **Stages 3–5** | **`Fix -> 5R -> Refactor`** | Fix all defects, re-audit `5x` (`R2.1`..`R2.5`), and execute clean-code refactoring + boundary `[Theory]` test expansion without contract drift. |
| **Stages 6–7** | **`5R -> 3T` + Post-Task Sync** | Pass final `5x` review (`R3.1`..`R3.5`), pass `run-phase-quality-loop.ps1 -Phase <3..10> -Round 3` (`0` warnings, `0` flaky tests), synchronize all affected `docs/`, `.docx`, trackers, `.agents/`, and `git-6month-schedule-plan.json`, and pass `node .agents/scripts/verify-post-task-sync.mjs`. |

---

## 5. Workspace Automation Scripts Execution

```powershell
# 1. Per-Phase 3-Battery Quality-Loop Test Runner (Phases 3..10)
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase 10 -Round 1
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase 10 -Round 3

# 2. Master Platform, Guardrails, MCP, Build/Test & Post-Task Sync Verifier
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/verify-dealer-platform.ps1 -Phase 10

# 3. Standalone Post-Task Docs, Code/Snapshot & 183-Day Git Schedule Sync Gate
node .agents/scripts/verify-post-task-sync.mjs

# 4. Full-Stack HTTP, RFC 7807, 48-RBAC & 10-Event SignalR Smoke Suite
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1
```

---

## 6. MCP-Accelerated QA Workflows

1. **Pre-Test Oracle (`motorcycle-dealer-guardrails` MCP)**: Call `check_phase_gate`, `get_wbs_tasks` (`88` tasks), `calculate_loyalty_preview`, and `lookup_rbac_permission` (`48` policies).
2. **Dual-Tab SignalR & 30-Route Guard E2E (`playwright` MCP)**: Prove zero-refresh `BookingCreated` & `TestDriveCreated` delivery and `/403` redirects.
3. **Contract Monitors (`postman_postman` MCP & Postman CLI)**: Validate `application/problem+json` `errorCode` and `traceId` across `400`/`401`/`403`/`404`/`409`/`422`.
4. **SQL 27-Table Reconciliation (`sqlserver-localdb-inspector` MCP)**: Verify `AvailablePoints == SUM(PointsDelta)`, `BookedCount <= MaxCapacity`, `QuotaRedeemed == COUNT(CustomerVouchers)`, and `GrossAmount == SUM(LineTotalVnd)` (`0` drift rows).
5. **Zero-Console-Error & Heap Sign-Off (`chrome-devtools-plugin_chrome-devtools` MCP)**: Verify zero JS console exceptions and zero `Chart.js`/SignalR memory leaks.
