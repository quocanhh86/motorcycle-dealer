(function () {
  'use strict';

  var ANALYTICS_PALETTE = Object.freeze({
    RacingRed: '#DC2626',
    RacingRedFill: 'rgba(220, 38, 38, 0.14)',
    WorkshopBlue: '#2563EB',
    CompletedGreen: '#16A34A',
    SlateInkLight: '#0F172A',
    CyanInkDark: '#38BDF8',
    BookingStatuses: Object.freeze([
      '#16A34A',
      '#2563EB',
      '#0891B2',
      '#7C3AED',
      '#D97706',
      '#64748B',
      '#DC2626'
    ]),
    LoyaltyTiers: Object.freeze([
      '#B45309',
      '#64748B',
      '#CA8A04',
      '#0284C7'
    ])
  });

  var I18N_PATCH_VI = {
    admin: {
      analytics: {
        screenCode: 'SCR-ADM-01 · FR-07 / MOD-06 / RISK-04',
        liveActivityCta: 'Có dữ liệu trực tiếp mới — Cập nhật biểu đồ',
        refreshing: 'Đang tải lại...',
        exportingCsv: 'Đang xuất CSV...',
        exportCsvCta: 'Xuất báo cáo CSV',
        windowPresetLabel: 'Khung thời gian:',
        presetLast6m: '6 tháng qua',
        presetLast12m: '12 tháng qua',
        presetYtd: 'Từ đầu năm (YTD)',
        presetAll2026: 'Cả năm 2026',
        fromDateLabel: 'Từ ngày (VN)',
        toDateLabel: 'Đến ngày (VN)',
        dealerScopeLabel: 'Phạm vi chi nhánh đại lý',
        applyFilters: 'Áp dụng lọc',
        resetFilters: 'Đặt lại',
        quickDealers: 'Đại lý',
        quickStaff: 'Nhân viên',
        quickModels: 'Mẫu xe & Quy chuẩn',
        emptyWindowMessage: 'Không có đơn bán xe showroom hoặc lịch hẹn bảo dưỡng nào trong khoảng thời gian đã chọn. Các trục chi nhánh và tháng được điền giá trị 0 bên dưới.',
        kpiBookingsFunnel: 'Lượt hẹn & Tỷ lệ hoàn tất',
        kpiMetaCombined: '{{dealers}} đại lý hoạt động · {{units}} xe VIN + {{invoices}} hóa đơn',
        kpiMetaShowroom: '{{units}} đơn giao xe VIN hoàn tất',
        kpiMetaWorkshop: '{{invoices}} hóa đơn dịch vụ đã thanh toán',
        kpiMetaBookings: '{{completion}}% hoàn tất · {{attrition}}% hủy / vắng mặt',
        liabilityTitle: 'Nghĩa vụ điểm thưởng & Chiết khấu Voucher',
        liabilityBadge: 'NGHĨA VỤ ĐIỂM THƯỞNG',
        unredeemedPool: 'Tổng điểm chưa đổi',
        impliedExposure: 'Giá trị quy đổi dự kiến',
        parityHint: '(quy đổi 500 ₫/điểm)',
        activeMembers: 'Hội viên đang hoạt động',
        discountsAbsorbed: 'Tổng chiết khấu voucher',
        linkLoyaltyTiers: 'Hạng thành viên',
        linkVoucherCatalog: 'Kho Voucher',
        csatWarrantyBadge: 'CHẤT LƯỢNG CSAT & BẢO HÀNH OEM',
        csatWarrantyTitle: 'Đánh giá CSAT 5 sao & Chi phí bảo hành toàn quốc',
        networkCsat: 'Điểm CSAT toàn hệ thống',
        reviewsCount: '{{count}} đánh giá',
        openWarrantyClaims: 'Hồ sơ bảo hành đang mở',
        approvedWarrantyCost: 'Chi phí bảo hành đã duyệt',
        linkQualityOversight: 'Giám sát Chất lượng & Bảo hành',
        chart1Subtitle: 'Tổng hợp SalesOrders.FinalAmount + ServiceInvoices.NetPaidAmount',
        chart1Badge: 'Cột chồng',
        chart2TitleBar: 'Phễu lịch bảo dưỡng theo chi nhánh',
        chart2TitleDoughnut: 'Cơ cấu 7 trạng thái lịch hẹn toàn quốc',
        chart2SubBar: 'So sánh Hoàn tất, Đang xử lý và Hủy / Không đến theo đại lý',
        chart2SubDoughnut: 'Phân bổ vòng đời 7 trạng thái đặt lịch xưởng',
        chart2ToggleDealer: 'Theo đại lý',
        chart2ToggleStatus: 'Tỷ trọng trạng thái',
        chart3Subtitle: 'Chuỗi tháng liên tục YYYY-MM (trục trái: Doanh thu · trục phải: Lượt hẹn)',
        chart3Badge: 'Đường 2 trục',
        chart4Subtitle: 'Phân bổ hội viên theo 4 hạng Đồng · Bạc · Vàng · Bạch kim',
        chart4Badge: 'Vòng tròn 4 hạng',
        dsShowroomSales: 'Doanh thu bán xe Showroom (VNĐ)',
        dsWorkshopService: 'Doanh thu hóa đơn Xưởng (VNĐ)',
        dsServiceBookings: 'Lượt đặt lịch bảo dưỡng',
        dsCompleted: 'Hoàn tất',
        dsActive: 'Đang xử lý',
        dsCancelledNoShow: 'Đã hủy / Không đến',
        dsMonthlyRevenue: 'Tổng doanh thu tháng (VNĐ)',
        dsMonthlyBookings: 'Số lịch hẹn trong tháng',
        dsLoyaltyMembers: 'Số lượng hội viên',
        axisRevenue: 'Doanh thu (VNĐ)',
        axisBookings: 'Số lượt hẹn',
        tableTitle: 'Bảng Hiệu suất & Quản trị Chi tiết theo Đại lý',
        tableSubtitle: 'Nhấn tiêu đề cột để sắp xếp chi nhánh; truy cập nhanh quản trị chi nhánh, nhân sự hoặc chất lượng.',
        branchCountBadge: '{{count}} chi nhánh',
        colCode: 'Mã chi nhánh',
        colDealerBranch: 'Chi nhánh đại lý',
        colCity: 'Thành phố',
        colUnitsSold: 'Xe đã bán',
        colShowroomRev: 'Doanh thu bán xe',
        colInvoices: 'Hóa đơn',
        colServiceRev: 'Doanh thu xưởng',
        colTotalRev: 'Tổng doanh thu',
        colBookings: 'Lịch hẹn',
        colCompletionPct: 'Tỷ lệ hoàn tất',
        colCsatWarranty: 'CSAT & Bảo hành',
        colGovernance: 'Quản trị',
        doneSuffix: 'hoàn tất',
        attritionSuffix: 'hủy/vắng',
        openClaimsSuffix: 'hồ sơ mở',
        actionManageBranch: 'Quản lý chi nhánh',
        actionViewStaff: 'Nhân sự',
        actionQuality: 'Chất lượng',
        emptyTableTitle: 'Không có chi nhánh phù hợp bộ lọc',
        emptyTableDesc: 'Hãy đặt lại phạm vi đại lý hoặc khoảng thời gian để xem toàn bộ báo cáo toàn quốc.',
        dateInvertedError: 'Ngày bắt đầu ({{from}}) phải nhỏ hơn hoặc bằng ngày kết thúc ({{to}}).',
        dateSpanError: 'Khoảng thời gian phân tích không được vượt quá 731 ngày (24 tháng).',
        invalidDateToastTitle: 'Bộ lọc ngày không hợp lệ',
        csvExportedTitle: 'Xuất báo cáo CSV thành công',
        csvExportedMessage: 'Đã tải xuống tệp {{filename}}.',
        csvExportFailedTitle: 'Xuất báo cáo CSV thất bại'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      analytics: {
        screenCode: 'SCR-ADM-01 · FR-07 / MOD-06 / RISK-04',
        liveActivityCta: 'New Live Activity — Refresh Charts',
        refreshing: 'Refreshing...',
        exportingCsv: 'Exporting CSV...',
        exportCsvCta: 'Export CSV Report',
        windowPresetLabel: 'Window Preset:',
        presetLast6m: 'Last 6M',
        presetLast12m: 'Last 12M',
        presetYtd: 'YTD',
        presetAll2026: 'All 2026',
        fromDateLabel: 'From Date (VN)',
        toDateLabel: 'To Date (VN)',
        dealerScopeLabel: 'Dealership Branch Scope',
        applyFilters: 'Apply Filters',
        resetFilters: 'Reset',
        quickDealers: 'Dealers',
        quickStaff: 'Staff',
        quickModels: 'Models & Rules',
        emptyWindowMessage: 'No completed showroom sales or workshop bookings occurred within the selected filter window. Zero-filled dealer and calendar axes are displayed below.',
        kpiBookingsFunnel: 'Service Bookings & Funnel',
        kpiMetaCombined: '{{dealers}} Active Dealers · {{units}} VINs + {{invoices}} Invoices',
        kpiMetaShowroom: '{{units}} VINs Completed',
        kpiMetaWorkshop: '{{invoices}} Settled Invoices',
        kpiMetaBookings: '{{completion}}% completion · {{attrition}}% attrition',
        liabilityTitle: 'Loyalty Points Liability & Voucher Exposure',
        liabilityBadge: 'LOYALTY LIABILITY',
        unredeemedPool: 'Unredeemed Pool',
        impliedExposure: 'Implied Exposure',
        parityHint: '(500 ₫/pt parity)',
        activeMembers: 'Active Members',
        discountsAbsorbed: 'Discounts Absorbed',
        linkLoyaltyTiers: 'Loyalty Tiers',
        linkVoucherCatalog: 'Voucher Catalog',
        csatWarrantyBadge: 'CSAT & OEM WARRANTY',
        csatWarrantyTitle: '5-Star Service CSAT & Nationwide Warranty Exposure',
        networkCsat: 'Network CSAT',
        reviewsCount: '{{count}} reviews',
        openWarrantyClaims: 'Open Warranty Claims',
        approvedWarrantyCost: 'Approved Warranty Cost',
        linkQualityOversight: 'Quality & Warranty Oversight',
        chart1Subtitle: 'Realized SalesOrders.FinalAmount + ServiceInvoices.NetPaidAmount',
        chart1Badge: 'Stacked Bar',
        chart2TitleBar: 'Service Bookings Funnel by Dealer',
        chart2TitleDoughnut: 'Network Booking Status Mix',
        chart2SubBar: 'Completed vs. Active vs. Cancelled/NoShow per branch',
        chart2SubDoughnut: '7-status appointment lifecycle distribution',
        chart2ToggleDealer: 'By Dealer',
        chart2ToggleStatus: 'Status Mix',
        chart3Subtitle: 'Zero-filled contiguous YYYY-MM spine (Left: Revenue · Right: Bookings)',
        chart3Badge: 'Dual-Axis Line',
        chart4Subtitle: 'Bronze · Silver · Gold · Platinum membership snapshot',
        chart4Badge: '4-Tier Doughnut',
        dsShowroomSales: 'Showroom Bike Sales (VND)',
        dsWorkshopService: 'Workshop Service Invoices (VND)',
        dsServiceBookings: 'Service Bookings',
        dsCompleted: 'Completed',
        dsActive: 'Active',
        dsCancelledNoShow: 'Cancelled / NoShow',
        dsMonthlyRevenue: 'Monthly Total Revenue (VND)',
        dsMonthlyBookings: 'Monthly Bookings',
        dsLoyaltyMembers: 'Loyalty Members',
        axisRevenue: 'Revenue (VND)',
        axisBookings: 'Bookings',
        tableTitle: 'Per-Dealer Performance & Governance Breakdown',
        tableSubtitle: 'Click column headers to sort branches; jump directly to branch configuration, staff access, or quality oversight.',
        branchCountBadge: '{{count}} Branch(es)',
        colCode: 'Code',
        colDealerBranch: 'Dealer Branch',
        colCity: 'City',
        colUnitsSold: 'Units Sold',
        colShowroomRev: 'Showroom Revenue',
        colInvoices: 'Invoices',
        colServiceRev: 'Service Revenue',
        colTotalRev: 'Total Revenue',
        colBookings: 'Bookings',
        colCompletionPct: 'Completion %',
        colCsatWarranty: 'CSAT & Warranty',
        colGovernance: 'Governance Actions',
        doneSuffix: 'done',
        attritionSuffix: 'attr.',
        openClaimsSuffix: 'open claims',
        actionManageBranch: 'Manage Branch',
        actionViewStaff: 'View Staff',
        actionQuality: 'Quality',
        emptyTableTitle: 'No dealership rows matched the selected scope',
        emptyTableDesc: 'Try resetting branch or date range filters to view full nationwide analytics.',
        dateInvertedError: 'From Date ({{from}}) must be earlier than or equal to To Date ({{to}}).',
        dateSpanError: 'Analytics date window cannot exceed 731 days (24 months).',
        invalidDateToastTitle: 'Invalid Date Filter',
        csvExportedTitle: 'CSV Report Exported',
        csvExportedMessage: '{{filename}} downloaded.',
        csvExportFailedTitle: 'CSV Export Failed'
      }
    }
  };

  registerAnalyticsI18n.$inject = ['I18nService'];
  function registerAnalyticsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerAnalyticsI18n)
    .component('adminAnalytics', {
      controllerAs: '$ctrl',
      controller: AdminAnalyticsController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.analytics.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.analyticsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.analyticsSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary btn-sm is-signalr-pulse" ' +
                      'ng-if="$ctrl.hasPendingLiveActivity" ' +
                      'ng-click="$ctrl.loadAnalytics()" ' +
                      'ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="broadcast" weight="fill" size="15"></app-icon>' +
                '<span>{{ \'admin.analytics.liveActivityCta\' | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn btn-secondary" ' +
                      'ng-click="$ctrl.loadAnalytics()" ' +
                      'ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.analytics.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button id="exportAnalyticsCsvBtn" type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Analytics.ExportReports" ' +
                      'ng-disabled="$ctrl.isExporting || Boolean($ctrl.dateValidationError)" ' +
                      'ng-click="$ctrl.exportCsvReport()">' +
                '<span>{{ ($ctrl.isExporting ? \'admin.analytics.exportingCsv\' : \'admin.analytics.exportCsvCta\') | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="download-simple" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          /* Error Banner with Retry */
          '<error-retry-banner ' +
            'message="$ctrl.loadProblem ? ($ctrl.loadProblem.title + \': \' + $ctrl.loadProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.loadProblem && $ctrl.loadProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadAnalytics()">' +
          '</error-retry-banner>' +

          /* Interactive Quiet-Chrome Filter Toolbar */
          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:10px;margin-bottom:14px;">' +
                '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;" role="group">' +
                  '<span class="form-label" style="margin-right:4px;display:inline-flex;align-items:center;gap:6px;">' +
                    '<app-icon name="calendar-blank" weight="duotone" size="16"></app-icon>' +
                    '<span>{{ \'admin.analytics.windowPresetLabel\' | t }}</span>' +
                  '</span>' +
                  '<button type="button" class="btn btn-sm" ' +
                          'ng-repeat="preset in $ctrl.datePresets track by preset.key" ' +
                          'ng-class="$ctrl.activePreset === preset.key ? \'btn-primary\' : \'btn-secondary\'" ' +
                          'ng-disabled="$ctrl.isLoading" ' +
                          'ng-click="$ctrl.selectPreset(preset.key)">' +
                    '{{ preset.labelKey | t }}' +
                  '</button>' +
                '</div>' +
                '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                  '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Dealers.ViewPublic" ui-sref="app.admin.dealers">' +
                    '<app-icon name="buildings" weight="regular" size="14"></app-icon>' +
                    '<span>{{ \'admin.analytics.quickDealers\' | t }}</span>' +
                  '</a>' +
                  '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Users.ManageStaff" ui-sref="app.admin.staff">' +
                    '<app-icon name="users-three" weight="regular" size="14"></app-icon>' +
                    '<span>{{ \'admin.analytics.quickStaff\' | t }}</span>' +
                  '</a>' +
                  '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Showroom.ManageModels" ui-sref="app.admin.models">' +
                    '<app-icon name="motorcycle" weight="regular" size="14"></app-icon>' +
                    '<span>{{ \'admin.analytics.quickModels\' | t }}</span>' +
                  '</a>' +
                '</div>' +
              '</div>' +

              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="anaFromDate">{{ \'admin.analytics.fromDateLabel\' | t }}</label>' +
                  '<input id="anaFromDate" type="date" class="form-control mono-num" ' +
                         'ng-model="$ctrl.filters.fromDate" ' +
                         'ng-change="$ctrl.onDateInputChanged()" />' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="anaToDate">{{ \'admin.analytics.toDateLabel\' | t }}</label>' +
                  '<input id="anaToDate" type="date" class="form-control mono-num" ' +
                         'ng-model="$ctrl.filters.toDate" ' +
                         'ng-change="$ctrl.onDateInputChanged()" />' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="anaDealerSelect">{{ \'admin.analytics.dealerScopeLabel\' | t }}</label>' +
                  '<select id="anaDealerSelect" class="form-control" ' +
                          'ng-model="$ctrl.filters.dealerId" ' +
                          'ng-options="opt.id as opt.label for opt in $ctrl.dealerFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<div style="display:flex;gap:6px;">' +
                    '<button id="applyAnalyticsFiltersBtn" type="button" class="btn btn-primary" style="flex:1;" ' +
                            'ng-disabled="$ctrl.isLoading" ' +
                            'ng-click="$ctrl.applyFilters()">' +
                      '<app-icon name="funnel" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'admin.analytics.applyFilters\' | t }}</span>' +
                    '</button>' +
                    '<button id="resetAnalyticsFiltersBtn" type="button" class="btn btn-secondary" ' +
                            'ng-disabled="$ctrl.isLoading" ' +
                            'ng-click="$ctrl.resetFilters()">' +
                      '{{ \'admin.analytics.resetFilters\' | t }}' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="field-error" role="alert" style="margin-top:10px;display:inline-flex;align-items:center;gap:6px;" ng-if="$ctrl.dateValidationError">' +
                '<app-icon name="warning" weight="fill" size="15"></app-icon>' +
                '<span>{{ $ctrl.dateValidationError }}</span>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Initial Skeleton Loaders (non-destructive: charts remain mounted below) */
          '<div ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" style="margin-bottom:20px;display:flex;flex-direction:column;gap:16px;">' +
            '<skeleton-loader variant="kpi" count="4"></skeleton-loader>' +
            '<skeleton-loader variant="cards" count="2"></skeleton-loader>' +
          '</div>' +

          /* Graceful Empty-Window Callout */
          '<div class="alert-banner alert-warning" role="status" ng-if="$ctrl.hasLoadedOnce && !$ctrl.isLoading && $ctrl.isWindowEmpty">' +
            '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
              '<span style="display:inline-flex;align-items:center;gap:8px;">' +
                '<app-icon name="warning-circle" weight="duotone" size="18"></app-icon>' +
                '<span>{{ \'admin.analytics.emptyWindowMessage\' | t }}</span>' +
              '</span>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.resetFilters()">{{ \'admin.analytics.resetFilters\' | t }}</button>' +
            '</div>' +
          '</div>' +

          /* 4 Primary Executive Double-Bezel KPI Cards with kpi-counter */
          '<div class="bento-grid" style="margin-bottom:16px;" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<div class="col-3 bezel-card stagger-item" style="--i: {{ $index }};" ng-repeat="kpi in $ctrl.kpis track by kpi.id">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:8px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ kpi.labelKey | t }}</span>' +
                  '<app-icon name="{{ kpi.icon }}" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="font-size:20px;margin-top:0;" ' +
                     'kpi-counter="kpi.numericValue" kpi-format="{{ kpi.format }}">{{ kpi.value }}</div>' +
                '<div class="kpi-meta">{{ kpi.meta }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Executive Loyalty Liability & CSAT/Warranty Double-Bezel Bento Strip */
          '<div class="bento-grid" style="margin-bottom:24px;" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            /* Loyalty Liability Card */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:12px;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                  '<span class="badge badge-tier-gold mono-num">' +
                    '<app-icon name="crown" weight="fill" size="13"></app-icon>' +
                    '<span>{{ \'admin.analytics.liabilityBadge\' | t }}</span>' +
                  '</span>' +
                  '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                    '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Loyalty.ManageTiers" ui-sref="app.admin.loyaltyTiers">' +
                      '<span>{{ \'admin.analytics.linkLoyaltyTiers\' | t }}</span>' +
                      '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                    '</a>' +
                    '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Loyalty.ManageVoucherCatalog" ui-sref="app.admin.voucherCatalog">' +
                      '<span>{{ \'admin.analytics.linkVoucherCatalog\' | t }}</span>' +
                      '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                    '</a>' +
                  '</div>' +
                '</div>' +
                '<div style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;">' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<div class="kpi-label">{{ \'admin.analytics.unredeemedPool\' | t }}</div>' +
                    '<div class="mono-num text-primary" style="font-size:15px;font-weight:700;margin-top:4px;" ' +
                         'kpi-counter="$ctrl.liabilitySummary.unredeemedPointsPool" kpi-format="points">' +
                      '{{ $ctrl.liabilitySummary.unredeemedPointsPool | number }}' +
                    '</div>' +
                  '</div>' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<div class="kpi-label">{{ \'admin.analytics.impliedExposure\' | t }}</div>' +
                    '<div class="mono-num text-primary" style="font-size:15px;font-weight:700;margin-top:4px;" ' +
                         'kpi-counter="$ctrl.liabilitySummary.pointsLiabilityVnd" kpi-format="vnd">' +
                      '{{ $ctrl.liabilitySummary.pointsLiabilityVnd | vndCurrency }}' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:11px;">{{ \'admin.analytics.parityHint\' | t }}</div>' +
                  '</div>' +
                '</div>' +
                '<div style="display:flex;gap:16px;flex-wrap:wrap;font-size:12px;" class="text-secondary">' +
                  '<span>{{ \'admin.analytics.activeMembers\' | t }}: <strong class="mono-num text-primary" kpi-counter="$ctrl.liabilitySummary.totalMembersCount" kpi-format="number">{{ $ctrl.liabilitySummary.totalMembersCount | number }}</strong></span>' +
                  '<span>{{ \'admin.analytics.discountsAbsorbed\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.overview.totalVoucherDiscountVnd | vndCurrency }}</strong></span>' +
                '</div>' +
              '</div>' +
            '</div>' +

            /* CSAT & OEM Warranty Exposure Card */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:12px;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                  '<span class="badge badge-info mono-num">' +
                    '<app-icon name="shield-check" weight="fill" size="13"></app-icon>' +
                    '<span>{{ \'admin.analytics.csatWarrantyBadge\' | t }}</span>' +
                  '</span>' +
                  '<a class="btn btn-secondary btn-sm" has-permission="Permissions.Warranty.ViewDealer" ui-sref="app.admin.qualityWarranty">' +
                    '<span>{{ \'admin.analytics.linkQualityOversight\' | t }}</span>' +
                    '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                  '</a>' +
                '</div>' +
                '<div style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;">' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<div class="kpi-label">{{ \'admin.analytics.networkCsat\' | t }}</div>' +
                    '<div style="display:flex;align-items:center;gap:8px;margin-top:4px;flex-wrap:wrap;">' +
                      '<strong class="mono-num text-primary" style="font-size:16px;" kpi-counter="$ctrl.overview.averageCsatRating" kpi-format="decimal">' +
                        '{{ $ctrl.overview.averageCsatRating | number:2 }}' +
                      '</strong>' +
                      '<star-rating value="$ctrl.overview.averageCsatRating" readonly="true" size="14"></star-rating>' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:11px;margin-top:2px;">{{ \'admin.analytics.reviewsCount\' | t:{ count: ($ctrl.overview.totalServiceReviewsCount || 0) } }}</div>' +
                  '</div>' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<div class="kpi-label">{{ \'admin.analytics.approvedWarrantyCost\' | t }}</div>' +
                    '<div class="mono-num text-primary" style="font-size:15px;font-weight:700;margin-top:4px;" ' +
                         'kpi-counter="$ctrl.overview.approvedWarrantyCoveredVnd" kpi-format="vnd">' +
                      '{{ $ctrl.overview.approvedWarrantyCoveredVnd | vndCurrency }}' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:11px;margin-top:2px;">' +
                      '{{ \'admin.analytics.openWarrantyClaims\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.overview.openWarrantyClaimsCount | number }}</strong>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* 2x2 Chart.js v4 Double-Bezel Grid */
          '<div class="bento-grid" style="margin-bottom:24px;" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            /* Chart 1: Revenue by Dealer Stacked Bar */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<div>' +
                    '<h2 class="surface-card-title">{{ \'admin.chartRevenueByDealer\' | t }}</h2>' +
                    '<div class="text-muted" style="font-size:12px;">{{ \'admin.analytics.chart1Subtitle\' | t }}</div>' +
                  '</div>' +
                  '<span class="badge badge-neutral mono-num">' +
                    '<app-icon name="chart-bar" weight="duotone" size="13"></app-icon>' +
                    '<span>{{ \'admin.analytics.chart1Badge\' | t }}</span>' +
                  '</span>' +
                '</div>' +
                '<dealer-chart config="$ctrl.revenueByDealerChart" height-px="290" aria-label="{{ \'admin.chartRevenueByDealer\' | t }}"></dealer-chart>' +
              '</div>' +
            '</div>' +

            /* Chart 2: Toggleable Bookings Funnel (Grouped Bar <-> Status Doughnut) */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<div>' +
                    '<h2 class="surface-card-title">{{ ($ctrl.bookingsChartMode === \'bar\' ? \'admin.analytics.chart2TitleBar\' : \'admin.analytics.chart2TitleDoughnut\') | t }}</h2>' +
                    '<div class="text-muted" style="font-size:12px;">{{ ($ctrl.bookingsChartMode === \'bar\' ? \'admin.analytics.chart2SubBar\' : \'admin.analytics.chart2SubDoughnut\') | t }}</div>' +
                  '</div>' +
                  '<div style="display:inline-flex;gap:4px;" role="group">' +
                    '<button id="bookingsChartByDealerBtn" type="button" class="btn btn-sm" ' +
                            'ng-class="$ctrl.bookingsChartMode === \'bar\' ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'ng-click="$ctrl.setBookingsChartMode(\'bar\')">' +
                      '{{ \'admin.analytics.chart2ToggleDealer\' | t }}' +
                    '</button>' +
                    '<button id="bookingsChartStatusMixBtn" type="button" class="btn btn-sm" ' +
                            'ng-class="$ctrl.bookingsChartMode === \'doughnut\' ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'ng-click="$ctrl.setBookingsChartMode(\'doughnut\')">' +
                      '{{ \'admin.analytics.chart2ToggleStatus\' | t }}' +
                    '</button>' +
                  '</div>' +
                '</div>' +
                '<dealer-chart config="$ctrl.bookingsChart" height-px="290" aria-label="{{ \'admin.chartBookingsByDealer\' | t }}"></dealer-chart>' +
              '</div>' +
            '</div>' +

            /* Chart 3: Monthly Revenue & Booking Trends Dual-Axis Line */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<div>' +
                    '<h2 class="surface-card-title">{{ \'admin.chartMonthlyTrends\' | t }}</h2>' +
                    '<div class="text-muted" style="font-size:12px;">{{ \'admin.analytics.chart3Subtitle\' | t }}</div>' +
                  '</div>' +
                  '<span class="badge badge-neutral mono-num">' +
                    '<app-icon name="chart-line-up" weight="duotone" size="13"></app-icon>' +
                    '<span>{{ \'admin.analytics.chart3Badge\' | t }}</span>' +
                  '</span>' +
                '</div>' +
                '<dealer-chart config="$ctrl.monthlyTrendsChart" height-px="280" aria-label="{{ \'admin.chartMonthlyTrends\' | t }}"></dealer-chart>' +
              '</div>' +
            '</div>' +

            /* Chart 4: 4-Tier Loyalty Membership Distribution Doughnut */
            '<div class="col-6 bezel-card">' +
              '<div class="bezel-card-inner">' +
                '<div class="surface-card-header">' +
                  '<div>' +
                    '<h2 class="surface-card-title">{{ \'admin.chartLoyaltyDistribution\' | t }}</h2>' +
                    '<div class="text-muted" style="font-size:12px;">{{ \'admin.analytics.chart4Subtitle\' | t }}</div>' +
                  '</div>' +
                  '<span class="badge badge-neutral mono-num">' +
                    '<app-icon name="chart-donut" weight="duotone" size="13"></app-icon>' +
                    '<span>{{ \'admin.analytics.chart4Badge\' | t }}</span>' +
                  '</span>' +
                '</div>' +
                '<dealer-chart config="$ctrl.loyaltyDistributionChart" height-px="280" aria-label="{{ \'admin.chartLoyaltyDistribution\' | t }}"></dealer-chart>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Per-Dealer Performance & Governance Breakdown Quiet-Chrome Table */
          '<div class="bezel-card quiet-table-frame">' +
            '<div class="bezel-card-inner">' +
              '<div class="surface-card-header" style="margin-bottom:12px;">' +
                '<div>' +
                  '<h2 class="surface-card-title">{{ \'admin.analytics.tableTitle\' | t }}</h2>' +
                  '<div class="text-muted" style="font-size:12px;">{{ \'admin.analytics.tableSubtitle\' | t }}</div>' +
                '</div>' +
                '<span class="badge badge-info mono-num">{{ \'admin.analytics.branchCountBadge\' | t:{ count: $ctrl.sortedDealerRows.length } }}</span>' +
              '</div>' +

              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.sortedDealerRows.length === 0" ' +
                           'icon="buildings" ' +
                           'title-key="admin.analytics.emptyTableTitle" ' +
                           'desc-key="admin.analytics.emptyTableDesc" ' +
                           'action-key="admin.analytics.resetFilters" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.sortedDealerRows.length > 0" style="max-height:540px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'dealerCode\')">{{ \'admin.analytics.colCode\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'dealerName\')">{{ \'admin.analytics.colDealerBranch\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.analytics.colCity\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'unitsSoldCount\')">{{ \'admin.analytics.colUnitsSold\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'showroomRevenueVnd\')">{{ \'admin.analytics.colShowroomRev\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'invoicesIssuedCount\')">{{ \'admin.analytics.colInvoices\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'serviceRevenueVnd\')">{{ \'admin.analytics.colServiceRev\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'totalRevenueVnd\')">{{ \'admin.analytics.colTotalRev\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'totalBookings\')">{{ \'admin.analytics.colBookings\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'completionRatePct\')">{{ \'admin.analytics.colCompletionPct\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;"><button type="button" class="btn btn-secondary btn-sm" style="padding:2px 8px;min-height:26px;" ng-click="$ctrl.toggleSort(\'averageCsatRating\')">{{ \'admin.analytics.colCsatWarranty\' | t }}</button></th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.analytics.colGovernance\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="row in $ctrl.sortedDealerRows track by row.dealerId">' +
                      '<td class="mono-num" style="font-weight:700;">{{ row.dealerCode }}</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">' +
                          '{{ row.dealerName }}' +
                          '<span class="badge badge-neutral" style="margin-left:6px;" ng-if="row.isActive === false">{{ \'common.inactive\' | t }}</span>' +
                        '</div>' +
                      '</td>' +
                      '<td>{{ row.city }}</td>' +
                      '<td class="mono-num">{{ row.unitsSoldCount | number }}</td>' +
                      '<td class="mono-num">{{ row.showroomRevenueVnd | vndCurrency }}</td>' +
                      '<td class="mono-num">{{ row.invoicesIssuedCount | number }}</td>' +
                      '<td class="mono-num">{{ row.serviceRevenueVnd | vndCurrency }}</td>' +
                      '<td class="mono-num" style="font-weight:700;">{{ row.totalRevenueVnd | vndCurrency }}</td>' +
                      '<td class="mono-num">' +
                        '{{ row.totalBookings | number }}' +
                        '<div class="text-muted" style="font-size:11px;">{{ row.completedBookings | number }} {{ \'admin.analytics.doneSuffix\' | t }}</div>' +
                      '</td>' +
                      '<td class="mono-num">' +
                        '<span class="badge" ng-class="row.completionRatePct >= 75 ? \'badge-success\' : (row.totalBookings === 0 ? \'badge-neutral\' : \'badge-warning\')">' +
                          '{{ row.completionRatePct | number:1 }}%' +
                        '</span>' +
                        '<div class="text-muted" style="font-size:11px;margin-top:2px;">{{ row.cancellationRatePct | number:1 }}% {{ \'admin.analytics.attritionSuffix\' | t }}</div>' +
                      '</td>' +
                      '<td class="mono-num">' +
                        '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                          '<span class="badge" ng-class="row.totalServiceReviewsCount > 0 ? (row.averageCsatRating >= 4.5 ? \'badge-tier-gold\' : \'badge-info\') : \'badge-neutral\'">' +
                            '<app-icon name="star" weight="fill" size="12"></app-icon>' +
                            '<span>{{ (row.averageCsatRating || 0) | number:2 }}</span>' +
                          '</span>' +
                          '<span class="text-muted" style="font-size:11px;">{{ \'admin.analytics.reviewsCount\' | t:{ count: (row.totalServiceReviewsCount || 0) } }}</span>' +
                        '</div>' +
                        '<div class="text-muted" style="font-size:11px;margin-top:2px;">{{ (row.openWarrantyClaimsCount || 0) | number }} {{ \'admin.analytics.openClaimsSuffix\' | t }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.Dealers.ManageAll" ' +
                                  'ng-click="$ctrl.manageDealerBranch(row)">' +
                            '{{ \'admin.analytics.actionManageBranch\' | t }}' +
                          '</button>' +
                          '<a class="btn btn-secondary btn-sm" ' +
                             'has-permission="Permissions.Users.ManageStaff" ' +
                             'ui-sref="app.admin.staff({ dealerId: row.dealerId })">' +
                            '{{ \'admin.analytics.actionViewStaff\' | t }}' +
                          '</a>' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.Warranty.ViewDealer" ' +
                                  'ng-click="$ctrl.openDealerGovernance(row, \'app.admin.qualityWarranty\')">' +
                            '<span>{{ \'admin.analytics.actionQuality\' | t }}</span>' +
                            '<app-icon name="arrow-right" weight="regular" size="12"></app-icon>' +
                          '</button>' +
                        '</div>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminAnalyticsController.$inject = [
    '$q',
    '$rootScope',
    '$state',
    '$location',
    '$window',
    '$timeout',
    '$filter',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    '$scope',
    'I18nService',
    'ThemeService'
  ];
  function AdminAnalyticsController(
    $q,
    $rootScope,
    $state,
    $location,
    $window,
    $timeout,
    $filter,
    DealerApiService,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    $scope,
    I18nService,
    ThemeService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var fetchSeq = 0;
    var cleanupFns = [];
    var pendingRevokeTimers = [];
    var pendingBlobUrls = [];
    var lastHydratedBundle = null;
    var lastDealersCatalog = [];

    var vndFormatter = $filter ? $filter('vndCurrency') : null;

    function t(key, fallback, params) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      var text = fallback !== undefined ? String(fallback) : String(key);
      if (params && angular.isObject(params)) {
        text = text.replace(/\{\{\s*([A-Za-z0-9_.-]+)\s*\}\}/g, function (_m, tok) {
          return params[tok] !== undefined && params[tok] !== null ? String(params[tok]) : '';
        });
      }
      return text;
    }

    function enumLabel(group, code) {
      if (I18nService && angular.isFunction(I18nService.enumLabel)) {
        return I18nService.enumLabel(group, code);
      }
      return String(code || '');
    }

    function getChartLocale() {
      if (I18nService && angular.isFunction(I18nService.getLocale)) {
        return I18nService.getLocale();
      }
      return 'vi-VN';
    }

    function isDarkTheme() {
      if (ThemeService && angular.isFunction(ThemeService.isDark)) {
        return Boolean(ThemeService.isDark());
      }
      return false;
    }

    function getChartThemePalette() {
      var dark = isDarkTheme();
      return {
        textColor: dark ? '#F8FAFC' : '#0F172A',
        tickColor: dark ? '#94A3B8' : '#64748B',
        gridColor: dark ? '#334155' : '#E2E8F0',
        lineSecondaryColor: dark ? ANALYTICS_PALETTE.CyanInkDark : ANALYTICS_PALETTE.SlateInkLight,
        tooltipBg: dark ? '#020617' : '#0F172A',
        tooltipTitleColor: '#F8FAFC',
        tooltipBodyColor: '#E2E8F0',
        tooltipBorderColor: dark ? '#334155' : '#1E293B'
      };
    }

    function formatVnd(amount) {
      if (I18nService && angular.isFunction(I18nService.formatVnd)) {
        return I18nService.formatVnd(amount);
      }
      if (angular.isFunction(vndFormatter)) {
        return vndFormatter(amount);
      }
      var rounded = Math.round(Number(amount) || 0);
      return rounded.toLocaleString('vi-VN') + ' ₫';
    }

    function formatInt(val) {
      if (I18nService && angular.isFunction(I18nService.formatNumber)) {
        return I18nService.formatNumber(Math.round(Number(val) || 0));
      }
      return Math.round(Number(val) || 0).toLocaleString('vi-VN');
    }

    function formatPct(val) {
      var n = Number(val);
      if (!isFinite(n)) {
        return '0.0';
      }
      return (Math.round(n * 10) / 10).toFixed(1);
    }

    function pad2(n) {
      var v = Math.floor(Math.abs(Number(n) || 0));
      return v < 10 ? ('0' + v) : String(v);
    }

    function ymdToLocalDate(ymd) {
      if (!angular.isString(ymd) || !/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(ymd.trim())) {
        return null;
      }
      var parts = ymd.trim().split('-');
      return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]), 0, 0, 0, 0);
    }

    function filterValueToYmd(val) {
      if (!val) {
        return '';
      }
      if (val instanceof Date) {
        if (isNaN(val.getTime())) {
          return '';
        }
        return val.getFullYear() + '-' + pad2(val.getMonth() + 1) + '-' + pad2(val.getDate());
      }
      var s = String(val).trim();
      if (/^[0-9]{4}-[0-9]{2}-[0-9]{2}/.test(s)) {
        return s.slice(0, 10);
      }
      return '';
    }

    function computePresetRangeYmd(presetKey) {
      var todayYmd = DealerApiService.getTodayDateVn();
      var currentYear = Number(todayYmd.slice(0, 4)) || 2026;
      var currentMonth = Number(todayYmd.slice(5, 7)) || 1;
      if (presetKey === 'LAST_12M') {
        var startMonthUtc = new Date(Date.UTC(currentYear, currentMonth - 1 - 11, 1, 12, 0, 0));
        var startYmd = startMonthUtc.getUTCFullYear() + '-' + pad2(startMonthUtc.getUTCMonth() + 1) + '-01';
        return { fromYmd: startYmd, toYmd: todayYmd };
      }
      if (presetKey === 'YTD') {
        return { fromYmd: String(currentYear) + '-01-01', toYmd: todayYmd };
      }
      if (presetKey === 'ALL_2026') {
        return { fromYmd: '2026-01-01', toYmd: '2026-12-31' };
      }
      return { fromYmd: DealerApiService.addDaysDateVn(todayYmd, -180), toYmd: todayYmd };
    }

    function buildCommonTooltipConfig(themePal) {
      return {
        backgroundColor: themePal.tooltipBg,
        titleColor: themePal.tooltipTitleColor,
        bodyColor: themePal.tooltipBodyColor,
        borderColor: themePal.tooltipBorderColor,
        borderWidth: 1,
        padding: 10,
        cornerRadius: 8
      };
    }

    function buildRevenueChartConfig(revDto) {
      var themePal = getChartThemePalette();
      return {
        type: 'bar',
        data: {
          labels: (revDto.labels || []).slice(),
          datasets: [
            {
              label: t('admin.analytics.dsShowroomSales', 'Showroom Bike Sales (VND)'),
              data: (revDto.showroomSalesVnd || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.RacingRed,
              borderRadius: 4,
              stack: 'totalRevenue'
            },
            {
              label: t('admin.analytics.dsWorkshopService', 'Workshop Service Invoices (VND)'),
              data: (revDto.workshopServiceVnd || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.WorkshopBlue,
              borderRadius: 4,
              stack: 'totalRevenue'
            }
          ]
        },
        options: {
          locale: getChartLocale(),
          responsive: true,
          maintainAspectRatio: false,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: {
              position: 'top',
              labels: { color: themePal.textColor, usePointStyle: true, boxWidth: 8 }
            },
            tooltip: buildCommonTooltipConfig(themePal)
          },
          scales: {
            x: {
              stacked: true,
              ticks: { color: themePal.tickColor },
              grid: { color: themePal.gridColor, display: false }
            },
            y: {
              stacked: true,
              beginAtZero: true,
              title: {
                display: true,
                text: t('admin.analytics.axisRevenue', 'Revenue (VND)'),
                color: themePal.textColor
              },
              ticks: { color: themePal.tickColor },
              grid: { color: themePal.gridColor }
            }
          }
        }
      };
    }

    function buildBookingsChartConfig(bkDto, mode) {
      var themePal = getChartThemePalette();
      if (mode === 'doughnut') {
        var localizedStatusLabels = (bkDto.statusLabels || []).map(function (st) {
          return enumLabel('BookingStatus', st);
        });
        return {
          type: 'doughnut',
          data: {
            labels: localizedStatusLabels,
            datasets: [
              {
                label: t('admin.analytics.dsServiceBookings', 'Service Bookings'),
                data: (bkDto.statusCounts || []).slice(),
                backgroundColor: ANALYTICS_PALETTE.BookingStatuses.slice(),
                borderWidth: 0
              }
            ]
          },
          options: {
            locale: getChartLocale(),
            responsive: true,
            maintainAspectRatio: false,
            cutout: '58%',
            plugins: {
              legend: {
                position: 'right',
                labels: { color: themePal.textColor, usePointStyle: true, boxWidth: 8 }
              },
              tooltip: buildCommonTooltipConfig(themePal)
            }
          }
        };
      }
      return {
        type: 'bar',
        data: {
          labels: (bkDto.dealerLabels || []).slice(),
          datasets: [
            {
              label: t('admin.analytics.dsCompleted', 'Completed'),
              data: (bkDto.completedByDealer || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.CompletedGreen,
              borderRadius: 4
            },
            {
              label: t('admin.analytics.dsActive', 'Active'),
              data: (bkDto.activeByDealer || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.WorkshopBlue,
              borderRadius: 4
            },
            {
              label: t('admin.analytics.dsCancelledNoShow', 'Cancelled / NoShow'),
              data: (bkDto.cancelledOrNoShowByDealer || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.RacingRed,
              borderRadius: 4
            }
          ]
        },
        options: {
          locale: getChartLocale(),
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: 'top',
              labels: { color: themePal.textColor, usePointStyle: true, boxWidth: 8 }
            },
            tooltip: buildCommonTooltipConfig(themePal)
          },
          scales: {
            x: {
              stacked: false,
              ticks: { color: themePal.tickColor },
              grid: { color: themePal.gridColor, display: false }
            },
            y: {
              stacked: false,
              beginAtZero: true,
              title: {
                display: true,
                text: t('admin.analytics.axisBookings', 'Bookings'),
                color: themePal.textColor
              },
              ticks: { color: themePal.tickColor },
              grid: { color: themePal.gridColor }
            }
          }
        }
      };
    }

    function buildMonthlyTrendsChartConfig(trendsDto) {
      var themePal = getChartThemePalette();
      return {
        type: 'line',
        data: {
          labels: (trendsDto.monthLabels || []).slice(),
          datasets: [
            {
              label: t('admin.analytics.dsMonthlyRevenue', 'Monthly Total Revenue (VND)'),
              data: (trendsDto.combinedRevenueVnd || []).slice(),
              borderColor: ANALYTICS_PALETTE.RacingRed,
              backgroundColor: ANALYTICS_PALETTE.RacingRedFill,
              yAxisID: 'yRevenue',
              tension: 0.3,
              fill: true
            },
            {
              label: t('admin.analytics.dsMonthlyBookings', 'Monthly Bookings'),
              data: (trendsDto.bookingCounts || []).slice(),
              borderColor: themePal.lineSecondaryColor,
              backgroundColor: themePal.lineSecondaryColor,
              yAxisID: 'yBookings',
              tension: 0.3
            }
          ]
        },
        options: {
          locale: getChartLocale(),
          responsive: true,
          maintainAspectRatio: false,
          interaction: { mode: 'index', intersect: false },
          plugins: {
            legend: {
              position: 'top',
              labels: { color: themePal.textColor, usePointStyle: true, boxWidth: 8 }
            },
            tooltip: buildCommonTooltipConfig(themePal)
          },
          scales: {
            x: {
              ticks: { color: themePal.tickColor },
              grid: { display: false, color: themePal.gridColor }
            },
            yRevenue: {
              type: 'linear',
              position: 'left',
              beginAtZero: true,
              title: {
                display: true,
                text: t('admin.analytics.axisRevenue', 'Revenue (VND)'),
                color: themePal.textColor
              },
              ticks: { color: themePal.tickColor },
              grid: { color: themePal.gridColor }
            },
            yBookings: {
              type: 'linear',
              position: 'right',
              beginAtZero: true,
              title: {
                display: true,
                text: t('admin.analytics.axisBookings', 'Bookings'),
                color: themePal.textColor
              },
              ticks: { color: themePal.tickColor },
              grid: { drawOnChartArea: false, color: themePal.gridColor }
            }
          }
        }
      };
    }

    function buildLoyaltyDistributionChartConfig(loyaltyDto) {
      var themePal = getChartThemePalette();
      var baseLabels = angular.isArray(loyaltyDto.tierLabels) && loyaltyDto.tierLabels.length > 0
        ? loyaltyDto.tierLabels
        : (loyaltyDto.displayLabels || []);
      var localizedLabels = baseLabels.map(function (tierCode, idx) {
        var cleanCode = String(tierCode || '').split(' ')[0];
        var label = enumLabel('LoyaltyTierCode', cleanCode);
        var count = angular.isArray(loyaltyDto.customerCounts) ? (Number(loyaltyDto.customerCounts[idx]) || 0) : 0;
        return I18nService ? (label + ' (' + formatInt(count) + ')') : ((loyaltyDto.displayLabels && loyaltyDto.displayLabels[idx]) || label);
      });
      return {
        type: 'doughnut',
        data: {
          labels: localizedLabels,
          datasets: [
            {
              label: t('admin.analytics.dsLoyaltyMembers', 'Loyalty Members'),
              data: (loyaltyDto.customerCounts || []).slice(),
              backgroundColor: ANALYTICS_PALETTE.LoyaltyTiers.slice(),
              borderWidth: 0
            }
          ]
        },
        options: {
          locale: getChartLocale(),
          responsive: true,
          maintainAspectRatio: false,
          cutout: '62%',
          plugins: {
            legend: {
              position: 'right',
              labels: { color: themePal.textColor, usePointStyle: true, boxWidth: 8 }
            },
            tooltip: buildCommonTooltipConfig(themePal)
          }
        }
      };
    }

    function rebuildDealerFilterOptions(dealers) {
      lastDealersCatalog = angular.isArray(dealers) ? dealers.slice() : [];
      var opts = [{ id: '', label: t('showroom.filterAllDealers', 'All Dealerships') }];
      var inactiveWord = t('common.inactive', 'Inactive');
      for (var i = 0; i < lastDealersCatalog.length; i++) {
        var d = lastDealersCatalog[i];
        if (!d || d.isDeleted === true) {
          continue;
        }
        var branchId = String(d.id || d.dealerId || '');
        if (!branchId) {
          continue;
        }
        var baseLabel = String(d.code || d.dealerCode || '') + ' • ' + String(d.name || d.dealerName || '');
        var statusSuffix = d.isActive === false ? (' (' + inactiveWord + ')') : '';
        opts.push({
          id: branchId,
          label: baseLabel + statusSuffix,
          isActive: d.isActive !== false
        });
      }
      $ctrl.dealerFilterOptions = opts;
    }

    function buildLookupByIdOrCode(rows) {
      var byId = Object.create(null);
      var byCode = Object.create(null);
      var list = angular.isArray(rows) ? rows : [];
      for (var i = 0; i < list.length; i++) {
        var item = list[i];
        if (!item) {
          continue;
        }
        if (item.dealerId) {
          byId[item.dealerId] = item;
        }
        if (item.dealerCode) {
          byCode[String(item.dealerCode).toUpperCase()] = item;
        }
      }
      return {
        find: function (dealerId, dealerCode) {
          if (dealerId && byId[dealerId]) {
            return byId[dealerId];
          }
          var norm = dealerCode ? String(dealerCode).toUpperCase() : '';
          return norm ? (byCode[norm] || null) : null;
        }
      };
    }

    function mergeAndSortDealerRows(overviewDealers, revDealers, bkDealers) {
      var hasAuthoritativeOverview = angular.isArray(overviewDealers) && overviewDealers.length > 0;
      var baseRows = hasAuthoritativeOverview
        ? overviewDealers
        : (angular.isArray(revDealers) && revDealers.length > 0 ? revDealers : (bkDealers || []));
      var revLookup = buildLookupByIdOrCode(revDealers);
      var bkLookup = buildLookupByIdOrCode(bkDealers);

      $ctrl.dealerRows = baseRows.map(function (primary) {
        var row = angular.extend({}, primary);
        var revMatch = revLookup.find(row.dealerId, row.dealerCode);
        var bkMatch = bkLookup.find(row.dealerId, row.dealerCode);

        if (!row.city) {
          row.city = (revMatch && revMatch.city) || (bkMatch && bkMatch.city) || '';
        }
        if (!hasAuthoritativeOverview) {
          if (revMatch) {
            row.unitsSoldCount = revMatch.unitsSoldCount;
            row.unitsSold = revMatch.unitsSoldCount;
            row.showroomRevenueVnd = revMatch.showroomRevenueVnd;
            row.invoicesIssuedCount = revMatch.invoicesIssuedCount;
            row.invoicesIssued = revMatch.invoicesIssuedCount;
            row.serviceRevenueVnd = revMatch.serviceRevenueVnd;
            row.totalRevenueVnd = revMatch.totalRevenueVnd;
          }
          if (bkMatch) {
            row.totalBookings = bkMatch.totalBookings;
            row.completedBookings = bkMatch.completedBookings;
            row.activeBookings = bkMatch.activeBookings;
            row.cancelledOrNoShowBookings = bkMatch.cancelledOrNoShowBookings;
            row.completionRatePct = bkMatch.completionRatePct;
            row.cancellationRatePct = bkMatch.cancellationRatePct;
          }
        }
        return row;
      });

      if ($ctrl.filters && $ctrl.filters.dealerId) {
        $ctrl.dealerRows = $ctrl.dealerRows.filter(function (r) {
          return r.dealerId === $ctrl.filters.dealerId;
        });
      }
      resortDealerTable();
    }

    function resortDealerTable() {
      var key = $ctrl.sortKey || 'totalRevenueVnd';
      var desc = $ctrl.sortDesc !== false;
      var copy = ($ctrl.dealerRows || []).slice();
      copy.sort(function (a, b) {
        var av = a[key];
        var bv = b[key];
        var cmp = 0;
        if (typeof av === 'string' || typeof bv === 'string') {
          cmp = String(av || '').localeCompare(String(bv || ''));
        } else {
          cmp = (Number(av) || 0) - (Number(bv) || 0);
        }
        if (cmp === 0) {
          cmp = String(a.dealerCode || '').localeCompare(String(b.dealerCode || ''));
        }
        return desc ? -cmp : cmp;
      });
      $ctrl.sortedDealerRows = copy;
    }

    function rebuildVisualizationsFromCache() {
      if (!lastHydratedBundle) {
        return;
      }
      var overview = lastHydratedBundle.overview;
      var revDto = lastHydratedBundle.revenueByDealer;
      var bkDto = lastHydratedBundle.bookingsByDealer;
      var trendsDto = lastHydratedBundle.monthlyTrends;
      var loyaltyDto = lastHydratedBundle.loyaltyDistribution;

      $ctrl.kpis = [
        {
          id: 'combined-revenue',
          labelKey: 'admin.kpiTotalRevenue',
          label: t('admin.kpiTotalRevenue', 'Combined Network Revenue'),
          icon: 'currency-circle-dollar',
          format: 'vnd',
          numericValue: Number(overview.totalRevenueVnd) || 0,
          value: formatVnd(overview.totalRevenueVnd),
          meta: t(
            'admin.analytics.kpiMetaCombined',
            '{{dealers}} Active Dealers · {{units}} VINs + {{invoices}} Invoices',
            {
              dealers: overview.activeDealersCount,
              units: formatInt(overview.unitsSoldCount),
              invoices: formatInt(overview.invoicesIssuedCount)
            }
          )
        },
        {
          id: 'showroom-sales',
          labelKey: 'admin.kpiShowroomRevenue',
          label: t('admin.kpiShowroomRevenue', 'Showroom Bike Sales'),
          icon: 'motorcycle',
          format: 'vnd',
          numericValue: Number(overview.showroomRevenueVnd) || 0,
          value: formatVnd(overview.showroomRevenueVnd),
          meta: t('admin.analytics.kpiMetaShowroom', '{{units}} VINs Completed', {
            units: formatInt(overview.unitsSoldCount)
          })
        },
        {
          id: 'service-revenue',
          labelKey: 'admin.kpiWorkshopRevenue',
          label: t('admin.kpiWorkshopRevenue', 'Workshop Service Revenue'),
          icon: 'wrench',
          format: 'vnd',
          numericValue: Number(overview.serviceRevenueVnd) || 0,
          value: formatVnd(overview.serviceRevenueVnd),
          meta: t('admin.analytics.kpiMetaWorkshop', '{{invoices}} Settled Invoices', {
            invoices: formatInt(overview.invoicesIssuedCount)
          })
        },
        {
          id: 'bookings-funnel',
          labelKey: 'admin.analytics.kpiBookingsFunnel',
          label: t('admin.analytics.kpiBookingsFunnel', 'Service Bookings & Funnel'),
          icon: 'calendar-blank',
          format: 'number',
          numericValue: Number(overview.totalBookings) || 0,
          value: formatInt(overview.totalBookings),
          meta: t(
            'admin.analytics.kpiMetaBookings',
            '{{completion}}% completion · {{attrition}}% attrition',
            {
              completion: formatPct(overview.completionRatePct),
              attrition: formatPct(overview.cancellationRatePct)
            }
          )
        }
      ];

      $ctrl.revenueByDealerChart = buildRevenueChartConfig(revDto);
      $ctrl.bookingsChart = buildBookingsChartConfig(bkDto, $ctrl.bookingsChartMode);
      $ctrl.bookingsByStatusChart = $ctrl.bookingsChart;
      $ctrl.monthlyTrendsChart = buildMonthlyTrendsChartConfig(trendsDto);
      $ctrl.loyaltyDistributionChart = buildLoyaltyDistributionChartConfig(loyaltyDto);
    }

    function applyHydratedBundle(bundle) {
      lastHydratedBundle = bundle;
      var overview = bundle.overview;
      var revDto = bundle.revenueByDealer;
      var bkDto = bundle.bookingsByDealer;
      var loyaltyDto = bundle.loyaltyDistribution;

      $ctrl.overview = overview;
      $ctrl.bookingsData = bkDto;
      $ctrl.loyaltyData = loyaltyDto;

      var membersCount = loyaltyDto.totalMembers > 0 ? loyaltyDto.totalMembers : overview.totalMembersCount;
      var unredeemedPool = loyaltyDto.unredeemedPointsPool > 0 || overview.unredeemedPointsPool === 0
        ? loyaltyDto.unredeemedPointsPool
        : overview.unredeemedPointsPool;
      var liabilityVnd = loyaltyDto.pointsLiabilityVnd > 0 || overview.pointsLiabilityVnd === 0
        ? loyaltyDto.pointsLiabilityVnd
        : overview.pointsLiabilityVnd;

      $ctrl.liabilitySummary = {
        totalMembersCount: membersCount,
        unredeemedPointsPool: unredeemedPool,
        pointsLiabilityVnd: liabilityVnd
      };

      rebuildVisualizationsFromCache();
      mergeAndSortDealerRows(overview.dealers, revDto.dealers, bkDto.dealerBreakdown);
      $ctrl.isWindowEmpty = overview.totalRevenueVnd === 0 && overview.totalBookings === 0;
    }

    function initEmptyAnalyticsState() {
      var emptyOverview = DealerApiService.normalizeAnalyticsOverview({
        totalRevenueVnd: 0,
        showroomRevenueVnd: 0,
        unitsSoldCount: 0,
        serviceRevenueVnd: 0,
        invoicesIssuedCount: 0,
        totalVoucherDiscountVnd: 0,
        totalBookings: 0,
        completedBookings: 0,
        activeBookings: 0,
        cancelledOrNoShowBookings: 0,
        completionRatePct: 0,
        cancellationRatePct: 0,
        activeDealersCount: 0,
        totalMembersCount: 0,
        unredeemedPointsPool: 0,
        pointsLiabilityVnd: 0,
        averageCsatRating: 0,
        totalServiceReviewsCount: 0,
        openWarrantyClaimsCount: 0,
        approvedWarrantyCoveredVnd: 0,
        dealers: []
      });
      var emptyRev = DealerApiService.normalizeAnalyticsRevenueByDealer({
        dealers: []
      });
      var emptyBk = DealerApiService.normalizeAnalyticsBookingsByDealer({
        statusLabels: ['Completed', 'Confirmed', 'CheckedIn', 'InProgress', 'Pending', 'Cancelled', 'NoShow'],
        statusCounts: [0, 0, 0, 0, 0, 0, 0],
        dealerBreakdown: []
      });
      var emptyTrends = DealerApiService.normalizeAnalyticsMonthlyTrends({
        monthLabels: [],
        combinedRevenueVnd: [],
        bookingCounts: []
      });
      var emptyLoyalty = DealerApiService.normalizeAnalyticsLoyaltyDistribution({
        tierLabels: ['Bronze', 'Silver', 'Gold', 'Platinum'],
        customerCounts: [0, 0, 0, 0],
        availablePointsByTier: [0, 0, 0, 0],
        totalMembers: 0,
        unredeemedPointsPool: 0,
        pointsLiabilityVnd: 0
      });

      rebuildDealerFilterOptions([]);

      applyHydratedBundle({
        overview: emptyOverview,
        revenueByDealer: emptyRev,
        bookingsByDealer: emptyBk,
        monthlyTrends: emptyTrends,
        loyaltyDistribution: emptyLoyalty
      });
    }

    $ctrl.validateDateWindow = function () {
      var fromYmd = filterValueToYmd($ctrl.filters && $ctrl.filters.fromDate);
      var toYmd = filterValueToYmd($ctrl.filters && $ctrl.filters.toDate);
      if (fromYmd && toYmd) {
        if (fromYmd > toYmd) {
          $ctrl.dateValidationError = t(
            'admin.analytics.dateInvertedError',
            'From Date ({{from}}) must be earlier than or equal to To Date ({{to}}).',
            { from: fromYmd, to: toYmd }
          );
          return false;
        }
        var fromMs = Date.parse(fromYmd + 'T00:00:00Z');
        var toMs = Date.parse(toYmd + 'T00:00:00Z');
        var spanDays = Math.round((toMs - fromMs) / 86400000);
        if (spanDays > 731) {
          $ctrl.dateValidationError = t(
            'admin.analytics.dateSpanError',
            'Analytics date window cannot exceed 731 days (24 months).'
          );
          return false;
        }
      }
      $ctrl.dateValidationError = null;
      return true;
    };

    function buildActiveFilterPayload() {
      var fromYmd = filterValueToYmd($ctrl.filters && $ctrl.filters.fromDate);
      var toYmd = filterValueToYmd($ctrl.filters && $ctrl.filters.toDate);
      return {
        fromDate: fromYmd || null,
        toDate: toYmd || null,
        dealerId: ($ctrl.filters && $ctrl.filters.dealerId) || null
      };
    }

    function handleVisualEnvChanged() {
      if (isDestroyed) {
        return;
      }
      rebuildDealerFilterOptions(lastDealersCatalog);
      rebuildVisualizationsFromCache();
      if ($ctrl.dateValidationError) {
        $ctrl.validateDateWindow();
      }
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isExporting = false;
      $ctrl.hasLoadedOnce = false;
      $ctrl.isWindowEmpty = false;
      $ctrl.hasPendingLiveActivity = false;
      $ctrl.loadProblem = null;
      $ctrl.dateValidationError = null;
      $ctrl.bookingsChartMode = 'bar';
      $ctrl.sortKey = 'totalRevenueVnd';
      $ctrl.sortDesc = true;

      $ctrl.datePresets = [
        { key: 'LAST_6M', labelKey: 'admin.analytics.presetLast6m', label: 'Last 6M' },
        { key: 'LAST_12M', labelKey: 'admin.analytics.presetLast12m', label: 'Last 12M' },
        { key: 'YTD', labelKey: 'admin.analytics.presetYtd', label: 'YTD' },
        { key: 'ALL_2026', labelKey: 'admin.analytics.presetAll2026', label: 'All 2026' }
      ];

      var initialRange = computePresetRangeYmd('LAST_6M');
      $ctrl.activePreset = 'LAST_6M';
      $ctrl.filters = {
        fromDate: ymdToLocalDate(initialRange.fromYmd),
        toDate: ymdToLocalDate(initialRange.toYmd),
        dealerId: ''
      };

      initEmptyAnalyticsState();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        var liveAnalyticsEvents = [
          'BookingCreated',
          'ServiceReviewSubmitted',
          'WarrantyClaimCreated',
          'WarrantyClaimStatusChanged'
        ];
        for (var eIdx = 0; eIdx < liveAnalyticsEvents.length; eIdx++) {
          cleanupFns.push(SignalRClientService.on(liveAnalyticsEvents[eIdx], function () {
            if (!isDestroyed) {
              $ctrl.hasPendingLiveActivity = true;
            }
          }));
        }
      }
      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed && !$ctrl.isLoading) {
            $ctrl.loadAnalytics();
          }
        }));
      }

      var eventBus = ($scope && angular.isFunction($scope.$on))
        ? $scope
        : (($rootScope && angular.isFunction($rootScope.$on)) ? $rootScope : null);
      if (eventBus) {
        cleanupFns.push(eventBus.$on('i18n:changed', handleVisualEnvChanged));
        cleanupFns.push(eventBus.$on('theme:changed', handleVisualEnvChanged));
      }

      $ctrl.loadAnalytics(true);
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        if (angular.isFunction(cleanupFns[i])) {
          cleanupFns[i]();
        }
      }
      cleanupFns.length = 0;

      if ($timeout && angular.isFunction($timeout.cancel)) {
        for (var tIdx = 0; tIdx < pendingRevokeTimers.length; tIdx++) {
          $timeout.cancel(pendingRevokeTimers[tIdx]);
        }
      }
      pendingRevokeTimers.length = 0;

      var urlApi = $window && $window.URL;
      if (urlApi && angular.isFunction(urlApi.revokeObjectURL)) {
        for (var u = 0; u < pendingBlobUrls.length; u++) {
          try {
            urlApi.revokeObjectURL(pendingBlobUrls[u]);
          } catch (e) {
            // Safe cleanup
          }
        }
      }
      pendingBlobUrls.length = 0;
    };

    $ctrl.onDateInputChanged = function () {
      $ctrl.activePreset = 'CUSTOM';
      $ctrl.validateDateWindow();
    };

    $ctrl.selectPreset = function (presetKey) {
      var range = computePresetRangeYmd(presetKey);
      $ctrl.activePreset = presetKey;
      $ctrl.filters.fromDate = ymdToLocalDate(range.fromYmd);
      $ctrl.filters.toDate = ymdToLocalDate(range.toYmd);
      $ctrl.dateValidationError = null;
      return $ctrl.loadAnalytics(false);
    };

    $ctrl.applyFilters = function () {
      if (!$ctrl.validateDateWindow()) {
        ToastService.warning(t('admin.analytics.invalidDateToastTitle', 'Invalid Date Filter'), $ctrl.dateValidationError);
        return $q.resolve(null);
      }
      return $ctrl.loadAnalytics(false);
    };

    $ctrl.resetFilters = function () {
      var range = computePresetRangeYmd('LAST_6M');
      $ctrl.activePreset = 'LAST_6M';
      $ctrl.filters = {
        fromDate: ymdToLocalDate(range.fromYmd),
        toDate: ymdToLocalDate(range.toYmd),
        dealerId: ''
      };
      $ctrl.dateValidationError = null;
      return $ctrl.loadAnalytics(false);
    };

    $ctrl.setBookingsChartMode = function (mode) {
      var nextMode = mode === 'doughnut' ? 'doughnut' : 'bar';
      if ($ctrl.bookingsChartMode === nextMode) {
        return;
      }
      $ctrl.bookingsChartMode = nextMode;
      if ($ctrl.bookingsData) {
        $ctrl.bookingsChart = buildBookingsChartConfig($ctrl.bookingsData, nextMode);
        $ctrl.bookingsByStatusChart = $ctrl.bookingsChart;
      }
    };

    $ctrl.toggleSort = function (columnKey) {
      if ($ctrl.sortKey === columnKey) {
        $ctrl.sortDesc = !$ctrl.sortDesc;
      } else {
        $ctrl.sortKey = columnKey;
        $ctrl.sortDesc = columnKey !== 'dealerCode' && columnKey !== 'dealerName';
      }
      resortDealerTable();
    };

    $ctrl.openDealerGovernance = function (row, targetState) {
      if (!row) {
        return;
      }
      var destination = targetState || 'app.admin.qualityWarranty';
      DealerApiService.setAdminFocusContext({
        dealerId: row.dealerId,
        dealerCode: row.dealerCode,
        dealerName: row.dealerName || ''
      });
      if (destination === 'app.admin.dealers' && $location && angular.isFunction($location.search)) {
        $location.search('dealerCode', row.dealerCode);
      }
      if ($state && angular.isFunction($state.go)) {
        if (destination === 'app.admin.staff') {
          $state.go(destination, { dealerId: row.dealerId });
        } else {
          $state.go(destination);
        }
      }
    };

    $ctrl.manageDealerBranch = function (row) {
      $ctrl.openDealerGovernance(row, 'app.admin.dealers');
    };

    $ctrl.loadAnalytics = function (includeDealersCatalog) {
      if (!$ctrl.validateDateWindow()) {
        ToastService.warning(t('admin.analytics.invalidDateToastTitle', 'Invalid Date Filter'), $ctrl.dateValidationError);
        return $q.resolve(null);
      }

      var currentSeq = ++fetchSeq;
      var queryPayload = buildActiveFilterPayload();
      $ctrl.isLoading = true;
      $ctrl.loadProblem = null;
      $ctrl.hasPendingLiveActivity = false;

      var requests = {
        overview: DealerApiService.getAnalyticsOverview(queryPayload),
        revenueByDealer: DealerApiService.getAnalyticsRevenueByDealer(queryPayload),
        bookingsByDealer: DealerApiService.getAnalyticsBookingsByDealer(queryPayload),
        monthlyTrends: DealerApiService.getAnalyticsMonthlyTrends(queryPayload),
        loyaltyDistribution: DealerApiService.getAnalyticsLoyaltyDistribution()
      };
      if (includeDealersCatalog) {
        requests.dealersList = DealerApiService.listDealers({ includeInactive: true }).catch(function () {
          return [];
        });
      }

      return $q.all(requests)
        .then(function (bundle) {
          if (isDestroyed || currentSeq !== fetchSeq) {
            return null;
          }
          if (angular.isArray(bundle.dealersList) && bundle.dealersList.length > 0) {
            rebuildDealerFilterOptions(bundle.dealersList);
          } else if (bundle.overview && bundle.overview.dealers && bundle.overview.dealers.length > 0 && (!$ctrl.dealerFilterOptions || $ctrl.dealerFilterOptions.length <= 1)) {
            rebuildDealerFilterOptions(bundle.overview.dealers);
          }
          applyHydratedBundle(bundle);
          $ctrl.hasLoadedOnce = true;
          return bundle.overview;
        })
        .catch(function (rejection) {
          if (!isDestroyed && currentSeq === fetchSeq) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.loadProblem = prob;
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed && currentSeq === fetchSeq) {
            $ctrl.isLoading = false;
          }
        });
    };

    function hasUtf8Bom(bytesOrStr) {
      if (!bytesOrStr) {
        return false;
      }
      if (angular.isString(bytesOrStr)) {
        return bytesOrStr.charCodeAt(0) === 0xFEFF;
      }
      if (Object.prototype.toString.call(bytesOrStr) === '[object ArrayBuffer]') {
        if (bytesOrStr.byteLength < 3) {
          return false;
        }
        var view = new Uint8Array(bytesOrStr, 0, 3);
        return view[0] === 0xEF && view[1] === 0xBB && view[2] === 0xBF;
      }
      if (typeof ArrayBuffer !== 'undefined' && ArrayBuffer.isView && ArrayBuffer.isView(bytesOrStr)) {
        if (bytesOrStr.byteLength < 3) {
          return false;
        }
        var arrView = new Uint8Array(bytesOrStr.buffer, bytesOrStr.byteOffset, 3);
        return arrView[0] === 0xEF && arrView[1] === 0xBB && arrView[2] === 0xBF;
      }
      return false;
    }

    $ctrl.exportCsvReport = function () {
      if ($ctrl.isExporting) {
        return $q.resolve(null);
      }
      if (!$ctrl.validateDateWindow()) {
        ToastService.warning(t('admin.analytics.invalidDateToastTitle', 'Invalid Date Filter'), $ctrl.dateValidationError);
        return $q.resolve(null);
      }

      $ctrl.isExporting = true;
      var queryPayload = buildActiveFilterPayload();

      return DealerApiService.exportAnalyticsCsv(queryPayload)
        .then(function (csvResult) {
          if (isDestroyed || !csvResult) {
            return null;
          }
          var rawBytes = csvResult.bytes || '';
          var blobParts = hasUtf8Bom(rawBytes) ? [rawBytes] : ['\ufeff', rawBytes];
          var BlobCtor = ($window && $window.Blob) || (typeof Blob !== 'undefined' ? Blob : null);
          var urlApi = $window && $window.URL;
          var doc = $window && $window.document;

          if (BlobCtor && urlApi && angular.isFunction(urlApi.createObjectURL) && doc && angular.isFunction(doc.createElement)) {
            var blob = new BlobCtor(blobParts, { type: 'text/csv;charset=utf-8;' });
            var blobUrl = urlApi.createObjectURL(blob);
            pendingBlobUrls.push(blobUrl);

            var anchor = doc.createElement('a');
            anchor.href = blobUrl;
            anchor.download = csvResult.filename;
            anchor.style.display = 'none';
            if (doc.body && angular.isFunction(doc.body.appendChild)) {
              doc.body.appendChild(anchor);
            }
            if (angular.isFunction(anchor.click)) {
              anchor.click();
            }
            if (anchor.parentNode && angular.isFunction(anchor.parentNode.removeChild)) {
              anchor.parentNode.removeChild(anchor);
            }

            if ($timeout) {
              var timer = $timeout(function () {
                if (angular.isFunction(urlApi.revokeObjectURL)) {
                  urlApi.revokeObjectURL(blobUrl);
                }
                var idx = pendingBlobUrls.indexOf(blobUrl);
                if (idx !== -1) {
                  pendingBlobUrls.splice(idx, 1);
                }
              }, 100);
              pendingRevokeTimers.push(timer);
            } else if (angular.isFunction(urlApi.revokeObjectURL)) {
              urlApi.revokeObjectURL(blobUrl);
            }
          }

          ToastService.success(
            t('admin.analytics.csvExportedTitle', 'CSV Report Exported'),
            t('admin.analytics.csvExportedMessage', '{{filename}} downloaded.', { filename: csvResult.filename })
          );
          return csvResult;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var problem = ProblemDetailsMapper.extract(rejection);
            ToastService.danger(problem.title || t('admin.analytics.csvExportFailedTitle', 'CSV Export Failed'), problem.detail);
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isExporting = false;
          }
        });
    };
  }
})();
