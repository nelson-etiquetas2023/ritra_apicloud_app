using System.Net.Http.Headers;
using WEB.Services.LocalStorage;

namespace WEB.Services.Auth
{
    public class AuthMessageHandler(ILocalStorage localStorage) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await localStorage.GetItemAsync<string>("authToken");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));
                }
            }
            catch
            {
                // Sin token, se envía la petición sin encabezado de autorización.
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
