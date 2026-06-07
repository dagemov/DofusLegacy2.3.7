using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Dialogs.Teleports;
using Sunshine.WorldServer.Game.Teleports;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("tp", RoleEnum.Player)]
    public class TpPanelCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("No puedes abrir el panel durante un combate o un diálogo.");
                return;
            }

            var destinations = CustomTeleportService.GetDestinations(CustomTeleportCategory.Maps);
            if (!destinations.Any())
            {
                Client.Character.SendServerMessage("No hay ningún destino .tp configurado.");
                return;
            }

            new CustomTeleportDialog(Client.Character, destinations, ".tp").Open();
        }

        public override string Description => "Abre el panel de teletransporte personalizado.";
    }
}
