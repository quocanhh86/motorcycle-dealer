(function () {
  'use strict';

  var REFRESH_STORAGE_KEY = 'mdlmp.refresh_token';
  var SESSION_STORAGE_KEY = 'mdlmp.auth.session';

  angular
    .module('app.core')
    .service('TokenStorageService', TokenStorageService)
    .service('AuthService', AuthService);

  TokenStorageService.$inject = ['$window'];
  function TokenStorageService($window) {
    var inMemoryAccessToken = null;
    var inMemoryRefreshToken = null;
    var inMemoryExpiresAtMs = 0;
    var inMemoryExpiresAtUtc = null;

    function resolvePrimaryStorage() {
      if ($window && $window.sessionStorage) {
        return $window.sessionStorage;
      }
      if ($window && $window.localStorage) {
        return $window.localStorage;
      }
      return null;
    }

    function safeGetItem(store, key) {
      if (!store) {
        return null;
      }
      try {
        return store.getItem(key);
      } catch (e) {
        return null;
      }
    }

    function safeSetItem(store, key, value) {
      if (!store) {
        return;
      }
      try {
        store.setItem(key, value);
      } catch (e) {
        // Ignore storage quota errors in restricted browsing contexts.
      }
    }

    function safeRemoveItem(store, key) {
      if (!store) {
        return;
      }
      try {
        store.removeItem(key);
      } catch (e) {
        // Ignore storage removal errors.
      }
    }

    function resolveExpiryWindow(expiresInOrUtc) {
      if (angular.isString(expiresInOrUtc) && expiresInOrUtc.trim()) {
        var parsedMs = Date.parse(expiresInOrUtc);
        if (!isNaN(parsedMs) && parsedMs > 0) {
          return {
            expiresAtMs: parsedMs,
            expiresAtUtc: new Date(parsedMs).toISOString()
          };
        }
      }
      var ttlSeconds = Math.max(60, Number(expiresInOrUtc) || 900);
      var computedMs = Date.now() + (ttlSeconds * 1000);
      return {
        expiresAtMs: computedMs,
        expiresAtUtc: new Date(computedMs).toISOString()
      };
    }

    this.getAccessToken = function () {
      return inMemoryAccessToken;
    };

    this.getExpiresAtUtc = function () {
      return inMemoryExpiresAtUtc;
    };

    this.hasValidAccessToken = function (skewSeconds) {
      if (!inMemoryAccessToken) {
        return false;
      }
      var bufferMs = (Number(skewSeconds) || 30) * 1000;
      return inMemoryExpiresAtMs - Date.now() > bufferMs;
    };

    this.setTokens = function (accessToken, expiresInOrUtc, refreshToken) {
      inMemoryAccessToken = accessToken || null;
      if (accessToken) {
        var exp = resolveExpiryWindow(expiresInOrUtc);
        inMemoryExpiresAtMs = exp.expiresAtMs;
        inMemoryExpiresAtUtc = exp.expiresAtUtc;
      } else {
        inMemoryExpiresAtMs = 0;
        inMemoryExpiresAtUtc = null;
      }

      if (refreshToken !== undefined) {
        inMemoryRefreshToken = refreshToken || null;
      }

      var store = resolvePrimaryStorage();
      if (inMemoryRefreshToken) {
        safeSetItem(store, REFRESH_STORAGE_KEY, inMemoryRefreshToken);
      } else if (refreshToken === null) {
        safeRemoveItem(store, REFRESH_STORAGE_KEY);
      }
    };

    this.saveSession = function (sessionRecord) {
      var store = resolvePrimaryStorage();
      if (!sessionRecord || !sessionRecord.accessToken) {
        safeRemoveItem(store, SESSION_STORAGE_KEY);
        return;
      }
      safeSetItem(store, SESSION_STORAGE_KEY, JSON.stringify({
        accessToken: sessionRecord.accessToken,
        refreshToken: sessionRecord.refreshToken || inMemoryRefreshToken || null,
        expiresAtUtc: sessionRecord.expiresAtUtc || inMemoryExpiresAtUtc,
        user: sessionRecord.user || null
      }));
      if (sessionRecord.refreshToken) {
        safeSetItem(store, REFRESH_STORAGE_KEY, sessionRecord.refreshToken);
      }
    };

    this.loadSession = function () {
      var primary = resolvePrimaryStorage();
      var raw = safeGetItem(primary, SESSION_STORAGE_KEY);
      if (!raw && $window && $window.localStorage && primary !== $window.localStorage) {
        raw = safeGetItem($window.localStorage, SESSION_STORAGE_KEY);
      }
      if (!raw) {
        return null;
      }
      try {
        var parsed = JSON.parse(raw);
        if (!parsed || typeof parsed !== 'object') {
          return null;
        }
        return parsed;
      } catch (e) {
        return null;
      }
    };

    this.getRefreshToken = function () {
      if (inMemoryRefreshToken) {
        return inMemoryRefreshToken;
      }
      var primary = resolvePrimaryStorage();
      var stored = safeGetItem(primary, REFRESH_STORAGE_KEY);
      if (!stored && $window && $window.localStorage && primary !== $window.localStorage) {
        stored = safeGetItem($window.localStorage, REFRESH_STORAGE_KEY);
      }
      return stored || null;
    };

    this.clear = function () {
      inMemoryAccessToken = null;
      inMemoryRefreshToken = null;
      inMemoryExpiresAtMs = 0;
      inMemoryExpiresAtUtc = null;

      if ($window && $window.sessionStorage) {
        safeRemoveItem($window.sessionStorage, REFRESH_STORAGE_KEY);
        safeRemoveItem($window.sessionStorage, SESSION_STORAGE_KEY);
      }
      if ($window && $window.localStorage) {
        safeRemoveItem($window.localStorage, REFRESH_STORAGE_KEY);
        safeRemoveItem($window.localStorage, SESSION_STORAGE_KEY);
      }
    };
  }

  var AUTH_I18N_VI = {
    auth: {
      sessionExpiredTitle: 'Phiên đăng nhập đã hết hạn',
      noRefreshTokenDetail: 'Không tìm thấy mã làm mới phiên. Vui lòng đăng nhập lại.'
    }
  };

  var AUTH_I18N_EN = {
    auth: {
      sessionExpiredTitle: 'Session Expired',
      noRefreshTokenDetail: 'No refresh token available.'
    }
  };

  AuthService.$inject = [
    '$q',
    '$injector',
    '$rootScope',
    'APP_CONFIG',
    'ROLE_HOME',
    'ROLE_PERMISSION_PRESETS',
    'TokenStorageService',
    'I18nService'
  ];
  function AuthService(
    $q,
    $injector,
    $rootScope,
    APP_CONFIG,
    ROLE_HOME,
    ROLE_PERMISSION_PRESETS,
    TokenStorageService,
    I18nService
  ) {
    var self = this;
    var _currentUser = null;
    var permissionLookup = Object.create(null);
    var sharedRefreshPromise = null;
    var _currentUserFlight = null;
    var _currentUserFlightToken = null;
    var _sessionEpoch = 0;

    function resolveI18nService() {
      if (I18nService && angular.isFunction(I18nService.t)) {
        return I18nService;
      }
      if ($injector && angular.isFunction($injector.has) && $injector.has('I18nService')) {
        return $injector.get('I18nService');
      }
      return null;
    }

    function translateAuth(key, fallback) {
      var i18nSvc = resolveI18nService();
      if (i18nSvc) {
        if (angular.isFunction(i18nSvc.extend)) {
          i18nSvc.extend(AUTH_I18N_VI, AUTH_I18N_EN);
        }
        var translated = i18nSvc.t(key);
        if (translated && translated !== key) {
          return translated;
        }
      }
      return fallback;
    }

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(AUTH_I18N_VI, AUTH_I18N_EN);
    }

    function resetCurrentUserFlight() {
      _currentUserFlight = null;
      _currentUserFlightToken = null;
      _sessionEpoch++;
    }

    function decodeJwtClaimsSafe(jwtToken) {
      if (!angular.isString(jwtToken)) {
        return {};
      }
      var segments = jwtToken.split('.');
      if (segments.length !== 3 || typeof window.atob !== 'function') {
        return {};
      }
      try {
        var b64 = segments[1].replace(/-/g, '+').replace(/_/g, '/');
        while (b64.length % 4 !== 0) {
          b64 += '=';
        }
        var binary = window.atob(b64);
        var utf8 = decodeURIComponent(
          binary.split('').map(function (ch) {
            return '%' + ('00' + ch.charCodeAt(0).toString(16)).slice(-2);
          }).join('')
        );
        return JSON.parse(utf8) || {};
      } catch (e) {
        try {
          return JSON.parse(window.atob(segments[1].replace(/-/g, '+').replace(/_/g, '/'))) || {};
        } catch (innerErr) {
          return {};
        }
      }
    }

    function unwrapEnvelope(rawInput) {
      var payload = rawInput && rawInput.status !== undefined && rawInput.data !== undefined
        ? rawInput.data
        : rawInput;
      if (
        payload &&
        angular.isObject(payload) &&
        !angular.isArray(payload) &&
        payload.data !== undefined &&
        angular.isObject(payload.data)
      ) {
        return payload.data;
      }
      return payload || {};
    }

    function coercePermissionsArray(candidatePerms, fallbackRole, fallbackExistingPerms) {
      if (angular.isArray(candidatePerms)) {
        return candidatePerms.slice();
      }
      if (angular.isString(candidatePerms) && candidatePerms.trim()) {
        return [candidatePerms.trim()];
      }
      if (angular.isArray(fallbackExistingPerms) && fallbackExistingPerms.length > 0) {
        return fallbackExistingPerms.slice();
      }
      var preset = fallbackRole && ROLE_PERMISSION_PRESETS[fallbackRole];
      return preset ? preset.slice() : [];
    }

    function normalizeUser(rawUserInput, fallbackUser, claims, accessToken, envelopeBody) {
      var rawUser = (rawUserInput && angular.isObject(rawUserInput)) ? rawUserInput : {};
      var existing = fallbackUser !== undefined ? fallbackUser : _currentUser;
      var safeClaims = claims || {};
      var body = envelopeBody || rawUser;

      var role = rawUser.role ||
        rawUser.Role ||
        (angular.isArray(rawUser.roles) && rawUser.roles[0]) ||
        safeClaims.role ||
        safeClaims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        (existing && existing.role) ||
        null;

      var rawDealerId = rawUser.dealerId !== undefined
        ? rawUser.dealerId
        : (rawUser.DealerId !== undefined
          ? rawUser.DealerId
          : (safeClaims.dealer_id || safeClaims.dealerId || (existing && existing.dealerId) || null));
      var dealerId = rawDealerId ? String(rawDealerId) : null;

      var dealerCode = rawUser.dealerCode ||
        rawUser.DealerCode ||
        safeClaims.dealer_code ||
        safeClaims.dealerCode ||
        (existing && existing.dealerCode) ||
        (role === 'Admin' && !dealerId ? 'ALL-DEALERS' : null);

      var dealerName = rawUser.dealerName ||
        rawUser.DealerName ||
        (existing && existing.dealerName) ||
        (role === 'Admin' && !dealerId ? 'Nationwide Network HQ' : null);

      var rawPerms = rawUser.permissions ||
        rawUser.Permissions ||
        body.permissions ||
        body.Permissions ||
        safeClaims.permissions ||
        safeClaims.permission;
      var permissions = coercePermissionsArray(rawPerms, role, existing && existing.permissions);

      var isCustomerRole = role === 'Customer';

      var resolvedAvail = rawUser.availablePoints !== undefined
        ? rawUser.availablePoints
        : (rawUser.AvailablePoints !== undefined
          ? rawUser.AvailablePoints
          : (existing ? existing.availablePoints : undefined));
      var availablePts = isCustomerRole
        ? (angular.isNumber(resolvedAvail) ? resolvedAvail : 0)
        : (angular.isNumber(resolvedAvail) ? resolvedAvail : null);

      var resolvedLifetime = rawUser.lifetimeQualifyingPoints !== undefined
        ? rawUser.lifetimeQualifyingPoints
        : (rawUser.LifetimeQualifyingPoints !== undefined
          ? rawUser.LifetimeQualifyingPoints
          : (existing ? existing.lifetimeQualifyingPoints : undefined));
      var lifetimePts = isCustomerRole
        ? (angular.isNumber(resolvedLifetime) ? resolvedLifetime : 0)
        : (angular.isNumber(resolvedLifetime) ? resolvedLifetime : null);

      var rawMult = rawUser.tierMultiplier !== undefined && rawUser.tierMultiplier !== null
        ? rawUser.tierMultiplier
        : (rawUser.TierMultiplier !== undefined && rawUser.TierMultiplier !== null
          ? rawUser.TierMultiplier
          : (rawUser.pointsMultiplier !== undefined && rawUser.pointsMultiplier !== null
            ? rawUser.pointsMultiplier
            : (rawUser.PointsMultiplier !== undefined && rawUser.PointsMultiplier !== null
              ? rawUser.PointsMultiplier
              : (existing
                ? (existing.tierMultiplier !== undefined ? existing.tierMultiplier : existing.pointsMultiplier)
                : undefined))));
      var numericMult = Number(rawMult);
      var resolvedMultiplier = isCustomerRole
        ? (numericMult > 0 ? numericMult : 1.0)
        : (rawMult !== null && rawMult !== undefined && !isNaN(numericMult) && numericMult > 0 ? numericMult : null);

      return {
        userId: rawUser.userId || rawUser.UserId || rawUser.id || rawUser.Id || safeClaims.sub || (existing && existing.userId) || null,
        email: rawUser.email || rawUser.Email || safeClaims.email || (existing && existing.email) || '',
        fullName: rawUser.fullName || rawUser.FullName || safeClaims.name || (existing && existing.fullName) || '',
        phoneNumber: rawUser.phoneNumber || rawUser.PhoneNumber || (existing && existing.phoneNumber) || null,
        role: role,
        dealerId: dealerId,
        dealerCode: dealerCode,
        dealerName: dealerName,
        staffEmployeeCode: rawUser.staffEmployeeCode || rawUser.StaffEmployeeCode || (existing && existing.staffEmployeeCode) || null,
        tierCode: rawUser.tierCode || rawUser.TierCode || rawUser.loyaltyTier || (existing && existing.tierCode) || (isCustomerRole ? 'Bronze' : null),
        tierMultiplier: resolvedMultiplier,
        pointsMultiplier: resolvedMultiplier,
        availablePoints: availablePts,
        lifetimeQualifyingPoints: lifetimePts,
        permissions: permissions
      };
    }

    function normalizeAuthPayload(rawInput, fallbackUser) {
      var body = unwrapEnvelope(rawInput);
      var existing = fallbackUser || _currentUser || null;

      var accessToken = body.accessToken || body.AccessToken || TokenStorageService.getAccessToken() || null;
      var refreshToken = body.refreshToken !== undefined
        ? body.refreshToken
        : (body.RefreshToken !== undefined ? body.RefreshToken : TokenStorageService.getRefreshToken());
      var claims = decodeJwtClaimsSafe(accessToken);

      var expiresAtUtc = body.expiresAtUtc || body.ExpiresAtUtc || body.accessTokenExpiresAtUtc || null;
      var expiresInSeconds = Number(body.expiresInSeconds || body.ExpiresInSeconds || body.expiresIn || 0);
      if (!expiresAtUtc && claims.exp) {
        expiresAtUtc = new Date(Number(claims.exp) * 1000).toISOString();
      }

      var rawUser = (body.user && angular.isObject(body.user))
        ? body.user
        : ((body.User && angular.isObject(body.User)) ? body.User : body);

      var userProfile = normalizeUser(rawUser, existing, claims, accessToken, body);

      return {
        accessToken: accessToken,
        refreshToken: refreshToken || null,
        expiresAtUtc: expiresAtUtc,
        expiresInSeconds: expiresInSeconds > 0 ? expiresInSeconds : 900,
        user: userProfile
      };
    }

    function rebuildPermissionLookup(permissions) {
      permissionLookup = Object.create(null);
      if (angular.isArray(permissions)) {
        for (var i = 0; i < permissions.length; i++) {
          permissionLookup[permissions[i]] = true;
        }
      }
    }

    function applyAuthenticatedSession(rawPayload, fallbackUser) {
      var normalized = normalizeAuthPayload(rawPayload, fallbackUser);
      var profile = normalized.user;

      _currentUser = Object.freeze({
        userId: profile.userId,
        email: profile.email,
        fullName: profile.fullName,
        phoneNumber: profile.phoneNumber,
        role: profile.role,
        dealerId: profile.dealerId,
        dealerCode: profile.dealerCode,
        dealerName: profile.dealerName,
        staffEmployeeCode: profile.staffEmployeeCode,
        tierCode: profile.tierCode,
        tierMultiplier: profile.tierMultiplier,
        pointsMultiplier: profile.pointsMultiplier,
        availablePoints: profile.availablePoints,
        lifetimeQualifyingPoints: profile.lifetimeQualifyingPoints,
        permissions: Object.freeze(profile.permissions.slice())
      });

      rebuildPermissionLookup(profile.permissions);

      TokenStorageService.setTokens(
        normalized.accessToken,
        normalized.expiresAtUtc || normalized.expiresInSeconds,
        normalized.refreshToken
      );

      TokenStorageService.saveSession({
        accessToken: normalized.accessToken,
        refreshToken: normalized.refreshToken,
        expiresAtUtc: TokenStorageService.getExpiresAtUtc(),
        user: profile
      });

      $rootScope.$broadcast('auth:changed', _currentUser);
      return _currentUser;
    }

    function isLiveCustomerSession() {
      return Boolean(
        _currentUser &&
        _currentUser.role === 'Customer' &&
        TokenStorageService.getAccessToken()
      );
    }

    function patchLiveCustomerSession(patchFields) {
      if (!isLiveCustomerSession()) {
        return null;
      }
      var mergedUser = angular.extend({}, _currentUser, patchFields);
      return applyAuthenticatedSession({
        accessToken: TokenStorageService.getAccessToken(),
        refreshToken: TokenStorageService.getRefreshToken(),
        expiresAtUtc: TokenStorageService.getExpiresAtUtc(),
        user: mergedUser
      }, _currentUser);
    }

    function startRealtimeIfLive() {
      if (
        TokenStorageService.getAccessToken() &&
        $injector.has('SignalRClientService')
      ) {
        $injector.get('SignalRClientService').start();
      }
    }

    if ($rootScope && angular.isFunction($rootScope.$on)) {
      $rootScope.$on('signalr:LoyaltyPointsUpdated', function (_evt, payload) {
        if (!isLiveCustomerSession()) {
          return;
        }
        var p = payload || {};
        var nextAvailable = p.availablePointsAfter !== undefined
          ? Number(p.availablePointsAfter)
          : (p.AvailablePointsAfter !== undefined
            ? Number(p.AvailablePointsAfter)
            : (p.availablePoints !== undefined
              ? Number(p.availablePoints)
              : (p.newAvailablePoints !== undefined
                ? Number(p.newAvailablePoints)
                : (p.pointsDelta !== undefined && angular.isNumber(_currentUser.availablePoints)
                  ? Math.max(0, _currentUser.availablePoints + Number(p.pointsDelta))
                  : _currentUser.availablePoints))));

        var nextLifetime = p.lifetimeQualifyingPointsAfter !== undefined
          ? Number(p.lifetimeQualifyingPointsAfter)
          : (p.LifetimeQualifyingPointsAfter !== undefined
            ? Number(p.LifetimeQualifyingPointsAfter)
            : (p.lifetimeQualifyingPoints !== undefined
              ? Number(p.lifetimeQualifyingPoints)
              : (p.newLifetimeQualifyingPoints !== undefined
                ? Number(p.newLifetimeQualifyingPoints)
                : _currentUser.lifetimeQualifyingPoints)));

        patchLiveCustomerSession({
          availablePoints: !isNaN(nextAvailable) ? nextAvailable : _currentUser.availablePoints,
          lifetimeQualifyingPoints: !isNaN(nextLifetime) ? nextLifetime : _currentUser.lifetimeQualifyingPoints
        });

        self.fetchCurrentUser().catch(angular.noop);
      });

      $rootScope.$on('signalr:TierUpgraded', function (_evt, payload) {
        if (!isLiveCustomerSession()) {
          return;
        }
        var p = payload || {};
        var nextTierCode = p.newTierCode || p.NewTierCode || p.tierCode || p.TierCode || _currentUser.tierCode;
        var rawNextMult = p.newPointsMultiplier !== undefined
          ? p.newPointsMultiplier
          : (p.NewPointsMultiplier !== undefined
            ? p.NewPointsMultiplier
            : (p.newMultiplier !== undefined
              ? p.newMultiplier
              : (p.pointsMultiplier !== undefined
                ? p.pointsMultiplier
                : (p.tierMultiplier !== undefined ? p.tierMultiplier : undefined))));
        var parsedMult = Number(rawNextMult);
        var nextMult = (!isNaN(parsedMult) && parsedMult > 0) ? parsedMult : _currentUser.tierMultiplier;

        var patch = {
          tierCode: nextTierCode,
          tierMultiplier: nextMult,
          pointsMultiplier: nextMult
        };
        var rawTierLifetime = p.lifetimeQualifyingPoints !== undefined
          ? p.lifetimeQualifyingPoints
          : p.LifetimeQualifyingPoints;
        if (rawTierLifetime !== undefined && !isNaN(Number(rawTierLifetime))) {
          patch.lifetimeQualifyingPoints = Number(rawTierLifetime);
        }

        patchLiveCustomerSession(patch);
        self.fetchCurrentUser().catch(angular.noop);
      });
    }

    // Hydrate persisted tab session on service construction so full-page navigation preserves JWT state
    (function hydrateStoredSessionOnBoot() {
      var persisted = TokenStorageService.loadSession();
      if (!persisted || !persisted.user) {
        return;
      }
      applyAuthenticatedSession(persisted, persisted.user);
      if (TokenStorageService.hasValidAccessToken(10) && angular.isFunction($rootScope.$evalAsync)) {
        $rootScope.$evalAsync(function () {
          startRealtimeIfLive();
        });
      }
    })();

    self.isPasswordComplex = function (password) {
      if (!angular.isString(password) || password.length < 8 || password.length > 128 || /\s/.test(password)) {
        return false;
      }
      return (
        /[A-Z]/.test(password) &&
        /[a-z]/.test(password) &&
        /[0-9]/.test(password) &&
        /[^A-Za-z0-9\s]/.test(password)
      );
    };

    self.normalizeUser = function (rawUserInput, fallbackUser) {
      return normalizeUser(
        rawUserInput,
        fallbackUser !== undefined ? fallbackUser : _currentUser,
        null,
        TokenStorageService.getAccessToken(),
        rawUserInput
      );
    };

    self.normalizeAuthResponse = function (rawInput) {
      return normalizeAuthPayload(rawInput, _currentUser);
    };

    self.setSession = function (sessionPayload) {
      if (!sessionPayload) {
        self.clearSessionAndRealtime();
        return null;
      }
      resetCurrentUserFlight();
      return applyAuthenticatedSession(sessionPayload, null);
    };

    self.isAuthenticated = function () {
      return Boolean(_currentUser && TokenStorageService.hasValidAccessToken(5));
    };

    self.getCurrentUser = function () {
      return _currentUser;
    };

    self.getRole = function () {
      return _currentUser ? _currentUser.role : null;
    };

    self.getDealerId = function () {
      return _currentUser ? _currentUser.dealerId : null;
    };

    self.getPermissions = function () {
      return _currentUser && _currentUser.permissions ? _currentUser.permissions.slice() : [];
    };

    self.getDefaultHomeState = function () {
      var role = self.getRole();
      return (role && ROLE_HOME[role]) || 'app.public.showroom';
    };

    var INTERNAL_PATH_STATE_MAP = Object.freeze({
      '/showroom': 'app.public.showroom',
      '/customer/dashboard': 'app.customer.dashboard',
      '/customer/garage': 'app.customer.garage',
      '/customer/bookings/new': 'app.customer.bookingNew',
      '/customer/booking/new': 'app.customer.bookingNew',
      '/customer/book-service': 'app.customer.bookService',
      '/customer/bookings': 'app.customer.bookings',
      '/customer/invoices': 'app.customer.bookings',
      '/customer/loyalty': 'app.customer.loyalty',
      '/customer/orders': 'app.customer.loyalty',
      '/customer/test-drives': 'app.customer.testDrives',
      '/staff/workbench': 'app.staff.workbench',
      '/staff/invoice-builder': 'app.staff.invoiceBuilder',
      '/staff/invoicing': 'app.staff.invoicing',
      '/staff/sales-orders/new': 'app.staff.salesDesk',
      '/staff/sales-orders': 'app.staff.salesDesk',
      '/staff/inventory': 'app.staff.salesDesk',
      '/staff/sales-new': 'app.staff.salesNew',
      '/staff/time-slots': 'app.staff.timeSlots',
      '/staff/slots': 'app.staff.slots',
      '/staff/customers': 'app.staff.customers',
      '/staff/test-drives': 'app.staff.testDrives',
      '/admin/analytics': 'app.admin.analytics',
      '/admin/dealers': 'app.admin.dealers',
      '/admin/staff': 'app.admin.staff',
      '/admin/models': 'app.admin.models',
      '/admin/loyalty/tiers': 'app.admin.loyaltyTiers',
      '/admin/loyalty-tiers': 'app.admin.loyaltyTiers',
      '/admin/loyalty/adjustments': 'app.admin.loyaltyTiers',
      '/admin/loyalty/vouchers': 'app.admin.voucherCatalog',
      '/admin/voucher-catalog': 'app.admin.voucherCatalog',
      '/admin/spare-parts': 'app.admin.spareParts'
    });

    function safeDecodeParamComponent(str) {
      try {
        return decodeURIComponent(String(str || '').replace(/\+/g, ' '));
      } catch (e) {
        return String(str || '');
      }
    }

    function parseQueryParamsSafe(queryStr) {
      var params = {};
      if (!angular.isString(queryStr) || !queryStr) {
        return params;
      }
      var pairs = queryStr.split('&');
      for (var i = 0; i < pairs.length; i++) {
        var pair = pairs[i];
        if (!pair) {
          continue;
        }
        var eqIdx = pair.indexOf('=');
        var rawKey = eqIdx === -1 ? pair : pair.slice(0, eqIdx);
        var rawVal = eqIdx === -1 ? '' : pair.slice(eqIdx + 1);
        var key = safeDecodeParamComponent(rawKey).trim();
        if (!key || key === '__proto__' || key === 'constructor' || key === 'prototype') {
          continue;
        }
        params[key] = safeDecodeParamComponent(rawVal);
      }
      return params;
    }

    function matchParameterizedInternalPath(pathname) {
      var invoiceMatch = /^\/staff\/bookings\/([^/?#]+)\/invoice$/i.exec(pathname);
      if (invoiceMatch) {
        return {
          state: 'app.staff.invoiceBuilder',
          params: { bookingId: safeDecodeParamComponent(invoiceMatch[1]) }
        };
      }
      var invoicingAliasMatch = /^\/staff\/invoicing\/([^/?#]+)$/i.exec(pathname);
      if (invoicingAliasMatch) {
        return {
          state: 'app.staff.invoicing',
          params: { bookingId: safeDecodeParamComponent(invoicingAliasMatch[1]) }
        };
      }
      return null;
    }

    function isAuthorizedNavigableState(candidateStateName, $state) {
      if (!angular.isString(candidateStateName)) {
        return false;
      }
      var trimmed = candidateStateName.trim();
      if (!trimmed || !/^app\.[A-Za-z0-9_.]+$/.test(trimmed)) {
        return false;
      }
      if (
        trimmed === 'app.public.login' ||
        trimmed === 'app.public.register' ||
        trimmed === 'app.public.forbidden'
      ) {
        return false;
      }
      if ($state && angular.isFunction($state.get)) {
        var resolvedDef = $state.get(trimmed);
        if (!resolvedDef || resolvedDef.abstract) {
          return false;
        }
        var stData = resolvedDef.data || {};
        if (angular.isArray(stData.roles) && stData.roles.length > 0) {
          var activeRole = self.getRole();
          if (activeRole && stData.roles.indexOf(activeRole) === -1) {
            return false;
          }
        }
        if (angular.isArray(stData.permissions) && stData.permissions.length > 0) {
          if (!self.hasPermissions(stData.permissions, stData.permissionMode || 'ALL')) {
            return false;
          }
        }
      }
      return true;
    }

    self.resolvePostLoginState = function (candidateStateName, $state) {
      var fallback = self.getDefaultHomeState();
      if (!angular.isString(candidateStateName)) {
        return fallback;
      }
      var trimmed = candidateStateName.trim();
      return isAuthorizedNavigableState(trimmed, $state) ? trimmed : fallback;
    };

    self.resolvePostLoginTarget = function (returnUrl, returnTo, $state) {
      var fallback = self.getDefaultHomeState();
      var trimmedUrl = angular.isString(returnUrl) ? returnUrl.trim() : '';
      var trimmedReturnTo = angular.isString(returnTo) ? returnTo.trim() : '';

      // 1. Direct state name passed in returnUrl (e.g. 'app.staff.workbench')
      if (trimmedUrl && /^app\.[A-Za-z0-9_.]+$/.test(trimmedUrl)) {
        if (isAuthorizedNavigableState(trimmedUrl, $state)) {
          return { state: trimmedUrl, params: {} };
        }
        return { state: fallback, params: {} };
      }

      // 2. Internal single-slash SPA path (strictly blocks //evil.com, https://, javascript:, backslashes)
      if (
        trimmedUrl &&
        trimmedUrl.charAt(0) === '/' &&
        trimmedUrl.charAt(1) !== '/' &&
        !/[\r\n\\]/.test(trimmedUrl)
      ) {
        var withoutHash = trimmedUrl.split('#')[0];
        var qIdx = withoutHash.indexOf('?');
        var rawPathname = qIdx === -1 ? withoutHash : withoutHash.slice(0, qIdx);
        var rawQuery = qIdx === -1 ? '' : withoutHash.slice(qIdx + 1);
        var normalizedPath = rawPathname.length > 1
          ? rawPathname.replace(/\/+$/, '').toLowerCase()
          : rawPathname;

        // Explicitly reject auth-loop / forbidden paths even if returnTo is also present
        if (
          normalizedPath === '/login' ||
          normalizedPath === '/register' ||
          normalizedPath === '/403'
        ) {
          return { state: fallback, params: {} };
        }

        var queryParams = parseQueryParamsSafe(rawQuery);
        var mappedState = INTERNAL_PATH_STATE_MAP[normalizedPath] || null;
        if (!mappedState) {
          var paramMatch = matchParameterizedInternalPath(rawPathname.replace(/\/+$/, ''));
          if (paramMatch) {
            mappedState = paramMatch.state;
            queryParams = angular.extend({}, paramMatch.params, queryParams);
          }
        }

        if (mappedState) {
          if (isAuthorizedNavigableState(mappedState, $state)) {
            return { state: mappedState, params: queryParams };
          }
          return { state: fallback, params: {} };
        }

        if (trimmedReturnTo && isAuthorizedNavigableState(trimmedReturnTo, $state)) {
          return { state: trimmedReturnTo, params: queryParams };
        }

        return { state: fallback, params: {} };
      }

      // 3. No valid internal returnUrl; fall back to returnTo if no malicious external returnUrl was supplied
      if (!trimmedUrl && trimmedReturnTo && isAuthorizedNavigableState(trimmedReturnTo, $state)) {
        return { state: trimmedReturnTo, params: {} };
      }

      return { state: fallback, params: {} };
    };

    self.getAccessToken = function () {
      return TokenStorageService.getAccessToken();
    };

    self.hasPermission = function (permissionCode) {
      if (!permissionCode) {
        return true;
      }
      return Boolean(permissionLookup[permissionCode]);
    };

    self.hasPermissions = function (requiredPermissions, mode) {
      if (!requiredPermissions) {
        return true;
      }
      var list = angular.isArray(requiredPermissions) ? requiredPermissions : [requiredPermissions];
      if (list.length === 0) {
        return true;
      }
      var evaluationMode = String(mode || 'ALL').toUpperCase();
      if (evaluationMode === 'ANY') {
        for (var i = 0; i < list.length; i++) {
          if (permissionLookup[list[i]]) {
            return true;
          }
        }
        return false;
      }
      for (var j = 0; j < list.length; j++) {
        if (!permissionLookup[list[j]]) {
          return false;
        }
      }
      return true;
    };

    self.ensureValidSession = function () {
      if (_currentUser && TokenStorageService.hasValidAccessToken(30)) {
        return $q.resolve(true);
      }
      var storedRefresh = TokenStorageService.getRefreshToken();
      if (!storedRefresh) {
        return $q.resolve(false);
      }
      return self.refreshTokens()
        .then(function () {
          startRealtimeIfLive();
          return true;
        })
        .catch(function () {
          self.clearSessionAndRealtime();
          return false;
        });
    };

    function enrichDealerFromLiveCatalog(sessionUser, requestEpoch, requestToken) {
      if (
        !sessionUser ||
        !sessionUser.dealerId ||
        (sessionUser.dealerCode && sessionUser.dealerName) ||
        !$injector.has('$http')
      ) {
        return $q.resolve(sessionUser);
      }
      var $http = $injector.get('$http');
      return $http.get(APP_CONFIG.apiBaseUrl + '/dealers')
        .then(function (dealerRes) {
          if (_sessionEpoch !== requestEpoch || TokenStorageService.getAccessToken() !== requestToken) {
            return _currentUser;
          }
          var body = unwrapEnvelope(dealerRes);
          var list = angular.isArray(body) ? body : (body && angular.isArray(body.items) ? body.items : []);
          var targetId = String(sessionUser.dealerId).toLowerCase();
          for (var i = 0; i < list.length; i++) {
            var d = list[i];
            var did = d && (d.id || d.dealerId || d.Id);
            if (did && String(did).toLowerCase() === targetId) {
              var merged = angular.extend({}, sessionUser, {
                dealerCode: sessionUser.dealerCode || d.code || d.dealerCode || d.Code || null,
                dealerName: sessionUser.dealerName || d.name || d.dealerName || d.Name || null
              });
              return applyAuthenticatedSession({
                accessToken: TokenStorageService.getAccessToken(),
                refreshToken: TokenStorageService.getRefreshToken(),
                expiresAtUtc: TokenStorageService.getExpiresAtUtc(),
                user: merged
              }, _currentUser);
            }
          }
          return sessionUser;
        })
        .catch(function () {
          return sessionUser;
        });
    }

    self.fetchCurrentUser = function () {
      var requestToken = TokenStorageService.getAccessToken();
      if (!requestToken || !$injector.has('$http')) {
        return $q.resolve(_currentUser);
      }
      if (_currentUserFlight && _currentUserFlightToken === requestToken) {
        return _currentUserFlight;
      }
      var requestEpoch = _sessionEpoch;
      var $http = $injector.get('$http');
      var flightPromise = null;
      flightPromise = $http.get(APP_CONFIG.apiBaseUrl + '/auth/me')
        .then(function (response) {
          if (
            _sessionEpoch !== requestEpoch ||
            TokenStorageService.getAccessToken() !== requestToken
          ) {
            return _currentUser;
          }
          var meDto = unwrapEnvelope(response);
          var meUserObj = (meDto && meDto.user && angular.isObject(meDto.user)) ? meDto.user : meDto;
          var meUserId = meUserObj && (meUserObj.userId || meUserObj.UserId || meUserObj.id || meUserObj.Id);
          if (
            _currentUser &&
            _currentUser.userId &&
            meUserId &&
            String(meUserId).toLowerCase() !== String(_currentUser.userId).toLowerCase()
          ) {
            return _currentUser;
          }
          var updatedUser = applyAuthenticatedSession({
            accessToken: TokenStorageService.getAccessToken(),
            refreshToken: TokenStorageService.getRefreshToken(),
            expiresAtUtc: TokenStorageService.getExpiresAtUtc(),
            user: meDto
          }, _currentUser);
          return enrichDealerFromLiveCatalog(updatedUser, requestEpoch, requestToken);
        })
        .finally(function () {
          if (_currentUserFlight === flightPromise) {
            _currentUserFlight = null;
            _currentUserFlightToken = null;
          }
        });
      _currentUserFlight = flightPromise;
      _currentUserFlightToken = requestToken;
      return flightPromise;
    };

    self.getMe = self.fetchCurrentUser;

    self.login = function (credentials) {
      var $http = $injector.get('$http');
      var payload = {
        email: String((credentials && credentials.email) || '').trim(),
        password: String((credentials && credentials.password) || '')
      };

      return $http.post(APP_CONFIG.apiBaseUrl + '/auth/login', payload)
        .then(function (response) {
          resetCurrentUserFlight();
          var sessionUser = applyAuthenticatedSession(response, null);
          return self.fetchCurrentUser()
            .catch(function () {
              return sessionUser;
            })
            .then(function (enrichedUser) {
              startRealtimeIfLive();
              return enrichedUser;
            });
        });
    };

    self.registerCustomer = function (registrationDto) {
      var $http = $injector.get('$http');
      var payload = {
        email: String((registrationDto && registrationDto.email) || '').trim(),
        password: String((registrationDto && registrationDto.password) || ''),
        fullName: String((registrationDto && registrationDto.fullName) || '').trim(),
        phoneNumber: String((registrationDto && registrationDto.phoneNumber) || '').trim()
      };

      return $http.post(APP_CONFIG.apiBaseUrl + '/auth/register', payload)
        .then(function (response) {
          var normalized = normalizeAuthPayload(response, null);
          if (normalized && normalized.accessToken) {
            resetCurrentUserFlight();
            var sessionUser = applyAuthenticatedSession(response, null);
            return self.fetchCurrentUser()
              .catch(function () {
                return sessionUser;
              })
              .then(function (enrichedUser) {
                startRealtimeIfLive();
                return enrichedUser;
              });
          }
          return unwrapEnvelope(response);
        });
    };

    self.register = self.registerCustomer;

    self.refreshTokens = function () {
      if (sharedRefreshPromise) {
        return sharedRefreshPromise;
      }
      var refreshToken = TokenStorageService.getRefreshToken();
      if (!refreshToken) {
        return $q.reject({
          status: 401,
          data: {
            title: translateAuth('auth.sessionExpiredTitle', 'Session Expired'),
            detail: translateAuth('auth.noRefreshTokenDetail', 'No refresh token available.'),
            errorCode: 'MISSING_REFRESH_TOKEN'
          }
        });
      }

      var $http = $injector.get('$http');
      sharedRefreshPromise = $http.post(APP_CONFIG.apiBaseUrl + '/auth/refresh', {
        refreshToken: refreshToken
      }).then(function (response) {
        applyAuthenticatedSession(response, _currentUser);
        if ($injector.has('SignalRClientService')) {
          var signalrSvc = $injector.get('SignalRClientService');
          var hubStatus = signalrSvc && angular.isFunction(signalrSvc.getConnectionStatus)
            ? signalrSvc.getConnectionStatus()
            : null;
          if (hubStatus === 'Disconnected' || hubStatus === 'Unavailable' || hubStatus === 'Offline') {
            startRealtimeIfLive();
          }
        }
        return TokenStorageService.getAccessToken();
      }).finally(function () {
        sharedRefreshPromise = null;
      });

      return sharedRefreshPromise;
    };

    self.clearSessionAndRealtime = function () {
      _currentUser = null;
      resetCurrentUserFlight();
      permissionLookup = Object.create(null);
      TokenStorageService.clear();
      var stopPromise = null;
      if ($injector.has('SignalRClientService')) {
        var signalrSvc = $injector.get('SignalRClientService');
        if (signalrSvc && angular.isFunction(signalrSvc.stop)) {
          stopPromise = signalrSvc.stop();
        }
      }
      if ($injector.has('ToastService')) {
        var toastSvc = $injector.get('ToastService');
        if (toastSvc && angular.isFunction(toastSvc.clearAll)) {
          toastSvc.clearAll();
        }
      }
      $rootScope.$broadcast('auth:changed', null);
      return stopPromise || $q.resolve();
    };

    self.logout = function () {
      var hasToken = Boolean(TokenStorageService.getAccessToken());
      var refreshToken = TokenStorageService.getRefreshToken();

      var finalizeLogout = function () {
        return self.clearSessionAndRealtime().then(function () {
          return true;
        });
      };

      if (!hasToken || !$injector.has('$http')) {
        return finalizeLogout();
      }

      var $http = $injector.get('$http');
      return $http.post(APP_CONFIG.apiBaseUrl + '/auth/logout', {
        refreshToken: refreshToken || null
      })
        .catch(angular.noop)
        .then(finalizeLogout);
    };
  }
})();
