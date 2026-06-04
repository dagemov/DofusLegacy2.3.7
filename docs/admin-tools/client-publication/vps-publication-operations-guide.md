# Guía de operaciones VPS — publicación cliente (Phase 4)

Documentación basada en scripts **verificados en el repo** (`scripts/deploy-vps.ps1`, `scripts/vps/restart-world-safe.*`, `scripts/vps/backup-before-restart.sh`, `docker/docker-compose*.yml`).  
**No publicar al cliente real ni restaurar producción desde esta guía sin aprobación explícita.**

## Conexión SSH

| Parámetro | Valor por defecto en scripts |
| --- | --- |
| Host | `174.138.35.107` (`VPS_HOST`) |
| Usuario | `root` (`SSH_USER`) |
| Clave | `SSH/private_key_sebas.pem` en el repo (gitignored) o ruta local `Downloads/keys/private_key_sebas.pem` |

```bash
ssh -i /path/to/private_key_sebas.pem -o StrictHostKeyChecking=accept-new root@174.138.35.107
```

PowerShell (deploy):

```powershell
$sshKey = "C:\...\DofusLegacy2.3.7\SSH\private_key_sebas.pem"
ssh -i $sshKey root@174.138.35.107 "hostname && uptime"
```

## Stack Docker verificado en VPS

Ruta remota por defecto: `/opt/dofus-2.0.0`

Arranque (desde `scripts/deploy-vps.ps1`):

```bash
cd /opt/dofus-2.0.0/docker && docker compose --env-file ../.env \
  -f docker-compose.yml \
  -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml \
  up -d --build

cd /opt/dofus-2.0.0/docker && docker compose -f docker-compose-traefik.yml up -d
```

Contenedores definidos en `docker/docker-compose.yml` (nombres relevantes):

| container_name | Rol |
| --- | --- |
| `sunshine-db` | MariaDB — base `sunshine` |
| `sunshine-server` | World / emulador |
| `onelauncher-api` | API launcher (`docker-compose-onelauncher-api.yml`) |
| `rollblack-website` | Web (`docker-compose-website.yml`) |
| `traefik_proxy` | Traefik (`docker-compose-traefik.yml`) |

## Inventario y backup (Phase 4 scripts locales)

Desde el repo (no modifica producción por defecto):

```powershell
.\Infrastructure\scripts\PublicationBackup\backup-vps-state.ps1
$env:CONFIRM_BACKUP='1'; .\Infrastructure\scripts\PublicationBackup\backup-vps-state.ps1
```

Captura en `backups/vps/YYYYMMDD-HHmmss/vps-inventory.txt`:

- `hostname`, `uptime`
- `docker ps -a`
- `docker images`
- listado de `docker/` remoto
- cabecera de `docker compose config` del stack principal

### Backup DB en VPS (script existente)

`scripts/vps/backup-before-restart.sh` — mysqldump **selectivo** dentro de `sunshine-db`:

```bash
CONFIRM_BACKUP=1 ./scripts/vps/backup-before-restart.sh
```

Variables: `DB_CONTAINER_NAME=sunshine-db`, tablas `items`, `accounts`, `worlds_characters`, etc.  
Salida remota: `/root/backups/sunshine-focused-YYYYMMDD-HHmmss.sql`

## Validar backup

1. Comprobar tamaño del dump: `ls -lh /root/backups/sunshine-focused-*.sql`
2. Local: revisar `backups/client/*/manifest.json` y `checksums.sha256` tras `backup-client`
3. Admin API: `GET /api/admin/v1/publication/backup-status`

## Restore

**Producción:** no automatizado en Phase 4.

| Ámbito | Script | Destino real |
| --- | --- | --- |
| Cliente | `restore-client.ps1` dry-run / `-Execute` + `CONFIRM_RESTORE=1` | Solo `Infrastructure/staging-client/client-restore-sandbox/` |
| DB local dev | `restore-db.ps1` | Contenedor `sunshine-db` local |

Restore DB en VPS requiere procedimiento manual supervisado (no incluido en Phase 4).

## Reinicio seguro del world

Flujo obligatorio documentado:

```txt
backup (DB + inventario)
  ↓
(validar paquete staging + publish lane READY — sin publish real en Phase 4)
  ↓
restart world (solo tras CONFIRM)
  ↓
health checks
  ↓
online
```

**Nunca** `restart` sin backup previo.

### Script verificado: `scripts/vps/restart-world-safe.sh`

1. Descubre contenedor/servicio por nombre (`sunshine`, `world`):
   - `docker ps -a --format 'docker|{{.Names}}|{{.Image}}'`
   - fallback `systemctl` si aplica
2. Por defecto **no reinicia** (`CONFIRM_RESTART=0`)
3. Reinicio real:

```bash
CONFIRM_RESTART=1 ./scripts/vps/restart-world-safe.sh
```

Comando remoto ejecutado para Docker:

```bash
docker restart '<nombre-contenedor>' && docker logs --tail 50 '<nombre-contenedor>'
```

## Comandos operativos en VPS

```bash
docker ps -a
docker logs --tail 100 sunshine-server
docker logs --tail 100 sunshine-db
docker restart sunshine-server   # solo tras backup + CONFIRM explícito
cd /opt/dofus-2.0.0/docker && docker compose ps
```

## Health checks sugeridos

1. `docker ps` — `sunshine-server` y `sunshine-db` **Up**
2. `docker logs sunshine-server --tail 50` — sin excepciones fatales recientes
3. Puerto world publicado (`.env`: `WORLD_PORT`, por defecto `5557`)
4. Login cliente QA contra host publicado

## Publish lane (local)

```powershell
.\Infrastructure\scripts\PublicationBackup\update-publish-lane.ps1
```

Estados: `READY`, `BLOCKED`, `NEEDS_BACKUP`, `NEEDS_VALIDATION` — ver `Infrastructure/staging-client/publish-lane/lane-state.json`.

## Referencias repo

- [PublicationBackup README](../../../Infrastructure/scripts/PublicationBackup/README.md)
- [Phase 3C staging package](./client-publication-phase3c-staging-package.md)
- `scripts/deploy-vps.ps1`
- `scripts/vps/backup-before-restart.sh`
