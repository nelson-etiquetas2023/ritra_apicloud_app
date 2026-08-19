using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Orders;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
	{
		InitializeComponent();     
    }

    private async void Btn_SaveConfigOrders_Clicked(object? sender, EventArgs? e)
    {
        var vm = BindingContext as OrderConfigViewModel;
        await vm!.SaveConfigCommand.ExecuteAsync(null);    

        

    }
}