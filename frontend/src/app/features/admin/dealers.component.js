(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      dealers: {
        screenCode: 'SCR-ADM-02 · FR-01 / FR-02',
        backToAnalytics: 'Phân tích HQ',
        refreshing: 'Đang tải lại...',
        createDealerCta: 'Tạo đại lý mới',
        focusedBanner: 'Đang lọc theo chi nhánh {{code}} từ Bảng Phân tích HQ.',
        showAllBranches: 'Hiển thị tất cả chi nhánh',
        kpiTotalBranches: 'Tổng số chi nhánh',
        kpiActiveBranches: 'Chi nhánh đang hoạt động',
        kpiHourlyCapacity: 'Tổng công suất khoang / giờ',
        searchLabel: 'Tìm theo mã, tên chi nhánh hoặc địa chỉ',
        searchPlaceholder: 'Tìm mã chi nhánh, tên đại lý, thành phố hoặc địa chỉ...',
        cityLabel: 'Thành phố',
        allCities: 'Tất cả thành phố',
        statusLabel: 'Trạng thái chi nhánh',
        statusAll: 'Tất cả chi nhánh',
        statusActiveOnly: 'Chỉ đang hoạt động',
        statusInactiveOnly: 'Chỉ tạm ngưng',
        colCode: 'Mã đại lý',
        colNameAddress: 'Tên chi nhánh & Địa chỉ',
        colCity: 'Thành phố',
        colHours: 'Giờ mở xưởng',
        colBays: 'Khoang / giờ',
        actionStaff: 'Nhân sự',
        emptyTitle: 'Không có đại lý phù hợp bộ lọc',
        emptyDesc: 'Hãy xóa bộ lọc tìm kiếm hoặc tạo chi nhánh đại lý ủy quyền mới.',
        modalEditTitle: 'Chỉnh sửa Chi nhánh Đại lý',
        modalCreateTitle: 'Tạo Chi nhánh Đại lý Ủy quyền',
        fieldCode: 'Mã chi nhánh *',
        fieldCodePlaceholder: 'Nhập mã chi nhánh duy nhất (VD: DLR-CT-01)',
        fieldName: 'Tên đại lý *',
        fieldNamePlaceholder: 'Nhập tên đại lý ủy quyền',
        fieldCity: 'Thành phố *',
        fieldCityPlaceholder: 'Nhập tên thành phố',
        fieldPhone: 'Số điện thoại *',
        fieldPhonePlaceholder: 'Nhập số hotline chi nhánh',
        fieldEmail: 'Email liên hệ *',
        fieldEmailPlaceholder: 'Nhập email liên hệ chi nhánh',
        fieldAddress: 'Địa chỉ chi nhánh *',
        fieldAddressPlaceholder: 'Nhập địa chỉ đường phố đầy đủ',
        fieldOpenTime: 'Giờ mở cửa (HH:mm) *',
        fieldCloseTime: 'Giờ đóng cửa (HH:mm) *',
        hoursInvalid: 'Giờ đóng cửa phải lớn hơn giờ mở cửa.',
        fieldCapacity: 'Sức chứa khoang (1–50) *',
        fieldActiveCheck: 'Kích hoạt đại lý trên Showroom công khai & Bộ chọn khung giờ xưởng',
        saveBranchCta: 'Lưu thay đổi chi nhánh',
        createBranchCta: 'Tạo đại lý mới',
        toastUpdated: 'Đã cập nhật đại lý',
        toastCreated: 'Đã tạo đại lý mới',
        toastSavedBody: '{{code}} · {{name}} đã được lưu.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      dealers: {
        screenCode: 'SCR-ADM-02 · FR-01 / FR-02',
        backToAnalytics: 'Analytics',
        refreshing: 'Refreshing...',
        createDealerCta: 'Create New Dealership',
        focusedBanner: 'Focused on branch {{code}} from Executive Analytics.',
        showAllBranches: 'Show All Branches',
        kpiTotalBranches: 'Total Branches',
        kpiActiveBranches: 'Active Branches',
        kpiHourlyCapacity: 'Total Bays / Hour',
        searchLabel: 'Search Code, Branch Name, or Address',
        searchPlaceholder: 'Search by branch code, dealership name, city, or street address...',
        cityLabel: 'City',
        allCities: 'All Cities',
        statusLabel: 'Branch Status',
        statusAll: 'All Branches',
        statusActiveOnly: 'Active Only',
        statusInactiveOnly: 'Inactive Only',
        colCode: 'Dealer Code',
        colNameAddress: 'Branch Name & Address',
        colCity: 'City',
        colHours: 'Workshop Hours',
        colBays: 'Bays / hr',
        actionStaff: 'Staff',
        emptyTitle: 'No dealerships match your active filters',
        emptyDesc: 'Try clearing the search or status filters, or create a new authorized dealership branch.',
        modalEditTitle: 'Edit Dealership Branch',
        modalCreateTitle: 'Create Authorized Dealership',
        fieldCode: 'Branch Code *',
        fieldCodePlaceholder: 'Enter unique branch code',
        fieldName: 'Dealership Name *',
        fieldNamePlaceholder: 'Enter authorized dealership name',
        fieldCity: 'City *',
        fieldCityPlaceholder: 'Enter city',
        fieldPhone: 'Phone Number *',
        fieldPhonePlaceholder: 'Enter branch phone number',
        fieldEmail: 'Contact Email *',
        fieldEmailPlaceholder: 'Enter branch contact email',
        fieldAddress: 'Street Address *',
        fieldAddressPlaceholder: 'Enter branch street address',
        fieldOpenTime: 'Opening Time (HH:mm) *',
        fieldCloseTime: 'Closing Time (HH:mm) *',
        hoursInvalid: 'Closing time must be after opening time.',
        fieldCapacity: 'Slot Capacity (1–50) *',
        fieldActiveCheck: 'Dealership Active in Public Showroom & Workshop Slot Picker',
        saveBranchCta: 'Save Branch Changes',
        createBranchCta: 'Create Dealership',
        toastUpdated: 'Dealership Updated',
        toastCreated: 'Dealership Created',
        toastSavedBody: '{{code}} · {{name}} saved.'
      }
    }
  };

  registerDealersI18n.$inject = ['I18nService'];
  function registerDealersI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerDealersI18n)
    .component('adminDealers', {
      controllerAs: '$ctrl',
      controller: AdminDealersController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.dealers.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.dealersTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.dealersSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.dealers.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadDealers()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.dealers.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Dealers.ManageAll" ' +
                      'ng-click="$ctrl.openCreateModal()">' +
                '<span>{{ \'admin.dealers.createDealerCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="plus" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<div class="alert-banner alert-info" role="status" ng-if="$ctrl.focusedDealerBanner">' +
            '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;">' +
              '<span style="display:inline-flex;align-items:center;gap:8px;">' +
                '<app-icon name="map-pin" weight="duotone" size="16"></app-icon>' +
                '<span>{{ \'admin.dealers.focusedBanner\' | t:{ code: $ctrl.focusedDealerBanner } }}</span>' +
              '</span>' +
              '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.resetFilters()">{{ \'admin.dealers.showAllBranches\' | t }}</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.loadDealers()">' +
          '</error-retry-banner>' +

          /* Double-Bezel KPI Summary Strip */
          '<div class="bento-grid" style="margin-bottom:20px;">' +
            '<div class="col-4 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.dealers.kpiTotalBranches\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalBranches" kpi-format="number">{{ $ctrl.stats.totalBranches }}</div>' +
                '</div>' +
                '<app-icon name="buildings" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.dealers.kpiActiveBranches\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.activeBranches" kpi-format="number">{{ $ctrl.stats.activeBranches }}</div>' +
                '</div>' +
                '<app-icon name="check-circle" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.dealers.kpiHourlyCapacity\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalBaysPerHour" kpi-format="number">{{ $ctrl.stats.totalBaysPerHour }}</div>' +
                '</div>' +
                '<app-icon name="wrench" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12">' +
                '<div class="col-6 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="dealerSearchInput">{{ \'admin.dealers.searchLabel\' | t }}</label>' +
                  '<input id="dealerSearchInput" type="text" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'admin.dealers.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="dealerCityFilter">{{ \'admin.dealers.cityLabel\' | t }}</label>' +
                  '<select id="dealerCityFilter" class="form-control" ' +
                          'ng-model="$ctrl.filters.city" ' +
                          'ng-options="c.value as c.label for c in $ctrl.cityFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="dealerStatusFilter">{{ \'admin.dealers.statusLabel\' | t }}</label>' +
                  '<select id="dealerStatusFilter" class="form-control" ' +
                          'ng-model="$ctrl.filters.status" ' +
                          'ng-options="s.value as s.label for s in $ctrl.statusOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="4" ng-if="$ctrl.isLoading && $ctrl.dealers.length === 0"></skeleton-loader>' +

          '<div class="bezel-card quiet-table-frame" ng-if="!$ctrl.isLoading || $ctrl.dealers.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredDealers.length === 0" ' +
                           'icon="buildings" ' +
                           'title-key="admin.dealers.emptyTitle" ' +
                           'desc-key="admin.dealers.emptyDesc" ' +
                           'action-key="common.clearFilters" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredDealers.length > 0" style="max-height:540px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.dealers.colCode\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.dealers.colNameAddress\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.dealers.colCity\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.dealers.colHours\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.dealers.colBays\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.actions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="d in $ctrl.filteredDealers track by d.id">' +
                      '<td class="mono-num" style="font-weight:700;">{{ d.code }}</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">{{ d.name }}</div>' +
                        '<div class="text-muted" style="font-size:12px;">{{ d.address }}</div>' +
                      '</td>' +
                      '<td>{{ d.city }}</td>' +
                      '<td class="mono-num">{{ d.openingTime }} - {{ d.closingTime }}</td>' +
                      '<td class="mono-num">{{ d.defaultSlotCapacity }}</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="d.isActive ? \'badge-success\' : \'badge-neutral\'">' +
                          '<app-icon name="{{ d.isActive ? \'check-circle\' : \'minus-circle\' }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ (d.isActive ? \'common.active\' : \'common.inactive\') | t }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td>' +
                        '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.Dealers.ManageAll" ' +
                                  'ng-click="$ctrl.openEditModal(d)">' +
                            '<app-icon name="pencil-simple" weight="regular" size="13"></app-icon>' +
                            '<span>{{ \'common.edit\' | t }}</span>' +
                          '</button>' +
                          '<a class="btn btn-secondary btn-sm" ' +
                             'has-permission="Permissions.Users.ManageStaff" ' +
                             'ui-sref="app.admin.staff({ dealerId: d.id })">' +
                            '<span>{{ \'admin.dealers.actionStaff\' | t }}</span>' +
                            '<app-icon name="arrow-right" weight="regular" size="12"></app-icon>' +
                          '</a>' +
                        '</div>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="dealerModalTitle" ng-if="$ctrl.modalOpen">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ $ctrl.editingDealerId ? (\'PUT /api/v1/dealers/\' + $ctrl.editingDealerId) : \'POST /api/v1/dealers\' }}</span>' +
                  '<h2 id="dealerModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ ($ctrl.editingDealerId ? \'admin.dealers.modalEditTitle\' : \'admin.dealers.modalCreateTitle\') | t }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.dealerForm" ng-submit="$ctrl.saveDealer()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.modalProblem">' +
                    '<div><strong>{{ $ctrl.modalProblem.title }}:</strong> {{ $ctrl.modalProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.modalProblem.traceId">' +
                      'traceId: {{ $ctrl.modalProblem.traceId }}' +
                    '</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrCode">{{ \'admin.dealers.fieldCode\' | t }}</label>' +
                      '<input id="dlrCode" name="code" type="text" maxlength="20" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.code" ng-change="$ctrl.onFieldEdited(\'code\')" ' +
                             'ng-pattern="/^DLR-[A-Z0-9-]{2,16}$/" ' +
                             'ng-readonly="Boolean($ctrl.editingDealerId)" required ' +
                             'ng-attr-placeholder="{{ \'admin.dealers.fieldCodePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.code">{{ $ctrl.fieldErrors.code }}</div>' +
                    '</div>' +

                    '<div class="col-8 form-group">' +
                      '<label class="form-label" for="dlrName">{{ \'admin.dealers.fieldName\' | t }}</label>' +
                      '<input id="dlrName" name="name" type="text" maxlength="150" class="form-control" ' +
                             'ng-model="$ctrl.form.name" ng-change="$ctrl.onFieldEdited(\'name\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.dealers.fieldNamePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.name">{{ $ctrl.fieldErrors.name }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrCity">{{ \'admin.dealers.fieldCity\' | t }}</label>' +
                      '<input id="dlrCity" name="city" type="text" maxlength="80" class="form-control" ' +
                             'ng-model="$ctrl.form.city" ng-change="$ctrl.onFieldEdited(\'city\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.dealers.fieldCityPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.city">{{ $ctrl.fieldErrors.city }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrPhone">{{ \'admin.dealers.fieldPhone\' | t }}</label>' +
                      '<input id="dlrPhone" name="phoneNumber" type="tel" maxlength="20" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.phoneNumber" ng-change="$ctrl.onFieldEdited(\'phoneNumber\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.dealers.fieldPhonePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.phoneNumber">{{ $ctrl.fieldErrors.phoneNumber }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrEmail">{{ \'admin.dealers.fieldEmail\' | t }}</label>' +
                      '<input id="dlrEmail" name="contactEmail" type="email" maxlength="256" class="form-control" ' +
                             'ng-model="$ctrl.form.contactEmail" ng-change="$ctrl.onFieldEdited(\'contactEmail\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.dealers.fieldEmailPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.contactEmail">{{ $ctrl.fieldErrors.contactEmail }}</div>' +
                    '</div>' +

                    '<div class="col-12 form-group">' +
                      '<label class="form-label" for="dlrAddress">{{ \'admin.dealers.fieldAddress\' | t }}</label>' +
                      '<input id="dlrAddress" name="address" type="text" maxlength="300" class="form-control" ' +
                             'ng-model="$ctrl.form.address" ng-change="$ctrl.onFieldEdited(\'address\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.dealers.fieldAddressPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.address">{{ $ctrl.fieldErrors.address }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrOpen">{{ \'admin.dealers.fieldOpenTime\' | t }}</label>' +
                      '<input id="dlrOpen" name="openingTime" type="text" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.openingTime" ng-change="$ctrl.onFieldEdited(\'openingTime\')" ' +
                             'ng-pattern="/^([01][0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$/" ' +
                             'required placeholder="08:00" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.openingTime">{{ $ctrl.fieldErrors.openingTime }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrClose">{{ \'admin.dealers.fieldCloseTime\' | t }}</label>' +
                      '<input id="dlrClose" name="closingTime" type="text" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.closingTime" ng-change="$ctrl.onFieldEdited(\'closingTime\')" ' +
                             'ng-pattern="/^([01][0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$/" ' +
                             'required placeholder="18:00" />' +
                      '<div class="field-error" ng-if="!$ctrl.isHoursRangeValid()">{{ \'admin.dealers.hoursInvalid\' | t }}</div>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.closingTime">{{ $ctrl.fieldErrors.closingTime }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="dlrBays">{{ \'admin.dealers.fieldCapacity\' | t }}</label>' +
                      '<input id="dlrBays" name="defaultSlotCapacity" type="number" min="1" max="50" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.defaultSlotCapacity" ng-change="$ctrl.onFieldEdited(\'defaultSlotCapacity\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.defaultSlotCapacity">{{ $ctrl.fieldErrors.defaultSlotCapacity }}</div>' +
                    '</div>' +

                    '<div class="col-12 form-group" style="margin-bottom:0;">' +
                      '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;">' +
                        '<input type="checkbox" ng-model="$ctrl.form.isActive" />' +
                        '<span>{{ \'admin.dealers.fieldActiveCheck\' | t }}</span>' +
                      '</label>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeModal()" ng-disabled="$ctrl.isSaving">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'has-permission="Permissions.Dealers.ManageAll" ' +
                          'ng-disabled="$ctrl.isSaving || $ctrl.dealerForm.$invalid || !$ctrl.isHoursRangeValid()">' +
                    '{{ ($ctrl.isSaving ? \'common.saving\' : ($ctrl.editingDealerId ? \'admin.dealers.saveBranchCta\' : \'admin.dealers.createBranchCta\')) | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminDealersController.$inject = [
    '$http',
    'APP_CONFIG',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    '$location',
    'DealerApiService',
    '$scope',
    'I18nService'
  ];
  function AdminDealersController(
    $http,
    APP_CONFIG,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    $location,
    DealerApiService,
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

    function toHmDisplay(value, fallback) {
      if (!angular.isString(value) || !value.trim()) {
        return fallback;
      }
      return value.trim().slice(0, 5);
    }

    function toApiTime(value, fallback) {
      var raw = angular.isString(value) && value.trim() ? value.trim() : fallback;
      if (/^[0-9]{2}:[0-9]{2}$/.test(raw)) {
        return raw + ':00';
      }
      return raw;
    }

    function normalizeDealer(d) {
      return {
        id: d.id || d.Id || d.code,
        code: String(d.code || d.Code || '').toUpperCase(),
        name: d.name || d.Name || '',
        city: d.city || d.City || '',
        address: d.address || d.Address || '',
        phoneNumber: d.phoneNumber || d.PhoneNumber || '',
        contactEmail: d.contactEmail || d.ContactEmail || '',
        latitude: d.latitude !== undefined ? d.latitude : (d.Latitude !== undefined ? d.Latitude : null),
        longitude: d.longitude !== undefined ? d.longitude : (d.Longitude !== undefined ? d.Longitude : null),
        openingTime: toHmDisplay(d.openingTime || d.OpeningTime, '08:00'),
        closingTime: toHmDisplay(d.closingTime || d.ClosingTime, '18:00'),
        defaultSlotCapacity: Number(d.defaultSlotCapacity || d.DefaultSlotCapacity) || 4,
        isActive: d.isActive !== undefined ? Boolean(d.isActive) : (d.IsActive !== undefined ? Boolean(d.IsActive) : true)
      };
    }

    function rebuildStatusOptions() {
      $ctrl.statusOptions = [
        { value: 'ALL', label: t('admin.dealers.statusAll', 'All Branches') },
        { value: 'ACTIVE', label: t('admin.dealers.statusActiveOnly', 'Active Only') },
        { value: 'INACTIVE', label: t('admin.dealers.statusInactiveOnly', 'Inactive Only') }
      ];
    }

    function rebuildCityFilterOptions() {
      var seen = Object.create(null);
      var opts = [{ value: '', label: t('admin.dealers.allCities', 'All Cities') }];
      for (var i = 0; i < ($ctrl.dealers || []).length; i++) {
        var city = $ctrl.dealers[i].city;
        if (city && !seen[city]) {
          seen[city] = true;
          opts.push({ value: city, label: city });
        }
      }
      $ctrl.cityFilterOptions = opts;
    }

    function recomputeStats() {
      var list = $ctrl.dealers || [];
      var active = 0;
      var bays = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].isActive) {
          active++;
          bays += Number(list[i].defaultSlotCapacity) || 0;
        }
      }
      $ctrl.stats = {
        totalBranches: list.length,
        activeBranches: active,
        totalBaysPerHour: bays
      };
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isSaving = false;
      $ctrl.modalOpen = false;
      $ctrl.editingDealerId = null;
      $ctrl.listProblem = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};

      rebuildStatusOptions();

      var searchQuery = ($location && angular.isFunction($location.search)) ? ($location.search() || {}) : {};
      var handoff = (DealerApiService && angular.isFunction(DealerApiService.consumeAdminFocusContext))
        ? DealerApiService.consumeAdminFocusContext()
        : null;
      var initialDealerFocus = String(
        searchQuery.dealerCode ||
        searchQuery.search ||
        (handoff && handoff.dealerCode) ||
        ''
      ).trim();

      $ctrl.focusedDealerBanner = initialDealerFocus || null;
      $ctrl.filters = {
        search: initialDealerFocus,
        city: searchQuery.city ? String(searchQuery.city).trim() : '',
        status: 'ALL'
      };

      $ctrl.loadProblem = null;
      $ctrl.dealers = [];
      recomputeStats();
      rebuildCityFilterOptions();
      $ctrl.applyFilters();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildStatusOptions();
            rebuildCityFilterOptions();
          }
        });
      }

      $ctrl.loadDealers();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (angular.isFunction(offLocale)) {
        offLocale();
        offLocale = null;
      }
    };

    $ctrl.loadDealers = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $http.get(APP_CONFIG.apiBaseUrl + '/dealers', {
        params: { includeInactive: true }
      })
        .then(function (response) {
          if (isDestroyed) {
            return;
          }
          var rows = ProblemDetailsMapper.unwrapList(response);
          $ctrl.dealers = rows.map(normalizeDealer);
          recomputeStats();
          rebuildCityFilterOptions();
          $ctrl.applyFilters();
        })
        .catch(function (rejection) {
          if (isDestroyed) {
            return;
          }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = problem;
          $ctrl.loadProblem = problem;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.applyFilters = function () {
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      var city = ($ctrl.filters && $ctrl.filters.city) || '';
      var status = ($ctrl.filters && $ctrl.filters.status) || 'ALL';

      $ctrl.filteredDealers = ($ctrl.dealers || []).filter(function (d) {
        if (city && d.city !== city) {
          return false;
        }
        if (status === 'ACTIVE' && !d.isActive) {
          return false;
        }
        if (status === 'INACTIVE' && d.isActive) {
          return false;
        }
        if (q) {
          var haystack = (d.code + ' ' + d.name + ' ' + d.address + ' ' + d.city).toLowerCase();
          if (haystack.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      $ctrl.focusedDealerBanner = null;
      if ($location && angular.isFunction($location.search)) {
        $location.search('dealerCode', null);
        $location.search('search', null);
        $location.search('city', null);
      }
      $ctrl.filters = { search: '', city: '', status: 'ALL' };
      $ctrl.applyFilters();
    };

    $ctrl.onFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.dealerForm, $ctrl.fieldErrors, fieldName);
    };

    $ctrl.isHoursRangeValid = function () {
      if (!$ctrl.form) {
        return true;
      }
      var openHm = toHmDisplay($ctrl.form.openingTime, '');
      var closeHm = toHmDisplay($ctrl.form.closingTime, '');
      if (!/^[0-9]{2}:[0-9]{2}$/.test(openHm) || !/^[0-9]{2}:[0-9]{2}$/.test(closeHm)) {
        return true;
      }
      return closeHm > openHm;
    };

    $ctrl.openCreateModal = function () {
      $ctrl.editingDealerId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.form = {
        code: 'DLR-',
        name: '',
        city: '',
        address: '',
        phoneNumber: '',
        contactEmail: '',
        latitude: null,
        longitude: null,
        openingTime: '08:00',
        closingTime: '18:00',
        defaultSlotCapacity: 4,
        isActive: true
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.openEditModal = function (dealer) {
      if (!dealer) {
        return;
      }
      $ctrl.editingDealerId = dealer.id;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.form = {
        code: dealer.code,
        name: dealer.name,
        city: dealer.city,
        address: dealer.address,
        phoneNumber: dealer.phoneNumber,
        contactEmail: dealer.contactEmail || '',
        latitude: dealer.latitude,
        longitude: dealer.longitude,
        openingTime: dealer.openingTime,
        closingTime: dealer.closingTime,
        defaultSlotCapacity: dealer.defaultSlotCapacity,
        isActive: dealer.isActive
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.closeModal = function () {
      $ctrl.modalOpen = false;
      $ctrl.editingDealerId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
    };

    $ctrl.saveDealer = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.dealerForm);
      if (($ctrl.dealerForm && $ctrl.dealerForm.$invalid) || !$ctrl.isHoursRangeValid()) {
        return;
      }

      var payload = {
        code: String($ctrl.form.code || '').trim().toUpperCase(),
        name: String($ctrl.form.name || '').trim(),
        city: String($ctrl.form.city || '').trim(),
        address: String($ctrl.form.address || '').trim(),
        phoneNumber: String($ctrl.form.phoneNumber || '').trim(),
        contactEmail: String($ctrl.form.contactEmail || '').trim(),
        latitude: $ctrl.form.latitude === '' || $ctrl.form.latitude === null || $ctrl.form.latitude === undefined
          ? null
          : Number($ctrl.form.latitude),
        longitude: $ctrl.form.longitude === '' || $ctrl.form.longitude === null || $ctrl.form.longitude === undefined
          ? null
          : Number($ctrl.form.longitude),
        openingTime: toApiTime($ctrl.form.openingTime, '08:00:00'),
        closingTime: toApiTime($ctrl.form.closingTime, '18:00:00'),
        defaultSlotCapacity: Number($ctrl.form.defaultSlotCapacity) || 4,
        isActive: Boolean($ctrl.form.isActive)
      };

      $ctrl.isSaving = true;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};

      var requestPromise = $ctrl.editingDealerId
        ? $http.put(APP_CONFIG.apiBaseUrl + '/dealers/' + encodeURIComponent($ctrl.editingDealerId), payload)
        : $http.post(APP_CONFIG.apiBaseUrl + '/dealers', payload);

      requestPromise
        .then(function () {
          ToastService.success(
            $ctrl.editingDealerId
              ? t('admin.dealers.toastUpdated', 'Dealership Updated')
              : t('admin.dealers.toastCreated', 'Dealership Created'),
            t('admin.dealers.toastSavedBody', '{{code}} · {{name}} saved.', {
              code: payload.code,
              name: payload.name
            })
          );
          $ctrl.closeModal();
          $ctrl.loadDealers();
        })
        .catch(function (rejection) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.dealerForm, problem);
          $ctrl.modalProblem = problem;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSaving = false;
          }
        });
    };
  }
})();
