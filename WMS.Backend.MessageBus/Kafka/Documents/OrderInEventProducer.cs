using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka.Documents
{
    internal class OrderInEventProducer(IConfiguration configuration, KafkaProducer kafkaProducer) : IOrderInEventProducer
    {
        private readonly KafkaConfiguration _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

        private readonly KafkaProducer _kafkaProducer = kafkaProducer;

        public async Task OrderCreatedEventProduce(OrderIn orderIn)
        {
            var message = JsonSerializer.Serialize(orderIn);

            var topic = _configuration.Topics["OrderInCreatedEvent"];

            await _kafkaProducer.ProduceAsync(topic, message);
        }
    }
}
