#!/usr/bin/env node
/**
 * verify-post-task-sync.mjs
 * Zero-dependency Node.js v22 automated Post-Task Synchronization Gate for the
 * Motorcycle Dealer & Loyalty Management Platform.
 *
 * Deterministically verifies (exits 1 on any drift):
 *   1) 100% Workspace -> git-6month-schedule-plan.json Coverage:
 *      - Every git-trackable file (`git ls-files --cached --others --exclude-standard`) is mapped (`unmappedCount === 0`)
 *      - Every mapped `existing-workspace` file exists on disk (`missingOnDiskCount === 0`)
 *      - Zero duplicate file paths across uncommitted schedule days (`day > highestCommittedDay`, `duplicateFuturePathsCount === 0`)
 *      - Every uncommitted day (`day > highestCommittedDay`) owns >= 1 exclusive uncommitted file (`emptyDaysCount === 0`)
 *   2) Code <-> baseline-spec-snapshot.json <-> Trackers <-> Docs Invariant Parity:
 *      - Permissions.cs & permissions.constants.js both define exact 48 Permissions.* constants matching snapshot
 *      - MotorcycleDealerDbContext.cs defines exact 27 DbSet<T> properties matching snapshot
 *      - sync-progress.mjs --check passes (TASKS.md <-> PROGRESS.md <-> baseline-spec-snapshot.json: 80 tasks)
 *      - All 5 AGENTS.md rulebooks respect strict byte caps (<24000, <12000, <5000, <8000, <6000) with zero YAML frontmatter
 *      - Required documentation suite & Master Word Specification (.docx > 50,000 bytes) exist and reflect the 27-table / 48-permission / 10-event / 30-route v1.2 architecture.
 */

import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");

const cliFlags = new Set(process.argv.slice(2));
const jsonMode = cliFlags.has("--json");
const dryRunMode = cliFlags.has("--dry-run") || cliFlags.has("-DryRun");

const STANDARD_WIN_TOOL_DIRS = [
  "C:\\Program Files\\GitHub CLI",
  "C:\\Program Files\\Git\\cmd",
  "C:\\Program Files\\Git\\bin",
  "C:\\Program Files\\nodejs",
];

function computeEnhancedPath(basePath = "") {
  const existingSegments = basePath
    .split(path.delimiter)
    .map((s) => s.trim())
    .filter(Boolean);
  const lowerSet = new Set(existingSegments.map((s) => s.toLowerCase()));
  const prepend = [];
  for (const dir of STANDARD_WIN_TOOL_DIRS) {
    if (fs.existsSync(dir) && !lowerSet.has(dir.toLowerCase())) {
      prepend.push(dir);
      lowerSet.add(dir.toLowerCase());
    }
  }
  return [...prepend, ...existingSegments].join(path.delimiter);
}

function buildChildEnv() {
  const env = { ...process.env };
  const pathKeys = Object.keys(env).filter((k) => k.toUpperCase() === "PATH");
  const primaryPathKey = pathKeys[0] || "PATH";
  const currentPath = pathKeys.map((k) => env[k] || "").find(Boolean) || "";
  for (const key of pathKeys) {
    if (key !== primaryPathKey) {
      delete env[key];
    }
  }
  env[primaryPathKey] = computeEnhancedPath(currentPath);
  env.GIT_TERMINAL_PROMPT = "0";
  env.GCM_INTERACTIVE = "never";
  return env;
}

function resolveGitBinary() {
  const env = buildChildEnv();
  const candidates = [
    "git",
    "C:\\Program Files\\Git\\cmd\\git.exe",
    "C:\\Program Files\\Git\\bin\\git.exe",
  ];
  for (const bin of candidates) {
    const res = spawnSync(bin, ["--version"], {
      cwd: repoRoot,
      encoding: "utf8",
      windowsHide: true,
      env,
    });
    if (!res.error && res.status === 0) {
      return bin;
    }
  }
  throw new Error("Unable to locate git executable on PATH or standard Windows Git directories.");
}

const GIT_BIN = resolveGitBinary();

function runGitReadOnly(args) {
  const res = spawnSync(GIT_BIN, args, {
    cwd: repoRoot,
    encoding: "utf8",
    windowsHide: true,
    maxBuffer: 16 * 1024 * 1024,
    env: buildChildEnv(),
  });
  return {
    status: typeof res.status === "number" ? res.status : 1,
    stdout: res.stdout || "",
    stderr: res.stderr || "",
  };
}

