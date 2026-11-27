# Public & Customer Portal Screen Specifications (`SCR-PUB-01`..`SCR-CUS-07`)

| Metadata | Details |
| :--- | :--- |
| **Document ID** | `UX-CUST-MDLMP-2026-V1.2` |
| **Version** | `1.2.0` (**Implemented & Verified in Production Code** — `30` Navigable Routes including `SCR-CUS-07` Warranty & CSAT) |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **Repository** | [https://github.com/quocanhh86/motorcycle-dealer](https://github.com/quocanhh86/motorcycle-dealer) |
| **Covered Screens** | `SCR-PUB-01`..`SCR-PUB-03` (Public & Auth) · `SCR-CUS-01`..`SCR-CUS-07` (Customer Portal including Test-Drives, Printable Receipts, CSAT Reviews & Warranty Claims) |
| **Frontend Target Stack** | AngularJS `1.8.3` (`ng-strict-di`, `@uirouter/angularjs`, `@microsoft/signalr`, `<printable-receipt-modal>`, `controllerAs: '$ctrl'`) |
| **Traceability** | `FR-01`..`FR-06`, `FR-08`, `FR-09` · `ADR-002`..`ADR-006` ([PRD.md](../PRD.md), [DESIGN.md](../DESIGN.md), [DATABASE_DESIGN.md](../DATABASE_DESIGN.md), [DD_04_SCREEN_SPECIFICATIONS.md](../detail-design/DD_04_SCREEN_SPECIFICATIONS.md)) |
| **Companion Specs** | [DESIGN_SYSTEM_AND_NAVIGATION.md](./DESIGN_SYSTEM_AND_NAVIGATION.md) · [STAFF_AND_ADMIN_SCREENS.md](./STAFF_AND_ADMIN_SCREENS.md) |

---

## Table of Contents

1. [`SCR-PUB-01` — Public Motorcycle Showroom, Model Specs & `[Book Test Drive]` CTA](#1-scr-pub-01--public-motorcycle-showroom-model-specs--book-test-drive-cta)
2. [`SCR-PUB-02` — Authentication Portal: Sign In & Customer Self-Registration](#2-scr-pub-02--authentication-portal-sign-in--customer-self-registration)
3. [`SCR-PUB-03` — Unauthorized Access / `403 Forbidden` Guard Screen](#3-scr-pub-03--unauthorized-access--403-forbidden-guard-screen)
4. [`SCR-CUS-01` — Customer Portal Overview Dashboard](#4-scr-cus-01--customer-portal-overview-dashboard)
5. [`SCR-CUS-02` — Digital VIN Garage, Odometer Tracker & Maintenance Schedule](#5-scr-cus-02--digital-vin-garage-odometer-tracker--maintenance-schedule)
6. [`SCR-CUS-03` — Guided Workshop Service Booking & Concurrency-Safe Slot Picker](#6-scr-cus-03--guided-workshop-service-booking--concurrency-safe-slot-picker)
7. [`SCR-CUS-04` — My Service Appointments, Live Status Stepper & Printable Service Receipts](#7-scr-cus-04--my-service-appointments-live-status-stepper--printable-service-receipts)
8. [`SCR-CUS-05` — Loyalty Wallet, 4-Tier Progress, Voucher Store & Printable Order Receipts](#8-scr-cus-05--loyalty-wallet-4-tier-progress-voucher-store--printable-order-receipts)
9. [`SCR-CUS-06` — Showroom Test-Drive Scheduling & Live Status Tracker (`v1.1.0`)](#9-scr-cus-06--showroom-test-drive-scheduling--live-status-tracker-v110)

---

## 1. `SCR-PUB-01` — Public Motorcycle Showroom, Model Specs & `[Book Test Drive]` CTA

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Public entry showroom allowing prospective buyers and returning customers to filter `MotorcycleModels` by category/brand/price, inspect technical specifications and OEM `MaintenanceRules` intervals, check live per-dealer `'Available'` `VIN` stock counts across branches (`Hanoi`, `Da Nang`, `Ho Chi Minh City`), preview loyalty points earned at purchase, and launch **`[Book Test Drive]`** (`v1.1.0`) preserving `modelId` and `dealerId` across login deep-links (`FR-02`, `FR-05`, `FR-08`).
- **UI-Router State(s)**: `app.public.showroom` (`/showroom`), plus detail/locator companion states `app.public.showroomDetail` (`/showroom/:modelId`), `app.public.dealers` (`/dealers`), and landing alias `app.public.home` (`/`).
- **Guard Policy**: `data: { requiresAuth: false }` — accessible anonymously and by all authenticated roles (`Customer`, `DealerStaff`, `Admin`). Maps to anonymous/public `Permissions.Showroom.View` and `Permissions.Dealers.ViewPublic`.
- **Component Paths**:
  - `frontend/src/app/features/showroom/showroom-catalog.component.js` (`<showroom-catalog>`)
  - `frontend/src/app/features/showroom/showroom-model-detail.component.js` (`<showroom-model-detail>`)
  - `frontend/src/app/features/showroom/dealer-locator.component.js` (`<dealer-locator>`)

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 12-Column Showroom Grid)
+------------------------------------------------------------------------------------------------------+
| [APEX MOTO]   Showroom Catalog (Active)   Authorized Dealers               [Sign In] [Create Account]|
+------------------------------------------------------------------------------------------------------+
| HERO BANNER: PRECISION MOTORCYCLES • CLOSED-LOOP VIN CARE • EARN UP TO 2.0x LOYALTY POINTS           |
+------------------------------------------------------------------------------------------------------+
| Search: [ Search SH160i, Streetfighter... ]  Category: [All Categories v]  Branch: [All Dealers   v] |
| Price Sort: [MSRP: Low to High v]            [x] In-Stock VINs Only        Showing 6 of 6 Models     |
+------------------------------------------------------------------------------------------------------+
| +--------------------------------+ +--------------------------------+ +----------------------------+ |
| | [Hero Image: HND-SH160I-2026]  | | [Hero Image: YMH-EXC155-2026]  | | [Hero Image: KWS-Z400-26]  | |
| | [Scooter] • 156.9 cc           | | [Underbone] • 155 cc           | | [Naked] • 399 cc           | |
| | Honda SH160i ABS Sport         | | Yamaha Exciter 155 VVA GP      | | Kawasaki Z400 ABS SE       | |
| | MSRP: 102.500.000 ₫ (.u-mono)  | | MSRP: 51.000.000 ₫ (.u-mono)   | | MSRP: 149.000.000 ₫        | |
| | Est. Loyalty: +10,250 pts*     | | Est. Loyalty: +5,100 pts*      | | Est. Loyalty: +14,900 pts* | |
| | ------------------------------ | | ------------------------------ | | -------------------------- | |
| | ● Saigon Center: 4 VINs Ready  | | ● Da Nang Hub: 3 VINs Ready    | | ▲ Hanoi Flagship: 1 Left   | |
| | [Specs]   [Book Test Drive ->] | | [Specs]   [Book Test Drive ->] | | [Specs]   [Book Test Drive]| |
| +--------------------------------+ +--------------------------------+ +----------------------------+ |
+------------------------------------------------------------------------------------------------------+
| *Logged-in Customer sees tier-adjusted preview: Floor(Floor(BaseRetailPrice / 10000) * Multiplier)   |
+------------------------------------------------------------------------------------------------------+

MOBILE (< 768px — Collapsible Filter Accordion + Single-Column Stack)
+-----------------------------------------------+
| [☰] [APEX MOTO]                     [Sign In] |
+-----------------------------------------------+
| [🔍 Search models...            ] [⚙ Filters] |
| Active Filter Pills: [Scooter ✕] [Saigon ✕]   |
+-----------------------------------------------+
| +-------------------------------------------+ |
| | [Image: Honda SH160i ABS Sport]           | |
| | Honda SH160i ABS Sport    102.500.000 ₫   | |
| | 156.9cc • Warranty: 36 mo / 30,000 km     | |
| | ● 4 Available VINs at Saigon Center       | |
| | [View Specs & Maintenance Rules ->]       | |
| +-------------------------------------------+ |
+-----------------------------------------------+
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**:
  - `<showroom-catalog>` (Container)
    - `<showroom-filter-bar filters="$ctrl.filters" dealers="$ctrl.dealers" on-filter-change="$ctrl.applyFilters(next)">`
    - `<motorcycle-model-card ng-repeat="model in $ctrl.models track by model.id" model="model" user-multiplier="$ctrl.activeMultiplier" on-inspect="$ctrl.openModelDetail(model.id)">`
    - `<showroom-model-drawer model-detail="$ctrl.selectedModel" rules="$ctrl.selectedModelRules" on-close="$ctrl.closeDrawer()">`
    - `<pagination-bar pagination="$ctrl.pagination" on-page-change="$ctrl.changePage(pageNumber, pageSize)">`
- **`$ctrl` ViewModel State**:
  - `$ctrl.filters = { search: '', category: '', dealerId: '', inStockOnly: false, sortBy: 'priceAsc', pageNumber: 1, pageSize: 12 }`
  - `$ctrl.activeMultiplier`: `1.00` when anonymous; bound from `LoyaltyStore.getMultiplier()` (`1.00`, `1.20`, `1.50`, `2.00`) when logged in as `Customer`.
- **One-Time Bindings (`::`)**: `{{ ::model.modelCode }}`, `{{ ::model.brand }}`, `{{ ::model.modelName }}`, `{{ ::model.category }}`, `{{ ::model.engineDisplacementCc }}`, `{{ ::model.baseRetailPrice | vndCurrency }}`, `{{ ::rule.intervalKm }}`, `{{ ::rule.intervalMonths }}`.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Control / Query Param | Field Name | Input Type & Allowed Values | Client & Server Validation Rules |
| :--- | :--- | :--- | :--- |
| Keyword Search | `search` | Text input (`300ms` `ng-model-options="{ debounce: 300 }"`) | Trimmed; `maxLength: 80`; stripped of control characters; server applies parameterized `LIKE` on `Brand`, `ModelName`, `ModelCode`. |
| Category Filter | `category` | `<select>` enum | Empty (`All`) or exact `CK_MotorcycleModels_Category` member: `'Scooter'`, `'Underbone'`, `'Sport'`, `'Naked'`, `'Adventure'`, `'Cruiser'`. |
| Branch Stock Filter | `dealerId` | `<select>` (`UUID`) | Optional valid `Dealers.Id` GUID (`400 VALIDATION_FAILED` if malformed UUID query param). |
| In-Stock Toggle | `inStockOnly` | Checkbox (`boolean`) | Filters models having `COUNT(DealerInventories WHERE StockStatus = 'Available') >= 1`. |
| Loyalty Estimator | *(Read-only computed)* | Formula badge | `Math.floor(Math.floor(model.baseRetailPrice / 10000) * $ctrl.activeMultiplier)`. |
| **`[Book Test Drive]` CTA (`v1.1.0`)** | `modelId`, `dealerId` | Primary action button | Authenticated `Customer` navigates directly to `app.customer.testDrives({ modelId: model.id, dealerId: $ctrl.filters.dealerId })`; anonymous visitors route to `app.public.login({ returnUrl: '/customer/test-drives?modelId=...' })` validated safely by `AuthService.resolvePostLoginTarget`. |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/dealers` (loads active dealerships for branch selector & locator).
  - `GET /api/v1/motorcycles?search=&category=&dealerId=&inStockOnly=&pageNumber=1&pageSize=12` (loads catalog + aggregated `availableVinCount` per dealer; never exposes raw unsold `VIN` serials publicly to prevent scraping, showing branch stock counts instead).
  - `GET /api/v1/motorcycles/{modelId}` and `GET /api/v1/motorcycles/{modelId}/maintenance-rules` (on detail view/drawer open).
- **SignalR Reactions**:
  - Anonymous visitors open **zero** WebSocket connections.
  - Logged-in `Customer` receives `TierUpgraded` / `LoyaltyPointsUpdated` via global `SignalRClientService`; `$ctrl` listens to `signalr:TierUpgraded` (unregistered in `$onDestroy`) to recompute `$ctrl.activeMultiplier` on visible points estimator tags immediately.

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Initial route activation or filter change in-flight | `<skeleton-loader variant="card-grid" rows="6">` preserving `340px` card heights (`CLS = 0`). |
| **`Empty`** | `data.length === 0` (e.g., `'Cruiser'` + `'In-Stock Only'` at Da Nang returns `0` matches) | Centered illustration + `"No motorcycles match your current filters"` + `[Reset All Filters]` button restoring default catalog. |
| **`Populated`** | `data.length >= 1` | Responsive 3-column (`lg`), 2-column (`md`), or 1-column (`xs`) model cards with branch stock pills (`Available`, `Last Unit`, `Out of Stock`) and pagination bar. |
| **`Validation Error (400/422)`** | Malformed URL query parameter (`?dealerId=not-a-guid` or `pageSize=999`) | `ProblemDetailsMapper` sanitizes invalid query params, displays non-blocking warning toast (`400 VALIDATION_FAILED`), and reloads default valid query. |
| **`Concurrency Conflict (409)`** | N/A *(Read-only screen; no mutating writes)* | Not applicable on `SCR-PUB-01`; stock count staleness is resolved when `DealerStaff` locks `DealerInventories.RowVersion` on `SCR-STF-03`. |

---

## 2. `SCR-PUB-02` — Authentication Portal: Sign In & Customer Self-Registration

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Provides credential authentication (`app.public.login`) with automatic role-home or deep-link `returnUrl` routing across all 3 roles (`Customer`, `DealerStaff`, `Admin`), plus self-service `Customer` onboarding (`app.public.register`) that atomically creates a `Users` record, assigns the `'Customer'` role (`16` customer permissions), and initializes a `'Bronze'` (`1.00x`, `0` pts) `LoyaltyAccounts` wallet (`FR-01`, `FR-05`).
- **UI-Router State(s)**:
  - `SCR-PUB-02A`: `app.public.login` (`/login?returnUrl` — supports canonical `?returnTo` & `?returnUrl` params)
  - `SCR-PUB-02B`: `app.public.register` (`/register`)
- **Guard Policy**: Anonymous-only entry; `$transitions.onBefore` redirects already-authenticated users immediately to their role home (`Customer -> app.customer.garage`, `DealerStaff -> app.staff.workbench`, `Admin -> app.admin.analytics`).
- **Component Paths**:
  - `frontend/src/app/features/auth/login-view.component.js` (`<login-view>`)
  - `frontend/src/app/features/auth/register-customer-view.component.js` (`<register-customer-view>`)

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Split Brand Showcase + Auth Card)
+------------------------------------------------------------------------------------------------------+
| +--------------------------------------------------+ +---------------------------------------------+ |
| | APEX MOTO LOYALTY & WORKSHOP NETWORK             | | [Tab: Sign In (Active)] [Tab: Register]     | |
| |                                                  | | ------------------------------------------- | |
| |  • Track every 17-character VIN in one garage    | | WELCOME BACK                                | |
| |  • Guaranteed workshop bay time-slot booking     | | Sign in to access your portal               | |
| |  • 10,000 VND = 1 Base Pt (Up to 2.0x Platinum)  | |                                             | |
| |  • Instant real-time workshop status updates     | | Email Address *                             | |
| |                                                  | | [ minh.nguyen@example.vn                  ] | |
| |  Authorized Flagship Hubs:                       | |                                             | |
| |  Hanoi • Da Nang • Ho Chi Minh City              | | Password *                         [Show 👁] | |
| |                                                  | | [ ••••••••••••••••                        ] | |
| |                                                  | |                                             | |
| |                                                  | | [         Sign In to Portal ->            ] | |
| |                                                  | |                                             | |
| |                                                  | | New motorcycle owner? [Create Account]      | |
| +--------------------------------------------------+ +---------------------------------------------+ |
+------------------------------------------------------------------------------------------------------+

SCR-PUB-02B (app.public.register — Customer Self-Registration Form):
  Full Name *         [ Minh Nguyen                      ]
  Email Address *     [ minh.nguyen@example.vn           ]
  Mobile Phone *      [ 0909123456                       ]
  Password *          [ ••••••••••••••••        ] [Strength: Strong ■■■■]
  Confirm Password *  [ ••••••••••••••••        ]
  [x] Enroll in Loyalty Program (Starts at Bronze Tier • 1.00x Multiplier)
  [        Create Customer Account ->        ]

MOBILE (< 768px): Left marketing panel collapses into a compact 64px brand header above a full-width card.
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<login-view>` / `<register-customer-view>` -> `<form name="$ctrl.authForm" novalidate>` -> `<problem-details-alert problem="$ctrl.serverProblem">` -> `<password-strength-meter value="$ctrl.form.password">`.
- **`$ctrl` State**:
  - `loginView`: `$ctrl.credentials = { email: '', password: '' }`, `$ctrl.showPassword = false`, `$ctrl.isSubmitting = false`, `$ctrl.returnTarget = $stateParams.returnUrl || $stateParams.returnTo || null`.
  - `registerCustomerView`: `$ctrl.form = { fullName: '', email: '', phoneNumber: '', password: '', confirmPassword: '' }`.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Form / Screen | Field Name | Constraints & Input Mask | Client (`ngMessages`) & Server (`FluentValidation`) Rules |
| :--- | :--- | :--- | :--- |
| **Login & Register** | `email` | `type="email"`, `maxlength="256"`, `autocomplete="email"` | Required; RFC 5322 email syntax; normalized uppercase server-side (`NormalizedEmail`). Duplicate on register -> `409 Conflict` (`EMAIL_ALREADY_REGISTERED`). |
| **Register** | `fullName` | `type="text"`, `maxlength="150"` | Required; `2..150` Unicode characters (`NVARCHAR(150)` supporting Vietnamese diacritics). |
| **Register** | `phoneNumber` | `type="tel"`, `maxlength="20"` | Required; regex `^(\+84|0)[35789][0-9]{8}$` (Vietnamese mobile E.164/domestic); unique across active users (`UQ_Users_PhoneNumber`). |
| **Login & Register** | `password` | `type="password"`, `minlength="8"`, `maxlength="128"` | Login: required non-empty. Register: `>= 8` chars, at least 1 uppercase, 1 lowercase, 1 digit, and 1 symbol. |
| **Register** | `confirmPassword` | `type="password"` | Client custom validator `matchPassword`: must strictly equal `$ctrl.form.password`. |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `POST /api/v1/auth/login` -> stores JWT access token (`15m` TTL) + rotating refresh token via `AuthService`, populates `AuthStore` (`userId`, `role`, `dealerId`, `permissions`), and navigates to validated internal `returnTarget` (rejecting external open-redirect URLs not starting with `app.`) or default role home.
  - `POST /api/v1/auth/register` -> creates `Customer` + `Bronze` `LoyaltyAccount`, auto-authenticates (or routes to `app.customer.garage` with welcome toast).
- **SignalR Reactions**:
  - Immediately upon `200 OK` / `201 Created` token storage, `AuthService` invokes `SignalRClientService.start()`, opening `/hubs/notifications?access_token={jwt}` so server `OnConnectedAsync` binds `user-{userId}` (and `dealer-{dealerId}` for `DealerStaff`, `role-Admin` for `Admin`) + fetches unread count via `GET /api/v1/notifications?unreadOnly=true`.

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Form submit in-flight (`$ctrl.isSubmitting === true`) | Disables inputs and submit CTA; renders inline spinner `"Signing in..."` / `"Provisioning Bronze Wallet..."`. |
| **`Empty`** | Initial clean form load (`$pristine`) | Clean unfocused inputs; if redirected from expired session (`?reason=sessionExpired`), displays info banner `"Your session expired. Please sign in to continue."` |
| **`Populated`** | User enters valid fields (`$ctrl.authForm.$valid === true`) | Submit button transitions to active `#DC2626` state; password strength bar turns emerald (`Strong`). |
| **`Validation Error (400/422)`** | `400 VALIDATION_FAILED` (field format), `401 UNAUTHORIZED` (wrong password / locked staff account), or `429 RATE_LIMIT_EXCEEDED` | `400`: highlights offending inputs via `ProblemDetailsMapper`. `401`: displays generic security-safe banner `"Invalid email or password"` (never leaks account existence) and clears password field. |
| **`Concurrency Conflict (409)`** | `POST /api/v1/auth/register` hits existing `Email` or `PhoneNumber` (`UQ_Users_Email` / `UQ_Users_PhoneNumber`) | Highlights `email` or `phoneNumber` field with inline message + quick link `[Sign in with this email instead ->]`. |

---

## 3. `SCR-PUB-03` — Unauthorized Access / `403 Forbidden` Guard Screen

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Deterministic RBAC boundary screen rendered whenever `@uirouter/angularjs` `$transitions.onBefore` or an API `403 Forbidden` response blocks a user from accessing a route outside their assigned role or `Permissions.<Module>.<Action>` set (`AC-01.1`, `BO-06`).
- **UI-Router State**: `app.public.forbidden` (`/403`).
- **Guard Policy**: `data: { requiresAuth: false }` — reachable by any principal without causing redirect loops.
- **Component Path**: `frontend/src/app/features/auth/forbidden-view.component.js` (`<forbidden-view>`).

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP & MOBILE CENTERED GUARD CARD (Max-Width: 540px)
+----------------------------------------------------------------------+
|                         [ ⊘ SHIELD LOCK 403 ]                        |
|                                                                      |
|                   403 — ACCESS RESTRICTED BY POLICY                  |
|   Your current account role (Customer) does not hold the required    |
|   permission to view the requested administrative or branch resource.|
|                                                                      |
|   Signed-In Role : [Customer]          Trace Ref: 00-8f2a...91b      |
|   Attempted State: app.admin.analytics                               |
|                                                                      |
|        [ <- Return to My VIN Garage ]     [ Switch Account ]         |
+----------------------------------------------------------------------+
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **State**: `$ctrl.currentRole = AuthStore.getRole() || 'Anonymous'`, `$ctrl.homeState = AuthStore.getDefaultHomeState()`, `$ctrl.homeLabel` (`'Return to My VIN Garage'` | `'Return to Live Workbench'` | `'Return to Executive Analytics'` | `'Browse Showroom'`). Uses `::` one-time bindings for all static diagnostic labels.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Primary CTA (`Return to <Role Home>`)**: Navigates deterministically via `$state.go($ctrl.homeState)`.
- **Secondary CTA (`Switch Account`)**: Invokes `AuthService.logout()` (revoking `RefreshTokens` via `POST /api/v1/auth/logout` and stopping SignalR) then routes to `app.public.login`.

### e) API Calls & Real-Time SignalR Reactions
- Zero automatic API calls on mount (keeps existing session and SignalR WebSocket alive unless user explicitly clicks `Switch Account`).

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading` / `Empty` / `Populated`** | Rendered synchronously from in-memory `AuthStore` | Instant zero-network render displaying current role badge and safe navigation recovery button. |
| **`Validation Error` / `Conflict`** | N/A | Read-only error boundary view; never exposes internal server stack traces or permission table schemas. |

---

## 4. `SCR-CUS-01` — Customer Portal Overview Dashboard

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Unified self-service command center summarizing the customer's active VIN maintenance alerts, next confirmed workshop appointment, current loyalty tier progress (`AvailablePoints` vs. `LifetimeQualifyingPoints`), and recent `SalesOrders` / `ServiceInvoices` (`FR-01`..`FR-06`).
- **UI-Router State**: `app.customer.dashboard` (`/customer/dashboard`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Vehicles.ViewOwn', 'Permissions.Bookings.ViewOwn', 'Permissions.Loyalty.ViewOwn']`.
- **Component Path**: `frontend/src/app/features/garage/customer-dashboard.component.js` (`<customer-dashboard>`).

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 12-Column Executive Customer Summary)
+------------------------------------------------------------------------------------------------------+
| WELCOME BACK, MINH NGUYEN                        [+ Register External VIN]  [Book Workshop Service ->]|
+------------------------------------------------------------------------------------------------------+
| [!] ACTIONABLE VIN ALERT: Honda SH160i (RLHJF5109RZ104829) is 150 km from 4,000 km Periodic Service  |
|     Target: 4,000 km or 25/05/2026 • Trigger: MileageThreshold                [Book This VIN Now ->] |
+------------------------------------------------------------------------------------------------------+
| +------------------------------+ +----------------------------------+ +----------------------------+ |
| | MY GARAGE FLEET              | | NEXT WORKSHOP APPOINTMENT        | | LOYALTY & TIER STATUS      | |
| | 2 Active Motorcycles         | | SB-202605-0041 • [✓ Confirmed]   | | [◆◆ Silver • 1.20x]        | |
| | • SH160i (3,850 km • DueSoon)| | 16/05/2026 • 09:00 - 10:00       | | Available: 649 pts         | |
| | • Exciter 155 (11,200 km)    | | Apex Moto Saigon Center          | | Qualifying: 849 / 2,000    | |
| | [Open My VIN Garage ->]      | | [View Booking Timeline ->]       | | [Redeem Vouchers (2) ->]   | |
| +------------------------------+ +----------------------------------+ +----------------------------+ |
| +------------------------------------------------+ +-----------------------------------------------+ |
| | RECENT WORKSHOP INVOICES (ServiceInvoices)     | | SHOWROOM PURCHASES (SalesOrders)              | |
| | INV-202603-0019 • 12/03/2026 • 450.000 ₫ (+45p)| | SO-202602-0008 • SH160i • 101.000.000 ₫       | |
| | [View All Invoices ->]                         | | VIN: RLHJF5109RZ104829   [View Receipt ->]    | |
| +------------------------------------------------+ +-----------------------------------------------+ |
+------------------------------------------------------------------------------------------------------+

MOBILE (< 768px): Alert banner stacks on top, followed by swipeable/stacked 1-column summary cards and a sticky bottom [Book Service] CTA.
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<customer-dashboard>` -> `<vin-reminder-alert-banner>`, `<garage-summary-card>`, `<next-booking-card>`, `<loyalty-snapshot-card>`, `<recent-receipts-list>`.
- **State**: Hydrated concurrently via `$q.all({ vehicles, bookings, loyalty, invoices, orders })` on `$onInit`; immutable identifiers (`::inv.invoiceNumber`, `::ord.orderNumber`, `::veh.vin`) use one-time bindings.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **`[Book This VIN Now ->]`**: Calls `$state.go('app.customer.bookService', { vehicleId: reminder.customerVehicleId })` so `SCR-CUS-03` pre-selects the exact `VIN` and pre-fills `PeriodicMaintenance`.
- **Read-Only Ownership Scope**: Every widget displays strictly records where `CustomerId == CurrentUser.UserId`.

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/vehicles/my`, `GET /api/v1/bookings/my?pageNumber=1&pageSize=3`, `GET /api/v1/loyalty/account`, `GET /api/v1/service-invoices/my?pageSize=3`, `GET /api/v1/sales-orders/my?pageSize=3`.
- **SignalR Reactions** (`$onDestroy`-cleaned):
  - `MaintenanceReminderTriggered` -> inserts/updates top warning banner immediately.
  - `BookingStatusChanged` -> updates `nextBookingCard` status badge (`Pending -> Confirmed -> InProgress -> Completed`) live.
  - `LoyaltyPointsUpdated` & `TierUpgraded` -> animates `AvailablePoints` and updates tier badge/multiplier.

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | `$q.all(...)` initial fetch | 3-column `<skeleton-loader variant="kpi-strip">` + 2 table skeletons. |
| **`Empty`** | Brand-new customer (`0` vehicles, `0` bookings, `Bronze` `0` pts) | Onboarding hero card with two clear CTAs: `[Browse Showroom Catalog]` and `[Register Existing 17-Char Motorcycle VIN]`. |
| **`Populated`** | Customer owns `>= 1` vehicle or booking | Full 3-pillar summary cards + conditional amber/red VIN maintenance alert banner when `DueSoon` or `Overdue`. |
| **`Validation Error (400/422)`** | Partial widget API error | Isolates error banner to the failed card with a local `[Retry]` button while keeping sibling cards interactive. |
| **`Concurrency Conflict (409)`** | N/A | Read-only aggregation dashboard. |

---

## 5. `SCR-CUS-02` — Digital VIN Garage, Odometer Tracker & Maintenance Schedule

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Enables customers to manage all motorcycles they own—both bikes auto-onboarded from showroom `SalesOrders` (`RegistrationSource = 'ShowroomSale'`) and externally acquired bikes registered manually by 17-character ISO 3779 `VIN` (`RegistrationSource = 'ExternalManual'`)—update current odometer mileage, and inspect OEM `MaintenanceRules` milestones and deduplicated `MaintenanceReminders` (`FR-03`, `FR-04`, `ADR-005`).
- **UI-Router State(s)**: `app.customer.garage` (`/customer/garage`) + detail view/drawer `app.customer.vehicleDetail` (`/customer/garage/:vehicleId`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Vehicles.ViewOwn']` (`[+ Register External VIN]` & `[Update Odometer]` guarded by `has-permission="Permissions.Vehicles.RegisterOwn"`).
- **Component Paths**:
  - `frontend/src/app/features/garage/customer-garage.component.js` (`<customer-garage>`)
  - `frontend/src/app/features/garage/customer-vehicle-detail.component.js` (`<customer-vehicle-detail>`)

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Garage Fleet Grid + Slide-Over OEM Schedule Drawer)
+------------------------------------------------------------------------------------------------------+
| MY DIGITAL VIN GARAGE (2 Motorcycles)                             [+ Register External Bike by VIN]  |
+------------------------------------------------------------------------------------------------------+
| [!] DUE SOON: Honda SH160i (VIN: RLHJF5109RZ104829) — 4,000 km Periodic Service (150 km left) [Book] |
+------------------------------------------------------------------------------------------------------+
| +------------------------------------------------+ +-----------------------------------------------+ |
| | Honda SH160i ABS Sport   [Verified Showroom ✓] | | Yamaha Exciter 155 VVA      [External VIN]    | |
| | VIN: RLHJF5109RZ104829 (.u-mono)               | | VIN: RLCUG1010PY092341 (.u-mono)              | |
| | License Plate: 59-A3 889.21 • Racing Red       | | License Plate: 59-X1 412.09 • Matte Blue      | |
| | ---------------------------------------------- | | --------------------------------------------- | |
| | Current Odometer : 3,850 km  [Update Odometer] | | Current Odometer : 11,200 km [Update Odometer]| |
| | Last Serviced    : 1,000 km (12/03/2026)       | | Last Serviced    : 8,000 km (04/01/2026)      | |
| | Next Milestone   : 4,000 km / 12/09/2026       | | Next Milestone   : 12,000 km / 04/07/2026     | |
| | Wear Progress    : [█████████████░] 95% (Due!) | | Wear Progress    : [███████████░░░] 80% (OK)  | |
| | [OEM Schedule & History]   [Book Service ->]   | | [OEM Schedule & History]   [Book Service ->]  | |
| +------------------------------------------------+ +-----------------------------------------------+ |
+------------------------------------------------------------------------------------------------------+

MODAL 1 — REGISTER EXTERNAL BIKE VIN (POST /api/v1/vehicles):
+--------------------------------------------------------------------+
| REGISTER EXTERNAL MOTORCYCLE BY ISO 3779 VIN                   [✕] |
+--------------------------------------------------------------------+
| 17-Character VIN * (Uppercase A-Z, 0-9; excludes I, O, Q)   [17/17]|
| [ RLHKF4102PY009876                                      ] (.u-mono)|
| Motorcycle Model *     [ Honda Air Blade 160 SE (156.9cc)      v ] |
| Current Odometer (km)* [ 3,650                                   ] |
| License Plate          [ 59-C2 451.88                            ] |
| Exterior Color         [ Matte Black                             ] |
| Note: External bikes earn 0 purchase points, but earn full tiered  |
|       points on every future workshop ServiceInvoice!              |
|                                    [Cancel]  [Register Bike VIN ->]|
+--------------------------------------------------------------------+

MODAL 2 — UPDATE ODOMETER (PATCH /api/v1/vehicles/{vehicleId}/mileage):
  Recorded Odometer: 3,850 km -> New Odometer (km) *: [ 3,920 ] (Must be >= 3,850 km)

MOBILE (< 768px): Vehicle cards stack 1-column; modals render as full-width bottom sheets with numeric keypad (`inputmode="numeric"`) for odometer entry.
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**:
  - `<customer-garage>`
    - `<vin-reminder-alert-banner reminders="$ctrl.activeReminders">`
    - `<customer-vehicle-card ng-repeat="v in $ctrl.vehicles track by v.id" vehicle="v" on-update-odometer="$ctrl.openOdometerModal(v)" on-view-schedule="$ctrl.openScheduleDrawer(v)" on-book="$ctrl.goToBook(v.id)">`
    - `<external-vin-modal is-open="$ctrl.showRegisterModal" models="$ctrl.catalogModels" on-saved="$ctrl.onVehicleRegistered(created)">`
    - `<odometer-update-modal vehicle="$ctrl.editingVehicle" on-saved="$ctrl.onOdometerUpdated(updated)">`
    - `<milestone-timeline-drawer vehicle="$ctrl.selectedVehicle" history="$ctrl.vehicleHistory">`
- **Bindings**: `{{ ::v.vin }}`, `{{ ::v.motorcycleModelName }}`, `{{ ::v.registrationSource }}` are one-time (`::`); `{{ v.currentMileageKm | number }}`, `{{ v.nextDueMileageKm | number }}`, and wear progress bar remain dynamic so inline odometer updates re-render immediately.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Modal / Action | Field Name | Type & Directive | Client & Server Validation Rules (`EC-03`, `EC-04`) |
| :--- | :--- | :--- | :--- |
| **External VIN Modal** | `vin` | `text` + `vin-input` directive | Required; exact length `17`; uppercase regex `^[A-HJ-NPR-Z0-9]{17}$` (**rejects `I`, `O`, `Q` immediately with inline explanation `"ISO 3779 VINs never contain letters I, O, or Q"`**); server checks `UQ_CustomerVehicles_VIN`. |
| **External VIN Modal** | `motorcycleModelId` | `<select>` (`UUID`) | Required; must reference active `MotorcycleModels.Id`. |
| **External VIN Modal** | `currentMileageKm` | `number` (`int`) | Required integer `0 <= currentMileageKm <= 500000`. |
| **External VIN Modal** | `licensePlate` | `text` (`optional`) | Optional `VARCHAR(20)` (`maxLength: 20`); unique when non-null (`UQ_CustomerVehicles_LicensePlate`). |
| **Odometer Modal** | `currentMileageKm` | `number` (`int`) | **Monotonicity Invariant**: client `min="{{ $ctrl.editingVehicle.currentMileageKm }}"` and server domain check `newMileageKm >= vehicle.CurrentMileageKm`; rollback attempt returns `422 ODOMETER_REGRESSION` (`AC-03.2`). |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/vehicles/my` (loads garage fleet + precomputed `nextDueMileageKm`, `nextDueDateUtc`, and active reminder flags).
  - `GET /api/v1/motorcycles?pageSize=100` (populates model dropdown when opening External VIN modal).
  - `POST /api/v1/vehicles` (registers external VIN with `PurchasedSalesOrderId = NULL`).
  - `PATCH /api/v1/vehicles/{vehicleId}/mileage` (updates `CurrentMileageKm`).
  - `GET /api/v1/vehicles/{vehicleId}/maintenance-history` (loads computed OEM milestone timeline: `Completed`, `DueSoon`, `Overdue`, `Upcoming` + historical `ServiceInvoices`).
- **SignalR Reactions**:
  - `MaintenanceReminderTriggered`: `$rootScope.$applyAsync()` attaches the live reminder badge to the matching `CustomerVehicle` card (`v.vin === payload.vin`) and reveals the top `vinReminderAlertBanner` without page refresh (`AC-04.2`).

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching `/api/v1/vehicles/my` or `/maintenance-history` | 2-card `<skeleton-loader variant="card-grid">` (or timeline skeleton inside slide-over drawer). |
| **`Empty`** | Customer has `0` vehicles in `CustomerVehicles` | Empty garage illustration explaining automatic showroom onboarding vs. manual registration + primary `[+ Register Existing 17-Char VIN]` button. |
| **`Populated`** | `>= 1` vehicle in garage | Displays cards sorted with `Overdue` / `DueSoon` vehicles first, color-coded progress bar (`< 80%` emerald, `80–99%` amber, `>= 100%` crimson), and one-click `[Book Service ->]`. |
| **`Validation Error (400/422)`** | Invalid 17-char `VIN` (`400 VALIDATION_FAILED`) or odometer rollback `< recorded km` (`422 ODOMETER_REGRESSION`) | Keeps modal open, maps field error directly under `vin` or `currentMileageKm` (`"Odometer cannot be lower than recorded 8,200 km"`), and focuses input. |
| **`Concurrency Conflict (409)`** | `409 DUPLICATE_VIN` (VIN already registered in `CustomerVehicles` or duplicate `LicensePlate`) | Displays `ProblemDetails.detail` inside modal banner (`"This 17-character VIN is already registered"`) without losing other form inputs. |

---

## 6. `SCR-CUS-03` — Guided Workshop Service Booking & Concurrency-Safe Slot Picker

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Multi-step guided booking wizard allowing a `Customer` to select an owned `CustomerVehicle` (`VIN`), choose an authorized `Dealer` and date, pick an open `ServiceTimeSlot` guarded by SQL Server `RowVersion`, optionally attach an eligible `'Available'` `CustomerVoucher`, and submit `POST /api/v1/bookings`—triggering an instant SignalR `BookingCreated` broadcast to the target branch's `DealerStaff` workbench (`FR-04`, `FR-06`, `ADR-005`).
- **UI-Router State**: `app.customer.bookService` (alias `app.customer.bookingNew`, URL `/customer/bookings/new?vehicleId&dealerId`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Bookings.Create', 'Permissions.TimeSlots.ViewAvailable']`.
- **Component Path**: `frontend/src/app/features/booking/service-slot-picker.component.js` (`<service-slot-picker>`).

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 8-Col Wizard + 4-Col Sticky Summary Card)
+------------------------------------------------------------------------------------------------------+
| BOOK WORKSHOP MAINTENANCE                      [1. Bike & Service] -> [2. Dealer & Slot] -> [3. Confirm]|
+------------------------------------------------------------------+-----------------------------------+
| STEP 1: MOTORCYCLE & SERVICE DETAILS                             | BOOKING SUMMARY (Sticky Card)     |
| Select Garage Bike *: [ Honda SH160i • RLHJF5109RZ104829     v ] | --------------------------------- |
| Current Odometer *  : [ 3,920 ] km (Min: 3,850 km)               | Bike   : Honda SH160i ABS Sport   |
| Service Type *      : [ PeriodicMaintenance (4,000 km)       v ] | VIN    : RLHJF5109RZ104829        |
| ---------------------------------------------------------------- | Dealer : Apex Moto Saigon Center  |
| STEP 2: AUTHORIZED DEALER & LIVE WORKSHOP BAY TIME SLOTS         | Date   : Saturday, 16/05/2026     |
| Dealership * : [ DLR-HCM-01 • Apex Moto Saigon Center        v ] | Window : 09:00 - 10:00            |
| Service Date*: [ 2026-05-16 ]   (Hours: 08:00 - 19:00) [↻ Refresh]| Voucher: SVC100K-7K9P2M4X (-100k)|
|                                                                  |                                   |
| +--------------------+ +--------------------+ +----------------+ | Loyalty Accrual Rule:             |
| | 08:00 - 09:00      | | 09:00 - 10:00 [✓]  | | 10:00 - 11:00  | | Net workshop spend earns 1.20x    |
| | [■ Full (5/5)]     | | [▲ Last Bay (1/5)] | | [● Available]  | | Silver points upon completion!    |
| | Unavailable        | | Selected (RowVer)  | | 4 of 5 Bays    | |                                   |
| +--------------------+ +--------------------+ +----------------+ | [   Confirm Workshop Booking -> ] |
| ---------------------------------------------------------------- |                                   |
| STEP 3: OPTIONAL LOYALTY VOUCHER & TECHNICIAN NOTES              |                                   |
| Attach Voucher: [ SVC100K-7K9P2M4X • 100.000 ₫ Off (Min 500k) v] |                                   |
|   (i) Showroom-only & expired vouchers are automatically hidden. |                                   |
| Reported Notes: [ 4,000 km oil change, CVT belt & brake check  ] |                                   |
+------------------------------------------------------------------+-----------------------------------+

409 CONFLICT RECOVERY BANNER / MODAL (Triggered on HTTP 409 SLOT_CAPACITY_EXHAUSTED):
+--------------------------------------------------------------------+
| [▲] THAT LAST BAY WAS JUST BOOKED MOMENTS AGO!                     |
| Slot 09:00-10:00 on 16/05/2026 at Apex Moto Saigon Center reached  |
| full capacity. We automatically refreshed live bay availability —  |
| please pick one of the highlighted open windows (e.g. 10:00-11:00).|
+--------------------------------------------------------------------+

MOBILE (< 768px): Summary card collapses into a sticky bottom bar showing `16/05 • 09:00-10:00` + `[Confirm Booking ->]`; time-slot grid renders in 2 touch-friendly columns (`min-height: 64px`).
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<service-slot-picker>` -> `<vehicle-service-step>`, `<dealer-date-selector>`, `<time-slot-capacity-grid>`, `<voucher-eligibility-picker>`, `<booking-summary-sidebar>`.
- **`$ctrl` State**:
  - `$ctrl.draft = { customerVehicleId, serviceType: 'PeriodicMaintenance', odometerAtBookingKm, dealerId, slotDate, serviceTimeSlotId: null, slotRowVersion: null, appliedCustomerVoucherId: null, voucherRowVersion: null, reportedNotes: '' }`.
  - Selecting a slot copies **both** `slot.id` and `slot.rowVersion` (`Base64` concurrency token) into `$ctrl.draft`; selecting a voucher copies `voucher.id` and `voucher.rowVersion`.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Field Name | UI Control | Client & Server Validation Rules (`FR-04`, `EC-01`, `EC-04`) |
| :--- | :--- | :--- |
| `customerVehicleId` | Vehicle `<select>` | Required; must belong to `CurrentUser.UserId` and have no overlapping active (`Pending`, `Confirmed`, `CheckedIn`, `InProgress`) booking in the same slot (`UQ_ServiceBookings_ActiveVehicleSlot`). |
| `odometerAtBookingKm` | Numeric input (`km`) | Required integer `>= selectedVehicle.currentMileageKm` (`<= 500000`); pre-populated from `selectedVehicle.currentMileageKm`. Rejects rollback with `422 ODOMETER_REGRESSION`. |
| `serviceType` | Radio / `<select>` | Required; exact `CK_ServiceBookings_ServiceType`: `'PeriodicMaintenance'`, `'WarrantyRepair'`, `'GeneralRepair'`, `'Inspection'`, `'OilAndTire'`. |
| `dealerId` & `slotDate` | `<select>` + Date picker | Required active `Dealers.Id`; `slotDate >= today` (in `Asia/Ho_Chi_Minh` workshop date) and `slotStartUtc > UtcNow`. |
| `serviceTimeSlotId` + `slotRowVersion` | Capacity card grid | Required; cards compute `remaining = slot.maxCapacity - slot.bookedCount`: `remaining >= 2` -> `Available` (`emerald`), `remaining === 1` -> `Last Bay` (`amber`), `remaining <= 0 || slot.isBlocked` -> `Full / Closed` (`disabled`, non-selectable). |
| `appliedCustomerVoucherId` + `voucherRowVersion` | Filtered `<select>` | Optional; client filters `/api/v1/vouchers/my?status=Available` to items where `applicableScope IN ('ServiceOnly', 'All')` and `expiresAtUtc > now`, displaying `MinOrderAmount` notice (`"Requires >= 500.000 ₫ gross workshop invoice at settlement"`). |
| `reportedNotes` | `<textarea>` | Optional `NVARCHAR(1000)` (`maxlength="1000"` with character counter). |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/vehicles/my`, `GET /api/v1/dealers`, and `GET /api/v1/vouchers/my?status=Available` on `$onInit`.
  - `GET /api/v1/time-slots?dealerId={dealerId}&fromDate={slotDate}&toDate={slotDate}` whenever `dealerId` or `slotDate` changes.
  - `POST /api/v1/bookings` with `Idempotency-Key: <UUIDv4>` header (`EC-07`).
- **Graceful `409 Conflict` Auto-Refresh UX (`AC-04.1`, `EC-01`)**:
  - If `POST /api/v1/bookings` returns HTTP `409 Conflict` (`errorCode === 'SLOT_CAPACITY_EXHAUSTED'` or `'CONCURRENCY_CONFLICT'`), `$ctrl`:
    1. Preserves `$ctrl.draft.customerVehicleId`, `odometerAtBookingKm`, `serviceType`, `appliedCustomerVoucherId`, and `reportedNotes` so the customer **re-types zero form fields**;
    2. Clears the stale `serviceTimeSlotId` / `slotRowVersion`;
    3. Automatically invokes `GET /api/v1/time-slots` to re-render fresh bay counts with the full slot now greyed out (`Full`);
    4. Scrolls to the slot grid and displays the amber conflict recovery alert prompting selection of another open window.
- **Post-Commit Routing**: On `201 Created`, shows confirmation toast (`Booking SB-... submitted!`) and transitions to `app.customer.bookings` highlighting the new appointment.

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Initial wizard load or `GET /api/v1/time-slots` on date change | Slot grid displays 8-tile `<skeleton-loader>`; submit CTA remains disabled until a valid slot is chosen. |
| **`Empty`** | Customer owns `0` vehicles **OR** all slots on selected date are closed/holiday | If `0` vehicles: prompts `[Register Your Motorcycle VIN First ->]` (`app.customer.garage`). If `0` open slots on date: shows `[Jump to Next Available Date ->]` helper button. |
| **`Populated`** | Vehicles loaded and `>= 1` bookable slot exists | Interactive capacity cards (`Available`, `Last Bay`, `Full`), live sticky summary, and voucher selector. |
| **`Validation Error (400/422)`** | `422 ODOMETER_REGRESSION`, `422 VOUCHER_INELIGIBLE` (expired mid-session), or duplicate active booking on same VIN/slot | `ProblemDetailsMapper` highlights the offending step (`odometerAtBookingKm` or voucher dropdown) and refreshes voucher wallet if voucher expired. |
| **`Concurrency Conflict (409)`** | `409 SLOT_CAPACITY_EXHAUSTED` (`ServiceTimeSlots.RowVersion`) or `CustomerVouchers.RowVersion` conflict | Auto-reloads `/api/v1/time-slots` (and `/api/v1/vouchers/my` if voucher conflict), retains all other user inputs, and highlights remaining open slots. |

---

## 7. `SCR-CUS-04` — My Service Appointments, Live Status Stepper & Printable Service Receipts

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Real-time appointment tracker and itemized workshop receipt viewer where a `Customer` monitors active bookings across a 5-stage live status stepper (`Pending -> Confirmed -> CheckedIn -> InProgress -> Completed`), cancels `Pending`/`Confirmed` appointments (atomically releasing bay capacity and unexpired vouchers per `AC-04.3`), inspects finalized `ServiceInvoices` with itemized `ServiceInvoiceLineItems`, and opens **`[Print Receipt]`** via `<printable-receipt-modal>` (`RCP-SVC-*` with SHA-256 verification seal) (`FR-04`, `FR-05`, `FR-06`, `FR-08`).
- **UI-Router State(s)**: `app.customer.bookings` (`/customer/bookings`) + companion receipt view `app.customer.invoices` (`/customer/invoices`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Bookings.ViewOwn']` (`[Cancel Appointment]` guarded by `has-permission="Permissions.Bookings.CancelOwn"`; invoice drawer/tab guarded by `Permissions.Invoices.ViewOwn`).
- **Component Paths**:
  - `frontend/src/app/features/booking/customer-bookings-list.component.js` (`<customer-bookings-list>`)
  - `frontend/src/app/features/loyalty/customer-invoices.component.js` (`<customer-invoices>`)

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Live Status Stepper Cards + Invoice Detail Drawer)
+------------------------------------------------------------------------------------------------------+
| MY SERVICE APPOINTMENTS & WORKSHOP INVOICES                             [+ Book New Service Slot ->] |
+------------------------------------------------------------------------------------------------------+
| Tabs: [Active Appointments (1)]   [Past & Cancelled History (4)]   [Workshop Invoices (3)]           |
+------------------------------------------------------------------------------------------------------+
| +--------------------------------------------------------------------------------------------------+ |
| | SB-202605-0041 (.u-mono) • [⚙ In Service Bay]                 Apex Moto Saigon Center (DLR-HCM-01)| |
| | Date & Window : 16/05/2026 • 09:00 - 10:00 (Asia/Ho_Chi_Minh)                                    | |
| | Motorcycle    : Honda SH160i ABS Sport • VIN: RLHJF5109RZ104829 • Booking Odo: 3,920 km          | |
| | Voucher Held  : [ReservedOnBooking] SVC100K-7K9P2M4X (100.000 ₫ Off)                             | |
| |                                                                                                  | |
| | LIVE STEPPER:  (✓ Pending) ===> (✓ Confirmed) ===> (✓ CheckedIn) ===> (● InProgress) ---> (○ Done)| |
| |                                                                                                  | |
| | *(Cancel disabled once CheckedIn / InProgress)*                        [Live SignalR Sync Active]| |
| +--------------------------------------------------------------------------------------------------+ |
| +--------------------------------------------------------------------------------------------------+ |
| | SB-202605-0052 (.u-mono) • [● Pending Confirmation]            Apex Moto Saigon Center           | |
| | Date & Window : 23/05/2026 • 10:00 - 11:00 • PeriodicMaintenance                                 | |
| | LIVE STEPPER:  (● Pending) ---> (○ Confirmed) ---> (○ CheckedIn) ---> (○ InProgress) ---> (○ Done)| |
| |                                                                      [✕ Cancel Appointment]      | |
| +--------------------------------------------------------------------------------------------------+ |
+------------------------------------------------------------------------------------------------------+

MOBILE (< 768px): Horizontal stepper converts into a compact vertical timeline inside each card; invoice breakdown opens as a full-screen sheet.
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<customer-bookings-list>` -> `<booking-live-card>` -> `<booking-status-stepper status="booking.status">` + `<cancel-booking-modal>` + `<invoice-receipt-drawer>`.
- **Bindings**: `{{ ::booking.bookingNumber }}`, `{{ ::booking.vin }}`, `{{ ::booking.dealerName }}`, `{{ ::booking.slotDate }}` are one-time (`::`); `booking.status`, `booking.rowVersion`, and action button visibility remain bound so live SignalR transitions update in place.

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Action / Control | Field Name | Client & Server Validation Rules (`AC-04.3`, `EC-06`) |
| :--- | :--- | :--- |
| **Cancel Appointment Modal** | `cancellationReason` + `rowVersion` | Allowed **only** when `booking.status IN ('Pending', 'Confirmed')`. Requires optional/recommended `cancellationReason` (`maxLength: 300`) and passes `booking.rowVersion`. Confirmation dialog explicitly informs user: `"Workshop bay will be released and any unexpired reserved voucher will return to Available status."` |
| **View / Print Receipt (`v1.1.0`)** | `invoiceId` | Enabled when `booking.status === 'Completed'`; displays immutable `InvoiceNumber`, itemized `ServiceInvoiceLineItems` (`PartCodeSnapshot`, `Description`, `Quantity`, `UnitPriceVnd`, `LineTotalVnd`), `GrossAmount`, `VoucherDiscountAmount`, `NetPaidAmount`, `OdometerAtCompletionKm`, `+PointsEarned`, and **`[🖨 Print Receipt]`** mounting `<printable-receipt-modal receipt-type="SERVICE_INVOICE">` (`GET /api/v1/service-invoices/{invoiceId}/receipt` -> `RCP-SVC-*` + 16-char SHA-256 `verificationCode`). |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/bookings/my?status=&pageNumber=1&pageSize=20`
  - `POST /api/v1/bookings/{bookingId}/cancel` (decrements `ServiceTimeSlots.BookedCount` and restores `CustomerVouchers.Status` to `'Available'` if `UtcNow < ExpiresAtUtc`, else `'Expired'` per `EC-06`).
  - `GET /api/v1/service-invoices/my` and `GET /api/v1/service-invoices/{invoiceId}`.
- **SignalR Reactions (`BookingStatusChanged`)**:
  - When `DealerStaff` transitions the booking (`Confirmed`, `CheckedIn`, `InProgress`, `Completed`, `NoShow`), `SignalRClientService` dispatches `signalr:BookingStatusChanged` inside `$rootScope.$applyAsync()`.
  - `$ctrl` locates `bookingId` in `$ctrl.bookings`, advances the stepper pill with a subtle highlight animation, hides `[Cancel Appointment]` once status enters `CheckedIn`, and reveals `[View Final Invoice]` immediately upon `Completed`!

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching `/api/v1/bookings/my` or `/api/v1/service-invoices/my` | `<skeleton-loader variant="table-rows" rows="4">`. |
| **`Empty`** | Zero active or past bookings | Empty state card `"No workshop appointments scheduled yet"` + `[Book Maintenance Slot ->]`. |
| **`Populated`** | `>= 1` booking returned | Renders live stepper cards ordered by upcoming `SlotDate ASC` (active tab) or `CreatedAtUtc DESC` (history tab). |
| **`Validation Error (400/422)`** | `422 INVALID_STATUS_TRANSITION` (customer clicks cancel milliseconds after staff already checked the bike in) | Closes cancel modal, displays domain toast `"This motorcycle has already been checked in at the workshop"`, and refreshes booking row. |
| **`Concurrency Conflict (409)`** | `409 CONCURRENCY_CONFLICT` (`ServiceBookings.RowVersion` mismatch between customer cancel & staff status update) | Reloads `GET /api/v1/bookings/{bookingId}` automatically and updates the card's live status & `rowVersion`. |

---

## 8. `SCR-CUS-05` — Loyalty Wallet, 4-Tier Progress, Voucher Store & Printable Order Receipts

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Financial-grade customer rewards center displaying current membership tier (`Bronze`, `Silver`, `Gold`, `Platinum`), `PointsMultiplier`, spendable `AvailablePoints` vs. cumulative `LifetimeQualifyingPoints` progress bar, active `VoucherCatalogs` reward store with one-click point redemption (`33`-char `8CharUpperAlnumNoIOQ` voucher codes), owned `CustomerVouchers` wallet, the append-only `PointLedgerEntries` audit trail, and Showroom `SalesOrders` history with **`[🖨 Print Receipt]`** (`<printable-receipt-modal receipt-type="SALES_ORDER">`, `GET /api/v1/sales-orders/{orderId}/receipt` -> `RCP-SLS-*` + SHA-256 seal) (`FR-05`, `FR-08`, `ADR-003`).
- **UI-Router State(s)**: `app.customer.loyalty` (`/customer/loyalty`) + showroom purchase receipts sub-view `app.customer.orders` (`/customer/orders`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Loyalty.ViewOwn']` (`[Redeem Voucher]` buttons guarded by `has-permission="Permissions.Loyalty.RedeemVoucher"`).
- **Component Paths**:
  - `frontend/src/app/features/loyalty/loyalty-wallet-store.component.js` (`<loyalty-wallet-store>`)
  - `frontend/src/app/features/loyalty/customer-sales-orders.component.js` (`<customer-sales-orders>`)

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Tier Hero + 3-Tab Rewards & Immutable Ledger Workspace)
+------------------------------------------------------------------------------------------------------+
| [★ CELEBRATION BANNER — LIVE TIER UPGRADE]: Promoted from SILVER to GOLD (1.50x Multiplier)!     [✕] |
+------------------------------------------------------------------------------------------------------+
| +----------------------------------------------------------------+ +-------------------------------+ |
| | CURRENT MEMBERSHIP: [◆◆ SILVER MEMBER • 1.20x Point Multiplier]| | SPENDABLE AVAILABLE POINTS    | |
| | Lifetime Qualifying Points: 1,840 pts  (Never drops on redeem!)| |           640 pts             | |
| | Next Tier: GOLD (2,000 pts • 1.50x) — Only 160 qualifying pts! | | Earn Formula:                 | |
| | [██████████████████████░░] 92%  (Bronze 0 | Silver 500 |       | | Floor(Floor(VND/10k) * 1.20x) | |
| |                                  Gold 2,000 | Platinum 5,000)  | | Lifetime Spend: 15.830.000 ₫  | |
| +----------------------------------------------------------------+ +-------------------------------+ |
| Tabs: [1. Redeem Voucher Catalog (Active)]  [2. My Vouchers (3)]  [3. Point Ledger]  [4. Bike Orders]|
+------------------------------------------------------------------------------------------------------+
| +------------------------------------------------+ +-----------------------------------------------+ |
| | [ServiceOnly] • Min Tier: [◆ Bronze]           | | [ServiceOnly] • Min Tier: [★ Gold]            | |
| | 100.000 ₫ Off Periodic Maintenance             | | 20% Off Major Overhaul (Max 300.000 ₫)        | |
| | Min Gross Spend: 500.000 ₫ • Valid: 90 Days    | | Min Gross Spend: 1.000.000 ₫ • Valid: 90 Days | |
| | Quota Remaining: 58 / 100                      | | Quota Remaining: 25 / 50                      | |
| | Cost: 300 pts (.u-mono)                        | | Cost: 600 pts (.u-mono)                       | |
| | [   Redeem for 300 Available Points   ]        | | [ 🔒 Requires Gold Tier (2,000+ Qual. Pts) ]  | |
| +------------------------------------------------+ +-----------------------------------------------+ |
|                                                                                                      |
| TAB 3 PREVIEW — IMMUTABLE POINT LEDGER (dbo.PointLedgerEntries — Read-Only Financial Statement):     |
| Date (VN Time)   | Tx Type            | Reference         | Points Delta | Balance After | Notes     |
| 16/05/26 16:54   | ServiceInvoiceEarn | INV-202605-0118   |      +99 pts |       640 pts | 830k @1.2x|
| 14/05/26 11:20   | VoucherRedeem      | SVC100K-7K9P2M4X  |     -200 pts |       541 pts | Redeemed  |
+------------------------------------------------------------------------------------------------------+

MOBILE (< 768px): Tier Hero stacks vertically above horizontally scrollable tabs; Point Ledger rows collapse into crisp statement cards with green `+pts` / slate `-pts` tabular pills.
```

### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<loyalty-wallet-store>` -> `<tier-progress-hero>`, `<tier-upgrade-celebration-banner>`, `<voucher-catalog-grid>`, `<my-customer-vouchers-list>`, `<point-ledger-table>`, `<confirm-modal>`.
- **State**: `$ctrl.account` (includes `availablePoints`, `lifetimeQualifyingPoints`, `currentTierCode`, `pointsMultiplier`, `rowVersion`), `$ctrl.tiers` (`Bronze` `0/1.00x`, `Silver` `500/1.20x`, `Gold` `2000/1.50x`, `Platinum` `5000/2.00x`), `$ctrl.activeTab = 'catalog'`.
- **One-Time Bindings (`::`)**: Every row in `<point-ledger-table>` is strictly append-only (`ADR-003`), so **100% of ledger row cells** (`{{ ::entry.createdAtUtc | utcToLocal }}`, `{{ ::entry.transactionType }}`, `{{ ::entry.pointsDelta }}`, `{{ ::entry.balanceAfter }}`, `{{ ::entry.notes }}`) use `::` for zero watcher overhead!

### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Control / Action | Payload Fields | Client Eligibility & Server Invariants (`AC-05.2`, `AC-05.3`, `EC-11`) |
| :--- | :--- | :--- |
| **Redeem Catalog Card** | `{ voucherCatalogId, accountRowVersion }` | Button enabled **only** when: (1) `catalog.isActive === true`, (2) `account.currentTierRank >= catalog.minTierRank`, (3) `account.availablePoints >= catalog.pointsCost`, and (4) `catalog.quotaTotal === null || catalog.quotaRedeemed < catalog.quotaTotal`. Ineligible cards show explicit disabled reason (`"Need 150 more Available Points"` or `"Requires Gold Tier"`). Confirmation modal emphasizes: `"Deducts 300 Available Points -> Remaining: 340 pts. Your Lifetime Qualifying Points (1,840 pts) and Silver Tier will NOT decrease."` |
| **Copy / Apply My Voucher** | `voucherCode` | On `My Vouchers` tab, `'Available'` vouchers render `.u-mono` code + `[Copy Code]` + `[Use on New Booking ->]` (`$state.go('app.customer.bookService', { voucherId: v.id })`). |
| **Ledger Filter** | `transactionType`, `pageNumber`, `pageSize` | Filters `PointLedgerEntries` by `'SalesOrderEarn'`, `'ServiceInvoiceEarn'`, `'VoucherRedeem'`, `'VoucherExpireRefund'`, `'AdminAdjustment'`, `'WelcomeBonus'`. |

### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/loyalty/account`, `GET /api/v1/loyalty/tiers`, `GET /api/v1/vouchers/catalog`
  - `POST /api/v1/vouchers/redeem` (with `Idempotency-Key` + `accountRowVersion`) -> mints `CustomerVoucher` (`Status = 'Available'`), appends negative `PointLedgerEntries` row, updates `$ctrl.account.rowVersion` & `availablePoints`.
  - `GET /api/v1/vouchers/my?status=` and `GET /api/v1/loyalty/ledger?pageNumber=1&pageSize=20` (and `GET /api/v1/sales-orders/my` on Tab 4).
- **SignalR Reactions (`LoyaltyPointsUpdated` & `TierUpgraded`)**:
  - When `DealerStaff` settles a `SalesOrder` or `ServiceInvoice` while the customer has `SCR-CUS-05` open, `$rootScope.$applyAsync()` updates `availablePoints` and `lifetimeQualifyingPoints` live, prepends/refreshes the top ledger page, and—when `TierUpgraded` fires—displays the gold/indigo celebratory promotion banner + modal showing the new `PointsMultiplier` (`1.20x` / `1.50x` / `2.00x`) and newly unlocked tier-gated vouchers!

### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Initial load of wallet/catalog or tab switch to Ledger | Hero skeleton + 4-card `<skeleton-loader variant="card-grid">`. |
| **`Empty`** | `0` redeemed `CustomerVouchers` on Tab 2 or `0` ledger entries on brand-new `Bronze` wallet | Tab 2: `"You have no redeemed vouchers yet — browse the Catalog tab!"` Tab 3: `"Earn 1 base point per 10,000 VND spent on motorcycles and workshop maintenance."` |
| **`Populated`** | Active loyalty account + catalog items | Full tier progress bar with 4 milestone ticks (`0`, `500`, `2,000`, `5,000`), interactive redemption cards, status-badged `CustomerVouchers`, and paginated ledger. |
| **`Validation Error (400/422)`** | `422 INSUFFICIENT_LOYALTY_POINTS` or `422 VOUCHER_INELIGIBLE` | Closes confirmation modal, shows `ProblemDetails.detail` banner, and refreshes `GET /api/v1/loyalty/account`. |
| **`Concurrency Conflict (409)`** | `409 CONCURRENCY_CONFLICT` (`LoyaltyAccounts.RowVersion` changed due to simultaneous invoice point credit) or `409 VOUCHER_QUOTA_EXHAUSTED` (`EC-11`) | Reloads `GET /api/v1/loyalty/account` + `GET /api/v1/vouchers/catalog` automatically; if quota exhausted, marks card `Sold Out` with zero points deducted; if `RowVersion` refreshed with higher points, prompts user to confirm retry with updated balance. |

---

## 9. `SCR-CUS-06` — Showroom Test-Drive Scheduling & Live Status Tracker (`v1.1.0`)

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Allows an authenticated `Customer` to schedule a 45-minute showroom test-drive appointment (`TD-YYYYMMDD-XXXXXX`) for any active `MotorcycleModel` at an active `Dealer`, pre-filling `$stateParams.modelId` and `$stateParams.dealerId` from `SCR-PUB-01` (`[Book Test Drive]`), monitor live advisor confirmation and assigned demo `VIN` updates via SignalR `TestDriveStatusChanged` with zero page reloads, and cancel `Requested`/`Confirmed` appointments under `RowVersion` concurrency protection (`FR-02`, `FR-06`, `FR-08`, `ADR-002`, `ADR-004`, `ADR-005`).
- **UI-Router State**: `app.customer.testDrives` (`/customer/test-drives?modelId&dealerId`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.TestDrives.ViewOwn', 'Permissions.TestDrives.Create']` (`[✕ Cancel Test Drive]` guarded by `has-permission="Permissions.TestDrives.CancelOwn"`).
- **Component Path**: `frontend/src/app/features/customer/test-drives.component.js` (`<customer-test-drives>`).

### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 5-Col Test-Drive Booking Form + 7-Col Live Test-Drive Tracker)
+------------------------------------------------------------------------------------------------------+
| SHOWROOM TEST-DRIVE SCHEDULING & LIVE TRACKER                              [← Browse Showroom Models] |
+--------------------------------------------------+---------------------------------------------------+
| SCHEDULE A SHOWROOM TEST RIDE                    | MY TEST-DRIVE APPOINTMENTS (Live SignalR Sync)    |
| ------------------------------------------------ | ------------------------------------------------- |
| Dealership *    : [ Apex Moto Saigon Center  v ] | +-----------------------------------------------+ |
| Motorcycle *    : [ Honda SH160i ABS Sport   v ] | | TD-20260518-A4F91C • [✓ Confirmed]            | |
| Preferred Date *: [ 2026-05-18               v ] | | Honda SH160i ABS Sport • Saigon Center        | |
| Time Window *   : [ 10:00 - 10:45 (45 min)   v ] | | Date & Window: 18/05/2026 • 10:00 - 10:45     | |
| Driver License *: [ 790198004512               ] | | Assigned Demo VIN: RLHJC9204RY000309 (.u-mono)| |
| Contact Phone * : [ 0909123456                 ] | | Stepper: (✓ Requested)->(● Confirmed)->(○ Done)| |
| Notes (Optional): [ Compare ABS & seat height  ] | |                         [✕ Cancel Test Drive] | |
|                                                  | +-----------------------------------------------+ |
| [        Confirm Test-Drive Request ->         ] |                                                   |
+--------------------------------------------------+---------------------------------------------------+
```

### c) Component Hierarchy, Validation Rules & SignalR Reactions
- **`$ctrl` State & Pre-Selection**: Reads `$stateParams.modelId` and `$stateParams.dealerId` on `$onInit`, loads `GET /api/v1/dealers`, `GET /api/v1/motorcycles`, and `GET /api/v1/test-drives/my`.
- **Validation Rules**:
  - `dealerId` & `motorcycleModelId`: Required active GUIDs.
  - `preferredDate` & `startTime` (`45m` window): Future slot inside dealership operating hours; server enforces `<= 2` active (`Requested`/`Confirmed`) test drives per `(DealerId, MotorcycleModelId, PreferredDate, StartTime)` (`409 TEST_DRIVE_SLOT_FULL`) and zero customer overlap (`409 TEST_DRIVE_OVERLAP`).
  - `driverLicenseNumber` (`^[A-Za-z0-9 -]{6,30}$`) & `contactPhone` (`BranchPhoneRegex` `^(\+84|0)[0-9]{8,11}$`).
- **REST & SignalR Integration (`$onDestroy` Cleaned)**:
  - Submits `POST /api/v1/test-drives` and cancels via `POST /api/v1/test-drives/{testDriveId}/cancel` (with `rowVersion`).
  - Subscribes to `SignalRClientService.on('TestDriveStatusChanged', ...)` and `signalr:reconnected` inside `$rootScope.$applyAsync()`, updating `status`, `assignedVin`, and `rowVersion` live with zero page reloads and disposing all listeners in `$ctrl.$onDestroy()`.

### d) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Hydrating dealers, models, and `/api/v1/test-drives/my` | Skeleton form & appointment list placeholders. |
| **`Empty`** | Customer has `0` scheduled test drives | Prompt `"Select any motorcycle model on the left to schedule a 45-minute dealership test ride."` |
| **`Populated`** | `>= 1` `TestDriveBookings` row | Status-badged cards showing `TD-*`, window, assigned demo `VIN`, and conditional `[✕ Cancel Test Drive]` button. |
| **`Validation Error (400/422)`** | Invalid license/phone or `422 INVALID_STATUS_TRANSITION` | Inline `ProblemDetailsMapper` field messages without clearing user inputs. |
| **`Concurrency Conflict (409)`** | `409 TEST_DRIVE_SLOT_FULL`, `409 TEST_DRIVE_OVERLAP`, or `RowVersion` mismatch on cancel | Preserves selected model, license, and notes, displays contextual conflict alert, and refreshes `/api/v1/test-drives/my` automatically. |

---

## 10. `SCR-CUS-07` — Post-Service 5-Star CSAT Reviews (`+25` Bonus Points) & Factory VIN Warranty Claims (`v1.2.0`)

### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Unified customer quality & warranty portal (`FR-09`, `ADR-002`, `ADR-003`, `ADR-004`, `ADR-005`) enabling `Customer` users to: (1) submit a 1-to-5 star CSAT `ServiceReview` on any owned settled `ServiceInvoice` (`POST /api/v1/service-reviews`) to earn **`+25` flat spendable `AvailablePoints`** (`SERVICE_REVIEW:{InvoiceId}:BONUS`) while viewing branch `StaffResponse` replies, and (2) check `36-month / 30,000-km` factory warranty eligibility per `CustomerVehicle` (`VIN`), file `WarrantyClaims` (`POST /api/v1/warranty-claims`), and track live status transitions via SignalR `WarrantyClaimStatusChanged`.
- **UI-Router State**: `app.customer.warranty` (`/customer/warranty?vehicleId&invoiceId`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Customer']`, `permissions: ['Permissions.Warranty.ViewOwn', 'Permissions.Warranty.CreateOwn', 'Permissions.Reviews.ViewOwn']` (review submission gated by `Permissions.Reviews.CreateOwn`).
- **Component Path**: `frontend/src/app/features/customer/warranty.component.js` (`<customer-warranty>`).

### b) Key Interactions, Validation & SignalR Teardown
- **CSAT Review Form**: `overallRating`, `technicianQualityRating`, `advisorHospitalityRating` (`1..5`), `wouldRecommend` (`boolean`), and `comment` (`<= 1000` chars); handles `409 REVIEW_ALREADY_SUBMITTED` gracefully and updates navbar `AvailablePoints` (`+25`, `LifetimeQualifyingPoints` unchanged) on `LoyaltyPointsUpdated`.
- **VIN Warranty Claim Form**: `dealerId`, `customerVehicleId` (pre-filled from `$stateParams.vehicleId`), `defectCategory`, `mileageAtClaimKm` (`>= currentMileageKm`), and `symptomDescription` (`10..1000` chars); surfaces `422 WARRANTY_COVERAGE_EXPIRED` and `409 ACTIVE_WARRANTY_CLAIM_EXISTS`, and disposes all `SignalRClientService` listeners in `$ctrl.$onDestroy()`.
