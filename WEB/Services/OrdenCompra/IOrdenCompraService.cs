namespace WEB.Services.OrdenCompra
{
    public interface IOrdenCompraService
    {
        Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync();
        Task<Shared.Dtos.Compras.OrdenCompra?> GetOrderByIdAsync(string numero);
        Task<bool> AddOrderAsync(Shared.Dtos.Compras.OrdenCompra oc);
        Task<bool> UpdateOrderAsync(string numero, Shared.Dtos.Compras.OrdenCompra oc);
        Task<bool> DeleteOrderAsync(string numero);
        Task<int> DeleteAllOrdersAsync(); // Borra TODAS las órdenes de compra (irreversible)
        Task<string> GetNextNumAsync();
        Task<bool> AutorizarParaRecepcionAsync(string numero);
        Task<bool> AnularOrderAsync(string numero);
    }
}
