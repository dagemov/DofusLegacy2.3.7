# NPC shop distribution (unified9)

Generated: 2026-06-21T05:02:51.481625+00:00

## Objetivo

- **9 tiendas fijas** alineadas a `.tienda 1` … `.tienda 9`.
- **Sin límite** de ítems por NPC.
- **Sin filtro por nivel** del personaje.
- Economía alineada a **75,000,000 kamas** iniciales.

## Sincronización de precios (servidor ↔ cliente)

| Capa | Campo | Rol |
|------|-------|-----|
| DB | `npcs_items.Price` | Precio autoritativo de compra cuando `> 0` |
| DB | `items.Price` | Plantilla D2O; fallback si override=0; base al vender |
| Servidor | `NpcShop.GetPrice()` | `Price > 0 ? Price : templatePrice` |
| Servidor | mensaje 5761 `objectPrice` | Precio enviado al cliente en lista tienda |
| Cliente L0 | `ItemWrapper.price` | Debe usarse en lista **y** panel compra |

**No sincronizar** precios de tienda vía D2O del cliente. El kit L0 (`TradeCenter.swf`) lee
`param1.price` del ítem de tienda en `ItemNpcStore`, no `dataApi.getItem().price`.

### Regenerar precios

```powershell
python tools/npc-shop-audit/run_npc_shop_audit.py --mode unified9 --source sql
```

Salida: `database/patches/npc-shop-unified9-apply.sql` + `economy-proposal.json`.

Aplicar en VPS:

```powershell
.\scripts\vps\apply-npc-shop-unified9.ps1
```

## Economía v2 (multiplicadores por categoría)

Bandas base por nivel + multiplicador por familia (sombrero/capa/arma/dofus premium;
consumibles/recursos baratos). Piezas de set: ×1.12 adicional.

| Nivel | Min | Max |
|-------|-----|-----|
| 1-20 | 25,000 | 250,000 |
| 21-60 | 250,000 | 1,500,000 |
| 61-120 | 1,500,000 | 8,000,000 |
| 121-180 | 8,000,000 | 22,000,000 |
| 181-999 | 18,000,000 | 32,000,000 |

| Familia | Multiplicador |
|---------|---------------|
| Anillos_amuletos | 0.55 |
| Armas_distance | 1.0 |
| Armas_melee | 1.0 |
| Capas | 1.0 |
| Cinturones_botas | 0.4 |
| Consumibles | 0.04 |
| Divers | 0.35 |
| Dofus_familiers | 1.0 |
| Escudos | 1.0 |
| Recursos | 0.015 |
| Sombreros | 1.0 |
| Set bonus | ×1.12 |

## Tiendas

| Slot | Comando | NpcId | Categoría | Items | Nivel min-max |
|------|---------|-------|-----------|-------|---------------|
| 1 | .tienda 1 | 9101 | Sombrero | 290 | 1-199 |
| 2 | .tienda 2 | 9102 | Capa | 214 | 1-197 |
| 3 | .tienda 3 | 9103 | Anillo y amuleto | 485 | 1-199 |
| 4 | .tienda 4 | 9104 | Cinturon y botas | 450 | 1-197 |
| 5 | .tienda 5 | 9105 | Escudo | 122 | 1-10 |
| 6 | .tienda 6 | 9106 | Consumible | 78 | 1-100 |
| 7 | .tienda 7 | 9107 | Recurso | 44 | 1-50 |
| 8 | .tienda 8 | 9108 | Dofus y mascota | 157 | 1-80 |
| 9 | .tienda 9 | 9109 | Diverso | 235 | 1-190 |

## Aplicar en VPS

1. Revisar `tools/npc-shop-audit/virtual-shops-unified9.json`.
2. `docker exec -i sunshine-db mariadb -uroot -p... sunshine < database/patches/npc-shop-unified9-apply.sql`
3. Sync `VirtualShopCatalog.cs` + rebuild `sunshine-server`.
