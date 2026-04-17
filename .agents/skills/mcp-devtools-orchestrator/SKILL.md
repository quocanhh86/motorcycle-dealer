---
name: mcp-devtools-orchestrator
description: >-
  Use this skill when inspecting, orchestrating, or troubleshooting Model Context Protocol (MCP)
  workflows across SQL Server LocalDB (27 tables), 48-permission REST APIs, 10-event SignalR browser
  E2E, design, or repository tools ("dùng mcp", "kiểm tra bằng mcp", "test e2e playwright mcp",
  "đồng bộ postman / github / notion", "soi sql localdb mcp", "run MCP audit", "troubleshoot MCP server").
---

# Master 9-Server MCP Orchestrator Playbook (`mcp-devtools-orchestrator`)

Authoritative runbook for combining the **2 Workspace Plugin Stdio MCP Servers** and **7 Global MCP Servers** across **Phases 2–9+** of the **Motorcycle Dealer & Loyalty Management Platform** (`https://github.com/quocanhh86/motorcycle-dealer`).

> [!WARNING]
> **Critical Database MCP Routing Rule (`sqlserver-localdb-inspector` vs `dbhub`)**:
> The global `dbhub` MCP server in this environment is bound to an **Oracle** instance from a separate project. **NEVER** invoke `dbhub` for `MotorcycleDealerDb` queries! Always use our workspace **`sqlserver-localdb-inspector`** MCP server (`(localdb)\MSSQLLocalDB` / SQL Server) for all database introspection and read-only SQL verification.

---

## 1. Complete 9-Server MCP Architecture

### 1.1 Workspace Plugin Stdio MCP Servers (2 Servers)

Registered in [mcp_config.json](../../plugins/motorcycle-dealer-suite/mcp_config.json) and implemented as zero-dependency Node.js JSON-RPC 2.0 Stdio servers under `.agents/scripts/`:

| MCP Server Name | Script Path | Exposed Tools (`9` Total) | Primary Engineering Purpose |
| :--- | :--- | :--- | :--- |
| **`motorcycle-dealer-guardrails`** | [mcp-dealer-guardrails-server.mjs](../../scripts/mcp-dealer-guardrails-server.mjs) | 1. `check_phase_gate`<br>2. `lookup_rbac_permission`<br>3. `lookup_db_table_spec`<br>4. `calculate_loyalty_preview`<br>5. `get_wbs_tasks` | Enforces `9`-phase file boundaries (`Phases 1..9`), queries the `48` `Permissions.<Module>.<Action>` RBAC matrix & `8` `IDealerScoped` rules, pulls `27`-table specs from `docs/DATABASE_DESIGN.md`, computes deterministic two-step floor loyalty points/tiers, and queries `80` WBS tasks (`T-101`..`T-908`). |
| **`sqlserver-localdb-inspector`** | [mcp-sqlserver-localdb-server.mjs](../../scripts/mcp-sqlserver-localdb-server.mjs) | 1. `sqlserver_status`<br>2. `list_sqlserver_tables`<br>3. `inspect_sqlserver_schema`<br>4. `query_readonly_sql` | Inspects `(localdb)\MSSQLLocalDB` readiness, lists the `27` `MotorcycleDealerDb` tables & row counts, inspects SQL types (`DECIMAL(18,2)`, `8` `ROWVERSION` columns) & B-tree indexes, and runs sanitized `TOP 200` `SELECT`/`WITH` queries (blocking all DML/DDL & multi-statement `;` injection). |

#### Canonical Workspace MCP Tool Call Examples

```json
// 1. Verify active phase boundary (Phases 1..8) before scaffolding new folders
{
  "ServerName": "motorcycle-dealer-guardrails",
  "ToolName": "check_phase_gate",
  "Arguments": { "phase": 8 }
}

// 2. Oracle preview for Loyalty accrual (455,000 VND at Gold 1.50x -> 67 pts)
{
  "ServerName": "motorcycle-dealer-guardrails",
  "ToolName": "calculate_loyalty_preview",
  "Arguments": {
    "netPaidAmountVnd": 455000,
    "tierMultiplier": 1.5,
    "currentLifetimeQualifyingPoints": 2100
  }
}

// 3. Lookup DealerStaff RBAC permissions (18 of 40) & 6-table IDealerScoped constraints
{
  "ServerName": "motorcycle-dealer-guardrails",
  "ToolName": "lookup_rbac_permission",
  "Arguments": {
    "role": "DealerStaff",
    "module": "TestDrives"
  }
}

// 4. Reconcile immutable PointLedgerEntries & ServiceInvoiceLineItems in LocalDB
{
  "ServerName": "sqlserver-localdb-inspector",
  "ToolName": "query_readonly_sql",
  "Arguments": {
    "sql": "SELECT la.Id, la.AvailablePoints, ISNULL(SUM(ple.PointsDelta), 0) AS LedgerSum FROM LoyaltyAccounts la LEFT JOIN PointLedgerEntries ple ON ple.LoyaltyAccountId = la.Id GROUP BY la.Id, la.AvailablePoints"
  }
}
```

---

### 1.2 Installed Global MCP Servers (`7` Servers)

