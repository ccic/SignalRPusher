using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PushServer
{
    public class ClientHub : Hub
    {
        private IPusher<Hub> _pusher;
        public ClientHub(IPusher<Hub> pusher)
        {
            _pusher = pusher;
        }

        public void echo(List<long> timestamps)
        {
            _pusher.ForClientEcho(timestamps);
        }

        public override Task OnConnectedAsync()
        {
            _pusher.OnClientConnected(Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _pusher.OnClientDisconnected(Context.ConnectionId);
            return Task.CompletedTask;
        }
    }
}
