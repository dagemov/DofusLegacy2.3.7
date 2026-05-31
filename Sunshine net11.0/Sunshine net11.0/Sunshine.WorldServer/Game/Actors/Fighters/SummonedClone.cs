using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class SummonedClone : AIFighter, ISummoned
    {
        public FightActor Summoner { get; set; }

        public SummonedClone(int id, FightActor summoner, StatsFields stats, ObjectPosition position)
            : base(id, summoner.Look, summoner.Level, stats, summoner.Fight)
        {
            Summoner = summoner;
            Position = position;
            Visibility = GameActionFightInvisibilityStateEnum.VISIBLE;
        }

        public override bool IsAlive { get { return this.Summoner.Stats.Health.Total > 0; } }

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            GameFightFighterInformations fightFighterInformations = Summoner.GetGameFightFighterInformations(client);
            fightFighterInformations.stats = this.GetGameFightMinimalStats(client);
            fightFighterInformations.disposition.cellId = Position.Cell;
            fightFighterInformations.disposition.direction = (sbyte)Position.Direction;
            fightFighterInformations.contextualId = Id;
            fightFighterInformations.stats.invisibilityState = (sbyte)Visibility;
            return fightFighterInformations;
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            GameFightFighterInformations fightFighterInformations = Summoner.GetGameFightFighterPreparationInformations(client);
            fightFighterInformations.stats = this.GetGameFightMinimalStatsPreparation(client);
            fightFighterInformations.contextualId = Id;
            fightFighterInformations.disposition.cellId = Position.Cell;
            fightFighterInformations.disposition.direction = (sbyte)Position.Direction;
            fightFighterInformations.stats.invisibilityState = (sbyte)Visibility;
            return fightFighterInformations;
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            FightTeamMemberInformations fightTeamInformations = Summoner.GetFightTeamMemberInformations();
            fightTeamInformations.id = Id;
            return fightTeamInformations;
        }

        public void Die(FightActor byFighter = null)
        {
            base.OnDead(this, byFighter == null ? this : byFighter);
            this.Summoner.RemoveSummon(this);
        }
    }
}
