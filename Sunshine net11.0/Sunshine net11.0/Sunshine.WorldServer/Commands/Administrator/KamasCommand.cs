using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("kamas", RoleEnum.Moderator)]
    public class KamasCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 1)
            {
                Client.Character.SendServerMessage("Usage: .kamas <amount> | .kamas add <amount> | .kamas remove <amount>", Color.Red);
                return;
            }

            int amount;
            string action = "set";

            if (Parameters.Length == 1)
            {
                if (!int.TryParse(Parameters[0].ToString(), out amount))
                {
                    Client.Character.SendServerMessage("Invalid kamas amount.", Color.Red);
                    return;
                }
            }
            else
            {
                action = Parameters[0].ToString().ToLower();

                if (!int.TryParse(Parameters[1].ToString(), out amount))
                {
                    Client.Character.SendServerMessage("Invalid kamas amount.", Color.Red);
                    return;
                }
            }

            switch (action)
            {
                case "set":
                    if (amount < 0)
                        amount = 0;

                    Client.Character.Inventory.Kamas = amount;
                    break;

                case "add":
                    if (amount < 0)
                    {
                        Client.Character.SendServerMessage("Amount must be positive.", Color.Red);
                        return;
                    }

                    Client.Character.Inventory.Kamas += amount;
                    break;

                case "remove":
                    if (amount < 0)
                    {
                        Client.Character.SendServerMessage("Amount must be positive.", Color.Red);
                        return;
                    }

                    Client.Character.Inventory.Kamas -= amount;
                    if (Client.Character.Inventory.Kamas < 0)
                        Client.Character.Inventory.Kamas = 0;
                    break;

                default:
                    Client.Character.SendServerMessage("Usage: .kamas <amount> | .kamas add <amount> | .kamas remove <amount>", Color.Red);
                    return;
            }

            InventoryHandler.SendKamasUpdateMessage(Client, Client.Character.Inventory.Kamas);
            Client.Character.RefreshStats();
            Client.Character.SendServerMessage(string.Format("Kamas updated: {0}", Client.Character.Inventory.Kamas));
        }

        public override string Description
        {
            get { return "Allows set/add/remove kamas."; }
        }
    }
}