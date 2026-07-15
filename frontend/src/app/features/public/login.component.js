(function () {
  'use strict';

  var LOGIN_I18N_VI = {
    authLogin: {
      eyebrow: 'CỔNG XÁC THỰC JWT + RBAC 48 QUYỀN',
      heroHeadline: 'Một tài khoản duy nhất cho Garage VIN, Xưởng hãng & Ví Hội viên',
      heroCaption: 'Bảo mật bằng phiên JWT xoay vòng (Refresh Token), cô lập chi nhánh và đồng bộ thông báo thời gian thực.',
      pillarGarage: 'Theo dõi lịch bảo dưỡng định kỳ theo số khung VIN 17 ký tự',
      pillarLoyalty: 'Tích lũy điểm tự động và thăng hạng Đồng • Bạc • Vàng • Bạch kim',
      pillarRealtime: 'Cập nhật trạng thái xưởng và bảo hành tức thời qua SignalR',
      sessionExpiredTitle: 'Phiên đăng nhập đã hết hạn',
      continueToPrefix: 'Đăng nhập để tiếp tục truy cập',
      emailRequired: 'Vui lòng nhập địa chỉ email.',
      emailInvalid: 'Vui lòng nhập đúng định dạng email.',
      passwordRequired: 'Vui lòng nhập mật khẩu.',
      togglePasswordAria: 'Hiện hoặc ẩn mật khẩu',
      signedInToastTitle: 'Đăng nhập thành công ({{role}})',
      signedInToastMsg: 'Chào mừng trở lại, {{name}}.'
    }
  };

  var LOGIN_I18N_EN = {
    authLogin: {
      eyebrow: 'JWT + 48-PERMISSION RBAC ACCESS',
      heroHeadline: 'Unified Portal for Digital VIN Garage, Workshop & Loyalty',
      heroCaption: 'Protected by rotating refresh tokens, strict branch isolation, and live SignalR workshop updates.',
      pillarGarage: 'Track OEM periodic maintenance schedules by 17-character VIN',
      pillarLoyalty: 'Earn points automatically across Bronze, Silver, Gold & Platinum tiers',
      pillarRealtime: 'Receive instant workshop, test-drive, and warranty updates via SignalR',
      sessionExpiredTitle: 'Session Expired',
      continueToPrefix: 'Sign in to continue to',
      emailRequired: 'Email address is required.',
      emailInvalid: 'Enter a valid email address.',
      passwordRequired: 'Password is required.',
      togglePasswordAria: 'Toggle password visibility',
      signedInToastTitle: 'Signed In ({{role}})',
      signedInToastMsg: 'Welcome back, {{name}}.'
    }
  };

  angular
    .module('app.features.public')
    .run(registerLoginI18n)
    .component('publicLogin', {
      controllerAs: '$ctrl',
      controller: PublicLoginController,
      template:
        '<section class="grid-12 fade-slide-up" style="max-width:980px;margin:20px auto;align-items:stretch;">' +
          /* Editorial Motorsport Brand Showcase Column */
          '<div class="col-6 bezel-card">' +
            '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:space-between;gap:16px;">' +
              '<div>' +
                '<div style="position:relative;overflow:hidden;border-radius:var(--radius-lg);aspect-ratio:16/10;border:1px solid var(--clr-border-subtle);margin-bottom:16px;">' +
                  '<picture>' +
                    '<source srcset="/assets/editorial/workshop-bay-apex.webp" type="image/webp" />' +
                    '<img src="/assets/editorial/workshop-bay-apex.jpg" alt="" aria-hidden="true" style="width:100%;height:100%;object-fit:cover;display:block;" loading="lazy" decoding="async" />' +
                  '</picture>' +
                '</div>' +
                '<span class="badge badge-info mono-num" style="margin-bottom:8px;">' +
                  '<app-icon name="shield-check" weight="duotone" size="13"></app-icon>' +
                  '<span>{{ \'authLogin.eyebrow\' | t }}</span>' +
                '</span>' +
                '<h2 class="surface-card-title" style="font-size:20px;line-height:1.25;margin-top:4px;">{{ \'authLogin.heroHeadline\' | t }}</h2>' +
                '<p class="text-muted" style="font-size:13px;margin:8px 0 0;">{{ \'authLogin.heroCaption\' | t }}</p>' +
              '</div>' +

              '<div style="display:flex;flex-direction:column;gap:10px;padding-top:12px;border-top:1px solid var(--clr-border-subtle);">' +
                '<div class="text-secondary" style="display:flex;align-items:center;gap:10px;font-size:13px;">' +
                  '<app-icon name="garage" weight="duotone" size="18"></app-icon>' +
                  '<span>{{ \'authLogin.pillarGarage\' | t }}</span>' +
                '</div>' +
                '<div class="text-secondary" style="display:flex;align-items:center;gap:10px;font-size:13px;">' +
                  '<app-icon name="medal" weight="duotone" size="18"></app-icon>' +
                  '<span>{{ \'authLogin.pillarLoyalty\' | t }}</span>' +
                '</div>' +
                '<div class="text-secondary" style="display:flex;align-items:center;gap:10px;font-size:13px;">' +
                  '<app-icon name="broadcast" weight="duotone" size="18"></app-icon>' +
                  '<span>{{ \'authLogin.pillarRealtime\' | t }}</span>' +
                '</div>' +
              '</div>' +
            '</div>' +
          '</div>' +

          /* Sign-In Form Column */
          '<div class="col-6 bezel-card">' +
            '<div class="bezel-card-inner" style="display:flex;flex-direction:column;justify-content:center;">' +
              '<div class="surface-card-header">' +
                '<div>' +
                  '<span class="badge badge-neutral mono-num">SCR-PUB-02A</span>' +
                  '<h1 class="surface-card-title" style="margin-top:6px;font-size:22px;">{{ \'auth.loginTitle\' | t }}</h1>' +
                '</div>' +
                '<app-icon name="lock-key-open" weight="duotone" size="26"></app-icon>' +
              '</div>' +

              '<p class="text-muted" style="font-size:13px;margin-top:0;">{{ \'auth.loginSubtitle\' | t }}</p>' +

              '<div class="alert-banner alert-warning" ng-if="$ctrl.sessionExpiredNotice" style="display:flex;align-items:flex-start;gap:10px;">' +
                '<app-icon name="warning" weight="fill" size="18"></app-icon>' +
                '<div><strong>{{ \'authLogin.sessionExpiredTitle\' | t }}:</strong> {{ \'auth.sessionExpiredNotice\' | t }}</div>' +
              '</div>' +

              '<div class="alert-banner alert-info" ng-if="$ctrl.returnTarget && !$ctrl.problemBanner" style="display:flex;align-items:center;gap:8px;">' +
                '<app-icon name="info" weight="fill" size="16"></app-icon>' +
                '<span>{{ \'authLogin.continueToPrefix\' | t }} <strong class="mono-num">{{ $ctrl.returnTarget }}</strong>.</span>' +
              '</div>' +

              '<div class="alert-banner alert-danger" role="alert" ng-if="$ctrl.problemBanner">' +
                '<div style="display:flex;align-items:flex-start;gap:8px;">' +
                  '<app-icon name="warning-circle" weight="fill" size="18"></app-icon>' +
                  '<div>' +
                    '<div><strong>{{ $ctrl.problemBanner.title }}:</strong> {{ $ctrl.problemBanner.detail }}</div>' +
                    '<div class="mono-num" style="font-size:11px;margin-top:4px;opacity:0.88;" ng-if="$ctrl.problemBanner.errorCode">' +
                      'code: {{ $ctrl.problemBanner.errorCode }}' +
                      '<span ng-if="$ctrl.problemBanner.traceId"> • traceId: {{ $ctrl.problemBanner.traceId }}</span>' +
                    '</div>' +
                  '</div>' +
                '</div>' +
              '</div>' +

              '<form name="$ctrl.loginForm" ng-submit="$ctrl.submitLogin()" novalidate>' +
                '<div class="form-group">' +
                  '<label class="form-label" for="loginEmail">{{ \'auth.emailLabel\' | t }} *</label>' +
                  '<input id="loginEmail" name="email" type="email" maxlength="256" class="form-control" ' +
                         'ng-model="$ctrl.credentials.email" ng-change="$ctrl.onFieldEdited(\'email\')" ' +
                         'required autocomplete="email" ng-attr-placeholder="{{ \'auth.emailPlaceholder\' | t }}" />' +
                  '<div class="field-error" ng-if="$ctrl.loginForm.email.$touched && $ctrl.loginForm.email.$error.required">' +
                    '{{ \'authLogin.emailRequired\' | t }}' +
                  '</div>' +
                  '<div class="field-error" ng-if="$ctrl.loginForm.email.$touched && $ctrl.loginForm.email.$error.email">' +
                    '{{ \'authLogin.emailInvalid\' | t }}' +
                  '</div>' +
                  '<div class="field-error" ng-if="$ctrl.fieldErrors.email">{{ $ctrl.fieldErrors.email }}</div>' +
                '</div>' +

                '<div class="form-group">' +
                  '<label class="form-label" for="loginPassword">{{ \'auth.passwordLabel\' | t }} *</label>' +
                  '<div class="input-action-group">' +
                    '<input id="loginPassword" name="password" ' +
                           'type="{{ $ctrl.showPassword ? \'text\' : \'password\' }}" ' +
                           'maxlength="128" class="form-control" ' +
                           'ng-model="$ctrl.credentials.password" ng-change="$ctrl.onFieldEdited(\'password\')" ' +
                           'required autocomplete="current-password" ng-attr-placeholder="{{ \'auth.passwordPlaceholder\' | t }}" />' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-attr-aria-label="{{ \'authLogin.togglePasswordAria\' | t }}" ' +
                            'ng-attr-aria-pressed="{{ $ctrl.showPassword ? \'true\' : \'false\' }}" ' +
                            'ng-click="$ctrl.togglePasswordVisibility()">' +
                      '<app-icon name="{{ $ctrl.showPassword ? \'eye-slash\' : \'eye\' }}" weight="regular" size="16"></app-icon>' +
                    '</button>' +
                  '</div>' +
                  '<div class="field-error" ng-if="$ctrl.loginForm.password.$touched && $ctrl.loginForm.password.$error.required">' +
                    '{{ \'authLogin.passwordRequired\' | t }}' +
                  '</div>' +
                  '<div class="field-error" ng-if="$ctrl.fieldErrors.password">{{ $ctrl.fieldErrors.password }}</div>' +
                '</div>' +

                '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;margin-top:16px;flex-wrap:wrap;">' +
                  '<div style="font-size:13px;" class="text-muted">' +
                    '<span>{{ \'auth.noAccountPrompt\' | t }} </span>' +
                    '<a ui-sref="app.public.register" style="font-weight:600;">{{ \'nav.register\' | t }}</a>' +
                  '</div>' +
                  '<button id="loginSubmitBtn" type="submit" class="btn-pill-cta" ng-disabled="$ctrl.isSubmitting || $ctrl.loginForm.$invalid">' +
                    '<span>{{ $ctrl.isSubmitting ? (\'auth.signingIn\' | t) : (\'auth.signInCta\' | t) }}</span>' +
                    '<span class="btn-pill-well" aria-hidden="true">' +
                      '<app-icon name="{{ $ctrl.isSubmitting ? \'spinner-gap\' : \'arrow-right\' }}" weight="regular" size="15" extra-class="{{ $ctrl.isSubmitting ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                    '</span>' +
                  '</button>' +
                '</div>' +
              '</form>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  registerLoginI18n.$inject = ['I18nService'];
  function registerLoginI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(LOGIN_I18N_VI, LOGIN_I18N_EN);
    }
  }

  PublicLoginController.$inject = [
    '$state',
    '$stateParams',
    'AuthService',
    'ProblemDetailsMapper',
    'ToastService',
    'I18nService'
  ];
  function PublicLoginController(
    $state,
    $stateParams,
    AuthService,
    ProblemDetailsMapper,
    ToastService,
    I18nService
  ) {
    var $ctrl = this;

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(LOGIN_I18N_VI, LOGIN_I18N_EN);
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
      if (I18nService && angular.isFunction(I18nService.enumLabel) && code) {
        return I18nService.enumLabel(group, code);
      }
      return code || fallback || 'User';
    }

    $ctrl.$onInit = function () {
      $ctrl.credentials = { email: '', password: '' };
      $ctrl.showPassword = false;
      $ctrl.isSubmitting = false;
      $ctrl.problemBanner = null;
      $ctrl.fieldErrors = {};
      $ctrl.returnTarget = $stateParams.returnUrl || $stateParams.returnTo || null;
      $ctrl.sessionExpiredNotice = $stateParams.reason === 'sessionExpired';
    };

    $ctrl.togglePasswordVisibility = function () {
      $ctrl.showPassword = !$ctrl.showPassword;
    };

    $ctrl.onFieldEdited = function (fieldName) {
      ProblemDetailsMapper.clearFieldError($ctrl.loginForm, $ctrl.fieldErrors, fieldName);
    };

    $ctrl.submitLogin = function () {
      ProblemDetailsMapper.clearServerErrors($ctrl.loginForm);
      if ($ctrl.loginForm && $ctrl.loginForm.$invalid) {
        return;
      }
      executeLogin($ctrl.credentials);
    };

    function executeLogin(creds) {
      $ctrl.isSubmitting = true;
      $ctrl.problemBanner = null;
      $ctrl.fieldErrors = {};
      $ctrl.sessionExpiredNotice = false;

      AuthService.login(creds)
        .then(function (sessionUser) {
          var roleLabel = enumT('UserRoleCode', sessionUser.role, 'User');
          ToastService.success(
            t('authLogin.signedInToastTitle', { role: roleLabel }, 'Signed In (' + (sessionUser.role || 'User') + ')'),
            t('authLogin.signedInToastMsg', { name: sessionUser.fullName || sessionUser.email }, 'Welcome back, ' + (sessionUser.fullName || sessionUser.email) + '.')
          );
          if (angular.isFunction(AuthService.resolvePostLoginTarget)) {
            var target = AuthService.resolvePostLoginTarget(
              $stateParams.returnUrl,
              $stateParams.returnTo,
              $state
            );
            $state.go(target.state, target.params);
          } else {
            var targetState = AuthService.resolvePostLoginState(
              $stateParams.returnUrl || $stateParams.returnTo,
              $state
            );
            $state.go(targetState);
          }
        })
        .catch(function (rejection) {
          var problem = ProblemDetailsMapper.extract(rejection);
          $ctrl.fieldErrors = ProblemDetailsMapper.applyToForm($ctrl.loginForm, problem);
          $ctrl.problemBanner = problem;
          if (problem.status === 401) {
            $ctrl.credentials.password = '';
          }
        })
        .finally(function () {
          $ctrl.isSubmitting = false;
        });
    }
  }
})();
