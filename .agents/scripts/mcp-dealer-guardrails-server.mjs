import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Redirect any accidental console.log calls to stderr so stdout remains pure JSON-RPC 2.0
console.log = (...args) => {
  process.stderr.write(`${args.map((a) => (typeof a === 'string' ? a : JSON.stringify(a))).join(' ')}\n`);
};

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const WORKSPACE_ROOT = path.resolve(__dirname, '../..');
const SNAPSHOT_PATH = path.join(__dirname, 'baseline-spec-snapshot.json');
const PROGRESS_PATH = path.join(WORKSPACE_ROOT, 'PROGRESS.md');
const TASKS_PATH = path.join(WORKSPACE_ROOT, 'TASKS.md');
const DB_DESIGN_PATH = path.join(WORKSPACE_ROOT, 'docs', 'DATABASE_DESIGN.md');

const SERVER_INFO = {
  name: 'motorcycle-dealer-guardrails',
  version: '1.3.0'
};

const TOOLS = [
  {
    name: 'check_phase_gate',
    description:
      'Audits workspace files and rule size/frontmatter limits against the active Phase allowed artifacts in baseline-spec-snapshot.json and PROGRESS.md.',
    inputSchema: {
      type: 'object',
      properties: {
        phase: {
          type: 'integer',
          minimum: 1,
          maximum: 10,
          description: 'Optional phase number (1..10) to validate against. Defaults to active phase from PROGRESS.md / baseline snapshot.'
        }
      },
      additionalProperties: false
    }
  },
  {
    name: 'lookup_rbac_permission',
    description:
      'Filters the 48 canonical Permissions.<Module>.<Action> policies by role (Customer, DealerStaff, Admin), module, or permission string and returns 8-table IDealerScoped tenancy rules.',
    inputSchema: {
      type: 'object',
      properties: {
        role: {
          type: 'string',
          enum: ['Customer', 'DealerStaff', 'Admin'],
          description: 'Optional canonical role filter.'
        },
        module: {
          type: 'string',
          description: 'Optional module filter (e.g. Showroom, Inventories, SalesOrders, Vehicles, TimeSlots, Bookings, Invoices, Loyalty, Notifications, Dealers, Users, Analytics, TestDrives, SpareParts, Reviews, Warranty).'
        },
        permission: {
          type: 'string',
          description: 'Optional exact or substring permission code filter (e.g. Bookings.Create, Reviews.CreateOwn, Warranty.ManageDealer, or Permissions.Loyalty).'
        }
      },
      additionalProperties: false
    }
  },
  {
    name: 'lookup_db_table_spec',
    description:
      'Extracts the canonical 3NF Data Dictionary Markdown specification, columns, and constraints for one of the 27 SQL Server tables in MotorcycleDealerDb from docs/DATABASE_DESIGN.md.',
    inputSchema: {
      type: 'object',
      properties: {
        tableName: {
          type: 'string',
          description: 'PascalCase SQL Server table name (e.g. PointLedgerEntries, ServiceTimeSlots, ServiceBookings, CustomerVehicles, TestDriveBookings, SpareParts, ServiceInvoiceLineItems, ServiceReviews, WarrantyClaims) or "all" to list the 27-table inventory.'
        }
      },
      required: ['tableName'],
      additionalProperties: false
    }
  },
  {
    name: 'calculate_loyalty_preview',
    description:
      'Canonical loyalty math oracle computing basePoints = Math.floor(netPaidAmountVnd / 10000), pointsEarned = Math.floor(basePoints * tierMultiplier), and resulting tier across Bronze (0/1.00), Silver (500/1.20), Gold (2000/1.50), and Platinum (5000/2.00).',
    inputSchema: {
      type: 'object',
      properties: {
        netPaidAmountVnd: {
          type: 'number',
          minimum: 0,
          description: 'Net settled VND amount (SalesOrders.FinalAmount or ServiceInvoices.NetPaidAmount).'
        },
        tierMultiplier: {
          type: 'number',
          description: 'Optional explicit pre-transaction tier multiplier (e.g. 1.00, 1.20, 1.50, 2.00). If omitted, derived from currentTier or currentLifetimeQualifyingPoints.'
        },
        currentTier: {
          type: 'string',
          enum: ['Bronze', 'Silver', 'Gold', 'Platinum'],
          description: 'Optional current tier code before settlement.'
        },
        currentLifetimeQualifyingPoints: {
          type: 'integer',
          minimum: 0,
          description: 'Optional current LifetimeQualifyingPoints balance prior to earning points on this transaction.'
        },
        voucherPointsCost: {
          type: 'integer',
          minimum: 0,
          description: 'Optional voucher redemption points cost to preview non-downgrade AvailablePoints deduction.'
        }
      },
      required: ['netPaidAmountVnd'],
      additionalProperties: false
    }
  },
  {
    name: 'get_wbs_tasks',
    description:
      'Parses TASKS.md WBS items (T-101..T-1008) filtered by phase (1..10), status (done, pending, all), or taskId.',
    inputSchema: {
      type: 'object',
      properties: {
        phase: {
          type: 'integer',
          minimum: 1,
          maximum: 10,
          description: 'Optional phase number (1..10).'
        },
        status: {
          type: 'string',
          enum: ['all', 'done', 'pending'],
          description: 'Optional completion filter (default: all).'
        },
        taskId: {
          type: 'string',
          description: 'Optional specific task ID such as T-201 or T-601.'
        }
      },
      additionalProperties: false
    }
  }
];

