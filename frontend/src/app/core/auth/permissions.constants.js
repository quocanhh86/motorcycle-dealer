(function () {
  'use strict';

  var ROLES = Object.freeze({
    Customer: 'Customer',
    DealerStaff: 'DealerStaff',
    Admin: 'Admin'
  });

  var ROLE_HOME = Object.freeze({
    Customer: 'app.customer.garage',
    DealerStaff: 'app.staff.workbench',
    Admin: 'app.admin.analytics'
  });

  // Canonical 48-Permission RBAC Matrix (v1.2 Phase 9: docs/DESIGN.md §1.2 & baseline-spec-snapshot.json)
  var PERMISSIONS = Object.freeze({
    Showroom: Object.freeze({
      View: 'Permissions.Showroom.View',
      ManageModels: 'Permissions.Showroom.ManageModels'
    }),
    Inventories: Object.freeze({
      ViewDealer: 'Permissions.Inventories.ViewDealer',
      ManageDealer: 'Permissions.Inventories.ManageDealer'
    }),
    SalesOrders: Object.freeze({
      Create: 'Permissions.SalesOrders.Create',
      ViewOwn: 'Permissions.SalesOrders.ViewOwn',
      ViewDealer: 'Permissions.SalesOrders.ViewDealer'
    }),
    Vehicles: Object.freeze({
      ViewOwn: 'Permissions.Vehicles.ViewOwn',
      RegisterOwn: 'Permissions.Vehicles.RegisterOwn',
      ViewDealer: 'Permissions.Vehicles.ViewDealer',
      ManageRules: 'Permissions.Vehicles.ManageRules'
    }),
    TimeSlots: Object.freeze({
      ViewAvailable: 'Permissions.TimeSlots.ViewAvailable',
      ManageDealer: 'Permissions.TimeSlots.ManageDealer'
    }),
    Bookings: Object.freeze({
      Create: 'Permissions.Bookings.Create',
      CancelOwn: 'Permissions.Bookings.CancelOwn',
      ViewOwn: 'Permissions.Bookings.ViewOwn',
      ViewDealer: 'Permissions.Bookings.ViewDealer',
      UpdateStatus: 'Permissions.Bookings.UpdateStatus'
    }),
    TestDrives: Object.freeze({
      Create: 'Permissions.TestDrives.Create',
      CancelOwn: 'Permissions.TestDrives.CancelOwn',
      ViewOwn: 'Permissions.TestDrives.ViewOwn',
      ViewDealer: 'Permissions.TestDrives.ViewDealer',
      UpdateStatus: 'Permissions.TestDrives.UpdateStatus'
    }),
    Invoices: Object.freeze({
      CreateDealer: 'Permissions.Invoices.CreateDealer',
      ViewOwn: 'Permissions.Invoices.ViewOwn',
      ViewDealer: 'Permissions.Invoices.ViewDealer'
    }),
    SpareParts: Object.freeze({
      ManageCatalog: 'Permissions.SpareParts.ManageCatalog'
    }),
    Reviews: Object.freeze({
      CreateOwn: 'Permissions.Reviews.CreateOwn',
      ViewOwn: 'Permissions.Reviews.ViewOwn',
      ViewDealer: 'Permissions.Reviews.ViewDealer',
      RespondDealer: 'Permissions.Reviews.RespondDealer'
    }),
    Warranty: Object.freeze({
      CreateOwn: 'Permissions.Warranty.CreateOwn',
      ViewOwn: 'Permissions.Warranty.ViewOwn',
      ViewDealer: 'Permissions.Warranty.ViewDealer',
      ManageDealer: 'Permissions.Warranty.ManageDealer'
    }),
    Loyalty: Object.freeze({
      ViewOwn: 'Permissions.Loyalty.ViewOwn',
      RedeemVoucher: 'Permissions.Loyalty.RedeemVoucher',
      ViewDealerCustomer: 'Permissions.Loyalty.ViewDealerCustomer',
      ManageTiers: 'Permissions.Loyalty.ManageTiers',
      ManageVoucherCatalog: 'Permissions.Loyalty.ManageVoucherCatalog',
      AdjustPoints: 'Permissions.Loyalty.AdjustPoints'
    }),
    Notifications: Object.freeze({
      ViewOwn: 'Permissions.Notifications.ViewOwn',
      ViewDealer: 'Permissions.Notifications.ViewDealer'
    }),
    Dealers: Object.freeze({
      ViewPublic: 'Permissions.Dealers.ViewPublic',
      ManageAll: 'Permissions.Dealers.ManageAll'
    }),
    Users: Object.freeze({
      ManageStaff: 'Permissions.Users.ManageStaff'
    }),
    Analytics: Object.freeze({
      ViewAllDealers: 'Permissions.Analytics.ViewAllDealers',
      ExportReports: 'Permissions.Analytics.ExportReports'
    })
  });

  var ROLE_PERMISSION_PRESETS = Object.freeze({
    Customer: Object.freeze([
      PERMISSIONS.Showroom.View,
      PERMISSIONS.SalesOrders.ViewOwn,
      PERMISSIONS.Vehicles.ViewOwn,
      PERMISSIONS.Vehicles.RegisterOwn,
      PERMISSIONS.TimeSlots.ViewAvailable,
      PERMISSIONS.Bookings.Create,
      PERMISSIONS.Bookings.CancelOwn,
      PERMISSIONS.Bookings.ViewOwn,
      PERMISSIONS.TestDrives.Create,
      PERMISSIONS.TestDrives.CancelOwn,
      PERMISSIONS.TestDrives.ViewOwn,
      PERMISSIONS.Invoices.ViewOwn,
      PERMISSIONS.Reviews.CreateOwn,
      PERMISSIONS.Reviews.ViewOwn,
      PERMISSIONS.Warranty.CreateOwn,
      PERMISSIONS.Warranty.ViewOwn,
      PERMISSIONS.Loyalty.ViewOwn,
      PERMISSIONS.Loyalty.RedeemVoucher,
      PERMISSIONS.Notifications.ViewOwn,
      PERMISSIONS.Dealers.ViewPublic
    ]),
    DealerStaff: Object.freeze([
      PERMISSIONS.Showroom.View,
      PERMISSIONS.Inventories.ViewDealer,
      PERMISSIONS.Inventories.ManageDealer,
      PERMISSIONS.SalesOrders.Create,
      PERMISSIONS.SalesOrders.ViewDealer,
      PERMISSIONS.Vehicles.ViewDealer,
      PERMISSIONS.TimeSlots.ViewAvailable,
      PERMISSIONS.TimeSlots.ManageDealer,
      PERMISSIONS.Bookings.ViewDealer,
      PERMISSIONS.Bookings.UpdateStatus,
      PERMISSIONS.TestDrives.ViewDealer,
      PERMISSIONS.TestDrives.UpdateStatus,
      PERMISSIONS.Invoices.CreateDealer,
      PERMISSIONS.Invoices.ViewDealer,
      PERMISSIONS.Reviews.ViewDealer,
      PERMISSIONS.Reviews.RespondDealer,
      PERMISSIONS.Warranty.ViewDealer,
      PERMISSIONS.Warranty.ManageDealer,
      PERMISSIONS.Loyalty.ViewDealerCustomer,
      PERMISSIONS.Notifications.ViewOwn,
      PERMISSIONS.Notifications.ViewDealer,
      PERMISSIONS.Dealers.ViewPublic
    ]),
    Admin: Object.freeze([
      PERMISSIONS.Showroom.View,
      PERMISSIONS.Showroom.ManageModels,
      PERMISSIONS.Inventories.ViewDealer,
      PERMISSIONS.Inventories.ManageDealer,
      PERMISSIONS.SalesOrders.Create,
      PERMISSIONS.SalesOrders.ViewDealer,
      PERMISSIONS.Vehicles.ViewDealer,
      PERMISSIONS.Vehicles.ManageRules,
      PERMISSIONS.TimeSlots.ViewAvailable,
      PERMISSIONS.TimeSlots.ManageDealer,
      PERMISSIONS.Bookings.ViewDealer,
      PERMISSIONS.Bookings.UpdateStatus,
      PERMISSIONS.TestDrives.ViewDealer,
      PERMISSIONS.TestDrives.UpdateStatus,
      PERMISSIONS.Invoices.CreateDealer,
      PERMISSIONS.Invoices.ViewDealer,
      PERMISSIONS.SpareParts.ManageCatalog,
      PERMISSIONS.Reviews.ViewDealer,
      PERMISSIONS.Reviews.RespondDealer,
      PERMISSIONS.Warranty.ViewDealer,
      PERMISSIONS.Warranty.ManageDealer,
      PERMISSIONS.Loyalty.ViewDealerCustomer,
      PERMISSIONS.Loyalty.ManageTiers,
      PERMISSIONS.Loyalty.ManageVoucherCatalog,
      PERMISSIONS.Loyalty.AdjustPoints,
      PERMISSIONS.Notifications.ViewOwn,
      PERMISSIONS.Notifications.ViewDealer,
      PERMISSIONS.Dealers.ViewPublic,
      PERMISSIONS.Dealers.ManageAll,
      PERMISSIONS.Users.ManageStaff,
      PERMISSIONS.Analytics.ViewAllDealers,
      PERMISSIONS.Analytics.ExportReports
    ])
  });

  var LOYALTY_TIERS = Object.freeze([
    Object.freeze({ code: 'Bronze', rankOrder: 1, minQualifyingPoints: 0, multiplier: 1.0 }),
    Object.freeze({ code: 'Silver', rankOrder: 2, minQualifyingPoints: 500, multiplier: 1.2 }),
    Object.freeze({ code: 'Gold', rankOrder: 3, minQualifyingPoints: 2000, multiplier: 1.5 }),
    Object.freeze({ code: 'Platinum', rankOrder: 4, minQualifyingPoints: 5000, multiplier: 2.0 })
  ]);

  // Integer basis-point two-step floor math eliminating IEEE-754 drift (RISK-03)
  function calculateLoyaltyPoints(netPaidAmountVnd, tierMultiplier) {
    var safeAmount = Math.max(0, Number(netPaidAmountVnd) || 0);
    var safeMultiplier = Math.max(0, Number(tierMultiplier) || 1.0);
    var basePoints = Math.floor(safeAmount / 10000);
    var multiplierScaled = Math.round(safeMultiplier * 10000);
    return Math.floor((basePoints * multiplierScaled) / 10000);
  }

  // Returns extensible copies of frozen LOYALTY_TIERS so AngularJS ng-options / ng-repeat never hit $$hashKey TypeError
  function cloneLoyaltyTiers() {
    return LOYALTY_TIERS.map(function (tier) {
      return angular.extend({}, tier);
    });
  }

  function resolveNormalizedTiers(customTiers) {
    if (!angular.isArray(customTiers) || customTiers.length === 0) {
      return LOYALTY_TIERS;
    }
    var mapped = [];
    for (var i = 0; i < customTiers.length; i++) {
      var raw = customTiers[i];
      if (!raw || !angular.isObject(raw)) {
        continue;
      }
      var code = String(raw.tierCode || raw.code || '').trim();
      if (!code) {
        continue;
      }
      var rawMult = raw.pointsMultiplier !== undefined && raw.pointsMultiplier !== null
        ? Number(raw.pointsMultiplier)
        : Number(raw.multiplier);
      var mult = (!isNaN(rawMult) && rawMult > 0) ? rawMult : 1.0;
      var minPts = Math.max(0, Math.floor(Number(raw.minQualifyingPoints) || 0));
      var rank = Number(raw.rankOrder);
      mapped.push({
        code: code,
        tierCode: code,
        displayName: raw.displayName ? String(raw.displayName) : code,
        rankOrder: (!isNaN(rank) && rank > 0) ? Math.floor(rank) : (i + 1),
        minQualifyingPoints: minPts,
        multiplier: mult,
        pointsMultiplier: mult
      });
    }
    if (mapped.length === 0) {
      return LOYALTY_TIERS;
    }
    mapped.sort(function (a, b) {
      if (a.minQualifyingPoints !== b.minQualifyingPoints) {
        return a.minQualifyingPoints - b.minQualifyingPoints;
      }
      return a.rankOrder - b.rankOrder;
    });
    for (var j = 0; j < mapped.length; j++) {
      mapped[j].rankOrder = j + 1;
    }
    return mapped;
  }

  // Canonical 4-tier progression lookup by LifetimeQualifyingPoints (supports optional dynamic customTiers)
  function determineLoyaltyTier(lifetimeQualifyingPoints, customTiers) {
    var tiers = resolveNormalizedTiers(customTiers);
    var safePoints = Math.max(0, Math.floor(Number(lifetimeQualifyingPoints) || 0));
    var matched = tiers[0];
    for (var i = 0; i < tiers.length; i++) {
      if (safePoints >= tiers[i].minQualifyingPoints && tiers[i].rankOrder >= matched.rankOrder) {
        matched = tiers[i];
      }
    }
    return angular.extend({}, matched);
  }

  function determineLoyaltyTierCode(lifetimeQualifyingPoints, customTiers) {
    return determineLoyaltyTier(lifetimeQualifyingPoints, customTiers).code;
  }

  function resolveLoyaltyTierProgress(lifetimeQualifyingPoints, customTiers) {
    var tiers = resolveNormalizedTiers(customTiers);
    var safePoints = Math.max(0, Math.floor(Number(lifetimeQualifyingPoints) || 0));
    var matchedIndex = 0;
    for (var i = 0; i < tiers.length; i++) {
      if (safePoints >= tiers[i].minQualifyingPoints) {
        matchedIndex = i;
      }
    }

    var currentTier = tiers[matchedIndex];
    var nextTier = matchedIndex + 1 < tiers.length ? tiers[matchedIndex + 1] : null;
    if (!nextTier) {
      return {
        currentTierCode: currentTier.code,
        currentMultiplier: currentTier.multiplier,
        currentMinPoints: currentTier.minQualifyingPoints,
        lifetimeQualifyingPoints: safePoints,
        nextTierCode: null,
        nextTierMultiplier: null,
        nextTierThreshold: null,
        pointsToNextTier: 0,
        progressPercent: 100,
        isTopTier: true
      };
    }

    var bandSpan = Math.max(1, nextTier.minQualifyingPoints - currentTier.minQualifyingPoints);
    var earnedInBand = Math.max(0, safePoints - currentTier.minQualifyingPoints);
    var progressPercent = Math.min(100, Math.max(0, Math.round((earnedInBand / bandSpan) * 100)));

    return {
      currentTierCode: currentTier.code,
      currentMultiplier: currentTier.multiplier,
      currentMinPoints: currentTier.minQualifyingPoints,
      lifetimeQualifyingPoints: safePoints,
      nextTierCode: nextTier.code,
      nextTierMultiplier: nextTier.multiplier,
      nextTierThreshold: nextTier.minQualifyingPoints,
      pointsToNextTier: Math.max(0, nextTier.minQualifyingPoints - safePoints),
      progressPercent: progressPercent,
      isTopTier: false
    };
  }

  function getLoyaltyTierRank(tierCode, customTiers) {
    var tiers = resolveNormalizedTiers(customTiers);
    var code = String(tierCode || 'Bronze').trim().toLowerCase();
    for (var i = 0; i < tiers.length; i++) {
      if (tiers[i].code.toLowerCase() === code) {
        return tiers[i].rankOrder;
      }
    }
    return 1;
  }

  function isVietnameseLocale(locale) {
    return angular.isString(locale) && locale.trim().toLowerCase().indexOf('vi') === 0;
  }

  // Deterministic VND voucher discount evaluator (FixedAmount vs Percentage capped by MaxDiscountAmount & GrossAmount)
  function computeVoucherDiscount(voucher, grossAmountVnd, locale) {
    var gross = Math.max(0, Number(grossAmountVnd) || 0);
    if (!voucher || !angular.isObject(voucher)) {
      return {
        discountAmount: 0,
        isEligible: true,
        ineligibleReason: null
      };
    }

    var minOrder = Math.max(
      0,
      Number(voucher.minOrderAmount !== undefined ? voucher.minOrderAmount : voucher.minOrderVnd) || 0
    );
    if (gross < minOrder) {
      var isVi = isVietnameseLocale(locale);
      return {
        discountAmount: 0,
        isEligible: false,
        ineligibleReason: isVi
          ? ('Tổng tiền (' + Math.round(gross).toLocaleString('vi-VN') + ' ₫) thấp hơn mức tối thiểu của voucher (' + Math.round(minOrder).toLocaleString('vi-VN') + ' ₫).')
          : ('Gross amount (' + gross.toLocaleString('en-US') + ' VND) is below voucher minimum (' + minOrder.toLocaleString('en-US') + ' VND).')
      };
    }

    var discountType = String(voucher.discountType || 'FixedAmount');
    var discountVal = Math.max(0, Number(voucher.discountValue) || 0);
    var rawDiscount = 0;

    if (discountType === 'Percentage') {
      // Exact 2-decimal AwayFromZero rounding matching C# Math.Round(gross * (val / 100m), 2, MidpointRounding.AwayFromZero)
      var rawPct = (gross * discountVal) / 100;
      rawDiscount = Math.floor((rawPct * 100) + 0.5) / 100;
      if (voucher.maxDiscountAmount !== null && voucher.maxDiscountAmount !== undefined) {
        var cap = Number(voucher.maxDiscountAmount);
        if (!isNaN(cap) && cap > 0) {
          rawDiscount = Math.min(rawDiscount, cap);
        }
      }
    } else {
      rawDiscount = discountVal;
    }

    return {
      discountAmount: Math.min(gross, Math.max(0, rawDiscount)),
      isEligible: true,
      ineligibleReason: null
    };
  }

  // Shared deterministic discount label formatter (FixedAmount vs Percentage + optional MaxDiscountAmount cap)
  function formatDiscountSummary(voucherOrCatalog, locale) {
    if (!voucherOrCatalog || !angular.isObject(voucherOrCatalog)) {
      return '';
    }
    if (
      voucherOrCatalog.discountValue === undefined &&
      voucherOrCatalog.discountType === undefined &&
      voucherOrCatalog.discountSummary
    ) {
      return String(voucherOrCatalog.discountSummary);
    }
    var isVi = isVietnameseLocale(locale);
    var dtype = String(voucherOrCatalog.discountType || 'FixedAmount');
    var dval = Math.max(0, Number(voucherOrCatalog.discountValue) || 0);
    if (dtype === 'Percentage') {
      var rawCap = voucherOrCatalog.maxDiscountAmount;
      var capNum = rawCap !== null && rawCap !== undefined && rawCap !== '' ? Number(rawCap) : 0;
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

  angular
    .module('app.core')
    .constant('ROLES', ROLES)
    .constant('ROLE_HOME', ROLE_HOME)
    .constant('PERMISSIONS', PERMISSIONS)
    .constant('ROLE_PERMISSION_PRESETS', ROLE_PERMISSION_PRESETS)
    .constant('LOYALTY_TIERS', LOYALTY_TIERS)
    .constant('LoyaltyCalculator', Object.freeze({
      calculatePoints: calculateLoyaltyPoints,
      cloneTiers: cloneLoyaltyTiers,
      determineTier: determineLoyaltyTier,
      determineTierCode: determineLoyaltyTierCode,
      resolveTierProgress: resolveLoyaltyTierProgress,
      getTierRank: getLoyaltyTierRank,
      computeVoucherDiscount: computeVoucherDiscount,
      formatDiscountSummary: formatDiscountSummary,
      formatDiscountSummaryLocalized: formatDiscountSummary
    }));
})();
