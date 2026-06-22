using Shared.Dtos.AppMovil;

namespace API.Services.AppMovil
{
    public interface IAppMovilService
    {
        Task<List<OrderPurchase>> GetOrdersPurchaseAsync();
        Task<OrderPurchase?> GetOrderByIdAsync(string orderid);
        Task<OrderPurchase> CreateOrderAsync(OrderPurchase order);
        Task<OrderPurchase?> UpdateOrderAsync(OrderPurchase order);
        Task<OrderPurchase?> DeleteOrderAsync(string orderid);
        Task<bool> ProcesarOrden(string orderid);
    }
}
