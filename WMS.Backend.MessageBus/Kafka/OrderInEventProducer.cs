using System.Text.Json;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class OrderInEventProducer(KafkaProducer kafkaProducer) : IOrderInEventProducer
    {
        private readonly KafkaProducer _kafkaProducer = kafkaProducer;

        public async Task OrderInCreatedEventProduce(OrderIn order)
        {
            await _kafkaProducer.ProduceAsync(KafkaConfig.OrderInCreated,
                message: JsonSerializer.Serialize(order, AppConfig.JsonSerializerOptions));
        }

        public async Task OrderInUpdatedEventProduce(OrderIn order)
        {
            await _kafkaProducer.ProduceAsync(KafkaConfig.OrderInUpdated,
                message: JsonSerializer.Serialize(order, AppConfig.JsonSerializerOptions));
        }

        public async Task OrderInDeletedEventProduce(Guid id)
        {
            await _kafkaProducer.ProduceAsync(KafkaConfig.OrderInDeleted,
                message: id.ToString());
        }
    }
}
