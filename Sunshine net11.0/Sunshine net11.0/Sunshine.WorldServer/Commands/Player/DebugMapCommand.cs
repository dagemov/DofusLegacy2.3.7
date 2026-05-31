using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.MySql.Database.Managers;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("debugmap", RoleEnum.Player)]
    public class DebugMapCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("Impossible de se téléporter pendant un combat ou un dialogue.", Color.Red);
                return;
            }

            const int mapId = 2323;
            const short cellId = 328;
            const int direction = 3;

            var map = MapManager.Instance.GetMap(mapId);
            if (map == null)
            {
                Client.Character.SendServerMessage("La map 2323 est introuvable.", Color.Red);
                return;
            }

            Client.Character.Direction = direction;
            Client.Character.Teleport(mapId, cellId);
            Client.Character.Direction = direction;
        }

        public override string Description
        {
            get { return "Téléporte sur la map debug (2323, cellule 328, direction 3)."; }
        }
    }
}