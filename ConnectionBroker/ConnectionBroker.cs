using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace ConnectionBroker
{
    public class ConnectionBroker : IConnectionBroker
    {
        private readonly List<ConnectionContext> _serverConnections = new List<ConnectionContext>();
        private ConnectionList _clientConnections = new ConnectionList();

        public ConnectionBroker()
        {
        }

        public Task AddClientConnectionContext(ConnectionContext connection)
        {
            _clientConnections.Add(connection);
            return Task.CompletedTask;
        }

        public Task AddServerConnectionContext(ConnectionContext connection)
        {
            _serverConnections.Add(connection);
            return Task.CompletedTask;
        }

        public Task RemoveClientConnectionContext(ConnectionContext connection)
        {
            _clientConnections.Remove(connection);
            return Task.CompletedTask;
        }

        public Task RemoveServerConnectionContext(ConnectionContext connection)
        {
            _serverConnections.Remove(connection);
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

        // Send "<ConnectionID>|data" to server
        public Task SendToServer(string connectionId, ReadOnlyMemory<byte> payload)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append(connectionId).Append('|').Append(Encoding.UTF8.GetString(payload.Span));
            var index = StaticRandom.Next(_serverConnections.Count);
            return _serverConnections[index].Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(strBuilder.ToString())).AsTask();
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
