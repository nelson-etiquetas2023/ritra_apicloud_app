using Shared.Dtos;
using System.Text.Json;

namespace WEB.Services.Almacenes
{
    public interface IAlmacenesService
    {
        Task<List<Almacen>> GetAlmacenesAsync();
        Task<Almacen?> GetAlmacenByIdAsync(int id);
        Task<bool> CreateAlmacenAsync(Almacen almacen);
        Task<bool> UpdateAlmacenAsync(int id, Almacen almacen);
        Task<bool> DeleteAlmacenAsync(int id);
        Task<string> GetNextNumAsync();
    }

    public class AlmacenesService(IHttpClientFactory httpFactory) : IAlmacenesService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<Almacen>> GetAlmacenesAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/almacenes/get");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            return await JsonSerializer.DeserializeAsync<List<Almacen>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
        }

        public async Task<Almacen?> GetAlmacenByIdAsync(int id)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync($"api/almacenes/getbyid/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Almacen();
            return await JsonSerializer.DeserializeAsync<Almacen>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions) ?? new Almacen();
        }

        public async Task<bool> CreateAlmacenAsync(Almacen almacen)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(almacen, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync("api/almacenes/create", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo crear el almacén. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAlmacenAsync(int id, Almacen almacen)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(almacen, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/almacenes/update/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo actualizar el almacén. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAlmacenAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync($"api/almacenes/delete/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo eliminar el almacén. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetNextNumAsync()
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync("api/almacenes/getnextnum");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return "ALM-0001";
            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
            return obj != null && obj.TryGetValue("numero", out var numero) ? numero : "ALM-0001";
        }
    }
}
