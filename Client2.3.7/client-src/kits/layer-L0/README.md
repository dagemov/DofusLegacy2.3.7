# Kit L0 — copiar y pegar

Tienda NPC + `.tienda 1-9` sin UI custom. **Fix precio** en `DofusInvoker.swf` (no toca `TradeCenter.swf` baseline).

## Copia rápida (recomendado)

```powershell
$GameRoot = "C:\ruta\a\tu\cliente"
cd "C:\Dofus\2.0.0\Client2.3.7\client-src\kits\layer-L0"
.\aplicar.ps1 -GameRoot $GameRoot
```

## Copia manual

Copia todo el contenido de esta carpeta sobre la raíz de tu cliente (donde está `DofusInvoker.swf`):

| Archivo en el kit | Destino |
|-------------------|---------|
| `DofusInvoker.swf` | `DofusInvoker.swf` |
| `ui/Ankama_TradeCenter/TradeCenter.swf` | `ui/Ankama_TradeCenter/TradeCenter.swf` |
| `ui/Ankama_TradeCenter/Ankama_TradeCenter.dm` | `ui/Ankama_TradeCenter/Ankama_TradeCenter.dm` |
| `ui/Ankama_TradeCenter/xml/stock.xml` | `ui/Ankama_TradeCenter/xml/stock.xml` |
| `data/Launcher/VerInfo.rec` | `data/Launcher/VerInfo.rec` |

## Hashes (MD5)

| Archivo | MD5 | Tamaño |
|---------|-----|--------|
| `DofusInvoker.swf` | `C6F6AE0EC99C72D1ED439156876A60F1` | 2153256 |
| `TradeCenter.swf` | `84B9B72E9745692050513C8059A7696B` | 55127 |
| `Ankama_TradeCenter.dm` | `B96C91FD12BCC2C7AE85BED5C7A94935` | 4426 |
| `stock.xml` | `D243A0A389919A1DD7DFA44C7D2D7C32` | 11196 |

## Qué hace el fix de precio

Al abrir tienda (5761), el Invoker sincroniza temporalmente `Item.getItemById().price` con el precio del servidor. El panel de compra (`ItemNpcStore` en TradeCenter vanilla) muestra el precio correcto. Al cerrar la tienda se restauran los precios D2O.

**No uses** un `TradeCenter.swf` recompilado (p. ej. `61DE7225`) — congela al personaje.

## Verificación

```powershell
.\Client2.3.7\client-src\preflight-client.ps1 -GameRoot $GameRoot -Layer L0
```

## Prueba in-game

1. Login sin error de `Ankama_TradeCenter`.
2. `.tienda 1` → abre tienda (sin congelar).
3. Clic en un ítem → lista y panel muestran el **mismo precio**.

## Regenerar Invoker (desarrolladores)

```powershell
.\Client2.3.7\client-src\build-dofusinvoker.ps1 -GameRoot $GameRoot
.\Client2.3.7\client-src\sync-kit-l0-tradecenter-baseline.ps1
```
