# Publication Backup & Recovery (Phase 4)

Scripts locales para backup/recovery **sin publicar** al cliente real ni a producción.

## Reglas

- Por defecto los scripts solo **planifican** (dry-run) salvo variables `CONFIRM_*=1`.
- `restore-*` requiere `--execute` **y** `CONFIRM_RESTORE=1`.
- Los backups van a `backups/` en la raíz del repo (gitignored).

## Cliente (copia de referencia `Client2.3.7`)

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-client.ps1
$env:CONFIRM_BACKUP='1'; .\Infrastructure\scripts\PublicationBackup\backup-client.ps1
```

```bash
./Infrastructure/scripts/PublicationBackup/backup-client.sh
CONFIRM_BACKUP=1 ./Infrastructure/scripts/PublicationBackup/backup-client.sh
```

## Base de datos (`sunshine`)

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-db.ps1
$env:CONFIRM_BACKUP='1'; .\Infrastructure\scripts\PublicationBackup\backup-db.ps1
```

## VPS (solo inventario)

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-vps-state.ps1
$env:CONFIRM_BACKUP='1'; .\Infrastructure\scripts\PublicationBackup\backup-vps-state.ps1
```

## Restore (dry-run por defecto)

```powershell
.\Infrastructure\scripts\PublicationBackup\restore-client.ps1 -BackupId 20260604-120000
.\Infrastructure\scripts\PublicationBackup\restore-db.ps1 -BackupId 20260604-120000 -Execute
```

## Publish lane

```powershell
.\Infrastructure\scripts\PublicationBackup\update-publish-lane.ps1
```
