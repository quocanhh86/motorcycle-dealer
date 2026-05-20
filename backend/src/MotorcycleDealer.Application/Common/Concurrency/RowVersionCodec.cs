using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Application.Common.Concurrency;

/// <summary>
/// Canonical zero-allocation encoder, validator, and decoder for 8-byte SQL Server ROWVERSION Base64 tokens.
/// </summary>
public static class RowVersionCodec
{
    public const int ByteLength = 8;

    public static string Encode(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 }
            ? Convert.ToBase64String(rowVersion)
            : string.Empty;

    public static bool IsValidRequiredBase64(string? rawBase64)
    {
        if (string.IsNullOrWhiteSpace(rawBase64))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[16];
        return Convert.TryFromBase64Chars(rawBase64.AsSpan().Trim(), buffer, out var bytesWritten) &&
               bytesWritten == ByteLength;
    }

    public static bool IsValidOptionalBase64(string? rawBase64) =>
        string.IsNullOrWhiteSpace(rawBase64) || IsValidRequiredBase64(rawBase64);

    public static byte[] DecodeRequiredOrThrow(string? rawBase64)
    {
        if (string.IsNullOrWhiteSpace(rawBase64))
        {
            throw new DomainException("INVALID_ROW_VERSION", "RowVersion concurrency token is required.");
        }

        Span<byte> buffer = stackalloc byte[16];
        if (!Convert.TryFromBase64Chars(rawBase64.AsSpan().Trim(), buffer, out var bytesWritten) ||
            bytesWritten != ByteLength)
        {
            throw new DomainException(
                "INVALID_ROW_VERSION",
                "RowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");
        }

        return buffer[..ByteLength].ToArray();
    }

    public static byte[]? DecodeOptionalOrThrow(string? rawBase64) =>
        string.IsNullOrWhiteSpace(rawBase64) ? null : DecodeRequiredOrThrow(rawBase64);
}
