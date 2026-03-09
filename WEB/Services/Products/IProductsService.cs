using Shared.Dtos;

namespace WEB.Services.Products
{
    public interface IProductsService
    {
        Task<List<Product>> GetProductAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<bool> CreateproductAsync(Product product);
        Task<Product> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);
    }
}
