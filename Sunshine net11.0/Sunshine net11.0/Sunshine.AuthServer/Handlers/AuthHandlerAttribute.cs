using Sunshine.AuthServer.Client;
using Sunshine.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.AuthServer
{ 
    public class AuthHandler : Attribute
    {
        public uint Id;
        public AuthHandler(uint id)
        {
            Id = id;
        }
    }
}
