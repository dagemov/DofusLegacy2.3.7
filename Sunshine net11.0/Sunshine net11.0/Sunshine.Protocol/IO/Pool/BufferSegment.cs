using System.Threading;

namespace Sunshine.Protocol.IO.Pool
{
    public class BufferSegment
    {
        private readonly ArrayBuffer m_buffer;
        private readonly int m_length;
        private readonly int m_offset;
        private int m_uses;
        public byte this[int i]
        {
            get
            {
                return this.m_buffer.Array[this.m_offset + i];
            }
        }
        public byte[] SegmentData
        {
            get
            {
                byte[] array = new byte[this.m_length];
                System.Buffer.BlockCopy(this.m_buffer.Array, this.m_offset, array, 0, this.m_length);
                return array;
            }
        }
        public int Uses
        {
            get
            {
                return this.m_uses;
            }
            internal set
            {
                this.m_uses = value;
            }
        }
        public int Number
        {
            get;
            internal set;
        }
        public ArrayBuffer Buffer
        {
            get
            {
                return this.m_buffer;
            }
        }
        public int Offset
        {
            get
            {
                return this.m_offset;
            }
        }
        public int Length
        {
            get
            {
                return this.m_length;
            }
        }

        public BufferSegment(ArrayBuffer buffer, int offset, int length, int id)
        {
            this.m_buffer = buffer;
            this.m_offset = offset;
            this.m_length = length;
            this.Number = id;
        }

        public void CopyFrom(byte[] bytes, int offset)
        {
            System.Buffer.BlockCopy(bytes, offset, this.m_buffer.Array, this.m_offset + offset, bytes.Length - offset);
        }

        public void CopyTo(BufferSegment segment, int length)
        {
            System.Buffer.BlockCopy(this.m_buffer.Array, this.m_offset, segment.Buffer.Array, segment.Offset, length);
        }

        public void IncrementUsage()
        {
            Interlocked.Increment(ref this.m_uses);
        }

        public void DecrementUsage()
        {
            if (Interlocked.Decrement(ref this.m_uses) == 0)
            {
                this.m_buffer.CheckIn(this);
            }
        }

        public static BufferSegment CreateSegment(byte[] bytes)
        {
            return BufferSegment.CreateSegment(bytes, 0, bytes.Length);
        }

        public static BufferSegment CreateSegment(byte[] bytes, int offset, int length)
        {
            return new BufferSegment(new ArrayBuffer(bytes), offset, length, -1);
        }
    }
}