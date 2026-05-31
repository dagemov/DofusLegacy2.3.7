using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Fights.Teams
{
    public class FightTeam
    {
        public Fight Fight { get; set; }
        public List<FightActor> Attackers { get; set; }
        public List<FightActor> Defenders { get; set; }

        public bool ChallengerSecret { get; set; }
        public bool ChallengerPartyOnly { get; set; }
        public bool ChallengerClosed { get; set; }
        public bool ChallengerAskingForHelp { get; set; }

        public bool DefenderSecret { get; set; }
        public bool DefenderPartyOnly { get; set; }
        public bool DefenderClosed { get; set; }
        public bool DefenderAskingForHelp { get; set; }

        public FightTeam(Fight fight)
        {
            Fight = fight;
            Attackers = new List<FightActor>();
            Defenders = new List<FightActor>();
        }

        public FightOptionsInformations GetFightOptionsInformations(bool isChallenger)
        {
            return isChallenger
                ? new FightOptionsInformations(ChallengerSecret, ChallengerPartyOnly, ChallengerClosed, ChallengerAskingForHelp)
                : new FightOptionsInformations(DefenderSecret, DefenderPartyOnly, DefenderClosed, DefenderAskingForHelp);
        }

        public bool IsClosed(bool isChallenger) => isChallenger ? ChallengerClosed : DefenderClosed;
        public bool IsPartyOnly(bool isChallenger) => isChallenger ? ChallengerPartyOnly : DefenderPartyOnly;

        public bool ToggleOption(bool isChallenger, FightOptionsEnum option)
        {
            switch (option)
            {
                case FightOptionsEnum.FIGHT_OPTION_SET_SECRET:
                    if (isChallenger) ChallengerSecret = !ChallengerSecret; else DefenderSecret = !DefenderSecret;
                    return isChallenger ? ChallengerSecret : DefenderSecret;
                case FightOptionsEnum.FIGHT_OPTION_SET_TO_PARTY_ONLY:
                    if (isChallenger) ChallengerPartyOnly = !ChallengerPartyOnly; else DefenderPartyOnly = !DefenderPartyOnly;
                    return isChallenger ? ChallengerPartyOnly : DefenderPartyOnly;
                case FightOptionsEnum.FIGHT_OPTION_SET_CLOSED:
                    if (isChallenger) ChallengerClosed = !ChallengerClosed; else DefenderClosed = !DefenderClosed;
                    return isChallenger ? ChallengerClosed : DefenderClosed;
                case FightOptionsEnum.FIGHT_OPTION_ASK_FOR_HELP:
                    if (isChallenger) ChallengerAskingForHelp = !ChallengerAskingForHelp; else DefenderAskingForHelp = !DefenderAskingForHelp;
                    return isChallenger ? ChallengerAskingForHelp : DefenderAskingForHelp;
                default:
                    return false;
            }
        }

        public void AddAttacker(FightActor attacker)
        {
            if (!Attackers.Contains(attacker))
                Attackers.Add(attacker);
            else
            {
                Attackers.Remove(attacker);
                Attackers.Add(attacker);
            }
        }

        public void AddAttacker(FightActor summoner, FightActor attacker)
        {
            if (Attackers.Contains(summoner))
            {                
                int indexSummoner = Attackers.IndexOf(summoner);
                Attackers.Insert(indexSummoner + 1, attacker);
            }
        }

        public void AddDefender(FightActor summoner, FightActor defender)
        {
            if (Defenders.Contains(summoner))
            {
                int indexSummoner = Defenders.IndexOf(summoner);
                Defenders.Insert(indexSummoner + 1, defender);
            }
        }

        public void AddDefender(FightActor defender)
        {
            if (!Defenders.Contains(defender))
                Defenders.Add(defender);
            else
            {
                Defenders.Remove(defender);
                Defenders.Add(defender);
            }
        }

        public void RemoveAllTeams()
        {
            Attackers.Clear();
            Defenders.Clear();
        }

        public bool IsFull(bool isAttackerTeam = false)
        {
            if (isAttackerTeam)
                return Attackers.Count >= 8 ? true : false;
            else
                return Defenders.Count >= 8 ? true : false;
        }

        private bool HasActiveFighter(IEnumerable<FightActor> fighters)
        {
            if (fighters == null)
                return false;

            var timeLine = Fight?.TimeLine?.Fighters;
            return fighters.Any(x => x != null && x.IsAlive && !(x is ISummoned) && (timeLine == null || timeLine.Contains(x)));
        }

        public bool AreAllDead()
        {
            return !HasActiveFighter(Attackers) || !HasActiveFighter(Defenders);
        }

        public short BladePosition(bool isRedTeam = false)
        {
            if (isRedTeam && Attackers.Count == 0)
                return 0;

            if (!isRedTeam && Defenders.Count == 0)
                return 0;

            switch(Fight.Type)
            {
                case FightTypeEnum.FIGHT_TYPE_PvM:
                    var monsterLeader = (MonsterFighter)Defenders.FirstOrDefault(x => (x as MonsterFighter).Monster.GetMonsterGroup != null);
                    var monsterCell = monsterLeader.Monster.GetMonsterGroup.GetEntityDisposition().cellId;                   
                    return isRedTeam ? (Attackers[0] as CharacterFighter).Character.Cell.Id
                                     : monsterCell >= 550 ? Fight.Map.Cells[monsterCell - 1].Id : Fight.Map.Cells[monsterCell + 1].Id;
                case FightTypeEnum.FIGHT_TYPE_AGRESSION:
                case FightTypeEnum.FIGHT_TYPE_MXvM:
                    return isRedTeam ? (Attackers[0] as CharacterFighter).Character.Cell.Id
                                     : (Defenders[0] is CharacterFighter ? (Defenders[0] as CharacterFighter).Character.Cell.Id : Defenders[0].Position.Cell);
                case FightTypeEnum.FIGHT_TYPE_PvT:
                    return isRedTeam ? (Attackers[0] as CharacterFighter).Character.Cell.Id
                                     : (Defenders[0] as TaxCollectorFighter).TaxCollector.Cell;
                default:
                    return 0;
            }
        }

        public FightTeamInformations GetFightTeamInformations(bool isRedTeam = false)
        {
            if (isRedTeam)
            {
                var attackers = Attackers.Select(x => x.GetFightTeamMemberInformations()).ToArray();
                return new FightTeamInformations((sbyte)TeamEnum.TEAM_CHALLENGER, Attackers.Count > 0 ? Attackers[0].Id : 0, 0, (sbyte)Fight.Type, attackers);
            }
            else
            {
                var defenders = Defenders.Select(x => x.GetFightTeamMemberInformations()).ToArray();
                return new FightTeamInformations((sbyte)TeamEnum.TEAM_DEFENDER, Defenders.Count > 0 ? Defenders[0].Id : 0, 1, (sbyte)Fight.Type, defenders);
            }
        }
    }
}
