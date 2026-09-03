(function () {
  'use strict';

  var TEST_DRIVE_WINDOWS = [
    { key: '09:00-09:45', startTime: '09:00', endTime: '09:45' },
    { key: '10:00-10:45', startTime: '10:00', endTime: '10:45' },
    { key: '14:00-14:45', startTime: '14:00', endTime: '14:45' },
    { key: '15:30-16:15', startTime: '15:30', endTime: '16:15' }
  ];

  var TD_STEPPER_STAGES = [
    { code: 'Requested' },
    { code: 'Confirmed' },
    { code: 'Completed' }
  ];

  var TD_STAGE_INDEX = {
    Requested: 0,
    Confirmed: 1,
    Completed: 2
  };

  var VI_PATCH = {
    customerTestDrives: {
      eyebrow: 'LỊCH LÁI THỬ SHOWROOM • SIGNALR TRỰC TIẾP',
      refreshing: 'Đang làm mới...',
      browseShowroom: 'Xem Showroom',
      createdTitle: 'Đã gửi yêu cầu lái thử:',
      createdNotified: 'Nhân viên đại lý đã nhận thông báo thời gian thực.',
      dismiss: 'Ẩn',
      formTitle: 'Đăng ký lái thử xe tại Showroom',
      modelLabel: 'Mẫu xe máy muốn lái thử *',
      modelPlaceholder: '-- Chọn mẫu xe --',
      dealerLabel: 'Chi nhánh đại lý ủy quyền *',
      dealerPlaceholder: '-- Chọn chi nhánh đại lý --',
      dateLabel: 'Ngày lái thử dự kiến (Giờ VN) *',
      windowLabel: 'Khung giờ lái thử (45 phút) *',
      licenseLabel: 'Số giấy phép lái xe * (A1/A2)',
      licensePlaceholder: 'Nhập số GPLX (6–20 ký tự)',
      phoneLabel: 'Số điện thoại liên hệ *',
      phonePlaceholder: 'Nhập số điện thoại liên hệ',
      notesLabel: 'Kinh nghiệm lái / Ghi chú thêm (Không bắt buộc)',
      notesPlaceholder: 'VD: Cỡ mũ bảo hiểm L, muốn trải nghiệm bản phanh ABS...',
      submitting: 'Đang đặt lịch lái thử...',
      submitCta: 'Xác nhận đăng ký lái thử',
      tabAll: 'Tất cả yêu cầu',
      tabActive: 'Đang mở',
      tabCompleted: 'Đã lái thử',
      tabCancelled: 'Đã hủy / Không đến',
      demoVinBadge: 'Xe Demo VIN: {{vin}}',
      licensePrefix: 'GPLX',
      telPrefix: 'SĐT',
      advisorNoteLabel: 'Ghi chú từ cố vấn',
      cancelReasonLabel: 'Lý do hủy',
      cancelModalEyebrow: 'HỦY LỊCH LÁI THỬ SHOWROOM',
      cancelModalTitle: 'Hủy lịch lái thử {{number}}',
      cancelReasonInputLabel: 'Lý do hủy * (3–300 ký tự)',
      cancelReasonPlaceholder: 'Thay đổi lịch trình — sẽ chọn khung giờ lái thử khác.',
      keepSlot: 'Giữ khung giờ',
      cancelling: 'Đang hủy...',
      confirmCancel: 'Xác nhận hủy lịch',
      createdToastTitle: 'Đã đăng ký lái thử',
      createdToastBody: '{{number}} vào ngày {{date}} ({{window}}).',
      cancelledToastTitle: 'Đã hủy lịch lái thử',
      cancelledToastBody: 'Lịch lái thử {{number}} đã được hủy.'
    }
  };

  var EN_PATCH = {
    customerTestDrives: {
      eyebrow: 'SHOWROOM TEST DRIVES • LIVE SIGNALR',
      refreshing: 'Refreshing...',
      browseShowroom: 'Browse Showroom',
      createdTitle: 'Test-Drive Requested:',
      createdNotified: 'Branch staff have been notified in real time.',
      dismiss: 'Dismiss',
      formTitle: 'Request Showroom Demo Ride',
      modelLabel: 'Motorcycle Model *',
      modelPlaceholder: '-- Select Flagship Model --',
      dealerLabel: 'Authorized Dealership Branch *',
      dealerPlaceholder: '-- Select Dealership Branch --',
      dateLabel: 'Preferred Date (Asia/Ho_Chi_Minh) *',
      windowLabel: '45-Minute Test-Drive Window *',
      licenseLabel: 'Driver License # * (A1/A2)',
      licensePlaceholder: 'Enter driver license number',
      phoneLabel: 'Contact Phone *',
      phonePlaceholder: 'Enter contact phone number',
      notesLabel: 'Rider Experience / Special Notes (Optional)',
      notesPlaceholder: 'e.g. Helmet size L, interested in ABS Sport colorway...',
      submitting: 'Booking Test Drive...',
      submitCta: 'Confirm Test-Drive Request',
      tabAll: 'All Requests',
      tabActive: 'Active',
      tabCompleted: 'Completed',
      tabCancelled: 'Cancelled / No-Show',
      demoVinBadge: 'Demo VIN: {{vin}}',
      licensePrefix: 'License',
      telPrefix: 'Tel',
      advisorNoteLabel: 'Advisor Note',
      cancelReasonLabel: 'Cancellation Reason',
      cancelModalEyebrow: 'CANCEL SHOWROOM TEST DRIVE',
      cancelModalTitle: 'Cancel Test Drive {{number}}',
      cancelReasonInputLabel: 'Cancellation Reason * (3–300 chars)',
      cancelReasonPlaceholder: 'Schedule change — will book another demo slot.',
      keepSlot: 'Keep Slot',
      cancelling: 'Cancelling...',
      confirmCancel: 'Confirm Cancellation',
      createdToastTitle: 'Test-Drive Requested',
      createdToastBody: '{{number}} scheduled for {{date}} ({{window}}).',
      cancelledToastTitle: 'Test Drive Cancelled',
      cancelledToastBody: '{{number}} cancelled.'
    }
  };

  registerCustomerTestDrivesI18n.$inject = ['I18nService'];
  function registerCustomerTestDrivesI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerTestDrivesI18n)
    .component('customerTestDrives', {
      controllerAs: '$ctrl',
      controller: CustomerTestDrivesController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="steering-wheel" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerTestDrives.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">{{ \'booking.testDrivesTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'booking.testDrivesSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadTestDrives()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerTestDrives.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<a class="btn btn-secondary" ui-sref="app.public.showroom">' +
                '<app-icon name="storefront" weight="regular" size="16"></app-icon>' +
                '<span>{{ \'customerTestDrives.browseShowroom\' | t }}</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.createdBanner" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;">' +
              '<app-icon name="check-circle" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerTestDrives.createdTitle\' | t }}</strong> ' +
                '<span class="mono-num">{{ $ctrl.createdBanner.bookingNumber }}</span> • ' +
                '{{ $ctrl.createdBanner.motorcycleModelName }} — <strong>{{ $ctrl.createdBanner.dealerName }}</strong> ' +
                '(<span class="mono-num">{{ $ctrl.createdBanner.preferredDate }} {{ $ctrl.createdBanner.timeWindow }}</span>). ' +
                '{{ \'customerTestDrives.createdNotified\' | t }}' +
              '</div>' +
            '</div>' +
            '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.createdBanner = null">{{ \'customerTestDrives.dismiss\' | t }}</button>' +
          '</div>' +

          '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.submitProblem">' +
            '<div><strong>{{ $ctrl.submitProblem.title }} ({{ $ctrl.submitProblem.errorCode }}):</strong> {{ $ctrl.submitProblem.detail }}</div>' +
            '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.submitProblem.traceId">traceId: {{ $ctrl.submitProblem.traceId }}</div>' +
          '</div>' +

          '<div class="grid-12">' +
            /* Left Column: Schedule New Test-Drive Form inside Double-Bezel Card */
            '<div class="col-5">' +
              '<div class="bezel-card">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="steering-wheel" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerTestDrives.formTitle\' | t }}</h2>' +
                    '</div>' +
                  '</div>' +

                  '<form name="$ctrl.tdForm" ng-submit="$ctrl.submitTestDrive()" novalidate>' +
                    '<div class="form-group">' +
                      '<label class="form-label" for="tdModelSelect">{{ \'customerTestDrives.modelLabel\' | t }}</label>' +
                      '<select id="tdModelSelect" name="motorcycleModelId" class="form-control" ' +
                              'ng-model="$ctrl.draft.motorcycleModelId" ' +
                              'ng-options="m.id as (m.brand + \' \' + m.modelName + \' (\' + (m.baseRetailPrice | vndCurrency) + \')\') for m in $ctrl.models" ' +
                              'ng-change="$ctrl.onModelChanged()" required>' +
                        '<option value="">{{ \'customerTestDrives.modelPlaceholder\' | t }}</option>' +
                      '</select>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.motorcycleModelId">{{ $ctrl.fieldErrors.motorcycleModelId }}</div>' +
                    '</div>' +

                    /* Selected Model Visual Preview with <bike-image variant="thumb"> */
                    '<div ng-if="$ctrl.isLoading && $ctrl.models.length === 0" style="margin-bottom:16px;">' +
                      '<skeleton-loader variant="drawer" count="1"></skeleton-loader>' +
                    '</div>' +
                    '<div class="surface-sunken fade-slide-up" ng-if="$ctrl.selectedModel" ' +
                         'style="padding:10px 12px;margin-bottom:16px;display:flex;align-items:center;gap:12px;">' +
                      '<div style="width:92px;flex-shrink:0;">' +
                        '<bike-image src="$ctrl.selectedModel.imageUrl" alt="{{ $ctrl.selectedModel.modelName }}" variant="thumb"></bike-image>' +
                      '</div>' +
                      '<div style="min-width:0;flex:1;">' +
                        '<div class="text-primary" style="font-weight:700;font-size:14px;">{{ $ctrl.selectedModel.modelName }}</div>' +
                        '<div class="text-muted" style="font-size:12px;margin-top:2px;">' +
                          '{{ $ctrl.selectedModel.category | enumT:\'MotorcycleCategory\' }} • <span class="mono-num">{{ $ctrl.selectedModel.engineDisplacementCc }} cc</span>' +
                        '</div>' +
                        '<div class="mono-num text-primary" style="font-size:13px;font-weight:700;margin-top:4px;">' +
                          '{{ $ctrl.selectedModel.baseRetailPrice | vndCurrency }}' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="tdDealerSelect">{{ \'customerTestDrives.dealerLabel\' | t }}</label>' +
                      '<select id="tdDealerSelect" name="dealerId" class="form-control" ' +
                              'ng-model="$ctrl.draft.dealerId" ' +
                              'ng-options="d.id as (d.code + \' • \' + d.name + \' (\' + d.city + \')\') for d in $ctrl.dealers" ' +
                              'ng-change="$ctrl.onFieldEdited(\'dealerId\')" required>' +
                        '<option value="">{{ \'customerTestDrives.dealerPlaceholder\' | t }}</option>' +
                      '</select>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.dealerId">{{ $ctrl.fieldErrors.dealerId }}</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="tdDateInput">{{ \'customerTestDrives.dateLabel\' | t }}</label>' +
                      '<input id="tdDateInput" name="preferredDate" type="date" class="form-control mono-num" ' +
                             'ng-model="$ctrl.preferredDateInput" ng-change="$ctrl.onPreferredDateInputChanged()" required />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.preferredDate">{{ $ctrl.fieldErrors.preferredDate }}</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label">{{ \'customerTestDrives.windowLabel\' | t }}</label>' +
                      '<div class="grid-12" style="gap:8px;">' +
                        '<div class="col-6" ng-repeat="win in $ctrl.timeWindows track by win.key">' +
                          '<button type="button" class="btn btn-sm mono-num" style="width:100%;justify-content:center;" ' +
                                  'ng-class="$ctrl.selectedWindowKey === win.key ? \'btn-primary\' : \'btn-secondary\'" ' +
                                  'ng-click="$ctrl.selectTimeWindow(win)">' +
                            '<app-icon name="clock" weight="regular" size="14"></app-icon>' +
                            '<span>{{ win.startTime }}–{{ win.endTime }}</span>' +
                          '</button>' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="grid-12">' +
                      '<div class="col-6 form-group">' +
                        '<label class="form-label" for="tdLicenseInput">{{ \'customerTestDrives.licenseLabel\' | t }}</label>' +
                        '<input id="tdLicenseInput" name="driverLicenseNumber" type="text" minlength="6" maxlength="20" ' +
                               'class="form-control mono-num" ' +
                               'ng-model="$ctrl.draft.driverLicenseNumber" ' +
                               'ng-pattern="/^[A-Za-z0-9-]{6,20}$/" ' +
                               'ng-change="$ctrl.onLicenseEdited()" required ' +
                               'ng-attr-placeholder="{{ \'customerTestDrives.licensePlaceholder\' | t }}" />' +
                        '<div class="field-error" ng-if="$ctrl.fieldErrors.driverLicenseNumber">{{ $ctrl.fieldErrors.driverLicenseNumber }}</div>' +
                      '</div>' +

                      '<div class="col-6 form-group">' +
                        '<label class="form-label" for="tdPhoneInput">{{ \'customerTestDrives.phoneLabel\' | t }}</label>' +
                        '<input id="tdPhoneInput" name="contactPhone" type="tel" minlength="8" maxlength="20" ' +
                               'class="form-control mono-num" ' +
                               'ng-model="$ctrl.draft.contactPhone" ' +
                               'ng-pattern="/^\\+?[0-9]{8,15}$/" ' +
                               'ng-change="$ctrl.onFieldEdited(\'contactPhone\')" required ' +
                               'ng-attr-placeholder="{{ \'customerTestDrives.phonePlaceholder\' | t }}" />' +
                        '<div class="field-error" ng-if="$ctrl.fieldErrors.contactPhone">{{ $ctrl.fieldErrors.contactPhone }}</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="tdNotesInput">{{ \'customerTestDrives.notesLabel\' | t }}</label>' +
                      '<textarea id="tdNotesInput" name="customerNotes" rows="2" maxlength="500" class="form-control" ' +
                                'ng-model="$ctrl.draft.customerNotes" ' +
                                'ng-attr-placeholder="{{ \'customerTestDrives.notesPlaceholder\' | t }}"></textarea>' +
                    '</div>' +

                    '<button type="submit" id="submitTestDriveBtn" class="btn-pill-cta" style="width:100%;" ' +
                            'has-permission="Permissions.TestDrives.Create" ' +
                            'ng-disabled="$ctrl.isSubmitting || !$ctrl.canSubmitTestDrive()">' +
                      '<span>{{ $ctrl.isSubmitting ? (\'customerTestDrives.submitting\' | t) : (\'customerTestDrives.submitCta\' | t) }}</span>' +
                      '<span class="btn-pill-well" aria-hidden="true">' +
                        '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                      '</span>' +
                    '</button>' +
                  '</form>' +
                '</div>' +
              '</div>' +
            '</div>' +

            /* Right Column: My Test-Drive Appointments & Live Status */
            '<div class="col-7">' +
              '<div class="tab-strip">' +
                '<button type="button" class="tab-btn" ' +
                        'ng-repeat="tab in $ctrl.tabs track by tab.key" ' +
                        'ng-class="{\'is-active\': $ctrl.activeTab === tab.key}" ' +
                        'ng-click="$ctrl.setTab(tab.key)">' +
                  '<span>{{ tab.labelKey | t }}</span>' +
                  '<span class="mono-num">({{ $ctrl.countForTab(tab.key) }})</span>' +
                '</button>' +
              '</div>' +

              '<error-retry-banner ' +
                'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
                'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
                'retrying="$ctrl.isLoading" ' +
                'on-retry="$ctrl.loadInitialData()">' +
              '</error-retry-banner>' +

              '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.testDrives.length === 0" variant="cards" count="2"></skeleton-loader>' +

              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredTestDrives.length === 0" ' +
                           'icon="steering-wheel" ' +
                           'title-key="booking.emptyTestDrivesTitle" ' +
                           'desc-key="booking.emptyTestDrivesDesc">' +
              '</empty-state>' +

              '<div style="display:flex;flex-direction:column;gap:14px;" ng-if="$ctrl.filteredTestDrives.length > 0">' +
                '<article class="bezel-card stagger-item" ' +
                         'ng-repeat="td in $ctrl.filteredTestDrives track by td.id" ' +
                         'ng-class="{\'is-signalr-pulse\': td.isLiveArrival}" ' +
                         'style="--i:{{ $index }};">' +
                  '<div class="bezel-card-inner">' +
                    '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:14px;flex-wrap:wrap;">' +
                      '<div style="display:flex;align-items:flex-start;gap:12px;flex:1;min-width:240px;">' +
                        '<div style="width:92px;flex-shrink:0;">' +
                          '<bike-image src="$ctrl.getModelImageUrl(td.motorcycleModelId, td.motorcycleModelName, td.imageUrl || td.motorcycleImageUrl)" ' +
                                      'alt="{{ td.motorcycleModelName }}" ' +
                                      'variant="thumb">' +
                          '</bike-image>' +
                        '</div>' +
                        '<div style="min-width:0;flex:1;">' +
                          '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                            '<strong class="mono-num text-primary" style="font-size:15px;">{{ td.bookingNumber }}</strong>' +
                            '<span class="badge" ng-class="\'badge-status-\' + td.status.toLowerCase()">' +
                              '<app-icon name="{{ $ctrl.statusIcon(td.status) }}" weight="fill" size="13"></app-icon>' +
                              '<span>{{ td.status | enumT:\'TestDriveStatus\' }}</span>' +
                            '</span>' +
                            '<span class="badge badge-info mono-num" ng-if="td.assignedVin">' +
                              '<app-icon name="identification-card" weight="fill" size="13"></app-icon>' +
                              '<span>{{ \'customerTestDrives.demoVinBadge\' | t:{ vin: td.assignedVin } }}</span>' +
                            '</span>' +
                          '</div>' +
                          '<div class="text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">{{ td.motorcycleModelName }}</div>' +
                          '<div class="text-muted" style="font-size:13px;margin-top:4px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                            '<app-icon name="map-pin" weight="regular" size="14"></app-icon>' +
                            '<span>{{ td.dealerName || td.dealerCode }}</span>' +
                            '<span aria-hidden="true">•</span>' +
                            '<strong class="mono-num text-secondary">{{ td.preferredDate }} ({{ td.timeWindow }})</strong>' +
                          '</div>' +
                          '<div class="mono-num text-muted" style="font-size:12px;margin-top:4px;">' +
                            '{{ \'customerTestDrives.licensePrefix\' | t }}: {{ td.driverLicenseNumber }} • {{ \'customerTestDrives.telPrefix\' | t }}: {{ td.contactPhone }}' +
                          '</div>' +
                          '<div class="text-secondary" style="font-size:12px;margin-top:4px;" ng-if="td.customerNotes">' +
                            '<strong>{{ \'common.notes\' | t }}:</strong> {{ td.customerNotes }}' +
                          '</div>' +
                          '<div style="font-size:12px;color:var(--clr-info-fg);margin-top:4px;" ng-if="td.staffNotes">' +
                            '<strong>{{ \'customerTestDrives.advisorNoteLabel\' | t }}:</strong> {{ td.staffNotes }}' +
                          '</div>' +
                          '<div style="font-size:12px;color:var(--clr-danger-fg);margin-top:4px;" ng-if="td.cancellationReason">' +
                            '<strong>{{ \'customerTestDrives.cancelReasonLabel\' | t }}:</strong> {{ td.cancellationReason }}' +
                          '</div>' +
                        '</div>' +
                      '</div>' +

                      '<div style="display:flex;gap:8px;align-items:center;">' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'ng-if="$ctrl.canCancelTestDrive(td)" ' +
                                'has-permission="Permissions.TestDrives.CancelOwn" ' +
                                'ng-click="$ctrl.openCancelModal(td)">' +
                          '<app-icon name="x" weight="regular" size="14"></app-icon>' +
                          '<span>{{ \'common.cancel\' | t }}</span>' +
                        '</button>' +
                      '</div>' +
                    '</div>' +

                    '<div class="status-stepper-track" ng-if="td.status !== \'Cancelled\' && td.status !== \'NoShow\'">' +
                      '<span class="status-step-node" ' +
                            'ng-repeat="st in $ctrl.stepperStages track by st.code" ' +
                            'ng-class="{\'is-done\': $ctrl.isStageDone(td.status, st.code), \'is-current\': td.status === st.code}">' +
                        '<app-icon name="{{ $ctrl.isStageDone(td.status, st.code) ? \'check\' : (td.status === st.code ? \'lightning\' : \'clock\') }}" ' +
                                  'weight="{{ td.status === st.code ? \'fill\' : \'regular\' }}" size="13"></app-icon>' +
                        '<span>{{ st.code | enumT:\'TestDriveStatus\' }}</span>' +
                      '</span>' +
                    '</div>' +
                  '</div>' +
                '</article>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Cancel Test-Drive Confirmation Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="cancelTdTitle" ng-if="$ctrl.cancellingItem">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-warning mono-num">{{ \'customerTestDrives.cancelModalEyebrow\' | t }}</span>' +
                  '<h2 id="cancelTdTitle" class="surface-card-title" style="margin-top:4px;">{{ \'customerTestDrives.cancelModalTitle\' | t:{ number: $ctrl.cancellingItem.bookingNumber } }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCancelModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.cancelTdForm" ng-submit="$ctrl.confirmCancelTestDrive()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.cancelProblem">' +
                    '<strong>{{ $ctrl.cancelProblem.title }}:</strong> {{ $ctrl.cancelProblem.detail }}' +
                  '</div>' +
                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="tdCancelReasonInput">{{ \'customerTestDrives.cancelReasonInputLabel\' | t }}</label>' +
                    '<textarea id="tdCancelReasonInput" name="reason" rows="3" minlength="3" maxlength="300" class="form-control" ' +
                              'ng-model="$ctrl.cancelReason" required ' +
                              'ng-attr-placeholder="{{ \'customerTestDrives.cancelReasonPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeCancelModal()" ng-disabled="$ctrl.isCancelling">{{ \'customerTestDrives.keepSlot\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.isCancelling || $ctrl.cancelTdForm.$invalid">' +
                    '{{ $ctrl.isCancelling ? (\'customerTestDrives.cancelling\' | t) : (\'customerTestDrives.confirmCancel\' | t) }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  CustomerTestDrivesController.$inject = [
    '$q',
    '$rootScope',
    '$stateParams',
    'AuthService',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function CustomerTestDrivesController(
    $q,
    $rootScope,
    $stateParams,
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

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback !== undefined ? fallback : key;
    }

    function ymdToLocalDate(ymdStr) {
      var parts = String(ymdStr || '').split('-');
      if (parts.length === 3) {
        return new Date(Number(parts[0]) || 2026, (Number(parts[1]) || 1) - 1, Number(parts[2]) || 1, 12, 0, 0);
      }
      return new Date();
    }

    function matchesTab(item, tabKey) {
      if (tabKey === 'ACTIVE') {
        return item.status === 'Requested' || item.status === 'Confirmed';
      }
      if (tabKey === 'COMPLETED') {
        return item.status === 'Completed';
      }
      if (tabKey === 'CANCELLED') {
        return item.status === 'Cancelled' || item.status === 'NoShow';
      }
      return true;
    }

    function syncSelectedModel() {
      var list = $ctrl.models || [];
      $ctrl.selectedModel = null;
      for (var i = 0; i < list.length; i++) {
        if (list[i].id === $ctrl.draft.motorcycleModelId) {
          $ctrl.selectedModel = list[i];
          break;
        }
      }
    }

    function applyInitialSelectionIfBlank(validateMembership) {
      var models = $ctrl.models || [];
      var dealers = $ctrl.dealers || [];

      if (!$ctrl.draft.motorcycleModelId) {
        if (models.length > 0) {
          $ctrl.draft.motorcycleModelId = models[0].id;
        }
      } else if (validateMembership && models.length > 0) {
        var hasModel = false;
        for (var i = 0; i < models.length; i++) {
          if (models[i].id === $ctrl.draft.motorcycleModelId) {
            hasModel = true;
            break;
          }
        }
        if (!hasModel) {
          $ctrl.draft.motorcycleModelId = models[0].id;
        }
      }

      if (!$ctrl.draft.dealerId) {
        if (dealers.length > 0) {
          $ctrl.draft.dealerId = dealers[0].id;
        }
      } else if (validateMembership && dealers.length > 0) {
        var hasDealer = false;
        for (var j = 0; j < dealers.length; j++) {
          if (dealers[j].id === $ctrl.draft.dealerId) {
            hasDealer = true;
            break;
          }
        }
        if (!hasDealer) {
          $ctrl.draft.dealerId = dealers[0].id;
        }
      }

      syncSelectedModel();
    }

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      var pendingCtx = (DealerApiService && angular.isFunction(DealerApiService.consumePendingTestDriveContext))
        ? DealerApiService.consumePendingTestDriveContext()
        : null;

      var initModelId = ($stateParams && $stateParams.modelId) || (pendingCtx && pendingCtx.modelId) || '';
      var initDealerId = ($stateParams && $stateParams.dealerId) || (pendingCtx && pendingCtx.dealerId) || '';

      var todayVn = DealerApiService.getTodayDateVn();
      var tomorrowVn = DealerApiService.addDaysDateVn(todayVn, 1);

      $ctrl.isLoading = false;
      $ctrl.isSubmitting = false;
      $ctrl.submitProblem = null;
      $ctrl.listProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.createdBanner = null;
      $ctrl.activeTab = 'ALL';

      $ctrl.tabs = [
        { key: 'ALL', labelKey: 'customerTestDrives.tabAll' },
        { key: 'ACTIVE', labelKey: 'customerTestDrives.tabActive' },
        { key: 'COMPLETED', labelKey: 'customerTestDrives.tabCompleted' },
        { key: 'CANCELLED', labelKey: 'customerTestDrives.tabCancelled' }
      ];

      $ctrl.timeWindows = TEST_DRIVE_WINDOWS.map(function (w) {
        return angular.extend({}, w);
      });
      $ctrl.stepperStages = TD_STEPPER_STAGES.map(function (s) {
        return angular.extend({}, s);
      });

      $ctrl.selectedWindowKey = '09:00-09:45';
      $ctrl.preferredDateInput = ymdToLocalDate(tomorrowVn);
      $ctrl.draft = {
        motorcycleModelId: initModelId,
        dealerId: initDealerId,
        preferredDate: tomorrowVn,
        startTime: '09:00',
        endTime: '09:45',
        driverLicenseNumber: '',
        contactPhone: user.phoneNumber || '',
        customerNotes: ''
      };

      $ctrl.cancellingItem = null;
      $ctrl.cancelReason = '';
      $ctrl.isCancelling = false;
      $ctrl.cancelProblem = null;
      $ctrl.loadProblem = null;

      $ctrl.dealers = [];
      $ctrl.models = [];
      $ctrl.selectedModel = null;
      $ctrl.testDrives = [];
      $ctrl.applyTabFilter();

      function runInDigest(fn) {
        if ($rootScope && angular.isFunction($rootScope.$applyAsync)) {
          $rootScope.$applyAsync(fn);
        } else {
          fn();
        }
      }

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('TestDriveStatusChanged', function (payload) {
          if (isDestroyed || !payload) {
            return;
          }
          runInDigest(function () {
            if (isDestroyed) { return; }
            var targetId = payload.testDriveId || payload.id || '';
            var targetNum = payload.bookingNumber || '';
            var found = false;
            for (var i = 0; i < $ctrl.testDrives.length; i++) {
              if (
                (targetId && $ctrl.testDrives[i].id === targetId) ||
                (targetNum && $ctrl.testDrives[i].bookingNumber === targetNum)
              ) {
                $ctrl.testDrives[i].status = payload.newStatus || payload.status || $ctrl.testDrives[i].status;
                if (payload.assignedVin) {
                  $ctrl.testDrives[i].assignedVin = String(payload.assignedVin).toUpperCase();
                }
                $ctrl.testDrives[i].isLiveArrival = true;
                found = true;
              }
            }
            $ctrl.applyTabFilter();

            if (found && targetId) {
              DealerApiService.getTestDriveById(targetId)
                .then(function (fresh) {
                  if (isDestroyed) { return; }
                  for (var j = 0; j < $ctrl.testDrives.length; j++) {
                    if ($ctrl.testDrives[j].id === fresh.id) {
                      fresh.isLiveArrival = true;
                      $ctrl.testDrives[j] = fresh;
                    }
                  }
                  $ctrl.applyTabFilter();
                })
                .catch(angular.noop);
            } else if (!found) {
              $ctrl.loadTestDrives();
            }
          });
        }));
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadTestDrives();
          }
        }));
      }

      $ctrl.loadInitialData();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.statusIcon = function (status) {
      if (status === 'Completed') { return 'check-circle'; }
      if (status === 'Confirmed') { return 'steering-wheel'; }
      if (status === 'Cancelled' || status === 'NoShow') { return 'x-circle'; }
      return 'clock';
    };

    $ctrl.getModelImageUrl = function (modelId, modelName, directUrl) {
      if (angular.isString(directUrl) && directUrl.trim()) {
        return directUrl.trim();
      }
      var list = $ctrl.models || [];
      for (var i = 0; i < list.length; i++) {
        if (modelId && list[i].id === modelId && list[i].imageUrl) {
          return list[i].imageUrl;
        }
      }
      if (modelName) {
        var norm = String(modelName).trim().toLowerCase();
        for (var j = 0; j < list.length; j++) {
          if (String(list[j].modelName || '').trim().toLowerCase() === norm && list[j].imageUrl) {
            return list[j].imageUrl;
          }
        }
      }
      return '';
    };

    $ctrl.onModelChanged = function () {
      syncSelectedModel();
      $ctrl.onFieldEdited('motorcycleModelId');
    };

    $ctrl.loadInitialData = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        dealers: DealerApiService.listDealers().catch(function () { return null; }),
        models: DealerApiService.listMotorcycles({ pageSize: 100 }).catch(function () { return null; }),
        testDrives: DealerApiService.getMyTestDrives({ pageSize: 50 })
      }).then(function (bundle) {
        if (isDestroyed) { return bundle.testDrives; }
        if (angular.isArray(bundle.dealers) && bundle.dealers.length > 0) {
          $ctrl.dealers = bundle.dealers.filter(function (d) { return d.isActive; });
        }
        if (angular.isArray(bundle.models) && bundle.models.length > 0) {
          $ctrl.models = bundle.models.filter(function (m) { return m.isActive; });
        }
        applyInitialSelectionIfBlank(true);
        $ctrl.testDrives = bundle.testDrives || [];
        $ctrl.applyTabFilter();
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

    $ctrl.loadTestDrives = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.getMyTestDrives({ pageSize: 50 })
        .then(function (rows) {
          if (isDestroyed) { return rows; }
          $ctrl.testDrives = rows || [];
          $ctrl.applyTabFilter();
          return $ctrl.testDrives;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.testDrives;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.onPreferredDateInputChanged = function () {
      if ($ctrl.preferredDateInput instanceof Date && !isNaN($ctrl.preferredDateInput.getTime())) {
        var y = $ctrl.preferredDateInput.getFullYear();
        var m = ('0' + ($ctrl.preferredDateInput.getMonth() + 1)).slice(-2);
        var d = ('0' + $ctrl.preferredDateInput.getDate()).slice(-2);
        $ctrl.draft.preferredDate = y + '-' + m + '-' + d;
      }
      $ctrl.onFieldEdited('preferredDate');
    };

    $ctrl.selectTimeWindow = function (win) {
      if (!win) { return; }
      $ctrl.selectedWindowKey = win.key;
      $ctrl.draft.startTime = win.startTime;
      $ctrl.draft.endTime = win.endTime;
    };

    $ctrl.onLicenseEdited = function () {
      if ($ctrl.draft.driverLicenseNumber) {
        $ctrl.draft.driverLicenseNumber = String($ctrl.draft.driverLicenseNumber).toUpperCase();
      }
      $ctrl.onFieldEdited('driverLicenseNumber');
    };

    $ctrl.onFieldEdited = function (fieldName) {
      if (ProblemDetailsMapper) {
        ProblemDetailsMapper.clearFieldError($ctrl.tdForm, $ctrl.fieldErrors, fieldName);
      }
    };

    $ctrl.canSubmitTestDrive = function () {
      if (!$ctrl.draft.motorcycleModelId || !$ctrl.draft.dealerId || !$ctrl.draft.preferredDate) {
        return false;
      }
      var lic = String($ctrl.draft.driverLicenseNumber || '').trim();
      var phone = String($ctrl.draft.contactPhone || '').trim();
      return /^[A-Za-z0-9-]{6,20}$/.test(lic) && /^\+?[0-9]{8,15}$/.test(phone);
    };

    $ctrl.submitTestDrive = function () {
      if (ProblemDetailsMapper) {
        ProblemDetailsMapper.clearServerErrors($ctrl.tdForm);
      }
      if (!$ctrl.canSubmitTestDrive() || $ctrl.isSubmitting) {
        return $q.resolve(null);
      }

      $ctrl.isSubmitting = true;
      $ctrl.submitProblem = null;
      $ctrl.fieldErrors = {};

      return DealerApiService.createTestDrive({
        dealerId: $ctrl.draft.dealerId,
        motorcycleModelId: $ctrl.draft.motorcycleModelId,
        preferredDate: $ctrl.draft.preferredDate,
        startTime: $ctrl.draft.startTime,
        endTime: $ctrl.draft.endTime,
        driverLicenseNumber: $ctrl.draft.driverLicenseNumber,
        contactPhone: $ctrl.draft.contactPhone,
        customerNotes: $ctrl.draft.customerNotes || null
      }).then(function (created) {
        if (isDestroyed) { return created; }
        $ctrl.testDrives.unshift(created);
        $ctrl.createdBanner = created;
        $ctrl.draft.customerNotes = '';
        $ctrl.applyTabFilter();
        ToastService.success(
          t('customerTestDrives.createdToastTitle', null, 'Test-Drive Requested'),
          t(
            'customerTestDrives.createdToastBody',
            { number: created.bookingNumber, date: created.preferredDate, window: created.timeWindow },
            created.bookingNumber + ' scheduled for ' + created.preferredDate + ' (' + created.timeWindow + ').'
          )
        );
        return created;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.tdForm, problem);
        $ctrl.submitProblem = problem;
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSubmitting = false;
        }
      });
    };

    $ctrl.setTab = function (tabKey) {
      $ctrl.activeTab = tabKey;
      $ctrl.applyTabFilter();
    };

    $ctrl.countForTab = function (tabKey) {
      var count = 0;
      var all = $ctrl.testDrives || [];
      for (var i = 0; i < all.length; i++) {
        if (matchesTab(all[i], tabKey)) {
          count++;
        }
      }
      return count;
    };

    $ctrl.applyTabFilter = function () {
      $ctrl.filteredTestDrives = ($ctrl.testDrives || []).filter(function (td) {
        return matchesTab(td, $ctrl.activeTab);
      });
    };

    $ctrl.isStageDone = function (currentStatus, stageCode) {
      var curIdx = TD_STAGE_INDEX[currentStatus];
      var stageIdx = TD_STAGE_INDEX[stageCode];
      if (curIdx === undefined || stageIdx === undefined) {
        return false;
      }
      return curIdx > stageIdx || currentStatus === 'Completed';
    };

    $ctrl.canCancelTestDrive = function (td) {
      return Boolean(td && (td.status === 'Requested' || td.status === 'Confirmed'));
    };

    $ctrl.openCancelModal = function (td) {
      $ctrl.cancellingItem = td;
      $ctrl.cancelReason = '';
      $ctrl.cancelProblem = null;
    };

    $ctrl.closeCancelModal = function () {
      $ctrl.cancellingItem = null;
      $ctrl.cancelReason = '';
      $ctrl.cancelProblem = null;
    };

    $ctrl.confirmCancelTestDrive = function () {
      if (!$ctrl.cancellingItem || ($ctrl.cancelTdForm && $ctrl.cancelTdForm.$invalid)) {
        return $q.resolve(null);
      }

      $ctrl.isCancelling = true;
      $ctrl.cancelProblem = null;
      var target = $ctrl.cancellingItem;

      return DealerApiService.cancelTestDrive(target.id, {
        reason: $ctrl.cancelReason,
        rowVersion: target.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.testDrives.length; i++) {
          if ($ctrl.testDrives[i].id === target.id) {
            $ctrl.testDrives[i] = updated;
          }
        }
        $ctrl.applyTabFilter();
        ToastService.success(
          t('customerTestDrives.cancelledToastTitle', null, 'Test Drive Cancelled'),
          t('customerTestDrives.cancelledToastBody', { number: updated.bookingNumber }, updated.bookingNumber + ' cancelled.')
        );
        $ctrl.closeCancelModal();
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.cancelProblem = problem;
        if (problem.status === 409 || problem.status === 422) {
          $ctrl.loadTestDrives();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isCancelling = false;
        }
      });
    };
  }
})();
