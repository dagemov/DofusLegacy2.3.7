using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Others
{
    [EffectHandler(EffectsEnum.Effect_LaisseSpirituel)]
    public class SpiritualLeash : SpellEffectHandler
    {
        private FightActor _revivedActor;

        public override void Apply()
        {
            if (Caster == null || Fight == null || Fight.TimeLine == null)
                return;

            var actor = GetLastDeadFriendlyFighter();
            if (actor == null)
                return;

            int healPercent = Value > 0 ? Value : (int)(DiceNum > 0 ? DiceNum : 1);
            ReviveActor(actor, healPercent);
        }

        private FightActor GetLastDeadFriendlyFighter()
        {
            if (Fight.TimeLine?.Leavers == null)
                return null;

            for (int i = Fight.TimeLine.Leavers.Count - 1; i >= 0; i--)
            {
                var actor = Fight.TimeLine.Leavers[i];
                if (actor != null && actor.IsDead() && actor.IsFriendlyWith(Caster))
                    return actor;
            }

            return null;
        }

        private void ReviveActor(FightActor actor, int healPercent)
        {
            short reviveCell = ResolveReviveCell();
            if (reviveCell < 0)
                return;

            foreach (StatsEnum stat in Enum.GetValues(typeof(StatsEnum)))
                actor.Stats[stat].Context = 0;

            actor.Stats.Health.PermanentTaken = 0;
            actor.Stats.Health.Taken = actor.Stats.Health.TotalMax;
            HealHpPercent(actor, healPercent);
            actor.Position = new ObjectPosition(Fight.Map, reviveCell, Caster.Position != null ? Caster.Position.Direction : DirectionsEnum.DIRECTION_EAST);

            Fight.TimeLine.Leavers.Remove(actor);
            Fight.TimeLine.Fighters.Remove(actor);

            int casterIndex = Fight.TimeLine.Fighters.IndexOf(Caster);
            if (casterIndex >= 0)
                Fight.TimeLine.Fighters.Insert(casterIndex + 1, actor);
            else
                Fight.TimeLine.AddFighter(actor);

            if (actor is SummonedMonster summonedMonster)
            {
                summonedMonster.Summoner = Caster;
                Caster.AddSummon(summonedMonster);
            }
            else if (actor is ISummoned summoned)
            {
                Caster.AddSummon(summoned);
            }

            ActionsHandler.SendGameActionFightReviveMessage(Fight.Clients, Caster, actor);
            ContextHandler.SendGameFightTurnListMessage(Fight.Clients, Fight);

            _revivedActor = actor;
            Caster.Dead -= OnCasterDead;
            Caster.Dead += OnCasterDead;
        }

        private short ResolveReviveCell()
        {
            if (Fight.IsCellFree(TargetedCell))
                return TargetedCell;

            var point = MapPoint.GetPoint(TargetedCell);
            if (point != null)
            {
                var adjacent = point.GetAdjacentCells(cell => Fight.IsCellFree(cell)).FirstOrDefault();
                if (adjacent != null)
                    return adjacent.CellId;
            }

            if (Caster.Position != null && Fight.IsCellFree(Caster.Position.Cell))
                return Caster.Position.Cell;

            return -1;
        }

        private void HealHpPercent(FightActor actor, int percent)
        {
            var healAmount = (int)(actor.Stats.Health.TotalMax * (percent / 100d));
            if (healAmount <= 0)
                healAmount = 1;

            actor.Heal(healAmount, Caster, false);
        }

        private void OnCasterDead(FightActor actor, FightActor killer)
        {
            Caster.Dead -= OnCasterDead;

            if (_revivedActor == null || !_revivedActor.IsAlive)
                return;

            var source = killer ?? actor;
            if (_revivedActor is SlaveFighter slave)
                slave.Die(source);
            else if (_revivedActor is SummonedMonster summonedMonster)
                summonedMonster.Die(source);
            else
                _revivedActor.OnDead(_revivedActor, source);
        }
    }
}
