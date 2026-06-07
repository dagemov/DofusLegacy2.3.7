using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("parchotage", RoleEnum.Player)]
    public class ParchotageCommand : WorldCommand
    {
        public override string Description => "Aplica un parchotage 101 en sabiduría, fuerza, agilidad, suerte, inteligencia y vitalidad.";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Parameters.Length > 0 && Parameters[0] != null && Parameters[0].ToString() != "101")
            {
                Client.Character.SendServerMessage("Uso: .parchotage 101", Color.Red);
                return;
            }

            if (Client.Character.IsInFight() || Client.Character.IsBusy())
            {
                Client.Character.SendServerMessage("No puedes usar .parchotage 101 durante un combate, un intercambio o un diálogo.", Color.Red);
                return;
            }

            Client.Character.ApplyParchotage101();
            Client.Character.SendServerMessage("Parchotage 101 aplicado en sabiduría, fuerza, agilidad, suerte, inteligencia y vitalidad.");
        }
    }
}
