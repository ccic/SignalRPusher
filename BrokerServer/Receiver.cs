using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceBroker
{
    public class Receiver
    {
        private HubConnection _hubConnection;
        private string _clientMethod = BrokerConstants.DefaultEchoMethod;
        private Monitors _monitors;
        public Receiver(string protocol, string server, Monitors monitor)
        {
            _monitors = monitor;
            var proto = protocol == "json" ? (IHubProtocol)new JsonHubProtocol() : new MessagePackHubProtocol();
            var hubConnectionBuilder = new HubConnectionBuilder();
            hubConnectionBuilder.Services.AddSingleton(protocol);
            _hubConnection = hubConnectionBuilder.WithUrl(server, options => { options.Transports = HttpTransportType.WebSockets; }).Build();
            _hubConnection.On<List<long>>(_clientMethod, (recvMessage) =>
            {
                //Console.WriteLine("data: {0}", recvMessage);
                recvMessage.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                _hubConnection.SendAsync(_clientMethod, recvMessage);
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
