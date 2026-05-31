using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_Lancer)]
    public class Thrower : SpellEffectHandler
    {
        public override void Apply()
        {
            if (!Caster.IsCarrying)
                return;

            FightActor target = Caster.Carrying;
            if (target == null)
                return;

            target.Position.Cell = TargetedCell;

            if (target is BombFighter movedBomb)
                BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

            ActionsHandler.SendGameActionFightThrowCharacterMessage(Fight.Clients, Caster, target, TargetedCell);

            Fight.TriggerMarks(target.Position.Cell, target, TriggerTypeEnum.MOVE);

            Caster.Carrying = null;
            target.CarryingBy = null;

            foreach (var buff in Caster.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carrying).ToArray())
                Caster.RemoveBuff(buff);

            foreach (var buff in target.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carried).ToArray())
                target.RemoveBuff(buff);

            if (Caster.HasState(SpellStatesEnum.Carrying))
                Caster.RemoveState(SpellStatesEnum.Carrying);

            if (target.HasState(SpellStatesEnum.Carried))
                target.RemoveState(SpellStatesEnum.Carried);
        }
    }
}
