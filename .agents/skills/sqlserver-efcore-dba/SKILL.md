---
name: sqlserver-efcore-dba
description: >-
  Use this skill when authoring or modifying Entity Framework Core 10 entity configurations (IEntityTypeConfiguration<T>), generating or applying SQL Server migrations for MotorcycleDealerDb (27 normalized 3NF tables), configuring Guid.CreateVersion7() + NEWSEQUENTIALID() keys, DECIMAL(18,2) money, DATETIMEOFFSET(7) UTC timestamps, 8 RowVersion optimistic concurrency tokens, 8 IDealerScoped global query filters, 4 immutable financial/audit tables, ON DELETE RESTRICT foreign keys, B-Tree covering/filtered indexes, Read Committed Snapshot Isolation (RCSI), or idempotent 3-dealer demo seeding. Also activate on Vietnamese developer intents such as "tạo migration EF Core 10 SQL Server", "cấu hình 27 bảng chuẩn 3NF MotorcycleDealerDb", "thiết lập khóa ngoại ON DELETE RESTRICT và RowVersion", "tối ưu index B-Tree / RCSI", or "tạo dữ liệu mẫu seed 3 đại lý Hà Nội - Đà Nẵng - TP.HCM".
---

# SQL Server 3NF Schema (`27` Tables) & EF Core 10 DBA Skill (`sqlserver-efcore-dba`)

Authoritative database engineering runbook for `MotorcycleDealerDb` (`(localdb)\MSSQLLocalDB` in local dev; SQL Server 2022 `mcr.microsoft.com/mssql/server:2022-latest` in Docker), governed by [AGENTS.md](../../../AGENTS.md), [docs/DATABASE_DESIGN.md](../../../docs/DATABASE_DESIGN.md), and [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) (`ADR-002`, `ADR-003`, `ADR-005`).

---

## 1. MCP-Accelerated Workflow & Critical DB Server Routing

> [!CAUTION]
> **NEVER Use Global `dbhub` for `MotorcycleDealerDb`**: In this environment, the global `dbhub` MCP server connects to an external **Oracle** instance from an unrelated project. For all SQL Server `MotorcycleDealerDb` checks on `(localdb)\MSSQLLocalDB`, **ALWAYS** use the workspace **`sqlserver-localdb-inspector`** MCP server registered in `.agents/plugins/motorcycle-dealer-suite/mcp_config.json`.

Execute this MCP workflow whenever authoring configurations, migrations, indexes, or seeders:

1. **Authoritative Table Spec Lookup (`motorcycle-dealer-guardrails` MCP)**:
   - Call `check_phase_gate` (EF Core migrations and seeders belong to **Phases 4–8+**).
   - Call `lookup_db_table_spec` (`tableName`) to pull exact column names, SQL Server types, `CHECK` constraints, `UNIQUE` constraints, and `FK` rules directly from [docs/DATABASE_DESIGN.md](../../../docs/DATABASE_DESIGN.md) before writing `IEntityTypeConfiguration<T>`.
2. **EF Core 10 API Verification (`context7_context7` MCP)**:
   - Call `resolve-library-id` -> `query-docs` (`language: "C#"`) for **Entity Framework Core 10** (`ToTable(t => t.HasCheckConstraint(...))`, `HasIndex(...).IncludeProperties(...).HasFilter(...)`, `.IsRowVersion()`, and migration customization).
3. **Live `(localdb)\MSSQLLocalDB` Introspection (`sqlserver-localdb-inspector` MCP)**:
   - `sqlserver_status`: Verify `MSSQLLocalDB` instance health and whether `MotorcycleDealerDb` is online.
   - `list_sqlserver_tables`: Confirm **exact count of `27` user tables** in `dbo` plus seeded row counts.
   - `inspect_sqlserver_schema`: Verify deployed SQL column precision (`decimal(18,2)`, `datetimeoffset(7)`, `varchar(17)`), `ROWVERSION` columns (`8` tables), `ON DELETE NO ACTION` FKs, and all B-tree covering/filtered indexes.
   - `query_readonly_sql`: Run read-only `SELECT` diagnostics (`is_read_committed_snapshot_on`, ledger & invoice line-item reconciliation, 3-dealer seed balance).

