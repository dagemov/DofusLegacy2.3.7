using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Spells
{
    public class Effect
    {
        public EffectsEnum Id;
        public uint DiceNum;
        public uint DiceFace;
        public int Value;
        public int Delay;
        public int Duration;
        public SpellTargetType Target;
        public SpellShapeEnum ZoneShape;
        public uint ZoneMinSize;
        public uint ZoneSize;

        public Effect()
        {
        }

        public Effect(EffectsEnum id, uint diceNum, uint diceFace, int value, int delay, int duration, 
            SpellTargetType target, SpellShapeEnum zoneShape, uint zoneMinSize, uint zoneSize)
        {
            this.Id = id;
            this.DiceNum = diceNum;
            this.DiceFace = diceFace;
            this.Value = value;
            this.Delay = delay;
            this.Duration = duration;
            this.Target = target;
            this.ZoneShape = zoneShape;
            this.ZoneMinSize = zoneMinSize;
            this.ZoneSize = zoneSize;
        }

        public void GenerateEffect(bool isJetMax = false)
        {
            AsyncRandom rdn = new AsyncRandom();

            if (DiceNum > DiceFace)
                Value = (int)DiceNum;
            else
                Value = isJetMax ? (int)DiceFace : rdn.Next((int)DiceNum, (int)DiceFace + 1);
        }

        public ObjectEffect GetObjectEffect()
        {
            return new ObjectEffect((short)Id);
        }

        public ObjectEffectInteger GetObjectEffectInteger()
        {
            return new ObjectEffectInteger((short)Id, (short)Value);
        }

        public ObjectEffectMinMax GetObjectEffectMinMax()
        {
            return new ObjectEffectMinMax((short)Id, (short)DiceNum, (short)DiceFace);
        }

        public Effect Clone()
        {
            return (Effect)this.MemberwiseClone();
        }
    }
}
