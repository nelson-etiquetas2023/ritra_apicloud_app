using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Compras;
using ScanProMovil.Services.Products;
using ScanProMovil.Services.Session;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ScanProMovil.ViewModels
{
    public partial class AddComprasViewModels : ObservableObject
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
        public ObservableCollection<OrdenCompra> FilteredOrders { get; } = new();

        [ObservableProperty]
        private int totalOrders;

        [ObservableProperty]
        private OrdenCompra newOrder = new();

        [ObservableProperty]
        private OrdenCompra? selectedOrder;

        [ObservableProperty]
        private Product? productScan;

        IComprasService ComprasService { get; set; }
        IProductsService ProductService { get; set; }
        private readonly AppSession _session;

        public AddComprasViewModels(IComprasService comprasService, 
            IProductsService productService, AppSession session)
        {
            ComprasService = comprasService;
            ProductService = productService;
            _session = session;
            newOrder.Fecha = DateTime.Now;
            newOrder.FechaCreacion = DateTime.Now;
        }

        [RelayCommand]
        public async Task GetProductLocalById()
        {
            //busqueda por codigo de barras.
            ProductScan = await ProductService.GetProductLocalById(searchText);
        }

        public async Task LoadNextOrderNumberAsync()
        {
            try
            {
                var numero = await ComprasService.GetNextOrderNumberAsync();
                var current = NewOrder;
                NewOrder = new OrdenCompra
                {
                    Numero = numero,
                    Fecha = current.Fecha,
                    FechaCreacion = current.FechaCreacion == default ? DateTime.Now : current.FechaCreacion,
                    Description = current.Description,
                    Tipo_Documento = current.Tipo_Documento,
                    Supply_Name = current.Supply_Name,
                    Items = current.Items
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al generar el consecutivo de la orden: " + ex.Message);
            }
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

                var result = await ComprasService.GetOrdersLocalSqliteAsync();
                _allOrders = result;

                Ordenes = new
                    ObservableCollection<OrdenCompra>(_allOrders);

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
        public async Task<bool> SaveOrderLocalSqliteAsync()
        {
            try
            {
                NewOrder.Fecha = DateTime.Now;
                if (NewOrder.FechaCreacion == default)
                    NewOrder.FechaCreacion = DateTime.Now;
                CalcularTotales();
                FillSessionTrail(NewOrder);
                IsLoading = true;

                return await ComprasService.SaveOrdersLocalSqliteAsync(NewOrder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error al guardar las ordenes: " + ex.Message);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FillSessionTrail(OrdenCompra order)
        {
            _session.InitializeDevice();
            order.UserName = _session.UserDisplayName;
            order.UserEmail = _session.UserEmail ?? "";
            order.UserRole = _session.UserRole ?? "";
            order.DeviceCode = _session.DeviceCode;
            order.DeviceName = _session.DeviceDisplayName;
            order.WarehouseName = _session.WarehouseName;
        }

        public void CalcularTotales()
        {
            NewOrder.ItemsNumber = NewOrder.Items.Count;
            NewOrder.Subtotal = NewOrder.Items.Sum(x => x.Cantidad * x.Costo);
            NewOrder.Impuesto = 0;
            NewOrder.Total = NewOrder.Subtotal + NewOrder.Impuesto;
        }

        [RelayCommand]
        public async Task DeleteOrderLocalSqliteAsync()
        {
            if (SelectedOrder!.Numero == null)
                return;

            //bool deleted = await services.DeleteOrderLocalSqliteAsync(SelectedOrder.OrderNumber);

            //if (deleted)
            //{
            //    var toast = Toast.Make("Orden eliminada correctamente...", ToastDuration.Short);
            //    await toast.Show();
            //}
        }

        private void FilterOrders(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                Ordenes = new ObservableCollection<OrdenCompra>(_allOrders);
                TotalOrders = Ordenes.Count;
                return;
            }

            var filtered = _allOrders.Where(o => o.Numero.Contains(searchText,
                StringComparison.OrdinalIgnoreCase)).ToList();

            Ordenes = new ObservableCollection<OrdenCompra>(filtered);

            TotalOrders = Ordenes.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterOrders(value);
        }

    }
}
