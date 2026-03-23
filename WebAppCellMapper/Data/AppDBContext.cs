using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Data
{
    public class AppDBContext : DbContext, IHealthCheck
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {

        }

        public virtual DbSet<Station> stations { get; set; }
        public virtual DbSet<Operator> operators { get; set; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            try
            {
                var canConnect = await Database.CanConnectAsync(cancellationToken);

                if (canConnect)
                {
                    return HealthCheckResult.Healthy("ok");
                }
                return HealthCheckResult.Unhealthy("error");

            }
            catch (Exception ex)
            {
                
                return HealthCheckResult.Unhealthy(ex.Message,ex);
            }
        }

        //public DbSet<Country> countries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Station>(p =>
            {
                p.Property(p => p.Standard).HasConversion<string>();
             //   p.HasIndex(p=>p.Standard);
            });
        }
    }
}
