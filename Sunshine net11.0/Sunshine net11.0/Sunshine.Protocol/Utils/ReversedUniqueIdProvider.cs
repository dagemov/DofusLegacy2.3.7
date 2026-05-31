using System.Collections.Generic;
using System.Threading;

namespace Sunshine.Protocol.Utils
{
    public class ReversedUniqueIdProvider : UniqueIdProvider
    {
        public ReversedUniqueIdProvider()
        {
        }

        public ReversedUniqueIdProvider(int lastId)
            : base(lastId)
        {
        }

        public ReversedUniqueIdProvider(IEnumerable<int> freeIds)
            : base(freeIds)
        {
        }

        protected override int Next()
        {
            return Interlocked.Decrement(ref this.m_highestId);
        }
    }
}