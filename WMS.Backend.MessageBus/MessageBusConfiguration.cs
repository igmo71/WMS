using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Backend.MessageBus.Kafka;

namespace WMS.Backend.MessageBus
{
    public static class MessageBusConfiguration
    {
        public static IServiceCollection AddAppMessageBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<KafkaProducer>();

            services.AddSingleton<IOrderInEventProducer, OrderInEventProducer>();

            return services;
        }
    }
}
