# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 6 — Controlled publish + item skin catalog plan

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-publication-controlled-publish-phase6` |
| Base | `feature/client-publication-controlled-patch-phase5` (commits `7397c47`, `035bd6c`, `65e83bb`) |
| Estado | **`READY_FOR_OPERATOR`** |
| Docs | [client-publication-phase6-controlled-publish.md](../admin-tools/client-publication/client-publication-phase6-controlled-publish.md), [item-skin-catalog-plan-phase6.md](../admin-tools/sprite-preview/item-skin-catalog-plan-phase6.md) |

### Parte A — Publish real (código listo)

- CLI: `apply-package-to-real-client` (requiere `CONFIRM_PUBLISH=1` + backup client valido)
- CLI: `validate-real-client`
- **No** reinicio automatico; ver VPS guide
- Publish real: solo operador tras `CONFIRM_BACKUP=1`

### Parte B — Skin catalog

- CLI: `item-skin-catalog-dry-run` → `Infrastructure/temporal-artifacts/item-skin-catalog/`
- Excluye armas (`WeaponTypeFilter`)
- Angular: `src/assets/item-previews/by-category/*/.gitkeep` (sin PNG masivo)

### QA

| Item | Estado |
| --- | --- |
| Builds pipeline / API / Angular | Verificar en gate |
| Browser | `PENDING_OPERATOR_BROWSER_QA` |
| Publish real 12617 | `PENDING_OPERATOR` (backup + CONFIRM_PUBLISH) |

### Commits sugeridos

```txt
feat: add controlled real client publish command
feat: add item skin catalog dry run
docs: plan item skin catalog by category
docs: record controlled client publish qa
```

## Macro 4 / Phase 5 — referencia

`DONE` — sandbox, UX stats, 3 commits en `feature/client-publication-controlled-patch-phase5`.

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-publication-controlled-publish-phase6
```
