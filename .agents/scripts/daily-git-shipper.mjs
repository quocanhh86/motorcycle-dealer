import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const REPO_ROOT = path.resolve(__dirname, '../..');
const PLAN_PATH = path.join(__dirname, 'git-6month-schedule-plan.json');
const GIT_DIR = path.join(REPO_ROOT, '.git');
const SHIPPER_STATE_DIR = path.join(GIT_DIR, 'daily-shipper');
const HISTORY_LOG_PATH = path.join(SHIPPER_STATE_DIR, 'shipper-history.jsonl');
const LAST_RUN_PATH = path.join(SHIPPER_STATE_DIR, 'last-run.json');

function resolveGitExecutable() {
  const candidates = [
    'git',
    'C:\\Program Files\\Git\\cmd\\git.exe',
    'C:\\Program Files\\Git\\bin\\git.exe'
  ];
  for (const bin of candidates) {
    const res = spawnSync(bin, ['--version'], {
      cwd: REPO_ROOT,
      encoding: 'utf8',
      windowsHide: true
    });
    if (!res.error && res.status === 0) {
      return bin;
    }
  }
  throw new Error('Unable to locate git executable on PATH or standard Windows Git directories.');
}

const GIT_BIN = resolveGitExecutable();

function runGit(args, options = {}) {
  const env = {
    ...process.env,
    GIT_TERMINAL_PROMPT: '0',
    GCM_INTERACTIVE: 'never',
    ...(options.env || {})
  };
  const result = spawnSync(GIT_BIN, args, {
    cwd: REPO_ROOT,
    encoding: 'utf8',
    windowsHide: true,
    timeout: options.timeoutMs || 30000,
    maxBuffer: 16 * 1024 * 1024,
    env
  });

  if (result.error && !options.allowFailure) {
    throw result.error;
  }

  const status = typeof result.status === 'number' ? result.status : 1;
  if (status !== 0 && !options.allowFailure) {
    const detail = (result.stderr || result.stdout || '').trim();
    throw new Error(`git ${args.join(' ')} exited with code ${status}${detail ? `: ${detail}` : ''}`);
  }

  return {
    status,
    stdout: result.stdout || '',
    stderr: result.stderr || '',
    error: result.error || null
  };
}

