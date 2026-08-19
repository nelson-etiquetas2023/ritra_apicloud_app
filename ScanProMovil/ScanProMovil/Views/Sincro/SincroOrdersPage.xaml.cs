using ScanProMovil.ViewModels;
using System.Collections.Specialized;

namespace ScanProMovil.Views.Sincro;

public partial class SincroOrdersPage : ContentPage
{
	private readonly SincroOrdersViewModel _vm;

    public SincroOrdersPage()
	{
		InitializeComponent();
		_vm = new SincroOrdersViewModel();
		BindingContext = _vm;
        if (_vm.Logs != null)
        {
            _vm.Logs.CollectionChanged += Logs_CollectionChanged;
        }
	}

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0) 
        {

            var lastItem = e.NewItems[e.NewItems.Count - 1];
            // Ejecutar en el hilo principal para que funcione bien
            // Forzar ejecución en el hilo principal y esperar un ciclo de render
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50); // pequeño delay para que el CollectionView se actualice
                CollectionViewLogs.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
            });

        }
    }

    private async void btnStartSync_Clicked(object? sender, EventArgs? e)
    {
		await _vm.ExecuteTestSincroCommand.ExecuteAsync(null);
    }
}