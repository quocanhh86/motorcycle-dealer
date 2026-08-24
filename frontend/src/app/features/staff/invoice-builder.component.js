(function () {
  'use strict';

  var VI_PATCH = {
    enums: {
      PaymentMethod: {
        EWallet: 'Ví điện tử / QR'
      }
    },
    staff: {
      inv: {
        eyebrow: 'SCR-STF-02 • HÓA ĐƠN XƯỞNG & SỔ ĐIỂM',
        refreshQueues: 'Tải lại hàng đợi',
        refreshing: 'Đang tải...',
        backToWorkbench: 'Về bảng điều phối',
        settledHeading: 'Đã chốt hóa đơn xưởng:',
        tierUpgradedPill: 'THĂNG HẠNG {{tier}}',
        bookingPrefix: 'Lịch hẹn',
        grossLabel: 'Tổng trước giảm',
        loyaltyCredited: 'Điểm thưởng đã cộng',
        activeTierNow: 'Hạng hiện tại',
        exitOdoLabel: 'ODO xuất xưởng',
        printOfficialReceipt: 'In biên lai chính thức',
        dismiss: 'Ẩn',
        tabBuilder: '1. Chốt & Xuất hóa đơn ({{count}} sẵn sàng)',
        tabIssued: '2. Hóa đơn xưởng đã xuất ({{count}})',
        kpiReady: 'Lịch sẵn sàng quyết toán',
        kpiReadyMeta: 'Đã xác nhận • Tiếp nhận • Đang sửa',
        kpiIssued: 'Hóa đơn xưởng đã xuất',
        kpiIssuedMeta: 'Lưu trữ bất biến kèm biên lai A4',
        kpiRevenue: 'Doanh thu xưởng đã thu',
        kpiRevenueMeta: 'Tổng thực thu sau khấu trừ voucher',
        card1Title: '1. Lịch hẹn xưởng, ODO & Chi phí',
        selectBookingLabel: 'Chọn lịch hẹn sẵn sàng quyết toán (Đã xác nhận / Đã tiếp nhận / Đang sửa) *',
        selectBookingPlaceholder: '-- Chọn lịch hẹn xưởng để lập hóa đơn --',
        noSettleableHint: 'Hiện chưa có lịch hẹn nào ở trạng thái Đã xác nhận, Đã tiếp nhận hoặc Đang sửa chữa tại chi nhánh.',
        bookingOdo: 'ODO tiếp nhận',
        availablePts: 'Điểm khả dụng',
        lifetimeQualifying: 'Điểm xét hạng tích lũy',
        reservedVoucherPrefix: 'Đã giữ',
        laborLabel: 'Tiền công kỹ thuật (VNĐ) *',
        partsLabel: 'Phụ tùng & Dầu nhớt chính hãng (VNĐ) *',
        autoSumBadge: 'Tự cộng ({{count}})',
        partsAutoHint: 'Tự động tính tổng từ danh sách phụ tùng OEM bên dưới.',
        oemPickerTitle: 'Chọn chi tiết Phụ tùng Chính hãng OEM',
        oemPickerCountHint: '{{count}} dòng OEM • Tạm tính: {{subtotal}}',
        oemPickerOptionalHint: 'Không bắt buộc — chọn phụ tùng OEM hoặc nhập tổng tiền phụ tùng ở trên',
        oemPartLabel: 'Phụ tùng OEM tương thích & Dùng chung',
        oemPartPlaceholder: '-- Chọn phụ tùng OEM / Vật tư tiêu hao --',
        addOemPartCta: 'Thêm phụ tùng OEM',
        colPartCode: 'Mã SKU',
        colDescription: 'Mô tả phụ tùng',
        removeLineAria: 'Xóa dòng phụ tùng OEM',
        exitOdometerLabel: 'Số ODO lúc hoàn tất (km) * (Tối thiểu: {{min}} km)',
        odometerRegressionError: 'Số ODO hoàn tất không được nhỏ hơn {{min}} km.',
        voucherLabel: 'Voucher dịch vụ của khách hàng (Đang giữ hoặc Khả dụng)',
        noVoucherOption: '-- Không áp dụng Voucher (Giải phóng voucher đã giữ nếu có) --',
        voucherIneligibleSuffix: 'Tăng giá trị đơn hoặc bỏ chọn voucher.',
        activeTierMultiplierLabel: 'Hệ số hạng thành viên',
        workNotesLabel: 'Tóm tắt kỹ thuật & Hạng mục đã hoàn tất',
        workNotesPlaceholder: 'Đã hoàn tất thay dầu nhớt tổng hợp, kiểm tra bộ truyền động và an toàn phanh...',
        settlingCta: 'Đang chốt hóa đơn & Cộng điểm...',
        finalizeCta: 'Chốt & Xuất Hóa đơn Xưởng',
        card2Title: '2. Dự toán Thanh toán & Tích điểm Trực tiếp',
        oracleTitle: 'Công thức Tích điểm 2 Bước (Làm tròn xuống)',
        step1Label: 'Bước 1 — Điểm gốc (Floor(Net / 10.000))',
        step2Label: 'Bước 2 — Nhân hệ số hạng ({{mult}}x)',
        qualifyingAfterLabel: 'Điểm xét hạng sau thanh toán',
        pointsOnSettlement: 'Điểm cộng khi chốt hóa đơn',
        projectedTierLabel: 'Hạng sau đơn',
        autoPromotionTitle: 'DỰ KIẾN THĂNG HẠNG TỰ ĐỘNG',
        autoPromotionDesc: 'Chốt hóa đơn này cộng +{{pts}} điểm và nâng hạng khách hàng từ {{fromTier}} lên {{toTier}} ({{mult}}x).',
        colInvoiceNo: 'Số hóa đơn',
        colBookingVin: 'Lịch hẹn & VIN',
        colGrossDiscount: 'Tổng / Giảm giá',
        colLoyaltyAccrued: 'Điểm thưởng',
        colPaidAt: 'Thời điểm thanh toán',
        emptyIssuedTitle: 'Chưa có hóa đơn xưởng nào được xuất',
        emptyIssuedDesc: 'Mọi hóa đơn dịch vụ chốt tại chi nhánh sẽ lưu bất biến tại đây cùng biên lai in A4.',
        voucherReservedTag: '[Đã giữ] ',
        minOrderTag: ' • Tối thiểu {{amount}}',
        universalFitTag: '[Dùng chung]',
        unitFallback: 'Đơn vị',
        toastReceiptUnavailableTitle: 'Không thể tải biên lai',
        toastReceiptUnavailableMsg: 'Không tìm thấy mã hóa đơn để truy xuất biên lai chính thức.',
        toastSettledTitle: 'Đã chốt hóa đơn xưởng',
        toastSettledMsg: '{{invoiceNumber}} đã ghi nhận (+{{pts}} điểm{{upgrade}}).',
        toastUpgradedSuffix: ' — Thăng hạng {{tier}}',
        defaultDealerName: 'Đại lý ủy quyền',
        completedNotePrefix: 'Hoàn tất: '
      }
    }
  };

  var EN_PATCH = {
    enums: {
      PaymentMethod: {
        EWallet: 'E-Wallet / QR'
      }
    },
    staff: {
      inv: {
        eyebrow: 'SCR-STF-02 • WORKSHOP INVOICE & LOYALTY',
        refreshQueues: 'Refresh Queues',
        refreshing: 'Refreshing...',
        backToWorkbench: 'Back to Live Workbench',
        settledHeading: 'Workshop Invoice Settled:',
        tierUpgradedPill: 'TIER UPGRADED {{tier}}',
        bookingPrefix: 'Booking',
        grossLabel: 'Gross',
        loyaltyCredited: 'Loyalty Credited',
        activeTierNow: 'Active Tier Now',
        exitOdoLabel: 'Exit Odometer',
        printOfficialReceipt: 'Print Official Receipt',
        dismiss: 'Dismiss',
        tabBuilder: '1. Settle & Issue Invoice ({{count}} Ready)',
        tabIssued: '2. Issued Service Invoices ({{count}})',
        kpiReady: 'Ready to Settle',
        kpiReadyMeta: 'Confirmed • Checked In • In Progress',
        kpiIssued: 'Issued Workshop Invoices',
        kpiIssuedMeta: 'Immutable financial ledger with A4 receipt',
        kpiRevenue: 'Settled Workshop Revenue',
        kpiRevenueMeta: 'Net paid after voucher discounts',
        card1Title: '1. Workshop Booking, Odometer & Charges',
        selectBookingLabel: 'Select Settleable Workshop Booking (Confirmed / CheckedIn / InProgress) *',
        selectBookingPlaceholder: '-- Select Workshop Appointment to Settle --',
        noSettleableHint: 'No bookings are currently in Confirmed, CheckedIn, or InProgress status at this dealership.',
        bookingOdo: 'Booking Odo',
        availablePts: 'Available Pts',
        lifetimeQualifying: 'Lifetime Qualifying',
        reservedVoucherPrefix: 'Reserved',
        laborLabel: 'Labor Charges (VND) *',
        partsLabel: 'Genuine Parts & Fluids (VND) *',
        autoSumBadge: 'Auto-Sum ({{count}})',
        partsAutoHint: 'Auto-calculated from OEM Spare-Parts line items below.',
        oemPickerTitle: 'OEM Spare-Parts Line-Item Picker',
        oemPickerCountHint: '{{count}} OEM line(s) • Subtotal: {{subtotal}}',
        oemPickerOptionalHint: 'Optional — pick OEM parts or enter manual Parts amount above',
        oemPartLabel: 'Compatible & Universal OEM Part',
        oemPartPlaceholder: '-- Choose OEM Part / Consumable --',
        addOemPartCta: 'Add OEM Part',
        colPartCode: 'Part Code',
        colDescription: 'Description',
        removeLineAria: 'Remove OEM part line',
        exitOdometerLabel: 'Exit Odometer at Completion (km) * (Min: {{min}} km)',
        odometerRegressionError: 'Exit odometer cannot regress below {{min}} km.',
        voucherLabel: 'Customer Service Voucher (Reserved or Available)',
        noVoucherOption: '-- No Voucher Applied (Release Any Reserved Voucher) --',
        voucherIneligibleSuffix: 'Increase charges or unselect voucher.',
        activeTierMultiplierLabel: 'Active Tier Multiplier',
        workNotesLabel: 'Technician Completion Summary Notes',
        workNotesPlaceholder: 'Completed periodic synthetic oil change, CVT inspection, and safety checklist...',
        settlingCta: 'Settling Invoice & Ledger...',
        finalizeCta: 'Finalize & Issue Service Invoice',
        card2Title: '2. Live Settlement & Loyalty Preview',
        oracleTitle: 'Two-Step Floor Loyalty Oracle',
        step1Label: 'Step 1 Base Pts (Floor(Net / 10,000))',
        step2Label: 'Step 2 Tier Rate ({{mult}}x)',
        qualifyingAfterLabel: 'Qualifying Pts After Settlement',
        pointsOnSettlement: 'Points Earned on Settlement',
        projectedTierLabel: 'Projected Tier',
        autoPromotionTitle: 'AUTOMATIC TIER PROMOTION PREVIEW',
        autoPromotionDesc: 'Settling this invoice credits +{{pts}} pts and promotes customer from {{fromTier}} to {{toTier}} ({{mult}}x).',
        colInvoiceNo: 'Invoice #',
        colBookingVin: 'Booking & VIN',
        colGrossDiscount: 'Gross / Discount',
        colLoyaltyAccrued: 'Loyalty Accrued',
        colPaidAt: 'Paid At',
        emptyIssuedTitle: 'No workshop service invoices issued yet',
        emptyIssuedDesc: 'Finalized branch service invoices appear immutably here with printable A4 receipts.',
        voucherReservedTag: '[Reserved] ',
        minOrderTag: ' • Min {{amount}}',
        universalFitTag: '[Universal]',
        unitFallback: 'Unit',
        toastReceiptUnavailableTitle: 'Receipt Unavailable',
        toastReceiptUnavailableMsg: 'Could not resolve an invoice identifier for printable receipt lookup.',
        toastSettledTitle: 'Workshop Invoice Settled',
        toastSettledMsg: '{{invoiceNumber}} committed (+{{pts}} pts{{upgrade}}).',
        toastUpgradedSuffix: ' — Upgraded to {{tier}}',
        defaultDealerName: 'Authorized Dealership',
        completedNotePrefix: 'Completed: '
      }
    }
  };

  registerInvoiceBuilderI18n.$inject = ['I18nService'];
  function registerInvoiceBuilderI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerInvoiceBuilderI18n)
    .component('staffInvoiceBuilder', {
      controllerAs: '$ctrl',
      controller: StaffInvoiceBuilderController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="receipt" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.inv.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.invoiceBuilderTitle\' | t }}</h1>' +
              '<p class="page-subtitle">' +
                '<span class="mono-num">{{ $ctrl.dealerCode }}</span> ({{ $ctrl.dealerName }}) — {{ \'staff.invoiceBuilderSubtitle\' | t }}' +
              '</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadQueues()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrows-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'staff.inv.refreshing\' | t) : (\'staff.inv.refreshQueues\' | t) }}</span>' +
              '</button>' +
              '<a class="btn btn-secondary" ui-sref="app.staff.workbench">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'staff.inv.backToWorkbench\' | t }}</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.loadProblem ? ($ctrl.loadProblem.title + \': \' + $ctrl.loadProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.loadProblem && $ctrl.loadProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadQueues()">' +
          '</error-retry-banner>' +

          /* Issued Invoice Receipt Banner */
          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.issuedReceipt">' +
            '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap;">' +
              '<div>' +
                '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;font-size:15px;font-weight:700;">' +
                  '<app-icon name="check-circle" weight="fill" size="18"></app-icon>' +
                  '<span>{{ \'staff.inv.settledHeading\' | t }}</span>' +
                  '<span class="mono-num">{{ $ctrl.issuedReceipt.invoiceNumber }}</span>' +
                  '<span class="badge badge-tier-gold" ng-if="$ctrl.issuedReceipt.tierUpgraded">' +
                    '<app-icon name="sparkle" weight="fill" size="12"></app-icon>' +
                    '<span>{{ \'staff.inv.tierUpgradedPill\' | t:{ tier: ($ctrl.issuedReceipt.newTierCode | enumT:\'LoyaltyTierCode\') } }}</span>' +
                  '</span>' +
                '</div>' +
                '<div style="margin-top:6px;font-size:13px;">' +
                  '{{ \'staff.inv.bookingPrefix\' | t }} <strong class="mono-num">{{ $ctrl.issuedReceipt.bookingNumber || $ctrl.issuedReceipt.serviceBookingId }}</strong> • ' +
                  '{{ \'common.vin\' | t }} <strong class="mono-num">{{ $ctrl.issuedReceipt.vin }}</strong> • ' +
                  '{{ \'staff.inv.grossLabel\' | t }}: <span class="mono-num">{{ $ctrl.issuedReceipt.grossAmount | vndCurrency }}</span> • ' +
                  '{{ \'common.discount\' | t }}: <span class="mono-num">-{{ $ctrl.issuedReceipt.voucherDiscountAmount | vndCurrency }}</span> • ' +
                  '{{ \'common.netPaid\' | t }}: <strong class="mono-num">{{ $ctrl.issuedReceipt.netPaidAmount | vndCurrency }}</strong>' +
                '</div>' +
                '<div style="margin-top:4px;font-size:13px;">' +
                  '{{ \'staff.inv.loyaltyCredited\' | t }}: <strong class="mono-num">+{{ $ctrl.issuedReceipt.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</strong> ' +
                  '({{ \'staff.inv.activeTierNow\' | t }}: <strong>{{ $ctrl.issuedReceipt.newTierCode | enumT:\'LoyaltyTierCode\' }}</strong> • ' +
                  '{{ \'staff.inv.exitOdoLabel\' | t }}: <span class="mono-num">{{ $ctrl.issuedReceipt.odometerAtCompletionKm | number }} km</span>)' +
                '</div>' +
              '</div>' +
              '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
                '<button type="button" id="printSettledInvoiceBtn" class="btn btn-primary btn-sm" ' +
                        'has-permission="Permissions.Invoices.ViewDealer" ' +
                        'ng-click="$ctrl.openPrintableReceipt($ctrl.issuedReceipt)">' +
                  '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'staff.inv.printOfficialReceipt\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.dismissReceipt()">' +
                  '{{ \'staff.inv.dismiss\' | t }}' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.submitProblem">' +
            '<div style="display:flex;align-items:center;gap:8px;">' +
              '<app-icon name="warning-circle" weight="fill" size="18"></app-icon>' +
              '<div><strong>{{ $ctrl.submitProblem.title }} ({{ $ctrl.submitProblem.errorCode }}):</strong> {{ $ctrl.submitProblem.detail }}</div>' +
            '</div>' +
            '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.submitProblem.traceId">traceId: {{ $ctrl.submitProblem.traceId }}</div>' +
          '</div>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.inv.kpiReady\' | t }}</span>' +
                  '<app-icon name="wrench" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num tabular-nums" kpi-counter="$ctrl.settleableBookings.length">0</div>' +
                '<div class="kpi-meta">{{ \'staff.inv.kpiReadyMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.inv.kpiIssued\' | t }}</span>' +
                  '<app-icon name="check-square" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num tabular-nums" kpi-counter="$ctrl.issuedInvoices.length">0</div>' +
                '<div class="kpi-meta">{{ \'staff.inv.kpiIssuedMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.inv.kpiRevenue\' | t }}</span>' +
                  '<app-icon name="receipt" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num tabular-nums" style="font-size:19px;color:var(--clr-success-fg);" kpi-counter="$ctrl.totalIssuedVnd" kpi-format="vnd">0</div>' +
                '<div class="kpi-meta">{{ \'staff.inv.kpiRevenueMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="tab-strip">' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'BUILDER\'}" ' +
                    'ng-click="$ctrl.activeTab = \'BUILDER\'">' +
              '<app-icon name="receipt" weight="duotone" size="15"></app-icon>' +
              '<span>{{ \'staff.inv.tabBuilder\' | t:{ count: $ctrl.settleableBookings.length } }}</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'ISSUED\'}" ' +
                    'ng-click="$ctrl.activeTab = \'ISSUED\'">' +
              '<app-icon name="check-square" weight="duotone" size="15"></app-icon>' +
              '<span>{{ \'staff.inv.tabIssued\' | t:{ count: $ctrl.issuedInvoices.length } }}</span>' +
            '</button>' +
          '</div>' +

          /* Loading Skeletons */
          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.activeTab === \'BUILDER\' && $ctrl.settleableBookings.length === 0 && !$ctrl.selectedBooking" variant="detail" count="1"></skeleton-loader>' +
          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.activeTab === \'ISSUED\' && $ctrl.issuedInvoices.length === 0" variant="table" count="5"></skeleton-loader>' +

          /* TAB 1: DOUBLE-BEZEL SPLIT INVOICE WORKBENCH */
          '<div class="grid-12" ng-if="$ctrl.activeTab === \'BUILDER\'" ng-hide="$ctrl.isLoading && $ctrl.settleableBookings.length === 0 && !$ctrl.selectedBooking">' +
            /* Left Panel: Labor + OEM Spare Parts + Voucher Selector */
            '<div class="col-7 bezel-card">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<h2 class="surface-card-title">{{ \'staff.inv.card1Title\' | t }}</h2>' +
                  '<span class="mono-num text-muted" style="font-size:12px;">POST /api/v1/service-invoices</span>' +
                '</div>' +

                '<form name="$ctrl.invoiceForm" ng-submit="$ctrl.submitInvoice()" novalidate>' +
                  '<div class="form-group">' +
                    '<label class="form-label" for="invBookingSelect">{{ \'staff.inv.selectBookingLabel\' | t }}</label>' +
                    '<select id="invBookingSelect" name="serviceBookingId" class="form-control mono-num" ' +
                            'ng-model="$ctrl.selectedBookingId" ' +
                            'ng-options="bk.id as $ctrl.formatBookingOption(bk) for bk in $ctrl.settleableBookings" ' +
                            'ng-change="$ctrl.onBookingSelected()">' +
                      '<option value="">{{ \'staff.inv.selectBookingPlaceholder\' | t }}</option>' +
                    '</select>' +
                    '<div class="field-hint" ng-if="!$ctrl.isLoading && $ctrl.settleableBookings.length === 0">' +
                      '{{ \'staff.inv.noSettleableHint\' | t }}' +
                    '</div>' +
                  '</div>' +

                  /* Customer & Vehicle Context Strip */
                  '<div class="surface-sunken fade-slide-up" style="padding:12px 14px;margin-bottom:16px;font-size:13px;" ' +
                       'ng-if="$ctrl.selectedBooking">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<div class="text-primary">' +
                        '<strong>{{ $ctrl.selectedBooking.customerFullName || $ctrl.customerLoyalty.customerFullName || (\'common.customer\' | t) }}</strong> • ' +
                        '<span class="mono-num">{{ $ctrl.selectedBooking.motorcycleModelName }} ({{ $ctrl.selectedBooking.vin }})</span>' +
                      '</div>' +
                      '<span class="badge" ng-class="\'badge-tier-\' + ($ctrl.customerLoyalty.currentTierCode || \'Bronze\').toLowerCase()">' +
                        '<app-icon name="medal" weight="duotone" size="13"></app-icon>' +
                        '<span>{{ ($ctrl.customerLoyalty.currentTierCode || \'Bronze\') | enumT:\'LoyaltyTierCode\' }} • {{ $ctrl.invoice.tierMultiplier | number:2 }}x</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="margin-top:6px;display:flex;gap:16px;flex-wrap:wrap;font-size:12px;">' +
                      '<span>{{ \'staff.inv.bookingOdo\' | t }}: <strong class="mono-num">{{ $ctrl.selectedBooking.odometerAtBookingKm | number }} km</strong></span>' +
                      '<span>{{ \'staff.inv.availablePts\' | t }}: <strong class="mono-num">{{ $ctrl.customerLoyalty.availablePoints | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                      '<span>{{ \'staff.inv.lifetimeQualifying\' | t }}: <strong class="mono-num">{{ $ctrl.customerLoyalty.lifetimeQualifyingPoints | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                      '<span ng-if="$ctrl.selectedBooking.reservedVoucherCode" class="mono-num" style="display:inline-flex;align-items:center;gap:4px;color:var(--clr-info-fg);font-weight:600;">' +
                        '<app-icon name="ticket" weight="duotone" size="13"></app-icon>' +
                        '<span>{{ \'staff.inv.reservedVoucherPrefix\' | t }}: {{ $ctrl.selectedBooking.reservedVoucherCode }}</span>' +
                      '</span>' +
                    '</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="invLabor">{{ \'staff.inv.laborLabel\' | t }}</label>' +
                      '<input id="invLabor" name="laborAmount" type="number" min="0" step="10000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.invoice.laborAmount" ng-change="$ctrl.recalculate()" required />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.laborAmount">{{ $ctrl.fieldErrors.laborAmount }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="invParts">' +
                        '<span>{{ \'staff.inv.partsLabel\' | t }}</span>' +
                        '<span ng-if="$ctrl.draftLineItems.length > 0" class="badge badge-info mono-num" style="margin-left:6px;">' +
                          '{{ \'staff.inv.autoSumBadge\' | t:{ count: $ctrl.draftLineItems.length } }}' +
                        '</span>' +
                      '</label>' +
                      '<input id="invParts" name="partsAmount" type="number" min="0" step="10000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.invoice.partsAmount" ng-readonly="$ctrl.draftLineItems.length > 0" ng-change="$ctrl.recalculate()" required />' +
                      '<div class="field-hint" ng-if="$ctrl.draftLineItems.length > 0">{{ \'staff.inv.partsAutoHint\' | t }}</div>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.partsAmount">{{ $ctrl.fieldErrors.partsAmount }}</div>' +
                    '</div>' +

                    /* OEM Spare-Parts Line-Item Picker */
                    '<div class="col-12 surface-sunken" style="margin-bottom:16px;padding:14px;">' +
                      '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;margin-bottom:10px;">' +
                        '<div style="display:inline-flex;align-items:center;gap:6px;">' +
                          '<app-icon name="package" weight="duotone" size="16" extra-class="text-secondary"></app-icon>' +
                          '<strong class="text-primary" style="font-size:13px;">{{ \'staff.inv.oemPickerTitle\' | t }}</strong>' +
                          '<span class="mono-num text-muted" style="font-size:11px;">GET /api/v1/spare-parts</span>' +
                        '</div>' +
                        '<span class="text-muted" style="font-size:11px;">' +
                          '{{ $ctrl.draftLineItems.length > 0 ? (\'staff.inv.oemPickerCountHint\' | t:{ count: $ctrl.draftLineItems.length, subtotal: ($ctrl.partsLinesSubtotal | vndCurrency) }) : (\'staff.inv.oemPickerOptionalHint\' | t) }}' +
                        '</span>' +
                      '</div>' +

                      '<div class="grid-12" style="align-items:end;gap:10px;">' +
                        '<div class="col-7 form-group" style="margin-bottom:0;">' +
                          '<label class="form-label" for="invSparePartSelect" style="font-size:12px;">{{ \'staff.inv.oemPartLabel\' | t }}</label>' +
                          '<select id="invSparePartSelect" class="form-control mono-num" ' +
                                  'ng-model="$ctrl.partPicker.selectedPartId" ' +
                                  'ng-options="sp.id as $ctrl.formatSparePartOption(sp) for sp in $ctrl.availableSpareParts">' +
                            '<option value="">{{ \'staff.inv.oemPartPlaceholder\' | t }}</option>' +
                          '</select>' +
                        '</div>' +
                        '<div class="col-2 form-group" style="margin-bottom:0;">' +
                          '<label class="form-label" for="invSparePartQty" style="font-size:12px;">{{ \'common.quantity\' | t }}</label>' +
                          '<input id="invSparePartQty" type="number" min="1" max="50" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.partPicker.quantity" />' +
                        '</div>' +
                        '<div class="col-3 form-group" style="margin-bottom:0;">' +
                          '<button type="button" id="addOemPartLineBtn" class="btn btn-secondary" style="width:100%;" ' +
                                  'ng-disabled="!$ctrl.partPicker.selectedPartId" ' +
                                  'ng-click="$ctrl.addSparePartLine()">' +
                            '<app-icon name="plus" weight="regular" size="14"></app-icon>' +
                            '<span>{{ \'staff.inv.addOemPartCta\' | t }}</span>' +
                          '</button>' +
                        '</div>' +
                      '</div>' +

                      '<div class="table-wrap" ng-if="$ctrl.draftLineItems.length > 0" ' +
                           'style="margin-top:12px;max-height:280px;overflow-y:auto;background:var(--clr-surface-card);border-radius:var(--radius-sm);border:1px solid var(--clr-border-subtle);">' +
                        '<table class="data-table data-table-dense">' +
                          '<thead>' +
                            '<tr>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.inv.colPartCode\' | t }}</th>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.inv.colDescription\' | t }}</th>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);width:90px;text-align:right;">{{ \'common.quantity\' | t }}</th>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.unitPrice\' | t }}</th>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.lineTotal\' | t }}</th>' +
                              '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);width:54px;"></th>' +
                            '</tr>' +
                          '</thead>' +
                          '<tbody>' +
                            '<tr ng-repeat="li in $ctrl.draftLineItems track by li.uid">' +
                              '<td class="mono-num tabular-nums" style="font-weight:700;">{{ li.partCode }}</td>' +
                              '<td>{{ li.description }}</td>' +
                              '<td style="text-align:right;">' +
                                '<input type="number" min="1" max="50" class="form-control mono-num tabular-nums" ' +
                                       'style="min-height:32px;padding:4px 8px;text-align:right;" ' +
                                       'ng-model="li.quantity" ng-change="$ctrl.updateLineQuantity(li)" />' +
                              '</td>' +
                              '<td class="mono-num tabular-nums" style="text-align:right;">{{ li.unitPriceVnd | vndCurrency }}</td>' +
                              '<td class="mono-num tabular-nums" style="text-align:right;font-weight:700;">{{ li.lineTotalVnd | vndCurrency }}</td>' +
                              '<td style="text-align:right;">' +
                                '<button type="button" class="btn btn-secondary btn-sm" ' +
                                        'ng-attr-aria-label="{{ \'staff.inv.removeLineAria\' | t }}" ' +
                                        'ng-click="$ctrl.removeSparePartLine(li.uid)">' +
                                  '<app-icon name="x" weight="regular" size="13"></app-icon>' +
                                '</button>' +
                              '</td>' +
                            '</tr>' +
                          '</tbody>' +
                        '</table>' +
                      '</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="invOdometer">' +
                        '{{ \'staff.inv.exitOdometerLabel\' | t:{ min: ($ctrl.minCompletionOdometerKm | number) } }}' +
                      '</label>' +
                      '<input id="invOdometer" name="odometerAtCompletionKm" type="number" min="0" max="1000000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.invoice.odometerAtCompletionKm" ng-change="$ctrl.onFieldEdited(\'odometerAtCompletionKm\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.invoice.odometerAtCompletionKm < $ctrl.minCompletionOdometerKm">' +
                        '{{ \'staff.inv.odometerRegressionError\' | t:{ min: ($ctrl.minCompletionOdometerKm | number) } }}' +
                      '</div>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.odometerAtCompletionKm">{{ $ctrl.fieldErrors.odometerAtCompletionKm }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="invPaymentMethod">{{ \'common.paymentMethod\' | t }} *</label>' +
                      '<select id="invPaymentMethod" name="paymentMethod" class="form-control" ' +
                              'ng-model="$ctrl.invoice.paymentMethod" ' +
                              'ng-options="pm.value as (pm.value | enumT:\'PaymentMethod\') for pm in $ctrl.paymentMethods"></select>' +
                    '</div>' +

                    '<div class="col-8 form-group">' +
                      '<label class="form-label" for="invVoucherSelect">{{ \'staff.inv.voucherLabel\' | t }}</label>' +
                      '<select id="invVoucherSelect" name="appliedCustomerVoucherId" class="form-control mono-num" ' +
                              'ng-model="$ctrl.invoice.appliedCustomerVoucherId" ' +
                              'ng-options="v.id as $ctrl.formatVoucherOption(v) for v in $ctrl.eligibleVouchers" ' +
                              'ng-change="$ctrl.onVoucherSelectionChanged()">' +
                        '<option value="">{{ \'staff.inv.noVoucherOption\' | t }}</option>' +
                      '</select>' +
                      '<div class="field-error" style="display:flex;align-items:center;gap:6px;" ng-if="$ctrl.summary.voucherIneligibleReason">' +
                        '<app-icon name="warning" weight="fill" size="14"></app-icon>' +
                        '<span>{{ $ctrl.summary.voucherIneligibleReason }} {{ \'staff.inv.voucherIneligibleSuffix\' | t }}</span>' +
                      '</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="invTier">{{ \'staff.inv.activeTierMultiplierLabel\' | t }}</label>' +
                      '<select id="invTier" class="form-control" ' +
                              'ng-model="$ctrl.invoice.tierMultiplier" ' +
                              'ng-disabled="Boolean($ctrl.selectedBooking)" ' +
                              'ng-options="tier.multiplier as $ctrl.formatTierOption(tier) for tier in $ctrl.tiers" ' +
                              'ng-change="$ctrl.recalculate()"></select>' +
                    '</div>' +

                    '<div class="col-12 form-group" style="margin-bottom:0;">' +
                      '<label class="form-label" for="invWorkNotes">{{ \'staff.inv.workNotesLabel\' | t }}</label>' +
                      '<textarea id="invWorkNotes" name="workSummaryNotes" rows="2" maxlength="1500" class="form-control" ' +
                                'ng-model="$ctrl.invoice.workSummaryNotes" ' +
                                'ng-attr-placeholder="{{ \'staff.inv.workNotesPlaceholder\' | t }}"></textarea>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:flex-end;gap:10px;margin-top:18px;">' +
                    '<button type="submit" id="issueServiceInvoiceBtn" class="btn-pill-cta" ' +
                            'has-permission="Permissions.Invoices.CreateDealer" ' +
                            'ng-disabled="$ctrl.isSubmitting || !$ctrl.canSubmitInvoice()">' +
                      '<span>{{ $ctrl.isSubmitting ? (\'staff.inv.settlingCta\' | t) : (\'staff.inv.finalizeCta\' | t) }}</span>' +
                      '<span class="btn-pill-well" aria-hidden="true">' +
                        '<app-icon name="{{ $ctrl.isSubmitting ? \'circle-notch\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSubmitting ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                      '</span>' +
                    '</button>' +
                  '</div>' +
                '</form>' +
              '</div>' +
            '</div>' +

            /* Right Panel: Sticky Double-Bezel Live Floor-Math Summary */
            '<div class="col-5 bezel-card" style="position:sticky;top:calc(var(--navbar-height) + 16px);align-self:start;">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<h2 class="surface-card-title">{{ \'staff.inv.card2Title\' | t }}</h2>' +
                  '<span class="badge badge-neutral mono-num">ADR-003</span>' +
                '</div>' +

                '<div style="display:flex;flex-direction:column;gap:10px;font-size:14px;">' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;">' +
                    '<span class="text-secondary">{{ \'staff.laborAmountLabel\' | t }}</span>' +
                    '<span class="mono-num tabular-nums text-primary">{{ $ctrl.summary.laborAmount | vndCurrency }}</span>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;">' +
                    '<span class="text-secondary">{{ \'staff.partsSubtotalLabel\' | t }}</span>' +
                    '<span class="mono-num tabular-nums text-primary">{{ $ctrl.summary.partsAmount | vndCurrency }}</span>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;padding-top:8px;border-top:1px solid var(--clr-border-subtle);">' +
                    '<span class="text-secondary">{{ \'common.subtotal\' | t }}</span>' +
                    '<strong class="mono-num tabular-nums text-primary">{{ $ctrl.summary.grossAmount | vndCurrency }}</strong>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;color:var(--clr-danger-fg);">' +
                    '<span>{{ \'common.discount\' | t }}{{ $ctrl.selectedVoucher ? (\' (\' + $ctrl.selectedVoucher.voucherCode + \')\') : \'\' }}</span>' +
                    '<strong class="mono-num tabular-nums">-{{ $ctrl.summary.discountClamped | vndCurrency }}</strong>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;padding-top:10px;border-top:2px solid var(--clr-border-strong);font-size:17px;">' +
                    '<strong class="text-primary">{{ \'common.netPaid\' | t }}</strong>' +
                    '<strong class="mono-num tabular-nums text-primary">{{ $ctrl.summary.netPaidAmount | vndCurrency }}</strong>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:14px;margin-top:8px;font-size:13px;">' +
                    '<div style="display:flex;align-items:center;gap:6px;font-weight:700;margin-bottom:8px;" class="text-primary">' +
                      '<app-icon name="sparkle" weight="duotone" size="16"></app-icon>' +
                      '<span>{{ \'staff.inv.oracleTitle\' | t }}</span>' +
                    '</div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                      '<span class="text-secondary">{{ \'staff.inv.step1Label\' | t }}</span>' +
                      '<strong class="mono-num tabular-nums text-primary">{{ $ctrl.summary.basePoints | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                    '</div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:6px;">' +
                      '<span class="text-secondary">{{ \'staff.inv.step2Label\' | t:{ mult: ($ctrl.invoice.tierMultiplier | number:2) } }}</span>' +
                      '<strong class="mono-num tabular-nums" style="color:var(--clr-success-fg);">+{{ $ctrl.summary.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                    '</div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:8px;padding-top:8px;border-top:1px solid var(--clr-border-subtle);">' +
                      '<span class="text-secondary">{{ \'staff.inv.qualifyingAfterLabel\' | t }}</span>' +
                      '<strong class="mono-num tabular-nums text-primary" style="display:inline-flex;align-items:center;gap:5px;">' +
                        '<span>{{ $ctrl.summary.qualifyingBefore | number }}</span>' +
                        '<app-icon name="arrow-right" weight="regular" size="12"></app-icon>' +
                        '<span>{{ $ctrl.summary.qualifyingAfter | number }} {{ \'common.ptsShort\' | t }}</span>' +
                      '</strong>' +
                    '</div>' +
                  '</div>' +

                  '<div style="padding:12px 14px;border-radius:var(--radius-md);background:var(--clr-success-bg);color:var(--clr-success-fg);border:1px solid var(--clr-success-border);" ' +
                       'ng-if="!$ctrl.summary.willUpgradeTier">' +
                    '{{ \'staff.inv.pointsOnSettlement\' | t }}: <strong class="mono-num tabular-nums">+{{ $ctrl.summary.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</strong> ' +
                    '({{ \'staff.inv.projectedTierLabel\' | t }}: <strong>{{ $ctrl.summary.projectedTierCode | enumT:\'LoyaltyTierCode\' }}</strong>)' +
                  '</div>' +

                  '<div style="padding:12px 14px;border-radius:var(--radius-md);background:var(--clr-tier-gold-bg);color:var(--clr-tier-gold-fg);border:1px solid var(--clr-tier-gold-border);" ' +
                       'ng-if="$ctrl.summary.willUpgradeTier">' +
                    '<div style="display:flex;align-items:center;gap:6px;font-weight:700;">' +
                      '<app-icon name="crown" weight="fill" size="16"></app-icon>' +
                      '<span>{{ \'staff.inv.autoPromotionTitle\' | t }}</span>' +
                    '</div>' +
                    '<div style="font-size:13px;margin-top:4px;">' +
                      '{{ \'staff.inv.autoPromotionDesc\' | t:{ pts: ($ctrl.summary.pointsEarned | number), fromTier: ($ctrl.summary.currentTierCode | enumT:\'LoyaltyTierCode\'), toTier: ($ctrl.summary.projectedTierCode | enumT:\'LoyaltyTierCode\'), mult: ($ctrl.summary.projectedTierMultiplier | number:2) } }}' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* TAB 2: ISSUED DEALER INVOICES LEDGER */
          '<div ng-if="$ctrl.activeTab === \'ISSUED\'" ng-hide="$ctrl.isLoading && $ctrl.issuedInvoices.length === 0">' +
            '<empty-state ng-if="!$ctrl.isLoading && $ctrl.issuedInvoices.length === 0" ' +
                         'icon="receipt" ' +
                         'title-key="staff.inv.emptyIssuedTitle" ' +
                         'desc-key="staff.inv.emptyIssuedDesc">' +
            '</empty-state>' +

            '<div class="bezel-card" ng-if="$ctrl.issuedInvoices.length > 0">' +
              '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
                '<div class="table-wrap" style="max-height:62vh;overflow-y:auto;">' +
                  '<table class="data-table">' +
                    '<thead>' +
                      '<tr>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.inv.colInvoiceNo\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.inv.colBookingVin\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'common.customer\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.inv.colGrossDiscount\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.netPaid\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.inv.colLoyaltyAccrued\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.inv.colPaidAt\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.receipt\' | t }}</th>' +
                      '</tr>' +
                    '</thead>' +
                    '<tbody>' +
                      '<tr class="stagger-item" ng-repeat="inv in $ctrl.issuedInvoices track by inv.id" style="--i:{{ $index }};">' +
                        '<td class="mono-num tabular-nums" style="font-weight:700;">{{ inv.invoiceNumber }}</td>' +
                        '<td>' +
                          '<div class="mono-num tabular-nums">{{ inv.bookingNumber || inv.serviceBookingId }}</div>' +
                          '<div class="mono-num tabular-nums text-muted" style="font-size:12px;">{{ inv.vin }} • {{ inv.odometerAtCompletionKm | number }} km</div>' +
                        '</td>' +
                        '<td>' +
                          '<div class="text-primary" style="font-weight:600;">{{ inv.customerFullName || (\'common.customer\' | t) }}</div>' +
                          '<div class="text-muted" style="font-size:12px;">{{ inv.motorcycleModelName }}</div>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums" style="text-align:right;">' +
                          '<div>{{ inv.grossAmount | vndCurrency }}</div>' +
                          '<div style="font-size:11px;color:var(--clr-danger-fg);" ng-if="inv.voucherDiscountAmount > 0">-{{ inv.voucherDiscountAmount | vndCurrency }}</div>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums" style="text-align:right;font-weight:700;">{{ inv.netPaidAmount | vndCurrency }}</td>' +
                        '<td style="text-align:right;">' +
                          '<span class="badge badge-success mono-num tabular-nums">+{{ inv.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</span>' +
                          '<span class="badge badge-tier-gold" style="margin-left:4px;" ng-if="inv.tierUpgraded">' +
                            '<app-icon name="sparkle" weight="fill" size="11"></app-icon>' +
                            '<span>{{ inv.newTierCode | enumT:\'LoyaltyTierCode\' }}</span>' +
                          '</span>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums">{{ inv.paidAtUtc | utcToLocal }}</td>' +
                        '<td style="text-align:right;">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.Invoices.ViewDealer" ' +
                                  'ng-click="$ctrl.openPrintableReceipt(inv)">' +
                            '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                            '<span>{{ \'staff.inv.printOfficialReceipt\' | t }}</span>' +
                          '</button>' +
                        '</td>' +
                      '</tr>' +
                    '</tbody>' +
                  '</table>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<printable-receipt-modal receipt="$ctrl.activeReceipt" receipt-kind="$ctrl.activeReceiptKind" on-close="$ctrl.closeReceipt()"></printable-receipt-modal>' +
        '</section>'
    });

  StaffInvoiceBuilderController.$inject = [
    '$q',
    '$rootScope',
    '$stateParams',
    'AuthService',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function StaffInvoiceBuilderController(
    $q,
    $rootScope,
    $stateParams,
    AuthService,
    DealerApiService,
    LoyaltyCalculator,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;
    var lineUidSeq = 0;

    function t(key, params) {
      return (I18nService && angular.isFunction(I18nService.t)) ? I18nService.t(key, params) : key;
    }

    function enumT(group, code) {
      return (I18nService && angular.isFunction(I18nService.enumLabel))
        ? I18nService.enumLabel(group, code)
        : String(code || '');
    }

    function formatVndSafe(val) {
      if (I18nService && angular.isFunction(I18nService.formatVnd)) {
        return I18nService.formatVnd(val);
      }
      return Math.round(Number(val) || 0).toLocaleString('vi-VN') + ' ₫';
    }

    function isSettleableStatus(status) {
      return status === 'Confirmed' || status === 'CheckedIn' || status === 'InProgress';
    }

    function isRealBookingParam(paramVal) {
      if (!paramVal || !angular.isString(paramVal)) {
        return false;
      }
      var trimmed = paramVal.trim();
      return Boolean(trimmed && trimmed !== 'select');
    }

    function findSelectedVoucher() {
      var targetId = $ctrl.invoice && $ctrl.invoice.appliedCustomerVoucherId;
      if (!targetId) {
        return null;
      }
      var list = $ctrl.eligibleVouchers || [];
      for (var i = 0; i < list.length; i++) {
        if (list[i].id === targetId) {
          return list[i];
        }
      }
      return null;
    }

    $ctrl.formatBookingOption = function (bk) {
      if (!bk) { return ''; }
      return bk.bookingNumber + ' • [' + enumT('BookingStatus', bk.status) + '] • ' + bk.vin + ' • ' + (bk.customerFullName || t('common.customer'));
    };

    $ctrl.formatTierOption = function (tier) {
      if (!tier) { return ''; }
      return enumT('LoyaltyTierCode', tier.code) + ' (' + tier.multiplier + 'x)';
    };

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.dealerCode = user.dealerCode || '';
      $ctrl.dealerName = user.dealerName || t('staff.inv.defaultDealerName');

      $ctrl.activeTab = 'BUILDER';
      $ctrl.isLoading = false;
      $ctrl.isSubmitting = false;
      $ctrl.loadProblem = null;
      $ctrl.submitProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.issuedReceipt = null;

      var rawParamId = ($stateParams && $stateParams.bookingId) || '';
      $ctrl.selectedBookingId = isRealBookingParam(rawParamId) ? rawParamId.trim() : '';
      $ctrl.bookingId = $ctrl.selectedBookingId;
      $ctrl.selectedBooking = null;
      $ctrl.selectedVoucher = null;
      $ctrl.minCompletionOdometerKm = 0;

      $ctrl.tiers = LoyaltyCalculator.cloneTiers();
      $ctrl.paymentMethods = [
        { value: 'BankTransfer' },
        { value: 'Cash' },
        { value: 'Card' },
        { value: 'EWallet' }
      ];

      $ctrl.customerLoyalty = {
        currentTierCode: 'Bronze',
        pointsMultiplier: 1.0,
        availablePoints: 0,
        lifetimeQualifyingPoints: 0
      };

      $ctrl.eligibleVouchers = [];
      $ctrl.voucherCatalog = [];
      $ctrl.settleableBookings = [];
      $ctrl.issuedInvoices = [];
      $ctrl.totalIssuedVnd = 0;
      $ctrl.allSpareParts = [];
      $ctrl.availableSpareParts = [];
      $ctrl.draftLineItems = [];
      $ctrl.partsLinesSubtotal = 0;
      $ctrl.partPicker = {
        selectedPartId: '',
        quantity: 1
      };
      $ctrl.activeReceipt = null;
      $ctrl.activeReceiptKind = 'ServiceInvoice';

      $ctrl.invoice = {
        laborAmount: 0,
        partsAmount: 0,
        voucherDiscountAmount: 0,
        odometerAtCompletionKm: 0,
        appliedCustomerVoucherId: '',
        tierMultiplier: 1.0,
        paymentMethod: 'BankTransfer',
        workSummaryNotes: ''
      };

      $ctrl.recalculate();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('BookingCreated', function () {
          if (!isDestroyed && !$ctrl.isSubmitting) {
            $ctrl.loadQueues({ preserveDraft: true });
          }
        }));
        cleanupFns.push(SignalRClientService.on('BookingStatusChanged', function () {
          if (!isDestroyed && !$ctrl.isSubmitting) {
            $ctrl.loadQueues({ preserveDraft: true });
          }
        }));
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed && !$ctrl.isSubmitting) {
            $ctrl.loadQueues({ preserveDraft: true });
          }
        }));
      }

      $ctrl.loadQueues();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.formatVoucherOption = function (v) {
      if (!v) { return ''; }
      var tag = v.status === 'ReservedOnBooking' ? t('staff.inv.voucherReservedTag') : '';
      var discLabel = LoyaltyCalculator.formatDiscountSummary(v) || v.discountSummary || '';
      var minTag = v.minOrderAmount > 0 ? t('staff.inv.minOrderTag', { amount: formatVndSafe(v.minOrderAmount) }) : '';
      return tag + v.voucherCode + ' • ' + discLabel + minTag;
    };

    $ctrl.formatSparePartOption = function (sp) {
      if (!sp) { return ''; }
      var fitTag = sp.compatibleModelName ? ('[' + sp.compatibleModelName + ']') : t('staff.inv.universalFitTag');
      return sp.partCode + ' • ' + sp.name + ' ' + fitTag + ' — ' + formatVndSafe(sp.unitPriceVnd || 0) + '/' + (sp.unitLabel || t('staff.inv.unitFallback'));
    };

    function filterCompatiblePartsForBooking(partsList, booking) {
      var source = angular.isArray(partsList) ? partsList : [];
      var activeOnly = source.filter(function (sp) { return sp && sp.isActive !== false; });
      if (!booking) {
        return activeOnly.map(function (sp) { return angular.extend({}, sp); });
      }
      var targetModelId = booking.motorcycleModelId ? String(booking.motorcycleModelId).trim() : '';
      var targetModelName = booking.motorcycleModelName ? String(booking.motorcycleModelName).trim().toLowerCase() : '';
      return activeOnly.filter(function (sp) {
        if (!sp.compatibleModelId) {
          return true;
        }
        if (targetModelId && String(sp.compatibleModelId) === targetModelId) {
          return true;
        }
        if (!targetModelId && targetModelName && sp.compatibleModelName) {
          return String(sp.compatibleModelName).trim().toLowerCase() === targetModelName;
        }
        return !targetModelId && !targetModelName;
      }).map(function (sp) {
        return angular.extend({}, sp);
      });
    }

    function refreshBookingCompatibleParts(booking) {
      if (!DealerApiService || !angular.isFunction(DealerApiService.getSpareParts)) {
        $ctrl.availableSpareParts = filterCompatiblePartsForBooking($ctrl.allSpareParts, booking);
        return $q ? $q.resolve($ctrl.availableSpareParts) : null;
      }
      var targetBkId = booking ? booking.id : '';
      var params = { includeUniversal: true, pageSize: 100 };
      if (booking && booking.motorcycleModelId) {
        params.compatibleModelId = booking.motorcycleModelId;
      }
      return DealerApiService.getSpareParts(params)
        .then(function (parts) {
          if (isDestroyed) { return parts; }
          if (booking && $ctrl.selectedBooking && $ctrl.selectedBooking.id !== targetBkId) {
            return parts;
          }
          $ctrl.availableSpareParts = filterCompatiblePartsForBooking(parts, booking);
          if ($ctrl.partPicker && $ctrl.partPicker.selectedPartId) {
            var stillAvailable = false;
            for (var i = 0; i < $ctrl.availableSpareParts.length; i++) {
              if ($ctrl.availableSpareParts[i].id === $ctrl.partPicker.selectedPartId) {
                stillAvailable = true;
                break;
              }
            }
            if (!stillAvailable) {
              $ctrl.partPicker.selectedPartId = '';
            }
          }
          return $ctrl.availableSpareParts;
        })
        .catch(function () {
          if (!isDestroyed) {
            $ctrl.availableSpareParts = filterCompatiblePartsForBooking($ctrl.allSpareParts, booking);
          }
          return $ctrl.availableSpareParts;
        });
    }

    $ctrl.syncPartsAmountFromLines = function () {
      var lines = $ctrl.draftLineItems || [];
      var sum = 0;
      for (var i = 0; i < lines.length; i++) {
        var qty = Math.max(1, Math.floor(Number(lines[i].quantity) || 1));
        var unit = Math.max(0, Number(lines[i].unitPriceVnd) || 0);
        lines[i].quantity = qty;
        lines[i].lineTotalVnd = qty * unit;
        sum += lines[i].lineTotalVnd;
      }
      $ctrl.partsLinesSubtotal = sum;
      if (lines.length > 0) {
        $ctrl.invoice.partsAmount = sum;
      }
    };

    $ctrl.addSparePartLine = function (explicitPartId, explicitQty) {
      var chosenId = explicitPartId || ($ctrl.partPicker && $ctrl.partPicker.selectedPartId) || '';
      if (!chosenId) {
        return null;
      }
      var qty = Math.max(1, Math.floor(Number(explicitQty !== undefined ? explicitQty : ($ctrl.partPicker && $ctrl.partPicker.quantity)) || 1));
      var matchedPart = null;
      var pool = ($ctrl.availableSpareParts && $ctrl.availableSpareParts.length > 0)
        ? $ctrl.availableSpareParts
        : ($ctrl.allSpareParts || []);
      for (var i = 0; i < pool.length; i++) {
        if (pool[i].id === chosenId) {
          matchedPart = pool[i];
          break;
        }
      }
      if (!matchedPart) {
        return null;
      }

      for (var j = 0; j < $ctrl.draftLineItems.length; j++) {
        if ($ctrl.draftLineItems[j].sparePartId === matchedPart.id) {
          $ctrl.draftLineItems[j].quantity += qty;
          $ctrl.syncPartsAmountFromLines();
          $ctrl.recalculate();
          if ($ctrl.partPicker) {
            $ctrl.partPicker.selectedPartId = '';
            $ctrl.partPicker.quantity = 1;
          }
          return $ctrl.draftLineItems[j];
        }
      }

      lineUidSeq++;
      var newLine = {
        uid: 'draft-part-' + lineUidSeq,
        sparePartId: matchedPart.id,
        partCode: matchedPart.partCode,
        description: matchedPart.name,
        unitLabel: matchedPart.unitLabel || 'Unit',
        itemType: 'Part',
        quantity: qty,
        unitPriceVnd: matchedPart.unitPriceVnd,
        lineTotalVnd: qty * matchedPart.unitPriceVnd
      };
      $ctrl.draftLineItems.push(newLine);
      $ctrl.syncPartsAmountFromLines();
      $ctrl.recalculate();
      if ($ctrl.partPicker) {
        $ctrl.partPicker.selectedPartId = '';
        $ctrl.partPicker.quantity = 1;
      }
      return newLine;
    };

    $ctrl.updateLineQuantity = function (lineItem) {
      if (!lineItem) { return; }
      lineItem.quantity = Math.max(1, Math.floor(Number(lineItem.quantity) || 1));
      $ctrl.syncPartsAmountFromLines();
      $ctrl.recalculate();
    };

    $ctrl.removeSparePartLine = function (uidOrIndex) {
      if (!$ctrl.draftLineItems || $ctrl.draftLineItems.length === 0) {
        return;
      }
      if (typeof uidOrIndex === 'number') {
        $ctrl.draftLineItems.splice(uidOrIndex, 1);
      } else {
        for (var i = 0; i < $ctrl.draftLineItems.length; i++) {
          if ($ctrl.draftLineItems[i].uid === uidOrIndex || $ctrl.draftLineItems[i].sparePartId === uidOrIndex) {
            $ctrl.draftLineItems.splice(i, 1);
            break;
          }
        }
      }
      if ($ctrl.draftLineItems.length === 0) {
        $ctrl.partsLinesSubtotal = 0;
        $ctrl.invoice.partsAmount = 0;
      } else {
        $ctrl.syncPartsAmountFromLines();
      }
      $ctrl.recalculate();
    };

    $ctrl.openPrintableReceipt = function (invoice) {
      if (!invoice) {
        return $q ? $q.resolve(null) : null;
      }
      $ctrl.activeReceiptKind = 'ServiceInvoice';
      var invId = invoice.id || invoice.invoiceId || '';
      if (!invId || !DealerApiService || !angular.isFunction(DealerApiService.getServiceInvoiceReceipt)) {
        if (ToastService) {
          ToastService.warning(t('staff.inv.toastReceiptUnavailableTitle'), t('staff.inv.toastReceiptUnavailableMsg'));
        }
        return $q ? $q.resolve(null) : null;
      }
      return DealerApiService.getServiceInvoiceReceipt(invId)
        .then(function (rcpt) {
          if (!isDestroyed) {
            $ctrl.activeReceipt = rcpt;
          }
          return rcpt;
        })
        .catch(function (rejection) {
          if (!isDestroyed && ProblemDetailsMapper && ToastService) {
            var prob = ProblemDetailsMapper.extract(rejection);
            ToastService.warning(prob.title, prob.detail);
          }
          return null;
        });
    };

    $ctrl.closeReceipt = function () {
      $ctrl.activeReceipt = null;
    };

    function syncSelectedBookingInPlace(updatedBooking) {
      $ctrl.selectedBooking = updatedBooking;
      $ctrl.bookingId = updatedBooking.bookingNumber || updatedBooking.id;
      $ctrl.minCompletionOdometerKm = Math.max(0, Number(updatedBooking.odometerAtBookingKm) || 0);
      $ctrl.recalculate();
    }

    $ctrl.loadQueues = function () {
      if (!DealerApiService) {
        return $q ? $q.resolve($ctrl.settleableBookings) : null;
      }

      var previousHydratedId = $ctrl.selectedBooking ? $ctrl.selectedBooking.id : '';
      $ctrl.isLoading = true;
      $ctrl.loadProblem = null;
      var firstError = null;
      function captureQueueError(rejection) {
        if (!firstError && ProblemDetailsMapper) {
          firstError = ProblemDetailsMapper.extract(rejection);
        }
        return [];
      }
      var targetParamBookingId = isRealBookingParam($stateParams && $stateParams.bookingId)
        ? String($stateParams.bookingId).trim()
        : '';

      return $q.all({
        bookings: DealerApiService.listBookings({ pageSize: 100 }).catch(captureQueueError),
        invoices: DealerApiService.serviceInvoices.list({ pageSize: 50 }).catch(captureQueueError),
        tiers: DealerApiService.loyalty.getTiers().catch(function () { return null; }),
        catalog: DealerApiService.vouchers.listCatalog().catch(function () { return []; }),
        spareParts: (angular.isFunction(DealerApiService.getSpareParts)
          ? DealerApiService.getSpareParts({ includeUniversal: true, pageSize: 100 }).catch(function () { return []; })
          : $q.resolve([]))
      }).then(function (bundle) {
        if (isDestroyed) {
          return bundle;
        }
        if (firstError) {
          $ctrl.loadProblem = firstError;
        }

        if (angular.isArray(bundle.tiers) && bundle.tiers.length > 0) {
          $ctrl.tiers = bundle.tiers;
        }
        $ctrl.voucherCatalog = bundle.catalog || [];
        $ctrl.issuedInvoices = bundle.invoices || [];
        var issuedSum = 0;
        for (var invIdx = 0; invIdx < $ctrl.issuedInvoices.length; invIdx++) {
          issuedSum += Number($ctrl.issuedInvoices[invIdx].netPaidAmount) || 0;
        }
        $ctrl.totalIssuedVnd = issuedSum;
        if (angular.isArray(bundle.spareParts)) {
          $ctrl.allSpareParts = bundle.spareParts;
          $ctrl.availableSpareParts = filterCompatiblePartsForBooking(bundle.spareParts, $ctrl.selectedBooking);
        }

        var settleable = (bundle.bookings || []).filter(function (bk) {
          return isSettleableStatus(bk.status);
        });
        $ctrl.settleableBookings = settleable;

        var desiredId = $ctrl.selectedBookingId || targetParamBookingId;
        var matched = null;
        for (var i = 0; i < settleable.length; i++) {
          if (settleable[i].id === desiredId || settleable[i].bookingNumber === desiredId) {
            matched = settleable[i];
            break;
          }
        }

        if (!matched && desiredId && desiredId.length >= 32) {
          return DealerApiService.getBookingById(desiredId)
            .then(function (fetchedBk) {
              if (isDestroyed) { return bundle; }
              if (fetchedBk && isSettleableStatus(fetchedBk.status)) {
                $ctrl.settleableBookings.unshift(fetchedBk);
                $ctrl.selectedBookingId = fetchedBk.id;
                if (previousHydratedId && fetchedBk.id === previousHydratedId) {
                  syncSelectedBookingInPlace(fetchedBk);
                  return bundle;
                }
                return $ctrl.onBookingSelected();
              }
              if (fetchedBk && fetchedBk.status === 'Completed') {
                for (var k = 0; k < $ctrl.issuedInvoices.length; k++) {
                  if ($ctrl.issuedInvoices[k].serviceBookingId === fetchedBk.id) {
                    $ctrl.issuedReceipt = $ctrl.issuedInvoices[k];
                    break;
                  }
                }
              }
              if ($ctrl.settleableBookings.length > 0) {
                $ctrl.selectedBookingId = $ctrl.settleableBookings[0].id;
                return $ctrl.onBookingSelected();
              }
              $ctrl.selectedBookingId = '';
              $ctrl.selectedBooking = null;
              $ctrl.recalculate();
              return bundle;
            })
            .catch(function () {
              if (!isDestroyed && $ctrl.settleableBookings.length > 0) {
                $ctrl.selectedBookingId = $ctrl.settleableBookings[0].id;
                return $ctrl.onBookingSelected();
              }
              return bundle;
            });
        }

        if (matched) {
          $ctrl.selectedBookingId = matched.id;
          if (previousHydratedId && matched.id === previousHydratedId) {
            syncSelectedBookingInPlace(matched);
            return bundle;
          }
          return $ctrl.onBookingSelected();
        }
        if (settleable.length > 0) {
          $ctrl.selectedBookingId = settleable[0].id;
          return $ctrl.onBookingSelected();
        }

        $ctrl.selectedBookingId = '';
        $ctrl.selectedBooking = null;
        $ctrl.recalculate();
        return bundle;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.onBookingSelected = function () {
      var prevId = $ctrl.selectedBooking ? $ctrl.selectedBooking.id : '';
      $ctrl.submitProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.selectedBooking = null;

      for (var i = 0; i < ($ctrl.settleableBookings || []).length; i++) {
        if ($ctrl.settleableBookings[i].id === $ctrl.selectedBookingId) {
          $ctrl.selectedBooking = $ctrl.settleableBookings[i];
          break;
        }
      }

      if (!$ctrl.selectedBooking) {
        $ctrl.eligibleVouchers = [];
        $ctrl.draftLineItems = [];
        $ctrl.partsLinesSubtotal = 0;
        $ctrl.availableSpareParts = filterCompatiblePartsForBooking($ctrl.allSpareParts, null);
        $ctrl.invoice.appliedCustomerVoucherId = '';
        $ctrl.minCompletionOdometerKm = 0;
        $ctrl.recalculate();
        return $q ? $q.resolve(null) : null;
      }

      var bk = $ctrl.selectedBooking;
      if (prevId !== bk.id) {
        $ctrl.draftLineItems = [];
        $ctrl.partsLinesSubtotal = 0;
        if ($ctrl.partPicker) {
          $ctrl.partPicker.selectedPartId = '';
          $ctrl.partPicker.quantity = 1;
        }
      }
      refreshBookingCompatibleParts(bk);
      $ctrl.bookingId = bk.bookingNumber || bk.id;
      $ctrl.minCompletionOdometerKm = Math.max(0, Number(bk.odometerAtBookingKm) || 0);
      $ctrl.invoice.odometerAtCompletionKm = $ctrl.minCompletionOdometerKm;
      if (bk.reportedNotes) {
        $ctrl.invoice.workSummaryNotes = t('staff.inv.completedNotePrefix') + bk.reportedNotes;
      }

      if (!bk.customerId || !DealerApiService) {
        $ctrl.recalculate();
        return $q ? $q.resolve(null) : null;
      }

      var targetBookingId = bk.id;
      return DealerApiService.loyalty.getCustomerSummary(bk.customerId)
        .then(function (summary) {
          if (isDestroyed || !$ctrl.selectedBooking || $ctrl.selectedBooking.id !== targetBookingId) {
            return summary;
          }
          $ctrl.customerLoyalty = summary;
          $ctrl.invoice.tierMultiplier = summary.pointsMultiplier || 1.0;

          var rawList = summary.eligibleVouchers || [];
          var usable = rawList.filter(function (v) {
            var scopeOk = v.applicableScope === 'ServiceOnly' || v.applicableScope === 'All';
            var statusOk = v.effectiveStatus === 'Available' ||
              (v.status === 'ReservedOnBooking' && bk.appliedCustomerVoucherId && v.id === bk.appliedCustomerVoucherId);
            return scopeOk && statusOk;
          });

          if (bk.appliedCustomerVoucherId) {
            var hasReservedInList = false;
            for (var j = 0; j < usable.length; j++) {
              if (usable[j].id === bk.appliedCustomerVoucherId) {
                hasReservedInList = true;
                break;
              }
            }
            if (!hasReservedInList) {
              var prefix = bk.reservedVoucherCode ? String(bk.reservedVoucherCode).split('-')[0] : '';
              var matchedCat = null;
              for (var c = 0; c < ($ctrl.voucherCatalog || []).length; c++) {
                if ($ctrl.voucherCatalog[c].codePrefix === prefix) {
                  matchedCat = $ctrl.voucherCatalog[c];
                  break;
                }
              }
              usable.unshift(DealerApiService.normalizeCustomerVoucher({
                id: bk.appliedCustomerVoucherId,
                customerVoucherId: bk.appliedCustomerVoucherId,
                voucherCode: bk.reservedVoucherCode || 'RESERVED-VOUCHER',
                applicableScope: matchedCat ? matchedCat.applicableScope : 'ServiceOnly',
                discountType: matchedCat ? matchedCat.discountType : 'FixedAmount',
                discountValue: matchedCat ? matchedCat.discountValue : 0,
                maxDiscountAmount: matchedCat ? matchedCat.maxDiscountAmount : null,
                minOrderAmount: matchedCat ? matchedCat.minOrderAmount : 0,
                status: 'ReservedOnBooking',
                effectiveStatus: 'ReservedOnBooking'
              }));
            }
            $ctrl.invoice.appliedCustomerVoucherId = bk.appliedCustomerVoucherId;
          } else {
            $ctrl.invoice.appliedCustomerVoucherId = '';
          }

          $ctrl.eligibleVouchers = usable;
          $ctrl.recalculate();
          return summary;
        })
        .catch(function () {
          if (!isDestroyed) {
            $ctrl.recalculate();
          }
          return null;
        });
    };

    $ctrl.onVoucherSelectionChanged = function () {
      $ctrl.recalculate();
    };

    $ctrl.onFieldEdited = function (fieldName) {
      if (ProblemDetailsMapper) {
        ProblemDetailsMapper.clearFieldError($ctrl.invoiceForm, $ctrl.fieldErrors, fieldName);
      }
    };

    $ctrl.recalculate = function () {
      if ($ctrl.draftLineItems && $ctrl.draftLineItems.length > 0) {
        $ctrl.syncPartsAmountFromLines();
      } else {
        $ctrl.partsLinesSubtotal = 0;
      }
      var labor = Math.max(0, Number($ctrl.invoice.laborAmount) || 0);
      var parts = Math.max(0, Number($ctrl.invoice.partsAmount) || 0);
      var gross = labor + parts;

      $ctrl.selectedVoucher = findSelectedVoucher();
      var discount = 0;
      var ineligibleReason = null;

      if ($ctrl.selectedVoucher) {
        var voucherEval = LoyaltyCalculator.computeVoucherDiscount($ctrl.selectedVoucher, gross);
        discount = voucherEval.discountAmount;
        ineligibleReason = voucherEval.ineligibleReason;
        $ctrl.invoice.voucherDiscountAmount = discount;
      } else {
        discount = 0;
        $ctrl.invoice.voucherDiscountAmount = 0;
      }

      var netPaid = Math.max(0, gross - discount);
      var activeMult = Number($ctrl.invoice.tierMultiplier) || 1.0;
      var basePoints = Math.floor(netPaid / 10000);
      var pointsEarned = LoyaltyCalculator.calculatePoints(netPaid, activeMult);

      var curLoyalty = $ctrl.customerLoyalty || {};
      var currentTierCode = curLoyalty.currentTierCode || LoyaltyCalculator.determineTierCode(curLoyalty.lifetimeQualifyingPoints || 0, $ctrl.tiers);
      var qualifyingBefore = Math.max(0, Math.floor(Number(curLoyalty.lifetimeQualifyingPoints) || 0));
      var qualifyingAfter = qualifyingBefore + pointsEarned;
      var projectedTier = LoyaltyCalculator.determineTier(qualifyingAfter, $ctrl.tiers);
      var willUpgrade = LoyaltyCalculator.getTierRank(projectedTier.code, $ctrl.tiers) > LoyaltyCalculator.getTierRank(currentTierCode, $ctrl.tiers);

      $ctrl.summary = {
        laborAmount: labor,
        partsAmount: parts,
        grossAmount: gross,
        discountClamped: discount,
        voucherDiscountAmount: discount,
        voucherIneligibleReason: ineligibleReason,
        netPaidAmount: netPaid,
        basePoints: basePoints,
        pointsEarned: pointsEarned,
        qualifyingBefore: qualifyingBefore,
        qualifyingAfter: qualifyingAfter,
        currentTierCode: currentTierCode,
        projectedTierCode: projectedTier.code,
        projectedTierMultiplier: projectedTier.multiplier,
        willUpgradeTier: willUpgrade
      };
    };

    $ctrl.canSubmitInvoice = function () {
      if (!$ctrl.selectedBooking) {
        return false;
      }
      var grossOk = ($ctrl.summary && $ctrl.summary.grossAmount > 0);
      var odoOk = Number($ctrl.invoice.odometerAtCompletionKm) >= $ctrl.minCompletionOdometerKm;
      var voucherOk = !($ctrl.summary && $ctrl.summary.voucherIneligibleReason);
      return Boolean(grossOk && odoOk && voucherOk);
    };

    $ctrl.dismissReceipt = function () {
      $ctrl.issuedReceipt = null;
    };

    $ctrl.submitInvoice = function () {
      if (!$ctrl.canSubmitInvoice() || $ctrl.isSubmitting || !DealerApiService || !DealerApiService.serviceInvoices) {
        return $q ? $q.resolve(null) : null;
      }

      $ctrl.isSubmitting = true;
      $ctrl.submitProblem = null;
      $ctrl.fieldErrors = {};

      var bk = $ctrl.selectedBooking;
      var chosenVoucherId = $ctrl.invoice.appliedCustomerVoucherId || null;
      var hadReservedVoucher = Boolean(bk && bk.appliedCustomerVoucherId);
      var isUsingReserved = Boolean(hadReservedVoucher && chosenVoucherId === bk.appliedCustomerVoucherId);
      var isRemovingReserved = Boolean(hadReservedVoucher && !chosenVoucherId);
      var outboundLines = ($ctrl.draftLineItems && $ctrl.draftLineItems.length > 0)
        ? $ctrl.draftLineItems.map(function (li) {
          return {
            sparePartId: li.sparePartId,
            itemType: 'Part',
            description: li.description,
            quantity: li.quantity,
            unitPriceVnd: li.unitPriceVnd
          };
        })
        : null;

      return DealerApiService.serviceInvoices.create({
        serviceBookingId: bk.id,
        bookingRowVersion: bk.rowVersion || null,
        laborAmount: Number($ctrl.invoice.laborAmount) || 0,
        partsAmount: Number($ctrl.invoice.partsAmount) || 0,
        odometerAtCompletionKm: Math.floor(Number($ctrl.invoice.odometerAtCompletionKm) || 0),
        appliedCustomerVoucherId: chosenVoucherId,
        voucherCode: $ctrl.selectedVoucher ? $ctrl.selectedVoucher.voucherCode : null,
        customerVoucherRowVersion: $ctrl.selectedVoucher && $ctrl.selectedVoucher.rowVersion ? $ctrl.selectedVoucher.rowVersion : null,
        useBookingReservedVoucher: isUsingReserved,
        removeReservedVoucher: isRemovingReserved,
        paymentMethod: $ctrl.invoice.paymentMethod || 'BankTransfer',
        workSummaryNotes: $ctrl.invoice.workSummaryNotes || null,
        lineItems: outboundLines
      }).then(function (receipt) {
        if (isDestroyed) {
          return receipt;
        }
        $ctrl.issuedReceipt = receipt;
        $ctrl.draftLineItems = [];
        $ctrl.partsLinesSubtotal = 0;
        $ctrl.selectedBookingId = '';
        $ctrl.selectedBooking = null;
        if (ToastService) {
          var upgradeSuffix = receipt.tierUpgraded
            ? t('staff.inv.toastUpgradedSuffix', { tier: enumT('LoyaltyTierCode', receipt.newTierCode) })
            : '';
          ToastService.success(
            t('staff.inv.toastSettledTitle'),
            t('staff.inv.toastSettledMsg', {
              invoiceNumber: receipt.invoiceNumber,
              pts: receipt.pointsEarned,
              upgrade: upgradeSuffix
            })
          );
        }
        return $ctrl.loadQueues().then(function () {
          return receipt;
        });
      }).catch(function (rejection) {
        if (isDestroyed) {
          return null;
        }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.invoiceForm, problem);
        $ctrl.submitProblem = problem;
        if (problem.status === 409) {
          $ctrl.loadQueues();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSubmitting = false;
        }
      });
    };
  }
})();
