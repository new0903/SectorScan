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

    public class DatabaseConnectionSetup(IConfiguration configuration) : OptionsSetup<DatabaseConnection>("Storage", configuration);
    public class RequestSettingsSetup(IConfiguration configuration) : OptionsSetup<RequestSettings>("RequestSettings", configuration);

    //public class DatabaseDefaultConnectionSetup(IConfiguration configuration) : OptionsSetup<DatabaseDefaultConnection>("ConnectionStrings", configuration);

}
