<#
.SYNOPSIS
    Audits Phase-Gate boundaries, Clean Architecture inward dependencies, PointLedgerEntries immutability,
    AGENTS.md size/frontmatter rules, and zero-secrets hygiene for the Motorcycle Dealer & Loyalty Platform.
#>
[CmdletBinding()]
param(
    [ValidateRange(0, 10)]
    [int]$Phase = 0,

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$Failures = New-Object System.Collections.Generic.List[string]
$Warnings = New-Object System.Collections.Generic.List[string]

function Write-Pass([string]$Message) { Write-Host "[PASS] $Message" -ForegroundColor Green }
function Write-Info([string]$Message) { Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-WarnMsg([string]$Message) {
    $Warnings.Add($Message) | Out-Null
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}
function Write-FailMsg([string]$Message) {
    $Failures.Add($Message) | Out-Null
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

# 1. Resolve Active Phase
$EffectivePhase = $Phase
if ($EffectivePhase -eq 0) {
    $ProgressPath = Join-Path $RepoRoot "PROGRESS.md"
    if (Test-Path $ProgressPath) {
        $ProgressRaw = Get-Content -Path $ProgressPath -Raw -Encoding UTF8
        if ($ProgressRaw -match '\*\*Current Active Phase\*\*\s*\|\s*\*\*Phase\s+([1-9][0-9]*)') {
            $EffectivePhase = [int]$Matches[1]
        }
    }
    if ($EffectivePhase -eq 0) {
        if (Test-Path (Join-Path $RepoRoot "backend")) {
            $EffectivePhase = 3
        } elseif (Test-Path (Join-Path $RepoRoot ".agents")) {
            $EffectivePhase = 2
        } else {
            $EffectivePhase = 1
        }
    }
}

$ModeLabel = if ($DryRun) { "DRY-RUN" } else { "ENFORCE" }
Write-Info "Running Dealer Platform Guardrails Audit (EffectivePhase=$EffectivePhase, Mode=$ModeLabel, Root=$RepoRoot)"

# 2. Audit AGENTS.md Rule Files (per-file byte caps & Zero YAML Frontmatter)
$RuleCandidates = @(
    @{ Path = (Join-Path $RepoRoot "AGENTS.md"); Required = $true; Label = "AGENTS.md"; MaxBytes = 24000 },
    @{ Path = (Join-Path $RepoRoot ".agents\rules\AGENTS.md"); Required = $false; Label = ".agents/rules/AGENTS.md"; MaxBytes = 12000 },
    @{ Path = (Join-Path $RepoRoot ".agents\plugins\motorcycle-dealer-suite\rules\AGENTS.md"); Required = $false; Label = ".agents/plugins/motorcycle-dealer-suite/rules/AGENTS.md"; MaxBytes = 5000 },
    @{ Path = (Join-Path $RepoRoot "backend\AGENTS.md"); Required = $false; Label = "backend/AGENTS.md"; MaxBytes = 8000 },
    @{ Path = (Join-Path $RepoRoot "frontend\AGENTS.md"); Required = $false; Label = "frontend/AGENTS.md"; MaxBytes = 6000 },
    @{ Path = (Join-Path $RepoRoot "mobile\AGENTS.md"); Required = $false; Label = "mobile/AGENTS.md"; MaxBytes = 8000 }
)

foreach ($Entry in $RuleCandidates) {
    $FilePath = $Entry.Path
    $Label = $Entry.Label
    $MaxBytes = $Entry.MaxBytes
    if (-not (Test-Path $FilePath)) {
        if ($Entry.Required) {
            Write-FailMsg "Required governance file missing: $Label"
        } else {
            Write-Info "Optional/peer rule file not present yet: $Label"
        }
        continue
    }

    $Item = Get-Item -LiteralPath $FilePath
    if ($Item.Length -ge $MaxBytes) {
        Write-FailMsg "$Label exceeds $MaxBytes-byte Antigravity rule limit ($($Item.Length) bytes)."
    } else {
        Write-Pass "$Label byte size valid ($($Item.Length) < $MaxBytes bytes)."
    }

    $FirstLine = Get-Content -LiteralPath $FilePath -TotalCount 1 -Encoding UTF8
    if ($null -ne $FirstLine -and $FirstLine.Trim() -eq "---") {
        Write-FailMsg "$Label starts with YAML frontmatter ('---'), which is prohibited for standalone AGENTS.md rules."
    } else {
        Write-Pass "$Label has zero YAML frontmatter."
    }
}

# 3. Audit Phase-Gate Boundaries & Clean Architecture Dependencies
$BackendDir = Join-Path $RepoRoot "backend"
$FrontendDir = Join-Path $RepoRoot "frontend"
$RootNodeModules = Join-Path $RepoRoot "node_modules"
$RootPkgJson = Join-Path $RepoRoot "package.json"

if (Test-Path $RootNodeModules) {
    Write-FailMsg "Prohibited root node_modules/ directory detected."
} else {
    Write-Pass "Zero root node_modules/ pollution."
}

if (Test-Path $RootPkgJson) {
    Write-FailMsg "Prohibited root package.json detected (frontend dependencies belong inside frontend/ in Phase 3+)."
} else {
    Write-Pass "Zero root package.json pollution."
}

if ($EffectivePhase -le 2) {
    if (Test-Path $BackendDir) {
        Write-FailMsg "Phase-Gate Violation (Phase $EffectivePhase <= 2): premature 'backend/' directory exists before Phase 3 sign-off."
    } else {
        Write-Pass "Phase-Gate verified: zero premature 'backend/' directory in Phase $EffectivePhase."
    }

    if (Test-Path $FrontendDir) {
        Write-FailMsg "Phase-Gate Violation (Phase $EffectivePhase <= 2): premature 'frontend/' directory exists before Phase 3 sign-off."
    } else {
        Write-Pass "Phase-Gate verified: zero premature 'frontend/' directory in Phase $EffectivePhase."
    }

    $PrematureCode = Get-ChildItem -Path $RepoRoot -Recurse -File -Include "*.sln", "*.csproj", "*.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' }
    if ($PrematureCode -and @($PrematureCode).Count -gt 0) {
        Write-FailMsg "Phase-Gate Violation: found $(@($PrematureCode).Count) premature C#/.NET project file(s) in Phase $EffectivePhase."
    } else {
        Write-Pass "Phase-Gate verified: zero premature .sln/.csproj/.cs files in Phase $EffectivePhase."
    }
}

if (Test-Path $BackendDir) {
    Write-Info "Auditing Clean Architecture inward dependencies (ADR-001) and 4-table financial/audit immutability (ADR-003)..."

    $DomainCsproj = Join-Path $BackendDir "src\MotorcycleDealer.Domain\MotorcycleDealer.Domain.csproj"
    if (Test-Path $DomainCsproj) {
        $DomainXml = Get-Content -LiteralPath $DomainCsproj -Raw -Encoding UTF8
        if ($DomainXml -match '<ProjectReference') {
            Write-FailMsg "ADR-001 Violation: MotorcycleDealer.Domain.csproj must have zero ProjectReferences."
        } elseif ($DomainXml -match 'Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore') {
            Write-FailMsg "ADR-001 Violation: MotorcycleDealer.Domain.csproj references EF Core or ASP.NET Core."
        } else {
            Write-Pass "ADR-001: MotorcycleDealer.Domain.csproj has zero outward project/framework dependencies."
        }
    }

    $AppCsproj = Join-Path $BackendDir "src\MotorcycleDealer.Application\MotorcycleDealer.Application.csproj"
    if (Test-Path $AppCsproj) {
        $AppXml = Get-Content -LiteralPath $AppCsproj -Raw -Encoding UTF8
        if ($AppXml -match 'MotorcycleDealer\.(Infrastructure|WebApi)') {
            Write-FailMsg "ADR-001 Violation: MotorcycleDealer.Application.csproj references Infrastructure or WebApi."
        } elseif ($AppXml -match 'Microsoft\.EntityFrameworkCore\.SqlServer|Microsoft\.AspNetCore\.App') {
            Write-FailMsg "ADR-001 Violation: MotorcycleDealer.Application.csproj references SQL Server or ASP.NET host packages."
        } else {
            Write-Pass "ADR-001: MotorcycleDealer.Application.csproj inward dependency rule verified."
        }
    }

    $BackendSrcDir = Join-Path $BackendDir "src"
    if (Test-Path $BackendSrcDir) {
        $CsFiles = Get-ChildItem -Path $BackendSrcDir -Recurse -File -Include "*.cs", "*.sql" -ErrorAction SilentlyContinue
        $LedgerViolations = New-Object System.Collections.Generic.List[string]
        foreach ($CsFile in $CsFiles) {
            $Content = Get-Content -LiteralPath $CsFile.FullName -Raw -Encoding UTF8
            $HasRawMutation = $Content -match '(?i)\b(DELETE\s+FROM|TRUNCATE\s+TABLE)\s+(\[?dbo\]?\.)?\[?(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)\]?' -or
                ($Content -match '(?i)\bUPDATE\s+(\[?dbo\]?\.)?\[?(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)\]?\s+SET\b')
            $HasEfMutation = $Content -match '(PointLedgerEntries|SalesOrders|ServiceInvoices|ServiceInvoiceLineItems)\s*\.\s*(Remove|RemoveRange|Update|UpdateRange|ExecuteDelete|ExecuteDeleteAsync|ExecuteUpdate|ExecuteUpdateAsync)\s*\('
            if ($HasRawMutation -or $HasEfMutation) {
                $LedgerViolations.Add($CsFile.FullName) | Out-Null
            }
        }
        if ($LedgerViolations.Count -gt 0) {
            Write-FailMsg "ADR-003 Immutability Violation: PointLedgerEntries/SalesOrders/ServiceInvoices/ServiceInvoiceLineItems DELETE/UPDATE detected in: $($LedgerViolations -join ', ')"
        } else {
            Write-Pass "ADR-003: Zero PointLedgerEntries, SalesOrders, ServiceInvoices & ServiceInvoiceLineItems UPDATE/DELETE mutations across backend/src."
        }
    }
}

$MobileCoreDir = Join-Path $RepoRoot "mobile\src\MotorcycleDealer.Mobile.Core"
if (Test-Path $MobileCoreDir) {
    $MobileCoreCsprojs = @(Get-ChildItem -Path $MobileCoreDir -File -Filter "*.csproj" -ErrorAction SilentlyContinue)
    $MauiCoreHits = New-Object System.Collections.Generic.List[string]
    foreach ($McProj in $MobileCoreCsprojs) {
        $McXml = Get-Content -LiteralPath $McProj.FullName -Raw -Encoding UTF8
        if ($McXml -match '(?i)<UseMaui>\s*true\s*</UseMaui>' -or $McXml -match '(?i)Microsoft\.Maui\.') {
            $MauiCoreHits.Add($McProj.Name) | Out-Null
        }
    }
    if ($MauiCoreHits.Count -gt 0) {
        Write-FailMsg "Phase 10 Mobile Purity Violation: MotorcycleDealer.Mobile.Core must never set <UseMaui>true</UseMaui> or reference Microsoft.Maui.*: $($MauiCoreHits -join ', ')"
    } else {
        Write-Pass "Phase 10 Mobile Purity: MotorcycleDealer.Mobile.Core (*.csproj) has zero <UseMaui>true</UseMaui> or Microsoft.Maui.* references."
    }
}

# 4. Secret & Credential Hygiene Scan
$ForbiddenFiles = Get-ChildItem -Path $RepoRoot -Recurse -Force -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notmatch '[\\/](?:\.git|bin|obj|node_modules)[\\/]' -and
        (
            ($_.Name -match '^\.env(\..+)?$' -and $_.Name -ne '.env.example') -or
            ($_.Extension -in @('.pfx', '.pem', '.key', '.p12'))
        )
    }

if ($ForbiddenFiles -and @($ForbiddenFiles).Count -gt 0) {
    $Names = @($ForbiddenFiles | ForEach-Object { $_.FullName }) -join ", "
    Write-FailMsg "Secret Hygiene Violation: committed secret/certificate file(s) found: $Names"
} else {
    Write-Pass "Secret Hygiene: zero .env / private certificate files in workspace."
}

$ScannableFiles = Get-ChildItem -Path $RepoRoot -Recurse -File -Include "*.json", "*.cs", "*.js", "*.mjs", "*.ps1", "*.sh", "*.yml", "*.yaml", "*.xml" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notmatch '[\\/](?:\.git|bin|obj|node_modules)[\\/]' -and
        $_.Name -notin @("audit-dealer-guardrails.ps1", "audit-dealer-guardrails.sh", "safety-guard.mjs")
    }

$SecretHits = New-Object System.Collections.Generic.List[string]
foreach ($ScanFile in $ScannableFiles) {
    $Text = Get-Content -LiteralPath $ScanFile.FullName -Raw -Encoding UTF8
    if ($null -eq $Text) { continue }

    if ($Text -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' -or
        $Text -match '\bAKIA[0-9A-Z]{16}\b' -or
        $Text -match '\bghp_[A-Za-z0-9]{36}\b') {
        $SecretHits.Add($ScanFile.Name) | Out-Null
        continue
    }

    if ($ScanFile.Name -like "appsettings*.json") {
        if ($Text -match '"SigningKey"\s*:\s*"(?!REPLACE_|DEV_PLACEHOLDER|SET_VIA_USER_SECRETS|<)[^"]{8,}"') {
            $SecretHits.Add("$($ScanFile.Name) (hardcoded Jwt:SigningKey)") | Out-Null
        }
        if ($Text -match '(?i)(?:Pwd|Password)\s*=\s*(?!<|REPLACE_|\$\{)[^;"\s]{4,}') {
            $SecretHits.Add("$($ScanFile.Name) (hardcoded SQL Password)") | Out-Null
        }
    }
}

if ($SecretHits.Count -gt 0) {
    Write-FailMsg "Secret Hygiene Violation: sensitive token/key pattern detected in: $($SecretHits -join ', ')"
} else {
    Write-Pass "Secret Hygiene: zero hardcoded private keys, PATs, or production connection/JWT secrets."
}

# 5. Summary
if ($Failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Guardrails Audit FAILED with $($Failures.Count) violation(s)." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Guardrails Audit PASSED ($($Warnings.Count) warning(s), Mode=$ModeLabel, Phase=$EffectivePhase)." -ForegroundColor Green
exit 0
