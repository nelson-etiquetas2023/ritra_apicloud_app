using Shared.Dtos;

namespace API.Services.Vendedores
{
    public interface IVendedoresService
    {
        Task<List<Vendedor>> GetAllAsync();
        Task<Vendedor?> GetByIdAsync(int id);
        Task<Vendedor?> CreateAsync(Vendedor vendedor);
        Task<Vendedor?> UpdateAsync(int id, Vendedor vendedor);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
    }
}
