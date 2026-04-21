using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WebAppCellMapper.AuthenticationServices
{
    /// <summary>
    /// Класс для авторизации. Вдруг пригодится
    /// </summary>
    public class LocatorAuthHandler : AuthenticationHandler<LocatorAuthOptions>
    {
        const string ApiKeyHeader = "x-api-key";

        
        public LocatorAuthHandler(IOptionsMonitor<LocatorAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {

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
                
                if (Options.ApiKey.Contains(apiKey))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier,apiKey), 
                        new Claim(ClaimTypes.Name,apiKey),
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
