using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Application.Services.ProductServices;

namespace WMS.Backend.Application
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddScoped<IOrderService, OrderService>();
            serviceCollection.AddScoped<IProductService, ProductService>();

            return serviceCollection;
        }
    }
}
