using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
                opt.UseNpgsql(conn.Value.ToString());
            });

            return services;
        }

        public static IServiceCollection IncludeServices(this IServiceCollection services)
        {

            services.AddSingleton<GeoBoundsService>();
            services.AddSingleton<ProxyService>();
            services.AddScoped<StationsService>();
            services.AddScoped<OperatorsService>();
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
