using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceBroker;

namespace ConnectionServer
{
    class Program
    {
        static async Task StartClient(int r, string server)
        {
            Receiver[] receivers = new Receiver[r];
            var taskList = new List<Task>();
            var monitors = new Monitors();
            for (int i = 0; i < r; i++)
            {
                receivers[i] = new Receiver(server, monitors);
                taskList.Add(receivers[i].Connect());
            }
            await Task.WhenAll(taskList);
            //monitors.StartPrint();
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            for (int i = 0; i < r; i++)
            {
                await receivers[i].Stop();
            }
        }
        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: server [service_endpoint] [receiver_numbers]");
                return;
            }
            var server = args[0];
            var receivers = int.Parse(args[1]);
            await StartClient(receivers, server);
        }
    }
}
