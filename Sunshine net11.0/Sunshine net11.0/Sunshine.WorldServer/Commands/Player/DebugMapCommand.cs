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
                Client.Character.SendServerMessage("No puedes teletransportarte durante un combate o un diálogo.", Color.Red);
                return;
            }

            const int mapId = 2323;
            const short cellId = 328;
            const int direction = 3;

            var map = MapManager.Instance.GetMap(mapId);
            if (map == null)
            {
                Client.Character.SendServerMessage("El mapa 2323 no se encontró.", Color.Red);
                return;
            }

            Client.Character.Direction = direction;
            Client.Character.Teleport(mapId, cellId);
            Client.Character.Direction = direction;
        }

        public override string Description
        {
            get { return "Teletransporta al mapa de depuración (2323, celda 328, dirección 3)."; }
        }
    }
}