using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Common;
using Dto = WMS.Shared.Models;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaEventProducer<T> : IEventProducer<T> where T : Dto.EntityBase
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly IProducer<Guid, T?> _producer;
        private readonly ILogger _log = Log.ForContext<KafkaEventProducer<T>>();

        public KafkaEventProducer(IConfiguration configuration)
        {
            _kafkaConfig = configuration.GetSection(KafkaConfig.Section).Get<KafkaConfig>()
               ?? throw new InvalidOperationException("Kafka Configuration Not Found");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                CancellationDelayMaxMs = 1000
                //Debug = "all"
            };

            _producer = new ProducerBuilder<Guid, T?>(producerConfig)
                .SetKeySerializer(new JsonSerializer<Guid>())
                .SetValueSerializer(new JsonSerializer<T?>())
                .Build();
        }

        public async Task CreatedEventProduce(T entity)
        {
            var topic = $"{typeof(T).Name}{AppSettings.Events.Created}";
            var message = new Message<Guid, T?>() { Key = entity.Id, Value = entity };
            await EventProduce(topic, message);
        }

        public async Task UpdatedEventProduce(T entity)
        {
            var topic = $"{typeof(T).Name}{AppSettings.Events.Updated}";
            var message = new Message<Guid, T?>() { Key = entity.Id, Value = entity };
            await EventProduce(topic, message);
        }

        public async Task DeletedEventProduce(Guid id)
        {
            var topic = $"{typeof(T).Name}{AppSettings.Events.Deleted}";
            var message = new Message<Guid, T?>() { Key = id, Value = null };
            await EventProduce(topic, message);
        }

        private async Task EventProduce(string topic, Message<Guid, T?> message)
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
