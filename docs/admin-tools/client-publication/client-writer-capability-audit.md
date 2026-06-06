# Auditoría — capacidades de escritura cliente (Phase 2)

Date: `2026-06-04`  
Branch: `feature/client-item-publication-pipeline-phase1` (Phase 2 research)  
Método: inventario de código + PoC `ClientD2oWriterResearch`

## Respuesta directa

| Pregunta | Respuesta |
| --- | --- |
| ¿Sunshine sabe **leer** D2O? | **Sí**, pero solo si existe clase C# tipada con `[D2OClass]` en el ensamblado |
| ¿Sunshine sabe **escribir** D2O? | **Sí a nivel de código** (`D2OWriter`), pero **no operativo** para `Items.d2o` en este repo |
| ¿Sunshine sabe leer/escribir D2I? | **No** — no hay reader/writer D2I en Sunshine |
| ¿Sunshine sabe escribir D2P? | **Parcial** — `D2pFile.Save()` para packs de assets |

## Tabla inventario

| Sistema | Read | Write | Estado en repo | Uso producción Items |
| --- | --- | --- | --- | --- |
| **D2O** | Sí (`D2OReader`) | Sí (`D2OWriter`) | **Writer no usable para Items.d2o** | Bloqueado sin clase `Item` |
| **D2I** | No (Sunshine) | No | Solo lectura en Admin (`D2iTextLookup`) | Bloqueado |
| **D2P** | Sí | Sí (`D2pFile.Save`) | Operativo para iconos/packs | Condicional (reutilizar IconId) |

## Subfase 2.1 — Sunshine.Protocol / Tools

### D2O

| Componente | Ruta | Rol |
| --- | --- | --- |
| `D2OReader` | `Sunshine.Protocol/Tools/D2o/D2OReader.cs` | Lee índice, tabla de clases, objetos tipados |
| `D2OWriter` | `Sunshine.Protocol/Tools/D2o/D2OWriter.cs` | `StartWriting` / `EndWriting`, `Write<T>(obj, index)` |
| Clases tipadas | `Tools/D2o/Classes/Breed.cs` | **Única** clase `[D2OClass]` en repo |
| Uso runtime | `BreedsLoader.cs` | `ReadObjects<Breed>()` sobre `Breeds.d2o` |

**Mecanismo de enlace clase:** `D2OReader.FindType(className)` exige un tipo C# con `IDataObject` cuyo nombre coincida con el D2O (p. ej. `"Item"`).

**Contenido real de `Client2.3.7/data/common/Items.d2o` (PoC):**

```txt
index entries: 11067
class definitions: Item, Weapon, EffectInstance, EffectInstanceDice, EffectInstanceInteger
```

**No existe** `Sunshine.Protocol.Tools.D2o.Classes.Item` en el repositorio.

### D2I

- Búsqueda en `Sunshine net11.0` → **0** archivos `D2I` / `D2i` writer.
- Admin Infrastructure: `D2iTextLookup` — **solo lectura** de `i18n_es.d2i` / `i18n_en.d2i`.

### D2P

| Componente | Read | Write |
| --- | --- | --- |
| `D2pFile` | Sí | `Save()` |
| `D2pEntry.WriteEntryDefinition` | — | Sí |
| Admin `Sunshine.Protocol.D2pReadOnly` | Extracción iconos | No write |

### Sunshine.Tools

No existe proyecto/carpeta `Sunshine.Tools` separado. Herramientas viven bajo `Sunshine.Protocol/Tools/`.

## Subfase 2.2 — Lectores alternativos (Admin)

| Componente | Read Items.d2o | Write |
| --- | --- | --- |
| `FileSystemClientItemSourceReader` / `RawD2oFile` | Sí (genérico por campos) | No |
| `FileSystemItemClientPublicationInspector` | Índice de ids | No |

Estos lectores **sí** alimentan Client Identity y el manifiesto Phase 1, pero **no** publican.

## Subfase 2.3 — PoC aislado (staging)

Ruta staging (no toca `Client2.3.7` original):

```txt
Infrastructure/staging-client/data/common/Items.d2o
```

Herramienta:

```bash
dotnet run --project infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj
```

Resultados (`Infrastructure/temporal-artifacts/client-d2o-writer-research/research-results.json`):

| Step | Resultado |
| --- | --- |
| `index_table_integrity` | **PASS** — 11067 ids, `7754` presente, `12617` ausente |
| `file_copy_hash` | **PASS** |
| `sunshine_d2o_reader_open_items` | **FAIL** — `Sequence contains no elements` (clase `Item` no mapeada en C#) |
| `d2o_class_table_probe` | **PASS** — confirma clase D2O `"Item"` en archivo |
| `sunshine_d2owriter_roundtrip_items` | **FAIL** — `D2OWriter` no puede abrir `Items.d2o` sin tipo `Item` |
| `item_12617_publish_feasibility` | **FAIL** — requiere writer genérico o generar `Item.cs` |

## Subfase 2.4 — Caso 12617

| Campo | Valor |
| --- | --- |
| En DB sunshine | Sí (`Dofus Tester`) |
| En Items.d2o índice | **No** |
| Sunshine D2OWriter directo | **No viable** sin clase `Item` |
| Camino recomendado Phase 3 | 1) Generar `Item.cs` desde esquema D2O **o** 2) Writer genérico binario (Admin `RawD2o` + rebuilder índice) en staging |

Template de referencia: `7754` (Dofus Ocre) — clonar campos en staging tras writer operativo.

## Impacto en roadmap

La hipótesis “60–70% D2OWriter listo para Items” es **parcialmente cierta**:

- **Código writer existe** (~70% del camino D2O está en Sunshine.Protocol).
- **Operativo para Items.d2o en este repo: ~0%** hasta añadir clase `Item` o capa genérica.

El pipeline objetivo sigue siendo válido; Phase 3 debe elegir:

```txt
A) Generar Item.cs (+ EffectInstance*) desde Items.d2o  → reutilizar D2OWriter tipado
B) Generic D2O index/object writer en Admin.Infrastructure  → no depender de Sunshine tipado
```

**D2I sigue siendo hueco crítico** — sin writer, publicar solo D2O deja nombres/tooltips rotos.

## Referencias

- Phase 1: [client-d2o-d2i-write-capability-audit.md](./client-d2o-d2i-write-capability-audit.md) (borrador inicial)
- PoC: [client-writer-proof-of-concept.md](./client-writer-proof-of-concept.md)
- Plan Phase 2: [client-publication-phase2.md](./client-publication-phase2.md)
