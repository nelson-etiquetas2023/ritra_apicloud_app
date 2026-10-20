using Shared.Dtos;
using System.Text.Json;

namespace WEB.Services.Etiquetas
{
    public interface IEtiquetasService
    {
        Task<List<LabelTemplate>> GetTemplatesAsync();
        Task<LabelTemplate?> GetTemplateByIdAsync(int id);
        Task<LabelTemplate?> CreateTemplateAsync(LabelTemplate template);
        Task<bool> UpdateTemplateAsync(int id, LabelTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
        Task<LabelTemplate?> DuplicateTemplateAsync(int id);
        Task<LabelTemplate?> GetDefaultTemplateAsync();
        Task<(LabelTemplate? Template, string? Error)> SetDefaultTemplateAsync(int id);
        Task<string?> PreviewZplAsync(int? templateId, string? zpl, int? productId, int copies = 1);
        string PreviewPngUrl(int templateId, int? productId = null, int dpmm = 0);

        Task<List<Printer>> GetPrintersAsync();
        Task<Printer?> CreatePrinterAsync(Printer printer);
        Task<bool> UpdatePrinterAsync(int id, Printer printer);
        Task<bool> DeletePrinterAsync(int id);
        Task<string?> TestPrinterAsync(int id);
        Task<string?> CalibratePrinterAsync(int id);

        Task<List<PrintJob>> GetPendingJobsAsync();
        Task<(PrintJob? Job, string? Error)> EnqueueJobAsync(PrintJobEnqueueRequest request);
        Task<bool> ReorderJobsAsync(List<PrintJobReorderItem> items);
        Task<bool> CancelJobAsync(int id);
        Task<bool> RetryJobAsync(int id);
    }

    public class EtiquetasService(IHttpClientFactory httpFactory) : IEtiquetasService
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        private static readonly JsonSerializerOptions postOptions =
            new()
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,
            };

        private static StringContent JsonBody<T>(T obj) =>
            new(JsonSerializer.Serialize(obj, postOptions), System.Text.Encoding.UTF8, "application/json");

        private async Task<T?> GetJsonAsync<T>(string url) where T : class
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return await JsonSerializer.DeserializeAsync<T>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
        }

        private async Task<T?> PostJsonAsync<T>(string url, object body) where T : class
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var response = await clientHttp.PostAsync(url, JsonBody(body));
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return await JsonSerializer.DeserializeAsync<T>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
        }

        public async Task<List<LabelTemplate>> GetTemplatesAsync() =>
            await GetJsonAsync<List<LabelTemplate>>("api/labeltemplates/get") ?? [];

        public async Task<LabelTemplate?> GetTemplateByIdAsync(int id) =>
            await GetJsonAsync<LabelTemplate>($"api/labeltemplates/getbyid/{id}");

        public async Task<LabelTemplate?> CreateTemplateAsync(LabelTemplate template) =>
            await PostJsonAsync<LabelTemplate>("api/labeltemplates/create", template);

        public async Task<bool> UpdateTemplateAsync(int id, LabelTemplate template)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PutAsync($"api/labeltemplates/update/{id}", JsonBody(template));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync($"api/labeltemplates/delete/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LabelTemplate?> DuplicateTemplateAsync(int id) =>
            await PostJsonAsync<LabelTemplate>($"api/labeltemplates/{id}/duplicate", new { });

        public async Task<LabelTemplate?> GetDefaultTemplateAsync() =>
            await GetJsonAsync<LabelTemplate>("api/labeltemplates/default");

        public async Task<(LabelTemplate? Template, string? Error)> SetDefaultTemplateAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/labeltemplates/setdefault/{id}", JsonBody(new { }));
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return (null, string.IsNullOrWhiteSpace(json) ? $"Error {(int)response.StatusCode}" : json.Trim('"'));
                if (string.IsNullOrWhiteSpace(json)) return (null, "Respuesta vacía.");
                var tpl = await JsonSerializer.DeserializeAsync<LabelTemplate>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
                return (tpl, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<string?> PreviewZplAsync(int? templateId, string? zpl, int? productId, int copies = 1)
        {
            var result = await PostJsonAsync<Dictionary<string, string>>(
                "api/labeltemplates/preview-zpl",
                new PreviewZplRequest { TemplateId = templateId, Zpl = zpl ?? "", ProductId = productId, Copies = copies });
            return result != null && result.TryGetValue("zpl", out var value) ? value : null;
        }

        public string PreviewPngUrl(int templateId, int? productId = null, int dpmm = 0)
        {
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var baseUrl = clientHttp.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5220";
            var url = $"{baseUrl}/api/labeltemplates/preview-png/{templateId}";
            var query = new List<string>();
            if (productId.HasValue) query.Add($"productId={productId.Value}");
            if (dpmm is 6 or 8 or 12 or 24) query.Add($"dpmm={dpmm}");
            if (query.Count > 0) url += "?" + string.Join("&", query);
            return url;
        }

        public async Task<List<Printer>> GetPrintersAsync() =>
            await GetJsonAsync<List<Printer>>("api/printers/get") ?? [];

        public async Task<Printer?> CreatePrinterAsync(Printer printer) =>
            await PostJsonAsync<Printer>("api/printers/create", printer);

        public async Task<bool> UpdatePrinterAsync(int id, Printer printer)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PutAsync($"api/printers/update/{id}", JsonBody(printer));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeletePrinterAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.DeleteAsync($"api/printers/delete/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> TestPrinterAsync(int id)
        {
            try
            {
                var result = await PostJsonAsync<Dictionary<string, object>>($"api/printers/test/{id}", new { });
                if (result == null) return "Sin respuesta de la API.";
                if (result.TryGetValue("ok", out var ok) && ok?.ToString() == "True") return null;
                return result.TryGetValue("error", out var err) ? err?.ToString() : "Falló la prueba.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> CalibratePrinterAsync(int id)
        {
            try
            {
                var result = await PostJsonAsync<Dictionary<string, object>>($"api/printers/calibrate/{id}", new { });
                if (result == null) return "Sin respuesta de la API.";
                if (result.TryGetValue("ok", out var ok) && ok?.ToString() == "True") return null;
                return result.TryGetValue("error", out var err) ? err?.ToString() : "Falló el calibrado.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<List<PrintJob>> GetPendingJobsAsync() =>
            await GetJsonAsync<List<PrintJob>>("api/printjobs/pending") ?? [];

        public async Task<(PrintJob? Job, string? Error)> EnqueueJobAsync(PrintJobEnqueueRequest request)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync("api/printjobs/enqueue", JsonBody(request));
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return (null, string.IsNullOrWhiteSpace(json) ? $"Error {(int)response.StatusCode}" : json);
                if (string.IsNullOrWhiteSpace(json)) return (null, "Respuesta vacía de la API.");
                var job = await JsonSerializer.DeserializeAsync<PrintJob>(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
                return job == null ? (null, "Respuesta inválida.") : (job, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<bool> ReorderJobsAsync(List<PrintJobReorderItem> items)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync("api/printjobs/reorder", JsonBody(items));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CancelJobAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/printjobs/cancel/{id}", JsonBody(new { }));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RetryJobAsync(int id)
        {
            try
            {
                var clientHttp = HttpFactory.CreateClient("scanpro");
                var response = await clientHttp.PostAsync($"api/printjobs/retry/{id}", JsonBody(new { }));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
