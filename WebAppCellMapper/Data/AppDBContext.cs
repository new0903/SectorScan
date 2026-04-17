using Domain;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Data
{
    public class AppDBContext : BSContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) {     }

        public virtual DbSet<OperatorProgress> progresses { get; set; }
        public virtual DbSet<AppRuntimeState> runtime { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<OperatorProgress>(entity =>
            {
                entity.Property(p => p.Standard).HasConversion<string>();
                entity.Property(p => p.Status).HasConversion<string>();
            });
        }
    }
}
