(function () {
  'use strict';

  var ANIMATION_DURATION_MS = 520;

  angular
    .module('app.shared')
    .directive('kpiCounter', kpiCounterDirective);

  kpiCounterDirective.$inject = ['$window', 'I18nService'];
  function kpiCounterDirective($window, I18nService) {
    function prefersReducedMotion() {
      try {
        if ($window && angular.isFunction($window.matchMedia)) {
          return Boolean($window.matchMedia('(prefers-reduced-motion: reduce)').matches);
        }
      } catch (e) {
        // Safe in Node VM sandbox
      }
      return false;
    }

    function formatValue(val, fmt) {
      var numeric = Number(val);
      if (!isFinite(numeric)) {
        numeric = 0;
      }
      var kind = String(fmt || 'number').trim().toLowerCase();
      if (I18nService && angular.isFunction(I18nService.formatNumber)) {
        if (kind === 'vnd') {
          return I18nService.formatVnd(numeric);
        }
        if (kind === 'points') {
          return I18nService.formatPoints(numeric);
        }
        if (kind === 'decimal') {
          return I18nService.formatNumber(numeric, 2);
        }
        return I18nService.formatNumber(Math.round(numeric));
      }
      if (kind === 'vnd') {
        return Math.round(numeric).toLocaleString('vi-VN') + ' ₫';
      }
      if (kind === 'points') {
        return Math.round(numeric).toLocaleString('vi-VN') + ' pts';
      }
      if (kind === 'decimal') {
        return numeric.toFixed(2);
      }
      return Math.round(numeric).toLocaleString('vi-VN');
    }

    return {
      restrict: 'A',
      link: function (scope, element, attrs) {
        var rafId = null;
        var currentDisplayed = 0;
        var latestTarget = 0;
        var latestFormat = attrs.kpiFormat || 'number';

        function cancelFrame() {
          if (rafId !== null && $window && angular.isFunction($window.cancelAnimationFrame)) {
            $window.cancelAnimationFrame(rafId);
          }
          rafId = null;
        }

        function setElementText(text) {
          if (element && angular.isFunction(element.text)) {
            element.text(text);
          }
        }

        function animateTo(targetNum) {
          cancelFrame();
          latestTarget = targetNum;
          var hasRaf = Boolean($window && angular.isFunction($window.requestAnimationFrame));
          if (!hasRaf || prefersReducedMotion() || currentDisplayed === targetNum) {
            currentDisplayed = targetNum;
            setElementText(formatValue(targetNum, latestFormat));
            return;
          }

          var startVal = currentDisplayed;
          var delta = targetNum - startVal;
          var startTime = Date.now();

          function step() {
            var elapsed = Date.now() - startTime;
            var progress = Math.min(1, elapsed / ANIMATION_DURATION_MS);
            var eased = 1 - Math.pow(1 - progress, 3);
            currentDisplayed = startVal + delta * eased;
            setElementText(formatValue(currentDisplayed, latestFormat));

            if (progress < 1) {
              rafId = $window.requestAnimationFrame(step);
            } else {
              currentDisplayed = targetNum;
              rafId = null;
              setElementText(formatValue(targetNum, latestFormat));
            }
          }

          rafId = $window.requestAnimationFrame(step);
        }

        var unwatchVal = scope.$watch(attrs.kpiCounter, function (newVal) {
          var parsed = Number(newVal);
          animateTo(isFinite(parsed) ? parsed : 0);
        });

        var unobserveFmt = angular.isFunction(attrs.$observe)
          ? attrs.$observe('kpiFormat', function (nextFmt) {
            latestFormat = nextFmt || 'number';
            setElementText(formatValue(latestTarget, latestFormat));
          })
          : angular.noop;

        var offLocale = scope.$on('i18n:changed', function () {
          setElementText(formatValue(latestTarget, latestFormat));
        });

        scope.$on('$destroy', function () {
          cancelFrame();
          if (angular.isFunction(unwatchVal)) { unwatchVal(); }
          if (angular.isFunction(unobserveFmt)) { unobserveFmt(); }
          if (angular.isFunction(offLocale)) { offLocale(); }
        });
      }
    };
  }
})();
