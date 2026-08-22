using Shared.Dtos.Ventas;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Ventas
{
    public interface IVentasService
    {
        Task<List<PedidoVenta>> GetAllAsync();
        Task<PedidoVenta> GetByIdAsync(int id);
        Task<PedidoVentaSaveResult> CreateAsync(PedidoVenta pedido);
        Task<PedidoVentaSaveResult> UpdateAsync(int id, PedidoVenta pedido);
        Task<bool> DeleteAsync(int id);
        Task<string> GetNextNumAsync();
        Task<ProcesarPedidoResult?> ProcesarPedidoAsync(string numero);
        Task<bool> AnularAsync(int id);
    }

    public class VentasService(IHttpClientFactory httpFactory) : IVentasService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<PedidoVenta>> GetAllAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/ventas/get");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<PedidoVenta>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<PedidoVenta> GetByIdAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync($"api/ventas/getbyid/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new PedidoVenta();
            return await JsonSerializer.DeserializeAsync<PedidoVenta>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new PedidoVenta();
        }

        public async Task<PedidoVentaSaveResult> CreateAsync(PedidoVenta pedido)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(pedido, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/ventas/create", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new PedidoVentaSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<PedidoVentaSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new PedidoVentaSaveResult { Success = false, Message = "No se pudo guardar el pedido." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new PedidoVentaSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<PedidoVentaSaveResult> UpdateAsync(int id, PedidoVenta pedido)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(pedido, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/ventas/update/{id}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new PedidoVentaSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<PedidoVentaSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new PedidoVentaSaveResult { Success = false, Message = "No se pudo actualizar el pedido." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new PedidoVentaSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.DeleteAsync($"api/ventas/delete/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetNextNumAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/ventas/getnextnum");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return string.Empty;
                var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
                return obj != null && obj.TryGetValue("numero", out var numero) ? numero : string.Empty;
            }
            catch
            {
                // Si el endpoint no esta disponible, el API genera el numero real al guardar.
                return string.Empty;
            }
        }

        public async Task<ProcesarPedidoResult?> ProcesarPedidoAsync(string numero)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/ventas/process/{numero}", null);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson)) return null;
                return await JsonSerializer.DeserializeAsync<ProcesarPedidoResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo procesar el pedido. Detalle: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AnularAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/ventas/anular/{id}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo anular el pedido. Detalle: {ex.Message}");
                return false;
            }
        }
    }
}
