using WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.Cache
{
    public interface IAppCache<T>
    {
        Task<T?> GetAsync(Guid id);
        Task SetAsync(T orderDto);
        Task RemoveAsync(Guid id);
    }
}
