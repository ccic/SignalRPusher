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
        public readonly long Step = 100;    // latency unit
        public readonly long Length = 10;    // how many latency categories will be displayed
        public readonly string OutFile = "Timestamps.log";
        private long[] _latency;
        private long _totalReceivedBytes;
        private long _lastReceivedBytes;
        private long _receivedRate;
        private object _lock = new object();
        private Timer _timer;
        private long _startPrint;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        public Monitors(long s = 100, long l = 10)
        {
            Step = s;
            Length = l;
            _latency = new long[Length];
            _timer = new Timer(Report, state: this, dueTime: Interval, period: Interval);
        }

        public void StartPrint()
        {
             if (Interlocked.CompareExchange(ref _startPrint, 1, 0) == 0)
             {
                 _timer = new Timer(Report, state: this, dueTime: Interval, period: Interval);
             }
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

        public void WriteAll2File(List<long> allTimestamps)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(OutFile, true))
            {
                var sb = new StringBuilder();
                sb.Append(DateTimeOffset.Now).Append("|");
                foreach (long t in allTimestamps)
                {
                    sb.Append(":").Append(t);
                }
                file.WriteLine(sb.ToString());
            }
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
            // create a readable latency categories
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
            // dump out all statistics
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                Latency = dic,
                ReceivedRate = _receivedRate,
                TotalReceivedBytes = _totalReceivedBytes
            }));
        }
    }
}
