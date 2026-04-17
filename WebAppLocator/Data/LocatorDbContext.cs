using Domain;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using WebAppLocator.Data.Models;

namespace WebAppLocator.Data
{
    public class LocatorDbContext : BSContext
    {

        public LocatorDbContext(DbContextOptions<LocatorDbContext> options) : base(options) { }

       

        public virtual DbSet<TracePoints> traces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Station>().ToTable("stations", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Operator>().ToTable("operators", t => t.ExcludeFromMigrations());
        }
    }
} /*
         * пока тут пусто но всё ранво может быть пригодится
         * типа запоминать прошлую позицию и как то пытатся делать расчет на основе старых данных
         * надо будет уточнить этот момент
         */
