using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Effects.Spells.Heals
{
	[EffectHandler(EffectsEnum.Effect_RestoreHPPercent)]
	public class RestoreHpPercent : SpellEffectHandler
	{
		public override void Apply()
		{
			foreach (FightActor current in GetAffectedActors())
			{
                Effect.GenerateEffect();
                if (Duration > 0)
				{
					// trigger
				}
				else
				{
                    int healPoints = (int)((double)current.Stats.Health.TotalMax * ((double)Effect.Value / 100.0));
                    current.Heal(healPoints, Caster, false);
				}
			}
		}
	}
}
