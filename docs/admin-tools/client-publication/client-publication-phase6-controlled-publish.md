# Macro 4 / Phase 6A — Controlled publish to real client

**Estado:** `READY_FOR_OPERATOR` (código en repo; publish real solo con operador)  
**Rama:** `feature/client-publication-controlled-publish-phase6`

## Prerrequisitos

1. Paquete validado: `Infrastructure/staging-client/publication-package-phase3c/12617`
2. Sandbox Phase 5: `VALID_SANDBOX_CLIENT`
3. `git status` limpio de cambios ajenos (no mezclar OneLauncher, config, etc.)

## A.1 Backup obligatorio

```bash
export CONFIRM_BACKUP=1
bash Infrastructure/scripts/PublicationBackup/backup-client.sh
```

PowerShell (si existe script equivalente en repo, usar bash vía Git Bash/WSL):

```powershell
$env:CONFIRM_BACKUP="1"
bash Infrastructure/scripts/PublicationBackup/backup-client.sh
```

Validar:

```txt
backups/client/YYYYMMDD-HHmmss/manifest.json
backups/client/YYYYMMDD-HHmmss/checksums.sha256
data/common/Items.d2o
data/i18n/i18n_es.d2i
data/i18n/i18n_en.d2i
```

Si falla: **DETENERSE — NO PUBLICAR**.

## A.2 Apply al cliente real

Solo con backup OK y confirmación explícita:

```bash
export CONFIRM_PUBLISH=1
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode apply-package-to-real-client \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617" \
  --client "Client2.3.7" \
  --target-item-id 12617
```

El pipeline verifica que el backup más reciente coincide con el cliente **antes** de escribir.

Archivos tocados (solo):

```txt
Client2.3.7/data/common/Items.d2o
Client2.3.7/data/i18n/i18n_es.d2i
Client2.3.7/data/i18n/i18n_en.d2i
```

Manifiesto post-apply: `Infrastructure/temporal-artifacts/client-real-publish/12617/real-client-apply-manifest.json`

## A.3 Validar cliente real

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-real-client \
  --client "Client2.3.7" \
  --target-item-id 12617
```

Esperado post-patch:

```txt
ItemId 12617: FOUND
nameId 63079: ES+EN
descriptionId 63080: ES+EN
IconId 23012: OK
Status: VALID_REAL_CLIENT
```

Reportes: `Infrastructure/temporal-artifacts/client-real-publish/12617/real-client-validation-report.{json,md}`

## A.4 Reinicio (manual, no automático)

```bash
export CONFIRM_RESTART=1
bash scripts/vps/restart-world-safe.sh
```

Ver [vps-publication-operations-guide.md](./vps-publication-operations-guide.md).

## A.5 QA operador

| Superficie | Qué validar |
| --- | --- |
| Admin API | manifest item 12617 |
| `/admin/items/12617/publication-status` | ya no `CLIENT_UNKNOWN` en cliente parcheado |
| Cliente/juego | login QA si el operador puede |

Browser: `PENDING_OPERATOR_BROWSER_QA`

## Rollback

Restaurar desde `backups/client/<timestamp>/` los tres archivos sobre `Client2.3.7`. No reiniciar hasta validar archivos restaurados.
