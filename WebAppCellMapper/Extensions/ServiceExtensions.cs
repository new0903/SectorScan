using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using WebAppCellMapper.Data;
using WebAppCellMapper.Options;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Extensions
{
    public static class ServiceExtensions
    {
        private static Assembly currentAssembly => typeof(ServiceExtensions).Assembly;
        public static IServiceCollection InitDBContext(this IServiceCollection services)
        {
            services.AddDbContext<AppDBContext>((sP,opt) =>
            {
                var conn= sP.GetRequiredService<IOptions<DatabaseConnection>>();


                Environment.SetEnvironmentVariable("PG_CONNECTION_STRING", conn.Value.ToString());
                Environment.SetEnvironmentVariable("PG_USER", $"{conn.Value.Username}");
                Environment.SetEnvironmentVariable("PG_PASSWORD", $"{conn.Value.Password}");
                Environment.SetEnvironmentVariable("PG_SERVER", $"{conn.Value.Host}:{conn.Value.Port}");
                Environment.SetEnvironmentVariable("PG_DATABASE", $"{conn.Value.Database}");

                opt.UseNpgsql(conn.Value.ToString());
            });

            return services;
        }
       
        public static IServiceCollection IncludeServices(this IServiceCollection services)
        {
            //.ConfigurePrimaryHttpMessageHandler<HttpClientHandler>()
            services.AddSingleton<IGeoBoundsService,GeoBoundsService>();
            services.AddSingleton<IProxyService, ProxyService>();
            services.AddScoped<IStationsService, StationsService>();//AddScoped или AddTransient
            services.AddScoped<IOperatorsService ,OperatorsService>();
            return services;

        }
        public static IServiceCollection AddOptionsSetups(this IServiceCollection services)
        {
            var types = currentAssembly.GetTypes();
            var genericBaseType = typeof(OptionsSetup<>);
            var setups = types.Where(type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == genericBaseType);

            foreach (var type in setups)
            {
                services.ConfigureOptions(type);
            }

            return services;
        }
    }
}
