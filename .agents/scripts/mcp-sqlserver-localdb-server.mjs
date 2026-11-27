import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// Redirect any accidental console.log calls to stderr so stdout remains strictly JSON-RPC 2.0
console.log = (...args) => {
  process.stderr.write(`${args.map((a) => (typeof a === 'string' ? a : JSON.stringify(a))).join(' ')}\n`);
};

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const SNAPSHOT_PATH = path.join(__dirname, 'baseline-spec-snapshot.json');

const DEFAULT_SERVER = process.env.SQLSERVER_INSTANCE || '(localdb)\\MSSQLLocalDB';
const DEFAULT_DATABASE = process.env.SQLSERVER_DATABASE || 'MotorcycleDealerDb';
const MAX_ROWS_CAP = 200;

const SERVER_INFO = {
  name: 'sqlserver-localdb-inspector',
  version: '1.2.0'
};

// Static PowerShell SqlClient runner passed via -EncodedCommand; all parameters arrive strictly
// via environment variables (SQL_MCP_*) + SqlConnectionStringBuilder to prevent shell/connstring injection,
// with ApplicationIntent=ReadOnly and an explicit ReadCommitted transaction rolled back in finally.
const PS_SQLCLIENT_QUERY_SCRIPT = `
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
try {
  $csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
  $csb['Data Source'] = $env:SQL_MCP_SERVER
  $csb['Initial Catalog'] = $env:SQL_MCP_DATABASE
  $csb['Integrated Security'] = $true
  $csb['TrustServerCertificate'] = $true
  $csb['ApplicationIntent'] = 'ReadOnly'
  $csb['Connect Timeout'] = [int]$env:SQL_MCP_LOGIN_TIMEOUT

  $maxRows = [int]$env:SQL_MCP_MAX_ROWS
  $cmdTimeout = [int]$env:SQL_MCP_CMD_TIMEOUT

  $conn = New-Object System.Data.SqlClient.SqlConnection($csb.ConnectionString)
  $conn.Open()
  $tx = $conn.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
  try {
    $isMultiResult = ($env:SQL_MCP_MULTI_RESULT -eq '1')
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandTimeout = $cmdTimeout
    $cmd.CommandText = [string]$env:SQL_MCP_QUERY

    $batchResults = New-Object System.Collections.ArrayList
    $rdr = $cmd.ExecuteReader()
    try {
      do {
        $fieldCount = $rdr.FieldCount
        $columns = New-Object string[] $fieldCount
        $seenNames = @{}
        for ($i = 0; $i -lt $fieldCount; $i++) {
          $baseName = $rdr.GetName($i)
          if ([string]::IsNullOrEmpty($baseName)) {
            $baseName = "column_$($i + 1)"
          }
          if ($seenNames.ContainsKey($baseName)) {
            $seenNames[$baseName] = [int]$seenNames[$baseName] + 1
            $columns[$i] = "$($baseName)_$($seenNames[$baseName])"
          } else {
            $seenNames[$baseName] = 1
            $columns[$i] = $baseName
          }
        }

        $rows = New-Object System.Collections.ArrayList
        $truncated = $false
        while ($rdr.Read()) {
          if ($rows.Count -ge $maxRows) {
            $truncated = $true
            break
          }
          $rowObj = [ordered]@{}
          for ($i = 0; $i -lt $fieldCount; $i++) {
            $key = $columns[$i]
            if ($rdr.IsDBNull($i)) {
              $rowObj[$key] = $null
            } else {
              $val = $rdr.GetValue($i)
              if ($val -is [byte[]]) {
                $rowObj[$key] = '0x' + [System.BitConverter]::ToString($val).Replace('-', '')
              } elseif ($val -is [DateTimeOffset]) {
                $rowObj[$key] = $val.ToString('o')
              } elseif ($val -is [DateTime]) {
                $rowObj[$key] = $val.ToString('o')
              } elseif ($val -is [Guid] -or $val -is [TimeSpan]) {
                $rowObj[$key] = $val.ToString()
              } else {
                $rowObj[$key] = $val
              }
            }
          }
          [void]$rows.Add($rowObj)
        }

        [void]$batchResults.Add([ordered]@{
          columns = [string[]]$columns
          rows = [object[]]$rows.ToArray()
          rowCount = $rows.Count
          truncated = $truncated
        })
      } while ($isMultiResult -and $rdr.NextResult())
    } finally {
      $rdr.Close()
    }

    if ($isMultiResult) {
      $payload = [ordered]@{
        ok = $true
        results = [object[]]$batchResults.ToArray()
      }
    } else {
      $first = $batchResults[0]
      $payload = [ordered]@{
        ok = $true
        columns = $first.columns
        rows = $first.rows
        rowCount = $first.rowCount
        truncated = $first.truncated
      }
    }
    [Console]::Out.Write(($payload | ConvertTo-Json -Depth 7 -Compress))
  } finally {
    try { $tx.Rollback() } catch {}
    $conn.Close()
  }
} catch {
  $errPayload = [ordered]@{
    ok = $false
    error = $_.Exception.Message
  }
  [Console]::Out.Write(($errPayload | ConvertTo-Json -Depth 4 -Compress))
  exit 1
}
`;

