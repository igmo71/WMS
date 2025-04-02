using Microsoft.EntityFrameworkCore;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;
using WMS.Shared.Abstractions.Models;

namespace WMS.Backend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasKey(e => e.Id);
            modelBuilder.Entity<Order>().Property(e => e.Number).HasMaxLength(AppSettings.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<Order>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);
        }
    }

}
