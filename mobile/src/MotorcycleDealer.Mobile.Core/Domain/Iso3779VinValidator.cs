using System.Text;
using System.Text.RegularExpressions;
using MotorcycleDealer.Mobile.Core.Localization;

namespace MotorcycleDealer.Mobile.Core.Domain;

public sealed record VinValidationResult(
    bool IsValid,
    string NormalizedVin,
    string? ErrorCode,
    string? ErrorMessage,
    int CharacterCount,
    int RemainingCharacters);

/// <summary>
/// ISO 3779 17-character Vehicle Identification Number (VIN) validator and live input normalizer.
/// Permits strictly uppercase ASCII alphanumeric A-Z (excluding I, O, Q) and digits 0-9.
/// </summary>
public static partial class Iso3779VinValidator
{
    public const int CanonicalLength = 17;
    public const string Pattern = "^[A-HJ-NPR-Z0-9]{17}$";

    public static string SanitizeLiveInput(string? rawInput)
    {
        if (string.IsNullOrEmpty(rawInput))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(Math.Min(rawInput.Length, CanonicalLength));
        foreach (var rawChar in rawInput)
        {
            if (sb.Length >= CanonicalLength)
            {
                break;
            }

            var upper = char.ToUpperInvariant(rawChar);
            if (upper is >= '0' and <= '9')
            {
                sb.Append(upper);
            }
            else if (upper is >= 'A' and <= 'Z' && upper is not ('I' or 'O' or 'Q'))
            {
                sb.Append(upper);
            }
        }

        return sb.ToString();
    }

    public static VinValidationResult Validate(string? rawVin)
    {
        if (string.IsNullOrWhiteSpace(rawVin))
        {
            return new VinValidationResult(
                IsValid: false,
                NormalizedVin: string.Empty,
                ErrorCode: "INVALID_VIN_LENGTH",
                ErrorMessage: AppResources.Val_VinRequired,
                CharacterCount: 0,
                RemainingCharacters: CanonicalLength);
        }

        var trimmed = rawVin.Trim().ToUpperInvariant();
        if (trimmed.Length != CanonicalLength)
        {
            return new VinValidationResult(
                IsValid: false,
                NormalizedVin: trimmed,
                ErrorCode: "INVALID_VIN_LENGTH",
                ErrorMessage: AppResources.Format("Val_VinLength", CanonicalLength, trimmed.Length),
                CharacterCount: trimmed.Length,
                RemainingCharacters: Math.Max(0, CanonicalLength - trimmed.Length));
        }

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if (ch is 'I' or 'O' or 'Q')
            {
                return new VinValidationResult(
                    IsValid: false,
                    NormalizedVin: trimmed,
                    ErrorCode: "INVALID_VIN_CHAR",
                    ErrorMessage: AppResources.Format("Val_VinForbiddenChar", ch, i + 1),
                    CharacterCount: trimmed.Length,
                    RemainingCharacters: 0);
            }
        }

        if (!Iso3779Regex().IsMatch(trimmed))
        {
            return new VinValidationResult(
                IsValid: false,
                NormalizedVin: trimmed,
                ErrorCode: "INVALID_VIN_FORMAT",
                ErrorMessage: AppResources.Val_VinInvalidFormat,
                CharacterCount: trimmed.Length,
                RemainingCharacters: 0);
        }

        return new VinValidationResult(
            IsValid: true,
            NormalizedVin: trimmed,
            ErrorCode: null,
            ErrorMessage: null,
            CharacterCount: CanonicalLength,
            RemainingCharacters: 0);
    }

    public static bool IsValid(string? rawVin) => Validate(rawVin).IsValid;

    public static string FormatLiveCounter(string? rawVin)
    {
        var count = string.IsNullOrWhiteSpace(rawVin) ? 0 : rawVin.Trim().Length;
        return $"{count}/{CanonicalLength}";
    }

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant)]
    private static partial Regex Iso3779Regex();
}
