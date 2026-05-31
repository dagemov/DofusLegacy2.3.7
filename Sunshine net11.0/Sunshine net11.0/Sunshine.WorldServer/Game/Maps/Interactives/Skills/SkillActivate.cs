using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Interactives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(179)]
    public class SkillActivate : Skill, IDialog
    {
        public override void Execute()
        {
            var lever = Client.Character.Map.Interactives.FirstOrDefault(x => x.Element == Element && x.GetStatedElement == null);
            if (lever != null)
            {
                Interactive slab = null;
                int elemId = 0;
                Map map = Client.Character.Map;

                if (lever.Parameters.Count > 1)
                {
                    map = MapManager.Instance.GetMap(lever.Parameters[0]);
                    elemId = lever.Parameters[1];
                    slab = map.Interactives.FirstOrDefault(x => x.GetStatedElement != null &&
                                                                               x.GetStatedElement.elementId == elemId);
                }
                else
                {
                    elemId = lever.Parameters[0];
                    slab = map.Interactives.FirstOrDefault(x => x.GetStatedElement != null &&
                                                                                x.GetStatedElement.elementId == elemId);               
                }

                if (slab != null)
                {
                    InteractiveHandler.SendInteractiveUsedMessage(map.Clients, Client.Character, this);
                    for (int i = 0; i < slab.Parameters.Count; i++)
                        slab.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_OPENED;
                    slab.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_ACTIVATED;
                    map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(slab.GetStatedElement)));
                    map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(slab.GetObstacles)));
                    InteractiveHandler.SendInteractiveUseEndedMessage(Client, this);
                    Timer.Elapsed += (sender, e) => RespawnSlab(slab, map, e);
                    Timer.Start();
                }
            }
        }

        private void RespawnSlab(object sender, Map map, ElapsedEventArgs e)
        {
            Timer.Dispose();
            var slab = sender as Interactive;           
            for (int i = 0; i < slab.Parameters.Count; i++)
                slab.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_CLOSED;
            slab.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_NORMAL;
            map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(slab.GetStatedElement)));
            map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(slab.GetObstacles)));
        }
    }
}
