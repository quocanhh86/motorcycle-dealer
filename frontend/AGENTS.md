# `frontend/AGENTS.md` — AngularJS 1.8 SPA Engineering Rules

Scoped rulebook for the decoupled **AngularJS 1.8.3 SPA** (`frontend/`) of the **Motorcycle Dealer & Loyalty Management Platform** (`FR-01`..`FR-09`, `ADR-004`, `ADR-006`, `RISK-04`). Inherits root [AGENTS.md](../AGENTS.md).

---

## 1. Pinned Runtime Stack & Zero-CDN Staging

- **Dependencies (`frontend/package.json`)**: `angular@1.8.3`, `@uirouter/angularjs@^1.0.30`, `chart.js@^4.4.0`, `@microsoft/signalr@^8.0.0`, `@fontsource/{be-vietnam-pro,space-grotesk,jetbrains-mono}`, `@phosphor-icons/web`.
- **Strict Install Boundary**: Run `npm install`, `npm start`, `npm test` **exclusively inside `frontend/`** (never create root `package.json` or `node_modules/`).
- **Zero-CDN Offline Staging**: `scripts/stage-vendor.mjs` stages UMD JS bundles, Vietnamese+Latin `.woff2` (`vendor/fonts/fonts.css`), and Phosphor `{regular,fill,duotone}` icon stylesheets into gitignored `frontend/src/vendor/`. Zero external CDN links in `index.html`.

---

## 2. AngularJS 1.8.3 Architecture, Atelier UI, Motion & Bilingual i18n

1. **Classic IIFE & Component-Only Pattern**:
   - Wrap every file in `(function () { 'use strict'; ... })();` with zero globals.
   - Define screens and shared widgets via `.component('camelCaseName', { ... })` (`controllerAs: '$ctrl'`, bindings `<`/`@`/`&`) and explicit `$inject` arrays (`ng-strict-di`). **Prohibited**: standalone `ng-controller`, two-way `=`, or mutating `$rootScope` state.
2. **Atelier Design System, Theme, Motion & Full VI/EN i18n (`v1.4.0-web`)**:
   - **Theme & Motion**: `ThemeService` drives `data-theme="light|dark"`; `styles/motion.css` provides GPU-safe keyframes (`prefers-reduced-motion`). Zero inline light hex styles (`#FFFFFF`, `#0F172A`, etc.) — use CSS semantic variables in `design-tokens.css` & `app-shell.css`.
   - **Zero UI Emojis/Unicode Icons**: Use Phosphor `<app-icon>` and `<star-rating>` (zero raw emojis, Unicode symbols, or ASCII arrows). Render studio `.webp`/`.jpg` photography via `<bike-image>` and loading/empty/error states via `<skeleton-loader>`, `<empty-state>`, `<error-retry-banner>`, `<route-progress-bar>`, and `kpi-counter`.
   - **Live VI/EN Hot-Switch**: `I18nService` (`vi-VN`/`en-US`, `| t`, `| enumT`, `Accept-Language` header in `auth.interceptor.js`) translates all UI copy and `ProblemDetails` errors. Never use one-time `::` on translated text so live `VI <-> EN` toggles re-render immediately.
3. **List Rendering & Memory-Leak Prevention (`RISK-04`)**:
   - Pair every `ng-repeat` with `track by`; never bind frozen constants in `ng-options`.
   - **`Chart.js v4` (`<dealer-chart>`)**: Subscribe to `theme:changed` and `i18n:changed`, call `chartInstance.update('none')` in `$onChanges`, and call `chartInstance.destroy()` + listener disposers in `$onDestroy`.
   - **10-Event SignalR**: Bridge all `@microsoft/signalr` handlers (`BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`, `TestDriveCreated`, `TestDriveStatusChanged`, `ServiceReviewSubmitted`, `WarrantyClaimCreated`, `WarrantyClaimStatusChanged`) via `$rootScope.$applyAsync()` and clean up all listeners/timers in `$onDestroy`.

---

## 3. 30-Route RBAC Guards, Refresh Queue & Domain Formatting

- **UI-Router Guards (`route-guard.run.js`)**: Guard all **`30` navigable routes** (`26` primary + `4` aliases, `SCR-PUB-01`..`SCR-ADM-08`) using `data.requiresAuth`, `data.roles` (`Customer`, `DealerStaff`, `Admin`), and all **`48` `Permissions.*`** constants (`Customer=20`, `DealerStaff=22`, `Admin=32`). Redirect unauthenticated users to `app.public.login` and unauthorized users to `/403`. Server `[HasPermission]`, `8` `IDealerScoped`, `8` `RowVersion`, and `4` immutable tables (`27` 3NF tables) remain authoritative.
- **Single-Flight `401` Queue (`auth.interceptor.js`)**: Queue concurrent `401` calls behind one `/api/v1/auth/refresh` flight and attach `Accept-Language`.
- **Domain Formatting & Receipts**: `vin-input` enforces ISO 3779 17-char VINs (`^[A-HJ-NPR-Z0-9]{17}$`); `vndCurrency` and `utcToLocal` (`Asia/Ho_Chi_Minh`) respect active `I18nService` locale; `<printable-receipt-modal>` renders `@media print` receipts; `ProblemDetailsMapper` surfaces localized `RFC 7807` errors + `traceId`.

---

## 4. 7-Stage Quality Loop, Hygiene Guards & Post-Task Sync Gate

Every frontend change MUST pass **`5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test`** (`15R + 6T`), the **Zero-Mock & `v1.4.0` UI Hygiene Guards** (`verify-spa.mjs`), and the **Post-Task Sync Gate**:

1. **Stages 1–2 (`5R -> 3T`)**: Audit (`1`) SPA boundary & zero CDN/root `node_modules`, (`2`) `30` routes, `48` RBAC & VI/EN parity, (`3`) `/login` & `/403` guards, CORS & `ProblemDetailsMapper`, (`4`) `ng-strict-di` `$inject`, `401` queue, `$applyAsync()` & `$onDestroy` (`RISK-04`), and (`5`) offline `src/vendor/` & non-root Docker; run `npm test` and `run-phase-quality-loop.ps1 -Phase <N> -Round 1`.
2. **Stages 3–7 (`Fix -> 5R -> Refactor -> 5R -> 3T`)**: Fix defects, re-review `5x`, refactor cleanly + expand `verify-spa.mjs` assertions, pass final `5x` review, and pass Round 3 (`0` errors).
3. **Post-Task Sync Gate**: Update affected `docs/`, regenerate/validate `.docx`, sync `TASKS.md`/`PROGRESS.md`/`README.md`/`baseline-spec-snapshot.json` + `.agents/`, map `100%` of git-trackable files in `.agents/scripts/git-6month-schedule-plan.json`, and pass `node .agents/scripts/verify-post-task-sync.mjs`.

```powershell
Push-Location frontend; npm test; Pop-Location
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round <1|3>
node .agents/scripts/verify-post-task-sync.mjs
```
