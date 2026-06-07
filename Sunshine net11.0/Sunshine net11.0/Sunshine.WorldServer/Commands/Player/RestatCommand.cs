using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("restat", RoleEnum.Player)]
    public class RestatCommand : WorldCommand
    {
        public override string Description => "Reinicia tus características y tus puntos de hechizo a los valores base.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("No puedes usar .restat durante un combate, un intercambio o un diálogo.", Color.Red);
                return;
            }

            Client.Character.ResetCharacteristicsToBase(true);
            Client.Character.SendServerMessage("Tus características y tus hechizos han sido reiniciados.");
        }
    }
}
