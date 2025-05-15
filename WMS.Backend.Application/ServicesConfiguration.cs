using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Cache;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services;
using WMS.Backend.Application.Services.OrderInServices;
using WMS.Backend.Application.Services.ProductServices;

namespace WMS.Backend.Application
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            serviceCollection.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Configuration"];
                options.InstanceName = nameof(WMS.Backend);
            });

            serviceCollection.AddScoped<IAppCache, AppCache>();

            serviceCollection.AddScoped<IOrderInService, OrderInService>();
            serviceCollection.AddScoped<IOrderInService, OrderInService>();
            serviceCollection.AddScoped<IProductService, ProductService>();

            return serviceCollection;
        }
    }
}