function formatLocalYmd(date = new Date()) {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function loadSchedulePlan() {
  if (!fs.existsSync(PLAN_PATH)) {
    throw new Error(`Schedule plan missing at ${PLAN_PATH}`);
  }
  const parsed = JSON.parse(fs.readFileSync(PLAN_PATH, 'utf8'));
  if (!parsed || !Array.isArray(parsed.days) || parsed.days.length === 0) {
    throw new Error('Invalid git-6month-schedule-plan.json: expected non-empty "days" array.');
  }
  return parsed;
}

function parseArgs(argv) {
  const flags = new Set(argv.slice(2));
  const help = flags.has('--help') || flags.has('-h');
  const status = flags.has('--status');
  const dryRun = flags.has('--dry-run');
  const forceNext = flags.has('--force-next');
  const runDailyExplicit = flags.has('--run-daily');
  const runDaily = runDailyExplicit || (!help && !status && !dryRun);
  return { help, status, dryRun, forceNext, runDaily };
}

function hasHeadCommit() {
  return runGit(['rev-parse', '--verify', 'HEAD'], { allowFailure: true }).status === 0;
}

function getCurrentBranch() {
  const sym = runGit(['symbolic-ref', '--quiet', '--short', 'HEAD'], { allowFailure: true });
  if (sym.status === 0 && sym.stdout.trim()) {
    return sym.stdout.trim();
  }
  const rev = runGit(['rev-parse', '--abbrev-ref', 'HEAD'], { allowFailure: true });
  return rev.status === 0 ? rev.stdout.trim() : 'HEAD';
}

function getHeadInfo() {
  if (!hasHeadCommit()) {
    return null;
  }
  const dateShort = runGit(['log', '-1', '--format=%cd', '--date=short']).stdout.trim();
  const fullHash = runGit(['rev-parse', 'HEAD']).stdout.trim();
  const shortHash = runGit(['rev-parse', '--short', 'HEAD']).stdout.trim();
  const subject = runGit(['log', '-1', '--format=%s']).stdout.trim();
  const totalCommits = Number.parseInt(runGit(['rev-list', '--count', 'HEAD']).stdout.trim(), 10) || 0;
  return { dateShort, fullHash, shortHash, subject, totalCommits };
}

function getCommittedScheduleDays() {
  const committed = new Set();
  if (!hasHeadCommit()) {
    return committed;
  }
  const logBodies = runGit(['log', '--format=%B%x00']).stdout;
  const trailerRegex = /^X-Schedule-Day:\s*(\d+)(?:\/\d+)?\s*$/gim;
  let match = trailerRegex.exec(logBodies);
  while (match !== null) {
    const dayNum = Number.parseInt(match[1], 10);
    if (Number.isInteger(dayNum) && dayNum >= 1) {
      committed.add(dayNum);
    }
    match = trailerRegex.exec(logBodies);
  }
  return committed;
}

function getWorkingTreeStatus() {
  const raw = runGit(['status', '--porcelain', '-z', '--untracked-files=all']).stdout;
  const parts = raw.split('\0');
  const dirtyPaths = [];
  const dirtySet = new Set();
  let untrackedCount = 0;
  let modifiedCount = 0;
  let stagedCount = 0;

  for (let i = 0; i < parts.length; i += 1) {
    const token = parts[i];
    if (!token || token.length < 4) continue;
    const x = token[0];
    const y = token[1];
    const relPath = token.slice(3).replace(/\\/g, '/');

    if (x === 'R' || x === 'C') {
      i += 1; // skip original path entry in porcelain -z
    }

    if (x === '?' && y === '?') {
      untrackedCount += 1;
    } else {
      if (x !== ' ' && x !== '?') stagedCount += 1;
      if (y !== ' ' && y !== '?') modifiedCount += 1;
    }

    if (!dirtySet.has(relPath)) {
      dirtySet.add(relPath);
      dirtyPaths.push(relPath);
    }
  }

  const preStagedRaw = hasHeadCommit()
    ? runGit(['diff', '--cached', '--name-only', '-z'], { allowFailure: true }).stdout
    : '';
  const preStagedPaths = preStagedRaw
    .split('\0')
    .map((s) => s.trim().replace(/\\/g, '/'))
    .filter(Boolean);

  return {
    dirtyPaths,
    dirtySet,
    untrackedCount,
    modifiedCount,
    stagedCount,
    preStagedPaths
  };
}

function getRemoteSyncStatus(headInfo) {
  const remoteRes = runGit(['remote', 'get-url', 'origin'], { allowFailure: true });
  const originUrl = remoteRes.status === 0 ? remoteRes.stdout.trim() : null;

  if (!headInfo) {
    return {
      originUrl,
      hasOriginMainRef: false,
      aheadCount: 0,
      behindCount: 0,
      needsPush: false,
      summary: originUrl ? 'No local commits yet (0 ahead)' : 'No origin remote configured'
    };
  }

  const originMainVerify = runGit(['rev-parse', '--verify', 'refs/remotes/origin/main'], {
    allowFailure: true
  });

  if (originMainVerify.status !== 0) {
    return {
      originUrl,
      hasOriginMainRef: false,
      aheadCount: headInfo.totalCommits,
      behindCount: 0,
      needsPush: Boolean(originUrl && headInfo.totalCommits > 0),
      summary: `${headInfo.totalCommits} commit(s) ahead of origin/main (remote tracking ref not published yet)`
    };
  }

  const countsRaw = runGit(['rev-list', '--left-right', '--count', 'refs/remotes/origin/main...HEAD'], {
    allowFailure: true
  });
  let behindCount = 0;
  let aheadCount = headInfo.totalCommits;
  if (countsRaw.status === 0) {
    const nums = countsRaw.stdout.trim().split(/\s+/).map((n) => Number.parseInt(n, 10));
    if (nums.length === 2 && Number.isFinite(nums[0]) && Number.isFinite(nums[1])) {
      behindCount = nums[0];
      aheadCount = nums[1];
    }
  }

  return {
    originUrl,
    hasOriginMainRef: true,
    aheadCount,
    behindCount,
    needsPush: Boolean(originUrl && aheadCount > 0),
    summary:
      aheadCount === 0 && behindCount === 0
        ? 'In sync with origin/main (0 ahead, 0 behind)'
        : `${aheadCount} ahead, ${behindCount} behind origin/main`
  };
}

function selectFilesForSlice(entry, wtStatus, fallbackChangelogRel) {
  const expected = (entry.expectedPaths || []).map((p) => p.replace(/\\/g, '/'));
  const dirtyMatched = expected.filter((rel) => {
    const abs = path.join(REPO_ROOT, rel);
    return wtStatus.dirtySet.has(rel) && fs.existsSync(abs);
  });

  if (dirtyMatched.length > 0) {
    return {
      strategy: 'planned-slice',
      filesToStage: dirtyMatched,
      requiresChangelogAppend: false
    };
  }

  // Fallback 1: Pick next deterministic uncommitted existing file on disk (never git add .)
  const dirtyExisting = wtStatus.dirtyPaths
    .filter((rel) => fs.existsSync(path.join(REPO_ROOT, rel)))
    .sort((a, b) => a.localeCompare(b));

  const nonChangelogDirty = dirtyExisting.filter((rel) => rel !== fallbackChangelogRel);
  if (nonChangelogDirty.length > 0) {
    return {
      strategy: 'fallback-uncommitted-file',
      filesToStage: [nonChangelogDirty[0]],
      requiresChangelogAppend: false
    };
  }

  if (dirtyExisting.length > 0) {
    return {
      strategy: 'fallback-uncommitted-changelog',
      filesToStage: [dirtyExisting[0]],
      requiresChangelogAppend: false
    };
  }

  // Fallback 2: Working tree has 0 uncommitted files -> append dated verification entry to docs/DAILY_CHANGELOG.md
  return {
    strategy: 'fallback-daily-changelog',
    filesToStage: [fallbackChangelogRel],
    requiresChangelogAppend: true
  };
}

function appendDailyChangelogEntry(fallbackChangelogRel, entry, totalDays, headInfo) {
  const absPath = path.join(REPO_ROOT, fallbackChangelogRel);
  const parentDir = path.dirname(absPath);
  if (!fs.existsSync(parentDir)) {
    fs.mkdirSync(parentDir, { recursive: true });
  }

  const nowIso = new Date().toISOString();
  const localYmd = formatLocalYmd();
  const headLabel = headInfo ? `${headInfo.shortHash} (${headInfo.dateShort})` : 'initial';

  let prefix = '';
  if (!fs.existsSync(absPath)) {
    prefix = [
      '# Daily Engineering & Release Verification Changelog',
      '',
      'Automated daily engineering continuity log maintained by `.agents/scripts/daily-git-shipper.mjs` when scheduled roadmap slices execute prior to code check-in.',
      '',
      '---',
      ''
    ].join('\n');
  } else {
    const existing = fs.readFileSync(absPath, 'utf8');
    if (existing.length > 0 && !existing.endsWith('\n')) {
      prefix = '\n';
    }
  }

  const block = [
    `## Day ${String(entry.day).padStart(3, '0')} / ${totalDays} — ${localYmd} (Scheduled: ${entry.scheduledDate})`,
    `- **Phase / WBS Task**: Phase ${entry.phase} · \`${entry.taskId}\` (\`${entry.module}\`)`,
    `- **Planned Slice Target**: ${entry.expectedPaths.map((p) => `\`${p}\``).join(', ')}`,
    `- **Planned Objective**: ${entry.commitMessage}`,
    `- **Baseline HEAD Verified**: \`${headLabel}\` at \`${nowIso}\``,
    ''
  ].join('\n');

  fs.appendFileSync(absPath, `${prefix}${block}`, 'utf8');
}

