using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Channels;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Domain.Models.Documents;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInEventBus(
        IAppHubService<Dto.OrderIn> eventHub, 
        IServiceScopeFactory scopeFactory) 
        : BackgroundService, IEventProducer<OrderIn>
    {
        private readonly ILogger _log = Log.ForContext<OrderInEventBus>();
        private readonly Channel<IAppEvent> _channel = Channel.CreateUnbounded<IAppEvent>();
        private readonly IAppHubService<Dto.OrderIn> _eventHub = eventHub;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        // Producer methods
        public async Task CreatedEventProduce(OrderIn entity)
            => await _channel.Writer.WriteAsync(new CreatedEvent<OrderIn>(entity));
        public async Task UpdatedEventProduce(OrderIn entity)
            => await _channel.Writer.WriteAsync(new UpdatedEvent<OrderIn>(entity));
        public async Task DeletedEventProduce(Guid id)
            => await _channel.Writer.WriteAsync(new DeletedEvent(id));

        // Consumer (background processing)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Information("OrderInEventBus started");
            await foreach (IAppEvent appEvent in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _log.Information("Event received: {EventType}", appEvent.GetType().Name);

                using var scope = _scopeFactory.CreateScope();
                var subscribers = scope.ServiceProvider.GetServices<IOrderInEventSubscriber>();

                try
                {
                    switch (appEvent)
                    {
                        case CreatedEvent<OrderIn> createdEvent:
                            await Task.WhenAll(subscribers.Select(s => s.OnCreatedAsync(createdEvent.Value)));
                            await _eventHub.CreatedAsync(OrderInMapping.ToDto(createdEvent.Value));
                            break;
                        case UpdatedEvent<OrderIn> updatedEvent:
                            await Task.WhenAll(subscribers.Select(s => s.OnUpdatedAsync(updatedEvent.Value)));
                            await _eventHub.UpdatedAsync(OrderInMapping.ToDto(updatedEvent.Value));
                            break;
                        case DeletedEvent deletedEvent:
                            await Task.WhenAll(subscribers.Select(s => s.OnDeletedAsync(deletedEvent.Id)));
                            await _eventHub.DeletedAsync(deletedEvent.Id);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error processing event: {EventType}", appEvent.GetType().Name);
                }
            }
        }
    }
}
