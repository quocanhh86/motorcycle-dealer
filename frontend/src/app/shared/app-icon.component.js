(function () {
  'use strict';

  var WEIGHT_CLASSES = {
    regular: 'ph',
    fill: 'ph-fill',
    duotone: 'ph-duotone'
  };

  angular
    .module('app.shared')
    .component('appIcon', {
      bindings: {
        name: '@',
        weight: '@',
        size: '@',
        label: '@',
        extraClass: '@'
      },
      controllerAs: '$ctrl',
      controller: AppIconController,
      template:
        '<i class="app-icon" ' +
           'ng-class="[$ctrl.weightClass, $ctrl.nameClass, $ctrl.extraClass]" ' +
           'ng-style="$ctrl.iconStyle" ' +
           'ng-attr-role="{{ $ctrl.label ? \'img\' : undefined }}" ' +
           'ng-attr-aria-label="{{ $ctrl.label || undefined }}" ' +
           'ng-attr-aria-hidden="{{ $ctrl.label ? undefined : \'true\' }}">' +
        '</i>'
    });

  AppIconController.$inject = [];
  function AppIconController() {
    var $ctrl = this;

    function syncVisuals() {
      var rawWeight = String($ctrl.weight || 'regular').trim().toLowerCase();
      $ctrl.weightClass = WEIGHT_CLASSES[rawWeight] || 'ph';

      var rawName = String($ctrl.name || 'circle').trim().toLowerCase().replace(/[^a-z0-9-]/g, '');
      $ctrl.nameClass = 'ph-' + (rawName || 'circle');

      var px = Number($ctrl.size);
      var resolvedPx = (isFinite(px) && px >= 10 && px <= 96) ? px : 18;
      $ctrl.iconStyle = {
        'font-size': resolvedPx + 'px',
        'width': resolvedPx + 'px',
        'height': resolvedPx + 'px'
      };
    }

    $ctrl.$onInit = syncVisuals;
    $ctrl.$onChanges = syncVisuals;
  }
})();
