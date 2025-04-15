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
using WMS.Backend.MessageBus.Abstractions;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka.Documents.Queries
{
    internal class OrderInGetByIdQueryConsumer : BackgroundService
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInGetByIdQueryConsumer>();
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceProvider _services;
        private readonly IOrderInQueryService _orderQueryService;

        public OrderInGetByIdQueryConsumer(
            IConfiguration configuration, 
            IServiceProvider services,
            IOrderInQueryService orderQueryService)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration.BootstrapServers,
                GroupId = $"{nameof(OrderInGetByIdQueryConsumer)}Group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            _services = services;
            _orderQueryService = orderQueryService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(AppConfig.BACKGROUND_SERVICE_DELAY, stoppingToken);

            _log.Information("{Consumer} Start", nameof(OrderInGetByIdQueryConsumer));

            var topic = KafkaConfiguration.OrderInGetByIdQuery;

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

                var order = await GetOrderById(message);

                await _orderQueryService.OrderInGetByIdResponseProduce(order, correlationId);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "{Source} {Message}", nameof(ProcessMessage), ex.Message);
                throw;
            }
        }

        private async Task<OrderIn?> GetOrderById(string message)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source}", nameof(GetOrderById));

            var id = JsonSerializer.Deserialize<Guid>(message);

            activityListener.AddProperty("{Id}", id);

            using var scope = _services.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderInService>();

            var order = await orderService.GetOrderByIdAsync(id);

            activityListener.AddProperty("{Order}", order, destructureObjects: true);

            return order;
        }
    }
}
