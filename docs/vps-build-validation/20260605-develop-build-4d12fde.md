# Validación VPS — develop-build

| Campo | Valor |
|-------|--------|
| Fecha | 2026-06-05 16:21 UTC |
| Rama | `develop-build` |
| Commit | `4d12fde` (merge PR #14 + Fase 1 docs) |
| VPS | `174.138.35.107` |
| Path test | `/opt/dofus-2.0.0-build` |
| Path prod | `/opt/dofus-2.0.0` |

## Backup pre-test

| Artefacto | Ubicación |
|-----------|-----------|
| inspect JSON | `/opt/backups/sunshine-server-20260605-1621.json` |
| imagen Docker | `sunshine:prod-backup-20260605-1621` |
| logs | `/opt/backups/sunshine-server-20260605-1621.log` |

## Procedimiento

1. `docker stop sunshine-server` (liberar 2450 / 5557)
2. `git clone --branch develop-build` → `/opt/dofus-2.0.0-build`
3. `docker compose -f docker-compose.yml -f docker-compose.vps.yml build sunshine` → **OK**
4. `docker compose … up -d sunshine` → contenedor **Up**, 161 efectos cargados en logs
5. Puerto **5557** accesible; **2450** en arranque
6. Restauración: `stop` test + `compose up` desde `/opt/dofus-2.0.0/docker` → **RESTORE_OK**

## Resultado

| Criterio | Estado |
|----------|--------|
| Build Docker | **OK** |
| Runtime (WorldServer boot) | **OK** |
| EffectsLoader | 161 efectos |
| Restauración prod | **OK** |

## Notas

- MariaDB y Traefik no se detuvieron
- Fase 2 (solo docs) no requiere re-build; mismo SHA Sunshine hasta cambios `.cs`
