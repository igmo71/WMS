using Serilog;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInLogSubscriber : IOrderInEventSubscriber
    {
        private readonly ILogger _log = Log.ForContext<OrderInLogSubscriber>();

        public Task OnCreatedAsync(OrderIn entity)
        {
            _log.Debug("{Source} {@Order}", nameof(OnCreatedAsync), entity);

            return Task.CompletedTask; 
        }

        public Task OnUpdatedAsync(OrderIn entity)
        {
            _log.Debug("{Source} {@Order}", nameof(OnUpdatedAsync), entity);

            return Task.CompletedTask;
        }

        public Task OnDeletedAsync(Guid id)
        {
            _log.Debug("{Source} {OrderId}", nameof(OnDeletedAsync), id);

            return Task.CompletedTask;
        }
    }
}
