using Microsoft.AspNetCore.Components.Forms;
using Shared.Dtos.CargasIniciales;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WEB.Services.CargasIniciales
{
    public interface ICargasInicialesService
    {
        Task<List<Inicial>> GetAllAsync();
        Task<Inicial> GetByIdAsync(int id);
        Task<CargaInicialSaveResult> CreateAsync(Inicial inicial);
        Task<CargaInicialSaveResult> UpdateAsync(int id, Inicial inicial);
        Task<bool> DeleteAsync(int id);
        Task<CargaInicialImportResult> ImportAsync(IBrowserFile file);
        Task<byte[]> DownloadTemplateAsync();
        Task<List<Inicial>> GetDocumentsInitialsInventoryAsync();
        Task<string> GetNextNumAsync();
        Task<CargaInicialSaveResult> ProcesarAsync(int id);
    }

    public class CargasInicialesService(IHttpClientFactory httpFactory) : ICargasInicialesService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<Inicial>> GetAllAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/cargasIniciales/get");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<Inicial>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<Inicial> GetByIdAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync($"api/cargasIniciales/getbyid/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Inicial();
            return await JsonSerializer.DeserializeAsync<Inicial>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new Inicial();
        }

        public async Task<CargaInicialSaveResult> CreateAsync(Inicial inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/cargasIniciales/create", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new CargaInicialSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<CargaInicialSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new CargaInicialSaveResult { Success = false, Message = "No se pudo guardar la carga inicial." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new CargaInicialSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<CargaInicialSaveResult> UpdateAsync(int id, Inicial inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/cargasIniciales/update/{id}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new CargaInicialSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<CargaInicialSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new CargaInicialSaveResult { Success = false, Message = "No se pudo actualizar la carga inicial." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new CargaInicialSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.DeleteAsync($"api/cargasIniciales/delete/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<CargaInicialImportResult> ImportAsync(IBrowserFile file)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 30_000_000);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(streamContent, "file", file.Name);

                var response = await clientHttp.PostAsync("api/cargasIniciales/import", content);
                if (!response.IsSuccessStatusCode)
                {
                    return new CargaInicialImportResult
                    {
                        Success = false,
                        Errors = [new RowError { Row = 0, Message = $"Error del servidor: {(int)response.StatusCode}" }]
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return new CargaInicialImportResult();
                return await JsonSerializer.DeserializeAsync<CargaInicialImportResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new CargaInicialImportResult();
            }
            catch (Exception ex)
            {
                return new CargaInicialImportResult
                {
                    Success = false,
                    Errors = [new RowError { Row = 0, Message = ex.Message }]
                };
            }
        }

        public async Task<byte[]> DownloadTemplateAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/cargasIniciales/template");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<List<Inicial>> GetDocumentsInitialsInventoryAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/cargasIniciales/getDocumentsInitialsInventory");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<Inicial>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<string> GetNextNumAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/cargasIniciales/getnextnum");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return "0001";
            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
            return obj != null && obj.TryGetValue("numero", out var numero) ? numero : "0001";
        }

        public async Task<CargaInicialSaveResult> ProcesarAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var response = await clientHttp.PostAsync($"api/cargasIniciales/procesar/{id}", null);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new CargaInicialSaveResult { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                return await JsonSerializer.DeserializeAsync<CargaInicialSaveResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new CargaInicialSaveResult { Success = false, Message = "No se pudo procesar el documento." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return new CargaInicialSaveResult { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }
    }
}