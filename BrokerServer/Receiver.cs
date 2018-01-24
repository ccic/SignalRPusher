using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace PushClient
{
    public class Receiver
    {
        private HubConnection _hubConnection;
        private string _clientMethod = "echo";
        private Monitors _monitors;
        public Receiver(string protocol, string server, Monitors monitor)
        {
            _monitors = monitor;
            _hubConnection = protocol.ToLower() == "json" ?
                    new HubConnectionBuilder().WithUrl(server).WithJsonProtocol().Build() :
                    new HubConnectionBuilder().WithUrl(server).WithMessagePackProtocol().Build();
            _hubConnection.On<List<long>>(_clientMethod, (recvMessage) =>
            {
                //Console.WriteLine("data: {0}", recvMessage);
                recvMessage.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                _hubConnection.InvokeAsync<object>(_clientMethod, recvMessage);
                _monitors.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - recvMessage[0], sizeof(long) * recvMessage.Count);
            });
        }

        public async Task Connect()
        {
            await _hubConnection.StartAsync();
        }

        public async Task Stop()
        {
            await _hubConnection.StopAsync();
        }
    }
}
