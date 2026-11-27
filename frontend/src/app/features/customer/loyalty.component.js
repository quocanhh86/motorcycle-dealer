(function () {
  'use strict';

  var VI_PATCH = {
    enums: {
      VoucherScope: {
        ServiceOnly: 'Dịch vụ Xưởng',
        ShowroomOnly: 'Mua xe Showroom',
        All: 'Xưởng & Showroom'
      }
    },
    customerLoyalty: {
      eyebrow: 'HỘI VIÊN THÂN THIẾT • 4 HẠNG & SỔ ĐIỂM BẤT BIẾN',
      syncing: 'Đang đồng bộ...',
      refreshWallet: 'Làm mới ví điểm',
      tierPromotionTitle: 'CHÚC MỪNG THĂNG HẠNG THÀNH VIÊN!',
      tierPromotionBody: 'Bạn vừa được nâng lên hạng {{tier}} với hệ số tích điểm {{mult}}x.',
      dismiss: 'Ẩn thông báo',
      mintedTitle: 'Đã phát hành Voucher:',
      mintedReady: 'hiện ở trạng thái Sẵn sàng dùng trong ví của bạn.',
      copyCode: 'Sao chép mã',
      applyToBooking: 'Dùng khi đặt lịch',
      lifetimeSpendLabel: 'Tổng chi tiêu tích lũy',
      roadmapTitle: 'Lộ trình & Quyền lợi 4 hạng hội viên',
      qualPtsSuffix: 'điểm xét hạng+',
      currentTierTag: 'Hạng hiện tại',
      defaultPerkBronze: 'Tích điểm tự động 1.00x trên mọi hóa đơn',
      defaultPerkSilver: 'Nhân hệ số 1.20x & ưu đãi voucher bảo dưỡng',
      defaultPerkGold: 'Nhân hệ số 1.50x & ưu tiên khoang xưởng',
      defaultPerkPlatinum: 'Hệ số tối đa 2.00x & đặc quyền VIP toàn hệ thống',
      emptyCatalogTitle: 'Chưa có mẫu voucher nào đang mở đổi',
      emptyCatalogDesc: 'Các chiến dịch voucher ưu đãi mới từ HQ sẽ tự động xuất hiện tại đây.',
      minTierBadge: 'Từ hạng {{tier}}',
      discountLabel: 'Ưu đãi',
      minOrderLabel: 'Đơn tối thiểu',
      validityDays: '{{days}} ngày hiệu lực',
      quotaLabel: 'Số lượng còn lại',
      quotaUnlimited: 'Không giới hạn',
      redeemNow: 'Đổi ngay',
      unavailable: 'Ngừng áp dụng',
      soldOut: 'Đã hết lượt đổi',
      needMorePts: 'Thiếu {{missing}} điểm',
      filterAllVouchers: 'Tất cả Voucher',
      emptyFilteredVouchersTitle: 'Không có voucher nào thuộc bộ lọc này',
      emptyFilteredVouchersDesc: 'Chuyển bộ lọc trạng thái hoặc đổi thêm voucher ưu đãi bằng điểm khả dụng.',
      scopeLabel: 'Phạm vi',
      spentLabel: 'Đã đổi',
      useOnBooking: 'Dùng đặt lịch xưởng',
      filterLedgerLabel: 'Lọc theo loại giao dịch điểm',
      allLedgerTypes: 'Tất cả giao dịch điểm',
      colDate: 'Thời gian (Giờ VN)',
      colTxType: 'Loại giao dịch',
      colPointsDelta: 'Biến động điểm',
      colBalanceAfter: 'Số dư sau GD',
      colRefNotes: 'Mã chống trùng & Ghi chú',
      pageSummary: 'Trang {{page}} / {{total}}',
      colOrderNo: 'Mã đơn hàng',
      colVehicleVin: 'Xe máy & Số khung VIN',
      colDealer: 'Chi nhánh',
      colFinalPaid: 'Thanh toán thực',
      colPointsAccrued: 'Điểm đã cộng',
      colOrderDate: 'Ngày bàn giao',
      colReceipt: 'Biên lai',
      emptyOrdersTitle: 'Chưa có đơn mua xe Showroom',
      emptyOrdersDesc: 'Mọi đơn mua xe máy tại hệ thống đại lý ủy quyền sẽ tự động lưu trữ cùng biên lai điện tử tại đây.',
      printReceipt: 'In biên lai',
      redeemModalEyebrow: 'XÁC NHẬN ĐỔI ĐIỂM THƯỞNG',
      redeemModalTitle: 'Xác nhận đổi Voucher ưu đãi',
      codePrefixLabel: 'Tiền tố mã',
      pointsCostLabel: 'Điểm cần trừ',
      pointsAfterLabel: 'Điểm khả dụng còn lại',
      lifetimeProtectedNote: 'Điểm tích lũy xét hạng ({{lifetime}} điểm) và hạng {{tier}} của bạn KHÔNG bị giảm.',
      redeeming: 'Đang phát hành Voucher...',
      confirmRedeemBtn: 'Đổi ngay (-{{cost}} điểm)',
      redeemToastTitle: 'Đổi Voucher thành công!',
      redeemToastBody: 'Mã {{code}} đã được cấp (-{{spent}} điểm khả dụng).',
      copiedToastTitle: 'Đã sao chép mã Voucher',
      copiedToastBody: 'Đã sao chép {{code}} vào bộ nhớ tạm.',
      receiptUnavailableTitle: 'Biên lai chưa sẵn sàng',
      receiptUnavailableBody: 'Không xác định được mã đơn hàng để tải biên lai điện tử.'
    }
  };

  var EN_PATCH = {
    enums: {
      VoucherScope: {
        ServiceOnly: 'Workshop Service',
        ShowroomOnly: 'Showroom Purchase',
        All: 'Workshop & Showroom'
      }
    },
    customerLoyalty: {
      eyebrow: 'LOYALTY WALLET • 4 TIERS & IMMUTABLE LEDGER',
      syncing: 'Syncing...',
      refreshWallet: 'Refresh Wallet',
      tierPromotionTitle: 'LIVE TIER PROMOTION UNLOCKED!',
      tierPromotionBody: 'Congratulations — your membership upgraded to {{tier}} ({{mult}}x multiplier).',
      dismiss: 'Dismiss',
      mintedTitle: 'Voucher Minted:',
      mintedReady: 'is now Available in your wallet.',
      copyCode: 'Copy Code',
      applyToBooking: 'Apply to Booking',
      lifetimeSpendLabel: 'Lifetime Spend',
      roadmapTitle: '4-Tier Membership Roadmap & Perks',
      qualPtsSuffix: 'qual. pts+',
      currentTierTag: 'Active Tier',
      defaultPerkBronze: '1.00x automatic accrual on every invoice',
      defaultPerkSilver: '1.20x earn rate & workshop voucher access',
      defaultPerkGold: '1.50x earn rate & priority workshop bays',
      defaultPerkPlatinum: 'Maximum 2.00x earn rate & VIP nationwide perks',
      emptyCatalogTitle: 'No active reward vouchers published',
      emptyCatalogDesc: 'New promotional voucher campaigns from HQ will appear here automatically.',
      minTierBadge: 'Min Tier: {{tier}}',
      discountLabel: 'Discount',
      minOrderLabel: 'Min Spend',
      validityDays: '{{days}} days validity',
      quotaLabel: 'Quota Left',
      quotaUnlimited: 'Unlimited',
      redeemNow: 'Redeem Now',
      unavailable: 'Unavailable',
      soldOut: 'Sold Out',
      needMorePts: 'Need {{missing}} more pts',
      filterAllVouchers: 'All Vouchers',
      emptyFilteredVouchersTitle: 'No vouchers match this filter',
      emptyFilteredVouchersDesc: 'Switch status filters or redeem your available points for workshop discounts.',
      scopeLabel: 'Scope',
      spentLabel: 'Spent',
      useOnBooking: 'Use on Workshop Booking',
      filterLedgerLabel: 'Filter by Transaction Type',
      allLedgerTypes: 'All Ledger Transactions',
      colDate: 'Date (Asia/Ho_Chi_Minh)',
      colTxType: 'Transaction Type',
      colPointsDelta: 'Points Delta',
      colBalanceAfter: 'Balance After',
      colRefNotes: 'Idempotency Ref & Notes',
      pageSummary: 'Page {{page}} of {{total}}',
      colOrderNo: 'Order #',
      colVehicleVin: 'Motorcycle & VIN',
      colDealer: 'Dealership',
      colFinalPaid: 'Final Paid',
      colPointsAccrued: 'Points Accrued',
      colOrderDate: 'Delivery Date',
      colReceipt: 'Receipt',
      emptyOrdersTitle: 'No showroom purchases yet',
      emptyOrdersDesc: 'Completed showroom motorcycle deliveries and official printable receipts appear here automatically.',
      printReceipt: 'Print Receipt',
      redeemModalEyebrow: 'REWARD REDEMPTION CONFIRMATION',
      redeemModalTitle: 'Confirm Reward Voucher Redemption',
      codePrefixLabel: 'Code Prefix',
      pointsCostLabel: 'Points Cost',
      pointsAfterLabel: 'Available Points After',
      lifetimeProtectedNote: 'Your Lifetime Qualifying Points ({{lifetime}} pts) and {{tier}} tier will NOT decrease.',
      redeeming: 'Minting Voucher...',
      confirmRedeemBtn: 'Redeem Now (-{{cost}} pts)',
      redeemToastTitle: 'Voucher Redeemed!',
      redeemToastBody: 'Code {{code}} minted (-{{spent}} Available pts).',
      copiedToastTitle: 'Voucher Code Copied',
      copiedToastBody: '{{code}} copied to clipboard.',
      receiptUnavailableTitle: 'Receipt Unavailable',
      receiptUnavailableBody: 'Could not resolve a sales order identifier for printable receipt lookup.'
    }
  };

  registerCustomerLoyaltyI18n.$inject = ['I18nService'];
  function registerCustomerLoyaltyI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(VI_PATCH, EN_PATCH);
    }
  }

  angular
    .module('app.features.customer')
    .run(registerCustomerLoyaltyI18n)
    .component('customerLoyalty', {
      controllerAs: '$ctrl',
      controller: CustomerLoyaltyController,
      template:
        '<section class="view-enter">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-info mono-num">' +
                '<app-icon name="crown" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'customerLoyalty.eyebrow\' | t }}</span>' +
              '</span>' +
              '<h1 class="page-title" style="margin-top:8px;">{{ \'loyalty.pageTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'loyalty.pageSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadWalletData()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="16" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ $ctrl.isLoading ? (\'customerLoyalty.syncing\' | t) : (\'customerLoyalty.refreshWallet\' | t) }}</span>' +
              '</button>' +
              '<span class="badge" ng-class="\'badge-tier-\' + ($ctrl.summary.tierCode || \'Bronze\').toLowerCase()">' +
                '<app-icon name="crown" weight="fill" size="13"></app-icon>' +
                '<span>{{ $ctrl.summary.tierCode | enumT:\'LoyaltyTierCode\' }} • <span class="mono-num">{{ $ctrl.tierMultiplier | number:2 }}x</span></span>' +
              '</span>' +
            '</div>' +
          '</div>' +

          /* Live SignalR Tier Upgrade Celebration Banner */
          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.celebrationBanner" ' +
               'style="border-color:var(--clr-tier-gold-border);background:var(--clr-tier-gold-bg);color:var(--clr-tier-gold-fg);display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;">' +
              '<app-icon name="trophy" weight="fill" size="22"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerLoyalty.tierPromotionTitle\' | t }}</strong> ' +
                '<span>{{ \'customerLoyalty.tierPromotionBody\' | t:{ tier: ($ctrl.celebrationBanner.newTierCode | enumT:\'LoyaltyTierCode\'), mult: ($ctrl.celebrationBanner.newPointsMultiplier | number:2) } }}</span>' +
              '</div>' +
            '</div>' +
            '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.celebrationBanner = null">{{ \'customerLoyalty.dismiss\' | t }}</button>' +
          '</div>' +

          /* Newly Redeemed Voucher Banner */
          '<div class="alert-banner alert-success fade-slide-up" role="status" ng-if="$ctrl.lastRedeemedVoucher" ' +
               'style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
            '<div style="display:flex;align-items:center;gap:10px;flex-wrap:wrap;">' +
              '<app-icon name="ticket" weight="fill" size="20"></app-icon>' +
              '<div>' +
                '<strong>{{ \'customerLoyalty.mintedTitle\' | t }}</strong> ' +
                '<code class="mono-num" style="font-weight:700;">{{ $ctrl.lastRedeemedVoucher.voucherCode }}</code> ' +
                '({{ $ctrl.lastRedeemedVoucher.title }}) — {{ \'customerLoyalty.mintedReady\' | t }}' +
              '</div>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.copyVoucherCode($ctrl.lastRedeemedVoucher.voucherCode)">' +
                '<app-icon name="copy" weight="regular" size="14"></app-icon>' +
                '<span>{{ \'customerLoyalty.copyCode\' | t }}</span>' +
              '</button>' +
              '<a class="btn btn-primary btn-sm" ng-if="$ctrl.lastRedeemedVoucher.applicableScope !== \'ShowroomOnly\'" ' +
                 'ui-sref="app.customer.bookingNew({ voucherId: $ctrl.lastRedeemedVoucher.id })">' +
                '<span>{{ \'customerLoyalty.applyToBooking\' | t }}</span>' +
                '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.lastRedeemedVoucher = null">' +
                '<app-icon name="x" weight="regular" size="14"></app-icon>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.walletProblem ? ($ctrl.walletProblem.title + \': \' + $ctrl.walletProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.walletProblem && $ctrl.walletProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadWalletData()">' +
          '</error-retry-banner>' +

          /* Skeleton Loader for Initial Passbook Load */
          '<div ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" style="margin-bottom:24px;">' +
            '<skeleton-loader variant="passbook" count="1"></skeleton-loader>' +
          '</div>' +

          /* FLAGSHIP METALLIC 4-TIER LOYALTY PASSBOOK */
          '<div class="bezel-card fade-slide-up" ng-if="!$ctrl.isLoading || $ctrl.hasLoadedOnce" style="margin-bottom:24px;">' +
            '<div class="passbook-card" ng-class="[\'tier-\' + ($ctrl.summary.tierCode || \'bronze\').toLowerCase(), $ctrl.celebrationBanner ? \'is-sheen-active\' : \'\']">' +
              '<div class="passbook-sheen" aria-hidden="true"></div>' +
              '<div style="position:relative;z-index:2;">' +
                '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:16px;flex-wrap:wrap;">' +
                  '<div>' +
                    '<div style="display:inline-flex;align-items:center;gap:8px;font-size:11px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;opacity:0.86;">' +
                      '<app-icon name="crown" weight="duotone" size="18"></app-icon>' +
                      '<span>{{ \'loyalty.passbookEyebrow\' | t }}</span>' +
                    '</div>' +
                    '<div style="display:flex;align-items:baseline;gap:12px;flex-wrap:wrap;margin-top:8px;">' +
                      '<h2 style="margin:0;font-family:var(--font-display);font-size:clamp(1.6rem,2.8vw,2.25rem);font-weight:700;letter-spacing:-0.02em;">' +
                        '{{ $ctrl.summary.tierCode | enumT:\'LoyaltyTierCode\' }}' +
                      '</h2>' +
                      '<span class="mono-num" style="padding:4px 12px;border-radius:var(--radius-full);background:rgba(255,255,255,0.16);border:1px solid rgba(255,255,255,0.28);font-weight:700;font-size:13px;">' +
                        '{{ \'loyalty.pointsMultiplier\' | t }}: {{ $ctrl.tierMultiplier | number:2 }}x' +
                      '</span>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;gap:20px;flex-wrap:wrap;">' +
                    '<div style="padding:12px 16px;border-radius:var(--radius-lg);background:rgba(0,0,0,0.24);border:1px solid rgba(255,255,255,0.18);min-width:170px;">' +
                      '<div style="font-size:11px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.82;">{{ \'loyalty.availablePoints\' | t }}</div>' +
                      '<div class="mono-num" style="font-size:24px;font-weight:700;margin-top:4px;" kpi-counter="$ctrl.availablePoints" kpi-format="points"></div>' +
                    '</div>' +
                    '<div style="padding:12px 16px;border-radius:var(--radius-lg);background:rgba(0,0,0,0.24);border:1px solid rgba(255,255,255,0.18);min-width:170px;">' +
                      '<div style="font-size:11px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.82;">{{ \'loyalty.lifetimePoints\' | t }}</div>' +
                      '<div class="mono-num" style="font-size:24px;font-weight:700;margin-top:4px;" kpi-counter="$ctrl.lifetimePoints" kpi-format="points"></div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                /* GPU-Accelerated ScaleX Progress Bar to Next Tier */
                '<div style="margin-top:20px;padding-top:16px;border-top:1px solid rgba(255,255,255,0.18);">' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;font-size:13px;margin-bottom:8px;">' +
                    '<span ng-if="!$ctrl.tierProgress.isTopTier">' +
                      '{{ \'loyalty.nextTierProgress\' | t:{ remaining: ($ctrl.tierProgress.pointsToNextTier | number), nextTier: ($ctrl.tierProgress.nextTierCode | enumT:\'LoyaltyTierCode\') } }}' +
                    '</span>' +
                    '<span ng-if="$ctrl.tierProgress.isTopTier" style="display:inline-flex;align-items:center;gap:6px;font-weight:600;">' +
                      '<app-icon name="medal" weight="fill" size="16"></app-icon>' +
                      '<span>{{ \'loyalty.maxTierReached\' | t }}</span>' +
                    '</span>' +
                    '<span class="mono-num" style="font-weight:700;">{{ ($ctrl.tierProgress.progressPercent || 0) }}%</span>' +
                  '</div>' +
                  '<div style="width:100%;height:10px;border-radius:var(--radius-full);background:rgba(0,0,0,0.32);overflow:hidden;border:1px solid rgba(255,255,255,0.16);">' +
                    '<div style="width:100%;height:100%;border-radius:var(--radius-full);background:rgba(255,255,255,0.92);transform-origin:left center;transition:transform var(--dur-emphasis) var(--ease-expo);" ' +
                         'ng-style="{transform: \'scaleX(\' + (($ctrl.tierProgress.progressPercent || 0) / 100) + \')\'}"></div>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;margin-top:10px;font-size:12px;opacity:0.86;">' +
                    '<span>{{ \'loyalty.accrualFormula\' | t }}</span>' +
                    '<span ng-if="$ctrl.lifetimeSpentVnd > 0">{{ \'customerLoyalty.lifetimeSpendLabel\' | t }}: <strong class="mono-num">{{ $ctrl.lifetimeSpentVnd | vndCurrency }}</strong></span>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* 4-Tier Perks Roadmap Grid */
          '<div class="bezel-card fade-slide-up" style="margin-bottom:24px;">' +
            '<div class="bezel-card-inner" style="padding:var(--space-4) var(--space-5);">' +
              '<div style="display:flex;align-items:center;gap:8px;margin-bottom:12px;">' +
                '<app-icon name="trophy" weight="duotone" size="18"></app-icon>' +
                '<span class="kpi-label">{{ \'customerLoyalty.roadmapTitle\' | t }}</span>' +
              '</div>' +
              '<div class="grid-12" style="gap:12px;">' +
                '<div class="col-3 stagger-item" ng-repeat="tr in $ctrl.tiers track by tr.code" ng-attr-style="--i:{{ $index }};">' +
                  '<div class="surface-sunken" style="padding:12px 14px;height:100%;display:flex;flex-direction:column;justify-content:space-between;gap:8px;" ' +
                       'ng-style="$ctrl.tierCode === tr.code ? {\'border-color\': \'var(--clr-brand-primary)\', \'background\': \'var(--clr-brand-primary-soft)\'} : {}">' +
                    '<div>' +
                      '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;">' +
                        '<span class="badge" ng-class="\'badge-tier-\' + tr.code.toLowerCase()">' +
                          '<app-icon name="crown" weight="fill" size="12"></app-icon>' +
                          '<span>{{ tr.code | enumT:\'LoyaltyTierCode\' }}</span>' +
                        '</span>' +
                        '<strong class="mono-num text-primary">{{ (tr.pointsMultiplier || tr.multiplier || 1) | number:2 }}x</strong>' +
                      '</div>' +
                      '<div class="mono-num text-secondary" style="font-size:12px;margin-top:8px;">' +
                        '{{ tr.minQualifyingPoints | number }} {{ \'customerLoyalty.qualPtsSuffix\' | t }}' +
                      '</div>' +
                      '<div class="text-muted" style="font-size:12px;margin-top:4px;">' +
                        '{{ $ctrl.tierPerkLabel(tr) }}' +
                      '</div>' +
                    '</div>' +
                    '<div ng-if="$ctrl.tierCode === tr.code" style="font-size:11px;font-weight:700;color:var(--clr-brand-on-soft);display:inline-flex;align-items:center;gap:4px;">' +
                      '<app-icon name="check-circle" weight="fill" size="13"></app-icon>' +
                      '<span>{{ \'customerLoyalty.currentTierTag\' | t }}</span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Workspace Tabs */
          '<div class="tab-strip">' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'CATALOG\'}" ' +
                    'ng-click="$ctrl.setTab(\'CATALOG\')">' +
              '<app-icon name="gift" weight="regular" size="15"></app-icon>' +
              '<span>{{ \'loyalty.tabVoucherStore\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.catalog.length }})</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'MY_VOUCHERS\'}" ' +
                    'ng-click="$ctrl.setTab(\'MY_VOUCHERS\')">' +
              '<app-icon name="wallet" weight="regular" size="15"></app-icon>' +
              '<span>{{ \'loyalty.tabMyVouchers\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.myVouchers.length }})</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'LEDGER\'}" ' +
                    'ng-click="$ctrl.setTab(\'LEDGER\')">' +
              '<app-icon name="receipt" weight="regular" size="15"></app-icon>' +
              '<span>{{ \'loyalty.tabPointLedger\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.ledgerPagination.totalCount || $ctrl.ledgerEntries.length }})</span>' +
            '</button>' +
            '<button type="button" class="tab-btn" ' +
                    'ng-class="{\'is-active\': $ctrl.activeTab === \'ORDERS\'}" ' +
                    'ng-click="$ctrl.setTab(\'ORDERS\')">' +
              '<app-icon name="shopping-bag" weight="regular" size="15"></app-icon>' +
              '<span>{{ \'loyalty.tabSalesOrders\' | t }}</span>' +
              '<span class="mono-num">({{ $ctrl.salesOrders.length }})</span>' +
            '</button>' +
          '</div>' +

          /* TAB 1: DOUBLE-BEZEL REWARD VOUCHER STORE */
          '<div ng-if="$ctrl.activeTab === \'CATALOG\'">' +
            '<skeleton-loader ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" variant="cards" count="2"></skeleton-loader>' +

            '<empty-state ng-if="(!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.catalog.length === 0" ' +
                         'icon="crown" ' +
                         'title-key="customerLoyalty.emptyCatalogTitle" ' +
                         'desc-key="customerLoyalty.emptyCatalogDesc">' +
            '</empty-state>' +

            '<div class="grid-12" ng-if="$ctrl.catalog.length > 0">' +
              '<article class="col-6 bezel-card stagger-item" ng-repeat="voucher in $ctrl.catalog track by voucher.id" ng-attr-style="--i:{{ $index }};">' +
                '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:12px;">' +
                  '<div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<div style="display:flex;gap:6px;align-items:center;flex-wrap:wrap;">' +
                        '<span class="badge badge-neutral mono-num">' +
                          '<app-icon name="ticket" weight="duotone" size="13"></app-icon>' +
                          '<span>{{ voucher.codePrefix }}</span>' +
                        '</span>' +
                        '<span class="badge badge-info">{{ voucher.applicableScope | enumT:\'VoucherScope\' }}</span>' +
                      '</div>' +
                      '<span class="badge" ng-class="\'badge-tier-\' + (voucher.minTierCode || \'Bronze\').toLowerCase()">' +
                        '{{ \'customerLoyalty.minTierBadge\' | t:{ tier: (voucher.minTierCode | enumT:\'LoyaltyTierCode\') } }}' +
                      '</span>' +
                    '</div>' +

                    '<h3 class="surface-card-title" style="margin-top:12px;">{{ voucher.title }}</h3>' +
                    '<p class="text-muted" style="font-size:13px;margin:6px 0;">{{ voucher.description }}</p>' +

                    '<div class="surface-sunken text-secondary" style="padding:10px 12px;display:flex;gap:14px;flex-wrap:wrap;font-size:12px;margin-top:10px;">' +
                      '<span>{{ \'customerLoyalty.discountLabel\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.formatDiscountSummary(voucher) }}</strong></span>' +
                      '<span>{{ \'customerLoyalty.minOrderLabel\' | t }}: <strong class="mono-num text-primary">{{ voucher.minOrderAmount | vndCurrency }}</strong></span>' +
                      '<span>{{ \'customerLoyalty.validityDays\' | t:{ days: voucher.validDays } }}</span>' +
                      '<span>{{ \'customerLoyalty.quotaLabel\' | t }}: <strong class="mono-num text-primary">{{ voucher.quotaTotal !== null ? (voucher.remainingQuota + \' / \' + voucher.quotaTotal) : (\'customerLoyalty.quotaUnlimited\' | t) }}</strong></span>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;padding-top:12px;border-top:1px solid var(--clr-border-subtle);flex-wrap:wrap;">' +
                    '<strong class="mono-num text-primary" style="font-size:17px;">{{ voucher.pointsCost | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                    '<button type="button" class="btn btn-sm" ' +
                            'ng-class="$ctrl.canRedeemCatalogItem(voucher) ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'has-permission="Permissions.Loyalty.RedeemVoucher" ' +
                            'ng-disabled="!$ctrl.canRedeemCatalogItem(voucher)" ' +
                            'ng-click="$ctrl.openRedeemModal(voucher)">' +
                      '<app-icon name="{{ $ctrl.canRedeemCatalogItem(voucher) ? \'gift\' : \'lock-simple\' }}" weight="regular" size="14"></app-icon>' +
                      '<span>{{ $ctrl.redeemButtonLabel(voucher) }}</span>' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</article>' +
            '</div>' +
          '</div>' +

          /* TAB 2: DOUBLE-BEZEL MY VOUCHERS WALLET */
          '<div ng-if="$ctrl.activeTab === \'MY_VOUCHERS\'">' +
            '<div class="surface-card" style="margin-bottom:16px;display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;">' +
              '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                '<button type="button" class="btn btn-sm" ' +
                        'ng-repeat="vf in $ctrl.voucherStatusFilters track by vf.value" ' +
                        'ng-class="$ctrl.voucherStatusFilter === vf.value ? \'btn-primary\' : \'btn-secondary\'" ' +
                        'ng-click="$ctrl.setVoucherStatusFilter(vf.value)">' +
                  '{{ vf.value ? (vf.value | enumT:\'CustomerVoucherStatus\') : (\'customerLoyalty.filterAllVouchers\' | t) }}' +
                '</button>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" variant="cards" count="2"></skeleton-loader>' +

            '<empty-state ng-if="(!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.filteredMyVouchers.length === 0" ' +
                         'icon="ticket" ' +
                         'title-key="customerLoyalty.emptyFilteredVouchersTitle" ' +
                         'desc-key="customerLoyalty.emptyFilteredVouchersDesc">' +
            '</empty-state>' +

            '<div class="grid-12" ng-if="$ctrl.filteredMyVouchers.length > 0">' +
              '<article class="col-6 bezel-card stagger-item" ng-repeat="mv in $ctrl.filteredMyVouchers track by mv.id" ng-attr-style="--i:{{ $index }};">' +
                '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:10px;">' +
                  '<div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<code class="mono-num text-primary surface-sunken" style="font-size:15px;font-weight:700;padding:4px 10px;">{{ mv.voucherCode }}</code>' +
                      '<span class="badge" ng-class="$ctrl.voucherStatusBadgeClass(mv.effectiveStatus)">' +
                        '{{ mv.effectiveStatus | enumT:\'CustomerVoucherStatus\' }}' +
                      '</span>' +
                    '</div>' +
                    '<h3 class="surface-card-title" style="font-size:15px;margin-top:10px;">{{ mv.title }}</h3>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:6px;">' +
                      '{{ \'customerLoyalty.scopeLabel\' | t }}: <strong class="text-primary">{{ mv.applicableScope | enumT:\'VoucherScope\' }}</strong> • ' +
                      '<strong class="mono-num text-primary">{{ $ctrl.formatDiscountSummary(mv) }}</strong> • ' +
                      '{{ \'customerLoyalty.minOrderLabel\' | t }}: <span class="mono-num">{{ mv.minOrderAmount | vndCurrency }}</span>' +
                    '</div>' +
                    '<div class="mono-num text-muted" style="font-size:11px;margin-top:6px;">' +
                      '{{ \'customerLoyalty.spentLabel\' | t }}: {{ mv.pointsSpent | number }} {{ \'common.ptsShort\' | t }} • {{ \'loyalty.expiresAt\' | t }}: {{ mv.expiresAtUtc | utcToLocal }}' +
                    '</div>' +
                  '</div>' +
                  '<div style="display:flex;gap:8px;padding-top:10px;border-top:1px solid var(--clr-border-subtle);flex-wrap:wrap;">' +
                    '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.copyVoucherCode(mv.voucherCode)">' +
                      '<app-icon name="copy" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'customerLoyalty.copyCode\' | t }}</span>' +
                    '</button>' +
                    '<a class="btn btn-primary btn-sm" ' +
                       'ng-if="mv.effectiveStatus === \'Available\' && mv.applicableScope !== \'ShowroomOnly\'" ' +
                       'ui-sref="app.customer.bookingNew({ voucherId: mv.id })">' +
                      '<span>{{ \'customerLoyalty.useOnBooking\' | t }}</span>' +
                      '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                    '</a>' +
                  '</div>' +
                '</div>' +
              '</article>' +
            '</div>' +
          '</div>' +

          /* TAB 3: IMMUTABLE POINT LEDGER STATEMENT (tabular-nums) */
          '<div ng-if="$ctrl.activeTab === \'LEDGER\'">' +
            '<div class="surface-card" style="margin-bottom:16px;display:flex;justify-content:space-between;align-items:end;gap:12px;flex-wrap:wrap;">' +
              '<div class="form-group" style="margin-bottom:0;min-width:260px;">' +
                '<label class="form-label" for="ledgerTxTypeSelect">{{ \'customerLoyalty.filterLedgerLabel\' | t }}</label>' +
                '<select id="ledgerTxTypeSelect" class="form-control" ' +
                        'ng-model="$ctrl.ledgerFilter.transactionType" ' +
                        'ng-options="opt.value as (opt.value ? (opt.value | enumT:\'LedgerTransactionType\') : (\'customerLoyalty.allLedgerTypes\' | t)) for opt in $ctrl.transactionTypeOptions" ' +
                        'ng-change="$ctrl.onLedgerFilterChanged()"></select>' +
              '</div>' +
              '<div class="text-muted" style="font-size:12px;display:flex;align-items:center;gap:6px;">' +
                '<app-icon name="shield-check" weight="duotone" size="15"></app-icon>' +
                '<span>{{ \'loyalty.ledgerImmutableNote\' | t }}</span>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoadingLedger || ($ctrl.isLoading && !$ctrl.hasLoadedOnce)" variant="table" count="5"></skeleton-loader>' +

            '<empty-state ng-if="!$ctrl.isLoadingLedger && (!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.ledgerEntries.length === 0" ' +
                         'icon="crown" ' +
                         'title-key="loyalty.emptyLedgerTitle" ' +
                         'desc-key="loyalty.emptyLedgerDesc">' +
            '</empty-state>' +

            '<div class="bezel-card" ng-if="!$ctrl.isLoadingLedger && $ctrl.ledgerEntries.length > 0">' +
              '<div class="bezel-card-inner table-wrap" style="padding:var(--space-4);">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th>{{ \'customerLoyalty.colDate\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colTxType\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colPointsDelta\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colBalanceAfter\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colRefNotes\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="entry in $ctrl.ledgerEntries track by entry.id">' +
                      '<td class="mono-num text-secondary">{{ entry.createdAtUtc | utcToLocal }}</td>' +
                      '<td><span class="badge badge-neutral">{{ entry.transactionType | enumT:\'LedgerTransactionType\' }}</span></td>' +
                      '<td class="mono-num" style="font-weight:700;" ' +
                          'ng-style="{\'color\': entry.pointsDelta >= 0 ? \'var(--clr-success-fg)\' : \'var(--clr-danger-fg)\'}">' +
                        '{{ entry.pointsDelta >= 0 ? \'+\' : \'\' }}{{ entry.pointsDelta | number }} {{ \'common.ptsShort\' | t }}' +
                      '</td>' +
                      '<td class="mono-num text-primary">{{ entry.balanceAfter | number }} {{ \'common.ptsShort\' | t }}</td>' +
                      '<td>' +
                        '<div class="text-primary" style="font-size:13px;">{{ entry.notes || entry.referenceType }}</div>' +
                        '<div class="mono-num text-muted" style="font-size:11px;">{{ entry.idempotencyKey }}</div>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;margin-top:14px;padding-top:12px;border-top:1px solid var(--clr-border-subtle);" ' +
                     'ng-if="$ctrl.ledgerPagination.totalPages > 1">' +
                  '<span class="mono-num text-muted" style="font-size:12px;">' +
                    '{{ \'customerLoyalty.pageSummary\' | t:{ page: $ctrl.ledgerPagination.pageNumber, total: $ctrl.ledgerPagination.totalPages } }}' +
                  '</span>' +
                  '<div style="display:flex;gap:8px;">' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-disabled="!$ctrl.ledgerPagination.hasPreviousPage || $ctrl.isLoadingLedger" ' +
                            'ng-click="$ctrl.changeLedgerPage($ctrl.ledgerPagination.pageNumber - 1)">' +
                      '<app-icon name="caret-left" weight="regular" size="14"></app-icon>' +
                      '<span>{{ \'common.previous\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-disabled="!$ctrl.ledgerPagination.hasNextPage || $ctrl.isLoadingLedger" ' +
                            'ng-click="$ctrl.changeLedgerPage($ctrl.ledgerPagination.pageNumber + 1)">' +
                      '<span>{{ \'common.next\' | t }}</span>' +
                      '<app-icon name="caret-right" weight="regular" size="14"></app-icon>' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* TAB 4: SHOWROOM PURCHASES */
          '<div ng-if="$ctrl.activeTab === \'ORDERS\'">' +
            '<skeleton-loader ng-if="$ctrl.isLoading && !$ctrl.hasLoadedOnce" variant="table" count="3"></skeleton-loader>' +

            '<empty-state ng-if="(!$ctrl.isLoading || $ctrl.hasLoadedOnce) && $ctrl.salesOrders.length === 0" ' +
                         'icon="shopping-bag" ' +
                         'title-key="customerLoyalty.emptyOrdersTitle" ' +
                         'desc-key="customerLoyalty.emptyOrdersDesc">' +
            '</empty-state>' +

            '<div class="bezel-card" ng-if="$ctrl.salesOrders.length > 0">' +
              '<div class="bezel-card-inner table-wrap" style="padding:var(--space-4);">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th>{{ \'customerLoyalty.colOrderNo\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colVehicleVin\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colDealer\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colFinalPaid\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colPointsAccrued\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colOrderDate\' | t }}</th>' +
                      '<th>{{ \'customerLoyalty.colReceipt\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="ord in $ctrl.salesOrders track by ord.id">' +
                      '<td class="mono-num text-primary" style="font-weight:700;">{{ ord.orderNumber }}</td>' +
                      '<td>' +
                        '<div class="text-primary" style="font-weight:600;">{{ ord.motorcycleModelName }}</div>' +
                        '<div class="mono-num text-muted" style="font-size:12px;">{{ ord.vin }}</div>' +
                      '</td>' +
                      '<td class="text-secondary">{{ ord.dealerName || ord.dealerCode }}</td>' +
                      '<td class="mono-num text-primary">{{ ord.finalAmount | vndCurrency }}</td>' +
                      '<td><span class="badge badge-success mono-num">+{{ ord.pointsEarned | number }} {{ \'common.ptsShort\' | t }}</span></td>' +
                      '<td class="mono-num text-secondary">{{ ord.createdAtUtc | utcToLocal }}</td>' +
                      '<td>' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'has-permission="Permissions.SalesOrders.ViewOwn" ' +
                                'ng-click="$ctrl.openOrderReceipt(ord)">' +
                          '<app-icon name="printer" weight="regular" size="14"></app-icon>' +
                          '<span>{{ \'customerLoyalty.printReceipt\' | t }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* REDEEM CONFIRMATION MODAL */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="redeemModalTitle" ng-if="$ctrl.redeemingCatalogItem">' +
            '<div class="modal-dialog">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ \'customerLoyalty.redeemModalEyebrow\' | t }}</span>' +
                  '<h2 id="redeemModalTitle" class="surface-card-title" style="margin-top:4px;">{{ \'customerLoyalty.redeemModalTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeRedeemModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="modal-body">' +
                '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.redeemProblem">' +
                  '<strong>{{ $ctrl.redeemProblem.title }} ({{ $ctrl.redeemProblem.errorCode }}):</strong> {{ $ctrl.redeemProblem.detail }}' +
                '</div>' +

                '<div class="surface-sunken" style="padding:14px;font-size:13px;">' +
                  '<div class="text-primary" style="font-weight:700;font-size:15px;">{{ $ctrl.redeemingCatalogItem.title }}</div>' +
                  '<div class="text-secondary" style="margin-top:6px;">' +
                    '{{ \'customerLoyalty.codePrefixLabel\' | t }}: <code class="mono-num text-primary">{{ $ctrl.redeemingCatalogItem.codePrefix }}</code> • ' +
                    '{{ \'customerLoyalty.scopeLabel\' | t }}: <strong class="text-primary">{{ $ctrl.redeemingCatalogItem.applicableScope | enumT:\'VoucherScope\' }}</strong>' +
                  '</div>' +
                  '<div class="text-secondary" style="margin-top:4px;">' +
                    '{{ \'customerLoyalty.minOrderLabel\' | t }}: <span class="mono-num text-primary">{{ $ctrl.redeemingCatalogItem.minOrderAmount | vndCurrency }}</span> • ' +
                    '<strong>{{ \'customerLoyalty.validityDays\' | t:{ days: $ctrl.redeemingCatalogItem.validDays } }}</strong>' +
                  '</div>' +
                  '<hr style="border:0;border-top:1px solid var(--clr-border-subtle);margin:12px 0;" />' +
                  '<div style="display:flex;justify-content:space-between;">' +
                    '<span class="text-secondary">{{ \'customerLoyalty.pointsCostLabel\' | t }}:</span>' +
                    '<strong class="mono-num" style="color:var(--clr-danger-fg);">-{{ $ctrl.redeemingCatalogItem.pointsCost | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                  '</div>' +
                  '<div style="display:flex;justify-content:space-between;margin-top:6px;">' +
                    '<span class="text-secondary">{{ \'customerLoyalty.pointsAfterLabel\' | t }}:</span>' +
                    '<strong class="mono-num text-primary">{{ ($ctrl.availablePoints - $ctrl.redeemingCatalogItem.pointsCost) | number }} {{ \'common.ptsShort\' | t }}</strong>' +
                  '</div>' +
                  '<div style="margin-top:10px;font-size:12px;color:var(--clr-success-fg);font-weight:600;display:flex;align-items:center;gap:6px;">' +
                    '<app-icon name="check-circle" weight="fill" size="15"></app-icon>' +
                    '<span>{{ \'customerLoyalty.lifetimeProtectedNote\' | t:{ lifetime: ($ctrl.lifetimePoints | number), tier: ($ctrl.tierCode | enumT:\'LoyaltyTierCode\') } }}</span>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeRedeemModal()" ng-disabled="$ctrl.isRedeeming">{{ \'common.cancel\' | t }}</button>' +
                '<button type="button" id="confirmVoucherRedeemBtn" class="btn-pill-cta" ' +
                        'has-permission="Permissions.Loyalty.RedeemVoucher" ' +
                        'ng-disabled="$ctrl.isRedeeming" ' +
                        'ng-click="$ctrl.confirmRedeemVoucher()">' +
                  '<span>{{ $ctrl.isRedeeming ? (\'customerLoyalty.redeeming\' | t) : (\'customerLoyalty.confirmRedeemBtn\' | t:{ cost: ($ctrl.redeemingCatalogItem.pointsCost | number) }) }}</span>' +
                  '<span class="btn-pill-well" aria-hidden="true">' +
                    '<app-icon name="gift" weight="regular" size="15"></app-icon>' +
                  '</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<printable-receipt-modal receipt="$ctrl.activeReceipt" receipt-kind="$ctrl.activeReceiptKind" on-close="$ctrl.closeReceipt()"></printable-receipt-modal>' +
        '</section>'
    });

  CustomerLoyaltyController.$inject = [
    '$q',
    '$rootScope',
    '$window',
    'AuthService',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'ToastService',
    'TokenStorageService',
    'I18nService'
  ];
  function CustomerLoyaltyController(
    $q,
    $rootScope,
    $window,
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

    function getActiveLocale() {
      return (I18nService && angular.isFunction(I18nService.getLocale))
        ? I18nService.getLocale()
        : 'en-US';
    }

    function syncSummaryObject() {
      $ctrl.summary = {
        tierCode: $ctrl.tierCode || 'Bronze',
        tierMultiplier: $ctrl.tierMultiplier || 1.0,
        availablePoints: $ctrl.availablePoints || 0,
        lifetimePoints: $ctrl.lifetimePoints || 0,
        lifetimeSpentVnd: $ctrl.lifetimeSpentVnd || 0
      };
    }

    function isLiveSession() {
      return Boolean(
        AuthService &&
        AuthService.isAuthenticated() &&
        (!TokenStorageService || !angular.isFunction(TokenStorageService.getAccessToken) || TokenStorageService.getAccessToken())
      );
    }

    function syncLoyaltyFromSession() {
      var user = AuthService && angular.isFunction(AuthService.getCurrentUser)
        ? AuthService.getCurrentUser()
        : null;
      var isCustomer = Boolean(user && user.role === 'Customer');
      var availablePts = isCustomer && angular.isNumber(user.availablePoints)
        ? Math.max(0, Math.floor(user.availablePoints))
        : 0;
      var lifetimePts = isCustomer && angular.isNumber(user.lifetimeQualifyingPoints)
        ? Math.max(0, Math.floor(user.lifetimeQualifyingPoints))
        : 0;
      var progress = LoyaltyCalculator.resolveTierProgress(lifetimePts, $ctrl.tiers);
      var resolvedTierCode = (isCustomer && user.tierCode) || progress.currentTierCode || 'Bronze';
      var rawMult = isCustomer
        ? (user.tierMultiplier !== undefined && user.tierMultiplier !== null ? user.tierMultiplier : user.pointsMultiplier)
        : null;
      var resolvedMult = Number(rawMult) > 0 ? Number(rawMult) : (progress.currentMultiplier || 1.0);

      $ctrl.availablePoints = availablePts;
      $ctrl.lifetimePoints = lifetimePts;
      $ctrl.tierCode = resolvedTierCode;
      $ctrl.tierMultiplier = resolvedMult;
      $ctrl.tierProgress = progress;
      syncSummaryObject();
    }

    function applyAccountDto(account) {
      if (!account) {
        return;
      }
      $ctrl.account = account;
      $ctrl.accountRowVersion = account.rowVersion || '';
      $ctrl.availablePoints = account.availablePoints;
      $ctrl.lifetimePoints = account.lifetimeQualifyingPoints;
      $ctrl.lifetimeSpentVnd = account.lifetimeSpentVnd || 0;
      $ctrl.tierCode = account.currentTierCode || 'Bronze';
      $ctrl.tierMultiplier = account.pointsMultiplier || 1.0;
      $ctrl.tierProgress = LoyaltyCalculator.resolveTierProgress(account.lifetimeQualifyingPoints, $ctrl.tiers);
      syncSummaryObject();
    }

    $ctrl.$onInit = function () {
      $ctrl.activeTab = 'CATALOG';
      $ctrl.isLoading = false;
      $ctrl.hasLoadedOnce = false;
      $ctrl.isLoadingLedger = false;
      $ctrl.isRedeeming = false;
      $ctrl.walletProblem = null;
      $ctrl.redeemProblem = null;
      $ctrl.celebrationBanner = null;
      $ctrl.lastRedeemedVoucher = null;
      $ctrl.redeemingCatalogItem = null;
      $ctrl.accountRowVersion = '';
      $ctrl.lifetimeSpentVnd = 0;

      $ctrl.tiers = LoyaltyCalculator.cloneTiers().map(function (tierItem) {
        return angular.extend({}, tierItem, { pointsMultiplier: tierItem.multiplier });
      });

      $ctrl.voucherStatusFilters = [
        { value: '', label: 'All Vouchers' },
        { value: 'Available', label: 'Available' },
        { value: 'ReservedOnBooking', label: 'ReservedOnBooking' },
        { value: 'Used', label: 'Used' },
        { value: 'Expired', label: 'Expired' }
      ];
      $ctrl.voucherStatusFilter = '';

      var canonicalLedgerTypes = (DealerApiService && angular.isFunction(DealerApiService.getLedgerTransactionTypes))
        ? DealerApiService.getLedgerTransactionTypes()
        : [
          { value: 'SalesOrderEarn', label: 'SalesOrderEarn' },
          { value: 'ServiceInvoiceEarn', label: 'ServiceInvoiceEarn' },
          { value: 'ServiceReviewBonus', label: 'ServiceReviewBonus' },
          { value: 'VoucherRedeem', label: 'VoucherRedeem' },
          { value: 'VoucherExpireRefund', label: 'VoucherExpireRefund' },
          { value: 'AdminAdjustment', label: 'AdminAdjustment' },
          { value: 'WelcomeBonus', label: 'WelcomeBonus' }
        ];
      $ctrl.transactionTypeOptions = [{ value: '', label: 'All Ledger Transactions' }].concat(canonicalLedgerTypes);
      $ctrl.ledgerFilter = {
        transactionType: '',
        pageNumber: 1,
        pageSize: 15
      };

      $ctrl.formatDiscountSummary = function (voucherOrCatalog) {
        if (!voucherOrCatalog) { return ''; }
        var locale = getActiveLocale();
        if (LoyaltyCalculator) {
          if (angular.isFunction(LoyaltyCalculator.formatDiscountSummaryLocalized)) {
            return LoyaltyCalculator.formatDiscountSummaryLocalized(voucherOrCatalog, locale) || voucherOrCatalog.discountSummary || '';
          }
          if (angular.isFunction(LoyaltyCalculator.formatDiscountSummary)) {
            return LoyaltyCalculator.formatDiscountSummary(voucherOrCatalog, locale) || voucherOrCatalog.discountSummary || '';
          }
        }
        return voucherOrCatalog.discountSummary || '';
      };
      $ctrl.loadProblem = null;
      $ctrl.catalog = [];
      $ctrl.myVouchers = [];
      $ctrl.filteredMyVouchers = [];
      $ctrl.ledgerEntries = [];
      $ctrl.ledgerPagination = { pageNumber: 1, pageSize: 15, totalCount: 0, totalPages: 0 };
      $ctrl.salesOrders = [];
      $ctrl.activeReceipt = null;
      $ctrl.activeReceiptKind = 'SalesOrder';

      syncLoyaltyFromSession();

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        cleanupFns.push($rootScope.$on('auth:changed', function () {
          if (!isDestroyed) {
            syncLoyaltyFromSession();
          }
        }));

        cleanupFns.push($rootScope.$on('signalr:reconnected', function () {
          if (!isDestroyed && isLiveSession()) {
            $ctrl.loadWalletData();
          }
        }));
      }

      if (SignalRClientService && angular.isFunction(SignalRClientService.on)) {
        cleanupFns.push(SignalRClientService.on('LoyaltyPointsUpdated', function (payload) {
          if (isDestroyed) { return; }
          if (payload && payload.availablePointsAfter !== undefined) {
            $ctrl.availablePoints = Math.max(0, Math.floor(Number(payload.availablePointsAfter) || 0));
          }
          if (payload && payload.lifetimeQualifyingPointsAfter !== undefined) {
            $ctrl.lifetimePoints = Math.max(0, Math.floor(Number(payload.lifetimeQualifyingPointsAfter) || 0));
            $ctrl.tierProgress = LoyaltyCalculator.resolveTierProgress($ctrl.lifetimePoints, $ctrl.tiers);
          }
          syncSummaryObject();
          if (isLiveSession()) {
            $ctrl.loadWalletData();
          }
        }));

        cleanupFns.push(SignalRClientService.on('TierUpgraded', function (payload) {
          if (isDestroyed || !payload) { return; }
          var nextCode = payload.newTierCode || $ctrl.tierCode;
          var nextMult = Number(payload.newPointsMultiplier || payload.newMultiplier) || $ctrl.tierMultiplier;
          $ctrl.tierCode = nextCode;
          $ctrl.tierMultiplier = nextMult;
          if (payload.lifetimeQualifyingPoints !== undefined) {
            $ctrl.lifetimePoints = Math.max(0, Math.floor(Number(payload.lifetimeQualifyingPoints) || 0));
            $ctrl.tierProgress = LoyaltyCalculator.resolveTierProgress($ctrl.lifetimePoints, $ctrl.tiers);
          }
          syncSummaryObject();
          $ctrl.celebrationBanner = {
            previousTierCode: payload.previousTierCode || 'Previous',
            newTierCode: nextCode,
            newPointsMultiplier: nextMult
          };
          if (isLiveSession()) {
            $ctrl.loadWalletData();
          }
        }));
      }

      if (isLiveSession()) {
        $ctrl.loadWalletData();
      }
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < cleanupFns.length; i++) {
        cleanupFns[i]();
      }
      cleanupFns.length = 0;
    };

    $ctrl.tierPerkLabel = function (tierItem) {
      if (!tierItem) { return ''; }
      if (angular.isArray(tierItem.benefits) && tierItem.benefits.length > 0) {
        return tierItem.benefits[0];
      }
      var code = String(tierItem.code || 'Bronze').toLowerCase();
      if (code === 'platinum') { return t('customerLoyalty.defaultPerkPlatinum', null, 'Maximum 2.00x earn rate & VIP perks'); }
      if (code === 'gold') { return t('customerLoyalty.defaultPerkGold', null, '1.50x earn rate & priority workshop bays'); }
      if (code === 'silver') { return t('customerLoyalty.defaultPerkSilver', null, '1.20x earn rate & workshop vouchers'); }
      return t('customerLoyalty.defaultPerkBronze', null, '1.00x automatic accrual on every invoice');
    };

    $ctrl.setTab = function (tabKey) {
      $ctrl.activeTab = tabKey;
    };

    $ctrl.loadWalletData = function () {
      if (!isLiveSession() || !DealerApiService) {
        return $q ? $q.resolve(null) : null;
      }

      $ctrl.isLoading = true;
      $ctrl.walletProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        account: DealerApiService.loyalty.getMyAccount(),
        tiers: DealerApiService.loyalty.getTiers().catch(function () { return null; }),
        catalog: DealerApiService.vouchers.listCatalog().catch(function () { return []; }),
        myVouchers: DealerApiService.vouchers.listMyVouchers().catch(function () { return []; }),
        ledger: DealerApiService.loyalty.getMyLedger($ctrl.ledgerFilter).catch(function () {
          return { items: [], pagination: { pageNumber: 1, pageSize: 15, totalCount: 0, totalPages: 0 } };
        }),
        orders: (angular.isFunction(DealerApiService.listMySalesOrders)
          ? DealerApiService.listMySalesOrders({ pageSize: 20 })
          : DealerApiService.listSalesOrders({ pageSize: 20 })
        ).catch(function () { return []; })
      }).then(function (bundle) {
        if (isDestroyed) {
          return bundle;
        }
        if (angular.isArray(bundle.tiers) && bundle.tiers.length > 0) {
          $ctrl.tiers = bundle.tiers;
        }
        applyAccountDto(bundle.account);
        $ctrl.catalog = (bundle.catalog || []).map(function (c) {
          return angular.extend({}, c, { discountSummary: $ctrl.formatDiscountSummary(c) });
        });
        $ctrl.myVouchers = (bundle.myVouchers || []).map(function (mv) {
          return angular.extend({}, mv, { discountSummary: $ctrl.formatDiscountSummary(mv) });
        });
        $ctrl.applyMyVouchersFilter();
        $ctrl.ledgerEntries = (bundle.ledger && bundle.ledger.items) || [];
        $ctrl.ledgerPagination = (bundle.ledger && bundle.ledger.pagination) || $ctrl.ledgerPagination;
        $ctrl.salesOrders = bundle.orders || [];
        $ctrl.hasLoadedOnce = true;
        return bundle;
      }).catch(function (rejection) {
        if (!isDestroyed && ProblemDetailsMapper) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.walletProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.loadLedger = function () {
      if (!isLiveSession() || !DealerApiService) {
        return $q ? $q.resolve($ctrl.ledgerEntries) : null;
      }
      $ctrl.isLoadingLedger = true;
      return DealerApiService.loyalty.getMyLedger($ctrl.ledgerFilter)
        .then(function (result) {
          if (!isDestroyed) {
            $ctrl.ledgerEntries = result.items || [];
            $ctrl.ledgerPagination = result.pagination || $ctrl.ledgerPagination;
          }
          return $ctrl.ledgerEntries;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoadingLedger = false;
          }
        });
    };

    $ctrl.onLedgerFilterChanged = function () {
      $ctrl.ledgerFilter.pageNumber = 1;
      return $ctrl.loadLedger();
    };

    $ctrl.changeLedgerPage = function (nextPage) {
      if (nextPage < 1) { return $q ? $q.resolve(null) : null; }
      $ctrl.ledgerFilter.pageNumber = nextPage;
      return $ctrl.loadLedger();
    };

    $ctrl.setVoucherStatusFilter = function (statusVal) {
      $ctrl.voucherStatusFilter = statusVal || '';
      $ctrl.applyMyVouchersFilter();
    };

    $ctrl.applyMyVouchersFilter = function () {
      var st = $ctrl.voucherStatusFilter;
      $ctrl.filteredMyVouchers = ($ctrl.myVouchers || []).filter(function (v) {
        if (!st) { return true; }
        return v.effectiveStatus === st || v.status === st;
      });
    };

    $ctrl.voucherStatusBadgeClass = function (status) {
      if (status === 'Available') { return 'badge-status-available'; }
      if (status === 'ReservedOnBooking') { return 'badge-info'; }
      if (status === 'Expired') { return 'badge-danger'; }
      return 'badge-neutral';
    };

    $ctrl.canRedeemCatalogItem = function (voucher) {
      if (!voucher || !voucher.isActive) {
        return false;
      }
      if (voucher.quotaTotal !== null && voucher.remainingQuota !== null && voucher.remainingQuota <= 0) {
        return false;
      }
      var userRank = LoyaltyCalculator.getTierRank($ctrl.tierCode, $ctrl.tiers);
      var reqRank = LoyaltyCalculator.getTierRank(voucher.minTierCode || voucher.minTier, $ctrl.tiers);
      if (userRank < reqRank) {
        return false;
      }
      return $ctrl.availablePoints >= voucher.pointsCost;
    };

    $ctrl.redeemButtonLabel = function (voucher) {
      if (!voucher || !voucher.isActive) {
        return t('customerLoyalty.unavailable', null, 'Unavailable');
      }
      if (voucher.quotaTotal !== null && voucher.remainingQuota !== null && voucher.remainingQuota <= 0) {
        return t('customerLoyalty.soldOut', null, 'Sold Out');
      }
      var userRank = LoyaltyCalculator.getTierRank($ctrl.tierCode, $ctrl.tiers);
      var minTier = voucher.minTierCode || voucher.minTier || 'Bronze';
      if (userRank < LoyaltyCalculator.getTierRank(minTier, $ctrl.tiers)) {
        return t('loyalty.tierLocked', { tier: enumT('LoyaltyTierCode', minTier) }, 'Requires ' + minTier + ' Tier');
      }
      if ($ctrl.availablePoints < voucher.pointsCost) {
        var missing = voucher.pointsCost - $ctrl.availablePoints;
        return t('customerLoyalty.needMorePts', { missing: missing }, 'Need ' + missing + ' more pts');
      }
      return t('customerLoyalty.redeemNow', null, 'Redeem Now');
    };

    $ctrl.openRedeemModal = function (voucher) {
      if (!voucher) { return; }
      $ctrl.redeemingCatalogItem = voucher;
      $ctrl.redeemProblem = null;
    };

    $ctrl.previewRedeem = $ctrl.openRedeemModal;

    $ctrl.closeRedeemModal = function () {
      $ctrl.redeemingCatalogItem = null;
      $ctrl.redeemProblem = null;
    };

    $ctrl.confirmRedeemVoucher = function () {
      var target = $ctrl.redeemingCatalogItem;
      if (!target || $ctrl.isRedeeming || !DealerApiService || !DealerApiService.vouchers) {
        return $q ? $q.resolve(null) : null;
      }

      $ctrl.isRedeeming = true;
      $ctrl.redeemProblem = null;

      return DealerApiService.vouchers.redeem({
        voucherCatalogId: target.id,
        accountRowVersion: $ctrl.accountRowVersion || null
      }).then(function (redeemed) {
        if (isDestroyed) {
          return redeemed;
        }
        $ctrl.lastRedeemedVoucher = redeemed;
        if (redeemed.availablePointsAfter !== null && redeemed.availablePointsAfter !== undefined) {
          $ctrl.availablePoints = Number(redeemed.availablePointsAfter);
        } else {
          $ctrl.availablePoints = Math.max(0, $ctrl.availablePoints - target.pointsCost);
        }
        syncSummaryObject();
        if (AuthService && angular.isFunction(AuthService.fetchCurrentUser)) {
          AuthService.fetchCurrentUser().catch(angular.noop);
        }
        if (ToastService) {
          ToastService.success(
            t('customerLoyalty.redeemToastTitle', null, 'Voucher Redeemed!'),
            t(
              'customerLoyalty.redeemToastBody',
              { code: redeemed.voucherCode, spent: redeemed.pointsSpent },
              'Code ' + redeemed.voucherCode + ' minted (-' + redeemed.pointsSpent + ' Available pts).'
            )
          );
        }
        $ctrl.closeRedeemModal();
        return $ctrl.loadWalletData().then(function () {
          return redeemed;
        });
      }).catch(function (rejection) {
        if (isDestroyed) {
          return null;
        }
        var problem = ProblemDetailsMapper.extract(rejection);
        $ctrl.redeemProblem = problem;
        if (problem.status === 409 || problem.status === 422) {
          $ctrl.loadWalletData();
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isRedeeming = false;
        }
      });
    };

    $ctrl.copyVoucherCode = function (code) {
      if (!code) { return; }
      if ($window && $window.navigator && $window.navigator.clipboard && angular.isFunction($window.navigator.clipboard.writeText)) {
        $window.navigator.clipboard.writeText(code).catch(angular.noop);
      }
      if (ToastService) {
        ToastService.info(
          t('customerLoyalty.copiedToastTitle', null, 'Voucher Code Copied'),
          t('customerLoyalty.copiedToastBody', { code: code }, code + ' copied to clipboard.')
        );
      }
    };

    $ctrl.openOrderReceipt = function (order) {
      if (!order) {
        return $q ? $q.resolve(null) : null;
      }
      $ctrl.activeReceiptKind = 'SalesOrder';
      var orderId = order.id || order.salesOrderId || '';
      if (!orderId || !DealerApiService || !angular.isFunction(DealerApiService.getSalesOrderReceipt)) {
        if (ToastService) {
          ToastService.warning(
            t('customerLoyalty.receiptUnavailableTitle', null, 'Receipt Unavailable'),
            t('customerLoyalty.receiptUnavailableBody', null, 'Could not resolve a sales order identifier for printable receipt lookup.')
          );
        }
        return $q ? $q.resolve(null) : null;
      }
      return DealerApiService.getSalesOrderReceipt(orderId)
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
  }
})();
