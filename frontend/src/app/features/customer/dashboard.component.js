(function () {
  'use strict';

  var VI_PATCH = {
    customerDash: {
      eyebrow: 'CỔNG KHÁCH HÀNG • TỔNG QUAN',
      welcomeBack: 'Chào mừng trở lại',
      welcomeNamed: 'Chào mừng trở lại, {{name}}',
      subtitle: 'Theo dõi Garage số khung VIN 17 ký tự, lịch hẹn bảo dưỡng xưởng và quyền lợi hạng thành viên theo thời gian thực.',
      refreshing: 'Đang làm mới...',
      openGarage: 'Mở Garage VIN',
      bookService: 'Đặt lịch bảo dưỡng',
      overdueBannerTitle: 'QUÁ HẠN BẢO DƯỠNG:',
      dueSoonBannerTitle: 'SẮP ĐẾN MỐC BẢO DƯỠNG:',
      dueBannerDetail: '{{model}} (VIN {{vin}}) hiện đạt {{currentKm}} km — mốc kế tiếp {{targetKm}} km',
      bookNow: 'Đặt lịch ngay',
      kpiAvailablePoints: 'Điểm khả dụng',
      kpiAvailableMeta: 'Dùng để đổi voucher ưu đãi xưởng & showroom',
      kpiLifetimePoints: 'Điểm tích lũy xét hạng',
      kpiLifetimeMeta: 'Không giảm khi đổi voucher ưu đãi',
      kpiVehicles: 'Xe trong Garage VIN',
      kpiVehiclesAllGood: 'Tất cả xe đúng lịch bảo dưỡng OEM',
      kpiVehiclesDue: '{{count}} xe cần đặt lịch bảo dưỡng',
      kpiUpcoming: 'Lịch hẹn đang chờ',
      kpiUpcomingNone: 'Chưa có lịch hẹn xưởng nào đang mở',
      kpiNextSlot: 'Lịch tới: {{date}} • {{window}}',
      tierBannerEyebrow: 'THẺ HỘI VIÊN ĐIỆN TỬ',
      tierBannerDesc: 'Hệ số tích điểm tự động áp dụng cho mọi đơn mua xe và hóa đơn xưởng.',
      openLoyaltyCta: 'Ví điểm & Voucher',
      garageCardTitle: 'Xe trong Garage VIN',
      manageGarage: 'Quản lý Garage',
      recentBookingsTitle: 'Lịch hẹn bảo dưỡng gần đây',
      viewAllBookings: 'Xem tất cả',
      loadErrorTitle: 'Không thể tải dữ liệu tổng quan',
      loadErrorDetail: 'Không thể kết nối máy chủ API. Vui lòng kiểm tra mạng và thử lại.'
    }
  };

  var EN_PATCH = {
    customerDash: {
      eyebrow: 'CUSTOMER PORTAL • OVERVIEW',
      welcomeBack: 'Welcome Back',
      welcomeNamed: 'Welcome Back, {{name}}',
      subtitle: 'Live telemetry of your ISO 3779 VIN garage, upcoming workshop appointments, and membership tier benefits.',
      refreshing: 'Refreshing...',
      openGarage: 'Open VIN Garage',
      bookService: 'Book Workshop Service',
      overdueBannerTitle: 'OVERDUE MAINTENANCE:',
      dueSoonBannerTitle: 'MAINTENANCE DUE SOON:',
      dueBannerDetail: '{{model}} (VIN {{vin}}) is at {{currentKm}} km — target milestone {{targetKm}} km',
      bookNow: 'Book Now',
      kpiAvailablePoints: 'Available Points',
      kpiAvailableMeta: 'Redeemable for workshop & showroom vouchers',
      kpiLifetimePoints: 'Lifetime Qualifying Points',
      kpiLifetimeMeta: 'Never decreases on voucher redemption',
      kpiVehicles: 'VIN Garage Fleet',
      kpiVehiclesAllGood: 'All motorcycles on OEM schedule',
      kpiVehiclesDue: '{{count}} motorcycle(s) due for service',
      kpiUpcoming: 'Active Appointments',
      kpiUpcomingNone: 'Reserve a concurrency-protected bay anytime',
      kpiNextSlot: 'Next: {{date}} • {{window}}',
      tierBannerEyebrow: 'DIGITAL MEMBERSHIP PASSBOOK',
      tierBannerDesc: 'Automatic earn multiplier applied to every showroom sale and workshop invoice.',
      openLoyaltyCta: 'Loyalty & Vouchers',
      garageCardTitle: 'Registered Garage Motorcycles',
      manageGarage: 'Manage Garage',
      recentBookingsTitle: 'Recent Workshop Appointments',
      viewAllBookings: 'View All',
      loadErrorTitle: 'Unable to Load Dashboard Data',
      loadErrorDetail: 'Could not reach the backend API. Please verify connectivity and retry.'
    }
  };

  registerCustomerDashboardI18n.$inject = ['I18nService'];
  function registerCustomerDashboardI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerDashboardI18n)
    .component('customerDashboard', {
      controllerAs: '$ctrl',
      controller: CustomerDashboardController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="gauge" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerDash.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">' +
                '{{ $ctrl.user.fullName ? (\'customerDash.welcomeNamed\' | t:{ name: $ctrl.user.fullName }) : (\'customerDash.welcomeBack\' | t) }}' +
              '</h1>' +
              '<p class="page-subtitle">{{ \'customerDash.subtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.refreshOverview()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerDash.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<a class="btn btn-secondary" ui-sref="app.customer.garage">' +
                '<app-icon name="motorcycle" weight="regular" size="16"></app-icon>' +
                '<span>{{ \'customerDash.openGarage\' | t }}</span>' +
              '</a>' +
              '<a class="btn-pill-cta" ui-sref="app.customer.bookingNew">' +
                '<span>{{ \'customerDash.bookService\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.loadProblem ? ($ctrl.loadProblem.title + \': \' + $ctrl.loadProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.loadProblem && $ctrl.loadProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.refreshOverview()">' +
          '</error-retry-banner>' +

          '<div class="alert-banner alert-warning fade-slide-up" role="alert" ' +
               'ng-repeat="dueBike in $ctrl.dueVehicles track by dueBike.id" ' +
               'style="display:flex;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:flex-start;gap:10px;flex:1;min-width:220px;">' +
              '<app-icon name="warning" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ dueBike.maintenanceStatus === \'Overdue\' ? (\'customerDash.overdueBannerTitle\' | t) : (\'customerDash.dueSoonBannerTitle\' | t) }}</strong> ' +
                '<span>{{ dueBike.motorcycleModelName }}</span> ' +
                '(<span class="mono-num">{{ dueBike.vin }}</span>) • ' +
                '<strong class="mono-num">{{ dueBike.currentMileageKm | number }} km</strong> / ' +
                '<strong class="mono-num">{{ dueBike.nextDueMileageKm | number }} km</strong>' +
                '<span ng-if="dueBike.nextMilestoneName"> • {{ dueBike.nextMilestoneName }}</span>' +
              '</div>' +
            '</div>' +
            '<button type="button" class="btn btn-primary btn-sm" ng-click="$ctrl.bookVehicleNow(dueBike)">' +
              '<app-icon name="wrench" weight="regular" size="14"></app-icon>' +
              '<span>{{ \'customerDash.bookNow\' | t }}</span>' +
              '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
            '</button>' +
          '</div>' +

          /* Initial Skeleton Shimmer for Bento KPI Strip */
          '<div ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" style="margin-bottom:24px;">' +
            '<skeleton-loader variant="kpi" count="4"></skeleton-loader>' +
          '</div>' +

          /* Bento 4-Metric KPI Strip with GPU-Friendly kpi-counter */
          '<div class="bento-grid fade-slide-up" ng-if="!$ctrl.isLoading || $ctrl.hasLoadedOnce" style="margin-bottom:24px;">' +
            '<div class="col-3 bento-kpi stagger-item" style="--i:0;">' +
              '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                '<span class="kpi-label">{{ \'customerDash.kpiAvailablePoints\' | t }}</span>' +
                '<app-icon name="coins" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
              '</div>' +
              '<div class="kpi-value mono-num" kpi-counter="$ctrl.availablePoints" kpi-format="points"></div>' +
              '<div class="kpi-meta text-muted">{{ \'customerDash.kpiAvailableMeta\' | t }}</div>' +
            '</div>' +

            '<div class="col-3 bento-kpi stagger-item" style="--i:1;">' +
              '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                '<span class="kpi-label">{{ \'customerDash.kpiLifetimePoints\' | t }}</span>' +
                '<app-icon name="trophy" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
              '</div>' +
              '<div class="kpi-value mono-num" kpi-counter="$ctrl.lifetimePoints" kpi-format="points"></div>' +
              '<div class="kpi-meta text-muted">{{ \'customerDash.kpiLifetimeMeta\' | t }}</div>' +
            '</div>' +

            '<div class="col-3 bento-kpi stagger-item" style="--i:2;">' +
              '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                '<span class="kpi-label">{{ \'customerDash.kpiVehicles\' | t }}</span>' +
                '<app-icon name="motorcycle" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
              '</div>' +
              '<div class="kpi-value mono-num" kpi-counter="$ctrl.vehicleCount" kpi-format="number"></div>' +
              '<div class="kpi-meta text-muted">' +
                '{{ $ctrl.dueVehicles.length > 0 ? (\'customerDash.kpiVehiclesDue\' | t:{ count: $ctrl.dueVehicles.length }) : (\'customerDash.kpiVehiclesAllGood\' | t) }}' +
              '</div>' +
            '</div>' +

            '<div class="col-3 bento-kpi stagger-item" style="--i:3;">' +
              '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                '<span class="kpi-label">{{ \'customerDash.kpiUpcoming\' | t }}</span>' +
                '<app-icon name="calendar-check" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
              '</div>' +
              '<div class="kpi-value mono-num" kpi-counter="$ctrl.upcomingCount" kpi-format="number"></div>' +
              '<div class="kpi-meta text-muted" ng-if="$ctrl.nextActiveBooking">' +
                '<span class="mono-num">{{ \'customerDash.kpiNextSlot\' | t:{ date: $ctrl.nextActiveBooking.slotDate, window: $ctrl.nextActiveBooking.slotWindow } }}</span>' +
              '</div>' +
              '<div class="kpi-meta text-muted" ng-if="!$ctrl.nextActiveBooking">{{ \'customerDash.kpiUpcomingNone\' | t }}</div>' +
            '</div>' +
          '</div>' +

          /* Compact Metallic Tier Banner */
          '<div class="bezel-card fade-slide-up" style="margin-bottom:24px;">' +
            '<div class="passbook-card" ng-class="\'tier-\' + ($ctrl.user.tierCode || \'Bronze\').toLowerCase()" style="padding:var(--space-5) var(--space-6);">' +
              '<div class="passbook-sheen" aria-hidden="true"></div>' +
              '<div style="position:relative;z-index:2;display:flex;align-items:center;justify-content:space-between;gap:16px;flex-wrap:wrap;">' +
                '<div style="display:flex;align-items:center;gap:14px;flex-wrap:wrap;">' +
                  '<div style="width:46px;height:46px;border-radius:var(--radius-full);background:rgba(255,255,255,0.14);border:1px solid rgba(255,255,255,0.28);display:inline-flex;align-items:center;justify-content:center;">' +
                    '<app-icon name="crown" weight="duotone" size="24"></app-icon>' +
                  '</div>' +
                  '<div>' +
                    '<div style="font-size:11px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;opacity:0.84;">' +
                      '{{ \'customerDash.tierBannerEyebrow\' | t }}' +
                    '</div>' +
                    '<div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-top:2px;">' +
                      '<strong style="font-family:var(--font-display);font-size:20px;">' +
                        '{{ ($ctrl.user.tierCode || \'Bronze\') | enumT:\'LoyaltyTierCode\' }}' +
                      '</strong>' +
                      '<span class="mono-num" style="padding:2px 10px;border-radius:var(--radius-full);background:rgba(255,255,255,0.16);border:1px solid rgba(255,255,255,0.28);font-weight:700;">' +
                        '{{ ($ctrl.user.tierMultiplier || 1) | number:2 }}x' +
                      '</span>' +
                    '</div>' +
                    '<div style="font-size:13px;opacity:0.86;margin-top:4px;">{{ \'customerDash.tierBannerDesc\' | t }}</div>' +
                  '</div>' +
                '</div>' +
                '<a class="btn-pill-cta btn-pill-secondary" ui-sref="app.customer.loyalty">' +
                  '<span>{{ \'customerDash.openLoyaltyCta\' | t }}</span>' +
                  '<span class="btn-pill-well" aria-hidden="true">' +
                    '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                  '</span>' +
                '</a>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Main Split: Garage Preview Cards + Recent Bookings */
          '<div class="grid-12">' +
            '<div class="col-6">' +
              '<div class="bezel-card" style="height:100%;">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="motorcycle" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerDash.garageCardTitle\' | t }}</h2>' +
                    '</div>' +
                    '<a class="btn btn-secondary btn-sm" ui-sref="app.customer.garage">' +
                      '<span>{{ \'customerDash.manageGarage\' | t }}</span>' +
                      '<app-icon name="caret-right" weight="regular" size="14"></app-icon>' +
                    '</a>' +
                  '</div>' +

                  '<skeleton-loader ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" variant="drawer" count="2"></skeleton-loader>' +

                  '<empty-state ng-if="(!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.vehicles.length === 0" ' +
                               'icon="motorcycle" ' +
                               'title-key="booking.emptyGarageTitle" ' +
                               'desc-key="booking.emptyGarageDesc" ' +
                               'action-key="booking.registerVinCta" ' +
                               'on-action="$ctrl.goToGarage()">' +
                  '</empty-state>' +

                  '<div ng-if="$ctrl.vehicles.length > 0" style="display:flex;flex-direction:column;gap:12px;">' +
                    '<div class="surface-sunken stagger-item" ' +
                         'ng-repeat="bike in $ctrl.vehicles track by bike.id" ' +
                         'ng-attr-style="--i:{{ $index }};padding:12px;display:flex;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap;">' +
                      '<div style="display:flex;align-items:center;gap:12px;min-width:220px;flex:1;">' +
                        '<div style="width:92px;flex-shrink:0;">' +
                          '<bike-image src="bike.imageUrl" alt="{{ bike.motorcycleModelName }}" variant="thumb"></bike-image>' +
                        '</div>' +
                        '<div style="min-width:0;">' +
                          '<div class="text-primary" style="font-weight:700;font-size:14px;">{{ bike.motorcycleModelName }}</div>' +
                          '<div class="mono-num text-secondary" style="font-size:12px;margin-top:2px;">{{ bike.vin }}</div>' +
                          '<div class="text-muted" style="font-size:12px;margin-top:2px;">' +
                            '<span ng-if="bike.licensePlate" class="mono-num">{{ bike.licensePlate }} • </span>' +
                            '<span class="mono-num">{{ bike.currentMileageKm | number }} km</span>' +
                          '</div>' +
                        '</div>' +
                      '</div>' +
                      '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                        '<span class="badge" ng-class="bike.maintenanceStatus === \'Overdue\' ? \'badge-danger\' : (bike.maintenanceStatus === \'DueSoon\' ? \'badge-warning\' : \'badge-success\')">' +
                          '<app-icon name="{{ bike.maintenanceStatus === \'Overdue\' ? \'warning-circle\' : (bike.maintenanceStatus === \'DueSoon\' ? \'warning\' : \'check-circle\') }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ bike.maintenanceStatus | enumT:\'MaintenanceStatus\' }}</span>' +
                        '</span>' +
                        '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.bookVehicleNow(bike)">' +
                          '<app-icon name="wrench" weight="regular" size="14"></app-icon>' +
                          '<span>{{ \'customerDash.bookNow\' | t }}</span>' +
                        '</button>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +

            '<div class="col-6">' +
              '<div class="bezel-card" style="height:100%;">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="calendar-check" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerDash.recentBookingsTitle\' | t }}</h2>' +
                    '</div>' +
                    '<a class="btn btn-secondary btn-sm" ui-sref="app.customer.bookings">' +
                      '<span>{{ \'customerDash.viewAllBookings\' | t }}</span>' +
                      '<app-icon name="caret-right" weight="regular" size="14"></app-icon>' +
                    '</a>' +
                  '</div>' +

                  '<skeleton-loader ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" variant="drawer" count="2"></skeleton-loader>' +

                  '<empty-state ng-if="(!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.bookings.length === 0" ' +
                               'icon="calendar-blank" ' +
                               'title-key="booking.emptyBookingsTitle" ' +
                               'desc-key="booking.emptyBookingsDesc" ' +
                               'action-key="customerDash.bookService" ' +
                               'on-action="$ctrl.bookVehicleNow(null)">' +
                  '</empty-state>' +

                  '<div ng-if="$ctrl.bookings.length > 0" style="display:flex;flex-direction:column;gap:10px;">' +
                    '<div class="surface-sunken stagger-item" ' +
                         'ng-repeat="bk in $ctrl.bookings | limitTo:4 track by bk.id" ' +
                         'ng-attr-style="--i:{{ $index }};padding:12px;display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;">' +
                      '<div>' +
                        '<div class="mono-num text-primary" style="font-weight:700;">{{ bk.bookingNumber }}</div>' +
                        '<div class="text-secondary" style="font-size:13px;margin-top:2px;">' +
                          '{{ bk.motorcycleModelName || bk.dealerName }}' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:12px;margin-top:2px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                          '<app-icon name="map-pin" weight="regular" size="13"></app-icon>' +
                          '<span>{{ bk.dealerName }}</span>' +
                          '<span aria-hidden="true">•</span>' +
                          '<span class="mono-num">{{ bk.slotDate }} ({{ bk.slotWindow }})</span>' +
                        '</div>' +
                      '</div>' +
                      '<span class="badge" ng-class="\'badge-status-\' + bk.status.toLowerCase()">' +
                        '{{ bk.status | enumT:\'BookingStatus\' }}' +
                      '</span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  CustomerDashboardController.$inject = [
    '$q',
    '$rootScope',
    '$state',
    'AuthService',
    'DealerApiService',
    'SignalRClientService',
    'TokenStorageService',
    'I18nService'
  ];
  function CustomerDashboardController(
    $q,
    $rootScope,
    $state,
    AuthService,
    DealerApiService,
    SignalRClientService,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback !== undefined ? fallback : key;
    }

    function syncUser() {
      $ctrl.user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.availablePoints = Math.max(0, Math.floor(Number($ctrl.user.availablePoints) || 0));
      $ctrl.lifetimePoints = Math.max(0, Math.floor(Number($ctrl.user.lifetimeQualifyingPoints) || 0));
    }

    function recomputeDerivedState() {
      var bikes = $ctrl.vehicles || [];
      $ctrl.vehicleCount = bikes.length;
      $ctrl.dueVehicles = bikes.filter(function (b) {
        return b.maintenanceStatus === 'Overdue' || b.maintenanceStatus === 'DueSoon';
      });

      var activeStatuses = { Pending: true, Confirmed: true, CheckedIn: true, InProgress: true };
      var activeList = ($ctrl.bookings || []).filter(function (bk) {
        return Boolean(activeStatuses[bk.status]);
      });
      $ctrl.upcomingCount = activeList.length;
      $ctrl.nextActiveBooking = activeList.length > 0 ? activeList[0] : null;
    }

    $ctrl.$onInit = function () {
      syncUser();
      $ctrl.isLoading = false;
      $ctrl.hasLoadedOnce = false;
      $ctrl.loadProblem = null;
      $ctrl.vehicles = [];
      $ctrl.bookings = [];
      recomputeDerivedState();

      cleanupFns.push($rootScope.$on('auth:changed', function () {
        syncUser();
      }));

      cleanupFns.push(SignalRClientService.on('MaintenanceReminderTriggered', function (payload) {
        if (!payload || !payload.vin) {
          return;
        }
        var targetVin = String(payload.vin).toUpperCase();
        var matched = false;
        for (var i = 0; i < $ctrl.vehicles.length; i++) {
          if ($ctrl.vehicles[i].vin === targetVin) {
            $ctrl.vehicles[i].maintenanceStatus = 'DueSoon';
            $ctrl.vehicles[i].isDueSoonOrOverdue = true;
            if (payload.targetMileageKm) {
              $ctrl.vehicles[i].nextDueMileageKm = Number(payload.targetMileageKm);
            }
            if (payload.milestoneName) {
              $ctrl.vehicles[i].nextMilestoneName = payload.milestoneName;
            }
            matched = true;
          }
        }
        if (!matched) {
          $ctrl.refreshOverview();
          return;
        }
        recomputeDerivedState();
      }));

      cleanupFns.push(SignalRClientService.on('BookingStatusChanged', function (payload) {
        if (!payload) {
          return;
        }
        for (var i = 0; i < $ctrl.bookings.length; i++) {
          if (
            $ctrl.bookings[i].id === payload.bookingId ||
            $ctrl.bookings[i].bookingNumber === payload.bookingNumber
          ) {
            $ctrl.bookings[i].status = payload.newStatus || $ctrl.bookings[i].status;
          }
        }
        recomputeDerivedState();
      }));

      cleanupFns.push(SignalRClientService.on('LoyaltyPointsUpdated', function () {
        AuthService.fetchCurrentUser().catch(angular.noop);
      }));

      cleanupFns.push(SignalRClientService.on('TierUpgraded', function () {
        AuthService.fetchCurrentUser().catch(angular.noop);
      }));

      $ctrl.refreshOverview();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.refreshOverview = function () {
      $ctrl.isLoading = true;
      $ctrl.loadProblem = null;
      var firstError = null;

      function captureError(rejection) {
        if (!firstError && rejection) {
          var payload = (rejection && rejection.data) || rejection || {};
          firstError = {
            status: Number(rejection.status || payload.status) || 0,
            title: payload.title || t('customerDash.loadErrorTitle', null, 'Unable to Load Dashboard Data'),
            detail: payload.detail || t('customerDash.loadErrorDetail', null, 'Could not reach the backend API. Please verify connectivity and retry.'),
            traceId: payload.traceId || null
          };
        }
        return null;
      }

      return $q.all({
        me: AuthService.fetchCurrentUser().catch(function () { return null; }),
        vehicles: DealerApiService.listVehicles().catch(captureError),
        bookings: DealerApiService.listBookings({ pageSize: 10 }).catch(captureError)
      }).then(function (bundle) {
        if (isDestroyed) {
          return;
        }
        syncUser();
        if (angular.isArray(bundle.vehicles)) {
          $ctrl.vehicles = bundle.vehicles;
        }
        if (angular.isArray(bundle.bookings)) {
          $ctrl.bookings = bundle.bookings;
        }
        if (firstError) {
          $ctrl.loadProblem = firstError;
        }
        $ctrl.hasLoadedOnce = true;
        recomputeDerivedState();
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.goToGarage = function () {
      if ($state && angular.isFunction($state.go)) {
        $state.go('app.customer.garage');
      }
    };

    $ctrl.bookVehicleNow = function (vehicle) {
      var params = vehicle && vehicle.id ? { vehicleId: vehicle.id } : {};
      $state.go('app.customer.bookingNew', params);
    };
  }
})();