const PS_SQLCLIENT_ENCODED_COMMAND = Buffer.from(PS_SQLCLIENT_QUERY_SCRIPT, 'utf16le').toString('base64');

const TOOLS = [
  {
    name: 'sqlserver_status',
    description:
      'Checks SQL Server LocalDB (sqllocaldb info MSSQLLocalDB), sqlcmd / PowerShell SqlConnection availability, and whether MotorcycleDealerDb exists on (localdb)\\MSSQLLocalDB.',
    inputSchema: {
      type: 'object',
      properties: {
        server: {
          type: 'string',
          description: 'Optional SQL Server instance (defaults to SQLSERVER_INSTANCE or "(localdb)\\\\MSSQLLocalDB").'
        },
        database: {
          type: 'string',
          description: 'Optional target database name to check (defaults to "MotorcycleDealerDb").'
        },
        probeConnection: {
          type: 'boolean',
          description:
            'When true (default when LocalDB is Running), probes master via sqlcmd or PowerShell System.Data.SqlClient.SqlConnection to verify MotorcycleDealerDb existence.'
        }
      },
      additionalProperties: false
    }
  },
  {
    name: 'list_sqlserver_tables',
    description:
      'Lists SQL Server user tables and row counts in MotorcycleDealerDb (or master), falling back gracefully with the 27 planned 3NF tables if MotorcycleDealerDb is not migrated yet.',
    inputSchema: {
      type: 'object',
      properties: {
        database: {
          type: 'string',
          description: 'Database name (defaults to "MotorcycleDealerDb"; "master" also supported).'
        },
        server: {
          type: 'string',
          description: 'Optional SQL Server instance override.'
        }
      },
      additionalProperties: false
    }
  },
  {
    name: 'inspect_sqlserver_schema',
    description:
      'Returns live SQL Server columns, data types, primary keys, covering/filtered indexes, foreign keys, and check constraints for a specific table in MotorcycleDealerDb.',
    inputSchema: {
      type: 'object',
      properties: {
        tableName: {
          type: 'string',
          description: 'PascalCase table name in MotorcycleDealerDb (e.g. ServiceTimeSlots, PointLedgerEntries, Users).'
        },
        schemaName: {
          type: 'string',
          description: 'SQL Server schema name (defaults to "dbo").'
        },
        database: {
          type: 'string',
          description: 'Database name (defaults to "MotorcycleDealerDb").'
        },
        server: {
          type: 'string',
          description: 'Optional SQL Server instance override.'
        }
      },
      required: ['tableName'],
      additionalProperties: false
    }
  },
  {
    name: 'query_readonly_sql',
    description:
      'Executes a strictly validated read-only SELECT or WITH query (capped at TOP 200 rows) against SQL Server LocalDB via sqlcmd or PowerShell SqlConnection. Rejects INSERT, UPDATE, DELETE, DROP, ALTER, EXEC, MERGE, TRUNCATE, and multi-statement semicolons.',
    inputSchema: {
      type: 'object',
      properties: {
        sql: {
          type: 'string',
          description: 'Single-statement read-only SELECT or WITH T-SQL query (no semicolons or mutating keywords).'
        },
        database: {
          type: 'string',
          description: 'Target database name (defaults to "MotorcycleDealerDb"; falls back or errors cleanly if not yet migrated).'
        },
        server: {
          type: 'string',
          description: 'Optional SQL Server instance override.'
        },
        maxRows: {
          type: 'integer',
          minimum: 1,
          maximum: 200,
          description: 'Maximum rows to return (1..200, default 200).'
        }
      },
      required: ['sql'],
      additionalProperties: false
    }
  }
];

function loadPlannedTables() {
  try {
    if (fs.existsSync(SNAPSHOT_PATH)) {
      const snap = JSON.parse(fs.readFileSync(SNAPSHOT_PATH, 'utf8'));
      return snap.database?.tables?.map((t) => t.name) || [];
    }
  } catch {
    // ignore snapshot read errors
  }
  return [];
}

function resolveSqlcmdPath() {
  if (process.env.SQL_MCP_FORCE_SQLCLIENT === '1') {
    return null;
  }

  if (process.env.SQLCMD_PATH && fs.existsSync(process.env.SQLCMD_PATH)) {
    return process.env.SQLCMD_PATH;
  }

  try {
    const locator = process.platform === 'win32' ? 'where.exe' : 'which';
    const out = execFileSync(locator, ['sqlcmd'], {
      encoding: 'utf8',
      timeout: 3000,
      stdio: ['ignore', 'pipe', 'ignore']
    });
    const firstLine = out
      .split(/\r?\n/)
      .map((s) => s.trim())
      .find(Boolean);
    if (firstLine && fs.existsSync(firstLine)) {
      return firstLine;
    }
  } catch {
    // Not in PATH; on Windows, executeSqlQuery automatically falls back to PowerShell System.Data.SqlClient.SqlConnection.
  }

  return null;
}

