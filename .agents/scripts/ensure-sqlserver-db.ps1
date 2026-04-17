<#
.SYNOPSIS
    Ensures SQL Server LocalDB ((localdb)\MSSQLLocalDB) or SQL Server 2022 Docker container is healthy,
    probes connectivity via sqlcmd or System.Data.SqlClient.SqlConnection fallback, and optionally applies
    EF Core migrations via local dotnet tool manifest (.config/dotnet-tools.json) or global dotnet-ef.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$UseDocker,
    [switch]$Migrate,
    [string]$LocalDbInstance = "MSSQLLocalDB",
    [string]$DatabaseName = "MotorcycleDealerDb"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$SlnPath = Join-Path $RepoRoot "backend\MotorcycleDealer.sln"
$InfraProj = Join-Path $RepoRoot "backend\src\MotorcycleDealer.Infrastructure"
$WebApiProj = Join-Path $RepoRoot "backend\src\MotorcycleDealer.WebApi"
$ComposePath = Join-Path $RepoRoot "docker-compose.yml"
$ToolManifestPath = Join-Path $RepoRoot ".config\dotnet-tools.json"

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-WarnMsg([string]$Msg) { Write-Host "[WARN] $Msg" -ForegroundColor Yellow }

$ModeLabel = if ($DryRun) { "DRY-RUN" } else { "LIVE" }
$TargetEngine = if ($UseDocker) { "Docker SQL Server 2022" } else { "(localdb)\$LocalDbInstance" }
Write-Info "Ensuring SQL Server readiness for '$DatabaseName' via $TargetEngine (Mode=$ModeLabel)"

if ($DryRun) {
    if ($UseDocker) {
        $DockerCmd = Get-Command "docker" -ErrorAction SilentlyContinue
        $DockerStatus = if ($null -ne $DockerCmd) { $DockerCmd.Source } else { "not in PATH" }
        Write-Info "[DRY-RUN] Docker CLI: $DockerStatus"
        Write-Info "[DRY-RUN] Would check '$ComposePath' and run: docker compose up -d sqlserver && docker compose ps sqlserver"
    } else {
        $LocalDbCmd = Get-Command "sqllocaldb" -ErrorAction SilentlyContinue
        $SqlCmd = Get-Command "sqlcmd" -ErrorAction SilentlyContinue
        $LocalDbStatus = if ($null -ne $LocalDbCmd) { $LocalDbCmd.Source } else { "not in PATH" }
        $SqlCmdStatus = if ($null -ne $SqlCmd) { $SqlCmd.Source } else { "not in PATH (System.Data.SqlClient.SqlConnection fallback active)" }
        Write-Info "[DRY-RUN] sqllocaldb: $LocalDbStatus | sqlcmd: $SqlCmdStatus"
        Write-Info "[DRY-RUN] Would run: sqllocaldb info $LocalDbInstance"
        Write-Info "[DRY-RUN] Would start if stopped: sqllocaldb start $LocalDbInstance"
        if ($null -ne $SqlCmd) {
            Write-Info "[DRY-RUN] Would probe SQL connectivity via sqlcmd -S `"(localdb)\$LocalDbInstance`""
        } else {
            Write-Info "[DRY-RUN] Would probe SQL connectivity via System.Data.SqlClient.SqlConnection against `"(localdb)\$LocalDbInstance`""
        }
    }

    if ($Migrate) {
        if (Test-Path $SlnPath) {
            if (Test-Path $ToolManifestPath) {
                Write-Info "[DRY-RUN] Would restore local tools & migrate: dotnet tool restore && dotnet tool run dotnet-ef database update --project `"$InfraProj`" --startup-project `"$WebApiProj`""
            } else {
                Write-Info "[DRY-RUN] Would apply EF Core migrations: dotnet ef database update --project `"$InfraProj`" --startup-project `"$WebApiProj`""
            }
        } else {
            Write-Info "[DRY-RUN] backend/MotorcycleDealer.sln not scaffolded yet (Phase <= 2); EF Core migration step will activate in Phase 4+."
        }
    } else {
        Write-Info "[DRY-RUN] -Migrate flag not set; schema migration skipped."
    }

    Write-Pass "SQL Server readiness dry-run completed cleanly."
    exit 0
}

