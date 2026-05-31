using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Effects.Spells.Moves;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2795)]
    public class BotteHandler : SpellCastHandler
    {
        public BotteHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            var targetedBomb = Fight?.GetOneFighter(TargetedCell) as BombFighter;
            bool friendlyBomb = targetedBomb != null && Caster != null && Caster.IsFriendlyWith(targetedBomb);

            foreach (var handler in Handlers ?? Array.Empty<SpellEffectHandler>())
            {
                if (handler is Push push)
                {
                    push.DamagesDisabled = true;
                    if (friendlyBomb)
                        push.SubRangeForActor = targetedBomb;
                }
                else if (handler is PushBack pushBack)
                {
                    pushBack.DamagesDisabled = true;
                    if (friendlyBomb)
                        pushBack.SubRangeForActor = targetedBomb;
                }
            }
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            foreach (var handler in (Handlers ?? Array.Empty<SpellEffectHandler>()).Where(x => x != null).OrderByDescending(x => x.DiceNum))
                handler.Apply();
        }
    }
}
