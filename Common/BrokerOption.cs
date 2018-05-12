using System.Buffers;
using System.Text;

namespace ServiceBroker
{
    public class BrokerConstants
    {
        public static readonly byte ByteRecordSeparator = 0x1e;
        public static readonly string DefaultEchoMethod = "echo";
        public static readonly string ConnectionIdTerminator = "|";
        public static readonly string TimestampSeparator = ";";
        public static readonly string RecordSeparator = "!";
    }

    public class BrokerUtils
    {
        public static byte[] AddSeparator(string content)
        {
            var byteArrayBuilder = new ByteArrayBuilder();
            byteArrayBuilder.Append(Encoding.UTF8.GetBytes(content)).Append(BrokerConstants.ByteRecordSeparator);
            return byteArrayBuilder.ToArray();
        }

        public static bool TryParseMessage(ref string content, out string record)
        {
            var index = content.IndexOf(BrokerConstants.RecordSeparator);
            if (index == -1)
            {
                record = default;
                return false;
            }
            record = content.Substring(0, index);
            content = content.Substring(index + 1);
            return true;
        }

        public static bool GetConnectionId(string content, out string connectionId, out string timestamps)
        {
            // Bug: why there are leading whitespace?
            //content = content.Trim();
            var index = content.IndexOf(BrokerConstants.ConnectionIdTerminator);
            if (index != -1)
            {
                connectionId = content.Substring(0, index);
                timestamps = content.Substring(index + 1);
                return true;
            }
            connectionId = null;
            timestamps = null;
            return false;
        }

        public static bool ParseSendTimestamp(ref string content, out string sendTime)
        {
            var index = content.IndexOf(BrokerConstants.TimestampSeparator);
            if (index != -1)
            {
                sendTime = content.Substring(0, index);
                content = content.Substring(index + 1);
                return true;
            }
            sendTime = null;
            return false;
        }

        public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
        {
            var position = buffer.PositionOf(BrokerConstants.ByteRecordSeparator);
            if (position == null)
            {
                payload = default;
                return false;
            }

            payload = buffer.Slice(0, position.Value);

            // Skip record separator
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            return true;
        }
    }

    public class BrokerOption
    {
        public string EchoMethod { get; set; } = BrokerConstants.DefaultEchoMethod;

        public int ConnectionNumber { get; set; } = 2;
    }
}
