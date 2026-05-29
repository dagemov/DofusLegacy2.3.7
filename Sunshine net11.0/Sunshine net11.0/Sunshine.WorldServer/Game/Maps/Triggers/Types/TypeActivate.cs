using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Maps.Interactives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Triggers.Types
{
    [TypeHandler(1)]
    public class TypeActivate : Type
    {
        public override void Execute()
        {
            var elemId = Parameters[0];
            var grid = Client.Character.Map.Interactives.FirstOrDefault(x => x.GetStatedElement != null &&
                                                                            x.GetStatedElement.elementId == elemId);          
            if (grid != null)
            {
                for (int i = 1; i < Parameters.Count; i++)
                {
                    var cell = (short)Parameters[i];
                    var character = CharacterManager.Instance.GetCharacter(Client.Character.Map.Id, cell);
                    if (character == null)
                        return;
                }

                for (int i = 0; i < grid.Parameters.Count; i++)
                    grid.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_OPENED;
                grid.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_ACTIVATED;
                Client.Character.Map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(grid.GetStatedElement)));
                Client.Character.Map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(grid.GetObstacles)));
                Timer.Elapsed += (sender, e) => CloseGrid(grid, e);
                Timer.Start();
            }
        }

        private void CloseGrid(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            var grid = sender as Interactive;        
            for (int i = 0; i < grid.Parameters.Count; i++)
                grid.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_CLOSED;
            grid.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_NORMAL;
            Client.Character.Map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(grid.GetStatedElement)));
            Client.Character.Map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(grid.GetObstacles)));
        }
    }
}
