# Phase 7D.5 — Items Builder Final E2E QA

Date: `2026-06-04`  
Branch: `feature/items-final-effects-catalog-audit-7d1`  
Macro Items Final status: `DONE` (API/automation) / `PARTIAL` (browser operator pass)

## Macro closure summary

Phases 7D.1–7D.5 complete the Items Builder effects lane before Spells.

| Phase | Commit | Status |
| --- | --- | --- |
| 7D.1 Audit | `44632b8` | DONE |
| 7D.2 Catalog API | `10538e8` | DONE |
| 7D.3 Editor UX | `5a2fe50` | DONE |
| 7D.4 Presets | `d00ecaa` | DONE |
| 7D.5 QA | (this doc) | DONE |

**Spells Builder:** blocked until PR Macro Items Final is merged and explicitly approved.

## Build matrix (2026-06-04)

| Target | Result | Notes |
| --- | --- | --- |
| `RollblackLegacy.Admin.Api` | PASS | Tras `Stop-Process RollblackLegacy.Admin.Api` |
| `npm run build` | PASS | Budget +598 B (pre-existing) |
| `Sunshine.sln` | PASS | Tras detener `RollblackLegacy.Admin.Api` |

## API automation (LOCAL, Admin API `http://127.0.0.1:5249`)

### Caso 1 — Item `12616` (ADMIN TEST)

| Step | Result |
| --- | --- |
| `GET /items/12616/effects/edit` (before) | 1 fila (`111` + PA) |
| `PUT /items/12616/effects` — preset Dofus Tester QA (13 filas, type 70) | **PASS** |
| `GET /items/12616/effects/edit` (reload) | **13 filas** persistidas (111, 128, 117, …) |
| `GET /items/12616/publication-status` | Ejecutado — ver panel en browser para labels |

### Caso 2 — Item `7754` (Dofus Ocre)

| Step | Result |
| --- | --- |
| `GET /items/7754` | PASS — `IconId=23012`, `clientName=Dofus Ocre`, type Dofus |
| Client identity en detail | `itemId` conocido en metadata |
| `GET /items/7754/publication-status` | PASS (API) — validar `by-icon/23012` en browser |
| Preview icon | PENDING_OPERATOR — `/admin/items/7754` |

### Caso 3 — Item nuevo test

| Step | Result |
| --- | --- |
| Create item en shared DB | **PENDING_OPERATOR** — no crear filas adicionales en esta sesión (evitar `MAX(Id)+1` en DB compartida) |

Documentar en browser si el operador crea item de prueba:

```txt
ItemId:
Name:
IconId:
AppearanceId:
Preset aplicado:
Save/reload:
```

## Browser QA checklist (operator)

### 12616 — full lane

```txt
/admin/items/12616/edit
  - preset Dofus Tester QA → preview 13 líneas → aplicar (append o replace) → guardar
  - recargar → 13 efectos visibles
/admin/items/12616
  - detail effects list
/admin/items/12616/publication-status
  - publication + client identity cards
```

### 7754 — reference item

```txt
/admin/items/7754
/admin/items/7754/publication-status
  - icon preview 23012
  - client published / known
```

### Cross-cutting (Items Builder)

```txt
/admin/items — list + filters
/admin/items/icon-selector — modal
Create/edit shell (no new id unless disposable DB)
```

## PR Macro Items Final

Abrir **un solo PR** desde `feature/items-final-effects-catalog-audit-7d1` con commits:

```txt
44632b8 docs: audit final item effects catalog parity
10538e8 feat: add full item effects catalog api
5a2fe50 feat: add item effects editor parity ui
d00ecaa feat: add item stat templates
<hash> docs: record final items builder e2e qa
```

Título sugerido: `Macro Items Final — Items Builder effects parity`

## Known gaps (post-macro, no blockers Spells gate)

- MinMax / Duration / String row editing (7D.3 readonly).
- Browser publication-status labels — validar copy ES en UI.
- Item create en DB compartida — solo con dataset desechable.

## Next macro

**Macro 4 Spells Builder** — solo tras merge PR + aprobación explícita.
