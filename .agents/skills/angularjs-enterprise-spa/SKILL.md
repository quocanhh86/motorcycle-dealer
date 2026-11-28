---
name: angularjs-enterprise-spa
description: >-
  Use this skill when building, refactoring, or debugging the decoupled AngularJS 1.8.3 Single-Page
  Application (`frontend/src/app/`), `@uirouter/angularjs` `$transitions.onBefore` guards across all 30
  navigable routes and 48 RBAC permissions, the `has-permission` directive, single-flight `$http` 401
  refresh-token queues, `Chart.js v4` analytics components, `<printable-receipt-modal>`, 5-star CSAT
  review & VIN warranty screens, or `Asia/Ho_Chi_Minh` presentation formatting ("Tạo giao diện AngularJS",
  "Chặn route theo role", "Vẽ biểu đồ doanh thu Chart.js", "Tự động refresh token", "In biên lai",
  "Đánh giá 5 sao & yêu cầu bảo hành", "Sửa rò rỉ bộ nhớ SPA").
---

# AngularJS 1.8 Enterprise SPA Playbook (`30` Routes · `48` Permissions · `10` SignalR Events)

Authoritative frontend engineering guide for the **Motorcycle Dealer & Loyalty Management Platform** (`v1.4.0`, `FR-01`..`FR-09`, `ADR-006`, `RISK-04`). All frontend work resides strictly inside `frontend/src/` across **Phases 3–10+** (`30` navigable routes: `26` primary + `4` aliases, `48` permissions, `10` SignalR events, Light/Dark themes & `vi-VN`/`en-US` i18n).

---

## 1. Phase-Gate & Module Directory Topology

Before creating or editing any file under `frontend/`, verify that the active phase in [PROGRESS.md](../../../PROGRESS.md) is **Phase 3 or higher**.

```text
frontend/
├── package.json                                 # Pinned: angular@1.8.3, @uirouter/angularjs, chart.js@4, @microsoft/signalr
├── scripts/                                     # dev-server.mjs, stage-vendor.mjs, verify-spa.mjs
└── src/
    ├── index.html                               # App shell + role-adaptive navbar + toast container + print styles
    ├── styles/                                  # CSS custom properties, responsive layout & @media print rules
    └── app/
        ├── app.module.js                        # Root motorcycleDealerApp bootstrap & $compileProvider config
        ├── app.routes.js                        # 30 navigable UI-Router routes (26 primary + 4 aliases)
        ├── core/
        │   ├── auth/                            # auth.service.js, permissions.constants.js (48 permissions), auth.interceptor.js
        │   ├── guards/                          # route-guard.run.js ($transitions.onBefore RBAC hooks)
        │   ├── realtime/                        # signalr-client.service.js (10 events via $rootScope.$applyAsync)
        │   └── http/                            # api-client.service.js & problem-details.mapper.js
        ├── shared/                              # app-shell, has-permission, vin-input, vndCurrency, utcToLocal, dealerChart, printableReceiptModal
        └── features/
            ├── public/                          # publicShowroom (+ test-drive CTA), publicLogin, publicRegister, publicForbidden (/403)
            ├── customer/                        # customerDashboard, customerGarage, customerBookingNew, customerBookings, customerLoyalty, customerTestDrives
            ├── staff/                           # staffWorkbench, staffInvoiceBuilder (+ spare-parts picker), staffSalesDesk, staffTimeSlots, staffCustomers, staffTestDrives
            └── admin/                           # adminAnalytics, adminDealers, adminStaff, adminModels, adminLoyaltyTiers, adminVoucherCatalog, adminSpareParts
```

---

## 2. AngularJS 1.8.3 Component & Digest Discipline

1. **Component-Only Architecture**:
   - Define every screen and reusable widget (`<dealer-chart>`, `<printable-receipt-modal>`) with `.component('camelCaseName', { ... })` using `controllerAs: '$ctrl'`.
   - Use unidirectional inputs (`<`), string attributes (`@`), and expression callbacks (`&`). **Never** use two-way `=` bindings or standalone `ng-controller`.
   - Wrap classic scripts in `(function () { 'use strict'; ... })();` and annotate every controller/service/filter/directive with explicit minification-safe `$inject` arrays (`ng-strict-di`).
