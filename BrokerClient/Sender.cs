using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceBroker
{
    public class Sender
    {
        private HubConnection _hubConnection;
        private string _clientMethod = BrokerConstants.DefaultEchoMethod;
        private int _senders;
        private Monitors _monitors;
        private Timer _timer;
        private bool _start;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        public Sender(string protocol, string server, int currentSenders, Monitors monitor)
        {
            _senders = currentSenders;
            _monitors = monitor;
            var proto = protocol == "json" ? (IHubProtocol)new JsonHubProtocol() : new MessagePackHubProtocol();
            var hubConnectionBuilder = new HubConnectionBuilder();
            hubConnectionBuilder.Services.AddSingleton(protocol);
            _hubConnection = hubConnectionBuilder.WithUrl(server, options => { options.Transports = HttpTransportType.WebSockets; }).Build();
            _hubConnection.On<List<long>>(_clientMethod, (recvMessage) =>
            {
                _monitors.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - recvMessage[0], sizeof(long) * recvMessage.Count);
                _monitors.WriteAll2File(recvMessage);
            });
            _hubConnection.Closed += Close;
            _timer = new Timer(Start, state: this, dueTime: Interval, period: Interval);
        }

        public async Task Connect()
        {
            await _hubConnection.StartAsync();
        }

        public void Start()
        {
            _start = true;
        }

        private void Start(object state)
        {
            ((Sender)state).InternalStart();
        }

        private void InternalStart()
        {
            if (_start)
            {
                List<long> startTime = new List<long>();
                startTime.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                _ = _hubConnection.SendAsync(_clientMethod, startTime);
            }
        }

        private Task Close(Exception e)
        {
            Console.WriteLine($"Close connection for {e.Message}");
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            await _hubConnection.StopAsync();
        }
    }
}
