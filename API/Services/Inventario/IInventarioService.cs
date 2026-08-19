using Shared.Dtos.Compras;

namespace API.Services.Inventario
{
    public interface IInventarioService
    {
        Task<ProcesarOrdenResult> ProcesarCompraAsync(string numero);
    }
}