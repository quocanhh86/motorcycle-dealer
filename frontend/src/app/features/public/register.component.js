(function () {
  'use strict';

  var REGISTER_I18N_VI = {
    authRegister: {
      bronzeBadge: 'Kích hoạt sẵn Hạng Đồng (1.00x)',
      existingAccountCta: 'Đăng nhập bằng tài khoản đã tồn tại này',
      fullNameError: 'Vui lòng nhập họ và tên hợp lệ (2–150 ký tự).',
      phoneError: 'Vui lòng nhập số điện thoại hợp lệ (9–15 chữ số).',
      emailError: 'Vui lòng nhập địa chỉ email hợp lệ.',
      strengthLabel: 'Độ mạnh mật khẩu',
      strengthEmpty: 'Nhập tối thiểu 8 ký tự',
      strengthStrong: 'Mạnh — đạt đủ tiêu chuẩn bảo mật',
      strengthGood: 'Khá (cần thêm nhóm ký tự còn thiếu)',
      strengthWeak: 'Yếu (cần chữ hoa, chữ thường, số và ký tự đặc biệt)',
      confirmPlaceholder: 'Nhập lại mật khẩu để xác nhận',
      confirmMismatch: 'Mật khẩu xác nhận không khớp.',
      togglePasswordAria: 'Hiện hoặc ẩn mật khẩu',
      roadmapTitle: 'Lộ trình 4 Hạng Hội viên',
      roadmapSubtitle: 'Điểm xét hạng tích lũy trọn đời theo chi tiêu mua xe và bảo dưỡng.',
      tierBronzeDesc: '0+ điểm • Hệ số gốc 1.00x (10.000 đ = 1 điểm)',
      tierSilverDesc: '500+ điểm • Hệ số thưởng 1.20x',
      tierGoldDesc: '2.000+ điểm • Hệ số 1.50x & ưu tiên khoang xưởng',
      tierPlatinumDesc: '5.000+ điểm • Nhân đôi 2.00x mọi giao dịch',
      tierProtectionNote: 'Đổi Voucher chỉ khấu trừ Điểm khả dụng — Điểm xét hạng trọn đời và hạng thành viên không bao giờ bị hạ.',
      createdToastTitle: 'Khởi tạo Tài khoản Thành công',
      createdToastMsg: 'Chào mừng! Ví hội viên Hạng Đồng của bạn đã sẵn sàng.'
    }
  };

  var REGISTER_I18N_EN = {
    authRegister: {
      bronzeBadge: 'Auto-Provisions Bronze Tier (1.00x)',
      existingAccountCta: 'Sign in with this existing account instead',
      fullNameError: 'Full name is required (2–150 characters).',
      phoneError: 'Enter a valid mobile phone number (9–15 digits).',
      emailError: 'Enter a valid email address.',
      strengthLabel: 'Strength',
      strengthEmpty: 'Enter at least 8 characters',
      strengthStrong: 'Strong',
      strengthGood: 'Good (add missing complexity class)',
      strengthWeak: 'Weak (require upper, lower, digit & special char)',
      confirmPlaceholder: 'Re-enter password',
      confirmMismatch: 'Confirmation password does not match.',
      togglePasswordAria: 'Toggle password visibility',
      roadmapTitle: '4-Tier Membership Roadmap',
      roadmapSubtitle: 'Earn lifetime qualifying points on showroom purchases and workshop visits.',
      tierBronzeDesc: '0+ pts • 1.00x base accrual (10,000 VND = 1 pt)',
      tierSilverDesc: '500+ pts • 1.20x bonus accrual',
      tierGoldDesc: '2,000+ pts • 1.50x priority workshop lane',
      tierPlatinumDesc: '5,000+ pts • 2.00x double point rewards',
      tierProtectionNote: 'Voucher redemptions spend AvailablePoints only — your LifetimeQualifyingPoints and earned tier never drop.',
      createdToastTitle: 'Account Created',
      createdToastMsg: 'Welcome! Your Bronze Loyalty Wallet is active.'
    }
  };

  angular
    .module('app.features.public')
    .run(registerRegisterI18n)
    .component('publicRegister', {
      controllerAs: '$ctrl',
      controller: PublicRegisterController,
      template:
        '<section class="grid-12 fade-slide-up">' +
          '<div class="col-8 bezel-card">' +
            '<div class="bezel-card-inner">' +
              '<div class="surface-card-header" style="flex-wrap:wrap;">' +
                '<div>' +
                  '<span class="badge badge-neutral mono-num">SCR-PUB-02B</span>' +
                  '<h1 class="surface-card-title" style="margin-top:6px;font-size:22px;">{{ \'auth.registerTitle\' | t }}</h1>' +
                  '<p class="text-muted" style="font-size:13px;margin:4px 0 0;">{{ \'auth.registerSubtitle\' | t }}</p>' +
                '</div>' +
                '<span class="badge badge-tier-bronze">' +
                  '<app-icon name="medal" weight="fill" size="13"></app-icon>' +
                  '<span>{{ \'authRegister.bronzeBadge\' | t }}</span>' +
                '</span>' +
              '</div>' +

              '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.problemBanner">' +
                '<div style="display:flex;align-items:flex-start;gap:8px;">' +
                  '<app-icon name="warning-circle" weight="fill" size="18"></app-icon>' +
                  '<div>' +
                    '<div><strong>{{ $ctrl.problemBanner.title }}:</strong> {{ $ctrl.problemBanner.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;opacity:0.88;" ng-if="$ctrl.problemBanner.traceId">' +
                      'traceId: {{ $ctrl.problemBanner.traceId }}' +
                    '</div>' +
                    '<div style="margin-top:6px;" ng-if="$ctrl.problemBanner.status === 409">' +
                      '<a ui-sref="app.public.login" style="display:inline-flex;align-items:center;gap:4px;font-weight:600;">' +
                        '<span>{{ \'authRegister.existingAccountCta\' | t }}</span>' +
                        '<app-icon name="arrow-right" weight="regular" size="13"></app-icon>' +
                      '</a>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<form name="$ctrl.registerForm" ng-submit="$ctrl.submitRegistration()" novalidate>' +
                '<div class="grid-12" style="gap:var(--space-4);">' +
                  '<div class="col-6 form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="regFullName">{{ \'auth.fullNameLabel\' | t }} *</label>' +
                    '<input id="regFullName" name="fullName" type="text" minlength="2" maxlength="150" class="form-control" ' +
                           'ng-model="$ctrl.form.fullName" ng-change="$ctrl.onFieldEdited(\'fullName\')" ' +
                           'required ng-attr-placeholder="{{ \'auth.fullNamePlaceholder\' | t }}" />' +
                    '<div class="field-error" ng-if="$ctrl.registerForm.fullName.$touched && $ctrl.registerForm.fullName.$invalid && !$ctrl.fieldErrors.fullName">' +
                      '{{ \'authRegister.fullNameError\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.fieldErrors.fullName">{{ $ctrl.fieldErrors.fullName }}</div>' +
                  '</div>' +

                  '<div class="col-6 form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="regPhone">{{ \'auth.phoneLabel\' | t }} *</label>' +
                    '<input id="regPhone" name="phoneNumber" type="tel" maxlength="20" class="form-control mono-num" ' +
                           'ng-model="$ctrl.form.phoneNumber" ng-pattern="/^\\+?[0-9]{9,15}$/" ' +
                           'ng-change="$ctrl.onFieldEdited(\'phoneNumber\')" ' +
                           'required ng-attr-placeholder="{{ \'auth.phonePlaceholder\' | t }}" />' +
                    '<div class="field-error" ng-if="$ctrl.registerForm.phoneNumber.$touched && $ctrl.registerForm.phoneNumber.$invalid && !$ctrl.fieldErrors.phoneNumber">' +
                      '{{ \'authRegister.phoneError\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.fieldErrors.phoneNumber">{{ $ctrl.fieldErrors.phoneNumber }}</div>' +
                  '</div>' +

                  '<div class="col-12 form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="regEmail">{{ \'auth.emailLabel\' | t }} *</label>' +
                    '<input id="regEmail" name="email" type="email" maxlength="256" class="form-control" ' +
                           'ng-model="$ctrl.form.email" ng-change="$ctrl.onFieldEdited(\'email\')" ' +
                           'required autocomplete="email" ng-attr-placeholder="{{ \'auth.emailPlaceholder\' | t }}" />' +
                    '<div class="field-error" ng-if="$ctrl.registerForm.email.$touched && $ctrl.registerForm.email.$invalid && !$ctrl.fieldErrors.email">' +
                      '{{ \'authRegister.emailError\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.fieldErrors.email">{{ $ctrl.fieldErrors.email }}</div>' +
                  '</div>' +

                  '<div class="col-6 form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="regPassword">{{ \'auth.passwordLabel\' | t }} *</label>' +
                    '<div class="input-action-group">' +
                      '<input id="regPassword" name="password" ' +
                             'type="{{ $ctrl.showPassword ? \'text\' : \'password\' }}" ' +
                             'minlength="8" maxlength="128" class="form-control" ' +
                             'ng-model="$ctrl.form.password" ng-change="$ctrl.onPasswordChanged()" ' +
                             'required autocomplete="new-password" ng-attr-placeholder="{{ \'auth.passwordPlaceholder\' | t }}" />' +
                      '<button type="button" class="btn btn-secondary btn-sm" ' +
                              'ng-attr-aria-label="{{ \'authRegister.togglePasswordAria\' | t }}" ' +
                              'ng-attr-aria-pressed="{{ $ctrl.showPassword ? \'true\' : \'false\' }}" ' +
                              'ng-click="$ctrl.togglePasswordVisibility()">' +
                        '<app-icon name="{{ $ctrl.showPassword ? \'eye-slash\' : \'eye\' }}" weight="regular" size="16"></app-icon>' +
                      '</button>' +
                    '</div>' +
                    '<div class="field-hint" style="display:flex;align-items:center;gap:5px;margin-top:2px;">' +
                      '<app-icon name="{{ $ctrl.isPasswordComplex() ? \'shield-check\' : \'shield-warning\' }}" weight="duotone" size="14"></app-icon>' +
                      '<span>{{ \'authRegister.strengthLabel\' | t }}: <strong class="text-primary">{{ $ctrl.passwordStrengthLabel }}</strong></span>' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.form.password && !$ctrl.isPasswordComplex() && !$ctrl.fieldErrors.password">' +
                      '{{ \'auth.passwordRulesHint\' | t }}' +
                    '</div>' +
                    '<div class="field-error" ng-if="$ctrl.fieldErrors.password">{{ $ctrl.fieldErrors.password }}</div>' +
                  '</div>' +

                  '<div class="col-6 form-group" style="margin-bottom:0;">' +
                    '<label class="form-label" for="regConfirmPassword">{{ \'auth.confirmPasswordLabel\' | t }} *</label>' +
                    '<input id="regConfirmPassword" name="confirmPassword" ' +
                           'type="{{ $ctrl.showPassword ? \'text\' : \'password\' }}" ' +
                           'maxlength="128" class="form-control" ' +
                           'ng-model="$ctrl.form.confirmPassword" required autocomplete="new-password" ' +
                           'ng-attr-placeholder="{{ \'authRegister.confirmPlaceholder\' | t }}" />' +
                    '<div class="field-error" ng-if="$ctrl.form.confirmPassword && !$ctrl.passwordsMatch()">' +
                      '{{ \'authRegister.confirmMismatch\' | t }}' +
                    '</div>' +
                  '</div>' +
                '</div>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;margin-top:20px;padding-top:16px;border-top:1px solid var(--clr-border-subtle);flex-wrap:wrap;gap:12px;">' +
                  '<div class="text-muted" style="font-size:13px;">' +
                    '<span>{{ \'auth.hasAccountPrompt\' | t }} </span>' +
                    '<a ui-sref="app.public.login" style="font-weight:600;">{{ \'nav.signIn\' | t }}</a>' +
                  '</div>' +
                  '<button id="registerSubmitBtn" type="submit" class="btn-pill-cta" ' +
                          'ng-disabled="$ctrl.isSubmitting || $ctrl.registerForm.$invalid || !$ctrl.isPasswordComplex() || !$ctrl.passwordsMatch()">' +
                    '<span>{{ $ctrl.isSubmitting ? (\'auth.registering\' | t) : (\'auth.registerCta\' | t) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="{{ $ctrl.isSubmitting ? \'spinner-gap\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSubmitting ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +

          '<div class="col-4 bezel-card">' +
            '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:14px;">' +
              '<div>' +
                '<div style="display:flex;align-items:center;gap:8px;margin-bottom:6px;">' +
                  '<app-icon name="trophy" weight="duotone" size="22"></app-icon>' +
                  '<h2 class="surface-card-title">{{ \'authRegister.roadmapTitle\' | t }}</h2>' +
                '</div>' +
                '<p class="text-muted" style="font-size:12px;margin:0 0 14px;">{{ \'authRegister.roadmapSubtitle\' | t }}</p>' +

                '<div style="display:flex;flex-direction:column;gap:10px;">' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<span class="badge badge-tier-bronze"><app-icon name="medal" weight="fill" size="12"></app-icon> {{ \'Bronze\' | enumT:\'LoyaltyTierCode\' }}</span>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:4px;">{{ \'authRegister.tierBronzeDesc\' | t }}</div>' +
                  '</div>' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<span class="badge badge-tier-silver"><app-icon name="medal" weight="fill" size="12"></app-icon> {{ \'Silver\' | enumT:\'LoyaltyTierCode\' }}</span>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:4px;">{{ \'authRegister.tierSilverDesc\' | t }}</div>' +
                  '</div>' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<span class="badge badge-tier-gold"><app-icon name="medal" weight="fill" size="12"></app-icon> {{ \'Gold\' | enumT:\'LoyaltyTierCode\' }}</span>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:4px;">{{ \'authRegister.tierGoldDesc\' | t }}</div>' +
                  '</div>' +
                  '<div class="surface-sunken" style="padding:10px 12px;">' +
                    '<span class="badge badge-tier-platinum"><app-icon name="crown" weight="fill" size="12"></app-icon> {{ \'Platinum\' | enumT:\'LoyaltyTierCode\' }}</span>' +
                    '<div class="text-secondary" style="font-size:12px;margin-top:4px;">{{ \'authRegister.tierPlatinumDesc\' | t }}</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<div class="surface-sunken text-muted" style="padding:12px;font-size:12px;display:flex;align-items:flex-start;gap:8px;">' +
                '<app-icon name="shield-check" weight="duotone" size="16"></app-icon>' +
                '<span>{{ \'authRegister.tierProtectionNote\' | t }}</span>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  registerRegisterI18n.$inject = ['I18nService'];
  function registerRegisterI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(REGISTER_I18N_VI, REGISTER_I18N_EN);
    }
  }

  PublicRegisterController.$inject = [
    '$state',
    'AuthService',
    'ProblemDetailsMapper',
    'ToastService',
    '$rootScope',
    'I18nService'
  ];
  function PublicRegisterController(
    $state,
    AuthService,
    ProblemDetailsMapper,
    ToastService,
    $rootScope,
    I18nService
  ) {
    var $ctrl = this;
    var offLocale = angular.noop;

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(REGISTER_I18N_VI, REGISTER_I18N_EN);
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

    function computeStrengthLabel(pwd) {
      var val = String(pwd || '');
      if (!val) {
        return t('authRegister.strengthEmpty', null, 'Enter at least 8 characters');
      }
      if (AuthService.isPasswordComplex(val)) {
        return t('authRegister.strengthStrong', null, 'Strong');
      }
      var score = 0;
      if (val.length >= 8) { score++; }
      if (/[A-Z]/.test(val) && /[a-z]/.test(val)) { score++; }
      if (/[0-9]/.test(val)) { score++; }
      if (/[^A-Za-z0-9\s]/.test(val)) { score++; }
      if (score >= 3) {
        return t('authRegister.strengthGood', null, 'Good (add missing complexity class)');
      }
      return t('authRegister.strengthWeak', null, 'Weak (require upper, lower, digit & special char)');
    }

    $ctrl.$onInit = function () {
      $ctrl.form = {
        fullName: '',
        phoneNumber: '',
        email: '',
        password: '',
        confirmPassword: ''
      };
      $ctrl.showPassword = false;
      $ctrl.isSubmitting = false;
      $ctrl.problemBanner = null;
      $ctrl.fieldErrors = {};
      $ctrl.passwordStrengthLabel = computeStrengthLabel('');

      if ($rootScope && angular.isFunction($rootScope.$on)) {
        offLocale = $rootScope.$on('i18n:changed', function () {
          $ctrl.passwordStrengthLabel = computeStrengthLabel($ctrl.form && $ctrl.form.password);
        });
      }
    };

    $ctrl.$onDestroy = function () {
      if (angular.isFunction(offLocale)) {
        offLocale();
      }
    };

    $ctrl.togglePasswordVisibility = function () {
      $ctrl.showPassword = !$ctrl.showPassword;
    };

    $ctrl.onFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.registerForm, $ctrl.fieldErrors, fieldName);
    };

    $ctrl.onPasswordChanged = function () {
      $ctrl.onFieldEdited('password');
      $ctrl.passwordStrengthLabel = computeStrengthLabel($ctrl.form.password);
    };

    $ctrl.isPasswordComplex = function () {
      return AuthService.isPasswordComplex($ctrl.form && $ctrl.form.password);
    };

    $ctrl.passwordsMatch = function () {
      return Boolean($ctrl.form.password) && $ctrl.form.password === $ctrl.form.confirmPassword;
    };

    $ctrl.submitRegistration = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.registerForm);
      if (
        ($ctrl.registerForm && $ctrl.registerForm.$invalid) ||
        !$ctrl.isPasswordComplex() ||
        !$ctrl.passwordsMatch()
      ) {
        return;
      }
      $ctrl.isSubmitting = true;
      $ctrl.problemBanner = null;
      $ctrl.fieldErrors = {};

      AuthService.registerCustomer({
        fullName: $ctrl.form.fullName,
        phoneNumber: $ctrl.form.phoneNumber,
        email: $ctrl.form.email,
        password: $ctrl.form.password
      })
        .then(function () {
          ToastService.success(
            t('authRegister.createdToastTitle', null, 'Account Created'),
            t('authRegister.createdToastMsg', null, 'Welcome! Your Bronze Loyalty Wallet is active.')
          );
          var destination = AuthService.isAuthenticated()
            ? AuthService.getDefaultHomeState()
            : 'app.public.login';
          $state.go(destination);
        })
        .catch(function (rejection) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.registerForm, problem);
          $ctrl.problemBanner = problem;
        })
        .finally(function () {
          $ctrl.isSubmitting = false;
        });
    };
  }
})();
