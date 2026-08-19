using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using WEB.Services.LocalStorage;

namespace WEB
{
    public class CustomAuthStateProvider(ILocalStorage localStorage, HttpClient http) : AuthenticationStateProvider
    {
        public ILocalStorage LocalStorage { get; set; } = localStorage;
        public HttpClient Http { get; set; } = http;

        private static readonly Dictionary<string, string> ClaimTypeMap = new()
        {
            { "nameid", ClaimTypes.NameIdentifier },
            { "sub", ClaimTypes.NameIdentifier },
            { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", ClaimTypes.NameIdentifier },

            { "unique_name", ClaimTypes.Name },
            { "name", ClaimTypes.Name },
            { "preferred_username", ClaimTypes.Name },
            { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", ClaimTypes.Name },

            { "email", ClaimTypes.Email },
            { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", ClaimTypes.Email },

            { "role", ClaimTypes.Role },
            { "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", ClaimTypes.Role },
        };

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string authToken = await LocalStorage.GetItemAsync<string>("authToken") ?? "";

            var identity = new ClaimsIdentity();
            Http.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    identity = new ClaimsIdentity(ParseClaimsFromJwt(authToken), "jwt");
                    Http.DefaultRequestHeaders.Authorization = new
                        AuthenticationHeaderValue("Bearer", authToken.Replace("\"", ""));
                }
                catch
                {
                    await LocalStorage.RemoveItemAsync("authToken");
                    identity = new ClaimsIdentity();
                }
            }
            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private static string GetClaimValue(object value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind == JsonValueKind.String
                    ? element.GetString() ?? ""
                    : element.ToString();
            }
            return value.ToString() ?? "";
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = keyValuePairs!.Select(kvp =>
            {
                var type = ClaimTypeMap.TryGetValue(kvp.Key, out var mapped) ? mapped : kvp.Key;
                return new Claim(type, GetClaimValue(kvp.Value));
            });

            return claims;
        }
    }
}