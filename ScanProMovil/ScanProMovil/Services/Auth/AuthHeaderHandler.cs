using System.Net.Http.Headers;

namespace ScanProMovil.Services.Auth
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthSession _session;

        public AuthHeaderHandler(AuthSession session)
        {
            _session = session;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _session.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}