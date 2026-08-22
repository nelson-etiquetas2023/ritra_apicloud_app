using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shared.Dtos;

namespace WEB.Services.Customers
{
    public class CustomersService(IHttpClientFactory httpFactory) : ICustomersService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var url = "api/customers/getcustomers";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return [];
            var customers = await JsonSerializer.DeserializeAsync<List<Customer>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (customers ?? []);
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            var url = $"api/customers/getcustomerbyid/{id}";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return new Customer();
            var customer = await JsonSerializer.DeserializeAsync<Customer>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return customer ?? new Customer();
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var url = "api/customers/createcustomers";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var json = JsonSerializer.Serialize(customer, jsonOptions);
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

        public async Task<bool> UpdateCustomerAsync(int id, Customer customer)
        {
            try
            {
                var parametros = new ParametrosUpdateCustomers(id, customer);
                var url = "api/customers/updatecustomers";
                var json = JsonSerializer.Serialize(parametros, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo actualizar el cliente. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            try
            {
                var url = $"api/customers/deletecustomers/{id}";
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excepción]: No se pudo eliminar el cliente. Detalle: {ex.Message}");
                return false;
            }
        }

        public async Task<CustomerImportResult> ImportCustomersFromExcelAsync(IBrowserFile file)
        {
            try
            {
                var url = "api/customers/import-excel";
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
                    Console.WriteLine($"ImportCustomersFromExcelAsync: server returned {response.StatusCode}");
                    return new CustomerImportResult
                    {
                        Errors = [new CustomerImportError { Row = 0, Message = $"Error del servidor: {(int)response.StatusCode}" }]
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson)) return new CustomerImportResult();

                return await JsonSerializer.DeserializeAsync<CustomerImportResult>(
                    new MemoryStream(Encoding.UTF8.GetBytes(responseJson)), jsonOptions) ?? new CustomerImportResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing customers from excel: {ex.Message}");
                return new CustomerImportResult
                {
                    Errors = [new CustomerImportError { Row = 0, Message = ex.Message }]
                };
            }
        }

        public async Task<string> GetNextNumAsync()
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.GetAsync("api/customers/getnextnum");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return string.Empty;
                var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
                return obj != null && obj.TryGetValue("numero", out var numero) ? numero : string.Empty;
            }
            catch
            {
                // Si el endpoint no esta disponible, el API genera el codigo real al guardar.
                return string.Empty;
            }
        }
    }
}