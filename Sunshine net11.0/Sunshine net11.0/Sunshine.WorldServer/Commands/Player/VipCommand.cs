using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using System.Drawing;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("vip", RoleEnum.Player)]
    public class VipCommand : WorldCommand
    {
        public override string Description => "Muestra información VIP. Uso: .vip";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            bool isVip = Client.Account?.Vip == true;

            if (!isVip)
            {
                Client.Character.SendNotificationByServerMessage("No eres VIP. ¡Actívalo en la tienda!", NotificationEnum.INFORMATION);
                return;
            }

            Client.Character.SendServerMessage("=== BENEFICIOS VIP ===", Color.Gold);
            Client.Character.SendServerMessage("- XP en combates: x2", Color.Gold);
            Client.Character.SendServerMessage("- Kamas en combates: x2", Color.Gold);
            Client.Character.SendServerMessage("- Drop de monstruos: x2", Color.Gold);
            Client.Character.SendServerMessage("- XP de oficios: +50%", Color.Gold);
            Client.Character.SendServerMessage("- Cantidad al cosechar: x2", Color.Gold);
        }
    }
}
