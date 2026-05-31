using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects
{
    public class EffectManager : Singleton<EffectManager>
    {
        public Dictionary<EffectsEnum, Func<SpellEffectHandler>> SpellEffects = new Dictionary<EffectsEnum, Func<SpellEffectHandler>>();

        public List<Effect> GetEffects(string hexa)
        {           
            byte[] data = Utils.GetHexaToByteArray(hexa);
            BigEndianReader reader = new BigEndianReader(data);
            List<Effect> effects = new List<Effect>();
            int count = reader.ReadShort();
            for (int i = 0; i < count; i++)
            {
                EffectsEnum id = (EffectsEnum)reader.ReadUInt();
                uint diceNum = reader.ReadUInt();
                uint diceFace = reader.ReadUInt();
                int value = reader.ReadInt();
                int delay = reader.ReadInt();
                int duration = reader.ReadInt();
                SpellTargetType target = (SpellTargetType)reader.ReadInt();
                reader.ReadUTF();
                uint zoneMinSize = reader.ReadUInt();
                uint zoneSize = reader.ReadUInt();
                var zoneShape =  (SpellShapeEnum)reader.ReadUInt();
                reader.ReadBoolean();
                reader.ReadInt();
                reader.ReadInt();
                reader.ReadInt();
                reader.ReadBoolean();
                Effect effect = new Effect(id, diceNum, diceFace, value, delay, duration, target, zoneShape, zoneMinSize, zoneSize);
                effects.Add(effect);
            }
            return effects;
        }

        public List<Effect> GetEffects(byte[] buffer)
        {
            List<Effect> effects = new List<Effect>();

            if (buffer == null || buffer.Length == 0)
                return effects;

            BigEndianReader reader = new BigEndianReader(buffer);

            while (reader.BytesAvailable > 0)
            {
                byte serializationId = reader.ReadByte();

                EffectsEnum id = (EffectsEnum)reader.ReadInt();
                uint random = reader.ReadUInt();
                short duration = reader.ReadShort();
                SpellTargetType target = (SpellTargetType)reader.ReadUShort();
                var zoneShape = (SpellShapeEnum)reader.ReadByte();
                uint zoneSize = reader.ReadByte();

                uint diceNum = 0;
                uint diceFace = 0;
                int value = 0;

                switch (serializationId)
                {
                    case 1:
                        break;

                    case 4:
                        value = reader.ReadShort();
                        diceNum = (uint)Math.Max(0, (int)reader.ReadShort());
                        diceFace = (uint)Math.Max(0, (int)reader.ReadShort());
                        break;

                    case 6:
                        value = reader.ReadShort();
                        break;

                    default:
                        throw new Exception($"Unsupported rollback spell effect serialization id {serializationId}.");
                }

                Effect effect = new Effect(id, diceNum, diceFace, value, (int)random, duration, target, zoneShape, 0, zoneSize);
                effects.Add(effect);
            }

            return effects;
        }

        public List<List<Effect>> GetEffects(string hexa, bool isItemSet)
        {
            byte[] data = Utils.GetHexaToByteArray(hexa);
            BigEndianReader reader = new BigEndianReader(data);
            List<Effect> effects = new List<Effect>();
            List<List<Effect>> effectsTotal = new List<List<Effect>>();

            int countSet = reader.ReadShort();
            for (int y = 0; y < countSet; y++)
            {                        
                int count = reader.ReadShort();
                effects = new List<Effect>();
                for (int i = 0; i < count; i++)
                {                   
                    EffectsEnum id = (EffectsEnum)reader.ReadUInt();
                    int value = reader.ReadInt();
                    int delay = reader.ReadInt();
                    int duration = reader.ReadInt();
                    SpellTargetType target = (SpellTargetType)reader.ReadInt();
                    reader.ReadUTF();
                    uint zoneMinSize = reader.ReadUInt();
                    uint zoneSize = reader.ReadUInt();
                    var zoneShape = (SpellShapeEnum)reader.ReadUInt();
                    reader.ReadBoolean();
                    reader.ReadInt();
                    reader.ReadInt();
                    reader.ReadInt();
                    reader.ReadBoolean();
                    Effect effect = new Effect(id, 0, 0, value, delay, duration, target, zoneShape, zoneMinSize, zoneSize);
                    effects.Add(effect);
                }
                if(effects.Count != 0)
                    effectsTotal.Add(effects);
            }
            return effectsTotal;
        }

        public string SetEffects(IDataWriter writer, List<Effect> effects)
        {
            writer.WriteShort((short)effects.Count);
            foreach (var effect in effects)
            {
                writer.WriteUInt((uint)effect.Id);
                writer.WriteUInt(effect.DiceNum);
                writer.WriteUInt(effect.DiceFace);
                writer.WriteInt(effect.Value);
                writer.WriteInt(effect.Delay);
                writer.WriteInt(effect.Duration);
                writer.WriteInt((int)effect.Target);
                writer.WriteUTF("null zone");
                writer.WriteUInt(effect.ZoneMinSize);
                writer.WriteUInt(effect.ZoneSize);
                writer.WriteUInt((uint)effect.ZoneShape);
                writer.WriteBoolean(false);
                writer.WriteInt(0);
                writer.WriteInt(0);
                writer.WriteInt(0);
                writer.WriteBoolean(false);
            }
            return BitConverter.ToString(writer.Data)?.Replace("-", "");
        }


        public IEnumerable<FightActor> GetAffectedActors(FightActor caster, Effect effect, short targetedCell)
        {
            List<FightActor> actors = new List<FightActor>();
            var zone = new Zone(effect.ZoneShape, (byte)effect.ZoneSize, (byte)effect.ZoneMinSize)
            {
                Direction = ResolveZoneDirection(caster, targetedCell)
            };
            var affectedCells = zone.GetCells(targetedCell, caster.Map);

            foreach (short cell in affectedCells)
            {
                FightActor target = caster.Fight.GetOneFighter(cell);
                switch (effect.Target)
                {
                    case SpellTargetType.ALLY_ALL:
                        if (target != null && target.IsFriendlyWith(caster))
                            actors.Add(target);
                        break;

                    case SpellTargetType.ENEMY_ALL:
                        if (target != null && target.IsEnnemyWith(caster))
                            actors.Add(target);
                        break;

                    case (SpellTargetType)3840:
                        if (target != null && target != caster)
                            actors.Add(target);
                        break;

                    case SpellTargetType.ONLY_SELF:
                        if (target != null && target == caster)
                            actors.Add(target);
                        break;

                    default:
                        if (target != null)
                            actors.Add(target);
                        break;
                }
            }
            return actors;
        }

        private static DirectionsEnum ResolveZoneDirection(FightActor caster, short targetedCell)
        {
            if (caster?.Position?.Point == null)
                return DirectionsEnum.DIRECTION_SOUTH_EAST;

            if (caster.Position.Cell == targetedCell)
                return caster.Position.Direction;

            try
            {
                return caster.Position.Point.OrientationTo(MapPoint.GetPoint(targetedCell), true);
            }
            catch
            {
                return caster.Position.Direction;
            }
        }
    }
}
