using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("go", RoleEnum.Moderator)]
    public class TeleportCommand : WorldCommand
    {
        public override void Execute()
        {            
            if (Parameters.Length < 1)
                Client.Character.SendServerMessage("Unknown mapId !", Color.Red);
            else
            {
                int mapId = int.TryParse((string)Parameters[0], out int map) ? int.Parse((string)Parameters[0]) : -1;
                if (MapManager.Instance.GetMap(mapId) != null)
                {
                    short cellId = MapManager.Instance.GetMap(mapId).Cells.FirstOrDefault(x => x.Walkable && x.Id > 100).Id;
                    Client.Character.Teleport(mapId, cellId);
                }
                else
                    Client.Character.SendServerMessage("Map doesn't exist !", Color.Red);
            }
        }

        public override string Description
        {
            get { return "Allows teleport to an other map."; }
        }
    }
}
