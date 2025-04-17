using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IOrderInEventProducer
    {
        Task OrderCreatedEventProduce(OrderIn order);
        Task OrderUpdatedEventProduce(OrderIn order);
        Task OrderDeletedEventProduce(Guid orderId);
    }
}
