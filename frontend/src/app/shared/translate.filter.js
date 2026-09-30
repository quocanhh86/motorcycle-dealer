(function () {
  'use strict';

  angular
    .module('app.shared')
    .filter('t', translateFilter)
    .filter('enumT', enumTranslateFilter);

  translateFilter.$inject = ['I18nService'];
  function translateFilter(I18nService) {
    function translate(key, params) {
      if (!key) {
        return '';
      }
      if (I18nService && angular.isFunction(I18nService.t)) {
        return I18nService.t(key, params);
      }
      return String(key);
    }
    translate.$stateful = true;
    return translate;
  }

  enumTranslateFilter.$inject = ['I18nService'];
  function enumTranslateFilter(I18nService) {
    function translateEnum(value, group) {
      if (value === undefined || value === null || value === '') {
        return '—';
      }
      if (I18nService && angular.isFunction(I18nService.enumLabel)) {
        return I18nService.enumLabel(group, value);
      }
      return String(value);
    }
    translateEnum.$stateful = true;
    return translateEnum;
  }
})();
