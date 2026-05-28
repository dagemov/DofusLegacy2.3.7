using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Basic;

namespace Sunshine.WorldServer.Game.Effects.Spells.Marks
{
	[EffectHandler(EffectsEnum.Effect_Trap)]
	public class TrapSpawn : SpellEffectHandler
	{
		public override void Apply()
		{
			if (!SpellManager.Instance.Spells.ContainsKey((int)Effect.DiceNum))
            {
				Logs.Logger.WriteError(string.Format("Cannot find trap spell id = {0}, level = {1}. Casted Spell = {2}", Effect.DiceNum, Effect.DiceFace, Spell.Id));
                return;
			}
			else
			{
                Spell spell = SpellManager.Instance.Spells[(int)Effect.DiceNum][(int)Effect.DiceFace - 1];
                if (base.Fight.GetTriggers(base.TargetedCell).Length > 0)
				{
					if (base.Caster is CharacterFighter)
						BasicHandler.SendTextInformationMessage((base.Caster as CharacterFighter).Character.Client, TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 229);
                    return;
				}
				else
				{
					Trap trigger = Effect.ZoneShape == SpellShapeEnum.Q ? new Trap((short)Fight.PopNextTriggerId(), Caster, Effect, Spell, spell.Effects[0], spell, TargetedCell, GameActionMarkCellsTypeEnum.CELLS_CROSS, (byte)Effect.ZoneSize) : 
                                                                          new Trap((short)Fight.PopNextTriggerId(), Caster, Effect, Spell, spell.Effects[0], spell, TargetedCell, (byte)Effect.ZoneSize);
					base.Fight.AddTrigger(trigger);
				}
			}
		}

		public override bool RequireSilentCast()
		{
			return true;
		}
	}
}
