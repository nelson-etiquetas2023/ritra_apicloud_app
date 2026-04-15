using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.JSInterop;
using Shared.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace WEB.Services.Upload
{
    public class UploadService(IHttpClientFactory httpFactory, IJSRuntime JS)
    {
        IHttpClientFactory HttpFactory { get; set; } = httpFactory;
        private readonly IJSRuntime JS = JS;

        private static readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

        public async Task<List<UploadResult>> UploadFile(MultipartFormDataContent files) 
        {
            var url = $"api/upload/uploadfile";
            var clienteHttp = HttpFactory.CreateClient("ritrama");
            var response = await clienteHttp.PostAsync(url, files);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<UploadResult>>();
            return results!;
        }

        public async Task<List<UploadResult>> GetAllImages()
        {
            var url = "api/upload/getimages";
            var clienteHttp = HttpFactory.CreateClient("ritrama");
            var response = await clienteHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<UploadResult>>();
            return results!;
        }

        public async Task<byte[]> GetImageById(int id)
        {
            var url = $"api/upload/getimagenbyid?id={id}";
            var clienteHttp = HttpFactory.CreateClient("ritrama");
            var response = await clienteHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<bool> DeleteImage(int id)
        {
            var url = $"api/upload/deleteimage?id={id}";
            var clienteHttp = HttpFactory.CreateClient("ritrama");
            var response = await clienteHttp.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

    }
}
