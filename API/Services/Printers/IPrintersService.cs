using Shared.Dtos;

namespace API.Services.Printers
{
    public interface IPrintersService
    {
        Task<List<Printer>> GetAllAsync();
        Task<Printer?> GetByIdAsync(int id);
        Task<Printer?> CreateAsync(Printer printer);
        Task<Printer?> UpdateAsync(int id, Printer printer);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
        Task<(bool Ok, string? Error)> CalibrateAsync(int id);
    }
}
