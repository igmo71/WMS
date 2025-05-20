using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace WMS.Client.Core.Services
{
    internal class SignalRClientService
    {
        private readonly HubConnection _hubConnection;

        internal SignalRClientService()
        {
            _hubConnection = new HubConnectionBuilder().WithUrl("http://igmo-pc:8220/hub").WithAutomaticReconnect().Build();
            _hubConnection.StartAsync().Wait();
        }

        internal void Subscribe<T>(string method, Action<T> action) => _hubConnection.On<T>(method, action);
    }
}
