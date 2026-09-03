using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Services.Showroom;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests.TestDrives;

public sealed class TestDriveAndSparePartBoundaryTests
{
    [Theory]
    [InlineData("08:00", "08:30", true)]   // exact 30m minimum at opening boundary
    [InlineData("09:00", "09:45", true)]   // canonical morning slot
    [InlineData("16:30", "18:00", true)]   // exact 90m maximum at closing boundary
    [InlineData("07:59", "08:45", false)]  // before 08:00 opening
    [InlineData("17:30", "18:01", false)]  // after 18:00 closing
    [InlineData("09:00", "09:29", false)]  // < 30 minutes
    [InlineData("09:00", "10:31", false)]  // > 90 minutes
    [InlineData("10:00", "10:00", false)]  // zero duration
    [InlineData("11:00", "10:15", false)]  // inverted window
    public void TestDriveValidationRules_IsValidTimeWindow_ShouldEnforceShowroomHoursAndDurationBounds(
        string startRaw,
        string endRaw,
        bool expectedValid)
    {
        var start = TimeOnly.ParseExact(startRaw, "HH:mm");
        var end = TimeOnly.ParseExact(endRaw, "HH:mm");

        Assert.Equal(expectedValid, TestDriveValidationRules.IsValidTimeWindow(start, end));
    }

    [Theory]
    [InlineData("09:00", "09:45", "09:00", "09:45", true)]   // identical window
    [InlineData("09:00", "09:45", "09:30", "10:15", true)]   // partial overlap
    [InlineData("09:00", "09:45", "09:45", "10:30", false)]  // back-to-back boundary does not overlap
    [InlineData("09:00", "09:45", "10:00", "10:45", false)]  // disjoint slots
    public void TestDriveValidationRules_TimesOverlap_ShouldDetectHalfOpenIntervalIntersections(
        string startARaw,
        string endARaw,
        string startBRaw,
        string endBRaw,
        bool expectedOverlap)
    {
        var startA = TimeOnly.ParseExact(startARaw, "HH:mm");
        var endA = TimeOnly.ParseExact(endARaw, "HH:mm");
        var startB = TimeOnly.ParseExact(startBRaw, "HH:mm");
        var endB = TimeOnly.ParseExact(endBRaw, "HH:mm");

        Assert.Equal(expectedOverlap, TestDriveValidationRules.TimesOverlap(startA, endA, startB, endB));
    }

    [Theory]
    [InlineData("2026-06-10T16:59:59Z", "2026-06-10")] // 23:59:59 ICT
    [InlineData("2026-06-10T17:00:00Z", "2026-06-11")] // 00:00:00 ICT next day
    public void GetVietnamToday_ShouldConvertUtcTimestampUsingIctPlusSevenOffset(
        string utcIso,
        string expectedDateIso)
    {
        var utc = DateTimeOffset.Parse(utcIso);
        var expectedDate = DateOnly.ParseExact(expectedDateIso, "yyyy-MM-dd");

        Assert.Equal(expectedDate, TestDriveValidationRules.GetVietnamToday(utc));
    }

