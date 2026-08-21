namespace WEB.Services.OrdenCompra
{
    public interface IOrdenCompraService
    {
        Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync();
        Task<Shared.Dtos.Compras.OrdenCompra?> GetOrderByIdAsync(string numero);
        Task<bool> AddOrderAsync(Shared.Dtos.Compras.OrdenCompra oc);
        Task<bool> UpdateOrderAsync(string numero, Shared.Dtos.Compras.OrdenCompra oc);
        Task<bool> DeleteOrderAsync(string numero);
        Task<string> GetNextNumAsync();
        Task<bool> AnularOrderAsync(string numero);
    }
}
