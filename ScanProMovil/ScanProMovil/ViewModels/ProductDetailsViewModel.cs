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

        [ObservableProperty]
        private bool isSaving;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        private IProductsService productService;

        public ProductDetailsViewModel(Product producto, IProductsService productService)
        {
            this.Producto = producto;
            this.productService = productService;
        }

        [RelayCommand]
        private async Task SaveProductsAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;
                StatusMessage = "Guardando...";

                var localOk = await productService.UpdateProductLocal(Producto);
                var apiOk = await productService.UpdateProducts(Producto.Product_Id, Producto);

                if (localOk)
                {
                    StatusMessage = "Producto guardado en el dispositivo.";
                    await Toast.Make("Producto guardado en el dispositivo.").Show();
                }
                else
                {
                    StatusMessage = "No se pudo guardar el producto en el dispositivo.";
                    await Toast.Make("No se pudo guardar el producto en el dispositivo.").Show();
                }

                if (!apiOk)
                {
                    StatusMessage = "Producto guardado localmente, pendiente de sincronizar.";
                    await Toast.Make("Guardado local. La sincronización con el servidor falló.").Show();
                }
            }
            catch (Exception)
            {
                var localOk = await productService.UpdateProductLocal(Producto);
                if (localOk)
                {
                    StatusMessage = "Producto guardado en el dispositivo.";
                    await Toast.Make("Guardado local. Sin conexión con el servidor.").Show();
                }
                else
                {
                    StatusMessage = "Error al guardar el producto.";
                    await Toast.Make("Error al guardar el producto.").Show();
                }
            }
            finally
            {
                IsSaving = false;
            }
        }


    }
}
