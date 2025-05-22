using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.Cache;
using Dto = WMS.Shared.Models;

namespace WMS.Backend.Application.Services
{
    internal class AppCache<T>(IDistributedCache cache) : IAppCache<T> where T : Dto.EntityBase
    {
        private readonly ILogger _log = Log.ForContext<AppCache<T>>();
        private readonly IDistributedCache _cache = cache;

        public async Task<T?> GetAsync(Guid id)
        {
            var cachedBytes = await _cache.GetAsync(id.ToString());

            if (cachedBytes is null)
                return null;

            var result = JsonSerializer.Deserialize<T>(cachedBytes);

            _log.Debug("{Source} From Cache {Id}", nameof(GetAsync), id);

            return result;
        }

        public async Task SetAsync(T entity)
        {
            var cachedBytes = JsonSerializer.SerializeToUtf8Bytes(entity);

            await _cache.SetAsync(entity.Id.ToString(), cachedBytes, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(60)
            });
        }

        public async Task RemoveAsync(Guid id)
        {
            await _cache.RemoveAsync(id.ToString());
        }
    }
}
