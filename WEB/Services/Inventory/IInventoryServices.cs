using Shared.Dtos;

namespace WEB.Services.Inventory
{
    public interface IInventoryServices
    {
        public Task<List<OrderFisicoHeader>> GetOrders();
        public Task<OrderFisicoHeader> GetOrderById(string OrderNumber);
        public Task<bool> CreateOrders(OrderFisicoHeader order);
        public Task<bool> UpdateOrders(string OrderNumber, OrderFisicoHeader order);
        public Task<bool> DeleteOrders(string OrderNumber);

    }
}
