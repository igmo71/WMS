using WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.Hubs
{
    public interface IAppHubService
    {
        Task CreatedAsync<TEntity>(TEntity entity) where TEntity : EntityBase;
        Task UpdatedAsync<TEntity>(TEntity entity) where TEntity : EntityBase;
        Task DeletedAsync(Guid entityId);
    }
}
