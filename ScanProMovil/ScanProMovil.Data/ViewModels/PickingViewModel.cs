using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services;
using ScanProMovil.Services.Picking;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class PickingViewModel : ObservableObject
    {
        private readonly IPickingService _pickingService;
        private readonly IAlertaNotifier _alerta;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PickingOrder> ordenesPicking = [];

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PickingOrder> filteredOrders = [];

        private List<PickingOrder> _allOrders = new();

        /// <summary>Evita que se lancen dos sincronizaciones a la vez.</summary>
        private bool _sincronizando;

        public PickingViewModel(IPickingService pickingService, IAlertaNotifier alerta)
        {
            _pickingService = pickingService;
            _alerta = alerta;
        }

        [RelayCommand]
        public async Task SincronizarPickingAsync()
        {
            if (_sincronizando) return;

            try
            {
                _sincronizando = true;
                IsLoading = true;
                LoadingMessage = "Sincronizando picking desde el servidor...";

                var ok = await _pickingService.SincronizarPickingDesdeApiAsync();

                // La carga local se hace siempre, se haya sincronizado bien o
                // no. Si se omitiera, la lista se quedaria igual y no se
                // verian los pedidos que acaba de traer el servidor.
                await CargarPickingLocalAsync();

                await _alerta.MostrarAsync(
                    ok ? "Picking sincronizado correctamente." : "Error al sincronizar picking.",
                    ok ? TipoAlerta.Exito : TipoAlerta.Error);
            }
            catch (Exception ex)
            {
                await _alerta.MostrarAsync($"Error: {ex.Message}", TipoAlerta.Error);
            }
            finally
            {
                _sincronizando = false;
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task CargarPickingLocalAsync()
        {
            try
            {
                var result = await _pickingService.GetPickingOrdersLocalAsync();
                _allOrders = result;
                OrdenesPicking = new ObservableCollection<PickingOrder>(_allOrders);
                FilterOrders(SearchText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingViewModel] Error cargando picking local: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task MarcarCompletadoAsync(PickingOrder orden)
        {
            if (orden.Estado == 2) return;

            var ok = await _pickingService.MarcarCompletadoAsync(orden.PickingId);
            if (ok)
            {
                orden.Estado = 2;
                orden.FechaCompletado = DateTime.Now;
                FilterOrders(SearchText);
            }
        }

        private void FilterOrders(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredOrders = new ObservableCollection<PickingOrder>(_allOrders);
                return;
            }

            var filtered = _allOrders.Where(o =>
                o.NumeroPedidoVenta.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
                o.EstadoTexto.Contains(searchText, System.StringComparison.OrdinalIgnoreCase)
            ).ToList();

            FilteredOrders = new ObservableCollection<PickingOrder>(filtered);
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }
    }
}
