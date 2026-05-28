using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Parties;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.Context.RolePlay.Party
{
    public class DungeonPartyFinderHandler : WorldPacketHandler
    {
        [WorldHandler(DungeonPartyFinderAvailableDungeonsRequestMessage.Id)]
        public static void HandleDungeonPartyFinderAvailableDungeonsRequestMessage(WorldClient client, DungeonPartyFinderAvailableDungeonsRequestMessage message)
        {
            DungeonPartyFinderManager.Instance.RefreshPlayer(client);
        }

        [WorldHandler(DungeonPartyFinderRegisterRequestMessage.Id)]
        public static void HandleDungeonPartyFinderRegisterRequestMessage(WorldClient client, DungeonPartyFinderRegisterRequestMessage message)
        {
            try
            {
                if (client == null || client.Character == null)
                    return;

                var requested = (message.dungeonIds ?? Array.Empty<short>())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (DungeonPartyFinderManager.Instance.HasCatalog)
                    requested = requested.Where(DungeonPartyFinderManager.Instance.CanListen).ToList();

                DungeonPartyFinderManager.Instance.RegisterPlayerForDungeons(client.Character, requested);
                client.Send(new DungeonPartyFinderRegisterSuccessMessage(DungeonPartyFinderManager.Instance.GetPlayerRegisteredDungeons(client.Character.Id)));
                DungeonPartyFinderManager.Instance.RefreshPlayer(client);
            }
            catch
            {
                client.Send(new DungeonPartyFinderRegisterErrorMessage());
            }
        }

        [WorldHandler(DungeonPartyFinderListenRequestMessage.Id)]
        public static void HandleDungeonPartyFinderListenRequestMessage(WorldClient client, DungeonPartyFinderListenRequestMessage message)
        {
            try
            {
                if (client == null || client.Character == null)
                    return;

                if (message.dungeonId <= 0)
                {
                    DungeonPartyFinderManager.Instance.LeaveCurrentRoom(client.Character.Id);
                    client.Send(new DungeonPartyFinderRoomContentMessage(0, Array.Empty<Protocol.Types.DungeonPartyFinderPlayer>()));
                    return;
                }

                if (!DungeonPartyFinderManager.Instance.CanListen(message.dungeonId))
                {
                    client.Send(new DungeonPartyFinderListenErrorMessage(message.dungeonId));
                    return;
                }

                DungeonPartyFinderManager.Instance.JoinRoom(client.Character, message.dungeonId);
                client.Send(new DungeonPartyFinderRoomContentMessage(message.dungeonId, DungeonPartyFinderManager.Instance.GetRoomContent(message.dungeonId)));
            }
            catch
            {
                client.Send(new DungeonPartyFinderListenErrorMessage(message.dungeonId));
            }
        }

        public static void RefreshPlayerDungeonFinderData(WorldClient client)
        {
            DungeonPartyFinderManager.Instance.RefreshPlayer(client);
        }
    }
}
