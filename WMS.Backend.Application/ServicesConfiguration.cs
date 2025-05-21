using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Cache;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services;
using WMS.Backend.Application.Services.OrderInServices;
using WMS.Backend.Application.Services.ProductServices;

namespace WMS.Backend.Application
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            //var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Configuration"];
                options.InstanceName = nameof(WMS.Backend);
            });

            services.AddScoped<IAppCache, AppCache>();

            services.AddSingleton<OrderInEventChannel>();
            services.AddSingleton<IOrderInEventProducer, OrderInEventProducer>();
            services.AddHostedService<OrderInEventBus>();

            services.AddScoped<IOrderInEventSubscriber, OrderInLogSubscriber>();

            services.AddScoped<IOrderInService, OrderInService>();
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}
