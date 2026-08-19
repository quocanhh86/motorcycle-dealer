(function () {
  'use strict';

  var VI_PATCH = {
    staff: {
      wb: {
        eyebrow: 'SCR-STF-01 • ĐIỀU PHỐI TRỰC TIẾP',
        branchScopePrefix: 'Chi nhánh cố định:',
        syncQueue: 'Đồng bộ hàng đợi',
        syncing: 'Đang đồng bộ...',
        manageSlotsCta: 'Quản lý khung giờ xưởng',
        livePushTitle: 'LỊCH HẸN MỚI TRỰC TIẾP',
        showAllDates: 'Hiện tất cả ngày ({{count}} mới)',
        dismiss: 'Ẩn',
        kpiPending: 'Chờ xác nhận',
        kpiConfirmedCheckedIn: 'Đã xác nhận / Tiếp nhận',
        kpiInProgress: 'Đang sửa chữa tại khoang',
        kpiCompleted: 'Đã hoàn tất',
        kpiLiveHint: 'Cập nhật tự động qua SignalR',
        filterDateLabel: 'Lọc theo ngày làm việc',
        allDatesOption: 'Tất cả ngày có lịch',
        todaySuffix: '(Hôm nay)',
        searchLabel: 'Tìm theo mã lịch, số khung VIN 17 ký tự, biển số hoặc khách hàng',
        searchPlaceholder: 'Nhập mã lịch hẹn, số VIN 17 ký tự, biển số hoặc tên khách hàng...',
        lanePendingConfirmed: 'Chờ xác nhận',
        laneConfirmedCheckedIn: 'Đã xác nhận / Tiếp nhận',
        laneInBay: 'Đang sửa tại khoang',
        laneCompleted: 'Hoàn tất',
        emptyLaneNote: 'Chưa có lịch hẹn trong cột này',
        emptyBoardTitle: 'Không có lịch hẹn bảo dưỡng phù hợp',
        emptyBoardDesc: 'Lịch hẹn mới của khách hàng đặt cho chi nhánh sẽ tự động hiển thị tại đây qua SignalR.',
        defaultVehicle: 'Xe máy bảo dưỡng',
        voucherReserved: 'Đã giữ voucher',
        actionConfirm: 'Xác nhận',
        actionCheckIn: 'Tiếp nhận xe',
        actionStartWork: 'Bắt đầu sửa',
        actionSettleInvoice: 'Lập hóa đơn xưởng',
        actionNoShow: 'Không đến',
        actionCancel: 'Hủy lịch',
        cancelModalTitle: 'Hủy lịch hẹn {{bookingNumber}}',
        cancelReasonLabel: 'Lý do hủy của cố vấn dịch vụ * (3–300 ký tự)',
        cancelReasonPlaceholder: 'Nhập lý do hủy hoặc yêu cầu dời lịch của khách hàng...',
        cancelDefaultReason: 'Hủy tại bảng điều phối xưởng chi nhánh',
        confirmCancelCta: 'Xác nhận hủy & Giải phóng khoang',
        toastUpdatedTitle: 'Đã cập nhật lịch hẹn',
        toastUpdatedMsg: '{{bookingNumber}} đã chuyển sang trạng thái {{status}}.',
        statusNoteTemplate: 'Chuyển trạng thái sang {{status}}',
        defaultBranchName: 'Chi nhánh ủy quyền'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      wb: {
        eyebrow: 'SCR-STF-01 • LIVE WORKSHOP DISPATCH',
        branchScopePrefix: 'Branch scope:',
        syncQueue: 'Sync Queue',
        syncing: 'Syncing...',
        manageSlotsCta: 'Manage Bay Slots',
        livePushTitle: 'LIVE PUSH ARRIVED',
        showAllDates: 'Show All Dates ({{count}} new)',
        dismiss: 'Dismiss',
        kpiPending: 'Pending Confirmation',
        kpiConfirmedCheckedIn: 'Confirmed / Checked-In',
        kpiInProgress: 'In Service Bay',
        kpiCompleted: 'Completed',
        kpiLiveHint: 'Live SignalR feed',
        filterDateLabel: 'Filter Workshop Date',
        allDatesOption: 'All Scheduled Dates',
        todaySuffix: '(Today)',
        searchLabel: 'Filter by Booking #, 17-Char VIN, Plate, or Customer',
        searchPlaceholder: 'Search by booking number, 17-character VIN, license plate, or customer name...',
        lanePendingConfirmed: 'Pending Confirmation',
        laneConfirmedCheckedIn: 'Confirmed / Checked-In',
        laneInBay: 'In Service Bay',
        laneCompleted: 'Completed',
        emptyLaneNote: '0 appointments in this lane',
        emptyBoardTitle: 'No workshop appointments match filters',
        emptyBoardDesc: 'Incoming customer bookings for this dealership branch appear automatically via SignalR.',
        defaultVehicle: 'Workshop Motorcycle',
        voucherReserved: 'Voucher Reserved',
        actionConfirm: 'Confirm',
        actionCheckIn: 'Check-In',
        actionStartWork: 'Start Work',
        actionSettleInvoice: 'Settle & Issue Invoice',
        actionNoShow: 'No-Show',
        actionCancel: 'Cancel',
        cancelModalTitle: 'Cancel Booking {{bookingNumber}}',
        cancelReasonLabel: 'Advisor Cancellation Reason * (3–300 chars)',
        cancelReasonPlaceholder: 'Customer requested reschedule by phone...',
        cancelDefaultReason: 'Cancelled at dealership workbench',
        confirmCancelCta: 'Confirm Cancel & Release Bay',
        toastUpdatedTitle: 'Booking Updated',
        toastUpdatedMsg: '{{bookingNumber}} transitioned to {{status}}.',
        statusNoteTemplate: 'Status transitioned to {{status}}',
        defaultBranchName: 'Authorized Branch'
      }
    }
  };

  registerWorkbenchI18n.$inject = ['I18nService'];
  function registerWorkbenchI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerWorkbenchI18n)
    .component('staffWorkbench', {
      controllerAs: '$ctrl',
      controller: StaffWorkbenchController,
      template:
        '<section class="view-enter">' +
          /* Compact Workshop Editorial Strip */
          '<header class="atelier-hero" style="min-height:172px;padding:var(--space-5) var(--space-6);margin-bottom:var(--space-5);">' +
            '<picture>' +
              '<source srcset="/assets/editorial/workshop-bay-apex.webp" type="image/webp" />' +
              '<img class="atelier-hero-bg" src="/assets/editorial/workshop-bay-apex.jpg" alt="" loading="eager" decoding="async" />' +
            '</picture>' +
            '<div class="atelier-hero-overlay" aria-hidden="true"></div>' +
            '<div class="atelier-hero-content">' +
              '<div style="display:flex;justify-content:space-between;align-items:flex-end;gap:16px;flex-wrap:wrap;">' +
                '<div style="flex:1;min-width:260px;">' +
                  '<div class="atelier-hero-eyebrow" ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}">' +
                    '<app-icon name="wrench" weight="fill" size="13"></app-icon>' +
                    '<span>{{ \'staff.wb.eyebrow\' | t }}</span>' +
                    '<span class="mono-num" ng-if="$ctrl.dealerCode">• dealer-{{ $ctrl.dealerCode }}</span>' +
                  '</div>' +
                  '<h1 class="atelier-hero-title" style="font-size:clamp(1.4rem,2.4vw,1.95rem);margin-top:8px;">' +
                    '{{ \'staff.workbenchTitle\' | t }}' +
                  '</h1>' +
                  '<p class="atelier-hero-subtitle" style="margin-top:6px;font-size:var(--text-sm);">' +
                    '{{ \'staff.wb.branchScopePrefix\' | t }} <strong>{{ $ctrl.dealerName }}</strong> — {{ \'staff.workbenchSubtitle\' | t }}' +
                  '</p>' +
                '</div>' +
                '<div style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;">' +
                  '<button type="button" class="btn btn-ghost-dark" ng-click="$ctrl.loadWorkbench()" ng-disabled="$ctrl.isLoading">' +
                    '<app-icon name="arrows-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '<span>{{ $ctrl.isLoading ? (\'staff.wb.syncing\' | t) : (\'staff.wb.syncQueue\' | t) }}</span>' +
                  '</button>' +
                  '<a class="btn-pill-cta" ui-sref="app.staff.timeSlots">' +
                    '<span>{{ \'staff.wb.manageSlotsCta\' | t }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                    '</span>' +
                  '</a>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</header>' +

          /* Live SignalR Arrival Banner */
          '<div class="alert-banner alert-info fade-slide-up" role="status" ng-if="$ctrl.latestLiveBanner" ' +
               'ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
              '<app-icon name="lightning" weight="fill" size="18"></app-icon>' +
              '<strong>{{ \'staff.wb.livePushTitle\' | t }}:</strong>' +
              '<span class="mono-num" style="font-weight:700;">{{ $ctrl.latestLiveBanner.bookingNumber }}</span>' +
              '<span>• {{ $ctrl.latestLiveBanner.motorcycleModelName }} (<span class="mono-num">{{ $ctrl.latestLiveBanner.vin }}</span>)</span>' +
              '<span class="mono-num">• {{ $ctrl.latestLiveBanner.slotDate }} {{ $ctrl.latestLiveBanner.slotWindow }}</span>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-primary btn-sm" ' +
                      'ng-if="$ctrl.filters.slotDate && $ctrl.filters.slotDate !== $ctrl.latestLiveBanner.slotDate" ' +
                      'ng-click="$ctrl.showAllDates()">' +
                '{{ \'staff.wb.showAllDates\' | t:{ count: $ctrl.otherDateArrivalCount } }}' +
              '</button>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.dismissLiveBanner()">' +
                '{{ \'staff.wb.dismiss\' | t }}' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadWorkbench()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.wb.kpiPending\' | t }}</span>' +
                  '<app-icon name="clock-countdown" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.lanes[0].items.length">0</div>' +
                '<div class="kpi-meta">{{ \'staff.wb.kpiLiveHint\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.wb.kpiConfirmedCheckedIn\' | t }}</span>' +
                  '<app-icon name="sign-in" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.lanes[1].items.length">0</div>' +
                '<div class="kpi-meta">{{ \'enums.BookingStatus.Confirmed\' | t }} • {{ \'enums.BookingStatus.CheckedIn\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.wb.kpiInProgress\' | t }}</span>' +
                  '<app-icon name="wrench" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.lanes[2].items.length">0</div>' +
                '<div class="kpi-meta">{{ \'enums.BookingStatus.InProgress\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.wb.kpiCompleted\' | t }}</span>' +
                  '<app-icon name="check-circle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);" kpi-counter="$ctrl.lanes[3].items.length">0</div>' +
                '<div class="kpi-meta">{{ \'enums.BookingStatus.Completed\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Toolbar */
          '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
            '<div class="grid-12" style="align-items:end;">' +
              '<div class="col-4 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="wbDateSelect">{{ \'staff.wb.filterDateLabel\' | t }}</label>' +
                '<select id="wbDateSelect" class="form-control mono-num" ' +
                        'ng-model="$ctrl.filters.slotDate" ' +
                        'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateFilterOptions" ' +
                        'ng-change="$ctrl.rebuildLanes()"></select>' +
              '</div>' +
              '<div class="col-6 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="wbSearchInput">{{ \'staff.wb.searchLabel\' | t }}</label>' +
                '<input id="wbSearchInput" type="search" class="form-control" ' +
                       'ng-model="$ctrl.filters.search" ng-change="$ctrl.rebuildLanes()" ' +
                       'ng-attr-placeholder="{{ \'staff.wb.searchPlaceholder\' | t }}" />' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">' +
                  '<app-icon name="funnel" weight="regular" size="15"></app-icon>' +
                  '<span>{{ \'common.clearFilters\' | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Loading Skeleton */
          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.bookings.length === 0" variant="cards" count="4"></skeleton-loader>' +

          /* Empty Board State */
          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredTotal === 0" ' +
                       'icon="wrench" ' +
                       'title-key="staff.wb.emptyBoardTitle" ' +
                       'desc-key="staff.wb.emptyBoardDesc" ' +
                       'action-key="common.clearFilters" ' +
                       'on-action="$ctrl.resetFilters()">' +
          '</empty-state>' +

          /* 4-Lane Quiet-Chrome Double-Bezel Kanban Board */
          '<div class="bezel-card" ng-if="!$ctrl.isLoading || $ctrl.bookings.length > 0" ng-show="$ctrl.filteredTotal > 0 || $ctrl.isLoading">' +
            '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
              '<div class="kanban-board">' +
                '<div class="kanban-lane" style="max-height:70vh;overflow-y:auto;" ng-repeat="lane in $ctrl.lanes track by lane.key">' +
                  '<div class="kanban-lane-header" style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);padding:4px 0 8px;">' +
                    '<span style="display:inline-flex;align-items:center;gap:6px;">' +
                      '<app-icon name="{{ lane.icon }}" weight="duotone" size="16"></app-icon>' +
                      '<span>{{ lane.labelKey | t }}</span>' +
                    '</span>' +
                    '<span class="badge badge-neutral mono-num tabular-nums">{{ lane.items.length }}</span>' +
                  '</div>' +

                  '<div ng-if="!$ctrl.isLoading && lane.items.length === 0" class="text-muted" ' +
                       'style="font-size:var(--text-xs);padding:16px 8px;text-align:center;border:1px dashed var(--clr-border-subtle);border-radius:var(--radius-md);">' +
                    '<app-icon name="wrench" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                    '<div style="margin-top:4px;">{{ \'staff.wb.emptyLaneNote\' | t }}</div>' +
                  '</div>' +

                  '<article class="kanban-card stagger-item" ' +
                           'ng-repeat="card in lane.items track by card.bookingNumber" ' +
                           'style="--i:{{ $index }};" ' +
                           'ng-class="{\'is-live-arrival is-signalr-pulse\': card.isLiveArrival}">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;flex-wrap:wrap;">' +
                      '<strong class="mono-num tabular-nums text-primary">{{ card.bookingNumber }}</strong>' +
                      '<div style="display:flex;gap:4px;align-items:center;flex-wrap:wrap;">' +
                        '<span class="badge badge-danger" ng-if="card.isLiveArrival">' +
                          '<app-icon name="lightning" weight="fill" size="11"></app-icon>' +
                          '<span>{{ \'staff.liveArrivalTag\' | t }}</span>' +
                        '</span>' +
                        '<span class="badge" ng-class="\'badge-status-\' + card.status.toLowerCase()">' +
                          '{{ card.status | enumT:\'BookingStatus\' }}' +
                        '</span>' +
                      '</div>' +
                    '</div>' +

                    '<div style="display:flex;gap:10px;align-items:flex-start;margin-top:8px;">' +
                      '<div style="width:72px;flex-shrink:0;">' +
                        '<bike-image src="$ctrl.resolveCardImage(card)" alt="{{ card.motorcycleModelName }}" variant="thumb"></bike-image>' +
                      '</div>' +
                      '<div style="flex:1;min-width:0;">' +
                        '<div class="text-primary" style="font-size:13px;font-weight:700;">' +
                          '{{ card.motorcycleModelName || (\'staff.wb.defaultVehicle\' | t) }}' +
                        '</div>' +
                        '<div class="mono-num tabular-nums text-secondary" style="font-size:12px;margin-top:2px;">' +
                          '{{ \'common.vin\' | t }}: {{ card.vin }}' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:12px;margin-top:4px;">' +
                          '<div class="mono-num tabular-nums" style="display:inline-flex;align-items:center;gap:4px;">' +
                            '<app-icon name="clock" weight="regular" size="12"></app-icon>' +
                            '<span>{{ card.slotDate }} • {{ card.slotWindow }}</span>' +
                          '</div>' +
                          '<div style="margin-top:2px;">' +
                            '{{ card.customerFullName || (\'common.customer\' | t) }} • {{ card.serviceType | enumT:\'ServiceType\' }}' +
                          '</div>' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    '<div style="margin-top:6px;" ng-if="card.hasReservedVoucher || card.reservedVoucherCode">' +
                      '<span class="badge badge-info mono-num">' +
                        '<app-icon name="ticket" weight="duotone" size="13"></app-icon>' +
                        '<span>{{ card.reservedVoucherCode || (\'staff.wb.voucherReserved\' | t) }}</span>' +
                      '</span>' +
                    '</div>' +

                    '<div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:10px;padding-top:8px;border-top:1px solid var(--clr-border-subtle);" ' +
                         'ng-if="card.status !== \'Completed\' && card.status !== \'Cancelled\' && card.status !== \'NoShow\'">' +
                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'Pending\'" ' +
                              'has-permission="Permissions.Bookings.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyBookingId === card.id" ' +
                              'ng-click="$ctrl.transitionBooking(card, \'Confirmed\')">' +
                        '<app-icon name="{{ $ctrl.busyBookingId === card.id ? \'circle-notch\' : \'check\' }}" weight="regular" size="13" extra-class="{{ $ctrl.busyBookingId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.wb.actionConfirm\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.Bookings.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyBookingId === card.id" ' +
                              'ng-click="$ctrl.transitionBooking(card, \'CheckedIn\')">' +
                        '<app-icon name="{{ $ctrl.busyBookingId === card.id ? \'circle-notch\' : \'sign-in\' }}" weight="regular" size="13" extra-class="{{ $ctrl.busyBookingId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.wb.actionCheckIn\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-primary btn-sm" ' +
                              'ng-if="card.status === \'CheckedIn\'" ' +
                              'has-permission="Permissions.Bookings.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyBookingId === card.id" ' +
                              'ng-click="$ctrl.transitionBooking(card, \'InProgress\')">' +
                        '<app-icon name="{{ $ctrl.busyBookingId === card.id ? \'circle-notch\' : \'play\' }}" weight="{{ $ctrl.busyBookingId === card.id ? \'regular\' : \'fill\' }}" size="12" extra-class="{{ $ctrl.busyBookingId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.wb.actionStartWork\' | t }}</span>' +
                      '</button>' +

                      '<a class="btn btn-primary btn-sm" ' +
                         'ng-if="card.status === \'Confirmed\' || card.status === \'CheckedIn\' || card.status === \'InProgress\'" ' +
                         'has-permission="Permissions.Invoices.CreateDealer" ' +
                         'ui-sref="app.staff.invoiceBuilder({ bookingId: card.id })">' +
                        '<app-icon name="receipt" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'staff.wb.actionSettleInvoice\' | t }}</span>' +
                        '<app-icon name="arrow-right" weight="regular" size="12"></app-icon>' +
                      '</a>' +

                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-if="card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.Bookings.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyBookingId === card.id" ' +
                              'ng-click="$ctrl.transitionBooking(card, \'NoShow\')">' +
                        '<app-icon name="{{ $ctrl.busyBookingId === card.id ? \'circle-notch\' : \'prohibit\' }}" weight="regular" size="13" extra-class="{{ $ctrl.busyBookingId === card.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                        '<span>{{ \'staff.wb.actionNoShow\' | t }}</span>' +
                      '</button>' +

                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-if="card.status === \'Pending\' || card.status === \'Confirmed\'" ' +
                              'has-permission="Permissions.Bookings.UpdateStatus" ' +
                              'ng-disabled="$ctrl.busyBookingId === card.id" ' +
                              'ng-click="$ctrl.openStaffCancelModal(card)">' +
                        '<app-icon name="x-circle" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'staff.wb.actionCancel\' | t }}</span>' +
                      '</button>' +
                    '</div>' +
                  '</article>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Staff Cancel Booking Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="stfCancelTitle" ng-if="$ctrl.cancellingCard">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-warning mono-num">PATCH /api/v1/bookings/{{ $ctrl.cancellingCard.id }}/status</span>' +
                  '<h2 id="stfCancelTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'staff.wb.cancelModalTitle\' | t:{ bookingNumber: $ctrl.cancellingCard.bookingNumber } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeStaffCancelModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +
              '<form name="$ctrl.staffCancelForm" ng-submit="$ctrl.confirmStaffCancel()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" ng-if="$ctrl.cancelModalProblem">' +
                    '<strong>{{ $ctrl.cancelModalProblem.title }}:</strong> {{ $ctrl.cancelModalProblem.detail }}' +
                  '</div>' +
                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="staffCancelReason">{{ \'staff.wb.cancelReasonLabel\' | t }}</label>' +
                    '<textarea id="staffCancelReason" name="cancellationReason" rows="3" minlength="3" maxlength="300" class="form-control" ' +
                              'ng-model="$ctrl.staffCancelReason" required ' +
                              'ng-attr-placeholder="{{ \'staff.wb.cancelReasonPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +
                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeStaffCancelModal()" ng-disabled="$ctrl.busyBookingId">' +
                    '{{ \'common.close\' | t }}' +
                  '</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.busyBookingId || $ctrl.staffCancelForm.$invalid">' +
                    '<app-icon name="{{ $ctrl.busyBookingId ? \'circle-notch\' : \'x-circle\' }}" weight="regular" size="15" extra-class="{{ $ctrl.busyBookingId ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '<span>{{ \'staff.wb.confirmCancelCta\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  StaffWorkbenchController.$inject = [
    '$q',
    '$rootScope',
    'AuthService',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function StaffWorkbenchController(
    $q,
    $rootScope,
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

    function hydrateModelImageIndex() {
      if (!DealerApiService || !angular.isFunction(DealerApiService.listMotorcycles)) {
        return;
      }
      DealerApiService.listMotorcycles({ pageSize: 100 })
        .then(function (models) {
          if (isDestroyed || !angular.isArray(models)) {
            return;
          }
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

    function rebuildDateFilterOptions() {
      var seen = Object.create(null);
      var opts = [{ value: '', isAll: true, isToday: false }];
      var today = DealerApiService.getTodayDateVn();
      seen[today] = true;
      opts.push({ value: today, isAll: false, isToday: true });

      var list = $ctrl.bookings || [];
      for (var i = 0; i < list.length; i++) {
        var sd = list[i].slotDate;
        if (sd && !seen[sd]) {
          seen[sd] = true;
          opts.push({ value: sd, isAll: false, isToday: false });
        }
      }
      $ctrl.dateFilterOptions = opts;
    }

    function matchesFilters(card) {
      var dateFilter = ($ctrl.filters && $ctrl.filters.slotDate) || '';
      if (dateFilter && card.slotDate !== dateFilter) {
        return false;
      }
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      if (q) {
        var hay = (
          card.bookingNumber + ' ' +
          card.vin + ' ' +
          (card.licensePlate || '') + ' ' +
          card.customerFullName + ' ' +
          card.motorcycleModelName
        ).toLowerCase();
        if (hay.indexOf(q) === -1) {
          return false;
        }
      }
      return true;
    }

    function upsertOrPrependLiveBooking(payload) {
      if (isDestroyed || !payload) {
        return;
      }
      var normalized = DealerApiService.normalizeBooking(payload);
      if (!normalized.id && normalized.bookingNumber) {
        normalized.id = normalized.bookingNumber;
        normalized.bookingId = normalized.bookingNumber;
      }
      normalized.isLiveArrival = true;

      var existingIndex = -1;
      for (var i = 0; i < $ctrl.bookings.length; i++) {
        var cur = $ctrl.bookings[i];
        var sameId = normalized.id && cur.id && cur.id === normalized.id;
        var sameNumber = normalized.bookingNumber && cur.bookingNumber === normalized.bookingNumber;
        if (sameId || sameNumber) {
          existingIndex = i;
          break;
        }
      }

      if (existingIndex !== -1) {
        var merged = angular.extend({}, $ctrl.bookings[existingIndex], normalized, {
          rowVersion: normalized.rowVersion || $ctrl.bookings[existingIndex].rowVersion,
          isLiveArrival: true
        });
        $ctrl.bookings[existingIndex] = merged;
      } else {
        $ctrl.bookings.unshift(normalized);
      }

      if ($ctrl.filters.slotDate && normalized.slotDate && $ctrl.filters.slotDate !== normalized.slotDate) {
        $ctrl.otherDateArrivalCount = ($ctrl.otherDateArrivalCount || 0) + 1;
      }

      $ctrl.latestLiveBanner = normalized;
      rebuildDateFilterOptions();
      $ctrl.rebuildLanes();

      if (normalized.bookingId && normalized.bookingId.length >= 32) {
        DealerApiService.getBookingById(normalized.bookingId)
          .then(function (hydrated) {
            if (isDestroyed) { return; }
            for (var j = 0; j < $ctrl.bookings.length; j++) {
              if ($ctrl.bookings[j].id === hydrated.id || $ctrl.bookings[j].bookingNumber === hydrated.bookingNumber) {
                hydrated.isLiveArrival = true;
                $ctrl.bookings[j] = hydrated;
                break;
              }
            }
            $ctrl.rebuildLanes();
          })
          .catch(angular.noop);
      }
    }

    $ctrl.formatDateOption = function (opt) {
      if (!opt) { return ''; }
      if (opt.isAll) { return t('staff.wb.allDatesOption'); }
      if (opt.isToday) { return opt.value + ' ' + t('staff.wb.todaySuffix'); }
      return opt.value;
    };

    $ctrl.resolveCardImage = function (card) {
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
      $ctrl.dealerName = user.dealerName || t('staff.wb.defaultBranchName');

      $ctrl.isLoading = false;
      $ctrl.busyBookingId = null;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.latestLiveBanner = null;
      $ctrl.livePulseActive = false;
      $ctrl.otherDateArrivalCount = 0;
      $ctrl.filteredTotal = 0;

      $ctrl.cancellingCard = null;
      $ctrl.staffCancelReason = '';
      $ctrl.cancelModalProblem = null;

      $ctrl.filters = {
        slotDate: '',
        search: ''
      };

      $ctrl.lanes = [
        { key: 'Pending', status: 'Pending', labelKey: 'staff.wb.lanePendingConfirmed', icon: 'clock-countdown', items: [] },
        { key: 'Confirmed', status: 'CheckedIn', labelKey: 'staff.wb.laneConfirmedCheckedIn', icon: 'sign-in', items: [] },
        { key: 'InProgress', status: 'InProgress', labelKey: 'staff.wb.laneInBay', icon: 'wrench', items: [] },
        { key: 'Completed', status: 'Completed', labelKey: 'staff.wb.laneCompleted', icon: 'check-circle', items: [] }
      ];

      $ctrl.bookings = [];
      rebuildDateFilterOptions();
      $ctrl.rebuildLanes();
      hydrateModelImageIndex();

      cleanupFns.push(SignalRClientService.on('BookingCreated', function (payload) {
        $rootScope.$applyAsync(function () {
          upsertOrPrependLiveBooking(payload);
        });
      }));

      cleanupFns.push(SignalRClientService.on('BookingStatusChanged', function (payload) {
        if (isDestroyed || !payload) {
          return;
        }
        $rootScope.$applyAsync(function () {
          var matched = false;
          for (var i = 0; i < $ctrl.bookings.length; i++) {
            if (
              $ctrl.bookings[i].id === payload.bookingId ||
              $ctrl.bookings[i].bookingNumber === payload.bookingNumber
            ) {
              $ctrl.bookings[i].status = payload.newStatus || $ctrl.bookings[i].status;
              matched = true;
            }
          }
          $ctrl.rebuildLanes();
          if (!matched) {
            $ctrl.loadWorkbench();
          }
        });
      }));

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadWorkbench();
          }
        }));

        cleanupFns.push($rootScope.$on('signalr:live-pulse', function (_evt, meta) {
          if (isDestroyed || !meta) {
            return;
          }
          if (meta.eventName === 'BookingCreated' || meta.eventName === 'BookingStatusChanged') {
            triggerLivePulse();
          }
        }));
      }

      $ctrl.loadWorkbench();
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

    $ctrl.rebuildLanes = function () {
      var pending = [];
      var confirmedOrCheckedIn = [];
      var inProgress = [];
      var completed = [];

      var all = $ctrl.bookings || [];
      for (var i = 0; i < all.length; i++) {
        var card = all[i];
        if (!matchesFilters(card)) {
          continue;
        }
        if (card.status === 'Pending') {
          pending.push(card);
        } else if (card.status === 'Confirmed' || card.status === 'CheckedIn') {
          confirmedOrCheckedIn.push(card);
        } else if (card.status === 'InProgress') {
          inProgress.push(card);
        } else if (card.status === 'Completed') {
          completed.push(card);
        }
      }

      $ctrl.lanes[0].items = pending;
      $ctrl.lanes[1].items = confirmedOrCheckedIn;
      $ctrl.lanes[2].items = inProgress;
      $ctrl.lanes[3].items = completed;
      $ctrl.filteredTotal = pending.length + confirmedOrCheckedIn.length + inProgress.length + completed.length;
    };

    $ctrl.loadWorkbench = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.listBookings({ pageSize: 100 })
        .then(function (rows) {
          if (isDestroyed) {
            return rows;
          }
          $ctrl.bookings = rows;
          rebuildDateFilterOptions();
          $ctrl.rebuildLanes();
          return rows;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.bookings;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = { slotDate: '', search: '' };
      $ctrl.otherDateArrivalCount = 0;
      $ctrl.rebuildLanes();
    };

    $ctrl.showAllDates = function () {
      $ctrl.filters.slotDate = '';
      $ctrl.otherDateArrivalCount = 0;
      $ctrl.rebuildLanes();
    };

    $ctrl.dismissLiveBanner = function () {
      $ctrl.latestLiveBanner = null;
    };

    $ctrl.transitionBooking = function (card, nextStatus, reasonText) {
      if (!card || $ctrl.busyBookingId) {
        return $q.resolve(null);
      }

      $ctrl.busyBookingId = card.id;
      return DealerApiService.updateBookingStatus(card.id, {
        newStatus: nextStatus,
        cancellationReason: reasonText || null,
        notes: reasonText || t('staff.wb.statusNoteTemplate', { status: nextStatus }),
        rowVersion: card.rowVersion
      }).then(function (updated) {
        if (isDestroyed) {
          return updated;
        }
        for (var i = 0; i < $ctrl.bookings.length; i++) {
          if ($ctrl.bookings[i].id === card.id) {
            $ctrl.bookings[i] = updated;
          }
        }
        $ctrl.rebuildLanes();
        ToastService.success(
          t('staff.wb.toastUpdatedTitle'),
          t('staff.wb.toastUpdatedMsg', {
            bookingNumber: updated.bookingNumber,
            status: enumT('BookingStatus', updated.status)
          })
        );
        if ($ctrl.cancellingCard && $ctrl.cancellingCard.id === card.id) {
          $ctrl.closeStaffCancelModal();
        }
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) {
          return null;
        }
        var problem = ProblemDetailsMapper.extract(rejection);
        if ($ctrl.cancellingCard && $ctrl.cancellingCard.id === card.id) {
          $ctrl.cancelModalProblem = problem;
        } else {
          ToastService.warning(problem.title, problem.detail);
        }
        if (problem.status === 409) {
          $ctrl.loadWorkbench();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.busyBookingId = null;
        }
      });
    };

    $ctrl.openStaffCancelModal = function (card) {
      $ctrl.cancellingCard = card;
      $ctrl.staffCancelReason = t('staff.wb.cancelDefaultReason');
      $ctrl.cancelModalProblem = null;
    };

    $ctrl.closeStaffCancelModal = function () {
      $ctrl.cancellingCard = null;
      $ctrl.staffCancelReason = '';
      $ctrl.cancelModalProblem = null;
    };

    $ctrl.confirmStaffCancel = function () {
      if (!$ctrl.cancellingCard || ($ctrl.staffCancelForm && $ctrl.staffCancelForm.$invalid)) {
        return $q.resolve(null);
      }
      return $ctrl.transitionBooking($ctrl.cancellingCard, 'Cancelled', $ctrl.staffCancelReason);
    };
  }
})();
