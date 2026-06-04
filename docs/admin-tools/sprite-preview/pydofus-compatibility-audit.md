# PyDofus — auditoría de compatibilidad (auxiliar)

**Ruta analizada (solo lectura):** `C:\Users\Hombr\OneDrive\Escritorio\PyDofus-master`  
**No modificado.** **No es dependencia** del repo oficial.

## Resumen

| Capacidad | PyDofus | Pipeline C# oficial (Phase 6B) |
| --- | --- | --- |
| Leer D2P | Sí (`pydofus.d2p.D2PReader`) | Sí (`Sunshine.Protocol.Tools.D2p.D2pFile`) |
| Extraer PNG | Sí si entrada es PNG; SWL → SWF | Sí (`CatalogD2pIconResolver`, firma PNG) |
| Listar contenidos | Sí (`files` tras `load()`) | Sí (`D2pFile.Entries`) |
| Generar JSON catálogo | Manual vía `d2o_unpack` / scripts | `item-skin-catalog-dry-run` |
| Integración build | No | Sí |

## Hallazgos del código fuente

- `d2p_unpack.py` / `d2p_pack.py` — pack/unpack masivo desde carpeta `input/` (riesgo de extracción masiva sin `--limit`).
- Entradas `.swl` se convierten a `.swf` + `.json` — **no usar** para ítems bitmap PNG del catálogo (regla: no convertir D2P→SWF en Macro 4).
- `d2o_unpack.py` — solo unpack a JSON; útil como contraste, no reemplaza `ClientItemPublicationPipeline`.
- `d2i_unpack.py` / `d2i_pack.py` — ya cubierto por writer C# Phase 3B.

## Prueba runtime en este entorno

`python` no está en PATH del agente Windows — no se ejecutó unpack real. El operador puede validar:

```powershell
cd C:\Users\Hombr\OneDrive\Escritorio\PyDofus-master
# Copiar un bitmap*.d2p a input/ manualmente si hace falta
py -3 d2p_unpack.py
```

Artefactos locales recomendados (gitignored):

```txt
Infrastructure/temporal-artifacts/pydofus-audit/
```

## Decisión

- **Principal:** `ClientItemPublicationPipeline` + `ItemSpritePreviewPipeline` (C#).
- **Auxiliar:** PyDofus para experimentos manuales del operador.
- **Descartado como dependencia CI/build** hasta requisito explícito.

## JPEXS

Solo para SWF legacy; **no** para `bitmap*.d2p` del catálogo de ítems.
