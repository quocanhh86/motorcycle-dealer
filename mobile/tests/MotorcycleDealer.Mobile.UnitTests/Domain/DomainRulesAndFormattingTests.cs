using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Media;
using Xunit;

namespace MotorcycleDealer.Mobile.UnitTests.Domain;

public sealed class DomainRulesAndFormattingTests
{
    // =========================================================================
    // ISO 3779 VIN Validator
    // =========================================================================

    [Theory]
    [InlineData("RLHJC7920NY123456", true, null)]
    [InlineData("rlhjc7920ny123456", true, null)]
    [InlineData("  JH2SC5901NK200145  ", true, null)]
    [InlineData("SHORT123", false, "INVALID_VIN_LENGTH")]
    [InlineData("RLHJC7920NY12345678", false, "INVALID_VIN_LENGTH")]
    [InlineData("RLHJC7920NY12345I", false, "INVALID_VIN_CHAR")]
    [InlineData("RLHJC7920NY12345O", false, "INVALID_VIN_CHAR")]
    [InlineData("RLHJC7920NY12345Q", false, "INVALID_VIN_CHAR")]
    [InlineData("RLHJC7920NY12345-", false, "INVALID_VIN_FORMAT")]
    [InlineData("", false, "INVALID_VIN_LENGTH")]
    public void Iso3779VinValidator_ValidatesAllBoundaryCases(string input, bool expectedValid, string? expectedErrorCode)
    {
        var result = Iso3779VinValidator.Validate(input);
        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
    }

    [Fact]
    public void Iso3779VinValidator_SanitizeLiveInput_StripsForbiddenCharsAndClampsTo17()
    {
        var sanitized = Iso3779VinValidator.SanitizeLiveInput("rlh-ioq-jc7920ny123456999");
        Assert.Equal("RLHJC7920NY123456", sanitized);
        Assert.Equal("17/17", Iso3779VinValidator.FormatLiveCounter(sanitized));
    }

    // =========================================================================
    // Two-Step Floor Loyalty Preview Calculator
    // =========================================================================

    [Theory]
    [InlineData(0, 1.00, 0)]
    [InlineData(9_999, 1.00, 0)]
    [InlineData(9_999, 2.00, 0)]
    [InlineData(10_000, 1.00, 1)]
    [InlineData(10_000, 1.20, 1)]
    [InlineData(455_000, 1.50, 67)] // Floor(Floor(455000/10000) * 1.50) = Floor(45 * 1.50) = Floor(67.5) = 67
    [InlineData(599_999, 1.20, 70)] // Floor(Floor(599999/10000) * 1.20) = Floor(59 * 1.20) = Floor(70.8) = 70 (NOT 71!)
    [InlineData(830_000, 1.20, 99)] // Floor(83 * 1.20) = Floor(99.6) = 99
    [InlineData(1_255_000, 1.50, 187)] // Floor(125 * 1.50) = Floor(187.5) = 187
    [InlineData(45_000_000, 2.00, 9_000)]
    public void TwoStepLoyaltyCalculator_ComputesExactTwoStepFloorPoints(decimal netPaidVnd, decimal multiplier, int expectedPoints)
    {
        var earned = TwoStepLoyaltyPreviewCalculator.CalculatePointsEarned(netPaidVnd, multiplier);
        Assert.Equal(expectedPoints, earned);
    }

    [Fact]
    public void MobileEndpointDefaults_ExposesExpectedTransportAndAdminConsoleUrls()
    {
        Assert.Equal("http://10.0.2.2:5080", MobileEndpointDefaults.EmulatorBaseUrl);
        Assert.Equal("http://localhost:5080", MobileEndpointDefaults.AdbReverseLocalhostBaseUrl);
        Assert.Equal("http://localhost:4200/admin/analytics", MobileEndpointDefaults.AdminWebConsoleUrl);
    }

    [Theory]
    [InlineData(0, "Bronze")]
    [InlineData(499, "Bronze")]
    [InlineData(500, "Silver")]
    [InlineData(1999, "Silver")]
    [InlineData(2000, "Gold")]
    [InlineData(4999, "Gold")]
    [InlineData(5000, "Platinum")]
    [InlineData(12500, "Platinum")]
    public void TwoStepLoyaltyCalculator_Evaluates4TierThresholdsExactly(int qualifyingPoints, string expectedTier)
    {
        Assert.Equal(expectedTier, TwoStepLoyaltyPreviewCalculator.EvaluateTierCode(qualifyingPoints));
    }

    [Fact]
    public void TwoStepLoyaltyCalculator_EvaluateAccrualPreview_DetectsTierUpgrade()
    {
        var preview = TwoStepLoyaltyPreviewCalculator.EvaluateAccrualPreview(
            netPaidVnd: 1_500_000m,
            currentAvailablePoints: 400,
            currentLifetimeQualifyingPoints: 400,
            currentTierCode: "Bronze");

        Assert.Equal(150, preview.BasePoints);
        Assert.Equal(150, preview.PointsEarned);
        Assert.Equal(550, preview.AvailablePointsAfter);
        Assert.Equal(550, preview.LifetimeQualifyingPointsAfter);
        Assert.Equal("Silver", preview.ResultingTierCode);
        Assert.True(preview.TierUpgraded);
        Assert.Equal("Gold", preview.NextTierCode);
        Assert.Equal(1450, preview.PointsToNextTier);
    }

