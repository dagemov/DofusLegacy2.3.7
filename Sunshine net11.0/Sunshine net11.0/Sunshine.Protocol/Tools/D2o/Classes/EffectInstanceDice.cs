using System;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("EffectInstanceDice", "com.ankamagames.dofus.datacenter.effects.instances", true)]
    [Serializable]
    public class EffectInstanceDice : IDataObject
    {
        public int effectId;
        public int diceNum;
        public int duration;
        public bool hidden;
        public int diceSide;
        public int value;
        public int random;
        public int targetId;
        public int zoneSize;
        public int zoneShape;
    }
}
