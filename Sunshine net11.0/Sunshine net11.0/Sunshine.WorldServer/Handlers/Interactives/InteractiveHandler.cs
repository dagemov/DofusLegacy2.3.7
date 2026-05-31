using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using Sunshine.WorldServer.Game.Dialogs.Prisms;
using Sunshine.WorldServer.Game.Dialogs.Teleports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Interactives
{
    public class InteractiveHandler : WorldPacketHandler
    {
        [WorldHandler(5001)]
        public static void HandleInteractiveUseRequestMessage(WorldClient client, InteractiveUseRequestMessage message)
        {
            if(!client.Character.IsInDialog())
                SkillDispatcher.Dispatch(client, message.skillInstanceUid, message.elemId);
        }

        [WorldHandler(5961)]
        public static void HandleTeleportRequestMessage(WorldClient client, TeleportRequestMessage message)
        {
            var map = MapManager.Instance.GetMap(message.mapId);

            if (map == null)
                return;

            if (client.Character.IsInDialog() && client.Character.Dialog is SkillZaapTeleport)
                (client.Character.Dialog as SkillZaapTeleport).Teleport(map);
            else if (client.Character.IsInDialog() && client.Character.Dialog is SkillZaapiTeleport)
                (client.Character.Dialog as SkillZaapiTeleport).Teleport(map);
            else if (client.Character.IsInDialog() && client.Character.Dialog is CustomTeleportDialog)
                (client.Character.Dialog as CustomTeleportDialog).Teleport(map);
            else if (client.Character.IsInDialog() && client.Character.Dialog is PrismDialog)
                (client.Character.Dialog as PrismDialog).Teleport(map);
        }

        public static void SendInteractiveUsedMessage(List<WorldClient> clients, Character user, Skill skill, short duration = 0)
        {
            clients.ForEach(x => x.Send(new InteractiveUsedMessage(user.Id, skill.Element, (short)skill.Id, duration)));
        }

        public static void SendInteractiveUseEndedMessage(WorldClient client, Skill skill)
        {
            client.Send(new InteractiveUseEndedMessage(skill.Element, (short)skill.Id));
        }
    }
}
