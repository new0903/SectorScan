using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace WebAppCellMapper.Options
{
    public abstract class OptionsSetup<T> : IConfigureOptions<T> where T : class
    {
        protected string SettingsPath { get; init; }
        protected readonly IConfiguration Configuration;

        public OptionsSetup(string settingsPath, IConfiguration configuration)
        {
            SettingsPath = settingsPath;
            Configuration = configuration;

        }

        public virtual void Configure(T options)
        {
            Configuration.GetSection(SettingsPath).Bind(options);
        }
    }

   // public class DatabaseConnectionSetup(IConfiguration configuration) : OptionsSetup<DatabaseConnection>("PG", configuration);
    public class DatabaseConnectionSetup(IConfiguration configuration) : OptionsSetup<DatabaseConnection>("PG", configuration)
    {

        public override void Configure(DatabaseConnection options)
        {
            base.Configure(options);
            Environment.SetEnvironmentVariable("PG_CONNECTION_STRING", options.ToString());
            Environment.SetEnvironmentVariable("PG_USER", $"{options.Username}");
            Environment.SetEnvironmentVariable("PG_PASSWORD", $"{options.Password}");
            Environment.SetEnvironmentVariable("PG_SERVER", $"{options.Host}:{options.Port}");
            Environment.SetEnvironmentVariable("PG_DATABASE", $"{options.Database}");
        }
    }

    public class RequestSettingsSetup(IConfiguration configuration) : OptionsSetup<RequestSettings>("RequestSettings", configuration) 
    {
    
    }
    //public class DatabaseDefaultConnectionSetup(IConfiguration configuration) : OptionsSetup<DatabaseDefaultConnection>("ConnectionStrings", configuration);

}
