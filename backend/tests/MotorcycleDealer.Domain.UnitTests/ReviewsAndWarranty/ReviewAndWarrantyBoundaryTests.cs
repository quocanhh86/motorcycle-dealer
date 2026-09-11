using System.Reflection;
using System.Text.Json;
using MotorcycleDealer.Application.Features.Loyalty;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Services.Reviews;
using MotorcycleDealer.Infrastructure.Services.Showroom;
using MotorcycleDealer.Infrastructure.Services.Warranty;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests.ReviewsAndWarranty;

public sealed class ReviewAndWarrantyBoundaryTests
{
    private const string ValidRowVersionBase64 = "AAAAAAAAAAA=";

    [Fact]
    public void CanonicalConstants_AndDependencyInjectionRegistration_ShouldMatchSpecification()
    {
        Assert.Equal(25, ServiceReviewConstants.StandardReviewBonusPoints);
        Assert.Equal(25, ServiceReviewValidationRules.StandardReviewBonusPoints);
        Assert.Equal(25, ServiceReviewService.StandardReviewBonusPoints);
        Assert.Equal(8, ServiceReviewValidationRules.CanonicalQualityTags.Count);

        Assert.Equal(36, WarrantyClaimConstants.MaxWarrantyMonths);
        Assert.Equal(30_000, WarrantyClaimConstants.MaxWarrantyMileageKm);
        Assert.Equal(36, WarrantyClaimValidationRules.MaxWarrantyMonths);
        Assert.Equal(30_000, WarrantyClaimValidationRules.MaxWarrantyMileageKm);
        Assert.Equal(36, WarrantyClaimService.MaxWarrantyMonths);
        Assert.Equal(30_000, WarrantyClaimService.MaxWarrantyMileageKm);

        var diMethods = typeof(DependencyInjection)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AddReviewAndWarrantyServices", diMethods);

        var entityId = Guid.Parse("01980000-0000-7000-8000-00000012ab34");
        var stamp = new DateTimeOffset(2026, 8, 20, 10, 30, 0, TimeSpan.Zero);
        Assert.Equal("REV-20260820-12AB34", VoucherAndDocumentNumberHelper.FormatDocumentNumber("REV", stamp, entityId));
        Assert.Equal("WC-20260820-12AB34", VoucherAndDocumentNumberHelper.FormatDocumentNumber("WC", stamp, entityId));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void CreateServiceReviewRequestValidator_ShouldEnforceStarRatingBounds_1To5(
        int rating,
        bool expectedValid)
    {
        var validator = new CreateServiceReviewRequestValidator();
        var result = validator.Validate(new CreateServiceReviewRequest
        {
            ServiceInvoiceId = Guid.NewGuid(),
            Rating = rating,
            QualityTags = "FastTurnaround,GenuineParts",
            Comment = "Smooth workshop service experience."
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void CreateServiceReviewRequestValidator_ShouldEnforceCommentMaxLength1000(
        int commentLength,
        bool expectedValid)
    {
        var validator = new CreateServiceReviewRequestValidator();
        var result = validator.Validate(new CreateServiceReviewRequest
        {
            ServiceInvoiceId = Guid.NewGuid(),
            Rating = 5,
            Comment = commentLength == 0 ? null : new string('C', commentLength)
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(null, true, null, 0)]
    [InlineData("   ", true, null, 0)]
    [InlineData(", ; |  ", true, null, 0)]
    [InlineData("fastturnaround, GENUINEPARTS , fastturnaround", true, "FastTurnaround,GenuineParts", 2)]
    [InlineData("  cleanlounge ; expertechnician ", false, null, 0)] // typo rejected
    [InlineData("  cleanlounge ; experttechnician ; friendlyadvisor ", true, "CleanLounge,ExpertTechnician,FriendlyAdvisor", 3)]
    [InlineData("TransparentPricing | ClearExplanation | ThoroughInspection | transparentpricing", true, "TransparentPricing,ClearExplanation,ThoroughInspection", 3)]
    [InlineData("FastTurnaround,TransparentPricing,ExpertTechnician,CleanLounge,GenuineParts,FriendlyAdvisor,ClearExplanation,ThoroughInspection", true, "FastTurnaround,TransparentPricing,ExpertTechnician,CleanLounge,GenuineParts,FriendlyAdvisor,ClearExplanation,ThoroughInspection", 8)]
    [InlineData("FastTurnaround,TransparentPricing,ExpertTechnician,CleanLounge,GenuineParts,FriendlyAdvisor,ClearExplanation,ThoroughInspection,FastTurnaround", false, null, 0)] // 9 raw segments (> 8 cap)
    [InlineData("FastTurnaround,UnknownTag", false, null, 0)]
    public void ServiceReviewValidationRules_TryNormalizeQualityTags_ShouldEnforceCanonicalCatalogDelimitersAndMax8Tags(
        string? rawTags,
        bool expectedValid,
        string? expectedNormalizedCsv,
        int expectedListCount)
    {
        var ok = ServiceReviewValidationRules.TryNormalizeQualityTags(
            rawTags,
            out var normalizedCsv,
            out var normalizedList,
            out var error);

        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedNormalizedCsv, normalizedCsv);
        Assert.Equal(expectedListCount, normalizedList.Count);
        if (!expectedValid)
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    [Theory]
    [InlineData("  FastTurnaround ; GenuineParts ", true, false, "FastTurnaround,GenuineParts")]   // QualityTags primary wins over QualityTagList
    [InlineData("   ", true, true, "CleanLounge,ExpertTechnician")]                                  // QualityTagList fallback wins over Tags
    [InlineData(null, false, true, "FriendlyAdvisor,ClearExplanation")]                              // Tags fallback used when QualityTags & QualityTagList empty
    [InlineData("   ", false, false, null)]                                                          // All empty -> null
    public void CreateServiceReviewRequest_AliasAndDelimiterMatrix_ShouldResolveAndNormalizeDeterministically(
        string? qualityTags,
        bool populateQualityTagList,
        bool populateTagsFallback,
        string? expectedNormalizedCsv)
    {
        var req = new CreateServiceReviewRequest
        {
            ServiceInvoiceId = Guid.NewGuid(),
            Rating = 5,
            QualityTags = qualityTags,
            QualityTagList = populateQualityTagList ? ["  cleanlounge ", "ExpertTechnician"] : null,
            Tags = populateTagsFallback ? ["friendlyadvisor", "  ClearExplanation  "] : null
        };

        var ok = ServiceReviewValidationRules.TryNormalizeQualityTags(
            req.ResolveRawQualityTags(),
            out var normalizedCsv,
            out _,
            out _);

        Assert.True(ok);
        Assert.Equal(expectedNormalizedCsv, normalizedCsv);
    }

    [Fact]
    public void CreateServiceReviewRequest_ShouldSupportBothStringAndArrayQualityTagsJsonPayloads_RejectInvalidJsonToken_AndRejectOver200Chars()
    {
        var invoiceId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var fromArrayJson = JsonSerializer.Deserialize<CreateServiceReviewRequest>(
            $$"""{"serviceInvoiceId":"{{invoiceId}}","rating":5,"qualityTags":["FastTurnaround","ExpertTechnician","GenuineParts"]}""",
            jsonOpts)!;

        var fromCsvJson = JsonSerializer.Deserialize<CreateServiceReviewRequest>(
            $$"""{"invoiceId":"{{invoiceId}}","rating":5,"qualityTags":"FastTurnaround,ExpertTechnician,GenuineParts"}""",
            jsonOpts)!;

        var fromNullJson = JsonSerializer.Deserialize<CreateServiceReviewRequest>(
            $$"""{"serviceInvoiceId":"{{invoiceId}}","rating":4,"qualityTags":null}""",
            jsonOpts)!;

        Assert.Equal(invoiceId, fromArrayJson.ResolveEffectiveInvoiceId());
        Assert.Equal(invoiceId, fromCsvJson.ResolveEffectiveInvoiceId());
        Assert.Equal("FastTurnaround,ExpertTechnician,GenuineParts", fromArrayJson.QualityTags);
        Assert.Equal("FastTurnaround,ExpertTechnician,GenuineParts", fromCsvJson.QualityTags);
        Assert.Null(fromNullJson.QualityTags);

        // Invalid non-string / non-array JSON token (e.g., object or number) must throw JsonException
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateServiceReviewRequest>(
                $$"""{"serviceInvoiceId":"{{invoiceId}}","rating":5,"qualityTags":12345}""",
                jsonOpts));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateServiceReviewRequest>(
                $$$"""{"serviceInvoiceId":"{{{invoiceId}}}","rating":5,"qualityTags":{"tag":"FastTurnaround"}}""",
                jsonOpts));

        var validator = new CreateServiceReviewRequestValidator();
        Assert.True(validator.Validate(fromArrayJson).IsValid);
        Assert.True(validator.Validate(fromCsvJson).IsValid);

        var over200Chars = new CreateServiceReviewRequest
        {
            ServiceInvoiceId = invoiceId,
            Rating = 5,
            QualityTags = new string('A', 201)
        };
        Assert.False(validator.Validate(over200Chars).IsValid);
    }

    [Theory]
    [InlineData(0, false, ValidRowVersionBase64, false)]
    [InlineData(4, false, ValidRowVersionBase64, false)]    // 4 chars (< 5 min) rejected
    [InlineData(5, false, ValidRowVersionBase64, true)]     // exact 5-char min accepted
    [InlineData(1000, false, ValidRowVersionBase64, true)]  // exact 1000-char max accepted
    [InlineData(1001, false, ValidRowVersionBase64, false)] // 1001 chars (> 1000 max) rejected
    [InlineData(5, true, ValidRowVersionBase64, true)]      // Response alias accepted
    [InlineData(20, false, "", false)]                      // missing RowVersion rejected
    [InlineData(20, false, "not-base64", false)]            // malformed RowVersion rejected
    public void RespondToServiceReviewRequestValidator_ShouldEnforce5To1000CharResponseAnd8ByteRowVersion(
        int responseLength,
        bool useResponseAlias,
        string rowVersion,
        bool expectedValid)
    {
        var validator = new RespondToServiceReviewRequestValidator();
        var text = responseLength == 0 ? string.Empty : new string('R', responseLength);
        var request = useResponseAlias
            ? new RespondToServiceReviewRequest { StaffResponse = "   ", Response = text, RowVersion = rowVersion }
            : new RespondToServiceReviewRequest { StaffResponse = text, RowVersion = rowVersion };

        var result = validator.Validate(request);
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0, 0, 0, true)]          // brand-new 0 km at purchase timestamp
    [InlineData(29_999, 35, 0, true)]    // 29,999 km within 36m
    [InlineData(30_000, 36, 0, true)]    // exact 30,000 km & exact 36-month boundary
    [InlineData(30_001, 12, 0, false)]   // 30,001 km exceeds 30,000 km cap
    [InlineData(12_500, 36, 1, false)]   // 36 months + 1 second exceeds 36-month window
    [InlineData(30_000, 37, 0, false)]   // 37 months expired despite <= 30,000 km
    [InlineData(-1, 1, 0, false)]        // negative odometer invalid
    public void WarrantyClaimValidationRules_IsWithinWarrantyCoverage_ShouldEnforceExact36MonthAnd30000KmBoundaries(
        int odometerKm,
        int monthsElapsed,
        int extraSecondsElapsed,
        bool expectedCovered)
    {
        var purchaseStartUtc = new DateTimeOffset(2024, 2, 15, 8, 30, 0, TimeSpan.Zero);
        var evaluatedAtUtc = purchaseStartUtc.AddMonths(monthsElapsed).AddSeconds(extraSecondsElapsed);

        var actual = WarrantyClaimValidationRules.IsWithinWarrantyCoverage(
            purchaseStartUtc,
            evaluatedAtUtc,
            odometerKm);

        Assert.Equal(expectedCovered, actual);
    }

    [Theory]
    [InlineData("2024-02-29T08:30:00Z", "2027-02-28T08:30:00Z", 30_000, true)]   // Leap-day purchase + 36m -> exact 2027-02-28 boundary covered
    [InlineData("2024-02-29T08:30:00Z", "2027-02-28T08:30:01Z", 10_000, false)]  // Leap-day purchase + 36m + 1s -> expired
    [InlineData("2024-02-29T08:30:00Z", "2027-02-28T08:30:00Z", 30_001, false)]  // Leap-day exact date but 30,001 km -> expired
    public void WarrantyClaimValidationRules_LeapYear36MonthExpiry_ShouldClampFeb29ToFeb28Deterministically(
        string startIsoUtc,
        string evalIsoUtc,
        int odometerKm,
        bool expectedCovered)
    {
        var startUtc = DateTimeOffset.Parse(startIsoUtc);
        var evalUtc = DateTimeOffset.Parse(evalIsoUtc);
        var computedExpiry = WarrantyClaimValidationRules.ComputeWarrantyExpiresAtUtc(startUtc);

        Assert.Equal(new DateTimeOffset(2027, 2, 28, 8, 30, 0, TimeSpan.Zero), computedExpiry);
        Assert.Equal(expectedCovered, WarrantyClaimValidationRules.IsWithinWarrantyCoverage(startUtc, evalUtc, odometerKm));
    }

    [Fact]
    public void WarrantyClaimValidationRules_ResolveWarrantyStartUtc_ShouldFollowCanonicalFallbackOrder()
    {
        var ownershipUtc = new DateTimeOffset(2025, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var salesCompletedUtc = new DateTimeOffset(2025, 3, 9, 0, 0, 0, TimeSpan.Zero);
        var createdUtc = new DateTimeOffset(2025, 3, 12, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            ownershipUtc,
            WarrantyClaimValidationRules.ResolveWarrantyStartUtc(ownershipUtc, createdUtc, salesCompletedUtc));

        Assert.Equal(
            salesCompletedUtc,
            WarrantyClaimValidationRules.ResolveWarrantyStartUtc(default, createdUtc, salesCompletedUtc));

        Assert.Equal(
            createdUtc,
            WarrantyClaimValidationRules.ResolveWarrantyStartUtc(default, createdUtc, null));
    }

    [Theory]
    [InlineData("submitted", true, WarrantyClaimStatus.Submitted)]
    [InlineData("UNDERREVIEW", true, WarrantyClaimStatus.UnderReview)]
    [InlineData("  Approved ", true, WarrantyClaimStatus.Approved)]
    [InlineData("completed", true, WarrantyClaimStatus.Completed)]
    [InlineData("ReJeCtEd", true, WarrantyClaimStatus.Rejected)]
    [InlineData("Pending", false, default(WarrantyClaimStatus))]
    [InlineData("Cancelled", false, default(WarrantyClaimStatus))]
    [InlineData("   ", false, default(WarrantyClaimStatus))]
    [InlineData(null, false, default(WarrantyClaimStatus))]
    public void WarrantyClaimValidationRules_TryNormalizeStatus_ShouldEnforceCaseInsensitiveCanonicalStatuses(
        string? rawStatus,
        bool expectedValid,
        WarrantyClaimStatus expectedStatus)
    {
        var ok = WarrantyClaimValidationRules.TryNormalizeStatus(rawStatus, out var parsed);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedStatus, parsed);
    }

    [Theory]
    // Allowed forward transitions
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.UnderReview, true)]
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.Approved, true)]
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.Rejected, true)]
    [InlineData(WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Approved, true)]
    [InlineData(WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Rejected, true)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.Completed, true)]
    // Disallowed self / skip / backward / terminal transitions
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.Submitted, false)]
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.Completed, false)]
    [InlineData(WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Submitted, false)]
    [InlineData(WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.UnderReview, false)]
    [InlineData(WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Completed, false)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.Submitted, false)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.UnderReview, false)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.Approved, false)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.Rejected, false)]
    [InlineData(WarrantyClaimStatus.Completed, WarrantyClaimStatus.Submitted, false)]
    [InlineData(WarrantyClaimStatus.Completed, WarrantyClaimStatus.Approved, false)]
    [InlineData(WarrantyClaimStatus.Completed, WarrantyClaimStatus.Rejected, false)]
    [InlineData(WarrantyClaimStatus.Rejected, WarrantyClaimStatus.Submitted, false)]
    [InlineData(WarrantyClaimStatus.Rejected, WarrantyClaimStatus.UnderReview, false)]
    [InlineData(WarrantyClaimStatus.Rejected, WarrantyClaimStatus.Approved, false)]
    [InlineData(WarrantyClaimStatus.Rejected, WarrantyClaimStatus.Completed, false)]
    public void WarrantyClaimValidationRules_IsAllowedStatusTransition_ShouldEnforceStrictLifecycleGraph(
        WarrantyClaimStatus current,
        WarrantyClaimStatus next,
        bool expectedAllowed)
    {
        Assert.Equal(expectedAllowed, WarrantyClaimValidationRules.IsAllowedStatusTransition(current, next));
    }

    [Theory]
    [InlineData("engine", true, WarrantyDefectCategory.Engine)]
    [InlineData("TRANSMISSION", true, WarrantyDefectCategory.Transmission)]
    [InlineData("Electrical", true, WarrantyDefectCategory.Electrical)]
    [InlineData("brakesandsuspension", true, WarrantyDefectCategory.BrakesAndSuspension)]
    [InlineData("FrameAndPaint", true, WarrantyDefectCategory.FrameAndPaint)]
    [InlineData("exhaustandfuel", true, WarrantyDefectCategory.ExhaustAndFuel)]
    [InlineData("CosmeticWear", false, default(WarrantyDefectCategory))]
    [InlineData("", false, default(WarrantyDefectCategory))]
    public void WarrantyClaimValidationRules_TryNormalizeDefectCategory_ShouldMatchAllSixCanonicalCategories(
        string rawCategory,
        bool expectedValid,
        WarrantyDefectCategory expectedCategory)
    {
        var ok = WarrantyClaimValidationRules.TryNormalizeDefectCategory(rawCategory, out var parsed);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedCategory, parsed);
    }

    [Theory]
    [InlineData(0, "Electrical", 9, false)]     // SymptomDescription 9 chars (< 10) rejected
    [InlineData(0, "Electrical", 10, true)]     // exact 10-char min & 0 km accepted
    [InlineData(15_000, "Engine", 1000, true)]  // exact 1000-char max accepted
    [InlineData(15_000, "Engine", 1001, false)] // 1001 chars (> 1000) rejected
    [InlineData(-1, "Engine", 25, false)]       // negative odometer rejected
    [InlineData(5_000, "InvalidCat", 25, false)]// unknown defect category rejected
    public void CreateWarrantyClaimRequestValidator_ShouldEnforceOdometerCategoryAndSymptomBounds(
        int odometerKm,
        string category,
        int symptomLength,
        bool expectedValid)
    {
        var validator = new CreateWarrantyClaimRequestValidator();
        var result = validator.Validate(new CreateWarrantyClaimRequest
        {
            CustomerVehicleId = Guid.NewGuid(),
            DealerId = Guid.NewGuid(),
            OdometerAtClaimKm = odometerKm,
            DefectCategory = category,
            SymptomDescription = new string('S', symptomLength)
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("Approved", "Completed", true, true, "Primary Resolution", "Fallback Notes", "Primary Reject", "Fallback Reason", "Completed", true, "Primary Resolution", "Primary Reject")]
    [InlineData("Approved", "   ", false, true, "   ", "Fallback Notes", "   ", "Fallback Reason", "Approved", true, "Fallback Notes", "Fallback Reason")]
    [InlineData(null, "Rejected", false, false, null, null, null, "Fallback Reason", "Rejected", false, null, "Fallback Reason")]
    public void UpdateWarrantyClaimStatusRequest_AliasResolvers_ShouldCascadeDeterministicallyAndSupportNullCoveredAmount(
        string? status,
        string? newStatus,
        bool hasPrimaryPartId,
        bool hasFallbackSparePartId,
        string? resolutionNotes,
        string? notes,
        string? rejectionReason,
        string? reason,
        string expectedStatus,
        bool expectedPartResolved,
        string? expectedResolution,
        string? expectedRejection)
    {
        var primaryPart = Guid.Parse("12121212-1212-1212-1212-121212121207");
        var fallbackPart = Guid.Parse("12121212-1212-1212-1212-121212121208");

        var req = new UpdateWarrantyClaimStatusRequest
        {
            Status = status,
            NewStatus = newStatus,
            CoveredSparePartId = hasPrimaryPartId ? primaryPart : Guid.Empty,
            SparePartId = hasFallbackSparePartId ? fallbackPart : null,
            CoveredAmountVnd = null,
            ResolutionNotes = resolutionNotes,
            Notes = notes,
            RejectionReason = rejectionReason,
            Reason = reason,
            RowVersion = ValidRowVersionBase64
        };

        Assert.Equal(expectedStatus, req.ResolveEffectiveStatus());
        Assert.Equal(expectedResolution, req.ResolveEffectiveResolutionNotes());
        Assert.Equal(expectedRejection, req.ResolveEffectiveRejectionReason());

        var resolvedPart = req.ResolveEffectiveSparePartId();
        Assert.Equal(expectedPartResolved, resolvedPart.HasValue);
        if (hasPrimaryPartId)
        {
            Assert.Equal(primaryPart, resolvedPart);
        }
        else if (hasFallbackSparePartId)
        {
            Assert.Equal(fallbackPart, resolvedPart);
        }

        // Verify validator accepts CoveredAmountVnd == null on Approved & Completed
        var validator = new UpdateWarrantyClaimStatusRequestValidator();
        Assert.True(validator.Validate(req).IsValid);
    }

    [Theory]
    [InlineData("UnderReview", 0, null, null, ValidRowVersionBase64, true)]
    [InlineData("Approved", 350_000, null, null, ValidRowVersionBase64, true)]
    [InlineData("Approved", -1, null, null, ValidRowVersionBase64, false)]         // negative CoveredAmountVnd rejected
    [InlineData("Completed", 350_000, 4, null, ValidRowVersionBase64, false)]      // Completed with 4-char ResolutionNotes rejected
    [InlineData("Completed", 350_000, 5, null, ValidRowVersionBase64, true)]       // Completed with 5-char ResolutionNotes accepted
    [InlineData("Completed", 350_000, 1000, null, ValidRowVersionBase64, true)]    // Completed with 1000-char ResolutionNotes accepted
    [InlineData("Completed", 350_000, 1001, null, ValidRowVersionBase64, false)]   // Completed with 1001-char ResolutionNotes rejected
    [InlineData("Rejected", 0, null, 4, ValidRowVersionBase64, false)]             // Rejected with 4-char RejectionReason rejected
    [InlineData("Rejected", 0, null, 5, ValidRowVersionBase64, true)]              // Rejected with 5-char RejectionReason accepted
    [InlineData("Rejected", 0, null, 500, ValidRowVersionBase64, true)]            // Rejected with 500-char RejectionReason accepted
    [InlineData("Rejected", 0, null, 501, ValidRowVersionBase64, false)]           // Rejected with 501-char RejectionReason rejected
    [InlineData("Approved", 0, null, null, "bad-token", false)]                    // invalid RowVersion rejected
    public void UpdateWarrantyClaimStatusRequestValidator_ShouldEnforceConditionalNotesAmountsAndRowVersion(
        string status,
        decimal coveredAmountVnd,
        int? resolutionLength,
        int? rejectionLength,
        string rowVersion,
        bool expectedValid)
    {
        var validator = new UpdateWarrantyClaimStatusRequestValidator();
        var result = validator.Validate(new UpdateWarrantyClaimStatusRequest
        {
            Status = status,
            CoveredAmountVnd = coveredAmountVnd,
            ResolutionNotes = resolutionLength.HasValue ? new string('N', resolutionLength.Value) : null,
            RejectionReason = rejectionLength.HasValue ? new string('X', rejectionLength.Value) : null,
            RowVersion = rowVersion
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("SalesOrderEarn", true, "SalesOrderEarn")]
    [InlineData("  salesorderearn  ", true, "SalesOrderEarn")]
    [InlineData("SALESORDEREARN", true, "SalesOrderEarn")]
    [InlineData("ServiceInvoiceEarn", true, "ServiceInvoiceEarn")]
    [InlineData("  serviceinvoiceearn ", true, "ServiceInvoiceEarn")]
    [InlineData("VoucherRedeem", true, "VoucherRedeem")]
    [InlineData(" VOUCHERREDEEM ", true, "VoucherRedeem")]
    [InlineData("VoucherExpireRefund", true, "VoucherExpireRefund")]
    [InlineData("  voucherexpirerefund ", true, "VoucherExpireRefund")]
    [InlineData("AdminAdjustment", true, "AdminAdjustment")]
    [InlineData(" adminadjustment ", true, "AdminAdjustment")]
    [InlineData("WelcomeBonus", true, "WelcomeBonus")]
    [InlineData("  WELCOMEBONUS  ", true, "WelcomeBonus")]
    [InlineData("ServiceReviewBonus", true, "ServiceReviewBonus")]
    [InlineData("  servicereviewbonus  ", true, "ServiceReviewBonus")]
    [InlineData("SERVICEREVIEWBONUS", true, "ServiceReviewBonus")]
    [InlineData("UnknownBonus", false, "")]
    [InlineData("WarrantyBonus", false, "")]
    public void PointLedgerQueryValidator_ShouldAcceptServiceReviewBonusAndAllCanonicalTransactionTypes(
        string rawType,
        bool expectedValid,
        string expectedNormalized)
    {
        // Confirm 1:1 coverage across all 7 LedgerTransactionType enum values
        foreach (var enumVal in Enum.GetValues<LedgerTransactionType>())
        {
            Assert.True(LoyaltyValidationRules.TryNormalizeTransactionType(enumVal.ToString(), out var canonical));
            Assert.Equal(enumVal.ToString(), canonical);
        }

        Assert.False(LoyaltyValidationRules.TryNormalizeTransactionType(null, out _));
        Assert.False(LoyaltyValidationRules.TryNormalizeTransactionType("   ", out _));

        var ok = LoyaltyValidationRules.TryNormalizeTransactionType(rawType, out var normalized);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedNormalized, normalized);

        var validator = new PointLedgerQueryValidator();
        var validationResult = validator.Validate(new PointLedgerQuery(
            PageNumber: 1,
            PageSize: 20,
            TransactionType: rawType));
        Assert.Equal(expectedValid, validationResult.IsValid);
    }

    [Theory]
    [InlineData(null, null, 1, 20, true)]
    [InlineData(1, null, 1, 20, true)]
    [InlineData(3, null, 1, 20, true)]
    [InlineData(5, null, 1, 20, true)]
    [InlineData(null, 1, 1, 20, true)]
    [InlineData(null, 3, 1, 20, true)]
    [InlineData(null, 5, 1, 20, true)]
    [InlineData(5, 4, 1, 100, true)]
    [InlineData(-1, null, 1, 20, false)]
    [InlineData(0, null, 1, 20, false)]
    [InlineData(6, null, 1, 20, false)]
    [InlineData(null, -1, 1, 20, false)]
    [InlineData(null, 0, 1, 20, false)]
    [InlineData(null, 6, 1, 20, false)]
    [InlineData(0, 6, 1, 20, false)]
    [InlineData(5, 4, 0, 20, false)]
    [InlineData(5, 4, 1, 101, false)]
    public void ServiceReviewListQueryValidator_ShouldEnforceRatingMinRatingAndPaginationBounds(
        int? rating,
        int? minRating,
        int pageNumber,
        int pageSize,
        bool expectedValid)
    {
        var validator = new ServiceReviewListQueryValidator();
        var result = validator.Validate(new ServiceReviewListQuery(
            Rating: rating,
            MinRating: minRating,
            PageNumber: pageNumber,
            PageSize: pageSize));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(null, null, 1, 20, true)]
    [InlineData("Submitted", "Electrical", 1, 20, true)]
    [InlineData("  approved ", " brakesandsuspension ", 1, 100, true)]
    [InlineData("InvalidStatus", "Electrical", 1, 20, false)]
    [InlineData("Submitted", "InvalidDefect", 1, 20, false)]
    [InlineData("Submitted", "Electrical", 0, 20, false)]
    [InlineData("Submitted", "Electrical", 1, 101, false)]
    public void WarrantyClaimAndTestDriveListQueryValidators_ShouldEnforceFilterAndPaginationBounds(
        string? status,
        string? defectCategory,
        int pageNumber,
        int pageSize,
        bool expectedWarrantyValid)
    {
        var warrantyValidator = new WarrantyClaimListQueryValidator();
        var warrantyResult = warrantyValidator.Validate(new WarrantyClaimListQuery(
            Status: status,
            DefectCategory: defectCategory,
            PageNumber: pageNumber,
            PageSize: pageSize));
        Assert.Equal(expectedWarrantyValid, warrantyResult.IsValid);

        var tdValidator = new TestDriveListQueryValidator();
        Assert.True(tdValidator.Validate(new TestDriveListQuery(Status: "Confirmed", PageNumber: 1, PageSize: 20)).IsValid);
        Assert.False(tdValidator.Validate(new TestDriveListQuery(Status: "UnknownStatus", PageNumber: 1, PageSize: 20)).IsValid);
        Assert.False(tdValidator.Validate(new TestDriveListQuery(Status: null, PageNumber: 1, PageSize: 101)).IsValid);
    }
}
