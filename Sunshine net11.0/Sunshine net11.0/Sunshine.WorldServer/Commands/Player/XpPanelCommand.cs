using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Dialogs.Teleports;
using Sunshine.WorldServer.Game.Teleports;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("xp", RoleEnum.Player)]
    public class XpPanelCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("Impossible d'ouvrir le panel pendant un combat ou un dialogue.");
                return;
            }

            var destinations = CustomTeleportService.GetDestinations(CustomTeleportCategory.XpZones);
            if (!destinations.Any())
            {
                Client.Character.SendServerMessage("Aucune destination .xp n'est configurée.");
                return;
            }

            new CustomTeleportDialog(Client.Character, destinations, ".xp").Open();
        }

        public override string Description => "Ouvre le panel de téléportation des zones XP.";
    }
}
