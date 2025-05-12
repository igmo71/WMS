using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using WMS.Backend.Application.Abstractions.EventBus;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaOrderInEventProducer : IOrderInEventProducer
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly IProducer<Guid, Dto.OrderIn?> _producer;
        private readonly ILogger _log = Log.ForContext<KafkaOrderInEventProducer>();

        public KafkaOrderInEventProducer(IConfiguration configuration)
        {
            _kafkaConfig = configuration.GetSection(KafkaConfig.Section).Get<KafkaConfig>()
                ?? throw new InvalidOperationException("Kafka Configuration Not Found");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                //Debug = "all"
            };

            _producer = new ProducerBuilder<Guid, Dto.OrderIn?>(producerConfig)
                .SetKeySerializer(new JsonSerializer<Guid>())
                .SetValueSerializer(new JsonSerializer<Dto.OrderIn?>())
                .Build();
        }

        public async Task OrderCreatedEventProduce(Dto.OrderIn orderDto)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderDto.Id, Value = orderDto };
            var cts = new CancellationTokenSource(1000);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic: KafkaConfig.OrderInCreated, message, cts.Token);

                _log.Debug("{Source} {Topic} {@OrderIn}", nameof(OrderCreatedEventProduce), KafkaConfig.OrderInCreated, orderDto);
            }
            catch (TaskCanceledException ex)
            {
                _log.Error(ex, "{Source} {Topic} {@OrderIn}", nameof(OrderCreatedEventProduce), KafkaConfig.OrderInCreated, orderDto);
            }
        }

        public async Task OrderUpdatedEventProduce(Dto.OrderIn orderDto)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderDto.Id, Value = orderDto };
            var cts = new CancellationTokenSource(1000);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic: KafkaConfig.OrderInUpdated, message, cts.Token);

                _log.Debug("{Source} {Topic} {@OrderIn}", nameof(OrderCreatedEventProduce), KafkaConfig.OrderInUpdated, orderDto);
            }
            catch (TaskCanceledException ex)
            {
                _log.Error(ex, "{Source} {Topic} {@OrderIn}", nameof(OrderCreatedEventProduce), KafkaConfig.OrderInUpdated, orderDto);
            }
        }

        public async Task OrderDeletedEventProduce(Guid orderId)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderId, Value = null };
            var cts = new CancellationTokenSource(1000);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic: KafkaConfig.OrderInDeleted, message, cts.Token);

                _log.Debug("{Source} {Topic} {OrderId}", nameof(OrderDeletedEventProduce), KafkaConfig.OrderInUpdated, orderId);
            }
            catch (TaskCanceledException ex)
            {
                _log.Error(ex, "{Source} {Topic} {OrderId}", nameof(OrderDeletedEventProduce), KafkaConfig.OrderInUpdated, orderId);
            }
        }
    }
}
