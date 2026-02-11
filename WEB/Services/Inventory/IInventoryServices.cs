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
        public Task<bool> SaveDataProductScanAsync(List<ScanProducts> products);
        public Task<List<ScanProducts>> GetscanProducts(string OrderId);
        public Task<bool> DeletescanProducts(Guid OrderId);
    }
}
