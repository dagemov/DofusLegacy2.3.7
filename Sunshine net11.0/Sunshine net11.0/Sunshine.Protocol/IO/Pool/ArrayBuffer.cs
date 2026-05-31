namespace Sunshine.Protocol.IO.Pool
{
    public class ArrayBuffer
    {
        private readonly byte[] m_array;
        private readonly BufferManager m_manager;
        public byte[] Array
        {
            get
            {
                return this.m_array;
            }
        }

        internal ArrayBuffer(byte[] array)
        {
            this.m_array = array;
        }

        internal ArrayBuffer(BufferManager manager, int bufferSize)
        {
            this.m_manager = manager;
            this.m_array = new byte[bufferSize];
        }

        protected internal void CheckIn(BufferSegment segment)
        {
            if (this.m_manager != null)
            {
                this.m_manager.CheckIn(segment);
            }
        }
    }
}