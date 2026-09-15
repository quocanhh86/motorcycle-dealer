using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MotorcycleDealer.Mobile.Core.Domain;

public enum QrPayloadKind
{
    Unknown = 0,
    Vin = 1,
    Voucher = 2,
    Booking = 3,
    Receipt = 4
}

public sealed record ParsedQrPayload(
    QrPayloadKind Kind,
    string RawValue,
    string? Vin = null,
    Guid? EntityId = null,
    string? VoucherCode = null,
    string? DocumentNumber = null,
    string? RowVersion = null,
    string? VerificationHash = null,
    string? ReceiptType = null)
{
    public bool IsRecognized => Kind != QrPayloadKind.Unknown;
}

/// <summary>
/// Parses and builds canonical QR/barcode payloads for VIN stickers, loyalty vouchers,
/// workshop bookings, and SHA-256 tamper-evident receipts.
/// </summary>
public static partial class DealerQrPayloadParser
{
    public const string Scheme = "dealer";

    public static ParsedQrPayload Parse(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new ParsedQrPayload(QrPayloadKind.Unknown, string.Empty);
        }

        var trimmed = rawInput.Trim();

        // 1. Canonical dealer:// deep-link URI payloads
        if (trimmed.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();
            var query = ParseQueryString(uri.Query);

            if (host == "voucher")
            {
                query.TryGetValue("code", out var code);
                query.TryGetValue("id", out var rawId);
                query.TryGetValue("rv", out var rv);
                Guid? entityId = Guid.TryParse(rawId, out var parsedId) && parsedId != Guid.Empty ? parsedId : null;

                if (!string.IsNullOrWhiteSpace(code) || entityId.HasValue)
                {
                    return new ParsedQrPayload(
                        Kind: QrPayloadKind.Voucher,
                        RawValue: trimmed,
                        EntityId: entityId,
                        VoucherCode: code?.Trim().ToUpperInvariant(),
                        RowVersion: string.IsNullOrWhiteSpace(rv) ? null : rv.Trim());
                }
            }
            else if (host == "booking")
            {
                query.TryGetValue("id", out var rawId);
                query.TryGetValue("num", out var num);
                query.TryGetValue("rv", out var rv);
                Guid? entityId = Guid.TryParse(rawId, out var parsedId) && parsedId != Guid.Empty ? parsedId : null;

                if (entityId.HasValue || !string.IsNullOrWhiteSpace(num))
                {
                    return new ParsedQrPayload(
                        Kind: QrPayloadKind.Booking,
                        RawValue: trimmed,
                        EntityId: entityId,
                        DocumentNumber: num?.Trim().ToUpperInvariant(),
                        RowVersion: string.IsNullOrWhiteSpace(rv) ? null : rv.Trim());
                }
            }
            else if (host == "receipt")
            {
                query.TryGetValue("num", out var num);
                query.TryGetValue("hash", out var hash);
                query.TryGetValue("type", out var receiptType);
                var cleanHash = hash?.Trim().ToUpperInvariant();

                if (!string.IsNullOrWhiteSpace(num) || (!string.IsNullOrWhiteSpace(cleanHash) && Hex16Regex().IsMatch(cleanHash)))
                {
                    return new ParsedQrPayload(
                        Kind: QrPayloadKind.Receipt,
                        RawValue: trimmed,
                        DocumentNumber: num?.Trim().ToUpperInvariant(),
                        VerificationHash: cleanHash,
                        ReceiptType: string.IsNullOrWhiteSpace(receiptType) ? null : receiptType.Trim());
                }
            }

            return new ParsedQrPayload(QrPayloadKind.Unknown, trimmed);
        }

