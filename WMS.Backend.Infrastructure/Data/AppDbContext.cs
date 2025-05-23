using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;
using WMS.Backend.Domain.Models.Catalogs;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
    {
        #region Catalogs

        public DbSet<Product> Products { get; set; }

        //public DbSet<Warehouse> Warehouses { get; set; }

        #endregion

        #region Documents

        public DbSet<OrderIn> OrdersIn { get; set; }
        public DbSet<OrderInArchive> OrdersInArchive { get; set; }
        public DbSet<OrderInProduct> OrderInProducts { get; set; }

        //public DbSet<OrderOut> OrdersOut { get; set; }
        //public DbSet<OrderOutProduct> OrderOutProducts { get; set; }        

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Catalogs

            modelBuilder.Entity<Product>().HasKey(e => e.Id);
            modelBuilder.Entity<Product>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);

            modelBuilder.Entity<Warehouse>().HasKey(e => e.Id);
            modelBuilder.Entity<Warehouse>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);

            #endregion

            #region Documents

            modelBuilder.Entity<OrderIn>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderIn>().Property(e => e.Number).HasMaxLength(AppSettings.NUMBER_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);
            modelBuilder.Entity<OrderIn>().Property(e => e.DateTime)
                .HasConversion(
                    dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                    dt => dt.ToLocalTime());
            modelBuilder.Entity<OrderIn>().HasMany(e => e.Products).WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId).HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderInArchive>().HasKey(e => e.Id);
            modelBuilder.Entity<OrderInArchive>().Property(e => e.DateTime)
                .HasConversion(
                    dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                    dt => dt.ToLocalTime());

            modelBuilder.Entity<OrderInProduct>().HasKey(e => new { e.OrderId, e.ProductId });
            modelBuilder.Entity<OrderInProduct>().HasOne(e => e.Product).WithMany()
                .HasForeignKey(e => e.ProductId).HasPrincipalKey(e => e.Id)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<OrderOut>().HasKey(e => e.Id);
            //modelBuilder.Entity<OrderOut>().Property(e => e.Number).HasMaxLength(AppSettings.NUMBER_MAX_LENGTH);
            //modelBuilder.Entity<OrderOut>().Property(e => e.Name).HasMaxLength(AppSettings.NAME_MAX_LENGTH);
            //modelBuilder.Entity<OrderOut>().Property(e => e.DateTime).HasConversion(
            //    dt => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
            //    dt => dt.ToLocalTime());
            //modelBuilder.Entity<OrderOut>().HasMany(e => e.Products).WithOne()
            //    .HasForeignKey(e => e.OrderId).HasPrincipalKey(e => e.Id);

            //modelBuilder.Entity<OrderOutProduct>().HasKey(e => new { e.OrderId, e.ProductId });

            #endregion
        }

        public override int SaveChanges()
        {
            UpdateDataVersion();
            return base.SaveChanges(); 
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateDataVersion();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateDataVersion()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            var entries = ChangeTracker.Entries<IHasDataVersion>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                entry.Entity.DataVersion = nowTicks;
            }
        }
    }
}
