---
name: loyalty-booking-domain
description: >-
  Use this skill when implementing core domain rules, transactional workflows, and background workers for ISO 3779 17-character VIN validation, atomic Showroom SalesOrders completion with automatic CustomerVehicles onboarding, concurrency-safe ServiceTimeSlots workshop booking and TestDriveBookings scheduling with RowVersion, VinMaintenanceReminderWorker BackgroundService with DeduplicationKey, itemized ServiceInvoices settlement with OEM SpareParts, ServiceInvoiceLineItems, CustomerVouchers, and printable receipts, post-service 5-star CSAT ServiceReviews awarding +25 AvailablePoints bonus, 36-month / 30,000-km VIN WarrantyClaims, immutable append-only PointLedgerEntries, two-step floor loyalty point calculation, or 4-tier membership progression (Bronze, Silver, Gold, Platinum). Also activate on Vietnamese developer intents such as "kiểm tra số khung VIN 17 ký tự", "bán xe showroom SalesOrders tự động thêm vào garage", "đặt lịch bảo dưỡng / lái thử chống trùng khung giờ RowVersion", "nhắc lịch bảo dưỡng tự động theo VIN", "hóa đơn phụ tùng chi tiết ServiceInvoiceLineItems / in biên lai", "đánh giá 5 sao tặng +25 điểm", "bảo hành xe VIN 36 tháng / 30.000 km", "tính điểm tích lũy hai bước làm tròn xuống", "nâng hạng thành viên", or "đổi và áp dụng voucher".
---

# Showroom VIN, Test-Drives, Itemized Workshop Booking & Immutable Loyalty Engine Skill (`loyalty-booking-domain`)

Authoritative domain logic and transactional orchestration runbook for `MotorcycleDealer.Domain`, `MotorcycleDealer.Application`, and `VinMaintenanceReminderWorker` (`MotorcycleDealer.Infrastructure`), governed by [AGENTS.md](../../../AGENTS.md), [docs/DESIGN.md](../../../docs/DESIGN.md) (`FR-02`..`FR-06`), and [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) (`ADR-003`, `ADR-005`).

---

## 1. MCP-Accelerated Domain Verification Workflow

Before hardcoding math assertions or committing multi-aggregate transactions across the `25` 3NF tables, leverage the workspace MCP servers:

1. **Canonical Loyalty Math & Tier Oracle (`motorcycle-dealer-guardrails` MCP)**:
   - Call `calculate_loyalty_preview` (`netPaidAmountVnd`, `tierMultiplier`, `currentLifetimeQualifyingPoints`) to verify exact two-step floor point outcomes and automatic tier jumps (e.g., `455,000 VND` at `Gold 1.50x` -> `67` pts; `101,000,000 VND` at `Bronze 1.00x` -> `10,100` pts + instant `Bronze -> Platinum` promotion).
   - Call `lookup_db_table_spec` (`SalesOrders`, `TestDriveBookings`, `CustomerVehicles`, `ServiceTimeSlots`, `ServiceBookings`, `SpareParts`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `LoyaltyAccounts`, `PointLedgerEntries`, `CustomerVouchers`, `MaintenanceReminders`) to align domain transitions with SQL `CHECK` constraints.
2. **Post-Transaction SQL Invariant Audit (`sqlserver-localdb-inspector` MCP)**:
   - Call `query_readonly_sql` against `(localdb)\MSSQLLocalDB` (never global `dbhub`, which targets Oracle!) to verify zero ledger drift (`AvailablePoints == SUM(PointsDelta)`), zero overbooked slots, zero invoice line-item drift (`GrossAmount == SUM(LineTotalVnd)`), and zero duplicate VIN reminders.

---

## 2. ISO 3779 17-Character `VIN` & Garage Odometer Invariants (`FR-02`, `FR-03`)

- **`Vin` Value Object (`MotorcycleDealer.Domain/ValueObjects/Vin.cs`)**:
  - Normalize via `.Trim().ToUpperInvariant()`.
  - Enforce exact length `17` matching `^[A-HJ-NPR-Z0-9]{17}$` — **strictly excluding `I`, `O`, and `Q`** per ISO 3779.
- **Dual Garage Onboarding Paths (`dbo.CustomerVehicles`)**:
  1. **Showroom Auto-Onboarding**: Created atomically inside `CreateSalesOrder` (`PurchasedSalesOrderId = salesOrder.Id`, `CurrentMileageKm = 0`, `LastServiceMileageKm = 0`, `LastServiceDateUtc = completedAtUtc`).
  2. **External Manual VIN Registration** (`POST /api/v1/vehicles`): Customer registers an externally bought bike (`PurchasedSalesOrderId = null`). Reject with `409 DUPLICATE_VIN` if `VIN` already exists in active `CustomerVehicles`.
