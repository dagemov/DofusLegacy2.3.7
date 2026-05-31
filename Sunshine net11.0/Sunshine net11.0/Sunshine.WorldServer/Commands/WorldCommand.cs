using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands
{
    public abstract class WorldCommand
    {
        public WorldClient Client { get; set; }

        public object[] Parameters { get; set; }

        public abstract void Execute();

        public abstract string Description { get; }

        public void Initialize(WorldClient client, object[] parameters)
        {
            Client = client;
            Parameters = parameters;
            this.Execute();
        }
    }
}
