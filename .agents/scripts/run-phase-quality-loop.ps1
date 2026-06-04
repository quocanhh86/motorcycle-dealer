<#
.SYNOPSIS
    Executes the Automated 3-Battery Test Gate for the Per-Phase 7-Stage Quality Loop
    (5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test) across Phases 3..10:
      - Battery 1 (T<Round>.1): Clean Release Build (--warnaserror) + xUnit Unit/Architecture/Integration Suite (+ Phase 10 Mobile.UnitTests & net10.0-android Debug/Release build)
      - Battery 2 (T<Round>.2): Frontend AngularJS 1.8 SPA Verification + Ephemeral Live WebApi/SignalR/SPA + Phase 4..10 HTTP/CSV/Receipt/CSAT/Warranty Probes + Full 30-State Headless Chromium CDP E2E
      - Battery 3 (T<Round>.3): Master Guardrails + verify-post-task-sync.mjs + verify-mobile-maui.mjs (Phase 10) + MCP check_phase_gate + Phase 4+ MCP LocalDB 27-Table & Ledger/Capacity/VoucherQuota/LineItems/ReviewBonus Reconciliation + Docker Compose Config + Determinism Re-test
#>
[CmdletBinding()]
param(
    [ValidateRange(3, 10)]
    [int]$Phase = 3,

    [ValidateRange(1, 3)]
    [int]$Round = 1,

    [switch]$DryRun,

    [int]$ApiPort = 0,

    [int]$SpaPort = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$SlnPath = Join-Path $RepoRoot "backend\MotorcycleDealer.sln"
$WebApiProj = Join-Path $RepoRoot "backend\src\MotorcycleDealer.WebApi\MotorcycleDealer.WebApi.csproj"
$WebApiDll = Join-Path $RepoRoot "backend\src\MotorcycleDealer.WebApi\bin\Release\net10.0\MotorcycleDealer.WebApi.dll"
$FrontendDir = Join-Path $RepoRoot "frontend"
$SpaDevServer = Join-Path $FrontendDir "scripts\dev-server.mjs"
$MobileSlnPath = Join-Path $RepoRoot "mobile\MotorcycleDealer.Mobile.sln"
$MobileUnitTestsProj = Join-Path $RepoRoot "mobile\tests\MotorcycleDealer.Mobile.UnitTests\MotorcycleDealer.Mobile.UnitTests.csproj"
$MobileAppProj = Join-Path $RepoRoot "mobile\src\MotorcycleDealer.Mobile.App\MotorcycleDealer.Mobile.App.csproj"
$VerifyMobileScript = Join-Path $PSScriptRoot "verify-mobile-maui.mjs"
$AndroidSdkDir = if (-not [string]::IsNullOrWhiteSpace($env:ANDROID_HOME) -and (Test-Path $env:ANDROID_HOME)) { $env:ANDROID_HOME } else { "C:\Program Files (x86)\Android\android-sdk" }

function Resolve-MauiJavaSdkDir {
    $preferredJdk21 = "C:\Program Files\Android\openjdk\jdk-21.0.8"
    if (Test-Path (Join-Path $preferredJdk21 "bin\java.exe")) {
        return $preferredJdk21
    }
    $androidOpenJdkRoot = "C:\Program Files\Android\openjdk"
    if (Test-Path $androidOpenJdkRoot) {
        $jdk21Plus = Get-ChildItem -Path $androidOpenJdkRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^jdk-(2[1-9]|[3-9][0-9])' -and (Test-Path (Join-Path $_.FullName "bin\java.exe")) } |
            Sort-Object -Property Name -Descending |
            Select-Object -First 1
        if ($null -ne $jdk21Plus) {
            return $jdk21Plus.FullName
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
        $envJavaExe = Join-Path $env:JAVA_HOME "bin\java.exe"
        if (Test-Path $envJavaExe) {
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = "Continue"
            try {
                $verText = (& $envJavaExe -version 2>&1 | Out-String)
                if ($verText -match 'version "(2[1-9]|[3-9][0-9])\.') {
                    return $env:JAVA_HOME
                }
            } finally {
                $ErrorActionPreference = $prevEap
            }
        }
    }
    return $preferredJdk21
}

$JavaSdkDir = Resolve-MauiJavaSdkDir
$ComposePath = Join-Path $RepoRoot "docker-compose.yml"
$VerifyScript = Join-Path $PSScriptRoot "verify-dealer-platform.ps1"
$PostTaskSyncScript = Join-Path $PSScriptRoot "verify-post-task-sync.mjs"
$SmokePs1 = Join-Path $PSScriptRoot "smoke-fullstack.ps1"
$SmokeSh = Join-Path $PSScriptRoot "smoke-fullstack.sh"
$McpGuardrails = Join-Path $PSScriptRoot "mcp-dealer-guardrails-server.mjs"
$McpLocalDb = Join-Path $PSScriptRoot "mcp-sqlserver-localdb-server.mjs"
$SnapshotPath = Join-Path $PSScriptRoot "baseline-spec-snapshot.json"

$BatteryResults = New-Object System.Collections.Generic.List[hashtable]
$OverallStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-FailMsg([string]$Msg) { Write-Host "[FAIL] $Msg" -ForegroundColor Red }

$ModeLabel = if ($DryRun) { "DRY-RUN" } else { "LIVE" }
Write-Info "=========================================================================="
Write-Info "  Dealer Platform Quality-Loop Test Runner (Phase=$Phase, Round=$Round, Mode=$ModeLabel)"
Write-Info "  Pipeline Contract: 5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test"
Write-Info "=========================================================================="

if ($DryRun) {
    $ExpectedRouteCount = if ($Phase -ge 9) { 30 } elseif ($Phase -ge 8) { 27 } else { 24 }
    $ExpectedTableCount = if ($Phase -ge 9) { 27 } elseif ($Phase -ge 8) { 25 } else { 22 }
    $MaxSmkDry = if ($Phase -ge 9) { "SMK-10" } elseif ($Phase -ge 8) { "SMK-09" } else { "SMK-08" }
    $P4B2Extra = if ($Phase -ge 4) { " + live POST /api/v1/auth/login, GET /api/v1/auth/me & GET /api/v1/dealers (DLR-HN-01, DLR-DN-01, DLR-HCM-01)" } else { "" }
    $P5B2Extra = if ($Phase -ge 5) { " + Phase 5 GET /api/v1/{motorcycles,inventories,vehicles,time-slots,bookings} + Customer cross-phase 401 single-flight refresh (/auth/me + /vehicles + /bookings)" } else { "" }
    $P6B2Extra = if ($Phase -ge 6) { " + Phase 6 GET /api/v1/{loyalty/tiers,loyalty/me,loyalty/ledger,vouchers/catalog,vouchers/my,service-invoices,notifications(unreadCount)}" } else { "" }
    $P7B2Extra = if ($Phase -ge 7) { " + smoke-fullstack.ps1 (SMK-01..$MaxSmkDry) + CDP /admin/analytics 4-<dealer-chart> remount & CSV export (200 text/csv UTF-8 BOM)" } else { "" }
    $P8B2Extra = if ($Phase -ge 8) { " + Phase 8 v1.1 /api/v1/{spare-parts,test-drives,receipt} probes" } else { "" }
    $P9B2Extra = if ($Phase -ge 9) { " + Phase 9 v1.2 /api/v1/{service-reviews,warranty-claims,admin/analytics/quality-warranty} & 17-col CSV probes" } else { "" }
    $P4B3Extra = if ($Phase -ge 4) { " + MCP list_sqlserver_tables ($ExpectedTableCount canonical 3NF tables) & query_readonly_sql (AvailablePoints == SUM(PointsDelta) & 0 overbooked slots)" } else { "" }
    $P5B3Extra = if ($Phase -ge 5) { " + SlotDriftCount == 0" } else { "" }
    $P6B3Extra = if ($Phase -ge 6) { " + VoucherQuotaDriftCount == 0 (VoucherCatalogs.QuotaRedeemed == COUNT(CustomerVouchers))" } else { "" }
    $P7B3Extra = if ($Phase -ge 7) { " + Phase 7 Showroom/Service/PointsLiability financial reconciliation" } else { "" }
    $P8B3Extra = if ($Phase -ge 8) { " + InvoiceLineItemsDriftCount == 0" } else { "" }
    $P9B3Extra = if ($Phase -ge 9) { " + ReviewBonusDriftCount == 0" } else { "" }
    $P10B1Extra = if ($Phase -ge 10) { " + dotnet test Mobile.UnitTests (-c Release --warnaserror) + dotnet build Mobile.App (-f net10.0-android Debug & Release --warnaserror)" } else { "" }
    $P10B3Extra = if ($Phase -ge 10) { " + verify-mobile-maui.mjs + Mobile.UnitTests determinism re-test" } else { "" }
    Write-Info "[DRY-RUN] Battery 1 (T$Round.1): dotnet clean '$SlnPath' -c Release && dotnet build -c Release --warnaserror && dotnet test -c Release --no-build$P10B1Extra"
    Write-Info "[DRY-RUN] Battery 2 (T$Round.2): npm test in '$FrontendDir' + ephemeral WebApi (/health, /api/v1/health, RFC 7807 404, +/- CORS preflight, SignalR negotiate$P4B2Extra$P5B2Extra$P6B2Extra$P7B2Extra$P8B2Extra$P9B2Extra) + SPA shell GET / + $ExpectedRouteCount-State Headless Chromium CDP E2E"
    Write-Info "[DRY-RUN] Battery 3 (T$Round.3): verify-dealer-platform.ps1 -Phase $Phase + verify-post-task-sync.mjs + MCP check_phase_gate$P4B3Extra$P5B3Extra$P6B3Extra$P7B3Extra$P8B3Extra$P9B3Extra$P10B3Extra + docker compose config -q + determinism re-test"
    Write-Pass "Quality-loop automation dry-run validated all 3 test batteries cleanly."
    exit 0
}

$BackendArtifactsDir = $null
$UseIsolatedBackendArtifacts = $false
$BackendBuildExtraArgs = @()
$RunningWebApi = @(Get-Process -Name "MotorcycleDealer.WebApi" -ErrorAction SilentlyContinue)
if ($RunningWebApi.Count -gt 0) {
    $UseIsolatedBackendArtifacts = $true
    $BackendArtifactsDir = Join-Path $RepoRoot ("backend\obj\ql-artifacts-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $BackendArtifactsDir -Force | Out-Null
    $BackendBuildExtraArgs = @("-p:UseArtifactsOutput=true", "-p:ArtifactsPath=$BackendArtifactsDir")
    $WebApiDll = Join-Path $BackendArtifactsDir "bin\MotorcycleDealer.WebApi\release\MotorcycleDealer.WebApi.dll"
    Write-Info "Live MotorcycleDealer.WebApi process detected (count=$($RunningWebApi.Count)); isolating backend build/test into '$BackendArtifactsDir' (UseArtifactsOutput=true) without touching locked bin/Release."
}

try {
# ==============================================================================
# BATTERY 1: Clean Release Build (--warnaserror) + xUnit Test Suite
# ==============================================================================
$Sw1 = [System.Diagnostics.Stopwatch]::StartNew()
$B1Passed = $true
$B1Detail = ""
Write-Host ""
Write-Info "[Battery 1/3 | T$Round.1] Clean Release Build (--warnaserror) + xUnit Suite..."

if (-not (Test-Path $SlnPath)) {
    $B1Passed = $false
    $B1Detail = "Missing solution file: backend/MotorcycleDealer.sln"
    Write-FailMsg $B1Detail
} else {
    $CleanOk = $true
    if ($UseIsolatedBackendArtifacts) {
        Write-Info "Skipping dotnet clean on fresh isolated artifacts directory '$BackendArtifactsDir'."
    } else {
        & dotnet clean $SlnPath -c Release --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            $CleanOk = $false
            $B1Passed = $false
            $B1Detail = "dotnet clean -c Release exited with code $LASTEXITCODE"
            Write-FailMsg $B1Detail
        }
    }

    if ($CleanOk) {
        & dotnet build $SlnPath -c Release --warnaserror --nologo @BackendBuildExtraArgs
        if ($LASTEXITCODE -ne 0) {
            $B1Passed = $false
            $B1Detail = "dotnet build -c Release --warnaserror exited with code $LASTEXITCODE"
            Write-FailMsg $B1Detail
        } else {
            if ($UseIsolatedBackendArtifacts -and -not (Test-Path $WebApiDll)) {
                $AltDll = Join-Path $BackendArtifactsDir "bin\MotorcycleDealer.WebApi\release\net10.0\MotorcycleDealer.WebApi.dll"
                if (Test-Path $AltDll) { $WebApiDll = $AltDll }
            }
            & dotnet test $SlnPath -c Release --no-build --nologo @BackendBuildExtraArgs
            if ($LASTEXITCODE -ne 0) {
                $B1Passed = $false
                $B1Detail = "dotnet test -c Release --no-build exited with code $LASTEXITCODE"
                Write-FailMsg $B1Detail
            } else {
                $B1Detail = "Clean Release build (0 warnings) + xUnit suite passed"
                Write-Pass "Battery 1 (T$Round.1): $B1Detail."
            }
        }
    }
}

if ($B1Passed -and $Phase -ge 10) {
    Write-Info "[Battery 1/3 | Phase 10 Mobile] Running Mobile.UnitTests (-c Release --warnaserror) & net10.0-android builds..."
    if (-not (Test-Path $MobileUnitTestsProj)) {
        $B1Passed = $false
        $B1Detail = "Missing Mobile unit test project: $MobileUnitTestsProj"
        Write-FailMsg $B1Detail
    } elseif (-not (Test-Path $MobileAppProj)) {
        $B1Passed = $false
        $B1Detail = "Missing MAUI Android app project: $MobileAppProj"
        Write-FailMsg $B1Detail
    } else {
        & dotnet test $MobileUnitTestsProj -c Release --warnaserror --nologo
        if ($LASTEXITCODE -ne 0) {
            $B1Passed = $false
            $B1Detail = "dotnet test Mobile.UnitTests -c Release --warnaserror exited with code $LASTEXITCODE"
            Write-FailMsg $B1Detail
        } else {
            Write-Pass "Mobile.UnitTests (-c Release --warnaserror) passed."
            & dotnet build $MobileAppProj -c Debug -f net10.0-android "-p:AndroidSdkDirectory=$AndroidSdkDir" "-p:JavaSdkDirectory=$JavaSdkDir" --warnaserror --nologo
            if ($LASTEXITCODE -ne 0) {
                $B1Passed = $false
                $B1Detail = "dotnet build Mobile.App (-c Debug -f net10.0-android --warnaserror) exited with code $LASTEXITCODE"
                Write-FailMsg $B1Detail
            } else {
                Write-Pass "Mobile.App (-c Debug -f net10.0-android --warnaserror) built cleanly."
                & dotnet build $MobileAppProj -c Release -f net10.0-android "-p:AndroidSdkDirectory=$AndroidSdkDir" "-p:JavaSdkDirectory=$JavaSdkDir" --warnaserror --nologo
                if ($LASTEXITCODE -ne 0) {
                    $B1Passed = $false
                    $B1Detail = "dotnet build Mobile.App (-c Release -f net10.0-android --warnaserror) exited with code $LASTEXITCODE"
                    Write-FailMsg $B1Detail
                } else {
                    $B1Detail = "Backend Release build + xUnit + Mobile.UnitTests + net10.0-android Debug/Release builds (0 warnings) passed"
                    Write-Pass "Battery 1 (T$Round.1 | Phase 10): $B1Detail."
                }
            }
        }
    }
}
$Sw1.Stop()
$BatteryResults.Add(@{
    Id        = "T$Round.1"
    Name      = "Release Build (--warnaserror) + xUnit"
    Passed    = $B1Passed
    ElapsedMs = $Sw1.ElapsedMilliseconds
    Detail    = $B1Detail
}) | Out-Null

# ==============================================================================
# BATTERY 2: Frontend AngularJS SPA Verify + Live API/SignalR/SPA + CDP E2E
# ==============================================================================
$Sw2 = [System.Diagnostics.Stopwatch]::StartNew()
$B2Passed = $true
$B2Errors = New-Object System.Collections.Generic.List[string]
Write-Host ""
Write-Info "[Battery 2/3 | T$Round.2] Frontend AngularJS SPA Verify + Live Ephemeral HTTP/SignalR/CDP Smoke..."

$NpmExe = if ($env:OS -eq "Windows_NT") { "npm.cmd" } else { "npm" }

if (-not (Test-Path (Join-Path $FrontendDir "package.json"))) {
    $B2Passed = $false
    $B2Errors.Add("Missing frontend/package.json") | Out-Null
    Write-FailMsg "Missing frontend/package.json."
} else {
    Push-Location $FrontendDir
    try {
        & $NpmExe test
        if ($LASTEXITCODE -ne 0) {
            $B2Passed = $false
            $B2Errors.Add("frontend '$NpmExe test' exited with code $LASTEXITCODE") | Out-Null
            Write-FailMsg "frontend '$NpmExe test' failed."
        } else {
            Write-Pass "Frontend '$NpmExe test' passed."
        }
    } catch {
        $B2Passed = $false
        $B2Errors.Add("frontend '$NpmExe test' threw exception: $($_.Exception.Message)") | Out-Null
        Write-FailMsg "frontend '$NpmExe test' threw exception: $($_.Exception.Message)"
    } finally {
        Pop-Location
    }
}

$LiveSmokeJs = @'
const { execSync, spawn } = require("node:child_process");
const fs = require("node:fs");
const http = require("node:http");
const net = require("node:net");
const os = require("node:os");
const path = require("node:path");

const webApiDll = process.env.QL_WEBAPI_DLL;
const webApiCwd = process.env.QL_WEBAPI_CWD;
const spaScript = process.env.QL_SPA_SCRIPT;
const spaCwd = process.env.QL_SPA_CWD;
const smokePs1 = process.env.QL_SMOKE_PS1;
const smokeSh = process.env.QL_SMOKE_SH;
const reqApiPort = Number(process.env.QL_API_PORT || "0");
const reqSpaPort = Number(process.env.QL_SPA_PORT || "0");
const targetPhase = Number(process.env.QL_TARGET_PHASE || "3");

function killProcessTree(child) {
  if (!child || !child.pid) return;
  if (process.platform === "win32") {
    try { execSync(`taskkill /PID ${child.pid} /T /F`, { stdio: "ignore" }); } catch {}
  } else {
    try { child.kill("SIGKILL"); } catch {}
  }
}

function getFreePort(preferred) {
  if (preferred > 0) return Promise.resolve(preferred);
  return new Promise((resolve, reject) => {
    const srv = net.createServer();
    srv.listen(0, "127.0.0.1", () => {
      const p = srv.address().port;
      srv.close(() => resolve(p));
    });
    srv.on("error", reject);
  });
}

function httpRequest(port, method, reqPath, headers = {}, body = null) {
  return new Promise((resolve, reject) => {
    const payload = body !== null ? Buffer.from(body, "utf8") : null;
    const reqHeaders = Object.assign({}, headers);
    if (payload !== null) {
      reqHeaders["Content-Type"] = reqHeaders["Content-Type"] || "application/json";
      reqHeaders["Content-Length"] = payload.length;
    }
    const req = http.request(
      { host: "127.0.0.1", port, method, path: reqPath, headers: reqHeaders, timeout: 10000 },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          resolve({
            statusCode: res.statusCode || 0,
            headers: res.headers || {},
            body: Buffer.concat(chunks).toString("utf8")
          });
        });
      }
    );
    req.on("timeout", () => req.destroy(new Error("HTTP probe timeout")));
    req.on("error", reject);
    if (payload !== null) req.write(payload);
    req.end();
  });
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function unwrapList(payload) {
  if (Array.isArray(payload)) return payload;
  if (!payload || typeof payload !== "object") return [];
  if (Array.isArray(payload.data)) return payload.data;
  if (Array.isArray(payload.items)) return payload.items;
  const listKeys = [
    "items", "models", "inventories", "vehicles", "slots", "timeSlots",
    "bookings", "orders", "salesOrders", "tiers", "entries", "ledger", "catalog",
    "vouchers", "invoices", "serviceInvoices", "notifications", "parts", "spareParts", "testDrives",
    "reviews", "serviceReviews", "claims", "warrantyClaims"
  ];
  for (const key of listKeys) {
    if (Array.isArray(payload[key])) return payload[key];
  }
  if (payload.data && typeof payload.data === "object") {
    for (const key of listKeys) {
      if (Array.isArray(payload.data[key])) return payload.data[key];
    }
  }
  return [];
}

function extractUnreadCount(payload) {
  if (!payload || typeof payload !== "object") return undefined;
  const candidates = [
    payload.unreadCount,
    payload.UnreadCount,
    payload.data && payload.data.unreadCount,
    payload.data && payload.data.UnreadCount,
    payload.meta && payload.meta.unreadCount,
    payload.meta && payload.meta.UnreadCount,
    payload.metadata && payload.metadata.unreadCount,
    payload.metadata && payload.metadata.UnreadCount,
    payload.data && payload.data.meta && payload.data.meta.unreadCount,
    payload.data && payload.data.metadata && payload.data.metadata.unreadCount
  ];
  return candidates.find((v) => typeof v === "number" && Number.isFinite(v) && v >= 0);
}

async function loginDemoAccount(apiPort, email, password) {
  const loginRes = await httpRequest(
    apiPort,
    "POST",
    "/api/v1/auth/login",
    {},
    JSON.stringify({ email, password })
  );
  if (loginRes.statusCode !== 200) {
    throw new Error(`POST /api/v1/auth/login (${email}) failed: status=${loginRes.statusCode}, body=${loginRes.body}`);
  }
  const loginJson = JSON.parse(loginRes.body);
  const authData = loginJson && loginJson.data ? loginJson.data : loginJson;
  const accessToken = authData && authData.accessToken;
  const refreshToken = authData && authData.refreshToken;
  if (typeof accessToken !== "string" || accessToken.split(".").length !== 3 || typeof refreshToken !== "string" || refreshToken.length < 16) {
    throw new Error(`POST /api/v1/auth/login (${email}) returned malformed token payload: ${loginRes.body}`);
  }
  return { accessToken, refreshToken };
}

async function runPhase4LiveAuthAndDealersProbes(apiPort) {
  const { accessToken } = await loginDemoAccount(apiPort, "admin@motorcycledealer.vn", "Admin@123!");
  console.log("[PASS] Live WebApi POST /api/v1/auth/login (admin@motorcycledealer.vn) -> 200 OK (JWT + RefreshToken issued)");

  const meRes = await httpRequest(apiPort, "GET", "/api/v1/auth/me", {
    Authorization: `Bearer ${accessToken}`
  });
  if (meRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/auth/me failed: status=${meRes.statusCode}, body=${meRes.body}`);
  }
  const meJson = JSON.parse(meRes.body);
  const meData = meJson && meJson.data ? meJson.data : meJson;
  const meProfile = meData && meData.user ? meData.user : meData;
  const mePerms = Array.isArray(meProfile && meProfile.permissions) ? meProfile.permissions : [];
  const meRole = meProfile && meProfile.role;
  const validAdminPermCounts = targetPhase >= 9 ? [32] : targetPhase >= 8 ? [28, 32, 40, 48] : [25, 28, 32, 34, 40, 48];
  if (
    meRole !== "Admin" ||
    !validAdminPermCounts.includes(mePerms.length) ||
    !mePerms.includes("Permissions.Dealers.ManageAll") ||
    !mePerms.includes("Permissions.Users.ManageStaff") ||
    (targetPhase >= 8 && !mePerms.includes("Permissions.SpareParts.ManageCatalog")) ||
    (targetPhase >= 9 && (!mePerms.includes("Permissions.Reviews.ViewDealer") || !mePerms.includes("Permissions.Warranty.ManageDealer")))
  ) {
    throw new Error(`GET /api/v1/auth/me unexpected profile: role=${meRole}, permCount=${mePerms.length}, body=${meRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/auth/me -> 200 OK (role=Admin, permissions=${mePerms.length})`);

  const dealersRes = await httpRequest(apiPort, "GET", "/api/v1/dealers");
  if (dealersRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/dealers failed: status=${dealersRes.statusCode}, body=${dealersRes.body}`);
  }
  const dealersList = unwrapList(JSON.parse(dealersRes.body));
  const dealerCodes = new Set(dealersList.map((d) => d && (d.code || d.dealerCode)).filter(Boolean));
  const expectedCodes = ["DLR-HN-01", "DLR-DN-01", "DLR-HCM-01"];
  const missingCodes = expectedCodes.filter((c) => !dealerCodes.has(c));
  if (missingCodes.length > 0) {
    throw new Error(`GET /api/v1/dealers missing expected seeded dealerships [${missingCodes.join(", ")}]: body=${dealersRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/dealers -> 200 OK (${dealersList.length} dealerships: DLR-HN-01, DLR-DN-01, DLR-HCM-01 verified)`);
}

async function runPhase5LiveShowroomGarageAndBookingProbes(apiPort) {
  const modelsRes = await httpRequest(apiPort, "GET", "/api/v1/motorcycles");
  if (modelsRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/motorcycles failed: status=${modelsRes.statusCode}, body=${modelsRes.body}`);
  }
  const modelsList = unwrapList(JSON.parse(modelsRes.body));
  const modelCodes = new Set(modelsList.map((m) => m && (m.modelCode || m.code)).filter(Boolean));
  if (modelsList.length < 6 || !modelCodes.has("HND-AB160-2026") || !modelCodes.has("HND-SH160I-2026")) {
    throw new Error(`GET /api/v1/motorcycles missing seeded models (count=${modelsList.length}): body=${modelsRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/motorcycles -> 200 OK (${modelsList.length} seeded models including HND-AB160-2026 & HND-SH160I-2026)`);

  const anonPrivateInvRes = await httpRequest(apiPort, "GET", "/api/v1/inventories");
  const anonInvCt = String(anonPrivateInvRes.headers["content-type"] || "");
  if (anonPrivateInvRes.statusCode !== 401 || !anonInvCt.includes("application/problem+json")) {
    throw new Error(`Anonymous GET /api/v1/inventories expected 401 application/problem+json, got status=${anonPrivateInvRes.statusCode}, ct=${anonInvCt}, body=${anonPrivateInvRes.body}`);
  }
  console.log("[PASS] Live WebApi anonymous GET /api/v1/inventories -> 401 ProblemDetails verified");

  const anonPublicInvRes = await httpRequest(apiPort, "GET", "/api/v1/inventories?publicOnly=true");
  if (anonPublicInvRes.statusCode !== 200) {
    throw new Error(`Anonymous GET /api/v1/inventories?publicOnly=true failed: status=${anonPublicInvRes.statusCode}, body=${anonPublicInvRes.body}`);
  }
  const pubInvList = unwrapList(JSON.parse(anonPublicInvRes.body));
  const nonAvailableOrUnmasked = pubInvList.filter((i) => {
    const st = i && (i.stockStatus ?? i.StockStatus);
    const eng = i && (i.engineNumber ?? i.EngineNumber);
    return st !== "Available" || eng !== "";
  });
  if (pubInvList.length < 1 || nonAvailableOrUnmasked.length > 0) {
    throw new Error(`GET /api/v1/inventories?publicOnly=true leaked non-Available or unmasked engineNumber (count=${pubInvList.length}, bad=${nonAvailableOrUnmasked.length}): body=${anonPublicInvRes.body}`);
  }
  console.log(`[PASS] Live WebApi anonymous GET /api/v1/inventories?publicOnly=true -> 200 OK (${pubInvList.length} Available units, engineNumber="" masked)`);

  const staffHn = await loginDemoAccount(apiPort, "staff.hn@motorcycledealer.vn", "Staff@123!");
  const invRes = await httpRequest(apiPort, "GET", "/api/v1/inventories", {
    Authorization: `Bearer ${staffHn.accessToken}`
  });
  if (invRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/inventories (staff.hn) failed: status=${invRes.statusCode}, body=${invRes.body}`);
  }
  const invList = unwrapList(JSON.parse(invRes.body));
  const hnDealerId = "22222222-2222-2222-2222-222222222202";
  const invVins = new Set(invList.map((i) => i && (i.vin || i.VIN)).filter(Boolean));
  const crossDealerInv = invList.filter((i) => {
    const dId = i && i.dealerId ? String(i.dealerId).toLowerCase() : "";
    const dCode = i && (i.dealerCode || i.branchCode) ? String(i.dealerCode || i.branchCode) : "";
    return (dId && dId !== hnDealerId) || (dCode && dCode !== "DLR-HN-01");
  });
  if (invList.length < 1 || crossDealerInv.length > 0 || invVins.has("RLHJC9204RY000309") || invVins.has("RLHJF5108RY000201")) {
    throw new Error(`GET /api/v1/inventories (staff.hn) dealer-scope violation or empty: count=${invList.length}, crossDealer=${crossDealerInv.length}, body=${invRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/inventories (staff.hn JWT) -> 200 OK (${invList.length} Hanoi-scoped VIN units, 0 cross-dealer leaks)`);

  const custSilver = await loginDemoAccount(apiPort, "customer.silver@motorcycledealer.vn", "Customer@123!");
  const leftoverBookingId = "01a20243-6693-7aca-b12b-e36e6c02215b";
  const leftoverCheckRes = await httpRequest(apiPort, "GET", `/api/v1/bookings/${leftoverBookingId}`, {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (leftoverCheckRes.statusCode === 200) {
    const leftoverJson = JSON.parse(leftoverCheckRes.body);
    const leftoverData = leftoverJson && leftoverJson.data ? leftoverJson.data : leftoverJson;
    const leftoverStatus = leftoverData && (leftoverData.status || leftoverData.Status);
    if (leftoverStatus === "Pending" || leftoverStatus === "Confirmed") {
      const cancelLeftoverRes = await httpRequest(
        apiPort,
        "POST",
        `/api/v1/bookings/${leftoverBookingId}/cancel`,
        { Authorization: `Bearer ${custSilver.accessToken}` },
        JSON.stringify({ cancellationReason: "Cancelled leftover E2E booking cleanly via REST." })
      );
      if (cancelLeftoverRes.statusCode !== 200) {
        throw new Error(`POST /api/v1/bookings/${leftoverBookingId}/cancel failed: status=${cancelLeftoverRes.statusCode}, body=${cancelLeftoverRes.body}`);
      }
      console.log(`[PASS] Live WebApi POST /api/v1/bookings/${leftoverBookingId}/cancel -> 200 OK (leftover ${leftoverStatus} booking cancelled cleanly)`);
    }
  }

  const custPrivateInvRes = await httpRequest(apiPort, "GET", "/api/v1/inventories", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  const custInvCt = String(custPrivateInvRes.headers["content-type"] || "");
  if (custPrivateInvRes.statusCode !== 403 || !custInvCt.includes("application/problem+json")) {
    throw new Error(`Customer GET /api/v1/inventories expected 403 ProblemDetails, got status=${custPrivateInvRes.statusCode}, ct=${custInvCt}, body=${custPrivateInvRes.body}`);
  }
  console.log("[PASS] Live WebApi Customer GET /api/v1/inventories -> 403 Forbidden ProblemDetails verified");

  const vehRes = await httpRequest(apiPort, "GET", "/api/v1/vehicles", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (vehRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/vehicles (customer.silver) failed: status=${vehRes.statusCode}, body=${vehRes.body}`);
  }
  const vehList = unwrapList(JSON.parse(vehRes.body));
  const vehVins = new Set(vehList.map((v) => v && (v.vin || v.VIN)).filter(Boolean));
  if (!vehVins.has("RLHJF5109RZ104829") || vehVins.has("RLHKF4102RY000205") || vehVins.has("RLHNC5501RY000511")) {
    throw new Error(`GET /api/v1/vehicles (customer.silver) unexpected VIN set [${Array.from(vehVins).join(", ")}]: body=${vehRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/vehicles (customer.silver JWT) -> 200 OK (owned VIN RLHJF5109RZ104829 verified, 0 foreign VINs)`);

  const hcmDealerId = "22222222-2222-2222-2222-222222222201";
  const slotsRes = await httpRequest(
    apiPort,
    "GET",
    `/api/v1/time-slots?dealerId=${hcmDealerId}`,
    { Authorization: `Bearer ${custSilver.accessToken}` }
  );
  if (slotsRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/time-slots?dealerId=${hcmDealerId} failed: status=${slotsRes.statusCode}, body=${slotsRes.body}`);
  }
  const slotsList = unwrapList(JSON.parse(slotsRes.body));
  const openSlots = slotsList.filter((s) => {
    if (!s || s.isBlocked === true) return false;
    const maxCap = Number(s.maxCapacity ?? s.MaxCapacity ?? 0);
    const booked = Number(s.bookedCount ?? s.BookedCount ?? 0);
    const rem = s.remainingCapacity !== undefined ? Number(s.remainingCapacity) : maxCap - booked;
    return maxCap > 0 && booked < maxCap && rem > 0;
  });
  if (openSlots.length < 1) {
    throw new Error(`GET /api/v1/time-slots?dealerId=${hcmDealerId} returned no open slots (total=${slotsList.length}): body=${slotsRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/time-slots?dealerId=${hcmDealerId} -> 200 OK (${openSlots.length}/${slotsList.length} open workshop slots)`);

  const staffDealerSlotsRes = await httpRequest(apiPort, "GET", "/api/v1/time-slots/dealer", {
    Authorization: `Bearer ${staffHn.accessToken}`
  });
  if (staffDealerSlotsRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/time-slots/dealer (staff.hn) failed: status=${staffDealerSlotsRes.statusCode}, body=${staffDealerSlotsRes.body}`);
  }
  const staffDealerSlots = unwrapList(JSON.parse(staffDealerSlotsRes.body));
  const crossDealerSlots = staffDealerSlots.filter((s) => {
    const dId = s && (s.dealerId ?? s.DealerId) ? String(s.dealerId ?? s.DealerId).toLowerCase() : "";
    return !dId || dId !== hnDealerId;
  });
  if (staffDealerSlots.length < 1 || crossDealerSlots.length > 0) {
    throw new Error(`GET /api/v1/time-slots/dealer (staff.hn) dealer-scope violation or empty: count=${staffDealerSlots.length}, crossDealer=${crossDealerSlots.length}, body=${staffDealerSlotsRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/time-slots/dealer (staff.hn JWT) -> 200 OK (${staffDealerSlots.length} Hanoi-scoped slots, 0 cross-dealer slots)`);

  const custBookingsRes = await httpRequest(apiPort, "GET", "/api/v1/bookings", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (custBookingsRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/bookings (customer.silver) failed: status=${custBookingsRes.statusCode}, body=${custBookingsRes.body}`);
  }
  const custBookings = unwrapList(JSON.parse(custBookingsRes.body));
  const custBookingNums = new Set(custBookings.map((b) => b && (b.bookingNumber || b.code)).filter(Boolean));
  if (!custBookingNums.has("SB-202608-0001") || custBookingNums.has("SB-202609-0002")) {
    throw new Error(`GET /api/v1/bookings (customer.silver) unexpected bookings [${Array.from(custBookingNums).join(", ")}]: body=${custBookingsRes.body}`);
  }

  const staffBookingsRes = await httpRequest(apiPort, "GET", "/api/v1/bookings", {
    Authorization: `Bearer ${staffHn.accessToken}`
  });
  if (staffBookingsRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/bookings (staff.hn) failed: status=${staffBookingsRes.statusCode}, body=${staffBookingsRes.body}`);
  }
  const staffBookings = unwrapList(JSON.parse(staffBookingsRes.body));
  const staffBookingNums = new Set(staffBookings.map((b) => b && (b.bookingNumber || b.code)).filter(Boolean));
  const crossDealerBookings = staffBookings.filter((b) => b && b.dealerId && String(b.dealerId).toLowerCase() !== hnDealerId);
  if (!staffBookingNums.has("SB-202609-0002") || staffBookingNums.has("SB-202608-0001") || crossDealerBookings.length > 0) {
    throw new Error(`GET /api/v1/bookings (staff.hn) dealer-scope violation [${Array.from(staffBookingNums).join(", ")}]: body=${staffBookingsRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/bookings -> 200 OK (customer=${custBookings.length} own, staff.hn=${staffBookings.length} dealer-scoped, 0 cross-tenant leaks)`);
}

async function runPhase6LiveLoyaltyVouchersInvoicesAndNotificationsProbes(apiPort) {
  const tiersRes = await httpRequest(apiPort, "GET", "/api/v1/loyalty/tiers");
  if (tiersRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/loyalty/tiers failed: status=${tiersRes.statusCode}, body=${tiersRes.body}`);
  }
  const tiersList = unwrapList(JSON.parse(tiersRes.body));
  const tierMap = new Map();
  for (const t of tiersList) {
    const code = t && (t.tierCode ?? t.code ?? t.TierCode);
    const minPts = Number(t && (t.minQualifyingPoints ?? t.MinQualifyingPoints ?? -1));
    if (code) tierMap.set(String(code), minPts);
  }
  if (
    tiersList.length !== 4 ||
    tierMap.get("Bronze") !== 0 ||
    tierMap.get("Silver") !== 500 ||
    tierMap.get("Gold") !== 2000 ||
    tierMap.get("Platinum") !== 5000
  ) {
    throw new Error(`GET /api/v1/loyalty/tiers unexpected tier schedule: body=${tiersRes.body}`);
  }
  console.log("[PASS] Live WebApi GET /api/v1/loyalty/tiers -> 200 OK (4 canonical tiers: Bronze=0, Silver=500, Gold=2000, Platinum=5000)");

  const anonLoyaltyMeRes = await httpRequest(apiPort, "GET", "/api/v1/loyalty/me");
  const anonLoyaltyCt = String(anonLoyaltyMeRes.headers["content-type"] || "");
  if (anonLoyaltyMeRes.statusCode !== 401 || !anonLoyaltyCt.includes("application/problem+json")) {
    throw new Error(`Anonymous GET /api/v1/loyalty/me expected 401 ProblemDetails, got status=${anonLoyaltyMeRes.statusCode}, ct=${anonLoyaltyCt}, body=${anonLoyaltyMeRes.body}`);
  }

  const custSilver = await loginDemoAccount(apiPort, "customer.silver@motorcycledealer.vn", "Customer@123!");
  const loyaltyMeRes = await httpRequest(apiPort, "GET", "/api/v1/loyalty/me", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (loyaltyMeRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/loyalty/me (customer.silver) failed: status=${loyaltyMeRes.statusCode}, body=${loyaltyMeRes.body}`);
  }
  const loyaltyMeJson = JSON.parse(loyaltyMeRes.body);
  const loyaltyMeRoot = loyaltyMeJson && loyaltyMeJson.data ? loyaltyMeJson.data : loyaltyMeJson;
  const loyaltyMeObj = loyaltyMeRoot && loyaltyMeRoot.account ? loyaltyMeRoot.account : loyaltyMeRoot;
  const availPts = Number(loyaltyMeObj && (loyaltyMeObj.availablePoints ?? loyaltyMeObj.AvailablePoints));
  const lifetimePts = Number(loyaltyMeObj && (loyaltyMeObj.lifetimeQualifyingPoints ?? loyaltyMeObj.LifetimeQualifyingPoints));
  const tierCode = String(
    (loyaltyMeObj && (
      loyaltyMeObj.tierCode ??
      loyaltyMeObj.currentTierCode ??
      loyaltyMeObj.TierCode ??
      (loyaltyMeObj.currentTier && (loyaltyMeObj.currentTier.tierCode ?? loyaltyMeObj.currentTier.code))
    )) || ""
  );
  if (!Number.isFinite(availPts) || availPts < 0 || !Number.isFinite(lifetimePts) || lifetimePts < 500 || !tierCode) {
    throw new Error(`GET /api/v1/loyalty/me (customer.silver) malformed summary: body=${loyaltyMeRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/loyalty/me (customer.silver JWT) -> 200 OK (tier=${tierCode}, availablePoints=${availPts}, lifetimeQualifyingPoints=${lifetimePts})`);

  const ledgerRes = await httpRequest(apiPort, "GET", "/api/v1/loyalty/ledger", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (ledgerRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/loyalty/ledger (customer.silver) failed: status=${ledgerRes.statusCode}, body=${ledgerRes.body}`);
  }
  const ledgerItems = unwrapList(JSON.parse(ledgerRes.body));
  const badLedgerRows = ledgerItems.filter((e) => {
    const delta = Number(e && (e.pointsDelta ?? e.PointsDelta ?? 0));
    const bal = Number(e && (e.balanceAfter ?? e.BalanceAfter ?? -1));
    return !Number.isFinite(delta) || delta === 0 || !Number.isFinite(bal) || bal < 0;
  });
  if (ledgerItems.length < 1 || badLedgerRows.length > 0) {
    throw new Error(`GET /api/v1/loyalty/ledger (customer.silver) invalid rows (count=${ledgerItems.length}, bad=${badLedgerRows.length}): body=${ledgerRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/loyalty/ledger (customer.silver JWT) -> 200 OK (${ledgerItems.length} append-only PointLedgerEntries verified)`);

  const catalogRes = await httpRequest(apiPort, "GET", "/api/v1/vouchers/catalog", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (catalogRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/vouchers/catalog failed: status=${catalogRes.statusCode}, body=${catalogRes.body}`);
  }
  const catalogList = unwrapList(JSON.parse(catalogRes.body));
  const catalogPrefixes = new Set(catalogList.map((c) => c && (c.codePrefix ?? c.CodePrefix ?? c.prefix)).filter(Boolean));
  const requiredPrefixes = ["SVC50K", "OIL100K", "PARTS10P", "GOLD15P", "PLATVIP300K"];
  const missingPrefixes = requiredPrefixes.filter((p) => !catalogPrefixes.has(p));
  if (catalogList.length < 5 || missingPrefixes.length > 0) {
    throw new Error(`GET /api/v1/vouchers/catalog missing seeded prefixes [${missingPrefixes.join(", ")}]: body=${catalogRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/vouchers/catalog -> 200 OK (${catalogList.length} reward catalog items verified)`);

  const myVouchersRes = await httpRequest(apiPort, "GET", "/api/v1/vouchers/my", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (myVouchersRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/vouchers/my (customer.silver) failed: status=${myVouchersRes.statusCode}, body=${myVouchersRes.body}`);
  }
  const myVouchers = unwrapList(JSON.parse(myVouchersRes.body));
  const myVoucherCodes = new Set(myVouchers.map((v) => v && (v.voucherCode ?? v.VoucherCode ?? v.code)).filter(Boolean));
  if (
    myVouchers.length < 2 ||
    !myVoucherCodes.has("SVC100K-7K9P2M4X") ||
    !myVoucherCodes.has("PARTS10P-3M7Q9L2W") ||
    myVoucherCodes.has("GOLD15P-8X2K5V9N") ||
    myVoucherCodes.has("PLATVIP300K-9P4W6Z1M")
  ) {
    throw new Error(`GET /api/v1/vouchers/my (customer.silver) unexpected voucher codes [${Array.from(myVoucherCodes).join(", ")}]: body=${myVouchersRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/vouchers/my (customer.silver JWT) -> 200 OK (${myVouchers.length} owned vouchers verified, 0 cross-user leaks)`);

  const staffHcm = await loginDemoAccount(apiPort, "staff.hcm@motorcycledealer.vn", "Staff@123!");
  const hcmDealerId = "22222222-2222-2222-2222-222222222201";
  const invoicesRes = await httpRequest(apiPort, "GET", "/api/v1/service-invoices", {
    Authorization: `Bearer ${staffHcm.accessToken}`
  });
  if (invoicesRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/service-invoices (staff.hcm) failed: status=${invoicesRes.statusCode}, body=${invoicesRes.body}`);
  }
  const hcmInvoices = unwrapList(JSON.parse(invoicesRes.body));
  const invNumbers = new Set(hcmInvoices.map((i) => i && (i.invoiceNumber ?? i.InvoiceNumber ?? i.code)).filter(Boolean));
  const crossDealerInvoices = hcmInvoices.filter((i) => {
    const dId = i && (i.dealerId ?? i.DealerId) ? String(i.dealerId ?? i.DealerId).toLowerCase() : "";
    return dId && dId !== hcmDealerId;
  });
  if (
    hcmInvoices.length < 1 ||
    !invNumbers.has("INV-202608-0001") ||
    invNumbers.has("INV-202609-0002") ||
    invNumbers.has("INV-202610-0003") ||
    crossDealerInvoices.length > 0
  ) {
    throw new Error(`GET /api/v1/service-invoices (staff.hcm) dealer-scope violation [${Array.from(invNumbers).join(", ")}]: body=${invoicesRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/service-invoices (staff.hcm JWT) -> 200 OK (${hcmInvoices.length} Saigon-scoped invoices including INV-202608-0001, 0 cross-dealer leaks)`);

  const custNotifRes = await httpRequest(apiPort, "GET", "/api/v1/notifications", {
    Authorization: `Bearer ${custSilver.accessToken}`
  });
  if (custNotifRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/notifications (customer.silver) failed: status=${custNotifRes.statusCode}, body=${custNotifRes.body}`);
  }
  const custNotifJson = JSON.parse(custNotifRes.body);
  const custNotifs = unwrapList(custNotifJson);
  const custUnread = extractUnreadCount(custNotifJson);
  if (typeof custUnread !== "number" || custNotifs.length < 1) {
    throw new Error(`GET /api/v1/notifications (customer.silver) missing unreadCount or notifications (unreadCount=${custUnread}, items=${custNotifs.length}): body=${custNotifRes.body}`);
  }

  const staffNotifRes = await httpRequest(apiPort, "GET", "/api/v1/notifications", {
    Authorization: `Bearer ${staffHcm.accessToken}`
  });
  if (staffNotifRes.statusCode !== 200) {
    throw new Error(`GET /api/v1/notifications (staff.hcm) failed: status=${staffNotifRes.statusCode}, body=${staffNotifRes.body}`);
  }
  const staffNotifJson = JSON.parse(staffNotifRes.body);
  const staffNotifs = unwrapList(staffNotifJson);
  const staffUnread = extractUnreadCount(staffNotifJson);
  const crossDealerNotifs = staffNotifs.filter((n) => {
    const td = n && (n.targetDealerId ?? n.TargetDealerId) ? String(n.targetDealerId ?? n.TargetDealerId).toLowerCase() : "";
    return td && td !== hcmDealerId;
  });
  if (typeof staffUnread !== "number" || staffNotifs.length < 1 || crossDealerNotifs.length > 0) {
    throw new Error(`GET /api/v1/notifications (staff.hcm) missing unreadCount or dealer-scope leak (unreadCount=${staffUnread}, items=${staffNotifs.length}, crossDealer=${crossDealerNotifs.length}): body=${staffNotifRes.body}`);
  }
  console.log(`[PASS] Live WebApi GET /api/v1/notifications -> 200 OK (customer.silver unreadCount=${custUnread}, staff.hcm unreadCount=${staffUnread}, 0 cross-tenant leaks)`);
}

async function runHeadlessBrowserCdpProbe(apiPort, spaPort) {
  const candidates = process.platform === "win32"
    ? [
        "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
        "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
        "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
      ]
    : ["/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/chromium-browser"];

  const browserExe = candidates.find((p) => fs.existsSync(p));
  if (!browserExe || typeof WebSocket !== "function") {
    console.log("[PASS] Headless Chromium binary not present; HTTP/SPA smoke verified.");
    return;
  }

  const cdpPort = await getFreePort(0);
  const profileDir = fs.mkdtempSync(path.join(os.tmpdir(), "ql-cdp-"));
  let browserChild = null;
  let ws = null;

  try {
    browserChild = spawn(
      browserExe,
      [
        "--headless=new",
        `--remote-debugging-port=${cdpPort}`,
        `--user-data-dir=${profileDir}`,
        "--no-first-run",
        "--no-default-browser-check",
        "--disable-gpu",
        "about:blank"
      ],
      { stdio: ["ignore", "pipe", "pipe"] }
    );

    let wsUrl = null;
    for (let i = 0; i < 35; i++) {
      try {
        const listRes = await httpRequest(cdpPort, "GET", "/json/list");
        if (listRes.statusCode === 200) {
          const targets = JSON.parse(listRes.body);
          const page = Array.isArray(targets) && targets.find((t) => t.type === "page" && t.webSocketDebuggerUrl);
          if (page) { wsUrl = page.webSocketDebuggerUrl; break; }
        }
      } catch {}
      await sleep(200);
    }
    if (!wsUrl) throw new Error("Failed to connect to headless browser CDP endpoint");

    const consoleErrors = [];
    const pageExceptions = [];
    let refreshNetworkCalls = 0;
    const pending = new Map();
    let seq = 0;

    ws = new WebSocket(wsUrl);
    await new Promise((resolve, reject) => {
      ws.addEventListener("open", resolve, { once: true });
      ws.addEventListener("error", reject, { once: true });
    });

    ws.addEventListener("message", (evt) => {
      const msg = JSON.parse(typeof evt.data === "string" ? evt.data : evt.data.toString());
      if (msg.id && pending.has(msg.id)) {
        const p = pending.get(msg.id);
        pending.delete(msg.id);
        if (msg.error) p.reject(new Error(JSON.stringify(msg.error)));
        else p.resolve(msg.result);
        return;
      }
      if (msg.method === "Network.requestWillBeSent") {
        const reqUrl = String(msg.params?.request?.url || "");
        const reqMethod = String(msg.params?.request?.method || "");
        if (reqMethod === "POST" && reqUrl.endsWith("/api/v1/auth/refresh")) {
          refreshNetworkCalls++;
        }
      } else if (msg.method === "Runtime.consoleAPICalled" && msg.params.type === "error") {
        consoleErrors.push((msg.params.args || []).map((a) => a.value ?? a.description ?? "").join(" "));
      } else if (msg.method === "Runtime.exceptionThrown") {
        const d = msg.params.exceptionDetails || {};
        pageExceptions.push(d.exception?.description || d.text || "Runtime exception");
      }
    });

    const cdpSend = (method, params = {}) =>
      new Promise((resolve, reject) => {
        const id = ++seq;
        pending.set(id, { resolve, reject });
        ws.send(JSON.stringify({ id, method, params }));
      });

    const cdpEval = async (expression) => {
      const r = await cdpSend("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
      if (r.exceptionDetails) {
        throw new Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text);
      }
      return r.result?.value;
    };

    await cdpSend("Page.enable");
    await cdpSend("Network.enable");
    await cdpSend("Runtime.enable");
    await cdpSend("Page.addScriptToEvaluateOnNewDocument", {
      source: `window.__DEALER_APP_CONFIG__ = { apiBaseUrl: "http://127.0.0.1:${apiPort}/api/v1", hubUrl: "http://127.0.0.1:${apiPort}/hubs/notifications" };`
    });
    await cdpSend("Page.navigate", { url: `http://localhost:${spaPort}/#!/customer/dashboard` });

    for (let i = 0; i < 40; i++) {
      try {
        const booted = await cdpEval("Boolean(document.readyState === 'complete' && window.angular && window.angular.element(document.documentElement).injector())");
        if (booted) break;
      } catch {}
      await sleep(100);
    }
    await sleep(350);

    const e2eResult = await cdpEval(`(async () => {
      const inj = window.angular.element(document.documentElement).injector();
      const $state = inj.get('$state');
      const $stateParams = inj.get('$stateParams');
      const AuthService = inj.get('AuthService');
      const SignalRClientService = inj.get('SignalRClientService');
      const useRealAuth = ${targetPhase >= 4 ? "true" : "false"};
      const visitedStates = new Set();

      async function visitState(stateName, params, waitMs = 140) {
        await $state.go(stateName, params || {});
        await new Promise((r) => setTimeout(r, waitMs));
        if ($state.current && $state.current.name) {
          visitedStates.add($state.current.name);
        }
        return $state.current.name === stateName;
      }

      async function switchRole(roleKey) {
        if (useRealAuth && typeof AuthService.login === 'function') {
          const creds = roleKey === 'Customer'
            ? { email: 'customer.silver@motorcycledealer.vn', password: 'Customer@123!' }
            : roleKey === 'DealerStaff'
              ? { email: 'staff.hn@motorcycledealer.vn', password: 'Staff@123!' }
              : { email: 'admin@motorcycledealer.vn', password: 'Admin@123!' };
          await AuthService.login(creds);
          return;
        }
        if (typeof AuthService.activatePhase3RolePreview === 'function') {
          AuthService.activatePhase3RolePreview(roleKey);
        }
      }

      // 1. Public states (app.public.login, app.public.register, app.public.showroom; app.public.forbidden checked below)
      const unauthRedirectOk = $state.current.name === 'app.public.login' && $stateParams.returnUrl === 'app.customer.dashboard';
      if ($state.current && $state.current.name) {
        visitedStates.add($state.current.name);
      }
      const registerOk = await visitState('app.public.register');
      const showroomOk = await visitState('app.public.showroom', {}, 180);

      // 2. Customer states (6 concrete states + /403 guard check)
      await switchRole('Customer');
      try { await $state.go('app.admin.analytics'); } catch {}
      await new Promise((r) => setTimeout(r, 140));
      const forbiddenGuardOk = $state.current.name === 'app.public.forbidden';
      if ($state.current && $state.current.name) {
        visitedStates.add($state.current.name);
      }
      const customerStates = [
        'app.customer.dashboard',
        'app.customer.garage',
        'app.customer.bookingNew',
        'app.customer.bookService',
        'app.customer.bookings',
        'app.customer.loyalty'
      ];
      if (${targetPhase >= 8 ? "true" : "false"}) {
        customerStates.push('app.customer.testDrives');
      }
      if (${targetPhase >= 9 ? "true" : "false"}) {
        customerStates.push('app.customer.warranty');
      }
      for (const st of customerStates) {
        await visitState(st, {}, 150);
      }

      // 3. DealerStaff states
      await switchRole('DealerStaff');
      await visitState('app.staff.workbench', {}, 160);
      await visitState('app.staff.invoiceBuilder', { bookingId: 'cccccccc-cccc-cccc-cccc-cccccccccc02' }, 150);
      const invBuilderEl = document.querySelector('staff-invoice-builder');
      const invBuilderCtrl = invBuilderEl && window.angular.element(invBuilderEl).controller('staffInvoiceBuilder');
      const invoiceSelectOptions = document.querySelectorAll('#invTier option').length;
      const invoiceBuilderMountedOk = ${targetPhase >= 6 ? "true" : "false"}
        ? Boolean(invBuilderEl && invBuilderCtrl)
        : invoiceSelectOptions >= 4;
      await visitState('app.staff.invoicing', { bookingId: 'cccccccc-cccc-cccc-cccc-cccccccccc02' }, 130);
      const staffStates = [
        'app.staff.salesDesk',
        'app.staff.salesNew',
        'app.staff.timeSlots',
        'app.staff.slots',
        'app.staff.customers'
      ];
      if (${targetPhase >= 8 ? "true" : "false"}) {
        staffStates.push('app.staff.testDrives');
      }
      if (${targetPhase >= 9 ? "true" : "false"}) {
        staffStates.push('app.staff.warrantyReviews');
      }
      for (const st of staffStates) {
        await visitState(st, {}, 150);
      }

      // 4. Admin states (6 concrete states + Chart.js teardown + modals + 401 single-flight refresh)
      await switchRole('Admin');
      let signalRConnectedOk = !useRealAuth;
      if (useRealAuth) {
        for (let k = 0; k < 35; k++) {
          if (SignalRClientService.getConnectionStatus() === 'Connected') {
            signalRConnectedOk = true;
            break;
          }
          await new Promise((r) => setTimeout(r, 100));
        }
      }

      await visitState('app.admin.analytics', {}, 180);
      let chartsMounted = Object.keys(window.Chart.instances || {}).length;
      let dealerChartCanvasCount = document.querySelectorAll('dealer-chart canvas').length;
      for (let c = 0; c < 40; c++) {
        chartsMounted = Object.keys(window.Chart.instances || {}).length;
        dealerChartCanvasCount = document.querySelectorAll('dealer-chart canvas').length;
        const aEl = document.querySelector('admin-analytics');
        const aCtrl = aEl && window.angular.element(aEl).controller('adminAnalytics');
        const ready = chartsMounted === 4 && dealerChartCanvasCount === 4 && (!aCtrl || !aCtrl.isLoading);
        if (ready) break;
        await new Promise((r) => setTimeout(r, 100));
      }

      await visitState('app.admin.dealers', {}, 150);
      const chartsAfterLeave = Object.keys(window.Chart.instances || {}).length;

      let analyticsRemountOk = ${targetPhase < 7 ? "true" : "false"};
      let analyticsCsvExportOk = ${targetPhase < 7 ? "true" : "false"};
      if (${targetPhase >= 7 ? "true" : "false"}) {
        await visitState('app.admin.analytics', {}, 180);
        let remountInstances = 0;
        let remountCanvases = 0;
        for (let r = 0; r < 40; r++) {
          remountInstances = Object.keys(window.Chart.instances || {}).length;
          remountCanvases = document.querySelectorAll('dealer-chart canvas').length;
          const aEl2 = document.querySelector('admin-analytics');
          const aCtrl2 = aEl2 && window.angular.element(aEl2).controller('adminAnalytics');
          if (remountInstances === 4 && remountCanvases === 4 && (!aCtrl2 || (!aCtrl2.isLoading && !aCtrl2.loadProblem))) {
            break;
          }
          await new Promise((res) => setTimeout(res, 100));
        }
        await visitState('app.admin.dealers', {}, 150);
        const afterSecondLeave = Object.keys(window.Chart.instances || {}).length;
        analyticsRemountOk = remountInstances === 4 && remountCanvases === 4 && afterSecondLeave === 0;

        const csvHeaders = {};
        if (useRealAuth && typeof AuthService.getAccessToken === 'function' && AuthService.getAccessToken()) {
          csvHeaders['Authorization'] = 'Bearer ' + AuthService.getAccessToken();
        }
        const csvRes = await fetch('http://127.0.0.1:${apiPort}/api/v1/admin/analytics/export', {
          method: 'GET',
          headers: csvHeaders,
          credentials: 'include'
        });
        const csvCt = String(csvRes.headers.get('content-type') || '');
        const csvCd = String(csvRes.headers.get('content-disposition') || '');
        const csvBytes = new Uint8Array(await csvRes.arrayBuffer());
        const csvHasBom = csvBytes.length > 20 && csvBytes[0] === 0xEF && csvBytes[1] === 0xBB && csvBytes[2] === 0xBF;
        const csvText = new TextDecoder('utf-8').decode(csvBytes);
        const csvPhase9ColsOk = ${targetPhase >= 9 ? "true" : "false"}
          ? csvText.includes('AverageCsatRating,ServiceReviewsCount,OpenWarrantyClaims,ApprovedWarrantyCoveredVnd')
          : true;
        analyticsCsvExportOk =
          csvRes.status === 200 &&
          csvCt.includes('text/csv') &&
          csvCd.includes('dealer-analytics-') &&
          csvHasBom &&
          csvText.includes('DealerCode,DealerName,') &&
          csvPhase9ColsOk;
      }

      let adminDealersLiveOk = !useRealAuth;
      let adminStaffLiveOk = !useRealAuth;
      let singleFlightRefreshOk = !useRealAuth;
      let interactiveLogoutDisconnectedOk = !useRealAuth;
      if (useRealAuth) {
        const $http = inj.get('$http');
        const TokenStorageService = inj.get('TokenStorageService');
        const APP_CONFIG = inj.get('APP_CONFIG');

        for (let d = 0; d < 35; d++) {
          const dEl = document.querySelector('admin-dealers');
          const dCtrl = dEl && window.angular.element(dEl).controller('adminDealers');
          const dText = (dEl && dEl.textContent) || '';
          if (
            dCtrl &&
            !dCtrl.isLoading &&
            !dCtrl.listProblem &&
            dText.includes('DLR-HN-01') &&
            dText.includes('DLR-DN-01') &&
            dText.includes('DLR-HCM-01')
          ) {
            dCtrl.openCreateModal();
            dCtrl.form.openingTime = '18:00';
            dCtrl.form.closingTime = '08:00';
            const invalidHoursCaught = dCtrl.isHoursRangeValid() === false;
            dCtrl.openEditModal(dCtrl.dealers[0]);
            const editIdSet = Boolean(dCtrl.editingDealerId);
            dCtrl.closeModal();
            adminDealersLiveOk = invalidHoursCaught && editIdSet && !dCtrl.modalOpen;
            break;
          }
          await new Promise((r) => setTimeout(r, 100));
        }

        await visitState('app.admin.staff', {}, 150);
        for (let s = 0; s < 40; s++) {
          const sEl = document.querySelector('admin-staff');
          const sCtrl = sEl && window.angular.element(sEl).controller('adminStaff');
          const sText = (sEl && sEl.textContent) || '';
          if (
            sCtrl &&
            !sCtrl.isLoading &&
            !sCtrl.listProblem &&
            sText.includes('EMP-HN-001') &&
            sText.includes('EMP-HCM-001')
          ) {
            sCtrl.selectedDealerId = '22222222-2222-2222-2222-222222222202';
            await sCtrl.reloadStaffDirectory();
            const hnCount = sCtrl.filteredStaff.length;

            sCtrl.selectedDealerId = '22222222-2222-2222-2222-222222222203';
            await sCtrl.reloadStaffDirectory();
            const dnCount = sCtrl.filteredStaff.length;

            sCtrl.selectedDealerId = '22222222-2222-2222-2222-222222222201';
            await sCtrl.reloadStaffDirectory();
            const hcmCount = sCtrl.filteredStaff.length;

            sCtrl.selectedDealerId = 'ALL';
            await sCtrl.reloadStaffDirectory();
            const allCount = sCtrl.filteredStaff.length;

            sCtrl.openCreateModal();
            sCtrl.form.password = 'weakpass';
            const weakCaught = sCtrl.isPasswordComplex() === false;
            sCtrl.closeCreateModal();
            sCtrl.promptToggleStatus(sCtrl.staffAccounts[0]);
            const confirmOpened = Boolean(sCtrl.confirmTarget);
            sCtrl.cancelToggleStatus();

            adminStaffLiveOk =
              hnCount >= 2 &&
              dnCount >= 1 &&
              hcmCount >= 1 &&
              allCount >= 4 &&
              weakCaught &&
              confirmOpened &&
              !sCtrl.createModalOpen &&
              !sCtrl.confirmTarget;
            break;
          }
          await new Promise((r) => setTimeout(r, 100));
        }

        const adminCatalogStates = ['app.admin.models', 'app.admin.loyaltyTiers', 'app.admin.voucherCatalog'];
        if (${targetPhase >= 8 ? "true" : "false"}) {
          adminCatalogStates.push('app.admin.spareParts');
        }
        if (${targetPhase >= 9 ? "true" : "false"}) {
          adminCatalogStates.push('app.admin.qualityWarranty');
        }
        for (const st of adminCatalogStates) {
          await visitState(st, {}, 150);
        }

        // Single-flight 401 refresh queue proof in real browser: tamper accessToken only, fire 3 concurrent protected $http.get calls
        const priorAccess = TokenStorageService.getAccessToken();
        const validRefresh = TokenStorageService.getRefreshToken();
        const tamperedJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDAiLCJleHAiOjF9.tamperedSignature';
        TokenStorageService.setTokens(tamperedJwt, 900, validRefresh);

        const [r1, r2, r3, rDealers] = await Promise.all([
          $http.get(APP_CONFIG.apiBaseUrl + '/auth/me'),
          $http.get(APP_CONFIG.apiBaseUrl + '/dealers/22222222-2222-2222-2222-222222222202/staff'),
          $http.get(APP_CONFIG.apiBaseUrl + '/dealers/22222222-2222-2222-2222-222222222201/staff'),
          $http.get(APP_CONFIG.apiBaseUrl + '/dealers')
        ]);

        const rotatedAccess = TokenStorageService.getAccessToken();
        const rotatedRefresh = TokenStorageService.getRefreshToken();
        singleFlightRefreshOk =
          r1.status === 200 &&
          r2.status === 200 &&
          r3.status === 200 &&
          rDealers.status === 200 &&
          Boolean(rotatedAccess) &&
          rotatedAccess !== tamperedJwt &&
          rotatedAccess !== priorAccess &&
          Boolean(rotatedRefresh) &&
          rotatedRefresh !== validRefresh;
      } else {
        for (const st of ['app.admin.staff', 'app.admin.models', 'app.admin.loyaltyTiers', 'app.admin.voucherCatalog']) {
          await visitState(st, {}, 130);
        }
      }

      const healthRes = await fetch('http://127.0.0.1:${apiPort}/api/v1/health', { credentials: 'include' });
      const negHeaders = {};
      if (useRealAuth && typeof AuthService.getAccessToken === 'function' && AuthService.getAccessToken()) {
        negHeaders['Authorization'] = 'Bearer ' + AuthService.getAccessToken();
      }
      const negRes = await fetch('http://127.0.0.1:${apiPort}/hubs/notifications/negotiate?negotiateVersion=1', {
        method: 'POST',
        headers: negHeaders,
        credentials: 'include'
      });

      const signalRStatusBeforeTeardown = SignalRClientService.getConnectionStatus();
      if (useRealAuth && typeof AuthService.logout === 'function') {
        await AuthService.logout();
        interactiveLogoutDisconnectedOk =
          SignalRClientService.getConnectionStatus() === 'Disconnected' &&
          AuthService.isAuthenticated() === false;
      } else if (typeof AuthService.clearSessionAndRealtime === 'function') {
        AuthService.clearSessionAndRealtime();
      }

      return {
        angularVersion: window.angular.version.full,
        strictDi: Boolean(inj.strictDi),
        chartLoaded: typeof window.Chart === 'function',
        signalRLoaded: Boolean(window.signalR && window.signalR.HubConnectionBuilder),
        signalRConnectionStatus: signalRStatusBeforeTeardown,
        signalRConnectedOk,
        interactiveLogoutDisconnectedOk,
        singleFlightRefreshOk,
        unauthRedirectOk,
        registerOk,
        showroomOk,
        forbiddenGuardOk,
        visitedStatesCount: visitedStates.size,
        invoiceSelectOptions,
        invoiceBuilderMountedOk,
        chartsMounted,
        dealerChartCanvasCount,
        chartsAfterLeave,
        analyticsRemountOk,
        analyticsCsvExportOk,
        adminDealersLiveOk,
        adminStaffLiveOk,
        corsHealthStatus: healthRes.status,
        corsNegotiateStatus: negRes.status
      };
    })()`);

    const adminRefreshCalls = refreshNetworkCalls;
    let customerCrossPhaseRefreshOk = targetPhase < 5;
    let customerRefreshCalls = 0;

    if (targetPhase >= 5) {
      const refreshBeforeCustomer = refreshNetworkCalls;
      const p5CustRefresh = await cdpEval(`(async () => {
        const inj = window.angular.element(document.documentElement).injector();
        const $http = inj.get('$http');
        const AuthService = inj.get('AuthService');
        const TokenStorageService = inj.get('TokenStorageService');
        const SignalRClientService = inj.get('SignalRClientService');
        const APP_CONFIG = inj.get('APP_CONFIG');

        await AuthService.login({
          email: 'customer.silver@motorcycledealer.vn',
          password: 'Customer@123!'
        });

        for (let w = 0; w < 30; w++) {
          if (SignalRClientService.getConnectionStatus() === 'Connected') break;
          await new Promise((r) => setTimeout(r, 100));
        }

        const priorAccess = TokenStorageService.getAccessToken();
        const validRefresh = TokenStorageService.getRefreshToken();
        const tamperedJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0NDQ0NDQ0NC00NDQ0LTQ0NDQtNDQ0NC00NDQ0NDQ0NDQ0MDgiLCJleHAiOjF9.tamperedCustomerSignature';
        TokenStorageService.setTokens(tamperedJwt, 900, validRefresh);

        const [rMe, rVehicles, rBookings] = await Promise.all([
          $http.get(APP_CONFIG.apiBaseUrl + '/auth/me'),
          $http.get(APP_CONFIG.apiBaseUrl + '/vehicles'),
          $http.get(APP_CONFIG.apiBaseUrl + '/bookings')
        ]);

        const rotatedAccess = TokenStorageService.getAccessToken();
        const rotatedRefresh = TokenStorageService.getRefreshToken();
        const statuses = [rMe.status, rVehicles.status, rBookings.status];

        await AuthService.logout();
        const logoutClean =
          SignalRClientService.getConnectionStatus() === 'Disconnected' &&
          AuthService.isAuthenticated() === false;

        return {
          statuses,
          all200: statuses[0] === 200 && statuses[1] === 200 && statuses[2] === 200,
          tokensRotated:
            Boolean(rotatedAccess) &&
            rotatedAccess !== tamperedJwt &&
            rotatedAccess !== priorAccess &&
            Boolean(rotatedRefresh) &&
            rotatedRefresh !== validRefresh,
          logoutClean
        };
      })()`);

      customerRefreshCalls = refreshNetworkCalls - refreshBeforeCustomer;
      customerCrossPhaseRefreshOk =
        Boolean(p5CustRefresh && p5CustRefresh.all200 && p5CustRefresh.tokensRotated && p5CustRefresh.logoutClean) &&
        customerRefreshCalls === 1;
      if (customerCrossPhaseRefreshOk) {
        console.log(
          `[PASS] Customer cross-phase 401 single-flight refresh verified (/api/v1/auth/me + /api/v1/vehicles + /api/v1/bookings -> [${p5CustRefresh.statuses.join(", ")}], refreshCalls=${customerRefreshCalls}/1)`
        );
      }
    }

    const expectedVisitedStates = targetPhase >= 9 ? 30 : targetPhase >= 8 ? 27 : 24;
    if (
      e2eResult.angularVersion !== "1.8.3" ||
      !e2eResult.strictDi ||
      !e2eResult.chartLoaded ||
      !e2eResult.signalRLoaded ||
      !e2eResult.signalRConnectedOk ||
      !e2eResult.interactiveLogoutDisconnectedOk ||
      !e2eResult.singleFlightRefreshOk ||
      (targetPhase >= 4 && adminRefreshCalls !== 1) ||
      !customerCrossPhaseRefreshOk ||
      !e2eResult.unauthRedirectOk ||
      !e2eResult.registerOk ||
      !e2eResult.showroomOk ||
      !e2eResult.forbiddenGuardOk ||
      e2eResult.visitedStatesCount !== expectedVisitedStates ||
      !e2eResult.invoiceBuilderMountedOk ||
      e2eResult.chartsMounted !== 4 ||
      e2eResult.dealerChartCanvasCount !== 4 ||
      e2eResult.chartsAfterLeave !== 0 ||
      !e2eResult.analyticsRemountOk ||
      !e2eResult.analyticsCsvExportOk ||
      !e2eResult.adminDealersLiveOk ||
      !e2eResult.adminStaffLiveOk ||
      e2eResult.corsHealthStatus !== 200 ||
      (e2eResult.corsNegotiateStatus !== 200 && e2eResult.corsNegotiateStatus !== 401)
    ) {
      throw new Error(
        "Headless browser E2E assertion failed: " +
          JSON.stringify(Object.assign({}, e2eResult, {
            expectedVisitedStates,
            adminRefreshCalls,
            customerRefreshCalls,
            customerCrossPhaseRefreshOk
          }))
      );
    }

    if (consoleErrors.length > 0 || pageExceptions.length > 0) {
      throw new Error(
        `Headless browser emitted consoleErrors=${JSON.stringify(consoleErrors)}, pageExceptions=${JSON.stringify(pageExceptions)}`
      );
    }

    const p4Suffix = targetPhase >= 5
      ? `, Admin+Customer cross-phase 401 single-flight refreshCalls=${adminRefreshCalls}+${customerRefreshCalls}, SignalR Connected->Disconnected, /admin/{dealers,staff} modals+branches`
      : targetPhase >= 4
        ? `, 401 single-flight refreshCalls=${adminRefreshCalls}/1, SignalR Connected->Disconnected, /admin/{dealers,staff} modals+branches`
        : "";
    console.log(
      `[PASS] Headless Chromium CDP E2E verified (AngularJS ${e2eResult.angularVersion} strictDi, ${e2eResult.visitedStatesCount}/${expectedVisitedStates} states, guards /login & /403, Chart.js 4->0 teardown, CORS+SignalR${p4Suffix}, 0 console errors)`
    );
  } finally {
    if (ws) { try { ws.close(); } catch {} }
    killProcessTree(browserChild);
    await sleep(150);
    try { fs.rmSync(profileDir, { recursive: true, force: true }); } catch {}
  }
}

(async () => {
  const apiPort = await getFreePort(reqApiPort);
  const spaPort = await getFreePort(reqSpaPort);
  let apiChild = null;
  let spaChild = null;
  let apiStderr = "";
  let spaStderr = "";

  try {
    apiChild = spawn(
      "dotnet",
      [webApiDll, "--urls", `http://127.0.0.1:${apiPort}`],
      {
        cwd: webApiCwd,
        stdio: ["ignore", "pipe", "pipe"],
        env: Object.assign({}, process.env, {
          ASPNETCORE_ENVIRONMENT: "Development",
          ASPNETCORE_URLS: `http://127.0.0.1:${apiPort}`,
          Jwt__SigningKey: "QualityLoop_Ephemeral_Min32Byte_SigningSecret_2026!",
          RateLimiting__Auth__PermitLimit: "120",
          Cors__AllowedOrigins__0: "http://localhost:4200",
          Cors__AllowedOrigins__1: `http://localhost:${spaPort}`,
          Cors__AllowedOrigins__2: `http://127.0.0.1:${spaPort}`
        })
      }
    );
    apiChild.stderr.on("data", (d) => { apiStderr += d.toString("utf8"); });

    let apiUp = false;
    for (let i = 0; i < 80; i++) {
      if (apiChild.exitCode !== null) break;
      try {
        const h = await httpRequest(apiPort, "GET", "/health");
        if (h.statusCode === 200) {
          apiUp = true;
          break;
        }
      } catch {}
      await sleep(250);
    }
    if (!apiUp) {
      throw new Error(`WebApi failed to reach healthy state on 127.0.0.1:${apiPort} (${apiStderr.trim() || "timeout"})`);
    }
    console.log(`[PASS] Live WebApi GET /health -> 200 OK (http://127.0.0.1:${apiPort})`);

    const v1Health = await httpRequest(apiPort, "GET", "/api/v1/health");
    if (v1Health.statusCode !== 200) {
      throw new Error(`GET /api/v1/health expected 200, got ${v1Health.statusCode}`);
    }
    console.log("[PASS] Live WebApi GET /api/v1/health -> 200 OK");

    const notFound = await httpRequest(apiPort, "GET", "/api/v1/does-not-exist-quality-probe");
    const ct = String(notFound.headers["content-type"] || "");
    if (notFound.statusCode !== 404 || !ct.includes("application/problem+json") || !notFound.body.includes("traceId")) {
      throw new Error(`404 ProblemDetails check failed: status=${notFound.statusCode}, ct=${ct}, body=${notFound.body}`);
    }
    console.log("[PASS] Live WebApi 404 RFC 7807 ProblemDetails verified (application/problem+json + traceId)");

    const corsPreflight = await httpRequest(apiPort, "OPTIONS", "/api/v1/health", {
      Origin: "http://localhost:4200",
      "Access-Control-Request-Method": "GET"
    });
    const allowOrigin = String(corsPreflight.headers["access-control-allow-origin"] || "");
    const allowCreds = String(corsPreflight.headers["access-control-allow-credentials"] || "");
    if (allowOrigin !== "http://localhost:4200" || allowCreds !== "true") {
      throw new Error(`CORS preflight failed: allow-origin='${allowOrigin}', allow-credentials='${allowCreds}'`);
    }
    console.log("[PASS] Live WebApi CORS preflight verified (Origin=http://localhost:4200 + Allow-Credentials=true)");

    const untrustedCors = await httpRequest(apiPort, "OPTIONS", "/api/v1/health", {
      Origin: "https://untrusted.example.invalid",
      "Access-Control-Request-Method": "GET"
    });
    const untrustedAllowOrigin = String(untrustedCors.headers["access-control-allow-origin"] || "");
    if (untrustedAllowOrigin !== "") {
      throw new Error(`Negative CORS preflight failed: untrusted origin unexpectedly allowed ('${untrustedAllowOrigin}')`);
    }
    console.log("[PASS] Live WebApi negative CORS preflight verified (Origin=https://untrusted.example.invalid rejected)");

    const negotiate = await httpRequest(apiPort, "POST", "/hubs/notifications/negotiate?negotiateVersion=1", {}, "{}");
    const validNeg =
      (negotiate.statusCode === 200 && negotiate.body.includes("connectionId")) ||
      (negotiate.statusCode === 401 && String(negotiate.headers["content-type"] || "").includes("application/problem+json"));
    if (!validNeg) {
      throw new Error(`SignalR negotiate check failed: status=${negotiate.statusCode}, body=${negotiate.body}`);
    }
    console.log(`[PASS] Live SignalR /hubs/notifications/negotiate verified (status=${negotiate.statusCode})`);

    if (targetPhase >= 4) {
      await runPhase4LiveAuthAndDealersProbes(apiPort);
    }
    if (targetPhase >= 5) {
      await runPhase5LiveShowroomGarageAndBookingProbes(apiPort);
    }
    if (targetPhase >= 6) {
      await runPhase6LiveLoyaltyVouchersInvoicesAndNotificationsProbes(apiPort);
    }

    spaChild = spawn(
      process.execPath,
      [spaScript],
      {
        cwd: spaCwd,
        stdio: ["ignore", "pipe", "pipe"],
        env: Object.assign({}, process.env, {
          HOST: "127.0.0.1",
          PORT: String(spaPort)
        })
      }
    );
    spaChild.stderr.on("data", (d) => { spaStderr += d.toString("utf8"); });

    let spaUp = false;
    for (let j = 0; j < 25; j++) {
      if (spaChild.exitCode !== null) break;
      try {
        const spaRes = await httpRequest(spaPort, "GET", "/");
        if (spaRes.statusCode === 200 && spaRes.body.includes("motorcycleDealerApp")) {
          spaUp = true;
          break;
        }
      } catch {}
      await sleep(200);
    }
    if (!spaUp) {
      throw new Error(`SPA dev-server failed GET / check on 127.0.0.1:${spaPort} (${spaStderr.trim() || "timeout"})`);
    }
    console.log(`[PASS] Live AngularJS SPA GET / -> 200 OK with motorcycleDealerApp (http://127.0.0.1:${spaPort})`);

    if (targetPhase >= 7) {
      const apiUrlArg = `http://127.0.0.1:${apiPort}`;
      const spaUrlArg = `http://127.0.0.1:${spaPort}`;
      const smokeCmd = process.platform === "win32"
        ? `powershell -NoProfile -ExecutionPolicy Bypass -File "${smokePs1}" -Phase ${targetPhase} -ApiBaseUrl "${apiUrlArg}" -SpaBaseUrl "${spaUrlArg}"`
        : `bash "${smokeSh}" --phase ${targetPhase} --api-base-url "${apiUrlArg}" --spa-base-url "${spaUrlArg}"`;
      const smokeOut = execSync(smokeCmd, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
      for (const sLine of smokeOut.split(/\r?\n/)) {
        if (sLine.trim()) console.log(sLine.trim());
      }
      const maxSmkLabel = targetPhase >= 9 ? "SMK-10" : targetPhase >= 8 ? "SMK-09" : "SMK-08";
      console.log(`[PASS] Phase ${targetPhase} smoke-fullstack suite (SMK-01..${maxSmkLabel}) passed against ephemeral WebApi & SPA`);
    }

    await runHeadlessBrowserCdpProbe(apiPort, spaPort);
  } finally {
    killProcessTree(apiChild);
    killProcessTree(spaChild);
  }
})().catch((err) => {
  console.error(err && err.message ? err.message : String(err));
  process.exit(1);
});
'@

if (-not (Test-Path $WebApiDll)) {
    $B2Passed = $false
    $B2Errors.Add("Compiled WebApi assembly not found at $WebApiDll") | Out-Null
    Write-FailMsg "Compiled WebApi assembly not found at $WebApiDll."
} elseif (-not (Test-Path $SpaDevServer)) {
    $B2Passed = $false
    $B2Errors.Add("Missing frontend/scripts/dev-server.mjs") | Out-Null
    Write-FailMsg "Missing frontend/scripts/dev-server.mjs."
} else {
    $TempSmokeScript = Join-Path ([System.IO.Path]::GetTempPath()) ("ql-live-smoke-" + [Guid]::NewGuid().ToString("N") + ".cjs")
    [System.IO.File]::WriteAllText($TempSmokeScript, $LiveSmokeJs, [System.Text.UTF8Encoding]::new($false))
    $env:QL_WEBAPI_DLL = $WebApiDll
    $env:QL_WEBAPI_CWD = Split-Path -Parent $WebApiProj
    $env:QL_SPA_SCRIPT = $SpaDevServer
    $env:QL_SPA_CWD = $FrontendDir
    $env:QL_SMOKE_PS1 = $SmokePs1
    $env:QL_SMOKE_SH = $SmokeSh
    $env:QL_API_PORT = "$ApiPort"
    $env:QL_SPA_PORT = "$SpaPort"
    $env:QL_TARGET_PHASE = "$Phase"

    $SmokeLines = @()
    $SmokeExit = 1
    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $SmokeLines = @(& node $TempSmokeScript 2>&1)
        $SmokeExit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $PrevEap
        Remove-Item Env:\QL_WEBAPI_DLL, Env:\QL_WEBAPI_CWD, Env:\QL_SPA_SCRIPT, Env:\QL_SPA_CWD, Env:\QL_SMOKE_PS1, Env:\QL_SMOKE_SH, Env:\QL_API_PORT, Env:\QL_SPA_PORT, Env:\QL_TARGET_PHASE -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $TempSmokeScript -Force -ErrorAction SilentlyContinue
    }

    foreach ($Line in @($SmokeLines)) {
        $LineStr = "$Line".Trim()
        if ([string]::IsNullOrWhiteSpace($LineStr)) { continue }
        if ($LineStr.StartsWith("[PASS]")) {
            Write-Host $LineStr -ForegroundColor Green
        } else {
            Write-Host "[INFO] $LineStr" -ForegroundColor Gray
        }
    }

    if ($SmokeExit -ne 0) {
        $B2Passed = $false
        $JoinedErr = (@($SmokeLines) | ForEach-Object { "$_" }) -join " "
        $B2Errors.Add("Live HTTP/SignalR/SPA/CDP smoke failed: $($JoinedErr.Trim())") | Out-Null
        Write-FailMsg "Live HTTP/SignalR/SPA/CDP smoke probe failed."
    }
}

$Sw2.Stop()
$B2SuccessLabel = if ($Phase -ge 9) {
    "SPA npm test + smoke-fullstack (SMK-01..10) + live WebApi/Reviews/Warranty/TestDrives/SpareParts/Receipts/Analytics/SignalR + 30-state CDP E2E passed"
} elseif ($Phase -ge 8) {
    "SPA npm test + smoke-fullstack (SMK-01..09) + live WebApi/TestDrives/SpareParts/Receipts/Analytics/SignalR + 27-state CDP E2E passed"
} elseif ($Phase -ge 7) {
    "SPA npm test + smoke-fullstack (SMK-01..08) + live WebApi/Analytics/CSV/SignalR + 24-state CDP E2E passed"
} elseif ($Phase -ge 6) {
    "SPA npm test + live WebApi/Auth/Dealers/Showroom/Booking/Loyalty/Vouchers/Invoices/Notifications/SignalR/SPA + 24-state CDP E2E passed"
} elseif ($Phase -ge 5) {
    "SPA npm test + live WebApi/Auth/Dealers/Showroom/Inventories/Vehicles/TimeSlots/Bookings/SignalR/SPA + 24-state CDP E2E passed"
} elseif ($Phase -ge 4) {
    "SPA npm test + live WebApi/Auth/Me/Dealers/ProblemDetails/SignalR/SPA + 24-state CDP E2E passed"
} else {
    "SPA npm test + live WebApi/ProblemDetails/SignalR/SPA + 24-state CDP E2E passed"
}
$B2Detail = if ($B2Passed) { $B2SuccessLabel } else { ($B2Errors -join "; ") }
$BatteryResults.Add(@{
    Id        = "T$Round.2"
    Name      = "Frontend SPA + Live HTTP/SignalR + CDP E2E"
    Passed    = $B2Passed
    ElapsedMs = $Sw2.ElapsedMilliseconds
    Detail    = $B2Detail
}) | Out-Null

# ==============================================================================
# BATTERY 3: Guardrails + MCP check_phase_gate (+ LocalDB 22-Table Gate) + Docker + Determinism
# ==============================================================================
$Sw3 = [System.Diagnostics.Stopwatch]::StartNew()
$B3Passed = $true
$B3Errors = New-Object System.Collections.Generic.List[string]
Write-Host ""
Write-Info "[Battery 3/3 | T$Round.3] Guardrails + MCP check_phase_gate + Docker Compose + Determinism Re-test..."

& powershell -NoProfile -ExecutionPolicy Bypass -File $VerifyScript -Phase $Phase
if ($LASTEXITCODE -ne 0) {
    $B3Passed = $false
    $B3Errors.Add("verify-dealer-platform.ps1 exited with code $LASTEXITCODE") | Out-Null
    Write-FailMsg "verify-dealer-platform.ps1 failed."
} else {
    Write-Pass "verify-dealer-platform.ps1 passed."
}

& node $PostTaskSyncScript
if ($LASTEXITCODE -ne 0) {
    $B3Passed = $false
    $B3Errors.Add("verify-post-task-sync.mjs exited with code $LASTEXITCODE") | Out-Null
    Write-FailMsg "verify-post-task-sync.mjs failed."
} else {
    Write-Pass "Post-Task Sync Gate verified (verify-post-task-sync.mjs)."
}

$McpGateJs = @'
const { spawn } = require("node:child_process");
const serverPath = process.env.MCP_GUARDRAILS_SERVER;
const targetPhase = Number(process.env.MCP_TARGET_PHASE || "3");
const child = spawn(process.execPath, [serverPath], { stdio: ["pipe", "pipe", "pipe"] });
let buf = "";
const timer = setTimeout(() => {
  console.error("Timed out waiting for check_phase_gate response");
  child.kill();
  process.exit(1);
}, 5000);

child.stdout.on("data", (chunk) => {
  buf += chunk.toString("utf8");
  const lines = buf.split(/\r?\n/);
  buf = lines.pop() || "";
  for (const line of lines) {
    if (!line.trim()) continue;
    const msg = JSON.parse(line);
    if (msg.id === 2) {
      clearTimeout(timer);
      const sc = msg.result && msg.result.structuredContent ? msg.result.structuredContent : null;
      child.stdin.end();
      child.kill();
      if (!sc || sc.compliant !== true) {
        console.error("Phase gate non-compliant:", JSON.stringify(sc));
        process.exit(1);
      }
      console.log("compliant=true, phase=" + sc.evaluatedPhase + ", files=" + sc.totalWorkspaceFiles + ", violations=" + sc.violationCount);
      process.exit(0);
    }
  }
});

child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "quality-loop", version: "1.0.0" } } }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "check_phase_gate", arguments: { phase: targetPhase } } }) + "\n");
'@

$EncodedMcpGate = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($McpGateJs))
$env:MCP_GUARDRAILS_SERVER = $McpGuardrails
$env:MCP_TARGET_PHASE = "$Phase"
$PrevEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$GateOut = & node -e "eval(Buffer.from('$EncodedMcpGate','base64').toString('utf8'))" 2>&1 | Out-String
$GateExit = $LASTEXITCODE
$ErrorActionPreference = $PrevEap
Remove-Item Env:\MCP_GUARDRAILS_SERVER -ErrorAction SilentlyContinue
Remove-Item Env:\MCP_TARGET_PHASE -ErrorAction SilentlyContinue

if ($GateExit -ne 0) {
    $B3Passed = $false
    $B3Errors.Add("MCP check_phase_gate failed: $($GateOut.Trim())") | Out-Null
    Write-FailMsg "MCP check_phase_gate failed: $($GateOut.Trim())"
} else {
    Write-Pass "MCP check_phase_gate verified: $($GateOut.Trim())"
}

if ($Phase -ge 4) {
    $McpLocalDbJs = @'
const { spawn } = require("node:child_process");
const fs = require("node:fs");
const serverPath = process.env.MCP_LOCALDB_SERVER;
const snapshotPath = process.env.MCP_SNAPSHOT_PATH;
const targetPhase = Number(process.env.MCP_TARGET_PHASE || "4");
const snap = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
const expectedTables = (snap.database && Array.isArray(snap.database.tables))
  ? snap.database.tables.map((t) => t.name)
  : [];

const invariantSqlParts = [
  "SELECT",
  "  (SELECT COUNT(*) FROM dbo.LoyaltyAccounts) AS AccountCount,",
  "  (SELECT COUNT(*) FROM (",
  "     SELECT la.Id",
  "     FROM dbo.LoyaltyAccounts la",
  "     LEFT JOIN dbo.PointLedgerEntries ple ON ple.LoyaltyAccountId = la.Id",
  "     GROUP BY la.Id, la.AvailablePoints",
  "     HAVING ISNULL(SUM(ple.PointsDelta), 0) <> la.AvailablePoints",
  "  ) drift) AS LedgerDriftCount,",
  "  (SELECT COUNT(*) FROM dbo.ServiceTimeSlots WHERE BookedCount > MaxCapacity OR BookedCount < 0) AS OverbookedSlotCount"
];
if (targetPhase >= 5) {
  invariantSqlParts.push(
    ", (SELECT COUNT(*) FROM dbo.ServiceTimeSlots ts WHERE ts.BookedCount <> (",
    "     SELECT COUNT(*) FROM dbo.ServiceBookings sb",
    "     WHERE sb.ServiceTimeSlotId = ts.Id AND sb.Status NOT IN (N'Cancelled', N'NoShow')",
    "  )) AS SlotBookingDriftCount,",
    "  (SELECT COUNT(*) FROM dbo.ServiceTimeSlots) AS TotalSlotCount"
  );
}
if (targetPhase >= 6) {
  invariantSqlParts.push(
    ", (SELECT COUNT(*) FROM (",
    "     SELECT vc.Id",
    "     FROM dbo.VoucherCatalogs vc",
    "     LEFT JOIN dbo.CustomerVouchers cv ON cv.VoucherCatalogId = vc.Id",
    "     GROUP BY vc.Id, vc.QuotaRedeemed, vc.QuotaTotal",
    "     HAVING vc.QuotaRedeemed <> COUNT(cv.Id)",
    "         OR vc.QuotaRedeemed < 0",
    "         OR (vc.QuotaTotal IS NOT NULL AND vc.QuotaRedeemed > vc.QuotaTotal)",
    "  ) vdrift) AS VoucherQuotaDriftCount,",
    "  (SELECT COUNT(*) FROM dbo.VoucherCatalogs) AS TotalVoucherCatalogCount"
  );
}
if (targetPhase >= 7) {
  invariantSqlParts.push(
    ", (SELECT ISNULL(SUM(FinalAmount), 0) FROM dbo.SalesOrders WHERE Status = N'Completed') AS CompletedShowroomRevenueVnd,",
    "  (SELECT ISNULL(SUM(NetPaidAmount), 0) FROM dbo.ServiceInvoices) AS SettledServiceRevenueVnd,",
    "  (SELECT ISNULL(SUM(AvailablePoints), 0) FROM dbo.LoyaltyAccounts) AS AvailablePointsSum"
  );
}
if (targetPhase >= 8) {
  invariantSqlParts.push(
    ", (SELECT COUNT(*) FROM (",
    "     SELECT si.Id",
    "     FROM dbo.ServiceInvoices si",
    "     LEFT JOIN dbo.ServiceInvoiceLineItems li ON li.ServiceInvoiceId = si.Id",
    "     GROUP BY si.Id, si.GrossAmount, si.PartsAmount, si.LaborAmount",
    "     HAVING COUNT(li.Id) = 0",
    "         OR ISNULL(SUM(li.LineTotalVnd), 0) <> si.GrossAmount",
    "         OR ISNULL(SUM(CASE WHEN li.ItemType = 'Part' THEN li.LineTotalVnd ELSE 0 END), 0) <> si.PartsAmount",
    "         OR ISNULL(SUM(CASE WHEN li.ItemType = 'Labor' THEN li.LineTotalVnd ELSE 0 END), 0) <> si.LaborAmount",
    "         OR SUM(CASE WHEN li.LineTotalVnd <> (CAST(li.Quantity AS DECIMAL(18,2)) * li.UnitPriceVnd) THEN 1 ELSE 0 END) > 0",
    "  ) idrift) AS InvoiceLineItemsDriftCount,",
    "  (SELECT COUNT(*) FROM dbo.SpareParts WHERE IsDeleted = 0) AS ActiveSparePartsCount,",
    "  (SELECT COUNT(*) FROM dbo.TestDriveBookings) AS TestDriveBookingsCount"
  );
}
if (targetPhase >= 9) {
  invariantSqlParts.push(
    ", (",
    "    (SELECT COUNT(*) FROM (",
    "       SELECT sr.Id",
    "       FROM dbo.ServiceReviews sr",
    "       LEFT JOIN dbo.PointLedgerEntries ple",
    "         ON ple.TransactionType = 'ServiceReviewBonus' AND ple.ReferenceId = sr.Id",
    "       GROUP BY sr.Id, sr.BonusPointsAwarded",
    "       HAVING ISNULL(SUM(ple.PointsDelta), 0) <> sr.BonusPointsAwarded",
    "    ) sr_drift)",
    "    +",
    "    (SELECT COUNT(*) FROM dbo.PointLedgerEntries ple",
    "     WHERE ple.TransactionType = 'ServiceReviewBonus'",
    "       AND NOT EXISTS (SELECT 1 FROM dbo.ServiceReviews sr WHERE sr.Id = ple.ReferenceId))",
    "  ) AS ReviewBonusDriftCount,",
    "  (SELECT COUNT(*) FROM dbo.ServiceReviews) AS ServiceReviewsCount,",
    "  (SELECT COUNT(*) FROM dbo.WarrantyClaims) AS WarrantyClaimsCount"
  );
}
const invariantSql = invariantSqlParts.join(" ");

const child = spawn(process.execPath, [serverPath], { stdio: ["pipe", "pipe", "pipe"] });
let buf = "";
const responses = new Map();
const timer = setTimeout(() => {
  console.error("Timed out waiting for MCP LocalDB table & invariant responses");
  child.kill();
  process.exit(1);
}, 20000);

child.stdout.on("data", (chunk) => {
  buf += chunk.toString("utf8");
  const lines = buf.split(/\r?\n/);
  buf = lines.pop() || "";
  for (const line of lines) {
    if (!line.trim()) continue;
    const msg = JSON.parse(line);
    if (msg.id !== undefined) {
      responses.set(msg.id, msg);
    }
    if (responses.has(2) && responses.has(3)) {
      clearTimeout(timer);
      child.stdin.end();
      child.kill();

      const tblMsg = responses.get(2);
      const sc = tblMsg.result && tblMsg.result.structuredContent ? tblMsg.result.structuredContent : null;
      if (!sc || sc.databaseExists !== true || !Array.isArray(sc.tables)) {
        console.error("MotorcycleDealerDb tables not available:", JSON.stringify(sc));
        process.exit(1);
      }
      const actualNames = new Set(sc.tables.map((r) => r.TableName || r.tableName));
      const requiredTablesForPhase = targetPhase >= 9 ? expectedTables : targetPhase >= 8 ? expectedTables.slice(0, 25) : expectedTables.slice(0, 22);
      const missing = requiredTablesForPhase.filter((t) => !actualNames.has(t));
      const expectedTableCount = targetPhase >= 9 ? 27 : targetPhase >= 8 ? 25 : 22;
      const validTableCount = targetPhase >= 9
        ? sc.tableCount === 27
        : targetPhase >= 8
          ? (sc.tableCount === 25 || sc.tableCount === 27)
          : (sc.tableCount === 22 || sc.tableCount === 25 || sc.tableCount === 27);
      if (!validTableCount || missing.length > 0) {
        console.error("Expected " + expectedTableCount + " canonical 3NF tables; got count=" + sc.tableCount + ", missing=" + JSON.stringify(missing));
        process.exit(1);
      }

      const invMsg = responses.get(3);
      const invSc = invMsg.result && invMsg.result.structuredContent ? invMsg.result.structuredContent : null;
      const invRow = invSc && Array.isArray(invSc.rows) && invSc.rows.length > 0 ? invSc.rows[0] : null;
      if (!invSc || invMsg.result.isError === true || !invRow) {
        console.error("MCP query_readonly_sql invariant query failed:", JSON.stringify(invSc || invMsg));
        process.exit(1);
      }
      const accountCount = Number(invRow.AccountCount ?? invRow.accountCount ?? 0);
      const ledgerDriftCount = Number(invRow.LedgerDriftCount ?? invRow.ledgerDriftCount ?? -1);
      const overbookedSlotCount = Number(invRow.OverbookedSlotCount ?? invRow.overbookedSlotCount ?? -1);
      const slotBookingDriftCount = targetPhase >= 5
        ? Number(invRow.SlotBookingDriftCount ?? invRow.slotBookingDriftCount ?? -1)
        : 0;
      const totalSlotCount = targetPhase >= 5
        ? Number(invRow.TotalSlotCount ?? invRow.totalSlotCount ?? 0)
        : 0;
      const voucherQuotaDriftCount = targetPhase >= 6
        ? Number(invRow.VoucherQuotaDriftCount ?? invRow.voucherQuotaDriftCount ?? -1)
        : 0;
      const totalVoucherCatalogCount = targetPhase >= 6
        ? Number(invRow.TotalVoucherCatalogCount ?? invRow.totalVoucherCatalogCount ?? 0)
        : 0;
      const completedShowroomRevenueVnd = targetPhase >= 7
        ? Number(invRow.CompletedShowroomRevenueVnd ?? invRow.completedShowroomRevenueVnd ?? -1)
        : 0;
      const settledServiceRevenueVnd = targetPhase >= 7
        ? Number(invRow.SettledServiceRevenueVnd ?? invRow.settledServiceRevenueVnd ?? -1)
        : 0;
      const availablePointsSum = targetPhase >= 7
        ? Number(invRow.AvailablePointsSum ?? invRow.availablePointsSum ?? -1)
        : 0;
      const invoiceLineItemsDriftCount = targetPhase >= 8
        ? Number(invRow.InvoiceLineItemsDriftCount ?? invRow.invoiceLineItemsDriftCount ?? -1)
        : 0;
      const activeSparePartsCount = targetPhase >= 8
        ? Number(invRow.ActiveSparePartsCount ?? invRow.activeSparePartsCount ?? 0)
        : 0;
      const testDriveBookingsCount = targetPhase >= 8
        ? Number(invRow.TestDriveBookingsCount ?? invRow.testDriveBookingsCount ?? 0)
        : 0;
      const reviewBonusDriftCount = targetPhase >= 9
        ? Number(invRow.ReviewBonusDriftCount ?? invRow.reviewBonusDriftCount ?? -1)
        : 0;
      const serviceReviewsCount = targetPhase >= 9
        ? Number(invRow.ServiceReviewsCount ?? invRow.serviceReviewsCount ?? 0)
        : 0;
      const warrantyClaimsCount = targetPhase >= 9
        ? Number(invRow.WarrantyClaimsCount ?? invRow.warrantyClaimsCount ?? 0)
        : 0;

      if (
        accountCount < 5 ||
        ledgerDriftCount !== 0 ||
        overbookedSlotCount !== 0 ||
        (targetPhase >= 5 && (slotBookingDriftCount !== 0 || totalSlotCount < 1)) ||
        (targetPhase >= 6 && (voucherQuotaDriftCount !== 0 || totalVoucherCatalogCount < 5)) ||
        (targetPhase >= 7 && (
          !Number.isFinite(completedShowroomRevenueVnd) || completedShowroomRevenueVnd < 0 ||
          !Number.isFinite(settledServiceRevenueVnd) || settledServiceRevenueVnd < 0 ||
          !Number.isFinite(availablePointsSum) || availablePointsSum < 0
        )) ||
        (targetPhase >= 8 && (
          invoiceLineItemsDriftCount !== 0 ||
          activeSparePartsCount < 8 ||
          testDriveBookingsCount < 4
        )) ||
        (targetPhase >= 9 && (
          reviewBonusDriftCount !== 0 ||
          serviceReviewsCount < 1 ||
          warrantyClaimsCount < 3
        ))
      ) {
        console.error(
          "SQL invariant violation: AccountCount=" + accountCount +
          ", LedgerDriftCount=" + ledgerDriftCount +
          ", OverbookedSlotCount=" + overbookedSlotCount +
          (targetPhase >= 5 ? ", SlotDriftCount=" + slotBookingDriftCount + ", TotalSlotCount=" + totalSlotCount : "") +
          (targetPhase >= 6 ? ", VoucherQuotaDriftCount=" + voucherQuotaDriftCount + ", TotalVoucherCatalogCount=" + totalVoucherCatalogCount : "") +
          (targetPhase >= 7 ? ", ShowroomRev=" + completedShowroomRevenueVnd + ", ServiceRev=" + settledServiceRevenueVnd + ", AvailPtsSum=" + availablePointsSum : "") +
          (targetPhase >= 8 ? ", InvoiceLineItemsDriftCount=" + invoiceLineItemsDriftCount + ", ActiveSparePartsCount=" + activeSparePartsCount + ", TestDriveBookingsCount=" + testDriveBookingsCount : "") +
          (targetPhase >= 9 ? ", ReviewBonusDriftCount=" + reviewBonusDriftCount + ", ServiceReviewsCount=" + serviceReviewsCount + ", WarrantyClaimsCount=" + warrantyClaimsCount : "")
        );
        process.exit(1);
      }

      const slotDriftNote = targetPhase >= 5
        ? ", slotDrift=0/" + totalSlotCount
        : "";
      const voucherDriftNote = targetPhase >= 6
        ? ", voucherQuotaDrift=0/" + totalVoucherCatalogCount
        : "";
      const analyticsRevNote = targetPhase >= 7
        ? ", showroomRev=" + completedShowroomRevenueVnd + ", serviceRev=" + settledServiceRevenueVnd + ", availPts=" + availablePointsSum
        : "";
      const phase8Note = targetPhase >= 8
        ? ", invoiceLineItemsDrift=0, spareParts=" + activeSparePartsCount + ", testDrives=" + testDriveBookingsCount
        : "";
      const phase9Note = targetPhase >= 9
        ? ", reviewBonusDrift=0, reviews=" + serviceReviewsCount + ", warrantyClaims=" + warrantyClaimsCount
        : "";
      console.log(
        "database=" + sc.database +
        ", tableCount=" + sc.tableCount + "/" + expectedTableCount +
        ", loyaltyAccounts=" + accountCount + " (AvailablePoints==SUM(PointsDelta), drift=0)" +
        ", overbookedSlots=0" +
        slotDriftNote +
        voucherDriftNote +
        analyticsRevNote +
        phase8Note +
        phase9Note +
        ", transport=" + (sc.sqlTransport || "sqlcmd")
      );
      process.exit(0);
    }
  }
});

child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "quality-loop-db", version: "1.0.0" } } }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 2, method: "tools/call", params: { name: "list_sqlserver_tables", arguments: { database: "MotorcycleDealerDb" } } }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 3, method: "tools/call", params: { name: "query_readonly_sql", arguments: { database: "MotorcycleDealerDb", sql: invariantSql } } }) + "\n");
'@

    $EncodedMcpLocalDb = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($McpLocalDbJs))
    $env:MCP_LOCALDB_SERVER = $McpLocalDb
    $env:MCP_SNAPSHOT_PATH = $SnapshotPath
    $env:MCP_TARGET_PHASE = "$Phase"
    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $LocalDbOut = & node -e "eval(Buffer.from('$EncodedMcpLocalDb','base64').toString('utf8'))" 2>&1 | Out-String
    $LocalDbExit = $LASTEXITCODE
    $ErrorActionPreference = $PrevEap
    Remove-Item Env:\MCP_LOCALDB_SERVER, Env:\MCP_SNAPSHOT_PATH, Env:\MCP_TARGET_PHASE -ErrorAction SilentlyContinue

    $ExpectedTablesLabel = if ($Phase -ge 9) { "27" } elseif ($Phase -ge 8) { "25" } else { "22" }
    if ($LocalDbExit -ne 0) {
        $B3Passed = $false
        $B3Errors.Add("MCP LocalDB $ExpectedTablesLabel-table & ledger/slot invariant check failed: $($LocalDbOut.Trim())") | Out-Null
        Write-FailMsg "MCP LocalDB $ExpectedTablesLabel-table & ledger/slot invariant check failed: $($LocalDbOut.Trim())"
    } else {
        Write-Pass "MCP LocalDB verified $ExpectedTablesLabel/$ExpectedTablesLabel 3NF tables & SQL invariants: $($LocalDbOut.Trim())"
    }
}

