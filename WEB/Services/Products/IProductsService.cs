using Shared.Dtos;
using Shared.Dtos.Inventario;
using Microsoft.AspNetCore.Components.Forms;

namespace WEB.Services.Products
{
    public interface IProductsService
    {
        Task<List<Product>> GetProductAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<ServiceResponse<Product>> CreateproductAsync(Product product);
        Task<ServiceResponse<Product>> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> AddProductImageAsync(int productId, MultipartFormDataContent content, int imageIndex);
        Task<bool> DeleteProductImageAsync(int imageId);
        Task<byte[]> GetProductImageAsync(int imageId);
        Task<ServiceResponse<Product>> CreateProductWithImagesAsync(CreateProductWithImagesRequest request);
        Task<ServiceResponse<Product>> CreateProductWithFilesAsync(Product product, List<IBrowserFile> files);
        Task<bool> SetProductActiveAsync(int productId, bool active);
        Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData);
        Task<int> BulkCreateProductsAsync(List<Product> products);
        Task<ProductImportResult> ImportProductsFromExcelAsync(IBrowserFile file);
        Task<byte[]> DownloadStockTemplateAsync(bool incluirProductos);
        Task<StockInicialResult> AplicarStockInicialAsync(IBrowserFile file, string usuario);
        Task<List<CargaInicialResumen>> GetCargasInicialesAsync();
        Task<StockInicialResult> RevertirCargaInicialAsync(string lote);
        Task<StockInicialResult> ActualizarCargaInicialAsync(string lote, DateTime fecha, string comentario);
        Task<List<Shared.Dtos.Inventario.AjusteInventario>> GetCargaDetalleAsync(string lote);
        string GetImageUrl(int imageId, int productId);
    }
}
