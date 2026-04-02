
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Configuration;
using System.Reflection;
using WebAppCellMapper.BackgroundServices;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.Helpers;
using WebAppCellMapper.Options;
using WebAppCellMapper.Proxy;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Extensions
{
    public static class ServiceExtensions
    {
        private static Assembly currentAssembly => typeof(ServiceExtensions).Assembly;
        public static IServiceCollection InitDBContext(this IServiceCollection services)
        {
            var conn = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");

            //conn = $"Host=127.0.0.1;" +
            //$"Port=54321;" +
            //$"Database=ConfigManager;" +
            //$"Username=root;" +
            //$"Password=root";
            services.AddNpgsqlDataSource(conn, (ds) =>
            {
                ds.EnableDynamicJson();
            });
            services.AddHealthChecks().AddNpgSql();
            services.AddDbContext<AppDBContext>(( sp,opt) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                opt.UseNpgsql(dataSource);//conn.Value.ToString()
            });
            return services;
        }
       
        public static IServiceCollection IncludeServices(this IServiceCollection services)
        {
            //.ConfigurePrimaryHttpMessageHandler<HttpClientHandler>()
            services.AddSingleton<IGeoBoundsService,GeoBoundsService>();
            services.AddSingleton<IProxyService, ProxyService>();
            services.AddSingleton<IProxyHandlerPoolService, ProxyHandlerPoolService>();
            services.AddSingleton<IStationsScanningManager, StationsScanningManager>();
            services.AddSingleton<IRequestIdGenerator, RequestIdGenerator>();
            services.AddScoped<IStationsService, StationsService>();
            services.AddScoped<IOperatorsService ,OperatorsService>();
            services.AddScoped<IProgressRepository, ProgressRepository>();
            services.AddScoped<IRuntimeRepository, RuntimeRepository>();
            services.AddHostedService<AppBackgroundService>();

            return services;
        }

        public static IServiceCollection AddOptionsSetups(this IServiceCollection services, IConfiguration configuration)
        {


            var types = currentAssembly.GetTypes();
            var genericBaseType = typeof(OptionsSetup<>);
            var setups = types.Where(type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == genericBaseType);

            foreach (var type in setups)
            {
                services.ConfigureOptions(type);
            }



            string host= Environment.GetEnvironmentVariable("PG_HOST");
            string port = Environment.GetEnvironmentVariable("PG_PORT");
            string database = Environment.GetEnvironmentVariable("PG_DATABASE");
            string user = Environment.GetEnvironmentVariable("PG_USER");
            string pass = Environment.GetEnvironmentVariable("PG_PASSWORD");
            string connString= $"Host={host};" +
                $"Port={port};" +
                $"Database={database};" +
                $"Username={user};" +
                $"Password={pass}";

            Environment.SetEnvironmentVariable("PG_CONNECTION_STRING", connString);

            

            return services;
        }
    }
}
