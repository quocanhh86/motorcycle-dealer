(function () {
  'use strict';

  angular
    .module('app.shared')
    .filter('vndCurrency', vndCurrencyFilter);

  vndCurrencyFilter.$inject = ['I18nService'];
  function vndCurrencyFilter(I18nService) {
    var fallbackFormatter = new Intl.NumberFormat('vi-VN', {
      maximumFractionDigits: 0
    });

    function formatVnd(amount, suffixStyle) {
      if (I18nService && angular.isFunction(I18nService.formatVnd)) {
        return I18nService.formatVnd(amount, suffixStyle);
      }
      if (amount === null || amount === undefined || amount === '') {
        return suffixStyle === 'code' ? '0 VND' : '0 ₫';
      }
      var numeric = Number(amount);
      if (!Number.isFinite(numeric)) {
        return suffixStyle === 'code' ? '0 VND' : '0 ₫';
      }
      var rounded = Math.round(numeric);
      var formattedDigits = fallbackFormatter.format(rounded);
      return suffixStyle === 'code' ? (formattedDigits + ' VND') : (formattedDigits + ' ₫');
    }

    formatVnd.$stateful = true;
    return formatVnd;
  }
})();
