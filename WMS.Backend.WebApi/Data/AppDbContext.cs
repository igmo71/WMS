using Microsoft.EntityFrameworkCore;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.WebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Order { get; set; } = default!;
    }
}
