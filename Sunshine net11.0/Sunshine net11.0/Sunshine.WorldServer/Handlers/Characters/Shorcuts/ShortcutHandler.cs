using Sunshine.MySql.Database.World.Characters.Shortcuts;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Characters.Shorcuts
{
    public class ShortcutHandler : WorldPacketHandler
    {
        [WorldHandler(6225)]
        public static void HandleShortcutBarAddRequestMessage(WorldClient client, ShortcutBarAddRequestMessage message)
        {
            client.Character.Shortcuts.AddShortcut(message.shortcut, (ShortcutBarEnum)message.barType);
            if ((ShortcutBarEnum)message.barType == ShortcutBarEnum.OBJECT)
                SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
        }
        [WorldHandler(6228)]
        public static void HandleShortcutBarRemoveRequestMessage(WorldClient client, ShortcutBarRemoveRequestMessage message)
        {
            client.Character.Shortcuts.RemoveShortcut((ShortcutBarEnum)message.barType, message.slot);
            if ((ShortcutBarEnum)message.barType == ShortcutBarEnum.OBJECT)
                SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
        }
        [WorldHandler(6230)]
        public static void HandleShortcutBarSwapRequestMessage(WorldClient client, ShortcutBarSwapRequestMessage message)
        {
            client.Character.Shortcuts.SwitchShortcut((ShortcutBarEnum)message.barType, message.firstSlot, message.secondSlot);
            if ((ShortcutBarEnum)message.barType == ShortcutBarEnum.OBJECT)
                SendShortcutBarContentMessage(client, ShortcutBarEnum.OBJECT);
        }
        public static void SendShortcutBarContentMessage(WorldClient client, ShortcutBarEnum barType)
        {
            if (barType == ShortcutBarEnum.OBJECT)
                client.Character.Shortcuts.CleanupInvalidItemShortcuts(false);

            var shortcuts = client.Character.Shortcuts.GetShortcuts(barType)?.ToArray() ?? Array.Empty<Protocol.Types.Shortcut>();
            client.Send(new ShortcutBarContentMessage((sbyte)barType, shortcuts));
        }

        public static void SendShortcutBarContentMessage(WorldClient client, ShortcutBarEnum barType, IEnumerable<Protocol.Types.Shortcut> shortcuts)
        {
            if (client == null)
                return;

            var values = shortcuts?.ToArray() ?? Array.Empty<Protocol.Types.Shortcut>();
            client.Send(new ShortcutBarContentMessage((sbyte)barType, values));
        }
        public static void SendShortcutBarRefreshMessage(WorldClient client, ShortcutBarEnum barType, Shortcut shortcut)
        {
            if (shortcut == null)
                return;

            var networkShortcut = shortcut.GetNetworkShortcut();
            if (networkShortcut == null)
                return;

            client.Send(new ShortcutBarRefreshMessage((sbyte)barType, networkShortcut));
        }
        public static void SendShortcutBarRemovedMessage(WorldClient client, ShortcutBarEnum barType, int slot)
        {
            client.Send(new ShortcutBarRemovedMessage((sbyte)barType, slot));
        }
        public static void SendShortcutBarRemoveErrorMessage(WorldClient client)
        {
            client.Send(new ShortcutBarRemoveErrorMessage());
        }
        public static void SendShortcutBarSwapErrorMessage(WorldClient client)
        {
            client.Send(new ShortcutBarSwapErrorMessage());
        }
        public static void SendShortcutBarAddErrorMessage(WorldClient client)
        {
            client.Send(new ShortcutBarAddErrorMessage());
        }
    }
}
