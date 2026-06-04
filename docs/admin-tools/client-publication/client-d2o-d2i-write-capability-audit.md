# Auditoría — capacidad de escritura D2O / D2I / D2P

Date: `2026-06-04`  
Phase: Macro 4 / Phase 1 (solo lectura)

## Resumen

| Formato | Reader en repo | Writer en repo | Uso Admin actual | Riesgo escritura Phase 2+ |
| --- | --- | --- | --- | --- |
| **D2O** | Sí (múltiple) | Sí (legado, sin Item tipado) | Client identity read-only | Alto — índice/clases corruptas |
| **D2I** | Sí (Admin infra) | **No** | Resolución nombres/descripciones | Alto — índice texto inconsistente |
| **D2P** | Sí | Parcial (`D2pFile`) | Sprite preview audit/extract | Medio — packs binarios |

## D2O

### Lectores

| Ubicación | Rol |
| --- | --- |
| `Sunshine.Protocol/Tools/D2o/D2OReader.cs` | Reader tipado legado (clases en `Classes/`, hoy `Breed` mínimo) |
| `RollblackLegacy.Admin.Infrastructure/.../FileSystemClientItemSourceReader.cs` | Reader genérico por campos (`RawD2oFile`) para client identity |
| `FileSystemItemClientPublicationInspector.cs` | Índice de ids en `Items.d2o` / `ItemTypes.d2o` |
| `Infrastructure/temporal-artifacts/DofusD2oScan` | Scanner QA gitignored |

### Writer

| Ubicación | Estado |
| --- | --- |
| `Sunshine.Protocol/Tools/D2o/D2OWriter.cs` | **Existe** — abre/crea D2O, sincroniza índice desde reader |

**Limitaciones:**

- Cobertura de clases tipadas casi vacía (`Breed.cs` únicamente en `Tools/D2o/Classes/`).
- No hay pipeline validado para clase **Item** en `Items.d2o`.
- Escribir sin mapeo genérico Item → corrupción o rechazo del cliente Flash/AIR.

**Riesgos:**

- Desalinear `nameId` / `descriptionId` con i18n.
- Duplicar `ItemId` en índice.
- Romper offsets de clases al reordenar entradas.

## D2I

### Lectores

| Ubicación | Rol |
| --- | --- |
| `D2iTextLookup` en `FileSystemClientItemSourceReader.cs` | Carga índice `textId → offset`, lectura UTF-8 |

### Writer

**No existe** writer D2I en el repo (ni en `Sunshine.Protocol` ni Admin).

**Implicación Phase 1:**

- Manifiesto marca `BLOCKED_I18N_WRITER_MISSING` para items no publicados.
- Publicar solo `Items.d2o` sin nuevas entradas i18n deja tooltips vacíos.

**Riesgos Phase 2:**

- Formato índice (dataSize + index table) debe preservarse.
- Reutilizar `DescriptionId` DB sin entrada i18n → nombre roto en cliente.

## D2P

| Componente | Lectura | Escritura |
| --- | --- | --- |
| `Sunshine.Protocol/Tools/D2p/D2pFile.cs` | Sí | Métodos de escritura de entries/properties |
| `Sunshine.Protocol.D2pReadOnly` (shared) | Extracción iconos pipeline | No |
| `ItemSpritePreviewPipeline` | Audit + extract icon | Copia curada PNG (no D2P pack) |

Iconos custom suelen requerir `bitmap*.d2p` **o** reutilizar `IconId` existente (ej. `23012`).

## Conclusión Phase 1

```txt
¿Existe writer D2O?  Sí (legado, no validado para Items)
¿Existe writer D2I?  No
¿Podemos escribir en Phase 1?  No (prohibido)
¿Podemos generar manifiesto dry-run?  Sí
```

Phase 2 debe prototipar en **copia staging** bajo `Infrastructure/temporal-artifacts/client-item-publication/` — nunca sobre `Client2.3.7` original.
