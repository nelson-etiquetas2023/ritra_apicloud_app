using Microsoft.AspNetCore.Components.Forms;
using Shared.Dtos.Inventarios;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Inventarios
{
    public interface IInventarioTeoricoService
    {
        Task<List<InventarioTeorico>> GetAllAsync();
        Task<InventarioTeorico> GetByIdAsync(int id);
        Task<List<InventarioTeorico>> GetFisicosAsync();
        Task<List<InventarioFisico>> GetInventarioFisicoAsync();
        Task<InventarioTeoricoSaveResult> CreateAsync(InventarioTeorico inicial);
        Task<InventarioTeoricoSaveResult> UpdateAsync(int id, InventarioTeorico inicial);
        Task<bool> DeleteAsync(int id);
        Task<InventarioTeoricoImportResult> ImportAsync(IBrowserFile file);
        Task<byte[]> DownloadTemplateAsync();
        Task<List<InventarioTeorico>> GetDocumentsInitialsInventoryAsync();
        Task<string> GetNextNumAsync();
        Task<ComparacionResult> CompararAsync(string documentoTeorico, string documentoFisico);
        Task<AplicarFisicoResult> AplicarFisicoAsync(int id, string? documentoTeorico);
    }

    public class InventarioTeoricoService(IHttpClientFactory httpFactory) : IInventarioTeoricoService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<InventarioTeorico>> GetAllAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/InventarioTeorico/get");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<InventarioTeorico>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<InventarioTeorico> GetByIdAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync($"api/InventarioTeorico/getbyid/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new InventarioTeorico();
            return await JsonSerializer.DeserializeAsync<InventarioTeorico>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new InventarioTeorico();
        }

        public async Task<List<InventarioTeorico>> GetFisicosAsync()
        {
            try
            {
                // Obtener documentos iniciales agrupados desde la app móvil
                // Este endpoint agrupa los ScanProducts por OrdenId
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/InventarioTeorico/getDocumentsInitialsInventory");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];

                return await JsonSerializer.DeserializeAsync<List<InventarioTeorico>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] GetFisicosAsync: {ex.Message}");
                return [];
            }
        }

        public async Task<List<InventarioFisico>> GetInventarioFisicoAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/InventarioFisico/get");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<InventarioFisico>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] GetInventarioFisicoAsync: {ex.Message}");
                return [];
            }
        }

        public async Task<InventarioTeoricoSaveResult> CreateAsync(InventarioTeorico inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/InventarioTeorico/create", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new InventarioTeoricoSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<InventarioTeoricoSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new InventarioTeoricoSaveResult { Success = false, Message = "No se pudo guardar la carga inicial." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new InventarioTeoricoSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<InventarioTeoricoSaveResult> UpdateAsync(int id, InventarioTeorico inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/InventarioTeorico/update/{id}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new InventarioTeoricoSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<InventarioTeoricoSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new InventarioTeoricoSaveResult { Success = false, Message = "No se pudo actualizar la carga inicial." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new InventarioTeoricoSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.DeleteAsync($"api/InventarioTeorico/delete/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<InventarioTeoricoImportResult> ImportAsync(IBrowserFile file)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 30_000_000);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(streamContent, "file", file.Name);

                var response = await clientHttp.PostAsync("api/InventarioTeorico/import", content);
                if (!response.IsSuccessStatusCode)
                {
                    return new InventarioTeoricoImportResult
                    {
                        Success = false,
                        Errors = [new RowError { Row = 0, Message = $"Error del servidor: {(int)response.StatusCode}" }]
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new InventarioTeoricoImportResult();
                return await JsonSerializer.DeserializeAsync<InventarioTeoricoImportResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new InventarioTeoricoImportResult();
            }
            catch (Exception ex)
            {
                return new InventarioTeoricoImportResult
                {
                    Success = false,
                    Errors = [new RowError { Row = 0, Message = ex.Message }]
                };
            }
        }

        public async Task<byte[]> DownloadTemplateAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/InventarioTeorico/template");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<List<InventarioTeorico>> GetDocumentsInitialsInventoryAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/InventarioTeorico/getDocumentsInitialsInventory");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<InventarioTeorico>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<string> GetNextNumAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/InventarioTeorico/getnextnum");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return "SIT-0001";
            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
            return obj != null && obj.TryGetValue("numero", out var numero) ? numero : "SIT-0001";
        }

        public async Task<ComparacionResult> CompararAsync(string documentoTeorico, string documentoFisico)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var json = JsonSerializer.Serialize(
                new ComparacionRequest { DocumentoTeorico = documentoTeorico, InventarioFisico = documentoFisico },
                jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await clientHttp.PostAsync("api/InventarioFisico/comparar", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var mensaje = ExtraerMensaje(body, $"No se pudo comparar. Código HTTP {(int)response.StatusCode}.");
                throw new InvalidOperationException(mensaje);
            }

            return string.IsNullOrWhiteSpace(body)
                ? new ComparacionResult()
                : JsonSerializer.Deserialize<ComparacionResult>(body, jsonOptions) ?? new ComparacionResult();
        }

        public async Task<AplicarFisicoResult> AplicarFisicoAsync(int id, string? documentoTeorico)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var json = JsonSerializer.Serialize(
                new AplicarFisicoRequest { DocumentoTeorico = documentoTeorico ?? "" },
                jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await clientHttp.PostAsync($"api/InventarioFisico/aplicar/{id}", content);
            var body = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(body))
                return new AplicarFisicoResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

            var resultado = JsonSerializer.Deserialize<AplicarFisicoResult>(body, jsonOptions);
            if (resultado is not null) return resultado;

            return new AplicarFisicoResult
            {
                Success = false,
                Message = ExtraerMensaje(body, $"No se pudo aplicar el conteo. Código HTTP {(int)response.StatusCode}.")
            };
        }

        private static string ExtraerMensaje(string body, string porDefecto)
        {
            if (string.IsNullOrWhiteSpace(body)) return porDefecto;

            try
            {
                var envelope = JsonSerializer.Deserialize<JsonElement>(body);
                if (envelope.ValueKind == JsonValueKind.Object &&
                    envelope.TryGetProperty("message", out var mensaje) &&
                    mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString() ?? porDefecto;
                }
            }
            catch (JsonException)
            {
            }

            var limpio = body.Trim().Trim('"');
            return string.IsNullOrWhiteSpace(limpio) ? porDefecto : limpio;
        }
    }
}