# Macro 4 / Phase 3B — D2I Writer Research

Date: `2026-06-04`  
Branch base: `feature/client-item-publication-d2o-item-class-phase3a`  
Estado: **`RESEARCH DONE`** (sin writer binario implementado aún)

## Resumen ejecutivo

| Pregunta | Respuesta corta |
| --- | --- |
| **P1** ¿Existe D2IReader/Writer tipado? | **No** con esos nombres. Hay **lectores binarios ad hoc** en Admin; **escritura i18n** en Rollback es vía **SWF + ActionScript tmp**, no `.d2i`. |
| **P2** ¿Sunshine lee `i18n_*.d2i`? | **No**. Solo resuelve **referencias** `nameId`/`descriptionId` dentro de D2O (`ReadFieldI18n`). |
| **P3** ¿Nuevos `nameId`/`descriptionId` en staging? | **Hoy no** de forma automatizada. **Viable** con prototipo basado en `D2iTextLookup` (formato ya invertido). |
| **P4** ¿Herramientas externas Rollback? | **FFDec / JPEXS** (SWF). **No** hay `D2OEditor`/`D2IEditor` en el repo. |
| **P5** ¿Template 7754 + nuevos IDs i18n? | **Sí, estrategia correcta** para cliente `2.3.7` (`D2O` + `D2I`). Stats vía `possibleEffects` (D2O) + DB. |

La hipótesis del ~70% de un lector oculto **se confirma parcialmente**: hay lectura D2I binaria en Admin, pero **no** en Sunshine ni bajo el nombre `D2IReader`. **DofusBeta** no está en este repo (solo referenciado en docs como ruta externa `DofusBeta-2.0/...`).

---

## Pregunta 1 — ¿Existe D2IReader / D2IWriter / I18nReader / I18nWriter?

### Sunshine (`Sunshine net11.0`)

| Símbolo buscado | Resultado |
| --- | --- |
| `D2IReader` / `D2IWriter` | **0** archivos |
| `D2iReader` / `D2iWriter` | **0** archivos |
| `I18nReader` / `I18nWriter` | **0** archivos |
| Relacionado | `D2OReader.ReadFieldI18n` / `D2OWriter.WriteFieldI18n` — lee/escribe **enteros** en D2O, **no** el archivo `.d2i` |

### RollblackLegacy Admin (Angular-tools)

| Componente | Tipo | Ruta |
| --- | --- | --- |
| **`D2iTextLookup`** | Lector binario `.d2i` | `RollblackLegacy.Admin.Infrastructure/.../FileSystemClientItemSourceReader.cs` |
| `D2iTextLookup` (QA) | Copia gitignored | `temporal-artifacts/DofusD2oScan/Program.cs` |
| Writer `.d2i` | **No existe** | — |

Formato implementado (lectura):

```txt
[data block UTF strings]
[int32 dataSize]
[int32 indexSize]
repeat: [int32 textId][int32 offset]
```

### legacy-reference / Rollback.Admin

| Componente | Qué hace | ¿Es D2I binario? |
| --- | --- | --- |
| **`ClientI18nTextService`** | Lee `i18n*.as` en `client/app/data/i18n_es/tmp` | **No** — ActionScript exportado |
| **`I18nCatalogService`** | Catálogo de textos desde `tmp/i18n*.as` | **No** |
| **`ItemClientPublishService.PublishTextAsync`** | Parchea **`i18n{chunk}.swf`** + escribe **`tmp/i18n{chunk}.as`** | **No** — lane SWF legacy |
| `SpellClientPublishService`, `MonsterClientPublishService`, `NpcClientPublishService`, `SetClientPublishService` | Mismo patrón `PublishTextAsync` | **No** |

### DofusBeta

- **No presente** en `DofusLegacy2.3.7`.
- Documentado como cliente **externo** (`C:\Users\Hombr\source\repos\DofusBeta-2.0\...`) con `i18n*.swf` + `tmp/i18n*.as` — misma familia que Rollback.Admin, **distinta** del lane `Client2.3.7` (`*.d2i`).

