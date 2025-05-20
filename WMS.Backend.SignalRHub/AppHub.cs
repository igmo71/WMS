using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace WMS.Backend.SignalRHub
{
    internal class AppHub() : Hub
    {
        private readonly ILogger _log = Log.ForContext<AppHub>();

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            _log.Debug("{Source} {ConnectionId}", nameof(OnConnectedAsync), Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            _log.Debug("{Source} {ConnectionId}", nameof(OnDisconnectedAsync), Context.ConnectionId);
        }
    }
}
