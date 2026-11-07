using Shared.Dtos.Compras;

namespace API.Services.OcMovil
{
    public interface IOcMovilService
    {
        Task<IEnumerable<OrdenCompra>> GetOrdersAsync();
        Task<OrdenCompra> GetOrderByIdAsync(string numero);
        Task<OrdenCompra?> AddOrderAsync(OrdenCompra oc);
        Task<OrdenCompra?> UpdateOrderAsync(string Numero,  OrdenCompra oc);
        Task<bool> DeleteOrderAsync(string numero);
        Task<int> DeleteAllOrdersAsync();
        Task<bool> AutorizarParaRecepcionAsync(string numero); // Borra TODAS las órdenes de compra (irreversible)
        Task<string> GetNextNumAsync();
        Task<bool> AnularOrderAsync(string numero);
    }
}
