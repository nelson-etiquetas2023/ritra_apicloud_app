using Microsoft.JSInterop;
using Shared.Dtos;
using Shared.Dtos.Inventario;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;

namespace WEB.Services.Products
{
    public class ProductsService(IHttpClientFactory httpFactory, IJSRuntime JS) : IProductsService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;
        private readonly IJSRuntime JS = JS;
        private readonly Dictionary<int, long> imageVersions = [];

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public string GetImageUrl(int imageId, int productId)
        {
            var client = HttpFactory.CreateClient("scanpro");
            var baseUrl = client.BaseAddress?.ToString().TrimEnd('/');
            var version = imageVersions.TryGetValue(productId, out long value) ? value : 0L;
            return $"{baseUrl}/api/products/getproductimage/{imageId}?v={version}";
        }

        public async Task<ServiceResponse<Product>> CreateProductWithFilesAsync(Product product, List<IBrowserFile> files)
        {
            try
            {
                var url = $"api/products/createproductwithfiles";
                var clientHttp = HttpFactory.CreateClient("scanpro");

                using var content = new MultipartFormDataContent();

                // Agregar el producto como JSON en un campo 'product'
                var productJson = JsonSerializer.Serialize(product, jsonOptions);
                var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
                content.Add(productContent, "product");

                // Agregar archivos
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file == null) continue;

                    var stream = file.OpenReadStream(maxAllowedSize: 50_000_000);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    // Name 'files' so server can bind Request.Form.Files
                    content.Add(streamContent, "files", file.Name);
                }

                var response = await clientHttp.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new ServiceResponse<Product> { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                var serviceResponse = await JsonSerializer.DeserializeAsync<ServiceResponse<Product>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var message = serviceResponse?.Message ?? $"El servidor devolvió el estado {(int)response.StatusCode}.";
                    return new ServiceResponse<Product> { Success = false, Message = message, Data = serviceResponse?.Data };
                }