- **Monotonic Odometer Rule**: Updating `CurrentMileageKm`, booking (`OdometerAtBookingKm`), or completing service (`OdometerAtCompletionKm`) MUST enforce `newKm >= CurrentMileageKm` (`422 ODOMETER_REGRESSION` on regression).

---

## 3. Canonical Loyalty Formula, 4-Tier Progression & 4 Immutable Tables (`ADR-003`, `FR-05`)

### 3.1 Deterministic Two-Step Floor Calculation (`10,000 VND = 1 Base Point`)
Both Showroom `SalesOrders.FinalAmount` and Workshop `ServiceInvoices.NetPaidAmount` MUST use `LoyaltyTierCalculator` in `MotorcycleDealer.Domain` with `decimal` arithmetic (never `double`/`float`):

```csharp
public static int CalculatePointsEarned(decimal netPaidAmountVnd, decimal tierMultiplier)
{
    if (netPaidAmountVnd <= 0m) return 0;
    decimal basePoints = Math.Floor(netPaidAmountVnd / 10_000m);
    return (int)Math.Floor(basePoints * tierMultiplier);
}
```

### 3.2 Canonical 4-Tier Thresholds & 4 Immutable Financial/Audit Tables

| `TierCode` | `RankOrder` | `MinQualifyingPoints` | `PointsMultiplier` | Upgrade & Redemption Invariants |
| :--- | :-: | :-: | :-: | :--- |
| `Bronze` | `1` | `0` | `1.00x` | Provisioned automatically on `POST /api/v1/auth/register`. |
| `Silver` | `2` | `500` | `1.20x` | Promoted automatically when `LifetimeQualifyingPoints >= 500`. |
| `Gold` | `3` | `2,000` | `1.50x` | Promoted automatically when `LifetimeQualifyingPoints >= 2,000`. |
| `Platinum` | `4` | `5,000` | `2.00x` | Promoted automatically when `LifetimeQualifyingPoints >= 5,000`. |

1. **Pre-Transaction Multiplier Rule**: `PointsEarned` uses `CurrentTier.PointsMultiplier` active **at payment time**; crossing higher thresholds promotes `CurrentTierId` immediately for all **subsequent** transactions and emits SignalR `TierUpgraded`.
2. **Voucher Spend & Review Bonus Non-Qualifying Invariant**: Redeeming a `VoucherCatalogs` reward deducts `PointsCost` **exclusively** from `LoyaltyAccounts.AvailablePoints`, and submitting a 5-star CSAT `ServiceReview` (`LedgerTransactionType.ServiceReviewBonus`, `SERVICE_REVIEW:{ServiceInvoiceId}:BONUS`) credits `+25` **exclusively** to `LoyaltyAccounts.AvailablePoints`. **`LifetimeQualifyingPoints` changes ONLY on qualifying spend (`SalesOrderEarn`, `ServiceInvoiceEarn`)**.
3. **4 Strictly Immutable Financial/Audit Tables**:
   - **NEVER** execute `UPDATE` or `DELETE` on **`PointLedgerEntries`**, **`SalesOrders`**, **`ServiceInvoices`**, or **`ServiceInvoiceLineItems`** (`IAppendOnlyEntity` / `AppendOnlyLedgerInterceptor`).
   - Every `PointLedgerEntries` row enforces unique `IdempotencyKey` (`SALES_ORDER:{SalesOrderId}:EARN`, `SERVICE_INVOICE:{ServiceInvoiceId}:EARN`, `VOUCHER_REDEEM:{CustomerVoucherId}:SPEND`, `SERVICE_REVIEW:{ServiceInvoiceId}:BONUS`, or audited `ADMIN_ADJ:{requestId}`).

---

## 4. Transactional Workflows & `6`-Table `RowVersion` Concurrency (`ADR-005`, `FR-04`)

### 4.1 Atomic Showroom Sale & Printable Delivery Receipt (`POST /api/v1/sales-orders`, `GET /api/v1/sales-orders/{id}/receipt`)
Execute inside one `BeginTransactionAsync` scope:
1. Enforce `DealerId` scope (`CurrentUser.DealerId` for `DealerStaff`); load `DealerInventories` (`StockStatus == "Available"`, verify `RowVersion`), transition `StockStatus = "Sold"`, `SoldAtUtc = UtcNow`.
2. Validate optional `AppliedCustomerVoucherId` (`ShowroomOnly` / `All`), insert immutable `SalesOrders`, auto-onboard `CustomerVehicles` (`VIN = inventory.VIN`, next `MaintenanceRule` due targets), append `PointLedgerEntries`, update `LoyaltyAccounts` (`RowVersion`) + tier upgrade, persist `Notifications`, commit, and broadcast `LoyaltyPointsUpdated` (+ optional `TierUpgraded`).
3. Expose structured printable delivery receipt DTO at `GET /api/v1/sales-orders/{id}/receipt` enforcing customer ownership / `IDealerScoped` isolation.

