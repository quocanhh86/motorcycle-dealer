using System.Text.RegularExpressions;
using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.ValueObjects;

/// <summary>
/// Immutable ISO 3779 17-character Vehicle Identification Number (VIN) value object.
/// Permits strictly uppercase ASCII alphanumeric characters A-Z and 0-9, excluding I, O, and Q.
/// </summary>
public sealed partial record Vin
{
    public const int CanonicalLength = 17;
    public const string Pattern = "^[A-HJ-NPR-Z0-9]{17}$";

    private Vin(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Vin Create(string? rawVin)
    {
        var result = CreateResult(rawVin);
        if (result.IsFailure)
        {
            throw new DomainException(result.Error.Code, result.Error.Description);
        }

        return result.Value;
    }

    public static Result<Vin> CreateResult(string? rawVin)
    {
        if (string.IsNullOrWhiteSpace(rawVin))
        {
            return Result.Failure<Vin>(Error.Validation(
                "INVALID_VIN_LENGTH",
                "VIN must be exactly 17 uppercase alphanumeric characters (ISO 3779)."));
        }

        var trimmed = rawVin.Trim();
        if (trimmed.Length != CanonicalLength)
        {
            return Result.Failure<Vin>(Error.Validation(
                "INVALID_VIN_LENGTH",
                $"VIN must be exactly {CanonicalLength} characters long, but received {trimmed.Length}."));
        }

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if (ch is 'I' or 'O' or 'Q')
            {
                return Result.Failure<Vin>(Error.Validation(
                    "INVALID_VIN_CHAR",
                    $"VIN contains ISO 3779 forbidden character '{ch}' at position {i + 1} (I, O, and Q are prohibited)."));
            }
        }

        if (!Iso3779Regex().IsMatch(trimmed))
        {
            return Result.Failure<Vin>(Error.Validation(
                "INVALID_VIN_FORMAT",
                "VIN must contain only uppercase ASCII letters A-Z (excluding I, O, Q) and digits 0-9."));
        }

        return Result.Success(new Vin(trimmed));
    }

    public static bool TryCreate(string? rawVin, out Vin? vin)
    {
        var result = CreateResult(rawVin);
        if (result.IsSuccess)
        {
            vin = result.Value;
            return true;
        }

        vin = null;
        return false;
    }

    public override string ToString() => Value;

    public static implicit operator string(Vin vin)
    {
        ArgumentNullException.ThrowIfNull(vin);
        return vin.Value;
    }

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant)]
    private static partial Regex Iso3779Regex();
}
