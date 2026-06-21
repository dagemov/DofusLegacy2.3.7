using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Dialogs
{
    public class DialogHandler : WorldPacketHandler
    {
        [WorldHandler(5501)]
        public static void HandleLeaveDialogRequestMessage(WorldClient client, LeaveDialogRequestMessage message)
        {
            if (client?.Character == null)
                return;

            if (client.Character.IsInTrade())
            {
                if (client.Character.Trade is Game.Exchanges.PlayerTrade)
                    client.Character.Trade.Cancel();
                else
                    client.Character.Trade.Close();

                client.Character.Dialog = null;
                return;
            }

            if (client.Character.Dialog is NpcBuySellAction)
            {
                InventoryHandler.SendExchangeLeaveMessage(client, true);
                client.Character.Dialog = null;
                return;
            }

            DialogHandler.SendLeaveDialogMessage(client);
        }

        public static void SendLeaveDialogMessage(WorldClient client)
        {
            client.Send(new LeaveDialogMessage());
            client.Character.Dialog = null;
        }
    }
}
