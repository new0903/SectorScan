using Microsoft.AspNetCore.Authentication;

namespace WebAppCellMapper.AuthenticationServices
{
    //public class AuthOptions : AuthenticationSchemeOptions
    //{
    //    public const string DefaultScheme = "DeviceToken";
    //    public string Scheme => DefaultScheme;
    //}

    public class LocatorAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "LocatorApiKey";
        public string Scheme => DefaultScheme;
        public string[] ApiKey { get; set; }
    }
}
