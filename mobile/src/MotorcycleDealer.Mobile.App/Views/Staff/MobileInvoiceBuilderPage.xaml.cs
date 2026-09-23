using MotorcycleDealer.Mobile.Core.ViewModels.Staff;

namespace MotorcycleDealer.Mobile.App.Views.Staff;

public partial class MobileInvoiceBuilderPage : ContentPage, IQueryAttributable
{
    public MobileInvoiceBuilderViewModel ViewModel { get; }

    public MobileInvoiceBuilderPage(MobileInvoiceBuilderViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        BindingContext = ViewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("bookingId", out object? raw) &&
            Guid.TryParse(raw?.ToString(), out Guid bookingId) &&
            bookingId != Guid.Empty)
        {
            await ViewModel.LoadForBookingCommand.ExecuteAsync(bookingId);
        }
    }
}
