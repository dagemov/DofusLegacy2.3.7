using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Characters.Inventory;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public class Trader
    {
        public RolePlayActor Actor { get; set; }

        public Trader(RolePlayActor actor)
        {
            Actor = actor;
        }

        public int Id { get { return Actor.Id; } }

        public WorldClient Client { get { return (Actor as Character).Client; } }

        public Inventory Inventory { get { return (Actor as Character).Inventory; } }

        public Trade Trade
        {
            get { return (Actor as Character).Trade; }
            set { (Actor as Character).Trade = value; }
        }

        public int Kamas { get; set; }

        public bool IsReady { get; set; }

    }
}
