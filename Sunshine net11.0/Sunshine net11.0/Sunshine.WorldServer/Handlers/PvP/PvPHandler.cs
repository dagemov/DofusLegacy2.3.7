using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.Prisms;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs.Prisms;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Maps.Prisms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.PvP
{
    public class PvPHandler : WorldPacketHandler
    {
        [WorldHandler(1810)]
        public static void HandleSetEnablePVPRequestMessage(WorldClient client, SetEnablePVPRequestMessage message)
        {
            if (!client.Character.IsInFight())
                client.Character.TogglePvPMode(message.enable);
        }

        [WorldHandler(1811)]
        public static void HandleGetPVPActivationCostMessage(WorldClient client, GetPVPActivationCostMessage message)
        {
            client.Send(new PVPActivationCostMessage((short)(client.Character.Alignment.Honor / 35)));
        }

        [WorldHandler(5840)]
        public static void HandlePrismCurrentBonusRequestMessage(WorldClient client, PrismCurrentBonusRequestMessage message)
        {
            var total = PrismManager.Instance.CountActivePrisms();
            if (total <= 0)
            {
                client.Send(new PrismAlignmentBonusResultMessage(new AlignmentBonusInformations(100, client.Character.Alignment.Grade)));
                return;
            }

            var owned = PrismManager.Instance.CountOwned(client.Character.Alignment.Side);
            int pct = (int)Math.Max(0, Math.Min(100, (1.0d - ((double)owned / (double)total)) * 100.0d));
            client.Send(new PrismAlignmentBonusResultMessage(new AlignmentBonusInformations(pct, client.Character.Alignment.Grade)));
        }

        [WorldHandler(5839)]
        public static void HandlePrismBalanceRequestMessage(WorldClient client, PrismBalanceRequestMessage message)
        {
            var total = PrismManager.Instance.CountActivePrisms();
            if (total <= 0)
            {
                client.Send(new PrismBalanceResultMessage(0, 0));
                return;
            }

            var owned = PrismManager.Instance.CountOwned(client.Character.Alignment.Side);
            int worldBalance = (int)Math.Max(0, Math.Min(100, ((double)owned / (double)total) * 100.0d));

            var currentSubAreaPrism = PrismManager.Instance.GetPrism(client.Character.Map.SubAreaId);
            int subAreaBalance = currentSubAreaPrism != null && currentSubAreaPrism.AlignmentSide == (sbyte)client.Character.Alignment.Side ? 100 : 0;

            client.Send(new PrismBalanceResultMessage((sbyte)worldBalance, (sbyte)subAreaBalance));
        }

        [WorldHandler(PrismWorldInformationRequestMessage.Id)]
        public static void HandlePrismWorldInformationRequestMessage(WorldClient client, PrismWorldInformationRequestMessage message)
        {
            SendPrismWorldInformationMessage(client);
        }

        [WorldHandler(PrismUseRequestMessage.Id)]
        public static void HandlePrismUseRequestMessage(WorldClient client, PrismUseRequestMessage message)
        {
            if (client == null || client.Character == null || client.Character.Map == null)
                return;

            PrismManager.Instance.EnsureMapPrismState(client.Character.Map);
            var prism = PrismManager.Instance.GetAuthoritativePrismActor(client.Character.Map);
            if (prism == null || prism.Record == null)
                return;

            if (prism.Record.IsInFight || prism.Record.WasDefeated)
                return;

            if (prism.Record.AlignmentSide != (sbyte)client.Character.Alignment.Side)
                return;

            var destinations = PrismManager.Instance.GetTeleportMaps(client.Character.Alignment.Side).ToList();
            if (destinations.Count <= 0)
                return;

            new PrismDialog(client.Character, destinations).Open();
        }

        [WorldHandler(PrismAttackRequestMessage.Id)]
        public static void HandlePrismAttackRequestMessage(WorldClient client, PrismAttackRequestMessage message)
        {
            if (client?.Character?.Map == null || client.Character.IsInFight())
                return;

            PrismManager.Instance.EnsureMapPrismState(client.Character.Map);
            var prismActor = PrismManager.Instance.GetAuthoritativePrismActor(client.Character.Map);
            if (prismActor == null || prismActor.Record == null)
                return;

            var prism = prismActor.Record;
            if (prism == null || prism.WasDefeated || prism.AlignmentSide <= 0)
            {
                client.Character.SendServerMessage("Ce prisme n'est plus disponible.");
                return;
            }

            if (prism.AlignmentSide == (sbyte)client.Character.Alignment.Side)
            {
                client.Character.SendServerMessage("Vous ne pouvez pas attaquer un prisme de votre alignement.");
                return;
            }

            if (prism.IsInFight)
            {
                client.Character.SendServerMessage("Ce prisme est déjà en combat.");
                return;
            }

            if (prism.LastFight.HasValue && (DateTime.UtcNow - prism.LastFight.Value) < TimeSpan.FromHours(2))
            {
                var remain = TimeSpan.FromHours(2) - (DateTime.UtcNow - prism.LastFight.Value);
                client.Character.SendServerMessage($"Veuillez patienter : {Math.Max(0, remain.Hours)}h {Math.Max(0, remain.Minutes)}min.");
                return;
            }

            var fight = new FightPvPrism(FightManager.Instance.GenerateId(false), client.Character, prism);
            fight.PrismFighter = new PrismFighter(prism, fight);
            fight.AddFighter(fight.PrismFighter);

            client.Character.SetFight(FightTypeEnum.FIGHT_TYPE_MXvM, fight);
            fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character), true);

            PrismManager.Instance.MarkInFight(prism);
            SendPrismInfoState(client, prism, fight);
            BroadcastPrismAttacked(prism, fight);
        }

        [WorldHandler(PrismFightJoinLeaveRequestMessage.Id)]
        public static void HandlePrismFightJoinLeaveRequestMessage(WorldClient client, PrismFightJoinLeaveRequestMessage message)
        {
            if (client?.Character == null)
                return;

            var prism = PrismManager.Instance.GetInFightPrism(client.Character.Alignment.Side);
            if (prism == null)
            {
                client.Send(new PrismInfoInValidMessage(0));
                client.Send(new PrismInfoCloseMessage());
                return;
            }

            if (prism.AlignmentSide != (sbyte)client.Character.Alignment.Side)
            {
                client.Character.SendServerMessage("Vous ne pouvez pas défendre un prisme ennemi.");
                return;
            }

            var fight = PrismManager.Instance.GetPrismFight(prism);
            if (fight == null)
            {
                client.Send(new PrismInfoInValidMessage(0));
                client.Send(new PrismInfoCloseMessage());
                return;
            }

            if (message.join)
            {
                FighterRefusedReasonEnum reason;
                if (!fight.AddDefender(client.Character, out reason))
                {
                    switch (reason)
                    {
                        case FighterRefusedReasonEnum.TEAM_FULL:
                            client.Character.SendServerMessage("La défense du prisme est complète.");
                            break;
                        case FighterRefusedReasonEnum.IM_OCCUPIED:
                            client.Character.SendServerMessage("Vous êtes déjà occupé.");
                            break;
                        default:
                            client.Character.SendServerMessage("Impossible de rejoindre la défense du prisme.");
                            break;
                    }
                    return;
                }
                foreach (var alignedClient in GetAlignmentClients((AlignmentSideEnum)prism.AlignmentSide))
                    {
                        SendPrismFightDefenderAddMessage(alignedClient, fight.Id, client.Character, false);
                    }
            }
            else
            {
                if (!fight.RemoveDefender(client.Character))
                    return;

                foreach (var alignedClient in GetAlignmentClients((AlignmentSideEnum)prism.AlignmentSide))
                {
                    SendPrismFightDefenderLeaveMessage(alignedClient, fight.Id, client.Character.Id, 0);
                }
            }

            BroadcastPrismDefenseState(prism, fight);
        }

        [WorldHandler(PrismInfoJoinLeaveRequestMessage.Id)]
        public static void HandlePrismInfoJoinLeaveRequestMessage(WorldClient client, PrismInfoJoinLeaveRequestMessage message)
        {
            if (client?.Character == null)
                return;

            if (!message.join)
            {
                client.Send(new PrismInfoCloseMessage());
                return;
            }

            var prism = PrismManager.Instance.GetInFightPrism(client.Character.Alignment.Side);
            if (prism == null)
            {
                client.Send(new PrismInfoInValidMessage(0));
                client.Send(new PrismInfoCloseMessage());
                return;
            }

            var fight = PrismManager.Instance.GetPrismFight(prism);
            if (fight == null)
            {
                client.Send(new PrismInfoInValidMessage(0));
                client.Send(new PrismInfoCloseMessage());
                return;
            }

            SendPrismInfoState(client, prism, fight);
        }

        public static void SendAlignmentRankUpdateMessage(WorldClient client)
        {
            client.Send(new AlignmentRankUpdateMessage(client.Character.Alignment.Grade, false));
        }

        public static void SendAlignmentSubAreasListMessage(WorldClient client)
        {
            client.Send(new AlignmentSubAreasListMessage(
                PrismManager.Instance.GetAlignmentSubAreas(AlignmentSideEnum.ALIGNMENT_ANGEL),
                PrismManager.Instance.GetAlignmentSubAreas(AlignmentSideEnum.ALIGNMENT_EVIL)));
        }

        public static void SendPrismWorldInformationMessage(WorldClient client)
        {
            // Ne renvoyer que les prismes réellement valides/actifs évite l'affichage
            // de marqueurs fantômes sur la carte du monde pour des sous-zones sans prisme réel.
            var infos = PrismManager.Instance.GetAllPrisms(false)
                .Where(prism => prism != null && prism.AlignmentSide > 0)
                .Where(prism =>
                {
                    var map = MapManager.Instance.GetMap(prism.MapId);
                    return map != null && !map.IsInstance() && map.IsCanonicalMap() && map.SubAreaId == prism.SubAreaId;
                })
                .OrderBy(prism => prism.SubAreaId)
                .Select(prism => new PrismSubAreaInformation(
                    prism.SubAreaId,
                    prism.AlignmentSide,
                    prism.WorldX,
                    prism.WorldY,
                    prism.MapId,
                    prism.IsInFight,
                    prism.IsFightable))
                .ToList();

            int owned = PrismManager.Instance.CountOwned(client.Character.Alignment.Side);
            int total = infos.Count;

            client.Send(new PrismWorldInformationMessage(owned, total, total, infos, 0, 0, new List<PrismConquestInformation>()));
        }

        public static void SendPrismFightStateUpdateMessage(WorldClient client, sbyte state)
        {
            client.Send(new PrismFightStateUpdateMessage(state));
        }

        public static void SendPrismFightAttackedMessage(WorldClient client, WorldMapPrismRecord prism)
        {
            client.Send(new PrismFightAttackedMessage((short)prism.WorldX, (short)prism.WorldY, prism.MapId, (short)prism.SubAreaId, prism.AlignmentSide));
        }

        public static void SendPrismFightAttackerAddMessage(WorldClient client, double fightId, IEnumerable<Character> attackers)
        {
            client.Send(new PrismFightAttackerAddMessage(fightId,
                attackers.Where(x => x != null).Select(x => x.GetCharacterMinimalPlusLookAndGradeInformations())));
        }

        public static void SendPrismFightDefenderAddMessage(WorldClient client, double fightId, Character defender, bool inMain)
        {
            if (client == null || defender == null)
                return;

            client.Send(new PrismFightDefenderAddMessage(
                fightId,
                defender.GetCharacterMinimalPlusLookAndGradeInformations(),
                inMain));
        }

        public static void SendPrismFightDefenderLeaveMessage(WorldClient client, double fightId, int fighterToRemoveId, int successor)
        {
            if (client == null)
                return;

            client.Send(new PrismFightDefenderLeaveMessage(fightId, fighterToRemoveId, successor));
        }

        public static void SendPrismFightDefendersStateMessage(WorldClient client, FightPvPrism fight)
        {
            client.Send(new PrismFightDefendersStateMessage(
                fight.Id,
                fight.Team.Defenders.OfType<CharacterFighter>().Select(x => x.Character.GetCharacterMinimalPlusLookAndGradeInformations()),
                fight.DefendersQueue.Select(x => x.GetCharacterMinimalPlusLookAndGradeInformations())));
        }

        private static void SendPrismInfoState(WorldClient client, WorldMapPrismRecord prism, FightPvPrism fight)
        {
            client.Send(new PrismInfoValidMessage(
                new ProtectedEntityWaitingForHelpInfo(
                    fight.GetTimeLeftBeforeFight(),
                    fight.GetWaitTimeForPlacement(),
                    (sbyte)fight.GetDefendersLeftSlot())));

            SendPrismFightDefendersStateMessage(client, fight);
            SendPrismFightAttackerAddMessage(client, fight.Id, fight.Team.Attackers.OfType<CharacterFighter>().Select(x => x.Character));
            SendPrismFightStateUpdateMessage(client, 1);
        }

        private static void BroadcastPrismAttacked(WorldMapPrismRecord prism, FightPvPrism fight)
        {
            foreach (var alignedClient in GetAlignmentClients((AlignmentSideEnum)prism.AlignmentSide))
            {
                SendPrismFightAttackedMessage(alignedClient, prism);
                SendPrismFightStateUpdateMessage(alignedClient, 1);
                SendPrismFightAttackerAddMessage(alignedClient, fight.Id, fight.Team.Attackers.OfType<CharacterFighter>().Select(x => x.Character));
                SendPrismFightDefendersStateMessage(alignedClient, fight);
                SendPrismWorldInformationMessage(alignedClient);
            }
        }

        private static void BroadcastPrismDefenseState(WorldMapPrismRecord prism, FightPvPrism fight)
        {
            foreach (var alignedClient in GetAlignmentClients((AlignmentSideEnum)prism.AlignmentSide))
            {
                SendPrismInfoState(alignedClient, prism, fight);
                SendPrismWorldInformationMessage(alignedClient);
            }
        }

        public static void BroadcastPrismFightEnded(WorldMapPrismRecord prism, AlignmentSideEnum side)
        {
            if (prism == null)
                return;

            if (side != AlignmentSideEnum.ALIGNMENT_ANGEL &&
                side != AlignmentSideEnum.ALIGNMENT_EVIL)
                return;

            foreach (var alignedClient in GetAlignmentClients(side))
            {
                alignedClient.Send(new PrismInfoInValidMessage(0));
                alignedClient.Send(new PrismInfoCloseMessage());
                SendPrismFightStateUpdateMessage(alignedClient, 0);
                SendPrismWorldInformationMessage(alignedClient);
            }
        }

        private static IEnumerable<WorldClient> GetAlignmentClients(AlignmentSideEnum side)
        {
            return CharacterManager.Instance.Characters.Values
                .Where(x => x != null && x.Client != null && x.Alignment.Side == side)
                .Select(x => x.Client)
                .Distinct()
                .ToList();
        }
    }
}
