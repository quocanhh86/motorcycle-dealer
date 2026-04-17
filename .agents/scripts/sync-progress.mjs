#!/usr/bin/env node
/**
 * sync-progress.mjs
 * Zero-dependency Node ESM utility (--check | --write | --dry-run) that parses all Master WBS
 * task checkboxes (T-101..T-708) in TASKS.md, validates counts against baseline-spec-snapshot.json,
 * computes per-phase and overall completion counts/percentages, and verifies (or updates)
 * TASKS.md Section 2 & PROGRESS.md consistency.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");

const tasksPath = path.join(repoRoot, "TASKS.md");
const progressPath = path.join(repoRoot, "PROGRESS.md");
const snapshotPath = path.join(__dirname, "baseline-spec-snapshot.json");

const args = new Set(process.argv.slice(2));
const writeMode = args.has("--write");
const checkMode = args.has("--check") || args.has("--dry-run") || !writeMode;

if (!fs.existsSync(tasksPath)) {
  console.error(`[FAIL] TASKS.md not found at ${tasksPath}`);
  process.exit(1);
}
if (!fs.existsSync(progressPath)) {
  console.error(`[FAIL] PROGRESS.md not found at ${progressPath}`);
  process.exit(1);
}

const tasksRaw = fs.readFileSync(tasksPath, "utf8");
const progressRaw = fs.readFileSync(progressPath, "utf8");

let maxPhase = 10;
let expectedTotalTasks = 88;
if (fs.existsSync(snapshotPath)) {
  const snapshot = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
  if (Array.isArray(snapshot.phases) && snapshot.phases.length > 0) {
    maxPhase = Math.max(...snapshot.phases.map((p) => Number(p.phaseNumber || 0)));
    expectedTotalTasks = snapshot.phases.reduce((sum, p) => sum + Number(p.taskCount || 0), 0);
  }
}

// Parse all WBS task checkboxes in TASKS.md: - [x] **`T-101` ...** or - [ ] **`T-1001` ...**
const taskRegex = /^-\s*\[([ xX])\]\s*\*\*`(T-([1-9]\d*)\d{2})`/gm;
const phases = new Map();
for (let p = 1; p <= maxPhase; p++) {
  phases.set(p, { phase: p, total: 0, done: 0, pending: 0, ids: [] });
}

const seenIds = new Set();
let match;
while ((match = taskRegex.exec(tasksRaw)) !== null) {
  const checked = match[1].toLowerCase() === "x";
  const taskId = match[2];
  const phaseNum = Number(match[3]);

  if (seenIds.has(taskId)) {
    console.error(`[FAIL] Duplicate task ID detected in TASKS.md: ${taskId}`);
    process.exit(1);
  }
  seenIds.add(taskId);

  if (!phases.has(phaseNum)) {
    phases.set(phaseNum, { phase: phaseNum, total: 0, done: 0, pending: 0, ids: [] });
  }
  const bucket = phases.get(phaseNum);
  bucket.total += 1;
  if (checked) {
    bucket.done += 1;
  } else {
    bucket.pending += 1;
  }
  bucket.ids.push(taskId);
}

const totalTasks = Array.from(phases.values()).reduce((sum, b) => sum + b.total, 0);
const totalDone = Array.from(phases.values()).reduce((sum, b) => sum + b.done, 0);
const totalPending = totalTasks - totalDone;
const overallPct = totalTasks > 0 ? ((totalDone / totalTasks) * 100).toFixed(1) : "0.0";

if (totalTasks !== expectedTotalTasks) {
  console.error(
    `[FAIL] Expected ${expectedTotalTasks} Master WBS tasks (from baseline-spec-snapshot.json) in TASKS.md, parsed ${totalTasks}.`
  );
  process.exit(1);
}

console.log(
  `[INFO] Parsed TASKS.md checkboxes: ${totalDone} / ${totalTasks} completed (${overallPct}%), ${totalPending} pending.`
);
for (const b of phases.values()) {
  const pPct = b.total > 0 ? Math.round((b.done / b.total) * 100) : 0;
  console.log(`  - Phase ${b.phase}: ${b.done} / ${b.total} (${pPct}%)`);
}

if (writeMode) {
  let updatedTasks = tasksRaw;
  for (const b of phases.values()) {
    const rowRe = new RegExp(
      `^(\\|\\s*\\*\\*Phase\\s+${b.phase}\\*\\*\\s*\\|[^|]+\\|[^|]+\\|)\\s*\\d+\\s*\\|\\s*\\d+\\s*\\|\\s*\\d+\\s*(\\|.*)$`,
      "m"
    );
    updatedTasks = updatedTasks.replace(rowRe, `$1 ${b.total} | ${b.done} | ${b.pending} $2`);
  }
  const totalRowRe =
    /^(\|\s*\*\*Total\*\*\s*\|[^|]+\|[^|]+\|)\s*\*\*\d+\*\*\s*\|\s*\*\*\d+\*\*\s*\|\s*\*\*\d+\*\*\s*(\|.*)$/m;
  updatedTasks = updatedTasks.replace(
    totalRowRe,
    `$1 **${totalTasks}** | **${totalDone}** | **${totalPending}** $2`
  );

  let updatedProgress = progressRaw;
  updatedProgress = updatedProgress.replace(
    /^(\|\s*\*\*Overall Roadmap Completion\*\*\s*\|\s*)\*\*`[\d.]+%`\*\*\s*\(`\d+\s*\/\s*\d+`(\s*Master WBS Tasks Completed\)\s*\|)$/m,
    `$1**\`${overallPct}%\`** (\`${totalDone} / ${totalTasks}\`$2`
  );

  for (const b of phases.values()) {
    const pctInt = b.total > 0 ? Math.round((b.done / b.total) * 100) : 0;
    const gateRowRe = new RegExp(
      `^(\\|\\s*\\*\\*Phase\\s+${b.phase}\\*\\*\\s*\\|[^|]+\\|\\s*\`T-${b.phase}\\d{2}\`\\.\\.\`T-${b.phase}\\d{2}\`\\s*\\|\\s*)\`\\d+\\s*\\/\\s*\\d+\`\\s*\\(\`\\d+%\`\\)(\\s*\\|.*)$`,
      "m"
    );
    updatedProgress = updatedProgress.replace(
      gateRowRe,
      `$1\`${b.done} / ${b.total}\` (\`${pctInt}%\`)$2`
    );
  }

  if (updatedTasks !== tasksRaw) {
    fs.writeFileSync(tasksPath, updatedTasks, "utf8");
    console.log("[PASS] Updated TASKS.md summary table.");
  }
  if (updatedProgress !== progressRaw) {
    fs.writeFileSync(progressPath, updatedProgress, "utf8");
    console.log("[PASS] Updated PROGRESS.md executive metrics and phase gate table.");
  }
}

// Verification (--check or post --write validation)
const checkTasksRaw = writeMode ? fs.readFileSync(tasksPath, "utf8") : tasksRaw;
const checkProgressRaw = writeMode ? fs.readFileSync(progressPath, "utf8") : progressRaw;
const mismatches = [];

// 1. Check TASKS.md Section 2 summary rows
for (const b of phases.values()) {
  const rowRe = new RegExp(
    `^\\|\\s*\\*\\*Phase\\s+${b.phase}\\*\\*\\s*\\|[^|]+\\|[^|]+\\|\\s*(\\d+)\\s*\\|\\s*(\\d+)\\s*\\|\\s*(\\d+)\\s*\\|`,
    "m"
  );
  const m = checkTasksRaw.match(rowRe);
  if (!m) {
    mismatches.push(`TASKS.md summary row missing for Phase ${b.phase}.`);
  } else {
    const [, tTotal, tDone, tPending] = m.map(Number);
    if (tTotal !== b.total || tDone !== b.done || tPending !== b.pending) {
      mismatches.push(
        `TASKS.md Phase ${b.phase} summary row (${tTotal}/${tDone}/${tPending}) != checkboxes (${b.total}/${b.done}/${b.pending}).`
      );
    }
  }
}

const totalRowMatch = checkTasksRaw.match(
  /^\|\s*\*\*Total\*\*\s*\|[^|]+\|[^|]+\|\s*\*\*(\d+)\*\*\s*\|\s*\*\*(\d+)\*\*\s*\|\s*\*\*(\d+)\*\*\s*\|/m
);
if (!totalRowMatch) {
  mismatches.push("TASKS.md Total summary row not found.");
} else {
  const [, rTotal, rDone, rPending] = totalRowMatch.map(Number);
  if (rTotal !== totalTasks || rDone !== totalDone || rPending !== totalPending) {
    mismatches.push(
      `TASKS.md Total row (${rTotal}/${rDone}/${rPending}) != checkboxes (${totalTasks}/${totalDone}/${totalPending}).`
    );
  }
}

// 2. Check PROGRESS.md Overall Roadmap Completion row
const overallLine = checkProgressRaw
  .split(/\r?\n/)
  .find((l) => l.includes("**Overall Roadmap Completion**"));

if (!overallLine) {
  mismatches.push("PROGRESS.md missing '**Overall Roadmap Completion**' row.");
} else {
  const countMatch = overallLine.match(/(\d+)\s*\/\s*(\d+)/);
  const pctMatch = overallLine.match(/(\d+(?:\.\d+)?)%/);
  if (!countMatch || !pctMatch) {
    mismatches.push(`PROGRESS.md Overall Roadmap Completion format unrecognized: ${overallLine}`);
  } else {
    const pDone = Number(countMatch[1]);
    const pTotal = Number(countMatch[2]);
    const pPct = parseFloat(pctMatch[1]);
    const expectedPctNum = parseFloat(overallPct);
    if (pDone !== totalDone || pTotal !== totalTasks) {
      mismatches.push(
        `PROGRESS.md Overall Roadmap Completion (${pDone} / ${pTotal}) != TASKS.md checkboxes (${totalDone} / ${totalTasks}).`
      );
    }
    if (Math.abs(pPct - expectedPctNum) > 0.15) {
      mismatches.push(
        `PROGRESS.md Overall Roadmap Completion percentage (${pPct}%) != expected (${overallPct}%).`
      );
    }
  }
}

// 3. Check PROGRESS.md Section 2 Phase-by-Phase Gate Status Table
for (const b of phases.values()) {
  const gateLineRe = new RegExp(
    `^\\|\\s*\\*\\*Phase\\s+${b.phase}\\*\\*\\s*\\|[^|]+\\|[^|]*T-${b.phase}\\d{2}[^|]*\\|([^|]+)\\|`,
    "m"
  );
  const gm = checkProgressRaw.match(gateLineRe);
  if (!gm) {
    mismatches.push(`PROGRESS.md Gate Status row missing for Phase ${b.phase}.`);
  } else {
    const progressCell = gm[1];
    const cm = progressCell.match(/(\d+)\s*\/\s*(\d+)/);
    if (!cm) {
      mismatches.push(`PROGRESS.md Phase ${b.phase} progress cell missing 'done / total': ${progressCell}`);
    } else {
      const cellDone = Number(cm[1]);
      const cellTotal = Number(cm[2]);
      if (cellDone !== b.done || cellTotal !== b.total) {
        mismatches.push(
          `PROGRESS.md Phase ${b.phase} progress (${cellDone} / ${cellTotal}) != TASKS.md checkboxes (${b.done} / ${b.total}).`
        );
      }
    }
  }
}

if (mismatches.length > 0 && checkMode) {
  for (const err of mismatches) {
    console.error(`[FAIL] ${err}`);
  }
  console.error(
    `[FAIL] WBS <-> PROGRESS synchronization check failed (${mismatches.length} mismatch(es)). Run 'node .agents/scripts/sync-progress.mjs --write' to reconcile.`
  );
  process.exit(1);
}

console.log(
  `[PASS] TASKS.md and PROGRESS.md are 100% synchronized (${totalDone} / ${totalTasks} tasks = ${overallPct}%).`
);
process.exit(0);
