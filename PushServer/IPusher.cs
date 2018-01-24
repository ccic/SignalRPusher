using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PushServer
{
    public interface IPusher<THub>
    {
        void ConfigurePusher(string connectionId, int concurrentSender, string clientMethod, string readyMethod);
        void OnConnected(string connectionId);
        void OnDisconnected(string connectionId);
        void OnReceived(long sendTimestamp);
        void Start();
        void Stop();

    }
}
