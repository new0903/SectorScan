using Microsoft.EntityFrameworkCore;
using WebAppCellMapper.Data;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Extensions
{
    public static class HostExtensions
    {


        public static void MigrateDB(this IHost host)
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
        public static void SeedData(this IHost host)
        {
            using (var serviceScope = host.Services.GetService<IServiceScopeFactory>()?.CreateScope())
            {
                try
                {
                    var service= serviceScope?.ServiceProvider.GetRequiredService<OperatorsService>();
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