function loadSnapshot() {
  const raw = fs.readFileSync(SNAPSHOT_PATH, 'utf8');
  return JSON.parse(raw);
}

function globToRegExp(glob) {
  const normalized = glob.replace(/\\/g, '/');
  let pattern = '^';
  let i = 0;
  while (i < normalized.length) {
    if (normalized.startsWith('/**/', i)) {
      pattern += '(?:/|/.+/)';
      i += 4;
    } else if (normalized.startsWith('/**', i) && i + 3 === normalized.length) {
      pattern += '(?:/.*)?';
      i += 3;
    } else if (normalized.startsWith('**/', i)) {
      pattern += '(?:.+/)?';
      i += 3;
    } else if (normalized.startsWith('**', i)) {
      pattern += '.*';
      i += 2;
    } else if (normalized[i] === '*') {
      pattern += '[^/]*';
      i += 1;
    } else if (normalized[i] === '?') {
      pattern += '[^/]';
      i += 1;
    } else {
      pattern += normalized[i].replace(/[.+^${}()|[\]\\]/g, '\\$&');
      i += 1;
    }
  }
  pattern += '$';
  return new RegExp(pattern, 'i');
}

const SKIPPED_TRANSIENT_DIRS = new Set([
  '.git',
  '.vs',
  '.idea',
  'bin',
  'obj',
  'testresults',
  'coverage'
]);

function listWorkspaceFiles(dir, baseDir = dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  const isWorkspaceRoot = path.resolve(dir) === path.resolve(baseDir);
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const lowerName = entry.name.toLowerCase();
    if (SKIPPED_TRANSIENT_DIRS.has(lowerName)) continue;

    const fullPath = path.join(dir, entry.name);
    const relPath = path.relative(baseDir, fullPath).replace(/\\/g, '/');

    if (entry.isDirectory()) {
      if (lowerName === 'node_modules') {
        // Always catch forbidden root node_modules/ without traversing thousands of transitive files;
        // skip nested dependency folders (e.g. frontend/node_modules/) during workspace file walks.
        if (isWorkspaceRoot) {
          acc.push('node_modules');
        }
        continue;
      }
      listWorkspaceFiles(fullPath, baseDir, acc);
    } else if (entry.isFile()) {
      acc.push(relPath);
    }
  }
  return acc;
}

function detectActivePhaseNumber(snapshot) {
  let progressPhase = null;
  if (fs.existsSync(PROGRESS_PATH)) {
    const content = fs.readFileSync(PROGRESS_PATH, 'utf8');
    const match = content.match(/\|\s*\*\*Current Active Phase\*\*\s*\|\s*[^|\r\n]*?Phase\s+(\d+)/i);
    if (match && match[1]) {
      progressPhase = Number.parseInt(match[1], 10);
    }
  }
  const snapshotPhase = Number.isInteger(snapshot.generatedForPhase) ? snapshot.generatedForPhase : 2;
  if (progressPhase && progressPhase >= snapshotPhase) {
    return { activePhase: progressPhase, progressMdPhase: progressPhase };
  }
  return {
    activePhase: snapshotPhase,
    progressMdPhase: progressPhase ?? snapshotPhase
  };
}

