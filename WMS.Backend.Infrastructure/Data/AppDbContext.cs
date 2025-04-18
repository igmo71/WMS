using Microsoft.EntityFrameworkCore;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Catalogs;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<OrderIn> OrdersIn { get; set; }
        public DbSet<OrderInProduct> OrderInProducts { get; set; }
        public DbSet<OrderInArchive> OrdersInArchive { get; set; }
        
        public DbSet<OrderOut> OrdersOut { get; set; }
        public DbSet<OrderOutProduct> OrderOutProducts { get; set; }
        
        public DbSet<Product> Products { get; set; }
        
        public DbSet<Warehouse> Warehouses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderIn>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderIn>().Property(e => e.Number).HasMaxLength(AppConfig.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().Property(e => e.Name).HasMaxLength(AppConfig.NAME_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().Property(e => e.DateTime).HasConversion(
                dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                dt => dt.ToLocalTime());
            modelBuilder.Entity<OrderIn>().HasMany(e => e.Products).WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId).HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderInProduct>().HasKey(e => new { e.OrderId, e.ProductId });
            modelBuilder.Entity<OrderInProduct>().HasOne(e => e.Product).WithMany()
                .HasForeignKey(e => e.ProductId).HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderInArchive>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderInArchive>().Property(e => e.DateTime).HasConversion(
                dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                dt => dt.ToLocalTime());

            modelBuilder.Entity<OrderOut>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderOut>().Property(e => e.Number).HasMaxLength(AppConfig.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<OrderOut>().Property(e => e.Name).HasMaxLength(AppConfig.NAME_MAX_LENGTH);
            modelBuilder.Entity<OrderOut>().Property(e => e.DateTime).HasConversion(
                dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                dt => dt.ToLocalTime());
            modelBuilder.Entity<OrderOut>().HasMany(e => e.Products).WithOne()
                .HasForeignKey(e => e.OrderId).HasPrincipalKey(e => e.Id);

            modelBuilder.Entity<OrderOutProduct>().HasKey(e => new { e.OrderId, e.ProductId });

            modelBuilder.Entity<Product>().HasKey(e => e.Id);
            modelBuilder.Entity<Product>().Property(e => e.Name).HasMaxLength(AppConfig.NAME_MAX_LENGTH);

            modelBuilder.Entity<Warehouse>().HasKey(e => e.Id);
            modelBuilder.Entity<Warehouse>().Property(e => e.Name).HasMaxLength(AppConfig.NAME_MAX_LENGTH);
        }
    }
}
