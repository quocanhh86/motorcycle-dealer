#!/usr/bin/env node
/**
 * replay-mar-sep-2026.mjs
 * Zero-dependency Node.js v22 ESM utility that validates and optionally replays
 * the 183-day Motorcycle Dealer & Loyalty Management Platform schedule
 * (2026-03-02 -> 2026-09-30) onto an isolated multi-branch orphan git history
 * with 11 chronological feature branches and --no-ff milestone merges.
 *
 * Safe defaults:
 *   - Default mode is --dry-run (read-only validation + branch/date preview).
 *   - --build-branch [branchName] builds into an isolated temporary git worktree
 *     (.git/replay-worktree-tmp), NEVER touches main/master/origin, NEVER alters
 *     the current working tree, and NEVER runs `git reset --hard`.
 */

import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const PLAN_PATH = path.join(__dirname, "git-6month-schedule-plan.json");
const GIT_DIR = path.join(REPO_ROOT, ".git");
const DEFAULT_TARGET_BRANCH = "history/mar-sep-2026";
const WORKTREE_TMP_DIR = path.join(GIT_DIR, "replay-worktree-tmp");

const STANDARD_WIN_TOOL_DIRS = [
  "C:\\Program Files\\GitHub CLI",
  "C:\\Program Files\\Git\\cmd",
  "C:\\Program Files\\Git\\bin",
  "C:\\Program Files\\nodejs",
];

function computeEnhancedPath(basePath = "") {
  const existing = basePath
    .split(path.delimiter)
    .map((s) => s.trim())
    .filter(Boolean);
  const lower = new Set(existing.map((s) => s.toLowerCase()));
  const prepend = [];
  for (const dir of STANDARD_WIN_TOOL_DIRS) {
    if (fs.existsSync(dir) && !lower.has(dir.toLowerCase())) {
      prepend.push(dir);
      lower.add(dir.toLowerCase());
    }
  }
  return [...prepend, ...existing].join(path.delimiter);
}

function buildChildEnv(extraEnv = {}) {
  const env = { ...process.env };
  const pathKeys = Object.keys(env).filter((k) => k.toUpperCase() === "PATH");
  const primaryPathKey = pathKeys[0] || "PATH";
  const currentPath = pathKeys.map((k) => env[k] || "").find(Boolean) || "";
  for (const key of pathKeys) {
    if (key !== primaryPathKey) delete env[key];
  }
  env[primaryPathKey] = computeEnhancedPath(currentPath);
  env.GIT_TERMINAL_PROMPT = "0";
  env.GCM_INTERACTIVE = "never";
  for (const [k, v] of Object.entries(extraEnv)) {
    if (k.toUpperCase() === "PATH") {
      env[primaryPathKey] = computeEnhancedPath(String(v || ""));
    } else {
      env[k] = v;
    }
  }
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
      cwd: REPO_ROOT,
      encoding: "utf8",
      windowsHide: true,
      env,
    });
    if (!res.error && res.status === 0) return bin;
  }
  throw new Error("Unable to locate git executable on PATH or standard Windows Git directories.");
}

const GIT_BIN = resolveGitBinary();

function runGit(args, options = {}) {
  const cwd = options.cwd || REPO_ROOT;
  const res = spawnSync(GIT_BIN, args, {
    cwd,
    encoding: "utf8",
    windowsHide: true,
    timeout: options.timeoutMs || 60000,
    maxBuffer: 32 * 1024 * 1024,
    input: options.input,
    env: buildChildEnv(options.env || {}),
  });
  if (res.error && !options.allowFailure) throw res.error;
  const status = typeof res.status === "number" ? res.status : 1;
  if (status !== 0 && !options.allowFailure) {
    const detail = (res.stderr || res.stdout || "").trim();
    throw new Error(`git ${args.join(" ")} failed (exit ${status})${detail ? `: ${detail}` : ""}`);
  }
  return {
    status,
    stdout: res.stdout || "",
    stderr: res.stderr || "",
    error: res.error || null,
  };
}

