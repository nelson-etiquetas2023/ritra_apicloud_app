using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Picking;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class PickingViewModel : ObservableObject
    {
        private readonly IPickingService _pickingService;

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

        public PickingViewModel(IPickingService pickingService)
        {
            _pickingService = pickingService;
        }

        [RelayCommand]
        public async Task SincronizarPickingAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Sincronizando picking desde el servidor...";

                var ok = await _pickingService.SincronizarPickingDesdeApiAsync();

                if (ok)
                {
                    await CargarPickingLocalAsync();
                    var toast = Toast.Make("Picking sincronizado correctamente.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                }
                else
                {
                    var toast = Toast.Make("Error al sincronizar picking.", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    await toast.Show();
                }
            }
            catch (Exception ex)
            {
                var toast = Toast.Make($"Error: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }

        [RelayCommand]
        public async Task CargarPickingLocalAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Cargando picking local...";

                var result = await _pickingService.GetPickingOrdersLocalAsync();
                _allOrders = result;
                OrdenesPicking = new ObservableCollection<PickingOrder>(_allOrders);
                FilterOrders(SearchText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PickingViewModel] Error cargando picking local: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
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
