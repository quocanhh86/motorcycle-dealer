(function () {
  'use strict';

  var GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  var VI_PATCH = {
    staff: {
      sales: {
        eyebrow: 'SCR-STF-03 • KHO VIN & BÀN GIAO SHOWROOM',
        refreshing: 'Đang tải...',
        kpiReadyVins: 'Xe VIN sẵn sàng giao',
        kpiReadyMeta: 'Khả dụng tại showroom chi nhánh',
        kpiReservedVins: 'Xe đang giữ chỗ',
        kpiReservedMeta: 'Đang chờ hoàn tất thủ tục',
        kpiSoldVins: 'Xe đã bàn giao',
        kpiSoldMeta: 'Đã mở Garage VIN tự động',
        kpiOrdersCount: 'Đơn bán xe đã chốt',
        kpiOrdersMeta: 'Sổ đơn bán xe bất biến',
        tabInventory: '1. Kho VIN Chi nhánh ({{count}})',
        tabOrders: '2. Sổ đơn bán xe Showroom ({{count}})',
        searchLabel: 'Tìm theo số VIN 17 ký tự, số máy, mẫu xe hoặc màu sơn',
        searchPlaceholder: 'Nhập số khung VIN 17 ký tự, số máy, tên mẫu xe hoặc màu sơn...',
        stockStatusLabel: 'Trạng thái kho',
        allStatusesOption: 'Tất cả trạng thái kho',
        colVin: 'Số khung VIN (17 ký tự)',
        colEngine: 'Số máy',
        colModelColor: 'Mẫu xe & Màu sơn',
        colListedPrice: 'Giá niêm yết',
        emptyInventoryTitle: 'Không có xe VIN nào khớp bộ lọc',
        emptyInventoryDesc: 'Nhập kho số khung VIN mới hoặc xóa bộ lọc để xem toàn bộ xe tại chi nhánh.',
        actionSellOnboard: 'Bán xe & Mở Garage',
        actionReserve: 'Giữ chỗ',
        actionRelease: 'Hủy giữ',
        colOrderNo: 'Mã đơn hàng',
        colBuyer: 'Khách mua xe',
        colSoldVinModel: 'Số khung VIN & Mẫu xe',
        colFinalPaid: 'Thực thu',
        colLoyaltyAccrued: 'Điểm tích lũy',
        colCompletedAt: 'Thời điểm hoàn tất',
        printReceiptCta: 'In biên lai',
        emptyOrdersTitle: 'Chưa có đơn bán xe nào được ghi nhận',
        emptyOrdersDesc: 'Các đơn bán xe hoàn tất tại chi nhánh sẽ hiển thị bất biến tại đây kèm biên lai in.',
        intakeModalTitle: 'Nhập kho Số khung VIN Showroom mới',
        selectModelLabel: 'Mẫu xe danh mục *',
        selectModelPlaceholder: '-- Chọn mẫu xe trong danh mục --',
        vinFieldLabel: 'Số khung VIN chuẩn ISO 3779 (17 ký tự, không gồm I, O, Q) *',
        vinForbiddenError: 'Chuẩn ISO 3779 không cho phép các ký tự I, O và Q.',
        vinFormatError: 'Số khung VIN phải gồm đúng 17 ký tự chữ hoa và số.',
        engineLabel: 'Số máy *',
        enginePlaceholder: 'Nhập số serial động cơ',
        colorLabel: 'Màu sơn *',
        colorPlaceholder: 'Nhập màu sắc ngoại thất',
        yearLabel: 'Năm SX *',
        listedPriceLabel: 'Giá bán niêm yết (VNĐ) *',
        addingVinCta: 'Đang nhập kho VIN...',
        addVinSubmitCta: 'Thêm VIN vào kho chi nhánh',
        checkoutTitleCompleted: 'Hoàn tất bán xe • {{orderNumber}}',
        checkoutTitleActive: 'Thanh toán Bán xe Showroom • {{vin}}',
        saleCommittedHeadline: 'Đã chốt đơn bán xe khép kín!',
        saleCommittedDesc: 'Số khung VIN {{vin}} đã chuyển trạng thái Đã bán và tự động thêm vào Garage VIN điện tử của khách hàng.',
        receiptKpiOrder: 'Mã đơn & Thực thanh toán',
        receiptKpiPaid: 'Đã thanh toán: {{amount}} ({{method}})',
        receiptKpiLoyalty: 'Điểm thưởng & Kích hoạt Garage',
        upgradedTag: '(Thăng hạng!)',
        doneReturnCta: 'Hoàn tất & Về kho VIN',
        recentBuyersLabel: 'Khách hàng gần đây (Chọn nhanh không bắt buộc)',
        recentBuyersPlaceholder: '-- Không bắt buộc: Chọn từ danh sách hoặc nhập Email / GUID bên dưới --',
        buyerEmailLabel: 'Email Khách hàng hoặc Customer GUID *',
        buyerEmailPlaceholder: 'Nhập email khách hàng hoặc mã GUID',
        lookupLoyaltyCta: 'Tra cứu hạng',
        lookingUpCta: 'Đang tra cứu...',
        promoDiscountLabel: 'Giảm giá khuyến mãi (VNĐ)',
        licensePlateOptLabel: 'Biển số xe (Không bắt buộc)',
        voucherOptLabel: 'Mã Voucher (Không bắt buộc)',
        previewTitle: 'Dự toán Bán xe & Tích điểm Tự động',
        vinLockBadge: 'KHÓA VIN ACID',
        buyerRateLabel: 'Hệ số hội viên',
        postSaleTierLabel: 'Hạng sau đơn hàng',
        autoOnboardNote: 'Tự động tạo bản ghi CustomerVehicle (0 km) gắn chuẩn bảo dưỡng OEM.',
        committingSaleCta: 'Đang chốt đơn ACID...',
        toastHandoffTitle: 'Chuyển tiếp từ Lịch Lái thử',
        toastHandoffMsg: 'Chọn xe VIN sẵn sàng bán bên dưới để chốt đơn cho {{customer}}.',
        toastVinAddedTitle: 'Đã nhập kho VIN mới',
        toastVinAddedMsg: '{{vin}} ({{model}}) đã sẵn sàng bán tại showroom.',
        toastStockUpdatedTitle: 'Đã cập nhật trạng thái kho',
        toastStockUpdatedMsg: '{{vin}} chuyển sang {{status}}.',
        toastSaleCompletedTitle: 'Hoàn tất đơn bán xe Showroom',
        toastSaleCompletedMsg: '{{orderNumber}} • VIN {{vin}} đã mở Garage (+{{pts}} điểm).',
        toastReceiptUnavailableTitle: 'Không thể tải biên lai',
        toastReceiptUnavailableMsg: 'Không tìm thấy mã đơn bán xe để tra cứu biên lai.',
        convertedNotePrefix: 'Chuyển đổi từ lịch lái thử ',
        defaultDealerName: 'Đại lý ủy quyền'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      sales: {
        eyebrow: 'SCR-STF-03 • VIN INVENTORY & SALES DESK',
        refreshing: 'Refreshing...',
        kpiReadyVins: 'Ready-to-Deliver VINs',
        kpiReadyMeta: 'Available in branch showroom',
        kpiReservedVins: 'Reserved VINs',
        kpiReservedMeta: 'Held pending checkout',
        kpiSoldVins: 'Sold Showroom Units',
        kpiSoldMeta: 'Auto-onboarded to VIN Garage',
        kpiOrdersCount: 'Completed Sales Orders',
        kpiOrdersMeta: 'Immutable sales ledger',
        tabInventory: '1. Dealer VIN Inventory ({{count}})',
        tabOrders: '2. Showroom Sales Orders ({{count}})',
        searchLabel: 'Search 17-Char VIN, Engine #, Model, or Color',
        searchPlaceholder: 'Search by 17-character VIN, engine #, motorcycle model, or color...',
        stockStatusLabel: 'Stock Status',
        allStatusesOption: 'All Stock Statuses',
        colVin: 'Physical VIN (17-Char)',
        colEngine: 'Engine #',
        colModelColor: 'Motorcycle Model & Color',
        colListedPrice: 'Listed Price',
        emptyInventoryTitle: 'No showroom VIN units match criteria',
        emptyInventoryDesc: 'Intake a new 17-character VIN unit or clear filters to inspect all branch inventory.',
        actionSellOnboard: 'Sell & Onboard VIN',
        actionReserve: 'Reserve',
        actionRelease: 'Release',
        colOrderNo: 'Order #',
        colBuyer: 'Buyer',
        colSoldVinModel: 'Sold VIN & Model',
        colFinalPaid: 'Final Paid',
        colLoyaltyAccrued: 'Loyalty Accrued',
        colCompletedAt: 'Completed At',
        printReceiptCta: 'Print Receipt',
        emptyOrdersTitle: 'No showroom sales orders recorded yet',
        emptyOrdersDesc: 'Completed showroom bike sales are archived immutably here with printable delivery receipts.',
        intakeModalTitle: 'Intake New Showroom Motorcycle VIN',
        selectModelLabel: 'Motorcycle Model *',
        selectModelPlaceholder: '-- Select Catalog Model --',
        vinFieldLabel: '17-Character ISO 3779 VIN * (Excludes I, O, Q)',
        vinForbiddenError: 'ISO 3779 prohibits letters I, O, and Q.',
        vinFormatError: 'VIN must be 17 uppercase alphanumeric characters.',
        engineLabel: 'Engine Number *',
        enginePlaceholder: 'Enter engine serial number',
        colorLabel: 'Colorway *',
        colorPlaceholder: 'Enter exterior colorway',
        yearLabel: 'Year *',
        listedPriceLabel: 'Listed Retail Price (VND) *',
        addingVinCta: 'Adding VIN...',
        addVinSubmitCta: 'Add VIN to Branch Stock',
        checkoutTitleCompleted: 'Sale Completed • {{orderNumber}}',
        checkoutTitleActive: 'Showroom Bike Sale Checkout • {{vin}}',
        saleCommittedHeadline: 'Closed-Loop Showroom Sale Committed!',
        saleCommittedDesc: 'Physical VIN {{vin}} marked Sold and automatically onboarded into the customer Digital VIN Garage.',
        receiptKpiOrder: 'Order # & Final Settled',
        receiptKpiPaid: 'Paid: {{amount}} ({{method}})',
        receiptKpiLoyalty: 'Loyalty Points & Garage Onboarding',
        upgradedTag: '(Upgraded!)',
        doneReturnCta: 'Done & Return to Inventory',
        recentBuyersLabel: 'Recently Seen Live Customers (Optional Quick-Pick)',
        recentBuyersPlaceholder: '-- Optional: Select from Live Records or Enter Email / GUID Below --',
        buyerEmailLabel: 'Buyer Customer Email or Customer GUID *',
        buyerEmailPlaceholder: 'Enter customer email address or GUID',
        lookupLoyaltyCta: 'Lookup Loyalty',
        lookingUpCta: 'Looking up...',
        promoDiscountLabel: 'Promo Discount (VND)',
        licensePlateOptLabel: 'License Plate (Optional)',
        voucherOptLabel: 'Voucher Code (Optional)',
        previewTitle: 'Atomic Sale & Loyalty Preview',
        vinLockBadge: 'ACID VIN LOCK',
        buyerRateLabel: 'Buyer Rate',
        postSaleTierLabel: 'Post-Sale Tier',
        autoOnboardNote: 'Auto-creates CustomerVehicle (0 km) bound to OEM MaintenanceRules.',
        committingSaleCta: 'Committing ACID Sale...',
        toastHandoffTitle: 'Test-Drive Handoff',
        toastHandoffMsg: 'Select an Available VIN unit below to complete bike sale for {{customer}}.',
        toastVinAddedTitle: 'VIN Added to Stock',
        toastVinAddedMsg: '{{vin}} ({{model}}) is now Available.',
        toastStockUpdatedTitle: 'Stock Updated',
        toastStockUpdatedMsg: '{{vin}} marked {{status}}.',
        toastSaleCompletedTitle: 'Showroom Sale Completed',
        toastSaleCompletedMsg: '{{orderNumber}} • VIN {{vin}} onboarded (+{{pts}} pts).',
        toastReceiptUnavailableTitle: 'Receipt Unavailable',
        toastReceiptUnavailableMsg: 'Could not resolve a sales order identifier for printable receipt lookup.',
        convertedNotePrefix: 'Converted from Test-Drive ',
        defaultDealerName: 'Authorized Dealership'
      }
    }
  };

  registerSalesDeskI18n.$inject = ['I18nService'];
  function registerSalesDeskI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerSalesDeskI18n)
    .component('staffSalesDesk', {
      controllerAs: '$ctrl',
      controller: StaffSalesDeskController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="storefront" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.sales.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.salesDeskTitle\' | t }}</h1>' +
              '<p class="page-subtitle">' +
                '<span class="mono-num">{{ $ctrl.dealerCode }}</span> ({{ $ctrl.dealerName }}) — {{ \'staff.salesDeskSubtitle\' | t }}' +
              '</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadSalesDeskData()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrows-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'staff.sales.refreshing\' | t) : (\'common.refresh\' | t) }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Inventories.ManageDealer" ' +
                      'ng-click="$ctrl.openIntakeModal()">' +
                '<span>{{ \'staff.intakeVinCta\' | t }}</span>' +
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
            'on-retry="$ctrl.loadSalesDeskData()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.sales.kpiReadyVins\' | t }}</span>' +
                  '<app-icon name="motorcycle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-success-fg);" kpi-counter="$ctrl.stockStats.available">0</div>' +
                '<div class="kpi-meta">{{ \'staff.sales.kpiReadyMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.sales.kpiReservedVins\' | t }}</span>' +
                  '<app-icon name="lock" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="color:var(--clr-warning-fg);" kpi-counter="$ctrl.stockStats.reserved">0</div>' +
                '<div class="kpi-meta">{{ \'staff.sales.kpiReservedMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.sales.kpiSoldVins\' | t }}</span>' +
                  '<app-icon name="check-circle" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.stockStats.sold">0</div>' +
                '<div class="kpi-meta">{{ \'staff.sales.kpiSoldMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'staff.sales.kpiOrdersCount\' | t }}</span>' +
                  '<app-icon name="receipt" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.salesOrders.length">0</div>' +
                '<div class="kpi-meta">{{ \'staff.sales.kpiOrdersMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="tab-strip">' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'INVENTORY\'}" ' +
                    'ng-click="$ctrl.activeTab = \'INVENTORY\'">' +
              '<app-icon name="motorcycle" weight="duotone" size="15"></app-icon>' +
              '<span>{{ \'staff.sales.tabInventory\' | t:{ count: $ctrl.inventories.length } }}</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'ORDERS\'}" ' +
                    'ng-click="$ctrl.activeTab = \'ORDERS\'">' +
              '<app-icon name="receipt" weight="duotone" size="15"></app-icon>' +
              '<span>{{ \'staff.sales.tabOrders\' | t:{ count: $ctrl.salesOrders.length } }}</span>' +
            '</button>' +
          '</div>' +

          /* TAB 1: Dealer VIN Inventory */
          '<div ng-if="$ctrl.activeTab === \'INVENTORY\'">' +
            '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-5 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="invSearchInput">{{ \'staff.sales.searchLabel\' | t }}</label>' +
                  '<input id="invSearchInput" type="search" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyInventoryFilters()" ' +
                         'ng-attr-placeholder="{{ \'staff.sales.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="invStatusSelect">{{ \'staff.sales.stockStatusLabel\' | t }}</label>' +
                  '<select id="invStatusSelect" class="form-control" ' +
                          'ng-model="$ctrl.filters.stockStatus" ' +
                          'ng-options="s.value as $ctrl.formatStockStatusOption(s) for s in $ctrl.stockStatusOptions" ' +
                          'ng-change="$ctrl.applyInventoryFilters()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetInventoryFilters()">' +
                    '<app-icon name="funnel" weight="regular" size="15"></app-icon>' +
                    '<span>{{ \'common.clearFilters\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.inventories.length === 0" variant="table" count="5"></skeleton-loader>' +

            '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredInventories.length === 0" ' +
                         'icon="motorcycle" ' +
                         'title-key="staff.sales.emptyInventoryTitle" ' +
                         'desc-key="staff.sales.emptyInventoryDesc" ' +
                         'action-key="common.clearFilters" ' +
                         'on-action="$ctrl.resetInventoryFilters()">' +
            '</empty-state>' +

            '<div class="bezel-card" ng-if="$ctrl.filteredInventories.length > 0">' +
              '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
                '<div class="table-wrap" style="max-height:62vh;overflow-y:auto;">' +
                  '<table class="data-table">' +
                    '<thead>' +
                      '<tr>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colVin\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colEngine\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colModelColor\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.sales.colListedPrice\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'common.status\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.actions\' | t }}</th>' +
                      '</tr>' +
                    '</thead>' +
                    '<tbody>' +
                      '<tr class="stagger-item" ng-repeat="unit in $ctrl.filteredInventories track by unit.id" style="--i:{{ $index }};">' +
                        '<td class="mono-num tabular-nums" style="font-weight:700;">{{ unit.vin }}</td>' +
                        '<td class="mono-num tabular-nums text-secondary">{{ unit.engineNumber }}</td>' +
                        '<td>' +
                          '<div style="display:flex;align-items:center;gap:10px;">' +
                            '<div style="width:68px;flex-shrink:0;">' +
                              '<bike-image src="$ctrl.resolveUnitImage(unit)" alt="{{ unit.motorcycleModelName }}" variant="thumb"></bike-image>' +
                            '</div>' +
                            '<div>' +
                              '<div class="text-primary" style="font-weight:600;">{{ unit.motorcycleModelName }}</div>' +
                              '<div class="text-muted" style="font-size:12px;">{{ unit.colorName }} • {{ unit.manufactureYear }}</div>' +
                            '</div>' +
                          '</div>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums" style="text-align:right;font-weight:600;">{{ unit.listedPrice | vndCurrency }}</td>' +
                        '<td>' +
                          '<span class="badge" ng-class="$ctrl.stockBadgeClass(unit.stockStatus)">' +
                            '<app-icon name="{{ unit.stockStatus === \'Available\' ? \'check-circle\' : (unit.stockStatus === \'Reserved\' ? \'lock\' : \'check-square\') }}" weight="fill" size="12"></app-icon>' +
                            '<span>{{ unit.stockStatus | enumT:\'StockStatus\' }}</span>' +
                          '</span>' +
                        '</td>' +
                        '<td style="text-align:right;">' +
                          '<div style="display:inline-flex;gap:6px;flex-wrap:wrap;justify-content:flex-end;">' +
                            '<button type="button" class="btn btn-primary btn-sm" ' +
                                    'ng-if="unit.stockStatus === \'Available\' || unit.stockStatus === \'Reserved\'" ' +
                                    'has-permission="Permissions.SalesOrders.Create" ' +
                                    'ng-disabled="$ctrl.busyInventoryId === unit.id" ' +
                                    'ng-click="$ctrl.openCheckoutModal(unit)">' +
                              '<span>{{ \'staff.sales.actionSellOnboard\' | t }}</span>' +
                              '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                            '</button>' +
                            '<button type="button" class="btn btn-secondary btn-sm" ' +
                                    'ng-if="unit.stockStatus === \'Available\' || unit.stockStatus === \'Reserved\'" ' +
                                    'has-permission="Permissions.Inventories.ManageDealer" ' +
                                    'ng-disabled="$ctrl.busyInventoryId === unit.id" ' +
                                    'ng-click="$ctrl.toggleReserveStatus(unit)">' +
                              '<app-icon name="{{ $ctrl.busyInventoryId === unit.id ? \'circle-notch\' : (unit.stockStatus === \'Available\' ? \'lock\' : \'lock-open\') }}" weight="regular" size="13" extra-class="{{ $ctrl.busyInventoryId === unit.id ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                              '<span>{{ unit.stockStatus === \'Available\' ? (\'staff.sales.actionReserve\' | t) : (\'staff.sales.actionRelease\' | t) }}</span>' +
                            '</button>' +
                          '</div>' +
                        '</td>' +
                      '</tr>' +
                    '</tbody>' +
                  '</table>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* TAB 2: Completed SalesOrders Ledger */
          '<div ng-if="$ctrl.activeTab === \'ORDERS\'">' +
            '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.salesOrders.length === 0" variant="table" count="5"></skeleton-loader>' +

            '<empty-state ng-if="!$ctrl.isLoading && $ctrl.salesOrders.length === 0" ' +
                         'icon="receipt" ' +
                         'title-key="staff.sales.emptyOrdersTitle" ' +
                         'desc-key="staff.sales.emptyOrdersDesc">' +
            '</empty-state>' +

            '<div class="bezel-card" ng-if="$ctrl.salesOrders.length > 0">' +
              '<div class="bezel-card-inner" style="padding:var(--space-3);">' +
                '<div class="table-wrap" style="max-height:62vh;overflow-y:auto;">' +
                  '<table class="data-table">' +
                    '<thead>' +
                      '<tr>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colOrderNo\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colBuyer\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colSoldVinModel\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.sales.colFinalPaid\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'staff.sales.colLoyaltyAccrued\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);">{{ \'staff.sales.colCompletedAt\' | t }}</th>' +
                        '<th style="position:sticky;top:0;z-index:2;background:var(--clr-surface-sunken);text-align:right;">{{ \'common.receipt\' | t }}</th>' +
                      '</tr>' +
                    '</thead>' +
                    '<tbody>' +
                      '<tr class="stagger-item" ng-repeat="ord in $ctrl.salesOrders track by ord.id" style="--i:{{ $index }};">' +
                        '<td class="mono-num tabular-nums" style="font-weight:700;">{{ ord.orderNumber }}</td>' +
                        '<td>' +
                          '<div class="text-primary" style="font-weight:600;">{{ ord.customerFullName || (\'common.customer\' | t) }}</div>' +
                          '<div class="text-muted" style="font-size:12px;">{{ ord.customerEmail }}</div>' +
                        '</td>' +
                        '<td>' +
                          '<div style="display:flex;align-items:center;gap:10px;">' +
                            '<div style="width:56px;flex-shrink:0;">' +
                              '<bike-image src="$ctrl.resolveUnitImage(ord)" alt="{{ ord.motorcycleModelName }}" variant="thumb"></bike-image>' +
                            '</div>' +
                            '<div>' +
                              '<div class="mono-num tabular-nums">{{ ord.vin }}</div>' +
                              '<div class="text-muted" style="font-size:12px;">{{ ord.motorcycleModelName }}</div>' +
                            '</div>' +
                          '</div>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums" style="text-align:right;font-weight:700;">{{ ord.finalAmount | vndCurrency }}</td>' +
                        '<td style="text-align:right;">' +
                          '<span class="badge badge-success mono-num tabular-nums">+{{ ord.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</span>' +
                        '</td>' +
                        '<td class="mono-num tabular-nums">{{ ord.completedAtUtc | utcToLocal }}</td>' +
                        '<td style="text-align:right;">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.SalesOrders.ViewDealer" ' +
                                  'ng-click="$ctrl.openOrderReceipt(ord)">' +
                            '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                            '<span>{{ \'staff.sales.printReceiptCta\' | t }}</span>' +
                          '</button>' +
                        '</td>' +
                      '</tr>' +
                    '</tbody>' +
                  '</table>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* MODAL 1: Intake New VIN Stock (POST /api/v1/inventories) */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="intakeVinTitle" ng-if="$ctrl.intakeModalOpen">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">POST /api/v1/inventories • {{ $ctrl.dealerCode }}</span>' +
                  '<h2 id="intakeVinTitle" class="surface-card-title" style="margin-top:4px;">{{ \'staff.sales.intakeModalTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeIntakeModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.intakeForm" ng-submit="$ctrl.submitIntakeVin()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.intakeProblem">' +
                    '<div><strong>{{ $ctrl.intakeProblem.title }}:</strong> {{ $ctrl.intakeProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.intakeProblem.traceId">traceId: {{ $ctrl.intakeProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="intakeModelSelect">{{ \'staff.sales.selectModelLabel\' | t }}</label>' +
                    '<select id="intakeModelSelect" name="motorcycleModelId" class="form-control" ' +
                            'ng-model="$ctrl.intakeDraft.motorcycleModelId" ' +
                            'ng-options="m.id as (m.brand + \' \' + m.modelName + \' (\' + (m.baseRetailPrice | vndCurrency) + \')\') for m in $ctrl.catalogModels" ' +
                            'ng-change="$ctrl.onIntakeModelChanged()" required>' +
                      '<option value="">{{ \'staff.sales.selectModelPlaceholder\' | t }}</option>' +
                    '</select>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="intakeVinInput">{{ \'staff.sales.vinFieldLabel\' | t }}</label>' +
                    '<div class="vin-field-wrapper">' +
                      '<input id="intakeVinInput" name="vin" type="text" class="form-control" style="flex:1;" ' +
                             'vin-input ng-model="$ctrl.intakeDraft.vin" ' +
                             'ng-change="$ctrl.onIntakeFieldEdited(\'vin\')" required ng-attr-placeholder="{{ \'common.vinPlaceholder\' | t }}" />' +
                      '<span class="vin-counter-pill">{{ ($ctrl.intakeDraft.vin || \'\').length }}/17</span>' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.intakeForm.vin.$dirty && $ctrl.intakeForm.vin.$error.noForbiddenVinChars">{{ \'staff.sales.vinForbiddenError\' | t }}</div>' +
                    '<div class="field-error" ng-if="$ctrl.intakeForm.vin.$dirty && !$ctrl.intakeForm.vin.$error.noForbiddenVinChars && $ctrl.intakeForm.vin.$error.isoVin">{{ \'staff.sales.vinFormatError\' | t }}</div>' +
                    '<div class="field-error" ng-if="$ctrl.intakeFieldErrors.vin">{{ $ctrl.intakeFieldErrors.vin }}</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="intakeEngineInput">{{ \'staff.sales.engineLabel\' | t }}</label>' +
                      '<input id="intakeEngineInput" name="engineNumber" type="text" maxlength="30" class="form-control mono-num" ' +
                             'ng-model="$ctrl.intakeDraft.engineNumber" ng-pattern="/^[A-Za-z0-9-]{4,30}$/" ' +
                             'ng-change="$ctrl.onIntakeFieldEdited(\'engineNumber\')" required ng-attr-placeholder="{{ \'staff.sales.enginePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.intakeFieldErrors.engineNumber">{{ $ctrl.intakeFieldErrors.engineNumber }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="intakeColorInput">{{ \'staff.sales.colorLabel\' | t }}</label>' +
                      '<input id="intakeColorInput" name="colorName" type="text" maxlength="60" class="form-control" ' +
                             'ng-model="$ctrl.intakeDraft.colorName" ' +
                             'ng-change="$ctrl.onIntakeFieldEdited(\'colorName\')" required ng-attr-placeholder="{{ \'staff.sales.colorPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.intakeFieldErrors.colorName">{{ $ctrl.intakeFieldErrors.colorName }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group" style="margin-bottom:0;">' +
                      '<label class="form-label" for="intakeYearInput">{{ \'staff.sales.yearLabel\' | t }}</label>' +
                      '<input id="intakeYearInput" name="manufactureYear" type="number" min="2000" max="2100" class="form-control mono-num" ' +
                             'ng-model="$ctrl.intakeDraft.manufactureYear" required />' +
                    '</div>' +

                    '<div class="col-8 form-group" style="margin-bottom:0;">' +
                      '<label class="form-label" for="intakePriceInput">{{ \'staff.sales.listedPriceLabel\' | t }}</label>' +
                      '<input id="intakePriceInput" name="listedPrice" type="number" min="1000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.intakeDraft.listedPrice" ' +
                             'ng-change="$ctrl.onIntakeFieldEdited(\'listedPrice\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.intakeFieldErrors.listedPrice">{{ $ctrl.intakeFieldErrors.listedPrice }}</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeIntakeModal()" ng-disabled="$ctrl.isSavingIntake">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ng-disabled="$ctrl.isSavingIntake || $ctrl.intakeForm.$invalid">' +
                    '<app-icon name="{{ $ctrl.isSavingIntake ? \'circle-notch\' : \'plus\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSavingIntake ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '<span>{{ $ctrl.isSavingIntake ? (\'staff.sales.addingVinCta\' | t) : (\'staff.sales.addVinSubmitCta\' | t) }}</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* MODAL 2: Showroom SalesOrder Checkout & Confirmation Receipt */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="checkoutTitle" ng-if="$ctrl.checkoutUnit">' +
            '<div class="modal-dialog modal-dialog-wide motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">POST /api/v1/sales-orders • ACID</span>' +
                  '<h2 id="checkoutTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ $ctrl.completedReceipt ? (\'staff.sales.checkoutTitleCompleted\' | t:{ orderNumber: $ctrl.completedReceipt.orderNumber }) : (\'staff.sales.checkoutTitleActive\' | t:{ vin: $ctrl.checkoutUnit.vin }) }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCheckoutModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              /* Confirmation Receipt State */
              '<div class="modal-body" ng-if="$ctrl.completedReceipt">' +
                '<div class="alert-banner alert-success">' +
                  '<div style="display:flex;align-items:center;gap:8px;font-weight:700;">' +
                    '<app-icon name="check-circle" weight="fill" size="18"></app-icon>' +
                    '<span>{{ \'staff.sales.saleCommittedHeadline\' | t }}</span>' +
                  '</div>' +
                  '<div style="margin-top:4px;">{{ \'staff.sales.saleCommittedDesc\' | t:{ vin: $ctrl.completedReceipt.vin } }}</div>' +
                '</div>' +
                '<div class="grid-12">' +
                  '<div class="col-6 bezel-card">' +
                    '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                      '<div class="kpi-label">{{ \'staff.sales.receiptKpiOrder\' | t }}</div>' +
                      '<div class="kpi-value mono-num" style="font-size:18px;">{{ $ctrl.completedReceipt.orderNumber }}</div>' +
                      '<div class="kpi-meta mono-num">' +
                        '{{ \'staff.sales.receiptKpiPaid\' | t:{ amount: ($ctrl.completedReceipt.finalAmount | vndCurrency), method: ($ctrl.completedReceipt.paymentMethod | enumT:\'PaymentMethod\') } }}' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                  '<div class="col-6 bezel-card">' +
                    '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                      '<div class="kpi-label">{{ \'staff.sales.receiptKpiLoyalty\' | t }}</div>' +
                      '<div class="kpi-value mono-num" style="font-size:18px;color:var(--clr-success-fg);">+{{ $ctrl.completedReceipt.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</div>' +
                      '<div class="kpi-meta">' +
                        '{{ \'common.tier\' | t }}: <strong>{{ $ctrl.completedReceipt.newTierCode | enumT:\'LoyaltyTierCode\' }}</strong> ' +
                        '<span ng-if="$ctrl.completedReceipt.tierUpgraded">{{ \'staff.sales.upgradedTag\' | t }}</span> • ' +
                        '{{ \'common.vin\' | t }} <span class="mono-num">{{ $ctrl.completedReceipt.vin }}</span>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer" ng-if="$ctrl.completedReceipt">' +
                '<button type="button" class="btn btn-secondary" ' +
                        'has-permission="Permissions.SalesOrders.ViewDealer" ' +
                        'ng-click="$ctrl.openOrderReceipt($ctrl.completedReceipt)">' +
                  '<app-icon name="printer" weight="regular" size="15"></app-icon>' +
                  '<span>{{ \'staff.sales.printReceiptCta\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-primary" ng-click="$ctrl.closeCheckoutModal()">' +
                  '{{ \'staff.sales.doneReturnCta\' | t }}' +
                '</button>' +
              '</div>' +

              /* Checkout Form State */
              '<form name="$ctrl.checkoutForm" ng-if="!$ctrl.completedReceipt" ng-submit="$ctrl.submitSalesOrder()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.checkoutProblem">' +
                    '<div><strong>{{ $ctrl.checkoutProblem.title }} ({{ $ctrl.checkoutProblem.errorCode }}):</strong> {{ $ctrl.checkoutProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.checkoutProblem.traceId">traceId: {{ $ctrl.checkoutProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-7">' +
                      '<div class="form-group" ng-if="$ctrl.customerDirectory.length > 0">' +
                        '<label class="form-label" for="buyerPresetSelect">{{ \'staff.sales.recentBuyersLabel\' | t }}</label>' +
                        '<select id="buyerPresetSelect" class="form-control" ' +
                                'ng-model="$ctrl.selectedDirectoryCustomerId" ' +
                                'ng-options="c.customerId as (c.fullName + \' • \' + c.email + (c.tierCode ? (\' (\' + (c.tierCode | enumT:\'LoyaltyTierCode\') + \')\') : \'\')) for c in $ctrl.customerDirectory" ' +
                                'ng-change="$ctrl.onCustomerPresetPicked()">' +
                          '<option value="">{{ \'staff.sales.recentBuyersPlaceholder\' | t }}</option>' +
                        '</select>' +
                      '</div>' +

                      '<div class="form-group">' +
                        '<label class="form-label" for="buyerEmailInput">{{ \'staff.sales.buyerEmailLabel\' | t }}</label>' +
                        '<div class="input-action-group">' +
                          '<input id="buyerEmailInput" name="customerEmail" type="text" class="form-control" ' +
                                 'ng-model="$ctrl.checkoutDraft.customerEmail" ' +
                                 'ng-change="$ctrl.onBuyerEmailChanged()" required ng-attr-placeholder="{{ \'staff.sales.buyerEmailPlaceholder\' | t }}" />' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'ng-click="$ctrl.lookupBuyerLoyalty()" ' +
                                  'ng-disabled="$ctrl.isLookingUpBuyer || !$ctrl.checkoutDraft.customerEmail">' +
                            '<app-icon name="{{ $ctrl.isLookingUpBuyer ? \'circle-notch\' : \'magnifying-glass\' }}" weight="regular" size="14" extra-class="{{ $ctrl.isLookingUpBuyer ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                            '<span>{{ $ctrl.isLookingUpBuyer ? (\'staff.sales.lookingUpCta\' | t) : (\'staff.sales.lookupLoyaltyCta\' | t) }}</span>' +
                          '</button>' +
                        '</div>' +
                      '</div>' +

                      '<div class="grid-12">' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="saleDiscountInput">{{ \'staff.sales.promoDiscountLabel\' | t }}</label>' +
                          '<input id="saleDiscountInput" name="discountAmount" type="number" min="0" max="{{ $ctrl.checkoutUnit.listedPrice }}" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.checkoutDraft.discountAmount" ng-change="$ctrl.recalculateSalePreview()" />' +
                        '</div>' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="salePaymentSelect">{{ \'common.paymentMethod\' | t }} *</label>' +
                          '<select id="salePaymentSelect" class="form-control" ' +
                                  'ng-model="$ctrl.checkoutDraft.paymentMethod" ' +
                                  'ng-options="pm.value as (pm.value | enumT:\'PaymentMethod\') for pm in $ctrl.paymentMethods"></select>' +
                        '</div>' +
                        '<div class="col-6 form-group" style="margin-bottom:0;">' +
                          '<label class="form-label" for="salePlateInput">{{ \'staff.sales.licensePlateOptLabel\' | t }}</label>' +
                          '<input id="salePlateInput" type="text" maxlength="20" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.checkoutDraft.licensePlate" ng-attr-placeholder="{{ \'common.licensePlatePlaceholder\' | t }}" />' +
                        '</div>' +
                        '<div class="col-6 form-group" style="margin-bottom:0;">' +
                          '<label class="form-label" for="saleVoucherInput">{{ \'staff.sales.voucherOptLabel\' | t }}</label>' +
                          '<input id="saleVoucherInput" type="text" maxlength="32" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.checkoutDraft.voucherCode" ng-attr-placeholder="{{ \'common.voucherPlaceholder\' | t }}" />' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    /* Double-Bezel Sale Summary + VIN Lock Badge + Model Thumbnail */
                    '<div class="col-5 bezel-card">' +
                      '<div class="bezel-card-inner" style="padding:14px;font-size:13px;">' +
                        '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;margin-bottom:10px;">' +
                          '<strong class="text-primary">{{ \'staff.sales.previewTitle\' | t }}</strong>' +
                          '<span class="badge badge-info mono-num">' +
                            '<app-icon name="lock" weight="fill" size="11"></app-icon>' +
                            '<span>{{ \'staff.sales.vinLockBadge\' | t }}</span>' +
                          '</span>' +
                        '</div>' +
                        '<div style="display:flex;gap:10px;align-items:center;margin-bottom:10px;">' +
                          '<div style="width:76px;flex-shrink:0;">' +
                            '<bike-image src="$ctrl.resolveUnitImage($ctrl.checkoutUnit)" alt="{{ $ctrl.checkoutUnit.motorcycleModelName }}" variant="thumb"></bike-image>' +
                          '</div>' +
                          '<div style="min-width:0;">' +
                            '<div class="text-primary" style="font-weight:700;">{{ $ctrl.checkoutUnit.motorcycleModelName }}</div>' +
                            '<div class="mono-num text-secondary" style="font-size:12px;margin-top:2px;">{{ $ctrl.checkoutUnit.vin }}</div>' +
                          '</div>' +
                        '</div>' +
                        '<hr style="border:0;border-top:1px solid var(--clr-border-subtle);margin:10px 0;" />' +
                        '<div style="display:flex;justify-content:space-between;">' +
                          '<span class="text-secondary">{{ \'staff.sales.colListedPrice\' | t }}</span>' +
                          '<span class="mono-num tabular-nums">{{ $ctrl.checkoutUnit.listedPrice | vndCurrency }}</span>' +
                        '</div>' +
                        '<div style="display:flex;justify-content:space-between;margin-top:4px;" class="text-muted">' +
                          '<span>{{ \'common.discount\' | t }}</span>' +
                          '<span class="mono-num tabular-nums">-{{ $ctrl.salePreview.discountAmount | vndCurrency }}</span>' +
                        '</div>' +
                        '<div style="display:flex;justify-content:space-between;font-weight:700;font-size:15px;margin-top:8px;" class="text-primary">' +
                          '<span>{{ \'staff.sales.colFinalPaid\' | t }}</span>' +
                          '<span class="mono-num tabular-nums">{{ $ctrl.salePreview.finalAmount | vndCurrency }}</span>' +
                        '</div>' +
                        '<hr style="border:0;border-top:1px solid var(--clr-border-subtle);margin:10px 0;" />' +
                        '<div class="text-secondary">{{ \'staff.sales.buyerRateLabel\' | t }}: <strong class="text-primary">{{ $ctrl.salePreview.buyerTierCode | enumT:\'LoyaltyTierCode\' }} ({{ $ctrl.salePreview.buyerMultiplier }}x)</strong></div>' +
                        '<div style="color:var(--clr-success-fg);font-weight:700;margin-top:4px;">{{ \'staff.estimatedPointsLabel\' | t }}: <span class="mono-num tabular-nums">+{{ $ctrl.salePreview.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</span></div>' +
                        '<div style="margin-top:4px;font-size:12px;" class="text-secondary">{{ \'staff.sales.postSaleTierLabel\' | t }}: <strong class="text-primary">{{ $ctrl.salePreview.projectedTierCode | enumT:\'LoyaltyTierCode\' }}</strong></div>' +
                        '<div style="margin-top:8px;font-size:11px;" class="text-muted">{{ \'staff.sales.autoOnboardNote\' | t }}</div>' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeCheckoutModal()" ng-disabled="$ctrl.isSubmittingSale">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn-pill-cta" ' +
                          'has-permission="Permissions.SalesOrders.Create" ' +
                          'ng-disabled="$ctrl.isSubmittingSale || !$ctrl.canSubmitSale()">' +
                    '<span>{{ $ctrl.isSubmittingSale ? (\'staff.sales.committingSaleCta\' | t) : (\'staff.completeSaleCta\' | t) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="{{ $ctrl.isSubmittingSale ? \'circle-notch\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSubmittingSale ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          '<printable-receipt-modal receipt="$ctrl.activeReceipt" receipt-kind="$ctrl.activeReceiptKind" on-close="$ctrl.closeReceipt()"></printable-receipt-modal>' +
        '</section>'
    });

  StaffSalesDeskController.$inject = [
    '$q',
    'AuthService',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function StaffSalesDeskController(
    $q,
    AuthService,
    DealerApiService,
    LoyaltyCalculator,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var modelImageById = Object.create(null);
    var modelImageByName = Object.create(null);

    function t(key, params) {
      return (I18nService && angular.isFunction(I18nService.t)) ? I18nService.t(key, params) : key;
    }

    function enumT(group, code) {
      return (I18nService && angular.isFunction(I18nService.enumLabel))
        ? I18nService.enumLabel(group, code)
        : String(code || '');
    }

    function recomputeStockStats() {
      var list = $ctrl.inventories || [];
      var avail = 0;
      var reserved = 0;
      var sold = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].stockStatus === 'Available') {
          avail++;
        } else if (list[i].stockStatus === 'Reserved') {
          reserved++;
        } else if (list[i].stockStatus === 'Sold') {
          sold++;
        }
      }
      $ctrl.stockStats = {
        available: avail,
        reserved: reserved,
        sold: sold
      };
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

    function mergeLiveCustomerDirectory(vehicles, orders, bookings) {
      var byKey = Object.create(null);
      var out = [];

      function addCustomer(cid, email, fullName, tierCode, tierMultiplier, lifetimePts) {
        var normId = cid ? String(cid).trim() : '';
        var normEmail = email ? String(email).trim() : '';
        if (!normId && !normEmail) {
          return;
        }
        var key = normId ? normId.toLowerCase() : normEmail.toLowerCase();
        if (byKey[key]) {
          if (!byKey[key].email && normEmail) { byKey[key].email = normEmail; }
          if (!byKey[key].fullName && fullName) { byKey[key].fullName = fullName; }
          if (tierCode && !byKey[key].tierCode) { byKey[key].tierCode = tierCode; }
          if (tierMultiplier && byKey[key].tierMultiplier <= 1.0) { byKey[key].tierMultiplier = Number(tierMultiplier) || 1.0; }
          return;
        }
        var rec = {
          customerId: normId || normEmail,
          fullName: fullName || normEmail || t('common.customer'),
          email: normEmail,
          tierCode: tierCode || '',
          tierMultiplier: Number(tierMultiplier) || 1.0,
          lifetimeQualifyingPoints: Number(lifetimePts) || 0
        };
        byKey[key] = rec;
        out.push(rec);
      }

      var vList = angular.isArray(vehicles) ? vehicles : [];
      for (var i = 0; i < vList.length; i++) {
        addCustomer(vList[i].customerId, vList[i].customerEmail, vList[i].customerFullName, vList[i].tierCode, vList[i].tierMultiplier, 0);
      }
      var oList = angular.isArray(orders) ? orders : [];
      for (var j = 0; j < oList.length; j++) {
        addCustomer(oList[j].customerId, oList[j].customerEmail, oList[j].customerFullName, oList[j].newTierCode || '', 1.0, 0);
      }
      var bList = angular.isArray(bookings) ? bookings : [];
      for (var k = 0; k < bList.length; k++) {
        addCustomer(bList[k].customerId, bList[k].customerEmail, bList[k].customerFullName, '', 1.0, 0);
      }
      $ctrl.customerDirectory = out;
    }

    $ctrl.formatStockStatusOption = function (opt) {
      if (!opt || !opt.value) {
        return t('staff.sales.allStatusesOption');
      }
      return enumT('StockStatus', opt.value);
    };

    $ctrl.resolveUnitImage = function (unit) {
      if (!unit) { return ''; }
      if (unit.imageUrl) { return unit.imageUrl; }
      if (unit.motorcycleModelId && modelImageById[unit.motorcycleModelId]) {
        return modelImageById[unit.motorcycleModelId];
      }
      if (unit.motorcycleModelName) {
        var nameKey = String(unit.motorcycleModelName).trim().toLowerCase();
        if (modelImageByName[nameKey]) {
          return modelImageByName[nameKey];
        }
      }
      return '';
    };

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.dealerCode = user.dealerCode || '';
      $ctrl.dealerName = user.dealerName || t('staff.sales.defaultDealerName');

      $ctrl.activeTab = 'INVENTORY';
      $ctrl.isLoading = false;
      $ctrl.isLookingUpBuyer = false;
      $ctrl.busyInventoryId = null;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      $ctrl.stockStatusOptions = [
        { value: '' },
        { value: 'Available' },
        { value: 'Reserved' },
        { value: 'Sold' }
      ];

      $ctrl.paymentMethods = [
        { value: 'BankTransfer' },
        { value: 'Cash' },
        { value: 'Card' },
        { value: 'Installment' }
      ];

      $ctrl.filters = {
        search: '',
        stockStatus: ''
      };

      $ctrl.catalogModels = [];
      $ctrl.customerDirectory = [];
      $ctrl.stockStats = { available: 0, reserved: 0, sold: 0 };

      $ctrl.intakeModalOpen = false;
      $ctrl.isSavingIntake = false;
      $ctrl.intakeProblem = null;
      $ctrl.intakeFieldErrors = {};
      $ctrl.intakeDraft = {
        motorcycleModelId: '',
        vin: '',
        engineNumber: '',
        colorName: '',
        manufactureYear: new Date().getFullYear(),
        listedPrice: 0
      };

      $ctrl.checkoutUnit = null;
      $ctrl.selectedDirectoryCustomerId = '';
      $ctrl.inspectedBuyerLoyalty = null;
      $ctrl.isSubmittingSale = false;
      $ctrl.checkoutProblem = null;
      $ctrl.completedReceipt = null;
      $ctrl.checkoutDraft = {};
      $ctrl.salePreview = {};
      $ctrl.activeReceipt = null;
      $ctrl.activeReceiptKind = 'SalesOrder';
      $ctrl.pendingTestDriveHandoff = (DealerApiService && angular.isFunction(DealerApiService.consumeSalesDeskHandoffContext))
        ? DealerApiService.consumeSalesDeskHandoffContext()
        : null;

      $ctrl.inventories = [];
      $ctrl.salesOrders = [];
      recomputeStockStats();
      $ctrl.applyInventoryFilters();
      $ctrl.loadSalesDeskData().then(function () {
        if (!isDestroyed && $ctrl.pendingTestDriveHandoff) {
          applyTestDriveHandoff($ctrl.pendingTestDriveHandoff);
          $ctrl.pendingTestDriveHandoff = null;
        }
      });
    };

    function applyTestDriveHandoff(ctx) {
      if (!ctx) { return; }
      var units = $ctrl.inventories || [];
      var targetUnit = null;
      if (ctx.assignedInventoryId) {
        for (var i = 0; i < units.length; i++) {
          if (units[i].id === ctx.assignedInventoryId && (units[i].stockStatus === 'Available' || units[i].stockStatus === 'Reserved')) {
            targetUnit = units[i];
            break;
          }
        }
      }
      if (!targetUnit && ctx.motorcycleModelId) {
        for (var j = 0; j < units.length; j++) {
          if (units[j].motorcycleModelId === ctx.motorcycleModelId && units[j].stockStatus === 'Available') {
            targetUnit = units[j];
            break;
          }
        }
      }
      if (!targetUnit && ctx.motorcycleModelName) {
        $ctrl.filters.search = ctx.motorcycleModelName;
        $ctrl.applyInventoryFilters();
      }
      if (targetUnit) {
        $ctrl.openCheckoutModal(targetUnit);
        if (ctx.customerId) {
          $ctrl.selectedDirectoryCustomerId = ctx.customerId;
          $ctrl.checkoutDraft.customerId = ctx.customerId;
        }
        if (ctx.customerEmail) {
          $ctrl.checkoutDraft.customerEmail = ctx.customerEmail;
        }
        if (ctx.bookingNumber) {
          $ctrl.checkoutDraft.notes = t('staff.sales.convertedNotePrefix') + ctx.bookingNumber;
        }
        $ctrl.recalculateSalePreview();
      } else if (ToastService) {
        ToastService.info(
          t('staff.sales.toastHandoffTitle'),
          t('staff.sales.toastHandoffMsg', { customer: ctx.customerFullName || t('common.customer') })
        );
      }
    }

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
    };

    $ctrl.stockBadgeClass = function (status) {
      if (status === 'Available') { return 'badge-status-available'; }
      if (status === 'Reserved') { return 'badge-warning'; }
      return 'badge-neutral';
    };

    $ctrl.loadSalesDeskData = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      var vehiclesPromise = angular.isFunction(DealerApiService.lookupVehicles)
        ? DealerApiService.lookupVehicles({ pageSize: 100 }).catch(function () { return []; })
        : $q.resolve([]);
      var bookingsPromise = angular.isFunction(DealerApiService.listBookings)
        ? DealerApiService.listBookings({ pageSize: 50 }).catch(function () { return []; })
        : $q.resolve([]);

      return $q.all({
        inventories: DealerApiService.listInventories({ pageSize: 100 }),
        orders: DealerApiService.listSalesOrders({ pageSize: 50 }).catch(function () { return []; }),
        models: DealerApiService.listMotorcycles({ pageSize: 100 }).catch(function () { return []; }),
        vehicles: vehiclesPromise,
        bookings: bookingsPromise
      }).then(function (bundle) {
        if (isDestroyed) {
          return bundle;
        }
        $ctrl.inventories = bundle.inventories || [];
        $ctrl.salesOrders = bundle.orders || [];
        if (angular.isArray(bundle.models) && bundle.models.length > 0) {
          $ctrl.catalogModels = bundle.models.filter(function (m) { return m.isActive; });
          rebuildModelImageIndex(bundle.models);
        }
        mergeLiveCustomerDirectory(bundle.vehicles, bundle.orders, bundle.bookings);
        recomputeStockStats();
        $ctrl.applyInventoryFilters();
        return bundle;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.applyInventoryFilters = function () {
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      var st = ($ctrl.filters && $ctrl.filters.stockStatus) || '';

      $ctrl.filteredInventories = ($ctrl.inventories || []).filter(function (u) {
        if (st && u.stockStatus !== st) {
          return false;
        }
        if (q) {
          var hay = (u.vin + ' ' + u.engineNumber + ' ' + u.motorcycleModelName + ' ' + u.colorName).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetInventoryFilters = function () {
      $ctrl.filters = { search: '', stockStatus: '' };
      $ctrl.applyInventoryFilters();
    };

    /* Intake VIN Modal */
    $ctrl.openIntakeModal = function () {
      var firstModel = $ctrl.catalogModels[0] || null;
      $ctrl.intakeProblem = null;
      $ctrl.intakeFieldErrors = {};
      $ctrl.intakeDraft = {
        motorcycleModelId: firstModel ? firstModel.id : '',
        vin: '',
        engineNumber: '',
        colorName: '',
        manufactureYear: new Date().getFullYear(),
        listedPrice: firstModel ? firstModel.baseRetailPrice : 0
      };
      $ctrl.intakeModalOpen = true;
    };

    $ctrl.closeIntakeModal = function () {
      $ctrl.intakeModalOpen = false;
      $ctrl.intakeProblem = null;
      $ctrl.intakeFieldErrors = {};
    };

    $ctrl.onIntakeModelChanged = function () {
      for (var i = 0; i < $ctrl.catalogModels.length; i++) {
        if ($ctrl.catalogModels[i].id === $ctrl.intakeDraft.motorcycleModelId) {
          $ctrl.intakeDraft.listedPrice = $ctrl.catalogModels[i].baseRetailPrice;
          break;
        }
      }
      $ctrl.onIntakeFieldEdited('motorcycleModelId');
    };

    $ctrl.onIntakeFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.intakeForm, $ctrl.intakeFieldErrors, fieldName);
    };

    $ctrl.submitIntakeVin = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.intakeForm);
      if (($ctrl.intakeForm && $ctrl.intakeForm.$invalid) || $ctrl.isSavingIntake) {
        return $q.resolve(null);
      }

      $ctrl.isSavingIntake = true;
      $ctrl.intakeProblem = null;
      $ctrl.intakeFieldErrors = {};

      var payload = {
        motorcycleModelId: $ctrl.intakeDraft.motorcycleModelId,
        vin: String($ctrl.intakeDraft.vin || '').trim().toUpperCase(),
        engineNumber: String($ctrl.intakeDraft.engineNumber || '').trim().toUpperCase(),
        colorName: String($ctrl.intakeDraft.colorName || '').trim(),
        manufactureYear: Number($ctrl.intakeDraft.manufactureYear) || 2026,
        listedPrice: Number($ctrl.intakeDraft.listedPrice) || 0
      };

      return DealerApiService.createInventory(payload)
        .then(function (created) {
          if (isDestroyed) { return created; }
          ToastService.success(
            t('staff.sales.toastVinAddedTitle'),
            t('staff.sales.toastVinAddedMsg', { vin: created.vin, model: created.motorcycleModelName })
          );
          $ctrl.closeIntakeModal();
          $ctrl.loadSalesDeskData();
          return created;
        })
        .catch(function (rejection) {
          if (isDestroyed) { return null; }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.intakeFieldErrors = ProblemDetailsMapper.applyToForm($ctrl.intakeForm, problem);
          $ctrl.intakeProblem = problem;
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSavingIntake = false;
          }
        });
    };

    $ctrl.toggleReserveStatus = function (unit) {
      if (!unit || $ctrl.busyInventoryId) { return $q.resolve(null); }
      $ctrl.busyInventoryId = unit.id;
      var nextStatus = unit.stockStatus === 'Available' ? 'Reserved' : 'Available';
      return DealerApiService.updateInventory(unit.id, {
        stockStatus: nextStatus,
        rowVersion: unit.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.inventories.length; i++) {
          if ($ctrl.inventories[i].id === unit.id) {
            $ctrl.inventories[i] = updated;
          }
        }
        recomputeStockStats();
        $ctrl.applyInventoryFilters();
        ToastService.info(
          t('staff.sales.toastStockUpdatedTitle'),
          t('staff.sales.toastStockUpdatedMsg', {
            vin: updated.vin,
            status: enumT('StockStatus', updated.stockStatus)
          })
        );
        return updated;
      }).catch(function (rejection) {
        var problem = ProblemDetailsMapper.extract(rejection);
        ToastService.warning(problem.title, problem.detail);
        if (problem.status === 409) {
          $ctrl.loadSalesDeskData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.busyInventoryId = null;
        }
      });
    };

    /* SalesOrder Checkout Modal */
    $ctrl.openCheckoutModal = function (unit) {
      if (!unit) { return; }
      $ctrl.checkoutUnit = unit;
      $ctrl.completedReceipt = null;
      $ctrl.checkoutProblem = null;
      $ctrl.selectedDirectoryCustomerId = '';
      $ctrl.inspectedBuyerLoyalty = null;
      $ctrl.checkoutDraft = {
        customerId: '',
        customerEmail: '',
        discountAmount: 0,
        paymentMethod: 'BankTransfer',
        licensePlate: '',
        voucherCode: '',
        notes: ''
      };
      $ctrl.recalculateSalePreview();
    };

    $ctrl.closeCheckoutModal = function () {
      $ctrl.checkoutUnit = null;
      $ctrl.completedReceipt = null;
      $ctrl.checkoutProblem = null;
      $ctrl.inspectedBuyerLoyalty = null;
    };

    $ctrl.onCustomerPresetPicked = function () {
      $ctrl.inspectedBuyerLoyalty = null;
      for (var i = 0; i < $ctrl.customerDirectory.length; i++) {
        var c = $ctrl.customerDirectory[i];
        if (c.customerId === $ctrl.selectedDirectoryCustomerId) {
          $ctrl.checkoutDraft.customerId = GUID_REGEX.test(c.customerId) ? c.customerId : '';
          $ctrl.checkoutDraft.customerEmail = c.email || c.customerId;
          $ctrl.recalculateSalePreview();
          if ($ctrl.checkoutDraft.customerId) {
            $ctrl.lookupBuyerLoyalty();
          }
          return;
        }
      }
      $ctrl.checkoutDraft.customerId = '';
      $ctrl.recalculateSalePreview();
    };

    $ctrl.onBuyerEmailChanged = function () {
      $ctrl.inspectedBuyerLoyalty = null;
      var raw = String($ctrl.checkoutDraft.customerEmail || '').trim();
      if (GUID_REGEX.test(raw)) {
        $ctrl.selectedDirectoryCustomerId = raw;
        $ctrl.checkoutDraft.customerId = raw;
        $ctrl.recalculateSalePreview();
        return;
      }
      var typed = raw.toLowerCase();
      var matchedId = '';
      for (var i = 0; i < $ctrl.customerDirectory.length; i++) {
        if ($ctrl.customerDirectory[i].email && $ctrl.customerDirectory[i].email.toLowerCase() === typed) {
          matchedId = $ctrl.customerDirectory[i].customerId;
          break;
        }
      }
      $ctrl.selectedDirectoryCustomerId = matchedId;
      $ctrl.checkoutDraft.customerId = GUID_REGEX.test(matchedId) ? matchedId : '';
      $ctrl.recalculateSalePreview();
    };

    $ctrl.lookupBuyerLoyalty = function () {
      var raw = String($ctrl.checkoutDraft.customerEmail || '').trim();
      var targetGuid = GUID_REGEX.test($ctrl.checkoutDraft.customerId || '')
        ? String($ctrl.checkoutDraft.customerId).trim()
        : (GUID_REGEX.test(raw) ? raw : '');

      if (!targetGuid && !raw) {
        return $q.resolve(null);
      }

      $ctrl.isLookingUpBuyer = true;
      function hydrateFromCustomerId(cid) {
        if (!cid || !DealerApiService.loyalty || !angular.isFunction(DealerApiService.loyalty.getCustomerSummary)) {
          return $q.resolve(null);
        }
        return DealerApiService.loyalty.getCustomerSummary(cid)
          .then(function (summary) {
            if (!isDestroyed && summary) {
              $ctrl.inspectedBuyerLoyalty = summary;
              $ctrl.checkoutDraft.customerId = summary.customerId || cid;
              $ctrl.recalculateSalePreview();
            }
            return summary;
          })
          .catch(function () { return null; });
      }

      var lookupPromise = targetGuid
        ? hydrateFromCustomerId(targetGuid)
        : (angular.isFunction(DealerApiService.lookupVehicles)
          ? DealerApiService.lookupVehicles({ email: raw })
            .then(function (rows) {
              var first = angular.isArray(rows) && rows.length > 0 ? rows[0] : null;
              if (first && first.customerId) {
                $ctrl.checkoutDraft.customerId = first.customerId;
                return hydrateFromCustomerId(first.customerId);
              }
              return null;
            })
            .catch(function () { return null; })
          : $q.resolve(null));

      return lookupPromise.finally(function () {
        if (!isDestroyed) {
          $ctrl.isLookingUpBuyer = false;
        }
      });
    };

    $ctrl.recalculateSalePreview = function () {
      if (!$ctrl.checkoutUnit) {
        return;
      }
      var listed = Number($ctrl.checkoutUnit.listedPrice) || 0;
      var discount = Math.max(0, Math.min(listed, Number($ctrl.checkoutDraft.discountAmount) || 0));
      var finalAmt = Math.max(0, listed - discount);

      var buyerTier = 'Bronze';
      var buyerMult = 1.0;
      var buyerLifetime = 0;
      if ($ctrl.inspectedBuyerLoyalty) {
        buyerTier = $ctrl.inspectedBuyerLoyalty.currentTierCode || 'Bronze';
        buyerMult = Number($ctrl.inspectedBuyerLoyalty.pointsMultiplier) || 1.0;
        buyerLifetime = Number($ctrl.inspectedBuyerLoyalty.lifetimeQualifyingPoints) || 0;
      } else {
        for (var i = 0; i < $ctrl.customerDirectory.length; i++) {
          if ($ctrl.customerDirectory[i].customerId === $ctrl.selectedDirectoryCustomerId) {
            buyerTier = $ctrl.customerDirectory[i].tierCode || 'Bronze';
            buyerMult = Number($ctrl.customerDirectory[i].tierMultiplier) || 1.0;
            buyerLifetime = Number($ctrl.customerDirectory[i].lifetimeQualifyingPoints) || 0;
            break;
          }
        }
      }

      var pts = LoyaltyCalculator.calculatePoints(finalAmt, buyerMult);
      var projectedTier = LoyaltyCalculator.determineTier(buyerLifetime + pts);
      $ctrl.salePreview = {
        discountAmount: discount,
        finalAmount: finalAmt,
        buyerTierCode: buyerTier,
        buyerMultiplier: buyerMult,
        pointsEarned: pts,
        projectedTierCode: projectedTier.code,
        projectedTierMultiplier: projectedTier.multiplier
      };
    };

    $ctrl.canSubmitSale = function () {
      if (!$ctrl.checkoutUnit) { return false; }
      var hasBuyer = Boolean(
        ($ctrl.checkoutDraft.customerId && String($ctrl.checkoutDraft.customerId).trim()) ||
        ($ctrl.checkoutDraft.customerEmail && String($ctrl.checkoutDraft.customerEmail).trim())
      );
      var discount = Number($ctrl.checkoutDraft.discountAmount) || 0;
      return hasBuyer && discount >= 0 && discount <= $ctrl.checkoutUnit.listedPrice;
    };

    $ctrl.submitSalesOrder = function () {
      if (!$ctrl.canSubmitSale() || $ctrl.isSubmittingSale) {
        return $q.resolve(null);
      }
      $ctrl.isSubmittingSale = true;
      $ctrl.checkoutProblem = null;

      var rawEmailOrGuid = String($ctrl.checkoutDraft.customerEmail || '').trim();
      var typedGuid = GUID_REGEX.test(rawEmailOrGuid) ? rawEmailOrGuid : '';
      var resolvedCustomerId = typedGuid || (GUID_REGEX.test($ctrl.checkoutDraft.customerId || '') ? String($ctrl.checkoutDraft.customerId).trim() : null);
      var resolvedCustomerEmail = typedGuid ? null : (rawEmailOrGuid || null);

      return DealerApiService.createSalesOrder({
        customerId: resolvedCustomerId,
        customerEmail: resolvedCustomerEmail,
        dealerInventoryId: $ctrl.checkoutUnit.id,
        inventoryRowVersion: $ctrl.checkoutUnit.rowVersion,
        discountAmount: Number($ctrl.checkoutDraft.discountAmount) || 0,
        paymentMethod: $ctrl.checkoutDraft.paymentMethod,
        licensePlate: $ctrl.checkoutDraft.licensePlate || null,
        voucherCode: $ctrl.checkoutDraft.voucherCode || null,
        notes: $ctrl.checkoutDraft.notes || null
      }).then(function (receipt) {
        if (isDestroyed) { return receipt; }
        $ctrl.completedReceipt = receipt;
        ToastService.success(
          t('staff.sales.toastSaleCompletedTitle'),
          t('staff.sales.toastSaleCompletedMsg', {
            orderNumber: receipt.orderNumber,
            vin: receipt.vin,
            pts: receipt.pointsEarned
          })
        );
        $ctrl.loadSalesDeskData();
        return receipt;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.checkoutProblem = problem;
        if (problem.status === 409) {
          $ctrl.loadSalesDeskData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSubmittingSale = false;
        }
      });
    };

    $ctrl.openOrderReceipt = function (order) {
      if (!order) {
        return $q.resolve(null);
      }
      $ctrl.activeReceiptKind = 'SalesOrder';
      var orderId = order.id || order.salesOrderId || '';
      if (!orderId || !DealerApiService || !angular.isFunction(DealerApiService.getSalesOrderReceipt)) {
        ToastService.warning(t('staff.sales.toastReceiptUnavailableTitle'), t('staff.sales.toastReceiptUnavailableMsg'));
        return $q.resolve(null);
      }
      return DealerApiService.getSalesOrderReceipt(orderId)
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

    $ctrl.closeReceipt = function () {
      $ctrl.activeReceipt = null;
    };
  }
})();
