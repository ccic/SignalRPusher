using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace ConnectionBroker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.SetMinimumLevel(LogLevel.Warning);
                })
                .UseStartup<Startup>()
                .Build();
    }
}
