using Shared.Dtos.Inventarios;

namespace API.Services.Inventarios
{
    public interface IInventarioTeoricoService
    {
        Task<List<InventarioTeorico>> GetAllAsync();
        Task<InventarioTeorico?> GetByIdAsync(int id);
        Task<InventarioTeoricoSaveResult> CreateAsync(InventarioTeorico inicial);
        Task<InventarioTeoricoSaveResult> UpdateAsync(int id, InventarioTeorico inicial);
        Task<bool> DeleteAsync(int id);
        Task<InventarioTeoricoImportResult> ImportFromExcelAsync(Stream excelStream);
        byte[] GenerateTemplate();
        Task<List<InventarioTeorico>> GetDocumentsInitialsInventoryAsync();
        Task<string> GetNextNumAsync();
    }
}
