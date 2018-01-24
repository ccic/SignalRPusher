using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PushServer
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
