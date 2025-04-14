using WMS.Backend.Application.Services.OrderServices;

namespace WMS.Backend.MessageBus.Abstractions
{
    public interface IOrderInCommandProducer
    {
        Task CreateOrderCommandProduce(OrderInCreateCommand createOrderCommand);
    }
}
