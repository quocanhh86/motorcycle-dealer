(function () {
  'use strict';

  var VI_PATCH = {
    customerGarage: {
      eyebrow: 'GARAGE SỐ KHUNG VIN ĐIỆN TỬ • ISO 3779',
      title: 'Garage Xe máy của tôi ({{count}})',
      subtitle: 'Quản lý xe mua tại Showroom và xe đăng ký số khung VIN 17 ký tự, cập nhật công-tơ-mét tăng tiến và theo dõi mốc bảo dưỡng OEM.',
      refreshing: 'Đang làm mới...',
      scheduleService: 'Đặt lịch bảo dưỡng',
      liveAlertTitle: 'Cảnh báo nhắc lịch bảo dưỡng VIN trực tiếp:',
      liveAlertDetail: 'mốc {{milestone}} cho VIN {{vin}} (Mục tiêu: {{targetKm}} km).',
      bookService: 'Đặt lịch xưởng',
      verifiedShowroom: 'Xe mua Showroom',
      externalVin: 'Đăng ký VIN ngoài',
      warrantyCoveredChip: 'Còn bảo hành OEM',
      warrantyExpiredChip: 'Hết bảo hành OEM',
      currentOdometer: 'Số ODO hiện tại',
      nextDue: 'Mốc kế tiếp',
      lastService: 'Bảo dưỡng gần nhất',
      warrantyLeft: 'Bảo hành còn lại',
      wearProgress: 'Mức hao mòn chu kỳ',
      wearAriaLabel: 'Tiến độ chu kỳ bảo dưỡng',
      remindersScheduleCta: 'Nhắc lịch & Lịch OEM',
      warrantyClaimCta: 'Yêu cầu bảo hành',
      dueSoonKmLeft: 'Sắp đến hạn (còn {{km}} km)',
      regModalEyebrow: 'ISO 3779 • SỐ KHUNG 17 KÝ TỰ',
      regModalTitle: 'Đăng ký xe máy ngoài hệ thống bằng số VIN',
      regVinLabel: 'Số khung VIN 17 ký tự * (Không gồm I, O, Q)',
      regVinPlaceholder: 'Nhập đúng 17 ký tự số khung viết hoa',
      regVinForbiddenErr: 'Số khung chuẩn ISO 3779 không bao giờ chứa ký tự I, O hoặc Q.',
      regVinLengthErr: 'Số khung VIN phải gồm đúng 17 ký tự chữ in hoa và số.',
      regModelLabel: 'Mẫu xe máy *',
      regModelPlaceholder: '-- Chọn mẫu xe máy --',
      regOdoLabel: 'Số ODO hiện tại (km) *',
      regPlateLabel: 'Biển số xe',
      regPlatePlaceholder: 'Nhập biển số xe',
      regColorLabel: 'Màu sắc ngoại thất',
      regColorPlaceholder: 'Nhập tên màu xe',
      regHint: 'Xe đăng ký ngoài không cộng điểm mua xe showroom nhưng kích hoạt đầy đủ nhắc lịch bảo dưỡng tự động theo VIN và tích điểm theo hạng trên mọi hóa đơn xưởng.',
      regSaving: 'Đang đăng ký VIN...',
      regSubmit: 'Đăng ký số khung VIN',
      regToastTitle: 'Đã đăng ký xe bằng số VIN',
      regToastBody: '{{model}} ({{vin}}) đã được thêm vào Garage của bạn.',
      odoModalEyebrow: 'CẬP NHẬT CÔNG-TƠ-MÉT TĂNG TIẾN',
      odoModalTitle: 'Cập nhật số ODO hiện tại',
      odoRecordedLabel: 'Số ODO đang lưu trên hệ thống',
      odoNewLabel: 'Số ODO mới (km) *',
      odoMonotonicWarn: 'Nguyên tắc tăng tiến yêu cầu >= {{minKm}} km (giá trị nhỏ hơn sẽ bị từ chối với mã lỗi ODOMETER_REGRESSION).',
      odoSaving: 'Đang cập nhật...',
      odoSubmit: 'Lưu số ODO mới',
      odoToastTitle: 'Đã cập nhật công-tơ-mét',
      odoToastBody: 'Số khung VIN {{vin}} đã cập nhật thành {{km}} km.',
      drawerTitleSuffix: 'Nhắc lịch & Quy chuẩn OEM',
      drawerRemindersHeading: 'Cảnh báo nhắc lịch tự động ({{count}})',
      drawerNoRemindersTitle: 'Chưa có cảnh báo nhắc lịch nào',
      drawerNoRemindersDesc: 'Hệ thống sẽ tự động thông báo khi xe sắp đạt mốc bảo dưỡng định kỳ.',
      drawerTargetKm: 'Mốc mục tiêu',
      drawerDueDate: 'Hạn dự kiến',
      drawerDedupKey: 'Mã chống trùng',
      drawerRulesHeading: 'Lịch bảo dưỡng định kỳ chuẩn OEM ({{count}})',
      drawerMonthsShort: 'tháng',
      kpiTotalBikes: 'Tổng xe trong Garage',
      kpiOnTrack: 'Đúng lịch OEM',
      kpiNeedAttention: 'Cần bảo dưỡng',
      kpiWarrantyCovered: 'Còn bảo hành OEM',
      angleFront: '3/4 Trước',
      angleSide: 'Thân xe',
      angleRear: 'Đuôi LED',
      angleCockpit: 'Đồng hồ',
      inspect3dCta: 'HD 360°',
      drawerModePhotos: 'Ảnh Thật 4 Góc HD',
      drawerMode3d: 'Soi HD 360° Xe Thật & Linh Kiện',
      drawerMediaModeAria: 'Chế độ xem hình ảnh xe trong Garage'
    }
  };

  var EN_PATCH = {
    customerGarage: {
      eyebrow: 'DIGITAL VIN GARAGE • ISO 3779',
      title: 'My Digital Motorcycle VIN Garage ({{count}})',
      subtitle: 'Manage showroom-onboarded and external ISO 3779 17-character VINs, update monotonic odometer readings, and track OEM reminders.',
      refreshing: 'Refreshing...',
      scheduleService: 'Schedule Workshop Slot',
      liveAlertTitle: 'Live VIN Maintenance Alert:',
      liveAlertDetail: '{{milestone}} due for VIN {{vin}} (Target: {{targetKm}} km).',
      bookService: 'Book Service',
      verifiedShowroom: 'Verified Showroom',
      externalVin: 'External VIN',
      warrantyCoveredChip: 'OEM Covered',
      warrantyExpiredChip: 'Out of Warranty',
      currentOdometer: 'Current Odometer',
      nextDue: 'Next Due',
      lastService: 'Last Service',
      warrantyLeft: 'OEM Warranty Left',
      wearProgress: 'Interval Wear',
      wearAriaLabel: 'Maintenance interval wear progress',
      remindersScheduleCta: 'Reminders & Schedule',
      warrantyClaimCta: 'Warranty Claim',
      dueSoonKmLeft: 'Due Soon ({{km}} km left)',
      regModalEyebrow: 'ISO 3779 • 17-CHARACTER VIN',
      regModalTitle: 'Register External Motorcycle by VIN',
      regVinLabel: '17-Character ISO 3779 VIN * (Excludes I, O, Q)',
      regVinPlaceholder: 'Enter 17-character ISO 3779 VIN',
      regVinForbiddenErr: 'ISO 3779 VINs never contain letters I, O, or Q.',
      regVinLengthErr: 'VIN must be exactly 17 uppercase alphanumeric characters.',
      regModelLabel: 'Motorcycle Model *',
      regModelPlaceholder: '-- Select Motorcycle Model --',
      regOdoLabel: 'Current Odometer (km) *',
      regPlateLabel: 'License Plate',
      regPlatePlaceholder: 'Enter license plate',
      regColorLabel: 'Color Name',
      regColorPlaceholder: 'Enter exterior color',
      regHint: 'External bikes earn 0 showroom purchase points, but unlock automated VIN maintenance reminders and earn full tiered loyalty points on every workshop invoice.',
      regSaving: 'Registering VIN...',
      regSubmit: 'Register Bike VIN',
      regToastTitle: 'External VIN Registered',
      regToastBody: '{{model}} ({{vin}}) added to your garage.',
      odoModalEyebrow: 'MONOTONIC ODOMETER TELEMETRY',
      odoModalTitle: 'Update Monotonic Odometer Reading',
      odoRecordedLabel: 'Recorded Odometer',
      odoNewLabel: 'New Current Odometer (km) *',
      odoMonotonicWarn: 'Monotonic invariant requires >= {{minKm}} km (lower values are rejected with ODOMETER_REGRESSION).',
      odoSaving: 'Updating...',
      odoSubmit: 'Save Odometer Reading',
      odoToastTitle: 'Odometer Updated',
      odoToastBody: 'VIN {{vin}} updated to {{km}} km.',
      drawerTitleSuffix: 'Reminders & OEM Schedule',
      drawerRemindersHeading: 'Automated Maintenance Reminders ({{count}})',
      drawerNoRemindersTitle: 'No active reminder alerts yet',
      drawerNoRemindersDesc: 'Automated reminders will trigger as your motorcycle approaches its next OEM milestone.',
      drawerTargetKm: 'Target',
      drawerDueDate: 'Due Date',
      drawerDedupKey: 'Dedup Key',
      drawerRulesHeading: 'OEM Model Maintenance Rules ({{count}})',
      drawerMonthsShort: 'mo',
      kpiTotalBikes: 'Total Garage VINs',
      kpiOnTrack: 'On OEM Schedule',
      kpiNeedAttention: 'Service Due / Overdue',
      kpiWarrantyCovered: 'Active OEM Warranty',
      angleFront: 'Front 3/4',
      angleSide: 'Side',
      angleRear: 'Rear LED',
      angleCockpit: 'Cockpit',
      inspect3dCta: 'HD 360°',
      drawerModePhotos: '4-Angle Real HD Photos',
      drawerMode3d: 'Interactive Real HD 360°',
      drawerMediaModeAria: 'Garage inspection media mode'
    }
  };

  var GARAGE_MULTI_ANGLE_SLUGS = {
    'honda-airblade-160': true,
    'honda-sh160i-sport': true,
    'honda-winner-x': true,
    'yamaha-exciter-155': true,
    'honda-cb300r': true,
    'honda-nx500': true
  };

  var GARAGE_CODE_TO_SLUG = {
    'HND-AB160-2026': 'honda-airblade-160',
    'HND-SH160I-2026': 'honda-sh160i-sport',
    'HND-WINX-2026': 'honda-winner-x',
    'YMH-EXC155-2026': 'yamaha-exciter-155',
    'HND-CB300R-2026': 'honda-cb300r',
    'HND-NX500-2026': 'honda-nx500'
  };

  var GARAGE_ANGLE_DEFS = [
    { key: 'front', suffix: '', labelKey: 'customerGarage.angleFront', fallback: 'Front 3/4' },
    { key: 'side', suffix: '-side', labelKey: 'customerGarage.angleSide', fallback: 'Side' },
    { key: 'rear', suffix: '-rear', labelKey: 'customerGarage.angleRear', fallback: 'Rear LED' },
    { key: 'cockpit', suffix: '-cockpit', labelKey: 'customerGarage.angleCockpit', fallback: 'Cockpit' }
  ];

  registerCustomerGarageI18n.$inject = ['I18nService'];
  function registerCustomerGarageI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerGarageI18n)
    .component('customerGarage', {
      controllerAs: '$ctrl',
      controller: CustomerGarageController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="motorcycle" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerGarage.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">{{ \'customerGarage.title\' | t:{ count: $ctrl.vehicles.length } }}</h1>' +
              '<p class="page-subtitle">{{ \'customerGarage.subtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadGarage()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerGarage.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<button type="button" class="btn btn-secondary" ' +
                      'has-permission="Permissions.Vehicles.RegisterOwn" ' +
                      'ng-click="$ctrl.openRegisterModal()">' +
                '<app-icon name="plus" weight="regular" size="16"></app-icon>' +
                '<span>{{ \'booking.registerVinCta\' | t }}</span>' +
              '</button>' +
              '<a class="btn-pill-cta" ui-sref="app.customer.bookingNew">' +
                '<span>{{ \'customerGarage.scheduleService\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadGarage()">' +
          '</error-retry-banner>' +

          '<div class="alert-banner alert-warning fade-slide-up" role="alert" ng-if="$ctrl.liveReminderBanner" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;">' +
              '<app-icon name="bell-ringing" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerGarage.liveAlertTitle\' | t }}</strong> ' +
                '<span>{{ $ctrl.liveReminderBanner.milestoneName }}</span> • ' +
                '<span class="mono-num">{{ $ctrl.liveReminderBanner.vin }}</span> ' +
                '(<span class="mono-num">{{ $ctrl.liveReminderBanner.targetMileageKm | number }} km</span>)' +
              '</div>' +
            '</div>' +
            '<a class="btn btn-primary btn-sm" ui-sref="app.customer.bookingNew({ vehicleId: $ctrl.liveReminderBanner.customerVehicleId || undefined })">' +
              '<span>{{ \'customerGarage.bookService\' | t }}</span>' +
              '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
            '</a>' +
          '</div>' +

          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.vehicles.length === 0" variant="cards" count="3"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.vehicles.length === 0" ' +
                       'icon="motorcycle" ' +
                       'title-key="booking.emptyGarageTitle" ' +
                       'desc-key="booking.emptyGarageDesc" ' +
                       'action-key="booking.registerVinCta" ' +
                       'on-action="$ctrl.openRegisterModal()">' +
          '</empty-state>' +

          /* Fleet Health Telemetry Bento Strip */
          '<div class="bento-grid fade-slide-up" ng-if="$ctrl.vehicles.length > 0" style="margin-bottom:20px;">' +
            '<div class="col-3 bento-kpi">' +
              '<span class="kpi-label">{{ \'customerGarage.kpiTotalBikes\' | t }}</span>' +
              '<div class="kpi-value mono-num">{{ $ctrl.fleetStats.total }}</div>' +
            '</div>' +
            '<div class="col-3 bento-kpi">' +
              '<span class="kpi-label">{{ \'customerGarage.kpiOnTrack\' | t }}</span>' +
              '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);">{{ $ctrl.fleetStats.onTrack }}</div>' +
            '</div>' +
            '<div class="col-3 bento-kpi">' +
              '<span class="kpi-label">{{ \'customerGarage.kpiNeedAttention\' | t }}</span>' +
              '<div class="kpi-value mono-num" ng-style="$ctrl.fleetStats.dueCount > 0 ? {\'color\': \'var(--clr-warning-fg)\'} : {}">{{ $ctrl.fleetStats.dueCount }}</div>' +
            '</div>' +
            '<div class="col-3 bento-kpi">' +
              '<span class="kpi-label">{{ \'customerGarage.kpiWarrantyCovered\' | t }}</span>' +
              '<div class="kpi-value mono-num">{{ $ctrl.fleetStats.warrantyCovered }}</div>' +
            '</div>' +
          '</div>' +

          /* Double-Bezel Vehicle Cards Grid */
          '<div class="grid-12" ng-if="$ctrl.vehicles.length > 0">' +
            '<article class="col-6 bezel-card stagger-item" ' +
                     'ng-repeat="bike in $ctrl.vehicles track by bike.id" ' +
                     'ng-attr-style="--i:{{ $index }};">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:14px;">' +
                '<div>' +
                  '<div class="card-gallery-wrap" style="margin-bottom:14px;">' +
                    '<div class="card-gallery-stage">' +
                      '<bike-image src="$ctrl.getVehicleActiveImageUrl(bike)" ' +
                                  'alt="{{ bike.motorcycleModelName }}" ' +
                                  'variant="card" ' +
                                  'badge="{{ bike.licensePlate || bike.modelCode || \'\' }}">' +
                      '</bike-image>' +
                      '<span class="card-gallery-count-pill mono-num" aria-hidden="true" ng-if="$ctrl.getVehicleGallery(bike).length > 1">' +
                        '<app-icon name="images" weight="duotone" size="12"></app-icon>' +
                        '<span>{{ $ctrl.getVehicleAngleIndex(bike) + 1 }}/{{ $ctrl.getVehicleGallery(bike).length }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="card-angle-strip" ng-if="$ctrl.getVehicleGallery(bike).length > 1" role="group" ng-attr-aria-label="{{ bike.motorcycleModelName }}">' +
                      '<button type="button" class="card-angle-pill" ' +
                              'ng-repeat="ang in $ctrl.getVehicleGallery(bike) track by ang.key" ' +
                              'ng-class="{\'is-active\': $ctrl.getVehicleAngleIndex(bike) === $index}" ' +
                              'ng-attr-aria-pressed="{{ $ctrl.getVehicleAngleIndex(bike) === $index ? \'true\' : \'false\' }}" ' +
                              'ng-click="$ctrl.selectVehicleAngle(bike, $index)">' +
                        '{{ ang.labelKey | t }}' +
                      '</button>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;margin-bottom:10px;">' +
                    '<div style="display:flex;gap:6px;align-items:center;flex-wrap:wrap;">' +
                      '<span class="badge" ng-class="bike.registrationSource === \'ShowroomSale\' ? \'badge-info\' : \'badge-neutral\'">' +
                        '<app-icon name="{{ bike.registrationSource === \'ShowroomSale\' ? \'check-circle\' : \'identification-card\' }}" weight="fill" size="13"></app-icon>' +
                        '<span>{{ bike.registrationSource | enumT:\'RegistrationSource\' }}</span>' +
                      '</span>' +
                      '<span class="badge" ng-class="bike.isWarrantyCovered ? \'badge-success\' : \'badge-warning\'">' +
                        '<app-icon name="{{ bike.isWarrantyCovered ? \'shield-check\' : \'warning\' }}" weight="fill" size="13"></app-icon>' +
                        '<span>{{ bike.isWarrantyCovered ? (\'customerGarage.warrantyCoveredChip\' | t) : (\'customerGarage.warrantyExpiredChip\' | t) }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<span class="badge" ng-class="$ctrl.statusBadgeClass(bike.maintenanceStatus)">' +
                      '<app-icon name="{{ bike.maintenanceStatus === \'Overdue\' ? \'warning-circle\' : (bike.maintenanceStatus === \'DueSoon\' ? \'warning\' : \'check-circle\') }}" weight="fill" size="13"></app-icon>' +
                      '<span>{{ $ctrl.statusLabel(bike) }}</span>' +
                    '</span>' +
                  '</div>' +

                  '<h2 class="surface-card-title">{{ bike.motorcycleModelName }}</h2>' +
                  '<div class="text-secondary" style="margin-top:6px;font-size:13px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                    '<span>{{ \'common.vin\' | t }}:</span>' +
                    '<strong class="mono-num text-primary" style="padding:2px 8px;border-radius:var(--radius-sm);background:var(--clr-surface-sunken);border:1px solid var(--clr-border-subtle);">{{ bike.vin }}</strong>' +
                    '<span ng-if="bike.colorName" class="text-muted">• {{ bike.colorName }}</span>' +
                  '</div>' +

                  '<div class="surface-sunken" style="margin-top:14px;padding:12px 14px;">' +
                    '<div style="display:flex;justify-content:space-between;font-size:13px;margin-bottom:8px;flex-wrap:wrap;gap:8px;">' +
                      '<span class="text-secondary">{{ \'customerGarage.currentOdometer\' | t }}: <strong class="mono-num text-primary">{{ bike.currentMileageKm | number }} km</strong></span>' +
                      '<span class="text-secondary">{{ \'customerGarage.nextDue\' | t }}: <strong class="mono-num text-primary">{{ bike.nextDueMileageKm | number }} km</strong>' +
                        '<span ng-if="bike.nextDueDateUtc" class="text-muted"> ({{ bike.nextDueDateUtc | utcToLocal:\'date\' }})</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="progress-track" role="progressbar" aria-valuemin="0" aria-valuemax="100" ng-attr-aria-valuenow="{{ bike.wearProgressPct || 0 }}" ng-attr-aria-label="{{ \'customerGarage.wearAriaLabel\' | t }}">' +
                      '<div class="progress-fill" ' +
                           'ng-class="{\'is-due\': bike.maintenanceStatus === \'DueSoon\', \'is-overdue\': bike.maintenanceStatus === \'Overdue\'}" ' +
                           'style="width:100%;transform-origin:left center;transition:transform var(--dur-emphasis) var(--ease-expo);" ' +
                           'ng-style="{transform: \'scaleX(\' + ((bike.wearProgressPct || 0) / 100) + \')\'}"></div>' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:11px;margin-top:8px;display:flex;justify-content:space-between;flex-wrap:wrap;gap:6px;">' +
                      '<span>{{ \'customerGarage.lastService\' | t }}: <span class="mono-num text-secondary">{{ bike.lastServiceMileageKm | number }} km</span></span>' +
                      '<span>{{ \'customerGarage.warrantyLeft\' | t }}: <span class="mono-num text-secondary">{{ bike.warrantyRemainingKm | number }} km</span></span>' +
                      '<span>{{ \'customerGarage.wearProgress\' | t }}: <span class="mono-num text-secondary">{{ bike.wearProgressPct }}%</span></span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;padding-top:12px;border-top:1px solid var(--clr-border-subtle);">' +
                  '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'has-permission="Permissions.Vehicles.RegisterOwn" ' +
                            'ng-click="$ctrl.openOdometerModal(bike)">' +
                      '<app-icon name="gauge" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'booking.updateOdometerCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.openRemindersDrawer(bike)">' +
                      '<app-icon name="bell" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'customerGarage.remindersScheduleCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.openRemindersDrawer(bike, \'3d\')">' +
                      '<app-icon name="camera" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'customerGarage.inspect3dCta\' | t }}</span>' +
                    '</button>' +
                    '<a class="btn btn-secondary btn-sm" ' +
                       'has-permission="Permissions.Warranty.CreateOwn" ' +
                       'ui-sref="app.customer.warranty({ vehicleId: bike.id })">' +
                      '<app-icon name="shield-check" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'customerGarage.warrantyClaimCta\' | t }}</span>' +
                    '</a>' +
                  '</div>' +
                  '<a class="btn btn-primary btn-sm" ui-sref="app.customer.bookingNew({ vehicleId: bike.id })">' +
                    '<app-icon name="wrench" weight="regular" size="14"></app-icon>' +
                    '<span>{{ \'customerGarage.bookService\' | t }}</span>' +
                    '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                  '</a>' +
                '</div>' +
              '</div>' +
            '</article>' +
          '</div>' +

          /* Modal 1: Register External Bike VIN */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="regVinTitle" ng-if="$ctrl.registerModalOpen">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ \'customerGarage.regModalEyebrow\' | t }}</span>' +
                  '<h2 id="regVinTitle" class="surface-card-title" style="margin-top:4px;">{{ \'customerGarage.regModalTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeRegisterModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.registerForm" ng-submit="$ctrl.submitRegisterVehicle()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.registerProblem">' +
                    '<div><strong>{{ $ctrl.registerProblem.title }}:</strong> {{ $ctrl.registerProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.registerProblem.traceId">traceId: {{ $ctrl.registerProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="extVinField">{{ \'customerGarage.regVinLabel\' | t }}</label>' +
                    '<div class="vin-field-wrapper">' +
                      '<input id="extVinField" name="vin" type="text" class="form-control mono-num" style="flex:1;" ' +
                             'vin-input ng-model="$ctrl.newVehicle.vin" ' +
                             'ng-change="$ctrl.onRegisterFieldEdited(\'vin\')" required ' +
                             'ng-attr-placeholder="{{ \'customerGarage.regVinPlaceholder\' | t }}" />' +
                      '<span class="vin-counter-pill">{{ ($ctrl.newVehicle.vin || \'\').length }}/17</span>' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.registerForm.vin.$dirty && $ctrl.registerForm.vin.$error.noForbiddenVinChars">' +
                      '{{ \'customerGarage.regVinForbiddenErr\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.registerForm.vin.$dirty && !$ctrl.registerForm.vin.$error.noForbiddenVinChars && $ctrl.registerForm.vin.$error.isoVin">' +
                      '{{ \'customerGarage.regVinLengthErr\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.registerFieldErrors.vin">{{ $ctrl.registerFieldErrors.vin }}</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="extModelSelect">{{ \'customerGarage.regModelLabel\' | t }}</label>' +
                    '<select id="extModelSelect" name="motorcycleModelId" class="form-control" ' +
                            'ng-model="$ctrl.newVehicle.motorcycleModelId" ' +
                            'ng-options="m.id as (m.brand + \' \' + m.modelName + \' (\' + m.engineDisplacementCc + \' cc)\') for m in $ctrl.catalogModels" ' +
                            'ng-change="$ctrl.onRegisterFieldEdited(\'motorcycleModelId\')" required>' +
                      '<option value="">{{ \'customerGarage.regModelPlaceholder\' | t }}</option>' +
                    '</select>' +
                    '<div class="field-error" ng-if="$ctrl.registerFieldErrors.motorcycleModelId">{{ $ctrl.registerFieldErrors.motorcycleModelId }}</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="extMileageField">{{ \'customerGarage.regOdoLabel\' | t }}</label>' +
                      '<input id="extMileageField" name="currentMileageKm" type="number" min="0" max="1000000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.newVehicle.currentMileageKm" ' +
                             'ng-change="$ctrl.onRegisterFieldEdited(\'currentMileageKm\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.registerFieldErrors.currentMileageKm">{{ $ctrl.registerFieldErrors.currentMileageKm }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="extPlateField">{{ \'customerGarage.regPlateLabel\' | t }}</label>' +
                      '<input id="extPlateField" name="licensePlate" type="text" maxlength="20" class="form-control mono-num" ' +
                             'ng-model="$ctrl.newVehicle.licensePlate" ' +
                             'ng-change="$ctrl.onRegisterFieldEdited(\'licensePlate\')" ' +
                             'ng-attr-placeholder="{{ \'customerGarage.regPlatePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.registerFieldErrors.licensePlate">{{ $ctrl.registerFieldErrors.licensePlate }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="extColorField">{{ \'customerGarage.regColorLabel\' | t }}</label>' +
                      '<input id="extColorField" name="colorName" type="text" maxlength="60" class="form-control" ' +
                             'ng-model="$ctrl.newVehicle.colorName" ' +
                             'ng-change="$ctrl.onRegisterFieldEdited(\'colorName\')" ' +
                             'ng-attr-placeholder="{{ \'customerGarage.regColorPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.registerFieldErrors.colorName">{{ $ctrl.registerFieldErrors.colorName }}</div>' +
                    '</div>' +
                  '</div>' +

                  '<div class="field-hint">{{ \'customerGarage.regHint\' | t }}</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeRegisterModal()" ng-disabled="$ctrl.isSavingRegister">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'ng-disabled="$ctrl.isSavingRegister || $ctrl.registerForm.$invalid">' +
                    '<app-icon name="plus" weight="regular" size="15"></app-icon>' +
                    '<span>{{ $ctrl.isSavingRegister ? (\'customerGarage.regSaving\' | t) : (\'customerGarage.regSubmit\' | t) }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Modal 2: Update Odometer */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="odoModalTitle" ng-if="$ctrl.odometerVehicle">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ \'customerGarage.odoModalEyebrow\' | t }}</span>' +
                  '<h2 id="odoModalTitle" class="surface-card-title" style="margin-top:4px;">{{ \'customerGarage.odoModalTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeOdometerModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.odometerForm" ng-submit="$ctrl.submitOdometerUpdate()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.odometerProblem">' +
                    '<div><strong>{{ $ctrl.odometerProblem.title }} ({{ $ctrl.odometerProblem.errorCode }}):</strong> {{ $ctrl.odometerProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.odometerProblem.traceId">traceId: {{ $ctrl.odometerProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:16px;font-size:13px;display:flex;align-items:center;gap:12px;">' +
                    '<div style="width:76px;flex-shrink:0;">' +
                      '<bike-image src="$ctrl.odometerVehicle.imageUrl" alt="{{ $ctrl.odometerVehicle.motorcycleModelName }}" variant="thumb"></bike-image>' +
                    '</div>' +
                    '<div>' +
                      '<div class="text-primary"><strong>{{ $ctrl.odometerVehicle.motorcycleModelName }}</strong> • <span class="mono-num">{{ $ctrl.odometerVehicle.vin }}</span></div>' +
                      '<div class="text-secondary" style="margin-top:4px;">{{ \'customerGarage.odoRecordedLabel\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.odometerVehicle.currentMileageKm | number }} km</strong></div>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="newMileageField">{{ \'customerGarage.odoNewLabel\' | t }}</label>' +
                    '<input id="newMileageField" name="currentMileageKm" type="number" min="0" max="1000000" class="form-control mono-num" ' +
                           'ng-model="$ctrl.odometerDraftKm" ' +
                           'ng-change="$ctrl.onOdometerEdited()" required />' +
                    '<div class="field-hint" ng-if="$ctrl.odometerDraftKm < $ctrl.odometerVehicle.currentMileageKm" style="color:var(--clr-warning-fg);">' +
                      '{{ \'customerGarage.odoMonotonicWarn\' | t:{ minKm: ($ctrl.odometerVehicle.currentMileageKm | number) } }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.odometerFieldErrors.currentMileageKm">{{ $ctrl.odometerFieldErrors.currentMileageKm }}</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeOdometerModal()" ng-disabled="$ctrl.isSavingOdometer">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.isSavingOdometer || $ctrl.odometerDraftKm === null || $ctrl.odometerDraftKm === undefined">' +
                    '<app-icon name="check" weight="regular" size="15"></app-icon>' +
                    '<span>{{ $ctrl.isSavingOdometer ? (\'customerGarage.odoSaving\' | t) : (\'customerGarage.odoSubmit\' | t) }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Slide-Over Drawer: Maintenance Reminders & OEM Schedule */
          '<div class="drawer-backdrop" role="dialog" aria-modal="true" aria-labelledby="remDrawerTitle" ng-if="$ctrl.drawerVehicle">' +
            '<aside class="drawer-panel-wide">' +
              '<div class="notification-drawer-header">' +
                '<div>' +
                  '<div class="mono-num" style="font-size:11px;opacity:0.78;">{{ $ctrl.drawerVehicle.vin }}</div>' +
                  '<strong id="remDrawerTitle" style="font-size:15px;">{{ $ctrl.drawerVehicle.motorcycleModelName }} — {{ \'customerGarage.drawerTitleSuffix\' | t }}</strong>' +
                '</div>' +
                '<button type="button" class="btn btn-ghost-dark btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeRemindersDrawer()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="notification-drawer-list">' +
                /* Studio Showcase Header inside Detail Drawer (4-Angle Photos OR Interactive 3D 360°) */
                '<div class="surface-sunken fade-slide-up" style="padding:12px;margin-bottom:4px;">' +
                  '<div class="moto3d-mode-switch" role="group" ng-attr-aria-label="{{ \'customerGarage.drawerMediaModeAria\' | t }}">' +
                    '<button type="button" class="moto3d-mode-btn" ng-class="{\'is-active\': $ctrl.drawerMediaMode !== \'3d\'}" ng-attr-aria-pressed="{{ $ctrl.drawerMediaMode !== \'3d\' ? \'true\' : \'false\' }}" ng-click="$ctrl.setDrawerMediaMode(\'photos\')">' +
                      '<app-icon name="images" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'customerGarage.drawerModePhotos\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="moto3d-mode-btn" ng-class="{\'is-active\': $ctrl.drawerMediaMode === \'3d\'}" ng-attr-aria-pressed="{{ $ctrl.drawerMediaMode === \'3d\' ? \'true\' : \'false\' }}" ng-click="$ctrl.setDrawerMediaMode(\'3d\')">' +
                      '<app-icon name="camera" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'customerGarage.drawerMode3d\' | t }}</span>' +
                    '</button>' +
                  '</div>' +
                  '<div ng-if="$ctrl.drawerMediaMode === \'3d\'">' +
                    '<moto-3d-viewer model="$ctrl.drawerVehicle"></moto-3d-viewer>' +
                  '</div>' +
                  '<div ng-if="$ctrl.drawerMediaMode !== \'3d\'">' +
                    '<bike-image src="$ctrl.getVehicleActiveImageUrl($ctrl.drawerVehicle)" ' +
                                'alt="{{ $ctrl.drawerVehicle.motorcycleModelName }}" ' +
                                'variant="modal" ' +
                                'badge="{{ $ctrl.drawerVehicle.licensePlate || $ctrl.drawerVehicle.modelCode || \'\' }}">' +
                    '</bike-image>' +
                    '<div class="card-angle-strip" style="margin-top:8px;" ng-if="$ctrl.getVehicleGallery($ctrl.drawerVehicle).length > 1" role="group" ng-attr-aria-label="{{ $ctrl.drawerVehicle.motorcycleModelName }}">' +
                      '<button type="button" class="card-angle-pill" ' +
                              'ng-repeat="ang in $ctrl.getVehicleGallery($ctrl.drawerVehicle) track by ang.key" ' +
                              'ng-class="{\'is-active\': $ctrl.getVehicleAngleIndex($ctrl.drawerVehicle) === $index}" ' +
                              'ng-attr-aria-pressed="{{ $ctrl.getVehicleAngleIndex($ctrl.drawerVehicle) === $index ? \'true\' : \'false\' }}" ' +
                              'ng-click="$ctrl.selectVehicleAngle($ctrl.drawerVehicle, $index)">' +
                        '{{ ang.labelKey | t }}' +
                      '</button>' +
                    '</div>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;margin-top:10px;font-size:12px;">' +
                    '<span class="text-secondary">{{ \'customerGarage.currentOdometer\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.drawerVehicle.currentMileageKm | number }} km</strong></span>' +
                    '<span class="badge" ng-class="$ctrl.drawerVehicle.isWarrantyCovered ? \'badge-success\' : \'badge-warning\'">' +
                      '<app-icon name="{{ $ctrl.drawerVehicle.isWarrantyCovered ? \'shield-check\' : \'warning\' }}" weight="fill" size="12"></app-icon>' +
                      '<span>{{ $ctrl.drawerVehicle.isWarrantyCovered ? (\'customerGarage.warrantyCoveredChip\' | t) : (\'customerGarage.warrantyExpiredChip\' | t) }}</span>' +
                    '</span>' +
                  '</div>' +
                '</div>' +

                '<error-retry-banner ' +
                  'message="$ctrl.drawerProblem ? ($ctrl.drawerProblem.title + \': \' + $ctrl.drawerProblem.detail) : \'\'" ' +
                  'trace-id="$ctrl.drawerProblem && $ctrl.drawerProblem.traceId" ' +
                  'retrying="$ctrl.isLoadingReminders" ' +
                  'on-retry="$ctrl.openRemindersDrawer($ctrl.drawerVehicle)">' +
                '</error-retry-banner>' +

                '<skeleton-loader ng-if="$ctrl.isLoadingReminders" variant="drawer" count="3"></skeleton-loader>' +

                '<div ng-if="!$ctrl.isLoadingReminders">' +
                  '<div class="form-label" style="margin-bottom:10px;display:flex;align-items:center;gap:6px;">' +
                    '<app-icon name="bell-ringing" weight="duotone" size="16"></app-icon>' +
                    '<span>{{ \'customerGarage.drawerRemindersHeading\' | t:{ count: $ctrl.drawerReminders.length } }}</span>' +
                  '</div>' +

                  '<empty-state ng-if="$ctrl.drawerReminders.length === 0" ' +
                               'icon="bell" ' +
                               'title-key="customerGarage.drawerNoRemindersTitle" ' +
                               'desc-key="customerGarage.drawerNoRemindersDesc">' +
                  '</empty-state>' +

                  '<div class="notification-item stagger-item" ng-repeat="rem in $ctrl.drawerReminders track by rem.id" ' +
                       'ng-class="{\'is-unread\': rem.status === \'Sent\' || rem.status === \'Pending\'}" ng-attr-style="--i:{{ $index }};margin-bottom:10px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                      '<strong class="text-primary">{{ rem.milestoneName }}</strong>' +
                      '<span class="badge" ng-class="rem.status === \'Completed\' ? \'badge-success\' : \'badge-warning\'">' +
                        '{{ rem.status | enumT:\'ReminderStatus\' }}' +
                      '</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:4px;">' +
                      '{{ \'customerGarage.drawerTargetKm\' | t }}: <strong class="mono-num text-primary">{{ rem.targetMileageKm | number }} km</strong>' +
                      '<span ng-if="rem.dueDateUtc"> • {{ \'customerGarage.drawerDueDate\' | t }}: <span class="mono-num">{{ rem.dueDateUtc | utcToLocal:\'date\' }}</span></span>' +
                    '</div>' +
                    '<div class="mono-num text-muted" style="font-size:11px;margin-top:4px;" ng-if="rem.deduplicationKey">' +
                      '{{ \'customerGarage.drawerDedupKey\' | t }}: {{ rem.deduplicationKey }}' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-label" style="margin:18px 0 10px;display:flex;align-items:center;gap:6px;">' +
                    '<app-icon name="clipboard-text" weight="duotone" size="16"></app-icon>' +
                    '<span>{{ \'customerGarage.drawerRulesHeading\' | t:{ count: $ctrl.drawerRules.length } }}</span>' +
                  '</div>' +
                  '<div class="notification-item" ng-repeat="rule in $ctrl.drawerRules track by rule.id" style="margin-bottom:8px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                      '<strong class="text-primary">{{ rule.milestoneName }}</strong>' +
                      '<span class="badge badge-neutral mono-num">{{ rule.intervalKm | number }} km / {{ rule.intervalMonths }} {{ \'customerGarage.drawerMonthsShort\' | t }}</span>' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:12px;margin-top:4px;">{{ rule.checklistSummary }}</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeRemindersDrawer()">{{ \'common.close\' | t }}</button>' +
                '<a class="btn btn-primary" ng-click="$ctrl.closeRemindersDrawer()" ui-sref="app.customer.bookingNew({ vehicleId: $ctrl.drawerVehicle.id })">' +
                  '<app-icon name="wrench" weight="regular" size="15"></app-icon>' +
                  '<span>{{ \'customerGarage.bookService\' | t }}</span>' +
                  '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                '</a>' +
              '</div>' +
            '</aside>' +
          '</div>' +
        '</section>'
    });

  CustomerGarageController.$inject = [
    '$q',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function CustomerGarageController(
    $q,
    DealerApiService,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var offReminder = angular.noop;
    var isDestroyed = false;
    var galleryCache = Object.create(null);

    function resolveVehicleSlug(bike) {
      if (!bike) {
        return null;
      }
      var code = String(bike.modelCode || '').trim().toUpperCase();
      if (code && GARAGE_CODE_TO_SLUG[code]) {
        return GARAGE_CODE_TO_SLUG[code];
      }
      var raw = String(bike.imageUrl || '').trim();
      var match = raw.match(/\/assets\/models\/([a-z0-9-]+)\.(?:jpe?g|webp|png)(?:\?.*)?$/i);
      if (match && GARAGE_MULTI_ANGLE_SLUGS[match[1].toLowerCase()]) {
        return match[1].toLowerCase();
      }
      return null;
    }

    function buildVehicleGallery(bike) {
      if (!bike) {
        return [];
      }
      var slug = resolveVehicleSlug(bike);
      var key = slug || (String(bike.id || '') + '::' + String(bike.imageUrl || ''));
      if (galleryCache[key]) {
        return galleryCache[key];
      }
      var items;
      if (slug && GARAGE_MULTI_ANGLE_SLUGS[slug]) {
        items = GARAGE_ANGLE_DEFS.map(function (def) {
          return {
            key: def.key,
            url: '/assets/models/' + slug + def.suffix + '.jpg',
            labelKey: def.labelKey,
            fallback: def.fallback
          };
        });
      } else {
        items = [{
          key: 'front',
          url: String(bike.imageUrl || '').trim(),
          labelKey: 'customerGarage.angleFront',
          fallback: 'Front 3/4'
        }];
      }
      galleryCache[key] = items;
      return items;
    }

    function computeFleetStats(list) {
      var rows = list || [];
      var dueCount = 0;
      var warrantyCovered = 0;
      for (var i = 0; i < rows.length; i++) {
        if (rows[i].maintenanceStatus === 'Overdue' || rows[i].maintenanceStatus === 'DueSoon') {
          dueCount++;
        }
        if (rows[i].isWarrantyCovered) {
          warrantyCovered++;
        }
      }
      $ctrl.fleetStats = {
        total: rows.length,
        onTrack: Math.max(0, rows.length - dueCount),
        dueCount: dueCount,
        warrantyCovered: warrantyCovered
      };
    }

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback !== undefined ? fallback : key;
    }

    function enumT(group, code) {
      if (I18nService && angular.isFunction(I18nService.enumLabel)) {
        return I18nService.enumLabel(group, code);
      }
      return code || '';
    }

    function sortVehicles(list) {
      var rank = { Overdue: 0, DueSoon: 1, Upcoming: 2 };
      return (list || []).slice().sort(function (a, b) {
        var rA = rank[a.maintenanceStatus] !== undefined ? rank[a.maintenanceStatus] : 3;
        var rB = rank[b.maintenanceStatus] !== undefined ? rank[b.maintenanceStatus] : 3;
        return rA - rB;
      });
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.listProblem = null;
      $ctrl.liveReminderBanner = null;

      $ctrl.registerModalOpen = false;
      $ctrl.isSavingRegister = false;
      $ctrl.registerProblem = null;
      $ctrl.registerFieldErrors = {};
      $ctrl.catalogModels = [];
      $ctrl.newVehicle = {
        vin: '',
        motorcycleModelId: '',
        currentMileageKm: 0,
        licensePlate: '',
        colorName: ''
      };

      $ctrl.odometerVehicle = null;
      $ctrl.odometerDraftKm = 0;
      $ctrl.isSavingOdometer = false;
      $ctrl.odometerProblem = null;
      $ctrl.odometerFieldErrors = {};

      $ctrl.drawerVehicle = null;
      $ctrl.drawerMediaMode = 'photos';
      $ctrl.isLoadingReminders = false;
      $ctrl.drawerProblem = null;
      $ctrl.drawerReminders = [];
      $ctrl.drawerRules = [];

      $ctrl.loadProblem = null;
      $ctrl.vehicles = [];
      $ctrl.vehicleAngleIndexById = Object.create(null);
      computeFleetStats([]);

      offReminder = SignalRClientService.on('MaintenanceReminderTriggered', function (payload) {
        if (!payload) {
          return;
        }
        $ctrl.liveReminderBanner = payload;
        var targetVin = String(payload.vin || '').toUpperCase();
        for (var i = 0; i < $ctrl.vehicles.length; i++) {
          if ($ctrl.vehicles[i].vin === targetVin) {
            $ctrl.vehicles[i].maintenanceStatus = 'DueSoon';
            $ctrl.vehicles[i].isDueSoonOrOverdue = true;
            if (payload.targetMileageKm) {
              $ctrl.vehicles[i].nextDueMileageKm = Number(payload.targetMileageKm);
            }
          }
        }
        $ctrl.vehicles = sortVehicles($ctrl.vehicles);
        computeFleetStats($ctrl.vehicles);
      });

      $ctrl.loadGarage();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      offReminder();
    };

    $ctrl.statusBadgeClass = function (status) {
      if (status === 'Overdue') { return 'badge-danger'; }
      if (status === 'DueSoon') { return 'badge-warning'; }
      return 'badge-success';
    };

    $ctrl.statusLabel = function (bike) {
      if (!bike) { return enumT('MaintenanceStatus', 'Upcoming'); }
      if (bike.maintenanceStatus === 'Overdue') {
        return enumT('MaintenanceStatus', 'Overdue');
      }
      if (bike.maintenanceStatus === 'DueSoon') {
        var diff = Math.max(0, (Number(bike.nextDueMileageKm) || 0) - (Number(bike.currentMileageKm) || 0));
        return t('customerGarage.dueSoonKmLeft', { km: diff }, 'Due Soon (' + diff + ' km left)');
      }
      return enumT('MaintenanceStatus', 'Upcoming');
    };

    $ctrl.loadGarage = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.listVehicles()
        .then(function (rows) {
          if (isDestroyed) {
            return rows;
          }
          $ctrl.vehicles = sortVehicles(rows);
          computeFleetStats($ctrl.vehicles);
          return $ctrl.vehicles;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.vehicles;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.openRegisterModal = function () {
      $ctrl.registerProblem = null;
      $ctrl.registerFieldErrors = {};
      $ctrl.newVehicle = {
        vin: '',
        motorcycleModelId: ($ctrl.catalogModels[0] && $ctrl.catalogModels[0].id) || '',
        currentMileageKm: 0,
        licensePlate: '',
        colorName: ''
      };
      $ctrl.registerModalOpen = true;

      if ($ctrl.catalogModels.length === 0) {
        return DealerApiService.listMotorcycles({ pageSize: 100 })
          .then(function (models) {
            if (!isDestroyed) {
              $ctrl.catalogModels = models.filter(function (m) { return m.isActive; });
              if (!$ctrl.newVehicle.motorcycleModelId && $ctrl.catalogModels.length > 0) {
                $ctrl.newVehicle.motorcycleModelId = $ctrl.catalogModels[0].id;
              }
            }
            return $ctrl.catalogModels;
          })
          .catch(function () {
            return $ctrl.catalogModels;
          });
      }
      return $q.resolve($ctrl.catalogModels);
    };

    $ctrl.closeRegisterModal = function () {
      $ctrl.registerModalOpen = false;
      $ctrl.registerProblem = null;
      $ctrl.registerFieldErrors = {};
    };

    $ctrl.onRegisterFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.registerForm, $ctrl.registerFieldErrors, fieldName);
    };

    $ctrl.submitRegisterVehicle = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.registerForm);
      if (($ctrl.registerForm && $ctrl.registerForm.$invalid) || $ctrl.isSavingRegister) {
        return $q.resolve(null);
      }

      $ctrl.isSavingRegister = true;
      $ctrl.registerProblem = null;
      $ctrl.registerFieldErrors = {};

      return DealerApiService.registerVehicle($ctrl.newVehicle)
        .then(function (created) {
          if (isDestroyed) {
            return created;
          }
          ToastService.success(
            t('customerGarage.regToastTitle', null, 'External VIN Registered'),
            t(
              'customerGarage.regToastBody',
              { model: created.motorcycleModelName, vin: created.vin },
              created.motorcycleModelName + ' (' + created.vin + ') added to your garage.'
            )
          );
          $ctrl.closeRegisterModal();
          $ctrl.loadGarage();
          return created;
        })
        .catch(function (rejection) {
          if (isDestroyed) {
            return null;
          }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.registerFieldErrors = ProblemDetailsMapper.applyToForm($ctrl.registerForm, problem);
          $ctrl.registerProblem = problem;
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSavingRegister = false;
          }
        });
    };

    $ctrl.submitExternalVehicle = $ctrl.submitRegisterVehicle;

    $ctrl.openOdometerModal = function (bike) {
      if (!bike) {
        return;
      }
      $ctrl.odometerVehicle = bike;
      $ctrl.odometerDraftKm = bike.currentMileageKm;
      $ctrl.odometerProblem = null;
      $ctrl.odometerFieldErrors = {};
    };

    $ctrl.closeOdometerModal = function () {
      $ctrl.odometerVehicle = null;
      $ctrl.odometerProblem = null;
      $ctrl.odometerFieldErrors = {};
    };

    $ctrl.onOdometerEdited = function () {
      $ctrl.odometerProblem = null;
      ProblemDetailsMapper.clearFieldError($ctrl.odometerForm, $ctrl.odometerFieldErrors, 'currentMileageKm', ['newMileageKm']);
    };

    $ctrl.submitOdometerUpdate = function () {
      if (!$ctrl.odometerVehicle || $ctrl.isSavingOdometer) {
        return $q.resolve(null);
      }
      ProblemDetailsMapper.clearServerErrors($ctrl.odometerForm);
      $ctrl.isSavingOdometer = true;
      $ctrl.odometerProblem = null;
      $ctrl.odometerFieldErrors = {};

      var targetId = $ctrl.odometerVehicle.id;
      return DealerApiService.updateVehicleMileage(targetId, $ctrl.odometerDraftKm)
        .then(function (updated) {
          if (isDestroyed) {
            return updated;
          }
          for (var i = 0; i < $ctrl.vehicles.length; i++) {
            if ($ctrl.vehicles[i].id === targetId) {
              $ctrl.vehicles[i] = updated;
            }
          }
          $ctrl.vehicles = sortVehicles($ctrl.vehicles);
          computeFleetStats($ctrl.vehicles);
          ToastService.success(
            t('customerGarage.odoToastTitle', null, 'Odometer Updated'),
            t(
              'customerGarage.odoToastBody',
              { vin: updated.vin, km: updated.currentMileageKm },
              'VIN ' + updated.vin + ' updated to ' + updated.currentMileageKm + ' km.'
            )
          );
          $ctrl.closeOdometerModal();
          return updated;
        })
        .catch(function (rejection) {
          if (isDestroyed) {
            return null;
          }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.odometerFieldErrors = ProblemDetailsMapper.applyToForm($ctrl.odometerForm, problem);
          $ctrl.odometerProblem = problem;
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSavingOdometer = false;
          }
        });
    };

    $ctrl.setDrawerMediaMode = function (mode) {
      $ctrl.drawerMediaMode = mode === '3d' ? '3d' : 'photos';
    };

    $ctrl.openRemindersDrawer = function (bike, initialMode) {
      if (!bike) {
        return $q.resolve(null);
      }
      $ctrl.drawerVehicle = bike;
      $ctrl.drawerMediaMode = initialMode === '3d' ? '3d' : 'photos';
      $ctrl.drawerProblem = null;
      $ctrl.drawerReminders = [];
      $ctrl.drawerRules = [];

      $ctrl.isLoadingReminders = true;
      return DealerApiService.getVehicleReminders(bike.id)
        .then(function (schedule) {
          if (isDestroyed || !$ctrl.drawerVehicle || $ctrl.drawerVehicle.id !== bike.id) {
            return schedule;
          }
          $ctrl.drawerReminders = schedule.reminders || [];
          $ctrl.drawerRules = schedule.rules || [];
          if ($ctrl.drawerRules.length === 0 && bike.motorcycleModelId) {
            return DealerApiService.listMaintenanceRules(bike.motorcycleModelId)
              .then(function (rules) {
                if (!isDestroyed && $ctrl.drawerVehicle && $ctrl.drawerVehicle.id === bike.id) {
                  $ctrl.drawerRules = rules;
                }
                return {
                  reminders: $ctrl.drawerReminders,
                  rules: $ctrl.drawerRules
                };
              })
              .catch(function () {
                return {
                  reminders: $ctrl.drawerReminders,
                  rules: $ctrl.drawerRules
                };
              });
          }
          return {
            reminders: $ctrl.drawerReminders,
            rules: $ctrl.drawerRules
          };
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            $ctrl.drawerProblem = ProblemDetailsMapper.extract(rejection);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed && $ctrl.drawerVehicle && $ctrl.drawerVehicle.id === bike.id) {
            $ctrl.isLoadingReminders = false;
          }
        });
    };

    $ctrl.closeRemindersDrawer = function () {
      $ctrl.drawerVehicle = null;
      $ctrl.drawerProblem = null;
      $ctrl.drawerReminders = [];
      $ctrl.drawerRules = [];
    };

    $ctrl.getVehicleGallery = function (bike) {
      return buildVehicleGallery(bike);
    };

    $ctrl.getVehicleAngleIndex = function (bike) {
      if (!bike || !bike.id) {
        return 0;
      }
      var gallery = buildVehicleGallery(bike);
      var idx = Number($ctrl.vehicleAngleIndexById[bike.id]) || 0;
      return (idx >= 0 && idx < gallery.length) ? idx : 0;
    };

    $ctrl.selectVehicleAngle = function (bike, index) {
      if (!bike || !bike.id) {
        return;
      }
      var gallery = buildVehicleGallery(bike);
      if (index >= 0 && index < gallery.length) {
        $ctrl.vehicleAngleIndexById[bike.id] = index;
      }
    };

    $ctrl.getVehicleActiveImageUrl = function (bike) {
      var gallery = buildVehicleGallery(bike);
      var idx = $ctrl.getVehicleAngleIndex(bike);
      return gallery[idx] ? gallery[idx].url : (bike && bike.imageUrl) || '';
    };
  }
})();
