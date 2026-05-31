using Sunshine.MySql.Database.World.Characters;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Interactives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    public abstract class Skill
    {
        public WorldClient Client { get; set; }

        public int Id { get; set; }

        public int Element { get; set; }

        public CharacterJobRecord Job { get; set; }

        public int Level { get; set; }

        public Timer Timer { get; set; }

        public abstract void Execute();

        public void Initialize(WorldClient client, int id, int element)
        {
            Client = client;
            Id = id;
            Element = element;
            Timer = new Timer(30000);
            this.Execute();
        }
    }
}
