using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Common;
using Dto = WMS.Shared.Models;

namespace WMS.Backend.SignalRHub
{
    internal class AppHubService(IHubContext<AppHub> hubContext) : IAppHubService
    {
        private readonly IHubContext<AppHub> _hubContext = hubContext;

        public async Task CreatedAsync<TEntity>(TEntity entity) where TEntity : Dto.EntityBase
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(TEntity).Name}{AppSettings.Events.Created}", entity);
        }

        public async Task UpdatedAsync<TEntity>(TEntity entity) where TEntity : Dto.EntityBase
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(TEntity).Name}{AppSettings.Events.Updated}", entity);
        }

        public async Task DeletedAsync<TEntity>(Guid entityId) where TEntity : Dto.EntityBase
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(TEntity).Name}{AppSettings.Events.Deleted}", entityId);
        }
    }
}
