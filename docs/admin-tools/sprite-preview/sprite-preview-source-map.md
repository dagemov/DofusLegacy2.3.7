# Sprite Preview — Source Map

Mapa de fuentes reales para el pipeline Macro 3. Solo lectura en Phase 1.

## Cliente actual (`Client2.3.7`)

| Fuente | Tipo | Ruta | Formato | Icono | Appearance | Sprite equipado | Herramienta | Riesgo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Items | metadata | `data/common/Items.d2o` | D2O | indirecto (`iconId`, `appearanceId`) | indirecto | indirecto | Admin D2O reader (existente) | Bajo — solo lookup puntual |
| ItemTypes | metadata | `data/common/ItemTypes.d2o` | D2O | no | no | no | Admin D2O reader | Bajo |
| ItemSets | metadata | `data/common/ItemSets.d2o` | D2O | no | no | no | Admin D2O reader | Bajo |
| Appearances | metadata | `data/common/Appearances.d2o` | D2O | no | sí (índice) | sí (look) | Admin D2O reader (Phase 2+) | Medio — mapping no trivial |
| i18n ES/EN | texto | `data/i18n/i18n_es.d2i`, `i18n_en.d2i` | D2I | no | no | no | D2I lookup | Bajo |
| Item bitmap packs | gfx empaquetado | `content/gfx/items/bitmap0.d2p`, `bitmap1.d2p` | D2P | **sí** (`IconId`) | no directo | no | **`Sunshine.Protocol.D2p.D2pFile` (Phase 2 DONE)** | Medio — extracción puntual por `{iconId}.png` |
| Item vector packs | gfx empaquetado | `content/gfx/items/vector0.d2p`, `vector1.d2p` | D2P | alternativo UI | no | no | Lector D2P | Medio |
| Cliente binario | runtime | `cliente/DofusInvoker.swf`, etc. | SWF/AIR | no directo Admin | no | no | JPEXS/FFDec (legacy) | No usar como fuente principal 2.3.7 |

## Admin Angular (catálogo curado)

| Fuente | Tipo | Ruta | Formato | Icono | Appearance | Sprite equipado | Herramienta | Riesgo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| by-icon | PNG curado | `src/assets/item-previews/by-icon/{iconId}.png` | PNG | **sí** | no | no | Copia manual / import script | Bajo — controlado |
| by-item | PNG curado | `src/assets/item-previews/by-item/{itemId}.png` | PNG | sí (override) | no | no | Copia manual | Bajo |
| by-appearance | PNG curado | `src/assets/item-previews/by-appearance/{appearanceId}.png` | PNG | no | **sí** | parcial | Copia manual + investigación gfx | Medio |
| manual-assets | PNG legacy | `src/assets/manual-assets/items/{itemId}.png` | PNG | fallback | no | no | Filesystem resolver | Bajo |

## Legacy reference (`legacy-reference/`)

| Fuente | Tipo | Ruta | Formato | Icono | Appearance | Sprite equipado | Herramienta | Riesgo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Items SWF | metadata legacy | `Items0.swf` … | SWF | sí (Blazor) | parcial | no | JPEXS / FFDec | Solo referencia; no es cliente 2.3.7 |
| PNG bitmap exportado | gfx suelto | `client/app/content/gfx/items/bitmap/**/*.png` | PNG | **sí** si existe export | no | no | Copia selectiva | Medio — puede desalinearse del D2P actual |
| Rollback.Admin preview | código | `GameAssetPreviewService` | C# | resuelve `iconId.png` en bitmap folder | hint appearance | no | Port concept, no runtime | Documental |

## Base de datos (Sunshine)

| Fuente | Tipo | Ruta | Formato | Icono | Appearance | Sprite equipado | Herramienta | Riesgo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| sunshine.items | runtime | MySQL | SQL | `IconId` | `AppearanceId` | no | Admin API read-only | Bajo |

## Reglas de interpretación

```txt
IconId != AppearanceId
DB row + IconId != preview visible en Angular sin PNG curado o extractor D2P
CLIENT_KNOWN != icon preview disponible (ej. 7754 / IconId 23012)
```

## Estrategia D2P vs SWF vs PNG

| Rama | Cuándo usar | Phase 1 |
| --- | --- | --- |
| **D2P actual** | Fuente de verdad del cliente 2.3.7 para iconos empaquetados | Solo verificar presencia de `bitmap*.d2p`; **no extraer** |
| **SWF legacy** | Referencia Blazor / auditoría histórica | JPEXS/FFDec documentado; **no forzar** sobre D2P |
| **PNG curados** | Lo que Angular sirve hoy | Copia mínima (1–3 por fase) a `item-previews/` |

## Herramienta offline Phase 1

`Infrastructure/scripts/ItemSpritePreviewPipeline` — modo `audit` únicamente.
