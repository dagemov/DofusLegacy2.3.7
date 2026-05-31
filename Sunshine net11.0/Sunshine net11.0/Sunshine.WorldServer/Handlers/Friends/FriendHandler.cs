using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Social;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.Friends
{
    public class FriendHandler : WorldPacketHandler
    {
        [WorldHandler(FriendsGetListMessage.Id)]
        public static void HandleFriendsGetListMessage(WorldClient client, FriendsGetListMessage message)
        {
            SendFriendsListMessage(client);
        }

        [WorldHandler(FriendAddRequestMessage.Id)]
        public static void HandleFriendAddRequestMessage(WorldClient client, FriendAddRequestMessage message)
        {
            var target = FindCharacterByName(message.name);
            if (target == null)
            {
                client.Send(new FriendAddFailureMessage(1));
                return;
            }

            if (!SocialRelationManager.Instance.AddFriend(client.Character, target))
            {
                client.Send(new FriendAddFailureMessage(2));
                return;
            }

            client.Send(new FriendAddedMessage(SocialRelationManager.Instance.BuildFriendInformations(client.Character).Last()));
            SendFriendsListMessage(client);
        }

        [WorldHandler(FriendDeleteRequestMessage.Id)]
        public static void HandleFriendDeleteRequestMessage(WorldClient client, FriendDeleteRequestMessage message)
        {
            bool deleted = SocialRelationManager.Instance.DeleteFriend(client.Character.Id, message.name);
            client.Send(new FriendDeleteResultMessage(deleted, message.name));
            if (deleted)
                SendFriendsListMessage(client);
        }

        [WorldHandler(IgnoredGetListMessage.Id)]
        public static void HandleIgnoredGetListMessage(WorldClient client, IgnoredGetListMessage message)
        {
            SendIgnoredListMessage(client);
        }

        [WorldHandler(IgnoredAddRequestMessage.Id)]
        public static void HandleIgnoredAddRequestMessage(WorldClient client, IgnoredAddRequestMessage message)
        {
            var name = (message.name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Equals(client.Character.Name, StringComparison.OrdinalIgnoreCase))
            {
                client.Send(new IgnoredAddFailureMessage(1));
                return;
            }

            if (!client.SessionIgnoredNames.Add(name))
            {
                client.Send(new IgnoredAddFailureMessage(2));
                return;
            }

            var target = FindCharacterByName(name);
            IgnoredInformations info = target != null
                ? (IgnoredInformations)new IgnoredOnlineInformations(name, target.Id, target.Name, (sbyte)target.Breed, target.Sex)
                : new IgnoredInformations(name, 0);
            client.Send(new IgnoredAddedMessage(info, true));
            SendIgnoredListMessage(client);
        }

        [WorldHandler(IgnoredDeleteRequestMessage.Id)]
        public static void HandleIgnoredDeleteRequestMessage(WorldClient client, IgnoredDeleteRequestMessage message)
        {
            bool deleted = client.SessionIgnoredNames.Remove(message.name ?? string.Empty);

            client.Send(new IgnoredDeleteResultMessage(deleted, true, message.name));

            if (deleted)
                SendIgnoredListMessage(client);
        }

        public static void SendFriendsListMessage(WorldClient client)
        {
            client.Send(new FriendsListMessage(SocialRelationManager.Instance.BuildFriendInformations(client.Character).ToList()));
        }

        public static void SendIgnoredListMessage(WorldClient client)
        {
            client.Send(new IgnoredListMessage(SocialRelationManager.Instance.BuildIgnoredInformations(client.Character, client.SessionIgnoredNames).ToList()));
        }

        private static Character FindCharacterByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return MySql.Database.Managers.CharacterManager.Instance.Characters.Values
                .FirstOrDefault(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
