using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ServiceBroker
{
    public class Monitors : IDisposable
    {
        public readonly long Step = 100;    // latency unit
        public readonly long Length = 10;    // how many latency categories will be displayed
        public readonly string OutFile = "Timestamps.log";
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        private long[] _latency;
        private long _totalReceivedBytes;
        private long _lastReceivedBytes;
        private long _receivedBytesRate;
        private long _totalReceived;
        private long _lastReceived;
        private long _receivedRate;

        private long[] _batchMessageCounter;

        private object _lock = new object();
        private Timer _timer;
        private long _startPrint;

        private bool _hasRecord;

        public Monitors(long s = 100, long l = 10)
        {
            Step = s;
            Length = l;
            _latency = new long[Length];
            _batchMessageCounter = new long[Length];
            //_timer = new Timer(Report, state: this, dueTime: Interval, period: Interval);
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
            if (index >= Length)
            {
                index = Length - 1;
            }
            Interlocked.Increment(ref _latency[index]);
            Interlocked.Add(ref _totalReceivedBytes, receivedBytes);
            Interlocked.Increment(ref _totalReceived);
            _hasRecord = true;
        }

        public void RecordBatchMessage(long batchMessageLevel)
        {
            if (batchMessageLevel <= 0)
                return;

            long index = batchMessageLevel - 1;
            if (index >= Length)
            {
                index = Length - 1;
            }
            Interlocked.Increment(ref _batchMessageCounter[index]);
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
            if (_hasRecord)
            {
                ((Monitors)state).InternalReport();
                _hasRecord = false;
            }
        }

        private void InternalReport()
        {   
            lock (_lock) {
                var totalReceivedBytes = Interlocked.Read(ref _totalReceivedBytes);
                var lastReceivedBytes = Interlocked.Read(ref _lastReceivedBytes);
                Interlocked.Exchange(ref _receivedBytesRate, totalReceivedBytes - lastReceivedBytes);
                _lastReceivedBytes = _totalReceivedBytes;

                var totalReceived = Interlocked.Read(ref _totalReceived);
                var lastReceived = Interlocked.Read(ref _lastReceived);
                Interlocked.Exchange(ref _receivedRate, totalReceived - lastReceived);
                lastReceived = totalReceived;
            }
            // create a readable latency categories
            var dic = new ConcurrentDictionary<string, long>();
            var batchMessageDic = new ConcurrentDictionary<string, long>();
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
            for (var i = 0; i < Length; i++)
            {
                sb.Clear();
                sb.Append("batch_");
                sb.Append(Convert.ToString(i+1));
                batchMessageDic[sb.ToString()] = _batchMessageCounter[i];
            }
            // dump out all statistics
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                Latency = dic,
                ReceivedRate = _receivedRate,
                ReceivedBytesRate = _receivedBytesRate,
                TotalReceivedBytes = _totalReceivedBytes,
                BatchMessage = batchMessageDic
            }));
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
