#!/usr/bin/env bash
# Audits Phase-Gate boundaries, Clean Architecture inward dependencies, PointLedgerEntries immutability,
# AGENTS.md size/frontmatter rules, and zero-secrets hygiene for the Motorcycle Dealer & Loyalty Platform.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

PHASE=0
DRY_RUN=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --phase|-Phase)
      PHASE="${2:-0}"
      shift 2
      ;;
    --dry-run|-DryRun)
      DRY_RUN=1
      shift
      ;;
    *)
      echo "[WARN] Ignoring unknown flag: $1"
      shift
      ;;
  esac
done

FAILURES=0
WARNINGS=0

pass() { echo "[PASS] $1"; }
info() { echo "[INFO] $1"; }
warn() { WARNINGS=$((WARNINGS + 1)); echo "[WARN] $1"; }
fail() { FAILURES=$((FAILURES + 1)); echo "[FAIL] $1"; }

# 1. Resolve Effective Phase
EFFECTIVE_PHASE="$PHASE"
if [[ "$EFFECTIVE_PHASE" -eq 0 ]]; then
  if [[ -f "${REPO_ROOT}/PROGRESS.md" ]]; then
    DETECTED="$(grep -E '\*\*Current Active Phase\*\*' "${REPO_ROOT}/PROGRESS.md" | sed -nE 's/.*\*\*Phase ([1-9][0-9]*).*/\1/p' | head -n 1 || true)"
    if [[ -n "$DETECTED" ]]; then
      EFFECTIVE_PHASE="$DETECTED"
    fi
  fi
  if [[ "$EFFECTIVE_PHASE" -eq 0 ]]; then
    if [[ -d "${REPO_ROOT}/backend" ]]; then
      EFFECTIVE_PHASE=3
    elif [[ -d "${REPO_ROOT}/.agents" ]]; then
      EFFECTIVE_PHASE=2
    else
      EFFECTIVE_PHASE=1
    fi
  fi
fi

MODE_LABEL="ENFORCE"
if [[ "$DRY_RUN" -eq 1 ]]; then
  MODE_LABEL="DRY-RUN"
fi
info "Running Dealer Platform Guardrails Audit (EffectivePhase=${EFFECTIVE_PHASE}, Mode=${MODE_LABEL}, Root=${REPO_ROOT})"

# 2. Audit AGENTS.md Rule Files (per-file byte caps & zero YAML frontmatter)
check_rule_file() {
  local rel="$1"
  local required="$2"
  local max_bytes="${3:-24000}"
  local full="${REPO_ROOT}/${rel}"

  if [[ ! -f "$full" ]]; then
    if [[ "$required" == "1" ]]; then
      fail "Required governance file missing: ${rel}"
    else
      info "Optional/peer rule file not present yet: ${rel}"
    fi
    return
  fi

  local size
  size="$(wc -c < "$full" | tr -d '[:space:]')"
  if [[ "$size" -ge "$max_bytes" ]]; then
    fail "${rel} exceeds ${max_bytes}-byte Antigravity rule limit (${size} bytes)."
  else
    pass "${rel} byte size valid (${size} < ${max_bytes} bytes)."
  fi

  local first_line
  first_line="$(head -n 1 "$full" | tr -d '\r')"
  if [[ "$first_line" == "---" ]]; then
    fail "${rel} starts with YAML frontmatter ('---'), which is prohibited for standalone AGENTS.md rules."
  else
    pass "${rel} has zero YAML frontmatter."
  fi
}

check_rule_file "AGENTS.md" "1" "24000"
check_rule_file ".agents/rules/AGENTS.md" "0" "12000"
check_rule_file ".agents/plugins/motorcycle-dealer-suite/rules/AGENTS.md" "0" "5000"
check_rule_file "backend/AGENTS.md" "0" "8000"
check_rule_file "frontend/AGENTS.md" "0" "6000"
check_rule_file "mobile/AGENTS.md" "0" "8000"

# 3. Root Hygiene & Phase-Gate Boundaries
if [[ -d "${REPO_ROOT}/node_modules" ]]; then
  fail "Prohibited root node_modules/ directory detected."
else
  pass "Zero root node_modules/ pollution."
fi

if [[ -f "${REPO_ROOT}/package.json" ]]; then
  fail "Prohibited root package.json detected."
else
  pass "Zero root package.json pollution."
fi

if [[ "$EFFECTIVE_PHASE" -le 2 ]]; then
  if [[ -d "${REPO_ROOT}/backend" ]]; then
    fail "Phase-Gate Violation (Phase ${EFFECTIVE_PHASE} <= 2): premature 'backend/' directory exists."
  else
    pass "Phase-Gate verified: zero premature 'backend/' directory in Phase ${EFFECTIVE_PHASE}."
  fi

  if [[ -d "${REPO_ROOT}/frontend" ]]; then
    fail "Phase-Gate Violation (Phase ${EFFECTIVE_PHASE} <= 2): premature 'frontend/' directory exists."
  else
    pass "Phase-Gate verified: zero premature 'frontend/' directory in Phase ${EFFECTIVE_PHASE}."
  fi

  CODE_COUNT="$(find "${REPO_ROOT}" -path "${REPO_ROOT}/.git" -prune -o \( -name '*.sln' -o -name '*.csproj' -o -name '*.cs' \) -print | wc -l | tr -d '[:space:]')"
  if [[ "$CODE_COUNT" -gt 0 ]]; then
    fail "Phase-Gate Violation: found ${CODE_COUNT} premature .sln/.csproj/.cs file(s) in Phase ${EFFECTIVE_PHASE}."
  else
    pass "Phase-Gate verified: zero premature .sln/.csproj/.cs files in Phase ${EFFECTIVE_PHASE}."
  fi
