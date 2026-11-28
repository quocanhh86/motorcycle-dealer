---
name: signalr-realtime-hub
description: >-
  Use this skill when implementing, debugging, or testing ASP.NET Core SignalR real-time notifications (NotificationHub : Hub<INotificationClient> at /hubs/notifications), WebSocket JWT access_token query-string authentication, claim-driven server group routing (dealer-{dealerId}, user-{userId}, role-Admin), persistent Notifications table outbox-first dispatch across all 10 events (BookingCreated, BookingStatusChanged, MaintenanceReminderTriggered, LoyaltyPointsUpdated, TierUpgraded, TestDriveCreated, TestDriveStatusChanged, ServiceReviewSubmitted, WarrantyClaimCreated, WarrantyClaimStatusChanged), or AngularJS 1.8 @microsoft/signalr integration with withAutomaticReconnect, $rootScope.$applyAsync(), and $onDestroy listener cleanup. Also activate on Vietnamese developer intents such as "thông báo đặt lịch / lái thử / đánh giá / bảo hành realtime cho nhân viên đại lý không cần F5", "cấu hình SignalR /hubs/notifications phân nhóm theo đại lý", "kết nối WebSocket JWT", or "tích hợp @microsoft/signalr vào AngularJS không lỗi $digest".
---

# SignalR Real-Time Notification Hub (`10` Events) & Zero-Refresh AngularJS Bridge Skill (`signalr-realtime-hub`)

Authoritative implementation and live verification runbook for real-time push notifications (`FR-06`, `ADR-004`, `ADR-006`, `RISK-04`) across `MotorcycleDealer.WebApi`, `MotorcycleDealer.Infrastructure`, and `frontend/src/app/core/realtime/`, governed by [AGENTS.md](../../../AGENTS.md), [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md), and [docs/DESIGN.md](../../../docs/DESIGN.md).

---

## 1. Strongly-Typed Hub & Zero-Trust Group Routing (`ADR-004`)

### 1.1 WebSocket JWT Handshake (`JwtBearerEvents.OnMessageReceived`)
Browser `WebSocket` upgrades cannot attach custom `Authorization` HTTP headers. Configure `Program.cs` to extract `access_token` **exclusively** for `/hubs/notifications` while requiring explicit CORS origins (`AllowCredentials()` with `Cors:AllowedOrigins` — **never** `AllowAnyOrigin()`):

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

### 1.2 Claim-Driven `NotificationHub : Hub<INotificationClient>` (`/hubs/notifications`)
> [!IMPORTANT]
> **Zero Client Group Trust**: NEVER expose a hub method like `JoinDealerGroup(string dealerId)` or trust client-passed group names. Group membership MUST be derived 100% server-side in `OnConnectedAsync` from verified JWT claims (`sub`, `role`, `dealer_id`) so `DealerStaff` at Hanoi (`DLR-HN-01`) can never eavesdrop on Saigon (`DLR-HCM-01`) or Da Nang (`DLR-DN-01`) workshop bookings or showroom test-drives.

```csharp
[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirstValue("sub");
        var role = Context.User?.FindFirstValue(ClaimTypes.Role)
                   ?? Context.User?.FindFirstValue("role");
        var dealerId = Context.User?.FindFirstValue("dealer_id");

        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        if (string.Equals(role, "DealerStaff", StringComparison.Ordinal) &&
            Guid.TryParse(dealerId, out var parsedDealerId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dealer-{parsedDealerId:D}");
        }
        else if (string.Equals(role, "Admin", StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "role-Admin");
        }

        await base.OnConnectedAsync();
    }
}
```

---

## 2. Canonical `10` `INotificationClient` Events & Persistent `Notifications` Backing

Every real-time notification MUST follow **Persist-Commit-Then-Broadcast**:
1. Insert a `dbo.Notifications` row (`RecipientUserId`, `TargetDealerId`, `TargetRole`, `EventType`, `Title`, `Message`, `PayloadJson` with `ISJSON = 1`, `IsRead = 0`) inside the active EF Core transaction so offline/reconnecting clients never lose alerts.
2. Commit the SQL Server transaction **first** (`await transaction.CommitAsync(ct)`).
3. Push via `SignalRNotificationPublisher` (`IHubContext<NotificationHub, INotificationClient>`) inside a non-throwing `try/catch` log wrapper so committed DB work is never aborted by transient socket disconnects.

