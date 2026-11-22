<#
.SYNOPSIS
    Registers, inspects, or unregisters the user-level Windows Scheduled Task
    'MotorcycleDealer-DailyGitShipper' to commit and push 1 logical slice every day for 6 months.
#>
[CmdletBinding()]
param(
    [string]$TaskName = "MotorcycleDealer-DailyGitShipper",

    [ValidatePattern('^([01]\d|2[0-3]):[0-5]\d$')]
    [string]$Time = "20:00",

    [switch]$Unregister,

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$ShipperPs1 = Join-Path $PSScriptRoot "daily-git-shipper.ps1"

if (-not (Test-Path $ShipperPs1)) {
    throw "Required script missing: $ShipperPs1"
}

if ($Unregister) {
    if ($DryRun) {
        Write-Host "[DRY-RUN] Would unregister Windows Scheduled Task '$TaskName' if present." -ForegroundColor Cyan
        exit 0
    }

    $Existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if ($null -eq $Existing) {
        Write-Host "[INFO] Scheduled Task '$TaskName' is not currently registered." -ForegroundColor Yellow
        exit 0
    }

    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "[PASS] Unregistered Windows Scheduled Task '$TaskName'." -ForegroundColor Green
    exit 0
}

$TaskArgs = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$ShipperPs1`" -RunDaily"

if ($DryRun) {
    Write-Host "[DRY-RUN] Would register Windows Scheduled Task '$TaskName':" -ForegroundColor Cyan
    Write-Host "  Daily Trigger : $Time (local time, StartWhenAvailable=true)"
    Write-Host "  Execute       : powershell.exe $TaskArgs"
    Write-Host "  WorkingDir    : $RepoRoot"
    exit 0
}

$TriggerTime = [datetime]::ParseExact($Time, "HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
$Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $TaskArgs -WorkingDirectory $RepoRoot
$Trigger = New-ScheduledTaskTrigger -Daily -At $TriggerTime
$Settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

$Description = "Daily 6-month (183-day) Git slice shipper for Motorcycle Dealer & Loyalty Management Platform ($RepoRoot)."

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $Trigger `
    -Settings $Settings `
    -Description $Description `
    -Force | Out-Null

$VerifiedTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Stop
$TaskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -ErrorAction Stop

Write-Host "[PASS] Registered Windows Scheduled Task '$($VerifiedTask.TaskName)' (State=$($VerifiedTask.State), NextRunTime=$($TaskInfo.NextRunTime))." -ForegroundColor Green
exit 0
