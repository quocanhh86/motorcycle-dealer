using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Common;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests.Booking;

public sealed class Phase5BookingBoundaryTests
{
    private static readonly string Valid8ByteRowVersionBase64 =
        Convert.ToBase64String([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xD1]);

    private static readonly string Invalid7ByteRowVersionBase64 =
        Convert.ToBase64String([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);

    private static readonly string Invalid9ByteRowVersionBase64 =
        Convert.ToBase64String([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09]);

    public static TheoryData<string?, bool> RowVersionBoundaryCases => new()
    {
        { null, true },
        { string.Empty, true },
        { "   ", true },
        { Valid8ByteRowVersionBase64, true },
        { $"  {Valid8ByteRowVersionBase64}  ", true },
        { "AAAA", false },                     // 3 bytes
        { Invalid7ByteRowVersionBase64, false }, // 7 bytes
        { Invalid9ByteRowVersionBase64, false }, // 9 bytes
        { "not-valid-base64!!!", false }
    };

    [Theory]
    [MemberData(nameof(RowVersionBoundaryCases))]
    public void IsValidOptionalBase64RowVersion_ShouldEnforceExact8ByteSqlRowVersionContract(
        string? candidate,
        bool expectedIsValid)
    {
        var actual = TimeSlotValidationRules.IsValidOptionalBase64RowVersion(candidate);
        Assert.Equal(expectedIsValid, actual);

        if (!expectedIsValid)
        {
            var ex = Assert.Throws<DomainException>(() => TimeSlotValidationRules.DecodeOptionalRowVersion(candidate));
            Assert.Equal("INVALID_ROW_VERSION", ex.ErrorCode);
        }
        else
        {
            var decoded = TimeSlotValidationRules.DecodeOptionalRowVersion(candidate);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                Assert.Null(decoded);
            }
            else
            {
                Assert.NotNull(decoded);
                Assert.Equal(8, decoded.Length);
            }
        }
    }

    public static TheoryData<string?, string?, string?, bool> CancelBookingValidationCases => new()
    {
        { null, null, null, true },
        { string.Empty, null, null, true },
        { "   ", null, null, true },
        { "abc", null, null, true },
        { "  abc  ", null, Valid8ByteRowVersionBase64, true },
        { new string('x', 300), null, null, true },
        { "ab", null, null, false },
        { "  ab  ", null, null, false },
        { null, "xy", null, false },
        { new string('x', 301), null, null, false },
        { null, new string('y', 301), null, false },
        { "Valid reason", null, "AAAA", false }
    };

    [Theory]
    [MemberData(nameof(CancelBookingValidationCases))]
    public void CancelBookingRequestValidator_ShouldEnforceReasonLengthAndRowVersionBoundaries(
        string? cancellationReason,
        string? reason,
        string? rowVersion,
        bool expectedValid)
    {
        var validator = new CancelBookingRequestValidator();
        var result = validator.Validate(new CancelBookingRequest
        {
            CancellationReason = cancellationReason,
            Reason = reason,
            RowVersion = rowVersion
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("PeriodicMaintenance", true, "PeriodicMaintenance")]
    [InlineData("periodicmaintenance", true, "PeriodicMaintenance")]
    [InlineData("  WarrantyRepair  ", true, "WarrantyRepair")]
    [InlineData("GeneralRepair", true, "GeneralRepair")]
    [InlineData("Inspection", true, "Inspection")]
    [InlineData("OilAndTire", true, "OilAndTire")]
    [InlineData("CustomEngineSwap", false, "")]
    [InlineData("", false, "")]
    [InlineData(null, false, "")]
    public void TryNormalizeServiceType_ShouldAcceptOnlyCanonicalFiveServiceTypes(
        string? rawServiceType,
        bool expectedSuccess,
        string expectedNormalized)
    {
        var ok = BookingValidationRules.TryNormalizeServiceType(rawServiceType, out var normalized);
        Assert.Equal(expectedSuccess, ok);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData("Pending", true, "Pending")]
    [InlineData("confirmed", true, "Confirmed")]
    [InlineData("CheckedIn", true, "CheckedIn")]
    [InlineData("InProgress", true, "InProgress")]
    [InlineData("Completed", true, "Completed")]
    [InlineData("Cancelled", true, "Cancelled")]
    [InlineData("NoShow", true, "NoShow")]
    [InlineData("Archived", false, "")]
    [InlineData(" ", false, "")]
    [InlineData(null, false, "")]
    public void TryNormalizeBookingStatus_ShouldAcceptOnlyCanonicalSevenStatuses(
        string? rawStatus,
        bool expectedSuccess,
        string expectedNormalized)
    {
        var ok = BookingValidationRules.TryNormalizeBookingStatus(rawStatus, out var normalized);
        Assert.Equal(expectedSuccess, ok);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData("PeriodicMaintenance", 0, null, true)]
    [InlineData("OilAndTire", 1_000_000, null, true)]
    [InlineData("PeriodicMaintenance", -1, null, false)]
    [InlineData("PeriodicMaintenance", 1_000_001, null, false)]
    [InlineData("InvalidType", 1_000, null, false)]
    [InlineData("PeriodicMaintenance", 1_000, "AAAA", false)]
    public void CreateServiceBookingRequestValidator_ShouldEnforceOdometerServiceTypeAndRowVersionBoundaries(
        string serviceType,
        int odometerKm,
        string? slotRowVersion,
        bool expectedValid)
    {
        var validator = new CreateServiceBookingRequestValidator();
        var result = validator.Validate(new CreateServiceBookingRequest
        {
            DealerId = Guid.CreateVersion7(),
            ServiceTimeSlotId = Guid.CreateVersion7(),
            CustomerVehicleId = Guid.CreateVersion7(),
            ServiceType = serviceType,
            OdometerAtBookingKm = odometerKm,
            SlotRowVersion = slotRowVersion
        });

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("Confirmed", null, null, true)]
    [InlineData(null, "CheckedIn", null, true)]
    [InlineData("", "", null, false)]
    [InlineData("BogusStatus", null, null, false)]
    [InlineData("Confirmed", null, "AAAA", false)]
    public void UpdateBookingStatusRequestValidator_ShouldValidateStatusAliasAndRowVersion(
        string? newStatus,
        string? statusAlias,
        string? rowVersion,
        bool expectedValid)
    {
        var validator = new UpdateBookingStatusRequestValidator();
        var result = validator.Validate(new UpdateBookingStatusRequest
        {
            NewStatus = newStatus,
            Status = statusAlias,
            RowVersion = rowVersion
        });

        Assert.Equal(expectedValid, result.IsValid);
    }
}
