(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      models: {
        screenCode: 'SCR-ADM-04 · FR-02 / FR-03 / FR-04',
        backToAnalytics: 'Phân tích HQ',
        refreshing: 'Đang tải lại...',
        addModelCta: 'Thêm mẫu xe mới',
        kpiTotalModels: 'Tổng mẫu xe danh mục',
        kpiActiveModels: 'Mẫu xe đang mở bán',
        kpiTotalRules: 'Mốc bảo dưỡng OEM',
        searchLabel: 'Tìm theo mã SKU, thương hiệu hoặc tên mẫu xe',
        searchPlaceholder: 'Tìm mã SKU mẫu xe, hãng sản xuất hoặc tên xe...',
        categoryLabel: 'Phân khúc xe',
        clearBtn: 'Xóa lọc',
        colThumb: 'Ảnh Studio',
        colCode: 'Mã SKU',
        colBrandModel: 'Thương hiệu & Mẫu xe',
        colCategory: 'Phân khúc',
        colEngine: 'Động cơ',
        colMsrp: 'Giá niêm yết',
        colRulesActions: 'Quy chuẩn OEM & Thao tác',
        warrantyShort: 'Bảo hành {{months}} tháng / {{km}} km',
        oemRulesBtn: 'Quy chuẩn OEM ({{count}})',
        emptyTitle: 'Không tìm thấy mẫu xe phù hợp',
        emptyDesc: 'Thử xóa từ khóa hoặc bộ lọc phân khúc, hoặc tạo mẫu xe mới cho Showroom.',
        modalEditTitle: 'Chỉnh sửa mẫu xe danh mục',
        modalCreateTitle: 'Tạo mẫu xe danh mục mới',
        fieldCode: 'Mã SKU mẫu xe *',
        fieldCodePlaceholder: 'Nhập mã SKU duy nhất (3-30 ký tự)',
        fieldBrand: 'Thương hiệu *',
        fieldBrandPlaceholder: 'Nhập hãng sản xuất',
        fieldCategory: 'Phân khúc *',
        fieldName: 'Tên đầy đủ mẫu xe *',
        fieldNamePlaceholder: 'Nhập tên mẫu xe máy',
        fieldCc: 'Dung tích xi-lanh (cc) *',
        fieldPrice: 'Giá bán lẻ đề xuất (VNĐ) *',
        fieldWarrantyMo: 'Bảo hành (tháng) *',
        fieldWarrantyKm: 'Bảo hành (km) *',
        fieldImageUrl: 'Đường dẫn ảnh Studio (ImageUrl — tối đa 500 ký tự)',
        fieldImageUrlPlaceholder: '/assets/bikes/studio-model.jpg hoặc https://...',
        fieldImageUrlHint: 'Hỗ trợ đường dẫn nội bộ /assets/... hoặc URL HTTPS công khai (tối đa 500 ký tự).',
        imageUrlTooLong: 'Đường dẫn ảnh không được vượt quá 500 ký tự.',
        imagePreviewTitle: 'Xem trước ảnh Studio trực tiếp',
        imagePreviewAlt: 'Xem trước ảnh mẫu xe',
        fieldSpecsJson: 'Thông số kỹ thuật JSON (tùy chọn)',
        specsJsonInvalid: 'Thông số kỹ thuật phải là đối tượng hoặc mảng JSON hợp lệ.',
        fieldIsActive: 'Hiển thị mẫu xe trên Showroom công khai & Garage VIN',
        saveModelCta: 'Lưu thay đổi mẫu xe',
        createModelCta: 'Tạo mẫu xe mới',
        studioSuffix: 'Studio Quy chuẩn OEM',
        configuredMilestones: 'Các mốc đã cấu hình ({{count}})',
        newRuleBtn: 'Thêm mốc mới',
        recurringLabel: 'Định kỳ lặp lại',
        oneTimeLabel: 'Một lần (Rô-đa)',
        intervalSummary: 'Chu kỳ: {{km}} km / {{months}} tháng · Nhắc trước: {{leadKm}} km / {{leadDays}} ngày',
        editRuleHeading: 'Chỉnh sửa mốc: {{code}}',
        addRuleHeading: 'Thêm mốc bảo dưỡng định kỳ OEM',
        fieldRuleCode: 'Mã mốc bảo dưỡng *',
        fieldRuleCodePlaceholder: 'Nhập mã mốc (VD: PERIODIC_4K)',
        fieldRuleName: 'Tên mốc bảo dưỡng *',
        fieldRuleNamePlaceholder: 'Nhập tên hạng mục bảo dưỡng định kỳ',
        fieldIntervalKm: 'Chu kỳ quãng đường (km) *',
        fieldIntervalMo: 'Chu kỳ thời gian (tháng) *',
        fieldLeadKm: 'Nhắc sớm trước (km)',
        fieldLeadDays: 'Nhắc sớm trước (ngày)',
        fieldChecklist: 'Danh mục kiểm tra kỹ thuật',
        fieldChecklistPlaceholder: 'Thay nhớt tổng hợp, vệ sinh lọc gió, kiểm tra phanh và truyền động.',
        fieldRecurring: 'Mốc bảo dưỡng định kỳ lặp lại',
        resetRuleBtn: 'Đặt lại',
        updateRuleCta: 'Cập nhật mốc',
        saveRuleCta: 'Lưu mốc bảo dưỡng',
        toastModelUpdated: 'Đã cập nhật mẫu xe',
        toastModelCreated: 'Đã tạo mẫu xe mới',
        toastModelSavedBody: '{{code}} · {{name}} đã được lưu.',
        toastRuleSaved: 'Đã lưu quy chuẩn OEM',
        toastRuleSavedBody: 'Mốc {{code}} ({{km}} km) đã được lưu.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      models: {
        screenCode: 'SCR-ADM-04 · FR-02 / FR-03 / FR-04',
        backToAnalytics: 'Analytics',
        refreshing: 'Refreshing...',
        addModelCta: 'Add Motorcycle Model',
        kpiTotalModels: 'Catalog Models',
        kpiActiveModels: 'Active Showroom Models',
        kpiTotalRules: 'OEM Maintenance Rules',
        searchLabel: 'Search SKU, Brand, or Model Name',
        searchPlaceholder: 'Search by model SKU code, brand, or model name...',
        categoryLabel: 'Category',
        clearBtn: 'Clear',
        colThumb: 'Studio Photo',
        colCode: 'Model Code',
        colBrandModel: 'Brand & Model',
        colCategory: 'Category',
        colEngine: 'Engine',
        colMsrp: 'Base MSRP',
        colRulesActions: 'OEM Rules & Actions',
        warrantyShort: 'Warranty: {{months}} mo / {{km}} km',
        oemRulesBtn: 'OEM Rules ({{count}})',
        emptyTitle: 'No motorcycle models match your filters',
        emptyDesc: 'Try clearing the search query or category filter, or add a new model to the catalog.',
        modalEditTitle: 'Edit Motorcycle Catalog Model',
        modalCreateTitle: 'Create Motorcycle Catalog Model',
        fieldCode: 'Model SKU Code *',
        fieldCodePlaceholder: 'Enter unique model SKU code',
        fieldBrand: 'Brand *',
        fieldBrandPlaceholder: 'Enter manufacturer brand',
        fieldCategory: 'Category *',
        fieldName: 'Full Model Name *',
        fieldNamePlaceholder: 'Enter full motorcycle model name',
        fieldCc: 'Displacement (cc) *',
        fieldPrice: 'Base Retail Price (VND) *',
        fieldWarrantyMo: 'Warranty Months *',
        fieldWarrantyKm: 'Warranty Mileage (km) *',
        fieldImageUrl: 'Studio Image URL (ImageUrl — max 500 chars)',
        fieldImageUrlPlaceholder: '/assets/bikes/studio-model.jpg or https://...',
        fieldImageUrlHint: 'Accepts local /assets/... studio asset path or public HTTPS URL (max 500 chars).',
        imageUrlTooLong: 'Image URL must not exceed 500 characters.',
        imagePreviewTitle: 'Live Studio Preview',
        imagePreviewAlt: 'Motorcycle studio preview',
        fieldSpecsJson: 'Specifications JSON (Optional Object)',
        specsJsonInvalid: 'Specifications must be a valid JSON object or array.',
        fieldIsActive: 'Model Active in Public Showroom & Garage Dropdowns',
        saveModelCta: 'Save Model Changes',
        createModelCta: 'Create Model',
        studioSuffix: 'OEM Rules Studio',
        configuredMilestones: 'Configured Milestones ({{count}})',
        newRuleBtn: 'New Rule',
        recurringLabel: 'Recurring',
        oneTimeLabel: 'One-Time',
        intervalSummary: 'Interval: {{km}} km / {{months}} mo · Lead: {{leadKm}} km / {{leadDays}} d',
        editRuleHeading: 'Edit Rule: {{code}}',
        addRuleHeading: 'Add OEM Maintenance Milestone',
        fieldRuleCode: 'Milestone Code *',
        fieldRuleCodePlaceholder: 'Enter milestone code',
        fieldRuleName: 'Milestone Name *',
        fieldRuleNamePlaceholder: 'Enter maintenance milestone name',
        fieldIntervalKm: 'Interval (km) *',
        fieldIntervalMo: 'Interval (Months) *',
        fieldLeadKm: 'Lead Mileage (km)',
        fieldLeadDays: 'Lead Days',
        fieldChecklist: 'Checklist Summary',
        fieldChecklistPlaceholder: 'Synthetic oil, air filter, belt & brake inspection.',
        fieldRecurring: 'Recurring Interval Milestone',
        resetRuleBtn: 'Reset',
        updateRuleCta: 'Update Rule',
        saveRuleCta: 'Save Milestone Rule',
        toastModelUpdated: 'Model Updated',
        toastModelCreated: 'Model Created',
        toastModelSavedBody: '{{code}} · {{name}} saved.',
        toastRuleSaved: 'OEM Rule Saved',
        toastRuleSavedBody: '{{code}} ({{km}} km) saved.'
      }
    }
  };

  registerModelsI18n.$inject = ['I18nService'];
  function registerModelsI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerModelsI18n)
    .component('adminModels', {
      controllerAs: '$ctrl',
      controller: AdminModelsController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.models.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.modelsTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.modelsSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.models.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.loadModels()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.models.refreshing\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Showroom.ManageModels" ' +
                      'ng-click="$ctrl.openCreateModelModal()">' +
                '<span>{{ \'admin.models.addModelCta\' | t }}</span>' +
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
            'on-retry="$ctrl.loadModels()">' +
          '</error-retry-banner>' +

          /* Double-Bezel KPI Summary Strip */
          '<div class="bento-grid" style="margin-bottom:20px;">' +
            '<div class="col-4 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.models.kpiTotalModels\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalModels" kpi-format="number">{{ $ctrl.stats.totalModels }}</div>' +
                '</div>' +
                '<app-icon name="motorcycle" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.models.kpiActiveModels\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.activeModels" kpi-format="number">{{ $ctrl.stats.activeModels }}</div>' +
                '</div>' +
                '<app-icon name="check-circle" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-4 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.models.kpiTotalRules\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalRules" kpi-format="number">{{ $ctrl.stats.totalRules }}</div>' +
                '</div>' +
                '<app-icon name="sliders-horizontal" weight="duotone" size="28" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Filter Card */
          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12" style="align-items:end;">' +
                '<div class="col-6 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admModelSearch">{{ \'admin.models.searchLabel\' | t }}</label>' +
                  '<input id="admModelSearch" type="search" class="form-control" ' +
                         'ng-model="$ctrl.filters.search" ng-change="$ctrl.applyFilters()" ' +
                         'ng-attr-placeholder="{{ \'admin.models.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="admModelCategory">{{ \'admin.models.categoryLabel\' | t }}</label>' +
                  '<select id="admModelCategory" class="form-control" ' +
                          'ng-model="$ctrl.filters.category" ' +
                          'ng-options="c.value as c.label for c in $ctrl.categoryFilterOptions" ' +
                          'ng-change="$ctrl.applyFilters()"></select>' +
                '</div>' +
                '<div class="col-2 form-group" style="margin-bottom:0;">' +
                  '<button type="button" class="btn btn-secondary" style="width:100%;" ng-click="$ctrl.resetFilters()">{{ \'admin.models.clearBtn\' | t }}</button>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="5" ng-if="$ctrl.isLoading && $ctrl.models.length === 0"></skeleton-loader>' +

          /* Quiet-Chrome Models Table with Studio Thumbnails */
          '<div class="bezel-card quiet-table-frame" ng-if="!$ctrl.isLoading || $ctrl.models.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredModels.length === 0" ' +
                           'icon="motorcycle" ' +
                           'title-key="admin.models.emptyTitle" ' +
                           'desc-key="admin.models.emptyDesc" ' +
                           'action-key="admin.models.clearBtn" ' +
                           'on-action="$ctrl.resetFilters()">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredModels.length > 0" style="max-height:580px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;width:116px;">{{ \'admin.models.colThumb\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.models.colCode\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.models.colBrandModel\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.models.colCategory\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.models.colEngine\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;text-align:right;">{{ \'admin.models.colMsrp\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'common.status\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.models.colRulesActions\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="m in $ctrl.filteredModels track by m.id">' +
                      '<td style="width:116px;">' +
                        '<bike-image src="m.imageUrl" alt="{{ m.modelName || m.modelCode }}" category="{{ m.category }}" variant="thumb"></bike-image>' +
                      '</td>' +
                      '<td class="mono-num" style="font-weight:700;">{{ m.modelCode }}</td>' +
                      '<td>' +
                        '<div style="font-weight:600;" class="text-primary">{{ m.modelName }}</div>' +
                        '<div class="text-muted" style="font-size:12px;">' +
                          '{{ m.brand }} · {{ \'admin.models.warrantyShort\' | t:{ months: m.warrantyMonths, km: (m.warrantyMileageKm | number) } }}' +
                        '</div>' +
                      '</td>' +
                      '<td><span class="badge badge-neutral">{{ m.category | enumT:\'MotorcycleCategory\' }}</span></td>' +
                      '<td class="mono-num">{{ m.engineDisplacementCc }} cc</td>' +
                      '<td class="mono-num" style="text-align:right;font-weight:700;">{{ m.baseRetailPrice | vndCurrency }}</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="m.isActive ? \'badge-success\' : \'badge-neutral\'">' +
                          '<app-icon name="{{ m.isActive ? \'check-circle\' : \'minus-circle\' }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ (m.isActive ? \'common.active\' : \'common.inactive\') | t }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td>' +
                        '<div style="display:flex;gap:6px;flex-wrap:wrap;">' +
                          '<button type="button" class="btn btn-secondary btn-sm" ' +
                                  'has-permission="Permissions.Showroom.ManageModels" ' +
                                  'ng-click="$ctrl.openEditModelModal(m)">' +
                            '<app-icon name="pencil-simple" weight="regular" size="13"></app-icon>' +
                            '<span>{{ \'common.edit\' | t }}</span>' +
                          '</button>' +
                          '<button type="button" class="btn btn-primary btn-sm" ' +
                                  'has-permission="Permissions.Vehicles.ManageRules" ' +
                                  'ng-click="$ctrl.openRulesStudio(m)">' +
                            '<app-icon name="sliders-horizontal" weight="regular" size="13"></app-icon>' +
                            '<span>{{ \'admin.models.oemRulesBtn\' | t:{ count: (m.maintenanceRules ? m.maintenanceRules.length : 0) } }}</span>' +
                          '</button>' +
                        '</div>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Create / Edit MotorcycleModel Modal with ImageUrl + Live Studio Preview */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="modelModalTitle" ng-if="$ctrl.modelModalOpen">' +
            '<div class="modal-dialog modal-dialog-wide motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">{{ $ctrl.editingModelId ? (\'PUT /api/v1/motorcycles/\' + $ctrl.editingModelId) : \'POST /api/v1/motorcycles\' }}</span>' +
                  '<h2 id="modelModalTitle" class="surface-card-title" style="margin-top:4px;">' +
                    '{{ ($ctrl.editingModelId ? \'admin.models.modalEditTitle\' : \'admin.models.modalCreateTitle\') | t }}' +
                  '</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeModelModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.modelForm" ng-submit="$ctrl.saveModel()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.modelModalProblem">' +
                    '<div><strong>{{ $ctrl.modelModalProblem.title }}:</strong> {{ $ctrl.modelModalProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.modelModalProblem.traceId">traceId: {{ $ctrl.modelModalProblem.traceId }}</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlCodeInput">{{ \'admin.models.fieldCode\' | t }}</label>' +
                      '<input id="mdlCodeInput" name="modelCode" type="text" maxlength="30" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.modelCode" ng-change="$ctrl.onModelFieldEdited(\'modelCode\')" ' +
                             'ng-pattern="/^[A-Za-z0-9_-]{3,30}$/" ng-readonly="Boolean($ctrl.editingModelId)" required ' +
                             'ng-attr-placeholder="{{ \'admin.models.fieldCodePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.modelCode">{{ $ctrl.modelFieldErrors.modelCode }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlBrandInput">{{ \'admin.models.fieldBrand\' | t }}</label>' +
                      '<input id="mdlBrandInput" name="brand" type="text" maxlength="80" class="form-control" ' +
                             'ng-model="$ctrl.modelDraft.brand" ng-change="$ctrl.onModelFieldEdited(\'brand\')" required ' +
                             'ng-attr-placeholder="{{ \'admin.models.fieldBrandPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.brand">{{ $ctrl.modelFieldErrors.brand }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlCatSelect">{{ \'admin.models.fieldCategory\' | t }}</label>' +
                      '<select id="mdlCatSelect" name="category" class="form-control" ' +
                              'ng-model="$ctrl.modelDraft.category" ' +
                              'ng-options="c.value as c.label for c in $ctrl.modelCategories" required></select>' +
                    '</div>' +

                    '<div class="col-8 form-group">' +
                      '<label class="form-label" for="mdlNameInput">{{ \'admin.models.fieldName\' | t }}</label>' +
                      '<input id="mdlNameInput" name="modelName" type="text" maxlength="150" class="form-control" ' +
                             'ng-model="$ctrl.modelDraft.modelName" ng-change="$ctrl.onModelFieldEdited(\'modelName\')" required ' +
                             'ng-attr-placeholder="{{ \'admin.models.fieldNamePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.modelName">{{ $ctrl.modelFieldErrors.modelName }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlCcInput">{{ \'admin.models.fieldCc\' | t }}</label>' +
                      '<input id="mdlCcInput" name="engineDisplacementCc" type="number" min="49" max="2500" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.engineDisplacementCc" ng-change="$ctrl.onModelFieldEdited(\'engineDisplacementCc\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.engineDisplacementCc">{{ $ctrl.modelFieldErrors.engineDisplacementCc }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlPriceInput">{{ \'admin.models.fieldPrice\' | t }}</label>' +
                      '<input id="mdlPriceInput" name="baseRetailPrice" type="number" min="1000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.baseRetailPrice" ng-change="$ctrl.onModelFieldEdited(\'baseRetailPrice\')" required />' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.baseRetailPrice">{{ $ctrl.modelFieldErrors.baseRetailPrice }}</div>' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlWarMonths">{{ \'admin.models.fieldWarrantyMo\' | t }}</label>' +
                      '<input id="mdlWarMonths" name="warrantyMonths" type="number" min="1" max="120" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.warrantyMonths" required />' +
                    '</div>' +

                    '<div class="col-4 form-group">' +
                      '<label class="form-label" for="mdlWarKm">{{ \'admin.models.fieldWarrantyKm\' | t }}</label>' +
                      '<input id="mdlWarKm" name="warrantyMileageKm" type="number" min="1000" max="500000" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.warrantyMileageKm" required />' +
                    '</div>' +

                    /* ImageUrl Input + Live <bike-image> Preview */
                    '<div class="col-7 form-group">' +
                      '<label class="form-label" for="mdlImageUrlInput" style="display:inline-flex;align-items:center;gap:6px;">' +
                        '<app-icon name="image" weight="duotone" size="15"></app-icon>' +
                        '<span>{{ \'admin.models.fieldImageUrl\' | t }}</span>' +
                      '</label>' +
                      '<input id="mdlImageUrlInput" name="imageUrl" type="text" maxlength="500" class="form-control mono-num" ' +
                             'ng-model="$ctrl.modelDraft.imageUrl" ng-change="$ctrl.onModelFieldEdited(\'imageUrl\')" ' +
                             'ng-attr-placeholder="{{ \'admin.models.fieldImageUrlPlaceholder\' | t }}" />' +
                      '<div class="field-hint">{{ \'admin.models.fieldImageUrlHint\' | t }}</div>' +
                      '<div class="field-error" ng-if="!$ctrl.isImageUrlValid()">{{ \'admin.models.imageUrlTooLong\' | t }}</div>' +
                      '<div class="field-error" ng-if="$ctrl.modelFieldErrors.imageUrl">{{ $ctrl.modelFieldErrors.imageUrl }}</div>' +
                    '</div>' +

                    '<div class="col-5 form-group">' +
                      '<div class="bezel-card">' +
                        '<div class="bezel-card-inner" style="padding:10px;">' +
                          '<div class="kpi-label" style="margin-bottom:6px;">{{ \'admin.models.imagePreviewTitle\' | t }}</div>' +
                          '<bike-image src="$ctrl.modelDraft.imageUrl" ' +
                                      'alt="{{ $ctrl.modelDraft.modelName || (\'admin.models.imagePreviewAlt\' | t) }}" ' +
                                      'category="{{ $ctrl.modelDraft.category }}" ' +
                                      'variant="card">' +
                          '</bike-image>' +
                        '</div>' +
                      '</div>' +
                    '</div>' +

                    '<div class="col-12 form-group">' +
                      '<label class="form-label" for="mdlSpecsJson">{{ \'admin.models.fieldSpecsJson\' | t }}</label>' +
                      '<textarea id="mdlSpecsJson" name="specificationsJson" rows="2" class="form-control mono-num" ' +
                                'ng-model="$ctrl.modelDraft.specificationsJson" placeholder=\'{"horsepower":"29.0 hp","abs":"Dual-Channel"}\'></textarea>' +
                      '<div class="field-error" ng-if="!$ctrl.isSpecsJsonValid()">{{ \'admin.models.specsJsonInvalid\' | t }}</div>' +
                    '</div>' +

                    '<div class="col-12 form-group" style="margin-bottom:0;">' +
                      '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;">' +
                        '<input type="checkbox" ng-model="$ctrl.modelDraft.isActive" />' +
                        '<span>{{ \'admin.models.fieldIsActive\' | t }}</span>' +
                      '</label>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeModelModal()" ng-disabled="$ctrl.isSavingModel">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'has-permission="Permissions.Showroom.ManageModels" ' +
                          'ng-disabled="$ctrl.isSavingModel || $ctrl.modelForm.$invalid || !$ctrl.isSpecsJsonValid() || !$ctrl.isImageUrlValid()">' +
                    '{{ ($ctrl.isSavingModel ? \'common.saving\' : ($ctrl.editingModelId ? \'admin.models.saveModelCta\' : \'admin.models.createModelCta\')) | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Double-Bezel OEM MaintenanceRules Slide-Over Studio */
          '<div class="drawer-backdrop" role="dialog" aria-modal="true" aria-labelledby="rulesStudioTitle" ng-if="$ctrl.studioModel">' +
            '<aside class="drawer-panel-wide motion-drawer">' +
              '<div class="notification-drawer-header">' +
                '<div>' +
                  '<div class="mono-num" style="font-size:11px;opacity:0.8;">{{ $ctrl.studioModel.modelCode }}</div>' +
                  '<strong id="rulesStudioTitle" style="font-size:15px;">{{ $ctrl.studioModel.modelName }} — {{ \'admin.models.studioSuffix\' | t }}</strong>' +
                '</div>' +
                '<button type="button" class="btn btn-ghost-dark btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeRulesStudio()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<div class="notification-drawer-list">' +
                '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.ruleProblem">' +
                  '<div><strong>{{ $ctrl.ruleProblem.title }}:</strong> {{ $ctrl.ruleProblem.detail }}</div>' +
                  '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.ruleProblem.traceId">traceId: {{ $ctrl.ruleProblem.traceId }}</div>' +
                '</div>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;">' +
                  '<span class="form-label">{{ \'admin.models.configuredMilestones\' | t:{ count: $ctrl.studioRules.length } }}</span>' +
                  '<button type="button" class="btn btn-primary btn-sm" ' +
                          'has-permission="Permissions.Vehicles.ManageRules" ' +
                          'ng-click="$ctrl.startNewRule()">' +
                    '<app-icon name="plus" weight="regular" size="13"></app-icon>' +
                    '<span>{{ \'admin.models.newRuleBtn\' | t }}</span>' +
                  '</button>' +
                '</div>' +

                '<div class="bezel-card" ng-repeat="r in $ctrl.studioRules track by r.id">' +
                  '<div class="bezel-card-inner" style="padding:12px 14px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                      '<div>' +
                        '<span class="mono-num" style="font-weight:700;">{{ r.milestoneCode }}</span> · <strong class="text-primary">{{ r.milestoneName }}</strong>' +
                      '</div>' +
                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'has-permission="Permissions.Vehicles.ManageRules" ' +
                              'ng-click="$ctrl.editRule(r)">' +
                        '<app-icon name="pencil-simple" weight="regular" size="13"></app-icon>' +
                        '<span>{{ \'common.edit\' | t }}</span>' +
                      '</button>' +
                    '</div>' +
                    '<div class="mono-num text-secondary" style="font-size:12px;margin-top:6px;">' +
                      '{{ \'admin.models.intervalSummary\' | t:{ km: (r.intervalKm | number), months: r.intervalMonths, leadKm: r.leadMileageKm, leadDays: r.leadTimeDays } }} · ' +
                      '<span class="badge" ng-class="r.isRecurring ? \'badge-info\' : \'badge-neutral\'">{{ (r.isRecurring ? \'admin.models.recurringLabel\' : \'admin.models.oneTimeLabel\') | t }}</span>' +
                    '</div>' +
                    '<div class="text-muted" style="font-size:12px;margin-top:4px;" ng-if="r.checklistSummary">{{ r.checklistSummary }}</div>' +
                  '</div>' +
                '</div>' +

                '<div class="bezel-card" style="margin-top:6px;">' +
                  '<div class="bezel-card-inner" style="padding:14px;">' +
                    '<form name="$ctrl.ruleForm" ng-submit="$ctrl.saveRule()" novalidate>' +
                      '<div style="font-weight:700;font-size:13px;margin-bottom:10px;display:flex;align-items:center;gap:6px;" class="text-primary">' +
                        '<app-icon name="sliders-horizontal" weight="duotone" size="16"></app-icon>' +
                        '<span>{{ $ctrl.editingRuleId ? (\'admin.models.editRuleHeading\' | t:{ code: $ctrl.ruleDraft.milestoneCode }) : (\'admin.models.addRuleHeading\' | t) }}</span>' +
                      '</div>' +

                      '<div class="grid-12">' +
                        '<div class="col-5 form-group">' +
                          '<label class="form-label" for="ruleCodeInput">{{ \'admin.models.fieldRuleCode\' | t }}</label>' +
                          '<input id="ruleCodeInput" name="milestoneCode" type="text" maxlength="30" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.ruleDraft.milestoneCode" ng-change="$ctrl.onRuleFieldEdited(\'milestoneCode\')" ' +
                                 'ng-pattern="/^[A-Za-z0-9_-]{3,30}$/" required ' +
                                 'ng-attr-placeholder="{{ \'admin.models.fieldRuleCodePlaceholder\' | t }}" />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.milestoneCode">{{ $ctrl.ruleFieldErrors.milestoneCode }}</div>' +
                        '</div>' +
                        '<div class="col-7 form-group">' +
                          '<label class="form-label" for="ruleNameInput">{{ \'admin.models.fieldRuleName\' | t }}</label>' +
                          '<input id="ruleNameInput" name="milestoneName" type="text" maxlength="120" class="form-control" ' +
                                 'ng-model="$ctrl.ruleDraft.milestoneName" ng-change="$ctrl.onRuleFieldEdited(\'milestoneName\')" ' +
                                 'required ng-attr-placeholder="{{ \'admin.models.fieldRuleNamePlaceholder\' | t }}" />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.milestoneName">{{ $ctrl.ruleFieldErrors.milestoneName }}</div>' +
                        '</div>' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="ruleKmInput">{{ \'admin.models.fieldIntervalKm\' | t }}</label>' +
                          '<input id="ruleKmInput" name="intervalKm" type="number" min="500" max="200000" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.ruleDraft.intervalKm" ng-change="$ctrl.onRuleFieldEdited(\'intervalKm\')" required />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.intervalKm">{{ $ctrl.ruleFieldErrors.intervalKm }}</div>' +
                        '</div>' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="ruleMoInput">{{ \'admin.models.fieldIntervalMo\' | t }}</label>' +
                          '<input id="ruleMoInput" name="intervalMonths" type="number" min="1" max="120" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.ruleDraft.intervalMonths" ng-change="$ctrl.onRuleFieldEdited(\'intervalMonths\')" required />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.intervalMonths">{{ $ctrl.ruleFieldErrors.intervalMonths }}</div>' +
                        '</div>' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="ruleLeadKm">{{ \'admin.models.fieldLeadKm\' | t }}</label>' +
                          '<input id="ruleLeadKm" name="leadMileageKm" type="number" min="0" max="5000" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.ruleDraft.leadMileageKm" ng-change="$ctrl.onRuleFieldEdited(\'leadMileageKm\')" required />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.leadMileageKm">{{ $ctrl.ruleFieldErrors.leadMileageKm }}</div>' +
                        '</div>' +
                        '<div class="col-6 form-group">' +
                          '<label class="form-label" for="ruleLeadDays">{{ \'admin.models.fieldLeadDays\' | t }}</label>' +
                          '<input id="ruleLeadDays" name="leadTimeDays" type="number" min="0" max="90" class="form-control mono-num" ' +
                                 'ng-model="$ctrl.ruleDraft.leadTimeDays" ng-change="$ctrl.onRuleFieldEdited(\'leadTimeDays\')" required />' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.leadTimeDays">{{ $ctrl.ruleFieldErrors.leadTimeDays }}</div>' +
                        '</div>' +
                        '<div class="col-12 form-group">' +
                          '<label class="form-label" for="ruleChecklist">{{ \'admin.models.fieldChecklist\' | t }}</label>' +
                          '<textarea id="ruleChecklist" name="checklistSummary" rows="2" maxlength="1000" class="form-control" ' +
                                    'ng-model="$ctrl.ruleDraft.checklistSummary" ng-change="$ctrl.onRuleFieldEdited(\'checklistSummary\')" ' +
                                    'ng-attr-placeholder="{{ \'admin.models.fieldChecklistPlaceholder\' | t }}"></textarea>' +
                          '<div class="field-error" ng-if="$ctrl.ruleFieldErrors.checklistSummary">{{ $ctrl.ruleFieldErrors.checklistSummary }}</div>' +
                        '</div>' +
                        '<div class="col-12 form-group" style="margin-bottom:10px;">' +
                          '<label style="display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;cursor:pointer;">' +
                            '<input type="checkbox" ng-model="$ctrl.ruleDraft.isRecurring" />' +
                            '<span>{{ \'admin.models.fieldRecurring\' | t }}</span>' +
                          '</label>' +
                        '</div>' +
                      '</div>' +

                      '<div style="display:flex;justify-content:flex-end;gap:8px;">' +
                        '<button type="button" class="btn btn-secondary btn-sm" ng-if="$ctrl.editingRuleId" ng-click="$ctrl.startNewRule()">{{ \'admin.models.resetRuleBtn\' | t }}</button>' +
                        '<button type="submit" class="btn btn-primary btn-sm" ' +
                                'has-permission="Permissions.Vehicles.ManageRules" ' +
                                'ng-disabled="$ctrl.isSavingRule || $ctrl.ruleForm.$invalid">' +
                          '{{ ($ctrl.isSavingRule ? \'common.saving\' : ($ctrl.editingRuleId ? \'admin.models.updateRuleCta\' : \'admin.models.saveRuleCta\')) | t }}' +
                        '</button>' +
                      '</div>' +
                    '</form>' +
                  '</div>' +
                '</div>' +
              '</div>' +
            '</aside>' +
          '</div>' +
        '</section>'
    });

  AdminModelsController.$inject = [
    '$q',
    'DealerApiService',
    'ProblemDetailsMapper',
    'ToastService',
    'TokenStorageService',
    '$location',
    '$scope',
    'I18nService'
  ];
  function AdminModelsController(
    $q,
    DealerApiService,
    ProblemDetailsMapper,
    ToastService,
    TokenStorageService,
    $location,
    $scope,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var offLocale = null;

    var CATEGORY_CODES = ['Scooter', 'Underbone', 'Sport', 'Naked', 'Adventure', 'Cruiser'];

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

    function rebuildCategoryOptions() {
      $ctrl.modelCategories = CATEGORY_CODES.map(function (code) {
        return { value: code, label: enumLabel('MotorcycleCategory', code) };
      });
      $ctrl.categoryFilterOptions = [{ value: '', label: t('showroom.filterAllCategories', 'All Categories') }].concat(
        $ctrl.modelCategories.map(function (c) { return angular.extend({}, c); })
      );
    }

    function recomputeStats() {
      var list = $ctrl.models || [];
      var active = 0;
      var rules = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].isActive !== false) {
          active++;
        }
        if (angular.isArray(list[i].maintenanceRules)) {
          rules += list[i].maintenanceRules.length;
        }
      }
      $ctrl.stats = {
        totalModels: list.length,
        activeModels: active,
        totalRules: rules
      };
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.listProblem = null;
      rebuildCategoryOptions();

      var urlQuery = ($location && angular.isFunction($location.search)) ? ($location.search() || {}) : {};
      $ctrl.filters = {
        search: String(urlQuery.modelCode || urlQuery.search || '').trim(),
        category: String(urlQuery.category || '').trim()
      };

      $ctrl.modelModalOpen = false;
      $ctrl.editingModelId = null;
      $ctrl.isSavingModel = false;
      $ctrl.modelModalProblem = null;
      $ctrl.modelFieldErrors = {};
      $ctrl.modelDraft = {};

      $ctrl.studioModel = null;
      $ctrl.studioRules = [];
      $ctrl.editingRuleId = null;
      $ctrl.isSavingRule = false;
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
      $ctrl.ruleDraft = {};

      $ctrl.loadProblem = null;
      $ctrl.models = [];
      recomputeStats();
      $ctrl.applyFilters();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildCategoryOptions();
          }
        });
      }

      $ctrl.loadModels();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (angular.isFunction(offLocale)) {
        offLocale();
        offLocale = null;
      }
    };

    $ctrl.loadModels = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      return DealerApiService.listMotorcycles({ includeInactive: true, pageSize: 100 })
        .then(function (rows) {
          if (isDestroyed) { return rows; }
          $ctrl.models = rows;
          recomputeStats();
          $ctrl.applyFilters();
          return rows;
        })
        .catch(function (rejection) {
          if (!isDestroyed) {
            var prob = ProblemDetailsMapper.extract(rejection);
            $ctrl.listProblem = prob;
            $ctrl.loadProblem = prob;
          }
          return $ctrl.models;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isLoading = false;
          }
        });
    };

    $ctrl.applyFilters = function () {
      var q = String(($ctrl.filters && $ctrl.filters.search) || '').trim().toLowerCase();
      var cat = ($ctrl.filters && $ctrl.filters.category) || '';

      $ctrl.filteredModels = ($ctrl.models || []).filter(function (m) {
        if (cat && m.category !== cat) {
          return false;
        }
        if (q) {
          var hay = (m.modelCode + ' ' + m.brand + ' ' + m.modelName + ' ' + m.category).toLowerCase();
          if (hay.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.resetFilters = function () {
      if ($location && angular.isFunction($location.search)) {
        $location.search('modelCode', null);
        $location.search('search', null);
        $location.search('category', null);
      }
      $ctrl.filters = { search: '', category: '' };
      $ctrl.applyFilters();
    };

    $ctrl.isImageUrlValid = function () {
      var raw = $ctrl.modelDraft && $ctrl.modelDraft.imageUrl;
      if (!raw) {
        return true;
      }
      return String(raw).trim().length <= 500;
    };

    $ctrl.isSpecsJsonValid = function () {
      var raw = $ctrl.modelDraft && $ctrl.modelDraft.specificationsJson;
      if (!raw || !String(raw).trim()) {
        return true;
      }
      try {
        var parsed = JSON.parse(raw);
        return parsed !== null && typeof parsed === 'object';
      } catch (e) {
        return false;
      }
    };

    $ctrl.openCreateModelModal = function () {
      $ctrl.editingModelId = null;
      $ctrl.modelModalProblem = null;
      $ctrl.modelFieldErrors = {};
      $ctrl.modelDraft = {
        modelCode: '',
        brand: '',
        modelName: '',
        category: 'Scooter',
        engineDisplacementCc: 150,
        baseRetailPrice: 0,
        warrantyMonths: 36,
        warrantyMileageKm: 30000,
        imageUrl: '',
        specificationsJson: '',
        isActive: true
      };
      $ctrl.modelModalOpen = true;
    };

    $ctrl.openEditModelModal = function (model) {
      if (!model) { return; }
      $ctrl.editingModelId = model.id;
      $ctrl.modelModalProblem = null;
      $ctrl.modelFieldErrors = {};
      $ctrl.modelDraft = {
        modelCode: model.modelCode,
        brand: model.brand,
        modelName: model.modelName,
        category: model.category,
        engineDisplacementCc: model.engineDisplacementCc,
        baseRetailPrice: model.baseRetailPrice,
        warrantyMonths: model.warrantyMonths,
        warrantyMileageKm: model.warrantyMileageKm,
        imageUrl: model.imageUrl || '',
        specificationsJson: model.specificationsJson || '',
        isActive: model.isActive
      };
      $ctrl.modelModalOpen = true;
    };

    $ctrl.closeModelModal = function () {
      $ctrl.modelModalOpen = false;
      $ctrl.editingModelId = null;
      $ctrl.modelModalProblem = null;
      $ctrl.modelFieldErrors = {};
    };

    $ctrl.onModelFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.modelForm, $ctrl.modelFieldErrors, fieldName);
    };

    $ctrl.saveModel = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.modelForm);
      if (($ctrl.modelForm && $ctrl.modelForm.$invalid) || !$ctrl.isSpecsJsonValid() || !$ctrl.isImageUrlValid()) {
        return $q.resolve(null);
      }
      $ctrl.isSavingModel = true;
      $ctrl.modelModalProblem = null;
      $ctrl.modelFieldErrors = {};

      var cleanedImageUrl = String($ctrl.modelDraft.imageUrl || '').trim();
      var payload = {
        modelCode: String($ctrl.modelDraft.modelCode || '').trim().toUpperCase(),
        brand: String($ctrl.modelDraft.brand || '').trim(),
        modelName: String($ctrl.modelDraft.modelName || '').trim(),
        category: $ctrl.modelDraft.category,
        engineDisplacementCc: Number($ctrl.modelDraft.engineDisplacementCc) || 150,
        baseRetailPrice: Number($ctrl.modelDraft.baseRetailPrice) || 0,
        warrantyMonths: Number($ctrl.modelDraft.warrantyMonths) || 36,
        warrantyMileageKm: Number($ctrl.modelDraft.warrantyMileageKm) || 30000,
        imageUrl: cleanedImageUrl ? cleanedImageUrl.slice(0, 500) : null,
        specificationsJson: String($ctrl.modelDraft.specificationsJson || '').trim() || null,
        isActive: Boolean($ctrl.modelDraft.isActive)
      };

      var reqPromise = $ctrl.editingModelId
        ? DealerApiService.updateMotorcycle($ctrl.editingModelId, payload)
        : DealerApiService.createMotorcycle(payload);

      return reqPromise
        .then(function (saved) {
          if (isDestroyed) { return saved; }
          ToastService.success(
            $ctrl.editingModelId
              ? t('admin.models.toastModelUpdated', 'Model Updated')
              : t('admin.models.toastModelCreated', 'Model Created'),
            t('admin.models.toastModelSavedBody', '{{code}} · {{name}} saved.', {
              code: saved.modelCode,
              name: saved.modelName
            })
          );
          $ctrl.closeModelModal();
          $ctrl.loadModels();
          return saved;
        })
        .catch(function (rejection) {
          if (isDestroyed) { return null; }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.modelFieldErrors = ProblemDetailsMapper.applyToForm($ctrl.modelForm, problem);
          $ctrl.modelModalProblem = problem;
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSavingModel = false;
          }
        });
    };

    /* OEM MaintenanceRules Studio */
    $ctrl.openRulesStudio = function (model) {
      if (!model) { return; }
      $ctrl.studioModel = model;
      $ctrl.studioRules = angular.isArray(model.maintenanceRules) ? model.maintenanceRules.slice() : [];
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
      $ctrl.startNewRule();

      DealerApiService.listMaintenanceRules(model.id, { includeInactive: true })
        .then(function (rules) {
          if (!isDestroyed && $ctrl.studioModel && $ctrl.studioModel.id === model.id) {
            $ctrl.studioRules = rules;
            model.maintenanceRules = rules;
            recomputeStats();
          }
        })
        .catch(angular.noop);
    };

    $ctrl.closeRulesStudio = function () {
      $ctrl.studioModel = null;
      $ctrl.studioRules = [];
      $ctrl.editingRuleId = null;
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
    };

    $ctrl.startNewRule = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.ruleForm);
      $ctrl.editingRuleId = null;
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
      $ctrl.ruleDraft = {
        milestoneCode: 'PERIODIC_',
        milestoneName: '',
        intervalKm: 4000,
        intervalMonths: 6,
        leadMileageKm: 250,
        leadTimeDays: 7,
        estimatedDurationMinutes: 60,
        isRecurring: true,
        checklistSummary: '',
        isActive: true
      };
    };

    $ctrl.editRule = function (rule) {
      if (!rule) { return; }
      ProblemDetailsMapper.clearServerErrors($ctrl.ruleForm);
      $ctrl.editingRuleId = rule.id;
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
      $ctrl.ruleDraft = {
        milestoneCode: rule.milestoneCode,
        milestoneName: rule.milestoneName,
        intervalKm: rule.intervalKm,
        intervalMonths: rule.intervalMonths,
        leadMileageKm: rule.leadMileageKm,
        leadTimeDays: rule.leadTimeDays,
        estimatedDurationMinutes: rule.estimatedDurationMinutes || 60,
        isRecurring: rule.isRecurring,
        checklistSummary: rule.checklistSummary || '',
        isActive: rule.isActive !== false
      };
    };

    $ctrl.onRuleFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.ruleForm, $ctrl.ruleFieldErrors, fieldName);
    };

    $ctrl.saveRule = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.ruleForm);
      if (!$ctrl.studioModel || ($ctrl.ruleForm && $ctrl.ruleForm.$invalid)) {
        return $q.resolve(null);
      }
      $ctrl.isSavingRule = true;
      $ctrl.ruleProblem = null;
      $ctrl.ruleFieldErrors = {};
      var targetModelId = $ctrl.studioModel.id;

      var payload = {
        milestoneCode: String($ctrl.ruleDraft.milestoneCode || '').trim().toUpperCase(),
        milestoneName: String($ctrl.ruleDraft.milestoneName || '').trim(),
        intervalKm: Number($ctrl.ruleDraft.intervalKm) || 1000,
        intervalMonths: Number($ctrl.ruleDraft.intervalMonths) || 6,
        leadMileageKm: Number($ctrl.ruleDraft.leadMileageKm) || 0,
        leadTimeDays: Number($ctrl.ruleDraft.leadTimeDays) || 0,
        estimatedDurationMinutes: Number($ctrl.ruleDraft.estimatedDurationMinutes) || 60,
        isRecurring: Boolean($ctrl.ruleDraft.isRecurring),
        checklistSummary: String($ctrl.ruleDraft.checklistSummary || '').trim() || null,
        isActive: Boolean($ctrl.ruleDraft.isActive)
      };

      var req = $ctrl.editingRuleId
        ? DealerApiService.updateMaintenanceRule(targetModelId, $ctrl.editingRuleId, payload)
        : DealerApiService.createMaintenanceRule(targetModelId, payload);

      return req.then(function (savedRule) {
        if (isDestroyed) { return savedRule; }
        ToastService.success(
          t('admin.models.toastRuleSaved', 'OEM Rule Saved'),
          t('admin.models.toastRuleSavedBody', '{{code}} ({{km}} km) saved.', {
            code: savedRule.milestoneCode,
            km: savedRule.intervalKm
          })
        );
        $ctrl.startNewRule();
        return DealerApiService.listMaintenanceRules(targetModelId, { includeInactive: true })
          .then(function (rules) {
            if (!isDestroyed && $ctrl.studioModel && $ctrl.studioModel.id === targetModelId) {
              $ctrl.studioRules = rules;
              $ctrl.studioModel.maintenanceRules = rules;
              recomputeStats();
            }
            return savedRule;
          });
      }).catch(function (rejection) {
        if (!isDestroyed) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.ruleFieldErrors = ProblemDetailsMapper.applyToForm($ctrl.ruleForm, problem);
          $ctrl.ruleProblem = problem;
        }
        return null;
      }).finally(function () {
        if (!isDestroyed) {
          $ctrl.isSavingRule = false;
        }
      });
    };
  }
})();