function inspectRuleFiles() {
  const ruleSpecs = [
    { path: 'AGENTS.md', maxBytes: 24000 },
    { path: '.agents/rules/AGENTS.md', maxBytes: 12000 },
    { path: '.agents/plugins/motorcycle-dealer-suite/rules/AGENTS.md', maxBytes: 5000 },
    { path: 'backend/AGENTS.md', maxBytes: 8000 },
    { path: 'frontend/AGENTS.md', maxBytes: 6000 },
    { path: 'mobile/AGENTS.md', maxBytes: 8000 }
  ];
  const results = [];
  for (const spec of ruleSpecs) {
    const rel = spec.path;
    const maxBytes = spec.maxBytes;
    const abs = path.join(WORKSPACE_ROOT, rel);
    if (!fs.existsSync(abs)) {
      results.push({ path: rel, exists: false, maxBytes, valid: true });
      continue;
    }
    const stat = fs.statSync(abs);
    const content = fs.readFileSync(abs, 'utf8');
    const firstLine = content.split(/\r?\n/, 1)[0].trim();
    const hasFrontmatter = firstLine === '---';
    const underByteCap = stat.size < maxBytes;
    results.push({
      path: rel,
      exists: true,
      bytes: stat.size,
      maxBytes,
      hasYamlFrontmatter: hasFrontmatter,
      valid: underByteCap && !hasFrontmatter
    });
  }
  return results;
}

function handleCheckPhaseGate(args) {
  const snapshot = loadSnapshot();
  const detected = detectActivePhaseNumber(snapshot);
  const targetPhaseNumber =
    typeof args.phase === 'number' && Number.isInteger(args.phase) ? args.phase : detected.activePhase;

  const phaseSpec = snapshot.phases.find((p) => p.phaseNumber === targetPhaseNumber);
  if (!phaseSpec) {
    return {
      isError: true,
      payload: { error: `Invalid phase number: ${targetPhaseNumber}. Expected 1..${snapshot.phases.length}.` }
    };
  }

  const allowedRegexes = phaseSpec.allowedGlobs.map(globToRegExp);
  const forbiddenPrefixes = (phaseSpec.forbiddenPrefixes || []).map((p) => p.replace(/\\/g, '/').toLowerCase());
  const allFiles = listWorkspaceFiles(WORKSPACE_ROOT);

  const violations = [];
  for (const relFile of allFiles) {
    const lowerFile = relFile.toLowerCase();
    const hitForbidden = forbiddenPrefixes.find(
      (prefix) => lowerFile === prefix.replace(/\/$/, '') || lowerFile.startsWith(prefix)
    );
    if (hitForbidden) {
      violations.push({
        file: relFile,
        reason: `Matches forbidden prefix "${hitForbidden}" for ${phaseSpec.phaseId}`
      });
      continue;
    }
    const isAllowed = allowedRegexes.some((rx) => rx.test(relFile));
    if (!isAllowed) {
      violations.push({
        file: relFile,
        reason: `Outside allowedGlobs for ${phaseSpec.phaseId}`
      });
    }
  }

  const ruleChecks = inspectRuleFiles();
  for (const rc of ruleChecks) {
    if (rc.exists && !rc.valid) {
      violations.push({
        file: rc.path,
        reason: rc.hasYamlFrontmatter
          ? 'Rule file contains prohibited YAML frontmatter (standalone AGENTS.md must not use frontmatter)'
          : `Rule file size (${rc.bytes} bytes) exceeds ${rc.maxBytes.toLocaleString('en-US')}-byte limit`
      });
    }
  }

  return {
    isError: false,
    payload: {
      compliant: violations.length === 0,
      evaluatedPhase: phaseSpec.phaseId,
      phaseName: phaseSpec.name,
      progressMdPhase: `Phase ${detected.progressMdPhase}`,
      exitGate: phaseSpec.exitGate,
      totalWorkspaceFiles: allFiles.length,
      allowedGlobs: phaseSpec.allowedGlobs,
      forbiddenPrefixes: phaseSpec.forbiddenPrefixes,
      ruleFileChecks: ruleChecks,
      violationCount: violations.length,
      violations
    }
  };
}

