<#
.SYNOPSIS
    Portfolio end-to-end live runner & evidence harvester:
    1) Ensures SQL Server LocalDB / Docker readiness (& migrations in Phase 4+)
    2) Boots ASP.NET Core 10 WebApi + AngularJS 1.8 SPA (in Phase 3+)
    3) Executes seed-demo-scenarios.mjs & capture-live-evidence.mjs into docs/evidence/
    4) Tears down background processes deterministically.
    Supports -DryRun cleanly so Phase 2 exits 0 with zero side effects.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$UseDocker,
    [switch]$SkipBuild,
    [string]$ApiUrl = "http://localhost:5080",
    [string]$SpaUrl = "http://localhost:4200"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$EnsureDbScript = Join-Path $PSScriptRoot "ensure-sqlserver-db.ps1"
$SeedScript = Join-Path $PSScriptRoot "seed-demo-scenarios.mjs"
$CaptureScript = Join-Path $PSScriptRoot "capture-live-evidence.mjs"
$SlnPath = Join-Path $RepoRoot "backend\MotorcycleDealer.sln"
$WebApiProj = Join-Path $RepoRoot "backend\src\MotorcycleDealer.WebApi\MotorcycleDealer.WebApi.csproj"
$FrontendDir = Join-Path $RepoRoot "frontend"

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }

if ($DryRun) {
    Write-Info "[DRY-RUN] Starting Run-and-Capture Dealer Platform pipeline..."
    $DbArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $EnsureDbScript, "-DryRun", "-Migrate")
    if ($UseDocker) { $DbArgs += "-UseDocker" }
    & powershell @DbArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (Test-Path $SlnPath) {
        Write-Info "[DRY-RUN] Would start background API: dotnet run --project `"$WebApiProj`" --urls `"$ApiUrl`""
        Write-Info "[DRY-RUN] Would start background AngularJS SPA server in `"$FrontendDir`" on `"$SpaUrl`""
    } else {
        Write-Info "[DRY-RUN] Phase <= 2 detected (backend/ & frontend/ not scaffolded yet); live server spawn deferred to Phase 3+."
    }

    & node $SeedScript --dry-run --api-url $ApiUrl
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & node $CaptureScript --dry-run --api-url $ApiUrl --spa-url $SpaUrl
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Pass "Run-and-Capture dry-run completed with 0 errors."
    exit 0
}

if (-not (Test-Path $SlnPath)) {
    throw "backend/MotorcycleDealer.sln does not exist yet (active Phase <= 2). Re-run with -DryRun or complete Phase 3 first."
}

# 1. Ensure SQL Server & Migrations
$DbLiveArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $EnsureDbScript, "-Migrate")
if ($UseDocker) { $DbLiveArgs += "-UseDocker" }
& powershell @DbLiveArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipBuild) {
    Write-Info "Building backend solution..."
    & dotnet build $SlnPath --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}

$ApiProc = $null
$SpaProc = $null

function Wait-HttpReady([string]$ProbeUrl, [int]$TimeoutSeconds = 30) {
    $Deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $Deadline) {
        try {
            $Resp = Invoke-WebRequest -Uri $ProbeUrl -UseBasicParsing -TimeoutSec 3
            if ([int]$Resp.StatusCode -ge 200 -and [int]$Resp.StatusCode -lt 500) {
                return $true
            }
        } catch {
            Start-Sleep -Milliseconds 750
        }
    }
    return $false
}

try {
    Write-Info "Launching MotorcycleDealer.WebApi on $ApiUrl..."
    $ApiProc = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--no-build", "--configuration", "Release", "--project", $WebApiProj, "--urls", $ApiUrl) -WorkingDirectory $RepoRoot -PassThru -WindowStyle Hidden

    if (Test-Path (Join-Path $FrontendDir "package.json")) {
        Write-Info "Launching AngularJS 1.8 SPA server on $SpaUrl..."
        $SpaProc = Start-Process -FilePath "npm" -ArgumentList @("start") -WorkingDirectory $FrontendDir -PassThru -WindowStyle Hidden
    }

    $ApiRoot = $ApiUrl.TrimEnd('/')
    if (-not (Wait-HttpReady -ProbeUrl "$ApiRoot/health" -TimeoutSeconds 35)) {
        if (-not (Wait-HttpReady -ProbeUrl "$ApiRoot/api/v1/health" -TimeoutSeconds 10)) {
            throw "Timed out waiting for WebApi health check at $ApiRoot."
        }
    }
    Write-Pass "WebApi is healthy at $ApiRoot."

    if ($null -ne $SpaProc) {
        if (-not (Wait-HttpReady -ProbeUrl $SpaUrl -TimeoutSeconds 25)) {
            throw "Timed out waiting for AngularJS SPA at $SpaUrl."
        }
        Write-Pass "AngularJS SPA is reachable at $SpaUrl."
    }

    & node $SeedScript --api-url $ApiUrl
    if ($LASTEXITCODE -ne 0) { throw "seed-demo-scenarios.mjs failed." }

    & node $CaptureScript --api-url $ApiUrl --spa-url $SpaUrl
    if ($LASTEXITCODE -ne 0) { throw "capture-live-evidence.mjs failed." }

    Write-Pass "Portfolio live run and evidence capture completed successfully!"
} finally {
    if ($null -ne $SpaProc -and -not $SpaProc.HasExited) {
        Write-Info "Stopping AngularJS SPA process (PID=$($SpaProc.Id))..."
        Stop-Process -Id $SpaProc.Id -Force -ErrorAction SilentlyContinue
    }
    if ($null -ne $ApiProc -and -not $ApiProc.HasExited) {
        Write-Info "Stopping WebApi process (PID=$($ApiProc.Id))..."
        Stop-Process -Id $ApiProc.Id -Force -ErrorAction SilentlyContinue
    }
}

exit 0
