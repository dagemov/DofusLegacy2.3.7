using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    /// <summary>
    /// Handler for Remission spell (ID 2809)
    /// Pushes back attackers or teleports bombs
    /// </summary>
    [SpellCastHandler(2809)]
    public class RemissionHandler : SpellCastHandler
    {
        private Effects.Spells.SpellEffectHandler BuffRafoulage;
        private Effects.Spells.SpellEffectHandler PushBack;

        public RemissionHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            if (Fight == null || Handlers == null || Handlers.Length < 2)
                return;

            var target = Fight.GetOneFighter(TargetedCell);
            if (target != null)
            {
                if (target is BombFighter)
                {
                    // Teleport bomb
                    Handlers[1].TargetedCell = TargetedCell;
                    Handlers[1].Apply();
                }
                else
                {
                    // Add trigger buff to push back attackers
                    BuffRafoulage = Handlers[0];
                    PushBack = Handlers[1];

                    int id = target.PopNextBuffId();
                    var triggerBuff = new TriggerBuff(
                        id, 
                        target, 
                        Caster, 
                        BuffRafoulage.Effect, 
                        Spell, 
                        BuffTriggerType.BEFORE_ATTACKED, 
                        OnBeforeAttacked
                    )
                    {
                        Duration = 1
                    };
                    target.AddBuff(triggerBuff);
                }
            }
        }

        private void OnBeforeAttacked(TriggerBuff buff, BuffTriggerType trigger, object token)
        {
            if (token is Damage damage &&
                buff?.Target != null &&
                buff.Target.Position != null &&
                damage.Source != null &&
                damage.Source.Position != null &&
                PushBack != null &&
                !(buff.Target is BombFighter) &&
                !(damage.Source is BombFighter) &&
                damage.Source.Position.Point.IsAdjacentTo(buff.Target.Position.Point))
            {
                PushBack.TargetedCell = damage.Source.Position.Cell;
                PushBack.Apply();
            }
        }
    }
}
