using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaEventProducer<TEntity> : IEventProducer<TEntity> where TEntity : EntityBase
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly IProducer<Guid, TEntity?> _producer;
        private readonly ILogger _log = Log.ForContext<KafkaEventProducer<TEntity>>();

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

            _producer = new ProducerBuilder<Guid, TEntity?>(producerConfig)
                .SetKeySerializer(new JsonSerializer<Guid>())
                .SetValueSerializer(new JsonSerializer<TEntity?>())
                .Build();
        }

        public async Task CreatedEventProduce(TEntity entity)
        {
            var topic = $"{typeof(TEntity).Name}{KafkaConfig.Created}";
            var message = new Message<Guid, TEntity?>() { Key = entity.Id, Value = entity };
            await EventProduce(topic, message);
        }

        public async Task UpdatedEventProduce(TEntity entity)
        {
            var topic = $"{typeof(TEntity).Name}{KafkaConfig.Updated}";
            var message = new Message<Guid, TEntity?>() { Key = entity.Id, Value = entity };
            await EventProduce(topic, message);
        }

        public async Task DeletedEventProduce(Guid id)
        {
            var topic = $"{typeof(TEntity).Name}{KafkaConfig.Deleted}";
            var message = new Message<Guid, TEntity?>() { Key = id, Value = null };
            await EventProduce(topic, message);
        }

        private async Task EventProduce(string topic, Message<Guid, TEntity?> message)
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
