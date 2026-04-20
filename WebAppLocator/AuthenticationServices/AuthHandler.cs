using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WebAppLocator.AuthenticationServices
{
    /// <summary>
    /// Класс для авторизации. Вдруг пригодится
    /// </summary>
    public class AuthHandler : AuthenticationHandler<AuthOptions>
    {
        const string ApiKeyHeader = "Authorization";
        private readonly IHttpClientFactory clientFactory;

        public AuthHandler(IOptionsMonitor<AuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory clientFactory) : base(options, logger, encoder)
        {
            this.clientFactory = clientFactory;
        }

        


        /// <summary>
        /// Аутентификация
        /// </summary>
        /// <returns>
        /// Надо уточнить надо ли это вообще делать и если надо то как
        /// </returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
             string apiKey = string.Empty;

           
            if (Request.Headers.TryGetValue(ApiKeyHeader, out var headerValue))
            {
                var queryKey = headerValue.FirstOrDefault(); 
                if (queryKey != null && queryKey.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
                {
                    //apiKey = queryKey.Substring("Bearer ".Length); 
                }
            }


            /*
            var client=clientFactory.CreateClient("Authorization");

            var res = await client.PostAsJsonAsync("https://hzhzhz/","tipa json");

            if (res.IsSuccessStatusCode)
            {
                var content=await res.Content.ReadAsStringAsync();


                //какая то проверка и т.д.
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "device"), // или что там может быть
                    new Claim(ClaimTypes.Name, "device"),
                    new Claim("Token", apiKey)
                };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            */
            return AuthenticateResult.Fail("Invalid api key.");
        }
    }
}
