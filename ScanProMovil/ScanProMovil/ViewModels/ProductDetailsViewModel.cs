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

            var codeBar = Producto.CodeBar?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(codeBar))
            {
                StatusMessage = "El código de barra no puede estar vacío.";
                await Toast.Make("Ingresa o escanea un código de barra.").Show();
                return;
            }
            // Limpia * si viene del escáner
            codeBar = codeBar.Trim('*').Trim();
            Producto.CodeBar = codeBar;

            // Validación duplicado local (excepto el propio producto)
            var duplicado = await productService.GetProductLocalById(codeBar);
            if (duplicado is not null && duplicado.Product_Id != Producto.Product_Id)
            {
                StatusMessage = $"El código '{codeBar}' ya está asignado a '{duplicado.Product_Name}' ({duplicado.product_code}).";
                await Toast.Make($"Código duplicado: ya existe en {duplicado.product_code}").Show();
                return;
            }

            try
            {
                IsSaving = true;
                StatusMessage = "Guardando...";

                var localOk = await productService.UpdateProductLocal(Producto);
                if (localOk)
                {
                    StatusMessage = "Código guardado en el dispositivo.";
                    await Toast.Make("Código de barra guardado localmente.").Show();
                }
                else
                {
                    StatusMessage = "No se pudo guardar en el dispositivo.";
                    await Toast.Make("No se pudo guardar en el dispositivo.").Show();
                    return;
                }

                // Intento de sincronizar con API (no bloquea éxito local) - muestra motivo si falla
                var (apiOk, apiError) = await productService.UpdateProducts(Producto.Product_Id, Producto);

                if (!apiOk)
                {
                    StatusMessage = string.IsNullOrWhiteSpace(apiError)
                        ? "Guardado local. Pendiente sincronizar con web."
                        : $"Guardado local. WEB falló: {apiError}";
                    await Toast.Make(StatusMessage).Show();
                    System.Diagnostics.Debug.WriteLine($"Update WEB falló: {apiError}");
                }
                else
                {
                    StatusMessage = "Sincronizado con la web correctamente.";
                    await Toast.Make("Sincronizado con la web.").Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SaveProducts: {ex.Message}");
                // Fallback: intenta solo local
                var localOk = false;
                try { localOk = await productService.UpdateProductLocal(Producto); } catch { }
                if (localOk)
                {
                    StatusMessage = "Guardado local. Sin conexión con el servidor.";
                    await Toast.Make("Guardado local. Sin conexión.").Show();
                }
                else
                {
                    StatusMessage = "Error al guardar.";
                    await Toast.Make("Error al guardar.").Show();
                }
            }
            finally
            {
                IsSaving = false;
            }
        }


    }
}
