using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Tools.Dlm;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("interactives show", RoleEnum.Moderator)]
    public class ShowElementCommand : WorldCommand
    {
        public override void Execute()
        {
            if(Client.Character.Map.Elements.Count > 0)
            {
                List<Color> colors = new List<Color>
                {
                    Color.Blue, Color.Red, Color.OrangeRed,
                    Color.Violet, Color.Purple, Color.Cyan,
                    Color.Chocolate, Color.Yellow, Color.Green
                };

                int count = 0;
                foreach(var element in Client.Character.Map.Elements)
                {
                    var color = count >= colors.Count - 1 ? colors[count = 0] : colors[count];
                    Client.Send(new DebugHighlightCellsMessage(color.ToArgb() & 16777215, new List<short> { element.Cell }));
                    Client.Character.SendServerMessage($"Element Id : {element.Id}", color);
                    Client.Character.SendServerMessage($"Cell Id : {element.Cell}", color);
                    count++;
                }
            }
            else
                Client.Character.SendServerMessage("Any element exist !", Color.Red);
        }

        public override string Description
        {
            get { return "Allows show element in map."; }
        }
    }
}
