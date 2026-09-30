(function () {
  'use strict';

  var VI_PATCH = {
    enums: {
      ServiceType: {
        OilAndTire: 'Thay nhớt, lọc gió & Bảo dưỡng lốp',
        Inspection: 'Kiểm tra an toàn & Chẩn đoán kỹ thuật',
        WarrantyRepair: 'Sửa chữa bảo hành chính hãng OEM'
      }
    },
    customerBookingNew: {
      eyebrow: 'XƯỞNG DỊCH VỤ CHÍNH HÃNG • ROWVERSION AN TOÀN',
      myAppointments: 'Lịch hẹn của tôi',
      step1Label: 'Chi nhánh đại lý',
      step2Label: 'Xe VIN & Dịch vụ',
      step3Label: 'Ngày & Khung giờ',
      step4Label: 'Voucher & Xác nhận',
      conflictHint: 'Thông tin xe, số ODO và ghi chú đã được giữ nguyên — sức chứa khoang xưởng đã làm mới bên dưới. Hãy chọn khung giờ khác chỉ với 1 chạm.',
      step1Title: 'Bước 1: Chọn chi nhánh đại lý ủy quyền',
      hoursAndBays: 'Giờ mở cửa: {{hours}} • {{bays}} khoang/giờ',
      selectedAdvance: 'Đã chọn • Tiếp tục',
      selectDealer: 'Chọn chi nhánh',
      step2Title: 'Bước 2: Chọn xe trong Garage VIN & hạng mục bảo dưỡng',
      selectVehicleLabel: 'Chọn xe máy trong Garage *',
      odometerMinHint: 'Tối thiểu: {{minKm}} km',
      odometerRegressionErr: 'Số ODO không được nhỏ hơn số đang ghi nhận trong Garage ({{minKm}} km).',
      notesCounter: '{{len}}/1000 ký tự',
      backDealer: 'Quay lại: Chi nhánh',
      nextSlot: 'Tiếp theo: Chọn khung giờ',
      step3Title: 'Bước 3: Chọn ngày & khung giờ khoang xưởng',
      step3Hint: 'Trạng thái khoang trực tiếp được bảo vệ chống trùng lịch bằng RowVersion',
      refreshBays: 'Làm mới khoang',
      refreshing: 'Đang tải...',
      dateLabel: 'Ngày hẹn (Giờ Việt Nam) *',
      today: 'Hôm nay',
      tomorrow: 'Ngày mai',
      plusDays: '+{{days}} ngày',
      noSlotsTitle: 'Chưa mở khung giờ cho ngày {{date}}',
      noSlotsDesc: 'Chi nhánh này chưa có khoang trống trong ngày đã chọn. Hãy chuyển sang ngày kế tiếp.',
      tryNextDay: 'Xem ngày kế tiếp',
      slotSelected: 'Đã chọn',
      slotSelect: 'Chọn khung giờ',
      backVehicle: 'Quay lại: Xe VIN',
      nextReview: 'Tiếp theo: Voucher & Xác nhận',
      step4Title: 'Bước 4: Áp dụng Voucher ưu đãi & Xác nhận đặt lịch',
      selectVoucherOption: '-- Không áp dụng Voucher --',
      noVouchersHint: 'Bạn chưa có voucher dịch vụ khả dụng — hãy đổi điểm bất cứ lúc nào tại mục Ví điểm & Voucher.',
      manualVoucherLabel: 'Hoặc nhập trực tiếp mã Voucher',
      manualVoucherPlaceholder: 'Để trống hoặc nhập mã voucher trong ví của bạn',
      manualVoucherHint: 'Voucher được giữ tạm ngay khi đặt lịch và tự động hoàn lại trạng thái Sẵn sàng dùng nếu bạn hủy lịch trước khi tiếp nhận xe.',
      verifySummaryTitle: 'Xác nhận thông tin lịch hẹn xưởng',
      backSlot: 'Quay lại: Khung giờ',
      submitting: 'Đang giữ khoang xưởng...',
      summaryCardTitle: 'Tóm tắt lịch hẹn',
      stepBadge: 'Bước {{step}}/4',
      notSelected: 'Chưa chọn',
      pickOpenSlot: 'Chọn khung giờ còn khoang',
      earnLoyaltyNote: 'Chi tiêu hóa đơn xưởng ròng sẽ tự động tích điểm với hệ số {{mult}}x ngay khi hoàn tất.',
      slotConflictTitle: 'Khung giờ vừa đạt sức chứa tối đa',
      slotConflictDetail: 'Khách hàng khác vừa giữ khoang cuối cùng trước đó ít giây.',
      slotConflictToastTitle: 'Khung giờ vừa kín khoang',
      slotConflictToastBody: 'Đã làm mới danh sách khoang trống — vui lòng chọn khung giờ khác.',
      bookedToastTitle: 'Đặt lịch bảo dưỡng thành công',
      bookedToastBody: 'Mã lịch {{number}} đã được ghi nhận vào ngày {{date}} ({{window}}).',
      badgeBlocked: 'Tạm khóa ({{booked}}/{{max}})',
      badgeFull: 'Kín khoang (0/{{max}})',
      badgeLastBay: 'Khoang cuối (1/{{max}})',
      badgeOpen: 'Còn {{remaining}}/{{max}} khoang'
    }
  };

  var EN_PATCH = {
    enums: {
      ServiceType: {
        OilAndTire: 'Oil, Filter & Tire Service',
        Inspection: 'Safety & Diagnostic Inspection',
        WarrantyRepair: 'Authorized OEM Warranty Repair'
      }
    },
    customerBookingNew: {
      eyebrow: 'AUTHORIZED WORKSHOP • ROWVERSION PROTECTED',
      myAppointments: 'My Appointments',
      step1Label: 'Dealership',
      step2Label: 'Vehicle VIN',
      step3Label: 'Date & Slot',
      step4Label: 'Confirm',
      conflictHint: 'Your vehicle, odometer, and notes were preserved — live workshop bay availability has been refreshed below. Pick any open window in one click.',
      step1Title: 'Step 1: Choose Authorized Service Dealership',
      hoursAndBays: 'Hours: {{hours}} • {{bays}} bays/hr',
      selectedAdvance: 'Selected • Continue',
      selectDealer: 'Select Dealership',
      step2Title: 'Step 2: Select Garage Motorcycle VIN & Service Details',
      selectVehicleLabel: 'Select Owned Motorcycle VIN *',
      odometerMinHint: 'Min: {{minKm}} km',
      odometerRegressionErr: 'Odometer cannot be lower than recorded garage mileage ({{minKm}} km).',
      notesCounter: '{{len}}/1000 characters',
      backDealer: 'Back: Dealership',
      nextSlot: 'Next: Pick Time Slot',
      step3Title: 'Step 3: Pick Date & Concurrency-Safe Bay Time Slot',
      step3Hint: 'Live bay counts guarded atomically by SQL Server RowVersion concurrency',
      refreshBays: 'Refresh Live Bays',
      refreshing: 'Refreshing...',
      dateLabel: 'Service Date (Asia/Ho_Chi_Minh) *',
      today: 'Today',
      tomorrow: 'Tomorrow',
      plusDays: '+{{days}}d',
      noSlotsTitle: 'No workshop slots open for {{date}}',
      noSlotsDesc: 'This branch has no open service windows on the selected date. Try jumping to the next day.',
      tryNextDay: 'Try Next Day',
      slotSelected: 'Selected',
      slotSelect: 'Select Slot',
      backVehicle: 'Back: Vehicle',
      nextReview: 'Next: Voucher & Review',
      step4Title: 'Step 4: Optional Reward Voucher & Final Confirmation',
      selectVoucherOption: '-- No Loyalty Voucher Attached --',
      noVouchersHint: 'No available workshop vouchers in your wallet right now — redeem points anytime in Loyalty & Vouchers.',
      manualVoucherLabel: 'Or Enter Voucher Code Directly',
      manualVoucherPlaceholder: 'Leave blank or enter a voucher code from your wallet',
      manualVoucherHint: 'If attached, the voucher is atomically reserved now and automatically restored to Available if you cancel before check-in.',
      verifySummaryTitle: 'Appointment Verification Summary',
      backSlot: 'Back: Time Slot',
      submitting: 'Reserving Workshop Bay...',
      summaryCardTitle: 'Live Booking Summary',
      stepBadge: 'Step {{step}}/4',
      notSelected: 'Not selected',
      pickOpenSlot: 'Pick an open bay slot',
      earnLoyaltyNote: 'Net workshop invoice spend earns {{mult}}x tier loyalty points upon completion.',
      slotConflictTitle: 'Selected Time Slot Just Reached Capacity',
      slotConflictDetail: 'Another customer claimed the last bay moments ago.',
      slotConflictToastTitle: 'Time Slot Just Filled',
      slotConflictToastBody: 'Refreshing live bay availability — please pick another open slot.',
      bookedToastTitle: 'Workshop Appointment Booked',
      bookedToastBody: 'Booking {{number}} confirmed in queue for {{date}} ({{window}}).',
      badgeBlocked: 'Blocked ({{booked}}/{{max}})',
      badgeFull: 'Full (0/{{max}})',
      badgeLastBay: 'Last Bay (1/{{max}})',
      badgeOpen: '{{remaining}}/{{max}} Bays Open'
    }
  };

  registerCustomerBookingNewI18n.$inject = ['I18nService'];
  function registerCustomerBookingNewI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerBookingNewI18n)
    .component('customerBookingNew', {
      controllerAs: '$ctrl',
      controller: CustomerBookingNewController,
      template:
        '<section class="view-enter">' +
          /* Subtle Editorial Workshop Hero Banner */
          '<div class="atelier-hero" style="min-height:190px;padding:var(--space-6);margin-bottom:var(--space-5);">' +
            '<picture>' +
              '<source srcset="/assets/editorial/workshop-bay-apex.webp" type="image/webp" />' +
              '<img class="atelier-hero-bg" src="/assets/editorial/workshop-bay-apex.jpg" ng-attr-alt="{{ \'booking.wizardTitle\' | t }}" loading="eager" decoding="async" />' +
            '</picture>' +
            '<div class="atelier-hero-overlay" aria-hidden="true"></div>' +
            '<div class="atelier-hero-content">' +
              '<div style="display:flex;justify-content:space-between;align-items:flex-end;gap:16px;flex-wrap:wrap;">' +
                '<div>' +
                  '<div class="atelier-hero-eyebrow">' +
                    '<app-icon name="wrench" weight="duotone" size="14"></app-icon>' +
                    '<span>{{ \'customerBookingNew.eyebrow\' | t }}</span>' +
                  '</div>' +
                  '<h1 class="atelier-hero-title" style="font-size:clamp(1.4rem,2.4vw,2rem);margin-top:10px;">{{ \'booking.wizardTitle\' | t }}</h1>' +
                  '<p class="atelier-hero-subtitle" style="margin-top:6px;">{{ \'booking.wizardSubtitle\' | t }}</p>' +
                '</div>' +
                '<a class="btn btn-ghost-dark" ui-sref="app.customer.bookings">' +
                  '<app-icon name="calendar-check" weight="regular" size="16"></app-icon>' +
                  '<span>{{ \'customerBookingNew.myAppointments\' | t }}</span>' +
                '</a>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Responsive 4-Step Wizard Stepper */
          '<nav class="wizard-stepper" ng-attr-aria-label="{{ \'booking.wizardTitle\' | t }}">' +
            '<button type="button" class="wizard-step-pill" ' +
                    'ng-repeat="st in $ctrl.wizardSteps track by st.step" ' +
                    'ng-class="{\'is-active\': $ctrl.currentStep === st.step, \'is-completed\': $ctrl.isStepComplete(st.step) && $ctrl.currentStep !== st.step}" ' +
                    'ng-disabled="!$ctrl.canNavigateToStep(st.step)" ' +
                    'ng-click="$ctrl.goToStep(st.step)">' +
              '<span class="wizard-step-num">' +
                '<app-icon ng-if="$ctrl.isStepComplete(st.step) && $ctrl.currentStep !== st.step" name="check" weight="regular" size="13"></app-icon>' +
                '<span ng-if="!($ctrl.isStepComplete(st.step) && $ctrl.currentStep !== st.step)">{{ st.step }}</span>' +
              '</span>' +
              '<span>{{ st.labelKey | t }}</span>' +
            '</button>' +
          '</nav>' +

          '<error-retry-banner ' +
            'message="$ctrl.loadProblem ? ($ctrl.loadProblem.title + \': \' + $ctrl.loadProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.loadProblem && $ctrl.loadProblem.traceId" ' +
            'retrying="$ctrl.isLoadingInit" ' +
            'on-retry="$ctrl.loadWizardContext()">' +
          '</error-retry-banner>' +

          '<div class="alert-banner alert-warning fade-slide-up" role="alert" ng-if="$ctrl.slotConflictAlert">' +
            '<div style="display:flex;align-items:flex-start;gap:10px;">' +
              '<app-icon name="warning" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<div style="font-weight:700;">{{ $ctrl.slotConflictAlert.title }} ({{ $ctrl.slotConflictAlert.errorCode }})</div>' +
                '<div style="margin-top:4px;">{{ $ctrl.slotConflictAlert.detail }}</div>' +
                '<div style="margin-top:4px;font-size:12px;">{{ \'customerBookingNew.conflictHint\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.submitProblem">' +
            '<div style="display:flex;align-items:flex-start;gap:10px;">' +
              '<app-icon name="warning-circle" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<div><strong>{{ $ctrl.submitProblem.title }} ({{ $ctrl.submitProblem.errorCode }}):</strong> {{ $ctrl.submitProblem.detail }}</div>' +
                '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.submitProblem.traceId">traceId: {{ $ctrl.submitProblem.traceId }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="grid-12">' +
            '<div class="col-8">' +
              /* STEP 1: Select Dealership */
              '<div class="bezel-card fade-slide-up" ng-if="$ctrl.currentStep === 1">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="storefront" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerBookingNew.step1Title\' | t }}</h2>' +
                    '</div>' +
                  '</div>' +

                  '<skeleton-loader ng-if="$ctrl.isLoadingInit && $ctrl.dealers.length === 0" variant="cards" count="2"></skeleton-loader>' +

                  '<div class="grid-12" ng-if="$ctrl.dealers.length > 0">' +
                    '<div class="col-6 stagger-item" ng-repeat="dlr in $ctrl.dealers track by dlr.id" ng-attr-style="--i:{{ $index }};">' +
                      '<div class="slot-card" style="height:100%;" ng-class="{\'is-selected\': $ctrl.draft.dealerId === dlr.id}">' +
                        '<div>' +
                          '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;">' +
                            '<span class="badge badge-info mono-num">{{ dlr.code }}</span>' +
                            '<span class="badge badge-neutral">{{ dlr.city }}</span>' +
                          '</div>' +
                          '<div class="text-primary" style="font-weight:700;font-size:15px;margin-top:10px;">{{ dlr.name }}</div>' +
                          '<div class="text-muted" style="font-size:12px;margin-top:4px;display:flex;align-items:flex-start;gap:5px;">' +
                            '<app-icon name="map-pin" weight="regular" size="14"></app-icon>' +
                            '<span>{{ dlr.address }}</span>' +
                          '</div>' +
                          '<div class="mono-num text-secondary" style="font-size:12px;margin-top:8px;">' +
                            '{{ \'customerBookingNew.hoursAndBays\' | t:{ hours: dlr.hoursDisplay, bays: dlr.defaultSlotCapacity } }}' +
                          '</div>' +
                        '</div>' +
                        '<button type="button" class="btn btn-sm" style="margin-top:10px;" ' +
                                'ng-class="$ctrl.draft.dealerId === dlr.id ? \'btn-primary\' : \'btn-secondary\'" ' +
                                'ng-click="$ctrl.selectDealerAndAdvance(dlr)">' +
                          '<span>{{ $ctrl.draft.dealerId === dlr.id ? (\'customerBookingNew.selectedAdvance\' | t) : (\'customerBookingNew.selectDealer\' | t) }}</span>' +
                          '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                        '</button>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              /* STEP 2: Select Vehicle VIN, ServiceType, Odometer & Notes */
              '<div class="bezel-card fade-slide-up" ng-if="$ctrl.currentStep === 2">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="motorcycle" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerBookingNew.step2Title\' | t }}</h2>' +
                    '</div>' +
                  '</div>' +

                  '<skeleton-loader ng-if="$ctrl.isLoadingInit && $ctrl.vehicles.length === 0" variant="cards" count="2"></skeleton-loader>' +

                  '<empty-state ng-if="!$ctrl.isLoadingInit && $ctrl.vehicles.length === 0" ' +
                               'icon="motorcycle" ' +
                               'title-key="booking.emptyGarageTitle" ' +
                               'desc-key="booking.emptyGarageDesc" ' +
                               'action-key="booking.registerVinCta" ' +
                               'on-action="$ctrl.goToGarage()">' +
                  '</empty-state>' +

                  '<div ng-if="$ctrl.vehicles.length > 0">' +
                    /* Visual Vehicle Selector Cards with <bike-image variant="thumb"> */
                    '<div class="form-group">' +
                      '<label class="form-label" for="bookingVehicleSelect">{{ \'customerBookingNew.selectVehicleLabel\' | t }}</label>' +
                      '<div class="grid-12" style="gap:10px;margin-bottom:10px;">' +
                        '<div class="col-6 stagger-item" ng-repeat="v in $ctrl.vehicles track by v.id" ng-attr-style="--i:{{ $index }};">' +
                          '<button type="button" class="slot-card" style="width:100%;text-align:left;cursor:pointer;flex-direction:row;align-items:center;gap:12px;" ' +
                                  'ng-class="{\'is-selected\': $ctrl.draft.customerVehicleId === v.id}" ' +
                                  'ng-click="$ctrl.pickVehicleCard(v)">' +
                            '<div style="width:84px;flex-shrink:0;">' +
                              '<bike-image src="v.imageUrl" alt="{{ v.motorcycleModelName }}" variant="thumb"></bike-image>' +
                            '</div>' +
                            '<div style="min-width:0;flex:1;">' +
                              '<div class="text-primary" style="font-weight:700;font-size:13px;">{{ v.motorcycleModelName }}</div>' +
                              '<div class="mono-num text-secondary" style="font-size:11px;margin-top:2px;">{{ v.vin }}</div>' +
                              '<div class="mono-num text-muted" style="font-size:11px;margin-top:2px;">{{ v.currentMileageKm | number }} km</div>' +
                            '</div>' +
                            '<app-icon ng-if="$ctrl.draft.customerVehicleId === v.id" name="check-circle" weight="fill" size="20"></app-icon>' +
                          '</button>' +
                        '</div>' +
                      '</div>' +

                      '<select id="bookingVehicleSelect" class="form-control mono-num" ' +
                              'ng-model="$ctrl.draft.customerVehicleId" ' +
                              'ng-options="v.id as (v.motorcycleModelName + \' • \' + v.vin + \' (\' + v.currentMileageKm + \' km)\') for v in $ctrl.vehicles" ' +
                              'ng-change="$ctrl.onVehicleSelected()"></select>' +
                    '</div>' +

                    '<div class="grid-12">' +
                      '<div class="col-6 form-group">' +
                        '<label class="form-label" for="bookingServiceType">{{ \'booking.serviceTypeLabel\' | t }} *</label>' +
                        '<select id="bookingServiceType" class="form-control" ' +
                                'ng-model="$ctrl.draft.serviceType" ' +
                                'ng-options="st.value as (st.value | enumT:\'ServiceType\') for st in $ctrl.serviceTypeOptions"></select>' +
                      '</div>' +

                      '<div class="col-6 form-group">' +
                        '<label class="form-label" for="bookingOdometerInput">' +
                          '{{ \'booking.odometerLabel\' | t }} * ' +
                          '<span class="text-muted mono-num">({{ \'customerBookingNew.odometerMinHint\' | t:{ minKm: (($ctrl.selectedVehicle ? $ctrl.selectedVehicle.currentMileageKm : 0) | number) } }})</span>' +
                        '</label>' +
                        '<input id="bookingOdometerInput" type="number" min="0" max="1000000" class="form-control mono-num" ' +
                               'ng-model="$ctrl.draft.odometerAtBookingKm" />' +
                        '<div class="field-error" ng-if="$ctrl.selectedVehicle && $ctrl.draft.odometerAtBookingKm < $ctrl.selectedVehicle.currentMileageKm">' +
                          '{{ \'customerBookingNew.odometerRegressionErr\' | t:{ minKm: ($ctrl.selectedVehicle.currentMileageKm | number) } }}' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="bookingReportedNotes">{{ \'booking.notesLabel\' | t }}</label>' +
                      '<textarea id="bookingReportedNotes" rows="3" maxlength="1000" class="form-control" ' +
                                'ng-model="$ctrl.draft.reportedNotes" ' +
                                'ng-attr-placeholder="{{ \'booking.notesPlaceholder\' | t }}"></textarea>' +
                      '<div class="field-hint">{{ \'customerBookingNew.notesCounter\' | t:{ len: ($ctrl.draft.reportedNotes || \'\').length } }}</div>' +
                    '</div>' +

                    '<div style="display:flex;justify-content:space-between;gap:8px;margin-top:16px;flex-wrap:wrap;">' +
                      '<button type="button" class="btn btn-secondary" ng-click="$ctrl.goToStep(1)">' +
                        '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                        '<span>{{ \'customerBookingNew.backDealer\' | t }}</span>' +
                      '</button>' +
                      '<button type="button" class="btn btn-primary" ' +
                              'ng-disabled="!$ctrl.isStepComplete(2)" ' +
                              'ng-click="$ctrl.goToStep(3)">' +
                        '<span>{{ \'customerBookingNew.nextSlot\' | t }}</span>' +
                        '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              /* STEP 3: Pick Date & Live ServiceTimeSlot */
              '<div class="bezel-card fade-slide-up" ng-if="$ctrl.currentStep === 3">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header" style="flex-wrap:wrap;">' +
                    '<div>' +
                      '<div style="display:flex;align-items:center;gap:8px;">' +
                        '<app-icon name="clock" weight="duotone" size="20"></app-icon>' +
                        '<h2 class="surface-card-title">{{ \'customerBookingNew.step3Title\' | t }}</h2>' +
                      '</div>' +
                      '<div class="text-muted" style="font-size:12px;margin-top:3px;">{{ \'customerBookingNew.step3Hint\' | t }}</div>' +
                    '</div>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.loadTimeSlots()" ng-disabled="$ctrl.isLoadingSlots">' +
                      '<app-icon name="arrow-clockwise" weight="regular" size="14" extra-class="{{ $ctrl.isLoadingSlots ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                      '<span>{{ $ctrl.isLoadingSlots ? (\'customerBookingNew.refreshing\' | t) : (\'customerBookingNew.refreshBays\' | t) }}</span>' +
                    '</button>' +
                  '</div>' +

                  '<div style="display:flex;align-items:end;justify-content:space-between;gap:12px;flex-wrap:wrap;margin-bottom:18px;">' +
                    '<div class="form-group" style="margin-bottom:0;min-width:220px;">' +
                      '<label class="form-label" for="bookingDateSelect">{{ \'customerBookingNew.dateLabel\' | t }}</label>' +
                      '<select id="bookingDateSelect" class="form-control mono-num" ' +
                              'ng-model="$ctrl.draft.slotDate" ' +
                              'ng-options="d.value as $ctrl.formatDateOption(d) for d in $ctrl.upcomingDateOptions" ' +
                              'ng-change="$ctrl.onSlotDateChanged()"></select>' +
                    '</div>' +
                    '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                      '<button type="button" class="btn btn-sm" ' +
                              'ng-repeat="quick in $ctrl.quickDates track by quick.value" ' +
                              'ng-class="$ctrl.draft.slotDate === quick.value ? \'btn-primary\' : \'btn-secondary\'" ' +
                              'ng-click="$ctrl.pickQuickDate(quick.value)">' +
                        '{{ $ctrl.formatQuickDate(quick) }}' +
                      '</button>' +
                    '</div>' +
                  '</div>' +

                  '<skeleton-loader ng-if="$ctrl.isLoadingSlots" variant="kpi" count="6"></skeleton-loader>' +

                  '<empty-state ng-if="!$ctrl.isLoadingSlots && $ctrl.slots.length === 0" ' +
                               'icon="calendar-blank" ' +
                               'title="{{ \'customerBookingNew.noSlotsTitle\' | t:{ date: $ctrl.draft.slotDate } }}" ' +
                               'desc-key="customerBookingNew.noSlotsDesc" ' +
                               'action-key="customerBookingNew.tryNextDay" ' +
                               'on-action="$ctrl.jumpToNextDay()">' +
                  '</empty-state>' +

                  '<div class="grid-12" ng-if="!$ctrl.isLoadingSlots && $ctrl.slots.length > 0">' +
                    '<div class="col-4 stagger-item" ng-repeat="slot in $ctrl.slots track by slot.slotId" ng-attr-style="--i:{{ $index }};">' +
                      '<div class="slot-card" style="height:100%;" ' +
                           'ng-class="{\'is-selected\': $ctrl.draft.serviceTimeSlotId === slot.slotId, \'is-last-bay\': slot.isBookable && slot.remainingCapacity === 1, \'is-disabled\': !slot.isBookable}">' +
                        '<div>' +
                          '<div class="mono-num text-primary" style="font-weight:700;font-size:15px;">{{ slot.window }}</div>' +
                          '<div style="margin:10px 0;">' +
                            '<span class="badge" ng-class="$ctrl.slotBadgeClass(slot)">' +
                              '<app-icon name="{{ $ctrl.slotBadgeIcon(slot) }}" weight="fill" size="13"></app-icon>' +
                              '<span>{{ $ctrl.slotBadgeLabel(slot) }}</span>' +
                            '</span>' +
                          '</div>' +
                        '</div>' +
                        '<button type="button" class="btn btn-sm" ' +
                                'ng-class="$ctrl.draft.serviceTimeSlotId === slot.slotId ? \'btn-primary\' : \'btn-secondary\'" ' +
                                'ng-disabled="!slot.isBookable" ' +
                                'ng-click="$ctrl.selectSlot(slot)">' +
                          '<app-icon ng-if="$ctrl.draft.serviceTimeSlotId === slot.slotId" name="check" weight="regular" size="14"></app-icon>' +
                          '<span>{{ !slot.isBookable ? (slot.isBlocked ? (\'booking.slotBlocked\' | t) : (\'booking.slotFull\' | t)) : ($ctrl.draft.serviceTimeSlotId === slot.slotId ? (\'customerBookingNew.slotSelected\' | t) : (\'customerBookingNew.slotSelect\' | t)) }}</span>' +
                        '</button>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:space-between;gap:8px;margin-top:20px;flex-wrap:wrap;">' +
                    '<button type="button" class="btn btn-secondary" ng-click="$ctrl.goToStep(2)">' +
                      '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                      '<span>{{ \'customerBookingNew.backVehicle\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-primary" ' +
                            'ng-disabled="!$ctrl.isStepComplete(3)" ' +
                            'ng-click="$ctrl.goToStep(4)">' +
                      '<span>{{ \'customerBookingNew.nextReview\' | t }}</span>' +
                      '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              /* STEP 4: Optional Voucher & Final Review */
              '<div class="bezel-card fade-slide-up" ng-if="$ctrl.currentStep === 4">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="ticket" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerBookingNew.step4Title\' | t }}</h2>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="bookingVoucherSelect">{{ \'booking.applyVoucherLabel\' | t }}</label>' +
                    '<select id="bookingVoucherSelect" class="form-control mono-num" ' +
                            'ng-model="$ctrl.draft.appliedCustomerVoucherId" ' +
                            'ng-options="v.id as $ctrl.formatVoucherOption(v) for v in $ctrl.availableVouchers" ' +
                            'ng-change="$ctrl.onVoucherSelected()">' +
                      '<option value="">{{ \'customerBookingNew.selectVoucherOption\' | t }}</option>' +
                    '</select>' +
                    '<div class="field-hint" ng-if="$ctrl.availableVouchers.length === 0">{{ \'customerBookingNew.noVouchersHint\' | t }}</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="bookingVoucherCode">{{ \'customerBookingNew.manualVoucherLabel\' | t }}</label>' +
                    '<input id="bookingVoucherCode" type="text" maxlength="32" class="form-control mono-num" ' +
                           'ng-model="$ctrl.draft.voucherCode" ' +
                           'ng-change="$ctrl.onManualVoucherCodeChanged()" ' +
                           'ng-attr-placeholder="{{ \'customerBookingNew.manualVoucherPlaceholder\' | t }}" />' +
                    '<div class="field-hint">{{ \'customerBookingNew.manualVoucherHint\' | t }}</div>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:16px;margin-bottom:20px;font-size:13px;">' +
                    '<div class="text-primary" style="font-weight:700;margin-bottom:10px;display:flex;align-items:center;gap:6px;">' +
                      '<app-icon name="clipboard-text" weight="duotone" size="16"></app-icon>' +
                      '<span>{{ \'customerBookingNew.verifySummaryTitle\' | t }}</span>' +
                    '</div>' +
                    '<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px;">' +
                      '<div class="text-secondary">{{ \'common.dealer\' | t }}: <strong class="text-primary">{{ $ctrl.selectedDealer ? $ctrl.selectedDealer.name : \'—\' }}</strong></div>' +
                      '<div class="text-secondary">{{ \'common.time\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.draft.slotDate }} • {{ $ctrl.selectedSlot ? $ctrl.selectedSlot.window : \'—\' }}</strong></div>' +
                      '<div class="text-secondary">{{ \'common.vehicle\' | t }}: <strong class="text-primary">{{ $ctrl.selectedVehicle ? $ctrl.selectedVehicle.motorcycleModelName : \'—\' }}</strong></div>' +
                      '<div class="text-secondary">{{ \'common.vin\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.selectedVehicle ? $ctrl.selectedVehicle.vin : \'—\' }}</strong></div>' +
                      '<div class="text-secondary">{{ \'booking.serviceTypeLabel\' | t }}: <strong class="text-primary">{{ $ctrl.draft.serviceType | enumT:\'ServiceType\' }}</strong></div>' +
                      '<div class="text-secondary">{{ \'common.mileageKm\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.draft.odometerAtBookingKm | number }} km</strong></div>' +
                    '</div>' +
                    '<div style="margin-top:10px;" ng-if="$ctrl.draft.voucherCode">' +
                      '<span class="badge badge-info mono-num">' +
                        '<app-icon name="ticket" weight="fill" size="13"></app-icon>' +
                        '<span>{{ $ctrl.draft.voucherCode }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="margin-top:8px;" ng-if="$ctrl.draft.reportedNotes">' +
                      '<strong>{{ \'common.notes\' | t }}:</strong> {{ $ctrl.draft.reportedNotes }}' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;">' +
                    '<button type="button" class="btn btn-secondary" ng-click="$ctrl.goToStep(3)" ng-disabled="$ctrl.isSubmitting">' +
                      '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                      '<span>{{ \'customerBookingNew.backSlot\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" id="confirmBookingSubmitBtn" class="btn-pill-cta" ' +
                            'has-permission="Permissions.Bookings.Create" ' +
                            'ng-disabled="$ctrl.isSubmitting || !$ctrl.canSubmitBooking()" ' +
                            'ng-click="$ctrl.submitBooking()">' +
                      '<span>{{ $ctrl.isSubmitting ? (\'customerBookingNew.submitting\' | t) : (\'booking.confirmBookingCta\' | t) }}</span>' +
                      '<span class="btn-pill-well" aria-hidden="true">' +
                        '<app-icon name="check" weight="regular" size="15"></app-icon>' +
                      '</span>' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +

            /* Sticky Right Summary Sidebar */
            '<aside class="col-4">' +
              '<div class="bezel-card">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<h2 class="surface-card-title">{{ \'customerBookingNew.summaryCardTitle\' | t }}</h2>' +
                    '<span class="badge badge-info mono-num">{{ \'customerBookingNew.stepBadge\' | t:{ step: $ctrl.currentStep } }}</span>' +
                  '</div>' +

                  '<div ng-if="$ctrl.selectedVehicle" style="margin-bottom:14px;">' +
                    '<bike-image src="$ctrl.selectedVehicle.imageUrl" alt="{{ $ctrl.selectedVehicle.motorcycleModelName }}" variant="thumb"></bike-image>' +
                  '</div>' +

                  '<div style="font-size:13px;display:flex;flex-direction:column;gap:12px;">' +
                    '<div>' +
                      '<div class="kpi-label">{{ \'common.dealer\' | t }}</div>' +
                      '<div class="text-primary" style="font-weight:600;margin-top:3px;">' +
                        '{{ $ctrl.selectedDealer ? ($ctrl.selectedDealer.code + \' • \' + $ctrl.selectedDealer.name) : (\'customerBookingNew.notSelected\' | t) }}' +
                      '</div>' +
                    '</div>' +
                    '<div>' +
                      '<div class="kpi-label">{{ \'common.vehicle\' | t }}</div>' +
                      '<div class="text-primary" style="font-weight:600;margin-top:3px;">' +
                        '{{ $ctrl.selectedVehicle ? $ctrl.selectedVehicle.motorcycleModelName : (\'customerBookingNew.notSelected\' | t) }}' +
                      '</div>' +
                      '<div class="mono-num text-muted" style="font-size:12px;" ng-if="$ctrl.selectedVehicle">' +
                        '{{ $ctrl.selectedVehicle.vin }} ({{ $ctrl.draft.odometerAtBookingKm | number }} km)' +
                      '</div>' +
                    '</div>' +
                    '<div>' +
                      '<div class="kpi-label">{{ \'common.time\' | t }}</div>' +
                      '<div class="mono-num text-primary" style="font-weight:600;margin-top:3px;">' +
                        '{{ $ctrl.selectedSlot ? ($ctrl.draft.slotDate + \' • \' + $ctrl.selectedSlot.window) : (\'customerBookingNew.pickOpenSlot\' | t) }}' +
                      '</div>' +
                    '</div>' +
                    '<div style="padding-top:12px;border-top:1px solid var(--clr-border-subtle);font-size:12px;color:var(--clr-success-fg);display:flex;align-items:flex-start;gap:8px;">' +
                      '<app-icon name="sparkle" weight="fill" size="16"></app-icon>' +
                      '<span>{{ \'customerBookingNew.earnLoyaltyNote\' | t:{ mult: ($ctrl.userMultiplier | number:2) } }}</span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</aside>' +
          '</div>' +
        '</section>'
    });

  CustomerBookingNewController.$inject = [
    '$q',
    '$rootScope',
    '$state',
    '$stateParams',
    'AuthService',
    'DealerApiService',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    'I18nService',
    'LoyaltyCalculator'
  ];
  function CustomerBookingNewController(
    $q,
    $rootScope,
    $state,
    $stateParams,
    AuthService,
    DealerApiService,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    I18nService,
    LoyaltyCalculator
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

    function getActiveLocale() {
      return (I18nService && angular.isFunction(I18nService.getLocale))
        ? I18nService.getLocale()
        : 'en-US';
    }

    function formatVndSafe(val) {
      if (I18nService && angular.isFunction(I18nService.formatVnd)) {
        return I18nService.formatVnd(val);
      }
      return Math.round(Number(val) || 0).toLocaleString('vi-VN') + ' ₫';
    }

    $ctrl.formatVoucherDiscountSummary = function (voucher) {
      if (!voucher) { return ''; }
      var locale = getActiveLocale();
      if (LoyaltyCalculator) {
        if (angular.isFunction(LoyaltyCalculator.formatDiscountSummaryLocalized)) {
          return LoyaltyCalculator.formatDiscountSummaryLocalized(voucher, locale) || voucher.discountSummary || '';
        }
        if (angular.isFunction(LoyaltyCalculator.formatDiscountSummary)) {
          return LoyaltyCalculator.formatDiscountSummary(voucher, locale) || voucher.discountSummary || '';
        }
      }
      return voucher.discountSummary || '';
    };

    $ctrl.formatVoucherOption = function (voucher) {
      if (!voucher) { return ''; }
      return voucher.voucherCode + ' • ' + $ctrl.formatVoucherDiscountSummary(voucher) + ' (' + formatVndSafe(voucher.minOrderAmount) + ')';
    };

    function syncUserMultiplier() {
      var user = AuthService.getCurrentUser() || {};
      var mult = user.tierMultiplier !== undefined && user.tierMultiplier !== null
        ? user.tierMultiplier
        : user.pointsMultiplier;
      $ctrl.userMultiplier = Number(mult) || 1.0;
    }

    function buildUpcomingDateOptions() {
      var today = DealerApiService.getTodayDateVn();
      var options = [];
      var quick = [];
      for (var d = 0; d < 14; d++) {
        var iso = DealerApiService.addDaysDateVn(today, d);
        options.push({
          value: iso,
          dayOffset: d,
          label: iso
        });
        if (d < 4) {
          quick.push({
            value: iso,
            dayOffset: d
          });
        }
      }
      $ctrl.upcomingDateOptions = options;
      $ctrl.quickDates = quick;
    }

    function syncSelectedReferences() {
      $ctrl.selectedDealer = null;
      for (var i = 0; i < ($ctrl.dealers || []).length; i++) {
        if ($ctrl.dealers[i].id === $ctrl.draft.dealerId) {
          $ctrl.selectedDealer = $ctrl.dealers[i];
          break;
        }
      }

      $ctrl.selectedVehicle = null;
      for (var j = 0; j < ($ctrl.vehicles || []).length; j++) {
        if ($ctrl.vehicles[j].id === $ctrl.draft.customerVehicleId) {
          $ctrl.selectedVehicle = $ctrl.vehicles[j];
          break;
        }
      }

      $ctrl.selectedSlot = null;
      for (var k = 0; k < ($ctrl.slots || []).length; k++) {
        if ($ctrl.slots[k].slotId === $ctrl.draft.serviceTimeSlotId) {
          $ctrl.selectedSlot = $ctrl.slots[k];
          break;
        }
      }
    }

    $ctrl.$onInit = function () {
      syncUserMultiplier();
      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('auth:changed', function () {
          if (!isDestroyed) {
            syncUserMultiplier();
          }
        }));
      }

      $ctrl.wizardSteps = [
        { step: 1, labelKey: 'customerBookingNew.step1Label' },
        { step: 2, labelKey: 'customerBookingNew.step2Label' },
        { step: 3, labelKey: 'customerBookingNew.step3Label' },
        { step: 4, labelKey: 'customerBookingNew.step4Label' }
      ];

      $ctrl.serviceTypeOptions = [
        { value: 'PeriodicMaintenance', label: 'Periodic Maintenance (OEM Interval)' },
        { value: 'OilAndTire', label: 'Oil, Filter & Tire Service' },
        { value: 'Inspection', label: 'Safety & Diagnostic Inspection' },
        { value: 'GeneralRepair', label: 'General Mechanical Repair' },
        { value: 'WarrantyRepair', label: 'Authorized OEM Warranty Repair' }
      ];

      buildUpcomingDateOptions();

      var initialDate = $ctrl.upcomingDateOptions[0].value;

      $ctrl.currentStep = 1;
      $ctrl.isLoadingInit = false;
      $ctrl.isLoadingSlots = false;
      $ctrl.isSubmitting = false;
      $ctrl.loadProblem = null;
      $ctrl.slotConflictAlert = null;
      $ctrl.submitProblem = null;

      $ctrl.draft = {
        dealerId: ($stateParams && $stateParams.dealerId) || '',
        customerVehicleId: ($stateParams && $stateParams.vehicleId) || '',
        serviceType: 'PeriodicMaintenance',
        odometerAtBookingKm: 0,
        slotDate: initialDate,
        serviceTimeSlotId: '',
        slotRowVersion: '',
        appliedCustomerVoucherId: ($stateParams && $stateParams.voucherId) || '',
        voucherCode: '',
        voucherRowVersion: null,
        reportedNotes: ''
      };

      $ctrl.availableVouchers = [];
      $ctrl.dealers = [];
      $ctrl.vehicles = [];
      $ctrl.slots = [];
      $ctrl.loadWizardContext();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.formatDateOption = function (opt) {
      if (!opt) { return ''; }
      if (opt.dayOffset === 0) {
        return opt.value + ' (' + t('customerBookingNew.today', null, 'Today') + ')';
      }
      if (opt.dayOffset === 1) {
        return opt.value + ' (' + t('customerBookingNew.tomorrow', null, 'Tomorrow') + ')';
      }
      return opt.value;
    };

    $ctrl.formatQuickDate = function (quick) {
      if (!quick) { return ''; }
      if (quick.dayOffset === 0) {
        return t('customerBookingNew.today', null, 'Today');
      }
      if (quick.dayOffset === 1) {
        return t('customerBookingNew.tomorrow', null, 'Tomorrow');
      }
      return t('customerBookingNew.plusDays', { days: quick.dayOffset }, '+' + quick.dayOffset + 'd');
    };

    $ctrl.goToGarage = function () {
      if ($state && angular.isFunction($state.go)) {
        $state.go('app.customer.garage');
      }
    };

    $ctrl.pickVehicleCard = function (vehicle) {
      if (!vehicle) { return; }
      $ctrl.draft.customerVehicleId = vehicle.id;
      $ctrl.onVehicleSelected();
    };

    $ctrl.loadWizardContext = function () {
      $ctrl.isLoadingInit = true;
      $ctrl.loadProblem = null;
      var firstProblem = null;

      function captureError(rejection) {
        if (!firstProblem) {
          firstProblem = ProblemDetailsMapper.extract(rejection);
        }
        return [];
      }

      var vouchersPromise = (DealerApiService.vouchers && angular.isFunction(DealerApiService.vouchers.listMyVouchers))
        ? DealerApiService.vouchers.listMyVouchers({ status: 'Available' }).catch(function () { return []; })
        : $q.resolve([]);

      return $q.all({
        dealers: DealerApiService.listDealers().catch(captureError),
        vehicles: DealerApiService.listVehicles().catch(captureError),
        vouchers: vouchersPromise
      }).then(function (bundle) {
        if (isDestroyed) {
          return;
        }
        if (firstProblem) {
          $ctrl.loadProblem = firstProblem;
        }
        $ctrl.dealers = (bundle.dealers || []).filter(function (d) { return d.isActive; });
        $ctrl.vehicles = bundle.vehicles || [];
        $ctrl.availableVouchers = (bundle.vouchers || []).filter(function (v) {
          return v.effectiveStatus === 'Available' &&
            (v.applicableScope === 'ServiceOnly' || v.applicableScope === 'All');
        });

        if ($ctrl.draft.appliedCustomerVoucherId) {
          $ctrl.onVoucherSelected();
        }

        if (!$ctrl.draft.dealerId && $ctrl.dealers.length > 0) {
          $ctrl.draft.dealerId = $ctrl.dealers[0].id;
        }
        if (!$ctrl.draft.customerVehicleId && $ctrl.vehicles.length > 0) {
          $ctrl.draft.customerVehicleId = $ctrl.vehicles[0].id;
        }

        syncSelectedReferences();
        if ($ctrl.selectedVehicle && !$ctrl.draft.odometerAtBookingKm) {
          $ctrl.draft.odometerAtBookingKm = $ctrl.selectedVehicle.currentMileageKm;
        }

        if ($stateParams && $stateParams.dealerId && $stateParams.vehicleId) {
          $ctrl.currentStep = 3;
        } else if ($stateParams && $stateParams.dealerId) {
          $ctrl.currentStep = 2;
        }

        if ($ctrl.draft.dealerId) {
          return $ctrl.loadTimeSlots();
        }
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoadingInit = false;
        }
      });
    };

    $ctrl.loadTimeSlots = function () {
      if (!$ctrl.draft.dealerId || !$ctrl.draft.slotDate) {
        return $q.resolve($ctrl.slots);
      }
      $ctrl.isLoadingSlots = true;

      return DealerApiService.listTimeSlots({
        dealerId: $ctrl.draft.dealerId,
        fromDate: $ctrl.draft.slotDate,
        toDate: $ctrl.draft.slotDate
      }).then(function (rows) {
        if (isDestroyed) {
          return rows;
        }
        $ctrl.slots = rows;
        if ($ctrl.draft.serviceTimeSlotId) {
          var stillOpen = false;
          for (var i = 0; i < rows.length; i++) {
            if (rows[i].slotId === $ctrl.draft.serviceTimeSlotId && rows[i].isBookable) {
              $ctrl.draft.slotRowVersion = rows[i].rowVersion;
              stillOpen = true;
              break;
            }
          }
          if (!stillOpen) {
            $ctrl.draft.serviceTimeSlotId = '';
            $ctrl.draft.slotRowVersion = '';
          }
        }
        syncSelectedReferences();
        return rows;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          $ctrl.submitProblem = ProblemDetailsMapper.extract(rejection);
        }
        return [];
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoadingSlots = false;
        }
      });
    };

    $ctrl.isStepComplete = function (stepNum) {
      if (stepNum === 1) {
        return Boolean($ctrl.draft.dealerId);
      }
      if (stepNum === 2) {
        var minKm = $ctrl.selectedVehicle ? $ctrl.selectedVehicle.currentMileageKm : 0;
        return Boolean(
          $ctrl.draft.customerVehicleId &&
          $ctrl.draft.serviceType &&
          angular.isNumber(Number($ctrl.draft.odometerAtBookingKm)) &&
          Number($ctrl.draft.odometerAtBookingKm) >= minKm
        );
      }
      if (stepNum === 3) {
        return Boolean($ctrl.draft.slotDate && $ctrl.draft.serviceTimeSlotId);
      }
      return $ctrl.canSubmitBooking();
    };

    $ctrl.canNavigateToStep = function (targetStep) {
      if (targetStep <= 1) { return true; }
      if (targetStep === 2) { return $ctrl.isStepComplete(1); }
      if (targetStep === 3) { return $ctrl.isStepComplete(1) && $ctrl.isStepComplete(2); }
      if (targetStep === 4) { return $ctrl.isStepComplete(1) && $ctrl.isStepComplete(2) && $ctrl.isStepComplete(3); }
      return false;
    };

    $ctrl.goToStep = function (stepNum) {
      if (!$ctrl.canNavigateToStep(stepNum)) {
        return;
      }
      $ctrl.submitProblem = null;
      $ctrl.currentStep = stepNum;
      if (stepNum === 3) {
        $ctrl.loadTimeSlots();
      }
      if (stepNum === 4 && DealerApiService.vouchers && angular.isFunction(DealerApiService.vouchers.listMyVouchers)) {
        DealerApiService.vouchers.listMyVouchers({ status: 'Available' })
          .then(function (rows) {
            if (isDestroyed) { return; }
            $ctrl.availableVouchers = (rows || []).filter(function (v) {
              return v.effectiveStatus === 'Available' &&
                (v.applicableScope === 'ServiceOnly' || v.applicableScope === 'All');
            });
            if ($ctrl.draft.appliedCustomerVoucherId) {
              $ctrl.onVoucherSelected();
            }
          })
          .catch(angular.noop);
      }
    };

    $ctrl.onVoucherSelected = function () {
      var targetId = $ctrl.draft.appliedCustomerVoucherId;
      if (!targetId) {
        $ctrl.draft.voucherCode = '';
        $ctrl.draft.voucherRowVersion = null;
        return;
      }
      for (var i = 0; i < ($ctrl.availableVouchers || []).length; i++) {
        if ($ctrl.availableVouchers[i].id === targetId) {
          $ctrl.draft.voucherCode = $ctrl.availableVouchers[i].voucherCode;
          $ctrl.draft.voucherRowVersion = $ctrl.availableVouchers[i].rowVersion || null;
          return;
        }
      }
    };

    $ctrl.onManualVoucherCodeChanged = function () {
      var code = String($ctrl.draft.voucherCode || '').trim().toUpperCase();
      var matchedId = '';
      var matchedRowVer = null;
      for (var i = 0; i < ($ctrl.availableVouchers || []).length; i++) {
        if ($ctrl.availableVouchers[i].voucherCode === code) {
          matchedId = $ctrl.availableVouchers[i].id;
          matchedRowVer = $ctrl.availableVouchers[i].rowVersion || null;
          break;
        }
      }
      $ctrl.draft.appliedCustomerVoucherId = matchedId;
      $ctrl.draft.voucherRowVersion = matchedRowVer;
    };

    $ctrl.selectDealerAndAdvance = function (dealer) {
      if (!dealer) {
        return;
      }
      var changed = $ctrl.draft.dealerId !== dealer.id;
      $ctrl.draft.dealerId = dealer.id;
      if (changed) {
        $ctrl.draft.serviceTimeSlotId = '';
        $ctrl.draft.slotRowVersion = '';
      }
      syncSelectedReferences();
      $ctrl.currentStep = 2;
      $ctrl.loadTimeSlots();
    };

    $ctrl.onVehicleSelected = function () {
      syncSelectedReferences();
      if ($ctrl.selectedVehicle) {
        $ctrl.draft.odometerAtBookingKm = $ctrl.selectedVehicle.currentMileageKm;
      }
    };

    $ctrl.onSlotDateChanged = function () {
      $ctrl.draft.serviceTimeSlotId = '';
      $ctrl.draft.slotRowVersion = '';
      $ctrl.slotConflictAlert = null;
      syncSelectedReferences();
      $ctrl.loadTimeSlots();
    };

    $ctrl.pickQuickDate = function (dateStr) {
      $ctrl.draft.slotDate = dateStr;
      $ctrl.onSlotDateChanged();
    };

    $ctrl.jumpToNextDay = function () {
      var nextIso = DealerApiService.addDaysDateVn($ctrl.draft.slotDate, 1);
      var exists = false;
      for (var i = 0; i < $ctrl.upcomingDateOptions.length; i++) {
        if ($ctrl.upcomingDateOptions[i].value === nextIso) {
          exists = true;
          break;
        }
      }
      if (!exists) {
        $ctrl.upcomingDateOptions.push({ value: nextIso, dayOffset: 99, label: nextIso });
      }
      $ctrl.pickQuickDate(nextIso);
    };

    $ctrl.selectSlot = function (slot) {
      if (!slot || !slot.isBookable) {
        return;
      }
      $ctrl.draft.serviceTimeSlotId = slot.slotId;
      $ctrl.draft.slotRowVersion = slot.rowVersion || '';
      $ctrl.slotConflictAlert = null;
      syncSelectedReferences();
    };

    $ctrl.slotBadgeClass = function (slot) {
      if (!slot || slot.isBlocked || slot.remainingCapacity <= 0) {
        return 'badge-neutral';
      }
      if (slot.remainingCapacity === 1) {
        return 'badge-warning';
      }
      return 'badge-success';
    };

    $ctrl.slotBadgeIcon = function (slot) {
      if (!slot || slot.isBlocked) {
        return 'prohibit';
      }
      if (slot.remainingCapacity <= 0) {
        return 'x-circle';
      }
      if (slot.remainingCapacity === 1) {
        return 'warning';
      }
      return 'check-circle';
    };

    $ctrl.slotBadgeLabel = function (slot) {
      if (!slot) {
        return '';
      }
      if (slot.isBlocked) {
        return t(
          'customerBookingNew.badgeBlocked',
          { booked: slot.bookedCount, max: slot.maxCapacity },
          'Blocked (' + slot.bookedCount + '/' + slot.maxCapacity + ')'
        );
      }
      if (slot.remainingCapacity <= 0) {
        return t(
          'customerBookingNew.badgeFull',
          { max: slot.maxCapacity },
          'Full (0/' + slot.maxCapacity + ')'
        );
      }
      if (slot.remainingCapacity === 1) {
        return t(
          'customerBookingNew.badgeLastBay',
          { max: slot.maxCapacity },
          'Last Bay (1/' + slot.maxCapacity + ')'
        );
      }
      return t(
        'customerBookingNew.badgeOpen',
        { remaining: slot.remainingCapacity, max: slot.maxCapacity },
        slot.remainingCapacity + '/' + slot.maxCapacity + ' Bays Open'
      );
    };

    $ctrl.canSubmitBooking = function () {
      return $ctrl.isStepComplete(1) && $ctrl.isStepComplete(2) && $ctrl.isStepComplete(3);
    };

    $ctrl.handleBookingSubmitError = function (rejection) {
      var problem = ProblemDetailsMapper.extract(rejection);
      if (
        problem.status === 409 &&
        (problem.errorCode === 'SLOT_CAPACITY_EXHAUSTED' ||
         problem.errorCode === 'CONCURRENCY_CONFLICT' ||
         String(problem.errorCode || '').indexOf('SLOT') !== -1)
      ) {
        $ctrl.submitProblem = null;
        $ctrl.draft.serviceTimeSlotId = '';
        $ctrl.draft.slotRowVersion = '';
        syncSelectedReferences();
        $ctrl.slotConflictAlert = {
          title: problem.title || t('customerBookingNew.slotConflictTitle', null, 'Selected Time Slot Just Reached Capacity'),
          detail: problem.detail || t('customerBookingNew.slotConflictDetail', null, 'Another customer claimed the last bay moments ago.'),
          errorCode: problem.errorCode || 'SLOT_CAPACITY_EXHAUSTED',
          traceId: problem.traceId
        };
        $ctrl.currentStep = 3;
        ToastService.warning(
          t('customerBookingNew.slotConflictToastTitle', null, 'Time Slot Just Filled'),
          t('customerBookingNew.slotConflictToastBody', null, 'Refreshing live bay availability — please pick another open slot.')
        );
        return $ctrl.loadTimeSlots();
      }

      $ctrl.submitProblem = problem;
      if (problem.errorCode === 'ODOMETER_REGRESSION') {
        $ctrl.currentStep = 2;
      }
      return $q.resolve(problem);
    };

    $ctrl.submitBooking = function () {
      if (!$ctrl.canSubmitBooking() || $ctrl.isSubmitting) {
        return $q.resolve(null);
      }
      $ctrl.isSubmitting = true;
      $ctrl.submitProblem = null;
      $ctrl.slotConflictAlert = null;

      return DealerApiService.createBooking($ctrl.draft)
        .then(function (created) {
          if (isDestroyed) {
            return created;
          }
          ToastService.success(
            t('customerBookingNew.bookedToastTitle', null, 'Workshop Appointment Booked'),
            t(
              'customerBookingNew.bookedToastBody',
              { number: created.bookingNumber, date: created.slotDate, window: created.slotWindow },
              'Booking ' + created.bookingNumber + ' confirmed in queue for ' + created.slotDate + ' (' + created.slotWindow + ').'
            )
          );
          $state.go('app.customer.bookings');
          return created;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            return $ctrl.handleBookingSubmitError(rejection);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSubmitting = false;
          }
        });
    };
  }
})();
