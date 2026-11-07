using ScanProMovil.Data.Entities;

namespace ScanProMovil.Services.Compras
{
    public interface IComprasService
    {
        Task<bool> SaveOrdersLocalSqliteAsync(OrdenCompra order);
        Task<List<OrdenCompra>> GetOrdersLocalSqliteAsync();
        Task<bool> SincronizarComprasDesdeApiAsync();
        Task<string> GetNextOrderNumberAsync();
        Task<bool> MarkOrderSynchronizedAsync(string numero);
        Task<List<OrdenCompra>> getOrders();
        Task<OrdenCompra> getOrderById(string OrderId);
        Task<bool> SendPurchaseOrder(OrdenCompra order, CancellationToken cancellationToken = default);
        Task<OrdenCompra> UpdateOrder(OrdenCompra order);
        Task<bool> DeleteOrder(string OrderId);
        Task<bool> DeactivateOrder(string Orderid);

        /// <summary>Declara al servidor la recepcion de la orden (cantidades recibidas
        /// por linea desde la base local; el server calcula el delta sobre el inventario).</summary>
        Task<Shared.Dtos.Compras.ProcesarOrdenResult> ReceiveOrderAsync(string numero);

        /// <summary>Un escaneo/suma = una unidad recibida del item en SQLite (con tope).</summary>
        Task<bool> MarcarItemRecibidoAsync(string numeroOrden, int detailId);

        /// <summary>Borra TODAS las órdenes de compra locales de SQLite (irreversible).</summary>
        Task<bool> DeleteAllOrdersLocalAsync();

        /// <summary>Guarda los numeros de orden borrados localmente para no volver a bajarlos al sincronizar.</summary>
        void GuardarNumerosOrdenesBorradas(List<string> numeros);

        /// <summary>Borra TODAS las órdenes de compra del servidor (irreversible).</summary>
        Task<bool> DeleteAllOrdersRemotoAsync();
    }
}
