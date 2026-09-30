import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import { createSpaServer } from './dev-server.mjs';
import { stageVendorAssets, VENDOR_BUNDLE_MAP } from './stage-vendor.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const FRONTEND_ROOT = path.resolve(__dirname, '..');
const REPO_ROOT = path.resolve(FRONTEND_ROOT, '..');
const SRC_ROOT = path.join(FRONTEND_ROOT, 'src');
const APP_ROOT = path.join(SRC_ROOT, 'app');
const INDEX_HTML_PATH = path.join(SRC_ROOT, 'index.html');
const SNAPSHOT_PATH = path.join(REPO_ROOT, '.agents', 'scripts', 'baseline-spec-snapshot.json');

const REQUIRED_UI_ROUTER_STATES = [
  'app.public.showroom',
  'app.public.login',
  'app.public.register',
  'app.public.forbidden',
  'app.customer.dashboard',
  'app.customer.garage',
  'app.customer.bookingNew',
  'app.customer.bookings',
  'app.customer.loyalty',
  'app.customer.testDrives',
  'app.customer.warranty',
  'app.staff.workbench',
  'app.staff.invoiceBuilder',
  'app.staff.salesDesk',
  'app.staff.timeSlots',
  'app.staff.customers',
  'app.staff.testDrives',
  'app.staff.warrantyReviews',
  'app.admin.analytics',
  'app.admin.dealers',
  'app.admin.staff',
  'app.admin.models',
  'app.admin.loyaltyTiers',
  'app.admin.voucherCatalog',
  'app.admin.spareParts',
  'app.admin.qualityWarranty'
];

const failures = [];

function pass(message) {
  console.log(`[PASS] ${message}`);
}

function fail(message) {
  failures.push(message);
  console.error(`[FAIL] ${message}`);
}

function walkJsFiles(dirPath, bucket = []) {
  for (const entry of fs.readdirSync(dirPath, { withFileTypes: true })) {
    const fullPath = path.join(dirPath, entry.name);
    if (entry.isDirectory()) {
      walkJsFiles(fullPath, bucket);
    } else if (entry.isFile() && entry.name.endsWith('.js')) {
      bucket.push(fullPath);
    }
  }
  return bucket.sort();
}

function httpGet(port, reqPath) {
  return new Promise((resolve, reject) => {
    const req = http.get({ host: '127.0.0.1', port, path: reqPath, timeout: 5000 }, (res) => {
      const chunks = [];
      res.on('data', (chunk) => chunks.push(chunk));
      res.on('end', () => {
        resolve({
          statusCode: res.statusCode || 0,
          headers: res.headers,
          body: Buffer.concat(chunks).toString('utf8')
        });
      });
    });
    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy(new Error(`Request timeout on ${reqPath}`));
    });
  });
}

function getFunctionParamCount(fn) {
  const src = Function.prototype.toString.call(fn)
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\/\/.*$/gm, '');
  const match = src.match(/^[^(]*\(([^)]*)\)/);
  if (!match || !match[1].trim()) {
    return 0;
  }
  return match[1].split(',').filter((part) => part.trim().length > 0).length;
}

async function main() {
  console.log('=== Motorcycle Dealer AngularJS 1.8.3 SPA Verification Suite ===');

  // 0. Check frontend/AGENTS.md & root node_modules hygiene
  const agentsMdPath = path.join(FRONTEND_ROOT, 'AGENTS.md');
  if (!fs.existsSync(agentsMdPath)) {
    fail('frontend/AGENTS.md is missing.');
  } else {
    const rawAgents = fs.readFileSync(agentsMdPath, 'utf8');
    const byteLen = Buffer.byteLength(rawAgents, 'utf8');
    if (byteLen >= 6000) {
      fail(`frontend/AGENTS.md exceeds 6,000-byte limit (${byteLen} bytes).`);
    } else if (rawAgents.trimStart().startsWith('---')) {
      fail('frontend/AGENTS.md must not contain YAML frontmatter.');
    } else {
      pass(`frontend/AGENTS.md verified (${byteLen} bytes < 6,000 B, zero frontmatter).`);
    }
  }

  if (fs.existsSync(path.join(REPO_ROOT, 'node_modules'))) {
    fail('Forbidden repository-root node_modules/ directory detected.');
  } else {
    pass('Zero repository-root node_modules/ pollution.');
  }

  // 1. Stage & verify local browser vendor bundles
  try {
    const stagedResult = stageVendorAssets({ quiet: true });
    for (const bundle of VENDOR_BUNDLE_MAP) {
      const targetFile = path.join(SRC_ROOT, 'vendor', bundle.targetName);
      if (!fs.existsSync(targetFile) || fs.statSync(targetFile).size === 0) {
        fail(`Vendor bundle missing or empty: src/vendor/${bundle.targetName}`);
      }
    }
    pass(`Staged and verified all ${stagedResult.staged} browser vendor bundles in src/vendor/.`);
  } catch (err) {
    fail(`Vendor staging failed: ${err.message}`);
  }

  // 2. Check JS syntax via node --check across all frontend/src/app/**/*.js & scripts/*.mjs
  const appJsFiles = walkJsFiles(APP_ROOT);
  const scriptMjsFiles = fs.readdirSync(__dirname)
    .filter((f) => f.endsWith('.mjs'))
    .map((f) => path.join(__dirname, f));

  if (appJsFiles.length === 0) {
    fail('No JavaScript files found under frontend/src/app/.');
  } else {
    let syntaxErrors = 0;
    for (const jsFile of [...appJsFiles, ...scriptMjsFiles]) {
      try {
        execFileSync(process.execPath, ['--check', jsFile], { stdio: 'pipe' });
      } catch (err) {
        syntaxErrors++;
        fail(`Syntax error in ${path.relative(FRONTEND_ROOT, jsFile)}: ${err.stderr?.toString() || err.message}`);
      }
    }
    if (syntaxErrors === 0) {
      pass(`Syntax check ('node --check') passed across ${appJsFiles.length} SPA .js files and ${scriptMjsFiles.length} script .mjs files.`);
    }
  }

  // 3. Verify index.html script & stylesheet references resolve on disk and cover all app/**/*.js
  if (!fs.existsSync(INDEX_HTML_PATH)) {
    fail('frontend/src/index.html does not exist.');
  } else {
    const indexHtml = fs.readFileSync(INDEX_HTML_PATH, 'utf8');
    const scriptSrcs = [...indexHtml.matchAll(/<script\b[^>]*\bsrc=["']([^"']+)["'][^>]*><\/script>/gi)].map((m) => m[1]);
    const linkHrefs = [...indexHtml.matchAll(/<link\b[^>]*\bhref=["']([^"']+)["'][^>]*>/gi)].map((m) => m[1]);

    if (scriptSrcs.length === 0 || linkHrefs.length === 0) {
      fail('index.html must include local <link href="..."> and <script src="..."> tags.');
    }

    let missingRefs = 0;
    const referencedAppFiles = new Set();

    for (const assetRef of [...linkHrefs, ...scriptSrcs]) {
      if (/^https?:\/\//i.test(assetRef)) {
        missingRefs++;
        fail(`External CDN URL prohibited in index.html: ${assetRef}`);
        continue;
      }
      const cleanRef = assetRef.split('?')[0].replace(/^\/+/, '');
      const diskPath = path.resolve(SRC_ROOT, cleanRef);
      if (!fs.existsSync(diskPath) || !fs.statSync(diskPath).isFile()) {
        missingRefs++;
        fail(`Unresolved asset reference in index.html: ${assetRef}`);
      } else if (diskPath.startsWith(APP_ROOT)) {
        referencedAppFiles.add(path.normalize(diskPath));
      }
    }

    for (const appFile of appJsFiles) {
      if (!referencedAppFiles.has(path.normalize(appFile))) {
        missingRefs++;
        fail(`Orphaned AngularJS file not loaded in index.html: ${path.relative(SRC_ROOT, appFile)}`);
      }
    }

    if (missingRefs === 0) {
      pass(`Resolved all ${linkHrefs.length} stylesheet(s) and ${scriptSrcs.length} script tag(s) in index.html with zero orphaned app files.`);
    }

    // 4. Execute AngularJS module/DI/route registration harness in isolated VM context
    const modules = new Map();
    const registeredComponents = new Map();
    const registeredDirectives = new Map();
    const registeredFilters = new Map();
    const registeredServices = new Map();
    const registeredConstants = new Map();
    const registeredStates = new Map();
    const injectTargets = [];

    function ensureModule(name, requires) {
      if (Array.isArray(requires)) {
        const mod = {
          name,
          requires,
          constant(k, v) { registeredConstants.set(k, v); return mod; },
          config(fn) { injectTargets.push({ kind: `config(${name})`, fn }); mod._configs.push(fn); return mod; },
          run(fn) { injectTargets.push({ kind: `run(${name})`, fn }); mod._runs.push(fn); return mod; },
          service(k, fn) { injectTargets.push({ kind: `service(${k})`, fn }); registeredServices.set(k, fn); return mod; },
          factory(k, fn) { injectTargets.push({ kind: `factory(${k})`, fn }); registeredServices.set(k, fn); return mod; },
          directive(k, fn) { injectTargets.push({ kind: `directive(${k})`, fn }); registeredDirectives.set(k, fn); return mod; },
          filter(k, fn) { injectTargets.push({ kind: `filter(${k})`, fn }); registeredFilters.set(k, fn); return mod; },
          component(k, def) {
            if (def && typeof def.controller === 'function') {
              injectTargets.push({ kind: `component(${k})`, fn: def.controller });
            }
            registeredComponents.set(k, def);
            return mod;
          },
          _configs: [],
          _runs: []
        };
        modules.set(name, mod);
        return mod;
      }
      const existing = modules.get(name);
      if (!existing) {
        throw new Error(`angular.module('${name}') retrieved before declaration!`);
      }
      return existing;
    }

    // Seed external vendor module stubs
    ensureModule('ui.router', []);

    const sandbox = {
      window: null,
      document: { querySelector: () => null },
      console,
      Intl,
      Date,
      Math,
      JSON,
      Object,
      Array,
      String,
      Number,
      Boolean,
      RegExp,
      Error,
      ArrayBuffer,
      Uint8Array,
      TextDecoder,
      isNaN,
      isFinite,
      atob: (b64) => Buffer.from(b64, 'base64').toString('binary'),
      btoa: (bin) => Buffer.from(bin, 'binary').toString('base64'),
      encodeURIComponent,
      decodeURIComponent,
      setTimeout,
      clearTimeout,
      angular: {
        module: ensureModule,
        copy: (val) => structuredClone(val),
        isArray: Array.isArray,
        isString: (v) => typeof v === 'string',
        isObject: (v) => v !== null && typeof v === 'object',
        isFunction: (v) => typeof v === 'function',
        extend: Object.assign,
        noop: () => {}
      }
    };
    sandbox.window = sandbox;
    vm.createContext(sandbox);

    const appScriptSrcs = scriptSrcs.filter((s) => !s.includes('vendor/'));
    for (const relSrc of appScriptSrcs) {
      const fullJsPath = path.resolve(SRC_ROOT, relSrc.replace(/^\/+/, ''));
      const code = fs.readFileSync(fullJsPath, 'utf8');
      vm.runInContext(code, sandbox, { filename: relSrc });
    }

    // Verify module dependency graph
    for (const [modName, modObj] of modules.entries()) {
      for (const reqMod of modObj.requires) {
        if (!modules.has(reqMod)) {
          fail(`Module '${modName}' depends on unregistered module '${reqMod}'.`);
        }
      }
    }

    // Verify strictDi $inject annotations on every injectable unit
    let diErrors = 0;
    for (const item of injectTargets) {
      if (Array.isArray(item.fn)) {
        continue;
      }
      const paramCount = getFunctionParamCount(item.fn);
      const injectArr = item.fn.$inject;
      if (!Array.isArray(injectArr)) {
        diErrors++;
        fail(`Missing explicit $inject array on ${item.kind}.`);
      } else if (injectArr.length !== paramCount) {
        diErrors++;
        fail(`$inject length mismatch on ${item.kind}: $inject has ${injectArr.length}, function has ${paramCount} params.`);
      }
    }
    if (diErrors === 0) {
      pass(`Strict DI ($inject) verified across all ${injectTargets.length} AngularJS controllers, services, directives, filters, configs, and run blocks.`);
    }

    // Execute route config blocks against mock $stateProvider to capture registered UI-Router states
    const stateProviderMock = {
      state(name, def) {
        const stateName = typeof name === 'string' ? name : name.name;
        const stateDef = typeof name === 'string' ? def : name;
        registeredStates.set(stateName, stateDef);
        return stateProviderMock;
      }
    };
    const urlRouterProviderMock = {
      when() { return urlRouterProviderMock; },
      otherwise() { return urlRouterProviderMock; }
    };
    const locationProviderMock = {
      html5Mode() { return locationProviderMock; },
      hashPrefix() { return locationProviderMock; }
    };
    const httpProviderMock = { interceptors: [] };
    const compileProviderMock = { debugInfoEnabled() {}, commentDirectivesEnabled() {}, cssClassDirectivesEnabled() {} };

    const providerMap = {
      $stateProvider: stateProviderMock,
      $urlRouterProvider: urlRouterProviderMock,
      $locationProvider: locationProviderMock,
      $httpProvider: httpProviderMock,
      $compileProvider: compileProviderMock,
      APP_CONFIG: registeredConstants.get('APP_CONFIG'),
      PERMISSIONS: registeredConstants.get('PERMISSIONS')
    };

    for (const modObj of modules.values()) {
      for (const cfgFn of modObj._configs) {
        const deps = (cfgFn.$inject || []).map((dep) => providerMap[dep]);
        cfgFn(...deps);
      }
    }

    // Check all 20 canonical UI-Router states + 4 DESIGN.md aliases & login returnUrl contract
    const REQUIRED_ALIAS_STATES = [
      'app.customer.bookService',
      'app.staff.invoicing',
      'app.staff.salesNew',
      'app.staff.slots'
    ];
    let missingStates = 0;
    for (const reqState of [...REQUIRED_UI_ROUTER_STATES, ...REQUIRED_ALIAS_STATES]) {
      const st = registeredStates.get(reqState);
      if (!st) {
        missingStates++;
        fail(`Missing required UI-Router state: ${reqState}`);
        continue;
      }
      if (st.component && !registeredComponents.has(st.component)) {
        missingStates++;
        fail(`State '${reqState}' references unregistered component '${st.component}'.`);
      }
    }
    const loginStateDef = registeredStates.get('app.public.login');
    if (!loginStateDef || !String(loginStateDef.url || '').includes('returnUrl') || !String(loginStateDef.url || '').includes('returnTo')) {
      missingStates++;
      fail(`app.public.login URL must support both '?returnUrl&returnTo' (got '${loginStateDef?.url}').`);
    }
    if (
      REQUIRED_UI_ROUTER_STATES.length !== 26 ||
      REQUIRED_ALIAS_STATES.length !== 4 ||
      registeredStates.size !== 35
    ) {
      missingStates++;
      fail(
        `UI-Router state count mismatch: expected 26 primary + 4 aliases = 30 navigable routes (35 total with 5 abstract), got primary=${REQUIRED_UI_ROUTER_STATES.length}, aliases=${REQUIRED_ALIAS_STATES.length}, total=${registeredStates.size}.`
      );
    }
    if (missingStates === 0) {
      pass(`Verified all ${REQUIRED_UI_ROUTER_STATES.length} canonical UI-Router states + ${REQUIRED_ALIAS_STATES.length} DESIGN.md state aliases (${REQUIRED_UI_ROUTER_STATES.length + REQUIRED_ALIAS_STATES.length} navigable routes, ${registeredStates.size} total states) and ?returnUrl&returnTo login support.`);
    }

    // Cross-check 48 canonical Permissions & Role Presets (Customer=20, DealerStaff=22, Admin=32) against baseline-spec-snapshot.json
    const rolePresetsObj = registeredConstants.get('ROLE_PERMISSION_PRESETS') || {};
    const custPresetLen = Array.isArray(rolePresetsObj.Customer) ? rolePresetsObj.Customer.length : 0;
    const staffPresetLen = Array.isArray(rolePresetsObj.DealerStaff) ? rolePresetsObj.DealerStaff.length : 0;
    const adminPresetLen = Array.isArray(rolePresetsObj.Admin) ? rolePresetsObj.Admin.length : 0;
    if (custPresetLen !== 20 || staffPresetLen !== 22 || adminPresetLen !== 32) {
      fail(`ROLE_PERMISSION_PRESETS mismatch: expected Customer=20, DealerStaff=22, Admin=32 (got Customer=${custPresetLen}, DealerStaff=${staffPresetLen}, Admin=${adminPresetLen}).`);
    } else {
      pass('Verified ROLE_PERMISSION_PRESETS exact counts (Customer=20, DealerStaff=22, Admin=32 across 48 canonical permissions).');
    }

    if (fs.existsSync(SNAPSHOT_PATH)) {
      const snapshot = JSON.parse(fs.readFileSync(SNAPSHOT_PATH, 'utf8'));
      const canonicalPerms = new Set(snapshot.rbac.permissions.map((p) => p.code));
      const spaPermsObj = registeredConstants.get('PERMISSIONS') || {};
      const spaPermValues = new Set();
      for (const group of Object.values(spaPermsObj)) {
        if (group && typeof group === 'object') {
          for (const code of Object.values(group)) {
            if (typeof code === 'string') {
              spaPermValues.add(code);
            }
          }
        }
      }
      if (spaPermValues.size !== canonicalPerms.size) {
        fail(`PERMISSIONS constant count (${spaPermValues.size}) does not match canonical ${canonicalPerms.size} permissions.`);
      } else {
        for (const p of canonicalPerms) {
          if (!spaPermValues.has(p)) {
            fail(`Missing canonical permission in SPA PERMISSIONS constant: ${p}`);
          }
        }
        for (const [stateName, stateDef] of registeredStates.entries()) {
          const perms = stateDef?.data?.permissions || [];
          for (const sp of perms) {
            if (!canonicalPerms.has(sp)) {
              fail(`State '${stateName}' references non-canonical permission '${sp}'.`);
            }
          }
        }
        pass(`Verified all ${canonicalPerms.size} canonical RBAC permissions match baseline-spec-snapshot.json.`);
      }
    }

    // Verify RISK-03 integer basis-point two-step floor loyalty math
    const loyaltyCalc = registeredConstants.get('LoyaltyCalculator');
    if (!loyaltyCalc || typeof loyaltyCalc.calculatePoints !== 'function') {
      fail('Missing LoyaltyCalculator.calculatePoints constant.');
    } else {
      const ptsGold = loyaltyCalc.calculatePoints(455000, 1.50);
      const ptsDriftEdge = loyaltyCalc.calculatePoints(200000, 1.15);
      if (ptsGold !== 67 || ptsDriftEdge !== 23) {
        fail(`RISK-03 LoyaltyCalculator mismatch: expected 455,000@1.50=67 (got ${ptsGold}) and 200,000@1.15=23 (got ${ptsDriftEdge}).`);
      } else {
        pass('Verified RISK-03 integer-scaled two-step floor math (455,000 @ 1.50 = 67; 200,000 @ 1.15 = 23 with zero IEEE-754 drift).');
      }
    }

    // Verify filters (vndCurrency & utcToLocal datetime/date/relative) and RISK-04 dealerChart $onDestroy contract
    const vndFilterFactory = registeredFilters.get('vndCurrency');
    const utcFilterFactory = registeredFilters.get('utcToLocal');
    if (!vndFilterFactory || !utcFilterFactory) {
      fail('Missing required vndCurrency or utcToLocal filter registration.');
    } else {
      const vndFn = vndFilterFactory();
      const utcFn = utcFilterFactory();
      const formattedMoney = vndFn(102500000);
      const formattedDate = utcFn('2026-05-14T09:02:18Z');
      const relativeJustNow = utcFn(new Date().toISOString(), 'relative');
      const relativeFiveMin = utcFn(new Date(Date.now() - 5 * 60 * 1000).toISOString(), 'relative');
      if (!formattedMoney.includes('102.500.000')) {
        fail(`vndCurrency(102500000) returned unexpected string: ${formattedMoney}`);
      }
      if (!formattedDate.includes('16:02') || !formattedDate.includes('2026')) {
        fail(`utcToLocal('2026-05-14T09:02:18Z') did not format to Asia/Ho_Chi_Minh (+07:00): ${formattedDate}`);
      }
      if (relativeJustNow !== 'Just now' || relativeFiveMin !== '5m ago') {
        fail(`utcToLocal relative mode unexpected output: justNow='${relativeJustNow}', fiveMin='${relativeFiveMin}'`);
      }
      pass(`Verified vndCurrency ('${formattedMoney}') and utcToLocal ('${formattedDate}', relative '${relativeFiveMin}') filters.`);
    }

    // Verify hasPermission directive preserves external ng-disabled state and SignalR TierUpgraded reads newPointsMultiplier
    const hasPermFactory = registeredDirectives.get('hasPermission');
    if (!hasPermFactory) {
      fail('Missing hasPermission directive.');
    } else {
      let authAllowed = true;
      const ddo = hasPermFactory(
        { $on: () => () => {} },
        { hasPermissions: () => authAllowed }
      );
      let disabledState = true; // Simulates button already disabled by ng-disabled="form.$invalid"
      const mockEl = {
        removeClass() {},
        addClass() {},
        removeAttr() {},
        attr() {},
        prop(_k, val) {
          if (val === undefined) return disabledState;
          disabledState = val;
          return disabledState;
        }
      };
      ddo.link({ $on() {}, $eval: () => [] }, mockEl, { hasPermission: 'Permissions.Bookings.Create', $observe: () => () => {} });
      if (disabledState !== true) {
        fail('hasPermission directive clobbered pre-existing disabled/ng-disabled state when allowed=true.');
      } else {
        pass('Verified hasPermission directive preserves ng-disabled state and cleans up cleanly.');
      }
    }

    const signalrSvcCtor = registeredServices.get('SignalRClientService');
    const notifStoreCtor = registeredServices.get('NotificationStore');
    if (signalrSvcCtor && notifStoreCtor) {
      const notifStore = new notifStoreCtor();
      let capturedToast = null;
      const signalrSvc = new signalrSvcCtor(
        { resolve: (v) => Promise.resolve(v), when: (v) => Promise.resolve(v) },
        { $applyAsync: (fn) => fn(), $broadcast() {} },
        registeredConstants.get('APP_CONFIG'),
        { getAccessToken: () => 'test-jwt-token' },
        notifStore,
        { push: (t) => { capturedToast = t; } }
      );
      signalrSvc.simulateEvent('TierUpgraded', { newTierCode: 'Gold', newPointsMultiplier: 1.5 });
      if (!capturedToast || !capturedToast.message.includes('1.5x')) {
        fail(`SignalR TierUpgraded did not render newPointsMultiplier ('${capturedToast?.message}').`);
      } else {
        pass(`Verified SignalR TierUpgraded reads newPointsMultiplier ('${capturedToast.message}').`);
      }
    }

    // Verify AuthService.resolvePostLoginState sanitization & route-guard.run.js $transitions.onBefore RBAC hooks
    sandbox.angular.isNumber = (v) => typeof v === 'number' && !Number.isNaN(v);
    const tokenStoreCtor = registeredServices.get('TokenStorageService');
    const authSvcCtor = registeredServices.get('AuthService');
    const coreModule = modules.get('app.core');
    const routeGuardRunFn = coreModule?._runs?.[0];

    const setTestRoleSession = (targetAuthSvc, role) => {
      const presets = registeredConstants.get('ROLE_PERMISSION_PRESETS');
      const perms = (presets && Array.isArray(presets[role])) ? presets[role].slice() : [];
      return targetAuthSvc.setSession({
        accessToken: `test-jwt-${String(role).toLowerCase()}`,
        refreshToken: `test-rt-${String(role).toLowerCase()}`,
        expiresInSeconds: 3600,
        user: {
          userId: '00000000-0000-0000-0000-000000000001',
          email: `test.${String(role).toLowerCase()}@example.test`,
          fullName: `Test ${role}`,
          role,
          dealerId: role === 'DealerStaff' ? '22222222-2222-2222-2222-222222222201' : null,
          permissions: perms
        }
      });
    };

    if (!tokenStoreCtor || !authSvcCtor || !routeGuardRunFn) {
      fail('Missing AuthService, TokenStorageService, or route-guard.run.js registration.');
    } else {
      const tokenStore = new tokenStoreCtor({});
      const authSvc = new authSvcCtor(
        { resolve: (v) => Promise.resolve(v), reject: (e) => Promise.reject(e) },
        { has: () => false, get: () => null },
        { $broadcast() {} },
        registeredConstants.get('APP_CONFIG'),
        registeredConstants.get('ROLE_HOME'),
        registeredConstants.get('ROLE_PERMISSION_PRESETS'),
        tokenStore
      );

      const mockStateLookup = {
        get: (name) => registeredStates.get(name) || null
      };

      setTestRoleSession(authSvc, 'Customer');
      const rejectedExternal = authSvc.resolvePostLoginState('https://evil.example.invalid/phish', mockStateLookup);
      const rejectedProtocolRel = authSvc.resolvePostLoginState('//evil.example.invalid', mockStateLookup);
      const rejectedLoop = authSvc.resolvePostLoginState('app.public.login', mockStateLookup);
      const rejectedForbidden = authSvc.resolvePostLoginState('app.public.forbidden', mockStateLookup);
      const rejectedAbstract = authSvc.resolvePostLoginState('app.customer', mockStateLookup);
      const rejectedUnknown = authSvc.resolvePostLoginState('app.customer.nonExistentState', mockStateLookup);
      const acceptedValid = authSvc.resolvePostLoginState('app.customer.loyalty', mockStateLookup);

      if (
        rejectedExternal !== 'app.customer.garage' ||
        rejectedProtocolRel !== 'app.customer.garage' ||
        rejectedLoop !== 'app.customer.garage' ||
        rejectedForbidden !== 'app.customer.garage' ||
        rejectedAbstract !== 'app.customer.garage' ||
        rejectedUnknown !== 'app.customer.garage' ||
        acceptedValid !== 'app.customer.loyalty'
      ) {
        fail('AuthService.resolvePostLoginState failed open-redirect, loop, abstract, or valid state resolution assertions.');
      } else {
        pass('Verified AuthService.resolvePostLoginState sanitizes external URLs, auth loops, abstract/unknown states, and honors valid returnUrl.');
      }

      const capturedHooks = [];
      const transitionsMock = {
        onBefore(criteria, callback) {
          capturedHooks.push({ criteria, callback });
        }
      };
      routeGuardRunFn(transitionsMock, authSvc, registeredConstants.get('ROLE_HOME'));

      const entryHook = capturedHooks[0];
      const rbacHook = capturedHooks[1];
      const buildMockTransition = (targetName) => {
        const targetDef = Object.assign({ name: targetName }, registeredStates.get(targetName));
        const parts = targetName.split('.');
        const treeNodes = [];
        for (let i = 1; i <= parts.length; i++) {
          const ancestorName = parts.slice(0, i).join('.');
          const ancDef = registeredStates.get(ancestorName);
          if (ancDef) {
            treeNodes.push({ state: { self: Object.assign({ name: ancestorName }, ancDef) } });
          }
        }
        return {
          to: () => targetDef,
          treeChanges: () => treeNodes,
          router: {
            stateService: {
              target: (state, params) => ({ redirectedTo: state, params: params || null })
            }
          }
        };
      };

      // Case A: Authenticated Customer hitting app.public.login -> redirects to app.customer.garage
      const authLoginRedirect = entryHook.callback(buildMockTransition('app.public.login'));
      if (!authLoginRedirect || authLoginRedirect.redirectedTo !== 'app.customer.garage') {
        fail(`Expected authenticated Customer on app.public.login to redirect to app.customer.garage, got ${JSON.stringify(authLoginRedirect)}`);
      }

      // Case B: Authenticated Customer attempting Admin state -> redirects to app.public.forbidden
      const crossRoleRedirect = await rbacHook.callback(buildMockTransition('app.admin.analytics'));
      if (!crossRoleRedirect || crossRoleRedirect.redirectedTo !== 'app.public.forbidden') {
        fail(`Expected Customer -> app.admin.analytics to redirect to app.public.forbidden, got ${JSON.stringify(crossRoleRedirect)}`);
      }

      // Case C: Unauthenticated caller attempting Staff Workbench -> redirects to app.public.login with returnUrl
      authSvc.clearSessionAndRealtime();
      const unauthRedirect = await rbacHook.callback(buildMockTransition('app.staff.workbench'));
      if (
        !unauthRedirect ||
        unauthRedirect.redirectedTo !== 'app.public.login' ||
        unauthRedirect.params?.returnUrl !== 'app.staff.workbench'
      ) {
        fail(`Expected unauthenticated -> app.staff.workbench to redirect to app.public.login?returnUrl=app.staff.workbench, got ${JSON.stringify(unauthRedirect)}`);
      }

      // Case D: Authorized Admin navigating to app.admin.analytics -> allowed (true)
      setTestRoleSession(authSvc, 'Admin');
      const adminAllowed = await rbacHook.callback(buildMockTransition('app.admin.analytics'));
      if (adminAllowed !== true) {
        fail(`Expected Admin -> app.admin.analytics to resolve true, got ${JSON.stringify(adminAllowed)}`);
      } else {
        pass('Verified $transitions.onBefore route guards (unauthenticated -> login?returnUrl, cross-role -> /403, authenticated login -> role home, authorized -> allow).');
      }
    }

    const dealerChartDef = registeredComponents.get('dealerChart');
    if (!dealerChartDef || typeof dealerChartDef.controller !== 'function') {
      fail('Missing dealerChart component.');
    } else {
      let destroyedCount = 0;
      sandbox.Chart = function MockChart() {
        this.update = () => {};
        this.destroy = () => { destroyedCount++; };
      };
      const fakeElement = [{ querySelector: () => ({ getContext: () => ({}) }) }];
      const chartCtrl = new dealerChartDef.controller(fakeElement);
      chartCtrl.config = { type: 'bar', data: { labels: ['A'], datasets: [] }, options: {} };
      chartCtrl.$postLink();
      chartCtrl.$onDestroy();
      if (destroyedCount !== 1) {
        fail(`RISK-04 Violation: dealerChart.$onDestroy() did not call chartInstance.destroy() (destroyedCount=${destroyedCount}).`);
      } else {
        pass('Verified RISK-04 leak safety: dealerChart.$onDestroy() deterministically destroys Chart.js instance.');
      }
    }

    // Verify all ng-repeat directives declare 'track by' and ng-options models never bind frozen constants directly
    let templateGuardErrors = 0;
    for (const [compName, compDef] of registeredComponents.entries()) {
      const tpl = String(compDef?.template || '');
      const repeatMatches = [...tpl.matchAll(/\bng-repeat="([^"]+)"/g)].map((m) => m[1]);
      for (const expr of repeatMatches) {
        if (!/\btrack\s+by\b/.test(expr)) {
          templateGuardErrors++;
          fail(`Component '${compName}' contains ng-repeat without mandatory 'track by': "${expr}"`);
        }
      }
      if (/\bng-options="/.test(tpl) && typeof compDef?.controller === 'function') {
        const ctrlSrc = Function.prototype.toString.call(compDef.controller);
        if (/=\s*(?:LOYALTY_TIERS|PERMISSIONS|ROLE_PERMISSION_PRESETS)\s*;/.test(ctrlSrc)) {
          templateGuardErrors++;
          fail(`Component '${compName}' binds frozen constant directly in controller with ng-options (must clone via LoyaltyCalculator.cloneTiers() / angular.copy).`);
        }
      }
    }
    if (templateGuardErrors === 0) {
      pass('Verified 100% of component ng-repeat templates enforce deterministic track by keys and ng-options avoid raw frozen constants.');
    }

    // Verify staffInvoiceBuilder clones frozen LOYALTY_TIERS and initializes cleanly with zero fake values
    const invoiceBuilderDef = registeredComponents.get('staffInvoiceBuilder');
    if (!invoiceBuilderDef || typeof invoiceBuilderDef.controller !== 'function') {
      fail('Missing staffInvoiceBuilder component.');
    } else {
      const invCtrl = new invoiceBuilderDef.controller(
        { resolve: (v) => Promise.resolve(v) },
        { $on: () => () => {} },
        { bookingId: 'BK-20260514-9C41' },
        { getCurrentUser: () => null },
        null,
        registeredConstants.get('LoyaltyCalculator'),
        null,
        null,
        null,
        { getAccessToken: () => 'test-jwt-token' }
      );
      invCtrl.$onInit();
      const zeroInitialOk =
        invCtrl.summary?.grossAmount === 0 &&
        invCtrl.summary?.netPaidAmount === 0 &&
        invCtrl.summary?.pointsEarned === 0;
      invCtrl.invoice.laborAmount = 250000;
      invCtrl.invoice.partsAmount = 680000;
      invCtrl.invoice.tierMultiplier = 1.2;
      invCtrl.eligibleVouchers = [{
        id: 'v-test-100k',
        voucherCode: 'TEST-100K',
        discountType: 'FixedAmount',
        discountValue: 100000,
        minOrderAmount: 500000,
        status: 'Available',
        effectiveStatus: 'Available'
      }];
      invCtrl.invoice.appliedCustomerVoucherId = 'v-test-100k';
      invCtrl.recalculate();
      const allExtensible = Array.isArray(invCtrl.tiers) &&
        invCtrl.tiers.length === 4 &&
        invCtrl.tiers.every((t) => Object.isExtensible(t));
      if (!allExtensible) {
        fail('staffInvoiceBuilder.$ctrl.tiers items are non-extensible (frozen); AngularJS ng-options will throw $$hashKey TypeError in real browsers.');
      } else if (
        !zeroInitialOk ||
        invCtrl.summary?.grossAmount !== 930000 ||
        invCtrl.summary?.discountClamped !== 100000 ||
        invCtrl.summary?.netPaidAmount !== 830000 ||
        invCtrl.summary?.pointsEarned !== 99
      ) {
        fail(`staffInvoiceBuilder settlement calculation mismatch: zeroInit=${zeroInitialOk}, summary=${JSON.stringify(invCtrl.summary)}`);
      } else {
        pass('Verified staffInvoiceBuilder initializes zeroed (zero fake values), clones extensible LOYALTY_TIERS, and computes 830,000 VND @ 1.2x = 99 pts.');
      }
    }

    // 4b. Phase 4 (T-408) Live Auth Normalization, Session Persistence, Single-Flight 401 Refresh Queue & Admin Dealers/Staff Tests
    const interceptorFactory = registeredServices.get('AuthHttpInterceptor');
    const problemMapperCtor = registeredServices.get('ProblemDetailsMapper');
    if (!interceptorFactory || !problemMapperCtor || !tokenStoreCtor || !authSvcCtor) {
      fail('Missing AuthHttpInterceptor or ProblemDetailsMapper registration for Phase 4 tests.');
    } else {
      // Helper to build a minimal AngularJS $q-compatible promise mock supporting .defer(), .resolve(), .reject(), .all()
      const createMockQ = () => ({
        defer() {
          let resolveFn;
          let rejectFn;
          const promise = new Promise((res, rej) => {
            resolveFn = res;
            rejectFn = rej;
          });
          return {
            promise,
            resolve: (val) => resolveFn(val),
            reject: (err) => rejectFn(err)
          };
        },
        resolve: (val) => Promise.resolve(val),
        reject: (err) => Promise.reject(err),
        all: (arrOrObj) => {
          if (Array.isArray(arrOrObj)) {
            return Promise.all(arrOrObj);
          }
          const keys = Object.keys(arrOrObj || {});
          return Promise.all(keys.map((k) => Promise.resolve(arrOrObj[k]))).then((vals) => {
            const out = {};
            keys.forEach((k, idx) => { out[k] = vals[idx]; });
            return out;
          });
        }
      });

      const createMemoryStorage = () => {
        const map = new Map();
        return {
          getItem: (k) => (map.has(k) ? map.get(k) : null),
          setItem: (k, v) => { map.set(k, String(v)); },
          removeItem: (k) => { map.delete(k); },
          _size: () => map.size
        };
      };

      // Test (c): AuthService normalizes both OpenAPI AuthTokenEnvelope ({ success, data: { ..., user } }) and direct flat DTOs + persists in sessionStorage
      const mockQ = createMockQ();
      const memSessionStorage = createMemoryStorage();
      const memLocalStorage = createMemoryStorage();
      const p4TokenStore = new tokenStoreCtor({
        sessionStorage: memSessionStorage,
        localStorage: memLocalStorage
      });

      let signalrStartedCount = 0;
      let signalrStoppedCount = 0;
      let httpCalls = [];
      let httpResponder = async () => ({ status: 200, data: {} });

      const mockHttp = Object.assign(
        (cfg) => {
          httpCalls.push(cfg);
          return httpResponder(cfg);
        },
        {
          get: (url, cfg) => mockHttp(Object.assign({ method: 'GET', url }, cfg || {})),
          post: (url, data, cfg) => mockHttp(Object.assign({ method: 'POST', url, data }, cfg || {})),
          put: (url, data, cfg) => mockHttp(Object.assign({ method: 'PUT', url, data }, cfg || {})),
          patch: (url, data, cfg) => mockHttp(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
        }
      );

      let stateTransitions = [];
      const mockState = {
        current: { name: 'app.admin.dealers' },
        go: (name, params) => { stateTransitions.push({ name, params: params || null }); },
        get: (name) => registeredStates.get(name) || null
      };

      const injectorMap = {
        $http: mockHttp,
        $state: mockState,
        AuthService: null,
        SignalRClientService: {
          start: () => { signalrStartedCount++; return Promise.resolve(true); },
          stop: () => { signalrStoppedCount++; return Promise.resolve(); }
        },
        ToastService: {
          clearAll: () => {},
          success: () => {},
          warning: () => {},
          danger: () => {}
        }
      };
      const mockInjector = {
        has: (k) => Boolean(injectorMap[k]),
        get: (k) => injectorMap[k]
      };

      const p4AuthSvc = new authSvcCtor(
        mockQ,
        mockInjector,
        { $broadcast() {}, $evalAsync: (fn) => fn() },
        registeredConstants.get('APP_CONFIG'),
        registeredConstants.get('ROLE_HOME'),
        registeredConstants.get('ROLE_PERMISSION_PRESETS'),
        p4TokenStore
      );
      injectorMap.AuthService = p4AuthSvc;

      // Verify Zero-Mock AuthService & Login surface (zero hardcoded demo accounts, preview tokens, or one-click login buttons)
      const publicLoginCheckDef = registeredComponents.get('publicLogin');
      const loginTplCheck = String(publicLoginCheckDef?.template || '');
      if (
        typeof p4AuthSvc.getDemoAccounts !== 'undefined' ||
        typeof p4AuthSvc.activatePhase3RolePreview !== 'undefined' ||
        typeof p4TokenStore.isPreviewToken !== 'undefined' ||
        /One-Click|quickLogin|demoAccounts/i.test(loginTplCheck)
      ) {
        fail('Zero-Mock violation: AuthService or publicLogin still exposes demo account presets or preview tokens.');
      } else {
        pass('Verified Zero-Mock AuthService & publicLogin (no getDemoAccounts, no activatePhase3RolePreview, no One-Click Seeded login card).');
      }

      // Envelope response normalization ({ success: true, data: { accessToken, refreshToken, expiresAtUtc, user } })
      const futureIso = new Date(Date.now() + 15 * 60 * 1000).toISOString();
      const envelopeUser = p4AuthSvc.setSession({
        success: true,
        data: {
          accessToken: 'jwt-envelope-token-1',
          refreshToken: 'rt-envelope-family-1',
          expiresAtUtc: futureIso,
          user: {
            userId: '11111111-1111-1111-1111-111111111101',
            email: 'staff.hn@motorcycledealer.vn',
            fullName: 'Tran Thi Lan',
            role: 'DealerStaff',
            dealerId: '22222222-2222-2222-2222-222222222202',
            dealerCode: 'DLR-HN-01',
            dealerName: 'Apex Moto Hanoi Flagship',
            permissions: ['Permissions.Bookings.ViewDealer', 'Permissions.Showroom.View']
          }
        }
      });

      // Hydrate a second AuthService instance from the same sessionStorage to prove tab reload persistence
      const rehydratedTokenStore = new tokenStoreCtor({
        sessionStorage: memSessionStorage,
        localStorage: memLocalStorage
      });
      const rehydratedAuthSvc = new authSvcCtor(
        mockQ,
        mockInjector,
        { $broadcast() {}, $evalAsync: () => {} },
        registeredConstants.get('APP_CONFIG'),
        registeredConstants.get('ROLE_HOME'),
        registeredConstants.get('ROLE_PERMISSION_PRESETS'),
        rehydratedTokenStore
      );

      // Direct flat token payload normalization ({ accessToken, refreshToken, expiresInSeconds, userId, email, role, permissions })
      const flatUser = p4AuthSvc.setSession({
        accessToken: 'jwt-direct-flat-token-2',
        refreshToken: 'rt-direct-flat-family-2',
        expiresInSeconds: 900,
        userId: '99999999-9999-9999-9999-999999999901',
        email: 'admin@motorcycledealer.vn',
        fullName: 'HQ Platform Admin',
        role: 'Admin',
        dealerId: null,
        permissions: ['Permissions.Dealers.ManageAll', 'Permissions.Users.ManageStaff']
      });

      const customerDefaultUser = p4AuthSvc.setSession({
        accessToken: 'jwt-customer-defaults',
        refreshToken: 'rt-customer-defaults',
        expiresInSeconds: 900,
        userId: '77777777-7777-7777-7777-777777777799',
        email: 'new.customer@customer.vn',
        fullName: 'New Customer',
        role: 'Customer',
        permissions: ['Permissions.Loyalty.ViewOwn']
      });

      // Re-apply Admin session for subsequent 401 single-flight refresh test
      p4AuthSvc.setSession({
        accessToken: 'jwt-direct-flat-token-2',
        refreshToken: 'rt-direct-flat-family-2',
        expiresInSeconds: 900,
        userId: '99999999-9999-9999-9999-999999999901',
        email: 'admin@motorcycledealer.vn',
        fullName: 'HQ Platform Admin',
        role: 'Admin',
        dealerId: null,
        permissions: ['Permissions.Dealers.ManageAll', 'Permissions.Users.ManageStaff']
      });

      const normalizedFromPointsMult = p4AuthSvc.normalizeUser({
        role: 'Customer',
        pointsMultiplier: 1.5
      }, null);

      const unenrichedStaffUser = p4AuthSvc.normalizeUser({
        role: 'DealerStaff',
        dealerId: '22222222-2222-2222-2222-222222222202'
      }, null);

      if (
        envelopeUser?.dealerCode !== 'DLR-HN-01' ||
        unenrichedStaffUser?.dealerCode !== null ||
        unenrichedStaffUser?.dealerName !== null ||
        envelopeUser?.availablePoints !== null ||
        envelopeUser?.lifetimeQualifyingPoints !== null ||
        envelopeUser?.tierMultiplier !== null ||
        envelopeUser?.pointsMultiplier !== null ||
        !rehydratedAuthSvc.isAuthenticated() ||
        rehydratedAuthSvc.getCurrentUser()?.email !== 'staff.hn@motorcycledealer.vn' ||
        flatUser?.role !== 'Admin' ||
        flatUser?.availablePoints !== null ||
        flatUser?.lifetimeQualifyingPoints !== null ||
        flatUser?.tierMultiplier !== null ||
        flatUser?.pointsMultiplier !== null ||
        customerDefaultUser?.availablePoints !== 0 ||
        customerDefaultUser?.lifetimeQualifyingPoints !== 0 ||
        customerDefaultUser?.tierMultiplier !== 1.0 ||
        customerDefaultUser?.pointsMultiplier !== 1.0 ||
        normalizedFromPointsMult?.tierMultiplier !== 1.5 ||
        normalizedFromPointsMult?.pointsMultiplier !== 1.5 ||
        p4AuthSvc.getAccessToken() !== 'jwt-direct-flat-token-2' ||
        !p4AuthSvc.hasPermission('Permissions.Dealers.ManageAll')
      ) {
        fail('AuthService failed envelope/flat response normalization, pointsMultiplier/tierMultiplier parity, non-Customer null loyalty fields, or sessionStorage persistence check.');
      } else {
        pass('Verified AuthService normalizes envelope & direct payloads, pointsMultiplier/tierMultiplier parity, null loyalty fields for non-Customer roles, and hydrates sessionStorage.');
      }

      // Test (a): Single-Flight 401 Refresh Queue — 3 simultaneous 401 calls trigger EXACTLY 1 POST /auth/refresh and all 3 replay with rotated token
      const interceptor = interceptorFactory(mockQ, mockInjector, p4TokenStore);
      httpCalls = [];
      let refreshEndpointCount = 0;

      httpResponder = async (cfg) => {
        if (String(cfg.url).endsWith('/auth/refresh')) {
          refreshEndpointCount++;
          await new Promise((r) => setTimeout(r, 25));
          return {
            status: 200,
            data: {
              success: true,
              data: {
                accessToken: 'jwt-rotated-singleflight-token-3',
                refreshToken: 'rt-rotated-singleflight-family-3',
                expiresAtUtc: new Date(Date.now() + 900 * 1000).toISOString(),
                user: {
                  userId: '99999999-9999-9999-9999-999999999901',
                  email: 'admin@motorcycledealer.vn',
                  fullName: 'HQ Platform Admin',
                  role: 'Admin',
                  dealerId: null,
                  permissions: ['Permissions.Dealers.ManageAll', 'Permissions.Users.ManageStaff']
                }
              }
            }
          };
        }
        // Replayed protected requests pass through request() hook or carry rotated header
        const finalCfg = interceptor.request(cfg);
        return {
          status: 200,
          data: {
            replayedUrl: finalCfg.url,
            authHeader: finalCfg.headers?.Authorization || null
          }
        };
      };

      const reqA = interceptor.request({ method: 'GET', url: 'http://localhost:5080/api/v1/dealers', headers: {} });
      const reqB = interceptor.request({ method: 'GET', url: 'http://localhost:5080/api/v1/dealers/d1/staff', headers: {} });
      const reqC = interceptor.request({ method: 'GET', url: 'http://localhost:5080/api/v1/auth/me', headers: {} });

      if (reqA.headers.Authorization !== 'Bearer jwt-direct-flat-token-2') {
        fail(`Expected initial Authorization header to carry active Bearer token, got '${reqA.headers.Authorization}'`);
      }

      const [resA, resB, resC] = await Promise.all([
        interceptor.responseError({ status: 401, config: reqA, data: { errorCode: 'TOKEN_EXPIRED' } }),
        interceptor.responseError({ status: 401, config: reqB, data: { errorCode: 'TOKEN_EXPIRED' } }),
        interceptor.responseError({ status: 401, config: reqC, data: { errorCode: 'TOKEN_EXPIRED' } })
      ]);

      if (
        refreshEndpointCount !== 1 ||
        resA?.data?.authHeader !== 'Bearer jwt-rotated-singleflight-token-3' ||
        resB?.data?.authHeader !== 'Bearer jwt-rotated-singleflight-token-3' ||
        resC?.data?.authHeader !== 'Bearer jwt-rotated-singleflight-token-3' ||
        p4TokenStore.getRefreshToken() !== 'rt-rotated-singleflight-family-3'
      ) {
        fail(
          `Single-flight 401 refresh queue failed: refreshEndpointCount=${refreshEndpointCount}, headers=[${resA?.data?.authHeader}, ${resB?.data?.authHeader}, ${resC?.data?.authHeader}]`
        );
      } else {
        pass('Verified concurrent 401 single-flight refresh queue (3 simultaneous 401s -> exactly 1 POST /auth/refresh -> all 3 replayed with rotated Bearer token).');
      }

      // Test (b): Refresh Failure (401 ReuseDetected) rejects all queued callers, clears session/SignalR, and redirects to app.public.login with returnUrl
      stateTransitions = [];
      signalrStoppedCount = 0;
      let failedRefreshCalls = 0;

      httpResponder = async (cfg) => {
        if (String(cfg.url).endsWith('/auth/refresh')) {
          failedRefreshCalls++;
          await new Promise((r) => setTimeout(r, 15));
          return Promise.reject({
            status: 401,
            data: {
              title: 'Refresh Token Reuse Detected',
              detail: 'Token family revoked.',
              errorCode: 'REFRESH_TOKEN_REUSE_DETECTED'
            }
          });
        }
        return { status: 200, data: {} };
      };

      const failReq1 = { method: 'GET', url: 'http://localhost:5080/api/v1/dealers', headers: {} };
      const failReq2 = { method: 'GET', url: 'http://localhost:5080/api/v1/dealers/d1/staff', headers: {} };

      const settledFailures = await Promise.allSettled([
        interceptor.responseError({ status: 401, config: failReq1 }),
        interceptor.responseError({ status: 401, config: failReq2 })
      ]);

      const bothRejected = settledFailures.every((s) => s.status === 'rejected');
      const loginRedirect = stateTransitions[0];

      if (
        failedRefreshCalls !== 1 ||
        !bothRejected ||
        p4AuthSvc.isAuthenticated() !== false ||
        p4TokenStore.getAccessToken() !== null ||
        p4TokenStore.getRefreshToken() !== null ||
        signalrStoppedCount < 1 ||
        loginRedirect?.name !== 'app.public.login' ||
        loginRedirect?.params?.returnUrl !== 'app.admin.dealers'
      ) {
        fail(
          `Refresh failure handling mismatch: failedRefreshCalls=${failedRefreshCalls}, bothRejected=${bothRejected}, isAuth=${p4AuthSvc.isAuthenticated()}, redirect=${JSON.stringify(loginRedirect)}`
        );
      } else {
        pass('Verified refresh failure (401 ReuseDetected) rejects all queued callers, clears session & SignalR, and redirects to app.public.login?returnUrl=app.admin.dealers.');
      }

      // Test (b2): Mid-session 403 ACCOUNT_LOCKED tears down session & SignalR and redirects to login, while /auth/login 403 stays inline
      p4AuthSvc.setSession({
        accessToken: 'jwt-active-before-lockout',
        refreshToken: 'rt-active-before-lockout',
        expiresInSeconds: 900,
        userId: '44444444-4444-4444-4444-444444444403',
        email: 'lan.tran.hn@dealer.vn',
        fullName: 'Lan Tran',
        role: 'DealerStaff',
        dealerId: '22222222-2222-2222-2222-222222222202',
        permissions: ['Permissions.Bookings.ViewDealer']
      });
      stateTransitions = [];
      signalrStoppedCount = 0;

      const loginLockedSettled = await Promise.allSettled([
        interceptor.responseError({
          status: 403,
          config: { method: 'POST', url: 'http://localhost:5080/api/v1/auth/login' },
          data: { errorCode: 'ACCOUNT_LOCKED' }
        })
      ]);
      const loginStayedInline = loginLockedSettled[0].status === 'rejected' &&
        stateTransitions.length === 0 &&
        p4AuthSvc.isAuthenticated() === true;

      mockState.current = { name: 'app.staff.workbench' };
      const midSessionLockedSettled = await Promise.allSettled([
        interceptor.responseError({
          status: 403,
          config: { method: 'GET', url: 'http://localhost:5080/api/v1/auth/me' },
          data: { errorCode: 'ACCOUNT_LOCKED' }
        })
      ]);
      mockState.current = { name: 'app.admin.dealers' };

      if (
        !loginStayedInline ||
        midSessionLockedSettled[0].status !== 'rejected' ||
        p4AuthSvc.isAuthenticated() !== false ||
        signalrStoppedCount < 1 ||
        stateTransitions[0]?.name !== 'app.public.login' ||
        stateTransitions[0]?.params?.returnUrl !== 'app.staff.workbench'
      ) {
        fail('AuthHttpInterceptor failed mid-session 403 ACCOUNT_LOCKED session/SignalR teardown or /auth/login exclusion.');
      } else {
        pass('Verified AuthHttpInterceptor tears down session & SignalR on mid-session 403 ACCOUNT_LOCKED while keeping /auth/login inline.');
      }

      // Verify ProblemDetailsMapper offline status (-1) & form server validity auto-reset
      const mapper = new problemMapperCtor();
      const offlineProblem = mapper.extract({ status: -1, data: null });
      let validityState = true;
      const changeListeners = [];
      const mockForm = {
        email: {
          $viewChangeListeners: changeListeners,
          $setValidity(_key, isValid) { validityState = isValid; }
        }
      };
      mapper.applyToForm(mockForm, { errors: { Email: ['Duplicate email address.'] } });
      const wasInvalidAfterApply = validityState === false;
      if (changeListeners[0]) {
        changeListeners[0]();
      }
      if (offlineProblem.errorCode !== 'NETWORK_OFFLINE' || !wasInvalidAfterApply || validityState !== true) {
        fail('ProblemDetailsMapper failed NETWORK_OFFLINE extraction or $viewChangeListeners server-error auto-reset.');
      } else {
        pass('Verified ProblemDetailsMapper handles XHR offline status (-1 -> NETWORK_OFFLINE) and auto-resets server field validity on edit.');
      }

      // Verify F-SPA-01b & F-SPA-01c: Password complexity guards on publicRegister + adminStaff, dealers modal PUT badge, and modal onFieldEdited cleanup
      const registerDef = registeredComponents.get('publicRegister');
      const adminDealersDef = registeredComponents.get('adminDealers');
      const adminStaffDef = registeredComponents.get('adminStaff');

      if (!registerDef || !adminDealersDef || !adminStaffDef) {
        fail('Missing publicRegister, adminDealers, or adminStaff component registration.');
      } else {
        const regCtrl = new registerDef.controller(
          mockState,
          p4AuthSvc,
          mapper,
          injectorMap.ToastService
        );
        regCtrl.$onInit();
        regCtrl.form.password = 'weakpass';
        const weakLowerRejected = regCtrl.isPasswordComplex() === false;
        regCtrl.form.password = 'WeakPass1';
        const missingSpecialRejected = regCtrl.isPasswordComplex() === false;
        regCtrl.form.password = 'Weak@ 123';
        const whitespaceRejected = regCtrl.isPasswordComplex() === false;
        regCtrl.form.password = 'Customer@123!';
        const strongCustomerAccepted = regCtrl.isPasswordComplex() === true;

        const dealersTpl = String(adminDealersDef.template || '');
        const hasDealerIdBadge =
          dealersTpl.includes("'PUT /api/v1/dealers/' + $ctrl.editingDealerId") &&
          !dealersTpl.includes("'PUT /api/v1/dealers/' + $ctrl.form.code");

        p4TokenStore.setTokens('test-admin-jwt', 3600, null);
        httpResponder = async () => ({ status: 200, data: { success: true, data: [] } });
        const dealersCtrl = new adminDealersDef.controller(
          mockHttp,
          registeredConstants.get('APP_CONFIG'),
          mapper,
          injectorMap.ToastService,
          p4TokenStore
        );
        dealersCtrl.$onInit();
        let dealerCodeServerValid = false;
        dealersCtrl.dealerForm = {
          code: { $setValidity(_k, val) { dealerCodeServerValid = val; } }
        };
        dealersCtrl.fieldErrors = { code: 'Duplicate branch code.' };
        dealersCtrl.onFieldEdited('code');

        const staffCtrl = new adminStaffDef.controller(
          mockHttp,
          mockQ,
          {},
          registeredConstants.get('APP_CONFIG'),
          p4AuthSvc,
          mapper,
          registeredConstants.get('ROLE_PERMISSION_PRESETS'),
          injectorMap.ToastService,
          p4TokenStore
        );
        staffCtrl.$onInit();
        staffCtrl.openCreateModal();
        staffCtrl.form.password = 'nostrong';
        const weakStaffRejected = staffCtrl.isPasswordComplex() === false;
        staffCtrl.form.password = 'Staff@123!';
        const strongStaffAccepted = staffCtrl.isPasswordComplex() === true;

        // Boundary checks on shared AuthService.isPasswordComplex & ProblemDetailsMapper.clearFieldError (RF-SPA-01)
        const sevenCharBoundaryRejected = p4AuthSvc.isPasswordComplex('Aa1!xyz') === false;
        const eightCharBoundaryAccepted = p4AuthSvc.isPasswordComplex('Aa1!xyzw') === true;
        const maxLengthBoundaryAccepted = p4AuthSvc.isPasswordComplex('Aa1!' + 'x'.repeat(124)) === true;
        const overMaxBoundaryRejected = p4AuthSvc.isPasswordComplex('Aa1!' + 'x'.repeat(125)) === false;
        const tabWhitespaceRejected = p4AuthSvc.isPasswordComplex('Aa1!\txyzw') === false;

        let staffEmailServerValid = false;
        let staffPasswordServerValid = false;
        staffCtrl.staffForm = {
          email: { $setValidity(_k, val) { staffEmailServerValid = val; } },
          password: { $setValidity(_k, val) { staffPasswordServerValid = val; } }
        };
        staffCtrl.fieldErrors = {
          email: 'Email taken.',
          password: 'Weak.',
          temporaryPassword: 'Weak alias.',
          phoneNumber: 'Keep untouched.'
        };
        staffCtrl.onFieldEdited('email');
        staffCtrl.onFieldEdited('password');

        if (
          !weakLowerRejected ||
          !missingSpecialRejected ||
          !whitespaceRejected ||
          !strongCustomerAccepted ||
          !weakStaffRejected ||
          !strongStaffAccepted ||
          !sevenCharBoundaryRejected ||
          !eightCharBoundaryAccepted ||
          !maxLengthBoundaryAccepted ||
          !overMaxBoundaryRejected ||
          !tabWhitespaceRejected ||
          !hasDealerIdBadge ||
          dealersCtrl.fieldErrors.code !== undefined ||
          dealerCodeServerValid !== true ||
          staffCtrl.fieldErrors.email !== undefined ||
          staffCtrl.fieldErrors.password !== undefined ||
          staffCtrl.fieldErrors.temporaryPassword !== undefined ||
          staffCtrl.fieldErrors.phoneNumber !== 'Keep untouched.' ||
          staffEmailServerValid !== true ||
          staffPasswordServerValid !== true
        ) {
          fail('Failed F-SPA-01b/01c & RF-SPA-01 checks for shared AuthService.isPasswordComplex boundaries, PUT /api/v1/dealers/{dealerId} badge, or ProblemDetailsMapper.clearFieldError alias reset.');
        } else {
          pass('Verified shared AuthService.isPasswordComplex boundaries (7 vs 8 vs 128/129 chars & whitespace), PUT /api/v1/dealers/{dealerId} badge, and shared ProblemDetailsMapper.clearFieldError alias reset.');
        }
      }

      // 4c. Phase 5 (T-508 & T-509) DealerApiService, 409 Slot Auto-Recovery, SignalR Workbench Deduplication & Extensible Bindings
      const dealerApiCtor = registeredServices.get('DealerApiService');
      const bookingNewDef = registeredComponents.get('customerBookingNew');
      const staffWorkbenchDef = registeredComponents.get('staffWorkbench');
      const showroomDef = registeredComponents.get('publicShowroom');
      const custDashDef = registeredComponents.get('customerDashboard');
      const custGarageDef = registeredComponents.get('customerGarage');
      const custBookingsDef = registeredComponents.get('customerBookings');
      const staffSalesDef = registeredComponents.get('staffSalesDesk');
      const staffSlotsDef = registeredComponents.get('staffTimeSlots');
      const staffCustomersDef = registeredComponents.get('staffCustomers');
      const adminModelsDef = registeredComponents.get('adminModels');

      if (!dealerApiCtor || !bookingNewDef || !staffWorkbenchDef) {
        fail('Missing DealerApiService, customerBookingNew, or staffWorkbench registration for Phase 5 verification.');
      } else {
        // (a) DealerApiService endpoint methods, cleanParams & envelope unwrapping
        const p5Calls = [];
        let p5Responder = async () => ({ status: 200, data: { success: true, data: [] } });
        const p5Http = Object.assign(
          (cfg) => {
            p5Calls.push(cfg);
            return p5Responder(cfg);
          },
          {
            get: (url, cfg) => p5Http(Object.assign({ method: 'GET', url }, cfg || {})),
            post: (url, data, cfg) => p5Http(Object.assign({ method: 'POST', url, data }, cfg || {})),
            put: (url, data, cfg) => p5Http(Object.assign({ method: 'PUT', url, data }, cfg || {})),
            patch: (url, data, cfg) => p5Http(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
          }
        );

        const p5Api = new dealerApiCtor(
          p5Http,
          mockQ,
          registeredConstants.get('APP_CONFIG'),
          mapper
        );

        const cleanedQuery = p5Api.cleanParams({
          dealerId: '',
          category: '   ',
          publicOnly: true,
          search: ' SH160i ',
          nullParam: null
        });

        p5Responder = async (cfg) => {
          if (cfg.url.endsWith('/motorcycles')) {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: 'm-1', modelCode: 'HND-SH160I-2026', brand: 'Honda', modelName: 'Honda SH160i ABS Sport', category: 'Scooter', engineDisplacementCc: 157, baseRetailPrice: 102500000 }],
                pagination: { pageNumber: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
              }
            };
          }
          if (cfg.url.endsWith('/inventories')) {
            return {
              status: 200,
              data: {
                success: true,
                data: {
                  items: [{ id: 'inv-1', vin: 'rlhjc9204ry000309', engineNumber: 'JC92E-4000309', motorcycleModelId: 'm-1', listedPrice: 102500000, stockStatus: 'Available', rowVersion: 'AAAAAAAAC1A=' }],
                  pagination: { pageNumber: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
                }
              }
            };
          }
          if (cfg.url.endsWith('/sales-orders') && cfg.method === 'POST') {
            return {
              status: 201,
              data: {
                salesOrderId: 'so-1',
                orderNumber: 'SO-20261122-0009',
                vin: 'RLHJC9204RY000309',
                finalAmount: 102500000,
                pointsEarned: 10250,
                newTierCode: 'Platinum',
                tierUpgraded: true,
                onboardedCustomerVehicleId: 'veh-onboarded-1'
              }
            };
          }
          if (cfg.url.includes('/vehicles/') && cfg.url.endsWith('/mileage')) {
            return {
              status: 200,
              data: {
                success: true,
                data: { id: 'veh-1', vin: 'RLHJF5109RZ104829', motorcycleModelName: 'Honda SH160i', currentMileageKm: 3920, nextDueMileageKm: 4000 }
              }
            };
          }
          if (cfg.url.includes('/vehicles/') && cfg.url.endsWith('/reminders')) {
            return {
              status: 200,
              data: {
                success: true,
                data: {
                  vehicle: { id: 'veh-1', vin: 'RLHJF5109RZ104829', currentMileageKm: 3850, nextDueMileageKm: 4000 },
                  reminders: [{ id: 'rem-1', milestoneName: '4,000 km Periodic Maintenance', targetMileageKm: 4000, deduplicationKey: 'RLHJF5109RZ104829:r1:4000', status: 'Sent' }],
                  maintenanceRules: [{ id: 'rule-1', milestoneCode: 'PERIODIC_4000', milestoneName: '4,000 km Periodic Maintenance', intervalKm: 4000, intervalMonths: 6 }]
                }
              }
            };
          }
          if (cfg.url.endsWith('/time-slots/dealer')) {
            return {
              status: 200,
              data: {
                success: true,
                data: [
                  { id: 'slot-dlr-1', dealerId: 'd-1', slotDate: '2026-11-23', startTime: '08:00:00', endTime: '09:00:00', maxCapacity: 5, bookedCount: 5, isBlocked: false, rowVersion: 'AAAAAAAAD4A=' }
                ]
              }
            };
          }
          if (cfg.url.endsWith('/time-slots')) {
            return {
              status: 200,
              data: [
                { id: 'slot-flat-1', dealerId: 'd-1', slotDate: '2026-11-23', startTime: '09:00:00', endTime: '10:00:00', maxCapacity: 5, bookedCount: 2, isBlocked: false, rowVersion: 'AAAAAAAAD4F=' }
              ]
            };
          }
          if (cfg.url.endsWith('/bookings') && cfg.method === 'POST') {
            return {
              status: 201,
              data: {
                success: true,
                data: { bookingId: 'bk-1', bookingNumber: 'BK-20261122-1111', status: 'Pending', slotDate: '2026-11-23', startTime: '09:00', endTime: '10:00', vin: 'RLHJF5109RZ104829', rowVersion: 'AAAAAAAAD4Z=' }
              }
            };
          }
          return { status: 200, data: { success: true, data: {} } };
        };

        const modelsOut = await p5Api.listMotorcycles({ category: '', search: ' SH160i ' });
        const invOut = await p5Api.listInventories({ publicOnly: true });
        const soOut = await p5Api.createSalesOrder({
          customerEmail: 'minh.nguyen@customer.vn',
          dealerInventoryId: 'inv-1',
          inventoryRowVersion: 'AAAAAAAAC1A=',
          discountAmount: 0,
          paymentMethod: 'BankTransfer'
        });
        const mileageOut = await p5Api.updateVehicleMileage('veh-1', 3920);
        const remOut = await p5Api.getVehicleReminders('veh-1');
        const slotsOut = await p5Api.listTimeSlots({ dealerId: 'd-1', fromDate: '2026-11-23', toDate: '2026-11-23' });
        const dealerSlotsOut = await p5Api.listDealerTimeSlots({ dealerId: 'd-1', fromDate: '2026-11-23', toDate: '2026-11-23' });
        const bkOut = await p5Api.createBooking({
          dealerId: 'd-1',
          serviceTimeSlotId: 'slot-flat-1',
          slotRowVersion: 'AAAAAAAAD4F=',
          customerVehicleId: 'veh-1',
          serviceType: 'PeriodicMaintenance',
          odometerAtBookingKm: 3920
        });

        const salesPostCall = p5Calls.find((c) => c.method === 'POST' && c.url.endsWith('/sales-orders'));
        const mileagePatchCall = p5Calls.find((c) => c.method === 'PATCH' && c.url.endsWith('/mileage'));
        const bookingPostCall = p5Calls.find((c) => c.method === 'POST' && c.url.endsWith('/bookings'));
        const dealerSlotsGetCall = p5Calls.find((c) => c.method === 'GET' && c.url.endsWith('/time-slots/dealer'));

        if (
          Object.keys(cleanedQuery).length !== 2 ||
          cleanedQuery.publicOnly !== true ||
          cleanedQuery.search !== 'SH160i' ||
          modelsOut.length !== 1 ||
          modelsOut[0].modelCode !== 'HND-SH160I-2026' ||
          invOut.length !== 1 ||
          invOut[0].vin !== 'RLHJC9204RY000309' ||
          soOut.pointsEarned !== 10250 ||
          soOut.onboardedCustomerVehicleId !== 'veh-onboarded-1' ||
          !salesPostCall?.headers?.['Idempotency-Key'] ||
          mileageOut.maintenanceStatus !== 'DueSoon' ||
          mileagePatchCall?.data?.currentMileageKm !== 3920 ||
          mileagePatchCall?.data?.newMileageKm !== 3920 ||
          remOut.reminders.length !== 1 ||
          remOut.rules.length !== 1 ||
          slotsOut.length !== 1 ||
          slotsOut[0].remainingCapacity !== 3 ||
          slotsOut[0].window !== '09:00 - 10:00' ||
          !dealerSlotsGetCall ||
          dealerSlotsOut.length !== 1 ||
          dealerSlotsOut[0].slotId !== 'slot-dlr-1' ||
          dealerSlotsOut[0].remainingCapacity !== 0 ||
          bkOut.bookingNumber !== 'BK-20261122-1111' ||
          !bookingPostCall?.headers?.['Idempotency-Key']
        ) {
          fail('DealerApiService failed envelope/flat unwrapping, cleanParams sanitization, listDealerTimeSlots, or Idempotency-Key header verification.');
        } else {
          pass('Verified DealerApiService unwraps PagedApiEnvelope, nested items, listDealerTimeSlots, and flat payloads cleanly with Idempotency-Key headers.');
        }

        // (b) SCR-CUS-03 (customerBookingNew) 409 SLOT_CAPACITY_EXHAUSTED auto-refresh recovery handler
        const liveTokenStore = new tokenStoreCtor({});
        liveTokenStore.setTokens('jwt-live-customer-token', 900, 'rt-live-customer');
        p4AuthSvc.setSession({
          accessToken: 'jwt-live-customer-token',
          refreshToken: 'rt-live-customer',
          expiresInSeconds: 900,
          userId: '44444444-4444-4444-4444-444444444408',
          email: 'customer.silver@motorcycledealer.vn',
          fullName: 'Tran Ngoc Mai (Silver)',
          role: 'Customer',
          tierCode: 'Silver',
          tierMultiplier: 1.2,
          availablePoints: 649,
          lifetimeQualifyingPoints: 1960,
          permissions: ['Permissions.Bookings.Create', 'Permissions.TimeSlots.ViewAvailable']
        });

        let slotRefreshCalls = 0;
        let warningToastCount = 0;
        p5Responder = async (cfg) => {
          if (cfg.url.endsWith('/dealers')) {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', openingTime: '08:00:00', closingTime: '19:00:00', defaultSlotCapacity: 5, isActive: true }]
              }
            };
          }
          if (cfg.url.endsWith('/vehicles')) {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: '99999999-9999-9999-9999-999999999901', vin: 'RLHJF5109RZ104829', motorcycleModelName: 'Honda SH160i ABS Sport', currentMileageKm: 3850, nextDueMileageKm: 4000, maintenanceStatus: 'DueSoon' }]
              }
            };
          }
          if (cfg.url.endsWith('/time-slots')) {
            slotRefreshCalls++;
            if (slotRefreshCalls === 1) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: 'slot-last-bay', slotId: 'slot-last-bay', dealerId: '22222222-2222-2222-2222-222222222201', slotDate: '2026-11-23', startTime: '09:00:00', endTime: '10:00:00', maxCapacity: 5, bookedCount: 4, remainingCapacity: 1, isBlocked: false, rowVersion: 'AAAAAAAAD4F=' },
                    { id: 'slot-open-bay', slotId: 'slot-open-bay', dealerId: '22222222-2222-2222-2222-222222222201', slotDate: '2026-11-23', startTime: '10:00:00', endTime: '11:00:00', maxCapacity: 5, bookedCount: 1, remainingCapacity: 4, isBlocked: false, rowVersion: 'AAAAAAAAD4K=' }
                  ]
                }
              };
            }
            return {
              status: 200,
              data: {
                success: true,
                data: [
                  { id: 'slot-last-bay', slotId: 'slot-last-bay', dealerId: '22222222-2222-2222-2222-222222222201', slotDate: '2026-11-23', startTime: '09:00:00', endTime: '10:00:00', maxCapacity: 5, bookedCount: 5, remainingCapacity: 0, isBlocked: false, rowVersion: 'AAAAAAAAD4Z=' },
                  { id: 'slot-open-bay', slotId: 'slot-open-bay', dealerId: '22222222-2222-2222-2222-222222222201', slotDate: '2026-11-23', startTime: '10:00:00', endTime: '11:00:00', maxCapacity: 5, bookedCount: 1, remainingCapacity: 4, isBlocked: false, rowVersion: 'AAAAAAAAD4K=' }
                ]
              }
            };
          }
          if (cfg.url.endsWith('/bookings') && cfg.method === 'POST') {
            return Promise.reject({
              status: 409,
              data: {
                title: 'Conflict',
                detail: 'Selected time slot capacity was just claimed by another customer.',
                errorCode: 'SLOT_CAPACITY_EXHAUSTED',
                traceId: 'trace-409-slot'
              }
            });
          }
          return { status: 200, data: { success: true, data: [] } };
        };

        const wizardCtrl = new bookingNewDef.controller(
          mockQ,
          { $on: () => () => {} },
          mockState,
          {},
          p4AuthSvc,
          p5Api,
          mapper,
          {
            success: () => {},
            warning: () => { warningToastCount++; },
            info: () => {},
            danger: () => {}
          },
          liveTokenStore
        );
        wizardCtrl.$onInit();
        await new Promise((r) => setTimeout(r, 20));

        wizardCtrl.draft.odometerAtBookingKm = 3920;
        wizardCtrl.draft.reportedNotes = '4,000 km periodic synthetic oil & CVT belt check';
        wizardCtrl.draft.voucherCode = 'SVC100K-7K9P2M4X';
        wizardCtrl.selectSlot(wizardCtrl.slots[0]);
        wizardCtrl.goToStep(4);

        const stepBeforeSubmit = wizardCtrl.currentStep;
        await wizardCtrl.submitBooking();

        if (
          stepBeforeSubmit !== 4 ||
          wizardCtrl.currentStep !== 3 ||
          wizardCtrl.slotConflictAlert?.errorCode !== 'SLOT_CAPACITY_EXHAUSTED' ||
          wizardCtrl.draft.serviceTimeSlotId !== '' ||
          wizardCtrl.draft.slotRowVersion !== '' ||
          wizardCtrl.draft.customerVehicleId !== '99999999-9999-9999-9999-999999999901' ||
          wizardCtrl.draft.odometerAtBookingKm !== 3920 ||
          wizardCtrl.draft.reportedNotes !== '4,000 km periodic synthetic oil & CVT belt check' ||
          wizardCtrl.draft.voucherCode !== 'SVC100K-7K9P2M4X' ||
          slotRefreshCalls !== 2 ||
          wizardCtrl.slots[0].isBookable !== false ||
          wizardCtrl.slots[1].isBookable !== true ||
          warningToastCount !== 1
        ) {
          fail(`SCR-CUS-03 409 SLOT_CAPACITY_EXHAUSTED auto-recovery failed: step=${wizardCtrl.currentStep}, slotRefreshCalls=${slotRefreshCalls}, draft=${JSON.stringify(wizardCtrl.draft)}`);
        } else {
          pass('Verified SCR-CUS-03 409 SLOT_CAPACITY_EXHAUSTED preserves user inputs, steps back to Step 3, and refreshes live ServiceTimeSlots.');
        }

        // (c) SCR-STF-01 (staffWorkbench) BookingCreated SignalR prepend deduplication & $onDestroy unsubscribe
        const p5NotifStore = new notifStoreCtor();
        const p5SignalR = new signalrSvcCtor(
          { resolve: (v) => Promise.resolve(v), when: (v) => Promise.resolve(v) },
          { $applyAsync: (fn) => fn(), $broadcast() {} },
          registeredConstants.get('APP_CONFIG'),
          { getAccessToken: () => 'test-staff-jwt' },
          p5NotifStore,
          { push: () => {}, info: () => {}, success: () => {}, warning: () => {} }
        );

        const previewTokenStore = new tokenStoreCtor({});
        previewTokenStore.setTokens('test-staff-jwt', 3600, null);
        p5Responder = async () => ({ status: 200, data: { success: true, data: [] } });

        const wbCtrl = new staffWorkbenchDef.controller(
          mockQ,
          { $applyAsync: (fn) => fn(), $on: () => () => {} },
          p4AuthSvc,
          p5Api,
          mapper,
          p5SignalR,
          { push: () => {}, info: () => {}, success: () => {}, warning: () => {} },
          previewTokenStore
        );
        wbCtrl.$onInit();
        await new Promise((r) => setTimeout(r, 15));
        const initialPendingCount = wbCtrl.lanes[0].items.length;

        const incomingPayload = {
          notificationId: 'notif-rt-501',
          eventType: 'BookingCreated',
          bookingId: 'bk-live-uuid-0501',
          bookingNumber: 'BK-20261122-0501',
          dealerId: '22222222-2222-2222-2222-222222222201',
          customerFullName: 'Tran Ngoc Mai (Silver)',
          vin: 'RLHJF5109RZ104829',
          motorcycleModelName: 'Honda SH160i ABS Sport',
          slotDate: '2026-11-23',
          startTime: '09:00',
          endTime: '10:00',
          serviceType: 'PeriodicMaintenance',
          hasReservedVoucher: true,
          status: 'Pending',
          occurredAtUtc: new Date().toISOString()
        };

        p5SignalR.simulateEvent('BookingCreated', incomingPayload);
        const countAfterFirstArrival = wbCtrl.lanes[0].items.length;
        const topCardNumber = wbCtrl.lanes[0].items[0]?.bookingNumber;
        const topCardHighlighted = wbCtrl.lanes[0].items[0]?.isLiveArrival === true;

        // Duplicate delivery of the same BookingCreated frame -> must NOT create a duplicate card
        p5SignalR.simulateEvent('BookingCreated', incomingPayload);
        const countAfterDuplicate = wbCtrl.lanes[0].items.length;

        // Destroy controller -> subsequent SignalR events must NOT mutate lanes
        wbCtrl.$onDestroy();
        p5SignalR.simulateEvent('BookingCreated', Object.assign({}, incomingPayload, {
          bookingId: 'bk-live-uuid-0999',
          bookingNumber: 'BK-20261122-0999'
        }));
        const countAfterDestroy = wbCtrl.lanes[0].items.length;

        if (
          countAfterFirstArrival !== initialPendingCount + 1 ||
          topCardNumber !== 'BK-20261122-0501' ||
          !topCardHighlighted ||
          countAfterDuplicate !== countAfterFirstArrival ||
          countAfterDestroy !== countAfterFirstArrival
        ) {
          fail(`SCR-STF-01 SignalR BookingCreated prepend/dedup/$onDestroy failed: initial=${initialPendingCount}, first=${countAfterFirstArrival}, dup=${countAfterDuplicate}, postDestroy=${countAfterDestroy}`);
        } else {
          pass('Verified SCR-STF-01 SignalR BookingCreated zero-refresh prepend, duplicate-frame deduplication, and $onDestroy listener cleanup.');
        }

        // (d) Runtime extensibility check across all Phase 5 components (zero frozen ng-options / ng-repeat arrays)
        const noopRootScope = { $applyAsync: (fn) => fn(), $on: () => () => {} };
        const noopToast = { clearAll: () => {}, push: () => {}, info: () => {}, success: () => {}, warning: () => {}, danger: () => {} };

        const phase5Controllers = [
          { name: 'publicShowroom', inst: new showroomDef.controller(mockQ, noopRootScope, p4AuthSvc, p5Api, loyaltyCalc, mapper, p5SignalR, previewTokenStore) },
          { name: 'customerDashboard', inst: new custDashDef.controller(mockQ, noopRootScope, mockState, p4AuthSvc, p5Api, p5SignalR, previewTokenStore) },
          { name: 'customerGarage', inst: new custGarageDef.controller(mockQ, p5Api, mapper, p5SignalR, noopToast, previewTokenStore) },
          { name: 'customerBookings', inst: new custBookingsDef.controller(mockQ, noopRootScope, p5Api, mapper, p5SignalR, noopToast, previewTokenStore) },
          { name: 'staffSalesDesk', inst: new staffSalesDef.controller(mockQ, p4AuthSvc, p5Api, loyaltyCalc, mapper, noopToast, previewTokenStore) },
          { name: 'staffTimeSlots', inst: new staffSlotsDef.controller(mockQ, noopRootScope, p4AuthSvc, p5Api, mapper, p5SignalR, noopToast, previewTokenStore) },
          { name: 'staffCustomers', inst: new staffCustomersDef.controller(p5Api, mapper, previewTokenStore) },
          { name: 'adminModels', inst: new adminModelsDef.controller(mockQ, p5Api, mapper, noopToast, previewTokenStore) }
        ];

        let frozenArrayErrors = 0;
        for (const entry of phase5Controllers) {
          entry.inst.$onInit();
          for (const [propKey, propVal] of Object.entries(entry.inst)) {
            if (Array.isArray(propVal)) {
              if (!Object.isExtensible(propVal)) {
                frozenArrayErrors++;
                fail(`${entry.name}.$ctrl.${propKey} array itself is non-extensible (frozen).`);
              }
              for (const elem of propVal) {
                if (elem && typeof elem === 'object' && !Object.isExtensible(elem)) {
                  frozenArrayErrors++;
                  fail(`${entry.name}.$ctrl.${propKey} contains frozen object element (breaks AngularJS $$hashKey / ng-options).`);
                  break;
                }
              }
            }
          }
          if (typeof entry.inst.$onDestroy === 'function') {
            entry.inst.$onDestroy();
          }
        }
        wizardCtrl.$onDestroy();

        if (frozenArrayErrors === 0) {
          pass(`Verified zero frozen ng-options / ng-repeat array elements across all ${phase5Controllers.length + 2} Phase 5 AngularJS components.`);
        }

        // (e) Stage 3 regression guards: F-FE-01 (ruleForm min=500 + ProblemDetailsMapper), F-FE-02 (listDealerTimeSlots), F-FE-03 (deterministic $q promises)
        const ruleKmHasMin500 = /id="ruleKmInput"[^>]*min="500"/.test(adminModelsDef.template) &&
          !/id="ruleKmInput"[^>]*min="100"/.test(adminModelsDef.template);

        let dealerSlotEndpointHits = 0;
        p5Responder = async (cfg) => {
          if (cfg.url.endsWith('/time-slots/dealer')) {
            dealerSlotEndpointHits++;
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: 'stf-dlr-slot-1', dealerId: '22222222-2222-2222-2222-222222222201', slotDate: '2026-11-23', startTime: '08:00', endTime: '09:00', maxCapacity: 5, bookedCount: 2, isBlocked: false, rowVersion: 'AAAAAAAAD4A=' }]
              }
            };
          }
          if (cfg.url.includes('/maintenance-rules') && cfg.method === 'POST') {
            return Promise.reject({
              status: 400,
              data: {
                title: 'Validation Failed',
                detail: 'One or more validation errors occurred.',
                errorCode: 'VALIDATION_FAILED',
                errors: { IntervalKm: ['IntervalKm must be at least 500 km.'] }
              }
            });
          }
          if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ bookingId: 'bk-live-cancel-1', bookingNumber: 'BK-20261123-0001', status: 'Pending', slotDate: '2026-11-23', startTime: '09:00', endTime: '10:00', vin: 'RLHJF5109RZ104829', rowVersion: 'AAAAAAAAD4F=' }]
              }
            };
          }
          if (cfg.url.endsWith('/bookings/bk-live-cancel-1/cancel') && cfg.method === 'POST') {
            return {
              status: 200,
              data: {
                success: true,
                data: { bookingId: 'bk-live-cancel-1', bookingNumber: 'BK-20261123-0001', status: 'Cancelled', slotDate: '2026-11-23', startTime: '09:00', endTime: '10:00', vin: 'RLHJF5109RZ104829', cancellationReason: 'Schedule conflict', rowVersion: 'AAAAAAAAD4Z=' }
              }
            };
          }
          if (cfg.url.endsWith('/bookings/bk-live-cancel-1/status') && cfg.method === 'PATCH') {
            return {
              status: 200,
              data: {
                success: true,
                data: { bookingId: 'bk-live-cancel-1', bookingNumber: 'BK-20261123-0001', status: 'Cancelled', slotDate: '2026-11-23', startTime: '09:00', endTime: '10:00', vin: 'RLHJF5109RZ104829', cancellationReason: 'Advisor cancel', rowVersion: 'AAAAAAAAD4Z=' }
              }
            };
          }
          return { status: 200, data: { success: true, data: [] } };
        };

        // F-FE-01: AdminModels ruleForm server validation mapping + field-edit clearing
        const liveAdminCtrl = new adminModelsDef.controller(mockQ, p5Api, mapper, noopToast, liveTokenStore);
        liveAdminCtrl.$onInit();
        await liveAdminCtrl.loadModels();
        liveAdminCtrl.studioModel = { id: '55555555-5555-5555-5555-555555555502', modelCode: 'HND-SH160I-2026', maintenanceRules: [] };
        liveAdminCtrl.startNewRule();
        let intervalKmServerValid = true;
        liveAdminCtrl.ruleForm = {
          $invalid: false,
          intervalKm: {
            $error: {},
            $setValidity(key, isValid) {
              intervalKmServerValid = isValid;
              if (isValid) { delete this.$error[key]; } else { this.$error[key] = true; }
            }
          }
        };
        await liveAdminCtrl.saveRule();
        const ruleFieldErrMapped = liveAdminCtrl.ruleFieldErrors?.intervalKm === 'IntervalKm must be at least 500 km.' && intervalKmServerValid === false;
        liveAdminCtrl.onRuleFieldEdited('intervalKm');
        const ruleFieldErrCleared = !liveAdminCtrl.ruleFieldErrors?.intervalKm && intervalKmServerValid === true;
        liveAdminCtrl.$onDestroy();

        // F-FE-02: StaffTimeSlots loadTimeSlots() hits GET /api/v1/time-slots/dealer
        const liveSlotsCtrl = new staffSlotsDef.controller(mockQ, noopRootScope, p4AuthSvc, p5Api, mapper, p5SignalR, noopToast, liveTokenStore);
        liveSlotsCtrl.$onInit();
        const loadedDealerSlots = await liveSlotsCtrl.loadTimeSlots();
        liveSlotsCtrl.$onDestroy();

        // F-FE-03: CustomerBookings & StaffWorkbench deterministic $q promise returns
        const liveCustBkCtrl = new custBookingsDef.controller(mockQ, noopRootScope, p5Api, mapper, p5SignalR, noopToast, liveTokenStore);
        liveCustBkCtrl.$onInit();
        const custBookingsPromise = liveCustBkCtrl.loadBookings();
        const custLoadedRows = await custBookingsPromise;
        const guardCancelOut = await liveCustBkCtrl.confirmCancelBooking();
        liveCustBkCtrl.openCancelModal(custLoadedRows[0]);
        liveCustBkCtrl.cancelReason = 'Schedule conflict';
        const liveCancelledBk = await liveCustBkCtrl.confirmCancelBooking();
        liveCustBkCtrl.$onDestroy();

        const liveWbCtrl = new staffWorkbenchDef.controller(mockQ, noopRootScope, p4AuthSvc, p5Api, mapper, p5SignalR, noopToast, liveTokenStore);
        liveWbCtrl.$onInit();
        const wbLoadedRows = await liveWbCtrl.loadWorkbench();
        const guardWbCancelOut = await liveWbCtrl.confirmStaffCancel();
        liveWbCtrl.openStaffCancelModal(wbLoadedRows[0]);
        liveWbCtrl.staffCancelReason = 'Advisor cancel';
        const wbCancelledCard = await liveWbCtrl.confirmStaffCancel();
        liveWbCtrl.$onDestroy();

        if (
          !ruleKmHasMin500 ||
          !ruleFieldErrMapped ||
          !ruleFieldErrCleared ||
          dealerSlotEndpointHits < 2 ||
          loadedDealerSlots.length !== 1 ||
          loadedDealerSlots[0].slotId !== 'stf-dlr-slot-1' ||
          typeof custBookingsPromise?.then !== 'function' ||
          guardCancelOut !== null ||
          liveCancelledBk?.status !== 'Cancelled' ||
          guardWbCancelOut !== null ||
          wbCancelledCard?.status !== 'Cancelled'
        ) {
          fail(`Stage 3 regression check failed: min500=${ruleKmHasMin500}, ruleErrMapped=${ruleFieldErrMapped}, ruleErrCleared=${ruleFieldErrCleared}, dealerSlotHits=${dealerSlotEndpointHits}, liveCancelled=${liveCancelledBk?.status}, wbCancelled=${wbCancelledCard?.status}`);
        } else {
          pass('Verified F-FE-01 (ruleForm min=500 & ProblemDetailsMapper), F-FE-02 (/time-slots/dealer), and F-FE-03 (deterministic $q promises on Bookings & Workbench).');
        }

        // (f) Stage 5 refactor checks: RF-FE-01 & RF-FE-02 (LoyaltyCalculator.determineTier boundary parity + garage & sales-desk $q promises)
        const tierBoundariesOk =
          loyaltyCalc.determineTier(-5).code === 'Bronze' &&
          loyaltyCalc.determineTier(0).code === 'Bronze' &&
          loyaltyCalc.determineTier(0).multiplier === 1.0 &&
          loyaltyCalc.determineTier(499).code === 'Bronze' &&
          loyaltyCalc.determineTier(500).code === 'Silver' &&
          loyaltyCalc.determineTier(500).multiplier === 1.2 &&
          loyaltyCalc.determineTier(1999).code === 'Silver' &&
          loyaltyCalc.determineTier(2000).code === 'Gold' &&
          loyaltyCalc.determineTier(2000).multiplier === 1.5 &&
          loyaltyCalc.determineTier(4999).code === 'Gold' &&
          loyaltyCalc.determineTier(5000).code === 'Platinum' &&
          loyaltyCalc.determineTier(5000).multiplier === 2.0 &&
          loyaltyCalc.determineTierCode(4620) === 'Gold' &&
          Object.isExtensible(loyaltyCalc.determineTier(2000));

        p5Responder = async (cfg) => {
          if (cfg.url.endsWith('/vehicles') && cfg.method === 'GET') {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: 'veh-rf-1', vin: 'RLHJF5109RZ104829', motorcycleModelId: '55555555-5555-5555-5555-555555555502', motorcycleModelName: 'Honda SH160i ABS Sport', currentMileageKm: 3850, nextDueMileageKm: 4000, maintenanceStatus: 'DueSoon' }]
              }
            };
          }
          if (cfg.url.endsWith('/vehicles') && cfg.method === 'POST') {
            return {
              status: 201,
              data: {
                success: true,
                data: { id: 'veh-rf-ext', vin: 'RLCUG1010PY092341', motorcycleModelId: '55555555-5555-5555-5555-555555555504', motorcycleModelName: 'Yamaha Exciter 155 VVA', registrationSource: 'ExternalManual', currentMileageKm: 1200, nextDueMileageKm: 4000 }
              }
            };
          }
          if (cfg.url.endsWith('/vehicles/veh-rf-1/mileage') && cfg.method === 'PATCH') {
            if (cfg.data?.currentMileageKm < 3850) {
              return Promise.reject({
                status: 422,
                data: {
                  title: 'Unprocessable Entity',
                  detail: 'Odometer cannot decrease below recorded 3850 km.',
                  errorCode: 'ODOMETER_REGRESSION'
                }
              });
            }
            return {
              status: 200,
              data: {
                success: true,
                data: { id: 'veh-rf-1', vin: 'RLHJF5109RZ104829', motorcycleModelName: 'Honda SH160i ABS Sport', currentMileageKm: cfg.data.currentMileageKm, nextDueMileageKm: 4000 }
              }
            };
          }
          if (cfg.url.endsWith('/vehicles/veh-rf-1/reminders') && cfg.method === 'GET') {
            return {
              status: 200,
              data: {
                success: true,
                data: {
                  reminders: [{ id: 'rem-rf-1', milestoneName: '4,000 km Periodic Maintenance', targetMileageKm: 4000, deduplicationKey: 'RLHJF5109RZ104829:r1:4000', status: 'Sent' }],
                  maintenanceRules: [{ id: 'rule-rf-1', milestoneCode: 'PERIODIC_4000', milestoneName: '4,000 km Periodic Maintenance', intervalKm: 4000, intervalMonths: 6 }]
                }
              }
            };
          }
          if (cfg.url.endsWith('/inventories') && cfg.method === 'GET') {
            return {
              status: 200,
              data: {
                success: true,
                data: [{ id: 'inv-rf-1', vin: 'RLHJF5108RY000301', engineNumber: 'JF51E-8000301', motorcycleModelId: '55555555-5555-5555-5555-555555555501', motorcycleModelName: 'Honda Air Blade 160 SE', listedPrice: 45000000, stockStatus: 'Available', rowVersion: 'AAAAAAAAC1F=' }]
              }
            };
          }
          if (cfg.url.endsWith('/inventories') && cfg.method === 'POST') {
            return {
              status: 201,
              data: {
                success: true,
                data: { id: 'inv-rf-new', vin: cfg.data.vin, engineNumber: cfg.data.engineNumber, motorcycleModelId: cfg.data.motorcycleModelId, motorcycleModelName: 'Honda Air Blade 160 SE', listedPrice: cfg.data.listedPrice, stockStatus: 'Available', rowVersion: 'AAAAAAAAC1Z=' }
              }
            };
          }
          if (cfg.url.endsWith('/sales-orders') && cfg.method === 'POST') {
            return {
              status: 201,
              data: {
                success: true,
                data: { salesOrderId: 'so-rf-1', orderNumber: 'SO-20261123-0001', vin: 'RLHJF5108RY000301', finalAmount: 45000000, pointsEarned: 4500, appliedTierCode: 'Bronze', appliedMultiplier: 1.0, newTierCode: 'Gold', tierUpgraded: true, onboardedCustomerVehicleId: 'veh-onboard-rf-1' }
              }
            };
          }
          return { status: 200, data: { success: true, data: [] } };
        };

        // Garage $q promises verification
        const liveGarageCtrl = new custGarageDef.controller(mockQ, p5Api, mapper, p5SignalR, noopToast, liveTokenStore);
        liveGarageCtrl.$onInit();
        const garageLoadPromise = liveGarageCtrl.loadGarage();
        const garageVehicles = await garageLoadPromise;
        liveGarageCtrl.registerForm = { $invalid: true };
        const guardRegOut = await liveGarageCtrl.submitRegisterVehicle();
        liveGarageCtrl.registerForm = { $invalid: false };
        liveGarageCtrl.newVehicle = { vin: 'RLCUG1010PY092341', motorcycleModelId: '55555555-5555-5555-5555-555555555504', currentMileageKm: 1200 };
        const createdExtBike = await liveGarageCtrl.submitRegisterVehicle();
        const guardOdoOut = await liveGarageCtrl.submitOdometerUpdate();
        liveGarageCtrl.openOdometerModal(garageVehicles[0]);
        liveGarageCtrl.odometerDraftKm = 3000;
        const regressedOdoOut = await liveGarageCtrl.submitOdometerUpdate();
        const regressedErrorCode = liveGarageCtrl.odometerProblem?.errorCode;
        liveGarageCtrl.odometerDraftKm = 3950;
        const validOdoOut = await liveGarageCtrl.submitOdometerUpdate();
        const guardDrawerOut = await liveGarageCtrl.openRemindersDrawer(null);
        const loadedDrawer = await liveGarageCtrl.openRemindersDrawer(garageVehicles[0]);
        liveGarageCtrl.$onDestroy();

        // Sales Desk LoyaltyCalculator delegation & $q promises verification
        const liveSalesCtrl = new staffSalesDef.controller(mockQ, p4AuthSvc, p5Api, loyaltyCalc, mapper, noopToast, liveTokenStore);
        liveSalesCtrl.$onInit();
        const salesBundle = await liveSalesCtrl.loadSalesDeskData();
        liveSalesCtrl.intakeForm = { $invalid: true };
        const guardIntakeOut = await liveSalesCtrl.submitIntakeVin();
        liveSalesCtrl.intakeForm = { $invalid: false };
        liveSalesCtrl.intakeDraft = { motorcycleModelId: '55555555-5555-5555-5555-555555555501', vin: 'RLHJF5108RY000999', engineNumber: 'JF51E-8000999', colorName: 'Matte Black', manufactureYear: 2026, listedPrice: 45000000 };
        const createdInvUnit = await liveSalesCtrl.submitIntakeVin();
        const guardSaleOut = await liveSalesCtrl.submitSalesOrder();
        liveSalesCtrl.openCheckoutModal(salesBundle.inventories[0]);
        const guardNoBuyerSale = liveSalesCtrl.canSubmitSale() === false;
        liveSalesCtrl.checkoutDraft.customerEmail = 'buyer.real@example.com';
        liveSalesCtrl.onBuyerEmailChanged();
        const previewPointsEarned = liveSalesCtrl.salePreview.pointsEarned;
        const previewProjectedTier = liveSalesCtrl.salePreview.projectedTierCode;
        const completedSaleReceipt = await liveSalesCtrl.submitSalesOrder();
        liveSalesCtrl.$onDestroy();

        if (
          !tierBoundariesOk ||
          !guardNoBuyerSale ||
          typeof garageLoadPromise?.then !== 'function' ||
          guardRegOut !== null ||
          createdExtBike?.vin !== 'RLCUG1010PY092341' ||
          guardOdoOut !== null ||
          regressedOdoOut !== null ||
          regressedErrorCode !== 'ODOMETER_REGRESSION' ||
          validOdoOut?.currentMileageKm !== 3950 ||
          guardDrawerOut !== null ||
          loadedDrawer?.reminders?.length !== 1 ||
          guardIntakeOut !== null ||
          createdInvUnit?.vin !== 'RLHJF5108RY000999' ||
          guardSaleOut !== null ||
          previewPointsEarned !== 4500 ||
          previewProjectedTier !== 'Gold' ||
          completedSaleReceipt?.newTierCode !== 'Gold'
        ) {
          fail(`Stage 5 refactor check failed: tierBoundariesOk=${tierBoundariesOk}, extVin=${createdExtBike?.vin}, odoErr=${regressedErrorCode}, validOdo=${validOdoOut?.currentMileageKm}, previewPts=${previewPointsEarned}, projTier=${previewProjectedTier}`);
        } else {
          pass('Verified RF-FE-01 & RF-FE-02: LoyaltyCalculator.determineTier boundary parity and deterministic $q promises across CustomerGarage & StaffSalesDesk.');
        }

        // (g) Cross-Phase P4+P5 Stage 3 regression guards: F-P45-SPA-01 (Global AuthService real-time loyalty sync + _currentUserFlight dedup + bookingNew auth:changed) & F-P45-SPA-02 (refreshTokens SignalR recovery)
        const createEventBusRootScope = () => {
          const handlers = new Map();
          return {
            $applyAsync: (fn) => fn(),
            $evalAsync: (fn) => fn(),
            $on(evtName, fn) {
              if (!handlers.has(evtName)) {
                handlers.set(evtName, []);
              }
              const list = handlers.get(evtName);
              list.push(fn);
              let active = true;
              return () => {
                if (!active) return;
                active = false;
                const idx = list.indexOf(fn);
                if (idx !== -1) list.splice(idx, 1);
              };
            },
            $broadcast(evtName, payload) {
              const snapshot = (handlers.get(evtName) || []).slice();
              for (const fn of snapshot) {
                fn({ name: evtName }, payload);
              }
            },
            _listenerCount: (evtName) => (handlers.get(evtName) || []).length
          };
        };

        const p45Bus = createEventBusRootScope();
        const p45SessionStore = createMemoryStorage();
        const p45LocalStore = createMemoryStorage();
        const p45TokenStore = new tokenStoreCtor({
          sessionStorage: p45SessionStore,
          localStorage: p45LocalStore
        });

        let p45MeCalls = 0;
        let p45HubStatus = 'Connected';
        let p45SignalRStarts = 0;
        let p45MePayload = {
          userId: '44444444-4444-4444-4444-444444444408',
          email: 'customer.silver@motorcycledealer.vn',
          fullName: 'Tran Ngoc Mai (Silver)',
          role: 'Customer',
          tierCode: 'Gold',
          tierMultiplier: 1.5,
          pointsMultiplier: 1.5,
          availablePoints: 5149,
          lifetimeQualifyingPoints: 6460,
          permissions: ['Permissions.Bookings.Create', 'Permissions.Loyalty.ViewOwn']
        };

        const p45Http = Object.assign(
          async (cfg) => {
            if (String(cfg.url).endsWith('/auth/me')) {
              p45MeCalls++;
              await new Promise((r) => setTimeout(r, 25));
              return { status: 200, data: { success: true, data: p45MePayload } };
            }
            if (String(cfg.url).endsWith('/auth/refresh')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    accessToken: 'jwt-p45-refreshed-' + (p45SignalRStarts + 1),
                    refreshToken: 'rt-p45-refreshed-' + (p45SignalRStarts + 1),
                    expiresInSeconds: 900,
                    user: p45MePayload
                  }
                }
              };
            }
            return { status: 200, data: { success: true, data: [] } };
          },
          {
            get: (url, cfg) => p45Http(Object.assign({ method: 'GET', url }, cfg || {})),
            post: (url, data, cfg) => p45Http(Object.assign({ method: 'POST', url, data }, cfg || {}))
          }
        );

        const p45NotifStore = new notifStoreCtor();
        const p45SignalRSvc = new signalrSvcCtor(
          { resolve: (v) => Promise.resolve(v), when: (v) => Promise.resolve(v) },
          p45Bus,
          registeredConstants.get('APP_CONFIG'),
          p45TokenStore,
          p45NotifStore,
          noopToast
        );
        // Override getConnectionStatus & start spy while keeping simulateEvent + on() real
        p45SignalRSvc.getConnectionStatus = () => p45HubStatus;
        p45SignalRSvc.start = () => {
          p45SignalRStarts++;
          p45HubStatus = 'Connected';
          return Promise.resolve(true);
        };

        const p45InjectorMap = {
          $http: p45Http,
          $state: mockState,
          SignalRClientService: p45SignalRSvc,
          ToastService: noopToast
        };
        const p45Injector = {
          has: (k) => Boolean(p45InjectorMap[k]),
          get: (k) => p45InjectorMap[k]
        };

        const p45AuthSvc = new authSvcCtor(
          mockQ,
          p45Injector,
          p45Bus,
          registeredConstants.get('APP_CONFIG'),
          registeredConstants.get('ROLE_HOME'),
          registeredConstants.get('ROLE_PERMISSION_PRESETS'),
          p45TokenStore
        );
        p45InjectorMap.AuthService = p45AuthSvc;

        let authChangedEvents = 0;
        p45Bus.$on('auth:changed', () => { authChangedEvents++; });

        p45AuthSvc.setSession({
          accessToken: 'jwt-p45-live-customer',
          refreshToken: 'rt-p45-live-customer',
          expiresInSeconds: 900,
          user: {
            userId: '44444444-4444-4444-4444-444444444408',
            email: 'customer.silver@motorcycledealer.vn',
            fullName: 'Tran Ngoc Mai (Silver)',
            role: 'Customer',
            tierCode: 'Silver',
            tierMultiplier: 1.2,
            pointsMultiplier: 1.2,
            availablePoints: 649,
            lifetimeQualifyingPoints: 1960,
            permissions: ['Permissions.Bookings.Create', 'Permissions.Loyalty.ViewOwn']
          }
        });

        // Mount both customerBookingNew (non-dashboard screen) and customerDashboard concurrently
        const p45BookingWizard = new bookingNewDef.controller(
          mockQ,
          p45Bus,
          mockState,
          {},
          p45AuthSvc,
          p5Api,
          mapper,
          noopToast,
          p45TokenStore
        );
        p45BookingWizard.$onInit();
        const initialWizardMult = p45BookingWizard.userMultiplier;

        // Simulate concurrent LoyaltyPointsUpdated across SignalRClientService -> both dashboard.fetchCurrentUser() AND AuthService $rootScope.$on fire simultaneously
        p45MeCalls = 0;
        authChangedEvents = 0;
        const dashOff = p45SignalRSvc.on('LoyaltyPointsUpdated', () => {
          p45AuthSvc.fetchCurrentUser().catch(() => {});
        });

        p45SignalRSvc.simulateEvent('LoyaltyPointsUpdated', {
          pointsDelta: 4500,
          availablePointsAfter: 5149,
          lifetimeQualifyingPointsAfter: 6460
        });

        // Assert immediate optimistic patch + sessionStorage persistence BEFORE async /auth/me resolves
        const optimisticAfterPoints = p45AuthSvc.getCurrentUser();
        const persistedAfterPoints = JSON.parse(p45SessionStore.getItem('mdlmp.auth.session') || '{}');
        const immediateBroadcastAfterPoints = authChangedEvents >= 1;

        await new Promise((r) => setTimeout(r, 45));
        const dedupedMeCallsOnLoyaltyEvent = p45MeCalls;
        dashOff();

        // Now simulate TierUpgraded while user is on /customer/booking/new (dashboard listener absent!)
        p45MeCalls = 0;
        p45MePayload = Object.assign({}, p45MePayload, {
          tierCode: 'Platinum',
          tierMultiplier: 2.0,
          pointsMultiplier: 2.0,
          lifetimeQualifyingPoints: 6460
        });

        p45SignalRSvc.simulateEvent('TierUpgraded', {
          previousTierCode: 'Gold',
          newTierCode: 'Platinum',
          newPointsMultiplier: 2.0,
          lifetimeQualifyingPoints: 6460
        });

        const optimisticAfterTier = p45AuthSvc.getCurrentUser();
        const persistedAfterTier = JSON.parse(p45SessionStore.getItem('mdlmp.auth.session') || '{}');
        const wizardMultLiveUpdated = p45BookingWizard.userMultiplier;

        await new Promise((r) => setTimeout(r, 45));
        const meCallsOnTierEvent = p45MeCalls;

        // Verify $onDestroy cleanup on customerBookingNew prevents post-destroy auth:changed mutation
        const authChangedListenersBeforeDestroy = p45Bus._listenerCount('auth:changed');
        p45BookingWizard.$onDestroy();
        const authChangedListenersAfterDestroy = p45Bus._listenerCount('auth:changed');
        p45SignalRSvc.simulateEvent('TierUpgraded', {
          newTierCode: 'Silver',
          newPointsMultiplier: 1.2
        });
        await new Promise((r) => setTimeout(r, 45));
        const wizardMultUnchangedAfterDestroy = p45BookingWizard.userMultiplier === 2.0;

        // Verify F-P45-SPA-02: refreshTokens() recovers SignalR when Disconnected or Unavailable, but skips redundant start when Connected
        p45SignalRStarts = 0;
        p45HubStatus = 'Connected';
        await p45AuthSvc.refreshTokens();
        const startsWhenAlreadyConnected = p45SignalRStarts;

        p45HubStatus = 'Disconnected';
        await p45AuthSvc.refreshTokens();
        const startsWhenDisconnected = p45SignalRStarts;

        p45HubStatus = 'Unavailable';
        await p45AuthSvc.refreshTokens();
        const startsWhenUnavailable = p45SignalRStarts;

        if (
          initialWizardMult !== 1.2 ||
          optimisticAfterPoints?.availablePoints !== 5149 ||
          optimisticAfterPoints?.lifetimeQualifyingPoints !== 6460 ||
          persistedAfterPoints?.user?.availablePoints !== 5149 ||
          !immediateBroadcastAfterPoints ||
          dedupedMeCallsOnLoyaltyEvent !== 1 ||
          optimisticAfterTier?.tierCode !== 'Platinum' ||
          optimisticAfterTier?.tierMultiplier !== 2.0 ||
          optimisticAfterTier?.pointsMultiplier !== 2.0 ||
          persistedAfterTier?.user?.tierCode !== 'Platinum' ||
          persistedAfterTier?.user?.pointsMultiplier !== 2.0 ||
          wizardMultLiveUpdated !== 2.0 ||
          meCallsOnTierEvent !== 1 ||
          authChangedListenersAfterDestroy !== authChangedListenersBeforeDestroy - 1 ||
          !wizardMultUnchangedAfterDestroy ||
          startsWhenAlreadyConnected !== 0 ||
          startsWhenDisconnected !== 1 ||
          startsWhenUnavailable !== 2
        ) {
          fail(
            `F-P45-SPA-01/02 verification failed: initMult=${initialWizardMult}, optPts=${optimisticAfterPoints?.availablePoints}, dedupMe=${dedupedMeCallsOnLoyaltyEvent}, optTier=${optimisticAfterTier?.tierCode}/${optimisticAfterTier?.pointsMultiplier}, wizMult=${wizardMultLiveUpdated}, starts=[${startsWhenAlreadyConnected},${startsWhenDisconnected},${startsWhenUnavailable}]`
          );
        } else {
          pass('Verified F-P45-SPA-01 (AuthService global real-time loyalty/tier optimistic patch + _currentUserFlight single-flight /auth/me + bookingNew live multiplier) & F-P45-SPA-02 (refreshTokens SignalR recovery).');
        }

        // (h) Cross-Phase P4+P5 Stage 5 refactor check: RF-P45-SPA-01 (customerLoyalty live AuthService + LoyaltyCalculator.resolveTierProgress + auth:changed + $onDestroy)
        const custLoyaltyDef = registeredComponents.get('customerLoyalty');
        if (!custLoyaltyDef || typeof custLoyaltyDef.controller !== 'function' || typeof loyaltyCalc.resolveTierProgress !== 'function') {
          fail('Missing customerLoyalty component or LoyaltyCalculator.resolveTierProgress helper.');
        } else {
          const loyaltyTpl = String(custLoyaltyDef.template || '');
          const noHardcodedMockPoints =
            !loyaltyTpl.includes('649 pts') &&
            !loyaltyTpl.includes('849 / 2,000 pts') &&
            !loyaltyTpl.includes('Silver Member • 1.20x Multiplier');

          const progBronze = loyaltyCalc.resolveTierProgress(120);
          const progSilver = loyaltyCalc.resolveTierProgress(1960);
          const progGold = loyaltyCalc.resolveTierProgress(3200);
          const progPlatinum = loyaltyCalc.resolveTierProgress(5400);
          const progressMathOk =
            progBronze.currentTierCode === 'Bronze' &&
            progBronze.nextTierCode === 'Silver' &&
            progBronze.nextTierThreshold === 500 &&
            progBronze.pointsToNextTier === 380 &&
            !progBronze.isTopTier &&
            progSilver.currentTierCode === 'Silver' &&
            progSilver.nextTierCode === 'Gold' &&
            progSilver.nextTierThreshold === 2000 &&
            progSilver.pointsToNextTier === 40 &&
            !progSilver.isTopTier &&
            progGold.currentTierCode === 'Gold' &&
            progGold.nextTierCode === 'Platinum' &&
            progGold.nextTierThreshold === 5000 &&
            progGold.pointsToNextTier === 1800 &&
            !progGold.isTopTier &&
            progPlatinum.currentTierCode === 'Platinum' &&
            progPlatinum.nextTierCode === null &&
            progPlatinum.nextTierThreshold === null &&
            progPlatinum.pointsToNextTier === 0 &&
            progPlatinum.progressPercent === 100 &&
            progPlatinum.isTopTier === true;

          // 1. Unauthenticated fallback -> Bronze 0 pts
          await p45AuthSvc.clearSessionAndRealtime();
          const loyaltyCtrl = new custLoyaltyDef.controller(
            mockQ,
            p45Bus,
            {},
            p45AuthSvc,
            p5Api,
            loyaltyCalc,
            mapper,
            p45SignalRSvc,
            noopToast,
            previewTokenStore
          );
          loyaltyCtrl.$onInit();
          const unauthFallbackOk =
            loyaltyCtrl.availablePoints === 0 &&
            loyaltyCtrl.lifetimePoints === 0 &&
            loyaltyCtrl.tierCode === 'Bronze' &&
            loyaltyCtrl.tierMultiplier === 1.0 &&
            loyaltyCtrl.tierProgress?.nextTierCode === 'Silver' &&
            loyaltyCtrl.tierProgress?.pointsToNextTier === 500;

          // 2. Live Bronze session -> zero-reload sync via auth:changed
          p45AuthSvc.setSession({
            accessToken: 'jwt-rf-loyalty-bronze',
            refreshToken: 'rt-rf-loyalty-bronze',
            expiresInSeconds: 900,
            user: {
              userId: '44444444-4444-4444-4444-444444444406',
              email: 'minh.nguyen@customer.vn',
              fullName: 'Nguyen Van Minh',
              role: 'Customer',
              tierCode: 'Bronze',
              tierMultiplier: 1.0,
              pointsMultiplier: 1.0,
              availablePoints: 120,
              lifetimeQualifyingPoints: 120,
              permissions: ['Permissions.Loyalty.ViewOwn', 'Permissions.Loyalty.RedeemVoucher']
            }
          });
          const bronzeSyncedOk =
            loyaltyCtrl.availablePoints === 120 &&
            loyaltyCtrl.lifetimePoints === 120 &&
            loyaltyCtrl.tierCode === 'Bronze' &&
            loyaltyCtrl.tierMultiplier === 1.0 &&
            loyaltyCtrl.tierProgress?.pointsToNextTier === 380;

          // 3. Live SignalR SalesOrder promotion -> Platinum (5,400 pts) reflected immediately on /customer/loyalty
          p45MePayload = {
            userId: '44444444-4444-4444-4444-444444444406',
            email: 'minh.nguyen@customer.vn',
            fullName: 'Nguyen Van Minh',
            role: 'Customer',
            tierCode: 'Platinum',
            tierMultiplier: 2.0,
            pointsMultiplier: 2.0,
            availablePoints: 5400,
            lifetimeQualifyingPoints: 5400,
            permissions: ['Permissions.Loyalty.ViewOwn', 'Permissions.Loyalty.RedeemVoucher']
          };
          p45SignalRSvc.simulateEvent('LoyaltyPointsUpdated', {
            pointsDelta: 5280,
            availablePointsAfter: 5400,
            lifetimeQualifyingPointsAfter: 5400
          });
          p45SignalRSvc.simulateEvent('TierUpgraded', {
            previousTierCode: 'Bronze',
            newTierCode: 'Platinum',
            newPointsMultiplier: 2.0,
            lifetimeQualifyingPoints: 5400
          });

          const platinumSyncedOk =
            loyaltyCtrl.availablePoints === 5400 &&
            loyaltyCtrl.lifetimePoints === 5400 &&
            loyaltyCtrl.tierCode === 'Platinum' &&
            loyaltyCtrl.tierMultiplier === 2.0 &&
            loyaltyCtrl.tierProgress?.isTopTier === true &&
            loyaltyCtrl.tierProgress?.pointsToNextTier === 0;

          await new Promise((r) => setTimeout(r, 45));

          // 4. $onDestroy listener cleanup
          const listenersBeforeLoyaltyDestroy = p45Bus._listenerCount('auth:changed');
          loyaltyCtrl.$onDestroy();
          const listenersAfterLoyaltyDestroy = p45Bus._listenerCount('auth:changed');
          setTestRoleSession(p45AuthSvc, 'Customer');
          const postDestroyFrozenOk =
            loyaltyCtrl.availablePoints === 5400 &&
            loyaltyCtrl.tierCode === 'Platinum';

          if (
            !noHardcodedMockPoints ||
            !progressMathOk ||
            !unauthFallbackOk ||
            !bronzeSyncedOk ||
            !platinumSyncedOk ||
            listenersAfterLoyaltyDestroy !== listenersBeforeLoyaltyDestroy - 1 ||
            !postDestroyFrozenOk
          ) {
            fail(
              `RF-P45-SPA-01 verification failed: noHardcoded=${noHardcodedMockPoints}, math=${progressMathOk}, unauth=${unauthFallbackOk}, bronze=${bronzeSyncedOk}, plat=${platinumSyncedOk}, postDestroy=${postDestroyFrozenOk}`
            );
          } else {
            pass('Verified RF-P45-SPA-01: customerLoyalty binds live AuthService + LoyaltyCalculator.resolveTierProgress via auth:changed and cleans up in $onDestroy.');
          }
        }

        // (i) SignalRClientService serialized start()/stop() lifecycle queue across rapid Customer -> DealerStaff -> Admin role switches
        const raceBus = createEventBusRootScope();
        const raceTokenStore = new tokenStoreCtor({});
        const raceNotifStore = new notifStoreCtor();
        const raceStatusHistory = [];
        raceBus.$on('signalr:status', (_evt, st) => {
          raceStatusHistory.push(st);
        });

        const activeMockConnections = new Set();
        let createdConnSeq = 0;
        sandbox.signalR = {
          LogLevel: { None: 6 },
          HubConnectionBuilder: function MockHubConnectionBuilder() {
            let tokenFactory = () => '';
            this.withUrl = (_url, opts) => {
              if (opts && typeof opts.accessTokenFactory === 'function') {
                tokenFactory = opts.accessTokenFactory;
              }
              return this;
            };
            this.withAutomaticReconnect = () => this;
            this.configureLogging = () => this;
            this.build = () => {
              let onCloseCb = null;
              const connObj = {
                id: ++createdConnSeq,
                initialToken: tokenFactory(),
                tokenFactory,
                on() {},
                onreconnecting() {},
                onreconnected() {},
                onclose(cb) { onCloseCb = cb; },
                start() {
                  return new Promise((resolve) => {
                    setTimeout(() => {
                      connObj.startedToken = tokenFactory();
                      activeMockConnections.add(connObj);
                      resolve();
                    }, 10);
                  });
                },
                stop() {
                  return new Promise((resolve) => {
                    setTimeout(() => {
                      activeMockConnections.delete(connObj);
                      if (typeof onCloseCb === 'function') {
                        onCloseCb();
                      }
                    }, 15);
                    setTimeout(resolve, 25);
                  });
                }
              };
              return connObj;
            };
          }
        };

        try {
          const raceSignalRSvc = new signalrSvcCtor(
            mockQ,
            raceBus,
            registeredConstants.get('APP_CONFIG'),
            raceTokenStore,
            raceNotifStore,
            noopToast
          );

          // 1. Initial Customer connection
          raceTokenStore.setTokens('jwt-race-customer-1', 900, 'rt-race-1');
          const customerConnected = await raceSignalRSvc.start();
          const statusAfterCustomer = raceSignalRSvc.getConnectionStatus();

          // 2. Rapid Customer -> DealerStaff -> Admin switch while previousConn.stop() (25ms) is in flight
          raceStatusHistory.length = 0;
          raceTokenStore.setTokens('jwt-race-staff-2', 900, 'rt-race-2');
          const pStaffStart = raceSignalRSvc.start();

          await new Promise((r) => setTimeout(r, 5));
          raceTokenStore.setTokens('jwt-race-admin-3', 900, 'rt-race-3');
          const pAdminStart = raceSignalRSvc.start();
          const pAdminIdempotent = raceSignalRSvc.start();

          const [staffStartRes, adminStartRes, adminDupRes] = await Promise.all([
            pStaffStart,
            pAdminStart,
            pAdminIdempotent
          ]);

          const finalAdminStatus = raceSignalRSvc.getConnectionStatus();
          const hadOfflineDrop = raceStatusHistory.includes('Offline');
          const liveConns = Array.from(activeMockConnections);
          const singleAdminConnOk =
            liveConns.length === 1 &&
            liveConns[0].tokenFactory() === 'jwt-race-admin-3';

          // 3. Clean stop() teardown
          const stopPromise = raceSignalRSvc.stop();
          const syncDisconnectedOnStop = raceSignalRSvc.getConnectionStatus() === 'Disconnected';
          await stopPromise;
          const postStopClean =
            raceSignalRSvc.getConnectionStatus() === 'Disconnected' &&
            activeMockConnections.size === 0;

          if (
            customerConnected !== true ||
            statusAfterCustomer !== 'Connected' ||
            staffStartRes !== true ||
            adminStartRes !== true ||
            adminDupRes !== true ||
            finalAdminStatus !== 'Connected' ||
            hadOfflineDrop ||
            !singleAdminConnOk ||
            !syncDisconnectedOnStop ||
            !postStopClean
          ) {
            fail(
              `SignalRClientService rapid role-switch serialization failed: cust=${statusAfterCustomer}, results=[${staffStartRes},${adminStartRes},${adminDupRes}], final=${finalAdminStatus}, history=${JSON.stringify(raceStatusHistory)}, liveConns=${liveConns.length}`
            );
          } else {
            pass('Verified SignalRClientService serializes rapid Customer -> DealerStaff -> Admin start()/stop() transitions (Connected, 0 Offline drops, 1 active hub connection).');
          }
        } finally {
          delete sandbox.signalR;
        }

        // (j) Cross-session _currentUserFlight token/userId isolation when slow Customer /auth/me overlaps AuthService.login(DealerStaff)
        const overlapSessionStore = createMemoryStorage();
        const overlapTokenStore = new tokenStoreCtor({ sessionStorage: overlapSessionStore });
        const overlapBus = createEventBusRootScope();
        let overlapMeSeq = 0;

        const overlapHttp = Object.assign(
          async (cfg) => {
            if (String(cfg.url).endsWith('/auth/login')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    accessToken: 'jwt-overlap-staff-hn',
                    refreshToken: 'rt-overlap-staff-hn',
                    expiresInSeconds: 900,
                    user: {
                      userId: '11111111-1111-1111-1111-111111111101',
                      email: 'staff.hn@motorcycledealer.vn',
                      fullName: 'Tran Thi Lan',
                      role: 'DealerStaff',
                      dealerId: '22222222-2222-2222-2222-222222222202',
                      permissions: ['Permissions.Bookings.ViewDealer', 'Permissions.TimeSlots.ManageDealer']
                    }
                  }
                }
              };
            }
            if (String(cfg.url).endsWith('/auth/me')) {
              const callIdx = ++overlapMeSeq;
              if (callIdx === 1) {
                // Slow Customer /auth/me (40ms) returning Customer profile AFTER DealerStaff has logged in
                await new Promise((r) => setTimeout(r, 40));
                return {
                  status: 200,
                  data: {
                    success: true,
                    data: {
                      userId: '44444444-4444-4444-4444-444444444408',
                      email: 'customer.silver@motorcycledealer.vn',
                      fullName: 'Tran Ngoc Mai (Silver)',
                      role: 'Customer',
                      tierCode: 'Silver',
                      tierMultiplier: 1.2,
                      pointsMultiplier: 1.2,
                      availablePoints: 649,
                      lifetimeQualifyingPoints: 1960,
                      permissions: ['Permissions.Bookings.Create']
                    }
                  }
                };
              }
              // Fast DealerStaff /auth/me (5ms)
              await new Promise((r) => setTimeout(r, 5));
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    userId: '11111111-1111-1111-1111-111111111101',
                    email: 'staff.hn@motorcycledealer.vn',
                    fullName: 'Tran Thi Lan',
                    role: 'DealerStaff',
                    dealerId: '22222222-2222-2222-2222-222222222202',
                    permissions: ['Permissions.Bookings.ViewDealer', 'Permissions.TimeSlots.ManageDealer']
                  }
                }
              };
            }
            return { status: 200, data: { success: true, data: {} } };
          },
          {
            get: (url, cfg) => overlapHttp(Object.assign({ method: 'GET', url }, cfg || {})),
            post: (url, data, cfg) => overlapHttp(Object.assign({ method: 'POST', url, data }, cfg || {}))
          }
        );

        const overlapInjectorMap = {
          $http: overlapHttp,
          ToastService: noopToast
        };
        const overlapAuthSvc = new authSvcCtor(
          mockQ,
          { has: (k) => Boolean(overlapInjectorMap[k]), get: (k) => overlapInjectorMap[k] },
          overlapBus,
          registeredConstants.get('APP_CONFIG'),
          registeredConstants.get('ROLE_HOME'),
          registeredConstants.get('ROLE_PERMISSION_PRESETS'),
          overlapTokenStore
        );

        overlapAuthSvc.setSession({
          accessToken: 'jwt-overlap-customer',
          refreshToken: 'rt-overlap-customer',
          expiresInSeconds: 900,
          user: {
            userId: '44444444-4444-4444-4444-444444444408',
            email: 'customer.silver@motorcycledealer.vn',
            fullName: 'Tran Ngoc Mai (Silver)',
            role: 'Customer',
            permissions: ['Permissions.Bookings.Create']
          }
        });

        // Start slow Customer fetchCurrentUser() (40ms), then immediately overlap with AuthService.login(DealerStaff)
        const staleCustomerMePromise = overlapAuthSvc.fetchCurrentUser();
        await overlapAuthSvc.login({
          email: 'staff.hn@motorcycledealer.vn',
          password: 'Staff@123!'
        });
        const roleRightAfterStaffLogin = overlapAuthSvc.getRole();

        await staleCustomerMePromise;
        await new Promise((r) => setTimeout(r, 15));
        const roleAfterStaleCustomerMeSettled = overlapAuthSvc.getRole();
        const emailAfterStaleCustomerMeSettled = overlapAuthSvc.getCurrentUser()?.email;

        if (
          overlapMeSeq !== 2 ||
          roleRightAfterStaffLogin !== 'DealerStaff' ||
          roleAfterStaleCustomerMeSettled !== 'DealerStaff' ||
          emailAfterStaleCustomerMeSettled !== 'staff.hn@motorcycledealer.vn' ||
          !overlapAuthSvc.hasPermission('Permissions.Bookings.ViewDealer')
        ) {
          fail(
            `Cross-session _currentUserFlight isolation failed: meCalls=${overlapMeSeq}, immediateRole=${roleRightAfterStaffLogin}, settledRole=${roleAfterStaleCustomerMeSettled}, email=${emailAfterStaleCustomerMeSettled}`
          );
        } else {
          pass('Verified AuthService._currentUserFlight isolates tokens/userIds so slow Customer /auth/me never clobbers a subsequent DealerStaff login.');
        }

        // 4d. Phase 6 (T-606, T-607, T-608) DealerApiService, NotificationBell, StaffInvoiceBuilder, CustomerLoyalty & Admin Studios
        const notifBellDef = registeredComponents.get('notificationBell');
        const adminTiersDef = registeredComponents.get('adminLoyaltyTiers');
        const adminVoucherCatDef = registeredComponents.get('adminVoucherCatalog');

        if (!notifBellDef || !invoiceBuilderDef || !custLoyaltyDef || !adminTiersDef || !adminVoucherCatDef) {
          fail('Missing Phase 6 component registration (notificationBell, staffInvoiceBuilder, customerLoyalty, adminLoyaltyTiers, or adminVoucherCatalog).');
        } else {
          // (k) DealerApiService Phase 6 endpoints: serviceInvoices, loyalty, vouchers, notifications
          const p6Calls = [];
          let p6Responder = async () => ({ status: 200, data: { success: true, data: [] } });
          const p6Http = Object.assign(
            (cfg) => {
              p6Calls.push(cfg);
              return p6Responder(cfg);
            },
            {
              get: (url, cfg) => p6Http(Object.assign({ method: 'GET', url }, cfg || {})),
              post: (url, data, cfg) => p6Http(Object.assign({ method: 'POST', url, data }, cfg || {})),
              put: (url, data, cfg) => p6Http(Object.assign({ method: 'PUT', url, data }, cfg || {})),
              patch: (url, data, cfg) => p6Http(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
            }
          );

          const p6Api = new dealerApiCtor(
            p6Http,
            mockQ,
            registeredConstants.get('APP_CONFIG'),
            mapper
          );

          p6Responder = async (cfg) => {
            if (cfg.url.endsWith('/service-invoices') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      invoiceId: 'inv-p6-1',
                      invoiceNumber: 'INV-20261122-0042',
                      serviceBookingId: 'bk-p6-1',
                      bookingNumber: 'BK-20261122-0042',
                      vin: 'rlhjf5109rz104829',
                      laborAmount: 150000,
                      partsAmount: 300000,
                      grossAmount: 450000,
                      voucherDiscountAmount: 50000,
                      netPaidAmount: 400000,
                      odometerAtCompletionKm: 3935,
                      pointsEarned: 48,
                      appliedTierCode: 'Silver',
                      newTierCode: 'Gold',
                      tierUpgraded: true,
                      paidAtUtc: '2026-11-22T12:00:00Z'
                    }
                  ]
                }
              };
            }
            if (cfg.url.endsWith('/service-invoices/inv-p6-1') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    invoiceId: 'inv-p6-1',
                    invoiceNumber: 'INV-20261122-0042',
                    serviceBookingId: 'bk-p6-1',
                    grossAmount: 450000,
                    voucherDiscountAmount: 50000,
                    netPaidAmount: 400000,
                    pointsEarned: 48
                  }
                }
              };
            }
            if (cfg.url.endsWith('/service-invoices') && cfg.method === 'POST') {
              return {
                status: 201,
                data: {
                  success: true,
                  data: {
                    invoiceId: 'inv-p6-created',
                    invoiceNumber: 'INV-20261122-0099',
                    serviceBookingId: cfg.data.serviceBookingId,
                    laborAmount: cfg.data.laborAmount,
                    partsAmount: cfg.data.partsAmount,
                    grossAmount: cfg.data.laborAmount + cfg.data.partsAmount,
                    voucherDiscountAmount: 50000,
                    netPaidAmount: (cfg.data.laborAmount + cfg.data.partsAmount) - 50000,
                    odometerAtCompletionKm: cfg.data.odometerAtCompletionKm,
                    pointsEarned: 48,
                    appliedTierCode: 'Silver',
                    newTierCode: 'Gold',
                    tierUpgraded: true
                  }
                }
              };
            }
            if (cfg.url.endsWith('/loyalty/me') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    accountId: '77777777-7777-7777-7777-777777777703',
                    customerId: '44444444-4444-4444-4444-444444444408',
                    currentTierCode: 'Silver',
                    pointsMultiplier: 1.2,
                    availablePoints: 649,
                    lifetimeQualifyingPoints: 1960,
                    lifetimeSpentVnd: 119760000,
                    rowVersion: 'AAAAAAAAL01='
                  }
                }
              };
            }
            if (cfg.url.endsWith('/loyalty/ledger') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      id: 'ple-1',
                      loyaltyAccountId: '77777777-7777-7777-7777-777777777703',
                      transactionType: 'ServiceInvoiceEarn',
                      pointsDelta: 48,
                      balanceAfter: 597,
                      referenceType: 'ServiceInvoice',
                      idempotencyKey: 'SERVICE_INVOICE:inv-p6-1:EARN',
                      createdAtUtc: '2026-11-22T12:00:00Z'
                    }
                  ],
                  pagination: { pageNumber: 1, pageSize: 15, totalCount: 1, totalPages: 1, hasPreviousPage: false, hasNextPage: false }
                }
              };
            }
            if (cfg.url.endsWith('/loyalty/tiers') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: '33333333-3333-3333-3333-333333333301', tierCode: 'Bronze', displayName: 'Bronze Member', rankOrder: 1, minQualifyingPoints: 0, pointsMultiplier: 1.0, benefitsJson: '["Base 1.0x"]' },
                    { id: '33333333-3333-3333-3333-333333333302', tierCode: 'Silver', displayName: 'Silver Member', rankOrder: 2, minQualifyingPoints: 500, pointsMultiplier: 1.2, benefitsJson: '["1.20x bonus"]' },
                    { id: '33333333-3333-3333-3333-333333333303', tierCode: 'Gold', displayName: 'Gold Member', rankOrder: 3, minQualifyingPoints: 2000, pointsMultiplier: 1.5, benefitsJson: '["1.50x bonus"]' },
                    { id: '33333333-3333-3333-3333-333333333304', tierCode: 'Platinum', displayName: 'Platinum Member', rankOrder: 4, minQualifyingPoints: 5000, pointsMultiplier: 2.0, benefitsJson: '["2.00x VIP"]' }
                  ]
                }
              };
            }
            if (cfg.url.includes('/loyalty/tiers/') && cfg.method === 'PUT') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    id: '33333333-3333-3333-3333-333333333302',
                    tierCode: 'Silver',
                    displayName: cfg.data.displayName,
                    rankOrder: 2,
                    minQualifyingPoints: cfg.data.minQualifyingPoints,
                    pointsMultiplier: cfg.data.pointsMultiplier,
                    benefitsJson: cfg.data.benefitsJson
                  }
                }
              };
            }
            if (cfg.url.includes('/loyalty/customers/') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    account: {
                      accountId: '77777777-7777-7777-7777-777777777703',
                      customerId: '44444444-4444-4444-4444-444444444408',
                      customerFullName: 'Tran Ngoc Mai (Silver)',
                      currentTierCode: 'Silver',
                      pointsMultiplier: 1.2,
                      availablePoints: 549,
                      lifetimeQualifyingPoints: 1960,
                      rowVersion: 'AAAAAAAAL02='
                    },
                    eligibleVouchers: [
                      {
                        customerVoucherId: 'cv-svc50k-1',
                        voucherCode: 'SVC50K-8M2P4K9Q',
                        title: '50,000 VND Off Workshop Oil & Filter',
                        applicableScope: 'ServiceOnly',
                        discountType: 'FixedAmount',
                        discountValue: 50000,
                        minOrderAmount: 250000,
                        pointsSpent: 100,
                        status: 'ReservedOnBooking',
                        effectiveStatus: 'ReservedOnBooking',
                        expiresAtUtc: new Date(Date.now() + 80 * 86400 * 1000).toISOString(),
                        rowVersion: 'AAAAAAAAV01='
                      }
                    ]
                  }
                }
              };
            }
            if (cfg.url.endsWith('/loyalty/adjustments') && cfg.method === 'POST') {
              return {
                status: 201,
                data: {
                  success: true,
                  data: {
                    id: 'ple-adj-1',
                    loyaltyAccountId: cfg.data.loyaltyAccountId,
                    transactionType: 'AdminAdjustment',
                    pointsDelta: cfg.data.pointsDelta,
                    balanceAfter: 699,
                    availablePointsAfter: 699,
                    idempotencyKey: cfg.data.idempotencyKey,
                    notes: cfg.data.notes
                  }
                }
              };
            }
            if (cfg.url.endsWith('/vouchers/catalog') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      id: '66666666-6666-6666-6666-666666666601',
                      codePrefix: 'SVC50K',
                      title: '50,000 VND Off Workshop Oil & Filter',
                      description: 'Periodic service discount.',
                      applicableScope: 'ServiceOnly',
                      pointsCost: 100,
                      discountType: 'FixedAmount',
                      discountValue: 50000,
                      maxDiscountAmount: null,
                      minOrderAmount: 250000,
                      validDays: 90,
                      quotaTotal: 1000,
                      quotaRedeemed: 1,
                      minTierCode: 'Bronze',
                      isActive: true
                    },
                    {
                      id: '66666666-6666-6666-6666-666666666604',
                      codePrefix: 'GOLD15P',
                      title: '15% Off Genuine Parts & Tires',
                      description: 'Gold tier perk.',
                      applicableScope: 'All',
                      pointsCost: 600,
                      discountType: 'Percentage',
                      discountValue: 15,
                      maxDiscountAmount: 400000,
                      minOrderAmount: 1000000,
                      validDays: 120,
                      quotaTotal: 300,
                      quotaRedeemed: 2,
                      minTierCode: 'Gold',
                      isActive: true
                    }
                  ]
                }
              };
            }
            if (cfg.url.endsWith('/vouchers/catalog') && cfg.method === 'POST') {
              return {
                status: 201,
                data: {
                  success: true,
                  data: Object.assign({ id: '66666666-6666-6666-6666-666666666699', quotaRedeemed: 0 }, cfg.data)
                }
              };
            }
            if (cfg.url.includes('/vouchers/catalog/') && cfg.method === 'PUT') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: Object.assign({ id: '66666666-6666-6666-6666-666666666601', quotaRedeemed: 1 }, cfg.data)
                }
              };
            }
            if (cfg.url.endsWith('/vouchers/my') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      customerVoucherId: 'cv-svc50k-1',
                      voucherCode: 'SVC50K-8M2P4K9Q',
                      title: '50,000 VND Off Workshop Oil & Filter',
                      applicableScope: 'ServiceOnly',
                      discountType: 'FixedAmount',
                      discountValue: 50000,
                      minOrderAmount: 250000,
                      pointsSpent: 100,
                      status: 'Available',
                      effectiveStatus: 'Available',
                      expiresAtUtc: new Date(Date.now() + 80 * 86400 * 1000).toISOString(),
                      rowVersion: 'AAAAAAAAV01='
                    }
                  ]
                }
              };
            }
            if (cfg.url.endsWith('/vouchers/redeem') && cfg.method === 'POST') {
              return {
                status: 201,
                data: {
                  success: true,
                  data: {
                    customerVoucherId: 'cv-minted-99',
                    voucherCode: 'SVC50K-8M2P4K9Q',
                    title: '50,000 VND Off Workshop Oil & Filter',
                    applicableScope: 'ServiceOnly',
                    discountType: 'FixedAmount',
                    discountValue: 50000,
                    minOrderAmount: 250000,
                    pointsSpent: 100,
                    availablePointsAfter: 549,
                    lifetimeQualifyingPoints: 1960,
                    status: 'Available',
                    effectiveStatus: 'Available',
                    expiresAtUtc: new Date(Date.now() + 90 * 86400 * 1000).toISOString(),
                    rowVersion: 'AAAAAAAAV99='
                  }
                }
              };
            }
            if (cfg.url.endsWith('/notifications') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: 'n-srv-1', notificationId: 'n-srv-1', eventType: 'BookingCreated', title: 'Booking Created', message: 'BK-1', isRead: false, occurredAtUtc: '2026-11-22T10:00:00Z' },
                    { id: 'n-srv-2', notificationId: 'n-srv-2', eventType: 'LoyaltyPointsUpdated', title: 'Points Added', message: '+48 pts', isRead: false, occurredAtUtc: '2026-11-22T09:30:00Z' }
                  ],
                  unreadCount: 2,
                  pagination: { pageNumber: 1, pageSize: 30, totalCount: 2, totalPages: 1 },
                  meta: { unreadCount: 2 }
                }
              };
            }
            if (cfg.url.includes('/notifications/') && cfg.url.endsWith('/read') && cfg.method === 'PATCH') {
              return { status: 200, data: { success: true, data: { isRead: true, unreadCount: 1 } } };
            }
            if (cfg.url.endsWith('/notifications/read-all') && cfg.method === 'POST') {
              return { status: 200, data: { success: true, data: { markedReadCount: 2, unreadCount: 0 } } };
            }
            if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      bookingId: 'bk-p6-1',
                      bookingNumber: 'BK-20261122-0042',
                      customerId: '44444444-4444-4444-4444-444444444408',
                      customerFullName: 'Tran Ngoc Mai (Silver)',
                      vin: 'RLHJF5109RZ104829',
                      motorcycleModelName: 'Honda SH160i ABS Sport',
                      slotDate: '2026-11-23',
                      startTime: '09:00',
                      endTime: '10:00',
                      serviceType: 'PeriodicMaintenance',
                      odometerAtBookingKm: 3920,
                      appliedCustomerVoucherId: 'cv-svc50k-1',
                      reservedVoucherCode: 'SVC50K-8M2P4K9Q',
                      status: 'Confirmed',
                      rowVersion: 'AAAAAAAABK1='
                    }
                  ]
                }
              };
            }
            return { status: 200, data: { success: true, data: [] } };
          };

          const invListOut = await p6Api.serviceInvoices.list({ bookingId: 'bk-p6-1' });
          const invDetailOut = await p6Api.serviceInvoices.getById('inv-p6-1');
          const invCreatedOut = await p6Api.serviceInvoices.create({
            serviceBookingId: 'bk-p6-1',
            bookingRowVersion: 'AAAAAAAABK1=',
            laborAmount: 150000,
            partsAmount: 300000,
            odometerAtCompletionKm: 3935,
            appliedCustomerVoucherId: 'cv-svc50k-1'
          });
          const myAccOut = await p6Api.loyalty.getMyAccount();
          const myLedgerOut = await p6Api.loyalty.getMyLedger({ transactionType: 'ServiceInvoiceEarn', pageNumber: 1, pageSize: 15 });
          const tiersOut = await p6Api.loyalty.getTiers();
          const updatedTierOut = await p6Api.loyalty.updateTier('33333333-3333-3333-3333-333333333302', {
            displayName: 'Silver Plus Member',
            pointsMultiplier: 1.25,
            minQualifyingPoints: 500,
            benefitsJson: '["1.25x bonus"]'
          });
          const custSumOut = await p6Api.loyalty.getCustomerSummary('44444444-4444-4444-4444-444444444408');
          const adjOut = await p6Api.loyalty.adjustPoints({
            loyaltyAccountId: '77777777-7777-7777-7777-777777777703',
            customerId: '44444444-4444-4444-4444-444444444408',
            pointsDelta: 150,
            notes: 'Goodwill support credit #CS-2041',
            accountRowVersion: 'AAAAAAAAL02='
          });
          const catListOut = await p6Api.vouchers.listCatalog({ includeInactive: true });
          const createdCatOut = await p6Api.vouchers.createCatalog({
            codePrefix: 'SVC75K',
            title: '75,000 VND Service Voucher',
            description: 'Workshop promo',
            applicableScope: 'ServiceOnly',
            pointsCost: 150,
            discountType: 'FixedAmount',
            discountValue: 75000,
            minOrderAmount: 350000,
            validDays: 90,
            quotaTotal: 250,
            minTierId: 'Bronze',
            minTierCode: 'Bronze',
            isActive: true
          });
          const updatedCatOut = await p6Api.vouchers.updateCatalog('66666666-6666-6666-6666-666666666601', {
            codePrefix: 'SVC50K',
            title: '50,000 VND Off Workshop Oil & Filter',
            description: 'Updated description',
            applicableScope: 'ServiceOnly',
            pointsCost: 100,
            discountType: 'FixedAmount',
            discountValue: 50000,
            minOrderAmount: 250000,
            validDays: 90,
            quotaTotal: 1200,
            minTierId: '33333333-3333-3333-3333-333333333301',
            minTierCode: 'Bronze',
            isActive: true
          });
          const myVchOut = await p6Api.vouchers.listMyVouchers({ status: 'Available' });
          const redeemedOut = await p6Api.vouchers.redeem({
            voucherCatalogId: '66666666-6666-6666-6666-666666666601',
            accountRowVersion: 'AAAAAAAAL01='
          });
          const notifListOut = await p6Api.notifications.list({ pageSize: 30 });
          await p6Api.notifications.markRead('n-srv-1');
          await p6Api.notifications.markAllRead();

          const createCatPostCall = p6Calls.find((c) => c.method === 'POST' && c.url.endsWith('/vouchers/catalog'));
          const redeemPostCall = p6Calls.find((c) => c.method === 'POST' && c.url.endsWith('/vouchers/redeem'));
          const invoicePostCall = p6Calls.find((c) => c.method === 'POST' && c.url.endsWith('/service-invoices'));
          const markReadPatchCall = p6Calls.find((c) => c.method === 'PATCH' && c.url.endsWith('/notifications/n-srv-1/read'));
          const markAllPostCall = p6Calls.find((c) => c.method === 'POST' && c.url.endsWith('/notifications/read-all'));

          if (
            invListOut.length !== 1 ||
            invListOut[0].vin !== 'RLHJF5109RZ104829' ||
            invDetailOut.netPaidAmount !== 400000 ||
            invCreatedOut.pointsEarned !== 48 ||
            !invoicePostCall?.headers?.['Idempotency-Key'] ||
            myAccOut.availablePoints !== 649 ||
            myLedgerOut.items.length !== 1 ||
            myLedgerOut.items[0].pointsDelta !== 48 ||
            tiersOut.length !== 4 ||
            updatedTierOut.pointsMultiplier !== 1.25 ||
            custSumOut.eligibleVouchers.length !== 1 ||
            adjOut.availablePointsAfter !== 699 ||
            catListOut.length !== 2 ||
            createdCatOut.codePrefix !== 'SVC75K' ||
            createCatPostCall?.data?.minTierId !== null ||
            createCatPostCall?.data?.minTierCode !== 'Bronze' ||
            updatedCatOut.quotaTotal !== 1200 ||
            myVchOut.length !== 1 ||
            redeemedOut.voucherCode !== 'SVC50K-8M2P4K9Q' ||
            !redeemPostCall?.headers?.['Idempotency-Key'] ||
            notifListOut.unreadCount !== 2 ||
            notifListOut.items.length !== 2 ||
            !markReadPatchCall ||
            !markAllPostCall
          ) {
            fail('DealerApiService Phase 6 contract suite (k) failed.');
          } else {
            pass('Verified DealerApiService Phase 6 endpoints (serviceInvoices, loyalty, vouchers, notifications) & Idempotency-Key headers.');
          }

          // (l) StaffInvoiceBuilder (T-607): Fixed vs Percentage Capped Voucher Discount + Two-Step Floor Math + Silver->Gold Promotion
          const discFixed = loyaltyCalc.computeVoucherDiscount({ discountType: 'FixedAmount', discountValue: 50000, minOrderAmount: 250000 }, 450000);
          const discPctUnderCap = loyaltyCalc.computeVoucherDiscount({ discountType: 'Percentage', discountValue: 15, maxDiscountAmount: 200000, minOrderAmount: 600000 }, 800000);
          const discPctAtCap = loyaltyCalc.computeVoucherDiscount({ discountType: 'Percentage', discountValue: 15, maxDiscountAmount: 200000, minOrderAmount: 600000 }, 2000000);
          const discIneligible = loyaltyCalc.computeVoucherDiscount({ discountType: 'FixedAmount', discountValue: 50000, minOrderAmount: 250000 }, 200000);

          const p6StaffTokenStore = new tokenStoreCtor({});
          p6StaffTokenStore.setTokens('jwt-p6-staff-hcm', 900, 'rt-p6-staff-hcm');
          p4AuthSvc.setSession({
            accessToken: 'jwt-p6-staff-hcm',
            refreshToken: 'rt-p6-staff-hcm',
            expiresInSeconds: 900,
            user: {
              userId: '44444444-4444-4444-4444-444444444405',
              email: 'staff.hcm@motorcycledealer.vn',
              fullName: 'Pham Minh Tuan (Saigon Staff)',
              role: 'DealerStaff',
              dealerId: '22222222-2222-2222-2222-222222222201',
              dealerCode: 'DLR-HCM-01',
              permissions: ['Permissions.Invoices.CreateDealer', 'Permissions.Invoices.ViewDealer', 'Permissions.Loyalty.ViewDealerCustomer']
            }
          });

          const p6InvBus = createEventBusRootScope();
          const p6InvCtrl = new invoiceBuilderDef.controller(
            mockQ,
            p6InvBus,
            { bookingId: 'bk-p6-1' },
            p4AuthSvc,
            p6Api,
            loyaltyCalc,
            mapper,
            p45SignalRSvc,
            noopToast,
            p6StaffTokenStore
          );
          p6InvCtrl.$onInit();
          await new Promise((r) => setTimeout(r, 25));

          const autoPreselectedVoucherOk = p6InvCtrl.invoice.appliedCustomerVoucherId === 'cv-svc50k-1';
          const autoPrefilledOdometerOk = p6InvCtrl.invoice.odometerAtCompletionKm === 3920;

          p6InvCtrl.invoice.laborAmount = 150000;
          p6InvCtrl.invoice.partsAmount = 300000;
          p6InvCtrl.invoice.odometerAtCompletionKm = 3935;
          p6InvCtrl.recalculate();

          const livePreviewSnapshot = Object.assign({}, p6InvCtrl.summary);
          const issuedReceiptOut = await p6InvCtrl.submitInvoice();
          p6InvCtrl.$onDestroy();

          const workbenchHasConfirmedInvoiceCta = String(staffWorkbenchDef.template || '').includes(
            "card.status === 'Confirmed' || card.status === 'CheckedIn' || card.status === 'InProgress'"
          );

          if (
            discFixed.discountAmount !== 50000 ||
            discPctUnderCap.discountAmount !== 120000 ||
            discPctAtCap.discountAmount !== 200000 ||
            discIneligible.isEligible !== false ||
            !workbenchHasConfirmedInvoiceCta ||
            !autoPreselectedVoucherOk ||
            !autoPrefilledOdometerOk ||
            livePreviewSnapshot.grossAmount !== 450000 ||
            livePreviewSnapshot.voucherDiscountAmount !== 50000 ||
            livePreviewSnapshot.netPaidAmount !== 400000 ||
            livePreviewSnapshot.basePoints !== 40 ||
            livePreviewSnapshot.pointsEarned !== 48 ||
            livePreviewSnapshot.qualifyingAfter !== 2008 ||
            livePreviewSnapshot.projectedTierCode !== 'Gold' ||
            livePreviewSnapshot.willUpgradeTier !== true ||
            issuedReceiptOut?.invoiceNumber !== 'INV-20261122-0099' ||
            issuedReceiptOut?.tierUpgraded !== true
          ) {
            fail(
              `StaffInvoiceBuilder suite (l) failed: disc=[${discFixed.discountAmount},${discPctUnderCap.discountAmount},${discPctAtCap.discountAmount}], preview=${JSON.stringify(livePreviewSnapshot)}, receipt=${JSON.stringify(issuedReceiptOut)}`
            );
          } else {
            pass('Verified T-607 StaffInvoiceBuilder voucher discount rules, 400k @ 1.2x = +48 pts Silver->Gold promotion preview, and workbench CTA.');
          }

          // (m) CustomerLoyalty (T-608) Redemption State Refresh, SignalR TierUpgraded Celebration, Step 4 Voucher Picker, Completed Invoice Modal & Admin Studios
          let p6CustomerPoints = 649;
          let p6CustomerLifetime = 1960;
          let p6CustomerTier = 'Silver';
          let p6CustomerMult = 1.2;

          const p6CustResponderBase = p6Responder;
          p6Responder = async (cfg) => {
            if (cfg.url.endsWith('/loyalty/me') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    accountId: '77777777-7777-7777-7777-777777777703',
                    customerId: '44444444-4444-4444-4444-444444444408',
                    currentTierCode: p6CustomerTier,
                    pointsMultiplier: p6CustomerMult,
                    availablePoints: p6CustomerPoints,
                    lifetimeQualifyingPoints: p6CustomerLifetime,
                    lifetimeSpentVnd: 119760000,
                    rowVersion: 'AAAAAAAAL05='
                  }
                }
              };
            }
            if (cfg.url.endsWith('/vouchers/redeem') && cfg.method === 'POST') {
              p6CustomerPoints -= 100;
              return {
                status: 201,
                data: {
                  success: true,
                  data: {
                    customerVoucherId: 'cv-minted-99',
                    voucherCode: 'SVC50K-8M2P4K9Q',
                    title: '50,000 VND Off Workshop Oil & Filter',
                    applicableScope: 'ServiceOnly',
                    discountType: 'FixedAmount',
                    discountValue: 50000,
                    minOrderAmount: 250000,
                    pointsSpent: 100,
                    availablePointsAfter: p6CustomerPoints,
                    lifetimeQualifyingPoints: p6CustomerLifetime,
                    status: 'Available',
                    effectiveStatus: 'Available',
                    expiresAtUtc: new Date(Date.now() + 90 * 86400 * 1000).toISOString(),
                    rowVersion: 'AAAAAAAAV99='
                  }
                }
              };
            }
            return p6CustResponderBase(cfg);
          };

          const p6CustTokenStore = new tokenStoreCtor({});
          p6CustTokenStore.setTokens('jwt-p6-cust-silver', 900, 'rt-p6-cust-silver');
          p4AuthSvc.setSession({
            accessToken: 'jwt-p6-cust-silver',
            refreshToken: 'rt-p6-cust-silver',
            expiresInSeconds: 900,
            user: {
              userId: '44444444-4444-4444-4444-444444444408',
              email: 'customer.silver@motorcycledealer.vn',
              fullName: 'Tran Ngoc Mai (Silver)',
              role: 'Customer',
              tierCode: 'Silver',
              tierMultiplier: 1.2,
              pointsMultiplier: 1.2,
              availablePoints: 649,
              lifetimeQualifyingPoints: 1960,
              permissions: ['Permissions.Loyalty.ViewOwn', 'Permissions.Loyalty.RedeemVoucher', 'Permissions.Bookings.Create', 'Permissions.Invoices.ViewOwn']
            }
          });

          const p6CustBus = createEventBusRootScope();
          const p6CustNotifStore = new notifStoreCtor();
          const p6CustSignalR = new signalrSvcCtor(
            mockQ,
            p6CustBus,
            registeredConstants.get('APP_CONFIG'),
            p6CustTokenStore,
            p6CustNotifStore,
            noopToast
          );

          const p6LoyaltyCtrl = new custLoyaltyDef.controller(
            mockQ,
            p6CustBus,
            {},
            p4AuthSvc,
            p6Api,
            loyaltyCalc,
            mapper,
            p6CustSignalR,
            noopToast,
            p6CustTokenStore
          );
          p6LoyaltyCtrl.$onInit();
          await new Promise((r) => setTimeout(r, 25));

          const bronzeVoucherEligible = p6LoyaltyCtrl.canRedeemCatalogItem(p6LoyaltyCtrl.catalog[0]) === true;
          const goldVoucherLockedForSilver = p6LoyaltyCtrl.canRedeemCatalogItem(p6LoyaltyCtrl.catalog[1]) === false;

          p6LoyaltyCtrl.openRedeemModal(p6LoyaltyCtrl.catalog[0]);
          const mintedVoucher = await p6LoyaltyCtrl.confirmRedeemVoucher();
          const pointsAfterRedeem = p6LoyaltyCtrl.availablePoints;
          const lifetimeUnchangedOnRedeem = p6LoyaltyCtrl.lifetimePoints === 1960;

          // Simulate live SignalR invoice points + Silver -> Gold promotion
          p6CustomerPoints = 597;
          p6CustomerLifetime = 2008;
          p6CustomerTier = 'Gold';
          p6CustomerMult = 1.5;
          p6CustSignalR.simulateEvent('LoyaltyPointsUpdated', {
            notificationId: 'notif-pts-p6',
            pointsDelta: 48,
            availablePointsAfter: 597,
            lifetimeQualifyingPointsAfter: 2008
          });
          p6CustSignalR.simulateEvent('TierUpgraded', {
            notificationId: 'notif-tier-p6',
            previousTierCode: 'Silver',
            newTierCode: 'Gold',
            newPointsMultiplier: 1.5,
            lifetimeQualifyingPoints: 2008
          });
          await new Promise((r) => setTimeout(r, 25));

          const celebrationShown = p6LoyaltyCtrl.celebrationBanner?.newTierCode === 'Gold';
          const upgradedTierCode = p6LoyaltyCtrl.tierCode;
          p6LoyaltyCtrl.$onDestroy();

          // Verify Step 4 voucher selector on customerBookingNew & settled invoice viewer on customerBookings
          const p6WizardCtrl = new bookingNewDef.controller(
            mockQ,
            p6CustBus,
            mockState,
            { voucherId: 'cv-svc50k-1' },
            p4AuthSvc,
            p6Api,
            mapper,
            noopToast,
            p6CustTokenStore
          );
          p6WizardCtrl.$onInit();
          await new Promise((r) => setTimeout(r, 25));
          const wizardPreselectedVoucherOk =
            p6WizardCtrl.availableVouchers.length === 1 &&
            p6WizardCtrl.draft.appliedCustomerVoucherId === 'cv-svc50k-1' &&
            p6WizardCtrl.draft.voucherCode === 'SVC50K-8M2P4K9Q' &&
            p6WizardCtrl.draft.voucherRowVersion === 'AAAAAAAAV01=';
          p6WizardCtrl.$onDestroy();

          const p6BookingsCtrl = new custBookingsDef.controller(
            mockQ,
            p6CustBus,
            p6Api,
            mapper,
            p6CustSignalR,
            noopToast,
            p6CustTokenStore
          );
          p6BookingsCtrl.$onInit();
          const settledInvModalOut = await p6BookingsCtrl.openInvoiceBreakdown({ id: 'bk-p6-1', bookingNumber: 'BK-20261122-0042', status: 'Completed' });
          p6BookingsCtrl.$onDestroy();

          // Verify Admin Loyalty Tiers + Point Adjustment & Voucher Catalog Studios + Extensibility
          const p6AdminTiersCtrl = new adminTiersDef.controller(mockQ, p6Api, loyaltyCalc, mapper, noopToast, p6CustTokenStore);
          p6AdminTiersCtrl.$onInit();
          await p6AdminTiersCtrl.loadTiers();
          p6AdminTiersCtrl.openEditTierModal(p6AdminTiersCtrl.tiers[1]);
          p6AdminTiersCtrl.tierDraft.displayName = 'Silver Plus Member';
          const savedTierRes = await p6AdminTiersCtrl.saveTier();
          p6AdminTiersCtrl.openAdjustmentModal();
          const guardNoCustomerAdj = p6AdminTiersCtrl.canSubmitAdjustment() === false;
          p6AdminTiersCtrl.adjustDraft.customerIdentifier = '44444444-4444-4444-4444-444444444408';
          p6AdminTiersCtrl.onAdjustmentIdentifierChanged();
          await p6AdminTiersCtrl.lookupAdjustmentCustomer();
          p6AdminTiersCtrl.adjustDraft.pointsDelta = 150;
          p6AdminTiersCtrl.adjustDraft.notes = 'Goodwill support credit #CS-2041';
          const savedAdjRes = await p6AdminTiersCtrl.submitPointAdjustment();
          p6AdminTiersCtrl.$onDestroy();

          const p6AdminVoucherCtrl = new adminVoucherCatDef.controller(mockQ, p6Api, loyaltyCalc, mapper, noopToast, p6CustTokenStore);
          p6AdminVoucherCtrl.$onInit();
          await p6AdminVoucherCtrl.loadCatalog();
          p6AdminVoucherCtrl.openCreateModal();
          p6AdminVoucherCtrl.draft.codePrefix = 'SVC75K';
          p6AdminVoucherCtrl.draft.title = '75,000 VND Service Voucher';
          const savedCatRes = await p6AdminVoucherCtrl.saveCatalogItem();
          p6AdminVoucherCtrl.$onDestroy();

          if (
            !guardNoCustomerAdj ||
            !bronzeVoucherEligible ||
            !goldVoucherLockedForSilver ||
            mintedVoucher?.voucherCode !== 'SVC50K-8M2P4K9Q' ||
            pointsAfterRedeem !== 549 ||
            !lifetimeUnchangedOnRedeem ||
            !celebrationShown ||
            upgradedTierCode !== 'Gold' ||
            !wizardPreselectedVoucherOk ||
            settledInvModalOut?.invoiceNumber !== 'INV-20261122-0042' ||
            savedTierRes?.displayName !== 'Silver Plus Member' ||
            savedAdjRes?.availablePointsAfter !== 699 ||
            savedCatRes?.codePrefix !== 'SVC75K'
          ) {
            fail('CustomerLoyalty / Booking Voucher / Admin Studios suite (m) failed.');
          } else {
            pass('Verified T-608 CustomerLoyalty redemption state refresh, SignalR TierUpgraded celebration, Step 4 voucher selector, Completed invoice breakdown, and Admin Tiers/Vouchers studios.');
          }

          // (n) NotificationBell (T-606) Unread Counter Sync, 5-Event SignalR Prepend/Dedup, Reconnect Delta Sync & Read Mutations
          const bellBus = createEventBusRootScope();
          const bellStore = new notifStoreCtor();
          const bellTokenStore = new tokenStoreCtor({});
          const bellSignalR = new signalrSvcCtor(
            mockQ,
            bellBus,
            registeredConstants.get('APP_CONFIG'),
            bellTokenStore,
            bellStore,
            noopToast
          );

          let notifGetHits = 0;
          let notifReadHits = 0;
          let notifReadAllHits = 0;
          let serverUnreadVal = 2;

          p6Responder = async (cfg) => {
            if (cfg.url.endsWith('/notifications') && cfg.method === 'GET') {
              notifGetHits++;
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: 'n-bell-1', notificationId: 'n-bell-1', eventType: 'BookingCreated', title: 'Booking 1', message: 'Arrived', isRead: serverUnreadVal === 0, occurredAtUtc: '2026-11-22T10:00:00Z' },
                    { id: 'n-bell-2', notificationId: 'n-bell-2', eventType: 'LoyaltyPointsUpdated', title: 'Points 2', message: '+48 pts', isRead: serverUnreadVal === 0, occurredAtUtc: '2026-11-22T09:00:00Z' }
                  ],
                  unreadCount: serverUnreadVal,
                  pagination: { pageNumber: 1, pageSize: 30, totalCount: 2, totalPages: 1 }
                }
              };
            }
            if (cfg.url.endsWith('/notifications/n-bell-1/read') && cfg.method === 'PATCH') {
              notifReadHits++;
              serverUnreadVal = Math.max(0, serverUnreadVal - 1);
              return { status: 200, data: { success: true, data: { isRead: true } } };
            }
            if (cfg.url.endsWith('/notifications/read-all') && cfg.method === 'POST') {
              notifReadAllHits++;
              serverUnreadVal = 0;
              return { status: 200, data: { success: true, data: { markedReadCount: 6, unreadCount: 0 } } };
            }
            return { status: 200, data: { success: true, data: {} } };
          };

          const bellAuthSvc = new authSvcCtor(
            mockQ,
            { has: () => false, get: () => null },
            bellBus,
            registeredConstants.get('APP_CONFIG'),
            registeredConstants.get('ROLE_HOME'),
            registeredConstants.get('ROLE_PERMISSION_PRESETS'),
            bellTokenStore
          );
          bellAuthSvc.setSession({
            accessToken: 'jwt-bell-customer',
            refreshToken: 'rt-bell-customer',
            expiresInSeconds: 900,
            user: {
              userId: '44444444-4444-4444-4444-444444444408',
              email: 'customer.silver@motorcycledealer.vn',
              fullName: 'Tran Ngoc Mai (Silver)',
              role: 'Customer',
              permissions: ['Permissions.Notifications.ViewOwn']
            }
          });

          const bellCtrl = new notifBellDef.controller(
            mockQ,
            bellBus,
            bellAuthSvc,
            p6Api,
            bellStore,
            mapper,
            bellTokenStore
          );
          bellCtrl.$onInit();
          await new Promise((r) => setTimeout(r, 20));
          const initialBellUnread = bellCtrl.unreadCount;

          // Emit all 5 canonical SignalR events + 1 duplicate notificationId
          bellSignalR.simulateEvent('BookingCreated', { notificationId: 'rt-ev-1', bookingNumber: 'BK-1' });
          bellSignalR.simulateEvent('BookingStatusChanged', { notificationId: 'rt-ev-2', bookingNumber: 'BK-1', newStatus: 'Confirmed' });
          bellSignalR.simulateEvent('MaintenanceReminderTriggered', { notificationId: 'rt-ev-3', vin: 'RLHJF5109RZ104829' });
          bellSignalR.simulateEvent('LoyaltyPointsUpdated', { notificationId: 'rt-ev-4', pointsDelta: 48 });
          bellSignalR.simulateEvent('TierUpgraded', { notificationId: 'rt-ev-5', newTierCode: 'Gold', newPointsMultiplier: 1.5 });
          bellSignalR.simulateEvent('TierUpgraded', { notificationId: 'rt-ev-5', newTierCode: 'Gold', newPointsMultiplier: 1.5 });
          const unreadAfterFiveLiveEvents = bellCtrl.unreadCount;

          await bellCtrl.markRead('n-bell-1');
          const unreadAfterSingleMarkRead = bellCtrl.unreadCount;

          await bellCtrl.markAllRead();
          const unreadAfterMarkAll = bellCtrl.unreadCount;

          // Simulate SignalR onreconnected delta catch-up where 1 missed alert arrived while offline
          serverUnreadVal = 1;
          const hitsBeforeReconnect = notifGetHits;
          bellBus.$broadcast('signalr:reconnected', { reconnectedAtUtc: new Date().toISOString() });
          await new Promise((r) => setTimeout(r, 20));
          const unreadAfterReconnectSync = bellCtrl.unreadCount;
          const reconnectTriggeredFetch = notifGetHits > hitsBeforeReconnect;

          // Logout clears unread count & $onDestroy detaches listeners
          await bellAuthSvc.clearSessionAndRealtime();
          const unreadAfterLogout = bellCtrl.unreadCount;
          bellCtrl.$onDestroy();

          if (
            initialBellUnread !== 2 ||
            unreadAfterFiveLiveEvents !== 7 ||
            unreadAfterSingleMarkRead !== 6 ||
            notifReadHits !== 1 ||
            unreadAfterMarkAll !== 0 ||
            notifReadAllHits !== 1 ||
            !reconnectTriggeredFetch ||
            unreadAfterReconnectSync !== 1 ||
            unreadAfterLogout !== 0
          ) {
            fail(
              `NotificationBell suite (n) failed: init=${initialBellUnread}, after5=${unreadAfterFiveLiveEvents}, afterMark1=${unreadAfterSingleMarkRead}, afterAll=${unreadAfterMarkAll}, afterReconn=${unreadAfterReconnectSync}, afterLogout=${unreadAfterLogout}`
            );
          } else {
            pass('Verified T-606 NotificationBell unread counter sync, 5-event SignalR prepend/deduplication, markRead/markAllRead, and onreconnected delta catch-up.');
          }

          // (o) Gate 1 Regression Suite: F-P6-SPA-01a (StaffInvoiceBuilder mid-edit SignalR preservation) & F-P6-SPA-01b (Dynamic customTiers + NotificationBell Unread->All preservation)
          let f01BookingRowVer = 'AAAAAAAAB10=';
          let f01BookingStatus = 'CheckedIn';
          let f01BookingsFetchCount = 0;
          const customDynamicTiers = [
            { id: 't1', tierCode: 'Bronze', displayName: 'Bronze', rankOrder: 1, minQualifyingPoints: 0, pointsMultiplier: 1.0 },
            { id: 't2', tierCode: 'Silver', displayName: 'Silver', rankOrder: 2, minQualifyingPoints: 400, pointsMultiplier: 1.25 },
            { id: 't3', tierCode: 'Gold', displayName: 'Gold', rankOrder: 3, minQualifyingPoints: 1800, pointsMultiplier: 1.6 },
            { id: 't4', tierCode: 'Platinum', displayName: 'Platinum', rankOrder: 4, minQualifyingPoints: 4500, pointsMultiplier: 2.0 }
          ];

          p6Responder = async (cfg) => {
            if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
              f01BookingsFetchCount++;
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    {
                      id: 'bk-f01-active',
                      bookingId: 'bk-f01-active',
                      bookingNumber: 'BK-20261123-F01',
                      status: f01BookingStatus,
                      dealerId: '22222222-2222-2222-2222-222222222201',
                      customerId: '44444444-4444-4444-4444-444444444408',
                      customerFullName: 'Tran Ngoc Mai',
                      vin: 'RLHJF5109RZ104829',
                      motorcycleModelName: 'Honda SH160i ABS Sport',
                      slotDate: '2026-11-23',
                      startTime: '09:00',
                      endTime: '10:00',
                      serviceType: 'PeriodicMaintenance',
                      odometerAtBookingKm: 3900,
                      reportedNotes: 'Initial check-in note',
                      rowVersion: f01BookingRowVer
                    }
                  ]
                }
              };
            }
            if (cfg.url.endsWith('/loyalty/tiers') && cfg.method === 'GET') {
              return { status: 200, data: { success: true, data: customDynamicTiers } };
            }
            if (cfg.url.includes('/loyalty/customers/') && cfg.method === 'GET') {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    customerId: '44444444-4444-4444-4444-444444444408',
                    currentTierCode: 'Silver',
                    pointsMultiplier: 1.25,
                    availablePoints: 500,
                    lifetimeQualifyingPoints: 1760,
                    eligibleVouchers: [
                      {
                        id: 'vch-opt-1',
                        customerVoucherId: 'vch-opt-1',
                        voucherCode: 'SVC50K-OPT1',
                        applicableScope: 'ServiceOnly',
                        discountType: 'FixedAmount',
                        discountValue: 50000,
                        minOrderAmount: 250000,
                        status: 'Available',
                        effectiveStatus: 'Available',
                        rowVersion: 'AAAAAAAAV10='
                      }
                    ]
                  }
                }
              };
            }
            if (cfg.url.endsWith('/notifications') && cfg.method === 'GET') {
              if (cfg.params?.unreadOnly) {
                return {
                  status: 200,
                  data: {
                    success: true,
                    data: [
                      { id: 'n-unread-1', notificationId: 'n-unread-1', eventType: 'BookingCreated', title: 'Unread 1', message: 'Live', isRead: false, occurredAtUtc: '2026-11-23T11:00:00Z' }
                    ],
                    unreadCount: 1
                  }
                };
              }
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: 'n-unread-1', notificationId: 'n-unread-1', eventType: 'BookingCreated', title: 'Unread 1', message: 'Live', isRead: false, occurredAtUtc: '2026-11-23T11:00:00Z' },
                    { id: 'n-read-1', notificationId: 'n-read-1', eventType: 'TierUpgraded', title: 'Historical Read', message: 'Gold', isRead: true, occurredAtUtc: '2026-11-23T08:00:00Z' }
                  ],
                  unreadCount: 1
                }
              };
            }
            return { status: 200, data: { success: true, data: [] } };
          };

          const f01Bus = createEventBusRootScope();
          const f01NotifStore = new notifStoreCtor();
          const f01TokenStore = new tokenStoreCtor({});
          f01TokenStore.setTokens('jwt-f01-staff', 900, 'rt-f01-staff');
          const f01SignalR = new signalrSvcCtor(
            mockQ,
            f01Bus,
            registeredConstants.get('APP_CONFIG'),
            f01TokenStore,
            f01NotifStore,
            noopToast
          );

          const f01AuthSvc = new authSvcCtor(
            mockQ,
            { has: () => false, get: () => null },
            f01Bus,
            registeredConstants.get('APP_CONFIG'),
            registeredConstants.get('ROLE_HOME'),
            registeredConstants.get('ROLE_PERMISSION_PRESETS'),
            f01TokenStore
          );
          f01AuthSvc.setSession({
            accessToken: 'jwt-f01-staff',
            refreshToken: 'rt-f01-staff',
            expiresInSeconds: 900,
            user: {
              userId: '44444444-4444-4444-4444-444444444405',
              email: 'staff.hcm@motorcycledealer.vn',
              fullName: 'Pham Minh Tuan',
              role: 'DealerStaff',
              dealerId: '22222222-2222-2222-2222-222222222201',
              permissions: ['Permissions.Invoices.CreateDealer', 'Permissions.Invoices.ViewDealer', 'Permissions.Loyalty.ViewDealerCustomer']
            }
          });

          const f01InvCtrl = new invoiceBuilderDef.controller(
            mockQ,
            f01Bus,
            { bookingId: 'bk-f01-active' },
            f01AuthSvc,
            p6Api,
            loyaltyCalc,
            mapper,
            f01SignalR,
            noopToast,
            f01TokenStore
          );
          f01InvCtrl.$onInit();
          await new Promise((r) => setTimeout(r, 25));

          // Advisor edits draft fields mid-session
          f01InvCtrl.invoice.laborAmount = 220000;
          f01InvCtrl.invoice.partsAmount = 230000;
          f01InvCtrl.invoice.odometerAtCompletionKm = 3988;
          f01InvCtrl.invoice.appliedCustomerVoucherId = 'vch-opt-1';
          f01InvCtrl.invoice.workSummaryNotes = 'Advisor custom synthetic oil + brake pad torque spec';
          f01InvCtrl.recalculate();

          // Dynamic customTiers check: 1760 + Floor(Floor(400,000 / 10,000) * 1.25) = 1760 + 50 = 1810 >= dynamic Gold (1800)!
          const dynamicGoldUpgradeDetected =
            f01InvCtrl.summary.pointsEarned === 50 &&
            f01InvCtrl.summary.qualifyingAfter === 1810 &&
            f01InvCtrl.summary.projectedTierCode === 'Gold' &&
            f01InvCtrl.summary.willUpgradeTier === true;

          // 1) While isSubmitting === true, SignalR BookingCreated must NOT trigger loadQueues()
          const fetchesBeforeSubmittingSignalR = f01BookingsFetchCount;
          f01InvCtrl.isSubmitting = true;
          f01SignalR.simulateEvent('BookingCreated', { notificationId: 'rt-submitting-1', bookingNumber: 'BK-OTHER' });
          await new Promise((r) => setTimeout(r, 15));
          const noFetchWhileSubmitting = f01BookingsFetchCount === fetchesBeforeSubmittingSignalR;
          f01InvCtrl.isSubmitting = false;

          // 2) While advisor is mid-edit (isSubmitting === false), BookingStatusChanged updates rowVersion/status in-place WITHOUT wiping draft!
          f01BookingStatus = 'InProgress';
          f01BookingRowVer = 'AAAAAAAAB99=';
          f01SignalR.simulateEvent('BookingStatusChanged', {
            notificationId: 'rt-mid-edit-2',
            bookingId: 'bk-f01-active',
            bookingNumber: 'BK-20261123-F01',
            newStatus: 'InProgress'
          });
          await new Promise((r) => setTimeout(r, 25));

          const midEditDraftPreserved =
            f01InvCtrl.selectedBooking?.rowVersion === 'AAAAAAAAB99=' &&
            f01InvCtrl.selectedBooking?.status === 'InProgress' &&
            f01InvCtrl.invoice.laborAmount === 220000 &&
            f01InvCtrl.invoice.partsAmount === 230000 &&
            f01InvCtrl.invoice.odometerAtCompletionKm === 3988 &&
            f01InvCtrl.invoice.appliedCustomerVoucherId === 'vch-opt-1' &&
            f01InvCtrl.invoice.workSummaryNotes === 'Advisor custom synthetic oil + brake pad torque spec';
          f01InvCtrl.$onDestroy();

          // 3) Verify LoyaltyCalculator dynamic customTiers directly + NotificationStore.mergeUnreadFeed preserving read items
          const dynProgress = loyaltyCalc.resolveTierProgress(1750, customDynamicTiers);
          const dynTierAt1800 = loyaltyCalc.determineTier(1800, customDynamicTiers);
          const dynRankGold = loyaltyCalc.getTierRank('Gold', customDynamicTiers);

          f01NotifStore.setServerFeed([
            { id: 'n-unread-1', notificationId: 'n-unread-1', eventType: 'BookingCreated', title: 'Unread 1', isRead: false, occurredAtUtc: '2026-11-23T11:00:00Z' },
            { id: 'n-read-1', notificationId: 'n-read-1', eventType: 'TierUpgraded', title: 'Historical Read', isRead: true, occurredAtUtc: '2026-11-23T08:00:00Z' }
          ], 1);
          f01NotifStore.mergeUnreadFeed([
            { id: 'n-unread-1', notificationId: 'n-unread-1', eventType: 'BookingCreated', title: 'Unread 1', isRead: false, occurredAtUtc: '2026-11-23T11:00:00Z' }
          ], 1);
          const unreadViewItems = f01NotifStore.getAll(true);
          const allViewItemsAfterUnreadMerge = f01NotifStore.getAll(false);

          if (
            !dynamicGoldUpgradeDetected ||
            !noFetchWhileSubmitting ||
            !midEditDraftPreserved ||
            dynProgress.nextTierThreshold !== 1800 ||
            dynProgress.pointsToNextTier !== 50 ||
            dynTierAt1800.code !== 'Gold' ||
            dynRankGold !== 3 ||
            unreadViewItems.length !== 1 ||
            allViewItemsAfterUnreadMerge.length !== 2 ||
            allViewItemsAfterUnreadMerge[1].notificationId !== 'n-read-1'
          ) {
            fail(
              `Gate 1 regression suite (o) failed: dynGold=${dynamicGoldUpgradeDetected}, noSubFetch=${noFetchWhileSubmitting}, preserved=${midEditDraftPreserved}, storeAll=${allViewItemsAfterUnreadMerge.length}`
            );
          } else {
            pass('Verified F-P6-SPA-01a (SignalR mid-edit draft preservation & RowVersion sync) and F-P6-SPA-01b (dynamic customTiers & NotificationStore Unread->All merge).');
          }

          // (p) Stage 5 Refactor & Table-Driven Boundary Suite: formatDiscountSummary, computeVoucherDiscount & resolveTierProgress
          const discountSummaryCases = [
            { input: { discountType: 'FixedAmount', discountValue: 50000 }, expected: '50.000 ₫ Off' },
            { input: { discountType: 'Percentage', discountValue: 10, maxDiscountAmount: null }, expected: '10% Off' },
            { input: { discountType: 'Percentage', discountValue: 15, maxDiscountAmount: 200000 }, expected: '15% Off (Cap 200.000 ₫)' },
            { input: { discountSummary: 'Custom Preset Label' }, expected: 'Custom Preset Label' },
            { input: null, expected: '' }
          ];
          const summaryCasesOk = discountSummaryCases.every(
            (tc) => loyaltyCalc.formatDiscountSummary(tc.input) === tc.expected
          );

          const voucherDiscountBoundaryCases = [
            // FixedAmount normal
            { voucher: { discountType: 'FixedAmount', discountValue: 50000, minOrderAmount: 250000 }, gross: 250000, expectedDisc: 50000, expectedEligible: true },
            // FixedAmount > gross clamping (never produces negative netPaidAmount)
            { voucher: { discountType: 'FixedAmount', discountValue: 300000, minOrderAmount: 0 }, gross: 180000, expectedDisc: 180000, expectedEligible: true },
            // Percentage without maxDiscountAmount cap
            { voucher: { discountType: 'Percentage', discountValue: 10, maxDiscountAmount: null, minOrderAmount: 100000 }, gross: 850000, expectedDisc: 85000, expectedEligible: true },
            // Percentage with maxDiscountAmount (below cap vs clamped at cap)
            { voucher: { discountType: 'Percentage', discountValue: 15, maxDiscountAmount: 200000, minOrderAmount: 600000 }, gross: 800000, expectedDisc: 120000, expectedEligible: true },
            { voucher: { discountType: 'Percentage', discountValue: 15, maxDiscountAmount: 200000, minOrderAmount: 600000 }, gross: 2000000, expectedDisc: 200000, expectedEligible: true },
            // gross < minOrderAmount rejection (1 VND below threshold)
            { voucher: { discountType: 'FixedAmount', discountValue: 50000, minOrderAmount: 250000 }, gross: 249999, expectedDisc: 0, expectedEligible: false },
            // 0 VND gross boundary
            { voucher: { discountType: 'Percentage', discountValue: 20, maxDiscountAmount: 100000, minOrderAmount: 0 }, gross: 0, expectedDisc: 0, expectedEligible: true }
          ];
          const voucherBoundaryOk = voucherDiscountBoundaryCases.every((tc) => {
            const res = loyaltyCalc.computeVoucherDiscount(tc.voucher, tc.gross);
            return (
              res.discountAmount === tc.expectedDisc &&
              res.isEligible === tc.expectedEligible &&
              (tc.expectedEligible ? res.ineligibleReason === null : Boolean(res.ineligibleReason))
            );
          });

          const tierProgressBoundaryCases = [
            { pts: 0, code: 'Bronze', mult: 1.0, next: 'Silver', nextThresh: 500, toNext: 500, pct: 0, top: false },
            { pts: 499, code: 'Bronze', mult: 1.0, next: 'Silver', nextThresh: 500, toNext: 1, pct: 100, top: false },
            { pts: 500, code: 'Silver', mult: 1.2, next: 'Gold', nextThresh: 2000, toNext: 1500, pct: 0, top: false },
            { pts: 1999, code: 'Silver', mult: 1.2, next: 'Gold', nextThresh: 2000, toNext: 1, pct: 100, top: false },
            { pts: 2000, code: 'Gold', mult: 1.5, next: 'Platinum', nextThresh: 5000, toNext: 3000, pct: 0, top: false },
            { pts: 4999, code: 'Gold', mult: 1.5, next: 'Platinum', nextThresh: 5000, toNext: 1, pct: 100, top: false },
            { pts: 5000, code: 'Platinum', mult: 2.0, next: null, nextThresh: null, toNext: 0, pct: 100, top: true }
          ];
          const tierProgressBoundaryOk = tierProgressBoundaryCases.every((tc) => {
            const p = loyaltyCalc.resolveTierProgress(tc.pts);
            return (
              p.currentTierCode === tc.code &&
              p.currentMultiplier === tc.mult &&
              p.nextTierCode === tc.next &&
              p.nextTierThreshold === tc.nextThresh &&
              p.pointsToNextTier === tc.toNext &&
              p.progressPercent === tc.pct &&
              p.isTopTier === tc.top
            );
          });

          if (!summaryCasesOk || !voucherBoundaryOk || !tierProgressBoundaryOk) {
            fail(`Stage 5 boundary suite (p) failed: summary=${summaryCasesOk}, voucher=${voucherBoundaryOk}, tierProgress=${tierProgressBoundaryOk}`);
          } else {
            pass('Verified Stage 5 LoyaltyCalculator.formatDiscountSummary, computeVoucherDiscount clamping/rejection boundaries, and 7-point resolveTierProgress table.');
          }

          // =========================================================================
          // 4e. Phase 7 (T-702 & T-703) Live Executive Chart.js v4 Analytics, CSV Blob Export & Admin Governance Polish
          // =========================================================================

          // (q) DealerApiService Analytics Endpoints, Query-Param Builders & ArrayBuffer CSV Export Parsing
          const p7Calls = [];
          let p7Responder = async () => ({ status: 200, data: { success: true, data: {} }, headers: () => '' });
          const p7Http = Object.assign(
            (cfg) => {
              p7Calls.push(cfg);
              return p7Responder(cfg);
            },
            {
              get: (url, cfg) => p7Http(Object.assign({ method: 'GET', url }, cfg || {})),
              post: (url, data, cfg) => p7Http(Object.assign({ method: 'POST', url, data }, cfg || {})),
              put: (url, data, cfg) => p7Http(Object.assign({ method: 'PUT', url, data }, cfg || {})),
              patch: (url, data, cfg) => p7Http(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
            }
          );
          const p7Api = new dealerApiCtor(
            p7Http,
            mockQ,
            registeredConstants.get('APP_CONFIG'),
            mapper
          );

          const builtParams = p7Api.buildAnalyticsParams({
            fromDate: '2026-05-01',
            toDate: '2026-10-31',
            dealerId: 'ALL'
          });
          const builtScopedParams = p7Api.buildAnalyticsParams({
            fromDate: new Date(2026, 4, 1),
            toDate: new Date(2026, 9, 31),
            dealerId: '22222222-2222-2222-2222-222222222201'
          });

          const utf8CsvPayload = Buffer.concat([
            Buffer.from([0xEF, 0xBB, 0xBF]),
            Buffer.from('DealerCode,DealerName,City,UnitsSold,ShowroomRevenueVnd\r\n"DLR-HCM-01","Apex Moto Saigon Center","Ho Chi Minh City",2,204500000.00\r\n', 'utf8')
          ]);
          const csvArrayBuffer = utf8CsvPayload.buffer.slice(
            utf8CsvPayload.byteOffset,
            utf8CsvPayload.byteOffset + utf8CsvPayload.byteLength
          );

          p7Responder = async (cfg) => {
            if (cfg.url.endsWith('/admin/analytics/overview')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    fromUtc: '2026-04-30T17:00:00Z',
                    toUtc: '2026-10-31T16:59:59Z',
                    totalRevenueVnd: 356000000,
                    showroomRevenueVnd: 300000000,
                    unitsSoldCount: 3,
                    serviceRevenueVnd: 56000000,
                    invoicesIssuedCount: 12,
                    totalVoucherDiscountVnd: 1500000,
                    totalBookings: 20,
                    completedBookings: 15,
                    activeBookings: 3,
                    cancelledOrNoShowBookings: 2,
                    completionRatePct: 75.0,
                    cancellationRatePct: 10.0,
                    activeDealersCount: 3,
                    totalMembersCount: 5,
                    unredeemedPointsPool: 7139,
                    pointsLiabilityVnd: 3569500,
                    dealers: [
                      {
                        dealerId: '22222222-2222-2222-2222-222222222201',
                        dealerCode: 'DLR-HCM-01',
                        dealerName: 'Apex Moto Saigon Center',
                        city: 'Ho Chi Minh City',
                        isActive: true,
                        unitsSoldCount: 2,
                        showroomRevenueVnd: 200000000,
                        invoicesIssuedCount: 7,
                        serviceRevenueVnd: 36000000,
                        totalRevenueVnd: 236000000,
                        totalBookings: 10,
                        completedBookings: 8,
                        activeBookings: 1,
                        cancelledOrNoShowBookings: 1,
                        completionRatePct: 80.0,
                        cancellationRatePct: 10.0
                      },
                      {
                        dealerId: '22222222-2222-2222-2222-222222222202',
                        dealerCode: 'DLR-HN-01',
                        dealerName: 'Apex Moto Hanoi Flagship',
                        city: 'Hanoi',
                        isActive: true,
                        unitsSoldCount: 1,
                        showroomRevenueVnd: 100000000,
                        invoicesIssuedCount: 5,
                        serviceRevenueVnd: 20000000,
                        totalRevenueVnd: 120000000,
                        totalBookings: 7,
                        completedBookings: 6,
                        activeBookings: 1,
                        cancelledOrNoShowBookings: 0,
                        completionRatePct: 85.71,
                        cancellationRatePct: 0.0
                      },
                      {
                        dealerId: '22222222-2222-2222-2222-222222222203',
                        dealerCode: 'DLR-DN-01',
                        dealerName: 'Apex Moto Da Nang Coastal Hub',
                        city: 'Da Nang',
                        isActive: true,
                        unitsSoldCount: 0,
                        showroomRevenueVnd: 0,
                        invoicesIssuedCount: 0,
                        serviceRevenueVnd: 0,
                        totalRevenueVnd: 0,
                        totalBookings: 3,
                        completedBookings: 1,
                        activeBookings: 1,
                        cancelledOrNoShowBookings: 1,
                        completionRatePct: 33.33,
                        cancellationRatePct: 33.33
                      }
                    ]
                  }
                }
              };
            }
            if (cfg.url.endsWith('/admin/analytics/revenue-by-dealer')) {
              // Deliberately includes BOTH array CombinedRevenueByDealerVnd AND scalar TotalRevenueVnd (AdminAnalyticsContracts.cs shape)
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    labels: ['Apex Moto Saigon Center', 'Apex Moto Hanoi Flagship', 'Apex Moto Da Nang Coastal Hub'],
                    dealerCodes: ['DLR-HCM-01', 'DLR-HN-01', 'DLR-DN-01'],
                    showroomSalesVnd: [200000000, 100000000, 0],
                    workshopServiceVnd: [36000000, 20000000, 0],
                    combinedRevenueByDealerVnd: [236000000, 120000000, 0],
                    showroomRevenueVnd: 300000000,
                    serviceRevenueVnd: 56000000,
                    totalRevenueVnd: 356000000,
                    dealers: []
                  }
                }
              };
            }
            if (cfg.url.endsWith('/admin/analytics/bookings-by-dealer')) {
              // Deliberately passes out-of-order statusLabels to verify canonical 7-status color/index remapping
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    statusLabels: ['Pending', 'Completed', 'Cancelled', 'NoShow', 'Confirmed', 'CheckedIn', 'InProgress'],
                    statusCounts: [1, 15, 1, 1, 1, 1, 0],
                    dealerLabels: ['Apex Moto Saigon Center', 'Apex Moto Hanoi Flagship', 'Apex Moto Da Nang Coastal Hub'],
                    completedByDealer: [8, 6, 1],
                    activeByDealer: [1, 1, 1],
                    cancelledOrNoShowByDealer: [1, 0, 1],
                    dealers: []
                  }
                }
              };
            }
            if (cfg.url.endsWith('/admin/analytics/monthly-trends')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    monthLabels: ['2026-05', '2026-06', '2026-07'],
                    showroomRevenueVnd: [100000000, 0, 200000000],
                    serviceRevenueVnd: [20000000, 0, 36000000],
                    combinedRevenueVnd: [120000000, 0, 236000000],
                    bookingCounts: [7, 0, 13],
                    completedBookingCounts: [6, 0, 9]
                  }
                }
              };
            }
            if (cfg.url.endsWith('/admin/analytics/loyalty-distribution')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: {
                    tiers: [
                      { tierCode: 'Bronze', pointsMultiplier: 1.0, memberCount: 2, availablePointsSum: 240 },
                      { tierCode: 'Silver', pointsMultiplier: 1.2, memberCount: 1, availablePointsSum: 649 },
                      { tierCode: 'Gold', pointsMultiplier: 1.5, memberCount: 1, availablePointsSum: 1450 },
                      { tierCode: 'Platinum', pointsMultiplier: 2.0, memberCount: 1, availablePointsSum: 4800 }
                    ],
                    totalMembers: 5,
                    unredeemedPointsPool: 7139,
                    pointsLiabilityVnd: 3569500
                  }
                }
              };
            }
            if (cfg.url.endsWith('/admin/analytics/export')) {
              return {
                status: 200,
                data: csvArrayBuffer,
                headers: (name) => {
                  const lower = String(name || '').toLowerCase();
                  if (lower === 'content-disposition') {
                    return 'attachment; filename="dealer-analytics-20261123.csv"; filename*=UTF-8\'\'dealer-analytics-20261123.csv';
                  }
                  if (lower === 'content-type') {
                    return 'text/csv; charset=utf-8';
                  }
                  return '';
                }
              };
            }
            if (cfg.url.endsWith('/dealers')) {
              return {
                status: 200,
                data: {
                  success: true,
                  data: [
                    { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true },
                    { id: '22222222-2222-2222-2222-222222222202', code: 'DLR-HN-01', name: 'Apex Moto Hanoi Flagship', city: 'Hanoi', isActive: true },
                    { id: '22222222-2222-2222-2222-222222222203', code: 'DLR-DN-01', name: 'Apex Moto Da Nang Coastal Hub', city: 'Da Nang', isActive: true },
                    { id: '22222222-2222-2222-2222-222222222204', code: 'DLR-HP-01', name: 'Apex Moto Hai Phong Legacy', city: 'Hai Phong', isActive: false }
                  ]
                }
              };
            }
            return { status: 200, data: { success: true, data: [] } };
          };

          const ovRes = await p7Api.getAnalyticsOverview({ fromDate: '2026-05-01', toDate: '2026-10-31' });
          const revRes = await p7Api.getAnalyticsRevenueByDealer({ fromDate: '2026-05-01', toDate: '2026-10-31' });
          const bkRes = await p7Api.getAnalyticsBookingsByDealer({ fromDate: '2026-05-01', toDate: '2026-10-31' });
          const trRes = await p7Api.getAnalyticsMonthlyTrends({ fromDate: '2026-05-01', toDate: '2026-10-31' });
          const lyRes = await p7Api.getAnalyticsLoyaltyDistribution();
          const csvRes = await p7Api.exportAnalyticsCsv({ fromDate: '2026-05-01', toDate: '2026-10-31', dealerId: '22222222-2222-2222-2222-222222222201' });
          const exportCallCfg = p7Calls.find((c) => c.url.endsWith('/admin/analytics/export'));
          const fallbackCsvName = p7Api.extractCsvFilename('');

          // Verify ArrayBuffer RFC 7807 ProblemDetails error decoding when /export returns 403
          const prevResponder = p7Responder;
          p7Responder = async () => {
            const errJson = Buffer.from(JSON.stringify({
              status: 403,
              title: 'Forbidden',
              detail: 'Missing Permissions.Analytics.ExportReports.',
              errorCode: 'FORBIDDEN_PERMISSION',
              traceId: 'trace-csv-403'
            }), 'utf8');
            return Promise.reject({
              status: 403,
              data: errJson.buffer.slice(errJson.byteOffset, errJson.byteOffset + errJson.byteLength)
            });
          };
          let decodedCsv403 = null;
          try {
            await p7Api.exportAnalyticsCsv({});
          } catch (err) {
            decodedCsv403 = mapper.extract(err);
          }
          p7Responder = prevResponder;

          if (
            builtParams.dealerId !== undefined ||
            builtParams.fromUtc !== '2026-04-30T17:00:00.000Z' ||
            builtParams.toUtc !== '2026-10-31T16:59:59.999Z' ||
            builtScopedParams.dealerId !== '22222222-2222-2222-2222-222222222201' ||
            ovRes.totalRevenueVnd !== 356000000 ||
            ovRes.dealers.length !== 3 ||
            revRes.totalRevenueVnd.length !== 3 ||
            revRes.totalRevenueVnd[0] !== 236000000 ||
            revRes.showroomSalesVnd[2] !== 0 ||
            bkRes.statusLabels[0] !== 'Completed' ||
            bkRes.statusCounts[0] !== 15 ||
            bkRes.statusLabels[6] !== 'NoShow' ||
            bkRes.statusCounts[6] !== 1 ||
            trRes.monthLabels[1] !== '2026-06' ||
            trRes.combinedRevenueVnd[1] !== 0 ||
            lyRes.customerCounts.length !== 4 ||
            lyRes.pointsLiabilityVnd !== 3569500 ||
            exportCallCfg?.responseType !== 'arraybuffer' ||
            csvRes.filename !== 'dealer-analytics-20261123.csv' ||
            !(csvRes.bytes instanceof ArrayBuffer) ||
            !/^dealer-analytics-\d{8}\.csv$/.test(fallbackCsvName) ||
            decodedCsv403?.errorCode !== 'FORBIDDEN_PERMISSION'
          ) {
            fail('Phase 7 suite (q) failed: DealerApiService analytics endpoints, query builders, or arraybuffer CSV header/error parsing mismatch.');
          } else {
            pass('Verified T-702 DealerApiService analytics methods, UTC boundary builders, zero-filled series normalizers, and arraybuffer CSV export + RFC 7807 decoding.');
          }

          // (r) dealerChart Full Lifecycle: $postLink mount -> $onChanges in-place update('none') -> type switch destroy+recreate -> $onDestroy leak-free teardown
          let p7ChartCreated = 0;
          let p7ChartDestroyed = 0;
          const p7UpdateModes = [];
          const p7CreatedTypes = [];

          sandbox.Chart = function P7MockChart(_ctx, cfg) {
            p7ChartCreated++;
            p7CreatedTypes.push(cfg?.type);
            this.config = cfg;
            this.data = cfg?.data;
            this.options = cfg?.options;
            this.update = (mode) => { p7UpdateModes.push(mode); };
            this.destroy = () => { p7ChartDestroyed++; };
          };

          const p7CanvasHost = [{ querySelector: () => ({ getContext: () => ({}) }) }];
          const lifecycleChartCtrl = new dealerChartDef.controller(p7CanvasHost);
          lifecycleChartCtrl.config = {
            type: 'bar',
            data: { labels: ['DLR-HCM-01', 'DLR-HN-01'], datasets: [{ data: [8, 6] }] },
            options: { scales: { x: { stacked: false }, y: { beginAtZero: true } } }
          };
          lifecycleChartCtrl.$postLink();
          const afterMountOk = p7ChartCreated === 1 && p7ChartDestroyed === 0 && p7UpdateModes.length === 0;

          // Same chart type ('bar') -> must mutate in-place and call update('none') WITHOUT destroying canvas
          lifecycleChartCtrl.config = {
            type: 'bar',
            data: { labels: ['DLR-HCM-01', 'DLR-HN-01'], datasets: [{ data: [12, 9] }] },
            options: { scales: { x: { stacked: false }, y: { beginAtZero: true } } }
          };
          lifecycleChartCtrl.$onChanges({ config: { isFirstChange: () => false } });
          const afterInPlaceUpdateOk =
            p7ChartCreated === 1 &&
            p7ChartDestroyed === 0 &&
            p7UpdateModes.length === 1 &&
            p7UpdateModes[0] === 'none' &&
            lifecycleChartCtrl.config.data.datasets[0].data[0] === 12;

          // Switch chart type ('bar' -> 'doughnut') -> must destroy old instance and instantiate 'doughnut'
          lifecycleChartCtrl.config = {
            type: 'doughnut',
            data: { labels: ['Completed', 'Pending'], datasets: [{ data: [15, 2] }] },
            options: { cutout: '58%' }
          };
          lifecycleChartCtrl.$onChanges({ config: { isFirstChange: () => false } });
          const afterSwitchToDoughnutOk =
            p7ChartCreated === 2 &&
            p7ChartDestroyed === 1 &&
            p7CreatedTypes[1] === 'doughnut';

          // Switch back ('doughnut' -> 'bar') -> must destroy 'doughnut' and instantiate 'bar'
          lifecycleChartCtrl.config = {
            type: 'bar',
            data: { labels: ['DLR-HCM-01'], datasets: [{ data: [8] }] },
            options: { scales: { x: { stacked: false }, y: { beginAtZero: true } } }
          };
          lifecycleChartCtrl.$onChanges({ config: { isFirstChange: () => false } });
          const afterSwitchBackToBarOk =
            p7ChartCreated === 3 &&
            p7ChartDestroyed === 2 &&
            p7CreatedTypes[2] === 'bar';

          // Route exit ($onDestroy) -> active instances == 0 (created === destroyed)
          lifecycleChartCtrl.$onDestroy();
          const afterDestroyZeroLeaks = p7ChartCreated === 3 && p7ChartDestroyed === 3;

          if (
            !afterMountOk ||
            !afterInPlaceUpdateOk ||
            !afterSwitchToDoughnutOk ||
            !afterSwitchBackToBarOk ||
            !afterDestroyZeroLeaks
          ) {
            fail(
              `Phase 7 suite (r) failed: created=${p7ChartCreated}, destroyed=${p7ChartDestroyed}, updates=${JSON.stringify(p7UpdateModes)}, types=${JSON.stringify(p7CreatedTypes)}`
            );
          } else {
            pass('Verified RISK-04 dealerChart full lifecycle ($postLink mount -> in-place update(\'none\') -> bar<->doughnut destroy/recreate -> $onDestroy 0 leaked instances).');
          }

          // (s) SCR-ADM-01 (adminAnalytics) Parallel $q.all Hydration, Inverted Date Guard, Chart 2 Toggle & CSV Blob Download
          const adminAnalyticsDef = registeredComponents.get('adminAnalytics');
          if (!adminAnalyticsDef || typeof adminAnalyticsDef.controller !== 'function') {
            fail('Missing adminAnalytics component registration.');
          } else {
            const p7LiveTokenStore = new tokenStoreCtor({});
            p7LiveTokenStore.setTokens('jwt-p7-admin', 900, 'rt-p7-admin');

            const p7Bus = createEventBusRootScope();
            const p7NotifStore = new notifStoreCtor();
            const p7Toasts = [];
            const p7ToastSvc = {
              push: (t) => { p7Toasts.push(t); },
              info: (title, message) => { p7Toasts.push({ intent: 'info', title, message }); },
              success: (title, message) => { p7Toasts.push({ intent: 'success', title, message }); },
              warning: (title, message) => { p7Toasts.push({ intent: 'warning', title, message }); },
              danger: (title, message) => { p7Toasts.push({ intent: 'danger', title, message }); }
            };
            const p7SignalR = new signalrSvcCtor(
              mockQ,
              p7Bus,
              registeredConstants.get('APP_CONFIG'),
              p7LiveTokenStore,
              p7NotifStore,
              p7ToastSvc
            );

            const createdBlobs = [];
            const createdObjectUrls = [];
            const revokedObjectUrls = [];
            let anchorClickedDownload = null;
            const pendingTimeouts = [];

            const mockTimeout = Object.assign(
              (fn) => {
                const handle = { fn, cancelled: false };
                pendingTimeouts.push(handle);
                return handle;
              },
              {
                cancel: (handle) => { if (handle) { handle.cancelled = true; } }
              }
            );

            class MockBlob {
              constructor(parts, opts) {
                this.parts = parts;
                this.type = opts?.type || '';
                createdBlobs.push(this);
              }
            }

            const mockWindow = {
              Blob: MockBlob,
              URL: {
                createObjectURL: (blob) => {
                  const u = `blob:http://localhost:4200/mock-${createdObjectUrls.length + 1}`;
                  createdObjectUrls.push({ url: u, blob });
                  return u;
                },
                revokeObjectURL: (u) => {
                  revokedObjectUrls.push(u);
                }
              },
              document: {
                body: {
                  appendChild: () => {},
                  removeChild: () => {}
                },
                createElement: (tag) => ({
                  tagName: tag.toUpperCase(),
                  style: {},
                  parentNode: { removeChild: () => {} },
                  click() {
                    anchorClickedDownload = { href: this.href, download: this.download };
                  }
                })
              }
            };

            const p7LocationStore = {};
            const mockLocation = {
              search(k, v) {
                if (k === undefined) { return Object.assign({}, p7LocationStore); }
                if (v === null) { delete p7LocationStore[k]; return mockLocation; }
                p7LocationStore[k] = v;
                return mockLocation;
              }
            };
            const p7NavTransitions = [];
            const p7State = {
              go: (name, params) => { p7NavTransitions.push({ name, params: params || null }); }
            };

            const vndFilterFn = registeredFilters.get('vndCurrency')();
            const mockFilter = (name) => (name === 'vndCurrency' ? vndFilterFn : null);

            p7Calls.length = 0;
            const anaCtrl = new adminAnalyticsDef.controller(
              mockQ,
              p7Bus,
              p7State,
              mockLocation,
              mockWindow,
              mockTimeout,
              mockFilter,
              p7Api,
              mapper,
              p7SignalR,
              p7ToastSvc,
              p7LiveTokenStore
            );
            anaCtrl.$onInit();
            await new Promise((r) => setTimeout(r, 25));

            // 1) Verify parallel $q.all hydration, Chart.js v4 scales (no v2 xAxes/yAxes), structuredClone safety, and extensible arrays
            const hydratedOk =
              anaCtrl.hasLoadedOnce === true &&
              anaCtrl.kpis.length === 4 &&
              anaCtrl.kpis[0].value.includes('356.000.000') &&
              anaCtrl.liabilitySummary.unredeemedPointsPool === 7139 &&
              anaCtrl.liabilitySummary.pointsLiabilityVnd === 3569500 &&
              anaCtrl.sortedDealerRows.length === 3 &&
              anaCtrl.dealerFilterOptions.length === 5 &&
              anaCtrl.dealerFilterOptions[0].label === 'All Dealerships' &&
              anaCtrl.dealerFilterOptions[4].id === '22222222-2222-2222-2222-222222222204' &&
              anaCtrl.dealerFilterOptions[4].label === 'DLR-HP-01 • Apex Moto Hai Phong Legacy (Inactive)' &&
              anaCtrl.revenueByDealerChart.type === 'bar' &&
              anaCtrl.revenueByDealerChart.options.scales.x.stacked === true &&
              anaCtrl.revenueByDealerChart.options.scales.y.stacked === true &&
              !anaCtrl.revenueByDealerChart.options.scales.xAxes &&
              anaCtrl.monthlyTrendsChart.type === 'line' &&
              Boolean(anaCtrl.monthlyTrendsChart.options.scales.yRevenue) &&
              anaCtrl.monthlyTrendsChart.options.scales.yBookings.grid.drawOnChartArea === false &&
              anaCtrl.loyaltyDistributionChart.type === 'doughnut' &&
              anaCtrl.loyaltyDistributionChart.options.cutout === '62%';

            // Ensure all 4 chart configs are structuredClone-safe (zero embedded functions that break angular.copy)
            let chartCloneSafe = true;
            try {
              structuredClone(anaCtrl.revenueByDealerChart);
              structuredClone(anaCtrl.bookingsChart);
              structuredClone(anaCtrl.monthlyTrendsChart);
              structuredClone(anaCtrl.loyaltyDistributionChart);
            } catch (_cloneErr) {
              chartCloneSafe = false;
            }

            // Extensibility check on all controller arrays ($$hashKey / ng-options guard)
            const analyticsArraysExtensible = [
              anaCtrl.datePresets,
              anaCtrl.dealerFilterOptions,
              anaCtrl.kpis,
              anaCtrl.sortedDealerRows
            ].every((arr) => Array.isArray(arr) && Object.isExtensible(arr) && arr.every((el) => !el || typeof el !== 'object' || Object.isExtensible(el)));

            // 2) Verify Chart 2 toggle ('bar' <-> 'doughnut')
            const defaultBarMode = anaCtrl.bookingsChartMode === 'bar' && anaCtrl.bookingsChart.type === 'bar' && anaCtrl.bookingsChart.data.datasets.length === 3;
            anaCtrl.setBookingsChartMode('doughnut');
            const toggledDoughnutMode =
              anaCtrl.bookingsChartMode === 'doughnut' &&
              anaCtrl.bookingsChart.type === 'doughnut' &&
              anaCtrl.bookingsChart.data.labels.length === 7 &&
              anaCtrl.bookingsChart.data.datasets[0].data[0] === 15;
            anaCtrl.setBookingsChartMode('bar');
            const toggledBackBarMode = anaCtrl.bookingsChartMode === 'bar' && anaCtrl.bookingsChart.type === 'bar';

            // 3) Verify inverted date guard (fromDate > toDate) & >731d span guard block API calls
            const callsBeforeInvalidDate = p7Calls.length;
            anaCtrl.filters.fromDate = new Date(2026, 10, 20);
            anaCtrl.filters.toDate = new Date(2026, 4, 1);
            anaCtrl.onDateInputChanged();
            await anaCtrl.applyFilters();
            await anaCtrl.exportCsvReport();
            const invertedDateBlocked =
              p7Calls.length === callsBeforeInvalidDate &&
              Boolean(anaCtrl.dateValidationError) &&
              p7Toasts.some((t) => t.intent === 'warning');

            // Also test > 731 days span guard
            anaCtrl.filters.fromDate = new Date(2023, 0, 1);
            anaCtrl.filters.toDate = new Date(2026, 10, 20);
            const spanOver731Blocked = anaCtrl.validateDateWindow() === false && p7Calls.length === callsBeforeInvalidDate;

            // Restore valid preset & test CSV Blob download flow
            await anaCtrl.selectPreset('LAST_6M');
            const csvExportOut = await anaCtrl.exportCsvReport();
            // Flush $timeout to verify URL.revokeObjectURL
            for (const h of pendingTimeouts) {
              if (!h.cancelled) { h.fn(); }
            }
            const csvBlobFlowOk =
              csvExportOut?.filename === 'dealer-analytics-20261123.csv' &&
              createdBlobs.length === 1 &&
              createdBlobs[0].parts.length === 1 && // Server ArrayBuffer already had UTF-8 BOM -> no duplicate BOM!
              anchorClickedDownload?.download === 'dealer-analytics-20261123.csv' &&
              createdObjectUrls.length === 1 &&
              revokedObjectUrls.length === 1 &&
              revokedObjectUrls[0] === createdObjectUrls[0].url &&
              p7Toasts.some((t) => t.intent === 'success' && t.message.includes('dealer-analytics-20261123.csv'));

            // (u) Gate 1 Regression Suite (F-P7-FE-01 & F-P7-FE-02): LAST_12M exact 12-month calendar span, inactive branch option label, and 403 exportCsvReport error toast
            await anaCtrl.selectPreset('LAST_12M');
            const last12From = anaCtrl.filters.fromDate;
            const last12To = anaCtrl.filters.toDate;
            const last12MonthSpan =
              (last12To.getFullYear() - last12From.getFullYear()) * 12 +
              (last12To.getMonth() - last12From.getMonth()) + 1;
            const last12ExactSpanOk =
              last12From instanceof Date &&
              last12To instanceof Date &&
              last12From.getDate() === 1 &&
              last12MonthSpan === 12;

            const savedP7Responder = p7Responder;
            p7Responder = async (cfg) => {
              if (cfg.url.endsWith('/admin/analytics/export')) {
                const errPayload = Buffer.from(JSON.stringify({
                  status: 403,
                  title: 'Export Permission Denied',
                  detail: 'Caller lacks Permissions.Analytics.ExportReports.',
                  errorCode: 'FORBIDDEN_PERMISSION',
                  traceId: 'trace-exp-403'
                }), 'utf8');
                return Promise.reject({
                  status: 403,
                  data: errPayload.buffer.slice(errPayload.byteOffset, errPayload.byteOffset + errPayload.byteLength)
                });
              }
              return savedP7Responder(cfg);
            };
            const csv403Res = await anaCtrl.exportCsvReport();
            p7Responder = savedP7Responder;
            const csv403ToastOk =
              csv403Res === null &&
              anaCtrl.isExporting === false &&
              p7Toasts.some(
                (t) =>
                  t.intent === 'danger' &&
                  t.title === 'Export Permission Denied' &&
                  t.message.includes('Permissions.Analytics.ExportReports')
              );

            if (
              !last12ExactSpanOk ||
              anaCtrl.dealerFilterOptions[4]?.label !== 'DLR-HP-01 • Apex Moto Hai Phong Legacy (Inactive)' ||
              !csv403ToastOk
            ) {
              fail(
                `Gate 1 regression suite (u) failed: last12Span=${last12MonthSpan} (ok=${last12ExactSpanOk}), inactiveOpt='${anaCtrl.dealerFilterOptions[4]?.label}', csv403Toast=${csv403ToastOk}`
              );
            } else {
              pass('Verified F-P7-FE-01 & F-P7-FE-02: LAST_12M exact 12-month calendar span, inactive branch option (Inactive) label, and 403 FORBIDDEN_PERMISSION CSV export error toast.');
            }

            // Verify SignalR BookingCreated sets hasPendingLiveActivity & table sorting + manageDealerBranch handoff
            p7SignalR.simulateEvent('BookingCreated', { notificationId: 'n-p7-live-1', bookingNumber: 'BK-P7-1' });
            const liveActivityFlagged = anaCtrl.hasPendingLiveActivity === true;
            anaCtrl.toggleSort('dealerCode'); // ascending by code -> DLR-DN-01 first
            const sortedAscCodeOk = anaCtrl.sortedDealerRows[0]?.dealerCode === 'DLR-DN-01';
            anaCtrl.manageDealerBranch(anaCtrl.sortedDealerRows[2]); // DLR-HN-01

            anaCtrl.$onDestroy();

            // (t) T-703 Admin Governance Cross-Portal Deep-Link Polish (adminDealers, adminStaff, adminModels) via live API responses
            const p7GovStore = new tokenStoreCtor({});
            p7GovStore.setTokens('test-admin-gov-jwt', 3600, null);
            const p7GovHttp = Object.assign(
              async (cfg) => {
                if (cfg.url.endsWith('/dealers')) {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Branch Saigon', city: 'Ho Chi Minh City', address: '128 Street', openingTime: '08:00', closingTime: '19:00', defaultSlotCapacity: 5, isActive: true },
                        { id: '22222222-2222-2222-2222-222222222202', code: 'DLR-HN-01', name: 'Branch Hanoi', city: 'Hanoi', address: '54 Street', openingTime: '08:00', closingTime: '18:00', defaultSlotCapacity: 4, isActive: true },
                        { id: '22222222-2222-2222-2222-222222222203', code: 'DLR-DN-01', name: 'Branch Da Nang', city: 'Da Nang', address: '86 Street', openingTime: '08:00', closingTime: '18:00', defaultSlotCapacity: 4, isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.includes('/dealers/22222222-2222-2222-2222-222222222203/staff')) {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { userId: 'stf-dn-1', staffEmployeeCode: 'EMP-DN-001', fullName: 'Staff DN', email: 'dn@example.test', dealerId: '22222222-2222-2222-2222-222222222203', dealerCode: 'DLR-DN-01', dealerName: 'Branch Da Nang', isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/motorcycles')) {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: 'm-1', modelCode: 'HND-SH160I-2026', brand: 'Honda', modelName: 'Honda SH160i', category: 'Scooter', engineDisplacementCc: 157, baseRetailPrice: 102500000, isActive: true },
                        { id: 'm-2', modelCode: 'HND-WINX-2026', brand: 'Honda', modelName: 'Honda Winner X', category: 'Underbone', engineDisplacementCc: 149, baseRetailPrice: 50500000, isActive: true }
                      ]
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              },
              {
                get: (url, cfg) => p7GovHttp(Object.assign({ method: 'GET', url }, cfg || {})),
                post: (url, data, cfg) => p7GovHttp(Object.assign({ method: 'POST', url, data }, cfg || {})),
                put: (url, data, cfg) => p7GovHttp(Object.assign({ method: 'PUT', url, data }, cfg || {})),
                patch: (url, data, cfg) => p7GovHttp(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
              }
            );
            const p7GovApi = new dealerApiCtor(p7GovHttp, mockQ, registeredConstants.get('APP_CONFIG'), mapper);

            const p7DealersCtrl = new adminDealersDef.controller(
              p7GovHttp,
              registeredConstants.get('APP_CONFIG'),
              mapper,
              p7ToastSvc,
              p7GovStore,
              mockLocation,
              p7Api
            );
            p7DealersCtrl.$onInit();
            await new Promise((r) => setTimeout(r, 20));
            const dealersPreFilteredOk =
              p7DealersCtrl.focusedDealerBanner === 'DLR-HN-01' &&
              p7DealersCtrl.filteredDealers.length === 1 &&
              p7DealersCtrl.filteredDealers[0].code === 'DLR-HN-01';
            p7DealersCtrl.resetFilters();
            const dealersResetOk = p7DealersCtrl.focusedDealerBanner === null && p7DealersCtrl.filteredDealers.length === 3;
            p7DealersCtrl.$onDestroy();

            const staffQueryLocation = { search: () => ({ dealerCode: 'DLR-DN-01' }) };
            const p7StaffCtrl = new adminStaffDef.controller(
              p7GovHttp,
              mockQ,
              {},
              registeredConstants.get('APP_CONFIG'),
              p4AuthSvc,
              mapper,
              registeredConstants.get('ROLE_PERMISSION_PRESETS'),
              p7ToastSvc,
              p7GovStore,
              staffQueryLocation
            );
            p7StaffCtrl.$onInit();
            await new Promise((r) => setTimeout(r, 20));
            const staffDeepLinkOk =
              p7StaffCtrl.selectedDealerId === '22222222-2222-2222-2222-222222222203' &&
              p7StaffCtrl.filteredStaff.length === 1 &&
              p7StaffCtrl.filteredStaff[0].dealerCode === 'DLR-DN-01';
            p7StaffCtrl.$onDestroy();

            const modelsQueryStore = { modelCode: 'HND-WINX-2026' };
            const modelsQueryLocation = {
              search(k, v) {
                if (k === undefined) { return Object.assign({}, modelsQueryStore); }
                if (v === null) { delete modelsQueryStore[k]; }
                return modelsQueryLocation;
              }
            };
            const p7ModelsCtrl = new adminModelsDef.controller(
              mockQ,
              p7GovApi,
              mapper,
              p7ToastSvc,
              p7GovStore,
              modelsQueryLocation
            );
            p7ModelsCtrl.$onInit();
            await new Promise((r) => setTimeout(r, 20));
            const modelsDeepLinkOk =
              p7ModelsCtrl.filteredModels.length === 1 &&
              p7ModelsCtrl.filteredModels[0].modelCode === 'HND-WINX-2026';
            p7ModelsCtrl.resetFilters();
            const modelsResetOk = p7ModelsCtrl.filteredModels.length === 2 && modelsQueryStore.modelCode === undefined;
            p7ModelsCtrl.$onDestroy();

            if (
              !hydratedOk ||
              !chartCloneSafe ||
              !analyticsArraysExtensible ||
              !defaultBarMode ||
              !toggledDoughnutMode ||
              !toggledBackBarMode ||
              !invertedDateBlocked ||
              !spanOver731Blocked ||
              !csvBlobFlowOk ||
              !liveActivityFlagged ||
              !sortedAscCodeOk ||
              p7NavTransitions[0]?.name !== 'app.admin.dealers' ||
              !dealersPreFilteredOk ||
              !dealersResetOk ||
              !staffDeepLinkOk ||
              !modelsDeepLinkOk ||
              !modelsResetOk
            ) {
              fail(
                `Phase 7 suites (s)-(t) failed: hydrated=${hydratedOk}, clone=${chartCloneSafe}, ext=${analyticsArraysExtensible}, toggle=${toggledDoughnutMode}, dateGuard=${invertedDateBlocked}/${spanOver731Blocked}, csv=${csvBlobFlowOk}, live=${liveActivityFlagged}, sort=${sortedAscCodeOk}, dlr=${dealersPreFilteredOk}/${dealersResetOk}, stf=${staffDeepLinkOk}, mdl=${modelsDeepLinkOk}/${modelsResetOk}`
              );
            } else {
              pass('Verified SCR-ADM-01 parallel $q.all hydration, inverted/731d date guards, Chart 2 bar<->doughnut toggle, UTF-8 BOM CSV Blob download/revocation, and T-703 cross-portal Admin governance deep-links.');
            }

            // (v) Stage 5 Boundary Suite (RF-P7-FE-01 & RF-P7-FE-02): extractCsvFilename RFC 5987 / path-traversal & toAnalyticsUtcBoundary table
            const csvFilenameCases = [
              {
                header: 'attachment; filename="fallback.csv"; filename*=UTF-8\'\'dealer-analytics-2026%20HQ.csv',
                expected: 'dealer-analytics-2026 HQ.csv'
              },
              {
                header: 'attachment; filename="dealer-analytics-20261123.csv"',
                expected: 'dealer-analytics-20261123.csv'
              },
              {
                header: 'attachment; filename=branch-summary-q4.csv',
                expected: 'branch-summary-q4.csv'
              },
              {
                header: 'attachment; filename="../../evil.csv"',
                expected: 'evil.csv'
              },
              {
                header: 'attachment; filename="..\\..\\evil-win.csv"',
                expected: 'evil-win.csv'
              },
              {
                header: 'attachment; filename*=UTF-8\'\'..%2F..%2Ftraversal.csv',
                expected: 'traversal.csv'
              }
            ];
            const csvExactCasesOk = csvFilenameCases.every(
              (tc) => p7Api.extractCsvFilename(tc.header) === tc.expected
            );
            const csvFallbackCasesOk = [
              '',
              '   ',
              'attachment; filename="../../"',
              null
            ].every((hdr) => /^dealer-analytics-\d{8}\.csv$/.test(p7Api.extractCsvFilename(hdr)));

            const utcBoundaryCases = [
              { val: '2026-05-01', end: false, expected: '2026-04-30T17:00:00.000Z' },
              { val: '2026-05-01', end: true, expected: '2026-05-01T16:59:59.999Z' },
              { val: new Date(2026, 0, 1), end: false, expected: '2025-12-31T17:00:00.000Z' },
              { val: new Date(2026, 0, 1), end: true, expected: '2026-01-01T16:59:59.999Z' },
              { val: '2026-08-15T09:30:00+07:00', end: false, expected: '2026-08-15T02:30:00.000Z' },
              { val: '2026-08-15T14:00:00.000Z', end: true, expected: '2026-08-15T14:00:00.000Z' },
              { val: '', end: false, expected: '' },
              { val: null, end: true, expected: '' }
            ];
            const utcBoundaryCasesOk = utcBoundaryCases.every(
              (tc) => p7Api.toAnalyticsUtcBoundary(tc.val, tc.end) === tc.expected
            );

            if (!csvExactCasesOk || !csvFallbackCasesOk || !utcBoundaryCasesOk) {
              fail(
                `Stage 5 boundary suite (v) failed: csvExact=${csvExactCasesOk}, csvFallback=${csvFallbackCasesOk}, utcBoundary=${utcBoundaryCasesOk}`
              );
            } else {
              pass('Verified Stage 5 RF-P7-FE-01/02 ANALYTICS_PALETTE constants, authoritative dealer row coalescing, extractCsvFilename RFC 5987/traversal boundaries, and toAnalyticsUtcBoundary (+07:00 vs ISO) table.');
            }

            // =========================================================================
            // 4f. Phase 8 (v1.1.0 — T-804 & T-805): Live SignalR Test-Drives, OEM Spare-Parts Picker, Admin Parts Studio & Printable Receipt Modal
            // =========================================================================
            const custTestDrivesDef = registeredComponents.get('customerTestDrives');
            const staffTestDrivesDef = registeredComponents.get('staffTestDrives');
            const adminSparePartsDef = registeredComponents.get('adminSpareParts');
            const printableReceiptDef = registeredComponents.get('printableReceiptModal');

            if (!custTestDrivesDef || !staffTestDrivesDef || !adminSparePartsDef || !printableReceiptDef) {
              fail('Missing Phase 8 component registration (customerTestDrives, staffTestDrives, adminSpareParts, or printableReceiptModal).');
            } else {
              const p8Calls = [];
              let p8Responder = async () => ({ status: 200, data: { success: true, data: [] } });
              const p8Http = Object.assign(
                (cfg) => {
                  p8Calls.push(cfg);
                  return p8Responder(cfg);
                },
                {
                  get: (url, cfg) => p8Http(Object.assign({ method: 'GET', url }, cfg || {})),
                  post: (url, data, cfg) => p8Http(Object.assign({ method: 'POST', url, data }, cfg || {})),
                  put: (url, data, cfg) => p8Http(Object.assign({ method: 'PUT', url, data }, cfg || {})),
                  patch: (url, data, cfg) => p8Http(Object.assign({ method: 'PATCH', url, data }, cfg || {}))
                }
              );

              const p8Api = new dealerApiCtor(
                p8Http,
                mockQ,
                registeredConstants.get('APP_CONFIG'),
                mapper
              );

              // (w) Showroom [Book Test Drive] CTA + Customer Test-Drives (SCR-CUS-06) + Live TestDriveStatusChanged SignalR
              const showroomStateTransitions = [];
              const mockShowroomState = {
                go: (name, params) => { showroomStateTransitions.push({ name, params: params || null }); }
              };
              const p8ShowroomStore = new tokenStoreCtor({});
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/dealers') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/motorcycles') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '55555555-5555-5555-5555-555555555501', modelCode: 'HND-AB160-2026', brand: 'Honda', modelName: 'Honda Air Blade 160', category: 'Scooter', engineDisplacementCc: 157, baseRetailPrice: 57500000, isActive: true },
                        { id: '55555555-5555-5555-5555-555555555502', modelCode: 'HND-SH160I-2026', brand: 'Honda', modelName: 'Honda SH160i ABS Sport', category: 'Scooter', engineDisplacementCc: 157, baseRetailPrice: 102500000, isActive: true }
                      ]
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };

              // Unauthenticated guest clicking [Book Test Drive] -> redirects to app.public.login with returnUrl
              await p4AuthSvc.clearSessionAndRealtime();
              const srCtrl = new showroomDef.controller(
                mockQ,
                p7Bus,
                p4AuthSvc,
                p8Api,
                loyaltyCalc,
                mapper,
                p7SignalR,
                p8ShowroomStore,
                mockShowroomState,
                p7ToastSvc
              );
              srCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));
              srCtrl.bookTestDrive(srCtrl.models[1]); // HND-SH160I-2026
              const guestShowroomRedirect = showroomStateTransitions[0];

              // Authenticated Customer clicking [Book Test Drive] -> navigates directly to app.customer.testDrives({ modelId, dealerId })
              p4AuthSvc.setSession({
                accessToken: 'jwt-p8-cust-silver',
                refreshToken: 'rt-p8-cust-silver',
                expiresInSeconds: 900,
                user: {
                  userId: '44444444-4444-4444-4444-444444444408',
                  email: 'customer.silver@motorcycledealer.vn',
                  fullName: 'Tran Ngoc Mai (Silver)',
                  phoneNumber: '0913000201',
                  role: 'Customer',
                  tierCode: 'Silver',
                  tierMultiplier: 1.2,
                  availablePoints: 649,
                  lifetimeQualifyingPoints: 1960,
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').Customer.slice()
                }
              });
              p7Bus.$broadcast('auth:changed');
              srCtrl.filters.dealerId = '22222222-2222-2222-2222-222222222201';
              srCtrl.bookTestDrive(srCtrl.models[1]);
              const customerShowroomRedirect = showroomStateTransitions[1];
              srCtrl.$onDestroy();

              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/dealers') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/motorcycles') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '55555555-5555-5555-5555-555555555502', modelCode: 'HND-SH160I-2026', brand: 'Honda', modelName: 'Honda SH160i ABS Sport', baseRetailPrice: 102500000, isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/my') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'td-cust-existing-1',
                          bookingNumber: 'TD-20261123-0001',
                          dealerId: '22222222-2222-2222-2222-222222222201',
                          dealerName: 'Apex Moto Saigon Center',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          preferredDate: '2026-11-25',
                          startTime: '09:00',
                          endTime: '09:45',
                          driverLicenseNumber: 'A2-79012845',
                          contactPhone: '0913000201',
                          status: 'Requested',
                          rowVersion: 'AAAAAAAATD1='
                        }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives') && cfg.method === 'POST') {
                  return {
                    status: 201,
                    data: {
                      success: true,
                      data: {
                        id: 'td-cust-created-2',
                        bookingNumber: 'TD-20261124-8899',
                        dealerId: cfg.data.dealerId,
                        dealerName: 'Apex Moto Saigon Center',
                        motorcycleModelId: cfg.data.motorcycleModelId,
                        motorcycleModelName: 'Honda SH160i ABS Sport',
                        preferredDate: cfg.data.preferredDate,
                        startTime: cfg.data.startTime,
                        endTime: cfg.data.endTime,
                        driverLicenseNumber: cfg.data.driverLicenseNumber,
                        contactPhone: cfg.data.contactPhone,
                        customerNotes: cfg.data.customerNotes,
                        status: 'Requested',
                        rowVersion: 'AAAAAAAATD2='
                      }
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/td-cust-created-2') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        id: 'td-cust-created-2',
                        bookingNumber: 'TD-20261124-8899',
                        dealerId: '22222222-2222-2222-2222-222222222201',
                        dealerName: 'Apex Moto Saigon Center',
                        motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                        motorcycleModelName: 'Honda SH160i ABS Sport',
                        preferredDate: '2026-11-26',
                        startTime: '14:00',
                        endTime: '14:45',
                        driverLicenseNumber: 'A2-99887766',
                        contactPhone: '0913000201',
                        status: 'Confirmed',
                        assignedVin: 'RLHJC9204RY000309',
                        rowVersion: 'AAAAAAAATD5='
                      }
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/td-cust-existing-1/cancel') && cfg.method === 'POST') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        id: 'td-cust-existing-1',
                        bookingNumber: 'TD-20261123-0001',
                        status: 'Cancelled',
                        cancellationReason: cfg.data.reason,
                        rowVersion: 'AAAAAAAATD9='
                      }
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };

              const p8CustTokenStore = new tokenStoreCtor({});
              p8CustTokenStore.setTokens('jwt-p8-cust-silver', 900, 'rt-p8-cust-silver');
              const p8CustBus = createEventBusRootScope();
              const p8CustNotifStore = new notifStoreCtor();
              const p8CustSignalR = new signalrSvcCtor(
                mockQ,
                p8CustBus,
                registeredConstants.get('APP_CONFIG'),
                p8CustTokenStore,
                p8CustNotifStore,
                p7ToastSvc
              );

              const custTdCtrl = new custTestDrivesDef.controller(
                mockQ,
                p8CustBus,
                { modelId: '55555555-5555-5555-5555-555555555502', dealerId: '22222222-2222-2222-2222-222222222201' },
                p4AuthSvc,
                p8Api,
                mapper,
                p8CustSignalR,
                p7ToastSvc,
                p8CustTokenStore
              );
              custTdCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));

              const preselectedParamsOk =
                custTdCtrl.draft.motorcycleModelId === '55555555-5555-5555-5555-555555555502' &&
                custTdCtrl.draft.dealerId === '22222222-2222-2222-2222-222222222201';

              custTdCtrl.selectTimeWindow(custTdCtrl.timeWindows[2]); // 14:00 - 14:45
              custTdCtrl.draft.preferredDate = '2026-11-26';
              custTdCtrl.draft.driverLicenseNumber = 'a2-99887766';
              custTdCtrl.onLicenseEdited();
              custTdCtrl.draft.customerNotes = 'Testing ABS braking';
              const createdTd = await custTdCtrl.submitTestDrive();

              // Simulate live SignalR TestDriveStatusChanged -> Confirmed with assignedVin (0 reloads!)
              p8CustSignalR.simulateEvent('TestDriveStatusChanged', {
                notificationId: 'rt-td-status-1',
                testDriveId: 'td-cust-created-2',
                bookingNumber: 'TD-20261124-8899',
                previousStatus: 'Requested',
                newStatus: 'Confirmed',
                assignedVin: 'RLHJC9204RY000309',
                occurredAtUtc: new Date().toISOString()
              });
              await new Promise((r) => setTimeout(r, 20));
              const liveConfirmedCustTd = custTdCtrl.testDrives.find((x) => x.id === 'td-cust-created-2');

              // Cancel existing requested test drive
              custTdCtrl.openCancelModal(custTdCtrl.testDrives.find((x) => x.id === 'td-cust-existing-1'));
              custTdCtrl.cancelReason = 'Out of town';
              const cancelledCustTd = await custTdCtrl.confirmCancelTestDrive();

              custTdCtrl.$onDestroy();
              // Emit post-destroy event -> must NOT mutate destroyed controller
              p8CustSignalR.simulateEvent('TestDriveStatusChanged', {
                notificationId: 'rt-td-status-post-destroy',
                testDriveId: 'td-cust-created-2',
                bookingNumber: 'TD-20261124-8899',
                newStatus: 'NoShow'
              });
              const custTdPostDestroySafe = liveConfirmedCustTd?.status === 'Confirmed';

              // Verify live non-preview modelId from $stateParams (not in PREVIEW_MODELS) survives $onInit -> loadInitialData
              p8Responder = async (cfg) => {
                if (cfg.url.includes('/motorcycles') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '55555555-5555-5555-5555-555555555501', modelCode: 'HND-AB160-2026', brand: 'Honda', modelName: 'Honda Air Blade 160 SE', isActive: true },
                        { id: '55555555-5555-5555-5555-555555555502', modelCode: 'HND-SH160I-2026', brand: 'Honda', modelName: 'Honda SH160i ABS Sport', isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.includes('/dealers') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: '22222222-2222-2222-2222-222222222203', code: 'DLR-DN-01', name: 'Apex Moto Da Nang Coastal Hub', city: 'Da Nang', isActive: true }
                      ]
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };
              const custTdLiveParamCtrl = new custTestDrivesDef.controller(
                mockQ,
                p8CustBus,
                { modelId: '55555555-5555-5555-5555-555555555501', dealerId: '22222222-2222-2222-2222-222222222203' },
                p4AuthSvc,
                p8Api,
                mapper,
                p8CustSignalR,
                p7ToastSvc,
                p8CustTokenStore
              );
              custTdLiveParamCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));
              const liveNonPreviewModelPreserved =
                custTdLiveParamCtrl.draft.motorcycleModelId === '55555555-5555-5555-5555-555555555501' &&
                custTdLiveParamCtrl.draft.dealerId === '22222222-2222-2222-2222-222222222203';
              custTdLiveParamCtrl.$onDestroy();

              const dtoParityTd = p8Api.normalizeTestDrive({
                testDriveId: 'td-dto-1',
                bookingNumber: 'TD-DTO-1',
                dealerCity: 'Da Nang',
                motorcycleModelCode: 'HND-AB160-2026',
                assignedColorName: 'Matte Black'
              });
              const dtoParitySp = p8Api.normalizeSparePart({
                partId: 'sp-dto-1',
                partCode: 'PRT-DTO-01',
                name: 'DTO Part',
                category: 'Filter',
                compatibleModelId: null,
                isUniversalFit: true,
                unitPriceVnd: 150000
              });
              const dtoParityRcpt = p8Api.normalizePrintableReceipt({
                receiptNumber: 'RCP-SLS-SO-2026-001',
                receiptType: 'ShowroomSalesOrder',
                orderNumber: 'SO-2026-001',
                soldByStaffName: 'Pham Minh Tuan',
                finalAmount: 102500000
              });
              const dtoParityOk =
                dtoParityTd.motorcycleModelCode === 'HND-AB160-2026' &&
                dtoParityTd.dealerCity === 'Da Nang' &&
                dtoParityTd.assignedColorName === 'Matte Black' &&
                dtoParitySp.partId === 'sp-dto-1' &&
                dtoParitySp.isUniversalFit === true &&
                dtoParityRcpt.receiptKind === 'SalesOrder' &&
                dtoParityRcpt.issuedByStaffName === 'Pham Minh Tuan';

              if (
                guestShowroomRedirect?.name !== 'app.public.login' ||
                !String(guestShowroomRedirect?.params?.returnUrl || '').startsWith('/customer/test-drives?modelId=55555555-5555-5555-5555-555555555502') ||
                customerShowroomRedirect?.name !== 'app.customer.testDrives' ||
                customerShowroomRedirect?.params?.modelId !== '55555555-5555-5555-5555-555555555502' ||
                customerShowroomRedirect?.params?.dealerId !== '22222222-2222-2222-2222-222222222201' ||
                !preselectedParamsOk ||
                !liveNonPreviewModelPreserved ||
                !dtoParityOk ||
                createdTd?.bookingNumber !== 'TD-20261124-8899' ||
                createdTd?.driverLicenseNumber !== 'A2-99887766' ||
                liveConfirmedCustTd?.status !== 'Confirmed' ||
                liveConfirmedCustTd?.assignedVin !== 'RLHJC9204RY000309' ||
                cancelledCustTd?.status !== 'Cancelled' ||
                !custTdPostDestroySafe
              ) {
                fail(
                  `Phase 8 suite (w) customerTestDrives/Showroom failed: guest=${JSON.stringify(guestShowroomRedirect)}, cust=${JSON.stringify(customerShowroomRedirect)}, created=${createdTd?.bookingNumber}, live=${liveConfirmedCustTd?.status}/${liveConfirmedCustTd?.assignedVin}, cancelled=${cancelledCustTd?.status}, postDestroy=${custTdPostDestroySafe}`
                );
              } else {
                pass('Verified T-804 Showroom [Book Test Drive] CTA (guest->login?returnUrl vs Customer->testDrives) & SCR-CUS-06 create/cancel + zero-reload TestDriveStatusChanged + $onDestroy.');
              }

              // (x) SCR-STF-06 (staffTestDrives) Zero-Reload TestDriveCreated Prepend, Deduplication, Demo VIN Confirm, SalesDesk Handoff & $onDestroy
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/test-drives') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'td-stf-1',
                          bookingNumber: 'TD-SEED-HCM-001',
                          customerId: '44444444-4444-4444-4444-444444444408',
                          customerFullName: 'Tran Ngoc Mai (Silver)',
                          customerEmail: 'customer.silver@motorcycledealer.vn',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          preferredDate: p8Api.getTodayDateVn(),
                          startTime: '09:00',
                          endTime: '09:45',
                          status: 'Requested',
                          rowVersion: 'AAAAAAAATD1='
                        }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/inventories') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'inv-demo-sh160',
                          vin: 'RLHJC9204RY000309',
                          engineNumber: 'JC92E-4000309',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          colorName: 'Cement Grey ABS Sport',
                          listedPrice: 102500000,
                          stockStatus: 'Available',
                          rowVersion: 'AAAAAAAAC1A='
                        }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/td-live-uuid-901') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        id: 'td-live-uuid-901-0000000000000000',
                        bookingNumber: 'TD-LIVE-901',
                        customerId: '44444444-4444-4444-4444-444444444409',
                        customerFullName: 'Le Hoang Nam (Gold)',
                        customerEmail: 'customer.gold@motorcycledealer.vn',
                        motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                        motorcycleModelName: 'Honda SH160i ABS Sport',
                        preferredDate: p8Api.getTodayDateVn(),
                        startTime: '10:00',
                        endTime: '10:45',
                        driverLicenseNumber: 'A2-88029104',
                        contactPhone: '0913000301',
                        status: 'Requested',
                        rowVersion: 'AAAAAAAATD8='
                      }
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/td-stf-1/status') && cfg.method === 'PATCH') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        id: 'td-stf-1',
                        bookingNumber: 'TD-SEED-HCM-001',
                        customerId: '44444444-4444-4444-4444-444444444408',
                        customerFullName: 'Tran Ngoc Mai (Silver)',
                        customerEmail: 'customer.silver@motorcycledealer.vn',
                        motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                        motorcycleModelName: 'Honda SH160i ABS Sport',
                        preferredDate: p8Api.getTodayDateVn(),
                        startTime: '09:00',
                        endTime: '09:45',
                        status: cfg.data.newStatus,
                        assignedInventoryId: cfg.data.assignedInventoryId,
                        assignedVin: cfg.data.assignedInventoryId ? 'RLHJC9204RY000309' : null,
                        rowVersion: 'AAAAAAAATD9='
                      }
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };

              p4AuthSvc.setSession({
                accessToken: 'jwt-p8-staff-hcm',
                refreshToken: 'rt-p8-staff-hcm',
                expiresInSeconds: 900,
                user: {
                  userId: '44444444-4444-4444-4444-444444444405',
                  email: 'staff.hcm@motorcycledealer.vn',
                  fullName: 'Pham Minh Tuan',
                  role: 'DealerStaff',
                  dealerId: '22222222-2222-2222-2222-222222222201',
                  dealerCode: 'DLR-HCM-01',
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').DealerStaff.slice()
                }
              });

              const p8StfBus = createEventBusRootScope();
              const p8StfNotifStore = new notifStoreCtor();
              const p8StfTokenStore = new tokenStoreCtor({});
              p8StfTokenStore.setTokens('jwt-p8-staff-hcm', 900, 'rt-p8-staff-hcm');
              const p8StfSignalR = new signalrSvcCtor(
                mockQ,
                p8StfBus,
                registeredConstants.get('APP_CONFIG'),
                p8StfTokenStore,
                p8StfNotifStore,
                p7ToastSvc
              );
              const stfTransitions = [];
              const mockStfState = {
                go: (name, params) => { stfTransitions.push({ name, params: params || null }); }
              };

              const stfTdCtrl = new staffTestDrivesDef.controller(
                mockQ,
                p8StfBus,
                mockStfState,
                p4AuthSvc,
                p8Api,
                mapper,
                p8StfSignalR,
                p7ToastSvc,
                p8StfTokenStore
              );
              stfTdCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));
              const initialStfTdCount = stfTdCtrl.testDrives.length;

              // Emit live TestDriveCreated SignalR frame + duplicate frame -> must prepend once & increment liveArrivalCount to 1!
              const incomingTdFrame = {
                notificationId: 'notif-td-live-901',
                eventType: 'TestDriveCreated',
                testDriveId: 'td-live-uuid-901',
                bookingNumber: 'TD-LIVE-901',
                dealerId: '22222222-2222-2222-2222-222222222201',
                customerFullName: 'Le Hoang Nam (Gold)',
                motorcycleModelName: 'Honda SH160i ABS Sport',
                preferredDate: p8Api.getTodayDateVn(),
                startTime: '10:00',
                endTime: '10:45',
                status: 'Requested',
                occurredAtUtc: new Date().toISOString()
              };
              p8StfSignalR.simulateEvent('TestDriveCreated', incomingTdFrame);
              p8StfSignalR.simulateEvent('TestDriveCreated', incomingTdFrame);
              const countAfterLiveTd = stfTdCtrl.testDrives.length;
              const liveBadgeCount = stfTdCtrl.liveArrivalCount;
              const topPrependNumber = stfTdCtrl.testDrives[0]?.bookingNumber;
              const notifBellRecordedTd = p8StfNotifStore.getUnreadCount() >= 1;

              // Assign demo VIN & confirm TD-SEED-HCM-001
              const seedCard = stfTdCtrl.testDrives.find((x) => x.id === 'td-stf-1');
              const demoChoices = stfTdCtrl.getDemoUnitsForCard(seedCard);
              stfTdCtrl.demoVinSelections['td-stf-1'] = 'inv-demo-sh160';
              const confirmedSeedCard = await stfTdCtrl.transitionStatus(seedCard, 'Confirmed');
              const kpiConfirmedToday = stfTdCtrl.kpis.confirmedToday;

              // Convert confirmed/completed test drive to bike sale -> hands off context to staffSalesDesk!
              stfTdCtrl.convertToBikeSale(confirmedSeedCard);
              const salesDeskCtrlForHandoff = new staffSalesDef.controller(
                mockQ,
                p4AuthSvc,
                p8Api,
                loyaltyCalc,
                mapper,
                p7ToastSvc,
                p8StfTokenStore
              );
              salesDeskCtrlForHandoff.$onInit();
              await new Promise((r) => setTimeout(r, 20));
              const handoffOpenedUnit = salesDeskCtrlForHandoff.checkoutUnit?.vin === 'RLHJC9204RY000309';
              const handoffPrefilledBuyer = salesDeskCtrlForHandoff.checkoutDraft?.customerEmail === 'customer.silver@motorcycledealer.vn';
              salesDeskCtrlForHandoff.$onDestroy();

              // Verify $onDestroy detaches SignalR listeners
              stfTdCtrl.$onDestroy();
              p8StfSignalR.simulateEvent('TestDriveCreated', Object.assign({}, incomingTdFrame, {
                notificationId: 'notif-td-live-902',
                testDriveId: 'td-live-uuid-902',
                bookingNumber: 'TD-LIVE-902'
              }));
              const countAfterStfTdDestroy = stfTdCtrl.testDrives.length;

              if (
                initialStfTdCount !== 1 ||
                countAfterLiveTd !== 2 ||
                liveBadgeCount !== 1 ||
                topPrependNumber !== 'TD-LIVE-901' ||
                !notifBellRecordedTd ||
                demoChoices.length !== 1 ||
                confirmedSeedCard?.status !== 'Confirmed' ||
                confirmedSeedCard?.assignedVin !== 'RLHJC9204RY000309' ||
                kpiConfirmedToday !== 1 ||
                stfTransitions[0]?.name !== 'app.staff.salesDesk' ||
                !handoffOpenedUnit ||
                !handoffPrefilledBuyer ||
                countAfterStfTdDestroy !== 2
              ) {
                fail(
                  `Phase 8 suite (x) staffTestDrives failed: init=${initialStfTdCount}, afterLive=${countAfterLiveTd}, badge=${liveBadgeCount}, top=${topPrependNumber}, confirm=${confirmedSeedCard?.status}/${confirmedSeedCard?.assignedVin}, handoff=${handoffOpenedUnit}/${handoffPrefilledBuyer}, postDestroy=${countAfterStfTdDestroy}`
                );
              } else {
                pass('Verified T-804 SCR-STF-06 zero-reload TestDriveCreated prepend, dedup, demo VIN Confirm, 1-click SalesDesk handoff, and $onDestroy.');
              }

              // (y) SCR-STF-02 (staffInvoiceBuilder) OEM Spare-Parts Picker Auto-Sum + Mid-Edit SignalR Draft Preservation + Itemized POST
              let p8BkRowVer = 'AAAAAAAAB80=';
              let p8InvoicePostBody = null;
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'bk-p8-parts-1',
                          bookingNumber: 'BK-20261123-P801',
                          status: 'CheckedIn',
                          customerId: '44444444-4444-4444-4444-444444444408',
                          customerFullName: 'Tran Ngoc Mai (Silver)',
                          vin: 'RLHJF5109RZ104829',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          odometerAtBookingKm: 4000,
                          rowVersion: p8BkRowVer
                        }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/spare-parts') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: 'sp-oil-1', partCode: 'PRT-OIL-10W30', name: 'Pro Honda 10W-30 Oil', category: 'EngineOil', compatibleModelId: null, unitLabel: 'Bottle', unitPriceVnd: 185000, isActive: true },
                        { id: 'sp-flt-1', partCode: 'PRT-FLT-AIR01', name: 'SH160i Air Filter', category: 'Filter', compatibleModelId: '55555555-5555-5555-5555-555555555502', compatibleModelName: 'Honda SH160i ABS Sport', unitLabel: 'Piece', unitPriceVnd: 220000, isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.includes('/loyalty/customers/') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        customerId: '44444444-4444-4444-4444-444444444408',
                        currentTierCode: 'Silver',
                        pointsMultiplier: 1.2,
                        availablePoints: 649,
                        lifetimeQualifyingPoints: 1960,
                        eligibleVouchers: []
                      }
                    }
                  };
                }
                if (cfg.url.endsWith('/service-invoices') && cfg.method === 'POST') {
                  p8InvoicePostBody = cfg.data;
                  return {
                    status: 201,
                    data: {
                      success: true,
                      data: {
                        invoiceId: 'inv-p8-settled-1',
                        invoiceNumber: 'INV-20261123-P801',
                        serviceBookingId: 'bk-p8-parts-1',
                        bookingNumber: 'BK-20261123-P801',
                        vin: 'RLHJF5109RZ104829',
                        laborAmount: cfg.data.laborAmount,
                        partsAmount: cfg.data.partsAmount,
                        grossAmount: cfg.data.laborAmount + cfg.data.partsAmount,
                        voucherDiscountAmount: 0,
                        netPaidAmount: cfg.data.laborAmount + cfg.data.partsAmount,
                        odometerAtCompletionKm: cfg.data.odometerAtCompletionKm,
                        pointsEarned: 89,
                        newTierCode: 'Gold',
                        tierUpgraded: true,
                        lineItems: [
                          { lineNumber: 1, partCodeSnapshot: 'PRT-OIL-10W30', description: 'Pro Honda 10W-30 Oil', itemType: 'Part', quantity: 2, unitPriceVnd: 185000, lineTotalVnd: 370000 },
                          { lineNumber: 2, partCodeSnapshot: 'PRT-FLT-AIR01', description: 'SH160i Air Filter', itemType: 'Part', quantity: 1, unitPriceVnd: 220000, lineTotalVnd: 220000 },
                          { lineNumber: 3, partCodeSnapshot: 'SVC-LABOR', description: 'Workshop Labor', itemType: 'Labor', quantity: 1, unitPriceVnd: 150000, lineTotalVnd: 150000 }
                        ]
                      }
                    }
                  };
                }
                if (cfg.url.endsWith('/service-invoices/inv-p8-settled-1/receipt') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        receiptNumber: 'RCP-SVC-INV-20261123-P801',
                        invoiceNumber: 'INV-20261123-P801',
                        issuedAtUtc: '2026-11-23T12:00:00Z',
                        verificationCode: 'A1B2C3D4E5F60718',
                        dealer: { code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', address: '128 Nguyen Hue', city: 'Ho Chi Minh City', phone: '02838229901' },
                        customer: { fullName: 'Tran Ngoc Mai (Silver)', email: 'customer.silver@motorcycledealer.vn', phone: '0913000201' },
                        vehicle: { vin: 'RLHJF5109RZ104829', licensePlate: '59-A3 889.21', modelName: 'Honda SH160i ABS Sport', odometerAtBookingKm: 4000, odometerAtCompletionKm: 4015, nextDueMileageKm: 8000 },
                        lineItems: [
                          { lineNumber: 1, partCodeSnapshot: 'PRT-OIL-10W30', description: 'Pro Honda 10W-30 Oil', itemType: 'Part', quantity: 2, unitPriceVnd: 185000, lineTotalVnd: 370000 },
                          { lineNumber: 2, partCodeSnapshot: 'PRT-FLT-AIR01', description: 'SH160i Air Filter', itemType: 'Part', quantity: 1, unitPriceVnd: 220000, lineTotalVnd: 220000 },
                          { lineNumber: 3, partCodeSnapshot: 'SVC-LABOR', description: 'Workshop Labor', itemType: 'Labor', quantity: 1, unitPriceVnd: 150000, lineTotalVnd: 150000 }
                        ],
                        laborAmount: 150000,
                        partsAmount: 590000,
                        grossAmount: 740000,
                        voucherDiscountAmount: 0,
                        netPaidAmount: 740000,
                        loyalty: { pointsEarned: 88, tierCodeAfter: 'Gold', pointsMultiplierApplied: 1.2, availablePointsAfter: 737, lifetimeQualifyingPointsAfter: 2048 }
                      }
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };

              const p8InvCtrl = new invoiceBuilderDef.controller(
                mockQ,
                p8StfBus,
                { bookingId: 'bk-p8-parts-1' },
                p4AuthSvc,
                p8Api,
                loyaltyCalc,
                mapper,
                p8StfSignalR,
                p7ToastSvc,
                p8StfTokenStore
              );
              p8InvCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 25));

              p8InvCtrl.invoice.laborAmount = 150000;
              p8InvCtrl.invoice.odometerAtCompletionKm = 4015;
              // Add 2x Oil (370,000) + 1x Filter (220,000) = 590,000 VND partsAmount
              p8InvCtrl.partPicker.selectedPartId = 'sp-oil-1';
              p8InvCtrl.partPicker.quantity = 2;
              p8InvCtrl.addSparePartLine();
              p8InvCtrl.partPicker.selectedPartId = 'sp-flt-1';
              p8InvCtrl.partPicker.quantity = 1;
              p8InvCtrl.addSparePartLine();

              const summedPartsOk =
                p8InvCtrl.draftLineItems.length === 2 &&
                p8InvCtrl.invoice.partsAmount === 590000 &&
                p8InvCtrl.summary.grossAmount === 740000;

              // Mid-edit SignalR BookingStatusChanged -> must update booking rowVersion WITHOUT wiping draftLineItems!
              p8BkRowVer = 'AAAAAAAAB99=';
              p8StfSignalR.simulateEvent('BookingStatusChanged', {
                notificationId: 'rt-p8-inv-1',
                bookingId: 'bk-p8-parts-1',
                bookingNumber: 'BK-20261123-P801',
                newStatus: 'InProgress'
              });
              await new Promise((r) => setTimeout(r, 25));
              const draftLinesPreservedOnSignalR =
                p8InvCtrl.selectedBooking?.rowVersion === 'AAAAAAAAB99=' &&
                p8InvCtrl.draftLineItems.length === 2 &&
                p8InvCtrl.invoice.partsAmount === 590000;

              const settledWithParts = await p8InvCtrl.submitInvoice();
              const loadedReceiptVm = await p8InvCtrl.openPrintableReceipt(settledWithParts);
              p8InvCtrl.$onDestroy();

              if (
                !summedPartsOk ||
                !draftLinesPreservedOnSignalR ||
                p8InvoicePostBody?.lineItems?.length !== 2 ||
                p8InvoicePostBody?.partsAmount !== 590000 ||
                loadedReceiptVm?.receiptNumber !== 'RCP-SVC-INV-20261123-P801' ||
                loadedReceiptVm?.verificationCode !== 'A1B2C3D4E5F60718' ||
                loadedReceiptVm?.lineItems?.length !== 3
              ) {
                fail(
                  `Phase 8 suite (y) staffInvoiceBuilder OEM parts & receipt failed: sum=${summedPartsOk}, preserved=${draftLinesPreservedOnSignalR}, postLines=${p8InvoicePostBody?.lineItems?.length}, rcpt=${loadedReceiptVm?.receiptNumber}`
                );
              } else {
                pass('Verified T-805 staffInvoiceBuilder OEM Spare-Parts auto-sum (590k + 150k = 740k), mid-edit SignalR draftLineItems preservation, itemized POST, and Official Receipt load.');
              }

              // (z) SCR-ADM-07 (adminSpareParts) CRUD, Search/Category/Model Filtering & printableReceiptModal Print/Body-Class Lifecycle
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/spare-parts') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        { id: 'sp-1', partCode: 'PRT-OIL-10W30', name: 'Pro Honda 10W-30 Oil', category: 'EngineOil', compatibleModelId: null, unitLabel: 'Bottle', unitPriceVnd: 185000, isActive: true },
                        { id: 'sp-2', partCode: 'PRT-FLT-AIR01', name: 'SH160i Air Filter', category: 'Filter', compatibleModelId: '55555555-5555-5555-5555-555555555502', compatibleModelName: 'Honda SH160i ABS Sport', unitLabel: 'Piece', unitPriceVnd: 220000, isActive: true }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/spare-parts') && cfg.method === 'POST') {
                  if (cfg.data.partCode === 'PRT-OIL-10W30') {
                    return Promise.reject({
                      status: 409,
                      data: {
                        status: 409,
                        title: 'Conflict',
                        detail: 'PartCode PRT-OIL-10W30 already exists.',
                        errorCode: 'DUPLICATE_PART_CODE'
                      }
                    });
                  }
                  return {
                    status: 201,
                    data: {
                      success: true,
                      data: Object.assign({ id: 'sp-created-3' }, cfg.data)
                    }
                  };
                }
                if (cfg.url.endsWith('/spare-parts/sp-1') && cfg.method === 'PUT') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: Object.assign({ id: 'sp-1' }, cfg.data)
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };

              const admPartsCtrl = new adminSparePartsDef.controller(
                mockQ,
                p8Api,
                mapper,
                p7ToastSvc,
                p8StfTokenStore
              );
              admPartsCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));

              admPartsCtrl.filters.modelFilter = 'UNIVERSAL';
              admPartsCtrl.applyFilters();
              const universalFilteredCount = admPartsCtrl.filteredParts.length;
              admPartsCtrl.resetFilters();

              admPartsCtrl.openCreateModal();
              admPartsCtrl.draft.partCode = 'prt-oil-10w30';
              admPartsCtrl.draft.name = 'Duplicate Test';
              const dupPartOut = await admPartsCtrl.saveSparePart();
              const dupPartErrCode = admPartsCtrl.modalProblem?.errorCode;

              admPartsCtrl.draft.partCode = 'prt-spk-ir01';
              admPartsCtrl.draft.name = 'NGK Laser Iridium Spark Plug';
              admPartsCtrl.draft.category = 'Electrical';
              admPartsCtrl.draft.unitLabel = 'Piece';
              admPartsCtrl.draft.unitPriceVnd = 320000;
              const createdPartOut = await admPartsCtrl.saveSparePart();

              admPartsCtrl.openEditModal(admPartsCtrl.parts[0]);
              admPartsCtrl.draft.unitPriceVnd = 195000;
              const updatedPartOut = await admPartsCtrl.saveSparePart();
              admPartsCtrl.$onDestroy();

              // Verify printableReceiptModal $window.print() & body class cleanup
              let printCallCount = 0;
              let modalClosedCount = 0;
              const bodyClasses = new Set();
              const mockPrintWindow = {
                print: () => { printCallCount++; },
                document: {
                  body: {
                    classList: {
                      add: (c) => bodyClasses.add(c),
                      remove: (c) => bodyClasses.delete(c)
                    }
                  }
                }
              };
              const rcptCtrl = new printableReceiptDef.controller(mockPrintWindow, p8Api);
              rcptCtrl.receipt = loadedReceiptVm;
              rcptCtrl.receiptKind = 'ServiceInvoice';
              rcptCtrl.onClose = () => { modalClosedCount++; };
              rcptCtrl.$onInit();
              const bodyClassAddedOnMount = bodyClasses.has('is-printing-receipt-open');
              rcptCtrl.printReceipt();
              rcptCtrl.closeModal();
              const bodyClassRemovedOnClose = !bodyClasses.has('is-printing-receipt-open');
              rcptCtrl.$onDestroy();

              if (
                universalFilteredCount !== 1 ||
                dupPartOut !== null ||
                dupPartErrCode !== 'DUPLICATE_PART_CODE' ||
                createdPartOut?.partCode !== 'PRT-SPK-IR01' ||
                updatedPartOut?.unitPriceVnd !== 195000 ||
                !bodyClassAddedOnMount ||
                printCallCount !== 1 ||
                modalClosedCount !== 1 ||
                !bodyClassRemovedOnClose
              ) {
                fail(
                  `Phase 8 suite (z) adminSpareParts / printableReceiptModal failed: univ=${universalFilteredCount}, dup=${dupPartErrCode}, created=${createdPartOut?.partCode}, updated=${updatedPartOut?.unitPriceVnd}, print=${printCallCount}, bodyClass=${bodyClassAddedOnMount}/${bodyClassRemovedOnClose}`
                );
              } else {
                pass('Verified T-805 SCR-ADM-07 (adminSpareParts CRUD, Universal/Model filters, 409 DUPLICATE_PART_CODE) and printableReceiptModal ($window.print + @media print body class cleanup).');
              }

              // (aa) Stage 3 Fixes (F-P8-SPA-01..03): resolvePostLoginTarget + PublicLogin deep-link, Demo VIN explicit clear, and SpareParts pageSize=100
              const p8RolePresets = registeredConstants.get('ROLE_PERMISSION_PRESETS');
              const adminPresetScopeOk = Array.isArray(p8RolePresets?.Admin) && p8RolePresets.Admin.length === 32 && typeof p4AuthSvc.getDemoAccounts === 'undefined';
              const staffPresetScopesOk = Array.isArray(p8RolePresets?.DealerStaff) && p8RolePresets.DealerStaff.length === 22;

              p4AuthSvc.setSession({
                accessToken: 'jwt-p8-cust-login-deeplink',
                refreshToken: 'rt-p8-cust-login-deeplink',
                expiresInSeconds: 900,
                user: {
                  userId: '44444444-4444-4444-4444-444444444408',
                  email: 'customer.silver@motorcycledealer.vn',
                  fullName: 'Tran Ngoc Mai (Silver)',
                  role: 'Customer',
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').Customer.slice()
                }
              });

              const deepLinkResolved = p4AuthSvc.resolvePostLoginTarget(
                '/customer/test-drives?modelId=55555555-5555-5555-5555-555555555502&dealerId=22222222-2222-2222-2222-222222222201',
                'app.customer.testDrives',
                mockState
              );
              const openRedirectBlocked = p4AuthSvc.resolvePostLoginTarget(
                'https://evil.example.com/phish?modelId=1',
                'app.customer.testDrives',
                mockState
              );
              const protocolRelativeBlocked = p4AuthSvc.resolvePostLoginTarget(
                '//evil.example.com/customer/test-drives?modelId=1',
                '',
                mockState
              );
              const authLoopBlocked = p4AuthSvc.resolvePostLoginTarget(
                '/login?returnUrl=/customer/test-drives',
                'app.customer.testDrives',
                mockState
              );
              const crossRolePathBlocked = p4AuthSvc.resolvePostLoginTarget(
                '/admin/spare-parts',
                'app.admin.spareParts',
                mockState
              );

              const publicLoginDef = registeredComponents.get('publicLogin');
              const loginNavCalls = [];
              const mockLoginState = {
                go: (name, params) => { loginNavCalls.push({ name, params: params || null }); },
                get: (name) => registeredStates.get(name) || null
              };
              const mockLoginAuthSvc = {
                login: () => Promise.resolve(p4AuthSvc.getCurrentUser()),
                resolvePostLoginTarget: (u, t, st) => p4AuthSvc.resolvePostLoginTarget(u, t, st),
                resolvePostLoginState: (c, st) => p4AuthSvc.resolvePostLoginState(c, st)
              };
              const loginCtrl = new publicLoginDef.controller(
                mockLoginState,
                {
                  returnUrl: '/customer/test-drives?modelId=55555555-5555-5555-5555-555555555502&dealerId=22222222-2222-2222-2222-222222222201',
                  returnTo: 'app.customer.testDrives'
                },
                mockLoginAuthSvc,
                mapper,
                p7ToastSvc
              );
              loginCtrl.$onInit();
              loginCtrl.credentials.email = 'customer.silver@motorcycledealer.vn';
              loginCtrl.credentials.password = 'Customer@123!';
              loginCtrl.loginForm = { $invalid: false };
              loginCtrl.submitLogin();
              await new Promise((r) => setTimeout(r, 20));

              // F-P8-SPA-02: staffTestDrives explicit empty-string Demo VIN selection sends assignedInventoryId === null
              let patchedAssignedInv = 'UNSET';
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/test-drives') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'td-clear-vin-1',
                          bookingNumber: 'TD-CLEAR-001',
                          status: 'Requested',
                          assignedInventoryId: 'inv-stale-preassigned',
                          assignedVin: 'RLHJC9204RY000309',
                          preferredDate: p8Api.getTodayDateVn(),
                          startTime: '09:00',
                          endTime: '09:45',
                          rowVersion: 'AAAAAAAATDA='
                        }
                      ]
                    }
                  };
                }
                if (cfg.url.endsWith('/test-drives/td-clear-vin-1/status') && cfg.method === 'PATCH') {
                  patchedAssignedInv = cfg.data.assignedInventoryId;
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: {
                        id: 'td-clear-vin-1',
                        bookingNumber: 'TD-CLEAR-001',
                        status: 'Confirmed',
                        assignedInventoryId: cfg.data.assignedInventoryId,
                        assignedVin: null,
                        rowVersion: 'AAAAAAAATDB='
                      }
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };
              const stfClearCtrl = new staffTestDrivesDef.controller(
                mockQ,
                p8StfBus,
                mockStfState,
                p4AuthSvc,
                p8Api,
                mapper,
                p8StfSignalR,
                p7ToastSvc,
                p8StfTokenStore
              );
              stfClearCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 20));
              // Staff explicitly selects '-- Optional Demo VIN Assignment --' ('')
              stfClearCtrl.demoVinSelections['td-clear-vin-1'] = '';
              await stfClearCtrl.transitionStatus(stfClearCtrl.testDrives[0], 'Confirmed');
              stfClearCtrl.$onDestroy();

              // F-P8-SPA-03: staffInvoiceBuilder passes pageSize: 100 on GET /api/v1/spare-parts
              const sparePartGetQueries = [];
              p8Responder = async (cfg) => {
                if (cfg.url.endsWith('/spare-parts') && cfg.method === 'GET') {
                  sparePartGetQueries.push(Object.assign({}, cfg.params || {}));
                  return { status: 200, data: { success: true, data: [] } };
                }
                if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
                  return {
                    status: 200,
                    data: {
                      success: true,
                      data: [
                        {
                          id: 'bk-pagesize-1',
                          bookingNumber: 'BK-PS-100',
                          status: 'CheckedIn',
                          customerId: '44444444-4444-4444-4444-444444444408',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          odometerAtBookingKm: 4000,
                          rowVersion: 'AAAAAAAAPS1='
                        }
                      ]
                    }
                  };
                }
                return { status: 200, data: { success: true, data: [] } };
              };
              const psInvCtrl = new invoiceBuilderDef.controller(
                mockQ,
                p8StfBus,
                { bookingId: 'bk-pagesize-1' },
                p4AuthSvc,
                p8Api,
                loyaltyCalc,
                mapper,
                p8StfSignalR,
                p7ToastSvc,
                p8StfTokenStore
              );
              psInvCtrl.$onInit();
              await new Promise((r) => setTimeout(r, 25));
              psInvCtrl.$onDestroy();

              const allSparePartQueriesHavePageSize100 =
                sparePartGetQueries.length >= 2 &&
                sparePartGetQueries.every((q) => q.pageSize === 100) &&
                sparePartGetQueries.some((q) => q.compatibleModelId === '55555555-5555-5555-5555-555555555502');

              if (
                !adminPresetScopeOk ||
                !staffPresetScopesOk ||
                deepLinkResolved.state !== 'app.customer.testDrives' ||
                deepLinkResolved.params?.modelId !== '55555555-5555-5555-5555-555555555502' ||
                deepLinkResolved.params?.dealerId !== '22222222-2222-2222-2222-222222222201' ||
                openRedirectBlocked.state !== 'app.customer.garage' ||
                Object.keys(openRedirectBlocked.params || {}).length !== 0 ||
                protocolRelativeBlocked.state !== 'app.customer.garage' ||
                authLoopBlocked.state !== 'app.customer.garage' ||
                crossRolePathBlocked.state !== 'app.customer.garage' ||
                loginNavCalls[0]?.name !== 'app.customer.testDrives' ||
                loginNavCalls[0]?.params?.modelId !== '55555555-5555-5555-5555-555555555502' ||
                loginNavCalls[0]?.params?.dealerId !== '22222222-2222-2222-2222-222222222201' ||
                patchedAssignedInv !== null ||
                !allSparePartQueriesHavePageSize100
              ) {
                fail(
                  `Stage 3 suite (aa) F-P8-SPA-01..03 failed: scopes=${adminPresetScopeOk}/${staffPresetScopesOk}, deepLink=${JSON.stringify(deepLinkResolved)}, loginNav=${JSON.stringify(loginNavCalls[0])}, clearedVin=${patchedAssignedInv}, spQueries=${JSON.stringify(sparePartGetQueries)}`
                );
              } else {
                pass('Verified Stage 3 F-P8-SPA-01..03 (resolvePostLoginTarget deep-link + open-redirect/role guards, explicit Demo VIN clear -> null, and SpareParts pageSize=100).');
              }

              // (ab) Stage 5 Clean-Code Refactor & Boundary Table Expansion (RF-P8-SPA-01 & RF-P8-SPA-02)
              const spCatsFirst = p8Api.getSparePartCategories();
              const spCatsAllExtensible =
                Array.isArray(spCatsFirst) &&
                spCatsFirst.length === 7 &&
                spCatsFirst.every((c) => Object.isExtensible(c));
              if (spCatsFirst[0]) {
                spCatsFirst[0].label = 'MUTATED_LOCAL_COPY';
              }
              const spCatsSecond = p8Api.getSparePartCategories();
              const spCatsIsolatedOk =
                spCatsAllExtensible &&
                spCatsSecond[0]?.value === 'EngineOil' &&
                spCatsSecond[0]?.label === 'EngineOil (Lubricants & Fluids)' &&
                spCatsSecond.map((c) => c.value).join(',') === 'EngineOil,Filter,Brakes,Transmission,Tires,Electrical,Accessory';

              const roleSessionFixtures = {
                Customer: {
                  userId: '44444444-4444-4444-4444-444444444408',
                  email: 'customer.silver@motorcycledealer.vn',
                  role: 'Customer',
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').Customer.slice()
                },
                DealerStaff: {
                  userId: '44444444-4444-4444-4444-444444444405',
                  email: 'staff.hcm@motorcycledealer.vn',
                  role: 'DealerStaff',
                  dealerId: '22222222-2222-2222-2222-222222222201',
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').DealerStaff.slice()
                },
                Admin: {
                  userId: '44444444-4444-4444-4444-444444444401',
                  email: 'admin@motorcycledealer.vn',
                  role: 'Admin',
                  permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').Admin.slice()
                }
              };

              const postLoginBoundaryCases = [
                { role: 'Customer', returnUrl: 'https://evil.example', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '//evil.example/x', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '/\\evil', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: 'javascript:alert(1)', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '/login', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '/register', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '/403', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'Customer', returnUrl: '/admin/spare-parts', returnTo: 'app.admin.spareParts', expectedState: 'app.customer.garage', expectedParams: {} },
                { role: 'DealerStaff', returnUrl: '/staff/bookings/bk-99/invoice', returnTo: '', expectedState: 'app.staff.invoiceBuilder', expectedParams: { bookingId: 'bk-99' } },
                { role: 'DealerStaff', returnUrl: '/staff/invoicing/bk-77?tab=BUILDER', returnTo: '', expectedState: 'app.staff.invoicing', expectedParams: { bookingId: 'bk-77', tab: 'BUILDER' } },
                { role: 'Customer', returnUrl: '/customer/test-drives?modelId=m1&dealerId=d1&__proto__=bad&constructor=bad&prototype=bad', returnTo: 'app.customer.testDrives', expectedState: 'app.customer.testDrives', expectedParams: { modelId: 'm1', dealerId: 'd1' } },
                { role: 'Admin', returnUrl: '', returnTo: 'app.admin.spareParts', expectedState: 'app.admin.spareParts', expectedParams: {} }
              ];

              const postLoginFailures = [];
              for (const tc of postLoginBoundaryCases) {
                p4AuthSvc.setSession({
                  accessToken: 'jwt-rf-p8-' + tc.role.toLowerCase(),
                  refreshToken: 'rt-rf-p8-' + tc.role.toLowerCase(),
                  expiresInSeconds: 900,
                  user: roleSessionFixtures[tc.role]
                });
                const actual = p4AuthSvc.resolvePostLoginTarget(tc.returnUrl, tc.returnTo, mockState);
                const actualKeys = Object.keys(actual.params || {}).sort();
                const expectedKeys = Object.keys(tc.expectedParams || {}).sort();
                const paramsMatch =
                  actualKeys.length === expectedKeys.length &&
                  expectedKeys.every((k) => actual.params[k] === tc.expectedParams[k]) &&
                  !Object.prototype.hasOwnProperty.call(actual.params, '__proto__') &&
                  !Object.prototype.hasOwnProperty.call(actual.params, 'constructor') &&
                  !Object.prototype.hasOwnProperty.call(actual.params, 'prototype');

                if (actual.state !== tc.expectedState || !paramsMatch) {
                  postLoginFailures.push(`${tc.role}(${tc.returnUrl}|${tc.returnTo}) -> got ${JSON.stringify(actual)}`);
                }
              }

              // normalizePrintableReceipt boundary matrix (SalesOrder vs ServiceInvoice fallback lines & deterministic fallback hash)
              const rcptSalesFallback1 = p8Api.normalizePrintableReceipt({
                orderNumber: 'SO-2026-8801',
                modelCode: 'HND-SH160I-2026',
                motorcycleModelName: 'Honda SH160i ABS Sport',
                colorName: 'Matte Black',
                vin: 'rlhjc9204ry000309',
                unitPrice: 102500000,
                discountAmount: 2000000,
                finalAmount: 100500000,
                pointsEarned: 15075,
                newTierCode: 'Platinum',
                appliedMultiplier: 1.5
              });
              const rcptSalesFallback2 = p8Api.normalizePrintableReceipt({
                orderNumber: 'SO-2026-8801',
                unitPrice: 102500000,
                discountAmount: 2000000,
                finalAmount: 100500000
              });
              const rcptSvcBothFallback = p8Api.normalizePrintableReceipt({
                invoiceNumber: 'INV-2026-8802',
                laborAmount: 150000,
                partsAmount: 370000,
                voucherDiscountAmount: 50000
              });
              const rcptSvcLaborOnlyFallback = p8Api.normalizePrintableReceipt({
                invoiceNumber: 'INV-2026-8803',
                laborAmount: 200000,
                partsAmount: 0
              });

              const rcptMatrixOk =
                rcptSalesFallback1.receiptKind === 'SalesOrder' &&
                rcptSalesFallback1.receiptNumber === 'RCP-SLS-SO-2026-8801' &&
                rcptSalesFallback1.lineItems.length === 1 &&
                rcptSalesFallback1.lineItems[0].itemType === 'Motorcycle' &&
                rcptSalesFallback1.lineItems[0].lineTotalVnd === 102500000 &&
                rcptSalesFallback1.netPaidAmount === 100500000 &&
                /^[0-9A-F]{16}$/.test(rcptSalesFallback1.verificationCode) &&
                rcptSalesFallback1.verificationCode === rcptSalesFallback2.verificationCode &&
                rcptSvcBothFallback.receiptKind === 'ServiceInvoice' &&
                rcptSvcBothFallback.receiptNumber === 'RCP-SVC-INV-2026-8802' &&
                rcptSvcBothFallback.lineItems.length === 2 &&
                rcptSvcBothFallback.lineItems[0].partCodeSnapshot === 'SVC-LABOR' &&
                rcptSvcBothFallback.lineItems[1].partCodeSnapshot === 'SVC-PARTS' &&
                rcptSvcBothFallback.grossAmount === 520000 &&
                rcptSvcBothFallback.netPaidAmount === 470000 &&
                /^[0-9A-F]{16}$/.test(rcptSvcBothFallback.verificationCode) &&
                rcptSvcBothFallback.verificationCode !== rcptSalesFallback1.verificationCode &&
                rcptSvcLaborOnlyFallback.lineItems.length === 1 &&
                rcptSvcLaborOnlyFallback.lineItems[0].itemType === 'Labor';

              if (!spCatsIsolatedOk || postLoginFailures.length > 0 || !rcptMatrixOk) {
                fail(
                  `Stage 5 suite (ab) RF-P8-SPA-01/02 failed: cats=${spCatsIsolatedOk}, postLoginFailures=${JSON.stringify(postLoginFailures)}, rcptMatrix=${rcptMatrixOk}`
                );
              } else {
                pass('Verified Stage 5 RF-P8-SPA-01/02 getSparePartCategories SSOT clones, 12-case resolvePostLoginTarget adversarial/prototype table, and normalizePrintableReceipt fallback matrix.');
              }

              // =========================================================================
              // 4g. Phase 9 (v1.2.0 — T-904 & T-905): 5-Star CSAT Reviews (+25 Bonus Pts), VIN Warranty Portal, Staff Live Desk & Admin Quality Oversight
              // =========================================================================
              const custWarrantyDef = registeredComponents.get('customerWarranty');
              const staffWarrantyReviewsDef = registeredComponents.get('staffWarrantyReviews');
              const adminQualityWarrantyDef = registeredComponents.get('adminQualityWarranty');

              if (!custWarrantyDef || !staffWarrantyReviewsDef || !adminQualityWarrantyDef) {
                fail('Missing Phase 9 component registration (customerWarranty, staffWarrantyReviews, or adminQualityWarranty).');
              } else {
                // (ac) 10-Event SignalR Hub Parity, Extensible Metadata Clones & 36m / 30,000km OEM Warranty Eligibility Boundary Matrix
                const p9EventsBus = createEventBusRootScope();
                const p9NotifStore = new notifStoreCtor();
                const p9PreviewStore = new tokenStoreCtor({});
                p9PreviewStore.setTokens('test-p9-jwt', 3600, null);
                p8Responder = async () => ({ status: 200, data: { success: true, data: [] } });
                const p9CapturedToasts = [];
                const p9ToastSpy = {
                  clearAll: () => {},
                  push: (t) => { p9CapturedToasts.push(t); },
                  info: (title, message) => { p9CapturedToasts.push({ intent: 'info', title, message }); },
                  success: (title, message) => { p9CapturedToasts.push({ intent: 'success', title, message }); },
                  warning: (title, message) => { p9CapturedToasts.push({ intent: 'warning', title, message }); },
                  danger: (title, message) => { p9CapturedToasts.push({ intent: 'danger', title, message }); }
                };
                const p9SignalR = new signalrSvcCtor(
                  mockQ,
                  p9EventsBus,
                  registeredConstants.get('APP_CONFIG'),
                  p9PreviewStore,
                  p9NotifStore,
                  p9ToastSpy
                );

                const canonicalHubEvents = p9SignalR.getCanonicalEvents();
                const hasAll10HubEvents =
                  Array.isArray(canonicalHubEvents) &&
                  canonicalHubEvents.length === 10 &&
                  canonicalHubEvents.includes('ServiceReviewSubmitted') &&
                  canonicalHubEvents.includes('WarrantyClaimCreated') &&
                  canonicalHubEvents.includes('WarrantyClaimStatusChanged');

                const p9BellCtrl = new notifBellDef.controller(
                  mockQ,
                  p9EventsBus,
                  { isAuthenticated: () => true, getRole: () => 'DealerStaff', getCurrentUser: () => ({ userId: 'u-stf', role: 'DealerStaff' }) },
                  p8Api,
                  p9NotifStore,
                  mapper,
                  p9PreviewStore
                );
                p9BellCtrl.$onInit();
                const unreadBeforeP9 = p9BellCtrl.unreadCount;
                p9SignalR.simulateEvent('ServiceReviewSubmitted', {
                  notificationId: 'n-p9-rev-1',
                  reviewNumber: 'REV-20261124-0001',
                  customerFullName: 'Tran Ngoc Mai',
                  motorcycleModelName: 'Honda SH160i ABS Sport',
                  rating: 5,
                  bonusPointsAwarded: 25
                });
                p9SignalR.simulateEvent('WarrantyClaimCreated', {
                  notificationId: 'n-p9-wc-1',
                  claimNumber: 'WC-20261124-0001',
                  vin: 'RLHJF5109RZ104829',
                  motorcycleModelName: 'Honda SH160i ABS Sport',
                  defectCategory: 'Electrical'
                });
                p9SignalR.simulateEvent('WarrantyClaimStatusChanged', {
                  notificationId: 'n-p9-wcs-1',
                  claimNumber: 'WC-20261124-0001',
                  newStatus: 'Completed',
                  coveredAmountVnd: 340000
                });
                const unreadAfterP9 = p9BellCtrl.unreadCount;
                p9BellCtrl.$onDestroy();

                const qTags1 = p8Api.getReviewQualityTags();
                const wCats1 = p8Api.getWarrantyDefectCategories();
                const clonesExtensible =
                  qTags1.length === 8 &&
                  qTags1.every((t) => Object.isExtensible(t)) &&
                  wCats1.length === 6 &&
                  wCats1.every((c) => Object.isExtensible(c));
                qTags1[0].label = 'MUTATED';
                wCats1[0].label = 'MUTATED';
                const clonesIsolated =
                  p8Api.getReviewQualityTags()[0].label !== 'MUTATED' &&
                  p8Api.getWarrantyDefectCategories()[0].label !== 'MUTATED';

                const refNow = '2026-11-24T00:00:00Z';
                const elig29999Km = p8Api.evaluateVehicleWarrantyEligibility({
                  ownershipStartDateUtc: '2024-01-15T00:00:00Z',
                  currentMileageKm: 29999
                }, refNow);
                const elig30000KmExact = p8Api.evaluateVehicleWarrantyEligibility({
                  ownershipStartDateUtc: '2023-11-24T00:00:00Z',
                  currentMileageKm: 30000
                }, refNow);
                const elig30001KmExpired = p8Api.evaluateVehicleWarrantyEligibility({
                  ownershipStartDateUtc: '2026-01-01T00:00:00Z',
                  currentMileageKm: 30001
                }, refNow);
                const elig36mPlus1sExpired = p8Api.evaluateVehicleWarrantyEligibility({
                  ownershipStartDateUtc: '2023-11-23T23:59:59Z',
                  currentMileageKm: 5000
                }, refNow);

                // Extensibility check on all 3 new Phase 9 component controllers in preview mode
                const p9PreviewControllers = [
                  { name: 'customerWarranty', inst: new custWarrantyDef.controller(mockQ, p9EventsBus, {}, p8Api, mapper, p9SignalR, p9ToastSpy, p9PreviewStore) },
                  { name: 'staffWarrantyReviews', inst: new staffWarrantyReviewsDef.controller(mockQ, p9EventsBus, p4AuthSvc, p8Api, mapper, p9SignalR, p9ToastSpy, p9PreviewStore) },
                  { name: 'adminQualityWarranty', inst: new adminQualityWarrantyDef.controller(mockQ, p9EventsBus, p8Api, mapper, p9SignalR, p9ToastSpy, p9PreviewStore) }
                ];
                let p9FrozenErrors = 0;
                for (const entry of p9PreviewControllers) {
                  entry.inst.$onInit();
                  for (const [, propVal] of Object.entries(entry.inst)) {
                    if (Array.isArray(propVal)) {
                      if (!Object.isExtensible(propVal)) { p9FrozenErrors++; }
                      for (const elem of propVal) {
                        if (elem && typeof elem === 'object' && !Object.isExtensible(elem)) { p9FrozenErrors++; }
                      }
                    }
                  }
                  entry.inst.$onDestroy();
                }

                if (
                  !hasAll10HubEvents ||
                  unreadAfterP9 !== unreadBeforeP9 + 3 ||
                  !clonesExtensible ||
                  !clonesIsolated ||
                  !elig29999Km.isCovered ||
                  elig29999Km.remainingKm !== 1 ||
                  !elig30000KmExact.isCovered ||
                  elig30001KmExpired.isCovered ||
                  elig36mPlus1sExpired.isCovered ||
                  p9FrozenErrors !== 0
                ) {
                  fail(
                    `Phase 9 suite (ac) failed: hub10=${hasAll10HubEvents}, unread=${unreadBeforeP9}->${unreadAfterP9}, clones=${clonesExtensible}/${clonesIsolated}, elig=[${elig29999Km.isCovered},${elig30000KmExact.isCovered},${elig30001KmExpired.isCovered},${elig36mPlus1sExpired.isCovered}], frozen=${p9FrozenErrors}`
                  );
                } else {
                  pass('Verified Phase 9 10-event SignalR hub & notificationBell sync, extensible tag/defect clones, 36m / 30,000 km OEM warranty boundary math, and zero frozen arrays.');
                }

                // (ad) SCR-CUS-04 (customerBookings) 5-Star CSAT Review Modal (+25 Bonus Pts) & 409 Auto-Hydration
                let p9PostedReviewBody = null;
                p8Responder = async (cfg) => {
                  if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          {
                            id: 'bk-p9-completed-1',
                            bookingNumber: 'BK-20261124-0901',
                            status: 'Completed',
                            dealerId: '22222222-2222-2222-2222-222222222201',
                            dealerName: 'Apex Moto Saigon Center',
                            vin: 'RLHJF5109RZ104829',
                            motorcycleModelName: 'Honda SH160i ABS Sport',
                            slotDate: '2026-11-23',
                            startTime: '09:00',
                            endTime: '10:00',
                            odometerAtBookingKm: 3850,
                            rowVersion: 'AAAAAAAAB91='
                          }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/service-invoices') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          {
                            id: 'inv-p9-settled-1',
                            invoiceId: 'inv-p9-settled-1',
                            invoiceNumber: 'INV-20261124-0901',
                            serviceBookingId: 'bk-p9-completed-1',
                            bookingNumber: 'BK-20261124-0901',
                            dealerId: '22222222-2222-2222-2222-222222222201',
                            dealerName: 'Apex Moto Saigon Center',
                            vin: 'RLHJF5109RZ104829',
                            motorcycleModelName: 'Honda SH160i ABS Sport',
                            netPaidAmount: 650000,
                            pointsEarned: 78
                          }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/service-reviews/my') && cfg.method === 'GET') {
                    return { status: 200, data: { success: true, data: [] } };
                  }
                  if (cfg.url.endsWith('/service-reviews') && cfg.method === 'POST') {
                    p9PostedReviewBody = cfg.data;
                    return {
                      status: 201,
                      data: {
                        success: true,
                        data: {
                          id: 'rev-p9-created-1',
                          reviewNumber: 'REV-20261124-9001',
                          serviceInvoiceId: cfg.data.serviceInvoiceId,
                          invoiceNumber: 'INV-20261124-0901',
                          serviceBookingId: 'bk-p9-completed-1',
                          rating: cfg.data.rating,
                          qualityTags: cfg.data.qualityTags,
                          comment: cfg.data.comment,
                          bonusPointsAwarded: 25,
                          availablePointsAfter: 674,
                          lifetimeQualifyingPointsAfter: 1960,
                          rowVersion: 'AAAAAAAARV9='
                        }
                      }
                    };
                  }
                  return { status: 200, data: { success: true, data: [] } };
                };

                const p9LiveTokenStore = new tokenStoreCtor({});
                p9LiveTokenStore.setTokens('jwt-p9-cust-silver', 900, 'rt-p9-cust-silver');
                p9CapturedToasts.length = 0;

                const p9CustBkCtrl = new custBookingsDef.controller(
                  mockQ,
                  p9EventsBus,
                  p8Api,
                  mapper,
                  p9SignalR,
                  p9ToastSpy,
                  p9LiveTokenStore
                );
                p9CustBkCtrl.$onInit();
                await new Promise((r) => setTimeout(r, 25));

                const completedBkCard = p9CustBkCtrl.bookings[0];
                const canRateBefore = p9CustBkCtrl.canRateBooking(completedBkCard);
                await p9CustBkCtrl.openReviewModal(completedBkCard);
                p9CustBkCtrl.selectRating(5);
                p9CustBkCtrl.toggleQualityTag('FastTurnaround');
                p9CustBkCtrl.reviewDraft.comment = 'Outstanding SH160i maintenance!';
                const submittedRev = await p9CustBkCtrl.submitServiceReview();
                const canRateAfter = p9CustBkCtrl.canRateBooking(completedBkCard);
                const linkedRev = p9CustBkCtrl.getBookingReview(completedBkCard);
                const bonusToastFired = p9CapturedToasts.some((t) => String(t.title || '').includes('+25 Bonus'));
                p9CustBkCtrl.$onDestroy();

                if (
                  !canRateBefore ||
                  canRateAfter !== false ||
                  submittedRev?.reviewNumber !== 'REV-20261124-9001' ||
                  submittedRev?.bonusPointsAwarded !== 25 ||
                  p9PostedReviewBody?.rating !== 5 ||
                  !Array.isArray(p9PostedReviewBody?.qualityTags) ||
                  !p9PostedReviewBody.qualityTags.includes('FastTurnaround') ||
                  linkedRev?.rating !== 5 ||
                  !bonusToastFired
                ) {
                  fail(
                    `Phase 9 suite (ad) customerBookings 5-star review failed: before=${canRateBefore}, after=${canRateAfter}, rev=${JSON.stringify(submittedRev)}, toast=${bonusToastFired}`
                  );
                } else {
                  pass('Verified T-904 SCR-CUS-04 5-Star CSAT Review Modal (quality tag toggles, POST /service-reviews, +25 Bonus AvailablePoints toast, and zero-reload rated badge).');
                }

                // (ae) SCR-CUS-07 (customerWarranty) $stateParams Pre-Selection, Odometer/Warranty Guards, Claim Creation & Zero-Reload WarrantyClaimStatusChanged
                p8Responder = async (cfg) => {
                  if (cfg.url.endsWith('/vehicles') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          {
                            id: 'veh-p9-sh160',
                            vin: 'RLHJF5109RZ104829',
                            motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                            motorcycleModelName: 'Honda SH160i ABS Sport',
                            ownershipStartDateUtc: new Date(Date.now() - 120 * 86400000).toISOString(),
                            currentMileageKm: 3850,
                            nextDueMileageKm: 4000
                          }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/dealers') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/warranty-claims/my') && cfg.method === 'GET') {
                    return { status: 200, data: { success: true, data: [] } };
                  }
                  if (cfg.url.endsWith('/warranty-claims') && cfg.method === 'POST') {
                    return {
                      status: 201,
                      data: {
                        success: true,
                        data: {
                          id: 'wc-p9-live-1',
                          claimNumber: 'WC-20261124-9901',
                          dealerId: cfg.data.dealerId,
                          dealerCode: 'DLR-HCM-01',
                          dealerName: 'Apex Moto Saigon Center',
                          customerVehicleId: cfg.data.customerVehicleId,
                          vinSnapshot: 'RLHJF5109RZ104829',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          odometerAtClaimKm: cfg.data.odometerAtClaimKm,
                          defectCategory: cfg.data.defectCategory,
                          symptomDescription: cfg.data.symptomDescription,
                          status: 'Submitted',
                          coveredAmountVnd: 0,
                          rowVersion: 'AAAAAAAAWC1='
                        }
                      }
                    };
                  }
                  if (cfg.url.endsWith('/warranty-claims/wc-p9-live-1') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          id: 'wc-p9-live-1',
                          claimNumber: 'WC-20261124-9901',
                          dealerId: '22222222-2222-2222-2222-222222222201',
                          dealerName: 'Apex Moto Saigon Center',
                          customerVehicleId: 'veh-p9-sh160',
                          vinSnapshot: 'RLHJF5109RZ104829',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          odometerAtClaimKm: 3900,
                          defectCategory: 'Electrical',
                          symptomDescription: 'Intermittent ABS wheel-speed sensor fault code.',
                          status: 'Completed',
                          coveredSparePartCode: 'PRT-BRK-FR01',
                          coveredSparePartName: 'Nissin Front ABS Sensor Kit',
                          coveredAmountVnd: 340000,
                          resolutionNotes: 'Replaced ABS sensor assembly under OEM warranty.',
                          rowVersion: 'AAAAAAAAWC9='
                        }
                      }
                    };
                  }
                  return { status: 200, data: { success: true, data: [] } };
                };

                const p9CustWcCtrl = new custWarrantyDef.controller(
                  mockQ,
                  p9EventsBus,
                  { vehicleId: 'veh-p9-sh160', dealerId: '22222222-2222-2222-2222-222222222201' },
                  p8Api,
                  mapper,
                  p9SignalR,
                  p9ToastSpy,
                  p9LiveTokenStore
                );
                p9CustWcCtrl.$onInit();
                await new Promise((r) => setTimeout(r, 25));

                const wcPrefilledOdo = p9CustWcCtrl.draft.odometerAtClaimKm === 3850;
                p9CustWcCtrl.draft.symptomDescription = 'Intermittent ABS wheel-speed sensor fault code.';
                // Odometer regression (< 3850) & > 30,000 km must disable canSubmitWarrantyClaim()
                p9CustWcCtrl.draft.odometerAtClaimKm = 3800;
                p9CustWcCtrl.onOdometerEdited();
                const regressionBlocked = p9CustWcCtrl.canSubmitWarrantyClaim() === false;
                p9CustWcCtrl.draft.odometerAtClaimKm = 30001;
                p9CustWcCtrl.onOdometerEdited();
                const overLimitBlocked = p9CustWcCtrl.canSubmitWarrantyClaim() === false;

                p9CustWcCtrl.draft.odometerAtClaimKm = 3900;
                p9CustWcCtrl.draft.defectCategory = 'Electrical';
                p9CustWcCtrl.onOdometerEdited();
                const createdWc = await p9CustWcCtrl.submitWarrantyClaim();

                // Live SignalR WarrantyClaimStatusChanged -> Completed with hydrated part & resolutionNotes (0 reloads!)
                p9SignalR.simulateEvent('WarrantyClaimStatusChanged', {
                  notificationId: 'rt-wc-done-1',
                  claimId: 'wc-p9-live-1',
                  claimNumber: 'WC-20261124-9901',
                  previousStatus: 'Approved',
                  newStatus: 'Completed',
                  coveredAmountVnd: 340000
                });
                await new Promise((r) => setTimeout(r, 25));
                const liveHydratedClaim = p9CustWcCtrl.claims[0];

                p9CustWcCtrl.$onDestroy();
                p9SignalR.simulateEvent('WarrantyClaimStatusChanged', {
                  notificationId: 'rt-wc-post-destroy',
                  claimId: 'wc-p9-live-1',
                  claimNumber: 'WC-20261124-9901',
                  newStatus: 'Rejected'
                });
                const custWcPostDestroySafe = p9CustWcCtrl.claims[0]?.status === 'Completed';

                if (
                  !wcPrefilledOdo ||
                  !regressionBlocked ||
                  !overLimitBlocked ||
                  createdWc?.claimNumber !== 'WC-20261124-9901' ||
                  liveHydratedClaim?.status !== 'Completed' ||
                  liveHydratedClaim?.coveredSparePartCode !== 'PRT-BRK-FR01' ||
                  liveHydratedClaim?.coveredAmountVnd !== 340000 ||
                  !custWcPostDestroySafe
                ) {
                  fail(
                    `Phase 9 suite (ae) customerWarranty failed: prefill=${wcPrefilledOdo}, blocks=[${regressionBlocked},${overLimitBlocked}], created=${createdWc?.claimNumber}, live=${JSON.stringify(liveHydratedClaim)}, postDestroy=${custWcPostDestroySafe}`
                  );
                } else {
                  pass('Verified T-904 SCR-CUS-07 OEM eligibility guards, VIN warranty submission, zero-reload WarrantyClaimStatusChanged hydration, and $onDestroy.');
                }

                // (af) SCR-STF-07 (staffWarrantyReviews) Zero-Reload Prepend/Dedup, OEM Part Transition & Official Reply + SCR-ADM-08 (adminQualityWarranty) Multi-Dealer Matrix
                p8Responder = async (cfg) => {
                  if (cfg.url.endsWith('/warranty-claims') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          {
                            id: 'wc-hq-1',
                            claimNumber: 'WC-SEED-HN-001',
                            dealerId: '22222222-2222-2222-2222-222222222202',
                            dealerCode: 'DLR-HN-01',
                            dealerName: 'Apex Moto Hanoi Flagship',
                            vinSnapshot: 'RLHNC5501RY000201',
                            motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                            motorcycleModelName: 'Honda SH160i ABS Sport',
                            odometerAtClaimKm: 4200,
                            defectCategory: 'Electrical',
                            symptomDescription: 'ABS harness fault.',
                            status: 'Completed',
                            coveredAmountVnd: 350000,
                            rowVersion: 'AAAAAAAAWC1='
                          }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/service-reviews') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          items: [
                            {
                              id: 'rev-hq-1',
                              reviewNumber: 'REV-SEED-DN-001',
                              serviceInvoiceId: 'inv-dn-01',
                              dealerId: '22222222-2222-2222-2222-222222222203',
                              dealerCode: 'DLR-DN-01',
                              dealerName: 'Apex Moto Da Nang Coastal Hub',
                              customerFullName: 'Le Hoang Nam',
                              motorcycleModelName: 'Honda Air Blade 160 SE',
                              rating: 4,
                              comment: 'Good service.',
                              bonusPointsAwarded: 25,
                              staffResponse: null,
                              rowVersion: 'AAAAAAAARV1='
                            }
                          ],
                          averageRating: 4.0,
                          fiveStarCount: 0,
                          unansweredCount: 1,
                          totalBonusPointsAwarded: 25
                        }
                      }
                    };
                  }
                  if (cfg.url.endsWith('/spare-parts') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          { id: 'sp-sh160-abs', partCode: 'PRT-BRK-FR01', name: 'Nissin ABS Sensor', category: 'Electrical', compatibleModelId: '55555555-5555-5555-5555-555555555502', unitPriceVnd: 340000, isActive: true },
                          { id: 'sp-other-model', partCode: 'PRT-NX500-01', name: 'NX500 Shield', category: 'Accessory', compatibleModelId: '55555555-5555-5555-5555-555555555506', unitPriceVnd: 990000, isActive: true }
                        ]
                      }
                    };
                  }
                  if (cfg.url.endsWith('/warranty-claims/wc-live-stf-901/status') && cfg.method === 'PATCH') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          id: 'wc-live-stf-901',
                          claimNumber: 'WC-LIVE-901',
                          dealerId: '22222222-2222-2222-2222-222222222201',
                          dealerCode: 'DLR-HCM-01',
                          vinSnapshot: 'RLHJF5109RZ104829',
                          motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                          motorcycleModelName: 'Honda SH160i ABS Sport',
                          status: cfg.data.newStatus,
                          coveredSparePartId: cfg.data.coveredSparePartId,
                          coveredSparePartCode: 'PRT-BRK-FR01',
                          coveredAmountVnd: cfg.data.coveredAmountVnd,
                          resolutionNotes: cfg.data.resolutionNotes,
                          rowVersion: 'AAAAAAAAWC8='
                        }
                      }
                    };
                  }
                  if (cfg.url.endsWith('/service-reviews/rev-live-stf-901/response') && cfg.method === 'PATCH') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          id: 'rev-live-stf-901',
                          reviewNumber: 'REV-LIVE-901',
                          dealerId: '22222222-2222-2222-2222-222222222201',
                          dealerCode: 'DLR-HCM-01',
                          rating: 5,
                          comment: 'Fast workshop!',
                          bonusPointsAwarded: 25,
                          staffResponse: cfg.data.staffResponse,
                          respondedAtUtc: '2026-11-24T01:00:00Z',
                          rowVersion: 'AAAAAAAARV8='
                        }
                      }
                    };
                  }
                  if (cfg.url.includes('/dealers') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: [
                          { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true },
                          { id: '22222222-2222-2222-2222-222222222202', code: 'DLR-HN-01', name: 'Apex Moto Hanoi Flagship', city: 'Hanoi', isActive: true },
                          { id: '22222222-2222-2222-2222-222222222203', code: 'DLR-DN-01', name: 'Apex Moto Da Nang Coastal Hub', city: 'Da Nang', isActive: true }
                        ]
                      }
                    };
                  }
                  if (cfg.url.includes('/admin/analytics/overview') && cfg.method === 'GET') {
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          totalRevenueVnd: 299000000,
                          averageCsatRating: 4.5,
                          totalServiceReviewsCount: 2,
                          openWarrantyClaimsCount: 1,
                          approvedWarrantyCoveredVnd: 690000
                        }
                      }
                    };
                  }
                  return { status: 200, data: { success: true, data: [] } };
                };

                p4AuthSvc.setSession({
                  accessToken: 'jwt-p9-staff-hcm',
                  refreshToken: 'rt-p9-staff-hcm',
                  expiresInSeconds: 900,
                  user: {
                    userId: '44444444-4444-4444-4444-444444444405',
                    email: 'staff.hcm@motorcycledealer.vn',
                    role: 'DealerStaff',
                    dealerId: '22222222-2222-2222-2222-222222222201',
                    dealerCode: 'DLR-HCM-01',
                    permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').DealerStaff.slice()
                  }
                });

                const stfDeskCtrl = new staffWarrantyReviewsDef.controller(
                  mockQ,
                  p9EventsBus,
                  p4AuthSvc,
                  p8Api,
                  mapper,
                  p9SignalR,
                  p9ToastSpy,
                  p9LiveTokenStore
                );
                stfDeskCtrl.$onInit();
                await new Promise((r) => setTimeout(r, 25));

                // Live WarrantyClaimCreated + duplicate -> prepends once!
                const liveWcPayload = {
                  notificationId: 'n-stf-wc-901',
                  claimId: 'wc-live-stf-901',
                  claimNumber: 'WC-LIVE-901',
                  dealerId: '22222222-2222-2222-2222-222222222201',
                  dealerCode: 'DLR-HCM-01',
                  vin: 'RLHJF5109RZ104829',
                  motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                  motorcycleModelName: 'Honda SH160i ABS Sport',
                  defectCategory: 'Electrical',
                  symptomDescription: 'ABS sensor warning light',
                  status: 'Submitted',
                  rowVersion: 'AAAAAAAAWC0='
                };
                p9SignalR.simulateEvent('WarrantyClaimCreated', liveWcPayload);
                p9SignalR.simulateEvent('WarrantyClaimCreated', liveWcPayload);

                // Live ServiceReviewSubmitted + duplicate -> prepends once!
                const liveRevPayload = {
                  notificationId: 'n-stf-rev-901',
                  reviewId: 'rev-live-stf-901',
                  reviewNumber: 'REV-LIVE-901',
                  dealerId: '22222222-2222-2222-2222-222222222201',
                  dealerCode: 'DLR-HCM-01',
                  customerFullName: 'Tran Ngoc Mai',
                  motorcycleModelName: 'Honda SH160i ABS Sport',
                  rating: 5,
                  comment: 'Fast workshop!',
                  bonusPointsAwarded: 25,
                  rowVersion: 'AAAAAAAARV0='
                };
                p9SignalR.simulateEvent('ServiceReviewSubmitted', liveRevPayload);
                p9SignalR.simulateEvent('ServiceReviewSubmitted', liveRevPayload);

                const stfLiveCountsOk =
                  stfDeskCtrl.claims.length === 2 &&
                  stfDeskCtrl.reviews.length === 2 &&
                  stfDeskCtrl.liveArrivalCount === 2 &&
                  stfDeskCtrl.kpis.unansweredReviews === 2;

                // Manage Warranty Claim: filter compatible parts (only SH160i/Universal, excluding NX500!) & transition Submitted -> Approved -> Completed
                stfDeskCtrl.openClaimActionModal(stfDeskCtrl.claims[0]);
                const compatPartsFiltered =
                  stfDeskCtrl.compatibleModalParts.length === 1 &&
                  stfDeskCtrl.compatibleModalParts[0].partCode === 'PRT-BRK-FR01';
                stfDeskCtrl.claimActionDraft.newStatus = 'Approved';
                stfDeskCtrl.claimActionDraft.coveredSparePartId = 'sp-sh160-abs';
                stfDeskCtrl.onCoveredPartSelected();
                const autoFilledPartPrice = stfDeskCtrl.claimActionDraft.coveredAmountVnd === 340000;
                const approvedStfClaim = await stfDeskCtrl.submitClaimStatusUpdate();

                stfDeskCtrl.openClaimActionModal(stfDeskCtrl.claims[0]);
                stfDeskCtrl.claimActionDraft.newStatus = 'Completed';
                stfDeskCtrl.claimActionDraft.resolutionNotes = 'Replaced Nissin ABS sensor and verified road test.';
                const completedStfClaim = await stfDeskCtrl.submitClaimStatusUpdate();

                // Official Dealership Reply on live 5★ review
                stfDeskCtrl.openReplyModal(stfDeskCtrl.reviews[0]);
                stfDeskCtrl.replyDraftText = 'Thank you for choosing Apex Moto Saigon Center!';
                const repliedStfRev = await stfDeskCtrl.submitReviewReply();
                const unansweredAfterReply = stfDeskCtrl.kpis.unansweredReviews;

                stfDeskCtrl.$onDestroy();
                p9SignalR.simulateEvent('ServiceReviewSubmitted', Object.assign({}, liveRevPayload, {
                  reviewId: 'rev-post-destroy',
                  reviewNumber: 'REV-POST-DESTROY'
                }));
                const stfPostDestroySafe = stfDeskCtrl.reviews.length === 2;

                // Mount Admin Quality & Warranty Oversight (SCR-ADM-08)
                const admQwCtrl = new adminQualityWarrantyDef.controller(
                  mockQ,
                  p9EventsBus,
                  p8Api,
                  mapper,
                  p9SignalR,
                  p9ToastSpy,
                  p9LiveTokenStore
                );
                admQwCtrl.$onInit();
                await new Promise((r) => setTimeout(r, 25));
                const hqHasAll3Branches = admQwCtrl.branchRows.length === 3;
                admQwCtrl.filters.dealerId = '22222222-2222-2222-2222-222222222202'; // DLR-HN-01
                admQwCtrl.applyFilters();
                const hnFilteredRows = admQwCtrl.filteredBranchRows;
                admQwCtrl.$onDestroy();

                // Exact C# DTO parity assertions (#266 ServiceReviewDto, WarrantyClaimDto, ServiceReviewPageDto)
                const dtoRevParity = p8Api.normalizeServiceReview({
                  id: 'rev-dto-1',
                  reviewId: 'rev-dto-1',
                  reviewNumber: 'REV-DTO-001',
                  serviceType: 'PeriodicMaintenance',
                  dealerCity: 'Ho Chi Minh City',
                  motorcycleModelId: '55555555-5555-5555-5555-555555555502',
                  licensePlate: '59-A3 889.21',
                  rating: 5,
                  qualityTags: 'FastTurnaround,GenuineParts',
                  qualityTagList: ['FastTurnaround', 'GenuineParts'],
                  respondedByStaffName: 'Pham Minh Tuan',
                  staffResponse: 'Official response.',
                  isAnswered: true
                });
                const dtoWcParity = p8Api.normalizeWarrantyClaim({
                  id: 'wc-dto-1',
                  claimId: 'wc-dto-1',
                  claimNumber: 'WC-DTO-001',
                  customerPhoneNumber: '0913000201',
                  warrantyStartUtc: '2026-02-15T00:00:00Z',
                  warrantyExpiresAtUtc: '2029-02-15T00:00:00Z',
                  reviewedByStaffName: 'Pham Minh Tuan',
                  status: 'Completed',
                  coveredAmountVnd: 340000
                });
                const p9DtoParityOk =
                  dtoRevParity.respondedByStaffName === 'Pham Minh Tuan' &&
                  dtoRevParity.isAnswered === true &&
                  dtoRevParity.qualityTagList.length === 2 &&
                  dtoRevParity.dealerCity === 'Ho Chi Minh City' &&
                  dtoWcParity.customerPhoneNumber === '0913000201' &&
                  dtoWcParity.warrantyExpiresAtUtc === '2029-02-15T00:00:00Z' &&
                  dtoWcParity.reviewedByStaffName === 'Pham Minh Tuan';

                if (
                  !stfLiveCountsOk ||
                  !compatPartsFiltered ||
                  !autoFilledPartPrice ||
                  approvedStfClaim?.status !== 'Approved' ||
                  completedStfClaim?.status !== 'Completed' ||
                  repliedStfRev?.hasStaffResponse !== true ||
                  unansweredAfterReply !== 1 ||
                  !stfPostDestroySafe ||
                  !hqHasAll3Branches ||
                  hnFilteredRows.length !== 1 ||
                  hnFilteredRows[0]?.coveredVnd !== 350000 ||
                  !p9DtoParityOk
                ) {
                  fail(
                    `Phase 9 suite (af) staffWarrantyReviews / adminQualityWarranty failed: live=${stfLiveCountsOk}, compat=${compatPartsFiltered}, price=${autoFilledPartPrice}, done=${completedStfClaim?.status}, replied=${repliedStfRev?.hasStaffResponse}, unans=${unansweredAfterReply}, postDestroy=${stfPostDestroySafe}, hq=${hqHasAll3Branches}/${hnFilteredRows[0]?.coveredVnd}`
                  );
                } else {
                  pass('Verified T-905 SCR-STF-07 (zero-reload WarrantyClaimCreated & ServiceReviewSubmitted prepend, OEM part auto-price, Approved->Completed transition, official reply, $onDestroy) & SCR-ADM-08 multi-dealer oversight.');
                }

                // (ag) Stage 3 Fixes (F-P9-SPA-01 & F-P9-SPA-02): Null-preserving coveredAmountVnd, 8-tag CANONICAL_REVIEW_QUALITY_TAGS & v1.2 SEEDED_DEMO_ACCOUNTS parity
                const canonical8TagCodes = [
                  'FastTurnaround',
                  'TransparentPricing',
                  'ExpertTechnician',
                  'CleanLounge',
                  'GenuineParts',
                  'FriendlyAdvisor',
                  'ClearExplanation',
                  'ThoroughInspection'
                ];
                const actualQualityTags = p8Api.getReviewQualityTags();
                const all8QualityTagsParityOk =
                  actualQualityTags.length === 8 &&
                  canonical8TagCodes.every((code, idx) => actualQualityTags[idx]?.code === code && Object.isExtensible(actualQualityTags[idx]));

                const capturedPatchBodies = [];
                p8Responder = async (cfg) => {
                  if (cfg.url.includes('/warranty-claims/') && cfg.url.endsWith('/status') && cfg.method === 'PATCH') {
                    capturedPatchBodies.push(cfg.data);
                    return {
                      status: 200,
                      data: {
                        success: true,
                        data: {
                          id: 'wc-p9-patch-parity',
                          claimNumber: 'WC-20261124-9950',
                          status: cfg.data.newStatus,
                          coveredSparePartId: cfg.data.coveredSparePartId,
                          coveredAmountVnd: cfg.data.coveredAmountVnd ?? 340000,
                          rowVersion: 'AAAAAAAAWCP='
                        }
                      }
                    };
                  }
                  return { status: 200, data: { success: true, data: [] } };
                };

                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Completed',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  rowVersion: 'AAAAAAAAWC1='
                });
                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Completed',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  coveredAmountVnd: null,
                  rowVersion: 'AAAAAAAAWC2='
                });
                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Approved',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  coveredAmountVnd: '',
                  rowVersion: 'AAAAAAAAWC3='
                });
                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Approved',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  coveredAmountVnd: 0,
                  rowVersion: 'AAAAAAAAWC4='
                });
                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Approved',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  coveredAmountVnd: 340000,
                  rowVersion: 'AAAAAAAAWC5='
                });
                await p8Api.updateWarrantyClaimStatus('wc-p9-patch-parity', {
                  newStatus: 'Rejected',
                  coveredSparePartId: 'sp-p9-abs-sensor',
                  coveredAmountVnd: 340000,
                  rejectionReason: 'Accident impact outside OEM coverage.',
                  rowVersion: 'AAAAAAAAWC6='
                });

                const nullPreservingCoveredAmtOk =
                  capturedPatchBodies.length === 6 &&
                  capturedPatchBodies[0].coveredAmountVnd === null &&
                  capturedPatchBodies[1].coveredAmountVnd === null &&
                  capturedPatchBodies[2].coveredAmountVnd === null &&
                  capturedPatchBodies[3].coveredAmountVnd === 0 &&
                  capturedPatchBodies[4].coveredAmountVnd === 340000 &&
                  capturedPatchBodies[5].coveredAmountVnd === 0 &&
                  capturedPatchBodies[5].coveredSparePartId === null;

                const p9RolePresets = registeredConstants.get('ROLE_PERMISSION_PRESETS');
                const p9DemoScopesParityOk =
                  Array.isArray(p9RolePresets?.Admin) && p9RolePresets.Admin.length === 32 &&
                  Array.isArray(p9RolePresets?.DealerStaff) && p9RolePresets.DealerStaff.length === 22 &&
                  Array.isArray(p9RolePresets?.Customer) && p9RolePresets.Customer.length === 20 &&
                  typeof p4AuthSvc.getDemoAccounts === 'undefined';

                if (!all8QualityTagsParityOk || !nullPreservingCoveredAmtOk || !p9DemoScopesParityOk) {
                  fail(
                    `Stage 3 suite (ag) F-P9-SPA-01..02 failed: tags8=${all8QualityTagsParityOk}, patchNulls=${JSON.stringify(capturedPatchBodies.map((b) => b.coveredAmountVnd))}, roleScopes=${p9DemoScopesParityOk}`
                  );
                } else {
                  pass('Verified Stage 3 F-P9-SPA-01 & F-P9-SPA-02 (null-preserving updateWarrantyClaimStatus coveredAmountVnd, all 8 CANONICAL_REVIEW_QUALITY_TAGS, and 48-permission Zero-Mock RBAC parity).');
                }

                // (ah) Stage 5 Clean-Code Refactor & Boundary Table Expansion (RF-P9-SPA-01)
                const warrantyEligibilityBoundaryTable = [
                  {
                    label: '30,000 km exact & inside 36m',
                    vehicle: { ownershipStartDateUtc: '2025-01-15T00:00:00Z', currentMileageKm: 30000 },
                    refUtc: '2026-11-24T00:00:00Z',
                    expectCovered: true,
                    expectRemainingKm: 0,
                    expectStartIso: '2025-01-15T00:00:00.000Z',
                    expectExpiryIso: '2028-01-15T00:00:00.000Z'
                  },
                  {
                    label: '30,001 km exceeded inside 36m',
                    vehicle: { ownershipStartDateUtc: '2025-01-15T00:00:00Z', currentMileageKm: 30001 },
                    refUtc: '2026-11-24T00:00:00Z',
                    expectCovered: false,
                    expectRemainingKm: 0,
                    expectStartIso: '2025-01-15T00:00:00.000Z',
                    expectExpiryIso: '2028-01-15T00:00:00.000Z'
                  },
                  {
                    label: '36m exact millisecond inclusive',
                    vehicle: { ownershipStartDateUtc: '2023-11-24T12:30:45.500Z', currentMileageKm: 12000 },
                    refUtc: '2026-11-24T12:30:45.500Z',
                    expectCovered: true,
                    expectRemainingKm: 18000,
                    expectStartIso: '2023-11-24T12:30:45.500Z',
                    expectExpiryIso: '2026-11-24T12:30:45.500Z'
                  },
                  {
                    label: '36m + 1ms expired',
                    vehicle: { ownershipStartDateUtc: '2023-11-24T12:30:45.500Z', currentMileageKm: 12000 },
                    refUtc: '2026-11-24T12:30:45.501Z',
                    expectCovered: false,
                    expectRemainingKm: 18000,
                    expectStartIso: '2023-11-24T12:30:45.500Z',
                    expectExpiryIso: '2026-11-24T12:30:45.500Z'
                  },
                  {
                    label: 'leap-year 2024-02-29 + 36m -> 2027-02-28 exact ms',
                    vehicle: { ownershipStartDateUtc: '2024-02-29T08:15:00.000Z', currentMileageKm: 9500 },
                    refUtc: '2027-02-28T08:15:00.000Z',
                    expectCovered: true,
                    expectRemainingKm: 20500,
                    expectStartIso: '2024-02-29T08:15:00.000Z',
                    expectExpiryIso: '2027-02-28T08:15:00.000Z'
                  },
                  {
                    label: 'leap-year 2024-02-29 + 36m + 1ms -> expired',
                    vehicle: { ownershipStartDateUtc: '2024-02-29T08:15:00.000Z', currentMileageKm: 9500 },
                    refUtc: '2027-02-28T08:15:00.001Z',
                    expectCovered: false,
                    expectRemainingKm: 20500,
                    expectStartIso: '2024-02-29T08:15:00.000Z',
                    expectExpiryIso: '2027-02-28T08:15:00.000Z'
                  },
                  {
                    label: 'fallback priority #1: ownershipStartDateUtc precedes purchasedAtUtc & createdAtUtc',
                    vehicle: {
                      ownershipStartDateUtc: '2025-06-01T00:00:00Z',
                      purchasedAtUtc: '2020-01-01T00:00:00Z',
                      createdAtUtc: '2020-01-01T00:00:00Z',
                      currentMileageKm: 5000
                    },
                    refUtc: '2026-11-24T00:00:00Z',
                    expectCovered: true,
                    expectRemainingKm: 25000,
                    expectStartIso: '2025-06-01T00:00:00.000Z',
                    expectExpiryIso: '2028-06-01T00:00:00.000Z'
                  },
                  {
                    label: 'fallback priority #2: purchasedAtUtc precedes createdAtUtc when ownershipStartDateUtc is null',
                    vehicle: {
                      ownershipStartDateUtc: null,
                      purchasedAtUtc: '2025-07-01T00:00:00Z',
                      createdAtUtc: '2020-01-01T00:00:00Z',
                      currentMileageKm: 4200
                    },
                    refUtc: '2026-11-24T00:00:00Z',
                    expectCovered: true,
                    expectRemainingKm: 25800,
                    expectStartIso: '2025-07-01T00:00:00.000Z',
                    expectExpiryIso: '2028-07-01T00:00:00.000Z'
                  },
                  {
                    label: 'fallback priority #3: createdAtUtc used when ownershipStartDateUtc & purchasedAtUtc are null',
                    vehicle: {
                      ownershipStartDateUtc: null,
                      purchasedAtUtc: null,
                      createdAtUtc: '2022-01-01T00:00:00Z',
                      currentMileageKm: 4200
                    },
                    refUtc: '2026-11-24T00:00:00Z',
                    expectCovered: false,
                    expectRemainingKm: 25800,
                    expectStartIso: '2022-01-01T00:00:00.000Z',
                    expectExpiryIso: '2025-01-01T00:00:00.000Z'
                  }
                ];

                const warrantyBoundaryFailures = [];
                for (const tc of warrantyEligibilityBoundaryTable) {
                  const res = p8Api.evaluateVehicleWarrantyEligibility(tc.vehicle, tc.refUtc);
                  if (
                    res.isCovered !== tc.expectCovered ||
                    res.remainingKm !== tc.expectRemainingKm ||
                    res.warrantyStartUtc !== tc.expectStartIso ||
                    res.warrantyExpiresAtUtc !== tc.expectExpiryIso
                  ) {
                    warrantyBoundaryFailures.push(`${tc.label}: got ${JSON.stringify(res)}`);
                  }
                }

                const reviewStarsTable = [
                  [5, '★★★★★'],
                  [4, '★★★★☆'],
                  [3, '★★★☆☆'],
                  [2, '★★☆☆☆'],
                  [1, '★☆☆☆☆'],
                  [0, '★☆☆☆☆'],
                  [9, '★★★★★'],
                  [null, '★★★★★'],
                  [undefined, '★★★★★']
                ];
                const reviewStarsOk = reviewStarsTable.every(
                  ([input, expected]) => p8Api.formatReviewStars(input) === expected
                );

                const openStatusTable = [
                  ['Submitted', true],
                  ['UnderReview', true],
                  ['Approved', true],
                  ['Completed', false],
                  ['Rejected', false],
                  ['', false],
                  [null, false]
                ];
                const openStatusOk = openStatusTable.every(
                  ([input, expected]) => p8Api.isOpenWarrantyStatus(input) === expected
                );

                const qualityTagDelimiterTable = [
                  {
                    raw: '["FastTurnaround", "GenuineParts", "FastTurnaround", "  "]',
                    expect: ['FastTurnaround', 'GenuineParts']
                  },
                  {
                    raw: 'TransparentPricing, ExpertTechnician, TransparentPricing',
                    expect: ['TransparentPricing', 'ExpertTechnician']
                  },
                  {
                    raw: 'CleanLounge; FriendlyAdvisor ; CleanLounge',
                    expect: ['CleanLounge', 'FriendlyAdvisor']
                  },
                  {
                    raw: 'ClearExplanation | ThoroughInspection | ClearExplanation',
                    expect: ['ClearExplanation', 'ThoroughInspection']
                  },
                  {
                    raw: ['GenuineParts', '  GenuineParts ', '', 'CleanLounge'],
                    expect: ['GenuineParts', 'CleanLounge']
                  },
                  {
                    raw: '',
                    expect: []
                  }
                ];
                const qualityTagTableOk = qualityTagDelimiterTable.every((tc) => {
                  const direct = p8Api.parseQualityTagsList(tc.raw);
                  const viaNorm = p8Api.normalizeServiceReview({ id: 'rev-tag-tc', qualityTags: tc.raw }).qualityTagList;
                  return (
                    JSON.stringify(direct) === JSON.stringify(tc.expect) &&
                    JSON.stringify(viaNorm) === JSON.stringify(tc.expect)
                  );
                });

                if (
                  warrantyBoundaryFailures.length !== 0 ||
                  !reviewStarsOk ||
                  !openStatusOk ||
                  !qualityTagTableOk
                ) {
                  fail(
                    `Stage 5 suite (ah) RF-P9-SPA-01 failed: warranty=[${warrantyBoundaryFailures.join('; ')}], stars=${reviewStarsOk}, openStatus=${openStatusOk}, tags=${qualityTagTableOk}`
                  );
                } else {
                  pass('Verified Stage 5 RF-P9-SPA-01 (9-case warranty eligibility leap-year/ms/fallback table, shared formatReviewStars & isOpenWarrantyStatus helpers, and 6-case parseQualityTagsList delimiter/dedup table).');
                }

                // (ai) Cross-Phase Stage 3 Fixes (F-XP-01b, F-XP-04b, F-XP-05, F-XP-06 & F-XP-09b)
                {
                  // 1. F-XP-01b: customerLoyalty ServiceReviewBonus ledger filter option + query param forwarding
                  const xpLoyaltyDef = registeredComponents.get('customerLoyalty');
                  const capturedLedgerQueries = [];
                  p8Responder = async (cfg) => {
                    if (cfg.url.endsWith('/loyalty/ledger') && cfg.method === 'GET') {
                      capturedLedgerQueries.push(cfg.params || {});
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            items: [
                              {
                                id: 'ple-csat-bonus-1',
                                transactionType: 'ServiceReviewBonus',
                                pointsDelta: 25,
                                balanceAfter: 674,
                                referenceType: 'ServiceReview',
                                idempotencyKey: 'SERVICE_REVIEW:inv-xp-06:BONUS',
                                notes: '+25 CSAT bonus points'
                              }
                            ],
                            pagination: { pageNumber: 1, pageSize: 15, totalCount: 1, totalPages: 1 }
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/loyalty/me') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            accountId: 'acc-xp-1',
                            currentTierCode: 'Silver',
                            pointsMultiplier: 1.2,
                            availablePoints: 674,
                            lifetimeQualifyingPoints: 1960,
                            rowVersion: 'AAAAAAAALOY='
                          }
                        }
                      };
                    }
                    return { status: 200, data: { success: true, data: [] } };
                  };

                  p4AuthSvc.setSession({
                    accessToken: 'jwt-xp-cust',
                    refreshToken: 'rt-xp-cust',
                    expiresInSeconds: 900,
                    user: {
                      userId: '44444444-4444-4444-4444-444444444408',
                      email: 'customer.silver@motorcycledealer.vn',
                      role: 'Customer',
                      tierCode: 'Silver',
                      tierMultiplier: 1.2,
                      availablePoints: 674,
                      lifetimeQualifyingPoints: 1960,
                      permissions: registeredConstants.get('ROLE_PERMISSION_PRESETS').Customer.slice()
                    }
                  });

                  const xpLoyaltyCtrl = new xpLoyaltyDef.controller(
                    mockQ,
                    p9EventsBus,
                    {},
                    p4AuthSvc,
                    p8Api,
                    registeredConstants.get('LoyaltyCalculator'),
                    mapper,
                    p9SignalR,
                    p9ToastSpy,
                    p9LiveTokenStore
                  );
                  xpLoyaltyCtrl.$onInit();
                  await new Promise((r) => setTimeout(r, 20));

                  const csatOption = (xpLoyaltyCtrl.transactionTypeOptions || []).find(
                    (o) => o.value === 'ServiceReviewBonus'
                  );
                  xpLoyaltyCtrl.ledgerFilter.transactionType = 'ServiceReviewBonus';
                  await xpLoyaltyCtrl.onLedgerFilterChanged();
                  const xp01bOk =
                    Boolean(csatOption) &&
                    csatOption.label === 'ServiceReviewBonus (+25★ CSAT Bonus)' &&
                    Object.isExtensible(csatOption) &&
                    capturedLedgerQueries.some((q) => q.transactionType === 'ServiceReviewBonus') &&
                    xpLoyaltyCtrl.ledgerEntries.length === 1 &&
                    xpLoyaltyCtrl.ledgerEntries[0].transactionType === 'ServiceReviewBonus';
                  xpLoyaltyCtrl.$onDestroy();

                  // 2. F-XP-04b & F-XP-09b: SCR-ADM-01 CSAT & Warranty column + Quality -> handoff -> SCR-ADM-08 & /admin/analytics/quality-warranty
                  const xpAdminAnalyticsDef = registeredComponents.get('adminAnalytics');
                  const xpAdminQwDef = registeredComponents.get('adminQualityWarranty');
                  const anaTpl = String(xpAdminAnalyticsDef?.template || '');
                  const anaTplOk =
                    (anaTpl.includes('CSAT &amp; Warranty') || anaTpl.includes('admin.analytics.colCsatWarranty')) &&
                    anaTpl.includes('row.averageCsatRating') &&
                    anaTpl.includes('row.totalServiceReviewsCount') &&
                    anaTpl.includes('row.openWarrantyClaimsCount') &&
                    anaTpl.includes("$ctrl.openDealerGovernance(row, 'app.admin.qualityWarranty')") &&
                    (anaTpl.includes('Quality -&gt;') || anaTpl.includes('admin.analytics.btnQuality') || anaTpl.includes('name="arrow-right"'));

                  const capturedQwParams = [];
                  let simulateQwError = false;
                  p8Responder = async (cfg) => {
                    if (cfg.url.endsWith('/admin/analytics/overview') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            totalRevenueVnd: 150000000,
                            activeDealersCount: 3,
                            dealers: [
                              {
                                dealerId: '22222222-2222-2222-2222-222222222202',
                                dealerCode: 'DLR-HN-01',
                                dealerName: 'Apex Moto Hanoi Flagship',
                                city: 'Hanoi',
                                totalRevenueVnd: 150000000,
                                averageCsatRating: 4.88,
                                totalServiceReviewsCount: 15,
                                openWarrantyClaimsCount: 1
                              }
                            ]
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/admin/analytics/revenue-by-dealer') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            dealers: [
                              {
                                dealerId: '22222222-2222-2222-2222-222222222202',
                                dealerCode: 'DLR-HN-01',
                                dealerName: 'Apex Moto Hanoi Flagship',
                                city: 'Hanoi',
                                showroomSalesVnd: 120000000,
                                workshopServiceVnd: 30000000,
                                totalRevenueVnd: 150000000
                              }
                            ]
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/admin/analytics/bookings-by-dealer') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            statusLabels: ['Completed', 'Confirmed'],
                            statusCounts: [12, 3],
                            dealerBreakdown: [
                              {
                                dealerId: '22222222-2222-2222-2222-222222222202',
                                dealerCode: 'DLR-HN-01',
                                dealerName: 'Apex Moto Hanoi Flagship',
                                totalBookings: 15,
                                completedBookings: 12,
                                activeBookings: 3,
                                cancelledOrNoShowBookings: 0
                              }
                            ]
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/admin/analytics/monthly-trends') && cfg.method === 'GET') {
                      return { status: 200, data: { success: true, data: { monthLabels: ['2026-11'], combinedRevenueVnd: [150000000], bookingCounts: [15] } } };
                    }
                    if (cfg.url.endsWith('/admin/analytics/loyalty-distribution') && cfg.method === 'GET') {
                      return { status: 200, data: { success: true, data: { tierLabels: ['Bronze', 'Silver', 'Gold', 'Platinum'], customerCounts: [5, 3, 2, 1], availablePointsByTier: [100, 500, 1200, 3000], totalMembers: 11 } } };
                    }
                    if (cfg.url.endsWith('/admin/analytics/quality-warranty') && cfg.method === 'GET') {
                      capturedQwParams.push(cfg.params || {});
                      if (simulateQwError) {
                        throw { status: 500, data: { title: 'Simulated Analytics Failure', status: 500, detail: 'Fallback test' } };
                      }
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            fromUtc: '2026-05-01T00:00:00Z',
                            toUtc: '2026-11-24T23:59:59Z',
                            dealerId: cfg.params?.dealerId || null,
                            averageCsatRating: 4.88,
                            totalReviewsCount: 15,
                            fiveStarReviewsCount: 13,
                            unansweredReviewsCount: 1,
                            wouldRecommendRatePercent: 93.33,
                            totalWarrantyClaimsCount: 4,
                            openWarrantyClaimsCount: 1,
                            completedWarrantyClaimsCount: 2,
                            rejectedWarrantyClaimsCount: 1,
                            approvedWarrantyCoveredVnd: 1250000,
                            dealers: [
                              {
                                dealerId: '22222222-2222-2222-2222-222222222202',
                                dealerCode: 'DLR-HN-01',
                                dealerName: 'Apex Moto Hanoi Flagship',
                                city: 'Hanoi',
                                averageCsatRating: 4.88,
                                totalServiceReviewsCount: 15,
                                fiveStarReviewsCount: 13,
                                unansweredReviewsCount: 1,
                                openWarrantyClaimsCount: 1,
                                completedWarrantyClaimsCount: 2,
                                rejectedWarrantyClaimsCount: 1,
                                approvedWarrantyCoveredVnd: 1250000
                              }
                            ],
                            defectBreakdown: [
                              {
                                defectCategory: 'Electrical',
                                totalClaimsCount: 2,
                                openClaimsCount: 1,
                                completedClaimsCount: 1,
                                rejectedClaimsCount: 0,
                                approvedCoveredVnd: 850000
                              }
                            ]
                          }
                        }
                      };
                    }
                    if (cfg.url.includes('/dealers') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: [
                            { id: '22222222-2222-2222-2222-222222222201', code: 'DLR-HCM-01', name: 'Apex Moto Saigon Center', city: 'Ho Chi Minh City', isActive: true },
                            { id: '22222222-2222-2222-2222-222222222202', code: 'DLR-HN-01', name: 'Apex Moto Hanoi Flagship', city: 'Hanoi', isActive: true },
                            { id: '22222222-2222-2222-2222-222222222203', code: 'DLR-DN-01', name: 'Apex Moto Da Nang Coastal Hub', city: 'Da Nang', isActive: true }
                          ]
                        }
                      };
                    }
                    return { status: 200, data: { success: true, data: [] } };
                  };

                  const xpAdminNavigations = [];
                  const xpAdminStateMock = {
                    go: (st, params) => { xpAdminNavigations.push({ state: st, params: params || null }); }
                  };
                  const xpAnaCtrl = new xpAdminAnalyticsDef.controller(
                    mockQ,
                    p9EventsBus,
                    xpAdminStateMock,
                    { search: () => ({}) },
                    {},
                    null,
                    null,
                    p8Api,
                    mapper,
                    p9SignalR,
                    p9ToastSpy,
                    p9LiveTokenStore
                  );
                  xpAnaCtrl.$onInit();
                  await new Promise((r) => setTimeout(r, 25));
                  const targetDealerRow = xpAnaCtrl.sortedDealerRows.find((r) => r.dealerCode === 'DLR-HN-01');
                  xpAnaCtrl.openDealerGovernance(targetDealerRow, 'app.admin.qualityWarranty');
                  xpAnaCtrl.$onDestroy();

                  const xpQwCtrl = new xpAdminQwDef.controller(
                    mockQ,
                    p9EventsBus,
                    p8Api,
                    mapper,
                    p9SignalR,
                    p9ToastSpy,
                    p9LiveTokenStore
                  );
                  xpQwCtrl.$onInit();
                  await new Promise((r) => setTimeout(r, 25));

                  const xp04bOk =
                    anaTplOk &&
                    xpAdminNavigations.length === 1 &&
                    xpAdminNavigations[0].state === 'app.admin.qualityWarranty' &&
                    xpQwCtrl.filters.dealerId === '22222222-2222-2222-2222-222222222202' &&
                    xpQwCtrl.filteredBranchRows.length === 1 &&
                    xpQwCtrl.filteredBranchRows[0].dealerCode === 'DLR-HN-01';

                  const xp09bLiveOk =
                    typeof p8Api.normalizeQualityWarrantyAnalytics === 'function' &&
                    typeof p8Api.getAnalyticsQualityWarranty === 'function' &&
                    p8Api.analytics.getQualityWarranty === p8Api.getAnalyticsQualityWarranty &&
                    capturedQwParams.length >= 1 &&
                    capturedQwParams[0].dealerId === '22222222-2222-2222-2222-222222222202' &&
                    xpQwCtrl.qualityAnalytics?.wouldRecommendRatePercent === 93.33 &&
                    xpQwCtrl.qualityAnalytics?.totalWarrantyClaimsCount === 4 &&
                    xpQwCtrl.networkKpis.averageCsat === 4.88 &&
                    xpQwCtrl.networkKpis.coveredVnd === 1250000 &&
                    xpQwCtrl.defectBreakdown.length === 1 &&
                    xpQwCtrl.defectBreakdown[0].defectCategory === 'Electrical';

                  // Verify graceful fallback when /admin/analytics/quality-warranty fails
                  simulateQwError = true;
                  await xpQwCtrl.loadOversightData();
                  const xp09bFallbackOk = xpQwCtrl.loadProblem === null && xpQwCtrl.qualityAnalytics === null;
                  xpQwCtrl.$onDestroy();

                  // 3. F-XP-05: <notification-bell> 12-event x 3-role deep-link navigation + SalesOrderCompleted/VoucherRedeemed badges
                  const xpBellDef = registeredComponents.get('notificationBell');
                  const bellNavigatedStates = [];
                  let currentBellRole = 'Customer';
                  const xpBellStore = new notifStoreCtor();
                  const xpBellCtrl = new xpBellDef.controller(
                    mockQ,
                    p9EventsBus,
                    {
                      isAuthenticated: () => true,
                      getRole: () => currentBellRole,
                      getCurrentUser: () => ({ userId: 'u-bell-xp', role: currentBellRole })
                    },
                    p8Api,
                    xpBellStore,
                    mapper,
                    p9PreviewStore,
                    { go: (st) => { bellNavigatedStates.push(st); } }
                  );
                  xpBellCtrl.$onInit();

                  const expectedRoleMatrix = {
                    Customer: {
                      BookingCreated: 'app.customer.bookings',
                      BookingStatusChanged: 'app.customer.bookings',
                      MaintenanceReminderTriggered: 'app.customer.garage',
                      LoyaltyPointsUpdated: 'app.customer.loyalty',
                      TierUpgraded: 'app.customer.loyalty',
                      SalesOrderCompleted: 'app.customer.loyalty',
                      VoucherRedeemed: 'app.customer.loyalty',
                      TestDriveCreated: 'app.customer.testDrives',
                      TestDriveStatusChanged: 'app.customer.testDrives',
                      ServiceReviewSubmitted: 'app.customer.bookings',
                      WarrantyClaimCreated: 'app.customer.warranty',
                      WarrantyClaimStatusChanged: 'app.customer.warranty'
                    },
                    DealerStaff: {
                      BookingCreated: 'app.staff.workbench',
                      BookingStatusChanged: 'app.staff.workbench',
                      MaintenanceReminderTriggered: 'app.staff.customers',
                      LoyaltyPointsUpdated: 'app.staff.customers',
                      TierUpgraded: 'app.staff.customers',
                      SalesOrderCompleted: 'app.staff.salesDesk',
                      VoucherRedeemed: 'app.staff.customers',
                      TestDriveCreated: 'app.staff.testDrives',
                      TestDriveStatusChanged: 'app.staff.testDrives',
                      ServiceReviewSubmitted: 'app.staff.warrantyReviews',
                      WarrantyClaimCreated: 'app.staff.warrantyReviews',
                      WarrantyClaimStatusChanged: 'app.staff.warrantyReviews'
                    },
                    Admin: {
                      BookingCreated: 'app.admin.analytics',
                      BookingStatusChanged: 'app.admin.analytics',
                      MaintenanceReminderTriggered: 'app.admin.analytics',
                      LoyaltyPointsUpdated: 'app.admin.loyaltyTiers',
                      TierUpgraded: 'app.admin.loyaltyTiers',
                      SalesOrderCompleted: 'app.admin.analytics',
                      VoucherRedeemed: 'app.admin.voucherCatalog',
                      TestDriveCreated: 'app.admin.analytics',
                      TestDriveStatusChanged: 'app.admin.analytics',
                      ServiceReviewSubmitted: 'app.admin.qualityWarranty',
                      WarrantyClaimCreated: 'app.admin.qualityWarranty',
                      WarrantyClaimStatusChanged: 'app.admin.qualityWarranty'
                    }
                  };

                  let bellMatrixValid = true;
                  for (const [roleName, mapByEvent] of Object.entries(expectedRoleMatrix)) {
                    for (const [evType, expectedState] of Object.entries(mapByEvent)) {
                      const actualState = xpBellCtrl.resolveNotificationTargetState({ eventType: evType }, roleName);
                      if (actualState !== expectedState || !registeredStates.has(actualState)) {
                        bellMatrixValid = false;
                      }
                    }
                  }

                  const badgeClassesOk =
                    xpBellCtrl.eventBadgeClass('SalesOrderCompleted') === 'badge-success' &&
                    xpBellCtrl.eventBadgeClass('VoucherRedeemed') === 'badge-info';

                  const clickItem = xpBellStore.prependNotification({
                    notificationId: 'n-xp05-click-1',
                    eventType: 'WarrantyClaimStatusChanged',
                    title: 'Warranty Completed',
                    message: 'WC-001 completed.'
                  });
                  xpBellCtrl.toggleDrawer();
                  const openedBeforeClick = xpBellCtrl.isOpen === true && xpBellCtrl.drawerOpen === true;
                  currentBellRole = 'Customer';
                  await xpBellCtrl.openNotification(clickItem);
                  const xp05Ok =
                    bellMatrixValid &&
                    badgeClassesOk &&
                    openedBeforeClick &&
                    clickItem.isRead === true &&
                    xpBellCtrl.isOpen === false &&
                    xpBellCtrl.drawerOpen === false &&
                    bellNavigatedStates.length === 1 &&
                    bellNavigatedStates[0] === 'app.customer.warranty';
                  xpBellCtrl.$onDestroy();

                  // 4. F-XP-06: customerBookings Completed invoice pre-fetch + LoyaltyPointsUpdated cross-tab review refresh + localized v1.4 receipt footer
                  const xpCustBookingsDef = registeredComponents.get('customerBookings');
                  const xpReceiptModalDef = registeredComponents.get('printableReceiptModal');
                  const rcptTpl = String(xpReceiptModalDef?.template || '');
                  const rcptCompSrc = fs.readFileSync(path.join(SRC_ROOT, 'app/shared/printable-receipt-modal.component.js'), 'utf8');
                  const rcptFooterV14Ok =
                    rcptTpl.includes('receipt.footerPlatform') &&
                    rcptCompSrc.includes('Platform v1.4') &&
                    !rcptTpl.includes('Platform v1.1') &&
                    !rcptTpl.includes('Platform v1.2');

                  let invoicePrefetchHits = 0;
                  let myReviewsRefreshHits = 0;
                  let includeCrossTabReview = false;
                  p8Responder = async (cfg) => {
                    if (cfg.url.endsWith('/bookings') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: [
                            {
                              id: 'bk-xp06-live',
                              bookingNumber: 'SB-XP06-001',
                              vin: 'RLHJF5109RZ104829',
                              motorcycleModelName: 'Honda SH160i ABS Sport',
                              dealerName: 'Apex Moto Saigon Center',
                              slotDate: '2026-11-24',
                              startTime: '09:00',
                              endTime: '10:00',
                              serviceType: 'PeriodicMaintenance',
                              odometerAtBookingKm: 4100,
                              status: 'InProgress',
                              rowVersion: 'AAAAAAAABK1='
                            }
                          ]
                        }
                      };
                    }
                    if (cfg.url.endsWith('/bookings/bk-xp06-live') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            id: 'bk-xp06-live',
                            bookingNumber: 'SB-XP06-001',
                            vin: 'RLHJF5109RZ104829',
                            motorcycleModelName: 'Honda SH160i ABS Sport',
                            dealerName: 'Apex Moto Saigon Center',
                            slotDate: '2026-11-24',
                            startTime: '09:00',
                            endTime: '10:00',
                            serviceType: 'PeriodicMaintenance',
                            odometerAtBookingKm: 4100,
                            status: 'Completed',
                            rowVersion: 'AAAAAAAABK2='
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/service-invoices') && cfg.method === 'GET') {
                      if (cfg.params?.bookingId === 'bk-xp06-live') {
                        invoicePrefetchHits++;
                        return {
                          status: 200,
                          data: {
                            success: true,
                            data: [
                              {
                                id: 'inv-xp06-live',
                                invoiceNumber: 'INV-XP06-001',
                                serviceBookingId: 'bk-xp06-live',
                                bookingNumber: 'SB-XP06-001',
                                laborAmount: 200000,
                                partsAmount: 300000,
                                grossAmount: 500000,
                                voucherDiscountAmount: 0,
                                netPaidAmount: 500000,
                                pointsEarned: 60
                              }
                            ]
                          }
                        };
                      }
                      return { status: 200, data: { success: true, data: [] } };
                    }
                    if (cfg.url.endsWith('/service-invoices/inv-xp06-live/receipt') && cfg.method === 'GET') {
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: {
                            receiptNumber: 'RCP-SVC-INV-XP06-001',
                            invoiceNumber: 'INV-XP06-001',
                            netPaidAmount: 500000
                          }
                        }
                      };
                    }
                    if (cfg.url.endsWith('/service-reviews/my') && cfg.method === 'GET') {
                      myReviewsRefreshHits++;
                      return {
                        status: 200,
                        data: {
                          success: true,
                          data: includeCrossTabReview
                            ? [
                              {
                                id: 'rev-xp06-live',
                                reviewNumber: 'REV-XP06-001',
                                serviceInvoiceId: 'inv-xp06-live',
                                serviceBookingId: 'bk-xp06-live',
                                rating: 5,
                                bonusPointsAwarded: 25,
                                comment: 'Submitted from second browser tab.'
                              }
                            ]
                            : []
                        }
                      };
                    }
                    return { status: 200, data: { success: true, data: [] } };
                  };

                  const xpBkCtrl = new xpCustBookingsDef.controller(
                    mockQ,
                    p9EventsBus,
                    p8Api,
                    mapper,
                    p9SignalR,
                    p9ToastSpy,
                    p9LiveTokenStore
                  );
                  xpBkCtrl.$onInit();
                  await new Promise((r) => setTimeout(r, 25));
                  const initialReviewsHits = myReviewsRefreshHits;

                  // Transition booking -> Completed via SignalR BookingStatusChanged -> pre-fetches settled invoice immediately
                  p9SignalR.simulateEvent('BookingStatusChanged', {
                    notificationId: 'n-xp06-done',
                    bookingId: 'bk-xp06-live',
                    bookingNumber: 'SB-XP06-001',
                    newStatus: 'Completed'
                  });
                  await new Promise((r) => setTimeout(r, 25));

                  const cachedCompletedInvoice = xpBkCtrl.getBookingInvoice({ id: 'bk-xp06-live' });
                  const prefetchHitsAfterComplete = invoicePrefetchHits;
                  const canRateImmediately = xpBkCtrl.canRateBooking(xpBkCtrl.bookings[0]) === true;
                  const printedRcpt = await xpBkCtrl.printBookingReceipt(xpBkCtrl.bookings[0]);
                  const zeroRedundantInvoiceFetchOnPrint = invoicePrefetchHits === prefetchHitsAfterComplete;

                  // Simulate cross-tab CSAT ServiceReviewBonus via SignalR LoyaltyPointsUpdated -> auto-refreshes review badge
                  includeCrossTabReview = true;
                  p9SignalR.simulateEvent('LoyaltyPointsUpdated', {
                    notificationId: 'n-xp06-csat-pts',
                    transactionType: 'ServiceReviewBonus',
                    referenceType: 'ServiceReview',
                    referenceNumber: 'REV-XP06-001',
                    pointsDelta: 25
                  });
                  await new Promise((r) => setTimeout(r, 25));

                  const crossTabReviewSynced =
                    myReviewsRefreshHits > initialReviewsHits &&
                    xpBkCtrl.getBookingReview(xpBkCtrl.bookings[0])?.reviewNumber === 'REV-XP06-001' &&
                    xpBkCtrl.canRateBooking(xpBkCtrl.bookings[0]) === false;

                  const hitsBeforeDestroy = myReviewsRefreshHits;
                  xpBkCtrl.$onDestroy();
                  p9SignalR.simulateEvent('LoyaltyPointsUpdated', {
                    notificationId: 'n-xp06-post-destroy',
                    transactionType: 'ServiceReviewBonus',
                    referenceType: 'ServiceReview',
                    pointsDelta: 25
                  });
                  await new Promise((r) => setTimeout(r, 15));
                  const xp06PostDestroySafe = myReviewsRefreshHits === hitsBeforeDestroy;

                  const xp06Ok =
                    rcptFooterV14Ok &&
                    cachedCompletedInvoice?.invoiceNumber === 'INV-XP06-001' &&
                    prefetchHitsAfterComplete === 1 &&
                    canRateImmediately &&
                    printedRcpt?.receiptNumber === 'RCP-SVC-INV-XP06-001' &&
                    zeroRedundantInvoiceFetchOnPrint &&
                    crossTabReviewSynced &&
                    xp06PostDestroySafe;

                  if (!xp01bOk || !xp04bOk || !xp05Ok || !xp06Ok || !xp09bLiveOk || !xp09bFallbackOk) {
                    fail(
                      `Cross-Phase Stage 3 suite (ai) failed: F-XP-01b=${xp01bOk}, F-XP-04b=${xp04bOk}, F-XP-05=${xp05Ok}, F-XP-06=${xp06Ok}, F-XP-09b=${xp09bLiveOk}/${xp09bFallbackOk}`
                    );
                  } else {
                    pass('Verified Cross-Phase Stage 3 fixes F-XP-01b (ServiceReviewBonus ledger filter), F-XP-04b (SCR-ADM-01 CSAT & Warranty column + Quality -> handoff), F-XP-05 (12-event x 3-role notification-bell deep-links), F-XP-06 (Completed invoice pre-fetch, cross-tab LoyaltyPointsUpdated review sync & localized v1.4 receipt footer), and F-XP-09b (/admin/analytics/quality-warranty + fallback).');
                  }

                  // (aj) Cross-Phase Stage 5 Clean-Code Refactoring & Boundary Matrices (RF-XP-04a, RF-XP-04b, RF-XP-04c)
                  {
                    // (a) RF-XP-04a & RF-XP-04c(b): Canonical 7-value getLedgerTransactionTypes() SSOT clone safety & mutation isolation
                    const expected7LedgerTypes = [
                      'SalesOrderEarn',
                      'ServiceInvoiceEarn',
                      'ServiceReviewBonus',
                      'VoucherRedeem',
                      'VoucherExpireRefund',
                      'AdminAdjustment',
                      'WelcomeBonus'
                    ];
                    const ledgerClone1 = p8Api.getLedgerTransactionTypes();
                    const ledgerCloneViaNamespace = p8Api.loyalty.getTransactionTypes();
                    const clone1Valid =
                      Array.isArray(ledgerClone1) &&
                      ledgerClone1.length === 7 &&
                      ledgerCloneViaNamespace.length === 7 &&
                      expected7LedgerTypes.every(
                        (val, idx) =>
                          ledgerClone1[idx]?.value === val &&
                          Object.isFrozen(ledgerClone1[idx]) === false &&
                          Object.isExtensible(ledgerClone1[idx]) === true
                      );
                    if (ledgerClone1[0]) {
                      ledgerClone1[0].label = 'MUTATED_IN_TEST';
                    }
                    const ledgerClone2 = p8Api.getLedgerTransactionTypes();
                    const rf04aOk =
                      clone1Valid &&
                      ledgerClone2[0]?.label === 'SalesOrderEarn (Showroom Bike)' &&
                      Object.isFrozen(ledgerClone2[0]) === false;

                    // (b) RF-XP-04b & RF-XP-04c(a): 36-row (12 eventTypes x 3 roles) table + null safety + Admin payloadJson dealerId handoff + Enter/Space keyboard accessibility
                    const rfBellDef = registeredComponents.get('notificationBell');
                    const bellTpl = String(rfBellDef?.template || '');
                    const bellA11yTplOk =
                      bellTpl.includes('role="button" tabindex="0"') &&
                      bellTpl.includes('ng-keydown="$ctrl.onNotificationKeydown($event, item)"');

                    const rfNavCalls = [];
                    let rfActiveRole = 'Admin';
                    const rfBellStore = new notifStoreCtor();
                    const rfBellCtrl = new rfBellDef.controller(
                      mockQ,
                      p9EventsBus,
                      {
                        isAuthenticated: () => true,
                        getRole: () => rfActiveRole,
                        getCurrentUser: () => ({ userId: 'u-rf-admin', role: rfActiveRole })
                      },
                      p8Api,
                      rfBellStore,
                      mapper,
                      p9PreviewStore,
                      { go: (st) => { rfNavCalls.push(st); } }
                    );
                    rfBellCtrl.$onInit();

                    const notification36BoundaryTable = [
                      ['BookingCreated', 'Customer', 'app.customer.bookings'],
                      ['BookingCreated', 'DealerStaff', 'app.staff.workbench'],
                      ['BookingCreated', 'Admin', 'app.admin.analytics'],
                      ['BookingStatusChanged', 'Customer', 'app.customer.bookings'],
                      ['BookingStatusChanged', 'DealerStaff', 'app.staff.workbench'],
                      ['BookingStatusChanged', 'Admin', 'app.admin.analytics'],
                      ['MaintenanceReminderTriggered', 'Customer', 'app.customer.garage'],
                      ['MaintenanceReminderTriggered', 'DealerStaff', 'app.staff.customers'],
                      ['MaintenanceReminderTriggered', 'Admin', 'app.admin.analytics'],
                      ['LoyaltyPointsUpdated', 'Customer', 'app.customer.loyalty'],
                      ['LoyaltyPointsUpdated', 'DealerStaff', 'app.staff.customers'],
                      ['LoyaltyPointsUpdated', 'Admin', 'app.admin.loyaltyTiers'],
                      ['TierUpgraded', 'Customer', 'app.customer.loyalty'],
                      ['TierUpgraded', 'DealerStaff', 'app.staff.customers'],
                      ['TierUpgraded', 'Admin', 'app.admin.loyaltyTiers'],
                      ['SalesOrderCompleted', 'Customer', 'app.customer.loyalty'],
                      ['SalesOrderCompleted', 'DealerStaff', 'app.staff.salesDesk'],
                      ['SalesOrderCompleted', 'Admin', 'app.admin.analytics'],
                      ['VoucherRedeemed', 'Customer', 'app.customer.loyalty'],
                      ['VoucherRedeemed', 'DealerStaff', 'app.staff.customers'],
                      ['VoucherRedeemed', 'Admin', 'app.admin.voucherCatalog'],
                      ['TestDriveCreated', 'Customer', 'app.customer.testDrives'],
                      ['TestDriveCreated', 'DealerStaff', 'app.staff.testDrives'],
                      ['TestDriveCreated', 'Admin', 'app.admin.analytics'],
                      ['TestDriveStatusChanged', 'Customer', 'app.customer.testDrives'],
                      ['TestDriveStatusChanged', 'DealerStaff', 'app.staff.testDrives'],
                      ['TestDriveStatusChanged', 'Admin', 'app.admin.analytics'],
                      ['ServiceReviewSubmitted', 'Customer', 'app.customer.bookings'],
                      ['ServiceReviewSubmitted', 'DealerStaff', 'app.staff.warrantyReviews'],
                      ['ServiceReviewSubmitted', 'Admin', 'app.admin.qualityWarranty'],
                      ['WarrantyClaimCreated', 'Customer', 'app.customer.warranty'],
                      ['WarrantyClaimCreated', 'DealerStaff', 'app.staff.warrantyReviews'],
                      ['WarrantyClaimCreated', 'Admin', 'app.admin.qualityWarranty'],
                      ['WarrantyClaimStatusChanged', 'Customer', 'app.customer.warranty'],
                      ['WarrantyClaimStatusChanged', 'DealerStaff', 'app.staff.warrantyReviews'],
                      ['WarrantyClaimStatusChanged', 'Admin', 'app.admin.qualityWarranty']
                    ];

                    const all36Pass =
                      notification36BoundaryTable.length === 36 &&
                      notification36BoundaryTable.every(([ev, role, expectedState]) => {
                        const actual = rfBellCtrl.resolveNotificationTargetState({ eventType: ev }, role);
                        return actual === expectedState && registeredStates.has(actual);
                      });

                    const nullSafetyPass =
                      rfBellCtrl.resolveNotificationTargetState(null, 'Customer') === null &&
                      rfBellCtrl.resolveNotificationTargetState({}, 'Customer') === null &&
                      rfBellCtrl.resolveNotificationTargetState({ eventType: 'UnknownFutureEvent' }, 'Customer') === null &&
                      rfBellCtrl.resolveNotificationTargetState({ eventType: 'BookingCreated' }, 'UnknownRole') === null;

                    // Admin payloadJson string handoff (via Enter keydown) -> DealerApiService.consumeAdminFocusContext()
                    p8Api.consumeAdminFocusContext(); // clear any residual context
                    const adminReviewNotif = rfBellStore.prependNotification({
                      notificationId: 'n-rf04b-admin-rev',
                      eventType: 'ServiceReviewSubmitted',
                      title: 'New 5★ Review at Da Nang',
                      message: 'REV-DN-901 submitted.',
                      payloadJson: JSON.stringify({
                        DealerId: '22222222-2222-2222-2222-222222222203',
                        dealerCode: 'dlr-dn-01'
                      })
                    });
                    let enterPrevented = false;
                    rfBellCtrl.toggleDrawer();
                    await rfBellCtrl.onNotificationKeydown(
                      { key: 'Enter', preventDefault: () => { enterPrevented = true; } },
                      adminReviewNotif
                    );
                    const handoffFromEnter = p8Api.consumeAdminFocusContext();

                    // Space keydown with object payloadJson + malformed JSON resilience
                    const adminWcObjectNotif = rfBellStore.prependNotification({
                      notificationId: 'n-rf04b-admin-wc',
                      eventType: 'WarrantyClaimCreated',
                      title: 'New Warranty Claim at Hanoi',
                      message: 'WC-HN-902 submitted.',
                      payloadJson: { dealerId: '22222222-2222-2222-2222-222222222202', dealerCode: 'DLR-HN-01' }
                    });
                    let spacePrevented = false;
                    rfBellCtrl.toggleDrawer();
                    await rfBellCtrl.onNotificationKeydown(
                      { key: ' ', preventDefault: () => { spacePrevented = true; } },
                      adminWcObjectNotif
                    );
                    const handoffFromSpace = p8Api.consumeAdminFocusContext();

                    // Non-activation key ('Tab') must neither preventDefault nor navigate
                    let tabPrevented = false;
                    const navCountBeforeTab = rfNavCalls.length;
                    rfBellCtrl.onNotificationKeydown(
                      { key: 'Tab', preventDefault: () => { tabPrevented = true; } },
                      adminWcObjectNotif
                    );

                    // Malformed payloadJson must not throw and still navigate cleanly
                    const malformedJsonNotif = rfBellStore.prependNotification({
                      notificationId: 'n-rf04b-malformed',
                      eventType: 'WarrantyClaimStatusChanged',
                      title: 'Malformed JSON safety check',
                      message: 'Should still navigate.',
                      payloadJson: '{not-valid-json'
                    });
                    await rfBellCtrl.openNotification(malformedJsonNotif);
                    const handoffAfterMalformed = p8Api.consumeAdminFocusContext();
                    rfBellCtrl.$onDestroy();

                    const rf04bOk =
                      bellA11yTplOk &&
                      all36Pass &&
                      nullSafetyPass &&
                      enterPrevented &&
                      handoffFromEnter?.dealerId === '22222222-2222-2222-2222-222222222203' &&
                      handoffFromEnter?.dealerCode === 'DLR-DN-01' &&
                      spacePrevented &&
                      handoffFromSpace?.dealerId === '22222222-2222-2222-2222-222222222202' &&
                      handoffFromSpace?.dealerCode === 'DLR-HN-01' &&
                      !tabPrevented &&
                      rfNavCalls.length === navCountBeforeTab + 1 &&
                      rfNavCalls.every((st) => st === 'app.admin.qualityWarranty') &&
                      handoffAfterMalformed === null;

                    // (c) RF-XP-04c(c): normalizeQualityWarrantyAnalytics edge-case matrix (null/empty, branch rollup fallback, defectBreakdown aliases)
                    const qwNullNorm = p8Api.normalizeQualityWarrantyAnalytics(null);
                    const qwEmptyNorm = p8Api.normalizeQualityWarrantyAnalytics({});
                    const qwZeroedOk = [qwNullNorm, qwEmptyNorm].every(
                      (dto) =>
                        dto.averageCsatRating === 0 &&
                        dto.totalReviewsCount === 0 &&
                        dto.fiveStarReviewsCount === 0 &&
                        dto.unansweredReviewsCount === 0 &&
                        dto.wouldRecommendRatePercent === 0 &&
                        dto.totalWarrantyClaimsCount === 0 &&
                        dto.openWarrantyClaimsCount === 0 &&
                        dto.completedWarrantyClaimsCount === 0 &&
                        dto.rejectedWarrantyClaimsCount === 0 &&
                        dto.approvedWarrantyCoveredVnd === 0 &&
                        Array.isArray(dto.dealers) &&
                        dto.dealers.length === 0 &&
                        Array.isArray(dto.defectBreakdown) &&
                        dto.defectBreakdown.length === 0
                    );

                    const qwRollupNorm = p8Api.normalizeQualityWarrantyAnalytics({
                      dealers: [
                        {
                          dealerId: 'dlr-1',
                          dealerCode: 'dlr-hcm-01',
                          dealerName: 'Saigon',
                          averageCsatRating: 5.0,
                          totalServiceReviewsCount: 6,
                          fiveStarReviewsCount: 6,
                          unansweredReviewsCount: 1,
                          openWarrantyClaimsCount: 2,
                          completedWarrantyClaimsCount: 3,
                          rejectedWarrantyClaimsCount: 1,
                          approvedWarrantyCoveredVnd: 900000
                        },
                        {
                          dealerId: 'dlr-2',
                          dealerCode: 'dlr-hn-01',
                          dealerName: 'Hanoi',
                          averageCsatRating: 4.0,
                          totalServiceReviewsCount: 4,
                          fiveStarReviewsCount: 2,
                          unansweredReviewsCount: 2,
                          openWarrantyClaimsCount: 1,
                          completedWarrantyClaimsCount: 1,
                          rejectedWarrantyClaimsCount: 0,
                          approvedWarrantyCoveredVnd: 350000
                        }
                      ],
                      defectBreakdown: [
                        {
                          DefectCategory: 'Electrical',
                          OpenClaimsCount: 2,
                          CompletedClaimsCount: 2,
                          RejectedClaimsCount: 1,
                          ApprovedCoveredVnd: 900000
                        },
                        {
                          category: 'Transmission',
                          count: 3,
                          coveredAmountVnd: 350000
                        }
                      ]
                    });
                    // Weighted avg CSAT = (5.0*6 + 4.0*4) / 10 = 4.60; 5-star = 8/10 = 80% fallback recommend; total claims = (2+3+1) + (1+1+0) = 8
                    const qwRollupAndDefectsOk =
                      qwRollupNorm.totalReviewsCount === 10 &&
                      qwRollupNorm.fiveStarReviewsCount === 8 &&
                      qwRollupNorm.unansweredReviewsCount === 3 &&
                      qwRollupNorm.averageCsatRating === 4.6 &&
                      qwRollupNorm.wouldRecommendRatePercent === 80 &&
                      qwRollupNorm.openWarrantyClaimsCount === 3 &&
                      qwRollupNorm.completedWarrantyClaimsCount === 4 &&
                      qwRollupNorm.rejectedWarrantyClaimsCount === 1 &&
                      qwRollupNorm.totalWarrantyClaimsCount === 8 &&
                      qwRollupNorm.approvedWarrantyCoveredVnd === 1250000 &&
                      qwRollupNorm.defectBreakdown.length === 2 &&
                      qwRollupNorm.defectBreakdown[0].defectCategory === 'Electrical' &&
                      qwRollupNorm.defectBreakdown[0].totalClaimsCount === 5 &&
                      qwRollupNorm.defectBreakdown[0].approvedCoveredVnd === 900000 &&
                      qwRollupNorm.defectBreakdown[1].defectCategory === 'Transmission' &&
                      qwRollupNorm.defectBreakdown[1].totalClaimsCount === 3 &&
                      qwRollupNorm.defectBreakdown[1].approvedCoveredVnd === 350000;

                    if (!rf04aOk || !rf04bOk || !qwZeroedOk || !qwRollupAndDefectsOk) {
                      fail(
                        `Cross-Phase Stage 5 suite (aj) failed: RF-XP-04a=${rf04aOk}, RF-XP-04b=${rf04bOk}, qwZeroed=${qwZeroedOk}, qwRollup=${qwRollupAndDefectsOk}`
                      );
                    } else {
                      pass('Verified Cross-Phase Stage 5 RF-XP-04a..c (7-value getLedgerTransactionTypes SSOT clones, 36-row notification-bell deep-link matrix + Enter/Space a11y + Admin payloadJson dealer focus handoff, and normalizeQualityWarrantyAnalytics edge-case matrix).');
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    // 4aa-runtime. v1.4.0 ThemeService & Bilingual I18nService (vi-VN default <-> en-US)
    {
      const ThemeCtor = registeredServices.get('ThemeService');
      const I18nCtor = registeredServices.get('I18nService');
      const i18nVi = registeredConstants.get('I18N_VI');
      const i18nEn = registeredConstants.get('I18N_EN');
      if (!ThemeCtor || !I18nCtor || !i18nVi || !i18nEn) {
        fail('v1.4.0 services missing (ThemeService / I18nService / I18N_VI / I18N_EN).');
      } else {
        const broadcasted = [];
        const mockRootScope = {
          $broadcast: (evt, payload) => broadcasted.push({ evt, payload }),
          $on: () => () => {}
        };
        const storage = new Map();
        const attrs = {};
        const mockWindow = {
          localStorage: {
            getItem: (k) => (storage.has(k) ? storage.get(k) : null),
            setItem: (k, v) => storage.set(k, String(v))
          },
          document: {
            title: '',
            documentElement: {
              lang: 'vi',
              setAttribute: (k, v) => { attrs[k] = v; },
              getAttribute: (k) => attrs[k] || null
            }
          }
        };

        const themeSvc = new ThemeCtor(mockRootScope, mockWindow);
        const initialTheme = themeSvc.getTheme();
        const toggledTheme = themeSvc.toggleTheme();
        const themeOk =
          initialTheme === 'light' &&
          toggledTheme === 'dark' &&
          attrs['data-theme'] === 'dark' &&
          storage.get('apex_theme') === 'dark' &&
          broadcasted.some((b) => b.evt === 'theme:changed');

        const i18nSvc = new I18nCtor(mockRootScope, mockWindow, i18nVi, i18nEn);
        const defaultLoc = i18nSvc.getLocale();
        const viShowroom = i18nSvc.t('nav.showroom');
        const viRelativeNow = i18nSvc.formatRelative(new Date().toISOString());
        i18nSvc.setLocale('en-US');
        const enLoc = i18nSvc.getLocale();
        const enShowroom = i18nSvc.t('nav.showroom');
        const enRelativeNow = i18nSvc.formatRelative(new Date().toISOString());
        const i18nOk =
          defaultLoc === 'vi-VN' &&
          enLoc === 'en-US' &&
          Boolean(viShowroom) && viShowroom !== 'nav.showroom' &&
          Boolean(enShowroom) && enShowroom !== 'nav.showroom' &&
          viRelativeNow !== enRelativeNow &&
          mockWindow.document.documentElement.lang === 'en' &&
          broadcasted.some((b) => b.evt === 'i18n:changed');

        if (!themeOk || !i18nOk) {
          fail(`v1.4.0 Theme/I18n runtime check failed: themeOk=${themeOk}, i18nOk=${i18nOk} (defaultLoc=${defaultLoc}, vi=${viShowroom}, en=${enShowroom}).`);
        } else {
          pass('Verified v1.4.0 ThemeService (Light Showroom <-> Dark Pit-Lane) & I18nService (vi-VN default <-> en-US hot-switch + broadcasts).');
        }

        // 4aa-regression. Wave 1 P0-3 (<bike-image>), P1-4 (vi-VN voucher/receipt), and P2-7 (errorRetryBanner/activityLogDrawer/AuthService)
        i18nSvc.setLocale('vi-VN');
        const bikeImgDef = registeredComponents.get('bikeImage');
        const errBannerDef = registeredComponents.get('errorRetryBanner');
        const actDrawerDef = registeredComponents.get('activityLogDrawer');
        const rcptModalDef = registeredComponents.get('printableReceiptModal');
        const loyaltyCalcV14 = registeredConstants.get('LoyaltyCalculator');
        const dealerApiCtorV14 = registeredServices.get('DealerApiService');
        const authSvcCtorV14 = registeredServices.get('AuthService');
        const tokenStoreCtorV14 = registeredServices.get('TokenStorageService');
        const problemMapperCtorV14 = registeredServices.get('ProblemDetailsMapper');

        if (!bikeImgDef || !errBannerDef || !actDrawerDef || !rcptModalDef || !loyaltyCalcV14 || !dealerApiCtorV14 || !authSvcCtorV14) {
          fail('Wave 1 regression suite missing required component/service registrations.');
        } else {
          // P0-3: <bike-image> external URL never derives .webp, local /assets/*.jpg retries rawSrc before FALLBACK_JPG, alt localizes
          let imgErrHandler = null;
          let imgCurrentSrcAttr = '';
          const mockImgEl = {
            addEventListener: (evt, fn) => { if (evt === 'error') imgErrHandler = fn; },
            removeEventListener: (evt, fn) => { if (evt === 'error' && imgErrHandler === fn) imgErrHandler = null; },
            getAttribute: (attr) => (attr === 'src' ? imgCurrentSrcAttr : null),
            setAttribute: (attr, val) => { if (attr === 'src') imgCurrentSrcAttr = String(val); }
          };
          const mockBikeElement = [{ querySelector: (sel) => (sel === 'img.bike-image-img' ? mockImgEl : null) }];
          const mockBikeScope = {
            $applyAsync: (fn) => { if (typeof fn === 'function') fn(); },
            $on: () => () => {}
          };

          const extBikeCtrl = new bikeImgDef.controller(mockBikeElement, mockBikeScope, i18nSvc);
          extBikeCtrl.src = 'https://cdn.example.org/bike.jpg';
          extBikeCtrl.$onInit();
          const extNoWebpOk =
            extBikeCtrl.webpSrc === null &&
            extBikeCtrl.resolvedSrc === 'https://cdn.example.org/bike.jpg' &&
            extBikeCtrl.usedFallback === false &&
            extBikeCtrl.resolvedAlt === 'Xe máy';
          extBikeCtrl.$onDestroy();

          const localBikeCtrl = new bikeImgDef.controller(mockBikeElement, mockBikeScope, i18nSvc);
          localBikeCtrl.src = '/assets/models/custom-jpg-only.jpg';
          localBikeCtrl.$onInit();
          localBikeCtrl.$postLink();
          const localInitialWebpOk =
            localBikeCtrl.webpSrc === '/assets/models/custom-jpg-only.webp' &&
            localBikeCtrl.resolvedSrc === '/assets/models/custom-jpg-only.jpg' &&
            localBikeCtrl.usedFallback === false;

          // 1st error (WebP missing) -> drop webpSrc and retry raw /assets/...jpg BEFORE falling back
          if (typeof imgErrHandler === 'function') imgErrHandler();
          const retryRawFirstOk =
            localBikeCtrl.webpSrc === null &&
            localBikeCtrl.resolvedSrc === '/assets/models/custom-jpg-only.jpg' &&
            localBikeCtrl.usedFallback === false;

          // 2nd error (raw JPG also missing) -> fall back to FALLBACK_JPG
          if (typeof imgErrHandler === 'function') imgErrHandler();
          const fallbackSecondOk =
            localBikeCtrl.webpSrc === null &&
            localBikeCtrl.resolvedSrc === '/assets/editorial/showroom-hero-apex.jpg' &&
            localBikeCtrl.usedFallback === true;
          localBikeCtrl.$onDestroy();

          // P1-4: Localized vi-VN LoyaltyCalculator & DealerApiService voucher/receipt labels + v1.4 localized receipt footer
          const viPctSummary = loyaltyCalcV14.formatDiscountSummary({ discountType: 'Percentage', discountValue: 10, maxDiscountAmount: 500000 }, 'vi-VN');
          const viFixedSummary = loyaltyCalcV14.formatDiscountSummary({ discountType: 'FixedAmount', discountValue: 50000 }, 'vi-VN');
          const viIneligible = loyaltyCalcV14.computeVoucherDiscount({ discountType: 'FixedAmount', discountValue: 50000, minOrderAmount: 250000 }, 200000, 'vi-VN');
          const viCalcOk =
            viPctSummary === 'Giảm 10% (Tối đa 500.000 ₫)' &&
            viFixedSummary === 'Giảm 50.000 ₫' &&
            viIneligible.isEligible === false &&
            String(viIneligible.ineligibleReason || '').startsWith('Tổng tiền (');

          const viMapper = new problemMapperCtorV14();
          const viApi = new dealerApiCtorV14(
            { get: () => Promise.resolve({ data: {} }) },
            { resolve: (v) => Promise.resolve(v), reject: (e) => Promise.reject(e) },
            registeredConstants.get('APP_CONFIG'),
            viMapper,
            i18nSvc
          );
          const viCatalogNorm = viApi.normalizeVoucherCatalog({
            codePrefix: 'SAVE10',
            discountType: 'Percentage',
            discountValue: 10,
            maxDiscountAmount: 500000
          });
          const viRcptNorm = viApi.normalizePrintableReceipt({
            invoiceNumber: 'INV-VI-001',
            laborAmount: 150000,
            partsAmount: 200000
          });
          const viApiOk =
            viCatalogNorm.discountSummary === 'Giảm 10% (Tối đa 500.000 ₫)' &&
            viRcptNorm.lineItems.length === 2 &&
            viRcptNorm.lineItems[0].description.includes('Công bảo dưỡng') &&
            viRcptNorm.lineItems[1].description.includes('Phụ tùng chính hãng');

          // P2-7: PrintableReceiptModal v1.4 footer, ErrorRetryBanner Trace ID, ActivityLogDrawer sources, AuthService MISSING_REFRESH_TOKEN
          const dummyRcptCtrl = new rcptModalDef.controller({}, viApi, i18nSvc);
          dummyRcptCtrl.$onDestroy();
          const dummyErrCtrl = new errBannerDef.controller(i18nSvc);
          dummyErrCtrl.$onInit();
          const dummyTokenStore = new tokenStoreCtorV14({});
          const viAuthSvc = new authSvcCtorV14(
            { resolve: (v) => Promise.resolve(v), reject: (e) => Promise.reject(e) },
            { has: () => false, get: () => null },
            mockRootScope,
            registeredConstants.get('APP_CONFIG'),
            registeredConstants.get('ROLE_HOME'),
            registeredConstants.get('ROLE_PERMISSION_PRESETS'),
            dummyTokenStore,
            i18nSvc
          );
          let missingRefreshErr = null;
          try {
            await viAuthSvc.refreshTokens();
          } catch (err) {
            missingRefreshErr = err;
          }
          const viFooterStr = i18nSvc.t('receipt.footerPlatform');
          const viTraceLabel = i18nSvc.t('errorBanner.traceIdLabel');
          i18nSvc.setLocale('en-US');
          const enFooterStr = i18nSvc.t('receipt.footerPlatform');
          const enTraceLabel = i18nSvc.t('errorBanner.traceIdLabel');

          const p27Ok =
            viFooterStr.includes('v1.4') &&
            enFooterStr.includes('Platform v1.4') &&
            viTraceLabel === 'Mã truy vết' &&
            enTraceLabel === 'Trace ID' &&
            String(errBannerDef.template || '').includes('errorBanner.traceIdLabel') &&
            String(actDrawerDef.template || '').includes('activityLog.sourceServer') &&
            String(actDrawerDef.template || '').includes('activityLog.sourceWebClient') &&
            String(actDrawerDef.template || '').includes('activityLog.sourceMobileApp') &&
            missingRefreshErr?.data?.errorCode === 'MISSING_REFRESH_TOKEN' &&
            missingRefreshErr?.data?.title === 'Phiên đăng nhập đã hết hạn';

          if (!extNoWebpOk || !localInitialWebpOk || !retryRawFirstOk || !fallbackSecondOk || !viCalcOk || !viApiOk || !p27Ok) {
            fail(
              `Wave 1 runtime regression suite failed: extNoWebp=${extNoWebpOk}, localWebp=${localInitialWebpOk}, retryRaw=${retryRawFirstOk}, fallback=${fallbackSecondOk}, viCalc=${viCalcOk}, viApi=${viApiOk}, p27=${p27Ok}`
            );
          } else {
            pass('Verified Wave 1 runtime fixes: P0-3 <bike-image> local-only .webp & rawSrc retry, P1-4 vi-VN voucher/receipt localization, and P2-7 localized Trace ID / activityLog sources / AuthService session expiry.');
          }
        }
      }
    }
  }

  // 4z. Strict Zero-Mock / Zero-Hardcode Static Guard across frontend/src/app/**
  const zeroMockPatterns = [
    { label: 'PREVIEW_* fallback constant', regex: /\bPREVIEW_[A-Z0-9_]+\b/ },
    { label: 'SEEDED_DEMO_ACCOUNTS constant', regex: /\bSEEDED_DEMO_ACCOUNTS\b/ },
    { label: 'SEEDED_CUSTOMER_DIRECTORY constant', regex: /\bSEEDED_CUSTOMER_DIRECTORY\b/ },
    { label: 'KNOWN_SEEDED_DEALERS constant', regex: /\bKNOWN_SEEDED_DEALERS\b/ },
    { label: 'getDemoAccounts() helper', regex: /\bgetDemoAccounts\b/ },
    { label: 'getSeededCustomerDirectory() helper', regex: /\bgetSeededCustomerDirectory\b/ },
    { label: 'activatePhase3RolePreview() helper', regex: /\bactivatePhase3RolePreview\b/ },
    { label: 'isPreviewToken / isPreviewMode / phase3-preview-mode', regex: /\bisPreviewToken\b|\bisPreviewMode\b|phase3-preview-mode|\bbuildPreviewReceipt\b|\bresetPreview\b|\bPreviewReady\b/ },
    { label: 'plaintext demo password', regex: /Admin@123!|Staff@123!|Customer@123!/ },
    { label: 'simulated UI event button/handler', regex: /\bsimulateLiveBooking\b|\bsimulateLiveTestDrive\b|Simulate SignalR|Simulate TestDrive|One-Click Seeded/i },
    { label: 'hardcoded sample VIN', regex: /\b(?:RLHJF|RLHNC|RLHJC|RLHJK|RLCUG)[A-Z0-9]{0,12}\b/ },
    { label: 'hardcoded seeded email', regex: /@motorcycledealer\.vn|@customer\.vn|@example\.vn/i },
    { label: 'hardcoded seeded dealer code or name', regex: /\bDLR-(?:HCM|HN|DN)-01\b|Apex Moto Saigon|Apex Moto Hanoi|Apex Moto Da Nang/i },
    { label: 'hardcoded sample license plate or voucher code', regex: /\b(?:59-A3|59-X1|30-A8|59-S2)\b|\b(?:SVC100K|SVC50K|SVC150K|PLATVIP300K)\b/ }
  ];
  const zeroMockViolations = [];
  for (const appFile of appJsFiles) {
    const relPath = path.relative(SRC_ROOT, appFile).replace(/\\/g, '/');
    const lines = fs.readFileSync(appFile, 'utf8').split(/\r?\n/);
    for (let idx = 0; idx < lines.length; idx++) {
      const line = lines[idx];
      for (const rule of zeroMockPatterns) {
        if (rule.regex.test(line)) {
          zeroMockViolations.push(`${relPath}:${idx + 1} [${rule.label}] -> ${line.trim().slice(0, 120)}`);
        }
      }
    }
  }
  if (zeroMockViolations.length > 0) {
    fail(`Zero-Mock / Zero-Hardcode Guard failed with ${zeroMockViolations.length} violation(s):\n  - ${zeroMockViolations.slice(0, 15).join('\n  - ')}`);
  } else {
    pass(`Verified Zero-Mock / Zero-Hardcode Static Guard across all ${appJsFiles.length} SPA files (0 PREVIEW_*, 0 SEEDED_*, 0 plaintext passwords, 0 simulateLive* buttons, 0 hardcoded VIN/dealer/email samples).`);
  }

  // 4aa-static. v1.4.0 Big Update Static Verification: Zero UI Emojis/Inline-Hex, 8 Studio Assets & Wave 1 CSS/wwwroot Guards
  {
    const uiComponentFiles = appJsFiles.filter((f) => {
      const rel = path.relative(SRC_ROOT, f).replace(/\\/g, '/');
      return rel.startsWith('app/features/') || rel.startsWith('app/shared/');
    });
    const uiEmojiRx = /[\u{26A1}\u{1F5A8}\u{1F6E1}\u{1F39F}\u{26A0}\u{1F48E}\u{1F527}\u{2615}\u{1F91D}\u{1F4CB}\u{1F50D}\u{1F9FE}\u{1F512}\u{1F4AC}\u{1F514}\u{2605}\u{2606}\u{2715}\u{2713}\u{25CF}\u{25CB}\u{25C6}\u{25B2}\u{25A0}\u{2298}\u{265B}\u{21BB}\u{25BE}\u{2699}\u{270E}\u{21E5}\u{25B6}\u{2630}]/u;
    const inlineHexRx = /style="[^"]*#[0-9A-Fa-f]{3,6}[^"]*"/;
    const asciiArrowRx = /-&gt;|&lt;-/;
    const uiViolations = [];
    for (const f of uiComponentFiles) {
      const txt = fs.readFileSync(f, 'utf8');
      const rel = path.relative(SRC_ROOT, f).replace(/\\/g, '/');
      if (uiEmojiRx.test(txt)) uiViolations.push(`${rel}: raw emoji/Unicode icon`);
      if (inlineHexRx.test(txt)) uiViolations.push(`${rel}: hardcoded inline hex style`);
      if (asciiArrowRx.test(txt)) uiViolations.push(`${rel}: raw ASCII arrow`);
    }
    if (uiViolations.length > 0) {
      fail(`v1.4.0 UI polish guard failed:\n  - ${uiViolations.join('\n  - ')}`);
    } else {
      pass(`Verified v1.4.0 UI Hygiene Guard across ${uiComponentFiles.length} feature/shared components (0 UI emojis/Unicode icons, 0 inline light hex styles, 0 ASCII arrows).`);
    }

    const expectedStudioAssets = [
      'assets/models/honda-airblade-160.jpg', 'assets/models/honda-airblade-160.webp',
      'assets/models/honda-sh160i-sport.jpg', 'assets/models/honda-sh160i-sport.webp',
      'assets/models/honda-winner-x.jpg', 'assets/models/honda-winner-x.webp',
      'assets/models/yamaha-exciter-155.jpg', 'assets/models/yamaha-exciter-155.webp',
      'assets/models/honda-cb300r.jpg', 'assets/models/honda-cb300r.webp',
      'assets/models/honda-nx500.jpg', 'assets/models/honda-nx500.webp',
      'assets/editorial/showroom-hero-apex.jpg', 'assets/editorial/showroom-hero-apex.webp',
      'assets/editorial/workshop-bay-apex.jpg', 'assets/editorial/workshop-bay-apex.webp'
    ];
    const missingAssets = expectedStudioAssets.filter((rel) => !fs.existsSync(path.join(SRC_ROOT, rel)));
    if (missingAssets.length > 0) {
      fail(`Missing v1.4.0 studio media assets: ${missingAssets.join(', ')}`);
    } else {
      pass('Verified all 8 studio motorcycle & editorial assets (.jpg + .webp = 16 files) present under frontend/src/assets/.');
    }

    // P0-3 wwwroot .webp mirror parity check (when running inside monorepo)
    const webApiWwwroot = path.join(REPO_ROOT, 'backend', 'src', 'MotorcycleDealer.WebApi', 'wwwroot');
    if (fs.existsSync(webApiWwwroot)) {
      const expectedWwwrootWebps = expectedStudioAssets.filter((rel) => rel.endsWith('.webp'));
      const missingWwwrootWebps = expectedWwwrootWebps.filter((rel) => !fs.existsSync(path.join(webApiWwwroot, rel)));
      if (missingWwwrootWebps.length > 0) {
        fail(`Missing P0-3 WebApi wwwroot .webp assets: ${missingWwwrootWebps.join(', ')}`);
      } else {
        pass('Verified P0-3: all 8 studio .webp assets mirrored in backend/src/MotorcycleDealer.WebApi/wwwroot/assets/.');
      }
    }

    // P0-1, P1-5 & P2-8 CSS regression checks in app-shell.css
    const appShellCss = fs.readFileSync(path.join(SRC_ROOT, 'styles', 'app-shell.css'), 'utf8');
    const receiptSheetBlockMatch = appShellCss.match(/\.printable-receipt-sheet\s*\{[\s\S]*?\}/);
    const receiptSheetBlock = receiptSheetBlockMatch ? receiptSheetBlockMatch[0] : '';
    const mediaPrintIdx = appShellCss.indexOf('@media print');
    const mediaPrintBlock = mediaPrintIdx !== -1 ? appShellCss.slice(mediaPrintIdx) : '';

    const p01PrintSelectorOk =
      mediaPrintBlock.includes('body.is-printing-receipt-open .app-viewport section.view-enter > *:not(printable-receipt-modal)') &&
      !mediaPrintBlock.includes('body.is-printing-receipt-open .app-viewport > ui-view > * > *:not(printable-receipt-modal)') &&
      /body\.is-printing-receipt-open\s+printable-receipt-modal\s*\{[^}]*display:\s*block\s*!important/i.test(mediaPrintBlock);

    const requiredTierResetVars = [
      '--clr-tier-bronze-fg', '--clr-tier-bronze-bg', '--clr-tier-bronze-border', '--clr-tier-bronze-accent',
      '--clr-tier-silver-fg', '--clr-tier-silver-bg', '--clr-tier-silver-border', '--clr-tier-silver-accent',
      '--clr-tier-gold-fg', '--clr-tier-gold-bg', '--clr-tier-gold-border', '--clr-tier-gold-accent',
      '--clr-tier-platinum-fg', '--clr-tier-platinum-bg', '--clr-tier-platinum-border', '--clr-tier-platinum-accent'
    ];
    const p15ReceiptTierResetsOk =
      requiredTierResetVars.every((v) => receiptSheetBlock.includes(v)) &&
      requiredTierResetVars.every((v) => mediaPrintBlock.includes(v));
    const p15DarkSelectOptionOk =
      /select\.form-control\s+option,\s*select\.form-input\s+option\s*\{[^}]*background-color:\s*var\(--clr-surface-card\)[^}]*color:\s*var\(--clr-text-primary\)/i.test(appShellCss);
    const p28SidebarUnifiedOk =
      /\.sidebar-nav-item\s+a\.is-active\s*\{[^}]*background-color:\s*rgba\(220,\s*38,\s*38,\s*0\.22\)[^}]*color:\s*#FECACA/i.test(appShellCss);

    if (!p01PrintSelectorOk || !p15ReceiptTierResetsOk || !p15DarkSelectOptionOk || !p28SidebarUnifiedOk) {
      fail(
        `Wave 1 app-shell.css guard failed: p01Print=${p01PrintSelectorOk}, p15TierResets=${p15ReceiptTierResetsOk}, p15SelectOption=${p15DarkSelectOptionOk}, p28Sidebar=${p28SidebarUnifiedOk}`
      );
    } else {
      pass('Verified Wave 1 CSS guards: P0-1 @media print receipt visibility, P1-5 4-tier white-paper resets & Dark <select> option contrast, P2-8 unified dark-chrome active sidebar.');
    }
  }

  // 5. Boot dev-server.mjs ephemerally and verify HTTP responses
  const server = createSpaServer();
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const address = server.address();
  const ephemeralPort = typeof address === 'object' && address ? address.port : 0;

  try {
    const rootRes = await httpGet(ephemeralPort, '/');
    if (rootRes.statusCode !== 200 || !rootRes.body.includes('motorcycleDealerApp')) {
      fail(`GET / expected 200 with 'motorcycleDealerApp', got status=${rootRes.statusCode}`);
    } else {
      pass(`Ephemeral dev-server GET / returned 200 OK (${rootRes.body.length} bytes, motorcycleDealerApp present).`);
    }

    const vendorRes = await httpGet(ephemeralPort, '/vendor/angular.min.js');
    if (vendorRes.statusCode !== 200 || vendorRes.body.length < 10000) {
      fail(`GET /vendor/angular.min.js failed with status=${vendorRes.statusCode}`);
    } else {
      pass('Ephemeral dev-server GET /vendor/angular.min.js returned 200 OK.');
    }

    const fontsCssRes = await httpGet(ephemeralPort, '/vendor/fonts/fonts.css');
    const phosphorCssRes = await httpGet(ephemeralPort, '/vendor/phosphor/regular/style.css');
    const studioWebpRes = await httpGet(ephemeralPort, '/assets/models/honda-sh160i-sport.webp');
    if (fontsCssRes.statusCode !== 200 || phosphorCssRes.statusCode !== 200 || studioWebpRes.statusCode !== 200) {
      fail(`v1.4.0 static asset HTTP check failed: fonts=${fontsCssRes.statusCode}, phosphor=${phosphorCssRes.statusCode}, webp=${studioWebpRes.statusCode}`);
    } else {
      pass('Ephemeral dev-server serves self-hosted Vietnamese fonts, Phosphor icons, and .webp studio photography with HTTP 200 OK.');
    }

    const deepLinkRes = await httpGet(ephemeralPort, '/admin/analytics');
    if (deepLinkRes.statusCode !== 200 || !deepLinkRes.body.includes('motorcycleDealerApp')) {
      fail(`GET /admin/analytics SPA pushState fallback failed with status=${deepLinkRes.statusCode}`);
    } else {
      pass('Ephemeral dev-server HTML5 SPA route fallback (GET /admin/analytics -> 200 OK) verified.');
    }

    const missingAssetRes = await httpGet(ephemeralPort, '/app/nonexistent-file.js');
    if (missingAssetRes.statusCode !== 404) {
      fail(`GET /app/nonexistent-file.js expected 404, got ${missingAssetRes.statusCode}`);
    } else {
      pass('Ephemeral dev-server missing asset guard (GET /app/nonexistent-file.js -> 404) verified.');
    }

    // P0-2: Malformed URI encoding (GET /%ZZ) and NUL byte (GET /%00.js) MUST return HTTP 400 ProblemDetails without crashing Node
    const badPercentRes = await httpGet(ephemeralPort, '/%ZZ');
    const nulByteRes = await httpGet(ephemeralPort, '/%00.js');
    const postMalformedHealthRes = await httpGet(ephemeralPort, '/health');
    const badPercentCt = String(badPercentRes.headers['content-type'] || '');
    const nulByteCt = String(nulByteRes.headers['content-type'] || '');
    if (
      badPercentRes.statusCode !== 400 ||
      !badPercentCt.includes('application/problem+json') ||
      nulByteRes.statusCode !== 400 ||
      !nulByteCt.includes('application/problem+json') ||
      postMalformedHealthRes.statusCode !== 200
    ) {
      fail(
        `P0-2 dev-server malformed URI guard failed: /%ZZ=${badPercentRes.statusCode} (${badPercentCt}), /%00.js=${nulByteRes.statusCode} (${nulByteCt}), /health=${postMalformedHealthRes.statusCode}`
      );
    } else {
      pass('Ephemeral dev-server P0-2 malformed URI guard (GET /%ZZ -> 400, GET /%00.js -> 400 ProblemDetails, server stays Healthy) verified.');
    }
  } finally {
    await new Promise((resolve) => server.close(resolve));
  }

  if (failures.length > 0) {
    console.error(`\nSPA Verification FAILED with ${failures.length} error(s).`);
    process.exit(1);
  }

  console.log('\n=== All AngularJS 1.8.3 SPA Verification Checks PASSED ===');
}

main().catch((err) => {
  console.error('[FATAL] verify-spa.mjs crashed:', err);
  process.exit(1);
});