function parseCliArgs(argv) {
  const args = argv.slice(2);
  let mode = "dry-run";
  let targetBranch = DEFAULT_TARGET_BRANCH;
  let forceBranch = false;
  let keepFeatureBranches = false;
  let jsonMode = false;
  let help = false;

  for (let i = 0; i < args.length; i += 1) {
    const token = args[i];
    if (token === "--help" || token === "-h") {
      help = true;
    } else if (token === "--dry-run" || token === "-DryRun") {
      mode = "dry-run";
    } else if (token === "--build-branch") {
      mode = "build-branch";
      const next = args[i + 1];
      if (next && !next.startsWith("-")) {
        targetBranch = next.trim();
        i += 1;
      }
    } else if (token.startsWith("--build-branch=")) {
      mode = "build-branch";
      const val = token.slice("--build-branch=".length).trim();
      if (val) targetBranch = val;
    } else if (token === "--force-branch") {
      forceBranch = true;
    } else if (token === "--keep-feature-branches") {
      keepFeatureBranches = true;
    } else if (token === "--json") {
      jsonMode = true;
    } else {
      throw new Error(`Unknown CLI argument: ${token}. Run with --help for usage.`);
    }
  }

  return { mode, targetBranch, forceBranch, keepFeatureBranches, jsonMode, help };
}

function printHelp() {
  console.log(
    [
      "Motorcycle Dealer Platform — Mar–Sep 2026 Multi-Branch Git History Replay",
      "",
      "Usage:",
      "  node .agents/scripts/replay-mar-sep-2026.mjs --dry-run [--json]",
      "  node .agents/scripts/replay-mar-sep-2026.mjs --build-branch [branchName] [--force-branch] [--keep-feature-branches]",
      "",
      "Options:",
      "  --dry-run                (Default) Validate 100% file coverage, Mar–Sep 2026 dates, and 11-branch merge plan.",
      `  --build-branch [name]    Build isolated orphan multi-branch history (default: ${DEFAULT_TARGET_BRANCH}).`,
      "  --force-branch           Replace an existing non-protected target branch (never main/master/active HEAD).",
      "  --keep-feature-branches  Keep local feat/* branch refs after --no-ff merge into target branch.",
      "  --json                   Output machine-readable JSON summary in --dry-run mode.",
    ].join("\n")
  );
}

function isIsoDate(str) {
  return typeof str === "string" && /^\d{4}-\d{2}-\d{2}$/.test(str);
}

function isSundayUtc(ymd) {
  const [y, m, d] = ymd.split("-").map(Number);
  return new Date(Date.UTC(y, m - 1, d)).getUTCDay() === 0;
}

/**
 * Deterministic realistic ICT (+07:00) work timestamp between 09:10:00 and 21:15:00
 * so a closing --no-ff merge commit (+8..19m) remains <= 21:35:00 +07:00.
 */
function deterministicSliceTimestamp(dayEntry) {
  const seed = `${dayEntry.day}|${dayEntry.scheduledDate}|${dayEntry.taskId || ""}|${dayEntry.commitMessage || ""}`;
  const digest = crypto.createHash("sha256").update(seed).digest();
  const minMinute = 9 * 60 + 10; // 09:10
  const maxMinute = 21 * 60 + 15; // 21:15
  const span = maxMinute - minMinute + 1;
  const offsetMin = digest.readUInt32BE(0) % span;
  const totalMinutes = minMinute + offsetMin;
  const hh = String(Math.floor(totalMinutes / 60)).padStart(2, "0");
  const mm = String(totalMinutes % 60).padStart(2, "0");
  const ss = String(digest[4] % 60).padStart(2, "0");
  return {
    iso: `${dayEntry.scheduledDate}T${hh}:${mm}:${ss}+07:00`,
    totalMinutes,
    seconds: digest[4] % 60,
  };
}

