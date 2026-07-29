using MotorcycleDealer.Application.Features.Inventories;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.Services;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests.Showroom;

public sealed class Phase5ShowroomGarageBoundaryTests
{
    [Theory]
    [InlineData("HND-AB160-2026", true)]
    [InlineData("YMH_EXC155", true)]
    [InlineData("ABC", true)]
    [InlineData("A12345678901234567890123456789", true)] // 30 chars
    [InlineData("AB", false)] // < 3 chars
    [InlineData("A123456789012345678901234567890", false)] // 31 chars
    [InlineData("hnd-ab160", false)] // lowercase before normalization
    [InlineData("HND AB160", false)] // whitespace forbidden
    public void ShowroomCodeRegex_ShouldEnforceLengthAndCharacterBoundaries(string code, bool expectedValid)
    {
        var isMatch = ShowroomValidationRules.CodeRegex().IsMatch(code);
        Assert.Equal(expectedValid, isMatch);
    }

    [Theory]
    [InlineData("scooter", true, "Scooter")]
    [InlineData("UNDERBONE", true, "Underbone")]
    [InlineData("Sport", true, "Sport")]
    [InlineData("naked", true, "Naked")]
    [InlineData("ADVENTURE", true, "Adventure")]
    [InlineData("Cruiser", true, "Cruiser")]
    [InlineData("", false, "")]
    [InlineData("   ", false, "")]
    [InlineData("Hatchback", false, "")]
    public void TryNormalizeCategory_ShouldNormalizeCanonicalCategoriesCaseInsensitively(
        string? rawCategory,
        bool expectedValid,
        string expectedNormalized)
    {
        var ok = ShowroomValidationRules.TryNormalizeCategory(rawCategory, out var normalized);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("{\"horsepower\":\"16.6 hp\",\"abs\":true}", true)]
    [InlineData("[\"Dual-Channel ABS\",\"Smart Key\"]", true)]
    [InlineData("12345", false)] // primitive number rejected
    [InlineData("\"raw-string\"", false)] // primitive string rejected
    [InlineData("true", false)] // primitive bool rejected
    [InlineData("{malformed-json", false)]
    public void IsValidOptionalJson_ShouldAcceptNullOrObjectOrArray_AndRejectPrimitivesOrMalformedJson(
        string? rawJson,
        bool expectedValid)
    {
        Assert.Equal(expectedValid, ShowroomValidationRules.IsValidOptionalJson(rawJson));
    }

    [Theory]
    [InlineData("AAAAAAAAAAA=", true)] // exact 8 zero bytes
    [InlineData("AQIDBAUGBwg=", true)] // exact 8 bytes [1..8]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("AAAA", false)] // 3 bytes
    [InlineData("AAAAAAAAA==", false)] // invalid padding / length
    [InlineData("AAAAAAAAAAAA", false)] // 9 bytes
    [InlineData("not-valid-base64$$$", false)]
    public void IsValidRowVersionBase64_ShouldRequireExact8ByteSqlRowVersionToken(
        string? rawToken,
        bool expectedValid)
    {
        Assert.Equal(expectedValid, ShowroomValidationRules.IsValidRowVersionBase64(rawToken));
    }

    [Theory]
    [InlineData("available", true, "Available")]
    [InlineData("RESERVED", true, "Reserved")]
    [InlineData("Sold", true, "Sold")]
    [InlineData("transferred", true, "Transferred")]
    [InlineData(null, false, "")]
    [InlineData("", false, "")]
    [InlineData("Damaged", false, "")]
    public void TryNormalizeStockStatus_ShouldAcceptOnlyCanonicalFourInventoryStatuses(
        string? rawStatus,
        bool expectedValid,
        string expectedNormalized)
    {
        var ok = InventoryValidationRules.TryNormalizeStockStatus(rawStatus, out var normalized);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData("JF51E-8000101", true)]
    [InlineData("AB12", true)] // min 4 chars
    [InlineData("ENG", false)] // < 4 chars
    [InlineData("jf51e-8000101", false)] // lowercase before normalization
    [InlineData("JF51E 8000101", false)] // space forbidden
    public void EngineNumberRegex_ShouldEnforceCanonicalFormat(string engineNumber, bool expectedValid)
    {
        Assert.Equal(expectedValid, InventoryValidationRules.EngineNumberRegex().IsMatch(engineNumber));
    }

