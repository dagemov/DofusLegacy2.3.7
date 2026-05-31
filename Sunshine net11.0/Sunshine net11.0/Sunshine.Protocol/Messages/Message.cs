using Sunshine.Protocol.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.Protocol.Messages
{
    public abstract class Message
    {
        readonly byte BIT_RIGHT_SHIFT_LEN_PACKET_ID = 2;

        public abstract uint MessageId { get; }
        public abstract void Serialize(IDataWriter writer);
        public abstract void Deserialize(IDataReader reader);

        public void Pack(BigEndianWriter writer)
        {
            Serialize(writer);
            WritePacket(writer);
        }

        public void Unpack(BigEndianReader reader)
        {
            Deserialize(reader);
        }

        private void WritePacket(IDataWriter packet)
        {
            byte[] data = packet.Data;
            packet.Clear();
            uint typeLen = PacketLength((uint)data.Length);
            packet.WriteShort((short)SubComputeStaticHeader(MessageId, typeLen));
            switch (typeLen)
            {
                case 0:
                    return;
                case 1:
                    packet.WriteByte((byte)data.Length);
                    break;
                case 2:
                    packet.WriteShort((short)data.Length);
                    break;
                case 3:
                    packet.WriteByte((byte)(data.Length >> 16 & 255));
                    packet.WriteShort((short)(data.Length & 65535));
                    break;
            }
            packet.WriteBytes(data);
        }

        private uint PacketLength(uint len)
        {
            if (len > 65535)
            {
                return 3;
            }
            if (len > 255)
            {
                return 2;
            }
            if (len > 0)
            {
                return 1;
            }
            return 0;
        }

        private uint SubComputeStaticHeader(uint msgId, uint typeLen)
        {
            return msgId << BIT_RIGHT_SHIFT_LEN_PACKET_ID | typeLen;
        }

        public static Dictionary<uint, Func<Message>> Messages = new Dictionary<uint, Func<Message>>();

        public override string ToString() { return GetType().Name; }
    }
}
