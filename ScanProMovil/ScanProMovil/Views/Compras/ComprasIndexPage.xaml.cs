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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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
    }



    private async void btnAddOrders_Clicked(object? sender, EventArgs? e)
    {
        //Validar la orden de Compra Seleccionada.
        _vm.RefreshListOrders = true;
        var page = MauiProgram.Services!.GetService<AddComprasPage>();
        await Navigation.PushAsync(page!);
    }

    private async void btnDetailsOrders_Clicked(object? sender, EventArgs? e)
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
    }

    private async void btnConfig_Clicked(object? sender, EventArgs? e)
    {
        _vm.RefreshListOrders = true;
        var page = MauiProgram.Services!.GetService<ConfigComprasPage>();
        await Navigation.PushAsync(page!);
    }

    private async void btn_sincrOrdenes_Clicked(object? sender, EventArgs? e)
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
        page!.BindingContext = vmSincro;


        await Navigation.PushAsync(page!);
    }

    private async void btnDeleteOrder_Clicked(object? sender, EventArgs? e)
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
    }

    private async void btnCancelOrder_Clicked(object? sender, EventArgs? e)
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
    }
}