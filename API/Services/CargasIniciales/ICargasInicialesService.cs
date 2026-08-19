using Shared.Dtos.CargasIniciales;

namespace API.Services.CargasIniciales
{
    public interface ICargasInicialesService
    {
        Task<List<Inicial>> GetAllAsync();
        Task<Inicial?> GetByIdAsync(int id);
        Task<Inicial> CreateAsync(Inicial inicial);
        Task<Inicial?> UpdateAsync(int id, Inicial inicial);
        Task<bool> DeleteAsync(int id);
        Task<CargaInicialImportResult> ImportFromExcelAsync(Stream excelStream);
        byte[] GenerateTemplate();
        Task<List<Inicial>> GetDocumentsInitialsInventoryAsync();
    }
}