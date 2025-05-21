using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInEventProducer(OrderInEventChannel channel) : IOrderInEventProducer
    {
        private readonly OrderInEventChannel _channel = channel;

        public async Task CreatedEventProduce(OrderIn entity)
        {
            var createdEvent = new CreatedEvent<OrderIn>(entity);
            await _channel.Writer.WriteAsync(createdEvent);
        }

        public async Task UpdatedEventProduce(OrderIn entity)
        {
            var updatedEvent = new UpdatedEvent<OrderIn>(entity);
            await _channel.Writer.WriteAsync(updatedEvent);
        }

        public async Task DeletedEventProduce(Guid id)
        {
            var deletedEvent = new DeletedEvent(id);
            await _channel.Writer.WriteAsync(deletedEvent);
        }
    }
}
