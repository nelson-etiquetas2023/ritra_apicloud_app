using Shared.Dtos;
using System.Text;
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


        public async Task<bool> DeletescanProducts(Guid id)
        {
            var url = $"api/orderfisico/deletescanProducts/{id}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        public async Task<List<ScanProducts>> GetscanProducts(string OrderId) 
        {
            var url = $"api/orderfisico/getscanproducts/{OrderId}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new List<ScanProducts>();
            var scanProducts = await JsonSerializer.DeserializeAsync<List<ScanProducts>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (scanProducts ?? new List<ScanProducts>());
        }
        public async Task<bool> SaveDataProductScanAsync(List<ScanProducts> products)
        {
            var json = JsonSerializer.Serialize(products, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var url = $"api/orderfisico/savedatascanproducts";
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
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

        public async Task<bool> CreateOrders(OrderFisicoHeader order)
        {
            //algunos valores por defecto de la orden.
            order.Order_Hour = DateTime.Now.ToString("HH:mm:ss");
            order.Notes = string.Empty;
            var equip = new Equipo() { Id = new Guid(), OrderNumber=order.OrderNumberID,DateCreated=DateTime.Now };
            order.Equipo = equip;
            order.Status = "OPEN";
            order.Status_Name = "DOCUMENTO DE INVENTARIO";
            //primero hay que serializar la orden a json.
            var json = JsonSerializer.Serialize(order, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var url = $"api/orderfisico/createorder";
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else 
            {
                return false;
            }
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
