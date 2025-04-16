using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.MessageBus
{
    public interface IOrderInEventProducer
    {
        Task OrderInCreatedEventProduce(OrderIn order);
        Task OrderInUpdatedEventProduce(OrderIn order);
        Task OrderInDeletedEventProduce(Guid id);
    }
}
