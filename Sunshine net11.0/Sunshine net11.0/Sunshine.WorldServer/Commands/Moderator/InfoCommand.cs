using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("info", RoleEnum.Moderator)]
    public class InfoCommand : WorldCommand
    {
        public override void Execute()
        {
            int count = CharacterManager.Instance.Characters.Where(x => x.Value.Client != null && x.Value.Client.Account != null).Count();

            Client.Character.SendServerMessage(string.Format("{0} accounts connected", count));
        }

        public override string Description
        {
            get { return "Allows display the number of accounts connected."; }
        }
    }
}
