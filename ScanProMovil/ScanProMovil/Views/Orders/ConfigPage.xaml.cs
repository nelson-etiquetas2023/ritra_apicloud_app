using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Orders;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
	{
		InitializeComponent();     
    }

    private void Btn_SaveConfigOrders_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        var vm = BindingContext as OrderConfigViewModel;
        if (vm is null)
        {
            await DisplayAlertAsync("Error", "No se encontró la configuración de órdenes.", "OK");
            return;
        }
        await vm.SaveConfigCommand.ExecuteAsync(null);
    });
}