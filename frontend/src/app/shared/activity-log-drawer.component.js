(function () {
  'use strict';

  var ACTIVITY_LOG_I18N_VI = {
    activityLog: {
      triggerLabel: 'Nhật ký',
      drawerTitle: 'Nhật ký Thao tác Trực tiếp',
      sessionLabel: 'Phiên',
      tabClient: 'Trình duyệt ({{count}})',
      tabServerAdmin: 'Toàn hệ thống (Server/Web/Mobile)',
      tabServerMine: 'Nhật ký Server + Mobile của tôi',
      filterPlaceholder: 'Lọc theo thao tác, màn hình, đường dẫn hoặc correlationId...',
      flushNow: 'Đồng bộ lên Server',
      clearLocal: 'Xóa tạm trình duyệt',
      allSources: 'Tất cả nguồn',
      sourceServer: 'Máy chủ API (Server)',
      sourceWebClient: 'Trình duyệt Web (WebClient)',
      sourceMobileApp: 'Ứng dụng Di động (MobileApp)',
      screenLabel: 'Màn hình',
      targetLabel: 'Đích',
      totalBuffered: 'Tổng số bản ghi',
      anonymous: 'Khách vãng lai',
      emptyClientTitle: 'Chưa có thao tác cục bộ nào',
      emptyClientDesc: 'Không có thao tác trình duyệt nào khớp với bộ lọc hiện tại.',
      emptyServerTitle: 'Không có nhật ký kiểm toán nào',
      emptyServerDesc: 'Chưa có sự kiện server nào khớp với bộ lọc nguồn và từ khóa.',
      loadFailed: 'Không thể tải nhật ký thao tác từ máy chủ.'
    }
  };

  var ACTIVITY_LOG_I18N_EN = {
    activityLog: {
      triggerLabel: 'Logs',
      drawerTitle: 'Live User Action Logs',
      sessionLabel: 'Session',
      tabClient: 'Client Browser ({{count}})',
      tabServerAdmin: 'All Tiers (Server/Web/Mobile)',
      tabServerMine: 'My Server + Mobile Trail',
      filterPlaceholder: 'Filter action, screen, path, or correlationId...',
      flushNow: 'Flush to Server',
      clearLocal: 'Clear Local',
      allSources: 'All Sources',
      sourceServer: 'API Server',
      sourceWebClient: 'Web Browser',
      sourceMobileApp: 'Mobile App',
      screenLabel: 'Screen',
      targetLabel: 'Target',
      totalBuffered: 'Total buffered',
      anonymous: 'Anonymous',
      emptyClientTitle: 'No local actions found',
      emptyClientDesc: 'No local client actions match the current search filter.',
      emptyServerTitle: 'No server audit events found',
      emptyServerDesc: 'No correlated server audit events match your active filter.',
      loadFailed: 'Unable to load server activity logs.'
    }
  };

  angular
    .module('app.shared')
    .run(registerActivityLogI18n)
    .component('activityLogDrawer', {
      controllerAs: '$ctrl',
      controller: ActivityLogDrawerController,
      template:
        '<div class="activity-log-anchor">' +
          '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                  'ng-attr-title="{{ \'a11y.openActivityLog\' | t }}" ' +
                  'ng-attr-aria-expanded="{{ $ctrl.isOpen ? \'true\' : \'false\' }}" ' +
                  'data-action="Open Activity Log Drawer" ' +
                  'ng-click="$ctrl.toggleDrawer()">' +
            '<app-icon name="terminal-window" weight="duotone" size="15"></app-icon>' +
            '<span>{{ \'activityLog.triggerLabel\' | t }}</span>' +
            '<span class="badge badge-info mono-num" style="margin-left:2px;">{{ $ctrl.localCount }}</span>' +
          '</button>' +

          '<div class="activity-log-backdrop" ng-if="$ctrl.isOpen" ng-click="$ctrl.closeDrawer()"></div>' +
          '<section class="activity-log-panel drawer-slide-in motion-drawer" ng-if="$ctrl.isOpen" role="dialog" aria-modal="true" ' +
                   'ng-attr-aria-label="{{ \'activityLog.drawerTitle\' | t }}">' +
            '<header class="activity-log-header">' +
              '<div>' +
                '<div style="display:flex;align-items:center;gap:8px;font-weight:700;font-size:15px;">' +
                  '<app-icon name="terminal-window" weight="duotone" size="17"></app-icon>' +
                  '<span>{{ \'activityLog.drawerTitle\' | t }}</span>' +
                '</div>' +
                '<div class="mono-num" style="font-size:11px;opacity:0.75;margin-top:2px;">' +
                  '{{ \'activityLog.sessionLabel\' | t }}: {{ $ctrl.sessionId }}' +
                '</div>' +
              '</div>' +
              '<button type="button" class="btn btn-ghost-dark btn-sm" ' +
                      'ng-click="$ctrl.closeDrawer()" ' +
                      'ng-attr-aria-label="{{ \'a11y.closeModal\' | t }}">' +
                '<app-icon name="x" weight="regular" size="16"></app-icon>' +
              '</button>' +
            '</header>' +

            '<div class="activity-log-tabs">' +
              '<button type="button" class="btn btn-sm" ' +
                      'ng-class="$ctrl.activeTab === \'client\' ? \'btn-primary\' : \'btn-secondary\'" ' +
                      'ng-click="$ctrl.selectTab(\'client\')">' +
                '<app-icon name="browsers" weight="duotone" size="14"></app-icon>' +
                '<span>{{ \'activityLog.tabClient\' | t:{ count: $ctrl.localLogs.length } }}</span>' +
              '</button>' +
              '<button type="button" class="btn btn-sm" ng-if="$ctrl.isAuthenticated" ' +
                      'ng-class="$ctrl.activeTab === \'server\' ? \'btn-primary\' : \'btn-secondary\'" ' +
                      'ng-click="$ctrl.selectTab(\'server\')">' +
                '<app-icon name="hard-drives" weight="duotone" size="14"></app-icon>' +
                '<span>{{ $ctrl.isAdmin ? (\'activityLog.tabServerAdmin\' | t) : (\'activityLog.tabServerMine\' | t) }}</span>' +
              '</button>' +
            '</div>' +

            '<div class="activity-log-toolbar">' +
              '<input type="text" class="form-input" ' +
                     'ng-attr-placeholder="{{ \'activityLog.filterPlaceholder\' | t }}" ' +
                     'ng-model="$ctrl.searchTerm" ng-change="$ctrl.onFilterChanged()" />' +
              '<div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:8px;align-items:center;">' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-if="$ctrl.activeTab === \'client\'" ng-click="$ctrl.flushNow()">' +
                  '<app-icon name="cloud-arrow-up" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'activityLog.flushNow\' | t }}</span>' +
                '</button>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-if="$ctrl.activeTab === \'client\'" ng-click="$ctrl.clearLocal()">' +
                  '<app-icon name="trash" weight="regular" size="14"></app-icon>' +
                  '<span>{{ \'activityLog.clearLocal\' | t }}</span>' +
                '</button>' +
                '<select class="form-input" style="max-width:160px;min-height:34px;padding:4px 8px;font-size:12px;" ng-if="$ctrl.activeTab === \'server\'" ' +
                        'ng-model="$ctrl.sourceFilter" ng-change="$ctrl.loadServerLogs()">' +
                  '<option value="">{{ \'activityLog.allSources\' | t }}</option>' +
                  '<option value="Server">{{ \'activityLog.sourceServer\' | t }}</option>' +
                  '<option value="WebClient">{{ \'activityLog.sourceWebClient\' | t }}</option>' +
                  '<option value="MobileApp">{{ \'activityLog.sourceMobileApp\' | t }}</option>' +
                '</select>' +
                '<button type="button" class="btn btn-secondary btn-sm" ng-if="$ctrl.activeTab === \'server\'" ' +
                        'ng-disabled="$ctrl.loadingServer" ng-click="$ctrl.loadServerLogs()">' +
                  '<app-icon name="arrows-clockwise" weight="regular" size="14" extra-class="{{ $ctrl.loadingServer ? \'spin-smooth\' : \'\' }}"></app-icon>' +
                  '<span>{{ \'common.refresh\' | t }}</span>' +
                '</button>' +
              '</div>' +
            '</div>' +

            '<div class="activity-log-body" ng-if="$ctrl.activeTab === \'client\'">' +
              '<empty-state ng-if="$ctrl.localLogs.length === 0" ' +
                           'icon="clipboard-text" ' +
                           'title-key="activityLog.emptyClientTitle" ' +
                           'desc-key="activityLog.emptyClientDesc"></empty-state>' +
              '<article class="activity-log-row fade-slide-up" ng-repeat="entry in $ctrl.localLogs track by entry.eventId">' +
                '<div class="activity-log-row-top">' +
                  '<span class="badge badge-info">{{ entry.category }}</span>' +
                  '<span class="badge" ng-class="$ctrl.outcomeBadge(entry.outcome)">{{ entry.outcome }}</span>' +
                  '<span class="mono-num text-muted" style="font-size:11px;margin-left:auto;">{{ entry.timestampUtc | utcToLocal:\'datetime\' }}</span>' +
                '</div>' +
                '<div class="text-primary" style="font-weight:600;font-size:13px;margin-top:4px;">{{ entry.action }}</div>' +
                '<div class="text-secondary" style="font-size:12px;margin-top:2px;">' +
                  '<span>{{ \'activityLog.screenLabel\' | t }}: <strong>{{ entry.screen || \'—\' }}</strong></span>' +
                  '<span ng-if="entry.target"> • {{ \'activityLog.targetLabel\' | t }}: {{ entry.target }}</span>' +
                  '<span ng-if="entry.statusCode"> • HTTP {{ entry.statusCode }}</span>' +
                  '<span ng-if="entry.durationMs !== null && entry.durationMs !== undefined"> ({{ entry.durationMs }}ms)</span>' +
                '</div>' +
                '<div class="mono-num text-muted" style="font-size:10px;margin-top:3px;">CID: {{ entry.correlationId }}</div>' +
              '</article>' +
            '</div>' +

            '<div class="activity-log-body" ng-if="$ctrl.activeTab === \'server\'">' +
              '<error-retry-banner ng-if="$ctrl.serverProblem" ' +
                                   'message="$ctrl.serverProblem.detail" ' +
                                   'trace-id="$ctrl.serverProblem.traceId" ' +
                                   'retrying="$ctrl.loadingServer" ' +
                                   'on-retry="$ctrl.loadServerLogs()"></error-retry-banner>' +
              '<div class="text-secondary" style="font-size:12px;margin-bottom:4px;" ng-if="$ctrl.serverSummary">' +
                '<span>{{ \'activityLog.totalBuffered\' | t }}: <strong class="mono-num text-primary">{{ $ctrl.serverSummary.totalBufferedCount }}</strong></span>' +
                '<span ng-if="$ctrl.serverSummary.logFilePath" class="mono-num text-muted" style="display:block;font-size:10px;margin-top:2px;word-break:break-all;">NDJSON: {{ $ctrl.serverSummary.logFilePath }}</span>' +
              '</div>' +
              '<skeleton-loader ng-if="$ctrl.loadingServer" variant="drawer" count="4"></skeleton-loader>' +
              '<empty-state ng-if="!$ctrl.loadingServer && $ctrl.serverLogs.length === 0 && !$ctrl.serverProblem" ' +
                           'icon="hard-drives" ' +
                           'title-key="activityLog.emptyServerTitle" ' +
                           'desc-key="activityLog.emptyServerDesc"></empty-state>' +
              '<article class="activity-log-row fade-slide-up" ng-if="!$ctrl.loadingServer" ng-repeat="entry in $ctrl.serverLogs track by entry.eventId">' +
                '<div class="activity-log-row-top">' +
                  '<span class="badge badge-neutral">{{ entry.source }}</span>' +
                  '<span class="badge badge-info">{{ entry.clientPlatform }}</span>' +
                  '<span class="badge" ng-class="$ctrl.outcomeBadge(entry.outcome)">{{ entry.outcome }}</span>' +
                  '<span class="mono-num text-muted" style="font-size:11px;margin-left:auto;">{{ entry.timestampUtc | utcToLocal:\'datetime\' }}</span>' +
                '</div>' +
                '<div class="text-primary" style="font-weight:600;font-size:13px;margin-top:4px;">{{ entry.action }}</div>' +
                '<div class="text-secondary" style="font-size:12px;margin-top:2px;">' +
                  '<span>{{ entry.email || (\'activityLog.anonymous\' | t) }} ({{ entry.role ? (entry.role | enumT:\'UserRoleCode\') : (\'activityLog.anonymous\' | t) }})</span>' +
                  '<span ng-if="entry.screen"> • {{ entry.screen }}</span>' +
                  '<span ng-if="entry.httpMethod && entry.path"> • <span class="mono-num">{{ entry.httpMethod }} {{ entry.path }}</span></span>' +
                  '<span ng-if="entry.statusCode"> • HTTP {{ entry.statusCode }}</span>' +
                  '<span ng-if="entry.durationMs !== null && entry.durationMs !== undefined"> ({{ entry.durationMs }}ms)</span>' +
                '</div>' +
                '<div class="mono-num text-muted" style="font-size:10px;margin-top:3px;">CID: {{ entry.correlationId }}</div>' +
              '</article>' +
            '</div>' +
          '</section>' +
        '</div>'
    });

  registerActivityLogI18n.$inject = ['I18nService'];
  function registerActivityLogI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(ACTIVITY_LOG_I18N_VI, ACTIVITY_LOG_I18N_EN);
    }
  }

  ActivityLogDrawerController.$inject = [
    '$http',
    '$rootScope',
    'APP_CONFIG',
    'PERMISSIONS',
    'AuthService',
    'ProblemDetailsMapper',
    'UserActionLoggerService',
    'I18nService'
  ];
  function ActivityLogDrawerController(
    $http,
    $rootScope,
    APP_CONFIG,
    PERMISSIONS,
    AuthService,
    ProblemDetailsMapper,
    UserActionLoggerService,
    I18nService
  ) {
    var $ctrl = this;
    var disposers = [];

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(ACTIVITY_LOG_I18N_VI, ACTIVITY_LOG_I18N_EN);
    }

    function t(key, params, fallback) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t(key, params);
        if (resolved && resolved !== key) {
          return resolved;
        }
      }
      return fallback || key;
    }

    function syncRoleFlags() {
      $ctrl.isAuthenticated = AuthService.isAuthenticated();
      $ctrl.isAdmin = $ctrl.isAuthenticated &&
        AuthService.hasPermissions([PERMISSIONS.Analytics.ViewAllDealers], 'ALL');
      if (!$ctrl.isAuthenticated && $ctrl.activeTab === 'server') {
        $ctrl.activeTab = 'client';
      }
    }

    function refreshLocalLogs() {
      $ctrl.localLogs = UserActionLoggerService.getLocalLogs($ctrl.searchTerm, '');
      $ctrl.localCount = UserActionLoggerService.getLocalLogs('', '').length;
    }

    $ctrl.$onInit = function () {
      $ctrl.isOpen = false;
      $ctrl.activeTab = 'client';
      $ctrl.searchTerm = '';
      $ctrl.sourceFilter = '';
      $ctrl.sessionId = UserActionLoggerService.getSessionId();
      $ctrl.localLogs = [];
      $ctrl.localCount = 0;
      $ctrl.serverLogs = [];
      $ctrl.serverSummary = null;
      $ctrl.loadingServer = false;
      $ctrl.serverError = '';
      $ctrl.serverProblem = null;

      syncRoleFlags();
      refreshLocalLogs();

      disposers.push($rootScope.$on('auth:changed', function () {
        syncRoleFlags();
      }));
      disposers.push($rootScope.$on('activityLog:updated', function () {
        refreshLocalLogs();
      }));
    };

    $ctrl.$onDestroy = function () {
      for (var i = 0; i < disposers.length; i++) {
        if (angular.isFunction(disposers[i])) {
          disposers[i]();
        }
      }
      disposers.length = 0;
    };

    $ctrl.toggleDrawer = function () {
      $ctrl.isOpen = !$ctrl.isOpen;
      if ($ctrl.isOpen) {
        refreshLocalLogs();
        if ($ctrl.activeTab === 'server' && $ctrl.isAuthenticated) {
          $ctrl.loadServerLogs();
        }
      }
    };

    $ctrl.closeDrawer = function () {
      $ctrl.isOpen = false;
    };

    $ctrl.selectTab = function (tab) {
      $ctrl.activeTab = tab;
      if (tab === 'client') {
        refreshLocalLogs();
      } else if ($ctrl.isAuthenticated) {
        $ctrl.loadServerLogs();
      }
    };

    $ctrl.onFilterChanged = function () {
      if ($ctrl.activeTab === 'client') {
        refreshLocalLogs();
      } else if ($ctrl.isAuthenticated) {
        $ctrl.loadServerLogs();
      }
    };

    $ctrl.flushNow = function () {
      UserActionLoggerService.flush(false);
    };

    $ctrl.clearLocal = function () {
      UserActionLoggerService.clearLocalLogs();
      refreshLocalLogs();
    };

    $ctrl.loadServerLogs = function () {
      if (!$ctrl.isAuthenticated) {
        return;
      }
      $ctrl.loadingServer = true;
      $ctrl.serverError = '';
      $ctrl.serverProblem = null;
      var base = String((APP_CONFIG && APP_CONFIG.apiBaseUrl) || 'http://localhost:5080/api/v1').replace(/\/+$/, '');
      var url = $ctrl.isAdmin ? (base + '/activity-logs') : (base + '/activity-logs/my');
      var params = { take: 100 };
      if ($ctrl.sourceFilter) {
        params.source = $ctrl.sourceFilter;
      }
      if ($ctrl.searchTerm && $ctrl.searchTerm.trim()) {
        params.search = $ctrl.searchTerm.trim();
      }

      $http.get(url, { params: params, _skipActivityLog: true })
        .then(function (resp) {
          var payload = (resp && resp.data && resp.data.data) || (resp && resp.data) || {};
          $ctrl.serverLogs = angular.isArray(payload.items) ? payload.items : [];
          $ctrl.serverSummary = {
            totalBufferedCount: payload.totalBufferedCount || $ctrl.serverLogs.length,
            logFilePath: payload.logFilePath || ''
          };
        })
        .catch(function (err) {
          var extracted = ProblemDetailsMapper && angular.isFunction(ProblemDetailsMapper.extract)
            ? ProblemDetailsMapper.extract(err)
            : { detail: t('activityLog.loadFailed', null, 'Unable to load server activity logs.'), traceId: null };
          $ctrl.serverProblem = extracted;
          $ctrl.serverError = extracted.detail || t('activityLog.loadFailed', null, 'Unable to load server activity logs.');
        })
        .finally(function () {
          $ctrl.loadingServer = false;
        });
    };

    $ctrl.outcomeBadge = function (outcome) {
      var norm = String(outcome || '').toLowerCase();
      if (norm === 'success' || norm === 'executed') {
        return 'badge-success';
      }
      if (norm === 'denied' || norm === 'clienterror' || norm === 'failed') {
        return 'badge-warning';
      }
      if (norm === 'servererror' || norm === 'error') {
        return 'badge-danger';
      }
      return 'badge-neutral';
    };
  }
})();
