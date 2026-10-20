using Shared.Dtos.Picking;
using Shared.Dtos.Ventas;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Picking
{
    public class PickingService(IHttpClientFactory httpFactory) : IPickingService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public async Task<PickingOrderResult> TransferirAPickingAsync(List<string> numerosPedido, string? usuario)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var request = new TransferirAPickingRequest
                {
                    NumerosPedido = numerosPedido,
                    Usuario = usuario
                };
                var json = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/picking/transfer", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseJson))
                    return new PickingOrderResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<PickingOrderResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions)
                    ?? new PickingOrderResult { Success = false, Message = "No se pudo transferir a picking." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo transferir a picking. Detalle: {ex.Message}");
                return new PickingOrderResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<List<PedidoVenta>> GetPedidosParaPickingAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/picking/pending");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<PedidoVenta>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }
}
