using Shared.Dtos;

namespace API.Services.Inventory
{
    public interface IInventoryService
    {
        Task<IEnumerable<OrderFisicoHeader>> GetOrdersAsync();
        Task<OrderFisicoHeader?> GetOrderByIdAsync(string OrderNumber);
        Task<OrderFisicoHeader> CreateOrderAsync(OrderFisicoHeader order);
        Task<OrderFisicoHeader?> UpdateOrderAsync(string OrderNumber, OrderFisicoHeader order);
        Task<bool> DeleteOrderAsync(string id);
        Task<bool> SaveDataProductScanAsync(List<ScanProducts> products);
        public Task<List<ScanProducts>> GetScanProductsAsync(string OrderId);
        public Task<bool> DeleteScanProductsAsync(Guid OrderId);
    }
}
