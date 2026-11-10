using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using ScanProMovil.ViewModels;
using ScanProMovil.ViewModels.Compras;

namespace ScanProMovil.Views.Compras;

public partial class ComprasIndexPage : ContentPage
{
    private readonly ComprasViewModel _vm;
    public ComprasIndexPage(ComprasViewModel vm)
	{
		InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UiGuard.Run(this, async () =>
        {
            if (_vm.RefreshListOrders)
            {
                await Task.Yield();
                await _vm.GetOrdersLocalSqliteCommand.ExecuteAsync(null);
            }

            bool selectMultiple = Preferences.Get("SelectMultipleRows", false);

            if (selectMultiple)
            {
                //habilitar la seleccion multiple en la lista de ordenes.
                CvOrdenes.SelectionMode = SelectionMode.Multiple;
            }
        });
    }

    private void OnSincronizarClicked(object? sender, EventArgs e) => UiGuard.Run(this, async () =>
    {
        await _vm.SincronizarComprasCommand.ExecuteAsync(null);
    });

    private void btnAddOrders_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        //Validar la orden de Compra Seleccionada.
        _vm.RefreshListOrders = true;
        var page = MauiProgram.Services!.GetService<AddComprasPage>();
        if (page is null)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir el formulario de compra.", "OK");
            return;
        }
        await Navigation.PushAsync(page);
    });

    private void btnDetailsOrders_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        // Validar la orden seleccionada.
        if (_vm.SelectedOrder == null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar una orden de la Lista...", "Ok.");
            return;
        }

        if (_vm.SelectedOrder.Status == 2)
        {
            await DisplayAlertAsync("Documento Sincronizado",
                "El documento ya fue sincronizado, no se puede modificar.", "Ok.");
            return;
        }

        _vm.RefreshListOrders = false;
        await Navigation.PushAsync(new DetailsComprasPage(_vm.SelectedOrder));
    });

    private void btnConfig_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        _vm.RefreshListOrders = true;
        var page = MauiProgram.Services!.GetService<ConfigComprasPage>();
        if (page is null)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la configuración de compras.", "OK");
            return;
        }
        await Navigation.PushAsync(page);
    });

    private void btn_sincrOrdenes_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        // Validar la orden seleccionada.
        if (_vm.SelectedOrder == null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar una orden de la Lista...", "Ok.");
            return;
        }

        if (_vm.SelectedOrder.Status == 2)
        {
            await DisplayAlertAsync("Documento Sincronizado",
                "El documento ya fue sincronizado...", "Ok.");
            return;
        }

        //al volver refresca la lista para mostrar el estado sincronizado.
        _vm.RefreshListOrders = true;

        //resolver el servico de compras desde contenedor DI
        var serviceSincro = MauiProgram.Services!.GetService<IComprasService>();

        //crear el viewmodel con injeccion de servicio + orden.
        var vmSincro = new SincroComprasViewModels(serviceSincro!, _vm.SelectedOrder);

        //creo la pagina desde el contenedo DI con la vm
        //que tiene el servicio + orden seleccionada.

        var page = MauiProgram.Services!.GetService<SincroComprasPage>();
        if (page is null)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la pantalla de sincronización.", "OK");
            return;
        }
        page.BindingContext = vmSincro;

        await Navigation.PushAsync(page);
    });

    private void btnDeleteOrder_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedOrder == null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar una orden de la Lista...", "Ok.");
            return;
        }

        if (_vm.SelectedOrder.Status == 2)
        {
            await DisplayAlertAsync("Documento Sincronizado",
                "El documento ya fue sincronizado, no se puede eliminar.", "Ok.");
            return;
        }

        var confirm = await DisplayAlertAsync("Eliminar documento",
            $"¿Seguro que desea eliminar el documento {_vm.SelectedOrder.Numero}?",
            "Eliminar", "Cancelar");
        if (!confirm) return;

        var service = MauiProgram.Services!.GetService<IComprasService>();
        if (service is null) return;

        var deleted = await service.DeleteOrder(_vm.SelectedOrder.Numero);
        if (deleted)
        {
            _vm.RefreshListOrders = true;
            await _vm.GetOrdersLocalSqliteCommand.ExecuteAsync(null);
            await DisplayAlertAsync("Documento eliminado",
                $"El documento {_vm.SelectedOrder.Numero} fue eliminado.", "Ok.");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo eliminar el documento.", "Ok.");
        }
    });

    private void btnCancelOrder_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedOrder == null)
        {
            await DisplayAlertAsync("Warning", "Tiene que seleccionar una orden de la Lista...", "Ok.");
            return;
        }

        if (_vm.SelectedOrder.Status == 2)
        {
            await DisplayAlertAsync("Documento Sincronizado",
                "El documento ya fue sincronizado, no se puede cerrar.", "Ok.");
            return;
        }

        var confirm = await DisplayAlertAsync("Cerrar documento",
            $"¿Seguro que desea cerrar el documento {_vm.SelectedOrder.Numero}?",
            "Cerrar", "Cancelar");
        if (!confirm) return;

        var service = MauiProgram.Services!.GetService<IComprasService>();
        if (service is null) return;

        var deactivated = await service.DeactivateOrder(_vm.SelectedOrder.Numero);
        if (deactivated)
        {
            _vm.RefreshListOrders = true;
            await _vm.GetOrdersLocalSqliteCommand.ExecuteAsync(null);
            await DisplayAlertAsync("Documento cerrado",
                $"El documento {_vm.SelectedOrder.Numero} fue cerrado.", "Ok.");
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo cerrar el documento.", "Ok.");
        }
    });

    private void btnReceiveOrders_Clicked(object? sender, EventArgs? e) => UiGuard.Run(this, async () =>
    {
        if (_vm.SelectedCount == 0)
        {
            await DisplayAlertAsync("Warning", "Seleccione al menos una orden de compra para recibir.", "Ok.");
            return;
        }

        var confirm = await DisplayAlertAsync("Recibir compras",
            $"¿Recibir {_vm.SelectedCount} orden(es) seleccionada(s)? Esto afectara el inventario.",
            "Recibir", "Cancelar");
        if (!confirm) return;

        await _vm.ReceiveSelectedCommand.ExecuteAsync(null);

        //Resumen de resultados.
        if (_vm.ReceiveResults.Count > 0)
        {
            var exitos = _vm.ReceiveResults.Count(r => r.Success);
            var fallos = _vm.ReceiveResults.Count(r => !r.Success);
            var msg = $"Procesadas: {_vm.ReceiveResults.Count}\nExitosas: {exitos}\nFallidas: {fallos}";

            if (fallos > 0)
            {
                var errores = string.Join("\n", _vm.ReceiveResults
                    .Where(r => !r.Success)
                    .Select(r => $"{r.Message}"));
                msg += $"\n\nErrores:\n{errores}";
            }

            await DisplayAlertAsync("Resultado de Recepcion", msg, "Ok.");
        }
    });

/// <summary>Abre el detalle de productos de la orden al hacer tap en el card.</summary>
    private void OnOrdenCardTapped(object? sender, TappedEventArgs e)
    {
        UiGuard.Run(this, async () =>
        {
            if (sender is not Border border || border.BindingContext is not OrdenCompra orden) return;

            // Cargar la orden (con todos sus productos) desde la base local para
            // asegurar que se muestren todos los productos de la orden de compra.
            var service = MauiProgram.Services!.GetService<IComprasService>();
            var ordenCompleta = orden;
            if (service is not null)
            {
                try
                {
                    var cargada = await service.getOrderById(orden.Numero);
                    if (cargada is not null && cargada.Items is not null && cargada.Items.Count > 0)
                        ordenCompleta = cargada;
                }
                catch { }
            }

            var page = new ComprasDetalleProductosPage(ordenCompleta);
            await Navigation.PushAsync(page);
        });
    }
}