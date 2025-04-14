using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Backend.MessageBus.Abstractions;
using WMS.Backend.MessageBus.Kafka;
using WMS.Backend.MessageBus.Kafka.Documents;

namespace WMS.Backend.MessageBus
{
    public static class MessageBusConfiguration
    {
        public static IServiceCollection AddAppMessageBus ( this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<KafkaProducer>();            
            services.AddSingleton<IOrderInCommandProducer, OrderInCommandProducer>();
            services.AddSingleton<IOrderInEventProducer, OrderInEventProducer>();
            services.AddHostedService<OrderInCreateCommandConsumer>();

            return services;
        }
    }
}
