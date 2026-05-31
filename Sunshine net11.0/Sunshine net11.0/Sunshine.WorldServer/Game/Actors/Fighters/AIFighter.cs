using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.BaseServer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI
{
    public class AIFighter : FightActor
    {
        public AIFighter(int id, ActorLook look, short level, StatsFields stats, Fight fight)
        {
            Id = id;
            Look = look;
            Level = level;
            Stats = stats;
            Fight = fight;
            Visibility = GameActionFightInvisibilityStateEnum.VISIBLE;
            SpellHistory = new SpellHistory(this);
            FightLoot = new Fights.Results.FightLoot(this);
        }

        public override int Id { get; set; }

        public override ActorLook Look { get; set; }

        public override Fight Fight { get; }

        public override FightTeam Team { get { return Fight.Team; } }

        public override bool IsAttacker() { return Fight != null ? Fight.Team.Attackers.Contains(this) : false; }

        public override short Level { get; }

        public override StatsFields Stats { get; set; }

        public override SpellHistory SpellHistory { get; set; }

        public override GameActionFightInvisibilityStateEnum Visibility { get; set; }

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            return new GameFightFighterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStats(client));
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightFighterInformations(
                Id,
                Look.GetEntityLook(),
                GetEntityDispositionInformations(client),
                IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER,
                IsAlive,
                GetGameFightMinimalStatsPreparation(client));
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberInformations(Id);
        }

        public override SpellCastResult CanCastSpell(Spell spell, short cell)
        {
            if (!IsFighterTurn() || IsDead())
                return SpellCastResult.CANNOT_PLAY;

            if ((long)Stats.AP.TotalMax < (long)((ulong)spell.Template.ApCost))
                return SpellCastResult.NOT_ENOUGH_AP;

            if (HasReachedBombPlacementLimit(spell))
                return SpellCastResult.UNKNOWN;

            bool cellIsFree = Fight.IsCellFree(cell);
            if ((spell.Template.NeedFreeCell && !cellIsFree) || (spell.Template.NeedTakenCell && cellIsFree))
                return SpellCastResult.CELL_NOT_FREE;

            if (spell.StatesForbidden.Any(x => HasState((SpellStatesEnum)x)))
                return SpellCastResult.STATE_FORBIDDEN;

            if (spell.StatesRequired.Any(x => !HasState((SpellStatesEnum)x)))
                return SpellCastResult.STATE_REQUIRED;

            short[] castZone = GetCastZone(spell.Template);
            if (!castZone.Contains(cell))
                return SpellCastResult.NOT_IN_ZONE;

            return SpellCastResult.OK;
        }

        private static int TurnStartDelayMs => Math.Max(0, GameConfig.GetInt("MonsterTurnStartDelayMs", 100));

        private static int TurnEndDelayMs => Math.Max(0, GameConfig.GetInt("MonsterTurnEndDelayMs", 80));

        public void PlayAI()
        {
            _ = PlayAIAsync();
        }

        public async Task PlayAIAsync()
        {
            try
            {
                if (!IsAlive || Fight == null || Fight.State != FightStateEnum.Fighting || !IsFighterTurn())
                    return;

                int startDelay = TurnStartDelayMs;
                if (startDelay > 0)
                    await Task.Delay(startDelay);

                if (!IsAlive || Fight == null || Fight.State != FightStateEnum.Fighting || !IsFighterTurn())
                    return;

                if (this is TaxCollectorFighter)
                {
                    var taxCollectorFighter = this as TaxCollectorFighter;
                    AIDispatcher.Dispatch(AIEnum.RUNNER, taxCollectorFighter.TaxCollector, taxCollectorFighter);
                }
                else if (this is IMonster monsterFighter)
                {
                    Monster monster = monsterFighter.Monster;
                    if (monster != null && monster.CanPlay())
                        await MonsterAttackAI.PlayAsync(monster, this);
                }

                int endDelay = TurnEndDelayMs;
                if (endDelay > 0)
                    await Task.Delay(endDelay);

                if (IsAlive && Fight != null && Fight.State == FightStateEnum.Fighting && IsFighterTurn())
                    EndTurn();
            }
            catch
            {
            }
        }
    }
}
