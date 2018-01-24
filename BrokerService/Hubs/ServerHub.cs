using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PushServer
{
    public class ServerHub : Hub
    {
        private IPusher<Hub> _pusher;
        public ServerHub(IPusher<Hub> pusher)
        {
            _pusher = pusher;
        }

        public void echo(List<long> timestamps)
        {
            _pusher.ForServerEcho(timestamps);
        }
        
        public override Task OnConnectedAsync()
        {
            _pusher.OnServerConnected(Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _pusher.OnServerDisconnected(Context.ConnectionId);
            return Task.CompletedTask;
        }
    }
}
