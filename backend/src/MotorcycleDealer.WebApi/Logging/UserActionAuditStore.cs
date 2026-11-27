using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotorcycleDealer.WebApi.Logging;

public interface IUserActionAuditStore
{
    string CurrentLogFilePath { get; }
    void Record(UserActionLogEntry entry);
    UserActionLogQueryResultDto Query(
        string? source = null,
        string? clientPlatform = null,
        string? category = null,
        string? role = null,
        Guid? userId = null,
        Guid? dealerId = null,
        string? correlationId = null,
        string? search = null,
        int take = 100);
}

/// <summary>
/// Thread-safe, durable user action audit store backing Server, Web SPA, and Mobile (Android/iOS) telemetry.
/// Emits structured ILogger lines (category MotorcycleDealer.UserActionAudit), persists daily rolling
/// JSON Lines (NDJSON) under logs/user-actions-yyyyMMdd.ndjson, and serves live correlated queries.
/// </summary>
public sealed class UserActionAuditStore : IUserActionAuditStore
{
    public const int MaxBufferedEntries = 2000;

    private static readonly JsonSerializerOptions NdjsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _logsDirectory;
    private readonly ILogger _auditLogger;
    private readonly object _syncLock = new();
    private readonly LinkedList<UserActionLogEntry> _buffer = new();

    public UserActionAuditStore(IHostEnvironment environment, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logsDirectory = Path.Combine(environment.ContentRootPath, "logs");
        _auditLogger = loggerFactory.CreateLogger("MotorcycleDealer.UserActionAudit");

        TryEnsureDirectory();
        LoadTodayTailIntoBuffer();
    }

    public string CurrentLogFilePath => BuildDailyLogPath(DateTimeOffset.UtcNow);

    public void Record(UserActionLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var serializedLine = JsonSerializer.Serialize(entry, NdjsonOptions);

        lock (_syncLock)
        {
            _buffer.AddFirst(entry);
            while (_buffer.Count > MaxBufferedEntries)
            {
                _buffer.RemoveLast();
            }

            AppendLineToDailyFile(entry.TimestampUtc, serializedLine);
        }

        EmitStructuredLog(entry);
    }

