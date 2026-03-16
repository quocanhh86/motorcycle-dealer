# Dealer Staff & Admin Portal Screen Specifications (`SCR-STF-01`..`SCR-ADM-08`)

| Metadata | Details |
| :--- | :--- |
| **Document ID** | `UX-OPSADM-MDLMP-2026-V1.2` |
| **Version** | `1.2.0` (**Implemented & Verified in Production Code** — `30` Navigable Routes including `SCR-STF-07` & `SCR-ADM-08`) |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **Repository** | [https://github.com/quocanhh86/motorcycle-dealer](https://github.com/quocanhh86/motorcycle-dealer) |
| **Covered Screens** | `SCR-STF-01`..`SCR-STF-07` (`DealerStaff` Scoped Portal) · `SCR-ADM-01`..`SCR-ADM-08` (`Admin` HQ Portal) |
| **Frontend Target Stack** | AngularJS `1.8.3` (`ng-strict-di`, `@uirouter/angularjs`, `Chart.js` v4, `@microsoft/signalr`, `<printable-receipt-modal>`, `controllerAs: '$ctrl'`) |
| **Traceability** | `FR-01`..`FR-09` · `ADR-001`..`ADR-006` · `RISK-01`..`RISK-06` ([PRD.md](../PRD.md), [DESIGN.md](../DESIGN.md), [DATABASE_DESIGN.md](../DATABASE_DESIGN.md), [DD_04_SCREEN_SPECIFICATIONS.md](../detail-design/DD_04_SCREEN_SPECIFICATIONS.md)) |
| **Companion Specs** | [DESIGN_SYSTEM_AND_NAVIGATION.md](./DESIGN_SYSTEM_AND_NAVIGATION.md) · [CUSTOMER_PORTAL_SCREENS.md](./CUSTOMER_PORTAL_SCREENS.md) |

---

## Table of Contents

1. [Part I — Dealer Staff Operational Screens (`IDealerScoped` Branch Isolation)](#part-i--dealer-staff-operational-screens-idealerscoped-branch-isolation)
   - [`SCR-STF-01` — Zero-Refresh SignalR Live Booking Workbench & VIN Check-In](#1-scr-stf-01--zero-refresh-signalr-live-booking-workbench--vin-check-in)
   - [`SCR-STF-02` — Workshop Service Invoice Builder, OEM Spare-Parts Picker & Official Receipt](#2-scr-stf-02--workshop-service-invoice-builder-oem-spare-parts-picker--official-receipt)
   - [`SCR-STF-03` — Showroom VIN Inventory, Bike Sales Checkout Desk & Official Receipt](#3-scr-stf-03--showroom-vin-inventory-bike-sales-checkout-desk--official-receipt)
   - [`SCR-STF-04` — Branch Workshop Bay & Service Time-Slot Capacity Manager](#4-scr-stf-04--branch-workshop-bay--service-time-slot-capacity-manager)
   - [`SCR-STF-05` — Branch Customer, VIN Garage & Loyalty Lookup](#5-scr-stf-05--branch-customer-vin-garage--loyalty-lookup)
   - [`SCR-STF-06` — Zero-Refresh SignalR Showroom Test-Drive Workbench (`v1.1.0`)](#6-scr-stf-06--zero-refresh-signalr-showroom-test-drive-workbench-v110)
2. [Part II — Admin Network Governance & `Chart.js` Analytics Screens](#part-ii--admin-network-governance--chartjs-analytics-screens)
   - [`SCR-ADM-01` — Executive Multi-Dealer `Chart.js v4` Analytics Dashboard](#7-scr-adm-01--executive-multi-dealer-chartjs-v4-analytics-dashboard)
   - [`SCR-ADM-02` — Multi-Dealer Branch Network Governance](#8-scr-adm-02--multi-dealer-branch-network-governance)
   - [`SCR-ADM-03` — Dealer Staff Provisioning & Access Governance](#9-scr-adm-03--dealer-staff-provisioning--access-governance)
   - [`SCR-ADM-04` — Master Motorcycle Catalog & OEM Maintenance Rules Studio](#10-scr-adm-04--master-motorcycle-catalog--oem-maintenance-rules-studio)
   - [`SCR-ADM-05` — Loyalty Tiers Studio & Audited Point Adjustments](#11-scr-adm-05--loyalty-tiers-studio--audited-point-adjustments)
   - [`SCR-ADM-06` — Reward Voucher Catalog & Quota Studio](#12-scr-adm-06--reward-voucher-catalog--quota-studio)
   - [`SCR-ADM-07` — OEM Spare-Parts Master Catalog Studio (`v1.1.0`)](#13-scr-adm-07--oem-spare-parts-master-catalog-studio-v110)

---

## Part I — Dealer Staff Operational Screens (`IDealerScoped` Branch Isolation)

> [!IMPORTANT]
> **Multi-Branch Tenant Invariant (`ADR-002`)**: Every `DealerStaff` session is cryptographically bound to a single `DealerId` in its JWT (`dealer_id` claim, `18` `DealerStaff` permissions). None of `SCR-STF-01`..`SCR-STF-06` expose a branch switcher dropdown to `DealerStaff`; the header displays a read-only locked branch badge (e.g., `DLR-HCM-01 • Apex Moto Saigon Center`) and the server enforces `IDealerScoped` global query filters and write guards across all `6` dealer-owned aggregates (`DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`).

---

### 1. `SCR-STF-01` — Zero-Refresh SignalR Live Booking Workbench & VIN Check-In

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Real-time 8-hour shift operational Kanban board for dealership service advisors (`FR-04`, `FR-06`, `BO-02`). Displays incoming customer `ServiceBookings` instantly via SignalR (`BookingCreated`) **without browser page refresh**, supports physical 17-character `VIN` intake verification (`CheckedIn`), manages workshop bay status transitions (`Confirmed`, `InProgress`, `Cancelled`, `NoShow`), and hands off completed bay work to `SCR-STF-02` (`ServiceInvoices`).
- **UI-Router State(s)**: `app.staff.workbench` (`/staff/workbench`) + slide-over/modal sub-state `app.staff.checkin` (`/staff/bookings/:bookingId/check-in`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.Bookings.ViewDealer']` (transition buttons guarded by `has-permission="Permissions.Bookings.UpdateStatus"`, check-in VIN lookup by `Permissions.Vehicles.ViewDealer`, and `[Issue Invoice ->]` by `Permissions.Invoices.CreateDealer`).
- **Component Paths**:
  - `frontend/src/app/features/staff-workbench/staff-live-workbench.component.js` (`<staff-live-workbench>`)
  - `frontend/src/app/features/staff-workbench/staff-vin-checkin.component.js` (`<staff-vin-checkin>`)
  - `frontend/src/app/features/staff-workbench/live-booking-queue.store.js` (`LiveBookingQueueStore`)

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 4-Column Zero-Refresh Kanban Workbench + VIN Check-In Slide-Over)
+------------------------------------------------------------------------------------------------------+
| LIVE WORKSHOP WORKBENCH • DLR-HCM-01 (Saigon Center)     [● SignalR: Connected]   [Manage Bay Slots] |
+------------------------------------------------------------------------------------------------------+
| [⚡ LIVE PUSH • 2s ago]: New Booking SB-202605-0041 • Honda SH160i (RLHJF5109RZ104829) • 09:00-10:00 |
+------------------------------------------------------------------------------------------------------+
| Date: [ 2026-05-16 ] [+0 Other Dates] | Slot: [All Windows v] | Filter VIN/Plate: [           ] [🔊] |
+-------------------------+-------------------------+-------------------------+------------------------+
| ● PENDING (2)           | ✓ CONFIRMED / CHECK-IN  | ⚙ IN SERVICE BAY (1)    | ✓✓ COMPLETED TODAY (6) |
+-------------------------+-------------------------+-------------------------+------------------------+
| +---------------------+ | +---------------------+ | +---------------------+ | +--------------------+ |
| | [NEW • Just now] 🔴 | | | SB-202605-0038      | | | SB-202605-0031      | | | SB-202605-0024     | |
| | SB-202605-0041      | | | 08:00-09:00 [✓ Conf]| | | 08:00-09:00 [⚙ Bay2]| | | INV-202605-0117    | |
| | 09:00 - 10:00       | | | Yamaha Exciter 155  | | | Honda Air Blade 160 | | | Paid: 1.240.000 ₫  | |
| | Minh Nguyen         | | | VIN: RLCUG1010PY... | | | VIN: RLHKF4102PY... | | | +186 pts (Gold)    | |
| | Honda SH160i ABS    | | | Odo: 11,200 km      | | | CheckedIn: 08:04    | | | [Receipt]          | |
| | RLHJF5109RZ104829   | | | [⇥ Check-In VIN]    | | | [Issue Invoice ->]  | | +--------------------+ |
| | 🎟 Voucher Reserved | | | [No-Show]  [Cancel] | | +---------------------+ |                        |
| | [✓ Confirm] [Cancel]| | +---------------------+ |                         |                        |
| +---------------------+ |                         |                         |                        |
+-------------------------+-------------------------+-------------------------+------------------------+

SLIDE-OVER DRAWER — PHYSICAL VIN CHECK-IN (app.staff.checkin — 460px right overlay):
+--------------------------------------------------------------------+
| WORKSHOP BAY CHECK-IN • SB-202605-0038                         [✕] |
+--------------------------------------------------------------------+
| Expected VIN (.u-mono) : RLCUG1010PY092341  [x] Physical VIN Match |
| Customer & Tier        : Tran Quoc Bao • [★ Gold • 1.50x]          |
| Booking Odometer       : 11,200 km                                 |
| Verified Intake Odo *  : [ 11,218 ] km (Must be >= 11,200 km)      |
| Reserved Voucher       : SVC100K-7K9P2M4X (Available / Valid ✓)    |
| Due OEM Milestones     : 12,000 km Major Service (Due in 782 km)   |
| Advisor Bay / Intake   : [ Bay #3 — Technician Hung assigned     ] |
|                               [Cancel]  [Confirm Check-In & Bay ->]|
+--------------------------------------------------------------------+

TABLET / MOBILE (< 1024px): 4 Kanban lanes switch to horizontal scroll-snap columns with sticky column count tabs (`Pending (2) | Confirmed (3) | In Bay (1) | Done (6)`).
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<staff-live-workbench>` -> `<workbench-filter-bar>`, `<kanban-lane status="'Pending'">` .. `<kanban-lane status="'Completed'">` -> `<staff-booking-card>` (`track by booking.bookingId`) + `<staff-vin-checkin>`.
- **Zero-Reset Filter & Scroll Preservation Design**:
  - `LiveBookingQueueStore` holds normalized booking entities keyed by `bookingId` plus sorted lane arrays (`pendingIds`, `confirmedAndCheckedInIds`, `inProgressIds`, `completedIds`).
  - Client-side filter predicates (`slotId`, `searchVinOrPlate`) run over `LiveBookingQueueStore` without mutating or resetting `$ctrl.filters` or DOM `laneElement.scrollTop`.
  - When a new `BookingCreated` frame arrives:
    - If `payload.slotDate === $ctrl.filters.slotDate`: `LiveBookingQueueStore.prependIncomingBooking(payload)` inserts the card at index `0` of `pendingIds` with `_highlightUntil = Date.now() + 12000`. If the advisor has scrolled down inside the `Pending` lane (`scrollTop > 40`), a floating pill `[↑ 1 New Booking Arrived]` appears at the top of the lane rather than jarring the advisor's scroll position!
    - If `payload.slotDate !== $ctrl.filters.slotDate`: increments `$ctrl.otherDateIncomingCount` badge next to the date picker (`[+1 New on 2026-05-18]`) and displays the global SignalR toast.
- **One-Time Bindings (`::`)**: `{{ ::booking.bookingNumber }}`, `{{ ::booking.vin }}`, `{{ ::booking.customerFullName }}`, `{{ ::booking.motorcycleModelName }}`, `{{ ::booking.startTime }}`.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Action / Field | Target Endpoint / Field | Client & Server Validation Rules (`FR-04`, `FR-06`) |
| :--- | :--- | :--- |
| **Confirm Booking** | `newStatus = 'Confirmed'`, `rowVersion` | Valid only from `'Pending'`; requires `booking.rowVersion`. |
| **VIN Check-In Drawer** | `verifiedVinConfirmed`, `intakeOdometerKm`, `advisorNotes`, `rowVersion` | Transitions `'Confirmed'` (or walk-in `'Pending'`) -> `'CheckedIn'` (and optionally straight to `'InProgress'`). Requires checkbox confirming physical 17-char `VIN` matches `booking.vin` and `intakeOdometerKm >= booking.odometerAtBookingKm` (`422 ODOMETER_REGRESSION` if lower). |
| **Start Bay Work** | `newStatus = 'InProgress'`, `rowVersion` | Valid from `'CheckedIn'` (or `'Confirmed'`). Enables `[Issue Invoice ->]` CTA (`$state.go('app.staff.invoicing', { bookingId: booking.bookingId })`). |
| **Mark No-Show / Cancel** | `newStatus = 'NoShow' \| 'Cancelled'`, `cancellationReason` | Valid prior to `'InProgress'`; `'Cancelled'` requires `cancellationReason` (`5..300` chars) inside `confirmModal`, atomically decrementing `ServiceTimeSlots.BookedCount` and releasing any unexpired `ReservedOnBooking` voucher. |

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/bookings/dealer?slotDate={yyyy-MM-dd}&status=` and `GET /api/v1/time-slots/dealer?slotDate={yyyy-MM-dd}` on init / date change.
  - `GET /api/v1/bookings/{bookingId}` + `GET /api/v1/vehicles/{vehicleId}/maintenance-history` when opening VIN Check-In drawer.
  - `PATCH /api/v1/bookings/{bookingId}/status` (with `rowVersion`).
- **Real-Time SignalR Reactions (`/hubs/notifications` -> `dealer-{dealerId}`)**:
  1. `BookingCreated`: Wrapped in `$rootScope.$applyAsync()`, prepends card to `Pending` lane in `< 500 ms` (`BO-02`), plays optional subtle notification chime (toggleable `[🔊]`), shows toast, and increments `notificationBell`.
  2. `BookingStatusChanged`: Synchronizes status transitions made by peer `DealerStaff` workstations at the **same** dealership or customer self-cancellations (`Pending -> Cancelled` removes card from `Pending` lane and shows `"Cancelled by Customer"` pill).
  3. `onreconnected`: Automatically re-fetches `GET /api/v1/bookings/dealer?slotDate={current}` and reconciles by `bookingId` + `rowVersion` (`EC-09`).
  4. `$ctrl.$onDestroy`: Deregisters all `signalr:BookingCreated` and `signalr:BookingStatusChanged` listeners (`RISK-04`).

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Initial load or date-picker change | 4-column `<skeleton-loader variant="kanban" rows="3">`. |
| **`Empty`** | Zero bookings scheduled on selected date | Displays `"0 Appointments Scheduled"` across lanes with live status indicator `[● SignalR Listening for Incoming Bookings...]`. |
| **`Populated`** | `>= 1` booking on selected date | 4 status lanes with live counts, color-coded service type & reserved-voucher pills, and one-click workflow transitions. |
| **`Validation Error (400/422)`** | `422 INVALID_STATUS_TRANSITION` or `422 ODOMETER_REGRESSION` in Check-In drawer | Displays inline `ProblemDetails.detail` inside the drawer without closing it. |
| **`Concurrency Conflict (409)`** | Two advisors click `[Confirm]` on the same card (`ServiceBookings.RowVersion` mismatch) or customer cancelled simultaneously | Catches `409 CONCURRENCY_CONFLICT`, reloads `GET /api/v1/bookings/{bookingId}`, moves card smoothly to its updated lane, and shows toast `"Updated by colleague / customer"`. |

---

### 2. `SCR-STF-02` — Workshop Service Invoice Builder, OEM Spare-Parts Picker & Official Receipt

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Financial settlement, **OEM Spare-Parts line-item builder** (`v1.1.0`), loyalty preview calculator, and **Official Printable Receipt** console for completed workshop jobs (`FR-04`, `FR-05`, `FR-08`, `ADR-003`). Queries model-compatible OEM `SpareParts` (`GET /api/v1/spare-parts?compatibleModelId={booking.motorcycleModelId}`), auto-sums `PartsAmount` + `LaborAmount == GrossAmount` while preserving mid-edit `draftLineItems` across background SignalR events, validates and caps any attached `CustomerVoucher`, previews exact **two-step floor** `PointsEarned` and `LoyaltyTiers` promotion in real time, atomically issues `ServiceInvoices` + immutable `ServiceInvoiceLineItems`, and opens **`[🖨 Print Official Receipt]`** (`<printable-receipt-modal>` `RCP-SVC-*` with SHA-256 verification seal).
- **UI-Router State**: `app.staff.invoiceBuilder` (alias `app.staff.invoicing`, URL `/staff/bookings/:bookingId/invoice`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.Invoices.CreateDealer', 'Permissions.Invoices.ViewDealer', 'Permissions.Loyalty.ViewDealerCustomer']`.
- **Component Path**: `frontend/src/app/features/staff/invoice-builder.component.js` (`<staff-invoice-builder>`) + `shared/printable-receipt-modal.component.js` (`<printable-receipt-modal>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 7-Col Invoice Editor + 5-Col Live Financial & Loyalty Settlement Preview)
+------------------------------------------------------------------------------------------------------+
| ISSUE WORKSHOP SERVICE INVOICE • Booking SB-202605-0041                  [<- Back to Live Workbench] |
+------------------------------------------------------------------+-----------------------------------+
| 1. VEHICLE & COMPLETION ODOMETER                                 | LIVE SETTLEMENT & LOYALTY PREVIEW |
| Customer : Minh Nguyen • Tier: [◆◆ Silver • 1.20x Multiplier]    | --------------------------------- |
| Bike/VIN : Honda SH160i ABS • RLHJF5109RZ104829 (.u-mono)        | Labor Charges   :     250.000 ₫   |
| Intake   : 3,920 km  |  Exit Odometer (km) *: [ 3,935 ]          | Genuine Parts   :     450.000 ₫   |
| ---------------------------------------------------------------- | Gross Subtotal  :     700.000 ₫   |
| 2. ITEMIZED CHARGES (DECIMAL(18,2) VND)                          | Voucher Applied :    -100.000 ₫   |
| Labor Charges (VND) * : [ 250000 ]                               |   (SVC100K-7K9P2M4X -> Used)      |
| Parts & Fluids (VND) *: [ 450000 ]                               | ================================= |
| Optional Line Items Breakdown (+ Add Part/Operation Row):        | NET PAID AMOUNT :     600.000 ₫   |
|   • Synthetic 10W-30 Oil (1L)        180.000 ₫          [Remove] | ================================= |
|   • Genuine Air Filter & Belt Clean  270.000 ₫          [Remove] | TWO-STEP FLOOR LOYALTY ORACLE:    |
| ---------------------------------------------------------------- | Step 1 Base Pts : Floor(600k/10k) |
| 3. CUSTOMER VOUCHER & PAYMENT METHOD                             |                 = 60 Base Pts     |
| Voucher: [ SVC100K-7K9P2M4X • 100.000 ₫ Off (Min 500k) ✓ Valid v]| Step 2 Tier x   : Floor(60 * 1.20)|
| Payment Method *: (•) BankTransfer  ( ) Cash  ( ) Card  ( ) EWal | POINTS EARNED   : +72 Points      |
| Technician Completion Notes *:                                   | --------------------------------- |
| [ 4,000 km periodic service completed; next due at 8,000 km.   ] | Qual. Pts After : 1,960 -> 2,032  |
|                                                                  | [★ TIER UPGRADE PREVIEW]:         |
|                                                                  | Promotes Silver -> GOLD (1.50x)!  |
|                                                                  | [  Finalize & Settle Invoice -> ] |
+------------------------------------------------------------------+-----------------------------------+

MOBILE / TABLET (< 1024px): Live Settlement & Loyalty Preview docks below the charge inputs with a sticky bottom bar showing `Net: 600.000 ₫ • +72 pts` + `[Settle Invoice]`.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<staff-invoice-builder>` -> `<booking-vehicle-banner>`, `<invoice-charges-form>`, `<voucher-settlement-selector>`, `<loyalty-settlement-preview-card>`, `<confirm-modal>`.
- **Client-Side Deterministic Calculation (`recalculatePreview()`)**:
  ```javascript
  // Exact mirror of backend C# & baseline-spec-snapshot.json formula (zero float drift via integer VND cents/units)
  var grossVnd = Math.max(0, Number($ctrl.form.laborAmount || 0) + Number($ctrl.form.partsAmount || 0));
  var discountVnd = computeEligibleVoucherDiscount($ctrl.selectedVoucher, grossVnd); // 0 if grossVnd < minOrderAmount
  var netPaidVnd = Math.max(0, grossVnd - discountVnd);
  var basePoints = Math.floor(netPaidVnd / 10000);
  var pointsEarned = Math.floor(basePoints * $ctrl.customerLoyalty.pointsMultiplier);
  ```

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Field Name | Type & DB Mapping | Client & Server Validation Rules (`AC-05.1`, `EC-05`, `EC-07`) |
| :--- | :--- | :--- |
| `laborAmount`, `partsAmount` | Numeric `DECIMAL(18,2)` (`VND`) | Required `>= 0`; `laborAmount + partsAmount > 0` (at least one positive charge); rejects negatives or `NaN`. |
| `odometerAtCompletionKm` | Integer (`km`) | Required integer `>= Math.max(booking.odometerAtBookingKm, vehicle.currentMileageKm)` (`422 ODOMETER_REGRESSION` if lower). |
| `appliedCustomerVoucherId` | `<select>` (`UUID` nullable) | Pre-selects `booking.appliedCustomerVoucherId` (`Status == 'ReservedOnBooking'`) or lets advisor attach an `'Available'` customer voucher (`ApplicableScope IN ('ServiceOnly', 'All')`). If `grossAmount < voucher.minOrderAmount`, shows inline amber warning `"Gross amount (350.000 ₫) is below voucher minimum (500.000 ₫) — unselect voucher or add eligible work"` and blocks submit until resolved (`422 VOUCHER_INELIGIBLE`). Caps discount at `grossAmount` (`EC-05`). |
| `paymentMethod` | Radio / `<select>` | Required `CK_ServiceInvoices_PaymentMethod`: `'Cash'`, `'BankTransfer'`, `'Card'`, `'EWallet'`. |
| `workSummaryNotes` | `<textarea>` (`NVARCHAR(1500)`) | Required `5..1500` chars documenting completed workshop operations. |

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/bookings/{bookingId}` + `GET /api/v1/spare-parts?compatibleModelId={booking.motorcycleModelId}` + `GET /api/v1/loyalty/customers/{customerId}` + `GET /api/v1/loyalty/tiers` on `$onInit`.
  - `POST /api/v1/service-invoices` (with `bookingRowVersion`, optional `lineItems[]` [`sparePartId`, `itemType`, `description`, `quantity`, `unitPriceVnd`], and `Idempotency-Key: <UUIDv4>`).
  - `GET /api/v1/service-invoices/{invoiceId}/receipt` when clicking **`[🖨 Print Official Receipt]`** (`<printable-receipt-modal receipt-type="SERVICE_INVOICE">`, `$window.print()` `@media print` A4/thermal sheet with 16-char SHA-256 `verificationCode`).
- **Atomic Post-Commit Side Effects**:
  - Server validates `SpareParts` model compatibility (`422 SPARE_PART_INCOMPATIBLE`) and active status (`422 SPARE_PART_INACTIVE`), commits `ServiceInvoices` + immutable `ServiceInvoiceLineItems` (`SUM(LineTotalVnd) == GrossAmount`), marks `ServiceBookings.Status = 'Completed'`, transitions `CustomerVouchers.Status = 'Used'`, updates `CustomerVehicles` mileage/next-due schedule, appends `PointLedgerEntries` (`ServiceInvoiceEarn`, `IdempotencyKey = SERVICE_INVOICE:{ServiceInvoiceId}:EARN`), evaluates `LoyaltyTiers`, and broadcasts `BookingStatusChanged`, `LoyaltyPointsUpdated`, and (when threshold crossed) `TierUpgraded` to `user-{customerId}`.
  - Staff UI surfaces **`[🖨 Print Official Receipt]`** (`RCP-SVC-*`) immediately upon settlement (`draftLineItems` preserved safely if unrelated SignalR events fire mid-edit).

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Loading booking & customer loyalty context | Split 2-column `<skeleton-loader>`. |
| **`Empty` / Invalid Entry** | Booking `bookingId` not found in staff's `DealerId` (`404`) or already `'Completed'` | Renders read-only finalized invoice banner if already settled (`UQ_ServiceInvoices_BookingId`), or `404` branch-isolated message with `[Back to Workbench]`. |
| **`Populated`** | Booking in `'CheckedIn'` or `'InProgress'` | Interactive charge calculator updating `NetPaidAmount`, `PointsEarned`, and tier promotion preview on every keystroke. |
| **`Validation Error (400/422)`** | `422 VOUCHER_INELIGIBLE` (`GrossAmount < MinOrderAmount` or expired) or `422 ODOMETER_REGRESSION` | Highlights exact field (`odometerAtCompletionKm` or voucher box) via `ProblemDetailsMapper` without losing entered line items. |
| **`Concurrency Conflict (409)`** | `409 INVOICE_ALREADY_ISSUED` or `bookingRowVersion` mismatch (colleague already settled the booking) | Loads and displays the already-issued `ServiceInvoice` safely with zero duplicate ledger entries (`EC-07`). |

---

### 3. `SCR-STF-03` — Showroom VIN Inventory, Bike Sales Checkout Desk & Official Receipt

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Branch-scoped showroom vehicle inventory manager and motorcycle sales checkout desk (`FR-02`, `FR-05`, `FR-08`, `AC-02.1`, `AC-02.2`). Allows `DealerStaff` to intake physical 17-character ISO 3779 `VIN` units into `DealerInventories`, execute atomic `SalesOrders` that transition `StockStatus` from `'Available'` to `'Sold'`, auto-onboard the sold `VIN` into the buyer's `CustomerVehicles` garage, credit `SalesOrderEarn` loyalty points, and open **`[🖨 Print Official Receipt]`** (`<printable-receipt-modal receipt-type="SALES_ORDER">` fetching `GET /api/v1/sales-orders/{orderId}/receipt` -> `RCP-SLS-*` with SHA-256 verification seal).
- **UI-Router State(s)**: `app.staff.salesNew` (alias `app.staff.salesDesk`, URL `/staff/sales-orders/new`) + integrated tabs/views `app.staff.inventory` (`/staff/inventory`) and `app.staff.salesList` (`/staff/sales-orders`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.SalesOrders.Create', 'Permissions.Inventories.ViewDealer']` (`[+ Intake New VIN Unit]` guarded by `has-permission="Permissions.Inventories.ManageDealer"`).
- **Component Paths**:
  - `frontend/src/app/features/staff-workbench/staff-sales-order-desk.component.js` (`<staff-sales-order-desk>`)
  - `frontend/src/app/features/staff-workbench/staff-vin-inventory.component.js` (`<staff-vin-inventory>`)
  - `frontend/src/app/features/staff-workbench/staff-sales-order-list.component.js` (`<staff-sales-order-list>`)

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Tabs: [1. Showroom VIN Checkout (Active)] [2. Branch VIN Stock (14)] [3. Sales Ledger])
+------------------------------------------------------------------------------------------------------+
| SHOWROOM BIKE SALES & VIN ONBOARDING DESK • DLR-HCM-01                   [+ Intake New Showroom VIN] |
+------------------------------------------------------------------+-----------------------------------+
| STEP 1: SELECT 'AVAILABLE' BRANCH MOTORCYCLE UNIT (RowVersion)   | ATOMIC SALE & GARAGE ONBOARDING   |
| Filter Model: [All Models v]   Search VIN: [ RLHJF510...       ] | --------------------------------- |
| +--------------------------------------------------------------+ | Selected VIN (.u-mono):           |
| | (•) RLHJF5109RZ104829 • Honda SH160i ABS Sport • Racing Red  | |   RLHJF5109RZ104829 [Available ✓] |
| |     Engine: JF51E-0104829 • 2026 • Listed: 102.500.000 ₫     | | Listed Price    : 102.500.000 ₫   |
| | ( ) RLCUG1010RZ208114 • Yamaha Exciter 155 GP • Matte Blue   | | Promo / Voucher :  -1.500.000 ₫   |
| |     Engine: G3N8E-0208114 • 2026 • Listed:  51.000.000 ₫     | | ================================= |
| +--------------------------------------------------------------+ | FINAL AMOUNT    : 101.000.000 ₫   |
| STEP 2: SELECT BUYING CUSTOMER & REGISTRATION DETAILS            | ================================= |
| Lookup Customer *: [ minh.nguyen@example.vn         ] [🔍 Find]  | Buyer Tier Now  : [◆ Bronze 1.0x] |
|   Matched: Minh Nguyen • Current Tier: [◆ Bronze • 1.00x] (0 pt) | Points Earned   : +10,100 pts     |
| License Plate (Optional): [ 59-A3 889.21 ]                       |   Floor(Floor(101M/10k) * 1.00)   |
| Promo Discount (VND)    : [ 1500000      ]                       | Tier After Sale : [♛ PLATINUM 2x] |
| Showroom Voucher (Opt.) : [ None (ShowroomOnly / All scope)  v ] | Auto-Garage Action:               |
| Payment Method *        : [ BankTransfer v ]                     | • Creates CustomerVehicle (0 km)  |
| Delivery Notes          : [ Launch promo + 36-mo OEM warranty  ] | • Binds 3 OEM MaintenanceRules    |
|                                                                  | [ Complete Sale & Onboard VIN -> ]|
+------------------------------------------------------------------+-----------------------------------+

MOBILE (< 768px): Inventory radio cards collapse into a searchable selector list above buyer lookup and sticky bottom settlement summary.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<staff-sales-order-desk>` -> `<available-vin-selector>`, `<customer-lookup-box>`, `<sales-pricing-form>`, `<sales-onboarding-preview>`, `<intake-vin-modal>`.
- **Bindings**: `{{ ::unit.vin }}`, `{{ ::unit.engineNumber }}`, `{{ ::unit.listedPrice | vndCurrency }}` use one-time `::` inside inventory tables; `unit.stockStatus` and `unit.rowVersion` stay bound for concurrency tracking.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Flow / Modal | Field Name | Client & Server Validation Rules (`AC-02.1`, `AC-02.2`, `EC-02`, `EC-03`) |
| :--- | :--- | :--- |
| **VIN Intake Modal (`POST /api/v1/inventories`)** | `vin`, `engineNumber`, `motorcycleModelId`, `colorName`, `manufactureYear`, `listedPrice` | `vin`: `vin-input` directive (`^[A-HJ-NPR-Z0-9]{17}$`, excludes `I`, `O`, `Q`, unique via `UQ_DealerInventories_VIN`). `engineNumber`: required `3..30` chars (`UQ_DealerInventories_EngineNumber`). `manufactureYear`: `2000..2100`. `listedPrice >= 0` (`DECIMAL(18,2)`). `DealerId` bound server-side from `CurrentUser.DealerId`. |
| **Sales Checkout (`POST /api/v1/sales-orders`)** | `dealerInventoryId` + `inventoryRowVersion` | Must have `StockStatus === 'Available'` and belong to `CurrentUser.DealerId`. |
| **Sales Checkout** | `customerId` | Required active `Users.Id` holding `'Customer'` role. |
| **Sales Checkout** | `discountAmount`, `appliedCustomerVoucherId` | `0 <= discountAmount <= listedPrice`; `finalAmount = totalAmount - discountAmount >= 0`. Optional voucher must have `ApplicableScope IN ('ShowroomOnly', 'All')`. |
| **Sales Checkout** | `paymentMethod` | Exact `CK_SalesOrders_PaymentMethod`: `'Cash'`, `'BankTransfer'`, `'Card'`, `'Installment'`. |

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**:
  - `GET /api/v1/inventories?stockStatus=Available&pageNumber=1&pageSize=50`
  - `GET /api/v1/vehicles/lookup?email=...` & `GET /api/v1/loyalty/customers/{customerId}`
  - `POST /api/v1/inventories` (new VIN stock intake) and `PATCH /api/v1/inventories/{inventoryId}` (status adjustment `Available <-> Reserved`)
  - `POST /api/v1/sales-orders` (with `inventoryRowVersion` and `Idempotency-Key`)
  - `GET /api/v1/sales-orders?pageNumber=1&pageSize=20` (Sales Ledger tab).
- **SignalR Dispatch**: Upon `201 Created`, server pushes `LoyaltyPointsUpdated` and `TierUpgraded` to `user-{customerId}` so if the buyer has their mobile portal open in the showroom, their new `VIN` in `My Garage` and `Platinum` badge appear live!

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching branch `DealerInventories` or customer lookup | Table/selector `<skeleton-loader variant="table-rows" rows="5">`. |
| **`Empty`** | `0` `'Available'` VIN units in branch stock | Empty banner `"No Available showroom VINs in stock at this dealership"` + `[+ Intake New Showroom VIN]` button. |
| **`Populated`** | `>= 1` `'Available'` VIN unit | Selectable VIN stock rows + real-time `FinalAmount`, `PointsEarned`, and tier promotion preview card. |
| **`Validation Error (400/422)`** | Invalid 17-char `VIN` (`I/O/Q` character) on intake (`400`) or `discountAmount > totalAmount` (`422`) | Maps inline error via `ProblemDetailsMapper`. |
| **`Concurrency Conflict (409)`** | Two sales advisors submit `POST /api/v1/sales-orders` on the same `DealerInventoryId` (`AC-02.2`, `EC-02`) | Second caller receives `409 CONCURRENCY_CONFLICT`; UI removes the sold `VIN` from the `'Available'` list immediately, preserves selected customer, and prompts advisor to pick another `'Available'` unit. |

---

### 4. `SCR-STF-04` — Branch Workshop Bay & Service Time-Slot Capacity Manager

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Allows `DealerStaff` to batch-generate daily workshop `ServiceTimeSlots` from branch operating hours, tune `MaxCapacity` per window (`RowVersion`-protected, never below existing `BookedCount`), and block/unblock windows (`IsBlocked`) for equipment maintenance or holidays (`FR-04`, `ADR-005`).
- **UI-Router State**: `app.staff.slots` (alias `app.staff.timeSlots`, URL `/staff/time-slots`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.TimeSlots.ManageDealer']`.
- **Component Path**: `frontend/src/app/features/staff-workbench/staff-slot-manager.component.js` (`<staff-slot-manager>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Weekly/Daily Slot Capacity Matrix + Batch Generator Modal)
+------------------------------------------------------------------------------------------------------+
| WORKSHOP BAY TIME-SLOT MANAGER • DLR-HCM-01 (Default Bays: 5 • Hours: 08:00-19:00) [+ Batch Generate]|
+------------------------------------------------------------------------------------------------------+
| Selected Date: [<-] [ 2026-05-16 ] [->]     Utilization Today: 18 / 40 Bays Booked (45.0%)           |
+------------------------------------------------------------------------------------------------------+
| Time Window   | Booked / Max Capacity         | Status Pill           | Capacity Control  | Bay Lock |
|---------------|-------------------------------|-----------------------|-------------------|----------|
| 08:00 - 09:00 | [██████████] 5 / 5 Bays       | [■ Full (5/5)]        | [-] [ 5 ] [+]     | [Block]  |
| 09:00 - 10:00 | [████████░░] 4 / 5 Bays       | [▲ Last Bay (1 Left)] | [-] [ 5 ] [+]     | [Block]  |
| 10:00 - 11:00 | [████░░░░░░] 2 / 5 Bays       | [● Available (3 Left)]| [-] [ 5 ] [+]     | [Block]  |
| 12:00 - 13:00 | [░░░░░░░░░░] 0 / 5 Bays       | [⊘ Blocked (Lunch)]   | [-] [ 5 ] [+]     | [Unblock]|
+------------------------------------------------------------------------------------------------------+
| Note: MaxCapacity cannot be reduced below current BookedCount (CK_ServiceTimeSlots_Capacity).        |
+------------------------------------------------------------------------------------------------------+

MOBILE (< 768px): Each time window renders as a stacked card with `[-]`/`[+]` stepper buttons (`44x44px` hit area) and `[Block/Unblock]` toggle.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<staff-slot-manager>` -> `<date-utilization-header>`, `<slot-capacity-row ng-repeat="slot in $ctrl.slots track by slot.id">`, `<batch-slot-generator-modal>`.
- **Bindings**: `{{ ::slot.startTime }}`, `{{ ::slot.endTime }}` are one-time (`::`); `slot.bookedCount`, `slot.maxCapacity`, `slot.isBlocked`, and `slot.rowVersion` are dynamic.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Batch Generate Modal (`POST /api/v1/time-slots/dealer/generate`)**: `fromDate` (`>= today`), `toDate` (`fromDate <= toDate <= fromDate + 30 days`), `slotDurationMinutes` (`30` or `60`), `maxCapacity` (`1..50` per `CK_Dealers_Capacity`), `excludeLunchHour` (`boolean`). Idempotently skips windows already existing (`UQ_ServiceTimeSlots_Dealer_Window`).
- **Inline Capacity Stepper (`PATCH /api/v1/time-slots/{slotId}`)**: Disables `[-]` button when `slot.maxCapacity <= Math.max(1, slot.bookedCount)` (`CK_ServiceTimeSlots_Capacity`); passes `slot.rowVersion`.

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/time-slots/dealer?fromDate=&toDate=`, `POST /api/v1/time-slots/dealer/generate`, `PATCH /api/v1/time-slots/{slotId}`.
- **SignalR Reactions**: Subscribes to `signalr:BookingCreated` and `signalr:BookingStatusChanged` (cleaned in `$onDestroy`) so if a customer books or cancels a slot while the manager screen is open, `slot.bookedCount` updates live!

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching branch slot schedule | 8-row `<skeleton-loader variant="table-rows">`. |
| **`Empty`** | Selected future date has `0` generated `ServiceTimeSlots` | Callout `"No workshop time slots generated for this date yet"` + primary `[+ Generate Daily Time Slots]` button. |
| **`Populated`** | Slots loaded | Utilization progress bars + `RowVersion`-safe capacity steppers. |
| **`Validation Error (400/422)`** | Attempt to set `maxCapacity < bookedCount` (if customer booked mid-edit) | Displays `ProblemDetails.detail` (`"Cannot lower capacity to 3 because 4 bookings are already active"`) and syncs row. |
| **`Concurrency Conflict (409)`** | `ServiceTimeSlots.RowVersion` changed due to concurrent customer booking | Reloads `/api/v1/time-slots/dealer` automatically and highlights updated `BookedCount`. |

---

### 5. `SCR-STF-05` — Branch Customer, VIN Garage & Loyalty Lookup

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Walk-in reception and service advisor lookup screen enabling `DealerStaff` to search any customer or motorcycle by 17-character `VIN`, `LicensePlate`, `Email`, or `PhoneNumber`, inspect OEM maintenance history and open `MaintenanceReminders`, and verify customer `LoyaltyTiers` multiplier and eligible `CustomerVouchers` before sales or service intake (`FR-02`, `FR-03`, `FR-05`).
- **UI-Router State**: `app.staff.customers` (`/staff/customers`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.Vehicles.ViewDealer', 'Permissions.Loyalty.ViewDealerCustomer']`.
- **Component Path**: `frontend/src/app/features/staff-workbench/staff-customer-lookup.component.js` (`<staff-customer-lookup>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Instant Reception Search + Split Customer Loyalty & VIN History View)
+------------------------------------------------------------------------------------------------------+
| CUSTOMER, VIN GARAGE & LOYALTY LOOKUP                                                                |
+------------------------------------------------------------------------------------------------------+
| Search by 17-Char VIN, Plate, Email, or Phone: [ RLHJF5109RZ104829               ] [🔍 Lookup] [Clear]|
+--------------------------------------------------+---------------------------------------------------+
| CUSTOMER & LOYALTY PROFILE                       | REGISTERED GARAGE VEHICLES & OEM SCHEDULE         |
| ------------------------------------------------ | ------------------------------------------------- |
| Minh Nguyen (minh.nguyen@example.vn • 0909123456)| 1. Honda SH160i ABS Sport [Verified Showroom ✓]   |
| Tier Badge   : [◆◆ Silver Member • 1.20x]        |    VIN: RLHJF5109RZ104829 • Plate: 59-A3 889.21   |
| Available Pts: 649 pts | Qualifying: 1,960 pts   |    Odometer: 3,850 km • Next Due: 4,000 km [Due!] |
| Eligible Service/Showroom Vouchers (2):          |    Last Service: 1,000 km (12/03/2026)            |
|  • SVC100K-7K9P2M4X [ReservedOnBooking] (100k)   |    [Inspect OEM History]                          |
|  • SVC15PCT-4R8N2W9Q [Available] (15% up to 200k)| ------------------------------------------------- |
|                                                  | Quick Actions:                                    |
| *(Read-only: Staff cannot manually edit points)* | [Sell New Showroom VIN ->] (`app.staff.salesNew`) |
+--------------------------------------------------+---------------------------------------------------+

MOBILE (< 768px): Search bar sticky on top; Customer Loyalty card stacks above Garage Vehicles list.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<staff-customer-lookup>` -> `<reception-search-bar>`, `<staff-customer-loyalty-card>`, `<staff-vehicle-history-panel>`.
- **Separation-of-Duties Enforcement**: Renders customer balances and vouchers strictly read-only (`DealerStaff` never holds `Permissions.Loyalty.AdjustPoints` or `Permissions.Loyalty.RedeemVoucher`).

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Search Query (`q` / `vin` / `licensePlate` / `email`)**: Required `>= 3` characters before triggering lookup (`400 VALIDATION_FAILED` if `< 3` chars to prevent broad user enumeration); auto-detects 17-char ISO 3779 `VIN` format when `q.length === 17`.

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/vehicles/lookup?query={q}`, `GET /api/v1/loyalty/customers/{customerId}`, `GET /api/v1/vehicles/{vehicleId}/maintenance-history`.
- **SignalR Reactions**: Global `notificationBell` and toast stack remain active in the shell so `DealerStaff` never misses a new `BookingCreated` alert while looking up a walk-in customer!

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Executing `GET /api/v1/vehicles/lookup` | Dual-panel `<skeleton-loader variant="card-grid" rows="2">`. |
| **`Empty`** | Initial unsearched state **OR** `0` matching customers/VINs | Initial: prompt `"Enter a 17-char VIN, license plate, email, or phone number"`. No match: `"No customer or registered VIN matched your query"`. |
| **`Populated`** | Match returned | Displays customer contact, tier badge, voucher status list, and vehicle maintenance milestones + `[Sell New Showroom VIN ->]` deep link. |
| **`Validation Error (400/422)`** | Search query `< 3` chars or invalid filter characters | Inline helper message `"Enter at least 3 characters (or full 17-character VIN)"`. |
| **`Concurrency Conflict (409)`** | N/A | Read-only lookup view. |

---

### 6. `SCR-STF-06` — Zero-Refresh SignalR Showroom Test-Drive Workbench (`v1.1.0`)

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Real-time showroom coordinator workbench (`FR-02`, `FR-06`, `FR-08`, `ADR-002`, `ADR-004`, `ADR-005`) scoped strictly to the advisor's assigned `DealerId` (`IDealerScoped`). Receives incoming customer `TestDriveBookings` (`TD-*`) instantly via SignalR `TestDriveCreated` **with zero page reloads**, supports demo `VIN` assignment (`Available` stock filtered by `MotorcycleModelId`) and `RowVersion`-guarded lifecycle transitions (`Requested -> Confirmed -> Completed / NoShow / Cancelled`), and provides 1-click **`[Convert to Bike Sale ->]`** handoff into `app.staff.salesDesk` (`SCR-STF-03`).
- **UI-Router State**: `app.staff.testDrives` (`/staff/test-drives`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['DealerStaff']`, `permissions: ['Permissions.TestDrives.ViewDealer', 'Permissions.TestDrives.UpdateStatus']` (`[Convert to Bike Sale ->]` guarded by `has-permission="Permissions.SalesOrders.Create"`).
- **Component Path**: `frontend/src/app/features/staff/test-drives.component.js` (`<staff-test-drives>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — 4-KPI Strip + Zero-Reload Live Test-Drive Queue + Demo VIN Modal)
+------------------------------------------------------------------------------------------------------+
| SHOWROOM TEST-DRIVE WORKBENCH • DLR-HCM-01               [● SignalR: Connected] [Sell Showroom VIN ->]|
+------------------------------------------------------------------------------------------------------+
| [⚡ LIVE ARRIVAL]: New Test Drive TD-20260518-A4F91C • Honda SH160i ABS • Minh Nguyen (10:00-10:45)   |
+------------------------------------------------------------------------------------------------------+
| [Requested: 2]   |   [Confirmed Rides: 3]   |   [Completed Rides: 5]   |   [Cancelled / No-Show: 1]  |
+------------------------------------------------------------------------------------------------------+
| Date: [All Dates v]   Status: [All Statuses v]   Search Customer/Model/TD#: [                      ] |
+------------------------------------------------------------------------------------------------------+
| TD-20260518-A4F91C [⚡ NEW] • [● Requested] • Honda SH160i ABS Sport • 18/05/2026 10:00-10:45         |
| Rider: Minh Nguyen • License: 790198004512 • Phone: 0909123456                                       |
| Actions: [✓ Confirm & Assign Demo VIN]   [✕ Cancel]                                                  |
|------------------------------------------------------------------------------------------------------|
| TD-20260518-8B20E1 • [✓✓ Completed] • Honda Air Blade 160 SE • Demo VIN: RLHJF5108RY000101           |
| Actions: [Convert to Bike Sale ->] (`app.staff.salesDesk`)                                           |
+------------------------------------------------------------------------------------------------------+
```

#### c) API Calls, SignalR Reactions & `$onDestroy` Memory Hygiene
- **REST API Calls**: `GET /api/v1/test-drives?pageSize=100`, `GET /api/v1/test-drives/{testDriveId}`, `GET /api/v1/inventories?status=Available` (for model-matched demo `VIN` selection), `PATCH /api/v1/test-drives/{testDriveId}/status` (with `rowVersion` and optional `assignedInventoryId`), and `POST /api/v1/test-drives/{testDriveId}/cancel`.
- **Zero-Reload SignalR Reactions (`$onDestroy` Cleaned)**:
  - `TestDriveCreated`: Wrapped in `$rootScope.$applyAsync()`, deduplicates by `id`, prepends the highlighted card, shows `$ctrl.liveArrivalBanner`, and hydrates `rowVersion` via `getTestDriveById`.
  - `TestDriveStatusChanged`: Synchronizes peer branch workstation transitions and customer cancellations live (`Confirmed -> Sold -> Completed` preserves `assignedVin`).
  - Disposes all 3 listeners (`TestDriveCreated`, `TestDriveStatusChanged`, `signalr:reconnected`) inside `$ctrl.$onDestroy()`.

---

## Part II — Admin Network Governance & `Chart.js` Analytics Screens

---

### 7. `SCR-ADM-01` — Executive Multi-Dealer `Chart.js v4` Analytics Dashboard

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Headquarters executive business intelligence dashboard (`FR-07`, `BO-05`, `ADR-006`, `RISK-04`). Visualizes cross-dealer revenue (Showroom `SalesOrders.FinalAmount` vs. Workshop `ServiceInvoices.NetPaidAmount`), appointment conversion funnels (`ServiceBookings` by `Status`), month-over-month velocity trends, and `LoyaltyTiers` population across Hanoi (`DLR-HN-01`), Da Nang (`DLR-DN-01`), and Ho Chi Minh City (`DLR-HCM-01`) using **`Chart.js v4`**, paired with a drill-down branch comparison table and CSV export.
- **UI-Router State**: `app.admin.analytics` (`/admin/analytics`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Analytics.ViewAllDealers']` (`[Export CSV Report]` button guarded by `has-permission="Permissions.Analytics.ExportReports"`).
- **Component Paths**:
  - `frontend/src/app/features/admin-analytics/admin-analytics-dashboard.component.js` (`<admin-analytics-dashboard>`)
  - `frontend/src/app/shared/components/dealer-chart.component.js` (`<dealer-chart>`)

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1280px — 5-Card KPI Strip + 2x2 Chart.js v4 Grid + Sortable Dealer Comparison Table)
+------------------------------------------------------------------------------------------------------+
| EXECUTIVE MULTI-DEALER ANALYTICS                                     [⬇ Export CSV Report]           |
| Range: [2026-02-01] to [2026-08-31] [Last 30d|Q2|YTD]   Branch Scope: [All Authorized Dealers (3) v] |
+------------------------------------------------------------------------------------------------------+
| +------------------+ +------------------+ +------------------+ +------------------+ +--------------+ |
| | NETWORK REVENUE  | | SHOWROOM SALES   | | WORKSHOP SERVICE | | SERVICE BOOKINGS | | LOYALTY BASE | |
| | 14.820.500.000 ₫ | | 13.450.000.000 ₫ | |  1.370.500.000 ₫ | | 1,284 (76.0% ✓)  | | 1,000 Active | |
| | +12.4% vs prior  | | 138 Bikes Sold   | | 976 Invoices     | | 0 Overbookings   | | 19% Gold+Plat| |
| +------------------+ +------------------+ +------------------+ +------------------+ +--------------+ |
| +--------------------------------------------------+ +---------------------------------------------+ |
| | CHART 1: Stacked Bar (Chart.js v4)               | | CHART 2: Dual-Axis Multi-Line (Chart.js v4) | |
| | Showroom vs. Workshop Service Revenue per Dealer | | Monthly Revenue (VND) & Booking Velocity    | |
| |  Saigon  [██████████████████▓▓▓] 6.79B ₫         | |  VND  \      /-----\     Bookings (Right Y) | |
| |  Hanoi   [██████████████▓▓]      5.12B ₫         | |  (L)   \----/       \---                    | |
| |  Da Nang [████████▓]             2.90B ₫         | |       Feb  Mar  Apr  May  Jun  Jul  Aug     | |
| +--------------------------------------------------+ +---------------------------------------------+ |
| +--------------------------------------------------+ +---------------------------------------------+ |
| | CHART 3: Doughnut Funnel (Chart.js v4)           | | CHART 4: Tier Distribution [Doughnut | Bar] | |
| | Workshop Bookings by Status                      | | Active Customers by LoyaltyTier             | |
| |  Completed 76% (976) • Confirmed 7% (88)         | |  Bronze: 540 (54%) • Silver: 270 (27%)      | |
| |  CheckedIn/InProg 5% • Pending 5% • Cancel/NS 7% | |  Gold  : 140 (14%) • Platinum: 50 (5%)      | |
| +--------------------------------------------------+ +---------------------------------------------+ |
| MULTI-DEALER PERFORMANCE COMPARISON TABLE (Click column header to sort):                             |
| Branch Code & Name             | Bikes Sold | Showroom Rev   | Bookings | Done % | Service Rev     | |
| DLR-HCM-01 • Saigon Center     |         62 | 6.150.000.000₫ |      560 |  79.1% |   640.500.000 ₫ | |
| DLR-HN-01  • Hanoi Flagship    |         48 | 4.680.000.000₫ |      438 |  74.9% |   445.000.000 ₫ | |
| DLR-DN-01  • Da Nang Hub       |         28 | 2.620.000.000₫ |      286 |  71.7% |   285.000.000 ₫ | |
+------------------------------------------------------------------------------------------------------+

MOBILE / TABLET (< 1024px): KPI cards wrap into a 2-column grid; all 4 `<dealer-chart>` panels stack 1-column (`height: 280px`); comparison table scrolls horizontally with sticky `Branch Code & Name` column.
```

#### c) Component Hierarchy, Exact `Chart.js v4` Configurations & Leak-Free `$onDestroy` Lifecycle (`RISK-04`)
- **Hierarchy**: `<admin-analytics-dashboard>` -> `<analytics-filter-bar>`, `<executive-kpi-strip>`, 4x `<dealer-chart config="$ctrl.chartConfigs.xyz" height-px="320">`, `<dealer-comparison-table>`.
- **Memory & Canvas Lifecycle Contract (`dealer-chart.component.js`)**:
  1. Instantiates `chartInstance = new window.Chart(ctx, angular.copy($ctrl.config))` inside `$postLink` using **strict `Chart.js v4` options** (`scales.x`, `scales.y`, `plugins.legend`, `plugins.tooltip` — zero deprecated v2 `yAxes` arrays).
  2. When `$ctrl.filters` (`fromUtc`, `toUtc`, `dealerId`) or Chart 4's `['doughnut' | 'bar']` view toggle updates, `<dealer-chart>` `$onChanges` mutates `chartInstance.data` / `options` and calls `chartInstance.update('none')` (unless `config.type` changed between `'doughnut'` and `'bar'`, in which case it calls `chartInstance.destroy()` first before creating the new instance).
  3. **Mandatory `$onDestroy` Cleanup**: Calls `if (chartInstance) { chartInstance.destroy(); chartInstance = null; }` so rapid route switching (`app.admin.analytics <-> app.admin.dealers`) leaves **zero detached `<canvas>` nodes or resize observers** (`EC-12`, `RISK-04`).

```javascript
// Canonical Chart.js v4 Builder Contract (frontend/src/app/features/admin-analytics/analytics-chart-builder.service.js)
// 1. Stacked Bar: Showroom vs Service Revenue per Dealer (GET /api/v1/admin/analytics/revenue-by-dealer)
var revenueByDealerConfig = {
  type: 'bar',
  data: {
    labels: dto.dealers.map(function (d) { return d.dealerName; }),
    datasets: [
      { label: 'Showroom Bike Sales (VND)', data: dto.dealers.map(function (d) { return d.showroomRevenueVnd; }), backgroundColor: '#DC2626', stack: 'rev', borderRadius: 4 },
      { label: 'Workshop Service Invoices (VND)', data: dto.dealers.map(function (d) { return d.serviceRevenueVnd; }), backgroundColor: '#2563EB', stack: 'rev', borderRadius: 4 }
    ]
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: { legend: { position: 'top' } },
    scales: {
      x: { stacked: true, grid: { display: false } },
      y: { stacked: true, beginAtZero: true }
    }
  }
};

// 2. Multi-Line Dual-Axis: Monthly Revenue & Booking Trends (GET /api/v1/admin/analytics/monthly-trends)
var monthlyTrendsConfig = {
  type: 'line',
  data: {
    labels: trendsDto.months, // e.g. ["2026-02", ..., "2026-08"]
    datasets: [
      { label: 'Total Realized Revenue (VND)', data: trendsDto.totalRevenueVnd, borderColor: '#DC2626', backgroundColor: 'rgba(220,38,38,0.10)', yAxisID: 'yRevenue', tension: 0.3, fill: true },
      { label: 'Service Bookings Volume', data: trendsDto.bookingCounts, borderColor: '#0F172A', backgroundColor: '#0F172A', yAxisID: 'yBookings', tension: 0.3 }
    ]
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    scales: {
      yRevenue: { type: 'linear', position: 'left', beginAtZero: true },
      yBookings: { type: 'linear', position: 'right', beginAtZero: true, grid: { drawOnChartArea: false } }
    }
  }
};

// 3. Doughnut: Booking Status Funnel (GET /api/v1/admin/analytics/bookings-by-dealer)
var bookingFunnelConfig = {
  type: 'doughnut',
  data: {
    labels: ['Completed', 'Confirmed', 'CheckedIn', 'InProgress', 'Pending', 'Cancelled', 'NoShow'],
    datasets: [{
      data: [s.completed, s.confirmed, s.checkedIn, s.inProgress, s.pending, s.cancelled, s.noShow],
      backgroundColor: ['#16A34A', '#2563EB', '#0891B2', '#7C3AED', '#D97706', '#64748B', '#DC2626']
    }]
  },
  options: { responsive: true, maintainAspectRatio: false, cutout: '62%', plugins: { legend: { position: 'right' } } }
};

// 4. Doughnut / Bar Toggle: Loyalty Tier Distribution (GET /api/v1/admin/analytics/loyalty-distribution)
var loyaltyTierConfig = {
  type: $ctrl.tierChartMode || 'doughnut', // 'doughnut' | 'bar'
  data: {
    labels: ['Bronze (1.00x)', 'Silver (1.20x)', 'Gold (1.50x)', 'Platinum (2.00x)'],
    datasets: [{
      label: 'Active Customers',
      data: [t.bronzeCount, t.silverCount, t.goldCount, t.platinumCount],
      backgroundColor: ['#B45309', '#64748B', '#CA8A04', '#4F46E5']
    }]
  },
  options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'right' } } }
};
```

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Date Range Filter (`fromDate`, `toDate`)**: Converted to UTC ISO-8601 boundaries (`fromUtc`, `toUtc`) before querying API; validates `fromUtc <= toUtc` and maximum window `<= 366 days` (`400 VALIDATION_FAILED` if inverted). Quick presets: `Last 30 Days`, `Current Quarter`, `Year to Date`.
- **Dealer Filter (`dealerId`)**: Optional `Dealers.Id` GUID (`All Dealers` = `null`); filters KPI strip, Chart 2, Chart 3, and highlights the selected branch in Chart 1 & comparison table.
- **CSV Export (`[⬇ Export CSV Report]`)**: Streams `GET /api/v1/admin/analytics/export?fromUtc=&toUtc=&dealerId=` as UTF-8 BOM CSV (`dealer-analytics-YYYYMMDD.csv`).

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls** (executed in parallel via `$q.all` backed by SQL Server covering indexes `IX_SalesOrders_Analytics_Covering` & `IX_ServiceInvoices_Analytics_Covering`, `< 300 ms` P95):
  - `GET /api/v1/admin/analytics/overview`
  - `GET /api/v1/admin/analytics/revenue-by-dealer`
  - `GET /api/v1/admin/analytics/bookings-by-dealer`
  - `GET /api/v1/admin/analytics/monthly-trends`
  - `GET /api/v1/admin/analytics/loyalty-distribution`
  - `GET /api/v1/admin/analytics/export` (on export click).
- **SignalR Reactions (`role-Admin`)**: Receives `BookingCreated` notifications on `role-Admin` group; displays a subtle non-intrusive `[● New Network Activity — Click to Refresh Charts]` pill in the filter bar rather than thrashing canvas animations mid-inspection.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | `$q.all` fetching 5 analytics endpoints | 5-card `<skeleton-loader variant="kpi-strip">` + 4x `<skeleton-loader variant="chart">`. |
| **`Empty`** | Narrow date range with `0` completed sales/invoices/bookings | Renders `0 ₫` KPI cards and clean zero-state overlays inside chart containers (`"No finalized transactions in selected date window"`) + `[Reset to Last 6 Months]`. |
| **`Populated`** | Aggregated data returned | Interactive 4-chart `Chart.js v4` matrix + sortable comparison table where cancelled/void records are strictly excluded from realized revenue (`AC-07.1`). |
| **`Validation Error (400/422)`** | `fromUtc > toUtc` (`400 VALIDATION_FAILED`) | Inline red border on date range picker + message `"Start date must be on or before end date"`. |
| **`Concurrency Conflict (409)`** | N/A | Read-only RCSI snapshot aggregation queries never block or conflict with branch writers. |

---

### 8. `SCR-ADM-02` — Multi-Dealer Branch Network Governance

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Nationwide branch master-data console allowing `Admin` to create, edit, activate, or deactivate authorized `Dealers` (`Code`, `City`, `Address`, GPS coordinates, `OpeningTime`/`ClosingTime`, `DefaultSlotCapacity`) (`FR-01`, `FR-02`).
- **UI-Router State**: `app.admin.dealers` (`/admin/dealers`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Dealers.ManageAll']`.
- **Component Path**: `frontend/src/app/features/admin-analytics/admin-dealers-manager.component.js` (`<admin-dealers-manager>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Branch Registry Table + Create/Edit Slide-Over Drawer)
+------------------------------------------------------------------------------------------------------+
| AUTHORIZED DEALERSHIP NETWORK (3 Branches)                                  [+ Create New Dealership]|
+------------------------------------------------------------------------------------------------------+
| Search Code/Name: [             ]   City: [All Cities v]   Status: [Active Only v]                   |
+------------------------------------------------------------------------------------------------------+
| Code (.u-mono)| Dealership Name             | City        | Workshop Hours | Bays | Status | Actions |
|---------------|-----------------------------|-------------|----------------|------|--------|---------|
| DLR-HN-01     | Apex Moto Hanoi Flagship    | Hanoi       | 08:00 - 18:00  |    4 | Active | [Edit]  |
| DLR-DN-01     | Apex Moto Da Nang Hub       | Da Nang     | 08:00 - 18:00  |    4 | Active | [Edit]  |
| DLR-HCM-01    | Apex Moto Saigon Center     | HCMC        | 08:00 - 19:00  |    5 | Active | [Edit]  |
+------------------------------------------------------------------------------------------------------+
MOBILE (< 768px): Branches collapse into stacked cards with `[Edit Branch]` & `[Manage Staff ->]` actions.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<admin-dealers-manager>` -> `<dealer-registry-table>`, `<dealer-editor-drawer>`, `<confirm-modal>`. Uses `::` on `{{ ::dealer.code }}` and `{{ ::dealer.createdAtUtc | utcToLocal:'date' }}`.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Fields (`POST / PUT /api/v1/dealers`)**:
  - `code`: Required uppercase `VARCHAR(20)` (`^DLR-[A-Z0-9-]{2,16}$`, unique via `UQ_Dealers_Code`, immutable after creation).
  - `name` (`NVARCHAR(150)`), `city` (`NVARCHAR(80)`), `address` (`NVARCHAR(300)`), `phoneNumber` (`VARCHAR(20)`), `contactEmail` (`VARCHAR(256)` email).
  - `openingTime`, `closingTime`: `HH:mm:ss` enforcing `closingTime > openingTime` (`CK_Dealers_Hours`).
  - `defaultSlotCapacity`: Integer `1..50` (`CK_Dealers_Capacity`).
  - `latitude` (`-90..90`), `longitude` (`-180..180`): Both null or both populated (`CK_Dealers_Coordinates`).

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/dealers`, `POST /api/v1/dealers`, `PUT /api/v1/dealers/{dealerId}`.
- **SignalR Reactions**: Standard shell notification feed.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching `/api/v1/dealers` | `<skeleton-loader variant="table-rows" rows="4">`. |
| **`Empty`** | Filter matches `0` branches | Empty table message + `[Clear Filters]`. |
| **`Populated`** | `>= 1` dealership returned | Sortable branch rows with `[Edit]` and deep link `[View Staff ->]` (`app.admin.staff?dealerId=...`). |
| **`Validation Error (400/422)`** | `closingTime <= openingTime` or invalid coordinates (`400`) | Inline field errors inside slide-over drawer via `ProblemDetailsMapper`. |
| **`Concurrency Conflict (409)`** | Duplicate `Dealers.Code` (`UQ_Dealers_Code`) | Highlights `code` input with `"Branch code already exists"`. |

---

### 9. `SCR-ADM-03` — Dealer Staff Provisioning & Access Governance

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Enables `Admin` to provision `DealerStaff` accounts strictly bound to an assigned `DealerId` and `StaffEmployeeCode` (`CK_Users_DealerStaff_DealerId`), audit the **`18` granted `DealerStaff` permissions** (including `TestDrives.ViewDealer` and `TestDrives.UpdateStatus`), and lock/unlock staff credentials immediately (`IsActive` toggle revoking active `RefreshTokens` and blocking requests via `ActivePrincipalMiddleware`) (`FR-01`, `ADR-002`).
- **UI-Router State**: `app.admin.staff` (`/admin/staff?dealerId`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Users.ManageStaff']`.
- **Component Path**: `frontend/src/app/features/admin-analytics/admin-staff-manager.component.js` (`<admin-staff-manager>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Branch Selector + Staff Directory + Provisioning Modal)
+------------------------------------------------------------------------------------------------------+
| DEALER STAFF PROVISIONING & ACCESS GOVERNANCE                           [+ Provision Dealer Staff]   |
+------------------------------------------------------------------------------------------------------+
| Select Dealership Branch *: [ DLR-HCM-01 • Apex Moto Saigon Center v ]  Status: [All Staff v]        |
+------------------------------------------------------------------------------------------------------+
| Badge Code    | Full Name        | Email                          | Assigned Branch | Status | Action|
|---------------|------------------|--------------------------------|-----------------|--------|-------|
| EMP-HCM-001   | Lan Tran         | staff.hcm@motorcycledealer.vn  | DLR-HCM-01      | Active | [Lock]|
| EMP-HCM-002   | Hung Pham        | hung.hcm@motorcycledealer.vn   | DLR-HCM-01      | Locked | [Open]|
+------------------------------------------------------------------------------------------------------+
MOBILE (< 768px): Branch dropdown at top; staff accounts stack into cards with `[Lock / Unlock]` confirmation action.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<admin-staff-manager>` -> `<branch-selector-bar>`, `<staff-directory-table>`, `<provision-staff-modal>`, `<role-permissions-preview-accordion>` (read-only list of the `18` canonical `DealerStaff` permissions).

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Provision Staff Modal (`POST /api/v1/dealers/{dealerId}/staff`)**:
  - `dealerId` (`UUID`, required non-null), `staffEmployeeCode` (`VARCHAR(30)`, required `^EMP-[A-Z0-9-]{3,25}$`, enforcing `CK_Users_DealerStaff_DealerId` and `UQ_Users_StaffEmployeeCode`), `fullName` (`2..150` chars), `email` (`UQ_Users_Email`), `phoneNumber` (`UQ_Users_PhoneNumber`), `temporaryPassword` (`>= 8` chars complexity).
- **Status Toggle (`PATCH /api/v1/dealers/{dealerId}/staff/{userId}/status`)**: `isActive: boolean` via `confirmModal` (`"Locking this staff account immediately revokes their refresh tokens and blocks API/SignalR access"`).

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/dealers`, `GET /api/v1/dealers/{dealerId}/staff`, `POST /api/v1/dealers/{dealerId}/staff`, `PATCH /api/v1/dealers/{dealerId}/staff/{userId}/status`.
- **SignalR Reactions**: Standard shell notification feed.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Loading branch staff list | `<skeleton-loader variant="table-rows" rows="4">`. |
| **`Empty`** | Newly created dealership has `0` provisioned `DealerStaff` | Callout `"No staff assigned to this dealership yet"` + `[+ Provision First Staff Advisor]`. |
| **`Populated`** | `>= 1` staff user in branch | Renders badge codes (`.u-mono`), last login (`utcToLocal`), and `[Lock / Activate]` controls. |
| **`Validation Error (400/422)`** | Missing `staffEmployeeCode` or weak password (`400`) | Inline field errors inside provisioning modal. |
| **`Concurrency Conflict (409)`** | Duplicate `email`, `phoneNumber`, or `staffEmployeeCode` (`409`) | Highlights conflicting field (`UQ_Users_*`). |

---

### 10. `SCR-ADM-04` — Master Motorcycle Catalog & OEM Maintenance Rules Studio

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Master product & engineering studio where `Admin` curates platform-wide `MotorcycleModels` (`ModelCode`, `Category`, `EngineDisplacementCc`, `BaseRetailPrice`, warranty limits, `SpecificationsJson`) and configures each model's OEM `MaintenanceRules` (`MilestoneCode`, `IntervalKm`, `IntervalMonths`, `LeadMileageKm`, `LeadTimeDays`, `IsRecurring`, `ChecklistSummary`) evaluated by `VinMaintenanceReminderWorker` (`FR-02`, `FR-03`, `FR-04`, `ADR-005`).
- **UI-Router State(s)**: `app.admin.models` (`/admin/models`) + sub-state/drawer `app.admin.maintenanceRules` (`/admin/models/:modelId/rules`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Showroom.ManageModels']` (`MaintenanceRules` editor guarded by `has-permission="Permissions.Vehicles.ManageRules"`).
- **Component Paths**:
  - `frontend/src/app/features/admin-analytics/admin-models-manager.component.js` (`<admin-models-manager>`)
  - `frontend/src/app/features/admin-analytics/admin-maintenance-rules.component.js` (`<admin-maintenance-rules>`)

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Master Models Table + OEM Maintenance Rules Slide-Over Studio)
+------------------------------------------------------------------------------------------------------+
| MASTER MOTORCYCLE CATALOG & OEM MAINTENANCE RULES STUDIO                        [+ Add Motorcycle]   |
+------------------------------------------------------------------------------------------------------+
| SKU (.u-mono)    | Brand & Model            | Category  | Engine | MSRP (VND)     | OEM Rules | Edit |
|------------------|--------------------------|-----------|--------|----------------|-----------|------|
| HND-SH160I-2026  | Honda SH160i ABS Sport   | Scooter   | 157 cc | 102.500.000 ₫  | [3 Rules] | [✎]  |
| YMH-EXC155-2026  | Yamaha Exciter 155 VVA   | Underbone | 155 cc |  51.000.000 ₫  | [3 Rules] | [✎]  |
+------------------------------------------------------------------------------------------------------+

OEM MAINTENANCE RULES STUDIO DRAWER (app.admin.maintenanceRules — Honda SH160i ABS Sport):
  Code          | Milestone Name            | Interval Km | Months | Lead Window       | Recurring
  BREAKIN_1000  | Initial Break-In Check    |    1,000 km |   1 mo | 200 km / 7 days   | One-Time (0)
  PERIODIC_4000 | Periodic Oil & CVT Check  |    4,000 km |   6 mo | 250 km / 7 days   | Recurring (1)
  MAJOR_12000   | Major Valve/Belt Overhaul |   12,000 km |  12 mo | 250 km / 7 days   | Recurring (1)
  [+ Add Milestone Rule]

MOBILE (< 768px): Models render as cards; OEM Rules Studio opens as a full-screen stacked editor.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<admin-models-manager>` -> `<models-admin-table>`, `<model-editor-modal>`, `<admin-maintenance-rules>`.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Model Form (`POST / PUT /api/v1/motorcycles`)**: `modelCode` (`UQ_MotorcycleModels_ModelCode`), `brand`, `modelName`, `category` (`'Scooter'`, `'Underbone'`, `'Sport'`, `'Naked'`, `'Adventure'`, `'Cruiser'`), `engineDisplacementCc` (`49..2500`), `baseRetailPrice > 0` (`DECIMAL(18,2)`), `warrantyMonths > 0`, `warrantyMileageKm > 0`, structured key-value spec builder serializing valid JSON into `specificationsJson` (`CK_MotorcycleModels_SpecsJson`).
- **Rule Form (`POST / PUT /api/v1/motorcycles/{modelId}/maintenance-rules`)**: `milestoneCode` (unique per model via `UQ_MaintenanceRules_Model_Code`), `milestoneName`, `intervalKm > 0`, `intervalMonths > 0`, `leadMileageKm >= 0`, `leadTimeDays >= 0`, `estimatedDurationMinutes > 0`, `isRecurring`, `checklistSummary` (`NVARCHAR(1000)`).

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/motorcycles`, `POST /api/v1/motorcycles`, `PUT /api/v1/motorcycles/{modelId}`, `GET /api/v1/motorcycles/{modelId}/maintenance-rules`, `POST /api/v1/motorcycles/{modelId}/maintenance-rules`, `PUT /api/v1/motorcycles/{modelId}/maintenance-rules/{ruleId}`.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching catalog or model rules | `<skeleton-loader variant="table-rows" rows="6">`. |
| **`Empty`** | Newly created model has `0` `MaintenanceRules` | Warning banner inside drawer `"Add at least 1 OEM MaintenanceRule so VinMaintenanceReminderWorker can monitor sold VINs"` + `[+ Add Milestone Rule]`. |
| **`Populated`** | Catalog & rules loaded | Tabular MSRP & interval columns + JSON spec preview pills. |
| **`Validation Error (400/422)`** | `engineDisplacementCc < 49`, `baseRetailPrice <= 0`, or invalid JSON (`400`) | Inline `ProblemDetailsMapper` field errors. |
| **`Concurrency Conflict (409)`** | Duplicate `ModelCode` or duplicate `(MotorcycleModelId, MilestoneCode)` | Highlights `modelCode` / `milestoneCode` field. |

---

### 11. `SCR-ADM-05` — Loyalty Tiers Studio & Audited Point Adjustments

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Governs the 4 canonical `LoyaltyTiers` (`Bronze` `0 / 1.00x`, `Silver` `500 / 1.20x`, `Gold` `2,000 / 1.50x`, `Platinum` `5,000 / 2.00x`) and provides an audited, append-only **Manual Point Adjustment** console (`AdminAdjustment` rows in `PointLedgerEntries`) for goodwill credits or dispute resolutions without ever mutating historical ledger rows (`FR-05`, `ADR-003`).
- **UI-Router State(s)**: `app.admin.loyaltyTiers` (`/admin/loyalty/tiers`) + integrated tab/sub-state `app.admin.pointAdjustments` (`/admin/loyalty/adjustments`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Loyalty.ManageTiers']` (Adjustments tab/form guarded by `has-permission="Permissions.Loyalty.AdjustPoints"`).
- **Component Paths**:
  - `frontend/src/app/features/admin-analytics/admin-loyalty-tiers.component.js` (`<admin-loyalty-tiers>`)
  - `frontend/src/app/features/admin-analytics/admin-point-adjustments.component.js` (`<admin-point-adjustments>`)

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Tabs: [1. 4-Tier Matrix & Perks (Active)]  [2. Audited Ledger Point Adjustments])
+------------------------------------------------------------------------------------------------------+
| LOYALTY TIERS STUDIO & AUDITED LEDGER GOVERNANCE                                                     |
+------------------------------------------------------------------------------------------------------+
| Rank | Tier Code  | Min Qualifying Pts | Points Multiplier | Member Perks (BenefitsJson)    | Action |
|------|------------|--------------------|-------------------|--------------------------------|--------|
|  1   | ◆ Bronze   |              0 pts |            1.00x  | Base accrual + VIN alerts      | [Edit] |
|  2   | ◆◆ Silver  |            500 pts |            1.20x  | +20% bonus + Silver vouchers   | [Edit] |
|  3   | ★ Gold     |          2,000 pts |            1.50x  | +50% bonus + Priority bay      | [Edit] |
|  4   | ♛ Platinum |          5,000 pts |            2.00x  | 2x double pts + VIP overhaul   | [Edit] |
+------------------------------------------------------------------------------------------------------+
| TAB 2 — AUDITED GOODWILL / DISPUTE POINT ADJUSTMENT (Append-Only dbo.PointLedgerEntries):            |
| Customer Lookup *: [ minh.nguyen@example.vn ] -> Balance: 649 Available pts | 1,960 Qualifying pts   |
| Signed Points Delta * : [ +150 ] pts  (Non-zero integer; negative cannot drop AvailablePoints < 0)   |
| Affect Qualifying Pts?: [x] Credit LifetimeQualifyingPoints (Evaluates automatic tier promotion)     |
| Mandatory Audit Note *: [ Goodwill credit for delayed spare-part shipment on Ticket #CS-2041       ] |
|                                                        [ Append Immutable Adjustment Entry -> ]      |
+------------------------------------------------------------------------------------------------------+
MOBILE (< 768px): 4 Tier cards stack vertically; Adjustment form renders as a single-column audited card.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<admin-loyalty-tiers>` -> `<tier-governance-card>` (4 canonical ranks) + `<admin-point-adjustments>` -> `<customer-wallet-inspector>`, `<ledger-adjustment-form>`, `<confirm-modal>`.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
- **Tier Benefits Editor (`PUT /api/v1/loyalty/tiers/{tierId}`)**: Edits `displayName` (`NVARCHAR(60)`) and structured perks list (`benefitsJson`, validated `ISJSON = 1` per `CK_LoyaltyTiers_BenefitsJson`), while protecting canonical `TierCode` (`'Bronze'`, `'Silver'`, `'Gold'`, `'Platinum'`) and monotonic `RankOrder` (`1..4`).
- **Audited Adjustment (`POST /api/v1/loyalty/accounts/{accountId}/adjustments`)**:
  - `pointsDelta`: Non-zero integer (`pointsDelta !== 0` per `CK_PointLedgerEntries_NonZero`) and `availablePoints + pointsDelta >= 0` (`CK_PointLedgerEntries_Balance`).
  - `notes`: **Mandatory** `15..500` chars (`NVARCHAR(500)`) explaining business justification (`400 VALIDATION_FAILED` if blank or `< 15` chars).
  - `accountRowVersion` + `Idempotency-Key: <UUIDv4>` (`UQ_PointLedgerEntries_IdempotencyKey`).

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/loyalty/tiers`, `PUT /api/v1/loyalty/tiers/{tierId}`, `GET /api/v1/vehicles/lookup` / `GET /api/v1/loyalty/customers/{customerId}`, `POST /api/v1/loyalty/accounts/{accountId}/adjustments`.
- **SignalR Dispatch**: Adjustment immediately pushes `LoyaltyPointsUpdated` (and `TierUpgraded` if threshold crossed) to `user-{customerId}`.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching 4 tiers or customer loyalty account | 4-row `<skeleton-loader variant="table-rows">`. |
| **`Empty`** | No customer selected yet on Tab 2 | Search prompt `"Lookup a customer account to record an audited point adjustment"`. |
| **`Populated`** | 4 canonical tiers rendered / customer wallet loaded | Interactive benefits tag editor + live post-adjustment balance preview. |
| **`Validation Error (400/422)`** | `pointsDelta === 0`, missing audit `notes` (`400`), or negative delta exceeding `AvailablePoints` (`422`) | Inline field error via `ProblemDetailsMapper`. |
| **`Concurrency Conflict (409)`** | `LoyaltyAccounts.RowVersion` changed mid-review (`409 CONCURRENCY_CONFLICT`) | Reloads customer's latest `AvailablePoints` and `rowVersion` before prompting Admin to confirm again. |

---

### 12. `SCR-ADM-06` — Reward Voucher Catalog & Quota Studio

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Administrative studio for creating and governing `VoucherCatalogs` reward templates (`CodePrefix`, `ApplicableScope`, `PointsCost`, `DiscountType` `'FixedAmount'` vs `'Percentage'`, `MaxDiscountAmount`, `MinOrderAmount`, `ValidDays`, `QuotaTotal` vs `QuotaRedeemed`, `MinTierId`, and `IsActive`) surfaced in `SCR-CUS-05` (`FR-05`, `EC-11`).
- **UI-Router State**: `app.admin.voucherCatalog` (`/admin/loyalty/vouchers`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.Loyalty.ManageVoucherCatalog']`.
- **Component Path**: `frontend/src/app/features/admin-analytics/admin-voucher-catalog.component.js` (`<admin-voucher-catalog>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — Voucher Catalog Table + Quota Progress + Create/Edit Slide-Over)
+------------------------------------------------------------------------------------------------------+
| REWARD VOUCHER CATALOG & QUOTA STUDIO                                    [+ Create Voucher Template] |
+------------------------------------------------------------------------------------------------------+
| Scope: [All Scopes v]   Min Tier: [All Tiers v]   Status: [Active Only v]                            |
+------------------------------------------------------------------------------------------------------+
| Prefix     | Reward Title                     | Scope       | Min Tier | Cost    | Redeemed / Quota  |
|------------|----------------------------------|-------------|----------|---------|-------------------|
| SVC100K    | 100.000 ₫ Off Periodic Service   | ServiceOnly | Bronze   | 200 pts | [████░░]  42 / 100|
| SVC15PCT   | 15% Off Maintenance (Max 200k ₫) | ServiceOnly | Silver   | 300 pts | [███░░░]  51 / 200|
| GOLD20PCT  | 20% Off Major Overhaul (Max 300k)| ServiceOnly | Gold     | 600 pts | [██░░░░]  12 / 50 |
| SHWRM1M    | 1.000.000 ₫ Showroom Bike Rebate | ShowroomOnly| Platinum | 900 pts | [█░░░░░]   4 / ∞  |
+------------------------------------------------------------------------------------------------------+
MOBILE (< 768px): Catalog templates render as stacked cards with quota bars and `[Edit / Pause]` buttons.
```

#### c) Component Hierarchy & `$ctrl` State / One-Time Bindings (`::`)
- **Hierarchy**: `<admin-voucher-catalog>` -> `<voucher-catalog-filter-bar>`, `<voucher-template-table>`, `<voucher-template-drawer>`. Uses `::` on `{{ ::item.codePrefix }}`.

#### d) Interactive Controls, Form Fields & Client/Server Validation Rules
| Field Name | SQL Constraint Anchor | Client & Server Validation Rules (`DATABASE_DESIGN.md` Table 20) |
| :--- | :--- | :--- |
| `codePrefix` | `UQ_VoucherCatalogs_CodePrefix` | Required uppercase alphanumeric `3..15` chars (`^[A-Z0-9]{3,15}$`); immutable once created. |
| `title`, `description` | `NVARCHAR(150)` / `NVARCHAR(500)` | Required (`5..150` and `10..500` chars). |
| `applicableScope` | `CK_VoucherCatalogs_Scope` | Exact enum: `'ServiceOnly'`, `'ShowroomOnly'`, `'All'`. |
| `pointsCost` | `CK_VoucherCatalogs_PointsCost` | Required integer `> 0`. |
| `discountType` & `discountValue` & `maxDiscountAmount` | `CK_VoucherCatalogs_DiscountRules` | If `'FixedAmount'`: `discountValue > 0` (`VND`), `maxDiscountAmount` hidden/`null`. If `'Percentage'`: `0 < discountValue <= 100` **AND `maxDiscountAmount > 0` is mandatory**! |
| `minOrderAmount` | `CK_VoucherCatalogs_MinOrder` | Required `DECIMAL(18,2) >= 0`. |
| `validDays` | `CK_VoucherCatalogs_ValidDays` | Required integer `1..3650` (default `90`). |
| `quotaTotal` | `CK_VoucherCatalogs_Quota` | Nullable integer (`null` = unlimited); when non-null, must satisfy `quotaTotal >= Math.max(1, item.quotaRedeemed)`. |
| `minTierId` | `FK_VoucherCatalogs_Tiers` | Required dropdown of `Bronze`, `Silver`, `Gold`, `Platinum`. |

#### e) API Calls & Real-Time SignalR Reactions
- **REST API Calls**: `GET /api/v1/vouchers/catalog`, `GET /api/v1/loyalty/tiers`, `POST /api/v1/vouchers/catalog`, `PUT /api/v1/vouchers/catalog/{catalogId}`.

#### f) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching `/api/v1/vouchers/catalog` | `<skeleton-loader variant="table-rows" rows="5">`. |
| **`Empty`** | `0` voucher templates match filter | Empty state `"No reward voucher templates found"` + `[+ Create Voucher Template]`. |
| **`Populated`** | Templates loaded | Displays `.u-mono` prefixes, tier badges, quota progress bars, and active/paused toggle. |
| **`Validation Error (400/422)`** | `'Percentage'` selected without `maxDiscountAmount`, or `quotaTotal < quotaRedeemed` (`400`/`422`) | Inline field error via `ProblemDetailsMapper` inside drawer. |
| **`Concurrency Conflict (409)`** | Duplicate `CodePrefix` (`UQ_VoucherCatalogs_CodePrefix`) | Highlights `codePrefix` input with `"Voucher code prefix already in use"`. |

---

### 13. `SCR-ADM-07` — OEM Spare-Parts Master Catalog Studio (`v1.1.0`)

#### a) Purpose, Route, Role/Permission Guard & Component Path
- **Business Purpose**: Headquarters OEM spare-parts & workshop consumables catalog studio (`FR-08`, `ADR-001`, `ADR-003`) enabling `Admin` to create, price, categorize, bind model compatibility (`CompatibleModelId` or universal fit `NULL`), and activate/deactivate genuine `SpareParts` SKUs (`PartCode` e.g. `PRT-OIL-10W30`..`PRT-CVT-BLT01`) consumed by `SCR-STF-02` (`staffInvoiceBuilder`) while preserving 100% immutability on historical `ServiceInvoiceLineItems`.
- **UI-Router State**: `app.admin.spareParts` (`/admin/spare-parts`).
- **Guard Policy**: `requiresAuth: true`, `roles: ['Admin']`, `permissions: ['Permissions.SpareParts.ManageCatalog']`.
- **Component Path**: `frontend/src/app/features/admin/spare-parts.component.js` (`<admin-spare-parts>`).

#### b) Detailed Structured ASCII Layout Wireframe

```text
DESKTOP (>= 1024px — OEM Spare-Parts Master Registry + Create/Edit SKU Modal)
+------------------------------------------------------------------------------------------------------+
| OEM SPARE-PARTS & CONSUMABLES MASTER CATALOG STUDIO                        [+ Add OEM Spare Part]    |
+------------------------------------------------------------------------------------------------------+
| Category: [All Categories v]   Model Fitment: [All / Universal v]   Search SKU/Name: [             ] |
+------------------------------------------------------------------------------------------------------+
| Part Code (.u-mono)| OEM Part Name                        | Category  | Fitment       | Unit Price   |
|--------------------|--------------------------------------|-----------|---------------|--------------|
| PRT-OIL-10W30      | Pro Synthetic 10W-30 Oil (1L)        | EngineOil | Universal Fit | 180.000 ₫/btl|
| PRT-FLT-SH160      | Genuine Air Filter Element SH160i    | Filter    | Honda SH160i  | 240.000 ₫/pc |
| PRT-CVT-BLT01      | Kevlar V-Belt Drive Assembly         | Drivetrain| Honda SH160i  | 680.000 ₫/set|
+------------------------------------------------------------------------------------------------------+
```

#### c) Interactive Controls, Validation Rules & API Integration
- **Fields (`POST /api/v1/spare-parts` & `PUT /api/v1/spare-parts/{partId}`)**:
  - `partCode`: Required uppercase `^[A-Z0-9_-]{3,40}$` (`UQ_SpareParts_PartCode` -> `409 DUPLICATE_PART_CODE`, read-only on edit).
  - `name` (`NVARCHAR(160)`), `category` (`EngineOil`, `Filter`, `Brakes`, `Transmission`, `Tires`, `Electrical`, `Accessory`), `compatibleModelId` (`null` = Universal Fit across all models, or specific `MotorcycleModels.Id`), `unitLabel` (`NVARCHAR(24)`), `unitPriceVnd` (`DECIMAL(18,2) >= 0`), `isActive` (`boolean`).
- **REST API Calls**: `GET /api/v1/spare-parts?includeInactive=true&pageSize=100` (dual-mode endpoint exposing inactive SKUs strictly when caller holds `Permissions.SpareParts.ManageCatalog`), `GET /api/v1/motorcycles`, `POST /api/v1/spare-parts`, `PUT /api/v1/spare-parts/{partId}`.
- **Historical Invoice Protection**: Editing `unitPriceVnd` or toggling `isActive = false` affects future `SCR-STF-02` picker selections only; all settled `ServiceInvoiceLineItems` (`PartCodeSnapshot`, `UnitPriceVnd`, `LineTotalVnd`) remain immutable.

#### d) UI State Matrix
| UI State | Trigger Condition | Visual Presentation & Recovery UX |
| :--- | :--- | :--- |
| **`Loading`** | Fetching `/api/v1/spare-parts?includeInactive=true` | `<skeleton-loader variant="table-rows" rows="6">`. |
| **`Empty`** | Filter matches `0` OEM SKUs | Empty state `"No OEM spare parts match your filter"` + `[Reset Filters]` / `[+ Add OEM Spare Part]`. |
| **`Populated`** | `>= 1` SKU loaded (seeded with `8` OEM parts) | Displays `.u-mono` `PartCode`, fitment pill (`Universal Fit` vs model name), `vndCurrency` unit price, active badge, and `[Edit]` button. |
| **`Validation Error (400/422)`** | Negative `unitPriceVnd` or invalid `partCode` (`400 VALIDATION_FAILED`) | Inline field errors inside modal via `ProblemDetailsMapper`. |
| **`Concurrency Conflict (409)`** | Duplicate `PartCode` (`409 DUPLICATE_PART_CODE`) | Highlights `partCode` input inside modal without losing other form fields. |

---

### 14. `SCR-STF-07` — Zero-Refresh SignalR Warranty Claims & CSAT Reviews Workbench (`v1.2.0`)

- **UI-Router State & Guard**: `app.staff.warrantyReviews` (`/staff/warranty-reviews`), `roles: ['DealerStaff']`, `permissions: ['Permissions.Warranty.ViewDealer', 'Permissions.Warranty.ManageDealer', 'Permissions.Reviews.ViewDealer', 'Permissions.Reviews.RespondDealer']`.
- **Component Path**: `frontend/src/app/features/staff/warranty-reviews.component.js` (`<staff-warranty-reviews>`).
- **Operational Capabilities (`IDealerScoped` + `RowVersion`)**:
  - Subscribes to `ServiceReviewSubmitted`, `WarrantyClaimCreated`, and `WarrantyClaimStatusChanged` via `$rootScope.$applyAsync()` (`0` page reloads; listeners disposed in `$onDestroy`).
  - Allows branch advisors to post official `StaffResponse` replies (`PATCH /api/v1/service-reviews/{reviewId}/response`) and adjudicate `WarrantyClaims` (`PATCH /api/v1/warranty-claims/{claimId}/status` across `Submitted -> UnderReview -> Approved | Rejected -> Completed`) with model-compatible OEM `CoveredSparePartId` (`GET /api/v1/spare-parts`) and `ApprovedCoveredAmountVnd`.

---

### 15. `SCR-ADM-08` — Executive CSAT & Factory Warranty Quality Dashboard (`Chart.js v4`, `v1.2.0`)

- **UI-Router State & Guard**: `app.admin.qualityWarranty` (`/admin/quality-warranty`), `roles: ['Admin']`, `permissions: ['Permissions.Analytics.ViewAllDealers', 'Permissions.Reviews.ViewDealer', 'Permissions.Warranty.ViewDealer']`.
- **Component Path**: `frontend/src/app/features/admin/quality-warranty.component.js` (`<admin-quality-warranty>`).
- **Visualizations & Memory Safety (`RISK-04`)**:
  - Hydrates `GET /api/v1/admin/analytics/quality-warranty` (`?fromUtc=&toUtc=&dealerId=`) alongside cross-dealer `GET /api/v1/service-reviews` and `GET /api/v1/warranty-claims`.
  - Renders `Chart.js v4` `<dealer-chart>` visualizations comparing per-dealer `AverageCsatRating`, `WouldRecommendRatePercent`, warranty defect category distribution, and `ApprovedWarrantyCoveredVnd`, destroying all `Chart.js` instances cleanly inside `$onDestroy`.
