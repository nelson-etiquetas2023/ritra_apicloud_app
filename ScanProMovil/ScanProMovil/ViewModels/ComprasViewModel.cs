using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using System.Collections.ObjectModel;
using System.Diagnostics;

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

            try
            {
                IsLoading = true;
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
            TotalOrders = Ordenes.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }
    }
}