### Conclusión P1

No hay clase `D2IReader`/`D2IWriter` reutilizable como `D2OReader`/`D2OWriter`. Lo más cercano:

1. **Admin `D2iTextLookup`** — reader binario para `Client2.3.7`.
2. **Rollback `PublishTextAsync`** — writer de textos en **SWF/AS**, no en `.d2i`.

---

## Pregunta 2 — ¿Sunshine lee `i18n_es.d2i` / `i18n_en.d2i`?

**No.**

- Búsqueda en `Sunshine net11.0` → sin referencias a `.d2i` ni cargadores i18n.
- Sunshine runtime carga templates desde **DB** (`ItemsLoader`, etc.), no desde archivos i18n del cliente en disco.

**Sí lee (Admin Infrastructure):**

```txt
Client2.3.7/data/i18n/i18n_es.d2i  (~6.3 MB)
Client2.3.7/data/i18n/i18n_en.d2i  (~5.8 MB)
```

Usado por Client Identity, manifiesto de publicación y `FileSystemClientItemSourceReader`.

Ejemplo verificado (item control):

| Campo | Valor |
| --- | --- |
| Item `7754` | Dofus Ocre |
| `nameId` | `40904` |
| `descriptionId` | `40905` |

---

## Pregunta 3 — ¿Podemos crear nuevo `nameId` / `descriptionId` en staging?

### Estado actual

| Capacidad | Staging | Bloqueo |
| --- | --- | --- |
| Leer texto por `textId` | Sí (`D2iTextLookup`) | — |
| Escribir entrada nueva en `.d2i` | **No** | Sin `D2IWriter` |
| Manifiesto Admin | `BLOCKED_I18N_WRITER_MISSING` | `ItemPublicationManifestStates` |

### Lane Rollback (referencia, **no** aplicable tal cual a 2.3.7)

`ItemClientPublishService.AllocateNewTextIds()`:

- Escanea `i18n*.as` en tmp.
- Asigna `(MaxId+1, MaxId+2)` en el chunk con hueco.
- Publica vía parche SWF.

Eso **no escribe** `i18n_es.d2i` del cliente AIR actual.

### Prototipo Phase 3B recomendado (binario, staging)

1. Copiar `i18n_es.d2i` / `i18n_en.d2i` → `Infrastructure/staging-client/i18n-phase3b/`.
2. Implementar **`D2iWriter`** simétrico a `D2iTextLookup`:
   - append string en data block;
   - append par `(textId, offset)` en índice;
   - recalcular `dataSize` / `indexSize`.
3. Asignar IDs: `max(index keys)+1` (o hueco documentado en chunk lógico `textId/1000`).
4. Actualizar item staging (`12617`) con nuevos `nameId`/`descriptionId` vía `D2OWriter` (Phase 3A).

**Riesgo:** corrupción de índice si no se preserva endianness y offsets — mismo perfil que D2O.

---

## Pregunta 4 — ¿Herramientas externas usadas por Rollback?

| Herramienta | En repo | Uso documentado |
| --- | --- | --- |
| **JPEXS / FFDec** | Mencionado en docs | Extraer/editar **SWF** (`Items*.swf`, `i18n*.swf`) — lane legacy |
| **Ankama Tools** (oficial) | No integrado | N/A |
| **D2OEditor** | **No** | — |
| **D2IEditor** | **No** | — |
| Herramienta “bendita” D2I | **Pendiente** | `item-publication-pipeline.md` — gap explícito |

Rollback **no** depende de un editor D2I binario; depende de **FFDec + scripts AS** en el árbol `client/app/`.

Para **Client2.3.7**, la documentación interna prioriza **`D2O + D2I + D2P`** sobre FFDec (`admin-tools-migration-risk-register.md` R38).

