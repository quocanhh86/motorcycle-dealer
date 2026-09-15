using System.Globalization;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Domain;

public sealed record LoyaltyTierRule(
    string TierCode,
    string DisplayName,
    int RankOrder,
    int MinQualifyingPoints,
    decimal Multiplier);

public sealed record LoyaltyAccrualPreviewResult(
    int BasePoints,
    string AppliedTierCode,
    decimal AppliedMultiplier,
    int PointsEarned,
    int AvailablePointsAfter,
    int LifetimeQualifyingPointsAfter,
    string ResultingTierCode,
    bool TierUpgraded,
    string? NextTierCode,
    int PointsToNextTier);

/// <summary>
/// Deterministic two-step floor loyalty calculator mirroring backend LoyaltyPointsCalculator (ADR-003, FR-05).
/// Formula: PointsEarned = (int)Math.Floor(Math.Floor(NetPaidAmount / 10_000m) * TierMultiplier)
/// </summary>
public static class TwoStepLoyaltyPreviewCalculator
{
    public const decimal BaseUnitVnd = 10_000m;
    public const int ServiceReviewBonusPoints = 25;

    public const int BronzeMinQualifyingPoints = 0;
    public const int SilverMinQualifyingPoints = 500;
    public const int GoldMinQualifyingPoints = 2_000;
    public const int PlatinumMinQualifyingPoints = 5_000;

    public const decimal BronzeMultiplier = 1.00m;
    public const decimal SilverMultiplier = 1.20m;
    public const decimal GoldMultiplier = 1.50m;
    public const decimal PlatinumMultiplier = 2.00m;

    public static IReadOnlyList<LoyaltyTierRule> CanonicalTiers { get; } =
    [
        new("Bronze", "Bronze Member", 1, BronzeMinQualifyingPoints, BronzeMultiplier),
        new("Silver", "Silver Member", 2, SilverMinQualifyingPoints, SilverMultiplier),
        new("Gold", "Gold Member", 3, GoldMinQualifyingPoints, GoldMultiplier),
        new("Platinum", "Platinum Member", 4, PlatinumMinQualifyingPoints, PlatinumMultiplier)
    ];

    public static int CalculateBasePoints(decimal netPaidVnd)
    {
        if (netPaidVnd < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(netPaidVnd), "Net paid amount cannot be negative.");
        }

