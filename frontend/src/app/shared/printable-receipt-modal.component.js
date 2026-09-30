(function () {
  'use strict';

  var RECEIPT_I18N_VI = {
    receipt: {
      officialTitle: 'APEX MOTO LOYALTY — BIÊN LAI THUẾ & ĐIỂM THƯỞNG CHÍNH HÃNG',
      kindSalesOrder: 'HÓA ĐƠN BÁN XE SHOWROOM',
      kindServiceInvoice: 'HÓA ĐƠN DỊCH VỤ XƯỞNG',
      receiptNoLabel: 'Số biên lai',
      refLabel: 'Mã chứng từ',
      issuedLabel: 'Ngày lập',
      hotlineLabel: 'Hotline',
      sealTooltip: 'Mã xác thực biên lai SHA-256 bất biến',
      customerSection: 'Thông tin Khách hàng & Thanh toán',
      vehicleSection: 'Thông tin Xe VIN & Mốc Bảo dưỡng Kế tiếp',
      telLabel: 'SĐT',
      paymentLabel: 'Thanh toán',
      voucherLabel: 'Voucher',
      advisorLabel: 'Cố vấn',
      engineLabel: 'Số máy',
      plateLabel: 'Biển số',
      pendingRegistration: 'Chờ đăng ký biển số',
      odometerLabel: 'Số ODO',
      nextOemDueLabel: 'Mốc bảo dưỡng OEM kế tiếp',
      orLabel: 'hoặc',
      colSku: 'Mã SKU',
      colDesc: 'Hạng mục chi tiết',
      colType: 'Phân loại',
      colQty: 'SL',
      colUnitPrice: 'Đơn giá',
      colLineTotal: 'Thành tiền',
      loyaltyStatementTitle: 'Sao kê Điểm thưởng Hội viên (ADR-003)',
      pointsEarnedLabel: 'Điểm cộng trên biên lai',
      tierUpgradedTag: 'Đã thăng hạng!',
      walletAfterLabel: 'Số dư sau giao dịch',
      availShort: 'khả dụng',
      qualifyingShort: 'xét hạng',
      remarksLabel: 'Ghi chú Cố vấn / Kỹ thuật viên',
      laborSubtotal: 'Tổng tiền công xưởng',
      partsSubtotal: 'Tổng phụ tùng chính hãng OEM',
      grossAmount: 'Tổng trước giảm giá',
      netPaidTitle: 'THỰC THANH TOÁN (VNĐ)',
      footerImmutable: 'Chứng từ sổ cái tài chính bất biến',
      footerPlatform: 'Nền tảng Quản lý Đại lý Xe máy & Khách hàng Thân thiết v1.4',
      verifyLabel: 'Xác thực'
    }
  };

  var RECEIPT_I18N_EN = {
    receipt: {
      officialTitle: 'APEX MOTO LOYALTY — OFFICIAL DEALERSHIP TAX & LOYALTY RECEIPT',
      kindSalesOrder: 'SHOWROOM VEHICLE SALE',
      kindServiceInvoice: 'WORKSHOP SERVICE INVOICE',
      receiptNoLabel: 'Receipt #',
      refLabel: 'Ref',
      issuedLabel: 'Issued',
      hotlineLabel: 'Hotline',
      sealTooltip: 'Deterministic SHA-256 Receipt Verification Seal',
      customerSection: 'Customer & Settlement Info',
      vehicleSection: 'Motorcycle VIN & Next Maintenance Due',
      telLabel: 'Tel',
      paymentLabel: 'Payment',
      voucherLabel: 'Voucher',
      advisorLabel: 'Advisor',
      engineLabel: 'Engine',
      plateLabel: 'Plate',
      pendingRegistration: 'Pending Registration',
      odometerLabel: 'Odometer',
      nextOemDueLabel: 'Next OEM Service Due',
      orLabel: 'or',
      colSku: 'Code / SKU',
      colDesc: 'Itemized Description',
      colType: 'Type',
      colQty: 'Qty',
      colUnitPrice: 'Unit Price',
      colLineTotal: 'Line Total',
      loyaltyStatementTitle: 'Loyalty Ledger Statement (ADR-003)',
      pointsEarnedLabel: 'Points Earned on Receipt',
      tierUpgradedTag: 'Tier Upgraded!',
      walletAfterLabel: 'Wallet After',
      availShort: 'Avail pts',
      qualifyingShort: 'Qualifying pts',
      remarksLabel: 'Advisor / Technician Remarks',
      laborSubtotal: 'Workshop Labor Subtotal',
      partsSubtotal: 'Genuine OEM Parts Subtotal',
      grossAmount: 'Gross Amount',
      netPaidTitle: 'NET PAID (VND)',
      footerImmutable: 'Immutable Financial Ledger Record',
      footerPlatform: 'Motorcycle Dealer & Loyalty Management Platform v1.4',
      verifyLabel: 'Verify'
    }
  };

  angular
    .module('app.shared')
    .run(registerReceiptI18n)
    .component('printableReceiptModal', {
      bindings: {
        receipt: '<',
        receiptKind: '<',
        onClose: '&'
      },
      controllerAs: '$ctrl',
      controller: PrintableReceiptModalController,
      template:
        '<div class="modal-backdrop printable-receipt-overlay" role="dialog" aria-modal="true" ' +
             'aria-labelledby="officialReceiptTitle" ng-if="$ctrl.viewModel" ng-click="$ctrl.closeModal()">' +
          '<div class="modal-dialog modal-dialog-wide printable-receipt-dialog modal-spring-in motion-modal" ng-click="$event.stopPropagation()">' +
            '<div class="printable-receipt-sheet">' +
              /* 1. Official Dealership Header & Verification Hash */
              '<header class="receipt-brand-header">' +
                '<div>' +
                  '<div style="display:flex;align-items:center;gap:8px;">' +
                    '<span class="brand-accent-bar" aria-hidden="true"></span>' +
                    '<strong id="officialReceiptTitle" class="text-primary" style="font-family:var(--font-display);font-size:17px;letter-spacing:-0.01em;">' +
                      '{{ \'receipt.officialTitle\' | t }}' +
                    '</strong>' +
                  '</div>' +
                  '<div class="text-primary" style="font-weight:700;font-size:14px;margin-top:8px;">' +
                    '{{ $ctrl.viewModel.dealer.name }} (<span class="mono-num">{{ $ctrl.viewModel.dealer.code }}</span>)' +
                  '</div>' +
                  '<div class="text-muted" style="font-size:12px;margin-top:2px;">' +
                    '{{ $ctrl.viewModel.dealer.address }}, {{ $ctrl.viewModel.dealer.city }}' +
                    '<span ng-if="$ctrl.viewModel.dealer.phone"> • {{ \'receipt.hotlineLabel\' | t }}: <span class="mono-num">{{ $ctrl.viewModel.dealer.phone }}</span></span>' +
                  '</div>' +
                '</div>' +

                '<div style="text-align:right;">' +
                  '<span class="badge" ng-class="$ctrl.viewModel.receiptKind === \'SalesOrder\' ? \'badge-tier-platinum\' : \'badge-info\'">' +
                    '{{ $ctrl.viewModel.receiptKind === \'SalesOrder\' ? (\'receipt.kindSalesOrder\' | t) : (\'receipt.kindServiceInvoice\' | t) }}' +
                  '</span>' +
                  '<div class="mono-num text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">' +
                    '{{ \'receipt.receiptNoLabel\' | t }}: {{ $ctrl.viewModel.receiptNumber }}' +
                  '</div>' +
                  '<div class="mono-num text-muted" style="font-size:12px;margin-top:2px;">' +
                    '{{ \'receipt.refLabel\' | t }}: {{ $ctrl.viewModel.documentNumber }} • {{ \'receipt.issuedLabel\' | t }}: {{ $ctrl.viewModel.issuedAtUtc | utcToLocal }}' +
                  '</div>' +
                  '<div style="margin-top:6px;">' +
                    '<span class="receipt-verification-pill" ng-attr-title="{{ \'receipt.sealTooltip\' | t }}">' +
                      '<app-icon name="seal-check" weight="fill" size="14"></app-icon>' +
                      '<span>SHA-256: {{ $ctrl.viewModel.verificationCode }}</span>' +
                    '</span>' +
                  '</div>' +
                '</div>' +
              '</header>' +

              /* 2. Customer & Vehicle / OEM Maintenance Due Grid */
              '<div class="receipt-meta-grid">' +
                '<div class="receipt-meta-box">' +
                  '<div class="text-muted" style="font-weight:700;text-transform:uppercase;letter-spacing:0.05em;margin-bottom:6px;">' +
                    '{{ \'receipt.customerSection\' | t }}' +
                  '</div>' +
                  '<div class="text-primary" style="font-weight:700;font-size:14px;">{{ $ctrl.viewModel.customer.fullName }}</div>' +
                  '<div class="mono-num text-secondary" ng-if="$ctrl.viewModel.customer.email" style="margin-top:2px;">{{ $ctrl.viewModel.customer.email }}</div>' +
                  '<div class="mono-num text-secondary" ng-if="$ctrl.viewModel.customer.phone" style="margin-top:2px;">{{ \'receipt.telLabel\' | t }}: {{ $ctrl.viewModel.customer.phone }}</div>' +
                  '<div style="margin-top:6px;padding-top:6px;border-top:1px solid var(--clr-border-subtle);display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap;">' +
                    '<span>{{ \'receipt.paymentLabel\' | t }}: <strong>{{ $ctrl.viewModel.paymentMethod | enumT:\'PaymentMethod\' }}</strong></span>' +
                    '<span ng-if="$ctrl.viewModel.appliedVoucherCode">{{ \'receipt.voucherLabel\' | t }}: <strong class="mono-num">{{ $ctrl.viewModel.appliedVoucherCode }}</strong></span>' +
                    '<span ng-if="$ctrl.viewModel.issuedByStaffName">{{ \'receipt.advisorLabel\' | t }}: <strong>{{ $ctrl.viewModel.issuedByStaffName }}</strong></span>' +
                  '</div>' +
                '</div>' +

                '<div class="receipt-meta-box">' +
                  '<div class="text-muted" style="font-weight:700;text-transform:uppercase;letter-spacing:0.05em;margin-bottom:6px;">' +
                    '{{ \'receipt.vehicleSection\' | t }}' +
                  '</div>' +
                  '<div class="text-primary" style="font-weight:700;font-size:14px;">' +
                    '{{ $ctrl.viewModel.vehicle.modelName }}' +
                    '<span ng-if="$ctrl.viewModel.vehicle.colorName" class="text-muted" style="font-weight:500;"> ({{ $ctrl.viewModel.vehicle.colorName }})</span>' +
                  '</div>' +
                  '<div class="mono-num text-secondary" style="margin-top:2px;">' +
                    'VIN: <strong class="text-primary">{{ $ctrl.viewModel.vehicle.vin || \'N/A\' }}</strong>' +
                    '<span ng-if="$ctrl.viewModel.vehicle.engineNumber"> • {{ \'receipt.engineLabel\' | t }}: {{ $ctrl.viewModel.vehicle.engineNumber }}</span>' +
                  '</div>' +
                  '<div class="mono-num text-secondary" style="margin-top:2px;">' +
                    '{{ \'receipt.plateLabel\' | t }}: {{ $ctrl.viewModel.vehicle.licensePlate || (\'receipt.pendingRegistration\' | t) }}' +
                    '<span ng-if="$ctrl.viewModel.vehicle.odometerAtCompletionKm !== null && $ctrl.viewModel.vehicle.odometerAtCompletionKm !== undefined">' +
                      ' • {{ \'receipt.odometerLabel\' | t }}: {{ $ctrl.viewModel.vehicle.odometerAtCompletionKm | number }} km' +
                    '</span>' +
                  '</div>' +
                  '<div style="margin-top:6px;padding-top:6px;border-top:1px solid var(--clr-border-subtle);color:var(--clr-info-fg);font-weight:600;" ' +
                       'ng-if="$ctrl.viewModel.vehicle.nextDueMileageKm || $ctrl.viewModel.vehicle.nextDueDateUtc">' +
                    '{{ \'receipt.nextOemDueLabel\' | t }}: ' +
                    '<span class="mono-num" ng-if="$ctrl.viewModel.vehicle.nextDueMileageKm">{{ $ctrl.viewModel.vehicle.nextDueMileageKm | number }} km</span>' +
                    '<span ng-if="$ctrl.viewModel.vehicle.nextDueMileageKm && $ctrl.viewModel.vehicle.nextDueDateUtc"> {{ \'receipt.orLabel\' | t }} </span>' +
                    '<span class="mono-num" ng-if="$ctrl.viewModel.vehicle.nextDueDateUtc">{{ $ctrl.viewModel.vehicle.nextDueDateUtc | utcToLocal:\'date\' }}</span>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              /* 3. Itemized LineItems Table */
              '<div class="table-wrap" style="margin-bottom:16px;border:1px solid var(--clr-border-subtle);border-radius:var(--radius-md);">' +
                '<table class="data-table">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="width:44px;">#</th>' +
                      '<th>{{ \'receipt.colSku\' | t }}</th>' +
                      '<th>{{ \'receipt.colDesc\' | t }}</th>' +
                      '<th>{{ \'receipt.colType\' | t }}</th>' +
                      '<th style="text-align:right;">{{ \'receipt.colQty\' | t }}</th>' +
                      '<th style="text-align:right;">{{ \'receipt.colUnitPrice\' | t }}</th>' +
                      '<th style="text-align:right;">{{ \'receipt.colLineTotal\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="line in $ctrl.viewModel.lineItems track by (line.id || line.lineNumber)">' +
                      '<td class="mono-num">{{ line.lineNumber }}</td>' +
                      '<td class="mono-num" style="font-weight:700;">{{ line.partCodeSnapshot || \'—\' }}</td>' +
                      '<td>{{ line.description }}</td>' +
                      '<td><span class="badge badge-neutral">{{ line.itemType | enumT:\'InvoiceItemType\' }}</span></td>' +
                      '<td class="mono-num" style="text-align:right;">{{ line.quantity | number }}</td>' +
                      '<td class="mono-num" style="text-align:right;">{{ line.unitPriceVnd | vndCurrency }}</td>' +
                      '<td class="mono-num" style="text-align:right;font-weight:700;">{{ line.lineTotalVnd | vndCurrency }}</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +

              /* 4. Loyalty Accrual & Financial Totals Row */
              '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:20px;flex-wrap:wrap;">' +
                '<div style="flex:1;min-width:260px;">' +
                  '<div style="padding:12px 14px;border-radius:var(--radius-md);border:1px solid var(--clr-success-border);background:var(--clr-success-bg);color:var(--clr-success-fg);font-size:13px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<strong>{{ \'receipt.loyaltyStatementTitle\' | t }}</strong>' +
                      '<span class="badge" ng-class="\'badge-tier-\' + ($ctrl.viewModel.loyalty.tierCodeAfter || \'Bronze\').toLowerCase()">' +
                        '<app-icon name="medal" weight="fill" size="13"></app-icon>' +
                        '<span>{{ $ctrl.viewModel.loyalty.tierCodeAfter | enumT:\'LoyaltyTierCode\' }} ({{ $ctrl.viewModel.loyalty.pointsMultiplierApplied | number:2 }}x)</span>' +
                      '</span>' +
                    '</div>' +
                    '<div style="margin-top:6px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                      '<span>{{ \'receipt.pointsEarnedLabel\' | t }}:</span>' +
                      '<strong class="mono-num" style="font-size:15px;">+{{ $ctrl.viewModel.loyalty.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                      '<span ng-if="$ctrl.viewModel.loyalty.tierUpgraded" style="display:inline-flex;align-items:center;gap:4px;font-weight:700;">' +
                        '<app-icon name="sparkle" weight="fill" size="13"></app-icon>' +
                        '<span>({{ \'receipt.tierUpgradedTag\' | t }})</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ' +
                         'ng-if="$ctrl.viewModel.loyalty.availablePointsAfter !== null && $ctrl.viewModel.loyalty.availablePointsAfter !== undefined">' +
                      '{{ \'receipt.walletAfterLabel\' | t }}: {{ $ctrl.viewModel.loyalty.availablePointsAfter | number }} {{ \'receipt.availShort\' | t }}' +
                      '<span ng-if="$ctrl.viewModel.loyalty.lifetimeQualifyingPointsAfter !== null && $ctrl.viewModel.loyalty.lifetimeQualifyingPointsAfter !== undefined">' +
                        ' • {{ $ctrl.viewModel.loyalty.lifetimeQualifyingPointsAfter | number }} {{ \'receipt.qualifyingShort\' | t }}' +
                      '</span>' +
                    '</div>' +
                  '</div>' +

                  '<div ng-if="$ctrl.viewModel.notes" class="text-secondary" style="margin-top:10px;font-size:12px;">' +
                    '<strong>{{ \'receipt.remarksLabel\' | t }}:</strong> {{ $ctrl.viewModel.notes }}' +
                  '</div>' +
                '</div>' +

                '<div class="receipt-totals-box">' +
                  '<div style="display:flex;justify-content:space-between;" ng-if="$ctrl.viewModel.receiptKind === \'ServiceInvoice\'">' +
                    '<span class="text-muted">{{ \'receipt.laborSubtotal\' | t }}:</span>' +
                    '<span class="mono-num text-primary">{{ $ctrl.viewModel.laborAmount | vndCurrency }}</span>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;" ng-if="$ctrl.viewModel.receiptKind === \'ServiceInvoice\'">' +
                    '<span class="text-muted">{{ \'receipt.partsSubtotal\' | t }}:</span>' +
                    '<span class="mono-num text-primary">{{ $ctrl.viewModel.partsAmount | vndCurrency }}</span>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;padding-top:6px;border-top:1px solid var(--clr-border-subtle);">' +
                    '<span class="text-primary">{{ \'receipt.grossAmount\' | t }}:</span>' +
                    '<strong class="mono-num text-primary">{{ $ctrl.viewModel.grossAmount | vndCurrency }}</strong>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;color:var(--clr-danger-fg);">' +
                    '<span>{{ \'common.discount\' | t }}{{ $ctrl.viewModel.appliedVoucherCode ? (\' (\' + $ctrl.viewModel.appliedVoucherCode + \')\') : \'\' }}:</span>' +
                    '<strong class="mono-num">-{{ $ctrl.viewModel.discountAmount | vndCurrency }}</strong>' +
                  '</div>' +
                  '<div class="text-primary" style="display:flex;justify-content:space-between;padding-top:8px;border-top:2px solid var(--clr-text-primary);font-size:17px;">' +
                    '<strong>{{ \'receipt.netPaidTitle\' | t }}:</strong>' +
                    '<strong class="mono-num">{{ $ctrl.viewModel.netPaidAmount | vndCurrency }}</strong>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<footer class="text-muted" style="margin-top:18px;padding-top:10px;border-top:1px dashed var(--clr-border-strong);display:flex;justify-content:space-between;font-size:11px;flex-wrap:wrap;gap:8px;">' +
                '<span>{{ \'receipt.footerImmutable\' | t }} • {{ \'receipt.footerPlatform\' | t }}</span>' +
                '<span class="mono-num">{{ \'receipt.verifyLabel\' | t }}: {{ $ctrl.viewModel.receiptNumber }} / {{ $ctrl.viewModel.verificationCode }}</span>' +
              '</footer>' +
            '</div>' +

            '<div class="modal-footer receipt-modal-actions no-print">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeModal()">' +
                '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'common.close\' | t }}</span>' +
              '</button>' +
              '<button type="button" id="printOfficialReceiptBtn" class="btn btn-primary" ng-click="$ctrl.printReceipt()">' +
                '<app-icon name="printer" weight="duotone" size="16"></app-icon>' +
                '<span>{{ \'common.print\' | t }}</span>' +
              '</button>' +
            '</div>' +
          '</div>' +
        '</div>'
    });

  registerReceiptI18n.$inject = ['I18nService'];
  function registerReceiptI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(RECEIPT_I18N_VI, RECEIPT_I18N_EN);
    }
  }

  PrintableReceiptModalController.$inject = ['$window', 'DealerApiService', 'I18nService'];
  function PrintableReceiptModalController($window, DealerApiService, I18nService) {
    var $ctrl = this;

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(RECEIPT_I18N_VI, RECEIPT_I18N_EN);
    }

    function toggleBodyPrintClass(isActive) {
      if ($window && $window.document && $window.document.body && $window.document.body.classList) {
        if (isActive) {
          $window.document.body.classList.add('is-printing-receipt-open');
        } else {
          $window.document.body.classList.remove('is-printing-receipt-open');
        }
      }
    }

    function syncReceiptViewModel() {
      if (!$ctrl.receipt || !angular.isObject($ctrl.receipt)) {
        $ctrl.viewModel = null;
        toggleBodyPrintClass(false);
        return;
      }
      var kind = $ctrl.receiptKind || $ctrl.receipt.receiptKind || null;
      $ctrl.viewModel = (DealerApiService && angular.isFunction(DealerApiService.normalizePrintableReceipt))
        ? DealerApiService.normalizePrintableReceipt($ctrl.receipt, kind)
        : angular.extend({}, $ctrl.receipt);
      toggleBodyPrintClass(true);
    }

    $ctrl.$onInit = function () {
      $ctrl.viewModel = null;
      syncReceiptViewModel();
    };

    $ctrl.$onChanges = function () {
      syncReceiptViewModel();
    };

    $ctrl.$onDestroy = function () {
      toggleBodyPrintClass(false);
    };

    $ctrl.printReceipt = function () {
      if ($window && angular.isFunction($window.print)) {
        $window.print();
      }
    };

    $ctrl.closeModal = function () {
      toggleBodyPrintClass(false);
      if (angular.isFunction($ctrl.onClose)) {
        $ctrl.onClose();
      }
    };
  }
})();
