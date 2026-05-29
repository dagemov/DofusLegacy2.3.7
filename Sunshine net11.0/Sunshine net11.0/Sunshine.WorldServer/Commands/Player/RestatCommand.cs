using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("restat", RoleEnum.Player)]
    public class RestatCommand : WorldCommand
    {
        public override string Description => "Réinitialise vos caractéristiques et vos points de sorts de base.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("Impossible d'utiliser .restat pendant un combat, un échange ou un dialogue.", Color.Red);
                return;
            }

            Client.Character.ResetCharacteristicsToBase(true);
            Client.Character.SendServerMessage("Vos caractéristiques et vos sorts ont été réinitialisés.");
        }
    }
}
