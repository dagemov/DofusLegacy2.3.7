# Macro 4 / Phase 4 — Backup, recovery y publish lane

**Estado:** `DONE`  
**Rama:** `feature/client-publication-controlled-patch-phase4`

## Objetivo

Pipeline seguro de backup/recovery y carril de publicación controlada **sin publicar** al cliente real ni a producción.

## Componentes

| Pieza | Ubicación |
| --- | --- |
| Backup client/DB/VPS | `Infrastructure/scripts/PublicationBackup/` |
| Backups locales | `backups/{client,db,vps}/` (gitignored) |
| Publish lane | `Infrastructure/staging-client/publish-lane/lane-state.json` |
| API read-only | `GET /api/admin/v1/publication/backup-status` |
| UI | `/admin/publication` |

## Estados publish lane

- `READY` — paquete validado + backup cliente presente (publicación real sigue bloqueada)
- `NEEDS_BACKUP`
- `NEEDS_VALIDATION`
- `BLOCKED`

## Smoke local

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-client.ps1
$env:CONFIRM_BACKUP='1'; .\Infrastructure\scripts\PublicationBackup\backup-client.ps1

.\Infrastructure\scripts\PublicationBackup\backup-db.ps1
# requiere sunshine-db local

.\Infrastructure\scripts\PublicationBackup\update-publish-lane.ps1
```

## Siguiente

**Phase 5** — aplicar patch en copia backup del cliente (aún no cliente real).
