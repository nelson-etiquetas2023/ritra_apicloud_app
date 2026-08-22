using Microsoft.JSInterop;
using Shared.Dtos;
using Shared.Security;
using System.Net.Http.Json;
using System.Text.Json;

namespace WEB.Services.Auth
{
    public class AuthService(IHttpClientFactory httpFactory, IJSRuntime JS) : IAuthService
    {
        public IHttpClientFactory HttpFactory { get; set; } = httpFactory;
        public IJSRuntime JS { get; set; } = JS;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        public async Task<ServiceResponse<int>> Register(UserRegister user)
        {
            var url = $"api/auth/register";
            var clientHttp = HttpFactory.CreateClient("scanpro");
            var json = JsonSerializer.Serialize(user, jsonOptions);
            var jsonContent = new StringContent(json, System.Text.Encoding.UTF8,"application/json");
            var response = await clientHttp.PostAsync(url, jsonContent);
            var result = new ServiceResponse<int>();
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                result = JsonSerializer.Deserialize<ServiceResponse<int>>(content, jsonOptions) ?? new ServiceResponse<int>();
            }
            else 
            {
                result.Success = false;
                result.Message = "usuario ya existe...";
            }
            return result;
         }
        public async Task<ServiceResponse<string>> Login(UserLogin user)
        {
            var url = $"api/auth/login";
            var clienteHttp = HttpFactory.CreateClient("scanpro");
            var json = JsonSerializer.Serialize(user, jsonOptions);
            var jsonContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await clienteHttp.PostAsync(url, jsonContent);
            var deserized = await response.Content.ReadFromJsonAsync<ServiceResponse<string>>();
            return deserized ?? new ServiceResponse<string>();
        }
    }
}
