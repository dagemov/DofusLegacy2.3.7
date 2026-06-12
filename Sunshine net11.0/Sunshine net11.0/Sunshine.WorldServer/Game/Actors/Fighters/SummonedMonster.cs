using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class SummonedMonster : AIFighter, ISummoned, IMonster
    {
        public FightActor Summoner { get; set; }

        public Monster Monster { get; set; }

        public SummonedMonster(Monster monster, FightActor summoner, ObjectPosition position)
            : base(ActorManager.Instance.GenerateId(true), monster.Look, monster.Grade.Level, monster.Stats.CloneAndChangeOwner(monster), summoner.Fight)
        {
            Monster = monster;
            Summoner = summoner;
            Position = position;

            // Stat scaling (Dofus 2.x): Summons gain % of summoner's stats
            ScaleStats();
            NormalizeFightHealth(true);
        }

        private void ScaleStats()
        {
            // Simple scaling: add 50% of summoner's stats (excluding HP)
            // In a real 2.3.7 server, this depends on the spell level and summoner's stats
            foreach (StatsEnum stat in typeof(StatsEnum).GetEnumValues())
            {
                if (stat == StatsEnum.Health || stat == StatsEnum.Vitality || stat == StatsEnum.WaterDamageArmor || stat == StatsEnum.FireDamageArmor
                    || stat == StatsEnum.EarthDamageArmor || stat == StatsEnum.AirDamageArmor || stat == StatsEnum.NeutralDamageArmor)
                    continue;

                Stats[stat].Context += (short)(Summoner.Stats[stat].TotalMax * 0.5);
            }

            // HP scaling is often different (e.g., base HP + % of summoner's HP)
            Stats.Health.Base += (int)(Summoner.Stats.Health.TotalMax * 0.2);
        }

        public override bool IsAlive { get { return base.IsAlive; } }

        public override GameFightMinimalStats GetGameFightMinimalStats(WorldClient client = null)
        {
            var stats = base.GetGameFightMinimalStats(client);
            stats.summoner = Summoner?.Id ?? 0;
            return stats;
        }

        public override GameFightMinimalStatsPreparation GetGameFightMinimalStatsPreparation(WorldClient client = null)
        {
            var stats = base.GetGameFightMinimalStatsPreparation(client);
            stats.summoner = Summoner?.Id ?? 0;
            return stats;
        }

        public virtual bool CanPlayTurn => Monster?.Record?.CanPlay ?? true;

        public bool DiesAtTurnEnd => Monster?.Record != null && !Monster.Record.UseSummonSlot;

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            return new GameFightMonsterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStats(client),
                (short)Monster.Record.Id,
                (sbyte)Monster.Grade.GradeId);
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightMonsterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStatsPreparation(client),
                (short)Monster.Record.Id,
                (sbyte)Monster.Grade.GradeId);
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberMonsterInformations(Id, Monster.Record.Id, (sbyte)Monster.Grade.GradeId);
        }

        public virtual void Die(FightActor byFighter = null)
        {
            FightCombatLogger.LogSummonDie(Fight, this, byFighter ?? this);
            OnDead(this, byFighter == null ? this : byFighter);
        }

        public override void OnDead(FightActor target, FightActor fighter)
        {
            if (target != this)
            {
                base.OnDead(target, fighter);
                return;
            }

            base.OnDead(target, fighter);
            Summoner?.RemoveSummon(this);
        }
    }
}
