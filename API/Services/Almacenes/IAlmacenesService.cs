using Shared.Dtos;

namespace API.Services.Almacenes
{
    public interface IAlmacenesService
    {
        Task<List<Almacen>> GetAllAsync();
        Task<Almacen?> GetByIdAsync(int id);
        Task<Almacen?> CreateAsync(Almacen almacen);
        Task<Almacen?> UpdateAsync(int id, Almacen almacen);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
    }
}
