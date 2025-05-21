using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    internal interface IOrderInEventProducer : IAppEventProducer<OrderIn>
    {
    }
}
