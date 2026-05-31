using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("a", RoleEnum.Moderator)]
    public class AnnounceCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 1)
                Client.Character.SendServerMessage("Unknown message !", Color.Red);
            else
            {
                string message = (string)Parameters[0];
                string etoile = string.Format($"<font size=\"{+27}\">{"★"}</font>");
                string announce = $"(ANNOUNCE) {etoile}<b>[{Client.Character.Name}]</b> : {message}";

                var characters = CharacterManager.Instance.Characters;

                foreach (var character in characters.Values.Where(x => x.IsInWorld()))
                    character.SendServerMessage(announce, Color.OrangeRed);
            }
        }

        public override string Description
        {
            get { return "Allows send announce for the people."; }
        }
    }
}