    [Theory]
    [InlineData("Percentage", 15, 200_000, 2_000_000, 200_000)]
    [InlineData("Percentage", 10, null, 800_000, 80_000)]
    [InlineData("FixedAmount", 150_000, null, 600_000, 150_000)]
    [InlineData("FixedAmount", 500_000, null, 300_000, 300_000)]
    public void TwoStepLoyaltyCalculator_CalculatesVoucherDiscountsWithCaps(
        string discountType,
        decimal discountValue,
        int? maxCap,
        decimal gross,
        decimal expectedDiscount)
    {
        var actual = TwoStepLoyaltyPreviewCalculator.CalculateVoucherDiscount(
            discountType,
            discountValue,
            maxCap.HasValue ? (decimal)maxCap.Value : null,
            gross);

        Assert.Equal(expectedDiscount, actual);
    }

    // =========================================================================
    // Dealer QR / Barcode Parser & Builders
    // =========================================================================

    [Fact]
    public void QrPayloadParser_ParsesRawVinBarcode()
    {
        var parsed = DealerQrPayloadParser.Parse("RLHJC7920NY123456");
        Assert.Equal(QrPayloadKind.Vin, parsed.Kind);
        Assert.Equal("RLHJC7920NY123456", parsed.Vin);
    }

    [Fact]
    public void QrPayloadParser_RoundtripsVoucherDeepLinkPayload()
    {
        var voucherId = Guid.Parse("77777777-7777-7777-7777-777777777701");
        var qr = DealerQrPayloadParser.BuildVoucherQrPayload("SVC150-XK9M", voucherId, "AAAAAAAAB9F=");
        var parsed = DealerQrPayloadParser.Parse(qr);

        Assert.Equal(QrPayloadKind.Voucher, parsed.Kind);
        Assert.Equal("SVC150-XK9M", parsed.VoucherCode);
        Assert.Equal(voucherId, parsed.EntityId);
        Assert.Equal("AAAAAAAAB9F=", parsed.RowVersion);
    }

    [Fact]
    public void QrPayloadParser_RoundtripsBookingDeepLinkPayload()
    {
        var bookingId = Guid.Parse("88888888-8888-8888-8888-888888888802");
        var qr = DealerQrPayloadParser.BuildBookingQrPayload(bookingId, "BK-2026-0099", "AAAAAAAAD02=");
        var parsed = DealerQrPayloadParser.Parse(qr);

        Assert.Equal(QrPayloadKind.Booking, parsed.Kind);
        Assert.Equal(bookingId, parsed.EntityId);
        Assert.Equal("BK-2026-0099", parsed.DocumentNumber);
        Assert.Equal("AAAAAAAAD02=", parsed.RowVersion);
    }

    [Fact]
    public void QrPayloadParser_ParsesReceiptQrPipeAndHex16VerificationPayloads()
    {
        var invoiceId = Guid.Parse("99999999-9999-9999-9999-999999999903");
        var hash = DealerQrPayloadParser.ComputeServiceReceiptVerificationHash(invoiceId, "INV-2026-0001", 850_000m);
        Assert.Equal(16, hash.Length);

        var qr = DealerQrPayloadParser.BuildReceiptQrPayload("RCP-SVC-2026-0001", hash, "Service");
        var parsedQr = DealerQrPayloadParser.Parse(qr);
        Assert.Equal(QrPayloadKind.Receipt, parsedQr.Kind);
        Assert.Equal("RCP-SVC-2026-0001", parsedQr.DocumentNumber);
        Assert.Equal(hash, parsedQr.VerificationHash);

        var parsedPipe = DealerQrPayloadParser.Parse($"RCP-SVC-2026-0001|{hash}");
        Assert.Equal(QrPayloadKind.Receipt, parsedPipe.Kind);
        Assert.Equal("RCP-SVC-2026-0001", parsedPipe.DocumentNumber);
        Assert.Equal(hash, parsedPipe.VerificationHash);

        var parsedHex = DealerQrPayloadParser.Parse(hash);
        Assert.Equal(QrPayloadKind.Receipt, parsedHex.Kind);
        Assert.Equal(hash, parsedHex.VerificationHash);

        var unknown = DealerQrPayloadParser.Parse("https://example.com/unrelated");
        Assert.Equal(QrPayloadKind.Unknown, unknown.Kind);
        Assert.False(unknown.IsRecognized);
    }

    // =========================================================================
    // Vietnam Formatting (vi-VN VND & Asia/Ho_Chi_Minh UTC+07:00)
    // =========================================================================

