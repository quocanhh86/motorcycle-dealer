using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Domain.Services;

public sealed record LoyaltyAccrualPreview(
    int BasePoints,
    LoyaltyTierCode AppliedTier,
    decimal AppliedMultiplier,
    int PointsEarned,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    LoyaltyTierCode ResultingTier,
    bool TierUpgraded);

public sealed record VoucherRedemptionPreview(
    int PointsSpent,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    LoyaltyTierCode ResultingTier);

/// <summary>
/// Pure deterministic domain calculator for two-step floor loyalty accrual and 4-tier progression (ADR-003, FR-05).
/// Formula: PointsEarned = (int)Math.Floor(Math.Floor(NetPaidAmount / 10,000m) * TierMultiplier)
/// </summary>
public static class LoyaltyPointsCalculator
{
    public const decimal BaseUnitVnd = 10_000m;

    public const string VoucherCodeSuffixAlphabet = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";

    public const int BronzeMinQualifyingPoints = 0;
    public const int SilverMinQualifyingPoints = 500;
    public const int GoldMinQualifyingPoints = 2_000;
    public const int PlatinumMinQualifyingPoints = 5_000;

    public const decimal BronzeMultiplier = 1.00m;
    public const decimal SilverMultiplier = 1.20m;
    public const decimal GoldMultiplier = 1.50m;
    public const decimal PlatinumMultiplier = 2.00m;

    public static int CalculateBasePoints(decimal netPaidAmount)
    {
        if (netPaidAmount < 0m)
        {
            throw new DomainException("INVALID_NET_PAID_AMOUNT", "Net paid amount cannot be negative.");
        }

        return (int)Math.Floor(netPaidAmount / BaseUnitVnd);
    }

    public static int CalculatePointsEarned(decimal netPaidAmount, decimal tierMultiplier)
    {
        if (netPaidAmount < 0m)
        {
            throw new DomainException("INVALID_NET_PAID_AMOUNT", "Net paid amount cannot be negative.");
        }

        if (tierMultiplier < 1.00m || tierMultiplier > 10.00m)
        {
            throw new DomainException("INVALID_TIER_MULTIPLIER", "Tier multiplier must be between 1.00 and 10.00.");
        }

        var basePoints = Math.Floor(netPaidAmount / BaseUnitVnd);
        return (int)Math.Floor(basePoints * tierMultiplier);
    }

    public static int CalculatePointsEarned(decimal netPaidAmount, LoyaltyTierCode activeTier) =>
        CalculatePointsEarned(netPaidAmount, GetMultiplier(activeTier));

    public static decimal GetMultiplier(LoyaltyTierCode tier) =>
        tier switch
        {
            LoyaltyTierCode.Bronze => BronzeMultiplier,
            LoyaltyTierCode.Silver => SilverMultiplier,
            LoyaltyTierCode.Gold => GoldMultiplier,
            LoyaltyTierCode.Platinum => PlatinumMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unsupported loyalty tier code.")
        };

    public static int GetMinQualifyingPoints(LoyaltyTierCode tier) =>
        tier switch
        {
            LoyaltyTierCode.Bronze => BronzeMinQualifyingPoints,
            LoyaltyTierCode.Silver => SilverMinQualifyingPoints,
            LoyaltyTierCode.Gold => GoldMinQualifyingPoints,
            LoyaltyTierCode.Platinum => PlatinumMinQualifyingPoints,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unsupported loyalty tier code.")
        };

    public static LoyaltyTierCode EvaluateTier(int lifetimeQualifyingPoints)
    {
        if (lifetimeQualifyingPoints < 0)
        {
            throw new DomainException("INVALID_QUALIFYING_POINTS", "Lifetime qualifying points cannot be negative.");
        }

        return lifetimeQualifyingPoints switch
        {
            >= PlatinumMinQualifyingPoints => LoyaltyTierCode.Platinum,
            >= GoldMinQualifyingPoints => LoyaltyTierCode.Gold,
            >= SilverMinQualifyingPoints => LoyaltyTierCode.Silver,
            _ => LoyaltyTierCode.Bronze
        };
    }

