using System.Text;
using System.Text.Json;

namespace WEB.Services.OrdenCompra
{                   
    public class OrdenCompraService(IHttpClientFactory HttpFactory) : IOrdenCompraService
    {
        private readonly IHttpClientFactory HttpFactory = HttpFactory;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public async Task<List<Shared.Dtos.Compras.OrdenCompra>> GetOrdersAsync()
        {
            var url = $"api/ordencompra/getorders";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString))
                    return [];

                var orders = JsonSerializer.Deserialize
                    <List<Shared.Dtos.Compras.OrdenCompra>>(contentString, jsonOptions);

                return orders ?? [];

            }
            catch (HttpRequestException ex)
            {
                // Aquí puedes registrar el error o mostrar mensaje al usuario
                Console.WriteLine($"Error al obtener órdenes: {ex.Message}");
                return [];
            }
        }

        public async Task<Shared.Dtos.Compras.OrdenCompra?> GetOrderByIdAsync(string numero)
        {
            var url = $"api/ordencompra/getorderbyid/{numero}";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString)) return null;

                return JsonSerializer.Deserialize
                    <Shared.Dtos.Compras.OrdenCompra>(contentString, jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al obtener la orden {numero}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AddOrderAsync(Shared.Dtos.Compras.OrdenCompra oc)
        {
            var url = $"api/ordencompra/addorder";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var json = JsonSerializer.Serialize(oc, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clienteHttp.PostAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al crear la orden: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderAsync(string numero, Shared.Dtos.Compras.OrdenCompra oc)
        {
            var url = $"api/ordencompra/updateorder/{numero}";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var json = JsonSerializer.Serialize(oc, jsonOptions);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await clienteHttp.PutAsync(url, jsonContent);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al actualizar la orden {numero}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteOrderAsync(string numero)
        {
            var url = $"api/ordencompra/deleteorder/{numero}";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al eliminar la orden {numero}: {ex.Message}");
                return false;
            }
        }

        public async Task<int> DeleteAllOrdersAsync()
        {
            var url = "api/ordencompra/deleteall";
            var clienteHttp = HttpFactory.CreateClient("scanpro");

            try
            {
                var response = await clienteHttp.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al eliminar todas las órdenes ({(int)response.StatusCode}): {error}");
                    throw new InvalidOperationException(
                        $"El API respondió {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                        (string.IsNullOrWhiteSpace(error) ? "Sin detalle." : error));
                }

                var contentString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(contentString))
                    throw new InvalidOperationException("El API respondió correctamente, pero no devolvió el resultado del borrado.");

                using var doc = JsonDocument.Parse(contentString);
                if (doc.RootElement.TryGetProperty("eliminadas", out var el) && el.TryGetInt32(out var count))
                    return count;

                throw new InvalidOperationException("El API no devolvió la propiedad 'eliminadas'.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al eliminar todas las órdenes: {ex.Message}");
                throw new InvalidOperationException("No se pudo conectar con el API para borrar las órdenes.", ex);
            }
        }

        public async Task<string> GetNextNumAsync()
        {
            var numeroLocal = await CalcularSiguienteNumeroLocalAsync();

            try
            {
                var clienteHttp = HttpFactory.CreateClient("scanpro");
                var response = await clienteHttp.GetAsync("api/ordencompra/getnextnum");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return numeroLocal;
                var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
                var numeroApi = obj != null && obj.TryGetValue("numero", out var numero)
                    ? numero
                    : null;

                return ElegirSiguienteNumero(numeroApi, numeroLocal);
            }
            catch
            {
                // Si el endpoint no esta disponible, se calcula con las ordenes existentes.
                // El API revalida/genera el numero definitivo al guardar.
                return numeroLocal;
            }
        }

        private static string ElegirSiguienteNumero(string? numeroApi, string? numeroLocal)
        {
            var siguienteApi = ExtraerConsecutivo(numeroApi);
            var siguienteLocal = ExtraerConsecutivo(numeroLocal);
            return $"PO-{Math.Max(siguienteApi, siguienteLocal):0000}";
        }

        private static int ExtraerConsecutivo(string? numero)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                numero?.Trim() ?? string.Empty,
                @"^PO-(\d+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success && int.TryParse(match.Groups[1].Value, out var consecutivo)
                ? consecutivo
                : 1;
        }

        /// <summary>
        /// Calcula el siguiente número de orden de compra localmente con formato PO-9999.
        /// PO = Purchase Order, 9999 = consecutivo con 4 dígitos (relleno con ceros a la izquierda).
        /// Ejemplo: PO-0001, PO-0002, ..., PO-9999, PO-10000
        /// </summary>
        private async Task<string> CalcularSiguienteNumeroLocalAsync()
        {
            try
            {
                var ordenes = await GetOrdersAsync();
                var max = 0;
                foreach (var o in ordenes)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(o.Numero?.Trim() ?? "", @"^PO-(\d+)$");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > max)
                        max = n;
                }
                return $"PO-{max + 1:0000}";
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<bool> AutorizarParaRecepcionAsync(string numero)
        {
            try
            {
                var clienteHttp = HttpFactory.CreateClient("scanpro");
                var response = await clienteHttp.PostAsync($"api/ordencompra/autorizar-recepcion/{numero}", null);
                // Siempre confirmamos y persistimos el estado mediante PUT.
                // Esto también es compatible con una API publicada sin el endpoint POST.
                var orden = await GetOrderByIdAsync(numero);
                if (orden is null) return false;

                orden.Status = 2;
                orden.Sincro = true;
                return await UpdateOrderAsync(numero, orden);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al autorizar la orden {numero} para recepción: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AnularOrderAsync(string numero)
        {
            try
            {
                var clienteHttp = HttpFactory.CreateClient("scanpro");
                var response = await clienteHttp.PostAsync($"api/ordencompra/anular/{numero}", null);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al anular la orden {numero}: {ex.Message}");
                return false;
            }
        }
    }
}               
