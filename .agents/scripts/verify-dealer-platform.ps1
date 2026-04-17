<#
.SYNOPSIS
    Master verification orchestrator for the Motorcycle Dealer & Loyalty Management Platform:
    1) Validates all .agents/**/*.json files parse as valid JSON & plugin structure rules
    2) Validates all .agents/skills/*/SKILL.md files contain valid YAML frontmatter (name & description)
    3) Runs audit-dealer-guardrails.ps1
    4) Runs node .agents/scripts/sync-progress.mjs --check
    5) Runs node .agents/scripts/verify-post-task-sync.mjs (100% docs, code/snapshot & 183-day git schedule sync gate)
    6) Tests MCP JSON-RPC 2.0 initialize + tools/list handshake on workspace Stdio MCP servers
    7) Runs dotnet build & dotnet test once backend/MotorcycleDealer.sln exists (Phase 3+)
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [ValidateRange(0, 10)]
    [int]$Phase = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$AgentsDir = Join-Path $RepoRoot ".agents"
$SkillsDir = Join-Path $AgentsDir "skills"
$AuditScript = Join-Path $PSScriptRoot "audit-dealer-guardrails.ps1"
$SyncScript = Join-Path $PSScriptRoot "sync-progress.mjs"
$PostTaskSyncScript = Join-Path $PSScriptRoot "verify-post-task-sync.mjs"
$MobileVerifyScript = Join-Path $PSScriptRoot "verify-mobile-maui.mjs"
$SlnPath = Join-Path $RepoRoot "backend\MotorcycleDealer.sln"
$MobileSlnPath = Join-Path $RepoRoot "mobile\MotorcycleDealer.Mobile.sln"
$MobileUnitTestDir = Join-Path $RepoRoot "mobile\tests\MotorcycleDealer.Mobile.UnitTests"

$Failures = New-Object System.Collections.Generic.List[string]

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-WarnMsg([string]$Msg) { Write-Host "[WARN] $Msg" -ForegroundColor Yellow }
function Write-FailMsg([string]$Msg) {
    $Failures.Add($Msg) | Out-Null
    Write-Host "[FAIL] $Msg" -ForegroundColor Red
}

$ModeLabel = if ($DryRun) { "DRY-RUN" } else { "FULL" }
Write-Info "=== Motorcycle Dealer & Loyalty Platform Master Verifier (Mode=$ModeLabel) ==="

# Stage 1: Validate all .agents/**/*.json manifests
Write-Info "[Stage 1/7] Validating .agents/**/*.json manifests..."
$JsonFiles = @(Get-ChildItem -Path $AgentsDir -Recurse -File -Filter "*.json" -ErrorAction SilentlyContinue)
if ($JsonFiles.Count -eq 0) {
    Write-WarnMsg "No .json files currently found under .agents/."
} else {
    $ValidJsonCount = 0
    foreach ($Jf in $JsonFiles) {
        try {
            $Parsed = Get-Content -LiteralPath $Jf.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($null -eq $Parsed) {
                Write-FailMsg "Empty JSON payload in $($Jf.FullName)"
            } else {
                $ValidJsonCount++
            }
        } catch {
            Write-FailMsg "Invalid JSON syntax in $($Jf.FullName): $($_.Exception.Message)"
        }
    }
    if ($ValidJsonCount -eq $JsonFiles.Count) {
        Write-Pass "Parsed $ValidJsonCount / $($JsonFiles.Count) JSON manifest(s) under .agents/ cleanly."
    }
}

$DupPluginSkillsDir = Join-Path $AgentsDir "plugins\motorcycle-dealer-suite\skills"
if (Test-Path $DupPluginSkillsDir) {
    Write-FailMsg "Duplicate skills/ directory found inside .agents/plugins/motorcycle-dealer-suite/skills (skills must reside solely in .agents/skills/)."
}

