using ScanProMovil.Data.Entities;

namespace ScanProMovil.Services.Picking
{
    public interface IPickingService
    {
        Task<List<PickingOrder>> GetPedidosParaPickingRemotoAsync();
        Task<List<PickingOrder>> GetPickingOrdersLocalAsync();
        Task<bool> SincronizarPickingDesdeApiAsync();
        Task<bool> MarcarCompletadoAsync(int pickingId);
        Task<bool> MarcarItemCompletadoAsync(int pickingId, int detailId);
        Task<bool> CompletarOrdenRemotoAsync(int pickingId);
    }
}
