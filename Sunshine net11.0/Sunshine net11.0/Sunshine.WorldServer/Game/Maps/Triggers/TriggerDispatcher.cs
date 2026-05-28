using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Triggers
{
    public static class TriggerDispatcher
    {
        public static void Dispatch(WorldClient client, Trigger trigger)
        {
            if (MapManager.Instance.Triggers.ContainsKey(trigger.Type))
            {
                var trig = MapManager.Instance.Triggers[trigger.Type]();
                trig.Initialize(client, trigger.Parameters);
            }
            else
                Logs.Logger.WriteError($"Cannot dispatch the trigger {trigger.Map} !");
        }
    }
}
