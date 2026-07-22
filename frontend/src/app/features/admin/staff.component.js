(function () {
  'use strict';

  var I18N_PATCH_VI = {
    admin: {
      staff: {
        screenCode: 'SCR-ADM-03 · FR-01 / ADR-002',
        backToAnalytics: 'Phân tích HQ',
        dealersLink: 'Đại lý',
        loadingLabel: 'Đang tải...',
        provisionCta: 'Cấp tài khoản nhân viên',
        kpiTotalStaff: 'Tổng nhân viên chi nhánh',
        kpiActiveStaff: 'Đang hoạt động',
        kpiLockedStaff: 'Đang bị khóa',
        kpiPolicies: 'Quyền RBAC DealerStaff',
        branchSelectLabel: 'Chọn chi nhánh đại lý',
        allBranchesOption: 'Tất cả đại lý ủy quyền ({{count}})',
        searchLabel: 'Tìm mã nhân viên, họ tên hoặc email',
        searchPlaceholder: 'Tìm mã nhân viên, họ tên hoặc email công vụ...',
        statusLabel: 'Trạng thái tài khoản',
        statusAll: 'Tất cả nhân viên',
        statusActiveOnly: 'Chỉ đang hoạt động',
        statusLockedOnly: 'Chỉ đang bị khóa',
        colEmployeeCode: 'Mã nhân viên',
        colFullName: 'Họ và tên',
        colEmailPhone: 'Email & Điện thoại',
        colAssignedBranch: 'Chi nhánh phụ trách',
        colAccountStatus: 'Trạng thái',
        colAccessControl: 'Kiểm soát truy cập',
        lockedBadge: 'Đã khóa',
        lockAccountBtn: 'Khóa tài khoản',
        activateAccountBtn: 'Mở khóa',
        emptyTitle: 'Chưa có tài khoản nhân viên phù hợp',
        emptyDesc: 'Thay đổi chi nhánh, trạng thái lọc hoặc cấp mới tài khoản nhân viên đại lý.',
        rbacPanelTitle: 'Tập quyền RBAC cấp cho vai trò DealerStaff ({{count}} quyền)',
        rbacPanelSubtitle: 'Thực thi phía máy chủ qua PermissionAuthorizationHandler + bộ lọc cô lập IDealerScoped.',
        hidePoliciesBtn: 'Ẩn danh sách quyền',
        inspectPoliciesBtn: 'Xem danh sách quyền',
        modalProvisionTitle: 'Cấp tài khoản nhân viên gắn chi nhánh',
        fieldDealer: 'Chi nhánh đại lý được gán *',
        fieldBadge: 'Mã nhân viên (EMP-*) *',
        fieldBadgePlaceholder: 'Nhập mã nhân viên (VD: EMP-HN-002)',
        fieldFullName: 'Họ và tên *',
        fieldFullNamePlaceholder: 'Nhập họ và tên nhân viên',
        fieldPhone: 'Số điện thoại *',
        fieldPhonePlaceholder: 'Nhập số di động (9-15 chữ số)',
        fieldEmail: 'Email công vụ *',
        fieldEmailPlaceholder: 'Nhập địa chỉ email nhân viên',
        fieldPassword: 'Mật khẩu khởi tạo *',
        fieldPasswordPlaceholder: 'Tối thiểu 8 ký tự, chữ hoa/thường/số/ký tự đặc biệt',
        showPwd: 'Hiện',
        hidePwd: 'Ẩn',
        pwdComplexError: 'Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt (không có khoảng trắng).',
        provisioningCta: 'Đang khởi tạo...',
        confirmProvisionCta: 'Khởi tạo tài khoản',
        confirmLockTitle: 'Khóa tài khoản DealerStaff?',
        confirmActivateTitle: 'Mở khóa tài khoản DealerStaff?',
        confirmLockBody: 'Khóa tài khoản {{name}} ({{code}}) sẽ thu hồi ngay các refresh token hiện hành và chặn truy cập REST & SignalR.',
        confirmActivateBody: 'Mở khóa tài khoản {{name}} ({{code}}) sẽ khôi phục quyền truy cập giới hạn trong chi nhánh {{dealerCode}}.',
        confirmLockCta: 'Xác nhận khóa',
        confirmActivateCta: 'Xác nhận mở khóa',
        toastProvisionedTitle: 'Đã cấp tài khoản nhân viên',
        toastProvisionedBody: '{{code}} ({{email}}) đã được gắn vào chi nhánh.',
        toastActivatedTitle: 'Đã mở khóa nhân viên',
        toastLockedTitle: 'Đã khóa tài khoản nhân viên',
        toastStatusUpdatedBody: 'Đã cập nhật trạng thái cho {{code}}.'
      }
    }
  };

  var I18N_PATCH_EN = {
    admin: {
      staff: {
        screenCode: 'SCR-ADM-03 · FR-01 / ADR-002',
        backToAnalytics: 'Analytics',
        dealersLink: 'Dealers',
        loadingLabel: 'Loading...',
        provisionCta: 'Provision Dealer Staff',
        kpiTotalStaff: 'Total Branch Staff',
        kpiActiveStaff: 'Active Accounts',
        kpiLockedStaff: 'Locked Accounts',
        kpiPolicies: 'DealerStaff RBAC Policies',
        branchSelectLabel: 'Select Dealership Branch',
        allBranchesOption: 'All Authorized Dealerships ({{count}})',
        searchLabel: 'Search Badge, Name, or Email',
        searchPlaceholder: 'Search by employee badge, full name, or email...',
        statusLabel: 'Account Status',
        statusAll: 'All Staff',
        statusActiveOnly: 'Active Only',
        statusLockedOnly: 'Locked Only',
        colEmployeeCode: 'Employee Code',
        colFullName: 'Full Name',
        colEmailPhone: 'Email & Phone',
        colAssignedBranch: 'Assigned Branch',
        colAccountStatus: 'Account Status',
        colAccessControl: 'Access Control',
        lockedBadge: 'Locked',
        lockAccountBtn: 'Lock Account',
        activateAccountBtn: 'Activate Account',
        emptyTitle: 'No DealerStaff accounts match your filters',
        emptyDesc: 'Try switching the branch or status filter, or provision a new branch-scoped staff account.',
        rbacPanelTitle: 'Granted DealerStaff Role Permission Set ({{count}} Policies)',
        rbacPanelSubtitle: 'Enforced server-side via PermissionAuthorizationHandler + IDealerScoped Global Query Filters.',
        hidePoliciesBtn: 'Hide Policies',
        inspectPoliciesBtn: 'Inspect Policies',
        modalProvisionTitle: 'Provision Branch-Scoped Dealer Staff',
        fieldDealer: 'Assigned Dealership Branch *',
        fieldBadge: 'Employee Badge Code *',
        fieldBadgePlaceholder: 'Enter employee badge code',
        fieldFullName: 'Full Name *',
        fieldFullNamePlaceholder: 'Enter staff member full name',
        fieldPhone: 'Mobile Phone *',
        fieldPhonePlaceholder: 'Enter mobile phone number',
        fieldEmail: 'Corporate Email *',
        fieldEmailPlaceholder: 'Enter corporate email address',
        fieldPassword: 'Initial Password *',
        fieldPasswordPlaceholder: 'Min 8 chars, upper/lower/digit/symbol',
        showPwd: 'Show',
        hidePwd: 'Hide',
        pwdComplexError: 'Password must be 8+ chars with uppercase, lowercase, digit, and non-whitespace special character.',
        provisioningCta: 'Provisioning...',
        confirmProvisionCta: 'Provision Staff Account',
        confirmLockTitle: 'Lock DealerStaff Account?',
        confirmActivateTitle: 'Activate DealerStaff Account?',
        confirmLockBody: 'Locking {{name}} ({{code}}) immediately revokes active refresh tokens and blocks REST & SignalR access.',
        confirmActivateBody: 'Activating {{name}} ({{code}}) restores access strictly scoped to {{dealerCode}}.',
        confirmLockCta: 'Confirm Lock',
        confirmActivateCta: 'Confirm Activate',
        toastProvisionedTitle: 'Dealer Staff Provisioned',
        toastProvisionedBody: '{{code}} ({{email}}) bound to branch.',
        toastActivatedTitle: 'Staff Account Activated',
        toastLockedTitle: 'Staff Account Locked',
        toastStatusUpdatedBody: '{{code}} status updated.'
      }
    }
  };

  registerStaffI18n.$inject = ['I18nService'];
  function registerStaffI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(I18N_PATCH_VI, I18N_PATCH_EN);
    }
  }

  angular
    .module('app.features.admin')
    .run(registerStaffI18n)
    .component('adminStaff', {
      controllerAs: '$ctrl',
      controller: AdminStaffController,
      template:
        '<section class="fade-slide-up">' +
          '<div class="page-header">' +
            '<div>' +
              '<span class="badge badge-tier-platinum mono-num">{{ \'admin.staff.screenCode\' | t }}</span>' +
              '<h1 class="page-title" style="margin-top:6px;">{{ \'admin.staffTitle\' | t }}</h1>' +
              '<p class="page-subtitle">{{ \'admin.staffSubtitle\' | t }}</p>' +
            '</div>' +
            '<div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" has-permission="Permissions.Analytics.ViewAllDealers" ui-sref="app.admin.analytics">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.staff.backToAnalytics\' | t }}</span>' +
              '</a>' +
              '<a class="btn btn-secondary" has-permission="Permissions.Dealers.ViewPublic" ui-sref="app.admin.dealers">' +
                '<app-icon name="buildings" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'admin.staff.dealersLink\' | t }}</span>' +
              '</a>' +
              '<button type="button" class="btn btn-secondary" ng-click="$ctrl.reloadStaffDirectory()" ng-disabled="$ctrl.isLoading">' +
                '<app-icon name="arrow-clockwise" weight="regular" size="15" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                '<span>{{ ($ctrl.isLoading ? \'admin.staff.loadingLabel\' : \'common.refresh\') | t }}</span>' +
              '</button>' +
              '<button type="button" class="btn-pill-cta" ' +
                      'has-permission="Permissions.Users.ManageStaff" ' +
                      'ng-click="$ctrl.openCreateModal()">' +
                '<span>{{ \'admin.staff.provisionCta\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="user-plus" weight="regular" size="15"></app-icon>' +
                '</span>' +
              '</button>' +
            '</div>' +
          '</div>' +

          '<error-retry-banner ' +
            'message="$ctrl.listProblem ? ($ctrl.listProblem.title + \': \' + $ctrl.listProblem.detail) : \'\'" ' +
            'trace-id="$ctrl.listProblem && $ctrl.listProblem.traceId" ' +
            'retrying="$ctrl.isLoading" ' +
            'on-retry="$ctrl.reloadStaffDirectory()">' +
          '</error-retry-banner>' +

          /* Double-Bezel Summary Strip */
          '<div class="bento-grid" style="margin-bottom:20px;">' +
            '<div class="col-3 bezel-card stagger-item" style="--i:0;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.staff.kpiTotalStaff\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.totalStaff" kpi-format="number">{{ $ctrl.stats.totalStaff }}</div>' +
                '</div>' +
                '<app-icon name="users-three" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:1;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.staff.kpiActiveStaff\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.activeStaff" kpi-format="number">{{ $ctrl.stats.activeStaff }}</div>' +
                '</div>' +
                '<app-icon name="check-circle" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:2;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.staff.kpiLockedStaff\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.stats.lockedStaff" kpi-format="number">{{ $ctrl.stats.lockedStaff }}</div>' +
                '</div>' +
                '<app-icon name="lock-simple" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
            '<div class="col-3 bezel-card stagger-item" style="--i:3;">' +
              '<div class="bezel-card-inner" style="display:flex;align-items:center;justify-content:space-between;">' +
                '<div>' +
                  '<div class="kpi-label">{{ \'admin.staff.kpiPolicies\' | t }}</div>' +
                  '<div class="kpi-value mono-num" kpi-counter="$ctrl.staffPermissionsList.length" kpi-format="number">{{ $ctrl.staffPermissionsList.length }}</div>' +
                '</div>' +
                '<app-icon name="shield-check" weight="duotone" size="26" extra-class="text-muted"></app-icon>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="bezel-card" style="margin-bottom:20px;">' +
            '<div class="bezel-card-inner">' +
              '<div class="grid-12">' +
                '<div class="col-5 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="staffBranchSelect">{{ \'admin.staff.branchSelectLabel\' | t }}</label>' +
                  '<select id="staffBranchSelect" class="form-control" ' +
                          'ng-model="$ctrl.selectedDealerId" ' +
                          'ng-options="b.id as b.label for b in $ctrl.branchFilterOptions" ' +
                          'ng-change="$ctrl.onBranchSelectionChanged()"></select>' +
                '</div>' +
                '<div class="col-4 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="staffSearchInput">{{ \'admin.staff.searchLabel\' | t }}</label>' +
                  '<input id="staffSearchInput" type="text" class="form-control" ' +
                         'ng-model="$ctrl.searchText" ng-change="$ctrl.applyClientFilter()" ' +
                         'ng-attr-placeholder="{{ \'admin.staff.searchPlaceholder\' | t }}" />' +
                '</div>' +
                '<div class="col-3 form-group" style="margin-bottom:0;">' +
                  '<label class="form-label" for="staffStatusFilter">{{ \'admin.staff.statusLabel\' | t }}</label>' +
                  '<select id="staffStatusFilter" class="form-control" ' +
                          'ng-model="$ctrl.statusFilter" ' +
                          'ng-options="opt.value as opt.label for opt in $ctrl.statusFilterOptions" ' +
                          'ng-change="$ctrl.applyClientFilter()"></select>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<skeleton-loader variant="table" count="4" ng-if="$ctrl.isLoading && $ctrl.staffAccounts.length === 0"></skeleton-loader>' +

          '<div class="bezel-card quiet-table-frame" style="margin-bottom:20px;" ng-if="!$ctrl.isLoading || $ctrl.staffAccounts.length > 0">' +
            '<div class="bezel-card-inner">' +
              '<empty-state ng-if="!$ctrl.isLoading && $ctrl.filteredStaff.length === 0" ' +
                           'icon="users-three" ' +
                           'title-key="admin.staff.emptyTitle" ' +
                           'desc-key="admin.staff.emptyDesc">' +
              '</empty-state>' +

              '<div class="table-wrap" ng-if="$ctrl.filteredStaff.length > 0" style="max-height:540px;overflow:auto;">' +
                '<table class="data-table tabular-nums">' +
                  '<thead>' +
                    '<tr>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colEmployeeCode\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colFullName\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colEmailPhone\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colAssignedBranch\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colAccountStatus\' | t }}</th>' +
                      '<th style="position:sticky;top:0;z-index:1;">{{ \'admin.staff.colAccessControl\' | t }}</th>' +
                    '</tr>' +
                  '</thead>' +
                  '<tbody>' +
                    '<tr ng-repeat="st in $ctrl.filteredStaff track by st.userId">' +
                      '<td class="mono-num" style="font-weight:700;">{{ st.staffEmployeeCode }}</td>' +
                      '<td style="font-weight:600;" class="text-primary">{{ st.fullName }}</td>' +
                      '<td>' +
                        '<div class="mono-num" style="font-size:12px;">{{ st.email }}</div>' +
                        '<div class="mono-num text-muted" style="font-size:11px;" ng-if="st.phoneNumber">{{ st.phoneNumber }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge badge-info mono-num">{{ st.dealerCode }}</span>' +
                        '<div class="text-muted" style="font-size:11px;margin-top:2px;" ng-if="st.dealerName">{{ st.dealerName }}</div>' +
                      '</td>' +
                      '<td>' +
                        '<span class="badge" ng-class="st.isActive ? \'badge-success\' : \'badge-danger\'">' +
                          '<app-icon name="{{ st.isActive ? \'check-circle\' : \'lock-simple\' }}" weight="fill" size="13"></app-icon>' +
                          '<span>{{ (st.isActive ? \'common.active\' : \'admin.staff.lockedBadge\') | t }}</span>' +
                        '</span>' +
                      '</td>' +
                      '<td>' +
                        '<button type="button" class="btn btn-sm" ' +
                                'has-permission="Permissions.Users.ManageStaff" ' +
                                'ng-class="st.isActive ? \'btn-secondary\' : \'btn-primary\'" ' +
                                'ng-disabled="$ctrl.togglingUserId === st.userId" ' +
                                'ng-click="$ctrl.promptToggleStatus(st)">' +
                          '<app-icon name="{{ st.isActive ? \'lock-simple\' : \'lock-simple-open\' }}" weight="regular" size="13"></app-icon>' +
                          '<span>{{ (st.isActive ? \'admin.staff.lockAccountBtn\' : \'admin.staff.activateAccountBtn\') | t }}</span>' +
                        '</button>' +
                      '</td>' +
                    '</tr>' +
                  '</tbody>' +
                '</table>' +
              '</div>' +
            '</div>' +
          '</div>' +

          '<div class="bezel-card">' +
            '<div class="bezel-card-inner">' +
              '<div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px;">' +
                '<div>' +
                  '<strong style="font-size:14px;" class="text-primary">{{ \'admin.staff.rbacPanelTitle\' | t:{ count: $ctrl.staffPermissionsList.length } }}</strong>' +
                  '<div class="text-muted" style="font-size:12px;">{{ \'admin.staff.rbacPanelSubtitle\' | t }}</div>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-click="$ctrl.showPermissionsPanel = !$ctrl.showPermissionsPanel">' +
                  '<app-icon name="shield-check" weight="regular" size="14"></app-icon>' +
                  '<span>{{ ($ctrl.showPermissionsPanel ? \'admin.staff.hidePoliciesBtn\' : \'admin.staff.inspectPoliciesBtn\') | t }}</span>' +
                '</button>' +
              '</div>' +
              '<div style="display:flex;flex-wrap:wrap;gap:6px;margin-top:12px;" ng-if="$ctrl.showPermissionsPanel">' +
                '<span class="badge badge-neutral mono-num" ng-repeat="perm in $ctrl.staffPermissionsList track by perm">{{ ::perm }}</span>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Provision Staff Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="provisionStaffTitle" ng-if="$ctrl.createModalOpen">' +
            '<div class="modal-dialog motion-modal">' +
              '<div class="modal-header">' +
                '<div>' +
                  '<span class="badge badge-info mono-num">POST /api/v1/dealers/{dealerId}/staff</span>' +
                  '<h2 id="provisionStaffTitle" class="surface-card-title" style="margin-top:4px;">{{ \'admin.staff.modalProvisionTitle\' | t }}</h2>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.closeCreateModal()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +

              '<form name="$ctrl.staffForm" ng-submit="$ctrl.submitCreateStaff()" novalidate>' +
                '<div class="modal-body">' +
                  '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.modalProblem">' +
                    '<div><strong>{{ $ctrl.modalProblem.title }}:</strong> {{ $ctrl.modalProblem.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;" ng-if="$ctrl.modalProblem.traceId">' +
                      'traceId: {{ $ctrl.modalProblem.traceId }}' +
                    '</div>' +
                  '</div>' +

                  '<div class="grid-12">' +
                    '<div class="col-7 form-group">' +
                      '<label class="form-label" for="stfDealer">{{ \'admin.staff.fieldDealer\' | t }}</label>' +
                      '<select id="stfDealer" name="dealerId" class="form-control" ' +
                              'ng-model="$ctrl.form.dealerId" ng-change="$ctrl.onFieldEdited(\'dealerId\')" ' +
                              'ng-options="d.id as (d.code + \' · \' + d.name) for d in $ctrl.dealers" ' +
                              'required></select>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.dealerId">{{ $ctrl.fieldErrors.dealerId }}</div>' +
                    '</div>' +

                    '<div class="col-5 form-group">' +
                      '<label class="form-label" for="stfBadge">{{ \'admin.staff.fieldBadge\' | t }}</label>' +
                      '<input id="stfBadge" name="staffEmployeeCode" type="text" maxlength="30" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.staffEmployeeCode" ng-change="$ctrl.onFieldEdited(\'staffEmployeeCode\')" ' +
                             'ng-pattern="/^EMP-[A-Z0-9-]{3,25}$/" ' +
                             'required ng-attr-placeholder="{{ \'admin.staff.fieldBadgePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.staffEmployeeCode">{{ $ctrl.fieldErrors.staffEmployeeCode }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="stfFullName">{{ \'admin.staff.fieldFullName\' | t }}</label>' +
                      '<input id="stfFullName" name="fullName" type="text" minlength="2" maxlength="150" class="form-control" ' +
                             'ng-model="$ctrl.form.fullName" ng-change="$ctrl.onFieldEdited(\'fullName\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.staff.fieldFullNamePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.fullName">{{ $ctrl.fieldErrors.fullName }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="stfPhone">{{ \'admin.staff.fieldPhone\' | t }}</label>' +
                      '<input id="stfPhone" name="phoneNumber" type="tel" maxlength="20" class="form-control mono-num" ' +
                             'ng-model="$ctrl.form.phoneNumber" ng-change="$ctrl.onFieldEdited(\'phoneNumber\')" ' +
                             'ng-pattern="/^\\+?[0-9]{9,15}$/" ' +
                             'required ng-attr-placeholder="{{ \'admin.staff.fieldPhonePlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.phoneNumber">{{ $ctrl.fieldErrors.phoneNumber }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="stfEmail">{{ \'admin.staff.fieldEmail\' | t }}</label>' +
                      '<input id="stfEmail" name="email" type="email" maxlength="256" class="form-control" ' +
                             'ng-model="$ctrl.form.email" ng-change="$ctrl.onFieldEdited(\'email\')" ' +
                             'required ng-attr-placeholder="{{ \'admin.staff.fieldEmailPlaceholder\' | t }}" />' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.email">{{ $ctrl.fieldErrors.email }}</div>' +
                    '</div>' +

                    '<div class="col-6 form-group">' +
                      '<label class="form-label" for="stfPassword">{{ \'admin.staff.fieldPassword\' | t }}</label>' +
                      '<div class="input-action-group">' +
                        '<input id="stfPassword" name="password" ' +
                               'type="{{ $ctrl.showTempPassword ? \'text\' : \'password\' }}" ' +
                               'minlength="8" maxlength="128" class="form-control" ' +
                               'ng-model="$ctrl.form.password" ng-change="$ctrl.onFieldEdited(\'password\')" ' +
                               'required autocomplete="new-password" ' +
                               'ng-attr-placeholder="{{ \'admin.staff.fieldPasswordPlaceholder\' | t }}" />' +
                        '<button type="button" class="btn btn-secondary btn-sm" ' +
                                'ng-click="$ctrl.showTempPassword = !$ctrl.showTempPassword">' +
                          '<app-icon name="{{ $ctrl.showTempPassword ? \'eye-slash\' : \'eye\' }}" weight="regular" size="14"></app-icon>' +
                          '<span>{{ ($ctrl.showTempPassword ? \'admin.staff.hidePwd\' : \'admin.staff.showPwd\') | t }}</span>' +
                        '</button>' +
                      '</div>' +
                      '<div class="field-error" ng-if="$ctrl.form.password && !$ctrl.isPasswordComplex() && !$ctrl.fieldErrors.password && !$ctrl.fieldErrors.temporaryPassword">' +
                        '{{ \'admin.staff.pwdComplexError\' | t }}' +
                      '</div>' +
                      '<div class="field-error" ng-if="$ctrl.fieldErrors.password || $ctrl.fieldErrors.temporaryPassword">' +
                        '{{ $ctrl.fieldErrors.password || $ctrl.fieldErrors.temporaryPassword }}' +
                      '</div>' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-secondary" ng-click="$ctrl.closeCreateModal()" ng-disabled="$ctrl.isSubmitting">{{ \'common.cancel\' | t }}</button>' +
                  '<button type="submit" class="btn btn-primary" ' +
                          'has-permission="Permissions.Users.ManageStaff" ' +
                          'ng-disabled="$ctrl.isSubmitting || $ctrl.staffForm.$invalid || !$ctrl.isPasswordComplex()">' +
                    '{{ ($ctrl.isSubmitting ? \'admin.staff.provisioningCta\' : \'admin.staff.confirmProvisionCta\') | t }}' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          /* Lock / Activate Confirmation Modal */
          '<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="confirmToggleStaffTitle" ng-if="$ctrl.confirmTarget">' +
            '<div class="modal-dialog motion-modal" style="max-width:480px;">' +
              '<div class="modal-header">' +
                '<h2 id="confirmToggleStaffTitle" class="surface-card-title">' +
                  '{{ ($ctrl.confirmTarget.isActive ? \'admin.staff.confirmLockTitle\' : \'admin.staff.confirmActivateTitle\') | t }}' +
                '</h2>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ng-click="$ctrl.cancelToggleStatus()">' +
                  '<app-icon name="x" weight="regular" size="15"></app-icon>' +
                '</button>' +
              '</div>' +
              '<div class="modal-body">' +
                '<p class="text-secondary" style="margin:0;font-size:13px;">' +
                  '<span ng-if="$ctrl.confirmTarget.isActive">' +
                    '{{ \'admin.staff.confirmLockBody\' | t:{ name: $ctrl.confirmTarget.fullName, code: $ctrl.confirmTarget.staffEmployeeCode } }}' +
                  '</span>' +
                  '<span ng-if="!$ctrl.confirmTarget.isActive">' +
                    '{{ \'admin.staff.confirmActivateBody\' | t:{ name: $ctrl.confirmTarget.fullName, code: $ctrl.confirmTarget.staffEmployeeCode, dealerCode: $ctrl.confirmTarget.dealerCode } }}' +
                  '</span>' +
                '</p>' +
              '</div>' +
              '<div class="modal-footer">' +
                '<button type="button" class="btn btn-secondary" ng-click="$ctrl.cancelToggleStatus()" ng-disabled="$ctrl.togglingUserId">{{ \'common.cancel\' | t }}</button>' +
                '<button type="button" class="btn btn-primary" ' +
                        'has-permission="Permissions.Users.ManageStaff" ' +
                        'ng-click="$ctrl.confirmToggleStatus()" ' +
                        'ng-disabled="$ctrl.togglingUserId">' +
                  '{{ ($ctrl.confirmTarget.isActive ? \'admin.staff.confirmLockCta\' : \'admin.staff.confirmActivateCta\') | t }}' +
                '</button>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  AdminStaffController.$inject = [
    '$http',
    '$q',
    '$stateParams',
    'APP_CONFIG',
    'AuthService',
    'ProblemDetailsMapper',
    'ROLE_PERMISSION_PRESETS',
    'ToastService',
    'TokenStorageService',
    '$location',
    '$scope',
    'I18nService'
  ];
  function AdminStaffController(
    $http,
    $q,
    $stateParams,
    APP_CONFIG,
    AuthService,
    ProblemDetailsMapper,
    ROLE_PERMISSION_PRESETS,
    ToastService,
    TokenStorageService,
    $location,
    $scope,
    I18nService
  ) {
    var $ctrl = this;
    var isDestroyed = false;
    var offLocale = null;
    var requestedDealerCode = '';

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

    function normalizeBranch(d) {
      return {
        id: String(d.id || d.Id || ''),
        code: String(d.code || d.Code || '').toUpperCase(),
        name: d.name || d.Name || '',
        city: d.city || d.City || ''
      };
    }

    function normalizeStaffRow(s, fallbackBranch) {
      var dealerId = String(s.dealerId || s.DealerId || (fallbackBranch && fallbackBranch.id) || '');
      return {
        userId: s.userId || s.UserId || s.id || s.Id || s.email,
        staffEmployeeCode: s.staffEmployeeCode || s.StaffEmployeeCode || s.employeeCode || '',
        fullName: s.fullName || s.FullName || '',
        email: s.email || s.Email || '',
        phoneNumber: s.phoneNumber || s.PhoneNumber || '',
        dealerId: dealerId,
        dealerCode: s.dealerCode || s.DealerCode || (fallbackBranch && fallbackBranch.code) || '',
        dealerName: s.dealerName || s.DealerName || (fallbackBranch && fallbackBranch.name) || '',
        isActive: s.isActive !== undefined ? Boolean(s.isActive) : (s.IsActive !== undefined ? Boolean(s.IsActive) : true)
      };
    }

    function rebuildStatusFilterOptions() {
      $ctrl.statusFilterOptions = [
        { value: 'ALL', label: t('admin.staff.statusAll', 'All Staff') },
        { value: 'ACTIVE', label: t('admin.staff.statusActiveOnly', 'Active Only') },
        { value: 'LOCKED', label: t('admin.staff.statusLockedOnly', 'Locked Only') }
      ];
    }

    function rebuildBranchFilterOptions() {
      var opts = [{
        id: 'ALL',
        label: t('admin.staff.allBranchesOption', 'All Authorized Dealerships ({{count}})', { count: ($ctrl.dealers || []).length })
      }];
      for (var i = 0; i < ($ctrl.dealers || []).length; i++) {
        var b = $ctrl.dealers[i];
        opts.push({ id: b.id, label: b.code + ' · ' + b.name });
      }
      $ctrl.branchFilterOptions = opts;
    }

    function recomputeStats() {
      var list = $ctrl.staffAccounts || [];
      var active = 0;
      var locked = 0;
      for (var i = 0; i < list.length; i++) {
        if (list[i].isActive) {
          active++;
        } else {
          locked++;
        }
      }
      $ctrl.stats = {
        totalStaff: list.length,
        activeStaff: active,
        lockedStaff: locked
      };
    }

    function fetchStaffForSingleBranch(branch) {
      return $http.get(APP_CONFIG.apiBaseUrl + '/dealers/' + encodeURIComponent(branch.id) + '/staff', {
        params: { includeInactive: true }
      }).then(function (res) {
        var rows = ProblemDetailsMapper.unwrapList(res);
        return rows.map(function (item) {
          return normalizeStaffRow(item, branch);
        });
      });
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      $ctrl.isSubmitting = false;
      $ctrl.togglingUserId = null;
      $ctrl.createModalOpen = false;
      $ctrl.confirmTarget = null;
      $ctrl.showTempPassword = false;
      $ctrl.showPermissionsPanel = false;
      $ctrl.listProblem = null;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.searchText = '';
      $ctrl.statusFilter = 'ALL';

      rebuildStatusFilterOptions();

      // Clone frozen ROLE_PERMISSION_PRESETS.DealerStaff into extensible strings
      $ctrl.staffPermissionsList = (ROLE_PERMISSION_PRESETS.DealerStaff || []).slice();

      var urlQuery = ($location && angular.isFunction($location.search)) ? ($location.search() || {}) : {};
      requestedDealerCode = urlQuery.dealerCode ? String(urlQuery.dealerCode).trim().toUpperCase() : '';
      if (urlQuery.search) {
        $ctrl.searchText = String(urlQuery.search).trim();
      }

      $ctrl.loadProblem = null;
      $ctrl.dealers = [];
      rebuildBranchFilterOptions();
      var paramDealerId = ($stateParams && $stateParams.dealerId) || urlQuery.dealerId || '';
      $ctrl.selectedDealerId = paramDealerId ? String(paramDealerId) : 'ALL';

      $ctrl.staffAccounts = [];
      recomputeStats();
      $ctrl.applyClientFilter();

      if ($scope && angular.isFunction($scope.$on)) {
        offLocale = $scope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            rebuildStatusFilterOptions();
            rebuildBranchFilterOptions();
          }
        });
      }

      loadBranchesThenStaff();
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      if (angular.isFunction(offLocale)) {
        offLocale();
        offLocale = null;
      }
    };

    function loadBranchesThenStaff() {
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
          $ctrl.dealers = rows.map(normalizeBranch);
          rebuildBranchFilterOptions();
          if ($ctrl.selectedDealerId === 'ALL' && requestedDealerCode) {
            for (var cIdx = 0; cIdx < $ctrl.dealers.length; cIdx++) {
              if ($ctrl.dealers[cIdx].code === requestedDealerCode) {
                $ctrl.selectedDealerId = $ctrl.dealers[cIdx].id;
                break;
              }
            }
          }
          if ($ctrl.selectedDealerId !== 'ALL') {
            var exists = $ctrl.dealers.some(function (d) { return d.id === $ctrl.selectedDealerId; });
            if (!exists) {
              $ctrl.selectedDealerId = 'ALL';
            }
          }
          return $ctrl.reloadStaffDirectory();
        })
        .catch(function (rejection) {
          if (isDestroyed) {
            return;
          }
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.listProblem = problem;
          $ctrl.loadProblem = problem;
          $ctrl.isLoading = false;
        });
    }

    $ctrl.reloadStaffDirectory = function () {
      $ctrl.isLoading = true;
      $ctrl.listProblem = null;
      $ctrl.loadProblem = null;

      var targetBranches = $ctrl.selectedDealerId === 'ALL'
        ? $ctrl.dealers.slice()
        : $ctrl.dealers.filter(function (d) { return d.id === $ctrl.selectedDealerId; });

      var promises = targetBranches.map(fetchStaffForSingleBranch);
      return $q.all(promises)
        .then(function (nestedLists) {
          if (isDestroyed) {
            return;
          }
          var merged = [];
          var seenUsers = Object.create(null);
          for (var i = 0; i < nestedLists.length; i++) {
            var branchRows = nestedLists[i] || [];
            for (var j = 0; j < branchRows.length; j++) {
              var uid = branchRows[j].userId;
              if (uid && !seenUsers[uid]) {
                seenUsers[uid] = true;
                merged.push(branchRows[j]);
              }
            }
          }
          $ctrl.staffAccounts = merged;
          recomputeStats();
          $ctrl.applyClientFilter();
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

    $ctrl.onBranchSelectionChanged = function () {
      $ctrl.reloadStaffDirectory();
    };

    $ctrl.applyClientFilter = function () {
      var q = String($ctrl.searchText || '').trim().toLowerCase();
      var branchId = $ctrl.selectedDealerId || 'ALL';
      var statusMode = $ctrl.statusFilter || 'ALL';

      $ctrl.filteredStaff = ($ctrl.staffAccounts || []).filter(function (st) {
        if (branchId !== 'ALL' && st.dealerId !== branchId) {
          return false;
        }
        if (statusMode === 'ACTIVE' && !st.isActive) {
          return false;
        }
        if (statusMode === 'LOCKED' && st.isActive) {
          return false;
        }
        if (q) {
          var haystack = (st.staffEmployeeCode + ' ' + st.fullName + ' ' + st.email + ' ' + st.dealerCode).toLowerCase();
          if (haystack.indexOf(q) === -1) {
            return false;
          }
        }
        return true;
      });
    };

    $ctrl.isPasswordComplex = function () {
      return AuthService.isPasswordComplex($ctrl.form && $ctrl.form.password);
    };

    $ctrl.onFieldEdited = function (fieldName) {
      var aliases = fieldName === 'password' ? ['temporaryPassword'] : null;
      ProblemDetailsMapper.clearFieldError($ctrl.staffForm, $ctrl.fieldErrors, fieldName, aliases);
    };

    $ctrl.openCreateModal = function () {
      var defaultDealerId = $ctrl.selectedDealerId !== 'ALL'
        ? $ctrl.selectedDealerId
        : ($ctrl.dealers[0] ? $ctrl.dealers[0].id : '');

      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
      $ctrl.showTempPassword = false;
      $ctrl.form = {
        dealerId: defaultDealerId,
        staffEmployeeCode: 'EMP-',
        fullName: '',
        phoneNumber: '',
        email: '',
        password: ''
      };
      $ctrl.createModalOpen = true;
    };

    $ctrl.closeCreateModal = function () {
      $ctrl.createModalOpen = false;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};
    };

    $ctrl.submitCreateStaff = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.staffForm);
      if (($ctrl.staffForm && $ctrl.staffForm.$invalid) || !$ctrl.isPasswordComplex()) {
        return;
      }

      var dealerId = String($ctrl.form.dealerId || '').trim();
      var pwd = String($ctrl.form.password || '');
      var payload = {
        email: String($ctrl.form.email || '').trim(),
        password: pwd,
        temporaryPassword: pwd,
        fullName: String($ctrl.form.fullName || '').trim(),
        phoneNumber: String($ctrl.form.phoneNumber || '').trim(),
        staffEmployeeCode: String($ctrl.form.staffEmployeeCode || '').trim().toUpperCase()
      };

      $ctrl.isSubmitting = true;
      $ctrl.modalProblem = null;
      $ctrl.fieldErrors = {};

      $http.post(APP_CONFIG.apiBaseUrl + '/dealers/' + encodeURIComponent(dealerId) + '/staff', payload)
        .then(function () {
          ToastService.success(
            t('admin.staff.toastProvisionedTitle', 'Dealer Staff Provisioned'),
            t('admin.staff.toastProvisionedBody', '{{code}} ({{email}}) bound to branch.', {
              code: payload.staffEmployeeCode,
              email: payload.email
            })
          );
          $ctrl.closeCreateModal();
          $ctrl.reloadStaffDirectory();
        })
        .catch(function (rejection) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.staffForm, problem);
          $ctrl.modalProblem = problem;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSubmitting = false;
          }
        });
    };

    $ctrl.promptToggleStatus = function (staffRow) {
      if (!staffRow) {
        return;
      }
      $ctrl.confirmTarget = angular.extend({}, staffRow);
    };

    $ctrl.cancelToggleStatus = function () {
      $ctrl.confirmTarget = null;
    };

    $ctrl.confirmToggleStatus = function () {
      var target = $ctrl.confirmTarget;
      if (!target || !target.dealerId || !target.userId) {
        return;
      }
      var nextActive = !target.isActive;
      $ctrl.togglingUserId = target.userId;

      $http.patch(
        APP_CONFIG.apiBaseUrl + '/dealers/' + encodeURIComponent(target.dealerId) + '/staff/' + encodeURIComponent(target.userId) + '/status',
        {
          isActive: nextActive,
          reason: nextActive ? 'Unlocked by Admin via SCR-ADM-03' : 'Locked by Admin via SCR-ADM-03'
        }
      )
        .then(function () {
          for (var i = 0; i < $ctrl.staffAccounts.length; i++) {
            if ($ctrl.staffAccounts[i].userId === target.userId) {
              $ctrl.staffAccounts[i].isActive = nextActive;
              break;
            }
          }
          recomputeStats();
          $ctrl.applyClientFilter();
          $ctrl.confirmTarget = null;
          ToastService.success(
            nextActive
              ? t('admin.staff.toastActivatedTitle', 'Staff Account Activated')
              : t('admin.staff.toastLockedTitle', 'Staff Account Locked'),
            t('admin.staff.toastStatusUpdatedBody', '{{code}} status updated.', { code: target.staffEmployeeCode })
          );
        })
        .catch(function (rejection) {
          var problem = ProblemDetailsMapper.extract(rejection);
          ToastService.danger(problem.title, problem.detail);
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.togglingUserId = null;
          }
        });
    };
  }
})();
