using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2805)]
    public class PoudreHandler : SpellCastHandler
    {
        public PoudreHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            var bombs = RogueBombSpellHelper.GetAffectedFriendlyBombs(
                    Caster,
                    Spell,
                    Critical,
                    TargetedCell,
                    EffectsEnum.Effect_AddState)
                .ToArray();

            if (bombs.Length == 0)
                return;

            var unmovableEffect = RogueBombSpellHelper.GetFirstEffect(
                Spell,
                Critical,
                EffectsEnum.Effect_AddState,
                x => x.Value == (int)SpellStatesEnum.Unmovable);

            short duration = unmovableEffect != null && unmovableEffect.Duration > 0
                ? (short)unmovableEffect.Duration
                : (short)2;

            foreach (var bomb in bombs)
                RogueBombSpellHelper.ApplyOrRefreshState(Caster, bomb, Spell, unmovableEffect, SpellStatesEnum.Unmovable, duration);
        }
    }
}
