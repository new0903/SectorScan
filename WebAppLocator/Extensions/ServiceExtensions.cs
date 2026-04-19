
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Configuration;
using System.Reflection;
using WebAppCellMapper.Services;
using WebAppLocator.AuthenticationServices;
using WebAppLocator.BackgroundServices;
using WebAppLocator.Data;
using WebAppLocator.Data.Repository;
using WebAppLocator.Helpers;
using WebAppLocator.Service;


namespace WebAppLocator.Extensions
{
    public static class ServiceExtensions
    {
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
            services.AddDbContext<LocatorDbContext>((sp, opt) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                opt.UseNpgsql(dataSource);//conn.Value.ToString()
            });
            return services;
        }
       
        public static IServiceCollection IncludeServices(this IServiceCollection services)
        {
            services.AddHttpClient("Authorization", client =>
            {
               // client.BaseAddress =new Uri("");
               //надо будет до настроить
            });

            services.AddHttpClient("Graph", client =>
            {
                client.BaseAddress =new Uri("http://graphhopper:8989/");//лучше из конфига наверное, но может быть вообще не пригодится и вырежу полностью graphhopper
                //надо будет до настроить
            });

            services.AddSingleton<WorkflowService>();
            services.AddSingleton<GeoHelper>();
            services.AddScoped<ILocatorService, LocatorService>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IGraphHopperService,GraphHopperService>();

            services.AddHostedService<EraserDbBackgroundService>();
           
            return services;
        }



        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = "SmartScheme"; //моя схема
            }).AddPolicyScheme("SmartScheme", "Authorization Scheme", opt => opt.ForwardDefaultSelector=context=>
            {  
                var hasApiKey = context.Request.Headers.ContainsKey("x-api-key") &&
                                context.Request.Headers["x-api-key"].ToString().StartsWith("Token ");
                var hasBearer = context.Request.Headers.ContainsKey("Authorization") &&
                                context.Request.Headers["Authorization"].ToString().StartsWith("Token ");

                if (hasApiKey)
                    return TestAuthOptions.DefaultScheme;

                if (hasBearer)
                    return AuthOptions.DefaultScheme;

                return AuthOptions.DefaultScheme; 
            })
            .AddScheme<AuthOptions, AuthHandler>(AuthOptions.DefaultScheme, config => { })
            .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthOptions.DefaultScheme, configuration.GetSection("TestApiKey").Bind); //это надо исключительно для тестирования
            services.AddAuthorization();
            return services;
        }


        public static IServiceCollection AddOptionsSetups(this IServiceCollection services)
        {

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
