using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_Porter)]
    public class Carrier : SpellEffectHandler
    {
        public override void Apply()
        {
            FightActor target = GetAffectedActors().FirstOrDefault();
            if (target == null || target == Caster)
                return;

            if (Caster.IsCarrying || Caster.IsCarried || target.IsCarrying || target.IsCarried)
                return;

            target.Position.Cell = Caster.Position.Cell;
            Caster.Carrying = target;
            target.CarryingBy = Caster;

            foreach (var buff in Caster.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carrying).ToArray())
                Caster.RemoveBuff(buff);

            foreach (var buff in target.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carried).ToArray())
                target.RemoveBuff(buff);

            if (Caster.HasState(SpellStatesEnum.Carrying))
                Caster.RemoveState(SpellStatesEnum.Carrying);

            if (target.HasState(SpellStatesEnum.Carried))
                target.RemoveState(SpellStatesEnum.Carried);

            Caster.AddBuff(new StateBuff(Caster, Caster, Spell, Effect, 1000, false, SpellStatesEnum.Carrying));
            target.AddBuff(new StateBuff(Caster, target, Spell, Effect, 1000, false, SpellStatesEnum.Carried));

            if (target is BombFighter movedBomb)
                BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

            ActionsHandler.SendGameActionFightCarryCharacterMessage(Fight.Clients, Caster, target);

            if (Caster is CharacterFighter casterCharacter)
                casterCharacter.Character?.RefreshStats();

            if (target is CharacterFighter targetCharacter)
                targetCharacter.Character?.RefreshStats();
        }
    }
}
