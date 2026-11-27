#!/usr/bin/env bash
# Master verification orchestrator for the Motorcycle Dealer & Loyalty Management Platform:
# 1) Validates all .agents/**/*.json files parse as valid JSON
# 2) Validates all .agents/skills/*/SKILL.md files contain valid YAML frontmatter (name & description)
# 3) Runs audit-dealer-guardrails.sh
# 4) Runs node .agents/scripts/sync-progress.mjs --check
# 5) Runs node .agents/scripts/verify-post-task-sync.mjs (100% docs, code/snapshot & 183-day git schedule sync gate)
# 6) Tests MCP JSON-RPC 2.0 initialize + tools/list handshake on workspace Stdio MCP servers
# 7) Runs dotnet build & dotnet test once backend/MotorcycleDealer.sln exists (Phase 3+)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

DRY_RUN=0
PHASE=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run|-DryRun)
      DRY_RUN=1
      shift
      ;;
    --phase|-Phase)
      PHASE="${2:-0}"
      shift 2
      ;;
    *)
      echo "[WARN] Ignoring unknown argument: $1"
      shift
      ;;
  esac
done

FAILURES=0
MODE_LABEL="FULL"
if [[ "$DRY_RUN" -eq 1 ]]; then
  MODE_LABEL="DRY-RUN"
fi

pass() { echo "[PASS] $1"; }
info() { echo "[INFO] $1"; }
fail() { FAILURES=$((FAILURES + 1)); echo "[FAIL] $1"; }

info "=== Motorcycle Dealer & Loyalty Platform Master Verifier (Mode=${MODE_LABEL}) ==="

# Stage 1: Validate all .agents/**/*.json manifests
info "[Stage 1/7] Validating .agents/**/*.json manifests..."
if ! node --input-type=module -e '
import fs from "node:fs";
import path from "node:path";
const root = process.argv[1];
const agentsDir = path.join(root, ".agents");
function walk(dir) {
  let out = [];
  if (!fs.existsSync(dir)) return out;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) out = out.concat(walk(p));
    else if (ent.isFile() && ent.name.endsWith(".json")) out.push(p);
  }
  return out;
}
const files = walk(agentsDir);
for (const f of files) {
  JSON.parse(fs.readFileSync(f, "utf8"));
}
if (fs.existsSync(path.join(agentsDir, "plugins", "motorcycle-dealer-suite", "skills"))) {
  throw new Error("Duplicate skills/ directory inside .agents/plugins/motorcycle-dealer-suite/skills");
}
console.log(`[PASS] Parsed ${files.length} JSON manifest(s) under .agents/ cleanly.`);
' "$REPO_ROOT"; then
  fail "JSON manifest validation failed."
fi

# Stage 2: Validate .agents/skills/*/SKILL.md YAML frontmatter
info "[Stage 2/7] Validating .agents/skills/*/SKILL.md YAML frontmatter..."
if ! node --input-type=module -e '
import fs from "node:fs";
import path from "node:path";
const skillsDir = path.join(process.argv[1], ".agents", "skills");
if (!fs.existsSync(skillsDir)) {
  console.log("[INFO] .agents/skills/ directory not populated yet.");
  process.exit(0);
}
const dirs = fs.readdirSync(skillsDir, { withFileTypes: true }).filter((d) => d.isDirectory());
let count = 0;
for (const d of dirs) {
  const skillFile = path.join(skillsDir, d.name, "SKILL.md");
  if (!fs.existsSync(skillFile)) continue;
  const raw = fs.readFileSync(skillFile, "utf8");
  const m = raw.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  if (!m) throw new Error(`Missing YAML frontmatter in ${skillFile}`);
  const fm = m[1];
  const nm = fm.match(/^name:\s*["\x27]?([a-z0-9-]+)["\x27]?\s*$/m);
  const hasDesc = /^description:\s*\S+/m.test(fm);
  if (!nm || nm[1] !== d.name) throw new Error(`Frontmatter name mismatch in ${skillFile}`);
  if (!hasDesc) throw new Error(`Missing description in ${skillFile}`);
  count++;
}
console.log(`[PASS] Validated YAML frontmatter across ${count} SKILL.md file(s).`);
' "$REPO_ROOT"; then
  fail "SKILL.md frontmatter validation failed."
fi

# Stage 3: Run audit-dealer-guardrails.sh
info "[Stage 3/7] Executing audit-dealer-guardrails.sh..."
AUDIT_ARGS=()
if [[ "$PHASE" -gt 0 ]]; then
  AUDIT_ARGS+=(--phase "$PHASE")
fi
if [[ "$DRY_RUN" -eq 1 ]]; then
  AUDIT_ARGS+=(--dry-run)
fi
if bash "${SCRIPT_DIR}/audit-dealer-guardrails.sh" "${AUDIT_ARGS[@]}"; then
  pass "audit-dealer-guardrails.sh passed."
else
  fail "audit-dealer-guardrails.sh failed."
fi

# Stage 4: Run sync-progress.mjs --check
info "[Stage 4/7] Checking TASKS.md <-> PROGRESS.md synchronization..."
if node "${SCRIPT_DIR}/sync-progress.mjs" --check; then
  pass "sync-progress.mjs --check passed."
else
  fail "sync-progress.mjs --check failed."
fi

