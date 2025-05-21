using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    internal interface IOrderInEventSubscriber
    {
        Task OnCreatedAsync(OrderIn entity);
        Task OnUpdatedAsync(OrderIn entity);
        Task OnDeletedAsync(Guid id);
    }
}