| `INotificationClient` Method (`10` Total) | Target SignalR Group(s) | Triggering Use Case | AngularJS Zero-Refresh UI Effect |
| :--- | :--- | :--- | :--- |
| `BookingCreated(payload)` | `dealer-{dealerId}`, `role-Admin` | Customer submits `POST /api/v1/bookings` | **Instant `DealerStaff` Workbench (`app.staff.workbench`) card prepend** + live toast & bell counter without page reload (`F5`) |
| `BookingStatusChanged(payload)` | `user-{customerId}`, `dealer-{dealerId}` | Staff updates booking status or customer cancels | Updates status pill in `app.customer.bookings` & syncs peer `DealerStaff` tabs at the same branch |
| `MaintenanceReminderTriggered(payload)` | `user-{customerId}` | `VinMaintenanceReminderWorker` detects due VIN milestone | Renders persistent due-soon banner in `app.customer.garage` + increments unread bell badge |
| `LoyaltyPointsUpdated(payload)` | `user-{customerId}` | `SalesOrders`, `ServiceInvoices`, CSAT review bonus, voucher redeem, or admin adjustment | Updates `AvailablePoints` & `LifetimeQualifyingPoints` live in navbar & `app.customer.loyalty` |
| `TierUpgraded(payload)` | `user-{customerId}` | `LifetimeQualifyingPoints` crosses `500` / `2,000` / `5,000` | Displays celebratory `Silver` / `Gold` / `Platinum` promotion toast/modal with new multiplier (`1.2x`..`2.0x`) |
| `TestDriveCreated(payload)` | `dealer-{dealerId}`, `role-Admin` | Customer submits `POST /api/v1/test-drives` | **Instant `DealerStaff` Test-Drives Desk (`app.staff.testDrives`) card prepend** + live toast & bell counter without page reload (`F5`) |
| `TestDriveStatusChanged(payload)` | `user-{customerId}`, `dealer-{dealerId}` | Staff confirms/assigns demo VIN/completes test-drive or customer cancels | Updates status badge & assigned demo VIN in `app.customer.testDrives` & syncs peer `DealerStaff` tabs live |
| `ServiceReviewSubmitted(payload)` | `dealer-{dealerId}`, `role-Admin` | Customer submits `POST /api/v1/service-reviews` | **Instant `DealerStaff` Warranty & CSAT Desk (`app.staff.warrantyReviews`) review prepend** + live toast without reload |
| `WarrantyClaimCreated(payload)` | `dealer-{dealerId}`, `role-Admin` | Customer submits `POST /api/v1/warranty-claims` | **Instant `DealerStaff` Warranty & CSAT Desk (`app.staff.warrantyReviews`) claim prepend** + live toast without reload |
| `WarrantyClaimStatusChanged(payload)` | `user-{customerId}`, `dealer-{dealerId}`, `role-Admin` | Staff/Admin transitions warranty status | Updates status stepper, covered OEM part & resolution notes in `app.customer.warranty` & syncs staff/admin tabs live |

---

## 3. AngularJS 1.8 `@microsoft/signalr` Singleton & Digest Safety (`ADR-006`, `RISK-04`)

Implement `SignalRClientService` at `frontend/src/app/core/realtime/signalr-client.service.js` obeying four strict lifecycle rules:

1. **Automatic Reconnect & Token Factory**:
   - Build connection with `.withUrl(hubUrl, { accessTokenFactory: () => AuthService.getAccessToken() }).withAutomaticReconnect([0, 2000, 5000, 10000, 30000]).build()`.
2. **Always Bridge via `$rootScope.$applyAsync()`**:
   - WebSocket callbacks run outside AngularJS's turn. Wrap **every** `connection.on(...)` state update across all `10` events in `$rootScope.$applyAsync(...)` — never call raw `$scope.$apply()`.
3. **Reconnect Delta Reconciliation (`onreconnected`)**:
   - On `connection.onreconnected()`, refresh unread notifications and active `app.staff.workbench` / `app.staff.testDrives` queues silently to reconcile alerts created during transient disconnects.
4. **Mandatory `$onDestroy` & Logout Teardown**:
   - Components (`staffWorkbench`, `staffTestDrives`, `customerBookings`, `customerTestDrives`, `customerGarage`, `customerLoyalty`, `appShell`) MUST store every disposer returned by `SignalRClientService.on(...)` or `$rootScope.$on(...)` and invoke it inside `$onDestroy`.
   - Call `SignalRClientService.stop()` immediately inside `AuthService.logout()` so connections never outlive user sessions.

---

## 4. MCP-Accelerated Verification Workflow (Dual-Tab Live Proof)

Verify real-time push end-to-end across both Workshop Bookings and Showroom Test-Drives:

1. **Framework Reference Check (`context7_context7` MCP)**:
   - Call `resolve-library-id` -> `query-docs` for **ASP.NET Core SignalR** (`Hub<T>`, `IHubContext`, `JwtBearerEvents`) and **`@microsoft/signalr`**.
2. **Dual-Tab Zero-Refresh E2E Proof (`playwright` MCP)**:
   - Open Tab 1 signed in as `staff.hcm@motorcycledealer.vn` (`DLR-HCM-01`) on `/#!/staff/workbench` (or `/#!/staff/test-drives`) and Tab 0 signed in as `customer.silver@motorcycledealer.vn`.
   - Submit a booking (`POST /api/v1/bookings`) or test-drive (`POST /api/v1/test-drives`) in Tab 0, switch back to Tab 1 **without reloading**, and verify instant `BookingCreated` / `TestDriveCreated` rendering while `staff.hn@motorcycledealer.vn` (`DLR-HN-01`) receives **zero** cross-dealer events.
3. **WebSocket Frame & Memory-Leak Inspection (`chrome-devtools-plugin_chrome-devtools` MCP)**:
   - Inspect `POST /hubs/notifications/negotiate` (`200 OK`), `101 Switching Protocols`, zero console `$digest` errors, and zero `$onDestroy` listener leaks (`RISK-04`) across all `30` routes.

---

## 5. Validation & Mandatory Post-Task Sync Checklist

1. **Architecture & Guardrail Audit**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/audit-dealer-guardrails.ps1`
2. **SignalR Hub Negotiate & Full-Stack Smoke Verification (`SMK-01`..`SMK-10`)**:
   - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/smoke-fullstack.ps1`
3. **Mandatory Post-Task Docs, Snapshot & Git Schedule Sync Gate**:
   - Command: `node .agents/scripts/verify-post-task-sync.mjs`