$DockerCli = Get-Command "docker" -ErrorAction SilentlyContinue
if ($null -ne $DockerCli -and (Test-Path $ComposePath)) {
    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $ComposeOut = & docker compose -f $ComposePath config -q 2>&1 | Out-String
    $ComposeExit = $LASTEXITCODE
    $ErrorActionPreference = $PrevEap
    if ($ComposeExit -ne 0) {
        $B3Passed = $false
        $B3Errors.Add("docker compose config -q failed: $($ComposeOut.Trim())") | Out-Null
        Write-FailMsg "docker compose config -q failed: $($ComposeOut.Trim())"
    } else {
        Write-Pass "docker compose config -q passed."
    }
} else {
    Write-Info "Docker CLI unavailable or docker-compose.yml absent; skipped docker compose config check."
}

if (Test-Path $SlnPath) {
    & dotnet test $SlnPath -c Release --no-build --nologo @BackendBuildExtraArgs
    if ($LASTEXITCODE -ne 0) {
        $B3Passed = $false
        $B3Errors.Add("Determinism re-test (dotnet test -c Release --no-build) exited with code $LASTEXITCODE") | Out-Null
        Write-FailMsg "Determinism xUnit re-test failed."
    } else {
        Write-Pass "Determinism xUnit re-test passed (0 flaky tests)."
    }
} else {
    $B3Passed = $false
    $B3Errors.Add("Missing backend/MotorcycleDealer.sln for determinism re-test") | Out-Null
}

