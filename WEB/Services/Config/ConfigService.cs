using Microsoft.JSInterop;
using Shared.Dtos;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

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
            var clientHttp = httpFactory.CreateClient("scanpro");
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
            var clientHttp = httpFactory.CreateClient("scanpro");
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

        public async Task<List<Category>> GetCategoriesAsync()
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.GetAsync("api/config/categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Category>>(jsonOptions) ?? [];
        }

        public async Task<Category?> CreateCategoryAsync(Category category)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PostAsJsonAsync("api/config/categories", category, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Category>(jsonOptions);
        }

        public async Task<Category?> UpdateCategoryAsync(int id, Category category)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PutAsJsonAsync($"api/config/categories/{id}", category, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Category>(jsonOptions);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.DeleteAsync($"api/config/categories/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ProductUnit>> GetProductUnitsAsync()
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.GetAsync("api/config/units");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ProductUnit>>(jsonOptions) ?? [];
        }

        public async Task<ProductUnit?> CreateProductUnitAsync(ProductUnit unit)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PostAsJsonAsync("api/config/units", unit, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProductUnit>(jsonOptions);
        }

        public async Task<ProductUnit?> UpdateProductUnitAsync(int id, ProductUnit unit)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PutAsJsonAsync($"api/config/units/{id}", unit, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProductUnit>(jsonOptions);
        }

        public async Task<bool> DeleteProductUnitAsync(int id)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.DeleteAsync($"api/config/units/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.GetAsync("api/config/warehouses");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Warehouse>>(jsonOptions) ?? [];
        }

        public async Task<List<Location>> GetLocationsAsync()
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.GetAsync("api/config/locations");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Location>>(jsonOptions) ?? [];
        }

        public async Task<Location?> CreateLocationAsync(Location location)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PostAsJsonAsync("api/config/locations", location, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Location>(jsonOptions);
        }

        public async Task<Location?> UpdateLocationAsync(int id, Location location)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.PutAsJsonAsync($"api/config/locations/{id}", location, jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Location>(jsonOptions);
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            var client = httpFactory.CreateClient("scanpro");
            var response = await client.DeleteAsync($"api/config/locations/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
