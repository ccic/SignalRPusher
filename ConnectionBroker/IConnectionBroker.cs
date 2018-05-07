using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace ConnectionBroker
{
    public interface IConnectionBroker
    {
        Task AddClientConnectionContext(ConnectionContext connection);

        Task AddServerConnectionContext(ConnectionContext connection);

        Task RemoveClientConnectionContext(ConnectionContext connection);

        Task RemoveServerConnectionContext(ConnectionContext connection);

        Task SendToServer(string connectionId, ReadOnlyMemory<byte> payload);

        Task SendToClient(string connectionId, ReadOnlyMemory<byte> payload);
    }
}
