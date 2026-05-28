using System;
using System.IO;

namespace Sunshine.Protocol.IO.Pool
{
    public class SegmentStream : Stream
    {
        private int m_position;
        private BufferSegment m_segment;
        private int m_length;
        public BufferSegment Segment
        {
            get
            {
                return this.m_segment;
            }
        }
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }
        public override long Length
        {
            get
            {
                return (long)this.m_length;
            }
        }
        public override long Position
        {
            get
            {
                return (long)(this.m_position - this.m_segment.Offset);
            }
            set
            {
                this.m_position = (int)value + this.m_segment.Offset;
            }
        }

        public SegmentStream(BufferSegment segment)
        {
            this.m_segment = segment;
            this.m_position = this.m_segment.Offset;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    this.m_position = this.m_segment.Offset + (int)offset;
                    break;

                case SeekOrigin.Current:
                    this.m_position += (int)offset;
                    break;

                case SeekOrigin.End:
                    this.m_position = this.m_segment.Offset + this.m_segment.Length - (int)offset;
                    break;
            }
            if (this.Position > (long)this.m_segment.Length)
            {
                this.Position = (long)this.m_segment.Length;
            }
            return (long)this.m_position;
        }

        public override void SetLength(long value)
        {
            this.m_length = (int)value;
            if (this.m_position > this.m_length)
            {
                this.m_position = this.m_length + this.m_segment.Offset;
            }
            if (this.m_length > this.m_segment.Length)
            {
                this.EnsureCapacity(this.m_length);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, this.m_segment.Offset + this.m_segment.Length - this.m_position);
            Buffer.BlockCopy(this.m_segment.Buffer.Array, this.m_position, buffer, offset, count);
            this.m_position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.m_position + count >= this.m_segment.Offset + this.m_segment.Length)
            {
                this.EnsureCapacity(this.m_position - this.m_segment.Offset + count);
            }
            Buffer.BlockCopy(buffer, offset, this.m_segment.Buffer.Array, this.m_position, count);
            this.m_position += count;
            this.m_length = Math.Max(this.m_length, this.m_position - this.m_segment.Offset);
        }

        public override int ReadByte()
        {
            return (int)this.m_segment.Buffer.Array[this.m_position++];
        }

        public override void WriteByte(byte value)
        {
            if (this.m_position + 1 >= this.m_segment.Offset + this.m_segment.Length)
            {
                this.EnsureCapacity(this.m_position - this.m_segment.Offset + 1);
            }
            this.m_segment.Buffer.Array[this.m_position++] = value;
            this.m_length = Math.Max(this.m_length, this.m_position - this.m_segment.Offset);
        }

        private void EnsureCapacity(int size)
        {
            BufferSegment segment = BufferManager.GetSegment(size);
            this.m_segment.CopyTo(segment, this.m_length);
            this.m_position = this.m_position - this.m_segment.Offset + segment.Offset;
            this.m_segment.DecrementUsage();
            this.m_segment = segment;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.m_segment.DecrementUsage();
        }
    }
}