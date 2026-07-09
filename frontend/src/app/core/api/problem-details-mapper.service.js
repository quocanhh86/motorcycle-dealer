(function () {
  'use strict';

  var STATUS_TO_DEFAULT_CODE = {
    400: 'VALIDATION_FAILED',
    401: 'UNAUTHORIZED',
    403: 'FORBIDDEN_PERMISSION',
    404: 'RESOURCE_NOT_FOUND',
    409: 'CONCURRENCY_CONFLICT',
    422: 'DOMAIN_RULE_VIOLATION',
    429: 'RATE_LIMIT_EXCEEDED',
    500: 'INTERNAL_SERVER_ERROR'
  };

  var CANONICAL_ERROR_CODES = Object.freeze([
    'VALIDATION_FAILED',
    'UNAUTHORIZED',
    'TOKEN_EXPIRED',
    'INVALID_REFRESH_TOKEN',
    'REFRESH_TOKEN_REUSE_DETECTED',
    'INVALID_CREDENTIALS',
    'ACCOUNT_LOCKED',
    'FORBIDDEN_PERMISSION',
    'FORBIDDEN_DEALER_SCOPE',
    'FORBIDDEN_RESOURCE_OWNER',
    'RESOURCE_NOT_FOUND',
    'CONCURRENCY_CONFLICT',
    'SLOT_CAPACITY_EXHAUSTED',
    'DUPLICATE_VIN',
    'INSUFFICIENT_LOYALTY_POINTS',
    'VOUCHER_INELIGIBLE',
    'REVIEW_ALREADY_SUBMITTED',
    'WARRANTY_COVERAGE_EXPIRED'
  ]);

  var EXTENDED_ERRORS_VI = {
    errors: {
      DOMAIN_RULE_VIOLATION: 'Yêu cầu vi phạm quy tắc nghiệp vụ của hệ thống. Vui lòng kiểm tra lại thông tin.',
      titles: {
        NETWORK_OFFLINE: 'Không thể kết nối máy chủ API',
        UNAUTHORIZED: 'Xác thực không thành công',
        FORBIDDEN: 'Không có quyền truy cập',
        NOT_FOUND: 'Không tìm thấy dữ liệu',
        CONFLICT: 'Xung đột dữ liệu',
        VALIDATION: 'Dữ liệu chưa hợp lệ',
        SERVER_ERROR: 'Lỗi hệ thống',
        GENERIC: 'Thao tác không thành công'
      }
    }
  };

  var EXTENDED_ERRORS_EN = {
    errors: {
      DOMAIN_RULE_VIOLATION: 'The request violates a domain business rule. Please review your input.',
      titles: {
        NETWORK_OFFLINE: 'Backend API Unreachable',
        UNAUTHORIZED: 'Authentication Failed',
        FORBIDDEN: 'Access Denied',
        NOT_FOUND: 'Resource Not Found',
        CONFLICT: 'Conflict Detected',
        VALIDATION: 'Validation Failed',
        SERVER_ERROR: 'Platform Error',
        GENERIC: 'Operation Failed'
      }
    }
  };

  angular
    .module('app.core')
    .service('ProblemDetailsMapper', ProblemDetailsMapper);

  ProblemDetailsMapper.$inject = ['I18nService'];
  function ProblemDetailsMapper(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(EXTENDED_ERRORS_VI, EXTENDED_ERRORS_EN);
    }

    function toCamelCase(fieldName) {
      if (!fieldName) {
        return '';
      }
      var clean = String(fieldName);
      var dotIdx = clean.lastIndexOf('.');
      if (dotIdx !== -1 && dotIdx < clean.length - 1) {
        clean = clean.substring(dotIdx + 1);
      }
      return clean.charAt(0).toLowerCase() + clean.slice(1);
    }

    function attachServerAutoReset(ngModelCtrl) {
      if (!ngModelCtrl || ngModelCtrl._hasServerErrorAutoReset || !angular.isArray(ngModelCtrl.$viewChangeListeners)) {
        return;
      }
      ngModelCtrl._hasServerErrorAutoReset = true;
      ngModelCtrl.$viewChangeListeners.push(function () {
        ngModelCtrl.$setValidity('server', true);
      });
    }

    function localizeError(code, fallbackText) {
      if (I18nService && angular.isFunction(I18nService.errorLabel)) {
        return I18nService.errorLabel(code, fallbackText);
      }
      return fallbackText;
    }

    function localizeTitle(titleKey, fallbackTitle) {
      if (I18nService && angular.isFunction(I18nService.t)) {
        var resolved = I18nService.t('errors.titles.' + titleKey);
        if (resolved && resolved !== ('errors.titles.' + titleKey)) {
          return resolved;
        }
      }
      return fallbackTitle;
    }

    function defaultTitleForStatus(rawStatus) {
      if (rawStatus === 400 || rawStatus === 422) {
        return localizeTitle('VALIDATION', 'Validation Failed');
      }
      if (rawStatus === 401) {
        return localizeTitle('UNAUTHORIZED', 'Authentication Failed');
      }
      if (rawStatus === 403) {
        return localizeTitle('FORBIDDEN', 'Access Denied');
      }
      if (rawStatus === 404) {
        return localizeTitle('NOT_FOUND', 'Resource Not Found');
      }
      if (rawStatus === 409) {
        return localizeTitle('CONFLICT', 'Conflict Detected');
      }
      if (rawStatus >= 500) {
        return localizeTitle('SERVER_ERROR', 'Platform Error');
      }
      return localizeTitle('GENERIC', 'Operation Failed');
    }

    this.getCanonicalErrorCodes = function () {
      return CANONICAL_ERROR_CODES.slice();
    };

    this.mapErrorCode = function (errorCode, fallbackDetail) {
      return localizeError(errorCode, fallbackDetail);
    };

    this.unwrapData = function (responseOrBody) {
      var body = responseOrBody && responseOrBody.status !== undefined && responseOrBody.data !== undefined
        ? responseOrBody.data
        : responseOrBody;
      if (body && angular.isObject(body) && body.data !== undefined && !angular.isArray(body)) {
        return body.data;
      }
      return body;
    };

    this.unwrapList = function (responseOrBody) {
      var unwrapped = this.unwrapData(responseOrBody);
      if (angular.isArray(unwrapped)) {
        return unwrapped;
      }
      if (unwrapped && angular.isArray(unwrapped.items)) {
        return unwrapped.items;
      }
      if (unwrapped && angular.isArray(unwrapped.data)) {
        return unwrapped.data;
      }
      return [];
    };

    this.extract = function (httpRejection) {
      var payload = (httpRejection && httpRejection.data && angular.isObject(httpRejection.data))
        ? httpRejection.data
        : {};
      var rawStatus = httpRejection && typeof httpRejection.status === 'number'
        ? httpRejection.status
        : (typeof payload.status === 'number' ? payload.status : 0);

      if (rawStatus <= 0) {
        var offlineFallback = 'Could not connect to MotorcycleDealer.WebApi (/api/v1). Verify the .NET 10 API server is running and reachable.';
        return {
          status: 0,
          title: localizeTitle('NETWORK_OFFLINE', 'Backend API Unreachable'),
          detail: localizeError('NETWORK_OFFLINE', offlineFallback),
          errorCode: 'NETWORK_OFFLINE',
          traceId: null,
          errors: {}
        };
      }

      var traceId = payload.traceId || null;
      var rawCode = payload.errorCode || payload.code || STATUS_TO_DEFAULT_CODE[rawStatus] || (rawStatus >= 500 ? 'INTERNAL_SERVER_ERROR' : 'REQUEST_FAILED');
      var errorCode = String(rawCode).trim();
      var safeTitle = payload.title || defaultTitleForStatus(rawStatus);
      var rawDetail = rawStatus >= 500
        ? 'An unexpected platform error occurred. Reference traceId for support.'
        : (payload.detail || payload.title || 'Request could not be completed.');
      var localizedDetail = localizeError(errorCode, rawDetail);

      return {
        status: rawStatus,
        title: safeTitle,
        detail: localizedDetail,
        rawDetail: payload.detail || null,
        errorCode: errorCode,
        traceId: traceId,
        errors: angular.isObject(payload.errors) ? payload.errors : {}
      };
    };

    this.fromHttpError = function (httpRejection) {
      var extracted = this.extract(httpRejection);
      return angular.extend({ message: extracted.detail }, extracted);
    };

    this.clearFieldError = function (formController, fieldErrors, fieldName, extraKeys) {
      if (fieldErrors && angular.isObject(fieldErrors) && fieldName) {
        delete fieldErrors[fieldName];
        if (angular.isArray(extraKeys)) {
          for (var i = 0; i < extraKeys.length; i++) {
            if (extraKeys[i]) {
              delete fieldErrors[extraKeys[i]];
            }
          }
        }
      }
      if (
        formController &&
        fieldName &&
        formController[fieldName] &&
        angular.isFunction(formController[fieldName].$setValidity)
      ) {
        formController[fieldName].$setValidity('server', true);
      }
    };

    this.clearServerErrors = function (formController) {
      if (!formController || !angular.isObject(formController)) {
        return;
      }
      var keys = Object.keys(formController);
      for (var i = 0; i < keys.length; i++) {
        var key = keys[i];
        if (key.charAt(0) !== '$' && formController[key] && angular.isFunction(formController[key].$setValidity)) {
          formController[key].$setValidity('server', true);
        }
      }
    };

    this.applyToForm = function (formController, problem) {
      this.clearServerErrors(formController);
      var fieldMessages = {};
      if (!problem || !angular.isObject(problem.errors)) {
        return fieldMessages;
      }

      var keys = Object.keys(problem.errors);
      for (var i = 0; i < keys.length; i++) {
        var rawField = keys[i];
        var camelKey = toCamelCase(rawField);
        var messages = problem.errors[rawField];
        var joined = angular.isArray(messages) ? messages.join(' ') : String(messages || '');
        if (camelKey) {
          fieldMessages[camelKey] = joined;
        }

        if (formController && camelKey && formController[camelKey] && formController[camelKey].$setValidity) {
          attachServerAutoReset(formController[camelKey]);
          formController[camelKey].$setValidity('server', false);
        }
      }

      return fieldMessages;
    };
  }
})();
