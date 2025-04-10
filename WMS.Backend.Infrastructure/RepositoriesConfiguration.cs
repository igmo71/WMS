using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Common;
using WMS.Backend.Infrastructure.Data;
using WMS.Backend.Infrastructure.Repositories;

namespace WMS.Backend.Infrastructure
{
    public static class RepositoriesConfiguration
    {
        public static IServiceCollection AddAppRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            var connectionString = configuration.GetConnectionString("AppDbContext")
                    ?? throw new InvalidOperationException("Connection string 'AppDbContext' not found.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.AddScoped<IOrderInRepository, OrderInRepository>();
            services.AddScoped<IOrderInProductRepository, OrderInProductRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();

            return services;
        }
    }
}
