# Notas técnicas — formato D2I (cliente 2.3.7)

Date: `2026-06-04`  
Fuente: `Client2.3.7/data/i18n/i18n_es.d2i`, `i18n_en.d2i`  
Implementación de referencia: `D2iTextLookup` (Admin) + `D2iFile` (pipeline Phase 3B)

## Resumen

| Pregunta | Respuesta |
| --- | --- |
| Magic / header | **No** hay cabecera `D2I`. El primer `int32` BE es el **offset** al bloque índice. |
| Índice | Tabla `(textId:int32, offset:int32)` tras el bloque de datos. |
| Encoding texto | `uint16` longitud BE + bytes **UTF-8**. |
| Claves | `int textId` → string (diccionario). |
| Diacríticos / overrides | **No** detectados en esta build (sin tabla secundaria). |
| Compresión | **No** (pool de strings en claro). |
| Bloque final | `indexSize` (`int32` BE) + pares id/offset hasta EOF. |

## Layout binario

```txt
[0..3]     dataSize (int32 BE) — offset absoluto donde empieza el índice
[4..dataSize-1]  pool de cadenas (cada una: uint16 len BE + UTF-8)
[dataSize..dataSize+3]  indexSize (int32 BE) — bytes de la tabla (= 8 * entryCount)
[dataSize+4..EOF]  repeat: textId (int32 BE), stringOffset (int32 BE)
```

`stringOffset` apunta al inicio de la cadena en el archivo (posición del `uint16` de longitud).

## Modelo de IDs entre idiomas

**Decisión Phase 3B:** el mismo `textId` (entero) se usa en `i18n_es.d2i` e `i18n_en.d2i`; cada archivo contiene el texto en su idioma para ese id.  
`Items.d2o` guarda un solo `nameId` y un solo `descriptionId` que resuelven contra el archivo i18n del locale del cliente.

No hay `NameIdEs` / `NameIdEn` separados en D2O — solo `nameId` / `descriptionId`.

## Lectura (Admin `D2iTextLookup`)

```csharp
var dataSize = reader.ReadInt32BigEndian(); // offset 0
reader.Position = dataSize;
var indexSize = reader.ReadInt32BigEndian();
// index at dataSize + 4, length indexSize
```

## Escritura staging (`D2iFile.Save`)

1. Ordena entradas por `textId`.
2. Escribe pool de strings comenzando en offset `4`.
3. Escribe índice al final.
4. Escribe `dataSize = 4 + poolLength` en offset `0`.

**No** sobrescribe el archivo fuente: siempre `Save(path)` a copia staging.

## PoC medido (inspect)

| Archivo | Tamaño | Entradas índice | textId máximo |
| --- | ---: | ---: | ---: |
| `i18n_es.d2i` | ~6.3 MB | 62710 | 63078 (antes de append) |
| `i18n_en.d2i` | ~5.8 MB | 62710 | 63078 |

Tras append control (+2 textos por archivo): **62712** entradas; nuevos ids **63079** (nombre) y **63080** (descripción) en la primera ejecución de append del PoC.

## Limitaciones conocidas

- Re-serializar reordena el pool (offsets cambian); el cliente solo usa el índice.
- `Update` de texto existente no implementado (solo `AppendText`).
- Sin validación de unicidad global entre chunks `textId/1000` (Rollback legacy sí validaba chunks SWF).
