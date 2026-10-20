using Shared.Dtos.Inventarios;

namespace API.Services.Inventarios
{
    public interface IInventarioFisicoService
    {
        Task<List<InventarioFisico>> GetAllAsync();
        Task<InventarioFisico?> GetByIdAsync(int id);
        Task<FiscalSaveResult> CreateAsync(InventarioFisico documento);
        Task<FiscalSaveResult> UpdateAsync(int id, InventarioFisico documento);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();

        /// <summary>Compara un conteo físico contra un documento teórico. No escribe en la base de datos.</summary>
        Task<ComparacionResult> CompararAsync(string documentoTeorico, string documentoFisico);

        /// <summary>
        /// Aplica el conteo físico al inventario de productos. Si documentoTeorico viene vacío,
        /// solo se actualizan los productos escaneados; si viene informado, los productos del
        /// teórico que no se escanearon quedan en cero.
        /// </summary>
        Task<AplicarFisicoResult> AplicarAsync(int id, string? documentoTeorico);
    }
}
