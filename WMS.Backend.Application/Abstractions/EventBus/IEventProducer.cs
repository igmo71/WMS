using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IEventProducer<TEntity> where TEntity : EntityBase
    {
        Task CreatedEventProduce(TEntity entity);
        Task UpdatedEventProduce(TEntity entity);
        Task DeletedEventProduce(Guid id);
    }
}
