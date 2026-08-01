(function () {
  'use strict';

  angular
    .module('app.shared')
    .directive('hasPermission', hasPermissionDirective);

  hasPermissionDirective.$inject = ['$rootScope', 'AuthService'];
  function hasPermissionDirective($rootScope, AuthService) {
    return {
      restrict: 'A',
      link: function (scope, element, attrs) {
        var disabledByDirective = false;

        function resolvePermissionList() {
          var rawAttr = (attrs.hasPermission || '').trim();
          if (!rawAttr) {
            return [];
          }
          if (rawAttr.charAt(0) === '[' || rawAttr.indexOf('$ctrl.') === 0) {
            var evaluated = scope.$eval(rawAttr);
            if (angular.isArray(evaluated)) {
              return evaluated;
            }
            return evaluated ? [String(evaluated)] : [];
          }
          var unquoted = rawAttr.replace(/^['"]|['"]$/g, '');
          return unquoted ? [unquoted] : [];
        }

        function evaluateAccess() {
          var required = resolvePermissionList();
          var mode = attrs.permissionMode || 'ALL';
          var allowed = AuthService.hasPermissions(required, mode);

          if (allowed) {
            element.removeClass('ng-hide');
            element.removeAttr('aria-hidden');
            if (disabledByDirective && !attrs.disabled) {
              element.prop('disabled', false);
              disabledByDirective = false;
            }
          } else {
            element.addClass('ng-hide');
            element.attr('aria-hidden', 'true');
            if (!element.prop('disabled')) {
              element.prop('disabled', true);
              disabledByDirective = true;
            }
          }
        }

        evaluateAccess();
        var offAuthChanged = $rootScope.$on('auth:changed', evaluateAccess);
        var offAttrObserve = attrs.$observe('hasPermission', evaluateAccess);

        scope.$on('$destroy', function () {
          offAuthChanged();
          if (angular.isFunction(offAttrObserve)) {
            offAttrObserve();
          }
        });
      }
    };
  }
})();