function persistShipperLog(record) {
  if (!fs.existsSync(GIT_DIR)) {
    return;
  }
  fs.mkdirSync(SHIPPER_STATE_DIR, { recursive: true });
  fs.appendFileSync(HISTORY_LOG_PATH, `${JSON.stringify(record)}\n`, 'utf8');
  fs.writeFileSync(LAST_RUN_PATH, `${JSON.stringify(record, null, 2)}\n`, 'utf8');
}

function attemptOriginPush() {
  const pushRes = runGit(['push', '-u', 'origin', 'main'], {
    allowFailure: true,
    timeoutMs: 25000
  });

  if (pushRes.status === 0) {
    console.log('[PASS] Pushed to origin/main');
    return { pushed: true, detail: (pushRes.stdout || pushRes.stderr).trim() };
  }

  const rawDetail = (pushRes.stderr || pushRes.stdout || (pushRes.error ? pushRes.error.message : '')).trim();
  const compactReason = rawDetail.split(/\r?\n/).slice(0, 2).join(' | ');
  console.log(
    '[WARN] Committed locally on main (ahead of origin); push will auto-sync once GitHub credentials/repo are configured' +
      (compactReason ? ` (${compactReason})` : '')
  );
  return { pushed: false, detail: compactReason };
}

function readLastShipperRun() {
  if (!fs.existsSync(LAST_RUN_PATH)) return null;
  try {
    return JSON.parse(fs.readFileSync(LAST_RUN_PATH, 'utf8'));
  } catch {
    return null;
  }
}

