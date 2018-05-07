using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace ConnectionBroker.ConnectionHandlers
{
    public class ServerConnectionHandler : ConnectionHandler
    {
        private IConnectionBroker _connectionBroker;

        public ServerConnectionHandler(IConnectionBroker connectionBroker)
        {
            _connectionBroker = connectionBroker;
        }

        private string ExtractConnectionId(ReadOnlyMemory<byte> buffer)
        {
            string connectionId = null;
            var data = Encoding.UTF8.GetString(buffer.Span);
            var index = data.IndexOf('|');
            if (index != -1)
            {
                connectionId = data.Substring(0, index);
            }
            return connectionId;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            await _connectionBroker.AddServerConnectionContext(connection);

            try
            {
                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            // We can avoid the copy here but we'll deal with that later
                            if (buffer.IsSingleSegment)
                            {
                                var connectionId = ExtractConnectionId(buffer.First);
                                if (connectionId != null)
                                {
                                    _ = _connectionBroker.SendToClient(connectionId, buffer.First);
                                }
                            }
                            else
                            {
                                var position = buffer.Start;
                                while (buffer.TryGet(ref position, out var memory))
                                {
                                    var connectionId = ExtractConnectionId(memory);
                                    if (connectionId != null)
                                    {
                                        _ = _connectionBroker.SendToServer(connectionId, memory);
                                    }
                                }
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        connection.Transport.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            finally
            {
                await _connectionBroker.RemoveClientConnectionContext(connection);
            }
        }
    }
}
