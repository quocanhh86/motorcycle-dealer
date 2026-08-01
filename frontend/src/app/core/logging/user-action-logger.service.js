(function () {
  'use strict';

  var STORAGE_LOGS_KEY = 'mdlmp.client.user_action_logs';
  var STORAGE_SESSION_KEY = 'mdlmp.client.session_id';
  var MAX_LOCAL_LOGS = 300;
  var FLUSH_BATCH_SIZE = 10;
  var FLUSH_INTERVAL_MS = 3000;
  var SENSITIVE_KEY_RX = /password|passwd|pwd|token|secret|authorization|cookie|apikey|api_key|signingkey|passphrase|credential|jwt/i;

  angular
    .module('app.core')
    .service('UserActionLoggerService', UserActionLoggerService);

  UserActionLoggerService.$inject = ['$window', '$rootScope', '$injector', 'APP_CONFIG', 'TokenStorageService'];
  function UserActionLoggerService($window, $rootScope, $injector, APP_CONFIG, TokenStorageService) {
    var self = this;
    var seqCounter = 0;
    var flushTimer = null;
    var flushInFlight = false;
    var pendingServerQueue = [];
    var currentScreen = {
      state: 'app.public.showroom',
      screenId: 'SCR-PUB-01',
      breadcrumb: 'Showroom'
    };

    var sessionId = resolveOrCreateSessionId();
    var localLogs = loadPersistedLogs();

    function resolveStorage(kind) {
      try {
        return ($window && $window[kind]) || null;
      } catch (e) {
        return null;
      }
    }

    function generateId(prefix) {
      seqCounter++;
      var rand = Math.floor(Math.random() * 0xffffff).toString(16);
      return (prefix || 'evt') + '-' + Date.now().toString(36) + '-' + seqCounter + '-' + rand;
    }

    function resolveOrCreateSessionId() {
      var store = resolveStorage('sessionStorage');
      try {
        if (store) {
          var existing = store.getItem(STORAGE_SESSION_KEY);
          if (existing && existing.trim()) {
            return existing.trim();
          }
          var created = generateId('web');
          store.setItem(STORAGE_SESSION_KEY, created);
          return created;
        }
      } catch (e) {
        // Ignore storage restrictions.
      }
      return generateId('web');
    }

    function loadPersistedLogs() {
      var store = resolveStorage('localStorage');
      if (!store) {
        return [];
      }
      try {
        var raw = store.getItem(STORAGE_LOGS_KEY);
        if (!raw) {
          return [];
        }
        var parsed = JSON.parse(raw);
        return angular.isArray(parsed) ? parsed.slice(0, MAX_LOCAL_LOGS) : [];
      } catch (e) {
        return [];
      }
    }

    function persistLocalLogs() {
      var store = resolveStorage('localStorage');
      if (!store) {
        return;
      }
      try {
        store.setItem(STORAGE_LOGS_KEY, JSON.stringify(localLogs.slice(0, MAX_LOCAL_LOGS)));
      } catch (e) {
        // Ignore quota errors in restricted browsing modes.
      }
    }

    function cleanString(value, maxLen) {
      if (value === undefined || value === null) {
        return null;
      }
      var text = String(value).replace(/[\r\n\t]+/g, ' ').trim();
      if (!text) {
        return null;
      }
      return text.length > maxLen ? text.slice(0, maxLen) : text;
    }

    function sanitizeMetadata(rawMeta) {
      if (!rawMeta || !angular.isObject(rawMeta)) {
        return null;
      }
      var out = {};
      var keys = Object.keys(rawMeta);
      var count = 0;
      for (var i = 0; i < keys.length && count < 16; i++) {
        var key = cleanString(keys[i], 64);
        if (!key) {
          continue;
        }
        if (SENSITIVE_KEY_RX.test(key)) {
          out[key] = '[REDACTED]';
        } else {
          var val = cleanString(rawMeta[keys[i]], 240);
          if (val !== null) {
            out[key] = val;
          }
        }
        count++;
      }
      return count > 0 ? out : null;
    }

    function getActiveUserSnapshot() {
      try {
        if ($injector && $injector.has('AuthService')) {
          var authSvc = $injector.get('AuthService');
          if (authSvc && angular.isFunction(authSvc.getCurrentUser)) {
            return authSvc.getCurrentUser() || null;
          }
        }
      } catch (e) {
        // Safe during bootstrap before AuthService resolves.
      }
      return null;
    }

    function resolveIngestEndpoint() {
      var apiBase = String((APP_CONFIG && APP_CONFIG.apiBaseUrl) || 'http://localhost:5080/api/v1').replace(/\/+$/, '');
      return apiBase + '/activity-logs';
    }

    function scheduleFlush() {
      if (pendingServerQueue.length >= FLUSH_BATCH_SIZE) {
        self.flush(false);
        return;
      }
      if (flushTimer || typeof setTimeout !== 'function') {
        return;
      }
      flushTimer = setTimeout(function () {
        flushTimer = null;
        self.flush(false);
      }, FLUSH_INTERVAL_MS);
    }

    function emitConsoleRecord(entry) {
      try {
        if (typeof console !== 'undefined' && typeof console.info === 'function') {
          console.info(
            '[USER-ACTION]',
            entry.category + ' :: ' + entry.action,
            '| screen=' + (entry.screen || '-'),
            '| target=' + (entry.target || '-'),
            '| outcome=' + entry.outcome,
            '| cid=' + entry.correlationId
          );
        }
      } catch (e) {
        // Never fail on console unavailability.
      }
    }

    self.getSessionId = function () {
      return sessionId;
    };

    self.createCorrelationId = function () {
      return generateId('cid');
    };

    self.setCurrentScreen = function (stateName, screenId, breadcrumb) {
      currentScreen = {
        state: cleanString(stateName, 96) || currentScreen.state,
        screenId: cleanString(screenId, 32) || '',
        breadcrumb: cleanString(breadcrumb, 96) || ''
      };
    };

    self.getCurrentScreen = function () {
      return currentScreen.screenId
        ? (currentScreen.screenId + ' (' + currentScreen.state + ')')
        : currentScreen.state;
    };

    self.buildCorrelationHeaders = function (actionHint) {
      var headers = {
        'X-Client-Platform': 'WebSpa',
        'X-Client-Session-Id': sessionId,
        'X-Client-Screen': self.getCurrentScreen(),
        'X-Correlation-Id': self.createCorrelationId()
      };
      var cleanHint = cleanString(actionHint, 96);
      if (cleanHint) {
        headers['X-Client-Action'] = cleanHint;
      }
      return headers;
    };

    self.track = function (action, details) {
      var cleanAction = cleanString(action, 128);
      if (!cleanAction) {
        return null;
      }

      var opts = (details && angular.isObject(details)) ? details : {};
      var activeUser = getActiveUserSnapshot();
      var entry = {
        eventId: generateId('webact'),
        timestampUtc: new Date().toISOString(),
        source: 'WebClient',
        clientPlatform: 'WebSpa',
        sessionId: sessionId,
        correlationId: cleanString(opts.correlationId, 64) || self.createCorrelationId(),
        userId: (activeUser && activeUser.userId) || null,
        email: (activeUser && activeUser.email) || null,
        role: (activeUser && activeUser.role) || 'Anonymous',
        dealerId: (activeUser && activeUser.dealerId) || null,
        category: cleanString(opts.category, 48) || 'UserInteraction',
        action: cleanAction,
        screen: cleanString(opts.screen, 120) || self.getCurrentScreen(),
        target: cleanString(opts.target, 200),
        httpMethod: cleanString(opts.httpMethod, 16),
        path: cleanString(opts.path, 320),
        statusCode: typeof opts.statusCode === 'number' ? opts.statusCode : null,
        outcome: cleanString(opts.outcome, 32) || 'Success',
        durationMs: typeof opts.durationMs === 'number' ? Math.max(0, Math.round(opts.durationMs)) : null,
        metadata: sanitizeMetadata(opts.metadata)
      };

      localLogs.unshift(entry);
      if (localLogs.length > MAX_LOCAL_LOGS) {
        localLogs.length = MAX_LOCAL_LOGS;
      }
      persistLocalLogs();
      emitConsoleRecord(entry);

      if (opts.syncToServer !== false) {
        pendingServerQueue.push({
          eventId: entry.eventId,
          timestampUtc: entry.timestampUtc,
          source: entry.source,
          clientPlatform: entry.clientPlatform,
          sessionId: entry.sessionId,
          correlationId: entry.correlationId,
          category: entry.category,
          action: entry.action,
          screen: entry.screen,
          target: entry.target,
          httpMethod: entry.httpMethod,
          path: entry.path,
          statusCode: entry.statusCode,
          outcome: entry.outcome,
          durationMs: entry.durationMs,
          metadata: entry.metadata
        });
        if (pendingServerQueue.length > 200) {
          pendingServerQueue.splice(0, pendingServerQueue.length - 200);
        }
        scheduleFlush();
      }

      if ($rootScope && angular.isFunction($rootScope.$broadcast)) {
        $rootScope.$broadcast('activityLog:updated', entry);
      }

      return entry;
    };

    self.flush = function (useKeepalive) {
      if (pendingServerQueue.length === 0) {
        return;
      }

      var fetchFn = ($window && typeof $window.fetch === 'function')
        ? $window.fetch.bind($window)
        : (typeof fetch === 'function' ? fetch : null);
      if (!fetchFn) {
        return;
      }

      if (flushInFlight && !useKeepalive) {
        return;
      }

      var batch = pendingServerQueue.splice(0, 40);
      var headers = {
        'Content-Type': 'application/json',
        'X-Client-Platform': 'WebSpa',
        'X-Client-Session-Id': sessionId,
        'X-Client-Screen': self.getCurrentScreen(),
        'X-Correlation-Id': self.createCorrelationId()
      };

      var accessToken = TokenStorageService && angular.isFunction(TokenStorageService.getAccessToken)
        ? TokenStorageService.getAccessToken()
        : null;
      if (accessToken) {
        headers.Authorization = 'Bearer ' + accessToken;
      }

      flushInFlight = true;
      try {
        var reqPromise = fetchFn(resolveIngestEndpoint(), {
          method: 'POST',
          headers: headers,
          body: JSON.stringify({ events: batch }),
          keepalive: Boolean(useKeepalive)
        });
        if (reqPromise && angular.isFunction(reqPromise.then)) {
          reqPromise
            .then(function (resp) {
              if (!resp || !resp.ok) {
                // Re-queue once on transient server unavailability without growing unbounded
                if (pendingServerQueue.length < 120) {
                  pendingServerQueue = batch.concat(pendingServerQueue);
                }
              }
            })
            .catch(function () {
              if (pendingServerQueue.length < 120) {
                pendingServerQueue = batch.concat(pendingServerQueue);
              }
            })
            .finally(function () {
              flushInFlight = false;
            });
        } else {
          flushInFlight = false;
        }
      } catch (e) {
        flushInFlight = false;
      }
    };

    self.getLocalLogs = function (searchTerm, categoryFilter) {
      var list = localLogs.slice();
      var term = cleanString(searchTerm, 80);
      var cat = cleanString(categoryFilter, 48);
      if (!term && !cat) {
        return list;
      }
      var lowerTerm = term ? term.toLowerCase() : '';
      var lowerCat = cat ? cat.toLowerCase() : '';
      return list.filter(function (entry) {
        if (lowerCat && String(entry.category || '').toLowerCase() !== lowerCat) {
          return false;
        }
        if (!lowerTerm) {
          return true;
        }
        var hay = [
          entry.action,
          entry.category,
          entry.screen,
          entry.target,
          entry.path,
          entry.outcome,
          entry.correlationId
        ].join(' ').toLowerCase();
        return hay.indexOf(lowerTerm) !== -1;
      });
    };

    self.getPendingCount = function () {
      return pendingServerQueue.length;
    };

    self.clearLocalLogs = function () {
      localLogs = [];
      persistLocalLogs();
      if ($rootScope && angular.isFunction($rootScope.$broadcast)) {
        $rootScope.$broadcast('activityLog:updated', null);
      }
    };
  }
})();