function deterministicMergeTimestamp(closingDayEntry, branchName) {
  const base = deterministicSliceTimestamp(closingDayEntry);
  const digest = crypto.createHash("sha256").update(`merge|${branchName}|${closingDayEntry.day}`).digest();
  const addMinutes = 8 + (digest[0] % 12); // +8..+19 minutes (always <= 21:34)
  const totalMinutes = Math.min(21 * 60 + 35, base.totalMinutes + addMinutes);
  const hh = String(Math.floor(totalMinutes / 60)).padStart(2, "0");
  const mm = String(totalMinutes % 60).padStart(2, "0");
  const ss = String(digest[1] % 60).padStart(2, "0");
  return `${closingDayEntry.scheduledDate}T${hh}:${mm}:${ss}+07:00`;
}

function getTrackableWorkspaceFiles() {
  const lsRes = runGit(["ls-files", "-z", "--cached", "--others", "--exclude-standard"]);
  return Array.from(
    new Set(
      lsRes.stdout
        .split("\0")
        .map((p) => p.trim().replace(/\\/g, "/"))
        .filter(Boolean)
    )
  ).sort((a, b) => a.localeCompare(b));
}

function loadAndValidatePlan() {
  if (!fs.existsSync(PLAN_PATH)) {
    throw new Error(`Missing schedule plan at ${PLAN_PATH}`);
  }
  const plan = JSON.parse(fs.readFileSync(PLAN_PATH, "utf8"));
  const days = Array.isArray(plan.days) ? plan.days : [];
  const win = plan.scheduleWindow || {};
  const totalDays = Number(win.totalDays || 183);
  const trackableFiles = getTrackableWorkspaceFiles();
  const trackableSet = new Set(trackableFiles);

  const errors = [];

  if (win.startDate !== "2026-03-02" || win.endDate !== "2026-09-30") {
    errors.push(`scheduleWindow expected 2026-03-02..2026-09-30, got ${win.startDate}..${win.endDate}.`);
  }
  if (days.length !== totalDays) {
    errors.push(`Expected ${totalDays} scheduled slices, got ${days.length}.`);
  }

  const allMapped = new Set();
  const missingOnDisk = [];
  const nonAscendingDates = [];
  const sundayDates = [];
  const emptyReplaySlices = [];
  const seenReplayPaths = new Set();
  const branchSegments = [];
  const seenFeatureBranches = new Set();
  const fragmentedBranches = [];

  let prevDate = null;
  let currentSegment = null;

  for (let i = 0; i < days.length; i += 1) {
    const d = days[i];
    const expectedDayNum = i + 1;
    if (d.day !== expectedDayNum) {
      errors.push(`Slice index ${i} expected day=${expectedDayNum}, got day=${d.day}.`);
    }

    if (!isIsoDate(d.scheduledDate)) {
      errors.push(`Day ${d.day} has invalid scheduledDate: ${String(d.scheduledDate)}.`);
    } else {
      if (d.scheduledDate < "2026-03-02" || d.scheduledDate > "2026-09-30") {
        errors.push(`Day ${d.day} scheduledDate ${d.scheduledDate} falls outside 2026-03-02..2026-09-30.`);
      }
      if (prevDate && d.scheduledDate <= prevDate) {
        nonAscendingDates.push(`Day ${d.day} (${d.scheduledDate} <= ${prevDate})`);
      }
      if (isSundayUtc(d.scheduledDate)) {
        sundayDates.push(`Day ${d.day} (${d.scheduledDate})`);
      }
      prevDate = d.scheduledDate;
    }

    const relPaths = Array.isArray(d.expectedPaths)
      ? d.expectedPaths.map((p) => String(p).replace(/\\/g, "/"))
      : [];

    let newOnReplay = 0;
    for (const rel of relPaths) {
      allMapped.add(rel);
      if ((d.sliceKind || "existing-workspace") === "existing-workspace") {
        if (!fs.existsSync(path.join(REPO_ROOT, rel))) {
          missingOnDisk.push(`Day ${String(d.day).padStart(3, "0")}: ${rel}`);
        }
      }
      if (!seenReplayPaths.has(rel)) {
        seenReplayPaths.add(rel);
        newOnReplay += 1;
      }
    }

    if (newOnReplay === 0) {
      emptyReplaySlices.push(`Day ${String(d.day).padStart(3, "0")} (${d.taskId || "no-task"})`);
    }

    const branchName = String(d.branch || "").trim();
    if (!branchName) {
      errors.push(`Day ${d.day} is missing a 'branch' annotation.`);
      continue;
    }

    // Day 1 on 'main' is the orphan root bootstrap commit; all feat/* days group into contiguous segments
    if (d.day === 1 && branchName === "main") {
      continue;
    }

    if (!currentSegment || currentSegment.branch !== branchName) {
      if (seenFeatureBranches.has(branchName)) {
        fragmentedBranches.push(`${branchName} (re-entered at Day ${d.day})`);
      }
      seenFeatureBranches.add(branchName);
      currentSegment = {
        index: branchSegments.length + 1,
        branch: branchName,
        startDay: d.day,
        endDay: d.day,
        openDate: d.scheduledDate,
        mergeDate: d.scheduledDate,
        days: [d],
        uniqueFiles: new Set(relPaths),
      };
      branchSegments.push(currentSegment);
    } else {
      currentSegment.endDay = d.day;
      currentSegment.mergeDate = d.scheduledDate;
      currentSegment.days.push(d);
      for (const rel of relPaths) currentSegment.uniqueFiles.add(rel);
    }
  }

  const unmappedFiles = trackableFiles.filter((f) => !allMapped.has(f));
  const nonTrackableMapped = Array.from(allMapped).filter((p) => !trackableSet.has(p));

  if (unmappedFiles.length > 0) {
    errors.push(`Found ${unmappedFiles.length} unmapped git-trackable file(s): ${unmappedFiles.slice(0, 12).join(", ")}`);
  }
  if (missingOnDisk.length > 0) {
    errors.push(`Found ${missingOnDisk.length} mapped file(s) missing on disk: ${missingOnDisk.slice(0, 12).join("; ")}`);
  }
  if (nonTrackableMapped.length > 0) {
    errors.push(`Found ${nonTrackableMapped.length} mapped path(s) ignored or not git-trackable: ${nonTrackableMapped.slice(0, 12).join(", ")}`);
  }
  if (nonAscendingDates.length > 0) {
    errors.push(`Non-ascending scheduledDate entries: ${nonAscendingDates.slice(0, 10).join(", ")}`);
  }
  if (fragmentedBranches.length > 0) {
    errors.push(`Non-contiguous feature branch segments: ${fragmentedBranches.join(", ")}`);
  }
  if (emptyReplaySlices.length > 0) {
    errors.push(`Slices with 0 newly added files on fresh replay: ${emptyReplaySlices.join(", ")}`);
  }

  const bootstrapDay = days[0] && days[0].branch === "main" ? days[0] : null;
  const totalCommits = days.length + branchSegments.length;

  return {
    plan,
    days,
    bootstrapDay,
    branchSegments,
    trackableFiles,
    mappedUniqueCount: allMapped.size,
    unmappedFiles,
    missingOnDisk,
    sundayDates,
    emptyReplaySlices,
    totalCommits,
    errors,
  };
}

