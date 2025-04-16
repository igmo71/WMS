using System.Text.Json;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class OrderInEventProducer(KafkaProducer kafkaProducer) : IOrderInEventProducer
    {
        private readonly KafkaProducer _kafkaProducer = kafkaProducer;

        public async Task OrderInCreatedEventProduce(OrderIn order, byte[]? correlationId = null)
        {
            var message = JsonSerializer.Serialize(order);

            var topic = KafkaConfiguration.OrderInCreated;

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }

        public async Task OrderInDeletedEventProduce(Guid id, byte[]? correlationId = null)
        {
            var message = JsonSerializer.Serialize(id);

            var topic = KafkaConfiguration.OrderInDeleted;

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }
    }
}
