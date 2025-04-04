using Microsoft.EntityFrameworkCore;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderIn> OrdersIn { get; set; }
        public DbSet<OrderInProducts> OrderInProducts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasKey(e => e.Id);
            modelBuilder.Entity<Order>().Property(e => e.Number).HasMaxLength(AppSettings.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<Order>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);

            modelBuilder.Entity<OrderIn>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderIn>().Property(e => e.Number).HasMaxLength(AppSettings.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().HasMany(e => e.Products).WithOne()
                .HasForeignKey(e => e.OrderId).HasPrincipalKey(e => e.Id);

            modelBuilder.Entity<OrderInProducts>().HasKey(e => e.Id);

            modelBuilder.Entity<Product>().HasKey(e => e.Id);
            modelBuilder.Entity<Product>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);

            modelBuilder.Entity<Warehouse>().HasKey(e => e.Id);
            modelBuilder.Entity<Warehouse>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);
        }
    }

}
