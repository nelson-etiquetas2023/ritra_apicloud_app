using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Orders;

public partial class OrderDetailsPage : ContentPage
{
    private readonly OrderDetailsViewModel _vm;

    public OrderDetailsPage(Order order)
	{
		InitializeComponent();

		_vm = new OrderDetailsViewModel(order);
		BindingContext = _vm;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, async () =>
        {
            _vm.IsLoading = true;
            await Task.Delay(2000); // Simulate a delay
            _vm.IsLoading = false;
        });
    }
}