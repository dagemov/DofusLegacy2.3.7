using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Actors.Npcs;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("tiendas", RoleEnum.Player)]
    public class TiendasCommand : WorldCommand
    {
        public override string Description => "Lista tiendas virtuales. Uso: .tiendas";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            Client.Character.SendServerMessage("<b>Tiendas virtuales</b> (.tienda NUMERO o .tienda ALIAS)");
            foreach (var slot in VirtualShopCatalog.Slots.OrderBy(s => s.Number))
            {
                Client.Character.SendServerMessage(
                    $"  .tienda {slot.Number} — {slot.Label}  (ej: .tienda {slot.Aliases[0]})");
            }
        }
    }
}
