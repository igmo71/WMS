using Dto = WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.Hubs
{
    public interface IAppHubService
    {
        Task CreatedAsync<TEntity>(TEntity entity) where TEntity : Dto.EntityBase;
        Task UpdatedAsync<TEntity>(TEntity entity) where TEntity : Dto.EntityBase;
        Task DeletedAsync<TEntity>(Guid entityId) where TEntity : Dto.EntityBase;
    }
}
