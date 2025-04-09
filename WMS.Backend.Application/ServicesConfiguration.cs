using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Application.Services.ProductServices;
using WMS.Backend.Common;

namespace WMS.Backend.Application
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {

            var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            serviceCollection.AddScoped<IOrderInService, OrderInService>();
            serviceCollection.AddScoped<IProductService, ProductService>();

            return serviceCollection;
        }
    }
}
