using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using System.Text.Json;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.MessageBus.Abstractions;

namespace WMS.Backend.MessageBus.Kafka.Documents
{
    public class OrderInCommandProducer : IOrderInCommandProducer
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInCommandProducer>();
        private readonly IProducer<Null, string> _producer;

        public OrderInCommandProducer(IConfiguration configuration)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.BootstrapServers
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public async Task CreateOrderCommandProduce(OrderInCreateCommand createOrderCommand)
        {
            var message = JsonSerializer.Serialize(createOrderCommand);

            var topic = _configuration.Topics[nameof(OrderInCreateCommand)];

            await ProduceAsync(topic, message);
        }

        private async Task ProduceAsync(string topic, string message)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {Topic} (Message)", nameof(ProduceAsync), topic, message);

            var kafkaMessage = new Message<Null, string>() { Value = message };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

            activityListener.AddProperty("{DeliveryResult}", deliveryResult, destructureObjects: true);
        }
    }
}