function printDryRunReport(validation, targetBranch, jsonMode) {
  const {
    days,
    bootstrapDay,
    branchSegments,
    trackableFiles,
    mappedUniqueCount,
    unmappedFiles,
    missingOnDisk,
    sundayDates,
    emptyReplaySlices,
    totalCommits,
    errors,
  } = validation;

  const firstDate = days[0]?.scheduledDate || "";
  const lastDate = days[days.length - 1]?.scheduledDate || "";

  if (jsonMode) {
    console.log(
      JSON.stringify(
        {
          passed: errors.length === 0,
          targetBranch,
          dateRange: { startDate: firstDate, endDate: lastDate },
          sliceCommits: days.length,
          featureBranchCount: branchSegments.length,
          mergeCommits: branchSegments.length,
          totalCommits,
          trackableFilesCount: trackableFiles.length,
          mappedUniqueCount,
          unmappedCount: unmappedFiles.length,
          missingOnDiskCount: missingOnDisk.length,
          sundayCount: sundayDates.length,
          emptyReplaySlicesCount: emptyReplaySlices.length,
          bootstrapCommit: bootstrapDay
            ? { day: bootstrapDay.day, date: bootstrapDay.scheduledDate, files: bootstrapDay.expectedPaths.length }
            : null,
          branches: branchSegments.map((seg) => ({
            index: seg.index,
            branch: seg.branch,
            dayRange: `${seg.startDay}..${seg.endDay}`,
            commits: seg.days.length,
            filesTouched: seg.uniqueFiles.size,
            openDate: seg.openDate,
            mergeDate: seg.mergeDate,
          })),
          errors,
        },
        null,
        2
      )
    );
    return;
  }

  console.log("=== Motorcycle Dealer — Mar–Sep 2026 Multi-Branch Replay [DRY-RUN] ===");
  console.log(`Target Orphan Branch : ${targetBranch}`);
  console.log(`Schedule Window      : ${firstDate} -> ${lastDate} (${days.length} working slices, ${sundayDates.length} Sundays)`);
  console.log(`Commit Graph Totals  : ${days.length} slice commits + ${branchSegments.length} --no-ff merges = ${totalCommits} commits`);
  console.log(
    `File Coverage        : ${trackableFiles.length} trackable / ${mappedUniqueCount} mapped (unmapped=${unmappedFiles.length}, missing=${missingOnDisk.length}, emptySlices=${emptyReplaySlices.length})`
  );
  if (bootstrapDay) {
    console.log(
      `Root Bootstrap       : Day 001 (${bootstrapDay.scheduledDate}) on ${targetBranch} [${bootstrapDay.expectedPaths.length} files]`
    );
  }
  console.log("");
  console.log(`Feature Branches (${branchSegments.length} contiguous epics -> --no-ff merges):`);
  console.log("  #  Branch                                Days       Commits  Files  Open Date   Merge Date (--no-ff)");
  console.log("  -- ------------------------------------- ---------- -------  -----  ----------  --------------------");
  for (const seg of branchSegments) {
    const idx = String(seg.index).padStart(2, " ");
    const name = seg.branch.padEnd(37, " ");
    const range = `${String(seg.startDay).padStart(3, "0")}..${String(seg.endDay).padStart(3, "0")}`.padEnd(10, " ");
    const cnt = String(seg.days.length).padStart(7, " ");
    const files = String(seg.uniqueFiles.size).padStart(5, " ");
    console.log(`  ${idx} ${name} ${range} ${cnt}  ${files}  ${seg.openDate}  ${seg.mergeDate}`);
  }
  console.log("");

  if (errors.length > 0) {
    for (const err of errors) {
      console.error(`[FAIL] ${err}`);
    }
    console.error(`\n[FAIL] Dry-run validation failed with ${errors.length} error(s).`);
  } else {
    console.log(
      `[PASS] Dry-run validation PASSED (100% of ${trackableFiles.length} trackable files covered, ${branchSegments.length} feature branches, ${totalCommits} commits across ${firstDate}..${lastDate}).`
    );
    console.log(
      `[INFO] Ready to build when approved: node .agents/scripts/replay-mar-sep-2026.mjs --build-branch ${targetBranch}`
    );
  }
}