    [Theory]
    [InlineData(0, 0, LoyaltyTierCode.Bronze, 0, LoyaltyTierCode.Bronze, false)]
    [InlineData(9_999, 0, LoyaltyTierCode.Bronze, 0, LoyaltyTierCode.Bronze, false)]
    [InlineData(10_000, 0, LoyaltyTierCode.Bronze, 1, LoyaltyTierCode.Bronze, false)]
    [InlineData(45_000_000, 0, LoyaltyTierCode.Bronze, 4_500, LoyaltyTierCode.Gold, true)]
    [InlineData(45_000_000, 120, LoyaltyTierCode.Bronze, 4_500, LoyaltyTierCode.Gold, true)]
    [InlineData(45_500_000, 2_000, LoyaltyTierCode.Gold, 6_825, LoyaltyTierCode.Platinum, true)]
    [InlineData(455_000, 2_000, LoyaltyTierCode.Gold, 67, LoyaltyTierCode.Gold, false)]
    [InlineData(101_000_000, 0, LoyaltyTierCode.Bronze, 10_100, LoyaltyTierCode.Platinum, true)]
    public void EvaluateAccrualPreview_ShowroomSaleBoundaries_ShouldMatchTwoStepFloorAndTierThresholds(
        decimal finalAmountVnd,
        int startingQualifyingPoints,
        LoyaltyTierCode currentTier,
        int expectedPointsEarned,
        LoyaltyTierCode expectedResultingTier,
        bool expectedTierUpgraded)
    {
        var preview = LoyaltyPointsCalculator.EvaluateAccrualPreview(
            netPaidAmount: finalAmountVnd,
            currentAvailablePoints: startingQualifyingPoints,
            currentLifetimeQualifyingPoints: startingQualifyingPoints,
            currentTier: currentTier);

        Assert.Equal(expectedPointsEarned, preview.PointsEarned);
        Assert.Equal(startingQualifyingPoints + expectedPointsEarned, preview.AvailablePointsAfter);
        Assert.Equal(startingQualifyingPoints + expectedPointsEarned, preview.LifetimeQualifyingPointsAfter);
        Assert.Equal(expectedResultingTier, preview.ResultingTier);
        Assert.Equal(expectedTierUpgraded, preview.TierUpgraded);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(-1, null, false)]
    [InlineData(1_000_001, null, false)]
    [InlineData(0, null, true)]
    [InlineData(null, 3_850, true)]
    [InlineData(1_000_000, null, true)]
    public void UpdateVehicleMileageRequestValidator_ShouldRejectMissingOrOutOfBoundsMileage(
        int? currentMileageKm,
        int? newMileageKm,
        bool expectedValid)
    {
        var validator = new UpdateVehicleMileageRequestValidator();
        var result = validator.Validate(new UpdateVehicleMileageRequest
        {
            CurrentMileageKm = currentMileageKm,
            NewMileageKm = newMileageKm
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("Cash", true, "Cash")]
    [InlineData("banktransfer", true, "BankTransfer")]
    [InlineData("CARD", true, "Card")]
    [InlineData("Installment", true, "Installment")]
    [InlineData(null, true, "BankTransfer")]
    [InlineData("", true, "BankTransfer")]
    [InlineData("Crypto", false, "")]
    public void TryNormalizePaymentMethod_ShouldEnforceCanonicalPaymentMethods(
        string? rawPaymentMethod,
        bool expectedValid,
        string expectedNormalized)
    {
        var ok = SalesOrderValidationRules.TryNormalizePaymentMethod(rawPaymentMethod, out var normalized);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedNormalized, normalized);
    }
}
