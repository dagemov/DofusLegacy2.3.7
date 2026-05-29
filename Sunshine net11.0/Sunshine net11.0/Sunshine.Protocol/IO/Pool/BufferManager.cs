using System;
using System.Collections.Generic;
using System.Threading;
using Sunshine.Protocol.IO.Collections;

namespace Sunshine.Protocol.IO.Pool
{
    public class BufferManager
    {
        public static readonly List<BufferManager> Managers = new List<BufferManager>();
        public static readonly BufferManager Tiny = new BufferManager(1024, 128);
        public static readonly BufferManager Small = new BufferManager(1024, 1024);
        public static readonly BufferManager Default = new BufferManager(128, 8196);
        public static readonly BufferManager Large = new BufferManager(128, 65536);
        public static readonly BufferManager ExtraLarge = new BufferManager(32, 524288);
        public static readonly BufferManager SuperSized = new BufferManager(16, 1048576);
        public static long GlobalAllocatedMemory = 0L;
        private static volatile int m_segmentId;
        private readonly LockFreeQueue<BufferSegment> m_availableSegments;
        private readonly List<ArrayBuffer> m_buffers;
        private readonly int m_segmentCount;
        private readonly int m_segmentSize;
        private int m_totalSegmentCount;
        public int AvailableSegmentsCount
        {
            get
            {
                return this.m_availableSegments.Count;
            }
        }
        public bool InUse
        {
            get
            {
                return this.m_availableSegments.Count < this.m_totalSegmentCount;
            }
        }
        public int UsedSegmentCount
        {
            get
            {
                return this.m_totalSegmentCount - this.m_availableSegments.Count;
            }
        }
        public int TotalBufferCount
        {
            get
            {
                return this.m_buffers.Count;
            }
        }
        public int TotalSegmentCount
        {
            get
            {
                return this.m_totalSegmentCount;
            }
        }
        public int TotalAllocatedMemory
        {
            get
            {
                return this.m_buffers.Count * (this.m_segmentCount * this.m_segmentSize);
            }
        }
        public int SegmentSize
        {
            get
            {
                return this.m_segmentSize;
            }
        }

        public BufferManager(int segmentCount, int segmentSize)
        {
            this.m_segmentCount = segmentCount;
            this.m_segmentSize = segmentSize;
            this.m_buffers = new List<ArrayBuffer>();
            this.m_availableSegments = new LockFreeQueue<BufferSegment>();
            BufferManager.Managers.Add(this);
        }

        public BufferSegment CheckOut()
        {
            BufferSegment bufferSegment;
            if (!this.m_availableSegments.TryDequeue(out bufferSegment))
            {
                lock (this.m_buffers)
                {
                    while (!this.m_availableSegments.TryDequeue(out bufferSegment))
                    {
                        this.CreateBuffer();
                    }
                }
            }
            if (bufferSegment.Uses > 1)
            {
    //            BufferManager.log.Error("Checked out segment (Size: {0}, Number: {1}) that is already in use! Queue contains: {2}, Buffer amount: {3}", new object[]
				//{
				//	bufferSegment.Length,
				//	bufferSegment.Number,
				//	this.m_availableSegments.Count,
				//	this.m_buffers.Count
				//});
            }
            bufferSegment.Uses = 1;
            return bufferSegment;
        }

        public SegmentStream CheckOutStream()
        {
            return new SegmentStream(this.CheckOut());
        }

        public void CheckIn(BufferSegment segment)
        {
            if (segment.Uses > 1)
            {
    //            BufferManager.log.Error("Checked in segment (Size: {0}, Number: {1}) that is already in use! Queue contains: {2}, Buffer amount: {3}", new object[]
				//{
				//	segment.Length,
				//	segment.Number,
				//	this.m_availableSegments.Count,
				//	this.m_buffers.Count
				//});
            }
            this.m_availableSegments.Enqueue(segment);
        }

        private void CreateBuffer()
        {
            ArrayBuffer arrayBuffer = new ArrayBuffer(this, this.m_segmentCount * this.m_segmentSize);
            for (int i = 0; i < this.m_segmentCount; i++)
            {
                this.m_availableSegments.Enqueue(new BufferSegment(arrayBuffer, i * this.m_segmentSize, this.m_segmentSize, BufferManager.m_segmentId++));
            }
            this.m_totalSegmentCount += this.m_segmentCount;
            this.m_buffers.Add(arrayBuffer);
            Interlocked.Add(ref BufferManager.GlobalAllocatedMemory, (long)(this.m_segmentCount * this.m_segmentSize));
        }

        public static BufferSegment GetSegment(int payloadSize)
        {
            BufferSegment result;
            if (payloadSize <= BufferManager.Tiny.SegmentSize)
            {
                result = BufferManager.Tiny.CheckOut();
            }
            else
            {
                if (payloadSize <= BufferManager.Small.SegmentSize)
                {
                    result = BufferManager.Small.CheckOut();
                }
                else
                {
                    if (payloadSize <= BufferManager.Default.SegmentSize)
                    {
                        result = BufferManager.Default.CheckOut();
                    }
                    else
                    {
                        if (payloadSize <= BufferManager.Large.SegmentSize)
                        {
                            result = BufferManager.Large.CheckOut();
                        }
                        else
                        {
                            if (payloadSize > BufferManager.ExtraLarge.SegmentSize)
                            {
                                throw new ArgumentOutOfRangeException("Required buffer is way too big: " + payloadSize);
                            }
                            result = BufferManager.ExtraLarge.CheckOut();
                        }
                    }
                }
            }
            return result;
        }

        public static SegmentStream GetSegmentStream(int payloadSize)
        {
            return new SegmentStream(BufferManager.GetSegment(payloadSize));
        }

        ~BufferManager()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            BufferSegment bufferSegment;
            while (this.m_availableSegments.TryDequeue(out bufferSegment))
            {
                this.m_buffers.Clear();
            }
        }
    }
}