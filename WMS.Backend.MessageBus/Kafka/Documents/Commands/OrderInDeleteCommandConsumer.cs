using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Common;

namespace WMS.Backend.MessageBus.Kafka.Documents.Commands
{
    internal class OrderInDeleteCommandConsumer : BackgroundService
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInDeleteCommandConsumer>();
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceProvider _services;

        public OrderInDeleteCommandConsumer(IConfiguration configuration, IServiceProvider services)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration.BootstrapServers,
                GroupId = $"{nameof(OrderInDeleteCommandConsumer)}Group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                //EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(AppConfig.BACKGROUND_SERVICE_DELAY, stoppingToken);

            _log.Information("{Consumer} Start", nameof(OrderInDeleteCommandConsumer));

            var topic = KafkaConfiguration.OrderInDeleteCommand;

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

                var correlationId = consumeResult.Message.Headers.GetLastBytes(AppConfig.CORRELATION_ID);

                //_consumer.Commit(consumeResult);

                _log.Debug("{Source} {Message} {CorrelationId}", nameof(ProcessMessage), message, correlationId);

                await DeleteOrder(message, correlationId);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "{Source} {Message}", nameof(ProcessMessage), ex.Message);
                throw;
            }
        }

        private async Task DeleteOrder(string message, byte[]? correlationId)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {CorrelationId}", nameof(DeleteOrder), correlationId);

            var id = JsonSerializer.Deserialize<Guid>(message);

            activityListener.AddProperty("{Id}", id);            

            using var scope = _services.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderInService>();

            await orderService.DeleteOrderAsync(id, correlationId);
        }
    }
}
