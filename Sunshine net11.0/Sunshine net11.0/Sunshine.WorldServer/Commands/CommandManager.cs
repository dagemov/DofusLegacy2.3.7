using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands
{
    class CommandManager : Singleton<CommandManager>
    {
        public Dictionary<string, Tuple<RoleEnum, Func<WorldCommand>>> Commands = new Dictionary<string, Tuple<RoleEnum, Func<WorldCommand>>>();

        public bool IsAvailable(WorldClient client, RoleEnum roleRequired)
        {
            if (client.Account.Role >= (sbyte)roleRequired)
                return true;
            return false;
        }
    }
}
