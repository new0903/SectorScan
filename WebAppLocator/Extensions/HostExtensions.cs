

using Microsoft.EntityFrameworkCore;
using WebAppLocator.Data;
using WebAppLocator.Grpc;

namespace WebAppLocator.Extensions
{
    public static class HostExtensions
    {

        public static void AddGrpc(this WebApplication app)
        {

            // Configure the HTTP request pipeline.
            app.MapGrpcService<LocatorGrpcService>();
        }

        public static void MigrateDB(this WebApplication host)
        {
            using (var serviceScope = host.Services.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                try
                {
                    serviceScope?.ServiceProvider.GetRequiredService<LocatorDbContext>().Database.Migrate();

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
