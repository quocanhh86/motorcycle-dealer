(function () {
  'use strict';

  var SIGNALR_EVENTS = [
    'BookingCreated',
    'BookingStatusChanged',
    'MaintenanceReminderTriggered',
    'LoyaltyPointsUpdated',
    'TierUpgraded',
    'TestDriveCreated',
    'TestDriveStatusChanged',
    'ServiceReviewSubmitted',
    'WarrantyClaimCreated',
    'WarrantyClaimStatusChanged'
  ];

  var SENSITIVE_FIELD_RX = /password|passwd|pwd|token|secret|key|credential/i;

  angular
    .module('motorcycleDealerApp')
    .run(registerGlobalUserActionTracking);

  registerGlobalUserActionTracking.$inject = [
    '$window',
    '$document',
    '$rootScope',
    '$transitions',
    'UserActionLoggerService'
  ];
  function registerGlobalUserActionTracking(
    $window,
    $document,
    $rootScope,
    $transitions,
    UserActionLoggerService
  ) {
    function describeElement(el) {
      if (!el || el.nodeType !== 1) {
        return null;
      }
      var tag = String(el.tagName || '').toLowerCase();
      var aria = el.getAttribute && el.getAttribute('aria-label');
      var dataAction = el.getAttribute && el.getAttribute('data-action');
      var uiSref = el.getAttribute && el.getAttribute('ui-sref');
      var ngClick = el.getAttribute && el.getAttribute('ng-click');
      var text = (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
      if (text.length > 64) {
        text = text.slice(0, 64);
      }
      var label = dataAction || aria || text || uiSref || ngClick || el.id || tag;
      return {
        tag: tag,
        label: label,
        uiSref: uiSref || null,
        ngClick: ngClick ? ngClick.slice(0, 80) : null
      };
    }

    function findInteractiveTarget(startNode) {
      var node = startNode;
      var depth = 0;
      while (node && node.nodeType === 1 && depth < 6) {
        var tag = String(node.tagName || '').toLowerCase();
        var role = node.getAttribute && node.getAttribute('role');
        if (
          tag === 'button' ||
          tag === 'a' ||
          role === 'button' ||
          role === 'tab' ||
          (node.getAttribute && (node.getAttribute('ng-click') || node.getAttribute('ui-sref')))
        ) {
          return node;
        }
        node = node.parentElement;
        depth++;
      }
      return null;
    }

    // 1. Initial app session boot log
    UserActionLoggerService.track('App.SessionStarted', {
      category: 'Lifecycle',
      target: ($window && $window.location && $window.location.pathname) || '/',
      metadata: {
        userAgent: ($window && $window.navigator && $window.navigator.userAgent)
          ? String($window.navigator.userAgent).slice(0, 120)
          : 'browser'
      }
    });

    // 2. Every UI-Router navigation transition & RBAC guard redirect
    if ($transitions && angular.isFunction($transitions.onSuccess)) {
      $transitions.onSuccess({}, function (transition) {
        var toState = (transition && angular.isFunction(transition.to) && transition.to()) || {};
        var fromState = (transition && angular.isFunction(transition.from) && transition.from()) || {};
        var toData = toState.data || {};
        UserActionLoggerService.setCurrentScreen(toState.name, toData.screenId, toData.breadcrumb);

        UserActionLoggerService.track('Navigation.RouteChanged', {
          category: 'Navigation',
          screen: UserActionLoggerService.getCurrentScreen(),
          target: toState.name || toState.url || 'unknown',
          metadata: {
            fromState: fromState.name || 'initial',
            toState: toState.name || 'unknown',
            screenId: toData.screenId || '',
            breadcrumb: toData.breadcrumb || ''
          }
        });
      });
    }

    if ($transitions && angular.isFunction($transitions.onError)) {
      $transitions.onError({}, function (transition) {
        var toState = (transition && angular.isFunction(transition.to) && transition.to()) || {};
        var err = transition && angular.isFunction(transition.error) ? transition.error() : null;
        var detail = err && err.detail ? String(err.detail) : (err && err.message ? String(err.message) : 'TransitionError');
        UserActionLoggerService.track('Navigation.TransitionRejected', {
          category: 'Navigation',
          target: toState.name || 'unknown',
          outcome: 'Denied',
          metadata: { reason: detail.slice(0, 140) }
        });
      });
    }

    // 3. Authentication state transitions
    if ($rootScope && angular.isFunction($rootScope.$on)) {
      var previousUserId = null;
      $rootScope.$on('auth:changed', function (_evt, currentUser) {
        if (currentUser && currentUser.userId) {
          var isNewSignIn = previousUserId !== currentUser.userId;
          previousUserId = currentUser.userId;
          if (isNewSignIn) {
            UserActionLoggerService.track('Auth.SessionEstablished', {
              category: 'Auth',
              target: currentUser.role || 'Authenticated',
              metadata: {
                role: currentUser.role || '',
                dealerCode: currentUser.dealerCode || ''
              }
            });
          }
        } else if (previousUserId) {
          previousUserId = null;
          UserActionLoggerService.track('Auth.SessionCleared', {
            category: 'Auth',
            target: 'Anonymous'
          });
        }
      });

      // 4. SignalR connection & all 10 real-time events
      $rootScope.$on('signalr:status', function (_evt, status) {
        UserActionLoggerService.track('Realtime.ConnectionStatus', {
          category: 'Realtime',
          target: String(status || 'Unknown'),
          outcome: status === 'Offline' ? 'Failed' : 'Success'
        });
      });

      for (var i = 0; i < SIGNALR_EVENTS.length; i++) {
        (function bindSignalrAudit(evtName) {
          $rootScope.$on('signalr:' + evtName, function (_evt, payload) {
            var p = payload || {};
            UserActionLoggerService.track('Realtime.' + evtName, {
              category: 'Realtime',
              target: p.bookingNumber || p.claimNumber || p.reviewNumber || p.vin || p.newTierCode || evtName,
              metadata: {
                notificationId: p.notificationId ? String(p.notificationId) : ''
              }
            });
          });
        })(SIGNALR_EVENTS[i]);
      }
    }

    // 5. Document-level DOM interaction capture (clicks, form submits, safe filter/input changes)
    var docEl = ($document && $document[0]) || ($window && $window.document) || null;
    if (docEl && typeof docEl.addEventListener === 'function') {
      docEl.addEventListener('click', function onDocumentClick(event) {
        var interactive = findInteractiveTarget(event && event.target);
        if (!interactive) {
          return;
        }
        var info = describeElement(interactive);
        if (!info) {
          return;
        }
        UserActionLoggerService.track('Ui.Click', {
          category: 'UserInteraction',
          target: info.label,
          metadata: {
            element: info.tag,
            uiSref: info.uiSref || '',
            handler: info.ngClick || ''
          }
        });
      }, true);

      docEl.addEventListener('submit', function onDocumentSubmit(event) {
        var formEl = event && event.target;
        if (!formEl || String(formEl.tagName || '').toLowerCase() !== 'form') {
          return;
        }
        var formName = (formEl.getAttribute && (formEl.getAttribute('name') || formEl.getAttribute('ng-submit') || formEl.id)) || 'form';
        UserActionLoggerService.track('Ui.FormSubmit', {
          category: 'UserInteraction',
          target: String(formName).slice(0, 120)
        });
      }, true);

      docEl.addEventListener('change', function onDocumentChange(event) {
        var el = event && event.target;
        if (!el || el.nodeType !== 1) {
          return;
        }
        var tag = String(el.tagName || '').toLowerCase();
        if (tag !== 'select' && tag !== 'input' && tag !== 'textarea') {
          return;
        }
        var inputType = String(el.type || '').toLowerCase();
        var fieldKey = (el.getAttribute && (el.getAttribute('aria-label') || el.getAttribute('name') || el.getAttribute('ng-model') || el.id)) || tag;
        var isSensitive = inputType === 'password' || SENSITIVE_FIELD_RX.test(String(fieldKey));

        var safeVal = null;
        if (isSensitive) {
          safeVal = '[REDACTED]';
        } else if (inputType === 'checkbox' || inputType === 'radio') {
          safeVal = el.checked ? 'checked' : 'unchecked';
        } else if (tag === 'select') {
          safeVal = String(el.value || '').slice(0, 64);
        } else {
          safeVal = 'length:' + String(el.value || '').length;
        }

        UserActionLoggerService.track('Ui.InputChange', {
          category: 'UserInteraction',
          target: String(fieldKey).slice(0, 100),
          metadata: {
            control: tag + (inputType ? ':' + inputType : ''),
            value: safeVal
          }
        });
      }, true);

      docEl.addEventListener('visibilitychange', function onVisibilityChange() {
        if (docEl.visibilityState === 'hidden') {
          UserActionLoggerService.flush(true);
        }
      });
    }

    if ($window && typeof $window.addEventListener === 'function') {
      $window.addEventListener('pagehide', function () {
        UserActionLoggerService.flush(true);
      });
    }
  }
})();