# Stage 2: Validate .agents/skills/*/SKILL.md YAML Frontmatter
Write-Info "[Stage 2/7] Validating .agents/skills/*/SKILL.md YAML frontmatter..."
if (Test-Path $SkillsDir) {
    $SkillFiles = @(Get-ChildItem -Path $SkillsDir -Recurse -File -Filter "SKILL.md" -ErrorAction SilentlyContinue)
    if ($SkillFiles.Count -eq 0) {
        Write-WarnMsg "No SKILL.md files found under .agents/skills/ yet."
    } else {
        $ValidSkillCount = 0
        foreach ($Sf in $SkillFiles) {
            $FolderName = $Sf.Directory.Name
            $RawSkill = Get-Content -LiteralPath $Sf.FullName -Raw -Encoding UTF8
            if ($RawSkill -notmatch '(?s)\A---\r?\n(.*?)\r?\n---') {
                Write-FailMsg "Missing YAML frontmatter block in .agents/skills/$FolderName/SKILL.md"
                continue
            }
            $Frontmatter = $Matches[1]
            $HasName = $Frontmatter -match '(?m)^name:\s*["'']?([a-z0-9-]+)["'']?\s*$'
            $ParsedName = if ($HasName) { $Matches[1] } else { "" }
            $HasDesc = $Frontmatter -match '(?m)^description:\s*\S+'
            if (-not $HasName) {
                Write-FailMsg "Missing 'name:' field in .agents/skills/$FolderName/SKILL.md frontmatter."
            } elseif ($ParsedName -ne $FolderName) {
                Write-FailMsg "Skill frontmatter name '$ParsedName' does not match folder '$FolderName'."
            } elseif (-not $HasDesc) {
                Write-FailMsg "Missing 'description:' field in .agents/skills/$FolderName/SKILL.md frontmatter."
            } else {
                $ValidSkillCount++
            }
        }
        if ($ValidSkillCount -eq $SkillFiles.Count) {
            Write-Pass "Validated YAML frontmatter (name + description) across $ValidSkillCount SKILL.md file(s)."
        }
    }
} else {
    Write-Info ".agents/skills/ directory not populated yet."
}

# Stage 3: Run audit-dealer-guardrails.ps1
Write-Info "[Stage 3/7] Executing audit-dealer-guardrails.ps1..."
$AuditArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $AuditScript)
if ($Phase -gt 0) { $AuditArgs += @("-Phase", "$Phase") }
if ($DryRun) { $AuditArgs += "-DryRun" }
& powershell @AuditArgs
if ($LASTEXITCODE -ne 0) {
    Write-FailMsg "audit-dealer-guardrails.ps1 exited with code $LASTEXITCODE."
} else {
    Write-Pass "audit-dealer-guardrails.ps1 passed."
}

# Stage 4: Run sync-progress.mjs --check
Write-Info "[Stage 4/7] Checking TASKS.md <-> PROGRESS.md synchronization..."
& node $SyncScript --check
if ($LASTEXITCODE -ne 0) {
    Write-FailMsg "sync-progress.mjs --check reported mismatches."
} else {
    Write-Pass "sync-progress.mjs --check passed."
}

# Stage 5: Run verify-post-task-sync.mjs
Write-Info "[Stage 5/7] Executing Post-Task Docs, Code/Snapshot & 183-Day Git Schedule Sync Gate..."
$PostSyncArgs = @($PostTaskSyncScript)
if ($DryRun) { $PostSyncArgs += "--dry-run" }
& node @PostSyncArgs
if ($LASTEXITCODE -ne 0) {
    Write-FailMsg "verify-post-task-sync.mjs reported synchronization drift."
} else {
    Write-Pass "verify-post-task-sync.mjs passed."
}

# Stage 6: Test MCP JSON-RPC 2.0 initialize + tools/list Handshake
Write-Info "[Stage 6/7] Testing workspace Stdio MCP servers (initialize + tools/list)..."
$McpServers = @(
    Join-Path $PSScriptRoot "mcp-dealer-guardrails-server.mjs"
    Join-Path $PSScriptRoot "mcp-sqlserver-localdb-server.mjs"
)

$McpProbeJs = @'
const { spawn } = require("node:child_process");
const serverPath = process.env.MCP_TARGET_SERVER;
const child = spawn(process.execPath, [serverPath], { stdio: ["pipe", "pipe", "pipe"] });
let buf = "";
const responses = new Map();
const timer = setTimeout(() => {
  console.error("Timed out waiting for MCP JSON-RPC handshake from " + serverPath);
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
    if (msg.id !== undefined) responses.set(msg.id, msg);
    if (responses.has(1) && responses.has(2)) {
      clearTimeout(timer);
      const initRes = responses.get(1);
      const listRes = responses.get(2);
      if (initRes.error || !initRes.result || !initRes.result.capabilities) {
        console.error("Invalid initialize response", JSON.stringify(initRes));
        child.kill();
        process.exit(1);
      }
      const tools = listRes.result && Array.isArray(listRes.result.tools) ? listRes.result.tools : null;
      if (listRes.error || !tools || tools.length === 0) {
        console.error("Invalid tools/list response", JSON.stringify(listRes));
        child.kill();
        process.exit(1);
      }
      const sName = (initRes.result.serverInfo && initRes.result.serverInfo.name) ? initRes.result.serverInfo.name : "mcp";
      console.log(sName + " (" + tools.length + " tools: " + tools.map((t) => t.name).join(", ") + ")");
      child.stdin.end();
      child.kill();
      process.exit(0);
    }
  }
});

