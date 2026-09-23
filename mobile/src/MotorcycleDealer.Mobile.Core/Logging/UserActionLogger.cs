using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Persistence;
using MotorcycleDealer.Mobile.Core.Realtime;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.Logging;

public static class MobileUserActionHeaders
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string ClientPlatform = "X-Client-Platform";
    public const string ClientSessionId = "X-Client-Session-Id";
    public const string ClientScreen = "X-Client-Screen";
    public const string ClientAction = "X-Client-Action";
}

public interface IAppPlatformInfo
{
    string ClientPlatform { get; }
}

public sealed class DefaultAppPlatformInfo(string clientPlatform = "MobileCore") : IAppPlatformInfo
{
    public string ClientPlatform { get; } = string.IsNullOrWhiteSpace(clientPlatform) ? "MobileCore" : clientPlatform;
}

public interface IUserActionPlatformSink
{
    void WriteLine(string level, string message);
}

public sealed class NoOpUserActionPlatformSink : IUserActionPlatformSink
{
    public static NoOpUserActionPlatformSink Instance { get; } = new();
    public void WriteLine(string level, string message) { }
}

public sealed record UserActionLogDto(
    string EventId,
    DateTimeOffset TimestampUtc,
    string Source,
    string ClientPlatform,
    string SessionId,
    string CorrelationId,
    Guid? UserId,
    string? Email,
    string? Role,
    Guid? DealerId,
    string Category,
    string Action,
    string? Screen,
    string? Target,
    string? HttpMethod,
    string? Path,
    int? StatusCode,
    string Outcome,
    long? DurationMs,
    IReadOnlyDictionary<string, string>? Metadata)
{
    public string SourceBadge => $"{Source} · {ClientPlatform}";

    public string DetailSummary
    {
        get
        {
            var parts = new List<string>(4);
            if (!string.IsNullOrWhiteSpace(Screen)) parts.Add(Screen);
            if (!string.IsNullOrWhiteSpace(Target)) parts.Add(Target);
            if (StatusCode.HasValue && StatusCode.Value > 0) parts.Add($"HTTP {StatusCode.Value}");
            if (DurationMs.HasValue) parts.Add($"{DurationMs.Value}ms");
            return parts.Count > 0 ? string.Join(" · ", parts) : Category;
        }
    }

    public string CorrelationCaption => $"CID: {CorrelationId}";
}

public sealed record UserActionLogIngestItemDto(
    string EventId,
    DateTimeOffset TimestampUtc,
    string Source,
    string ClientPlatform,
    string SessionId,
    string CorrelationId,
    string Category,
    string Action,
    string? Screen,
    string? Target,
    string? HttpMethod,
    string? Path,
    int? StatusCode,
    string Outcome,
    long? DurationMs,
    Dictionary<string, string>? Metadata);

public sealed record UserActionLogBatchPayload(List<UserActionLogIngestItemDto> Events);

public sealed record UserActionLogIngestResultDto(
    int AcceptedCount,
    int RejectedCount,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc);

public sealed record UserActionLogQueryResultDto(
    IReadOnlyList<UserActionLogDto> Items,
    int TotalBufferedCount,
    IReadOnlyDictionary<string, int> CountsBySource,
    IReadOnlyDictionary<string, int> CountsByPlatform,
    string LogFilePath);

public interface IUserActionLogger
{
    string SessionId { get; }
    string ClientPlatform { get; }
    string CurrentScreen { get; }
    string LocalLogFilePath { get; }

    event EventHandler<UserActionLogDto>? ActionLogged;

    void SetCurrentScreen(string screen);
    string CreateCorrelationId();

    UserActionLogDto Track(
        string action,
        string category = "UserInteraction",
        string? target = null,
        string outcome = "Success",
        string? correlationId = null,
        string? httpMethod = null,
        string? path = null,
        int? statusCode = null,
        long? durationMs = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        bool syncToServer = true);

    IReadOnlyList<UserActionLogDto> GetRecentLogs(int take = 100);
    Task FlushPendingAsync(CancellationToken cancellationToken = default);
    void ClearLocalLogs();
}

/// <summary>
/// Ambient DI-populated hub so all 17 ViewModels can record user actions without breaking unit-test constructors.
/// </summary>
public static class MobileUserActionLogHub
{
    public static IUserActionLogger? Current { get; set; }
}