---

## 2. Master 27-Table 3NF Inventory (`dbo`, Collation `SQL_Latin1_General_CP1_CI_AS`)

Implement **`27` dedicated `IEntityTypeConfiguration<T>` classes** under `backend/src/MotorcycleDealer.Infrastructure/Persistence/Configurations/` (`ApplyConfigurationsFromAssembly`), keeping `MotorcycleDealer.Domain` 100% free of ORM attributes:

| Bounded Context | Tables (`27` Total) | `IDealerScoped` (`8` Tables) | `RowVersion` (`8` Tables) | Deletion / Immutability Policy |
| :--- | :--- | :--- | :--- | :--- |
| **1. Identity & RBAC (6)** | `Users`, `Roles`, `Permissions`, `UserRoles` *(PK: `UserId, RoleId`)*, `RolePermissions` *(PK: `RoleId, PermissionId`)*, `RefreshTokens` | `Users.DealerId` nullable (`NOT NULL` for `DealerStaff`) | None | `Users`: Soft Delete (`IsDeleted`); `Roles`/`Permissions`: System Seed (`48` permissions); `UserRoles`/`RolePermissions`/`RefreshTokens`: Cascade/Revoke |
| **2. Dealers & Showroom (5)** | `Dealers`, `MotorcycleModels`, `DealerInventories`, `SalesOrders`, `TestDriveBookings` | `DealerInventories`, `SalesOrders`, `TestDriveBookings` (all `NOT NULL`) | **`DealerInventories`**, **`TestDriveBookings`** | `Dealers`, `MotorcycleModels`: Soft Delete; `DealerInventories`/`TestDriveBookings`: Status-Driven; **`SalesOrders`: Strictly Immutable** |
| **3. Customer Garage & Warranty (4)** | `CustomerVehicles`, `MaintenanceRules`, `MaintenanceReminders`, `WarrantyClaims` | `WarrantyClaims` (`NOT NULL`) | **`WarrantyClaims`** | `CustomerVehicles`: Soft Delete; `MaintenanceRules`: `IsActive`; **`MaintenanceReminders`: Immutable Audit Log**; `WarrantyClaims`: Status-Driven |
| **4. Booking, Parts, Invoicing & CSAT (6)** | `ServiceTimeSlots`, `ServiceBookings`, `SpareParts`, `ServiceInvoices`, `ServiceInvoiceLineItems`, `ServiceReviews` | `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `ServiceReviews` (all `NOT NULL`) | **`ServiceTimeSlots`**, **`ServiceBookings`**, **`ServiceReviews`** | `ServiceTimeSlots`: `IsBlocked`; `ServiceBookings`: Status-Driven; `SpareParts`: Soft Delete; **`ServiceInvoices` & `ServiceInvoiceLineItems`: Strictly Immutable**; `ServiceReviews`: Permanent 1:1 CSAT Record |
| **5. Loyalty & Vouchers (5)** | `LoyaltyTiers`, `LoyaltyAccounts`, `PointLedgerEntries`, `VoucherCatalogs`, `CustomerVouchers` | None (Platform/Account-scoped) | **`LoyaltyAccounts`**, **`CustomerVouchers`** | `VoucherCatalogs`: Soft Delete; **`PointLedgerEntries`: Strictly Append-Only (`TR_PointLedgerEntries_BlockMutation`)** |
| **6. Notifications (1)** | `Notifications` | Nullable `TargetDealerId` | None | `IsRead` / `ReadAtUtc` state tracking (`10` canonical `EventType`s) |

---

## 3. Non-Negotiable Physical Schema & EF Core Conventions

1. **Sequential Clustered UUID Keys**:
   - Surrogate PK tables (`25` of `27`; `UserRoles` & `RolePermissions` use composite PKs): `builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");` while domain constructors initialize `Id = Guid.CreateVersion7()`.
