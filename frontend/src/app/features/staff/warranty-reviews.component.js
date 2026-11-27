(function () {
  'use strict';

  var ALLOWED_WARRANTY_TRANSITIONS = {
    Submitted: [
      { value: 'UnderReview', labelKey: 'staff.wr.transUnderReview' },
      { value: 'Approved', labelKey: 'staff.wr.transApproved' },
      { value: 'Rejected', labelKey: 'staff.wr.transRejected' }
    ],
    UnderReview: [
      { value: 'Approved', labelKey: 'staff.wr.transApproved' },
      { value: 'Rejected', labelKey: 'staff.wr.transRejected' }
    ],
    Approved: [
      { value: 'Completed', labelKey: 'staff.wr.transCompleted' }
    ]
  };

  var VI_PATCH = {
    staff: {
      wr: {
        eyebrow: 'SCR-STF-07 • BẢO HÀNH VIN & ĐÁNH GIÁ CSAT',
        liveNewBadge: '{{count}} mới trực tiếp',
        syncDesk: 'Đồng bộ bàn xử lý',
        syncing: 'Đang đồng bộ...',
        backToWorkbench: 'Về bảng điều phối',
        openTabWarranty: 'Mở hồ sơ Bảo hành',
        openTabReviews: 'Mở đánh giá CSAT',
        dismiss: 'Ẩn',
        kpiAvgCsat: 'Điểm CSAT trung bình',
        kpiAvgCsatMeta: '{{total}} đánh giá ({{fiveStar}} đạt 5/5)',
        kpiUnanswered: 'Đánh giá chưa phản hồi',
        kpiUnansweredMeta: 'Đang chờ phản hồi chính thức từ đại lý',
        kpiOpenClaims: 'Hồ sơ bảo hành đang mở',
        kpiOpenClaimsMeta: 'Đã tiếp nhận • Đang thẩm định • Đã duyệt',
        kpiCoveredVnd: 'Tổng chi trả bảo hành OEM',
        kpiCoveredMeta: '{{completed}} hoàn tất • {{rejected}} từ chối',
        tabWarranty: 'Hồ sơ Bảo hành VIN',
        tabReviews: 'Đánh giá 5 sao CSAT',
        claimStatusLabel: 'Trạng thái hồ sơ',
        allClaimStatuses: 'Tất cả trạng thái bảo hành',
        allDefects: 'Tất cả nhóm hạng mục lỗi',
        searchClaimsLabel: 'Tìm mã hồ sơ, VIN, khách hàng hoặc triệu chứng',
        searchClaimsPlaceholder: 'Nhập mã hồ sơ bảo hành, số khung VIN 17 ký tự, tên khách hoặc triệu chứng...',
        clearShort: 'Xóa',
        emptyClaimsTitle: 'Không có hồ sơ bảo hành VIN nào khớp bộ lọc',
        emptyClaimsDesc: 'Yêu cầu bảo hành mới gửi từ khách hàng cho chi nhánh sẽ tự động hiển thị tại đây qua SignalR.',
        filedAtPrefix: 'Gửi lúc',
        symptomPrefix: 'Triệu chứng',
        coveredAmountPrefix: 'Hãng chi trả',
        oemPartPrefix: 'Phụ tùng OEM',
        assignedFallback: 'Đã gán',
        resolutionNotesPrefix: 'Ghi chú xử lý kỹ thuật',
        rejectionReasonPrefix: 'Lý do từ chối',
        manageClaimCta: 'Thẩm định bảo hành',
        starRatingLabel: 'Mức điểm đánh giá',
        allRatings: 'Tất cả mức sao',
        ratingFiveOnly: 'Chỉ 5/5 sao',
        ratingFourOnly: '4/5 sao',
        ratingThreeOrBelow: '3/5 sao trở xuống',
        replyStatusLabel: 'Trạng thái phản hồi',
        allReviews: 'Tất cả đánh giá',
        unansweredOnly: 'Chưa phản hồi',
        repliedOnly: 'Đã phản hồi',
        searchReviewsLabel: 'Tìm mã đánh giá, số hóa đơn, khách hàng hoặc nhận xét',
        searchReviewsPlaceholder: 'Nhập mã đánh giá, số hóa đơn, khách hàng hoặc nội dung phản hồi...',
        emptyReviewsTitle: 'Không có đánh giá CSAT nào khớp bộ lọc',
        emptyReviewsDesc: 'Đánh giá mới của khách hàng sau bảo dưỡng cho chi nhánh sẽ xuất hiện trực tiếp tại đây qua SignalR.',
        repliedBadge: 'Đã phản hồi',
        unansweredBadge: 'Chưa phản hồi',
        workshopServiceFallback: 'Dịch vụ bảo dưỡng xưởng',
        officialReplyPrefix: 'Phản hồi chính thức chi nhánh',
        updateReplyCta: 'Sửa phản hồi',
        postReplyCta: 'Gửi phản hồi đại lý',
        claimModalTitle: 'Thẩm định Hồ sơ {{claimNumber}} ({{status}})',
        defectPrefix: 'Hạng mục lỗi',
        nextStatusLabel: 'Trạng thái chuyển tiếp mục tiêu *',
        transUnderReview: 'Đang thẩm định — Bắt đầu kiểm tra kỹ thuật tại xưởng',
        transApproved: 'Đã phê duyệt — Chấp thuận bảo hành chính hãng OEM',
        transRejected: 'Từ chối — Ngoài phạm vi bảo hành / Hao mòn tự nhiên',
        transCompleted: 'Hoàn tất — Thay thế phụ tùng bảo hành & Bàn giao xe',
        coveredPartLabel: 'Phụ tùng OEM chính hãng được bảo hành (Tương thích mẫu xe)',
        coveredPartPlaceholder: '-- Chọn phụ tùng OEM thay thế (Không bắt buộc) --',
        coveredVndLabel: 'Giá trị bảo hành hãng chi trả (VNĐ) *',
        resolutionLabelRequired: 'Ghi chú kiểm tra & Xử lý xưởng * (5–1.000 ký tự)',
        resolutionLabelOptional: 'Ghi chú kiểm tra & Xử lý xưởng (Không bắt buộc)',
        resolutionPlaceholder: 'Ghi nhận nguyên nhân gốc, mã phụ tùng OEM đã thay thế và kết quả chạy thử...',
        rejectReasonLabel: 'Lý do từ chối chính thức * (5–500 ký tự)',
        rejectReasonPlaceholder: 'Giải thích rõ lý do tình trạng lỗi nằm ngoài tiêu chuẩn bảo hành OEM...',
        updatingClaimCta: 'Đang cập nhật hồ sơ...',
        confirmTransitionCta: 'Xác nhận chuyển sang {{status}}',
        replyModalTitle: 'Phản hồi Chính thức Đại lý • {{reviewNumber}}',
        replyInputLabel: 'Nội dung phản hồi chính thức * (5–1.000 ký tự)',
        replyInputPlaceholder: 'Cảm ơn khách hàng đã tin tưởng xưởng dịch vụ hoặc phản hồi chi tiết góp ý...',
        postingReplyCta: 'Đang gửi phản hồi...',
        liveClaimHeadline: 'HỒ SƠ BẢO HÀNH VIN MỚI',
        liveReviewHeadline: 'ĐÁNH GIÁ CSAT {{rating}}/5 MỚI',
        liveReviewDetail: '{{customer}} đánh giá {{model}} đạt {{rating}}/5 điểm',
        toastClaimUpdatedTitle: 'Đã cập nhật hồ sơ bảo hành',
        toastClaimUpdatedMsg: '{{claimNumber}} đã chuyển sang {{status}}.',
        toastReplyPostedTitle: 'Đã đăng phản hồi chính thức',
        toastReplyPostedMsg: 'Đã cập nhật phản hồi đại lý cho {{reviewNumber}}.',
        defaultDealerName: 'Đại lý ủy quyền'
      }
    }
  };

  var EN_PATCH = {
    staff: {
      wr: {
        eyebrow: 'SCR-STF-07 • VIN WARRANTY & CSAT DESK',
        liveNewBadge: '{{count}} Live New',
        syncDesk: 'Sync Desk',
        syncing: 'Syncing...',
        backToWorkbench: 'Live Workbench',
        openTabWarranty: 'Open Warranty Claims',
        openTabReviews: 'Open CSAT Reviews',
        dismiss: 'Dismiss',
        kpiAvgCsat: 'Average Branch CSAT',
        kpiAvgCsatMeta: '{{total}} reviews ({{fiveStar}} rated 5/5)',
        kpiUnanswered: 'Unanswered Reviews',
        kpiUnansweredMeta: 'Awaiting official dealership reply',
        kpiOpenClaims: 'Open Warranty Claims',
        kpiOpenClaimsMeta: 'Submitted • Under Review • Approved',
        kpiCoveredVnd: 'Warranty Covered VND',
        kpiCoveredMeta: '{{completed}} completed • {{rejected}} rejected',
        tabWarranty: 'VIN Warranty Claims',
        tabReviews: '5-Star CSAT Reviews',
        claimStatusLabel: 'Claim Status',
        allClaimStatuses: 'All Warranty Statuses',
        allDefects: 'All Defect Categories',
        searchClaimsLabel: 'Search Claim #, VIN, Customer, or Symptom',
        searchClaimsPlaceholder: 'Search by claim number, 17-character VIN, customer name, or symptom...',
        clearShort: 'Clear',
        emptyClaimsTitle: 'No VIN Warranty Claims Match Current Filters',
        emptyClaimsDesc: 'Incoming customer warranty claims for this dealership appear automatically via SignalR.',
        filedAtPrefix: 'Filed',
        symptomPrefix: 'Symptom',
        coveredAmountPrefix: 'Covered',
        oemPartPrefix: 'OEM Part',
        assignedFallback: 'Assigned',
        resolutionNotesPrefix: 'Resolution Notes',
        rejectionReasonPrefix: 'Rejection Reason',
        manageClaimCta: 'Manage Warranty Claim',
        starRatingLabel: 'Star Rating',
        allRatings: 'All Star Ratings',
        ratingFiveOnly: '5 / 5 Only',
        ratingFourOnly: '4 / 5',
        ratingThreeOrBelow: '3 / 5 & Below',
        replyStatusLabel: 'Reply Status',
        allReviews: 'All Reviews',
        unansweredOnly: 'Unanswered Only',
        repliedOnly: 'Replied Only',
        searchReviewsLabel: 'Search Review #, Invoice #, Customer, or Comment',
        searchReviewsPlaceholder: 'Search by review number, invoice number, customer, or feedback...',
        emptyReviewsTitle: 'No Workshop CSAT Reviews Match Current Filters',
        emptyReviewsDesc: 'Submitted customer 5-star reviews for this branch appear live via SignalR.',
        repliedBadge: 'Replied',
        unansweredBadge: 'Unanswered',
        workshopServiceFallback: 'Workshop Service',
        officialReplyPrefix: 'Official Branch Reply',
        updateReplyCta: 'Update Reply',
        postReplyCta: 'Post Official Reply',
        claimModalTitle: 'Manage Warranty Claim {{claimNumber}} ({{status}})',
        defectPrefix: 'Defect',
        nextStatusLabel: 'Target Warranty Status Transition *',
        transUnderReview: 'Under Review — Begin Workshop Inspection',
        transApproved: 'Approved — Authorize OEM Warranty Coverage',
        transRejected: 'Rejected — Non-OEM / Wear-and-Tear Exclusion',
        transCompleted: 'Completed — Settle Warranty Replacement & Release Bike',
        coveredPartLabel: 'Covered Genuine OEM Spare Part (Model-Compatible)',
        coveredPartPlaceholder: '-- Optional OEM Replacement Part --',
        coveredVndLabel: 'Covered Warranty Value (VND) *',
        resolutionLabelRequired: 'Workshop Inspection & Resolution Notes * (5–1,000 chars)',
        resolutionLabelOptional: 'Workshop Inspection & Resolution Notes (Optional)',
        resolutionPlaceholder: 'Document root-cause inspection, replaced OEM part serial, and post-repair test...',
        rejectReasonLabel: 'Official Claim Rejection Reason * (5–500 chars)',
        rejectReasonPlaceholder: 'Explain why the reported condition falls outside factory OEM defect coverage...',
        updatingClaimCta: 'Updating Claim...',
        confirmTransitionCta: 'Confirm Transition to {{status}}',
        replyModalTitle: 'Official Dealership Reply • {{reviewNumber}}',
        replyInputLabel: 'Official Dealership Response * (5–1,000 chars)',
        replyInputPlaceholder: 'Thank the customer for visiting our workshop or address their feedback...',
        postingReplyCta: 'Posting Reply...',
        liveClaimHeadline: 'LIVE VIN WARRANTY CLAIM FILED',
        liveReviewHeadline: 'LIVE {{rating}}/5 CSAT REVIEW SUBMITTED',
        liveReviewDetail: '{{customer}} rated {{model}} {{rating}}/5',
        toastClaimUpdatedTitle: 'Warranty Claim Updated',
        toastClaimUpdatedMsg: '{{claimNumber}} transitioned to {{status}}.',
        toastReplyPostedTitle: 'Official Dealership Reply Posted',
        toastReplyPostedMsg: 'Published response for {{reviewNumber}}.',
        defaultDealerName: 'Authorized Dealership'
      }
    }
  };

  registerWarrantyReviewsI18n.$inject = ['I18nService'];
  function registerWarrantyReviewsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.staff')
    .run(registerWarrantyReviewsI18n)
    .component('staffWarrantyReviews', {
      controllerAs: '$ctrl',
      controller: StaffWarrantyReviewsController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="shield-check" weight="duotone" size="13"></app-icon>' +
                '<span>{{ \'staff.wr.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'staff.warrantyReviewsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">' +
                '<span class="mono-num">dealer-{{ $ctrl.dealerCode }}</span> ({{ $ctrl.dealerName }}) — {{ \'staff.warrantyReviewsSubtitle\' | t }}' +
              '</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<span class="badge badge-danger mono-num tabular-nums" ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}" ng-if="$ctrl.liveArrivalCount > 0">' +
                '<app-icon name="lightning" weight="fill" size="12"></app-icon>' +
                '<span>{{ \'staff.wr.liveNewBadge\' | t:{ count: $ctrl.liveArrivalCount } }}</span>' +
              '</span>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadDeskData()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrows-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'staff.wr.syncing\' | t) : (\'staff.wr.syncDesk\' | t) }}</span>' +
              '</button>' +
              '<a class="btn btn-secondary" ui-sref="app.staff.workbench">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'staff.wr.backToWorkbench\' | t }}</span>' +
              '</a>' +
            '</div>' +
          '</div>' +

          /* Live SignalR Arrival Banner */
          '<div class="alert-banner alert-info fade-slide-up" role="status" ng-if="$ctrl.latestLiveBanner" ' +
               'ng-class="{\'is-signalr-pulse\': $ctrl.livePulseActive}" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
              '<app-icon name="lightning" weight="fill" size="18"></app-icon>' +
              '<strong>{{ $ctrl.latestLiveBanner.headline }}:</strong>' +
              '<span class="mono-num" style="font-weight:700;">{{ $ctrl.latestLiveBanner.code }}</span>' +
              '<span>• {{ $ctrl.latestLiveBanner.detail }}</span>' +
            '</div>' +
            '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-primary btn-sm" ng-click="$ctrl.setActiveTab($ctrl.latestLiveBanner.targetTab)">' +
                '<span>{{ $ctrl.latestLiveBanner.targetTab === \'REVIEWS\' ? (\'staff.wr.openTabReviews\' | t) : (\'staff.wr.openTabWarranty\' | t) }}</span>' +
                '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
              '</button>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.latestLiveBanner = null">{{ \'staff.wr.dismiss\' | t }}</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadDeskData()">' +
          '</error-retry-banner>' +

          /* Quiet-Chrome Bento 4-Card KPI Strip */
          '<div class="bento-grid" style="margin-bottom:var(--space-5);">' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.wr.kpiAvgCsat\' | t }}</span>' +
                  '<app-icon name="star" weight="fill" size="17" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;margin-top:6px;">' +
                  '<span class="kpi-value mono-num" style="margin-top:0;" kpi-counter="$ctrl.kpis.averageCsat" kpi-format="decimal">0.00</span>' +
                  '<star-rating value="$ctrl.kpis.averageCsat" readonly="true" size="14"></star-rating>' +
                '</div>' +
                '<div class="kpi-meta">{{ \'staff.wr.kpiAvgCsatMeta\' | t:{ total: $ctrl.kpis.totalReviews, fiveStar: $ctrl.kpis.fiveStarReviews } }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.wr.kpiUnanswered\' | t }}</span>' +
                  '<app-icon name="chat-circle-text" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" ng-style="{color: $ctrl.kpis.unansweredReviews > 0 ? \'var(--clr-warning-fg)\' : \'var(--clr-success-fg)\'}" kpi-counter="$ctrl.kpis.unansweredReviews">0</div>' +
                '<div class="kpi-meta">{{ \'staff.wr.kpiUnansweredMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.wr.kpiOpenClaims\' | t }}</span>' +
                  '<app-icon name="shield-check" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num" kpi-counter="$ctrl.kpis.openClaims">0</div>' +
                '<div class="kpi-meta">{{ \'staff.wr.kpiOpenClaimsMeta\' | t }}</div>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card">' +
              '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                  '<span class="kpi-label">{{ \'staff.wr.kpiCoveredVnd\' | t }}</span>' +
                  '<app-icon name="receipt" weight="duotone" size="18" extra-class="text-muted"></app-icon>' +
                '</div>' +
                '<div class="kpi-value mono-num tabular-nums" style="font-size:19px;" kpi-counter="$ctrl.kpis.warrantyCoveredVnd" kpi-format="vnd">0</div>' +
                '<div class="kpi-meta">{{ \'staff.wr.kpiCoveredMeta\' | t:{ completed: $ctrl.kpis.completedClaims, rejected: $ctrl.kpis.rejectedClaims } }}</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Split Desk Tabs: VIN Warranty Claims | CSAT Reviews */
          '<div class="tab-strip">' +
            '<button type="button" id="tabWarrantyClaimsBtn" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'WARRANTY\'}" ' +
                    'ng-click="$ctrl.setActiveTab(\'WARRANTY\')">' +
              '<app-icon name="shield-check" weight="duotone" size="16"></app-icon>' +
              '<span>{{ \'staff.wr.tabWarranty\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.claims.length }})</span>' +
            '</button>' +
            '<button type="button" id="tabCsatReviewsBtn" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'REVIEWS\'}" ' +
                    'ng-click="$ctrl.setActiveTab(\'REVIEWS\')">' +
              '<app-icon name="star" weight="fill" size="15"></app-icon>' +
              '<span>{{ \'staff.wr.tabReviews\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.reviews.length }})</span>' +
            '</button>' +
          '</div>' +

          /* TAB 1: VIN Warranty Claims */
          '<div ng-if="$ctrl.activeTab === \'WARRANTY\'">' +
            '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfWcStatusFilter">{{ \'staff.wr.claimStatusLabel\' | t }}</label>' +
                  '<select id="stfWcStatusFilter" class="form-control" ' +
                          'ng-model="$ctrl.claimFilters.status" ' +
                          'ng-options="s.value as $ctrl.formatClaimStatusOption(s) for s in $ctrl.claimStatusOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfWcCatFilter">{{ \'warranty.defectCategoryLabel\' | t }}</label>' +
                  '<select id="stfWcCatFilter" class="form-control" ' +
                          'ng-model="$ctrl.claimFilters.defectCategory" ' +
                          'ng-options="c.value as $ctrl.formatDefectOption(c) for c in $ctrl.defectFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfWcSearch">{{ \'staff.wr.searchClaimsLabel\' | t }}</label>' +
                  '<input id="stfWcSearch" type="search" class="form-control" ' +
                         'ng-model="$ctrl.claimFilters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'staff.wr.searchClaimsPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">' +
                    '<app-icon name="funnel" weight="regular" size="15"></app-icon>' +
                    '<span>{{ \'staff.wr.clearShort\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.claims.length === 0" variant="table" count="4"></skeleton-loader>' +

            '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredClaims.length === 0" ' +
                         'icon="shield-check" ' +
                         'title-key="staff.wr.emptyClaimsTitle" ' +
                         'desc-key="staff.wr.emptyClaimsDesc" ' +
                         'action-key="common.clearFilters" ' +
                         'on-action="$ctrl.resetFilters()">' +
            '</empty-state>' +

            '<div style="display:flex;flex-direction:column;gap:14px;" ng-if="$ctrl.filteredClaims.length > 0">' +
              '<article class="bezel-card stagger-item" ' +
                       'ng-repeat="clm in $ctrl.filteredClaims track by (clm.id || clm.claimNumber)" ' +
                       'ng-attr-style="--i:{{ $index }};" ' +
                       'ng-class="{\'is-live-arrival is-signalr-pulse\': clm.isLiveArrival}">' +
                '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                  '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap;">' +
                    '<div style="flex:1;min-width:260px;">' +
                      '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                        '<strong class="mono-num text-primary" style="font-size:16px;">{{ clm.claimNumber }}</strong>' +
                        '<span class="badge badge-danger" ng-if="clm.isLiveArrival">' +
                          '<app-icon name="lightning" weight="fill" size="11"></app-icon>' +
                          '<span>{{ \'staff.liveArrivalTag\' | t }}</span>' +
                        '</span>' +
                        '<span class="badge" ng-class="\'badge-status-\' + clm.status.toLowerCase()">' +
                          '{{ clm.status | enumT:\'WarrantyClaimStatus\' }}' +
                        '</span>' +
                        '<span class="badge badge-neutral">{{ clm.defectCategory | enumT:\'WarrantyDefectCategory\' }}</span>' +
                      '</div>' +
                      '<div class="text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">' +
                        '{{ clm.motorcycleModelName || (\'common.vehicle\' | t) }} • {{ \'common.vin\' | t }} <span class="mono-num">{{ clm.vin }}</span> ' +
                        '• <span class="text-secondary" style="font-weight:600;">{{ clm.customerFullName || (\'common.customer\' | t) }}</span>' +
                      '</div>' +
                      '<div class="mono-num text-muted" style="font-size:12px;margin-top:4px;">' +
                        '{{ \'common.odometer\' | t }}: <strong class="text-primary">{{ clm.odometerAtClaimKm | number }} km</strong>' +
                        '<span ng-if="clm.createdAtUtc"> • {{ \'staff.wr.filedAtPrefix\' | t }} {{ clm.createdAtUtc | utcToLocal }}</span>' +
                      '</div>' +
                      '<div class="text-primary" style="font-size:13px;margin-top:6px;">' +
                        '<strong>{{ \'staff.wr.symptomPrefix\' | t }}:</strong> {{ clm.symptomDescription }}' +
                      '</div>' +
                      '<div style="margin-top:6px;display:flex;gap:8px;flex-wrap:wrap;" ng-if="clm.coveredSparePartCode || clm.coveredSparePartName || clm.coveredAmountVnd > 0">' +
                        '<span class="badge badge-success mono-num">{{ \'staff.wr.coveredAmountPrefix\' | t }}: {{ clm.coveredAmountVnd | vndCurrency }}</span>' +
                        '<span class="badge badge-info mono-num" ng-if="clm.coveredSparePartCode || clm.coveredSparePartName">' +
                          '{{ \'staff.wr.oemPartPrefix\' | t }}: {{ clm.coveredSparePartCode ? (clm.coveredSparePartCode + \' • \') : \'\' }}{{ clm.coveredSparePartName || (\'staff.wr.assignedFallback\' | t) }}' +
                        '</span>' +
                      '</div>' +
                      '<div style="font-size:12px;color:var(--clr-success-fg);margin-top:6px;" ng-if="clm.resolutionNotes">' +
                        '<strong>{{ \'staff.wr.resolutionNotesPrefix\' | t }}:</strong> {{ clm.resolutionNotes }}' +
                      '</div>' +
                      '<div style="font-size:12px;color:var(--clr-danger-fg);margin-top:6px;" ng-if="clm.rejectionReason">' +
                        '<strong>{{ \'staff.wr.rejectionReasonPrefix\' | t }}:</strong> {{ clm.rejectionReason }}' +
                      '</div>' +
                    '</div>' +

                    '<div style="display:flex;gap:8px;align-items:center;" ' +
                         'ng-if="$ctrl.canManageClaim(clm)" ' +
                         'has-permission="Permissions.Warranty.ManageDealer">' +
                      '<button type="button" class="btn btn-primary btn-sm" ng-click="$ctrl.openClaimActionModal(clm)">' +
                        '<app-icon name="sliders" weight="regular" size="14"></app-icon>' +
                        '<span>{{ \'staff.wr.manageClaimCta\' | t }}</span>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</article>' +
            '</div>' +
          '</div>' +

          /* TAB 2: 5-Star CSAT Reviews */
          '<div ng-if="$ctrl.activeTab === \'REVIEWS\'">' +
            '<div class="surface-card" style="margin-bottom:var(--space-5);">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfRevRatingFilter">{{ \'staff.wr.starRatingLabel\' | t }}</label>' +
                  '<select id="stfRevRatingFilter" class="form-control" ' +
                          'ng-model="$ctrl.reviewFilters.rating" ' +
                          'ng-options="r.value as (r.labelKey | t) for r in $ctrl.ratingFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfRevReplyFilter">{{ \'staff.wr.replyStatusLabel\' | t }}</label>' +
                  '<select id="stfRevReplyFilter" class="form-control" ' +
                          'ng-model="$ctrl.reviewFilters.replyState" ' +
                          'ng-options="o.value as (o.labelKey | t) for o in $ctrl.replyFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="stfRevSearch">{{ \'staff.wr.searchReviewsLabel\' | t }}</label>' +
                  '<input id="stfRevSearch" type="search" class="form-control" ' +
                         'ng-model="$ctrl.reviewFilters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'staff.wr.searchReviewsPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">' +
                    '<app-icon name="funnel" weight="regular" size="15"></app-icon>' +
                    '<span>{{ \'staff.wr.clearShort\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.reviews.length === 0" variant="table" count="4"></skeleton-loader>' +

            '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredReviews.length === 0" ' +
                         'icon="star" ' +
                         'title-key="staff.wr.emptyReviewsTitle" ' +
                         'desc-key="staff.wr.emptyReviewsDesc" ' +
                         'action-key="common.clearFilters" ' +
                         'on-action="$ctrl.resetFilters()">' +
            '</empty-state>' +

            '<div style="display:flex;flex-direction:column;gap:14px;" ng-if="$ctrl.filteredReviews.length > 0">' +
              '<article class="bezel-card stagger-item" ' +
                       'ng-repeat="rev in $ctrl.filteredReviews track by (rev.id || rev.reviewNumber)" ' +
                       'ng-attr-style="--i:{{ $index }};" ' +
                       'ng-class="{\'is-live-arrival is-signalr-pulse\': rev.isLiveArrival}">' +
                '<div class="bezel-card-inner" style="padding:var(--space-4);">' +
                  '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap;">' +
                    '<div style="flex:1;min-width:260px;">' +
                      '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                        '<strong class="mono-num text-primary" style="font-size:15px;">{{ rev.reviewNumber || \'REV-LIVE\' }}</strong>' +
                        '<span class="badge badge-danger" ng-if="rev.isLiveArrival">' +
                          '<app-icon name="lightning" weight="fill" size="11"></app-icon>' +
                          '<span>{{ \'staff.liveArrivalTag\' | t }}</span>' +
                        '</span>' +
                        '<span class="badge badge-tier-gold" style="gap:6px;">' +
                          '<star-rating value="rev.rating" readonly="true" size="13"></star-rating>' +
                          '<span class="mono-num">({{ rev.rating }}/5)</span>' +
                        '</span>' +
                        '<span class="badge" ng-class="rev.hasStaffResponse ? \'badge-success\' : \'badge-warning\'">' +
                          '<app-icon name="{{ rev.hasStaffResponse ? \'check-circle\' : \'clock-countdown\' }}" weight="fill" size="12"></app-icon>' +
                          '<span>{{ rev.hasStaffResponse ? (\'staff.wr.repliedBadge\' | t) : (\'staff.wr.unansweredBadge\' | t) }}</span>' +
                        '</span>' +
                        '<span class="badge badge-info mono-num" ng-if="rev.invoiceNumber">{{ rev.invoiceNumber }}</span>' +
                      '</div>' +
                      '<div class="text-primary" style="font-size:14px;font-weight:700;margin-top:6px;">' +
                        '{{ rev.motorcycleModelName || (\'staff.wr.workshopServiceFallback\' | t) }}' +
                        '<span ng-if="rev.vin"> • {{ \'common.vin\' | t }} <span class="mono-num">{{ rev.vin }}</span></span>' +
                        ' • <span class="text-secondary" style="font-weight:600;">{{ rev.customerFullName || (\'common.customer\' | t) }}</span>' +
                      '</div>' +
                      '<div style="margin-top:6px;display:flex;gap:6px;flex-wrap:wrap;" ng-if="rev.qualityTags && rev.qualityTags.length > 0">' +
                        '<span class="badge badge-neutral" ng-repeat="tag in rev.qualityTags track by tag">{{ tag | enumT:\'ReviewQualityTag\' }}</span>' +
                      '</div>' +
                      '<div class="text-primary" style="font-size:13px;margin-top:8px;" ng-if="rev.comment">' +
                        '“{{ rev.comment }}”' +
                      '</div>' +
                      '<div class="staff-reply-callout" ng-if="rev.staffResponse">' +
                        '<strong style="color:var(--clr-info-fg);">{{ \'staff.wr.officialReplyPrefix\' | t }}:</strong> {{ rev.staffResponse }}' +
                        '<span class="mono-num text-muted" style="margin-left:8px;" ng-if="rev.respondedAtUtc">({{ rev.respondedAtUtc | utcToLocal }})</span>' +
                      '</div>' +
                    '</div>' +

                    '<div style="display:flex;gap:8px;align-items:center;" has-permission="Permissions.Reviews.RespondDealer">' +
                      '<button type="button" class="btn btn-sm" ' +
                              'ng-class="rev.hasStaffResponse ? \'btn-secondary\' : \'btn-primary\'" ' +
                              'ng-click="$ctrl.openReplyModal(rev)">' +
                        '<app-icon name="{{ rev.hasStaffResponse ? \'pencil-simple\' : \'chat-circle-text\' }}" weight="regular" size="14"></app-icon>' +
                        '<span>{{ rev.hasStaffResponse ? (\'staff.wr.updateReplyCta\' | t) : (\'staff.wr.postReplyCta\' | t) }}</span>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</article>' +
            '</div>' +
          '</div>' +

          /* Modal 1: Warranty Claim Adjudication Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="stfWcModalTitle" ng-if="$ctrl.activeClaimModal">' +
            '<div class="modal-dialog modal-dialog-wide motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">PATCH /api/v1/warranty-claims/{{ $ctrl.activeClaimModal.id }}/status • RowVersion</span>' +
                  '<h2 id="stfWcModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'staff.wr.claimModalTitle\' | t:{ claimNumber: $ctrl.activeClaimModal.claimNumber, status: ($ctrl.activeClaimModal.status | enumT:\'WarrantyClaimStatus\') } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeClaimActionModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.claimActionForm" ng-submit="$ctrl.submitClaimStatusUpdate()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.claimModalProblem">' +
                    '<div><strong>{{ $ctrl.claimModalProblem.title }} ({{ $ctrl.claimModalProblem.errorCode }}):</strong> {{ $ctrl.claimModalProblem.detail }}</div>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:16px;font-size:13px;">' +
                    '<div class="text-primary"><strong>{{ $ctrl.activeClaimModal.motorcycleModelName }}</strong> • {{ \'common.vin\' | t }} <span class="mono-num">{{ $ctrl.activeClaimModal.vin }}</span> • {{ \'common.odometer\' | t }}: <span class="mono-num">{{ $ctrl.activeClaimModal.odometerAtClaimKm | number }} km</span></div>' +
                    '<div class="text-secondary" style="margin-top:4px;"><strong>{{ \'staff.wr.defectPrefix\' | t }} ({{ $ctrl.activeClaimModal.defectCategory | enumT:\'WarrantyDefectCategory\' }}):</strong> {{ $ctrl.activeClaimModal.symptomDescription }}</div>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="wcNextStatusSelect">{{ \'staff.wr.nextStatusLabel\' | t }}</label>' +
                    '<select id="wcNextStatusSelect" name="newStatus" class="form-control" ' +
                            'ng-model="$ctrl.claimActionDraft.newStatus" ' +
                            'ng-options="st.value as (st.labelKey | t) for st in $ctrl.allowedNextStatuses" ' +
                            'ng-change="$ctrl.onNextStatusChanged()" required></select>' +
                  '</div>' +

                  '<div class="grid-12" ng-if="$ctrl.claimActionDraft.newStatus === \'Approved\' || $ctrl.claimActionDraft.newStatus === \'Completed\'">' +
                    '<div class="col-7 form-group">' +
                      '<label class="form-label" for="wcPartSelect">{{ \'staff.wr.coveredPartLabel\' | t }}</label>' +
                      '<select id="wcPartSelect" name="coveredSparePartId" class="form-control mono-num" ' +
                              'ng-model="$ctrl.claimActionDraft.coveredSparePartId" ' +
                              'ng-options="p.id as (p.partCode + \' • \' + p.name + \' (\' + (p.unitPriceVnd | vndCurrency) + \')\') for p in $ctrl.compatibleModalParts" ' +
                              'ng-change="$ctrl.onCoveredPartSelected()">' +
                        '<option value="">{{ \'staff.wr.coveredPartPlaceholder\' | t }}</option>' +
                      '</select>' +
                    '</div>' +

                    '<div class="col-5 form-group">' +
                      '<label class="form-label" for="wcCoveredVndInput">{{ \'staff.wr.coveredVndLabel\' | t }}</label>' +
                      '<input id="wcCoveredVndInput" name="coveredAmountVnd" type="number" min="0" step="any" ' +
                             'class="form-control mono-num" ' +
                             'ng-model="$ctrl.claimActionDraft.coveredAmountVnd" required />' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;" ng-if="$ctrl.claimActionDraft.newStatus !== \'Rejected\'">' +
                    '<label class="form-label" for="wcResolutionInput">' +
                      '{{ $ctrl.claimActionDraft.newStatus === \'Completed\' ? (\'staff.wr.resolutionLabelRequired\' | t) : (\'staff.wr.resolutionLabelOptional\' | t) }}' +
                    '</label>' +
                    '<textarea id="wcResolutionInput" name="resolutionNotes" rows="3" maxlength="1000" class="form-control" ' +
                              'ng-model="$ctrl.claimActionDraft.resolutionNotes" ' +
                              'ng-required="$ctrl.claimActionDraft.newStatus === \'Completed\'" ' +
                              'ng-attr-placeholder="{{ \'staff.wr.resolutionPlaceholder\' | t }}"></textarea>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;" ng-if="$ctrl.claimActionDraft.newStatus === \'Rejected\'">' +
                    '<label class="form-label" for="wcRejectReasonInput">{{ \'staff.wr.rejectReasonLabel\' | t }}</label>' +
                    '<textarea id="wcRejectReasonInput" name="rejectionReason" rows="3" minlength="5" maxlength="500" class="form-control" ' +
                              'ng-model="$ctrl.claimActionDraft.rejectionReason" required ' +
                              'ng-attr-placeholder="{{ \'staff.wr.rejectReasonPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeClaimActionModal()" ng-disabled="$ctrl.isSavingClaim">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" id="confirmWarrantyStatusBtn" class="btn-pill-cta" ' +
                          'has-permission="Permissions.Warranty.ManageDealer" ' +
                          'ng-disabled="$ctrl.isSavingClaim || !$ctrl.canSubmitClaimAction()">' +
                    '<span>{{ $ctrl.isSavingClaim ? (\'staff.wr.updatingClaimCta\' | t) : (\'staff.wr.confirmTransitionCta\' | t:{ status: ($ctrl.claimActionDraft.newStatus | enumT:\'WarrantyClaimStatus\') }) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="{{ $ctrl.isSavingClaim ? \'circle-notch\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSavingClaim ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Modal 2: Official CSAT Review Reply Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="stfRevReplyTitle" ng-if="$ctrl.replyingReview">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-tier-gold mono-num">PATCH /api/v1/service-reviews/{{ $ctrl.replyingReview.id }}/response • RowVersion</span>' +
                  '<h2 id="stfRevReplyTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'staff.wr.replyModalTitle\' | t:{ reviewNumber: $ctrl.replyingReview.reviewNumber } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeReplyModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.replyForm" ng-submit="$ctrl.submitReviewReply()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.replyModalProblem">' +
                    '<strong>{{ $ctrl.replyModalProblem.title }}:</strong> {{ $ctrl.replyModalProblem.detail }}' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:16px;font-size:13px;">' +
                    '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<strong class="text-primary">{{ $ctrl.replyingReview.customerFullName || (\'common.customer\' | t) }}</strong>' +
                      '<star-rating value="$ctrl.replyingReview.rating" readonly="true" size="15"></star-rating>' +
                      '<span class="mono-num text-secondary">({{ $ctrl.replyingReview.rating }}/5)</span>' +
                    '</div>' +
                    '<div class="text-secondary" style="margin-top:6px;" ng-if="$ctrl.replyingReview.comment">“{{ $ctrl.replyingReview.comment }}”</div>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="staffReviewReplyInput">{{ \'staff.wr.replyInputLabel\' | t }}</label>' +
                    '<textarea id="staffReviewReplyInput" name="staffResponse" rows="4" minlength="5" maxlength="1000" class="form-control" ' +
                              'ng-model="$ctrl.replyDraftText" required ' +
                              'ng-attr-placeholder="{{ \'staff.wr.replyInputPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeReplyModal()" ng-disabled="$ctrl.isSavingReply">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" id="submitReviewReplyBtn" class="btn-pill-cta" ' +
                          'has-permission="Permissions.Reviews.RespondDealer" ' +
                          'ng-disabled="$ctrl.isSavingReply || ($ctrl.replyDraftText || \'\').trim().length < 5">' +
                    '<span>{{ $ctrl.isSavingReply ? (\'staff.wr.postingReplyCta\' | t) : (\'staff.wr.postReplyCta\' | t) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="{{ $ctrl.isSavingReply ? \'circle-notch\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSavingReply ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  StaffWarrantyReviewsController.$inject = [
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
  function StaffWarrantyReviewsController(
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
    var livePulseTimer = null;

    function triggerLivePulse() {
      if (isDestroyed) { return; }
      if (livePulseTimer) {
        clearTimeout(livePulseTimer);
        livePulseTimer = null;
      }
      $ctrl.livePulseActive = true;
      livePulseTimer = setTimeout(function () {
        livePulseTimer = null;
        if (isDestroyed) { return; }
        runInDigest(function () {
          $ctrl.livePulseActive = false;
        });
      }, 1300);
    }

    function t(key, params) {
      return (I18nService && angular.isFunction(I18nService.t)) ? I18nService.t(key, params) : key;
    }

    function enumT(group, code) {
      return (I18nService && angular.isFunction(I18nService.enumLabel))
        ? I18nService.enumLabel(group, code)
        : String(code || '');
    }

    function runInDigest(fn) {
      if ($rootScope && angular.isFunction($rootScope.$applyAsync)) {
        $rootScope.$applyAsync(fn);
      } else {
        fn();
      }
    }

    function recomputeKpis() {
      var reviews = $ctrl.reviews || [];
      var claims = $ctrl.claims || [];

      var ratingSum = 0;
      var fiveStar = 0;
      var unanswered = 0;
      for (var i = 0; i < reviews.length; i++) {
        ratingSum += Number(reviews[i].rating) || 0;
        if (reviews[i].rating === 5) {
          fiveStar++;
        }
        if (!reviews[i].hasStaffResponse) {
          unanswered++;
        }
      }
      var avgCsat = reviews.length > 0 ? (Math.round((ratingSum / reviews.length) * 100) / 100) : 0;

      var openClaims = 0;
      var completedClaims = 0;
      var rejectedClaims = 0;
      var coveredVnd = 0;
      for (var j = 0; j < claims.length; j++) {
        var st = claims[j].status;
        if (DealerApiService.isOpenWarrantyStatus(st)) {
          openClaims++;
        } else if (st === 'Completed') {
          completedClaims++;
        } else if (st === 'Rejected') {
          rejectedClaims++;
        }
        if (st === 'Approved' || st === 'Completed') {
          coveredVnd += Number(claims[j].coveredAmountVnd) || 0;
        }
      }

      $ctrl.kpis = {
        averageCsat: avgCsat,
        totalReviews: reviews.length,
        fiveStarReviews: fiveStar,
        unansweredReviews: unanswered,
        openClaims: openClaims,
        completedClaims: completedClaims,
        rejectedClaims: rejectedClaims,
        warrantyCoveredVnd: coveredVnd
      };
    }

    function upsertOrPrependLiveClaim(payload) {
      if (isDestroyed || !payload) {
        return;
      }
      var normalized = DealerApiService.normalizeWarrantyClaim(payload);
      if (!normalized.id && normalized.claimNumber) {
        normalized.id = normalized.claimNumber;
        normalized.claimId = normalized.claimNumber;
      }
      normalized.isLiveArrival = true;

      var existingIdx = -1;
      for (var i = 0; i < $ctrl.claims.length; i++) {
        var cur = $ctrl.claims[i];
        if (
          (normalized.id && cur.id && cur.id === normalized.id) ||
          (normalized.claimNumber && cur.claimNumber === normalized.claimNumber)
        ) {
          existingIdx = i;
          break;
        }
      }

      if (existingIdx !== -1) {
        $ctrl.claims[existingIdx] = angular.extend({}, $ctrl.claims[existingIdx], normalized, {
          rowVersion: normalized.rowVersion || $ctrl.claims[existingIdx].rowVersion,
          symptomDescription: normalized.symptomDescription || $ctrl.claims[existingIdx].symptomDescription,
          isLiveArrival: true
        });
      } else {
        $ctrl.claims.unshift(normalized);
        $ctrl.liveArrivalCount = ($ctrl.liveArrivalCount || 0) + 1;
      }

      $ctrl.latestLiveBanner = {
        headline: t('staff.wr.liveClaimHeadline'),
        code: normalized.claimNumber || 'WC-LIVE',
        detail: (normalized.motorcycleModelName || t('common.vehicle')) + ' (' + t('common.vin') + ' ' + (normalized.vin || 'N/A') + ') • ' + enumT('WarrantyDefectCategory', normalized.defectCategory),
        targetTab: 'WARRANTY'
      };
      recomputeKpis();
      $ctrl.applyFilters();

      if (normalized.claimId && normalized.claimId.length >= 20) {
        DealerApiService.getWarrantyClaimById(normalized.claimId)
          .then(function (hydrated) {
            if (isDestroyed) { return; }
            for (var j = 0; j < $ctrl.claims.length; j++) {
              if ($ctrl.claims[j].id === hydrated.id || $ctrl.claims[j].claimNumber === hydrated.claimNumber) {
                hydrated.isLiveArrival = true;
                $ctrl.claims[j] = hydrated;
                break;
              }
            }
            recomputeKpis();
            $ctrl.applyFilters();
          })
          .catch(angular.noop);
      }
    }

    function upsertOrPrependLiveReview(payload) {
      if (isDestroyed || !payload) {
        return;
      }
      var normalized = DealerApiService.normalizeServiceReview(payload);
      if (!normalized.id && normalized.reviewNumber) {
        normalized.id = normalized.reviewNumber;
        normalized.reviewId = normalized.reviewNumber;
      }
      normalized.isLiveArrival = true;

      var existingIdx = -1;
      for (var i = 0; i < $ctrl.reviews.length; i++) {
        var cur = $ctrl.reviews[i];
        if (
          (normalized.id && cur.id && cur.id === normalized.id) ||
          (normalized.reviewNumber && cur.reviewNumber && cur.reviewNumber === normalized.reviewNumber) ||
          (normalized.serviceInvoiceId && cur.serviceInvoiceId && cur.serviceInvoiceId === normalized.serviceInvoiceId)
        ) {
          existingIdx = i;
          break;
        }
      }

      if (existingIdx !== -1) {
        $ctrl.reviews[existingIdx] = angular.extend({}, $ctrl.reviews[existingIdx], normalized, {
          rowVersion: normalized.rowVersion || $ctrl.reviews[existingIdx].rowVersion,
          comment: normalized.comment || $ctrl.reviews[existingIdx].comment,
          isLiveArrival: true
        });
      } else {
        $ctrl.reviews.unshift(normalized);
        $ctrl.liveArrivalCount = ($ctrl.liveArrivalCount || 0) + 1;
      }

      $ctrl.latestLiveBanner = {
        headline: t('staff.wr.liveReviewHeadline', { rating: normalized.rating }),
        code: normalized.reviewNumber || normalized.invoiceNumber || 'REV-LIVE',
        detail: t('staff.wr.liveReviewDetail', {
          customer: normalized.customerFullName || t('common.customer'),
          model: normalized.motorcycleModelName || t('staff.wr.workshopServiceFallback'),
          rating: normalized.rating
        }),
        targetTab: 'REVIEWS'
      };
      recomputeKpis();
      $ctrl.applyFilters();

      if (normalized.serviceInvoiceId && angular.isFunction(DealerApiService.getServiceReviewByInvoice)) {
        DealerApiService.getServiceReviewByInvoice(normalized.serviceInvoiceId)
          .then(function (hydrated) {
            if (isDestroyed || !hydrated) { return; }
            for (var j = 0; j < $ctrl.reviews.length; j++) {
              if (
                $ctrl.reviews[j].id === hydrated.id ||
                $ctrl.reviews[j].reviewNumber === hydrated.reviewNumber ||
                $ctrl.reviews[j].serviceInvoiceId === hydrated.serviceInvoiceId
              ) {
                hydrated.isLiveArrival = true;
                $ctrl.reviews[j] = hydrated;
                break;
              }
            }
            recomputeKpis();
            $ctrl.applyFilters();
          })
          .catch(angular.noop);
      }
    }

    $ctrl.formatClaimStatusOption = function (opt) {
      if (!opt || !opt.value) {
        return t('staff.wr.allClaimStatuses');
      }
      return enumT('WarrantyClaimStatus', opt.value);
    };

    $ctrl.formatDefectOption = function (opt) {
      if (!opt || !opt.value) {
        return t('staff.wr.allDefects');
      }
      return enumT('WarrantyDefectCategory', opt.value);
    };

    $ctrl.$onInit = function () {
      var user = (AuthService && angular.isFunction(AuthService.getCurrentUser) && AuthService.getCurrentUser()) || {};
      $ctrl.dealerId = user.dealerId || '';
      $ctrl.dealerCode = user.dealerCode || '';
      $ctrl.dealerName = user.dealerName || t('staff.wr.defaultDealerName');

      $ctrl.isLoading = false;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.latestLiveBanner = null;
      $ctrl.livePulseActive = false;
      $ctrl.liveArrivalCount = 0;
      $ctrl.activeTab = 'WARRANTY';

      $ctrl.claimStatusOptions = [
        { value: '' },
        { value: 'Submitted' },
        { value: 'UnderReview' },
        { value: 'Approved' },
        { value: 'Completed' },
        { value: 'Rejected' }
      ];
      $ctrl.defectFilterOptions = [{ value: '' }].concat(
        DealerApiService.getWarrantyDefectCategories()
      );
      $ctrl.ratingFilterOptions = [
        { value: '', labelKey: 'staff.wr.allRatings' },
        { value: '5', labelKey: 'staff.wr.ratingFiveOnly' },
        { value: '4', labelKey: 'staff.wr.ratingFourOnly' },
        { value: '3', labelKey: 'staff.wr.ratingThreeOrBelow' }
      ];
      $ctrl.replyFilterOptions = [
        { value: '', labelKey: 'staff.wr.allReviews' },
        { value: 'UNANSWERED', labelKey: 'staff.wr.unansweredOnly' },
        { value: 'REPLIED', labelKey: 'staff.wr.repliedOnly' }
      ];

      $ctrl.claimFilters = { status: '', defectCategory: '', search: '' };
      $ctrl.reviewFilters = { rating: '', replyState: '', search: '' };

      $ctrl.activeClaimModal = null;
      $ctrl.allowedNextStatuses = [];
      $ctrl.compatibleModalParts = [];
      $ctrl.claimActionDraft = {};
      $ctrl.isSavingClaim = false;
      $ctrl.claimModalProblem = null;

      $ctrl.replyingReview = null;
      $ctrl.replyDraftText = '';
      $ctrl.isSavingReply = false;
      $ctrl.replyModalProblem = null;

      $ctrl.claims = [];
      $ctrl.reviews = [];
      $ctrl.spareParts = [];

      recomputeKpis();
      $ctrl.applyFilters();

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('WarrantyClaimCreated', function (payload) {
          runInDigest(function () {
            upsertOrPrependLiveClaim(payload);
          });
        }));

        cleanupFns.push(SignalRClientService.on('WarrantyClaimStatusChanged', function (payload) {
          if (isDestroyed || !payload) { return; }
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
                matched = true;
              }
            }
            recomputeKpis();
            $ctrl.applyFilters();
            if (matched && targetId) {
              DealerApiService.getWarrantyClaimById(targetId)
                .then(function (fresh) {
                  if (isDestroyed) { return; }
                  for (var j = 0; j < $ctrl.claims.length; j++) {
                    if ($ctrl.claims[j].id === fresh.id) {
                      $ctrl.claims[j] = fresh;
                    }
                  }
                  recomputeKpis();
                  $ctrl.applyFilters();
                })
                .catch(angular.noop);
            } else if (!matched) {
              $ctrl.loadDeskData();
            }
          });
        }));

        cleanupFns.push(SignalRClientService.on('ServiceReviewSubmitted', function (payload) {
          runInDigest(function () {
            upsertOrPrependLiveReview(payload);
          });
        }));
      }

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed) {
            $ctrl.loadDeskData();
          }
        }));

        cleanupFns.push($rootScope.$on('signalr:live-pulse', function (_evt, meta) {
          if (isDestroyed || !meta) {
            return;
          }
          if (
            meta.eventName === 'WarrantyClaimCreated' ||
            meta.eventName === 'WarrantyClaimStatusChanged' ||
            meta.eventName === 'ServiceReviewSubmitted'
          ) {
            triggerLivePulse();
          }
        }));
      }

      $ctrl.loadDeskData();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (livePulseTimer) {
        clearTimeout(livePulseTimer);
        livePulseTimer = null;
      }
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.loadDeskData = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        claims: DealerApiService.getDealerWarrantyClaims({ pageSize: 100 }),
        reviews: DealerApiService.getDealerServiceReviews({ pageSize: 100 }),
        parts: DealerApiService.getSpareParts({ pageSize: 100, includeUniversal: true }).catch(function () { return []; })
      }).then(function (bundle) {
        if (isDestroyed) { return bundle; }
        $ctrl.claims = bundle.claims || [];
        $ctrl.reviews = bundle.reviews || [];
        $ctrl.spareParts = (bundle.parts || []).filter(function (p) { return p.isActive !== false; });
        recomputeKpis();
        $ctrl.applyFilters();
        return bundle;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return { claims: $ctrl.claims, reviews: $ctrl.reviews };
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.setActiveTab = function (tabKey) {
      $ctrl.activeTab = tabKey === 'REVIEWS' ? 'REVIEWS' : 'WARRANTY';
    };

    $ctrl.renderStars = function (rating) {
      return String(Math.max(0, Math.min(5, Math.round(Number(rating) || 0)))) + '/5';
    };

    $ctrl.applyFilters = function () {
      var cStatus = ($ctrl.claimFilters && $ctrl.claimFilters.status) || '';
      var cCat = ($ctrl.claimFilters && $ctrl.claimFilters.defectCategory) || '';
      var cQuery = String(($ctrl.claimFilters && $ctrl.claimFilters.search) || '').trim().toLowerCase();

      $ctrl.filteredClaims = ($ctrl.claims || []).filter(function (clm) {
        if (cStatus && clm.status !== cStatus) { return false; }
        if (cCat && clm.defectCategory !== cCat) { return false; }
        if (cQuery) {
          var hay = (
            clm.claimNumber + ' ' +
            clm.vin + ' ' +
            clm.motorcycleModelName + ' ' +
            clm.customerFullName + ' ' +
            clm.symptomDescription
          ).toLowerCase();
          if (hay.indexOf(cQuery) === -1) { return false; }
        }
        return true;
      });

      var rRating = ($ctrl.reviewFilters && $ctrl.reviewFilters.rating) || '';
      var rReply = ($ctrl.reviewFilters && $ctrl.reviewFilters.replyState) || '';
      var rQuery = String(($ctrl.reviewFilters && $ctrl.reviewFilters.search) || '').trim().toLowerCase();

      $ctrl.filteredReviews = ($ctrl.reviews || []).filter(function (rev) {
        if (rRating === '5' && rev.rating !== 5) { return false; }
        if (rRating === '4' && rev.rating !== 4) { return false; }
        if (rRating === '3' && rev.rating > 3) { return false; }
        if (rReply === 'UNANSWERED' && rev.hasStaffResponse) { return false; }
        if (rReply === 'REPLIED' && !rev.hasStaffResponse) { return false; }
        if (rQuery) {
          var hay = (
            rev.reviewNumber + ' ' +
            rev.invoiceNumber + ' ' +
            rev.vin + ' ' +
            rev.motorcycleModelName + ' ' +
            rev.customerFullName + ' ' +
            (rev.comment || '') + ' ' +
            (rev.staffResponse || '')
          ).toLowerCase();
          if (hay.indexOf(rQuery) === -1) { return false; }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      $ctrl.claimFilters = { status: '', defectCategory: '', search: '' };
      $ctrl.reviewFilters = { rating: '', replyState: '', search: '' };
      $ctrl.applyFilters();
    };

    $ctrl.canManageClaim = function (clm) {
      return Boolean(clm && DealerApiService.isOpenWarrantyStatus(clm.status));
    };

    $ctrl.openClaimActionModal = function (clm) {
      if (!clm) { return; }
      var rawAllowed = ALLOWED_WARRANTY_TRANSITIONS[clm.status] || [];
      $ctrl.allowedNextStatuses = rawAllowed.map(function (opt) {
        return angular.extend({}, opt);
      });
      var defaultNext = clm.status === 'Approved' ? 'Completed' : 'Approved';
      var hasDefault = false;
      for (var i = 0; i < $ctrl.allowedNextStatuses.length; i++) {
        if ($ctrl.allowedNextStatuses[i].value === defaultNext) {
          hasDefault = true;
          break;
        }
      }
      if (!hasDefault && $ctrl.allowedNextStatuses.length > 0) {
        defaultNext = $ctrl.allowedNextStatuses[0].value;
      }

      $ctrl.compatibleModalParts = ($ctrl.spareParts || []).filter(function (p) {
        if (!p.compatibleModelId || !clm.motorcycleModelId) {
          return true;
        }
        return p.compatibleModelId === clm.motorcycleModelId;
      }).map(function (p) {
        return angular.extend({}, p);
      });

      $ctrl.activeClaimModal = clm;
      $ctrl.claimModalProblem = null;
      $ctrl.claimActionDraft = {
        newStatus: defaultNext,
        coveredSparePartId: clm.coveredSparePartId || '',
        coveredAmountVnd: clm.coveredAmountVnd || 0,
        resolutionNotes: clm.resolutionNotes || '',
        rejectionReason: clm.rejectionReason || ''
      };
    };

    $ctrl.closeClaimActionModal = function () {
      $ctrl.activeClaimModal = null;
      $ctrl.claimModalProblem = null;
      $ctrl.isSavingClaim = false;
    };

    $ctrl.onNextStatusChanged = function () {
      if ($ctrl.claimActionDraft.newStatus === 'Rejected') {
        $ctrl.claimActionDraft.coveredSparePartId = '';
        $ctrl.claimActionDraft.coveredAmountVnd = 0;
      }
      $ctrl.claimModalProblem = null;
    };

    $ctrl.onCoveredPartSelected = function () {
      var partId = $ctrl.claimActionDraft.coveredSparePartId;
      if (!partId) {
        return;
      }
      var list = $ctrl.compatibleModalParts || [];
      for (var i = 0; i < list.length; i++) {
        if (list[i].id === partId) {
          $ctrl.claimActionDraft.coveredAmountVnd = list[i].unitPriceVnd;
          break;
        }
      }
    };

    $ctrl.canSubmitClaimAction = function () {
      if (!$ctrl.activeClaimModal || !$ctrl.claimActionDraft.newStatus) {
        return false;
      }
      var next = $ctrl.claimActionDraft.newStatus;
      if (next === 'Completed') {
        var resNotes = String($ctrl.claimActionDraft.resolutionNotes || '').trim();
        return resNotes.length >= 5 && resNotes.length <= 1000;
      }
      if (next === 'Rejected') {
        var rejReason = String($ctrl.claimActionDraft.rejectionReason || '').trim();
        return rejReason.length >= 5 && rejReason.length <= 500;
      }
      return true;
    };

    $ctrl.submitClaimStatusUpdate = function () {
      if (!$ctrl.canSubmitClaimAction() || $ctrl.isSavingClaim) {
        return $q.resolve(null);
      }
      var target = $ctrl.activeClaimModal;
      var draft = $ctrl.claimActionDraft;

      $ctrl.isSavingClaim = true;
      $ctrl.claimModalProblem = null;

      return DealerApiService.updateWarrantyClaimStatus(target.id, {
        newStatus: draft.newStatus,
        coveredSparePartId: draft.coveredSparePartId || null,
        coveredAmountVnd: draft.coveredAmountVnd,
        resolutionNotes: draft.resolutionNotes,
        rejectionReason: draft.rejectionReason,
        rowVersion: target.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.claims.length; i++) {
          if ($ctrl.claims[i].id === target.id || $ctrl.claims[i].claimNumber === target.claimNumber) {
            $ctrl.claims[i] = updated;
          }
        }
        recomputeKpis();
        $ctrl.applyFilters();
        ToastService.success(
          t('staff.wr.toastClaimUpdatedTitle'),
          t('staff.wr.toastClaimUpdatedMsg', {
            claimNumber: updated.claimNumber,
            status: enumT('WarrantyClaimStatus', updated.status)
          })
        );
        $ctrl.closeClaimActionModal();
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.claimModalProblem = problem;
        if (problem.status === 409) {
          $ctrl.loadDeskData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSavingClaim = false;
        }
      });
    };

    $ctrl.openReplyModal = function (rev) {
      if (!rev) { return; }
      $ctrl.replyingReview = rev;
      $ctrl.replyDraftText = rev.staffResponse || '';
      $ctrl.replyModalProblem = null;
    };

    $ctrl.closeReplyModal = function () {
      $ctrl.replyingReview = null;
      $ctrl.replyDraftText = '';
      $ctrl.replyModalProblem = null;
      $ctrl.isSavingReply = false;
    };

    $ctrl.submitReviewReply = function () {
      var trimmed = String($ctrl.replyDraftText || '').trim();
      if (!$ctrl.replyingReview || trimmed.length < 5 || $ctrl.isSavingReply) {
        return $q.resolve(null);
      }
      var target = $ctrl.replyingReview;

      $ctrl.isSavingReply = true;
      $ctrl.replyModalProblem = null;

      return DealerApiService.respondToServiceReview(target.id, {
        staffResponse: trimmed,
        rowVersion: target.rowVersion
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        for (var i = 0; i < $ctrl.reviews.length; i++) {
          if (
            $ctrl.reviews[i].id === target.id ||
            ($ctrl.reviews[i].reviewNumber && $ctrl.reviews[i].reviewNumber === target.reviewNumber)
          ) {
            $ctrl.reviews[i] = updated;
          }
        }
        recomputeKpis();
        $ctrl.applyFilters();
        ToastService.success(
          t('staff.wr.toastReplyPostedTitle'),
          t('staff.wr.toastReplyPostedMsg', { reviewNumber: updated.reviewNumber || 'REV' })
        );
        $ctrl.closeReplyModal();
        return updated;
      }).catch(function (rejection) {
        if (isDestroyed) { return null; }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.replyModalProblem = problem;
        if (problem.status === 409) {
          $ctrl.loadDeskData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSavingReply = false;
        }
      });
    };
  }
})();
