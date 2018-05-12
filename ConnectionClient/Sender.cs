using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker;

namespace ConnectionClient
{
    public class Sender : IDisposable
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
        private Timer _timer;
        private bool _isDisposed;
        private bool _start;
        private string _serverUrl;
        private ClientWebSocket _ws;
        private Monitors _monitor;

        public Sender(string server, Monitors monitor)
        {
            _serverUrl = server;
            _monitor = monitor;
        }

        public async Task Connect()
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(_serverUrl), CancellationToken.None);
            if (_ws.State == WebSocketState.Open)
            {
                _ = Receiving(_ws);
                _timer = new Timer(Start, state: this, dueTime: Interval, period: Interval);
            }
            else
            {
                Console.WriteLine($"Fail to connect to {_serverUrl}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _timer.Dispose();
                _isDisposed = true;
            }
        }

        public void Start()
        {
            _start = true;
        }

        public async Task Stop()
        {
            _start = false;
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }

        private void Start(object state)
        {
            ((Sender)state).InternalStart();
        }

        private void InternalStart()
        {
            if (_start)
            {
                // message format: "timestamp" + separator
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var buffer = BrokerUtils.AddSeparator(timestamp);

                if (_ws.State == WebSocketState.Open)
                {
                    _ws.SendAsync(new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None);
                }
            }
        }

        private async Task Receiving(ClientWebSocket ws)
        {
            var buffer = new byte[2048];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {   
                    ProcessReceived(buffer, result.Count);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    ProcessReceived(buffer, result.Count);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }

        private void ProcessReceived(byte[] buffer, int count)
        {
            var batchMessageCounter = -1;
            var received = Encoding.UTF8.GetString(buffer, 0, count);

            while (BrokerUtils.TryParseMessage(ref received, out var record))
            {
                var sendTime = record;
                _monitor.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Convert.ToInt64(sendTime), count);
                batchMessageCounter++;
            }
            _monitor.RecordBatchMessage(batchMessageCounter);
        }
    }
}
