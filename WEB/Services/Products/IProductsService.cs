using Shared.Dtos;
using Microsoft.AspNetCore.Components.Forms;

namespace WEB.Services.Products
{
    public interface IProductsService
    {
        Task<List<Product>> GetProductAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<bool> CreateproductAsync(Product product);
        Task<bool> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> AddProductImageAsync(int productId, MultipartFormDataContent content, int imageIndex);
        Task<bool> DeleteProductImageAsync(int imageId);
        Task<byte[]> GetProductImageAsync(int imageId);
        Task<Product?> CreateProductWithImagesAsync(CreateProductWithImagesRequest request);
        Task<Product?> CreateProductWithFilesAsync(Product product, List<IBrowserFile> files);
        Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData);
        Task<int> BulkCreateProductsAsync(List<Product> products);
        Task<ProductImportResult> ImportProductsFromExcelAsync(IBrowserFile file);
        string GetImageUrl(int imageId, int productId);
    }
}
