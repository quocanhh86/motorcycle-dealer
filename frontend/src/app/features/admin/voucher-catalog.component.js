(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      vouchers: {
        screenCode: 'SCR-ADM-06 · FR-05',
        backToAnalytics: 'Phân tích HQ',
        refreshing: 'Đang tải lại...',
        createVoucherCta: 'Tạo mẫu Voucher mới',
        kpiTotalTemplates: 'Tổng mẫu Voucher',
        kpiActiveTemplates: 'Đang mở đổi điểm',
        kpiTotalRedeemed: 'Tổng lượt đã đổi',
        scopeFilterLabel: 'Phạm vi áp dụng',
        allScopesOption: 'Tất cả phạm vi',
        scopeServiceOnly: 'Chỉ xưởng dịch vụ (ServiceOnly)',
        scopeShowroomOnly: 'Chỉ mua xe Showroom (ShowroomOnly)',
        scopeAll: 'Toàn hệ thống (Xưởng & Showroom)',
        minTierFilterLabel: 'Hạng thành viên tối thiểu',
        allTiersOption: 'Tất cả hạng thành viên',
        searchLabel: 'Tìm theo tiền tố mã hoặc tiêu đề',
        searchPlaceholder: 'Tìm tiền tố mã voucher, tiêu đề hoặc mô tả...',
        colPrefix: 'Tiền tố mã',
        colTitleScope: 'Tên ưu đãi & Phạm vi',
        colPointsCost: 'Điểm đổi',
        colDiscountMin: 'Mức giảm & Đơn tối thiểu',
        colMinTier: 'Hạng tối thiểu',
        colQuota: 'Đã đổi / Hạn mức',
        validDaysSummary: 'Hiệu lực {{days}} ngày',
        minOrderShort: 'Đơn tối thiểu: {{amount}}',
        unlimited: 'Không giới hạn',
        pausedBadge: 'Tạm ngưng',
        emptyTitle: 'Không có mẫu Voucher nào phù hợp bộ lọc',
        emptyDesc: 'Hãy xóa bộ lọc hoặc tạo mẫu Voucher ưu đãi mới cho cửa hàng đổi điểm.',
        modalEditTitle: 'Chỉnh sửa mẫu Voucher: {{prefix}}',
        modalCreateTitle: 'Tạo mẫu Voucher ưu đãi mới',
        fieldPrefix: 'Tiền tố mã * (3–15 ký tự chữ/số)',
        fieldPrefixPlaceholder: 'Nhập tiền tố mã voucher',
        fieldScope: 'Phạm vi áp dụng *',
        fieldMinTier: 'Hạng thành viên tối thiểu *',
        fieldTitle: 'Tên ưu đãi * (Tối đa 150 ký tự)',
        fieldTitlePlaceholder: 'Nhập tên hiển thị của Voucher',
        fieldPointsCost: 'Chi phí điểm đổi * (>= 1)',
        fieldDescription: 'Mô tả điều kiện * (Tối đa 500 ký tự)',
        fieldDescriptionPlaceholder: 'Áp dụng cho hóa đơn bảo dưỡng xưởng đạt mức chi tiêu tối thiểu.',
        fieldDiscountType: 'Loại giảm giá *',
        discountTypeFixed: 'Giảm tiền cố định (VNĐ)',
        discountTypePercent: 'Giảm theo phần trăm (%)',
        fieldDiscountPercent: 'Phần trăm giảm (1–100%) *',
        fieldDiscountFixed: 'Số tiền giảm cố định (VNĐ) *',
        fieldMaxCap: 'Mức giảm tối đa (VNĐ) *',
        fieldMinOrder: 'Đơn hàng tối thiểu (VNĐ) *',
        fieldValidDays: 'Thời hạn hiệu lực (Ngày) *',
        fieldQuotaTotal: 'Tổng hạn mức (Để trống = Không giới hạn)',
        fieldQuotaPlaceholder: 'Không giới hạn',
        fieldActiveCheck: 'Mở đổi trực tiếp tại Cửa hàng Voucher khách hàng',
        saveVoucherCta: 'Lưu thay đổi Voucher',
        createVoucherSubmitCta: 'Tạo mẫu Voucher',
        toastUpdatedTitle: 'Đã cập nhật mẫu Voucher',
        toastCreatedTitle: 'Đã tạo mẫu Voucher mới',
        toastSavedBody: '{{prefix}} · {{title}} đã được lưu.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      vouchers: {
        screenCode: 'SCR-ADM-06 · FR-05',
        backToAnalytics: 'Analytics',
        refreshing: 'Refreshing...',
        createVoucherCta: 'Create Voucher Template',
        kpiTotalTemplates: 'Catalog Templates',
        kpiActiveTemplates: 'Active Campaigns',
        kpiTotalRedeemed: 'Total Redemptions',
        scopeFilterLabel: 'Applicable Scope',
        allScopesOption: 'All Scopes',
        scopeServiceOnly: 'ServiceOnly (Workshop Invoices)',
        scopeShowroomOnly: 'ShowroomOnly (Bike Purchases)',
        scopeAll: 'All (Workshop & Showroom)',
        minTierFilterLabel: 'Minimum Tier',
        allTiersOption: 'All Tiers',
        searchLabel: 'Search Prefix or Title',
        searchPlaceholder: 'Search by code prefix, title, or description...',
        colPrefix: 'Prefix',
        colTitleScope: 'Reward Title & Scope',
        colPointsCost: 'Points Cost',
        colDiscountMin: 'Discount & Min Order',
        colMinTier: 'Min Tier',
        colQuota: 'Redeemed / Quota',
        validDaysSummary: 'Valid {{days}} days',
        minOrderShort: 'Min: {{amount}}',
        unlimited: 'Unlimited',
        pausedBadge: 'Paused',
        emptyTitle: 'No voucher catalog templates match your filters',
        emptyDesc: 'Try clearing active filters or create a new point-redeemable reward template.',
        modalEditTitle: 'Edit Voucher Template: {{prefix}}',
        modalCreateTitle: 'Create Reward Voucher Template',
        fieldPrefix: 'Code Prefix * (3–15 Alnum)',
        fieldPrefixPlaceholder: 'Enter voucher code prefix',
        fieldScope: 'Applicable Scope *',
        fieldMinTier: 'Minimum Tier Gate *',
        fieldTitle: 'Reward Title * (Max 150 chars)',
        fieldTitlePlaceholder: 'Enter reward voucher title',
        fieldPointsCost: 'Points Cost * (>= 1)',
        fieldDescription: 'Description * (Max 500 chars)',
        fieldDescriptionPlaceholder: 'Applicable on workshop maintenance invoices meeting minimum gross spend.',
        fieldDiscountType: 'Discount Type *',
        discountTypeFixed: 'Fixed Amount (VND)',
        discountTypePercent: 'Percentage (%)',
        fieldDiscountPercent: 'Discount % (1–100) *',
        fieldDiscountFixed: 'Fixed Discount (VND) *',
        fieldMaxCap: 'Max Discount Cap (VND) *',
        fieldMinOrder: 'Min Gross Order (VND) *',
        fieldValidDays: 'Validity (Days) *',
        fieldQuotaTotal: 'Total Quota (Blank = Unlimited)',
        fieldQuotaPlaceholder: 'Unlimited',
        fieldActiveCheck: 'Active in Customer Reward Store',
        saveVoucherCta: 'Save Voucher Changes',
        createVoucherSubmitCta: 'Create Voucher Template',
        toastUpdatedTitle: 'Voucher Template Updated',
        toastCreatedTitle: 'Voucher Template Created',
        toastSavedBody: '{{prefix}} · {{title}} saved.'
      }
    }
  };

  registerVoucherCatalogI18n.$inject = ['I18nService'];
  function registerVoucherCatalogI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerVoucherCatalogI18n)
    .component('adminVoucherCatalog', {
      controllerAs: '$ctrl',
      controller: AdminVoucherCatalogController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.vouchers.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.voucherCatalogTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.voucherCatalogSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.vouchers.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadCatalog()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.vouchers.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Loyalty.ManageVoucherCatalog" ' +
                      'ng-click="$ctrl.openCreateModal()">' +
                '<span>{{ \'admin.vouchers.createVoucherCta\' | t }}</span>' +
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
            'on-retry="$ctrl.loadCatalog()">' +
          '</error-retry-banner>' +

          /* Double-Bezel KPI Summary Strip */
          '<div class="bento-grid" style="margin-bottom:20px;">' +
            '<div class="col-4 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.vouchers.kpiTotalTemplates\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalTemplates" kpi-format="number">{{ $ctrl.stats.totalTemplates }}</div>' +
                '</div>' +
                '<app-icon name="ticket" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.vouchers.kpiActiveTemplates\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.activeTemplates" kpi-format="number">{{ $ctrl.stats.activeTemplates }}</div>' +
                '</div>' +
                '<app-icon name="check-circle" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.vouchers.kpiTotalRedeemed\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalRedeemed" kpi-format="number">{{ $ctrl.stats.totalRedeemed }}</div>' +
                '</div>' +
                '<app-icon name="gift" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Strip */
          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="vchScopeFilter">{{ \'admin.vouchers.scopeFilterLabel\' | t }}</label>' +
                  '<select id="vchScopeFilter" class="form-control" ' +
                          'ng-model="$ctrl.filters.scope" ' +
                          'ng-options="s.value as s.label for s in $ctrl.scopeFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="vchTierFilter">{{ \'admin.vouchers.minTierFilterLabel\' | t }}</label>' +
                  '<select id="vchTierFilter" class="form-control" ' +
                          'ng-model="$ctrl.filters.minTierCode" ' +
                          'ng-options="t.value as t.label for t in $ctrl.tierFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="vchSearchInput">{{ \'admin.vouchers.searchLabel\' | t }}</label>' +
                  '<input id="vchSearchInput" type="search" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'admin.vouchers.searchPlaceholder\' | t }}" />' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="4" ng-if="$ctrl.isLoading && $ctrl.vouchers.length === 0"></skeleton-loader>' +

          '<div class="bezel-card quiet-table-frame" ng-if="!$ctrl.isLoading || $ctrl.vouchers.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredVouchers.length === 0" ' +
                           'icon="ticket" ' +
                           'title-key="admin.vouchers.emptyTitle" ' +
                           'desc-key="admin.vouchers.emptyDesc" ' +
                           'action-key="common.clearFilters" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredVouchers.length > 0" style="max-height:560px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colPrefix\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colTitleScope\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colPointsCost\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colDiscountMin\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colMinTier\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.vouchers.colQuota\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.actions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="item in $ctrl.filteredVouchers track by item.id">' +
                      '<td class="mono-num" style="font-weight:700;">{{ item.codePrefix }}</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">{{ item.title }}</div>' +
                        '<div class="text-muted" style="font-size:12px;margin-top:2px;">' +
                          '<span class="badge badge-info" style="margin-right:6px;">{{ $ctrl.getScopeLabel(item.applicableScope) }}</span>' +
                          '<span>{{ \'admin.vouchers.validDaysSummary\' | t:{ days: item.validDays } }}</span>' +
                        '</div>' +
                      '</td>' +
                      '<td class="mono-num" style="font-weight:700;">{{ item.pointsCost | number }} {{ \'common.ptsShort\' | t }}</td>' +
                      '<td class="mono-num">' +
                        '<div>{{ item.discountSummary }}</div>' +
                        '<div class="text-muted" style="font-size:11px;">{{ \'admin.vouchers.minOrderShort\' | t:{ amount: (item.minOrderAmount | vndCurrency) } }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="\'badge-tier-\' + (item.minTierCode || \'Bronze\').toLowerCase()">' +
                          '<app-icon name="crown" weight="fill" size="12"></app-icon>' +
                          '<span>{{ (item.minTierCode || \'Bronze\') | enumT:\'LoyaltyTierCode\' }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td class="mono-num">' +
                        '{{ item.quotaRedeemed | number }} / ' +
                        '<span ng-if="item.quotaTotal !== null">{{ item.quotaTotal | number }}</span>' +
                        '<span ng-if="item.quotaTotal === null">{{ \'admin.vouchers.unlimited\' | t }}</span>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="item.isActive ? \'badge-success\' : \'badge-neutral\'">' +
                          '<app-icon name="{{ item.isActive ? \'check-circle\' : \'minus-circle\' }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ (item.isActive ? \'common.active\' : \'admin.vouchers.pausedBadge\') | t }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td>' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'has-permission="Permissions.Loyalty.ManageVoucherCatalog" ' +
                                'ng-click="$ctrl.openEditModal(item)">' +
                          '<app-icon name="pencil-simple" weight="regular" size="13"></app-icon>' +
                          '<span>{{ \'common.edit\' | t }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* CREATE / EDIT VOUCHER TEMPLATE MODAL */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="vchModalTitle" ng-if="$ctrl.modalOpen">' +
            '<div class="modal-dialog modal-dialog-wide motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ $ctrl.editingCatalogId ? (\'PUT /api/v1/vouchers/catalog/\' + $ctrl.editingCatalogId) : \'POST /api/v1/vouchers/catalog\' }}</span>' +
                  '<h2 id="vchModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ $ctrl.editingCatalogId ? (\'admin.vouchers.modalEditTitle\' | t:{ prefix: $ctrl.draft.codePrefix }) : (\'admin.vouchers.modalCreateTitle\' | t) }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.catalogForm" ng-submit="$ctrl.saveCatalogItem()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.modalProblem">' +
                    '<strong>{{ $ctrl.modalProblem.title }} ({{ $ctrl.modalProblem.errorCode }}):</strong> {{ $ctrl.modalProblem.detail }}' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchPrefixInput">{{ \'admin.vouchers.fieldPrefix\' | t }}</label>' +
                      '<input id="vchPrefixInput" name="codePrefix" type="text" maxlength="15" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.codePrefix" ng-pattern="/^[A-Za-z0-9_-]{3,15}$/" ' +
                             'ng-readonly="Boolean($ctrl.editingCatalogId)" required ' +
                             'ng-attr-placeholder="{{ \'admin.vouchers.fieldPrefixPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.codePrefix">{{ $ctrl.fieldErrors.codePrefix }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchScopeInput">{{ \'admin.vouchers.fieldScope\' | t }}</label>' +
                      '<select id="vchScopeInput" name="applicableScope" class="form-control" ' +
                              'ng-model="$ctrl.draft.applicableScope" ' +
                              'ng-options="s.value as s.label for s in $ctrl.scopeOptions" required></select>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchMinTierInput">{{ \'admin.vouchers.fieldMinTier\' | t }}</label>' +
                      '<select id="vchMinTierInput" name="minTierCode" class="form-control" ' +
                              'ng-model="$ctrl.draft.minTierCode" ' +
                              'ng-options="t.code as (t.label + \' (\' + t.pointsMultiplier + \'x)\') for t in $ctrl.tierSelectOptions" required></select>' +
                    '</div>' +

                    '<div class="col-8 form-group">' +
                      '<label class="form-label" for="vchTitleInput">{{ \'admin.vouchers.fieldTitle\' | t }}</label>' +
                      '<input id="vchTitleInput" name="title" type="text" maxlength="150" class="form-control" ' +
                             'ng-model="$ctrl.draft.title" required ' +
                             'ng-attr-placeholder="{{ \'admin.vouchers.fieldTitlePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.title">{{ $ctrl.fieldErrors.title }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchCostInput">{{ \'admin.vouchers.fieldPointsCost\' | t }}</label>' +
                      '<input id="vchCostInput" name="pointsCost" type="number" min="1" step="10" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.pointsCost" required />' +
                    '</div>' +

                    '<div class="col-12 form-group">' +
                      '<label class="form-label" for="vchDescInput">{{ \'admin.vouchers.fieldDescription\' | t }}</label>' +
                      '<textarea id="vchDescInput" name="description" rows="2" maxlength="500" class="form-control" ' +
                                'ng-model="$ctrl.draft.description" required ' +
                                'ng-attr-placeholder="{{ \'admin.vouchers.fieldDescriptionPlaceholder\' | t }}"></textarea>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchDiscType">{{ \'admin.vouchers.fieldDiscountType\' | t }}</label>' +
                      '<select id="vchDiscType" name="discountType" class="form-control" ' +
                              'ng-model="$ctrl.draft.discountType" ' +
                              'ng-options="dt.value as dt.label for dt in $ctrl.discountTypes" required></select>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchDiscVal">{{ ($ctrl.draft.discountType === \'Percentage\' ? \'admin.vouchers.fieldDiscountPercent\' : \'admin.vouchers.fieldDiscountFixed\') | t }}</label>' +
                      '<input id="vchDiscVal" name="discountValue" type="number" min="1" max="{{ $ctrl.draft.discountType === \'Percentage\' ? 100 : 100000000 }}" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.discountValue" required />' +
                    '</div>' +

                    '<div class="col-4 form-group" ng-if="$ctrl.draft.discountType === \'Percentage\'">' +
                      '<label class="form-label" for="vchMaxCap">{{ \'admin.vouchers.fieldMaxCap\' | t }}</label>' +
                      '<input id="vchMaxCap" name="maxDiscountAmount" type="number" min="1000" step="10000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.maxDiscountAmount" required />' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchMinOrder">{{ \'admin.vouchers.fieldMinOrder\' | t }}</label>' +
                      '<input id="vchMinOrder" name="minOrderAmount" type="number" min="0" step="50000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.minOrderAmount" required />' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchValidDays">{{ \'admin.vouchers.fieldValidDays\' | t }}</label>' +
                      '<input id="vchValidDays" name="validDays" type="number" min="1" max="3650" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.validDays" required />' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="vchQuotaTotal">{{ \'admin.vouchers.fieldQuotaTotal\' | t }}</label>' +
                      '<input id="vchQuotaTotal" name="quotaTotal" type="number" min="1" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.quotaTotal" ng-attr-placeholder="{{ \'admin.vouchers.fieldQuotaPlaceholder\' | t }}" />' +
                    '</div>' +

                    '<div class="col-12 form-group" style="margin-bottom:0;">' +
                      '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;">' +
                        '<input type="checkbox" ng-model="$ctrl.draft.isActive" />' +
                        '<span>{{ \'admin.vouchers.fieldActiveCheck\' | t }}</span>' +
                      '</label>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeModal()" ng-disabled="$ctrl.isSaving">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'has-permission="Permissions.Loyalty.ManageVoucherCatalog" ' +
                          'ng-disabled="$ctrl.isSaving || $ctrl.catalogForm.$invalid">' +
                    '{{ ($ctrl.isSaving ? \'common.saving\' : ($ctrl.editingCatalogId ? \'admin.vouchers.saveVoucherCta\' : \'admin.vouchers.createVoucherSubmitCta\')) | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminVoucherCatalogController.$inject = [
    '$q',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    '$scope',
    'I18nService'
  ];
  function AdminVoucherCatalogController(
    $q,
    DealerApiService,
    LoyaltyCalculator,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    $scope,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var offLocale = null;

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

    function resolveMinTierIdByCode(tierCode) {
      for (var i = 0; i < ($ctrl.tiers || []).length; i++) {
        if ($ctrl.tiers[i].code === tierCode && $ctrl.tiers[i].id && $ctrl.tiers[i].id.length >= 32) {
          return $ctrl.tiers[i].id;
        }
      }
      return null;
    }

    function rebuildLocalizedOptions() {
      $ctrl.scopeOptions = [
        { value: 'ServiceOnly', label: t('admin.vouchers.scopeServiceOnly', 'ServiceOnly (Workshop Invoices)') },
        { value: 'ShowroomOnly', label: t('admin.vouchers.scopeShowroomOnly', 'ShowroomOnly (Bike Purchases)') },
        { value: 'All', label: t('admin.vouchers.scopeAll', 'All (Workshop & Showroom)') }
      ];
      $ctrl.scopeFilterOptions = [{ value: '', label: t('admin.vouchers.allScopesOption', 'All Scopes') }].concat(
        $ctrl.scopeOptions.map(function (s) { return angular.extend({}, s); })
      );

      $ctrl.tierFilterOptions = [
        { value: '', label: t('admin.vouchers.allTiersOption', 'All Tiers') },
        { value: 'Bronze', label: enumLabel('LoyaltyTierCode', 'Bronze') },
        { value: 'Silver', label: enumLabel('LoyaltyTierCode', 'Silver') },
        { value: 'Gold', label: enumLabel('LoyaltyTierCode', 'Gold') },
        { value: 'Platinum', label: enumLabel('LoyaltyTierCode', 'Platinum') }
      ];

      $ctrl.discountTypes = [
        { value: 'FixedAmount', label: t('admin.vouchers.discountTypeFixed', 'Fixed Amount (VND)') },
        { value: 'Percentage', label: t('admin.vouchers.discountTypePercent', 'Percentage (%)') }
      ];

      $ctrl.tierSelectOptions = ($ctrl.tiers || []).map(function (tier) {
        return {
          code: tier.code,
          label: enumLabel('LoyaltyTierCode', tier.code),
          pointsMultiplier: tier.pointsMultiplier
        };
      });
    }

    function recomputeStats() {
      var list = $ctrl.vouchers || [];
      var active = 0;
      var redeemed = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].isActive) {
          active++;
        }
        redeemed += Number(list[i].quotaRedeemed) || 0;
      }
      $ctrl.stats = {
        totalTemplates: list.length,
        activeTemplates: active,
        totalRedeemed: redeemed
      };
    }

    $ctrl.getScopeLabel = function (scopeCode) {
      if (scopeCode === 'ServiceOnly') {
        return t('admin.vouchers.scopeServiceOnly', 'ServiceOnly');
      }
      if (scopeCode === 'ShowroomOnly') {
        return t('admin.vouchers.scopeShowroomOnly', 'ShowroomOnly');
      }
      if (scopeCode === 'All') {
        return t('admin.vouchers.scopeAll', 'All');
      }
      return String(scopeCode || '');
    };

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isSaving = false;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.modalOpen = false;
      $ctrl.editingCatalogId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {};

      $ctrl.tiers = LoyaltyCalculator.cloneTiers().map(function (tier) {
        return angular.extend({}, tier, { pointsMultiplier: tier.multiplier });
      });

      rebuildLocalizedOptions();

      $ctrl.filters = {
        scope: '',
        minTierCode: '',
        search: ''
      };

      $ctrl.vouchers = [];
      recomputeStats();
      $ctrl.applyFilters();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildLocalizedOptions();
          }
        });
      }

      $ctrl.loadCatalog();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (angular.isFunction(offLocale)) {
        offLocale();
        offLocale = null;
      }
    };

    $ctrl.loadCatalog = function () {
      if (!DealerApiService || !DealerApiService.vouchers) {
        return $q ? $q.resolve($ctrl.vouchers) : $ctrl.vouchers;
      }
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        catalog: DealerApiService.vouchers.listCatalog({ includeInactive: true }),
        tiers: DealerApiService.loyalty.getTiers().catch(function () { return null; })
      }).then(function (bundle) {
        if (isDestroyed) { return $ctrl.vouchers; }
        if (angular.isArray(bundle.tiers) && bundle.tiers.length > 0) {
          $ctrl.tiers = bundle.tiers;
          rebuildLocalizedOptions();
        }
        $ctrl.vouchers = bundle.catalog || [];
        recomputeStats();
        $ctrl.applyFilters();
        return $ctrl.vouchers;
      }).catch(function (rejection) {
        if (!isDestroyed && ProblemDetailsMapper) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return $ctrl.vouchers;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.applyFilters = function () {
      var sc = ($ctrl.filters && $ctrl.filters.scope) || '';
      var mt = ($ctrl.filters && $ctrl.filters.minTierCode) || '';
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();

      $ctrl.filteredVouchers = ($ctrl.vouchers || []).filter(function (v) {
        if (sc && v.applicableScope !== sc) {
          return false;
        }
        if (mt && v.minTierCode !== mt) {
          return false;
        }
        if (q) {
          var hay = (v.codePrefix + ' ' + v.title + ' ' + (v.description || '')).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = { scope: '', minTierCode: '', search: '' };
      $ctrl.applyFilters();
    };

    $ctrl.openCreateModal = function () {
      $ctrl.editingCatalogId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {
        codePrefix: 'SVC',
        title: '',
        description: t('admin.vouchers.fieldDescriptionPlaceholder', 'Workshop maintenance loyalty reward voucher.'),
        applicableScope: 'ServiceOnly',
        minTierCode: 'Bronze',
        pointsCost: 150,
        discountType: 'FixedAmount',
        discountValue: 75000,
        maxDiscountAmount: 200000,
        minOrderAmount: 400000,
        validDays: 90,
        quotaTotal: 500,
        isActive: true
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.openEditModal = function (item) {
      if (!item) { return; }
      $ctrl.editingCatalogId = item.id;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {
        codePrefix: item.codePrefix,
        title: item.title,
        description: item.description || '',
        applicableScope: item.applicableScope,
        minTierCode: item.minTierCode || 'Bronze',
        pointsCost: item.pointsCost,
        discountType: item.discountType,
        discountValue: item.discountValue,
        maxDiscountAmount: item.maxDiscountAmount || 200000,
        minOrderAmount: item.minOrderAmount,
        validDays: item.validDays,
        quotaTotal: item.quotaTotal,
        isActive: item.isActive !== false
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.closeModal = function () {
      $ctrl.modalOpen = false;
      $ctrl.editingCatalogId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
    };

    $ctrl.saveCatalogItem = function () {
      if (($ctrl.catalogForm && $ctrl.catalogForm.$invalid) || $ctrl.isSaving || !DealerApiService || !DealerApiService.vouchers) {
        return $q ? $q.resolve(null) : null;
      }

      var payload = {
        codePrefix: String($ctrl.draft.codePrefix || '').trim().toUpperCase(),
        title: String($ctrl.draft.title || '').trim(),
        description: String($ctrl.draft.description || '').trim(),
        applicableScope: $ctrl.draft.applicableScope,
        pointsCost: Number($ctrl.draft.pointsCost) || 100,
        discountType: $ctrl.draft.discountType,
        discountValue: Number($ctrl.draft.discountValue) || 0,
        maxDiscountAmount: $ctrl.draft.discountType === 'Percentage' ? (Number($ctrl.draft.maxDiscountAmount) || 0) : null,
        minOrderAmount: Number($ctrl.draft.minOrderAmount) || 0,
        validDays: Number($ctrl.draft.validDays) || 90,
        quotaTotal: $ctrl.draft.quotaTotal !== '' && $ctrl.draft.quotaTotal !== null && $ctrl.draft.quotaTotal !== undefined
          ? Number($ctrl.draft.quotaTotal)
          : null,
        minTierId: resolveMinTierIdByCode($ctrl.draft.minTierCode),
        minTierCode: $ctrl.draft.minTierCode,
        isActive: Boolean($ctrl.draft.isActive)
      };

      $ctrl.isSaving = true;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};

      var req = $ctrl.editingCatalogId
        ? DealerApiService.vouchers.updateCatalog($ctrl.editingCatalogId, payload)
        : DealerApiService.vouchers.createCatalog(payload);

      return req.then(function (saved) {
        if (isDestroyed) { return saved; }
        if (ToastService) {
          ToastService.success(
            $ctrl.editingCatalogId
              ? t('admin.vouchers.toastUpdatedTitle', 'Voucher Template Updated')
              : t('admin.vouchers.toastCreatedTitle', 'Voucher Template Created'),
            t('admin.vouchers.toastSavedBody', '{{prefix}} · {{title}} saved.', {
              prefix: saved.codePrefix,
              title: saved.title
            })
          );
        }
        $ctrl.closeModal();
        $ctrl.loadCatalog();
        return saved;
      }).catch(function (rejection) {
        if (!isDestroyed && ProblemDetailsMapper) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.catalogForm, problem);
          $ctrl.modalProblem = problem;
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSaving = false;
        }
      });
    };
  }
})();
