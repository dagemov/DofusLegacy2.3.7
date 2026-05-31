using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Triggers.Types
{
    [TypeHandler(0)]
    public class TypeTeleport : Type
    {
        public override void Execute()
        {
            var map = MapManager.Instance.GetMap(Parameters[0]);
            if (map == null)
                return;
            short cell = (short)Parameters[1];
            var direction = Parameters[2];
            Client.Character.Direction = direction;
            Client.Character.Teleport(map.Id, cell);
        }
    }
}
