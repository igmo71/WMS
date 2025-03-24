using Microsoft.EntityFrameworkCore;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasKey(e => e.Id);
            modelBuilder.Entity<Order>().Property(e => e.Number).HasMaxLength(50);
            modelBuilder.Entity<Order>().Property(e => e.Name).HasMaxLength(100);
        }
    }

}
