using Shared.Dtos;

namespace WEB.Services.Config
{
    public interface IConfigService
    {
        Task<List<Parameter>> LoadDataConfig();
        Task<bool> UpdateDocumentSettings(string filter, DocumentSettings setting);
        Task<List<Category>> GetCategoriesAsync();
        Task<Category?> CreateCategoryAsync(Category category);
        Task<Category?> UpdateCategoryAsync(int id, Category category);
        Task<bool> DeleteCategoryAsync(int id);
        Task<List<ProductUnit>> GetProductUnitsAsync();
        Task<ProductUnit?> CreateProductUnitAsync(ProductUnit unit);
        Task<ProductUnit?> UpdateProductUnitAsync(int id, ProductUnit unit);
        Task<bool> DeleteProductUnitAsync(int id);
        Task<List<Warehouse>> GetWarehousesAsync();
        Task<List<Location>> GetLocationsAsync();
        Task<Location?> CreateLocationAsync(Location location);
        Task<Location?> UpdateLocationAsync(int id, Location location);
        Task<bool> DeleteLocationAsync(int id);
    }
}
