#!/usr/bin/env bash
# Ensures SQL Server 2022 Docker container is healthy on Linux/macOS/WSL,
# and optionally runs dotnet ef database update when Phase 4+ exists.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

DRY_RUN=0
USE_DOCKER=1
MIGRATE=0
DB_NAME="MotorcycleDealerDb"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run|-DryRun)
      DRY_RUN=1
      shift
      ;;
    --use-docker|-UseDocker)
      USE_DOCKER=1
      shift
      ;;
    --migrate|-Migrate)
      MIGRATE=1
      shift
      ;;
    *)
      echo "[WARN] Ignoring unknown argument: $1"
      shift
      ;;
  esac
done

SLN_PATH="${REPO_ROOT}/backend/MotorcycleDealer.sln"
INFRA_PROJ="${REPO_ROOT}/backend/src/MotorcycleDealer.Infrastructure"
WEBAPI_PROJ="${REPO_ROOT}/backend/src/MotorcycleDealer.WebApi"
COMPOSE_PATH="${REPO_ROOT}/docker-compose.yml"

if [[ "$DRY_RUN" -eq 1 ]]; then
  echo "[INFO] [DRY-RUN] Ensuring SQL Server 2022 readiness for '${DB_NAME}' (UseDocker=${USE_DOCKER}, Migrate=${MIGRATE})"
  if command -v docker >/dev/null 2>&1; then
    echo "[INFO] [DRY-RUN] Docker CLI detected: $(command -v docker)"
  else
    echo "[INFO] [DRY-RUN] Docker CLI not currently in PATH"
  fi
  echo "[INFO] [DRY-RUN] Would inspect '${COMPOSE_PATH}' via: docker compose ps sqlserver"
  if [[ "$MIGRATE" -eq 1 ]]; then
    if [[ -f "$SLN_PATH" ]]; then
      echo "[INFO] [DRY-RUN] Would run: dotnet ef database update --project \"${INFRA_PROJ}\" --startup-project \"${WEBAPI_PROJ}\""
    else
      echo "[INFO] [DRY-RUN] backend/MotorcycleDealer.sln not scaffolded yet (Phase <= 2); migration deferred to Phase 4+."
    fi
  fi
  echo "[PASS] SQL Server readiness dry-run completed cleanly."
  exit 0
fi

if [[ ! -f "$COMPOSE_PATH" ]]; then
  echo "[WARN] docker-compose.yml not present yet (scaffolded in Phase 3 T-308). Skipping container startup."
  exit 0
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "[FAIL] docker CLI is required on Linux/macOS to host SQL Server 2022."
  exit 1
fi

echo "[INFO] Checking SQL Server 2022 container health via docker compose..."
docker compose -f "$COMPOSE_PATH" up -d sqlserver
docker compose -f "$COMPOSE_PATH" ps sqlserver
echo "[PASS] SQL Server 2022 container is running."

if [[ "$MIGRATE" -eq 1 ]]; then
  if [[ -f "$SLN_PATH" && -d "$INFRA_PROJ" && -d "$WEBAPI_PROJ" ]]; then
    echo "[INFO] Running EF Core database migrations for ${DB_NAME}..."
    dotnet ef database update --project "$INFRA_PROJ" --startup-project "$WEBAPI_PROJ"
    echo "[PASS] Database '${DB_NAME}' migrated cleanly."
  else
    echo "[INFO] backend/MotorcycleDealer.sln not scaffolded yet; skipping dotnet ef database update."
  fi
fi

exit 0
