using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Summon
{
    [EffectHandler(EffectsEnum.Effect_ActivateBomb)]
    public class ActivateBomb : SpellEffectHandler
    {
        public override void Apply()
        {
            var bomb = Fight?.GetAllFighters()
                .OfType<BombFighter>()
                .FirstOrDefault(x => x != null && x.IsAlive && x.Position.Cell == TargetedCell);

            if (bomb == null || bomb.Summoner != Caster)
                return;

            if (bomb.WasJustSummonedForSameAction(Caster))
                return;

            bomb.Explode(Caster);
        }
    }
}