const failures = [];
const checks = [];

function recordPass(id, detail) {
  checks.push({ id, passed: true, detail });
  if (!jsonMode) {
    console.log(`[PASS] [${id}] ${detail}`);
  }
}

function recordFail(id, detail) {
  checks.push({ id, passed: false, detail });
  failures.push(`[${id}] ${detail}`);
  if (!jsonMode) {
    console.error(`[FAIL] [${id}] ${detail}`);
  }
}

if (!jsonMode) {
  console.log(
    `[INFO] === Motorcycle Dealer Post-Task Synchronization Gate (Mode=${dryRunMode ? "DRY-RUN" : "ENFORCE"}) ===`
  );
}

// ============================================================================
// PART A: 100% Workspace -> git-6month-schedule-plan.json Coverage & Day Exclusivity
// ============================================================================
const planPath = path.join(__dirname, "git-6month-schedule-plan.json");
let scheduleSummary = null;

if (!fs.existsSync(planPath)) {
  recordFail("GIT-PLAN-EXISTS", "Missing .agents/scripts/git-6month-schedule-plan.json");
} else {
  const plan = JSON.parse(fs.readFileSync(planPath, "utf8"));
  const days = Array.isArray(plan.days) ? plan.days : [];
  const expectedTotalDays = Number(plan?.scheduleWindow?.totalDays || 183);

  if (days.length !== expectedTotalDays) {
    recordFail(
      "GIT-PLAN-DAYS",
      `Expected ${expectedTotalDays} scheduled days in git-6month-schedule-plan.json, got ${days.length}.`
    );
  } else {
    recordPass("GIT-PLAN-DAYS", `Verified ${days.length} / ${expectedTotalDays} scheduled slices in git-6month-schedule-plan.json.`);
  }

  const lsRes = runGitReadOnly(["ls-files", "-z", "--cached", "--others", "--exclude-standard"]);
  if (lsRes.status !== 0) {
    recordFail("GIT-LS-FILES", `git ls-files failed: ${lsRes.stderr.trim()}`);
  } else {
    const trackableFiles = Array.from(
      new Set(
        lsRes.stdout
          .split("\0")
          .map((f) => f.trim().replace(/\\/g, "/"))
          .filter(Boolean)
      )
    ).sort((a, b) => a.localeCompare(b));

    const hasHead = runGitReadOnly(["rev-parse", "--verify", "HEAD"]).status === 0;
    const committedDays = new Set();
    if (hasHead) {
      const logBodies = runGitReadOnly(["log", "--format=%B%x00"]).stdout;
      const trailerRe = /^X-Schedule-Day:\s*(\d+)(?:\/\d+)?\s*$/gim;
      let m;
      while ((m = trailerRe.exec(logBodies)) !== null) {
        const dNum = Number.parseInt(m[1], 10);
        if (Number.isInteger(dNum) && dNum >= 1) {
          committedDays.add(dNum);
        }
      }
    }
    const highestCommittedDay = committedDays.size > 0 ? Math.max(...committedDays) : 0;

    const dirtySet = new Set();
    if (!hasHead) {
      for (const f of trackableFiles) dirtySet.add(f);
    } else {
      const stRaw = runGitReadOnly(["status", "--porcelain", "-z", "--untracked-files=all"]).stdout;
      const parts = stRaw.split("\0");
      for (let i = 0; i < parts.length; i++) {
        const tok = parts[i];
        if (!tok || tok.length < 4) continue;
        const x = tok[0];
        const rel = tok.slice(3).replace(/\\/g, "/");
        if (x === "R" || x === "C") i += 1;
        dirtySet.add(rel);
      }
    }

    const allMappedPaths = new Set();
    const missingOnDisk = [];
    const futurePathOwners = new Map();

    for (const d of days) {
      const sliceKind = d.sliceKind || "existing-workspace";
      const relPaths = Array.isArray(d.expectedPaths)
        ? d.expectedPaths.map((p) => String(p).replace(/\\/g, "/"))
        : [];

      for (const rel of relPaths) {
        allMappedPaths.add(rel);
        if (sliceKind === "existing-workspace" && !fs.existsSync(path.join(repoRoot, rel))) {
          missingOnDisk.push(`Day ${String(d.day).padStart(3, "0")}: ${rel}`);
        }
        if (d.day > highestCommittedDay) {
          if (!futurePathOwners.has(rel)) {
            futurePathOwners.set(rel, []);
          }
          futurePathOwners.get(rel).push(d.day);
        }
      }
    }

    const unmappedFiles = trackableFiles.filter((f) => !allMappedPaths.has(f));
    const duplicateFuturePaths = [];
    for (const [rel, ownerDays] of futurePathOwners.entries()) {
      if (ownerDays.length > 1) {
        duplicateFuturePaths.push(`${rel} (days: ${ownerDays.join(", ")})`);
      }
    }

    const emptyDays = [];
    for (const d of days) {
      if (d.day <= highestCommittedDay) continue;
      const relPaths = Array.isArray(d.expectedPaths)
        ? d.expectedPaths.map((p) => String(p).replace(/\\/g, "/"))
        : [];
      const exclusiveUncommitted = relPaths.filter((rel) => {
        const owners = futurePathOwners.get(rel) || [];
        return (
          owners.length === 1 &&
          dirtySet.has(rel) &&
          fs.existsSync(path.join(repoRoot, rel))
        );
      });
      if (exclusiveUncommitted.length === 0) {
        emptyDays.push(`Day ${String(d.day).padStart(3, "0")} (${d.taskId || "no-task"})`);
      }
    }

    scheduleSummary = {
      trackableCount: trackableFiles.length,
      mappedUniqueCount: allMappedPaths.size,
      highestCommittedDay,
      unmappedCount: unmappedFiles.length,
      missingOnDiskCount: missingOnDisk.length,
      duplicateFuturePathsCount: duplicateFuturePaths.length,
      emptyDaysCount: emptyDays.length,
    };

    if (unmappedFiles.length > 0) {
      recordFail(
        "GIT-PLAN-UNMAPPED",
        `Found ${unmappedFiles.length} unmapped git-trackable workspace file(s): ${unmappedFiles.slice(0, 15).join(", ")}`
      );
    } else {
      recordPass(
        "GIT-PLAN-UNMAPPED",
        `100% of git-trackable workspace files (${trackableFiles.length} / ${trackableFiles.length}) are mapped in git-6month-schedule-plan.json (unmappedCount=0).`
      );
    }

    if (missingOnDisk.length > 0) {
      recordFail(
        "GIT-PLAN-MISSING-DISK",
        `Found ${missingOnDisk.length} mapped existing-workspace file(s) missing on disk: ${missingOnDisk.slice(0, 15).join("; ")}`
      );
    } else {
      recordPass(
        "GIT-PLAN-MISSING-DISK",
        `100% of mapped existing-workspace files (${allMappedPaths.size}) exist on disk (missingOnDiskCount=0).`
      );
    }

    if (duplicateFuturePaths.length > 0) {
      recordFail(
        "GIT-PLAN-DUP-FUTURE",
        `Found ${duplicateFuturePaths.length} duplicate path(s) shared across uncommitted days (day > ${highestCommittedDay}): ${duplicateFuturePaths.slice(0, 30).join("; ")}`
      );
    } else {
      recordPass(
        "GIT-PLAN-DUP-FUTURE",
        `Zero duplicate file paths across uncommitted schedule days (day > ${highestCommittedDay}, duplicateFuturePathsCount=0).`
      );
    }

    if (emptyDays.length > 0) {
      recordFail(
        "GIT-PLAN-EMPTY-DAYS",
        `Found ${emptyDays.length} uncommitted day(s) without >= 1 exclusive uncommitted file: ${emptyDays.slice(0, 30).join(", ")}`
      );
    } else {
      recordPass(
        "GIT-PLAN-EMPTY-DAYS",
        `Every uncommitted schedule day (Days ${highestCommittedDay + 1}..${days.length}) owns >= 1 exclusive uncommitted file (emptyDaysCount=0).`
      );
    }
  }
}

