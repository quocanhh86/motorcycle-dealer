#!/usr/bin/env node
/**
 * build-spec-docx.mjs
 * Compiles docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx using docx-js resolved via
 * ESM createRequire + NODE_PATH / scratch / global npm fallbacks (ensuring ZERO node_modules or
 * package.json ever enters the repository root).
 *
 * Usage:
 *   node .agents/scripts/build-spec-docx.mjs --dry-run
 *   NODE_PATH="..." node .agents/scripts/build-spec-docx.mjs [outputPath]
 */

import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");
const require = createRequire(import.meta.url);

function loadDocxLibrary() {
  try {
    return require("docx");
  } catch {
    // Continue through explicit search locations so zero local node_modules is needed
  }

  const candidateNodeModuleDirs = [];
  if (process.env.NODE_PATH) {
    for (const seg of process.env.NODE_PATH.split(path.delimiter)) {
      if (seg && seg.trim()) candidateNodeModuleDirs.push(seg.trim());
    }
  }

  candidateNodeModuleDirs.push(
    "C:\\Users\\HELLO\\.gemini\\antigravity\\brain\\dfa8ffa1-46c0-461f-8f59-3ffe9853361f\\scratch\\docx_builder\\node_modules"
  );

  if (process.env.APPDATA) {
    candidateNodeModuleDirs.push(path.join(process.env.APPDATA, "npm", "node_modules"));
  }

  try {
    const globalRoot = execFileSync("npm", ["root", "-g"], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
      shell: process.platform === "win32",
    }).trim();
    if (globalRoot) candidateNodeModuleDirs.push(globalRoot);
  } catch {
    // Ignore if npm is unavailable
  }

  for (const nmDir of candidateNodeModuleDirs) {
    const pkgPath = path.join(nmDir, "docx");
    if (fs.existsSync(pkgPath)) {
      return require(pkgPath);
    }
  }

  throw new Error(
    "Unable to resolve 'docx' package via NODE_PATH, scratch node_modules, or global npm root. Install 'docx' globally or pass NODE_PATH."
  );
}

const rawArgs = process.argv.slice(2);
let dryRun = false;
let customOutPath = "";

for (let i = 0; i < rawArgs.length; i++) {
  const arg = rawArgs[i];
  if (arg === "--dry-run" || arg === "-DryRun") {
    dryRun = true;
  } else if (arg === "--out" || arg === "-Out") {
    customOutPath = rawArgs[i + 1] || "";
    i++;
  } else if (!arg.startsWith("-") && !customOutPath) {
    customOutPath = arg;
  }
}

const outPath = customOutPath
  ? path.resolve(customOutPath)
  : path.join(repoRoot, "docs", "Motorcycle_Dealer_Loyalty_Platform_Specification.docx");

const {
  Document,
  Packer,
  Paragraph,
  TextRun,
  Table,
  TableRow,
  TableCell,
  Header,
  Footer,
  AlignmentType,
  LevelFormat,
  TableOfContents,
  HeadingLevel,
  BorderStyle,
  WidthType,
  ShadingType,
  VerticalAlign,
  PageNumber,
  PageBreak,
} = loadDocxLibrary();

const CONTENT_WIDTH = 9360; // 12240 (US Letter) - 2 * 1440 (1" margins)
const COLOR_NAVY = "0F172A";
const COLOR_ACCENT = "B91C1C";
const COLOR_SUBHEAD = "1E3A8A";
const COLOR_HEADER_FILL = "E2E8F0";
const COLOR_ZEBRA_FILL = "F8FAFC";
const COLOR_CALLOUT_FILL = "EFF6FF";
const COLOR_BORDER = "CBD5E1";
const COLOR_MUTED = "475569";

const thinBorder = { style: BorderStyle.SINGLE, size: 1, color: COLOR_BORDER };
const cellBorders = {
  top: thinBorder,
  bottom: thinBorder,
  left: thinBorder,
  right: thinBorder,
};
const cellMargins = { top: 80, bottom: 80, left: 120, right: 120 };

function cleanText(str) {
  return String(str).replace(/\r?\n/g, " ");
}

function tr(text, opts = {}) {
  return new TextRun({
    text: cleanText(text),
    font: opts.mono ? "Consolas" : "Arial",
    size: opts.size || (opts.mono ? 19 : 21),
    bold: Boolean(opts.bold),
    italics: Boolean(opts.italics),
    color: opts.color || "1E293B",
  });
}

function p(text, opts = {}) {
  const runs = Array.isArray(text)
    ? text.map((t) =>
        typeof t === "string"
          ? new TextRun({ text: cleanText(t), font: "Arial", size: opts.size || 21, color: opts.color || "1E293B" })
          : t
      )
    : [
        new TextRun({
          text: cleanText(text),
          font: "Arial",
          size: opts.size || 21,
          bold: Boolean(opts.bold),
          italics: Boolean(opts.italics),
          color: opts.color || "1E293B",
        }),
      ];

  return new Paragraph({
    alignment: opts.alignment || AlignmentType.LEFT,
    spacing: opts.spacing || { before: 60, after: 120, line: 276 },
    children: runs,
  });
}

function h1(text, pageBreakBefore = true) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    pageBreakBefore,
    children: [new TextRun({ text: cleanText(text) })],
  });
}

function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    children: [new TextRun({ text: cleanText(text) })],
  });
}

function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    children: [new TextRun({ text: cleanText(text) })],
  });
}

function bullet(label, body, level = 0) {
  const children = label
    ? [tr(label + ": ", { bold: true, color: COLOR_NAVY }), tr(body)]
    : [tr(body)];
  return new Paragraph({
    numbering: { reference: "bullets", level },
    spacing: { before: 40, after: 80, line: 264 },
    children,
  });
}

function numItem(reference, label, body) {
  const children = label
    ? [tr(label + ": ", { bold: true, color: COLOR_NAVY }), tr(body)]
    : [tr(body)];
  return new Paragraph({
    numbering: { reference, level: 0 },
    spacing: { before: 40, after: 80, line: 264 },
    children,
  });
}

function calloutBox(title, bodyText) {
  return new Table({
    width: { size: CONTENT_WIDTH, type: WidthType.DXA },
    columnWidths: [CONTENT_WIDTH],
    rows: [
      new TableRow({
        children: [
          new TableCell({
            width: { size: CONTENT_WIDTH, type: WidthType.DXA },
            borders: {
              top: thinBorder,
              bottom: thinBorder,
              right: thinBorder,
              left: { style: BorderStyle.SINGLE, size: 18, color: COLOR_SUBHEAD },
            },
            shading: { fill: COLOR_CALLOUT_FILL, type: ShadingType.CLEAR },
            margins: { top: 120, bottom: 120, left: 180, right: 160 },
            children: [
              p([tr(title + " \u2014 ", { bold: true, color: COLOR_SUBHEAD }), tr(bodyText)], {
                spacing: { before: 20, after: 20, line: 264 },
              }),
            ],
          }),
        ],
      }),
    ],
  });
}

function makeTable(headers, rows, colWidths) {
  const sum = colWidths.reduce((acc, w) => acc + w, 0);
  if (sum !== CONTENT_WIDTH) {
    throw new Error(`Column widths [${colWidths.join(", ")}] sum to ${sum}, expected ${CONTENT_WIDTH}`);
  }
  if (headers.length !== colWidths.length) {
    throw new Error(`Header count ${headers.length} != columnWidths length ${colWidths.length}`);
  }

  const headerRow = new TableRow({
    tableHeader: true,
    children: headers.map(
      (hdr, idx) =>
        new TableCell({
          width: { size: colWidths[idx], type: WidthType.DXA },
          borders: cellBorders,
          shading: { fill: COLOR_HEADER_FILL, type: ShadingType.CLEAR },
          margins: cellMargins,
          verticalAlign: VerticalAlign.CENTER,
          children: [
            new Paragraph({
              spacing: { before: 20, after: 20, line: 240 },
              children: [tr(hdr, { bold: true, size: 19, color: COLOR_NAVY })],
            }),
          ],
        })
    ),
  });

  const dataRows = rows.map((row, rIdx) => {
    if (row.length !== colWidths.length) {
      throw new Error(`Row ${rIdx} length ${row.length} != columnWidths length ${colWidths.length}`);
    }
    const fill = rIdx % 2 === 1 ? COLOR_ZEBRA_FILL : "FFFFFF";
    return new TableRow({
      children: row.map(
        (cellVal, cIdx) =>
          new TableCell({
            width: { size: colWidths[cIdx], type: WidthType.DXA },
            borders: cellBorders,
            shading: { fill, type: ShadingType.CLEAR },
            margins: cellMargins,
            verticalAlign: VerticalAlign.TOP,
            children: [
              new Paragraph({
                spacing: { before: 20, after: 20, line: 240 },
                children: [
                  tr(cellVal, {
                    size: 18,
                    bold: cIdx === 0,
                    color: cIdx === 0 ? COLOR_NAVY : "1E293B",
                  }),
                ],
              }),
            ],
          })
      ),
    });
  });

  return new Table({
    width: { size: CONTENT_WIDTH, type: WidthType.DXA },
    columnWidths: colWidths,
    rows: [headerRow, ...dataRows],
  });
}

function spacer() {
  return new Paragraph({ spacing: { before: 40, after: 80 }, children: [tr("")] });
}

// ============================================================================
// BUILD FULL SPECIFICATION CONTENT (PHASES 1-10 RATIFIED BASELINE v1.3.0)
// ============================================================================

