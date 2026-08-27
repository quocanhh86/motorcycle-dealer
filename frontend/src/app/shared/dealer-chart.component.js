(function () {
  'use strict';

  var CHART_I18N_VI = {
    charts: {
      defaultAriaLabel: 'Biểu đồ phân tích đại lý'
    }
  };

  var CHART_I18N_EN = {
    charts: {
      defaultAriaLabel: 'Dealer analytics chart'
    }
  };

  var RADIAL_CHART_TYPES = {
    doughnut: true,
    pie: true,
    polarArea: true
  };

  angular
    .module('app.shared')
    .run(registerDealerChartI18n)
    .component('dealerChart', {
      bindings: {
        config: '<',
        heightPx: '@?',
        ariaLabel: '@?'
      },
      controllerAs: '$ctrl',
      controller: DealerChartController,
      template:
        '<div class="chart-box" ng-style="{ height: ($ctrl.heightPx || 300) + \'px\' }">' +
          '<canvas role="img" ng-attr-aria-label="{{ $ctrl.ariaLabel || (\'charts.defaultAriaLabel\' | t) }}"></canvas>' +
        '</div>'
    });

  registerDealerChartI18n.$inject = ['I18nService'];
  function registerDealerChartI18n(I18nService) {
    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(CHART_I18N_VI, CHART_I18N_EN);
    }
  }

  DealerChartController.$inject = ['$element', '$rootScope', 'ThemeService', 'I18nService'];
  function DealerChartController($element, $rootScope, ThemeService, I18nService) {
    var $ctrl = this;
    var chartInstance = null;
    var activeChartType = null;
    var isDestroyed = false;
    var disposers = [];

    if (I18nService && angular.isFunction(I18nService.extend)) {
      I18nService.extend(CHART_I18N_VI, CHART_I18N_EN);
    }

    function resolveIsDark() {
      if (ThemeService && angular.isFunction(ThemeService.isDark)) {
        return Boolean(ThemeService.isDark());
      }
      try {
        if (typeof document !== 'undefined' && document.documentElement && angular.isFunction(document.documentElement.getAttribute)) {
          return document.documentElement.getAttribute('data-theme') === 'dark';
        }
      } catch (e) {
        // Safe in Node VM sandbox
      }
      return false;
    }

    function resolvePalette() {
      if (resolveIsDark()) {
        return {
          textPrimary: '#F8FAFC',
          textSecondary: '#F8FAFC',
          tickColor: '#94A3B8',
          gridColor: '#334155',
          borderColor: '#334155',
          surfaceCard: '#0F172A',
          tooltipBg: '#020617',
          tooltipTitle: '#F8FAFC',
          tooltipBody: '#E2E8F0',
          tooltipBorder: '#334155'
        };
      }
      return {
        textPrimary: '#0F172A',
        textSecondary: '#0F172A',
        tickColor: '#64748B',
        gridColor: '#E2E8F0',
        borderColor: '#E2E8F0',
        surfaceCard: '#FFFFFF',
        tooltipBg: '#0F172A',
        tooltipTitle: '#F8FAFC',
        tooltipBody: '#E2E8F0',
        tooltipBorder: '#1E293B'
      };
    }

    function applyThemeToConfig(clonedConfig) {
      var palette = resolvePalette();
      var chartType = String(clonedConfig.type || '').toLowerCase();
      var isRadial = Boolean(RADIAL_CHART_TYPES[chartType]);

      clonedConfig.options = clonedConfig.options || {};
      clonedConfig.options.color = palette.textPrimary;

      clonedConfig.options.plugins = clonedConfig.options.plugins || {};
      clonedConfig.options.plugins.legend = clonedConfig.options.plugins.legend || {};
      clonedConfig.options.plugins.legend.labels = clonedConfig.options.plugins.legend.labels || {};
      clonedConfig.options.plugins.legend.labels.color = palette.textPrimary;

      if (clonedConfig.options.plugins.title) {
        clonedConfig.options.plugins.title.color = palette.textPrimary;
      }

      clonedConfig.options.plugins.tooltip = clonedConfig.options.plugins.tooltip || {};
      clonedConfig.options.plugins.tooltip.backgroundColor = palette.tooltipBg;
      clonedConfig.options.plugins.tooltip.titleColor = palette.tooltipTitle;
      clonedConfig.options.plugins.tooltip.bodyColor = palette.tooltipBody;
      clonedConfig.options.plugins.tooltip.borderColor = palette.tooltipBorder;
      if (clonedConfig.options.plugins.tooltip.borderWidth === undefined) {
        clonedConfig.options.plugins.tooltip.borderWidth = 1;
      }

      if (isRadial) {
        var datasets = (clonedConfig.data && angular.isArray(clonedConfig.data.datasets))
          ? clonedConfig.data.datasets
          : [];
        for (var d = 0; d < datasets.length; d++) {
          if (datasets[d] && angular.isObject(datasets[d])) {
            datasets[d].borderColor = palette.surfaceCard;
            if (datasets[d].borderWidth === undefined) {
              datasets[d].borderWidth = 2;
            }
          }
        }
      } else {
        clonedConfig.options.scales = clonedConfig.options.scales || { x: {}, y: {} };
        var scaleKeys = Object.keys(clonedConfig.options.scales);
        if (scaleKeys.length === 0) {
          clonedConfig.options.scales.x = {};
          clonedConfig.options.scales.y = {};
          scaleKeys = ['x', 'y'];
        }
        for (var i = 0; i < scaleKeys.length; i++) {
          var scale = clonedConfig.options.scales[scaleKeys[i]];
          if (!scale || !angular.isObject(scale)) {
            continue;
          }
          scale.ticks = scale.ticks || {};
          scale.ticks.color = palette.tickColor;

          scale.grid = scale.grid || {};
          if (scale.grid.display !== false) {
            scale.grid.color = palette.gridColor;
          }

          scale.border = scale.border || {};
          scale.border.color = palette.borderColor;

          if (scale.title && angular.isObject(scale.title)) {
            scale.title.color = palette.textSecondary;
          }
        }
      }

      return clonedConfig;
    }

    $ctrl.$onInit = function () {
      if ($rootScope && angular.isFunction($rootScope.$on)) {
        disposers.push($rootScope.$on('theme:changed', function () {
          if (!isDestroyed) {
            renderOrUpdateChart();
          }
        }));
        disposers.push($rootScope.$on('i18n:changed', function () {
          if (!isDestroyed) {
            renderOrUpdateChart();
          }
        }));
      }
    };

    $ctrl.$postLink = function () {
      renderOrUpdateChart();
    };

    $ctrl.$onChanges = function (changes) {
      if (changes && changes.config && !changes.config.isFirstChange()) {
        renderOrUpdateChart();
      }
    };

    // Mandatory RISK-04 mitigation: release Chart.js canvas context and event subscriptions cleanly
    $ctrl.$onDestroy = function () {
      isDestroyed = true;
      for (var i = 0; i < disposers.length; i++) {
        if (angular.isFunction(disposers[i])) {
          disposers[i]();
        }
      }
      disposers.length = 0;

      if (chartInstance) {
        chartInstance.destroy();
        chartInstance = null;
      }
      activeChartType = null;
    };

    function renderOrUpdateChart() {
      if (isDestroyed || !$ctrl.config || !window.Chart) {
        return;
      }
      var rootNode = $element && $element[0];
      var canvas = rootNode ? rootNode.querySelector('canvas') : null;
      if (!canvas || !canvas.getContext) {
        return;
      }

      var clonedConfig = angular.copy($ctrl.config);
      clonedConfig.options = clonedConfig.options || {};
      if (clonedConfig.options.responsive === undefined) {
        clonedConfig.options.responsive = true;
      }
      if (clonedConfig.options.maintainAspectRatio === undefined) {
        clonedConfig.options.maintainAspectRatio = false;
      }

      applyThemeToConfig(clonedConfig);

      if (chartInstance) {
        var currentType = (chartInstance.config && chartInstance.config.type) || activeChartType;
        if (currentType !== clonedConfig.type) {
          chartInstance.destroy();
          chartInstance = null;
          activeChartType = null;
        } else {
          chartInstance.data = clonedConfig.data;
          chartInstance.options = clonedConfig.options;
          chartInstance.update('none');
          return;
        }
      }

      chartInstance = new window.Chart(canvas.getContext('2d'), clonedConfig);
      activeChartType = clonedConfig.type || null;
    }
  }
})();
