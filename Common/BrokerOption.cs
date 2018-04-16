
namespace ServiceBroker
{
    public class BrokerConstants
    {
        public static string DefaultEchoMethod = "echo";
    }

    public class BrokerOption
    {
        public string EchoMethod { get; set; } = BrokerConstants.DefaultEchoMethod;

        public int ConnectionNumber { get; set; } = 2;
    }
}
