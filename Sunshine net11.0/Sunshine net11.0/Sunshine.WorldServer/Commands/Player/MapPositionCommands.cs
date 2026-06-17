using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("mapid", RoleEnum.Player)]
    public class MapIdCommand : WorldCommand
    {
        public override string Description => "Muestra el ID del mapa actual.";

        public override void Execute()
        {
            if (Client?.Character?.Map == null)
                return;

            Client.Character.SendServerMessage(
                $"MapId: {Client.Character.Map.Id}",
                Color.Aqua);
        }
    }

    [CommandHandler("cellid", RoleEnum.Player)]
    public class CellIdCommand : WorldCommand
    {
        public override string Description => "Muestra el ID de la celda actual en el mapa.";

        public override void Execute()
        {
            if (Client?.Character?.Cell == null)
                return;

            Client.Character.SendServerMessage(
                $"CellId: {Client.Character.Cell.Id}",
                Color.Aqua);
        }
    }
}