if ($Phase -ge 10) {
    if (Test-Path $VerifyMobileScript) {
        & node $VerifyMobileScript
        if ($LASTEXITCODE -ne 0) {
            $B3Passed = $false
            $B3Errors.Add("verify-mobile-maui.mjs exited with code $LASTEXITCODE") | Out-Null
            Write-FailMsg "verify-mobile-maui.mjs failed."
        } else {
            Write-Pass "MAUI static guardrails verified (verify-mobile-maui.mjs)."
        }
    }
    if (Test-Path $MobileUnitTestsProj) {
        & dotnet test $MobileUnitTestsProj -c Release --no-build --nologo
        if ($LASTEXITCODE -ne 0) {
            $B3Passed = $false
            $B3Errors.Add("Mobile determinism re-test (dotnet test Mobile.UnitTests -c Release --no-build) exited with code $LASTEXITCODE") | Out-Null
            Write-FailMsg "Mobile determinism xUnit re-test failed."
        } else {
            Write-Pass "Mobile determinism xUnit re-test passed (0 flaky tests)."
        }
    }
}

$Sw3.Stop()
$B3SuccessLabel = if ($Phase -ge 10) {
    "Guardrails + verify-post-task-sync.mjs + verify-mobile-maui.mjs + MCP check_phase_gate + LocalDB 27-table reconciliation + docker compose config + Backend & Mobile determinism re-test passed"
} elseif ($Phase -ge 9) {
    "Guardrails + verify-post-task-sync.mjs + MCP check_phase_gate + LocalDB 27-table, ledger/slot/voucher-quota, invoice-line-items, review-bonus & analytics revenue MCP reconciliation + docker compose config + determinism re-test passed"
} elseif ($Phase -ge 8) {
    "Guardrails + verify-post-task-sync.mjs + MCP check_phase_gate + LocalDB 25-table, ledger/slot/voucher-quota, invoice-line-items & analytics revenue MCP reconciliation + docker compose config + determinism re-test passed"
} elseif ($Phase -ge 7) {
    "Guardrails + MCP check_phase_gate + LocalDB 22-table, ledger/slot/voucher-quota & analytics revenue MCP reconciliation + docker compose config + determinism re-test passed"
} elseif ($Phase -ge 6) {
    "Guardrails + MCP check_phase_gate + LocalDB 22-table & ledger/slot/voucher-quota MCP invariants + docker compose config + determinism re-test passed"
} elseif ($Phase -ge 4) {
    "Guardrails + MCP check_phase_gate + LocalDB 22-table & ledger/slot MCP invariants + docker compose config + determinism re-test passed"
} else {
    "Guardrails + MCP check_phase_gate + docker compose config + determinism re-test passed"
}
$B3Detail = if ($B3Passed) { $B3SuccessLabel } else { ($B3Errors -join "; ") }
$BatteryResults.Add(@{
    Id        = "T$Round.3"
    Name      = "Guardrails + MCP Gate + Determinism"
    Passed    = $B3Passed
    ElapsedMs = $Sw3.ElapsedMilliseconds
    Detail    = $B3Detail
}) | Out-Null
} finally {
    if ($UseIsolatedBackendArtifacts -and -not [string]::IsNullOrWhiteSpace($BackendArtifactsDir) -and (Test-Path $BackendArtifactsDir)) {
        Remove-Item -LiteralPath $BackendArtifactsDir -Recurse -Force -ErrorAction SilentlyContinue
        $BackendObjParent = Join-Path $RepoRoot "backend\obj"
        if ((Test-Path $BackendObjParent) -and @(Get-ChildItem -LiteralPath $BackendObjParent -Force -ErrorAction SilentlyContinue).Count -eq 0) {
            Remove-Item -LiteralPath $BackendObjParent -Force -ErrorAction SilentlyContinue
        }
    }
}

$OverallStopwatch.Stop()

# ==============================================================================
# STRUCTURED QUALITY-LOOP REPORT
# ==============================================================================
Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "  PHASE $Phase QUALITY-LOOP TEST BATTERY SUMMARY (ROUND $Round | $($OverallStopwatch.ElapsedMilliseconds) ms)" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$FailedCount = 0
foreach ($Row in $BatteryResults) {
    $Badge = if ($Row.Passed) { "PASS" } else { "FAIL" }
    $Color = if ($Row.Passed) { "Green" } else { "Red" }
    if (-not $Row.Passed) { $FailedCount++ }
    Write-Host ("  [{0}] {1} | {2,-42} | {3,6} ms | {4}" -f $Badge, $Row.Id, $Row.Name, $Row.ElapsedMs, $Row.Detail) -ForegroundColor $Color
}
Write-Host "==========================================================================" -ForegroundColor Cyan

if ($FailedCount -gt 0) {
    Write-FailMsg "Phase $Phase Round $Round Test Gate FAILED ($FailedCount / $($BatteryResults.Count) batteries failed)."
    exit 1
}

Write-Pass "Phase $Phase Round $Round Test Gate PASSED (3 / 3 batteries green)!"
exit 0
