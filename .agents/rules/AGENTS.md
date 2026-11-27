# `.agents/` Operational Suite & Master MCP Routing Guide

Operational rulebook for the `.agents/` suite and **9-Server MCP Ecosystem** of the **Motorcycle Dealer & Loyalty Management Platform** (`quocanhh86/motorcycle-dealer`). Complements [AGENTS.md](../../AGENTS.md) and [mobile/AGENTS.md](../../mobile/AGENTS.md).

---

## 1. `.agents/` Layout & Single-Root Skill Rule

```text
.agents/
├── plugins.json                              # Registers .agents/plugins
├── skills.json                               # Registers .agents/skills (single skill root!)
├── rules/AGENTS.md                           # Operational layout, 9-MCP matrix, EN/VI router & sync gate
├── plugins/motorcycle-dealer-suite/
│   ├── plugin.json / mcp_config.json         # Native manifest & 2 Node v22 Stdio MCP servers
│   ├── hooks.json                            # SessionStart (Phase 1..10 banner) + PreToolUse safety guard
│   ├── hooks/{session-start,safety-guard}.mjs
│   └── rules/AGENTS.md                       # Plugin safety, phase-gate, sync & LocalDB MCP rules
├── scripts/
│   ├── baseline-spec-snapshot.json           # Canonical 10-phase (88 tasks), 27-table, 48-RBAC, 10-event JSON
│   ├── mcp-dealer-guardrails-server.mjs      # Stdio MCP #1 (5 spec, 48-RBAC, 27-table, WBS & loyalty tools)
│   ├── mcp-sqlserver-localdb-server.mjs      # Stdio MCP #2 (4 read-only LocalDB sqlcmd tools)
│   ├── audit-dealer-guardrails.{ps1,sh}      # Clean Arch, Phase 1..10, 4-table immutability & secret audit
│   ├── ensure-sqlserver-db.{ps1,sh}          # LocalDB / Docker SQL Server 2022 readiness & EF migrator
│   ├── ensure-maui-android.ps1               # SDK/JDK/WHPX Pixel_8_API_35 AVD & adb reverse tcp:5080
│   ├── verify-mobile-maui.mjs                # 7-check MAUI static guardrails (x:DataType, SourceGen, 48-RBAC)
│   ├── smoke-mobile-android.ps1              # APK build, adb install, pidof, uiautomator & PNG screenshot
│   ├── seed-demo-scenarios.mjs / capture-live-evidence.mjs / run-and-capture-dealer-platform.ps1
│   ├── run-phase-quality-loop.ps1            # 3-battery runner for 7-stage loop (Phases 3..10)
│   ├── smoke-fullstack.{ps1,sh}              # REST /api/v1, RFC 7807, 48-RBAC & SignalR smoke suite
│   ├── sync-progress.mjs                     # TASKS.md <-> PROGRESS.md <-> snapshot (88 tasks) reconciler
│   ├── verify-post-task-sync.mjs             # 100% docs, 6 rule caps, 3-way 48-RBAC & 183d git plan parity
│   ├── git-6month-schedule-plan.json         # 183-day daily GitHub slice schedule (100% workspace mapped)
│   ├── daily-git-shipper.{mjs,ps1}           # Lock-safe daily slice committer & origin/main publisher
│   ├── build-spec-docx.mjs                   # Master Word (.docx) generator (zero repo node_modules)
│   └── verify-dealer-platform.{ps1,sh}       # Master gate across JSON, skills, MCP, sync, .NET, SPA & MAUI
└── skills/                                   # All 9 domain, stack, mobile, QA, DOCX & MCP skills live ONLY here
    ├── dotnet-clean-api/SKILL.md             ├── sqlserver-efcore-dba/SKILL.md
    ├── angularjs-enterprise-spa/SKILL.md     ├── dotnet-maui-mobile/SKILL.md
    ├── loyalty-booking-domain/SKILL.md       ├── signalr-realtime-hub/SKILL.md
    ├── qa-verification-suite/SKILL.md        ├── docx-spec-generator/SKILL.md
    └── mcp-devtools-orchestrator/SKILL.md
```

- **No Duplicate Skill Folder**: Keep skills **only** in `.agents/skills/` (never create `.agents/plugins/motorcycle-dealer-suite/skills/`).

---

## 2. Master MCP Tooling & Routing Matrix (`2` Workspace + `7` Global)

