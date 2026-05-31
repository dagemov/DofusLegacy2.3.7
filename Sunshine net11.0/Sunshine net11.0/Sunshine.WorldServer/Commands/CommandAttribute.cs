using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands
{
    public class CommandHandler : Attribute
    {
        public string Name { get; set; }

        public RoleEnum Role { get; set; }

        public CommandHandler(string name, RoleEnum role)
        {
            Name = name;
            Role = role;
        }
    }
}
