using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.SignalR;

namespace ServiceBroker
{
    public class DefaultPusher<THub> : IPusher<THub> where THub : Hub
    {
        private HubLifetimeManager<ClientHub> _clientHubLifetimeManager;
        private HubLifetimeManager<ServerHub> _serverHubLifetimeManager;
        private List<string> _clientConnections = new List<string>();
        private List<string> _serverConnections = new List<string>();

        //private BrokerOption _brokerOption;
        private long _clientSelector;
        private long _serverSelector;
        private long _clientConnectionCounter;
        private long _serverConnectionCounter;

        public DefaultPusher(HubLifetimeManager<ClientHub> clientHubLifetimeManager,
            HubLifetimeManager<ServerHub> serverHubLifetimeManager)
        {
            _clientHubLifetimeManager = clientHubLifetimeManager;
            _serverHubLifetimeManager = serverHubLifetimeManager;
        }

        /* Timestamp distribution:
         * 
         * client(timestamp1)         ---> clientHub(timestamp2)   ---> server(timestamp3)
         * client(local timestamp)    <--- serverHub(timestamp4)   <--- server
         */
        public void ForClientEcho(List<long> timestamps)
        {
            timestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (_serverConnections.Count > 0)
            {
                // round-robin select the output server
                int index = (int)(Interlocked.Read(ref _serverSelector) % _serverConnections.Count);
                _serverHubLifetimeManager.SendConnectionAsync(_serverConnections[index], BrokerConstants.DefaultEchoMethod, new object[] { timestamps });
                Interlocked.Increment(ref _serverSelector);
            }
        }

        public void ForServerEcho(List<long> timestamps)
        {
            timestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (_clientConnections.Count > 0)
            {
                // round-robin select the output client
                int index = (int)(Interlocked.Read(ref _clientSelector) % _clientConnections.Count);
                _clientHubLifetimeManager.SendConnectionAsync(_clientConnections[index], BrokerConstants.DefaultEchoMethod, new object[] { timestamps });
                Interlocked.Increment(ref _clientSelector);
            }
        }

        public void OnClientConnected(string connectionId)
        {
            _clientConnections.Add(connectionId);
            Interlocked.Increment(ref _clientConnectionCounter);
            if (Interlocked.Read(ref _clientConnectionCounter) % 1000 == 0)
            {
                Console.WriteLine($"Client connection number: {Interlocked.Read(ref _clientConnectionCounter)}");
            }
        }

        public void OnClientDisconnected(string connectionId)
        {
            _clientConnections.Remove(connectionId);
            Interlocked.Decrement(ref _clientConnectionCounter);
            if (Interlocked.Read(ref _clientConnectionCounter) % 1000 == 0)
            {
                Console.WriteLine($"Client connection number: {Interlocked.Read(ref _clientConnectionCounter)}");
            }
        }

        public void OnServerConnected(string connectionId)
        {
            _serverConnections.Add(connectionId);
            Interlocked.Increment(ref _serverConnectionCounter);
            if (Interlocked.Read(ref _serverConnectionCounter) % 2 == 0)
            {
                Console.WriteLine($"Server connection number: {Interlocked.Read(ref _serverConnectionCounter)}");
            }
        }

        public void OnServerDisconnected(string connectionId)
        {
            _serverConnections.Remove(connectionId);
            Interlocked.Decrement(ref _serverConnectionCounter);
            if (Interlocked.Read(ref _serverConnectionCounter) % 2 == 0)
            {
                Console.WriteLine($"Server connection number: {Interlocked.Read(ref _serverConnectionCounter)}");
            }
        }
    }
}
