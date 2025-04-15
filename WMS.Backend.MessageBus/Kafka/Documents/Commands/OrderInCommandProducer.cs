using System.Text.Json;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.MessageBus.Abstractions;

namespace WMS.Backend.MessageBus.Kafka.Documents.Commands
{
    internal class OrderInCommandProducer(KafkaProducer kafkaProducer) : IOrderInCommandProducer
    {
        private readonly KafkaProducer _kafkaProducer = kafkaProducer;

        public async Task OrderInCreateCommandProduce(OrderInCreateCommand createOrderCommand)
        {
            var message = JsonSerializer.Serialize(createOrderCommand);

            var topic = KafkaConfiguration.OrderInCreateCommand;

            var correlationId = System.Text.Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString());

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }

        public async Task OrderInDeleteCommandProduce(Guid id)
        {
            var message = JsonSerializer.Serialize(id);

            var topic = KafkaConfiguration.OrderInDeleteCommand;

            var correlationId = System.Text.Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString());

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }
    }
}
