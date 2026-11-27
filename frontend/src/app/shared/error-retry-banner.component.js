(function () {
  'use strict';

  angular
    .module('app.shared')
    .component('errorRetryBanner', {
      bindings: {
        message: '<',
        messageKey: '@',
        traceId: '<',
        retrying: '<',
        onRetry: '&'
      },
      controllerAs: '$ctrl',
      controller: ErrorRetryBannerController,
      template:
        '<div class="error-retry-banner fade-slide-up" role="alert" ng-if="$ctrl.hasMessage">' +
          '<div class="error-retry-content">' +
            '<app-icon name="warning-circle" weight="fill" size="20"></app-icon>' +
            '<div>' +
              '<div style="font-weight:600;">' +
                '{{ $ctrl.messageKey ? ($ctrl.messageKey | t) : $ctrl.message }}' +
              '</div>' +
              '<div class="font-mono-code" style="font-size:11px;opacity:0.85;margin-top:2px;" ng-if="$ctrl.traceId">' +
                'Trace ID: {{ $ctrl.traceId }}' +
              '</div>' +
            '</div>' +
          '</div>' +
          '<button type="button" class="btn btn-secondary btn-sm" ' +
                  'ng-if="$ctrl.showRetry" ' +
                  'ng-disabled="$ctrl.retrying" ' +
                  'ng-attr-aria-label="{{ \'a11y.retryAction\' | t }}" ' +
                  'ng-click="$ctrl.retry()">' +
            '<app-icon name="arrow-clockwise" weight="regular" size="14"></app-icon>' +
            '<span>{{ \'common.retry\' | t }}</span>' +
          '</button>' +
        '</div>'
    });

  ErrorRetryBannerController.$inject = [];
  function ErrorRetryBannerController() {
    var $ctrl = this;

    function syncBindings() {
      $ctrl.hasMessage = Boolean(($ctrl.messageKey && String($ctrl.messageKey).trim()) || ($ctrl.message && String($ctrl.message).trim()));
      $ctrl.showRetry = angular.isFunction($ctrl.onRetry);
    }

    $ctrl.$onInit = syncBindings;
    $ctrl.$onChanges = syncBindings;

    $ctrl.retry = function () {
      if (angular.isFunction($ctrl.onRetry)) {
        $ctrl.onRetry();
      }
    };
  }
})();
