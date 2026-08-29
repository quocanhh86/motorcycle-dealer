using System.Globalization;
using System.Text;
using MotorcycleDealer.Application.Features.Analytics;

namespace MotorcycleDealer.Infrastructure.Services.Analytics;

public static class AnalyticsCsvExporter
{
    public const string CsvContentType = "text/csv; charset=utf-8";

    public const string HeaderRow =
        "DealerCode,DealerName,City,UnitsSold,ShowroomRevenueVnd,InvoicesIssued,ServiceRevenueVnd,TotalRevenueVnd,TotalBookings,CompletedBookings,CancelledOrNoShowBookings,CompletionRatePct,CancellationRatePct,AverageCsatRating,ServiceReviewsCount,OpenWarrantyClaims,ApprovedWarrantyCoveredVnd";

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private static readonly char[] FormulaTriggerChars = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] Rfc4180QuoteTriggerChars = [',', '"', '\r', '\n', '\''];

    public static AnalyticsCsvExportFile BuildDealerAnalyticsCsv(
        IReadOnlyList<DealerAnalyticsRowDto> rows,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder(capacity: Math.Max(256, (rows.Count + 2) * 160));
        sb.Append(HeaderRow).Append("\r\n");

        foreach (var row in rows)
        {
            sb.Append(FormatTextCell(row.DealerCode)).Append(',')
              .Append(FormatTextCell(row.DealerName)).Append(',')
              .Append(FormatTextCell(row.City)).Append(',')
              .Append(FormatIntegerCell(row.UnitsSoldCount)).Append(',')
              .Append(FormatDecimalCell(row.ShowroomRevenueVnd)).Append(',')
              .Append(FormatIntegerCell(row.InvoicesIssuedCount)).Append(',')
              .Append(FormatDecimalCell(row.ServiceRevenueVnd)).Append(',')
              .Append(FormatDecimalCell(row.TotalRevenueVnd)).Append(',')
              .Append(FormatIntegerCell(row.TotalBookings)).Append(',')
              .Append(FormatIntegerCell(row.CompletedBookings)).Append(',')
              .Append(FormatIntegerCell(row.CancelledOrNoShowBookings)).Append(',')
              .Append(FormatDecimalCell(row.CompletionRatePct)).Append(',')
              .Append(FormatDecimalCell(row.CancellationRatePct)).Append(',')
              .Append(FormatDecimalCell(row.AverageCsatRating)).Append(',')
              .Append(FormatIntegerCell(row.TotalServiceReviewsCount)).Append(',')
              .Append(FormatIntegerCell(row.OpenWarrantyClaimsCount)).Append(',')
              .Append(FormatDecimalCell(row.ApprovedWarrantyCoveredVnd)).Append("\r\n");
        }

        var bodyBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var payload = new byte[Utf8Bom.Length + bodyBytes.Length];
        Utf8Bom.CopyTo(payload);
        bodyBytes.CopyTo(payload.AsSpan(Utf8Bom.Length));

        var fileName = $"dealer-analytics-{generatedAtUtc.ToUniversalTime():yyyyMMdd}.csv";
        return new AnalyticsCsvExportFile(payload, CsvContentType, fileName);
    }

    public static string FormatTextCell(string? rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return string.Empty;
        }

        var sanitized = IsFormulaInjectionRisk(rawValue)
            ? $"'{rawValue}"
            : rawValue;

        var requiresQuoting = sanitized.IndexOfAny(Rfc4180QuoteTriggerChars) >= 0 ||
                              char.IsWhiteSpace(sanitized[0]) ||
                              char.IsWhiteSpace(sanitized[^1]);

        if (!requiresQuoting)
        {
            return sanitized;
        }

        var escaped = sanitized.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    public static bool IsFormulaInjectionRisk(string? rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return false;
        }

        if (FormulaTriggerChars.Contains(rawValue[0]))
        {
            return true;
        }

        var trimmedSpaces = rawValue.AsSpan().TrimStart(' ');
        return !trimmedSpaces.IsEmpty && FormulaTriggerChars.Contains(trimmedSpaces[0]);
    }

    private static string FormatIntegerCell(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDecimalCell(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
