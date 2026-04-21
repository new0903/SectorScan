using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using WebAppCellMapper.Data;
using WebAppCellMapper.Grpc;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Extensions
{
    public static class HostExtensions
    {

        public static void AddGrpc(this WebApplication app)
        {

            // Configure the HTTP request pipeline.
            app.MapGrpcService<LocatorGrpcService>();
            app.MapGrpcService<StationGRPCService>();
            app.MapGrpcService<OperatorGrpcService>();
        }

        public static void MigrateDB(this WebApplication host)
        {
            using (var serviceScope = host.Services.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                try
                {
                    serviceScope?.ServiceProvider.GetRequiredService<AppDBContext>().Database.Migrate();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
        public static void SeedData(this WebApplication host)
        {
            using (var serviceScope = host.Services.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                try
                {
                    var service= serviceScope?.ServiceProvider.GetRequiredService<IOperatorsService>();
                    if (service!=null)
                    {
                         service.SaveOperators();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

        }
    }
}
