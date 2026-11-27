(function () {
  'use strict';

  angular
    .module('app.shared')
    .component('emptyState', {
      bindings: {
        icon: '@',
        titleKey: '@',
        descKey: '@',
        actionKey: '@',
        title: '@',
        desc: '@',
        actionLabel: '@',
        onAction: '&'
      },
      controllerAs: '$ctrl',
      controller: EmptyStateController,
      template:
        '<div class="empty-state-box fade-slide-up">' +
          '<div class="empty-state-emblem" aria-hidden="true">' +
            '<app-icon name="{{ $ctrl.iconName }}" weight="duotone" size="28"></app-icon>' +
          '</div>' +
          '<h3 class="empty-state-title">' +
            '{{ $ctrl.titleKey ? ($ctrl.titleKey | t) : ($ctrl.title || (\'common.noData\' | t)) }}' +
          '</h3>' +
          '<p class="empty-state-desc" ng-if="$ctrl.descKey || $ctrl.desc">' +
            '{{ $ctrl.descKey ? ($ctrl.descKey | t) : $ctrl.desc }}' +
          '</p>' +
          '<button type="button" class="btn-pill-cta" ng-if="$ctrl.hasAction" ng-click="$ctrl.triggerAction()">' +
            '<span>{{ $ctrl.actionKey ? ($ctrl.actionKey | t) : $ctrl.actionLabel }}</span>' +
            '<span class="btn-pill-well" aria-hidden="true">' +
              '<app-icon name="arrow-right" weight="regular" size="15"></app-icon>' +
            '</span>' +
          '</button>' +
        '</div>'
    });

  EmptyStateController.$inject = [];
  function EmptyStateController() {
    var $ctrl = this;

    function syncBindings() {
      $ctrl.iconName = String($ctrl.icon || 'motorcycle').trim() || 'motorcycle';
      $ctrl.hasAction = Boolean(($ctrl.actionKey || $ctrl.actionLabel) && angular.isFunction($ctrl.onAction));
    }

    $ctrl.$onInit = syncBindings;
    $ctrl.$onChanges = syncBindings;

    $ctrl.triggerAction = function () {
      if (angular.isFunction($ctrl.onAction)) {
        $ctrl.onAction();
      }
    };
  }
})();
