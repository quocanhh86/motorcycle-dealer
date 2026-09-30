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

  BikeImageController.$inject = ['$element', '$scope'];
  function BikeImageController($element, $scope) {
    var $ctrl = this;
    var boundImgEl = null;

    function deriveWebpCandidate(url) {
      if (!angular.isString(url) || !url) {
        return null;
      }
      var clean = url.split('?')[0];
      if (/\.webp$/i.test(clean)) {
        return url;
      }
      if (/\.(jpe?g|png)$/i.test(clean)) {
        return url.replace(/\.(jpe?g|png)(\?.*)?$/i, '.webp$2');
      }
      return null;
    }

    function syncBindings() {
      var rawVariant = String($ctrl.variant || 'card').trim().toLowerCase();
      $ctrl.resolvedVariant = VALID_VARIANTS[rawVariant] ? rawVariant : 'card';

      var rawSrc = angular.isString($ctrl.src) ? $ctrl.src.trim() : '';
      $ctrl.usedFallback = !rawSrc;
      $ctrl.resolvedSrc = rawSrc || FALLBACK_JPG;
      $ctrl.webpSrc = $ctrl.usedFallback ? null : deriveWebpCandidate($ctrl.resolvedSrc);
      $ctrl.resolvedAlt = String($ctrl.alt || $ctrl.category || 'Motorcycle').trim();
      $ctrl.badgeText = String($ctrl.badge || '').trim();
    }

    function handleImgError() {
      if ($ctrl.usedFallback) {
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

    $ctrl.$onInit = syncBindings;
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
      boundImgEl = null;
    };
  }
})();
