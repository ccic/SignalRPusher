// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace ConnectionBroker
{
    public class ClientConnectionHandler : ConnectionHandler
    {
        private IConnectionBroker _connectionBroker;

        public ClientConnectionHandler(IConnectionBroker connectionBroker)
        {
            _connectionBroker = connectionBroker;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            await _connectionBroker.AddClientConnectionContext(connection);

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
                            // send "<ConnectionID>|timestamp" to server
                            if (buffer.IsSingleSegment)
                            {
                                _ = _connectionBroker.SendToServer(connection.ConnectionId, buffer.First);
                            }
                            else
                            {
                                var position = buffer.Start;
                                while (buffer.TryGet(ref position, out var memory))
                                {
                                    _ = _connectionBroker.SendToServer(connection.ConnectionId, memory);
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
