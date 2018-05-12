using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class WebsocketTransport
    {
        private WebSocketMessageType _webSocketMessageType = WebSocketMessageType.Binary;
        private ClientWebSocket _ws;
        private string _url;

        private IDuplexPipe Application { get; set; }

        public WebsocketTransport(IDuplexPipe application, string url)
        {
            Application = application;
            _url = url;
        }

        public async Task Start()
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(_url), CancellationToken.None);
            if (_ws.State == WebSocketState.Open)
            {   
                var receiving = StartReceiving(_ws);
                var sending = StartSending(_ws);

                // Wait for send or receive to complete
                var trigger = await Task.WhenAny(receiving, sending);

                if (trigger == receiving)
                {
                    // We're waiting for the application to finish and there are 2 things it could be doing
                    // 1. Waiting for application data
                    // 2. Waiting for a websocket send to complete

                    // Cancel the application so that ReadAsync yields
                    Application.Input.CancelPendingRead();

                    using (var delayCts = new CancellationTokenSource())
                    {
                        var resultTask = await Task.WhenAny(sending, Task.Delay(new TimeSpan(30), delayCts.Token));

                        if (resultTask != sending)
                        {
                            // Abort the websocket if we're stuck in a pending send to the client
                            _ws.Abort();
                        }
                        else
                        {
                            // Cancel the timeout
                            delayCts.Cancel();
                        }
                    }
                }
                else
                {
                    // We're waiting on the websocket to close and there are 2 things it could be doing
                    // 1. Waiting for websocket data
                    // 2. Waiting on a flush to complete (backpressure being applied)

                    // Abort the websocket if we're stuck in a pending receive from the client
                    _ws.Abort();

                    // Cancel any pending flush so that we can quit
                    Application.Output.CancelPendingFlush();
                }
            }
            else
            {
                Console.WriteLine($"Fail to connect to {_url}");
            }
        }

        private async Task StartReceiving(ClientWebSocket ws)
        {
            var buffer = new byte[2048];

            while (ws.State == WebSocketState.Open)
            {
                var memory = Application.Output.GetMemory();
                var isArray = MemoryMarshal.TryGetArray<byte>(memory, out var arraySegment);

                var result = await ws.ReceiveAsync(arraySegment, CancellationToken.None);
                Application.Output.Advance(result.Count);
                var flushResult = await Application.Output.FlushAsync();

                // We canceled in the middle of applying back pressure
                // or if the consumer is done
                if (flushResult.IsCanceled || flushResult.IsCompleted)
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Application.Output.Complete();
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }

        private async Task StartSending(WebSocket socket)
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    var result = await Application.Input.ReadAsync();
                    var buffer = result.Buffer;

                    // Get a frame from the application

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                if (WebSocketCanSend(socket))
                                {
                                    await socket.SendAsync(buffer, _webSocketMessageType);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        Application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (WebSocketCanSend(socket))
                {
                    // We're done sending, send the close frame to the client if the websocket is still open
                    await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }

                Application.Input.Complete();
            }
        }

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                   ws.State == WebSocketState.Closed ||
                   ws.State == WebSocketState.CloseSent);
        }

        public async Task Stop()
        {
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}
