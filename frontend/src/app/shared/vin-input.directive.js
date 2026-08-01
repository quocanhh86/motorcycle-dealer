(function () {
  'use strict';

  var ISO_3779_VIN_REGEX = /^[A-HJ-NPR-Z0-9]{17}$/;
  var FORBIDDEN_VIN_CHARS_REGEX = /[IOQ]/i;

  angular
    .module('app.shared')
    .directive('vinInput', vinInputDirective);

  vinInputDirective.$inject = [];
  function vinInputDirective() {
    return {
      restrict: 'A',
      require: 'ngModel',
      link: function (scope, element, _attrs, ngModelCtrl) {
        element.addClass('font-mono-code');
        element.attr('maxlength', '17');
        element.attr('autocapitalize', 'characters');
        element.attr('spellcheck', 'false');

        ngModelCtrl.$parsers.push(function normalizeIsoVin(viewValue) {
          if (viewValue === null || viewValue === undefined) {
            return '';
          }
          var normalized = String(viewValue).replace(/\s+/g, '').toUpperCase();
          if (normalized !== viewValue) {
            ngModelCtrl.$setViewValue(normalized);
            ngModelCtrl.$render();
          }
          return normalized;
        });

        ngModelCtrl.$validators.noForbiddenVinChars = function (_modelValue, viewValue) {
          var candidate = String(viewValue || '');
          if (!candidate) {
            return true;
          }
          return !FORBIDDEN_VIN_CHARS_REGEX.test(candidate);
        };

        ngModelCtrl.$validators.isoVin = function (modelValue, viewValue) {
          var candidate = String(modelValue || viewValue || '');
          if (!candidate) {
            return true;
          }
          return ISO_3779_VIN_REGEX.test(candidate);
        };

        scope.$on('$destroy', angular.noop);
      }
    };
  }
})();
