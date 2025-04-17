using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IOrderInEventProducer
    {
        Task OrderCreatedEventProduce(Dto.OrderIn orderDto);
        Task OrderUpdatedEventProduce(Dto.OrderIn orderDto);
        Task OrderDeletedEventProduce(Guid orderId);
    }
}
