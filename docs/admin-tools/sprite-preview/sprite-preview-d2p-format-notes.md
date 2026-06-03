# D2P — Notas de formato (cliente 2.3.7)

Fuente de lectura reutilizada: `Sunshine.Protocol.Tools.D2p` en `Sunshine net11.0/Sunshine net11.0/Sunshine.Protocol/Tools/D2p/`, expuesta al pipeline vía `infrastructure/shared/Sunshine.Protocol.D2pReadOnly/`.

## Archivos auditados

| Archivo | Tamaño aprox. | Entradas | Link |
| --- | ---: | ---: | --- |
| `bitmap0.d2p` | 8.4 MB | 2634 | → `bitmap1.d2p` |
| `bitmap1.d2p` | 8.3 MB | 2716 | — |
| `vector0.d2p` | 5.5 MB | 2634 | → `vector1.d2p` |
| `vector1.d2p` | 5.2 MB | 2716 | — |

Ruta: `Client2.3.7/content/gfx/items/`

## Estructura detectada

1. **Cabecera:** bytes `0x02`, `0x01` (si falla → `Corrupted d2p header`).
2. **Tabla al final del archivo** (24 bytes, seek desde EOF):
   - `OffsetBase` (int BE)
   - `Size` (int BE)
   - `EntriesDefinitionOffset` (int BE)
   - `EntriesCount` (int BE)
   - `PropertiesOffset` (int BE)
   - `PropertiesCount` (int BE)
3. **Propiedades** (opcional): pares clave/valor UTF; clave `link` apunta a otro `.d2p` del mismo pack.
4. **Definiciones de entrada** (`EntriesCount` veces):
   - `FullFileName` (UTF) — ruta lógica, ej. `23012.png` o `dofus_png/23012.png`
   - `Index` (int BE) — offset relativo a `OffsetBase`
   - `Size` (int BE) — longitud del payload
5. **Payload:** `ReadBytes(Size)` en `OffsetBase + Index` — en los iconos probados es **PNG crudo** (firma `89 50 4E 47`).

## Compresión

El lector Sunshine **no** aplica descompresión al leer entradas D2P. Los iconos de inventario auditados (`1001`, `23012`) salen como PNG válido directamente.

## Búsqueda por IconId

Convención observada en `bitmap*.d2p`:

```txt
{iconId}.png
```

Ejemplos validados:

| IconId | Pack | Entry path | PNG |
| --- | --- | --- | --- |
| 1001 | bitmap0.d2p | `1001.png` | sí |
| 23012 | bitmap0.d2p | `23012.png` | sí |

El resolver del pipeline compara `Path.GetFileName(entry) == "{iconId}.png"` (case-insensitive).

## Vector D2P

`vector*.d2p` comparten conteo de entradas con bitmap y enlaces `vector0 → vector1`. Phase 2 no extrajo vector; iconos de inventario usan **bitmap** en los casos de control.

## Herramientas que no aplican

- **JPEXS / FFDec:** SWF/AIR legacy, no sustituyen este lector D2P.
- **Extracción masiva:** prohibida; usar `--mode extract-icon --icon-id N` puntual.

## Referencia código existente

| Componente | Ruta |
| --- | --- |
| `D2pFile` | `Sunshine.Protocol/Tools/D2p/D2pFile.cs` |
| Uso maps | `Sunshine.BaseServer/Loaders/World/Maps/MapsLoader.cs` (`maps0.d2p`) |
| Pipeline Phase 2 | `Infrastructure/scripts/ItemSpritePreviewPipeline/D2p/` |
