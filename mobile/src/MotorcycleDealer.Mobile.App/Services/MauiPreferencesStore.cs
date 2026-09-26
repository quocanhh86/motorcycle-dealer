using MotorcycleDealer.Mobile.Core.Abstractions;

namespace MotorcycleDealer.Mobile.App.Services;

/// <summary>
/// Native cross-platform preferences store backed by Microsoft.Maui.Storage.Preferences
/// (Android SharedPreferences / iOS NSUserDefaults), keeping the SQLCipher database at exactly 9 tables.
/// </summary>
public sealed class MauiPreferencesStore : IAppPreferencesStore
{
    public string GetString(string key, string defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            return Preferences.Default.Get(key, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetString(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            Preferences.Default.Set(key, value ?? string.Empty);
        }
        catch
        {
            // Keep UI theme/locale toggles resilient if native storage is temporarily unavailable
        }
    }
}
