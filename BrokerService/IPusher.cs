using System.Collections.Generic;

namespace ServiceBroker
{
    public interface IPusher<THub>
    {
        void OnClientConnected(string connectionId);

        void OnClientDisconnected(string connectionId);

        void OnServerConnected(string connectionId);

        void OnServerDisconnected(string connectionId);

        void ForClientEcho(List<long> timestamps);

        void ForServerEcho(List<long> timestampes);
    }
}
