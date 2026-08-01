(function () {
  'use strict';

  angular
    .module('app.shared')
    .filter('utcToLocal', utcToLocalFilter)
    .filter('utcToVnTime', utcToLocalFilter);

  utcToLocalFilter.$inject = ['I18nService'];
  function utcToLocalFilter(I18nService) {
    var fallbackDateTimeFormatter = new Intl.DateTimeFormat('vi-VN', {
      timeZone: 'Asia/Ho_Chi_Minh',
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });

    var fallbackDateOnlyFormatter = new Intl.DateTimeFormat('vi-VN', {
      timeZone: 'Asia/Ho_Chi_Minh',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });

    function formatFallbackRelative(parsedMs) {
      var diffSeconds = Math.round((Date.now() - parsedMs) / 1000);
      var absSeconds = Math.abs(diffSeconds);

      if (absSeconds < 45) {
        return 'Just now';
      }
      var absMinutes = Math.floor(absSeconds / 60);
      if (absMinutes < 60) {
        return diffSeconds >= 0 ? (absMinutes + 'm ago') : ('in ' + absMinutes + 'm');
      }
      var absHours = Math.floor(absMinutes / 60);
      if (absHours < 24) {
        return diffSeconds >= 0 ? (absHours + 'h ago') : ('in ' + absHours + 'h');
      }
      var absDays = Math.floor(absHours / 24);
      if (absDays < 7) {
        return diffSeconds >= 0 ? (absDays + 'd ago') : ('in ' + absDays + 'd');
      }
      return fallbackDateOnlyFormatter.format(new Date(parsedMs));
    }

    function formatUtcToHoChiMinh(utcIsoValue, mode) {
      if (!utcIsoValue) {
        return '—';
      }
      if (I18nService && angular.isFunction(I18nService.formatDateTime)) {
        return I18nService.formatDateTime(utcIsoValue, mode);
      }
      var parsed = new Date(utcIsoValue);
      var parsedMs = parsed.getTime();
      if (Number.isNaN(parsedMs)) {
        return String(utcIsoValue);
      }
      if (mode === 'date') {
        return fallbackDateOnlyFormatter.format(parsed);
      }
      if (mode === 'relative') {
        return formatFallbackRelative(parsedMs);
      }
      return fallbackDateTimeFormatter.format(parsed);
    }

    formatUtcToHoChiMinh.$stateful = true;
    return formatUtcToHoChiMinh;
  }
})();
