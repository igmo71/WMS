using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;

namespace WMS.Backend.MessageBus.Kafka.Documents.Commands
{
    internal class OrderInCreateCommandConsumer : BackgroundService
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInCreateCommandConsumer>();
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceProvider _services;

        public OrderInCreateCommandConsumer(IConfiguration configuration, IServiceProvider services)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration.BootstrapServers,
                GroupId = $"{nameof(OrderInCreateCommandConsumer)}Group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(AppConfig.BACKGROUND_SERVICE_DELAY, stoppingToken);

            _log.Information("{Consumer} Start", nameof(OrderInCreateCommandConsumer));

            var topic = KafkaConfiguration.OrderInCreateCommand;

            _consumer.Subscribe(topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessMessage(stoppingToken);
            }

            _consumer.Close();
        }

        private async Task ProcessMessage(CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                var message = consumeResult.Message.Value;

                var correlationId = consumeResult.Message.Headers.GetLastBytes(AppConfig.CORRELATION_ID) ;

                _consumer.Commit(consumeResult);

                _log.Debug("{Source} {Message} {CorrelationId}", nameof(ProcessMessage), message, correlationId);

                await CreateOrder(message, correlationId);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "{Source} {Message}", nameof(ProcessMessage), ex.Message);
                throw;
            }
        }

        private async Task CreateOrder(string message, byte[]? correlationId)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {CorrelationId}", nameof(CreateOrder), correlationId);

            var createOrderCommand = JsonSerializer.Deserialize<OrderInCreateCommand>(message);

            activityListener.AddProperty("{CreateOrderInCommand}", createOrderCommand, destructureObjects: true);

            if (createOrderCommand is null)
                return;

            using var scope = _services.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderInService>();

            var orderIn = await orderService.CreateOrderAsync(createOrderCommand, correlationId);

            activityListener.AddProperty("{OrderIn}", orderIn, destructureObjects: true);
        }
    }
}
