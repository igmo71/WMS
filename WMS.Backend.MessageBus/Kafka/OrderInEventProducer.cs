using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class OrderInEventProducer : IOrderInEventProducer
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly IProducer<Guid, OrderIn?> _producer;
        private readonly ILogger _log = Log.ForContext<OrderInEventProducer>();

        public OrderInEventProducer(IConfiguration configuration)
        {
            _kafkaConfig = configuration.GetSection(KafkaConfig.Section).Get<KafkaConfig>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                //Debug = "all"
            };

            _producer = new ProducerBuilder<Guid, OrderIn?>(producerConfig)
                .SetKeySerializer(new JsonSerializer<Guid>())
                .SetValueSerializer(new JsonSerializer<OrderIn?>())
                .Build();
        }

        public async Task OrderInCreatedEventProduce(OrderIn order) => 
            await OrderInEventProduce(order, KafkaConfig.OrderInCreated);

        public async Task OrderInUpdatedEventProduce(OrderIn order) => 
            await OrderInEventProduce(order, KafkaConfig.OrderInUpdated);

        public async Task OrderInEventProduce(OrderIn order, string operation)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {Topic} {@OrderIn}",
                nameof(OrderInEventProduce), operation, order);

            var message = new Message<Guid, OrderIn?>() { Key = order.Id, Value = order };

            var deliveryResult = await _producer.ProduceAsync(topic: operation, message);

            activity.AddProperty("{DeliveryResult}", deliveryResult, destructureObjects: true);
        }

        public async Task OrderInDeletedEventProduce(Guid orderId) 
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {Topic} {OrderId}",
                nameof(OrderInEventProduce), KafkaConfig.OrderInDeleted, orderId);

            var message = new Message<Guid, OrderIn?> () { Key = orderId, Value = null };

            var deliveryResult = await _producer.ProduceAsync(topic: KafkaConfig.OrderInDeleted, message);

            activity.AddProperty("{DeliveryResult}", deliveryResult, destructureObjects: true);
        }
    }
}
