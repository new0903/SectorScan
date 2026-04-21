namespace WebAppCellMapper.Options
{
    public class AuthorityJWTOptions
    {
        public string AuthorityUrl { get; set; }
        public string AuthorityRealmName { get; set; }
        public string AuthorityAudience { get; set; }

        public AuthorityJWTOptions()
        {
            AuthorityUrl = Environment.GetEnvironmentVariable("AUTHORITY_URL");
            AuthorityRealmName = Environment.GetEnvironmentVariable("AUTHORITY_REALM_NAME");
            AuthorityAudience = Environment.GetEnvironmentVariable("AUTHORITY_AUDIENCE");
        }

    }
}