    public UserActionLogQueryResultDto Query(
        string? source = null,
        string? clientPlatform = null,
        string? category = null,
        string? role = null,
        Guid? userId = null,
        Guid? dealerId = null,
        string? correlationId = null,
        string? search = null,
        int take = 100)
    {
        var boundedTake = Math.Clamp(take, 1, 500);
        List<UserActionLogEntry> snapshot;
        lock (_syncLock)
        {
            snapshot = [.. _buffer];
        }

        var filtered = snapshot.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(source))
        {
            var src = source.Trim();
            filtered = filtered.Where(e => string.Equals(e.Source, src, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(clientPlatform))
        {
            var plat = clientPlatform.Trim();
            filtered = filtered.Where(e => string.Equals(e.ClientPlatform, plat, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            filtered = filtered.Where(e => string.Equals(e.Category, cat, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var rl = role.Trim();
            filtered = filtered.Where(e => string.Equals(e.Role, rl, StringComparison.OrdinalIgnoreCase));
        }

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            filtered = filtered.Where(e => e.UserId == userId.Value);
        }

        if (dealerId.HasValue && dealerId.Value != Guid.Empty)
        {
            filtered = filtered.Where(e => e.DealerId == dealerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var cid = correlationId.Trim();
            filtered = filtered.Where(e => string.Equals(e.CorrelationId, cid, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(e =>
                e.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Screen?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Target?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Path?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                e.CorrelationId.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var matchedList = filtered.ToList();
        var countsBySource = matchedList
            .GroupBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var countsByPlatform = matchedList
            .GroupBy(e => e.ClientPlatform, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return new UserActionLogQueryResultDto(
            Items: matchedList.Take(boundedTake).ToArray(),
            TotalBufferedCount: matchedList.Count,
            CountsBySource: countsBySource,
            CountsByPlatform: countsByPlatform,
            LogFilePath: CurrentLogFilePath);
    }

    private void EmitStructuredLog(UserActionLogEntry entry)
    {
        if (string.Equals(entry.Outcome, "ServerError", StringComparison.OrdinalIgnoreCase))
        {
            _auditLogger.LogError(
                "[USER-ACTION] Source={Source} Platform={Platform} User={Email}({Role}) Action={Action} Category={Category} Screen={Screen} Target={Target} Http={Method} {Path} Status={StatusCode} Outcome={Outcome} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                entry.Source,
                entry.ClientPlatform,
                entry.Email ?? "anonymous",
                entry.Role ?? "Anonymous",
                entry.Action,
                entry.Category,
                entry.Screen ?? "-",
                entry.Target ?? "-",
                entry.HttpMethod ?? "-",
                entry.Path ?? "-",
                entry.StatusCode ?? 0,
                entry.Outcome,
                entry.DurationMs ?? 0,
                entry.CorrelationId);
            return;
        }

        if (string.Equals(entry.Outcome, "Denied", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entry.Outcome, "ClientError", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entry.Outcome, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            _auditLogger.LogWarning(
                "[USER-ACTION] Source={Source} Platform={Platform} User={Email}({Role}) Action={Action} Category={Category} Screen={Screen} Target={Target} Http={Method} {Path} Status={StatusCode} Outcome={Outcome} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                entry.Source,
                entry.ClientPlatform,
                entry.Email ?? "anonymous",
                entry.Role ?? "Anonymous",
                entry.Action,
                entry.Category,
                entry.Screen ?? "-",
                entry.Target ?? "-",
                entry.HttpMethod ?? "-",
                entry.Path ?? "-",
                entry.StatusCode ?? 0,
                entry.Outcome,
                entry.DurationMs ?? 0,
                entry.CorrelationId);
            return;
        }

        _auditLogger.LogInformation(
            "[USER-ACTION] Source={Source} Platform={Platform} User={Email}({Role}) Action={Action} Category={Category} Screen={Screen} Target={Target} Http={Method} {Path} Status={StatusCode} Outcome={Outcome} DurationMs={DurationMs} CorrelationId={CorrelationId}",
            entry.Source,
            entry.ClientPlatform,
            entry.Email ?? "anonymous",
            entry.Role ?? "Anonymous",
            entry.Action,
            entry.Category,
            entry.Screen ?? "-",
            entry.Target ?? "-",
            entry.HttpMethod ?? "-",
            entry.Path ?? "-",
            entry.StatusCode ?? 0,
            entry.Outcome,
            entry.DurationMs ?? 0,
            entry.CorrelationId);
    }

    private string BuildDailyLogPath(DateTimeOffset timestampUtc)
    {
        var dateStamp = timestampUtc.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return Path.Combine(_logsDirectory, $"user-actions-{dateStamp}.ndjson");
    }

    private void TryEnsureDirectory()
    {
        try
        {
            Directory.CreateDirectory(_logsDirectory);
        }
        catch
        {
            // Keep server resilient even on read-only file systems.
        }
    }

    private void AppendLineToDailyFile(DateTimeOffset timestampUtc, string serializedLine)
    {
        try
        {
            TryEnsureDirectory();
            var path = BuildDailyLogPath(timestampUtc);
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine(serializedLine);
        }
        catch
        {
            // Never disrupt business requests if disk logging is temporarily unavailable.
        }
    }

    private void LoadTodayTailIntoBuffer()
    {
        try
        {
            var path = CurrentLogFilePath;
            if (!File.Exists(path))
            {
                return;
            }

            var lines = File.ReadLines(path).TakeLast(MaxBufferedEntries).ToArray();
            lock (_syncLock)
            {
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        var parsed = JsonSerializer.Deserialize<UserActionLogEntry>(line, NdjsonOptions);
                        if (parsed is not null)
                        {
                            _buffer.AddFirst(parsed);
                            while (_buffer.Count > MaxBufferedEntries)
                            {
                                _buffer.RemoveLast();
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed partial lines safely.
                    }
                }
            }
        }
        catch
        {
            // Ignore startup file read errors.
        }
    }
}
