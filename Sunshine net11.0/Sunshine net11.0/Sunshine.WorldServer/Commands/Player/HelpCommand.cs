using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("help", RoleEnum.Player)]
    public class HelpCommand : WorldCommand
    {
        public override void Execute()
        {
            var commands = CommandManager.Instance.Commands;
            foreach(var command in commands.Where(x => x.Value.Item1 <= (RoleEnum)Client.Account.Role))
                Client.Character.SendServerMessage($"<b>{command.Key}</b> : {command.Value.Item2().Description}");
        }

        public override string Description
        {
            get { return "Allows show all commands."; }
        }
    }
}
