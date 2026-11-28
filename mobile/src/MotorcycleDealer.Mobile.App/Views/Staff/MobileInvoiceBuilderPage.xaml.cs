using System.Windows.Input;
using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class MobileInvoiceBuilderPage : ContentPage, IQueryAttributable
{
    private Guid _lastBookingId;

    public MobileInvoiceBuilderViewModel ViewModel { get; }

    public ICommand RetryLoadCommand { get; }

    public MobileInvoiceBuilderPage(MobileInvoiceBuilderViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        RetryLoadCommand = new Command(async () =>
        {
            try
            {
                if (_lastBookingId != Guid.Empty)
                {
                    await ViewModel.LoadForBookingCommand.ExecuteAsync(_lastBookingId);
                }
            }
            catch
            {
                // Keep command delegate crash-safe
            }
        });

        InitializeComponent();
        BindingContext = ViewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("bookingId", out object? raw) &&
                Guid.TryParse(raw?.ToString(), out Guid bookingId) &&
                bookingId != Guid.Empty)
            {
                _lastBookingId = bookingId;
                await ViewModel.LoadForBookingCommand.ExecuteAsync(bookingId);
            }
        }
        catch
        {
            // Keep async void query navigation handler crash-safe
        }
    }
}
