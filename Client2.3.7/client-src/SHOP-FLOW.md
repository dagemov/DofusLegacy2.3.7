# Flujo tienda NPC / `.tiendas` — kit L0 único

## Reglas

1. **Invoker L0** — baseline `DFFED0C8` + parche `ExchangeManagementFrame` (`C6F6AE0E`) para precio tienda.
2. **TradeCenter baseline** — `84B9B72E` vanilla; **no recompilar** (congela al personaje).
3. **Kit único** — `client-src/kits/layer-L0/` + `aplicar.ps1`.

---

## Kit L0

| Archivo | Rol |
|---------|-----|
| `DofusInvoker.swf` | Invoker `DFFED0C8` |
| `TradeCenter.swf` | UI tienda (baseline + parches L0) |
| `Ankama_TradeCenter.dm` | Sin `stockNpcVirtual` |
| `xml/stock.xml` | UI vanilla NPC + `.tiendas` |
| `data/Launcher/VerInfo.rec` | Hashes launcher |

Copia: ver [`kits/layer-L0/README.md`](kits/layer-L0/README.md).

---

## Fix precio tienda (ItemNpcStore)

El panel de compra debe usar `ItemWrapper.price` del mensaje 5761 (`objectPrice` del servidor), no `dataApi.getItem().price` (D2O plantilla ~1 K).

Parche en `tradecenter/scripts/scripts/ui/ItemNpcStore.as` → rebuild → SWF en kit L0.

---

## Personaje bloqueado (5761 sin UI)

```
Servidor 5761 → ExchangeManagementFrame → isInExchange=true
  → TradeCenter hook ExchangeStartOkNpcShop → loadUi(stockNpcStore)
```

Si no aparece ventana: `/quit` o relog. Revisar consola al login (`Ankama_TradeCenter`).

---

## Herramientas

### Preflight

```powershell
$GameRoot = "C:\ruta\a\tu\cliente"
.\Client2.3.7\client-src\preflight-client.ps1 -GameRoot $GameRoot -Layer L0
```

### Build TradeCenter (L0)

```powershell
.\Client2.3.7\client-src\build-tradecenter.ps1 -GameRoot $GameRoot
```

Overlay actual: `ui/ItemNpcStore.as` (precio tienda), `ui/EstateForm.as` (sin AddMapFlag).

---

## Comandos chat (servidor)

| Comando | Tienda |
|---------|--------|
| `.tiendas` | Lista 9 categorías |
| `.tienda 1` … `.tienda 9` | Sombrero … Diverso |

Servidor: NPCs 9101–9109, precios en `npcs_items.Price`.

---

## Rollback

Restaurar kit L0 completo desde `client-src/kits/layer-L0/`.
