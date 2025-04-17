using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IOrderInEventProducer
    {
        Task OrderInCreatedEventProduce(OrderIn order);
        Task OrderInUpdatedEventProduce(OrderIn order);
        Task OrderInDeletedEventProduce(Guid orderId);
    }
}