function printHelp() {
  console.log(
    [
      'Motorcycle Dealer Platform — 6-Month (183-Day) Daily Git Shipper',
      '',
      'Usage:',
      '  node .agents/scripts/daily-git-shipper.mjs --status',
      '  node .agents/scripts/daily-git-shipper.mjs --dry-run [--force-next]',
      '  node .agents/scripts/daily-git-shipper.mjs --run-daily [--force-next]'
    ].join('\n')
  );
}

function handleStatus(plan) {
  const totalDays = plan.days.length;
  const branch = getCurrentBranch();
  const headInfo = getHeadInfo();
  const committedDays = getCommittedScheduleDays();
  const nextEntry = plan.days.find((d) => !committedDays.has(d.day)) || null;
  const wtStatus = getWorkingTreeStatus();
  const syncStatus = getRemoteSyncStatus(headInfo);
  const lastRun = readLastShipperRun();

  const highestDay = committedDays.size > 0 ? Math.max(...committedDays) : 0;
  const remainingCount = totalDays - committedDays.size;

  console.log('=== Motorcycle Dealer — 183-Day Git Shipper Status ===');
  console.log(`Branch               : ${branch}`);
  console.log(`Schedule Progress    : Day ${highestDay} / ${totalDays} (${committedDays.size} committed, ${remainingCount} remaining)`);
  console.log(
    `Last Commit on HEAD  : ${
      headInfo ? `${headInfo.shortHash} on ${headInfo.dateShort} — "${headInfo.subject}"` : 'None (unborn branch)'
    }`
  );
  console.log(
    `Next Scheduled Slice : ${
      nextEntry
        ? `Day ${String(nextEntry.day).padStart(3, '0')} / ${totalDays} (${nextEntry.scheduledDate}, Phase ${nextEntry.phase} ${nextEntry.taskId}) — "${nextEntry.commitMessage}"`
        : 'All 183 slices complete'
    }`
  );
  console.log(
    `Working Tree         : ${wtStatus.untrackedCount} untracked, ${wtStatus.modifiedCount} modified, ${wtStatus.stagedCount} staged`
  );
  console.log(`Remote Origin        : ${syncStatus.originUrl || 'Not configured'}`);
  console.log(`Push Sync Status     : ${syncStatus.summary}`);
  if (lastRun) {
    console.log(`Last Shipper Run     : ${lastRun.timestampUtc} [${lastRun.outcome}] (Day ${lastRun.day ?? '-'})`);
  }
}

