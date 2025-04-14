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

namespace WMS.Backend.MessageBus.Kafka.Documents
{
    internal class OrderInCreateCommandConsumer : BackgroundService
    {
        private readonly KafkaConfiguration _configuration;
        private readonly ILogger _log = Log.ForContext<OrderInCreateCommandConsumer>();
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceProvider _services;
        //private readonly IOrderInService _orderService;

        public OrderInCreateCommandConsumer(
            IConfiguration configuration,
            IServiceProvider services
            /*IOrderInService orderInService*/)
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
            //_orderService = orderInService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000, stoppingToken);

            _log.Information("{Consumer} Start", nameof(OrderInCreateCommandConsumer));

            var topic = _configuration.Topics[nameof(OrderInCreateCommand)];

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
                using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source}", nameof(ProcessMessage));

                var consumeResult = _consumer.Consume(stoppingToken);

                var message = consumeResult.Message.Value;

                _consumer.Commit(consumeResult);

                activityListener.AddProperty("{Message}", message);

                bool flowControl = await CreateOrder(message);
                
                if (!flowControl)
                {
                    return;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<bool> CreateOrder(string message)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source}", nameof(CreateOrder));

            var createOrderCommand = JsonSerializer.Deserialize<OrderInCreateCommand>(message);

            activityListener.AddProperty("{CreateOrderInCommand}", createOrderCommand, destructureObjects: true);

            if (createOrderCommand is null)
            {
                return false;
            }

            using var scope = _services.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderInService>();

            var orderIn = await orderService.CreateOrderAsync(createOrderCommand);

            activityListener.AddProperty("{OrderIn}", orderIn, destructureObjects: true);

            return true;
        }
    }
}