2. **Exact Monetary & Temporal Types**:
   - Money (VND): `DECIMAL(18,2) NOT NULL` (`ConfigureConventions` + explicit `HasPrecision(18, 2)`). Tier multiplier: `HasPrecision(5, 2)`. Coordinates: `HasPrecision(9, 6)`. **Never** `float` or `double`.
   - Timestamps: `DATETIMEOFFSET(7)` in UTC (`SYSUTCDATETIME()`), suffixed `Utc`. Workshop schedules use `DateOnly` (`date`) and `TimeOnly` (`time(0)`).
   - Strings: `.IsUnicode(false)` (`VARCHAR`) for codes, SKUs, emails, hashes, statuses, and ISO 3779 `VIN` (`VARCHAR(17)` + `CK_*_VIN: LEN(VIN) = 17 AND VIN NOT LIKE '%[^A-HJ-NPR-Z0-9]%'`). `.IsUnicode(true)` (`NVARCHAR`) for Vietnamese names, descriptions, addresses, notes, and JSON snapshots (`ISJSON(...) = 1`).
3. **8 `RowVersion` Concurrency Tokens (`IConcurrencyProtected`)**:
   - Configure `.IsRowVersion()` on `DealerInventories`, `ServiceTimeSlots`, `ServiceBookings`, `LoyaltyAccounts`, `CustomerVouchers`, `TestDriveBookings`, `ServiceReviews`, and `WarrantyClaims`.
4. **4 Immutable Financial/Audit Tables (`IAppendOnlyEntity` & SQL Triggers)**:
   - `PointLedgerEntries`, `SalesOrders`, `ServiceInvoices`, and `ServiceInvoiceLineItems` MUST NEVER be updated or deleted (`AppendOnlyLedgerInterceptor` + SQL triggers). Every `ServiceInvoices` row reconciles `GrossAmount == LaborAmount + PartsAmount == SUM(ServiceInvoiceLineItems.LineTotalVnd)`.
5. **Foreign Key `ON DELETE` Policy (Zero SQL Server `Msg 1785` Cycles)**:
   - Set `.OnDelete(DeleteBehavior.Restrict)` (`ON DELETE NO ACTION`) on every domain aggregate relationship (`Dealers`, `Users`, `MotorcycleModels`, `CustomerVouchers`, `SpareParts`, `ServiceInvoices` -> `ServiceInvoiceLineItems`).
   - Allow `.OnDelete(DeleteBehavior.Cascade)` **only** on single-parent leaf tables: `UserRoles`, `RolePermissions`, `RefreshTokens`, and `Notifications.RecipientUserId`.
