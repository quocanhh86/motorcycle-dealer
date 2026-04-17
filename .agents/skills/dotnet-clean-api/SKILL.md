---
name: dotnet-clean-api
description: >-
  Use this skill when building, modifying, or reviewing ASP.NET Core 10 Web API endpoints, Clean Architecture layers (MotorcycleDealer.Domain, Application, Infrastructure, WebApi), CQRS commands/queries, FluentValidation validators, JWT Access and rotating RefreshTokens authentication, dynamic 48-permission RBAC policies (PermissionPolicyProvider and PermissionAuthorizationHandler), 8-table IDealerScoped multi-dealer isolation, or RFC 7807 ProblemDetails exception handling. Also activate on Vietnamese developer intents such as "tạo API mới /api/v1", "viết command/service tầng Application", "kiểm tra dữ liệu FluentValidation", "xác thực JWT / xoay vòng RefreshToken", "phân quyền RBAC 48 quyền Permissions", "cô lập dữ liệu đại lý IDealerScoped", or "xử lý lỗi chuẩn RFC 7807 ProblemDetails".
---

# ASP.NET Core 10 Clean Architecture & 48-Permission RBAC API Skill (`dotnet-clean-api`)

Authoritative implementation runbook for the `.NET 10` (`net10.0`) backend of the **Motorcycle Dealer & Loyalty Management Platform** (`backend/MotorcycleDealer.sln`), governed by [AGENTS.md](../../../AGENTS.md), [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) (`ADR-001`, `ADR-002`), and [docs/DESIGN.md](../../../docs/DESIGN.md) (`FR-01`..`FR-09`).

---

## 1. Clean Architecture 4-Layer Dependency Contract (`ADR-001`)

All projects under `backend/` inherit `Directory.Build.props` (`TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`). Dependencies point **strictly inward**:

