using WMS.Shared.Models;

namespace WMS.Backend.Application.Abstractions.Cache
{
    public interface IAppCache
    {
        Task<T?> GetAsync<T>(Guid id);
        Task SetAsync<T>(T orderDto) where T : EntityBase;
    }
}
