using WMS.Backend.Domain.Models;
using Dto = WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IEventProducer<TEntity>
    {
        Task CreatedEventProduce(TEntity entity);
        Task UpdatedEventProduce(TEntity entity);
        Task DeletedEventProduce(Guid id);
    }

    public interface IAppEventProducer<TEntity> : IEventProducer<TEntity> where TEntity : EntityBase { }

    public interface IDtoEventProducer<TEntity> : IEventProducer<TEntity> where TEntity : Dto.EntityBase { }
}
