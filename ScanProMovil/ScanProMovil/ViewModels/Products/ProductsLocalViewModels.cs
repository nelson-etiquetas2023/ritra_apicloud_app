using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Products;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ScanProMovil.ViewModels.Products
{
    public partial class ProductsLocalViewModels : ObservableObject
    {

        [ObservableProperty]
        private ObservableCollection<Product> productos = [];

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private Product? selectedProduct;

        [ObservableProperty]
        private int totalProducts;

        IProductsService service;

        public ProductsLocalViewModels(IProductsService Service)
        {
            this.service = Service; 
        }

        public async Task GetProductLocal()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await Task.Delay(1000);
                var productfromLocal = await service.GetProductsLocal();
                if (Productos.Count != 0) Productos.Clear();
                Productos = new ObservableCollection<Product>(productfromLocal);
                TotalProducts = Productos.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");

                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    "Ha ocurrido un error inesperado al obtener los datos del dispositivo...",
                    "Aceptar");
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SearchProducts() 
        {
            if(IsBusy) return;
            try
            {
                IsBusy = true;
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                var FilteredProducts = await service.SearchProductsLocal(SearchText);
                Productos.Clear();
                foreach (var item in FilteredProducts)
                {
                    Productos.Add(item);
                }
                TotalProducts = Productos.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    "Ha ocurrido un error inesperado en la busqueda de productos...",
                    "Aceptar");
            }
            finally 
            {
                IsBusy = false;
            }
        }
    }
}
