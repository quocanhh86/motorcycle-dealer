using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.ValueObjects;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class VinTests
{
    [Theory]
    [InlineData("RLHJF5109RZ104829", "RLHJF5109RZ104829")]
    [InlineData("RLCUG1010PY092341", "RLCUG1010PY092341")]
    [InlineData("  MLHNC5904P5200118  ", "MLHNC5904P5200118")]
    [InlineData("JH2SC5900GK200391", "JH2SC5900GK200391")]
    public void Create_WithValidIso3779Vin_ShouldReturnNormalizedImmutableValueObject(
        string rawInput,
        string expectedValue)
    {
        var vin = Vin.Create(rawInput);

        Assert.Equal(expectedValue, vin.Value);
        Assert.Equal(expectedValue, vin.ToString());
        Assert.Equal(expectedValue, (string)vin);
        Assert.True(Vin.TryCreate(rawInput, out var parsed));
        Assert.Equal(vin, parsed);
    }

    [Theory]
    [InlineData(null, "INVALID_VIN_LENGTH")]
    [InlineData("", "INVALID_VIN_LENGTH")]
    [InlineData("   ", "INVALID_VIN_LENGTH")]
    [InlineData("RLHJF5109RZ10482", "INVALID_VIN_LENGTH")]
    [InlineData("RLHJF5109RZ1048299", "INVALID_VIN_LENGTH")]
    [InlineData("RLHJF510IRZ104829", "INVALID_VIN_CHAR")]
    [InlineData("RLHJF510ORZ104829", "INVALID_VIN_CHAR")]
    [InlineData("RLHJF510QRZ104829", "INVALID_VIN_CHAR")]
    [InlineData("rlhjf5109rz104829", "INVALID_VIN_FORMAT")]
    [InlineData("RLHJF510-RZ104829", "INVALID_VIN_FORMAT")]
    [InlineData("RLHJF510_RZ104829", "INVALID_VIN_FORMAT")]
    public void Create_WithInvalidIso3779Vin_ShouldRejectWithCanonicalErrorCode(
        string? invalidInput,
        string expectedErrorCode)
    {
        var result = Vin.CreateResult(invalidInput);
        Assert.True(result.IsFailure);
        Assert.Equal(expectedErrorCode, result.Error.Code);

        Assert.False(Vin.TryCreate(invalidInput, out var parsed));
        Assert.Null(parsed);

        var exception = Assert.Throws<DomainException>(() => Vin.Create(invalidInput));
        Assert.Equal(expectedErrorCode, exception.ErrorCode);
    }
}
