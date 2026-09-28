(function () {
  'use strict';

  var STORAGE_KEY = 'apex_locale';
  var DEFAULT_LOCALE = 'vi-VN';
  var DEFAULT_TIMEZONE = 'Asia/Ho_Chi_Minh';

  angular
    .module('app.core')
    .service('I18nService', I18nService);

  I18nService.$inject = ['$rootScope', '$window', 'I18N_VI', 'I18N_EN'];
  function I18nService($rootScope, $window, I18N_VI, I18N_EN) {
    var self = this;
    var dictionaries = {
      'vi-VN': I18N_VI || {},
      'en-US': I18N_EN || {}
    };
    var activeLocale = DEFAULT_LOCALE;
    var activeTitleKey = null;
    var formattersCache = Object.create(null);

    function normalizeLocale(raw) {
      if (!angular.isString(raw)) {
        return DEFAULT_LOCALE;
      }
      var lower = raw.trim().toLowerCase();
      if (lower === 'en' || lower === 'en-us' || lower.indexOf('en-') === 0) {
        return 'en-US';
      }
      return 'vi-VN';
    }

    function safeReadStorage() {
      try {
        if ($window && $window.localStorage && angular.isFunction($window.localStorage.getItem)) {
          var saved = $window.localStorage.getItem(STORAGE_KEY);
          if (saved) {
            return normalizeLocale(saved);
          }
        }
      } catch (e) {
        // Storage unavailable in sandbox or private browsing
      }
      return DEFAULT_LOCALE;
    }

    function safeWriteStorage(locale) {
      try {
        if ($window && $window.localStorage && angular.isFunction($window.localStorage.setItem)) {
          $window.localStorage.setItem(STORAGE_KEY, locale);
        }
      } catch (e) {
        // Ignore storage errors
      }
    }

    function applyDomLocale(locale) {
      try {
        var doc = ($window && $window.document) || (typeof document !== 'undefined' ? document : null);
        if (doc && doc.documentElement) {
          doc.documentElement.lang = locale === 'en-US' ? 'en' : 'vi';
        }
        if (activeTitleKey) {
          applyDocumentTitle(activeTitleKey);
        }
      } catch (e) {
        // Guarded for Node VM sandbox
      }
    }

    function applyDocumentTitle(titleKey) {
      try {
        var doc = ($window && $window.document) || (typeof document !== 'undefined' ? document : null);
        if (!doc) {
          return;
        }
        var brand = self.t('nav.brandName');
        var resolved = titleKey ? self.t(titleKey) : '';
        if (resolved && resolved !== titleKey) {
          doc.title = resolved + ' — ' + brand;
        } else if (titleKey) {
          doc.title = titleKey + ' — ' + brand;
        } else {
          doc.title = brand;
        }
      } catch (e) {
        // Ignore in sandbox
      }
    }

    function lookupNested(dict, dottedKey) {
      if (!dict || !angular.isString(dottedKey) || !dottedKey) {
        return undefined;
      }
      var parts = dottedKey.split('.');
      var cursor = dict;
      for (var i = 0; i < parts.length; i++) {
        if (cursor === null || cursor === undefined || typeof cursor !== 'object') {
          return undefined;
        }
        cursor = cursor[parts[i]];
      }
      return typeof cursor === 'string' ? cursor : undefined;
    }

    function interpolate(templateStr, params) {
      if (!params || !angular.isObject(params) || templateStr.indexOf('{{') === -1) {
        return templateStr;
      }
      return templateStr.replace(/\{\{\s*([A-Za-z0-9_.-]+)\s*\}\}/g, function (_match, token) {
        var val = params[token];
        return val !== undefined && val !== null ? String(val) : '';
      });
    }

    function getFormatters(locale) {
      if (!formattersCache[locale]) {
        formattersCache[locale] = {
          number: new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }),
          decimal: new Intl.NumberFormat(locale, { maximumFractionDigits: 2 }),
          dateTime: new Intl.DateTimeFormat(locale, {
            timeZone: DEFAULT_TIMEZONE,
            hour12: false,
            hour: '2-digit',
            minute: '2-digit',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
          }),
          dateOnly: new Intl.DateTimeFormat(locale, {
            timeZone: DEFAULT_TIMEZONE,
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
          })
        };
      }
      return formattersCache[locale];
    }

    self.getLocale = function () {
      return activeLocale;
    };

    self.getShortCode = function () {
      return activeLocale === 'en-US' ? 'en' : 'vi';
    };

    self.setLocale = function (locale) {
      var normalized = normalizeLocale(locale);
      activeLocale = normalized;
      safeWriteStorage(normalized);
      applyDomLocale(normalized);
      if ($rootScope && angular.isFunction($rootScope.$broadcast)) {
        $rootScope.$broadcast('i18n:changed', {
          locale: normalized,
          shortCode: self.getShortCode()
        });
      }
      return normalized;
    };

    self.toggleLocale = function () {
      return self.setLocale(activeLocale === 'vi-VN' ? 'en-US' : 'vi-VN');
    };

    self.t = function (key, params) {
      if (!angular.isString(key) || !key) {
        return '';
      }
      var activeDict = dictionaries[activeLocale] || dictionaries['vi-VN'];
      var found = lookupNested(activeDict, key);
      if (found === undefined) {
        var fallbackLocale = activeLocale === 'vi-VN' ? 'en-US' : 'vi-VN';
        found = lookupNested(dictionaries[fallbackLocale], key);
      }
      if (found === undefined) {
        return key;
      }
      return interpolate(found, params);
    };

    self.enumLabel = function (group, code) {
      if (code === undefined || code === null || code === '') {
        return '—';
      }
      var codeStr = String(code).trim();
      if (!group) {
        return codeStr;
      }
      var key = 'enums.' + group + '.' + codeStr;
      var translated = self.t(key);
      return translated === key ? codeStr : translated;
    };

    self.errorLabel = function (code, fallback) {
      var fallbackText = fallback || self.t('errors.REQUEST_FAILED');
      if (!angular.isString(code) || !code.trim()) {
        return fallbackText;
      }
      var normalizedCode = code.trim().toUpperCase();
      var key = 'errors.' + normalizedCode;
      var translated = self.t(key);
      return translated === key ? fallbackText : translated;
    };

    function deepMergeDict(target, patch) {
      if (!target || !patch || typeof patch !== 'object') {
        return;
      }
      var keys = Object.keys(patch);
      for (var i = 0; i < keys.length; i++) {
        var k = keys[i];
        var val = patch[k];
        if (val && typeof val === 'object' && !Array.isArray(val)) {
          if (!target[k] || typeof target[k] !== 'object') {
            target[k] = {};
          }
          deepMergeDict(target[k], val);
        } else if (typeof val === 'string') {
          target[k] = val;
        }
      }
    }

    self.extend = function (viPatch, enPatch) {
      if (viPatch) {
        deepMergeDict(dictionaries['vi-VN'], viPatch);
      }
      if (enPatch) {
        deepMergeDict(dictionaries['en-US'], enPatch);
      }
    };

    self.formatVnd = function (amount, suffixStyle) {
      var numeric = Number(amount);
      if (amount === null || amount === undefined || amount === '' || !isFinite(numeric)) {
        return suffixStyle === 'code' ? '0 VND' : '0 ₫';
      }
      var rounded = Math.round(numeric);
      // Canonical Vietnamese Dong dot grouping (e.g., 102.500.000 ₫) across both vi-VN and en-US
      var fmt = getFormatters('vi-VN').number;
      var digits = fmt.format(rounded);
      return suffixStyle === 'code' ? (digits + ' VND') : (digits + ' ₫');
    };

    self.formatNumber = function (n, decimals) {
      var numeric = Number(n);
      if (n === null || n === undefined || n === '' || !isFinite(numeric)) {
        return '0';
      }
      if (decimals && decimals > 0) {
        return new Intl.NumberFormat(activeLocale, {
          minimumFractionDigits: decimals,
          maximumFractionDigits: decimals
        }).format(numeric);
      }
      return getFormatters(activeLocale).number.format(numeric);
    };

    self.formatPoints = function (pts) {
      var formatted = self.formatNumber(Math.round(Number(pts) || 0));
      return formatted + ' ' + self.t('common.ptsShort');
    };

    self.formatDateTime = function (iso, mode) {
      if (!iso) {
        return '—';
      }
      var parsed = new Date(iso);
      var parsedMs = parsed.getTime();
      if (isNaN(parsedMs)) {
        return String(iso);
      }
      if (mode === 'relative') {
        return self.formatRelative(iso);
      }
      var fmts = getFormatters(activeLocale);
      if (mode === 'date') {
        return fmts.dateOnly.format(parsed);
      }
      return fmts.dateTime.format(parsed);
    };

    self.formatRelative = function (iso) {
      if (!iso) {
        return '—';
      }
      var parsed = new Date(iso);
      var parsedMs = parsed.getTime();
      if (isNaN(parsedMs)) {
        return String(iso);
      }
      var diffSeconds = Math.round((Date.now() - parsedMs) / 1000);
      var absSeconds = Math.abs(diffSeconds);
      if (absSeconds < 45) {
        return self.t('common.justNow');
      }
      var absMinutes = Math.floor(absSeconds / 60);
      if (absMinutes < 60) {
        return self.t(diffSeconds >= 0 ? 'common.minutesAgo' : 'common.inMinutes', { count: absMinutes });
      }
      var absHours = Math.floor(absMinutes / 60);
      if (absHours < 24) {
        return self.t(diffSeconds >= 0 ? 'common.hoursAgo' : 'common.inHours', { count: absHours });
      }
      var absDays = Math.floor(absHours / 24);
      if (absDays < 7) {
        return self.t(diffSeconds >= 0 ? 'common.daysAgo' : 'common.inDays', { count: absDays });
      }
      return getFormatters(activeLocale).dateOnly.format(parsed);
    };

    self.setPageTitle = function (titleKey) {
      activeTitleKey = titleKey || null;
      applyDocumentTitle(activeTitleKey);
    };

    // Initialize default or persisted locale on construction
    activeLocale = safeReadStorage();
    applyDomLocale(activeLocale);
  }
})();
