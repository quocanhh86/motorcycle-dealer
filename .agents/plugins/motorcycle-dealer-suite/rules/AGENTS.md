# `motorcycle-dealer-suite` Plugin Rules

Always-on rules for the `motorcycle-dealer-suite` plugin in the **Motorcycle Dealer & Loyalty Management Platform** (`quocanhh86/motorcycle-dealer`). Complements root `AGENTS.md`, `backend/AGENTS.md`, `frontend/AGENTS.md`, and `mobile/AGENTS.md`.

## 1. Strict Phase-Gate & Architectural Enforcement (`Phases 1–10`)
- **Verify Phase Gate First**: Invoke `check_phase_gate` on `motorcycle-dealer-guardrails` (`Phases 1..10`) and check `PROGRESS.md` before scaffolding top-level folders (never create `backend/`/`frontend/` before Phase 3 or `mobile/` before Phase 10).
- **Inward Clean Architecture (`ADR-001`, `ADR-007`)**: `Domain` has zero dependencies; `Application` -> `Domain`; `Infrastructure` -> `Application` + `Domain`; `WebApi` is composition root (never inject `MotorcycleDealerDbContext` into controllers or return EF entities). In `mobile/`, `Mobile.Core` (`net10.0`) MUST NEVER set `<UseMaui>true</UseMaui>` or reference `Microsoft.Maui.*`.
- **Multi-Dealer Isolation (`ADR-002`)**: All **`8` `IDealerScoped` entities** (`DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, `WarrantyClaims`) enforce EF Core Global Query Filters (`DealerId == currentUser.DealerId` for `DealerStaff`) and server-derived `DealerId` writes. Verify **`48` canonical permissions** (`Customer=20`, `DealerStaff=22`, `Admin=32`) via `lookup_rbac_permission` across Backend, Frontend, and `Mobile.Core`.

## 2. Lifecycle Hooks, Immutability, Concurrency, SignalR & Mobile Guardrails
- **Active Hooks (`hooks.json`)**:
  - `SessionStart` (`hooks/session-start.mjs`) injects active Phase `1..10` context & Post-Task Sync reminder.
  - `PreToolUse` (`hooks/safety-guard.mjs`) blocks `DROP DATABASE`, `TRUNCATE TABLE`, `UPDATE`/`DELETE` on `PointLedgerEntries` and `ServiceInvoiceLineItems`, `DELETE` on `SalesOrders` or `ServiceInvoices`, `git reset --hard`, and `git push --force`.
- **Financial Immutability & Concurrency (`ADR-003`, `ADR-005`)**:
  - **`4` Immutable Financial/Audit Tables**: `PointLedgerEntries` (append-only, unique `IdempotencyKey`), `SalesOrders`, `ServiceInvoices`, and `ServiceInvoiceLineItems` MUST NEVER be updated/deleted (`ServiceReviewBonus` `+25` credits `AvailablePoints` ONLY).
  - Loyalty formula: `Floor(Floor(NetPaidAmount / 10000m) * TierMultiplier)` across `Bronze` (`0`/`1.00x`), `Silver` (`500`/`1.20x`), `Gold` (`2,000`/`1.50x`), `Platinum` (`5,000`/`2.00x`), cross-checked via `calculate_loyalty_preview`.
  - Guard all **`8` `RowVersion` tables** (`DealerInventories`, `ServiceTimeSlots`, `ServiceBookings`, `LoyaltyAccounts`, `CustomerVouchers`, `TestDriveBookings`, `ServiceReviews`, `WarrantyClaims`) mapping conflicts to `409 ProblemDetails`.
  - Route `NotificationHub` (`/hubs/notifications`) **`10` events** strictly from JWT claims across **`30` Web SPA routes** and **`16` compiled `x:DataType` `.NET 10 MAUI` Android views** (`SourceGen`, C# 13 `partial` `[ObservableProperty]`, encrypted `9`-table `SQLCipher` WAL rejecting offline financial/`RowVersion` mutations).

## 3. Custom MCP Servers & Zero-Pollution Rules
- **Spec & DB Inspection**: Call `lookup_db_table_spec` (`27` 3NF tables) and `get_wbs_tasks` (`88` WBS tasks, `T-101`..`T-1008`) on `motorcycle-dealer-guardrails`. Inspect SQL Server `(localdb)\MSSQLLocalDB` (`MotorcycleDealerDb`) via `sqlserver-localdb-inspector` (**never** global `dbhub`, which targets Oracle).
- **Zero Root `node_modules` & Zero Secrets**: Keep `.agents/scripts/*.mjs` dependency-free on Node.js `v22` built-ins; never commit credentials, `.env`, or build outputs.

## 4. 7-Stage Quality Loop (`Phases 3–10`) & Mandatory Post-Task Sync Gate
- **7-Stage Pipeline (`5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T`)**: Every coding phase (`Phases 3–10`) MUST pass `15` reviews + `6` batteries via `.agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round <1|3>` (`0` warnings, `0` flakes, `check_phase_gate` `compliant: true`).
- **Mandatory Post-Task Sync Gate**: Every completed phase/feature/fix MUST update affected `docs/`, regenerate & validate `.docx`, sync `TASKS.md`/`PROGRESS.md`/`README.md`/`baseline-spec-snapshot.json` + `.agents/`, map `100%` of files into `.agents/scripts/git-6month-schedule-plan.json`, enforce all `6` `AGENTS.md` byte caps (`mobile/AGENTS.md < 8 KB`), and pass `sync-progress.mjs --check`, `verify-mobile-maui.mjs`, and `verify-post-task-sync.mjs`.