    public static LoyaltyAccrualPreview EvaluateAccrualPreview(
        decimal netPaidAmount,
        int currentAvailablePoints,
        int currentLifetimeQualifyingPoints,
        LoyaltyTierCode currentTier)
    {
        if (currentAvailablePoints < 0 || currentLifetimeQualifyingPoints < 0)
        {
            throw new DomainException("INVALID_POINT_BALANCE", "Point balances cannot be negative.");
        }

        var basePoints = CalculateBasePoints(netPaidAmount);
        var appliedMultiplier = GetMultiplier(currentTier);
        var pointsEarned = CalculatePointsEarned(netPaidAmount, appliedMultiplier);
        var availableAfter = checked(currentAvailablePoints + pointsEarned);
        var qualifyingAfter = checked(currentLifetimeQualifyingPoints + pointsEarned);
        var evaluatedTier = EvaluateTier(qualifyingAfter);
        var resultingTier = evaluatedTier > currentTier ? evaluatedTier : currentTier;

        return new LoyaltyAccrualPreview(
            BasePoints: basePoints,
            AppliedTier: currentTier,
            AppliedMultiplier: appliedMultiplier,
            PointsEarned: pointsEarned,
            AvailablePointsAfter: availableAfter,
            LifetimeQualifyingPointsAfter: qualifyingAfter,
            ResultingTier: resultingTier,
            TierUpgraded: resultingTier > currentTier);
    }

    public static VoucherRedemptionPreview EvaluateVoucherRedemption(
        int pointsCost,
        int currentAvailablePoints,
        int currentLifetimeQualifyingPoints,
        LoyaltyTierCode currentTier)
    {
        if (pointsCost <= 0)
        {
            throw new DomainException("INVALID_POINTS_COST", "Voucher points cost must be strictly positive.");
        }

        if (currentAvailablePoints < 0 || currentLifetimeQualifyingPoints < 0)
        {
            throw new DomainException("INVALID_POINT_BALANCE", "Point balances cannot be negative.");
        }

        if (pointsCost > currentAvailablePoints)
        {
            throw new BusinessRuleViolationException(
                "INSUFFICIENT_LOYALTY_POINTS",
                $"Insufficient available loyalty points: required {pointsCost}, available {currentAvailablePoints}.");
        }

        return new VoucherRedemptionPreview(
            PointsSpent: pointsCost,
            AvailablePointsAfter: currentAvailablePoints - pointsCost,
            LifetimeQualifyingPointsAfter: currentLifetimeQualifyingPoints,
            ResultingTier: currentTier);
    }

    public static decimal CalculateVoucherDiscount(
        string discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal grossAmount)
    {
        if (grossAmount < 0m)
        {
            throw new DomainException("INVALID_GROSS_AMOUNT", "Gross amount cannot be negative.");
        }

        if (discountValue < 0m)
        {
            throw new DomainException("INVALID_DISCOUNT_VALUE", "Discount value cannot be negative.");
        }

        if (grossAmount == 0m || discountValue == 0m)
        {
            return 0m;
        }

        decimal rawDiscount;
        if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            if (discountValue > 100m)
            {
                throw new DomainException("INVALID_DISCOUNT_PERCENTAGE", "Percentage discount cannot exceed 100.");
            }

            rawDiscount = Math.Round(grossAmount * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
            if (maxDiscountAmount.HasValue)
            {
                rawDiscount = Math.Min(rawDiscount, Math.Max(0m, maxDiscountAmount.Value));
            }
        }
        else if (string.Equals(discountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
        {
            rawDiscount = discountValue;
            if (maxDiscountAmount is > 0m)
            {
                rawDiscount = Math.Min(rawDiscount, maxDiscountAmount.Value);
            }
        }
        else
        {
            throw new DomainException("INVALID_DISCOUNT_TYPE", $"Unsupported voucher discount type '{discountType}'.");
        }

        return Math.Min(rawDiscount, grossAmount);
    }
}