child.stdin.write(JSON.stringify({
  jsonrpc: "2.0",
  id: 1,
  method: "initialize",
  params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "verify-dealer-platform", version: "1.0.0" } }
}) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
child.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: 2, method: "tools/list", params: {} }) + "\n");
'@

$EncodedProbe = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($McpProbeJs))

foreach ($McpPath in $McpServers) {
    $ServerName = [System.IO.Path]::GetFileName($McpPath)
    if (-not (Test-Path $McpPath)) {
        Write-Info "MCP server '$ServerName' not present on disk yet; skipping handshake."
        continue
    }
    $env:MCP_TARGET_SERVER = $McpPath
    $PrevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $HandshakeOut = & node -e "eval(Buffer.from('$EncodedProbe','base64').toString('utf8'))" 2>&1 | Out-String
    $ProbeExit = $LASTEXITCODE
    $ErrorActionPreference = $PrevEap
    Remove-Item Env:\MCP_TARGET_SERVER -ErrorAction SilentlyContinue

    if ($ProbeExit -ne 0) {
        Write-FailMsg "MCP handshake failed for ${ServerName}: $($HandshakeOut.Trim())"
    } else {
        Write-Pass "MCP JSON-RPC handshake verified: $($HandshakeOut.Trim())"
    }
}

# Stage 7: Build & Test Solution (Phase 3+)
Write-Info "[Stage 7/7] Checking .NET 10 solution build & test gate..."
if (Test-Path $SlnPath) {
    if ($DryRun) {
        Write-Info "[DRY-RUN] Solution detected at backend/MotorcycleDealer.sln; would run 'dotnet build --warnaserror' and 'dotnet test'."
    } else {
        $VerifyArtifactsDir = $null
        $VerifyExtraArgs = @()
        if (@(Get-Process -Name "MotorcycleDealer.WebApi" -ErrorAction SilentlyContinue).Count -gt 0) {
            $VerifyArtifactsDir = Join-Path $RepoRoot ("backend\obj\vdp-artifacts-" + [Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path $VerifyArtifactsDir -Force | Out-Null
            $VerifyExtraArgs = @("-p:UseArtifactsOutput=true", "-p:ArtifactsPath=$VerifyArtifactsDir")
            Write-Info "Live MotorcycleDealer.WebApi process detected; isolating Stage 7 backend build/test into '$VerifyArtifactsDir'."
        }
        try {
            & dotnet build $SlnPath --warnaserror @VerifyExtraArgs
            if ($LASTEXITCODE -ne 0) {
                Write-FailMsg "dotnet build --warnaserror failed."
            } else {
                & dotnet test $SlnPath --no-build @VerifyExtraArgs
                if ($LASTEXITCODE -ne 0) {
                    Write-FailMsg "dotnet test failed."
                } else {
                    Write-Pass "dotnet build --warnaserror and dotnet test passed."
                }
            }
        } finally {
            if ($VerifyArtifactsDir -and (Test-Path $VerifyArtifactsDir)) {
                Remove-Item -LiteralPath $VerifyArtifactsDir -Recurse -Force -ErrorAction SilentlyContinue
                $BackendObjParent = Join-Path $RepoRoot "backend\obj"
                if ((Test-Path $BackendObjParent) -and @(Get-ChildItem -LiteralPath $BackendObjParent -Force -ErrorAction SilentlyContinue).Count -eq 0) {
                    Remove-Item -LiteralPath $BackendObjParent -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
} else {
    Write-Pass "Phase <= 2 No-Code Gate active (backend/MotorcycleDealer.sln absent as expected)."
}

if (Test-Path $MobileSlnPath) {
    Write-Info "[Stage 7b/7] Checking .NET 10 MAUI Android static guardrails & Mobile.UnitTests..."
    if (Test-Path $MobileVerifyScript) {
        & node $MobileVerifyScript
        if ($LASTEXITCODE -ne 0) {
            Write-FailMsg "verify-mobile-maui.mjs reported MAUI guardrail failure(s)."
        } else {
            Write-Pass "verify-mobile-maui.mjs passed."
        }
    }
    if ($DryRun) {
        Write-Info "[DRY-RUN] Mobile solution detected at mobile/MotorcycleDealer.Mobile.sln; would run 'dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests -c Release --warnaserror'."
    } elseif (Test-Path $MobileUnitTestDir) {
        & dotnet test $MobileUnitTestDir -c Release --warnaserror --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-FailMsg "dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests failed."
        } else {
            Write-Pass "dotnet test mobile/tests/MotorcycleDealer.Mobile.UnitTests passed."
        }
    }
}

if ($Failures.Count -gt 0) {
    Write-Host ""
    Write-Host "=== Verification FAILED ($($Failures.Count) failure(s)) ===" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== All Dealer Platform Verification Gates PASSED (Mode=$ModeLabel) ===" -ForegroundColor Green
exit 0
