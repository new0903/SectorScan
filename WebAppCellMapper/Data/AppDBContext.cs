using Microsoft.EntityFrameworkCore;
using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {

        }

        public DbSet<Station> stations { get; set; }
        public DbSet<Operator> operators { get; set; }
        //public DbSet<Country> countries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Station>(p =>
            {
                p.Property(p => p.Standard).HasConversion<string>();
                p.HasIndex(p=>p.Standard);
            });
        }
    }
}
