using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    public abstract class Reply
    {
        public WorldClient Client { get; set; }
        
        public Npc Npc { get; set; }

        public List<object> Parameters { get; set; }

        public abstract bool Execute();

        public bool Initialize(WorldClient client, Npc npc, List<object> parameters)
        {
            Client = client;
            Npc = npc;
            Parameters = parameters;
            return this.Execute();
        }
    }
}
