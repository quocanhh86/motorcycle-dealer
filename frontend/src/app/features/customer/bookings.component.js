(function () {
  'use strict';

  var STEPPER_STAGES = [
    { code: 'Pending' },
    { code: 'Confirmed' },
    { code: 'CheckedIn' },
    { code: 'InProgress' },
    { code: 'Completed' }
  ];

  var STAGE_INDEX = {
    Pending: 0,
    Confirmed: 1,
    CheckedIn: 2,
    InProgress: 3,
    Completed: 4
  };

  var VI_PATCH = {
    customerBookings: {
      eyebrow: 'LỊCH HẸN XƯỞNG & HÓA ĐƠN • SIGNALR TRỰC TIẾP',
      refreshing: 'Đang làm mới...',
      bookNewCta: 'Đặt lịch bảo dưỡng mới',
      reminderTitle: 'Nhắc lịch bảo dưỡng đến hạn:',
      reminderBookNow: 'Đặt lịch ngay',
      tabAll: 'Tất cả lịch hẹn',
      tabActive: 'Đang mở',
      tabCompleted: 'Hoàn tất',
      tabCancelled: 'Đã hủy / Không đến',
      cancellationReasonLabel: 'Lý do hủy lịch',
      ratedChip: '{{rating}}/5 (+{{pts}} điểm)',
      viewInvoiceCta: 'Xem hóa đơn xưởng',
      printReceiptCta: 'In biên lai',
      reviewHeading: 'Đánh giá CSAT của bạn ({{ref}})',
      reviewBonusTag: '+{{pts}} điểm khả dụng thưởng',
      cancelModalEyebrow: 'HỦY LỊCH HẸN & GIẢI PHÓNG KHOANG',
      cancelModalTitle: 'Hủy lịch hẹn {{number}}',
      cancelModalDesc: 'Hủy lịch hẹn sẽ giải phóng khoang xưởng tại {{dealer}} ({{date}} {{window}}) và tự động hoàn lại voucher chưa hết hạn về trạng thái Sẵn sàng dùng.',
      cancelReasonLabel: 'Lý do hủy lịch * (3–300 ký tự)',
      cancelReasonPlaceholder: 'Thay đổi lịch trình cá nhân — sẽ đặt lại lịch vào tuần sau.',
      cancelKeepCta: 'Giữ lại lịch hẹn',
      cancelConfirmCta: 'Xác nhận hủy lịch',
      cancelBusy: 'Đang hủy...',
      cancelToastTitle: 'Đã hủy lịch hẹn',
      cancelToastBody: '{{number}} đã hủy và giải phóng khoang xưởng.',
      invoiceModalEyebrow: 'HÓA ĐƠN XƯỞNG ĐÃ QUYẾT TOÁN',
      invoiceModalTitle: 'Hóa đơn xưởng • {{number}}',
      invoiceEmpty: 'Chưa có bản ghi hóa đơn quyết toán cho lịch hẹn này.',
      invoiceVehicleOdo: 'Xe máy & ODO xuất xưởng',
      invoiceLabor: 'Tiền công kỹ thuật',
      invoiceParts: 'Phụ tùng & dầu nhớt OEM',
      invoiceGross: 'Tổng trước giảm',
      invoiceVoucherDiscount: 'Giảm giá Voucher',
      invoiceNetPaid: 'Thực thanh toán ({{method}})',
      invoicePointsEarned: 'Điểm thưởng đã cộng:',
      invoiceTierSuffix: 'Hạng: {{tier}}',
      invoiceTechSummary: 'Ghi chú kỹ thuật viên',
      invoiceCloseCta: 'Đóng hóa đơn',
      receiptUnavailableTitle: 'Biên lai chưa sẵn sàng',
      receiptUnavailableBody: 'Không tìm thấy mã hóa đơn hợp lệ để tải biên lai điện tử.',
      receiptNotReadyTitle: 'Chưa có hóa đơn quyết toán',
      receiptNotReadyBody: 'Chưa tìm thấy hóa đơn quyết toán cho lịch {{number}}.',
      csatModalEyebrow: 'ĐÁNH GIÁ CSAT • +25 ĐIỂM KHẢ DỤNG THƯỞNG',
      csatModalTitle: 'Đánh giá chất lượng xưởng • {{number}}',
      csatVerifyingInvoice: 'Đang xác thực hóa đơn đã quyết toán cho {{number}}...',
      csatRewardNote: 'Phần thưởng: +25 điểm khả dụng (Điểm tích lũy xét hạng giữ nguyên theo chuẩn ADR-003).',
      csatRatingOutOfFive: '{{rating}} / 5 sao',
      csatSubmitting: 'Đang gửi đánh giá...',
      csatInvoicePendingTitle: 'Hóa đơn chưa quyết toán',
      csatInvoicePendingDetail: 'Hóa đơn xưởng cho lịch {{number}} chưa được quyết toán.',
      reviewBonusToastTitle: '+25 điểm thưởng CSAT đã cộng!',
      reviewBonusToastBody: 'Cảm ơn đánh giá {{rating}}/5 sao của bạn ({{ref}}).'
    }
  };

  var EN_PATCH = {
    customerBookings: {
      eyebrow: 'WORKSHOP APPOINTMENTS & INVOICES • LIVE SIGNALR',
      refreshing: 'Refreshing...',
      bookNewCta: 'Book New Service Slot',
      reminderTitle: 'Due Maintenance Reminder:',
      reminderBookNow: 'Book Now',
      tabAll: 'All Appointments',
      tabActive: 'Active',
      tabCompleted: 'Completed',
      tabCancelled: 'Cancelled / No-Show',
      cancellationReasonLabel: 'Cancellation Reason',
      ratedChip: '{{rating}}/5 (+{{pts}} pts)',
      viewInvoiceCta: 'View Settled Invoice',
      printReceiptCta: 'Print Receipt',
      reviewHeading: 'Your CSAT Review ({{ref}})',
      reviewBonusTag: '+{{pts}} Bonus Available Points',
      cancelModalEyebrow: 'CANCEL APPOINTMENT & RELEASE BAY',
      cancelModalTitle: 'Cancel Appointment {{number}}',
      cancelModalDesc: 'Cancelling releases the workshop bay at {{dealer}} ({{date}} {{window}}) and automatically restores any unexpired reserved voucher to Available status.',
      cancelReasonLabel: 'Cancellation Reason * (3–300 chars)',
      cancelReasonPlaceholder: 'Schedule conflict — will rebook next week.',
      cancelKeepCta: 'Keep Appointment',
      cancelConfirmCta: 'Confirm Cancellation',
      cancelBusy: 'Cancelling...',
      cancelToastTitle: 'Appointment Cancelled',
      cancelToastBody: '{{number}} cancelled and bay capacity released.',
      invoiceModalEyebrow: 'SETTLED WORKSHOP INVOICE',
      invoiceModalTitle: 'Settled Workshop Invoice • {{number}}',
      invoiceEmpty: 'No settled ServiceInvoice record found for this appointment yet.',
      invoiceVehicleOdo: 'Motorcycle & Exit Odometer',
      invoiceLabor: 'Labor Charges',
      invoiceParts: 'OEM Parts & Fluids',
      invoiceGross: 'Gross Amount',
      invoiceVoucherDiscount: 'Voucher Discount',
      invoiceNetPaid: 'Net Paid ({{method}})',
      invoicePointsEarned: 'Loyalty Points Accrued:',
      invoiceTierSuffix: 'Tier: {{tier}}',
      invoiceTechSummary: 'Technician Summary',
      invoiceCloseCta: 'Close Invoice',
      receiptUnavailableTitle: 'Receipt Unavailable',
      receiptUnavailableBody: 'Could not resolve a settled invoice identifier for printable receipt lookup.',
      receiptNotReadyTitle: 'Receipt Not Ready',
      receiptNotReadyBody: 'No settled ServiceInvoice found for {{number}} yet.',
      csatModalEyebrow: 'CSAT REVIEW • +25 BONUS AVAILABLE POINTS',
      csatModalTitle: 'Rate Workshop Service • {{number}}',
      csatVerifyingInvoice: 'Verifying settled ServiceInvoice for {{number}}...',
      csatRewardNote: 'Reward: +25 Bonus Available Points (Lifetime qualifying points unchanged per ADR-003).',
      csatRatingOutOfFive: '{{rating}} / 5 stars',
      csatSubmitting: 'Submitting Review...',
      csatInvoicePendingTitle: 'Invoice Pending Settlement',
      csatInvoicePendingDetail: 'Workshop invoice has not been settled for {{number}} yet.',
      reviewBonusToastTitle: '+25 Bonus Loyalty Points Earned!',
      reviewBonusToastBody: 'Thank you for your {{rating}}/5 review ({{ref}})!'
    }
  };

  registerCustomerBookingsI18n.$inject = ['I18nService'];
  function registerCustomerBookingsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerBookingsI18n)
    .component('customerBookings', {
      controllerAs: '$ctrl',
      controller: CustomerBookingsController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="calendar-check" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerBookings.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">{{ \'booking.myBookingsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'booking.myBookingsSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadBookings()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerBookings.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<a class="btn-pill-cta" ui-sref="app.customer.bookingNew">' +
                '<span>{{ \'customerBookings.bookNewCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="plus" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-warning fade-slide-up" role="alert" ng-if="$ctrl.reminderAlert" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;">' +
              '<app-icon name="bell-ringing" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerBookings.reminderTitle\' | t }}</strong> ' +
                '<span class="mono-num">{{ $ctrl.reminderAlert.vin }}</span> • ' +
                '<span>{{ $ctrl.reminderAlert.milestoneName || (\'enums.ServiceType.PeriodicMaintenance\' | t) }}</span> ' +
                '(<span class="mono-num">{{ $ctrl.reminderAlert.targetMileageKm | number }} km</span>)' +
              '</div>' +
            '</div>' +
            '<a class="btn btn-primary btn-sm" ui-sref="app.customer.bookingNew({ vehicleId: $ctrl.reminderAlert.customerVehicleId || undefined })">' +
              '<span>{{ \'customerBookings.reminderBookNow\' | t }}</span>' +
              '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
            '</a>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadBookings()">' +
          '</error-retry-banner>' +

          '<div class="tab-strip">' +
            '<button type="button" class="tab-btn" ' +
                    'ng-repeat="tab in $ctrl.tabs track by tab.key" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === tab.key}" ' +
                    'ng-click="$ctrl.setTab(tab.key)">' +
              '<span>{{ tab.labelKey | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.countForTab(tab.key) }})</span>' +
            '</button>' +
          '</div>' +

          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.bookings.length === 0" variant="table" count="4"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredBookings.length === 0" ' +
                       'icon="calendar-blank" ' +
                       'title-key="booking.emptyBookingsTitle" ' +
                       'desc-key="booking.emptyBookingsDesc" ' +
                       'action-key="customerBookings.bookNewCta" ' +
                       'on-action="$ctrl.goToNewBooking()">' +
          '</empty-state>' +

          /* Double-Bezel Appointment Cards */
          '<div style="display:flex;flex-direction:column;gap:16px;" ng-if="$ctrl.filteredBookings.length > 0">' +
            '<article class="bezel-card stagger-item" ' +
                     'ng-repeat="bk in $ctrl.filteredBookings track by bk.id" ' +
                     'ng-class="{\'is-signalr-pulse\': bk.isLiveArrival}" ' +
                     'ng-attr-style="--i:{{ $index }};">' +
              '<div class="bezel-card-inner">' +
                '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:14px;flex-wrap:wrap;">' +
                  '<div style="flex:1;min-width:250px;">' +
                    '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<strong class="mono-num text-primary" style="font-size:16px;">{{ bk.bookingNumber }}</strong>' +
                      '<span class="badge" ng-class="\'badge-status-\' + bk.status.toLowerCase()">' +
                        '{{ bk.status | enumT:\'BookingStatus\' }}' +
                      '</span>' +
                      '<span class="badge badge-info mono-num" ng-if="bk.reservedVoucherCode">' +
                        '<app-icon name="ticket" weight="fill" size="13"></app-icon>' +
                        '<span>{{ bk.reservedVoucherCode }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="text-primary" style="font-size:14px;font-weight:600;margin-top:8px;">' +
                      '{{ bk.motorcycleModelName }} • <span class="mono-num">{{ bk.vin }}</span>' +
                      '<span ng-if="bk.licensePlate" class="text-muted"> ({{ bk.licensePlate }})</span>' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:13px;margin-top:4px;display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<span>{{ bk.dealerName }}</span>' +
                      '<span aria-hidden="true">•</span>' +
                      '<strong class="mono-num text-secondary">{{ bk.slotDate }} ({{ bk.slotWindow }})</strong>' +
                      '<span aria-hidden="true">•</span>' +
                      '<span>{{ bk.serviceType | enumT:\'ServiceType\' }}</span>' +
                      '<span aria-hidden="true">•</span>' +
                      '<span class="mono-num">{{ bk.odometerAtBookingKm | number }} km</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:6px;" ng-if="bk.reportedNotes">' +
                      '<strong>{{ \'common.notes\' | t }}:</strong> {{ bk.reportedNotes }}' +
                    '</div>' +
                    '<div style="font-size:12px;color:var(--clr-danger-fg);margin-top:4px;" ng-if="bk.cancellationReason">' +
                      '<strong>{{ \'customerBookings.cancellationReasonLabel\' | t }}:</strong> {{ bk.cancellationReason }}' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
                    '<span class="badge badge-tier-gold mono-num" ' +
                          'ng-if="bk.status === \'Completed\' && $ctrl.getBookingReview(bk)">' +
                      '<app-icon name="star" weight="fill" size="13"></app-icon>' +
                      '<span>{{ \'customerBookings.ratedChip\' | t:{ rating: $ctrl.getBookingReview(bk).rating, pts: ($ctrl.getBookingReview(bk).bonusPointsAwarded || 25) } }}</span>' +
                    '</span>' +
                    '<button type="button" class="btn btn-primary btn-sm" ' +
                            'ng-if="$ctrl.canRateBooking(bk)" ' +
                            'has-permission="Permissions.Reviews.CreateOwn" ' +
                            'ng-click="$ctrl.openReviewModal(bk)">' +
                      '<app-icon name="star" weight="fill" size="14"></app-icon>' +
                      '<span>{{ \'booking.rateServiceCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-if="bk.status === \'Completed\'" ' +
                            'has-permission="Permissions.Invoices.ViewOwn" ' +
                            'ng-click="$ctrl.openInvoiceBreakdown(bk)">' +
                      '<app-icon name="receipt" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'customerBookings.viewInvoiceCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-if="bk.status === \'Completed\'" ' +
                            'has-permission="Permissions.Invoices.ViewOwn" ' +
                            'ng-click="$ctrl.printBookingReceipt(bk)">' +
                      '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'customerBookings.printReceiptCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-if="$ctrl.canCancelBooking(bk)" ' +
                            'has-permission="Permissions.Bookings.CancelOwn" ' +
                            'ng-click="$ctrl.openCancelModal(bk)">' +
                      '<app-icon name="x" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'booking.cancelBookingCta\' | t }}</span>' +
                    '</button>' +
                  '</div>' +
                '</div>' +

                /* Submitted CSAT Review Callout with <star-rating> */
                '<div class="staff-reply-callout" ng-if="bk.status === \'Completed\' && $ctrl.getBookingReview(bk)">' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;">' +
                    '<div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;">' +
                      '<strong class="text-primary">{{ \'customerBookings.reviewHeading\' | t:{ ref: ($ctrl.getBookingReview(bk).reviewNumber || bk.bookingNumber) } }}</strong>' +
                      '<star-rating value="$ctrl.getBookingReview(bk).rating" readonly="true" size="16"></star-rating>' +
                    '</div>' +
                    '<span class="mono-num" style="color:var(--clr-success-fg);font-weight:700;">' +
                      '{{ \'customerBookings.reviewBonusTag\' | t:{ pts: ($ctrl.getBookingReview(bk).bonusPointsAwarded || 25) } }}' +
                    '</span>' +
                  '</div>' +
                  '<div style="margin-top:6px;display:flex;gap:6px;flex-wrap:wrap;" ng-if="$ctrl.getBookingReview(bk).qualityTags && $ctrl.getBookingReview(bk).qualityTags.length > 0">' +
                    '<span class="badge badge-neutral" ng-repeat="tagCode in $ctrl.getBookingReview(bk).qualityTags track by tagCode">' +
                      '<app-icon name="check" weight="regular" size="12"></app-icon>' +
                      '<span>{{ tagCode | enumT:\'ReviewQualityTag\' }}</span>' +
                    '</span>' +
                  '</div>' +
                  '<div class="text-secondary" style="margin-top:6px;" ng-if="$ctrl.getBookingReview(bk).comment">' +
                    '“{{ $ctrl.getBookingReview(bk).comment }}”' +
                  '</div>' +
                  '<div style="margin-top:8px;padding-top:8px;border-top:1px solid var(--clr-info-border);" ng-if="$ctrl.getBookingReview(bk).staffResponse">' +
                    '<strong style="color:var(--clr-info-fg);">{{ \'booking.staffReplyLabel\' | t }}:</strong> ' +
                    '<span class="text-primary">{{ $ctrl.getBookingReview(bk).staffResponse }}</span>' +
                  '</div>' +
                '</div>' +

                /* Live Status Stepper with Phosphor Icons */
                '<div class="status-stepper-track" ng-if="bk.status !== \'Cancelled\' && bk.status !== \'NoShow\'">' +
                  '<span class="status-step-node" ' +
                        'ng-repeat="st in $ctrl.stepperStages track by st.code" ' +
                        'ng-class="{\'is-done\': $ctrl.isStageDone(bk.status, st.code), \'is-current\': bk.status === st.code}">' +
                    '<app-icon name="{{ $ctrl.isStageDone(bk.status, st.code) ? \'check\' : (bk.status === st.code ? \'lightning\' : \'clock\') }}" ' +
                              'weight="{{ bk.status === st.code ? \'fill\' : \'regular\' }}" size="13"></app-icon>' +
                    '<span>{{ st.code | enumT:\'BookingStatus\' }}</span>' +
                  '</span>' +
                '</div>' +
              '</div>' +
            '</article>' +
          '</div>' +

          /* Cancel Booking Confirmation Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="cancelBkTitle" ng-if="$ctrl.cancellingBooking">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-warning mono-num">{{ \'customerBookings.cancelModalEyebrow\' | t }}</span>' +
                  '<h2 id="cancelBkTitle" class="surface-card-title" style="margin-top:4px;">{{ \'customerBookings.cancelModalTitle\' | t:{ number: $ctrl.cancellingBooking.bookingNumber } }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCancelModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.cancelForm" ng-submit="$ctrl.confirmCancelBooking()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.cancelProblem">' +
                    '<strong>{{ $ctrl.cancelProblem.title }}:</strong> {{ $ctrl.cancelProblem.detail }}' +
                  '</div>' +

                  '<p class="text-secondary" style="font-size:13px;margin-top:0;">' +
                    '{{ \'customerBookings.cancelModalDesc\' | t:{ dealer: $ctrl.cancellingBooking.dealerName, date: $ctrl.cancellingBooking.slotDate, window: $ctrl.cancellingBooking.slotWindow } }}' +
                  '</p>' +

                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="cancelReasonInput">{{ \'customerBookings.cancelReasonLabel\' | t }}</label>' +
                    '<textarea id="cancelReasonInput" name="cancellationReason" rows="3" minlength="3" maxlength="300" class="form-control" ' +
                              'ng-model="$ctrl.cancelReason" required ' +
                              'ng-attr-placeholder="{{ \'customerBookings.cancelReasonPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeCancelModal()" ng-disabled="$ctrl.isCancelling">{{ \'customerBookings.cancelKeepCta\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.isCancelling || $ctrl.cancelForm.$invalid">' +
                    '{{ $ctrl.isCancelling ? (\'customerBookings.cancelBusy\' | t) : (\'customerBookings.cancelConfirmCta\' | t) }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Settled ServiceInvoice Breakdown Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="custInvTitle" ng-if="$ctrl.inspectingInvoiceBooking">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-success mono-num">{{ \'customerBookings.invoiceModalEyebrow\' | t }}</span>' +
                  '<h2 id="custInvTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'customerBookings.invoiceModalTitle\' | t:{ number: $ctrl.inspectingInvoiceBooking.bookingNumber } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeInvoiceBreakdown()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="modal-body">' +
                '<skeleton-loader ng-if="$ctrl.isLoadingInvoice" variant="drawer" count="2"></skeleton-loader>' +
                '<error-retry-banner ' +
                  'ng-if="!$ctrl.isLoadingInvoice" ' +
                  'message="$ctrl.invoiceProblem ? ($ctrl.invoiceProblem.title + \': \' + $ctrl.invoiceProblem.detail) : \'\'" ' +
                  'trace-id="$ctrl.invoiceProblem && $ctrl.invoiceProblem.traceId" ' +
                  'retrying="$ctrl.isLoadingInvoice" ' +
                  'on-retry="$ctrl.openInvoiceBreakdown($ctrl.inspectingInvoiceBooking)">' +
                '</error-retry-banner>' +
                '<div class="text-muted" ng-if="!$ctrl.isLoadingInvoice && !$ctrl.invoiceProblem && !$ctrl.selectedInvoice" style="padding:16px 0;font-size:13px;">' +
                  '{{ \'customerBookings.invoiceEmpty\' | t }}' +
                '</div>' +

                '<div ng-if="$ctrl.selectedInvoice" style="font-size:13px;display:flex;flex-direction:column;gap:10px;">' +
                  '<div class="surface-sunken" style="display:flex;justify-content:space-between;align-items:center;padding:10px 12px;">' +
                    '<strong class="mono-num text-primary" style="font-size:15px;">{{ $ctrl.selectedInvoice.invoiceNumber }}</strong>' +
                    '<span class="mono-num text-secondary">{{ $ctrl.selectedInvoice.paidAtUtc | utcToLocal }}</span>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;"><span class="text-secondary">{{ \'customerBookings.invoiceVehicleOdo\' | t }}:</span><span class="mono-num text-primary">{{ $ctrl.inspectingInvoiceBooking.vin }} • {{ $ctrl.selectedInvoice.odometerAtCompletionKm | number }} km</span></div>' +
                  '<div style="display:flex;justify-content:space-between;"><span class="text-secondary">{{ \'customerBookings.invoiceLabor\' | t }}:</span><span class="mono-num text-primary">{{ $ctrl.selectedInvoice.laborAmount | vndCurrency }}</span></div>' +
                  '<div style="display:flex;justify-content:space-between;"><span class="text-secondary">{{ \'customerBookings.invoiceParts\' | t }}:</span><span class="mono-num text-primary">{{ $ctrl.selectedInvoice.partsAmount | vndCurrency }}</span></div>' +
                  '<div style="display:flex;justify-content:space-between;padding-top:6px;border-top:1px solid var(--clr-border-subtle);"><span class="text-secondary">{{ \'customerBookings.invoiceGross\' | t }}:</span><strong class="mono-num text-primary">{{ $ctrl.selectedInvoice.grossAmount | vndCurrency }}</strong></div>' +
                  '<div style="display:flex;justify-content:space-between;color:var(--clr-danger-fg);"><span>{{ \'customerBookings.invoiceVoucherDiscount\' | t }}:</span><strong class="mono-num">-{{ $ctrl.selectedInvoice.voucherDiscountAmount | vndCurrency }}</strong></div>' +
                  '<div style="display:flex;justify-content:space-between;padding-top:8px;border-top:2px solid var(--clr-border-strong);font-size:16px;">' +
                    '<strong class="text-primary">{{ \'customerBookings.invoiceNetPaid\' | t:{ method: ($ctrl.selectedInvoice.paymentMethod | enumT:\'PaymentMethod\') } }}:</strong>' +
                    '<strong class="mono-num text-primary">{{ $ctrl.selectedInvoice.netPaidAmount | vndCurrency }}</strong>' +
                  '</div>' +
                  '<div style="padding:10px 12px;border-radius:var(--radius-md);background:var(--clr-success-bg);color:var(--clr-success-fg);display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                    '<app-icon name="sparkle" weight="fill" size="16"></app-icon>' +
                    '<span>{{ \'customerBookings.invoicePointsEarned\' | t }} <strong class="mono-num">+{{ $ctrl.selectedInvoice.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                    '<span ng-if="$ctrl.selectedInvoice.newTierCode">({{ \'customerBookings.invoiceTierSuffix\' | t:{ tier: ($ctrl.selectedInvoice.newTierCode | enumT:\'LoyaltyTierCode\') } }})</span>' +
                  '</div>' +
                  '<div ng-if="$ctrl.selectedInvoice.workSummaryNotes" class="text-secondary">' +
                    '<strong>{{ \'customerBookings.invoiceTechSummary\' | t }}:</strong> {{ $ctrl.selectedInvoice.workSummaryNotes }}' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-if="$ctrl.selectedInvoice && !$ctrl.getInvoiceReview($ctrl.selectedInvoice)" ' +
                        'has-permission="Permissions.Reviews.CreateOwn" ' +
                        'ng-click="$ctrl.openReviewFromInvoiceModal()">' +
                  '<app-icon name="star" weight="fill" size="14"></app-icon>' +
                  '<span>{{ \'booking.rateServiceCta\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-secondary" ng-if="$ctrl.selectedInvoice" ' +
                        'has-permission="Permissions.Invoices.ViewOwn" ' +
                        'ng-click="$ctrl.openPrintableReceipt($ctrl.selectedInvoice)">' +
                  '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'customerBookings.printReceiptCta\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-primary" ng-click="$ctrl.closeInvoiceBreakdown()">{{ \'customerBookings.invoiceCloseCta\' | t }}</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Interactive 5-Star CSAT Review Modal with <star-rating> */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="csatReviewModalTitle" ng-if="$ctrl.reviewingBooking">' +
            '<div class="modal-dialog modal-dialog-wide">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-tier-gold mono-num">' +
                    '<app-icon name="star" weight="fill" size="13"></app-icon>' +
                    '<span>{{ \'customerBookings.csatModalEyebrow\' | t }}</span>' +
                  '</span>' +
                  '<h2 id="csatReviewModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'customerBookings.csatModalTitle\' | t:{ number: $ctrl.reviewingBooking.bookingNumber } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeReviewModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.reviewForm" ng-submit="$ctrl.submitServiceReview()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.reviewProblem">' +
                    '<div><strong>{{ $ctrl.reviewProblem.title }} ({{ $ctrl.reviewProblem.errorCode }}):</strong> {{ $ctrl.reviewProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.reviewProblem.traceId">traceId: {{ $ctrl.reviewProblem.traceId }}</div>' +
                  '</div>' +

                  '<div ng-if="$ctrl.isLoadingReviewInvoice" style="margin-bottom:14px;">' +
                    '<skeleton-loader variant="drawer" count="1"></skeleton-loader>' +
                    '<div class="field-hint" style="margin-top:6px;">{{ \'customerBookings.csatVerifyingInvoice\' | t:{ number: $ctrl.reviewingBooking.bookingNumber } }}</div>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:16px;font-size:13px;">' +
                    '<div style="display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap;">' +
                      '<strong class="text-primary">{{ $ctrl.reviewingBooking.motorcycleModelName }} • <span class="mono-num">{{ $ctrl.reviewingBooking.vin }}</span></strong>' +
                      '<span class="mono-num text-secondary" ng-if="$ctrl.reviewingInvoice">{{ $ctrl.reviewingInvoice.invoiceNumber }} ({{ $ctrl.reviewingInvoice.netPaidAmount | vndCurrency }})</span>' +
                    '</div>' +
                    '<div class="text-muted" style="margin-top:4px;">' +
                      '{{ $ctrl.reviewingBooking.dealerName }} • <strong style="color:var(--clr-success-fg);">{{ \'customerBookings.csatRewardNote\' | t }}</strong>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label">{{ \'booking.csatRatingLabel\' | t }} *</label>' +
                    '<div style="display:flex;align-items:center;gap:16px;flex-wrap:wrap;">' +
                      '<star-rating value="$ctrl.reviewDraft.rating" readonly="false" size="28" on-change="$ctrl.selectRating(value)"></star-rating>' +
                      '<strong class="mono-num text-primary" style="font-size:15px;">' +
                        '{{ \'customerBookings.csatRatingOutOfFive\' | t:{ rating: $ctrl.reviewDraft.rating } }}' +
                      '</strong>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label">{{ \'booking.csatTagsLabel\' | t }}</label>' +
                    '<div class="quality-tag-grid">' +
                      '<button type="button" class="quality-tag-chip" ' +
                              'ng-repeat="tag in $ctrl.qualityTagOptions track by tag.code" ' +
                              'ng-class="{\'is-selected\': $ctrl.isQualityTagSelected(tag.code)}" ' +
                              'ng-click="$ctrl.toggleQualityTag(tag.code)">' +
                        '<app-icon name="{{ $ctrl.isQualityTagSelected(tag.code) ? \'check\' : (tag.icon || \'tag\') }}" ' +
                                  'weight="{{ $ctrl.isQualityTagSelected(tag.code) ? \'regular\' : \'duotone\' }}" size="14"></app-icon>' +
                        '<span>{{ tag.code | enumT:\'ReviewQualityTag\' }}</span>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="csatCommentInput">{{ \'booking.csatCommentLabel\' | t }}</label>' +
                    '<textarea id="csatCommentInput" name="comment" rows="3" maxlength="1000" class="form-control" ' +
                              'ng-model="$ctrl.reviewDraft.comment" ' +
                              'ng-attr-placeholder="{{ \'booking.csatCommentPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeReviewModal()" ng-disabled="$ctrl.isSubmittingReview">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" id="submitCsatReviewBtn" class="btn-pill-cta" ' +
                          'has-permission="Permissions.Reviews.CreateOwn" ' +
                          'ng-disabled="$ctrl.isSubmittingReview || $ctrl.isLoadingReviewInvoice || !$ctrl.reviewingInvoice">' +
                    '<span>{{ $ctrl.isSubmittingReview ? (\'customerBookings.csatSubmitting\' | t) : (\'booking.csatSubmitCta\' | t) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="star" weight="fill" size="15"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          '<printable-receipt-modal receipt="$ctrl.activeReceipt" receipt-kind="$ctrl.activeReceiptKind" on-close="$ctrl.closeReceipt()"></printable-receipt-modal>' +
        '</section>'
    });

  CustomerBookingsController.$inject = [
    '$q',
    '$rootScope',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService',
    '$state'
  ];
  function CustomerBookingsController(
    $q,
    $rootScope,
    DealerApiService,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    I18nService,
    $state
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;
    var invoiceByBookingId = Object.create(null);
    var reviewByInvoiceId = Object.create(null);
    var reviewByBookingId = Object.create(null);

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback !== undefined ? fallback : key;
    }

    function indexSettledInvoicesAndReviews(invoicesList, reviewsList) {
      var invs = angular.isArray(invoicesList) ? invoicesList : [];
      for (var i = 0; i < invs.length; i++) {
        var inv = invs[i];
        if (!inv) { continue; }
        var bkKey = inv.serviceBookingId || inv.bookingId || '';
        if (bkKey) {
          invoiceByBookingId[bkKey] = inv;
        }
        if (inv.hasReview && (inv.reviewId || inv.reviewRating) && !reviewByInvoiceId[inv.id]) {
          var synthesized = DealerApiService && angular.isFunction(DealerApiService.normalizeServiceReview)
            ? DealerApiService.normalizeServiceReview({
              id: inv.reviewId || ('rev-' + inv.id),
              serviceInvoiceId: inv.id,
              invoiceNumber: inv.invoiceNumber,
              serviceBookingId: bkKey,
              rating: inv.reviewRating || 5,
              staffResponse: inv.reviewStaffResponse || null,
              bonusPointsAwarded: 25
            })
            : {
              id: inv.reviewId || ('rev-' + inv.id),
              serviceInvoiceId: inv.id,
              serviceBookingId: bkKey,
              rating: inv.reviewRating || 5,
              staffResponse: inv.reviewStaffResponse || null,
              bonusPointsAwarded: 25,
              qualityTags: []
            };
          reviewByInvoiceId[inv.id] = synthesized;
          if (bkKey) {
            reviewByBookingId[bkKey] = synthesized;
          }
        }
      }

      var revs = angular.isArray(reviewsList) ? reviewsList : [];
      for (var j = 0; j < revs.length; j++) {
        var rev = revs[j];
        if (!rev) { continue; }
        if (rev.serviceInvoiceId) {
          reviewByInvoiceId[rev.serviceInvoiceId] = rev;
        }
        if (rev.serviceBookingId) {
          reviewByBookingId[rev.serviceBookingId] = rev;
        }
      }

      var bkKeys = Object.keys(invoiceByBookingId);
      for (var k = 0; k < bkKeys.length; k++) {
        var bId = bkKeys[k];
        var matchedInv = invoiceByBookingId[bId];
        if (matchedInv && matchedInv.id && reviewByInvoiceId[matchedInv.id]) {
          reviewByBookingId[bId] = reviewByInvoiceId[matchedInv.id];
        }
      }
    }

    function matchesTab(booking, tabKey) {
      if (tabKey === 'ACTIVE') {
        return booking.status === 'Pending' || booking.status === 'Confirmed' || booking.status === 'CheckedIn' || booking.status === 'InProgress';
      }
      if (tabKey === 'COMPLETED') {
        return booking.status === 'Completed';
      }
      if (tabKey === 'CANCELLED') {
        return booking.status === 'Cancelled' || booking.status === 'NoShow';
      }
      return true;
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.listProblem = null;
      $ctrl.reminderAlert = null;
      $ctrl.activeTab = 'ALL';

      $ctrl.tabs = [
        { key: 'ALL', labelKey: 'customerBookings.tabAll' },
        { key: 'ACTIVE', labelKey: 'customerBookings.tabActive' },
        { key: 'COMPLETED', labelKey: 'customerBookings.tabCompleted' },
        { key: 'CANCELLED', labelKey: 'customerBookings.tabCancelled' }
      ];

      $ctrl.stepperStages = STEPPER_STAGES.map(function (s) {
        return angular.extend({}, s);
      });

      $ctrl.cancellingBooking = null;
      $ctrl.cancelReason = '';
      $ctrl.isCancelling = false;
      $ctrl.cancelProblem = null;

      $ctrl.inspectingInvoiceBooking = null;
      $ctrl.selectedInvoice = null;
      $ctrl.isLoadingInvoice = false;
      $ctrl.invoiceProblem = null;
      $ctrl.activeReceipt = null;
      $ctrl.activeReceiptKind = 'ServiceInvoice';

      $ctrl.starOptions = [1, 2, 3, 4, 5];
      $ctrl.qualityTagOptions = (DealerApiService && angular.isFunction(DealerApiService.getReviewQualityTags))
        ? DealerApiService.getReviewQualityTags()
        : [
          { code: 'FastTurnaround', label: 'Fast Turnaround', icon: 'lightning' },
          { code: 'TransparentPricing', label: 'Transparent Pricing', icon: 'currency-circle-dollar' },
          { code: 'ExpertTechnician', label: 'Expert Technician', icon: 'wrench' },
          { code: 'CleanLounge', label: 'Clean Lounge', icon: 'coffee' },
          { code: 'GenuineParts', label: 'Genuine OEM Parts', icon: 'shield-check' }
        ];
      $ctrl.reviewingBooking = null;
      $ctrl.reviewingInvoice = null;
      $ctrl.isLoadingReviewInvoice = false;
      $ctrl.isSubmittingReview = false;
      $ctrl.reviewProblem = null;
      $ctrl.reviewDraft = {
        rating: 5,
        qualityTags: ['GenuineParts', 'ExpertTechnician'],
        comment: ''
      };

      invoiceByBookingId = Object.create(null);
      reviewByInvoiceId = Object.create(null);
      reviewByBookingId = Object.create(null);

      $ctrl.loadProblem = null;
      $ctrl.bookings = [];
      $ctrl.applyTabFilter();

      cleanupFns.push(SignalRClientService.on('BookingStatusChanged', function (payload) {
        if (isDestroyed || !payload) {
          return;
        }
        var found = false;
        var matchedId = payload.bookingId || '';
        var transitionedToCompleted = payload.newStatus === 'Completed';
        for (var i = 0; i < $ctrl.bookings.length; i++) {
          if (
            $ctrl.bookings[i].id === payload.bookingId ||
            $ctrl.bookings[i].bookingNumber === payload.bookingNumber
          ) {
            $ctrl.bookings[i].status = payload.newStatus || $ctrl.bookings[i].status;
            $ctrl.bookings[i].isLiveArrival = true;
            if (!matchedId) {
              matchedId = $ctrl.bookings[i].id;
            }
            if ($ctrl.bookings[i].status === 'Completed') {
              transitionedToCompleted = true;
            }
            found = true;
          }
        }
        $ctrl.applyTabFilter();
        if (found && matchedId) {
          if (angular.isFunction(DealerApiService.getBookingById)) {
            DealerApiService.getBookingById(matchedId)
              .then(function (fresh) {
                if (isDestroyed || !fresh) { return; }
                for (var j = 0; j < $ctrl.bookings.length; j++) {
                  if ($ctrl.bookings[j].id === fresh.id) {
                    fresh.isLiveArrival = true;
                    $ctrl.bookings[j] = fresh;
                  }
                }
                $ctrl.applyTabFilter();
              })
              .catch(angular.noop);
          }
          if (transitionedToCompleted) {
            var listInvoicesFn = angular.isFunction(DealerApiService.listServiceInvoices)
              ? function (query) { return DealerApiService.listServiceInvoices(query); }
              : (DealerApiService.serviceInvoices && angular.isFunction(DealerApiService.serviceInvoices.list)
                ? function (query) { return DealerApiService.serviceInvoices.list(query); }
                : null);
            if (listInvoicesFn) {
              listInvoicesFn({ bookingId: matchedId })
                .then(function (invRows) {
                  if (isDestroyed || !angular.isArray(invRows) || invRows.length === 0) {
                    return;
                  }
                  indexSettledInvoicesAndReviews(invRows, []);
                })
                .catch(angular.noop);
            }
          }
        } else if (!found) {
          $ctrl.loadBookings();
        }
      }));

      cleanupFns.push(SignalRClientService.on('LoyaltyPointsUpdated', function (payload) {
        if (isDestroyed || !payload) {
          return;
        }
        var txType = String(payload.transactionType || payload.TransactionType || '');
        var refType = String(payload.referenceType || payload.ReferenceType || '');
        var idempKey = String(payload.idempotencyKey || payload.IdempotencyKey || '');
        var isReviewBonus =
          txType === 'ServiceReviewBonus' ||
          refType === 'ServiceReview' ||
          idempKey.indexOf('SERVICE_REVIEW:') === 0;
        if (!isReviewBonus) {
          return;
        }
        if (angular.isFunction(DealerApiService.getMyServiceReviews)) {
          DealerApiService.getMyServiceReviews({ pageSize: 50 })
            .then(function (revs) {
              if (isDestroyed) { return; }
              indexSettledInvoicesAndReviews([], revs);
              if ($ctrl.reviewingBooking && $ctrl.getBookingReview($ctrl.reviewingBooking)) {
                $ctrl.closeReviewModal();
              }
              $ctrl.applyTabFilter();
            })
            .catch(angular.noop);
        }
      }));

      cleanupFns.push(SignalRClientService.on('MaintenanceReminderTriggered', function (payload) {
        if (payload) {
          $ctrl.reminderAlert = payload;
        }
      }));

      cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
        $ctrl.loadBookings();
      }));

      $ctrl.loadBookings();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.goToNewBooking = function () {
      if ($state && angular.isFunction($state.go)) {
        $state.go('app.customer.bookingNew');
      }
    };

    $ctrl.loadBookings = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      var invoicesPromise = (DealerApiService.serviceInvoices && angular.isFunction(DealerApiService.serviceInvoices.list))
        ? DealerApiService.serviceInvoices.list({ pageSize: 50 }).catch(function () { return []; })
        : $q.resolve([]);
      var reviewsPromise = angular.isFunction(DealerApiService.getMyServiceReviews)
        ? DealerApiService.getMyServiceReviews({ pageSize: 50 }).catch(function () { return []; })
        : $q.resolve([]);

      return DealerApiService.listBookings({ pageSize: 50 })
        .then(function (rows) {
          return invoicesPromise.then(function (invs) {
            return reviewsPromise.then(function (revs) {
              if (isDestroyed) {
                return rows;
              }
              indexSettledInvoicesAndReviews(invs, revs);
              $ctrl.bookings = rows;
              $ctrl.applyTabFilter();
              return rows;
            });
          });
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

    $ctrl.setTab = function (tabKey) {
      $ctrl.activeTab = tabKey;
      $ctrl.applyTabFilter();
    };

    $ctrl.countForTab = function (tabKey) {
      var count = 0;
      var all = $ctrl.bookings || [];
      for (var i = 0; i < all.length; i++) {
        if (matchesTab(all[i], tabKey)) {
          count++;
        }
      }
      return count;
    };

    $ctrl.applyTabFilter = function () {
      $ctrl.filteredBookings = ($ctrl.bookings || []).filter(function (bk) {
        return matchesTab(bk, $ctrl.activeTab);
      });
    };

    $ctrl.isStageDone = function (currentStatus, stageCode) {
      var curIdx = STAGE_INDEX[currentStatus];
      var stageIdx = STAGE_INDEX[stageCode];
      if (curIdx === undefined || stageIdx === undefined) {
        return false;
      }
      return curIdx > stageIdx || currentStatus === 'Completed';
    };

    $ctrl.canCancelBooking = function (bk) {
      return Boolean(bk && (bk.status === 'Pending' || bk.status === 'Confirmed'));
    };

    $ctrl.openCancelModal = function (bk) {
      $ctrl.cancellingBooking = bk;
      $ctrl.cancelReason = '';
      $ctrl.cancelProblem = null;
    };

    $ctrl.closeCancelModal = function () {
      $ctrl.cancellingBooking = null;
      $ctrl.cancelReason = '';
      $ctrl.cancelProblem = null;
    };

    $ctrl.confirmCancelBooking = function () {
      if (!$ctrl.cancellingBooking || ($ctrl.cancelForm && $ctrl.cancelForm.$invalid)) {
        return $q.resolve(null);
      }
      $ctrl.isCancelling = true;
      $ctrl.cancelProblem = null;
      var target = $ctrl.cancellingBooking;

      return DealerApiService.cancelBooking(target.id, {
        cancellationReason: $ctrl.cancelReason,
        rowVersion: target.rowVersion
      }).then(function (updated) {
        if (isDestroyed) {
          return updated;
        }
        for (var i = 0; i < $ctrl.bookings.length; i++) {
          if ($ctrl.bookings[i].id === target.id) {
            $ctrl.bookings[i] = updated;
          }
        }
        $ctrl.applyTabFilter();
        ToastService.success(
          t('customerBookings.cancelToastTitle', null, 'Appointment Cancelled'),
          t('customerBookings.cancelToastBody', { number: target.bookingNumber }, target.bookingNumber + ' cancelled and bay capacity released.')
        );
        $ctrl.closeCancelModal();
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) {
          return null;
        }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.cancelProblem = problem;
        if (problem.status === 409 || problem.status === 422) {
          $ctrl.loadBookings();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isCancelling = false;
        }
      });
    };

    $ctrl.openInvoiceBreakdown = function (bk) {
      if (!bk || !DealerApiService.serviceInvoices) {
        return $q.resolve(null);
      }
      $ctrl.inspectingInvoiceBooking = bk;
      $ctrl.selectedInvoice = null;
      $ctrl.invoiceProblem = null;

      $ctrl.isLoadingInvoice = true;
      var targetBkId = bk.id;
      return DealerApiService.serviceInvoices.list({ bookingId: targetBkId })
        .then(function (rows) {
          if (isDestroyed || !$ctrl.inspectingInvoiceBooking || $ctrl.inspectingInvoiceBooking.id !== targetBkId) {
            return null;
          }
          var matched = (rows && rows.length > 0) ? rows[0] : null;
          $ctrl.selectedInvoice = matched;
          return matched;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            $ctrl.invoiceProblem = ProblemDetailsMapper.extract(rejection);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoadingInvoice = false;
          }
        });
    };

    $ctrl.closeInvoiceBreakdown = function () {
      $ctrl.inspectingInvoiceBooking = null;
      $ctrl.selectedInvoice = null;
      $ctrl.isLoadingInvoice = false;
      $ctrl.invoiceProblem = null;
    };

    $ctrl.openPrintableReceipt = function (invoice) {
      if (!invoice) {
        return $q.resolve(null);
      }
      $ctrl.activeReceiptKind = 'ServiceInvoice';
      var invId = invoice.id || invoice.invoiceId || '';
      if (!invId || !DealerApiService || !angular.isFunction(DealerApiService.getServiceInvoiceReceipt)) {
        ToastService.warning(
          t('customerBookings.receiptUnavailableTitle', null, 'Receipt Unavailable'),
          t('customerBookings.receiptUnavailableBody', null, 'Could not resolve a settled invoice identifier for printable receipt lookup.')
        );
        return $q.resolve(null);
      }
      return DealerApiService.getServiceInvoiceReceipt(invId)
        .then(function (rcpt) {
          if (!isDestroyed) {
            $ctrl.activeReceipt = rcpt;
          }
          return rcpt;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            ToastService.warning(prob.title, prob.detail);
          }
          return null;
        });
    };

    $ctrl.printBookingReceipt = function (bk) {
      if (!bk || !DealerApiService.serviceInvoices) {
        return $q.resolve(null);
      }
      var cachedInv = invoiceByBookingId[bk.id] || null;
      if (cachedInv) {
        return $ctrl.openPrintableReceipt(cachedInv);
      }
      return DealerApiService.serviceInvoices.list({ bookingId: bk.id })
        .then(function (rows) {
          var matched = (rows && rows.length > 0) ? rows[0] : null;
          if (!matched) {
            ToastService.info(
              t('customerBookings.receiptNotReadyTitle', null, 'Receipt Not Ready'),
              t('customerBookings.receiptNotReadyBody', { number: bk.bookingNumber }, 'No settled ServiceInvoice found for ' + bk.bookingNumber + ' yet.')
            );
            return null;
          }
          invoiceByBookingId[bk.id] = matched;
          return $ctrl.openPrintableReceipt(matched);
        })
        .catch(function (rejection) {
          var prob = ProblemDetailsMapper.extract(rejection);
          ToastService.warning(prob.title, prob.detail);
          return null;
        });
    };

    $ctrl.closeReceipt = function () {
      $ctrl.activeReceipt = null;
    };

    $ctrl.getBookingInvoice = function (bk) {
      if (!bk || !bk.id) { return null; }
      return invoiceByBookingId[bk.id] || null;
    };

    $ctrl.getInvoiceReview = function (inv) {
      if (!inv) { return null; }
      var invId = inv.id || inv.invoiceId || '';
      if (invId && reviewByInvoiceId[invId]) {
        return reviewByInvoiceId[invId];
      }
      if (inv.serviceBookingId && reviewByBookingId[inv.serviceBookingId]) {
        return reviewByBookingId[inv.serviceBookingId];
      }
      return null;
    };

    $ctrl.getBookingReview = function (bk) {
      if (!bk || !bk.id) { return null; }
      if (reviewByBookingId[bk.id]) {
        return reviewByBookingId[bk.id];
      }
      var inv = invoiceByBookingId[bk.id];
      if (inv && inv.id && reviewByInvoiceId[inv.id]) {
        return reviewByInvoiceId[inv.id];
      }
      return null;
    };

    $ctrl.canRateBooking = function (bk) {
      if (!bk || bk.status !== 'Completed') {
        return false;
      }
      return !$ctrl.getBookingReview(bk);
    };

    $ctrl.selectRating = function (stars) {
      var n = Math.max(1, Math.min(5, Math.floor(Number(stars) || 5)));
      $ctrl.reviewDraft.rating = n;
    };

    $ctrl.isQualityTagSelected = function (tagCode) {
      var list = ($ctrl.reviewDraft && $ctrl.reviewDraft.qualityTags) || [];
      return list.indexOf(tagCode) !== -1;
    };

    $ctrl.toggleQualityTag = function (tagCode) {
      if (!tagCode) { return; }
      var current = ($ctrl.reviewDraft.qualityTags || []).slice();
      var idx = current.indexOf(tagCode);
      if (idx !== -1) {
        current.splice(idx, 1);
      } else if (current.length < 8) {
        current.push(tagCode);
      }
      $ctrl.reviewDraft.qualityTags = current;
    };

    $ctrl.openReviewFromInvoiceModal = function () {
      var bk = $ctrl.inspectingInvoiceBooking;
      var inv = $ctrl.selectedInvoice;
      $ctrl.closeInvoiceBreakdown();
      return $ctrl.openReviewModal(bk, inv);
    };

    $ctrl.openReviewModal = function (bk, explicitInvoice) {
      if (!bk) {
        return $q.resolve(null);
      }
      $ctrl.reviewingBooking = bk;
      $ctrl.reviewProblem = null;
      $ctrl.reviewDraft = {
        rating: 5,
        qualityTags: ['GenuineParts', 'ExpertTechnician'],
        comment: ''
      };

      var cachedInv = explicitInvoice || invoiceByBookingId[bk.id] || null;
      if (cachedInv) {
        $ctrl.reviewingInvoice = cachedInv;
        return $q.resolve(cachedInv);
      }

      if (!DealerApiService.serviceInvoices) {
        $ctrl.reviewingInvoice = null;
        return $q.resolve(null);
      }

      $ctrl.reviewingInvoice = null;
      $ctrl.isLoadingReviewInvoice = true;
      var targetBkId = bk.id;

      return DealerApiService.serviceInvoices.list({ bookingId: targetBkId })
        .then(function (rows) {
          if (isDestroyed || !$ctrl.reviewingBooking || $ctrl.reviewingBooking.id !== targetBkId) {
            return null;
          }
          var matched = (rows && rows.length > 0) ? rows[0] : null;
          if (!matched) {
            $ctrl.reviewProblem = {
              title: t('customerBookings.csatInvoicePendingTitle', null, 'Invoice Pending Settlement'),
              errorCode: 'INVOICE_NOT_FOUND',
              detail: t('customerBookings.csatInvoicePendingDetail', { number: bk.bookingNumber }, 'Workshop invoice has not been settled for ' + bk.bookingNumber + ' yet.')
            };
            return null;
          }
          invoiceByBookingId[targetBkId] = matched;
          $ctrl.reviewingInvoice = matched;
          if (angular.isFunction(DealerApiService.getServiceReviewByInvoice)) {
            return DealerApiService.getServiceReviewByInvoice(matched.id)
              .then(function (existingRev) {
                if (existingRev && !isDestroyed) {
                  indexSettledInvoicesAndReviews([matched], [existingRev]);
                  $ctrl.closeReviewModal();
                }
                return matched;
              })
              .catch(function () { return matched; });
          }
          return matched;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            $ctrl.reviewProblem = ProblemDetailsMapper.extract(rejection);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoadingReviewInvoice = false;
          }
        });
    };

    $ctrl.closeReviewModal = function () {
      $ctrl.reviewingBooking = null;
      $ctrl.reviewingInvoice = null;
      $ctrl.isLoadingReviewInvoice = false;
      $ctrl.isSubmittingReview = false;
      $ctrl.reviewProblem = null;
    };

    $ctrl.submitServiceReview = function () {
      if (!$ctrl.reviewingBooking || !$ctrl.reviewingInvoice || $ctrl.isSubmittingReview) {
        return $q.resolve(null);
      }

      var targetBk = $ctrl.reviewingBooking;
      var targetInv = $ctrl.reviewingInvoice;
      var invId = targetInv.id || targetInv.invoiceId;
      var chosenRating = Math.max(1, Math.min(5, Math.floor(Number($ctrl.reviewDraft.rating) || 5)));
      var chosenTags = ($ctrl.reviewDraft.qualityTags || []).slice();
      var chosenComment = $ctrl.reviewDraft.comment ? String($ctrl.reviewDraft.comment).trim() : null;

      $ctrl.isSubmittingReview = true;
      $ctrl.reviewProblem = null;

      return DealerApiService.createServiceReview({
        serviceInvoiceId: invId,
        rating: chosenRating,
        qualityTags: chosenTags,
        comment: chosenComment
      }).then(function (created) {
        if (isDestroyed) {
          return created;
        }
        if (!created.serviceBookingId) {
          created.serviceBookingId = targetBk.id;
        }
        indexSettledInvoicesAndReviews([targetInv], [created]);
        ToastService.success(
          t('customerBookings.reviewBonusToastTitle', null, '+25 Bonus Loyalty Points Earned!'),
          t(
            'customerBookings.reviewBonusToastBody',
            { rating: created.rating, ref: (created.reviewNumber || targetInv.invoiceNumber) },
            'Thank you for your ' + created.rating + '/5 review (' + (created.reviewNumber || targetInv.invoiceNumber) + ')!'
          )
        );
        $ctrl.closeReviewModal();
        return created;
      }).catch(function (rejection) {
        if (isDestroyed) {
          return null;
        }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.reviewProblem = problem;
        if (problem.status === 409 && angular.isFunction(DealerApiService.getServiceReviewByInvoice)) {
          DealerApiService.getServiceReviewByInvoice(invId)
            .then(function (existing) {
              if (!isDestroyed && existing) {
                indexSettledInvoicesAndReviews([targetInv], [existing]);
                $ctrl.closeReviewModal();
              }
            })
            .catch(angular.noop);
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSubmittingReview = false;
        }
      });
    };
  }
})();
