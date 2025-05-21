using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.Cache;
using WMS.Shared.Models;

namespace WMS.Backend.Application.Services
{
    internal class AppCache(IDistributedCache cache) : IAppCache
    {
        private readonly ILogger _log = Log.ForContext<AppCache>();
        private readonly IDistributedCache _cache = cache;

        public async Task<T?> GetAsync<T>(Guid id)
        {
            var cachedBytes = await _cache.GetAsync(id.ToString());

            T? result = default;

            if (cachedBytes is not null)
            {
                result = JsonSerializer.Deserialize<T>(cachedBytes);
                _log.Debug("{Source} From Cache {Id}", nameof(GetAsync), id);
            }

            return result;
        }

        public async Task SetAsync<T>(T orderDto) where T : EntityBase
        {
            var cachedBytes = JsonSerializer.SerializeToUtf8Bytes(orderDto);

            await _cache.SetAsync(orderDto.Id.ToString(), cachedBytes, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(60)
            });
        }
    }
}
