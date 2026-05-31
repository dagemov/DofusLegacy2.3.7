using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Context;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class RogueImageFighter : AIFighter, ISummoned
    {
        private bool _alive = true;
        private bool _deathHandled;

        public RogueImageFighter(FightActor summoner, ObjectPosition position)
            : base(ActorManager.Instance.GenerateId(true), summoner.Look, summoner.Level, summoner.Stats.CloneAndChangeOwner(new object()), summoner.Fight)
        {
            Summoner = summoner;
            Position = position;
            Visibility = GameActionFightInvisibilityStateEnum.VISIBLE;
        }

        public FightActor Summoner { get; set; }

        public override bool IsAlive => _alive && Stats != null && Stats.Health.Total > 0 && Summoner != null && Summoner.IsAlive;

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            var fightFighterInformations = Summoner.GetGameFightFighterInformations(client);
            fightFighterInformations.stats = this.GetGameFightMinimalStats(client);
            fightFighterInformations.disposition.cellId = Position.Cell;
            fightFighterInformations.disposition.direction = (sbyte)Position.Direction;
            fightFighterInformations.contextualId = Id;
            fightFighterInformations.stats.invisibilityState = (sbyte)Visibility;
            return fightFighterInformations;
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            var fightFighterInformations = Summoner.GetGameFightFighterPreparationInformations(client);
            fightFighterInformations.stats = this.GetGameFightMinimalStatsPreparation(client);
            fightFighterInformations.contextualId = Id;
            fightFighterInformations.disposition.cellId = Position.Cell;
            fightFighterInformations.disposition.direction = (sbyte)Position.Direction;
            fightFighterInformations.stats.invisibilityState = (sbyte)Visibility;
            return fightFighterInformations;
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            var fightTeamInformations = Summoner.GetFightTeamMemberInformations();
            fightTeamInformations.id = Id;
            return fightTeamInformations;
        }

        private void CleanupFromFight()
        {
            var fight = Fight;
            if (fight == null)
                return;

            fight.TimeLine?.Fighters?.Remove(this);
            fight.TimeLine?.Leavers?.Remove(this);
            fight.Team?.Attackers?.Remove(this);
            fight.Team?.Defenders?.Remove(this);
            fight.RemoveTriggers(this);
        }

        public void Die(FightActor byFighter = null)
        {
            if (_deathHandled || !_alive)
                return;

            _deathHandled = true;
            _alive = false;

            var fight = Fight;
            var summoner = Summoner;
            var killer = byFighter ?? summoner ?? this;

            CleanupFromFight();

            summoner?.RemoveSummon(this);

            base.OnDead(this, killer);

            if (fight != null)
                fight.TimeLine?.Leavers?.Remove(this);

            if (fight != null)
                ContextHandler.SendGameFightTurnListMessage(fight.Clients, fight);
        }

        public override void OnDead(FightActor target, FightActor fighter)
        {
            if (target != this)
            {
                base.OnDead(target, fighter);
                return;
            }

            if (_deathHandled)
                return;

            Die(fighter ?? Summoner ?? this);
        }
    }
}
