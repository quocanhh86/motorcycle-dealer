(function () {
  'use strict';

  var VI_PATCH = {
    staff: {
      td: {
        eyebrow: 'SCR-STF-06 • ĐIỀU PHỐI LÁI THỬ TRỰC TIẾP',
        liveNewBadge: '{{count}} mới trực tiếp',
        syncQueue: 'Đồng bộ hàng đợi',
        syncing: 'Đang đồng bộ...',
        salesDeskCta: 'Kho VIN & Bán xe',
        liveBannerHeadline: 'YÊU CẦU LÁI THỬ MỚI',
        liveBannerFor: 'cho khách',
        dismiss: 'Ẩn',
        kpiRequested: 'Chờ duyệt lịch',
        kpiRequestedLiveMeta: '{{count}} yêu cầu vừa đến qua SignalR',
        kpiRequestedDefaultMeta: 'Hộp thư chi nhánh thời gian thực',
        kpiConfirmedToday: 'Đã xác nhận hôm nay',
        kpiConfirmedMeta: '{{total}} lịch đã xác nhận sắp tới',
        kpiCompleted: 'Đã hoàn tất lái thử',
        kpiCompletedMeta: 'Sẵn sàng chuyển đổi đơn bán xe',
        kpiCancelled: 'Đã hủy / Không đến',
        kpiCancelledMeta: 'Đã giải phóng suất lái thử',
        preferredDateLabel: 'Ngày hẹn lái thử',
        allDatesOption: 'Tất cả ngày có lịch',
        todaySuffix: '(Hôm nay)',
        allStatusesOption: 'Tất cả trạng thái',
        searchLabel: 'Tìm theo mã lịch, mẫu xe, khách hàng hoặc GPLX',
        searchPlaceholder: 'Nhập mã lịch lái thử, mẫu xe, tên khách hàng hoặc số GPLX...',
        emptyTitle: 'Không có lịch lái thử nào khớp bộ lọc',
        emptyDesc: 'Yêu cầu lái thử mới từ khách hàng cho chi nhánh sẽ xuất hiện tự động tại đây qua SignalR.',
        demoVinBadgePrefix: 'Xe Demo VIN',
        schedulePrefix: 'Khung giờ',
        licensePrefix: 'GPLX',
        phonePrefix: 'SĐT',
        customerNotesPrefix: 'Ghi chú khách hàng',
        cancelReasonPrefix: 'Lý do hủy',
        demoVinAria: 'Gán số khung xe Demo không bắt buộc',
        demoVinPlaceholder: '-- Gán xe Demo VIN (Không bắt buộc) --',
        actionConfirm: 'Xác nhận lái thử',
        actionComplete: 'Hoàn tất lái thử',
        actionNoShow: 'Không đến',
        actionCancel: 'Hủy lịch',
        actionConvertSale: 'Chuyển sang Bán xe',
        cancelModalTitle: 'Hủy lịch lái thử {{bookingNumber}}',
        cancelReasonLabel: 'Lý do hủy của tư vấn viên * (3–300 ký tự)',
        cancelReasonPlaceholder: 'Nhập lý do hủy hoặc yêu cầu dời lịch của khách hàng...',
        cancelDefaultReason: 'Hủy tại quầy điều phối lái thử chi nhánh',
        confirmCancelCta: 'Xác nhận hủy',
        toastUpdatedTitle: 'Đã cập nhật lịch lái thử',
        toastUpdatedMsg: '{{bookingNumber}} đã chuyển sang {{status}}.',
        statusNoteTemplate: 'Chuyển sang {{status}} tại {{dealer}}',
        defaultDealerName: 'Đại lý ủy quyền'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      td: {
        eyebrow: 'SCR-STF-06 • LIVE TEST-DRIVE DESK',
        liveNewBadge: '{{count}} Live New',
        syncQueue: 'Sync Queue',
        syncing: 'Syncing...',
        salesDeskCta: 'Showroom Sales Desk',
        liveBannerHeadline: 'LIVE TEST-DRIVE ARRIVED',
        liveBannerFor: 'for',
        dismiss: 'Dismiss',
        kpiRequested: 'Requested (Awaiting Confirm)',
        kpiRequestedLiveMeta: '{{count}} arrived live via SignalR',
        kpiRequestedDefaultMeta: 'Real-time branch inbox',
        kpiConfirmedToday: 'Confirmed Today',
        kpiConfirmedMeta: '{{total}} total confirmed upcoming',
        kpiCompleted: 'Completed Demo Rides',
        kpiCompletedMeta: 'Ready for 1-click Bike Sale conversion',
        kpiCancelled: 'Cancelled / No-Show',
        kpiCancelledMeta: 'Released slot capacity',
        preferredDateLabel: 'Preferred Date',
        allDatesOption: 'All Scheduled Dates',
        todaySuffix: '(Today)',
        allStatusesOption: 'All Statuses',
        searchLabel: 'Search Booking #, Model, Customer, or License',
        searchPlaceholder: 'Search by booking number, motorcycle model, customer name, or license...',
        emptyTitle: 'No Showroom Test-Drives Match Current Filters',
        emptyDesc: 'Incoming customer test-drive requests for this dealership appear automatically via SignalR.',
        demoVinBadgePrefix: 'Demo VIN',
        schedulePrefix: 'Schedule',
        licensePrefix: 'License',
        phonePrefix: 'Tel',
        customerNotesPrefix: 'Customer Notes',
        cancelReasonPrefix: 'Cancellation Reason',
        demoVinAria: 'Assign optional demo motorcycle VIN',
        demoVinPlaceholder: '-- Optional Demo VIN Assignment --',
        actionConfirm: 'Confirm Test Drive',
        actionComplete: 'Complete Ride',
        actionNoShow: 'No-Show',
        actionCancel: 'Cancel',
        actionConvertSale: 'Convert to Bike Sale',
        cancelModalTitle: 'Cancel Test Drive {{bookingNumber}}',
        cancelReasonLabel: 'Advisor Cancellation Reason * (3–300 chars)',
        cancelReasonPlaceholder: 'Customer requested reschedule by phone...',
        cancelDefaultReason: 'Cancelled at dealership test-drive desk',
        confirmCancelCta: 'Confirm Cancel',
        toastUpdatedTitle: 'Test Drive Updated',
        toastUpdatedMsg: '{{bookingNumber}} transitioned to {{status}}.',
        statusNoteTemplate: 'Transitioned to {{status}} at {{dealer}}',
        defaultDealerName: 'Authorized Dealership'
      }
    }
  };

  registerTestDrivesI18n.$inject = ['I18nService'];
  function registerTestDrivesI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerTestDrivesI18n)
    .component('staffTestDrives', {
      controllerAs: '$ctrl',
      controller: StaffTestDrivesController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="steering-wheel" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.td.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.testDrivesTitle\' | t }}</h1>' +
              '<p class="page-subtitle">' +
                '<span class="mono-num">dealer-{{ $ctrl.dealerCode }}</span> ({{ $ctrl.dealerName }}) — {{ \'staff.testDrivesSubtitle\' | t }}' +
              '</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<span class="badge badge-danger mono-num tabular-nums" ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}" ng-if="$ctrl.liveArrivalCount > 0">' +
                '<app-icon name="lightning" weight="fill" size="12"></app-icon>' +
                '<span>{{ \'staff.td.liveNewBadge\' | t:{ count: $ctrl.liveArrivalCount } }}</span>' +
              '</span>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadDeskData()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrows-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'staff.td.syncing\' | t) : (\'staff.td.syncQueue\' | t) }}</span>' +
              '</button>' +
              '<a class="btn-pill-cta" ui-sref="app.staff.salesDesk">' +
                '<span>{{ \'staff.td.salesDeskCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          /* Live SignalR Arrival Banner */
          '<div class="alert-banner alert-info fade-slide-up" role="status" ng-if="$ctrl.latestLiveBanner" ' +
               'ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
              '<app-icon name="lightning" weight="fill" size="18"></app-icon>' +
              '<strong>{{ \'staff.td.liveBannerHeadline\' | t }}:</strong>' +
              '<span class="mono-num" style="font-weight:700;">{{ $ctrl.latestLiveBanner.bookingNumber }}</span>' +
              '<span>• {{ $ctrl.latestLiveBanner.motorcycleModelName }} {{ \'staff.td.liveBannerFor\' | t }} <strong>{{ $ctrl.latestLiveBanner.customerFullName || (\'common.customer\' | t) }}</strong></span>' +
              '<span class="mono-num">({{ $ctrl.latestLiveBanner.preferredDate }} {{ $ctrl.latestLiveBanner.timeWindow }})</span>' +
            '</div>' +
            '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.latestLiveBanner = null">{{ \'staff.td.dismiss\' | t }}</button>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadDeskData()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.td.kpiRequested\' | t }}</span>' +
                  '<app-icon name="clock-countdown" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.kpis.requested">0</div>' +
                '<div class="kpi-meta">{{ $ctrl.liveArrivalCount > 0 ? (\'staff.td.kpiRequestedLiveMeta\' | t:{ count: $ctrl.liveArrivalCount }) : (\'staff.td.kpiRequestedDefaultMeta\' | t) }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.td.kpiConfirmedToday\' | t }}</span>' +
                  '<app-icon name="check-circle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.kpis.confirmedToday">0</div>' +
                '<div class="kpi-meta">{{ \'staff.td.kpiConfirmedMeta\' | t:{ total: $ctrl.kpis.confirmedTotal } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.td.kpiCompleted\' | t }}</span>' +
                  '<app-icon name="flag-checkered" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);" kpi-counter="$ctrl.kpis.completed">0</div>' +
                '<div class="kpi-meta">{{ \'staff.td.kpiCompletedMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.td.kpiCancelled\' | t }}</span>' +
                  '<app-icon name="prohibit" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.kpis.cancelledOrNoShow">0</div>' +
                '<div class="kpi-meta">{{ \'staff.td.kpiCancelledMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Toolbar */
          '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
            '<div class="grid-12" style="align-items:end;">' +
              '<div class="col-3 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="stfTdDateSelect">{{ \'staff.td.preferredDateLabel\' | t }}</label>' +
                '<select id="stfTdDateSelect" class="form-control mono-num" ' +
                        'ng-model="$ctrl.filters.preferredDate" ' +
                        'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateFilterOptions" ' +
                        'ng-change="$ctrl.applyFilters()"></select>' +
              '</div>' +
              '<div class="col-3 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="stfTdStatusSelect">{{ \'common.status\' | t }}</label>' +
                '<select id="stfTdStatusSelect" class="form-control" ' +
                        'ng-model="$ctrl.filters.status" ' +
                        'ng-options="s.value as $ctrl.formatStatusOption(s) for s in $ctrl.statusOptions" ' +
                        'ng-change="$ctrl.applyFilters()"></select>' +
              '</div>' +
              '<div class="col-4 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="stfTdSearch">{{ \'staff.td.searchLabel\' | t }}</label>' +
                '<input id="stfTdSearch" type="search" class="form-control" ' +
                       'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                       'ng-attr-placeholder="{{ \'staff.td.searchPlaceholder\' | t }}" />' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">' +
                  '<app-icon name="funnel" weight="regular" size="15"></app-icon>' +
                  '<span>{{ \'common.clearFilters\' | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.testDrives.length === 0" variant="table" count="4"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredTestDrives.length === 0" ' +
                       'icon="steering-wheel" ' +
                       'title-key="staff.td.emptyTitle" ' +
                       'desc-key="staff.td.emptyDesc" ' +
                       'action-key="common.clearFilters" ' +
                       'on-action="$ctrl.resetFilters()">' +
          '</empty-state>' +

          /* Test-Drive Queue Cards */
          '<div style="display:flex;flex-direction:column;gap:14px;" ng-if="$ctrl.filteredTestDrives.length > 0">' +
            '<article class="bezel-card stagger-item" ' +
                     'ng-repeat="card in $ctrl.filteredTestDrives track by (card.id || card.bookingNumber)" ' +
                     'style="--i:{{ $index }};" ' +
                     'ng-class="{\'is-live-arrival is-signalr-pulse\': card.isLiveArrival}">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:14px;flex-wrap:wrap;">' +
                  '<div style="display:flex;gap:14px;align-items:flex-start;flex:1;min-width:260px;">' +
                    '<div style="width:88px;flex-shrink:0;">' +
                      '<bike-image src="$ctrl.resolveModelImage(card)" alt="{{ card.motorcycleModelName }}" variant="thumb"></bike-image>' +
                    '</div>' +
                    '<div style="flex:1;min-width:0;">' +
                      '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                        '<strong class="mono-num text-primary" style="font-size:16px;">{{ card.bookingNumber }}</strong>' +
                        '<span class="badge badge-danger" ng-if="card.isLiveArrival">' +
                          '<app-icon name="lightning" weight="fill" size="11"></app-icon>' +
                          '<span>{{ \'staff.liveArrivalTag\' | t }}</span>' +
                        '</span>' +
                        '<span class="badge" ng-class="\'badge-status-\' + card.status.toLowerCase()">' +
                          '{{ card.status | enumT:\'TestDriveStatus\' }}' +
                        '</span>' +
                        '<span class="badge badge-info mono-num" ng-if="card.assignedVin">' +
                          '{{ \'staff.td.demoVinBadgePrefix\' | t }}: {{ card.assignedVin }}' +
                        '</span>' +
                      '</div>' +
                      '<div class="text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">' +
                        '{{ card.motorcycleModelName }} • <span class="text-secondary" style="font-weight:600;">{{ card.customerFullName || (\'common.customer\' | t) }}</span>' +
                      '</div>' +
                      '<div class="mono-num text-muted" style="font-size:12px;margin-top:4px;">' +
                        '{{ \'staff.td.schedulePrefix\' | t }}: <strong class="text-primary">{{ card.preferredDate }} ({{ card.timeWindow }})</strong>' +
                        '<span ng-if="card.driverLicenseNumber"> • {{ \'staff.td.licensePrefix\' | t }}: {{ card.driverLicenseNumber }}</span>' +
                        '<span ng-if="card.contactPhone"> • {{ \'staff.td.phonePrefix\' | t }}: {{ card.contactPhone }}</span>' +
                      '</div>' +
                      '<div class="text-secondary" style="font-size:12px;margin-top:4px;" ng-if="card.customerNotes">' +
                        '{{ \'staff.td.customerNotesPrefix\' | t }}: {{ card.customerNotes }}' +
                      '</div>' +
                      '<div style="font-size:12px;color:var(--clr-danger-fg);margin-top:4px;" ng-if="card.cancellationReason">' +
                        '{{ \'staff.td.cancelReasonPrefix\' | t }}: {{ card.cancellationReason }}' +
                      '</div>' +
                    '</div>' +
                  '</div>' +

                  /* Right Action Cluster */
                  '<div style="display:flex;flex-direction:column;align-items:flex-end;gap:8px;min-width:260px;">' +
                    '<div style="width:100%;" ng-if="card.status === \'Requested\' || card.status === \'Confirmed\'" ' +
                         'has-permission="Permissions.TestDrives.UpdateStatus">' +
                      '<select class="form-control mono-num" style="min-height:34px;font-size:12px;padding:4px 8px;" ' +
                              'ng-attr-aria-label="{{ \'staff.td.demoVinAria\' | t }}" ' +
                              'ng-model="$ctrl.demoVinSelections[card.id]" ' +
                              'ng-options="u.id as (u.vin + \' • \' + (u.colorName || u.motorcycleModelName)) for u in $ctrl.getDemoUnitsForCard(card)">' +
                        '<option value="">{{ \'staff.td.demoVinPlaceholder\' | t }}</option>' +
                      '</select>' +
                    '</div>' +

                    '<div style="display:flex;gap:6px;flex-wrap:wrap;justify-content:flex-end;">' +
                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'Requested\'" ' +
                              'has-permission="Permissions.TestDrives.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyId === card.id" ' +
                              'ng-click="$ctrl.transitionStatus(card, \'Confirmed\')">' +
                        '<app-icon name="{{ $ctrl.busyId === card.id ? \'circle-notch\' : \'check-circle\' }}" weight="{{ $ctrl.busyId === card.id ? \'regular\' : \'fill\' }}" size="13" extra-class="{{ $ctrl.busyId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.td.actionConfirm\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.TestDrives.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyId === card.id" ' +
                              'ng-click="$ctrl.transitionStatus(card, \'Completed\')">' +
                        '<app-icon name="{{ $ctrl.busyId === card.id ? \'circle-notch\' : \'flag-checkered\' }}" weight="{{ $ctrl.busyId === card.id ? \'regular\' : \'fill\' }}" size="13" extra-class="{{ $ctrl.busyId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.td.actionComplete\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-if="card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.TestDrives.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyId === card.id" ' +
                              'ng-click="$ctrl.transitionStatus(card, \'NoShow\')">' +
                        '<app-icon name="{{ $ctrl.busyId === card.id ? \'circle-notch\' : \'prohibit\' }}" weight="regular" size="13" extra-class="{{ $ctrl.busyId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.td.actionNoShow\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-if="card.status === \'Requested\' || card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.TestDrives.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyId === card.id" ' +
                              'ng-click="$ctrl.openCancelModal(card)">' +
                        '<app-icon name="x-circle" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'staff.td.actionCancel\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'Confirmed\' || card.status === \'Completed\'" ' +
                              'has-permission="Permissions.SalesOrders.Create" ' +
                              'ng-click="$ctrl.convertToBikeSale(card)">' +
                        '<app-icon name="storefront" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'staff.td.actionConvertSale\' | t }}</span>' +
                        '<app-icon name="arrow-right" weight="regular" size="12"></app-icon>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</article>' +
          '</div>' +

          /* Advisor Cancel Test-Drive Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="stfTdCancelTitle" ng-if="$ctrl.cancellingCard">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-warning mono-num">PATCH /api/v1/test-drives/{{ $ctrl.cancellingCard.id }}/status</span>' +
                  '<h2 id="stfTdCancelTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'staff.td.cancelModalTitle\' | t:{ bookingNumber: $ctrl.cancellingCard.bookingNumber } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCancelModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +
              '<form name="$ctrl.staffCancelTdForm" ng-submit="$ctrl.confirmStaffCancel()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" ng-if="$ctrl.cancelModalProblem">' +
                    '<strong>{{ $ctrl.cancelModalProblem.title }}:</strong> {{ $ctrl.cancelModalProblem.detail }}' +
                  '</div>' +
                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="stfTdCancelReason">{{ \'staff.td.cancelReasonLabel\' | t }}</label>' +
                    '<textarea id="stfTdCancelReason" name="cancellationReason" rows="3" minlength="3" maxlength="300" class="form-control" ' +
                              'ng-model="$ctrl.staffCancelReason" required ' +
                              'ng-attr-placeholder="{{ \'staff.td.cancelReasonPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +
                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeCancelModal()" ng-disabled="$ctrl.busyId">{{ \'common.close\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.busyId || $ctrl.staffCancelTdForm.$invalid">' +
                    '<app-icon name="{{ $ctrl.busyId ? \'circle-notch\' : \'x-circle\' }}" weight="regular" size="15" extra-class="{{ $ctrl.busyId ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '<span>{{ \'staff.td.confirmCancelCta\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  StaffTestDrivesController.$inject = [
    '$q',
    '$rootScope',
    '$state',
    'AuthService',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function StaffTestDrivesController(
    $q,
    $rootScope,
    $state,
    AuthService,
    DealerApiService,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;
    var demoUnitsCacheByModel = Object.create(null);
    var modelImageById = Object.create(null);
    var modelImageByName = Object.create(null);
    var livePulseTimer = null;

    function triggerLivePulse() {
      if (isDestroyed) { return; }
      if (livePulseTimer) {
        clearTimeout(livePulseTimer);
        livePulseTimer = null;
      }
      $ctrl.livePulseActive = true;
      livePulseTimer = setTimeout(function () {
        livePulseTimer = null;
        if (isDestroyed) { return; }
        if ($rootScope && angular.isFunction($rootScope.$applyAsync)) {
          $rootScope.$applyAsync(function () {
            $ctrl.livePulseActive = false;
          });
        } else {
          $ctrl.livePulseActive = false;
        }
      }, 1300);
    }

    function t(key, params) {
      return (I18nService && angular.isFunction(I18nService.t)) ? I18nService.t(key, params) : key;
    }

    function enumT(group, code) {
      return (I18nService && angular.isFunction(I18nService.enumLabel))
        ? I18nService.enumLabel(group, code)
        : String(code || '');
    }

    function rebuildModelImageIndex(models) {
      modelImageById = Object.create(null);
      modelImageByName = Object.create(null);
      var arr = angular.isArray(models) ? models : [];
      for (var i = 0; i < arr.length; i++) {
        if (arr[i].imageUrl) {
          if (arr[i].id) {
            modelImageById[arr[i].id] = arr[i].imageUrl;
          }
          var fullLabel = String((arr[i].brand ? (arr[i].brand + ' ') : '') + (arr[i].modelName || '')).trim().toLowerCase();
          if (fullLabel) {
            modelImageByName[fullLabel] = arr[i].imageUrl;
          }
          if (arr[i].modelName) {
            modelImageByName[String(arr[i].modelName).trim().toLowerCase()] = arr[i].imageUrl;
          }
        }
      }
    }

    function rebuildDateFilterOptions() {
      var seen = Object.create(null);
      var opts = [{ value: '', isAll: true, isToday: false }];
      var today = DealerApiService.getTodayDateVn();
      seen[today] = true;
      opts.push({ value: today, isAll: false, isToday: true });

      var list = $ctrl.testDrives || [];
      for (var i = 0; i < list.length; i++) {
        var pd = list[i].preferredDate;
        if (pd && !seen[pd]) {
          seen[pd] = true;
          opts.push({ value: pd, isAll: false, isToday: false });
        }
      }
      $ctrl.dateFilterOptions = opts;
    }

    function recomputeKpis() {
      var today = DealerApiService.getTodayDateVn();
      var requested = 0;
      var confirmedToday = 0;
      var confirmedTotal = 0;
      var completed = 0;
      var cancelledOrNoShow = 0;

      var all = $ctrl.testDrives || [];
      for (var i = 0; i < all.length; i++) {
        var st = all[i].status;
        if (st === 'Requested') {
          requested++;
        } else if (st === 'Confirmed') {
          confirmedTotal++;
          if (all[i].preferredDate === today) {
            confirmedToday++;
          }
        } else if (st === 'Completed') {
          completed++;
        } else if (st === 'Cancelled' || st === 'NoShow') {
          cancelledOrNoShow++;
        }
      }

      $ctrl.kpis = {
        requested: requested,
        confirmedToday: confirmedToday,
        confirmedTotal: confirmedTotal,
        completed: completed,
        cancelledOrNoShow: cancelledOrNoShow
      };
    }

    function rebuildDemoUnitsIndex() {
      demoUnitsCacheByModel = Object.create(null);
      var units = ($ctrl.availableInventories || []).filter(function (u) {
        return u.stockStatus === 'Available';
      });
      demoUnitsCacheByModel.__ALL__ = units.map(function (u) { return angular.extend({}, u); });
      for (var i = 0; i < units.length; i++) {
        var mKey = units[i].motorcycleModelId || units[i].motorcycleModelName || '';
        if (mKey) {
          if (!demoUnitsCacheByModel[mKey]) {
            demoUnitsCacheByModel[mKey] = [];
          }
          demoUnitsCacheByModel[mKey].push(angular.extend({}, units[i]));
        }
      }
    }

    function syncDemoSelections() {
      var list = $ctrl.testDrives || [];
      for (var i = 0; i < list.length; i++) {
        var td = list[i];
        if (td.assignedInventoryId && !$ctrl.demoVinSelections[td.id]) {
          $ctrl.demoVinSelections[td.id] = td.assignedInventoryId;
        }
      }
    }

    function upsertOrPrependLiveTestDrive(payload) {
      if (isDestroyed || !payload) {
        return;
      }
      var normalized = DealerApiService.normalizeTestDrive(payload);
      if (!normalized.id && normalized.bookingNumber) {
        normalized.id = normalized.bookingNumber;
        normalized.testDriveId = normalized.bookingNumber;
      }
      normalized.isLiveArrival = true;

      var existingIdx = -1;
      for (var i = 0; i < $ctrl.testDrives.length; i++) {
        var cur = $ctrl.testDrives[i];
        var sameId = normalized.id && cur.id && cur.id === normalized.id;
        var sameNum = normalized.bookingNumber && cur.bookingNumber === normalized.bookingNumber;
        if (sameId || sameNum) {
          existingIdx = i;
          break;
        }
      }

      if (existingIdx !== -1) {
        $ctrl.testDrives[existingIdx] = angular.extend({}, $ctrl.testDrives[existingIdx], normalized, {
          rowVersion: normalized.rowVersion || $ctrl.testDrives[existingIdx].rowVersion,
          isLiveArrival: true
        });
      } else {
        $ctrl.testDrives.unshift(normalized);
        $ctrl.liveArrivalCount = ($ctrl.liveArrivalCount || 0) + 1;
      }

      $ctrl.latestLiveBanner = normalized;
      rebuildDateFilterOptions();
      recomputeKpis();
      $ctrl.applyFilters();

      if (normalized.testDriveId && normalized.testDriveId.length >= 32) {
        DealerApiService.getTestDriveById(normalized.testDriveId)
          .then(function (hydrated) {
            if (isDestroyed) { return; }
            for (var j = 0; j < $ctrl.testDrives.length; j++) {
              if ($ctrl.testDrives[j].id === hydrated.id || $ctrl.testDrives[j].bookingNumber === hydrated.bookingNumber) {
                hydrated.isLiveArrival = true;
                $ctrl.testDrives[j] = hydrated;
                break;
              }
            }
            syncDemoSelections();
            recomputeKpis();
            $ctrl.applyFilters();
          })
          .catch(angular.noop);
      }
    }

    $ctrl.formatDateOption = function (opt) {
      if (!opt) { return ''; }
      if (opt.isAll) { return t('staff.td.allDatesOption'); }
      if (opt.isToday) { return opt.value + ' ' + t('staff.td.todaySuffix'); }
      return opt.value;
    };

    $ctrl.formatStatusOption = function (opt) {
      if (!opt || !opt.value) {
        return t('staff.td.allStatusesOption');
      }
      return enumT('TestDriveStatus', opt.value);
    };

    $ctrl.resolveModelImage = function (card) {
      if (!card) { return ''; }
      if (card.imageUrl) { return card.imageUrl; }
      if (card.motorcycleModelId && modelImageById[card.motorcycleModelId]) {
        return modelImageById[card.motorcycleModelId];
      }
      if (card.motorcycleModelName) {
        var nameKey = String(card.motorcycleModelName).trim().toLowerCase();
        if (modelImageByName[nameKey]) {
          return modelImageByName[nameKey];
        }
      }
      return '';
    };

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.dealerId = user.dealerId || '';
      $ctrl.dealerCode = user.dealerCode || '';
      $ctrl.dealerName = user.dealerName || t('staff.td.defaultDealerName');

      $ctrl.isLoading = false;
      $ctrl.busyId = null;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.latestLiveBanner = null;
      $ctrl.livePulseActive = false;
      $ctrl.liveArrivalCount = 0;
      $ctrl.demoVinSelections = {};

      $ctrl.cancellingCard = null;
      $ctrl.staffCancelReason = '';
      $ctrl.cancelModalProblem = null;

      $ctrl.statusOptions = [
        { value: '' },
        { value: 'Requested' },
        { value: 'Confirmed' },
        { value: 'Completed' },
        { value: 'Cancelled' },
        { value: 'NoShow' }
      ];

      $ctrl.filters = {
        preferredDate: '',
        status: '',
        search: ''
      };

      $ctrl.testDrives = [];
      $ctrl.availableInventories = [];

      rebuildDemoUnitsIndex();
      syncDemoSelections();
      rebuildDateFilterOptions();
      recomputeKpis();
      $ctrl.applyFilters();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('TestDriveCreated', function (payload) {
          $rootScope.$applyAsync(function () {
            upsertOrPrependLiveTestDrive(payload);
          });
        }));

        cleanupFns.push(SignalRClientService.on('TestDriveStatusChanged', function (payload) {
          if (isDestroyed || !payload) { return; }
          $rootScope.$applyAsync(function () {
            var targetId = payload.testDriveId || payload.id || '';
            var targetNum = payload.bookingNumber || '';
            var matched = false;
            for (var i = 0; i < $ctrl.testDrives.length; i++) {
              if (
                (targetId && $ctrl.testDrives[i].id === targetId) ||
                (targetNum && $ctrl.testDrives[i].bookingNumber === targetNum)
              ) {
                $ctrl.testDrives[i].status = payload.newStatus || payload.status || $ctrl.testDrives[i].status;
                if (payload.assignedVin) {
                  $ctrl.testDrives[i].assignedVin = String(payload.assignedVin).toUpperCase();
                }
                matched = true;
              }
            }
            recomputeKpis();
            $ctrl.applyFilters();
            if (!matched) {
              $ctrl.loadDeskData();
            }
          });
        }));
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadDeskData();
          }
        }));

        cleanupFns.push($rootScope.$on('signalr:live-pulse', function (_evt, meta) {
          if (isDestroyed || !meta) {
            return;
          }
          if (meta.eventName === 'TestDriveCreated' || meta.eventName === 'TestDriveStatusChanged') {
            triggerLivePulse();
          }
        }));
      }

      $ctrl.loadDeskData();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (livePulseTimer) {
        clearTimeout(livePulseTimer);
        livePulseTimer = null;
      }
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.loadDeskData = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      var modelsPromise = angular.isFunction(DealerApiService.listMotorcycles)
        ? DealerApiService.listMotorcycles({ pageSize: 100 }).catch(function () { return []; })
        : $q.resolve([]);

      return $q.all({
        testDrives: DealerApiService.getDealerTestDrives({ pageSize: 100 }),
        inventories: DealerApiService.getInventories({ status: 'Available', pageSize: 100 }).catch(function () { return []; }),
        models: modelsPromise
      }).then(function (bundle) {
        if (isDestroyed) { return bundle.testDrives; }
        $ctrl.testDrives = bundle.testDrives || [];
        $ctrl.availableInventories = bundle.inventories || [];
        if (angular.isArray(bundle.models) && bundle.models.length > 0) {
          rebuildModelImageIndex(bundle.models);
        }
        rebuildDemoUnitsIndex();
        syncDemoSelections();
        rebuildDateFilterOptions();
        recomputeKpis();
        $ctrl.applyFilters();
        return $ctrl.testDrives;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return $ctrl.testDrives;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.getDemoUnitsForCard = function (card) {
      if (!card) {
        return demoUnitsCacheByModel.__ALL__ || [];
      }
      if (card.motorcycleModelId && demoUnitsCacheByModel[card.motorcycleModelId]) {
        return demoUnitsCacheByModel[card.motorcycleModelId];
      }
      if (card.motorcycleModelName && demoUnitsCacheByModel[card.motorcycleModelName]) {
        return demoUnitsCacheByModel[card.motorcycleModelName];
      }
      if (!card.motorcycleModelId && !card.motorcycleModelName) {
        return demoUnitsCacheByModel.__ALL__ || [];
      }
      return [];
    };

    $ctrl.applyFilters = function () {
      var dateFilter = ($ctrl.filters && $ctrl.filters.preferredDate) || '';
      var statusFilter = ($ctrl.filters && $ctrl.filters.status) || '';
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();

      $ctrl.filteredTestDrives = ($ctrl.testDrives || []).filter(function (td) {
        if (dateFilter && td.preferredDate !== dateFilter) {
          return false;
        }
        if (statusFilter && td.status !== statusFilter) {
          return false;
        }
        if (q) {
          var hay = (
            td.bookingNumber + ' ' +
            td.motorcycleModelName + ' ' +
            td.customerFullName + ' ' +
            td.driverLicenseNumber + ' ' +
            (td.assignedVin || '')
          ).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = { preferredDate: '', status: '', search: '' };
      $ctrl.applyFilters();
    };

    $ctrl.transitionStatus = function (card, nextStatus, cancelReasonText) {
      if (!card || $ctrl.busyId) {
        return $q.resolve(null);
      }
      var hasExplicitSelection = Boolean(
        $ctrl.demoVinSelections &&
        Object.prototype.hasOwnProperty.call($ctrl.demoVinSelections, card.id)
      );
      var rawSelectedInv = hasExplicitSelection
        ? $ctrl.demoVinSelections[card.id]
        : card.assignedInventoryId;
      var chosenInvId = rawSelectedInv ? (String(rawSelectedInv).trim() || null) : null;

      $ctrl.busyId = card.id;
      return DealerApiService.updateTestDriveStatus(card.id, {
        newStatus: nextStatus,
        assignedInventoryId: (nextStatus === 'Confirmed' || nextStatus === 'Completed') ? chosenInvId : null,
        staffNotes: cancelReasonText || t('staff.td.statusNoteTemplate', { status: nextStatus, dealer: $ctrl.dealerCode }),
        cancellationReason: nextStatus === 'Cancelled' ? (cancelReasonText || t('staff.td.cancelDefaultReason')) : null,
        rowVersion: card.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.testDrives.length; i++) {
          if ($ctrl.testDrives[i].id === card.id) {
            $ctrl.testDrives[i] = updated;
          }
        }
        recomputeKpis();
        $ctrl.applyFilters();
        ToastService.success(
          t('staff.td.toastUpdatedTitle'),
          t('staff.td.toastUpdatedMsg', {
            bookingNumber: updated.bookingNumber,
            status: enumT('TestDriveStatus', updated.status)
          })
        );
        if ($ctrl.cancellingCard && $ctrl.cancellingCard.id === card.id) {
          $ctrl.closeCancelModal();
        }
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        if ($ctrl.cancellingCard && $ctrl.cancellingCard.id === card.id) {
          $ctrl.cancelModalProblem = problem;
        } else {
          ToastService.warning(problem.title, problem.detail);
        }
        if (problem.status === 409) {
          $ctrl.loadDeskData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.busyId = null;
        }
      });
    };

    $ctrl.openCancelModal = function (card) {
      $ctrl.cancellingCard = card;
      $ctrl.staffCancelReason = t('staff.td.cancelDefaultReason');
      $ctrl.cancelModalProblem = null;
    };

    $ctrl.closeCancelModal = function () {
      $ctrl.cancellingCard = null;
      $ctrl.staffCancelReason = '';
      $ctrl.cancelModalProblem = null;
    };

    $ctrl.confirmStaffCancel = function () {
      if (!$ctrl.cancellingCard || ($ctrl.staffCancelTdForm && $ctrl.staffCancelTdForm.$invalid)) {
        return $q.resolve(null);
      }
      return $ctrl.transitionStatus($ctrl.cancellingCard, 'Cancelled', $ctrl.staffCancelReason);
    };

    $ctrl.convertToBikeSale = function (card) {
      if (!card) { return; }
      if (DealerApiService && angular.isFunction(DealerApiService.setSalesDeskHandoffContext)) {
        DealerApiService.setSalesDeskHandoffContext({
          testDriveId: card.id,
          bookingNumber: card.bookingNumber,
          customerId: card.customerId || '',
          customerEmail: card.customerEmail || '',
          customerFullName: card.customerFullName || '',
          motorcycleModelId: card.motorcycleModelId || '',
          motorcycleModelName: card.motorcycleModelName || '',
          assignedInventoryId: $ctrl.demoVinSelections[card.id] || card.assignedInventoryId || null
        });
      }
      if ($state && angular.isFunction($state.go)) {
        $state.go('app.staff.salesDesk');
      }
    };
  }
})();
