using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2811)]
    public class ReboursHandler : SpellCastHandler
    {
        public ReboursHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
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
                    EffectsEnum.Effect_AddComboDamage,
                    EffectsEnum.Effect_ActivateBomb,
                    EffectsEnum.Effect_1060)
                .ToArray();

            if (bombs.Length == 0)
                return;

            foreach (var bomb in bombs)
            {
                bomb.IncreaseCombo(true);
                bomb.ScheduleDelayedExplosion(1);
            }

            if (Fight != null)
                Game.Fights.Bombs.BombManager.Instance.CheckWalls(Fight, Caster);
        }
    }
}
