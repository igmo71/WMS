using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInEventBus(
        OrderInEventChannel channel,
        IAppHubService eventHub,
         IServiceScopeFactory scopeFactory) : BackgroundService 
    {
        private readonly ILogger _log = Log.ForContext<OrderInEventBus>();
        private readonly OrderInEventChannel _channel = channel;
        private readonly IAppHubService _eventHub = eventHub;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (IAppEvent appEvent in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                using var scope = scopeFactory.CreateScope();
                var subscribers = scope.ServiceProvider.GetServices<IOrderInEventSubscriber>();
                try
                {
                    switch (appEvent)
                    {
                        case CreatedEvent<OrderIn> createdEvent:
                            var createdTasks = subscribers.Select(s => s.OnCreatedAsync(createdEvent.Value));
                            await Task.WhenAll(createdTasks);
                            var createdDto = OrderInMapping.ToDto(createdEvent.Value);
                            await _eventHub.CreatedAsync(createdDto);
                            break;

                        case UpdatedEvent<OrderIn> updatedEvent:
                            var updatedTasks = subscribers.Select(s => s.OnUpdatedAsync(updatedEvent.Value));
                            await Task.WhenAll(updatedTasks);
                            var updatedDto = OrderInMapping.ToDto(updatedEvent.Value);
                            await _eventHub.UpdatedAsync(updatedDto);
                            break;
                        case DeletedEvent deletedEvent:
                            var deletedTasks = subscribers.Select(s => s.OnDeletedAsync(deletedEvent.Id));
                            await Task.WhenAll(deletedTasks);
                            await _eventHub.DeletedAsync<Shared.Models.Documents.OrderIn>(deletedEvent.Id);
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
