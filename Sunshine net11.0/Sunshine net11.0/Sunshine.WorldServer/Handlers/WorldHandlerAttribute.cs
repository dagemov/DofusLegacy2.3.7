using Sunshine.AuthServer.Client;
using Sunshine.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer
{
    public class WorldHandler : Attribute
    {
        public uint Id;
        public WorldHandler(uint id)
        {
            Id = id;
        }
    }
}
