# Macro Items Final — Effects Catalog & Blazor Parity

**Goal:** close Items Builder to **100% functional parity** with `legacy-reference/Rollback.Web` **before** Macro 4 (Spells).

**Status:** `IN_PROGRESS` — Phase 7D.1 audit complete; implementation phases 7D.2–7D.5 pending.

## Phases

| Phase | Doc | Scope | Status |
| --- | --- | --- | --- |
| 7D.1 | [items-effects-catalog-audit-phase7d1.md](./items-effects-catalog-audit-phase7d1.md) | Gap analysis: legacy catalog vs Admin API vs Angular UX | `DONE` |
| 7D.2 | (planned) | Full catalog API (`IItemEffectsCatalog`, extend `GET item-effects/options`) | `PENDING` |
| 7D.3 | [items-effects-editor-ui-phase7d3.md](./items-effects-editor-ui-phase7d3.md) | Angular editor parity (`EffectListEditor` MVP) | `DONE` |
| 7D.4 | (planned) | Templates and presets for common item stat bundles | `PENDING` |
| 7D.5 | (planned) | End-to-end QA vs Blazor workflows (item `12616`, resist sets, dice rows) | `PENDING` |

## Master plan

- [items-final-macro-plan.md](./items-final-macro-plan.md)

## Related (already shipped, incomplete for parity)

- Phase 7B codec + `PUT /items/{id}/effects` — serialization works; **catalog is curated (~26 stats), not full legacy dropdown**
- [items-builder-effects-serialization-audit.md](../items-builder-effects-serialization-audit.md)
- [items-builder-blazor-parity-audit.md](../items-builder-blazor-parity-audit.md)

## Prohibitions (this macro)

- Do **not** open Macro 4 Spells Builder.
- Do **not** implement EntityLook / Tiphon renderer (deferred from Macro 3).
- Do **not** bulk-extract client assets as a substitute for catalog metadata.
