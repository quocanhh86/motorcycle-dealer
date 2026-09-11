(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      qualityWarranty: {
        screenCode: 'SCR-ADM-08 · v1.2 Giám sát CSAT & Bảo hành OEM',
        liveActivityBadge: 'Có hoạt động SignalR trực tiếp',
        backToAnalytics: 'Phân tích Doanh thu',
        refreshing: 'Đang tải lại...',
        refreshCta: 'Tải lại dữ liệu HQ',
        kpiAvgCsat: 'Điểm CSAT trung bình toàn quốc',
        kpiFiveStarMeta: '{{pct}}% tỷ lệ 5 sao ({{count}} đánh giá)',
        kpiUnanswered: 'Đánh giá chưa phản hồi',
        kpiResponseRateMeta: '{{pct}}% tỷ lệ đại lý đã phản hồi',
        kpiOpenClaims: 'Hồ sơ bảo hành VIN đang mở',
        kpiSettledRejectedMeta: '{{done}} hoàn tất · {{rej}} từ chối',
        kpiCoveredCost: 'Chi phí bảo hành hãng duyệt',
        kpiBonusPointsMeta: '+{{pts}} điểm thưởng CSAT đã cộng',
        dealerFilterLabel: 'Chi nhánh đại lý',
        allBranchesOption: 'Tất cả chi nhánh đại lý',
        claimStatusLabel: 'Trạng thái bảo hành',
        allStatusesOption: 'Tất cả trạng thái bảo hành',
        searchLabel: 'Tìm chi nhánh, mã hồ sơ/đánh giá, VIN hoặc khách hàng',
        searchPlaceholder: 'Tìm mã chi nhánh, mã hồ sơ, số khung VIN 17 ký tự hoặc khách hàng...',
        resetBtn: 'Đặt lại',
        matrixTitle: 'Ma trận so sánh CSAT & Bảo hành Đa Đại lý',
        matrixSubtitle: 'Đối chiếu chất lượng dịch vụ 5 sao và chi phí bảo hành OEM giữa các chi nhánh trên toàn quốc.',
        branchCountBadge: '{{count}} chi nhánh',
        colCode: 'Mã chi nhánh',
        colDealerBranch: 'Chi nhánh đại lý',
        colAvgCsat: 'CSAT trung bình',
        colFiveStarShare: 'Tỷ lệ 5 sao',
        colReviewsUnanswered: 'Đánh giá / Chưa phản hồi',
        colOpenClaims: 'Hồ sơ mở',
        colCompletedRejected: 'Hoàn tất / Từ chối',
        colCoveredVnd: 'Chi trả bảo hành (VNĐ)',
        totalReviewsSuffix: '{{count}} tổng',
        unansweredCountBadge: '{{count}} chưa trả lời',
        openCountBadge: '{{count}} mở',
        doneRejectedSummary: '{{done}} hoàn tất / {{rej}} từ chối',
        notAvailable: 'Chưa có',
        emptyBranchesTitle: 'Không có chi nhánh phù hợp bộ lọc',
        emptyBranchesDesc: 'Hãy đặt lại bộ lọc chi nhánh hoặc từ khóa để xem toàn bộ mạng lưới đại lý.',
        tabWarranty: 'Hồ sơ Bảo hành Toàn quốc',
        tabReviews: 'Đánh giá CSAT Toàn quốc',
        colClaimNo: 'Mã hồ sơ',
        colBranch: 'Chi nhánh',
        colVinModel: 'Số khung VIN & Mẫu xe',
        colDefectOdo: 'Nhóm lỗi & ODO',
        colCoveredShort: 'Hãng chi trả',
        emptyClaimsTitle: 'Không có hồ sơ bảo hành phù hợp',
        emptyClaimsDesc: 'Thay đổi trạng thái hoặc phạm vi chi nhánh để xem các hồ sơ bảo hành VIN khác.',
        colReviewNo: 'Mã đánh giá',
        colRating: 'Đánh giá sao',
        colCustomerFeedback: 'Khách hàng & Nhận xét',
        colOfficialResponse: 'Phản hồi chính thức',
        unansweredBadge: 'Chưa phản hồi',
        emptyReviewsTitle: 'Không có đánh giá CSAT phù hợp',
        emptyReviewsDesc: 'Thay đổi chi nhánh hoặc từ khóa tìm kiếm để xem thêm đánh giá dịch vụ từ khách hàng.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      qualityWarranty: {
        screenCode: 'SCR-ADM-08 · v1.2 Multi-Dealer CSAT & OEM Warranty Governance',
        liveActivityBadge: 'Live SignalR Activity',
        backToAnalytics: 'Revenue Analytics',
        refreshing: 'Refreshing...',
        refreshCta: 'Refresh HQ Data',
        kpiAvgCsat: 'Network Average CSAT',
        kpiFiveStarMeta: '{{pct}}% 5-star share ({{count}} reviews)',
        kpiUnanswered: 'Unanswered CSAT Reviews',
        kpiResponseRateMeta: '{{pct}}% official branch response rate',
        kpiOpenClaims: 'Open VIN Warranty Claims',
        kpiSettledRejectedMeta: '{{done}} settled · {{rej}} rejected',
        kpiCoveredCost: 'Approved Warranty Cost',
        kpiBonusPointsMeta: '+{{pts}} CSAT bonus pts awarded',
        dealerFilterLabel: 'Dealership Branch',
        allBranchesOption: 'All Dealership Branches',
        claimStatusLabel: 'Warranty Status',
        allStatusesOption: 'All Warranty Statuses',
        searchLabel: 'Search Branch, Claim/Review #, VIN, or Customer',
        searchPlaceholder: 'Search by branch code, claim #, review #, 17-character VIN, or customer...',
        resetBtn: 'Reset',
        matrixTitle: 'Multi-Dealer CSAT & Warranty Comparison Matrix',
        matrixSubtitle: 'Real-time service quality and OEM warranty exposure benchmark across nationwide branches.',
        branchCountBadge: '{{count}} Branch(es)',
        colCode: 'Code',
        colDealerBranch: 'Dealership Branch',
        colAvgCsat: 'Avg CSAT',
        colFiveStarShare: '5-Star Share',
        colReviewsUnanswered: 'Reviews / Unanswered',
        colOpenClaims: 'Open Claims',
        colCompletedRejected: 'Completed / Rejected',
        colCoveredVnd: 'Covered Warranty VND',
        totalReviewsSuffix: '{{count}} total',
        unansweredCountBadge: '{{count}} unanswered',
        openCountBadge: '{{count}} open',
        doneRejectedSummary: '{{done}} done / {{rej}} rej.',
        notAvailable: 'N/A',
        emptyBranchesTitle: 'No branches match the selected filter',
        emptyBranchesDesc: 'Reset the branch scope or keyword filter to inspect the full nationwide network.',
        tabWarranty: 'Nationwide Warranty Claims',
        tabReviews: 'Nationwide CSAT Reviews',
        colClaimNo: 'Claim #',
        colBranch: 'Branch',
        colVinModel: 'VIN & Model',
        colDefectOdo: 'Defect & Odo',
        colCoveredShort: 'Covered VND',
        emptyClaimsTitle: 'No warranty claims match current filters',
        emptyClaimsDesc: 'Adjust the branch or warranty status filters to inspect additional VIN warranty claims.',
        colReviewNo: 'Review #',
        colRating: 'Rating',
        colCustomerFeedback: 'Customer & Feedback',
        colOfficialResponse: 'Official Response',
        unansweredBadge: 'Unanswered',
        emptyReviewsTitle: 'No CSAT reviews match current filters',
        emptyReviewsDesc: 'Try resetting the branch or keyword filters to browse nationwide customer ratings.'
      }
    }
  };

  registerQualityWarrantyI18n.$inject = ['I18nService'];
  function registerQualityWarrantyI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerQualityWarrantyI18n)
    .component('adminQualityWarranty', {
      controllerAs: '$ctrl',
      controller: AdminQualityWarrantyController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.qualityWarranty.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.qualityWarrantyTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.qualityWarrantySubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<span class="badge badge-danger mono-num is-signalr-pulse" ng-if="$ctrl.hasLiveUpdates">' +
                '<app-icon name="broadcast" weight="fill" size="13"></app-icon>' +
                '<span>{{ \'admin.qualityWarranty.liveActivityBadge\' | t }}</span>' +
              '</span>' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.qualityWarranty.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn-pill-cta" ng-click="$ctrl.loadOversightData()" ng-disabled="$ctrl.isLoading">' +
                '<span>{{ ($ctrl.isLoading ? \'admin.qualityWarranty.refreshing\' : \'admin.qualityWarranty.refreshCta\') | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.loadProblem ? ($ctrl.loadProblem.title + \': \' + $ctrl.loadProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.loadProblem && $ctrl.loadProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadOversightData()">' +
          '</error-retry-banner>' +

          /* Initial Skeleton Loaders */
          '<div ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" style="margin-bottom:20px;display:flex;flex-direction:column;gap:16px;">' +
            '<skeleton-loader variant="kpi" count="4"></skeleton-loader>' +
            '<skeleton-loader variant="table" count="4"></skeleton-loader>' +
          '</div>' +

          /* 4 Executive Double-Bezel KPI Cards */
          '<div class="bento-grid" style="margin-bottom:20px;" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<div class="col-3 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:8px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'admin.qualityWarranty.kpiAvgCsat\' | t }}</span>' +
                  '<app-icon name="star" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                  '<div class="kpi-value mono-num" style="margin-top:0;" kpi-counter="$ctrl.networkKpis.averageCsat" kpi-format="decimal">' +
                    '{{ $ctrl.networkKpis.averageCsat | number:2 }}' +
                  '</div>' +
                  '<star-rating value="$ctrl.networkKpis.averageCsat" readonly="true" size="14"></star-rating>' +
                '</div>' +
                '<div class="kpi-meta">{{ \'admin.qualityWarranty.kpiFiveStarMeta\' | t:{ pct: ($ctrl.networkKpis.fiveStarSharePct | number:1), count: $ctrl.networkKpis.totalReviews } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:8px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'admin.qualityWarranty.kpiUnanswered\' | t }}</span>' +
                  '<app-icon name="chat-circle-text" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="margin-top:0;" ' +
                     'ng-style="{color: $ctrl.networkKpis.unansweredReviews > 0 ? \'var(--clr-warning-fg)\' : \'var(--clr-success-fg)\'}" ' +
                     'kpi-counter="$ctrl.networkKpis.unansweredReviews" kpi-format="number">' +
                  '{{ $ctrl.networkKpis.unansweredReviews }}' +
                '</div>' +
                '<div class="kpi-meta">{{ \'admin.qualityWarranty.kpiResponseRateMeta\' | t:{ pct: ($ctrl.networkKpis.responseRatePct | number:1) } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:8px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'admin.qualityWarranty.kpiOpenClaims\' | t }}</span>' +
                  '<app-icon name="shield-check" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="margin-top:0;" kpi-counter="$ctrl.networkKpis.openClaims" kpi-format="number">' +
                  '{{ $ctrl.networkKpis.openClaims }}' +
                '</div>' +
                '<div class="kpi-meta">{{ \'admin.qualityWarranty.kpiSettledRejectedMeta\' | t:{ done: $ctrl.networkKpis.completedClaims, rej: $ctrl.networkKpis.rejectedClaims } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:3;">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:8px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="kpi-label">{{ \'admin.qualityWarranty.kpiCoveredCost\' | t }}</span>' +
                  '<app-icon name="receipt" weight="duotone" size="20" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" style="font-size:19px;margin-top:0;" kpi-counter="$ctrl.networkKpis.coveredVnd" kpi-format="vnd">' +
                  '{{ $ctrl.networkKpis.coveredVnd | vndCurrency }}' +
                '</div>' +
                '<div class="kpi-meta">{{ \'admin.qualityWarranty.kpiBonusPointsMeta\' | t:{ pts: ($ctrl.networkKpis.totalBonusPoints | number) } }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Toolbar */
          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admQwDealerSelect">{{ \'admin.qualityWarranty.dealerFilterLabel\' | t }}</label>' +
                  '<select id="admQwDealerSelect" class="form-control" ' +
                          'ng-model="$ctrl.filters.dealerId" ' +
                          'ng-options="d.value as d.label for d in $ctrl.dealerOptions" ' +
                          'ng-change="$ctrl.onDealerFilterChanged()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admQwClaimStatus">{{ \'admin.qualityWarranty.claimStatusLabel\' | t }}</label>' +
                  '<select id="admQwClaimStatus" class="form-control" ' +
                          'ng-model="$ctrl.filters.claimStatus" ' +
                          'ng-options="s.value as s.label for s in $ctrl.claimStatusOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admQwSearch">{{ \'admin.qualityWarranty.searchLabel\' | t }}</label>' +
                  '<input id="admQwSearch" type="search" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'admin.qualityWarranty.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">{{ \'admin.qualityWarranty.resetBtn\' | t }}</button>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Multi-Dealer Branch Comparison Table */
          '<div class="bezel-card quiet-table-frame" style="margin-bottom:24px;" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<div class="bezel-card-inner">' +
              '<div class="surface-card-header" style="margin-bottom:12px;">' +
                '<div>' +
                  '<h2 class="surface-card-title">{{ \'admin.qualityWarranty.matrixTitle\' | t }}</h2>' +
                  '<div class="text-muted" style="font-size:12px;">{{ \'admin.qualityWarranty.matrixSubtitle\' | t }}</div>' +
                '</div>' +
                '<span class="badge badge-info mono-num">{{ \'admin.qualityWarranty.branchCountBadge\' | t:{ count: $ctrl.filteredBranchRows.length } }}</span>' +
              '</div>' +

              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredBranchRows.length === 0" ' +
                           'icon="buildings" ' +
                           'title-key="admin.qualityWarranty.emptyBranchesTitle" ' +
                           'desc-key="admin.qualityWarranty.emptyBranchesDesc" ' +
                           'action-key="admin.qualityWarranty.resetBtn" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredBranchRows.length > 0">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th>{{ \'admin.qualityWarranty.colCode\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colDealerBranch\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colAvgCsat\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colFiveStarShare\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colReviewsUnanswered\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colOpenClaims\' | t }}</th>' +
                      '<th>{{ \'admin.qualityWarranty.colCompletedRejected\' | t }}</th>' +
                      '<th style="text-align:right;">{{ \'admin.qualityWarranty.colCoveredVnd\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="row in $ctrl.filteredBranchRows track by row.dealerId">' +
                      '<td class="mono-num" style="font-weight:700;">{{ row.dealerCode }}</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">{{ row.dealerName }}</div>' +
                        '<div class="text-muted" style="font-size:11px;">{{ row.city }}</div>' +
                      '</td>' +
                      '<td class="mono-num">' +
                        '<span class="badge" ng-class="row.totalReviews === 0 ? \'badge-neutral\' : (row.avgCsat >= 4.5 ? \'badge-success\' : \'badge-warning\')">' +
                          '<app-icon name="star" weight="fill" size="12" ng-if="row.totalReviews > 0"></app-icon>' +
                          '<span>{{ row.totalReviews > 0 ? (row.avgCsat | number:2) : (\'admin.qualityWarranty.notAvailable\' | t) }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td class="mono-num">{{ row.fiveStarSharePct | number:1 }}%</td>' +
                      '<td class="mono-num">' +
                        '<span>{{ \'admin.qualityWarranty.totalReviewsSuffix\' | t:{ count: row.totalReviews } }}</span>' +
                        '<span class="badge badge-warning" style="margin-left:6px;" ng-if="row.unansweredReviews > 0">{{ \'admin.qualityWarranty.unansweredCountBadge\' | t:{ count: row.unansweredReviews } }}</span>' +
                      '</td>' +
                      '<td class="mono-num">' +
                        '<span class="badge" ng-class="row.openClaims > 0 ? \'badge-info\' : \'badge-neutral\'">{{ \'admin.qualityWarranty.openCountBadge\' | t:{ count: row.openClaims } }}</span>' +
                      '</td>' +
                      '<td class="mono-num">{{ \'admin.qualityWarranty.doneRejectedSummary\' | t:{ done: row.completedClaims, rej: row.rejectedClaims } }}</td>' +
                      '<td class="mono-num" style="text-align:right;font-weight:700;">{{ row.coveredVnd | vndCurrency }}</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Drill-Down Feed Tabs */
          '<div class="tab-strip" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeFeedTab === \'WARRANTY\'}" ' +
                    'ng-click="$ctrl.activeFeedTab = \'WARRANTY\'">' +
              '<app-icon name="shield-check" weight="duotone" size="16"></app-icon>' +
              '<span>{{ \'admin.qualityWarranty.tabWarranty\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.filteredClaims.length }})</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeFeedTab === \'REVIEWS\'}" ' +
                    'ng-click="$ctrl.activeFeedTab = \'REVIEWS\'">' +
              '<app-icon name="star" weight="fill" size="16"></app-icon>' +
              '<span>{{ \'admin.qualityWarranty.tabReviews\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.filteredReviews.length }})</span>' +
            '</button>' +
          '</div>' +

          /* Feed 1: Nationwide Warranty Claims */
          '<div class="bezel-card quiet-table-frame" ng-if="$ctrl.activeFeedTab === \'WARRANTY\'" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredClaims.length === 0" ' +
                           'icon="shield-check" ' +
                           'title-key="admin.qualityWarranty.emptyClaimsTitle" ' +
                           'desc-key="admin.qualityWarranty.emptyClaimsDesc" ' +
                           'action-key="admin.qualityWarranty.resetBtn" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredClaims.length > 0" style="max-height:540px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colClaimNo\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colBranch\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colVinModel\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colDefectOdo\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;text-align:right;">{{ \'admin.qualityWarranty.colCoveredShort\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="clm in $ctrl.filteredClaims track by (clm.id || clm.claimNumber)">' +
                      '<td class="mono-num" style="font-weight:700;">{{ clm.claimNumber }}</td>' +
                      '<td class="mono-num">{{ clm.dealerCode || clm.dealerName }}</td>' +
                      '<td>' +
                        '<div class="mono-num text-primary" style="font-weight:600;">{{ clm.vin }}</div>' +
                        '<div class="text-muted" style="font-size:12px;">{{ clm.motorcycleModelName }} · {{ clm.customerFullName }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge badge-neutral">{{ clm.defectCategory | enumT:\'WarrantyDefectCategory\' }}</span>' +
                        '<div class="mono-num text-muted" style="font-size:11px;margin-top:3px;">{{ clm.odometerAtClaimKm | number }} km</div>' +
                      '</td>' +
                      '<td><span class="badge" ng-class="\'badge-status-\' + clm.status.toLowerCase()">{{ clm.status | enumT:\'WarrantyClaimStatus\' }}</span></td>' +
                      '<td class="mono-num" style="text-align:right;font-weight:700;">{{ clm.coveredAmountVnd | vndCurrency }}</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Feed 2: Nationwide CSAT Reviews */
          '<div class="bezel-card quiet-table-frame" ng-if="$ctrl.activeFeedTab === \'REVIEWS\'" ng-show="$ctrl.hasLoadedOnce || !$ctrl.isLoading">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredReviews.length === 0" ' +
                           'icon="star" ' +
                           'title-key="admin.qualityWarranty.emptyReviewsTitle" ' +
                           'desc-key="admin.qualityWarranty.emptyReviewsDesc" ' +
                           'action-key="admin.qualityWarranty.resetBtn" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredReviews.length > 0" style="max-height:540px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colReviewNo\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colBranch\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colRating\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colCustomerFeedback\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.qualityWarranty.colOfficialResponse\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="rev in $ctrl.filteredReviews track by (rev.id || rev.reviewNumber)">' +
                      '<td class="mono-num" style="font-weight:700;">{{ rev.reviewNumber }}</td>' +
                      '<td class="mono-num">{{ rev.dealerCode || rev.dealerName }}</td>' +
                      '<td>' +
                        '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                          '<star-rating value="rev.rating" readonly="true" size="14"></star-rating>' +
                          '<span class="badge badge-tier-gold mono-num">{{ rev.rating }} / 5</span>' +
                        '</div>' +
                      '</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">{{ rev.customerFullName }} · <span class="text-muted">{{ rev.motorcycleModelName }}</span></div>' +
                        '<div class="text-secondary" style="font-size:12px;margin-top:2px;" ng-if="rev.comment">“{{ rev.comment }}”</div>' +
                      '</td>' +
                      '<td>' +
                        '<span ng-if="rev.staffResponse" class="text-primary" style="font-size:12px;">{{ rev.staffResponse }}</span>' +
                        '<span class="badge badge-warning" ng-if="!rev.staffResponse">{{ \'admin.qualityWarranty.unansweredBadge\' | t }}</span>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminQualityWarrantyController.$inject = [
    '$q',
    '$rootScope',
    'DealerApiService',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    '$scope',
    'I18nService'
  ];
  function AdminQualityWarrantyController(
    $q,
    $rootScope,
    DealerApiService,
    ProblemDetailsMapper,
    SignalRClientService,
    ToastService,
    TokenStorageService,
    $scope,
    I18nService
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;

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

    function round2(n) {
      var val = Number(n);
      if (!isFinite(val)) { return 0; }
      return Math.round(val * 100) / 100;
    }

    function rebuildClaimStatusOptions() {
      $ctrl.claimStatusOptions = [
        { value: '', label: t('admin.qualityWarranty.allStatusesOption', 'All Warranty Statuses') },
        { value: 'Submitted', label: enumLabel('WarrantyClaimStatus', 'Submitted') },
        { value: 'UnderReview', label: enumLabel('WarrantyClaimStatus', 'UnderReview') },
        { value: 'Approved', label: enumLabel('WarrantyClaimStatus', 'Approved') },
        { value: 'Completed', label: enumLabel('WarrantyClaimStatus', 'Completed') },
        { value: 'Rejected', label: enumLabel('WarrantyClaimStatus', 'Rejected') }
      ];
    }

    function rebuildDealerDropdownAndMatrix() {
      var dealers = $ctrl.dealers || [];
      var reviews = $ctrl.reviews || [];
      var claims = $ctrl.claims || [];

      var opts = [{ value: '', label: t('admin.qualityWarranty.allBranchesOption', 'All Dealership Branches') }];
      var statsByDealerKey = Object.create(null);
      var orderedRows = [];

      for (var i = 0; i < dealers.length; i++) {
        var d = dealers[i];
        opts.push({ value: d.id, label: d.code + ' · ' + d.name });
        var entry = {
          dealerId: d.id,
          dealerCode: d.code,
          dealerName: d.name,
          city: d.city,
          ratingSum: 0,
          avgCsat: 0,
          totalReviews: 0,
          fiveStarReviews: 0,
          fiveStarSharePct: 0,
          unansweredReviews: 0,
          totalBonusPoints: 0,
          openClaims: 0,
          completedClaims: 0,
          rejectedClaims: 0,
          coveredVnd: 0
        };
        statsByDealerKey[d.id] = entry;
        if (d.code) {
          statsByDealerKey[d.code.toUpperCase()] = entry;
        }
        orderedRows.push(entry);
      }

      var netRatingSum = 0;
      var netFiveStar = 0;
      var netUnanswered = 0;
      var netBonusPts = 0;
      for (var rIdx = 0; rIdx < reviews.length; rIdx++) {
        var rev = reviews[rIdx];
        netRatingSum += rev.rating;
        if (rev.rating === 5) { netFiveStar++; }
        if (!rev.hasStaffResponse) { netUnanswered++; }
        netBonusPts += rev.bonusPointsAwarded || 0;

        var dHit = statsByDealerKey[rev.dealerId] || (rev.dealerCode ? statsByDealerKey[rev.dealerCode.toUpperCase()] : null);
        if (dHit) {
          dHit.totalReviews++;
          dHit.ratingSum += rev.rating;
          if (rev.rating === 5) { dHit.fiveStarReviews++; }
          if (!rev.hasStaffResponse) { dHit.unansweredReviews++; }
          dHit.totalBonusPoints += rev.bonusPointsAwarded || 0;
        }
      }

      var netOpen = 0;
      var netCompleted = 0;
      var netRejected = 0;
      var netCoveredVnd = 0;
      for (var cIdx = 0; cIdx < claims.length; cIdx++) {
        var clm = claims[cIdx];
        var st = clm.status;
        var isOpenClaim = DealerApiService.isOpenWarrantyStatus(st);
        if (isOpenClaim) {
          netOpen++;
        } else if (st === 'Completed') {
          netCompleted++;
        } else if (st === 'Rejected') {
          netRejected++;
        }
        if (st === 'Approved' || st === 'Completed') {
          netCoveredVnd += clm.coveredAmountVnd || 0;
        }

        var cHit = statsByDealerKey[clm.dealerId] || (clm.dealerCode ? statsByDealerKey[clm.dealerCode.toUpperCase()] : null);
        if (cHit) {
          if (isOpenClaim) {
            cHit.openClaims++;
          } else if (st === 'Completed') {
            cHit.completedClaims++;
          } else if (st === 'Rejected') {
            cHit.rejectedClaims++;
          }
          if (st === 'Approved' || st === 'Completed') {
            cHit.coveredVnd += clm.coveredAmountVnd || 0;
          }
        }
      }

      var qa = $ctrl.qualityAnalytics || null;
      var qaDealers = qa && angular.isArray(qa.dealers) ? qa.dealers : [];
      var qaByDealerKey = Object.create(null);
      for (var qIdx = 0; qIdx < qaDealers.length; qIdx++) {
        var qd = qaDealers[qIdx];
        if (!qd) { continue; }
        if (qd.dealerId) {
          qaByDealerKey[qd.dealerId] = qd;
        }
        if (qd.dealerCode) {
          qaByDealerKey[String(qd.dealerCode).toUpperCase()] = qd;
        }
      }

      for (var k = 0; k < orderedRows.length; k++) {
        var row = orderedRows[k];
        var qaHit = qaByDealerKey[row.dealerId] || (row.dealerCode ? qaByDealerKey[row.dealerCode.toUpperCase()] : null);
        if (qaHit) {
          if (qaHit.totalServiceReviewsCount > row.totalReviews) {
            row.totalReviews = qaHit.totalServiceReviewsCount;
            row.fiveStarReviews = qaHit.fiveStarReviewsCount;
            row.unansweredReviews = qaHit.unansweredReviewsCount;
            row.avgCsat = qaHit.averageCsatRating;
          }
          if (qaHit.openWarrantyClaimsCount > row.openClaims) {
            row.openClaims = qaHit.openWarrantyClaimsCount;
          }
          if (qaHit.completedWarrantyClaimsCount > row.completedClaims) {
            row.completedClaims = qaHit.completedWarrantyClaimsCount;
          }
          if ((qaHit.rejectedWarrantyClaimsCount || 0) > row.rejectedClaims) {
            row.rejectedClaims = qaHit.rejectedWarrantyClaimsCount || 0;
          }
          if (qaHit.approvedWarrantyCoveredVnd > row.coveredVnd) {
            row.coveredVnd = qaHit.approvedWarrantyCoveredVnd;
          }
        }
        if (row.totalReviews > 0 && row.ratingSum > 0 && (!qaHit || qaHit.totalServiceReviewsCount <= row.totalReviews)) {
          row.avgCsat = round2(row.ratingSum / row.totalReviews);
        }
        row.fiveStarSharePct = row.totalReviews > 0 ? round2((row.fiveStarReviews * 100) / row.totalReviews) : 0;
      }

      var totalRevs = reviews.length;
      var overview = $ctrl.overview || {};
      var hasServerQaMetrics = Boolean(
        qa && (
          qa.totalReviewsCount > 0 ||
          qa.totalWarrantyClaimsCount > 0 ||
          qa.approvedWarrantyCoveredVnd > 0 ||
          qaDealers.length > 0
        )
      );
      var resolvedTotalReviews = hasServerQaMetrics && qa.totalReviewsCount > totalRevs
        ? qa.totalReviewsCount
        : (totalRevs || (qa && qa.totalReviewsCount) || overview.totalServiceReviewsCount || 0);
      var resolvedFiveStar = hasServerQaMetrics && qa.fiveStarReviewsCount > netFiveStar
        ? qa.fiveStarReviewsCount
        : netFiveStar;
      var resolvedUnanswered = hasServerQaMetrics && qa.unansweredReviewsCount > netUnanswered
        ? qa.unansweredReviewsCount
        : netUnanswered;
      var effectiveAvgCsat = hasServerQaMetrics && qa.averageCsatRating > 0
        ? qa.averageCsatRating
        : (totalRevs > 0 ? round2(netRatingSum / totalRevs) : (overview.averageCsatRating || 0));
      var effectiveCoveredVnd = hasServerQaMetrics && qa.approvedWarrantyCoveredVnd > netCoveredVnd
        ? qa.approvedWarrantyCoveredVnd
        : (netCoveredVnd > 0 ? netCoveredVnd : (overview.approvedWarrantyCoveredVnd || 0));
      var effectiveOpenClaims = hasServerQaMetrics && qa.openWarrantyClaimsCount > netOpen
        ? qa.openWarrantyClaimsCount
        : (claims.length > 0 ? netOpen : (overview.openWarrantyClaimsCount || 0));
      var effectiveCompletedClaims = hasServerQaMetrics && qa.completedWarrantyClaimsCount > netCompleted
        ? qa.completedWarrantyClaimsCount
        : netCompleted;
      var effectiveRejectedClaims = hasServerQaMetrics && qa.rejectedWarrantyClaimsCount > netRejected
        ? qa.rejectedWarrantyClaimsCount
        : netRejected;
      var effectiveRecommendPct = hasServerQaMetrics && qa.wouldRecommendRatePercent > 0
        ? qa.wouldRecommendRatePercent
        : (resolvedTotalReviews > 0 ? round2((resolvedFiveStar * 100) / resolvedTotalReviews) : 0);

      $ctrl.dealerOptions = opts;
      $ctrl.branchRows = orderedRows;
      $ctrl.defectBreakdown = (qa && angular.isArray(qa.defectBreakdown)) ? qa.defectBreakdown : [];
      $ctrl.networkKpis = {
        averageCsat: effectiveAvgCsat,
        totalReviews: resolvedTotalReviews,
        fiveStarSharePct: resolvedTotalReviews > 0 ? round2((resolvedFiveStar * 100) / resolvedTotalReviews) : 0,
        wouldRecommendRatePct: effectiveRecommendPct,
        unansweredReviews: resolvedUnanswered,
        responseRatePct: resolvedTotalReviews > 0 ? round2(((resolvedTotalReviews - resolvedUnanswered) * 100) / resolvedTotalReviews) : 100,
        openClaims: effectiveOpenClaims,
        completedClaims: effectiveCompletedClaims,
        rejectedClaims: effectiveRejectedClaims,
        coveredVnd: effectiveCoveredVnd,
        totalBonusPoints: netBonusPts || (resolvedTotalReviews * 25)
      };
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.hasLoadedOnce = false;
      $ctrl.loadProblem = null;
      $ctrl.hasLiveUpdates = false;
      $ctrl.activeFeedTab = 'WARRANTY';

      rebuildClaimStatusOptions();

      var focusHandoff = (DealerApiService && angular.isFunction(DealerApiService.consumeAdminFocusContext))
        ? DealerApiService.consumeAdminFocusContext()
        : null;
      var initialDealerId = (focusHandoff && focusHandoff.dealerId) ? String(focusHandoff.dealerId).trim() : '';
      $ctrl.filters = {
        dealerId: initialDealerId,
        claimStatus: '',
        search: (!initialDealerId && focusHandoff && focusHandoff.dealerCode) ? String(focusHandoff.dealerCode).trim() : ''
      };

      $ctrl.dealers = [];
      $ctrl.reviews = [];
      $ctrl.claims = [];
      $ctrl.overview = null;
      $ctrl.qualityAnalytics = null;
      $ctrl.defectBreakdown = [];

      rebuildDealerDropdownAndMatrix();
      $ctrl.applyFilters();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        var liveEvents = ['ServiceReviewSubmitted', 'WarrantyClaimCreated', 'WarrantyClaimStatusChanged'];
        for (var i = 0; i < liveEvents.length; i++) {
          cleanupFns.push(SignalRClientService.on(liveEvents[i], function () {
            if (isDestroyed) { return; }
            $ctrl.hasLiveUpdates = true;
            $ctrl.loadOversightData();
          }));
        }
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadOversightData();
          }
        }));
      }

      var localeBus = ($scope && angular.isFunction($scope.$on))
        ? $scope
        : (($rootScope && angular.isFunction($rootScope.$on)) ? $rootScope : null);
      if (localeBus) {
        cleanupFns.push(localeBus.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildClaimStatusOptions();
            rebuildDealerDropdownAndMatrix();
            $ctrl.applyFilters();
          }
        }));
      }

      $ctrl.loadOversightData();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        if (angular.isFunction(cleanupFns[i])) {
          cleanupFns[i]();
        }
      }
      cleanupFns.length = 0;
    };

    $ctrl.loadOversightData = function () {
      $ctrl.isLoading = true;
      $ctrl.loadProblem = null;

      var scopedDealerId = ($ctrl.filters && $ctrl.filters.dealerId) ? $ctrl.filters.dealerId : undefined;
      var qualityAnalyticsPromise = angular.isFunction(DealerApiService.getAnalyticsQualityWarranty)
        ? DealerApiService.getAnalyticsQualityWarranty({ dealerId: scopedDealerId }).catch(function () { return null; })
        : $q.resolve(null);

      return $q.all({
        dealers: DealerApiService.listDealers({ includeInactive: true }).catch(function () { return null; }),
        reviews: DealerApiService.getDealerServiceReviews({ pageSize: 100 }),
        claims: DealerApiService.getDealerWarrantyClaims({ pageSize: 100 }),
        overview: DealerApiService.getAnalyticsOverview({ dealerId: scopedDealerId }).catch(function () { return null; }),
        qualityWarranty: qualityAnalyticsPromise
      }).then(function (bundle) {
        if (isDestroyed) { return $ctrl.branchRows; }
        if (angular.isArray(bundle.dealers) && bundle.dealers.length > 0) {
          $ctrl.dealers = bundle.dealers;
        }
        $ctrl.reviews = bundle.reviews || [];
        $ctrl.claims = bundle.claims || [];
        $ctrl.overview = bundle.overview || null;
        $ctrl.qualityAnalytics = bundle.qualityWarranty || null;
        $ctrl.hasLiveUpdates = false;
        $ctrl.hasLoadedOnce = true;
        rebuildDealerDropdownAndMatrix();
        $ctrl.applyFilters();
        return $ctrl.branchRows;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.loadProblem = prob;
        }
        return $ctrl.branchRows;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.applyFilters = function () {
      var dId = ($ctrl.filters && $ctrl.filters.dealerId) || '';
      var cSt = ($ctrl.filters && $ctrl.filters.claimStatus) || '';
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();

      var selectedDealerCode = '';
      if (dId) {
        for (var i = 0; i < $ctrl.dealers.length; i++) {
          if ($ctrl.dealers[i].id === dId) {
            selectedDealerCode = ($ctrl.dealers[i].code || '').toUpperCase();
            break;
          }
        }
      }

      function matchesDealerScope(itemDealerId, itemDealerCode) {
        if (!dId) { return true; }
        if (itemDealerId && itemDealerId === dId) { return true; }
        if (selectedDealerCode && itemDealerCode && itemDealerCode.toUpperCase() === selectedDealerCode) { return true; }
        return false;
      }

      $ctrl.filteredBranchRows = ($ctrl.branchRows || []).filter(function (row) {
        if (!matchesDealerScope(row.dealerId, row.dealerCode)) { return false; }
        if (q) {
          var hay = (row.dealerCode + ' ' + row.dealerName + ' ' + row.city).toLowerCase();
          if (hay.indexOf(q) === -1) { return false; }
        }
        return true;
      });

      $ctrl.filteredClaims = ($ctrl.claims || []).filter(function (clm) {
        if (!matchesDealerScope(clm.dealerId, clm.dealerCode)) { return false; }
        if (cSt && clm.status !== cSt) { return false; }
        if (q) {
          var hay = (
            clm.claimNumber + ' ' +
            clm.dealerCode + ' ' +
            clm.dealerName + ' ' +
            clm.vin + ' ' +
            clm.motorcycleModelName + ' ' +
            clm.customerFullName
          ).toLowerCase();
          if (hay.indexOf(q) === -1) { return false; }
        }
        return true;
      });

      $ctrl.filteredReviews = ($ctrl.reviews || []).filter(function (rev) {
        if (!matchesDealerScope(rev.dealerId, rev.dealerCode)) { return false; }
        if (q) {
          var hay = (
            rev.reviewNumber + ' ' +
            rev.dealerCode + ' ' +
            rev.dealerName + ' ' +
            rev.vin + ' ' +
            rev.motorcycleModelName + ' ' +
            rev.customerFullName + ' ' +
            (rev.comment || '')
          ).toLowerCase();
          if (hay.indexOf(q) === -1) { return false; }
        }
        return true;
      });
    };

    $ctrl.onDealerFilterChanged = function () {
      $ctrl.applyFilters();
      if (angular.isFunction(DealerApiService.getAnalyticsQualityWarranty)) {
        DealerApiService.getAnalyticsQualityWarranty({
          dealerId: ($ctrl.filters && $ctrl.filters.dealerId) || undefined
        }).then(function (qa) {
          if (isDestroyed) { return; }
          $ctrl.qualityAnalytics = qa || null;
          rebuildDealerDropdownAndMatrix();
          $ctrl.applyFilters();
        }).catch(angular.noop);
      }
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = { dealerId: '', claimStatus: '', search: '' };
      $ctrl.onDealerFilterChanged();
    };
  }
})();
