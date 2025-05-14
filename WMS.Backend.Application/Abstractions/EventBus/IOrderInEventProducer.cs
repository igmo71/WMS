using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IOrderInEventProducer
    {
        Task CreatedEventProduce(Dto.OrderIn orderDto);
        Task UpdatedEventProduce(Dto.OrderIn orderDto);
        Task DeletedEventProduce(Guid orderId);
    }
}