2. **Zero `$rootScope` State Pollution**:
   - Never attach mutable user, booking, or chart state onto `$rootScope`. Hold state inside dedicated singleton services (`AuthService`, `NotificationStore`).
   - Only use `$rootScope.$applyAsync()` to marshal external WebSocket/DOM callbacks safely into a single digest turn, and `$rootScope.$broadcast` / `$rootScope.$on` for decoupled cross-module events.
3. **Read-Only Table & List Performance**:
   - Always pair `ng-repeat` with deterministic `track by item.id` (or `track by item.vin` / `track by item.partCode`).
   - Use one-time bindings (`::`) for immutable fields inside high-density lists (`{{ ::item.vin }}`, `{{ ::item.bookingNumber }}`, `{{ ::item.unitPriceVnd | vndCurrency }}`).

---

## 3. Canonical 30-Route UI-Router Tree (`26` Primary + `4` Aliases) & `$transitions.onBefore` Guards

### 3.1 Role Entry States & Screen Catalog (`SCR-PUB-01`..`SCR-ADM-08`)

| Namespace | Role Scope | Default Home State | Navigable Child Routes (`26` Primary + `4` Aliases = `30` Total) |
| :--- | :--- | :--- | :--- |
| `app.public.*` (`4`) | Anonymous / All | `app.public.showroom` | `showroom` (`/showroom`, `SCR-PUB-01`), `login` (`/login`, `SCR-PUB-02A`), `register` (`/register`, `SCR-PUB-02B`), `forbidden` (`/403`, `SCR-PUB-03`) |
| `app.customer.*` (`7` + `1` alias) | `['Customer']` (`20` perms) | `app.customer.garage` | `dashboard` (`/customer/dashboard`, `SCR-CUS-01`), `garage` (`/customer/garage`, `SCR-CUS-02`), `bookingNew` (`/customer/bookings/new`, `SCR-CUS-03` + alias `bookService`), `bookings` (`/customer/bookings`, `SCR-CUS-04` + 5-star CSAT modal), `loyalty` (`/customer/loyalty`, `SCR-CUS-05`), `testDrives` (`/customer/test-drives`, `SCR-CUS-06`), `warranty` (`/customer/warranty`, `SCR-CUS-07`) |
| `app.staff.*` (`7` + `3` aliases) | `['DealerStaff']` (`22` perms) | `app.staff.workbench` | `workbench` (`/staff/workbench`, `SCR-STF-01`), `invoiceBuilder` (`/staff/bookings/:bookingId/invoice`, `SCR-STF-02` + alias `invoicing`), `salesDesk` (`/staff/sales-orders/new`, `SCR-STF-03` + alias `salesNew`), `timeSlots` (`/staff/time-slots`, `SCR-STF-04` + alias `slots`), `customers` (`/staff/customers`, `SCR-STF-05`), `testDrives` (`/staff/test-drives`, `SCR-STF-06`), `warrantyReviews` (`/staff/warranty-reviews`, `SCR-STF-07`) |
| `app.admin.*` (`8`) | `['Admin']` (`32` perms) | `app.admin.analytics` | `analytics` (`/admin/analytics`, `SCR-ADM-01`), `dealers` (`/admin/dealers`, `SCR-ADM-02`), `staff` (`/admin/staff`, `SCR-ADM-03`), `models` (`/admin/models`, `SCR-ADM-04`), `loyaltyTiers` (`/admin/loyalty/tiers`, `SCR-ADM-05`), `voucherCatalog` (`/admin/loyalty/vouchers`, `SCR-ADM-06`), `spareParts` (`/admin/spare-parts`, `SCR-ADM-07`), `qualityWarranty` (`/admin/quality-warranty`, `SCR-ADM-08`) |

