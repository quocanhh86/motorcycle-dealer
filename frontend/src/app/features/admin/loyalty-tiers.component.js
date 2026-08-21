(function () {
  'use strict';

  var GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  var I18N_PATCH_VI = {
    admin: {
      loyaltyTiers: {
        screenCode: 'SCR-ADM-05 · FR-05 / ADR-003',
        backToAnalytics: 'Phân tích HQ',
        refreshing: 'Đang tải lại...',
        recordAdjustmentCta: 'Ghi nhận điều chỉnh điểm',
        kpiTotalTiers: 'Số hạng thành viên',
        kpiMaxMultiplier: 'Hệ số nhân cao nhất',
        kpiBaseSpendRule: 'Quy đổi gốc chuẩn',
        kpiIndexedMembers: 'Khách hàng khả dụng',
        adjustmentSuccessTitle: 'Đã ghi nối tiếp điều chỉnh bất biến:',
        newAvailableBalance: 'Số dư khả dụng mới:',
        dismissBtn: 'Ẩn thông báo',
        colRank: 'Thứ bậc',
        colTierBadge: 'Hạng & Tên hiển thị',
        colMinQualifying: 'Điểm xét hạng tối thiểu',
        colMultiplier: 'Hệ số tích điểm',
        colPerks: 'Quyền lợi thành viên (BenefitsJson)',
        formulaHint: 'Floor(Floor(Thực chi / 10.000) × {{mult}})',
        standardPerksFallback: 'Quyền lợi chuẩn theo hạng',
        editTierBtn: 'Chỉnh sửa hạng',
        emptyTitle: 'Chưa tải được cấu hình hạng thành viên',
        emptyDesc: 'Thử tải lại dữ liệu hạng hội viên chuẩn từ máy chủ.',
        modalEditTitle: 'Chỉnh sửa cấu hình hạng {{code}}',
        fieldDisplayName: 'Tên hiển thị * (Tối đa 60 ký tự)',
        fieldMinPoints: 'Điểm xét hạng tối thiểu *',
        bronzeZeroHint: 'Hạng khởi điểm Đồng (Bronze) bắt buộc giữ mức 0 điểm.',
        fieldMultiplier: 'Hệ số nhân điểm * (>= 1.00x)',
        fieldPerks: 'Quyền lợi thành viên (Mỗi dòng một quyền lợi cho BenefitsJson)',
        fieldPerksPlaceholder: 'Hệ số tích điểm ưu tiên\nƯu tiên tiếp nhận xưởng dịch vụ',
        savingTierCta: 'Đang lưu hạng...',
        saveTierCta: 'Lưu thay đổi hạng',
        modalAdjTitle: 'Ghi nhận điều chỉnh sổ cái điểm thưởng có kiểm toán',
        fieldQuickPick: 'Khách hàng ghi nhận gần đây (Chọn nhanh tùy chọn)',
        fieldQuickPickEmpty: '— Tùy chọn: Chọn khách hàng hoặc nhập Email / GUID bên dưới —',
        fieldCustomerIdent: 'Email hoặc GUID khách hàng *',
        fieldCustomerIdentPlaceholder: 'Nhập địa chỉ email hoặc mã GUID khách hàng',
        lookupWalletBtn: 'Tra cứu ví điểm',
        lookingUpBtn: 'Đang tra cứu...',
        walletFallbackTitle: 'Ví điểm khách hàng',
        walletAvailable: 'Khả dụng:',
        walletQualifying: 'Xét hạng:',
        walletProjected: 'Dự kiến sau điều chỉnh:',
        fieldDelta: 'Biến động điểm có dấu * (Số nguyên khác 0)',
        deltaZeroError: 'Biến động điểm phải khác 0 (+cộng hoặc -trừ).',
        deltaNegativeError: 'Điều chỉnh giảm không được làm số dư điểm khả dụng xuống dưới 0.',
        fieldQualifiesCheck: 'Tính cộng dồn vào điểm tích lũy xét hạng (LifetimeQualifyingPoints)',
        fieldAuditNotes: 'Lý do kiểm toán bắt buộc * (10–500 ký tự)',
        fieldAuditNotesPlaceholder: 'Nhập lý do kiểm toán bắt buộc cho giao dịch sổ cái (10–500 ký tự)',
        appendingLedgerCta: 'Đang ghi sổ cái...',
        appendAdjustmentCta: 'Ghi nối tiếp điều chỉnh bất biến',
        toastTierUpdatedTitle: 'Đã cập nhật hạng thành viên',
        toastTierUpdatedBody: 'Hạng {{code}} ({{mult}}x) đã được lưu.',
        toastAdjRecordedTitle: 'Đã ghi nhận điều chỉnh điểm',
        toastAdjRecordedBody: '{{signed}} điểm đã được ghi nối tiếp vào PointLedgerEntries.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      loyaltyTiers: {
        screenCode: 'SCR-ADM-05 · FR-05 / ADR-003',
        backToAnalytics: 'Analytics',
        refreshing: 'Refreshing...',
        recordAdjustmentCta: 'Record Point Adjustment',
        kpiTotalTiers: 'Configured Tiers',
        kpiMaxMultiplier: 'Top Earn Multiplier',
        kpiBaseSpendRule: 'Base Spend Rule',
        kpiIndexedMembers: 'Indexed Live Customers',
        adjustmentSuccessTitle: 'Immutable Adjustment Appended:',
        newAvailableBalance: 'New Available Balance:',
        dismissBtn: 'Dismiss',
        colRank: 'Rank',
        colTierBadge: 'Tier Badge & Name',
        colMinQualifying: 'Min Qualifying Points',
        colMultiplier: 'Accrual Multiplier',
        colPerks: 'Member Perks (BenefitsJson)',
        formulaHint: 'Floor(Floor(Net / 10k) × {{mult}})',
        standardPerksFallback: 'Standard tier benefits',
        editTierBtn: 'Edit Tier',
        emptyTitle: 'No loyalty tiers loaded',
        emptyDesc: 'Try refreshing the canonical 4-tier loyalty configuration from the API.',
        modalEditTitle: 'Edit {{code}} Tier Configuration',
        fieldDisplayName: 'Display Name * (Max 60 chars)',
        fieldMinPoints: 'Min Qualifying Points *',
        bronzeZeroHint: 'Bronze entry tier must remain at 0 qualifying points.',
        fieldMultiplier: 'Points Multiplier * (>= 1.00x)',
        fieldPerks: 'Member Perks (One perk per line for BenefitsJson)',
        fieldPerksPlaceholder: 'Accelerated points multiplier\nPriority workshop check-in',
        savingTierCta: 'Saving Tier...',
        saveTierCta: 'Save Tier Changes',
        modalAdjTitle: 'Record Audited Point Ledger Adjustment',
        fieldQuickPick: 'Recently Seen Live Customers (Optional Quick-Pick)',
        fieldQuickPickEmpty: '— Optional: Pick Live Customer or Enter Email / GUID Below —',
        fieldCustomerIdent: 'Customer Email or Customer GUID *',
        fieldCustomerIdentPlaceholder: 'Enter customer email address or GUID',
        lookupWalletBtn: 'Lookup Wallet',
        lookingUpBtn: 'Loading...',
        walletFallbackTitle: 'Customer Wallet',
        walletAvailable: 'Available:',
        walletQualifying: 'Qualifying:',
        walletProjected: 'Projected Available:',
        fieldDelta: 'Signed Points Delta * (Non-Zero Integer)',
        deltaZeroError: 'PointsDelta must be non-zero (+credit or -debit).',
        deltaNegativeError: 'Negative adjustment cannot drop AvailablePoints below 0.',
        fieldQualifiesCheck: 'Count toward Lifetime Qualifying Points',
        fieldAuditNotes: 'Mandatory Audit Justification Notes * (10–500 chars)',
        fieldAuditNotesPlaceholder: 'Enter mandatory audit justification (10–500 characters)',
        appendingLedgerCta: 'Appending Ledger Entry...',
        appendAdjustmentCta: 'Append Immutable Adjustment',
        toastTierUpdatedTitle: 'Loyalty Tier Updated',
        toastTierUpdatedBody: '{{code}} ({{mult}}x) saved.',
        toastAdjRecordedTitle: 'Point Adjustment Recorded',
        toastAdjRecordedBody: '{{signed}} pts appended to PointLedgerEntries.'
      }
    }
  };

  registerLoyaltyTiersI18n.$inject = ['I18nService'];
  function registerLoyaltyTiersI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerLoyaltyTiersI18n)
    .component('adminLoyaltyTiers', {
      controllerAs: '$ctrl',
      controller: AdminLoyaltyTiersController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.loyaltyTiers.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.loyaltyTiersTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.loyaltyTiersSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.loyaltyTiers.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadTiers()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.loyaltyTiers.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Loyalty.AdjustPoints" ' +
                      'ng-click="$ctrl.openAdjustmentModal()">' +
                '<span>{{ \'admin.loyaltyTiers.recordAdjustmentCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="coins" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadTiers()">' +
          '</error-retry-banner>' +

          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.lastAdjustmentResult">' +
            '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
              '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                '<app-icon name="check-circle" weight="fill" size="18"></app-icon>' +
                '<strong>{{ \'admin.loyaltyTiers.adjustmentSuccessTitle\' | t }}</strong>' +
                '<span class="mono-num">{{ $ctrl.lastAdjustmentResult.pointsDelta >= 0 ? \'+\' : \'\' }}{{ $ctrl.lastAdjustmentResult.pointsDelta | number }} {{ \'common.ptsShort\' | t }}</span>' +
                '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                '<span>{{ \'admin.loyaltyTiers.newAvailableBalance\' | t }} <strong class="mono-num">{{ $ctrl.lastAdjustmentResult.availablePointsAfter | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                '<code class="mono-num" ng-if="$ctrl.lastAdjustmentResult.idempotencyKey">{{ $ctrl.lastAdjustmentResult.idempotencyKey }}</code>' +
              '</div>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.lastAdjustmentResult = null">{{ \'admin.loyaltyTiers.dismissBtn\' | t }}</button>' +
            '</div>' +
          '</div>' +

          /* 4-Tile Double-Bezel KPI Summary Strip */
          '<div class="bento-grid" style="margin-bottom:20px;">' +
            '<div class="col-3 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.loyaltyTiers.kpiTotalTiers\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalTiers" kpi-format="number">{{ $ctrl.stats.totalTiers }}</div>' +
                '</div>' +
                '<app-icon name="crown" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.loyaltyTiers.kpiMaxMultiplier\' | t }}</div>' +
                  '<div class="kpi-value mono-num">{{ $ctrl.stats.maxMultiplier | number:2 }}x</div>' +
                '</div>' +
                '<app-icon name="sparkle" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.loyaltyTiers.kpiBaseSpendRule\' | t }}</div>' +
                  '<div class="kpi-value mono-num" style="font-size:18px;">{{ 10000 | vndCurrency }}</div>' +
                '</div>' +
                '<app-icon name="scales" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:3;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.loyaltyTiers.kpiIndexedMembers\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.customerDirectory.length" kpi-format="number">{{ $ctrl.customerDirectory.length }}</div>' +
                '</div>' +
                '<app-icon name="users-three" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="4" ng-if="$ctrl.isLoading && $ctrl.tiers.length === 0"></skeleton-loader>' +

          '<div class="bezel-card quiet-table-frame" ng-if="!$ctrl.isLoading || $ctrl.tiers.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.tiers.length === 0" ' +
                           'icon="crown" ' +
                           'title-key="admin.loyaltyTiers.emptyTitle" ' +
                           'desc-key="admin.loyaltyTiers.emptyDesc" ' +
                           'action-key="common.refresh" ' +
                           'on-action="$ctrl.loadTiers()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.tiers.length > 0">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th>{{ \'admin.loyaltyTiers.colRank\' | t }}</th>' +
                      '<th>{{ \'admin.loyaltyTiers.colTierBadge\' | t }}</th>' +
                      '<th>{{ \'admin.loyaltyTiers.colMinQualifying\' | t }}</th>' +
                      '<th>{{ \'admin.loyaltyTiers.colMultiplier\' | t }}</th>' +
                      '<th>{{ \'admin.loyaltyTiers.colPerks\' | t }}</th>' +
                      '<th>{{ \'common.actions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="tier in $ctrl.tiers track by tier.code">' +
                      '<td class="mono-num" style="font-weight:700;">#{{ tier.rankOrder }}</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="\'badge-tier-\' + tier.code.toLowerCase()">' +
                          '<app-icon name="crown" weight="fill" size="12"></app-icon>' +
                          '<span>{{ tier.code | enumT:\'LoyaltyTierCode\' }}</span>' +
                        '</span>' +
                        '<div class="text-primary" style="font-size:12px;font-weight:600;margin-top:4px;">{{ tier.displayName }}</div>' +
                      '</td>' +
                      '<td class="mono-num">{{ tier.minQualifyingPoints | number }} {{ \'common.ptsShort\' | t }}</td>' +
                      '<td class="mono-num">' +
                        '<strong>{{ tier.pointsMultiplier | number:2 }}x</strong>' +
                        '<div class="text-muted" style="font-size:11px;">{{ \'admin.loyaltyTiers.formulaHint\' | t:{ mult: (tier.pointsMultiplier | number:2) } }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<div class="text-secondary" style="display:flex;flex-direction:column;gap:4px;font-size:12px;">' +
                          '<span style="display:inline-flex;align-items:center;gap:6px;" ng-repeat="perk in tier.benefits track by $index">' +
                            '<app-icon name="check" weight="regular" size="13"></app-icon>' +
                            '<span>{{ perk }}</span>' +
                          '</span>' +
                          '<span ng-if="!tier.benefits || tier.benefits.length === 0" class="text-muted">{{ \'admin.loyaltyTiers.standardPerksFallback\' | t }}</span>' +
                        '</div>' +
                      '</td>' +
                      '<td>' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'has-permission="Permissions.Loyalty.ManageTiers" ' +
                                'ng-click="$ctrl.openEditTierModal(tier)">' +
                          '<app-icon name="pencil-simple" weight="regular" size="13"></app-icon>' +
                          '<span>{{ \'admin.loyaltyTiers.editTierBtn\' | t }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* MODAL 1: EDIT LOYALTY TIER (PUT /api/v1/loyalty/tiers/{tierId}) */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="editTierTitle" ng-if="$ctrl.editingTier">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">PUT /api/v1/loyalty/tiers/{{ $ctrl.editingTier.id }}</span>' +
                  '<h2 id="editTierTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ \'admin.loyaltyTiers.modalEditTitle\' | t:{ code: ($ctrl.editingTier.code | enumT:\'LoyaltyTierCode\') } }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeEditTierModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.tierForm" ng-submit="$ctrl.saveTier()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.tierModalProblem">' +
                    '<strong>{{ $ctrl.tierModalProblem.title }} ({{ $ctrl.tierModalProblem.errorCode }}):</strong> {{ $ctrl.tierModalProblem.detail }}' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="tierDisplayName">{{ \'admin.loyaltyTiers.fieldDisplayName\' | t }}</label>' +
                    '<input id="tierDisplayName" name="displayName" type="text" maxlength="60" class="form-control" ' +
                           'ng-model="$ctrl.tierDraft.displayName" required />' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="tierMinPts">{{ \'admin.loyaltyTiers.fieldMinPoints\' | t }}</label>' +
                      '<input id="tierMinPts" name="minQualifyingPoints" type="number" min="0" step="50" class="form-control mono-num" ' +
                             'ng-model="$ctrl.tierDraft.minQualifyingPoints" ' +
                             'ng-readonly="$ctrl.editingTier.code === \'Bronze\'" required />' +
                      '<div class="field-hint" ng-if="$ctrl.editingTier.code === \'Bronze\'">{{ \'admin.loyaltyTiers.bronzeZeroHint\' | t }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="tierMultInput">{{ \'admin.loyaltyTiers.fieldMultiplier\' | t }}</label>' +
                      '<input id="tierMultInput" name="pointsMultiplier" type="number" min="1" max="10" step="0.05" class="form-control mono-num" ' +
                             'ng-model="$ctrl.tierDraft.pointsMultiplier" required />' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="tierPerksText">{{ \'admin.loyaltyTiers.fieldPerks\' | t }}</label>' +
                    '<textarea id="tierPerksText" name="benefitsText" rows="4" class="form-control" ' +
                              'ng-model="$ctrl.tierDraft.benefitsText" ' +
                              'ng-attr-placeholder="{{ \'admin.loyaltyTiers.fieldPerksPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeEditTierModal()" ng-disabled="$ctrl.isSavingTier">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'has-permission="Permissions.Loyalty.ManageTiers" ' +
                          'ng-disabled="$ctrl.isSavingTier || $ctrl.tierForm.$invalid">' +
                    '{{ ($ctrl.isSavingTier ? \'admin.loyaltyTiers.savingTierCta\' : \'admin.loyaltyTiers.saveTierCta\') | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* MODAL 2: AUDITED POINT ADJUSTMENT (POST /api/v1/loyalty/adjustments) */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="adjModalTitle" ng-if="$ctrl.adjustmentModalOpen">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-tier-platinum mono-num">POST /api/v1/loyalty/adjustments · ADR-003</span>' +
                  '<h2 id="adjModalTitle" class="surface-card-title" style="margin-top:4px;">{{ \'admin.loyaltyTiers.modalAdjTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeAdjustmentModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.adjustForm" ng-submit="$ctrl.submitPointAdjustment()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.adjustProblem">' +
                    '<strong>{{ $ctrl.adjustProblem.title }} ({{ $ctrl.adjustProblem.errorCode }}):</strong> {{ $ctrl.adjustProblem.detail }}' +
                  '</div>' +

                  '<div class="form-group" ng-if="$ctrl.customerDirectory.length > 0">' +
                    '<label class="form-label" for="adjCustomerSelect">{{ \'admin.loyaltyTiers.fieldQuickPick\' | t }}</label>' +
                    '<select id="adjCustomerSelect" name="selectedCustomerId" class="form-control" ' +
                            'ng-model="$ctrl.selectedDirectoryCustomerId" ' +
                            'ng-options="c.customerId as (c.fullName + \' · \' + c.email) for c in $ctrl.customerDirectory" ' +
                            'ng-change="$ctrl.onDirectoryCustomerPicked()">' +
                      '<option value="">{{ \'admin.loyaltyTiers.fieldQuickPickEmpty\' | t }}</option>' +
                    '</select>' +
                  '</div>' +

                  '<div class="form-group">' +
                    '<label class="form-label" for="adjCustomerIdentifier">{{ \'admin.loyaltyTiers.fieldCustomerIdent\' | t }}</label>' +
                    '<div class="input-action-group">' +
                      '<input id="adjCustomerIdentifier" name="customerIdentifier" type="text" class="form-control" ' +
                             'ng-model="$ctrl.adjustDraft.customerIdentifier" ' +
                             'ng-change="$ctrl.onAdjustmentIdentifierChanged()" required ' +
                             'ng-attr-placeholder="{{ \'admin.loyaltyTiers.fieldCustomerIdentPlaceholder\' | t }}" />' +
                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-click="$ctrl.lookupAdjustmentCustomer()" ' +
                              'ng-disabled="$ctrl.isLookingUpCustomer || !$ctrl.adjustDraft.customerIdentifier">' +
                        '<app-icon name="magnifying-glass" weight="regular" size="14"></app-icon>' +
                        '<span>{{ ($ctrl.isLookingUpCustomer ? \'admin.loyaltyTiers.lookingUpBtn\' : \'admin.loyaltyTiers.lookupWalletBtn\') | t }}</span>' +
                      '</button>' +
                    '</div>' +
                  '</div>' +

                  '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:14px;font-size:13px;" ' +
                       'ng-if="$ctrl.inspectedCustomer">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<strong class="text-primary">{{ $ctrl.inspectedCustomer.customerFullName || (\'admin.loyaltyTiers.walletFallbackTitle\' | t) }}</strong>' +
                      '<span class="badge" ng-class="\'badge-tier-\' + ($ctrl.inspectedCustomer.currentTierCode || \'Bronze\').toLowerCase()">' +
                        '<app-icon name="crown" weight="fill" size="12"></app-icon>' +
                        '<span>{{ $ctrl.inspectedCustomer.currentTierCode | enumT:\'LoyaltyTierCode\' }} ({{ $ctrl.inspectedCustomer.pointsMultiplier | number:2 }}x)</span>' +
                      '</span>' +
                    '</div>' +
                    '<div style="margin-top:8px;display:flex;gap:16px;flex-wrap:wrap;" class="mono-num text-secondary">' +
                      '<span>{{ \'admin.loyaltyTiers.walletAvailable\' | t }} <strong class="text-primary">{{ $ctrl.inspectedCustomer.availablePoints | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                      '<span>{{ \'admin.loyaltyTiers.walletQualifying\' | t }} <strong class="text-primary">{{ $ctrl.inspectedCustomer.lifetimeQualifyingPoints | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                      '<span>{{ \'admin.loyaltyTiers.walletProjected\' | t }} <strong class="text-primary">{{ ($ctrl.inspectedCustomer.availablePoints + ($ctrl.adjustDraft.pointsDelta || 0)) | number }} {{ \'common.ptsShort\' | t }}</strong></span>' +
                    '</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="adjDeltaInput">{{ \'admin.loyaltyTiers.fieldDelta\' | t }}</label>' +
                      '<input id="adjDeltaInput" name="pointsDelta" type="number" step="10" class="form-control mono-num" ' +
                             'ng-model="$ctrl.adjustDraft.pointsDelta" required />' +
                      '<div class="field-error" ng-if="$ctrl.adjustDraft.pointsDelta === 0">{{ \'admin.loyaltyTiers.deltaZeroError\' | t }}</div>' +
                      '<div class="field-error" ng-if="$ctrl.inspectedCustomer && ($ctrl.inspectedCustomer.availablePoints + ($ctrl.adjustDraft.pointsDelta || 0)) < 0">' +
                        '{{ \'admin.loyaltyTiers.deltaNegativeError\' | t }}' +
                      '</div>' +
                    '</div>' +

                    '<div class="col-6 form-group" style="justify-content:center;">' +
                      '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;margin-top:22px;">' +
                        '<input type="checkbox" ng-model="$ctrl.adjustDraft.qualifiesForTier" />' +
                        '<span>{{ \'admin.loyaltyTiers.fieldQualifiesCheck\' | t }}</span>' +
                      '</label>' +
                    '</div>' +
                  '</div>' +

                  '<div class="form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="adjNotesInput">{{ \'admin.loyaltyTiers.fieldAuditNotes\' | t }}</label>' +
                    '<textarea id="adjNotesInput" name="notes" rows="3" minlength="10" maxlength="500" class="form-control" ' +
                              'ng-model="$ctrl.adjustDraft.notes" required ' +
                              'ng-attr-placeholder="{{ \'admin.loyaltyTiers.fieldAuditNotesPlaceholder\' | t }}"></textarea>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeAdjustmentModal()" ng-disabled="$ctrl.isSubmittingAdjustment">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" id="submitPointAdjustmentBtn" class="btn btn-primary" ' +
                          'has-permission="Permissions.Loyalty.AdjustPoints" ' +
                          'ng-disabled="$ctrl.isSubmittingAdjustment || !$ctrl.canSubmitAdjustment()">' +
                    '<span>{{ ($ctrl.isSubmittingAdjustment ? \'admin.loyaltyTiers.appendingLedgerCta\' : \'admin.loyaltyTiers.appendAdjustmentCta\') | t }}</span>' +
                    '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminLoyaltyTiersController.$inject = [
    '$q',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    '$scope',
    'I18nService'
  ];
  function AdminLoyaltyTiersController(
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

    function recomputeStats() {
      var list = $ctrl.tiers || [];
      var maxMult = 1.0;
      for (var i = 0; i < list.length; i++) {
        var m = Number(list[i].pointsMultiplier) || 1.0;
        if (m > maxMult) {
          maxMult = m;
        }
      }
      $ctrl.stats = {
        totalTiers: list.length,
        maxMultiplier: maxMult
      };
    }

    function mergeLiveCustomers(vehicles, orders, bookings) {
      var byKey = Object.create(null);
      var out = [];

      function addRec(cid, email, fullName) {
        var normId = cid ? String(cid).trim() : '';
        var normEmail = email ? String(email).trim() : '';
        if (!normId && !normEmail) { return; }
        var key = normId ? normId.toLowerCase() : normEmail.toLowerCase();
        if (byKey[key]) {
          if (!byKey[key].email && normEmail) { byKey[key].email = normEmail; }
          if (!byKey[key].fullName && fullName) { byKey[key].fullName = fullName; }
          return;
        }
        var rec = {
          customerId: normId || normEmail,
          fullName: fullName || normEmail || t('common.customer', 'Customer'),
          email: normEmail
        };
        byKey[key] = rec;
        out.push(rec);
      }

      (angular.isArray(vehicles) ? vehicles : []).forEach(function (v) {
        addRec(v.customerId, v.customerEmail, v.customerFullName);
      });
      (angular.isArray(orders) ? orders : []).forEach(function (o) {
        addRec(o.customerId, o.customerEmail, o.customerFullName);
      });
      (angular.isArray(bookings) ? bookings : []).forEach(function (b) {
        addRec(b.customerId, b.customerEmail, b.customerFullName);
      });
      $ctrl.customerDirectory = out;
    }

    function loadLiveCustomers() {
      if (!DealerApiService || !$q) {
        return $q ? $q.resolve([]) : null;
      }
      var vehiclesP = angular.isFunction(DealerApiService.lookupVehicles)
        ? DealerApiService.lookupVehicles({ pageSize: 100 }).catch(function () { return []; })
        : $q.resolve([]);
      var ordersP = angular.isFunction(DealerApiService.listSalesOrders)
        ? DealerApiService.listSalesOrders({ pageSize: 50 }).catch(function () { return []; })
        : $q.resolve([]);
      var bookingsP = angular.isFunction(DealerApiService.listBookings)
        ? DealerApiService.listBookings({ pageSize: 50 }).catch(function () { return []; })
        : $q.resolve([]);

      return $q.all({ vehicles: vehiclesP, orders: ordersP, bookings: bookingsP })
        .then(function (bundle) {
          if (!isDestroyed) {
            mergeLiveCustomers(bundle.vehicles, bundle.orders, bundle.bookings);
          }
          return $ctrl.customerDirectory;
        });
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isLookingUpCustomer = false;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;
      $ctrl.tiers = [];
      $ctrl.customerDirectory = [];
      $ctrl.selectedDirectoryCustomerId = '';

      $ctrl.editingTier = null;
      $ctrl.isSavingTier = false;
      $ctrl.tierModalProblem = null;
      $ctrl.tierDraft = {};

      $ctrl.adjustmentModalOpen = false;
      $ctrl.isSubmittingAdjustment = false;
      $ctrl.adjustProblem = null;
      $ctrl.inspectedCustomer = null;
      $ctrl.lastAdjustmentResult = null;
      $ctrl.adjustDraft = {};

      recomputeStats();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            recomputeStats();
          }
        });
      }

      $ctrl.loadTiers();
      loadLiveCustomers();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (angular.isFunction(offLocale)) {
        offLocale();
        offLocale = null;
      }
    };

    $ctrl.loadTiers = function () {
      if (!DealerApiService || !DealerApiService.loyalty) {
        return $q ? $q.resolve($ctrl.tiers) : $ctrl.tiers;
      }
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.loyalty.getTiers()
        .then(function (rows) {
          if (!isDestroyed && angular.isArray(rows)) {
            $ctrl.tiers = rows;
            recomputeStats();
          }
          return $ctrl.tiers;
        })
        .catch(function (rejection) {
          if (!isDestroyed && ProblemDetailsMapper) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.tiers;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.openEditTierModal = function (tier) {
      if (!tier) { return; }
      $ctrl.editingTier = tier;
      $ctrl.tierModalProblem = null;
      $ctrl.tierDraft = {
        displayName: tier.displayName || (tier.code + ' Member'),
        minQualifyingPoints: tier.minQualifyingPoints,
        pointsMultiplier: tier.pointsMultiplier,
        benefitsText: angular.isArray(tier.benefits) ? tier.benefits.join('\n') : '',
        rowVersion: tier.rowVersion || ''
      };
    };

    $ctrl.closeEditTierModal = function () {
      $ctrl.editingTier = null;
      $ctrl.tierModalProblem = null;
      $ctrl.tierDraft = {};
    };

    $ctrl.saveTier = function () {
      if (!$ctrl.editingTier || ($ctrl.tierForm && $ctrl.tierForm.$invalid) || $ctrl.isSavingTier || !DealerApiService || !DealerApiService.loyalty) {
        return $q ? $q.resolve(null) : null;
      }

      var perks = String($ctrl.tierDraft.benefitsText || '')
        .split(/\r?\n/)
        .map(function (line) { return line.trim(); })
        .filter(function (line) { return line.length > 0; });
      var benefitsJson = JSON.stringify(perks);

      $ctrl.isSavingTier = true;
      $ctrl.tierModalProblem = null;

      return DealerApiService.loyalty.updateTier($ctrl.editingTier.id, {
        displayName: $ctrl.tierDraft.displayName,
        minQualifyingPoints: $ctrl.editingTier.code === 'Bronze' ? 0 : Number($ctrl.tierDraft.minQualifyingPoints) || 0,
        pointsMultiplier: Number($ctrl.tierDraft.pointsMultiplier) || 1.0,
        benefitsJson: benefitsJson,
        rowVersion: $ctrl.tierDraft.rowVersion || null
      }).then(function (updated) {
        if (isDestroyed) { return updated; }
        if (ToastService) {
          ToastService.success(
            t('admin.loyaltyTiers.toastTierUpdatedTitle', 'Loyalty Tier Updated'),
            t('admin.loyaltyTiers.toastTierUpdatedBody', '{{code}} ({{mult}}x) saved.', {
              code: updated.code,
              mult: updated.pointsMultiplier.toFixed(2)
            })
          );
        }
        $ctrl.closeEditTierModal();
        $ctrl.loadTiers();
        return updated;
      }).catch(function (rejection) {
        if (!isDestroyed && ProblemDetailsMapper) {
          $ctrl.tierModalProblem = ProblemDetailsMapper.extract(rejection);
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSavingTier = false;
        }
      });
    };

    /* Audited Point Adjustment Modal */
    $ctrl.openAdjustmentModal = function () {
      $ctrl.adjustProblem = null;
      $ctrl.inspectedCustomer = null;
      $ctrl.selectedDirectoryCustomerId = '';
      $ctrl.adjustDraft = {
        customerIdentifier: '',
        customerId: '',
        customerEmail: '',
        pointsDelta: 0,
        qualifiesForTier: true,
        notes: ''
      };
      $ctrl.adjustmentModalOpen = true;
      if ($ctrl.customerDirectory.length === 0) {
        loadLiveCustomers();
      }
    };

    $ctrl.closeAdjustmentModal = function () {
      $ctrl.adjustmentModalOpen = false;
      $ctrl.adjustProblem = null;
      $ctrl.inspectedCustomer = null;
    };

    $ctrl.onDirectoryCustomerPicked = function () {
      $ctrl.inspectedCustomer = null;
      var target = $ctrl.selectedDirectoryCustomerId;
      for (var i = 0; i < $ctrl.customerDirectory.length; i++) {
        var c = $ctrl.customerDirectory[i];
        if (c.customerId === target) {
          $ctrl.adjustDraft.customerId = GUID_REGEX.test(c.customerId) ? c.customerId : '';
          $ctrl.adjustDraft.customerEmail = c.email || '';
          $ctrl.adjustDraft.customerIdentifier = c.email || c.customerId;
          $ctrl.lookupAdjustmentCustomer();
          return;
        }
      }
    };

    $ctrl.onAdjustmentIdentifierChanged = function () {
      $ctrl.inspectedCustomer = null;
      var raw = String($ctrl.adjustDraft.customerIdentifier || '').trim();
      if (GUID_REGEX.test(raw)) {
        $ctrl.adjustDraft.customerId = raw;
        $ctrl.adjustDraft.customerEmail = '';
        $ctrl.selectedDirectoryCustomerId = raw;
      } else {
        $ctrl.adjustDraft.customerEmail = raw;
        var matchedId = '';
        var lower = raw.toLowerCase();
        for (var i = 0; i < $ctrl.customerDirectory.length; i++) {
          if ($ctrl.customerDirectory[i].email && $ctrl.customerDirectory[i].email.toLowerCase() === lower) {
            matchedId = $ctrl.customerDirectory[i].customerId;
            break;
          }
        }
        $ctrl.adjustDraft.customerId = GUID_REGEX.test(matchedId) ? matchedId : '';
        $ctrl.selectedDirectoryCustomerId = matchedId;
      }
    };

    $ctrl.onAdjustmentCustomerChanged = function () {
      var cid = $ctrl.adjustDraft.customerId;
      if (!cid || !DealerApiService || !DealerApiService.loyalty) {
        $ctrl.inspectedCustomer = null;
        return $q ? $q.resolve(null) : null;
      }
      return DealerApiService.loyalty.getCustomerSummary(cid)
        .then(function (summary) {
          if (!isDestroyed && $ctrl.adjustDraft.customerId === cid) {
            $ctrl.inspectedCustomer = summary;
          }
          return summary;
        })
        .catch(function () {
          return null;
        });
    };

    $ctrl.lookupAdjustmentCustomer = function () {
      if (!DealerApiService) {
        return $q ? $q.resolve(null) : null;
      }
      var raw = String($ctrl.adjustDraft.customerIdentifier || $ctrl.adjustDraft.customerEmail || $ctrl.adjustDraft.customerId || '').trim();
      var targetGuid = GUID_REGEX.test($ctrl.adjustDraft.customerId || '')
        ? String($ctrl.adjustDraft.customerId).trim()
        : (GUID_REGEX.test(raw) ? raw : '');

      if (!targetGuid && !raw) {
        $ctrl.inspectedCustomer = null;
        return $q ? $q.resolve(null) : null;
      }

      $ctrl.isLookingUpCustomer = true;
      var lookupPromise = targetGuid
        ? (function () {
          $ctrl.adjustDraft.customerId = targetGuid;
          return $ctrl.onAdjustmentCustomerChanged();
        })()
        : (angular.isFunction(DealerApiService.lookupVehicles)
          ? DealerApiService.lookupVehicles({ email: raw })
            .then(function (rows) {
              var first = angular.isArray(rows) && rows.length > 0 ? rows[0] : null;
              if (first && first.customerId) {
                $ctrl.adjustDraft.customerId = first.customerId;
                return $ctrl.onAdjustmentCustomerChanged();
              }
              return null;
            })
            .catch(function () { return null; })
          : $q.resolve(null));

      return lookupPromise.finally(function () {
        if (!isDestroyed) {
          $ctrl.isLookingUpCustomer = false;
        }
      });
    };

    $ctrl.canSubmitAdjustment = function () {
      var delta = Math.trunc(Number($ctrl.adjustDraft.pointsDelta) || 0);
      var notes = String($ctrl.adjustDraft.notes || '').trim();
      var hasTarget = Boolean(
        String($ctrl.adjustDraft.customerIdentifier || '').trim() ||
        String($ctrl.adjustDraft.customerId || '').trim() ||
        String($ctrl.adjustDraft.customerEmail || '').trim()
      );
      if (!hasTarget || delta === 0 || notes.length < 10) {
        return false;
      }
      if ($ctrl.inspectedCustomer && ($ctrl.inspectedCustomer.availablePoints + delta) < 0) {
        return false;
      }
      return true;
    };

    $ctrl.submitPointAdjustment = function () {
      if (!$ctrl.canSubmitAdjustment() || $ctrl.isSubmittingAdjustment || !DealerApiService || !DealerApiService.loyalty) {
        return $q ? $q.resolve(null) : null;
      }

      var delta = Math.trunc(Number($ctrl.adjustDraft.pointsDelta) || 0);
      var rawIdent = String($ctrl.adjustDraft.customerIdentifier || $ctrl.adjustDraft.customerEmail || '').trim();
      var isGuidIdent = GUID_REGEX.test(rawIdent);
      var resolvedCustomerId = isGuidIdent
        ? rawIdent
        : (GUID_REGEX.test($ctrl.adjustDraft.customerId || '') ? String($ctrl.adjustDraft.customerId).trim() : null);
      var resolvedCustomerEmail = isGuidIdent
        ? null
        : (rawIdent || ($ctrl.adjustDraft.customerEmail ? String($ctrl.adjustDraft.customerEmail).trim() : null));

      $ctrl.isSubmittingAdjustment = true;
      $ctrl.adjustProblem = null;

      return DealerApiService.loyalty.adjustPoints({
        loyaltyAccountId: $ctrl.inspectedCustomer ? $ctrl.inspectedCustomer.accountId : null,
        customerId: resolvedCustomerId,
        customerEmail: resolvedCustomerEmail,
        pointsDelta: delta,
        qualifiesForTier: Boolean($ctrl.adjustDraft.qualifiesForTier),
        notes: String($ctrl.adjustDraft.notes || '').trim(),
        accountRowVersion: $ctrl.inspectedCustomer && $ctrl.inspectedCustomer.rowVersion
          ? $ctrl.inspectedCustomer.rowVersion
          : null
      }).then(function (result) {
        if (isDestroyed) { return result; }
        $ctrl.lastAdjustmentResult = result;
        if (ToastService) {
          var signed = (result.pointsDelta >= 0 ? '+' : '') + result.pointsDelta;
          ToastService.success(
            t('admin.loyaltyTiers.toastAdjRecordedTitle', 'Point Adjustment Recorded'),
            t('admin.loyaltyTiers.toastAdjRecordedBody', '{{signed}} pts appended to PointLedgerEntries.', { signed: signed })
          );
        }
        $ctrl.closeAdjustmentModal();
        return result;
      }).catch(function (rejection) {
        if (!isDestroyed && ProblemDetailsMapper) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.adjustProblem = prob;
          if (prob.status === 409) {
            $ctrl.onAdjustmentCustomerChanged();
          }
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSubmittingAdjustment = false;
        }
      });
    };
  }
})();
