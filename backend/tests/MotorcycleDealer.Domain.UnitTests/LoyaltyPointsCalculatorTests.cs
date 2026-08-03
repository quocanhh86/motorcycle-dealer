using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.Services;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class LoyaltyPointsCalculatorTests
{
    [Theory]
    [InlineData(0.00, 1.00, 0)]
    [InlineData(9_999.99, 2.00, 0)]
    [InlineData(10_000.00, 1.00, 1)]
    [InlineData(159_999.00, 1.00, 15)]
    [InlineData(830_000.00, 1.20, 99)]
    [InlineData(455_000.00, 1.50, 67)]
    [InlineData(1_250_000.00, 2.00, 250)]
    [InlineData(101_000_000.00, 1.00, 10_100)]
    [InlineData(340_000.00, 1.20, 40)]
    [InlineData(200_000.00, 1.50, 30)]
    public void CalculatePointsEarned_ShouldApplyCanonicalTwoStepFloorFormula(
        decimal netPaidAmount,
        decimal tierMultiplier,
        int expectedPoints)
    {
        var points = LoyaltyPointsCalculator.CalculatePointsEarned(netPaidAmount, tierMultiplier);

        Assert.Equal(expectedPoints, points);
    }

    [Theory]
    [InlineData(0, LoyaltyTierCode.Bronze)]
    [InlineData(499, LoyaltyTierCode.Bronze)]
    [InlineData(500, LoyaltyTierCode.Silver)]
    [InlineData(1_999, LoyaltyTierCode.Silver)]
    [InlineData(2_000, LoyaltyTierCode.Gold)]
    [InlineData(4_999, LoyaltyTierCode.Gold)]
    [InlineData(5_000, LoyaltyTierCode.Platinum)]
    [InlineData(10_100, LoyaltyTierCode.Platinum)]
    public void EvaluateTier_ShouldPromoteAtExactQualifyingPointBoundaries(
        int lifetimeQualifyingPoints,
        LoyaltyTierCode expectedTier)
    {
        var tier = LoyaltyPointsCalculator.EvaluateTier(lifetimeQualifyingPoints);

        Assert.Equal(expectedTier, tier);
    }

    [Fact]
    public void EvaluateAccrualPreview_ShouldUsePreTransactionMultiplier_AndPromoteImmediatelyAfterEarn()
    {
        // Showroom purchase: Bronze customer (0 pts, 1.00x) pays 101,000,000 VND -> earns 10,100 pts -> jumps to Platinum
        var preview = LoyaltyPointsCalculator.EvaluateAccrualPreview(
            netPaidAmount: 101_000_000m,
            currentAvailablePoints: 0,
            currentLifetimeQualifyingPoints: 0,
            currentTier: LoyaltyTierCode.Bronze);

        Assert.Equal(10_100, preview.BasePoints);
        Assert.Equal(LoyaltyTierCode.Bronze, preview.AppliedTier);
        Assert.Equal(1.00m, preview.AppliedMultiplier);
        Assert.Equal(10_100, preview.PointsEarned);
        Assert.Equal(10_100, preview.AvailablePointsAfter);
        Assert.Equal(10_100, preview.LifetimeQualifyingPointsAfter);
        Assert.Equal(LoyaltyTierCode.Platinum, preview.ResultingTier);
        Assert.True(preview.TierUpgraded);
    }

    [Fact]
    public void EvaluateVoucherRedemption_ShouldDeductAvailablePointsOnly_AndNeverDowngradeTier()
    {
        var redemption = LoyaltyPointsCalculator.EvaluateVoucherRedemption(
            pointsCost: 300,
            currentAvailablePoints: 600,
            currentLifetimeQualifyingPoints: 600,
            currentTier: LoyaltyTierCode.Silver);

        Assert.Equal(300, redemption.PointsSpent);
        Assert.Equal(300, redemption.AvailablePointsAfter);
        Assert.Equal(600, redemption.LifetimeQualifyingPointsAfter);
        Assert.Equal(LoyaltyTierCode.Silver, redemption.ResultingTier);
    }

    [Fact]
    public void EvaluateVoucherRedemption_WhenAvailablePointsInsufficient_ShouldThrowBusinessRuleViolation()
    {
        var ex = Assert.Throws<BusinessRuleViolationException>(() =>
            LoyaltyPointsCalculator.EvaluateVoucherRedemption(
                pointsCost: 500,
                currentAvailablePoints: 300,
                currentLifetimeQualifyingPoints: 5_400,
                currentTier: LoyaltyTierCode.Platinum));

        Assert.Equal("INSUFFICIENT_LOYALTY_POINTS", ex.ErrorCode);
    }

    [Fact]
    public void CalculatePointsEarned_WithNegativeAmount_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            LoyaltyPointsCalculator.CalculatePointsEarned(-1m, LoyaltyTierCode.Bronze));

        Assert.Equal("INVALID_NET_PAID_AMOUNT", ex.ErrorCode);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(0.00)]
    [InlineData(-1.50)]
    [InlineData(10.01)]
    [InlineData(50.00)]
    public void CalculatePointsEarned_WithOutOfBoundsMultiplier_ShouldThrowDomainException(decimal invalidMultiplier)
    {
        var ex = Assert.Throws<DomainException>(() =>
            LoyaltyPointsCalculator.CalculatePointsEarned(100_000m, invalidMultiplier));

        Assert.Equal("INVALID_TIER_MULTIPLIER", ex.ErrorCode);
    }

    [Theory]
    [InlineData(1.00, 10)]
    [InlineData(10.00, 100)]
    public void CalculatePointsEarned_AtExactAllowedMultiplierBoundaries_ShouldSucceed(
        decimal boundaryMultiplier,
        int expectedPoints)
    {
        var points = LoyaltyPointsCalculator.CalculatePointsEarned(100_000m, boundaryMultiplier);

        Assert.Equal(expectedPoints, points);
    }

    [Theory]
    [InlineData('I')]
    [InlineData('O')]
    [InlineData('Q')]
    public void VoucherCodeSuffixAlphabet_ShouldContain33UniqueUpperAlnumChars_AndExcludeAmbiguousChars(char forbiddenChar)
    {
        var alphabet = LoyaltyPointsCalculator.VoucherCodeSuffixAlphabet;

        Assert.Equal(33, alphabet.Length);
        Assert.Equal(33, alphabet.Distinct().Count());
        Assert.DoesNotContain(forbiddenChar, alphabet);
        Assert.All(alphabet, ch => Assert.True((ch is >= 'A' and <= 'Z') || (ch is >= '0' and <= '9')));
    }

    [Theory]
    [InlineData("FixedAmount", 100_000.00, null, 930_000.00, 100_000.00)]
    [InlineData("FixedAmount", 100_000.00, null, 80_000.00, 80_000.00)]
    [InlineData("FixedAmount", 50_000.00, null, 0.00, 0.00)]
    [InlineData("Percentage", 15.00, null, 800_000.00, 120_000.00)]
    [InlineData("Percentage", 15.00, 200_000.00, 800_000.00, 120_000.00)]
    [InlineData("Percentage", 15.00, 200_000.00, 2_000_000.00, 200_000.00)]
    [InlineData("Percentage", 100.00, 500_000.00, 350_000.00, 350_000.00)]
    [InlineData("Percentage", 15.00, 200_000.00, 0.00, 0.00)]
    public void CalculateVoucherDiscount_ShouldHandleFixedAmountClamping_PercentageCaps_AndZeroGrossBoundary(
        string discountType,
        decimal discountValue,
        double? maxDiscountCapRaw,
        decimal grossAmount,
        decimal expectedDiscount)
    {
        decimal? maxDiscountAmount = maxDiscountCapRaw.HasValue ? (decimal)maxDiscountCapRaw.Value : null;

        var discount = LoyaltyPointsCalculator.CalculateVoucherDiscount(
            discountType,
            discountValue,
            maxDiscountAmount,
            grossAmount);

        Assert.Equal(expectedDiscount, discount);
    }

    [Theory]
    [InlineData(400_000.00, 549, 1_960, LoyaltyTierCode.Silver, 48, 2_008, LoyaltyTierCode.Gold, true)]
    [InlineData(400_000.00, 549, 1_950, LoyaltyTierCode.Silver, 48, 1_998, LoyaltyTierCode.Silver, false)]
    [InlineData(400_000.00, 1_450, 4_940, LoyaltyTierCode.Gold, 60, 5_000, LoyaltyTierCode.Platinum, true)]
    public void EvaluateAccrualPreview_AtExactTierPromotionBoundaries_ShouldResolveResultingTierDeterministically(
        decimal netPaidAmount,
        int currentAvailablePoints,
        int currentLifetimeQualifyingPoints,
        LoyaltyTierCode currentTier,
        int expectedPointsEarned,
        int expectedLifetimeAfter,
        LoyaltyTierCode expectedResultingTier,
        bool expectedTierUpgraded)
    {
        var preview = LoyaltyPointsCalculator.EvaluateAccrualPreview(
            netPaidAmount,
            currentAvailablePoints,
            currentLifetimeQualifyingPoints,
            currentTier);

        Assert.Equal(expectedPointsEarned, preview.PointsEarned);
        Assert.Equal(expectedLifetimeAfter, preview.LifetimeQualifyingPointsAfter);
        Assert.Equal(expectedResultingTier, preview.ResultingTier);
        Assert.Equal(expectedTierUpgraded, preview.TierUpgraded);
    }
}
