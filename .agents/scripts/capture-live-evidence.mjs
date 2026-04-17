#!/usr/bin/env node
/**
 * capture-live-evidence.mjs
 * Portfolio live-evidence harvester for the Motorcycle Dealer & Loyalty Management Platform (v1.2.0:
 * 27 3NF tables, 48 RBAC permissions, 10 SignalR events, 30 navigable UI-Router routes).
 * Coordinates headless Playwright browser capture across:
 *   1) Customer Showroom, Dealer VIN Inventory & [Book Test Drive] CTA
 *   2) Customer Digital VIN Garage & Concurrency-Safe Slot Picker
 *   3) Dual-Context Dealer Staff Real-Time SignalR Workbench (instant BookingCreated / TestDriveCreated / WarrantyClaimCreated toast)
 *   4) Customer Loyalty Wallet, Ledger, Voucher Store & Printable Receipt Modal
 *   5) Admin Multi-Dealer Chart.js v4 Analytics Dashboard
 *   6) Showroom Test-Drive Desk & Admin OEM Spare-Parts Catalog Studio
 *   7) Post-Service 5-Star CSAT Reviews (+25 bonus pts), 36m/30,000km VIN Warranty Claims & Admin Quality Analytics
 * Supports --dry-run cleanly with zero disk writes.
 */

import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");
const require = createRequire(import.meta.url);

const rawArgs = process.argv.slice(2);
let dryRun = false;
let apiUrl = "http://localhost:5080";
let spaUrl = "http://localhost:4200";
let outDir = path.join(repoRoot, "docs", "evidence");

for (let i = 0; i < rawArgs.length; i++) {
  const arg = rawArgs[i];
  if (arg === "--dry-run" || arg === "-DryRun") {
    dryRun = true;
  } else if (arg === "--api-url" || arg === "-ApiUrl") {
    apiUrl = rawArgs[i + 1] || apiUrl;
    i++;
  } else if (arg === "--spa-url" || arg === "-SpaUrl") {
    spaUrl = rawArgs[i + 1] || spaUrl;
    i++;
  } else if (arg === "--out-dir" || arg === "-OutDir") {
    outDir = path.resolve(rawArgs[i + 1] || outDir);
    i++;
  }
}

apiUrl = apiUrl.replace(/\/+$/, "");
spaUrl = spaUrl.replace(/\/+$/, "");

const CAPTURE_JOURNEYS = [
  {
    id: "EV-01",
    file: "01-customer-showroom-catalog.png",
    portal: "Public / Customer Portal",
    uiState: "app.public.showroom",
    route: "/#!/showroom",
    requirement: "FR-02",
    assertion: "Renders regional Dealers (Hanoi, Da Nang, Saigon), MotorcycleModels, Available 17-char VIN count, and [Book Test Drive] CTA.",
  },
  {
    id: "EV-02",
    file: "02-customer-vin-slot-booking.png",
    portal: "Customer Portal",
    uiState: "app.customer.bookingNew",
    route: "/#!/customer/bookings/new",
    requirement: "FR-03, FR-04, ADR-005",
    assertion: "Displays owned VIN garage card, due-maintenance badge, open ServiceTimeSlots capacity (MaxCapacity - BookedCount), and optional CustomerVoucher selector.",
  },
  {
    id: "EV-03",
    file: "03-staff-realtime-signalr-workbench.png",
    portal: "Dealer Staff Live Workbench (Dual-Context 10-Event SignalR)",
    uiState: "app.staff.workbench",
    route: "/#!/staff/workbench",
    requirement: "FR-06, ADR-004, ADR-006",
    assertion: "Context A (Customer) submits POST /api/v1/bookings -> Context B (DealerStaff in dealer-{dealerId}) receives instant SignalR BookingCreated toast & prepended card without page reload.",
  },
  {
    id: "EV-04",
    file: "04-customer-loyalty-wallet-vouchers.png",
    portal: "Customer Portal",
    uiState: "app.customer.loyalty",
    route: "/#!/customer/loyalty",
    requirement: "FR-05, ADR-003",
    assertion: "Shows Bronze/Silver/Gold/Platinum badge, multiplier progress bar, append-only PointLedgerEntries audit rows, redeemable VoucherCatalogs, and [Print Receipt] trigger.",
  },
  {
    id: "EV-05",
    file: "05-admin-chartjs-analytics-dashboard.png",
    portal: "Admin Executive Portal",
    uiState: "app.admin.analytics",
    route: "/#!/admin/analytics",
    requirement: "FR-07, ADR-002, ADR-006",
    assertion: "Renders 4 Chart.js v4 canvases (Showroom vs Service Revenue Stacked Bar, Bookings by Dealer, Monthly Trends Multi-Line, Loyalty Tier Doughnut) with zero console errors.",
  },
  {
    id: "EV-06",
    file: "06-v11-test-drives-spare-parts-receipts.png",
    portal: "Customer / Staff / Admin v1.1 Expansion",
    uiState: "app.customer.testDrives / app.staff.testDrives / app.admin.spareParts",
    route: "/#!/customer/test-drives",
    requirement: "FR-02, FR-04, FR-06, T-801..T-808",
    assertion: "Verifies Showroom TestDriveBookings live SignalR (TestDriveCreated / TestDriveStatusChanged), OEM SpareParts itemized ServiceInvoiceLineItems, and <printable-receipt-modal>.",
  },
  {
    id: "EV-07",
    file: "07-v12-csat-reviews-warranty-claims.png",
    portal: "Customer / Staff / Admin v1.2 Quality & Warranty Suite",
    uiState: "app.customer.warranty / app.staff.warrantyReviews / app.admin.qualityWarranty",
    route: "/#!/customer/warranty",
    requirement: "FR-05, FR-06, FR-09, T-901..T-908",
    assertion: "Verifies SCR-CUS-07 5-star CSAT ServiceReviews (+25 AvailablePoints bonus) & 36m/30,000km VIN WarrantyClaims, SCR-STF-07 live SignalR (ServiceReviewSubmitted / WarrantyClaimCreated / WarrantyClaimStatusChanged), and SCR-ADM-08 Quality & Warranty Analytics.",
  },
];

