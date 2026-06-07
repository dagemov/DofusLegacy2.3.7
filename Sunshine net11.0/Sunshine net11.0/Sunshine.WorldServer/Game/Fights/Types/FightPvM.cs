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
using Sunshine.WorldServer.Game.Actors.Monsters;

namespace Sunshine.WorldServer.Game.Fights.Types
{
    public class FightPvM : Fight
    {
        public FightPvM(int id, Character leader)
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
            StartAction(GetPlacementTimeLeft(), "StartFight");
        }

        public override int Id { get; }

        public override Map Map { get; }

        public override FightTeam Team { get; }

        public override FightTypeEnum Type { get { return FightTypeEnum.FIGHT_TYPE_PvM; } }

        public override Timer Timer { get; set; }

        public DateTime FightTime { get; set; }

        public MonsterGroup SourceMonsterGroup { get; set; }

        // Combate contra Dopeul: cuando se activa, la victoria entrega Doplones.
        public bool IsDopeulFight { get; set; }

        public int DopeulMonsterId { get; set; }

        public override FightCommonInformations GetFightCommonInformations
            => new FightCommonInformations(Id, (sbyte)Type, new List<FightTeamInformations> { Team.GetFightTeamInformations(true), Team.GetFightTeamInformations() },
                new List<short>() { Team.BladePosition(true), Team.BladePosition() }, new List<FightOptionsInformations>() { Team.GetFightOptionsInformations(true), Team.GetFightOptionsInformations(false) });

        public override void AddFighter(FightActor fighter, bool isAttacker = false)
        {
            Map.EnsureFightCells();

            if (State == FightStateEnum.Placement && !Team.IsFull(isAttacker))
            {
                TimeLine.AddFighter(fighter);
                if (fighter is CharacterFighter && isAttacker)
                {
                    var client = (fighter as CharacterFighter).Character.Client;
                    Clients.Add(client);
                    Map.LeaveActor(client.Character);
                    Team.AddAttacker(fighter);
                    fighter.GeneratePosition();
                    EnterFighter(client, fighter);
                }
                else
                {
                    Team.AddDefender(fighter);
                    fighter.GeneratePosition();
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

        public override int GetPlacementTimeLeft(FightActor fighter = null)
        {
            double num = 30000 - (DateTime.Now - FightTime).TotalMilliseconds;
            return num == 0.0 ? 0 : (int)num;
        }       
    }
}