function handleLookupRbacPermission(args) {
  const snapshot = loadSnapshot();
  const allPerms = snapshot.rbac.permissions;

  const roleFilter = typeof args.role === 'string' ? args.role.trim() : '';
  const moduleFilter = typeof args.module === 'string' ? args.module.trim().toLowerCase() : '';
  const permFilter = typeof args.permission === 'string' ? args.permission.trim().toLowerCase() : '';

  const filtered = allPerms.filter((p) => {
    if (roleFilter && !p.roles.some((r) => r.toLowerCase() === roleFilter.toLowerCase())) {
      return false;
    }
    if (moduleFilter && p.module.toLowerCase() !== moduleFilter && !p.boundedContext.toLowerCase().includes(moduleFilter)) {
      return false;
    }
    if (permFilter && !p.code.toLowerCase().includes(permFilter)) {
      return false;
    }
    return true;
  });

  const roleTotals = {};
  for (const r of snapshot.rbac.roles) {
    roleTotals[r] = allPerms.filter((p) => p.roles.includes(r)).length;
  }

  return {
    isError: false,
    payload: {
      filtersApplied: {
        role: roleFilter || null,
        module: args.module || null,
        permission: args.permission || null
      },
      totalCatalogPermissions: allPerms.length,
      rolePermissionTotals: roleTotals,
      matchedCount: filtered.length,
      dealerScopedRules: snapshot.rbac.dealerScopedRules,
      permissions: filtered
    }
  };
}

