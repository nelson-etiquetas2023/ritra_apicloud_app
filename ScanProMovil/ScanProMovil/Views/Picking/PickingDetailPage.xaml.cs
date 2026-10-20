using ScanProMovil.Data.Entities;
using ScanProMovil.ViewModels;

namespace ScanProMovil.Views.Picking;

public partial class PickingDetailPage : ContentPage
{
    private readonly PickingDetailViewModel _vm;

    public PickingDetailPage(PickingOrder orden)
    {
        InitializeComponent();
        _vm = MauiProgram.Services!.GetRequiredService<PickingDetailViewModel>();

        // Al completar, volver a la lista de picking
        _vm.OnCompletado = async () =>
        {
            await Navigation.PopAsync();
        };

        _vm.Inicializar(orden);
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // El binding ya refresca automáticamente las propiedades del ViewModel.
    }
}
