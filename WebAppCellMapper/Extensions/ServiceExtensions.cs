
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Configuration;
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
            var conn = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
            services.AddDbContext<AppDBContext>((opt) =>
            {
              //  var s= sP.GetRequiredService<IOptions<DatabaseConnection>>().Value;

               
                opt.UseNpgsql(conn);//conn.Value.ToString()
            });
            services.AddNpgsqlDataSource(conn);
            services.AddHealthChecks().AddNpgSql();
            return services;
        }
       
        public static IServiceCollection IncludeServices(this IServiceCollection services)
        {
            //.ConfigurePrimaryHttpMessageHandler<HttpClientHandler>()
            services.AddSingleton<IGeoBoundsService,GeoBoundsService>();
            services.AddSingleton<IProxyService, ProxyService>();
            services.AddSingleton<IProxyHandlerPoolService, ProxyHandlerPoolService>();
            services.AddScoped<IStationsService, StationsService>();//AddScoped или AddTransient
            services.AddScoped<IOperatorsService ,OperatorsService>();
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

            
            //var dbConnection = new DatabaseConnection();
            //configuration.GetSection("PG").Bind(dbConnection);
            //Console.WriteLine(dbConnection.ToString());

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
            //Environment.SetEnvironmentVariable("PG_USER", $"{dbConnection.USER}");
            //Environment.SetEnvironmentVariable("PG_PASSWORD", $"{dbConnection.PASSWORD}");
            //Environment.SetEnvironmentVariable("PG_SERVER", $"{dbConnection.HOST}:{dbConnection.PORT}");
            //Environment.SetEnvironmentVariable("PG_DATABASE", $"{dbConnection.DATABASE}");
            

            return services;
        }
    }
}
