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
    }
}               