function handleLookupDbTableSpec(args) {
  const snapshot = loadSnapshot();
  const rawName = typeof args.tableName === 'string' ? args.tableName.trim() : '';
  const cleanName = rawName.replace(/^\[?dbo\]?\./i, '').replace(/^\[|\]$/g, '');

  if (!cleanName || cleanName.toLowerCase() === 'all') {
    return {
      isError: false,
      payload: {
        databaseName: snapshot.project.database.databaseName,
        tableCount: snapshot.database.tables.length,
        boundedContexts: snapshot.database.boundedContexts,
        dealerScopedTables: snapshot.database.dealerScopedTables,
        rowVersionTables: snapshot.database.rowVersionTables,
        immutableTables: snapshot.database.immutableTables,
        appendOnlyTables: snapshot.database.appendOnlyTables,
        tables: snapshot.database.tables
      }
    };
  }

  const tableMeta = snapshot.database.tables.find(
    (t) => t.name.toLowerCase() === cleanName.toLowerCase()
  );

  if (!tableMeta) {
    return {
      isError: true,
      payload: {
        error: `Table "${rawName}" is not one of the ${snapshot.database.tables.length} canonical 3NF tables in MotorcycleDealerDb.`,
        validTableNames: snapshot.database.tables.map((t) => t.name)
      }
    };
  }

  let markdownSection = '';
  const constraints = [];
  if (fs.existsSync(DB_DESIGN_PATH)) {
    const doc = fs.readFileSync(DB_DESIGN_PATH, 'utf8');
    const lines = doc.split(/\r?\n/);
    const headerRegex = new RegExp(
      `^####\\s+Table\\s+\\d+:\\s+\`(?:dbo\\.)?${tableMeta.name}\``,
      'i'
    );
    const startIdx = lines.findIndex((line) => headerRegex.test(line));
    if (startIdx !== -1) {
      let endIdx = lines.length;
      for (let i = startIdx + 1; i < lines.length; i += 1) {
        if (/^(?:####\s+Table\s+\d+:|###\s+4\.\d+|##\s+5\.)/.test(lines[i])) {
          endIdx = i;
          break;
        }
      }
      const slice = lines.slice(startIdx, endIdx);
      while (slice.length > 0 && (slice[slice.length - 1].trim() === '' || slice[slice.length - 1].trim() === '---')) {
        slice.pop();
      }
      markdownSection = slice.join('\n');

      for (const line of slice) {
        const bulletMatch = line.match(/^\s{2,}-\s+`([^`]+)`(.*)$/);
        if (bulletMatch) {
          constraints.push(`${bulletMatch[1]}${bulletMatch[2] || ''}`.trim());
        }
      }
    }
  }

  return {
    isError: false,
    payload: {
      table: tableMeta,
      isDealerScoped: snapshot.database.dealerScopedTables.includes(tableMeta.name),
      hasRowVersion: snapshot.database.rowVersionTables.includes(tableMeta.name),
      isImmutable: snapshot.database.immutableTables.includes(tableMeta.name),
      isAppendOnly: snapshot.database.appendOnlyTables.includes(tableMeta.name),
      constraints,
      markdownSpec: markdownSection
    }
  };
}

function resolveTierByPoints(tiers, qualifyingPoints) {
  let resolved = tiers[0];
  for (const tier of tiers) {
    if (qualifyingPoints >= tier.minQualifyingPoints && tier.minQualifyingPoints >= resolved.minQualifyingPoints) {
      resolved = tier;
    }
  }
  return resolved;
}

function handleCalculateLoyaltyPreview(args) {
  const snapshot = loadSnapshot();
  const tiers = snapshot.loyalty.tiers;
  const baseUnitVnd = snapshot.loyalty.baseUnitVnd;

  const netPaidAmountVnd = Number(args.netPaidAmountVnd);
  if (!Number.isFinite(netPaidAmountVnd) || netPaidAmountVnd < 0) {
    return {
      isError: true,
      payload: { error: 'netPaidAmountVnd must be a non-negative number.' }
    };
  }

  const hasExplicitPoints =
    typeof args.currentLifetimeQualifyingPoints === 'number' &&
    Number.isFinite(args.currentLifetimeQualifyingPoints);

  let startingTier = tiers[0];
  let startingLifetimePoints = 0;

  if (typeof args.currentTier === 'string' && args.currentTier.trim()) {
    const found = tiers.find((t) => t.code.toLowerCase() === args.currentTier.trim().toLowerCase());
    if (found) {
      startingTier = found;
      startingLifetimePoints = hasExplicitPoints
        ? Math.max(0, Math.trunc(args.currentLifetimeQualifyingPoints))
        : found.minQualifyingPoints;
    }
  } else if (hasExplicitPoints) {
    startingLifetimePoints = Math.max(0, Math.trunc(args.currentLifetimeQualifyingPoints));
    startingTier = resolveTierByPoints(tiers, startingLifetimePoints);
  } else if (typeof args.tierMultiplier === 'number' && Number.isFinite(args.tierMultiplier)) {
    const matchedByMultiplier = tiers.find((t) => Math.abs(t.multiplier - args.tierMultiplier) < 0.0001);
    if (matchedByMultiplier) {
      startingTier = matchedByMultiplier;
      startingLifetimePoints = matchedByMultiplier.minQualifyingPoints;
    }
  }

  const effectiveMultiplier =
    typeof args.tierMultiplier === 'number' && Number.isFinite(args.tierMultiplier)
      ? args.tierMultiplier
      : startingTier.multiplier;

  if (effectiveMultiplier < 0) {
    return {
      isError: true,
      payload: { error: 'tierMultiplier must be non-negative.' }
    };
  }

  // Exact decimal-safe two-step floor matching C# (int)Math.Floor(Math.Floor(NetPaidAmount / 10_000m) * TierMultiplier)
  const basePoints = Math.floor(netPaidAmountVnd / baseUnitVnd);
  const multiplierScaled = Math.round(effectiveMultiplier * 10000);
  const pointsEarned = Math.floor((basePoints * multiplierScaled) / 10000);

  const lifetimeQualifyingPointsAfter = startingLifetimePoints + pointsEarned;
  const resultingTier = resolveTierByPoints(tiers, lifetimeQualifyingPointsAfter);
  const tierUpgraded = resultingTier.rankOrder > startingTier.rankOrder;

  const nextTier = tiers.find((t) => t.rankOrder === resultingTier.rankOrder + 1) || null;
  const pointsToNextTier = nextTier
    ? Math.max(0, nextTier.minQualifyingPoints - lifetimeQualifyingPointsAfter)
    : 0;

  let voucherRedemptionPreview = null;
  if (typeof args.voucherPointsCost === 'number' && args.voucherPointsCost > 0) {
    const cost = Math.trunc(args.voucherPointsCost);
    voucherRedemptionPreview = {
      voucherPointsCost: cost,
      lifetimeQualifyingPointsUnchanged: lifetimeQualifyingPointsAfter,
      tierPreserved: resultingTier.code,
      note: snapshot.loyalty.redemptionRule
    };
  }

  return {
    isError: false,
    payload: {
      formula: snapshot.loyalty.formula,
      netPaidAmountVnd,
      baseUnitVnd,
      basePoints,
      tierMultiplier: effectiveMultiplier,
      pointsEarned,
      previousTier: startingTier.code,
      currentLifetimeQualifyingPointsBefore: startingLifetimePoints,
      lifetimeQualifyingPointsAfter,
      resultingTier: resultingTier.code,
      resultingTierMultiplier: resultingTier.multiplier,
      tierUpgraded,
      nextTier: nextTier ? nextTier.code : null,
      pointsToNextTier,
      voucherRedemptionPreview
    }
  };
}

function parseTasksMd() {
  if (!fs.existsSync(TASKS_PATH)) {
    return [];
  }
  const content = fs.readFileSync(TASKS_PATH, 'utf8');
  const lines = content.split(/\r?\n/);
  const tasks = [];
  let current = null;

  const taskLineRegex = /^-\s+\[([ xX])\]\s+\*\*`(T-(\d+)\d{2})`\s+—\s+(.+?)\*\*\s*$/;

  for (const line of lines) {
    const match = line.match(taskLineRegex);
    if (match) {
      if (current) tasks.push(current);
      const checked = match[1].toLowerCase() === 'x';
      current = {
        taskId: match[2],
        phase: Number.parseInt(match[3], 10),
        status: checked ? 'done' : 'pending',
        title: match[4].trim(),
        ownerRole: null,
        traceability: null,
        dependencies: null,
        targetDeliverablePaths: null,
        verificationCommand: null
      };
      continue;
    }

    if (current) {
      if (/^###\s+Phase\s+\d+:/.test(line) || /^---$/.test(line.trim())) {
        tasks.push(current);
        current = null;
        continue;
      }
      const fieldMatch = line.match(/^\s+-\s+\*\*(.+?)\*\*:\s*(.+)$/);
      if (fieldMatch) {
        const key = fieldMatch[1].trim().toLowerCase();
        const val = fieldMatch[2].trim();
        if (key === 'owner role') current.ownerRole = val;
        else if (key === 'traceability') current.traceability = val;
        else if (key === 'dependencies') current.dependencies = val;
        else if (key === 'target deliverable paths') current.targetDeliverablePaths = val;
        else if (key === 'verification command') current.verificationCommand = val;
      }
    }
  }
  if (current) tasks.push(current);
  return tasks;
}

function handleGetWbsTasks(args) {
  const allTasks = parseTasksMd();
  const statusFilter = typeof args.status === 'string' ? args.status.toLowerCase() : 'all';
  const phaseFilter = typeof args.phase === 'number' && Number.isInteger(args.phase) ? args.phase : null;
  const taskIdFilter = typeof args.taskId === 'string' && args.taskId.trim() ? args.taskId.trim().toUpperCase() : null;

  const filtered = allTasks.filter((t) => {
    if (phaseFilter !== null && t.phase !== phaseFilter) return false;
    if (statusFilter !== 'all' && t.status !== statusFilter) return false;
    if (taskIdFilter && t.taskId.toUpperCase() !== taskIdFilter) return false;
    return true;
  });

  const totalCount = allTasks.length;
  const doneCount = allTasks.filter((t) => t.status === 'done').length;
  const pendingCount = totalCount - doneCount;

  return {
    isError: false,
    payload: {
      overallSummary: {
        totalTasks: totalCount,
        doneTasks: doneCount,
        pendingTasks: pendingCount,
        completionPercentage: totalCount > 0 ? Number(((doneCount / totalCount) * 100).toFixed(1)) : 0
      },
      filtersApplied: {
        phase: phaseFilter,
        status: statusFilter,
        taskId: taskIdFilter
      },
      matchedCount: filtered.length,
      tasks: filtered
    }
  };
}

function dispatchToolCall(params) {
  if (!params || typeof params.name !== 'string') {
    return {
      jsonRpcError: {
        code: -32602,
        message: 'Invalid params: tool "name" is required.'
      }
    };
  }

  const toolName = params.name;
  const toolArgs = params.arguments && typeof params.arguments === 'object' ? params.arguments : {};

  try {
    switch (toolName) {
      case 'check_phase_gate':
        return handleCheckPhaseGate(toolArgs);
      case 'lookup_rbac_permission':
        return handleLookupRbacPermission(toolArgs);
      case 'lookup_db_table_spec':
        return handleLookupDbTableSpec(toolArgs);
      case 'calculate_loyalty_preview':
        return handleCalculateLoyaltyPreview(toolArgs);
      case 'get_wbs_tasks':
        return handleGetWbsTasks(toolArgs);
      default:
        return {
          isError: true,
          payload: {
            error: `Unknown tool "${toolName}". Available tools: ${TOOLS.map((t) => t.name).join(', ')}`
          }
        };
    }
  } catch (err) {
    return {
      isError: true,
      payload: {
        error: err instanceof Error ? err.message : String(err)
      }
    };
  }
}

function sendJsonRpc(message) {
  process.stdout.write(`${JSON.stringify(message)}\n`);
}

function handleJsonRpcMessage(msg) {
  if (!msg || typeof msg !== 'object' || msg.jsonrpc !== '2.0') {
    return;
  }

  const hasId = Object.prototype.hasOwnProperty.call(msg, 'id');
  const method = msg.method;

  // JSON-RPC notifications (e.g. notifications/initialized) do not carry an id and receive no response
  if (!hasId) {
    return;
  }

  const id = msg.id;

  if (method === 'initialize') {
    const requestedVersion =
      msg.params && typeof msg.params.protocolVersion === 'string'
        ? msg.params.protocolVersion
        : '2024-11-05';
    const supportedVersions = ['2025-06-18', '2025-03-26', '2024-11-05'];
    const negotiatedVersion = supportedVersions.includes(requestedVersion)
      ? requestedVersion
      : '2024-11-05';

    sendJsonRpc({
      jsonrpc: '2.0',
      id,
      result: {
        protocolVersion: negotiatedVersion,
        capabilities: {
          tools: {
            listChanged: false
          }
        },
        serverInfo: SERVER_INFO
      }
    });
    return;
  }

  if (method === 'ping') {
    sendJsonRpc({ jsonrpc: '2.0', id, result: {} });
    return;
  }

  if (method === 'resources/list') {
    sendJsonRpc({ jsonrpc: '2.0', id, result: { resources: [] } });
    return;
  }

  if (method === 'prompts/list') {
    sendJsonRpc({ jsonrpc: '2.0', id, result: { prompts: [] } });
    return;
  }

  if (method === 'tools/list') {
    sendJsonRpc({
      jsonrpc: '2.0',
      id,
      result: {
        tools: TOOLS
      }
    });
    return;
  }

  if (method === 'tools/call') {
    const outcome = dispatchToolCall(msg.params);
    if (outcome.jsonRpcError) {
      sendJsonRpc({
        jsonrpc: '2.0',
        id,
        error: outcome.jsonRpcError
      });
      return;
    }

    sendJsonRpc({
      jsonrpc: '2.0',
      id,
      result: {
        content: [
          {
            type: 'text',
            text: JSON.stringify(outcome.payload, null, 2)
          }
        ],
        structuredContent: outcome.payload,
        isError: Boolean(outcome.isError)
      }
    });
    return;
  }

  sendJsonRpc({
    jsonrpc: '2.0',
    id,
    error: {
      code: -32601,
      message: `Method not found: ${method}`
    }
  });
}

function startMcpStdioServer() {
  process.stdin.setEncoding('utf8');
  let buffer = '';

  const processLine = (line) => {
    const trimmed = line.trim();
    if (!trimmed || /^Content-Length:/i.test(trimmed)) return;
    try {
      const parsed = JSON.parse(trimmed);
      if (Array.isArray(parsed)) {
        for (const item of parsed) handleJsonRpcMessage(item);
      } else {
        handleJsonRpcMessage(parsed);
      }
    } catch (err) {
      process.stderr.write(
        `[${SERVER_INFO.name}] Ignored malformed JSON-RPC line: ${err instanceof Error ? err.message : String(err)}\n`
      );
    }
  };

  process.stdin.on('data', (chunk) => {
    buffer += chunk;
    let newlineIndex = buffer.indexOf('\n');
    while (newlineIndex !== -1) {
      const line = buffer.slice(0, newlineIndex);
      buffer = buffer.slice(newlineIndex + 1);
      processLine(line);
      newlineIndex = buffer.indexOf('\n');
    }
  });

  process.stdin.on('end', () => {
    if (buffer.trim()) {
      processLine(buffer);
      buffer = '';
    }
  });

  process.stdin.on('error', (err) => {
    process.stderr.write(`[${SERVER_INFO.name}] stdin error: ${err.message}\n`);
  });

  process.stdin.resume();
}

startMcpStdioServer();
