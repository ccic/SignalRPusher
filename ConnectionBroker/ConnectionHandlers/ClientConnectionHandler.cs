// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using ServiceBroker;

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
                            while (BrokerUtils.TryParseMessage(ref buffer, out var record))
                            {
                                await _connectionBroker.SendToServer(connection.ConnectionId, record.First);
                            }
                            // send "<ConnectionID>|timestamp;" to server
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
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