function inspectLocalDbInstance(instanceName = 'MSSQLLocalDB', autoStartIfStopped = false) {
  const readInfo = () => {
    const raw = execFileSync('sqllocaldb', ['info', instanceName], {
      encoding: 'utf8',
      timeout: 5000,
      stdio: ['ignore', 'pipe', 'pipe']
    });
    const info = {};
    for (const line of raw.split(/\r?\n/)) {
      const idx = line.indexOf(':');
      if (idx !== -1) {
        const key = line.slice(0, idx).trim();
        const val = line.slice(idx + 1).trim();
        if (key) info[key] = val;
      }
    }
    return {
      installed: true,
      instanceName,
      version: info['Version'] || null,
      state: info['State'] || 'Unknown',
      autoCreate: info['Auto-create'] || null,
      pipeName: info['Instance pipe name'] || null
    };
  };

  try {
    let parsed = readInfo();
    if (autoStartIfStopped && parsed.installed && parsed.state !== 'Running') {
      try {
        execFileSync('sqllocaldb', ['start', instanceName], {
          encoding: 'utf8',
          timeout: 12000,
          stdio: ['ignore', 'pipe', 'pipe']
        });
        parsed = readInfo();
      } catch {
        // Fall through with current state if start fails
      }
    }
    return parsed;
  } catch (err) {
    return {
      installed: false,
      instanceName,
      state: 'Unavailable',
      error: err instanceof Error ? err.message : String(err)
    };
  }
}

function parseSqlcmdTabularOutput(stdout, maxRows = MAX_ROWS_CAP) {
  const lines = stdout
    .split(/\r?\n/)
    .map((l) => l.replace(/\r$/, ''))
    .filter((l) => l.trim() !== '' && !/^\(\d+\s+rows?\s+affected\)$/i.test(l.trim()));

  if (lines.length === 0) {
    return { columns: [], rows: [], rowCount: 0, truncated: false };
  }

  // Standard sqlcmd -W -s "\t" outputs: line 0 = headers, line 1 = "---\t---"
  if (lines.length >= 2 && /^[-]+(\t[-]+)*$/.test(lines[1])) {
    const columns = lines[0].split('\t').map((c) => c.trim());
    const dataLines = lines.slice(2);
    const truncated = dataLines.length > maxRows;
    const boundedLines = dataLines.slice(0, maxRows);
    const rows = boundedLines.map((line) => {
      const cells = line.split('\t');
      const obj = {};
      for (let i = 0; i < columns.length; i += 1) {
        const colName = columns[i] || `column_${i + 1}`;
        const rawVal = cells[i] !== undefined ? cells[i] : null;
        obj[colName] = rawVal === 'NULL' ? null : rawVal;
      }
      return obj;
    });
    return {
      columns,
      rows,
      rowCount: rows.length,
      truncated
    };
  }

  return {
    columns: ['value'],
    rows: lines.slice(0, maxRows).map((l) => ({ value: l })),
    rowCount: Math.min(lines.length, maxRows),
    truncated: lines.length > maxRows
  };
}

function runPowerShellSqlClient({ server, db, sql, maxRows = MAX_ROWS_CAP, loginTimeoutSec = 5, execTimeoutMs = 12000 }) {
  const cmdTimeoutSec = Math.max(5, Math.ceil(execTimeoutMs / 1000));
  const env = {
    ...process.env,
    SQL_MCP_SERVER: server,
    SQL_MCP_DATABASE: db,
    SQL_MCP_QUERY: sql,
    SQL_MCP_MULTI_RESULT: '0',
    SQL_MCP_MAX_ROWS: String(maxRows),
    SQL_MCP_LOGIN_TIMEOUT: String(loginTimeoutSec),
    SQL_MCP_CMD_TIMEOUT: String(cmdTimeoutSec)
  };

  try {
    const stdout = execFileSync(
      'powershell.exe',
      ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', PS_SQLCLIENT_ENCODED_COMMAND],
      {
        encoding: 'utf8',
        timeout: execTimeoutMs + 3000,
        env,
        stdio: ['ignore', 'pipe', 'pipe']
      }
    );

    const parsed = JSON.parse(stdout.trim());
    if (!parsed || parsed.ok !== true) {
      return {
        ok: false,
        transport: 'powershell-sqlclient',
        reason: 'SQLCLIENT_EXECUTION_FAILED',
        message: (parsed && parsed.error) || 'PowerShell SqlConnection returned non-ok status.'
      };
    }

    const columns = Array.isArray(parsed.columns)
      ? parsed.columns
      : parsed.columns !== null && parsed.columns !== undefined
        ? [String(parsed.columns)]
        : [];
    const rows = Array.isArray(parsed.rows)
      ? parsed.rows
      : parsed.rows !== null && parsed.rows !== undefined
        ? [parsed.rows]
        : [];

    return {
      ok: true,
      transport: 'powershell-sqlclient',
      sqlcmdPath: null,
      columns,
      rows,
      rowCount: rows.length,
      truncated: Boolean(parsed.truncated)
    };
  } catch (err) {
    const stdoutText = err && err.stdout ? String(err.stdout).trim() : '';
    const stderrText = err && err.stderr ? String(err.stderr).trim() : '';
    if (stdoutText) {
      try {
        const parsedErr = JSON.parse(stdoutText);
        if (parsedErr && parsedErr.error) {
          return {
            ok: false,
            transport: 'powershell-sqlclient',
            reason: 'SQLCLIENT_EXECUTION_FAILED',
            message: String(parsedErr.error)
          };
        }
      } catch {
        // fall through to raw error message
      }
    }
    return {
      ok: false,
      transport: 'powershell-sqlclient',
      reason: 'SQLCLIENT_EXECUTION_FAILED',
      message: stderrText || stdoutText || (err instanceof Error ? err.message : String(err))
    };
  }
}

