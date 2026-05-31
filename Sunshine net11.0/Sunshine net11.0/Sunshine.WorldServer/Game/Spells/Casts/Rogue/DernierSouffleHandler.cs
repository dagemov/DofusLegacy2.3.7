using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    [SpellCastHandler(2810)]
    public class DernierSouffleHandler : SpellCastHandler
    {
        public DernierSouffleHandler(FightActor caster, Spell spell, short targetedCell, bool critical)
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (Fight == null || Handlers == null || Handlers.Length == 0)
                return;

            if (Handlers.Length > 3)
            {
                Handlers[3].TargetedCell = Caster.Position.Cell;
                Handlers[3].Apply();
            }

            var allies = Fight.GetAllFighters()
                .Where(x => x.IsAlive && x != Caster && Caster.IsFriendlyWith(x));

            foreach (var target in allies)
            {
                if (target is BombFighter bomb)
                {
                    bool comboAppliedDirectly = false;

                    for (int i = 0; i < Handlers.Length && i < 2; i++)
                    {
                        var handler = Handlers[i];
                        if (handler == null)
                            continue;

                        if (!comboAppliedDirectly && handler.Id == EffectsEnum.Effect_AddComboDamage)
                        {
                            bomb.IncreaseCombo(true);
                            comboAppliedDirectly = true;
                            continue;
                        }

                        handler.TargetedCell = target.Position.Cell;
                        handler.Apply();
                    }

                    if (!comboAppliedDirectly)
                        bomb.IncreaseCombo(true);
                }
                else
                {
                    if (Handlers.Length > 2)
                    {
                        Handlers[2].TargetedCell = target.Position.Cell;
                        Handlers[2].Apply();
                    }
                }
            }
        }
    }
}
