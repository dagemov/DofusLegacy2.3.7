using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights;

namespace Sunshine.WorldServer.Game.Effects.Spells.Breeds.Rogue
{
    /// <summary>
    /// Legacy helper kept for compatibility. Real bomb detonation is handled by Effect_ActivateBomb.
    /// </summary>
    public class Detonate : Game.Effects.Spells.SpellEffectHandler
    {
        public override void Apply()
        {
            var target = Fight.GetOneFighter(TargetedCell) as BombFighter;
            if (target != null)
                target.Explode(Caster);
        }
    }
}