const children = [
  // COVER PAGE
  new Paragraph({
    spacing: { before: 480, after: 120 },
    children: [
      tr("MASTER ARCHITECTURE & PRODUCT ENGINEERING SPECIFICATION", {
        bold: true,
        size: 22,
        color: COLOR_ACCENT,
      }),
    ],
  }),
  new Paragraph({
    spacing: { before: 60, after: 200 },
    children: [
      tr("Motorcycle Dealer & Loyalty Management Platform", {
        bold: true,
        size: 44,
        color: COLOR_NAVY,
      }),
    ],
  }),
  p(
    "Closed-Loop Multi-Dealer Showroom Sales & Test-Drive Scheduling, ISO 3779 VIN Garage Lifecycle, Concurrency-Safe Workshop Slot Scheduling, OEM Spare-Parts Catalog & Itemized Invoices, SHA-256 Printable Receipts, Post-Service 5-Star CSAT Reviews (+25 Bonus Loyalty Points), Factory VIN Warranty Claims (36m / 30,000 km), Automated VIN Maintenance Reminders, Immutable Spend-to-Points Loyalty & Tier Engine, Real-Time SignalR Notifications (10 Events), Admin Chart.js Analytics (9 Bindings & 17-Column CSV), Native C# .NET 10 MAUI Android & iOS Field App (net10.0-android;net10.0-ios, CommunityToolkit.Maui 13.0, FluentValidation, Polly v8 Http.Resilience, Humanizer, WeakReferenceMessenger, 16 Compiled Screens, 9-Table Encrypted SQLCipher WAL Cache & Camera2/AVFoundation VIN/QR), and Antigravity .agents/ + 9-Server MCP Engineering Suite.",
    { size: 22, color: COLOR_MUTED, spacing: { before: 40, after: 280, line: 300 } }
  ),
  makeTable(
    ["Document Attribute", "Canonical Engineering Baseline"],
    [
      ["Document Identifier", "SPEC-MDLMP-2026-MASTER (v1.3.3 Multi-Platform .NET 10 MAUI Android & iOS + CommunityToolkit.Maui 13.0 Sync Edition \u2014 All 10 Phases Delivered & Verified)"],
      ["Project Name", "Motorcycle Dealer & Loyalty Management Platform"],
      ["GitHub Repository", "https://github.com/quocanhh86/motorcycle-dealer"],
      ["Engineering Role & Team", "Full-Stack Developer \u2014 Team Size: 1 (Solo Engineering Project)"],
      ["Delivery Timeline", "02/2026 - 08/2026 (10-Phase Strictly Gated Roadmap \u2014 100% Complete)"],
      ["Front-End & Mobile Stack", "Decoupled AngularJS 1.8.3 SPA (30 Navigable Routes, Chart.js 4, @microsoft/signalr, Zero-Mock) + Native C# 13 .NET 10 MAUI Android & iOS Field App (net10.0-android API 26-35; net10.0-ios iOS 15+, CommunityToolkit.Maui 13.0, FluentValidation 11.11, Microsoft.Extensions.Http.Resilience Polly v8, Humanizer 2.14, WeakReferenceMessenger, 16 SourceGen x:DataType Screens, ZXing Camera2/AVFoundation VIN/QR, 9-Table Encrypted SQLCipher WAL)"],
      ["Back-End API Stack", "C#, ASP.NET Core Web API (.NET 10 / net10.0, 18 Controllers), Entity Framework Core 10, ASP.NET Core SignalR (10 Events)"],
      ["Architecture Style", "4-Layer Clean Architecture Monolith (Domain, Application, Infrastructure, WebApi) + Decoupled Mobile Solution (Mobile.Core, Mobile.App, Mobile.UnitTests)"],
      ["Data Stores", "Microsoft SQL Server (MotorcycleDealerDb \u2014 27 Normalized 3NF Tables, 8 RowVersion, 4 Immutable Tables, RCSI) + On-Device Encrypted SQLCipher WAL (9 Local Tables)"],
      ["Governed User Roles", "Customer (20), DealerStaff (22, IDealerScoped across 8 Tables), Admin (32, Cross-Dealer Web HQ) \u2014 48 Permissions (3-Way Parity across Backend, SPA & Mobile.Core)"],
      ["Deep-Dive & BD/DD Suite", "100% Unified v1.3.3 Parity across PRD (FR-01..10), ARCH (ADR-001..007), DB (27 Server + 9 SQLCipher Tables), DESIGN, BD_00 + DD_01..04, OpenAPI 3.1.0, 85-Case QA Plan (TC-001..085), Runbook, Release Notes v1.0.0..v1.3.3, 30-Route Web + 16-Screen Android/iOS Mobile UX & MOD-01..06"],
      ["Agentic & MCP Suite", "Antigravity .agents/ Plugin (motorcycle-dealer-suite), 6 Scoped AGENTS.md Rulebooks (+mobile/AGENTS.md), 9 EN/VI Skills (+dotnet-maui-mobile), Safety Hooks, verify-mobile-maui.mjs (8/8), verify-post-task-sync.mjs (11/11), 183-Day Git Shipper (502/502 Files Mapped, 0 Overlaps), 2 Local Stdio (v1.3.0) + 7 Global MCP Servers"],
      ["Roadmap Gate Status", "All 10 Phases Signed Off (88 / 88 WBS Tasks = 100.0% \u2014 v1.3.3 Multi-Platform Sync Verified: 830/830 xUnit [727 Backend + 103 Mobile, 0 warnings], 64/64 SPA, 8/8 MAUI Guardrails, SMK-01..10, 30/30 CDP Routes, Android APKs [92.50 MB Debug / 69.31 MB Release] + iOS net10.0-ios Debug & Release 0 Warnings, Zero-Mock Real-Data Verified, 502/502 Files Synced)"],
    ],
    [2800, 6560]
  ),
  spacer(),
  calloutBox(
    "Strict 10-Phase Gate & Mandatory Post-Task Synchronization Governance",
    "All 10 engineering phases (T-101..T-1008, 88 WBS tasks) execute sequentially behind the 7-Stage Quality Loop (15 adversarial reviews + 6 test batteries) and the permanent Mandatory Post-Task Synchronization Gate (.agents/scripts/verify-post-task-sync.mjs + 8-check verify-mobile-maui.mjs), guaranteeing 100% parity across all Markdown/OpenAPI specs, this OpenXML Word specification, trackers, 6 rulebooks, 9 skills, and all 502 files across the 183-day GitHub release schedule."
  ),

  // TABLE OF CONTENTS PAGE
  new Paragraph({ children: [new PageBreak()] }),
  new Paragraph({
    spacing: { before: 120, after: 200 },
    children: [tr("Table of Contents", { bold: true, size: 32, color: COLOR_NAVY })],
  }),
  new TableOfContents("Table of Contents", {
    hyperlink: true,
    headingStyleRange: "1-3",
  }),

  // ==========================================================================
  // SECTION 1: EXECUTIVE OVERVIEW & PRODUCT VISION
  // ==========================================================================
  h1("1. Executive Overview & Product Vision", true),
  p(
    "The Motorcycle Dealer & Loyalty Management Platform unifies multi-branch motorcycle showroom retail and post-sale workshop servicing into one auditable, real-time digital platform. Designed for regional dealership networks operating across Hanoi, Da Nang, and Ho Chi Minh City, the system connects initial vehicle sales directly to digital Vehicle Identification Number (VIN) ownership, proactive maintenance reminders, concurrency-safe service bay booking, instant WebSocket staff alerts, and tiered customer loyalty rewards."
  ),

  h2("1.1 Problem Statement & Operational Gaps"),
  bullet(
    "Customer Post-Sale Fragmentation",
    "Riders rely on paper warranty booklets, miss periodic mileage (1,000 km, 4,000 km, 12,000 km) or calendar (6-month) service intervals, lack live visibility into dealer bay capacity, and cannot seamlessly apply purchase loyalty points toward workshop maintenance vouchers."
  ),
  bullet(
    "Workshop Overbooking & Refresh Friction for Dealer Staff",
    "Static appointment tables require manual browser refreshes to spot incoming bookings, risk peak-hour bay double-booking under concurrent demand, and expose cross-branch confusion without strict single-dealer data scoping."
  ),
  bullet(
    "Executive Blind Spots & Loyalty Liability Drift",
    "Network administrators lack unified real-time Chart.js visibility comparing showroom sales revenue against workshop service revenue per dealer, while mutable point-counter columns in naive databases invite race conditions and unaudited point discrepancies."
  ),

  h2("1.2 Business Objectives & Quantitative KPIs"),
  makeTable(
    ["ID", "Business Objective", "Quantitative KPI", "Verification Mechanism", "Target"],
    [
      ["BO-01", "Eliminate Bay Overbooking", "Concurrent slot over-allocation rate (BookedCount > MaxCapacity)", "ServiceTimeSlots RowVersion + CHECK constraint + parallel race tests", "0 double-bookings (100%)"],
      ["BO-02", "Zero-Refresh Staff Alerts", "Latency from POST /api/v1/bookings commit to DealerStaff DOM update", "SignalR /hubs/notifications BookingCreated push to dealer-{dealerId}", "< 500 ms P95; 0 reloads"],
      ["BO-03", "Closed-Loop VIN Retention", "Auto-provisioned SalesOrders VINs + duplicate reminder rate", "Atomic CustomerVehicles insert + MaintenanceReminders.DeduplicationKey", "100% onboard; 0% dupes"],
      ["BO-04", "Financial Ledger Integrity", "AvailablePoints vs SUM(PointLedgerEntries.PointsDelta) drift", "Append-only PointLedgerEntries + unique IdempotencyKey + trigger", "0 point discrepancy"],
      ["BO-05", "Multi-Dealer Analytics Speed", "Cross-dealer Sales + Service revenue & booking query latency", "/api/v1/admin/analytics/* over covering SQL Server indexes", "< 300 ms P95"],
      ["BO-06", "Multi-Branch Tenant Safety", "Unauthorized cross-role or cross-dealer read/write rate", "UI-Router guards + [HasPermission] + IDealerScoped Global Query Filter", "100% blocked (401/403/404)"],
    ],
    [900, 1960, 2300, 2600, 1600]
  ),

  h2("1.3 Governed User Personas & Role Boundaries"),
  makeTable(
    ["Role Code", "Persona & Portal", "Tenancy / Data Scope", "Core Operational Capabilities"],
    [
      ["Customer", "Minh Nguyen \u2014 Customer Portal (app.customer.*)", "Users.DealerId IS NULL; strictly scoped to CustomerId == currentUser.UserId", "Browse showroom & dealer VIN stock; manage digital VIN garage (showroom & external bikes); update odometer; receive VIN reminders; book open time slots with vouchers; track points, tiers, and redeem rewards."],
      ["DealerStaff", "Lan Tran \u2014 Dealer Staff Workbench (app.staff.*)", "Users.DealerId == AssignedDealerId (mandatory); isolated via IDealerScoped filter & dealer-{dealerId} hub group", "Manage branch VIN inventory & ServiceTimeSlots capacities; execute showroom SalesOrders; receive instant SignalR BookingCreated alerts without page refresh; progress bookings; settle ServiceInvoices."],
      ["Admin", "Quoc Anh Hoang \u2014 Admin Portal (app.admin.*)", "Users.DealerId IS NULL; platform-wide cross-dealer oversight & role-Admin hub group", "Manage Dealers & DealerStaff assignments; govern MotorcycleModels & MaintenanceRules; configure LoyaltyTiers, VoucherCatalogs & audited adjustments; inspect multi-dealer Chart.js revenue & booking dashboards."],
    ],
    [1300, 2100, 2460, 3500]
  ),

  h2("1.4 MECE End-to-End Customer Lifecycle & Explicit Non-Goals"),
  p("Every table in MotorcycleDealerDb participates in an 8-stage closed-loop value chain:"),
  numItem("lifecycleNums", "Stage 1 \u2014 Showroom Discovery", "Customers browse Dealers, MotorcycleModels, and live VIN availability across regional showrooms."),
  numItem("lifecycleNums", "Stage 2 \u2014 VIN Onboarding (Sale or External)", "Showroom SalesOrders atomically mark DealerInventories.StockStatus = 'Sold', create CustomerVehicles, and credit purchase points; external owners register 17-char VINs manually."),
  numItem("lifecycleNums", "Stage 3 \u2014 Automated VIN Maintenance Monitoring", "VinMaintenanceReminderWorker evaluates odometer (NextDueMileageKm) and calendar (NextDueDateUtc) rules to emit deduplicated MaintenanceReminders."),
  numItem("lifecycleNums", "Stage 4 \u2014 Concurrency-Safe Slot Booking", "Customers lock an open ServiceTimeSlot (RowVersion) for a registered VIN and optionally reserve an Available CustomerVoucher."),
  numItem("lifecycleNums", "Stage 5 \u2014 Instant SignalR Dispatch", "NotificationHub pushes BookingCreated over WebSockets to dealer-{dealerId} so DealerStaff workbenches prepend the booking card without page refresh."),
  numItem("lifecycleNums", "Stage 6 \u2014 Workshop Execution & Settlement", "DealerStaff transitions booking status (Pending -> Confirmed -> CheckedIn -> InProgress -> Completed) and issues a 1:1 ServiceInvoice consuming any reserved voucher."),
  numItem("lifecycleNums", "Stage 7 \u2014 Immutable Loyalty Accrual & Rewards", "Net spending credits PointLedgerEntries (10,000 VND = 1 base point * TierMultiplier), evaluates automatic Bronze/Silver/Gold/Platinum promotions, and unlocks VoucherCatalogs redemptions."),
  numItem("lifecycleNums", "Stage 8 \u2014 Executive Retention Intelligence", "Admin Chart.js dashboards aggregate realized Showroom Sales + Workshop Service revenue and booking conversion funnels per dealer."),
  p("Explicit Non-Goals (Out of Scope): Online payment-gateway card processing (settled at dealership POS), commercial Apple App Store / Google Play store release signing & Admin mobile console (Phase 10 v1.3.3 delivers multi-platform C# .NET 10 MAUI net10.0-android + net10.0-ios compilation with iOS 15+ strict ATS/Keychain/UNUserNotificationCenter for Public, Customer & DealerStaff while Admin stays on AngularJS Web HQ), warehouse multi-bin supplier procurement ERP, multi-currency FX (standardized on VND DECIMAL(18,2)), peer-to-peer used bike classifieds, and physical deletion/mutation of financial records.", { italics: true, color: COLOR_MUTED }),

  // ==========================================================================
  // SECTION 2: PRODUCT REQUIREMENTS & ACCEPTANCE CRITERIA
  // ==========================================================================
  h1("2. Product Requirements & Acceptance Criteria (FR-01 to FR-10)", true),
  p("Every mandatory item in REQUIREMENTS.md and the v1.1 / v1.2 / v1.3 expansion scope maps deterministically to ten functional requirement domains (FR-01 through FR-10) and five non-functional requirement categories."),

  h2("2.1 Baseline CV-to-Specification Traceability Matrix"),
  makeTable(
    ["CV / Expansion Pillar", "PRD IDs", "Architectural & Schema Anchors", "Target Phase"],
    [
      ["Role-Based Access: 3 roles (Customer, Dealer Staff, Admin) with route guards & granular permissions", "FR-01", "Users, Roles, Permissions (48: 20/22/32), UserRoles, RolePermissions, RefreshTokens; 30-route UI-Router $transitions.onBefore + [HasPermission] + 8-table IDealerScoped (ADR-002, ADR-006)", "Phases 4, 8, 9 & 10"],
      ["Service Booking System: Book by dealer & available time slot + automated VIN & schedule reminders", "FR-02, FR-03, FR-04", "Dealers, MotorcycleModels, DealerInventories, SalesOrders, CustomerVehicles, MaintenanceRules, MaintenanceReminders, ServiceTimeSlots, ServiceBookings, VinMaintenanceReminderWorker (ADR-005)", "Phase 5"],
      ["Loyalty & Rewards Engine: Auto points from spending, membership tiers, voucher redemption", "FR-05", "ServiceInvoices, LoyaltyTiers (Bronze..Platinum), LoyaltyAccounts, PointLedgerEntries, VoucherCatalogs, CustomerVouchers (ADR-003)", "Phase 6"],
      ["Real-Time Notifications: SignalR instant booking, test-drive, CSAT & warranty alerts without refresh", "FR-06", "NotificationHub (/hubs/notifications, 10 events), dealer-{dealerId} & user-{userId} groups, Notifications table, $rootScope.$applyAsync & Android NotificationChannels (ADR-004, ADR-007)", "Phases 6, 8, 9 & 10"],
      ["Admin Analytics Dashboard: Revenue, booking, CSAT & warranty stats per dealer with Chart.js", "FR-07", "/api/v1/admin/analytics/* covering index projections + AngularJS Chart.js v4 reusable components & 17-col OWASP CSV export (ADR-001, ADR-006)", "Phases 7 & 9"],
      ["v1.1 Test-Drives, OEM Spare-Parts Itemized Invoicing & SHA-256 Printable Official Receipts", "FR-08", "TestDriveBookings (<=2/window cap), SpareParts, ServiceInvoiceLineItems (immutable), /receipt SHA-256 seals, SCR-CUS-06, SCR-STF-06, SCR-ADM-07, <printable-receipt-modal>", "Phase 8"],
      ["v1.2 Post-Service 5-Star CSAT Reviews (+25 Bonus Points) & Factory VIN Warranty Claims", "FR-09", "ServiceReviews (1:1 Paid ServiceInvoice, +25 AvailablePoints ServiceReviewBonus), WarrantyClaims (36m / 30,000 km OEM coverage, CoveredSparePartId), SCR-CUS-07, SCR-STF-07, SCR-ADM-08", "Phase 9"],
      ["v1.3 Native C# .NET 10 MAUI Android & iOS Field Super-App (CommunityToolkit.Maui 13.0, Polly v8, SQLCipher & Camera)", "FR-10", "mobile/MotorcycleDealer.Mobile.sln (Mobile.Core net10.0 [FluentValidation 11.11, Microsoft.Extensions.Http.Resilience 10.11.1, Humanizer 2.14.1, WeakReferenceMessenger] + Mobile.App net10.0-android;net10.0-ios [CommunityToolkit.Maui 13.0, 16 SourceGen x:DataType views, strict iOS ATS/Keychain/UNUserNotificationCenter] + 103 Mobile.UnitTests), 9-table encrypted SQLCipher WAL, SingleFlightTokenRefreshHandler, Camera2/AVFoundation VIN/QR & offline QrCodeMatrixEncoder (ADR-007)", "Phase 10"],
    ],
    [2500, 960, 4700, 1200]
  ),

  h2("2.2 Functional Requirements & Given/When/Then Acceptance Criteria"),
  h3("FR-01: Role-Based Access Control (RBAC) & Granular Permissions"),
  bullet("Scope", "Enforces 3 roles (Customer=20, DealerStaff=22, Admin=32 across 48 granular Permissions.<Module>.<Action> claims), rotating SHA-256 RefreshTokens (7-day TTL) paired with 15-minute JWTs, AngularJS UI-Router guards across 30 navigable routes, and 8-table single-dealer IDealerScoped isolation."),
  bullet("AC-01.1 (UI-Router Guard)", "Given a Customer session, When navigating to app.admin.analytics or app.staff.workbench, Then $transitions.onBefore blocks controller instantiation and redirects to app.public.forbidden (or app.public.login if unauthenticated)."),
  bullet("AC-01.2 (Server Policy Handler)", "Given a DealerStaff JWT lacking Permissions.Loyalty.ManageTiers, When calling PUT /api/v1/loyalty/tiers/{id}, Then PermissionAuthorizationHandler returns HTTP 403 Forbidden (ProblemDetails)."),
  bullet("AC-01.3 (IDealerScoped Isolation)", "Given DealerStaff assigned to Dealer D1, When requesting or mutating a ServiceBooking belonging to Dealer D2, Then EF Core Global Query Filters exclude D2 rows and return HTTP 404 Not Found / 403 Forbidden."),
  bullet("AC-01.4 (Single-Flight Refresh)", "Given an expired access token and valid RefreshToken, When parallel AngularJS $http calls receive 401, Then AuthHttpInterceptor queues requests behind one POST /api/v1/auth/refresh rotation and replays them transparently."),

  h3("FR-02 & FR-03: Showroom VIN Sales & Customer Digital VIN Garage"),
  bullet("Scope", "Dealers stock physical motorcycles by globally unique 17-char ISO 3779 VIN (StockStatus = 'Available'). Completing a SalesOrder atomically marks VIN 'Sold', creates CustomerVehicles with initial MaintenanceRules targets, and accrues purchase points. Customers can also register external VINs (PurchasedSalesOrderId = NULL) and update CurrentMileageKm monotonically."),
  bullet("AC-02.1 (Atomic Sale Onboarding)", "Given an 'Available' DealerInventory VIN at 45,000,000 VND and a Bronze buyer (1.00x, 0 pts), When DealerStaff submits POST /api/v1/sales-orders, Then StockStatus becomes 'Sold', CustomerVehicles is created for that VIN, 4,500 points are appended to PointLedgerEntries, and buyer upgrades automatically from Bronze to Gold (2,000+ pts)."),
  bullet("AC-02.2 (Concurrent VIN Sale Lock)", "Given two staff sessions submitting SalesOrders for the same VIN simultaneously, When both hit SQL Server, Then one commits (201 Created) and the second fails DealerInventories.RowVersion check with 409 Conflict."),
  bullet("AC-03.1 & AC-03.2 (External VIN & Monotonic Odometer)", "External 17-char VINs bind immediately to model MaintenanceRules without retroactive purchase points; PATCH /api/v1/vehicles/{id}/mileage with mileage < CurrentMileageKm fails with 422 Unprocessable Entity."),

  h3("FR-04: Concurrency-Safe Service Booking & Automated VIN Maintenance Reminders"),
  bullet("Scope", "Customers book open ServiceTimeSlots (BookedCount < MaxCapacity) with RowVersion optimistic locking and optional CustomerVoucher reservation (Available -> ReservedOnBooking). VinMaintenanceReminderWorker scans active vehicles against mileage (200 km lead) and calendar (7-day lead) thresholds with SQL-enforced DeduplicationKey uniqueness."),
  bullet("AC-04.1 (Anti-Double-Booking)", "Given ServiceTimeSlot S1 has MaxCapacity = 3 and BookedCount = 2, When Customer A and Customer B book S1 concurrently, Then one increments BookedCount to 3 (201 Created, initial booking Status = 'Pending') and the contender receives 409 Conflict after RowVersion check."),
  bullet("AC-04.2 (Deduplicated VIN Reminder)", "Given VIN RLHJF5108RY000123 reaches 900 km (within 200 km of the 1,000 km rule), When VinMaintenanceReminderWorker runs consecutive cycles, Then cycle 1 inserts one MaintenanceReminders row (DeduplicationKey = 'RLHJF5108RY000123:{RuleId}:1000') and pushes MaintenanceReminderTriggered, while cycle 2 creates zero duplicates."),
  bullet("AC-04.3 (Cancellation Release)", "Cancelling a Pending/Confirmed booking decrements ServiceTimeSlots.BookedCount by 1 and restores an unexpired ReservedOnBooking voucher to Available (or Expired if past ExpiresAtUtc)."),

  h3("FR-05, FR-06 & FR-07: Loyalty Engine, SignalR Real-Time Alerts & Admin Chart.js Analytics"),
  bullet("AC-05.1 (Workshop Invoice & Tier Upgrade)", "Settling a 700,000 VND gross invoice with a 100,000 VND reserved voucher for a Silver member (1.20x, 1,960 qualifying pts) records NetPaidAmount = 600,000 VND, marks voucher Used, appends Floor(Floor(600,000/10,000) * 1.20) = 72 points, promotes the member to Gold (2,032 qualifying pts), and advances vehicle LastServiceMileageKm."),
  bullet("AC-05.2 & AC-05.3 (Voucher Redemption Guards)", "Redeeming a 300-pt voucher deducts AvailablePoints ONLY (leaving LifetimeQualifyingPoints intact) and issues a unique CustomerVoucher code; insufficient points, unmet MinTierId, or exhausted QuotaTotal returns 422/409 with zero ledger mutation."),
  bullet("AC-06.1 & AC-06.2 (Instant SignalR Staff Alert & Tenant Isolation)", "When a customer creates a booking at Dealer D1, DealerStaff connected to /hubs/notifications in group dealer-{D1} sees the booking card prepended via $rootScope.$applyAsync in < 500 ms without page refresh, while dealer-{D2} receives zero frames."),
  bullet("AC-07.1 & AC-07.2 (Admin Chart.js Analytics)", "Admin views per-dealer Showroom + Service revenue Stacked Bar, Booking Status Doughnut/Bar, Monthly Revenue & Booking Multi-Line, and Loyalty Tier Doughnut charts matching exact SQL Server Completed/Paid sums; non-Admin callers receive 403 Forbidden."),

  h3("FR-08: Showroom Test-Drive Scheduling, OEM Spare-Parts Itemization & Printable Receipts (v1.1)"),
  bullet("AC-08.1 (Concurrency-Capped Test-Drives & Live SignalR Push)", "Given <= 2 active TestDriveBookings per (DealerId, MotorcycleModelId, ScheduledDate, StartTime), When a Customer books a test-ride on SCR-CUS-06, Then TestDriveService commits atomically (or returns 409 TEST_DRIVE_SLOT_FULL / TEST_DRIVE_OVERLAP) and pushes TestDriveCreated to dealer-{dealerId} without page refresh."),
  bullet("AC-08.2 (Model-Compatible OEM SpareParts & Immutable Line Items)", "When DealerStaff settles a workshop booking with OEM SpareParts on SCR-STF-02, ServiceInvoiceService validates IsActive and CompatibleModelId fitment (422 SPARE_PART_INCOMPATIBLE), persists append-only ServiceInvoiceLineItems summing to PartsAmount, and blocks all future UPDATE/DELETE mutations."),
  bullet("AC-08.3 (Tenant-Isolated Printable Receipts with SHA-256 Verification)", "Calling GET /api/v1/service-invoices/{id}/receipt or /api/v1/sales-orders/{id}/receipt enforces owner/IDealerScoped RBAC and returns RCP-SVC-* / RCP-SLS-* payloads with a deterministic 16-hex SHA-256 ReceiptVerificationHash rendered inside @media print <printable-receipt-modal>."),

  h3("FR-09: Post-Service 5-Star CSAT Reviews (+25 Bonus Points) & VIN Warranty Claims (v1.2)"),
  bullet("AC-09.1 (1:1 Paid ServiceInvoice CSAT Review & +25 Spendable-Only Bonus)", "When a Customer submits POST /api/v1/service-reviews for an owned Paid ServiceInvoice on SCR-CUS-04 or SCR-CUS-07, ServiceReviewService enforces UQ_ServiceReviews_ServiceInvoiceId (409 REVIEW_ALREADY_SUBMITTED on duplicate races), appends SERVICE_REVIEW:{InvoiceId}:BONUS (+25 AvailablePoints ONLY, leaving LifetimeQualifyingPoints unchanged), and pushes ServiceReviewSubmitted + LoyaltyPointsUpdated over SignalR."),
  bullet("AC-09.2 (Factory 36m / 30,000 km VIN Warranty Claims & Live Adjudication)", "Submitting POST /api/v1/warranty-claims for an owned CustomerVehicle validates <= WarrantyMonths (36) and <= WarrantyMileageKm (30,000) (422 WARRANTY_COVERAGE_EXPIRED), blocks duplicate open claims per (CustomerVehicleId, DefectCategory) (409 ACTIVE_WARRANTY_CLAIM_EXISTS), validates model-compatible OEM CoveredSparePartId on staff adjudication, and pushes WarrantyClaimCreated / WarrantyClaimStatusChanged without page refresh."),

  h3("FR-10: Native Android & iOS Field Mobile Application (.NET 10 MAUI, CommunityToolkit.Maui 13.0, Polly v8 & SQLCipher — v1.3.3)"),
  bullet("AC-10.1 (Single-Flight 401 Refresh, Polly v8 Resilience & Multi-Device Logout Isolation)", "SingleFlightTokenRefreshHandler collapses 10 concurrent 401 responses into exact 1 POST /api/v1/auth/refresh rotation and wraps Microsoft.Extensions.Http.Resilience 10.11.1 (Polly v8 idempotent-only retry on GET/HEAD/OPTIONS or explicit Idempotency-Key, circuit breaker 30s/0.5/6/15s, 15s attempt timeout); LogoutCoordinator posts LogoutRequest { RefreshToken, RevokeAllDevices = false } by default so signing out on mobile wipes Android Keystore/iOS Keychain + user-scoped SQLCipher rows without terminating desktop Web SPA sessions."),
  bullet("AC-10.2 (Encrypted 9-Table SQLCipher WAL Cache, FluentValidation & Online-Only Law)", "MobileOfflineDb encrypts 9 local tables via platform SecureStorage key (PRAGMA journal_mode=WAL, OwnerScopeKey partitioning, wrong-key SQLiteException rejection) while OfflineOutboxSyncService strictly prohibits offline queueing of SalesOrders, ServiceInvoices, ServiceInvoiceLineItems, PointLedgerEntries, RowVersion slot/test-drive reservations, and voucher redemptions. FluentValidation 11.11.0 (5 AbstractValidators) + WeakReferenceMessenger + Humanizer.Core 2.14.1 power clean, zero-mock forms and cross-ViewModel synchronization."),
  bullet("AC-10.3 (Dual-Platform Android/iOS Camera VIN/QR, CommunityToolkit.Maui 13.0 & 10-Event SignalR)", "CameraBarcodeScannerPage supports Android Camera2 and Apple iOS AVFoundation AVCaptureDevice fallback to decode 17-char ISO 3779 VINs and dealer://voucher|booking|receipt/v1 URIs; pure C# QrCodeMatrixEncoder renders offline QRs; CommunityToolkit.Maui 13.0 delivers StatusBarBehavior, Expander, TouchBehavior, EmailValidationBehavior, Toast/Snackbar, and official converters; SignalRMobileService subscribes to all 10 events with native Android NotificationChannels and Apple iOS UNUserNotificationCenter alerts."),

  h2("2.3 Edge Cases & Non-Functional Requirements Summary"),
  makeTable(
    ["Edge Case / NFR", "Deterministic Platform Behavior"],
    [
      ["EC-01..EC-02: Slot & VIN Races", "RowVersion concurrency tokens + SQL CHECK (BookedCount <= MaxCapacity) reject simultaneous last-bay or duplicate-VIN writes with HTTP 409 Conflict ProblemDetails."],
      ["EC-05..EC-06: Voucher Cap & Expiry", "VoucherDiscountAmount clamps to MIN(CalculatedDiscount, MaxDiscountAmount, GrossAmount) so NetPaidAmount >= 0; cancelling after ExpiresAtUtc marks voucher Expired rather than Available."],
      ["EC-07..EC-08: Network & Worker Retries", "UQ_ServiceInvoices_BookingId, UQ_PointLedgerEntries_IdempotencyKey, and UQ_MaintenanceReminders_DedupKey guarantee idempotent settlement and zero duplicate reminders across retries/restarts."],
      ["EC-09..EC-12: SignalR Reconnect & Digest Cleanup", "@microsoft/signalr automatic reconnect [0, 2s, 5s, 10s, 30s] reconciles missed notifications; $onDestroy destroys Chart.js instances and unregisters SignalR listeners."],
      ["NFR-SEC / NFR-AUD / NFR-PERF", "PBKDF2/Argon2id hashing, 256-bit+ externalized JWT keys, UTC DATETIMEOFFSET(7), DECIMAL(18,2) VND, < 200 ms CRUD P95, < 300 ms analytics P95, server pagination (max 100)."],
    ],
    [2600, 6760]
  ),

  // ==========================================================================
  // SECTION 3: SYSTEM ARCHITECTURE & ADRs
  // ==========================================================================
  h1("3. System Architecture & Architecture Decision Records (ADR-001 to ADR-007)", true),
  p("The platform pairs a 4-Layer Clean Architecture ASP.NET Core 10 modular monolith with both a decoupled AngularJS 1.8.3 Web SPA and a decoupled Multi-Platform Native C# .NET 10 MAUI Android & iOS Field App (net10.0-android;net10.0-ios) over HTTPS REST (/api/v1/*) and WebSocket SignalR (/hubs/notifications)."),

  h2("3.1 Clean Architecture Layering & Inward Dependency Rule"),
  makeTable(
    ["Project / Tier", "Allowed Dependencies", "Core Engineering Responsibilities"],
    [
      ["MotorcycleDealer.Domain", "Zero external NuGet or project references", "27 pure C# entities, ISO 3779 Vin value object, LoyaltyPointsCalculator, MaintenanceDueEvaluator, IDealerScoped, IAuditableEntity, domain exceptions."],
      ["MotorcycleDealer.Application", "References Domain ONLY", "Vertical feature slices (CQRS handlers/services), record DTOs, FluentValidation validators, 48 Permissions constants, VoucherAndDocumentNumberHelper, IApplicationDbContext & INotificationPublisher ports."],
      ["MotorcycleDealer.Infrastructure", "References Application & Domain", "MotorcycleDealerDbContext (EF Core 10), 27 IEntityTypeConfiguration<T> classes, Global Query Filters, SQL migrations & DatabaseSeeder, JWT/RefreshToken services, SignalR publisher, VinMaintenanceReminderWorker."],
      ["MotorcycleDealer.WebApi", "References Application & Infrastructure", "18 thin /api/v1/* controllers, strongly-typed NotificationHub (/hubs/notifications, 10 events), PermissionPolicyProvider & PermissionAuthorizationHandler, GlobalExceptionHandler (RFC 7807 ProblemDetails), health checks."],
      ["frontend/ (AngularJS 1.8 SPA)", "Decoupled static SPA communicating via REST & WSS", "Component-based modules (frontend/src/app/core, shared, features; 47 $inject units, 30 navigable routes, Zero-Mock real-data), @uirouter/angularjs $transitions.onBefore guards, single-flight 401 refresh queue, $rootScope.$applyAsync SignalR bridge, Chart.js v4 & printableReceiptModal."],
      ["mobile/ (.NET 10 MAUI Android & iOS)", "Decoupled 3-project solution (Mobile.Core net10.0 <- Mobile.App net10.0-android;net10.0-ios + Mobile.UnitTests)", "Mobile.Core (zero <UseMaui>, 48-RBAC parity, FluentValidation 11.11, Polly v8 Microsoft.Extensions.Http.Resilience 10.11.1, Humanizer 2.14.1, WeakReferenceMessenger, SingleFlightTokenRefreshHandler, 9-table encrypted SQLCipher WAL, 10-event SignalR, 17 C# 13 partial ViewModels) + Mobile.App (CommunityToolkit.Maui 13.0, 16 SourceGen x:DataType views, Camera2/AVFoundation VIN/QR, offline QR, Android Keystore/NotificationChannels & iOS strict ATS/Keychain/UNUserNotificationCenter)."],
      ["Unit & Integration Tests", "xUnit, FluentAssertions, WebApplicationFactory", "830 / 830 combined .NET xUnit tests: 645 Backend Domain.UnitTests + 82 SQL Server LocalDB Api.IntegrationTests + 103 pure net10.0 Mobile.UnitTests (0 warnings, 0 flaky)."],
    ],
    [2400, 2160, 4800]
  ),

  h2("3.2 Ratified Architecture Decision Records (ADR-001 to ADR-007)"),
  makeTable(
    ["ADR", "Decision Title", "Architectural Choice & Rationale", "Key Consequence / Safeguard"],
    [
      ["ADR-001", "Clean Architecture 4-Layer Monolith (.NET 10)", "Single deployable ASP.NET Core 10 modular monolith instead of distributed microservices or 2-tier controllers.", "ACID SQL transactions span booking, invoicing, voucher settlement, and ledger accrual with sub-ms pure domain unit tests."],
      ["ADR-002", "Single SQL Server DB + IDealerScoped Filters", "Shared MotorcycleDealerDb with DealerId FKs and EF Core Global Query Filters for DealerStaff; unrestricted for Admin.", "Customers roam across dealers with one wallet; Admin runs single-query Chart.js aggregations; zero BOLA/IDOR cross-dealer leaks."],
      ["ADR-003", "Double-Check Immutable Loyalty Ledger", "Append-only PointLedgerEntries (IdempotencyKey + mutation-blocking trigger) paired with RowVersion-guarded LoyaltyAccounts snapshot.", "O(1) wallet reads with bank-grade auditability (SUM(PointsDelta) == AvailablePoints) and non-downgrading LifetimeQualifyingPoints."],
      ["ADR-004", "Strongly-Typed SignalR Hub & Server Group Routing", "NotificationHub : Hub<INotificationClient> at /hubs/notifications enrolling user-{id}, dealer-{id}, and role-Admin from verified JWT claims.", "Sub-second real-time alerts on Web SPA and Android/iOS Field App without page refresh and zero client-spoofable group subscriptions."],
      ["ADR-005", "Hosted VinMaintenanceReminderWorker + DedupKey", "In-process .NET BackgroundService using PeriodicTimer + unique DeduplicationKey ({VIN}:{RuleId}:{TargetKm}) in SQL Server.", "Zero external scheduler/broker containers required; guaranteed at-most-once alert per vehicle milestone across restarts."],
      ["ADR-006", "Decoupled Modular AngularJS 1.8 SPA", ".component() architecture, @uirouter/angularjs hierarchy, queued 401 refresh interceptor, and $onDestroy lifecycle cleanup.", "Eliminates legacy $scope soup, concurrent token-refresh stampedes, detached WebSocket listeners, and Chart.js canvas leaks."],
      ["ADR-007", "Decoupled Multi-Platform Android & iOS .NET 10 MAUI, Enterprise Libraries & SQLCipher", "Standalone mobile/ solution isolating pure net10.0 Mobile.Core (FluentValidation, Polly v8 Http.Resilience, Humanizer, WeakReferenceMessenger) from net10.0-android;net10.0-ios Mobile.App (CommunityToolkit.Maui 13.0, SourceGen x:DataType, strict iOS ATS/Keychain/UNUserNotificationCenter), 9-table AES-256 SQLCipher WAL cache, and RevokeAllDevices=false logout.", "103 fast headless mobile unit tests, dual-platform Android/iOS 0-warning compilation, zero impact on backend Docker images, idempotent Polly v8 resilience, offline basement QR passbook/receipts, strict ADR-003/ADR-005 online-only financial guard, and 8/8 verify-mobile-maui.mjs verification."],
    ],
    [960, 2200, 3300, 2900]
  ),

  h2("3.3 Standardized RFC 7807 ProblemDetails & Deployment Profiles"),
  bullet("RFC 7807 Error Contract", "All 4xx/5xx responses emit application/problem+json containing type, title, status, detail, instance, errorCode, traceId, and validation errors (400 VALIDATION_FAILED, 401 UNAUTHORIZED, 403 FORBIDDEN_PERMISSION / FORBIDDEN_DEALER_SCOPE, 404 RESOURCE_NOT_FOUND, 409 CONCURRENCY_CONFLICT / SLOT_CAPACITY_EXHAUSTED, 422 INSUFFICIENT_LOYALTY_POINTS / ODOMETER_REGRESSION, 500 INTERNAL_SERVER_ERROR with suppressed stack traces)."),
  bullet("Dual Execution Profiles", "Profile A runs natively on Windows against (localdb)\\MSSQLLocalDB with automatic EF Core migration and idempotent demo seeding; Profile B launches docker-compose.yml (mssql/server:2022-latest + ASP.NET Core 10 WebApi + nginx:alpine serving AngularJS SPA and proxying /api/v1/ and /hubs/notifications WebSockets on port 8080)."),

  // ==========================================================================
  // SECTION 4: 27-TABLE 3NF SQL SERVER SCHEMA
  // ==========================================================================
  h1("4. 27-Table 3NF SQL Server Database Schema (MotorcycleDealerDb)", true),
  p("MotorcycleDealerDb enforces strict Third Normal Form (3NF) across 27 PascalCase plural tables in 6 bounded contexts on Microsoft SQL Server with READ_COMMITTED_SNAPSHOT ON (RCSI)."),

  h2("4.1 Global Physical Schema Conventions"),
  bullet("Clustered Sequential UUIDs", "Every entity uses Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() (generated in .NET 10 via Guid.CreateVersion7()) to prevent B-Tree page splits."),
  bullet("Monetary & Temporal Precision", "All currency columns use DECIMAL(18,2) NOT NULL (never FLOAT/REAL); all timestamps use UTC DATETIMEOFFSET(7); workshop and test-drive schedules use DATE and TIME(0)."),
  bullet("ISO 3779 VIN & Cascading Safety", "VIN columns enforce VARCHAR(17) uppercase alphanumeric excluding I, O, Q (CK_*_VIN). Domain FKs default to ON DELETE NO ACTION to prevent SQL Server Msg 1785 multiple-cascade cycles, while SalesOrders, ServiceInvoices, ServiceInvoiceLineItems, and PointLedgerEntries prohibit physical UPDATE/DELETE."),

  h2("4.2 Complete 27-Table Data Dictionary Summary"),
  makeTable(
    ["#", "Table Name", "Context", "Scope / RowVersion", "Key Columns & Integrity Constraints"],
    [
      ["1", "Users", "Identity & RBAC", "DealerId NULLable", "Email (UQ), NormalizedEmail (UQ), PasswordHash, FullName, PhoneNumber, DealerId (FK), StaffEmployeeCode, CK_Users_DealerStaff_DealerId, IsDeleted"],
      ["2", "Roles", "Identity & RBAC", "Global Seed", "Name (UQ, CHECK IN Customer/DealerStaff/Admin), DisplayName, IsSystemRole"],
      ["3", "Permissions", "Identity & RBAC", "Global Seed (48)", "Code (UQ, Permissions.<Module>.<Action>), Module, DisplayName, Description"],
      ["4", "UserRoles", "Identity & RBAC", "Composite PK", "(UserId, RoleId) PK/FK CASCADE, AssignedAtUtc, AssignedByUserId (NO ACTION)"],
      ["5", "RolePermissions", "Identity & RBAC", "Composite PK", "(RoleId, PermissionId) PK/FK CASCADE, GrantedAtUtc"],
      ["6", "RefreshTokens", "Identity & RBAC", "User-owned", "UserId (FK CASCADE), TokenHash (UQ SHA-256), TokenFamilyId, ExpiresAtUtc, RevokedAtUtc, ReplacedByTokenHash"],
      ["7", "Dealers", "Showroom & Sales", "Root Tenant", "Code (UQ: DLR-HN-01, DLR-DN-01, DLR-HCM-01), Name, City, Address, OpeningTime, ClosingTime, DefaultSlotCapacity, IsDeleted"],
      ["8", "MotorcycleModels", "Showroom & Sales", "Global Catalog", "ModelCode (UQ), Brand, ModelName, Category, EngineDisplacementCc, BaseRetailPrice DECIMAL(18,2), WarrantyMonths/Km, SpecificationsJson"],
      ["9", "DealerInventories", "Showroom & Sales", "IDealerScoped + RV", "DealerId, MotorcycleModelId, VIN VARCHAR(17) (UQ, ISO 3779), EngineNumber (UQ), ListedPrice, StockStatus (Available/Reserved/Sold/Transferred), RowVersion"],
      ["10", "SalesOrders", "Showroom & Sales", "IDealerScoped (Immutable)", "OrderNumber (UQ), DealerId, CustomerId, DealerInventoryId (filtered UQ active), TotalAmount, DiscountAmount, FinalAmount, PointsEarned, Status, CompletedAtUtc"],
      ["11", "CustomerVehicles", "Garage & Reminders", "Customer-owned", "VIN VARCHAR(17) (UQ), CustomerId, MotorcycleModelId, PurchasedSalesOrderId (NULL for external), LicensePlate, CurrentMileageKm, LastServiceMileageKm/DateUtc, NextDueMileageKm/DateUtc"],
      ["12", "MaintenanceRules", "Garage & Reminders", "Model-scoped", "MotorcycleModelId, MilestoneCode (UQ per model), IntervalKm, IntervalMonths, IsRecurring, LeadTimeDays (7), LeadMileageKm (250), ChecklistSummary"],
      ["13", "MaintenanceReminders", "Garage & Reminders", "Immutable Audit", "CustomerVehicleId, MaintenanceRuleId, TargetMileageKm, DueDateUtc, TriggerReason, DeduplicationKey (UQ), Status, ResolvedBookingId, SentAtUtc"],
      ["14", "ServiceTimeSlots", "Booking & Invoicing", "IDealerScoped + RV", "DealerId, SlotDate, StartTime, EndTime (UQ window), MaxCapacity, BookedCount (CHECK <= MaxCapacity), IsBlocked, RowVersion"],
      ["15", "ServiceBookings", "Booking & Invoicing", "IDealerScoped + RV", "BookingNumber (UQ), CustomerId, DealerId, CustomerVehicleId, ServiceTimeSlotId, AppliedCustomerVoucherId, ServiceType, OdometerAtBookingKm, Status (Default 'Pending'), RowVersion"],
      ["16", "ServiceInvoices", "Booking & Invoicing", "IDealerScoped (Immutable)", "InvoiceNumber (UQ), ServiceBookingId (UQ 1:1), DealerId, CustomerId, AppliedCustomerVoucherId, LaborAmount, PartsAmount, GrossAmount, VoucherDiscountAmount, NetPaidAmount, OdometerAtCompletionKm, PointsEarned, PaidAtUtc"],
      ["17", "LoyaltyTiers", "Loyalty & Vouchers", "Global Seed (4)", "TierCode (UQ: Bronze/Silver/Gold/Platinum), RankOrder (1..4), MinQualifyingPoints (0/500/2000/5000), PointsMultiplier (1.00/1.20/1.50/2.00), BenefitsJson"],
      ["18", "LoyaltyAccounts", "Loyalty & Vouchers", "Customer 1:1 + RV", "CustomerId (UQ), CurrentTierId, AvailablePoints (>=0), LifetimeQualifyingPoints (>=0), LifetimeSpentVnd, TierUpgradedAtUtc, RowVersion"],
      ["19", "PointLedgerEntries", "Loyalty & Vouchers", "Append-Only Ledger", "LoyaltyAccountId, TransactionType (SalesOrderEarn/ServiceInvoiceEarn/ServiceReviewBonus/VoucherRedeem/VoucherExpireRefund/AdminAdjustment/WelcomeBonus), PointsDelta (<>0), BalanceAfter (>=0), ReferenceType/Id, IdempotencyKey (UQ), BlockMutation Trigger"],
      ["20", "VoucherCatalogs", "Loyalty & Vouchers", "Global Store", "CodePrefix (UQ), Title, ApplicableScope, PointsCost, DiscountType (FixedAmount/Percentage), DiscountValue, MaxDiscountAmount, MinOrderAmount, ValidDays, QuotaTotal/Redeemed, MinTierId"],
      ["21", "CustomerVouchers", "Loyalty & Vouchers", "Account-scoped + RV", "VoucherCode (UQ), LoyaltyAccountId, VoucherCatalogId, PointsSpent, Status (Available/ReservedOnBooking/Used/Expired), RedeemedAtUtc, ExpiresAtUtc, UsedAtUtc, RowVersion"],
      ["22", "Notifications", "Real-Time Alerts", "User / Dealer / Role", "RecipientUserId, TargetDealerId, TargetRole, EventType (12 notification types across 10 hub events + order/voucher receipts), Title, Message, PayloadJson, IsRead, ReadAtUtc, CreatedAtUtc"],
      ["23", "TestDriveBookings", "Showroom & Sales", "IDealerScoped + RV", "BookingNumber (UQ), CustomerId, DealerId, MotorcycleModelId, AssignedInventoryId (FK), ScheduledDate, StartTime, EndTime, Status, DriverLicenseNumber, RowVersion, IsDeleted"],
      ["24", "SpareParts", "Booking & Invoicing", "Global Catalog", "PartCode (UQ), PartName, Category (EngineOil..Accessory), Brand, CompatibleModelId (NULL=Universal), UnitLabel, UnitPrice DECIMAL(18,2), IsActive"],
      ["25", "ServiceInvoiceLineItems", "Booking & Invoicing", "Immutable Audit", "ServiceInvoiceId (FK), SparePartId (NULL=Custom), LineNumber (UQ per invoice), PartCodeSnapshot, Description, Quantity (>0), UnitPrice, LineTotal"],
      ["26", "ServiceReviews", "Booking & Invoicing", "IDealerScoped + RV", "ReviewNumber (UQ), ServiceInvoiceId (UQ 1:1), CustomerId, DealerId, Rating (CHECK 1..5), QualityTags, BonusPointsAwarded (25), StaffResponse, RespondedByUserId, RowVersion"],
      ["27", "WarrantyClaims", "Garage & Reminders", "IDealerScoped + RV", "ClaimNumber (UQ), CustomerVehicleId, CustomerId, DealerId, CoveredSparePartId (FK), DefectCategory, MileageAtClaimKm, Status, ApprovedCoveredAmountVnd DECIMAL(18,2), RowVersion"],
    ],
    [420, 1780, 1460, 1500, 4200]
  ),

  h2("4.3 High-Impact Composite, Covering & Filtered Indexes + Seed Strategy"),
  bullet("Analytics Covering Indexes", "IX_SalesOrders_Analytics_Covering (DealerId, CompletedAtUtc) INCLUDE (TotalAmount, DiscountAmount, FinalAmount, PointsEarned) WHERE Status = 'Completed' and IX_ServiceInvoices_Analytics_Covering (DealerId, PaidAtUtc) INCLUDE (LaborAmount, PartsAmount, GrossAmount, VoucherDiscountAmount, NetPaidAmount, PointsEarned) satisfy Admin Chart.js revenue queries via 100% index-only scans."),
  bullet("Operational Hot-Path Indexes (31 Workload Indexes)", "IX_ServiceTimeSlots_Dealer_Date_Open, IX_ServiceBookings_Dealer_Status_Created, IX_TestDriveBookings_Dealer_Date_Window & IX_TestDriveBookings_Customer_CreatedDesc, IX_SpareParts_Category_Active_Model, IX_ServiceInvoiceLineItems_SparePartId, IX_ServiceReviews_Dealer_CreatedDesc & IX_ServiceReviews_Customer_CreatedDesc, IX_WarrantyClaims_Dealer_Status_Created & IX_WarrantyClaims_Customer_CreatedDesc, IX_CustomerVehicles_ReminderScan, IX_PointLedgerEntries_Account_CreatedDesc, and filtered unread notification indexes."),
  bullet("Idempotent Demo Seed (T-405, T-801 & T-901)", "Seeds 3 dealerships (Hanoi Flagship, Da Nang Central, Saigon South), 3 roles + 48 permissions, 4 LoyaltyTiers, 8 demo accounts across all roles/tiers, 6 MotorcycleModels with 18 MaintenanceRules, 8 OEM SpareParts, 18 ISO 3779 VIN stock units, rolling 14-day time slots, 5 VoucherCatalogs, 4 TestDriveBookings, 2 ServiceReviews (+25 bonus ledger reconciled), 3 WarrantyClaims, and 6 months of historical orders/bookings/invoices (+ 13 ServiceInvoiceLineItems)."),

  h2("4.4 On-Device Encrypted 9-Table SQLCipher WAL Cache (Phase 10 Android & iOS MAUI — ADR-007)"),
  p("Phase 10 requires zero breaking SQL Server migrations (MotorcycleDealerDb remains at 27 3NF tables). On Android & iOS devices, MobileOfflineDb persists 9 AES-256 encrypted SQLCipher WAL tables keyed via cross-platform MauiSecureTokenStore (Android Keystore allowBackup=false / Apple iOS Keychain Entitlements.plist): 3 public tables preserved across logout (CacheSyncState, CachedDealer, CachedMotorcycleModel) and 6 OwnerScopeKey-partitioned tables wiped on logout/403 (CachedCustomerVehicle, CachedLoyaltyWallet, CachedCustomerVoucher, CachedActiveAppointment, infinite-TTL CachedReceiptSnapshot, and OfflineMutationQueueItem allowing only NotificationRead, NotificationReadAll, VehicleMileageUpdate, ServiceReviewSubmit, and WarrantyClaimSubmit while strictly rejecting SalesOrders, ServiceInvoices, PointLedgerEntries, RowVersion slots/test-drives, and voucher redemptions)."),

  // ==========================================================================
  // SECTION 5: GRANULAR RBAC, REST API v1 & SIGNALR DESIGN
  // ==========================================================================
  h1("5. Granular RBAC Matrix, REST API v1 & SignalR Real-Time Design", true),

  h2("5.1 Complete 48-Permission RBAC Matrix Across Roles"),
  makeTable(
    ["Bounded Context", "Canonical Permission Codes (48 Total)", "Customer", "DealerStaff", "Admin"],
    [
      ["Showroom & Sales (7)", "Permissions.Showroom.View (All + Public); ManageModels (Admin); Inventories.ViewDealer & ManageDealer (Staff*, Admin); SalesOrders.Create & ViewDealer (Staff*, Admin); SalesOrders.ViewOwn (Customer)", "View, ViewOwn", "View, ViewDealer*, ManageDealer*, Create*", "All except ViewOwn"],
      ["Customer VIN Garage (4)", "Permissions.Vehicles.ViewOwn & RegisterOwn (Customer); Vehicles.ViewDealer (Staff*, Admin); Vehicles.ManageRules (Admin)", "ViewOwn, RegisterOwn", "ViewDealer*", "ViewDealer, ManageRules"],
      ["Service Booking (7)", "Permissions.TimeSlots.ViewAvailable (All); TimeSlots.ManageDealer (Staff*, Admin); Bookings.Create, CancelOwn, ViewOwn (Customer); Bookings.ViewDealer & UpdateStatus (Staff*, Admin)", "ViewAvailable, Create, CancelOwn, ViewOwn", "ViewAvailable, ManageDealer*, ViewDealer*, UpdateStatus*", "ViewAvailable, ManageDealer, ViewDealer, UpdateStatus"],
      ["Workshop Invoices (3)", "Permissions.Invoices.CreateDealer & ViewDealer (Staff*, Admin); Invoices.ViewOwn (Customer)", "ViewOwn", "CreateDealer*, ViewDealer*", "CreateDealer, ViewDealer"],
      ["Loyalty & Vouchers (6)", "Permissions.Loyalty.ViewOwn & RedeemVoucher (Customer); ViewDealerCustomer (Staff*, Admin); ManageTiers, ManageVoucherCatalog, AdjustPoints (Admin)", "ViewOwn, RedeemVoucher", "ViewDealerCustomer*", "ViewDealerCustomer, ManageTiers, ManageVoucherCatalog, AdjustPoints"],
      ["Notifications (2)", "Permissions.Notifications.ViewOwn (All); Notifications.ViewDealer (Staff*, Admin)", "ViewOwn", "ViewOwn, ViewDealer*", "ViewOwn, ViewDealer"],
      ["Platform & Analytics (5)", "Permissions.Dealers.ViewPublic (All + Public); Dealers.ManageAll, Users.ManageStaff, Analytics.ViewAllDealers, Analytics.ExportReports (Admin)", "ViewPublic", "ViewPublic", "All 5 Platform & Analytics Permissions"],
      ["Test-Drives & Parts (6)", "Permissions.TestDrives.Create, CancelOwn, ViewOwn (Customer); TestDrives.ViewDealer & UpdateStatus (Staff*, Admin); SpareParts.ManageCatalog (Admin)", "Create, CancelOwn, ViewOwn", "ViewDealer*, UpdateStatus*", "ViewDealer, UpdateStatus, ManageCatalog"],
      ["CSAT & Warranty (8)", "Permissions.Reviews.CreateOwn, ViewOwn & Warranty.CreateOwn, ViewOwn (Customer); Reviews.ViewDealer, RespondDealer & Warranty.ViewDealer, ManageDealer (Staff*, Admin)", "4 Own Policies", "4 Dealer* Policies", "4 Dealer Policies"],
    ],
    [1760, 3800, 1200, 1300, 1300]
  ),
  p("*Note: All DealerStaff permissions are additionally constrained at the data layer by IDealerScoped across 8 dealer-owned tables (DealerId == currentUser.DealerId). Customer self-service permissions (*.ViewOwn, *.CreateOwn, *.Create booking/test-drive, *.RedeemVoucher) are exclusive to Customer accounts for strict audit separation of duties (Customer=20, DealerStaff=22, Admin=32).", { size: 19, italics: true, color: COLOR_MUTED }),

  h2("5.2 REST API v1 Module Catalog (/api/v1/*)"),
  makeTable(
    ["Module Prefix", "Representative Endpoints & HTTP Verbs", "Primary Purpose & Guarding Policy"],
    [
      ["/api/v1/auth", "POST /register, /login, /refresh, /logout; GET /me", "Customer onboarding (+ Bronze LoyaltyAccount), JWT issuance, refresh rotation, and claim profile."],
      ["/api/v1/dealers", "GET /, /{id}; POST /; PUT /{id}; GET/POST /{id}/staff; PATCH /{id}/staff/{uid}/status", "Public dealer locator + Admin dealership governance & DealerStaff provisioning."],
      ["/api/v1/motorcycles", "GET /, /{id}, /{id}/maintenance-rules; POST/PUT models & rules", "Public showroom catalog & OEM maintenance schedules + Admin catalog/rule CRUD."],
      ["/api/v1/inventories", "GET /, POST /, PATCH /{inventoryId}", "Dealer-scoped 17-char VIN stock intake and RowVersion-guarded StockStatus updates."],
      ["/api/v1/sales-orders", "POST /, GET /my, GET /, GET /{orderId}, GET /{orderId}/receipt", "Atomic showroom bike sale -> VIN auto-registration in CustomerVehicles + Loyalty points + SHA-256 receipt."],
      ["/api/v1/vehicles", "GET /my, POST /, PATCH /{id}/mileage, GET /{id}/maintenance-history, GET /lookup", "Customer digital VIN garage, external VIN onboarding, monotonic odometer updates, staff VIN intake lookup."],
      ["/api/v1/time-slots", "GET /, GET /dealer, POST /dealer/generate, PATCH /{slotId}", "Customer open bay query (MaxCapacity - BookedCount) & dealer batch slot generation."],
      ["/api/v1/bookings", "POST /, GET /my, POST /{id}/cancel, GET /dealer, GET /{id}, PATCH /{id}/status", "RowVersion-locked slot booking, voucher reservation, cancellation release, and staff status workflow."],
      ["/api/v1/service-invoices", "POST /, GET /my, GET /, GET /{invoiceId}, GET /{invoiceId}/receipt", "1:1 itemized workshop settlement (ServiceInvoiceLineItems), voucher consumption, point accrual & SHA-256 receipt."],
      ["/api/v1/test-drives", "GET /slots, POST /, GET /my, POST /{id}/cancel, GET /, GET /{id}, PATCH /{id}/status", "Showroom test-drive 2-rider/model/60m slot scheduling, IDealerScoped workbench, demo VIN assignment & SignalR push."],
      ["/api/v1/spare-parts", "GET /, GET /{partId}, POST /, PUT /{partId}", "Public/Staff OEM SpareParts catalog filterable by CompatibleModelId + Admin catalog governance."],
      ["/api/v1/service-reviews", "POST /, GET /my, GET / (?rating&minRating), GET /by-invoice/{invoiceId}, PATCH /{reviewId}/response", "1:1 Paid ServiceInvoice 5-star CSAT review (+25 AvailablePoints ServiceReviewBonus), rating-filtered IDealerScoped feed & staff reply."],
      ["/api/v1/warranty-claims", "POST /, GET /my, GET /, GET /{claimId}, PATCH /{claimId}/status", "Factory 36m / 30,000 km VIN warranty claim filing, OEM CoveredSparePartId adjudication & SignalR status push."],
      ["/api/v1/loyalty", "GET /account, /ledger (?transactionType=), /tiers, /customers/{id}; PUT /tiers/{id}; POST /accounts/{id}/adjustments", "Wallet balance & tier progress, paginated immutable ledger (incl. ServiceReviewBonus), staff checkout lookup, Admin adjustments."],
      ["/api/v1/vouchers", "GET /catalog, POST/PUT /catalog, POST /redeem, GET /my", "Reward store browsing, atomic point-to-voucher redemption, and customer voucher wallet."],
      ["/api/v1/notifications", "GET /, PATCH /{id}/read, POST /read-all", "Durable unread notification feed recovery and read-state synchronization."],
      ["/api/v1/admin/analytics", "GET /overview (/kpis), /revenue-by-dealer, /bookings-by-dealer, /monthly-trends, /loyalty-distribution, /quality-warranty, /export (/export-csv)", "9 bindings: cross-dealer KPI summary, CSAT & warranty quality intelligence, Chart.js v4 datasets, and 17-column UTF-8 BOM CSV streaming."],
    ],
    [1960, 3500, 3900]
  ),

  h2("5.3 SignalR Real-Time Hub (/hubs/notifications) & AngularJS SPA Architecture"),
  bullet("Authenticated WebSocket Handshake & Group Isolation", "AngularJS SignalRClientService passes JWT via accessTokenFactory (/hubs/notifications?access_token=...). NotificationHub.OnConnectedAsync enrolls verified connections into user-{userId}, dealer-{dealerId} (from signed dealer_id claim for DealerStaff), and role-Admin with zero client-callable group join methods."),
  bullet("10 Strongly-Typed INotificationClient Events", "BookingCreated (-> dealer-{dealerId}, role-Admin), BookingStatusChanged (-> user-{customerId}, dealer-{dealerId}), TestDriveCreated (-> dealer-{dealerId}, role-Admin), TestDriveStatusChanged (-> user-{customerId}, dealer-{dealerId}), ServiceReviewSubmitted (-> dealer-{dealerId}, role-Admin), WarrantyClaimCreated (-> dealer-{dealerId}, role-Admin), WarrantyClaimStatusChanged (-> user-{customerId}, dealer-{dealerId}), MaintenanceReminderTriggered (-> user-{customerId}), LoyaltyPointsUpdated (-> user-{customerId}), and TierUpgraded (-> user-{customerId})."),
  bullet("Zero-Page-Refresh AngularJS Integration & Printable Receipts", "Callbacks execute inside $rootScope.$applyAsync() to prepend incoming service, test-drive, CSAT review, and warranty cards onto staff/customer screens instantly without browser reload, while <printable-receipt-modal> renders @media print A4/thermal receipts with SHA-256 verification seals."),
  bullet("Chart.js v4 Executive Visualizations", "app.admin.analytics and app.admin.qualityWarranty render responsive charts via <dealer-chart> (Showroom vs Service Revenue Stacked Bar, Monthly Revenue & Booking Multi-Line, Bookings by Status Doughnut, Loyalty Tier Doughnut, Dealer CSAT & Warranty Coverage Bar, and Defect Category Doughnut), calling chart.destroy() inside $onDestroy."),

  // ==========================================================================
  // SECTION 6: LOYALTY, CONCURRENCY & VIN REMINDER ALGORITHMS
  // ==========================================================================
  h1("6. Core Domain Algorithms: Loyalty Engine, Concurrency & VIN Reminders", true),

  h2("6.1 Canonical Loyalty Point Formula & 4-Tier Progression Schedule"),
  p("All customer spending across Showroom SalesOrders (FinalAmount) and Workshop ServiceInvoices (NetPaidAmount = GrossAmount - VoucherDiscountAmount) applies a deterministic two-step integer floor formula at 10,000 VND = 1 base point:"),
  calloutBox(
    "Canonical Loyalty Accrual Formula",
    "BasePoints = Math.Floor(NetPaidAmount / 10000m);  PointsEarned = (int)Math.Floor(BasePoints * CurrentTier.PointsMultiplier).  Example: 455,000 VND spent by a Gold member (1.50x) yields Floor(455,000 / 10,000) = 45 BasePoints -> Floor(45 * 1.50) = 67 PointsEarned."
  ),
  spacer(),
  makeTable(
    ["Tier Code", "Rank", "MinQualifyingPoints", "PointsMultiplier", "Effective Yield / 100,000 VND", "Progression & Non-Downgrade Rule"],
    [
      ["Bronze", "1", "0 pts", "1.00x", "10 pts", "Default tier provisioned automatically upon Customer registration."],
      ["Silver", "2", "500 pts", "1.20x", "12 pts", "Auto-promoted when LifetimeQualifyingPoints >= 500; unlocks Silver vouchers."],
      ["Gold", "3", "2,000 pts", "1.50x", "15 pts", "Auto-promoted when LifetimeQualifyingPoints >= 2,000; unlocks Gold labor discounts."],
      ["Platinum", "4", "5,000 pts", "2.00x", "20 pts", "Auto-promoted when LifetimeQualifyingPoints >= 5,000; double points & VIP rewards."],
    ],
    [1200, 700, 1660, 1400, 1700, 2700]
  ),
  bullet("Qualifying vs. Spendable Separation (+25 ServiceReviewBonus)", "SalesOrders and ServiceInvoices increment BOTH AvailablePoints and LifetimeQualifyingPoints. Post-service 5-star CSAT ServiceReviews (SERVICE_REVIEW:{InvoiceId}:BONUS) credit +25 AvailablePoints ONLY, and redeeming a voucher deducts AvailablePoints ONLY (-PointsCost), ensuring tier progression reflects qualifying spend exclusively and customers are never demoted for using rewards."),

  h2("6.2 Four Transactional Domain Workflows"),
  bullet("Flow 1 \u2014 Showroom Bike Sale (CreateSalesOrderCommandHandler)", "Verifies DealerInventories.StockStatus == 'Available' + RowVersion inside a single SQL transaction -> marks StockStatus = 'Sold' -> inserts Completed SalesOrder -> auto-inserts CustomerVehicles (17-char VIN + initial MaintenanceRules targets) -> appends PointLedgerEntries (SalesOrderEarn, IdempotencyKey) -> updates LoyaltyAccounts & evaluates tier upgrade -> commits & pushes SignalR LoyaltyPointsUpdated / TierUpgraded."),
  bullet("Flow 2 \u2014 Service Slot Booking (CreateServiceBookingCommandHandler)", "Verifies vehicle ownership & monotonic odometer -> checks ServiceTimeSlots (BookedCount < MaxCapacity) -> optionally locks Available CustomerVoucher as ReservedOnBooking (RowVersion) -> increments BookedCount + 1 guarded by ServiceTimeSlots.RowVersion (bounded retry if open bays remain; 409 SLOT_CAPACITY_EXHAUSTED if full) -> inserts ServiceBookings (Status = 'Pending') + Notifications -> commits -> pushes SignalR BookingCreated to dealer-{dealerId}."),
  bullet("Flow 3 \u2014 Workshop Invoice Settlement (IssueServiceInvoiceCommandHandler)", "Requires CheckedIn/InProgress booking -> validates & consumes ReservedOnBooking voucher (clamping discount to GrossAmount & MaxDiscountAmount; Status -> 'Used') -> inserts 1:1 ServiceInvoice -> updates CustomerVehicles (CurrentMileageKm, LastServiceMileageKm, LastServiceDateUtc, next due targets) & resolves open MaintenanceReminders -> appends PointLedgerEntries (ServiceInvoiceEarn) -> promotes tier if threshold crossed -> pushes BookingStatusChanged, LoyaltyPointsUpdated & TierUpgraded."),
  bullet("Flow 4 \u2014 Automated VIN Maintenance Reminder Worker (VinMaintenanceReminderWorker)", "Hosted BackgroundService sweeps active CustomerVehicles in batches using IX_CustomerVehicles_ReminderScan, evaluating CurrentMileageKm >= (NextDueMileageKm - LeadMileageKm) OR UtcNow >= (NextDueDateUtc - LeadTimeDays). Builds DeduplicationKey = '{VIN}:{MaintenanceRuleId}:{TargetMileageKm}' enforced by UQ_MaintenanceReminders_DedupKey, inserts MaintenanceReminders + Notifications, and pushes SignalR MaintenanceReminderTriggered to user-{customerId}."),

  // ==========================================================================
  // SECTION 7: ANTIGRAVITY .AGENTS/ SUITE & 9-SERVER MCP INTEGRATION
  // ==========================================================================
  h1("7. Antigravity .agents/ Automation Suite & Model Context Protocol (MCP) Integration", true),
  p("Phase 2 delivers a complete, zero-application-code developer and AI-agent engineering suite under .agents/ combining native Antigravity plugin/skill manifests, lifecycle safety hooks, cross-platform PowerShell/Bash/Node ESM verification scripts, 2 custom workspace Stdio MCP servers, and 7 global MCP server integrations."),

  h2("7.1 Workspace .agents/ Topology, Lifecycle Safety Hooks & Post-Task Sync Gate"),
  bullet("Discovery Manifests & Deduplicated Layout", ".agents/plugins.json registers .agents/plugins/motorcycle-dealer-suite (plugin.json, mcp_config.json, hooks.json, rules/AGENTS.md), while .agents/skills.json registers .agents/skills/ directly so all 9 workspace skills load exactly once with zero duplicate plugin skill collisions."),
  bullet("6 Scoped AGENTS.md Rulebooks & Standalone Hygiene", "Root AGENTS.md (< 24 KB), .agents/rules/AGENTS.md (< 12 KB), .agents/plugins/motorcycle-dealer-suite/rules/AGENTS.md (< 5 KB), backend/AGENTS.md (< 8 KB), frontend/AGENTS.md (< 6 KB), and mobile/AGENTS.md (< 8 KB) enforce zero YAML frontmatter, v1.3 27-table / 48-permission 3-way parity, and the Mandatory Post-Task Synchronization Gate."),
  bullet("SessionStart & PreToolUse Safety Hooks", "hooks.json runs node ./hooks/session-start.mjs at session startup to inject Phase 1..10 and Post-Task Sync context, and runs node ./hooks/safety-guard.mjs before every run_command tool call to deny destructive SQL (DROP DATABASE, TRUNCATE TABLE, DELETE/UPDATE against all 4 immutable tables: PointLedgerEntries, SalesOrders, ServiceInvoices, and ServiceInvoiceLineItems) and destructive git history rewrites."),
  bullet("Cross-Platform Automation, Android/iOS MAUI DevOps & 183-Day Git Shipper (.agents/scripts/)", "Includes verify-post-task-sync.mjs (100% docs/.docx/snapshot/3-way 48-RBAC/502-file git-plan parity), verify-mobile-maui.mjs (8 MAUI static guardrails incl. [MAUI-IOS-AND-TOOLKIT]), ensure-maui-android.ps1 (WHPX Pixel_8_API_35 AVD & adb reverse tcp:5080), smoke-mobile-android.ps1 (APK build, install, pidof, uiautomator & PNG screenshot), run-phase-quality-loop.ps1 (Phases 3..10 with JDK 21+ resolver), daily-git-shipper.{mjs,ps1}, git-6month-schedule-plan.json (502/502 files mapped across 183 days), audit-dealer-guardrails (.ps1/.sh), ensure-sqlserver-db (.ps1/.sh), smoke-fullstack (.ps1/.sh), verify-dealer-platform (.ps1/.sh), sync-progress.mjs (88 tasks), baseline-spec-snapshot.json, and build-spec-docx.mjs."),

  h2("7.2 Nine Bilingual (EN / VI) Workspace Skills (.agents/skills/*)"),
  makeTable(
    ["Skill Directory (.agents/skills/*)", "Bilingual EN / VI Trigger Intents", "Governed Architectural & MCP Scope"],
    [
      ["dotnet-clean-api", "Add API endpoint / Create command / Configure 48-RBAC | Tao API moi / Phan quyen 48 RBAC / ProblemDetails", ".NET 10 Clean Architecture layers, FluentValidation, [HasPermission] across 48 policies, 8-table IDealerScoped, context7 + guardrails + postman MCP."],
      ["sqlserver-efcore-dba", "Add EF Core migration / Tune index / Seed 27 tables | Tao migration SQL Server / Toi uu index / Du lieu seed", "MotorcycleDealerDb 27 3NF tables, Guid.CreateVersion7(), 8 RowVersion, 4 immutable tables, 31 workload indexes, sqlserver-localdb-inspector + guardrails MCP."],
      ["angularjs-enterprise-spa", "Build AngularJS component / 30-route guard / Chart.js | Tao giao dien AngularJS / Chan route / Danh gia & bao hanh", "Modular angular@1.8.3 .component(), 30 UI-Router guards, single-flight 401 refresh queue, printableReceiptModal, CSAT/warranty screens, Chart.js v4 $onDestroy."],
      ["dotnet-maui-mobile", "Build .NET 10 MAUI Android/iOS app / CommunityToolkit.Maui / SQLCipher | Tao app Android/iOS MAUI / Quet VIN QR / Gia lap ADB", "mobile/ net10.0-android;net10.0-ios (CommunityToolkit.Maui 13.0, 16 SourceGen x:DataType views, strict iOS ATS/Keychain), pure net10.0 Mobile.Core (FluentValidation, Polly v8 Http.Resilience, Humanizer, WeakReferenceMessenger), 9-table SQLCipher WAL, SingleFlightTokenRefreshHandler, 10-event SignalR & Pixel_8_API_35 ADB."],
      ["loyalty-booking-domain", "Slot, test-drive & warranty / CSAT +25 / Line-item invoice | Dat lich, lai thu, bao hanh / Danh gia +25 diem", "ISO 3779 VIN onboarding, ServiceTimeSlots, TestDriveBookings, ServiceReviews (+25 AvailablePoints) & WarrantyClaims (36m/30,000km), SpareParts, VIN worker."],
      ["signalr-realtime-hub", "Push booking, review & warranty alert / SignalR groups | Thong bao dat lich, danh gia & bao hanh realtime", "Strongly-typed NotificationHub (/hubs/notifications, 10 events), JWT claim groups (dealer-{id}, user-{id}, role-Admin), Web SPA + Android MAUI SignalR sync."],
      ["qa-verification-suite", "Write xUnit/integration tests / Race test / Sync gate | Viet unit/integration test / Kiem tra dong bo he thong", "RISK-01..06 verification, parallel slot/test-drive/CSAT/voucher races, 48-RBAC IDOR checks, verify-mobile-maui.mjs, smoke-fullstack & verify-post-task-sync.mjs."],
      ["docx-spec-generator", "Regenerate Word specification / Validate OpenXML | Xuat lai file dac ta Word .docx / Dong bo bao cao", "Zero-root-node_modules build-spec-docx.mjs execution via NODE_PATH + OpenXML validate.py schema verification + post-task sync check."],
      ["mcp-devtools-orchestrator", "Orchestrate MCP servers / Inspect LocalDB / E2E capture | Tan dung MCP / Kiem tra LocalDB Playwright Postman GitHub", "Master 9-server MCP playbook coordinating the 2 custom workspace Stdio MCP servers with all 7 global MCP developer integrations."],
    ],
    [2160, 2900, 4300]
  ),

  h2("7.3 Nine-Server Model Context Protocol (MCP) Ecosystem"),
  makeTable(
    ["MCP Server Identifier", "Scope / Transport", "Exposed Tools & Platform Engineering Workflow"],
    [
      ["motorcycle-dealer-guardrails", "Workspace Plugin (Stdio Node ESM)", "check_phase_gate (Phases 1..10), lookup_rbac_permission (48 policies), lookup_db_table_spec (27 tables), calculate_loyalty_preview (10k VND floor oracle), get_wbs_tasks (88 tasks)."],
      ["sqlserver-localdb-inspector", "Workspace Plugin (Stdio Node + SqlClient)", "sqlserver_status, list_sqlserver_tables, inspect_sqlserver_schema, query_readonly_sql against (localdb)\\MSSQLLocalDB (isolating SQL Server from Oracle dbhub)."],
      ["context7_context7", "Global MCP Server", "resolve-library-id & query-docs for authoritative .NET 10, EF Core 10, .NET MAUI, ASP.NET Core SignalR, AngularJS 1.8, @uirouter/angularjs, and Chart.js v4 APIs."],
      ["playwright", "Global MCP Server", "Multi-tab headless/headed browser automation proving Customer booking/test-drive/review/warranty in Tab 0 triggers instant Staff card in Tab 1 with zero reload."],
      ["chrome-devtools-plugin_chrome-devtools", "Global MCP Server", "WebSocket /hubs/notifications 10-event frame inspection, AngularJS $digest & Chart.js heap-snapshot leak verification (RISK-04), and Lighthouse audits."],
      ["postman_postman", "Global MCP Server", "Synchronizes /api/v1/* OpenAPI specs into Postman collections, 3-role JWT environments, and automated 48-permission ProblemDetails contract monitors."],
      ["github", "Global MCP Server", "Governs branches, traceable issues (T-101..T-1008), pull requests, and v1.0.0..v1.3.0 release publishing on github.com/quocanhh86/motorcycle-dealer."],
      ["figma_figma", "Global MCP Server", "Extracts design tokens, component layouts, and architecture/workflow diagrams across all 24 Web + 16 Android MAUI screen specs."],
      ["notion_notion", "Global MCP Server", "Synchronizes phase-gate executive summaries, ADR decision logs, and QA verification reports with external engineering documentation workspaces."],
    ],
    [2200, 1460, 5700]
  ),

  // ==========================================================================
  // SECTION 8: DEEP-DIVE BLUEPRINTS: 26-SCREEN UX, 6 MODULES & OPENAPI/QA SUITE
  // ==========================================================================
  h1("8. Deep-Dive Engineering Blueprints: 26-Screen UX, 6 Vertical Modules & OpenAPI/QA Suite", true),
  p("Tasks T-110 through T-112, Phase 8 (T-804..T-808), and Phase 9 (T-905..T-908) establish 100% implementation-verified specifications across all 23 Markdown/OpenAPI documents: OpenAPI 3.1.0 v1.2.0 contracts, 75 multi-level test cases (TC-001..TC-075), live demo operations, 26 primary role-governed UI/UX screens (+ 4 aliases = 30 navigable routes), and 6 Clean Architecture vertical feature slices."),

  h2("8.1 Complete 26-Screen UX & Guard Catalog (docs/screens/* & DD_04 v1.2.0)"),
  makeTable(
    ["Screen ID", "UI-Router State & Portal", "Role / Permission Guard", "AngularJS Component & Real-Time UX Contract"],
    [
      ["SCR-PUB-01", "app.public.showroom (+ detail, dealers)", "Public (+ Showroom.View, Dealers.ViewPublic)", "publicShowroom: Public catalog, OEM MaintenanceRules modal, live branch VIN stock, and [Book Test Drive] deep-link CTA."],
      ["SCR-PUB-02", "app.public.login & app.public.register", "Anonymous (redirects if authenticated)", "publicLogin & publicRegister: JWT + RefreshToken sign-in (preserving query-param returnUrl) and Bronze wallet registration."],
      ["SCR-PUB-03", "app.public.forbidden (/403)", "System RBAC Guard Boundary", "publicForbidden: Target of $transitions.onBefore when caller lacks required role or granular Permissions.<Module>.<Action>."],
      ["SCR-CUS-01", "app.customer.dashboard", "Customer (Vehicles/Bookings/Loyalty.ViewOwn)", "customerDashboard: Rider KPI summary, active booking status card, due VIN reminder banner, and tier progress strip."],
      ["SCR-CUS-02", "app.customer.garage (+ vehicleDetail)", "Customer (Vehicles.ViewOwn, RegisterOwn)", "customerGarage: Owned showroom & external ISO 3779 17-char VIN cards, monotonic odometer modal, and live MaintenanceReminderTriggered banner."],
      ["SCR-CUS-03", "app.customer.bookService (bookingNew)", "Customer (Bookings.Create, TimeSlots.ViewAvailable)", "customerBookingNew: Guided Dealer -> open ServiceTimeSlot -> VIN -> eligible CustomerVoucher wizard with 409 SLOT_CAPACITY_EXHAUSTED auto-refresh."],
      ["SCR-CUS-04", "app.customer.bookings (+ invoices)", "Customer (Bookings.ViewOwn, CancelOwn, Invoices.ViewOwn)", "customerBookings: Live SignalR BookingStatusChanged stepper, slot/voucher-releasing cancellation, itemized invoices, 5-star CSAT modal & printableReceiptModal."],
      ["SCR-CUS-05", "app.customer.loyalty (+ orders)", "Customer (Loyalty.ViewOwn, RedeemVoucher)", "customerLoyalty: Bronze..Platinum progress bar, append-only PointLedgerEntries table, VoucherCatalogs redemption & printable order receipts."],
      ["SCR-CUS-06", "app.customer.testDrives (?modelId&dealerId)", "Customer (TestDrives.ViewOwn, Create, CancelOwn)", "customerTestDrives (v1.1.0): 14-day showroom test-ride window picker, license validation, 409 recovery & live TestDriveStatusChanged tracker."],
      ["SCR-CUS-07", "app.customer.warranty (/customer/warranty)", "Customer (Warranty.ViewOwn|CreateOwn, Reviews.*)", "customerWarrantyReviews (v1.2.0): VIN 36m/30,000km eligibility badge, warranty claim filing, 5-star CSAT history (+25 pts) & live SignalR WarrantyClaimStatusChanged."],
      ["SCR-STF-01", "app.staff.workbench (+ checkin)", "DealerStaff* (Bookings.ViewDealer, UpdateStatus)", "staffWorkbench: Zero-refresh SignalR BookingCreated Kanban prepending cards via $rootScope.$applyAsync() without resetting filter/scroll state."],
      ["SCR-STF-02", "app.staff.invoicing (invoiceBuilder)", "DealerStaff* (Invoices.CreateDealer, ViewDealer)", "staffInvoiceBuilder: OEM SpareParts line-item picker, Labor + Parts settlement, voucher validation, points preview & printableReceiptModal."],
      ["SCR-STF-03", "app.staff.salesNew (salesDesk + inventory)", "DealerStaff* (SalesOrders.Create, Inventories.*)", "staffSalesDesk: 17-char VIN stock intake, atomic SalesOrder checkout onboarding CustomerVehicles + points & printableReceiptModal."],
      ["SCR-STF-04", "app.staff.slots (timeSlots)", "DealerStaff* (TimeSlots.ManageDealer)", "staffTimeSlots: Batch daily bay capacity generator and RowVersion-guarded MaxCapacity / IsBlocked slot controls."],
      ["SCR-STF-05", "app.staff.customers", "DealerStaff* (Vehicles.ViewDealer, ViewDealerCustomer)", "staffCustomers: Walk-in customer VIN service history, tier multiplier badge, and usable CustomerVouchers lookup."],
      ["SCR-STF-06", "app.staff.testDrives", "DealerStaff* (TestDrives.ViewDealer, UpdateStatus)", "staffTestDrives (v1.1.0): Zero-refresh SignalR TestDriveCreated workbench, demo VIN assignment & [Convert to Bike Sale] handoff."],
      ["SCR-STF-07", "app.staff.warrantyReviews", "DealerStaff* (Warranty.ViewDealer|ManageDealer, Reviews.*)", "staffWarrantyReviews (v1.2.0): Zero-refresh SignalR ServiceReviewSubmitted & WarrantyClaimCreated workbench, OEM CoveredSparePartId adjudication & staff CSAT reply."],
      ["SCR-ADM-01", "app.admin.analytics", "Admin (Analytics.ViewAllDealers, ExportReports)", "adminAnalytics: 4 leak-free Chart.js v4 charts ($onDestroy destroy()), KPI strip, dealer comparison table (incl. CSAT & Warranty column + Quality -> drill-down), and 17-column RFC 4180 CSV export."],
      ["SCR-ADM-02", "app.admin.dealers", "Admin (Dealers.ManageAll, ViewPublic)", "adminDealers: Nationwide dealership branch creation, operating hours, default bay capacities, and soft-deactivation."],
      ["SCR-ADM-03", "app.admin.staff", "Admin (Users.ManageStaff)", "adminStaff: Provision DealerStaff accounts strictly bound to a single DealerId and lock/unlock staff access."],
      ["SCR-ADM-04", "app.admin.models (+ maintenanceRules)", "Admin (Showroom.ManageModels, Vehicles.ManageRules)", "adminModels: Master MotorcycleModels catalog pricing/specs and OEM IntervalKm / IntervalMonths rules."],
      ["SCR-ADM-05", "app.admin.loyaltyTiers (+ pointAdjustments)", "Admin (Loyalty.ManageTiers, AdjustPoints)", "adminLoyaltyTiers: 4-tier perks metadata studio and audited goodwill/dispute PointLedgerEntries adjustments."],
      ["SCR-ADM-06", "app.admin.voucherCatalog", "Admin (Loyalty.ManageVoucherCatalog)", "adminVoucherCatalog: PointsCost, FixedAmount/Percentage discount caps, MinOrderAmount, MinTierId, and QuotaTotal governance."],
      ["SCR-ADM-07", "app.admin.spareParts", "Admin (SpareParts.ManageCatalog)", "adminSpareParts (v1.1.0): OEM SpareParts SKU catalog studio (category, model fitment, unit pricing & active lifecycle)."],
      ["SCR-ADM-08", "app.admin.qualityWarranty", "Admin (Analytics.ViewAllDealers)", "adminQualityWarranty (v1.2.0): Cross-dealer CSAT & Warranty Coverage Chart.js v4 studio (Dealer CSAT & Covered VND Bar + Defect Category Doughnut, $onDestroy teardown)."],
    ],
    [1160, 2100, 1900, 4200]
  ),

  h2("8.2 Six Clean Architecture Vertical Module Blueprints (docs/modules/MOD_01..MOD_06)"),
  makeTable(
    ["Module ID", "Blueprint & Phase Scope", "SQL Server 3NF Tables", "Clean Architecture Vertical Slice & Key Invariants"],
    [
      ["MOD-01", "Auth & Granular RBAC (Phases 4, 8 & 9)", "Users, Roles, Permissions, UserRoles, RolePermissions, RefreshTokens, Dealers", "15m HMAC-SHA256 JWT + SHA-256 rotating RefreshToken family breach revocation; 48 PermissionPolicyProvider policies; 8-table IDealerScoped Global Query Filter; AngularJS single-flight 401 refresh queue."],
      ["MOD-02", "Showroom, VIN Garage, Test-Drives & Warranty (Phases 5, 8 & 9)", "Dealers, MotorcycleModels, DealerInventories, SalesOrders, TestDriveBookings, CustomerVehicles, WarrantyClaims, MaintenanceRules", "ISO 3779 17-char Vin (no I/O/Q); RowVersion VIN stock lock; 5-step ACID SalesOrder + SHA-256 receipt; 2-rider/model/60m TestDriveBookings; 36m/30,000km WarrantyClaims + OEM part adjudication."],
      ["MOD-03", "Booking, CSAT Reviews & VIN Reminders (Phases 5 & 9)", "ServiceTimeSlots, ServiceBookings, ServiceReviews, MaintenanceRules, MaintenanceReminders, CustomerVouchers", "RowVersion capacity lock (BookedCount <= MaxCapacity); atomic voucher reservation/cancel release; 1:1 Paid ServiceInvoice ServiceReviews & staff replies; VinMaintenanceReminderWorker with DeduplicationKey."],
      ["MOD-04", "Loyalty, SpareParts, Invoices & Vouchers (Phases 6, 8 & 9)", "SpareParts, ServiceInvoices, ServiceInvoiceLineItems, ServiceReviews, LoyaltyTiers, LoyaltyAccounts, PointLedgerEntries, VoucherCatalogs, CustomerVouchers", "1:1 itemized ServiceInvoices + immutable ServiceInvoiceLineItems + SHA-256 receipt; +25 AvailablePoints ServiceReviewBonus (LifetimeQualifyingPoints unchanged); AppendOnlyLedgerInterceptor + Floor math."],
      ["MOD-05", "SignalR Real-Time Hub (Phases 6, 8 & 9)", "Notifications (+ post-commit triggers from Bookings, TestDrives, Reviews, Warranty, Reminders, Loyalty)", "NotificationHub : Hub<INotificationClient> at /hubs/notifications; server-derived JWT groups (user-{id}, dealer-{id}, role-Admin); 10 strongly-typed push events; AngularJS auto-reconnect + $applyAsync()."],
      ["MOD-06", "Admin Chart.js Analytics (Phases 7 & 9)", "Read projections over Dealers, SalesOrders, ServiceInvoices, ServiceBookings, ServiceReviews, WarrantyClaims, LoyaltyAccounts, LoyaltyTiers", "Admin-only IgnoreQueryFilters() + AsNoTracking() SQL projections hitting covering & quality indexes; zero-fill YYYY-MM buckets; CSAT & warranty breakdown; CSV sanitization; leak-free Chart.js v4."],
    ],
    [1060, 2200, 2000, 4100]
  ),

  h2("8.3 OpenAPI 3.1.0 Contract, 75-Case 8-Level QA Plan & Live Operations Runbook"),
  makeTable(
    ["Specification Artifact", "Coverage / Scale", "Engineering Contract & Verification Scope"],
    [
      ["docs/openapi.v1.yaml", "v1.2.1: 75 Paths / 10 Webhooks / 48 Policies", "Machine-readable OpenAPI 3.1.0 specification covering all 18 /api/v1/* controller modules (+ /service-reviews ?rating&minRating, /warranty-claims, /quality-warranty, 17-col CSV) + 10 SignalR webhooks, BearerAuth JWT, 48/48 canonical permissions, and RFC 7807 ProblemDetails."],
      ["docs/TEST_PLAN.md (Levels 1-2)", "TC-001..TC-018 + Cross-Phase Units", "Level 1 Clean Architecture inward-dependency, zero-secrets, and 4-table ledger/order/invoice/line-item immutability audits + Level 2 xUnit [Theory] matrices (645 unit tests) for ISO 3779 VINs, loyalty math, +25 CSAT bonus, 36m/30,000km warranty & SHA-256 hashes."],
      ["docs/TEST_PLAN.md (Levels 3-4)", "TC-019..TC-043 + SQL Races & RBAC", "Level 3 real SQL Server parallel Task.WhenAll slot, test-drive & CSAT review races, demo VIN overlap/sale lock, ledger trigger, and worker dedup tests (82 LocalDB integration tests) + Level 4 48-permission sweep, OWASP API1-10, and 8-table cross-dealer IDOR isolation."],
      ["docs/TEST_PLAN.md (Levels 5-8)", "TC-044..TC-075 + SMK-01..SMK-10", "Dual-tab Headless Edge CDP E2E proving zero-refresh BookingCreated, TestDriveCreated, ServiceReviewSubmitted & WarrantyClaim* delivery + 30/30 UI-Router route sweeps (0 console.error) + 64/64 SPA checks + SMK-01..SMK-10 live HTTP/RBAC + verify-post-task-sync.mjs gate."],
      ["docs/RUNBOOK.md & RELEASE_NOTES", "v1.0.0..v1.2.0 Live Operations", "Complete .NET 10 / SQL Server LocalDB & 2022 Docker / AngularJS 1.8 boot procedures, 3 seeded dealers, 8 role/tier credentials, verify-post-task-sync.mjs & daily-git-shipper.mjs (StopOnIdleEnd=False), and v1.0.0, v1.1.0 & v1.2.0 release notes."],
      ["docs/basic-design/BD_00_BASIC_DESIGN_MASTER.md", "System Basic Design Master (BD-00 v1.2)", "Unified system topology, Clean Architecture ADR-001 dependency matrix, MOD-01..06 boundaries, 35-state / 30-route UI-Router catalog, 97-endpoint REST + 10-event SignalR catalog, 27-table 3NF overview, and 48-permission RBAC matrix."],
      ["docs/detail-design/DD_01..DD_04_*.md", "4-Doc Detail Design Suite (v1.3.0)", "100% empirical source-code parity across DD_01 Common (+Mobile.Core helpers), DD_02 DB & TX-01..31 (+9 local SQLCipher tables), DD_03 APIs & Hub (+Mobile HTTP/SignalR nuances), and DD_04 Screens (+16 compiled Android MAUI views)."],
      ["docs/screens/MOBILE_MAUI_SCREENS.md & RELEASE_NOTES_v1.3.0.md", "16 Native Android & iOS MAUI Screens (v1.3.3)", "Role-Adaptive AppShell (Public 3 tabs, Customer 5 tabs, DealerStaff 5 tabs + Invoice Builder, Admin Web HQ notice), CommunityToolkit.Maui 13.0 (StatusBarBehavior, Expander, TouchBehavior, EmailValidationBehavior, Toast/Snackbar), Motorsport Light/Dark Pit-Lane tokens, 48dp ergonomics, ZXing Camera2/AVFoundation VIN/QR, and offline QrCodeMatrixEncoder."],
    ],
    [2160, 1700, 5500]
  ),

  h2("8.4 Native Android & iOS .NET 10 MAUI Field App Screen Catalog (16 Compiled x:DataType Views)"),
  makeTable(
    ["Role Mode / Tabs", "Screen IDs", "MAUI Views & Shell Routes (MobileRoutes)", "Key Field, CommunityToolkit.Maui 13.0, SQLCipher & Hardware Capabilities"],
    [
      ["Public (3 Tabs) + Admin Gate", "SCR-MOB-PUB-01..04", "ShowroomCatalogPage, DealerLocatorPage, LoginRegisterPage, AdminWebConsoleNoticePage", "Zero-Mock real-data UI; Humanizer dynamic counts + toolkit:Expander OEM/branch stock; collapsed toolkit:Expander endpoint panel (10.0.2.2 Android / localhost ADB & iOS Simulator) + EmailValidationBehavior + FluentValidation + native Toast; offline CachedMotorcycleModel & CachedDealer; Admin Web HQ redirect."],
      ["Customer (5 Tabs + Bell)", "SCR-MOB-CUS-01..06", "CustomerHomePage, VinGaragePage, BookingWizardPage, LoyaltyPassbookPage, CustomerWarrantyPage, NotificationsFeedPage", "Live /loyalty/tiers metallic LoyaltyPassbookCardView + toolkit:TouchBehavior; offline QrCodeDisplayView (dealer://voucher|booking|receipt/v1); Camera2/AVFoundation 17-char VIN scan; 409 slot reload; +25 pts CSAT; WeakReferenceMessenger sync; infinite-TTL CachedReceiptSnapshot."],
      ["DealerStaff (5 Tabs + Push + Bell)", "SCR-MOB-STF-01..06", "StaffWorkbenchPage, MobileInvoiceBuilderPage, ShowroomVinSalesPage, StaffTestDrivePage, StaffWarrantyCsatPage, SlotCapacityPage", "IDealerScoped + OwnerScopeKey isolation; SwipeView bay check-in; Camera2/AVFoundation QR/VIN scanner; OEM SpareParts invoice builder + live two-step floor preview; Polly v8 idempotent resilience; 10-event SignalR + Android NotificationChannels / iOS UNUserNotificationCenter."],
    ],
    [1560, 1400, 2700, 3700]
  ),

  // ==========================================================================
  // SECTION 9: 10-PHASE WBS ROADMAP & PRE-MORTEM RISK REGISTER
  // ==========================================================================
  h1("9. Master 10-Phase WBS Engineering Roadmap & Risk Governance", true),
  p("Development is partitioned into 88 atomic, verifiable tasks (T-101 through T-1008) across ten sequential engineering phases, with Phases 3 through 10 governed by the mandatory 7-Stage Quality Loop (5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test) and the permanent Mandatory Post-Task Synchronization Gate (.agents/scripts/verify-post-task-sync.mjs). All 10 Phases (Phase 1 through Phase 10) are complete and signed off (88 / 88 tasks = 100.0% \u2014 v1.3.3 Multi-Platform Android & iOS MAUI + Enterprise Libraries Sync Edition)."),

  h2("9.1 Master 10-Phase Gate & WBS Summary"),
  makeTable(
    ["Phase & Status", "Task Range", "Count", "Milestone Deliverables", "Mandatory Exit Gate"],
    [
      ["Phase 1 (Signed Off)", "T-101..T-112", "12", "Core specs (PRD, ARCH, DB, DESIGN, WBS, AGENTS, README), OpenAPI 3.1, TEST_PLAN, RUNBOOK, 18-screen UX (docs/screens/*), 6 modules (MOD_01..06), Master .docx", "Stage 3 Reader Test + OpenAPI/UX/Module QA + OpenXML validate.py pass + User Sign-off (12/12 Complete)"],
      ["Phase 2 (Signed Off)", "T-201..T-208", "8", "Antigravity .agents/ plugin (motorcycle-dealer-suite), safety hooks, rules, 8 bilingual EN/VI skills, 17 scripts/hooks, 2 Stdio MCP + 7 global MCP integrations", "JSON/YAML schema checks + MCP JSON-RPC handshakes + PowerShell/Node dry-runs pass (8/8 Complete)"],
      ["Phase 3 (Signed Off)", "T-301..T-308", "8", "backend/MotorcycleDealer.sln (.NET 10 4-layer Clean Arch + 89/89 xUnit tests), frontend/src AngularJS 1.8.3 shell (21/21 checks), non-root Dockerfiles & docker-compose.yml", "15R + 6T loop green; Release --warnaserror (0 warnings) + 89/89 xUnit + live HTTP/SignalR/CDP smoke + Docker Compose valid (8/8 Complete)"],
      ["Phase 4 (Signed Off)", "T-401..T-409", "9", "22 EF Core 3NF configurations (UseCompatibilityLevel(150)), 23 covering/filtered indexes, IDealerScoped filters, LocalDB migration & 3-dealer seeder, rotating JWT/RefreshTokens, 34-permission handler, live SPA auth & Admin Dealers/Staff", "15R + 6T loop green; 22/22 LocalDB 15.0 tables + 5/5 reconciled loyalty ledgers + 137/137 xUnit + 28/28 SPA + live multi-role Edge CDP E2E (9/9 Complete)"],
      ["Phase 5 (Signed Off)", "T-501..T-510", "10", "MotorcycleModels & OEM rules, IDealerScoped VIN stock, atomic SalesOrders -> CustomerVehicles + points, external VIN garage, RowVersion ServiceTimeSlots & ServiceBookings, VinMaintenanceReminderWorker, 10 live AngularJS views", "15R + 6T + Cross-Phase P4+P5 loop green; 10-way parallel slot race + 3-cycle reminder dedup + 299/299 xUnit + 38/38 SPA + Dual-Page SignalR E2E (10/10 Complete)"],
      ["Phase 6 (Signed Off)", "T-601..T-609", "9", "LoyaltyPointsCalculator, append-only PointLedgerEntries & 4-tier engine, ServiceInvoices settlement, VoucherCatalogs & CustomerVouchers 10-way race lock, /api/v1/notifications & SignalR /hubs/notifications, live Invoice Builder, Loyalty Store & Bell", "15R + 6T loop green; 335/335 xUnit (266 unit + 69 LocalDB integration, 0 flaky) + 43/43 SPA + Dual-Page Silver->Gold SignalR E2E + VoucherQuotaDriftCount=0 (9/9 Complete)"],
      ["Phase 7 (Signed Off)", "T-701..T-708", "8", "AdminAnalyticsController (all 8 bindings + OWASP UTF-8 BOM CSV export), live Chart.js v4 <dealer-chart> dashboard & governance polish, AuthRateLimitOptions + OWASP API1-10 audit, SMK-01..08 smoke, v1.0.0 release notes & 183-day git schedule", "15R + 6T loop green; 417/417 xUnit (340 unit + 77 LocalDB integration, 0 flaky) + 48/48 SPA + Closed-Loop Edge CDP Chart.js & CSV E2E + .docx v1.0.0 (8/8 Complete)"],
      ["Phase 8 (Signed Off)", "T-801..T-808", "8", "v1.1.0 TestDriveBookings, SpareParts & ServiceInvoiceLineItems (25 tables, 40 permissions), /api/v1/test-drives + SignalR, /api/v1/spare-parts & SHA-256 /receipt, SCR-CUS-06/STF-06/ADM-07 & printableReceiptModal, StopOnIdleEnd=False, BD/DD/DOCX v1.1.0", "15R + 6T loop green; 541/541 xUnit (461 unit + 80 LocalDB integration, 0 flaky) + 56/56 SPA + SMK-01..09 + 27/27 CDP routes + 25/25 SQL tables drift=0 (8/8 Complete)"],
      ["Phase 9 (Signed Off)", "T-901..T-908", "8", "v1.2.0/v1.2.1 ServiceReviews (+25 AvailablePoints) & WarrantyClaims (27 tables, 48 permissions, 10 SignalR events), /api/v1/service-reviews, /warranty-claims & /quality-warranty, 17-col CSV, SCR-CUS-07/STF-07/ADM-08", "15R + 6T + Cross-Phase P1..P9 loop green; 727/727 xUnit (645 unit + 82 LocalDB) + 64/64 SPA + SMK-01..10 + 30/30 CDP routes + 27/27 SQL tables drift=0 (8/8 Complete)"],
      ["Phase 10 (Signed Off)", "T-1001..T-1008", "8", "v1.3.3 Native C# .NET 10 MAUI Android & iOS Field App (mobile/: Mobile.Core net10.0 [FluentValidation, Polly v8 Http.Resilience, Humanizer, WeakReferenceMessenger] + Mobile.App net10.0-android;net10.0-ios [CommunityToolkit.Maui 13.0, 16 views] + 103 Mobile.UnitTests), 9-table SQLCipher WAL, Camera2/AVFoundation VIN/QR, 10-event SignalR, skill #9, mobile/AGENTS.md, DOCX v1.3.3 & 502-file sync", "15R + 6T loop green; 830/830 xUnit (727 Backend + 103 Mobile) + 8/8 verify-mobile-maui.mjs + Android APKs (92.50 MB Debug / 69.31 MB Release) + iOS net10.0-ios Debug & Release 0 warnings + Zero-Mock Pixel_8_API_35 proof + 502/502 git sync (8/8 Complete)"],
    ],
    [1400, 1200, 660, 3600, 2500]
  ),

  h2("9.2 Pre-Mortem Engineering Risk & Mitigation Register"),
  makeTable(
    ["Risk ID", "Failure Mode", "Severity", "Locked Architectural Mitigation"],
    [
      ["RISK-01", "Solo-Developer Scope & Spec Drift", "High", "Strict 9-Phase Gate Rule in AGENTS.md + Mandatory Post-Task Synchronization Gate (verify-post-task-sync.mjs); 80 atomic WBS tasks (T-101..T-908) & 100% 370-file git plan parity."],
      ["RISK-02", "Workshop Slot Double-Booking Race", "Critical", "ServiceTimeSlots.RowVersion + CHECK (BookedCount <= MaxCapacity) + ACID transaction returning 409 Conflict ProblemDetails."],
      ["RISK-03", "Loyalty Point Ledger Drift / Double Credit", "Critical", "Append-only PointLedgerEntries + UQ_PointLedgerEntries_IdempotencyKey + mutation-blocking SQL trigger + LoyaltyAccounts.RowVersion."],
      ["RISK-04", "AngularJS Digest Storms & Canvas Leaks", "High", "Singleton SignalRClientService using $rootScope.$applyAsync() + mandatory $onDestroy cleanup for hub listeners and Chart.js instances."],
      ["RISK-05", "Cross-Dealer Data Leakage (BOLA / IDOR)", "Critical", "8-table IDealerScoped EF Core Global Query Filter for DealerStaff + server-derived dealer-{dealerId} SignalR group enrollment from signed JWT."],
      ["RISK-06", "Duplicate Automated VIN Reminder Spam", "Medium", "Deterministic DeduplicationKey ({VIN}:{MaintenanceRuleId}:{TargetMileageKm}) enforced by UQ_MaintenanceReminders_DedupKey."],
    ],
    [1060, 2400, 1000, 4900]
  ),

  h2("9.3 Phase 4 Mandatory 7-Stage Quality Loop Verification Ledger (v1.4.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (SQL Server 2019 LocalDB 15.0.4382.1)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "12 Caught", "Audited Clean Arch + DI cohesion, 22-table 3NF + Compat 150, PBKDF2 + RCSI RefreshToken race + ActivePrincipalMiddleware + IDealerScoped, ledger trigger + SPA 401 queue, and MCP LocalDB inspector."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "3 / 3 PASS", "99,133 ms total: 131/131 xUnit (0 warnings), 27/27 SPA checks, live multi-role CDP E2E (SignalR=Connected), 22/22 LocalDB tables & 5/5 ledger sums verified."],
      ["Stage 3 (Fix)", "Surgical Remediation", "12 / 12 Fixed", "Unified AddInfrastructure DI, added ActivePrincipalMiddleware + NotificationHub lockout check, bool? IsActive guard, natural-key IgnoreQueryFilters seeder, and atomic ExecuteUpdateAsync rotation."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified all remediations across all 5 lenses with zero contract drift; scoped clean-code refactoring targets across seeder, auth, DI, SPA, and quality-loop scripts."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Memoized ActivePrincipalMiddleware per-request check, preserved stackalloc + ZeroMemory PBKDF2 & Lazy<string> seed hashes, and expanded suites to 137/137 xUnit + 28/28 SPA."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, 0 architecture leaks, 34/34 permissions, 23/23 B-tree indexes, and all 5 AGENTS.md rulebooks within scoped byte caps."],
      ["Stage 7 (3T)", "T2.1..T2.3 Round 3 Gate", "3 / 3 PASS", "92,735 ms total: 137/137 xUnit (113 unit + 24 LocalDB integration, 0 flaky), 28/28 SPA checks, interactive Edge CDP E2E (refreshCalls=1/1, 0 console errors), 254 files compliant, 22/22 tables."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.4 Phase 5 Mandatory 7-Stage Quality Loop Verification Ledger (v1.5.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (Showroom, Garage, Booking & Reminder Worker)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "9 Caught", "Audited ADR-001 purity, OpenAPI 3.1 & DTO parity, 34-permission RBAC & IDealerScoped, CreateExecutionStrategy ACID + RowVersion + ledger math + SignalR post-commit + $onDestroy; surfaced F-SG-01..03, F-BW-01..03, F-FE-01..03."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "3 / 3 PASS", "125,349 ms total: 146/146 xUnit (0 warnings), 32/32 SPA checks, Dual-Page Edge CDP SignalR E2E (13ms post-201 DOM render, 0 reloads, 24/24 states 0 console.error), 286 files compliant, 5/5 ledgers drift=0, overbookedSlots=0."],
      ["Stage 3 (Fix)", "Surgical Remediation", "9 / 9 Fixed", "Resolved historical SalesOrder ledger hydration, GET /inventories 401/403 + public EngineNumber masking, IgnoreQueryFilters plate check, 8-byte RowVersion Base64, MAJOR_12000 milestone tie-break, cancel minLength:3, and $q promises."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified all 9 fixes with zero regressions across all 5 lenses; scoped 7 clean-code refactoring & boundary [Theory] targets (RF-SG-01..02, RF-BW-01..02, RF-FE-01..02, RF-OPS-01)."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "AsNoTracking() OrderLedgerSnapshot structs, IX_CustomerVehicles_ReminderScan index ordering without MotorcycleModels JOINs, unified LoyaltyCalculator.determineTier & $q promises, +129 xUnit tests (146->275) & +2 SPA suites (32->34)."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, 0 architecture/RBAC/concurrency regressions, 0 orphan processes on :5080/:4200, and all 5 AGENTS.md files within byte caps (23,399 / 11,808 / 4,742 / 7,392 / 5,890 B)."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "114,481 ms total: 275/275 xUnit (232 unit + 43 LocalDB integration, 0 flaky), 34/34 SPA checks, Dual-Page Edge CDP SignalR E2E (19ms post-201 / 318ms submit->DOM, 0 leaks, 24/24 states 0 console.error), 288 files compliant, drift=0."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.5 Phase 6 Mandatory 7-Stage Quality Loop Verification Ledger (v1.6.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (ServiceInvoices, Loyalty, Vouchers & SignalR)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "7 Caught", "Audited ADR-001 purity (0 Phase shims), OpenAPI 3.1 & SPA parity, 34-permission RBAC & !IsDeleted, ADR-003 ledger/quota invariants + RowVersion + post-commit SignalR; caught F-P6-SEED-01, F-P6-LOY-01..03, F-P6-INV-01..02, F-P6-SPA-01a/b."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "3 Blockers Caught", "T1.1: 0 warnings, 307/307 xUnit PASS; T1.2: 41/41 SPA + Dual-Page Edge CDP Closed-Loop E2E PASS; T1.3: reproduced F-P6-SEED-01 (VoucherQuotaDriftCount=5), F-P6-LOY-01 ('Q' in suffix), and F-P6-LOY-03 (LongPolling timeout under parallel Debug load)."],
      ["Stage 3 (Fix)", "Surgical Remediation", "7 / 7 Fixed", "Reconciled DatabaseSeeder QuotaRedeemed == COUNT(CustomerVouchers) (healed MotorcycleDealerDb to VoucherQuotaDriftCount=0), fixed 33-char no-I/O/Q alphabet, !u.IsDeleted parity, in-memory WebSocket HubConnection, qualifying-only invoice reconstruction, scoped read-all, and SPA mid-edit preservation."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified all 7 fixes clean across all 5 lenses with zero contract drift; scoped clean-code refactoring & [Theory] boundary targets across Domain, Infrastructure, Integration Tests, and AngularJS SPA."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Promoted VoucherCodeSuffixAlphabet & CalculateVoucherDiscount onto Domain LoyaltyPointsCalculator, direct SQL .Select() ledger projection, ApplyInvoiceReadIncludes + ComputeNextMaintenanceSchedule, formatDiscountSummary, +28 xUnit tests (307->335) & +2 SPA suites (41->43)."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 warnings, 0 Phase shims, ADR-001 inward purity (7/7 arch tests), 100% OpenAPI 3.1 & 34-permission RBAC parity, git rev-list --count HEAD == 1 (d5c4a4f), and all 5 AGENTS.md files under scoped byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "147,952 ms total: 335/335 xUnit (266 unit + 69 LocalDB integration, 0 flaky across 1,005/1,005 runs), 43/43 SPA checks, Dual-Page Edge CDP Closed-Loop Silver->Gold SignalR E2E (0 console.error across 24/24 states), 22/22 tables, Ledger/Slot/VoucherQuota drift=0."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.6 Phase 7 Mandatory 7-Stage Quality Loop & OWASP API Top 10 Verification Ledger (v1.0.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (Admin Chart.js Analytics, CSV, OWASP & Release)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "6 Caught", "Audited ADR-001 purity, MOD-06/DD_03/DD_04 & Chart.js v4 parity, ADR-002 RBAC & OWASP API1-10, covering indexes & RISK-04 canvas safety; surfaced F-P7-CORS-01, F-P7-ANL-01, F-P7-SEC-01, F-P7-FE-01..02, and F-P7-OPS-01."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "2 Live Caught", "192,956 ms total: T1.1 0 warnings & 389/389 xUnit PASS; T1.2 46/46 SPA PASS + caught F-P7-OPS-01 PS 5.1 GET body coercion & F-P7-CORS-01 Content-Disposition exposure; T1.3 MCP LocalDB/Docker/determinism PASS."],
      ["Stage 3 (Fix)", "Surgical Remediation", "6 / 6 Fixed", "Added WithExposedHeaders(Content-Disposition, Retry-After, Location), EF.Functions.AtTimeZone(UTC) monthly grouping, 80-char rate-limit partition cap, LAST_12M exact 12-month span + inactive branch badge, and PS 5.1 GET guard (390/390 xUnit, 47/47 SPA)."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified all 6 fixes clean across all 5 lenses with zero contract drift; scoped clean-code refactoring & [Theory] boundary targets (RF-P7-BE-01..02, RF-P7-FE-01..02, RF-P7-OPS-01)."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Extracted BuildCompletedSalesQuery/BuildPaidInvoicesQuery/BuildScopedBookingsQuery, centralized AuthRateLimitOptions headers, added +27 [Theory] tests (390->417 xUnit), extracted ANALYTICS_PALETTE + sanitizeCsvBasename + suite (v) (47->48 SPA), and verified all 8 analytics routes in SMK-06."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, 0 FromSqlRaw concatenation, 0 Chart.js v2 xAxes/yAxes, OWASP API1-API10 compliance, git rev-list --count HEAD == 2, and all 5 AGENTS.md files under scoped byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "207,068 ms total: 417/417 xUnit (340 unit + 77 LocalDB integration; 1,251/1,251 runs 0 flaky), 48/48 SPA checks, SMK-01..08 PASS, Closed-Loop Edge CDP (+86M Showroom & +700k Service exact Chart.js deltas, 4->0->4->0 canvas teardown, UTF-8 BOM CSV, 24/24 states 0 errors), 22/22 tables drift=0."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.7 Phase 8 Mandatory 7-Stage Quality Loop Verification Ledger (v1.1.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (Test-Drives, SpareParts, Itemized Invoices & Receipts)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "4 Caught", "Audited ADR-001 purity, 25-table 3NF & 40-permission RBAC, IDealerScoped & SHA-256 receipts, RowVersion + Serializable test-drive locking + ServiceInvoiceLineItems immutability, and SPA query-param login redirect & $onDestroy hygiene."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "3 / 3 PASS", "210,126 ms total: 486/486 xUnit (0 warnings), 54/54 SPA checks, SMK-01..SMK-09 PASS, Dual-Page Edge CDP SignalR E2E + 27/27 routes (0 console.error), 25/25 SQL tables & InvoiceLineItemsDriftCount=0."],
      ["Stage 3 (Fix)", "Surgical Remediation", "4 / 4 Fixed", "Enforced DealerId/ModelId match on assigned test-drive VINs, preserved /customer/test-drives?modelId&dealerId across /login via AuthService.resolvePostLoginTarget, scoped Escape keydown on printableReceiptModal, and added +5 real SQL Server integration tests (486->491)."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified all 4 Stage 1-3 fixes clean across all 5 lenses with zero contract drift; scoped clean-code refactoring & [Theory] boundary targets across Application, Infrastructure, SPA, and Unit Tests."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Centralized document numbering & SHA-256 receipt hashing on LoyaltyPointsCalculator, extracted active dealer/model lookups, added +50 [Theory] boundary tests (491->541 xUnit) & +2 SPA suites (54->56)."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, ADR-001 inward purity, 40/40 RBAC permissions, StopOnIdleEnd=False Task Scheduler resilience, git rev-list --count HEAD == 2, and all 5 AGENTS.md files under scoped byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "210,766 ms total: 541/541 xUnit (461 unit + 80 LocalDB integration; 1,623/1,623 runs 0 flaky, StrayTestDbCount=0), 56/56 SPA checks, SMK-01..09 PASS, 27/27 CDP routes (0 console.error), 25/25 SQL tables (spareParts=8, testDrives=4, lineItems=13, drift=0)."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.8 Phase 9 Mandatory 7-Stage Quality Loop Verification Ledger (v1.2.0)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (CSAT Reviews +25 Bonus Points, VIN Warranty Claims & SignalR)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "1 Caught", "Audited ADR-001 purity, 27-table 3NF & 48-permission RBAC, 8-table IDealerScoped & OpenAPI 3.1 parity, 8-table RowVersion + +25 ServiceReviewBonus spendable-only invariant, and SPA $onDestroy hygiene; caught F-P9-SPA-01 (pre-selected invoiceId binding)."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "3 / 3 PASS", "638/638 xUnit (0 warnings, StrayTestDbCount=0), 60/60 SPA checks, SMK-01..SMK-10 PASS, Dual-Page Edge CDP Closed-Loop CSAT +25 & Warranty SignalR E2E (0 reloads, 30/30 routes 0 console.error), 27/27 SQL tables & ReviewBonusDriftCount=0."],
      ["Stage 3 (Fix)", "Surgical Remediation", "1 / 1 Fixed", "Fixed F-P9-SPA-01 in customer-warranty-reviews.component.js so /customer/warranty?invoiceId=<guid> resolves deep-linked invoices across full customer invoice history and opens the 5-star review form deterministically."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Verified F-P9-SPA-01 remediation clean across all 5 lenses with zero contract drift; scoped clean-code refactoring & boundary [Theory] targets across Application, Domain.UnitTests, and AngularJS SPA."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Extracted WarrantyEligibilityEvaluator onto Application layer, added +39 [Theory] boundary tests in Phase9BoundaryTests.cs (638->677 xUnit), and expanded frontend/scripts/verify-spa.mjs to 62/62 checks."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, 0 Phase shims, ADR-001 inward purity, 48/48 RBAC permissions (20/22/32), 10/10 SignalR events, git rev-list --count HEAD == 2, and all 5 AGENTS.md files under scoped byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "677/677 xUnit (595 unit + 82 LocalDB integration, 0 flaky, StrayTestDbCount=0), 62/62 SPA checks, SMK-01..10 PASS, 30/30 Edge CDP routes (0 console.error), 27/27 SQL tables (reviews=2, warrantyClaims=3, ReviewBonusDriftCount=0)."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.9 Cross-Phase (P1..P9) Forensic 7-Stage Quality Loop Verification Ledger (v1.2.1)"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (Cross-Phase P1..P9 Seam Hardening & Full-Stack Parity)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Cross-Phase Audit", "11 Caught", "Audited all 9 integrated phases (27 tables, 48 permissions, 18 controllers, 10 SignalR events, 30 routes, 23 docs, 18 scripts); surfaced F-XP-01..F-XP-11 across loyalty filters, demo VIN overlap/sale locks, 4-table immutability, 17-col CSV, /quality-warranty, and 36-cell notification deep-links."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 Suite", "Repros Locked", "Empirically reproduced F-XP-01 (?transactionType=ServiceReviewBonus 400), F-XP-03 (foreign-customer demo VIN sale race), F-XP-04 (13-col CSV omitting Phase 9 quality columns), and F-XP-05 (NotificationBell ServiceReview/Warranty routing) against live :5080/:4200 & LocalDB."],
      ["Stage 3 (Fix)", "Surgical Remediation", "11 / 11 Fixed", "Remediated F-XP-01..11: ServiceReviewBonus validator/UI filter, 409 DEMO_VIN_TIME_CONFLICT & INVENTORY_ASSIGNED_TO_TEST_DRIVE guards, 4-table AppendOnlyLedgerInterceptor + guardrail regexes, /quality-warranty & 17-col CSV, ?rating&minRating, 36-cell Bell routing, and OpenAPI/DD/MCP v1.2.0 sync (705 xUnit, 63 SPA)."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Re-audited 100% of Stage 3 backend, SPA, OpenAPI/DD, and .agents/ diffs across all 5 lenses confirming 0 regressions and 0 contract drift; scoped 4 clean-code refactoring & boundary [Theory] targets (RF-XP-01..RF-XP-04)."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Unified DI FluentValidation query validators (ServiceReview/WarrantyClaim/TestDrive), extracted shared QueryReviewsAndWarrantyByDealerAsync, centralized getLedgerTransactionTypes() & accessible NotificationBell, and expanded suites to 727/727 xUnit + 64/64 SPA checks."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, 0 Phase shims, 4-table immutability (PointLedgerEntries, SalesOrders, ServiceInvoices, ServiceInvoiceLineItems), 48/48 RBAC, 10/10 SignalR events, HEAD == 3 (Days 001..003 untouched), and all 5 AGENTS.md files under scoped byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "727/727 xUnit (645 unit + 82 LocalDB integration, 0 flaky, StrayTestDbCount=0), 64/64 SPA checks, SMK-01..SMK-10 PASS (all 9 analytics bindings + 17-col CSV), 30/30 Edge CDP routes (0 console.error), 27/27 SQL tables (0 drift), verify-post-task-sync 11/11 PASS."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.10 Phase 10 Mandatory 7-Stage Quality Loop & v1.3.3 Multi-Platform Verification Ledger"),
  makeTable(
    ["Quality Stage", "Gate Scope", "Outcome", "Empirical Verification Evidence (Native C# .NET 10 MAUI Android & iOS Field App)"],
    [
      ["Stage 1 (5R)", "R1.1..R1.5 Adversarial Audit", "5 Caught", "Audited Mobile.Core, Mobile.App, Mobile.UnitTests & Phase 10 scripts across 5 lenses (ADR-007 inward purity, 48-RBAC/10-event parity, SingleFlight/allowBackup/OwnerScopeKey hardening, online-only outbox law, x:DataType/SourceGen/48dp); surfaced F-P10-01..05."],
      ["Stage 2 (3T)", "T1.1..T1.3 Round 1 + Emulator", "Live Android Proof", "Backend 727/727 + Mobile 91/91 xUnit (0 warnings); live WHPX Pixel_8_API_35 Release APK (PID=2656, 0 logcat errors) rendering live :5080 Showroom/Wallet QR/Staff Bay; caught Debug Fast-Deploy SIGABRT (F-P10-01a), VND wrap (b), card name (c), POST_NOTIFICATIONS (d), and JDK-17 JAVA_HOME (F-P10-02)."],
      ["Stage 3 (Fix)", "Surgical Remediation", "5 / 5 Fixed", "Added EmbedAssembliesIntoApk=true + pm grant POST_NOTIFICATIONS + 1.55*,*,* NoWrap + Wallet.CustomerFullName (F-P10-01a-d), Resolve-MauiJavaSdkDir JDK 21+ (F-P10-02), StopAsync watermark/unread reset (F-P10-03), Idempotency-Key header + JsonException guard (F-P10-04), and localhost:4200 URL (F-P10-05)."],
      ["Stage 4 (5R)", "R2.1..R2.5 Post-Fix Audit", "0 Defects", "Re-audited all modified Mobile.Core, Mobile.App, Mobile.UnitTests & .agents/ scripts across all 5 lenses confirming 0 regressions and 0 contract drift; scoped clean-code refactors & boundary tests (RF-P10-01..03)."],
      ["Stage 5 (Ref)", "Clean-Code Refactoring", "Refactored", "Deduplicated DealerMobileApiClient transport into shared ExecuteRequestAsync<T> (RF-P10-01), expanded Mobile.UnitTests 91->103 (+12 regression & [Theory] cases, RF-P10-02), rebuilt zero-warning Debug & Release APKs, and verified live Debug Pixel_8_API_35 (RF-P10-03)."],
      ["Stage 6 (5R)", "R3.1..R3.5 Final Sign-Off", "5 / 5 PASS", "Confirmed 0 compiler warnings, pure net10.0 Mobile.Core, 21/21 x:DataType + SourceGen, 48/48 3-way RBAC parity, 10/10 SignalR events, allowBackup=false, HEAD == 3 (Days 001..003 untouched), and all 6 AGENTS.md rulebooks under byte caps."],
      ["Stage 7 (3T)", "T3.1..T3.3 Round 3 Gate", "3 / 3 PASS", "830/830 combined xUnit (727 Backend + 103 Mobile, 0 flaky), 64/64 SPA, SMK-01..10 PASS, 4 verified Pixel_8_API_35 screenshots (0 logcat errors), verify-post-task-sync 11/11 PASS, DOCX validate.py PASS."],
      ["v1.3.3 Upgrade", "Android + iOS & .NET 10 Libraries", "PASS (0 Warnings)", "Zero-Mock Real-Data (0 hits across 344 files) + Mobile.Core FluentValidation 11.11, Microsoft.Extensions.Http.Resilience 10.11.1 (Polly v8 idempotent retry + circuit breaker + 15s timeout), Humanizer.Core 2.14.1, WeakReferenceMessenger + Mobile.App net10.0-android;net10.0-ios (strict ATS Info.plist, Keychain Entitlements, AppDelegate, IosLocalNotificationService, AVCaptureDevice) + CommunityToolkit.Maui 13.0 (StatusBarBehavior, Expander, TouchBehavior, EmailValidationBehavior, Toast/Snackbar, converters); Android Debug 92.50 MB / Release 69.31 MB + iOS Debug/Release 0 warnings; verify-mobile-maui 8/8 PASS; 502/502 files synced."],
    ],
    [1360, 1900, 1100, 5000]
  ),

  h2("9.11 Mandatory Post-Task Synchronization Gate & 183-Day Git Schedule Parity (v1.3.3)"),
  makeTable(
    ["Check ID", "Enforced Invariant (.agents/scripts/verify-post-task-sync.mjs)", "Outcome", "Verified Workspace Evidence"],
    [
      ["GIT-PLAN-UNMAPPED", "100% of git-trackable files mapped in git-6month-schedule-plan.json", "PASS", "502 / 502 trackable workspace files mapped across 183 daily slices (unmappedCount=0, missingOnDiskCount=0, Days 001..003 untouched on HEAD == 3)."],
      ["GIT-PLAN-EXCLUSIVE", "Zero duplicate paths or empty uncommitted days (Days 004..183)", "PASS", "duplicateFuturePathsCount=0 and emptyDaysCount=0 across all 180 uncommitted schedule days (Phases 1-10 cleanly partitioned; Days 174..183 cover Phase 10 Android & iOS MAUI)."],
      ["CODE-SNAPSHOT-PARITY", "DbContext DbSet<T> & 3-Way Backend/SPA/Mobile Permissions vs. Snapshot", "PASS", "MotorcycleDealerDbContext=27/27 DbSet<T>; Permissions.cs (48), permissions.constants.js (48) & Mobile.Core/Contracts/Permissions.cs (48); SignalR=10 events."],
      ["WBS-PROGRESS-SYNC", "TASKS.md <-> PROGRESS.md <-> baseline-spec-snapshot.json", "PASS", "sync-progress.mjs --check verified 88 / 88 Master WBS tasks completed (100.0%) across Phases 1 through 10."],
      ["AGENTS-RULE-CAPS", "All 6 AGENTS.md rulebooks under byte caps & zero frontmatter", "PASS", "All 6 AGENTS.md rulebooks (root <24KB, .agents/rules <12KB, plugin <5KB, backend <8KB, frontend <6KB, mobile <8KB) verified under byte caps with zero frontmatter."],
      ["DOCS-SUITE-PARITY", "All Markdown/OpenAPI specs (+MOBILE_MAUI_SCREENS & v1.3.3) + Master .docx (>50 KB)", "PASS", "100% 27-server / 9-SQLCipher / 48-permission / 30-route / 16-mobile-view / 8-check verify-mobile-maui / 830-xUnit / 64-SPA parity across PRD (FR-01..10), ARCH (ADR-001..007), DESIGN, TEST_PLAN, RUNBOOK, BD/DD & DOCX v1.3.3."],
    ],
    [1760, 2500, 900, 4200]
  ),
];

const doc = new Document({
  creator: "Quoc Anh Hoang \u2014 Full-Stack Developer",
  title: "Motorcycle Dealer & Loyalty Management Platform \u2014 Master Engineering Specification",
  description: "v1.3.3 Multi-Platform .NET 10 MAUI Android & iOS + CommunityToolkit.Maui 13.0 Sync Edition: Complete Product (FR-01..10), Architecture (ADR-001..007), Basic & Detail Design Suite (BD_00 + DD_01..04 v1.3.3), 27-Table 3NF SQL Server + 9-Table Encrypted Mobile SQLCipher WAL, 48-Permission 3-Way RBAC, Showroom VIN Sales & Test-Drives, Garage & Factory VIN Warranty Claims (36m/30,000km), RowVersion Slot Booking, OEM SpareParts & Itemized ServiceInvoices, Post-Service 5-Star CSAT Reviews (+25 Bonus Points), SHA-256 Printable Receipts, 4-Tier Loyalty, CustomerVouchers, SignalR (10 Events), Admin Chart.js v4 Analytics, Multi-Platform Android & iOS MAUI Field App (net10.0-android;net10.0-ios, CommunityToolkit.Maui 13.0, FluentValidation, Polly v8 Http.Resilience, Humanizer, WeakReferenceMessenger, 16 Compiled Views, Camera2/AVFoundation VIN/QR), Mandatory Post-Task Sync Gate & 502-File 183-Day Git Schedule (88/88 WBS = 100%, 830 xUnit, 64 SPA, 8/8 MAUI)",
  features: {
    updateFields: true,
  },
  styles: {
    default: {
      document: {
        run: { font: "Arial", size: 21, color: "1E293B" },
        paragraph: { spacing: { before: 60, after: 120, line: 276 } },
      },
    },
    paragraphStyles: [
      {
        id: "Heading1",
        name: "Heading 1",
        basedOn: "Normal",
        next: "Normal",
        quickFormat: true,
        run: { size: 32, bold: true, font: "Arial", color: COLOR_NAVY },
        paragraph: { spacing: { before: 280, after: 180 }, outlineLevel: 0 },
      },
      {
        id: "Heading2",
        name: "Heading 2",
        basedOn: "Normal",
        next: "Normal",
        quickFormat: true,
        run: { size: 26, bold: true, font: "Arial", color: COLOR_SUBHEAD },
        paragraph: { spacing: { before: 220, after: 120 }, outlineLevel: 1 },
      },
      {
        id: "Heading3",
        name: "Heading 3",
        basedOn: "Normal",
        next: "Normal",
        quickFormat: true,
        run: { size: 22, bold: true, font: "Arial", color: COLOR_ACCENT },
        paragraph: { spacing: { before: 160, after: 80 }, outlineLevel: 2 },
      },
    ],
  },
  numbering: {
    config: [
      {
        reference: "bullets",
        levels: [
          {
            level: 0,
            format: LevelFormat.BULLET,
            text: "\u2022",
            alignment: AlignmentType.LEFT,
            style: { paragraph: { indent: { left: 720, hanging: 360 } } },
          },
          {
            level: 1,
            format: LevelFormat.BULLET,
            text: "\u25E6",
            alignment: AlignmentType.LEFT,
            style: { paragraph: { indent: { left: 1080, hanging: 360 } } },
          },
        ],
      },
      {
        reference: "lifecycleNums",
        levels: [
          {
            level: 0,
            format: LevelFormat.DECIMAL,
            text: "%1.",
            alignment: AlignmentType.LEFT,
            style: { paragraph: { indent: { left: 720, hanging: 360 } } },
          },
        ],
      },
    ],
  },
  sections: [
    {
      properties: {
        page: {
          size: {
            width: 12240,
            height: 15840,
          },
          margin: {
            top: 1440,
            right: 1440,
            bottom: 1440,
            left: 1440,
          },
        },
      },
      headers: {
        default: new Header({
          children: [
            new Paragraph({
              alignment: AlignmentType.RIGHT,
              spacing: { before: 0, after: 80 },
              children: [
                tr("Motorcycle Dealer & Loyalty Management Platform \u2014 Master Engineering Specification (v1.3.3 Multi-Platform Android & iOS MAUI Edition)", {
                  size: 17,
                  color: COLOR_MUTED,
                }),
              ],
            }),
          ],
        }),
      },
      footers: {
        default: new Footer({
          children: [
            new Paragraph({
              alignment: AlignmentType.CENTER,
              spacing: { before: 80, after: 0 },
              children: [
                tr("github.com/quocanhh86/motorcycle-dealer  |  v1.3.3 Complete (88/88 WBS \u2022 830 xUnit \u2022 64 SPA \u2022 16 Android/iOS Views \u2022 8/8 MAUI \u2022 100% Synced)  |  Page ", {
                  size: 17,
                  color: COLOR_MUTED,
                }),
                new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 17, color: COLOR_MUTED }),
                tr(" of ", { size: 17, color: COLOR_MUTED }),
                new TextRun({ children: [PageNumber.TOTAL_PAGES], font: "Arial", size: 17, color: COLOR_MUTED }),
              ],
            }),
          ],
        }),
      },
      children,
    },
  ],
});

const buffer = await Packer.toBuffer(doc);
if (dryRun) {
  console.log(
    `[PASS] [DRY-RUN] Compiled & verified Master Specification DOCX in memory (${buffer.length} bytes; 9 sections; target=${outPath}; disk write skipped).`
  );
  process.exit(0);
}

fs.mkdirSync(path.dirname(outPath), { recursive: true });
fs.writeFileSync(outPath, buffer);
console.log(`[PASS] Generated DOCX successfully: ${outPath} (${buffer.length} bytes)`);
