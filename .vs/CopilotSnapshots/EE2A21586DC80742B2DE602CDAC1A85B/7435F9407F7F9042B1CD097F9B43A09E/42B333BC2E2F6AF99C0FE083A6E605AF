using Shared.Dtos;

namespace API.Services.Products
{
    public interface IProductsService
    {
        Task<List<Product>> GetProductAsync();
        Task<Product?> GetProductByIdAsync(int productId);
        Task<Product> CreateproductAsync(Product producto);
        Task<Product?> UpdateProductAsync(int productId, Product producto);
        Task<bool> DeleteProductAsync(int productId);
    }
}
