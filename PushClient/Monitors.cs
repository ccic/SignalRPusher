using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace PushClient
{
    public class Monitors
    {
        public readonly long Step = 50;
        public readonly long Length = 5;

        private long[] _latency;
        private long _totalReceivedBytes;
        private long _lastReceivedBytes;
        private long _receivedRate;
        private object _lock = new object();
        private Timer _timer;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
        public Monitors(long s = 50, long l = 5)
        {
            Step = s;
            Length = l;
            _latency = new long[Length];
            _timer = new Timer(Report, state: this, dueTime: Interval, period: Interval);
        }

        public void Record(long dur, long receivedBytes)
        {
            long index = dur / Step;
            if (index > Length)
            {
                index = Length - 1;
            }
            Interlocked.Increment(ref _latency[index]);
            Interlocked.Add(ref _totalReceivedBytes, receivedBytes);
        }

        public Int64[] Latency => _latency; 

        private void Report(object state)
        {
            ((Monitors)state).InternalReport();
        }

        private void InternalReport()
        {
            
            lock (_lock) {
                var totalReceivedBytes = Interlocked.Read(ref _totalReceivedBytes);
                var lastReceivedBytes = Interlocked.Read(ref _lastReceivedBytes);
                Interlocked.Exchange(ref _receivedRate, totalReceivedBytes - lastReceivedBytes);
                _lastReceivedBytes = _totalReceivedBytes;
            }
            
            var dic = new ConcurrentDictionary<string, long>();
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < Length; i++)
            {
                sb.Clear();
                var label = Step + i * Step;
                if (i < Length - 1)
                {   
                    sb.Append("lt_");
                }
                else
                {
                    sb.Append("ge_");
                }
                sb.Append(Convert.ToString(label));
                dic[sb.ToString()] = _latency[i];
            }
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                Latency = dic,
                ReceivedRate = _receivedRate,
                TotalReceivedBytes = _totalReceivedBytes
            }));
        }
    }
}
