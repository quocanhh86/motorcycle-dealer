(function () {
  'use strict';

  var STORAGE_KEY = 'apex_theme';
  var VALID_THEMES = { light: true, dark: true };

  angular
    .module('app.core')
    .service('ThemeService', ThemeService);

  ThemeService.$inject = ['$rootScope', '$window'];
  function ThemeService($rootScope, $window) {
    var self = this;
    var activeTheme = 'light';

    function safeReadStorage() {
      try {
        if ($window && $window.localStorage && angular.isFunction($window.localStorage.getItem)) {
          var saved = $window.localStorage.getItem(STORAGE_KEY);
          if (saved && VALID_THEMES[saved]) {
            return saved;
          }
        }
      } catch (e) {
        // Storage unavailable in sandbox or private mode
      }
      return 'light';
    }

    function safeWriteStorage(theme) {
      try {
        if ($window && $window.localStorage && angular.isFunction($window.localStorage.setItem)) {
          $window.localStorage.setItem(STORAGE_KEY, theme);
        }
      } catch (e) {
        // Ignore storage quota or sandbox errors
      }
    }

    function applyDomTheme(theme) {
      try {
        var doc = ($window && $window.document) || (typeof document !== 'undefined' ? document : null);
        if (doc && doc.documentElement && angular.isFunction(doc.documentElement.setAttribute)) {
          doc.documentElement.setAttribute('data-theme', theme);
        }
      } catch (e) {
        // Guarded for Node VM sandbox
      }
    }

    self.init = function () {
      activeTheme = safeReadStorage();
      applyDomTheme(activeTheme);
      return activeTheme;
    };

    self.getTheme = function () {
      return activeTheme;
    };

    self.isDark = function () {
      return activeTheme === 'dark';
    };

    self.setTheme = function (nextTheme) {
      var normalized = nextTheme === 'dark' ? 'dark' : 'light';
      activeTheme = normalized;
      safeWriteStorage(normalized);
      applyDomTheme(normalized);
      if ($rootScope && angular.isFunction($rootScope.$broadcast)) {
        $rootScope.$broadcast('theme:changed', {
          theme: normalized,
          isDark: normalized === 'dark'
        });
      }
      return normalized;
    };

    self.toggleTheme = function () {
      return self.setTheme(activeTheme === 'dark' ? 'light' : 'dark');
    };

    // Apply initial theme immediately on service construction
    self.init();
  }
})();
