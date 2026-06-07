# Estándar — Item visible + equipable + publicable (borrador Fase 1)

Fecha: `2026-06-07`  
Estado: **BORRADOR** — base para Fase 2 formal; derivado de auditoría + caso `12623–12627`  
Audiencia: operador Admin (sin conocimiento D2O/D2I/Docker)

## Regla de producto

Todo ítem o set creado desde Angular debe poder quedar en:

**VISIBLE + OBTENIBLE + EQUIPABLE (con nivel suficiente) + PUBLICADO EN CLIENTE**

El operador no ejecuta scripts manuales ni interpreta logs técnicos.

---

## Estados objetivo (UX futura — Fase 3)

| Estado operador | Significado |
| --- | --- |
| Borrador | Solo diseño en Admin; no persistido en servidor |
| Guardado en servidor | Fila en `items` o `items_weapons`; runtime Sunshine |
| Pendiente publicación cliente | Servidor OK; cliente no conoce template |
| Publicado en cliente | `CLIENT_KNOWN` + checksums meta OK |
| Requiere actualizar cliente | Jugador debe reiniciar launcher / descargar parche |
| Validado en juego | QA equip + visibilidad confirmados |
| Error de publicación | Bloqueo con mensaje accionable en español |
| Rollback disponible | Backup previo restaurable desde Admin |

---

## Contrato mínimo por ítem

### Identidad

| Campo | Obligatorio | Notas |
| --- | --- | --- |
| `templateId` (GID) | Sí | Libre en `items` **y** `items_weapons` (sin colisión) |
| `resolvedName` | Sí | Persistido en DB (`Name`) |
| `description` | Recomendado | Hoy **no** persiste desde Angular → gap Fase 5+ |
| `nameId` / `descriptionId` | Sí para cliente | vía pipeline i18n |
| `typeId` | Sí | Determina tabla y clase D2O |
| `level` | Sí | Bloquea equip si personaje < level |
| `iconId` | Sí | Preview + inventario |
| `appearanceId` | Si equipable visual | Capas, capas, mascotas, etc. |

### Runtime DB (servidor)

| Campo | Obligatorio | Tabla |
| --- | --- | --- |
| `typeId`, `level`, `weight`, `price` | Sí | `items` o `items_weapons` |
| `criteria` / `conditions` | Opcional | Vacío recomendado salvo restricción explícita |
| `effects` | Según diseño | Hex runtime (`EffectManager`) |
| `itemSetId` | Si pertenece a set | FK lógica a `items_sets` |
| `usable`, `targetable`, `twoHanded`, `etheral` | Según tipo | Flags estándar |

### Metadata arma (`items_weapons` + D2O `Weapon`)

Obligatorio si `typeId` ∈ tipos arma (martillo, bastón, espada, etc.):

| Campo | Fuente referencia |
| --- | --- |
| `ApCost`, `MinRange`, `Range` | Clonar arma oficial misma familia |
| `CastInLine`, `CastInDiagonal`, `CastTestLos` | Idem |
| `CriticalHitProbability`, `CriticalHitBonus`, `CriticalFailureProbability` | Idem |
| `possibleEffects` en D2O | Clon binario; no `D2OWriter` |
| `criteria` en D2O | `''` salvo restricción explícita |
| Clase D2O | **`Weapon`** |

### Cliente (publicación)

| Artefacto | Validación |
| --- | --- |
| `Items.d2o` | Índice `templateId`; clase correcta |
| `i18n_es.d2i` / `i18n_en.d2i` | nameId + descriptionId |
| `data/i18n/data.meta` | MD5 i18n actualizado |
| `data/common/data.meta` | MD5 `Items.d2o` actualizado |
| Launcher patch lane | ZIP desplegado; jugador con parche ≥ requerido |

---

## ¿`items` o `items_weapons`?

| Criterio | Tabla |
| --- | --- |
| typeId de arma (2–8, 19–22, 83, 99, 114, …) | **`items_weapons`** |
| Equipo no-arma, consumible, dofus, recurso, etc. | **`items`** |
| Mismo Id en ambas tablas | **PROHIBIDO** (`FATAL_COLLISION`) |

`ItemManager` carga ambas en un solo diccionario por `Id`.

---

## Validaciones obligatorias (checklist automático — Fase 4)

### Servidor

1. Template existe en tabla correcta.
2. `ItemManager.Items.ContainsKey(templateId)` (probe en WorldServer o simulación read).
3. Sin colisión `items` + `items_weapons`.
4. Si arma: fila weapon completa (ApCost, Range, …).
5. Si set: `items_sets.Id` existe y miembros referencian `itemSetId`.

### Cliente

1. `CLIENT_KNOWN` en Items.d2o.
2. Clase D2O = `Weapon` si arma.
3. i18n ES/EN resuelve nameId/descriptionId.
4. `data.meta` coherente (common + i18n).
5. `criteria` D2O coherente con política (vacío por defecto custom).

### Equip

1. Personaje QA con `nivel ≥ item.level`.
2. Tras equip: `characters_items.Position` = slot arma (`1`) u otro según tipo.
3. Log `[Equip]` con `canEquip=true` si packet llegó al servidor.
4. Diagnósticos conocidos:
   - `client-blocked` — sin línea `[Equip]`
   - `server-level-too-low` — charLevel < itemLevel
   - `bad-position` / `bad-type` — typeId o slot
   - `missing-template` — ItemManager
   - `equip-ok`

### Criteria (servidor)

**No** forma parte del gate de equip hoy. Ver [server-item-criteria-audit-20260607.md](./server-item-criteria-audit-20260607.md).  
El estándar documenta criteria para **tooltip/cliente**, no para bloqueo server-side actual.

---

## Casos de referencia (evidencia)

| Caso | Resultado esperado |
| --- | --- |
| `12623` + Kuutar (158) | **No equipa** — nivel insuficiente |
| `9117` + Thero (200) | **Equipa** — `items_weapons` OK |
| `12623` + Thero (200) | **Debe equipar** tras publicación cliente |
| Set Boreal pieza `CA<600` | Puede equipar con suerte alta — servidor ignora criteria |

---

## Flujo objetivo one-click (Fase 5)

```
Guardar en Admin
  → Validar (Fase 4)
  → Backup automático
  → Publicar cliente (D2O + i18n + meta)
  → (Opcional) promover lane launcher
  → Reportar estado en Angular
  → QA equip integrado (Fase 6)
  → Marcar "Validado en juego"
```

Rollback: restaurar backup cliente + SQL restore documentado.

---

## Sets (extensión Fase 7)

Mismo estándar aplicado a:

- `items_sets` en DB
- Miembros con `itemSetId` publicados individualmente
- `ItemSets.d2o` + bonuses en cliente
- QA: bonos de set visibles con piezas equipadas

---

## Qué NO incluye este estándar (fases posteriores)

- Vendor/NPC automático
- Parser global criteria en servidor
- Publicación SWF/D2P masiva
- Spell Builder / Combat
