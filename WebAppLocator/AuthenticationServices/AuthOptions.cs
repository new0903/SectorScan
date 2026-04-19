using Microsoft.AspNetCore.Authentication;

namespace WebAppLocator.AuthenticationServices
{
    public class AuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "DeviceToken";
        public string Scheme => DefaultScheme;
    }

    public class TestAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "TestApiKey";
        public string Scheme => DefaultScheme;
        public string ApiKey { get; set; }
    }
}
