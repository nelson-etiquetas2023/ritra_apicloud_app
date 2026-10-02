using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using ScanProMovil.ViewModels;
using ScanProMovil.ViewModels.Compras;

namespace ScanProMovil.Views.Compras;

public partial class DetailsComprasPage : ContentPage
{
	private readonly DetailsComprasViewModels _vm;

	public DetailsComprasPage(OrdenCompra order)
	{
		InitializeComponent();
		_vm = new DetailsComprasViewModels(order);
		BindingContext = _vm;

    }

    private void OnSincroTapped(object? sender, TappedEventArgs e) => UiGuard.Run(this, async () =>
    {
        if (_vm.Order.Status == 2)
        {
            await DisplayAlertAsync("Documento Sincronizado",
                "El documento ya fue sincronizado.", "Ok.");
            return;
        }

        var service = MauiProgram.Services!.GetService<IComprasService>();
        if (service is null) return;

        var vmSincro = new SincroComprasViewModels(service, _vm.Order);
        var page = MauiProgram.Services!.GetService<SincroComprasPage>();
        if (page is null)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la pantalla de sincronización.", "OK");
            return;
        }
        page.BindingContext = vmSincro;

        await Navigation.PushAsync(page);
    });

    private void OnPrintTapped(object? sender, TappedEventArgs e)
        => UiGuard.Run(this, () => DisplayAlertAsync("Imprimir",
            "La impresión del documento está en desarrollo.", "Ok."));

    private void OnDownloadTapped(object? sender, TappedEventArgs e)
        => UiGuard.Run(this, () => DisplayAlertAsync("Descargar",
            "La descarga del documento está en desarrollo.", "Ok."));
}