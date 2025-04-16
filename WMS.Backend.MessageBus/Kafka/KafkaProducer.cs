using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using System.Text;
using WMS.Backend.Common;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaProducer
    {
        private readonly KafkaConfig _configuration;
        private readonly ILogger _log = Log.ForContext<KafkaProducer>();
        private readonly IProducer<Null, string> _producer;
        private readonly ICorrelationContext _correlationContext;

        public KafkaProducer(IConfiguration configuration, ICorrelationContext correlationContext)
        {
            _configuration = configuration.GetSection(KafkaConfig.Section).Get<KafkaConfig>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            _correlationContext = correlationContext;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.BootstrapServers,
                //Debug = "all"
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        internal async Task ProduceAsync(string topic, string? message)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {Topic} {Message}", nameof(ProduceAsync), topic, message);

            var kafkaMessage = new Message<Null, string>() { Value = message ?? AppConfig.NO_DATA };

            var correlationId = Encoding.UTF8.GetBytes(_correlationContext.CorrelationId);

            kafkaMessage.Headers = new Headers { { AppConfig.CORRELATION_HEADER, correlationId } };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

            activityListener.AddProperty("{DeliveryResult}", deliveryResult, destructureObjects: true);
        }
    }
}
