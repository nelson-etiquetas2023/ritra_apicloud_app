using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shared.Dtos;

namespace WEB.Services.Suppliers
{
    public class SuppliersService(IHttpClientFactory httpFactory) : ISuppliersService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            var url = "api/suppliers/getsuppliers";
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            var suppliers = await JsonSerializer.DeserializeAsync<List<Supplier>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (suppliers ?? []);
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            var url = $"api/suppliers/getsupplierbyid/{id}";
            var clientHttp = HttpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Supplier();
            var supplier = await JsonSerializer.DeserializeAsync<Supplier>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return supplier ?? new Supplier();
        }

        public async Task<bool> CreateSupplierAsync(Supplier supplier)
        {
            try
            {
                var url = "api/suppliers/createsuppliers";
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var json = JsonSerializer.Serialize(supplier, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clientHttp.PostAsync(url, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var detail = string.IsNullOrWhiteSpace(errorBody) ? $"Error del servidor: {(int)response.StatusCode}" : errorBody;
                    throw new Exception(detail);
                }
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo conectar con el servidor. Detalle: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSupplierAsync(int id, Supplier supplier)
        {
            try
            {
                var parametros = new ParametrosUpdateSuppliers(id, supplier);
                var url = "api/suppliers/updatesuppliers";
                var json = JsonSerializer.Serialize(parametros, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var response = await clientHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo actualizar el proveedor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            try
            {
                var url = $"api/suppliers/deletesuppliers/{id}";
                var clientHttp = HttpFactory.CreateClient("ritrama");
                var response = await clientHttp.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo eliminar el proveedor. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<SupplierImportResult> ImportSuppliersFromExcelAsync(IBrowserFile file)
        {
            try
            {
                var url = "api/suppliers/import-excel";
                var clientHttp = HttpFactory.CreateClient("ritrama");

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 30_000_000);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                    file.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(streamContent, "file", file.Name);

                var response = await clientHttp.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ImportSuppliersFromExcelAsync: server returned {response.StatusCode}");
                    return new SupplierImportResult
                    {
                        Errors = [new SupplierImportError { Row = 0, Message = $"Error del servidor: {(int)response.StatusCode}" }]
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson)) return new SupplierImportResult();

                return await JsonSerializer.DeserializeAsync<SupplierImportResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new SupplierImportResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing suppliers from excel: {ex.Message}");
                return new SupplierImportResult
                {
                    Errors = [new SupplierImportError { Row = 0, Message = ex.Message }]
                };
            }
        }
    }
}
