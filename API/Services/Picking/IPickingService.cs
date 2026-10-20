using Shared.Dtos.Picking;
using Shared.Dtos.Ventas;

namespace API.Services.Picking
{
    public interface IPickingService
    {
        Task<PickingOrderResult> TransferirAPickingAsync(List<string> numerosPedido, string? usuario);
        Task<List<PedidoVenta>> GetPedidosParaPickingAsync();
        Task<bool> CompletarPickingAsync(int pickingOrderId);
    }
}
