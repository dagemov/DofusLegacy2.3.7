using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Results;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Fights.Types
{
    public class FightPvT : Fight
    {
        public FightPvT(int id, Character leader)
        {
            Id = id;
            Leader = leader;
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
            StartAction(GetPlacementTimeLeft(), "TeleportPlayers");
        }

        public int PvTAttackersPlacementPhaseTime = 30000;

        public int PvTDefendersPlacementPhaseTime = 10000;

        public override int Id { get; }

        public override Map Map { get; }

        public override FightTeam Team { get; }

        public override FightTypeEnum Type { get { return FightTypeEnum.FIGHT_TYPE_PvT; } }

        public override Timer Timer { get; set; }

        public DateTime FightTime { get; set; }

        public TaxCollector TaxCollector { get; set; }

        public List<Character> DefendersQueue { get; set; }

        public override FightCommonInformations GetFightCommonInformations
            => new FightCommonInformations(Id, (sbyte)Type, new List<FightTeamInformations> { Team.GetFightTeamInformations(true), Team.GetFightTeamInformations() },
                new List<short>() { Team.BladePosition(true), Team.BladePosition() }, new List<FightOptionsInformations>() { Team.GetFightOptionsInformations(true), Team.GetFightOptionsInformations(false) });

        public override void AddFighter(FightActor fighter, bool isAttacker = false)
        {
            WorldClient client = null;
            if (State == FightStateEnum.Placement && !Team.IsFull(isAttacker))
            {
                TimeLine.AddFighter(fighter);
                if (fighter is CharacterFighter && isAttacker)
                {
                    client = (fighter as CharacterFighter).Character.Client;
                    Clients.Add(client);
                    Map.LeaveActor(client.Character);
                    Team.AddAttacker(fighter);
                    fighter.GeneratePosition();
                    EnterFighter(client, fighter);

                    for (int i = 0; i < TaxCollector.Guild.Members.Count; i++)
                    {
                        TaxCollectorHandler.SendGuildFightPlayersEnemiesListMessage(TaxCollector.Guild.Members[i].Client, TaxCollector,
                        Team.Attackers.Select(x => (x as CharacterFighter).Character));
                    }
                        
                }
                else
                {
                    if (fighter is CharacterFighter)
                    {
                        client = (fighter as CharacterFighter).Character.Client;
                        Clients.Add(client);
                        Map.LeaveActor(client.Character);
                        Team.AddDefender(fighter);
                        fighter.GeneratePosition();
                        EnterFighter(client, fighter);
                    }
                    else
                    {
                        Team.AddDefender(fighter);
                        fighter.GeneratePosition();
                    }           
                }
            }

            UpdateDirection();

            if (Team.Attackers.Count >= 1 && Team.Defenders.Count >= 1)
                ShowBlades();
        }

        public void AddDefender(Character character)
        {
            if (DefendersQueue.Count >= 8)
            {
                ContextHandler.SendChallengeFightJoinRefusedMessage(character.Client, character, FighterRefusedReasonEnum.TEAM_FULL);
                return;
            }

            if (State != FightStateEnum.Placement)
            {
                ContextHandler.SendChallengeFightJoinRefusedMessage(character.Client, character, FighterRefusedReasonEnum.TOO_LATE);
                return;
            }

            if (character.IsInFight())
            {
                ContextHandler.SendChallengeFightJoinRefusedMessage(character.Client, character, FighterRefusedReasonEnum.IM_OCCUPIED);
                return;
            }

            if (!character.IsInFight())

            for (int i = 0; i < TaxCollector.Guild.Members.Count; i++)
            {
                if (TaxCollector.Guild.Members[i].IsConnected())
                    TaxCollectorHandler.SendGuildFightPlayersHelpersJoinMessage(TaxCollector.Guild.Members[i].Client, TaxCollector, character);
            }

            if (DefendersQueue.Contains(character))
                DefendersQueue.Remove(character);

            DefendersQueue.Add(character);
        }

        public void RemoveDefender(Character character)
        {
            if (State == FightStateEnum.Placement)
            {
                if (DefendersQueue.Contains(character))
                    DefendersQueue.Remove(character);

                for (int i = 0; i < TaxCollector.Guild.Members.Count; i++)
                {
                    if (TaxCollector.Guild.Members[i].IsConnected())
                        TaxCollectorHandler.SendGuildFightPlayersHelpersLeaveMessage(TaxCollector.Guild.Members[i].Client, TaxCollector, character);
                }
            }               
        }

        public void TeleportDefendersQueue()
        {
            if (State != FightStateEnum.Placement)
                return;

            for (int i = 0; i < DefendersQueue.Count; i++)
            {
                if (!DefendersQueue[i].IsInFight())
                {
                    if (DefendersQueue[i].Map.Id != Map.Id)
                        DefendersQueue[i].Teleport(Map.Id, TaxCollector.Cell);

                    DefendersQueue[i].SetFight(Type, this);
                    AddFighter(DefendersQueue[i].Fighter = new CharacterFighter(DefendersQueue[i]));
                }
            }

            DefendersQueue.Clear();
            FightTime = DateTime.Now;
            StartAction(10000, "StartFight");

        }

        public override void CheckAllStatus()
        {
        }

        private void ChangeOptionTeam(FightTeam team, FightOptionsEnum option)
        {
            ContextHandler.SendGameFightOptionStateUpdateMessage(Clients, team, option, true, team == Team);
        }

        public int GetTimeLeftBeforeFight()
        {
            return PvTAttackersPlacementPhaseTime / 100;
        }

        public int GetWaitTimeForPlacement()
        { 
             return (int)TimeSpan.FromMilliseconds(PvTAttackersPlacementPhaseTime).TotalMilliseconds / 100;         
        }

        public override int GetPlacementTimeLeft(FightActor fighter = null)
        {
            double num = 30000 - (DateTime.Now - FightTime).TotalMilliseconds;

            if (fighter == null)         
                return num == 0.0 ? 0 : (int)num;
            
            if (!fighter.IsAttacker())
                return 100;

            return num == 0.0 ? 0 : (int)num;
        }

        public int GetDefendersLeftSlot()
        {
            return (7 - DefendersQueue.Count > 0) ? (7 - DefendersQueue.Count) : 0;
        }
    }
}
