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
        private string _readyMethod = "ready";
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
                //_monitors.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - recvMessage, sizeof(long));
            });
            _hubConnection.On(_readyMethod, async () =>
            {
                Console.WriteLine("Client is ready to receive data");
                await Start();
            });
        }

        public async Task Connect()
        {
            await _hubConnection.StartAsync();
        }

        public async Task Start()
        {
            await _hubConnection.InvokeAsync("Start");
        }

        public async Task Stop()
        {
            await _hubConnection.StopAsync();
        }
    }
}
