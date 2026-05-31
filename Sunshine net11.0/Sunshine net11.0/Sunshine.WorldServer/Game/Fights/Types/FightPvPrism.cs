using Sunshine.MySql.Database.World.Maps.Prisms;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Results;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Sunshine.WorldServer.Game.Fights.Types
{
    public class FightPvPrism : Fight
    {
        public FightPvPrism(int id, Character leader, WorldMapPrismRecord prismRecord)
        {
            Id = id;
            Leader = leader;
            PrismRecord = prismRecord;
            Map = leader.Map;
            Map.Fights.Add(this);
            Clients = new List<WorldClient>();
            DefendersQueue = new List<Character>();
            Team = new FightTeam(this);
            TimeLine = new TimeLine(this);
            State = FightStateEnum.Placement;
            MaxMemberCount = 8;
            FightTime = DateTime.Now;
            Results = new FightResults(this);
            CurrentPlacementPhaseDuration = AttackersPlacementPhaseTime;
            StartAction(GetPlacementTimeLeft(), "TeleportPlayers");
        }

        public int AttackersPlacementPhaseTime = 30000;
        public int DefendersPlacementPhaseTime = 10000;

        public override int Id { get; }
        public override Map Map { get; }
        public override FightTeam Team { get; }
        public override FightTypeEnum Type => FightTypeEnum.FIGHT_TYPE_MXvM;
        public override Timer Timer { get; set; }

        public DateTime FightTime { get; set; }
        public int CurrentPlacementPhaseDuration { get; set; }
        public WorldMapPrismRecord PrismRecord { get; }
        public PrismFighter PrismFighter { get; set; }
        public List<Character> DefendersQueue { get; }

        public override FightCommonInformations GetFightCommonInformations
            => new FightCommonInformations(Id, (sbyte)Type,
                new List<FightTeamInformations> { Team.GetFightTeamInformations(true), Team.GetFightTeamInformations(false) },
                new List<short>() { Team.BladePosition(true), Team.BladePosition(false) },
                new List<FightOptionsInformations>() { Team.GetFightOptionsInformations(true), Team.GetFightOptionsInformations(false) });

        public override void AddFighter(FightActor fighter, bool isAttacker = false)
        {
            if (fighter == null)
                return;

            Map.EnsureFightCells();

            if (fighter is CharacterFighter characterFighter && ContainsCharacter(characterFighter.Character.Id))
                return;

            WorldClient client = null;
            if (State != FightStateEnum.Placement || Team.IsFull(isAttacker))
                return;

            TimeLine.AddFighter(fighter);
            if (fighter is CharacterFighter)
            {
                client = (fighter as CharacterFighter).Character.Client;
                if (!Clients.Contains(client))
                    Clients.Add(client);

                Map.LeaveActor(client.Character);
                if (isAttacker)
                    Team.AddAttacker(fighter);
                else
                    Team.AddDefender(fighter);

                fighter.GeneratePosition();
                EnterFighter(client, fighter);
            }
            else
            {
                Team.AddDefender(fighter);

                if (fighter is PrismFighter || fighter.Position == null || Map.BlueCells == null || !Map.BlueCells.Contains(fighter.Position.Cell) || !IsCellFree(fighter.Position.Cell))
                    fighter.GeneratePosition();

                ContextHandler.SendGameFightShowFighterPreparationMessage(Clients, new[] { fighter });
                ContextHandler.SendGameEntitiesDispositionMessage(Clients, new[] { fighter });
                ContextHandler.SendGameFightUpdateTeamMessage(Clients, Team);
                ContextHandler.SendGameFightTurnListMessage(Clients, this);
            }

            UpdateDirection();
            if (Team.Attackers.Count >= 1 && Team.Defenders.Count >= 1)
                ShowBlades();
        }

        public bool AddDefender(Character character, out FighterRefusedReasonEnum reason)
        {
            reason = FighterRefusedReasonEnum.FIGHTER_ACCEPTED;

            if (State != FightStateEnum.Placement)
            {
                reason = FighterRefusedReasonEnum.TOO_LATE;
                return false;
            }

            if (character == null || character.Client == null)
            {
                reason = FighterRefusedReasonEnum.WRONG_MAP;
                return false;
            }

            if (character.IsInFight())
            {
                reason = FighterRefusedReasonEnum.IM_OCCUPIED;
                return false;
            }

            if (DefendersQueue.Count >= 7)
            {
                reason = FighterRefusedReasonEnum.TEAM_FULL;
                return false;
            }

            if (DefendersQueue.Contains(character))
                DefendersQueue.Remove(character);

            DefendersQueue.Add(character);
            return true;
        }

        public bool RemoveDefender(Character character)
        {
            if (State != FightStateEnum.Placement)
                return false;

            return DefendersQueue.Remove(character);
        }

        public void TeleportDefendersQueue()
        {
            if (State != FightStateEnum.Placement)
                return;

            foreach (var defender in DefendersQueue.ToList())
            {
                if (defender == null || defender.IsInFight())
                    continue;

                if (defender.Map.Id != Map.Id)
                    defender.Teleport(Map.Id, PrismRecord.CellId);

                defender.SetFight(FightTypeEnum.FIGHT_TYPE_MXvM, this);
                AddFighter(defender.Fighter = new CharacterFighter(defender));
            }

            DefendersQueue.Clear();
            FightTime = DateTime.Now;
            CurrentPlacementPhaseDuration = DefendersPlacementPhaseTime;
            StartAction(DefendersPlacementPhaseTime, "StartFight");
        }

        public override void CheckAllStatus()
        {
        }

        public int GetTimeLeftBeforeFight()
        {
            return Math.Max(0, GetPlacementTimeLeft() / 100);
        }

        public int GetWaitTimeForPlacement()
        {
            return DefendersPlacementPhaseTime / 100;
        }

        public int GetDefendersLeftSlot()
        {
            var left = 7 - DefendersQueue.Count;
            return left > 0 ? left : 0;
        }

        public override int GetPlacementTimeLeft(FightActor fighter = null)
        {
            double elapsed = (DateTime.Now - FightTime).TotalMilliseconds;
            double remaining = CurrentPlacementPhaseDuration - elapsed;
            if (remaining < 0)
                remaining = 0;

            return (int)remaining;
        }
    }
}
