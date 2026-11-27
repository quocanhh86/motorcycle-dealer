(function () {
  'use strict';

  var FALLBACK_JPG = '/assets/editorial/showroom-hero-apex.jpg';
  var VALID_VARIANTS = {
    card: true,
    hero: true,
    thumb: true,
    modal: true
  };

  angular
    .module('app.shared')
    .component('bikeImage', {
      bindings: {
        src: '<',
        alt: '@',
        category: '@',
        variant: '@',
        badge: '@'
      },
      controllerAs: '$ctrl',
      controller: BikeImageController,
      template:
        '<figure class="bike-image-frame" ng-class="\'variant-\' + $ctrl.resolvedVariant" style="margin:0;">' +
          '<picture>' +
            '<source ng-if="$ctrl.webpSrc && !$ctrl.usedFallback" ng-attr-srcset="{{ $ctrl.webpSrc }}" type="image/webp" />' +
            '<img class="bike-image-img" ' +
                 'ng-attr-src="{{ $ctrl.resolvedSrc }}" ' +
                 'ng-attr-alt="{{ $ctrl.resolvedAlt }}" ' +
                 'loading="lazy" ' +
                 'decoding="async" />' +
          '</picture>' +
          '<span class="bike-image-badge" ng-if="$ctrl.badgeText">{{ $ctrl.badgeText }}</span>' +
        '</figure>'
    });

  BikeImageController.$inject = ['$element', '$scope', 'I18nService'];
  function BikeImageController($element, $scope, I18nService) {
    var $ctrl = this;
    var boundImgEl = null;
    var disposeI18n = null;

    function resolveDefaultAlt() {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var translated = I18nService.t('common.vehicle');
        if (translated && translated !== 'common.vehicle') {
          return translated;
        }
      }
      return 'Motorcycle';
    }

    function deriveWebpCandidate(url) {
      if (!angular.isString(url) || !url) {
        return null;
      }
      var clean = url.split('?')[0];
      // Only derive a .webp sibling for self-hosted local /assets/... files (never external CDN URLs)
      if (!/^\/assets\//.test(clean)) {
        return null;
      }
      if (/\.webp$/i.test(clean)) {
        return null;
      }
      if (/\.(jpe?g|png)$/i.test(clean)) {
        return url.replace(/\.(jpe?g|png)(\?.*)?$/i, '.webp$2');
      }
      return null;
    }

    function syncAlt() {
      $ctrl.resolvedAlt = String($ctrl.alt || $ctrl.category || resolveDefaultAlt()).trim();
    }

    function syncBindings() {
      var rawVariant = String($ctrl.variant || 'card').trim().toLowerCase();
      $ctrl.resolvedVariant = VALID_VARIANTS[rawVariant] ? rawVariant : 'card';

      var rawSrc = angular.isString($ctrl.src) ? $ctrl.src.trim() : '';
      $ctrl.usedFallback = !rawSrc;
      $ctrl.retriedOriginalSrc = false;
      $ctrl.resolvedSrc = rawSrc || FALLBACK_JPG;
      $ctrl.webpSrc = $ctrl.usedFallback ? null : deriveWebpCandidate($ctrl.resolvedSrc);
      syncAlt();
      $ctrl.badgeText = String($ctrl.badge || '').trim();
    }

    function handleImgError() {
      if ($ctrl.usedFallback) {
        return;
      }
      // If a derived local .webp candidate failed first, drop <source> and retry the original rawSrc before falling back
      if ($ctrl.webpSrc && !$ctrl.retriedOriginalSrc) {
        $ctrl.webpSrc = null;
        $ctrl.retriedOriginalSrc = true;
        if (boundImgEl && angular.isFunction(boundImgEl.setAttribute) && $ctrl.resolvedSrc) {
          boundImgEl.setAttribute('src', $ctrl.resolvedSrc);
        }
        if ($scope && angular.isFunction($scope.$applyAsync)) {
          $scope.$applyAsync(angular.noop);
        }
        return;
      }
      $ctrl.usedFallback = true;
      $ctrl.webpSrc = null;
      $ctrl.resolvedSrc = FALLBACK_JPG;
      if (boundImgEl && boundImgEl.getAttribute && boundImgEl.getAttribute('src') !== FALLBACK_JPG) {
        boundImgEl.setAttribute('src', FALLBACK_JPG);
      }
      if ($scope && angular.isFunction($scope.$applyAsync)) {
        $scope.$applyAsync(angular.noop);
      }
    }

    $ctrl.$onInit = function () {
      syncBindings();
      if ($scope && angular.isFunction($scope.$on)) {
        disposeI18n = $scope.$on('i18n:changed', syncAlt);
      }
    };
    $ctrl.$onChanges = syncBindings;

    $ctrl.$postLink = function () {
      try {
        var rootEl = $element && $element[0];
        if (rootEl && angular.isFunction(rootEl.querySelector)) {
          boundImgEl = rootEl.querySelector('img.bike-image-img');
          if (boundImgEl && angular.isFunction(boundImgEl.addEventListener)) {
            boundImgEl.addEventListener('error', handleImgError);
          }
        }
      } catch (e) {
        // Guarded for Node VM sandbox
      }
    };

    $ctrl.$onDestroy = function () {
      if (boundImgEl && angular.isFunction(boundImgEl.removeEventListener)) {
        boundImgEl.removeEventListener('error', handleImgError);
      }
      if (angular.isFunction(disposeI18n)) {
        disposeI18n();
      }
      disposeI18n = null;
      boundImgEl = null;
    };
  }
})();
