# Agent Handoff - Admin Tools Migration

Generated: `2026-06-04`

## Macro 4 / Phase 4 — Controlled backup & publish lane

| Campo | Valor |
| --- | --- |
| Rama | `feature/client-publication-controlled-patch-phase4` |
| Base | `feature/client-item-publication-staging-package-phase3c` |
| Estado | **`DONE`** |
| Docs | [client-publication-phase4-backup-recovery.md](../admin-tools/client-publication/client-publication-phase4-backup-recovery.md) |

Resultados:

- `Infrastructure/scripts/PublicationBackup/` — backup client/db/vps, restore dry-run/execute (sandbox), `update-publish-lane`
- `backups/` gitignored — `client`, `db`, `vps`
- Publish lane: `Infrastructure/staging-client/publish-lane/lane-state.json`
- API: `GET /api/admin/v1/publication/backup-status`
- Angular: `/admin/publication`
- [vps-publication-operations-guide.md](../admin-tools/client-publication/vps-publication-operations-guide.md) — comandos docker/compose verificados en repo
- **Sin** publish real, **sin** tocar `Client2.3.7` real

**Siguiente:** Macro 4 / **Phase 5** — aplicar patch en copia backup del cliente.

## Macro 4 / Phase 3C — referencia

Commit `c429204` — staging package validator, item 12617 `READY_FOR_CONTROLLED_PUBLISH`.

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-publication-controlled-patch-phase4
```

## Commit sugerido

```txt
feat: add controlled publication backup and recovery pipeline
```
