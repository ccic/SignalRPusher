using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace PushClient
{
    class Program
    {
        static void StartClient(int senders, string server, string protocol)
        {
            var monitors = new Monitors();
            var receiver = new Receiver(protocol, server, senders, monitors);
            receiver.Connect().ContinueWith(t => {
                
            });
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            receiver.Stop().Wait();
        }
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: client [server_endpoint] [concurrent_sender_numbers] [protocol: json/messagepack]");
                return;
            }
            var server = args[0];
            var senders = int.Parse(args[1]);
            var protocol = args[2];
            StartClient(senders, server, protocol);
        }
    }
}
