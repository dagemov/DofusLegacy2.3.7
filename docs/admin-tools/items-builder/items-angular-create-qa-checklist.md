# Checklist QA — Crear ítem/set desde Angular Admin

Fecha: `2026-06-07`  
Macro: Items/Sets Angular Production Flow — Fase 1  
Uso: operador + agente; complementa [items-production-qa-checklist.md](./items-production-qa-checklist.md)

---

## A. Antes de crear

- [ ] Rama de trabajo acordada (no mezclar Spell/Combat).
- [ ] `templateId` candidato verificado sin colisión (`items` + `items_weapons`).
- [ ] Tipo de ítem decidido: ¿arma? → plan `items_weapons` + clase `Weapon` en cliente.
- [ ] Nivel objetivo definido y **personaje QA** con nivel ≥ item.level identificado.
- [ ] Template visual de referencia elegido (ej. icon/type de item oficial existente).
- [ ] Backup VPS documentado si se toca producción.

---

## B. Guardado en servidor (Admin / SQL)

- [ ] Ítem existe en tabla correcta (`items` o `items_weapons`).
- [ ] `TypeId`, `Level`, `IconId`, `Name` coherentes con diseño.
- [ ] Si arma: `ApCost`, `Range`, `MinRange`, flags cast, `TwoHanded` poblados.
- [ ] `Effects` hex válido (round-trip Admin effects editor o SQL auditado).
- [ ] `Criteria` intencional (vacío para items custom sin restricción).
- [ ] Sin `FATAL_COLLISION` (mismo Id en ambas tablas).
- [ ] Admin detalle muestra preview IconId / AppearanceId aceptable.
- [ ] Si set: `itemSetId` apunta a set existente en `items_sets`.

---

## C. Publicación cliente

- [ ] Paquete staging generado (manifest + i18n + plan D2O).
- [ ] `Items.d2o`: entrada con `templateId`; clase `Item` o `Weapon` correcta.
- [ ] Armas: `criteria` vacío salvo restricción explícita.
- [ ] `d2o-inspect-ids` (o equivalente) **PASS** para el Id.
- [ ] `i18n_es` / `i18n_en`: nameId y descriptionId resueltos.
- [ ] `data/i18n/data.meta` MD5 actualizado.
- [ ] `data/common/data.meta` MD5 de `Items.d2o` actualizado.
- [ ] Backup pre-parche guardado.
- [ ] Parche launcher desplegado (si aplica entorno jugador).
- [ ] Admin `publication-status`: `CLIENT_KNOWN` / `VISIBLE` tras parche.

---

## D. Obtención in-game

- [ ] Jugador con cliente actualizado (reinicio launcher si se indica).
- [ ] Ítem obtenido por vía prevista (vendor, comando admin, drop) — **unidad nueva** post-parche.
- [ ] Nombre e icono correctos en inventario/tienda.
- [ ] Sin `!ui.*` ni nombre vacío en UI relevante.

---

## E. Equipamiento (obligatorio para equipables)

### Precondición

- [ ] Personaje QA nivel ≥ `item.level` (ej. no probar martillo 180 en personaje 158).

### Procedimiento

- [ ] Intentar equipar (drag o acción equip).
- [ ] Si falla: revisar si hay línea `[Equip]` en logs servidor.

### Resultado DB (`characters_items`)

- [ ] `Item` = templateId correcto.
- [ ] `ItemUid` > 0.
- [ ] `Position` = slot esperado (arma → `1` = `ACCESSORY_POSITION_WEAPON`).
- [ ] Tras desequipar: `Position` = `63` (inventario).

### Diagnóstico logs `[Equip]` (si packet llegó)

| `reason` | Interpretación |
| --- | --- |
| *(sin log)* | Bloqueo cliente o binario sin telemetría |
| `level-too-low` | Subir nivel personaje o bajar level item |
| `bad-position` | typeId / slot incorrecto |
| `item-not-found` | UID inventario desincronizado |
| `ok` + `canEquip=true` | Servidor aceptó |

### Casos de regresión (referencia)

- [ ] Arma oficial control (ej. `9117` en personaje 200) sigue equipando.
- [ ] Custom (ej. `12623` en personaje 200) equipa tras publicación.
- [ ] Caso negativo documentado: `12623` en personaje 158 → fallo **esperado**.

---

## F. Sets (cuando aplique macro Fase 7)

- [ ] Set visible en Admin sets list/detail.
- [ ] Todos los miembros publicados en cliente.
- [ ] Bonos de set aplican con N piezas equipadas.
- [ ] Rollback set documentado.

---

## G. Cierre

- [ ] Estado en Admin actualizado (objetivo: Validado en juego).
- [ ] Rollback disponible y probado en staging.
- [ ] Limitaciones conocidas documentadas.
- [ ] Sin cambios fuera de alcance (launcher/i18n/vendor salvo fase explícita).

---

## H. Criterio PASS macro

**PASS** cuando un ítem creado desde Angular (o su equivalente SQL+Admin) cumple B+E sin scripts manuales del operador final, y el diagnóstico de fallo (si ocurre) es legible en español desde Admin.

**PASS WITH NOTES** si criteria servidor no se evalúa (comportamiento actual documentado).

**FAIL** si el ítem es visible pero no equipable en personaje con nivel suficiente, sin diagnóstico claro.
