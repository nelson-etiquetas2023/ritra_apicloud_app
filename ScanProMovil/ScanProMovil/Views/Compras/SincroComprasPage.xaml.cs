using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels.Compras;

namespace ScanProMovil.Views.Compras;

public partial class SincroComprasPage : ContentPage
{
    
    public SincroComprasPage()
	{
		InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, () => Task.CompletedTask);
    }

    private void btn_click_sincro_orders(object? sender, EventArgs? e)
        => UiGuard.Run(this, () => Task.CompletedTask);
}