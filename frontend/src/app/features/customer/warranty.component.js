(function () {
  'use strict';

  var WARRANTY_STEPPER_STAGES = [
    { code: 'Submitted' },
    { code: 'UnderReview' },
    { code: 'Approved' },
    { code: 'Completed' }
  ];

  var WARRANTY_STAGE_INDEX = {
    Submitted: 0,
    UnderReview: 1,
    Approved: 2,
    Completed: 3
  };

  var VI_PATCH = {
    customerWarranty: {
      eyebrow: 'BẢO HÀNH CHÍNH HÃNG OEM • 36 THÁNG / 30.000 KM',
      refreshing: 'Đang làm mới...',
      refreshClaims: 'Làm mới hồ sơ',
      backToGarage: 'Garage VIN của tôi',
      createdTitle: 'Đã gửi yêu cầu bảo hành VIN:',
      createdSentTo: 'đã chuyển thời gian thực tới',
      dismiss: 'Ẩn',
      formTitle: 'Gửi yêu cầu bảo hành OEM theo VIN',
      vehicleLabel: 'Chọn xe trong Garage (Số khung VIN ISO 3779) *',
      vehiclePlaceholder: '-- Chọn xe máy đã đăng ký --',
      coverageHeading: 'Quyền lợi bảo hành OEM (36 tháng / 30.000 km)',
      mileageGaugeLabel: 'Quãng đường đã sử dụng',
      timeGaugeLabel: 'Thời hạn bảo hành còn lại',
      monthsLeftShort: '~{{months}} / {{max}} tháng',
      errOdoRegression: 'Số ODO ({{candidate}} km) không được nhỏ hơn số đang ghi nhận ({{recorded}} km).',
      errMileageExceeded: 'Số ODO ({{current}} km) đã vượt giới hạn bảo hành {{max}} km của hãng.',
      errTimeExceeded: 'Xe đã vượt quá thời hạn bảo hành chính hãng {{max}} tháng.',
      dealerLabel: 'Đại lý tiếp nhận thẩm định *',
      dealerPlaceholder: '-- Chọn chi nhánh đại lý --',
      odoAtClaimLabel: 'Số ODO lúc báo lỗi (km) *',
      odoMinHint: 'Phải >= {{minKm}} km đã ghi nhận.',
      symptomRulesLabel: 'Mô tả triệu chứng & hiện tượng lỗi * (10–1.000 ký tự)',
      submitting: 'Đang gửi yêu cầu bảo hành...',
      tabAll: 'Tất cả hồ sơ',
      tabOpen: 'Đang mở / Thẩm định',
      tabCompleted: 'Hoàn tất',
      tabRejected: 'Từ chối',
      liveBadge: 'TRỰC TIẾP',
      fallbackVehicleName: 'Xe máy đã đăng ký',
      claimOdoLabel: 'ODO báo lỗi',
      filedLabel: 'Ngày gửi',
      symptomPrefix: 'Triệu chứng',
      oemCoveredPrefix: 'Hãng chi trả',
      oemPartPrefix: 'Phụ tùng OEM',
      oemPartFallback: 'Phụ tùng thay thế OEM',
      resolutionPrefix: 'Kết luận kỹ thuật xưởng',
      rejectionPrefix: 'Lý do từ chối',
      createdToastTitle: 'Đã gửi yêu cầu bảo hành OEM',
      createdToastBody: 'Hồ sơ {{number}} cho VIN {{vin}} ({{category}}).'
    }
  };

  var EN_PATCH = {
    customerWarranty: {
      eyebrow: 'OEM VIN WARRANTY • 36 MONTHS / 30,000 KM',
      refreshing: 'Refreshing...',
      refreshClaims: 'Refresh Claims',
      backToGarage: 'My VIN Garage',
      createdTitle: 'OEM Warranty Claim Submitted:',
      createdSentTo: 'sent in real time to',
      dismiss: 'Dismiss',
      formTitle: 'File OEM VIN Warranty Claim',
      vehicleLabel: 'Select Garage Motorcycle (ISO 3779 VIN) *',
      vehiclePlaceholder: '-- Select Registered VIN --',
      coverageHeading: 'OEM Coverage Gauge (36 Months / 30,000 km)',
      mileageGaugeLabel: 'Mileage Usage',
      timeGaugeLabel: 'Time Coverage Remaining',
      monthsLeftShort: '~{{months}} / {{max}} mo',
      errOdoRegression: 'Odometer ({{candidate}} km) cannot regress below recorded {{recorded}} km.',
      errMileageExceeded: 'Odometer ({{current}} km) exceeds the {{max}} km OEM warranty limit.',
      errTimeExceeded: 'Vehicle has exceeded the {{max}}-month OEM warranty coverage window.',
      dealerLabel: 'Authorized Servicing Dealership *',
      dealerPlaceholder: '-- Select Servicing Branch --',
      odoAtClaimLabel: 'Odometer at Claim (km) *',
      odoMinHint: 'Must be >= recorded {{minKm}} km.',
      symptomRulesLabel: 'Detailed Symptom & Failure Description * (10–1,000 chars)',
      submitting: 'Submitting Warranty Claim...',
      tabAll: 'All Claims',
      tabOpen: 'Open / In Review',
      tabCompleted: 'Completed',
      tabRejected: 'Rejected',
      liveBadge: 'LIVE',
      fallbackVehicleName: 'Registered Motorcycle',
      claimOdoLabel: 'Claim Odo',
      filedLabel: 'Filed',
      symptomPrefix: 'Symptom',
      oemCoveredPrefix: 'OEM Covered',
      oemPartPrefix: 'Part',
      oemPartFallback: 'OEM Replacement',
      resolutionPrefix: 'Workshop Resolution Notes',
      rejectionPrefix: 'Claim Rejection Reason',
      createdToastTitle: 'OEM Warranty Claim Submitted',
      createdToastBody: '{{number}} filed for VIN {{vin}} ({{category}}).'
    }
  };

  registerCustomerWarrantyI18n.$inject = ['I18nService'];
  function registerCustomerWarrantyI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerWarrantyI18n)
    .component('customerWarranty', {
      controllerAs: '$ctrl',
      controller: CustomerWarrantyController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="shield-check" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerWarranty.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">{{ \'warranty.pageTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'warranty.pageSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadWarrantyClaims()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerWarranty.refreshing\' | t) : (\'customerWarranty.refreshClaims\' | t) }}</span>' +
              '</button>' +
              '<a class="btn btn-secondary" ui-sref="app.customer.garage">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'customerWarranty.backToGarage\' | t }}</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.createdBanner" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;">' +
              '<app-icon name="shield-check" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerWarranty.createdTitle\' | t }}</strong> ' +
                '<span class="mono-num">{{ $ctrl.createdBanner.claimNumber }}</span> • ' +
                '<span class="mono-num">{{ $ctrl.createdBanner.vin }}</span> ' +
                '({{ $ctrl.createdBanner.defectCategory | enumT:\'WarrantyDefectCategory\' }}) — ' +
                '{{ \'customerWarranty.createdSentTo\' | t }} <strong>{{ $ctrl.createdBanner.dealerName }}</strong>.' +
              '</div>' +
            '</div>' +
            '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.createdBanner = null">{{ \'customerWarranty.dismiss\' | t }}</button>' +
          '</div>' +

          '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.submitProblem">' +
            '<div><strong>{{ $ctrl.submitProblem.title }} ({{ $ctrl.submitProblem.errorCode }}):</strong> {{ $ctrl.submitProblem.detail }}</div>' +
            '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.submitProblem.traceId">traceId: {{ $ctrl.submitProblem.traceId }}</div>' +
          '</div>' +

          '<div class="grid-12">' +
            /* Left Column: File New OEM VIN Warranty Claim inside Double-Bezel Card */
            '<div class="col-5">' +
              '<div class="bezel-card">' +
                '<div class="bezel-card-inner">' +
                  '<div class="surface-card-header">' +
                    '<div style="display:flex;align-items:center;gap:8px;">' +
                      '<app-icon name="shield-check" weight="duotone" size="20"></app-icon>' +
                      '<h2 class="surface-card-title">{{ \'customerWarranty.formTitle\' | t }}</h2>' +
                    '</div>' +
                  '</div>' +

                  '<form name="$ctrl.warrantyForm" ng-submit="$ctrl.submitWarrantyClaim()" novalidate>' +
                    '<div class="form-group">' +
                      '<label class="form-label" for="wcVehicleSelect">{{ \'customerWarranty.vehicleLabel\' | t }}</label>' +
                      '<select id="wcVehicleSelect" name="customerVehicleId" class="form-control mono-num" ' +
                              'ng-model="$ctrl.draft.customerVehicleId" ' +
                              'ng-options="v.id as (v.vin + \' • \' + v.motorcycleModelName + \' (\' + (v.currentMileageKm | number) + \' km)\') for v in $ctrl.vehicles" ' +
                              'ng-change="$ctrl.onVehicleSelected()" required>' +
                        '<option value="">{{ \'customerWarranty.vehiclePlaceholder\' | t }}</option>' +
                      '</select>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.customerVehicleId">{{ $ctrl.fieldErrors.customerVehicleId }}</div>' +
                    '</div>' +

                    /* Live 36-Month / 30,000-km Visual OEM Coverage Gauge (GPU scaleX) + <bike-image variant="thumb"> */
                    '<div ng-if="$ctrl.isLoading && $ctrl.vehicles.length === 0" style="margin-bottom:16px;">' +
                      '<skeleton-loader variant="detail" count="1"></skeleton-loader>' +
                    '</div>' +
                    '<div class="surface-sunken fade-slide-up" ng-if="$ctrl.selectedVehicle && $ctrl.selectedEligibility" ' +
                         'style="padding:14px;margin-bottom:16px;font-size:12px;">' +
                      '<div style="display:flex;align-items:center;gap:12px;margin-bottom:12px;">' +
                        '<div style="width:84px;flex-shrink:0;">' +
                          '<bike-image src="$ctrl.selectedVehicle.imageUrl" alt="{{ $ctrl.selectedVehicle.motorcycleModelName }}" variant="thumb"></bike-image>' +
                        '</div>' +
                        '<div style="min-width:0;flex:1;">' +
                          '<div class="text-primary" style="font-weight:700;font-size:13px;">{{ $ctrl.selectedVehicle.motorcycleModelName }}</div>' +
                          '<div class="mono-num text-muted" style="font-size:11px;margin-top:2px;">{{ $ctrl.selectedVehicle.vin }}</div>' +
                          '<div style="margin-top:6px;">' +
                            '<span class="badge" ng-class="$ctrl.selectedEligibility.isCovered ? \'badge-success\' : \'badge-danger\'">' +
                              '<app-icon name="{{ $ctrl.selectedEligibility.isCovered ? \'shield-check\' : \'warning-circle\' }}" weight="fill" size="13"></app-icon>' +
                              '<span>{{ $ctrl.selectedEligibility.isCovered ? (\'warranty.coveredBadge\' | t) : (\'warranty.expiredBadge\' | t) }}</span>' +
                            '</span>' +
                          '</div>' +
                        '</div>' +
                      '</div>' +

                      /* Gauge 1: Mileage Usage vs 30,000 km Limit (GPU scaleX) */
                      '<div style="margin-bottom:10px;">' +
                        '<div style="display:flex;justify-content:space-between;gap:8px;margin-bottom:4px;">' +
                          '<span class="text-secondary">{{ \'customerWarranty.mileageGaugeLabel\' | t }}</span>' +
                          '<strong class="mono-num text-primary">{{ $ctrl.selectedEligibility.currentMileageKm | number }} / {{ $ctrl.selectedEligibility.maxMileageKm | number }} km</strong>' +
                        '</div>' +
                        '<div class="progress-track">' +
                          '<div class="progress-fill" ' +
                               'ng-class="{\'is-due\': $ctrl.coverageGauges.mileageRatio >= 0.8 && $ctrl.coverageGauges.mileageRatio < 1, \'is-overdue\': $ctrl.coverageGauges.mileageRatio >= 1}" ' +
                               'style="width:100%;transform-origin:left center;transition:transform var(--dur-emphasis) var(--ease-expo);" ' +
                               'ng-style="{transform: \'scaleX(\' + $ctrl.coverageGauges.mileageRatio + \')\'}"></div>' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:11px;margin-top:3px;">' +
                          '{{ \'warranty.remainingMileage\' | t }}: <strong class="mono-num text-secondary">{{ $ctrl.selectedEligibility.remainingKm | number }} km</strong>' +
                        '</div>' +
                      '</div>' +

                      /* Gauge 2: Time Coverage Remaining vs 36 Months (GPU scaleX) */
                      '<div>' +
                        '<div style="display:flex;justify-content:space-between;gap:8px;margin-bottom:4px;">' +
                          '<span class="text-secondary">{{ \'customerWarranty.timeGaugeLabel\' | t }}</span>' +
                          '<strong class="mono-num text-primary">' +
                            '{{ \'customerWarranty.monthsLeftShort\' | t:{ months: $ctrl.selectedEligibility.remainingMonths, max: $ctrl.selectedEligibility.maxMonths } }}' +
                          '</strong>' +
                        '</div>' +
                        '<div class="progress-track">' +
                          '<div class="progress-fill" ' +
                               'ng-class="{\'is-due\': $ctrl.coverageGauges.timeRemainingRatio <= 0.25 && $ctrl.coverageGauges.timeRemainingRatio > 0, \'is-overdue\': $ctrl.coverageGauges.timeRemainingRatio <= 0}" ' +
                               'style="width:100%;transform-origin:left center;transition:transform var(--dur-emphasis) var(--ease-expo);" ' +
                               'ng-style="{transform: \'scaleX(\' + $ctrl.coverageGauges.timeRemainingRatio + \')\'}"></div>' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:11px;margin-top:3px;" ng-if="$ctrl.selectedEligibility.warrantyExpiresAtUtc">' +
                          '{{ \'warranty.expiresOn\' | t }}: <strong class="mono-num text-secondary">{{ $ctrl.selectedEligibility.warrantyExpiresAtUtc | utcToLocal:\'date\' }}</strong>' +
                        '</div>' +
                      '</div>' +

                      '<div class="field-error" style="margin-top:8px;" ng-if="$ctrl.selectedEligibility.ineligibleKey || $ctrl.selectedEligibility.ineligibleReason">' +
                        '{{ $ctrl.selectedEligibility.ineligibleKey ? ($ctrl.selectedEligibility.ineligibleKey | t:$ctrl.selectedEligibility.ineligibleParams) : $ctrl.selectedEligibility.ineligibleReason }}' +
                      '</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="wcDealerSelect">{{ \'customerWarranty.dealerLabel\' | t }}</label>' +
                      '<select id="wcDealerSelect" name="dealerId" class="form-control" ' +
                              'ng-model="$ctrl.draft.dealerId" ' +
                              'ng-options="d.id as (d.code + \' • \' + d.name + \' (\' + d.city + \')\') for d in $ctrl.dealers" ' +
                              'ng-change="$ctrl.onFieldEdited(\'dealerId\')" required>' +
                        '<option value="">{{ \'customerWarranty.dealerPlaceholder\' | t }}</option>' +
                      '</select>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.dealerId">{{ $ctrl.fieldErrors.dealerId }}</div>' +
                    '</div>' +

                    '<div class="grid-12">' +
                      '<div class="col-7 form-group">' +
                        '<label class="form-label" for="wcDefectSelect">{{ \'warranty.defectCategoryLabel\' | t }} *</label>' +
                        '<select id="wcDefectSelect" name="defectCategory" class="form-control" ' +
                                'ng-model="$ctrl.draft.defectCategory" ' +
                                'ng-options="c.value as (c.value | enumT:\'WarrantyDefectCategory\') for c in $ctrl.defectCategories" ' +
                                'ng-change="$ctrl.onFieldEdited(\'defectCategory\')" required></select>' +
                        '<div class="field-error" ng-if="$ctrl.fieldErrors.defectCategory">{{ $ctrl.fieldErrors.defectCategory }}</div>' +
                      '</div>' +

                      '<div class="col-5 form-group">' +
                        '<label class="form-label" for="wcOdoInput">{{ \'customerWarranty.odoAtClaimLabel\' | t }}</label>' +
                        '<input id="wcOdoInput" name="odometerAtClaimKm" type="number" min="0" max="1000000" ' +
                               'class="form-control mono-num" ' +
                               'ng-model="$ctrl.draft.odometerAtClaimKm" ' +
                               'ng-change="$ctrl.onOdometerEdited()" required />' +
                        '<div class="field-hint" ng-if="$ctrl.selectedVehicle && $ctrl.draft.odometerAtClaimKm < $ctrl.selectedVehicle.currentMileageKm" style="color:var(--clr-danger-fg);">' +
                          '{{ \'customerWarranty.odoMinHint\' | t:{ minKm: ($ctrl.selectedVehicle.currentMileageKm | number) } }}' +
                        '</div>' +
                        '<div class="field-error" ng-if="$ctrl.fieldErrors.odometerAtClaimKm">{{ $ctrl.fieldErrors.odometerAtClaimKm }}</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="form-group">' +
                      '<label class="form-label" for="wcSymptomInput">{{ \'customerWarranty.symptomRulesLabel\' | t }}</label>' +
                      '<textarea id="wcSymptomInput" name="symptomDescription" rows="3" minlength="10" maxlength="1000" class="form-control" ' +
                                'ng-model="$ctrl.draft.symptomDescription" ' +
                                'ng-change="$ctrl.onFieldEdited(\'symptomDescription\')" required ' +
                                'ng-attr-placeholder="{{ \'warranty.symptomPlaceholder\' | t }}"></textarea>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.symptomDescription">{{ $ctrl.fieldErrors.symptomDescription }}</div>' +
                    '</div>' +

                    '<button type="submit" id="submitWarrantyClaimBtn" class="btn-pill-cta" style="width:100%;" ' +
                            'has-permission="Permissions.Warranty.CreateOwn" ' +
                            'ng-disabled="$ctrl.isSubmitting || !$ctrl.canSubmitWarrantyClaim()">' +
                      '<span>{{ $ctrl.isSubmitting ? (\'customerWarranty.submitting\' | t) : (\'warranty.submitClaimCta\' | t) }}</span>' +
                      '<span class="btn-pill-well" aria-hidden="true">' +
                        '<app-icon name="shield-check" weight="regular" size="15"></app-icon>' +
                      '</span>' +
                    '</button>' +
                  '</form>' +
                '</div>' +
              '</div>' +
            '</div>' +

            /* Right Column: My Warranty Claims & Live Status Stepper */
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

              '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.claims.length === 0" variant="table" count="4"></skeleton-loader>' +

              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredClaims.length === 0" ' +
                           'icon="shield-check" ' +
                           'title-key="warranty.emptyClaimsTitle" ' +
                           'desc-key="warranty.emptyClaimsDesc">' +
              '</empty-state>' +

              '<div style="display:flex;flex-direction:column;gap:14px;" ng-if="$ctrl.filteredClaims.length > 0">' +
                '<article class="bezel-card stagger-item" ' +
                         'ng-repeat="clm in $ctrl.filteredClaims track by clm.id" ' +
                         'ng-class="{\'is-signalr-pulse\': clm.isLiveArrival}" ' +
                         'ng-attr-style="--i:{{ $index }};">' +
                  '<div class="bezel-card-inner">' +
                    '<div style="display:flex;align-items:flex-start;gap:14px;flex-wrap:wrap;">' +
                      '<div style="width:92px;flex-shrink:0;">' +
                        '<bike-image src="$ctrl.getVehicleImageUrl(clm.customerVehicleId, clm.vin, clm.imageUrl || clm.vehicleImageUrl)" ' +
                                    'alt="{{ clm.motorcycleModelName || clm.vin }}" ' +
                                    'variant="thumb">' +
                        '</bike-image>' +
                      '</div>' +

                      '<div style="flex:1;min-width:240px;">' +
                        '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                          '<strong class="mono-num text-primary" style="font-size:15px;">{{ clm.claimNumber }}</strong>' +
                          '<span class="badge" ng-class="\'badge-status-\' + clm.status.toLowerCase()">' +
                            '<app-icon name="{{ $ctrl.claimStatusIcon(clm.status) }}" weight="fill" size="13"></app-icon>' +
                            '<span>{{ clm.status | enumT:\'WarrantyClaimStatus\' }}</span>' +
                          '</span>' +
                          '<span class="badge badge-neutral">{{ clm.defectCategory | enumT:\'WarrantyDefectCategory\' }}</span>' +
                          '<span class="badge badge-danger mono-num" ng-if="clm.isLiveArrival">' +
                            '<app-icon name="lightning" weight="fill" size="12"></app-icon>' +
                            '<span>{{ \'customerWarranty.liveBadge\' | t }}</span>' +
                          '</span>' +
                        '</div>' +

                        '<div class="text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">' +
                          '{{ clm.motorcycleModelName || (\'customerWarranty.fallbackVehicleName\' | t) }} • <span class="mono-num">{{ clm.vin }}</span>' +
                        '</div>' +

                        '<div class="text-muted" style="font-size:12px;margin-top:4px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                          '<span>{{ clm.dealerName || clm.dealerCode }}</span>' +
                          '<span aria-hidden="true">•</span>' +
                          '<span>{{ \'customerWarranty.claimOdoLabel\' | t }}: <strong class="mono-num text-secondary">{{ clm.odometerAtClaimKm | number }} km</strong></span>' +
                          '<span ng-if="clm.createdAtUtc">• {{ \'customerWarranty.filedLabel\' | t }}: <span class="mono-num">{{ clm.createdAtUtc | utcToLocal }}</span></span>' +
                        '</div>' +

                        '<div class="text-secondary" style="font-size:13px;margin-top:8px;">' +
                          '<strong class="text-primary">{{ \'customerWarranty.symptomPrefix\' | t }}:</strong> {{ clm.symptomDescription }}' +
                        '</div>' +

                        '<div style="margin-top:10px;display:flex;gap:8px;flex-wrap:wrap;align-items:center;" ' +
                             'ng-if="clm.coveredSparePartCode || clm.coveredSparePartName || clm.coveredAmountVnd > 0">' +
                          '<span class="badge badge-success mono-num">' +
                            '<app-icon name="shield-check" weight="fill" size="13"></app-icon>' +
                            '<span>{{ \'customerWarranty.oemCoveredPrefix\' | t }}: {{ clm.coveredAmountVnd | vndCurrency }}</span>' +
                          '</span>' +
                          '<span class="badge badge-info mono-num" ng-if="clm.coveredSparePartCode || clm.coveredSparePartName">' +
                            '<app-icon name="wrench" weight="regular" size="13"></app-icon>' +
                            '<span>{{ \'customerWarranty.oemPartPrefix\' | t }}: {{ clm.coveredSparePartCode ? (clm.coveredSparePartCode + \' • \') : \'\' }}{{ clm.coveredSparePartName || (\'customerWarranty.oemPartFallback\' | t) }}</span>' +
                          '</span>' +
                        '</div>' +

                        '<div class="staff-reply-callout" ng-if="clm.resolutionNotes">' +
                          '<div style="display:flex;align-items:center;gap:6px;margin-bottom:3px;">' +
                            '<app-icon name="check-circle" weight="fill" size="14"></app-icon>' +
                            '<strong style="color:var(--clr-success-fg);">{{ \'customerWarranty.resolutionPrefix\' | t }}:</strong>' +
                          '</div>' +
                          '<span class="text-primary">{{ clm.resolutionNotes }}</span>' +
                        '</div>' +

                        '<div class="alert-banner alert-danger" style="margin:8px 0 0;padding:8px 12px;font-size:12px;" ng-if="clm.rejectionReason">' +
                          '<strong>{{ \'customerWarranty.rejectionPrefix\' | t }}:</strong> {{ clm.rejectionReason }}' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    /* Status Stepper: Submitted -> UnderReview -> Approved -> Completed / Rejected */
                    '<div class="status-stepper-track" ng-if="clm.status !== \'Rejected\'">' +
                      '<span class="status-step-node" ' +
                            'ng-repeat="st in $ctrl.stepperStages track by st.code" ' +
                            'ng-class="{\'is-done\': $ctrl.isStageDone(clm.status, st.code), \'is-current\': clm.status === st.code}">' +
                        '<app-icon name="{{ $ctrl.isStageDone(clm.status, st.code) ? \'check\' : (clm.status === st.code ? \'lightning\' : \'clock\') }}" ' +
                                  'weight="{{ clm.status === st.code ? \'fill\' : \'regular\' }}" size="13"></app-icon>' +
                        '<span>{{ st.code | enumT:\'WarrantyClaimStatus\' }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="status-stepper-track" ng-if="clm.status === \'Rejected\'">' +
                      '<span class="status-step-node is-done">' +
                        '<app-icon name="check" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'Submitted\' | enumT:\'WarrantyClaimStatus\' }}</span>' +
                      '</span>' +
                      '<span class="status-step-node is-rejected">' +
                        '<app-icon name="x" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'Rejected\' | enumT:\'WarrantyClaimStatus\' }}</span>' +
                      '</span>' +
                    '</div>' +
                  '</div>' +
                '</article>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  CustomerWarrantyController.$inject = [
    '$q',
    '$rootScope',
    '$stateParams',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function CustomerWarrantyController(
    $q,
    $rootScope,
    $stateParams,
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

    function enumT(group, code) {
      if (I18nService && angular.isFunction(I18nService.enumLabel)) {
        return I18nService.enumLabel(group, code);
      }
      return code || '';
    }

    function runInDigest(fn) {
      if ($rootScope && angular.isFunction($rootScope.$applyAsync)) {
        $rootScope.$applyAsync(fn);
      } else {
        fn();
      }
    }

    function matchesTab(claim, tabKey) {
      if (tabKey === 'OPEN') {
        return claim.status === 'Submitted' || claim.status === 'UnderReview' || claim.status === 'Approved';
      }
      if (tabKey === 'COMPLETED') {
        return claim.status === 'Completed';
      }
      if (tabKey === 'REJECTED') {
        return claim.status === 'Rejected';
      }
      return true;
    }

    function findSelectedVehicle(vehicleId) {
      var list = $ctrl.vehicles || [];
      for (var i = 0; i < list.length; i++) {
        if (list[i].id === vehicleId) {
          return list[i];
        }
      }
      return null;
    }

    function refreshSelectedVehicleAndEligibility(prefillOdometer) {
      var vehicle = findSelectedVehicle($ctrl.draft.customerVehicleId);
      $ctrl.selectedVehicle = vehicle;
      if (!vehicle) {
        $ctrl.selectedEligibility = null;
        $ctrl.coverageGauges = { mileageRatio: 0, timeRemainingRatio: 0 };
        return;
      }
      if (prefillOdometer) {
        $ctrl.draft.odometerAtClaimKm = vehicle.currentMileageKm;
      }
      var candidateKm = ($ctrl.draft.odometerAtClaimKm !== null && $ctrl.draft.odometerAtClaimKm !== undefined && !isNaN(Number($ctrl.draft.odometerAtClaimKm)))
        ? Math.max(0, Math.floor(Number($ctrl.draft.odometerAtClaimKm)))
        : vehicle.currentMileageKm;
      var evalInput = angular.extend({}, vehicle, { currentMileageKm: candidateKm });
      var eligibility = DealerApiService.evaluateVehicleWarrantyEligibility(evalInput);

      function fmtInt(n) {
        if (I18nService && angular.isFunction(I18nService.formatNumber)) {
          return I18nService.formatNumber(n);
        }
        return Number(n || 0).toLocaleString('vi-VN');
      }

      if (candidateKm < vehicle.currentMileageKm) {
        eligibility.isCovered = false;
        eligibility.isEligible = false;
        eligibility.ineligibleReason = 'Odometer (' + candidateKm.toLocaleString('en-US') + ' km) cannot regress below recorded ' + vehicle.currentMileageKm.toLocaleString('en-US') + ' km.';
        eligibility.ineligibleKey = 'customerWarranty.errOdoRegression';
        eligibility.ineligibleParams = {
          candidate: fmtInt(candidateKm),
          recorded: fmtInt(vehicle.currentMileageKm)
        };
      } else if (candidateKm > eligibility.maxMileageKm) {
        eligibility.ineligibleKey = 'customerWarranty.errMileageExceeded';
        eligibility.ineligibleParams = {
          current: fmtInt(candidateKm),
          max: fmtInt(eligibility.maxMileageKm)
        };
      } else if (!eligibility.isCovered) {
        eligibility.ineligibleKey = 'customerWarranty.errTimeExceeded';
        eligibility.ineligibleParams = {
          max: eligibility.maxMonths
        };
      } else {
        eligibility.ineligibleKey = null;
        eligibility.ineligibleParams = null;
      }

      var mileageRatio = eligibility.maxMileageKm > 0
        ? Math.max(0, Math.min(1, candidateKm / eligibility.maxMileageKm))
        : 0;
      var timeRemainingRatio = eligibility.maxMonths > 0
        ? Math.max(0, Math.min(1, eligibility.remainingMonths / eligibility.maxMonths))
        : 0;

      $ctrl.selectedEligibility = eligibility;
      $ctrl.coverageGauges = {
        mileageRatio: Number(mileageRatio.toFixed(4)),
        timeRemainingRatio: Number(timeRemainingRatio.toFixed(4))
      };
    }

    function applyDefaultSelections(validateMembership) {
      var vehicles = $ctrl.vehicles || [];
      var dealers = $ctrl.dealers || [];
      var requestedVehicleRemoved = false;

      if (!$ctrl.draft.customerVehicleId) {
        if (vehicles.length > 0) {
          var coveredFirst = null;
          for (var c = 0; c < vehicles.length; c++) {
            if (vehicles[c].isWarrantyCovered) {
              coveredFirst = vehicles[c];
              break;
            }
          }
          $ctrl.draft.customerVehicleId = (coveredFirst || vehicles[0]).id;
          requestedVehicleRemoved = true;
        }
      } else if (validateMembership && vehicles.length > 0 && !findSelectedVehicle($ctrl.draft.customerVehicleId)) {
        $ctrl.draft.customerVehicleId = vehicles[0].id;
        requestedVehicleRemoved = true;
      }

      if (!$ctrl.draft.dealerId) {
        if (dealers.length > 0) {
          $ctrl.draft.dealerId = dealers[0].id;
        }
      } else if (validateMembership && dealers.length > 0) {
        var foundDealer = false;
        for (var j = 0; j < dealers.length; j++) {
          if (dealers[j].id === $ctrl.draft.dealerId) {
            foundDealer = true;
            break;
          }
        }
        if (!foundDealer) {
          $ctrl.draft.dealerId = dealers[0].id;
        }
      }

      refreshSelectedVehicleAndEligibility(requestedVehicleRemoved || $ctrl.draft.odometerAtClaimKm === null);
    }

    $ctrl.$onInit = function () {
      var initVehicleId = ($stateParams && $stateParams.vehicleId) ? String($stateParams.vehicleId) : '';
      var initDealerId = ($stateParams && $stateParams.dealerId) ? String($stateParams.dealerId) : '';

      $ctrl.isLoading = false;
      $ctrl.isSubmitting = false;
      $ctrl.submitProblem = null;
      $ctrl.listProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.createdBanner = null;
      $ctrl.activeTab = 'ALL';

      $ctrl.tabs = [
        { key: 'ALL', labelKey: 'customerWarranty.tabAll' },
        { key: 'OPEN', labelKey: 'customerWarranty.tabOpen' },
        { key: 'COMPLETED', labelKey: 'customerWarranty.tabCompleted' },
        { key: 'REJECTED', labelKey: 'customerWarranty.tabRejected' }
      ];

      $ctrl.stepperStages = WARRANTY_STEPPER_STAGES.map(function (st) {
        return angular.extend({}, st);
      });
      $ctrl.defectCategories = DealerApiService.getWarrantyDefectCategories();

      $ctrl.draft = {
        customerVehicleId: initVehicleId,
        dealerId: initDealerId,
        defectCategory: 'Engine',
        odometerAtClaimKm: null,
        symptomDescription: ''
      };

      $ctrl.loadProblem = null;
      $ctrl.vehicles = [];
      $ctrl.dealers = [];
      $ctrl.selectedVehicle = null;
      $ctrl.selectedEligibility = null;
      $ctrl.coverageGauges = { mileageRatio: 0, timeRemainingRatio: 0 };
      $ctrl.claims = [];
      $ctrl.applyTabFilter();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('WarrantyClaimStatusChanged', function (payload) {
          if (isDestroyed || !payload) {
            return;
          }
          runInDigest(function () {
            if (isDestroyed) { return; }
            var targetId = payload.claimId || payload.id || payload.ClaimId || '';
            var targetNum = payload.claimNumber || payload.ClaimNumber || '';
            var matched = false;
            for (var i = 0; i < $ctrl.claims.length; i++) {
              if (
                (targetId && $ctrl.claims[i].id === targetId) ||
                (targetNum && $ctrl.claims[i].claimNumber === targetNum)
              ) {
                $ctrl.claims[i].status = payload.newStatus || payload.status || payload.NewStatus || $ctrl.claims[i].status;
                if (payload.coveredAmountVnd !== undefined && payload.coveredAmountVnd !== null) {
                  $ctrl.claims[i].coveredAmountVnd = Math.max(0, Number(payload.coveredAmountVnd) || 0);
                }
                if (payload.resolutionNotes) {
                  $ctrl.claims[i].resolutionNotes = payload.resolutionNotes;
                }
                if (payload.rejectionReason) {
                  $ctrl.claims[i].rejectionReason = payload.rejectionReason;
                }
                if (payload.coveredSparePartCode) {
                  $ctrl.claims[i].coveredSparePartCode = payload.coveredSparePartCode;
                }
                if (payload.coveredSparePartName) {
                  $ctrl.claims[i].coveredSparePartName = payload.coveredSparePartName;
                }
                $ctrl.claims[i].isLiveArrival = true;
                matched = true;
              }
            }
            $ctrl.applyTabFilter();

            if (matched && targetId) {
              DealerApiService.getWarrantyClaimById(targetId)
                .then(function (hydrated) {
                  if (isDestroyed) { return; }
                  for (var j = 0; j < $ctrl.claims.length; j++) {
                    if ($ctrl.claims[j].id === hydrated.id || $ctrl.claims[j].claimNumber === hydrated.claimNumber) {
                      hydrated.isLiveArrival = true;
                      $ctrl.claims[j] = hydrated;
                    }
                  }
                  $ctrl.applyTabFilter();
                })
                .catch(angular.noop);
            } else if (!matched) {
              $ctrl.loadWarrantyClaims();
            }
          });
        }));
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadWarrantyClaims();
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

    $ctrl.claimStatusIcon = function (status) {
      if (status === 'Completed' || status === 'Approved') { return 'shield-check'; }
      if (status === 'Rejected') { return 'x-circle'; }
      if (status === 'UnderReview') { return 'magnifying-glass'; }
      return 'clock';
    };

    $ctrl.getVehicleImageUrl = function (vehicleId, vin, directUrl) {
      if (angular.isString(directUrl) && directUrl.trim()) {
        return directUrl.trim();
      }
      var list = $ctrl.vehicles || [];
      var targetVin = vin ? String(vin).toUpperCase() : '';
      for (var i = 0; i < list.length; i++) {
        if ((vehicleId && list[i].id === vehicleId) || (targetVin && list[i].vin === targetVin)) {
          if (list[i].imageUrl) {
            return list[i].imageUrl;
          }
        }
      }
      return '';
    };

    $ctrl.loadInitialData = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        vehicles: DealerApiService.listVehicles().catch(function () { return null; }),
        dealers: DealerApiService.listDealers().catch(function () { return null; }),
        claims: DealerApiService.getMyWarrantyClaims({ pageSize: 50 })
      }).then(function (bundle) {
        if (isDestroyed) { return bundle.claims; }
        if (angular.isArray(bundle.vehicles)) {
          $ctrl.vehicles = bundle.vehicles;
        }
        if (angular.isArray(bundle.dealers) && bundle.dealers.length > 0) {
          $ctrl.dealers = bundle.dealers.filter(function (d) { return d.isActive; });
        }
        applyDefaultSelections(true);
        var chosen = findSelectedVehicle($ctrl.draft.customerVehicleId);
        if (chosen && ($ctrl.draft.odometerAtClaimKm === null || $ctrl.draft.odometerAtClaimKm < chosen.currentMileageKm)) {
          $ctrl.draft.odometerAtClaimKm = chosen.currentMileageKm;
          refreshSelectedVehicleAndEligibility(false);
        }
        $ctrl.claims = bundle.claims || [];
        $ctrl.applyTabFilter();
        return $ctrl.claims;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return $ctrl.claims;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.loadWarrantyClaims = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.getMyWarrantyClaims({ pageSize: 50 })
        .then(function (rows) {
          if (isDestroyed) { return rows; }
          $ctrl.claims = rows || [];
          $ctrl.applyTabFilter();
          return $ctrl.claims;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.claims;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.onVehicleSelected = function () {
      refreshSelectedVehicleAndEligibility(true);
      $ctrl.onFieldEdited('customerVehicleId');
      $ctrl.onFieldEdited('odometerAtClaimKm');
    };

    $ctrl.onOdometerEdited = function () {
      refreshSelectedVehicleAndEligibility(false);
      $ctrl.onFieldEdited('odometerAtClaimKm');
    };

    $ctrl.onFieldEdited = function (fieldName) {
      $ctrl.submitProblem = null;
      if (ProblemDetailsMapper) {
        ProblemDetailsMapper.clearFieldError($ctrl.warrantyForm, $ctrl.fieldErrors, fieldName);
      }
    };

    $ctrl.canSubmitWarrantyClaim = function () {
      if (!$ctrl.draft.customerVehicleId || !$ctrl.draft.dealerId || !$ctrl.draft.defectCategory) {
        return false;
      }
      var desc = String($ctrl.draft.symptomDescription || '').trim();
      if (desc.length < 10 || desc.length > 1000) {
        return false;
      }
      if ($ctrl.draft.odometerAtClaimKm === null || $ctrl.draft.odometerAtClaimKm === undefined || isNaN(Number($ctrl.draft.odometerAtClaimKm))) {
        return false;
      }
      if ($ctrl.selectedEligibility && !$ctrl.selectedEligibility.isCovered) {
        return false;
      }
      return true;
    };

    $ctrl.submitWarrantyClaim = function () {
      if (ProblemDetailsMapper) {
        ProblemDetailsMapper.clearServerErrors($ctrl.warrantyForm);
      }
      if (!$ctrl.canSubmitWarrantyClaim() || $ctrl.isSubmitting) {
        return $q.resolve(null);
      }

      $ctrl.isSubmitting = true;
      $ctrl.submitProblem = null;
      $ctrl.fieldErrors = {};

      return DealerApiService.createWarrantyClaim({
        customerVehicleId: $ctrl.draft.customerVehicleId,
        dealerId: $ctrl.draft.dealerId,
        odometerAtClaimKm: $ctrl.draft.odometerAtClaimKm,
        defectCategory: $ctrl.draft.defectCategory,
        symptomDescription: $ctrl.draft.symptomDescription
      }).then(function (created) {
        if (isDestroyed) { return created; }
        $ctrl.claims.unshift(created);
        $ctrl.createdBanner = created;
        $ctrl.draft.symptomDescription = '';
        if ($ctrl.selectedVehicle && created.odometerAtClaimKm > $ctrl.selectedVehicle.currentMileageKm) {
          $ctrl.selectedVehicle.currentMileageKm = created.odometerAtClaimKm;
          refreshSelectedVehicleAndEligibility(false);
        }
        $ctrl.applyTabFilter();
        ToastService.success(
          t('customerWarranty.createdToastTitle', null, 'OEM Warranty Claim Submitted'),
          t(
            'customerWarranty.createdToastBody',
            { number: created.claimNumber, vin: created.vin, category: enumT('WarrantyDefectCategory', created.defectCategory) },
            created.claimNumber + ' filed for VIN ' + created.vin + ' (' + created.defectCategory + ').'
          )
        );
        return created;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.warrantyForm, problem);
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
      var all = $ctrl.claims || [];
      for (var i = 0; i < all.length; i++) {
        if (matchesTab(all[i], tabKey)) {
          count++;
        }
      }
      return count;
    };

    $ctrl.applyTabFilter = function () {
      $ctrl.filteredClaims = ($ctrl.claims || []).filter(function (clm) {
        return matchesTab(clm, $ctrl.activeTab);
      });
    };

    $ctrl.isStageDone = function (currentStatus, stageCode) {
      var curIdx = WARRANTY_STAGE_INDEX[currentStatus];
      var stageIdx = WARRANTY_STAGE_INDEX[stageCode];
      if (curIdx === undefined || stageIdx === undefined) {
        return false;
      }
      return curIdx > stageIdx || currentStatus === 'Completed';
    };
  }
})();
