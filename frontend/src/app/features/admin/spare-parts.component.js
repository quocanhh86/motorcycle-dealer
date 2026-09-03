(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      spareParts: {
        screenCode: 'SCR-ADM-07 · v1.1 Studio Phụ tùng OEM',
        backToAnalytics: 'Phân tích HQ',
        refreshing: 'Đang tải lại...',
        addPartCta: 'Thêm phụ tùng OEM',
        kpiTotalSkus: 'Tổng mã SKU phụ tùng',
        kpiActiveSkus: 'Đang kinh doanh',
        kpiUniversalSkus: 'Phụ tùng dùng chung',
        searchLabel: 'Tìm mã SKU, tên phụ tùng hoặc dòng xe',
        searchPlaceholder: 'Tìm mã SKU phụ tùng, tên phụ tùng hoặc mẫu xe tương thích...',
        categoryLabel: 'Nhóm phụ tùng',
        modelFitmentLabel: 'Độ tương thích mẫu xe',
        allModelsOption: 'Tất cả độ tương thích',
        universalOnlyOption: 'Chỉ phụ tùng dùng chung',
        universalAllOption: 'Dùng chung (Mọi dòng xe)',
        universalFitBadge: 'Dùng chung OEM',
        clearBtn: 'Xóa lọc',
        colCode: 'Mã SKU',
        colName: 'Tên phụ tùng OEM',
        colCategory: 'Nhóm phụ tùng',
        colFitment: 'Tương thích xe',
        colUnit: 'Đơn vị',
        colUnitPrice: 'Đơn giá',
        emptyTitle: 'Không tìm thấy phụ tùng OEM phù hợp',
        emptyDesc: 'Hãy thử xóa bộ lọc hoặc thêm mã phụ tùng chính hãng mới vào danh mục.',
        modalEditTitle: 'Chỉnh sửa phụ tùng OEM: {{code}}',
        modalCreateTitle: 'Thêm phụ tùng chính hãng OEM mới',
        fieldCode: 'Mã SKU phụ tùng * (3–32 ký tự)',
        fieldCodePlaceholder: 'Nhập mã SKU duy nhất',
        fieldName: 'Tên phụ tùng chính hãng *',
        fieldNamePlaceholder: 'Nhập tên phụ tùng OEM',
        fieldCategory: 'Nhóm phụ tùng *',
        fieldCompatModel: 'Mẫu xe tương thích',
        fieldUnit: 'Đơn vị tính *',
        fieldUnitPlaceholder: 'Cái, Bộ, Chai, Cặp...',
        fieldUnitPrice: 'Đơn giá bán lẻ (VNĐ) *',
        fieldActiveCheck: 'Mở bán trong bộ chọn phụ tùng của Hóa đơn xưởng (ServiceInvoices)',
        createPartSubmitCta: 'Tạo phụ tùng OEM',
        toastUpdatedTitle: 'Đã cập nhật phụ tùng OEM',
        toastCreatedTitle: 'Đã tạo phụ tùng OEM mới',
        toastSavedBody: '{{code}} · {{name}} đã được lưu.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      spareParts: {
        screenCode: 'SCR-ADM-07 · v1.1 OEM SpareParts Studio',
        backToAnalytics: 'Analytics',
        refreshing: 'Refreshing...',
        addPartCta: 'Add OEM Spare Part',
        kpiTotalSkus: 'Total OEM Part SKUs',
        kpiActiveSkus: 'Active Catalog SKUs',
        kpiUniversalSkus: 'Universal Fit SKUs',
        searchLabel: 'Search Part SKU, Name, or Model',
        searchPlaceholder: 'Search by part SKU code, part name, or compatible model...',
        categoryLabel: 'Category',
        modelFitmentLabel: 'Model Compatibility',
        allModelsOption: 'All Model Fitments',
        universalOnlyOption: 'Universal Fit Only',
        universalAllOption: 'Universal Fit (All Motorcycle Models)',
        universalFitBadge: 'Universal Fit',
        clearBtn: 'Clear',
        colCode: 'Part Code',
        colName: 'OEM Part Name',
        colCategory: 'Category',
        colFitment: 'Model Fitment',
        colUnit: 'Unit',
        colUnitPrice: 'Unit Price',
        emptyTitle: 'No OEM spare parts match your filter criteria',
        emptyDesc: 'Try clearing active filters or add a new genuine OEM spare part to the catalog.',
        modalEditTitle: 'Edit OEM Spare Part: {{code}}',
        modalCreateTitle: 'Add New OEM Spare Part',
        fieldCode: 'Part SKU Code * (3–32 chars)',
        fieldCodePlaceholder: 'Enter unique part SKU code',
        fieldName: 'OEM Part Name *',
        fieldNamePlaceholder: 'Enter OEM spare part name',
        fieldCategory: 'Part Category *',
        fieldCompatModel: 'Model Compatibility',
        fieldUnit: 'Unit Label *',
        fieldUnitPlaceholder: 'Enter unit label (Piece, Set, Bottle)',
        fieldUnitPrice: 'Unit Retail Price (VND) *',
        fieldActiveCheck: 'Active in Workshop ServiceInvoice OEM Parts Picker',
        createPartSubmitCta: 'Create OEM Part',
        toastUpdatedTitle: 'OEM Part Updated',
        toastCreatedTitle: 'OEM Part Created',
        toastSavedBody: '{{code}} · {{name}} saved.'
      }
    }
  };

  registerSparePartsI18n.$inject = ['I18nService'];
  function registerSparePartsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerSparePartsI18n)
    .component('adminSpareParts', {
      controllerAs: '$ctrl',
      controller: AdminSparePartsController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.spareParts.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.sparePartsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.sparePartsSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.spareParts.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadCatalog()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.spareParts.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.SpareParts.ManageCatalog" ' +
                      'ng-click="$ctrl.openCreateModal()">' +
                '<span>{{ \'admin.spareParts.addPartCta\' | t }}</span>' +
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
                  '<div class="kpi-label">{{ \'admin.spareParts.kpiTotalSkus\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalSkus" kpi-format="number">{{ $ctrl.stats.totalSkus }}</div>' +
                '</div>' +
                '<app-icon name="package" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.spareParts.kpiActiveSkus\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.activeSkus" kpi-format="number">{{ $ctrl.stats.activeSkus }}</div>' +
                '</div>' +
                '<app-icon name="check-circle" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.spareParts.kpiUniversalSkus\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.universalSkus" kpi-format="number">{{ $ctrl.stats.universalSkus }}</div>' +
                '</div>' +
                '<app-icon name="wrench" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Toolbar */
          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admPartSearch">{{ \'admin.spareParts.searchLabel\' | t }}</label>' +
                  '<input id="admPartSearch" type="search" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'admin.spareParts.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admPartCategory">{{ \'admin.spareParts.categoryLabel\' | t }}</label>' +
                  '<select id="admPartCategory" class="form-control" ' +
                          'ng-model="$ctrl.filters.category" ' +
                          'ng-options="c.value as c.label for c in $ctrl.categoryFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admPartModelFilter">{{ \'admin.spareParts.modelFitmentLabel\' | t }}</label>' +
                  '<select id="admPartModelFilter" class="form-control" ' +
                          'ng-model="$ctrl.filters.modelFilter" ' +
                          'ng-options="m.value as m.label for m in $ctrl.modelFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">{{ \'admin.spareParts.clearBtn\' | t }}</button>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="5" ng-if="$ctrl.isLoading && $ctrl.parts.length === 0"></skeleton-loader>' +

          '<div class="bezel-card quiet-table-frame" ng-if="!$ctrl.isLoading || $ctrl.parts.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredParts.length === 0" ' +
                           'icon="wrench" ' +
                           'title-key="admin.spareParts.emptyTitle" ' +
                           'desc-key="admin.spareParts.emptyDesc" ' +
                           'action-key="admin.spareParts.clearBtn" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredParts.length > 0" style="max-height:560px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.spareParts.colCode\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.spareParts.colName\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.spareParts.colCategory\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.spareParts.colFitment\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.spareParts.colUnit\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;text-align:right;">{{ \'admin.spareParts.colUnitPrice\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.actions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="part in $ctrl.filteredParts track by part.id">' +
                      '<td class="mono-num" style="font-weight:700;">{{ part.partCode }}</td>' +
                      '<td style="font-weight:600;" class="text-primary">{{ part.name }}</td>' +
                      '<td><span class="badge badge-neutral">{{ part.category | enumT:\'SparePartCategory\' }}</span></td>' +
                      '<td>' +
                        '<span class="badge" ng-class="part.compatibleModelId ? \'badge-info\' : \'badge-neutral\'">' +
                          '{{ part.compatibleModelName || $ctrl.resolveModelLabel(part.compatibleModelId) || (\'admin.spareParts.universalFitBadge\' | t) }}' +
                        '</span>' +
                      '</td>' +
                      '<td class="mono-num">{{ part.unitLabel }}</td>' +
                      '<td class="mono-num" style="text-align:right;font-weight:700;">{{ part.unitPriceVnd | vndCurrency }}</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="part.isActive ? \'badge-success\' : \'badge-neutral\'">' +
                          '<app-icon name="{{ part.isActive ? \'check-circle\' : \'minus-circle\' }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ (part.isActive ? \'common.active\' : \'common.inactive\') | t }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td>' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'has-permission="Permissions.SpareParts.ManageCatalog" ' +
                                'ng-click="$ctrl.openEditModal(part)">' +
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

          /* Create / Edit OEM SparePart Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="sparePartModalTitle" ng-if="$ctrl.modalOpen">' +
            '<div class="modal-dialog modal-dialog-wide motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">' +
                    '{{ $ctrl.editingPartId ? (\'PUT /api/v1/spare-parts/\' + $ctrl.editingPartId) : \'POST /api/v1/spare-parts\' }}' +
                  '</span>' +
                  '<h2 id="sparePartModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ $ctrl.editingPartId ? (\'admin.spareParts.modalEditTitle\' | t:{ code: $ctrl.draft.partCode }) : (\'admin.spareParts.modalCreateTitle\' | t) }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.partForm" ng-submit="$ctrl.saveSparePart()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.modalProblem">' +
                    '<div><strong>{{ $ctrl.modalProblem.title }} ({{ $ctrl.modalProblem.errorCode }}):</strong> {{ $ctrl.modalProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.modalProblem.traceId">traceId: {{ $ctrl.modalProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="spCodeInput">{{ \'admin.spareParts.fieldCode\' | t }}</label>' +
                      '<input id="spCodeInput" name="partCode" type="text" minlength="3" maxlength="32" ' +
                             'class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.partCode" ng-pattern="/^[A-Za-z0-9_-]{3,32}$/" ' +
                             'ng-change="$ctrl.onFieldEdited(\'partCode\')" required ' +
                             'ng-attr-placeholder="{{ \'admin.spareParts.fieldCodePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.partCode">{{ $ctrl.fieldErrors.partCode }}</div>' +
                    '</div>' +

                    '<div class="col-8 form-group">' +
                      '<label class="form-label" for="spNameInput">{{ \'admin.spareParts.fieldName\' | t }}</label>' +
                      '<input id="spNameInput" name="name" type="text" maxlength="150" class="form-control" ' +
                             'ng-model="$ctrl.draft.name" ng-change="$ctrl.onFieldEdited(\'name\')" required ' +
                             'ng-attr-placeholder="{{ \'admin.spareParts.fieldNamePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.name">{{ $ctrl.fieldErrors.name }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="spCatSelect">{{ \'admin.spareParts.fieldCategory\' | t }}</label>' +
                      '<select id="spCatSelect" name="category" class="form-control" ' +
                              'ng-model="$ctrl.draft.category" ' +
                              'ng-options="c.value as c.label for c in $ctrl.categories" required></select>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="spCompatSelect">{{ \'admin.spareParts.fieldCompatModel\' | t }}</label>' +
                      '<select id="spCompatSelect" name="compatibleModelId" class="form-control" ' +
                              'ng-model="$ctrl.draft.compatibleModelId" ' +
                              'ng-options="m.value as m.label for m in $ctrl.modelCompatOptions"></select>' +
                    '</div>' +

                    '<div class="col-5 form-group">' +
                      '<label class="form-label" for="spUnitInput">{{ \'admin.spareParts.fieldUnit\' | t }}</label>' +
                      '<input id="spUnitInput" name="unitLabel" type="text" maxlength="24" class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.unitLabel" ng-change="$ctrl.onFieldEdited(\'unitLabel\')" required ' +
                             'ng-attr-placeholder="{{ \'admin.spareParts.fieldUnitPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.unitLabel">{{ $ctrl.fieldErrors.unitLabel }}</div>' +
                    '</div>' +

                    '<div class="col-7 form-group">' +
                      '<label class="form-label" for="spPriceInput">{{ \'admin.spareParts.fieldUnitPrice\' | t }}</label>' +
                      '<input id="spPriceInput" name="unitPriceVnd" type="number" min="1" step="any" ' +
                             'class="form-control mono-num" ' +
                             'ng-model="$ctrl.draft.unitPriceVnd" ng-change="$ctrl.onFieldEdited(\'unitPriceVnd\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.unitPriceVnd">{{ $ctrl.fieldErrors.unitPriceVnd }}</div>' +
                    '</div>' +

                    '<div class="col-12 form-group" style="margin-bottom:0;">' +
                      '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;">' +
                        '<input type="checkbox" ng-model="$ctrl.draft.isActive" />' +
                        '<span>{{ \'admin.spareParts.fieldActiveCheck\' | t }}</span>' +
                      '</label>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeModal()" ng-disabled="$ctrl.isSaving">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" id="saveSparePartBtn" class="btn btn-primary" ' +
                          'has-permission="Permissions.SpareParts.ManageCatalog" ' +
                          'ng-disabled="$ctrl.isSaving || $ctrl.partForm.$invalid">' +
                    '{{ ($ctrl.isSaving ? \'common.saving\' : ($ctrl.editingPartId ? \'common.save\' : \'admin.spareParts.createPartSubmitCta\')) | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminSparePartsController.$inject = [
    '$q',
    'DealerApiService',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    '$scope',
    'I18nService'
  ];
  function AdminSparePartsController(
    $q,
    DealerApiService,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    $scope,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var offLocale = null;
    var modelNameById = Object.create(null);

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

    function rebuildCategoryDropdowns() {
      var rawCats = (DealerApiService && angular.isFunction(DealerApiService.getSparePartCategories))
        ? DealerApiService.getSparePartCategories()
        : [];
      $ctrl.categories = rawCats.map(function (c) {
        return {
          value: c.value,
          label: enumLabel('SparePartCategory', c.value)
        };
      });
      $ctrl.categoryFilterOptions = [{ value: '', label: t('showroom.filterAllCategories', 'All Categories') }].concat(
        $ctrl.categories.map(function (c) { return angular.extend({}, c); })
      );
    }

    function rebuildModelDropdowns() {
      modelNameById = Object.create(null);
      var filterOpts = [
        { value: '', label: t('admin.spareParts.allModelsOption', 'All Model Fitments') },
        { value: 'UNIVERSAL', label: t('admin.spareParts.universalOnlyOption', 'Universal Fit Only') }
      ];
      var compatOpts = [
        { value: '', label: t('admin.spareParts.universalAllOption', 'Universal Fit (All Motorcycle Models)') }
      ];

      var list = $ctrl.models || [];
      for (var i = 0; i < list.length; i++) {
        var m = list[i];
        var label = (m.modelCode ? (m.modelCode + ' · ') : '') + m.modelName;
        modelNameById[m.id] = m.modelName;
        filterOpts.push({ value: m.id, label: label });
        compatOpts.push({ value: m.id, label: label });
      }
      $ctrl.modelFilterOptions = filterOpts;
      $ctrl.modelCompatOptions = compatOpts;
    }

    function recomputeStats() {
      var list = $ctrl.parts || [];
      var active = 0;
      var universal = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].isActive !== false) {
          active++;
        }
        if (!list[i].compatibleModelId) {
          universal++;
        }
      }
      $ctrl.stats = {
        totalSkus: list.length,
        activeSkus: active,
        universalSkus: universal
      };
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isSaving = false;
      $ctrl.listProblem = null;

      rebuildCategoryDropdowns();

      $ctrl.filters = {
        search: '',
        category: '',
        modelFilter: ''
      };

      $ctrl.modalOpen = false;
      $ctrl.editingPartId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {};

      $ctrl.loadProblem = null;
      $ctrl.models = [];
      $ctrl.parts = [];

      recomputeStats();
      rebuildModelDropdowns();
      $ctrl.applyFilters();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildCategoryDropdowns();
            rebuildModelDropdowns();
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

    $ctrl.resolveModelLabel = function (modelId) {
      if (!modelId) { return null; }
      return modelNameById[modelId] || null;
    };

    $ctrl.loadCatalog = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return $q.all({
        parts: DealerApiService.getSpareParts({ includeInactive: true, includeUniversal: true }),
        models: DealerApiService.listMotorcycles({ includeInactive: true, pageSize: 100 }).catch(function () { return null; })
      }).then(function (bundle) {
        if (isDestroyed) { return bundle.parts; }
        if (angular.isArray(bundle.models) && bundle.models.length > 0) {
          $ctrl.models = bundle.models;
          rebuildModelDropdowns();
        }
        $ctrl.parts = bundle.parts || [];
        recomputeStats();
        $ctrl.applyFilters();
        return $ctrl.parts;
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var prob = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = prob;
          $ctrl.loadProblem = prob;
        }
        return $ctrl.parts;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.applyFilters = function () {
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      var cat = ($ctrl.filters && $ctrl.filters.category) || '';
      var mFilter = ($ctrl.filters && $ctrl.filters.modelFilter) || '';

      $ctrl.filteredParts = ($ctrl.parts || []).filter(function (p) {
        if (cat && p.category !== cat) {
          return false;
        }
        if (mFilter === 'UNIVERSAL' && p.compatibleModelId) {
          return false;
        }
        if (mFilter && mFilter !== 'UNIVERSAL' && p.compatibleModelId !== mFilter) {
          return false;
        }
        if (q) {
          var fitment = p.compatibleModelName || modelNameById[p.compatibleModelId] || 'universal fit';
          var hay = (p.partCode + ' ' + p.name + ' ' + p.category + ' ' + fitment).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      $ctrl.filters = { search: '', category: '', modelFilter: '' };
      $ctrl.applyFilters();
    };

    $ctrl.openCreateModal = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.partForm);
      $ctrl.editingPartId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {
        partCode: 'PRT-',
        name: '',
        category: 'EngineOil',
        compatibleModelId: '',
        unitLabel: 'Piece',
        unitPriceVnd: 185000,
        isActive: true
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.openEditModal = function (part) {
      if (!part) { return; }
      ProblemDetailsMapper.clearServerErrors($ctrl.partForm);
      $ctrl.editingPartId = part.id;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.draft = {
        partCode: part.partCode,
        name: part.name,
        category: part.category,
        compatibleModelId: part.compatibleModelId || '',
        unitLabel: part.unitLabel,
        unitPriceVnd: part.unitPriceVnd,
        isActive: part.isActive !== false
      };
      $ctrl.modalOpen = true;
    };

    $ctrl.closeModal = function () {
      $ctrl.modalOpen = false;
      $ctrl.editingPartId = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
    };

    $ctrl.onFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.partForm, $ctrl.fieldErrors, fieldName);
    };

    $ctrl.saveSparePart = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.partForm);
      if (($ctrl.partForm && $ctrl.partForm.$invalid) || $ctrl.isSaving) {
        return $q.resolve(null);
      }

      $ctrl.isSaving = true;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};

      var payload = {
        partCode: String($ctrl.draft.partCode || '').trim().toUpperCase(),
        name: String($ctrl.draft.name || '').trim(),
        category: $ctrl.draft.category,
        compatibleModelId: $ctrl.draft.compatibleModelId ? String($ctrl.draft.compatibleModelId).trim() : null,
        unitLabel: String($ctrl.draft.unitLabel || 'Unit').trim(),
        unitPriceVnd: Math.max(0, Number($ctrl.draft.unitPriceVnd) || 0),
        isActive: Boolean($ctrl.draft.isActive)
      };

      var req = $ctrl.editingPartId
        ? DealerApiService.updateSparePart($ctrl.editingPartId, payload)
        : DealerApiService.createSparePart(payload);

      return req
        .then(function (saved) {
          if (isDestroyed) { return saved; }
          ToastService.success(
            $ctrl.editingPartId
              ? t('admin.spareParts.toastUpdatedTitle', 'OEM Part Updated')
              : t('admin.spareParts.toastCreatedTitle', 'OEM Part Created'),
            t('admin.spareParts.toastSavedBody', '{{code}} · {{name}} saved.', {
              code: saved.partCode,
              name: saved.name
            })
          );
          $ctrl.closeModal();
          return $ctrl.loadCatalog().then(function () {
            return saved;
          });
        })
        .catch(function (rejection) {
          if (isDestroyed) { return null; }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.partForm, problem);
          $ctrl.modalProblem = problem;
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSaving = false;
          }
        });
    };
  }
})();
