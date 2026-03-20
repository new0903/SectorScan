
using Microsoft.Extensions.Configuration;
using WebAppCellMapper.Extensions;
using WebAppCellMapper.Grpc;
using WebAppCellMapper.Options;

namespace WebAppCellMapper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);



            builder.Services.AddOptionsSetups();


            // Add services to the container.
            builder.Services.AddGrpc();

            builder.Services
                .InitDBContext()
                .IncludeServices();


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

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.AddGrpc();
            app.MapControllers();
            app.MigrateDB();
            app.SeedData();
            app.Run();
        }
    }
}
