using System.Text;
using System.Text.Json;
using ScanProMovil.Services.Session;

namespace ScanProMovil.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string email, string password);
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly AuthSession _session;
        private readonly AppSession _appSession;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthService(IHttpClientFactory httpFactory, AuthSession session, AppSession appSession)
        {
            _httpFactory = httpFactory;
            _session = session;
            _appSession = appSession;
        }

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            var client = _httpFactory.CreateClient("scanpro");
            var payload = JsonSerializer.Serialize(new { email, password });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/auth/login", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var dto = Deserialize(json);
                return new LoginResult
                {
                    Success = false,
                    Message = !string.IsNullOrWhiteSpace(dto?.Message)
                        ? dto.Message
                        : $"Error del servidor ({(int)response.StatusCode})."
                };
            }

            var serviceResponse = Deserialize(json);
            if (serviceResponse?.Success == true && !string.IsNullOrWhiteSpace(serviceResponse.Data))
            {
                await _session.SetTokenAsync(serviceResponse.Data);
                _session.Reset();
                _appSession.InitializeDevice();
                _appSession.SetUserFromToken(serviceResponse.Data);
                return new LoginResult
                {
                    Success = true,
                    Token = serviceResponse.Data,
                    Message = serviceResponse.Message
                };
            }

            return new LoginResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(serviceResponse?.Message)
                    ? "Credenciales incorrectas."
                    : serviceResponse.Message
            };
        }

        private static ServiceResponseDto? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<ServiceResponseDto>(json, jsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }

    public class ServiceResponseDto
    {
        public string? Data { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}