(function () {
  'use strict';

  angular.module('app.core', ['ui.router']);
  angular.module('app.shared', ['app.core']);
  angular.module('app.features.public', ['app.core', 'app.shared']);
  angular.module('app.features.customer', ['app.core', 'app.shared']);
  angular.module('app.features.staff', ['app.core', 'app.shared']);
  angular.module('app.features.admin', ['app.core', 'app.shared']);

  angular.module('motorcycleDealerApp', [
    'ui.router',
    'app.core',
    'app.shared',
    'app.features.public',
    'app.features.customer',
    'app.features.staff',
    'app.features.admin'
  ]);
})();
