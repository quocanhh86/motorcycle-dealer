import fs from 'node:fs';

const DESTRUCTIVE_PATTERNS = [
  {
    id: 'SQL_DROP_DATABASE',
    regex: /\bDROP\s+DATABASE\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: DROP DATABASE is prohibited. Use controlled EF Core migrations or .agents/scripts/ensure-sqlserver-db.ps1.'
  },
  {
    id: 'SQL_TRUNCATE_TABLE',
    regex: /\bTRUNCATE\s+TABLE\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: TRUNCATE TABLE is prohibited against normalized 3NF MotorcycleDealerDb tables.'
  },
  {
    id: 'LEDGER_DELETE_IMMUTABLE',
    regex: /\bDELETE\b[\s\S]{0,120}?\bPointLedgerEntries\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard (ADR-003): PointLedgerEntries is a strictly append-only financial audit ledger; DELETE is forbidden. Insert a compensating entry instead.'
  },
  {
    id: 'LEDGER_UPDATE_IMMUTABLE',
    regex: /\bUPDATE\b[\s\S]{0,120}?\bPointLedgerEntries\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard (ADR-003): PointLedgerEntries is strictly immutable; UPDATE is forbidden. Insert a compensating entry instead.'
  },
  {
    id: 'INVOICE_LINE_ITEM_DELETE_IMMUTABLE',
    regex: /\bDELETE\b[\s\S]{0,120}?\bServiceInvoiceLineItems\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard (ADR-003): ServiceInvoiceLineItems is an immutable itemized invoice audit table; DELETE is forbidden.'
  },
  {
    id: 'INVOICE_LINE_ITEM_UPDATE_IMMUTABLE',
    regex: /\bUPDATE\b[\s\S]{0,120}?\bServiceInvoiceLineItems\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard (ADR-003): ServiceInvoiceLineItems is strictly immutable; UPDATE is forbidden.'
  },
  {
    id: 'SALES_ORDER_DELETE_IMMUTABLE',
    regex: /\bDELETE\b[\s\S]{0,120}?\bSalesOrders\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: SalesOrders is an immutable financial record; physical DELETE is prohibited.'
  },
  {
    id: 'SALES_ORDER_UPDATE_IMMUTABLE',
    regex: /\bUPDATE\b[\s\S]{0,120}?\bSalesOrders\b[\s\S]{0,60}?\bSET\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: SalesOrders is an immutable financial record; SQL UPDATE is prohibited.'
  },
  {
    id: 'SERVICE_INVOICE_DELETE_IMMUTABLE',
    regex: /\bDELETE\b[\s\S]{0,120}?\bServiceInvoices\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: ServiceInvoices is an immutable financial record; physical DELETE is prohibited.'
  },
  {
    id: 'SERVICE_INVOICE_UPDATE_IMMUTABLE',
    regex: /\bUPDATE\b[\s\S]{0,120}?\bServiceInvoices\b[\s\S]{0,60}?\bSET\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: ServiceInvoices is an immutable financial record; SQL UPDATE is prohibited.'
  },
  {
    id: 'GIT_RESET_HARD',
    regex: /\bgit\s+reset\s+--hard\b/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: git reset --hard risks discarding uncommitted work across gated milestones.'
  },
  {
    id: 'GIT_PUSH_FORCE',
    regex: /\bgit\s+push\b[^\r\n;|&]*?(?:--force(?:-with-lease)?\b|\s-f(?:\s|$))/i,
    reason:
      'Blocked by motorcycle-dealer-suite safety-guard: force-pushing (git push --force / -f) is prohibited on motorcycle-dealer branches.'
  }
];

function extractCommandLine(payload) {
  if (!payload || typeof payload !== 'object') {
    return '';
  }
  const args = payload.toolCall && typeof payload.toolCall === 'object' ? payload.toolCall.args : null;
  if (args && typeof args === 'object') {
    if (typeof args.CommandLine === 'string') return args.CommandLine;
    if (typeof args.commandLine === 'string') return args.commandLine;
    if (typeof args.command === 'string') return args.command;
  }
  return '';
}

try {
  let raw = '';
  try {
    raw = fs.readFileSync(0, 'utf8');
  } catch {
    raw = '';
  }

  const trimmed = raw.trim();
  if (!trimmed) {
    process.stdout.write(`${JSON.stringify({ decision: 'allow' })}\n`);
    process.exit(0);
  }

  let payload = {};
  try {
    payload = JSON.parse(trimmed);
  } catch {
    process.stdout.write(`${JSON.stringify({ decision: 'allow' })}\n`);
    process.exit(0);
  }

  const commandLine = extractCommandLine(payload);
  if (!commandLine) {
    process.stdout.write(`${JSON.stringify({ decision: 'allow' })}\n`);
    process.exit(0);
  }

  for (const rule of DESTRUCTIVE_PATTERNS) {
    if (rule.regex.test(commandLine)) {
      process.stdout.write(
        `${JSON.stringify({
          decision: 'deny',
          reason: `[${rule.id}] ${rule.reason}`
        })}\n`
      );
      process.exit(0);
    }
  }

  process.stdout.write(`${JSON.stringify({ decision: 'allow' })}\n`);
} catch {
  process.stdout.write(`${JSON.stringify({ decision: 'allow' })}\n`);
}
