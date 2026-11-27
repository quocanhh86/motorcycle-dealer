---
name: docx-spec-generator
description: >-
  Use this skill when updating, regenerating, styling, or validating the executive Word specification
  `docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx` via `.agents/scripts/build-spec-docx.mjs`,
  OpenXML schema validation, and post-task synchronization ("Xuất lại file đặc tả Word .docx",
  "Đồng bộ tài liệu báo cáo Word", "Kiểm tra định dạng file .docx", "Regenerate Word specification").
---

# Executive Word Specification (`.docx`) Generator Runbook (`docx-spec-generator`)

Complete engineering runbook for maintaining, regenerating, and validating [docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx](../../../docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx) using [build-spec-docx.mjs](../../scripts/build-spec-docx.mjs) (`T-109`, `T-208`, `T-707`, `T-808`, `T-908`, and the **Mandatory Post-Task Synchronization Gate**).

---

## 1. Zero-Pollution Workspace Rule (`NODE_PATH` Resolution)

> [!IMPORTANT]
> **Never Run `npm install` in the Repository Root**: The workspace root (`c:\Users\HELLO\Downloads\bike_car_management`) MUST remain completely free of `package.json`, `package-lock.json`, and `node_modules/`.

`build-spec-docx.mjs` uses Node ESM `createRequire` with `NODE_PATH` fallback resolution so `docx` (`docx-js`) loads from external scratch or global npm directories without polluting git status:

```powershell
# 1. Dry-run check (verifies docx module resolution & table column math without writing files)
$env:NODE_PATH = "C:\Users\HELLO\.gemini\antigravity\brain\dfa8ffa1-46c0-461f-8f59-3ffe9853361f\scratch\docx_builder\node_modules"
node .agents/scripts/build-spec-docx.mjs --dry-run

# 2. Full regeneration of docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx
$env:NODE_PATH = "C:\Users\HELLO\.gemini\antigravity\brain\dfa8ffa1-46c0-461f-8f59-3ffe9853361f\scratch\docx_builder\node_modules"
node .agents/scripts/build-spec-docx.mjs
```

*(If the scratch `node_modules` folder does not exist in a fresh session, install `docx` inside `<appDataDir>\brain\<conversation-id>\scratch\docx_builder` first and point `$env:NODE_PATH` to its `node_modules` path.)*

---

## 2. Pre-Generation SSOT Synchronization Checklist (`v1.2` Parity)

Every completed phase, feature, or fix requires updating [build-spec-docx.mjs](../../scripts/build-spec-docx.mjs) and regenerating the `.docx` artifact with 100% parity across all Single Source of Truth documents:

1. **Metadata & Roadmap Counts**: Match [README.md](../../../README.md), [TASKS.md](../../../TASKS.md), and [PROGRESS.md](../../../PROGRESS.md) (`80` total WBS tasks `T-101`..`T-908` across `9` phases, exact completed/pending counts, Timeline `02/2026 - 08/2026`, Solo Full-Stack Developer, `https://github.com/quocanhh86/motorcycle-dealer`).
2. **Product, UX & Domain Rules**: Match [docs/PRD.md](../../../docs/PRD.md) (`FR-01`..`FR-09`), [docs/DESIGN.md](../../../docs/DESIGN.md), `docs/screens/*.md`, `docs/basic-design/BD_00_*.md`, and `docs/detail-design/DD_01..04_*.md` (`48` granular permissions: `Customer=20`, `DealerStaff=22`, `Admin=32`; `/api/v1/*` endpoints including test-drives, spare-parts, printable receipts, CSAT reviews & VIN warranty claims; `/hubs/notifications` `10` SignalR events; `30` navigable UI-Router routes across `24` screens `SCR-PUB-01`..`SCR-ADM-08`; two-step floor loyalty formula `Floor(Floor(NetPaidAmount / 10000m) * TierMultiplier)` + `+25` `AvailablePoints` CSAT review bonus across `Bronze`/`Silver`/`Gold`/`Platinum`).
3. **Architecture & `.agents/` Automation Suite**: Match [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) (`ADR-001`..`ADR-006`, `8` `.agents/skills/*/SKILL.md` playbooks, lifecycle hooks, `verify-post-task-sync.mjs`, `183`-day `git-6month-schedule-plan.json` shipper, and the **9-Server MCP Ecosystem**).
4. **Database Data Dictionary**: Match [docs/DATABASE_DESIGN.md](../../../docs/DATABASE_DESIGN.md) (exact **`27` normalized 3NF tables** in `MotorcycleDealerDb`, `8` `RowVersion` tables, `8` `IDealerScoped` entities, `4` immutable financial/audit tables, and B-tree indexes).