function handleDryRun(plan, forceNext) {
  const totalDays = plan.days.length;
  const todayLocal = formatLocalYmd();
  const headInfo = getHeadInfo();
  const committedDays = getCommittedScheduleDays();
  const nextEntry = plan.days.find((d) => !committedDays.has(d.day)) || null;
  const wtStatus = getWorkingTreeStatus();
  const syncStatus = getRemoteSyncStatus(headInfo);
  const fallbackRel = (plan.scheduleWindow && plan.scheduleWindow.fallbackChangelogPath) || 'docs/DAILY_CHANGELOG.md';

  const alreadyRanToday = Boolean(headInfo && headInfo.dateShort === todayLocal && !forceNext);

  console.log('=== Motorcycle Dealer — Daily Git Shipper [DRY-RUN] ===');
  console.log(`Local Date           : ${todayLocal}`);
  if (alreadyRanToday) {
    console.log(
      `[DRY-RUN] HEAD (${headInfo.shortHash}) already has a commit dated ${headInfo.dateShort}; --run-daily would [SKIP] committing and ${
        syncStatus.needsPush ? 'attempt git push -u origin main' : 'exit cleanly'
      }. (Pass --force-next to preview/commit the next slice today.)`
    );
  }

  if (!nextEntry) {
    console.log(`[DRY-RUN] All ${totalDays} / ${totalDays} slices are already committed.`);
    return;
  }

  const selection = selectFilesForSlice(nextEntry, wtStatus, fallbackRel);
  console.log(`Target Slice         : Day ${String(nextEntry.day).padStart(3, '0')} / ${totalDays} (Scheduled: ${nextEntry.scheduledDate})`);
  console.log(`Phase / Task         : Phase ${nextEntry.phase} · ${nextEntry.taskId} (${nextEntry.module})`);
  console.log(`Selection Strategy   : ${selection.strategy}`);
  console.log(`Commit Message       : ${nextEntry.commitMessage}`);
  console.log(`Commit Trailer       : X-Schedule-Day: ${nextEntry.day}/${totalDays}`);
  console.log(`Files to Stage (${selection.filesToStage.length}) :`);
  for (const f of selection.filesToStage) {
    console.log(`  - ${f}${selection.requiresChangelogAppend ? ' (would append dated entry)' : ''}`);
  }
}

