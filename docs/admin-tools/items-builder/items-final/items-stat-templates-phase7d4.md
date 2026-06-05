# Phase 7D.4 — Item Stat Templates / Presets

Date: `2026-06-04`  
Branch: `feature/items-final-effects-catalog-audit-7d1`  
Status: `DONE`

## Scope

Presets de operador en Angular (sin cambios API/codec). Archivo de definición:

`Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effect-presets.ts`

## Presets incluidos

| Id | Nombre | Resolución |
| --- | --- | --- |
| `dofus-tester-qa` | Dofus Tester QA | 13× `effectId` auditados (`dofus-tester-item-creation.md`) |
| `dofus-basico` | Dofus básico | `labelMatch` + valores sugeridos |
| `amuleto-basico` | Amuleto básico | `labelMatch` |
| `botas-basicas` | Botas básicas | `labelMatch` (+ Huida = Esquiva) |
| `capa-sombrero-basico` | Capa/Sombrero básico | `labelMatch` |

## UI (`ItemEffectsEditorComponent`)

- Selector de preset + preview de líneas antes de aplicar.
- Modos:
  - **Añadir / fusionar** — merge por `effectId` en filas editables; conserva unsupported.
  - **Reemplazar editables** — confirmación; conserva unsupported y `preservedSuffixHex`.
- Líneas no resueltas en catálogo: aviso; no se inventan IDs.

## Dofus Tester QA — EffectIds (API verificado 2026-06-04)

| Stat | EffectId | Label API |
| --- | ---: | --- |
| +6 PA | 111 | + PA |
| +6 PM | 128 | + PM |
| +3 Alcance | 117 | + Alcance |
| +3 Invocaciones | 182 | + Invocaciones |
| +500 Vitalidad | 125 | + Vitalidad |
| +200 Prospección | 176 | + Prospeccion |
| +400 Potencia | 138 | 138 (IncreaseDamage — % daños en core) |
| +50 Daños | 112 | + Danos |
| +200 Sabiduría | 124 | + Sabiduria |
| +40 Retiro PA | 410 | APAttack |
| +40 Retiro PM | 412 | 412 |
| +50 Placaje | 753 | + Placaje |
| +50 Esquiva | 752 | + Huida |

Nota: `+ Potencia` en preset QA usa `138` (equivalente gameplay documentado), no `701` del label map legacy.

## Validación

| Check | Resultado |
| --- | --- |
| `npm run build` | PASS |
| `dotnet build` Admin.Api | PASS |
| Catálogo API ids Tester | 13/13 OK |
| Browser QA 12616 | PENDING_OPERATOR — `/admin/items/12616/edit` aplicar preset + guardar |

## Commit

```txt
feat: add item stat templates
```