# Stage 5: Run verify-post-task-sync.mjs
info "[Stage 5/7] Executing Post-Task Docs, Code/Snapshot & 183-Day Git Schedule Sync Gate..."
POST_SYNC_ARGS=()
if [[ "$DRY_RUN" -eq 1 ]]; then
  POST_SYNC_ARGS+=(--dry-run)
fi
if node "${SCRIPT_DIR}/verify-post-task-sync.mjs" "${POST_SYNC_ARGS[@]}"; then
  pass "verify-post-task-sync.mjs passed."
else
  fail "verify-post-task-sync.mjs failed."
fi

# Stage 6: Test MCP JSON-RPC 2.0 initialize + tools/list handshake
info "[Stage 6/7] Testing workspace Stdio MCP servers (initialize + tools/list)..."
for SERVER_FILE in "mcp-dealer-guardrails-server.mjs" "mcp-sqlserver-localdb-server.mjs"; do
  SERVER_PATH="${SCRIPT_DIR}/${SERVER_FILE}"
  if [[ ! -f "$SERVER_PATH" ]]; then
    info "MCP server '${SERVER_FILE}' not present on disk yet; skipping handshake."
    continue
  fi
  if OUT="$(node --input-type=module -e '
import { spawn } from "node:child_process";
const serverPath = process.argv[1];
const child = spawn(process.execPath, [serverPath], { stdio: ["pipe", "pipe", "pipe"] });
let buf = "";
const responses = new Map();
const timer = setTimeout(() => { child.kill(); process.exit(1); }, 5000);
child.stdout.on("data", (chunk) => {
  buf += chunk.toString("utf8");
  const lines = buf.split(/\r?\n/);
  buf = lines.pop() || "";
  for (const line of lines) {
    if (!line.trim()) continue;
    const msg = JSON.parse(line);
    if (msg.id !== undefined) responses.set(msg.id, msg);
    if (responses.has(1) && responses.has(2)) {
      clearTimeout(timer);
      const initRes = responses.get(1);
      const listRes = responses.get(2);
      const tools = listRes.result && Array.isArray(listRes.result.tools) ? listRes.result.tools : null;
      if (initRes.error || !initRes.result?.capabilities || listRes.error || !tools || tools.length === 0) {
        child.kill();
        process.exit(1);
      }
      console.log(`${initRes.result.serverInfo?.name || "mcp"} (${tools.length} tools: ${tools.map((t) => t.name).join(", ")})`);
      child.stdin.end();
      child.kill();
      process.exit(0);
    }
  }
});
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "verify-dealer-platform", version: "1.0.0" } } }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} }) + "\n");
' "$SERVER_PATH")"; then
    pass "MCP JSON-RPC handshake verified: ${OUT}"
  else
    fail "MCP handshake failed for ${SERVER_FILE}."
  fi
done

# Stage 7: Build & Test Solution (Phase 3+)
info "[Stage 7/7] Checking .NET 10 solution build & test gate..."
SLN_PATH="${REPO_ROOT}/backend/MotorcycleDealer.sln"
if [[ -f "$SLN_PATH" ]]; then
  if [[ "$DRY_RUN" -eq 1 ]]; then
    info "[DRY-RUN] Solution detected at backend/MotorcycleDealer.sln; would run 'dotnet build --warnaserror' and 'dotnet test'."
  else
    if dotnet build "$SLN_PATH" --warnaserror && dotnet test "$SLN_PATH" --no-build; then
      pass "dotnet build --warnaserror and dotnet test passed."
    else
      fail ".NET build or test suite failed."
    fi
  fi
else
  pass "Phase <= 2 No-Code Gate active (backend/MotorcycleDealer.sln absent as expected)."
fi

MOBILE_SLN_PATH="${REPO_ROOT}/mobile/MotorcycleDealer.Mobile.sln"
MOBILE_UNIT_TESTS_DIR="${REPO_ROOT}/mobile/tests/MotorcycleDealer.Mobile.UnitTests"
if [[ -f "$MOBILE_SLN_PATH" ]]; then
  info "[Stage 7b/7] Checking .NET 10 MAUI Android static guardrails & Mobile.UnitTests..."
  if [[ -f "${SCRIPT_DIR}/verify-mobile-maui.mjs" ]]; then
    if node "${SCRIPT_DIR}/verify-mobile-maui.mjs"; then
      pass "verify-mobile-maui.mjs passed."
    else
      fail "verify-mobile-maui.mjs reported MAUI guardrail failure(s)."
    fi
  fi
  if [[ "$DRY_RUN" -eq 1 ]]; then
    info "[DRY-RUN] Mobile solution detected at mobile/MotorcycleDealer.Mobile.sln; would run 'dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests -c Release --warnaserror'."
  elif [[ -d "$MOBILE_UNIT_TESTS_DIR" ]]; then
    if dotnet test "$MOBILE_UNIT_TESTS_DIR" -c Release --warnaserror --nologo; then
      pass "dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests passed."
    else
      fail "dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests failed."
    fi
  fi
fi

echo ""
if [[ "$FAILURES" -gt 0 ]]; then
  echo "=== Verification FAILED (${FAILURES} failure(s)) ==="
  exit 1
fi

echo "=== All Dealer Platform Verification Gates PASSED (Mode=${MODE_LABEL}) ==="
exit 0