### 4.2 Showroom Test-Drive Scheduling (`POST /api/v1/test-drives`, `PATCH /api/v1/test-drives/{id}/status`)
1. Guard `TestDriveBookings` with `IDealerScoped` (`DealerId`) and `RowVersion` (`IConcurrencyProtected`).
2. Validate active `Dealer` and `MotorcycleModel`, verify operating hours and per-dealer/model concurrent demo capacity (preventing overlapping active customer/model slots with `409 TEST_DRIVE_CONFLICT`), and insert `TestDriveBookings` (`Status = "Requested"`).
3. Persist `Notifications` -> commit -> dispatch SignalR `TestDriveCreated` to `dealer-{dealerId}` & `role-Admin`. Status transitions (`Confirmed` with optional demo `AssignedInventoryId`, `Completed`, `Cancelled`, `NoShow`) verify `RowVersion` and emit `TestDriveStatusChanged` to `user-{customerId}` & `dealer-{dealerId}`.

### 4.3 Anti-Double-Booking Workshop Slot Reservation (`POST /api/v1/bookings`)
1. Verify `CustomerVehicle` ownership, monotonic `OdometerAtBookingKm`, optional `ReservedOnBooking` voucher lock (`CustomerVouchers.RowVersion`), and `!slot.IsBlocked && slot.BookedCount < slot.MaxCapacity`.
2. Increment `ServiceTimeSlots.BookedCount += 1` guarded by `ServiceTimeSlots.RowVersion` (with bounded contention retry; overflow returns `409 SLOT_CAPACITY_EXHAUSTED`), commit, and emit SignalR `BookingCreated`.

### 4.4 Itemized Workshop `ServiceInvoices` Settlement & Receipt (`POST /api/v1/service-invoices`, `GET /api/v1/service-invoices/{id}/receipt`)
1. Require `ServiceBookings.Status` in `"CheckedIn"` or `"InProgress"` and 1:1 uniqueness (`UQ_ServiceInvoices_BookingId`).
2. Build immutable `ServiceInvoiceLineItems` (`ItemType IN ('Part', 'Labor')`, snapshotting OEM `SpareParts` `PartCode`/`Description`/`UnitPriceVnd` or labor lines where `LineTotalVnd == Quantity * UnitPriceVnd`). Ensure exact reconciliation: `PartsAmount == SUM(Part LineTotalVnd)`, `LaborAmount == SUM(Labor LineTotalVnd)`, and `GrossAmount == LaborAmount + PartsAmount == SUM(LineTotalVnd)`.
3. Apply eligible `CustomerVoucher` discount (`NetPaidAmount = GrossAmount - VoucherDiscountAmount`), compute two-step `PointsEarned`, insert immutable `ServiceInvoices` + `ServiceInvoiceLineItems`, roll forward `CustomerVehicles` mileage/due schedule, resolve `MaintenanceReminders`, append `PointLedgerEntries`, update `LoyaltyAccounts` + tier check, commit, and emit SignalR events.

### 4.5 `VinMaintenanceReminderWorker` (`BackgroundService`, `ADR-005`)
- Evaluates active `CustomerVehicles` in batches against `MaintenanceRules` (`200 km` or `7 days` lead window) and enforces unique `DeduplicationKey` (`{VIN}:{MaintenanceRuleId}:{TargetMileageKm}`) on `MaintenanceReminders` (`UQ_MaintenanceReminders_DedupKey`).

---

## 5. Validation & Mandatory Post-Task Sync Steps

1. **Guardrail & 4-Table Immutability Static Scan**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/audit-dealer-guardrails.ps1`
2. **Domain Unit & Concurrency Integration Tests**:
   - Command: `dotnet test backend/MotorcycleDealer.sln -c Release --filter "FullyQualifiedName~Loyalty|FullyQualifiedName~Booking|FullyQualifiedName~TestDrive|FullyQualifiedName~Vin"`
3. **End-to-End Smoke & Post-Task Sync Gate**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1`
   - Command: `node .agents/scripts/verify-post-task-sync.mjs`