function getCurrentHeadBranch() {
  const sym = runGit(["symbolic-ref", "--quiet", "--short", "HEAD"], { allowFailure: true });
  return sym.status === 0 ? sym.stdout.trim() : "";
}

function verifyBranchTreeMatchesWorkspace(targetBranch, trackableFiles) {
  const lsTree = runGit(["ls-tree", "-r", "-z", targetBranch]);
  const branchBlobs = new Map();
  for (const entry of lsTree.stdout.split("\0")) {
    if (!entry) continue;
    const tabIdx = entry.indexOf("\t");
    if (tabIdx < 0) continue;
    const meta = entry.slice(0, tabIdx).split(/\s+/);
    const sha = meta[2];
    const rel = entry.slice(tabIdx + 1).replace(/\\/g, "/");
    branchBlobs.set(rel, sha);
  }

  const missingInBranch = [];
  const extraInBranch = [];
  const mismatchedContent = [];

  const trackableSet = new Set(trackableFiles);
  for (const rel of branchBlobs.keys()) {
    if (!trackableSet.has(rel)) extraInBranch.push(rel);
  }

  // Batch-hash all workspace files in one git hash-object --stdin-paths call
  const hashRes = runGit(["hash-object", "--stdin-paths"], {
    input: `${trackableFiles.join("\n")}\n`,
  });
  const hashes = hashRes.stdout.trim().split(/\r?\n/);

  for (let i = 0; i < trackableFiles.length; i += 1) {
    const rel = trackableFiles[i];
    const wsSha = (hashes[i] || "").trim();
    if (!branchBlobs.has(rel)) {
      missingInBranch.push(rel);
    } else if (branchBlobs.get(rel) !== wsSha) {
      mismatchedContent.push(rel);
    }
  }

  return {
    branchFileCount: branchBlobs.size,
    workspaceFileCount: trackableFiles.length,
    missingInBranch,
    extraInBranch,
    mismatchedContent,
  };
}