6. **`8` `IDealerScoped` & `6` `ISoftDeletable` Global Query Filters (`MotorcycleDealerDbContext`)**:
   - EF Core applies **one** `HasQueryFilter` per entity type — combine predicates into a single lambda whenever an entity implements both interfaces!
   - `8` `IDealerScoped` entities (`DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, `WarrantyClaims`) predicate `!IsDealerStaffScopeActive || e.DealerId == CurrentDealerIdOrEmpty`.
   - `6` `ISoftDeletable` entities (`Users`, `Dealers`, `MotorcycleModels`, `CustomerVehicles`, `VoucherCatalogs`, `SpareParts`) predicate `!e.IsDeleted`.

---

## 4. Workload-Tailored Non-Clustered B-Tree Indexes

Configure all non-clustered composite, covering (`IncludeProperties`), and filtered (`HasFilter`) indexes specified in [docs/DATABASE_DESIGN.md](../../../docs/DATABASE_DESIGN.md), including:

- **Identity, Showroom & Test-Drives**: `IX_Users_DealerId_Active`, `IX_RefreshTokens_UserId_Active`, `IX_MotorcycleModels_Category_Active`, `IX_DealerInventories_Dealer_Status_Model`, `IX_TestDriveBookings_Dealer_Schedule`, `IX_TestDriveBookings_Customer_Schedule`, `UQ_TestDriveBookings_Number`.
- **Sales & Analytics Covering**: `IX_SalesOrders_Customer_CreatedDesc`, `IX_SalesOrders_Dealer_CreatedDesc`, `IX_SalesOrders_Analytics_Covering`.
- **Garage & VIN Worker Scan**: `IX_CustomerVehicles_Customer_Active`, `IX_CustomerVehicles_ReminderScan`, `IX_MaintenanceReminders_PendingDispatch`, `IX_MaintenanceReminders_Vehicle_History`.
- **Slots, Bookings, Spare Parts & Itemized Invoices**: `IX_ServiceTimeSlots_Dealer_Date_Open`, `IX_ServiceBookings_Dealer_Status_Created`, `IX_ServiceBookings_Slot_Active`, `IX_ServiceBookings_Customer_CreatedDesc`, `IX_ServiceBookings_Analytics_Dealer`, `UQ_SpareParts_PartCode`, `IX_SpareParts_Category_Active`, `IX_ServiceInvoices_Customer_PaidDesc`, `IX_ServiceInvoices_Analytics_Covering`, `IX_ServiceInvoiceLineItems_InvoiceId`.
- **Loyalty & Unread Notifications**: `IX_LoyaltyAccounts_Tier_Distribution`, `IX_PointLedgerEntries_Account_CreatedDesc`, `IX_CustomerVouchers_Account_Usable`, `IX_Notifications_Recipient_Unread`, `IX_Notifications_Dealer_Unread`.

---

## 5. EF Core 10 CLI & Idempotent 3-Dealer Seeding (`DatabaseSeeder`)

### 5.1 Migration Commands (Run from Repo Root)
```powershell
dotnet ef database update --project backend/src/MotorcycleDealer.Infrastructure --startup-project backend/src/MotorcycleDealer.WebApi
```

### 5.2 Idempotent `DatabaseSeeder` Blueprint
Key every seed lookup by natural unique business codes inside an explicit transaction:
- **3 Regional Dealers**: `DLR-HN-01` (*Hanoi*), `DLR-DN-01` (*Da Nang*), `DLR-HCM-01` (*Ho Chi Minh City*).
- **RBAC & Loyalty**: `3` `Roles` (`Customer`, `DealerStaff`, `Admin`) + all **`48` `Permissions`** (`Customer=20`, `DealerStaff=22`, `Admin=32`) + `RolePermissions` + `4` `LoyaltyTiers` (`Bronze` `0`/`1.00`, `Silver` `500`/`1.20`, `Gold` `2000`/`1.50`, `Platinum` `5000`/`2.00`).
- **8 Demo Accounts**: `admin@motorcycledealer.vn`, `staff.{hn,dn,hcm}@motorcycledealer.vn`, and `customer.{bronze,silver,gold,platinum}@motorcycledealer.vn`.
- **Catalog, VINs, Spare Parts, Test-Drives & Itemized Invoices**: `6` `MotorcycleModels` + `18` `MaintenanceRules` + `18` ISO 3779 `DealerInventories` `VIN`s + `6` `CustomerVehicles` + `12` OEM `SpareParts` + `TestDriveBookings` + rolling `ServiceTimeSlots` + `SalesOrders`/`ServiceBookings`/`ServiceInvoices`/`ServiceInvoiceLineItems`/`PointLedgerEntries` history.

---

## 6. Verification & Mandatory Post-Task Sync Steps

1. **Provision / Verify SQL Server LocalDB & Migrations**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1`
2. **Verify Guardrails & Zero Immutable-Table Violations**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/audit-dealer-guardrails.ps1`
3. **Run Read-Only SQL Invariant Check (`sqlserver-localdb-inspector` MCP)**:
   - Confirm `is_read_committed_snapshot_on = 1` and `TableCount = 27` (`sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory'`).
4. **End-to-End Smoke & Post-Task Sync Verification**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1`
   - Command: `node .agents/scripts/verify-post-task-sync.mjs`
