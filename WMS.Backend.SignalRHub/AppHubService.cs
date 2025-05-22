using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Common;
using Dto = WMS.Shared.Models;

namespace WMS.Backend.SignalRHub
{
    internal class AppHubService<T>(IHubContext<AppHub> hubContext) : IAppHubService<T>
    {
        private readonly IHubContext<AppHub> _hubContext = hubContext;

        public async Task CreatedAsync(T entity)
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(T).Name}{AppSettings.Events.Created}", entity);
        }

        public async Task UpdatedAsync(T entity)
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(T).Name}{AppSettings.Events.Updated}", entity);
        }

        public async Task DeletedAsync(Guid entityId)
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(T).Name}{AppSettings.Events.Deleted}", entityId);
        }
    }
}
