using Shared.Dtos.Compras;
using Shared.Dtos.Inventario;

namespace API.Services.Inventario
{
    public interface IInventarioService
    {
        Task<ProcesarOrdenResult> ProcesarCompraAsync(string numero, RecepcionCompraRequest? recepcion);
        Task<MovimientosProductoResult> GetMovimientosProductoAsync(string codigo);
    }
}