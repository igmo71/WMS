using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.MessageBus.Abstractions;
using WMS.Backend.MessageBus.Kafka.Documents.OrderIn;

namespace WMS.Backend.MessageBus
{
    public static class MessageBusConfiguration
    {
        public static IServiceCollection AddAppMessageBus ( this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService<OrderInCreateCommandConsumer>();
            services.AddSingleton<IOrderInCommandProducer, OrderInCommandProducer>();

            return services;
        }
    }
}
