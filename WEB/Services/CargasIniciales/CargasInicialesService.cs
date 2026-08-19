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
        Task<bool> CreateAsync(Inicial inicial);
        Task<bool> UpdateAsync(int id, Inicial inicial);
        Task<bool> DeleteAsync(int id);
        Task<CargaInicialImportResult> ImportAsync(IBrowserFile file);
        Task<byte[]> DownloadTemplateAsync();
        Task<List<Inicial>> GetDocumentsInitialsInventoryAsync();
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

        public async Task<bool> CreateAsync(Inicial inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/cargasIniciales/create", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(int id, Inicial inicial)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(inicial, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/cargasIniciales/update/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return false;
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
    }
}