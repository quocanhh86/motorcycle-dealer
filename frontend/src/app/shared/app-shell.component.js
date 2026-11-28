(function () {
  'use strict';

  angular
    .module('app.shared')
    .component('appShell', {
      controllerAs: '$ctrl',
      controller: AppShellController,
      template:
        '<div class="app-root">' +
          '<route-progress-bar></route-progress-bar>' +
          '<a class="sr-only" href="#mainViewport">{{ \'a11y.skipToMain\' | t }}</a>' +

          '<header class="top-navbar" ng-class="{\'is-authenticated\': $ctrl.isAuthenticated}">' +
            '<div class="navbar-brand-cluster">' +
              '<button type="button" class="btn btn-ghost-dark mobile-menu-btn" ' +
                      'ng-if="$ctrl.isAuthenticated" ' +
                      'ng-attr-aria-label="{{ \'a11y.toggleMenu\' | t }}" ' +
                      'ng-attr-aria-expanded="{{ $ctrl.mobileSidebarOpen ? \'true\' : \'false\' }}" ' +
                      'ng-click="$ctrl.toggleMobileSidebar()">' +
                '<app-icon name="{{ $ctrl.mobileSidebarOpen ? \'x\' : \'list\' }}" weight="regular" size="18"></app-icon>' +
              '</button>' +
              '<a class="brand-logo" ui-sref="app.public.showroom">' +
                '<span class="brand-accent-bar" aria-hidden="true"></span>' +
                '<span>{{ \'nav.brandName\' | t }}</span>' +
              '</a>' +
              '<nav class="navbar-public-links" ng-attr-aria-label="{{ \'a11y.primaryNav\' | t }}">' +
                '<a class="navbar-nav-link" ui-sref="app.public.showroom" ui-sref-active="is-active">' +
                  '<app-icon name="storefront" weight="duotone" size="15"></app-icon>' +
                  '<span>{{ \'nav.showroom\' | t }}</span>' +
                '</a>' +
                '<a class="navbar-nav-link" ng-if="$ctrl.user.role === \'Customer\'" ui-sref="app.customer.garage" ui-sref-active="is-active">' +
                  '<app-icon name="garage" weight="duotone" size="15"></app-icon>' +
                  '<span>{{ \'nav.customerGarage\' | t }}</span>' +
                '</a>' +
                '<a class="navbar-nav-link" ng-if="$ctrl.user.role === \'DealerStaff\'" ui-sref="app.staff.workbench" ui-sref-active="is-active">' +
                  '<app-icon name="wrench" weight="duotone" size="15"></app-icon>' +
                  '<span>{{ \'nav.staffWorkbench\' | t }}</span>' +
                '</a>' +
                '<a class="navbar-nav-link" ng-if="$ctrl.user.role === \'Admin\'" ui-sref="app.admin.analytics" ui-sref-active="is-active">' +
                  '<app-icon name="chart-bar" weight="duotone" size="15"></app-icon>' +
                  '<span>{{ \'nav.adminAnalytics\' | t }}</span>' +
                '</a>' +
              '</nav>' +
            '</div>' +

            '<div class="navbar-right-cluster">' +
              '<span class="badge" ng-if="$ctrl.isAuthenticated" ' +
                    'ng-class="[$ctrl.signalrBadgeClass(), $ctrl.isLivePulsing ? \'is-signalr-pulse signalr-live-pulse\' : \'\']">' +
                '<app-icon name="{{ $ctrl.signalrStatus === \'Connected\' ? \'broadcast\' : ($ctrl.signalrStatus === \'Reconnecting\' || $ctrl.signalrStatus === \'Connecting\' ? \'arrows-clockwise\' : \'wifi-slash\') }}" ' +
                          'weight="fill" size="12" ' +
                          'extra-class="{{ ($ctrl.signalrStatus === \'Reconnecting\' || $ctrl.signalrStatus === \'Connecting\') ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ \'nav.signalrLabel\' | t }}: {{ $ctrl.signalrStatus | enumT:\'SignalRStatus\' }}</span>' +
              '</span>' +
              '<span class="badge" ng-if="$ctrl.user.role === \'Customer\'" ' +
                    'ng-class="\'badge-tier-\' + ($ctrl.user.tierCode || \'Bronze\').toLowerCase()">' +
                '<app-icon name="medal" weight="fill" size="13"></app-icon>' +
                '<span>{{ $ctrl.user.tierCode | enumT:\'LoyaltyTierCode\' }} ({{ $ctrl.user.tierMultiplier }}x)</span>' +
                '<span aria-hidden="true"> • </span>' +
                '<span class="mono-num">{{ $ctrl.user.availablePoints | number }} {{ \'common.ptsShort\' | t }}</span>' +
              '</span>' +
              '<span class="badge badge-info mono-num" ng-if="$ctrl.user.role === \'DealerStaff\'">' +
                '<app-icon name="buildings" weight="duotone" size="13"></app-icon>' +
                '<span>{{ $ctrl.user.dealerCode || \'DLR-SCOPED\' }}</span>' +
              '</span>' +
              '<span class="badge badge-tier-platinum" ng-if="$ctrl.user.role === \'Admin\'">' +
                '<app-icon name="crown" weight="fill" size="13"></app-icon>' +
                '<span>{{ \'nav.allDealersHq\' | t }}</span>' +
              '</span>' +

              /* Compact VI | EN Locale Hot-Switch Pill */
              '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                      'style="gap:6px;padding:4px 10px;border-radius:var(--radius-full);" ' +
                      'ng-attr-title="{{ \'a11y.toggleLocale\' | t }}" ' +
                      'ng-attr-aria-pressed="{{ $ctrl.localeCode === \'en\' ? \'true\' : \'false\' }}" ' +
                      'ng-click="$ctrl.toggleLocale()">' +
                '<app-icon name="translate" weight="regular" size="14"></app-icon>' +
                '<span class="mono-num" style="display:inline-flex;align-items:center;gap:4px;font-size:11px;">' +
                  '<span ng-style="{\'font-weight\': $ctrl.localeCode === \'vi\' ? \'700\' : \'500\', \'opacity\': $ctrl.localeCode === \'vi\' ? \'1\' : \'0.55\'}">{{ \'nav.localeVi\' | t }}</span>' +
                  '<span style="opacity:0.4;" aria-hidden="true">|</span>' +
                  '<span ng-style="{\'font-weight\': $ctrl.localeCode === \'en\' ? \'700\' : \'500\', \'opacity\': $ctrl.localeCode === \'en\' ? \'1\' : \'0.55\'}">{{ \'nav.localeEn\' | t }}</span>' +
                '</span>' +
                '<span class="sr-only"> — {{ \'a11y.toggleLocale\' | t }}</span>' +
              '</button>' +

              /* Light Showroom / Dark Pit-Lane Theme Toggle */
              '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                      'style="gap:6px;padding:4px 10px;border-radius:var(--radius-full);" ' +
                      'ng-attr-title="{{ \'a11y.toggleTheme\' | t }}" ' +
                      'ng-attr-aria-pressed="{{ $ctrl.isDarkTheme ? \'true\' : \'false\' }}" ' +
                      'ng-click="$ctrl.toggleTheme()">' +
                '<app-icon name="{{ $ctrl.isDarkTheme ? \'sun\' : \'moon\' }}" weight="duotone" size="15"></app-icon>' +
                '<span style="font-size:11px;">{{ $ctrl.isDarkTheme ? (\'nav.themeLight\' | t) : (\'nav.themeDark\' | t) }}</span>' +
                '<span class="sr-only"> — {{ \'a11y.toggleTheme\' | t }}</span>' +
              '</button>' +

              '<notification-bell ng-if="$ctrl.isAuthenticated"></notification-bell>' +
              '<activity-log-drawer></activity-log-drawer>' +

              '<div ng-if="!$ctrl.isAuthenticated" style="display:flex;gap:8px;">' +
                '<a class="btn btn-ghost-dark btn-sm" ui-sref="app.public.login">' +
                  '<app-icon name="sign-in" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'nav.signIn\' | t }}</span>' +
                '</a>' +
                '<a class="btn btn-primary btn-sm" ui-sref="app.public.register">' +
                  '<app-icon name="user-plus" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'nav.register\' | t }}</span>' +
                '</a>' +
              '</div>' +

              '<div ng-if="$ctrl.isAuthenticated" style="display:flex;align-items:center;gap:8px;">' +
                '<span class="navbar-user-name" style="font-size:12px;color:var(--clr-slate-50);opacity:0.9;">{{ $ctrl.user.fullName }}</span>' +
                '<button type="button" class="btn btn-ghost-dark btn-sm" ng-click="$ctrl.logout()">' +
                  '<app-icon name="sign-out" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'nav.signOut\' | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</header>' +

          '<div class="shell-body">' +
            '<aside class="role-sidebar" ng-if="$ctrl.isAuthenticated" ' +
                   'ng-attr-aria-label="{{ \'a11y.sidebarNav\' | t }}" ' +
                   'ng-class="{\'is-mobile-open\': $ctrl.mobileSidebarOpen}">' +
              '<div>' +
                '<div class="sidebar-section-label">' +
                  '{{ $ctrl.portalKey ? ($ctrl.portalKey | t) : $ctrl.portalLabel }}' +
                '</div>' +
                '<ul class="sidebar-nav-list">' +
                  '<li class="sidebar-nav-item" ng-repeat="nav in $ctrl.navItems track by nav.state">' +
                    '<a ui-sref="{{ ::nav.state }}" ui-sref-active="is-active" ng-click="$ctrl.closeMobileSidebar()">' +
                      '<span style="display:inline-flex;align-items:center;gap:8px;min-width:0;">' +
                        '<app-icon name="{{ ::nav.icon }}" weight="duotone" size="16"></app-icon>' +
                        '<span>{{ nav.labelKey | t }}</span>' +
                      '</span>' +
                      '<span class="mono-num" style="font-size:11px;opacity:0.75;">{{ ::nav.code }}</span>' +
                    '</a>' +
                  '</li>' +
                '</ul>' +
              '</div>' +

              '<div class="sidebar-scope-box">' +
                '<div style="display:flex;align-items:center;gap:6px;font-weight:700;color:var(--clr-slate-50);margin-bottom:6px;">' +
                  '<app-icon name="shield-check" weight="duotone" size="14"></app-icon>' +
                  '<span>{{ \'nav.rbacScopeTitle\' | t }}</span>' +
                '</div>' +
                '<div>{{ \'nav.roleLabel\' | t }}: <strong>{{ $ctrl.user.role | enumT:\'UserRoleCode\' }}</strong></div>' +
                '<div ng-if="$ctrl.user.dealerCode">{{ \'nav.tenantLabel\' | t }}: <span class="mono-num">{{ $ctrl.user.dealerCode }}</span></div>' +
                '<div ng-if="$ctrl.user.dealerName" style="margin-top:2px;opacity:0.78;">{{ $ctrl.user.dealerName }}</div>' +
              '</div>' +
            '</aside>' +

            '<div class="shell-main">' +
              '<div class="breadcrumb-bar">' +
                '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                  '<span class="text-muted">{{ $ctrl.portalKey ? ($ctrl.portalKey | t) : $ctrl.portalLabel }}</span>' +
                  '<app-icon name="caret-right" weight="regular" size="11"></app-icon>' +
                  '<strong class="text-primary">{{ $ctrl.activeTitleKey ? ($ctrl.activeTitleKey | t) : $ctrl.activeBreadcrumb }}</strong>' +
                  '<span class="badge badge-neutral mono-num" style="margin-left:4px;" ng-if="$ctrl.activeScreenId">{{ $ctrl.activeScreenId }}</span>' +
                '</div>' +
                '<div class="mono-num text-muted" style="font-size:12px;">API: {{ ::$ctrl.apiBaseUrl }}</div>' +
              '</div>' +

              '<main id="mainViewport" class="app-viewport" role="main">' +
                '<ui-view class="view-enter"></ui-view>' +
              '</main>' +
            '</div>' +
          '</div>' +

          '<toast-container></toast-container>' +
        '</div>'
    });

  AppShellController.$inject = [
    '$rootScope',
    '$state',
    '$transitions',
    'APP_CONFIG',
    'AuthService',
    'SignalRClientService',
    'I18nService',
    'ThemeService',
    '$timeout'
  ];
  function AppShellController(
    $rootScope,
    $state,
    $transitions,
    APP_CONFIG,
    AuthService,
    SignalRClientService,
    I18nService,
    ThemeService,
    $timeout
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var pulseTimer = null;
    var isDestroyed = false;

    function clearPulseTimer() {
      if (pulseTimer && $timeout && angular.isFunction($timeout.cancel)) {
        $timeout.cancel(pulseTimer);
      }
      pulseTimer = null;
    }

    function triggerLivePulse() {
      if (isDestroyed) {
        return;
      }
      clearPulseTimer();
      $ctrl.isLivePulsing = true;
      if ($timeout && angular.isFunction($timeout)) {
        pulseTimer = $timeout(function () {
          if (!isDestroyed) {
            $ctrl.isLivePulsing = false;
          }
          pulseTimer = null;
        }, 1300);
      }
    }

    var NAV_BY_ROLE = {
      Customer: [
        { state: 'app.customer.dashboard', labelKey: 'nav.customerDashboard', code: 'CUS-01', icon: 'squares-four' },
        { state: 'app.customer.garage', labelKey: 'nav.customerGarage', code: 'CUS-02', icon: 'garage' },
        { state: 'app.customer.bookingNew', labelKey: 'nav.customerBookingNew', code: 'CUS-03', icon: 'calendar-plus' },
        { state: 'app.customer.bookings', labelKey: 'nav.customerBookings', code: 'CUS-04', icon: 'clipboard-text' },
        { state: 'app.customer.loyalty', labelKey: 'nav.customerLoyalty', code: 'CUS-05', icon: 'medal' },
        { state: 'app.customer.testDrives', labelKey: 'nav.customerTestDrives', code: 'CUS-06', icon: 'motorcycle' },
        { state: 'app.customer.warranty', labelKey: 'nav.customerWarranty', code: 'CUS-07', icon: 'shield-check' },
        { state: 'app.public.showroom', labelKey: 'nav.showroom', code: 'PUB-01', icon: 'storefront' }
      ],
      DealerStaff: [
        { state: 'app.staff.workbench', labelKey: 'nav.staffWorkbench', code: 'STF-01', icon: 'kanban' },
        { state: 'app.staff.invoiceBuilder', labelKey: 'nav.staffInvoiceBuilder', code: 'STF-02', icon: 'receipt' },
        { state: 'app.staff.salesDesk', labelKey: 'nav.staffSalesDesk', code: 'STF-03', icon: 'tag' },
        { state: 'app.staff.timeSlots', labelKey: 'nav.staffTimeSlots', code: 'STF-04', icon: 'clock' },
        { state: 'app.staff.customers', labelKey: 'nav.staffCustomers', code: 'STF-05', icon: 'users' },
        { state: 'app.staff.testDrives', labelKey: 'nav.staffTestDrives', code: 'STF-06', icon: 'motorcycle' },
        { state: 'app.staff.warrantyReviews', labelKey: 'nav.staffWarrantyReviews', code: 'STF-07', icon: 'seal-check' },
        { state: 'app.public.showroom', labelKey: 'nav.showroom', code: 'PUB-01', icon: 'storefront' }
      ],
      Admin: [
        { state: 'app.admin.analytics', labelKey: 'nav.adminAnalytics', code: 'ADM-01', icon: 'chart-bar' },
        { state: 'app.admin.dealers', labelKey: 'nav.adminDealers', code: 'ADM-02', icon: 'buildings' },
        { state: 'app.admin.staff', labelKey: 'nav.adminStaff', code: 'ADM-03', icon: 'identification-badge' },
        { state: 'app.admin.models', labelKey: 'nav.adminModels', code: 'ADM-04', icon: 'motorcycle' },
        { state: 'app.admin.loyaltyTiers', labelKey: 'nav.adminLoyaltyTiers', code: 'ADM-05', icon: 'medal' },
        { state: 'app.admin.voucherCatalog', labelKey: 'nav.adminVoucherCatalog', code: 'ADM-06', icon: 'ticket' },
        { state: 'app.admin.spareParts', labelKey: 'nav.adminSpareParts', code: 'ADM-07', icon: 'gear-six' },
        { state: 'app.admin.qualityWarranty', labelKey: 'nav.adminQualityWarranty', code: 'ADM-08', icon: 'shield-star' },
        { state: 'app.public.showroom', labelKey: 'nav.showroom', code: 'PUB-01', icon: 'storefront' }
      ]
    };

    function resolvePortalKeyFallback(role) {
      if (role === 'Customer') {
        return 'nav.portalCustomer';
      }
      if (role === 'DealerStaff') {
        return 'nav.portalStaff';
      }
      if (role === 'Admin') {
        return 'nav.portalAdmin';
      }
      return 'nav.portalPublic';
    }

    function syncLocaleAndTheme() {
      $ctrl.localeCode = (I18nService && angular.isFunction(I18nService.getShortCode))
        ? I18nService.getShortCode()
        : 'vi';
      $ctrl.isDarkTheme = (ThemeService && angular.isFunction(ThemeService.isDark))
        ? ThemeService.isDark()
        : false;
    }

    function syncAuthContext() {
      $ctrl.isAuthenticated = AuthService.isAuthenticated();
      $ctrl.user = AuthService.getCurrentUser() || {};
      $ctrl.navItems = ($ctrl.user.role && NAV_BY_ROLE[$ctrl.user.role]) || [];
      $ctrl.signalrStatus = SignalRClientService.getConnectionStatus();
    }

    function syncRouteMetadata(stateObj) {
      var st = stateObj || ($state && $state.current) || {};
      var d = st.data || {};
      $ctrl.activeBreadcrumb = d.breadcrumb || d.pageTitle || 'Showroom';
      $ctrl.activeTitleKey = d.titleKey || null;
      $ctrl.activeScreenId = d.screenId || '';
      $ctrl.portalLabel = d.portalLabel || ($ctrl.user && $ctrl.user.role ? ($ctrl.user.role + ' Portal') : 'Public Portal');
      $ctrl.portalKey = d.portalKey || resolvePortalKeyFallback($ctrl.user && $ctrl.user.role);

      if (I18nService && angular.isFunction(I18nService.setPageTitle)) {
        I18nService.setPageTitle($ctrl.activeTitleKey || $ctrl.activeBreadcrumb);
      }
    }

    $ctrl.$onInit = function () {
      $ctrl.apiBaseUrl = APP_CONFIG.apiBaseUrl;
      $ctrl.mobileSidebarOpen = false;
      $ctrl.isLivePulsing = false;
      syncLocaleAndTheme();
      syncAuthContext();
      syncRouteMetadata($state && $state.current);

      cleanupFns.push($rootScope.$on('auth:changed', function () {
        syncAuthContext();
        syncRouteMetadata($state && $state.current);
      }));

      cleanupFns.push($rootScope.$on('signalr:status', function (_evt, nextStatus) {
        $ctrl.signalrStatus = nextStatus;
      }));

      cleanupFns.push($rootScope.$on('signalr:live-pulse', function () {
        triggerLivePulse();
      }));

      cleanupFns.push($rootScope.$on('i18n:changed', function () {
        syncLocaleAndTheme();
        syncRouteMetadata($state && $state.current);
      }));

      cleanupFns.push($rootScope.$on('theme:changed', function () {
        syncLocaleAndTheme();
      }));

      if ($transitions && angular.isFunction($transitions.onSuccess)) {
        cleanupFns.push($transitions.onSuccess({}, function (transition) {
          $ctrl.mobileSidebarOpen = false;
          syncRouteMetadata(transition.to());
        }));
      }
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      clearPulseTimer();
      for (var i = 0; i < cleanupFns.length; i++) {
        if (angular.isFunction(cleanupFns[i])) {
          cleanupFns[i]();
        }
      }
      cleanupFns.length = 0;
    };

    $ctrl.toggleLocale = function () {
      if (I18nService && angular.isFunction(I18nService.toggleLocale)) {
        I18nService.toggleLocale();
        syncLocaleAndTheme();
      }
    };

    $ctrl.toggleTheme = function () {
      if (ThemeService && angular.isFunction(ThemeService.toggleTheme)) {
        ThemeService.toggleTheme();
        syncLocaleAndTheme();
      }
    };

    $ctrl.toggleMobileSidebar = function () {
      $ctrl.mobileSidebarOpen = !$ctrl.mobileSidebarOpen;
    };

    $ctrl.closeMobileSidebar = function () {
      $ctrl.mobileSidebarOpen = false;
    };

    $ctrl.signalrBadgeClass = function () {
      if ($ctrl.signalrStatus === 'Connected') {
        return 'badge-success';
      }
      if ($ctrl.signalrStatus === 'Reconnecting' || $ctrl.signalrStatus === 'Connecting') {
        return 'badge-warning';
      }
      return 'badge-danger';
    };

    $ctrl.logout = function () {
      AuthService.logout().finally(function () {
        $state.go('app.public.login');
      });
    };
  }
})();