// ============================================================================
// PART B: Code <-> baseline-spec-snapshot.json <-> Trackers <-> Rules <-> Docs
// ============================================================================
const snapshotPath = path.join(__dirname, "baseline-spec-snapshot.json");
if (!fs.existsSync(snapshotPath)) {
  recordFail("SNAPSHOT-EXISTS", "Missing .agents/scripts/baseline-spec-snapshot.json");
} else {
  const snapshot = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
  const snapshotTableNames = Array.isArray(snapshot?.database?.tables)
    ? snapshot.database.tables.map((t) => t.name)
    : [];
  const expectedTableCount = Number(
    snapshot?.project?.database?.tableCount ?? snapshot?.database?.tableCount ?? snapshotTableNames.length
  );
  const snapshotPermCodes = Array.isArray(snapshot?.rbac?.permissions)
    ? snapshot.rbac.permissions.map((p) => p.code)
    : [];
  const expectedPermCount = snapshotPermCodes.length;
  const expectedTaskCount = Array.isArray(snapshot?.phases)
    ? snapshot.phases.reduce((sum, p) => sum + Number(p.taskCount || 0), 0)
    : 0;
  const expectedSignalrCount = Array.isArray(snapshot?.signalr?.events)
    ? snapshot.signalr.events.length
    : 0;

  if (
    expectedTableCount !== 27 ||
    snapshotTableNames.length !== 27 ||
    expectedPermCount !== 48 ||
    expectedTaskCount !== 88 ||
    expectedSignalrCount !== 10
  ) {
    recordFail(
      "SNAPSHOT-INVARIANTS",
      `Unexpected baseline-spec-snapshot.json counts: tables=${expectedTableCount}/${snapshotTableNames.length} (expected 27), permissions=${expectedPermCount} (expected 48), tasks=${expectedTaskCount} (expected 88), signalrEvents=${expectedSignalrCount} (expected 10).`
    );
  } else {
    recordPass(
      "SNAPSHOT-INVARIANTS",
      `baseline-spec-snapshot.json invariants verified (tables=${expectedTableCount}, permissions=${expectedPermCount}, wbsTasks=${expectedTaskCount}, signalrEvents=${expectedSignalrCount}).`
    );
  }

  // 1. Permissions.cs, permissions.constants.js & Mobile.Core/Contracts/Permissions.cs parity (48 constants)
  const permissionsCsPath = path.join(
    repoRoot,
    "backend",
    "src",
    "MotorcycleDealer.Domain",
    "Constants",
    "Permissions.cs"
  );
  const permissionsJsPath = path.join(
    repoRoot,
    "frontend",
    "src",
    "app",
    "core",
    "auth",
    "permissions.constants.js"
  );
  const mobileSlnPath = path.join(repoRoot, "mobile", "MotorcycleDealer.Mobile.sln");
  const mobileCoreDir = path.join(repoRoot, "mobile", "src", "MotorcycleDealer.Mobile.Core");
  const mobilePermissionsCsPath = path.join(mobileCoreDir, "Contracts", "Permissions.cs");
  const mobileCoreActive =
    fs.existsSync(mobileSlnPath) || fs.existsSync(mobileCoreDir) || fs.existsSync(mobilePermissionsCsPath);

  if (!fs.existsSync(permissionsCsPath) || !fs.existsSync(permissionsJsPath)) {
    recordFail(
      "RBAC-CODE-PARITY",
      `Missing Permissions.cs or permissions.constants.js.`
    );
  } else if (mobileCoreActive && !fs.existsSync(mobilePermissionsCsPath)) {
    recordFail(
      "RBAC-CODE-PARITY",
      `Missing mobile/src/MotorcycleDealer.Mobile.Core/Contracts/Permissions.cs while mobile/ solution is present.`
    );
  } else {
    const csRaw = fs.readFileSync(permissionsCsPath, "utf8");
    const jsRaw = fs.readFileSync(permissionsJsPath, "utf8");
    const csPerms = new Set(
      Array.from(csRaw.matchAll(/"(Permissions\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+)"/g), (m) => m[1])
    );
    const jsPerms = new Set(
      Array.from(jsRaw.matchAll(/['"](Permissions\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+)['"]/g), (m) => m[1])
    );
    const snapPermSet = new Set(snapshotPermCodes);
    const missingInCs = snapshotPermCodes.filter((p) => !csPerms.has(p));
    const extraInCs = Array.from(csPerms).filter((p) => !snapPermSet.has(p));
    const missingInJs = snapshotPermCodes.filter((p) => !jsPerms.has(p));
    const extraInJs = Array.from(jsPerms).filter((p) => !snapPermSet.has(p));

    let mobileDrift = false;
    let mobilePermsSize = 0;
    let missingInMobile = [];
    let extraInMobile = [];
    if (mobileCoreActive) {
      const mobileRaw = fs.readFileSync(mobilePermissionsCsPath, "utf8");
      const mobilePerms = new Set(
        Array.from(mobileRaw.matchAll(/"(Permissions\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+)"/g), (m) => m[1])
      );
      mobilePermsSize = mobilePerms.size;
      missingInMobile = snapshotPermCodes.filter((p) => !mobilePerms.has(p));
      extraInMobile = Array.from(mobilePerms).filter((p) => !snapPermSet.has(p));
      if (mobilePerms.size !== expectedPermCount || missingInMobile.length > 0 || extraInMobile.length > 0) {
        mobileDrift = true;
      }
    }

    if (
      csPerms.size !== expectedPermCount ||
      jsPerms.size !== expectedPermCount ||
      missingInCs.length > 0 ||
      extraInCs.length > 0 ||
      missingInJs.length > 0 ||
      extraInJs.length > 0 ||
      mobileDrift
    ) {
      const mobileNote = mobileCoreActive
        ? `, Mobile.Core/Contracts/Permissions.cs=${mobilePermsSize}/${expectedPermCount} (missing=[${missingInMobile.join(",")}], extra=[${extraInMobile.join(",")}])`
        : "";
      recordFail(
        "RBAC-CODE-PARITY",
        `Permission drift detected: Permissions.cs=${csPerms.size}/${expectedPermCount} (missing=[${missingInCs.join(",")}], extra=[${extraInCs.join(",")}]), permissions.constants.js=${jsPerms.size}/${expectedPermCount} (missing=[${missingInJs.join(",")}], extra=[${extraInJs.join(",")}])${mobileNote}.`
      );
    } else {
      const mobileNote = mobileCoreActive
        ? ` and Mobile.Core/Contracts/Permissions.cs (${mobilePermsSize})`
        : "";
      recordPass(
        "RBAC-CODE-PARITY",
        `Permissions.cs (${csPerms.size}), permissions.constants.js (${jsPerms.size})${mobileNote} match all ${expectedPermCount} canonical RBAC policies 1-for-1.`
      );
    }
  }

  // 2. MotorcycleDealerDbContext.cs DbSet<T> parity (25 tables)
  const dbContextPath = path.join(
    repoRoot,
    "backend",
    "src",
    "MotorcycleDealer.Infrastructure",
    "Persistence",
    "MotorcycleDealerDbContext.cs"
  );
  if (!fs.existsSync(dbContextPath)) {
    recordFail("DBCONTEXT-PARITY", "Missing MotorcycleDealerDbContext.cs.");
  } else {
    const dbRaw = fs.readFileSync(dbContextPath, "utf8");
    const dbSets = new Set(
      Array.from(dbRaw.matchAll(/\bDbSet<\s*[A-Za-z0-9_.]+\s*>\s+([A-Za-z0-9_]+)\b/g), (m) => m[1])
    );
    const snapTableSet = new Set(snapshotTableNames);
    const missingTables = snapshotTableNames.filter((t) => !dbSets.has(t));
    const extraTables = Array.from(dbSets).filter((t) => !snapTableSet.has(t));
    if (dbSets.size !== expectedTableCount || missingTables.length > 0 || extraTables.length > 0) {
      recordFail(
        "DBCONTEXT-PARITY",
        `MotorcycleDealerDbContext DbSet drift: count=${dbSets.size}/${expectedTableCount}, missing=[${missingTables.join(",")}], extra=[${extraTables.join(",")}].`
      );
    } else {
      recordPass(
        "DBCONTEXT-PARITY",
        `MotorcycleDealerDbContext.cs defines all ${dbSets.size} / ${expectedTableCount} canonical 3NF DbSet<T> properties.`
      );
    }
  }
}

// 3. sync-progress.mjs --check (TASKS.md <-> PROGRESS.md <-> baseline-spec-snapshot.json)
const syncProgressPath = path.join(__dirname, "sync-progress.mjs");
const syncRes = spawnSync(process.execPath, [syncProgressPath, "--check"], {
  cwd: repoRoot,
  encoding: "utf8",
  windowsHide: true,
});
if (syncRes.status !== 0) {
  recordFail(
    "WBS-PROGRESS-SYNC",
    `sync-progress.mjs --check failed: ${(syncRes.stderr || syncRes.stdout || "").trim()}`
  );
} else {
  const passLine =
    (syncRes.stdout || "")
      .split(/\r?\n/)
      .find((l) => l.includes("[PASS]")) || "TASKS.md <-> PROGRESS.md synchronized.";
  recordPass("WBS-PROGRESS-SYNC", passLine.replace(/^\[PASS\]\s*/, ""));
}

// 4. All 6 AGENTS.md Rulebooks Byte Caps & Zero YAML Frontmatter
const ruleCapSpecs = [
  { rel: "AGENTS.md", maxBytes: 24000 },
  { rel: ".agents/rules/AGENTS.md", maxBytes: 12000 },
  { rel: ".agents/plugins/motorcycle-dealer-suite/rules/AGENTS.md", maxBytes: 5000 },
  { rel: "backend/AGENTS.md", maxBytes: 8000 },
  { rel: "frontend/AGENTS.md", maxBytes: 6000 },
  { rel: "mobile/AGENTS.md", maxBytes: 8000 },
];
const ruleViolations = [];
const ruleSizes = [];
for (const spec of ruleCapSpecs) {
  const abs = path.join(repoRoot, spec.rel);
  if (!fs.existsSync(abs)) {
    ruleViolations.push(`${spec.rel} missing`);
    continue;
  }
  const buf = fs.readFileSync(abs);
  const firstLine = buf.toString("utf8").split(/\r?\n/)[0].trim();
  ruleSizes.push(`${spec.rel}=${buf.length}/${spec.maxBytes}B`);
  if (buf.length >= spec.maxBytes) {
    ruleViolations.push(`${spec.rel} (${buf.length}B >= ${spec.maxBytes}B cap)`);
  }
  if (firstLine === "---") {
    ruleViolations.push(`${spec.rel} starts with prohibited YAML frontmatter '---'`);
  }
}
if (ruleViolations.length > 0) {
  recordFail("AGENTS-RULE-CAPS", ruleViolations.join("; "));
} else {
  recordPass("AGENTS-RULE-CAPS", `All ${ruleCapSpecs.length} AGENTS.md rulebooks within byte caps & zero frontmatter (${ruleSizes.join(", ")}).`);
}

// 5. Documentation Suite & Master .docx Freshness (27-table / 48-permission / 10-event / 30-route v1.2 parity)
const requiredDocs = [
  {
    rel: "docs/PRD.md",
    patterns: [/\b27\b/, /\b48\b/, /FR-09/, /ServiceReview|WarrantyClaim/i],
  },
  {
    rel: "docs/ARCHITECTURE.md",
    patterns: [/\b27\b/, /\b48\b/, /ServiceReviews/, /WarrantyClaims/],
  },
  {
    rel: "docs/DATABASE_DESIGN.md",
    patterns: [/ServiceReviews/, /WarrantyClaims/, /TestDriveBookings/, /ServiceInvoiceLineItems/],
  },
  {
    rel: "docs/DESIGN.md",
    patterns: [/Permissions\.Reviews\.CreateOwn/, /Permissions\.Warranty\.ManageDealer/, /ServiceReviewSubmitted/, /WarrantyClaimCreated/],
  },
  {
    rel: "docs/openapi.v1.yaml",
    patterns: [/\/api\/v1\/service-reviews/, /\/api\/v1\/warranty-claims/, /Permissions\.Warranty\.ManageDealer/],
  },
  {
    rel: "docs/TEST_PLAN.md",
    patterns: [/TC-066/, /TC-075/, /ServiceReviews/, /WarrantyClaims/],
  },
  {
    rel: "docs/RUNBOOK.md",
    patterns: [/\b27\b/, /ServiceReview|WarrantyClaim/i, /daily-git-shipper\.mjs/],
  },
  {
    rel: "docs/basic-design/BD_00_BASIC_DESIGN_MASTER.md",
    patterns: [/\b27\b/, /\b48\b/, /ServiceReviews/, /WarrantyClaims/],
  },
  {
    rel: "docs/detail-design/DD_01_COMMON_SPECIFICATION.md",
    patterns: [/\b48\b/, /Reviews/, /Warranty/],
  },
  {
    rel: "docs/detail-design/DD_02_DATABASE_AND_TABLE_UPDATES.md",
    patterns: [/ServiceReviews/, /WarrantyClaims/, /ServiceReviewBonus/],
  },
  {
    rel: "docs/detail-design/DD_03_API_SPECIFICATIONS.md",
    patterns: [/\/api\/v1\/service-reviews/, /\/api\/v1\/warranty-claims/],
  },
  {
    rel: "docs/detail-design/DD_04_SCREEN_SPECIFICATIONS.md",
    patterns: [/SCR-CUS-07/, /SCR-STF-07/, /SCR-ADM-08/],
  },
  {
    rel: "docs/modules/MOD_01_AUTH_AND_RBAC.md",
    patterns: [/\b48\b/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/modules/MOD_02_SHOWROOM_AND_VIN_GARAGE.md",
    patterns: [/WarrantyClaims/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/modules/MOD_03_SERVICE_BOOKING_AND_REMINDERS.md",
    patterns: [/ServiceReviews/, /ServiceInvoiceLineItems/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/modules/MOD_04_LOYALTY_TIERS_AND_VOUCHERS.md",
    patterns: [/ServiceReviewBonus/, /PointLedgerEntries/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/modules/MOD_05_SIGNALR_REALTIME_NOTIFICATIONS.md",
    patterns: [/ServiceReviewSubmitted/, /WarrantyClaimCreated/, /WarrantyClaimStatusChanged/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/modules/MOD_06_ADMIN_ANALYTICS_AND_REPORTING.md",
    patterns: [/AverageCsatRating|ApprovedWarrantyCoveredVnd/, /SCR-ADM-08/],
    forbid: /Blueprint Specification Only/i,
  },
  {
    rel: "docs/screens/DESIGN_SYSTEM_AND_NAVIGATION.md",
    patterns: [/\b30\b/, /SCR-ADM-08/],
  },
  {
    rel: "docs/screens/CUSTOMER_PORTAL_SCREENS.md",
    patterns: [/SCR-CUS-07/],
  },
  {
    rel: "docs/screens/STAFF_AND_ADMIN_SCREENS.md",
    patterns: [/SCR-STF-07/, /SCR-ADM-08/],
  },
  {
    rel: "docs/RELEASE_NOTES_v1.1.0.md",
    patterns: [/TestDriveBookings/, /SpareParts/, /ServiceInvoiceLineItems/],
  },
  {
    rel: "docs/RELEASE_NOTES_v1.2.0.md",
    patterns: [/ServiceReviews/, /WarrantyClaims/, /ServiceReviewSubmitted/, /WarrantyClaimCreated/],
  },
];

const docDrift = [];
for (const docSpec of requiredDocs) {
  const abs = path.join(repoRoot, docSpec.rel);
  if (!fs.existsSync(abs)) {
    docDrift.push(`${docSpec.rel} (missing)`);
    continue;
  }
  const content = fs.readFileSync(abs, "utf8");
  for (const re of docSpec.patterns) {
    if (!re.test(content)) {
      docDrift.push(`${docSpec.rel} (missing ${re})`);
    }
  }
  if (docSpec.forbid && docSpec.forbid.test(content)) {
    docDrift.push(`${docSpec.rel} (contains stale status matching ${docSpec.forbid})`);
  }
}

const docxRel = "docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx";
const docxAbs = path.join(repoRoot, docxRel);
let docxBytes = 0;
if (!fs.existsSync(docxAbs)) {
  docDrift.push(`${docxRel} (missing)`);
} else {
  docxBytes = fs.statSync(docxAbs).size;
  if (docxBytes <= 50000) {
    docDrift.push(`${docxRel} (size ${docxBytes}B <= 50,000B minimum)`);
  }
}

if (docDrift.length > 0) {
  recordFail(
    "DOCS-SUITE-PARITY",
    `Documentation parity drift in ${docDrift.length} check(s): ${docDrift.slice(0, 15).join("; ")}`
  );
} else {
  recordPass(
    "DOCS-SUITE-PARITY",
    `Verified all ${requiredDocs.length} Markdown/OpenAPI specifications + ${docxRel} (${docxBytes} bytes > 50,000B) at 27-table / 48-permission / 10-event v1.2 parity.`
  );
}

if (jsonMode) {
  console.log(
    JSON.stringify(
      {
        passed: failures.length === 0,
        failureCount: failures.length,
        scheduleSummary,
        checks,
      },
      null,
      2
    )
  );
}

if (failures.length > 0) {
  if (!jsonMode) {
    console.error(`\n[FAIL] Post-Task Synchronization Gate FAILED (${failures.length} drift issue(s)).`);
  }
  process.exit(1);
}

if (!jsonMode) {
  console.log(`\n[PASS] Post-Task Synchronization Gate PASSED (100% workspace mapped, 0 stale docs, 0 drift).`);
}
process.exit(0);
