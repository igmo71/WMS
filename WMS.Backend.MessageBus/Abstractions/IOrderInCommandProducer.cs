using WMS.Backend.Application.Services.OrderServices;

namespace WMS.Backend.MessageBus.Abstractions
{
    public interface IOrderInCommandProducer
    {
        Task OrderInCreateCommandProduce(OrderInCreateCommand createOrderCommand);
        Task OrderInDeleteCommandProduce(Guid id);
    }
}
