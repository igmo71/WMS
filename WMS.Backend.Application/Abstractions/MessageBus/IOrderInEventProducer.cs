using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.MessageBus
{
    public interface IOrderInEventProducer
    {
        Task OrderCreatedEventProduce(OrderIn orderIn);
    }
}
