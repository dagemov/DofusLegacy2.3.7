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
    [CommandHandler("levelup", RoleEnum.Moderator)]
    public class LevelCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 1 || !int.TryParse((string)Parameters[0], out int requestedLevel))
            {
                Client.Character.SendServerMessage("Unknown level !", Color.Red);
                return;
            }

            requestedLevel = Math.Max(1, Math.Min(200, requestedLevel));

            if (requestedLevel == Client.Character.Level)
            {
                Client.Character.SendServerMessage($"You are already level {requestedLevel}.", Color.Orange);
                return;
            }

            if (requestedLevel < Client.Character.Level)
            {
                Client.Character.SendServerMessage("Lowering level is not supported by this command.", Color.Red);
                return;
            }

            var levelsToAdd = requestedLevel - Client.Character.Level;
            Client.Character.LevelUp((byte)levelsToAdd);
        }

        public override string Description
        {
            get { return "Allows change your level."; }
        }
    }
}
