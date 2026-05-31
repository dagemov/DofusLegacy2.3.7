using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("align", RoleEnum.Player)]
    public class AlignmentCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 1)
                Client.Character.SendServerMessage("Unknown alignment !", Color.Red);
            else
            {
                switch ((string)Parameters[0])
                {
                    case "neutre":
                    case "0":
                        Client.Character.ChangeAlignementSide(AlignmentSideEnum.ALIGNMENT_NEUTRAL);
                        Client.Character.SendServerMessage("You are now Neutral.");
                        break;

                    case "ange":
                    case "1":
                        Client.Character.ChangeAlignementSide(AlignmentSideEnum.ALIGNMENT_ANGEL);
                        Client.Character.SendServerMessage("You are now Angel.");
                        break;

                    case "demon":
                    case "2":
                        Client.Character.ChangeAlignementSide(AlignmentSideEnum.ALIGNMENT_EVIL);
                        Client.Character.SendServerMessage("You are now Evil.");
                        break;

                    case "mercenaire":
                    case "3":
                        Client.Character.ChangeAlignementSide(AlignmentSideEnum.ALIGNMENT_MERCENARY);
                        Client.Character.SendServerMessage("You are now Mercenary.");
                        break;

                    default:
                        Client.Character.SendServerMessage("Alignement value doesn't exist.");
                        break;
                }
            }
        }

        public override string Description
        {
            get { return "Allows change your alignment."; }
        }
    }
}
