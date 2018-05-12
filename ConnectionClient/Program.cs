using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceBroker;

namespace ConnectionClient
{
    class Program
    {
        static async Task StartClient(int s, string server)
        {
            Sender[] senders = new Sender[s];
            var monitors = new Monitors();
            var taskList = new List<Task>();
            for (int i = 0; i < s; i++)
            {
                senders[i] = new Sender(server, monitors);
                taskList.Add(senders[i].Connect());
            }
            await Task.WhenAll(taskList);
            for (int i = 0; i < s; i++)
            {
                senders[i].Start();
            }
            monitors.StartPrint();
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            for (int i = 0; i < s; i++)
            {
                await senders[i].Stop();
            }
        }

        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: client [server_endpoint] [concurrent_sender_numbers]");
                Console.Error.WriteLine("To connect to an ASP.NET Connection Handler, use 'ws://example.com/path/to/hub' or 'wss://example.com/path/to/hub' (for HTTPS)");
                return;
            }
            var server = args[0];
            var senders = int.Parse(args[1]);
            await StartClient(senders, server);
        }
    }
}
