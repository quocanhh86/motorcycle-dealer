(function () {
  'use strict';

  var VALID_VARIANTS = {
    cards: true,
    table: true,
    kpi: true,
    drawer: true,
    hero: true,
    passbook: true,
    detail: true
  };

  angular
    .module('app.shared')
    .component('skeletonLoader', {
      bindings: {
        variant: '@',
        count: '@'
      },
      controllerAs: '$ctrl',
      controller: SkeletonLoaderController,
      template:
        '<div class="skeleton-loader-root" role="status" aria-busy="true" ng-attr-aria-label="{{ \'a11y.loadingContent\' | t }}">' +
          /* Cards Grid Variant */
          '<div class="skeleton-grid-cards" ng-if="$ctrl.resolvedVariant === \'cards\'">' +
            '<div class="skeleton-card" ng-repeat="idx in $ctrl.items track by idx">' +
              '<div class="skeleton-block" style="width:100%;aspect-ratio:16/10;"></div>' +
              '<div class="skeleton-block" style="width:68%;height:18px;"></div>' +
              '<div class="skeleton-block" style="width:45%;height:14px;"></div>' +
              '<div class="skeleton-block" style="width:100%;height:36px;margin-top:4px;"></div>' +
            '</div>' +
          '</div>' +

          /* Table Rows Variant */
          '<div class="skeleton-table-stack" ng-if="$ctrl.resolvedVariant === \'table\'">' +
            '<div class="skeleton-table-row" ng-repeat="idx in $ctrl.items track by idx">' +
              '<div class="skeleton-block" style="height:16px;width:80%;"></div>' +
              '<div class="skeleton-block" style="height:16px;width:92%;"></div>' +
              '<div class="skeleton-block" style="height:16px;width:70%;"></div>' +
              '<div class="skeleton-block" style="height:16px;width:60%;"></div>' +
            '</div>' +
          '</div>' +

          /* KPI Row Variant */
          '<div class="skeleton-grid-kpi" ng-if="$ctrl.resolvedVariant === \'kpi\'">' +
            '<div class="skeleton-card" ng-repeat="idx in $ctrl.items track by idx">' +
              '<div class="skeleton-block" style="width:55%;height:12px;"></div>' +
              '<div class="skeleton-block" style="width:72%;height:28px;"></div>' +
              '<div class="skeleton-block" style="width:45%;height:12px;"></div>' +
            '</div>' +
          '</div>' +

          /* Drawer Stack Variant */
          '<div class="skeleton-table-stack" ng-if="$ctrl.resolvedVariant === \'drawer\'">' +
            '<div class="skeleton-card" ng-repeat="idx in $ctrl.items track by idx">' +
              '<div class="skeleton-block" style="width:60%;height:15px;"></div>' +
              '<div class="skeleton-block" style="width:95%;height:12px;"></div>' +
              '<div class="skeleton-block" style="width:78%;height:12px;"></div>' +
            '</div>' +
          '</div>' +

          /* Hero Banner Variant */
          '<div class="skeleton-card" ng-if="$ctrl.resolvedVariant === \'hero\'" style="min-height:240px;justify-content:flex-end;">' +
            '<div class="skeleton-block" style="width:220px;height:20px;"></div>' +
            '<div class="skeleton-block" style="width:65%;height:34px;"></div>' +
            '<div class="skeleton-block" style="width:48%;height:16px;"></div>' +
          '</div>' +

          /* Passbook Variant */
          '<div class="skeleton-card" ng-if="$ctrl.resolvedVariant === \'passbook\'" style="min-height:190px;">' +
            '<div class="skeleton-block" style="width:160px;height:14px;"></div>' +
            '<div class="skeleton-block" style="width:220px;height:36px;"></div>' +
            '<div class="skeleton-block" style="width:100%;height:10px;margin-top:8px;"></div>' +
            '<div class="skeleton-block" style="width:55%;height:14px;"></div>' +
          '</div>' +

          /* Detail View Variant */
          '<div class="skeleton-card" ng-if="$ctrl.resolvedVariant === \'detail\'">' +
            '<div class="skeleton-block" style="width:100%;aspect-ratio:16/9;max-height:260px;"></div>' +
            '<div class="skeleton-block" style="width:50%;height:24px;"></div>' +
            '<div class="skeleton-block" style="width:90%;height:14px;"></div>' +
            '<div class="skeleton-block" style="width:76%;height:14px;"></div>' +
          '</div>' +
        '</div>'
    });

  SkeletonLoaderController.$inject = [];
  function SkeletonLoaderController() {
    var $ctrl = this;

    function rebuild() {
      var rawVariant = String($ctrl.variant || 'cards').trim().toLowerCase();
      $ctrl.resolvedVariant = VALID_VARIANTS[rawVariant] ? rawVariant : 'cards';

      var parsedCount = Math.floor(Number($ctrl.count));
      var safeCount = (isFinite(parsedCount) && parsedCount > 0) ? Math.min(12, parsedCount) : 3;
      var arr = [];
      for (var i = 0; i < safeCount; i++) {
        arr.push(i);
      }
      $ctrl.items = arr;
    }

    $ctrl.$onInit = rebuild;
    $ctrl.$onChanges = rebuild;
  }
})();
