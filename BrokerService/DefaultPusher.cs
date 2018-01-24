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
        private HubLifetimeManager<ClientHub> _clientHubLifetimeManager;
        private HubLifetimeManager<ServerHub> _serverHubLifetimeManager;
        private List<string> _clientConnections = new List<string>();
        private List<string> _serverConnections = new List<string>();
        private List<Sender<THub>> _senders = new List<Sender<THub>>();
        private BrokerOption _brokerOption;
        private long _clientSelector;
        private long _serverSelector;
        private long _clientConnectionCounter;
        public DefaultPusher(HubLifetimeManager<ClientHub> clientHubLifetimeManager,
            HubLifetimeManager<ServerHub> serverHubLifetimeManager,
            BrokerOption brokerOption)
        {
            _clientHubLifetimeManager = clientHubLifetimeManager;
            _serverHubLifetimeManager = serverHubLifetimeManager;
            _brokerOption = brokerOption;
        }

        public void ForClientEcho(List<long> timestamps)
        {
            timestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (_serverConnections.Count > 0)
            {
                int index = (int)(Interlocked.Read(ref _serverSelector) % _serverConnections.Count);
                _serverHubLifetimeManager.InvokeConnectionAsync(_serverConnections[index], "echo", new object[] { timestamps });
                Interlocked.Increment(ref _serverSelector);
            }
        }

        public void ForServerEcho(List<long> timestamps)
        {
            timestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (_clientConnections.Count > 0)
            {
                int index = (int)(Interlocked.Read(ref _clientSelector) % _clientConnections.Count);
                _clientHubLifetimeManager.InvokeConnectionAsync(_clientConnections[index], "echo", new object[] { timestamps });
                Interlocked.Increment(ref _clientSelector);
            }
        }

        public void OnClientConnected(string connectionId)
        {
            _clientConnections.Add(connectionId);
            Interlocked.Increment(ref _clientConnectionCounter);
            if (Interlocked.Read(ref _clientConnectionCounter) == _brokerOption.ConnectionNumber)
            {
                _clientHubLifetimeManager.InvokeAllAsync("start", new object[0]);
            }
        }

        public void OnClientDisconnected(string connectionId)
        {
            _clientConnections.Remove(connectionId);
        }

        public void OnServerConnected(string connectionId)
        {
            _serverConnections.Add(connectionId);
        }

        public void OnServerDisconnected(string connectionId)
        {
            _serverConnections.Remove(connectionId);
        }
    }
}
