<#
.SYNOPSIS
    Validates Android SDK/JDK/WHPX acceleration, provisions the Pixel_8_API_35 AVD
    (system-images;android-35;google_apis_playstore;x86_64), optionally boots the emulator
    until sys.boot_completed=1, and optionally configures `adb reverse tcp:5080 tcp:5080`.
#>
[CmdletBinding()]
param(
    [string]$AndroidSdkDirectory = "C:\Program Files (x86)\Android\android-sdk",
    [string]$JavaSdkDirectory = "C:\Program Files\Android\openjdk\jdk-21.0.8",
    [string]$AvdName = "Pixel_8_API_35",
    [string]$SystemImage = "system-images;android-35;google_apis_playstore;x86_64",
    [int]$ApiPort = 5080,
    [int]$BootTimeoutSeconds = 180,
    [switch]$BootEmulator,
    [switch]$SetupAdbReverse,
    [switch]$Headless,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-WarnMsg([string]$Msg) { Write-Host "[WARN] $Msg" -ForegroundColor Yellow }
function Write-FailMsg([string]$Msg) { Write-Host "[FAIL] $Msg" -ForegroundColor Red }

function Invoke-NativeCapture {
    param(
        [string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$StdinText = $null
    )
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($null -ne $StdinText) {
            $output = @($StdinText | & $FilePath @Arguments 2>&1)
        } else {
            $output = @(& $FilePath @Arguments 2>&1)
        }
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $prevEap
    }
    return @{
        ExitCode = $code
        Lines    = $output | ForEach-Object { "$_" }
        Text     = (($output | ForEach-Object { "$_" }) -join "`n").Trim()
    }
}

function Resolve-CmdlineToolsBin([string]$SdkRoot) {
    $cmdlineRoot = Join-Path $SdkRoot "cmdline-tools"
    if (-not (Test-Path $cmdlineRoot)) {
        return $null
    }
    $latestBin = Join-Path $cmdlineRoot "latest\bin"
    if (Test-Path (Join-Path $latestBin "avdmanager.bat")) {
        return $latestBin
    }
    $candidates = Get-ChildItem -Path $cmdlineRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object -Property Name -Descending
    foreach ($dir in $candidates) {
        $binDir = Join-Path $dir.FullName "bin"
        if (Test-Path (Join-Path $binDir "avdmanager.bat")) {
            return $binDir
        }
    }
    return $null
}

Write-Info "=== .NET 10 MAUI Android SDK / WHPX / AVD Readiness ($AvdName) ==="

# 1. Verify ANDROID_HOME & JAVA_HOME
if (-not (Test-Path $AndroidSdkDirectory)) {
    Write-FailMsg "Android SDK directory not found at '$AndroidSdkDirectory'."
    exit 1
}
if (-not (Test-Path $JavaSdkDirectory)) {
    Write-FailMsg "OpenJDK directory not found at '$JavaSdkDirectory'."
    exit 1
}

$env:ANDROID_HOME = $AndroidSdkDirectory
$env:ANDROID_SDK_ROOT = $AndroidSdkDirectory
$env:JAVA_HOME = $JavaSdkDirectory

$AdbExe = Join-Path $AndroidSdkDirectory "platform-tools\adb.exe"
$EmulatorExe = Join-Path $AndroidSdkDirectory "emulator\emulator.exe"
$EmulatorCheckExe = Join-Path $AndroidSdkDirectory "emulator\emulator-check.exe"
$CmdlineBin = Resolve-CmdlineToolsBin $AndroidSdkDirectory

if (-not (Test-Path $AdbExe)) {
    Write-FailMsg "Missing adb.exe at '$AdbExe'."
    exit 1
}
if (-not (Test-Path $EmulatorExe)) {
    Write-FailMsg "Missing emulator.exe at '$EmulatorExe'."
    exit 1
}
if ([string]::IsNullOrWhiteSpace($CmdlineBin)) {
    Write-FailMsg "Missing Android cmdline-tools (avdmanager.bat) under '$AndroidSdkDirectory\cmdline-tools'."
    exit 1
}

$AvdManagerBat = Join-Path $CmdlineBin "avdmanager.bat"
$env:PATH = "$JavaSdkDirectory\bin;$CmdlineBin;$AndroidSdkDirectory\platform-tools;$AndroidSdkDirectory\emulator;$env:PATH"

Write-Pass "Resolved ANDROID_HOME='$AndroidSdkDirectory', JAVA_HOME='$JavaSdkDirectory', cmdline-tools='$CmdlineBin'."

# 2. Verify System Image on Disk
$ImageRelPath = ($SystemImage -replace ";", "\")
$ImageDir = Join-Path $AndroidSdkDirectory $ImageRelPath
if (-not (Test-Path $ImageDir)) {
    Write-FailMsg "Required Android system image '$SystemImage' not found at '$ImageDir'."
    exit 1
}
Write-Pass "System image present: $SystemImage"

# 3. Verify WHPX / Hardware Acceleration via emulator-check.exe accel
if (Test-Path $EmulatorCheckExe) {
    $accelRes = Invoke-NativeCapture -FilePath $EmulatorCheckExe -Arguments @("accel")
    $accelClean = ($accelRes.Lines -join " ").Trim()
    if ($accelRes.ExitCode -ne 0 -or ($accelRes.Text -notmatch "WHPX|HAXM|AEHD|usable")) {
        Write-FailMsg "Hardware acceleration check failed (exit=$($accelRes.ExitCode)): $accelClean"
        exit 1
    }
    Write-Pass "Emulator acceleration verified: $accelClean"
} else {
    Write-WarnMsg "emulator-check.exe not found at '$EmulatorCheckExe'; skipping WHPX probe."
}

# 4. Verify ADB Version
$adbVerRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("version")
if ($adbVerRes.ExitCode -ne 0) {
    Write-FailMsg "adb version failed: $($adbVerRes.Text)"
    exit 1
}
$adbFirstLine = ($adbVerRes.Lines | Select-Object -First 1)
Write-Pass "ADB ready: $adbFirstLine"

# 5. Check / Create AVD Pixel_8_API_35
$listAvdRes = Invoke-NativeCapture -FilePath $EmulatorExe -Arguments @("-list-avds")
$existingAvds = @($listAvdRes.Lines | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -notmatch "^(INFO|WARNING)" })
$avdExists = $existingAvds -contains $AvdName

if ($avdExists) {
    Write-Pass "AVD '$AvdName' already provisioned."
} else {
    Write-Info "AVD '$AvdName' not found; inspecting available avdmanager device profiles..."
    $devListRes = Invoke-NativeCapture -FilePath $AvdManagerBat -Arguments @("list", "device")
    if ($devListRes.ExitCode -ne 0) {
        Write-FailMsg "avdmanager list device failed: $($devListRes.Text)"
        exit 1
    }

    $availableDevices = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($line in $devListRes.Lines) {
        if ($line -match 'id:\s*\d+\s+or\s+"([^"]+)"') {
            $availableDevices.Add($Matches[1]) | Out-Null
        }
    }

    $preferredProfiles = @("pixel_8", "pixel_7", "pixel_6", "pixel_5", "pixel", "medium_phone")
    $selectedDevice = $null
    foreach ($candidate in $preferredProfiles) {
        if ($availableDevices.Contains($candidate)) {
            $selectedDevice = $candidate
            break
        }
    }

    if ($DryRun) {
        $devLabel = if ($selectedDevice) { $selectedDevice } else { "<default>" }
        Write-Info "[DRY-RUN] Would create AVD '$AvdName' using image '$SystemImage' and device profile '$devLabel'."
    } else {
        $createArgs = @("create", "avd", "--force", "-n", $AvdName, "-k", $SystemImage)
        if ($selectedDevice) {
            $createArgs += @("-d", $selectedDevice)
            Write-Info "Creating AVD '$AvdName' with device profile '$selectedDevice' and image '$SystemImage'..."
        } else {
            Write-Info "Creating AVD '$AvdName' with default generic phone profile and image '$SystemImage'..."
        }

        $createRes = Invoke-NativeCapture -FilePath $AvdManagerBat -Arguments $createArgs -StdinText "no"
        if ($createRes.ExitCode -ne 0) {
            Write-FailMsg "Failed to create AVD '$AvdName' (exit=$($createRes.ExitCode)): $($createRes.Text)"
            exit 1
        }

        # Ensure smooth keyboard & GPU config in AVD config.ini
        $avdConfigPath = Join-Path $env:USERPROFILE ".android\avd\$AvdName.avd\config.ini"
        if (Test-Path $avdConfigPath) {
            $cfgContent = Get-Content -LiteralPath $avdConfigPath -Raw -Encoding UTF8
            $additions = New-Object System.Collections.Generic.List[string]
            if ($cfgContent -notmatch "(?m)^hw\.keyboard\s*=") { $additions.Add("hw.keyboard=yes") | Out-Null }
            if ($cfgContent -notmatch "(?m)^hw\.gpu\.enabled\s*=") { $additions.Add("hw.gpu.enabled=yes") | Out-Null }
            if ($cfgContent -notmatch "(?m)^hw\.gpu\.mode\s*=") { $additions.Add("hw.gpu.mode=auto") | Out-Null }
            if ($additions.Count -gt 0) {
                $updatedCfg = $cfgContent.TrimEnd() + "`r`n" + ($additions -join "`r`n") + "`r`n"
                [System.IO.File]::WriteAllText($avdConfigPath, $updatedCfg, [System.Text.UTF8Encoding]::new($false))
            }
        }

        $verifyAvdRes = Invoke-NativeCapture -FilePath $EmulatorExe -Arguments @("-list-avds")
        $verifiedAvds = @($verifyAvdRes.Lines | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        if ($verifiedAvds -notcontains $AvdName) {
            Write-FailMsg "AVD '$AvdName' creation completed but was not returned by 'emulator -list-avds'."
            exit 1
        }
        Write-Pass "Provisioned AVD '$AvdName' (device profile: $(if ($selectedDevice) { $selectedDevice } else { 'default' }))."
    }
}

# 6. Helper to Detect Running Emulator Serial
function Get-OnlineAndroidSerial {
    $devRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("devices")
    foreach ($line in $devRes.Lines) {
        if ($line -match '^(\S+)\s+device$') {
            return $Matches[1]
        }
    }
    return $null
}

$ActiveSerial = Get-OnlineAndroidSerial

# 7. Optional: Boot Emulator & Wait for sys.boot_completed=1
if ($BootEmulator) {
    if ($DryRun) {
        Write-Info "[DRY-RUN] Would boot emulator AVD '$AvdName' and poll 'sys.boot_completed=1'."
    } else {
        if ($ActiveSerial) {
            Write-Pass "Android device/emulator already online: $ActiveSerial"
        } else {
            Write-Info "Starting Android emulator '$AvdName' (WHPX)..."
            $emuArgs = @("-avd", $AvdName, "-no-snapshot-load", "-no-boot-anim", "-gpu", "auto")
            if ($Headless) {
                $emuArgs += @("-no-window", "-no-audio")
            }
            Start-Process -FilePath $EmulatorExe -ArgumentList $emuArgs -WindowStyle Minimized | Out-Null

            $deadline = (Get-Date).AddSeconds($BootTimeoutSeconds)
            while ((Get-Date) -lt $deadline) {
                $ActiveSerial = Get-OnlineAndroidSerial
                if ($ActiveSerial) {
                    $bootRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $ActiveSerial, "shell", "getprop", "sys.boot_completed")
                    if ($bootRes.ExitCode -eq 0 -and $bootRes.Text.Trim() -eq "1") {
                        break
                    }
                }
                Start-Sleep -Seconds 3
            }

            if (-not $ActiveSerial) {
                Write-FailMsg "Timed out after ${BootTimeoutSeconds}s waiting for emulator '$AvdName' to appear in adb devices."
                exit 1
            }
            $finalBoot = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $ActiveSerial, "shell", "getprop", "sys.boot_completed")
            if ($finalBoot.Text.Trim() -ne "1") {
                Write-FailMsg "Timed out after ${BootTimeoutSeconds}s waiting for sys.boot_completed=1 on '$ActiveSerial'."
                exit 1
            }

            # Dismiss lockscreen & keep screen awake for automated UI inspection
            Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $ActiveSerial, "shell", "wm", "dismiss-keyguard") | Out-Null
            Write-Pass "Emulator '$AvdName' booted cleanly (serial=$ActiveSerial, sys.boot_completed=1)."
        }
    }
}

# 8. Optional: Configure ADB Reverse Tunnel (tcp:<ApiPort> -> tcp:<ApiPort>)
if ($SetupAdbReverse) {
    if ($DryRun) {
        Write-Info "[DRY-RUN] Would configure 'adb reverse tcp:$ApiPort tcp:$ApiPort'."
    } else {
        $ActiveSerial = Get-OnlineAndroidSerial
        if (-not $ActiveSerial) {
            Write-FailMsg "Cannot setup adb reverse: no online Android device/emulator detected. Re-run with -BootEmulator."
            exit 1
        }
        $revRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $ActiveSerial, "reverse", "tcp:$ApiPort", "tcp:$ApiPort")
        if ($revRes.ExitCode -ne 0) {
            Write-FailMsg "adb reverse tcp:$ApiPort tcp:$ApiPort failed: $($revRes.Text)"
            exit 1
        }
        $listRev = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $ActiveSerial, "reverse", "--list")
        Write-Pass "Configured ADB reverse tunnel on $ActiveSerial (tcp:$ApiPort -> tcp:$ApiPort): $($listRev.Text)"
    }
}

Write-Pass "Android MAUI environment preflight completed cleanly."
exit 0