Referencias externas útiles (fuera del repo, investigación):

- [DofusRE](https://github.com/Hydrofluoric0/DofusRE) — D2I read/write (versiones 2.51–2.60, no validado en 2.3.7).
- Comunidad: export JSON vía herramientas bot (AnkaBot doc) — no integrado aquí.

---

## Pregunta 5 — ¿Reutilizar Dofus Ocre `7754` solo cambiando nombre, descripción y stats?

**Sí — es la estrategia alineada con el pipeline.**

### Ya demostrado (Phase 3A)

| Paso | Estado |
| --- | --- |
| Clonar D2O `7754` → `12617` en staging | `DONE` |
| Mantener `nameId`/`descriptionId` de Ocre (`40904`/`40905`) | `DONE` (muestra texto Ocre en cliente hasta cambiar i18n) |
| Override `typeId=23`, `iconId=23012`, `appearanceId=0` | `DONE` |

### Para identidad nueva (nombre/descripción)

```txt
Items.d2o (staging): nameId=N1, descriptionId=N2  ← nuevos enteros
i18n_es.d2i: N1 → "Dofus de los Hielos" (ej.)
i18n_en.d2i: N1 → "Ice Dofus" (ej.)
```

No reutilizar `40904`/`40905` si el texto debe ser distinto — colisionaría con “Dofus Ocre”.

### Stats

| Capa | Mecanismo |
| --- | --- |
| **Cliente visible (efectos item)** | `possibleEffects` en `Items.d2o` (clases `EffectInstance*`) |
| **Runtime servidor** | `sunshine.items.Effects` / templates DB (Admin ya edita) |

Cambiar solo DB **sin** D2O deja el cliente con stats del template clonado (Ocre).

### Qué no hace falta tocar si se reutiliza 7754

- `iconId` `23012` (mismo arte Dofus Ocre) — válido para QA.
- Estructura base del item (peso, nivel, flags) — heredable del clone.

---

## Mapa del túnel (visión estratégica)

```txt
Angular (Items Builder)
    ↓
DB (templates + Effects)
    ↓
D2O staging writer     ← Phase 3A DONE
    ↓
D2I staging writer     ← Phase 3B NEXT (prototipo)
    ↓
Publication package (manifest + QA)
    ↓
Cliente (launcher patch — futuro)
```

Cerrar **D2I staging** desbloquea el manifiesto (`BLOCKED_I18N_WRITER_MISSING`) y acelera Sets, Vendors, NPCs, Quests y parte de Spells que comparten el mismo patrón `textId`.

---

## Recomendación Phase 3B implementación

| Prioridad | Tarea |
| ---: | --- |
| 1 | `D2iWriter` mínimo + tests round-trip en `Infrastructure/staging-client/i18n-phase3b/` |
| 2 | Modo CLI `--mode d2i-append-text` en `ClientItemPublicationPipeline` |
| 3 | Enlazar clone `12617` con nuevos `nameId`/`descriptionId` + textos ES/EN |
| 4 | Re-ejecutar Client Identity sobre staging (manifest → `READY_TO_STAGE` parcial) |

**No abrir** Spell/Glyph/Monster Builder hasta cerrar este eslabón.

---

## Referencias en repo

| Recurso | Ruta |
| --- | --- |
| Lector D2I Admin | `FileSystemClientItemSourceReader.cs` → `D2iTextLookup` |
| Auditoría D2I Phase 1 | [client-d2o-d2i-write-capability-audit.md](./client-d2o-d2i-write-capability-audit.md) |
| Phase 3A D2O | [client-publication-phase3a-d2o-item-class.md](./client-publication-phase3a-d2o-item-class.md) |
| Rollback publish item | `legacy-reference/Rollback.Admin/Services/ItemClientPublishService.cs` |
| I18N audit | [items-client-i18n-audit.md](../items-builder/items-client-i18n-audit.md) |
