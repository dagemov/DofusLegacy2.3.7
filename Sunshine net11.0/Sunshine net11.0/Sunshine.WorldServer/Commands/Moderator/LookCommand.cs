using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("look", RoleEnum.Moderator)]
    public class LookCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 1)
                Client.Character.SendServerMessage("Unknown morph !", Color.Red);
            else if ((string)Parameters[0] == "demorph")
            {
                Client.Character.CustomLook = "";
                Client.Character.UpdateLook(false);
            }
            else
            {
                string morph = Parameters[0].ToString().Replace("&#123;", "{").Replace("&#125;", "}");
                if (!morph.Contains("{") && !morph.Contains("}"))
                    Client.Character.SendServerMessage("Incorrect morph !");
                else
                {
                    var look = EntityManager.Instance.GetActorLook(morph);
                    Client.Character.CustomLook = EntityManager.Instance.ParseEntityLook(look.GetEntityLook());
                    Client.Character.UpdateLook(true);
                }
            }
        }

        public override string Description
        {
            get { return "Allows change your look."; }
        }
    }
}
