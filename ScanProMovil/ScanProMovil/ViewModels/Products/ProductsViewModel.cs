using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Products;
using System.Collections.ObjectModel;

namespace ScanProMovil.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        IProductsService serviceProducts { get; set; }

        [ObservableProperty]
        private int totalProducts = 0;

        [ObservableProperty]
        private DateTime lastUpdated;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private bool isBusy;

        public ObservableCollection<Product> Products { get; set; } = [];
        public ObservableCollection<Product> FilteredProducts { get; set; } = [];

        public ProductsViewModel(IProductsService serviceProducts)
        {
            this.serviceProducts = serviceProducts;
        }

        [RelayCommand]
        private async Task GetProducts() 
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                //busqueda remota. 
                var productfromApi = await serviceProducts.GetProducts();
                if (productfromApi is null) return;
                if (Products.Count != 0) Products.Clear();
                foreach (var producto in productfromApi)
                {
                    Products.Add(producto);
                }
                // almacenamiento local
                var SaveLocalProducts = await serviceProducts.SaveLocalProducts(productfromApi);
                //se actualizan los datos cantidad de producto y ultima Actualizacion
                TotalProducts = Products.Count();
                LastUpdated = DateTime.Now;
                SearchText = "";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void BuscarProductos() 
        {
            if (string.IsNullOrWhiteSpace(SearchText)) 
            {
                GetProductsCommand.Execute(null);
            }    
            else 
            {
                //lista filtrada.
                var filtered = Products.Where(p => p.Product_Name
                .Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

                Products.Clear();

                foreach (var producto in filtered)
                {
                    Products.Add(producto);
                }
                TotalProducts = Products.Count();
                LastUpdated = DateTime.Now;
            }
        }

    }
}
