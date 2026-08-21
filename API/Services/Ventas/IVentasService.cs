using Shared.Dtos.Ventas;

namespace API.Services.Ventas
{
    public interface IVentasService
    {
        Task<List<PedidoVenta>> GetAllAsync();
        Task<PedidoVenta?> GetByIdAsync(int id);
        Task<PedidoVentaSaveResult> CreateAsync(PedidoVenta pedido);
        Task<PedidoVentaSaveResult> UpdateAsync(int id, PedidoVenta pedido);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
        Task<ProcesarPedidoResult> ProcesarPedidoAsync(string numero);
        Task<bool> AnularAsync(int id);
    }
}
