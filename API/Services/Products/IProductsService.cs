using Shared.Dtos;
using Microsoft.AspNetCore.Http;

namespace API.Services.Products
{
    public interface IProductsService
    {
        Task<List<Product>> GetProductAsync();
        Task<Product?> GetProductByIdAsync(int productId);
        Task<Product> CreateproductAsync(Product producto);
        Task<Product?> UpdateProductAsync(int productId, Product producto);
        Task<bool> DeleteProductAsync(int productId);
        Task<bool> AddProductImageAsync(int productId, IFormFile file, int imageIndex);
        Task<bool> DeleteProductImageAsync(int imageId);
        Task<Product?> CreateProductWithImagesAsync(CreateProductWithImagesRequest request);
        Task<Product?> CreateProductWithFilesAsync(Product product, IFormFileCollection files);
        Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData);
        Task<int> BulkCreateProductsAsync(List<Product> products);
        Task<ProductImportResult> ImportFromExcelAsync(Stream excelStream);
    }
}