        // 2. Pipe-delimited or prefixed Receipt verification payload: RCP-SVC-2026-0001|0123456789ABCDEF or RCP-...
        if (trimmed.StartsWith("RCP-", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var docNum = parts[0].ToUpperInvariant();
            string? hash = null;
            if (parts.Length >= 2 && Hex16Regex().IsMatch(parts[1].ToUpperInvariant()))
            {
                hash = parts[1].ToUpperInvariant();
            }

            return new ParsedQrPayload(
                Kind: QrPayloadKind.Receipt,
                RawValue: trimmed,
                DocumentNumber: docNum,
                VerificationHash: hash);
        }

        // 3. Standalone 16-char uppercase hex SHA-256 receipt verification code
        var upper = trimmed.ToUpperInvariant();
        if (Hex16Regex().IsMatch(upper))
        {
            return new ParsedQrPayload(
                Kind: QrPayloadKind.Receipt,
                RawValue: trimmed,
                VerificationHash: upper);
        }

        // 4. Raw 17-character ISO 3779 VIN barcode
        var vinResult = Iso3779VinValidator.Validate(trimmed);
        if (vinResult.IsValid)
        {
            return new ParsedQrPayload(
                Kind: QrPayloadKind.Vin,
                RawValue: trimmed,
                Vin: vinResult.NormalizedVin);
        }

        return new ParsedQrPayload(QrPayloadKind.Unknown, trimmed);
    }

    public static string BuildVoucherQrPayload(string voucherCode, Guid customerVoucherId, string? rowVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voucherCode);

        var sb = new StringBuilder();
        sb.Append(Scheme)
            .Append("://voucher/v1?code=")
            .Append(Uri.EscapeDataString(voucherCode.Trim().ToUpperInvariant()))
            .Append("&id=")
            .Append(customerVoucherId.ToString("D"));

        if (!string.IsNullOrWhiteSpace(rowVersion))
        {
            sb.Append("&rv=").Append(Uri.EscapeDataString(rowVersion.Trim()));
        }

        return sb.ToString();
    }

    public static string BuildBookingQrPayload(Guid bookingId, string bookingNumber, string? rowVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingNumber);

        var sb = new StringBuilder();
        sb.Append(Scheme)
            .Append("://booking/v1?id=")
            .Append(bookingId.ToString("D"))
            .Append("&num=")
            .Append(Uri.EscapeDataString(bookingNumber.Trim().ToUpperInvariant()));

        if (!string.IsNullOrWhiteSpace(rowVersion))
        {
            sb.Append("&rv=").Append(Uri.EscapeDataString(rowVersion.Trim()));
        }

        return sb.ToString();
    }

    public static string BuildReceiptQrPayload(string receiptNumber, string verificationCode, string receiptType = "Service")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationCode);

        return $"{Scheme}://receipt/v1?type={Uri.EscapeDataString(receiptType)}&num={Uri.EscapeDataString(receiptNumber.Trim().ToUpperInvariant())}&hash={Uri.EscapeDataString(verificationCode.Trim().ToUpperInvariant())}";
    }

    public static string ComputeServiceReceiptVerificationHash(Guid invoiceId, string invoiceNumber, decimal netPaidAmount)
    {
        var canonical = string.Create(
            CultureInfo.InvariantCulture,
            $"SVC|{invoiceId:D}|{invoiceNumber.Trim().ToUpperInvariant()}|{netPaidAmount:0.00}");
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(digest)[..16];
    }

    public static string ComputeSalesReceiptVerificationHash(Guid salesOrderId, string orderNumber, decimal finalAmount)
    {
        var canonical = string.Create(
            CultureInfo.InvariantCulture,
            $"SLS|{salesOrderId:D}|{orderNumber.Trim().ToUpperInvariant()}|{finalAmount:0.00}");
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(digest)[..16];
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..idx]);
            var value = Uri.UnescapeDataString(pair[(idx + 1)..]);
            result[key] = value;
        }

        return result;
    }

    [GeneratedRegex("^[0-9A-F]{16}$", RegexOptions.CultureInvariant)]
    private static partial Regex Hex16Regex();
}
