using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
namespace PushServer
{
    public class Sender<THub>
    {
        private readonly string _connectionId;
        private HubLifetimeManager<THub> _hubLifetimeManager;
        private string _method;
        private Timer _timer;

        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
        private bool _started;
        //private CancellationTokenSource _sendCts = new CancellationTokenSource();

        public Sender(string method, string connectionId, HubLifetimeManager<THub> hubLifetimeManager)
        {
            _method = method;
            _connectionId = connectionId;
            _hubLifetimeManager = hubLifetimeManager;
            _timer = new Timer(StartSend, state: this, dueTime: Interval, period: Interval);
        }

        public void Start()
        {
            _started = true;
        }

        public void Stop()
        {
            _started = false;
        }

        private void StartSend(object state)
        {
            ((Sender<THub>)state).StartSendInternal();
        }

        private void StartSendInternal()
        {   
            if (_started)
            {
                //var dic = new ConcurrentDictionary<string, long>();
                //dic["A"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _ = _hubLifetimeManager.InvokeConnectionAsync(_connectionId, _method, new object[] { DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }
        }
    }
}
