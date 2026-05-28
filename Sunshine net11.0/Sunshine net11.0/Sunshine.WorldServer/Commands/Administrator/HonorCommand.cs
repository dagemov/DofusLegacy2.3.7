using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Handlers.Characters;
using Sunshine.WorldServer.Handlers.PvP;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("honor", RoleEnum.Moderator)]
    public class HonorCommand : WorldCommand
    {
        private const int MaxHonor = 20000;

        public override void Execute()
        {
            if (Parameters.Length < 1)
            {
                Client.Character.SendServerMessage("Usage: .honor <amount> | .honor add <amount> | .honor remove <amount>", Color.Red);
                return;
            }

            int amount;
            string action = "set";
            int currentHonor = Client.Character.Alignment.Honor;

            if (Parameters.Length == 1)
            {
                if (!int.TryParse(Parameters[0].ToString(), out amount))
                {
                    Client.Character.SendServerMessage("Invalid honor amount.", Color.Red);
                    return;
                }
            }
            else
            {
                action = Parameters[0].ToString().ToLower();

                if (!int.TryParse(Parameters[1].ToString(), out amount))
                {
                    Client.Character.SendServerMessage("Invalid honor amount.", Color.Red);
                    return;
                }
            }

            int newHonor = currentHonor;

            switch (action)
            {
                case "set":
                    newHonor = amount;
                    break;

                case "add":
                    if (amount < 0)
                    {
                        Client.Character.SendServerMessage("Amount must be positive.", Color.Red);
                        return;
                    }

                    newHonor += amount;
                    break;

                case "remove":
                    if (amount < 0)
                    {
                        Client.Character.SendServerMessage("Amount must be positive.", Color.Red);
                        return;
                    }

                    newHonor -= amount;
                    break;

                default:
                    Client.Character.SendServerMessage("Usage: .honor <amount> | .honor add <amount> | .honor remove <amount>", Color.Red);
                    return;
            }

            if (newHonor < 0)
                newHonor = 0;

            if (newHonor > MaxHonor)
                newHonor = MaxHonor;

            Client.Character.Alignment.Honor = (ushort)newHonor;

            PvPHandler.SendAlignmentRankUpdateMessage(Client);
            CharacterHandler.SendCharacterStatsListMessage(Client);
            Client.Character.RefreshActor();
            Client.Character.SendServerMessage(string.Format("Honor updated: {0}", newHonor));
        }

        public override string Description
        {
            get { return "Allows set/add/remove honor with a max of 20000."; }
        }
    }
}