using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PushServer
{
    public class ServerHub : Hub
    {
        private IPusher<ServerHub> _pusher;
        public ServerHub(IPusher<ServerHub> pusher)
        {
            _pusher = pusher;
        }

        public void Configure(int concurrentSenders, string clientMethod, string readyMethod)
        {
            _pusher.ConfigurePusher(Context.ConnectionId, concurrentSenders, clientMethod, readyMethod);
        }

        public void Start()
        {
            _pusher.Start();
        }

        public void Stop()
        {
            _pusher.Stop();
        }

        public override Task OnConnectedAsync()
        {
            _pusher.OnConnected(Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _pusher.OnDisconnected(Context.ConnectionId);
            return Task.CompletedTask;
        }
    }
}
