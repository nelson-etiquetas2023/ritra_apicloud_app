using Microsoft.JSInterop;
using Shared.Dtos;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Config
{
    public class ConfigService : IConfigService
    {
        private readonly IHttpClientFactory httpFactory;
        private readonly IJSRuntime JS;

        private static readonly JsonSerializerOptions jsonOptions =
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

        public ConfigService(IHttpClientFactory httpFactory, IJSRuntime JS)
        {
            this.httpFactory = httpFactory;
            this.JS = JS;
        }
        public async Task<List<Parameter>> LoadDataConfig()
        {
            var url = "api/config/getloaddataconfig";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new List<Parameter>();
            var data = await JsonSerializer.DeserializeAsync<List<Parameter>>(
                new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (data ?? new List<Parameter>());
        }

        public async Task<bool> UpdateDocumentSettings(string filter, DocumentSettings settings)
        {
            var url = $"api/config/updateconfigdocumentsettings/{filter}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var json = JsonSerializer.Serialize(settings, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
    }
}
