using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Pandawa
{
    [SpellCastHandler((int)SpellIdEnum.Karcham)]
    public class KarchamHandler : SpellCastHandler
    {
        public KarchamHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (!m_initialized)
                Initialize();

            var target = Fight?.GetOneFighter(TargetedCell);
            if (target == null || target == Caster)
                return;

            if (Caster.IsCarrying || Caster.IsCarried || target.IsCarrying || target.IsCarried)
                return;

            if (target.Look?.BonesID == 842)
                return;

            if (target.HasState(SpellStatesEnum.Heavy) || target.HasState(SpellStatesEnum.Unmovable))
                return;

            if (target is MonsterFighter monster && monster.Monster?.Record?.Id == 2877)
                return;

            foreach (var handler in Handlers ?? Enumerable.Empty<Effects.Spells.SpellEffectHandler>())
            {
                handler.TargetedCell = TargetedCell;
                handler.Apply();
            }
        }
    }
}