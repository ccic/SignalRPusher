using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
namespace PushServer
{
    public class DefaultPusher<THub> : IPusher<THub>
    {
        private HubLifetimeManager<THub> _hubLifetimeManager;
        private int _currentSenders;
        private string _clientMethod;
        private long _started;
        private List<string> _connectionIdList = new List<string>();
        private List<Sender<THub>> _senders = new List<Sender<THub>>();
        private object _locker = new object();
        public DefaultPusher(HubLifetimeManager<THub> hubLifetimeManager)
        {
            _hubLifetimeManager = hubLifetimeManager;
        }
        public void ConfigurePusher(string connectionId, int concurrentSenders, string clientMethod, string readyMethod)
        {
            _currentSenders = concurrentSenders;
            _clientMethod = clientMethod;
            _hubLifetimeManager.InvokeConnectionAsync(connectionId, readyMethod, new object[] { });
        }

        public void OnConnected(string connectionId)
        {
            _connectionIdList.Add(connectionId);
        }

        public void OnDisconnected(string connectionId)
        {
            _connectionIdList.Remove(connectionId);
        }

        public void OnReceived(long sendTimestamp)
        {
            
        }

        public void Start()
        {
            lock (_locker)
            {
                if (Interlocked.Read(ref _started) == 1)
                {
                    // never launch more threads to send than expected
                    return;
                }
                Console.WriteLine("Start to send data with {0} concurrent senders to {1} clients", _currentSenders, _connectionIdList.Count);
                var len = _connectionIdList.Count;
                for (int i = 0; i < _currentSenders; i++)
                {
                    var sender = new Sender<THub>(_clientMethod, _connectionIdList[i % len], _hubLifetimeManager);
                    sender.Start();
                    _senders.Add(sender);
                }
                Interlocked.CompareExchange(ref _started, 1, 0);
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                for (int i = 0; i < _senders.Count; i++)
                {
                    _senders[i].Stop();
                }
                _senders.Clear();
                _connectionIdList.Clear();
                Interlocked.CompareExchange(ref _started, 0, 1);
                Console.WriteLine("Stop and clear all senders");
            }
        }
    }
}
