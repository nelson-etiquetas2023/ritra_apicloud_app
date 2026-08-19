using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanProMovil.Data.Entities;
using ScanProMovil.Services.Products;

namespace ScanProMovil.ViewModels
{
    public partial class ProductDetailsViewModel: ObservableObject
    {
        [ObservableProperty]
        private Product producto;

        private IProductsService productService;

        public ProductDetailsViewModel(Product producto, IProductsService productService)
        {
            this.Producto = producto;
            this.productService = productService;
        }

        [RelayCommand]
        private async Task SaveProductsAsync() 
        {
            await productService.UpdateProducts(Producto.Product_Id, Producto);
        }


    }
}