    [Theory]
    [InlineData(0, "0 ₫")]
    [InlineData(250_000, "250.000 ₫")]
    [InlineData(254_990_000, "254.990.000 ₫")]
    public void VietnamFormatting_FormatsIntegerVndWithDotSeparators(decimal amount, string expected)
    {
        Assert.Equal(expected, VietnamFormatting.FormatVnd(amount));
    }

    [Fact]
    public void VietnamFormatting_ConvertsUtcToAsiaHoChiMinhUtcPlus7AcrossMidnight()
    {
        var utc = new DateTimeOffset(2026, 11, 24, 20, 30, 0, TimeSpan.Zero);
        Assert.Equal("25/11/2026 03:30", VietnamFormatting.FormatVietnamDateTime(utc));
        Assert.Equal("25/11/2026", VietnamFormatting.FormatVietnamDate(utc));
    }

    [Fact]
    public void VietnamFormatting_FormatsRelativeSyncAges()
    {
        var now = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("Never synced", VietnamFormatting.FormatRelativeSyncAge(null, now));
        Assert.Equal("Synced just now", VietnamFormatting.FormatRelativeSyncAge(now.AddSeconds(-15), now));
        Assert.Equal("Synced 12m ago", VietnamFormatting.FormatRelativeSyncAge(now.AddMinutes(-12), now));
        Assert.Equal("Synced 4h ago", VietnamFormatting.FormatRelativeSyncAge(now.AddHours(-4), now));
        Assert.Equal("Synced 2d ago", VietnamFormatting.FormatRelativeSyncAge(now.AddDays(-2), now));
    }

    // =========================================================================
    // Multi-Angle Studio Motorcycle Gallery Resolver (6 Flagship Models x 4 Angles)
    // =========================================================================

    [Theory]
    [InlineData("HND-AB160-2026", MotorcycleImageResolver.ModelHondaAirblade160, MotorcycleImageResolver.ModelHondaAirblade160Side, MotorcycleImageResolver.ModelHondaAirblade160Rear, MotorcycleImageResolver.ModelHondaAirblade160Cockpit)]
    [InlineData("HND-SH160I-2026", MotorcycleImageResolver.ModelHondaSh160iSport, MotorcycleImageResolver.ModelHondaSh160iSportSide, MotorcycleImageResolver.ModelHondaSh160iSportRear, MotorcycleImageResolver.ModelHondaSh160iSportCockpit)]
    [InlineData("HND-WINX-2026", MotorcycleImageResolver.ModelHondaWinnerX, MotorcycleImageResolver.ModelHondaWinnerXSide, MotorcycleImageResolver.ModelHondaWinnerXRear, MotorcycleImageResolver.ModelHondaWinnerXCockpit)]
    [InlineData("YMH-EXC155-2026", MotorcycleImageResolver.ModelYamahaExciter155, MotorcycleImageResolver.ModelYamahaExciter155Side, MotorcycleImageResolver.ModelYamahaExciter155Rear, MotorcycleImageResolver.ModelYamahaExciter155Cockpit)]
    [InlineData("HND-CB300R-2026", MotorcycleImageResolver.ModelHondaCb300r, MotorcycleImageResolver.ModelHondaCb300rSide, MotorcycleImageResolver.ModelHondaCb300rRear, MotorcycleImageResolver.ModelHondaCb300rCockpit)]
    [InlineData("HND-NX500-2026", MotorcycleImageResolver.ModelHondaNx500, MotorcycleImageResolver.ModelHondaNx500Side, MotorcycleImageResolver.ModelHondaNx500Rear, MotorcycleImageResolver.ModelHondaNx500Cockpit)]
    public void MotorcycleImageResolver_ResolveModelGallery_ReturnsAll4StudioAnglesForFlagshipModels(
        string modelCode,
        string expectedFront,
        string expectedSide,
        string expectedRear,
        string expectedCockpit)
    {
        var gallery = MotorcycleImageResolver.ResolveModelGallery(modelCode);

        Assert.Equal(4, gallery.Count);
        Assert.Equal(["front", "side", "rear", "cockpit"], gallery.Select(g => g.AngleKey).ToArray());
        Assert.Equal([expectedFront, expectedSide, expectedRear, expectedCockpit], gallery.Select(g => g.ImageSource).ToArray());
        Assert.Equal(
            ["ShowroomAngleFront", "ShowroomAngleSide", "ShowroomAngleRear", "ShowroomAngleCockpit"],
            gallery.Select(g => g.LabelResourceKey).ToArray());
    }

    [Fact]
    public void MotorcycleImageResolver_ResolveModelGallery_CustomExternalUrlFallsBackToSingleFrontAngle()
    {
        const string customUrl = "https://cdn.example.vn/models/custom-tourer-2027.jpg";
        var gallery = MotorcycleImageResolver.ResolveModelGallery("CUSTOM-2027", customUrl);

        var single = Assert.Single(gallery);
        Assert.Equal("front", single.AngleKey);
        Assert.Equal(customUrl, single.ImageSource);
        Assert.Equal("ShowroomAngleFront", single.LabelResourceKey);
    }
}
