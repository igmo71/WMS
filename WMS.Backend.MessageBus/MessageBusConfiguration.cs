using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.MessageBus.Kafka;

namespace WMS.Backend.MessageBus
{
    public static class MessageBusConfiguration
    {
        public static IServiceCollection AddAppMessageBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IOrderInEventProducer, OrderInEventProducer>();

            return services;
        }
    }
}
