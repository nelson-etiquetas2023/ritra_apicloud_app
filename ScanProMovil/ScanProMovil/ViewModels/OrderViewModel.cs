using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Orders;
using ScanProMovil.Services.Session;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ScanProMovil.ViewModels
{
    public partial class OrderViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool refreshListOrders = true;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        public bool isLoading;

        [ObservableProperty]
        private string loadingMessage = string.Empty;

        private List<Order> _allOrders = new();

        [ObservableProperty]
        private ObservableCollection<Order> ordenes = [];

        [ObservableProperty]
        public ObservableCollection<Order> FilteredOrders { get; } = new();

        [ObservableProperty]
        private int totalOrders;

        [ObservableProperty]
        private Order newOrder = new();

        [ObservableProperty]
        private Order? selectedOrder;

        IOrderServices services { get; set; }
        private readonly AppSession _session;

        public OrderViewModel(IOrderServices Service, AppSession session)
        {
            services = Service;
            _session = session;
            newOrder.OrderDate = DateTime.Now;
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

                await Task.Delay(1000);

                var result = await services.GetOrdersLocalSqliteAsync();
                _allOrders = result;

                Ordenes = new 
                    ObservableCollection<Order>(_allOrders);

                TotalOrders = _allOrders.Count;

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

        [RelayCommand]
        public async void SaveOrderLocalSqliteAsync() 
        {
            try
            {
                FillSessionTrail(NewOrder);
                IsLoading = true;
                await Task.Delay(1000);
                await services.SaveOrdersLocalSqliteAsync(NewOrder);
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al guardar las ordenes: " + ex.Message);
            }
            finally 
            {
                IsLoading = false;  

            }
        }

        private void FillSessionTrail(Order order)
        {
            _session.InitializeDevice();
            order.UserName = _session.UserDisplayName;
            order.UserEmail = _session.UserEmail ?? "";
            order.UserRole = _session.UserRole ?? "";
            order.DeviceCode = _session.DeviceCode;
            order.DeviceName = _session.DeviceDisplayName;
            order.WarehouseName = _session.WarehouseName;
        }

        [RelayCommand]
        public async Task DeleteOrderLocalSqliteAsync()
        {
            if (SelectedOrder!.OrderNumber == null)
                return;

            bool deleted = await services.DeleteOrderLocalSqliteAsync(SelectedOrder.OrderNumber);

            if (deleted) 
            {
                var toast = Toast.Make("Orden eliminada correctamente...",ToastDuration.Short);
                await toast.Show();
            }
        }

        private void FilterOrders(string searchText) 
        {
            if (string.IsNullOrEmpty(searchText)) 
            {
                Ordenes = new ObservableCollection<Order>(_allOrders);
                TotalOrders = Ordenes.Count;
                return;
            }

            var filtered = _allOrders.Where(o => o.OrderNumber.Contains(searchText,
                StringComparison.OrdinalIgnoreCase)).ToList();

            Ordenes = new ObservableCollection<Order>(filtered);

            TotalOrders = Ordenes.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }

    }
}
