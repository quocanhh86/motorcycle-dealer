import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function readStdinText() {
  try {
    return fs.readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

function resolveWorkspaceRoot(payload) {
  const candidates = [];
  if (payload && Array.isArray(payload.workspacePaths)) {
    for (const wp of payload.workspacePaths) {
      if (typeof wp === 'string' && wp.trim()) {
        candidates.push(path.resolve(wp));
      }
    }
  }
  if (payload && typeof payload.cwd === 'string' && payload.cwd.trim()) {
    candidates.push(path.resolve(payload.cwd));
  }
  if (process.env.PLUGIN_ROOT) {
    candidates.push(path.resolve(process.env.PLUGIN_ROOT, '../../..'));
  }
  candidates.push(path.resolve(__dirname, '../../../..'));
  candidates.push(process.cwd());

  for (const dir of candidates) {
    if (fs.existsSync(path.join(dir, 'PROGRESS.md')) || fs.existsSync(path.join(dir, 'AGENTS.md'))) {
      return dir;
    }
  }
  return path.resolve(__dirname, '../../../..');
}

function extractProgressSummary(workspaceRoot) {
  const progressPath = path.join(workspaceRoot, 'PROGRESS.md');
  let activePhase = 'Phase 9: v1.2 Post-Service 5-Star CSAT Reviews & VIN Warranty Claims';
  let completion = 'See PROGRESS.md';

  if (fs.existsSync(progressPath)) {
    const content = fs.readFileSync(progressPath, 'utf8');
    const phaseMatch = content.match(/\|\s*\*\*Current Active Phase\*\*\s*\|\s*([^|\r\n]+)\|/i);
    if (phaseMatch && phaseMatch[1]) {
      activePhase = phaseMatch[1].replace(/\*\*/g, '').replace(/`/g, '').trim();
    }
    const completionMatch = content.match(/\|\s*\*\*Overall Roadmap Completion\*\*\s*\|\s*([^|\r\n]+)\|/i);
    if (completionMatch && completionMatch[1]) {
      completion = completionMatch[1].replace(/\*\*/g, '').replace(/`/g, '').trim();
    }
  }

  return { activePhase, completion };
}

try {
  const rawInput = readStdinText().trim();
  let payload = {};
  if (rawInput) {
    try {
      payload = JSON.parse(rawInput);
    } catch {
      payload = {};
    }
  }

  const workspaceRoot = resolveWorkspaceRoot(payload);
  const { activePhase, completion } = extractProgressSummary(workspaceRoot);

  const banner = [
    `[Motorcycle Dealer & Loyalty Platform — Session Governance Active (Phases 1..9+)]`,
    `Current Active Phase: ${activePhase} | Roadmap Completion: ${completion}.`,
    `Non-Negotiable Guardrails: (1) Enforce strict Phase-Gate boundaries across Phases 1..9+ (call motorcycle-dealer-guardrails -> check_phase_gate before scaffolding new layers);`,
    `(2) Preserve 4-layer Clean Architecture inward dependencies (ADR-001), 48-permission RBAC, and 8-table IDealerScoped row-level isolation across 27 3NF tables (ADR-002);`,
    `(3) PointLedgerEntries, SalesOrders, ServiceInvoices, and ServiceInvoiceLineItems are strictly immutable (ADR-003; Floor(Floor(NetPaidAmount / 10000m) * TierMultiplier); +25 ServiceReviewBonus adds to AvailablePoints ONLY);`,
    `(4) For SQL Server (localdb)\\MSSQLLocalDB / MotorcycleDealerDb introspection, ALWAYS use workspace MCP sqlserver-localdb-inspector (NEVER global dbhub, which targets an external Oracle DB);`,
    `(5) Mandatory Post-Task Sync Gate: upon completing ANY phase, feature, or fix, update all affected docs/, regenerate/validate .docx, sync TASKS.md/PROGRESS.md/README.md/baseline-spec-snapshot.json & .agents/, map 100% of git-trackable files in .agents/scripts/git-6month-schedule-plan.json, and pass 'node .agents/scripts/verify-post-task-sync.mjs'.`
  ].join(' ');

  const output = {
    injectSteps: [
      {
        systemMessage: {
          systemMessage: banner
        }
      }
    ]
  };

  process.stdout.write(`${JSON.stringify(output)}\n`);
} catch {
  process.stdout.write(`${JSON.stringify({ injectSteps: [] })}\n`);
}
