using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Services.Showroom;

internal static class ShowroomRowVersionHelper
{
    public static string Encode(byte[]? rowVersion) =>
        RowVersionCodec.Encode(rowVersion);

    public static byte[] DecodeOrThrow(string? rawBase64) =>
        RowVersionCodec.DecodeRequiredOrThrow(rawBase64);

    public static bool TryGetUniqueViolationMessage(DbUpdateException exception, out string sqlMessage) =>
        SqlExceptionGuards.TryGetUniqueViolationMessage(exception, out sqlMessage);

    public static string EscapeLikePattern(string raw) =>
        raw.Replace("[", "[[]", StringComparison.Ordinal)
           .Replace("%", "[%]", StringComparison.Ordinal)
           .Replace("_", "[_]", StringComparison.Ordinal);
}
