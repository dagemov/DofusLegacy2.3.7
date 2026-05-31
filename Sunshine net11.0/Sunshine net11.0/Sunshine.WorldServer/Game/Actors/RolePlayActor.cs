using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors
{
    public abstract class RolePlayActor
    {
        public abstract int Id { get; }

        public abstract GameRolePlayActorInformations GetGameRolePlayActorInformations();

        public abstract void StartMove(Path path);

        public Fights.Results.FightLoot FightLoot { get; set; }
    }
}