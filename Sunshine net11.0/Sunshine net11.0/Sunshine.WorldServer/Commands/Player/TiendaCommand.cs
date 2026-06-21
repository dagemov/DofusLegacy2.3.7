using Sunshine.Logs;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Actors.Npcs;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("tienda", RoleEnum.Player)]
    public class TiendaCommand : WorldCommand
    {
        public override string Description => "Abre tienda virtual. Uso: .tienda 1 | .tienda sombrero | .tiendas para lista";

        public override void Execute()
        {
            if (Client?.Character == null)
                return;

            var arg = Parameters != null && Parameters.Length > 0 ? Parameters[0]?.ToString()?.Trim() : null;
            if (string.IsNullOrEmpty(arg))
            {
                Client.Character.SendServerMessage("Uso: .tienda 1 | .tienda sombrero | .tienda 2 | .tienda capa — Lista: .tiendas");
                return;
            }

            if (!VirtualShopCatalog.TryResolveSlot(arg, out var slot))
            {
                Client.Character.SendServerMessage("Tienda desconocida. Usa .tiendas para ver la lista.");
                return;
            }

            Logger.WriteInfo($"[ShopTrace] .tienda charId={Client.Character.Id} slot={slot.Number} label={slot.Label} arg={arg}");
            VirtualShopCatalog.TryOpenShop(Client.Character, slot);
        }
    }
}