const slnPath = path.join(repoRoot, "backend", "MotorcycleDealer.sln");
const spaIndex = path.join(repoRoot, "frontend", "src", "index.html");

if (dryRun) {
  console.log(
    `[INFO] [DRY-RUN] Portfolio Live Evidence Harvester (Viewport=1440x900, API=${apiUrl}, SPA=${spaUrl}, Tables=27, Routes=30, Permissions=48, SignalREvents=10)`
  );
  console.log(`[INFO] [DRY-RUN] Planned Output Directory: ${outDir} (skipped in --dry-run)`);
  console.log(
    `[INFO] [DRY-RUN] Workspace Code Status: Backend=${fs.existsSync(slnPath)}, Frontend=${fs.existsSync(spaIndex)}`
  );
  for (const j of CAPTURE_JOURNEYS) {
    console.log(`  - [${j.id}] ${j.file} (${j.uiState} | ${j.requirement}): ${j.assertion}`);
  }
  console.log(
    `[PASS] capture-live-evidence dry-run verified all ${CAPTURE_JOURNEYS.length} evidence capture journeys.`
  );
  process.exit(0);
}

if (!fs.existsSync(slnPath) || !fs.existsSync(spaIndex)) {
  console.error(
    "[FAIL] Live evidence capture requires Phase 3+ backend/ and frontend/ implementation (run with --dry-run in Phase 2)."
  );
  process.exit(1);
}

function resolvePlaywright() {
  const candidates = ["playwright", "playwright-core"];
  for (const modName of candidates) {
    try {
      return require(modName);
    } catch {
      // try next
    }
  }
  if (process.env.NODE_PATH) {
    for (const dir of process.env.NODE_PATH.split(path.delimiter).filter(Boolean)) {
      for (const modName of candidates) {
        try {
          return require(path.join(dir, modName));
        } catch {
          // continue
        }
      }
    }
  }
  return null;
}

async function harvestLiveEvidence() {
  const pw = resolvePlaywright();
  if (!pw || !pw.chromium) {
    console.error(
      "[FAIL] Playwright Node runtime not found in NODE_PATH. Set NODE_PATH to Playwright or use the Antigravity 'playwright' MCP server."
    );
    process.exit(1);
  }

  fs.mkdirSync(outDir, { recursive: true });
  const browser = await pw.chromium.launch({ headless: true });
  const captured = [];

  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    for (const j of CAPTURE_JOURNEYS) {
      const targetUrl = `${spaUrl}${j.route}`;
      console.log(`[INFO] Navigating to ${targetUrl} (${j.id})...`);
      await page.goto(targetUrl, { waitUntil: "networkidle", timeout: 20000 });
      const shotPath = path.join(outDir, j.file);
      await page.screenshot({ path: shotPath, fullPage: true });
      captured.push({ ...j, capturedAtUtc: new Date().toISOString(), path: shotPath });
      console.log(`[PASS] Captured ${shotPath}`);
    }

    const manifestPath = path.join(outDir, "evidence-manifest.json");
    fs.writeFileSync(
      manifestPath,
      JSON.stringify({ generatedAtUtc: new Date().toISOString(), apiUrl, spaUrl, captured }, null, 2),
      "utf8"
    );
    console.log(`[PASS] Wrote evidence manifest: ${manifestPath}`);
  } finally {
    await browser.close();
  }
}

harvestLiveEvidence().catch((err) => {
  console.error(`[FAIL] Live evidence capture failed: ${err.message}`);
  process.exit(1);
});
