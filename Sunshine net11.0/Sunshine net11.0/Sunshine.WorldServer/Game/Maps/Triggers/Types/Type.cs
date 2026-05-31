using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Triggers.Types
{
    public abstract class Type
    {
        public WorldClient Client { get; set; }

        public List<int> Parameters { get; set; }

        public Timer Timer { get; set; }

        public abstract void Execute();

        public void Initialize(WorldClient client, List<int> parameters)
        {
            Client = client;
            Parameters = parameters;
            Timer = new Timer(30000);
            this.Execute();
        }
    }
}
