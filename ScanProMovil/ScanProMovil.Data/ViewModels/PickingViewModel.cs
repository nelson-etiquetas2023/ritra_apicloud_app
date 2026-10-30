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
            => await SincronizarAsyncConAlertaAsync(mostrarAlerta: true);

        /// <summary>
        /// Sincronizacion discreta: baja los pendientes del servidor y recarga
        /// la lista sin un toast de exito. La usa la pantalla al entrar por
        /// primera vez, para que los pedidos liberados aparezcan solos.
        /// </summary>
        public async Task SincronizarEnSegundoPlanoAsync()
            => await SincronizarAsyncConAlertaAsync(mostrarAlerta: false);

        private async Task SincronizarAsyncConAlertaAsync(bool mostrarAlerta)
        {
            if (_sincronizando) return;

            try
            {
                _sincronizando = true;
                IsLoading = true;
                LoadingMessage = "Sincronizando picking desde el servidor...";

                Console.WriteLine("[PickingViewModel] Iniciando sincronización de picking desde API...");

                var ok = await _pickingService.SincronizarPickingDesdeApiAsync();

                Console.WriteLine($"[PickingViewModel] Sincronización API completada. ok={ok}");

                // La carga local se hace siempre, se haya sincronizado bien o
                // no. Si se omitiera, la lista se quedaria igual y no se
                // verian los pedidos que acaba de traer el servidor.
                await CargarPickingLocalAsync();

                if (mostrarAlerta)
                {
                    await _alerta.MostrarAsync(
                        ok ? "Picking sincronizado correctamente." : "Error al sincronizar picking.",
                        ok ? TipoAlerta.Exito : TipoAlerta.Error);
                }
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

                // Si la app se cerro despues del ultimo escaneo sin cerrar la
                // orden, aqui se recupera: cierra (remoto y local) para que la
                // web quede End Picked sin depender de que el usuario reabra.
                await RecuperarOrdenesCompletadasSinCerrarAsync();

                Console.WriteLine($"[PickingViewModel] Cargados localmente: {_allOrders.Count} pedidos pendientes.");

                FilterOrders(SearchText);

                Console.WriteLine($"[PickingViewModel] Mostrados tras filtro: {FilteredOrders.Count} pedidos.");
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

        /// <summary>
        /// Ordenes con todos los items escaneados que quedaron sin cerrar
        /// porque la app se cerro despues del ultimo escaneo. Se cierran
        /// automaticamente al cargar el listado: primero en el servidor
        /// (web queda End Picked) y luego localmente.
        /// </summary>
        private async Task RecuperarOrdenesCompletadasSinCerrarAsync()
        {
            var pendientesCerrar = _allOrders
                .Where(o => o.Estado != 2 && o.Items.Count != 0 && o.Items.All(i => i.Completado))
                .ToList();

            foreach (var orden in pendientesCerrar)
            {
                Console.WriteLine($"[PickingViewModel] Orden {orden.PickingId} ({orden.NumeroPedidoVenta}) con items completos sin cerrar: recuperando...");

                // Si ya estaba cerrada, no reintentar el cierre remoto; solo
                // asegurar el badge End Picked en memoria.
                if (orden.Estado < 2)
                {
                    await _pickingService.CompletarOrdenRemotoAsync(orden.PickingId);
                }

                if (await _pickingService.MarcarCompletadoAsync(orden.PickingId) || orden.Estado >= 2)
                {
                    // Igual que en el detalle: estado 3 solo en memoria para
                    // mostrar el badge PICKED; la proxima sincronizacion lo
                    // retira del listado.
                    orden.Estado = 3;
                    orden.FechaCompletado = DateTime.Now;
                    orden.NotificarProgreso();
                }
            }
        }

        private void FilterOrders(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredOrders = new ObservableCollection<PickingOrder>(_allOrders);
            }
            else
            {
                var filtered = _allOrders.Where(o =>
                    o.NumeroPedidoVenta.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
                    o.EstadoTexto.Contains(searchText, System.StringComparison.OrdinalIgnoreCase)
                ).ToList();

                FilteredOrders = new ObservableCollection<PickingOrder>(filtered);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }
    }
}
