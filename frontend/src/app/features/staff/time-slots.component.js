(function () {
  'use strict';

  var VI_PATCH = {
    staff: {
      slots: {
        eyebrow: 'SCR-STF-04 • KHOANG XƯỞNG & SỨC CHỨA ROWVERSION',
        refreshing: 'Đang tải...',
        kpiWindows: 'Khung giờ đang hiển thị',
        kpiBooked: 'Khoang đã đặt',
        kpiUtilizationMeta: '{{pct}}% tỷ lệ lấp đầy ({{booked}}/{{cap}} khoang)',
        kpiOpenBays: 'Khoang trống sẵn sàng',
        kpiOpenMeta: 'Cho phép khách hàng đặt trực tuyến',
        kpiBlocked: 'Khung giờ tạm khóa',
        kpiBlockedMeta: 'Ngừng nhận đặt lịch mới',
        fromDateLabel: 'Từ ngày',
        toDateLabel: 'Đến ngày',
        todayOnlyCta: 'Chỉ hôm nay',
        nextSevenDaysCta: '7 ngày tới',
        todaySuffix: '(Hôm nay)',
        tomorrowSuffix: '(Ngày mai)',
        matrixTitle: 'Ma trận Công suất Khoang Xưởng theo Ngày & Giờ',
        matrixHint: 'Màu xanh: còn khoang • Vàng: còn 1 khoang cuối • Đỏ: kín khoang • Xám: tạm khóa',
        colBookedCap: 'Đã đặt / Sức chứa',
        colCapacityControl: 'Điều chỉnh sức chứa',
        colBayLock: 'Khóa khung giờ',
        baysOpenSummary: '{{booked}} / {{max}} khoang (Còn {{rem}})',
        statusBlocked: 'Tạm khóa',
        statusFull: 'Kín khoang',
        statusLastBay: '1 khoang cuối',
        statusAvailable: 'Còn khoang',
        decreaseCapAria: 'Giảm sức chứa khung giờ',
        increaseCapAria: 'Tăng sức chứa khung giờ',
        unblockCta: 'Mở khóa',
        blockCta: 'Khóa khoang',
        emptyTitle: 'Chưa có khung giờ xưởng cho khoảng ngày này',
        emptyDesc: 'Khởi tạo hàng loạt khung giờ làm việc cho chi nhánh để khách hàng có thể đặt lịch bảo dưỡng trực tuyến.',
        genModalTitle: 'Khởi tạo Hàng loạt Khung giờ Xưởng theo Ngày',
        genFromLabel: 'Từ ngày *',
        genToLabel: 'Đến ngày *',
        genDateOrderError: 'Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.',
        genDurationLabel: 'Thời lượng mỗi khung (Phút) *',
        genDuration60: '60 phút (Khoang tiêu chuẩn)',
        genDuration30: '30 phút (Khoang bảo dưỡng nhanh)',
        genCapLabel: 'Sức chứa tối đa mỗi khung (1–50) *',
        genIdempotentHint: 'Khởi tạo lũy đẳng: các khung (DealerId, SlotDate, StartTime) đã tồn tại trong SQL Server sẽ tự động được bỏ qua an toàn.',
        generatingCta: 'Đang khởi tạo...',
        generateSubmitCta: 'Khởi tạo khung giờ',
        toastCapUpdatedTitle: 'Đã cập nhật sức chứa',
        toastCapUpdatedMsg: '{{date}} ({{window}}) hiện có {{cap}} khoang.',
        toastBlockedTitle: 'Đã khóa khung giờ',
        toastUnblockedTitle: 'Đã mở khóa khung giờ',
        toastBlockUpdatedMsg: 'Đã cập nhật khung {{date}} ({{window}}).',
        toastGeneratedTitle: 'Đã khởi tạo khung giờ xưởng',
        toastGeneratedMsg: 'Đã xác nhận {{count}} khung giờ làm việc.',
        defaultDealerName: 'Đại lý ủy quyền'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      slots: {
        eyebrow: 'SCR-STF-04 • BAY CAPACITY & ROWVERSION',
        refreshing: 'Refreshing...',
        kpiWindows: 'Loaded Time Windows',
        kpiBooked: 'Booked Workshop Bays',
        kpiUtilizationMeta: '{{pct}}% utilization ({{booked}}/{{cap}} bays)',
        kpiOpenBays: 'Open Bookable Bays',
        kpiOpenMeta: 'Available for online booking',
        kpiBlocked: 'Blocked Windows',
        kpiBlockedMeta: 'Paused for new appointments',
        fromDateLabel: 'From Date',
        toDateLabel: 'To Date',
        todayOnlyCta: 'Today Only',
        nextSevenDaysCta: 'Next 7 Days',
        todaySuffix: '(Today)',
        tomorrowSuffix: '(Tomorrow)',
        matrixTitle: 'Dual-Axis Bay Capacity Matrix (Date × Time Window)',
        matrixHint: 'Green: Open • Amber: Last Bay • Red: Full • Slate: Blocked',
        colBookedCap: 'Booked / Max Capacity',
        colCapacityControl: 'Capacity Control',
        colBayLock: 'Bay Lock',
        baysOpenSummary: '{{booked}} / {{max}} bays ({{rem}} open)',
        statusBlocked: 'Blocked',
        statusFull: 'Full',
        statusLastBay: 'Last Bay',
        statusAvailable: 'Available',
        decreaseCapAria: 'Decrease slot capacity',
        increaseCapAria: 'Increase slot capacity',
        unblockCta: 'Unblock',
        blockCta: 'Block Bay',
        emptyTitle: 'No workshop time slots for this date range',
        emptyDesc: 'Batch-generate daily bay windows for your dealership branch so customers can book service online.',
        genModalTitle: 'Batch-Generate Daily Workshop Time Slots',
        genFromLabel: 'From Date *',
        genToLabel: 'To Date *',
        genDateOrderError: 'To date must be on or after From date.',
        genDurationLabel: 'Slot Duration (Minutes) *',
        genDuration60: '60 Minutes (Standard Bay)',
        genDuration30: '30 Minutes (Express Bay)',
        genCapLabel: 'Max Capacity per Slot (1–50) *',
        genIdempotentHint: 'Idempotent generation: any (DealerId, SlotDate, StartTime) window already present in SQL Server is safely skipped.',
        generatingCta: 'Generating...',
        generateSubmitCta: 'Generate Time Slots',
        toastCapUpdatedTitle: 'Capacity Updated',
        toastCapUpdatedMsg: '{{date}} ({{window}}) set to {{cap}} bays.',
        toastBlockedTitle: 'Slot Blocked',
        toastUnblockedTitle: 'Slot Unblocked',
        toastBlockUpdatedMsg: '{{date}} ({{window}}) updated.',
        toastGeneratedTitle: 'Workshop Slots Generated',
        toastGeneratedMsg: 'Verified {{count}} bay windows.',
        defaultDealerName: 'Authorized Dealership'
      }
    }
  };

  registerTimeSlotsI18n.$inject = ['I18nService'];
  function registerTimeSlotsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerTimeSlotsI18n)
    .component('staffTimeSlots', {
      controllerAs: '$ctrl',
      controller: StaffTimeSlotsController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="clock" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.slots.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.timeSlotsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">' +
                '<span class="mono-num">{{ $ctrl.dealerCode }}</span> ({{ $ctrl.dealerName }}) — {{ \'staff.timeSlotsSubtitle\' | t }}' +
              '</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadSlots()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrows-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'staff.slots.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.TimeSlots.ManageDealer" ' +
                      'ng-click="$ctrl.openGenerateModal()">' +
                '<span>{{ \'staff.generateSlotsCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="plus" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadSlots()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.slots.kpiWindows\' | t }}</span>' +
                  '<app-icon name="calendar" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.slots.length">0</div>' +
                '<div class="kpi-meta mono-num" style="display:inline-flex;align-items:center;gap:4px;">' +
                  '<span>{{ $ctrl.filters.fromDate }}</span>' +
                  '<app-icon name="arrow-right" weight="regular" size="11"></app-icon>' +
                  '<span>{{ $ctrl.filters.toDate }}</span>' +
                '</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.slots.kpiBooked\' | t }}</span>' +
                  '<app-icon name="wrench" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.totalBooked">0</div>' +
                '<div class="kpi-meta">{{ \'staff.slots.kpiUtilizationMeta\' | t:{ pct: $ctrl.utilizationPct, booked: $ctrl.totalBooked, cap: $ctrl.totalCapacity } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.slots.kpiOpenBays\' | t }}</span>' +
                  '<app-icon name="check-circle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);" kpi-counter="$ctrl.totalOpenBays">0</div>' +
                '<div class="kpi-meta">{{ \'staff.slots.kpiOpenMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.slots.kpiBlocked\' | t }}</span>' +
                  '<app-icon name="lock" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.blockedCount">0</div>' +
                '<div class="kpi-meta">{{ \'staff.slots.kpiBlockedMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Date Filter Toolbar */
          '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
            '<div class="grid-12" style="align-items:end;">' +
              '<div class="col-4 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="slotFromDate">{{ \'staff.slots.fromDateLabel\' | t }}</label>' +
                '<select id="slotFromDate" class="form-control mono-num" ' +
                        'ng-model="$ctrl.filters.fromDate" ' +
                        'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateOptions" ' +
                        'ng-change="$ctrl.onDateRangeChanged()"></select>' +
              '</div>' +
              '<div class="col-4 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="slotToDate">{{ \'staff.slots.toDateLabel\' | t }}</label>' +
                '<select id="slotToDate" class="form-control mono-num" ' +
                        'ng-model="$ctrl.filters.toDate" ' +
                        'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateOptions" ' +
                        'ng-change="$ctrl.onDateRangeChanged()"></select>' +
              '</div>' +
              '<div class="col-4 form-group" style="margin-bottom:0;display:flex;flex-direction:row;gap:8px;">' +
                '<button type="button" class="btn btn-secondary" style="flex:1;" ng-click="$ctrl.setSingleToday()">{{ \'staff.slots.todayOnlyCta\' | t }}</button>' +
                '<button type="button" class="btn btn-secondary" style="flex:1;" ng-click="$ctrl.setNextSevenDays()">{{ \'staff.slots.nextSevenDaysCta\' | t }}</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.slots.length === 0" variant="table" count="5"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.slots.length === 0" ' +
                       'icon="clock" ' +
                       'title-key="staff.slots.emptyTitle" ' +
                       'desc-key="staff.slots.emptyDesc" ' +
                       'action-key="staff.generateSlotsCta" ' +
                       'on-action="$ctrl.openGenerateModal()">' +
          '</empty-state>' +

          /* Dual-Axis Slot Capacity Matrix Overview */
          '<div class="bezel-card" style="margin-bottom:var(--space-5);" ng-if="$ctrl.groupedSlotsByDate.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<div class="surface-card-header">' +
                '<h2 class="surface-card-title">{{ \'staff.slots.matrixTitle\' | t }}</h2>' +
                '<span class="text-muted" style="font-size:12px;">{{ \'staff.slots.matrixHint\' | t }}</span>' +
              '</div>' +
              '<div style="display:flex;flex-direction:column;gap:12px;">' +
                '<div class="surface-sunken stagger-item" ng-attr-style="--i:{{ $index }};padding:12px 14px;" ng-repeat="day in $ctrl.groupedSlotsByDate track by day.slotDate">' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;margin-bottom:10px;flex-wrap:wrap;">' +
                    '<strong class="mono-num tabular-nums text-primary" style="display:inline-flex;align-items:center;gap:6px;">' +
                      '<app-icon name="calendar-blank" weight="duotone" size="15"></app-icon>' +
                      '<span>{{ day.slotDate }}</span>' +
                    '</strong>' +
                    '<span class="badge badge-neutral mono-num tabular-nums">{{ day.booked }}/{{ day.capacity }}</span>' +
                  '</div>' +
                  '<div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(148px,1fr));gap:8px;">' +
                    '<div class="slot-card" style="padding:10px;" ' +
                         'ng-repeat="s in day.slots track by s.slotId" ' +
                         'ng-class="{\'is-disabled\': s.isBlocked, \'is-last-bay\': !s.isBlocked && s.remainingCapacity === 1}">' +
                      '<div style="display:flex;justify-content:space-between;align-items:center;gap:4px;">' +
                        '<span class="mono-num" style="font-size:12px;font-weight:700;">{{ s.window }}</span>' +
                        '<app-icon name="{{ $ctrl.slotStatusIcon(s) }}" weight="fill" size="14"></app-icon>' +
                      '</div>' +
                      '<div class="mono-num text-secondary" style="font-size:11px;">{{ s.bookedCount }}/{{ s.maxCapacity }}</div>' +
                      '<div class="progress-track" style="height:6px;">' +
                        '<div class="progress-fill" ' +
                             'ng-class="{\'is-overdue\': s.remainingCapacity === 0, \'is-due\': s.remainingCapacity === 1}" ' +
                             'ng-style="{width: $ctrl.slotUtilizationPct(s) + \'%\'}"></div>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Quiet-Chrome Capacity Control Table */
          '<div class="bezel-card" ng-if="$ctrl.slots.length > 0">' +
            '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
              '<div class="table-wrap" style="max-height:62vh;overflow-y:auto;">' +
                '<table class="data-table">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'common.date\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'common.time\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.slots.colBookedCap\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.slots.colCapacityControl\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.slots.colBayLock\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr class="stagger-item" ng-repeat="slot in $ctrl.slots track by slot.slotId" ng-attr-style="--i:{{ $index }};">' +
                      '<td class="mono-num tabular-nums">{{ slot.slotDate }}</td>' +
                      '<td class="mono-num tabular-nums" style="font-weight:700;">{{ slot.window }}</td>' +
                      '<td style="min-width:190px;">' +
                        '<div class="mono-num tabular-nums text-secondary" style="font-size:12px;margin-bottom:4px;">' +
                          '{{ \'staff.slots.baysOpenSummary\' | t:{ booked: slot.bookedCount, max: slot.maxCapacity, rem: slot.remainingCapacity } }}' +
                        '</div>' +
                        '<div class="progress-track">' +
                          '<div class="progress-fill" ' +
                               'ng-class="{\'is-overdue\': slot.remainingCapacity === 0, \'is-due\': slot.remainingCapacity === 1}" ' +
                               'ng-style="{width: $ctrl.slotUtilizationPct(slot) + \'%\'}"></div>' +
                        '</div>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="$ctrl.slotStatusClass(slot)">' +
                          '<app-icon name="{{ $ctrl.slotStatusIcon(slot) }}" weight="fill" size="12"></app-icon>' +
                          '<span>{{ $ctrl.slotStatusLabel(slot) }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td style="text-align:right;">' +
                        '<div style="display:inline-flex;align-items:center;gap:6px;" has-permission="Permissions.TimeSlots.ManageDealer">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'ng-attr-aria-label="{{ \'staff.slots.decreaseCapAria\' | t }}" ' +
                                  'ng-disabled="$ctrl.busySlotId === slot.slotId || slot.maxCapacity <= 1 || slot.maxCapacity <= slot.bookedCount" ' +
                                  'ng-click="$ctrl.adjustSlotCapacity(slot, -1)">' +
                            '<app-icon name="minus" weight="regular" size="13"></app-icon>' +
                          '</button>' +
                          '<span class="mono-num tabular-nums" style="min-width:26px;text-align:center;font-weight:700;">{{ slot.maxCapacity }}</span>' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'ng-attr-aria-label="{{ \'staff.slots.increaseCapAria\' | t }}" ' +
                                  'ng-disabled="$ctrl.busySlotId === slot.slotId || slot.maxCapacity >= 50" ' +
                                  'ng-click="$ctrl.adjustSlotCapacity(slot, 1)">' +
                            '<app-icon name="plus" weight="regular" size="13"></app-icon>' +
                          '</button>' +
                        '</div>' +
                      '</td>' +
                      '<td style="text-align:right;">' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'has-permission="Permissions.TimeSlots.ManageDealer" ' +
                                'ng-disabled="$ctrl.busySlotId === slot.slotId" ' +
                                'ng-click="$ctrl.toggleSlotBlock(slot)">' +
                          '<app-icon name="{{ $ctrl.busySlotId === slot.slotId ? \'circle-notch\' : (slot.isBlocked ? \'lock-open\' : \'lock\') }}" weight="regular" size="13" extra-class="{{ $ctrl.busySlotId === slot.slotId ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                          '<span>{{ slot.isBlocked ? (\'staff.slots.unblockCta\' | t) : (\'staff.slots.blockCta\' | t) }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Generate Daily Slots Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="genSlotsTitle" ng-if="$ctrl.generateModalOpen">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">POST /api/v1/time-slots/generate</span>' +
                  '<h2 id="genSlotsTitle" class="surface-card-title" style="margin-top:4px;">{{ \'staff.slots.genModalTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeGenerateModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.generateForm" ng-submit="$ctrl.submitGenerateSlots()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.generateProblem">' +
                    '<strong>{{ $ctrl.generateProblem.title }}:</strong> {{ $ctrl.generateProblem.detail }}' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="genFromSelect">{{ \'staff.slots.genFromLabel\' | t }}</label>' +
                      '<select id="genFromSelect" class="form-control mono-num" ' +
                              'ng-model="$ctrl.generateDraft.fromDate" ' +
                              'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateOptions"></select>' +
                    '</div>' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="genToSelect">{{ \'staff.slots.genToLabel\' | t }}</label>' +
                      '<select id="genToSelect" class="form-control mono-num" ' +
                              'ng-model="$ctrl.generateDraft.toDate" ' +
                              'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.dateOptions"></select>' +
                      '<div class="field-error" ng-if="$ctrl.generateDraft.toDate < $ctrl.generateDraft.fromDate">{{ \'staff.slots.genDateOrderError\' | t }}</div>' +
                    '</div>' +
                    '<div class="col-6 form-group" style="margin-bottom:0;">' +
                      '<label class="form-label" for="genDurationSelect">{{ \'staff.slots.genDurationLabel\' | t }}</label>' +
                      '<select id="genDurationSelect" class="form-control mono-num" ' +
                              'ng-model="$ctrl.generateDraft.slotDurationMinutes" ' +
                              'ng-options="dur.value as (dur.labelKey | t) for dur in $ctrl.durationOptions"></select>' +
                    '</div>' +
                    '<div class="col-6 form-group" style="margin-bottom:0;">' +
                      '<label class="form-label" for="genCapInput">{{ \'staff.slots.genCapLabel\' | t }}</label>' +
                      '<input id="genCapInput" type="number" min="1" max="50" class="form-control mono-num" ' +
                             'ng-model="$ctrl.generateDraft.maxCapacity" required />' +
                    '</div>' +
                  '</div>' +

                  '<div class="field-hint" style="margin-top:12px;">{{ \'staff.slots.genIdempotentHint\' | t }}</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeGenerateModal()" ng-disabled="$ctrl.isGenerating">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'ng-disabled="$ctrl.isGenerating || $ctrl.generateForm.$invalid || $ctrl.generateDraft.toDate < $ctrl.generateDraft.fromDate">' +
                    '<app-icon name="{{ $ctrl.isGenerating ? \'circle-notch\' : \'calendar\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isGenerating ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '<span>{{ $ctrl.isGenerating ? (\'staff.slots.generatingCta\' | t) : (\'staff.slots.generateSubmitCta\' | t) }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  StaffTimeSlotsController.$inject = [
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
  function StaffTimeSlotsController(
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

    function t(key, params) {
      return (I18nService && angular.isFunction(I18nService.t)) ? I18nService.t(key, params) : key;
    }

    function recomputeMetrics() {
      var list = $ctrl.slots || [];
      var cap = 0;
      var booked = 0;
      var open = 0;
      var blocked = 0;
      var dayMap = Object.create(null);
      var grouped = [];

      for (var i = 0; i < list.length; i++) {
        var sl = list[i];
        cap += sl.maxCapacity;
        booked += sl.bookedCount;
        if (sl.isBlocked) {
          blocked++;
        } else {
          open += sl.remainingCapacity;
        }
        var dKey = sl.slotDate || '';
        if (!dayMap[dKey]) {
          dayMap[dKey] = {
            slotDate: dKey,
            booked: 0,
            capacity: 0,
            slots: []
          };
          grouped.push(dayMap[dKey]);
        }
        dayMap[dKey].booked += sl.bookedCount;
        dayMap[dKey].capacity += sl.maxCapacity;
        dayMap[dKey].slots.push(sl);
      }

      $ctrl.totalCapacity = cap;
      $ctrl.totalBooked = booked;
      $ctrl.totalOpenBays = open;
      $ctrl.blockedCount = blocked;
      $ctrl.utilizationPct = cap > 0 ? Math.round((booked / cap) * 100) : 0;
      $ctrl.groupedSlotsByDate = grouped;
    }

    $ctrl.formatDateOption = function (opt) {
      if (!opt) { return ''; }
      if (opt.offset === 0) { return opt.value + ' ' + t('staff.slots.todaySuffix'); }
      if (opt.offset === 1) { return opt.value + ' ' + t('staff.slots.tomorrowSuffix'); }
      return opt.value;
    };

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.dealerId = user.dealerId || '';
      $ctrl.dealerCode = user.dealerCode || '';
      $ctrl.dealerName = user.dealerName || t('staff.slots.defaultDealerName');

      var today = DealerApiService.getTodayDateVn();
      var opts = [];
      for (var d = -1; d <= 21; d++) {
        var iso = DealerApiService.addDaysDateVn(today, d);
        opts.push({
          value: iso,
          offset: d
        });
      }
      $ctrl.dateOptions = opts;
      $ctrl.durationOptions = [
        { value: 60, labelKey: 'staff.slots.genDuration60' },
        { value: 30, labelKey: 'staff.slots.genDuration30' }
      ];

      $ctrl.isLoading = false;
      $ctrl.busySlotId = null;
      $ctrl.listProblem = null;

      $ctrl.filters = {
        fromDate: today,
        toDate: DealerApiService.addDaysDateVn(today, 2)
      };

      $ctrl.generateModalOpen = false;
      $ctrl.isGenerating = false;
      $ctrl.generateProblem = null;
      $ctrl.generateDraft = {
        fromDate: today,
        toDate: DealerApiService.addDaysDateVn(today, 6),
        slotDurationMinutes: 60,
        maxCapacity: 5
      };

      $ctrl.loadProblem = null;
      $ctrl.slots = [];
      $ctrl.groupedSlotsByDate = [];
      recomputeMetrics();

      cleanupFns.push(SignalRClientService.on('BookingCreated', function () {
        $ctrl.loadSlots();
      }));

      cleanupFns.push(SignalRClientService.on('BookingStatusChanged', function () {
        $ctrl.loadSlots();
      }));

      cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
        $ctrl.loadSlots();
      }));

      $ctrl.loadSlots();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.slotUtilizationPct = function (slot) {
      if (!slot || !slot.maxCapacity) { return 0; }
      return Math.min(100, Math.round((slot.bookedCount / slot.maxCapacity) * 100));
    };

    $ctrl.slotStatusClass = function (slot) {
      if (!slot || slot.isBlocked) { return 'badge-neutral'; }
      if (slot.remainingCapacity <= 0) { return 'badge-danger'; }
      if (slot.remainingCapacity === 1) { return 'badge-warning'; }
      return 'badge-success';
    };

    $ctrl.slotStatusIcon = function (slot) {
      if (!slot || slot.isBlocked) { return 'prohibit'; }
      if (slot.remainingCapacity <= 0) { return 'x-circle'; }
      if (slot.remainingCapacity === 1) { return 'warning'; }
      return 'check-circle';
    };

    $ctrl.slotStatusLabel = function (slot) {
      if (!slot) { return ''; }
      if (slot.isBlocked) { return t('staff.slots.statusBlocked'); }
      if (slot.remainingCapacity <= 0) { return t('staff.slots.statusFull'); }
      if (slot.remainingCapacity === 1) { return t('staff.slots.statusLastBay'); }
      return t('staff.slots.statusAvailable');
    };

    $ctrl.onDateRangeChanged = function () {
      if ($ctrl.filters.toDate < $ctrl.filters.fromDate) {
        $ctrl.filters.toDate = $ctrl.filters.fromDate;
      }
      $ctrl.loadSlots();
    };

    $ctrl.setSingleToday = function () {
      var today = DealerApiService.getTodayDateVn();
      $ctrl.filters.fromDate = today;
      $ctrl.filters.toDate = today;
      $ctrl.loadSlots();
    };

    $ctrl.setNextSevenDays = function () {
      var today = DealerApiService.getTodayDateVn();
      $ctrl.filters.fromDate = today;
      $ctrl.filters.toDate = DealerApiService.addDaysDateVn(today, 6);
      $ctrl.loadSlots();
    };

    $ctrl.loadTimeSlots = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.listDealerTimeSlots({
        dealerId: $ctrl.dealerId || undefined,
        fromDate: $ctrl.filters.fromDate,
        toDate: $ctrl.filters.toDate
      }).then(function (rows) {
        if (isDestroyed) { return rows; }
        $ctrl.slots = rows;
        recomputeMetrics();
        return rows;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return $ctrl.slots;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.loadSlots = $ctrl.loadTimeSlots;

    $ctrl.adjustSlotCapacity = function (slot, delta) {
      if (!slot || $ctrl.busySlotId) { return $q.resolve(null); }
      var targetCap = slot.maxCapacity + delta;
      if (targetCap < Math.max(1, slot.bookedCount) || targetCap > 50) {
        return $q.resolve(null);
      }
      $ctrl.busySlotId = slot.slotId;
      return DealerApiService.updateTimeSlot(slot.slotId, {
        maxCapacity: targetCap,
        isBlocked: slot.isBlocked,
        rowVersion: slot.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.slots.length; i++) {
          if ($ctrl.slots[i].slotId === slot.slotId) {
            $ctrl.slots[i] = updated;
          }
        }
        recomputeMetrics();
        ToastService.success(
          t('staff.slots.toastCapUpdatedTitle'),
          t('staff.slots.toastCapUpdatedMsg', {
            date: updated.slotDate,
            window: updated.window,
            cap: updated.maxCapacity
          })
        );
        return updated;
      }).catch(function (rejection) {
        var problem = ProblemDetailsMapper.extract(rejection);
        ToastService.warning(problem.title, problem.detail);
        if (problem.status === 409) {
          $ctrl.loadTimeSlots();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.busySlotId = null;
        }
      });
    };

    $ctrl.toggleSlotBlock = function (slot) {
      if (!slot || $ctrl.busySlotId) { return $q.resolve(null); }
      $ctrl.busySlotId = slot.slotId;
      return DealerApiService.updateTimeSlot(slot.slotId, {
        maxCapacity: slot.maxCapacity,
        isBlocked: !slot.isBlocked,
        rowVersion: slot.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.slots.length; i++) {
          if ($ctrl.slots[i].slotId === slot.slotId) {
            $ctrl.slots[i] = updated;
          }
        }
        recomputeMetrics();
        ToastService.info(
          updated.isBlocked ? t('staff.slots.toastBlockedTitle') : t('staff.slots.toastUnblockedTitle'),
          t('staff.slots.toastBlockUpdatedMsg', { date: updated.slotDate, window: updated.window })
        );
        return updated;
      }).catch(function (rejection) {
        var problem = ProblemDetailsMapper.extract(rejection);
        ToastService.warning(problem.title, problem.detail);
        if (problem.status === 409) {
          $ctrl.loadTimeSlots();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.busySlotId = null;
        }
      });
    };

    $ctrl.openGenerateModal = function () {
      $ctrl.generateProblem = null;
      $ctrl.generateDraft = {
        fromDate: $ctrl.filters.fromDate,
        toDate: DealerApiService.addDaysDateVn($ctrl.filters.fromDate, 6),
        slotDurationMinutes: 60,
        maxCapacity: 5
      };
      $ctrl.generateModalOpen = true;
    };

    $ctrl.closeGenerateModal = function () {
      $ctrl.generateModalOpen = false;
      $ctrl.generateProblem = null;
    };

    $ctrl.submitGenerateSlots = function () {
      if ($ctrl.generateForm && $ctrl.generateForm.$invalid) {
        return $q.resolve(null);
      }
      $ctrl.isGenerating = true;
      $ctrl.generateProblem = null;

      return DealerApiService.generateTimeSlots($ctrl.generateDraft)
        .then(function (result) {
          if (isDestroyed) { return result; }
          $ctrl.filters.fromDate = $ctrl.generateDraft.fromDate;
          $ctrl.filters.toDate = $ctrl.generateDraft.toDate;
          ToastService.success(
            t('staff.slots.toastGeneratedTitle'),
            t('staff.slots.toastGeneratedMsg', { count: result.createdCount || result.slots.length })
          );
          $ctrl.closeGenerateModal();
          $ctrl.loadTimeSlots();
          return result;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            $ctrl.generateProblem = ProblemDetailsMapper.extract(rejection);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isGenerating = false;
          }
        });
    };
  }
})();
