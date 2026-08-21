using Shared.Dtos;
using System.Text.Json;

namespace WEB.Services.Vendedores
{
    public interface IVendedoresService
    {
        Task<List<Vendedor>> GetVendedoresAsync();
        Task<Vendedor?> GetVendedorByIdAsync(int id);
        Task<bool> CreateVendedorAsync(Vendedor vendedor);
        Task<bool> UpdateVendedorAsync(int id, Vendedor vendedor);
        Task<bool> DeleteVendedorAsync(int id);
        Task<string> GetNextNumAsync();
    }

    public class VendedoresService(IHttpClientFactory httpFactory) : IVendedoresService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<Vendedor>> GetVendedoresAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/vendedores/get");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<Vendedor>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<Vendedor?> GetVendedorByIdAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync($"api/vendedores/getbyid/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Vendedor();
            return await JsonSerializer.DeserializeAsync<Vendedor>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new Vendedor();
        }

        public async Task<bool> CreateVendedorAsync(Vendedor vendedor)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(vendedor, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/vendedores/create", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo crear el vendedor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateVendedorAsync(int id, Vendedor vendedor)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(vendedor, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/vendedores/update/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo actualizar el vendedor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteVendedorAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var response = await clientHttp.DeleteAsync($"api/vendedores/delete/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo eliminar el vendedor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetNextNumAsync()
        {
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync("api/vendedores/getnextnum");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return "VEN-0001";
            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
            return obj != null && obj.TryGetValue("numero", out var numero) ? numero : "VEN-0001";
        }
    }
}
