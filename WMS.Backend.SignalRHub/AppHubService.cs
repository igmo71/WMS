using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Shared.Models;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.SignalRHub
{
    internal class AppHubService(IHubContext<AppHub> hubContext) : IAppHubService
    {
        private readonly IHubContext<AppHub> _hubContext = hubContext;

        public async Task CreatedAsync<TEntity>(TEntity entity) where TEntity : EntityBase
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(Dto.OrderIn).Name}Created", entity);
        }

        public async Task UpdatedAsync<TEntity>(TEntity entity) where TEntity : EntityBase
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(Dto.OrderIn).Name}Updated", entity);
        }

        public async Task DeletedAsync(Guid entityId)
        {
            await _hubContext.Clients.All.SendAsync($"{typeof(Dto.OrderIn).Name}Deleted", entityId);
        }
    }
}