function copySliceFilesToWorktree(entry, worktreeDir) {
  const relPaths = (entry.expectedPaths || []).map((p) => String(p).replace(/\\/g, "/"));
  for (const rel of relPaths) {
    const srcAbs = path.join(REPO_ROOT, rel);
    const dstAbs = path.join(worktreeDir, rel);
    fs.mkdirSync(path.dirname(dstAbs), { recursive: true });
    fs.copyFileSync(srcAbs, dstAbs);
  }
  return relPaths;
}

function commitSliceInWorktree(entry, totalDays, worktreeDir, allowEmptyFallback = true) {
  const relPaths = copySliceFilesToWorktree(entry, worktreeDir);
  runGit(["add", "--", ...relPaths], { cwd: worktreeDir });

  const ts = deterministicSliceTimestamp(entry);
  const trailers = [
    `X-Schedule-Day: ${entry.day}/${totalDays}`,
    `X-Schedule-Date: ${entry.scheduledDate}`,
    `X-Schedule-Task: ${entry.taskId || "N/A"}`,
    `X-Schedule-Branch: ${entry.branch || "main"}`,
  ].join("\n");

  const hasDelta = runGit(["diff", "--cached", "--quiet"], { cwd: worktreeDir, allowFailure: true }).status !== 0;
  const commitArgs = ["commit", "-m", entry.commitMessage, "-m", trailers];
  if (!hasDelta) {
    if (!allowEmptyFallback) {
      throw new Error(`Day ${entry.day} produced zero staged delta during replay.`);
    }
    commitArgs.push("--allow-empty");
  }

  runGit(commitArgs, {
    cwd: worktreeDir,
    env: {
      GIT_AUTHOR_DATE: ts.iso,
      GIT_COMMITTER_DATE: ts.iso,
    },
  });
}