| Global MCP Server | Core Tools Used in This Project | Domain / Phase Scope |
| :--- | :--- | :--- |
| **`context7_context7`** | `resolve-library-id` -> `query-docs` | Authoritative up-to-date docs for `.NET 10`, `EF Core 10`, `ASP.NET Core SignalR`, `FluentValidation`, `AngularJS 1.8.3`, `@uirouter/angularjs`, and `Chart.js v4` (`Phases 3–8+`). |
| **`playwright`** | `browser_navigate`, `browser_tabs`, `browser_snapshot`, `browser_fill_form`, `browser_click`, `browser_wait_for`, `browser_take_screenshot`, `browser_console_messages`, `browser_network_requests` | Multi-tab `Customer` + `DealerStaff` SignalR zero-refresh E2E proof (`BookingCreated`, `TestDriveCreated`), `27`-route RBAC guard tests, printable receipts, and responsive screenshots (`Phases 4–8+`). |
| **`chrome-devtools-plugin_chrome-devtools`** | `navigate_page`, `list_network_requests`, `get_network_request`, `list_console_messages`, `evaluate_script`, `take_heapsnapshot`, `lighthouse_audit` | WebSocket `/hubs/notifications` frame inspection (`7` events), AngularJS `$digest` & `Chart.js` `destroy()` memory-leak audits (`RISK-04`), `RFC 7807` payload inspection, and Lighthouse `>= 90` audits (`Phases 6–8+`). |
| **`postman_postman`** | `createWorkspace`, `createCollection`, `createCollectionRequest`, `createEnvironment`, `syncCollectionWithSpec`, `runMonitor` | Synchronizes `/api/v1/*` OpenAPI specs into automated Postman collections with `Customer`, `DealerStaff`, and `Admin` environments and `40`-permission `ProblemDetails` contract monitors (`Phases 4–8+`). |
| **`github`** | `create_repository`, `create_branch`, `issue_write`, `create_pull_request`, `pull_request_review_write` | Manages issues, milestone branches, traceable PRs (`FR-*` / `ADR-*` / `T-*`), and `v1.0.0` / `v1.1.0` releases on `https://github.com/quocanhh86/motorcycle-dealer` (`Phases 3–8+`). |
| **`figma_figma`** | `generate_diagram`, `get_design_context`, `get_screenshot` | Generates C4/sequence architecture diagrams and extracts design tokens/frames across all `21` screens (`SCR-PUB-01`..`SCR-ADM-07`). |
| **`notion_notion`** | `notion-search`, `notion-create-pages`, `notion-update-page` | Publishes sprint retrospectives, ADR summaries, and phase-gate sign-off notes to the developer's engineering workspace. |

---

## 2. Concrete Multi-Server MCP Orchestration Recipes

### Recipe 1 — Spec-to-API-to-Postman Contract Sync (`40` Permissions)
1. **`motorcycle-dealer-guardrails`**: Run `check_phase_gate`, `lookup_rbac_permission`, and `lookup_db_table_spec`.
2. **`context7_context7`**: Query `.NET 10` `IExceptionHandler` / `ProblemDetails` or `EF Core 10` configuration syntax.
3. **Implement & Test**: Build the Clean Architecture handler/controller and pass `dotnet test`.
4. **`postman_postman`**: Sync `/api/v1/*` OpenAPI routes and run `40`-permission contract monitors.

### Recipe 2 — Dual-Tab SignalR Instant Booking & Test-Drive Alert Proof (`FR-06`, `7` Events)
1. **`playwright`**: Open Tab 1 (`DealerStaff` at `DLR-HCM-01`) on `/#!/staff/workbench` or `/#!/staff/test-drives` and Tab 0 (`Customer`). Submit `POST /api/v1/bookings` or `POST /api/v1/test-drives` in Tab 0, switch back to Tab 1 **without reloading**, and verify instant `BookingCreated` / `TestDriveCreated` card rendering (`dealer-{dealerId}`).
2. **`chrome-devtools-plugin_chrome-devtools`**: Inspect `101 Switching Protocols` frames on `/hubs/notifications?access_token=...` and verify zero `$onDestroy` listener leaks.

### Recipe 3 — 25-Table Loyalty, Itemized Invoice & `RowVersion` DB Audit (`FR-04`, `FR-05`)
1. **`motorcycle-dealer-guardrails`**: Call `calculate_loyalty_preview` for oracle `pointsEarned` and tier progression.
2. **`sqlserver-localdb-inspector`**: Call `list_sqlserver_tables` (`25` tables) and `query_readonly_sql` to assert `BookedCount <= MaxCapacity`, `GrossAmount == SUM(LineTotalVnd)` on `ServiceInvoiceLineItems`, and `SUM(PointLedgerEntries.PointsDelta) == LoyaltyAccounts.AvailablePoints`.

### Recipe 4 — Post-Task Sync & Release Automation (`quocanhh86/motorcycle-dealer`)
1. **`motorcycle-dealer-guardrails`**: Call `check_phase_gate` and `get_wbs_tasks` (`status: "pending"`).
2. **Local Verification & Mandatory Post-Task Sync Gate**: Run `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/verify-dealer-platform.ps1 -Phase 8`, `node .agents/scripts/sync-progress.mjs --check`, and `node .agents/scripts/verify-post-task-sync.mjs`.
3. **`github` / `notion_notion`**: Publish traceable PRs/releases once scheduled slices ship cleanly.

---

## 3. MCP Troubleshooting & JSON-RPC Stdio Diagnostics

```powershell
# Smoke-test motorcycle-dealer-guardrails
'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}', '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | node .agents/scripts/mcp-dealer-guardrails-server.mjs

# Smoke-test sqlserver-localdb-inspector
'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}', '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"sqlserver_status","arguments":{}}}' | node .agents/scripts/mcp-sqlserver-localdb-server.mjs
```
If `sqlserver_status` reports `MSSQLLocalDB` stopped, run [ensure-sqlserver-db.ps1](../../scripts/ensure-sqlserver-db.ps1). For OAuth plugin errors (`github`, `postman_postman`, `figma_figma`, `notion_notion`), call `list_plugin_accounts` first.
