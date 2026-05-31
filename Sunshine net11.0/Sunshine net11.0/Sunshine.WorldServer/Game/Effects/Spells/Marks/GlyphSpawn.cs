using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Basic;

namespace Sunshine.WorldServer.Game.Effects.Spells.Marks
{
    [EffectHandler(EffectsEnum.Effect_Glyph_402), EffectHandler(EffectsEnum.Effect_Glyph)]
    public class GlyphSpawn : SpellEffectHandler
    {
        public override void Apply()
        {
            if (!SpellManager.Instance.Spells.ContainsKey((int)Effect.DiceNum))
            {
                Logs.Logger.WriteError(string.Format("Cannot find glyph spell id = {0}, level = {1}. Casted Spell = {2}", Effect.DiceNum, Effect.DiceFace, Spell.Id));
                return;
            }
            else
            {
                Spell spell = SpellManager.Instance.Spells[(int)Effect.DiceNum][(int)Effect.DiceFace - 1];

                Glyph trigger = Effect.ZoneShape == SpellShapeEnum.Q ? new Glyph((short)Fight.PopNextTriggerId(), Caster, Effect, Spell, spell.Effects[0], spell, TargetedCell, GameActionMarkCellsTypeEnum.CELLS_CROSS, (byte)Effect.ZoneSize) :
                                                                      new Glyph((short)Fight.PopNextTriggerId(), Caster, Effect, Spell, spell.Effects[0], spell, TargetedCell, (byte)Effect.ZoneSize);
                base.Fight.AddTrigger(trigger);

            }
        }

        public override bool RequireSilentCast()
        {
            return false;
        }
    }
}
