(function () {
  'use strict';

  angular
    .module('app.shared')
    .component('starRating', {
      bindings: {
        value: '<',
        readonly: '<',
        size: '@',
        onChange: '&'
      },
      controllerAs: '$ctrl',
      controller: StarRatingController,
      template:
        '<div class="star-rating" ' +
             'ng-class="{\'star-rating-readonly\': $ctrl.isReadonly}" ' +
             'ng-attr-role="{{ $ctrl.isReadonly ? \'img\' : \'radiogroup\' }}" ' +
             'ng-attr-aria-label="{{ $ctrl.isReadonly ? (\'a11y.ratingValue\' | t:{ value: $ctrl.numericValue }) : (\'a11y.starRatingGroup\' | t) }}" ' +
             'ng-mouseleave="$ctrl.clearHover()">' +
          '<button type="button" ' +
                  'class="star-rating-item" ' +
                  'ng-repeat="star in $ctrl.stars track by star" ' +
                  'ng-class="{\'is-filled\': $ctrl.isFilled(star)}" ' +
                  'ng-disabled="$ctrl.isReadonly" ' +
                  'ng-attr-aria-label="{{ \'a11y.selectStar\' | t:{ count: star } }}" ' +
                  'ng-attr-aria-checked="{{ $ctrl.isReadonly ? undefined : (star === $ctrl.numericValue ? \'true\' : \'false\') }}" ' +
                  'ng-mouseenter="$ctrl.setHover(star)" ' +
                  'ng-click="$ctrl.selectStar(star)">' +
            '<app-icon name="star" weight="{{ $ctrl.isFilled(star) ? \'fill\' : \'regular\' }}" size="{{ $ctrl.iconSize }}"></app-icon>' +
          '</button>' +
        '</div>'
    });

  StarRatingController.$inject = [];
  function StarRatingController() {
    var $ctrl = this;

    $ctrl.stars = [1, 2, 3, 4, 5];
    $ctrl.hoverValue = 0;

    function syncBindings() {
      var parsed = Number($ctrl.value);
      $ctrl.numericValue = isFinite(parsed) ? Math.max(0, Math.min(5, Math.round(parsed))) : 0;
      $ctrl.isReadonly = Boolean($ctrl.readonly);
      var px = Number($ctrl.size);
      $ctrl.iconSize = (isFinite(px) && px >= 12 && px <= 48) ? px : 18;
    }

    $ctrl.$onInit = syncBindings;
    $ctrl.$onChanges = syncBindings;

    $ctrl.isFilled = function (star) {
      var active = (!$ctrl.isReadonly && $ctrl.hoverValue > 0) ? $ctrl.hoverValue : $ctrl.numericValue;
      return star <= active;
    };

    $ctrl.setHover = function (star) {
      if (!$ctrl.isReadonly) {
        $ctrl.hoverValue = star;
      }
    };

    $ctrl.clearHover = function () {
      $ctrl.hoverValue = 0;
    };

    $ctrl.selectStar = function (star) {
      if ($ctrl.isReadonly) {
        return;
      }
      $ctrl.numericValue = star;
      if (angular.isFunction($ctrl.onChange)) {
        $ctrl.onChange({ value: star });
      }
    };
  }
})();
