using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.MessageBus.Abstractions;

namespace WMS.Backend.MessageBus.Kafka.Documents
{
    internal class OrderInCommandProducer(IConfiguration configuration, KafkaProducer kafkaProducer) : IOrderInCommandProducer
    {
        private readonly KafkaConfiguration _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

        private readonly KafkaProducer _kafkaProducer = kafkaProducer;


        public async Task CreateOrderCommandProduce(OrderInCreateCommand createOrderCommand)
        {
            var message = JsonSerializer.Serialize(createOrderCommand);

            var topic = _configuration.Topics[nameof(OrderInCreateCommand)];

            await _kafkaProducer.ProduceAsync(topic, message);
        }
    }
}
