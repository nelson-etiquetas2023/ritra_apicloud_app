using Shared.Dtos;
using System.Text.Json;

namespace WEB.Services.Inventory
{
    public class InventoryServices : IInventoryServices
    {
        IHttpClientFactory httpFactory { get; set; }

        private static readonly JsonSerializerOptions jsonOptions = 
            new JsonSerializerOptions() { 
                PropertyNameCaseInsensitive = true, 
                WriteIndented = true };
        
        public InventoryServices(IHttpClientFactory httpFactory)
        {
            this.httpFactory = httpFactory;
        }

        public async Task<List<OrderFisicoHeader>> GetOrders()
        {
            var url = $"api/orderfisico/getorders";
            var clientHttp = httpFactory.CreateClient("ritrama");

            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
                return new List<OrderFisicoHeader>();

            var orders = await JsonSerializer.DeserializeAsync<List<OrderFisicoHeader>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);

            return (orders ?? new List<OrderFisicoHeader>());
        }

        public Task<bool> CreateOrders(OrderFisicoHeader order)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteOrders(string OrderNumber)
        {
            throw new NotImplementedException();
        }

        public Task<OrderFisicoHeader> GetOrderById(string OrderNumber)
        {
            throw new NotImplementedException();
        }

       

        public Task<bool> UpdateOrders(string OrderNumber, OrderFisicoHeader order)
        {
            throw new NotImplementedException();
        }
    }
}
