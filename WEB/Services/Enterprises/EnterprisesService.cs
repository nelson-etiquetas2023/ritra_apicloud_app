using System.Text;
using System.Text.Json;
using Shared.Dtos;

namespace WEB.Services.Enterprises
{
    public class EnterprisesService(IHttpClientFactory httpFactory) : IEnterprisesService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<Enterprise?> GetEnterpriseAsync()
        {
            var url = "api/enterprises/getenterprise";
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            var enterprise = await JsonSerializer.DeserializeAsync<Enterprise>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return enterprise;
        }

        public async Task<bool> CreateEnterpriseAsync(Enterprise enterprise)
        {
            try
            {
                var url = "api/enterprises/createenterprise";
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(enterprise, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateEnterpriseAsync(int id, Enterprise enterprise)
        {
            try
            {
                var parametros = new ParametrosUpdateEnterprise(id, enterprise);
                var url = "api/enterprises/updateenterprise";
                var json = JsonSerializer.Serialize(parametros, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var response = await clientHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo actualizar la empresa. Detalle: {ex.Message}");
                return false;
            }
        }
    }
}