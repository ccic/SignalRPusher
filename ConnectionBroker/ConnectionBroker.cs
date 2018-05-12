using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using ServiceBroker;

namespace ConnectionBroker
{
    public class ConnectionBroker : IConnectionBroker
    {
        private readonly List<ConnectionContext> _serverConnections = new List<ConnectionContext>();
        private ConnectionList _clientConnections = new ConnectionList();
        private readonly SemaphoreSlim _serverWriteLock = new SemaphoreSlim(1);

        private long _clientConnectionCounter;
        private long _serverConnectionCounter;

        public ConnectionBroker()
        {
        }

        public Task AddClientConnectionContext(ConnectionContext connection)
        {
            _clientConnections.Add(connection);
            Interlocked.Increment(ref _clientConnectionCounter);
            if (Interlocked.Read(ref _clientConnectionCounter) % 1000 == 0)
            {
                Console.WriteLine($"Client connection number: {Interlocked.Read(ref _clientConnectionCounter)}");
            }
            return Task.CompletedTask;
        }

        public Task AddServerConnectionContext(ConnectionContext connection)
        {
            _serverConnections.Add(connection);
            Interlocked.Increment(ref _serverConnectionCounter);
            if (Interlocked.Read(ref _serverConnectionCounter) % 2 == 0)
            {
                Console.WriteLine($"Server connection number: {Interlocked.Read(ref _serverConnectionCounter)}");
            }
            return Task.CompletedTask;
        }

        public Task RemoveClientConnectionContext(ConnectionContext connection)
        {
            _clientConnections.Remove(connection);
            Interlocked.Decrement(ref _clientConnectionCounter);
            if (Interlocked.Read(ref _clientConnectionCounter) % 1000 == 0)
            {
                Console.WriteLine($"Client connection number: {Interlocked.Read(ref _clientConnectionCounter)}");
            }
            return Task.CompletedTask;
        }

        public Task RemoveServerConnectionContext(ConnectionContext connection)
        {
            _serverConnections.Remove(connection);
            Interlocked.Decrement(ref _serverConnectionCounter);
            if (Interlocked.Read(ref _serverConnectionCounter) % 2 == 0)
            {
                Console.WriteLine($"Server connection number: {Interlocked.Read(ref _serverConnectionCounter)}");
            }
            return Task.CompletedTask;
        }

        public Task SendToClient(string connectionId, ReadOnlyMemory<byte> payload)
        {
            var connection = _clientConnections[connectionId];
            if (connection == null)
            {
                Console.WriteLine($"Warning: unexisted client {connectionId}");
                return Task.CompletedTask;
            }
            return connection.Transport.Output.WriteAsync(payload).AsTask();
        }

        // Send "<ConnectionID>|data!" to server
        public async Task SendToServer(string connectionId, ReadOnlyMemory<byte> payload)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append(connectionId)
                      .Append(BrokerConstants.ConnectionIdTerminator)
                      .Append(Encoding.UTF8.GetString(payload.ToArray()));
            var buffer = BrokerUtils.AddSeparator(strBuilder.ToString());
            var index = StaticRandom.Next(_serverConnections.Count);
            await _serverWriteLock.WaitAsync();

            try
            {
                await _serverConnections[index].Transport.Output.WriteAsync(buffer);
            }
            finally
            {
                _serverWriteLock.Release();
            }
        }

        internal class StaticRandom
        {
            private static readonly object RandomLock = new object();
            private static readonly Random RandomInterval = new Random((int)DateTime.UtcNow.Ticks);

            public static int Next(int maxValue)
            {
                lock (RandomLock)
                {
                    return RandomInterval.Next(maxValue);
                }
            }
        }
    }
}
