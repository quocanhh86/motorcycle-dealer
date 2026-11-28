(function () {
  'use strict';

  var SHOWROOM_I18N_VI = {
    showroomExtra: {
      rolePortalSignIn: 'Đăng nhập Cổng Vai trò',
      staffTestDriveDesk: 'Điều phối Lái thử',
      bookWorkshopService: 'Đặt lịch Bảo dưỡng',
      dealersSectionTitle: 'Hệ thống Đại lý Ủy quyền Toàn quốc ({{count}})',
      dealersSectionSub: 'Danh sách chi nhánh và số khung VIN sẵn sàng giao theo thời gian thực',
      allCities: 'Tất cả thành phố',
      vinsReadyBadge: '{{count}} VIN sẵn sàng',
      baysPerHour: '{{count}} khoang/giờ',
      filteringBranch: 'Đang lọc chi nhánh',
      filterStockCta: 'Lọc tồn kho',
      searchLabel: 'Tìm mẫu xe, thương hiệu hoặc mã SKU',
      categoryLabel: 'Phân khúc xe',
      dealerScopeLabel: 'Phạm vi tồn kho đại lý',
      inStockOnlyLabel: 'Chỉ hiện xe còn kho',
      sortDefault: 'Mặc định Showroom',
      skuLabel: 'Mã SKU',
      monthsShort: 'tháng',
      estLoyaltyLabel: 'Tích lũy dự kiến',
      rateLabel: 'hệ số {{mult}}x',
      oemSparePartsCta: 'Danh mục Phụ tùng OEM',
      modalTitleSuffix: 'Thông số & Lịch bảo dưỡng OEM',
      engineAndWarranty: 'Động cơ & Bảo hành',
      verifiedIsoUnits: 'Đơn vị VIN chuẩn ISO 3779',
      techSpecsTitle: 'Thông số kỹ thuật chi tiết',
      colMilestoneCode: 'Mã mốc',
      colMilestoneName: 'Hạng mục bảo dưỡng',
      colInterval: 'Chu kỳ',
      colCadence: 'Tính chất',
      colChecklist: 'Tóm tắt kiểm tra OEM',
      recurringLabel: 'Định kỳ lặp lại',
      oneTimeLabel: 'Một lần (Rô-đai)',
      defaultChecklist: 'Kiểm tra tiêu chuẩn tại xưởng hãng',
      defaultRulesFallback: 'Áp dụng lịch bảo dưỡng tiêu chuẩn: rô-đai 1.000 km đầu và định kỳ mỗi 4.000 km.',
      bookServiceShort: 'Đặt lịch xưởng',
      staffToastTitle: 'Điều phối Lái thử Chi nhánh',
      staffToastMsg: 'Đang mở hàng đợi lịch lái thử của chi nhánh.',
      customerOnlyToastTitle: 'Thao tác Cổng Khách hàng',
      customerOnlyToastMsg: 'Vui lòng đăng nhập bằng tài khoản Khách hàng để đặt lịch lái thử showroom.',
      angleFront: '3/4 Trước',
      angleSide: 'Ngang thân xe',
      angleRear: 'Đuôi & Đèn LED',
      angleCockpit: 'Đồng hồ & Tay lái',
      angleFrontShort: '3/4 Trước',
      angleSideShort: 'Thân xe',
      angleRearShort: 'Đuôi LED',
      angleCockpitShort: 'Đồng hồ',
      prevAngle: 'Xem góc chụp trước',
      nextAngle: 'Xem góc chụp tiếp theo',
      openGalleryHint: 'Mở bộ ảnh đa góc và thông số chi tiết',
      compareAdd: 'So sánh',
      compareSelected: 'Đã chọn',
      compareDockTitle: 'So sánh trực quan',
      compareClear: 'Xóa chọn',
      compareNowCta: 'So sánh ({{count}}/3)',
      compareModalEyebrow: 'SO SÁNH THÔNG SỐ & ĐIỂM HỘI VIÊN',
      compareModalTitle: 'Đặt cạnh {{count}} mẫu xe Showroom',
      compareSyncAngleLabel: 'Xoay đồng bộ 4 góc:',
      compareTierSimLabel: 'Ước tính điểm theo hạng:',
      compareRowCategory: 'Phân khúc',
      compareRowEngine: 'Dung tích động cơ',
      compareRowMsrp: 'Giá bán niêm yết',
      compareRowLoyalty: 'Điểm tích lũy ước tính',
      compareRowWarranty: 'Bảo hành OEM',
      compareRowStock: 'Tồn kho VIN sẵn sàng',
      compareRowAction: 'Trải nghiệm thực tế',
      compareMaxToastTitle: 'Tối đa 3 mẫu xe',
      compareMaxToastMsg: 'Hãy bỏ chọn 1 mẫu trong thanh So sánh để thêm mẫu xe khác.',
      compareRemoveAria: 'Bỏ {{model}} khỏi danh sách so sánh',
      open3dCta: 'HD 360°',
      spotlightEyebrow: 'TIÊU ĐIỂM SHOWROOM · ẢNH THẬT HD',
      modePhotosTab: 'Ảnh Thật 4 Góc HD',
      mode3dTab: 'Soi HD 360° Xe Thật & Linh Kiện',
      mediaModeAria: 'Chế độ xem hình ảnh xe Showroom'
    }
  };

  var SHOWROOM_I18N_EN = {
    showroomExtra: {
      rolePortalSignIn: 'Role Portal Sign-In',
      staffTestDriveDesk: 'Staff Test-Drive Desk',
      bookWorkshopService: 'Book Workshop Service',
      dealersSectionTitle: 'Nationwide Authorized Dealerships ({{count}})',
      dealersSectionSub: 'Live branch directory and ready-to-deliver VIN inventory counts',
      allCities: 'All Cities',
      vinsReadyBadge: '{{count}} VINs Ready',
      baysPerHour: '{{count}} Bays/hr',
      filteringBranch: 'Filtering Branch',
      filterStockCta: 'Filter Stock',
      searchLabel: 'Search Model, Brand, or SKU',
      categoryLabel: 'Category',
      dealerScopeLabel: 'Dealership Stock Scope',
      inStockOnlyLabel: 'In-Stock Only',
      sortDefault: 'Featured Order',
      skuLabel: 'SKU',
      monthsShort: 'mo',
      estLoyaltyLabel: 'Est. Loyalty',
      rateLabel: '{{mult}}x rate',
      oemSparePartsCta: 'OEM Spare Parts',
      modalTitleSuffix: 'Specs & OEM Maintenance Schedule',
      engineAndWarranty: 'Engine & Warranty',
      verifiedIsoUnits: 'Verified ISO 3779 VIN units',
      techSpecsTitle: 'Technical Specifications',
      colMilestoneCode: 'Code',
      colMilestoneName: 'Milestone',
      colInterval: 'Interval',
      colCadence: 'Cadence',
      colChecklist: 'OEM Checklist Summary',
      recurringLabel: 'Recurring',
      oneTimeLabel: 'One-Time',
      defaultChecklist: 'Standard workshop inspection',
      defaultRulesFallback: 'Standard 1,000 km break-in and 4,000 km periodic service intervals apply.',
      bookServiceShort: 'Book Service',
      staffToastTitle: 'Staff Test-Drive Desk',
      staffToastMsg: 'Opening branch test-drive queue.',
      customerOnlyToastTitle: 'Customer Portal Action',
      customerOnlyToastMsg: 'Sign in with a Customer account to schedule a showroom test drive.',
      angleFront: 'Front 3/4',
      angleSide: 'Side Profile',
      angleRear: 'Rear & LED',
      angleCockpit: 'Cockpit & Dash',
      angleFrontShort: 'Front 3/4',
      angleSideShort: 'Side',
      angleRearShort: 'Rear LED',
      angleCockpitShort: 'Cockpit',
      prevAngle: 'Previous studio angle',
      nextAngle: 'Next studio angle',
      openGalleryHint: 'Open multi-angle gallery and specifications',
      compareAdd: 'Compare',
      compareSelected: 'Compared',
      compareDockTitle: 'Visual Spec Compare',
      compareClear: 'Clear',
      compareNowCta: 'Compare ({{count}}/3)',
      compareModalEyebrow: 'SIDE-BY-SIDE SPEC & LOYALTY COMPARE',
      compareModalTitle: 'Comparing {{count}} Showroom Motorcycles',
      compareSyncAngleLabel: 'Sync 4 studio angles:',
      compareTierSimLabel: 'Simulate loyalty tier:',
      compareRowCategory: 'Segment',
      compareRowEngine: 'Engine Displacement',
      compareRowMsrp: 'Retail MSRP',
      compareRowLoyalty: 'Est. Loyalty Earned',
      compareRowWarranty: 'OEM Warranty',
      compareRowStock: 'Live VIN Stock',
      compareRowAction: 'Book Experience',
      compareMaxToastTitle: 'Maximum 3 Motorcycles',
      compareMaxToastMsg: 'Remove one motorcycle from the compare dock before adding another.',
      compareRemoveAria: 'Remove {{model}} from comparison',
      open3dCta: 'HD 360°',
      spotlightEyebrow: 'SHOWROOM SPOTLIGHT · REAL HD PHOTO',
      modePhotosTab: '4-Angle Real HD Photos',
      mode3dTab: 'Interactive Real HD 360°',
      mediaModeAria: 'Showroom media view mode'
    }
  };

  var BUNDLED_MULTI_ANGLE_SLUGS = {
    'honda-airblade-160': true,
    'honda-sh160i-sport': true,
    'honda-winner-x': true,
    'yamaha-exciter-155': true,
    'honda-cb300r': true,
    'honda-nx500': true
  };

  var MODEL_CODE_TO_STUDIO_SLUG = {
    'HND-AB160-2026': 'honda-airblade-160',
    'HND-SH160I-2026': 'honda-sh160i-sport',
    'HND-WINX-2026': 'honda-winner-x',
    'YMH-EXC155-2026': 'yamaha-exciter-155',
    'HND-CB300R-2026': 'honda-cb300r',
    'HND-NX500-2026': 'honda-nx500'
  };

  var GALLERY_ANGLE_DEFS = [
    { key: 'front', suffix: '', labelKey: 'showroomExtra.angleFront', shortLabelKey: 'showroomExtra.angleFrontShort', fallback: 'Front 3/4' },
    { key: 'side', suffix: '-side', labelKey: 'showroomExtra.angleSide', shortLabelKey: 'showroomExtra.angleSideShort', fallback: 'Side Profile' },
    { key: 'rear', suffix: '-rear', labelKey: 'showroomExtra.angleRear', shortLabelKey: 'showroomExtra.angleRearShort', fallback: 'Rear & LED' },
    { key: 'cockpit', suffix: '-cockpit', labelKey: 'showroomExtra.angleCockpit', shortLabelKey: 'showroomExtra.angleCockpitShort', fallback: 'Cockpit & Dash' }
  ];

  angular
    .module('app.features.public')
    .run(registerShowroomI18n)
    .component('publicShowroom', {
      controllerAs: '$ctrl',
      controller: PublicShowroomController,
      template:
        '<section>' +
          /* 1. Flagship Asymmetric Atelier Showroom Hero + Live Flagship Spotlight Plate */
          '<header class="atelier-hero">' +
            '<picture aria-hidden="true">' +
              '<source srcset="/assets/editorial/showroom-hero-apex.webp" type="image/webp" />' +
              '<img class="atelier-hero-bg" src="/assets/editorial/showroom-hero-apex.jpg" alt="" loading="eager" decoding="async" />' +
            '</picture>' +
            '<div class="atelier-hero-overlay" aria-hidden="true"></div>' +
            '<div class="atelier-hero-gridlines" aria-hidden="true"></div>' +

            '<div class="atelier-hero-grid">' +
              '<div class="atelier-hero-content fade-slide-up">' +
                '<span class="atelier-hero-eyebrow">' +
                  '<app-icon name="sparkle" weight="fill" size="13"></app-icon>' +
                  '<span>{{ \'showroom.eyebrow\' | t }}</span>' +
                '</span>' +

                '<h1 class="atelier-hero-title">{{ \'showroom.heroTitle\' | t }}</h1>' +
                '<p class="atelier-hero-subtitle">{{ \'showroom.heroSubtitle\' | t }}</p>' +

                '<div style="display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin-top:4px;">' +
                  '<button type="button" class="btn-pill-cta" ' +
                          'ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'" ' +
                          'ng-click="$ctrl.bookTestDrive($ctrl.getSpotlightModel())">' +
                    '<span>{{ \'showroom.bookTestDriveCta\' | t }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                    '</span>' +
                  '</button>' +
                  '<a class="btn-pill-cta btn-pill-secondary" ' +
                     'ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'" ' +
                     'ui-sref="app.customer.bookingNew({ dealerId: $ctrl.filters.dealerId || undefined })">' +
                    '<span>{{ \'showroomExtra.bookWorkshopService\' | t }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="wrench" weight="duotone" size="15"></app-icon>' +
                    '</span>' +
                  '</a>' +
                  '<a class="btn-pill-cta btn-pill-secondary" ng-if="$ctrl.userRole === \'DealerStaff\'" ui-sref="app.staff.testDrives">' +
                    '<span>{{ \'showroomExtra.staffTestDriveDesk\' | t }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="motorcycle" weight="duotone" size="15"></app-icon>' +
                    '</span>' +
                  '</a>' +
                '</div>' +

                '<div class="atelier-hero-stats">' +
                  '<div class="atelier-hero-stat">' +
                    '<div style="font-size:10px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.78;">{{ \'showroom.statModels\' | t }}</div>' +
                    '<div class="mono-num" style="font-family:var(--font-display);font-size:22px;font-weight:700;margin-top:2px;" kpi-counter="$ctrl.heroStats.totalModels">0</div>' +
                  '</div>' +
                  '<div class="atelier-hero-stat">' +
                    '<div style="font-size:10px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.78;">{{ \'showroom.statDealers\' | t }}</div>' +
                    '<div class="mono-num" style="font-family:var(--font-display);font-size:22px;font-weight:700;margin-top:2px;" kpi-counter="$ctrl.heroStats.totalDealers">0</div>' +
                  '</div>' +
                  '<div class="atelier-hero-stat">' +
                    '<div style="font-size:10px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.78;">{{ \'showroom.statAvailableVins\' | t }}</div>' +
                    '<div class="mono-num" style="font-family:var(--font-display);font-size:22px;font-weight:700;margin-top:2px;" kpi-counter="$ctrl.heroStats.totalAvailableVins">0</div>' +
                  '</div>' +
                  '<div class="atelier-hero-stat">' +
                    '<div style="font-size:10px;text-transform:uppercase;letter-spacing:0.06em;opacity:0.78;">{{ \'showroom.statWarranty\' | t }}</div>' +
                    '<div style="font-family:var(--font-display);font-size:14px;font-weight:700;margin-top:6px;">{{ \'showroom.warrantyStandardValue\' | t }}</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              /* Right Asymmetric Flagship Real-Photo Spotlight Card */
              '<div class="atelier-spotlight-card fade-slide-up" ng-if="$ctrl.getSpotlightModel()">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;gap:8px;">' +
                  '<span class="mono-num atelier-spotlight-eyebrow">' +
                    '{{ \'showroomExtra.spotlightEyebrow\' | t }}' +
                  '</span>' +
                  '<span class="mono-num atelier-spotlight-cc">{{ $ctrl.getSpotlightModel().engineDisplacementCc }} cc</span>' +
                '</div>' +
                '<button type="button" class="atelier-spotlight-trigger" ' +
                        'ng-attr-aria-label="{{ $ctrl.getSpotlightModel().modelName }} · {{ \'showroomExtra.open3dCta\' | t }}" ' +
                        'ng-click="$ctrl.openScheduleModal($ctrl.getSpotlightModel(), null, \'3d\')">' +
                  '<bike-image src="$ctrl.getCardActiveImageUrl($ctrl.getSpotlightModel())" ' +
                              'alt="{{ $ctrl.getSpotlightModel().modelName }}" ' +
                              'category="{{ $ctrl.getSpotlightModel().category }}" ' +
                              'variant="card"></bike-image>' +
                '</button>' +
                '<div class="atelier-spotlight-meta">' +
                  '<div style="min-width:0;">' +
                    '<div class="atelier-spotlight-title">' +
                      '{{ $ctrl.getSpotlightModel().modelName }}' +
                    '</div>' +
                    '<div class="mono-num atelier-spotlight-price">' +
                      '{{ $ctrl.getSpotlightModel().baseRetailPrice | vndCurrency }}' +
                    '</div>' +
                  '</div>' +
                  '<button type="button" class="btn btn-primary btn-sm" style="white-space:nowrap;" ng-click="$ctrl.openScheduleModal($ctrl.getSpotlightModel(), null, \'3d\')">' +
                    '<app-icon name="camera" weight="duotone" size="14"></app-icon>' +
                    '<span>{{ \'showroomExtra.open3dCta\' | t }}</span>' +
                  '</button>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</header>' +

          /* 2. Error Retry Banner */
          '<error-retry-banner ng-if="$ctrl.catalogProblem" ' +
                               'message="$ctrl.catalogProblem.detail" ' +
                               'trace-id="$ctrl.catalogProblem.traceId" ' +
                               'retrying="$ctrl.isLoading" ' +
                               'on-retry="$ctrl.loadShowroomData()"></error-retry-banner>' +

          /* 3. Nationwide Authorized Dealerships Strip */
          '<div class="surface-card" style="margin-bottom:24px;">' +
            '<div class="surface-card-header" style="flex-wrap:wrap;">' +
              '<div>' +
                '<h2 class="surface-card-title" style="display:flex;align-items:center;gap:8px;">' +
                  '<app-icon name="map-pin" weight="duotone" size="20"></app-icon>' +
                  '<span>{{ \'showroomExtra.dealersSectionTitle\' | t:{ count: $ctrl.filteredDealers.length } }}</span>' +
                '</h2>' +
                '<div class="text-muted" style="font-size:12px;margin-top:2px;">{{ \'showroomExtra.dealersSectionSub\' | t }}</div>' +
              '</div>' +
              '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
                '<button type="button" class="btn btn-sm" ' +
                        'ng-repeat="cityOpt in $ctrl.cityOptions track by cityOpt.value" ' +
                        'ng-class="$ctrl.selectedCity === cityOpt.value ? \'btn-primary\' : \'btn-secondary\'" ' +
                        'ng-click="$ctrl.selectCity(cityOpt.value)">' +
                  '{{ cityOpt.labelKey ? (cityOpt.labelKey | t) : cityOpt.label }}' +
                '</button>' +
              '</div>' +
            '</div>' +

            '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.dealers.length === 0" variant="kpi" count="3"></skeleton-loader>' +

            '<div class="grid-12" ng-if="$ctrl.filteredDealers.length > 0">' +
              '<div class="col-4 stagger-item" ng-attr-style="--i:{{ $index }}" ng-repeat="dealer in $ctrl.filteredDealers track by dealer.id">' +
                '<div class="kpi-card" style="height:100%;display:flex;flex-direction:column;justify-content:space-between;">' +
                  '<div>' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;">' +
                      '<span class="kpi-label">{{ dealer.city }} • <span class="mono-num">{{ dealer.code }}</span></span>' +
                      '<span class="badge badge-success mono-num">{{ \'showroomExtra.vinsReadyBadge\' | t:{ count: $ctrl.getDealerStockCount(dealer) } }}</span>' +
                    '</div>' +
                    '<div class="text-primary" style="font-weight:700;font-size:15px;margin-top:6px;">{{ dealer.name }}</div>' +
                    '<div class="kpi-meta" style="margin-top:4px;">{{ dealer.address }}</div>' +
                  '</div>' +
                  '<div class="text-secondary" style="margin-top:12px;padding-top:10px;border-top:1px solid var(--clr-border-subtle);display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;font-size:12px;">' +
                    '<span class="mono-num" style="display:inline-flex;align-items:center;gap:5px;">' +
                      '<app-icon name="clock" weight="regular" size="13"></app-icon>' +
                      '<span>{{ dealer.hoursDisplay }} • {{ \'showroomExtra.baysPerHour\' | t:{ count: dealer.defaultSlotCapacity } }}</span>' +
                    '</span>' +
                    '<button type="button" class="btn btn-sm" ' +
                            'ng-class="$ctrl.filters.dealerId === dealer.id ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'ng-click="$ctrl.filterByDealer(dealer.id)">' +
                      '<app-icon name="{{ $ctrl.filters.dealerId === dealer.id ? \'check\' : \'funnel\' }}" weight="regular" size="12"></app-icon>' +
                      '<span>{{ $ctrl.filters.dealerId === dealer.id ? (\'showroomExtra.filteringBranch\' | t) : (\'showroomExtra.filterStockCta\' | t) }}</span>' +
                    '</button>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* 4. Catalog Filter & Sort Bar */
          '<div class="surface-card" style="margin-bottom:24px;">' +
            '<div class="grid-12" style="align-items:end;gap:var(--space-4);">' +
              '<div class="col-4 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="showroomSearch" style="display:inline-flex;align-items:center;gap:6px;">' +
                  '<app-icon name="magnifying-glass" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'showroomExtra.searchLabel\' | t }}</span>' +
                '</label>' +
                '<input id="showroomSearch" type="search" class="form-control" ' +
                       'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyCatalogFilters()" ' +
                       'ng-attr-placeholder="{{ \'showroom.filterSearchPlaceholder\' | t }}" />' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="showroomCategory">{{ \'showroomExtra.categoryLabel\' | t }}</label>' +
                '<select id="showroomCategory" class="form-control" ' +
                        'ng-model="$ctrl.filters.category" ' +
                        'ng-options="c.value as c.label for c in $ctrl.categoryOptions" ' +
                        'ng-change="$ctrl.applyCatalogFilters()"></select>' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="showroomDealerFilter">{{ \'showroomExtra.dealerScopeLabel\' | t }}</label>' +
                '<select id="showroomDealerFilter" class="form-control" ' +
                        'ng-model="$ctrl.filters.dealerId" ' +
                        'ng-options="d.value as d.label for d in $ctrl.dealerSelectOptions" ' +
                        'ng-change="$ctrl.applyCatalogFilters()"></select>' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<label class="form-label" for="showroomSortBy">{{ \'showroom.sortByLabel\' | t }}</label>' +
                '<select id="showroomSortBy" class="form-control" ' +
                        'ng-model="$ctrl.sortBy" ' +
                        'ng-options="s.value as s.label for s in $ctrl.sortOptions" ' +
                        'ng-change="$ctrl.applyCatalogFilters()"></select>' +
              '</div>' +
              '<div class="col-2 form-group" style="margin-bottom:0;">' +
                '<label class="text-primary" style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;min-height:40px;">' +
                  '<input type="checkbox" ng-model="$ctrl.filters.inStockOnly" ng-change="$ctrl.applyCatalogFilters()" />' +
                  '<span>{{ \'showroomExtra.inStockOnlyLabel\' | t }}</span>' +
                '</label>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* 5. Skeleton Loading / Empty State / Double-Bezel Motorcycle Catalog Grid */
          '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.models.length === 0" variant="cards" count="6"></skeleton-loader>' +

          '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredModels.length === 0" ' +
                       'icon="motorcycle" ' +
                       'title-key="showroom.emptyCatalogTitle" ' +
                       'desc-key="showroom.emptyCatalogDesc" ' +
                       'action-key="common.clearFilters" ' +
                       'on-action="$ctrl.resetFilters()"></empty-state>' +

          '<div class="grid-12" ng-if="$ctrl.filteredModels.length > 0">' +
            '<article class="col-4 bezel-card showroom-model-card stagger-item" ng-attr-style="--i:{{ $index }}" ng-repeat="model in $ctrl.filteredModels track by model.id">' +
              '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:14px;">' +
                '<div>' +
                  '<div class="card-gallery-wrap">' +
                    '<div class="card-gallery-stage">' +
                      '<button type="button" class="card-gallery-trigger" ' +
                              'ng-attr-aria-label="{{ model.modelName }} · {{ \'showroomExtra.openGalleryHint\' | t }}" ' +
                              'ng-click="$ctrl.openScheduleModal(model, $ctrl.getCardAngleIndex(model))">' +
                        '<bike-image src="$ctrl.getCardActiveImageUrl(model)" ' +
                                    'alt="{{ model.modelName }} · {{ $ctrl.getCardActiveAngleLabel(model) }}" ' +
                                    'category="{{ model.category }}" ' +
                                    'variant="card"></bike-image>' +
                      '</button>' +
                      '<span class="card-gallery-count-pill mono-num" aria-hidden="true" ng-if="$ctrl.getModelGallery(model).length > 1">' +
                        '<app-icon name="images" weight="duotone" size="12"></app-icon>' +
                        '<span>{{ $ctrl.getCardAngleIndex(model) + 1 }}/{{ $ctrl.getModelGallery(model).length }}</span>' +
                      '</span>' +
                    '</div>' +
                    '<div class="card-angle-strip" ng-if="$ctrl.getModelGallery(model).length > 1" role="group" ng-attr-aria-label="{{ model.modelName }}">' +
                      '<button type="button" class="card-angle-pill" ' +
                              'ng-repeat="ang in $ctrl.getModelGallery(model) track by ang.key" ' +
                              'ng-class="{\'is-active\': $ctrl.getCardAngleIndex(model) === $index}" ' +
                              'ng-attr-aria-pressed="{{ $ctrl.getCardAngleIndex(model) === $index ? \'true\' : \'false\' }}" ' +
                              'ng-attr-title="{{ ang.labelKey | t }}" ' +
                              'ng-click="$ctrl.selectCardAngle(model, $index)">' +
                        '{{ ang.shortLabelKey | t }}' +
                      '</button>' +
                    '</div>' +
                  '</div>' +

                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:14px;flex-wrap:wrap;">' +
                    '<span class="text-muted" style="font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:0.06em;">' +
                      '{{ model.brand }} · {{ model.category | enumT:\'MotorcycleCategory\' }} · <span class="mono-num">{{ model.modelCode }}</span>' +
                    '</span>' +
                    '<span class="badge" ng-class="$ctrl.getModelStockCount(model) > 0 ? \'badge-status-available\' : \'badge-neutral\'">' +
                      '<app-icon name="{{ $ctrl.getModelStockCount(model) > 0 ? \'check-circle\' : \'prohibit\' }}" weight="fill" size="12"></app-icon>' +
                      '<span>{{ $ctrl.getModelStockCount(model) > 0 ? (\'showroom.inStockBadge\' | t:{ count: $ctrl.getModelStockCount(model) }) : (\'showroom.outOfStockBadge\' | t) }}</span>' +
                    '</span>' +
                  '</div>' +

                  '<h2 class="surface-card-title" style="margin-top:6px;">{{ model.modelName }}</h2>' +

                  '<div class="text-muted" style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;font-size:12px;margin:8px 0 12px;">' +
                    '<span class="mono-num" style="display:inline-flex;align-items:center;gap:4px;">' +
                      '<app-icon name="gauge" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ model.engineDisplacementCc }} cc</span>' +
                    '</span>' +
                    '<span style="display:inline-flex;align-items:center;gap:4px;">' +
                      '<app-icon name="shield-check" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ model.warrantyMonths }} {{ \'showroomExtra.monthsShort\' | t }} / {{ model.warrantyMileageKm | number }} km</span>' +
                    '</span>' +
                  '</div>' +

                  '<div class="showroom-price-plate">' +
                    '<div class="text-muted" style="font-size:11px;text-transform:uppercase;font-weight:700;letter-spacing:0.04em;">{{ \'showroom.msrpLabel\' | t }}</div>' +
                    '<div class="mono-num text-primary" style="font-size:19px;font-weight:700;">{{ model.baseRetailPrice | vndCurrency }}</div>' +
                    '<div style="font-size:12px;color:var(--clr-success-fg);display:flex;align-items:center;gap:4px;flex-wrap:wrap;">' +
                      '<app-icon name="medal" weight="fill" size="13"></app-icon>' +
                      '<span>{{ \'showroomExtra.estLoyaltyLabel\' | t }}: <strong class="mono-num">+{{ $ctrl.estimatePoints(model.baseRetailPrice) | number }} {{ \'common.ptsShort\' | t }}</strong> ({{ \'showroomExtra.rateLabel\' | t:{ mult: $ctrl.activeMultiplier } }})</span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;padding-top:12px;border-top:1px solid var(--clr-border-subtle);">' +
                  '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                    '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.openScheduleModal(model)">' +
                      '<app-icon name="wrench" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'showroom.inspectSpecsCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="compare-toggle-btn" ng-click="$ctrl.openScheduleModal(model, null, \'3d\')">' +
                      '<app-icon name="camera" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'showroomExtra.open3dCta\' | t }}</span>' +
                    '</button>' +
                    '<button type="button" class="compare-toggle-btn" ' +
                            'ng-class="{\'is-selected\': $ctrl.isInCompare(model)}" ' +
                            'ng-attr-aria-pressed="{{ $ctrl.isInCompare(model) ? \'true\' : \'false\' }}" ' +
                            'ng-click="$ctrl.toggleCompare(model)">' +
                      '<app-icon name="{{ $ctrl.isInCompare(model) ? \'check-circle\' : \'scales\' }}" weight="{{ $ctrl.isInCompare(model) ? \'fill\' : \'regular\' }}" size="14"></app-icon>' +
                      '<span>{{ $ctrl.isInCompare(model) ? (\'showroomExtra.compareSelected\' | t) : (\'showroomExtra.compareAdd\' | t) }}</span>' +
                    '</button>' +
                  '</div>' +
                  '<button type="button" class="btn-pill-cta book-test-drive-btn" ' +
                          'style="min-height:36px;padding:3px 4px 3px 14px;font-size:12px;" ' +
                          'ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'" ' +
                          'ng-click="$ctrl.bookTestDrive(model)">' +
                    '<span>{{ \'showroom.bookTestDriveCta\' | t }}</span>' +
                    '<span class="btn-pill-well" style="width:26px;height:26px;" aria-hidden="true">' +
                      '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                    '</span>' +
                  '</button>' +
                  '<a class="btn btn-secondary btn-sm" ng-if="$ctrl.userRole === \'DealerStaff\'" ui-sref="app.staff.testDrives">' +
                    '<span>{{ \'showroomExtra.staffTestDriveDesk\' | t }}</span>' +
                    '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                  '</a>' +
                  '<a class="btn btn-secondary btn-sm" ng-if="$ctrl.userRole === \'Admin\'" ui-sref="app.admin.spareParts">' +
                    '<span>{{ \'showroomExtra.oemSparePartsCta\' | t }}</span>' +
                    '<app-icon name="arrow-right" weight="regular" size="14"></app-icon>' +
                  '</a>' +
                '</div>' +
              '</div>' +
            '</article>' +
          '</div>' +

          /* 6. Vehicle Detail & OEM Schedule Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="showroomModalTitle" ng-if="$ctrl.selectedModel" ng-click="$ctrl.closeScheduleModal()">' +
            '<div class="modal-dialog modal-dialog-wide modal-spring-in motion-modal" ng-click="$event.stopPropagation()">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ $ctrl.selectedModel.modelCode }} • {{ $ctrl.selectedModel.category | enumT:\'MotorcycleCategory\' }}</span>' +
                  '<h2 id="showroomModalTitle" class="surface-card-title" style="margin-top:4px;">{{ $ctrl.selectedModel.modelName }} · {{ \'showroomExtra.modalTitleSuffix\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeScheduleModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="modal-body">' +
                '<div class="moto3d-mode-switch" role="group" ng-attr-aria-label="{{ \'showroomExtra.mediaModeAria\' | t }}">' +
                  '<button type="button" class="moto3d-mode-btn" ng-class="{\'is-active\': $ctrl.modalMediaMode === \'photos\'}" ng-attr-aria-pressed="{{ $ctrl.modalMediaMode === \'photos\' ? \'true\' : \'false\' }}" ng-click="$ctrl.setModalMediaMode(\'photos\')">' +
                    '<app-icon name="images" weight="duotone" size="15"></app-icon>' +
                    '<span>{{ \'showroomExtra.modePhotosTab\' | t }}</span>' +
                  '</button>' +
                  '<button type="button" class="moto3d-mode-btn" ng-class="{\'is-active\': $ctrl.modalMediaMode === \'3d\'}" ng-attr-aria-pressed="{{ $ctrl.modalMediaMode === \'3d\' ? \'true\' : \'false\' }}" ng-click="$ctrl.setModalMediaMode(\'3d\')">' +
                    '<app-icon name="camera" weight="duotone" size="15"></app-icon>' +
                    '<span>{{ \'showroomExtra.mode3dTab\' | t }}</span>' +
                  '</button>' +
                '</div>' +

                '<div ng-if="$ctrl.modalMediaMode === \'3d\'" style="margin-bottom:16px;">' +
                  '<moto-3d-viewer model="$ctrl.selectedModel"></moto-3d-viewer>' +
                '</div>' +

                '<div class="studio-gallery-wrap" ng-if="$ctrl.modalMediaMode !== \'3d\'" style="margin-bottom:16px;">' +
                  '<div class="studio-gallery-stage">' +
                    '<bike-image src="$ctrl.getSelectedModalAngleUrl()" ' +
                                'alt="{{ $ctrl.selectedModel.modelName }} · {{ $ctrl.getSelectedModalAngleLabel() }}" ' +
                                'category="{{ $ctrl.selectedModel.category }}" ' +
                                'variant="modal" ' +
                                'badge="{{ $ctrl.selectedModel.category | enumT:\'MotorcycleCategory\' }}"></bike-image>' +
                    '<button type="button" class="studio-gallery-nav is-prev" ' +
                            'ng-if="$ctrl.selectedModelGallery.length > 1" ' +
                            'ng-attr-aria-label="{{ \'showroomExtra.prevAngle\' | t }}" ' +
                            'ng-click="$ctrl.stepModalGallery(-1)">' +
                      '<app-icon name="caret-left" weight="regular" size="18"></app-icon>' +
                    '</button>' +
                    '<button type="button" class="studio-gallery-nav is-next" ' +
                            'ng-if="$ctrl.selectedModelGallery.length > 1" ' +
                            'ng-attr-aria-label="{{ \'showroomExtra.nextAngle\' | t }}" ' +
                            'ng-click="$ctrl.stepModalGallery(1)">' +
                      '<app-icon name="caret-right" weight="regular" size="18"></app-icon>' +
                    '</button>' +
                    '<div class="studio-gallery-caption mono-num" ng-if="$ctrl.selectedModelGallery.length > 0">' +
                      '<app-icon name="camera" weight="duotone" size="13"></app-icon>' +
                      '<span>{{ $ctrl.selectedGalleryIndex + 1 }} / {{ $ctrl.selectedModelGallery.length }}</span>' +
                      '<span aria-hidden="true">•</span>' +
                      '<span>{{ $ctrl.getSelectedModalAngleLabel() }}</span>' +
                    '</div>' +
                  '</div>' +
                  '<div class="studio-gallery-thumbs" ng-if="$ctrl.selectedModelGallery.length > 1">' +
                    '<button type="button" class="studio-gallery-thumb" ' +
                            'ng-repeat="ang in $ctrl.selectedModelGallery track by ang.key" ' +
                            'ng-class="{\'is-active\': $ctrl.selectedGalleryIndex === $index}" ' +
                            'ng-attr-aria-pressed="{{ $ctrl.selectedGalleryIndex === $index ? \'true\' : \'false\' }}" ' +
                            'ng-click="$ctrl.selectModalGalleryIndex($index)">' +
                      '<bike-image src="ang.url" decorative="true" variant="card"></bike-image>' +
                      '<span class="studio-gallery-thumb-label">{{ ang.labelKey | t }}</span>' +
                    '</button>' +
                  '</div>' +
                '</div>' +

                '<div class="grid-12" style="margin-bottom:16px;gap:var(--space-4);">' +
                  '<div class="col-4 kpi-card">' +
                    '<div class="kpi-label">{{ \'showroomExtra.engineAndWarranty\' | t }}</div>' +
                    '<div class="kpi-value mono-num" style="font-size:18px;">{{ $ctrl.selectedModel.engineDisplacementCc }} cc</div>' +
                    '<div class="kpi-meta">{{ $ctrl.selectedModel.warrantyMonths }} {{ \'showroomExtra.monthsShort\' | t }} / {{ $ctrl.selectedModel.warrantyMileageKm | number }} km</div>' +
                  '</div>' +
                  '<div class="col-4 kpi-card">' +
                    '<div class="kpi-label">{{ \'showroom.msrpLabel\' | t }}</div>' +
                    '<div class="kpi-value mono-num" style="font-size:18px;">{{ $ctrl.selectedModel.baseRetailPrice | vndCurrency }}</div>' +
                    '<div class="kpi-meta">+{{ $ctrl.estimatePoints($ctrl.selectedModel.baseRetailPrice) | number }} {{ \'common.ptsShort\' | t }} @ {{ $ctrl.activeMultiplier }}x</div>' +
                  '</div>' +
                  '<div class="col-4 kpi-card">' +
                    '<div class="kpi-label">{{ \'showroom.statAvailableVins\' | t }}</div>' +
                    '<div class="kpi-value mono-num" style="font-size:18px;">{{ $ctrl.getModelStockCount($ctrl.selectedModel) }} VIN</div>' +
                    '<div class="kpi-meta">{{ \'showroomExtra.verifiedIsoUnits\' | t }}</div>' +
                  '</div>' +
                '</div>' +

                '<div ng-if="$ctrl.selectedModelSpecEntries.length > 0" style="margin-bottom:16px;">' +
                  '<div class="form-label" style="margin-bottom:6px;">{{ \'showroomExtra.techSpecsTitle\' | t }}</div>' +
                  '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
                    '<span class="badge badge-neutral mono-num" ng-repeat="spec in $ctrl.selectedModelSpecEntries track by spec.key">' +
                      '{{ spec.key }}: {{ spec.value }}' +
                    '</span>' +
                  '</div>' +
                '</div>' +

                '<div style="margin-bottom:16px;" ng-if="$ctrl.selectedModelBranchStocks.length > 0">' +
                  '<div class="form-label" style="margin-bottom:6px;">{{ \'showroom.dealerStockTitle\' | t }}</div>' +
                  '<div style="display:flex;gap:8px;flex-wrap:wrap;">' +
                    '<span class="badge" ng-repeat="bs in $ctrl.selectedModelBranchStocks track by bs.dealerId" ' +
                          'ng-class="bs.count > 0 ? \'badge-status-available\' : \'badge-neutral\'">' +
                      '{{ bs.dealerName }} ({{ bs.dealerCode }}): {{ bs.count }} VIN' +
                    '</span>' +
                  '</div>' +
                '</div>' +

                '<div class="form-label" style="margin-bottom:8px;">{{ \'showroom.oemScheduleTitle\' | t }}</div>' +
                '<skeleton-loader ng-if="$ctrl.isLoadingRules" variant="table" count="3"></skeleton-loader>' +
                '<div class="table-wrap" ng-if="!$ctrl.isLoadingRules">' +
                  '<table class="data-table">' +
                    '<thead>' +
                      '<tr>' +
                        '<th>{{ \'showroomExtra.colMilestoneCode\' | t }}</th>' +
                        '<th>{{ \'showroomExtra.colMilestoneName\' | t }}</th>' +
                        '<th>{{ \'showroomExtra.colInterval\' | t }}</th>' +
                        '<th>{{ \'showroomExtra.colCadence\' | t }}</th>' +
                        '<th>{{ \'showroomExtra.colChecklist\' | t }}</th>' +
                      '</tr>' +
                    '</thead>' +
                    '<tbody>' +
                      '<tr ng-if="$ctrl.selectedModelRules.length === 0">' +
                        '<td colspan="5" class="text-muted" style="text-align:center;">{{ \'showroomExtra.defaultRulesFallback\' | t }}</td>' +
                      '</tr>' +
                      '<tr ng-repeat="rule in $ctrl.selectedModelRules track by rule.id">' +
                        '<td class="mono-num">{{ rule.milestoneCode }}</td>' +
                        '<td style="font-weight:600;">{{ rule.milestoneName }}</td>' +
                        '<td class="mono-num">{{ rule.intervalKm | number }} km / {{ rule.intervalMonths }} {{ \'showroomExtra.monthsShort\' | t }}</td>' +
                        '<td>' +
                          '<span class="badge" ng-class="rule.isRecurring ? \'badge-info\' : \'badge-neutral\'">' +
                            '{{ rule.isRecurring ? (\'showroomExtra.recurringLabel\' | t) : (\'showroomExtra.oneTimeLabel\' | t) }}' +
                          '</span>' +
                        '</td>' +
                        '<td class="text-secondary" style="font-size:12px;">{{ rule.checklistSummary || (\'showroomExtra.defaultChecklist\' | t) }}</td>' +
                      '</tr>' +
                    '</tbody>' +
                  '</table>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeScheduleModal()">{{ \'common.close\' | t }}</button>' +
                '<a class="btn btn-secondary" ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'" ng-click="$ctrl.closeScheduleModal()" ui-sref="app.customer.bookingNew({ dealerId: $ctrl.filters.dealerId || undefined })">' +
                  '<app-icon name="wrench" weight="duotone" size="15"></app-icon>' +
                  '<span>{{ \'showroomExtra.bookServiceShort\' | t }}</span>' +
                '</a>' +
                '<button type="button" class="btn-pill-cta" ' +
                        'ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'" ' +
                        'ng-click="$ctrl.bookSelectedModelTestDrive()">' +
                  '<span>{{ \'showroom.bookTestDriveCta\' | t }}</span>' +
                  '<span class="btn-pill-well" aria-hidden="true">' +
                    '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
                  '</span>' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* 7. Floating Bottom Compare Dock (Visible when 1..3 motorcycles selected) */
          '<div class="compare-dock fade-slide-up" role="region" ng-attr-aria-label="{{ \'showroomExtra.compareDockTitle\' | t }}" ng-if="$ctrl.compareModels.length > 0">' +
            '<div class="compare-dock-chips">' +
              '<span class="compare-dock-label">' +
                '<app-icon name="scales" weight="duotone" size="15"></app-icon>' +
                '<span>{{ \'showroomExtra.compareDockTitle\' | t }}</span>' +
              '</span>' +
              '<span class="compare-dock-chip" ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                '<img class="compare-dock-thumb" ng-src="{{ $ctrl.getCompareAngleUrl(cm) }}" alt="" width="34" height="24" />' +
                '<span>{{ cm.modelName }}</span>' +
                '<button type="button" class="compare-dock-remove" ' +
                        'ng-attr-aria-label="{{ \'showroomExtra.compareRemoveAria\' | t:{ model: cm.modelName } }}" ' +
                        'ng-click="$ctrl.removeFromCompare(cm.id)">' +
                  '<app-icon name="x" weight="regular" size="11"></app-icon>' +
                '</button>' +
              '</span>' +
            '</div>' +
            '<div style="display:flex;align-items:center;gap:8px;">' +
              '<button type="button" class="btn btn-ghost-dark btn-sm" ng-click="$ctrl.clearCompare()">{{ \'showroomExtra.compareClear\' | t }}</button>' +
              '<button type="button" class="btn-pill-cta" style="min-height:36px;padding:3px 4px 3px 14px;font-size:12px;" ng-click="$ctrl.openCompareModal()">' +
                '<span>{{ \'showroomExtra.compareNowCta\' | t:{ count: $ctrl.compareModels.length } }}</span>' +
                '<span class="btn-pill-well" style="width:26px;height:26px;" aria-hidden="true">' +
                  '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          /* 8. Side-by-Side Spec & Loyalty Comparison Modal with Synced 4-Angle Switcher */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="compareModalTitle" ng-if="$ctrl.compareModalOpen && $ctrl.compareModels.length > 0" ng-click="$ctrl.closeCompareModal()">' +
            '<div class="modal-dialog modal-dialog-wide modal-spring-in motion-modal" style="max-width:960px;" ng-click="$event.stopPropagation()">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ \'showroomExtra.compareModalEyebrow\' | t }}</span>' +
                  '<h2 id="compareModalTitle" class="surface-card-title" style="margin-top:4px;">{{ \'showroomExtra.compareModalTitle\' | t:{ count: $ctrl.compareModels.length } }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCompareModal()">' +
                  '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="modal-body">' +
                '<div class="surface-sunken" style="padding:12px 14px;margin-bottom:16px;display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
                  '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">' +
                    '<span class="text-secondary" style="font-size:12px;font-weight:700;">{{ \'showroomExtra.compareSyncAngleLabel\' | t }}</span>' +
                    '<button type="button" class="btn btn-sm" ' +
                            'ng-repeat="angDef in $ctrl.compareAngleDefs track by angDef.key" ' +
                            'ng-class="$ctrl.compareAngleIndex === $index ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'ng-click="$ctrl.setCompareAngleIndex($index)">' +
                      '{{ angDef.shortLabelKey | t }}' +
                    '</button>' +
                  '</div>' +
                  '<div style="display:flex;align-items:center;gap:6px;flex-wrap:wrap;">' +
                    '<span class="text-secondary" style="font-size:12px;font-weight:700;">{{ \'showroomExtra.compareTierSimLabel\' | t }}</span>' +
                    '<button type="button" class="btn btn-sm mono-num" ' +
                            'ng-repeat="tierOpt in $ctrl.compareTierOptions track by tierOpt.code" ' +
                            'ng-class="$ctrl.compareTierCode === tierOpt.code ? \'btn-primary\' : \'btn-secondary\'" ' +
                            'ng-click="$ctrl.setCompareTier(tierOpt)">' +
                      '{{ tierOpt.code | enumT:\'LoyaltyTierCode\' }} {{ tierOpt.mult | number:1 }}x' +
                    '</button>' +
                  '</div>' +
                '</div>' +

                '<div class="table-wrap">' +
                  '<table class="compare-table tabular-nums">' +
                    '<thead>' +
                      '<tr>' +
                        '<th scope="col" style="width:175px;">{{ \'showroomExtra.compareDockTitle\' | t }}</th>' +
                        '<th scope="col" ng-repeat="cm in $ctrl.compareModels track by cm.id" style="min-width:210px;vertical-align:top;">' +
                          '<div style="margin-bottom:8px;">' +
                            '<bike-image src="$ctrl.getCompareAngleUrl(cm)" alt="{{ cm.modelName }}" category="{{ cm.category }}" variant="card"></bike-image>' +
                          '</div>' +
                          '<div style="display:flex;justify-content:space-between;align-items:center;gap:6px;">' +
                            '<div>' +
                              '<div class="text-muted mono-num" style="font-size:11px;">{{ cm.brand }} • {{ cm.modelCode }}</div>' +
                              '<div class="text-primary" style="font-family:var(--font-display);font-size:15px;font-weight:700;">{{ cm.modelName }}</div>' +
                            '</div>' +
                            '<button type="button" class="btn btn-secondary btn-sm" ' +
                                    'ng-attr-aria-label="{{ \'showroomExtra.compareRemoveAria\' | t:{ model: cm.modelName } }}" ' +
                                    'ng-click="$ctrl.removeFromCompare(cm.id)">' +
                              '<app-icon name="x" weight="regular" size="13"></app-icon>' +
                            '</button>' +
                          '</div>' +
                        '</th>' +
                      '</tr>' +
                    '</thead>' +
                    '<tbody>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowCategory\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<span class="badge badge-info">{{ cm.category | enumT:\'MotorcycleCategory\' }}</span>' +
                        '</td>' +
                      '</tr>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowEngine\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<strong class="mono-num text-primary">{{ cm.engineDisplacementCc }} cc</strong>' +
                          '<div class="compare-cc-bar"><div class="compare-cc-fill" ng-style="{transform: \'scaleX(\' + $ctrl.getCompareCcRatio(cm) + \')\'}"></div></div>' +
                        '</td>' +
                      '</tr>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowMsrp\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<strong class="mono-num text-primary" style="font-size:16px;">{{ cm.baseRetailPrice | vndCurrency }}</strong>' +
                        '</td>' +
                      '</tr>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowLoyalty\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<span class="badge badge-success mono-num">+{{ $ctrl.estimatePointsWithMult(cm.baseRetailPrice, $ctrl.compareTierMult) | number }} {{ \'common.ptsShort\' | t }}</span>' +
                          '<div class="text-muted" style="font-size:11px;margin-top:3px;">{{ $ctrl.compareTierCode | enumT:\'LoyaltyTierCode\' }} ({{ $ctrl.compareTierMult | number:2 }}x)</div>' +
                        '</td>' +
                      '</tr>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowWarranty\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id" class="mono-num">' +
                          '{{ cm.warrantyMonths }} {{ \'showroomExtra.monthsShort\' | t }} / {{ cm.warrantyMileageKm | number }} km' +
                        '</td>' +
                      '</tr>' +
                      '<tr>' +
                        '<th scope="row">{{ \'showroomExtra.compareRowStock\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<span class="badge mono-num" ng-class="$ctrl.getModelStockCount(cm) > 0 ? \'badge-status-available\' : \'badge-neutral\'">' +
                            '{{ $ctrl.getModelStockCount(cm) }} VIN' +
                          '</span>' +
                        '</td>' +
                      '</tr>' +
                      '<tr ng-if="!$ctrl.isAuthenticated || $ctrl.userRole === \'Customer\'">' +
                        '<th scope="row">{{ \'showroomExtra.compareRowAction\' | t }}</th>' +
                        '<td ng-repeat="cm in $ctrl.compareModels track by cm.id">' +
                          '<button type="button" class="btn btn-primary btn-sm" ng-click="$ctrl.bookComparedModelTestDrive(cm)">' +
                            '<app-icon name="motorcycle" weight="duotone" size="14"></app-icon>' +
                            '<span>{{ \'showroom.bookTestDriveCta\' | t }}</span>' +
                          '</button>' +
                        '</td>' +
                      '</tr>' +
                    '</tbody>' +
                  '</table>' +
                '</div>' +
              '</div>' +

              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.clearCompare()">{{ \'showroomExtra.compareClear\' | t }}</button>' +
                '<button type="button" class="btn btn-primary" ng-click="$ctrl.closeCompareModal()">{{ \'common.close\' | t }}</button>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  registerShowroomI18n.$inject = ['I18nService'];
  function registerShowroomI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(SHOWROOM_I18N_VI, SHOWROOM_I18N_EN);
    }
  }

  PublicShowroomController.$inject = [
    '$q',
    '$rootScope',
    'AuthService',
    'DealerApiService',
    'LoyaltyCalculator',
    'ProblemDetailsMapper',
    'SignalRClientService',
    'TokenStorageService',
    '$state',
    'ToastService',
    '$window',
    '$element',
    'I18nService'
  ];
  function PublicShowroomController(
    $q,
    $rootScope,
    AuthService,
    DealerApiService,
    LoyaltyCalculator,
    ProblemDetailsMapper,
    SignalRClientService,
    TokenStorageService,
    $state,
    ToastService,
    $window,
    $element,
    I18nService
  ) {
    var $ctrl = this;
    var cleanupFns = [];
    var isDestroyed = false;
    var stockByDealerAndModel = Object.create(null);
    var stockTotalByModel = Object.create(null);
    var stockTotalByDealer = Object.create(null);
    var galleryCacheByKey = Object.create(null);
    var parallaxRafId = null;

    function resolveStudioSlug(model) {
      if (!model) {
        return null;
      }
      var code = String(model.modelCode || '').trim().toUpperCase();
      if (code && MODEL_CODE_TO_STUDIO_SLUG[code]) {
        return MODEL_CODE_TO_STUDIO_SLUG[code];
      }
      var rawUrl = String(model.imageUrl || '').trim();
      var match = rawUrl.match(/\/assets\/models\/([a-z0-9-]+)\.(?:jpe?g|webp|png)(?:\?.*)?$/i);
      if (match && BUNDLED_MULTI_ANGLE_SLUGS[match[1].toLowerCase()]) {
        return match[1].toLowerCase();
      }
      return null;
    }

    function buildModelGallery(model) {
      if (!model) {
        return [];
      }
      var slug = resolveStudioSlug(model);
      var cacheKey = slug || (String(model.id || '') + '::' + String(model.imageUrl || ''));
      if (galleryCacheByKey[cacheKey]) {
        return galleryCacheByKey[cacheKey];
      }
      var items;
      if (slug && BUNDLED_MULTI_ANGLE_SLUGS[slug]) {
        items = GALLERY_ANGLE_DEFS.map(function (def) {
          return {
            key: def.key,
            url: '/assets/models/' + slug + def.suffix + '.jpg',
            labelKey: def.labelKey,
            shortLabelKey: def.shortLabelKey,
            fallback: def.fallback
          };
        });
      } else {
        items = [{
          key: 'front',
          url: String(model.imageUrl || '').trim(),
          labelKey: 'showroomExtra.angleFront',
          shortLabelKey: 'showroomExtra.angleFrontShort',
          fallback: 'Front 3/4'
        }];
      }
      galleryCacheByKey[cacheKey] = items;
      return items;
    }

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(SHOWROOM_I18N_VI, SHOWROOM_I18N_EN);
    }

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback || key;
    }

    function enumT(group, code, fallback) {
      if (I18nService && angular.isFunction(I18nService.enumLabel)) {
        return I18nService.enumLabel(group, code);
      }
      return fallback || code;
    }

    $ctrl.getSpotlightModel = function () {
      if ($ctrl.filteredModels && $ctrl.filteredModels.length > 0) {
        return $ctrl.filteredModels[0];
      }
      if ($ctrl.models && $ctrl.models.length > 0) {
        return $ctrl.models[0];
      }
      return null;
    };

    function syncUserContext() {
      var user = AuthService.getCurrentUser();
      $ctrl.isAuthenticated = AuthService.isAuthenticated();
      $ctrl.userRole = ($ctrl.isAuthenticated && user && user.role) ? user.role : null;
      $ctrl.activeMultiplier = (user && Number(user.tierMultiplier)) || 1.0;
    }

    function rebuildLocalizedFilterOptions() {
      $ctrl.categoryOptions = [
        { value: '', label: t('showroom.filterAllCategories', null, 'All Categories') },
        { value: 'Scooter', label: enumT('MotorcycleCategory', 'Scooter', 'Scooter') },
        { value: 'Underbone', label: enumT('MotorcycleCategory', 'Underbone', 'Underbone') },
        { value: 'Sport', label: enumT('MotorcycleCategory', 'Sport', 'Sport') },
        { value: 'Naked', label: enumT('MotorcycleCategory', 'Naked', 'Naked') },
        { value: 'Adventure', label: enumT('MotorcycleCategory', 'Adventure', 'Adventure') },
        { value: 'Cruiser', label: enumT('MotorcycleCategory', 'Cruiser', 'Cruiser') },
        { value: 'Electric', label: enumT('MotorcycleCategory', 'Electric', 'Electric') }
      ];

      $ctrl.sortOptions = [
        { value: 'default', label: t('showroomExtra.sortDefault', null, 'Featured Order') },
        { value: 'priceAsc', label: t('showroom.sortPriceAsc', null, 'Price: Low to High') },
        { value: 'priceDesc', label: t('showroom.sortPriceDesc', null, 'Price: High to Low') },
        { value: 'ccDesc', label: t('showroom.sortCcDesc', null, 'Largest Displacement') }
      ];
    }

    function rebuildDealerSelectOptions() {
      var opts = [{ value: '', label: t('showroom.filterAllDealers', null, 'All Authorized Dealers') }];
      var cityOpts = [{ value: '', label: t('showroomExtra.allCities', null, 'All Cities'), labelKey: 'showroomExtra.allCities' }];
      var seenCities = Object.create(null);
      var list = $ctrl.dealers || [];
      for (var i = 0; i < list.length; i++) {
        opts.push({
          value: list[i].id,
          label: list[i].code + ' • ' + list[i].name
        });
        if (list[i].city && !seenCities[list[i].city]) {
          seenCities[list[i].city] = true;
          cityOpts.push({ value: list[i].city, label: list[i].city });
        }
      }
      $ctrl.dealerSelectOptions = opts;
      $ctrl.cityOptions = cityOpts;
    }

    function rebuildHeroStats() {
      var dealersList = $ctrl.dealers || [];
      var modelsList = $ctrl.models || [];
      var totalVins = 0;
      if ($ctrl.hasLiveInventoryIndex) {
        var dealerKeys = Object.keys(stockTotalByDealer);
        for (var i = 0; i < dealerKeys.length; i++) {
          totalVins += stockTotalByDealer[dealerKeys[i]] || 0;
        }
      } else {
        for (var j = 0; j < dealersList.length; j++) {
          totalVins += Number(dealersList[j].availableVinCount) || 0;
        }
        if (totalVins === 0) {
          for (var k = 0; k < modelsList.length; k++) {
            totalVins += Number(modelsList[k].availableStockCount) || 0;
          }
        }
      }
      $ctrl.heroStats = {
        totalModels: modelsList.length,
        totalDealers: dealersList.length,
        totalAvailableVins: totalVins
      };
    }

    function rebuildFilteredDealers() {
      var all = $ctrl.dealers || [];
      if (!$ctrl.selectedCity) {
        $ctrl.filteredDealers = all.slice();
        return;
      }
      var target = $ctrl.selectedCity.toLowerCase();
      $ctrl.filteredDealers = all.filter(function (d) {
        return String(d.city || '').toLowerCase() === target;
      });
    }

    function rebuildInventoryCounts(inventories) {
      stockByDealerAndModel = Object.create(null);
      stockTotalByModel = Object.create(null);
      stockTotalByDealer = Object.create(null);

      var rows = inventories || [];
      for (var i = 0; i < rows.length; i++) {
        var item = rows[i];
        if (item.stockStatus !== 'Available') {
          continue;
        }
        var mId = item.motorcycleModelId;
        var dId = item.dealerId;
        if (mId) {
          stockTotalByModel[mId] = (stockTotalByModel[mId] || 0) + 1;
        }
        if (dId) {
          stockTotalByDealer[dId] = (stockTotalByDealer[dId] || 0) + 1;
        }
        if (dId && mId) {
          var key = dId + '::' + mId;
          stockByDealerAndModel[key] = (stockByDealerAndModel[key] || 0) + 1;
        }
      }
    }

    function buildSpecEntries(model) {
      var specs = (model && model.specifications) || {};
      var keys = Object.keys(specs);
      var out = [];
      for (var i = 0; i < keys.length; i++) {
        out.push({ key: keys[i], value: String(specs[keys[i]]) });
      }
      return out;
    }

    function buildBranchStocksForModel(model) {
      if (model && angular.isArray(model.dealerStocks) && model.dealerStocks.length > 0) {
        return model.dealerStocks.map(function (ds) {
          return {
            dealerId: ds.dealerId,
            dealerCode: ds.dealerCode,
            dealerName: ds.dealerName,
            count: ds.availableCount
          };
        });
      }
      return ($ctrl.dealers || []).map(function (d) {
        var count = $ctrl.hasLiveInventoryIndex
          ? (stockByDealerAndModel[d.id + '::' + model.id] || 0)
          : 0;
        return {
          dealerId: d.id,
          dealerCode: d.code,
          dealerName: d.name,
          count: count
        };
      });
    }

    function setupHeroParallax() {
      try {
        if (!$window || !$element || !$element[0] || !angular.isFunction($element[0].querySelector)) {
          return;
        }
        var reducedMotion = angular.isFunction($window.matchMedia) &&
          $window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reducedMotion) {
          return;
        }
        var heroEl = $element[0].querySelector('.atelier-hero');
        var bgEl = $element[0].querySelector('.atelier-hero-bg');
        if (!heroEl || !bgEl || !angular.isFunction($window.addEventListener)) {
          return;
        }

        var pointerX = 0;
        var pointerY = 0;
        var scrollYOffset = 0;

        function scheduleTransform() {
          if (isDestroyed || parallaxRafId !== null) {
            return;
          }
          if (!angular.isFunction($window.requestAnimationFrame)) {
            return;
          }
          parallaxRafId = $window.requestAnimationFrame(function () {
            parallaxRafId = null;
            if (isDestroyed) {
              return;
            }
            if (($window.innerWidth || 0) < 768) {
              bgEl.style.transform = '';
              return;
            }
            bgEl.style.transform = 'translate3d(' + pointerX.toFixed(1) + 'px, ' + (scrollYOffset + pointerY).toFixed(1) + 'px, 0) scale(1.05)';
          });
        }

        function onScroll() {
          if (($window.innerWidth || 0) < 768) {
            scrollYOffset = 0;
            scheduleTransform();
            return;
          }
          var scrollTop = $window.scrollY || $window.pageYOffset || 0;
          scrollYOffset = Math.max(-18, Math.min(24, scrollTop * 0.08));
          scheduleTransform();
        }

        function onMouseMove(evt) {
          if (($window.innerWidth || 0) < 768 || !angular.isFunction(heroEl.getBoundingClientRect)) {
            return;
          }
          var rect = heroEl.getBoundingClientRect();
          if (!rect.width || !rect.height) {
            return;
          }
          var normX = ((evt.clientX - rect.left) / rect.width) - 0.5;
          var normY = ((evt.clientY - rect.top) / rect.height) - 0.5;
          pointerX = Math.max(-10, Math.min(10, normX * -14));
          pointerY = Math.max(-8, Math.min(8, normY * -10));
          scheduleTransform();
        }

        function onMouseLeave() {
          pointerX = 0;
          pointerY = 0;
          scheduleTransform();
        }

        function onResize() {
          if (($window.innerWidth || 0) < 768) {
            scrollYOffset = 0;
            pointerX = 0;
            pointerY = 0;
            bgEl.style.transform = '';
          }
        }

        $window.addEventListener('scroll', onScroll, { passive: true });
        $window.addEventListener('resize', onResize, { passive: true });
        heroEl.addEventListener('mousemove', onMouseMove, { passive: true });
        heroEl.addEventListener('mouseleave', onMouseLeave, { passive: true });

        cleanupFns.push(function () {
          if (parallaxRafId !== null && angular.isFunction($window.cancelAnimationFrame)) {
            $window.cancelAnimationFrame(parallaxRafId);
            parallaxRafId = null;
          }
          $window.removeEventListener('scroll', onScroll);
          $window.removeEventListener('resize', onResize);
          heroEl.removeEventListener('mousemove', onMouseMove);
          heroEl.removeEventListener('mouseleave', onMouseLeave);
        });
      } catch (e) {
        // Guarded for Node VM sandbox
      }
    }

    $ctrl.$onInit = function () {
      syncUserContext();
      $ctrl.isLoading = false;
      $ctrl.isLoadingRules = false;
      $ctrl.hasLiveInventoryIndex = false;
      $ctrl.catalogProblem = null;
      $ctrl.selectedCity = '';
      $ctrl.selectedModel = null;
      $ctrl.selectedModelRules = [];
      $ctrl.selectedModelSpecEntries = [];
      $ctrl.selectedModelBranchStocks = [];
      $ctrl.selectedModelGallery = [];
      $ctrl.selectedGalleryIndex = 0;
      $ctrl.modalMediaMode = 'photos';
      $ctrl.cardAngleIndexByModelId = Object.create(null);
      $ctrl.compareModels = [];
      $ctrl.compareModalOpen = false;
      $ctrl.compareAngleIndex = 0;
      $ctrl.compareAngleDefs = GALLERY_ANGLE_DEFS;
      $ctrl.compareTierOptions = [
        { code: 'Bronze', mult: 1.0 },
        { code: 'Silver', mult: 1.2 },
        { code: 'Gold', mult: 1.5 },
        { code: 'Platinum', mult: 2.0 }
      ];
      $ctrl.compareTierCode = ($ctrl.activeMultiplier >= 2.0) ? 'Platinum' : ($ctrl.activeMultiplier >= 1.5 ? 'Gold' : ($ctrl.activeMultiplier >= 1.2 ? 'Silver' : 'Bronze'));
      $ctrl.compareTierMult = $ctrl.activeMultiplier || 1.0;
      $ctrl.sortBy = 'default';
      $ctrl.heroStats = {
        totalModels: 0,
        totalDealers: 0,
        totalAvailableVins: 0
      };

      rebuildLocalizedFilterOptions();

      $ctrl.filters = {
        search: '',
        category: '',
        dealerId: '',
        inStockOnly: false
      };

      $ctrl.dealers = [];
      $ctrl.models = [];
      rebuildDealerSelectOptions();
      rebuildFilteredDealers();
      rebuildHeroStats();
      $ctrl.applyCatalogFilters();

      cleanupFns.push($rootScope.$on('auth:changed', function () {
        syncUserContext();
      }));

      cleanupFns.push($rootScope.$on('i18n:changed', function () {
        rebuildLocalizedFilterOptions();
        rebuildDealerSelectOptions();
      }));

      cleanupFns.push(SignalRClientService.on('TierUpgraded', function (payload) {
        if (payload && payload.newPointsMultiplier !== undefined) {
          $ctrl.activeMultiplier = Number(payload.newPointsMultiplier) || $ctrl.activeMultiplier;
        }
      }));

      $ctrl.loadShowroomData();
    };

    $ctrl.$postLink = function () {
      setupHeroParallax();
      if ($window && angular.isFunction($window.addEventListener)) {
        var onGalleryKeyDown = function (ev) {
          if (isDestroyed || (!$ctrl.selectedModel && !$ctrl.compareModalOpen) || !ev || ev.defaultPrevented) {
            return;
          }
          if (ev.altKey || ev.ctrlKey || ev.metaKey) {
            return;
          }
          var tag = ev.target && ev.target.tagName ? String(ev.target.tagName).toUpperCase() : '';
          if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || (ev.target && ev.target.isContentEditable)) {
            return;
          }
          if ($ctrl.compareModalOpen) {
            if (ev.key === 'Escape') {
              $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.closeCompareModal(); } });
            } else if (ev.key === 'ArrowRight') {
              ev.preventDefault();
              $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.setCompareAngleIndex(($ctrl.compareAngleIndex + 1) % GALLERY_ANGLE_DEFS.length); } });
            } else if (ev.key === 'ArrowLeft') {
              ev.preventDefault();
              $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.setCompareAngleIndex(($ctrl.compareAngleIndex - 1 + GALLERY_ANGLE_DEFS.length) % GALLERY_ANGLE_DEFS.length); } });
            }
            return;
          }
          if ($ctrl.modalMediaMode !== '3d' && ev.key === 'ArrowRight' && ($ctrl.selectedModelGallery || []).length > 1) {
            ev.preventDefault();
            $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.stepModalGallery(1); } });
          } else if ($ctrl.modalMediaMode !== '3d' && ev.key === 'ArrowLeft' && ($ctrl.selectedModelGallery || []).length > 1) {
            ev.preventDefault();
            $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.stepModalGallery(-1); } });
          } else if (ev.key === 'Escape') {
            $rootScope.$applyAsync(function () { if (!isDestroyed) { $ctrl.closeScheduleModal(); } });
          }
        };
        $window.addEventListener('keydown', onGalleryKeyDown);
        cleanupFns.push(function () {
          $window.removeEventListener('keydown', onGalleryKeyDown);
        });
      }
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

    $ctrl.loadShowroomData = function () {
      $ctrl.isLoading = true;
      $ctrl.catalogProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        dealers: DealerApiService.listDealers().catch(function (err) {
          if (!$ctrl.catalogProblem) {
            $ctrl.catalogProblem = ProblemDetailsMapper.extract(err);
            $ctrl.loadProblem = $ctrl.catalogProblem;
          }
          return null;
        }),
        models: DealerApiService.listMotorcycles({ pageSize: 50 }).catch(function (err) {
          var prob = ProblemDetailsMapper.extract(err);
          $ctrl.catalogProblem = prob;
          $ctrl.loadProblem = prob;
          return null;
        }),
        inventories: DealerApiService.listInventories({ publicOnly: true, pageSize: 100 }).catch(function () { return null; })
      }).then(function (bundle) {
        if (isDestroyed) {
          return;
        }
        if (angular.isArray(bundle.dealers)) {
          $ctrl.dealers = bundle.dealers.filter(function (d) { return d.isActive; });
          rebuildDealerSelectOptions();
          rebuildFilteredDealers();
        }
        if (angular.isArray(bundle.inventories)) {
          $ctrl.hasLiveInventoryIndex = true;
          rebuildInventoryCounts(bundle.inventories);
        }
        if (angular.isArray(bundle.models)) {
          $ctrl.models = bundle.models.filter(function (m) { return m.isActive; });
        }
        rebuildHeroStats();
        $ctrl.applyCatalogFilters();
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.getDealerStockCount = function (dealer) {
      if (!dealer) {
        return 0;
      }
      if ($ctrl.hasLiveInventoryIndex) {
        return stockTotalByDealer[dealer.id] || 0;
      }
      return dealer.availableVinCount || 0;
    };

    $ctrl.getModelStockCount = function (model) {
      if (!model) {
        return 0;
      }
      var selectedDealerId = ($ctrl.filters && $ctrl.filters.dealerId) || '';
      if (selectedDealerId) {
        if (angular.isArray(model.dealerStocks) && model.dealerStocks.length > 0) {
          for (var i = 0; i < model.dealerStocks.length; i++) {
            if (model.dealerStocks[i].dealerId === selectedDealerId) {
              return model.dealerStocks[i].availableCount || 0;
            }
          }
        }
        if ($ctrl.hasLiveInventoryIndex) {
          return stockByDealerAndModel[selectedDealerId + '::' + model.id] || 0;
        }
      }
      if ($ctrl.hasLiveInventoryIndex && stockTotalByModel[model.id] !== undefined) {
        return stockTotalByModel[model.id];
      }
      return model.availableStockCount || 0;
    };

    $ctrl.applyCatalogFilters = function () {
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      var cat = ($ctrl.filters && $ctrl.filters.category) || '';
      var inStockOnly = Boolean($ctrl.filters && $ctrl.filters.inStockOnly);

      var matched = ($ctrl.models || []).filter(function (m) {
        if (cat && m.category !== cat) {
          return false;
        }
        if (inStockOnly && $ctrl.getModelStockCount(m) <= 0) {
          return false;
        }
        if (q) {
          var hay = (m.brand + ' ' + m.modelName + ' ' + m.modelCode + ' ' + m.category).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });

      if ($ctrl.sortBy === 'priceAsc') {
        matched.sort(function (a, b) { return (Number(a.baseRetailPrice) || 0) - (Number(b.baseRetailPrice) || 0); });
      } else if ($ctrl.sortBy === 'priceDesc') {
        matched.sort(function (a, b) { return (Number(b.baseRetailPrice) || 0) - (Number(a.baseRetailPrice) || 0); });
      } else if ($ctrl.sortBy === 'ccDesc') {
        matched.sort(function (a, b) { return (Number(b.engineDisplacementCc) || 0) - (Number(a.engineDisplacementCc) || 0); });
      }

      $ctrl.filteredModels = matched;
    };

    $ctrl.filterByDealer = function (dealerId) {
      $ctrl.filters.dealerId = $ctrl.filters.dealerId === dealerId ? '' : dealerId;
      $ctrl.applyCatalogFilters();
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = {
        search: '',
        category: '',
        dealerId: '',
        inStockOnly: false
      };
      $ctrl.sortBy = 'default';
      $ctrl.applyCatalogFilters();
    };

    $ctrl.selectCity = function (cityValue) {
      $ctrl.selectedCity = cityValue || '';
      rebuildFilteredDealers();
    };

    $ctrl.estimatePoints = function (priceVnd) {
      return LoyaltyCalculator.calculatePoints(priceVnd, $ctrl.activeMultiplier);
    };

    $ctrl.estimatePointsWithMult = function (priceVnd, mult) {
      return LoyaltyCalculator.calculatePoints(priceVnd, mult);
    };

    $ctrl.isInCompare = function (model) {
      if (!model || !model.id) {
        return false;
      }
      for (var i = 0; i < $ctrl.compareModels.length; i++) {
        if ($ctrl.compareModels[i].id === model.id) {
          return true;
        }
      }
      return false;
    };

    $ctrl.toggleCompare = function (model) {
      if (!model || !model.id) {
        return;
      }
      if ($ctrl.isInCompare(model)) {
        $ctrl.removeFromCompare(model.id);
        return;
      }
      if ($ctrl.compareModels.length >= 3) {
        if (ToastService && angular.isFunction(ToastService.info)) {
          ToastService.info(
            t('showroomExtra.compareMaxToastTitle', null, 'Maximum 3 Motorcycles'),
            t('showroomExtra.compareMaxToastMsg', null, 'Remove one motorcycle from the compare dock before adding another.')
          );
        }
        return;
      }
      $ctrl.compareModels = $ctrl.compareModels.concat([model]);
    };

    $ctrl.removeFromCompare = function (modelId) {
      $ctrl.compareModels = ($ctrl.compareModels || []).filter(function (m) {
        return m.id !== modelId;
      });
      if ($ctrl.compareModels.length === 0) {
        $ctrl.compareModalOpen = false;
      }
    };

    $ctrl.clearCompare = function () {
      $ctrl.compareModels = [];
      $ctrl.compareModalOpen = false;
    };

    $ctrl.openCompareModal = function () {
      if ($ctrl.compareModels.length > 0) {
        $ctrl.compareModalOpen = true;
      }
    };

    $ctrl.closeCompareModal = function () {
      $ctrl.compareModalOpen = false;
    };

    $ctrl.setCompareAngleIndex = function (idx) {
      $ctrl.compareAngleIndex = Math.max(0, Math.min(GALLERY_ANGLE_DEFS.length - 1, Number(idx) || 0));
    };

    $ctrl.setCompareTier = function (tierOpt) {
      if (!tierOpt) {
        return;
      }
      $ctrl.compareTierCode = tierOpt.code;
      $ctrl.compareTierMult = tierOpt.mult;
    };

    $ctrl.getCompareAngleUrl = function (model) {
      var gallery = buildModelGallery(model);
      var item = gallery[$ctrl.compareAngleIndex] || gallery[0];
      return (item && item.url) || (model && model.imageUrl) || '/assets/editorial/showroom-hero-apex.jpg';
    };

    $ctrl.getCompareCcRatio = function (model) {
      var maxCc = 1;
      for (var i = 0; i < $ctrl.compareModels.length; i++) {
        var cc = Number($ctrl.compareModels[i].engineDisplacementCc) || 0;
        if (cc > maxCc) {
          maxCc = cc;
        }
      }
      return Math.max(0.12, Math.min(1, (Number(model && model.engineDisplacementCc) || 0) / maxCc));
    };

    $ctrl.bookComparedModelTestDrive = function (model) {
      $ctrl.closeCompareModal();
      $ctrl.bookTestDrive(model);
    };

    $ctrl.getModelGallery = function (model) {
      return buildModelGallery(model);
    };

    $ctrl.getCardAngleIndex = function (model) {
      if (!model || !model.id) {
        return 0;
      }
      var gallery = buildModelGallery(model);
      var idx = Number($ctrl.cardAngleIndexByModelId[model.id]) || 0;
      if (idx < 0 || idx >= gallery.length) {
        return 0;
      }
      return idx;
    };

    $ctrl.selectCardAngle = function (model, index) {
      if (!model || !model.id) {
        return;
      }
      var gallery = buildModelGallery(model);
      if (index >= 0 && index < gallery.length) {
        $ctrl.cardAngleIndexByModelId[model.id] = index;
      }
    };

    $ctrl.getCardActiveImageUrl = function (model) {
      var gallery = buildModelGallery(model);
      var idx = $ctrl.getCardAngleIndex(model);
      return gallery[idx] ? gallery[idx].url : (model && model.imageUrl) || '';
    };

    $ctrl.getCardActiveAngleLabel = function (model) {
      var gallery = buildModelGallery(model);
      var idx = $ctrl.getCardAngleIndex(model);
      var item = gallery[idx];
      return item ? t(item.labelKey, null, item.fallback) : '';
    };

    $ctrl.selectModalGalleryIndex = function (index) {
      var total = ($ctrl.selectedModelGallery || []).length;
      if (total === 0) {
        $ctrl.selectedGalleryIndex = 0;
        return;
      }
      $ctrl.selectedGalleryIndex = ((index % total) + total) % total;
    };

    $ctrl.stepModalGallery = function (delta) {
      $ctrl.selectModalGalleryIndex(($ctrl.selectedGalleryIndex || 0) + (Number(delta) || 0));
    };

    $ctrl.getSelectedModalAngleUrl = function () {
      var item = ($ctrl.selectedModelGallery || [])[$ctrl.selectedGalleryIndex || 0];
      return item ? item.url : ($ctrl.selectedModel && $ctrl.selectedModel.imageUrl) || '';
    };

    $ctrl.getSelectedModalAngleLabel = function () {
      var item = ($ctrl.selectedModelGallery || [])[$ctrl.selectedGalleryIndex || 0];
      return item ? t(item.labelKey, null, item.fallback) : '';
    };

    $ctrl.setModalMediaMode = function (mode) {
      $ctrl.modalMediaMode = mode === '3d' ? '3d' : 'photos';
    };

    $ctrl.openScheduleModal = function (model, initialAngleIndex, initialMode) {
      if (!model) {
        return;
      }
      $ctrl.selectedModel = model;
      $ctrl.modalMediaMode = initialMode === '3d' ? '3d' : 'photos';
      $ctrl.selectedModelGallery = buildModelGallery(model);
      $ctrl.selectedGalleryIndex = (typeof initialAngleIndex === 'number' && initialAngleIndex >= 0 && initialAngleIndex < $ctrl.selectedModelGallery.length)
        ? initialAngleIndex
        : $ctrl.getCardAngleIndex(model);
      $ctrl.selectedModelRules = angular.isArray(model.maintenanceRules) ? model.maintenanceRules.slice() : [];
      $ctrl.selectedModelSpecEntries = buildSpecEntries(model);
      $ctrl.selectedModelBranchStocks = buildBranchStocksForModel(model);

      $ctrl.isLoadingRules = true;
      $q.all({
        detail: DealerApiService.getMotorcycleById(model.id).catch(function () { return null; }),
        rules: DealerApiService.listMaintenanceRules(model.id).catch(function () { return null; })
      }).then(function (res) {
        if (isDestroyed || !$ctrl.selectedModel || $ctrl.selectedModel.id !== model.id) {
          return;
        }
        if (res.detail) {
          $ctrl.selectedModel = res.detail;
          $ctrl.selectedModelGallery = buildModelGallery(res.detail);
          if ($ctrl.selectedGalleryIndex >= $ctrl.selectedModelGallery.length) {
            $ctrl.selectedGalleryIndex = 0;
          }
          $ctrl.selectedModelSpecEntries = buildSpecEntries(res.detail);
          $ctrl.selectedModelBranchStocks = buildBranchStocksForModel(res.detail);
        }
        if (angular.isArray(res.rules)) {
          $ctrl.selectedModelRules = res.rules;
        } else if (res.detail && angular.isArray(res.detail.maintenanceRules)) {
          $ctrl.selectedModelRules = res.detail.maintenanceRules;
        }
      }).finally(function () {
        if (!isDestroyed && $ctrl.selectedModel && $ctrl.selectedModel.id === model.id) {
          $ctrl.isLoadingRules = false;
        }
      });
    };

    $ctrl.closeScheduleModal = function () {
      $ctrl.selectedModel = null;
      $ctrl.selectedModelGallery = [];
      $ctrl.selectedGalleryIndex = 0;
      $ctrl.modalMediaMode = 'photos';
      $ctrl.selectedModelRules = [];
    };

    $ctrl.bookSelectedModelTestDrive = function () {
      var target = $ctrl.selectedModel;
      $ctrl.closeScheduleModal();
      $ctrl.bookTestDrive(target);
    };

    $ctrl.bookTestDrive = function (model) {
      var targetModel = model || ($ctrl.filteredModels && $ctrl.filteredModels[0]) || ($ctrl.models && $ctrl.models[0]) || null;
      if (!targetModel) {
        return;
      }
      var selectedDealerId = ($ctrl.filters && $ctrl.filters.dealerId) || '';
      if (DealerApiService && angular.isFunction(DealerApiService.setPendingTestDriveContext)) {
        DealerApiService.setPendingTestDriveContext({
          modelId: targetModel.id,
          dealerId: selectedDealerId || ''
        });
      }

      if (!$ctrl.isAuthenticated) {
        var returnUrl = '/customer/test-drives?modelId=' + encodeURIComponent(targetModel.id) +
          (selectedDealerId ? ('&dealerId=' + encodeURIComponent(selectedDealerId)) : '');
        if ($state && angular.isFunction($state.go)) {
          $state.go('app.public.login', {
            returnUrl: returnUrl,
            returnTo: 'app.customer.testDrives'
          });
        }
        return;
      }

      if ($ctrl.userRole === 'Customer') {
        if ($state && angular.isFunction($state.go)) {
          $state.go('app.customer.testDrives', {
            modelId: targetModel.id,
            dealerId: selectedDealerId || undefined
          });
        }
        return;
      }

      if ($ctrl.userRole === 'DealerStaff') {
        if (ToastService && angular.isFunction(ToastService.info)) {
          ToastService.info(
            t('showroomExtra.staffToastTitle', null, 'Staff Test-Drive Desk'),
            t('showroomExtra.staffToastMsg', null, 'Opening branch test-drive queue.')
          );
        }
        if ($state && angular.isFunction($state.go)) {
          $state.go('app.staff.testDrives');
        }
        return;
      }

      if (ToastService && angular.isFunction(ToastService.info)) {
        ToastService.info(
          t('showroomExtra.customerOnlyToastTitle', null, 'Customer Portal Action'),
          t('showroomExtra.customerOnlyToastMsg', null, 'Sign in with a Customer account to schedule a showroom test drive.')
        );
      }
    };
  }
})();