| Project Path | Allowed Project References | Responsibilities | Strictly Prohibited |
| :--- | :--- | :--- | :--- |
| `backend/src/MotorcycleDealer.Domain` | **NONE** (Zero project & zero framework NuGet refs) | `27` domain entities/aggregates, `Vin` & `Money` value objects, `LoyaltyTierCalculator`, `MaintenanceDueEvaluator`, `48` `Permissions` constants, enums, domain exceptions, `BaseEntity`, `IAuditableEntity`, `ISoftDeletable`, `IDealerScoped`, `IConcurrencyProtected`, `IAppendOnlyEntity` | `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, HTTP status codes, SQL attributes, JSON transport annotations |
| `backend/src/MotorcycleDealer.Application` | `Domain` **ONLY** | Vertical feature slices (`Features/{Auth,Dealers,Showroom,TestDrives,Vehicles,Warranty,Bookings,SpareParts,Invoices,Reviews,Loyalty,Notifications,Analytics}`), CQRS commands/queries & handlers, C# `record` DTOs, `FluentValidation` validators, port interfaces (`IApplicationDbContext`, `ICurrentUserContext`, `INotificationPublisher`) | `DbContext` concrete types, `IHubContext`, `HttpContext`, raw SQL strings |
| `backend/src/MotorcycleDealer.Infrastructure` | `Application`, `Domain` | `MotorcycleDealerDbContext`, `27` `IEntityTypeConfiguration<T>` classes, SQL Server migrations, `JwtTokenGenerator`, `RefreshTokenService`, `PasswordHasher<User>`, `SignalRNotificationPublisher`, `VinMaintenanceReminderWorker` | HTTP controllers, exposing EF tracking quirks outside port interfaces |
| `backend/src/MotorcycleDealer.WebApi` | `Application`, `Infrastructure` | Thin `/api/v1/*` controllers, `NotificationHub` (`/hubs/notifications`), `PermissionPolicyProvider`, `PermissionAuthorizationHandler`, `GlobalExceptionHandler` (`RFC 7807`), CORS, RateLimiting, `/health` & `/api/v1/health` | Injecting `MotorcycleDealerDbContext` into controllers, returning EF Core entities in HTTP responses, business calculations in controllers |

---

## 2. MCP-Accelerated Workflow (Run Before & After Coding)

Always combine workspace Stdio MCP servers and global MCP servers in this exact sequence:

1. **Phase Gate & RBAC Pre-Flight (`motorcycle-dealer-guardrails` MCP)**:
   - Call `check_phase_gate` before creating or editing any file under `backend/` (application code is gated until **Phase 3+** is unlocked in [PROGRESS.md](../../../PROGRESS.md)).
   - Call `lookup_rbac_permission` (filtering by `module`, `role`, or `permission`) to verify the exact `Permissions.<Module>.<Action>` constant across all `40` policies, allowed roles (`Customer`, `DealerStaff`, `Admin`), and `6`-table `IDealerScoped` enforcement rules before wiring `[HasPermission(...)]`.
2. **Up-to-Date `.NET 10` & Library Reference (`context7_context7` MCP)**:
   - Call `resolve-library-id` -> `query-docs` (passing `language: "C#"`) for **ASP.NET Core 10** (`IExceptionHandler`, `AddProblemDetails`, `RateLimiter`, `IAuthorizationPolicyProvider`, `JwtBearerHandler`) and **FluentValidation** to ensure modern `net10.0` idioms.
3. **REST API Contract & Role Environment Sync (`postman_postman` MCP)**:
   - Whenever adding or updating `/api/v1/*` routes in `MotorcycleDealer.WebApi`, sync the Postman collection and role environments (`Customer`, `DealerStaff-Hanoi`, `DealerStaff-HCMC`, `Admin`) with `RFC 7807` status assertions.

---

## 3. Authentication, Rotating Refresh Tokens & 40-Permission RBAC (`FR-01`, `ADR-002`)

### 3.1 JWT Access Token & Token-Family Refresh Rotation (`ADR-006`)
- **Signing Secret**: Load `Jwt:SigningKey` (`>= 256-bit` / 32+ UTF-8 bytes) strictly from `dotnet user-secrets` or `Jwt__SigningKey` environment variable — **never** commit real keys to `appsettings.json`.
- **Access Token (`15 min` TTL)**: `HMAC-SHA256` carrying `sub` (`UserId`), `email`, `full_name`, `role` (`Customer` | `DealerStaff` | `Admin`), `dealer_id` (mandatory non-null GUID when `role == "DealerStaff"`), `permissions` string array, and `jti`.
- **Refresh Token (`7 days` TTL, `dbo.RefreshTokens`)**:
  - Generate 64-byte `RandomNumberGenerator.GetBytes(64)` opaque token; persist **only** its `SHA-256` hex digest (`TokenHash`) with `TokenFamilyId`.
  - On `POST /api/v1/auth/refresh`: if `RevokedAtUtc != null`, treat as **token replay attack** — immediately revoke every active token in `TokenFamilyId` (`RevocationReason = "ReuseDetected"`) and return `401 INVALID_REFRESH_TOKEN`. Otherwise mark current token `Rotated`, set `ReplacedByTokenHash`, and mint a new pair.

### 3.2 Complete 48-Permission Canonical Matrix (`Customer=20`, `DealerStaff=22`, `Admin=32`)
Defined in `MotorcycleDealer.Domain/Constants/Permissions.cs` and mirrored in `frontend/src/app/core/auth/permissions.constants.js`:

| Module | Canonical Permission Constant(s) (`48` Total) | Granted Roles (`Customer` / `DealerStaff` / `Admin`) |
| :--- | :--- | :--- |
| **Showroom & Sales (7)** | `Permissions.Showroom.View` *(All + Public GET)* · `Permissions.Showroom.ManageModels` *(Admin)* · `Permissions.Inventories.ViewDealer` *(Staff, Admin)* · `Permissions.Inventories.ManageDealer` *(Staff, Admin)* · `Permissions.SalesOrders.Create` *(Staff, Admin)* · `Permissions.SalesOrders.ViewOwn` *(Customer)* · `Permissions.SalesOrders.ViewDealer` *(Staff, Admin)* | Enforces strict separation between `ViewOwn` (`Customer`) and `ViewDealer` (`DealerStaff`/`Admin`) |
| **Test-Drives (5)** | `Permissions.TestDrives.Create` *(Customer)* · `Permissions.TestDrives.CancelOwn` *(Customer)* · `Permissions.TestDrives.ViewOwn` *(Customer)* · `Permissions.TestDrives.ViewDealer` *(Staff, Admin)* · `Permissions.TestDrives.UpdateStatus` *(Staff, Admin)* | `TestDrives.{Create,CancelOwn,ViewOwn}` belong exclusively to `Customer`; `ViewDealer`/`UpdateStatus` are dealer-scoped for `DealerStaff` |
| **Vehicles, Rules & Warranty (8)** | `Permissions.Vehicles.ViewOwn` *(Customer)* · `Permissions.Vehicles.RegisterOwn` *(Customer)* · `Permissions.Vehicles.ViewDealer` *(Staff, Admin)* · `Permissions.Vehicles.ManageRules` *(Admin)* · `Permissions.Warranty.CreateOwn` *(Customer)* · `Permissions.Warranty.ViewOwn` *(Customer)* · `Permissions.Warranty.ViewDealer` *(Staff, Admin)* · `Permissions.Warranty.ManageDealer` *(Staff, Admin)* | `Customer` self-service (`ViewOwn`, `RegisterOwn`, `Warranty.CreateOwn`) is **never** granted to `DealerStaff` or `Admin` |
| **TimeSlots & Bookings (7)** | `Permissions.TimeSlots.ViewAvailable` *(Customer, Staff, Admin)* · `Permissions.TimeSlots.ManageDealer` *(Staff, Admin)* · `Permissions.Bookings.Create` *(Customer)* · `Permissions.Bookings.CancelOwn` *(Customer)* · `Permissions.Bookings.ViewOwn` *(Customer)* · `Permissions.Bookings.ViewDealer` *(Staff, Admin)* · `Permissions.Bookings.UpdateStatus` *(Staff, Admin)* | `Bookings.Create` and `Bookings.CancelOwn` belong exclusively to `Customer` |
| **Spare Parts, Invoices & CSAT Reviews (8)** | `Permissions.SpareParts.ManageCatalog` *(Admin)* · `Permissions.Invoices.CreateDealer` *(Staff, Admin)* · `Permissions.Invoices.ViewOwn` *(Customer)* · `Permissions.Invoices.ViewDealer` *(Staff, Admin)* · `Permissions.Reviews.CreateOwn` *(Customer)* · `Permissions.Reviews.ViewOwn` *(Customer)* · `Permissions.Reviews.ViewDealer` *(Staff, Admin)* · `Permissions.Reviews.RespondDealer` *(Staff, Admin)* | OEM `SpareParts` managed by `Admin`; `Reviews.CreateOwn` (`+25` `AvailablePoints` bonus) is `Customer`-exclusive; `RespondDealer` is `IDealerScoped` |
| **Loyalty & Vouchers (6)** | `Permissions.Loyalty.ViewOwn` *(Customer)* · `Permissions.Loyalty.RedeemVoucher` *(Customer)* · `Permissions.Loyalty.ViewDealerCustomer` *(Staff, Admin)* · `Permissions.Loyalty.ManageTiers` *(Admin)* · `Permissions.Loyalty.ManageVoucherCatalog` *(Admin)* · `Permissions.Loyalty.AdjustPoints` *(Admin)* | `Loyalty.RedeemVoucher` is `Customer`-only; `Loyalty.AdjustPoints` is `Admin`-only |
| **Notifications (2)** | `Permissions.Notifications.ViewOwn` *(Customer, Staff, Admin)* · `Permissions.Notifications.ViewDealer` *(Staff, Admin)* | Personal inbox vs. `TargetDealerId` workshop/test-drive feed |
| **Dealers, Staff & Analytics (5)** | `Permissions.Dealers.ViewPublic` *(All + Public GET)* · `Permissions.Dealers.ManageAll` *(Admin)* · `Permissions.Users.ManageStaff` *(Admin)* · `Permissions.Analytics.ViewAllDealers` *(Admin)* · `Permissions.Analytics.ExportReports` *(Admin)* | Staff provisioning lives at `/api/v1/dealers/{dealerId}/staff` (`DealersController`) |

### 3.3 Dynamic Policy Provider & Handler Pattern
```csharp
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) =>
        policyName.StartsWith("Permissions.", StringComparison.Ordinal)
            ? new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build()
            : await base.GetPolicyAsync(policyName);
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permissions", requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

---

## 4. Multi-Dealer Tenant Isolation (`8` `IDealerScoped` Aggregates) & Ownership Rules

Every handler operating on dealer-scoped or customer-owned aggregates MUST enforce defense-in-depth:

1. **8 `IDealerScoped` Aggregates**: `DealerInventories`, `SalesOrders`, `ServiceTimeSlots`, `ServiceBookings`, `ServiceInvoices`, `TestDriveBookings`, `ServiceReviews`, and `WarrantyClaims`.
2. **Never Trust Client `dealerId` for `DealerStaff`**:
   - On **reads**: EF Core Global Query Filter automatically predicates `e.DealerId == currentUser.DealerId` when `currentUser.Role == "DealerStaff"`.
   - On **writes**: If `currentUser.Role == "DealerStaff"` and a request DTO supplies `dealerId != null && dealerId != currentUser.DealerId`, throw `ForbiddenDealerScopeException` (`403 FORBIDDEN_DEALER_SCOPE`). Always assign `DealerId = currentUser.DealerId!.Value` from `ICurrentUserContext`.
3. **Never Trust Client `customerId` for `Customer`**:
   - Endpoints guarded by `*.ViewOwn`, `*.RegisterOwn`, `*.CreateOwn`, `Bookings.Create`, `Bookings.CancelOwn`, `TestDrives.Create`, `TestDrives.CancelOwn`, or `Loyalty.RedeemVoucher` MUST bind `CustomerId = currentUser.UserId` from the verified JWT `sub` claim. Accessing another customer's vehicle, booking, test-drive, invoice, review, warranty claim, receipt, or voucher throws `ForbiddenResourceOwnerException` (`403 FORBIDDEN_RESOURCE_OWNER`).

---

## 5. REST `/api/v1/*` Envelopes & `RFC 7807 ProblemDetails` Mapping

### 5.1 Canonical 17 Domain Controller Route Prefixes (+ Health)
`/api/v1/auth` · `/api/v1/dealers` · `/api/v1/motorcycles` · `/api/v1/inventories` · `/api/v1/sales-orders` *(including `/{id}/receipt`)* · `/api/v1/test-drives` · `/api/v1/vehicles` · `/api/v1/warranty-claims` · `/api/v1/time-slots` · `/api/v1/bookings` · `/api/v1/spare-parts` · `/api/v1/service-invoices` *(including `/{id}/receipt`)* · `/api/v1/service-reviews` · `/api/v1/loyalty` · `/api/v1/vouchers` · `/api/v1/notifications` · `/api/v1/admin/analytics` · `/api/v1/health` (`/health`).

- **Serialization**: `camelCase` JSON keys, exact `PascalCase` enum strings (`"Pending"`, `"Requested"`, `"ReservedOnBooking"`, `"Platinum"`), `decimal` money (`DECIMAL(18,2)`), ISO-8601 UTC (`DateTimeOffset.UtcNow`), Base64 `rowVersion` strings (`byte[]`).
- **Success Envelopes**: Wrap single DTOs in `ApiResponse<T>(true, data, meta)` and collections in `PagedResponse<T>(true, data, pagination, meta)` with `traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier`.

### 5.2 `GlobalExceptionHandler : IExceptionHandler` (`application/problem+json`)
Map every failure deterministically without leaking SQL or stack traces outside `Development`:

| Exception Type | Status | Canonical `errorCode` |
| :--- | :---: | :--- |
| `ValidationException` (`FluentValidation`) | `400` | `VALIDATION_FAILED` (with field `errors` dictionary) |
| `UnauthorizedException` / Invalid Refresh Token | `401` | `UNAUTHORIZED` · `TOKEN_EXPIRED` · `INVALID_REFRESH_TOKEN` |
| `ForbiddenDealerScopeException` / Missing Policy / IDOR | `403` | `FORBIDDEN_PERMISSION` · `FORBIDDEN_DEALER_SCOPE` · `FORBIDDEN_RESOURCE_OWNER` |
| `EntityNotFoundException` | `404` | `RESOURCE_NOT_FOUND` |
| `DbUpdateConcurrencyException` / Overlap / Unique Key | `409` | `CONCURRENCY_CONFLICT` · `SLOT_CAPACITY_EXHAUSTED` · `TEST_DRIVE_CONFLICT` · `DUPLICATE_VIN` · `INVOICE_ALREADY_ISSUED` |
| `BusinessRuleViolationException` | `422` | `INSUFFICIENT_LOYALTY_POINTS` · `VOUCHER_INELIGIBLE` · `VOUCHER_QUOTA_EXHAUSTED` · `INVALID_STATUS_TRANSITION` · `ODOMETER_REGRESSION` |
| Rate Limiter Rejection | `429` | `RATE_LIMIT_EXCEEDED` |
| Unhandled Exception | `500` | `INTERNAL_SERVER_ERROR` |

---

## 6. Verification & Mandatory Post-Task Sync Checklist

Run these checks before marking any backend API task (`T-301`..`T-808` in [TASKS.md](../../../TASKS.md)) complete:

1. **Architecture, Phase-Gate & Secret Guardrails**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/audit-dealer-guardrails.ps1`
2. **SQL Server LocalDB Readiness**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/ensure-sqlserver-db.ps1`
3. **Zero-Warning Build & Automated Tests (Phases 3–8+)**:
   - Command: `dotnet build backend/MotorcycleDealer.sln -c Release --warnaserror && dotnet test backend/MotorcycleDealer.sln -c Release --no-build`
4. **Full-Stack HTTP, RBAC & SignalR Smoke Suite**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1`
5. **Mandatory Post-Task Docs, Snapshot & Git Schedule Sync Gate**:
   - Update all affected `docs/`, `.docx`, trackers, `.agents/`, and `git-6month-schedule-plan.json`, then run: `node .agents/scripts/verify-post-task-sync.mjs`
