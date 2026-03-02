<#
.SYNOPSIS
    Registers, inspects, or unregisters the user-level Windows Scheduled Task
    'MotorcycleDealer-DailyGitShipper' with 4 resilient triggers (AtLogOn + 09:05, 14:00, 20:00)
    to commit and push 1 logical slice every day for 6 months without missing offline days.
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

$PowerShellExe = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe"
if (-not (Test-Path $PowerShellExe)) {
    $PowerShellExe = (Get-Command "powershell.exe" -ErrorAction Stop).Source
}
$TaskArgs = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$ShipperPs1`" -RunDaily"
$DailySlotStrings = @("09:05", "14:00", $Time) | Select-Object -Unique | Sort-Object

if ($DryRun) {
    Write-Host "[DRY-RUN] Would register Windows Scheduled Task '$TaskName':" -ForegroundColor Cyan
    Write-Host "  Logon Trigger : AtLogOn ($env:USERNAME, Delay=PT2M)"
    Write-Host "  Daily Slots   : $($DailySlotStrings -join ', ') (local time, StartWhenAvailable=true, StopOnIdleEnd=false, RestartCount=3/15m)"
    Write-Host "  Execute       : $PowerShellExe $TaskArgs"
    Write-Host "  WorkingDir    : $RepoRoot"
    exit 0
}

$Action = New-ScheduledTaskAction -Execute $PowerShellExe -Argument $TaskArgs -WorkingDirectory $RepoRoot

$LogonTrigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$LogonTrigger.Delay = "PT2M"

$DailyTriggers = foreach ($Slot in $DailySlotStrings) {
    $ParsedTime = [datetime]::ParseExact($Slot, "HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)
    New-ScheduledTaskTrigger -Daily -At $ParsedTime
}

$AllTriggers = @($LogonTrigger) + @($DailyTriggers)

$Settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -DontStopOnIdleEnd `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 15) `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

$Description = "Daily 6-month (183-day) Git slice shipper for Motorcycle Dealer & Loyalty Management Platform ($RepoRoot). Idempotent multi-trigger schedule (AtLogOn + $($DailySlotStrings -join ', '))."

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $AllTriggers `
    -Settings $Settings `
    -Description $Description `
    -Force | Out-Null

$VerifiedTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Stop
$TaskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -ErrorAction Stop

Write-Host "[PASS] Registered Windows Scheduled Task '$($VerifiedTask.TaskName)' with $($VerifiedTask.Triggers.Count) triggers (AtLogOn + $($DailySlotStrings -join ', '); State=$($VerifiedTask.State), StopOnIdleEnd=$($VerifiedTask.Settings.IdleSettings.StopOnIdleEnd), NextRunTime=$($TaskInfo.NextRunTime))." -ForegroundColor Green
exit 0
