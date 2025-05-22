using Dto = WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.Hubs
{
    public interface IAppHubService<T>
    {
        Task CreatedAsync(T entity);
        Task UpdatedAsync(T entity);
        Task DeletedAsync(Guid entityId);
    }
}