        return (int)Math.Floor(netPaidVnd / BaseUnitVnd);
    }

    public static int CalculatePointsEarned(decimal netPaidVnd, decimal tierMultiplier)
    {
        if (netPaidVnd < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(netPaidVnd), "Net paid amount cannot be negative.");
        }

        if (tierMultiplier < 1.00m || tierMultiplier > 10.00m)
        {
            throw new ArgumentOutOfRangeException(nameof(tierMultiplier), "Tier multiplier must be between 1.00 and 10.00.");
        }

        var basePoints = Math.Floor(netPaidVnd / BaseUnitVnd);
        return (int)Math.Floor(basePoints * tierMultiplier);
    }

    public static decimal GetMultiplier(string? tierCode) =>
        NormalizeTierCode(tierCode) switch
        {
            "Silver" => SilverMultiplier,
            "Gold" => GoldMultiplier,
            "Platinum" => PlatinumMultiplier,
            _ => BronzeMultiplier
        };

    public static int GetRankOrder(string? tierCode) =>
        NormalizeTierCode(tierCode) switch
        {
            "Silver" => 2,
            "Gold" => 3,
            "Platinum" => 4,
            _ => 1
        };

    public static string EvaluateTierCode(int lifetimeQualifyingPoints)
    {
        if (lifetimeQualifyingPoints < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeQualifyingPoints), "Lifetime qualifying points cannot be negative.");
        }

        return lifetimeQualifyingPoints switch
        {
            >= PlatinumMinQualifyingPoints => "Platinum",
            >= GoldMinQualifyingPoints => "Gold",
            >= SilverMinQualifyingPoints => "Silver",
            _ => "Bronze"
        };
    }

    public static (string? NextTierCode, int PointsRemaining) GetNextTierProgress(int lifetimeQualifyingPoints)
    {
        var safePoints = Math.Max(0, lifetimeQualifyingPoints);
        if (safePoints < SilverMinQualifyingPoints)
        {
            return ("Silver", SilverMinQualifyingPoints - safePoints);
        }

        if (safePoints < GoldMinQualifyingPoints)
        {
            return ("Gold", GoldMinQualifyingPoints - safePoints);
        }

        if (safePoints < PlatinumMinQualifyingPoints)
        {
            return ("Platinum", PlatinumMinQualifyingPoints - safePoints);
        }

        return (null, 0);
    }

    public static decimal CalculateVoucherDiscount(
        string discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal grossAmount)
    {
        if (grossAmount <= 0m || discountValue <= 0m)
        {
            return 0m;
        }

        decimal rawDiscount;
        if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            var clampedPct = Math.Min(100m, discountValue);
            rawDiscount = Math.Round(grossAmount * (clampedPct / 100m), 2, MidpointRounding.AwayFromZero);
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
            return 0m;
        }

        return Math.Min(rawDiscount, grossAmount);
    }

    public static (double ProgressRatio, string NextTierHint) CalculateTierProgress(
        int lifetimeQualifyingPoints,
        IReadOnlyList<LoyaltyTierDto>? tiers)
    {
        var ordered = OrderActiveTiers(tiers);
        if (ordered.Count == 0)
        {
            return (0d, AppResources.LoyaltySyncingTiers);
        }

        var safePoints = Math.Max(0, lifetimeQualifyingPoints);
        var currentTier = ordered.LastOrDefault(t => safePoints >= t.MinQualifyingPoints) ?? ordered[0];
        var nextTier = ordered.FirstOrDefault(t =>
                           t.MinQualifyingPoints > safePoints &&
                           t.RankOrder > currentTier.RankOrder)
                       ?? ordered.FirstOrDefault(t => t.MinQualifyingPoints > safePoints);

        if (nextTier is null)
        {
            return (1.0d, AppResources.LoyaltyTopTierUnlocked);
        }

        var floorPoints = currentTier.MinQualifyingPoints < nextTier.MinQualifyingPoints
            ? currentTier.MinQualifyingPoints
            : 0;
        var span = nextTier.MinQualifyingPoints - floorPoints;
        var ratio = span <= 0
            ? 1.0d
            : Math.Clamp((double)(safePoints - floorPoints) / span, 0d, 1d);
        var remaining = Math.Max(0, nextTier.MinQualifyingPoints - safePoints);
        var formattedRemaining = VietnamFormatting.FormatInteger(remaining);
        var formattedMultiplier = nextTier.PointsMultiplier.ToString("0.00", CultureInfo.InvariantCulture);
        var localizedNextTier = LocalizationService.Current.GetEnumLabel("LoyaltyTier", nextTier.TierCode);

        return (ratio, AppResources.Format("LoyaltyNextTierHintFormat", formattedRemaining, localizedNextTier, formattedMultiplier));
    }

    public static LoyaltyAccrualPreviewResult Preview(
        decimal netPaidVnd,
        int currentAvailablePoints,
        int currentLifetimeQualifyingPoints,
        string currentTierCode,
        decimal? overrideMultiplier = null,
        IReadOnlyList<LoyaltyTierDto>? tiers = null) =>
        EvaluateAccrualPreview(
            netPaidVnd,
            currentAvailablePoints,
            currentLifetimeQualifyingPoints,
            currentTierCode,
            overrideMultiplier,
            tiers);

    public static LoyaltyAccrualPreviewResult EvaluateAccrualPreview(
        decimal netPaidVnd,
        int currentAvailablePoints,
        int currentLifetimeQualifyingPoints,
        string currentTierCode,
        decimal? overrideMultiplier = null,
        IReadOnlyList<LoyaltyTierDto>? tiers = null)
    {
        var ordered = OrderActiveTiers(tiers);
        if (ordered.Count > 0)
        {
            var matchedCurrent = ordered.FirstOrDefault(t =>
                string.Equals(t.TierCode, currentTierCode?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? ordered.LastOrDefault(t => Math.Max(0, currentLifetimeQualifyingPoints) >= t.MinQualifyingPoints)
                ?? ordered[0];

            var appliedTierCode = matchedCurrent.TierCode;
            var liveMultiplier = overrideMultiplier ?? matchedCurrent.PointsMultiplier;
            var liveBasePoints = CalculateBasePoints(netPaidVnd);
            var liveEarned = CalculatePointsEarned(netPaidVnd, liveMultiplier);
            var liveAvailableAfter = checked(Math.Max(0, currentAvailablePoints) + liveEarned);
            var liveQualifyingAfter = checked(Math.Max(0, currentLifetimeQualifyingPoints) + liveEarned);

            var evaluatedLiveTier = ordered.LastOrDefault(t => liveQualifyingAfter >= t.MinQualifyingPoints) ?? ordered[0];
            var resultingLiveTier = evaluatedLiveTier.RankOrder > matchedCurrent.RankOrder
                ? evaluatedLiveTier
                : matchedCurrent;

            var nextLiveTier = ordered.FirstOrDefault(t =>
                                   t.MinQualifyingPoints > liveQualifyingAfter &&
                                   t.RankOrder > resultingLiveTier.RankOrder)
                               ?? ordered.FirstOrDefault(t => t.MinQualifyingPoints > liveQualifyingAfter);

            return new LoyaltyAccrualPreviewResult(
                BasePoints: liveBasePoints,
                AppliedTierCode: appliedTierCode,
                AppliedMultiplier: liveMultiplier,
                PointsEarned: liveEarned,
                AvailablePointsAfter: liveAvailableAfter,
                LifetimeQualifyingPointsAfter: liveQualifyingAfter,
                ResultingTierCode: resultingLiveTier.TierCode,
                TierUpgraded: resultingLiveTier.RankOrder > matchedCurrent.RankOrder,
                NextTierCode: nextLiveTier?.TierCode,
                PointsToNextTier: nextLiveTier is not null ? Math.Max(0, nextLiveTier.MinQualifyingPoints - liveQualifyingAfter) : 0);
        }

        var normalizedCurrent = NormalizeTierCode(currentTierCode);
        var multiplier = overrideMultiplier ?? GetMultiplier(normalizedCurrent);
        var basePoints = CalculateBasePoints(netPaidVnd);
        var earned = CalculatePointsEarned(netPaidVnd, multiplier);
        var availableAfter = checked(Math.Max(0, currentAvailablePoints) + earned);
        var qualifyingAfter = checked(Math.Max(0, currentLifetimeQualifyingPoints) + earned);
        var evaluatedTier = EvaluateTierCode(qualifyingAfter);
        var resultingTier = GetRankOrder(evaluatedTier) > GetRankOrder(normalizedCurrent)
            ? evaluatedTier
            : normalizedCurrent;
        var (nextTierCode, pointsToNext) = GetNextTierProgress(qualifyingAfter);

        return new LoyaltyAccrualPreviewResult(
            BasePoints: basePoints,
            AppliedTierCode: normalizedCurrent,
            AppliedMultiplier: multiplier,
            PointsEarned: earned,
            AvailablePointsAfter: availableAfter,
            LifetimeQualifyingPointsAfter: qualifyingAfter,
            ResultingTierCode: resultingTier,
            TierUpgraded: GetRankOrder(resultingTier) > GetRankOrder(normalizedCurrent),
            NextTierCode: nextTierCode,
            PointsToNextTier: pointsToNext);
    }

    private static IReadOnlyList<LoyaltyTierDto> OrderActiveTiers(IReadOnlyList<LoyaltyTierDto>? tiers)
    {
        if (tiers is null || tiers.Count == 0)
        {
            return [];
        }

        var active = tiers.Where(t => t.IsActive).ToArray();
        var source = active.Length > 0 ? active : tiers;
        return source
            .OrderBy(t => t.RankOrder)
            .ThenBy(t => t.MinQualifyingPoints)
            .ToArray();
    }

    public static string NormalizeTierCode(string? tierCode) =>
        tierCode?.Trim().ToUpperInvariant() switch
        {
            "SILVER" => "Silver",
            "GOLD" => "Gold",
            "PLATINUM" => "Platinum",
            _ => "Bronze"
        };
}