fi

# 4. Clean Architecture & 4-Table Financial/Audit Immutability Checks (Phases 3-9)
if [[ -d "${REPO_ROOT}/backend" ]]; then
  info "Auditing Clean Architecture inward dependencies (ADR-001) and 4-table financial/audit immutability (ADR-003)..."

  DOMAIN_CSPROJ="${REPO_ROOT}/backend/src/MotorcycleDealer.Domain/MotorcycleDealer.Domain.csproj"
  if [[ -f "$DOMAIN_CSPROJ" ]]; then
    if grep -Eq '<ProjectReference|Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore' "$DOMAIN_CSPROJ"; then
      fail "ADR-001 Violation: MotorcycleDealer.Domain.csproj contains outward project or framework references."
    else
      pass "ADR-001: MotorcycleDealer.Domain.csproj has zero outward dependencies."
    fi
  fi

  APP_CSPROJ="${REPO_ROOT}/backend/src/MotorcycleDealer.Application/MotorcycleDealer.Application.csproj"
  if [[ -f "$APP_CSPROJ" ]]; then
    if grep -Eq 'MotorcycleDealer\.(Infrastructure|WebApi)|Microsoft\.EntityFrameworkCore\.SqlServer|Microsoft\.AspNetCore\.App' "$APP_CSPROJ"; then
      fail "ADR-001 Violation: MotorcycleDealer.Application.csproj references Infrastructure/WebApi/Host packages."
    else
      pass "ADR-001: MotorcycleDealer.Application.csproj inward dependency rule verified."
    fi
  fi

  if [[ -d "${REPO_ROOT}/backend/src" ]]; then
    LEDGER_HITS="$(grep -RInE '(DELETE[[:space:]]+FROM|TRUNCATE[[:space:]]+TABLE)[[:space:]]+(\[?dbo\]?\.)?\[?(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)\]?|UPDATE[[:space:]]+(\[?dbo\]?\.)?\[?(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)\]?[[:space:]]+SET|(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)[[:space:]]*\.[[:space:]]*(Remove|RemoveRange|Update|UpdateRange|ExecuteDelete|ExecuteDeleteAsync|ExecuteUpdate|ExecuteUpdateAsync)[[:space:]]*\(' "${REPO_ROOT}/backend/src" || true)"
    if [[ -n "$LEDGER_HITS" ]]; then
      fail "ADR-003 Immutability Violation: PointLedgerEntries, SalesOrders, ServiceInvoices, or ServiceInvoiceLineItems mutation found in backend/src."
    else
      pass "ADR-003: Zero PointLedgerEntries, SalesOrders, ServiceInvoices & ServiceInvoiceLineItems UPDATE/DELETE mutations across backend/src."
    fi
  fi
fi

MOBILE_CORE_DIR="${REPO_ROOT}/mobile/src/MotorcycleDealer.Mobile.Core"
if [[ -d "$MOBILE_CORE_DIR" ]]; then
  if grep -iEq '<UseMaui>[[:space:]]*true[[:space:]]*</UseMaui>|Microsoft\.Maui\.' "${MOBILE_CORE_DIR}"/*.csproj 2>/dev/null; then
    fail "Phase 10 Mobile Purity Violation: MotorcycleDealer.Mobile.Core (*.csproj) must never set <UseMaui>true</UseMaui> or reference Microsoft.Maui.*."
  else
    pass "Phase 10 Mobile Purity: MotorcycleDealer.Mobile.Core (*.csproj) has zero <UseMaui>true</UseMaui> or Microsoft.Maui.* references."
  fi
fi

# 5. Secret Hygiene Scan
ENV_COUNT="$(find "${REPO_ROOT}" -path "${REPO_ROOT}/.git" -prune -o -path '*/node_modules' -prune -o \( -name '.env' -o -name '.env.*' -o -name '*.pfx' -o -name '*.pem' -o -name '*.key' \) ! -name '.env.example' -type f -print | wc -l | tr -d '[:space:]')"
if [[ "$ENV_COUNT" -gt 0 ]]; then
  fail "Secret Hygiene Violation: found ${ENV_COUNT} forbidden .env or private key/cert file(s)."
else
  pass "Secret Hygiene: zero .env / private certificate files in workspace."
fi

SECRET_HITS="$(grep -RInE --exclude-dir=.git --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj --exclude='audit-dealer-guardrails.*' --exclude='safety-guard.mjs' --exclude='*.docx' -e '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}' "${REPO_ROOT}" || true)"
if [[ -n "$SECRET_HITS" ]]; then
  fail "Secret Hygiene Violation: sensitive key/PAT token pattern detected."
else
  pass "Secret Hygiene: zero hardcoded private keys or PAT tokens."
fi

echo ""
if [[ "$FAILURES" -gt 0 ]]; then
  echo "Guardrails Audit FAILED with ${FAILURES} violation(s)."
  exit 1
fi

echo "Guardrails Audit PASSED (${WARNINGS} warning(s), Mode=${MODE_LABEL}, Phase=${EFFECTIVE_PHASE})."
exit 0
