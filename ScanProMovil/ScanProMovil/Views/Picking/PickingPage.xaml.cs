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

    private bool _cargaInicialHecha;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Solo carga la primera vez. Al volver del detalle no se recarga:
        // la tarjeta comparte la instancia con el detalle y ya viene
        // actualizada (progreso 3/3 y estado "End Picked"); una recarga
        // aqui reemplazaria las entidades por copias viejas de la BD.
        if (_cargaInicialHecha) return;
        _cargaInicialHecha = true;

        // Sincronizacion discreta al entrar: baja del servidor los pendientes
        // liberados (nuevos Ids de picking) sin depender del boton Sincronizar.
        // Despues ya recarga la lista local con lo que trajo.
        _ = _vm.SincronizarEnSegundoPlanoAsync();
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

    private void OnPickingCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is PickingOrder orden)
        {
            _ = Navigation.PushAsync(new PickingDetailPage(orden));
        }
    }
}
