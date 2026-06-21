#!/usr/bin/env python3
"""Patch VPS virtual shop sellerId resolution + exchange leave for stuck .tienda."""
from pathlib import Path
import sys

ROOT = Path("/opt/dofus-2.0.0-build/Sunshine net11.0/Sunshine net11.0")
REGISTRY = ROOT / "Sunshine.WorldServer/Game/Actors/Npcs/VirtualShopRegistry.cs"
INVENTORY = ROOT / "Sunshine.WorldServer/Handlers/Characters/Inventory/InventoryHandler.cs"
CATALOG = ROOT / "Sunshine.WorldServer/Game/Actors/Npcs/VirtualShopCatalog.cs"
DIALOG = ROOT / "Sunshine.WorldServer/Handlers/Dialogs/DialogHandler.cs"


def patch_inventory(text: str) -> str:
    old = """                // Tiendas virtuales (.tiendas): el cliente no tiene el actor en mapa, usa template id.
                if (isVirtualShop)
                    sellerId = npc.Record.Id;"""
    new = """                if (isVirtualShop)
                    sellerId = VirtualShopRegistry.Instance.ResolveVirtualSellerId(client.Character, npc);"""
    if new in text:
        return text
    if old not in text:
        raise SystemExit("InventoryHandler: virtual sellerId anchor not found")
    return text.replace(old, new, 1)


def patch_catalog(text: str) -> str:
    old = """            character.Dialog = null;
            Logger.WriteInfo(
                $"[ShopTrace] .tienda OPEN charId={character.Id}"""
    new = """            if (character.Dialog is NpcBuySellAction)
            {
                InventoryHandler.SendExchangeLeaveMessage(character.Client, true);
                character.Dialog = null;
            }

            Logger.WriteInfo(
                $"[ShopTrace] .tienda OPEN charId={character.Id}"""
    if new.split("Logger.WriteInfo")[0] in text:
        return text
    if old not in text:
        raise SystemExit("VirtualShopCatalog: dialog clear anchor not found")
    return text.replace(old, new, 1)


def patch_dialog(text: str) -> str:
    if "Dialog is NpcBuySellAction" in text:
        return text
    old = """            DialogHandler.SendLeaveDialogMessage(client);
        }

        public static void SendLeaveDialogMessage"""
    new = """            if (client.Character.Dialog is NpcBuySellAction)
            {
                InventoryHandler.SendExchangeLeaveMessage(client, true);
                client.Character.Dialog = null;
                return;
            }

            DialogHandler.SendLeaveDialogMessage(client);
        }

        public static void SendLeaveDialogMessage"""
    if old not in text:
        raise SystemExit("DialogHandler: leave anchor not found")
    text = text.replace(old, new, 1)
    if "using Sunshine.WorldServer.Game.Actors.Npcs.Actions;" not in text:
        text = text.replace(
            "using Sunshine.WorldServer.Client;",
            "using Sunshine.WorldServer.Client;\nusing Sunshine.WorldServer.Game.Actors.Npcs.Actions;\nusing Sunshine.WorldServer.Handlers.Characters.Inventory;",
        )
    return text


def main() -> int:
    for path in (REGISTRY, INVENTORY, CATALOG, DIALOG):
        if not path.exists():
            print(f"MISSING {path}")
            return 1

    inv = INVENTORY.read_text(encoding="utf-8")
    inv_new = patch_inventory(inv)
    if inv_new != inv:
        INVENTORY.write_text(inv_new, encoding="utf-8")
        print("Patched InventoryHandler.cs")

    cat = CATALOG.read_text(encoding="utf-8")
    cat_new = patch_catalog(cat)
    if cat_new != cat:
        CATALOG.write_text(cat_new, encoding="utf-8")
        print("Patched VirtualShopCatalog.cs")

    dlg = DIALOG.read_text(encoding="utf-8")
    dlg_new = patch_dialog(dlg)
    if dlg_new != dlg:
        DIALOG.write_text(dlg_new, encoding="utf-8")
        print("Patched DialogHandler.cs")

    reg = REGISTRY.read_text(encoding="utf-8")
    if "ResolveVirtualSellerId" not in reg:
        print("VirtualShopRegistry.cs must be synced from repo (ResolveVirtualSellerId missing)")
        return 1
    print("VirtualShopRegistry.cs OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
