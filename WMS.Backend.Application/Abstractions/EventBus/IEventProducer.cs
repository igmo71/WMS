namespace WMS.Backend.Application.Abstractions.EventBus
{
    public interface IEventProducer<T>
    {
        Task CreatedEventProduce(T entity);
        Task UpdatedEventProduce(T entity);
        Task DeletedEventProduce(Guid id);
    }
}
