using System;
using System.Linq;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("add", RoleEnum.Administrator)]
    public class AddVipCommand : WorldCommand
    {
        public override string Description => "Activa VIP en un jugador conectado. Uso: .add vip <nombre_jugador>";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            if (Parameters == null || Parameters.Length < 2 || (Parameters[0] ?? string.Empty).ToString().ToLowerInvariant() != "vip")
            {
                Client.Character.SendServerMessage("Uso: .add vip <nombre_jugador>", Color.Orange);
                return;
            }

            string playerName = (Parameters[1] ?? string.Empty).ToString().Trim();
            var target = CharacterManager.Instance.Characters.Values
                .FirstOrDefault(x => x != null && x.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase) && x.Client != null);

            if (target?.Client?.Account == null)
            {
                Client.Character.SendServerMessage($"El jugador '{playerName}' no existe o no está conectado.", Color.Red);
                return;
            }

            target.Client.Account.Vip = true;
            AccountManager.Instance.UpdateVip(target.Client.Account.Id, true);

            Client.Character.SendServerMessage($"VIP activado para {target.Name}.", Color.Green);
            target.SendNotificationByServerMessage("¡Felicidades! Ahora eres VIP. Usa .vip para ver tus beneficios.", NotificationEnum.INFORMATION);
        }
    }
}
