# Release Notes — `v1.2.0` (`Motorcycle Dealer & Loyalty Management Platform`)

| Release Attribute | Canonical Value |
| :--- | :--- |
| **Version** | **`v1.2.0` / `v1.2.1` Cross-Phase Forensic Hardened** (Phase 9 Feature Expansion — Post-Service 5-Star CSAT Reviews & VIN Warranty Claims) |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **GitHub Repository** | [https://github.com/quocanhh86/motorcycle-dealer](https://github.com/quocanhh86/motorcycle-dealer) |
| **Engineering Timeline & Team** | `02/2026 - 08/2026` · Solo Full-Stack Engineering Release (`Team Size: 1`) |
| **Back-End Runtime** | `C#`, `ASP.NET Core Web API` (`.NET 10` / `net10.0`, `18` Controllers), `Entity Framework Core 10`, `SignalR`, `FluentValidation` |
| **Front-End SPA** | Decoupled Modular `AngularJS 1.8.3` (`ng-strict-di`, `30` Navigable Routes), `@uirouter/angularjs`, `Chart.js v4`, `@microsoft/signalr` |
| **Database Engine** | Microsoft SQL Server (`(localdb)\MSSQLLocalDB` & SQL Server 2022 Docker) · `MotorcycleDealerDb` (`27` 3NF tables, `RCSI`) |
| **Roadmap & Gate Status** | **`9 / 9` Phases (`80 / 80` WBS Tasks = `100.0%`)** · Passed Mandatory Per-Phase **7-Stage Quality Loop (`15R + 6T`)** |

---

## 1. Executive Summary

**`v1.2.0`** expands the **`v1.1.0`** platform with **Phase 9 (`T-901`..`T-908`)**, delivering closed-loop **Post-Service 5-Star CSAT `ServiceReviews` Awarding `+25` Spendable Bonus Loyalty Points**, **36-Month / 30,000-km Factory VIN `WarrantyClaims` Management**, **3 New Real-Time SignalR Events** (`ServiceReviewSubmitted`, `WarrantyClaimCreated`, `WarrantyClaimStatusChanged` -> `10` total events), **8 New Granular RBAC Permissions** (`48` total policies: `Customer = 20`, `DealerStaff = 22`, `Admin = 32`), and **3 New AngularJS 1.8 Screens** (`SCR-CUS-07`, `SCR-STF-07`, `SCR-ADM-08` -> `30` navigable UI-Router routes).

---

## 2. What's New in `v1.2.0` (`T-901` .. `T-908`)

### 2.1 Post-Service 5-Star CSAT `ServiceReviews` & `+25` Spendable Bonus Loyalty Credit (`T-901`, `T-902`, `T-905`)
- **Normalized `ServiceReviews` Aggregate (`Table 26`, `IDealerScoped`, `RowVersion`)**:
  - Enforces a strict `1:1` link to a settled workshop `ServiceInvoice` (`UQ_ServiceReviews_ServiceInvoiceId` -> `409 REVIEW_ALREADY_SUBMITTED` under parallel race contention), capturing `ReviewNumber` (`REV-YYYYMMDD-XXXXXX`), `Rating` (`1..5`), `QualityTags` (`string[]` / CSV up to `8` tags), `Comment`, and optional branch `StaffResponse` (`RespondedByUserId`, `RespondedAtUtc`).
- **Idempotent `+25` Spendable Bonus Points (`ADR-003` Compliant)**:
  - Submitting `POST /api/v1/service-reviews` atomically appends an immutable `PointLedgerEntries` row (`TransactionType = 'ServiceReviewBonus'`, `PointsDelta = +25`, `ReferenceType = 'ServiceReview'`, `IdempotencyKey = $"SERVICE_REVIEW:{ServiceInvoiceId}:BONUS"`) and increments **`LoyaltyAccounts.AvailablePoints += 25` ONLY**, keeping **`LifetimeQualifyingPoints` strictly unchanged** (`ReviewBonusDriftCount == 0`), filterable via `GET /api/v1/loyalty/ledger?transactionType=ServiceReviewBonus`.
- **REST `/api/v1/service-reviews` & Live SignalR Push**:
  - `ServiceReviewsController` exposes `POST /api/v1/service-reviews` (`Permissions.Reviews.CreateOwn`), `GET /api/v1/service-reviews/my` (`Permissions.Reviews.ViewOwn`, `?rating=&minRating=`), `GET /api/v1/service-reviews` (`Permissions.Reviews.ViewDealer`, `?dealerId=&rating=&minRating=&unansweredOnly=&search=`), `GET /api/v1/service-reviews/by-invoice/{invoiceId}`, and `PATCH /api/v1/service-reviews/{reviewId}/response` (`Permissions.Reviews.RespondDealer`, `RowVersion` guarded).
  - Broadcasts post-commit `ServiceReviewSubmitted` to `dealer-{dealerId}` + `role-Admin` and `LoyaltyPointsUpdated` to `user-{customerId}`.

### 2.2 36-Month / 30,000-km Factory VIN `WarrantyClaims` & Live Adjudication (`T-901`, `T-903`, `T-904`, `T-905`)
- **Normalized `WarrantyClaims` Aggregate (`Table 27`, `IDealerScoped`, `RowVersion`)**:
  - Tracks customer factory warranty claims (`WC-YYYYMMDD-XXXXXX`) per `CustomerVehicle` (`VIN`) and `Dealer`, capturing `DefectCategory` (`Engine`, `Transmission`, `Electrical`, `Brakes`, `FrameSuspension`, `PaintBody`, `Other`), `SymptomDescription`, `MileageAtClaimKm`, `Status` (`Submitted`, `UnderReview`, `Approved`, `Rejected`, `Completed`), optional OEM `CoveredSparePartId`, `ApprovedCoveredAmountVnd`, and `ResolutionNotes`.
  - Enforces factory warranty coverage (`OwnershipStartDateUtc + WarrantyMonths [36]` and `MileageAtClaimKm <= WarrantyMileageKm [30,000]` -> `422 WARRANTY_COVERAGE_EXPIRED`), monotonic odometer (`MileageAtClaimKm >= CurrentMileageKm` -> `422 ODOMETER_REGRESSION`), active duplicate claim prevention (`409 ACTIVE_WARRANTY_CLAIM_EXISTS`), and OEM `CoveredSparePartId` model compatibility (`422 SPARE_PART_INCOMPATIBLE` / `SPARE_PART_INACTIVE`).
- **REST `/api/v1/warranty-claims` & Zero-Refresh SignalR Dispatch**:
  - `WarrantyClaimsController` exposes `POST /api/v1/warranty-claims` (`Permissions.Warranty.CreateOwn`), `GET /api/v1/warranty-claims/my` (`Permissions.Warranty.ViewOwn`), `GET /api/v1/warranty-claims` (`Permissions.Warranty.ViewDealer`), `GET /api/v1/warranty-claims/{claimId}`, and `PATCH /api/v1/warranty-claims/{claimId}/status` (`Permissions.Warranty.ManageDealer`, `RowVersion` guarded).
  - Broadcasts post-commit `WarrantyClaimCreated` to `dealer-{dealerId}` + `role-Admin` and `WarrantyClaimStatusChanged` to `user-{customerId}` + `dealer-{dealerId}`.

### 2.3 AngularJS `1.8.3` Portals (`30` Routes) & Admin Quality Analytics (`T-904`..`T-906`)
- **Customer Warranty & CSAT Portal (`app.customer.warranty`, `SCR-CUS-07`) + `SCR-CUS-02` / `SCR-CUS-04` CTAs**:
  - Interactive 5-star review modal (`+25` bonus points badge) on paid workshop invoices and VIN warranty eligibility badge + claim filing form in the customer portal.
- **Dealer Staff Live Warranty & Reviews Workbench (`app.staff.warrantyReviews`, `SCR-STF-07`)**:
  - Receives `ServiceReviewSubmitted` and `WarrantyClaimCreated` cards in real time with **zero page reloads**, supporting branch CSAT responses and `RowVersion`-guarded warranty claim adjudication with compatible OEM spare-part selection.
- **Admin CSAT & Warranty Quality Dashboard (`app.admin.qualityWarranty`, `SCR-ADM-08`)**:
  - Powered by `GET /api/v1/admin/analytics/quality-warranty`, visualizing per-dealer `AverageCsatRating`, recommendation rates, warranty defect breakdown, and `ApprovedWarrantyCoveredVnd` via leak-free `Chart.js v4` components (`chart.destroy()` on `$onDestroy`).

### 2.4 Expanded `48`-Permission RBAC, `SMK-10` Smoke Probe & Governance Sync (`T-901`, `T-907`, `T-908`)
- **48 Canonical RBAC Policies**: Adds `Permissions.Reviews.{CreateOwn,ViewOwn,ViewDealer,RespondDealer}` (`4`) and `Permissions.Warranty.{CreateOwn,ViewOwn,ViewDealer,ManageDealer}` (`4`) -> `48` total policies (`Customer = 20`, `DealerStaff = 22`, `Admin = 32`, `RolePermissions = 74`).
- **Automated Verification & Sync Gates**: Adds `SMK-10` (`smoke-fullstack.{ps1,sh}`), `30`-route Headless Edge CDP sweep, `ReviewBonusDriftCount == 0` LocalDB reconciliation in `run-phase-quality-loop.ps1`, and dynamic `27 / 48 / 80 / 10` snapshot validation across all `370` workspace files in `verify-post-task-sync.mjs`.

---

## 3. Final `v1.2.0` / `v1.2.1` Empirical Verification & Cross-Phase Quality-Loop Metrics

| Verification Suite | Verified Result | Coverage Highlights |
| :--- | :--- | :--- |
| **Phase 9 & Cross-Phase (`P1..P9`) 7-Stage Loop** | **`15 / 15` Reviews + `6 / 6` Test Batteries PASS** | `5R -> 3T -> Fix -> 5R -> Refactor -> 5R -> 3T` (`run-phase-quality-loop.ps1 -Phase 9 -Round 3`, `0` warnings) |
| **`.NET 10` xUnit Automated Suite** | **`727 / 727` PASS (`0` warnings, `0` flaky)** | `645` `Domain.UnitTests` + `82` SQL Server LocalDB `Api.IntegrationTests` (`StrayTestDbCount = 0`) |
| **AngularJS `1.8.3` SPA Verification** | **`64 / 64` Automated Checks PASS** | `45` `.js` + `3` `.mjs` files, `47` `ng-strict-di` registrations, `35` UI-Router states / `30` navigable routes, `36`-route bell matrix |
| **Full-Stack HTTP & SignalR Smoke** | **`10 / 10` Probes PASS (`SMK-01`..`SMK-10`)** | Health, OWASP headers, `401`/`403` `ProblemDetails`, `IDealerScoped`, SignalR, all `9` Analytics bindings (`/quality-warranty` + `17`-col CSV), Spare-Parts/Test-Drives/Receipts, plus `SMK-10` Reviews & Warranty |
| **Headless Edge CDP Closed-Loop E2E** | **`30 / 30` Navigable Routes (`0` Console Errors)** | `INV-202608-0001` `5★` `REV-20261123-AAEC37` (`+25` `AvailablePoints` `649->674`, `Lifetime=1,960` unchanged) + zero-reload `WC-20261123-F12F84` `Submitted->Approved->Completed` (`350,000 VND`) |
| **SQL Server LocalDB & Post-Task Sync** | **`27 / 27` 3NF Tables & `370 / 370` Files (`0` Drift)** | `LedgerDriftCount = 0`, `SlotDriftCount = 0/309`, `VoucherQuotaDriftCount = 0/5`, `InvoiceLineItemsDriftCount = 0`, `ReviewBonusDriftCount = 0`, `.docx` `validate.py` PASS |
