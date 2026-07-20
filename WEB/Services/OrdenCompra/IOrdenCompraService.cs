namespace WEB.Services.OrdenCompra
{
    public interface IOrdenCompraService
    {
        Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync();
    }
}
