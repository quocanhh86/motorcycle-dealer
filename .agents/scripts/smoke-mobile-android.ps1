<#
.SYNOPSIS
    End-to-end Android Emulator Smoke Runner for MotorcycleDealer.Mobile.App (.NET 10 MAUI):
      1) Builds MotorcycleDealer.Mobile.App (-f net10.0-android) with explicit AndroidSdkDirectory & JavaSdkDirectory
      2) Ensures Pixel_8_API_35 AVD is booted (sys.boot_completed=1) and configures `adb reverse tcp:5080 tcp:5080`
      3) Installs the compiled APK via `adb install -r`
      4) Launches the MainActivity via `am start` and verifies process is alive via `pidof`
      5) Dumps live UI hierarchy via `uiautomator dump` and captures a real PNG screenshot via `adb exec-out screencap -p`
#>
[CmdletBinding()]
param(
    [string]$AndroidSdkDirectory = "C:\Program Files (x86)\Android\android-sdk",
    [string]$JavaSdkDirectory = "C:\Program Files\Android\openjdk\jdk-21.0.8",
    [string]$AvdName = "Pixel_8_API_35",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$PackageId = "vn.motorcycledealer.mobile",
    [int]$ApiPort = 5080,
    [string]$ScreenshotDir = "",
    [int]$LaunchWaitSeconds = 5,
    [switch]$SkipBuild,
    [switch]$Headless,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$AppCsproj = Join-Path $RepoRoot "mobile\src\MotorcycleDealer.Mobile.App\MotorcycleDealer.Mobile.App.csproj"
$EnsureScript = Join-Path $PSScriptRoot "ensure-maui-android.ps1"
$AdbExe = Join-Path $AndroidSdkDirectory "platform-tools\adb.exe"

if ([string]::IsNullOrWhiteSpace($ScreenshotDir)) {
    $ScreenshotDir = Join-Path ([System.IO.Path]::GetTempPath()) "motorcycle-dealer-mobile-smoke"
}

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-FailMsg([string]$Msg) { Write-Host "[FAIL] $Msg" -ForegroundColor Red }

function Invoke-NativeCapture {
    param(
        [string]$FilePath,
        [string[]]$Arguments = @()
    )
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& $FilePath @Arguments 2>&1)
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

function Capture-AdbBinaryStdout {
    param(
        [string]$AdbPath,
        [string[]]$Arguments,
        [string]$OutputFile
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $AdbPath
    $psi.Arguments = ($Arguments -join " ")
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    $proc.Start() | Out-Null

    $fs = [System.IO.File]::Create($OutputFile)
    try {
        $proc.StandardOutput.BaseStream.CopyTo($fs)
    } finally {
        $fs.Dispose()
    }
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    return @{
        ExitCode = $proc.ExitCode
        Stderr   = $stderr
    }
}

Write-Info "=== .NET 10 MAUI Android Smoke Runner (Config=$Configuration, AVD=$AvdName, Package=$PackageId) ==="

if (-not (Test-Path $AppCsproj)) {
    Write-FailMsg "MAUI Android project not found at '$AppCsproj'."
    exit 1
}
if (-not (Test-Path $AdbExe)) {
    Write-FailMsg "adb.exe not found at '$AdbExe'."
    exit 1
}

if ($DryRun) {
    Write-Info "[DRY-RUN] 1) dotnet build '$AppCsproj' -c $Configuration -f net10.0-android -p:AndroidSdkDirectory='$AndroidSdkDirectory' -p:JavaSdkDirectory='$JavaSdkDirectory' -p:EmbedAssembliesIntoApk=true --warnaserror"
    Write-Info "[DRY-RUN] 2) ensure-maui-android.ps1 -AvdName '$AvdName' -ApiPort $ApiPort -BootEmulator -SetupAdbReverse"
    Write-Info "[DRY-RUN] 3) adb install -r <signed-apk> && adb shell pm grant $PackageId android.permission.POST_NOTIFICATIONS && am start && pidof $PackageId && uiautomator dump && adb exec-out screencap -p -> '$ScreenshotDir\app-launch.png'"
    Write-Pass "Android smoke runner dry-run validated cleanly."
    exit 0
}

# 1. Build Android APK
if (-not $SkipBuild) {
    Write-Info "[Step 1/6] Building MotorcycleDealer.Mobile.App ($Configuration, net10.0-android, EmbedAssembliesIntoApk=true)..."
    & dotnet build $AppCsproj `
        -c $Configuration `
        -f net10.0-android `
        "-p:AndroidSdkDirectory=$AndroidSdkDirectory" `
        "-p:JavaSdkDirectory=$JavaSdkDirectory" `
        "-p:EmbedAssembliesIntoApk=true" `
        --warnaserror `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-FailMsg "dotnet build ($Configuration, net10.0-android) failed with exit code $LASTEXITCODE."
        exit 1
    }
    Write-Pass "Built MotorcycleDealer.Mobile.App ($Configuration, EmbedAssembliesIntoApk=true, 0 warnings)."
} else {
    Write-Info "[Step 1/6] Skipping build (-SkipBuild)."
}

# Locate APK
$BinOutDir = Join-Path $RepoRoot "mobile\src\MotorcycleDealer.Mobile.App\bin\$Configuration\net10.0-android"
$ApkFile = Get-ChildItem -Path $BinOutDir -Recurse -File -Filter "*-Signed.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
if ($null -eq $ApkFile) {
    $ApkFile = Get-ChildItem -Path $BinOutDir -Recurse -File -Filter "*.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}
if ($null -eq $ApkFile) {
    Write-FailMsg "No compiled .apk found under '$BinOutDir'."
    exit 1
}
Write-Pass "Located Android APK: $($ApkFile.FullName) ($([Math]::Round($ApkFile.Length / 1MB, 2)) MB)"

# 2. Ensure Emulator Booted & ADB Reverse Configured
Write-Info "[Step 2/6] Ensuring emulator '$AvdName' is booted & adb reverse tcp:$ApiPort is active..."
$ensureArgs = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $EnsureScript,
    "-AndroidSdkDirectory", $AndroidSdkDirectory,
    "-JavaSdkDirectory", $JavaSdkDirectory,
    "-AvdName", $AvdName,
    "-ApiPort", "$ApiPort",
    "-BootEmulator",
    "-SetupAdbReverse"
)
if ($Headless) { $ensureArgs += "-Headless" }
& powershell @ensureArgs
if ($LASTEXITCODE -ne 0) {
    Write-FailMsg "ensure-maui-android.ps1 failed with exit code $LASTEXITCODE."
    exit 1
}

# Resolve online device serial
$devRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("devices")
$Serial = $null
foreach ($line in $devRes.Lines) {
    if ($line -match '^(\S+)\s+device$') {
        $Serial = $Matches[1]
        break
    }
}
if ([string]::IsNullOrWhiteSpace($Serial)) {
    Write-FailMsg "No online Android device/emulator found after ensure-maui-android.ps1."
    exit 1
}

# 3. Install APK & Pre-Grant Runtime Notification Permission (Android 13+ API 33+)
Write-Info "[Step 3/6] Installing APK onto $Serial..."
$installRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "install", "-r", $ApkFile.FullName)
if ($installRes.ExitCode -ne 0 -or $installRes.Text -notmatch "Success") {
    Write-FailMsg "adb install -r failed: $($installRes.Text)"
    exit 1
}
Write-Pass "Installed $PackageId onto $Serial."

$grantRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "pm", "grant", $PackageId, "android.permission.POST_NOTIFICATIONS")
if ($grantRes.ExitCode -eq 0) {
    Write-Pass "Granted android.permission.POST_NOTIFICATIONS to $PackageId (prevents Android 13+ permission modal blocking smoke)."
} else {
    Write-Info "POST_NOTIFICATIONS runtime grant skipped/unavailable on device API level: $($grantRes.Text)"
}

# 4. Launch MainActivity & Verify Process Alive
Write-Info "[Step 4/6] Resolving & launching main activity for $PackageId..."
Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "logcat", "-c") | Out-Null
Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "am", "force-stop", $PackageId) | Out-Null

$resolveRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "cmd", "package", "resolve-activity", "--brief", $PackageId)
$activityComponent = ($resolveRes.Lines | ForEach-Object { $_.Trim() } | Where-Object { $_ -match "^$([Regex]::Escape($PackageId))/.+" } | Select-Object -Last 1)

if ($activityComponent) {
    $startRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "am", "start", "-W", "-n", $activityComponent)
    if ($startRes.ExitCode -ne 0 -or $startRes.Text -match "(?i)Error:") {
        Write-FailMsg "am start -W -n $activityComponent failed: $($startRes.Text)"
        exit 1
    }
    Write-Pass "Launched activity: $activityComponent"
} else {
    Write-Info "Falling back to monkey launcher intent for $PackageId..."
    $monkeyRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "monkey", "-p", $PackageId, "-c", "android.intent.category.LAUNCHER", "1")
    if ($monkeyRes.ExitCode -ne 0) {
        Write-FailMsg "Failed to launch $PackageId via monkey: $($monkeyRes.Text)"
        exit 1
    }
    Write-Pass "Launched $PackageId via LAUNCHER intent."
}

Start-Sleep -Seconds $LaunchWaitSeconds

$appPid = ""
for ($attempt = 1; $attempt -le 5; $attempt++) {
    $pidRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "pidof", $PackageId)
    $candidatePid = $pidRes.Text.Trim()
    if ($candidatePid -match '^\d+') {
        $appPid = $candidatePid
        break
    }
    Start-Sleep -Seconds 1
}

if ([string]::IsNullOrWhiteSpace($appPid)) {
    $crashLog = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "logcat", "-d", "-t", "120", "AndroidRuntime:E", "monodroid:E", "mono-stderr:E", "DOTNET:E", "DEBUG:F", "mono-stdout:I", "*:S")
    Write-FailMsg "Process '$PackageId' is NOT alive after launch! Recent crash logcat:`n$($crashLog.Text)"
    exit 1
}
Write-Pass "Process '$PackageId' verified alive (PID=$appPid)."

# 5. Dump Live UI Tree via uiautomator dump
New-Item -ItemType Directory -Path $ScreenshotDir -Force | Out-Null
$UiDumpLocal = Join-Path $ScreenshotDir "window_dump.xml"
Write-Info "[Step 5/6] Dumping live Android UI hierarchy via uiautomator..."
$dumpRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "shell", "uiautomator", "dump", "/sdcard/window_dump.xml")
if ($dumpRes.ExitCode -ne 0 -or $dumpRes.Text -notmatch "dumped to") {
    Write-FailMsg "uiautomator dump failed: $($dumpRes.Text)"
    exit 1
}
$pullRes = Invoke-NativeCapture -FilePath $AdbExe -Arguments @("-s", $Serial, "pull", "/sdcard/window_dump.xml", $UiDumpLocal)
if ($pullRes.ExitCode -ne 0 -or -not (Test-Path $UiDumpLocal)) {
    Write-FailMsg "Failed to pull /sdcard/window_dump.xml: $($pullRes.Text)"
    exit 1
}
$uiDumpLen = (Get-Item -LiteralPath $UiDumpLocal).Length
if ($uiDumpLen -lt 128) {
    Write-FailMsg "Pulled window_dump.xml is unexpectedly small ($uiDumpLen bytes)."
    exit 1
}
Write-Pass "UI hierarchy captured: $UiDumpLocal ($uiDumpLen bytes)."

# 6. Capture Binary PNG Screenshot via adb exec-out screencap -p
$ScreenshotPng = Join-Path $ScreenshotDir "app-launch.png"
Write-Info "[Step 6/6] Capturing live Android screenshot PNG via adb exec-out screencap -p..."
$capRes = Capture-AdbBinaryStdout -AdbPath $AdbExe -Arguments @("-s", $Serial, "exec-out", "screencap", "-p") -OutputFile $ScreenshotPng
if ($capRes.ExitCode -ne 0 -or -not (Test-Path $ScreenshotPng)) {
    Write-FailMsg "adb exec-out screencap -p failed (exit=$($capRes.ExitCode)): $($capRes.Stderr)"
    exit 1
}
$pngBytes = [System.IO.File]::ReadAllBytes($ScreenshotPng)
$validPngHeader = ($pngBytes.Length -gt 1024) -and ($pngBytes[0] -eq 0x89) -and ($pngBytes[1] -eq 0x50) -and ($pngBytes[2] -eq 0x4E) -and ($pngBytes[3] -eq 0x47)
if (-not $validPngHeader) {
    Write-FailMsg "Captured screenshot '$ScreenshotPng' failed PNG magic-header/size validation ($($pngBytes.Length) bytes)."
    exit 1
}
Write-Pass "Screenshot PNG captured & verified: $ScreenshotPng ($($pngBytes.Length) bytes)."

Write-Host ""
Write-Pass "=== Android MAUI Smoke PASSED (PID=$appPid, Serial=$Serial, APK=$($ApkFile.Name), Screenshot=$ScreenshotPng) ==="
exit 0
