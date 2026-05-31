using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Timers;
using Sunshine.Protocol.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Fights.Results;

namespace Sunshine.WorldServer.Game.Fights.Types
{
    public class FightAgression : Fight
    {
        public FightAgression(int id, Character leader)
        {
            Id = id;
            Leader = leader;
            Map = leader.Map;
            Map.Fights.Add(this);
            Clients = new List<WorldClient>();
            Team = new FightTeam(this);
            TimeLine = new TimeLine(this);
            State = FightStateEnum.Placement;
            MaxMemberCount = 8;
            FightTime = DateTime.Now;
            Results = new FightResults(this);
        }

        public override int Id { get; }

        public override Map Map { get; }

        public override FightTeam Team { get; }

        public override FightTypeEnum Type { get { return FightTypeEnum.FIGHT_TYPE_AGRESSION; } }

        public override Timer Timer { get; set; }

        public DateTime FightTime { get; set; }

        public override FightCommonInformations GetFightCommonInformations
            => new FightCommonInformations(Id, (sbyte)Type, new List<FightTeamInformations> { Team.GetFightTeamInformations(true), Team.GetFightTeamInformations() },
                new List<short>() { Team.BladePosition(true), Team.BladePosition() }, new List<FightOptionsInformations>() { Team.GetFightOptionsInformations(true), Team.GetFightOptionsInformations(false) });

        public override void AddFighter(FightActor fighter, bool isAttacker = false)
        {
            if (fighter == null)
                return;

            if (fighter is CharacterFighter characterFighter && ContainsCharacter(characterFighter.Character.Id))
                return;

            if (State == FightStateEnum.Placement && !Team.IsFull(isAttacker))
            {
                TimeLine.AddFighter(fighter);
                if (fighter is CharacterFighter)
                {
                    var client = (fighter as CharacterFighter).Character.Client;
                    Clients.Add(client);
                    Map.LeaveActor(client.Character);
                    if (isAttacker)
                        Team.AddAttacker(fighter);
                    else
                        Team.AddDefender(fighter);
                    fighter.GeneratePosition();
                    EnterAllFighters(fighter as CharacterFighter, TimeLine.Fighters.Count);
                }
            }

            UpdateDirection();

            if (Team.Attackers.Count >= 1 && Team.Defenders.Count >= 1)
                ShowBlades();
        }

        public override void CheckAllStatus()
        {
            if (Clients.TrueForAll(x => x.Character.Fighter.IsReady))
                StartFight();
        }

        private void ChangeOptionTeam(FightTeam team, FightOptionsEnum option)
        {
            ContextHandler.SendGameFightOptionStateUpdateMessage(Clients, team, option, true, team == Team);
        }

        private void EnterAllFighters(CharacterFighter fighter, int count)
        {
            if (count < 2)
                return;

            if (count == 2)
            {
                StartAction(GetPlacementTimeLeft(), "StartFight");
                TimeLine.Fighters.ForEach(x => EnterFighter((x as CharacterFighter).Client, x));
            }
            else
                EnterFighter(fighter.Client, fighter);
        }

        public override int GetPlacementTimeLeft(FightActor fighter = null)
        {
            double num = 30000 - (DateTime.Now - FightTime).TotalMilliseconds;
            return num == 0.0 ? 0 : (int)num;
        }
    }
}