function buildReplayBranch(validation, opts) {
  const { targetBranch, forceBranch, keepFeatureBranches } = opts;
  const { days, bootstrapDay, branchSegments, trackableFiles } = validation;

  const activeBranch = getCurrentHeadBranch();
  const protectedBranches = new Set(["main", "master", activeBranch].filter(Boolean));
  if (protectedBranches.has(targetBranch)) {
    throw new Error(
      `Refusing to build onto protected/active branch '${targetBranch}'. Choose an isolated branch such as '${DEFAULT_TARGET_BRANCH}'.`
    );
  }

  const branchCheck = runGit(["rev-parse", "--verify", `refs/heads/${targetBranch}`], { allowFailure: true });
  if (branchCheck.status === 0) {
    if (!forceBranch) {
      throw new Error(
        `Branch '${targetBranch}' already exists. Pass --force-branch to rebuild it, or specify a new branch name.`
      );
    }
    runGit(["branch", "-D", targetBranch]);
  }

  const createdTempRefs = [];
  const totalDays = days.length;

  const cleanupWorktree = () => {
    if (fs.existsSync(WORKTREE_TMP_DIR)) {
      runGit(["worktree", "remove", "--force", WORKTREE_TMP_DIR], { allowFailure: true });
      if (fs.existsSync(WORKTREE_TMP_DIR)) {
        try {
          fs.rmSync(WORKTREE_TMP_DIR, { recursive: true, force: true, maxRetries: 10, retryDelay: 300 });
        } catch (cleanupErr) {
          console.warn(
            `[WARN] Temporary worktree cleanup hit Windows file lock on '${WORKTREE_TMP_DIR}' (${cleanupErr instanceof Error ? cleanupErr.message : String(cleanupErr)}); continuing post-build verification.`
          );
        }
      }
    }
    runGit(["worktree", "prune"], { allowFailure: true });
    for (const refName of createdTempRefs) {
      runGit(["branch", "-D", refName], { allowFailure: true });
    }
  };

  console.log(`[INFO] Building isolated Mar–Sep 2026 multi-branch history on '${targetBranch}'...`);
  console.log(`[INFO] Temporary worktree: ${WORKTREE_TMP_DIR} (current working tree & 'main' untouched)`);

  try {
    cleanupWorktree();

    const hasHead = runGit(["rev-parse", "--verify", "HEAD"], { allowFailure: true }).status === 0;
    if (hasHead) {
      runGit(["worktree", "add", "--no-checkout", "--detach", WORKTREE_TMP_DIR, "HEAD"]);
    } else {
      runGit(["worktree", "add", "--orphan", "-b", targetBranch, WORKTREE_TMP_DIR]);
    }

    if (hasHead) {
      runGit(["checkout", "--orphan", targetBranch], { cwd: WORKTREE_TMP_DIR });
      runGit(["read-tree", "--empty"], { cwd: WORKTREE_TMP_DIR });
    }

    // 1. Root bootstrap commit (Day 1) directly on targetBranch
    if (bootstrapDay) {
      commitSliceInWorktree(bootstrapDay, totalDays, WORKTREE_TMP_DIR, false);
    }

    // 2. Replay all 11 contiguous feature branches and merge each with --no-ff
    for (const seg of branchSegments) {
      const slug = seg.branch.replace(/[^a-zA-Z0-9._/-]/g, "-");
      const workingRef = keepFeatureBranches ? slug : `replay-tmp/${slug}`;

      if (!keepFeatureBranches) {
        runGit(["branch", "-D", workingRef], { allowFailure: true });
        createdTempRefs.push(workingRef);
      } else {
        const exists = runGit(["rev-parse", "--verify", `refs/heads/${workingRef}`], { allowFailure: true }).status === 0;
        if (exists) {
          if (!forceBranch) {
            throw new Error(`Feature branch '${workingRef}' already exists. Pass --force-branch or omit --keep-feature-branches.`);
          }
          runGit(["branch", "-D", workingRef]);
        }
      }

      runGit(["checkout", "-b", workingRef], { cwd: WORKTREE_TMP_DIR });

      for (const dayEntry of seg.days) {
        commitSliceInWorktree(dayEntry, totalDays, WORKTREE_TMP_DIR, false);
      }

      runGit(["checkout", targetBranch], { cwd: WORKTREE_TMP_DIR });

      const closingDay = seg.days[seg.days.length - 1];
      const mergeTs = deterministicMergeTimestamp(closingDay, seg.branch);
      const mergeSubject = `Merge branch '${seg.branch}' into ${targetBranch}`;
      const mergeBody = [
        `Milestone merge: ${seg.branch} (Days ${String(seg.startDay).padStart(3, "0")}..${String(seg.endDay).padStart(3, "0")})`,
        `Window: ${seg.openDate} -> ${seg.mergeDate} (${seg.days.length} commits, ${seg.uniqueFiles.size} files)`,
      ].join("\n");

      runGit(["merge", "--no-ff", workingRef, "-m", mergeSubject, "-m", mergeBody], {
        cwd: WORKTREE_TMP_DIR,
        env: {
          GIT_AUTHOR_DATE: mergeTs,
          GIT_COMMITTER_DATE: mergeTs,
        },
      });

      if (!keepFeatureBranches) {
        runGit(["branch", "-d", workingRef], { cwd: WORKTREE_TMP_DIR, allowFailure: true });
        const idx = createdTempRefs.indexOf(workingRef);
        if (idx >= 0) createdTempRefs.splice(idx, 1);
      }
    }
  } finally {
    cleanupWorktree();
  }

  // 3. Post-build verification: 100% byte-identical tree parity with workspace + git diff --stat HEAD <branch>
  const parity = verifyBranchTreeMatchesWorkspace(targetBranch, trackableFiles);
  const headDiff = runGit(["diff", "--stat", "HEAD", targetBranch], { allowFailure: true });
  const commitCount = Number.parseInt(runGit(["rev-list", "--count", targetBranch]).stdout.trim(), 10) || 0;

  if (
    parity.missingInBranch.length > 0 ||
    parity.extraInBranch.length > 0 ||
    parity.mismatchedContent.length > 0
  ) {
    throw new Error(
      `Tree parity mismatch on '${targetBranch}': missing=${parity.missingInBranch.length}, extra=${parity.extraInBranch.length}, contentDiff=${parity.mismatchedContent.length}`
    );
  }

  console.log(
    `[PASS] Built '${targetBranch}' with ${commitCount} commits (${days.length} slices + ${branchSegments.length} --no-ff merges) across ${days[0].scheduledDate}..${days[days.length - 1].scheduledDate}.`
  );
  console.log(
    `[PASS] Verified 100% byte-identical tree parity with workspace (${parity.branchFileCount}/${parity.workspaceFileCount} files, 0 missing, 0 extra, 0 content diffs).`
  );

  const headDiffOut = headDiff.stdout.trim();
  if (!headDiffOut) {
    console.log(`[PASS] git diff --stat HEAD ${targetBranch} is completely empty (0 diffs).`);
  } else {
    const diffLines = headDiffOut.split(/\r?\n/);
    console.log(
      `[INFO] git diff --stat HEAD ${targetBranch}: ${diffLines[diffLines.length - 1]} (expected while 'main' HEAD is still staged at early schedule day; workspace tree parity is 100%).`
    );
  }

  console.log("");
  console.log("=== Safe Inspection & Optional Push Commands ===");
  console.log(`  git log --graph --oneline --date=short --format="%h %ad %d %s" ${targetBranch} | head -n 30`);
  console.log(`  git log --merges --date=short --format="%h %ad %s" ${targetBranch}`);
  console.log(`  git diff --stat HEAD ${targetBranch}`);
  console.log(`  # Optional (publish to a dedicated remote branch without rewriting origin/main):`);
  console.log(`  git push -u origin ${targetBranch}`);
  console.log(`  # Optional (only when ready & explicitly approved by user to publish as new main):`);
  console.log(`  git checkout -B main ${targetBranch} && git push --force-with-lease origin main`);
}

function main() {
  if (!fs.existsSync(GIT_DIR)) {
    throw new Error(`Not a git repository: ${REPO_ROOT}`);
  }
  const opts = parseCliArgs(process.argv);
  if (opts.help) {
    printHelp();
    return;
  }

  const validation = loadAndValidatePlan();

  if (opts.mode === "dry-run") {
    printDryRunReport(validation, opts.targetBranch, opts.jsonMode);
    if (validation.errors.length > 0) process.exit(1);
    return;
  }

  if (validation.errors.length > 0) {
    printDryRunReport(validation, opts.targetBranch, false);
    process.exit(1);
  }

  buildReplayBranch(validation, opts);
}

try {
  main();
} catch (err) {
  console.error(`[FAIL] replay-mar-sep-2026 error: ${err instanceof Error ? err.message : String(err)}`);
  process.exit(1);
}