> [!IMPORTANT]
> **SQL Server vs. Global `dbhub` Isolation**: Global `dbhub` connects to an external **Oracle** DB. **NEVER** query `dbhub` for `MotorcycleDealerDb`! Always use **`sqlserver-localdb-inspector`** for `(localdb)\MSSQLLocalDB`.

### 2.1 Workspace Plugin Stdio MCP Servers (`.agents/plugins/motorcycle-dealer-suite/mcp_config.json`)

| Server ID | Exposed Tools | When & How Agents Must Use It |
| :--- | :--- | :--- |
| **`motorcycle-dealer-guardrails`** | `check_phase_gate`, `lookup_rbac_permission`, `lookup_db_table_spec`, `calculate_loyalty_preview`, `get_wbs_tasks` | Run `check_phase_gate` (`Phases 1..10`); call `lookup_rbac_permission` (`48` policies & `8` `IDealerScoped` rules); call `lookup_db_table_spec` (`27` 3NF tables); call `calculate_loyalty_preview`; call `get_wbs_tasks` (`T-101`..`T-1008`, `88` tasks). |
| **`sqlserver-localdb-inspector`** | `sqlserver_status`, `list_sqlserver_tables`, `inspect_sqlserver_schema`, `query_readonly_sql` | Check `MSSQLLocalDB` & `MotorcycleDealerDb` (`27` tables) readiness, inspect PK/FK/index/`RowVersion` metadata, and run safe read-only `SELECT`/`WITH` (`TOP 200`) queries. |

### 2.2 Installed Global MCP Servers

| Global MCP Server | Key Tools | Mandatory Engineering Workflow |
| :--- | :--- | :--- |
| **`context7_context7`** | `resolve-library-id` -> `query-docs` | Official docs for `.NET 10`, `EF Core 10`, `.NET MAUI`, `SignalR`, `AngularJS 1.8`, `@uirouter`, `Chart.js 4`. |
| **`playwright`** | `browser_navigate`, `browser_tabs`, `browser_snapshot`, `browser_click`, `browser_take_screenshot` | Multi-tab `Customer` + `DealerStaff` SignalR zero-refresh E2E proof, `30`-route `/403` guards, and receipts. |
| **`chrome-devtools-plugin_chrome-devtools`** | `navigate_page`, `list_network_requests`, `list_console_messages`, `evaluate_script`, `take_heapsnapshot` | Inspect `/hubs/notifications` WS frames (`10` events), `RFC 7807`, `$digest`/`Chart.js` leaks (`RISK-04`), Lighthouse. |
| **`postman_postman`** | `createCollection`, `createEnvironment`, `syncCollectionWithSpec`, `runMonitor` | Sync `/api/v1/*` OpenAPI specs into Postman collections and run `48`-permission contract monitors. |
| **`github`** | `create_branch`, `issue_write`, `create_pull_request`, `pull_request_review_write` | Manage milestone branches, issues, PRs, and `v1.0.0`..`v1.3.0` releases on `quocanhh86/motorcycle-dealer`. |
| **`figma_figma`** | `generate_diagram`, `get_design_context`, `get_screenshot` | Generate architecture diagrams and wireframe context across `24` web + `16` mobile screens. |
| **`notion_notion`** | `notion-search`, `notion-create-pages`, `notion-update-page` | Sync sprint notes, ADR logs, and engineering handbook pages when requested. |

---

## 3. Bilingual (EN / VI) Quick-Trigger Cheatsheet (`9` Skills)

