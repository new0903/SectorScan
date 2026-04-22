
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Reflection;
using WebAppCellMapper.AuthenticationServices;
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
            services.AddSingleton<IRequestHelper, RequestHelper>();

            services.AddScoped<IStationsService, StationsService>();
            services.AddScoped<IOperatorsService ,OperatorsService>();

            services.AddScoped<IProgressRepository, ProgressRepository>();
            services.AddScoped<IRuntimeRepository, RuntimeRepository>();
            services.AddScoped<IStationsRepository, StationsRepository>(); 
            

            services.AddHostedService<AppBackgroundService>();//запускает задачу если сервис был перезапущен и задача была преравана
            services.AddHostedService<AppBackgroundService>(); // стирает станные данные о прогрессе.
            return services;
        }
        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var authority = new AuthorityJWTOptions();//уточнить правильно ли сделал авторизацию и куда пихать JWT
            services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = "SmartScheme"; 
            }).AddPolicyScheme("SmartScheme", "Authorization Scheme", opt => opt.ForwardDefaultSelector = context =>
            {
                var hasApiKey = context.Request.Headers.ContainsKey("x-api-key");
                var hasBearer = context.Request.Headers.ContainsKey("Authorization") &&
                                context.Request.Headers["Authorization"].ToString().StartsWith("Bearer ");

                if (hasApiKey)
                    return LocatorAuthOptions.DefaultScheme;

                if (hasBearer)
                    return JwtBearerDefaults.AuthenticationScheme;

                return JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer((jwt) =>
            {
                jwt.Authority = $"{authority.AuthorityUrl}/realms/{authority.AuthorityRealmName}";
                jwt.Audience = authority.AuthorityAudience;
                jwt.TokenValidationParameters.ValidateAudience = true;
            })
            .AddScheme<LocatorAuthOptions, LocatorAuthHandler>(LocatorAuthOptions.DefaultScheme, configuration.GetSection("Authentication").Bind); 
            
            //это надо исключительно для тестирования
            //.AddScheme<AuthOptions, AuthHandler>(AuthOptions.DefaultScheme, config => { })
            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection IncludeLocatorServices(this IServiceCollection services)
        {
        

            services.AddHttpClient("Graph", (sp,client) =>
            {
                var opt = sp.GetRequiredService<IOptions<GraphOptions>>();
                client.BaseAddress = new Uri(opt.Value.Url);
            });

            services.AddSingleton<WorkflowService>();
            services.AddSingleton<GeoHelper>();
            services.AddScoped<ILocatorService, LocatorService>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IGraphHopperService, GraphHopperService>();

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
