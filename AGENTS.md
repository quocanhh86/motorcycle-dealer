# AGENTS.md — Workspace Engineering Constitution

This file is the authoritative, always-on governance rulebook for human developers and AI coding agents operating in the **Motorcycle Dealer & Loyalty Management Platform** workspace (`https://github.com/quocanhh86/motorcycle-dealer`). Every planning decision, specification update, script, migration, backend service, and frontend component MUST comply with these rules.

---

## 1. Canonical Project Contract & Documentation Index

| Attribute | Canonical Value |
| :--- | :--- |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **Repository** | `https://github.com/quocanhh86/motorcycle-dealer` |
| **Timeline & Team** | `02/2026 - 08/2026` · Solo Full-Stack Engineering Project (`Team Size: 1`) |
| **Backend Stack** | `C#`, `ASP.NET Core Web API` (`.NET 10` / `net10.0`), `Entity Framework Core 10`, `SignalR` |
| **Architecture Pattern** | 4-Layer Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebApi`) + Unit & Integration Tests |
| **Frontend Stack** | Decoupled Modular `AngularJS 1.8` SPA (`angular@1.8.3`, `@uirouter/angularjs`, `chart.js@4`, `@microsoft/signalr`) |
| **Database** | Microsoft SQL Server (`(localdb)\MSSQLLocalDB` default + SQL Server 2022 Docker), DB `MotorcycleDealerDb` (`27` normalized 3NF tables) |
| **User Roles & RBAC** | `Customer` (`20`), `DealerStaff` (`22`, strictly scoped to `DealerId`), `Admin` (`32`, platform-wide) — `48` unique permissions |

### Single Source of Truth (SSOT) Documents

Before modifying any design or writing code, consult and synchronize the canonical specifications:

1. **CV & Raw Scope Baseline**: [REQUIREMENTS.md](./REQUIREMENTS.md) · **Landing Overview**: [README.md](./README.md)
2. **Product Requirements (`FR-01`..`FR-09`)**: [docs/PRD.md](./docs/PRD.md) · **Architecture & ADRs (`ADR-001`..`ADR-006`)**: [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)
3. **SQL Server 27-Table 3NF Data Dictionary**: [docs/DATABASE_DESIGN.md](./docs/DATABASE_DESIGN.md)
4. **48-Permission RBAC, REST/SignalR Contracts (`10` Events) & Domain Flows**: [docs/DESIGN.md](./docs/DESIGN.md)
5. **Master 9-Phase WBS (`T-101`..`T-908`, `80` Tasks) & Gate Tracker**: [TASKS.md](./TASKS.md) · [PROGRESS.md](./PROGRESS.md)
6. **Executive Word Specification**: [docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx](./docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx)
7. **OpenAPI 3.1, QA Plan, Runbook & Releases**: [docs/openapi.v1.yaml](./docs/openapi.v1.yaml) · [docs/TEST_PLAN.md](./docs/TEST_PLAN.md) · [docs/RUNBOOK.md](./docs/RUNBOOK.md) · [docs/RELEASE_NOTES_v1.1.0.md](./docs/RELEASE_NOTES_v1.1.0.md) · [docs/RELEASE_NOTES_v1.2.0.md](./docs/RELEASE_NOTES_v1.2.0.md)
8. **24-Screen UI/UX Catalog (`SCR-PUB-01`..`SCR-ADM-08`, `30` UI-Router Routes: `26` Primary + `4` Aliases)**: [Design System & Nav](./docs/screens/DESIGN_SYSTEM_AND_NAVIGATION.md) · [Customer Screens](./docs/screens/CUSTOMER_PORTAL_SCREENS.md) · [Staff & Admin Screens](./docs/screens/STAFF_AND_ADMIN_SCREENS.md)
9. **Vertical Module Blueprints (`MOD-01`..`MOD-06`)**: [MOD_01](./docs/modules/MOD_01_AUTH_AND_RBAC.md) · [MOD_02](./docs/modules/MOD_02_SHOWROOM_AND_VIN_GARAGE.md) · [MOD_03](./docs/modules/MOD_03_SERVICE_BOOKING_AND_REMINDERS.md) · [MOD_04](./docs/modules/MOD_04_LOYALTY_TIERS_AND_VOUCHERS.md) · [MOD_05](./docs/modules/MOD_05_SIGNALR_REALTIME_NOTIFICATIONS.md) · [MOD_06](./docs/modules/MOD_06_ADMIN_ANALYTICS_AND_REPORTING.md)
10. **Basic & Detail Design Suite**: [BD_00](./docs/basic-design/BD_00_BASIC_DESIGN_MASTER.md) · [DD_01](./docs/detail-design/DD_01_COMMON_SPECIFICATION.md) · [DD_02](./docs/detail-design/DD_02_DATABASE_AND_TABLE_UPDATES.md) · [DD_03](./docs/detail-design/DD_03_API_SPECIFICATIONS.md) · [DD_04](./docs/detail-design/DD_04_SCREEN_SPECIFICATIONS.md)

---

## 2. Strict Phase-Gate Governance Rule

Development is structured into **9 sequential, gated engineering phases** (`Phases 1–9+`):

| Phase | Scope | Allowed Workspace Artifacts | Exit Gate Requirement |
| :--- | :--- | :--- | :--- |
| **Phase 1** | Documentation, Deep-Dive Specs & `.docx` | `.md` (`docs/**`) + `docs/*.yaml` + `docs/*.docx` ONLY (ZERO code) | Cross-doc Reader Testing + `.docx` OpenXML validation + User sign-off |
| **Phase 2** | Antigravity `.agents/` & MCP Suite | `.agents/{rules,skills,scripts,plugins}/*`, `.agents/*.json`, spec sync | Plugin/skill/MCP JSON validation + hook/MCP/script dry-run pass |
| **Phase 3** | Clean Arch (`.NET 10`) & AngularJS Skeleton | `backend/MotorcycleDealer.sln` (6 projects), `frontend/` SPA, `docker-compose.yml` | `dotnet build --warnaserror` & `dotnet test` green + SPA shell boots |
| **Phase 4** | SQL Server 3NF Migrations, Seed & RBAC | Core EF configs, migrations, seeder, JWT/Refresh auth, UI-Router guards | RBAC matrix & `DealerId` isolation integration tests pass |
| **Phase 5** | Showroom, Garage, Booking & VIN Worker | Catalog, VIN stock, `SalesOrders`, `CustomerVehicles`, `ServiceTimeSlots`, `ServiceBookings`, `VinMaintenanceReminderWorker` | Concurrent `RowVersion` slot test + deduplicated VIN reminder test pass |
| **Phase 6** | Loyalty Engine & SignalR Real-Time Hub | `ServiceInvoices`, `PointLedgerEntries`, `LoyaltyTiers`, vouchers, `NotificationHub` (`/hubs/notifications`) | Atomic ledger idempotency test + live WebSocket alert verification |
| **Phase 7** | Admin `Chart.js` Analytics, QA & `v1.0` | Multi-dealer revenue/booking endpoints, `Chart.js` views, OWASP audit, `.docx` sync | 100% CV requirements verified end-to-end + `v1.0.0` release ready |
| **Phase 8** | `v1.1` Test-Drives, Spare-Parts & Receipts | `TestDriveBookings`, `SpareParts`, `ServiceInvoiceLineItems`, `<printable-receipt-modal>`, `183`-day git schedule | `25`-table migration + `40`-permission RBAC + `7` SignalR events + sync gate pass |
| **Phase 9** | `v1.2` CSAT Reviews & VIN Warranty Claims | `ServiceReviews` (`+25` bonus pts), `WarrantyClaims` (`36m/30,000km`), `SCR-CUS-07`/`STF-07`/`ADM-08` | `27`-table migration + `48`-permission RBAC + `10` SignalR events + `30` routes + sync gate pass |

### Mandatory Execution Protocol

1. **Never Jump Phases**: NEVER scaffold or edit files belonging to Phase `N+1` until Phase `N` exit criteria are verified and approved.
2. **Requirement & Task Traceability**: Every implementation step MUST trace to `FR-01`..`FR-09`, `ADR-001`..`ADR-006`, and a Task ID (`T-101`..`T-908`) in [TASKS.md](./TASKS.md).
3. **Spec-First & Post-Completion Synchronization**: Update affected specifications in `docs/` when contracts evolve, and upon finishing ANY phase, feature, or fix, synchronize all related `docs/`, `.docx`, trackers, `.agents/`, and `.agents/scripts/git-6month-schedule-plan.json` (`verify-post-task-sync.mjs`).

---

## 3. Non-Negotiable Engineering Guardrails

### 3.1 Clean Architecture Dependency Rule (`ADR-001`)
- Dependencies MUST point strictly **inward**:
  - `MotorcycleDealer.Domain` -> **Zero external package or project dependencies** (pure C# entities, value objects, domain exceptions, domain events, `Permissions` constants, and marker interfaces).
  - `MotorcycleDealer.Application` -> References `Domain` ONLY (CQRS handlers/services, DTOs, `FluentValidation` validators, and port interfaces).
  - `MotorcycleDealer.Infrastructure` -> References `Application` and `Domain` (`MotorcycleDealerDbContext`, `27` `IEntityTypeConfiguration<T>`, SQL Server migrations, JWT/Refresh providers, SignalR adapter, `VinMaintenanceReminderWorker`).
  - `MotorcycleDealer.WebApi` -> Composition root referencing `Application` and `Infrastructure` (thin `/api/v1/*` controllers, `NotificationHub`, `PermissionAuthorizationHandler`, `ProblemDetails` middleware).
- **Prohibited**: Referencing `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, or SQL/HTTP transport types inside `MotorcycleDealer.Domain`; injecting `MotorcycleDealerDbContext` into controllers; returning raw EF Core entities over HTTP.

### 3.2 Multi-Dealer Row-Level Isolation & 48-Permission RBAC (`ADR-002`, `FR-01`)
- **Three Canonical Roles (`48` Permissions)**: `Customer` (`20`), `DealerStaff` (`22`), `Admin` (`32`).
- **8 `IDealerScoped` Entities**: Every dealer-owned aggregate (`DealerInventories`, `ServiceTimeSlots`, `SalesOrders`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, `WarrantyClaims`) MUST implement `IDealerScoped` (`Guid DealerId { get; }`).
- **EF Core Global Query Filter & Write Guard**: `MotorcycleDealerDbContext` MUST filter all `8` `IDealerScoped` entities by `e.DealerId == currentUser.DealerId` whenever the caller holds `DealerStaff` (cross-dealer visibility is restricted to `Admin`). `DealerStaff` handlers MUST NEVER trust client-supplied `dealerId`.
- **Customer Ownership Enforcement**: `Customer` endpoints MUST scope queries/writes to `currentUser.UserId` (`CustomerVehicles`, `SalesOrders`, `TestDriveBookings`, `ServiceBookings`, `ServiceInvoices`, `ServiceReviews`, `WarrantyClaims`, `LoyaltyAccounts`, `CustomerVouchers`, `Notifications`).
- **Granular Policies**: Protect API actions with `[HasPermission(Permissions.<Module>.<Action>)]` / `[Authorize(Policy = ...)]` across all `48` policies (e.g., `Bookings.Create`, `TestDrives.Create`, `SpareParts.ManageCatalog`, `Reviews.CreateOwn`, `Warranty.ManageDealer`, `Loyalty.RedeemVoucher`, `Analytics.ViewAllDealers`). Mirror checks in AngularJS via `$transitions.onBefore` across all **`30` navigable UI-Router routes** (`26` primary + `4` aliases) and `has-permission` (server enforcement remains authoritative).

### 3.3 4 Immutable Financial/Audit Tables & Loyalty Formula (`ADR-003`, `FR-05`, `FR-09`)
- **Strict Immutability (`4` Tables)**: **NEVER** execute `UPDATE` or `DELETE` (hard or soft) against **`PointLedgerEntries`**, **`SalesOrders`**, **`ServiceInvoices`**, or **`ServiceInvoiceLineItems`**. Corrections MUST append a compensating `PointLedgerEntries` row with a signed `PointsDelta` and reference note.
- **Idempotency**: Every `PointLedgerEntries` row MUST carry a unique `IdempotencyKey` enforced by a SQL Server `UNIQUE` index.
- **Canonical Point, Bonus & Tier Rules**:
  - Base accrual rate: **`10,000 VND = 1 base point`** (`PointsEarned = (int)Math.Floor(Math.Floor(NetPaidAmount / 10000m) * TierMultiplier)`).
  - Canonical 4-tier progression by `LifetimeQualifyingPoints`: `Bronze` (`0+`, `1.00x`), `Silver` (`500+`, `1.20x`), `Gold` (`2,000+`, `1.50x`), `Platinum` (`5,000+`, `2.00x`).
  - Voucher redemption deducts `AvailablePoints` ONLY; post-service CSAT `ServiceReviews` (`SERVICE_REVIEW:{invoiceId}:BONUS`) credit `+25` `AvailablePoints` ONLY; `LifetimeQualifyingPoints` changes strictly on qualifying spend.

### 3.4 Optimistic Concurrency & Transactional Integrity (`ADR-005`, `FR-04`, `FR-09`)
- **Mandatory `RowVersion` (`8` Tables)**: Configure `[Timestamp]` / `.IsRowVersion()` concurrency tokens on **`ServiceTimeSlots`**, **`DealerInventories`**, **`LoyaltyAccounts`**, **`CustomerVouchers`**, **`ServiceBookings`**, **`TestDriveBookings`**, **`ServiceReviews`**, and **`WarrantyClaims`**.
- **Anti-Double-Booking & Warranty Rules**: Workshop slot booking (`BookedCount < MaxCapacity`), showroom `TestDriveBookings`, 1:1 `ServiceReviews`, and `WarrantyClaims` (`<= 36m` & `<= 30,000 km` OEM coverage) MUST commit atomically with `RowVersion`/unique verification, mapping contention/duplicates to `409 Conflict` (`RFC 7807 ProblemDetails`).
- **Deduplicated VIN Maintenance Reminders**: `VinMaintenanceReminderWorker` (`BackgroundService`) evaluates `CustomerVehicles` against `MaintenanceRules` (`IntervalKm` / `IntervalMonths`) with unique `DeduplicationKey` (`{VIN}:{MaintenanceRuleId}:{TargetMileageKm}`) on `MaintenanceReminders`.

### 3.5 SQL Server Data Types, Time & Naming Conventions
- **Schema**: Exactly **`27` normalized 3NF PascalCase plural tables** in `MotorcycleDealerDb`: `Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `RefreshTokens`, `Dealers`, `MotorcycleModels`, `DealerInventories`, `SalesOrders`, `TestDriveBookings`, `CustomerVehicles`, `MaintenanceRules`, `MaintenanceReminders`, `WarrantyClaims`, `ServiceTimeSlots`, `ServiceBookings`, `SpareParts`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `ServiceReviews`, `LoyaltyTiers`, `LoyaltyAccounts`, `PointLedgerEntries`, `VoucherCatalogs`, `CustomerVouchers`, `Notifications`.
- **Primary Keys & Money**: `Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()` (`Guid.CreateVersion7()`); money/multipliers strictly `DECIMAL(18,2) NOT NULL` / `decimal` (**never** `float`/`double`).
- **Timestamps & VIN**: `DATETIMEOFFSET(7)` in **UTC** (`*Utc` suffix; convert to `Asia/Ho_Chi_Minh` solely in AngularJS presentation); ISO 3779 17-char uppercase alphanumeric `VIN` (`VARCHAR(17)` excluding `I`, `O`, `Q`).

### 3.6 API, 10-Event SignalR Hub & AngularJS 1.8 SPA Standards (`ADR-004`, `ADR-006`)
- **REST API Versioning & Errors**: Prefix endpoints with `/api/v1/`. All `4xx`/`5xx` responses emit `RFC 7807 ProblemDetails` (`application/problem+json`) with `type`, `title`, `status`, `detail`, `traceId`, and validation `errors` (zero stack/SQL leaks outside `Development`).
- **SignalR Real-Time Contract (`10` Events)**: Expose `NotificationHub : Hub<INotificationClient>` at `/hubs/notifications`, joining `dealer-{dealerId}`, `user-{userId}`, and `role-Admin` strictly from verified JWT claims on `OnConnectedAsync`. Dispatch **`10` canonical events**: `BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`, `TestDriveCreated`, `TestDriveStatusChanged`, `ServiceReviewSubmitted`, `WarrantyClaimCreated`, and `WarrantyClaimStatusChanged`.
- **AngularJS 1.8 Discipline**: Modular `.component()` (`controllerAs: '$ctrl'`), one-time bindings (`::`), `$onDestroy` SignalR/`Chart.js` cleanup, `$rootScope.$applyAsync()` digest bridging, and a single-flight `$http` `401` `/api/v1/auth/refresh` queue across all **`30` navigable routes** (`26` primary + `4` aliases).

### 3.7 Security & Zero-Secrets Hygiene
- **Zero Secrets in Git**: NEVER commit real DB passwords, production JWT keys (`< 256-bit` forbidden), `.env` files, or `bin/`/`obj/`/`node_modules/`/`frontend/src/vendor/` outputs. Load real secrets via `dotnet user-secrets` or environment variables (`Jwt__SigningKey`, `ConnectionStrings__DefaultConnection`).

---

## 4. Bilingual (English + Vietnamese) Natural Intent Router

### 4.1 Language Collaboration Policy
- **Repository Artifacts (`100% English`)**: All code identifiers, SQL objects, API payloads, git commits, test names, and specification documents MUST be written in clear **International English**.
- **Developer Conversation (`Bilingual EN / VI`)**: Reply in the developer's language (**English** or **Tiếng Việt**) while keeping exact English technical identifiers (`ServiceBookings`, `TestDriveBookings`, `PointLedgerEntries`, `RowVersion`).

### 4.2 Skill Routing Matrix (`.agents/skills/`)

Workspace skills live solely under `.agents/skills/` (registered in `.agents/skills.json`):

| Workspace Skill Path | English Trigger Intents | Vietnamese Trigger Intents (Tiếng Việt) | Primary Scope |
| :--- | :--- | :--- | :--- |
| `.agents/skills/dotnet-clean-api/SKILL.md` | "Add API endpoint", "Create command/validator", "48-permission RBAC / JWT", "ProblemDetails" | "Tạo API mới", "Viết service Application", "Phân quyền RBAC 48 quyền / JWT", "ProblemDetails" | `.NET 10` Clean Architecture layers, controllers, `FluentValidation`, `[HasPermission]` |
| `.agents/skills/sqlserver-efcore-dba/SKILL.md` | "Add EF Core migration", "Tune SQL index", "`IDealerScoped` filter", "Seed 27 tables" | "Tạo migration SQL Server", "Tối ưu index", "Cấu hình 27 bảng EF Core", "Tạo dữ liệu mẫu" | `MotorcycleDealerDbContext`, `27` `IEntityTypeConfiguration<T>`, `8` `RowVersion`, seeder |
| `.agents/skills/angularjs-enterprise-spa/SKILL.md` | "Build AngularJS component", "30 UI-Router guards", "Chart.js widget", "`401` refresh queue" | "Tạo giao diện AngularJS", "Chặn route theo role", "Vẽ biểu đồ Chart.js", "Refresh token" | `frontend/` AngularJS 1.8 components, `30` routes, `Chart.js 4`, receipts & CSAT/warranty |
| `.agents/skills/loyalty-booking-domain/SKILL.md` | "Slot/test-drive/warranty rules", "Loyalty & +25 CSAT pts", "Line-item invoice / VIN worker" | "Đặt lịch / lái thử / bảo hành", "Tính điểm & +25 đánh giá", "Hóa đơn phụ tùng / nhắc VIN" | `SalesOrders`, `TestDriveBookings`, `ServiceBookings`, `ServiceReviews`, `WarrantyClaims`, `PointLedgerEntries` |
| `.agents/skills/signalr-realtime-hub/SKILL.md` | "Push instant booking/review/warranty alert", "SignalR hub groups", "`@microsoft/signalr` SPA" | "Thông báo đặt lịch / đánh giá / bảo hành realtime", "SignalR group đại lý", "Cập nhật không cần F5" | `/hubs/notifications`, `NotificationHub` (`10` events), `dealer-{dealerId}` isolation |
| `.agents/skills/qa-verification-suite/SKILL.md` | "Write unit/integration tests", "Double-booking race test", "Audit cross-dealer RBAC" | "Viết unit / integration test", "Kiểm thử đặt trùng giờ", "Kiểm tra bảo mật đại lý" | `Domain.UnitTests`, `Api.IntegrationTests`, RBAC/concurrency & post-task sync gates |
| `.agents/skills/docx-spec-generator/SKILL.md` | "Regenerate Word specification", "Sync `.docx` report", "Validate `.docx` OpenXML" | "Xuất lại file đặc tả Word `.docx`", "Đồng bộ báo cáo Word", "Kiểm tra định dạng `.docx`" | `docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx` build & OpenXML validation |
| `.agents/skills/mcp-devtools-orchestrator/SKILL.md` | "Orchestrate 9 MCP servers, E2E browser/WS capture, sync Postman/GitHub, inspect LocalDB" | "Tận dụng MCP, kiểm tra LocalDB/WebSocket/Playwright qua MCP, đồng bộ Postman/GitHub" | 9-server MCP orchestration, live LocalDB/WebSocket/E2E verification & DevOps sync |

### 4.3 Model Context Protocol (MCP) Integration Policy

1. **Workspace Stdio MCP Servers (`.agents/plugins/motorcycle-dealer-suite/mcp_config.json`)**:
   - `motorcycle-dealer-guardrails` (`.agents/scripts/mcp-dealer-guardrails-server.mjs`): Spec & governance oracle exposing `check_phase_gate`, `lookup_rbac_permission` (`48` permissions + `8` `IDealerScoped` rules), `lookup_db_table_spec` (`27` 3NF tables), `calculate_loyalty_preview`, and `get_wbs_tasks` (`80` WBS tasks).
   - `sqlserver-localdb-inspector` (`.agents/scripts/mcp-sqlserver-localdb-server.mjs`): Read-only `(localdb)\MSSQLLocalDB` (`MotorcycleDealerDb`) inspector (`sqlserver_status`, `list_sqlserver_tables`, `inspect_sqlserver_schema`, `query_readonly_sql`). *(Never query global `dbhub`, which connects to an external Oracle DB.)*
2. **Global MCP Servers (`7`)**: `context7_context7` (`.NET 10`/`EF Core 10`/`SignalR`/`AngularJS 1.8`/`Chart.js 4` docs), `playwright` (dual-tab zero-refresh SignalR E2E), `chrome-devtools-plugin_chrome-devtools` (WS frames, `RFC 7807`, `RISK-04` heap audits), `postman_postman` (`48`-permission contract monitors), `github` (`quocanhh86/motorcycle-dealer`), `figma_figma`, and `notion_notion`.

---

## 5. Definition of Done (DoD), 7-Stage Quality Loop & Post-Task Sync Gate

### 5.1 Per-Phase 7-Stage Quality Loop (`5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T`)
Every coding phase (**Phases 3–9+**) MUST pass all **7 stages** (`15` reviews + `6` test batteries) before marking any `T-*` task `[x]`:

1. **Stage 1 — `5x` Adversarial Review (`R1.1`..`R1.5`)**: Audit (`1`) `ADR-001` Clean Architecture purity, (`2`) SSOT/OpenAPI/`48`-permission/`30`-route & loyalty math parity, (`3`) `8`-table `IDealerScoped` tenancy, CORS & `RFC 7807`, (`4`) `8`-table `RowVersion` concurrency, `4` immutable financial/audit tables & SPA memory safety (`RISK-02`..`04`), and (`5`) `TreatWarningsAsErrors=true`, non-root Dockerfiles & `< 24 KB` rule caps.
2. **Stage 2 — `3x` Test Batteries (`T1.1`..`T1.3`)**: Run `.agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round 1`: (`T1.1`) Release `dotnet build --warnaserror` + xUnit, (`T1.2`) `frontend` `npm test` + live HTTP/ProblemDetails/`10`-event SignalR/CDP E2E smoke, (`T1.3`) `verify-dealer-platform.ps1` + `verify-post-task-sync.mjs` + MCP `check_phase_gate`/LocalDB reconciliation + `docker compose config -q` + determinism re-test.
3. **Stages 3–5 (`Fix -> 5x Review -> Refactor`)**: Fix all defects from Stages 1–2, re-audit `5x` (`R2.1`..`R2.5`), and execute clean-code refactoring + boundary `[Theory]` expansion without contract drift.
4. **Stages 6–7 (`5x Final Review -> 3x Final Test`)**: Complete final `5x` review (`R3.1`..`R3.5`), pass `.agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round 3` (`0` warnings, `0` flaky tests), and log the `15R + 6T` ledger in [PROGRESS.md](./PROGRESS.md).

### 5.2 Mandatory Post-Task Synchronization Gate (Zero Stale / Zero Unmapped Files)
Every completed **phase, feature, or fix** MUST atomically synchronize all workspace artifacts before completion sign-off:
1. **Documentation Suite (`docs/`)**: Update every affected document across `PRD.md`, `ARCHITECTURE.md`, `DATABASE_DESIGN.md`, `DESIGN.md`, `openapi.v1.yaml`, `TEST_PLAN.md`, `RUNBOOK.md`, `modules/MOD_01..06_*.md`, `screens/*.md`, `basic-design/BD_00_*.md`, `detail-design/DD_01..04_*.md`, and `RELEASE_NOTES_*.md`.
2. **Executive Word Report (`.docx`)**: Update `.agents/scripts/build-spec-docx.mjs`, regenerate `docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx`, and pass OpenXML `validate.py`.
3. **Trackers, Snapshot & `.agents/` Suite**: Reconcile `TASKS.md`, `PROGRESS.md`, `README.md`, `.agents/scripts/baseline-spec-snapshot.json` (`node .agents/scripts/sync-progress.mjs --check`), and related `.agents/{rules,skills,plugins/motorcycle-dealer-suite/hooks,scripts}/*`.
4. **100% Git Schedule Coverage (`git-6month-schedule-plan.json`)**: Map `100%` of git-trackable workspace files in `.agents/scripts/git-6month-schedule-plan.json` (`0` unmapped files, `0` missing files on disk, `0` duplicate future paths, `0` empty future days), enforced via `node .agents/scripts/verify-post-task-sync.mjs`.

### 5.3 Final Gate Sign-Off Checklist
- [ ] **Phase Gate & Rule Hygiene**: Active phase `compliant: true` (`check_phase_gate`); all `5` `AGENTS.md` files under byte caps with zero YAML frontmatter.
- [ ] **Architecture, Tenancy & Immutability**: Zero inward dependency breaches; `8` `IDealerScoped` & `8` `RowVersion` tables enforced; `4` financial/audit tables strictly immutable.
- [ ] **7-Stage Quality Loop**: Phases 3–9+ pass `5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T` with `0` warnings and `0` regressions.
- [ ] **Post-Task Sync & Git Schedule Gate**: Both `node .agents/scripts/sync-progress.mjs --check` and `node .agents/scripts/verify-post-task-sync.mjs` exit `0` (`PASS`).
