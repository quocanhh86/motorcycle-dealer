using CommunityToolkit.Mvvm.ComponentModel;
using MotorcycleDealer.Mobile.Core.Logging;

namespace MotorcycleDealer.Mobile.Core.ViewModels;

/// <summary>
/// Shared observable base for all 17 mobile ViewModels using C# 13 partial [ObservableProperty] syntax.
/// </summary>
public abstract partial class MobileViewModelBase : ObservableObject
{
    protected MobileViewModelBase()
    {
        SyncAgeLabel = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsOfflineBannerVisible { get; set; }

    [ObservableProperty]
    public partial string SyncAgeLabel { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string SuccessMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnSuccessMessageChanged(string value) => OnPropertyChanged(nameof(HasSuccess));

    protected void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    protected void TrackUserAction(
        string action,
        string category = "UserInteraction",
        string? target = null,
        string outcome = "Success",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        MobileUserActionLogHub.Current?.Track(
            action: action,
            category: category,
            target: target,
            outcome: outcome,
            metadata: metadata);
    }
}
