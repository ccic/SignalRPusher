using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
namespace PushServer
{
    public class DefaultPusher<THub> : IPusher<THub>
    {
        private HubLifetimeManager<THub> _hubLifetimeManager;
        private int _currentSenders;
        private string _clientMethod;
        private List<string> _connectionIdList = new List<string>();
        private List<Sender<THub>> _senders = new List<Sender<THub>>();
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

        public void Start()
        {
            Console.WriteLine("Start to send data with {0} concurrent senders to {1} clients", _currentSenders, _connectionIdList.Count);
            var len = _connectionIdList.Count;
            for (int i = 0; i < _currentSenders; i++)
            {
                var sender = new Sender<THub>(_clientMethod, _connectionIdList[i % len], _hubLifetimeManager);
                sender.Start();
                _senders.Add(sender);
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _senders.Count; i++)
            {
                _senders[i].Stop();
            }
            _senders.Clear();
            _connectionIdList.Clear();
        }
    }
}
