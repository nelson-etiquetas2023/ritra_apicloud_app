using System.Text;
using System.Text.Json;

namespace WEB.Services.OrdenCompra
{                   
    public class OrdenCompraService(IHttpClientFactory HttpFactory) : IOrdenCompraService
    {
        private readonly IHttpClientFactory HttpFactory = HttpFactory;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public async Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync()
        {
            var url = $"api/ordencompra/getorders";
            var clienteHttp = HttpFactory.CreateClient("ritrama");

            try
            {
                var response = await clienteHttp.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString))
                    return [];

                var orders = JsonSerializer.Deserialize
                    <List<Shared.Dtos.Compras.OrdenCompra>>(contentString, jsonOptions);

                return orders ?? [];

            }
            catch (HttpRequestException ex)
            {
                // Aquí puedes registrar el error o mostrar mensaje al usuario
                Console.WriteLine($"Error al obtener órdenes: {ex.Message}");
                return [];
            }
        }

        public async Task<Shared.Dtos.Compras.OrdenCompra?> GetOrderByIdAsync(string numero)
        {
            var url = $"api/ordencompra/getorderbyid/{numero}";
            var clienteHttp = HttpFactory.CreateClient("ritrama");

            try
            {
                var response = await clienteHttp.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString)) return null;

                return JsonSerializer.Deserialize
                    <Shared.Dtos.Compras.OrdenCompra>(contentString, jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al obtener la orden {numero}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AddOrderAsync(Shared.Dtos.Compras.OrdenCompra oc)
        {
            var url = $"api/ordencompra/addorder";
            var clienteHttp = HttpFactory.CreateClient("ritrama");

            try
            {
                var json = JsonSerializer.Serialize(oc, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clienteHttp.PostAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al crear la orden: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderAsync(string numero, Shared.Dtos.Compras.OrdenCompra oc)
        {
            var url = $"api/ordencompra/updateorder/{numero}";
            var clienteHttp = HttpFactory.CreateClient("ritrama");

            try
            {
                var json = JsonSerializer.Serialize(oc, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clienteHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al actualizar la orden {numero}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(string numero)
        {
            var url = $"api/ordencompra/deleteorder/{numero}";
            var clienteHttp = HttpFactory.CreateClient("ritrama");

            try
            {
                var response = await clienteHttp.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al eliminar la orden {numero}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetNextNumAsync()
        {
            var clienteHttp = HttpFactory.CreateClient("ritrama");
            var response = await clienteHttp.GetAsync("api/ordencompra/getnextnum");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return "OC-0001";
            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
            return obj != null && obj.TryGetValue("numero", out var numero) ? numero : "OC-0001";
        }

        public async Task<bool> AnularOrderAsync(string numero)
        {
            try
            {
                var clienteHttp = HttpFactory.CreateClient("ritrama");
                var response = await clienteHttp.PostAsync($"api/ordencompra/anular/{numero}", null);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al anular la orden {numero}: {ex.Message}");
                return false;
            }
        }
    }
}               