function executeSqlQuery({ server, db, sql, maxRows = MAX_ROWS_CAP, loginTimeoutSec = 5, execTimeoutMs = 10000 }) {
  const sqlcmdBin = resolveSqlcmdPath();
  if (sqlcmdBin) {
    const args = ['-S', server, '-d', db, '-l', String(loginTimeoutSec), '-b', '-W', '-s', '\t', '-Q', sql];
    try {
      const stdout = execFileSync(sqlcmdBin, args, {
        encoding: 'utf8',
        timeout: execTimeoutMs,
        stdio: ['ignore', 'pipe', 'pipe']
      });
      const parsed = parseSqlcmdTabularOutput(stdout, maxRows);
      return {
        ok: true,
        transport: 'sqlcmd',
        sqlcmdPath: sqlcmdBin,
        columns: parsed.columns,
        rows: parsed.rows,
        rowCount: parsed.rowCount,
        truncated: parsed.truncated
      };
    } catch (err) {
      const stderrText = err && err.stderr ? String(err.stderr).trim() : '';
      const stdoutText = err && err.stdout ? String(err.stdout).trim() : '';
      return {
        ok: false,
        transport: 'sqlcmd',
        sqlcmdPath: sqlcmdBin,
        reason: 'SQLCMD_EXECUTION_FAILED',
        message: stderrText || stdoutText || (err instanceof Error ? err.message : String(err))
      };
    }
  }

  if (process.platform === 'win32') {
    return runPowerShellSqlClient({ server, db, sql, maxRows, loginTimeoutSec, execTimeoutMs });
  }

  return {
    ok: false,
    transport: 'none',
    sqlcmdPath: null,
    reason: 'SQLCMD_NOT_FOUND',
    message: 'sqlcmd executable was not found in PATH or standard Microsoft SQL Server Client SDK directories.'
  };
}

function executeSqlBatch({ server, db, queries, maxRows = MAX_ROWS_CAP, loginTimeoutSec = 5, execTimeoutMs = 12000 }) {
  const sqlcmdBin = resolveSqlcmdPath();
  if (!sqlcmdBin && process.platform === 'win32') {
    const cmdTimeoutSec = Math.max(5, Math.ceil(execTimeoutMs / 1000));
    const env = {
      ...process.env,
      SQL_MCP_SERVER: server,
      SQL_MCP_DATABASE: db,
      SQL_MCP_QUERY: queries.join(';\n'),
      SQL_MCP_MULTI_RESULT: '1',
      SQL_MCP_MAX_ROWS: String(maxRows),
      SQL_MCP_LOGIN_TIMEOUT: String(loginTimeoutSec),
      SQL_MCP_CMD_TIMEOUT: String(cmdTimeoutSec)
    };
    try {
      const stdout = execFileSync(
        'powershell.exe',
        ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', PS_SQLCLIENT_ENCODED_COMMAND],
        {
          encoding: 'utf8',
          timeout: execTimeoutMs + 4000,
          env,
          stdio: ['ignore', 'pipe', 'pipe']
        }
      );
      const parsed = JSON.parse(stdout.trim());
      if (!parsed || parsed.ok !== true || !Array.isArray(parsed.results)) {
        return {
          ok: false,
          transport: 'powershell-sqlclient',
          message: (parsed && parsed.error) || 'PowerShell SqlConnection batch returned non-ok status.'
        };
      }
      const results = parsed.results.map((item) => ({
        rows: Array.isArray(item.rows)
          ? item.rows
          : item.rows !== null && item.rows !== undefined
            ? [item.rows]
            : []
      }));
      return { ok: true, transport: 'powershell-sqlclient', results };
    } catch (err) {
      const stdoutText = err && err.stdout ? String(err.stdout).trim() : '';
      const stderrText = err && err.stderr ? String(err.stderr).trim() : '';
      return {
        ok: false,
        transport: 'powershell-sqlclient',
        message: stderrText || stdoutText || (err instanceof Error ? err.message : String(err))
      };
    }
  }

  const results = [];
  let transport = 'sqlcmd';
  for (const q of queries) {
    const res = executeSqlQuery({ server, db, sql: q, maxRows, loginTimeoutSec, execTimeoutMs });
    if (!res.ok) {
      return { ok: false, transport: res.transport, message: res.message };
    }
    transport = res.transport;
    results.push({ rows: res.rows });
  }
  return { ok: true, transport, results };
}