    [Theory]
    [InlineData("A2-010554321", true)]
    [InlineData("a1-791234567", true)]            // normalized to uppercase in validator
    [InlineData("AB1234", true)]                  // exact 6-char minimum
    [InlineData("A2-12345678901234567", true)]    // exact 20-char maximum
    [InlineData("A1234", false)]                  // < 6 chars
    [InlineData("A2-123456789012345678", false)]  // 21 chars
    [InlineData("A2 010554321", false)]           // whitespace forbidden
    [InlineData("A2@010554", false)]              // special symbol forbidden
    [InlineData("", false)]
    public void CreateTestDriveRequestValidator_ShouldValidateDriverLicenseBoundaries(
        string driverLicense,
        bool expectedValid)
    {
        var validator = new CreateTestDriveRequestValidator();
        var result = validator.Validate(new CreateTestDriveRequest
        {
            DealerId = Guid.NewGuid(),
            MotorcycleModelId = Guid.NewGuid(),
            PreferredDate = new DateOnly(2026, 7, 15),
            StartTime = "09:00",
            EndTime = "09:45",
            DriverLicenseNumber = driverLicense,
            ContactPhone = "0913000201",
            CustomerNotes = "Testing ergonomics."
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(TestDriveStatus.Requested, TestDriveStatus.Confirmed, true)]
    [InlineData(TestDriveStatus.Requested, TestDriveStatus.Cancelled, true)]
    [InlineData(TestDriveStatus.Requested, TestDriveStatus.Completed, false)]
    [InlineData(TestDriveStatus.Requested, TestDriveStatus.NoShow, false)]
    [InlineData(TestDriveStatus.Confirmed, TestDriveStatus.Completed, true)]
    [InlineData(TestDriveStatus.Confirmed, TestDriveStatus.NoShow, true)]
    [InlineData(TestDriveStatus.Confirmed, TestDriveStatus.Cancelled, true)]
    [InlineData(TestDriveStatus.Confirmed, TestDriveStatus.Requested, false)]
    [InlineData(TestDriveStatus.Completed, TestDriveStatus.Cancelled, false)]
    [InlineData(TestDriveStatus.Cancelled, TestDriveStatus.Confirmed, false)]
    [InlineData(TestDriveStatus.NoShow, TestDriveStatus.Completed, false)]
    public void IsAllowedStaffTransition_ShouldEnforceStateMachineTransitions(
        TestDriveStatus current,
        TestDriveStatus target,
        bool expectedAllowed)
    {
        Assert.Equal(expectedAllowed, TestDriveValidationRules.IsAllowedStaffTransition(current, target));
    }

    [Theory]
    [InlineData("Rescheduling to next weekend", null, null, true)]
    [InlineData(null, "Rider unavailable due to rain", "AAAAAAAAAAA=", true)]
    [InlineData("abc", null, null, true)]     // exact 3-char minimum
    [InlineData("ab", null, null, false)]     // < 3 chars
    [InlineData("", null, null, false)]       // empty
    [InlineData("Valid reason", null, "invalid-rowversion", false)]
    public void CancelTestDriveRequestValidator_ShouldEnforceReasonLengthAndRowVersionFormat(
        string? reason,
        string? cancellationReason,
        string? rowVersion,
        bool expectedValid)
    {
        var validator = new CancelTestDriveRequestValidator();
        var result = validator.Validate(new CancelTestDriveRequest
        {
            Reason = reason,
            CancellationReason = cancellationReason,
            RowVersion = rowVersion
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(2, false, false)]   // 2-char Reason rejected
    [InlineData(3, false, true)]    // 3-char Reason accepted
    [InlineData(300, false, true)]  // 300-char Reason accepted
    [InlineData(301, false, false)] // 301-char Reason rejected
    [InlineData(2, true, false)]    // 2-char CancellationReason fallback rejected
    [InlineData(3, true, true)]     // 3-char CancellationReason fallback accepted
    [InlineData(300, true, true)]   // 300-char CancellationReason fallback accepted
    [InlineData(301, true, false)]  // 301-char CancellationReason fallback rejected
    public void CancelTestDriveRequestValidator_ShouldEnforceExact2To301CharBoundariesAcrossReasonAndCancellationReasonFallback(
        int length,
        bool useCancellationReasonAlias,
        bool expectedValid)
    {
        var validator = new CancelTestDriveRequestValidator();
        var text = new string('R', length);
        var request = useCancellationReasonAlias
            ? new CancelTestDriveRequest { Reason = "   ", CancellationReason = text }
            : new CancelTestDriveRequest { Reason = text };

        var result = validator.Validate(request);
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(true, false, true)]   // MotorcycleModelId primary
    [InlineData(false, true, true)]   // ModelId fallback alias
    [InlineData(true, true, true)]    // MotorcycleModelId wins when both populated
    [InlineData(false, false, false)] // Guid.Empty when both omitted
    public void CreateTestDriveRequest_ResolveEffectiveModelId_ShouldPrioritizeMotorcycleModelIdAndFallbackToModelId(
        bool hasPrimaryModelId,
        bool hasAliasModelId,
        bool expectedNonEmpty)
    {
        var primary = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var alias = Guid.Parse("11111111-1111-1111-1111-111111111102");

        var req = new CreateTestDriveRequest
        {
            MotorcycleModelId = hasPrimaryModelId ? primary : Guid.Empty,
            ModelId = hasAliasModelId ? alias : null
        };

        var resolved = req.ResolveEffectiveModelId();
        Assert.Equal(expectedNonEmpty, resolved != Guid.Empty);
        if (hasPrimaryModelId)
        {
            Assert.Equal(primary, resolved);
        }
        else if (hasAliasModelId)
        {
            Assert.Equal(alias, resolved);
        }
    }

    [Theory]
    [InlineData("Confirmed", "Completed", "Completed")]
    [InlineData("Confirmed", null, "Confirmed")]
    [InlineData("   ", "NoShow", "NoShow")]
    [InlineData(null, null, "")]
    public void UpdateTestDriveStatusRequest_ResolveEffectiveStatus_ShouldPreferNewStatusOverStatus(
        string? status,
        string? newStatus,
        string expectedStatus)
    {
        var req = new UpdateTestDriveStatusRequest
        {
            Status = status,
            NewStatus = newStatus
        };

        Assert.Equal(expectedStatus, req.ResolveEffectiveStatus());
    }

    [Theory]
    [InlineData("Primary Cancel", "Fallback Reason", "Staff Note", "Plain Note", "Primary Cancel")]
    [InlineData("   ", "Fallback Reason", "Staff Note", "Plain Note", "Fallback Reason")]
    [InlineData(null, null, "Staff Note", "Plain Note", "Staff Note")]
    [InlineData(null, null, "  ", "Plain Note", "Plain Note")]
    [InlineData(null, null, null, null, null)]
    public void UpdateTestDriveStatusRequest_AliasResolvers_ShouldCascadeCancellationReasonToReasonToStaffNotesToNotes(
        string? cancellationReason,
        string? reason,
        string? staffNotes,
        string? notes,
        string? expectedResolvedCancelReason)
    {
        var req = new UpdateTestDriveStatusRequest
        {
            CancellationReason = cancellationReason,
            Reason = reason,
            StaffNotes = staffNotes,
            Notes = notes
        };

        Assert.Equal(expectedResolvedCancelReason, req.ResolveEffectiveCancellationReason());
    }

    [Theory]
    [InlineData("engineoil", true, SparePartCategory.EngineOil)]
    [InlineData("FILTER", true, SparePartCategory.Filter)]
    [InlineData("Brakes", true, SparePartCategory.Brakes)]
    [InlineData("transmission", true, SparePartCategory.Transmission)]
    [InlineData("TIRES", true, SparePartCategory.Tires)]
    [InlineData("Electrical", true, SparePartCategory.Electrical)]
    [InlineData("accessory", true, SparePartCategory.Accessory)]
    [InlineData("Suspension", false, default(SparePartCategory))]
    [InlineData("", false, default(SparePartCategory))]
    public void SparePartValidationRules_TryNormalizeCategory_ShouldMatchCanonicalCategories(
        string rawCategory,
        bool expectedValid,
        SparePartCategory expectedCategory)
    {
        var ok = SparePartValidationRules.TryNormalizeCategory(rawCategory, out var parsed);
        Assert.Equal(expectedValid, ok);
        Assert.Equal(expectedCategory, parsed);
    }

    [Theory]
    [InlineData("PRT-OIL-10W30", "Honda Pro Synthetic 10W-30", "EngineOil", "Bottle", 250_000, true)]
    [InlineData("prt_flt_01", "Air Filter Element", "Filter", "pc", 180_000, true)]
    [InlineData("AB", "Too Short Code", "Filter", "pc", 100_000, false)]
    [InlineData("PRT-OIL-10W30", "Zero Price Invalid", "EngineOil", "Bottle", 0, false)]
    [InlineData("PRT-OIL-10W30", "Negative Price Invalid", "EngineOil", "Bottle", -50_000, false)]
    [InlineData("PRT-OIL-10W30", "Unknown Category", "Bodywork", "pc", 150_000, false)]
    [InlineData("PRT-OIL-10W30", "Missing Unit Label", "EngineOil", "   ", 150_000, false)]
    public void UpsertSparePartRequestValidator_ShouldEnforceCatalogConstraints(
        string partCode,
        string name,
        string category,
        string unitLabel,
        decimal unitPriceVnd,
        bool expectedValid)
    {
        var validator = new UpsertSparePartRequestValidator();
        var result = validator.Validate(new UpsertSparePartRequest
        {
            PartCode = partCode,
            Name = name,
            Category = category,
            UnitLabel = unitLabel,
            UnitPriceVnd = unitPriceVnd,
            IsActive = true
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(2, 10, 4, false)]   // PartCode 2 chars (< 3) rejected
    [InlineData(3, 10, 4, true)]    // PartCode 3 chars accepted
    [InlineData(32, 10, 4, true)]   // PartCode 32 chars accepted
    [InlineData(33, 10, 4, false)]  // PartCode 33 chars (> 32) rejected
    [InlineData(10, 1, 4, false)]   // Name 1 char (< 2) rejected
    [InlineData(10, 2, 4, true)]    // Name 2 chars accepted
    [InlineData(10, 150, 4, true)]  // Name 150 chars accepted
    [InlineData(10, 151, 4, false)] // Name 151 chars (> 150) rejected
    [InlineData(10, 10, 1, true)]   // UnitLabel 1 char accepted
    [InlineData(10, 10, 24, true)]  // UnitLabel 24 chars accepted
    [InlineData(10, 10, 25, false)] // UnitLabel 25 chars (> 24) rejected
    public void UpsertSparePartRequestValidator_ShouldEnforceExactLengthBoundariesOnPartCodeNameAndUnitLabel(
        int partCodeLength,
        int nameLength,
        int unitLabelLength,
        bool expectedValid)
    {
        var validator = new UpsertSparePartRequestValidator();
        var result = validator.Validate(new UpsertSparePartRequest
        {
            PartCode = new string('P', partCodeLength),
            Name = new string('N', nameLength),
            Category = "Filter",
            UnitLabel = new string('U', unitLabelLength),
            UnitPriceVnd = 180_000m,
            IsActive = true
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(1, 250_000.00, 250_000.00)]
    [InlineData(2, 450_000.00, 900_000.00)]
    [InlineData(3, 125_500.50, 376_501.50)]
    public void ComputeLineTotalVnd_ShouldCalculateExactDecimalQuantityTimesUnitPrice(
        int quantity,
        decimal unitPriceVnd,
        decimal expectedTotalVnd)
    {
        var actual = ServiceInvoiceValidationRules.ComputeLineTotalVnd(quantity, unitPriceVnd);
        Assert.Equal(expectedTotalVnd, actual);
    }

    [Theory]
    [InlineData(true, "Part", 10, 1, true)]      // SparePartId + Part valid
    [InlineData(true, "Labor", 10, 1, false)]    // SparePartId + Labor rejected
    [InlineData(false, "Labor", 200, 1, true)]   // 200-char description accepted
    [InlineData(false, "Labor", 201, 1, false)]  // 201-char description rejected
    [InlineData(false, "Part", 20, 0, false)]    // Quantity 0 rejected
    [InlineData(false, "Part", 20, 1, true)]     // Quantity 1 accepted
    [InlineData(false, "Part", 20, 1_000, true)] // Quantity 1000 accepted
    [InlineData(false, "Part", 20, 1_001, false)]// Quantity 1001 rejected
    public void CreateServiceInvoiceLineItemRequestValidator_ShouldEnforceTypeDescriptionAndQuantityBoundaries(
        bool hasSparePartId,
        string itemType,
        int descriptionLength,
        int quantity,
        bool expectedValid)
    {
        var validator = new CreateServiceInvoiceLineItemRequestValidator();
        var result = validator.Validate(new CreateServiceInvoiceLineItemRequest
        {
            SparePartId = hasSparePartId ? Guid.NewGuid() : null,
            ItemType = itemType,
            Description = descriptionLength > 0 ? new string('D', descriptionLength) : null,
            Quantity = quantity,
            UnitPriceVnd = 100_000m
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(50, true)]  // exact 50-line cap accepted
    [InlineData(51, false)] // 51 lines rejected
    public void CreateServiceInvoiceRequestValidator_ShouldEnforce50LineItemsCap(
        int lineCount,
        bool expectedValid)
    {
        var validator = new CreateServiceInvoiceRequestValidator();
        var lines = Enumerable.Range(1, lineCount)
            .Select(i => new CreateServiceInvoiceLineItemRequest
            {
                ItemType = "Part",
                Description = $"Line {i}",
                Quantity = 1,
                UnitPriceVnd = 50_000m
            })
            .ToList();

        var result = validator.Validate(new CreateServiceInvoiceRequest
        {
            ServiceBookingId = Guid.NewGuid(),
            OdometerAtCompletionKm = 4_000,
            LineItems = lines
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void CreateServiceInvoiceRequestValidator_ShouldSupportLegacyTotalsAndItemizedLines()
    {
        var validator = new CreateServiceInvoiceRequestValidator();
        var bookingId = Guid.NewGuid();

        // 1. Legacy non-itemized invoice works identically
        var legacyValid = validator.Validate(new CreateServiceInvoiceRequest
        {
            ServiceBookingId = bookingId,
            LaborAmount = 200_000m,
            PartsAmount = 350_000m,
            OdometerAtCompletionKm = 4_000
        });
        Assert.True(legacyValid.IsValid);

        // 2. Zero totals without LineItems is rejected
        var zeroInvalid = validator.Validate(new CreateServiceInvoiceRequest
        {
            ServiceBookingId = bookingId,
            LaborAmount = 0m,
            PartsAmount = 0m,
            OdometerAtCompletionKm = 4_000
        });
        Assert.False(zeroInvalid.IsValid);

        // 3. Itemized SparePart line with 0 top-level PartsAmount/LaborAmount is valid
        var itemizedValid = validator.Validate(new CreateServiceInvoiceRequest
        {
            ServiceBookingId = bookingId,
            LaborAmount = 0m,
            PartsAmount = 0m,
            OdometerAtCompletionKm = 4_000,
            LineItems =
            [
                new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = Guid.NewGuid(),
                    ItemType = "Part",
                    Quantity = 2
                },
                new CreateServiceInvoiceLineItemRequest
                {
                    ItemType = "Labor",
                    Description = "4,000 km Periodic Inspection Labor",
                    Quantity = 1,
                    UnitPriceVnd = 250_000m
                }
            ]
        });
        Assert.True(itemizedValid.IsValid);

        // 4. Custom line without SparePartId missing Description or UnitPriceVnd is rejected
        var missingPriceInvalid = validator.Validate(new CreateServiceInvoiceRequest
        {
            ServiceBookingId = bookingId,
            OdometerAtCompletionKm = 4_000,
            LineItems =
            [
                new CreateServiceInvoiceLineItemRequest
                {
                    SparePartId = null,
                    ItemType = "Labor",
                    Description = "Diagnostic Labor",
                    Quantity = 1,
                    UnitPriceVnd = null
                }
            ]
        });
        Assert.False(missingPriceInvalid.IsValid);
    }

    [Theory]
    [InlineData("FixedAmount", 100_000, null, 700_000, 100_000)]
    [InlineData("FixedAmount", 500_000, null, 300_000, 300_000)]    // clamped to eligible amount
    [InlineData("Percentage", 15, 200_000, 1_000_000, 150_000)]     // 15% under cap
    [InlineData("Percentage", 15, 200_000, 2_000_000, 200_000)]     // 15% capped at MaxDiscountAmount
    [InlineData("Percentage", 10, null, 455_555, 45_555.50)]        // AwayFromZero 2-decimal rounding
    public void VoucherAndDocumentNumberHelper_ShouldComputeExactDiscounts_AndFormatDocumentNumbers(
        string discountType,
        decimal discountValue,
        int? maxCapInt,
        decimal eligibleAmount,
        decimal expectedDiscount)
    {
        var catalog = new VoucherCatalog
        {
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxCapInt.HasValue ? (decimal)maxCapInt.Value : null
        };

        var discount = VoucherAndDocumentNumberHelper.ComputeVoucherDiscountAmount(eligibleAmount, catalog);
        Assert.Equal(expectedDiscount, discount);

        var entityId = Guid.Parse("01970000-0000-7000-8000-000000abcdef");
        var docNo = VoucherAndDocumentNumberHelper.FormatDocumentNumber("td", new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero), entityId);
        Assert.Equal("TD-20260815-ABCDEF", docNo);
    }

    [Fact]
    public void ReceiptVerificationHash_ShouldBeDeterministic_16CharUppercaseHex_AndDomainSeparated()
    {
        var id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01");

        var svcHash1 = ReceiptVerificationHash.ComputeForServiceInvoice(id, "INV-202608-0001", 11_760_000m);
        var svcHash2 = ReceiptVerificationHash.ComputeForServiceInvoice(id, "  inv-202608-0001 ", 11_760_000.00m);
        var svcHashDifferentAmount = ReceiptVerificationHash.ComputeForServiceInvoice(id, "INV-202608-0001", 11_760_001m);
        var slsHashSameInputs = ReceiptVerificationHash.ComputeForSalesOrder(id, "INV-202608-0001", 11_760_000m);

        Assert.Equal(16, svcHash1.Length);
        Assert.Matches("^[0-9A-F]{16}$", svcHash1);
        Assert.Equal(svcHash1, svcHash2);
        Assert.NotEqual(svcHash1, svcHashDifferentAmount);
        Assert.NotEqual(svcHash1, slsHashSameInputs);
    }
}
