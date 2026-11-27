(function () {
  'use strict';

  angular
    .module('motorcycleDealerApp')
    .config(configureRoutes);

  configureRoutes.$inject = ['$stateProvider', '$urlRouterProvider', 'PERMISSIONS'];
  function configureRoutes($stateProvider, $urlRouterProvider, PERMISSIONS) {
    $urlRouterProvider.when('', '/showroom');
    $urlRouterProvider.when('/', '/showroom');
    $urlRouterProvider.when('/customer/booking/new', '/customer/bookings/new');
    $urlRouterProvider.when('/customer/invoices', '/customer/bookings');
    $urlRouterProvider.when('/customer/orders', '/customer/loyalty');
    $urlRouterProvider.when('/staff/invoice-builder', '/staff/bookings/select/invoice');
    $urlRouterProvider.when('/staff/invoicing', '/staff/invoicing/select');
    $urlRouterProvider.when('/staff/inventory', '/staff/sales-orders/new');
    $urlRouterProvider.when('/staff/sales-orders', '/staff/sales-orders/new');
    $urlRouterProvider.when('/admin/loyalty-tiers', '/admin/loyalty/tiers');
    $urlRouterProvider.when('/admin/loyalty/adjustments', '/admin/loyalty/tiers');
    $urlRouterProvider.when('/admin/voucher-catalog', '/admin/loyalty/vouchers');
    $urlRouterProvider.when('/customer/warranty-claims', '/customer/warranty');
    $urlRouterProvider.when('/staff/reviews', '/staff/warranty-reviews');
    $urlRouterProvider.when('/admin/warranty', '/admin/quality-warranty');
    $urlRouterProvider.otherwise('/showroom');

    function createPublicState(url, component, screenId, breadcrumb, titleKey) {
      return {
        url: url,
        component: component,
        data: {
          requiresAuth: false,
          screenId: screenId,
          pageTitle: breadcrumb,
          breadcrumb: breadcrumb,
          titleKey: titleKey
        }
      };
    }

    function createProtectedState(url, spec) {
      var def = {
        url: url,
        component: spec.component,
        data: {
          requiresAuth: true,
          roles: spec.roles,
          permissions: spec.permissions,
          screenId: spec.screenId,
          pageTitle: spec.breadcrumb,
          breadcrumb: spec.breadcrumb,
          titleKey: spec.titleKey
        }
      };
      if (spec.params) {
        def.params = angular.copy(spec.params);
      }
      return def;
    }

    function registerProtectedState(stateName, url, spec, aliasName, aliasUrl) {
      $stateProvider.state(stateName, createProtectedState(url, spec));
      if (aliasName && aliasUrl) {
        $stateProvider.state(aliasName, createProtectedState(aliasUrl, spec));
      }
    }

    // Root Shell & Namespace Abstract Parents (5 Abstract States)
    $stateProvider
      .state('app', {
        abstract: true,
        component: 'appShell'
      })
      .state('app.public', {
        abstract: true,
        template: '<ui-view></ui-view>',
        data: {
          requiresAuth: false,
          portalLabel: 'Public Showroom',
          portalKey: 'nav.portalPublic'
        }
      })
      .state('app.customer', {
        abstract: true,
        url: '/customer',
        template: '<ui-view></ui-view>',
        data: {
          requiresAuth: true,
          roles: ['Customer'],
          portalLabel: 'Customer Portal',
          portalKey: 'nav.portalCustomer'
        }
      })
      .state('app.staff', {
        abstract: true,
        url: '/staff',
        template: '<ui-view></ui-view>',
        data: {
          requiresAuth: true,
          roles: ['DealerStaff'],
          portalLabel: 'Dealer Staff Portal',
          portalKey: 'nav.portalStaff'
        }
      })
      .state('app.admin', {
        abstract: true,
        url: '/admin',
        template: '<ui-view></ui-view>',
        data: {
          requiresAuth: true,
          roles: ['Admin'],
          portalLabel: 'Admin Executive HQ',
          portalKey: 'nav.portalAdmin'
        }
      });

    // 1..4: Public Namespace (SCR-PUB-01 .. SCR-PUB-03)
    $stateProvider
      .state('app.public.showroom', createPublicState('/showroom', 'publicShowroom', 'SCR-PUB-01', 'Motorcycle Showroom & Live Stock', 'nav.showroom'))
      .state('app.public.login', createPublicState('/login?returnUrl&returnTo&reason', 'publicLogin', 'SCR-PUB-02A', 'Sign In', 'nav.signIn'))
      .state('app.public.register', createPublicState('/register', 'publicRegister', 'SCR-PUB-02B', 'Customer Self-Registration', 'nav.register'))
      .state('app.public.forbidden', createPublicState('/403', 'publicForbidden', 'SCR-PUB-03', '403 Access Denied', 'nav.forbidden'));

    // 5..11: Customer Portal Namespace (SCR-CUS-01 .. SCR-CUS-07 + bookService alias)
    registerProtectedState('app.customer.dashboard', '/dashboard', {
      component: 'customerDashboard',
      roles: ['Customer'],
      permissions: [PERMISSIONS.Vehicles.ViewOwn, PERMISSIONS.Bookings.ViewOwn, PERMISSIONS.Loyalty.ViewOwn],
      screenId: 'SCR-CUS-01',
      breadcrumb: 'Customer Overview Dashboard',
      titleKey: 'nav.customerDashboard'
    });

    registerProtectedState('app.customer.garage', '/garage', {
      component: 'customerGarage',
      roles: ['Customer'],
      permissions: [PERMISSIONS.Vehicles.ViewOwn, PERMISSIONS.Vehicles.RegisterOwn],
      screenId: 'SCR-CUS-02',
      breadcrumb: 'My Digital VIN Garage',
      titleKey: 'nav.customerGarage'
    });

    registerProtectedState(
      'app.customer.bookingNew',
      '/bookings/new?vehicleId&dealerId&voucherId',
      {
        component: 'customerBookingNew',
        roles: ['Customer'],
        permissions: [PERMISSIONS.Bookings.Create, PERMISSIONS.TimeSlots.ViewAvailable],
        screenId: 'SCR-CUS-03',
        breadcrumb: 'Book Workshop Service Slot',
        titleKey: 'nav.customerBookingNew'
      },
      'app.customer.bookService',
      '/book-service?vehicleId&dealerId&voucherId'
    );

    registerProtectedState('app.customer.bookings', '/bookings', {
      component: 'customerBookings',
      roles: ['Customer'],
      permissions: [PERMISSIONS.Bookings.ViewOwn, PERMISSIONS.Bookings.CancelOwn, PERMISSIONS.Invoices.ViewOwn],
      screenId: 'SCR-CUS-04',
      breadcrumb: 'My Service Appointments & Invoices',
      titleKey: 'nav.customerBookings'
    });

    registerProtectedState('app.customer.loyalty', '/loyalty', {
      component: 'customerLoyalty',
      roles: ['Customer'],
      permissions: [PERMISSIONS.Loyalty.ViewOwn, PERMISSIONS.Loyalty.RedeemVoucher, PERMISSIONS.SalesOrders.ViewOwn],
      screenId: 'SCR-CUS-05',
      breadcrumb: 'Loyalty Wallet & Voucher Store',
      titleKey: 'nav.customerLoyalty'
    });

    registerProtectedState('app.customer.testDrives', '/test-drives?modelId&dealerId', {
      component: 'customerTestDrives',
      roles: ['Customer'],
      permissions: [PERMISSIONS.TestDrives.ViewOwn, PERMISSIONS.TestDrives.Create],
      screenId: 'SCR-CUS-06',
      breadcrumb: 'Showroom Test-Drive Appointments',
      titleKey: 'nav.customerTestDrives'
    });

    registerProtectedState('app.customer.warranty', '/warranty?vehicleId&dealerId', {
      component: 'customerWarranty',
      roles: ['Customer'],
      permissions: [PERMISSIONS.Warranty.ViewOwn, PERMISSIONS.Warranty.CreateOwn],
      screenId: 'SCR-CUS-07',
      breadcrumb: 'OEM VIN Warranty Portal & Claims',
      titleKey: 'nav.customerWarranty'
    });

    // 12..18: Dealer Staff Portal Namespace (SCR-STF-01 .. SCR-STF-07 + invoicing/salesNew/slots aliases)
    registerProtectedState('app.staff.workbench', '/workbench', {
      component: 'staffWorkbench',
      roles: ['DealerStaff'],
      permissions: [PERMISSIONS.Bookings.ViewDealer, PERMISSIONS.Bookings.UpdateStatus, PERMISSIONS.Vehicles.ViewDealer],
      screenId: 'SCR-STF-01',
      breadcrumb: 'Live SignalR Booking Workbench',
      titleKey: 'nav.staffWorkbench'
    });

    registerProtectedState(
      'app.staff.invoiceBuilder',
      '/bookings/:bookingId/invoice',
      {
        component: 'staffInvoiceBuilder',
        params: { bookingId: { value: 'select', squash: false } },
        roles: ['DealerStaff'],
        permissions: [PERMISSIONS.Invoices.CreateDealer, PERMISSIONS.Invoices.ViewDealer, PERMISSIONS.Loyalty.ViewDealerCustomer],
        screenId: 'SCR-STF-02',
        breadcrumb: 'Workshop Service Invoice Builder',
        titleKey: 'nav.staffInvoiceBuilder'
      },
      'app.staff.invoicing',
      '/invoicing/:bookingId'
    );

    registerProtectedState(
      'app.staff.salesDesk',
      '/sales-orders/new',
      {
        component: 'staffSalesDesk',
        roles: ['DealerStaff'],
        permissions: [
          PERMISSIONS.SalesOrders.Create,
          PERMISSIONS.SalesOrders.ViewDealer,
          PERMISSIONS.Inventories.ViewDealer,
          PERMISSIONS.Inventories.ManageDealer
        ],
        screenId: 'SCR-STF-03',
        breadcrumb: 'Showroom VIN Inventory & Sales Desk',
        titleKey: 'nav.staffSalesDesk'
      },
      'app.staff.salesNew',
      '/sales-new'
    );

    registerProtectedState(
      'app.staff.timeSlots',
      '/time-slots',
      {
        component: 'staffTimeSlots',
        roles: ['DealerStaff'],
        permissions: [PERMISSIONS.TimeSlots.ManageDealer, PERMISSIONS.TimeSlots.ViewAvailable],
        screenId: 'SCR-STF-04',
        breadcrumb: 'Workshop Bay & Time-Slot Manager',
        titleKey: 'nav.staffTimeSlots'
      },
      'app.staff.slots',
      '/slots'
    );

    registerProtectedState('app.staff.customers', '/customers', {
      component: 'staffCustomers',
      roles: ['DealerStaff'],
      permissions: [PERMISSIONS.Vehicles.ViewDealer, PERMISSIONS.Loyalty.ViewDealerCustomer],
      screenId: 'SCR-STF-05',
      breadcrumb: 'Customer, VIN Garage & Loyalty Lookup',
      titleKey: 'nav.staffCustomers'
    });

    registerProtectedState('app.staff.testDrives', '/test-drives', {
      component: 'staffTestDrives',
      roles: ['DealerStaff'],
      permissions: [PERMISSIONS.TestDrives.ViewDealer, PERMISSIONS.TestDrives.UpdateStatus],
      screenId: 'SCR-STF-06',
      breadcrumb: 'Showroom Test-Drive Desk',
      titleKey: 'nav.staffTestDrives'
    });

    registerProtectedState('app.staff.warrantyReviews', '/warranty-reviews', {
      component: 'staffWarrantyReviews',
      roles: ['DealerStaff'],
      permissions: [PERMISSIONS.Warranty.ViewDealer, PERMISSIONS.Reviews.ViewDealer],
      screenId: 'SCR-STF-07',
      breadcrumb: 'VIN Warranty & 5-Star CSAT Desk',
      titleKey: 'nav.staffWarrantyReviews'
    });

    // 19..26: Admin Executive Portal Namespace (SCR-ADM-01 .. SCR-ADM-08)
    registerProtectedState('app.admin.analytics', '/analytics', {
      component: 'adminAnalytics',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Analytics.ViewAllDealers, PERMISSIONS.Analytics.ExportReports],
      screenId: 'SCR-ADM-01',
      breadcrumb: 'Executive Multi-Dealer Analytics',
      titleKey: 'nav.adminAnalytics'
    });

    registerProtectedState('app.admin.dealers', '/dealers', {
      component: 'adminDealers',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Dealers.ManageAll, PERMISSIONS.Dealers.ViewPublic],
      screenId: 'SCR-ADM-02',
      breadcrumb: 'Dealership Network Governance',
      titleKey: 'nav.adminDealers'
    });

    registerProtectedState('app.admin.staff', '/staff?dealerId', {
      component: 'adminStaff',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Users.ManageStaff],
      screenId: 'SCR-ADM-03',
      breadcrumb: 'Dealer Staff Provisioning',
      titleKey: 'nav.adminStaff'
    });

    registerProtectedState('app.admin.models', '/models', {
      component: 'adminModels',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Showroom.ManageModels, PERMISSIONS.Vehicles.ManageRules],
      screenId: 'SCR-ADM-04',
      breadcrumb: 'Motorcycle Models & OEM Rules',
      titleKey: 'nav.adminModels'
    });

    registerProtectedState('app.admin.loyaltyTiers', '/loyalty/tiers', {
      component: 'adminLoyaltyTiers',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Loyalty.ManageTiers, PERMISSIONS.Loyalty.AdjustPoints],
      screenId: 'SCR-ADM-05',
      breadcrumb: 'Loyalty Tiers & Audited Adjustments',
      titleKey: 'nav.adminLoyaltyTiers'
    });

    registerProtectedState('app.admin.voucherCatalog', '/loyalty/vouchers', {
      component: 'adminVoucherCatalog',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Loyalty.ManageVoucherCatalog],
      screenId: 'SCR-ADM-06',
      breadcrumb: 'Reward Voucher Catalog Studio',
      titleKey: 'nav.adminVoucherCatalog'
    });

    registerProtectedState('app.admin.spareParts', '/spare-parts', {
      component: 'adminSpareParts',
      roles: ['Admin'],
      permissions: [PERMISSIONS.SpareParts.ManageCatalog],
      screenId: 'SCR-ADM-07',
      breadcrumb: 'OEM Spare-Parts Catalog Studio',
      titleKey: 'nav.adminSpareParts'
    });

    registerProtectedState('app.admin.qualityWarranty', '/quality-warranty', {
      component: 'adminQualityWarranty',
      roles: ['Admin'],
      permissions: [PERMISSIONS.Warranty.ViewDealer, PERMISSIONS.Reviews.ViewDealer],
      screenId: 'SCR-ADM-08',
      breadcrumb: 'Quality, CSAT & Warranty Oversight',
      titleKey: 'nav.adminQualityWarranty'
    });
  }
})();