function checkDatabaseExistsOnServer(server, databaseName) {
  const safeDbLiteral = databaseName.replace(/'/g, "''");
  const probeSql =
    `SET NOCOUNT ON; ` +
    `SELECT CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR(50)) AS ProductVersion, ` +
    `CAST(SERVERPROPERTY('Edition') AS VARCHAR(80)) AS Edition, ` +
    `DB_ID(N'${safeDbLiteral}') AS TargetDbId, ` +
    `(SELECT TOP 1 state_desc FROM sys.databases WHERE name = N'${safeDbLiteral}') AS StateDesc`;

  const res = executeSqlQuery({ server, db: 'master', sql: probeSql, maxRows: 5, loginTimeoutSec: 12, execTimeoutMs: 15000 });
  if (!res.ok) {
    return {
      serverReachable: false,
      databaseExists: false,
      transport: res.transport,
      sqlcmdPath: res.sqlcmdPath || null,
      connectionMessage: res.message
    };
  }

  const row = res.rows[0] || {};
  const hasDb = row.TargetDbId !== null && row.TargetDbId !== undefined && row.TargetDbId !== '';
  return {
    serverReachable: true,
    databaseExists: hasDb,
    databaseState: hasDb ? row.StateDesc || 'ONLINE' : null,
    productVersion: row.ProductVersion || null,
    edition: row.Edition || null,
    transport: res.transport,
    sqlcmdPath: res.sqlcmdPath || null
  };
}

function validateIdentifier(value, label) {
  if (typeof value !== 'string' || !/^[A-Za-z_][A-Za-z0-9_]*$/.test(value.trim())) {
    return {
      valid: false,
      error: `Invalid SQL identifier for ${label}: "${value}". Only alphanumeric characters and underscores are permitted.`
    };
  }
  return { valid: true, identifier: value.trim() };
}

// Single-pass T-SQL lexical scanner that masks comments, string literals, and quoted identifiers
// in left-to-right source order so `'/*'` inside a string literal can never mask `; EXEC ...`!
function stripSqlCommentsAndLiterals(sql) {
  let out = '';
  let i = 0;
  const len = sql.length;

  while (i < len) {
    const ch = sql[i];
    const next = i + 1 < len ? sql[i + 1] : '';

    // Line comment: -- ... \n
    if (ch === '-' && next === '-') {
      i += 2;
      while (i < len && sql[i] !== '\n' && sql[i] !== '\r') {
        i += 1;
      }
      out += ' ';
      continue;
    }

    // Nested T-SQL block comment: /* ... */
    if (ch === '/' && next === '*') {
      let depth = 1;
      i += 2;
      while (i < len && depth > 0) {
        if (sql[i] === '/' && sql[i + 1] === '*') {
          depth += 1;
          i += 2;
        } else if (sql[i] === '*' && sql[i + 1] === '/') {
          depth -= 1;
          i += 2;
        } else {
          i += 1;
        }
      }
      if (depth > 0) {
        return { ok: false, error: 'Unclosed block comment ("/*") in SQL query.' };
      }
      out += ' ';
      continue;
    }

    // Unicode N'...' or standard '...' string literal
    if (ch === "'" || ((ch === 'N' || ch === 'n') && next === "'")) {
      if (ch !== "'") i += 1;
      i += 1; // consume opening '
      let closed = false;
      while (i < len) {
        if (sql[i] === "'") {
          if (sql[i + 1] === "'") {
            i += 2; // escaped ''
            continue;
          }
          i += 1;
          closed = true;
          break;
        }
        i += 1;
      }
      if (!closed) {
        return { ok: false, error: 'Unclosed string literal ("\'") in SQL query.' };
      }
      out += " '' ";
      continue;
    }

    // Bracketed identifier [...]
    if (ch === '[') {
      i += 1;
      let closed = false;
      while (i < len) {
        if (sql[i] === ']') {
          if (sql[i + 1] === ']') {
            i += 2;
            continue;
          }
          i += 1;
          closed = true;
          break;
        }
        i += 1;
      }
      if (!closed) {
        return { ok: false, error: 'Unclosed bracketed identifier ("[") in SQL query.' };
      }
      out += ' IDENT ';
      continue;
    }

    // Double-quoted identifier "..."
    if (ch === '"') {
      i += 1;
      let closed = false;
      while (i < len) {
        if (sql[i] === '"') {
          if (sql[i + 1] === '"') {
            i += 2;
            continue;
          }
          i += 1;
          closed = true;
          break;
        }
        i += 1;
      }
      if (!closed) {
        return { ok: false, error: 'Unclosed quoted identifier ("\\"") in SQL query.' };
      }
      out += ' IDENT ';
      continue;
    }

    out += ch;
    i += 1;
  }

  return { ok: true, stripped: out };
}

function validateReadOnlySql(rawSql) {
  if (typeof rawSql !== 'string' || !rawSql.trim()) {
    return { valid: false, reason: 'SQL query string must be non-empty.' };
  }

  const trimmed = rawSql.trim();
  const lexed = stripSqlCommentsAndLiterals(trimmed);
  if (!lexed.ok) {
    return { valid: false, reason: lexed.error };
  }

  const stripped = lexed.stripped;

  if (stripped.includes(';')) {
    return {
      valid: false,
      reason:
        'Rejected multi-statement semicolon (";"). Supply a single read-only SELECT or WITH statement without semicolons.'
    };
  }

  if (!/^\s*(?:SELECT|WITH)\b/i.test(stripped)) {
    return {
      valid: false,
      reason: 'Only read-only SELECT or WITH (CTE) queries are permitted.'
    };
  }

  const forbiddenMatch = stripped.match(
    /\b(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|EXEC|EXECUTE|MERGE|TRUNCATE|GRANT|REVOKE|DENY|BACKUP|RESTORE|DBCC|SHUTDOWN|RECONFIGURE|OPENROWSET|OPENDATASOURCE|BULK|WAITFOR)\b|\b(?:XP_|SP_CONFIGURE)\w*|\bINTO\s+(?:#|@|\[?[A-Za-z_]|IDENT\b)/i
  );
  if (forbiddenMatch) {
    return {
      valid: false,
      reason: `Rejected prohibited SQL keyword or construct "${forbiddenMatch[0]}" in read-only inspector.`
    };
  }

  return { valid: true, sanitizedSql: trimmed };
}

function handleSqlServerStatus(args) {
  const server = (typeof args.server === 'string' && args.server.trim()) || DEFAULT_SERVER;
  const database = (typeof args.database === 'string' && args.database.trim()) || DEFAULT_DATABASE;
  const sqlcmdPath = resolveSqlcmdPath();
  const sqlClientFallbackAvailable = process.platform === 'win32';
  const canExecuteQueries = Boolean(sqlcmdPath) || sqlClientFallbackAvailable;

  const isLocalDb = /\(localdb\)/i.test(server);
  const localDbInstanceName = isLocalDb ? server.split('\\')[1] || 'MSSQLLocalDB' : null;
  const wantProbe = typeof args.probeConnection === 'boolean' ? args.probeConnection : true;
  const localDbInfo = isLocalDb ? inspectLocalDbInstance(localDbInstanceName, wantProbe && canExecuteQueries) : null;

  const shouldProbe = wantProbe && (!isLocalDb || localDbInfo?.state === 'Running');

  let probeResult = {
    serverReachable: false,
    databaseExists: false,
    databaseState: null,
    serverVersion: null,
    serverEdition: null,
    sqlTransport: sqlcmdPath ? 'sqlcmd' : sqlClientFallbackAvailable ? 'powershell-sqlclient' : 'none',
    note:
      isLocalDb && localDbInfo?.state !== 'Running'
        ? `LocalDB instance "${localDbInstanceName}" is currently "${localDbInfo?.state || 'Stopped'}". Run .agents/scripts/ensure-sqlserver-db.ps1 (or pass probeConnection: true) to start it.`
        : null
  };

  if (shouldProbe && canExecuteQueries) {
    const dbCheck = checkDatabaseExistsOnServer(server, database);
    probeResult = {
      serverReachable: dbCheck.serverReachable,
      databaseExists: dbCheck.databaseExists,
      databaseState: dbCheck.databaseState || null,
      serverVersion: dbCheck.productVersion || null,
      serverEdition: dbCheck.edition || null,
      sqlTransport: dbCheck.transport,
      note: dbCheck.databaseExists
        ? `Database "${database}" is online on ${server} (${dbCheck.productVersion || 'SQL Server'} via ${dbCheck.transport}).`
        : dbCheck.serverReachable
          ? `Connected to ${server} (${dbCheck.productVersion || 'SQL Server'} via ${dbCheck.transport}), but "${database}" is not created yet.`
          : `Could not connect to ${server}: ${dbCheck.connectionMessage}`
    };
  }

  return {
    isError: false,
    payload: {
      server,
      targetDatabase: database,
      readOnlyMode: true,
      sqlcmdAvailable: Boolean(sqlcmdPath),
      sqlcmdPath,
      sqlClientFallbackAvailable,
      localDb: localDbInfo,
      ...probeResult,
      planned3nfTableCount: 27,
      mcpRoutingNote:
        'Use this sqlserver-localdb-inspector MCP server for all SQL Server MotorcycleDealerDb checks; never use global dbhub (Oracle).'
    }
  };
}

function handleListSqlServerTables(args) {
  const server = (typeof args.server === 'string' && args.server.trim()) || DEFAULT_SERVER;
  const database = (typeof args.database === 'string' && args.database.trim()) || DEFAULT_DATABASE;
  const dbIdCheck = validateIdentifier(database, 'database');
  if (!dbIdCheck.valid) {
    return { isError: true, payload: { error: dbIdCheck.error } };
  }

  const isLocalDb = /\(localdb\)/i.test(server);
  if (isLocalDb) {
    const localDbInstanceName = server.split('\\')[1] || 'MSSQLLocalDB';
    const localInfo = inspectLocalDbInstance(localDbInstanceName, true);
    if (localInfo.installed && localInfo.state !== 'Running') {
      return {
        isError: false,
        payload: {
          server,
          database,
          localDbState: localInfo.state,
          databaseExists: false,
          tableCount: 0,
          tables: [],
          planned3nfTables: loadPlannedTables(),
          message: `LocalDB (${localDbInstanceName}) is currently Stopped. Start LocalDB via .agents/scripts/ensure-sqlserver-db.ps1.`
        }
      };
    }
  }

  if (database.toLowerCase() !== 'master') {
    const check = checkDatabaseExistsOnServer(server, database);
    if (!check.serverReachable || !check.databaseExists) {
      return {
        isError: false,
        payload: {
          server,
          database,
          serverReachable: check.serverReachable,
          databaseExists: false,
          tableCount: 0,
          tables: [],
          planned3nfTables: loadPlannedTables(),
          message: check.serverReachable
            ? `Database "${database}" has not been migrated yet on ${server}.`
            : `SQL Server instance "${server}" is not currently reachable: ${check.connectionMessage || 'offline'}.`
        }
      };
    }
  }

  const listSql = [
    'SET NOCOUNT ON;',
    'SELECT s.name AS SchemaName, t.name AS TableName, COALESCE(SUM(p.rows), 0) AS ApproximateRowCount',
    'FROM sys.tables t',
    'INNER JOIN sys.schemas s ON t.schema_id = s.schema_id',
    'LEFT JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)',
    "WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory'",
    'GROUP BY s.name, t.name',
    'ORDER BY s.name, t.name'
  ].join(' ');

  const res = executeSqlQuery({ server, db: database, sql: listSql, maxRows: MAX_ROWS_CAP });
  if (!res.ok) {
    return {
      isError: false,
      payload: {
        server,
        database,
        databaseExists: false,
        tables: [],
        planned3nfTables: loadPlannedTables(),
        message: res.message
      }
    };
  }

  return {
    isError: false,
    payload: {
      server,
      database,
      sqlTransport: res.transport,
      databaseExists: true,
      tableCount: res.rowCount,
      tables: res.rows
    }
  };
}

function handleInspectSqlServerSchema(args) {
  const server = (typeof args.server === 'string' && args.server.trim()) || DEFAULT_SERVER;
  const database = (typeof args.database === 'string' && args.database.trim()) || DEFAULT_DATABASE;
  const schemaName = (typeof args.schemaName === 'string' && args.schemaName.trim()) || 'dbo';
  const rawTable = typeof args.tableName === 'string' ? args.tableName.replace(/^\[?dbo\]?\./i, '') : '';

  const dbValid = validateIdentifier(database, 'database');
  if (!dbValid.valid) return { isError: true, payload: { error: dbValid.error } };
  const schemaValid = validateIdentifier(schemaName, 'schemaName');
  if (!schemaValid.valid) return { isError: true, payload: { error: schemaValid.error } };
  const tableValid = validateIdentifier(rawTable, 'tableName');
  if (!tableValid.valid) return { isError: true, payload: { error: tableValid.error } };

  const isLocalDb = /\(localdb\)/i.test(server);
  if (isLocalDb) {
    const localDbInstanceName = server.split('\\')[1] || 'MSSQLLocalDB';
    const localInfo = inspectLocalDbInstance(localDbInstanceName, true);
    if (localInfo.installed && localInfo.state !== 'Running') {
      return {
        isError: false,
        payload: {
          server,
          database,
          schemaName: schemaValid.identifier,
          tableName: tableValid.identifier,
          databaseExists: false,
          message: `LocalDB (${localDbInstanceName}) is Stopped. Use motorcycle-dealer-guardrails -> lookup_db_table_spec for design specs.`
        }
      };
    }
  }

  if (database.toLowerCase() !== 'master') {
    const dbCheck = checkDatabaseExistsOnServer(server, database);
    if (!dbCheck.serverReachable || !dbCheck.databaseExists) {
      return {
        isError: false,
        payload: {
          server,
          database,
          schemaName: schemaValid.identifier,
          tableName: tableValid.identifier,
          databaseExists: false,
          message: dbCheck.serverReachable
            ? `Database "${database}" is not migrated yet on ${server}. Call motorcycle-dealer-guardrails -> lookup_db_table_spec for the canonical 3NF specification.`
            : `Could not connect to ${server}: ${dbCheck.connectionMessage}`
        }
      };
    }
  }

  const s = schemaValid.identifier;
  const t = tableValid.identifier;

  const columnsSql = `SET NOCOUNT ON; SELECT c.column_id AS ColumnOrder, c.name AS ColumnName, ty.name AS DataType, c.max_length AS MaxLength, c.precision AS NumericPrecision, c.scale AS NumericScale, c.is_nullable AS IsNullable, c.is_identity AS IsIdentity FROM sys.columns c INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id INNER JOIN sys.objects tb ON c.object_id = tb.object_id AND tb.type IN ('U', 'V', 'S') INNER JOIN sys.schemas sc ON tb.schema_id = sc.schema_id WHERE sc.name = N'${s}' AND tb.name = N'${t}' ORDER BY c.column_id`;
  const indexesSql =
    `SET NOCOUNT ON; SELECT i.name AS IndexName, i.type_desc AS IndexType, i.is_primary_key AS IsPrimaryKey, i.is_unique AS IsUnique, ` +
    `(SELECT STRING_AGG(col.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) FROM sys.index_columns ic INNER JOIN sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) AS KeyColumns, ` +
    `(SELECT STRING_AGG(col.name, ', ') WITHIN GROUP (ORDER BY ic.index_column_id) FROM sys.index_columns ic INNER JOIN sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1) AS IncludedColumns, ` +
    `i.filter_definition AS FilterDefinition ` +
    `FROM sys.indexes i INNER JOIN sys.objects tb ON i.object_id = tb.object_id AND tb.type IN ('U', 'V', 'S') INNER JOIN sys.schemas sc ON tb.schema_id = sc.schema_id WHERE sc.name = N'${s}' AND tb.name = N'${t}' AND i.name IS NOT NULL ORDER BY i.is_primary_key DESC, i.name`;
  const fkSql = `SET NOCOUNT ON; SELECT fk.name AS ForeignKeyName, cp.name AS ColumnName, rs.name AS ReferencedSchema, rt.name AS ReferencedTable, cr.name AS ReferencedColumn, fk.delete_referential_action_desc AS OnDeleteAction FROM sys.foreign_keys fk INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id INNER JOIN sys.schemas sp ON tp.schema_id = sp.schema_id INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id INNER JOIN sys.schemas rs ON rt.schema_id = rs.schema_id INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id WHERE sp.name = N'${s}' AND tp.name = N'${t}' ORDER BY fk.name`;
  const checkSql = `SET NOCOUNT ON; SELECT cc.name AS ConstraintName, cc.definition AS Definition, cc.is_disabled AS IsDisabled FROM sys.check_constraints cc INNER JOIN sys.tables tp ON cc.parent_object_id = tp.object_id INNER JOIN sys.schemas sp ON tp.schema_id = sp.schema_id WHERE sp.name = N'${s}' AND tp.name = N'${t}' ORDER BY cc.name`;

  const batchRes = executeSqlBatch({
    server,
    db: database,
    queries: [columnsSql, indexesSql, fkSql, checkSql],
    maxRows: MAX_ROWS_CAP
  });
  if (!batchRes.ok) {
    return { isError: true, payload: { error: batchRes.message } };
  }

  const columns = batchRes.results[0]?.rows || [];
  const indexes = batchRes.results[1]?.rows || [];
  const foreignKeys = batchRes.results[2]?.rows || [];
  const checkConstraints = batchRes.results[3]?.rows || [];

  return {
    isError: false,
    payload: {
      server,
      database,
      sqlTransport: batchRes.transport,
      schemaName: s,
      tableName: t,
      tableFound: columns.length > 0,
      columns,
      indexes,
      foreignKeys,
      checkConstraints
    }
  };
}

function handleQueryReadOnlySql(args) {
  const validation = validateReadOnlySql(args.sql);
  if (!validation.valid) {
    return {
      isError: true,
      payload: {
        error: validation.reason,
        securityPolicy:
          'query_readonly_sql enforces single-statement SELECT/WITH queries only (max 200 rows) and blocks all DDL/DML/EXEC/semicolon constructs.'
      }
    };
  }

  const server = (typeof args.server === 'string' && args.server.trim()) || DEFAULT_SERVER;
  const database = (typeof args.database === 'string' && args.database.trim()) || DEFAULT_DATABASE;
  const dbValid = validateIdentifier(database, 'database');
  if (!dbValid.valid) {
    return { isError: true, payload: { error: dbValid.error } };
  }

  const requestedRows =
    typeof args.maxRows === 'number' && Number.isInteger(args.maxRows)
      ? Math.min(Math.max(args.maxRows, 1), MAX_ROWS_CAP)
      : MAX_ROWS_CAP;

  const isLocalDb = /\(localdb\)/i.test(server);
  if (isLocalDb) {
    const localDbInstanceName = server.split('\\')[1] || 'MSSQLLocalDB';
    const localInfo = inspectLocalDbInstance(localDbInstanceName, true);
    if (localInfo.installed && localInfo.state !== 'Running') {
      return {
        isError: false,
        payload: {
          server,
          database,
          localDbState: localInfo.state,
          databaseExists: false,
          rowCount: 0,
          rows: [],
          message: `LocalDB (${localDbInstanceName}) is currently Stopped. Run .agents/scripts/ensure-sqlserver-db.ps1 to start LocalDB.`
        }
      };
    }
  }

  if (database.toLowerCase() !== 'master') {
    const dbCheck = checkDatabaseExistsOnServer(server, database);
    if (!dbCheck.serverReachable || !dbCheck.databaseExists) {
      return {
        isError: false,
        payload: {
          server,
          database,
          databaseExists: false,
          rowCount: 0,
          rows: [],
          message: dbCheck.serverReachable
            ? `Database "${database}" is not migrated yet on ${server}.`
            : `SQL Server "${server}" is not reachable: ${dbCheck.connectionMessage}`
        }
      };
    }
  }

  const wrappedSql = `SET NOCOUNT ON; SET ROWCOUNT ${requestedRows + 1}; ${validation.sanitizedSql}`;
  const res = executeSqlQuery({ server, db: database, sql: wrappedSql, maxRows: requestedRows });
  if (!res.ok) {
    return {
      isError: true,
      payload: {
        server,
        database,
        error: res.message
      }
    };
  }

  return {
    isError: false,
    payload: {
      server,
      database,
      sqlTransport: res.transport,
      maxRowsCap: requestedRows,
      rowCount: res.rowCount,
      truncated: res.truncated,
      columns: res.columns,
      rows: res.rows
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
      case 'sqlserver_status':
        return handleSqlServerStatus(toolArgs);
      case 'list_sqlserver_tables':
        return handleListSqlServerTables(toolArgs);
      case 'inspect_sqlserver_schema':
        return handleInspectSqlServerSchema(toolArgs);
      case 'query_readonly_sql':
        return handleQueryReadOnlySql(toolArgs);
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

  if (!Object.prototype.hasOwnProperty.call(msg, 'id')) {
    return;
  }

  const id = msg.id;
  const method = msg.method;

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
