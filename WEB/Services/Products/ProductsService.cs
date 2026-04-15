using Microsoft.JSInterop;
using Shared.Dtos;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Products
{
    public class ProductsService : IProductsService
    {
        IHttpClientFactory httpFactory { get; set; }
        private readonly IJSRuntime JS;

        private static readonly JsonSerializerOptions jsonOptions =
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public ProductsService(IHttpClientFactory httpFactory, IJSRuntime JS)
        {
            this.httpFactory = httpFactory;
            this.JS = JS;
        }

        public async Task<List<Product>> GetProductAsync()
        {
            var url = $"api/products/getproducts";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new List<Product>();
            var products = await JsonSerializer.DeserializeAsync<List<Product>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (products ?? new List<Product>());
        }

        public async Task<bool> CreateproductAsync(Product product)
        {
            try
            {
                var url = $"api/products/createproducts";
                var clientHttp = httpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(product, jsonOptions);
                var jsonContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: " +
                    $"{ex.Message}");
                return false;
                
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var url = $"api/products/deleteproducts/{id}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.DeleteAsync(url);
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
        public Task<Product> GetProductByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateProductAsync(int id, Product product)
        {
            //utilizo la tupla para pasar 2 parametros.
            var parametros = new ParametrosUpdateProducts(id, product);
            var url = $"api/products/updateproducts";
            var json = JsonSerializer.Serialize(parametros, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.PutAsync(url, jsonContent);
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
