using System.Text.Json;
using Shared.Dtos.Inventario;

namespace WEB.Services.Inventario
{
    public class InventarioService(IHttpClientFactory HttpFactory) : IInventarioService
    {
        private readonly IHttpClientFactory HttpFactory = HttpFactory;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<Shared.Dtos.Compras.ProcesarOrdenResult?> ProcesarOrdenAsync(string numero)
        {
            var url = $"api/inventario/process-compra/{numero}";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.PostAsync(url, null);
                if (response.IsSuccessStatusCode)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(contentString)) return null;
                    return JsonSerializer.Deserialize<Shared.Dtos.Compras.ProcesarOrdenResult>(contentString, jsonOptions);
                }
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al procesar la orden {numero}: {ex.Message}");
                return null;
            }
        }

        public async Task<Shared.Dtos.Inventario.MovimientosProductoResult?> GetMovimientosProductoAsync(string codigo)
        {
            var url = $"api/inventario/movimientos/{Uri.EscapeDataString(codigo)}";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString)) return null;

                return JsonSerializer.Deserialize<Shared.Dtos.Inventario.MovimientosProductoResult>(contentString, jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al obtener movimientos de {codigo}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RegistrarMovimientoInventarioAsync(RegistroMovimientoRequest request)
        {
            var url = $"api/inventario/registrar-movimiento";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(request, jsonOptions);
                var jsonContent = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clienteHttp.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                    return true;

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error registrando movimiento de inventario: HTTP {(int)response.StatusCode} - {errorContent}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error registrando movimiento de inventario: {ex.Message}");
                return false;
            }
        }
    }
}