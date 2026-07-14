(function () {
  'use strict';

  var TOAST_I18N_VI = {
    toast: {
      defaultTitle: 'Thông báo Hệ thống'
    }
  };

  var TOAST_I18N_EN = {
    toast: {
      defaultTitle: 'Platform Notification'
    }
  };

  angular
    .module('app.core')
    .service('ToastService', ToastService);

  angular
    .module('app.shared')
    .run(registerToastI18n)
    .component('toastContainer', {
      controllerAs: '$ctrl',
      controller: ToastContainerController,
      template:
        '<aside class="toast-stack" aria-live="polite" aria-atomic="false">' +
          '<div class="toast-card motion-toast" ' +
               'ng-repeat="toast in $ctrl.toasts track by toast.id" ' +
               'ng-class="\'toast-\' + toast.intent">' +
            '<div style="display:flex;align-items:flex-start;gap:10px;min-width:0;flex:1;">' +
              '<span style="margin-top:1px;" ng-style="$ctrl.intentIconStyle(toast.intent)">' +
                '<app-icon name="{{ $ctrl.intentIconName(toast.intent) }}" weight="fill" size="18"></app-icon>' +
              '</span>' +
              '<div style="min-width:0;flex:1;">' +
                '<div class="text-primary" style="font-size:13px;font-weight:700;">{{ toast.title }}</div>' +
                '<div class="text-secondary" style="font-size:12px;margin-top:2px;word-break:break-word;" ng-if="toast.message">{{ toast.message }}</div>' +
              '</div>' +
            '</div>' +
            '<button type="button" class="btn btn-secondary btn-sm" ' +
                    'style="min-height:28px;padding:4px 6px;" ' +
                    'ng-attr-aria-label="{{ \'a11y.dismissToast\' | t }}" ' +
                    'ng-click="$ctrl.dismiss(toast.id)">' +
              '<app-icon name="x" weight="regular" size="14"></app-icon>' +
            '</button>' +
          '</div>' +
        '</aside>'
    });

  registerToastI18n.$inject = ['I18nService'];
  function registerToastI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(TOAST_I18N_VI, TOAST_I18N_EN);
    }
  }

  ToastService.$inject = ['$rootScope', '$timeout', 'I18nService'];
  function ToastService($rootScope, $timeout, I18nService) {
    var self = this;
    var activeToasts = [];
    var timersById = Object.create(null);
    var sequence = 0;

    function resolveDefaultTitle() {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t('toast.defaultTitle');
        if (resolved && resolved !== 'toast.defaultTitle') {
          return resolved;
        }
      }
      return 'Platform Notification';
    }

    self.getActiveToasts = function () {
      return activeToasts;
    };

    self.push = function (options) {
      var opts = options || {};
      var id = 'toast-' + (++sequence);
      var item = {
        id: id,
        intent: opts.intent || 'info',
        title: opts.title || resolveDefaultTitle(),
        message: opts.message || ''
      };

      activeToasts.unshift(item);
      if (activeToasts.length > 4) {
        var overflow = activeToasts.pop();
        if (overflow && timersById[overflow.id]) {
          $timeout.cancel(timersById[overflow.id]);
          delete timersById[overflow.id];
        }
      }

      timersById[id] = $timeout(function () {
        self.dismiss(id);
      }, opts.durationMs || 6000);

      $rootScope.$broadcast('toasts:changed');
      return id;
    };

    self.info = function (title, message) {
      return self.push({ intent: 'info', title: title, message: message });
    };

    self.success = function (title, message) {
      return self.push({ intent: 'success', title: title, message: message });
    };

    self.warning = function (title, message) {
      return self.push({ intent: 'warning', title: title, message: message });
    };

    self.danger = function (title, message) {
      return self.push({ intent: 'danger', title: title, message: message });
    };

    self.dismiss = function (toastId) {
      if (timersById[toastId]) {
        $timeout.cancel(timersById[toastId]);
        delete timersById[toastId];
      }
      for (var i = 0; i < activeToasts.length; i++) {
        if (activeToasts[i].id === toastId) {
          activeToasts.splice(i, 1);
          break;
        }
      }
      $rootScope.$broadcast('toasts:changed');
    };

    self.clearAll = function () {
      var ids = Object.keys(timersById);
      for (var i = 0; i < ids.length; i++) {
        $timeout.cancel(timersById[ids[i]]);
        delete timersById[ids[i]];
      }
      activeToasts.length = 0;
      $rootScope.$broadcast('toasts:changed');
    };
  }

  ToastContainerController.$inject = ['$rootScope', 'ToastService'];
  function ToastContainerController($rootScope, ToastService) {
    var $ctrl = this;
    var offToastsChanged = angular.noop;

    $ctrl.$onInit = function () {
      $ctrl.toasts = ToastService.getActiveToasts();
      offToastsChanged = $rootScope.$on('toasts:changed', function () {
        $ctrl.toasts = ToastService.getActiveToasts();
      });
    };

    $ctrl.$onDestroy = function () {
      if (angular.isFunction(offToastsChanged)) {
        offToastsChanged();
      }
    };

    $ctrl.intentIconName = function (intent) {
      if (intent === 'success') {
        return 'check-circle';
      }
      if (intent === 'warning') {
        return 'warning';
      }
      if (intent === 'danger') {
        return 'warning-octagon';
      }
      return 'info';
    };

    $ctrl.intentIconStyle = function (intent) {
      if (intent === 'success') {
        return { color: 'var(--clr-success-accent)' };
      }
      if (intent === 'warning') {
        return { color: 'var(--clr-warning-accent)' };
      }
      if (intent === 'danger') {
        return { color: 'var(--clr-danger-accent)' };
      }
      return { color: 'var(--clr-info-accent)' };
    };

    $ctrl.dismiss = function (toastId) {
      ToastService.dismiss(toastId);
    };
  }
})();
