namespace WEB.Services.OrdenCompra
{
    public interface IOrdenCompraService
    {
        Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync();
        Task<bool> UpdateOrderAsync(string numero, Shared.Dtos.Compras.OrdenCompra oc);
    }
}