| Skill (`.agents/skills/*/SKILL.md`) | English Triggers | Vietnamese Triggers (Tiếng Việt) | Paired MCP Servers |
| :--- | :--- | :--- | :--- |
| **`dotnet-clean-api`** | "Add `/api/v1` endpoint", "CQRS/FluentValidation", "48-RBAC / JWT / RFC 7807" | "Tạo API `/api/v1`", "Viết handler/validator", "Phân quyền 48 RBAC / JWT" | `motorcycle-dealer-guardrails`, `context7_context7`, `postman_postman` |
| **`sqlserver-efcore-dba`** | "EF Core 10 27-table migration", "8 `IDealerScoped` / 8 `RowVersion`", "Seed DB" | "Cấu hình 27 bảng EF Core", "Tạo migration / index", "Kiểm tra LocalDB" | `sqlserver-localdb-inspector`, `motorcycle-dealer-guardrails`, `context7_context7` |
| **`angularjs-enterprise-spa`** | "Build AngularJS component", "30 UI-Router guards", "`Chart.js 4` / receipt modal" | "Tạo giao diện AngularJS", "Chặn route theo role", "Biểu đồ `Chart.js` / biên lai" | `context7_context7`, `figma_figma`, `chrome-devtools-plugin_chrome-devtools`, `playwright` |
| **`dotnet-maui-mobile`** | "Build `.NET 10 MAUI` Android app", "Camera2 VIN/QR", "`SQLCipher` cache", "`Pixel_8_API_35`" | "Tạo app Android `.NET 10 MAUI`", "Quét mã VIN/QR", "Cache `SQLCipher`", "Giả lập ADB" | `motorcycle-dealer-guardrails`, `context7_context7`, `figma_figma` |
| **`loyalty-booking-domain`** | "VIN sale / warranty / CSAT +25", "Line-item invoice", "4-tier loyalty / VIN worker" | "Bán xe VIN / bảo hành / đánh giá +25", "Hóa đơn phụ tùng", "Tính điểm / nhắc VIN" | `motorcycle-dealer-guardrails`, `sqlserver-localdb-inspector` |
| **`signalr-realtime-hub`** | "`NotificationHub` 10 events", "Instant staff push", "`@microsoft/signalr`" | "Thông báo đặt lịch / bảo hành realtime", "SignalR `dealer-{dealerId}` không F5" | `playwright`, `chrome-devtools-plugin_chrome-devtools`, `context7_context7` |
| **`qa-verification-suite`** | "Write xUnit concurrency/48-RBAC tests", "Verify `RISK-01`..`06`", "Post-task sync" | "Viết unit/integration test", "Kiểm thử trùng lịch & IDOR", "Kiểm tra đồng bộ" | `playwright`, `postman_postman`, `sqlserver-localdb-inspector`, `motorcycle-dealer-guardrails` |
| **`docx-spec-generator`** | "Regenerate `.docx` specification", "Sync Word report", "Validate `.docx` OpenXML" | "Xuất lại file đặc tả Word `.docx`", "Đồng bộ báo cáo Word", "Kiểm tra `validate.py`" | `motorcycle-dealer-guardrails` |
| **`mcp-devtools-orchestrator`** | "Run multi-MCP audit", "E2E Playwright/DevTools", "Sync Postman/GitHub/Notion" | "Dùng MCP kiểm tra toàn diện", "Soi LocalDB / WS / Playwright", "Đồng bộ GitHub" | All `9` MCP servers (`2` workspace + `7` global) |

---

## 4. Mandatory 7-Stage Quality Loop (`Phases 3–10`) & Post-Task Sync Gate

Every coding phase (**Phases 3–10**) MUST pass **`5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test`** (`15` reviews + `6` test batteries) before marking any `T-*` `[x]`:

1. **Stage 1 (`5x` Review `R1.1`..`R1.5`)**: Audit `ADR-001` purity, SSOT/`48`-RBAC/`30`-route/`16`-mobile-view parity, `8`-table `IDealerScoped`/CORS/`RFC 7807`/Android hardening, `8`-table `RowVersion`/`4` immutable tables/`SQLCipher` online-only/SPA memory, and `--warnaserror`/Docker/all `6` `AGENTS.md` caps (`root`, `.agents/rules`, `plugin`, `backend`, `frontend`, `mobile`).
2. **Stage 2 (`3x` Test `T1.1`..`T1.3`)**: Pass Round 1 (`dotnet build --warnaserror` + backend/mobile xUnit, SPA `npm test` + live HTTP/`10`-event SignalR/CDP/Android APK smoke, `verify-dealer-platform.ps1` + `verify-mobile-maui.mjs` + `verify-post-task-sync.mjs` + `check_phase_gate` + determinism).
3. **Stages 3–5 (`Fix -> 5x Review -> Refactor`)**: Fix defects, re-audit `5x` (`R2.1`..`R2.5`), refactor cleanly + expand `[Theory]` tests.
4. **Stages 6–7 (`5x Review -> 3x Test`)**: Confirm zero warnings/regressions (`R3.1`..`R3.5`), pass Round 3 (`0` flaky), log `15R + 6T` in `PROGRESS.md`.
5. **Mandatory Post-Task Sync Gate**: Update affected `docs/`, regenerate & validate `.docx`, sync `TASKS.md`/`PROGRESS.md`/`README.md`/`baseline-spec-snapshot.json` + `.agents/`, map `100%` of files into `git-6month-schedule-plan.json`, and pass `verify-post-task-sync.mjs`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round <1|3>
node .agents/scripts/verify-mobile-maui.mjs
node .agents/scripts/verify-post-task-sync.mjs
```
