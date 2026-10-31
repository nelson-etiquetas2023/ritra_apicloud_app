using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

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

        private async Task RecargarListaAsync()
        {
            try
            {
                IsLoading = true;
                loadingMessage = "Sincronizando compras...";

                await Service.SincronizarComprasDesdeApiAsync();

                loadingMessage = "Cargando ordenes...";
                Debug.WriteLine("Loading ON");

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
