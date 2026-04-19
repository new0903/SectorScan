using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using static System.Net.Mime.MediaTypeNames;

namespace WebAppLocator.AuthenticationServices
{
    /// <summary>
    /// Класс для авторизации. Вдруг пригодится
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
    {
        const string ApiKeyHeader = "x-api-key";
        private readonly IHttpClientFactory clientFactory;

        
        public TestAuthHandler(IOptionsMonitor<TestAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory clientFactory) : base(options, logger, encoder)
        {
            this.clientFactory = clientFactory;

        }

        


        /// <summary>
        /// Аутентификация 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
             string apiKey = string.Empty;

            if (Request.Headers[ApiKeyHeader].Any())
            {
                var queryKey = Request.Headers[ApiKeyHeader].FirstOrDefault();
                apiKey = queryKey.Substring("Token ".Length);
                
                if (Options.ApiKey == apiKey)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "testKeyIdentifier"), // или что там может быть
                        new Claim(ClaimTypes.Name, "testKey"),
                        new Claim("Token", apiKey)
                    };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);

                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }

            }

            

            return AuthenticateResult.Fail("Invalid api key.");
        }
    }
}
