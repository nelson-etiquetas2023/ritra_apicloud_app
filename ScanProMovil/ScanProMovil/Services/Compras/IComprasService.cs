using ScanProMovil.Data.Entities;

namespace ScanProMovil.Services.Compras
{
    public interface IComprasService
    {
        Task<bool> SaveOrdersLocalSqliteAsync(OrdenCompra order);
        Task<List<OrdenCompra>> GetOrdersLocalSqliteAsync();
        Task<string> GetNextOrderNumberAsync();
        Task<bool> MarkOrderSynchronizedAsync(string numero);
        Task<List<OrdenCompra>> getOrders();
        Task<OrdenCompra> getOrderById(string OrderId);
        Task<bool> SendPurchaseOrder(OrdenCompra order, CancellationToken cancellationToken = default);
        Task<OrdenCompra> UpdateOrder(OrdenCompra order);
        Task<bool> DeleteOrder(string OrderId);
        Task<bool> DeactivateOrder(string Orderid);
    }
}
