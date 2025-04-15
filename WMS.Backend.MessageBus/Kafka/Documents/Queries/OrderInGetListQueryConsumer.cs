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
using WMS.Backend.MessageBus.Abstractions;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Kafka.Documents.Queries
{
    internal class OrderInGetListQueryConsumer : BackgroundService
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInGetListQueryConsumer>();
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceProvider _services;
        private readonly IOrderInQueryService _orderQueryService;

        public OrderInGetListQueryConsumer(
            IConfiguration configuration, 
            IServiceProvider services,
            IOrderInQueryService orderQueryService)
        {
            _configuration = configuration.GetSection(KafkaConfiguration.Section).Get<KafkaConfiguration>()
                ?? throw new ApplicationException("Kafka Configuration Not Found");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration.BootstrapServers,
                GroupId = $"{nameof(OrderInGetListQueryConsumer)}Group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            _services = services;
            _orderQueryService = orderQueryService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(AppConfig.BACKGROUND_SERVICE_DELAY, stoppingToken);

            _log.Information("{Consumer} Start", nameof(OrderInGetListQueryConsumer));

            var topic = KafkaConfiguration.OrderInGetListQuery;

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

                var orders = await GetOrderList(message);

                await _orderQueryService.OrderInGetListResponseProduce(orders, correlationId);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "{Source} {Message}", nameof(ProcessMessage), ex.Message);
                throw;
            }
        }

        private async Task<List<OrderIn>?> GetOrderList(string message)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source}", nameof(GetOrderList));

            var orderInGetListQuery = JsonSerializer.Deserialize<OrderInGetListQuery>(message);

            activityListener.AddProperty("{OrderInGetListQuery}", orderInGetListQuery, destructureObjects: true);

            using var scope = _services.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderInService>();

            if(orderInGetListQuery is null)
                return default;

            var orders = await orderService.GetOrderListAsync(orderInGetListQuery);

            activityListener.AddProperty("{Orders}", orders, destructureObjects: true);

            return orders;
        }
    }
}
