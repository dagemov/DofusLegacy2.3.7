using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;

namespace Sunshine.WorldServer.Game.Spells.Casts.Enutrof
{
    /// <summary>
    /// Handler for Chance spell (ID 42)
    /// Applies buff that triggers another buff when it ends
    /// </summary>
    [SpellCastHandler(42)]
    public class ChanceHandler : SpellCastHandler
    {
        private Effects.Spells.SpellEffectHandler PuissanceBuff;

        public ChanceHandler(FightActor caster, Spell spell, short targetedCell, bool critical) 
            : base(caster, spell, targetedCell, critical)
        {
        }

        public override void Execute()
        {
            var target = Fight.GetOneFighter(TargetedCell);
            if (target != null && Handlers.Length >= 2)
            {
                // Modify targets to ALL
                Handlers[0].TargetType = SpellTargetType.ALL;
                Handlers[1].TargetType = SpellTargetType.ALL;

                // Apply first effect
                Handlers[0].Apply();

                // Store second effect for trigger
                PuissanceBuff = Handlers[1];

                // Add trigger buff that applies power buff when it ends
                int id = target.PopNextBuffId();
                var triggerBuff = new TriggerBuff(
                    id, 
                    target, 
                    Caster, 
                    PuissanceBuff.Effect, 
                    Spell, 
                    BuffTriggerType.BUFF_ENDED, 
                    OnBuffEnded
                )
                {
                    Duration = 1
                };
                target.AddBuff(triggerBuff);
            }
        }

        private void OnBuffEnded(TriggerBuff buff, BuffTriggerType trigger, object token)
        {
            PuissanceBuff.TargetedCell = buff.Target.Position.Cell;
            PuissanceBuff.Apply();
        }
    }
}
