using Microsoft.AspNetCore.Authentication;

namespace WebAppLocator.AuthenticationServices
{
    public class AuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "Bearer";
        public string Scheme => DefaultScheme;
    }

    public class TestAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "TestApiKey";
        public string Scheme => DefaultScheme;
        public string ApiKey => "c07999d9410506831250dc66450412cdd54c36c109344853971b24b2047acabe";
    }
}
