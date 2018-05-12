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

        public Task Connect()
        {
            _ = DispatchMessage();
            _ = _transport.Start();
            return Task.CompletedTask;
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
                            var batchMessageCounter = -1;
                            while (BrokerUtils.TryParseMessage(ref buffer, out var payload))
                            {
                                await ProcessReceived(payload.ToArray());
                                batchMessageCounter++;
                            }
                            _monitor.RecordBatchMessage(batchMessageCounter);
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
            var received = Encoding.UTF8.GetString(input);

            // format: "connectionId|timestamp1;timestamp2;...;"
            if (!BrokerUtils.GetConnectionId(received, out var connectionId, out var timestamps))
            {
                Console.WriteLine($"Illegal message: no connectionId {Encoding.UTF8.GetString(input)}");
            }
            else
            {
                var sendTime = timestamps;
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // calculate the latency on Server side
                _monitor.Record(now - Convert.ToInt64(sendTime), input.Length);

                // response format: "connectionId|timestamp_send;timestamp_recv!"
                var content = new StringBuilder(connectionId);
                content.Append(BrokerConstants.ConnectionIdTerminator);
                content.Append(sendTime);
                var buffer = BrokerUtils.AddSeparator(content.ToString());
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
    }
}
