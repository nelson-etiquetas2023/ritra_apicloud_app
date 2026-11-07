using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace ScanProMovil.ViewModels
{
    public partial class ComprasViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool refreshListOrders = true;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        public bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        private List<OrdenCompra> _allOrders = new();

        [ObservableProperty]
        private ObservableCollection<OrdenCompra> ordenes = [];

        [ObservableProperty]
        private int totalOrders;

        [ObservableProperty]
        private OrdenCompra? selectedOrder;

        [ObservableProperty]
        private int selectedCount;

        [ObservableProperty]
        private List<Shared.Dtos.Compras.ProcesarOrdenResult> receiveResults = new();

        public bool CanReceive => SelectedCount > 0;

        IComprasService Service;

        public ComprasViewModel(IComprasService service)
        {
            this.Service = service;
        }


        [RelayCommand]
        public async Task GetOrdersLocalSqlite()
        {
            if (IsLoading)
                return;

            await RecargarListaAsync();
        }

        /// <summary>Borra TODAS las órdenes de compra locales de SQLite (irreversible).</summary>
        [RelayCommand]
        public async Task DeleteAllOrdersLocal()
        {
            if (IsLoading)
                return;

            var confirm = false;
            var page = Application.Current?.Windows[0]?.Page;
            if (page is not null)
                confirm = await page.DisplayAlertAsync(
                    "Eliminar todas las órdenes",
                    "¿Seguro que deseas borrar TODAS las órdenes de compra locales? Esta acción no se puede deshacer y no afecta al servidor.",
                    "Sí, borrar todo",
                    "Cancelar");

            if (!confirm) return;

            try
            {
                IsLoading = true;
                loadingMessage = "Eliminando órdenes...";

                // Guardar los números de orden en la lista negra ANTES de borrarlos,
                // para que al sincronizar con el servidor no vuelvan a bajarse.
                var numerosBorrar = _allOrders.Select(o => o.Numero).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                Service.GuardarNumerosOrdenesBorradas(numerosBorrar);

                var ok = await Service.DeleteAllOrdersLocalAsync();
                if (ok)
                {
                    ordenes = [];
                    _allOrders = new();
                    FilterOrders(SearchText);
                    loadingMessage = string.Empty;
                    // Persistir en Preferences para que al entrar de nuevo no se
                    // sincronice con el servidor y traiga las órdenes de vuelta.
                    Preferences.Set("SkipAutoSyncCompras", true);
                    RefreshListOrders = false;

                    if (page is not null)
                        await page.DisplayAlertAsync(
                            "Órdenes eliminadas",
                            $"Se borraron {numerosBorrar.Count} orden(es) localmente.\nNo volverán a aparecer al sincronizar.\nUse 'Sincronizar' para traer solo órdenes nuevas del servidor.",
                            "Ok");
                }
                else
                {
                    await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync(
                        "Error", "No se pudieron eliminar las órdenes locales.", "Ok");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error eliminando órdenes locales: {ex.Message}");
                await Application.Current?.Windows[0]?.Page?.DisplayAlertAsync(
                    "Error", ex.Message, "Ok");
            }
            finally
            {
                IsLoading = false;
                loadingMessage = string.Empty;
            }
        }

        private async Task RecargarListaAsync(bool forzarSincronizacionRemota = false)
        {
            try
            {
                IsLoading = true;

                // Si no se forza la sincronización remota y el usuario pidió borrar
                // todo (SkipAutoSyncCompras = true), no se baja nada del servidor
                // para que el borrado local se mantenga.
                var skipAutoSync = Preferences.Get("SkipAutoSyncCompras", false);

                if (!skipAutoSync || forzarSincronizacionRemota)
                {
                    loadingMessage = "Sincronizando compras...";
                    await Service.SincronizarComprasDesdeApiAsync();
                    loadingMessage = "Cargando ordenes...";
                    Debug.WriteLine("Loading ON");
                }
                else
                {
                    loadingMessage = "Cargando ordenes locales...";
                    Debug.WriteLine("SkipAutoSync: saltando sincronización remota");
                }

                var result = await Service.GetOrdersLocalSqliteAsync();
                _allOrders = result;

                FilterOrders(SearchText);
            }
            catch (SqliteException ex)
            {
                Debug.Write("error al obtener los datos locales, error:" + ex.Message);
            }
            finally
            {
                IsLoading = false;
                loadingMessage = string.Empty;
                Debug.WriteLine("Loading OFF");
            }
        }

        private void FilterOrders(string text)
        {
            IEnumerable<OrdenCompra> filtered;

            if (string.IsNullOrWhiteSpace(text))
            {
                filtered = _allOrders;
            }
            else
            {
                filtered = _allOrders.Where(o =>
                    (o.Numero?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.Supply_Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.DeviceName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.UserName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    o.Fecha.ToString("dd-MM-yyyy HH:mm:ss").Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            Ordenes = new ObservableCollection<OrdenCompra>(filtered);
            //Suscribirse a los cambios de IsSelected de cada orden para mantener el contador.
            foreach (var o in Ordenes)
            {
                o.PropertyChanged -= OnOrderSelectionChanged;
                o.PropertyChanged += OnOrderSelectionChanged;
            }
            SelectedCount = Ordenes.Count(o => o.IsSelected);
            TotalOrders = Ordenes.Count;
        }

        private void OnOrderSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrdenCompra.IsSelected))
            {
                SelectedCount = Ordenes.Count(o => o.IsSelected);
                OnPropertyChanged(nameof(CanReceive));
            }
        }

        [RelayCommand(CanExecute = nameof(CanReceive))]
        public async Task ReceiveSelectedAsync()
        {
            var seleccionadas = Ordenes.Where(o => o.IsSelected).ToList();
            if (seleccionadas.Count == 0) return;

            IsLoading = true;
            loadingMessage = "Recibiendo ordenes...";
            receiveResults = new List<Shared.Dtos.Compras.ProcesarOrdenResult>();

            try
            {
                foreach (var orden in seleccionadas)
                {
                    var res = await Service.ReceiveOrderAsync(orden.Numero);
                    receiveResults.Add(res);

                    if (res.Success)
                    {
                        orden.IsSelected = false;
                    }
                }

                //Refrescar la lista para mostrar los estados actualizados (Procesado, etc.)
                await RecargarListaAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ReceiveSelected: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                loadingMessage = string.Empty;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }
    }
}
