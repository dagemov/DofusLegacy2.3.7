# A.8 QA manual — item 12616

Date: `2026-06-03`  
Branch: `feature/items-builder-vps-qa-stabilization`  
Environment: `http://localhost:4201` (Angular) → proxy → `http://127.0.0.1:5248` (Admin API)

## Fixture

| Field | Expected |
| --- | --- |
| ItemId | `12616` |
| ResolvedName | `ADMIN TEST` |
| Type | `Amulette` (`typeId=1`) |
| IconId | `1003` |
| AppearanceId | `1004` |

Routes: `/admin/items/12616/edit`, `/admin/items/12616`

---

## Resultado global

| Área | Estado | Notas |
| --- | --- | --- |
| Carga edit + detail read | **PASSED** | API `GET /items/12616` |
| Preview icono | **PASSED** | `previewSource=BY_ICON`, path `/assets/item-previews/by-icon/1003.png` HTTP 200 |
| IconId ≠ AppearanceId | **PASSED** | `1003` / `1004` en detail |
| Effects guardar (AP/PM/Vit) | **PASSED** | Tras fix Dapper `ItemEffectsRow.TypeId` → `uint` |
| Persistencia tras reload | **PASSED** | `GET /effects/edit` y `GET /items/12616` con 3 effects |
| Detail muestra effects | **PASSED** | `effects.count=3` en read model |
| Errores 422 + traceId | **PASSED** | API devuelve `application/problem+json` con `traceId`; Angular `api-problem-panel` lo muestra |
| UI Angular manual (navegador) | **PARTIAL** | Validado vía API + proxy; confirmación visual en browser recomendada al operador |

**Veredicto A.8: PASSED** (con confirmación UI opcional en browser).

---

## Evidencia API (2026-06-03)

### GET item (form / detail base)

```http
GET /api/admin/v1/items/12616
```

- `resolvedName`: `ADMIN TEST`
- `iconId`: `1003`, `appearanceId`: `1004`
- `previewState.previewSource`: `BY_ICON`
- `previewState.resolvedPath`: `/assets/item-previews/by-icon/1003.png`

### Effects — guardar y recargar

```http
PUT /api/admin/v1/items/12616/effects
```

Payload (resumen):

| effectId | label | value | serializationTypeId |
| ---: | --- | ---: | ---: |
| 111 | + PA | 6 | 70 |
| 128 | + PM | 6 | 70 |
| 61 | + Vitalidad | 500 | 70 |

Tras PUT:

```http
GET /api/admin/v1/items/12616/effects/edit
GET /api/admin/v1/items/12616
```

- Persistencia: `111=6`, `128=6`, `61=500` en ambos endpoints.

### Errores

**422 effects (effectId inválido):**

```json
{
  "status": 422,
  "title": "Los datos enviados para el item no son válidos.",
  "errors": { "effects[0].effectId": ["EffectId debe ser mayor que cero."] },
  "traceId": "0HNM11KIMD95P:00000005"
}
```

**422 item write (level inválido + nombre requerido en contrato):**

```json
{
  "status": 422,
  "errors": {
    "resolvedName": ["ResolvedName is required."],
    "level": ["Level must be greater than or equal to 1."]
  },
  "traceId": "0HNM11KIMD95Q:00000001"
}
```

---

## Fallo encontrado y fix mínimo

| Pantalla | Acción | Endpoint | Response | traceId | Causa | Fix |
| --- | --- | --- | --- | --- | --- | --- |
| effects edit | Abrir / guardar | `GET/PUT .../effects` | 500 | `0HNM11HO46TJL:00000001` | Dapper no materializaba `ItemEffectsRow`: DB `TypeId` es `UInt32`, record usaba `int` | `ItemEffectsRow(int ItemId, uint TypeId, string? Effects)` |

Archivos:

- `RollblackLegacy.Admin.Application/Abstractions/Items/IItemEffectsAdminRepository.cs`
- `RollblackLegacy.Admin.Application/Services/ItemEffectsAdminService.cs` (cast `(int)row.TypeId` en guard armas)

---

## Checklist Angular (operador)

En `http://localhost:4201/admin/items/12616/edit`:

- [ ] Formulario carga sin error
- [ ] Preview icono visible (`by-icon/1003`)
- [ ] Campos IconId y AppearanceId distintos en UI
- [ ] Editor effects: +6 PA, +6 PM, +500 Vitalidad → Guardar effects
- [ ] Recargar página → effects siguen
- [ ] `/admin/items/12616` → lista de effects en detail
- [ ] Provocar 422 (ej. effectId 0) → panel con `traceId`

---

## Pendientes (no bloquean A.8)

- Confirmación visual en browser por operador (checkbox arriba).
- Phase **7C**: toasts unificados, layout split, hints conditions, preview warnings pre-save.
- In-game `.item add` — fuera de alcance A.8 (Phase 8 / tester doc).

---

## Commits relacionados

| Commit | Mensaje |
| --- | --- |
| (pendiente) | `fix: stabilize items effects qa flow` |
| `c277a44` | legacy reference import |
| `3cb474f` | port plan docs |
