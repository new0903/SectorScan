using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public abstract class BSContext : DbContext
    {
        public BSContext(DbContextOptions options) : base(options) { }
        public virtual DbSet<Station> stations { get; set; }
        public virtual DbSet<Operator> operators { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Station>(p =>
            {
                p.Property(p => p.Standard).HasConversion<string>();
            });
           
        }
    }
}
