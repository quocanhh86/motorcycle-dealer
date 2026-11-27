<#
.SYNOPSIS
    PowerShell wrapper for the Motorcycle Dealer 6-month (183-day) idempotent daily Git shipper.
    Safe for interactive CLI execution and non-interactive Windows Task Scheduler runs.
#>
[CmdletBinding()]
param(
    [switch]$Status,
    [switch]$DryRun,
    [switch]$RunDaily,
    [switch]$ForceNext
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$ShipperMjs = Join-Path $PSScriptRoot "daily-git-shipper.mjs"
$GitDir = Join-Path $RepoRoot ".git"
$StateDir = Join-Path $GitDir "daily-shipper"
$SchedulerLog = Join-Path $StateDir "task-scheduler.log"

# Ensure GitHub CLI, Git, and Node standard Windows locations are prepended in headless Task Scheduler sessions
$ExtraPaths = @(
    "C:\Program Files\GitHub CLI",
    "C:\Program Files\Git\cmd",
    "C:\Program Files\Git\bin",
    "C:\Program Files\nodejs"
)
$CurrentSegments = @($env:PATH -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$ToPrepend = New-Object System.Collections.Generic.List[string]
foreach ($Dir in $ExtraPaths) {
    if ((Test-Path $Dir) -and (-not ($CurrentSegments | Where-Object { $_ -ieq $Dir }))) {
        $ToPrepend.Add($Dir) | Out-Null
    }
}
if ($ToPrepend.Count -gt 0) {
    $env:PATH = "$(($ToPrepend -join ';'));$env:PATH"
}

$NodeCmd = Get-Command "node" -ErrorAction SilentlyContinue
$NodeExe = if ($null -ne $NodeCmd) { $NodeCmd.Source } elseif (Test-Path "C:\Program Files\nodejs\node.exe") { "C:\Program Files\nodejs\node.exe" } else { $null }

if ([string]::IsNullOrWhiteSpace($NodeExe)) {
    Write-Error "Node.js executable not found. Install Node.js v22+ or add it to PATH."
    exit 1
}

$NodeArgs = New-Object System.Collections.Generic.List[string]
$NodeArgs.Add($ShipperMjs) | Out-Null

if ($Status) {
    $NodeArgs.Add("--status") | Out-Null
} elseif ($DryRun) {
    $NodeArgs.Add("--dry-run") | Out-Null
} else {
    $NodeArgs.Add("--run-daily") | Out-Null
}

if ($ForceNext) {
    $NodeArgs.Add("--force-next") | Out-Null
}

$env:GIT_TERMINAL_PROMPT = "0"
$env:GCM_INTERACTIVE = "never"

Push-Location $RepoRoot
try {
    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $OutputLines = & $NodeExe @NodeArgs 2>&1
    $ExitCode = $LASTEXITCODE
    $ErrorActionPreference = $PrevEap

    foreach ($Line in $OutputLines) {
        Write-Host "$Line"
    }

    if ((-not $Status) -and (-not $DryRun) -and (Test-Path $GitDir)) {
        if (-not (Test-Path $StateDir)) {
            New-Item -ItemType Directory -Path $StateDir -Force | Out-Null
        }
        $Stamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssK")
        $Joined = ($OutputLines | ForEach-Object { "$_" }) -join " || "
        Add-Content -LiteralPath $SchedulerLog -Value "[$Stamp] exit=$ExitCode :: $Joined" -Encoding UTF8
    }

    exit $ExitCode
} finally {
    Pop-Location
}