---

## 3. Mandatory `docx-js` Formatting & OpenXML Guardrails

| Element | Mandatory `docx-js` Rule | Prohibited Anti-Pattern |
| :--- | :--- | :--- |
| **Page Size & Margins** | Explicit US Letter (`width: 12240`, `height: 15840` DXA) with `1"` (`1440` DXA) margins -> **`CONTENT_WIDTH = 9360` DXA** | Relying on implicit `docx-js` defaults or mismatching landscape/portrait dimensions |
| **Dual-Width Tables** | `new Table({ width: { size: 9360, type: WidthType.DXA }, columnWidths: [...] })` where `sum(columnWidths) === 9360`, explicit `width` + `margins` on every `TableCell`, and `tableHeader: true` on header rows | `WidthType.PERCENTAGE` (breaks in Google Docs / LibreOffice) or `columnWidths` not summing to `9360` |
| **Cell Shading** | Always `shading: { fill: "E2E8F0", type: ShadingType.CLEAR }` (`#E2E8F0` header, `#F8FAFC` zebra rows, `#EFF6FF` callout boxes) | `ShadingType.SOLID` (renders solid black cells in Microsoft Word!) |
| **Headings & TOC** | Custom `Heading1`, `Heading2`, `Heading3` styles with explicit `outlineLevel: 0`, `1`, `2` + `new TableOfContents(...)` | Missing `outlineLevel` (causes empty Table of Contents) |
| **Bullet & Numbered Lists** | `numbering: { reference: "bullets", level: 0 }` backed by `LevelFormat.BULLET` in `numbering.config` | Typing literal `"• "` or `"- "` inside plain `Paragraph` runs |
| **Text Runs** | Sanitize strings via `cleanText()`; separate lines into separate `Paragraph` objects | Embedding `\n` or `\r\n` inside `new TextRun({ text })` |

---

## 4. Build, OpenXML Validation & Post-Task Sync Procedure

Execute all steps in sequence whenever regenerating the specification:

```powershell
# Step 1 — Regenerate Master .docx
$env:NODE_PATH = "C:\Users\HELLO\.gemini\antigravity\brain\dfa8ffa1-46c0-461f-8f59-3ffe9853361f\scratch\docx_builder\node_modules"
node .agents/scripts/build-spec-docx.mjs

# Step 2 — Run Official OpenXML Schema & Structural Validation (must exit 0: All validations PASSED!)
python "C:\Users\HELLO\.gemini\config\plugins\developer-power-skills\skills\docx\scripts\office\validate.py" "c:\Users\HELLO\Downloads\bike_car_management\docs\Motorcycle_Dealer_Loyalty_Platform_Specification.docx"

# Step 3 — Verify File Size (> 50,000 bytes), Zero Root Pollution & Post-Task Sync Gate
(Get-Item "docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx").Length -gt 50000
node .agents/scripts/verify-post-task-sync.mjs
```

---

## 5. MCP-Accelerated Workflows

1. **Spec Data Extraction (`motorcycle-dealer-guardrails` MCP)**: Call `get_wbs_tasks` (`80` tasks across `Phases 1–9`), `lookup_rbac_permission` (`48` permissions), `lookup_db_table_spec` (`27` tables), and `check_phase_gate`.
2. **Live Database Schema Cross-Check (`sqlserver-localdb-inspector` MCP)**: Call `list_sqlserver_tables` (`27` tables) and `inspect_sqlserver_schema` on `(localdb)\MSSQLLocalDB` (`MotorcycleDealerDb`) to verify 1-for-1 Data Dictionary parity.
3. **Executive Diagram & Stakeholder Publishing (`figma_figma` & `notion_notion` MCP)**: Render companion visuals and sync milestone release notes when requested.
