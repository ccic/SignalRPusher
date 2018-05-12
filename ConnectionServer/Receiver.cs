using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceBroker;

namespace ConnectionServer
{
    public class Receiver
    {
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        private Monitors _monitor;
        private WebsocketTransport _transport;

        private IDuplexPipe Transport { get; set; }

        private IDuplexPipe Application { get; set; }

        public Receiver(string server, Monitors monitor)
        {
            _monitor = monitor;
            var options = new PipeOptions(writerScheduler: PipeScheduler.ThreadPool,
                readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false,
                pauseWriterThreshold: 0, resumeWriterThreshold: 0);
            var pair = DuplexPipe.CreateConnectionPair(options, options);
            Transport = pair.Transport;
            Application = pair.Application;
            _transport = new WebsocketTransport(Application, server);
        }

        public async Task Connect()
        {
            _ = DispatchMessage();
            await _transport.Start();
        }

        public async Task Stop()
        {
            await _transport.Stop();
        }

        private async Task DispatchMessage()
        {
            try
            {
                var input = Transport.Input;
                while (true)
                {
                    var result = await input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            while (BrokerUtils.TryParseMessage(ref buffer, out var payload))
                            {
                                await ProcessReceived(payload.ToArray());
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                        // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                        // before yielding the read again.
                        input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // If there's an exception, bubble it to the caller
            }
        }

        private async Task ProcessReceived(byte[] input)
        {
            var batchMessageCounter = -1;
            var received = Encoding.UTF8.GetString(input);
            while (BrokerUtils.TryParseMessage(ref received, out var record))
            {
                // format: "connectionId|timestamp1;timestamp2;...;"
                if (!BrokerUtils.GetConnectionId(record, out var connectionId, out var timestamps))
                {
                    Console.WriteLine($"Illegal message: no connectionId {Encoding.UTF8.GetString(input)} {record}");
                }
                else
                {
                    timestamps = Encoding.UTF8.GetString(Convert.FromBase64String(timestamps));
                    while (BrokerUtils.ParseSendTimestamp(ref timestamps, out var sendTime))
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        // calculate the latency on Server side
                        _monitor.Record(now - Convert.ToInt64(sendTime), input.Length);

                        // response format: "connectionId|timestamp_send;timestamp_recv!"
                        var content = new StringBuilder(connectionId);
                        content.Append(BrokerConstants.ConnectionIdTerminator);
                        content.Append(sendTime).Append(BrokerConstants.TimestampSeparator);
                        content.Append(now).Append(BrokerConstants.TimestampSeparator);
                        content.Append(BrokerConstants.RecordSeparator);
                        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content.ToString()));
                        var buffer = BrokerUtils.AddSeparator(base64);
                        await _writeLock.WaitAsync();
                        try
                        {
                            await Transport.Output.WriteAsync(buffer);
                        }
                        finally
                        {
                            _writeLock.Release();
                        }
                    }
                }
                batchMessageCounter++;
            }
            _monitor.RecordBatchMessage(batchMessageCounter);
        }
    }
}
