(function () {
  'use strict';

  angular
    .module('app.core')
    .service('DealerApiService', DealerApiService);

  DealerApiService.$inject = ['$http', '$q', 'APP_CONFIG', 'ProblemDetailsMapper', 'I18nService'];
  function DealerApiService($http, $q, APP_CONFIG, ProblemDetailsMapper, I18nService) {
    var self = this;
    var idempSeq = 0;

    function isVietnameseActive() {
      if (I18nService && angular.isFunction(I18nService.getLocale)) {
        var loc = I18nService.getLocale();
        return angular.isString(loc) && loc.trim().toLowerCase().indexOf('vi') === 0;
      }
      return false;
    }

    function baseUrl() {
      return String(APP_CONFIG.apiBaseUrl || 'http://localhost:5080/api/v1').replace(/\/+$/, '');
    }

    function pickDefined(obj, keys, fallback) {
      if (!obj || !angular.isObject(obj)) {
        return fallback;
      }
      for (var i = 0; i < keys.length; i++) {
        var val = obj[keys[i]];
        if (val !== undefined && val !== null) {
          return val;
        }
      }
      return fallback;
    }

    function formatShortTime(value, fallback) {
      if (!angular.isString(value) || !value.trim()) {
        return fallback || '08:00';
      }
      return value.trim().slice(0, 5);
    }

    function formatDateOnly(value) {
      if (!value) {
        return '';
      }
      var str = String(value).trim();
      if (/^[0-9]{4}-[0-9]{2}-[0-9]{2}/.test(str)) {
        return str.slice(0, 10);
      }
      var parsed = Date.parse(str);
      if (!isNaN(parsed)) {
        return self.formatDateVn(new Date(parsed));
      }
      return str;
    }

    self.cleanParams = function (rawParams) {
      var cleaned = {};
      if (!rawParams || !angular.isObject(rawParams)) {
        return cleaned;
      }
      var keys = Object.keys(rawParams);
      for (var i = 0; i < keys.length; i++) {
        var k = keys[i];
        var val = rawParams[k];
        if (val === undefined || val === null) {
          continue;
        }
        if (angular.isString(val)) {
          var trimmed = val.trim();
          if (!trimmed) {
            continue;
          }
          cleaned[k] = trimmed;
        } else {
          cleaned[k] = val;
        }
      }
      return cleaned;
    };

    self.generateIdempotencyKey = function (prefix) {
      idempSeq++;
      var cleanPrefix = String(prefix || 'op').replace(/[^A-Za-z0-9_-]/g, '');
      var randPart = Math.floor(Math.random() * 16777215).toString(16);
      return cleanPrefix + '-' + Date.now().toString(36) + '-' + idempSeq + '-' + randPart;
    };

    self.formatDateVn = function (dateInput) {
      var dt = dateInput instanceof Date ? dateInput : new Date(dateInput || Date.now());
      if (isNaN(dt.getTime())) {
        dt = new Date();
      }
      try {
        var parts = new Intl.DateTimeFormat('en-CA', {
          timeZone: APP_CONFIG.defaultTimeZone || 'Asia/Ho_Chi_Minh',
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        }).formatToParts(dt);
        var year = '';
        var month = '';
        var day = '';
        for (var i = 0; i < parts.length; i++) {
          if (parts[i].type === 'year') { year = parts[i].value; }
          if (parts[i].type === 'month') { month = parts[i].value; }
          if (parts[i].type === 'day') { day = parts[i].value; }
        }
        if (year && month && day) {
          return year + '-' + month + '-' + day;
        }
      } catch (e) {
        // Fallback to UTC offset +07:00 math
      }
      var shifted = new Date(dt.getTime() + 7 * 3600 * 1000);
      return shifted.toISOString().slice(0, 10);
    };

    self.getTodayDateVn = function () {
      return self.formatDateVn(new Date());
    };

    self.addDaysDateVn = function (yyyyMmDd, deltaDays) {
      var baseStr = formatDateOnly(yyyyMmDd) || self.getTodayDateVn();
      var segments = baseStr.split('-');
      var utcDate = new Date(Date.UTC(
        Number(segments[0]) || 2026,
        (Number(segments[1]) || 1) - 1,
        (Number(segments[2]) || 1) + (Number(deltaDays) || 0),
        12, 0, 0
      ));
      return utcDate.toISOString().slice(0, 10);
    };

    self.unwrapItem = function (responseOrBody) {
      return ProblemDetailsMapper.unwrapData(responseOrBody);
    };

    self.unwrapList = function (responseOrBody) {
      return ProblemDetailsMapper.unwrapList(responseOrBody);
    };

    self.unwrapPaged = function (responseOrBody) {
      var rawBody = responseOrBody && responseOrBody.status !== undefined && responseOrBody.data !== undefined
        ? responseOrBody.data
        : responseOrBody;
      var dataLayer = ProblemDetailsMapper.unwrapData(responseOrBody);
      var items = ProblemDetailsMapper.unwrapList(responseOrBody);
      var pagination = (dataLayer && angular.isObject(dataLayer) && dataLayer.pagination) ||
        (rawBody && angular.isObject(rawBody) && rawBody.pagination) ||
        {
          pageNumber: 1,
          pageSize: items.length || 20,
          totalCount: items.length,
          totalPages: items.length > 0 ? 1 : 0,
          hasPreviousPage: false,
          hasNextPage: false
        };
      return {
        items: items,
        pagination: angular.extend({}, pagination)
      };
    };

    // --- Normalizers (Always produce extensible objects safe for ng-repeat & ng-options) ---

    self.normalizeDealer = function (raw) {
      var d = raw || {};
      var openHm = formatShortTime(pickDefined(d, ['openingTime', 'OpeningTime'], '08:00'), '08:00');
      var closeHm = formatShortTime(pickDefined(d, ['closingTime', 'ClosingTime'], '18:00'), '18:00');
      return {
        id: String(pickDefined(d, ['id', 'dealerId', 'Id'], '')),
        code: String(pickDefined(d, ['code', 'dealerCode', 'Code'], '')).toUpperCase(),
        name: String(pickDefined(d, ['name', 'dealerName', 'Name'], '')),
        city: String(pickDefined(d, ['city', 'City'], '')),
        address: String(pickDefined(d, ['address', 'Address'], '')),
        phoneNumber: String(pickDefined(d, ['phoneNumber', 'PhoneNumber'], '')),
        contactEmail: String(pickDefined(d, ['contactEmail', 'ContactEmail'], '')),
        openingTime: openHm,
        closingTime: closeHm,
        hoursDisplay: openHm + ' - ' + closeHm,
        defaultSlotCapacity: Number(pickDefined(d, ['defaultSlotCapacity', 'DefaultSlotCapacity'], 4)) || 4,
        availableVinCount: Number(pickDefined(d, ['availableVinCount', 'AvailableVinCount'], 0)) || 0,
        isActive: pickDefined(d, ['isActive', 'IsActive'], true) !== false
      };
    };

    self.normalizeMaintenanceRule = function (raw) {
      var r = raw || {};
      return {
        id: String(pickDefined(r, ['id', 'ruleId', 'Id'], '')),
        motorcycleModelId: String(pickDefined(r, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        milestoneCode: String(pickDefined(r, ['milestoneCode', 'code', 'MilestoneCode'], '')),
        milestoneName: String(pickDefined(r, ['milestoneName', 'name', 'MilestoneName'], '')),
        intervalKm: Number(pickDefined(r, ['intervalKm', 'IntervalKm'], 1000)) || 1000,
        intervalMonths: Number(pickDefined(r, ['intervalMonths', 'IntervalMonths'], 1)) || 1,
        isRecurring: Boolean(pickDefined(r, ['isRecurring', 'IsRecurring'], true)),
        leadTimeDays: Number(pickDefined(r, ['leadTimeDays', 'LeadTimeDays'], 7)) || 0,
        leadMileageKm: Number(pickDefined(r, ['leadMileageKm', 'LeadMileageKm'], 250)) || 0,
        estimatedDurationMinutes: Number(pickDefined(r, ['estimatedDurationMinutes', 'EstimatedDurationMinutes'], 60)) || 60,
        checklistSummary: String(pickDefined(r, ['checklistSummary', 'ChecklistSummary'], '')),
        isActive: pickDefined(r, ['isActive', 'IsActive'], true) !== false
      };
    };

    self.normalizeMotorcycleModel = function (raw) {
      var m = raw || {};
      var rawRules = pickDefined(m, ['maintenanceRules', 'rules', 'MaintenanceRules'], []);
      var rules = angular.isArray(rawRules)
        ? rawRules.map(self.normalizeMaintenanceRule).sort(function (a, b) { return a.intervalKm - b.intervalKm; })
        : [];
      var specsRaw = pickDefined(m, ['specificationsJson', 'SpecificationsJson'], null);
      var parsedSpecs = {};
      if (angular.isString(specsRaw) && specsRaw.trim()) {
        try {
          var obj = JSON.parse(specsRaw);
          if (obj && angular.isObject(obj) && !angular.isArray(obj)) {
            parsedSpecs = obj;
          }
        } catch (e) {
          parsedSpecs = {};
        }
      } else if (specsRaw && angular.isObject(specsRaw)) {
        parsedSpecs = angular.extend({}, specsRaw);
      }

      var rawDealerStocks = pickDefined(m, ['dealerStocks', 'dealerStock', 'dealerStockBreakdown', 'stockByDealer'], []);
      var dealerStocks = angular.isArray(rawDealerStocks)
        ? rawDealerStocks.map(function (ds) {
          return {
            dealerId: String(pickDefined(ds, ['dealerId', 'id'], '')),
            dealerCode: String(pickDefined(ds, ['dealerCode', 'code'], '')),
            dealerName: String(pickDefined(ds, ['dealerName', 'name'], '')),
            city: String(pickDefined(ds, ['city'], '')),
            availableCount: Number(pickDefined(ds, ['availableCount', 'availableVinCount', 'availableStockCount', 'count'], 0)) || 0
          };
        })
        : [];

      return {
        id: String(pickDefined(m, ['id', 'modelId', 'Id'], '')),
        modelCode: String(pickDefined(m, ['modelCode', 'code', 'ModelCode'], '')),
        brand: String(pickDefined(m, ['brand', 'Brand'], 'Honda')),
        modelName: String(pickDefined(m, ['modelName', 'name', 'ModelName'], '')),
        category: String(pickDefined(m, ['category', 'Category'], 'Scooter')),
        engineDisplacementCc: Number(pickDefined(m, ['engineDisplacementCc', 'cc', 'EngineDisplacementCc'], 150)) || 150,
        baseRetailPrice: Number(pickDefined(m, ['baseRetailPrice', 'msrpVnd', 'BaseRetailPrice'], 0)) || 0,
        warrantyMonths: Number(pickDefined(m, ['warrantyMonths', 'WarrantyMonths'], 36)) || 36,
        warrantyMileageKm: Number(pickDefined(m, ['warrantyMileageKm', 'WarrantyMileageKm'], 30000)) || 30000,
        imageUrl: pickDefined(m, ['imageUrl', 'ImageUrl'], null),
        specificationsJson: angular.isString(specsRaw) ? specsRaw : (Object.keys(parsedSpecs).length ? JSON.stringify(parsedSpecs) : null),
        specifications: parsedSpecs,
        availableStockCount: Number(pickDefined(m, ['availableStockCount', 'availableVinCount', 'availableVins', 'AvailableStockCount'], 0)) || 0,
        dealerStocks: dealerStocks,
        maintenanceRules: rules,
        isActive: pickDefined(m, ['isActive', 'IsActive'], true) !== false
      };
    };

    self.normalizeInventory = function (raw) {
      var inv = raw || {};
      var resolvedId = String(pickDefined(inv, ['id', 'inventoryId', 'dealerInventoryId', 'Id'], ''));
      return {
        id: resolvedId,
        inventoryId: resolvedId,
        dealerId: String(pickDefined(inv, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(inv, ['dealerCode', 'DealerCode'], '')),
        dealerName: String(pickDefined(inv, ['dealerName', 'DealerName'], '')),
        motorcycleModelId: String(pickDefined(inv, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        modelCode: String(pickDefined(inv, ['modelCode', 'ModelCode'], '')),
        motorcycleModelName: String(pickDefined(inv, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        imageUrl: String(pickDefined(inv, ['imageUrl', 'ImageUrl', 'motorcycleModelImageUrl', 'modelImageUrl', 'MotorcycleModelImageUrl', 'ModelImageUrl'], '') || '').trim(),
        brand: String(pickDefined(inv, ['brand', 'Brand'], '')),
        category: String(pickDefined(inv, ['category', 'Category'], '')),
        vin: String(pickDefined(inv, ['vin', 'VIN'], '')).toUpperCase(),
        engineNumber: String(pickDefined(inv, ['engineNumber', 'EngineNumber'], '')),
        colorName: String(pickDefined(inv, ['colorName', 'color', 'ColorName'], '')),
        manufactureYear: Number(pickDefined(inv, ['manufactureYear', 'ManufactureYear'], 2026)) || 2026,
        listedPrice: Number(pickDefined(inv, ['listedPrice', 'priceVnd', 'ListedPrice'], 0)) || 0,
        stockStatus: String(pickDefined(inv, ['stockStatus', 'StockStatus'], 'Available')),
        receivedAtUtc: pickDefined(inv, ['receivedAtUtc', 'ReceivedAtUtc'], null),
        soldAtUtc: pickDefined(inv, ['soldAtUtc', 'SoldAtUtc'], null),
        rowVersion: String(pickDefined(inv, ['rowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeSalesOrder = function (raw) {
      var so = raw || {};
      var resolvedId = String(pickDefined(so, ['salesOrderId', 'id', 'SalesOrderId', 'Id'], ''));
      return {
        id: resolvedId,
        salesOrderId: resolvedId,
        orderNumber: String(pickDefined(so, ['orderNumber', 'OrderNumber'], '')),
        dealerId: String(pickDefined(so, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(so, ['dealerCode', 'DealerCode'], '')),
        dealerName: String(pickDefined(so, ['dealerName', 'DealerName'], '')),
        customerId: String(pickDefined(so, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(so, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(so, ['customerEmail', 'CustomerEmail'], '')),
        dealerInventoryId: String(pickDefined(so, ['dealerInventoryId', 'DealerInventoryId'], '')),
        vin: String(pickDefined(so, ['vin', 'VIN'], '')).toUpperCase(),
        motorcycleModelName: String(pickDefined(so, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        licensePlate: pickDefined(so, ['licensePlate', 'LicensePlate'], null),
        totalAmount: Number(pickDefined(so, ['totalAmount', 'TotalAmount'], 0)) || 0,
        discountAmount: Number(pickDefined(so, ['discountAmount', 'DiscountAmount'], 0)) || 0,
        finalAmount: Number(pickDefined(so, ['finalAmount', 'FinalAmount'], 0)) || 0,
        pointsEarned: Number(pickDefined(so, ['pointsEarned', 'PointsEarned'], 0)) || 0,
        appliedTierCode: String(pickDefined(so, ['appliedTierCode', 'previousTierCode', 'AppliedTierCode'], 'Bronze')),
        appliedMultiplier: Number(pickDefined(so, ['appliedMultiplier', 'pointsMultiplier', 'AppliedMultiplier'], 1.0)) || 1.0,
        newTierCode: String(pickDefined(so, ['newTierCode', 'currentTierCode', 'NewTierCode'], pickDefined(so, ['appliedTierCode'], 'Bronze'))),
        tierUpgraded: Boolean(pickDefined(so, ['tierUpgraded', 'TierUpgraded'], false)),
        availablePointsAfter: pickDefined(so, ['availablePointsAfter', 'AvailablePointsAfter'], null),
        lifetimeQualifyingPointsAfter: pickDefined(so, ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'], null),
        onboardedCustomerVehicleId: pickDefined(so, ['onboardedCustomerVehicleId', 'customerVehicleId', 'OnboardedCustomerVehicleId'], null),
        paymentMethod: String(pickDefined(so, ['paymentMethod', 'PaymentMethod'], 'BankTransfer')),
        status: String(pickDefined(so, ['status', 'Status'], 'Completed')),
        notes: pickDefined(so, ['notes', 'Notes'], null),
        completedAtUtc: pickDefined(so, ['completedAtUtc', 'CompletedAtUtc', 'createdAtUtc', 'CreatedAtUtc'], null),
        createdAtUtc: pickDefined(so, ['createdAtUtc', 'CreatedAtUtc', 'completedAtUtc', 'CompletedAtUtc'], null)
      };
    };

    self.computeVehicleMaintenanceStatus = function (currentKm, nextDueKm, nextDueDateUtc, explicitStatus, isDueFlag) {
      if (angular.isString(explicitStatus) && explicitStatus.trim()) {
        var norm = explicitStatus.trim().toLowerCase();
        if (norm === 'overdue') { return 'Overdue'; }
        if (norm === 'duesoon' || norm === 'due_soon' || norm === 'due') { return 'DueSoon'; }
        if (norm === 'upcoming' || norm === 'ok' || norm === 'onschedule') { return 'Upcoming'; }
      }
      var cur = Number(currentKm) || 0;
      var target = Number(nextDueKm) || 0;
      var nowMs = Date.now();
      var dueMs = nextDueDateUtc ? Date.parse(nextDueDateUtc) : NaN;

      if ((target > 0 && cur >= target) || (!isNaN(dueMs) && dueMs <= nowMs)) {
        return 'Overdue';
      }
      if (
        isDueFlag === true ||
        (target > 0 && target - cur <= 300) ||
        (!isNaN(dueMs) && dueMs - nowMs <= 14 * 86400 * 1000)
      ) {
        return 'DueSoon';
      }
      return 'Upcoming';
    };

    function addCalendarMonthsUtc(baseDate, monthsToAdd) {
      var year = baseDate.getUTCFullYear();
      var month = baseDate.getUTCMonth();
      var day = baseDate.getUTCDate();
      var targetMonthTotal = month + monthsToAdd;
      var targetYear = year + Math.floor(targetMonthTotal / 12);
      var targetMonth = ((targetMonthTotal % 12) + 12) % 12;
      var maxDaysInTargetMonth = new Date(Date.UTC(targetYear, targetMonth + 1, 0)).getUTCDate();
      var clampedDay = Math.min(day, maxDaysInTargetMonth);
      return new Date(Date.UTC(
        targetYear,
        targetMonth,
        clampedDay,
        baseDate.getUTCHours(),
        baseDate.getUTCMinutes(),
        baseDate.getUTCSeconds(),
        baseDate.getUTCMilliseconds()
      ));
    }

    self.evaluateVehicleWarrantyEligibility = function (vehicle, referenceUtc) {
      var v = vehicle || {};
      var maxKm = Math.max(1000, Math.floor(Number(pickDefined(v, ['warrantyMileageKm', 'WarrantyMileageKm'], 30000)) || 30000));
      var maxMonths = Math.max(1, Math.floor(Number(pickDefined(v, ['warrantyMonths', 'WarrantyMonths'], 36)) || 36));
      var currentKm = Math.max(0, Math.floor(Number(pickDefined(v, ['currentMileageKm', 'mileageKm', 'CurrentMileageKm'], 0)) || 0));
      var remainingKm = Math.max(0, maxKm - currentKm);

      var nowMs = referenceUtc ? Date.parse(referenceUtc) : Date.now();
      if (isNaN(nowMs)) {
        nowMs = Date.now();
      }

      var rawStart = pickDefined(
        v,
        ['ownershipStartDateUtc', 'purchasedAtUtc', 'createdAtUtc', 'OwnershipStartDateUtc', 'PurchasedAtUtc', 'CreatedAtUtc'],
        null
      );
      var startMs = rawStart ? Date.parse(rawStart) : NaN;
      var hasStart = !isNaN(startMs);
      var startIso = hasStart ? new Date(startMs).toISOString() : null;
      var expiryDate = hasStart ? addCalendarMonthsUtc(new Date(startMs), maxMonths) : null;
      var expiryMs = expiryDate ? expiryDate.getTime() : NaN;
      var expiryIso = expiryDate ? expiryDate.toISOString() : null;

      var explicitExpired = pickDefined(v, ['isWarrantyExpired', 'warrantyExpired', 'IsWarrantyExpired'], null);
      var explicitCovered = pickDefined(v, ['isUnderWarranty', 'isWarrantyCovered', 'IsUnderWarranty', 'IsWarrantyCovered'], null);

      var isTimeValid = hasStart ? (nowMs <= expiryMs) : (explicitExpired === true ? false : true);
      var isMileageValid = currentKm <= maxKm;
      var isCovered = explicitCovered !== null && explicitCovered !== undefined
        ? (Boolean(explicitCovered) && isMileageValid && isTimeValid)
        : (isMileageValid && isTimeValid);

      var remainingDays = hasStart
        ? Math.max(0, Math.ceil((expiryMs - nowMs) / 86400000))
        : (maxMonths * 30);
      var remainingMonths = hasStart
        ? Math.max(0, Math.min(maxMonths, Math.floor(remainingDays / 30.4375)))
        : maxMonths;

      var reason = null;
      if (!isMileageValid) {
        reason = 'Odometer (' + currentKm.toLocaleString('en-US') + ' km) exceeds OEM ' + maxKm.toLocaleString('en-US') + ' km warranty limit.';
      } else if (!isTimeValid) {
        reason = 'Vehicle has exceeded the ' + maxMonths + '-month OEM warranty coverage window.';
      }

      return {
        isCovered: isCovered,
        isEligible: isCovered,
        statusCode: isCovered ? 'Covered' : 'OutOfWarranty',
        statusLabel: isCovered
          ? ('Covered (<= ' + maxKm.toLocaleString('en-US') + ' km & ' + maxMonths + 'm)')
          : 'Out of Warranty',
        currentMileageKm: currentKm,
        maxMileageKm: maxKm,
        remainingKm: remainingKm,
        maxMonths: maxMonths,
        warrantyStartUtc: startIso,
        warrantyExpiresAtUtc: expiryIso,
        remainingDays: remainingDays,
        remainingMonths: remainingMonths,
        ineligibleReason: reason
      };
    };

    self.normalizeVehicle = function (raw) {
      var v = raw || {};
      var resolvedId = String(pickDefined(v, ['id', 'vehicleId', 'customerVehicleId', 'Id'], ''));
      var currentKm = Number(pickDefined(v, ['currentMileageKm', 'mileageKm', 'CurrentMileageKm'], 0)) || 0;
      var lastKm = Number(pickDefined(v, ['lastServiceMileageKm', 'LastServiceMileageKm'], 0)) || 0;
      var nextKm = Number(pickDefined(v, ['nextDueMileageKm', 'nextMilestoneKm', 'NextDueMileageKm'], 0)) || 0;
      var nextDate = pickDefined(v, ['nextDueDateUtc', 'NextDueDateUtc'], null);
      var explicitStatus = pickDefined(v, ['maintenanceStatus', 'milestoneStatus', 'dueStatus', 'MaintenanceStatus'], null);
      var isDueFlag = pickDefined(v, ['isDueSoonOrOverdue', 'isDueSoon', 'IsDueSoonOrOverdue'], false);
      var status = self.computeVehicleMaintenanceStatus(currentKm, nextKm, nextDate, explicitStatus, isDueFlag);

      var span = Math.max(1, nextKm - lastKm);
      var covered = Math.max(0, currentKm - lastKm);
      var wearPct = nextKm > 0 ? Math.min(100, Math.round((covered / span) * 100)) : 0;
      var warrantyInfo = self.evaluateVehicleWarrantyEligibility(v);

      return {
        id: resolvedId,
        vehicleId: resolvedId,
        vin: String(pickDefined(v, ['vin', 'VIN'], '')).toUpperCase(),
        customerId: String(pickDefined(v, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(v, ['customerFullName', 'customerName', 'ownerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(v, ['customerEmail', 'ownerEmail', 'CustomerEmail'], '')),
        customerPhone: String(pickDefined(v, ['customerPhoneNumber', 'customerPhone', 'phoneNumber', 'CustomerPhoneNumber'], '')),
        tierCode: pickDefined(v, ['tierCode', 'currentTierCode', 'TierCode'], null),
        tierMultiplier: pickDefined(v, ['tierMultiplier', 'pointsMultiplier', 'TierMultiplier'], null),
        availablePoints: pickDefined(v, ['availablePoints', 'AvailablePoints'], null),
        motorcycleModelId: String(pickDefined(v, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        modelCode: String(pickDefined(v, ['modelCode', 'ModelCode'], '')),
        motorcycleModelName: String(pickDefined(v, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        imageUrl: String(pickDefined(v, ['imageUrl', 'ImageUrl', 'motorcycleModelImageUrl', 'modelImageUrl', 'MotorcycleModelImageUrl', 'ModelImageUrl'], '') || '').trim(),
        brand: String(pickDefined(v, ['brand', 'Brand'], '')),
        category: String(pickDefined(v, ['category', 'Category'], '')),
        engineDisplacementCc: Number(pickDefined(v, ['engineDisplacementCc', 'EngineDisplacementCc'], 0)) || 0,
        purchasedSalesOrderId: pickDefined(v, ['purchasedSalesOrderId', 'PurchasedSalesOrderId'], null),
        licensePlate: pickDefined(v, ['licensePlate', 'plate', 'LicensePlate'], null),
        colorName: String(pickDefined(v, ['colorName', 'color', 'ColorName'], '')),
        registrationSource: String(
          pickDefined(v, ['registrationSource', 'RegistrationSource'], v.purchasedSalesOrderId ? 'ShowroomSale' : 'ExternalManual')
        ),
        purchasedAtUtc: pickDefined(v, ['purchasedAtUtc', 'PurchasedAtUtc'], null),
        ownershipStartDateUtc: pickDefined(v, ['ownershipStartDateUtc', 'OwnershipStartDateUtc'], null),
        createdAtUtc: pickDefined(v, ['createdAtUtc', 'CreatedAtUtc'], null),
        currentMileageKm: currentKm,
        lastServiceMileageKm: lastKm,
        lastServiceDateUtc: pickDefined(v, ['lastServiceDateUtc', 'LastServiceDateUtc'], null),
        nextDueMileageKm: nextKm,
        nextDueDateUtc: nextDate,
        nextMilestoneName: pickDefined(v, ['nextMilestoneName', 'NextMilestoneName'], null),
        activeReminderCount: Number(pickDefined(v, ['activeReminderCount', 'ActiveReminderCount'], 0)) || 0,
        recentBookings: angular.isArray(v.recentBookings) ? v.recentBookings.slice() : [],
        recentInvoices: angular.isArray(v.recentInvoices) ? v.recentInvoices.slice() : [],
        kmRemaining: nextKm > 0 ? (nextKm - currentKm) : 0,
        wearProgressPct: wearPct,
        maintenanceStatus: status,
        isDueSoonOrOverdue: status === 'DueSoon' || status === 'Overdue',
        isWarrantyCovered: warrantyInfo.isCovered,
        warrantyStatusLabel: warrantyInfo.statusLabel,
        warrantyRemainingKm: warrantyInfo.remainingKm,
        warrantyRemainingMonths: warrantyInfo.remainingMonths,
        warrantyExpiresAtUtc: warrantyInfo.warrantyExpiresAtUtc,
        warrantyEligibility: warrantyInfo
      };
    };

    self.normalizeReminder = function (raw) {
      var r = raw || {};
      return {
        id: String(pickDefined(r, ['id', 'reminderId', 'Id'], '')),
        customerVehicleId: String(pickDefined(r, ['customerVehicleId', 'vehicleId', 'CustomerVehicleId'], '')),
        maintenanceRuleId: String(pickDefined(r, ['maintenanceRuleId', 'ruleId', 'MaintenanceRuleId'], '')),
        milestoneCode: String(pickDefined(r, ['milestoneCode', 'MilestoneCode'], '')),
        milestoneName: String(pickDefined(r, ['milestoneName', 'MilestoneName'], 'Periodic Maintenance')),
        targetMileageKm: Number(pickDefined(r, ['targetMileageKm', 'TargetMileageKm'], 0)) || 0,
        dueDateUtc: pickDefined(r, ['dueDateUtc', 'DueDateUtc'], null),
        triggerReason: String(pickDefined(r, ['triggerReason', 'TriggerReason'], 'MileageThreshold')),
        deduplicationKey: String(pickDefined(r, ['deduplicationKey', 'DeduplicationKey'], '')),
        status: String(pickDefined(r, ['status', 'Status'], 'Sent')),
        resolvedBookingId: pickDefined(r, ['resolvedBookingId', 'ResolvedBookingId'], null),
        sentAtUtc: pickDefined(r, ['sentAtUtc', 'SentAtUtc', 'createdAtUtc'], null)
      };
    };

    self.normalizeTimeSlot = function (raw) {
      var s = raw || {};
      var resolvedId = String(pickDefined(s, ['slotId', 'id', 'serviceTimeSlotId', 'Id'], ''));
      var maxCap = Number(pickDefined(s, ['maxCapacity', 'MaxCapacity'], 4)) || 4;
      var booked = Number(pickDefined(s, ['bookedCount', 'BookedCount'], 0)) || 0;
      var blocked = Boolean(pickDefined(s, ['isBlocked', 'IsBlocked'], false));
      var rawRem = pickDefined(s, ['remainingCapacity', 'availableBays', 'RemainingCapacity', 'AvailableBays'], null);
      var remaining = blocked
        ? 0
        : (rawRem !== null ? Math.max(0, Number(rawRem) || 0) : Math.max(0, maxCap - booked));
      var startHm = formatShortTime(pickDefined(s, ['startTime', 'StartTime'], '08:00'), '08:00');
      var endHm = formatShortTime(pickDefined(s, ['endTime', 'EndTime'], '09:00'), '09:00');

      return {
        id: resolvedId,
        slotId: resolvedId,
        dealerId: String(pickDefined(s, ['dealerId', 'DealerId'], '')),
        dealerName: String(pickDefined(s, ['dealerName', 'DealerName'], '')),
        slotDate: formatDateOnly(pickDefined(s, ['slotDate', 'SlotDate'], '')),
        startTime: startHm,
        endTime: endHm,
        window: startHm + ' - ' + endHm,
        maxCapacity: maxCap,
        bookedCount: booked,
        remainingCapacity: remaining,
        availableBays: remaining,
        isBlocked: blocked,
        isBookable: !blocked && remaining > 0,
        rowVersion: String(pickDefined(s, ['rowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeBooking = function (raw) {
      var b = raw || {};
      var resolvedId = String(pickDefined(b, ['bookingId', 'id', 'Id'], ''));
      var startHm = formatShortTime(pickDefined(b, ['startTime', 'StartTime'], '09:00'), '09:00');
      var endHm = formatShortTime(pickDefined(b, ['endTime', 'EndTime'], '10:00'), '10:00');
      var voucherId = pickDefined(b, ['appliedCustomerVoucherId', 'AppliedCustomerVoucherId'], null);
      var voucherCode = pickDefined(b, ['reservedVoucherCode', 'voucherCode', 'ReservedVoucherCode'], null);

      return {
        id: resolvedId,
        bookingId: resolvedId,
        bookingNumber: String(pickDefined(b, ['bookingNumber', 'BookingNumber'], '')),
        status: String(pickDefined(b, ['status', 'newStatus', 'Status'], 'Pending')),
        dealerId: String(pickDefined(b, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(b, ['dealerCode', 'DealerCode'], '')),
        dealerName: String(pickDefined(b, ['dealerName', 'DealerName'], '')),
        customerId: String(pickDefined(b, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(b, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerPhone: String(pickDefined(b, ['customerPhone', 'phoneNumber', 'CustomerPhone'], '')),
        customerVehicleId: String(pickDefined(b, ['customerVehicleId', 'vehicleId', 'CustomerVehicleId'], '')),
        vin: String(pickDefined(b, ['vin', 'VIN'], '')).toUpperCase(),
        licensePlate: pickDefined(b, ['licensePlate', 'LicensePlate'], null),
        motorcycleModelId: String(pickDefined(b, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        motorcycleModelName: String(pickDefined(b, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        imageUrl: String(pickDefined(b, ['imageUrl', 'ImageUrl', 'motorcycleModelImageUrl', 'modelImageUrl', 'MotorcycleModelImageUrl', 'ModelImageUrl'], '') || '').trim(),
        serviceTimeSlotId: String(pickDefined(b, ['serviceTimeSlotId', 'slotId', 'ServiceTimeSlotId'], '')),
        slotDate: formatDateOnly(pickDefined(b, ['slotDate', 'SlotDate'], '')),
        startTime: startHm,
        endTime: endHm,
        slotWindow: startHm + ' - ' + endHm,
        serviceType: String(pickDefined(b, ['serviceType', 'ServiceType'], 'PeriodicMaintenance')),
        odometerAtBookingKm: Number(pickDefined(b, ['odometerAtBookingKm', 'OdometerAtBookingKm'], 0)) || 0,
        appliedCustomerVoucherId: voucherId,
        reservedVoucherCode: voucherCode,
        hasReservedVoucher: Boolean(pickDefined(b, ['hasReservedVoucher'], Boolean(voucherCode || voucherId))),
        reportedNotes: String(pickDefined(b, ['reportedNotes', 'ReportedNotes'], '')),
        cancellationReason: pickDefined(b, ['cancellationReason', 'CancellationReason'], null),
        checkedInAtUtc: pickDefined(b, ['checkedInAtUtc', 'CheckedInAtUtc'], null),
        completedAtUtc: pickDefined(b, ['completedAtUtc', 'CompletedAtUtc'], null),
        cancelledAtUtc: pickDefined(b, ['cancelledAtUtc', 'CancelledAtUtc'], null),
        createdAtUtc: pickDefined(b, ['createdAtUtc', 'occurredAtUtc', 'CreatedAtUtc'], null),
        rowVersion: String(pickDefined(b, ['rowVersion', 'RowVersion'], '')),
        isLiveArrival: Boolean(b.isLiveArrival)
      };
    };

    // --- 1. Dealers ---
    self.listDealers = function (params) {
      return $http.get(baseUrl() + '/dealers', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeDealer);
      });
    };

    // --- 2. Motorcycles & OEM MaintenanceRules ---
    self.listMotorcycles = function (params) {
      return $http.get(baseUrl() + '/motorcycles', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeMotorcycleModel);
      });
    };

    self.getMotorcycleById = function (modelId) {
      return $http.get(baseUrl() + '/motorcycles/' + encodeURIComponent(modelId))
        .then(function (res) {
          return self.normalizeMotorcycleModel(self.unwrapItem(res));
        });
    };

    self.createMotorcycle = function (payload) {
      return $http.post(baseUrl() + '/motorcycles', payload)
        .then(function (res) {
          return self.normalizeMotorcycleModel(self.unwrapItem(res));
        });
    };

    self.updateMotorcycle = function (modelId, payload) {
      return $http.put(baseUrl() + '/motorcycles/' + encodeURIComponent(modelId), payload)
        .then(function (res) {
          return self.normalizeMotorcycleModel(self.unwrapItem(res));
        });
    };

    self.listMaintenanceRules = function (modelId, params) {
      return $http.get(baseUrl() + '/motorcycles/' + encodeURIComponent(modelId) + '/maintenance-rules', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res)
          .map(self.normalizeMaintenanceRule)
          .sort(function (a, b) { return a.intervalKm - b.intervalKm; });
      });
    };

    self.createMaintenanceRule = function (modelId, payload) {
      return $http.post(
        baseUrl() + '/motorcycles/' + encodeURIComponent(modelId) + '/maintenance-rules',
        payload
      ).then(function (res) {
        return self.normalizeMaintenanceRule(self.unwrapItem(res));
      });
    };

    self.updateMaintenanceRule = function (modelId, ruleId, payload) {
      var body = angular.extend({}, payload || {}, { id: ruleId, ruleId: ruleId });
      var primaryUrl = baseUrl() + '/motorcycles/' + encodeURIComponent(modelId) + '/maintenance-rules/' + encodeURIComponent(ruleId);
      return $http.put(primaryUrl, body)
        .catch(function (err) {
          if (err && (err.status === 404 || err.status === 405)) {
            return $http.put(
              baseUrl() + '/motorcycles/' + encodeURIComponent(modelId) + '/maintenance-rules',
              body
            );
          }
          return $q.reject(err);
        })
        .then(function (res) {
          return self.normalizeMaintenanceRule(self.unwrapItem(res));
        });
    };

    // --- 3. Dealer VIN Inventories ---
    self.listInventories = function (params) {
      var cleaned = self.cleanParams(params);
      if (cleaned.status && !cleaned.stockStatus) {
        cleaned.stockStatus = cleaned.status;
      }
      return $http.get(baseUrl() + '/inventories', {
        params: cleaned
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeInventory);
      });
    };

    self.getInventories = self.listInventories;

    self.createInventory = function (payload) {
      return $http.post(baseUrl() + '/inventories', payload)
        .then(function (res) {
          return self.normalizeInventory(self.unwrapItem(res));
        });
    };

    self.updateInventory = function (inventoryId, payload) {
      return $http.patch(baseUrl() + '/inventories/' + encodeURIComponent(inventoryId), payload)
        .then(function (res) {
          return self.normalizeInventory(self.unwrapItem(res));
        });
    };

    // --- 4. Showroom SalesOrders ---
    self.listSalesOrders = function (params) {
      return $http.get(baseUrl() + '/sales-orders', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeSalesOrder);
      });
    };

    self.listMySalesOrders = function (params) {
      return $http.get(baseUrl() + '/sales-orders/my', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeSalesOrder);
      });
    };

    self.getMySalesOrders = self.listMySalesOrders;

    self.getSalesOrderById = function (orderId) {
      return $http.get(baseUrl() + '/sales-orders/' + encodeURIComponent(orderId))
        .then(function (res) {
          return self.normalizeSalesOrder(self.unwrapItem(res));
        });
    };

    self.createSalesOrder = function (payload, idempotencyKey) {
      var p = payload || {};
      var key = idempotencyKey || self.generateIdempotencyKey('sales-order');
      var rowVer = p.inventoryRowVersion || p.rowVersion || null;
      var body = {
        customerId: p.customerId ? String(p.customerId).trim() : null,
        customerEmail: p.customerEmail ? String(p.customerEmail).trim() : null,
        dealerInventoryId: p.dealerInventoryId,
        inventoryRowVersion: rowVer,
        rowVersion: rowVer,
        appliedCustomerVoucherId: p.appliedCustomerVoucherId ? String(p.appliedCustomerVoucherId).trim() : null,
        voucherCode: p.voucherCode ? String(p.voucherCode).trim() : null,
        licensePlate: p.licensePlate ? String(p.licensePlate).trim() : null,
        discountAmount: Math.max(0, Number(p.discountAmount) || 0),
        paymentMethod: p.paymentMethod || 'BankTransfer',
        notes: p.notes ? String(p.notes).trim() : null
      };
      if (p.dealerId) {
        body.dealerId = p.dealerId;
      }
      return $http.post(baseUrl() + '/sales-orders', body, {
        headers: { 'Idempotency-Key': key }
      }).then(function (res) {
        return self.normalizeSalesOrder(self.unwrapItem(res));
      });
    };

    // --- 5. Customer VIN Garage & Vehicles ---
    self.listVehicles = function (params) {
      return $http.get(baseUrl() + '/vehicles', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeVehicle);
      });
    };

    self.lookupVehicles = function (params) {
      return $http.get(baseUrl() + '/vehicles/lookup', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeVehicle);
      });
    };

    self.getVehicleById = function (vehicleId) {
      return $http.get(baseUrl() + '/vehicles/' + encodeURIComponent(vehicleId))
        .then(function (res) {
          var item = self.unwrapItem(res);
          var rawVehicle = item && item.vehicle ? item.vehicle : item;
          return self.normalizeVehicle(rawVehicle);
        });
    };

    self.registerVehicle = function (payload) {
      var p = payload || {};
      var body = {
        vin: String(p.vin || '').trim().toUpperCase(),
        motorcycleModelId: p.motorcycleModelId,
        currentMileageKm: Math.max(0, Math.floor(Number(p.currentMileageKm) || 0)),
        licensePlate: p.licensePlate ? String(p.licensePlate).trim() : null,
        colorName: p.colorName ? String(p.colorName).trim() : null
      };
      return $http.post(baseUrl() + '/vehicles', body)
        .then(function (res) {
          return self.normalizeVehicle(self.unwrapItem(res));
        });
    };

    self.updateVehicleMileage = function (vehicleId, mileageKm) {
      var km = Math.floor(Number(mileageKm));
      return $http.patch(baseUrl() + '/vehicles/' + encodeURIComponent(vehicleId) + '/mileage', {
        currentMileageKm: km,
        newMileageKm: km
      }).then(function (res) {
        return self.normalizeVehicle(self.unwrapItem(res));
      });
    };

    self.getVehicleReminders = function (vehicleId) {
      return $http.get(baseUrl() + '/vehicles/' + encodeURIComponent(vehicleId) + '/reminders')
        .then(function (res) {
          var data = self.unwrapItem(res);
          if (angular.isArray(data)) {
            return {
              vehicle: null,
              rules: [],
              reminders: data.map(self.normalizeReminder)
            };
          }
          var obj = data || {};
          var rawReminders = pickDefined(obj, ['reminders', 'items', 'Reminders'], []);
          var rawRules = pickDefined(obj, ['maintenanceRules', 'rules', 'MaintenanceRules'], []);
          return {
            vehicle: obj.vehicle ? self.normalizeVehicle(obj.vehicle) : null,
            rules: angular.isArray(rawRules) ? rawRules.map(self.normalizeMaintenanceRule) : [],
            reminders: angular.isArray(rawReminders) ? rawReminders.map(self.normalizeReminder) : []
          };
        });
    };

    // --- 6. Workshop ServiceTimeSlots ---
    self.listTimeSlots = function (params) {
      return $http.get(baseUrl() + '/time-slots', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeTimeSlot);
      });
    };

    self.listDealerTimeSlots = function (params) {
      return $http.get(baseUrl() + '/time-slots/dealer', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeTimeSlot);
      });
    };

    self.createTimeSlot = function (payload) {
      return $http.post(baseUrl() + '/time-slots', payload)
        .then(function (res) {
          return self.normalizeTimeSlot(self.unwrapItem(res));
        });
    };

    self.generateTimeSlots = function (payload) {
      var p = payload || {};
      var cap = Math.max(1, Math.floor(Number(p.maxCapacity || p.maxCapacityPerSlot) || 4));
      var body = {
        fromDate: p.fromDate,
        toDate: p.toDate,
        slotDurationMinutes: Number(p.slotDurationMinutes) || 60,
        maxCapacity: cap,
        maxCapacityPerSlot: cap,
        excludeLunchHour: p.excludeLunchHour !== false
      };
      if (p.dealerId) {
        body.dealerId = p.dealerId;
      }
      return $http.post(baseUrl() + '/time-slots/generate', body)
        .then(function (res) {
          var unwrapped = self.unwrapItem(res);
          if (angular.isArray(unwrapped)) {
            return { slots: unwrapped.map(self.normalizeTimeSlot), createdCount: unwrapped.length };
          }
          var slotsArr = pickDefined(unwrapped, ['slots', 'items', 'generatedSlots'], []);
          return {
            slots: angular.isArray(slotsArr) ? slotsArr.map(self.normalizeTimeSlot) : [],
            createdCount: Number(pickDefined(unwrapped, ['createdCount', 'generatedCount', 'count'], slotsArr.length)) || 0,
            skippedCount: Number(pickDefined(unwrapped, ['skippedCount'], 0)) || 0
          };
        });
    };

    self.updateTimeSlot = function (slotId, payload) {
      var p = payload || {};
      var body = {
        maxCapacity: Math.max(1, Math.floor(Number(p.maxCapacity) || 1)),
        isBlocked: Boolean(p.isBlocked),
        rowVersion: p.rowVersion || null
      };
      return $http.patch(baseUrl() + '/time-slots/' + encodeURIComponent(slotId), body)
        .then(function (res) {
          return self.normalizeTimeSlot(self.unwrapItem(res));
        });
    };

    // --- 7. Workshop ServiceBookings ---
    self.listBookings = function (params) {
      return $http.get(baseUrl() + '/bookings', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeBooking);
      });
    };

    self.getBookingById = function (bookingId) {
      return $http.get(baseUrl() + '/bookings/' + encodeURIComponent(bookingId))
        .then(function (res) {
          return self.normalizeBooking(self.unwrapItem(res));
        });
    };

    self.createBooking = function (payload, idempotencyKey) {
      var p = payload || {};
      var key = idempotencyKey || self.generateIdempotencyKey('booking');
      var body = {
        dealerId: p.dealerId,
        serviceTimeSlotId: p.serviceTimeSlotId,
        slotRowVersion: p.slotRowVersion || null,
        customerVehicleId: p.customerVehicleId,
        serviceType: p.serviceType || 'PeriodicMaintenance',
        odometerAtBookingKm: Math.max(0, Math.floor(Number(p.odometerAtBookingKm) || 0)),
        reportedNotes: p.reportedNotes ? String(p.reportedNotes).trim() : null,
        appliedCustomerVoucherId: p.appliedCustomerVoucherId ? String(p.appliedCustomerVoucherId).trim() : null,
        voucherCode: p.voucherCode ? String(p.voucherCode).trim() : null,
        voucherRowVersion: p.voucherRowVersion || null
      };
      return $http.post(baseUrl() + '/bookings', body, {
        headers: { 'Idempotency-Key': key }
      }).then(function (res) {
        return self.normalizeBooking(self.unwrapItem(res));
      });
    };

    self.cancelBooking = function (bookingId, payload) {
      var p = payload || {};
      var reason = String(p.cancellationReason || p.reason || 'Cancelled by customer').trim();
      return $http.post(baseUrl() + '/bookings/' + encodeURIComponent(bookingId) + '/cancel', {
        cancellationReason: reason,
        reason: reason,
        rowVersion: p.rowVersion || null
      }).then(function (res) {
        return self.normalizeBooking(self.unwrapItem(res));
      });
    };

    self.updateBookingStatus = function (bookingId, payload) {
      var p = payload || {};
      var nextStatus = String(p.newStatus || p.status || '').trim();
      var reasonOrNotes = p.cancellationReason || p.notes || null;
      return $http.patch(baseUrl() + '/bookings/' + encodeURIComponent(bookingId) + '/status', {
        newStatus: nextStatus,
        status: nextStatus,
        notes: p.notes ? String(p.notes).trim() : reasonOrNotes,
        cancellationReason: p.cancellationReason ? String(p.cancellationReason).trim() : null,
        rowVersion: p.rowVersion || null
      }).then(function (res) {
        return self.normalizeBooking(self.unwrapItem(res));
      });
    };

    // --- Phase 6 Normalizers (ServiceInvoices, Loyalty, Vouchers, Notifications) ---

    function formatVoucherDiscountSummary(discountType, discountValue, maxDiscountAmount) {
      var isVi = isVietnameseActive();
      var dtype = String(discountType || 'FixedAmount');
      var dval = Math.max(0, Number(discountValue) || 0);
      if (dtype === 'Percentage') {
        var capNum = maxDiscountAmount !== null && maxDiscountAmount !== undefined ? Number(maxDiscountAmount) : 0;
        if (isVi) {
          var viCapLabel = (!isNaN(capNum) && capNum > 0)
            ? (' (Tối đa ' + Math.round(capNum).toLocaleString('vi-VN') + ' ₫)')
            : '';
          return 'Giảm ' + dval + '%' + viCapLabel;
        }
        var capLabel = (!isNaN(capNum) && capNum > 0)
          ? (' (Cap ' + Math.round(capNum).toLocaleString('vi-VN') + ' ₫)')
          : '';
        return dval + '% Off' + capLabel;
      }
      if (isVi) {
        return 'Giảm ' + Math.round(dval).toLocaleString('vi-VN') + ' ₫';
      }
      return Math.round(dval).toLocaleString('vi-VN') + ' ₫ Off';
    }

    self.normalizeInvoiceLineItem = function (raw, indexFallback) {
      var li = raw || {};
      var qty = Math.max(1, Math.floor(Number(pickDefined(li, ['quantity', 'Quantity'], 1)) || 1));
      var unitPrice = Math.max(0, Number(pickDefined(li, ['unitPriceVnd', 'unitPrice', 'UnitPriceVnd', 'UnitPrice'], 0)) || 0);
      var lineTotal = Math.max(
        0,
        Number(pickDefined(li, ['lineTotalVnd', 'lineTotal', 'LineTotalVnd', 'LineTotal'], qty * unitPrice)) || 0
      );
      var partCode = String(pickDefined(li, ['partCodeSnapshot', 'partCode', 'PartCodeSnapshot', 'PartCode'], '')).toUpperCase();
      return {
        id: String(pickDefined(li, ['id', 'lineItemId', 'Id'], 'li-' + (indexFallback || 1))),
        lineNumber: Math.max(1, Math.floor(Number(pickDefined(li, ['lineNumber', 'LineNumber'], indexFallback || 1)) || 1)),
        sparePartId: pickDefined(li, ['sparePartId', 'SparePartId'], null),
        partCodeSnapshot: partCode,
        partCode: partCode,
        description: String(pickDefined(li, ['description', 'name', 'Description'], partCode || 'Workshop Item')),
        itemType: String(pickDefined(li, ['itemType', 'ItemType'], partCode ? 'Part' : 'Labor')),
        quantity: qty,
        unitPriceVnd: unitPrice,
        lineTotalVnd: lineTotal
      };
    };

    self.normalizeServiceInvoice = function (raw) {
      var inv = raw || {};
      var resolvedId = String(pickDefined(inv, ['invoiceId', 'serviceInvoiceId', 'id', 'InvoiceId', 'Id'], ''));
      var resolvedBookingId = String(pickDefined(inv, ['serviceBookingId', 'bookingId', 'ServiceBookingId', 'BookingId'], ''));
      var appliedTier = String(pickDefined(inv, ['appliedTierCode', 'previousTierCode', 'AppliedTierCode'], 'Bronze'));
      var afterTier = String(
        pickDefined(
          inv,
          ['newTierCode', 'currentTierAfter', 'currentTierCode', 'NewTierCode', 'CurrentTierAfter'],
          appliedTier
        )
      );
      var rawLineItemsArr = pickDefined(inv, ['lineItems', 'LineItems'], null);
      var rawLineItems = pickDefined(inv, ['lineItemsJson', 'LineItemsJson'], null);
      var parsedLineItems = [];
      if (angular.isArray(rawLineItemsArr) && rawLineItemsArr.length > 0) {
        parsedLineItems = rawLineItemsArr.map(function (li, idx) {
          return self.normalizeInvoiceLineItem(li, idx + 1);
        });
      } else if (angular.isString(rawLineItems) && rawLineItems.trim()) {
        try {
          var arr = JSON.parse(rawLineItems);
          if (angular.isArray(arr)) {
            parsedLineItems = arr.map(function (li, idx) {
              return self.normalizeInvoiceLineItem(li, idx + 1);
            });
          }
        } catch (e) {
          parsedLineItems = [];
        }
      }

      var labor = Number(pickDefined(inv, ['laborAmount', 'LaborAmount'], 0)) || 0;
      var parts = Number(pickDefined(inv, ['partsAmount', 'PartsAmount'], 0)) || 0;
      var gross = Number(pickDefined(inv, ['grossAmount', 'GrossAmount'], labor + parts)) || 0;
      var voucherDisc = Number(pickDefined(inv, ['voucherDiscountAmount', 'discountAmount', 'VoucherDiscountAmount'], 0)) || 0;
      var netPaid = Number(pickDefined(inv, ['netPaidAmount', 'NetPaidAmount'], Math.max(0, gross - voucherDisc))) || 0;

      return {
        id: resolvedId,
        invoiceId: resolvedId,
        invoiceNumber: String(pickDefined(inv, ['invoiceNumber', 'InvoiceNumber'], '')),
        serviceBookingId: resolvedBookingId,
        bookingId: resolvedBookingId,
        bookingNumber: String(pickDefined(inv, ['bookingNumber', 'BookingNumber'], '')),
        dealerId: String(pickDefined(inv, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(inv, ['dealerCode', 'DealerCode'], '')),
        dealerName: String(pickDefined(inv, ['dealerName', 'DealerName'], '')),
        customerId: String(pickDefined(inv, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(inv, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(inv, ['customerEmail', 'CustomerEmail'], '')),
        vin: String(pickDefined(inv, ['vin', 'VIN'], '')).toUpperCase(),
        motorcycleModelName: String(pickDefined(inv, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        appliedCustomerVoucherId: pickDefined(inv, ['appliedCustomerVoucherId', 'AppliedCustomerVoucherId'], null),
        voucherCode: pickDefined(inv, ['voucherCode', 'appliedVoucherCode', 'VoucherCode'], null),
        laborAmount: labor,
        partsAmount: parts,
        grossAmount: gross,
        voucherDiscountAmount: voucherDisc,
        netPaidAmount: netPaid,
        odometerAtCompletionKm: Number(pickDefined(inv, ['odometerAtCompletionKm', 'OdometerAtCompletionKm'], 0)) || 0,
        appliedTierCode: appliedTier,
        appliedMultiplier: Number(pickDefined(inv, ['appliedMultiplier', 'pointsMultiplier', 'AppliedMultiplier'], 1.0)) || 1.0,
        pointsEarned: Number(pickDefined(inv, ['pointsEarned', 'PointsEarned'], 0)) || 0,
        availablePointsAfter: pickDefined(inv, ['availablePointsAfter', 'AvailablePointsAfter'], null),
        lifetimeQualifyingPointsAfter: pickDefined(inv, ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'], null),
        currentTierAfter: afterTier,
        newTierCode: afterTier,
        tierUpgraded: Boolean(pickDefined(inv, ['tierUpgraded', 'TierUpgraded'], afterTier !== appliedTier)),
        paymentMethod: String(pickDefined(inv, ['paymentMethod', 'PaymentMethod'], 'BankTransfer')),
        workSummaryNotes: pickDefined(inv, ['workSummaryNotes', 'WorkSummaryNotes'], null),
        lineItemsJson: angular.isString(rawLineItems) ? rawLineItems : (parsedLineItems.length ? JSON.stringify(parsedLineItems) : null),
        lineItems: parsedLineItems,
        reviewId: pickDefined(inv, ['reviewId', 'serviceReviewId', 'ReviewId', 'ServiceReviewId'], null),
        reviewNumber: pickDefined(inv, ['reviewNumber', 'ReviewNumber'], null),
        reviewRating: pickDefined(inv, ['reviewRating', 'rating', 'ReviewRating', 'Rating'], null),
        reviewStaffResponse: pickDefined(inv, ['reviewStaffResponse', 'staffResponse', 'ReviewStaffResponse', 'StaffResponse'], null),
        hasReview: Boolean(
          pickDefined(
            inv,
            ['hasReview', 'isReviewed', 'HasReview', 'IsReviewed'],
            Boolean(pickDefined(inv, ['reviewId', 'serviceReviewId', 'reviewRating', 'rating', 'ReviewId', 'ReviewRating', 'Rating'], null))
          )
        ),
        paidAtUtc: pickDefined(inv, ['paidAtUtc', 'PaidAtUtc', 'createdAtUtc', 'CreatedAtUtc'], null),
        createdAtUtc: pickDefined(inv, ['createdAtUtc', 'CreatedAtUtc', 'paidAtUtc', 'PaidAtUtc'], null)
      };
    };

    self.normalizeLoyaltyTier = function (raw) {
      var t = raw || {};
      var code = String(pickDefined(t, ['tierCode', 'code', 'TierCode'], 'Bronze'));
      var resolvedId = String(pickDefined(t, ['tierId', 'id', 'TierId', 'Id'], code));
      var mult = Number(pickDefined(t, ['pointsMultiplier', 'multiplier', 'PointsMultiplier'], 1.0)) || 1.0;
      var rawBenefits = pickDefined(t, ['benefitsJson', 'BenefitsJson'], null);
      var parsedBenefits = [];
      if (angular.isString(rawBenefits) && rawBenefits.trim()) {
        try {
          var arr = JSON.parse(rawBenefits);
          if (angular.isArray(arr)) {
            parsedBenefits = arr.map(function (b) { return String(b); });
          }
        } catch (e) {
          parsedBenefits = [];
        }
      } else if (angular.isArray(t.benefits)) {
        parsedBenefits = t.benefits.map(function (b) { return String(b); });
      }

      var defaultRanks = { bronze: 1, silver: 2, gold: 3, platinum: 4 };
      var rank = Number(pickDefined(t, ['rankOrder', 'RankOrder'], defaultRanks[code.toLowerCase()] || 1)) || 1;

      return {
        id: resolvedId,
        tierId: resolvedId,
        code: code,
        tierCode: code,
        displayName: String(pickDefined(t, ['displayName', 'DisplayName'], code + ' Member')),
        rankOrder: rank,
        minQualifyingPoints: Math.max(0, Math.floor(Number(pickDefined(t, ['minQualifyingPoints', 'MinQualifyingPoints'], 0)) || 0)),
        multiplier: mult,
        pointsMultiplier: mult,
        benefitsJson: angular.isString(rawBenefits) ? rawBenefits : JSON.stringify(parsedBenefits),
        benefits: parsedBenefits,
        isActive: pickDefined(t, ['isActive', 'IsActive'], true) !== false,
        rowVersion: String(pickDefined(t, ['rowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeLoyaltyAccount = function (raw) {
      var a = raw || {};
      var resolvedId = String(pickDefined(a, ['accountId', 'loyaltyAccountId', 'id', 'AccountId', 'LoyaltyAccountId', 'Id'], ''));
      var tierCode = String(pickDefined(a, ['currentTierCode', 'tierCode', 'CurrentTierCode', 'TierCode'], 'Bronze'));
      var mult = Number(pickDefined(a, ['pointsMultiplier', 'tierMultiplier', 'PointsMultiplier', 'TierMultiplier'], 1.0)) || 1.0;

      return {
        id: resolvedId,
        accountId: resolvedId,
        loyaltyAccountId: resolvedId,
        customerId: String(pickDefined(a, ['customerId', 'userId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(a, ['customerFullName', 'fullName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(a, ['customerEmail', 'email', 'CustomerEmail'], '')),
        currentTierId: String(pickDefined(a, ['currentTierId', 'tierId', 'CurrentTierId'], '')),
        currentTierCode: tierCode,
        tierCode: tierCode,
        pointsMultiplier: mult,
        tierMultiplier: mult,
        availablePoints: Math.max(0, Math.floor(Number(pickDefined(a, ['availablePoints', 'AvailablePoints'], 0)) || 0)),
        lifetimeQualifyingPoints: Math.max(0, Math.floor(Number(pickDefined(a, ['lifetimeQualifyingPoints', 'LifetimeQualifyingPoints'], 0)) || 0)),
        lifetimeSpentVnd: Math.max(0, Number(pickDefined(a, ['lifetimeSpentVnd', 'LifetimeSpentVnd'], 0)) || 0),
        nextTierCode: pickDefined(a, ['nextTierCode', 'NextTierCode'], null),
        pointsToNextTier: Math.max(0, Math.floor(Number(pickDefined(a, ['pointsToNextTier', 'PointsToNextTier'], 0)) || 0)),
        progressPercent: pickDefined(a, ['progressPercent', 'ProgressPercent'], null),
        availableVouchers: Number(pickDefined(a, ['availableVouchers', 'availableVouchersCount', 'AvailableVouchers'], 0)) || 0,
        reservedVouchers: Number(pickDefined(a, ['reservedVouchers', 'reservedVouchersCount', 'ReservedVouchers'], 0)) || 0,
        usedVouchers: Number(pickDefined(a, ['usedVouchers', 'usedVouchersCount', 'UsedVouchers'], 0)) || 0,
        tierUpgradedAtUtc: pickDefined(a, ['tierUpgradedAtUtc', 'TierUpgradedAtUtc'], null),
        rowVersion: String(pickDefined(a, ['rowVersion', 'accountRowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeLedgerEntry = function (raw) {
      var e = raw || {};
      var resolvedId = String(pickDefined(e, ['id', 'ledgerEntryId', 'entryId', 'Id', 'LedgerEntryId'], ''));
      var afterBal = Math.max(0, Math.floor(Number(pickDefined(e, ['balanceAfter', 'availablePointsAfter', 'BalanceAfter', 'AvailablePointsAfter'], 0)) || 0));
      return {
        id: resolvedId,
        ledgerEntryId: resolvedId,
        loyaltyAccountId: String(pickDefined(e, ['loyaltyAccountId', 'accountId', 'LoyaltyAccountId'], '')),
        customerId: pickDefined(e, ['customerId', 'CustomerId'], null),
        transactionType: String(pickDefined(e, ['transactionType', 'TransactionType'], 'ServiceInvoiceEarn')),
        pointsDelta: Math.trunc(Number(pickDefined(e, ['pointsDelta', 'PointsDelta'], 0)) || 0),
        balanceAfter: afterBal,
        availablePointsAfter: afterBal,
        lifetimeQualifyingPointsAfter: pickDefined(e, ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'], null),
        previousTierCode: pickDefined(e, ['previousTierCode', 'PreviousTierCode'], null),
        currentTierCode: pickDefined(e, ['currentTierCode', 'newTierCode', 'CurrentTierCode', 'NewTierCode'], null),
        newTierCode: pickDefined(e, ['newTierCode', 'currentTierCode', 'NewTierCode', 'CurrentTierCode'], null),
        pointsMultiplier: pickDefined(e, ['pointsMultiplier', 'PointsMultiplier'], null),
        tierUpgraded: Boolean(pickDefined(e, ['tierUpgraded', 'TierUpgraded'], false)),
        referenceType: String(pickDefined(e, ['referenceType', 'ReferenceType'], '')),
        referenceId: pickDefined(e, ['referenceId', 'ReferenceId'], null),
        referenceNumber: pickDefined(e, ['referenceNumber', 'ReferenceNumber'], null),
        idempotencyKey: String(pickDefined(e, ['idempotencyKey', 'IdempotencyKey'], '')),
        notes: pickDefined(e, ['notes', 'reasonNotes', 'Notes'], null),
        createdByUserId: pickDefined(e, ['createdByUserId', 'CreatedByUserId'], null),
        createdAtUtc: pickDefined(e, ['createdAtUtc', 'CreatedAtUtc'], null),
        rowVersion: String(pickDefined(e, ['rowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeVoucherCatalog = function (raw) {
      var v = raw || {};
      var prefix = String(pickDefined(v, ['codePrefix', 'code', 'CodePrefix'], '')).toUpperCase();
      var resolvedId = String(pickDefined(v, ['id', 'catalogId', 'voucherCatalogId', 'Id', 'VoucherCatalogId'], prefix));
      var dtype = String(pickDefined(v, ['discountType', 'DiscountType'], 'FixedAmount'));
      var dval = Number(pickDefined(v, ['discountValue', 'DiscountValue'], 0)) || 0;
      var rawMax = pickDefined(v, ['maxDiscountAmount', 'MaxDiscountAmount'], null);
      var maxDisc = rawMax !== null && rawMax !== undefined ? Number(rawMax) : null;
      var rawTotal = pickDefined(v, ['quotaTotal', 'totalQuota', 'QuotaTotal'], null);
      var quotaTotal = rawTotal !== null && rawTotal !== undefined ? Math.floor(Number(rawTotal)) : null;
      var quotaRedeemed = Math.max(0, Math.floor(Number(pickDefined(v, ['quotaRedeemed', 'QuotaRedeemed'], 0)) || 0));
      var rawRem = pickDefined(v, ['remainingQuota', 'RemainingQuota'], null);
      var remainingQuota = quotaTotal !== null
        ? (rawRem !== null && rawRem !== undefined ? Math.max(0, Math.floor(Number(rawRem))) : Math.max(0, quotaTotal - quotaRedeemed))
        : null;
      var minTierCode = String(pickDefined(v, ['minTierCode', 'minTier', 'MinTierCode'], 'Bronze'));
      var minOrder = Math.max(0, Number(pickDefined(v, ['minOrderAmount', 'minOrderVnd', 'MinOrderAmount'], 0)) || 0);

      return {
        id: resolvedId,
        catalogId: resolvedId,
        voucherCatalogId: resolvedId,
        code: prefix,
        codePrefix: prefix,
        title: String(pickDefined(v, ['title', 'Title'], '')),
        description: String(pickDefined(v, ['description', 'Description'], '')),
        applicableScope: String(pickDefined(v, ['applicableScope', 'ApplicableScope'], 'ServiceOnly')),
        pointsCost: Math.max(1, Math.floor(Number(pickDefined(v, ['pointsCost', 'PointsCost'], 100)) || 100)),
        discountType: dtype,
        discountValue: dval,
        maxDiscountAmount: maxDisc,
        discountSummary: pickDefined(v, ['discountSummary'], null) || formatVoucherDiscountSummary(dtype, dval, maxDisc),
        minOrderAmount: minOrder,
        minOrderVnd: minOrder,
        validDays: Math.max(1, Math.floor(Number(pickDefined(v, ['validDays', 'ValidDays'], 90)) || 90)),
        quotaTotal: quotaTotal,
        quotaRedeemed: quotaRedeemed,
        remainingQuota: remainingQuota,
        minTierId: String(pickDefined(v, ['minTierId', 'MinTierId'], '')),
        minTierCode: minTierCode,
        minTier: minTierCode,
        isEligibleForCaller: pickDefined(v, ['isEligibleForCaller', 'IsEligibleForCaller'], null),
        isActive: pickDefined(v, ['isActive', 'IsActive'], true) !== false
      };
    };

    self.normalizeCustomerVoucher = function (raw) {
      var cv = raw || {};
      var nestedCat = (cv.voucherCatalog && angular.isObject(cv.voucherCatalog))
        ? cv.voucherCatalog
        : ((cv.catalog && angular.isObject(cv.catalog)) ? cv.catalog : {});
      var resolvedId = String(pickDefined(cv, ['customerVoucherId', 'id', 'voucherId', 'CustomerVoucherId', 'Id'], ''));
      var code = String(pickDefined(cv, ['voucherCode', 'code', 'VoucherCode'], '')).toUpperCase();
      var dtype = String(pickDefined(cv, ['discountType', 'DiscountType'], pickDefined(nestedCat, ['discountType', 'DiscountType'], 'FixedAmount')));
      var dval = Number(pickDefined(cv, ['discountValue', 'DiscountValue'], pickDefined(nestedCat, ['discountValue', 'DiscountValue'], 0))) || 0;
      var rawMax = pickDefined(cv, ['maxDiscountAmount', 'MaxDiscountAmount'], pickDefined(nestedCat, ['maxDiscountAmount', 'MaxDiscountAmount'], null));
      var maxDisc = rawMax !== null && rawMax !== undefined ? Number(rawMax) : null;
      var minOrder = Math.max(
        0,
        Number(pickDefined(cv, ['minOrderAmount', 'MinOrderAmount'], pickDefined(nestedCat, ['minOrderAmount', 'MinOrderAmount'], 0))) || 0
      );
      var status = String(pickDefined(cv, ['status', 'Status'], 'Available'));
      var expiresAtUtc = pickDefined(cv, ['expiresAtUtc', 'ExpiresAtUtc'], null);
      var expMs = expiresAtUtc ? Date.parse(expiresAtUtc) : NaN;
      var rawEff = pickDefined(cv, ['effectiveStatus', 'EffectiveStatus'], null);
      var effectiveStatus = rawEff
        ? String(rawEff)
        : (status === 'Available' && !isNaN(expMs) && expMs <= Date.now() ? 'Expired' : status);

      return {
        id: resolvedId,
        customerVoucherId: resolvedId,
        voucherCatalogId: String(pickDefined(cv, ['voucherCatalogId', 'catalogId', 'VoucherCatalogId'], pickDefined(nestedCat, ['id', 'Id'], ''))),
        voucherCode: code,
        codePrefix: String(pickDefined(cv, ['codePrefix', 'CodePrefix'], pickDefined(nestedCat, ['codePrefix', 'CodePrefix'], code.split('-')[0] || ''))),
        title: String(pickDefined(cv, ['title', 'catalogTitle', 'Title'], pickDefined(nestedCat, ['title', 'Title'], code || 'Reward Voucher'))),
        description: String(pickDefined(cv, ['description', 'Description'], pickDefined(nestedCat, ['description', 'Description'], ''))),
        applicableScope: String(pickDefined(cv, ['applicableScope', 'ApplicableScope'], pickDefined(nestedCat, ['applicableScope', 'ApplicableScope'], 'ServiceOnly'))),
        discountType: dtype,
        discountValue: dval,
        maxDiscountAmount: maxDisc,
        discountSummary: formatVoucherDiscountSummary(dtype, dval, maxDisc),
        minOrderAmount: minOrder,
        pointsSpent: Math.max(0, Math.floor(Number(pickDefined(cv, ['pointsSpent', 'pointsCost', 'PointsSpent'], 0)) || 0)),
        status: status,
        effectiveStatus: effectiveStatus,
        redeemedAtUtc: pickDefined(cv, ['redeemedAtUtc', 'createdAtUtc', 'RedeemedAtUtc'], null),
        expiresAtUtc: expiresAtUtc,
        usedAtUtc: pickDefined(cv, ['usedAtUtc', 'UsedAtUtc'], null),
        availablePointsAfter: pickDefined(cv, ['availablePointsAfter', 'AvailablePointsAfter'], null),
        lifetimeQualifyingPoints: pickDefined(cv, ['lifetimeQualifyingPoints', 'LifetimeQualifyingPoints'], null),
        accountRowVersion: pickDefined(cv, ['accountRowVersion', 'AccountRowVersion'], null),
        rowVersion: String(pickDefined(cv, ['rowVersion', 'RowVersion'], ''))
      };
    };

    self.normalizeCustomerLoyaltySummary = function (raw) {
      var obj = raw || {};
      var accountSource = (obj.account && angular.isObject(obj.account)) ? obj.account : obj;
      var account = self.normalizeLoyaltyAccount(accountSource);
      var rawVouchers = pickDefined(
        obj,
        ['eligibleVouchers', 'availableServiceVouchers', 'availableVouchers', 'customerVouchers', 'vouchers', 'EligibleVouchers', 'AvailableServiceVouchers', 'AvailableVouchers'],
        []
      );
      var rawServiceVouchers = pickDefined(
        obj,
        ['availableServiceVouchers', 'AvailableServiceVouchers'],
        null
      );
      var rawLedger = pickDefined(
        obj,
        ['recentLedgerEntries', 'recentLedger', 'ledgerEntries', 'ledger', 'RecentLedgerEntries'],
        []
      );
      var vouchers = angular.isArray(rawVouchers) ? rawVouchers.map(self.normalizeCustomerVoucher) : [];
      var serviceVouchers = angular.isArray(rawServiceVouchers)
        ? rawServiceVouchers.map(self.normalizeCustomerVoucher)
        : vouchers.filter(function (v) { return v.applicableScope === 'ServiceOnly' || v.applicableScope === 'All'; });
      var ledger = angular.isArray(rawLedger) ? rawLedger.map(self.normalizeLedgerEntry) : [];

      return {
        account: account,
        accountId: account.accountId,
        loyaltyAccountId: account.accountId,
        customerId: account.customerId,
        customerFullName: account.customerFullName,
        customerEmail: account.customerEmail,
        currentTierCode: account.currentTierCode,
        tierCode: account.currentTierCode,
        pointsMultiplier: account.pointsMultiplier,
        tierMultiplier: account.pointsMultiplier,
        availablePoints: account.availablePoints,
        lifetimeQualifyingPoints: account.lifetimeQualifyingPoints,
        lifetimeSpentVnd: account.lifetimeSpentVnd,
        rowVersion: account.rowVersion,
        eligibleVouchers: vouchers,
        availableServiceVouchers: serviceVouchers,
        recentLedger: ledger
      };
    };

    self.normalizeNotification = function (raw) {
      var n = raw || {};
      var resolvedId = String(pickDefined(n, ['notificationId', 'id', 'NotificationId', 'Id'], ''));
      var ts = pickDefined(n, ['occurredAtUtc', 'createdAtUtc', 'OccurredAtUtc', 'CreatedAtUtc'], new Date().toISOString());
      return {
        id: resolvedId,
        notificationId: resolvedId,
        eventType: String(pickDefined(n, ['eventType', 'EventType'], 'SystemAlert')),
        title: String(pickDefined(n, ['title', 'Title'], 'Notification')),
        message: String(pickDefined(n, ['message', 'Message'], '')),
        payloadJson: pickDefined(n, ['payloadJson', 'PayloadJson'], null),
        isRead: Boolean(pickDefined(n, ['isRead', 'IsRead'], false)),
        readAtUtc: pickDefined(n, ['readAtUtc', 'ReadAtUtc'], null),
        createdAtUtc: ts,
        occurredAtUtc: ts
      };
    };

    // --- 8. Workshop ServiceInvoices (/api/v1/service-invoices) ---
    self.serviceInvoices = {
      list: function (params) {
        var cleaned = self.cleanParams(params);
        return $http.get(baseUrl() + '/service-invoices', {
          params: cleaned
        }).then(function (res) {
          var rows = self.unwrapList(res).map(self.normalizeServiceInvoice);
          if (cleaned.bookingId) {
            var targetBkId = String(cleaned.bookingId).toLowerCase();
            var matched = rows.filter(function (inv) {
              return inv.serviceBookingId && inv.serviceBookingId.toLowerCase() === targetBkId;
            });
            if (matched.length > 0 || rows.length === 0) {
              return matched;
            }
          }
          return rows;
        });
      },

      getById: function (invoiceId) {
        return $http.get(baseUrl() + '/service-invoices/' + encodeURIComponent(invoiceId))
          .then(function (res) {
            return self.normalizeServiceInvoice(self.unwrapItem(res));
          });
      },

      create: function (payload, idempotencyKey) {
        var p = payload || {};
        var key = idempotencyKey || p.idempotencyKey || self.generateIdempotencyKey('service-invoice');
        var bookingId = p.serviceBookingId || p.bookingId;
        var bookingRowVer = p.bookingRowVersion || p.rowVersion || null;
        var voucherId = p.appliedCustomerVoucherId ? String(p.appliedCustomerVoucherId).trim() : null;
        var voucherCode = p.voucherCode ? String(p.voucherCode).trim() : null;
        var voucherRowVer = p.customerVoucherRowVersion || p.voucherRowVersion || null;
        var body = {
          serviceBookingId: bookingId,
          bookingRowVersion: bookingRowVer,
          rowVersion: bookingRowVer,
          laborAmount: Math.max(0, Number(p.laborAmount) || 0),
          partsAmount: Math.max(0, Number(p.partsAmount) || 0),
          odometerAtCompletionKm: Math.max(0, Math.floor(Number(p.odometerAtCompletionKm) || 0)),
          appliedCustomerVoucherId: voucherId,
          voucherCode: voucherCode,
          customerVoucherRowVersion: voucherRowVer,
          voucherRowVersion: voucherRowVer,
          useBookingReservedVoucher: p.useBookingReservedVoucher !== undefined ? Boolean(p.useBookingReservedVoucher) : Boolean(voucherId || voucherCode),
          removeReservedVoucher: Boolean(p.removeReservedVoucher),
          paymentMethod: p.paymentMethod || 'BankTransfer',
          workSummaryNotes: p.workSummaryNotes ? String(p.workSummaryNotes).trim() : null,
          lineItemsJson: p.lineItemsJson ? String(p.lineItemsJson).trim() : null
        };
        if (angular.isArray(p.lineItems) && p.lineItems.length > 0) {
          body.lineItems = p.lineItems.map(function (li) {
            return {
              sparePartId: li.sparePartId || null,
              itemType: li.itemType || (li.sparePartId ? 'Part' : 'Labor'),
              description: li.description ? String(li.description).trim() : null,
              quantity: Math.max(1, Math.floor(Number(li.quantity) || 1)),
              unitPriceVnd: li.unitPriceVnd !== undefined && li.unitPriceVnd !== null
                ? Math.max(0, Number(li.unitPriceVnd) || 0)
                : null
            };
          });
        }
        return $http.post(baseUrl() + '/service-invoices', body, {
          headers: { 'Idempotency-Key': key }
        }).then(function (res) {
          return self.normalizeServiceInvoice(self.unwrapItem(res));
        });
      }
    };

    self.listServiceInvoices = self.serviceInvoices.list;
    self.getServiceInvoiceById = self.serviceInvoices.getById;
    self.createServiceInvoice = self.serviceInvoices.create;

    // --- 9. Loyalty Wallet, Tiers, Customer Checkout Summary & Audited Adjustments (/api/v1/loyalty/*) ---
    var CANONICAL_LEDGER_TRANSACTION_TYPES = Object.freeze([
      Object.freeze({ value: 'SalesOrderEarn', label: 'SalesOrderEarn (Showroom Bike)' }),
      Object.freeze({ value: 'ServiceInvoiceEarn', label: 'ServiceInvoiceEarn (Workshop)' }),
      Object.freeze({ value: 'ServiceReviewBonus', label: 'ServiceReviewBonus (+25\u2605 CSAT Bonus)' }),
      Object.freeze({ value: 'VoucherRedeem', label: 'VoucherRedeem (Reward Spend)' }),
      Object.freeze({ value: 'VoucherExpireRefund', label: 'VoucherExpireRefund' }),
      Object.freeze({ value: 'AdminAdjustment', label: 'AdminAdjustment (Goodwill / Audit)' }),
      Object.freeze({ value: 'WelcomeBonus', label: 'WelcomeBonus' })
    ]);

    self.getLedgerTransactionTypes = function () {
      return CANONICAL_LEDGER_TRANSACTION_TYPES.map(function (t) {
        return angular.extend({}, t);
      });
    };

    self.loyalty = {
      getTransactionTypes: self.getLedgerTransactionTypes,
      getMyAccount: function () {
        return $http.get(baseUrl() + '/loyalty/me')
          .catch(function (err) {
            if (err && err.status === 404) {
              return $http.get(baseUrl() + '/loyalty/account');
            }
            return $q.reject(err);
          })
          .then(function (res) {
            return self.normalizeLoyaltyAccount(self.unwrapItem(res));
          });
      },

      getMyLedger: function (params) {
        return $http.get(baseUrl() + '/loyalty/ledger', {
          params: self.cleanParams(params)
        }).then(function (res) {
          var paged = self.unwrapPaged(res);
          return {
            items: (paged.items || []).map(self.normalizeLedgerEntry),
            pagination: paged.pagination
          };
        });
      },

      getTiers: function () {
        return $http.get(baseUrl() + '/loyalty/tiers')
          .then(function (res) {
            return self.unwrapList(res)
              .map(self.normalizeLoyaltyTier)
              .sort(function (a, b) {
                if (a.rankOrder !== b.rankOrder) {
                  return a.rankOrder - b.rankOrder;
                }
                return a.minQualifyingPoints - b.minQualifyingPoints;
              });
          });
      },

      updateTier: function (tierId, payload) {
        var p = payload || {};
        var mult = Number(p.pointsMultiplier !== undefined ? p.pointsMultiplier : p.multiplier);
        var body = {
          displayName: String(p.displayName || '').trim(),
          pointsMultiplier: (!isNaN(mult) && mult >= 1.0) ? mult : 1.0,
          minQualifyingPoints: Math.max(0, Math.floor(Number(p.minQualifyingPoints) || 0)),
          benefitsJson: p.benefitsJson ? String(p.benefitsJson).trim() : '[]'
        };
        if (p.rowVersion) {
          body.rowVersion = p.rowVersion;
        }
        return $http.put(baseUrl() + '/loyalty/tiers/' + encodeURIComponent(tierId), body)
          .then(function (res) {
            return self.normalizeLoyaltyTier(self.unwrapItem(res));
          });
      },

      getCustomerSummary: function (customerId) {
        return $http.get(baseUrl() + '/loyalty/customers/' + encodeURIComponent(customerId))
          .then(function (res) {
            return self.normalizeCustomerLoyaltySummary(self.unwrapItem(res));
          });
      },

      adjustPoints: function (payload, idempotencyKey) {
        var p = payload || {};
        var key = idempotencyKey || p.idempotencyKey || self.generateIdempotencyKey('ADMIN_ADJUST');
        var delta = Math.trunc(Number(p.pointsDelta) || 0);
        var notesStr = String(p.notes || p.reasonNotes || '').trim();
        var qualifies = p.qualifiesForTier !== undefined
          ? Boolean(p.qualifiesForTier)
          : (p.affectQualifyingPoints !== undefined ? Boolean(p.affectQualifyingPoints) : (delta > 0));
        var rowVer = p.accountRowVersion || p.rowVersion || null;
        var body = {
          loyaltyAccountId: p.loyaltyAccountId || p.accountId || null,
          customerId: p.customerId ? String(p.customerId).trim() : null,
          customerEmail: p.customerEmail ? String(p.customerEmail).trim() : null,
          pointsDelta: delta,
          notes: notesStr,
          reasonNotes: notesStr,
          qualifiesForTier: qualifies,
          affectQualifyingPoints: qualifies,
          accountRowVersion: rowVer,
          rowVersion: rowVer,
          idempotencyKey: key
        };
        return $http.post(baseUrl() + '/loyalty/adjustments', body, {
          headers: { 'Idempotency-Key': key }
        }).then(function (res) {
          var unwrapped = self.unwrapItem(res);
          var entrySource = (unwrapped && angular.isObject(unwrapped.ledgerEntry || unwrapped.entry))
            ? (unwrapped.ledgerEntry || unwrapped.entry)
            : unwrapped;
          return self.normalizeLedgerEntry(entrySource);
        });
      }
    };

    function resolveCatalogTierPayload(p) {
      var rawId = p.minTierId ? String(p.minTierId).trim() : '';
      var rawCode = p.minTierCode ? String(p.minTierCode).trim() : '';
      var isTierName = /^(bronze|silver|gold|platinum)$/i.test(rawId);
      return {
        minTierId: (!rawId || isTierName) ? null : rawId,
        minTierCode: rawCode || (isTierName ? rawId : null)
      };
    }

    // --- 10. Reward Voucher Catalog & Customer Voucher Wallet (/api/v1/vouchers/*) ---
    self.vouchers = {
      listCatalog: function (params) {
        return $http.get(baseUrl() + '/vouchers/catalog', {
          params: self.cleanParams(params)
        }).then(function (res) {
          return self.unwrapList(res).map(self.normalizeVoucherCatalog);
        });
      },

      createCatalog: function (payload) {
        var p = payload || {};
        var dtype = p.discountType || 'FixedAmount';
        var rawQuota = p.quotaTotal !== '' && p.quotaTotal !== null && p.quotaTotal !== undefined
          ? Math.max(1, Math.floor(Number(p.quotaTotal) || 1))
          : null;
        var rawMax = dtype === 'Percentage' && p.maxDiscountAmount !== null && p.maxDiscountAmount !== undefined && p.maxDiscountAmount !== ''
          ? Math.max(0, Number(p.maxDiscountAmount) || 0)
          : null;
        var tierRef = resolveCatalogTierPayload(p);
        var body = {
          codePrefix: String(p.codePrefix || '').trim().toUpperCase(),
          title: String(p.title || '').trim(),
          description: String(p.description || '').trim(),
          applicableScope: p.applicableScope || 'ServiceOnly',
          pointsCost: Math.max(1, Math.floor(Number(p.pointsCost) || 1)),
          discountType: dtype,
          discountValue: Math.max(0, Number(p.discountValue) || 0),
          maxDiscountAmount: rawMax,
          minOrderAmount: Math.max(0, Number(p.minOrderAmount) || 0),
          validDays: Math.max(1, Math.floor(Number(p.validDays) || 90)),
          quotaTotal: rawQuota,
          minTierId: tierRef.minTierId,
          minTierCode: tierRef.minTierCode,
          isActive: p.isActive !== false
        };
        return $http.post(baseUrl() + '/vouchers/catalog', body)
          .then(function (res) {
            return self.normalizeVoucherCatalog(self.unwrapItem(res));
          });
      },

      updateCatalog: function (catalogId, payload) {
        var p = payload || {};
        var dtype = p.discountType || 'FixedAmount';
        var rawQuota = p.quotaTotal !== '' && p.quotaTotal !== null && p.quotaTotal !== undefined
          ? Math.max(1, Math.floor(Number(p.quotaTotal) || 1))
          : null;
        var rawMax = dtype === 'Percentage' && p.maxDiscountAmount !== null && p.maxDiscountAmount !== undefined && p.maxDiscountAmount !== ''
          ? Math.max(0, Number(p.maxDiscountAmount) || 0)
          : null;
        var tierRef = resolveCatalogTierPayload(p);
        var body = {
          codePrefix: String(p.codePrefix || '').trim().toUpperCase(),
          title: String(p.title || '').trim(),
          description: String(p.description || '').trim(),
          applicableScope: p.applicableScope || 'ServiceOnly',
          pointsCost: Math.max(1, Math.floor(Number(p.pointsCost) || 1)),
          discountType: dtype,
          discountValue: Math.max(0, Number(p.discountValue) || 0),
          maxDiscountAmount: rawMax,
          minOrderAmount: Math.max(0, Number(p.minOrderAmount) || 0),
          validDays: Math.max(1, Math.floor(Number(p.validDays) || 90)),
          quotaTotal: rawQuota,
          minTierId: tierRef.minTierId,
          minTierCode: tierRef.minTierCode,
          isActive: p.isActive !== false
        };
        return $http.put(baseUrl() + '/vouchers/catalog/' + encodeURIComponent(catalogId), body)
          .then(function (res) {
            return self.normalizeVoucherCatalog(self.unwrapItem(res));
          });
      },

      listMyVouchers: function (params) {
        return $http.get(baseUrl() + '/vouchers/my', {
          params: self.cleanParams(params)
        }).then(function (res) {
          return self.unwrapList(res).map(self.normalizeCustomerVoucher);
        });
      },

      redeem: function (payload, idempotencyKey) {
        var p = payload || {};
        var key = idempotencyKey || p.idempotencyKey || self.generateIdempotencyKey('VOUCHER_REDEEM');
        var catalogId = p.voucherCatalogId || p.catalogId;
        var rowVer = p.accountRowVersion || p.rowVersion || null;
        var body = {
          voucherCatalogId: catalogId,
          catalogId: catalogId,
          accountRowVersion: rowVer,
          rowVersion: rowVer,
          idempotencyKey: key
        };
        return $http.post(baseUrl() + '/vouchers/redeem', body, {
          headers: { 'Idempotency-Key': key }
        }).then(function (res) {
          var unwrapped = self.unwrapItem(res);
          var vSource = (unwrapped && angular.isObject(unwrapped.voucher || unwrapped.customerVoucher))
            ? (unwrapped.voucher || unwrapped.customerVoucher)
            : unwrapped;
          var normalized = self.normalizeCustomerVoucher(vSource);
          if (unwrapped && angular.isObject(unwrapped)) {
            if (normalized.availablePointsAfter === null && unwrapped.availablePointsAfter !== undefined) {
              normalized.availablePointsAfter = Number(unwrapped.availablePointsAfter);
            }
            if (!normalized.accountRowVersion && unwrapped.accountRowVersion) {
              normalized.accountRowVersion = String(unwrapped.accountRowVersion);
            }
          }
          return normalized;
        });
      }
    };

    // --- 11. Persistent Real-Time Notification Inbox (/api/v1/notifications*) ---
    self.notifications = {
      list: function (params) {
        return $http.get(baseUrl() + '/notifications', {
          params: self.cleanParams(params)
        }).then(function (res) {
          var rawEnvelope = res && res.data !== undefined ? res.data : res;
          var dataLayer = self.unwrapItem(res);
          var paged = self.unwrapPaged(res);
          var items = (paged.items || []).map(self.normalizeNotification);

          var rawUnread = pickDefined(
            dataLayer,
            ['unreadCount', 'UnreadCount'],
            pickDefined(
              rawEnvelope,
              ['unreadCount', 'UnreadCount'],
              rawEnvelope && angular.isObject(rawEnvelope.meta)
                ? pickDefined(rawEnvelope.meta, ['unreadCount', 'UnreadCount'], null)
                : null
            )
          );
          if (rawUnread === null && paged.pagination && paged.pagination.unreadCount !== undefined) {
            rawUnread = paged.pagination.unreadCount;
          }

          var fallbackUnread = 0;
          for (var i = 0; i < items.length; i++) {
            if (!items[i].isRead) {
              fallbackUnread++;
            }
          }

          return {
            items: items,
            unreadCount: rawUnread !== null && rawUnread !== undefined
              ? Math.max(0, Math.floor(Number(rawUnread) || 0))
              : fallbackUnread,
            pagination: paged.pagination
          };
        });
      },

      markRead: function (notificationId) {
        var targetUrl = baseUrl() + '/notifications/' + encodeURIComponent(notificationId) + '/read';
        return $http.patch(targetUrl, {})
          .catch(function (err) {
            if (err && err.status === 405) {
              return $http.post(targetUrl, {});
            }
            return $q.reject(err);
          })
          .then(function (res) {
            return self.unwrapItem(res);
          });
      },

      markAllRead: function () {
        return $http.post(baseUrl() + '/notifications/read-all', {})
          .then(function (res) {
            return self.unwrapItem(res);
          });
      }
    };

    // --- 12. Admin Cross-Portal Focus Context & Executive Multi-Dealer Analytics (/api/v1/admin/analytics/*) ---
    var adminFocusContext = null;

    self.setAdminFocusContext = function (ctx) {
      adminFocusContext = ctx && angular.isObject(ctx) ? angular.extend({}, ctx) : null;
    };

    self.consumeAdminFocusContext = function () {
      var snapshot = adminFocusContext ? angular.extend({}, adminFocusContext) : null;
      adminFocusContext = null;
      return snapshot;
    };

    var CANONICAL_BOOKING_STATUSES = [
      'Completed',
      'Confirmed',
      'CheckedIn',
      'InProgress',
      'Pending',
      'Cancelled',
      'NoShow'
    ];

    var CANONICAL_TIER_META = [
      { code: 'Bronze', multiplier: '1.00x', defaultMultNum: 1.0 },
      { code: 'Silver', multiplier: '1.20x', defaultMultNum: 1.2 },
      { code: 'Gold', multiplier: '1.50x', defaultMultNum: 1.5 },
      { code: 'Platinum', multiplier: '2.00x', defaultMultNum: 2.0 }
    ];

    function pad2(n) {
      var v = Math.floor(Math.abs(Number(n) || 0));
      return v < 10 ? ('0' + v) : String(v);
    }

    function extractDateOnlyYmd(input) {
      if (!input) {
        return '';
      }
      if (input instanceof Date) {
        if (isNaN(input.getTime())) {
          return '';
        }
        return input.getFullYear() + '-' + pad2(input.getMonth() + 1) + '-' + pad2(input.getDate());
      }
      var str = String(input).trim();
      if (/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(str)) {
        return str;
      }
      return '';
    }

    self.toAnalyticsUtcBoundary = function (value, isEndOfDay) {
      if (!value) {
        return '';
      }
      if (angular.isString(value)) {
        var trimmed = value.trim();
        if (!trimmed) {
          return '';
        }
        if (/^[0-9]{4}-[0-9]{2}-[0-9]{2}T/i.test(trimmed)) {
          var parsedIso = Date.parse(trimmed);
          return !isNaN(parsedIso) ? new Date(parsedIso).toISOString() : trimmed;
        }
      }
      var ymd = extractDateOnlyYmd(value);
      if (ymd) {
        var suffix = isEndOfDay ? 'T23:59:59.999+07:00' : 'T00:00:00.000+07:00';
        var boundaryMs = Date.parse(ymd + suffix);
        if (!isNaN(boundaryMs)) {
          if (isEndOfDay && ymd === self.getTodayDateVn()) {
            boundaryMs = Math.max(boundaryMs, Date.now());
          }
          return new Date(boundaryMs).toISOString();
        }
      }
      if (value instanceof Date && !isNaN(value.getTime())) {
        return value.toISOString();
      }
      return '';
    };

    self.buildAnalyticsParams = function (rawParams) {
      var p = rawParams && angular.isObject(rawParams) ? rawParams : {};
      var fromUtc = p.fromUtc
        ? self.toAnalyticsUtcBoundary(p.fromUtc, false)
        : (p.fromDate ? self.toAnalyticsUtcBoundary(p.fromDate, false) : '');
      var toUtc = p.toUtc
        ? self.toAnalyticsUtcBoundary(p.toUtc, true)
        : (p.toDate ? self.toAnalyticsUtcBoundary(p.toDate, true) : '');
      var dealerId = p.dealerId && String(p.dealerId).trim() && String(p.dealerId).trim().toUpperCase() !== 'ALL'
        ? String(p.dealerId).trim()
        : '';

      return self.cleanParams({
        fromUtc: fromUtc || null,
        toUtc: toUtc || null,
        dealerId: dealerId || null
      });
    };

    function readResponseHeader(res, headerName) {
      if (!res || !res.headers || !headerName) {
        return '';
      }
      var targetLower = String(headerName).toLowerCase();
      if (angular.isFunction(res.headers)) {
        return res.headers(headerName) || res.headers(targetLower) || '';
      }
      if (angular.isObject(res.headers)) {
        var keys = Object.keys(res.headers);
        for (var i = 0; i < keys.length; i++) {
          if (keys[i].toLowerCase() === targetLower) {
            return String(res.headers[keys[i]] || '');
          }
        }
      }
      return '';
    }

    function sanitizeCsvBasename(candidate) {
      if (!angular.isString(candidate)) {
        return '';
      }
      var unquoted = candidate.trim().replace(/^["']|["']$/g, '').trim();
      if (!unquoted) {
        return '';
      }
      var segments = unquoted.split(/[\\/]/);
      var leaf = String(segments[segments.length - 1] || '').replace(/^\.+/, '').trim();
      return leaf;
    }

    self.extractCsvFilename = function (contentDispositionHeader) {
      var fallback = 'dealer-analytics-' + self.getTodayDateVn().replace(/-/g, '') + '.csv';
      if (!angular.isString(contentDispositionHeader) || !contentDispositionHeader.trim()) {
        return fallback;
      }
      var raw = contentDispositionHeader.trim();
      var utf8Match = raw.match(/filename\*\s*=\s*UTF-8''([^;\r\n]+)/i);
      if (utf8Match && utf8Match[1]) {
        try {
          var cleanDecoded = sanitizeCsvBasename(decodeURIComponent(utf8Match[1].trim()));
          if (cleanDecoded) {
            return cleanDecoded;
          }
        } catch (e) {
          // Fall through to standard filename= parser
        }
      }
      var quotedMatch = raw.match(/filename\s*=\s*"([^"]+)"/i);
      if (quotedMatch && quotedMatch[1]) {
        var cleanQuoted = sanitizeCsvBasename(quotedMatch[1]);
        if (cleanQuoted) {
          return cleanQuoted;
        }
      }
      var plainMatch = raw.match(/filename\s*=\s*([^;\s\r\n]+)/i);
      if (plainMatch && plainMatch[1]) {
        var cleanPlain = sanitizeCsvBasename(plainMatch[1]);
        if (cleanPlain) {
          return cleanPlain;
        }
      }
      return fallback;
    };

    function isArrayBufferValue(val) {
      return Boolean(val) && Object.prototype.toString.call(val) === '[object ArrayBuffer]';
    }

    function decodeArrayBufferRejection(rejection) {
      if (!rejection || !rejection.data) {
        return rejection;
      }
      var data = rejection.data;
      var bytes = null;
      if (isArrayBufferValue(data)) {
        bytes = new Uint8Array(data);
      } else if (typeof ArrayBuffer !== 'undefined' && ArrayBuffer.isView && ArrayBuffer.isView(data)) {
        bytes = new Uint8Array(data.buffer, data.byteOffset, data.byteLength);
      }
      if (!bytes || bytes.length === 0) {
        return rejection;
      }
      try {
        var text = '';
        if (typeof TextDecoder !== 'undefined') {
          text = new TextDecoder('utf-8').decode(bytes);
        } else {
          for (var i = 0; i < bytes.length; i++) {
            text += String.fromCharCode(bytes[i]);
          }
        }
        var parsed = JSON.parse(text);
        if (parsed && angular.isObject(parsed)) {
          return angular.extend({}, rejection, { data: parsed });
        }
      } catch (e) {
        // Leave original rejection intact when body is not JSON
      }
      return rejection;
    }

    function round2(num) {
      var n = Number(num);
      if (!isFinite(n)) {
        return 0;
      }
      return Math.round(n * 100) / 100;
    }

    function pickArray(obj, keys) {
      if (!obj || !angular.isObject(obj)) {
        return null;
      }
      for (var i = 0; i < keys.length; i++) {
        var val = obj[keys[i]];
        if (angular.isArray(val)) {
          return val;
        }
      }
      return null;
    }

    self.normalizeDealerAnalyticsRow = function (raw) {
      var r = raw || {};
      var code = String(pickDefined(r, ['dealerCode', 'code', 'DealerCode', 'Code'], '')).toUpperCase();
      var name = String(pickDefined(r, ['dealerName', 'name', 'DealerName', 'Name'], code || 'Dealership'));
      var resolvedId = String(pickDefined(r, ['dealerId', 'id', 'DealerId', 'Id'], code || name));

      var unitsSold = Math.max(0, Math.floor(Number(pickDefined(r, ['unitsSoldCount', 'unitsSold', 'UnitsSoldCount', 'UnitsSold'], 0)) || 0));
      var showroomRev = Math.max(0, Number(pickDefined(r, ['showroomRevenueVnd', 'showroomSalesVnd', 'ShowroomRevenueVnd', 'ShowroomSalesVnd'], 0)) || 0);
      var showroomDisc = Math.max(0, Number(pickDefined(r, ['showroomDiscountVnd', 'ShowroomDiscountVnd'], 0)) || 0);
      var invoicesCount = Math.max(0, Math.floor(Number(pickDefined(r, ['invoicesIssuedCount', 'invoicesIssued', 'invoicesCount', 'InvoicesIssuedCount', 'InvoicesIssued', 'InvoicesCount'], 0)) || 0));
      var serviceRev = Math.max(0, Number(pickDefined(r, ['serviceRevenueVnd', 'workshopServiceVnd', 'ServiceRevenueVnd', 'WorkshopServiceVnd'], 0)) || 0);
      var serviceDisc = Math.max(0, Number(pickDefined(r, ['serviceVoucherDiscountVnd', 'ServiceVoucherDiscountVnd'], 0)) || 0);
      var totalRev = Math.max(0, Number(pickDefined(r, ['totalRevenueVnd', 'combinedRevenueVnd', 'TotalRevenueVnd'], showroomRev + serviceRev)) || 0);
      var revShare = round2(pickDefined(r, ['revenueSharePct', 'RevenueSharePct'], 0));

      var pendingBk = Math.max(0, Math.floor(Number(pickDefined(r, ['pendingBookings', 'pendingCount', 'pending', 'PendingBookings', 'Pending'], 0)) || 0));
      var confirmedBk = Math.max(0, Math.floor(Number(pickDefined(r, ['confirmedBookings', 'confirmedCount', 'confirmed', 'ConfirmedBookings', 'Confirmed'], 0)) || 0));
      var checkedInBk = Math.max(0, Math.floor(Number(pickDefined(r, ['checkedInBookings', 'checkedInCount', 'checkedIn', 'CheckedInBookings', 'CheckedIn'], 0)) || 0));
      var inProgressBk = Math.max(0, Math.floor(Number(pickDefined(r, ['inProgressBookings', 'inProgressCount', 'inProgress', 'InProgressBookings', 'InProgress'], 0)) || 0));
      var completedBk = Math.max(0, Math.floor(Number(pickDefined(r, ['completedBookings', 'completedCount', 'completed', 'CompletedBookings', 'Completed'], 0)) || 0));
      var cancelledBk = Math.max(0, Math.floor(Number(pickDefined(r, ['cancelledBookings', 'cancelledCount', 'cancelled', 'CancelledBookings', 'Cancelled'], 0)) || 0));
      var noShowBk = Math.max(0, Math.floor(Number(pickDefined(r, ['noShowBookings', 'noShowCount', 'noShow', 'NoShowBookings', 'NoShow'], 0)) || 0));

      var computedActive = pendingBk + confirmedBk + checkedInBk + inProgressBk;
      var activeBk = Math.max(0, Math.floor(Number(pickDefined(r, ['activeBookings', 'ActiveBookings'], computedActive)) || 0));
      var computedCancelledOrNoShow = cancelledBk + noShowBk;
      var cancelledOrNoShowBk = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['cancelledOrNoShowBookings', 'cancelledAndNoShowBookings', 'CancelledOrNoShowBookings'], computedCancelledOrNoShow)) || 0)
      );
      var totalBk = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['totalBookings', 'TotalBookings'], completedBk + activeBk + cancelledOrNoShowBk)) || 0)
      );

      var completionPct = round2(
        pickDefined(r, ['completionRatePct', 'CompletionRatePct'], totalBk > 0 ? ((completedBk * 100) / totalBk) : 0)
      );
      var cancellationPct = round2(
        pickDefined(r, ['cancellationRatePct', 'CancellationRatePct'], totalBk > 0 ? ((cancelledOrNoShowBk * 100) / totalBk) : 0)
      );

      var avgCsat = round2(pickDefined(r, ['averageCsatRating', 'averageRating', 'AverageCsatRating', 'AverageRating'], 0));
      var reviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['totalServiceReviewsCount', 'serviceReviewsCount', 'reviewsCount', 'TotalServiceReviewsCount'], 0)) || 0)
      );
      var fiveStarReviews = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['fiveStarReviewsCount', 'fiveStarCount', 'FiveStarReviewsCount'], 0)) || 0)
      );
      var unansweredReviews = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['unansweredReviewsCount', 'unansweredCount', 'UnansweredReviewsCount'], 0)) || 0)
      );
      var openWarrantyClaims = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['openWarrantyClaimsCount', 'openClaimsCount', 'OpenWarrantyClaimsCount'], 0)) || 0)
      );
      var completedWarrantyClaims = Math.max(
        0,
        Math.floor(Number(pickDefined(r, ['completedWarrantyClaimsCount', 'completedClaimsCount', 'CompletedWarrantyClaimsCount'], 0)) || 0)
      );
      var approvedWarrantyVnd = Math.max(
        0,
        Number(pickDefined(r, ['approvedWarrantyCoveredVnd', 'warrantyCoveredVnd', 'coveredAmountVnd', 'ApprovedWarrantyCoveredVnd'], 0)) || 0
      );

      return {
        dealerId: resolvedId,
        dealerCode: code,
        dealerName: name,
        city: String(pickDefined(r, ['city', 'City'], '')),
        isActive: pickDefined(r, ['isActive', 'IsActive'], true) !== false,
        unitsSold: unitsSold,
        unitsSoldCount: unitsSold,
        showroomRevenueVnd: showroomRev,
        showroomDiscountVnd: showroomDisc,
        invoicesIssued: invoicesCount,
        invoicesIssuedCount: invoicesCount,
        serviceRevenueVnd: serviceRev,
        serviceVoucherDiscountVnd: serviceDisc,
        totalRevenueVnd: totalRev,
        revenueSharePct: revShare,
        pendingBookings: pendingBk,
        confirmedBookings: confirmedBk,
        checkedInBookings: checkedInBk,
        inProgressBookings: inProgressBk,
        completedBookings: completedBk,
        cancelledBookings: cancelledBk,
        noShowBookings: noShowBk,
        activeBookings: activeBk,
        cancelledOrNoShowBookings: cancelledOrNoShowBk,
        totalBookings: totalBk,
        completionRatePct: completionPct,
        cancellationRatePct: cancellationPct,
        averageCsatRating: avgCsat,
        totalServiceReviewsCount: reviewsCount,
        fiveStarReviewsCount: fiveStarReviews,
        unansweredReviewsCount: unansweredReviews,
        openWarrantyClaimsCount: openWarrantyClaims,
        completedWarrantyClaimsCount: completedWarrantyClaims,
        approvedWarrantyCoveredVnd: approvedWarrantyVnd
      };
    };

    self.normalizeAnalyticsOverview = function (raw) {
      var o = raw || {};
      var rawDealers = pickDefined(o, ['dealers', 'dealerRows', 'dealerBreakdown', 'items', 'Dealers'], []);
      var dealers = angular.isArray(rawDealers) ? rawDealers.map(self.normalizeDealerAnalyticsRow) : [];

      var sumUnits = 0;
      var sumShowroom = 0;
      var sumInvoices = 0;
      var sumService = 0;
      var sumBk = 0;
      var sumDoneBk = 0;
      var sumActiveBk = 0;
      var sumCancelNoShowBk = 0;
      var sumReviews = 0;
      var sumWeightedRating = 0;
      var sumOpenWarranty = 0;
      var sumApprovedWarrantyVnd = 0;
      var activeBranchCount = 0;
      for (var i = 0; i < dealers.length; i++) {
        sumUnits += dealers[i].unitsSoldCount;
        sumShowroom += dealers[i].showroomRevenueVnd;
        sumInvoices += dealers[i].invoicesIssuedCount;
        sumService += dealers[i].serviceRevenueVnd;
        sumBk += dealers[i].totalBookings;
        sumDoneBk += dealers[i].completedBookings;
        sumActiveBk += dealers[i].activeBookings;
        sumCancelNoShowBk += dealers[i].cancelledOrNoShowBookings;
        sumReviews += dealers[i].totalServiceReviewsCount;
        sumWeightedRating += dealers[i].averageCsatRating * dealers[i].totalServiceReviewsCount;
        sumOpenWarranty += dealers[i].openWarrantyClaimsCount;
        sumApprovedWarrantyVnd += dealers[i].approvedWarrantyCoveredVnd;
        if (dealers[i].isActive) {
          activeBranchCount++;
        }
      }

      var showroomRev = Math.max(0, Number(pickDefined(o, ['showroomRevenueVnd', 'ShowroomRevenueVnd'], sumShowroom)) || 0);
      var serviceRev = Math.max(0, Number(pickDefined(o, ['serviceRevenueVnd', 'ServiceRevenueVnd'], sumService)) || 0);
      var totalRev = Math.max(0, Number(pickDefined(o, ['totalRevenueVnd', 'TotalRevenueVnd'], showroomRev + serviceRev)) || 0);
      var unitsSold = Math.max(0, Math.floor(Number(pickDefined(o, ['unitsSoldCount', 'unitsSold', 'UnitsSoldCount'], sumUnits)) || 0));
      var invoicesIssued = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['invoicesIssuedCount', 'invoicesIssued', 'invoicesCount', 'InvoicesIssuedCount'], sumInvoices)) || 0)
      );
      var totalVoucherDisc = Math.max(0, Number(pickDefined(o, ['totalVoucherDiscountVnd', 'TotalVoucherDiscountVnd'], 0)) || 0);

      var totalBk = Math.max(0, Math.floor(Number(pickDefined(o, ['totalBookings', 'TotalBookings'], sumBk)) || 0));
      var completedBk = Math.max(0, Math.floor(Number(pickDefined(o, ['completedBookings', 'CompletedBookings'], sumDoneBk)) || 0));
      var cancelledBk = Math.max(0, Math.floor(Number(pickDefined(o, ['cancelledBookings', 'CancelledBookings'], 0)) || 0));
      var noShowBk = Math.max(0, Math.floor(Number(pickDefined(o, ['noShowBookings', 'NoShowBookings'], 0)) || 0));
      var cancelledOrNoShowBk = Math.max(
        0,
        Math.floor(
          Number(
            pickDefined(
              o,
              ['cancelledOrNoShowBookings', 'cancelledAndNoShowBookings', 'CancelledOrNoShowBookings'],
              (cancelledBk + noShowBk) || sumCancelNoShowBk
            )
          ) || 0
        )
      );
      var activeBk = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['activeBookings', 'ActiveBookings'], sumActiveBk || Math.max(0, totalBk - completedBk - cancelledOrNoShowBk))) || 0)
      );

      var completionPct = round2(
        pickDefined(o, ['completionRatePct', 'CompletionRatePct'], totalBk > 0 ? ((completedBk * 100) / totalBk) : 0)
      );
      var cancellationPct = round2(
        pickDefined(o, ['cancellationRatePct', 'CancellationRatePct'], totalBk > 0 ? ((cancelledOrNoShowBk * 100) / totalBk) : 0)
      );

      var activeDealersCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['activeDealersCount', 'ActiveDealersCount'], activeBranchCount || dealers.length)) || 0)
      );
      var totalMembersCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['totalMembersCount', 'activeLoyaltyMembers', 'totalMembers', 'activeCustomersCount', 'TotalMembersCount', 'ActiveLoyaltyMembers'], 0)) || 0)
      );
      var unredeemedPointsPool = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['unredeemedPointsPool', 'activePointsLiability', 'UnredeemedPointsPool', 'ActivePointsLiability'], 0)) || 0)
      );
      var pointsLiabilityVnd = Math.max(
        0,
        Number(pickDefined(o, ['pointsLiabilityVnd', 'PointsLiabilityVnd'], unredeemedPointsPool * 500)) || 0
      );

      var totalServiceReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['totalServiceReviewsCount', 'serviceReviewsCount', 'TotalServiceReviewsCount'], sumReviews)) || 0)
      );
      var fallbackAvgCsat = sumReviews > 0 ? (sumWeightedRating / sumReviews) : 0;
      var averageCsatRating = round2(
        pickDefined(o, ['averageCsatRating', 'averageRating', 'AverageCsatRating', 'AverageRating'], fallbackAvgCsat)
      );
      var openWarrantyClaimsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['openWarrantyClaimsCount', 'OpenWarrantyClaimsCount'], sumOpenWarranty)) || 0)
      );
      var approvedWarrantyCoveredVnd = Math.max(
        0,
        Number(pickDefined(o, ['approvedWarrantyCoveredVnd', 'ApprovedWarrantyCoveredVnd'], sumApprovedWarrantyVnd)) || 0
      );
      var fiveStarReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['fiveStarReviewsCount', 'fiveStarCount', 'FiveStarReviewsCount'], 0)) || 0)
      );
      var unansweredReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['unansweredReviewsCount', 'unansweredCount', 'UnansweredReviewsCount'], 0)) || 0)
      );
      var completedWarrantyClaimsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(o, ['completedWarrantyClaimsCount', 'completedClaimsCount', 'CompletedWarrantyClaimsCount'], 0)) || 0)
      );

      return {
        fromUtc: pickDefined(o, ['fromUtc', 'FromUtc'], null),
        toUtc: pickDefined(o, ['toUtc', 'ToUtc'], null),
        totalRevenueVnd: totalRev,
        showroomRevenueVnd: showroomRev,
        unitsSoldCount: unitsSold,
        serviceRevenueVnd: serviceRev,
        invoicesIssuedCount: invoicesIssued,
        totalVoucherDiscountVnd: totalVoucherDisc,
        totalBookings: totalBk,
        completedBookings: completedBk,
        activeBookings: activeBk,
        cancelledBookings: cancelledBk,
        noShowBookings: noShowBk,
        cancelledOrNoShowBookings: cancelledOrNoShowBk,
        completionRatePct: completionPct,
        cancellationRatePct: cancellationPct,
        activeDealersCount: activeDealersCount,
        totalMembersCount: totalMembersCount,
        activeLoyaltyMembers: totalMembersCount,
        unredeemedPointsPool: unredeemedPointsPool,
        activePointsLiability: unredeemedPointsPool,
        pointsLiabilityVnd: pointsLiabilityVnd,
        averageCsatRating: averageCsatRating,
        totalServiceReviewsCount: totalServiceReviewsCount,
        fiveStarReviewsCount: fiveStarReviewsCount,
        unansweredReviewsCount: unansweredReviewsCount,
        openWarrantyClaimsCount: openWarrantyClaimsCount,
        completedWarrantyClaimsCount: completedWarrantyClaimsCount,
        approvedWarrantyCoveredVnd: approvedWarrantyCoveredVnd,
        dealers: dealers
      };
    };

    self.normalizeAnalyticsRevenueByDealer = function (raw) {
      var r = angular.isArray(raw) ? { dealers: raw } : (raw || {});
      var rawDealers = pickArray(r, ['dealers', 'items', 'rows', 'Dealers', 'Items']) || [];
      var dealers = rawDealers.map(self.normalizeDealerAnalyticsRow);

      var rawLabels = pickArray(r, ['labels', 'dealerLabels', 'Labels']);
      var rawCodes = pickArray(r, ['dealerCodes', 'codes', 'DealerCodes']);
      var rawShowroom = pickArray(r, ['showroomSalesVnd', 'showroomRevenueByDealerVnd', 'showroomRevenueVnd', 'ShowroomSalesVnd', 'ShowroomRevenueVnd']);
      var rawService = pickArray(r, ['workshopServiceVnd', 'serviceRevenueByDealerVnd', 'serviceRevenueVnd', 'WorkshopServiceVnd', 'ServiceRevenueVnd']);
      var rawTotal = pickArray(r, ['combinedRevenueByDealerVnd', 'totalRevenueByDealerVnd', 'totalRevenueVnd', 'CombinedRevenueByDealerVnd', 'TotalRevenueVnd']);

      var labels = angular.isArray(rawLabels)
        ? rawLabels.map(function (x) { return String(x || ''); })
        : dealers.map(function (d) { return d.dealerName || d.dealerCode; });
      var dealerCodes = angular.isArray(rawCodes)
        ? rawCodes.map(function (x) { return String(x || '').toUpperCase(); })
        : dealers.map(function (d) { return d.dealerCode; });
      var showroomSalesVnd = angular.isArray(rawShowroom)
        ? rawShowroom.map(function (x) { return Math.max(0, Number(x) || 0); })
        : dealers.map(function (d) { return d.showroomRevenueVnd; });
      var workshopServiceVnd = angular.isArray(rawService)
        ? rawService.map(function (x) { return Math.max(0, Number(x) || 0); })
        : dealers.map(function (d) { return d.serviceRevenueVnd; });
      var totalRevenueVnd = angular.isArray(rawTotal)
        ? rawTotal.map(function (x) { return Math.max(0, Number(x) || 0); })
        : labels.map(function (_lbl, idx) {
          return (showroomSalesVnd[idx] || 0) + (workshopServiceVnd[idx] || 0);
        });

      if (dealers.length === 0 && labels.length > 0) {
        dealers = labels.map(function (lbl, idx) {
          return self.normalizeDealerAnalyticsRow({
            dealerId: dealerCodes[idx] || ('dealer-' + idx),
            dealerCode: dealerCodes[idx] || ('DLR-' + (idx + 1)),
            dealerName: lbl,
            showroomRevenueVnd: showroomSalesVnd[idx] || 0,
            serviceRevenueVnd: workshopServiceVnd[idx] || 0,
            totalRevenueVnd: totalRevenueVnd[idx] || 0
          });
        });
      }

      return {
        labels: labels,
        dealerCodes: dealerCodes,
        showroomSalesVnd: showroomSalesVnd,
        workshopServiceVnd: workshopServiceVnd,
        totalRevenueVnd: totalRevenueVnd,
        dealers: dealers
      };
    };

    self.normalizeAnalyticsBookingsByDealer = function (raw) {
      var b = angular.isArray(raw) ? { dealers: raw } : (raw || {});
      var rawDealers = pickDefined(b, ['dealerBreakdown', 'dealers', 'items', 'rows', 'DealerBreakdown', 'Dealers'], []);
      var dealerBreakdown = angular.isArray(rawDealers) ? rawDealers.map(self.normalizeDealerAnalyticsRow) : [];

      var countsByStatusLower = Object.create(null);
      for (var sIdx = 0; sIdx < CANONICAL_BOOKING_STATUSES.length; sIdx++) {
        countsByStatusLower[CANONICAL_BOOKING_STATUSES[sIdx].toLowerCase()] = 0;
      }

      var rawStatusLabels = pickDefined(b, ['statusLabels', 'StatusLabels'], null);
      var rawStatusCounts = pickDefined(b, ['statusCounts', 'StatusCounts'], null);
      var rawStatusList = pickDefined(b, ['statusBreakdown', 'networkStatusBreakdown', 'statuses', 'StatusBreakdown', 'NetworkStatusBreakdown'], null);

      if (angular.isArray(rawStatusLabels) && angular.isArray(rawStatusCounts)) {
        for (var i = 0; i < rawStatusLabels.length; i++) {
          var key = String(rawStatusLabels[i] || '').trim().toLowerCase();
          if (key) {
            countsByStatusLower[key] = Math.max(0, Math.floor(Number(rawStatusCounts[i]) || 0));
          }
        }
      } else if (angular.isArray(rawStatusList)) {
        for (var j = 0; j < rawStatusList.length; j++) {
          var stObj = rawStatusList[j] || {};
          var stKey = String(pickDefined(stObj, ['status', 'statusCode', 'label', 'Status'], '')).trim().toLowerCase();
          var stVal = Math.max(0, Math.floor(Number(pickDefined(stObj, ['count', 'bookingCount', 'total', 'Count'], 0)) || 0));
          if (stKey) {
            countsByStatusLower[stKey] = stVal;
          }
        }
      } else if (dealerBreakdown.length > 0) {
        for (var dIdx = 0; dIdx < dealerBreakdown.length; dIdx++) {
          var row = dealerBreakdown[dIdx];
          countsByStatusLower.completed += row.completedBookings;
          countsByStatusLower.confirmed += row.confirmedBookings;
          countsByStatusLower.checkedin += row.checkedInBookings;
          countsByStatusLower.inprogress += row.inProgressBookings;
          countsByStatusLower.pending += row.pendingBookings;
          countsByStatusLower.cancelled += row.cancelledBookings;
          countsByStatusLower.noshow += row.noShowBookings;
        }
      }

      var statusLabels = CANONICAL_BOOKING_STATUSES.slice();
      var statusCounts = statusLabels.map(function (st) {
        return countsByStatusLower[st.toLowerCase()] || 0;
      });

      var rawDealerLabels = pickDefined(b, ['dealerLabels', 'labels', 'DealerLabels'], null);
      var rawCompletedSeries = pickDefined(b, ['completedByDealer', 'CompletedByDealer'], null);
      var rawActiveSeries = pickDefined(b, ['activeByDealer', 'ActiveByDealer'], null);
      var rawCancelSeries = pickDefined(b, ['cancelledOrNoShowByDealer', 'CancelledOrNoShowByDealer'], null);

      var dealerLabels = angular.isArray(rawDealerLabels)
        ? rawDealerLabels.map(function (x) { return String(x || ''); })
        : dealerBreakdown.map(function (d) { return d.dealerName || d.dealerCode; });
      var completedByDealer = angular.isArray(rawCompletedSeries)
        ? rawCompletedSeries.map(function (x) { return Math.max(0, Math.floor(Number(x) || 0)); })
        : dealerBreakdown.map(function (d) { return d.completedBookings; });
      var activeByDealer = angular.isArray(rawActiveSeries)
        ? rawActiveSeries.map(function (x) { return Math.max(0, Math.floor(Number(x) || 0)); })
        : dealerBreakdown.map(function (d) { return d.activeBookings; });
      var cancelledOrNoShowByDealer = angular.isArray(rawCancelSeries)
        ? rawCancelSeries.map(function (x) { return Math.max(0, Math.floor(Number(x) || 0)); })
        : dealerBreakdown.map(function (d) { return d.cancelledOrNoShowBookings; });

      return {
        statusLabels: statusLabels,
        statusCounts: statusCounts,
        dealerLabels: dealerLabels,
        completedByDealer: completedByDealer,
        activeByDealer: activeByDealer,
        cancelledOrNoShowByDealer: cancelledOrNoShowByDealer,
        dealerBreakdown: dealerBreakdown
      };
    };

    self.normalizeAnalyticsMonthlyTrends = function (raw) {
      var t = angular.isArray(raw) ? { buckets: raw } : (raw || {});
      var rawBuckets = pickDefined(t, ['buckets', 'months', 'items', 'trends', 'Buckets', 'Months', 'Items'], null);

      if (angular.isArray(rawBuckets) && rawBuckets.length > 0 && angular.isObject(rawBuckets[0])) {
        var normBuckets = rawBuckets.map(function (b) {
          var showroom = Math.max(0, Number(pickDefined(b, ['showroomRevenueVnd', 'ShowroomRevenueVnd'], 0)) || 0);
          var service = Math.max(0, Number(pickDefined(b, ['serviceRevenueVnd', 'ServiceRevenueVnd'], 0)) || 0);
          var combined = Math.max(0, Number(pickDefined(b, ['totalRevenueVnd', 'combinedRevenueVnd', 'TotalRevenueVnd', 'CombinedRevenueVnd'], showroom + service)) || 0);
          var bkCount = Math.max(0, Math.floor(Number(pickDefined(b, ['totalBookings', 'bookingCounts', 'bookingCount', 'TotalBookings', 'BookingCount'], 0)) || 0));
          var doneCount = Math.max(0, Math.floor(Number(pickDefined(b, ['completedBookings', 'completedBookingCounts', 'CompletedBookings'], 0)) || 0));
          return {
            month: String(pickDefined(b, ['month', 'monthLabel', 'yearMonth', 'label', 'Month', 'MonthLabel'], '')),
            showroomRevenueVnd: showroom,
            serviceRevenueVnd: service,
            totalRevenueVnd: combined,
            totalBookings: bkCount,
            completedBookings: doneCount
          };
        });
        return {
          monthLabels: normBuckets.map(function (x) { return x.month; }),
          showroomRevenueVnd: normBuckets.map(function (x) { return x.showroomRevenueVnd; }),
          serviceRevenueVnd: normBuckets.map(function (x) { return x.serviceRevenueVnd; }),
          combinedRevenueVnd: normBuckets.map(function (x) { return x.totalRevenueVnd; }),
          bookingCounts: normBuckets.map(function (x) { return x.totalBookings; }),
          completedBookingCounts: normBuckets.map(function (x) { return x.completedBookings; }),
          buckets: normBuckets
        };
      }

      var labelsArr = pickDefined(t, ['monthLabels', 'labels', 'MonthLabels', 'Labels'], []);
      var monthLabels = angular.isArray(labelsArr) ? labelsArr.map(function (x) { return String(x || ''); }) : [];
      var rawShowroom = pickDefined(t, ['showroomRevenueVnd', 'ShowroomRevenueVnd'], []);
      var rawService = pickDefined(t, ['serviceRevenueVnd', 'ServiceRevenueVnd'], []);
      var rawCombined = pickDefined(t, ['combinedRevenueVnd', 'totalRevenueVnd', 'CombinedRevenueVnd', 'TotalRevenueVnd'], null);
      var rawBookings = pickDefined(t, ['bookingCounts', 'totalBookings', 'BookingCounts', 'TotalBookings'], []);
      var rawCompleted = pickDefined(t, ['completedBookingCounts', 'completedBookings', 'CompletedBookingCounts'], []);

      var showroomSeries = monthLabels.map(function (_m, idx) {
        return angular.isArray(rawShowroom) ? Math.max(0, Number(rawShowroom[idx]) || 0) : 0;
      });
      var serviceSeries = monthLabels.map(function (_m, idx) {
        return angular.isArray(rawService) ? Math.max(0, Number(rawService[idx]) || 0) : 0;
      });
      var combinedSeries = monthLabels.map(function (_m, idx) {
        if (angular.isArray(rawCombined)) {
          return Math.max(0, Number(rawCombined[idx]) || 0);
        }
        return showroomSeries[idx] + serviceSeries[idx];
      });
      var bookingSeries = monthLabels.map(function (_m, idx) {
        return angular.isArray(rawBookings) ? Math.max(0, Math.floor(Number(rawBookings[idx]) || 0)) : 0;
      });
      var completedSeries = monthLabels.map(function (_m, idx) {
        return angular.isArray(rawCompleted) ? Math.max(0, Math.floor(Number(rawCompleted[idx]) || 0)) : 0;
      });

      var buckets = monthLabels.map(function (m, idx) {
        return {
          month: m,
          showroomRevenueVnd: showroomSeries[idx],
          serviceRevenueVnd: serviceSeries[idx],
          totalRevenueVnd: combinedSeries[idx],
          totalBookings: bookingSeries[idx],
          completedBookings: completedSeries[idx]
        };
      });

      return {
        monthLabels: monthLabels,
        showroomRevenueVnd: showroomSeries,
        serviceRevenueVnd: serviceSeries,
        combinedRevenueVnd: combinedSeries,
        bookingCounts: bookingSeries,
        completedBookingCounts: completedSeries,
        buckets: buckets
      };
    };

    self.normalizeAnalyticsLoyaltyDistribution = function (raw) {
      var l = angular.isArray(raw) ? { tiers: raw } : (raw || {});
      var byCodeLower = Object.create(null);

      var rawTiers = pickDefined(l, ['tiers', 'slices', 'items', 'distribution', 'Tiers', 'Slices', 'Items'], null);
      var rawLabels = pickDefined(l, ['tierLabels', 'TierLabels'], null);
      var rawDisplay = pickDefined(l, ['displayLabels', 'DisplayLabels'], null);
      var rawCounts = pickDefined(l, ['customerCounts', 'memberCounts', 'CustomerCounts', 'MemberCounts'], null);
      var rawPts = pickDefined(l, ['availablePointsByTier', 'AvailablePointsByTier'], null);

      if (angular.isArray(rawTiers)) {
        for (var i = 0; i < rawTiers.length; i++) {
          var tr = rawTiers[i] || {};
          var code = String(pickDefined(tr, ['tierCode', 'code', 'TierCode'], '')).trim();
          if (!code) {
            continue;
          }
          var multNum = Number(pickDefined(tr, ['pointsMultiplier', 'multiplier', 'PointsMultiplier'], 0)) || 0;
          var cnt = Math.max(0, Math.floor(Number(pickDefined(tr, ['memberCount', 'customerCount', 'count', 'MemberCount', 'CustomerCount'], 0)) || 0));
          var availPts = Math.max(0, Math.floor(Number(pickDefined(tr, ['availablePointsSum', 'availablePoints', 'AvailablePointsSum', 'AvailablePoints'], 0)) || 0));
          byCodeLower[code.toLowerCase()] = {
            tierCode: code,
            displayLabel: multNum > 0 ? (code + ' (' + multNum.toFixed(2) + 'x)') : null,
            pointsMultiplier: multNum,
            memberCount: cnt,
            availablePoints: availPts,
            pointsLiabilityVnd: Math.max(0, Number(pickDefined(tr, ['pointsLiabilityVnd', 'PointsLiabilityVnd'], availPts * 500)) || 0)
          };
        }
      } else if (angular.isArray(rawLabels)) {
        for (var j = 0; j < rawLabels.length; j++) {
          var lbl = String(rawLabels[j] || '').trim();
          var baseCode = lbl.split(/\s|\(/)[0];
          if (!baseCode) {
            continue;
          }
          var cVal = angular.isArray(rawCounts) ? Math.max(0, Math.floor(Number(rawCounts[j]) || 0)) : 0;
          var pVal = angular.isArray(rawPts) ? Math.max(0, Math.floor(Number(rawPts[j]) || 0)) : 0;
          byCodeLower[baseCode.toLowerCase()] = {
            tierCode: baseCode,
            displayLabel: angular.isArray(rawDisplay) && rawDisplay[j] ? String(rawDisplay[j]) : null,
            pointsMultiplier: 0,
            memberCount: cVal,
            availablePoints: pVal,
            pointsLiabilityVnd: pVal * 500
          };
        }
      }

      var tiers = CANONICAL_TIER_META.map(function (meta) {
        var hit = byCodeLower[meta.code.toLowerCase()] || {};
        var multNum = hit.pointsMultiplier > 0 ? hit.pointsMultiplier : meta.defaultMultNum;
        var memberCount = hit.memberCount || 0;
        var availPts = hit.availablePoints || 0;
        return {
          tierCode: meta.code,
          displayLabel: hit.displayLabel || (meta.code + ' (' + multNum.toFixed(2) + 'x)'),
          pointsMultiplier: multNum,
          memberCount: memberCount,
          availablePoints: availPts,
          pointsLiabilityVnd: hit.pointsLiabilityVnd !== undefined ? hit.pointsLiabilityVnd : (availPts * 500)
        };
      });

      var sumMembers = 0;
      var sumPoints = 0;
      for (var k = 0; k < tiers.length; k++) {
        sumMembers += tiers[k].memberCount;
        sumPoints += tiers[k].availablePoints;
      }

      var totalMembers = Math.max(0, Math.floor(Number(pickDefined(l, ['totalMembers', 'totalMembersCount', 'TotalMembers'], sumMembers)) || 0));
      var unredeemedPointsPool = Math.max(0, Math.floor(Number(pickDefined(l, ['unredeemedPointsPool', 'activePointsLiability', 'UnredeemedPointsPool'], sumPoints)) || 0));
      var pointsLiabilityVnd = Math.max(0, Number(pickDefined(l, ['pointsLiabilityVnd', 'PointsLiabilityVnd'], unredeemedPointsPool * 500)) || 0);

      for (var m = 0; m < tiers.length; m++) {
        tiers[m].memberSharePct = totalMembers > 0 ? round2((tiers[m].memberCount * 100) / totalMembers) : 0;
      }

      return {
        tierLabels: tiers.map(function (x) { return x.tierCode; }),
        displayLabels: tiers.map(function (x) { return x.displayLabel; }),
        customerCounts: tiers.map(function (x) { return x.memberCount; }),
        availablePointsByTier: tiers.map(function (x) { return x.availablePoints; }),
        totalMembers: totalMembers,
        unredeemedPointsPool: unredeemedPointsPool,
        pointsLiabilityVnd: pointsLiabilityVnd,
        tiers: tiers
      };
    };

    self.getAnalyticsOverview = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/overview', {
        params: self.buildAnalyticsParams(params)
      }).then(function (res) {
        return self.normalizeAnalyticsOverview(self.unwrapItem(res));
      });
    };

    self.getAnalyticsRevenueByDealer = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/revenue-by-dealer', {
        params: self.buildAnalyticsParams(params)
      }).then(function (res) {
        return self.normalizeAnalyticsRevenueByDealer(self.unwrapItem(res));
      });
    };

    self.getAnalyticsBookingsByDealer = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/bookings-by-dealer', {
        params: self.buildAnalyticsParams(params)
      }).then(function (res) {
        return self.normalizeAnalyticsBookingsByDealer(self.unwrapItem(res));
      });
    };

    self.getAnalyticsMonthlyTrends = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/monthly-trends', {
        params: self.buildAnalyticsParams(params)
      }).then(function (res) {
        return self.normalizeAnalyticsMonthlyTrends(self.unwrapItem(res));
      });
    };

    self.getAnalyticsLoyaltyDistribution = function (params) {
      var cleaned = params ? self.buildAnalyticsParams(params) : {};
      var cfg = Object.keys(cleaned).length > 0 ? { params: cleaned } : {};
      return $http.get(baseUrl() + '/admin/analytics/loyalty-distribution', cfg)
        .then(function (res) {
          return self.normalizeAnalyticsLoyaltyDistribution(self.unwrapItem(res));
        });
    };

    self.normalizeQualityWarrantyAnalytics = function (raw) {
      var q = raw || {};
      var rawDealers = pickDefined(q, ['dealers', 'dealerRows', 'dealerBreakdown', 'items', 'Dealers'], []);
      var dealers = angular.isArray(rawDealers)
        ? rawDealers.map(function (d) {
          var baseRow = self.normalizeDealerAnalyticsRow(d);
          var rawObj = d || {};
          var rejectedClaims = Math.max(
            0,
            Math.floor(Number(pickDefined(rawObj, ['rejectedWarrantyClaimsCount', 'rejectedClaimsCount', 'RejectedWarrantyClaimsCount'], 0)) || 0)
          );
          var totalClaims = Math.max(
            0,
            Math.floor(
              Number(
                pickDefined(
                  rawObj,
                  ['totalWarrantyClaimsCount', 'totalClaimsCount', 'TotalWarrantyClaimsCount'],
                  baseRow.openWarrantyClaimsCount + baseRow.completedWarrantyClaimsCount + rejectedClaims
                )
              ) || 0
            )
          );
          var recommendPct = round2(
            pickDefined(
              rawObj,
              ['wouldRecommendRatePercent', 'wouldRecommendPct', 'WouldRecommendRatePercent'],
              baseRow.totalServiceReviewsCount > 0 ? ((baseRow.fiveStarReviewsCount * 100) / baseRow.totalServiceReviewsCount) : 0
            )
          );
          return angular.extend({}, baseRow, {
            totalReviewsCount: baseRow.totalServiceReviewsCount,
            wouldRecommendRatePercent: recommendPct,
            rejectedWarrantyClaimsCount: rejectedClaims,
            totalWarrantyClaimsCount: totalClaims
          });
        })
        : [];

      var rawDefects = pickDefined(q, ['defectBreakdown', 'defects', 'DefectBreakdown', 'Defects'], []);
      var defectBreakdown = angular.isArray(rawDefects)
        ? rawDefects.map(function (df) {
          var item = df || {};
          var openCnt = Math.max(0, Math.floor(Number(pickDefined(item, ['openClaimsCount', 'openCount', 'OpenClaimsCount'], 0)) || 0));
          var completedCnt = Math.max(0, Math.floor(Number(pickDefined(item, ['completedClaimsCount', 'completedCount', 'CompletedClaimsCount'], 0)) || 0));
          var rejectedCnt = Math.max(0, Math.floor(Number(pickDefined(item, ['rejectedClaimsCount', 'rejectedCount', 'RejectedClaimsCount'], 0)) || 0));
          var totalCnt = Math.max(
            0,
            Math.floor(
              Number(
                pickDefined(
                  item,
                  ['totalClaimsCount', 'claimsCount', 'count', 'TotalClaimsCount', 'ClaimsCount', 'Count'],
                  openCnt + completedCnt + rejectedCnt
                )
              ) || 0
            )
          );
          var coveredVnd = Math.max(
            0,
            Number(pickDefined(item, ['approvedCoveredVnd', 'approvedWarrantyCoveredVnd', 'coveredAmountVnd', 'ApprovedCoveredVnd', 'ApprovedWarrantyCoveredVnd', 'CoveredAmountVnd'], 0)) || 0
          );
          return {
            defectCategory: String(pickDefined(item, ['defectCategory', 'category', 'DefectCategory'], 'Engine')),
            totalClaimsCount: totalCnt,
            count: totalCnt,
            openClaimsCount: openCnt,
            completedClaimsCount: completedCnt,
            rejectedClaimsCount: rejectedCnt,
            approvedCoveredVnd: coveredVnd
          };
        })
        : [];

      var sumReviews = 0;
      var sumWeightedRating = 0;
      var sumFiveStar = 0;
      var sumUnanswered = 0;
      var sumOpenClaims = 0;
      var sumCompletedClaims = 0;
      var sumRejectedClaims = 0;
      var sumTotalClaims = 0;
      var sumCoveredVnd = 0;
      for (var i = 0; i < dealers.length; i++) {
        sumReviews += dealers[i].totalServiceReviewsCount;
        sumWeightedRating += dealers[i].averageCsatRating * dealers[i].totalServiceReviewsCount;
        sumFiveStar += dealers[i].fiveStarReviewsCount;
        sumUnanswered += dealers[i].unansweredReviewsCount;
        sumOpenClaims += dealers[i].openWarrantyClaimsCount;
        sumCompletedClaims += dealers[i].completedWarrantyClaimsCount;
        sumRejectedClaims += dealers[i].rejectedWarrantyClaimsCount;
        sumTotalClaims += dealers[i].totalWarrantyClaimsCount;
        sumCoveredVnd += dealers[i].approvedWarrantyCoveredVnd;
      }

      var totalReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['totalReviewsCount', 'totalServiceReviewsCount', 'reviewsCount', 'TotalReviewsCount', 'TotalServiceReviewsCount'], sumReviews)) || 0)
      );
      var fiveStarReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['fiveStarReviewsCount', 'fiveStarCount', 'FiveStarReviewsCount'], sumFiveStar)) || 0)
      );
      var unansweredReviewsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['unansweredReviewsCount', 'unansweredCount', 'UnansweredReviewsCount'], sumUnanswered)) || 0)
      );
      var fallbackAvgCsat = sumReviews > 0 ? (sumWeightedRating / sumReviews) : 0;
      var averageCsatRating = round2(
        pickDefined(q, ['averageCsatRating', 'averageRating', 'AverageCsatRating', 'AverageRating'], fallbackAvgCsat)
      );
      var fallbackRecommendPct = totalReviewsCount > 0 ? ((fiveStarReviewsCount * 100) / totalReviewsCount) : 0;
      var wouldRecommendRatePercent = round2(
        pickDefined(q, ['wouldRecommendRatePercent', 'wouldRecommendPct', 'WouldRecommendRatePercent'], fallbackRecommendPct)
      );

      var openWarrantyClaimsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['openWarrantyClaimsCount', 'openClaimsCount', 'OpenWarrantyClaimsCount'], sumOpenClaims)) || 0)
      );
      var completedWarrantyClaimsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['completedWarrantyClaimsCount', 'completedClaimsCount', 'CompletedWarrantyClaimsCount'], sumCompletedClaims)) || 0)
      );
      var rejectedWarrantyClaimsCount = Math.max(
        0,
        Math.floor(Number(pickDefined(q, ['rejectedWarrantyClaimsCount', 'rejectedClaimsCount', 'RejectedWarrantyClaimsCount'], sumRejectedClaims)) || 0)
      );
      var totalWarrantyClaimsCount = Math.max(
        0,
        Math.floor(
          Number(
            pickDefined(
              q,
              ['totalWarrantyClaimsCount', 'totalClaimsCount', 'TotalWarrantyClaimsCount'],
              sumTotalClaims || (openWarrantyClaimsCount + completedWarrantyClaimsCount + rejectedWarrantyClaimsCount)
            )
          ) || 0
        )
      );
      var approvedWarrantyCoveredVnd = Math.max(
        0,
        Number(pickDefined(q, ['approvedWarrantyCoveredVnd', 'coveredAmountVnd', 'ApprovedWarrantyCoveredVnd'], sumCoveredVnd)) || 0
      );

      return {
        fromUtc: pickDefined(q, ['fromUtc', 'FromUtc'], null),
        toUtc: pickDefined(q, ['toUtc', 'ToUtc'], null),
        dealerId: pickDefined(q, ['dealerId', 'DealerId'], null),
        averageCsatRating: averageCsatRating,
        totalReviewsCount: totalReviewsCount,
        fiveStarReviewsCount: fiveStarReviewsCount,
        unansweredReviewsCount: unansweredReviewsCount,
        wouldRecommendRatePercent: wouldRecommendRatePercent,
        totalWarrantyClaimsCount: totalWarrantyClaimsCount,
        openWarrantyClaimsCount: openWarrantyClaimsCount,
        completedWarrantyClaimsCount: completedWarrantyClaimsCount,
        rejectedWarrantyClaimsCount: rejectedWarrantyClaimsCount,
        approvedWarrantyCoveredVnd: approvedWarrantyCoveredVnd,
        dealers: dealers,
        defectBreakdown: defectBreakdown
      };
    };

    self.getAnalyticsQualityWarranty = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/quality-warranty', {
        params: self.buildAnalyticsParams(params)
      }).then(function (res) {
        return self.normalizeQualityWarrantyAnalytics(self.unwrapItem(res));
      });
    };

    self.exportAnalyticsCsv = function (params) {
      return $http.get(baseUrl() + '/admin/analytics/export', {
        params: self.buildAnalyticsParams(params),
        responseType: 'arraybuffer'
      }).then(function (res) {
        var disposition = readResponseHeader(res, 'content-disposition');
        var contentType = readResponseHeader(res, 'content-type') || 'text/csv; charset=utf-8';
        return {
          bytes: res ? res.data : null,
          filename: self.extractCsvFilename(disposition),
          contentType: contentType
        };
      }).catch(function (rejection) {
        return $q.reject(decodeArrayBufferRejection(rejection));
      });
    };

    self.analytics = {
      buildParams: self.buildAnalyticsParams,
      getOverview: self.getAnalyticsOverview,
      getRevenueByDealer: self.getAnalyticsRevenueByDealer,
      getBookingsByDealer: self.getAnalyticsBookingsByDealer,
      getMonthlyTrends: self.getAnalyticsMonthlyTrends,
      getLoyaltyDistribution: self.getAnalyticsLoyaltyDistribution,
      getQualityWarranty: self.getAnalyticsQualityWarranty,
      exportCsv: self.exportAnalyticsCsv
    };

    // --- 13. Phase 8 (v1.1.0): Test-Drive Scheduling, OEM Spare-Parts Catalog & Printable Official Receipts ---
    var pendingTestDriveContext = null;
    var salesDeskHandoffContext = null;

    self.setPendingTestDriveContext = function (ctx) {
      pendingTestDriveContext = ctx && angular.isObject(ctx) ? angular.extend({}, ctx) : null;
    };

    self.consumePendingTestDriveContext = function () {
      var snap = pendingTestDriveContext ? angular.extend({}, pendingTestDriveContext) : null;
      pendingTestDriveContext = null;
      return snap;
    };

    self.setSalesDeskHandoffContext = function (ctx) {
      salesDeskHandoffContext = ctx && angular.isObject(ctx) ? angular.extend({}, ctx) : null;
    };

    self.consumeSalesDeskHandoffContext = function () {
      var snap = salesDeskHandoffContext ? angular.extend({}, salesDeskHandoffContext) : null;
      salesDeskHandoffContext = null;
      return snap;
    };

    self.normalizeTestDrive = function (raw) {
      var td = raw || {};
      var resolvedId = String(pickDefined(td, ['testDriveId', 'id', 'TestDriveId', 'Id'], ''));
      var startHm = formatShortTime(pickDefined(td, ['startTime', 'StartTime'], '09:00'), '09:00');
      var endHm = formatShortTime(pickDefined(td, ['endTime', 'EndTime'], '09:45'), '09:45');
      var assignedInvId = pickDefined(td, ['assignedInventoryId', 'AssignedInventoryId'], null);
      var rawVin = pickDefined(td, ['assignedVin', 'vin', 'AssignedVin', 'Vin'], null);
      var normVin = rawVin ? String(rawVin).trim().toUpperCase() : null;

      var resolvedModelCode = String(
        pickDefined(td, ['motorcycleModelCode', 'modelCode', 'MotorcycleModelCode', 'ModelCode'], '')
      );
      var assignedColor = pickDefined(td, ['assignedColorName', 'colorName', 'AssignedColorName', 'ColorName'], null);

      return {
        id: resolvedId,
        testDriveId: resolvedId,
        bookingNumber: String(pickDefined(td, ['bookingNumber', 'BookingNumber'], '')),
        status: String(pickDefined(td, ['status', 'newStatus', 'Status', 'NewStatus'], 'Requested')),
        previousStatus: pickDefined(td, ['previousStatus', 'PreviousStatus'], null),
        dealerId: String(pickDefined(td, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(td, ['dealerCode', 'DealerCode'], '')).toUpperCase(),
        dealerName: String(pickDefined(td, ['dealerName', 'DealerName'], '')),
        dealerCity: String(pickDefined(td, ['dealerCity', 'DealerCity'], '')),
        customerId: String(pickDefined(td, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(td, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(td, ['customerEmail', 'CustomerEmail'], '')),
        contactPhone: String(pickDefined(td, ['contactPhone', 'customerPhone', 'phoneNumber', 'ContactPhone'], '')),
        motorcycleModelId: String(pickDefined(td, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        modelCode: resolvedModelCode,
        motorcycleModelCode: resolvedModelCode,
        motorcycleModelName: String(pickDefined(td, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        imageUrl: String(pickDefined(td, ['imageUrl', 'ImageUrl', 'motorcycleModelImageUrl', 'modelImageUrl', 'motorcycleImageUrl', 'MotorcycleModelImageUrl', 'ModelImageUrl'], '') || '').trim(),
        assignedInventoryId: assignedInvId ? String(assignedInvId) : null,
        assignedVin: normVin,
        assignedColorName: assignedColor ? String(assignedColor) : null,
        preferredDate: formatDateOnly(pickDefined(td, ['preferredDate', 'slotDate', 'PreferredDate'], '')),
        startTime: startHm,
        endTime: endHm,
        timeWindow: startHm + ' - ' + endHm,
        driverLicenseNumber: String(pickDefined(td, ['driverLicenseNumber', 'DriverLicenseNumber'], '')).toUpperCase(),
        customerNotes: pickDefined(td, ['customerNotes', 'CustomerNotes'], null),
        staffNotes: pickDefined(td, ['staffNotes', 'StaffNotes'], null),
        cancellationReason: pickDefined(td, ['cancellationReason', 'reason', 'CancellationReason'], null),
        confirmedAtUtc: pickDefined(td, ['confirmedAtUtc', 'ConfirmedAtUtc'], null),
        completedAtUtc: pickDefined(td, ['completedAtUtc', 'CompletedAtUtc'], null),
        cancelledAtUtc: pickDefined(td, ['cancelledAtUtc', 'CancelledAtUtc'], null),
        createdAtUtc: pickDefined(td, ['createdAtUtc', 'occurredAtUtc', 'CreatedAtUtc', 'OccurredAtUtc'], null),
        updatedAtUtc: pickDefined(td, ['updatedAtUtc', 'UpdatedAtUtc'], null),
        rowVersion: String(pickDefined(td, ['rowVersion', 'RowVersion'], '')),
        isLiveArrival: Boolean(td.isLiveArrival)
      };
    };

    self.getMyTestDrives = function (params) {
      return $http.get(baseUrl() + '/test-drives/my', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeTestDrive);
      });
    };

    self.getDealerTestDrives = function (params) {
      return $http.get(baseUrl() + '/test-drives', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeTestDrive);
      });
    };

    self.getTestDriveById = function (id) {
      return $http.get(baseUrl() + '/test-drives/' + encodeURIComponent(id))
        .then(function (res) {
          return self.normalizeTestDrive(self.unwrapItem(res));
        });
    };

    self.createTestDrive = function (payload, idempotencyKey) {
      var p = payload || {};
      var key = idempotencyKey || self.generateIdempotencyKey('test-drive');
      var body = {
        dealerId: p.dealerId,
        motorcycleModelId: p.motorcycleModelId || p.modelId,
        preferredDate: formatDateOnly(p.preferredDate),
        startTime: formatShortTime(p.startTime, '09:00'),
        endTime: formatShortTime(p.endTime, '09:45'),
        driverLicenseNumber: String(p.driverLicenseNumber || '').trim().toUpperCase(),
        contactPhone: String(p.contactPhone || '').trim(),
        customerNotes: p.customerNotes ? String(p.customerNotes).trim() : null
      };
      return $http.post(baseUrl() + '/test-drives', body, {
        headers: { 'Idempotency-Key': key }
      }).then(function (res) {
        return self.normalizeTestDrive(self.unwrapItem(res));
      });
    };

    self.cancelTestDrive = function (id, payload) {
      var p = payload || {};
      var reasonText = String(p.reason || p.cancellationReason || 'Cancelled by customer').trim();
      var body = {
        reason: reasonText,
        cancellationReason: reasonText,
        rowVersion: p.rowVersion || null
      };
      return $http.post(baseUrl() + '/test-drives/' + encodeURIComponent(id) + '/cancel', body)
        .then(function (res) {
          return self.normalizeTestDrive(self.unwrapItem(res));
        });
    };

    self.updateTestDriveStatus = function (id, payload) {
      var p = payload || {};
      var nextStatus = String(p.newStatus || p.status || '').trim();
      var staffNotes = p.staffNotes !== undefined ? p.staffNotes : p.notes;
      var cancelReason = p.cancellationReason !== undefined ? p.cancellationReason : p.reason;
      var body = {
        newStatus: nextStatus,
        status: nextStatus,
        assignedInventoryId: p.assignedInventoryId ? String(p.assignedInventoryId).trim() : null,
        staffNotes: staffNotes ? String(staffNotes).trim() : null,
        notes: staffNotes ? String(staffNotes).trim() : null,
        cancellationReason: cancelReason ? String(cancelReason).trim() : null,
        reason: cancelReason ? String(cancelReason).trim() : null,
        rowVersion: p.rowVersion || null
      };
      return $http.patch(baseUrl() + '/test-drives/' + encodeURIComponent(id) + '/status', body)
        .then(function (res) {
          return self.normalizeTestDrive(self.unwrapItem(res));
        });
    };

    self.testDrives = {
      listMine: self.getMyTestDrives,
      listDealer: self.getDealerTestDrives,
      getById: self.getTestDriveById,
      create: self.createTestDrive,
      cancel: self.cancelTestDrive,
      updateStatus: self.updateTestDriveStatus
    };

    // --- OEM Spare-Parts Catalog (/api/v1/spare-parts) ---
    var CANONICAL_SPARE_PART_CATEGORIES = Object.freeze([
      Object.freeze({ value: 'EngineOil', label: 'EngineOil (Lubricants & Fluids)' }),
      Object.freeze({ value: 'Filter', label: 'Filter (Air & Oil Elements)' }),
      Object.freeze({ value: 'Brakes', label: 'Brakes (Pads, Rotors & ABS)' }),
      Object.freeze({ value: 'Transmission', label: 'Transmission (Chains, Sprockets & CVT)' }),
      Object.freeze({ value: 'Tires', label: 'Tires (Tubeless OEM Tires)' }),
      Object.freeze({ value: 'Electrical', label: 'Electrical (Spark Plugs, Batteries & Sensors)' }),
      Object.freeze({ value: 'Accessory', label: 'Accessory (Genuine Touring & Protection)' })
    ]);

    self.getSparePartCategories = function () {
      return CANONICAL_SPARE_PART_CATEGORIES.map(function (cat) {
        return angular.extend({}, cat);
      });
    };

    self.normalizeSparePart = function (raw) {
      var sp = raw || {};
      var resolvedId = String(pickDefined(sp, ['id', 'sparePartId', 'partId', 'Id', 'SparePartId'], ''));
      var compatId = pickDefined(sp, ['compatibleModelId', 'motorcycleModelId', 'CompatibleModelId'], null);
      var compatName = pickDefined(sp, ['compatibleModelName', 'motorcycleModelName', 'modelName', 'CompatibleModelName'], null);
      var compatCode = pickDefined(sp, ['compatibleModelCode', 'modelCode', 'CompatibleModelCode'], null);
      var universalFlag = !compatId && pickDefined(sp, ['isUniversalFit', 'IsUniversalFit'], true) !== false;
      return {
        id: resolvedId,
        partId: resolvedId,
        sparePartId: resolvedId,
        partCode: String(pickDefined(sp, ['partCode', 'code', 'PartCode'], '')).toUpperCase(),
        name: String(pickDefined(sp, ['name', 'partName', 'Name'], '')),
        category: String(pickDefined(sp, ['category', 'Category'], 'EngineOil')),
        compatibleModelId: compatId ? String(compatId) : null,
        compatibleModelCode: compatCode ? String(compatCode) : null,
        compatibleModelName: compatName ? String(compatName) : null,
        isUniversal: universalFlag,
        isUniversalFit: universalFlag,
        unitLabel: String(pickDefined(sp, ['unitLabel', 'unit', 'UnitLabel'], 'Unit')),
        unitPriceVnd: Math.max(0, Number(pickDefined(sp, ['unitPriceVnd', 'unitPrice', 'priceVnd', 'UnitPriceVnd'], 0)) || 0),
        isActive: pickDefined(sp, ['isActive', 'IsActive'], true) !== false
      };
    };

    self.getSpareParts = function (params) {
      return $http.get(baseUrl() + '/spare-parts', {
        params: self.cleanParams(params)
      }).then(function (res) {
        return self.unwrapList(res).map(self.normalizeSparePart);
      });
    };

    self.getSparePartById = function (partId) {
      return $http.get(baseUrl() + '/spare-parts/' + encodeURIComponent(partId))
        .then(function (res) {
          return self.normalizeSparePart(self.unwrapItem(res));
        });
    };

    self.createSparePart = function (payload) {
      var p = payload || {};
      var body = {
        partCode: String(p.partCode || '').trim().toUpperCase(),
        name: String(p.name || '').trim(),
        category: p.category || 'EngineOil',
        compatibleModelId: p.compatibleModelId ? String(p.compatibleModelId).trim() : null,
        unitLabel: String(p.unitLabel || 'Unit').trim(),
        unitPriceVnd: Math.max(0, Number(p.unitPriceVnd) || 0),
        isActive: p.isActive !== false
      };
      return $http.post(baseUrl() + '/spare-parts', body)
        .then(function (res) {
          return self.normalizeSparePart(self.unwrapItem(res));
        });
    };

    self.updateSparePart = function (partId, payload) {
      var p = payload || {};
      var body = {
        partCode: String(p.partCode || '').trim().toUpperCase(),
        name: String(p.name || '').trim(),
        category: p.category || 'EngineOil',
        compatibleModelId: p.compatibleModelId ? String(p.compatibleModelId).trim() : null,
        unitLabel: String(p.unitLabel || 'Unit').trim(),
        unitPriceVnd: Math.max(0, Number(p.unitPriceVnd) || 0),
        isActive: p.isActive !== false
      };
      return $http.put(baseUrl() + '/spare-parts/' + encodeURIComponent(partId), body)
        .then(function (res) {
          return self.normalizeSparePart(self.unwrapItem(res));
        });
    };

    self.spareParts = {
      getCategories: self.getSparePartCategories,
      list: self.getSpareParts,
      getById: self.getSparePartById,
      create: self.createSparePart,
      update: self.updateSparePart
    };

    // --- Official Printable Dealership Tax & Loyalty Receipts ---
    function computeFallbackVerificationHash(seedStr) {
      var str = String(seedStr || 'MDLMP-RECEIPT');
      var h1 = 0x811c9dc5;
      var h2 = 0x9e3779b9;
      for (var i = 0; i < str.length; i++) {
        var ch = str.charCodeAt(i);
        h1 ^= ch;
        h1 = Math.imul ? Math.imul(h1, 0x01000193) : ((h1 * 16777619) | 0);
        h2 ^= (ch + i);
        h2 = Math.imul ? Math.imul(h2, 0x85ebca6b) : ((h2 * 2246822507) | 0);
      }
      var hex1 = ('00000000' + (h1 >>> 0).toString(16)).slice(-8);
      var hex2 = ('00000000' + (h2 >>> 0).toString(16)).slice(-8);
      return (hex1 + hex2).toUpperCase();
    }

    self.normalizePrintableReceipt = function (raw, fallbackKind) {
      var r = raw || {};
      var rawRcptNum = String(pickDefined(r, ['receiptNumber', 'ReceiptNumber'], ''));
      var docInvoiceNum = pickDefined(r, ['invoiceNumber', 'InvoiceNumber'], null);
      var docOrderNum = pickDefined(r, ['orderNumber', 'OrderNumber'], null);
      var rawReceiptType = String(pickDefined(r, ['receiptType', 'ReceiptType'], ''));
      var kind = fallbackKind ||
        pickDefined(r, ['receiptKind', 'ReceiptKind'], null) ||
        (rawReceiptType && rawReceiptType.toLowerCase().indexOf('sale') !== -1 ? 'SalesOrder' : null) ||
        (rawRcptNum.indexOf('RCP-SLS-') === 0 || docOrderNum ? 'SalesOrder' : 'ServiceInvoice');

      var docNumber = String(
        docInvoiceNum ||
        docOrderNum ||
        rawRcptNum.replace(/^RCP-(SVC|SLS)-/, '') ||
        'DOC-0001'
      );
      var receiptNumber = rawRcptNum || ((kind === 'SalesOrder' ? 'RCP-SLS-' : 'RCP-SVC-') + docNumber);

      var dBlock = (r.dealer && angular.isObject(r.dealer)) ? r.dealer : ((r.Dealer && angular.isObject(r.Dealer)) ? r.Dealer : {});
      var dealer = {
        code: String(pickDefined(dBlock, ['code', 'dealerCode', 'Code', 'DealerCode'], pickDefined(r, ['dealerCode', 'DealerCode'], ''))).toUpperCase(),
        name: String(pickDefined(dBlock, ['name', 'dealerName', 'Name', 'DealerName'], pickDefined(r, ['dealerName', 'DealerName'], ''))),
        address: String(pickDefined(dBlock, ['address', 'Address'], pickDefined(r, ['dealerAddress', 'DealerAddress'], ''))),
        city: String(pickDefined(dBlock, ['city', 'City'], pickDefined(r, ['dealerCity', 'DealerCity'], ''))),
        phone: String(pickDefined(dBlock, ['phone', 'phoneNumber', 'Phone', 'PhoneNumber'], pickDefined(r, ['dealerPhone', 'DealerPhone'], '')))
      };

      var isVi = isVietnameseActive();
      var cBlock = (r.customer && angular.isObject(r.customer)) ? r.customer : ((r.Customer && angular.isObject(r.Customer)) ? r.Customer : {});
      var customer = {
        fullName: String(pickDefined(cBlock, ['fullName', 'name', 'FullName', 'Name'], pickDefined(r, ['customerFullName', 'CustomerFullName'], isVi ? 'Khách hàng thân thiết' : 'Valued Customer'))),
        email: String(pickDefined(cBlock, ['email', 'Email'], pickDefined(r, ['customerEmail', 'CustomerEmail'], ''))),
        phone: String(pickDefined(cBlock, ['phone', 'phoneNumber', 'Phone', 'PhoneNumber'], pickDefined(r, ['customerPhone', 'CustomerPhone'], '')))
      };

      var vBlock = (r.vehicle && angular.isObject(r.vehicle)) ? r.vehicle : ((r.Vehicle && angular.isObject(r.Vehicle)) ? r.Vehicle : {});
      var vehicle = {
        modelCode: String(pickDefined(vBlock, ['modelCode', 'ModelCode'], pickDefined(r, ['modelCode', 'ModelCode'], ''))),
        modelName: String(pickDefined(vBlock, ['modelName', 'motorcycleModelName', 'ModelName', 'MotorcycleModelName'], pickDefined(r, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], isVi ? 'Xe máy chính hãng' : 'Authorized Motorcycle'))),
        colorName: String(pickDefined(vBlock, ['colorName', 'ColorName'], pickDefined(r, ['colorName', 'ColorName'], ''))),
        vin: String(pickDefined(vBlock, ['vin', 'VIN', 'Vin'], pickDefined(r, ['vin', 'VIN'], ''))).toUpperCase(),
        engineNumber: String(pickDefined(vBlock, ['engineNumber', 'EngineNumber'], pickDefined(r, ['engineNumber', 'EngineNumber'], ''))),
        licensePlate: pickDefined(vBlock, ['licensePlate', 'LicensePlate'], pickDefined(r, ['licensePlate', 'LicensePlate'], null)),
        odometerAtBookingKm: pickDefined(vBlock, ['odometerAtBookingKm', 'OdometerAtBookingKm'], pickDefined(r, ['odometerAtBookingKm', 'OdometerAtBookingKm'], null)),
        odometerAtCompletionKm: pickDefined(vBlock, ['odometerAtCompletionKm', 'OdometerAtCompletionKm'], pickDefined(r, ['odometerAtCompletionKm', 'OdometerAtCompletionKm'], null)),
        nextDueMileageKm: pickDefined(
          vBlock,
          ['nextDueMileageKm', 'initialNextDueMileageKm', 'NextDueMileageKm', 'InitialNextDueMileageKm'],
          pickDefined(r, ['nextDueMileageKm', 'initialNextDueMileageKm', 'NextDueMileageKm', 'InitialNextDueMileageKm'], null)
        ),
        nextDueDateUtc: pickDefined(
          vBlock,
          ['nextDueDateUtc', 'initialNextDueDateUtc', 'NextDueDateUtc', 'InitialNextDueDateUtc'],
          pickDefined(r, ['nextDueDateUtc', 'initialNextDueDateUtc', 'NextDueDateUtc', 'InitialNextDueDateUtc'], null)
        )
      };

      var laborAmt = Math.max(0, Number(pickDefined(r, ['laborAmount', 'LaborAmount'], 0)) || 0);
      var partsAmt = Math.max(0, Number(pickDefined(r, ['partsAmount', 'PartsAmount'], 0)) || 0);
      var unitPrice = Math.max(0, Number(pickDefined(r, ['unitPrice', 'totalAmount', 'UnitPrice', 'TotalAmount'], 0)) || 0);
      var grossAmt = kind === 'SalesOrder'
        ? Math.max(0, Number(pickDefined(r, ['unitPrice', 'totalAmount', 'grossAmount', 'UnitPrice', 'TotalAmount', 'GrossAmount'], unitPrice)) || 0)
        : Math.max(0, Number(pickDefined(r, ['grossAmount', 'GrossAmount'], laborAmt + partsAmt)) || 0);
      var discAmt = Math.max(
        0,
        Number(pickDefined(r, ['voucherDiscountAmount', 'discountAmount', 'VoucherDiscountAmount', 'DiscountAmount'], 0)) || 0
      );
      var netPaidAmt = kind === 'SalesOrder'
        ? Math.max(0, Number(pickDefined(r, ['finalAmount', 'netPaidAmount', 'FinalAmount', 'NetPaidAmount'], Math.max(0, grossAmt - discAmt))) || 0)
        : Math.max(0, Number(pickDefined(r, ['netPaidAmount', 'NetPaidAmount'], Math.max(0, grossAmt - discAmt))) || 0);

      var rawLines = pickDefined(r, ['lineItems', 'LineItems'], null);
      var lineItems = [];
      if (angular.isArray(rawLines) && rawLines.length > 0) {
        lineItems = rawLines.map(function (li, idx) {
          return self.normalizeInvoiceLineItem(li, idx + 1);
        });
      } else if (kind === 'SalesOrder') {
        lineItems = [
          self.normalizeInvoiceLineItem({
            lineNumber: 1,
            partCodeSnapshot: vehicle.modelCode || 'SHOWROOM-VIN',
            description: vehicle.modelName + (vehicle.colorName ? (' (' + vehicle.colorName + ')') : '') + (vehicle.vin ? (' • VIN ' + vehicle.vin) : ''),
            itemType: 'Motorcycle',
            quantity: 1,
            unitPriceVnd: grossAmt,
            lineTotalVnd: grossAmt
          }, 1)
        ];
      } else {
        if (laborAmt > 0) {
          lineItems.push(self.normalizeInvoiceLineItem({
            lineNumber: lineItems.length + 1,
            partCodeSnapshot: 'SVC-LABOR',
            description: isVi
              ? 'Công bảo dưỡng xưởng & Kiểm tra đa điểm chính hãng'
              : 'Authorized Workshop Labor & Multi-Point Inspection',
            itemType: 'Labor',
            quantity: 1,
            unitPriceVnd: laborAmt,
            lineTotalVnd: laborAmt
          }, lineItems.length + 1));
        }
        if (partsAmt > 0) {
          lineItems.push(self.normalizeInvoiceLineItem({
            lineNumber: lineItems.length + 1,
            partCodeSnapshot: 'SVC-PARTS',
            description: isVi
              ? 'Phụ tùng chính hãng OEM & Vật tư xưởng dịch vụ'
              : 'Genuine OEM Parts & Workshop Consumables',
            itemType: 'Part',
            quantity: 1,
            unitPriceVnd: partsAmt,
            lineTotalVnd: partsAmt
          }, lineItems.length + 1));
        }
      }

      var lBlock = (r.loyalty && angular.isObject(r.loyalty)) ? r.loyalty : ((r.Loyalty && angular.isObject(r.Loyalty)) ? r.Loyalty : {});
      var pointsEarned = Math.max(
        0,
        Math.floor(Number(pickDefined(lBlock, ['pointsEarned', 'PointsEarned'], pickDefined(r, ['pointsEarned', 'PointsEarned'], 0))) || 0)
      );
      var tierCodeAfter = String(
        pickDefined(
          lBlock,
          ['tierCodeAfter', 'newTierCode', 'currentTierCode', 'TierCodeAfter', 'NewTierCode'],
          pickDefined(r, ['newTierCode', 'tierCodeAfter', 'appliedTierCode', 'NewTierCode'], 'Bronze')
        )
      );
      var multApplied = Number(
        pickDefined(
          lBlock,
          ['pointsMultiplierApplied', 'appliedMultiplier', 'pointsMultiplier', 'PointsMultiplierApplied', 'AppliedMultiplier'],
          pickDefined(r, ['appliedMultiplier', 'pointsMultiplierApplied', 'AppliedMultiplier'], 1.0)
        )
      ) || 1.0;

      var loyalty = {
        pointsEarned: pointsEarned,
        tierCodeAfter: tierCodeAfter,
        pointsMultiplierApplied: multApplied,
        availablePointsAfter: pickDefined(
          lBlock,
          ['availablePointsAfter', 'AvailablePointsAfter'],
          pickDefined(r, ['availablePointsAfter', 'AvailablePointsAfter'], null)
        ),
        lifetimeQualifyingPointsAfter: pickDefined(
          lBlock,
          ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'],
          pickDefined(r, ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'], null)
        ),
        tierUpgraded: Boolean(pickDefined(lBlock, ['tierUpgraded', 'TierUpgraded'], pickDefined(r, ['tierUpgraded', 'TierUpgraded'], false)))
      };

      var verificationCode = String(
        pickDefined(
          r,
          ['verificationCode', 'sha256Hash', 'VerificationCode'],
          computeFallbackVerificationHash(receiptNumber + ':' + docNumber + ':' + netPaidAmt)
        )
      );

      return {
        receiptKind: kind,
        receiptType: rawReceiptType || kind,
        receiptNumber: receiptNumber,
        documentNumber: docNumber,
        invoiceNumber: kind === 'ServiceInvoice' ? docNumber : null,
        orderNumber: kind === 'SalesOrder' ? docNumber : null,
        bookingNumber: pickDefined(r, ['bookingNumber', 'BookingNumber'], null),
        serviceType: pickDefined(r, ['serviceType', 'ServiceType'], null),
        issuedAtUtc: pickDefined(r, ['issuedAtUtc', 'paidAtUtc', 'completedAtUtc', 'createdAtUtc', 'IssuedAtUtc', 'PaidAtUtc', 'CompletedAtUtc'], new Date().toISOString()),
        issuedByStaffName: pickDefined(r, ['issuedByStaffName', 'soldByStaffName', 'IssuedByStaffName', 'SoldByStaffName'], null),
        verificationCode: verificationCode,
        paymentMethod: String(pickDefined(r, ['paymentMethod', 'PaymentMethod'], 'BankTransfer')),
        dealer: dealer,
        customer: customer,
        vehicle: vehicle,
        lineItems: lineItems,
        laborAmount: laborAmt,
        partsAmount: partsAmt,
        unitPrice: unitPrice || grossAmt,
        grossAmount: grossAmt,
        appliedVoucherCode: pickDefined(r, ['appliedVoucherCode', 'voucherCode', 'AppliedVoucherCode', 'VoucherCode'], null),
        discountAmount: discAmt,
        voucherDiscountAmount: discAmt,
        netPaidAmount: netPaidAmt,
        finalAmount: netPaidAmt,
        loyalty: loyalty,
        notes: pickDefined(r, ['workSummaryNotes', 'notes', 'WorkSummaryNotes', 'Notes'], null)
      };
    };

    self.getServiceInvoiceReceipt = function (invoiceId) {
      return $http.get(baseUrl() + '/service-invoices/' + encodeURIComponent(invoiceId) + '/receipt')
        .then(function (res) {
          return self.normalizePrintableReceipt(self.unwrapItem(res), 'ServiceInvoice');
        });
    };

    self.getSalesOrderReceipt = function (orderId) {
      return $http.get(baseUrl() + '/sales-orders/' + encodeURIComponent(orderId) + '/receipt')
        .then(function (res) {
          return self.normalizePrintableReceipt(self.unwrapItem(res), 'SalesOrder');
        });
    };

    self.serviceInvoices.getReceipt = self.getServiceInvoiceReceipt;

    // --- 14. Phase 9 (v1.2.0): Post-Service 5-Star CSAT Reviews & OEM VIN Warranty Claims ---
    var CANONICAL_REVIEW_QUALITY_TAGS = Object.freeze([
      Object.freeze({ code: 'FastTurnaround', label: 'Fast Turnaround', icon: 'lightning' }),
      Object.freeze({ code: 'TransparentPricing', label: 'Transparent Pricing', icon: 'currency-circle-dollar' }),
      Object.freeze({ code: 'ExpertTechnician', label: 'Expert Technician', icon: 'wrench' }),
      Object.freeze({ code: 'CleanLounge', label: 'Clean Lounge', icon: 'coffee' }),
      Object.freeze({ code: 'GenuineParts', label: 'Genuine OEM Parts', icon: 'shield-check' }),
      Object.freeze({ code: 'FriendlyAdvisor', label: 'Friendly Advisor', icon: 'handshake' }),
      Object.freeze({ code: 'ClearExplanation', label: 'Clear Explanation', icon: 'clipboard-text' }),
      Object.freeze({ code: 'ThoroughInspection', label: 'Thorough Inspection', icon: 'magnifying-glass' })
    ]);

    var CANONICAL_WARRANTY_DEFECT_CATEGORIES = Object.freeze([
      Object.freeze({ value: 'Engine', label: 'Engine (Cylinder, Valve Train & Cooling)' }),
      Object.freeze({ value: 'Transmission', label: 'Transmission (Gearbox, Clutch & CVT Drive)' }),
      Object.freeze({ value: 'Electrical', label: 'Electrical (ECU, Stator, ABS Sensor & Harness)' }),
      Object.freeze({ value: 'BrakesAndSuspension', label: 'BrakesAndSuspension (Calipers, Forks & Monoshock)' }),
      Object.freeze({ value: 'FrameAndPaint', label: 'FrameAndPaint (Chassis Welds & Factory Paint)' }),
      Object.freeze({ value: 'ExhaustAndFuel', label: 'ExhaustAndFuel (PGM-FI Injector, Fuel Pump & Exhaust)' })
    ]);

    self.getReviewQualityTags = function () {
      return CANONICAL_REVIEW_QUALITY_TAGS.map(function (t) {
        return angular.extend({}, t);
      });
    };

    self.getWarrantyDefectCategories = function () {
      return CANONICAL_WARRANTY_DEFECT_CATEGORIES.map(function (c) {
        return angular.extend({}, c);
      });
    };

    var OPEN_WARRANTY_STATUS_MAP = Object.freeze({
      Submitted: true,
      UnderReview: true,
      Approved: true
    });

    self.formatReviewStars = function (rating) {
      var parsed = (rating === undefined || rating === null || rating === '') ? NaN : Number(rating);
      var clamped = isFinite(parsed) ? Math.max(1, Math.min(5, Math.floor(parsed))) : 5;
      var out = '';
      for (var i = 0; i < clamped; i++) { out += '\u2605'; }
      for (var j = clamped; j < 5; j++) { out += '\u2606'; }
      return out;
    };

    self.isOpenWarrantyStatus = function (status) {
      if (!status) {
        return false;
      }
      var key = String(status).trim();
      return OPEN_WARRANTY_STATUS_MAP[key] === true;
    };

    function parseQualityTagsList(rawTags) {
      if (angular.isArray(rawTags)) {
        var cleanedArr = [];
        var seenArr = Object.create(null);
        for (var i = 0; i < rawTags.length; i++) {
          var token = String(rawTags[i] || '').trim();
          if (token && !seenArr[token]) {
            seenArr[token] = true;
            cleanedArr.push(token);
          }
        }
        return cleanedArr;
      }
      if (angular.isString(rawTags) && rawTags.trim()) {
        var str = rawTags.trim();
        if (str.charAt(0) === '[') {
          try {
            var parsed = JSON.parse(str);
            if (angular.isArray(parsed)) {
              return parseQualityTagsList(parsed);
            }
          } catch (e) {
            // Fall through to comma-delimited parser
          }
        }
        var parts = str.split(/[,;|]/);
        var out = [];
        var seen = Object.create(null);
        for (var j = 0; j < parts.length; j++) {
          var t = String(parts[j] || '').trim();
          if (t && !seen[t]) {
            seen[t] = true;
            out.push(t);
          }
        }
        return out;
      }
      return [];
    }

    self.parseQualityTagsList = parseQualityTagsList;

    self.normalizeServiceReview = function (raw) {
      var r = raw || {};
      var resolvedId = String(pickDefined(r, ['id', 'reviewId', 'serviceReviewId', 'Id', 'ReviewId'], ''));
      var rawRating = pickDefined(r, ['rating', 'Rating'], 5);
      var rating = Math.max(1, Math.min(5, Math.floor(Number(rawRating) || 5)));
      var tagsList = parseQualityTagsList(
        pickDefined(r, ['qualityTagList', 'qualityTags', 'tags', 'QualityTagList', 'QualityTags', 'Tags'], [])
      );
      var rawStaffReply = pickDefined(r, ['staffResponse', 'StaffResponse'], null);
      var staffResponse = rawStaffReply && String(rawStaffReply).trim() ? String(rawStaffReply).trim() : null;
      var isAnswered = Boolean(pickDefined(r, ['isAnswered', 'IsAnswered'], Boolean(staffResponse)));
      var respondedByName = pickDefined(
        r,
        ['respondedByStaffName', 'respondedByName', 'respondedByFullName', 'RespondedByStaffName', 'RespondedByName', 'RespondedByFullName'],
        null
      );
      var rawBonus = pickDefined(r, ['bonusPointsAwarded', 'pointsEarned', 'BonusPointsAwarded'], 25);
      var bonusPts = Math.max(0, Math.floor(Number(rawBonus !== null && rawBonus !== undefined ? rawBonus : 25)));

      return {
        id: resolvedId,
        reviewId: resolvedId,
        reviewNumber: String(pickDefined(r, ['reviewNumber', 'ReviewNumber'], '')),
        serviceInvoiceId: String(pickDefined(r, ['serviceInvoiceId', 'invoiceId', 'ServiceInvoiceId', 'InvoiceId'], '')),
        invoiceNumber: String(pickDefined(r, ['invoiceNumber', 'InvoiceNumber'], '')),
        serviceBookingId: String(pickDefined(r, ['serviceBookingId', 'bookingId', 'ServiceBookingId', 'BookingId'], '')),
        bookingNumber: String(pickDefined(r, ['bookingNumber', 'BookingNumber'], '')),
        serviceType: String(pickDefined(r, ['serviceType', 'ServiceType'], '')),
        dealerId: String(pickDefined(r, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(r, ['dealerCode', 'DealerCode'], '')).toUpperCase(),
        dealerName: String(pickDefined(r, ['dealerName', 'DealerName'], '')),
        dealerCity: String(pickDefined(r, ['dealerCity', 'DealerCity'], '')),
        customerId: String(pickDefined(r, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(r, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(r, ['customerEmail', 'CustomerEmail'], '')),
        customerVehicleId: String(pickDefined(r, ['customerVehicleId', 'vehicleId', 'CustomerVehicleId'], '')),
        motorcycleModelId: String(pickDefined(r, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        vin: String(pickDefined(r, ['vin', 'vinSnapshot', 'VIN', 'Vin'], '')).toUpperCase(),
        licensePlate: pickDefined(r, ['licensePlate', 'LicensePlate'], null),
        motorcycleModelName: String(pickDefined(r, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        rating: rating,
        qualityTags: tagsList,
        qualityTagList: tagsList.slice(),
        qualityTagsCsv: tagsList.join(','),
        comment: pickDefined(r, ['comment', 'Comment'], null),
        bonusPointsAwarded: isNaN(bonusPts) ? 25 : bonusPts,
        availablePointsAfter: pickDefined(r, ['availablePointsAfter', 'AvailablePointsAfter'], null),
        lifetimeQualifyingPointsAfter: pickDefined(r, ['lifetimeQualifyingPointsAfter', 'LifetimeQualifyingPointsAfter'], null),
        staffResponse: staffResponse,
        hasStaffResponse: isAnswered || Boolean(staffResponse),
        isAnswered: isAnswered || Boolean(staffResponse),
        respondedByUserId: pickDefined(r, ['respondedByUserId', 'RespondedByUserId'], null),
        respondedByName: respondedByName,
        respondedByStaffName: respondedByName,
        respondedAtUtc: pickDefined(r, ['respondedAtUtc', 'RespondedAtUtc'], null),
        createdAtUtc: pickDefined(r, ['createdAtUtc', 'occurredAtUtc', 'CreatedAtUtc', 'OccurredAtUtc'], null),
        updatedAtUtc: pickDefined(r, ['updatedAtUtc', 'UpdatedAtUtc'], null),
        rowVersion: String(pickDefined(r, ['rowVersion', 'RowVersion'], '')),
        isLiveArrival: Boolean(r.isLiveArrival)
      };
    };

    function attachNonEnumerableMeta(targetArray, metaObj) {
      var keys = Object.keys(metaObj || {});
      for (var i = 0; i < keys.length; i++) {
        var k = keys[i];
        Object.defineProperty(targetArray, k, {
          value: metaObj[k],
          writable: true,
          configurable: true,
          enumerable: false
        });
      }
      return targetArray;
    }

    self.unwrapServiceReviewCollection = function (responseOrBody) {
      var rawEnvelope = responseOrBody && responseOrBody.status !== undefined && responseOrBody.data !== undefined
        ? responseOrBody.data
        : responseOrBody;
      var dataLayer = self.unwrapItem(responseOrBody);
      var paged = self.unwrapPaged(responseOrBody);
      var rawList = paged.items;
      if ((!rawList || rawList.length === 0) && dataLayer && angular.isObject(dataLayer)) {
        var altItems = pickDefined(dataLayer, ['reviews', 'Reviews', 'items', 'Items'], null);
        if (angular.isArray(altItems)) {
          rawList = altItems;
        }
      }
      var normalized = (rawList || []).map(self.normalizeServiceReview);

      var computedSum = 0;
      var computedFiveStar = 0;
      var computedUnanswered = 0;
      var computedBonus = 0;
      for (var i = 0; i < normalized.length; i++) {
        computedSum += normalized[i].rating;
        if (normalized[i].rating === 5) {
          computedFiveStar++;
        }
        if (!normalized[i].hasStaffResponse) {
          computedUnanswered++;
        }
        computedBonus += normalized[i].bonusPointsAwarded;
      }

      var fallbackAvg = normalized.length > 0 ? round2(computedSum / normalized.length) : 0;
      var avgRating = round2(
        pickDefined(
          dataLayer,
          ['averageRating', 'averageCsatRating', 'AverageRating', 'AverageCsatRating'],
          pickDefined(rawEnvelope, ['averageRating', 'AverageRating'], fallbackAvg)
        )
      );
      var fiveStarCount = Math.max(
        0,
        Math.floor(
          Number(
            pickDefined(
              dataLayer,
              ['fiveStarCount', 'fiveStarReviewsCount', 'FiveStarCount'],
              pickDefined(rawEnvelope, ['fiveStarCount', 'FiveStarCount'], computedFiveStar)
            )
          ) || 0
        )
      );
      var unansweredCount = Math.max(
        0,
        Math.floor(
          Number(
            pickDefined(
              dataLayer,
              ['unansweredCount', 'unansweredReviewsCount', 'UnansweredCount'],
              pickDefined(rawEnvelope, ['unansweredCount', 'UnansweredCount'], computedUnanswered)
            )
          ) || 0
        )
      );
      var totalBonus = Math.max(
        0,
        Math.floor(
          Number(
            pickDefined(
              dataLayer,
              ['totalBonusPointsAwarded', 'TotalBonusPointsAwarded'],
              pickDefined(rawEnvelope, ['totalBonusPointsAwarded', 'TotalBonusPointsAwarded'], computedBonus)
            )
          ) || 0
        )
      );
      var totalCount = Math.max(
        normalized.length,
        Math.floor(
          Number(
            pickDefined(
              dataLayer,
              ['totalReviewsCount', 'totalCount', 'TotalReviewsCount', 'TotalCount'],
              (paged.pagination && paged.pagination.totalCount) || normalized.length
            )
          ) || 0
        )
      );

      return attachNonEnumerableMeta(normalized, {
        items: normalized,
        averageRating: avgRating,
        fiveStarCount: fiveStarCount,
        unansweredCount: unansweredCount,
        totalBonusPointsAwarded: totalBonus,
        totalReviewsCount: totalCount,
        totalCount: totalCount,
        pagination: paged.pagination
      });
    };

    self.getMyServiceReviews = function (params) {
      var cleaned = self.cleanParams(params);
      if (cleaned.pageNumber && !cleaned.page) {
        cleaned.page = cleaned.pageNumber;
      }
      return $http.get(baseUrl() + '/service-reviews/my', {
        params: cleaned
      }).then(function (res) {
        return self.unwrapServiceReviewCollection(res);
      });
    };

    self.getDealerServiceReviews = function (params) {
      var cleaned = self.cleanParams(params);
      if (cleaned.pageNumber && !cleaned.page) {
        cleaned.page = cleaned.pageNumber;
      }
      return $http.get(baseUrl() + '/service-reviews', {
        params: cleaned
      }).then(function (res) {
        return self.unwrapServiceReviewCollection(res);
      });
    };

    self.getServiceReviewByInvoice = function (invoiceId) {
      return $http.get(baseUrl() + '/service-reviews/by-invoice/' + encodeURIComponent(invoiceId))
        .then(function (res) {
          var item = self.unwrapItem(res);
          if (!item || (angular.isObject(item) && !item.id && !item.reviewId && !item.Id && !item.ReviewId)) {
            return null;
          }
          return self.normalizeServiceReview(item);
        })
        .catch(function (err) {
          if (err && err.status === 404) {
            return null;
          }
          return $q.reject(err);
        });
    };

    self.createServiceReview = function (payload, idempotencyKey) {
      var p = payload || {};
      var key = idempotencyKey || self.generateIdempotencyKey('service-review');
      var tagsList = parseQualityTagsList(p.qualityTags !== undefined ? p.qualityTags : p.tags);
      var body = {
        serviceInvoiceId: p.serviceInvoiceId || p.invoiceId,
        rating: Math.max(1, Math.min(5, Math.floor(Number(p.rating) || 5))),
        qualityTags: tagsList,
        comment: p.comment && String(p.comment).trim() ? String(p.comment).trim() : null
      };
      return $http.post(baseUrl() + '/service-reviews', body, {
        headers: { 'Idempotency-Key': key }
      }).then(function (res) {
        return self.normalizeServiceReview(self.unwrapItem(res));
      });
    };

    self.respondToServiceReview = function (reviewId, payload) {
      var p = payload || {};
      var body = {
        staffResponse: String(p.staffResponse || p.response || '').trim(),
        rowVersion: p.rowVersion || null
      };
      return $http.patch(baseUrl() + '/service-reviews/' + encodeURIComponent(reviewId) + '/response', body)
        .then(function (res) {
          return self.normalizeServiceReview(self.unwrapItem(res));
        });
    };

    self.serviceReviews = {
      getQualityTags: self.getReviewQualityTags,
      listMine: self.getMyServiceReviews,
      listDealer: self.getDealerServiceReviews,
      getByInvoice: self.getServiceReviewByInvoice,
      create: self.createServiceReview,
      respond: self.respondToServiceReview
    };

    self.normalizeWarrantyClaim = function (raw) {
      var wc = raw || {};
      var resolvedId = String(pickDefined(wc, ['id', 'claimId', 'warrantyClaimId', 'Id', 'ClaimId'], ''));
      var vinVal = String(pickDefined(wc, ['vinSnapshot', 'vin', 'VinSnapshot', 'Vin'], '')).toUpperCase();
      var coveredPartId = pickDefined(wc, ['coveredSparePartId', 'sparePartId', 'CoveredSparePartId'], null);
      var coveredPartCode = pickDefined(wc, ['coveredSparePartCode', 'partCode', 'CoveredSparePartCode'], null);
      var coveredPartName = pickDefined(wc, ['coveredSparePartName', 'partName', 'CoveredSparePartName'], null);
      var phoneVal = String(pickDefined(wc, ['customerPhoneNumber', 'customerPhone', 'contactPhone', 'phoneNumber', 'CustomerPhoneNumber', 'CustomerPhone'], ''));
      var reviewedByStaff = pickDefined(
        wc,
        ['reviewedByStaffName', 'reviewedByName', 'reviewedByFullName', 'ReviewedByStaffName', 'ReviewedByName', 'ReviewedByFullName'],
        null
      );

      return {
        id: resolvedId,
        claimId: resolvedId,
        claimNumber: String(pickDefined(wc, ['claimNumber', 'ClaimNumber'], '')),
        status: String(pickDefined(wc, ['status', 'newStatus', 'Status', 'NewStatus'], 'Submitted')),
        previousStatus: pickDefined(wc, ['previousStatus', 'PreviousStatus'], null),
        dealerId: String(pickDefined(wc, ['dealerId', 'DealerId'], '')),
        dealerCode: String(pickDefined(wc, ['dealerCode', 'DealerCode'], '')).toUpperCase(),
        dealerName: String(pickDefined(wc, ['dealerName', 'DealerName'], '')),
        dealerCity: String(pickDefined(wc, ['dealerCity', 'DealerCity'], '')),
        customerId: String(pickDefined(wc, ['customerId', 'CustomerId'], '')),
        customerFullName: String(pickDefined(wc, ['customerFullName', 'customerName', 'CustomerFullName'], '')),
        customerEmail: String(pickDefined(wc, ['customerEmail', 'CustomerEmail'], '')),
        customerPhone: phoneVal,
        customerPhoneNumber: phoneVal,
        customerVehicleId: String(pickDefined(wc, ['customerVehicleId', 'vehicleId', 'CustomerVehicleId'], '')),
        vin: vinVal,
        vinSnapshot: vinVal,
        licensePlate: pickDefined(wc, ['licensePlate', 'LicensePlate'], null),
        motorcycleModelId: String(pickDefined(wc, ['motorcycleModelId', 'modelId', 'MotorcycleModelId'], '')),
        motorcycleModelCode: String(pickDefined(wc, ['motorcycleModelCode', 'modelCode', 'MotorcycleModelCode'], '')),
        motorcycleModelName: String(pickDefined(wc, ['motorcycleModelName', 'modelName', 'MotorcycleModelName'], '')),
        imageUrl: String(pickDefined(wc, ['imageUrl', 'ImageUrl', 'vehicleImageUrl', 'motorcycleModelImageUrl', 'modelImageUrl', 'VehicleImageUrl', 'MotorcycleModelImageUrl', 'ModelImageUrl'], '') || '').trim(),
        warrantyStartUtc: pickDefined(wc, ['warrantyStartUtc', 'WarrantyStartUtc'], null),
        warrantyExpiresAtUtc: pickDefined(wc, ['warrantyExpiresAtUtc', 'WarrantyExpiresAtUtc'], null),
        odometerAtClaimKm: Math.max(0, Math.floor(Number(pickDefined(wc, ['odometerAtClaimKm', 'OdometerAtClaimKm'], 0)) || 0)),
        defectCategory: String(pickDefined(wc, ['defectCategory', 'DefectCategory'], 'Engine')),
        symptomDescription: String(pickDefined(wc, ['symptomDescription', 'SymptomDescription'], '')),
        coveredSparePartId: coveredPartId ? String(coveredPartId) : null,
        coveredSparePartCode: coveredPartCode ? String(coveredPartCode).toUpperCase() : null,
        coveredSparePartName: coveredPartName ? String(coveredPartName) : null,
        coveredAmountVnd: Math.max(0, Number(pickDefined(wc, ['coveredAmountVnd', 'CoveredAmountVnd'], 0)) || 0),
        resolutionNotes: pickDefined(wc, ['resolutionNotes', 'ResolutionNotes'], null),
        rejectionReason: pickDefined(wc, ['rejectionReason', 'RejectionReason'], null),
        reviewedByUserId: pickDefined(wc, ['reviewedByUserId', 'ReviewedByUserId'], null),
        reviewedByName: reviewedByStaff,
        reviewedByStaffName: reviewedByStaff,
        reviewedAtUtc: pickDefined(wc, ['reviewedAtUtc', 'ReviewedAtUtc'], null),
        completedAtUtc: pickDefined(wc, ['completedAtUtc', 'CompletedAtUtc'], null),
        createdAtUtc: pickDefined(wc, ['createdAtUtc', 'occurredAtUtc', 'CreatedAtUtc', 'OccurredAtUtc'], null),
        updatedAtUtc: pickDefined(wc, ['updatedAtUtc', 'UpdatedAtUtc'], null),
        rowVersion: String(pickDefined(wc, ['rowVersion', 'RowVersion'], '')),
        isLiveArrival: Boolean(wc.isLiveArrival)
      };
    };

    self.unwrapWarrantyClaimCollection = function (responseOrBody) {
      var dataLayer = self.unwrapItem(responseOrBody);
      var paged = self.unwrapPaged(responseOrBody);
      var rawList = paged.items;
      if ((!rawList || rawList.length === 0) && dataLayer && angular.isObject(dataLayer)) {
        var altItems = pickDefined(dataLayer, ['claims', 'warrantyClaims', 'Claims', 'WarrantyClaims', 'items', 'Items'], null);
        if (angular.isArray(altItems)) {
          rawList = altItems;
        }
      }
      var normalized = (rawList || []).map(self.normalizeWarrantyClaim);

      var openCount = 0;
      var completedCount = 0;
      var rejectedCount = 0;
      var coveredSum = 0;
      for (var i = 0; i < normalized.length; i++) {
        var st = normalized[i].status;
        if (self.isOpenWarrantyStatus(st)) {
          openCount++;
        } else if (st === 'Completed') {
          completedCount++;
        } else if (st === 'Rejected') {
          rejectedCount++;
        }
        if (st === 'Approved' || st === 'Completed') {
          coveredSum += normalized[i].coveredAmountVnd;
        }
      }

      return attachNonEnumerableMeta(normalized, {
        items: normalized,
        openCount: Math.max(0, Math.floor(Number(pickDefined(dataLayer, ['openCount', 'openClaimsCount', 'OpenCount'], openCount)) || 0)),
        completedCount: Math.max(0, Math.floor(Number(pickDefined(dataLayer, ['completedCount', 'CompletedCount'], completedCount)) || 0)),
        rejectedCount: Math.max(0, Math.floor(Number(pickDefined(dataLayer, ['rejectedCount', 'RejectedCount'], rejectedCount)) || 0)),
        totalCoveredAmountVnd: Math.max(0, Number(pickDefined(dataLayer, ['totalCoveredAmountVnd', 'approvedWarrantyCoveredVnd', 'TotalCoveredAmountVnd'], coveredSum)) || 0),
        totalCount: Math.max(
          normalized.length,
          Math.floor(Number(pickDefined(dataLayer, ['totalCount', 'TotalCount'], (paged.pagination && paged.pagination.totalCount) || normalized.length)) || 0)
        ),
        pagination: paged.pagination
      });
    };

    self.getMyWarrantyClaims = function (params) {
      var cleaned = self.cleanParams(params);
      if (cleaned.pageNumber && !cleaned.page) {
        cleaned.page = cleaned.pageNumber;
      }
      return $http.get(baseUrl() + '/warranty-claims/my', {
        params: cleaned
      }).then(function (res) {
        return self.unwrapWarrantyClaimCollection(res);
      });
    };

    self.getDealerWarrantyClaims = function (params) {
      var cleaned = self.cleanParams(params);
      if (cleaned.pageNumber && !cleaned.page) {
        cleaned.page = cleaned.pageNumber;
      }
      return $http.get(baseUrl() + '/warranty-claims', {
        params: cleaned
      }).then(function (res) {
        return self.unwrapWarrantyClaimCollection(res);
      });
    };

    self.getWarrantyClaimById = function (claimId) {
      return $http.get(baseUrl() + '/warranty-claims/' + encodeURIComponent(claimId))
        .then(function (res) {
          return self.normalizeWarrantyClaim(self.unwrapItem(res));
        });
    };

    self.createWarrantyClaim = function (payload, idempotencyKey) {
      var p = payload || {};
      var key = idempotencyKey || self.generateIdempotencyKey('warranty-claim');
      var body = {
        customerVehicleId: p.customerVehicleId || p.vehicleId,
        dealerId: p.dealerId,
        odometerAtClaimKm: Math.max(0, Math.floor(Number(p.odometerAtClaimKm) || 0)),
        defectCategory: p.defectCategory || 'Engine',
        symptomDescription: String(p.symptomDescription || '').trim()
      };
      return $http.post(baseUrl() + '/warranty-claims', body, {
        headers: { 'Idempotency-Key': key }
      }).then(function (res) {
        return self.normalizeWarrantyClaim(self.unwrapItem(res));
      });
    };

    self.updateWarrantyClaimStatus = function (claimId, payload) {
      var p = payload || {};
      var nextStatus = String(p.newStatus || p.status || '').trim();
      var coveredPartId = p.coveredSparePartId ? String(p.coveredSparePartId).trim() : null;
      var coveredAmt = nextStatus === 'Rejected'
        ? 0
        : (p.coveredAmountVnd === undefined || p.coveredAmountVnd === null || p.coveredAmountVnd === ''
            ? null
            : Math.max(0, Number(p.coveredAmountVnd) || 0));
      var body = {
        newStatus: nextStatus,
        status: nextStatus,
        coveredSparePartId: nextStatus === 'Rejected' ? null : (coveredPartId || null),
        coveredAmountVnd: coveredAmt,
        resolutionNotes: p.resolutionNotes && String(p.resolutionNotes).trim() ? String(p.resolutionNotes).trim() : null,
        rejectionReason: p.rejectionReason && String(p.rejectionReason).trim() ? String(p.rejectionReason).trim() : null,
        rowVersion: p.rowVersion || null
      };
      return $http.patch(baseUrl() + '/warranty-claims/' + encodeURIComponent(claimId) + '/status', body)
        .then(function (res) {
          return self.normalizeWarrantyClaim(self.unwrapItem(res));
        });
    };

    self.warrantyClaims = {
      getDefectCategories: self.getWarrantyDefectCategories,
      listMine: self.getMyWarrantyClaims,
      listDealer: self.getDealerWarrantyClaims,
      getById: self.getWarrantyClaimById,
      create: self.createWarrantyClaim,
      updateStatus: self.updateWarrantyClaimStatus
    };
  }
})();
