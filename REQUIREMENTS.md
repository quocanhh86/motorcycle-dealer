# Project Requirements — Motorcycle Dealer & Loyalty Management Platform

## 1. Raw Text Extracted from Image

```text
Projects
Full-Stack Developer                                        02/2026 - 08/2026
MOTORCYCLE DEALER & LOYALTY MANAGEMENT PLATFORM

Team Size: 1 (Solo project)
Technologies:
• Front-End: AngularJS
• Back-End: C#, ASP.NET Core Web API, Entity Framework Core, SignalR
• Database: SQL Server
Key Features:
• Role-Based Access: Three user roles (Customer, Dealer Staff, Admin) with route guards and granular permissions per role
• Service Booking System: Customers can book maintenance appointments by dealer and available time slot, with automated reminders based on each vehicle's VIN and maintenance schedule
• Loyalty & Rewards Engine: Automatically calculates points based on spending, manages membership tiers, and handles voucher redemption
• Real-Time Notifications: Implemented with SignalR so dealer staff receive new booking alerts instantly without page refresh
• Admin Analytics Dashboard: Revenue and booking statistics per dealer, visualized with Chart.js
Link Github: https://github.com/quocanhh86/motorcycle-dealer
```

---

## 2. Project Metadata & Scope

| Field | Requirement |
| :--- | :--- |
| **Project Name** | Motorcycle Dealer & Loyalty Management Platform |
| **Role** | Full-Stack Developer |
| **Timeline** | 02/2026 - 08/2026 |
| **Team Size** | 1 (Solo project) |
| **GitHub Repository** | https://github.com/quocanhh86/motorcycle-dealer |

---

## 3. Technical Stack Requirements (Mandatory)

- [ ] **Front-End**: AngularJS (with client-side route guards and Chart.js integration)
- [ ] **Back-End**:
  - C#
  - ASP.NET Core Web API
  - Entity Framework Core (ORM & Migrations)
  - SignalR (Real-time WebSocket hub)
- [ ] **Database**: Microsoft SQL Server

---

## 4. Functional Requirements (Key Features)

### FR-01: Role-Based Access Control (RBAC)
- [ ] Support exactly **three user roles**:
  1. `Customer`
  2. `Dealer Staff`
  3. `Admin`
- [ ] Implement Front-End **route guards** (AngularJS) to restrict page access by role.
- [ ] Implement Back-End **granular permissions per role** on ASP.NET Core Web API endpoints.

### FR-02: Service Booking System
- [ ] Allow `Customer` users to book maintenance appointments by selecting:
  - Target **Dealer**
  - **Available time slot**
- [ ] Track customer vehicles by **VIN** (Vehicle Identification Number) and **maintenance schedule**.
- [ ] Trigger **automated reminders** based on each vehicle's VIN and maintenance schedule.

### FR-03: Loyalty & Rewards Engine
- [ ] **Automatic Points Calculation**: Automatically calculate loyalty points based on customer spending.
- [ ] **Membership Tiers**: Manage tier progression/status based on accumulated points/spending.
- [ ] **Voucher Redemption**: Handle reward catalog browsing, point deduction, and voucher redemption.

### FR-04: Real-Time Notifications
- [ ] Implement real-time push notifications using **SignalR**.
- [ ] Ensure `Dealer Staff` receive **new booking alerts instantly without page refresh**.

### FR-05: Admin Analytics Dashboard
- [ ] Provide an analytics dashboard for `Admin` users.
- [ ] Display **revenue statistics per dealer**.
- [ ] Display **booking statistics per dealer**.
- [ ] Visualize dashboard charts using **Chart.js**.
