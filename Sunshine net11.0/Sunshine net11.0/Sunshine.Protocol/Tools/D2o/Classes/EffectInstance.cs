using System;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("EffectInstance", "com.ankamagames.dofus.datacenter.effects", true)]
    [Serializable]
    public class EffectInstance : IDataObject
    {
        public int effectId;
        public int targetId;
        public int duration;
        public int random;
        public bool hidden;
        public int zoneSize;
        public int zoneShape;
    }
}
