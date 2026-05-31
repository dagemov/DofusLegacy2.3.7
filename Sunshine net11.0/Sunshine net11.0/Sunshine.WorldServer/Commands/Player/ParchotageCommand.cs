using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("parchotage", RoleEnum.Player)]
    public class ParchotageCommand : WorldCommand
    {
        public override string Description => "Applique un parchotage 101 en sagesse, force, agilité, chance, intelligence et vitalité.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Parameters.Length > 0 && Parameters[0] != null && Parameters[0].ToString() != "101")
            {
                Client.Character.SendServerMessage("Usage : .parchotage 101", Color.Red);
                return;
            }

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("Impossible d'utiliser .parchotage 101 pendant un combat, un échange ou un dialogue.", Color.Red);
                return;
            }

            Client.Character.ApplyParchotage101();
            Client.Character.SendServerMessage("Parchotage 101 appliqué sur sagesse, force, agilité, chance, intelligence et vitalité.");
        }
    }
}