/// <summary>
/// Pure net10.0 mobile user action logger for Android and Apple iOS:
/// emits structured logs to Android logcat / iOS OSLog via ILogger + IUserActionPlatformSink,
/// writes durable rolling JSON Lines (user_actions.ndjson) alongside the 9-table SQLCipher cache,
/// keeps a bounded in-memory ring, and automatically batches events to POST /api/v1/activity-logs.
/// </summary>
public sealed class UserActionLogger : IUserActionLogger, IDisposable
{
    public const string ActivityLogHttpClientName = "DealerActivityLogClient";
    public const int MaxInMemoryEntries = 300;
    public const long MaxLogFileBytes = 1_048_576; // 1 MB rotation cap

    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "passwd",
        "pwd",
        "token",
        "secret",
        "authorization",
        "cookie",
        "apikey",
        "api_key",
        "signingkey",
        "passphrase",
        "credential",
        "jwt"
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiEndpointProvider _endpointProvider;
    private readonly ISecureTokenStore _tokenStore;
    private readonly MobileSessionState _sessionState;
    private readonly IConnectivityMonitor _connectivityMonitor;
    private readonly IUserActionPlatformSink _platformSink;
    private readonly ILogger _logger;
    private readonly object _syncLock = new();
    private readonly LinkedList<UserActionLogDto> _ring = new();
    private readonly Queue<UserActionLogIngestItemDto> _uploadQueue = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly Timer _flushTimer;

    private string _currentScreen = "//public/showroom";
    private bool _disposed;

    public UserActionLogger(
        IHttpClientFactory httpClientFactory,
        IApiEndpointProvider endpointProvider,
        ISecureTokenStore tokenStore,
        MobileSessionState sessionState,
        IDatabasePathProvider databasePathProvider,
        IAppPlatformInfo platformInfo,
        IConnectivityMonitor connectivityMonitor,
        IUserActionPlatformSink platformSink,
        ILoggerFactory? loggerFactory = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        ArgumentNullException.ThrowIfNull(databasePathProvider);
        _connectivityMonitor = connectivityMonitor ?? throw new ArgumentNullException(nameof(connectivityMonitor));
        _platformSink = platformSink ?? NoOpUserActionPlatformSink.Instance;

        ClientPlatform = platformInfo?.ClientPlatform ?? "MobileCore";
        SessionId = $"mob-{Guid.CreateVersion7():N}"[..20];
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger("MotorcycleDealer.UserAction");

        var dbDir = Path.GetDirectoryName(databasePathProvider.GetDatabasePath());
        if (string.IsNullOrWhiteSpace(dbDir))
        {
            dbDir = Path.GetTempPath();
        }

        LocalLogFilePath = Path.Combine(dbDir, "user_actions.ndjson");
        LoadExistingTail();

        _connectivityMonitor.ConnectivityChanged += OnConnectivityChanged;
        _flushTimer = new Timer(_ => _ = FlushPendingSafeAsync(), null, Timeout.Infinite, Timeout.Infinite);

        MobileUserActionLogHub.Current = this;
    }

    public string SessionId { get; }
    public string ClientPlatform { get; }
    public string CurrentScreen => _currentScreen;
    public string LocalLogFilePath { get; }

    public event EventHandler<UserActionLogDto>? ActionLogged;

