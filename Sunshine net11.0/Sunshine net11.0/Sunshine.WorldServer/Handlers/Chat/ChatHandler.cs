using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using Sunshine.Protocol.Utils.Extensions;
using System.Linq;
using System.Text;
using Sunshine.Protocol.Messages;
using System.Threading.Tasks;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Servers;
using Sunshine.WorldServer.Commands;

namespace Sunshine.WorldServer.Handlers.Chat
{
    public class ChatHandler : WorldPacketHandler
    {
        [WorldHandler(890)]
        public static void HandleChannelEnablingMessage(WorldClient client, ChannelEnablingMessage message)
        {
        }

        [WorldHandler(800)]
        public static void HandleChatSmileyRequestMessage(WorldClient client, ChatSmileyRequestMessage message)
        {
            client.Character.Map.Clients.ForEach(x => x.Send(new ChatSmileyMessage(client.Character.Id, message.smileyId, client.Account.Id)));
        }


        [WorldHandler(MoodSmileyRequestMessage.Id)]
        public static void HandleMoodSmileyRequestMessage(WorldClient client, MoodSmileyRequestMessage message)
        {
            if (client?.Character == null)
                return;

            if (!client.Character.CanChangeMoodSmiley())
            {
                client.Send(new MoodSmileyResultMessage(1, client.Character.MoodSmileyId));
                client.Character.SendServerMessage($"Tu dois attendre encore {client.Character.GetMoodSmileyCooldownSeconds()} seconde(s) avant de rechanger ton émoticône.");
                return;
            }

            client.Character.SetMoodSmiley(message.smileyId);
            client.Send(new MoodSmileyResultMessage(0, message.smileyId));

            Friends.FriendHandler.SendFriendsListMessage(client);

            if (client.Character.Guild != null)
            {
                foreach (var member in client.Character.Guild.Members.Where(x => x?.Client != null))
                    Guilds.GuildHandler.SendGuildInformationsMemberUpdateMessage(member.Client, client.Character.GuildMember);
            }

            foreach (var watcher in Game.Social.SocialRelationManager.Instance.FindOnlineFriendsOf(client.Character))
                Friends.FriendHandler.SendFriendsListMessage(watcher.Client);
        }

        [WorldHandler(861)]
        public static void HandleChatClientMultiMessage(WorldClient client, ChatClientMultiMessage message)
        {
            if (!string.IsNullOrEmpty(message.content) && message.content[0] == '.')
                CommandDispatcher.Dispatch(client, message.content);
            else
                ChatHandler.SendChatClientMultiMessage(ServersManager.Instance.GetClients<WorldClient>(), client.Character, message);
        }

        [WorldHandler(851)]
        public static void HandleChatClientPrivateMessage(WorldClient client, ChatClientPrivateMessage message)
        {
            if (!string.IsNullOrEmpty(message.content))
            {
                Character characterReceiver = WorldServerManager.Instance.GetCharacter(message.receiver);
                if (characterReceiver != null)
                {
                    if(client.Character.Name == characterReceiver.Name)
                        ChatHandler.SendChatErrorMessage(client, ChatErrorEnum.CHAT_ERROR_INTERIOR_MONOLOGUE);
                    else
                        ChatHandler.SendChatClientPrivateMessage(client, characterReceiver, message);
                }
                else
                    ChatHandler.SendChatErrorMessage(client, ChatErrorEnum.CHAT_ERROR_RECEIVER_NOT_FOUND);
            }
        }

        public static void SendEnabledChannelsMessage(WorldClient client)
        {
            var enabledChannels = new List<sbyte>
            {
                (sbyte)ChatActivableChannelsEnum.CHANNEL_GLOBAL,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_TEAM,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_GUILD,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_ALIGN,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_SALES,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_SEEK,
                (sbyte)ChatActivableChannelsEnum.CHANNEL_ADMIN,
                (sbyte)ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE,
                (sbyte)ChatActivableChannelsEnum.PSEUDO_CHANNEL_INFO,
                (sbyte)ChatActivableChannelsEnum.PSEUDO_CHANNEL_FIGHT_LOG
            };

            if (client?.Character?.IsInParty == true)
                enabledChannels.Add((sbyte)ChatActivableChannelsEnum.CHANNEL_PARTY);

            client.Send(new EnabledChannelsMessage(enabledChannels, new List<sbyte>()));
        }

        public static void SendChatServerMessage(WorldClient client, ChatActivableChannelsEnum channel, string content, int senderid, string sendername, int accountid)
        {
            if (!string.IsNullOrEmpty(content))
                client.Send(new ChatServerMessage((sbyte)channel, content, System.DateTime.Now.GetUnixTimeStamp(), "", senderid, sendername, accountid));
        }

        public static void SendChatErrorMessage(WorldClient client, ChatErrorEnum error)
        {
            client.Send(new ChatErrorMessage((sbyte)error));
        }

        private static void SendChatClientMultiMessage(IEnumerable<WorldClient> clients, Character sender, ChatClientMultiMessage message)
        {
            var clientsConnected = clients.Where(x => message.channel == 0 ? (x.Character != null && x.Character.Map == sender.Map) : x.Character != null);

            if (message.channel == (sbyte)ChatActivableChannelsEnum.CHANNEL_PARTY)
            {
                if (!sender.IsInParty)
                {
                    SendChatErrorMessage(sender.Client, ChatErrorEnum.CHAT_ERROR_NO_PARTY);
                    return;
                }

                clientsConnected = clientsConnected.Where(x => x.Character.IsInParty && x.Character.Party.Id == sender.Party.Id);
            }
            else if (message.channel == (sbyte)ChatActivableChannelsEnum.CHANNEL_GUILD)
            {
                if (sender.Guild == null)
                {
                    SendChatErrorMessage(sender.Client, ChatErrorEnum.CHAT_ERROR_NO_GUILD);
                    return;
                }

                clientsConnected = clientsConnected.Where(x => x.Character.Guild != null && x.Character.Guild.Id == sender.Guild.Id);
            }

            clientsConnected.ToList().ForEach(x => SendChatServerMessage(x, (ChatActivableChannelsEnum)message.channel, message.content, sender.Id, sender.Name, sender.Account.Id));

        }

        private static void SendChatClientPrivateMessage(WorldClient client, Character receiver, ChatClientPrivateMessage message)
        {
            client.Send(new ChatServerCopyMessage((sbyte)ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, System.DateTime.Now.GetUnixTimeStamp(), "", receiver.Id, receiver.Name));
            ChatHandler.SendChatServerMessage(receiver.Client, ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client.Character.Id, client.Character.Name, client.Character.Account.Id);
        }

    }
}
