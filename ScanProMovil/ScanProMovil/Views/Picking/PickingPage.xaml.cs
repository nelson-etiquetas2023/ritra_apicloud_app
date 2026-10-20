using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Picking;

public partial class PickingPage : ContentPage
{
    private readonly PickingViewModel _vm;

    public PickingPage(PickingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.CargarPickingLocalAsync();
    }

    private void Btn_Sincronizar_Clicked(object? sender, EventArgs e)
    {
        _ = _vm.SincronizarPickingAsync();
    }

    private void Btn_Detalle_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PickingOrder orden)
        {
            _ = Navigation.PushAsync(new PickingDetailPage(orden));
        }
    }
}
