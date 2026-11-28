(function () {
  'use strict';

  var NOTIFICATIONS_I18N_VI = {
    notifications: {
      triggerLabel: 'Thông báo',
      unreadBadgeCount: '{{count}} chưa đọc',
      subtitle: 'SignalR /hubs/notifications + Hộp thư đồng bộ',
      syncing: 'Đang đồng bộ...',
      syncCta: 'Đồng bộ',
      markReadCta: 'Đã đọc',
      emptyTitle: 'Không có thông báo nào',
      emptyDesc: 'Các cập nhật lịch bảo dưỡng, lái thử, bảo hành và điểm thưởng trực tiếp sẽ xuất hiện tại đây.'
    }
  };

  var NOTIFICATIONS_I18N_EN = {
    notifications: {
      triggerLabel: 'Alerts',
      unreadBadgeCount: '{{count}} Unread',
      subtitle: 'SignalR /hubs/notifications + Persistent Inbox',
      syncing: 'Syncing...',
      syncCta: 'Sync',
      markReadCta: 'Mark Read',
      emptyTitle: 'No notifications yet',
      emptyDesc: 'Real-time booking, test-drive, warranty, and loyalty updates will appear here automatically.'
    }
  };

  angular
    .module('app.shared')
    .run(registerNotificationI18n)
    .component('notificationBell', {
      controllerAs: '$ctrl',
      controller: NotificationBellController,
      template:
        '<div style="display:inline-flex;align-items:center;">' +
          '<button type="button" class="bell-trigger-btn" ' +
                  'ng-class="{\'signalr-live-pulse is-signalr-pulse\': $ctrl.isPulsing}" ' +
                  'ng-attr-title="{{ \'a11y.openNotifications\' | t }}" ' +
                  'ng-attr-aria-expanded="{{ $ctrl.isOpen ? \'true\' : \'false\' }}" ' +
                  'ng-click="$ctrl.toggleDrawer()">' +
            '<app-icon name="{{ $ctrl.isPulsing || $ctrl.unreadCount > 0 ? \'bell-ringing\' : \'bell\' }}" weight="duotone" size="16"></app-icon>' +
            '<span>{{ \'notifications.triggerLabel\' | t }}</span>' +
            '<span class="bell-unread-count mono-num" ng-if="$ctrl.unreadCount > 0">{{ $ctrl.unreadCount }}</span>' +
          '</button>' +
          '<div class="drawer-backdrop" ng-if="$ctrl.isOpen" ng-click="$ctrl.closeDrawer()">' +
            '<section class="notification-drawer drawer-slide-in motion-drawer" role="dialog" aria-modal="true" ' +
                     'ng-attr-aria-label="{{ \'nav.notifications\' | t }}" ' +
                     'ng-click="$event.stopPropagation()">' +
              '<header class="notification-drawer-header">' +
                '<div>' +
                  '<div style="display:flex;align-items:center;gap:8px;">' +
                    '<app-icon name="bell-ringing" weight="duotone" size="17"></app-icon>' +
                    '<strong style="font-size:14px;">{{ \'nav.notifications\' | t }} ({{ \'notifications.unreadBadgeCount\' | t:{ count: $ctrl.unreadCount } }})</strong>' +
                  '</div>' +
                  '<div style="font-size:11px;opacity:0.75;margin-top:2px;">{{ \'notifications.subtitle\' | t }}</div>' +
                '</div>' +
                '<div style="display:flex;gap:8px;align-items:center;">' +
                  '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                          'ng-disabled="$ctrl.isSyncing || $ctrl.unreadCount === 0" ' +
                          'ng-click="$ctrl.markAllRead()">' +
                    '<app-icon name="checks" weight="regular" size="14"></app-icon>' +
                    '<span>{{ \'nav.markAllRead\' | t }}</span>' +
                  '</button>' +
                  '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                          'ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}" ' +
                          'ng-click="$ctrl.closeDrawer()">' +
                    '<app-icon name="x" weight="regular" size="16"></app-icon>' +
                  '</button>' +
                '</div>' +
              '</header>' +
              '<div style="padding:10px 16px;border-bottom:1px solid var(--clr-border-subtle);display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                '<div style="display:flex;gap:8px;">' +
                  '<button type="button" class="btn btn-sm" ' +
                          'ng-class="$ctrl.unreadOnly ? \'btn-secondary\' : \'btn-primary\'" ' +
                          'ng-click="$ctrl.setFilter(false)">{{ \'nav.allNotifications\' | t }}</button>' +
                  '<button type="button" class="btn btn-sm" ' +
                          'ng-class="$ctrl.unreadOnly ? \'btn-primary\' : \'btn-secondary\'" ' +
                          'ng-click="$ctrl.setFilter(true)">{{ \'nav.unreadOnly\' | t }}</button>' +
                '</div>' +
                '<button type="button" class="btn btn-secondary btn-sm" ' +
                        'ng-disabled="$ctrl.isLoading" ' +
                        'ng-click="$ctrl.refreshNotifications()">' +
                  '<app-icon name="arrows-clockwise" weight="regular" size="14" extra-class="{{ $ctrl.isLoading ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                  '<span>{{ $ctrl.isLoading ? (\'notifications.syncing\' | t) : (\'notifications.syncCta\' | t) }}</span>' +
                '</button>' +
              '</div>' +
              '<div class="notification-drawer-list">' +
                '<error-retry-banner ng-if="$ctrl.feedProblem" ' +
                                     'message="$ctrl.feedProblem.detail" ' +
                                     'trace-id="$ctrl.feedProblem.traceId" ' +
                                     'retrying="$ctrl.isLoading" ' +
                                     'on-retry="$ctrl.refreshNotifications()"></error-retry-banner>' +
                '<skeleton-loader ng-if="$ctrl.isLoading && $ctrl.notifications.length === 0" variant="drawer" count="4"></skeleton-loader>' +
                '<empty-state ng-if="!$ctrl.isLoading && $ctrl.notifications.length === 0" ' +
                             'icon="bell-slash" ' +
                             'title-key="notifications.emptyTitle" ' +
                             'desc-key="notifications.emptyDesc"></empty-state>' +
                '<article class="notification-item fade-slide-up" ' +
                         'role="button" tabindex="0" ' +
                         'ng-repeat="item in $ctrl.notifications track by item.notificationId" ' +
                         'ng-class="{\'is-unread\': !item.isRead}" ' +
                         'ng-style="{\'cursor\': ($ctrl.resolveNotificationTargetState(item) || !item.isRead) ? \'pointer\' : \'default\'}" ' +
                         'ng-click="$ctrl.openNotification(item)" ' +
                         'ng-keydown="$ctrl.onNotificationKeydown($event, item)">' +
                  '<div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">' +
                    '<span class="badge" ng-class="$ctrl.eventBadgeClass(item.eventType)">' +
                      '{{ item.eventType | enumT:\'NotificationEventType\' }}' +
                    '</span>' +
                    '<span class="text-muted mono-num" style="font-size:11px;" ng-attr-title="{{ item.occurredAtUtc | utcToLocal }}">' +
                      '{{ item.occurredAtUtc | utcToLocal:\'relative\' }}' +
                    '</span>' +
                  '</div>' +
                  '<div class="text-primary" style="font-weight:600;font-size:13px;margin-top:6px;">{{ item.title }}</div>' +
                  '<div class="text-secondary" style="font-size:12px;margin-top:3px;">{{ item.message }}</div>' +
                  '<div style="margin-top:8px;display:flex;justify-content:space-between;align-items:center;" ng-if="!item.isRead">' +
                    '<span style="display:inline-flex;align-items:center;gap:5px;font-size:11px;color:var(--clr-brand-on-soft);font-weight:600;">' +
                      '<app-icon name="circle" weight="fill" size="8"></app-icon>' +
                      '<span>{{ \'nav.unreadOnly\' | t }}</span>' +
                    '</span>' +
                    '<button type="button" class="btn btn-secondary btn-sm" ' +
                            'ng-keydown="$event.stopPropagation()" ' +
                            'ng-click="$ctrl.markRead(item.notificationId); $event.stopPropagation()">' +
                      '<app-icon name="check" weight="regular" size="12"></app-icon>' +
                      '<span>{{ \'notifications.markReadCta\' | t }}</span>' +
                    '</button>' +
                  '</div>' +
                '</article>' +
              '</div>' +
            '</section>' +
          '</div>' +
        '</div>'
    });

  registerNotificationI18n.$inject = ['I18nService'];
  function registerNotificationI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(NOTIFICATIONS_I18N_VI, NOTIFICATIONS_I18N_EN);
    }
  }

  var NOTIFICATION_ROLE_TARGETS = Object.freeze({
    Customer: Object.freeze({
      BookingCreated: 'app.customer.bookings',
      BookingStatusChanged: 'app.customer.bookings',
      MaintenanceReminderTriggered: 'app.customer.garage',
      LoyaltyPointsUpdated: 'app.customer.loyalty',
      TierUpgraded: 'app.customer.loyalty',
      SalesOrderCompleted: 'app.customer.loyalty',
      VoucherRedeemed: 'app.customer.loyalty',
      TestDriveCreated: 'app.customer.testDrives',
      TestDriveStatusChanged: 'app.customer.testDrives',
      ServiceReviewSubmitted: 'app.customer.bookings',
      WarrantyClaimCreated: 'app.customer.warranty',
      WarrantyClaimStatusChanged: 'app.customer.warranty'
    }),
    DealerStaff: Object.freeze({
      BookingCreated: 'app.staff.workbench',
      BookingStatusChanged: 'app.staff.workbench',
      MaintenanceReminderTriggered: 'app.staff.customers',
      LoyaltyPointsUpdated: 'app.staff.customers',
      TierUpgraded: 'app.staff.customers',
      SalesOrderCompleted: 'app.staff.salesDesk',
      VoucherRedeemed: 'app.staff.customers',
      TestDriveCreated: 'app.staff.testDrives',
      TestDriveStatusChanged: 'app.staff.testDrives',
      ServiceReviewSubmitted: 'app.staff.warrantyReviews',
      WarrantyClaimCreated: 'app.staff.warrantyReviews',
      WarrantyClaimStatusChanged: 'app.staff.warrantyReviews'
    }),
    Admin: Object.freeze({
      BookingCreated: 'app.admin.analytics',
      BookingStatusChanged: 'app.admin.analytics',
      MaintenanceReminderTriggered: 'app.admin.analytics',
      LoyaltyPointsUpdated: 'app.admin.loyaltyTiers',
      TierUpgraded: 'app.admin.loyaltyTiers',
      SalesOrderCompleted: 'app.admin.analytics',
      VoucherRedeemed: 'app.admin.voucherCatalog',
      TestDriveCreated: 'app.admin.analytics',
      TestDriveStatusChanged: 'app.admin.analytics',
      ServiceReviewSubmitted: 'app.admin.qualityWarranty',
      WarrantyClaimCreated: 'app.admin.qualityWarranty',
      WarrantyClaimStatusChanged: 'app.admin.qualityWarranty'
    })
  });

  function resolveNotificationTargetState(item, role) {
    if (!item || !item.eventType || !role) {
      return null;
    }
    var roleTargets = NOTIFICATION_ROLE_TARGETS[role];
    if (!roleTargets) {
      return null;
    }
    return roleTargets[item.eventType] || null;
  }

  NotificationBellController.$inject = [
    '$q',
    '$rootScope',
    'AuthService',
    'DealerApiService',
    'NotificationStore',
    'ProblemDetailsMapper',
    'TokenStorageService',
    '$state',
    '$timeout',
    'I18nService'
  ];
  function NotificationBellController(
    $q,
    $rootScope,
    AuthService,
    DealerApiService,
    NotificationStore,
    ProblemDetailsMapper,
    TokenStorageService,
    $state,
    $timeout,
    I18nService
  ) {
    var $ctrl = this;
    var deregisterFns = [];
    var isDestroyed = false;
    var activeUserKey = null;
    var fetchEpoch = 0;
    var pulseTimer = null;

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(NOTIFICATIONS_I18N_VI, NOTIFICATIONS_I18N_EN);
    }

    function clearPulseTimer() {
      if (pulseTimer && $timeout && angular.isFunction($timeout.cancel)) {
        $timeout.cancel(pulseTimer);
      }
      pulseTimer = null;
    }

    function triggerLivePulse() {
      if (isDestroyed) {
        return;
      }
      clearPulseTimer();
      $ctrl.isPulsing = true;
      if ($timeout && angular.isFunction($timeout)) {
        pulseTimer = $timeout(function () {
          if (!isDestroyed) {
            $ctrl.isPulsing = false;
          }
          pulseTimer = null;
        }, 1300);
      }
    }

    function syncFeed() {
      $ctrl.unreadCount = NotificationStore.getUnreadCount();
      $ctrl.notifications = NotificationStore.getAll($ctrl.unreadOnly);
    }

    function resolvePrincipalKey() {
      if (!AuthService.isAuthenticated()) {
        return null;
      }
      var user = AuthService.getCurrentUser() || {};
      return String(user.userId || user.email || 'live') + ':' + String(user.role || '');
    }

    function handleSessionSync(forceReload) {
      if (isDestroyed) {
        return $q.resolve($ctrl.notifications);
      }
      if (!AuthService.isAuthenticated()) {
        activeUserKey = null;
        fetchEpoch++;
        NotificationStore.clear();
        $ctrl.isOpen = false;
        $ctrl.drawerOpen = false;
        $ctrl.feedProblem = null;
        syncFeed();
        return $q.resolve([]);
      }

      var nextKey = resolvePrincipalKey();
      var principalSwitched = activeUserKey !== nextKey;
      if (principalSwitched) {
        activeUserKey = nextKey;
        NotificationStore.clear();
        syncFeed();
      }
      if (principalSwitched || forceReload) {
        return $ctrl.refreshNotifications();
      }
      syncFeed();
      return $q.resolve($ctrl.notifications);
    }

    $ctrl.$onInit = function () {
      $ctrl.isOpen = false;
      $ctrl.drawerOpen = false;
      $ctrl.unreadOnly = false;
      $ctrl.isLoading = false;
      $ctrl.isSyncing = false;
      $ctrl.isPulsing = false;
      $ctrl.feedProblem = null;
      syncFeed();

      var liveHubEvents = [
        'signalr:BookingCreated',
        'signalr:BookingStatusChanged',
        'signalr:MaintenanceReminderTriggered',
        'signalr:LoyaltyPointsUpdated',
        'signalr:TierUpgraded',
        'signalr:TestDriveCreated',
        'signalr:TestDriveStatusChanged',
        'signalr:ServiceReviewSubmitted',
        'signalr:WarrantyClaimCreated',
        'signalr:WarrantyClaimStatusChanged'
      ];
      for (var i = 0; i < liveHubEvents.length; i++) {
        deregisterFns.push($rootScope.$on(liveHubEvents[i], function () {
          if (!isDestroyed) {
            syncFeed();
            triggerLivePulse();
          }
        }));
      }

      deregisterFns.push($rootScope.$on('signalr:live-pulse', function () {
        if (!isDestroyed) {
          syncFeed();
          triggerLivePulse();
        }
      }));

      deregisterFns.push($rootScope.$on('auth:changed', function () {
        if (!isDestroyed) {
          handleSessionSync(false);
        }
      }));

      deregisterFns.push($rootScope.$on('signalr:reconnected', function () {
        if (!isDestroyed) {
          $ctrl.refreshNotifications();
        }
      }));

      handleSessionSync(true);
    };

    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      fetchEpoch++;
      clearPulseTimer();
      for (var i = 0; i < deregisterFns.length; i++) {
        if (angular.isFunction(deregisterFns[i])) {
          deregisterFns[i]();
        }
      }
      deregisterFns.length = 0;
    };

    $ctrl.eventBadgeClass = function (eventType) {
      if (
        eventType === 'TierUpgraded' ||
        eventType === 'LoyaltyPointsUpdated' ||
        eventType === 'SalesOrderCompleted' ||
        eventType === 'TestDriveStatusChanged' ||
        eventType === 'ServiceReviewSubmitted' ||
        eventType === 'WarrantyClaimStatusChanged'
      ) {
        return 'badge-success';
      }
      if (eventType === 'MaintenanceReminderTriggered') {
        return 'badge-warning';
      }
      if (
        eventType === 'BookingCreated' ||
        eventType === 'VoucherRedeemed' ||
        eventType === 'TestDriveCreated' ||
        eventType === 'WarrantyClaimCreated'
      ) {
        return 'badge-info';
      }
      return 'badge-neutral';
    };

    function resolveEffectiveRole(explicitRole) {
      var effectiveRole = explicitRole || (AuthService && angular.isFunction(AuthService.getRole) ? AuthService.getRole() : null);
      if (!effectiveRole && AuthService && angular.isFunction(AuthService.getCurrentUser)) {
        var currentUser = AuthService.getCurrentUser();
        effectiveRole = currentUser && currentUser.role ? currentUser.role : null;
      }
      return effectiveRole;
    }

    function extractNotificationDealerFocus(item) {
      if (!item || !angular.isObject(item)) {
        return null;
      }
      var rawPayload = item.payloadJson !== undefined && item.payloadJson !== null
        ? item.payloadJson
        : (item.payload !== undefined ? item.payload : null);
      var parsed = null;
      if (angular.isString(rawPayload) && rawPayload.trim()) {
        try {
          var candidate = JSON.parse(rawPayload);
          if (candidate && angular.isObject(candidate) && !angular.isArray(candidate)) {
            parsed = candidate;
          }
        } catch (e) {
          parsed = null;
        }
      } else if (rawPayload && angular.isObject(rawPayload) && !angular.isArray(rawPayload)) {
        parsed = rawPayload;
      }

      var source = parsed || item;
      var rawDealerId = source.dealerId || source.DealerId || item.dealerId || item.DealerId || '';
      var rawDealerCode = source.dealerCode || source.DealerCode || item.dealerCode || item.DealerCode || '';
      var dealerId = String(rawDealerId || '').trim();
      var dealerCode = String(rawDealerCode || '').trim().toUpperCase();
      if (!dealerId && !dealerCode) {
        return null;
      }
      return {
        dealerId: dealerId || null,
        dealerCode: dealerCode || null
      };
    }

    $ctrl.resolveNotificationTargetState = function (item, explicitRole) {
      return resolveNotificationTargetState(item, resolveEffectiveRole(explicitRole));
    };

    $ctrl.openNotification = function (item) {
      if (!item) {
        return $q.resolve(null);
      }
      var markPromise = (!item.isRead && item.notificationId)
        ? $ctrl.markRead(item.notificationId)
        : $q.resolve(true);
      var effectiveRole = resolveEffectiveRole(null);
      var targetState = resolveNotificationTargetState(item, effectiveRole);
      if (
        effectiveRole === 'Admin' &&
        targetState === 'app.admin.qualityWarranty' &&
        (item.eventType === 'ServiceReviewSubmitted' ||
          item.eventType === 'WarrantyClaimCreated' ||
          item.eventType === 'WarrantyClaimStatusChanged') &&
        DealerApiService &&
        angular.isFunction(DealerApiService.setAdminFocusContext)
      ) {
        var focusCtx = extractNotificationDealerFocus(item);
        if (focusCtx) {
          DealerApiService.setAdminFocusContext(focusCtx);
        }
      }
      $ctrl.isOpen = false;
      $ctrl.drawerOpen = false;
      if (targetState && $state && angular.isFunction($state.go)) {
        $state.go(targetState);
      }
      return markPromise;
    };
    $ctrl.onNotificationClick = $ctrl.openNotification;

    $ctrl.onNotificationKeydown = function ($event, item) {
      if (!$event) {
        return null;
      }
      var key = $event.key || '';
      var keyCode = $event.keyCode || $event.which || 0;
      var isActivationKey =
        key === 'Enter' ||
        key === ' ' ||
        key === 'Spacebar' ||
        keyCode === 13 ||
        keyCode === 32;
      if (!isActivationKey) {
        return null;
      }
      if (angular.isFunction($event.preventDefault)) {
        $event.preventDefault();
      }
      return $ctrl.openNotification(item);
    };

    $ctrl.refreshNotifications = function () {
      if (!AuthService.isAuthenticated()) {
        syncFeed();
        return $q.resolve($ctrl.notifications);
      }

      var myEpoch = ++fetchEpoch;
      var requestedUnreadOnly = Boolean($ctrl.unreadOnly);
      $ctrl.isLoading = true;
      $ctrl.feedProblem = null;

      return DealerApiService.notifications.list({
        pageNumber: 1,
        pageSize: 30,
        unreadOnly: requestedUnreadOnly ? true : undefined
      }).then(function (result) {
        if (isDestroyed || myEpoch !== fetchEpoch) {
          return $ctrl.notifications;
        }
        if (requestedUnreadOnly) {
          NotificationStore.mergeUnreadFeed(result.items, result.unreadCount);
        } else {
          NotificationStore.setServerFeed(result.items, result.unreadCount);
        }
        syncFeed();
        return $ctrl.notifications;
      }).catch(function (rejection) {
        if (!isDestroyed && myEpoch === fetchEpoch) {
          var prob = ProblemDetailsMapper.extract(rejection);
          if (prob.status !== 404) {
            $ctrl.feedProblem = prob;
          }
          syncFeed();
        }
        return $ctrl.notifications;
      }).finally(function () {
        if (!isDestroyed && myEpoch === fetchEpoch) {
          $ctrl.isLoading = false;
        }
      });
    };

    $ctrl.toggleDrawer = function () {
      $ctrl.isOpen = !$ctrl.isOpen;
      $ctrl.drawerOpen = $ctrl.isOpen;
      if ($ctrl.isOpen) {
        syncFeed();
        if (AuthService.isAuthenticated()) {
          $ctrl.refreshNotifications();
        }
      }
    };

    $ctrl.closeDrawer = function () {
      $ctrl.isOpen = false;
      $ctrl.drawerOpen = false;
    };

    $ctrl.setFilter = function (unreadOnly) {
      $ctrl.unreadOnly = Boolean(unreadOnly);
      syncFeed();
      if (AuthService.isAuthenticated()) {
        $ctrl.refreshNotifications();
      }
    };

    $ctrl.markRead = function (notificationId) {
      if (!notificationId) {
        return $q.resolve(null);
      }
      NotificationStore.markAsRead(notificationId);
      syncFeed();

      if (!AuthService.isAuthenticated()) {
        return $q.resolve(true);
      }

      return DealerApiService.notifications.markRead(notificationId)
        .catch(function () {
          if (!isDestroyed) {
            $ctrl.refreshNotifications();
          }
          return null;
        });
    };

    $ctrl.markAllRead = function () {
      NotificationStore.markAllRead();
      syncFeed();

      if (!AuthService.isAuthenticated()) {
        return $q.resolve(true);
      }

      $ctrl.isSyncing = true;
      return DealerApiService.notifications.markAllRead()
        .catch(function () {
          if (!isDestroyed) {
            $ctrl.refreshNotifications();
          }
          return null;
        })
        .finally(function () {
          if (!isDestroyed) {
            $ctrl.isSyncing = false;
          }
        });
    };
  }
})();
