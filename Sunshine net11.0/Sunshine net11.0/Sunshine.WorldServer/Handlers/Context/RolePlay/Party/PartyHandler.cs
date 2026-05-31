using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Parties;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Context.RolePlay.Party
{
    public class PartyHandler : WorldPacketHandler
    {
        private static readonly Dictionary<int, short> DungeonInvitations = new Dictionary<int, short>();

        [WorldHandler(5585)]
        public static void HandlePartyInvitationRequestMessage(WorldClient client, PartyInvitationRequestMessage message)
        {
            Character guest = WorldServerManager.Instance.GetCharacter(message.name);

            if (guest == null)
            {
                PartyHandler.SendPartyCannotJoinErrorMessage(client, PartyJoinErrorEnum.PARTY_JOIN_ERROR_PLAYER_NOT_FOUND);
                return;
            }

            if (client.Character.Party == null)
                client.Character.SetParty(PartyTypeEnum.PARTY_TYPE_CLASSICAL, client.Character);

            client.Character.SetInvitation(PartyTypeEnum.PARTY_TYPE_CLASSICAL, guest);
        }

        [WorldHandler(6264)]
        public static void HandlePartyInvitationDetailsRequestMessage(WorldClient client, PartyInvitationDetailsRequestMessage message)
        {
            var party = PartyManager.Instance.GetParty(message.partyId);

            if (party == null)
                return;

            if (DungeonInvitations.TryGetValue(party.Id, out var dungeonId))
            {
                client.Send(new PartyInvitationDungeonDetailsMessage(party.Id, party.Sender.Id, party.Sender.Name, party.Leader.Id, party.GetPartyInvitationMemberInformations, dungeonId, party.GetPartyInvitationMemberInformations.Select(x => true)));
                return;
            }

            client.Send(new PartyInvitationDetailsMessage(party.Id, party.Sender.Id, party.Sender.Name, party.Leader.Id, party.GetPartyInvitationMemberInformations));
        }

        [WorldHandler(PartyInvitationDungeonRequestMessage.Id)]
        public static void HandlePartyInvitationDungeonRequestMessage(WorldClient client, PartyInvitationDungeonRequestMessage message)
        {
            Character guest = WorldServerManager.Instance.GetCharacter(message.name);

            if (guest == null)
            {
                PartyHandler.SendPartyCannotJoinErrorMessage(client, PartyJoinErrorEnum.PARTY_JOIN_ERROR_PLAYER_NOT_FOUND);
                return;
            }

            if (client.Character.Party == null)
                client.Character.SetParty(PartyTypeEnum.PARTY_TYPE_CLASSICAL, client.Character);

            DungeonInvitations[client.Character.Party.Id] = message.dungeonId;
            client.Character.Party.Sender = client.Character;
            client.Character.Party.AddGuest(guest);
            SendPartyInvitationDungeonMessage(guest.Client, client.Character.Party, client.Character, message.dungeonId);
        }

        [WorldHandler(5580)]
        public static void HandlePartyAcceptInvitationMessage(WorldClient client, PartyAcceptInvitationMessage message)
        {
            var party = PartyManager.Instance.GetParty(message.partyId);

            if (party != null)
                party.AcceptMember(client.Character);
        }

        [WorldHandler(5582)]
        public static void HandlePartyRefuseInvitationMessage(WorldClient client, PartyRefuseInvitationMessage message)
        {
            var party = PartyManager.Instance.GetParty(message.partyId);

            if (party != null)
                party.RemoveGuest(client.Character, false);
        }

        [WorldHandler(6254)]
        public static void HandlePartyCancelInvitationMessage(WorldClient client, PartyCancelInvitationMessage message)
        {
            if (client.Character.IsInParty)
            {
                Character guest = client.Character.Party.GetGuest(message.guestId);
                if (guest != null)
                    client.Character.Party.RemoveGuest(guest, true);
            }
        }

        [WorldHandler(5592)]
        public static void HandlePartyKickRequestMessage(WorldClient client, PartyKickRequestMessage message)
        {
            if (client.Character.IsPartyLeader)
            {
                Character member = client.Character.Party.GetMember(message.playerId);
                client.Character.Party.Kick(member);
            }
        }

        [WorldHandler(5593)]
        public static void HandlePartyLeaveRequestMessage(WorldClient client, PartyLeaveRequestMessage message)
        {
            if (client.Character.IsInParty)
                client.Character.LeaveParty();
        }

        public static void SendPartyNewGuestMessage(WorldClient client, Character guest)
        {
            client.Send(new PartyNewGuestMessage(guest.GetPartyGuestInformations));
        }

        public static void SendPartyInvitationMessage(WorldClient client, IParty party, Character from)
        {
            client.Send(new PartyInvitationMessage(party.Id, from.Id, from.Name, client.Character.Id));
        }

        public static void SendPartyInvitationDungeonMessage(WorldClient client, IParty party, Character from, short dungeonId)
        {
            client.Send(new PartyInvitationDungeonMessage(party.Id, from.Id, from.Name, client.Character.Id, dungeonId));
        }

        public static void SendPartyJoinMessage(WorldClient client, Game.Parties.Party party)
        {
            client.Send(new PartyJoinMessage(party.Id, party.Leader.Id, party.GetPartyMemberInformations, party.GetPartyGuestInformations, true));
        }

        public static void SendPartyCannotJoinErrorMessage(WorldClient client, PartyJoinErrorEnum reason)
        {
            client.Send(new PartyCannotJoinErrorMessage((sbyte)reason));
        }

        public static void SendPartyKickedByMessage(WorldClient client, Character kicker)
        {
            client.Send(new PartyKickedByMessage(kicker.Id));
        }

        public static void SendPartyDeletedMessage(WorldClient client, IParty party)
        {
            client.Send(new PartyDeletedMessage(party.Id));
        }

        public static void SendPartyMemberRemoveMessage(WorldClient client, Character leaver)
        {
            client.Send(new PartyMemberRemoveMessage(leaver.Id));
        }

        public static void SendPartyLeaveMessage(WorldClient client)
        {
            client.Send(new PartyLeaveMessage());
        }

        public static void SendPartyUpdateMessage(WorldClient client, Character member)
        {
            client.Send(new PartyUpdateMessage(member.GetPartyMemberInformations));
        }

        public static void SendPartyRefuseInvitationNotificationMessage(WorldClient client, Character guest)
        {
            client.Send(new PartyRefuseInvitationNotificationMessage(guest.Id));
        }

        public static void SendPartyInvitationCancelledForGuestMessage(WorldClient client, Game.Parties.Party party, Character canceler)
        {
            client.Send(new PartyInvitationCancelledForGuestMessage(party.Id, canceler.Id));
        }

        public static void SendPartyCancelInvitationNotificationMessage(WorldClient client, Character canceler, Character guest)
        {
            client.Send(new PartyCancelInvitationNotificationMessage(canceler.Id, guest.Id));
        }
    }
}
