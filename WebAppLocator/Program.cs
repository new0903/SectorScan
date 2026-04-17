
using Microsoft.AspNetCore.Server.Kestrel.Core;
using WebAppCellMapper.Services;
using WebAppLocator.Extensions;
using WebAppLocator.Grpc;

namespace WebAppLocator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("WebAppLocator");
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8082, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });

                options.ListenAnyIP(8083, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Services.AddOptionsSetups()
                .InitDBContext()
                .IncludeServices()
                .AddAuth();

            builder.Services.AddGrpc();


            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.AddGrpc();

            app.MigrateDB();
            app.Run();
        }
    }
}
