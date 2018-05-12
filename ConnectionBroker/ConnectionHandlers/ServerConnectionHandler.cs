using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using ServiceBroker;

namespace ConnectionBroker
{
    public class ServerConnectionHandler : ConnectionHandler
    {
        private IConnectionBroker _connectionBroker;

        public ServerConnectionHandler(IConnectionBroker connectionBroker)
        {
            _connectionBroker = connectionBroker;
        }

        private async Task ForwardMessageToClient(byte[] buffer)
        {
            var received = Encoding.UTF8.GetString(buffer);
            
            // format: "connectionId|timestamp1;timestamp2;...;"
            if (!BrokerUtils.GetConnectionId(received, out var connectionId, out var timestamps))
            {
                Console.WriteLine($"Illegal message: no connectionId in {Encoding.UTF8.GetString(buffer)} {received}");
            }
            else
            {
                // response format: "timestamp_send;timestamp_server_recv!"
                var content = new StringBuilder();
                content.Append(timestamps);
                content.Append(BrokerConstants.RecordSeparator);

                await _connectionBroker.SendToClient(connectionId, Encoding.UTF8.GetBytes(content.ToString()));
            }
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
                            while (BrokerUtils.TryParseMessage(ref buffer, out var record))
                            {
                                await ForwardMessageToClient(record.ToArray());
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                await _connectionBroker.RemoveServerConnectionContext(connection);
            }
        }
    }
}
