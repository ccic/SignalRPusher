using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PushClient
{
    class Program
    {
        static void StartClient(int s, string server, string protocol)
        {
            Sender[] senders = new Sender[s];
            var monitors = new Monitors();
            var taskList = new List<Task>();
            for (int i = 0; i < s; i++)
            {
                senders[i] = new Sender(protocol, server, s, monitors);
                taskList.Add(senders[i].Connect());
            }
            Task.WhenAll(taskList);
            for (int i = 0; i < s; i++)
            {
                senders[i].Start();
            }
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            for (int i = 0; i < s; i++)
            {
                senders[i].Stop().Wait();
            }
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
