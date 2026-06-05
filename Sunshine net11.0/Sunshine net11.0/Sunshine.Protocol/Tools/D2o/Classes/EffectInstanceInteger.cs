using System;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("EffectInstanceInteger", "com.ankamagames.dofus.datacenter.effects.instances", true)]
    [Serializable]
    public class EffectInstanceInteger : IDataObject
    {
        public int effectId;
        public int duration;
        public bool hidden;
        public int random;
        public int value;
        public int targetId;
        public int zoneShape;
        public int zoneSize;
    }
}
