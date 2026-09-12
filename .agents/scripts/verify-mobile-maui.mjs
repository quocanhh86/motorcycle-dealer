#!/usr/bin/env node
/**
 * verify-mobile-maui.mjs
 * Zero-dependency Node.js v22 static guardrail verifier for Phase 10 (.NET 10 MAUI Android & iOS Field App).
 *
 * Enforces all 8 non-negotiable mobile guardrails:
 *   (a) mobile/AGENTS.md exists, < 8,000 bytes, and has zero YAML frontmatter ('---')
 *   (b) Mobile.Core.csproj has NO <UseMaui>true</UseMaui> and zero Microsoft.Maui.* references
 *   (c) 100% of .xaml ContentPage / ContentView files declare x:DataType (and App.csproj uses SourceGen)
 *   (d) Zero legacy _field [ObservableProperty] patterns in Mobile.Core (must use C# 13 partial properties)
 *   (e) Platforms/Android/AndroidManifest.xml declares android:allowBackup="false" (and never usesCleartextTraffic="true")
 *   (f) network_security_config.xml enforces base-config cleartextTrafficPermitted="false" and restricts cleartext strictly to 10.0.2.2 / localhost / 127.0.0.1
 *   (g) 48 canonical RBAC permission 1-for-1 parity between Mobile.Core/Contracts/Permissions.cs, backend Permissions.cs, and baseline-spec-snapshot.json
 *   (h) [MAUI-IOS-AND-TOOLKIT] Multi-target net10.0-android + net10.0-ios (26.0 / 15.0), strict ATS Info.plist + iOS host/notification files,
 *       and real CommunityToolkit.Maui, FluentValidation, Humanizer & Microsoft.Extensions.Http.Resilience usage across mobile/src/**
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..", "..");

const cliFlags = new Set(process.argv.slice(2));
const jsonMode = cliFlags.has("--json");
const preflightMode = cliFlags.has("--preflight");

const checks = [];
const failures = [];

function recordPass(id, detail) {
  checks.push({ id, passed: true, detail });
  if (!jsonMode) console.log(`[PASS] [${id}] ${detail}`);
}

function recordFail(id, detail) {
  checks.push({ id, passed: false, detail });
  failures.push(`[${id}] ${detail}`);
  if (!jsonMode) console.error(`[FAIL] [${id}] ${detail}`);
}

function recordSkip(id, detail) {
  checks.push({ id, passed: true, skipped: true, detail });
  if (!jsonMode) console.log(`[INFO] [${id}] ${detail}`);
}

function walkFiles(dir, extFilter, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const ent of entries) {
    const lower = ent.name.toLowerCase();
    if (lower === "bin" || lower === "obj" || lower === ".git" || lower === "node_modules") {
      continue;
    }
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      walkFiles(full, extFilter, acc);
    } else if (ent.isFile() && extFilter(lower)) {
      acc.push(full);
    }
  }
  return acc;
}

if (!jsonMode) {
  console.log(`[INFO] === .NET 10 MAUI Android & iOS Static Guardrail Verifier (Mode=${preflightMode ? "PREFLIGHT" : "ENFORCE"}) ===`);
}

const mobileRoot = path.join(repoRoot, "mobile");
const mobileCoreDir = path.join(mobileRoot, "src", "MotorcycleDealer.Mobile.Core");
const mobileAppDir = path.join(mobileRoot, "src", "MotorcycleDealer.Mobile.App");

// (a) mobile/AGENTS.md < 8,000 B & zero YAML frontmatter
const mobileAgentsPath = path.join(mobileRoot, "AGENTS.md");
if (!fs.existsSync(mobileAgentsPath)) {
  recordFail("MAUI-AGENTS-MD", "Missing mobile/AGENTS.md rulebook.");
} else {
  const buf = fs.readFileSync(mobileAgentsPath);
  const firstLine = buf.toString("utf8").split(/\r?\n/, 1)[0].trim();
  if (buf.length >= 8000) {
    recordFail("MAUI-AGENTS-MD", `mobile/AGENTS.md size (${buf.length}B) >= 8,000B cap.`);
  } else if (firstLine === "---") {
    recordFail("MAUI-AGENTS-MD", "mobile/AGENTS.md starts with prohibited YAML frontmatter ('---').");
  } else {
    recordPass("MAUI-AGENTS-MD", `mobile/AGENTS.md valid (${buf.length}B < 8,000B, zero frontmatter).`);
  }
}

const hasCoreScaffold = fs.existsSync(mobileCoreDir);
const hasAppScaffold = fs.existsSync(mobileAppDir);

// (b) Mobile.Core.csproj has NO <UseMaui>true</UseMaui> & zero Microsoft.Maui.* references
if (!hasCoreScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-CORE-NO-USEMAUI", "mobile/src/MotorcycleDealer.Mobile.Core not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-CORE-NO-USEMAUI", "Missing mobile/src/MotorcycleDealer.Mobile.Core directory.");
  }
} else {
  const coreCsprojs = walkFiles(mobileCoreDir, (n) => n.endsWith(".csproj"));
  if (coreCsprojs.length === 0) {
    recordFail("MAUI-CORE-NO-USEMAUI", "No .csproj found under mobile/src/MotorcycleDealer.Mobile.Core.");
  } else {
    const coreViolations = [];
    for (const csproj of coreCsprojs) {
      const rel = path.relative(repoRoot, csproj).replace(/\\/g, "/");
      const xml = fs.readFileSync(csproj, "utf8");
      if (/<UseMaui>\s*true\s*<\/UseMaui>/i.test(xml)) {
        coreViolations.push(`${rel} sets <UseMaui>true</UseMaui>`);
      }
      if (/Microsoft\.Maui\./i.test(xml)) {
        coreViolations.push(`${rel} references Microsoft.Maui.*`);
      }
    }
    const buildPropsPath = path.join(mobileRoot, "Directory.Build.props");
    if (fs.existsSync(buildPropsPath)) {
      const propsXml = fs.readFileSync(buildPropsPath, "utf8");
      if (/<TargetFramework>[^<]+<\/TargetFramework>/i.test(propsXml)) {
        coreViolations.push("mobile/Directory.Build.props hardcodes singular <TargetFramework> (conflicts with Mobile.App TargetFrameworks)");
      }
      if (!/<TreatWarningsAsErrors>\s*true\s*<\/TreatWarningsAsErrors>/i.test(propsXml)) {
        coreViolations.push("mobile/Directory.Build.props missing <TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
      }
    }
    if (coreViolations.length > 0) {
      recordFail("MAUI-CORE-NO-USEMAUI", coreViolations.join("; "));
    } else {
      recordPass("MAUI-CORE-NO-USEMAUI", `Verified ${coreCsprojs.length} Mobile.Core project(s): pure net10.0, zero <UseMaui>true</UseMaui>, zero Microsoft.Maui.* refs.`);
    }
  }
}

// (c) 100% of .xaml ContentPage/ContentView files declare x:DataType (+ MauiXamlInflator=SourceGen)
if (!hasAppScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-XAML-DATATYPE", "mobile/src/MotorcycleDealer.Mobile.App not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-XAML-DATATYPE", "Missing mobile/src/MotorcycleDealer.Mobile.App directory.");
  }
} else {
  const xamlFiles = walkFiles(mobileAppDir, (n) => n.endsWith(".xaml"));
  const checkedViews = [];
  const missingDataType = [];
  for (const xf of xamlFiles) {
    const raw = fs.readFileSync(xf, "utf8");
    const stripped = raw.replace(/<!--[\s\S]*?-->/g, "").replace(/<\?xml[\s\S]*?\?>/g, "").trim();
    const rootMatch = stripped.match(/^<([A-Za-z0-9_:-]+)\b([^>]*)>/);
    if (!rootMatch) continue;
    const rootTag = rootMatch[1];
    const rootAttrs = rootMatch[2];
    const localTag = rootTag.includes(":") ? rootTag.split(":")[1] : rootTag;
    if (localTag === "ContentPage" || localTag === "ContentView") {
      const rel = path.relative(repoRoot, xf).replace(/\\/g, "/");
      checkedViews.push(rel);
      if (!/\bx:DataType\s*=\s*"[^"]+"/.test(rootAttrs)) {
        missingDataType.push(rel);
      }
    }
  }

  const appCsprojPath = path.join(mobileAppDir, "MotorcycleDealer.Mobile.App.csproj");
  const inflatorIssues = [];
  if (fs.existsSync(appCsprojPath)) {
    const appXml = fs.readFileSync(appCsprojPath, "utf8");
    if (!/<MauiXamlInflator>\s*SourceGen\s*<\/MauiXamlInflator>/i.test(appXml)) {
      inflatorIssues.push("MotorcycleDealer.Mobile.App.csproj missing <MauiXamlInflator>SourceGen</MauiXamlInflator>");
    }
  } else {
    inflatorIssues.push("Missing MotorcycleDealer.Mobile.App.csproj");
  }

  if (checkedViews.length === 0) {
    recordFail("MAUI-XAML-DATATYPE", "No ContentPage/ContentView .xaml files found under MotorcycleDealer.Mobile.App.");
  } else if (missingDataType.length > 0 || inflatorIssues.length > 0) {
    const parts = [];
    if (missingDataType.length > 0) parts.push(`Missing x:DataType on ${missingDataType.length} view(s): ${missingDataType.join(", ")}`);
    if (inflatorIssues.length > 0) parts.push(inflatorIssues.join("; "));
    recordFail("MAUI-XAML-DATATYPE", parts.join(" | "));
  } else {
    recordPass("MAUI-XAML-DATATYPE", `100% of ContentPage/ContentView XAML files (${checkedViews.length}/${checkedViews.length}) declare x:DataType + SourceGen enabled.`);
  }
}

// (d) Zero legacy _field [ObservableProperty] patterns in Mobile.Core (C# 13 partial properties required)
if (!hasCoreScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-CS13-PARTIAL-MVVM", "mobile/src/MotorcycleDealer.Mobile.Core not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-CS13-PARTIAL-MVVM", "Missing mobile/src/MotorcycleDealer.Mobile.Core directory.");
  }
} else {
  const csFiles = walkFiles(mobileCoreDir, (n) => n.endsWith(".cs"));
  const fieldHits = [];
  let partialPropCount = 0;

  // Match [ObservableProperty] followed by any optional attributes/whitespace and a field declaration ending in ; or = (not partial property)
  const legacyFieldRx = /\[(?:CommunityToolkit\.Mvvm\.ComponentModel\.)?ObservableProperty\](?:\s*\[[^\]]+\])*\s*(?:(?:private|protected|internal|public|readonly)\s+)*[A-Za-z0-9_<>,\.\?\s\[\]]+\s+_[A-Za-z0-9_]+\s*(?:=|;)/g;
  const partialPropRx = /\[(?:CommunityToolkit\.Mvvm\.ComponentModel\.)?ObservableProperty\](?:\s*\[[^\]]+\])*\s*public\s+partial\s+/g;

  for (const cf of csFiles) {
    const raw = fs.readFileSync(cf, "utf8");
    const rel = path.relative(repoRoot, cf).replace(/\\/g, "/");
    const matches = raw.match(legacyFieldRx);
    if (matches && matches.length > 0) {
      fieldHits.push(`${rel} (${matches.length})`);
    }
    const pMatches = raw.match(partialPropRx);
    if (pMatches) partialPropCount += pMatches.length;
  }

  if (fieldHits.length > 0) {
    recordFail("MAUI-CS13-PARTIAL-MVVM", `Found legacy _field [ObservableProperty] declaration(s) (must use C# 13 partial properties): ${fieldHits.join(", ")}`);
  } else {
    recordPass("MAUI-CS13-PARTIAL-MVVM", `Zero legacy _field [ObservableProperty] patterns across ${csFiles.length} Mobile.Core C# file(s) (${partialPropCount} C# 13 partial observable properties verified).`);
  }
}

// (e) AndroidManifest.xml has android:allowBackup="false" and never usesCleartextTraffic="true"
if (!hasAppScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-ANDROID-MANIFEST", "mobile/src/MotorcycleDealer.Mobile.App not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-ANDROID-MANIFEST", "Missing mobile/src/MotorcycleDealer.Mobile.App directory.");
  }
} else {
  const manifestPath = path.join(mobileAppDir, "Platforms", "Android", "AndroidManifest.xml");
  if (!fs.existsSync(manifestPath)) {
    recordFail("MAUI-ANDROID-MANIFEST", "Missing Platforms/Android/AndroidManifest.xml.");
  } else {
    const xml = fs.readFileSync(manifestPath, "utf8");
    const issues = [];
    if (!/android:allowBackup\s*=\s*"false"/i.test(xml)) {
      issues.push('Missing android:allowBackup="false"');
    }
    if (/android:usesCleartextTraffic\s*=\s*"true"/i.test(xml)) {
      issues.push('Prohibited global android:usesCleartextTraffic="true" in AndroidManifest.xml');
    }
    if (issues.length > 0) {
      recordFail("MAUI-ANDROID-MANIFEST", issues.join("; "));
    } else {
      recordPass("MAUI-ANDROID-MANIFEST", 'AndroidManifest.xml enforces android:allowBackup="false" with zero global usesCleartextTraffic="true".');
    }
  }
}

// (f) network_security_config.xml is Debug-only or strictly scoped to 10.0.2.2 / localhost / 127.0.0.1
if (!hasAppScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-NETWORK-SECURITY", "mobile/src/MotorcycleDealer.Mobile.App not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-NETWORK-SECURITY", "Missing mobile/src/MotorcycleDealer.Mobile.App directory.");
  }
} else {
  const androidDir = path.join(mobileAppDir, "Platforms", "Android");
  const netSecCandidates = walkFiles(androidDir, (n) => n === "network_security_config.xml");
  const manifestPath = path.join(androidDir, "AndroidManifest.xml");
  const manifestXml = fs.existsSync(manifestPath) ? fs.readFileSync(manifestPath, "utf8") : "";
  const referencesNetSec = /networkSecurityConfig/i.test(manifestXml);

  if (netSecCandidates.length === 0) {
    if (referencesNetSec) {
      recordFail("MAUI-NETWORK-SECURITY", "AndroidManifest.xml references networkSecurityConfig but network_security_config.xml was not found.");
    } else {
      recordPass("MAUI-NETWORK-SECURITY", "No cleartext network_security_config.xml present (strict Android HTTPS default).");
    }
  } else {
    const ALLOWED_CLEARTEXT_HOSTS = new Set(["10.0.2.2", "localhost", "127.0.0.1"]);
    const issues = [];
    for (const cfgPath of netSecCandidates) {
      const rel = path.relative(repoRoot, cfgPath).replace(/\\/g, "/");
      const raw = fs.readFileSync(cfgPath, "utf8").replace(/<!--[\s\S]*?-->/g, "");
      if (/<base-config[^>]*cleartextTrafficPermitted\s*=\s*"true"/i.test(raw)) {
        issues.push(`${rel} sets base-config cleartextTrafficPermitted="true"`);
      }
      if (!/<base-config[^>]*cleartextTrafficPermitted\s*=\s*"false"/i.test(raw)) {
        issues.push(`${rel} must explicitly set <base-config cleartextTrafficPermitted="false">`);
      }
      const domainConfigBlocks = raw.matchAll(/<domain-config\b([^>]*)>([\s\S]*?)<\/domain-config>/gi);
      for (const blk of domainConfigBlocks) {
        const attrs = blk[1] || "";
        const body = blk[2] || "";
        if (/cleartextTrafficPermitted\s*=\s*"true"/i.test(attrs)) {
          const domains = Array.from(body.matchAll(/<domain\b[^>]*>([^<]+)<\/domain>/gi), (m) => m[1].trim().toLowerCase());
          if (domains.length === 0) {
            issues.push(`${rel} has cleartext domain-config with no explicit <domain> entries`);
          }
          for (const d of domains) {
            if (!ALLOWED_CLEARTEXT_HOSTS.has(d)) {
              issues.push(`${rel} permits cleartext traffic for non-local host '${d}' (only 10.0.2.2, localhost, 127.0.0.1 allowed)`);
            }
          }
        }
      }
    }
    if (issues.length > 0) {
      recordFail("MAUI-NETWORK-SECURITY", issues.join("; "));
    } else {
      recordPass("MAUI-NETWORK-SECURITY", `Verified ${netSecCandidates.length} network_security_config.xml file(s): base-config HTTPS-only, cleartext strictly limited to 10.0.2.2/localhost/127.0.0.1.`);
    }
  }
}

// (g) 48 RBAC permission parity between Mobile.Core/Contracts/Permissions.cs and backend Permissions.cs
const backendPermsCandidates = [
  path.join(repoRoot, "backend", "src", "MotorcycleDealer.Domain", "Constants", "Permissions.cs"),
  path.join(repoRoot, "backend", "src", "MotorcycleDealer.Domain", "Security", "Permissions.cs"),
];
const backendPermsPath = backendPermsCandidates.find((p) => fs.existsSync(p));
const mobilePermsPath = path.join(mobileCoreDir, "Contracts", "Permissions.cs");
const snapshotPath = path.join(__dirname, "baseline-spec-snapshot.json");

if (!hasCoreScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-RBAC-48-PARITY", "mobile/src/MotorcycleDealer.Mobile.Core not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-RBAC-48-PARITY", "Missing mobile/src/MotorcycleDealer.Mobile.Core/Contracts/Permissions.cs.");
  }
} else if (!backendPermsPath) {
  recordFail("MAUI-RBAC-48-PARITY", "Backend Permissions.cs not found in MotorcycleDealer.Domain.");
} else if (!fs.existsSync(mobilePermsPath)) {
  recordFail("MAUI-RBAC-48-PARITY", "Missing mobile/src/MotorcycleDealer.Mobile.Core/Contracts/Permissions.cs.");
} else {
  const extractPerms = (txt) =>
    new Set(Array.from(txt.matchAll(/"(Permissions\.[A-Za-z0-9_]+\.[A-Za-z0-9_]+)"/g), (m) => m[1]));
  const backendPerms = extractPerms(fs.readFileSync(backendPermsPath, "utf8"));
  const mobilePerms = extractPerms(fs.readFileSync(mobilePermsPath, "utf8"));
  let snapshotPerms = backendPerms;
  if (fs.existsSync(snapshotPath)) {
    const snap = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
    if (Array.isArray(snap?.rbac?.permissions)) {
      snapshotPerms = new Set(snap.rbac.permissions.map((p) => p.code));
    }
  }

  const missingInMobile = Array.from(backendPerms).filter((p) => !mobilePerms.has(p));
  const extraInMobile = Array.from(mobilePerms).filter((p) => !backendPerms.has(p));
  const driftVsSnapshot = Array.from(snapshotPerms).filter((p) => !mobilePerms.has(p));

  if (
    backendPerms.size !== 48 ||
    mobilePerms.size !== 48 ||
    missingInMobile.length > 0 ||
    extraInMobile.length > 0 ||
    driftVsSnapshot.length > 0
  ) {
    recordFail(
      "MAUI-RBAC-48-PARITY",
      `Permission drift: mobile=${mobilePerms.size}/48, backend=${backendPerms.size}/48 (missing=[${missingInMobile.join(",")}], extra=[${extraInMobile.join(",")}], vsSnapshot=[${driftVsSnapshot.join(",")}]).`
    );
  } else {
    recordPass("MAUI-RBAC-48-PARITY", "Mobile.Core/Contracts/Permissions.cs matches all 48/48 canonical RBAC permissions 1-for-1 with backend & snapshot.");
  }
}

// (h) [MAUI-IOS-AND-TOOLKIT] Multi-target net10.0-android + net10.0-ios, strict iOS ATS/files, CommunityToolkit.Maui & .NET 10 libraries
if (!hasAppScaffold || !hasCoreScaffold) {
  if (preflightMode) {
    recordSkip("MAUI-IOS-AND-TOOLKIT", "mobile/src/MotorcycleDealer.Mobile.{Core,App} not scaffolded yet (--preflight).");
  } else {
    recordFail("MAUI-IOS-AND-TOOLKIT", "Missing mobile/src/MotorcycleDealer.Mobile.Core or MotorcycleDealer.Mobile.App directory.");
  }
} else {
  const issues = [];

  // (h.1) Mobile.App.csproj multi-targets net10.0-android AND net10.0-ios (SupportedOSPlatformVersion 26.0 / 15.0)
  const appCsprojPath = path.join(mobileAppDir, "MotorcycleDealer.Mobile.App.csproj");
  if (!fs.existsSync(appCsprojPath)) {
    issues.push("Missing MotorcycleDealer.Mobile.App.csproj");
  } else {
    const appCsprojXml = fs.readFileSync(appCsprojPath, "utf8").replace(/<!--[\s\S]*?-->/g, "");
    const tfmMatch = appCsprojXml.match(/<TargetFrameworks>([\s\S]*?)<\/TargetFrameworks>/i);
    const tfms = tfmMatch ? tfmMatch[1].split(";").map((t) => t.trim().toLowerCase()) : [];
    if (!tfms.includes("net10.0-android") || !tfms.includes("net10.0-ios")) {
      issues.push("Mobile.App.csproj <TargetFrameworks> must include both net10.0-android and net10.0-ios");
    }
    if (!/<SupportedOSPlatformVersion>\s*26\.0\s*<\/SupportedOSPlatformVersion>/i.test(appCsprojXml)) {
      issues.push("Mobile.App.csproj missing Android <SupportedOSPlatformVersion>26.0</SupportedOSPlatformVersion>");
    }
    if (!/<SupportedOSPlatformVersion>\s*15\.0\s*<\/SupportedOSPlatformVersion>/i.test(appCsprojXml)) {
      issues.push("Mobile.App.csproj missing iOS <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>");
    }
    if (!/PackageReference\s+Include="CommunityToolkit\.Maui"/i.test(appCsprojXml)) {
      issues.push("Mobile.App.csproj missing CommunityToolkit.Maui PackageReference");
    }
  }

  // (h.2) Platforms/iOS/Info.plist exists, declares NSCameraUsageDescription & UIStatusBarStyleLightContent, and enforces strict ATS
  const iosDir = path.join(mobileAppDir, "Platforms", "iOS");
  const infoPlistPath = path.join(iosDir, "Info.plist");
  if (!fs.existsSync(infoPlistPath)) {
    issues.push("Missing Platforms/iOS/Info.plist");
  } else {
    const plistXml = fs.readFileSync(infoPlistPath, "utf8").replace(/<!--[\s\S]*?-->/g, "");
    if (!/<key>\s*NSCameraUsageDescription\s*<\/key>/i.test(plistXml)) {
      issues.push("Platforms/iOS/Info.plist missing NSCameraUsageDescription");
    }
    if (!/<string>\s*UIStatusBarStyleLightContent\s*<\/string>/i.test(plistXml)) {
      issues.push("Platforms/iOS/Info.plist missing UIStatusBarStyleLightContent");
    }
    if (/<key>\s*NSAllowsArbitraryLoads\s*<\/key>\s*<true\s*\/>/i.test(plistXml)) {
      issues.push("Platforms/iOS/Info.plist sets prohibited NSAllowsArbitraryLoads=<true/> (violates strict ATS)");
    }
  }

  // (h.3) Required iOS host, entitlements & native UNUserNotificationCenter service files exist
  const requiredIosFiles = [
    "Entitlements.plist",
    "AppDelegate.cs",
    "Program.cs",
    path.join("Services", "IosLocalNotificationService.cs"),
  ];
  for (const relIos of requiredIosFiles) {
    if (!fs.existsSync(path.join(iosDir, relIos))) {
      issues.push(`Missing Platforms/iOS/${relIos.replace(/\\/g, "/")}`);
    }
  }

  // (h.4) Real CommunityToolkit.Maui, FluentValidation, Humanizer & Microsoft.Extensions.Http.Resilience usage across mobile/src/**
  const appSources = walkFiles(mobileAppDir, (n) => n.endsWith(".xaml") || n.endsWith(".cs"));
  const coreSources = walkFiles(mobileCoreDir, (n) => n.endsWith(".cs") || n.endsWith(".csproj"));
  const appCorpus = appSources.map((f) => fs.readFileSync(f, "utf8")).join("\n");
  const coreCorpus = coreSources.map((f) => fs.readFileSync(f, "utf8")).join("\n");

  const requiredAppTokens = [
    { label: "CommunityToolkit.Maui StatusBarBehavior", rx: /\bStatusBarBehavior\b/ },
    { label: "CommunityToolkit.Maui Expander", rx: /\btoolkit:Expander\b|\bExpander\b/ },
    { label: "CommunityToolkit.Maui TouchBehavior", rx: /\bTouchBehavior\b/ },
    { label: "CommunityToolkit.Maui Toast", rx: /\bToast\.Make\s*\(/ },
    { label: "CommunityToolkit.Maui Snackbar", rx: /\bSnackbar\.Make\s*\(/ },
  ];
  for (const check of requiredAppTokens) {
    if (!check.rx.test(appCorpus)) {
      issues.push(`Missing ${check.label} usage in MotorcycleDealer.Mobile.App`);
    }
  }

  const requiredCoreTokens = [
    { label: "FluentValidation (package + AbstractValidator)", rx: /PackageReference\s+Include="FluentValidation"/i },
    { label: "FluentValidation AbstractValidator<T>", rx: /\bAbstractValidator\s*</ },
    { label: "Humanizer (package)", rx: /PackageReference\s+Include="Humanizer(?:\.Core)?"/i },
    { label: "Humanizer API (.ToQuantity / .Humanize)", rx: /\.(?:ToQuantity|Humanize)\s*\(/ },
    { label: "Microsoft.Extensions.Http.Resilience (package)", rx: /PackageReference\s+Include="Microsoft\.Extensions\.Http\.Resilience"/i },
    { label: "Polly v8 AddResilienceHandler pipeline", rx: /\.AddResilienceHandler\s*\(/ },
  ];
  for (const check of requiredCoreTokens) {
    if (!check.rx.test(coreCorpus)) {
      issues.push(`Missing ${check.label} usage in MotorcycleDealer.Mobile.Core`);
    }
  }

  if (issues.length > 0) {
    recordFail("MAUI-IOS-AND-TOOLKIT", issues.join("; "));
  } else {
    recordPass(
      "MAUI-IOS-AND-TOOLKIT",
      "Verified net10.0-android (API 26+) & net10.0-ios (iOS 15+) multi-targeting, strict ATS Info.plist & iOS host/notification files, CommunityToolkit.Maui (StatusBarBehavior/Expander/TouchBehavior/Toast/Snackbar), FluentValidation, Humanizer & Http.Resilience."
    );
  }
}

if (jsonMode) {
  console.log(JSON.stringify({ passed: failures.length === 0, failureCount: failures.length, checks }, null, 2));
}

if (failures.length > 0) {
  if (!jsonMode) {
    console.error(`\n[FAIL] MAUI Mobile Guardrails Verification FAILED (${failures.length} issue(s)).`);
  }
  process.exit(1);
}

if (!jsonMode) {
  console.log(`\n[PASS] MAUI Mobile Guardrails Verification PASSED (${checks.length} checks green).`);
}
process.exit(0);
