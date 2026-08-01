(function () {
  'use strict';

  var CANONICAL_HUB_EVENTS = [
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

  angular
    .module('app.core')
    .service('NotificationStore', NotificationStore)
    .service('SignalRClientService', SignalRClientService);

  NotificationStore.$inject = [];
  function NotificationStore() {
    var items = [];
    var explicitUnreadCount = null;
    var rtSeq = 0;

    function countUnreadInList(list) {
      var count = 0;
      for (var i = 0; i < list.length; i++) {
        if (!list[i].isRead) {
          count++;
        }
      }
      return count;
    }

    this.getAll = function (unreadOnly) {
      if (!unreadOnly) {
        return items.slice();
      }
      return items.filter(function (n) { return !n.isRead; });
    };

    this.getUnreadCount = function () {
      if (explicitUnreadCount !== null) {
        return Math.max(0, explicitUnreadCount);
      }
      return countUnreadInList(items);
    };

    function normalizeStoreItems(serverItems) {
      var incoming = angular.isArray(serverItems) ? serverItems : [];
      var seen = Object.create(null);
      var normalized = [];
      for (var i = 0; i < incoming.length; i++) {
        var raw = incoming[i] || {};
        var nid = String(raw.notificationId || raw.id || ('srv-' + i));
        if (seen[nid]) {
          continue;
        }
        seen[nid] = true;
        var ts = raw.occurredAtUtc || raw.createdAtUtc || new Date().toISOString();
        normalized.push({
          id: nid,
          notificationId: nid,
          eventType: raw.eventType || 'SystemAlert',
          title: raw.title || raw.eventType || 'Notification',
          message: raw.message || '',
          payloadJson: raw.payloadJson !== undefined ? raw.payloadJson : null,
          occurredAtUtc: ts,
          createdAtUtc: ts,
          isRead: Boolean(raw.isRead)
        });
      }
      return normalized;
    }

    function applyExplicitUnreadCount(serverUnreadCount) {
      if (serverUnreadCount !== undefined && serverUnreadCount !== null && !isNaN(Number(serverUnreadCount))) {
        explicitUnreadCount = Math.max(0, Math.floor(Number(serverUnreadCount)));
      } else {
        explicitUnreadCount = null;
      }
    }

    this.setServerFeed = function (serverItems, serverUnreadCount) {
      items = normalizeStoreItems(serverItems).slice(0, 50);
      applyExplicitUnreadCount(serverUnreadCount);
    };

    // Merges an unread-only snapshot without wiping previously cached read items so Unread/All toggles seamlessly
    this.mergeUnreadFeed = function (unreadServerItems, serverUnreadCount) {
      var normalizedUnread = normalizeStoreItems(unreadServerItems);
      var unreadIdSet = Object.create(null);
      for (var i = 0; i < normalizedUnread.length; i++) {
        normalizedUnread[i].isRead = false;
        unreadIdSet[normalizedUnread[i].notificationId] = true;
      }

      var preservedRead = [];
      for (var j = 0; j < items.length; j++) {
        var existing = items[j];
        if (!unreadIdSet[existing.notificationId] && existing.isRead) {
          preservedRead.push(existing);
        }
      }

      var combined = normalizedUnread.concat(preservedRead);
      combined.sort(function (a, b) {
        return String(b.occurredAtUtc || '').localeCompare(String(a.occurredAtUtc || ''));
      });
      items = combined.slice(0, 50);
      applyExplicitUnreadCount(serverUnreadCount);
    };

    this.clear = function () {
      items = [];
      explicitUnreadCount = 0;
    };

    this.prependNotification = function (entry) {
      var safeEntry = entry || {};
      rtSeq++;
      var nid = String(safeEntry.notificationId || safeEntry.id || ('rt-' + Date.now() + '-' + rtSeq));
      var ts = safeEntry.occurredAtUtc || safeEntry.createdAtUtc || new Date().toISOString();

      for (var i = 0; i < items.length; i++) {
        if (items[i].notificationId === nid) {
          return items[i];
        }
      }

      var created = {
        id: nid,
        notificationId: nid,
        eventType: safeEntry.eventType || 'SystemAlert',
        title: safeEntry.title || safeEntry.eventType || 'Real-Time Alert',
        message: safeEntry.message || '',
        payloadJson: safeEntry.payloadJson !== undefined ? safeEntry.payloadJson : null,
        occurredAtUtc: ts,
        createdAtUtc: ts,
        isRead: false
      };
      items.unshift(created);
      if (items.length > 50) {
        items.length = 50;
      }
      if (explicitUnreadCount !== null) {
        explicitUnreadCount = Math.max(0, explicitUnreadCount) + 1;
      }
      return created;
    };

    this.markAsRead = function (notificationId) {
      var changed = false;
      for (var i = 0; i < items.length; i++) {
        if (items[i].notificationId === notificationId) {
          if (!items[i].isRead) {
            items[i].isRead = true;
            items[i].readAtUtc = new Date().toISOString();
            changed = true;
          }
          break;
        }
      }
      if (changed && explicitUnreadCount !== null) {
        explicitUnreadCount = Math.max(0, explicitUnreadCount - 1);
      }
      return changed;
    };

    this.markAllRead = function () {
      var nowIso = new Date().toISOString();
      for (var i = 0; i < items.length; i++) {
        if (!items[i].isRead) {
          items[i].isRead = true;
          items[i].readAtUtc = nowIso;
        }
      }
      if (explicitUnreadCount !== null) {
        explicitUnreadCount = 0;
      }
    };
  }

  SignalRClientService.$inject = [
    '$q',
    '$rootScope',
    'APP_CONFIG',
    'TokenStorageService',
    'NotificationStore',
    'ToastService',
    'I18nService'
  ];
  function SignalRClientService(
    $q,
    $rootScope,
    APP_CONFIG,
    TokenStorageService,
    NotificationStore,
    ToastService,
    I18nService
  ) {
    var self = this;
    var connection = null;
    var activeTokenSnapshot = null;
    var connectionStatus = 'Disconnected';
    var subscribersByEvent = Object.create(null);
    var desiredMode = 'stopped';
    var lifecycleChain = $q.resolve();
    var inFlightStartPromise = null;
    var lastSeenUtc = new Date().toISOString();

    function notifyAsyncDigest() {
      if ($rootScope && angular.isFunction($rootScope.$applyAsync)) {
        $rootScope.$applyAsync(angular.noop);
      }
    }

    function wrapNativePromise(thenable) {
      if (!thenable || !angular.isFunction(thenable.then)) {
        return $q.resolve(thenable);
      }
      if (angular.isFunction($q.defer)) {
        var deferred = $q.defer();
        thenable.then(
          function (val) {
            deferred.resolve(val);
            notifyAsyncDigest();
          },
          function (err) {
            deferred.reject(err);
            notifyAsyncDigest();
          }
        );
        return deferred.promise;
      }
      if (angular.isFunction($q.when)) {
        return $q.when(thenable);
      }
      return thenable;
    }

    function setConnectionStatus(nextStatus) {
      connectionStatus = nextStatus;
      $rootScope.$applyAsync(function () {
        $rootScope.$broadcast('signalr:status', connectionStatus);
      });
    }

    function dispatchIncomingHubEvent(eventName, payload) {
      var safePayload = payload || {};
      $rootScope.$applyAsync(function () {
        if (safePayload.occurredAtUtc) {
          lastSeenUtc = safePayload.occurredAtUtc;
        } else {
          lastSeenUtc = new Date().toISOString();
        }
        var summary = buildNotificationSummary(eventName, safePayload);
        NotificationStore.prependNotification({
          notificationId: safePayload.notificationId,
          eventType: eventName,
          title: summary.title,
          message: summary.message,
          payloadJson: safePayload,
          occurredAtUtc: safePayload.occurredAtUtc
        });

        ToastService.push({
          intent: summary.intent,
          title: summary.title,
          message: summary.message
        });

        var listeners = (subscribersByEvent[eventName] || []).slice();
        for (var i = 0; i < listeners.length; i++) {
          listeners[i](safePayload);
        }

        $rootScope.$broadcast('signalr:' + eventName, safePayload);
        $rootScope.$broadcast('signalr:live-pulse', {
          eventName: eventName,
          payload: safePayload,
          occurredAtUtc: lastSeenUtc
        });
      });
    }

    function translateText(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback;
    }

    function translateEnum(group, code, fallback) {
      if (I18nService && angular.isFunction(I18nService.enumLabel) && code) {
        return I18nService.enumLabel(group, code);
      }
      return code || fallback || '';
    }

    function buildNotificationSummary(eventName, payload) {
      if (eventName === 'BookingCreated') {
        var bkNum = payload.bookingNumber || 'LIVE';
        var bkModel = payload.motorcycleModelName || translateText('common.vehicle', null, 'Motorcycle');
        var bkVin = payload.vin || 'N/A';
        var bkStart = payload.startTime || '';
        return {
          intent: 'info',
          title: translateText('common.signalr.bookingCreatedTitle', { bookingNumber: bkNum }, 'New Workshop Booking • ' + bkNum),
          message: translateText(
            'common.signalr.bookingCreatedMessage',
            { model: bkModel, vin: bkVin, startTime: bkStart },
            bkModel + ' (VIN: ' + bkVin + ') scheduled ' + bkStart
          )
        };
      }
      if (eventName === 'BookingStatusChanged') {
        var bkStNum = payload.bookingNumber || 'Booking';
        var bkStLabel = translateEnum('BookingStatus', payload.newStatus, 'Updated');
        return {
          intent: 'success',
          title: translateText('common.signalr.bookingStatusTitle', null, 'Booking Status Updated'),
          message: translateText(
            'common.signalr.bookingStatusMessage',
            { bookingNumber: bkStNum, status: bkStLabel },
            bkStNum + ' transitioned to ' + (payload.newStatus || 'Updated')
          )
        };
      }
      if (eventName === 'MaintenanceReminderTriggered') {
        var remVin = payload.vin || '';
        return {
          intent: 'warning',
          title: translateText('common.signalr.reminderTitle', null, 'VIN Maintenance Reminder Due'),
          message: translateText(
            'common.signalr.reminderMessage',
            { vin: remVin },
            'VIN ' + remVin + ' approaching scheduled OEM milestone.'
          )
        };
      }
      if (eventName === 'LoyaltyPointsUpdated') {
        var deltaVal = payload.pointsDelta || 0;
        var deltaStr = (deltaVal >= 0 ? '+' : '') + deltaVal;
        return {
          intent: 'success',
          title: translateText('common.signalr.loyaltyUpdatedTitle', null, 'Loyalty Balance Updated'),
          message: translateText(
            'common.signalr.loyaltyUpdatedMessage',
            { delta: deltaStr },
            'Available balance updated (' + deltaStr + ' pts).'
          )
        };
      }
      if (eventName === 'TierUpgraded') {
        var resolvedMultiplier = payload.newPointsMultiplier !== undefined
          ? payload.newPointsMultiplier
          : (payload.newMultiplier || '');
        var tierLabel = translateEnum('LoyaltyTierCode', payload.newTierCode, 'Higher Tier');
        return {
          intent: 'success',
          title: translateText('common.signalr.tierUpgradedTitle', null, 'Membership Tier Upgraded!'),
          message: translateText(
            'common.signalr.tierUpgradedMessage',
            { tier: tierLabel, multiplier: resolvedMultiplier },
            'Promoted to ' + (payload.newTierCode || 'Higher Tier') + ' (' + resolvedMultiplier + 'x multiplier).'
          )
        };
      }
      if (eventName === 'TestDriveCreated') {
        var tdNum = payload.bookingNumber || 'LIVE';
        var tdModel = payload.motorcycleModelName || translateText('common.vehicle', null, 'Motorcycle');
        var tdDate = payload.preferredDate ? String(payload.preferredDate).slice(0, 10) : '';
        var tdStart = payload.startTime ? String(payload.startTime).slice(0, 5) : '';
        var custSuffix = payload.customerFullName ? (' — ' + payload.customerFullName) : '';
        var schedSuffix = tdDate ? (' (' + tdDate + (tdStart ? (' ' + tdStart) : '') + ')') : '';
        return {
          intent: 'info',
          title: translateText('common.signalr.testDriveCreatedTitle', { bookingNumber: tdNum }, 'New Test-Drive Request • ' + tdNum),
          message: translateText(
            'common.signalr.testDriveCreatedMessage',
            { model: tdModel, customerSuffix: custSuffix, scheduleSuffix: schedSuffix },
            tdModel + (payload.customerFullName ? (' for ' + payload.customerFullName) : '') + (tdDate ? (' scheduled ' + tdDate + (tdStart ? (' ' + tdStart) : '')) : '')
          )
        };
      }
      if (eventName === 'TestDriveStatusChanged') {
        var tdStatus = payload.newStatus || payload.status || 'Updated';
        var tdStatusLabel = translateEnum('TestDriveStatus', tdStatus, 'Updated');
        var tdStNum = payload.bookingNumber || 'Test Drive';
        var vinLabel = translateText('common.vin', null, 'Demo VIN');
        var vinSuffix = payload.assignedVin ? (' • ' + vinLabel + ': ' + payload.assignedVin) : '';
        return {
          intent: tdStatus === 'Cancelled' || tdStatus === 'NoShow' ? 'warning' : 'success',
          title: translateText('common.signalr.testDriveStatusTitle', null, 'Test-Drive Status Updated'),
          message: translateText(
            'common.signalr.testDriveStatusMessage',
            { bookingNumber: tdStNum, status: tdStatusLabel, vinSuffix: vinSuffix },
            tdStNum + ' transitioned to ' + tdStatus + (payload.assignedVin ? (' • Demo VIN: ' + payload.assignedVin) : '')
          )
        };
      }
      if (eventName === 'ServiceReviewSubmitted') {
        var rawStars = payload.rating !== undefined ? payload.rating : payload.Rating;
        var stars = Math.max(1, Math.min(5, Math.floor(Number(rawStars) || 5)));
        var revNum = payload.reviewNumber || payload.ReviewNumber || payload.invoiceNumber || 'LIVE';
        var revModel = payload.motorcycleModelName || payload.MotorcycleModelName || translateText('common.vehicle', null, 'Workshop Service');
        var revCustomer = payload.customerFullName || payload.CustomerFullName || translateText('common.customer', null, 'Customer');
        var rawBonus = payload.bonusPointsAwarded !== undefined ? payload.bonusPointsAwarded : payload.BonusPointsAwarded;
        var bonusSuffix = rawBonus ? (' (+' + rawBonus + ' ' + translateText('common.ptsShort', null, 'pts') + ')') : '';
        return {
          intent: stars >= 4 ? 'success' : 'warning',
          title: translateText(
            'common.signalr.reviewSubmittedTitle',
            { stars: stars, reviewNumber: revNum },
            'New ' + stars + '/5 CSAT Review • ' + revNum
          ),
          message: translateText(
            'common.signalr.reviewSubmittedMessage',
            { customer: revCustomer, model: revModel, stars: stars, bonusSuffix: bonusSuffix },
            revCustomer + ' rated ' + revModel + ' ' + stars + '/5' + bonusSuffix
          )
        };
      }
      if (eventName === 'WarrantyClaimCreated') {
        var clmNum = payload.claimNumber || payload.ClaimNumber || 'LIVE';
        var clmVin = payload.vin || payload.vinSnapshot || payload.Vin || payload.VinSnapshot || 'N/A';
        var rawDefectCat = payload.defectCategory || payload.DefectCategory || 'OEM Defect';
        var defectCat = translateEnum('WarrantyDefectCategory', rawDefectCat, rawDefectCat);
        var clmModel = payload.motorcycleModelName || payload.MotorcycleModelName || translateText('common.vehicle', null, 'Motorcycle');
        var wcCustSuffix = payload.customerFullName ? (' — ' + payload.customerFullName) : '';
        return {
          intent: 'info',
          title: translateText('common.signalr.warrantyCreatedTitle', { claimNumber: clmNum }, 'New VIN Warranty Claim • ' + clmNum),
          message: translateText(
            'common.signalr.warrantyCreatedMessage',
            { model: clmModel, vin: clmVin, category: defectCat, customerSuffix: wcCustSuffix },
            clmModel + ' (VIN: ' + clmVin + ') • ' + rawDefectCat + (payload.customerFullName ? (' for ' + payload.customerFullName) : '')
          )
        };
      }
      if (eventName === 'WarrantyClaimStatusChanged') {
        var wcStatus = payload.newStatus || payload.status || payload.NewStatus || 'Updated';
        var wcStatusLabel = translateEnum('WarrantyClaimStatus', wcStatus, 'Updated');
        var wcNum = payload.claimNumber || payload.ClaimNumber || 'Warranty Claim';
        var rawCovered = payload.coveredAmountVnd !== undefined ? payload.coveredAmountVnd : payload.CoveredAmountVnd;
        var coveredAmt = Number(rawCovered) || 0;
        var formattedCovered = (I18nService && angular.isFunction(I18nService.formatVnd))
          ? I18nService.formatVnd(coveredAmt)
          : (Math.round(coveredAmt).toLocaleString('vi-VN') + ' ₫');
        var coveredSuffix = coveredAmt > 0 ? (' • ' + formattedCovered) : '';
        return {
          intent: wcStatus === 'Rejected' ? 'warning' : 'success',
          title: translateText('common.signalr.warrantyStatusTitle', { status: wcStatusLabel }, 'Warranty Claim ' + wcStatus),
          message: translateText(
            'common.signalr.warrantyStatusMessage',
            { claimNumber: wcNum, status: wcStatusLabel, coveredSuffix: coveredSuffix },
            wcNum + ' transitioned to ' + wcStatus + coveredSuffix
          )
        };
      }
      return {
        intent: 'info',
        title: translateEnum('NotificationEventType', eventName, eventName),
        message: translateText('common.signalr.genericMessage', null, 'Real-time event received from /hubs/notifications.')
      };
    }

    self.getCanonicalEvents = function () {
      return CANONICAL_HUB_EVENTS.slice();
    };

    self.getConnectionStatus = function () {
      return connectionStatus;
    };

    // Registers an event handler and returns an explicit disposer function for $ctrl.$onDestroy (RISK-04)
    self.on = function (eventName, handlerFn) {
      if (!angular.isString(eventName) || !angular.isFunction(handlerFn)) {
        return angular.noop;
      }
      if (!subscribersByEvent[eventName]) {
        subscribersByEvent[eventName] = [];
      }
      subscribersByEvent[eventName].push(handlerFn);

      var disposed = false;
      return function unsubscribeSignalRListener() {
        if (disposed) {
          return;
        }
        disposed = true;
        var list = subscribersByEvent[eventName];
        if (!list) {
          return;
        }
        var idx = list.indexOf(handlerFn);
        if (idx !== -1) {
          list.splice(idx, 1);
        }
      };
    };

    function connectHubWithToken(signalRGlobal, tokenForConnection) {
      var reconnectDelays = APP_CONFIG.signalrReconnectScheduleMs || [0, 2000, 5000, 10000, 30000];
      var silentLogger = (signalRGlobal.LogLevel && signalRGlobal.LogLevel.None !== undefined)
        ? signalRGlobal.LogLevel.None
        : { log: angular.noop };
      var hubConn = new signalRGlobal.HubConnectionBuilder()
        .withUrl(APP_CONFIG.hubUrl, {
          accessTokenFactory: function () {
            return TokenStorageService.getAccessToken() || tokenForConnection || '';
          }
        })
        .withAutomaticReconnect(reconnectDelays)
        .configureLogging(silentLogger)
        .build();

      connection = hubConn;
      activeTokenSnapshot = tokenForConnection;

      for (var i = 0; i < CANONICAL_HUB_EVENTS.length; i++) {
        (function registerEvent(evtName) {
          hubConn.on(evtName, function (payload) {
            if (connection === hubConn) {
              dispatchIncomingHubEvent(evtName, payload);
            }
          });
        })(CANONICAL_HUB_EVENTS[i]);
      }

      if (angular.isFunction(hubConn.onreconnecting)) {
        hubConn.onreconnecting(function () {
          if (connection === hubConn && desiredMode === 'started') {
            setConnectionStatus('Reconnecting');
          }
        });
      }

      if (angular.isFunction(hubConn.onreconnected)) {
        hubConn.onreconnected(function () {
          if (connection === hubConn && desiredMode === 'started') {
            setConnectionStatus('Connected');
            var missedSinceUtc = lastSeenUtc;
            var reconnectedAtUtc = new Date().toISOString();
            lastSeenUtc = reconnectedAtUtc;
            $rootScope.$applyAsync(function () {
              $rootScope.$broadcast('signalr:reconnected', {
                reconnectedAtUtc: reconnectedAtUtc,
                sinceUtc: missedSinceUtc
              });
            });
          }
        });
      }

      if (angular.isFunction(hubConn.onclose)) {
        hubConn.onclose(function () {
          if (connection === hubConn && desiredMode === 'started') {
            setConnectionStatus('Offline');
          }
        });
      }

      setConnectionStatus('Connecting');
      return wrapNativePromise(hubConn.start())
        .then(function () {
          if (desiredMode !== 'started' || connection !== hubConn) {
            if (connection === hubConn) {
              connection = null;
              activeTokenSnapshot = null;
            }
            return wrapNativePromise(hubConn.stop())
              .catch(angular.noop)
              .then(function () {
                return false;
              });
          }
          setConnectionStatus('Connected');
          return true;
        })
        .catch(function () {
          if (connection === hubConn) {
            connection = null;
            activeTokenSnapshot = null;
            if (desiredMode === 'started' && TokenStorageService.getAccessToken() === tokenForConnection) {
              setConnectionStatus('Offline');
            }
          }
          return false;
        });
    }

    self.start = function () {
      var currentToken = TokenStorageService.getAccessToken();
      if (!currentToken) {
        desiredMode = 'stopped';
        setConnectionStatus('Disconnected');
        return $q.resolve(false);
      }

      var signalRGlobal = window.signalR;
      if (!signalRGlobal || !signalRGlobal.HubConnectionBuilder) {
        desiredMode = 'stopped';
        setConnectionStatus('Unavailable');
        return $q.resolve(false);
      }

      desiredMode = 'started';

      if (
        connection &&
        activeTokenSnapshot === currentToken &&
        (connectionStatus === 'Connected' || connectionStatus === 'Connecting' || connectionStatus === 'Reconnecting')
      ) {
        return inFlightStartPromise || $q.resolve(true);
      }

      if (!connection || activeTokenSnapshot !== currentToken || connectionStatus === 'Offline' || connectionStatus === 'Disconnected') {
        setConnectionStatus('Connecting');
      }

      var startStep = lifecycleChain.then(function () {
        if (desiredMode !== 'started') {
          return false;
        }

        var latestToken = TokenStorageService.getAccessToken();
        if (!latestToken) {
          setConnectionStatus('Disconnected');
          return false;
        }

        var latestSignalR = window.signalR;
        if (!latestSignalR || !latestSignalR.HubConnectionBuilder) {
          setConnectionStatus('Unavailable');
          return false;
        }

        if (
          connection &&
          activeTokenSnapshot === latestToken &&
          (connectionStatus === 'Connected' || connectionStatus === 'Reconnecting')
        ) {
          return true;
        }

        var stopPreviousPromise = $q.resolve();
        if (connection) {
          var previousConn = connection;
          connection = null;
          activeTokenSnapshot = null;
          setConnectionStatus('Connecting');
          stopPreviousPromise = wrapNativePromise(previousConn.stop()).catch(angular.noop);
        }

        return stopPreviousPromise.then(function () {
          if (desiredMode !== 'started') {
            return false;
          }
          var postStopToken = TokenStorageService.getAccessToken();
          if (!postStopToken) {
            setConnectionStatus('Disconnected');
            return false;
          }
          if (
            connection &&
            activeTokenSnapshot === postStopToken &&
            (connectionStatus === 'Connected' || connectionStatus === 'Reconnecting')
          ) {
            return true;
          }
          return connectHubWithToken(latestSignalR, postStopToken);
        });
      });

      inFlightStartPromise = startStep;
      lifecycleChain = startStep.then(angular.noop, angular.noop);
      return startStep.finally(function () {
        if (inFlightStartPromise === startStep) {
          inFlightStartPromise = null;
        }
      });
    };

    self.stop = function () {
      desiredMode = 'stopped';
      inFlightStartPromise = null;
      setConnectionStatus('Disconnected');

      var stopStep = lifecycleChain
        .then(function () {
          if (desiredMode !== 'stopped') {
            return;
          }
          if (!connection) {
            activeTokenSnapshot = null;
            setConnectionStatus('Disconnected');
            return;
          }
          var activeConn = connection;
          connection = null;
          activeTokenSnapshot = null;
          setConnectionStatus('Disconnected');
          return wrapNativePromise(activeConn.stop()).catch(angular.noop);
        })
        .finally(function () {
          if (desiredMode === 'stopped' && !connection) {
            setConnectionStatus('Disconnected');
          }
        });

      lifecycleChain = stopStep.then(angular.noop, angular.noop);
      return stopStep;
    };

    self.simulateEvent = function (eventName, payload) {
      dispatchIncomingHubEvent(eventName, payload);
    };
  }
})();
