using Sunshine.Logs;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Commands;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("tiendas", RoleEnum.Player)]
    public class TiendasCommand : WorldCommand
    {
        public override string Description => "Abre el catalogo de tiendas. Uso: .tiendas";

        public override void Execute()
        {
            if (Client?.Character == null)
            {
                Logger.WriteWarning("[ShopTrace] .tiendas char=null");
                return;
            }

            var charId = Client.Character.Id;
            Logger.WriteInfo($"[ShopTrace] .tiendas charId={charId} inFight={Client.Character.IsInFight()} dialog={Client.Character.Dialog?.GetType().Name ?? "null"} registry={VirtualShopRegistry.Instance.Count}");

            if (Client.Character.IsInFight())
            {
                Client.Character.SendServerMessage("No puedes abrir las tiendas durante un combate.");
                return;
            }

            // Dialogo de tienda colgado: el servidor lo asigno pero el cliente no abrio la UI.
            if (Client.Character.Dialog is NpcBuySellAction)
            {
                Logger.WriteInfo($"[ShopTrace] .tiendas charId={charId} clearing stale NpcBuySellAction dialog");
                Client.Character.Dialog = null;
            }

            if (Client.Character.IsInDialog())
            {
                Client.Character.SendServerMessage("No puedes abrir las tiendas mientras tienes otro dialogo abierto.");
                return;
            }

            var shop = VirtualShopRegistry.Instance.GetFirstShop();
            if (shop == null)
            {
                Logger.WriteWarning($"[ShopTrace] .tiendas charId={charId} no shops in registry (count=0)");
                Client.Character.SendServerMessage("No hay tiendas configuradas.");
                return;
            }

            Logger.WriteInfo($"[ShopTrace] .tiendas charId={charId} opening template={shop.Record?.Id} actor={shop.Id} items={shop.Shops?.Count ?? 0}");
            new NpcBuySellAction(shop, Client.Character).Execute();
        }
    }
}
