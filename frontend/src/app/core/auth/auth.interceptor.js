(function () {
  'use strict';

  angular
    .module('app.core')
    .factory('AuthHttpInterceptor', AuthHttpInterceptor);

  AuthHttpInterceptor.$inject = ['$q', '$injector', 'TokenStorageService'];
  function AuthHttpInterceptor($q, $injector, TokenStorageService) {
    var refreshInFlight = null;
    var pendingRequests = [];

    function isApiRequestUrl(url) {
      if (!angular.isString(url)) {
        return false;
      }
      return url.indexOf('/api/v1/') !== -1 || url.indexOf('/api/v1') !== -1;
    }

    function isAnonymousAuthUrl(url) {
      if (!angular.isString(url)) {
        return false;
      }
      return (
        url.indexOf('/auth/login') !== -1 ||
        url.indexOf('/auth/register') !== -1 ||
        url.indexOf('/auth/refresh') !== -1
      );
    }

    function isRefreshExcludedUrl(url) {
      if (!angular.isString(url)) {
        return false;
      }
      return isAnonymousAuthUrl(url) || url.indexOf('/auth/logout') !== -1;
    }

    function resolveLoginReturnParams($state) {
      var params = { reason: 'sessionExpired' };
      if ($state && $state.current && angular.isString($state.current.name)) {
        var activeName = $state.current.name;
        if (
          activeName &&
          activeName.indexOf('app.') === 0 &&
          activeName !== 'app.public.login' &&
          activeName !== 'app.public.register' &&
          activeName !== 'app.public.forbidden'
        ) {
          params.returnUrl = activeName;
          params.returnTo = activeName;
        }
      }
      return params;
    }

    function isActivityLogEndpoint(url) {
      if (!angular.isString(url)) {
        return false;
      }
      return url.indexOf('/activity-logs') !== -1 || url.indexOf('/health') !== -1;
    }

    function getLoggerSafe() {
      try {
        if ($injector && angular.isFunction($injector.has) && $injector.has('UserActionLoggerService')) {
          return $injector.get('UserActionLoggerService');
        }
      } catch (e) {
        return null;
      }
      return null;
    }

    function getI18nSafe() {
      try {
        if ($injector && angular.isFunction($injector.has) && $injector.has('I18nService')) {
          return $injector.get('I18nService');
        }
      } catch (e) {
        return null;
      }
      return null;
    }

    function extractRelativeApiPath(url) {
      if (!angular.isString(url)) {
        return '';
      }
      var idx = url.indexOf('/api/v1');
      return idx >= 0 ? url.slice(idx) : url;
    }

    return {
      request: function (config) {
        if (!config || !config.url || !isApiRequestUrl(config.url)) {
          return config;
        }

        config.headers = config.headers || {};
        config._actionLogStartMs = Date.now();

        if (!config.headers['Accept-Language']) {
          var i18n = getI18nSafe();
          config.headers['Accept-Language'] = (i18n && angular.isFunction(i18n.getLocale))
            ? i18n.getLocale()
            : 'vi-VN';
        }

        var logger = getLoggerSafe();
        if (logger && !config._skipActivityLog) {
          var corrHeaders = logger.buildCorrelationHeaders();
          if (!config.headers['X-Client-Platform']) {
            config.headers['X-Client-Platform'] = corrHeaders['X-Client-Platform'];
          }
          if (!config.headers['X-Client-Session-Id']) {
            config.headers['X-Client-Session-Id'] = corrHeaders['X-Client-Session-Id'];
          }
          if (!config.headers['X-Client-Screen']) {
            config.headers['X-Client-Screen'] = corrHeaders['X-Client-Screen'];
          }
          if (!config.headers['X-Correlation-Id']) {
            config.headers['X-Correlation-Id'] = corrHeaders['X-Correlation-Id'];
          }
          config._correlationId = config.headers['X-Correlation-Id'];
        }

        if (isAnonymousAuthUrl(config.url)) {
          return config;
        }

        var explicitRetryToken = config._retryAccessToken || null;
        var activeToken = explicitRetryToken || TokenStorageService.getAccessToken();
        if (activeToken) {
          config.headers.Authorization = 'Bearer ' + activeToken;
        }
        return config;
      },

      response: function (res) {
        var cfg = (res && res.config) || null;
        if (cfg && isApiRequestUrl(cfg.url) && !isActivityLogEndpoint(cfg.url) && !cfg._skipActivityLog) {
          var logger = getLoggerSafe();
          if (logger) {
            var durationMs = cfg._actionLogStartMs ? Math.max(0, Date.now() - cfg._actionLogStartMs) : null;
            var method = String(cfg.method || 'GET').toUpperCase();
            var relPath = extractRelativeApiPath(cfg.url);
            logger.track('Api.' + method + ' ' + relPath.split('?')[0], {
              category: 'Api',
              correlationId: cfg._correlationId || (cfg.headers && cfg.headers['X-Correlation-Id']),
              httpMethod: method,
              path: relPath,
              statusCode: res.status || 200,
              outcome: 'Success',
              durationMs: durationMs,
              syncToServer: false
            });
          }
        }
        return res;
      },

      responseError: function (rejection) {
        var originalConfig = (rejection && rejection.config) || {};
        if (
          originalConfig &&
          isApiRequestUrl(originalConfig.url) &&
          !isActivityLogEndpoint(originalConfig.url) &&
          !originalConfig._skipActivityLog
        ) {
          var errLogger = getLoggerSafe();
          if (errLogger) {
            var errDurationMs = originalConfig._actionLogStartMs
              ? Math.max(0, Date.now() - originalConfig._actionLogStartMs)
              : null;
            var errMethod = String(originalConfig.method || 'GET').toUpperCase();
            var errPath = extractRelativeApiPath(originalConfig.url);
            var errStatus = (rejection && typeof rejection.status === 'number') ? rejection.status : 0;
            errLogger.track('Api.' + errMethod + ' ' + errPath.split('?')[0], {
              category: 'Api',
              correlationId: originalConfig._correlationId || (originalConfig.headers && originalConfig.headers['X-Correlation-Id']),
              httpMethod: errMethod,
              path: errPath,
              statusCode: errStatus,
              outcome: errStatus === 401 || errStatus === 403 ? 'Denied' : (errStatus >= 500 ? 'ServerError' : 'Failed'),
              durationMs: errDurationMs,
              syncToServer: false
            });
          }
        }
        if (
          rejection &&
          rejection.status === 403 &&
          isApiRequestUrl(originalConfig.url) &&
          !isAnonymousAuthUrl(originalConfig.url)
        ) {
          var forbiddenCode = rejection.data && rejection.data.errorCode;
          if (forbiddenCode === 'ACCOUNT_LOCKED' || forbiddenCode === 'STAFF_DEALER_UNASSIGNED') {
            var LockedAuthService = $injector.get('AuthService');
            var lockedState = $injector.has('$state') ? $injector.get('$state') : null;
            LockedAuthService.clearSessionAndRealtime();
            if (lockedState && angular.isFunction(lockedState.go)) {
              lockedState.go('app.public.login', resolveLoginReturnParams(lockedState));
            }
            return $q.reject(rejection);
          }
        }

        if (
          !rejection ||
          rejection.status !== 401 ||
          originalConfig._retried ||
          !isApiRequestUrl(originalConfig.url) ||
          isRefreshExcludedUrl(originalConfig.url)
        ) {
          return $q.reject(rejection);
        }

        originalConfig._retried = true;
        var replayConfig = angular.extend({}, originalConfig, {
          _retried: true,
          headers: angular.extend({}, originalConfig.headers || {})
        });

        var deferred = $q.defer();
        pendingRequests.push({
          config: replayConfig,
          deferred: deferred
        });

        if (!refreshInFlight) {
          var AuthService = $injector.get('AuthService');
          var $http = $injector.get('$http');
          var $state = $injector.has('$state') ? $injector.get('$state') : null;

          refreshInFlight = AuthService.refreshTokens()
            .then(function (rotatedAccessToken) {
              var queued = pendingRequests.slice();
              pendingRequests.length = 0;
              refreshInFlight = null;

              for (var i = 0; i < queued.length; i++) {
                var item = queued[i];
                item.config._retryAccessToken = rotatedAccessToken;
                item.config.headers = item.config.headers || {};
                item.config.headers.Authorization = 'Bearer ' + rotatedAccessToken;
                $http(item.config).then(item.deferred.resolve, item.deferred.reject);
              }
            })
            .catch(function (refreshErr) {
              var queued = pendingRequests.slice();
              pendingRequests.length = 0;
              refreshInFlight = null;

              AuthService.clearSessionAndRealtime();
              if ($state && angular.isFunction($state.go)) {
                $state.go('app.public.login', resolveLoginReturnParams($state));
              }

              for (var j = 0; j < queued.length; j++) {
                queued[j].deferred.reject(refreshErr);
              }
            });
        }

        return deferred.promise;
      }
    };
  }
})();
