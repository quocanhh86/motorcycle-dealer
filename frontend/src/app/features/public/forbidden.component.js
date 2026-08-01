(function () {
  'use strict';

  var FORBIDDEN_I18N_VI = {
    authForbidden: {
      currentRoleLabel: 'Vai trò phiên hiện tại',
      anonymousRole: 'Chưa đăng nhập',
      guardNote: 'Hệ thống kiểm soát truy cập 48 quyền RBAC và cô lập chi nhánh đại lý được thực thi nghiêm ngặt trên máy chủ.'
    }
  };

  var FORBIDDEN_I18N_EN = {
    authForbidden: {
      currentRoleLabel: 'Current Session Role',
      anonymousRole: 'Anonymous',
      guardNote: 'Server-side 48-permission RBAC policies and dealership branch isolation are strictly enforced.'
    }
  };

  angular
    .module('app.features.public')
    .run(registerForbiddenI18n)
    .component('publicForbidden', {
      controllerAs: '$ctrl',
      controller: PublicForbiddenController,
      template:
        '<section class="bezel-card fade-slide-up" style="max-width:640px;margin:32px auto;">' +
          '<div class="bezel-card-inner" style="text-align:center;padding:var(--space-8) var(--space-6);">' +
            '<div class="empty-state-emblem" style="margin:0 auto 16px;" aria-hidden="true">' +
              '<app-icon name="lock-key" weight="duotone" size="34"></app-icon>' +
            '</div>' +

            '<span class="badge badge-danger mono-num">SCR-PUB-03 • HTTP 403 FORBIDDEN</span>' +
            '<h1 class="page-title" style="margin-top:12px;">{{ \'auth.forbiddenTitle\' | t }}</h1>' +
            '<p class="page-subtitle" style="margin:10px auto 16px;max-width:500px;">{{ \'auth.forbiddenSubtitle\' | t }}</p>' +

            '<div class="surface-sunken" style="padding:14px;margin-bottom:20px;font-size:13px;">' +
              '<div class="text-secondary">' +
                '{{ \'authForbidden.currentRoleLabel\' | t }}: ' +
                '<strong class="mono-num text-primary">{{ $ctrl.currentRole ? ($ctrl.currentRole | enumT:\'UserRoleCode\') : (\'authForbidden.anonymousRole\' | t) }}</strong>' +
              '</div>' +
              '<div class="text-muted" style="font-size:12px;margin-top:6px;">{{ \'authForbidden.guardNote\' | t }}</div>' +
            '</div>' +

            '<div style="display:flex;justify-content:center;align-items:center;gap:12px;flex-wrap:wrap;">' +
              '<a class="btn btn-secondary" ui-sref="app.public.showroom">' +
                '<app-icon name="arrow-left" weight="regular" size="15"></app-icon>' +
                '<span>{{ \'auth.backToShowroom\' | t }}</span>' +
              '</a>' +
              '<a class="btn-pill-cta" ui-sref="{{ $ctrl.homeState }}">' +
                '<span>{{ \'auth.backToHome\' | t }}</span>' +
                '<span class="btn-pill-well" aria-hidden="true">' +
                  '<app-icon name="house" weight="duotone" size="15"></app-icon>' +
                '</span>' +
              '</a>' +
            '</div>' +
          '</div>' +
        '</section>'
    });

  registerForbiddenI18n.$inject = ['I18nService'];
  function registerForbiddenI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(FORBIDDEN_I18N_VI, FORBIDDEN_I18N_EN);
    }
  }

  PublicForbiddenController.$inject = ['AuthService', 'I18nService'];
  function PublicForbiddenController(AuthService, I18nService) {
    var $ctrl = this;

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(FORBIDDEN_I18N_VI, FORBIDDEN_I18N_EN);
    }

    $ctrl.$onInit = function () {
      $ctrl.currentRole = AuthService.getRole();
      $ctrl.homeState = AuthService.isAuthenticated()
        ? AuthService.getDefaultHomeState()
        : 'app.public.login';
    };
  }
})();
