using System.Text.Json;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.MessageBus.Abstractions;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka.Documents.Producers
{
    internal class OrderInQueryService(KafkaProducer kafkaProducer) : IOrderInQueryService
    {
        private readonly KafkaProducer _kafkaProducer = kafkaProducer;

        public async Task OrderInGetListQueryProduce(OrderInGetListQuery orderQuery)
        {
            var message = JsonSerializer.Serialize(orderQuery);

            var topic = KafkaConfiguration.OrderInGetListQuery;

            var correlationId = System.Text.Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString());

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }

        public async Task OrderInGetListResponseProduce(List<OrderIn>? orders, byte[]? correlationId = null)
        {
            var message = JsonSerializer.Serialize(orders ?? []);

            var topic = KafkaConfiguration.OrderInGetListResponse;

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }

        public async Task OrderInGetByIdQueryProduce(Guid id)
        {
            var message = JsonSerializer.Serialize(id);

            var topic = KafkaConfiguration.OrderInGetByIdQuery;

            var correlationId = System.Text.Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString());

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }

        public async Task OrderInGetByIdResponseProduce(OrderIn? order, byte[]? correlationId = null)
        {
            var message = JsonSerializer.Serialize(order);

            var topic = KafkaConfiguration.OrderInGetByIdResponse;

            await _kafkaProducer.ProduceAsync(topic, message, correlationId);
        }
    }
}
