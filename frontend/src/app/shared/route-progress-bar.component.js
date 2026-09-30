(function () {
  'use strict';

  angular
    .module('app.shared')
    .component('routeProgressBar', {
      controllerAs: '$ctrl',
      controller: RouteProgressBarController,
      template:
        '<div class="route-progress-bar" ' +
             'ng-class="{\'is-active\': $ctrl.isLoading}" ' +
             'role="progressbar" ' +
             'ng-attr-aria-busy="{{ $ctrl.isLoading ? \'true\' : \'false\' }}" ' +
             'ng-attr-aria-label="{{ \'a11y.routeLoading\' | t }}">' +
          '<div class="route-progress-fill" ng-if="$ctrl.isLoading"></div>' +
        '</div>'
    });

  RouteProgressBarController.$inject = ['$transitions', '$timeout'];
  function RouteProgressBarController($transitions, $timeout) {
    var $ctrl = this;
    var deregisterFns = [];
    var finishTimer = null;

    function clearPendingTimer() {
      if (finishTimer && $timeout && angular.isFunction($timeout.cancel)) {
        $timeout.cancel(finishTimer);
      }
      finishTimer = null;
    }

    function stopProgress() {
      clearPendingTimer();
      if ($timeout && angular.isFunction($timeout)) {
        finishTimer = $timeout(function () {
          $ctrl.isLoading = false;
          finishTimer = null;
        }, 120);
      } else {
        $ctrl.isLoading = false;
      }
    }

    $ctrl.$onInit = function () {
      $ctrl.isLoading = false;
      if (!$transitions) {
        return;
      }
      if (angular.isFunction($transitions.onStart)) {
        deregisterFns.push($transitions.onStart({}, function () {
          clearPendingTimer();
          $ctrl.isLoading = true;
        }));
      }
      if (angular.isFunction($transitions.onFinish)) {
        deregisterFns.push($transitions.onFinish({}, stopProgress));
      }
      if (angular.isFunction($transitions.onError)) {
        deregisterFns.push($transitions.onError({}, stopProgress));
      }
    };

    $ctrl.$onDestroy = function () {
      clearPendingTimer();
      for (var i = 0; i < deregisterFns.length; i++) {
        if (angular.isFunction(deregisterFns[i])) {
          deregisterFns[i]();
        }
      }
      deregisterFns.length = 0;
    };
  }
})();