if ($UseDocker) {
    if (-not (Test-Path $ComposePath)) {
        Write-WarnMsg "docker-compose.yml not present yet (scaffolded in Phase 3 T-308). Nothing to start."
        exit 0
    }
    Write-Info "Checking Docker Compose SQL Server 2022 service..."
    & docker compose -f $ComposePath up -d sqlserver
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start sqlserver container via docker compose."
    }
    & docker compose -f $ComposePath ps sqlserver
    Write-Pass "Docker SQL Server 2022 service is up."
} else {
    $LocalDbCmd = Get-Command "sqllocaldb" -ErrorAction SilentlyContinue
    if ($null -eq $LocalDbCmd) {
        throw "sqllocaldb utility not found in PATH. Install SQL Server Express LocalDB or pass -UseDocker."
    }

    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $InfoOutput = & sqllocaldb info $LocalDbInstance 2>&1 | Out-String
    $InfoExit = $LASTEXITCODE
    $ErrorActionPreference = $PrevEap
    if ($InfoExit -ne 0) {
        Write-Info "Creating LocalDB instance '$LocalDbInstance'..."
        & sqllocaldb create $LocalDbInstance -s | Out-Host
    } elseif ($InfoOutput -notmatch 'State:\s+Running') {
        Write-Info "Starting LocalDB instance '$LocalDbInstance'..."
        & sqllocaldb start $LocalDbInstance | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to start LocalDB instance '$LocalDbInstance'."
        }
    } else {
        Write-Pass "LocalDB instance '$LocalDbInstance' is already running."
    }

    $SqlCmd = Get-Command "sqlcmd" -ErrorAction SilentlyContinue
    if ($null -ne $SqlCmd) {
        $PrevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $VersionOut = & sqlcmd -S "(localdb)\$LocalDbInstance" -W -h -1 -Q "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR(50)) + ' | ' + CAST(SERVERPROPERTY('Edition') AS VARCHAR(80));" 2>&1 | Out-String
        $VerExit = $LASTEXITCODE
        $ErrorActionPreference = $PrevEap
        if ($VerExit -ne 0) {
            throw "sqlcmd connection check to (localdb)\$LocalDbInstance failed: $VersionOut"
        }
        Write-Pass "Connected to (localdb)\$LocalDbInstance via sqlcmd -> $($VersionOut.Trim())"
    } else {
        $Csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $Csb["Data Source"] = "(localdb)\$LocalDbInstance"
        $Csb["Initial Catalog"] = "master"
        $Csb["Integrated Security"] = $true
        $Csb["TrustServerCertificate"] = $true
        $Csb["Connect Timeout"] = 10

        $Conn = New-Object System.Data.SqlClient.SqlConnection($Csb.ConnectionString)
        try {
            $Conn.Open()
            $Cmd = $Conn.CreateCommand()
            $Cmd.CommandText = "SELECT CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR(50)) + ' | ' + CAST(SERVERPROPERTY('Edition') AS VARCHAR(80));"
            $VersionScalar = [string]$Cmd.ExecuteScalar()
            Write-Pass "Connected to (localdb)\$LocalDbInstance via SqlClient -> $($VersionScalar.Trim())"
        } catch {
            throw "SqlClient connection check to (localdb)\$LocalDbInstance failed: $($_.Exception.Message)"
        } finally {
            $Conn.Close()
        }
    }
}

if ($Migrate) {
    if ((Test-Path $SlnPath) -and (Test-Path $InfraProj) -and (Test-Path $WebApiProj)) {
        Write-Info "Applying EF Core migrations and idempotent seed for $DatabaseName..."
        $ReleaseDll = Join-Path $RepoRoot "backend\src\MotorcycleDealer.WebApi\bin\Release\net10.0\MotorcycleDealer.WebApi.dll"
        Push-Location $RepoRoot
        try {
            if (Test-Path $ReleaseDll) {
                & dotnet run --no-build --configuration Release --project $WebApiProj -- --seed-only
            } else {
                & dotnet run --configuration Release --project $WebApiProj -- --seed-only
            }
            if ($LASTEXITCODE -ne 0) {
                throw "DatabaseSeeder (--seed-only) migration/seed failed with exit code $LASTEXITCODE."
            }
            Write-Pass "Database '$DatabaseName' migrated and seeded cleanly."
        } finally {
            Pop-Location
        }
    } else {
        Write-Info "backend/MotorcycleDealer.sln not scaffolded yet (Phase <= 3); skipping dotnet ef database update."
    }
}

exit 0
