(function () {
  'use strict';

  var VI_PATCH = {
    staff: {
      cust: {
        eyebrow: 'SCR-STF-05 • TRA CỨU KHÁCH HÀNG & GARAGE VIN',
        sellVinCta: 'Bán xe VIN Showroom',
        kpiVehicles: 'Xe VIN đã đăng ký',
        kpiVehiclesMeta: 'Hồ sơ khớp bộ lọc hiện tại',
        kpiAttention: 'Sắp đến hạn / Quá hạn',
        kpiAttentionMeta: 'Cần chủ động đặt lịch bảo dưỡng',
        kpiWarranty: 'Còn bảo hành OEM',
        kpiWarrantyMeta: 'Trong hạn 36 tháng / 30.000 km',
        searchLabel: 'Tra cứu theo số VIN 17 ký tự, biển số hoặc email khách hàng',
        searchPlaceholder: 'Nhập số khung VIN 17 ký tự, biển số hoặc địa chỉ email khách hàng...',
        lookupCta: 'Tra cứu VIN',
        searchingCta: 'Đang tra cứu...',
        clearCta: 'Xóa',
        colOwner: 'Chủ sở hữu & Hạng',
        colVinModel: 'Xe VIN & Mẫu xe',
        colPlateSource: 'Biển số & Nguồn',
        colOdoNextDue: 'ODO hiện tại / Mốc kế tiếp',
        colScheduleStatus: 'Trạng thái bảo dưỡng',
        registeredOwnerFallback: 'Khách hàng đã đăng ký',
        inspectHistoryCta: 'Hồ sơ & Lịch OEM',
        emptyTitle: 'Không tìm thấy xe khách hàng phù hợp',
        emptyDesc: 'Thử xóa từ khóa hoặc tìm theo số khung VIN 17 ký tự, biển số hay địa chỉ email.',
        drawerTitleSuffix: 'Hồ sơ Khách hàng & Lịch OEM',
        drawerMemberCardTitle: 'Hồ sơ Hội viên & Quyền lợi',
        drawerFleetTitle: 'Đội xe VIN của khách hàng ({{count}})',
        drawerActiveUnit: 'Đang xem',
        drawerOdoLabel: 'ODO hiện tại',
        drawerNextMilestone: 'Mốc kế tiếp',
        drawerRemindersTitle: 'Nhắc lịch bảo dưỡng tự động ({{count}})',
        drawerNoReminders: 'Chưa có thông báo nhắc bảo dưỡng tự động nào cho số khung VIN này.',
        drawerTargetKm: 'Mốc mục tiêu: {{km}} km',
        drawerRulesTitle: 'Chuẩn mốc bảo dưỡng OEM ({{count}})',
        drawerNoRules: 'Áp dụng lịch bảo dưỡng định kỳ chuẩn hãng.',
        intervalLabel: '{{km}} km / {{months}} tháng'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      cust: {
        eyebrow: 'SCR-STF-05 • CUSTOMER & VIN GARAGE LOOKUP',
        sellVinCta: 'Sell Showroom VIN',
        kpiVehicles: 'Registered VIN Fleet',
        kpiVehiclesMeta: 'Matching current lookup criteria',
        kpiAttention: 'Due Soon / Overdue',
        kpiAttentionMeta: 'Eligible for proactive workshop follow-up',
        kpiWarranty: 'OEM Warranty Active',
        kpiWarrantyMeta: 'Within 36-month / 30,000-km coverage',
        searchLabel: 'Search by 17-Char VIN, License Plate, or Customer Email',
        searchPlaceholder: 'Enter 17-character VIN, license plate, or customer email...',
        lookupCta: 'Lookup VIN',
        searchingCta: 'Searching...',
        clearCta: 'Clear',
        colOwner: 'Customer Owner & Tier',
        colVinModel: 'Registered VIN & Model',
        colPlateSource: 'Plate & Source',
        colOdoNextDue: 'Odometer / Next Due',
        colScheduleStatus: 'Schedule Status',
        registeredOwnerFallback: 'Registered Owner',
        inspectHistoryCta: 'Inspect Profile',
        emptyTitle: 'No registered customer vehicles matched',
        emptyDesc: 'Try clearing the query or searching by 17-character VIN, license plate, or customer email.',
        drawerTitleSuffix: 'Customer Profile & OEM History',
        drawerMemberCardTitle: 'Member Tier & Warranty Overview',
        drawerFleetTitle: 'Customer VIN Fleet ({{count}})',
        drawerActiveUnit: 'Inspecting',
        drawerOdoLabel: 'Odometer',
        drawerNextMilestone: 'Next Milestone',
        drawerRemindersTitle: 'Active & Historical Reminders ({{count}})',
        drawerNoReminders: 'No automated reminders dispatched yet for this VIN.',
        drawerTargetKm: 'Target: {{km}} km',
        drawerRulesTitle: 'OEM Maintenance Milestones ({{count}})',
        drawerNoRules: 'Standard factory periodic maintenance intervals apply.',
        intervalLabel: '{{km}} km / {{months}} mo'
      }
    }
  };

  registerCustomersI18n.$inject = ['I18nService'];
  function registerCustomersI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerCustomersI18n)
    .component('staffCustomers', {
      controllerAs: '$ctrl',
      controller: StaffCustomersController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="users" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.cust.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.customersTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'staff.customersSubtitle\' | t }}</p>' +
            '</div>' +
            '<a class="btn-pill-cta" ui-sref="app.staff.salesDesk">' +
              '<span>{{ \'staff.cust.sellVinCta\' | t }}</span>' +
              '<span class="btn-pill-well" aria-hidden="true">' +
                '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
              '</span>' +
            '</a>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.lookupProblem ? ($ctrl.lookupProblem.title + \': \' + $ctrl.lookupProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.lookupProblem && $ctrl.lookupProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.executeLookup()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.cust.kpiVehicles\' | t }}</span>' +
                  '<app-icon name="motorcycle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.vehicles.length">0</div>' +
                '<div class="kpi-meta">{{ \'staff.cust.kpiVehiclesMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.cust.kpiAttention\' | t }}</span>' +
                  '<app-icon name="warning" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-warning-fg);" kpi-counter="$ctrl.directoryStats.attentionCount">0</div>' +
                '<div class="kpi-meta">{{ \'staff.cust.kpiAttentionMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.cust.kpiWarranty\' | t }}</span>' +
                  '<app-icon name="shield-check" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);" kpi-counter="$ctrl.directoryStats.warrantyCoveredCount">0</div>' +
                '<div class="kpi-meta">{{ \'staff.cust.kpiWarrantyMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Search Bar */
          '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
            '<form ng-submit="$ctrl.executeLookup()" novalidate>' +
              '<label class="form-label" for="vinLookupInput">{{ \'staff.cust.searchLabel\' | t }}</label>' +
              '<div class="input-action-group" style="margin-top:6px;">' +
                '<input id="vinLookupInput" type="search" class="form-control mono-num" ' +
                       'ng-model="$ctrl.query" ng-attr-placeholder="{{ \'staff.cust.searchPlaceholder\' | t }}" />' +
                '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.isLoading">' +
                  '<app-icon name="{{ $ctrl.isLoading ? \'circle-notch\' : \'magnifying-glass\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                  '<span>{{ $ctrl.isLoading ? (\'staff.cust.searchingCta\' | t) : (\'staff.cust.lookupCta\' | t) }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.clearLookup()" ng-disabled="$ctrl.isLoading">' +
                  '{{ \'staff.cust.clearCta\' | t }}' +
                '</button>' +
              '</div>' +
            '</form>' +
          '</div>' +

          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.vehicles.length === 0" variant="table" count="5"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.vehicles.length === 0" ' +
                       'icon="users" ' +
                       'title-key="staff.cust.emptyTitle" ' +
                       'desc-key="staff.cust.emptyDesc" ' +
                       'action-key="common.clearFilters" ' +
                       'on-action="$ctrl.clearLookup()">' +
          '</empty-state>' +

          /* Quiet-Chrome Customer & VIN Directory Table */
          '<div class="bezel-card" ng-if="$ctrl.vehicles.length > 0">' +
            '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
              '<div class="table-wrap" style="max-height:62vh;overflow-y:auto;">' +
                '<table class="data-table">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.cust.colOwner\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.cust.colVinModel\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.cust.colPlateSource\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.cust.colOdoNextDue\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.cust.colScheduleStatus\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.actions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr class="stagger-item" ng-repeat="row in $ctrl.vehicles track by row.id" style="--i:{{ $index }};">' +
                      '<td>' +
                        '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                          '<span class="text-primary" style="font-weight:600;">{{ row.customerFullName || (\'staff.cust.registeredOwnerFallback\' | t) }}</span>' +
                          '<span class="badge" ng-if="row.tierCode" ng-class="\'badge-tier-\' + row.tierCode.toLowerCase()">' +
                            '<app-icon name="medal" weight="duotone" size="11"></app-icon>' +
                            '<span>{{ row.tierCode | enumT:\'LoyaltyTierCode\' }}</span>' +
                          '</span>' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:12px;">{{ row.customerEmail }}</div>' +
                        '<div class="mono-num tabular-nums text-muted" style="font-size:11px;" ng-if="row.customerPhone">{{ row.customerPhone }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<div style="display:flex;align-items:center;gap:10px;">' +
                          '<div style="width:64px;flex-shrink:0;">' +
                            '<bike-image src="$ctrl.resolveVehicleImage(row)" alt="{{ row.motorcycleModelName }}" variant="thumb"></bike-image>' +
                          '</div>' +
                          '<div>' +
                            '<div class="mono-num tabular-nums text-primary" style="font-weight:700;">{{ row.vin }}</div>' +
                            '<div class="text-secondary" style="font-size:12px;">{{ row.motorcycleModelName }}</div>' +
                          '</div>' +
                        '</div>' +
                      '</td>' +
                      '<td>' +
                        '<div class="mono-num tabular-nums">{{ row.licensePlate || \'—\' }}</div>' +
                        '<span class="badge badge-neutral" style="margin-top:4px;">{{ row.registrationSource | enumT:\'RegistrationSource\' }}</span>' +
                      '</td>' +
                      '<td class="mono-num tabular-nums" style="text-align:right;">{{ row.currentMileageKm | number }} / {{ row.nextDueMileageKm | number }} km</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="row.maintenanceStatus === \'Overdue\' ? \'badge-danger\' : (row.maintenanceStatus === \'DueSoon\' ? \'badge-warning\' : \'badge-success\')">' +
                          '{{ row.maintenanceStatus | enumT:\'MaintenanceStatus\' }}' +
                        '</span>' +
                      '</td>' +
                      '<td style="text-align:right;">' +
                        '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.inspectSchedule(row)">' +
                          '<app-icon name="identification-card" weight="regular" size="14"></app-icon>' +
                          '<span>{{ \'staff.cust.inspectHistoryCta\' | t }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Slide-Over Customer Detail & VIN Fleet Drawer */
          '<div class="drawer-backdrop" role="dialog" aria-modal="true" aria-labelledby="stfVehHistTitle" ng-if="$ctrl.selectedVehicle">' +
            '<aside class="drawer-panel-wide motion-drawer">' +
              '<div class="notification-drawer-header">' +
                '<div>' +
                  '<div class="mono-num" style="font-size:11px;opacity:0.75;">{{ $ctrl.selectedVehicle.vin }}</div>' +
                  '<strong id="stfVehHistTitle" style="font-size:15px;">{{ $ctrl.selectedVehicle.customerFullName || $ctrl.selectedVehicle.motorcycleModelName }} — {{ \'staff.cust.drawerTitleSuffix\' | t }}</strong>' +
                '</div>' +
                '<button type="button" class="btn btn-ghost-dark btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeSchedule()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="notification-drawer-list">' +
                /* Member Tier & Overview Card */
                '<div class="bezel-card">' +
                  '<div class="bezel-card-inner" style="padding:var(--space-4);font-size:13px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;margin-bottom:8px;">' +
                      '<strong class="text-primary">{{ \'staff.cust.drawerMemberCardTitle\' | t }}</strong>' +
                      '<span class="badge" ng-class="\'badge-tier-\' + ($ctrl.selectedVehicle.tierCode || \'Bronze\').toLowerCase()">' +
                        '<app-icon name="medal" weight="duotone" size="13"></app-icon>' +
                        '<span>{{ ($ctrl.selectedVehicle.tierCode || \'Bronze\') | enumT:\'LoyaltyTierCode\' }}</span>' +
                        '<span class="mono-num" ng-if="$ctrl.selectedVehicle.tierMultiplier">• {{ $ctrl.selectedVehicle.tierMultiplier | number:2 }}x</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="display:flex;flex-direction:column;gap:4px;">' +
                      '<div style="display:inline-flex;align-items:center;gap:6px;">' +
                        '<app-icon name="user-circle" weight="duotone" size="15"></app-icon>' +
                        '<strong class="text-primary">{{ $ctrl.selectedVehicle.customerFullName || (\'staff.cust.registeredOwnerFallback\' | t) }}</strong>' +
                      '</div>' +
                      '<div style="display:inline-flex;align-items:center;gap:6px;" ng-if="$ctrl.selectedVehicle.customerEmail">' +
                        '<app-icon name="envelope" weight="regular" size="14"></app-icon>' +
                        '<span>{{ $ctrl.selectedVehicle.customerEmail }}</span>' +
                      '</div>' +
                      '<div style="display:inline-flex;align-items:center;gap:6px;" ng-if="$ctrl.selectedVehicle.customerPhone">' +
                        '<app-icon name="phone" weight="regular" size="14"></app-icon>' +
                        '<span class="mono-num">{{ $ctrl.selectedVehicle.customerPhone }}</span>' +
                      '</div>' +
                      '<div style="display:flex;gap:8px;flex-wrap:wrap;margin-top:6px;">' +
                        '<span class="badge" ng-class="$ctrl.selectedVehicle.isWarrantyCovered ? \'badge-success\' : \'badge-neutral\'">' +
                          '<app-icon name="shield-check" weight="fill" size="12"></app-icon>' +
                          '<span>{{ $ctrl.selectedVehicle.isWarrantyCovered ? (\'warranty.coveredBadge\' | t) : (\'warranty.expiredBadge\' | t) }}</span>' +
                        '</span>' +
                        '<span class="badge badge-info mono-num" ng-if="$ctrl.selectedVehicle.availablePoints !== null && $ctrl.selectedVehicle.availablePoints !== undefined">' +
                          '{{ $ctrl.selectedVehicle.availablePoints | number }} {{ \'common.ptsShort\' | t }}' +
                        '</span>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                /* Customer VIN Fleet Strip */
                '<div style="margin-top:4px;">' +
                  '<div class="form-label" style="margin-bottom:8px;">{{ \'staff.cust.drawerFleetTitle\' | t:{ count: $ctrl.selectedCustomerFleet.length } }}</div>' +
                  '<div style="display:flex;flex-direction:column;gap:8px;">' +
                    '<div class="surface-sunken" style="padding:10px 12px;display:flex;align-items:center;gap:10px;cursor:pointer;" ' +
                         'ng-repeat="fv in $ctrl.selectedCustomerFleet track by fv.id" ' +
                         'ng-click="$ctrl.inspectSchedule(fv)">' +
                      '<div style="width:68px;flex-shrink:0;">' +
                        '<bike-image src="$ctrl.resolveVehicleImage(fv)" alt="{{ fv.motorcycleModelName }}" variant="thumb"></bike-image>' +
                      '</div>' +
                      '<div style="flex:1;min-width:0;">' +
                        '<div style="display:flex;align-items:center;justify-content:space-between;gap:6px;flex-wrap:wrap;">' +
                          '<strong class="text-primary" style="font-size:13px;">{{ fv.motorcycleModelName }}</strong>' +
                          '<span class="badge badge-info" ng-if="fv.id === $ctrl.selectedVehicle.id">{{ \'staff.cust.drawerActiveUnit\' | t }}</span>' +
                        '</div>' +
                        '<div class="mono-num text-secondary" style="font-size:12px;">{{ fv.vin }}</div>' +
                        '<div class="mono-num text-muted" style="font-size:11px;margin-top:2px;">' +
                          '{{ \'staff.cust.drawerOdoLabel\' | t }}: {{ fv.currentMileageKm | number }} km • ' +
                          '{{ \'staff.cust.drawerNextMilestone\' | t }}: {{ fv.nextDueMileageKm | number }} km' +
                        '</div>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<skeleton-loader ng-if="$ctrl.isLoadingDrawer" variant="drawer" count="2"></skeleton-loader>' +

                /* Reminders */
                '<div ng-if="!$ctrl.isLoadingDrawer">' +
                  '<div class="form-label" style="margin-top:8px;margin-bottom:6px;">{{ \'staff.cust.drawerRemindersTitle\' | t:{ count: $ctrl.selectedReminders.length } }}</div>' +
                  '<div ng-if="$ctrl.selectedReminders.length === 0" class="text-muted" style="font-size:12px;">{{ \'staff.cust.drawerNoReminders\' | t }}</div>' +
                  '<div style="display:flex;flex-direction:column;gap:8px;">' +
                    '<div class="notification-item" ng-repeat="rem in $ctrl.selectedReminders track by rem.id">' +
                      '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;">' +
                        '<strong class="text-primary">{{ rem.milestoneName }}</strong>' +
                        '<span class="badge badge-warning">{{ rem.status | enumT:\'ReminderStatus\' }}</span>' +
                      '</div>' +
                      '<div class="mono-num text-muted" style="font-size:12px;margin-top:4px;">' +
                        '{{ \'staff.cust.drawerTargetKm\' | t:{ km: (rem.targetMileageKm | number) } }} • {{ rem.deduplicationKey }}' +
                      '</div>' +
                    '</div>' +
                  '</div>' +

                  /* OEM Milestones */
                  '<div class="form-label" style="margin-top:14px;margin-bottom:6px;">{{ \'staff.cust.drawerRulesTitle\' | t:{ count: $ctrl.selectedRules.length } }}</div>' +
                  '<div ng-if="$ctrl.selectedRules.length === 0" class="text-muted" style="font-size:12px;">{{ \'staff.cust.drawerNoRules\' | t }}</div>' +
                  '<div style="display:flex;flex-direction:column;gap:8px;">' +
                    '<div class="notification-item" ng-repeat="rule in $ctrl.selectedRules track by rule.id">' +
                      '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;">' +
                        '<strong class="text-primary">{{ rule.milestoneName }}</strong>' +
                        '<span class="badge badge-neutral mono-num">{{ \'staff.cust.intervalLabel\' | t:{ km: (rule.intervalKm | number), months: rule.intervalMonths } }}</span>' +
                      '</div>' +
                      '<div class="text-muted" style="font-size:12px;margin-top:4px;">{{ rule.checklistSummary }}</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</aside>' +
          '</div>' +
        '</section>'
    });

  StaffCustomersController.$inject = [
    'DealerApiService',
    'ProblemDetailsMapper',
    'TokenStorageService',
    'I18nService'
  ];
  function StaffCustomersController(
    DealerApiService,
    ProblemDetailsMapper,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var modelImageById = Object.create(null);
    var modelImageByName = Object.create(null);

    function hydrateModelImageIndex() {
      if (!DealerApiService || !angular.isFunction(DealerApiService.listMotorcycles)) {
        return;
      }
      DealerApiService.listMotorcycles({ pageSize: 100 })
        .then(function (models) {
          if (isDestroyed || !angular.isArray(models)) { return; }
          for (var i = 0; i < models.length; i++) {
            if (models[i].imageUrl) {
              if (models[i].id) {
                modelImageById[models[i].id] = models[i].imageUrl;
              }
              var fullLabel = String((models[i].brand ? (models[i].brand + ' ') : '') + (models[i].modelName || '')).trim().toLowerCase();
              if (fullLabel) {
                modelImageByName[fullLabel] = models[i].imageUrl;
              }
              if (models[i].modelName) {
                modelImageByName[String(models[i].modelName).trim().toLowerCase()] = models[i].imageUrl;
              }
            }
          }
        })
        .catch(angular.noop);
    }

    $ctrl.resolveVehicleImage = function (vehicle) {
      if (!vehicle) { return ''; }
      if (vehicle.imageUrl) { return vehicle.imageUrl; }
      if (vehicle.motorcycleModelId && modelImageById[vehicle.motorcycleModelId]) {
        return modelImageById[vehicle.motorcycleModelId];
      }
      if (vehicle.motorcycleModelName) {
        var key = String(vehicle.motorcycleModelName).trim().toLowerCase();
        if (modelImageByName[key]) {
          return modelImageByName[key];
        }
      }
      return '';
    };

    function recomputeDirectoryStats() {
      var list = $ctrl.vehicles || [];
      var attention = 0;
      var covered = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].maintenanceStatus === 'DueSoon' || list[i].maintenanceStatus === 'Overdue') {
          attention++;
        }
        if (list[i].isWarrantyCovered) {
          covered++;
        }
      }
      $ctrl.directoryStats = {
        attentionCount: attention,
        warrantyCoveredCount: covered
      };
    }

    function buildFleetForVehicle(vehicle) {
      if (!vehicle) { return []; }
      var all = $ctrl.vehicles || [];
      var cid = vehicle.customerId ? String(vehicle.customerId).trim().toLowerCase() : '';
      var email = vehicle.customerEmail ? String(vehicle.customerEmail).trim().toLowerCase() : '';
      var fleet = [];
      for (var i = 0; i < all.length; i++) {
        var row = all[i];
        var sameCustomer = (cid && row.customerId && String(row.customerId).trim().toLowerCase() === cid) ||
          (!cid && email && row.customerEmail && String(row.customerEmail).trim().toLowerCase() === email);
        if (sameCustomer || row.id === vehicle.id) {
          fleet.push(row);
        }
      }
      if (fleet.length === 0) {
        fleet.push(vehicle);
      }
      return fleet;
    }

    function buildQueryParams(rawQuery) {
      var trimmed = String(rawQuery || '').trim();
      if (!trimmed) {
        return {};
      }
      if (/^[A-HJ-NPR-Z0-9]{17}$/i.test(trimmed)) {
        return { vin: trimmed.toUpperCase() };
      }
      if (trimmed.indexOf('@') !== -1) {
        return { email: trimmed };
      }
      if (/^[A-HJ-NPR-Z0-9]{6,16}$/i.test(trimmed)) {
        return { vin: trimmed.toUpperCase() };
      }
      return { search: trimmed };
    }

    $ctrl.$onInit = function () {
      $ctrl.query = '';
      $ctrl.isLoading = false;
      $ctrl.isLoadingDrawer = false;
      $ctrl.lookupProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.selectedVehicle = null;
      $ctrl.selectedCustomerFleet = [];
      $ctrl.selectedReminders = [];
      $ctrl.selectedRules = [];
      $ctrl.directoryStats = { attentionCount: 0, warrantyCoveredCount: 0 };

      $ctrl.vehicles = [];
      hydrateModelImageIndex();
      $ctrl.executeLookup();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
    };

    $ctrl.executeLookup = function () {
      $ctrl.isLoading = true;
      $ctrl.lookupProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.listVehicles(buildQueryParams($ctrl.query))
        .then(function (rows) {
          if (!isDestroyed) {
            $ctrl.vehicles = rows;
            recomputeDirectoryStats();
          }
          return rows;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.lookupProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return [];
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.clearLookup = function () {
      $ctrl.query = '';
      $ctrl.executeLookup();
    };

    $ctrl.inspectSchedule = function (vehicle) {
      if (!vehicle) { return; }
      $ctrl.selectedVehicle = vehicle;
      $ctrl.selectedCustomerFleet = buildFleetForVehicle(vehicle);
      $ctrl.selectedReminders = [];
      $ctrl.selectedRules = [];
      $ctrl.isLoadingDrawer = true;
      DealerApiService.getVehicleReminders(vehicle.id)
        .then(function (schedule) {
          if (isDestroyed || !$ctrl.selectedVehicle || $ctrl.selectedVehicle.id !== vehicle.id) {
            return;
          }
          $ctrl.selectedReminders = schedule.reminders || [];
          $ctrl.selectedRules = schedule.rules || [];
        })
        .catch(angular.noop)
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoadingDrawer = false;
          }
        });
    };

    $ctrl.closeSchedule = function () {
      $ctrl.selectedVehicle = null;
      $ctrl.selectedCustomerFleet = [];
      $ctrl.selectedReminders = [];
      $ctrl.selectedRules = [];
      $ctrl.isLoadingDrawer = false;
    };
  }
})();
