using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace PushClient
{
    public class Sender
    {
        private HubConnection _hubConnection;
        private string _clientMethod = "echo";
        private string _readyMethod = "start";
        private int _senders;
        private Monitors _monitors;
        private Timer _timer;
        private bool _start;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
        public Sender(string protocol, string server, int currentSenders, Monitors monitor)
        {
            _senders = currentSenders;
            _monitors = monitor;
            _hubConnection = new HubConnectionBuilder().WithUrl(server).WithJsonProtocol()
                .WithTransport(Microsoft.AspNetCore.Sockets.TransportType.WebSockets).Build();
            _hubConnection.On<List<long>>(_clientMethod, (recvMessage) =>
            {
                _monitors.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - recvMessage[0], sizeof(long) * recvMessage.Count);
                _monitors.WriteAll2File(recvMessage);
            });
            _hubConnection.On(_readyMethod, () =>
            {
                Console.WriteLine("Client starts continuous sending");
                _start = true;
                _monitors.StartPrint();
            });
            _timer = new Timer(Start, state: this, dueTime: Interval, period: Interval);
        }

        public async Task Connect()
        {
            await _hubConnection.StartAsync();
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
                _ = _hubConnection.InvokeAsync<object>(_clientMethod, startTime);
            }
        }
        public async Task Stop()
        {
            await _hubConnection.StopAsync();
        }
    }
}
