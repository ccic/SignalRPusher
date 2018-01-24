﻿using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace PushClient
{
    class Program
    {
        static void StartClient(int r, string server, string protocol)
        {
            Receiver[] receivers = new Receiver[r];
            var monitors = new Monitors();
            for (int i = 0; i < r; i++)
            {
                receivers[i] = new Receiver(protocol, server, monitors);
                _ = receivers[i].Connect();
            }
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            for (int i = 0; i < r; i++)
            {
                receivers[i].Stop().Wait();
            }
        }
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: client [server_endpoint] [receiver_numbers] [protocol: json/messagepack]");
                return;
            }
            var server = args[0];
            var receivers = int.Parse(args[1]);
            var protocol = args[2];
            StartClient(receivers, server, protocol);
        }
    }
}