### 3.2 Deterministic Transition Guard (`frontend/src/app/core/guards/route-guard.run.js`)
- Enforces `data.requiresAuth`, `data.roles`, and all **`48` canonical `Permissions.<Module>.<Action>`** strings defined in `permissions.constants.js`.
- Redirects unauthenticated visitors to `app.public.login` (preserving `returnTo` / `returnUrl`) and unauthorized visitors to `app.public.forbidden` (`/403`).
- `has-permission` directive (`frontend/src/app/shared/has-permission.directive.js`) hides unauthorized UI controls and deregisters its `auth:changed` listener inside `$scope.$on('$destroy', ...)`. Server `[HasPermission]` and `8`-table `IDealerScoped` remain authoritative.

---

## 4. Single-Flight `$http` `401` Refresh-Token Queue (`ADR-006`)

Queue concurrent `401` API responses in `auth.interceptor.js` behind a single `/api/v1/auth/refresh` flight so parallel requests (`/auth/me`, `/vehicles`, `/bookings`, `/test-drives`) never trigger refresh-token replay revocation (`ReuseDetected`).

---

## 5. Leak-Free `Chart.js v4`, `<printable-receipt-modal>` & Formatting (`RISK-04`)

1. **`<dealer-chart>` (`dealer-chart.component.js`)**: Powers the 4 executive visualizations on `app.admin.analytics` (`revenue-by-dealer` Stacked Bar, `monthly-trends` Multi-Line, `bookings-by-dealer` Status Bar/Doughnut, `loyalty-distribution` Tier Doughnut). Always updates via `chartInstance.update('none')` and calls `chartInstance.destroy()` in `$onDestroy`.
2. **`<printable-receipt-modal>`**: Renders itemized `ServiceInvoices` (`ServiceInvoiceLineItems` parts + labor breakdown, voucher discount, `NetPaidAmount`, loyalty `PointsEarned`) and `SalesOrders` vehicle delivery receipts (`GET /api/v1/service-invoices/{id}/receipt`, `GET /api/v1/sales-orders/{id}/receipt`) with clean `@media print` isolation (`window.print()`).
3. **10-Event SignalR Teardown**: Bridge `BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`, `TestDriveCreated`, `TestDriveStatusChanged`, `ServiceReviewSubmitted`, `WarrantyClaimCreated`, and `WarrantyClaimStatusChanged` via `$rootScope.$applyAsync()` and invoke every unsubscribe handle inside `$onDestroy`.
4. **UTC -> `Asia/Ho_Chi_Minh` & VND Formatting**: Format timestamps via `Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })` and `DECIMAL(18,2)` VND amounts via `Intl.NumberFormat('vi-VN')` strictly in presentation filters.

---

## 6. MCP-Accelerated Workflows

1. **Library API Lookup (`context7_context7`)**: Verify `@uirouter/angularjs`, `Chart.js v4`, and `AngularJS 1.8.3` lifecycle APIs before coding.
2. **Design Translation (`figma_figma`)**: Extract layout and token context across all `21` screens (`SCR-PUB-01`..`SCR-ADM-07`).
3. **Memory-Leak & Lighthouse Auditing (`chrome-devtools-plugin_chrome-devtools`)**: Verify zero detached `<canvas>` nodes, zero orphaned SignalR listeners, zero `$digest` errors, and `>= 90` Accessibility/Best Practices scores.
4. **Multi-Tab Visual & SignalR Verification (`playwright`)**: Verify dual-tab `Customer` -> `DealerStaff` zero-refresh `BookingCreated` and `TestDriveCreated` alerts across mobile (`375x812`) and desktop (`1440x900`) viewports.

---

## 7. Verification & Mandatory Post-Task Sync Checklist

1. Run SPA static & unit contract checks:
   ```powershell
   Push-Location frontend; npm test; Pop-Location
   ```
2. Run full-stack HTTP, SignalR & RBAC smoke checks:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1
   ```
3. Synchronize all affected `docs/`, `.docx`, trackers, `.agents/`, and `.agents/scripts/git-6month-schedule-plan.json`, then verify:
   ```powershell
   node .agents/scripts/verify-post-task-sync.mjs
   ```
