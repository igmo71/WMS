using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.MessageBus
{
    public interface IOrderInEventProducer
    {
        Task OrderInCreatedEventProduce(OrderIn order, byte[]? correlationId = null);
        Task OrderInDeletedEventProduce(Guid id, byte[]? correlationId = null);
    }
}