function handleRunDaily(plan, forceNext) {
  const totalDays = plan.days.length;
  const todayLocal = formatLocalYmd();
  const headBefore = getHeadInfo();
  const fallbackRel = (plan.scheduleWindow && plan.scheduleWindow.fallbackChangelogPath) || 'docs/DAILY_CHANGELOG.md';

  // 1) Guard against duplicate daily commits unless --force-next is explicitly supplied
  if (headBefore && headBefore.dateShort === todayLocal && !forceNext) {
    console.log(`[SKIP] Today's slice already committed (${headBefore.shortHash} on ${headBefore.dateShort})`);
    const syncBefore = getRemoteSyncStatus(headBefore);
    let pushOutcome = { pushed: false, detail: 'already-synced' };
    if (syncBefore.needsPush) {
      pushOutcome = attemptOriginPush();
    }
    persistShipperLog({
      timestampUtc: new Date().toISOString(),
      localDate: todayLocal,
      outcome: 'SKIP_ALREADY_COMMITTED_TODAY',
      commitHash: headBefore.fullHash,
      pushed: pushOutcome.pushed,
      pushDetail: pushOutcome.detail
    });
    return;
  }

  // 2) Resolve lowest uncommitted day (1..183) strictly from X-Schedule-Day trailers in git log
  const committedDays = getCommittedScheduleDays();
  const nextEntry = plan.days.find((d) => !committedDays.has(d.day)) || null;
  if (!nextEntry) {
    console.log(`[PASS] All ${totalDays} / ${totalDays} scheduled slices are already committed on main.`);
    const syncBefore = getRemoteSyncStatus(headBefore);
    if (syncBefore.needsPush) {
      attemptOriginPush();
    }
    return;
  }

  // 3) Stage exact slice files (or deterministic fallback so zero days can ever fail)
  const wtStatus = getWorkingTreeStatus();
  const selection = selectFilesForSlice(nextEntry, wtStatus, fallbackRel);

  if (selection.requiresChangelogAppend) {
    appendDailyChangelogEntry(fallbackRel, nextEntry, totalDays, headBefore);
  }

  runGit(['add', '--', ...selection.filesToStage]);

  // Verify staged changes exist
  const stagedVerify = hasHeadCommit()
    ? runGit(['diff', '--cached', '--name-only', '--', ...selection.filesToStage]).stdout.trim()
    : runGit(['status', '--porcelain', '--', ...selection.filesToStage]).stdout.trim();

  if (!stagedVerify) {
    throw new Error(
      `No index delta produced after staging [${selection.filesToStage.join(', ')}] for Day ${nextEntry.day}.`
    );
  }

  // 4) Commit with <entry.commitMessage> + X-Schedule-Day trailer
  const trailerBlock = [
    `X-Schedule-Day: ${nextEntry.day}/${totalDays}`,
    `X-Schedule-Date: ${nextEntry.scheduledDate}`,
    `X-Schedule-Task: ${nextEntry.taskId}`
  ].join('\n');

  const commitArgs = ['commit', '-m', nextEntry.commitMessage, '-m', trailerBlock];
  if (headBefore && wtStatus.preStagedPaths.some((p) => !selection.filesToStage.includes(p))) {
    commitArgs.push('--only', '--', ...selection.filesToStage);
  }

  runGit(commitArgs);

  const headAfter = getHeadInfo();
  console.log(
    `[PASS] Committed Day ${String(nextEntry.day).padStart(3, '0')}/${totalDays} (${headAfter.shortHash}): ${nextEntry.commitMessage} [${selection.filesToStage.length} file(s), strategy=${selection.strategy}]`
  );

  // 5) Attempt non-interactive push to origin/main
  const pushOutcome = attemptOriginPush();

  persistShipperLog({
    timestampUtc: new Date().toISOString(),
    localDate: todayLocal,
    outcome: 'COMMITTED',
    day: nextEntry.day,
    totalDays,
    scheduledDate: nextEntry.scheduledDate,
    taskId: nextEntry.taskId,
    strategy: selection.strategy,
    files: selection.filesToStage,
    commitHash: headAfter.fullHash,
    shortHash: headAfter.shortHash,
    commitMessage: nextEntry.commitMessage,
    pushed: pushOutcome.pushed,
    pushDetail: pushOutcome.detail
  });
}

function main() {
  if (!fs.existsSync(GIT_DIR)) {
    console.error(`[FAIL] Git repository is not initialized at ${REPO_ROOT}`);
    process.exit(1);
  }

  const opts = parseArgs(process.argv);
  if (opts.help) {
    printHelp();
    return;
  }

  const plan = loadSchedulePlan();

  if (opts.status) {
    handleStatus(plan);
    return;
  }

  if (opts.dryRun) {
    handleDryRun(plan, opts.forceNext);
    return;
  }

  if (opts.runDaily) {
    handleRunDaily(plan, opts.forceNext);
  }
}

try {
  main();
} catch (err) {
  console.error(`[FAIL] daily-git-shipper error: ${err instanceof Error ? err.message : String(err)}`);
  process.exit(1);
}
