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
            services.AddDbContext<AppDBContext>((sP, opt) =>
            {
                var s= sP.GetRequiredService<IOptions<DatabaseConnection>>().Value;

                var conn = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");

                opt.UseNpgsql(conn);//conn.Value.ToString()
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