                return serviceResponse ?? new ServiceResponse<Product> { Success = false, Message = "No se pudo crear el producto." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product with files: {ex.Message}");
                return new ServiceResponse<Product> { Success = false, Message = $"Error al crear el producto: {ex.Message}" };
            }
        }


        public async Task<List<Product>> GetProductAsync()
        {
            var url = $"api/products/getproducts";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            var products = await JsonSerializer.DeserializeAsync<List<Product>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (products ?? []);
        }
        public async Task<ServiceResponse<Product>> CreateproductAsync(Product product)
        {
            try
            {
                var url = $"api/products/createproducts";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(product, jsonOptions);
                var jsonContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync(url, jsonContent);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new ServiceResponse<Product> { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                var serviceResponse = await JsonSerializer.DeserializeAsync<ServiceResponse<Product>>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseJson)), jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var message = serviceResponse?.Message ?? $"El servidor devolvió el estado {(int)response.StatusCode}.";
                    return new ServiceResponse<Product> { Success = false, Message = message, Data = serviceResponse?.Data };
                }

                return serviceResponse ?? new ServiceResponse<Product> { Success = false, Message = "No se pudo crear el producto." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: " +
                    $"{ex.Message}");
                return new ServiceResponse<Product> { Success = false, Message = $"No se pudo conectar con el servidor: {ex.Message}" };
            }
        }
        public async Task<bool> DeleteProductAsync(int id)
        {
            var url = $"api/products/deleteproducts/{id}";
            var clientHttp = HttpFactory.CreateClient("scanpro");
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
        public async Task<Product> GetProductByIdAsync(int id)
        {
            var url = $"api/products/getproductbyid/{id}";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Product();
            var product = await JsonSerializer.DeserializeAsync<Product>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return product ?? new Product();
        }
        public async Task<ServiceResponse<Product>> UpdateProductAsync(int id, Product product)
        {
            try
            {
                var parametros = new ParametrosUpdateProducts(id, product);
                var url = $"api/products/updateproductsfull";
                var json = JsonSerializer.Serialize(parametros, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PutAsync(url, jsonContent);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new ServiceResponse<Product> { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                var serviceResponse = await JsonSerializer.DeserializeAsync<ServiceResponse<Product>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var message = serviceResponse?.Message ?? $"El servidor devolvió el estado {(int)response.StatusCode}.";
                    return new ServiceResponse<Product> { Success = false, Message = message, Data = serviceResponse?.Data };
                }

                return serviceResponse ?? new ServiceResponse<Product> { Success = false, Message = "No se pudo actualizar el producto." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return new ServiceResponse<Product> { Success = false, Message = $"Error al actualizar el producto: {ex.Message}" };
            }
        }
        public async Task<bool> AddProductImageAsync(int productId, MultipartFormDataContent content, int imageIndex)
        {
            try
            {
                var url = $"api/products/addproductimage/{productId}?imageIndex={imageIndex}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> DeleteProductImageAsync(int imageId)
        {
            try
            {
                var url = $"api/products/deleteproductimage/{imageId}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
                return false;
            }
        }
        public async Task<byte[]> GetProductImageAsync(int imageId)
        {
            try
            {
                var url = $"api/products/getproductimage/{imageId}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                Console.WriteLine($"GetProductImageAsync: Fetching image {imageId} from {url}");
                var response = await clientHttp.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"GetProductImageAsync: Failed to fetch image {imageId}. Status: {response.StatusCode}");
                    return [];
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"GetProductImageAsync: Image {imageId} fetched successfully. Size: {bytes.Length} bytes");
                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProductImageAsync: Error fetching image {imageId}: {ex.Message}");
                return [];
            }
        }
        public async Task<ServiceResponse<Product>> CreateProductWithImagesAsync(CreateProductWithImagesRequest request)
        {
            try
            {
                var url = $"api/products/createproductwithimages";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(request, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync(url, jsonContent);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new ServiceResponse<Product> { Success = false, Message = "El servidor no devolvió una respuesta válida." };

                var serviceResponse = await JsonSerializer.DeserializeAsync<ServiceResponse<Product>>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseJson)), jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var message = serviceResponse?.Message ?? $"El servidor devolvió el estado {(int)response.StatusCode}.";
                    return new ServiceResponse<Product> { Success = false, Message = message, Data = serviceResponse?.Data };
                }

                return serviceResponse ?? new ServiceResponse<Product> { Success = false, Message = "No se pudo crear el producto." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product with images: {ex.Message}");
                return new ServiceResponse<Product> { Success = false, Message = $"Error al crear el producto: {ex.Message}" };
            }
        }
        public async Task<int> BulkCreateProductsAsync(List<Product> products)
        {
            var url = $"api/products/bulkcreateproducts";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var json = JsonSerializer.Serialize(products, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await clientHttp.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode) return 0;
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseJson, jsonOptions);
            return result?.GetValueOrDefault("count") ?? 0;
        }

        public async Task<ProductImportResult> ImportProductsFromExcelAsync(IBrowserFile file)
        {
            try
            {
                var url = "api/products/import-excel";
                var clientHttp = HttpFactory.CreateClient("scanpro");

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 30_000_000);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(streamContent, "file", file.Name);

                var response = await clientHttp.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ImportProductsFromExcelAsync: server returned {response.StatusCode}");
                    return new ProductImportResult
                    {
                        Errors = [new ProductImportError { Row = 0, Message = $"Error del servidor: {(int)response.StatusCode}" }]
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson)) return new ProductImportResult();

                return await JsonSerializer.DeserializeAsync<ProductImportResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new ProductImportResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing products from excel: {ex.Message}");
                return new ProductImportResult
                {
                    Errors = [new ProductImportError { Row = 0, Message = ex.Message }]
                };
            }
        }

        public async Task<byte[]> DownloadStockTemplateAsync(bool incluirProductos)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/products/plantilla-stock-inicial?incluirProductos={incluirProductos}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error descargando plantilla de stock: {ex.Message}");
                return [];
            }
        }

        public async Task<StockInicialResult> AplicarStockInicialAsync(IBrowserFile file, string usuario)
        {
            try
            {
                var url = $"api/products/aplicar-stock-inicial?usuario={Uri.EscapeDataString(usuario ?? "")}";
                var clientHttp = HttpFactory.CreateClient("scanpro");

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 30_000_000);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(streamContent, "file", file.Name);

                var response = await clientHttp.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new StockInicialResult { Message = "El servidor no devolvió respuesta." };
                return await JsonSerializer.DeserializeAsync<StockInicialResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new StockInicialResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error aplicando carga inicial: {ex.Message}");
                return new StockInicialResult
                {
                    Errors = [new StockInicialError { Row = 0, Message = ex.Message }]
                };
            }
        }

        public async Task<List<CargaInicialResumen>> GetCargasInicialesAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/products/cargas-iniciales");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<CargaInicialResumen>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch { return []; }
        }

        public async Task<StockInicialResult> RevertirCargaInicialAsync(string lote)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync($"api/products/carga-inicial/{Uri.EscapeDataString(lote)}");
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new StockInicialResult { Message = "El servidor no devolvió respuesta." };
                return await JsonSerializer.DeserializeAsync<StockInicialResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new StockInicialResult();
            }
            catch (Exception ex)
            {
                return new StockInicialResult { Message = $"No se pudo conectar: {ex.Message}" };
            }
        }

        public async Task<StockInicialResult> ActualizarCargaInicialAsync(string lote, DateTime fecha, string comentario)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var req = new Shared.Dtos.Inventario.CargaInicialUpdate { Fecha = fecha, Comentario = comentario ?? "" };
                var content = new StringContent(JsonSerializer.Serialize(req, jsonOptions), Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync($"api/products/carga-inicial/{Uri.EscapeDataString(lote)}", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                    return new StockInicialResult { Message = "El servidor no devolvió respuesta." };
                return await JsonSerializer.DeserializeAsync<StockInicialResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new StockInicialResult();
            }
            catch (Exception ex)
            {
                return new StockInicialResult { Message = $"No se pudo conectar: {ex.Message}" };
            }
        }

        public async Task<List<Shared.Dtos.Inventario.AjusteInventario>> GetCargaDetalleAsync(string lote)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync($"api/products/carga-inicial/{Uri.EscapeDataString(lote)}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return [];
                return await JsonSerializer.DeserializeAsync<List<Shared.Dtos.Inventario.AjusteInventario>>(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)), jsonOptions) ?? [];
            }
            catch { return []; }
        }

        public async Task<bool> SetProductActiveAsync(int productId, bool active)
        {
            try
            {
                var url = $"api/products/setactive/{productId}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(active, jsonOptions), Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting product active state: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProductImageAsync(int productId, Base64ImageData imageData)
        {
            try
            {
                var url = $"api/products/updateproductimage/{productId}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(imageData, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product image: {ex.Message}");
                return false;
            }
        }
    }
}
