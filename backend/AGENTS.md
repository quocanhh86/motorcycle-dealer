# Backend Engineering Rules — MotorcycleDealer (.NET 10 Clean Architecture)

This rulebook governs all C# projects inside `backend/` (`MotorcycleDealer.sln`) for the **Motorcycle Dealer & Loyalty Management Platform**, complementing the root [AGENTS.md](../AGENTS.md) constitution.

## 1. Solution Topology & Inward Dependency Law (`ADR-001`)

All projects inherit `backend/Directory.Build.props` (`net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`). Dependencies MUST point strictly inward:

| Project | Allowed Project References | Allowed External NuGet Scope | Prohibited Dependencies |
| :--- | :--- | :--- | :--- |
| `src/MotorcycleDealer.Domain` | **NONE** | **NONE** (100% pure BCL C#) | `Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.*`, `FluentValidation`, JSON/HTTP/SQL transport libraries |
| `src/MotorcycleDealer.Application` | `Domain` **ONLY** | `FluentValidation.DependencyInjectionExtensions`, `Microsoft.Extensions.DependencyInjection.Abstractions` | Concrete `DbContext`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.*`, `IHubContext` |
| `src/MotorcycleDealer.Infrastructure` | `Application`, `Domain` | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Options.ConfigurationExtensions` | `MotorcycleDealer.WebApi`, MVC controllers |
| `src/MotorcycleDealer.WebApi` | `Application`, `Infrastructure` | ASP.NET Core 10, `JwtBearer`, `SignalR`, `OpenApi` | Injecting `MotorcycleDealerDbContext` into controllers or returning `BaseEntity` types directly |

## 2. Multi-Dealer Isolation (`8` Entities) & `48`-Permission RBAC (`ADR-002`, `FR-01`)

1. **Three Canonical Roles (`48` Permissions)**: `Customer` (`20`), `DealerStaff` (`22`), `Admin` (`32`) defined in `MotorcycleDealer.Domain/Constants/Permissions.cs`.
2. **`8` `IDealerScoped` Aggregates**: `DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, and `WarrantyClaims` implement `IDealerScoped` (`Guid DealerId { get; }`). `MotorcycleDealerDbContext` enforces a Global Query Filter (`e.DealerId == CurrentDealerId`) whenever the caller holds `DealerStaff`, plus write-side verification in `SaveChangesAsync`.
3. **Granular Policy Enforcement**: Protect `/api/v1/*` actions using `[HasPermission(Permissions.<Module>.<Action>)]` (or `[Authorize(Policy = ...)]`) resolved dynamically by `PermissionPolicyProvider` and `PermissionAuthorizationHandler` across all **`48` canonical permissions** (`TestDrives.*`, `SpareParts.ManageCatalog`, `Reviews.*`, `Warranty.*`).
4. **Separation of Duties**: Customer self-service policies (`*.ViewOwn`, `*.RegisterOwn`, `*.CreateOwn`, `Bookings.Create`, `Bookings.CancelOwn`, `TestDrives.Create`, `TestDrives.CancelOwn`, `Loyalty.RedeemVoucher`) belong exclusively to `Customer` accounts.

## 3. 27-Table Schema, 4 Immutable Tables & 8 `RowVersion` Invariants (`ADR-003`, `ADR-005`)

- **Schema & Keys**: **`27` normalized 3NF tables** in `MotorcycleDealerDb` (`+TestDriveBookings`, `+SpareParts`, `+ServiceInvoiceLineItems`, `+ServiceReviews`, `+WarrantyClaims`) using `Guid.CreateVersion7()` in `BaseEntity` paired with SQL Server `NEWSEQUENTIALID()`.
- **Monetary & Temporal Precision**: Strictly `decimal` mapped to `DECIMAL(18,2)` (`ConfigureConventions`; never `float`/`double`) and UTC `DateTimeOffset` (`*Utc` suffix) stamped via `IDateTimeProvider` and `AuditableEntityInterceptor`.
- **ISO 3779 VIN**: Validated via `Vin` value object (`17` uppercase alphanumeric characters excluding `I`, `O`, and `Q`: `^[A-HJ-NPR-Z0-9]{17}$`).
- **`4` Immutable Financial/Audit Tables**: `PointLedgerEntries` (append-only via `IAppendOnlyEntity` & `AppendOnlyLedgerInterceptor`), `SalesOrders`, `ServiceInvoices`, and `ServiceInvoiceLineItems` (`IAppendOnlyEntity`, `GrossAmount == SUM(LineTotalVnd)`) MUST NEVER be updated or deleted. Loyalty accrual follows `(int)Math.Floor(Math.Floor(netPaidAmount / 10_000m) * tierMultiplier)` across `Bronze` (`0+`, `1.00x`), `Silver` (`500+`, `1.20x`), `Gold` (`2,000+`, `1.50x`), and `Platinum` (`5,000+`, `2.00x`); `ServiceReviewBonus` (`+25`) increments `AvailablePoints` ONLY.
- **`8` `RowVersion` Concurrency Tables**: `IConcurrencyProtected` (`byte[] RowVersion`) guards `DealerInventories`, `ServiceTimeSlots`, `ServiceBookings`, `LoyaltyAccounts`, `CustomerVouchers`, `TestDriveBookings`, `ServiceReviews`, and `WarrantyClaims`; `DbUpdateConcurrencyException` maps to `409 Conflict`.

## 4. Transport, 10-Event SignalR Hub & Security Hygiene (`ADR-004`, `ADR-006`)

- **RFC 7807 ProblemDetails**: All `4xx`/`5xx` HTTP responses emit `application/problem+json` with `type`, `title`, `status`, `detail`, `instance`, `errorCode`, `traceId`, and `errors` (never expose stack traces or raw SQL errors outside `Development`).
- **Zero-Trust SignalR Hub (`10` Events)**: `NotificationHub : Hub<INotificationClient>` at `/hubs/notifications` extracts `access_token` solely for `/hubs/notifications`, assigns `user-{userId}`, `dealer-{dealerId}`, and `role-Admin` strictly from verified JWT claims on `OnConnectedAsync`, and dispatches **`10` events**: `BookingCreated`, `BookingStatusChanged`, `MaintenanceReminderTriggered`, `LoyaltyPointsUpdated`, `TierUpgraded`, `TestDriveCreated`, `TestDriveStatusChanged`, `ServiceReviewSubmitted`, `WarrantyClaimCreated`, and `WarrantyClaimStatusChanged`.
- **Zero Secrets**: Keep `appsettings*.json` free of real passwords or production JWT keys; load secrets via `dotnet user-secrets` or environment variables (`Jwt__SigningKey`, `ConnectionStrings__DefaultConnection`).

## 5. 7-Stage Quality Gate (`Phases 3–9+`) & Mandatory Post-Task Sync

Every backend change in **Phases 3–9+** MUST pass **`5x Review -> 3x Test -> Fix -> 5x Review -> Refactor -> 5x Review -> 3x Test`** (`15` adversarial reviews + `6` test batteries) and the **Mandatory Post-Task Synchronization Gate** before completion:

1. **Stages 1–2 (`5R -> 3T`)**: Audit (`1`) `ADR-001` inward purity, (`2`) OpenAPI/SSOT/`48`-permission & loyalty math parity, (`3`) `8`-table `IDealerScoped` isolation, JWT/SignalR claim routing, CORS & `RFC 7807`, (`4`) `Guid.CreateVersion7()`, `DECIMAL(18,2)`, `8`-table `RowVersion` & `4`-table immutability, and (`5`) `TreatWarningsAsErrors=true`, non-root Docker & zero secrets; then run `run-phase-quality-loop.ps1 -Phase <N> -Round 1`.
2. **Stages 3–5 (`Fix -> 5R -> Refactor`)**: Fix all defects/warnings, re-review `5x` (`R2.1`..`R2.5`), seal internal types, deduplicate handlers, and expand boundary `[Theory]` cases without breaking `/api/v1/*` or `/hubs/notifications` contracts.
3. **Stages 6–7 (`5R -> 3T`)**: Complete final `5x` review (`R3.1`..`R3.5`), pass `run-phase-quality-loop.ps1 -Phase <N> -Round 3` (`0` warnings, `0` flaky tests), and log the `15R + 6T` ledger in `PROGRESS.md`.
4. **Mandatory Post-Task Synchronization Gate**: Update all affected `docs/` (`PRD`, `ARCHITECTURE`, `DATABASE_DESIGN`, `DESIGN`, `openapi.v1.yaml`, `TEST_PLAN`, `RUNBOOK`, `modules/`, `screens/`, `basic-design/`, `detail-design/`, `RELEASE_NOTES`), regenerate/validate `docs/Motorcycle_Dealer_Loyalty_Platform_Specification.docx`, sync `TASKS.md`/`PROGRESS.md`/`README.md`/`baseline-spec-snapshot.json` + `.agents/{hooks,skills,scripts}`, map `100%` of git-trackable files into `.agents/scripts/git-6month-schedule-plan.json`, and pass `node .agents/scripts/verify-post-task-sync.mjs`.

```powershell
dotnet clean backend/MotorcycleDealer.sln -c Release && dotnet build backend/MotorcycleDealer.sln -c Release --warnaserror && dotnet test backend/MotorcycleDealer.sln -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/run-phase-quality-loop.ps1 -Phase <N> -Round <1|3>
node .agents/scripts/verify-post-task-sync.mjs
```
