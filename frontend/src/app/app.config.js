(function () {
  'use strict';

  var runtimeOverrides = (window && window.__DEALER_APP_CONFIG__) || {};

  var APP_CONFIG = {
    appName: 'Motorcycle Dealer & Loyalty Platform',
    apiBaseUrl: runtimeOverrides.apiBaseUrl || 'http://localhost:5080/api/v1',
    hubUrl: runtimeOverrides.hubUrl || 'http://localhost:5080/hubs/notifications',
    defaultTimeZone: 'Asia/Ho_Chi_Minh',
    defaultLocale: 'vi-VN',
    enableDebugInfo: Boolean(runtimeOverrides.enableDebugInfo),
    signalrReconnectScheduleMs: [0, 2000, 5000, 10000, 30000]
  };

  angular
    .module('app.core')
    .constant('APP_CONFIG', APP_CONFIG)
    .config(configurePlatformProviders);

  configurePlatformProviders.$inject = ['$httpProvider', '$compileProvider', '$locationProvider', 'APP_CONFIG'];
  function configurePlatformProviders($httpProvider, $compileProvider, $locationProvider, appConfig) {
    $compileProvider.debugInfoEnabled(Boolean(appConfig.enableDebugInfo));
    $compileProvider.commentDirectivesEnabled(false);
    $compileProvider.cssClassDirectivesEnabled(false);

    $locationProvider.html5Mode({ enabled: true, requireBase: true });
    $locationProvider.hashPrefix('!');

    $httpProvider.interceptors.push('AuthHttpInterceptor');
  }
})();
