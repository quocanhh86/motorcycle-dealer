using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.TimeSlots;
using MotorcycleDealer.Domain.Common;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class RowVersionCodecTests
{
    [Fact]
    public void Encode_WhenNullOrEmpty_ShouldReturnEmptyString_AndRoundTripExactEightBytes()
    {
        Assert.Equal(string.Empty, RowVersionCodec.Encode(null));
        Assert.Equal(string.Empty, RowVersionCodec.Encode([]));

        byte[] sampleBytes = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xD1];
        var encoded = RowVersionCodec.Encode(sampleBytes);

        Assert.Equal("AAAAAAAAB9E=", encoded);
        Assert.Equal(sampleBytes, RowVersionCodec.DecodeRequiredOrThrow(encoded));
        Assert.Equal(sampleBytes, RowVersionCodec.DecodeOptionalOrThrow($"  {encoded}\t "));
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData("", false, true)]
    [InlineData("   ", false, true)]
    [InlineData("AAAAAAAAB9E=", true, true)]
    [InlineData("  AAAAAAAAB9E=  ", true, true)]
    [InlineData("AAAAAA==", false, false)]          // 4 bytes
    [InlineData("AAAAAAAAEA==", false, false)]      // 7 bytes
    [InlineData("AAAAAAAAAAAA", false, false)]      // 9 bytes
    [InlineData("AAAA", false, false)]              // 3 bytes
    [InlineData("***not-base64***", false, false)]  // invalid Base64
    public void IsValidBase64_ShouldValidateRequiredAndOptionalBoundariesConsistentlyAcrossSharedRules(
        string? input,
        bool expectedRequiredValid,
        bool expectedOptionalValid)
    {
        Assert.Equal(expectedRequiredValid, RowVersionCodec.IsValidRequiredBase64(input));
        Assert.Equal(expectedRequiredValid, ShowroomValidationRules.IsValidRowVersionBase64(input));

        Assert.Equal(expectedOptionalValid, RowVersionCodec.IsValidOptionalBase64(input));
        Assert.Equal(expectedOptionalValid, TimeSlotValidationRules.IsValidOptionalBase64RowVersion(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DecodeOptionalOrThrow_WhenNullOrWhitespace_ShouldReturnNull_WhileDecodeRequiredOrThrowThrowsInvalidRowVersion(
        string? emptyInput)
    {
        Assert.Null(RowVersionCodec.DecodeOptionalOrThrow(emptyInput));
        Assert.Null(TimeSlotValidationRules.DecodeOptionalRowVersion(emptyInput));

        var ex = Assert.Throws<DomainException>(() => RowVersionCodec.DecodeRequiredOrThrow(emptyInput));
        Assert.Equal("INVALID_ROW_VERSION", ex.ErrorCode);
    }

    [Theory]
    [InlineData("AAAAAA==")]         // 4 bytes
    [InlineData("AAAAAAAAEA==")]     // 7 bytes
    [InlineData("AAAAAAAAAAAA")]     // 9 bytes
    [InlineData("AAAA")]             // 3 bytes
    [InlineData("@@@invalid@@@")]    // malformed Base64
    public void DecodeRequiredAndOptional_WhenWrongLengthOrMalformedBase64_ShouldThrowDomainExceptionInvalidRowVersion(
        string malformedToken)
    {
        var reqEx = Assert.Throws<DomainException>(() => RowVersionCodec.DecodeRequiredOrThrow(malformedToken));
        Assert.Equal("INVALID_ROW_VERSION", reqEx.ErrorCode);

        var optEx = Assert.Throws<DomainException>(() => RowVersionCodec.DecodeOptionalOrThrow(malformedToken));
        Assert.Equal("INVALID_ROW_VERSION", optEx.ErrorCode);
    }
}
