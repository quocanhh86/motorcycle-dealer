namespace MotorcycleDealer.Mobile.Core.Abstractions;

/// <summary>
/// Canonical preference storage keys persisted outside the 9-table SQLCipher database.
/// </summary>
public static class AppPreferenceKeys
{
    public const string Locale = "apex_locale";
    public const string Theme = "apex_theme";
    public const string ReducedMotion = "apex_reduced_motion";
}

/// <summary>
/// Pure-Core key-value preferences abstraction backed by native host storage preferences
/// in Mobile.App so theme and locale never add a 10th SQLCipher table.
/// </summary>
public interface IAppPreferencesStore
{
    string GetString(string key, string defaultValue);
    void SetString(string key, string value);
}

/// <summary>
/// Thread-safe in-memory fallback registered via TryAddSingleton when no native platform
/// preferences host is wired (e.g., pure net10.0 xUnit test suites).
/// </summary>
public sealed class InMemoryAppPreferencesStore : IAppPreferencesStore
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly Lock _sync = new();

    public string GetString(string key, string defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_sync)
        {
            return _values.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    public void SetString(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_sync)
        {
            _values[key] = value ?? string.Empty;
        }
    }
}
