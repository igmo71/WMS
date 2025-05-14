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
                CancellationDelayMaxMs = 1000
                //Debug = "all"
            };

            _producer = new ProducerBuilder<Guid, Dto.OrderIn?>(producerConfig)
                .SetKeySerializer(new JsonSerializer<Guid>())
                .SetValueSerializer(new JsonSerializer<Dto.OrderIn?>())
                .Build();
        }

        public async Task CreatedEventProduce(Dto.OrderIn orderDto)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderDto.Id, Value = orderDto };

            await EventProduce(KafkaConfig.OrderInCreated, message);
        }

        public async Task UpdatedEventProduce(Dto.OrderIn orderDto)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderDto.Id, Value = orderDto };

            await EventProduce(KafkaConfig.OrderInUpdated, message);
        }

        public async Task DeletedEventProduce(Guid orderId)
        {
            var message = new Message<Guid, Dto.OrderIn?>() { Key = orderId, Value = null };

            await EventProduce(KafkaConfig.OrderInDeleted, message);
        }

        private async Task EventProduce(string topic, Message<Guid, Dto.OrderIn?> message)
        {
            var cts = new CancellationTokenSource(1000);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic, message, cts.Token);

                _log.Debug("{Source} {Topic} {Message}", nameof(EventProduce), topic, message);
            }
            catch (TaskCanceledException ex)
            {
                _log.Error(ex, "{Source} {Topic} {Message}", nameof(EventProduce), topic, message);
            }
        }
    }
}
