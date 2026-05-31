using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors
{
    public class ActorManager : Singleton<ActorManager>
    {
        private UniqueIdProvider _idProvider = new UniqueIdProvider();
        private ReversedUniqueIdProvider _IdReverseProvider = new ReversedUniqueIdProvider(0);

        public int GenerateId(bool isReverse = false, bool reset = false)
        {
            if (reset)
                _IdReverseProvider = new ReversedUniqueIdProvider(0);

            if (isReverse)
                return _IdReverseProvider.Pop();
            else
                return _idProvider.Pop();
        }
    }
}