    public void SetCurrentScreen(string screen)
    {
        var cleaned = CleanOptional(screen, 120);
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            _currentScreen = cleaned;
        }
    }

    public string CreateCorrelationId() => $"cid-{Guid.CreateVersion7():N}"[..24];

    public UserActionLogDto Track(
        string action,
        string category = "UserInteraction",
        string? target = null,
        string outcome = "Success",
        string? correlationId = null,
        string? httpMethod = null,
        string? path = null,
        int? statusCode = null,
        long? durationMs = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        bool syncToServer = true)
    {
        var cleanAction = CleanText(action, 128, "UnknownAction");
        var cleanCategory = CleanText(category, 48, "UserInteraction");
        var cleanOutcome = CleanText(outcome, 32, "Success");
        var cleanCid = CleanText(correlationId, 64, CreateCorrelationId());
        var sanitizedMeta = SanitizeMetadata(metadata);

        var isAuthenticated = _sessionState.IsAuthenticated;
        var entry = new UserActionLogDto(
            EventId: $"mobact-{Guid.CreateVersion7():N}"[..24],
            TimestampUtc: DateTimeOffset.UtcNow,
            Source: "MobileApp",
            ClientPlatform: ClientPlatform,
            SessionId: SessionId,
            CorrelationId: cleanCid,
            UserId: isAuthenticated && _sessionState.UserId != Guid.Empty ? _sessionState.UserId : null,
            Email: isAuthenticated && !string.IsNullOrWhiteSpace(_sessionState.Email) ? _sessionState.Email : null,
            Role: isAuthenticated && !string.IsNullOrWhiteSpace(_sessionState.Role) ? _sessionState.Role : "Anonymous",
            DealerId: isAuthenticated ? _sessionState.DealerId : null,
            Category: cleanCategory,
            Action: cleanAction,
            Screen: _currentScreen,
            Target: CleanOptional(target, 200),
            HttpMethod: CleanOptional(httpMethod, 16),
            Path: CleanOptional(SanitizePath(path), 320),
            StatusCode: statusCode,
            Outcome: cleanOutcome,
            DurationMs: durationMs.HasValue ? Math.Max(0, durationMs.Value) : null,
            Metadata: sanitizedMeta);

        var serialized = JsonSerializer.Serialize(entry, MobileJsonSerializerContext.Default.UserActionLogDto);
        var shouldTriggerImmediateFlush = false;

        lock (_syncLock)
        {
            _ring.AddFirst(entry);
            while (_ring.Count > MaxInMemoryEntries)
            {
                _ring.RemoveLast();
            }

            AppendLineToLocalFile(serialized);

            if (syncToServer)
            {
                _uploadQueue.Enqueue(new UserActionLogIngestItemDto(
                    EventId: entry.EventId,
                    TimestampUtc: entry.TimestampUtc,
                    Source: entry.Source,
                    ClientPlatform: entry.ClientPlatform,
                    SessionId: entry.SessionId,
                    CorrelationId: entry.CorrelationId,
                    Category: entry.Category,
                    Action: entry.Action,
                    Screen: entry.Screen,
                    Target: entry.Target,
                    HttpMethod: entry.HttpMethod,
                    Path: entry.Path,
                    StatusCode: entry.StatusCode,
                    Outcome: entry.Outcome,
                    DurationMs: entry.DurationMs,
                    Metadata: sanitizedMeta is not null ? new Dictionary<string, string>(sanitizedMeta, StringComparer.Ordinal) : null));

                while (_uploadQueue.Count > 200)
                {
                    _uploadQueue.Dequeue();
                }

                shouldTriggerImmediateFlush = _uploadQueue.Count >= 6;
            }
        }

        EmitPlatformAndStructuredLog(entry);

        if (syncToServer && !_disposed)
        {
            if (shouldTriggerImmediateFlush)
            {
                _ = FlushPendingSafeAsync();
            }
            else
            {
                try
                {
                    _flushTimer.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // Disposed during shutdown.
                }
            }
        }

        ActionLogged?.Invoke(this, entry);
        return entry;
    }

    public IReadOnlyList<UserActionLogDto> GetRecentLogs(int take = 100)
    {
        var bounded = Math.Clamp(take, 1, MaxInMemoryEntries);
        lock (_syncLock)
        {
            return _ring.Take(bounded).ToArray();
        }
    }

    public async Task FlushPendingAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_connectivityMonitor.IsConnected)
        {
            return;
        }

        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<UserActionLogIngestItemDto> batch;
            lock (_syncLock)
            {
                if (_uploadQueue.Count == 0)
                {
                    return;
                }

                var count = Math.Min(40, _uploadQueue.Count);
                batch = new List<UserActionLogIngestItemDto>(count);
                for (var i = 0; i < count; i++)
                {
                    batch.Add(_uploadQueue.Dequeue());
                }
            }

            try
            {
                var client = _httpClientFactory.CreateClient(ActivityLogHttpClientName);
                var uri = _endpointProvider.BuildApiUri("/api/v1/activity-logs");
                using var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Headers.TryAddWithoutValidation(MobileUserActionHeaders.ClientPlatform, ClientPlatform);
                request.Headers.TryAddWithoutValidation(MobileUserActionHeaders.ClientSessionId, SessionId);
                request.Headers.TryAddWithoutValidation(MobileUserActionHeaders.ClientScreen, _currentScreen);
                request.Headers.TryAddWithoutValidation(MobileUserActionHeaders.CorrelationId, CreateCorrelationId());

                var token = await _tokenStore.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var payload = new UserActionLogBatchPayload(batch);
                request.Content = JsonContent.Create(payload, MobileJsonSerializerContext.Default.UserActionLogBatchPayload);

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    RequeueBatch(batch);
                }
            }
            catch
            {
                RequeueBatch(batch);
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public void ClearLocalLogs()
    {
        lock (_syncLock)
        {
            _ring.Clear();
            try
            {
                if (File.Exists(LocalLogFilePath))
                {
                    File.Delete(LocalLogFilePath);
                }
            }
            catch
            {
                // Ignore local filesystem errors.
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connectivityMonitor.ConnectivityChanged -= OnConnectivityChanged;
        _flushTimer.Dispose();
        _flushLock.Dispose();
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            _ = FlushPendingSafeAsync();
        }
    }

    private async Task FlushPendingSafeAsync()
    {
        try
        {
            await FlushPendingAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Telemetry flush must never crash mobile UI flows.
        }
    }

    private void RequeueBatch(List<UserActionLogIngestItemDto> batch)
    {
        lock (_syncLock)
        {
            if (_uploadQueue.Count + batch.Count > 200)
            {
                return;
            }

            foreach (var item in batch)
            {
                _uploadQueue.Enqueue(item);
            }
        }
    }

    private void EmitPlatformAndStructuredLog(UserActionLogDto entry)
    {
        var formatted =
            $"[USER-ACTION] Platform={entry.ClientPlatform} User={entry.Email ?? "anonymous"}({entry.Role ?? "Anonymous"}) " +
            $"Category={entry.Category} Action={entry.Action} Screen={entry.Screen ?? "-"} Target={entry.Target ?? "-"} " +
            $"Outcome={entry.Outcome} Status={entry.StatusCode ?? 0} DurationMs={entry.DurationMs ?? 0} CID={entry.CorrelationId}";

        var isWarning = string.Equals(entry.Outcome, "Denied", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.Outcome, "Failed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.Outcome, "ClientError", StringComparison.OrdinalIgnoreCase);
        var isError = string.Equals(entry.Outcome, "ServerError", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(entry.Outcome, "Error", StringComparison.OrdinalIgnoreCase);

        if (isError)
        {
            _logger.LogError("{UserActionLine}", formatted);
            _platformSink.WriteLine("ERROR", formatted);
        }
        else if (isWarning)
        {
            _logger.LogWarning("{UserActionLine}", formatted);
            _platformSink.WriteLine("WARN", formatted);
        }
        else
        {
            _logger.LogInformation("{UserActionLine}", formatted);
            _platformSink.WriteLine("INFO", formatted);
        }
    }

    private void AppendLineToLocalFile(string line)
    {
        try
        {
            var dir = Path.GetDirectoryName(LocalLogFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(LocalLogFilePath) && new FileInfo(LocalLogFilePath).Length > MaxLogFileBytes)
            {
                var backup = LocalLogFilePath + ".prev";
                File.Move(LocalLogFilePath, backup, overwrite: true);
            }

            using var stream = new FileStream(LocalLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine(line);
        }
        catch
        {
            // Keep app resilient on low-storage devices.
        }
    }

    private void LoadExistingTail()
    {
        try
        {
            if (!File.Exists(LocalLogFilePath))
            {
                return;
            }

            var tail = File.ReadLines(LocalLogFilePath).TakeLast(MaxInMemoryEntries).ToArray();
            lock (_syncLock)
            {
                foreach (var line in tail)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        var parsed = JsonSerializer.Deserialize(line, MobileJsonSerializerContext.Default.UserActionLogDto);
                        if (parsed is not null)
                        {
                            _ring.AddFirst(parsed);
                            while (_ring.Count > MaxInMemoryEntries)
                            {
                                _ring.RemoveLast();
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip truncated lines safely.
                    }
                }
            }
        }
        catch
        {
            // Ignore startup read failures.
        }
    }

    private static string CleanText(string? raw, int maxLength, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var sb = new StringBuilder(Math.Min(raw.Length, maxLength));
        foreach (var ch in raw.Trim())
        {
            if (sb.Length >= maxLength)
            {
                break;
            }

            if (ch is '\r' or '\n' or '\t')
            {
                sb.Append(' ');
            }
            else if (!char.IsControl(ch))
            {
                sb.Append(ch);
            }
        }

        var text = sb.ToString().Trim();
        return text.Length == 0 ? fallback : text;
    }

    private static string? CleanOptional(string? raw, int maxLength)
    {
        var text = CleanText(raw, maxLength, string.Empty);
        return text.Length == 0 ? null : text;
    }

    private static string? SanitizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var qIdx = path.IndexOf('?');
        if (qIdx < 0)
        {
            return path;
        }

        var basePath = path[..qIdx];
        var queryParts = path[(qIdx + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries);
        var safeParts = new List<string>(queryParts.Length);
        foreach (var seg in queryParts)
        {
            var eq = seg.IndexOf('=');
            var key = eq >= 0 ? seg[..eq] : seg;
            safeParts.Add(IsSensitiveKey(key) ? $"{key}=[REDACTED]" : seg);
        }

        return $"{basePath}?{string.Join('&', safeParts)}";
    }

    private static IReadOnlyDictionary<string, string>? SanitizeMetadata(IReadOnlyDictionary<string, string>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return null;
        }

        var safe = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in raw.Take(16))
        {
            var key = CleanText(pair.Key, 64, string.Empty);
            if (key.Length == 0)
            {
                continue;
            }

            safe[key] = IsSensitiveKey(key) ? "[REDACTED]" : CleanText(pair.Value, 240, string.Empty);
        }

        return safe.Count == 0 ? null : safe;
    }

    private static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        foreach (var fragment in SensitiveKeyFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
