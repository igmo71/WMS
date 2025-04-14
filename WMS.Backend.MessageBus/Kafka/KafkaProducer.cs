using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SerilogTracing;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaProducer
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<KafkaProducer>();
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(IConfiguration configuration)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.BootstrapServers
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        internal async Task ProduceAsync(string topic, string message, byte[]? correlationId = null)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {Topic} {Message} {CorrelationId}", 
                nameof(ProduceAsync), topic, message, correlationId);

            var kafkaMessage = new Message<Null, string>() { Value = message };

            if (correlationId is not null)
                kafkaMessage.Headers = new Headers
                {
                    { "СorrelationId", correlationId }
                };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

            activityListener.AddProperty("{DeliveryResult}", deliveryResult, destructureObjects: true);
        }
    }
}
